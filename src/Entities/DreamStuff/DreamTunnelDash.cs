using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Entities {
    public static class DreamTunnelDash {

        #region Vanilla Constants

        internal const float Player_DashSpeed = 240f;
        internal const float Player_ClimbMaxStamina = 110f;
        internal const float Player_DreamDashMinTime = 0.1f;
        internal const int Player_DashCornerCorrection = 4;

        #endregion

        #region CommunalHelper Constants

        private const string Player_dreamTunnelDashCanEndTimer = "communalHelperDreamTunnelDashCanEndTimer";
        private const string Player_solid = "communalHelperSolid";

        #endregion

        public static int StDreamTunnelDash;
        private static bool hasDreamTunnelDash;
        public static bool HasDreamTunnelDash {
            get { return hasDreamTunnelDash || CommunalHelperModule.Settings.AlwaysActiveDreamRefillCharge; }
            set { 
                hasDreamTunnelDash = value;
            }
        }
        private static bool dreamTunnelDashAttacking;
        private static float dreamTunnelDashTimer;

        private static bool overrideDreamDashCheck;

        public static Color[] DreamTrailColors;
        public static int DreamTrailColorIndex = 0;


        private static IDetour hook_Player_DashCoroutine;
        private static IDetour hook_Player_orig_Update;
        private static IDetour hook_Player_orig_UpdateSprite;

        public static void Load() {
            On.Celeste.Player.ctor += Player_ctor;
            On.Celeste.Player.DashBegin += Player_DashBegin;
            On.Celeste.Player.CreateTrail += Player_CreateTrail;
            IL.Celeste.Player.OnCollideH += Player_OnCollideH;
            IL.Celeste.Player.OnCollideV += Player_OnCollideV;
            On.Celeste.Player.DreamDashCheck += Player_DreamDashCheck;
            On.Celeste.Player.Update += Player_Update;
            On.Celeste.Player.Die += Player_Die;

            hook_Player_DashCoroutine = new ILHook(
                typeof(Player).GetMethod("DashCoroutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget(),
                Player_DashCoroutine);
            IL.Celeste.Player.IsRiding_Solid += State_DreamDashEqual;
            IL.Celeste.Player.IsRiding_JumpThru += State_DreamDashEqual;
            IL.Celeste.Player.OnCollideH += State_DreamDashEqual;
            IL.Celeste.Player.OnCollideV += State_DreamDashEqual;
            hook_Player_orig_Update = new ILHook(
                typeof(Player).GetMethod("orig_Update"),
                Player_orig_Update);
            hook_Player_orig_UpdateSprite = new ILHook(
                typeof(Player).GetMethod("orig_UpdateSprite", BindingFlags.NonPublic | BindingFlags.Instance),
                State_DreamDashEqual);

            On.Celeste.Level.EnforceBounds += Level_EnforceBounds;
            On.Celeste.Player.OnBoundsH += Player_OnBoundsH;
            On.Celeste.Player.OnBoundsV += Player_OnBoundsV;

            IL.Celeste.FakeWall.Update += State_DreamDashNotEqual;
            IL.Celeste.Spring.OnCollide += State_DreamDashEqual;
            IL.Celeste.Solid.Update += State_DreamDashNotEqual;
        }

        public static void Unload() {
            On.Celeste.Player.ctor -= Player_ctor;
            On.Celeste.Player.DashBegin -= Player_DashBegin;
            IL.Celeste.Player.OnCollideH -= Player_OnCollideH;
            IL.Celeste.Player.OnCollideV -= Player_OnCollideV;
            On.Celeste.Player.DreamDashCheck -= Player_DreamDashCheck;
            On.Celeste.Player.Update -= Player_Update;
            On.Celeste.Player.Die -= Player_Die;

            hook_Player_DashCoroutine.Dispose();
            IL.Celeste.Player.IsRiding_Solid -= State_DreamDashEqual;
            IL.Celeste.Player.IsRiding_JumpThru -= State_DreamDashEqual;
            IL.Celeste.Player.OnCollideH -= State_DreamDashEqual;
            IL.Celeste.Player.OnCollideV -= State_DreamDashEqual;
            hook_Player_orig_Update.Dispose();
            hook_Player_orig_UpdateSprite.Dispose();

            On.Celeste.Level.EnforceBounds -= Level_EnforceBounds;
            On.Celeste.Player.OnBoundsH -= Player_OnBoundsH;
            On.Celeste.Player.OnBoundsV -= Player_OnBoundsV;

            IL.Celeste.FakeWall.Update -= State_DreamDashNotEqual;
            IL.Celeste.Spring.OnCollide -= State_DreamDashEqual;
            IL.Celeste.Solid.Update -= State_DreamDashNotEqual;
        }

        public static void LoadContent() {
            DreamTrailColors = new Color[]{
                Calc.HexToColor("FFEF11"),
                Calc.HexToColor("08A310"),
                Calc.HexToColor("FF00D0"),
                Calc.HexToColor("5FCDE4"),
                Calc.HexToColor("E0564C")
            };
        }

        #region Hooks

        // Adds custom dream tunnel dash state
        private static void Player_ctor(On.Celeste.Player.orig_ctor orig, Player player, Vector2 position, PlayerSpriteMode spriteMode) {
            orig(player, position, spriteMode);
            HasDreamTunnelDash = dreamTunnelDashAttacking = false;
            StDreamTunnelDash = player.StateMachine.AddState(player.DreamTunnelDashUpdate, null, player.DreamTunnelDashBegin, player.DreamTunnelDashEnd);
        }

        private static void Player_DashBegin(On.Celeste.Player.orig_DashBegin orig, Player self) {
            orig(self);            

            if (HasDreamTunnelDash) {
                dreamTunnelDashAttacking = true;
                dreamTunnelDashTimer = self.GetData().Get<float>("dashAttackTimer");

                // Ensures the player enters the dream tunnel dash state if dashing into a fast moving block
                // Because of how it works, it removes dashdir leniency :(
                DynData<Player> playerData = self.GetData();
                Vector2 lastAim = playerData.Get<Vector2>("lastAim");
                Vector2 dir = lastAim.Sign();
                if (!self.CollideCheck<Solid, DreamBlock>() && self.CollideCheck<Solid, DreamBlock>(self.Position + dir)) {
                    self.Speed = self.DashDir = lastAim;
                    self.MoveHExact((int) dir.X, playerData.Get<Collision>("onCollideH"));
                    self.MoveVExact((int) dir.Y, playerData.Get<Collision>("onCollideV"));
                }
            }
            HasDreamTunnelDash = false;
        }

        // DreamTunnelDash trail recoloring
        private static void Player_CreateTrail(On.Celeste.Player.orig_CreateTrail orig, Player player) {
            if (dreamTunnelDashAttacking) {
                player.CreateDreamTrail();
            } else {
                orig(player);
            }
        }

        private static void Player_DashCoroutine(ILContext il) {
            /*
             * adds a check for !HasDreamTunnelDash to
             * if (player.onGround && player.DashDir.X != 0f && player.DashDir.Y > 0f && player.Speed.Y > 0f && 
             *  (!player.Inventory.DreamDash || !player.CollideCheck<DreamBlock>(player.Position + Vector2.UnitY)))
             */
            ILCursor cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After, instr => instr.MatchLdfld<Player>("onGround"));
            cursor.Emit(cursor.Next.OpCode, cursor.Next.Operand);
            cursor.Emit(OpCodes.Ldsfld, typeof(DreamTunnelDash).GetField("dreamTunnelDashAttacking", BindingFlags.NonPublic | BindingFlags.Static));
            cursor.Next.OpCode = OpCodes.Brtrue;
        }

        private static void Player_Update(On.Celeste.Player.orig_Update orig, Player self) {
            orig(self);

            float dashAttackTimer = self.GetData().Get<float>("dashAttackTimer");
            if (dashAttackTimer < dreamTunnelDashTimer)
                dreamTunnelDashTimer = dashAttackTimer;
            else if (dreamTunnelDashTimer > 0)
                dreamTunnelDashTimer -= Engine.DeltaTime;

            if (dreamTunnelDashTimer <= 0f)
                dreamTunnelDashAttacking = false;

            if ((HasDreamTunnelDash) && self.Scene.OnInterval(0.1f))
                self.CreateDreamTrail();
        }

        private static void Player_OnCollideH(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            while (cursor.TryGotoNext(instr => instr.MatchCallvirt<Player>("DreamDashCheck"))) {
                Instruction idx = cursor.Next;
                cursor.GotoPrev(instr => instr.MatchCall<Vector2>("get_UnitX"));

                cursor.Emit(OpCodes.Dup);
                cursor.EmitDelegate<Func<Player, bool>>(player => player.DreamTunnelDashCheck(Vector2.UnitX * Math.Sign(player.Speed.X)));
                cursor.Emit(OpCodes.Brfalse_S, cursor.Next);
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ret);

                // No infinite loops please
                cursor.Goto(idx, MoveType.After);
            }
        }

        private static void Player_OnCollideV(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            while (cursor.TryGotoNext(instr => instr.MatchCallvirt<Player>("DreamDashCheck"))) {
                Instruction idx = cursor.Next;
                cursor.GotoPrev(instr => instr.MatchCall<Vector2>("get_UnitY"));

                cursor.Emit(OpCodes.Dup);
                cursor.EmitDelegate<Func<Player, bool>>(player => player.DreamTunnelDashCheck(Vector2.UnitY * Math.Sign(player.Speed.Y)));
                cursor.Emit(OpCodes.Brfalse_S, cursor.Next);
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ret);

                // No infinite loops please
                cursor.Goto(idx, MoveType.After);
            }
        }

        private static bool Player_DreamDashCheck(On.Celeste.Player.orig_DreamDashCheck orig, Player self, Vector2 dir) {
            if (overrideDreamDashCheck)
                return overrideDreamDashCheck = false;
            return orig(self, dir);
        }

        // Fixes bug with dreamSfx soundsource not being stopped
        private static PlayerDeadBody Player_Die(On.Celeste.Player.orig_Die orig, Player self, Vector2 dir, bool evenIfInvincible, bool registerDeathInStats) {
            SoundSource dreamSfxLoop = self.GetData().Get<SoundSource>("dreamSfxLoop");
            if (dreamSfxLoop != null) {
                dreamSfxLoop.Stop();
            }

            return orig(self, dir, evenIfInvincible, registerDeathInStats);
        }

        // Patch any method that checks the player's State
        private static readonly ILContext.Manipulator State_DreamDashEqual = il => Check_State_DreamDash(new ILCursor(il), true);
        private static readonly ILContext.Manipulator State_DreamDashNotEqual = il => Check_State_DreamDash(new ILCursor(il), true);
        private static void Check_State_DreamDash(ILCursor cursor, bool equal) {
            if (cursor.TryGotoNext(instr => instr.MatchLdcI4(Player.StDreamDash))) {
                // Duplicate the Player State
                cursor.Emit(OpCodes.Dup);
                // Check whether the state matches StDreamTunnelDash AND we want them to match
                cursor.EmitDelegate<Func<int, bool>>(st => (st == StDreamTunnelDash) == equal);
                // If not, skip the rest of the emitted instructions
                cursor.Emit(OpCodes.Brfalse_S, cursor.Next);

                // Else
                // Duplicated Player State value will be unused, so it must be trashed
                cursor.Emit(OpCodes.Pop);

                // Retrieve the next break instruction that checks equality
                Instruction breakInstr = cursor.Clone().GotoNext(instr => instr.Match(OpCodes.Beq_S) || instr.Match(OpCodes.Bne_Un_S) || instr.Match(OpCodes.Ceq)).Next;

                // For SteamFNA, if there is a check for equality just break to after it after pushing true to the stack
                if (breakInstr.OpCode == OpCodes.Ceq) {
                    cursor.Emit(OpCodes.Ldc_I4_1);
                    cursor.Emit(OpCodes.Br_S, breakInstr.Next);
                }
                // If our intended behaviour matches what the break instruction is checking for, break to its target
                else if ((breakInstr.OpCode == OpCodes.Beq_S) && equal)
                    cursor.Emit(OpCodes.Br_S, breakInstr.Operand);
                // Otherwise, break to after the break instruction (skip it)
                else
                    cursor.Emit(OpCodes.Br_S, breakInstr.Next);
            }
        }

        private static void Player_orig_Update(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            Check_State_DreamDash(cursor, true);
            Check_State_DreamDash(cursor, false);
            Check_State_DreamDash(cursor, false);
            Check_State_DreamDash(cursor, false);
        }

        private static void Level_EnforceBounds(On.Celeste.Level.orig_EnforceBounds orig, Level self, Player player) {
            if (new DynData<Level>(self).Get<Coroutine>("transition") != null)
                return;

            if (player.StateMachine.State == StDreamTunnelDash) {
                Rectangle bounds = self.Bounds;
                if (player.Right > bounds.Right || player.Left < bounds.Left || player.Top < bounds.Top || player.Bottom > bounds.Bottom) {
                    player.DreamDashDie();
                    return;
                }
                // Continue here, since it may be caught be player.OnBoundsH/OnBoundsV
            }

            orig(self, player);
        }

        private static void Player_OnBoundsH(On.Celeste.Player.orig_OnBoundsH orig, Player self) {
            if (self.StateMachine.State == StDreamTunnelDash) {
                self.DreamDashDie();
                return;
            }

            orig(self);
        }

        private static void Player_OnBoundsV(On.Celeste.Player.orig_OnBoundsV orig, Player self) {
            if (self.StateMachine.State == StDreamTunnelDash) {
                self.DreamDashDie();
                return;
            }

            orig(self);
        }

        #endregion

        #region Extensions

        public static void CreateTrail(this Player player, Color color) {
            Vector2 scale = new Vector2(Math.Abs(player.Sprite.Scale.X) * (float) player.Facing, player.Sprite.Scale.Y);
            TrailManager.Add(player, scale, color);
        }

        public static void CreateDreamTrail(this Player player) {
            CreateTrail(player, DreamTrailColors[DreamTrailColorIndex]);
            ++DreamTrailColorIndex;
            DreamTrailColorIndex %= 5;
        }

        public static bool DreamDashDie(this Player player, bool evenIfInvincible = false) {
            if (!evenIfInvincible && SaveData.Instance.Assists.Invincible) {
                player.Speed *= -1f;
                player.Play(SFX.game_assist_dreamblockbounce, null, 0f);
                return false;
            }

            player.Die(Vector2.Zero, evenIfInvincible, true);
            return true;
        }

        private static bool DreamTunneledIntoDeath(this Player player) {
            if (player.CollideCheck<DreamBlock>()) {
                for (int i = 1; i <= 5; i++) {
                    for (int j = -1; j <= 1; j += 2) {
                        for (int k = 1; k <= 5; k++) {
                            for (int l = -1; l <= 1; l += 2) {
                                Vector2 value = new Vector2(i * j, k * l);
                                if (!player.CollideCheck<DreamBlock>(player.Position + value)) {
                                    player.Position += value;
                                    return false;
                                }
                            }
                        }
                    }
                }
                return true;
            }
            return false;
        }

        #region DreamTunnelState

        private static bool DreamTunnelDashCheck(this Player player, Vector2 dir) {
            if (dreamTunnelDashAttacking && player.DashAttacking && (dir.X == Math.Sign(player.DashDir.X) || dir.Y == Math.Sign(player.DashDir.Y))) {
                Solid solid = null;

                // Check for dream blocks first, then for solids
                DreamBlock block = player.CollideFirst<DreamBlock>(player.Position + dir);
                if (block != null) {
                    Vector2 side = new Vector2(Math.Abs(dir.Y), Math.Abs(dir.X));

                    bool dashedIntoDreamBlock = true;
                    bool checkNegative = dir.X != 0f ? player.Speed.Y <= 0f : player.Speed.X <= 0f;
                    bool checkPositive = dir.X != 0f ? player.Speed.Y >= 0f : player.Speed.X >= 0f;
                    if (checkNegative) {
                        for (int i = -1; i >= -Player_DashCornerCorrection; i--) {
                            Vector2 at = player.Position + dir + side * i;
                            if (!player.CollideCheck<DreamBlock>(at) && (solid = player.CollideFirst<Solid, DreamBlock>(at)) != null) {
                                player.Position += side * i;
                                dashedIntoDreamBlock = false;
                                goto CheckDreamBlock;
                            }
                        }
                    }

                    if (checkPositive) {
                        for (int i = 1; i <= Player_DashCornerCorrection; i++) {
                            Vector2 at = player.Position + dir + side * i;
                            if (!player.CollideCheck<DreamBlock>(at) && (solid = player.CollideFirst<Solid, DreamBlock>(at)) != null) {
                                player.Position += side * i;
                                dashedIntoDreamBlock = false;
                                goto CheckDreamBlock;
                            }
                        }
                    }

                    CheckDreamBlock:
                    if (dashedIntoDreamBlock) {
                        if (new DynData<DreamBlock>(block).Get<bool>("playerHasDreamDash"))
                            player.Die(-dir);

                        dreamTunnelDashAttacking = false;
                        overrideDreamDashCheck = true;
                        return false;
                    }
                }

                solid = solid ?? player.CollideFirst<Solid, DreamBlock>(player.Position + dir);
                if (solid != null) {
                    DynData<Player> playerData = player.GetData();
                    player.StateMachine.State = StDreamTunnelDash;
                    playerData[Player_solid] = solid;
                    playerData["dashAttackTimer"] = 0;
                    playerData["gliderBoostTimer"] = 0;
                    return true;
                }
            }
            return false;
        }

        private static void DreamTunnelDashBegin(this Player player) {
            DynData<Player> playerData = player.GetData();

            if (playerData["dreamSfxLoop"] == null) {
                SoundSource dreamSfxLoop = new SoundSource();
                player.Add(dreamSfxLoop);
                playerData["dreamSfxLoop"] = dreamSfxLoop;
            }

            // Extra correction for fast moving solids, this does not cause issues with dashdir leniency
            Vector2 dir = player.DashDir.Sign();
            if (!player.CollideCheck<Solid, DreamBlock>() && player.CollideCheck<Solid, DreamBlock>(player.Position + dir)) {
                player.NaiveMove(dir);
            }

            player.Speed = player.DashDir * Player_DashSpeed;
            player.TreatNaive = true;
            player.Depth = Depths.PlayerDreamDashing;
            playerData[Player_dreamTunnelDashCanEndTimer] = 0.1f;
            player.Stamina = Player_ClimbMaxStamina;
            playerData["dreamJump"] = false;
            player.Play(SFX.char_mad_dreamblock_enter, null, 0f);
            player.Loop(playerData.Get<SoundSource>("dreamSfxLoop"), SFX.char_mad_dreamblock_travel);
        }

        private static void DreamTunnelDashEnd(this Player player) {
            DynData<Player> playerData = player.GetData();

            player.Depth = Depths.Player;
            if (!playerData.Get<bool>("dreamJump")) {
                player.AutoJump = true;
                player.AutoJumpTimer = 0f;
            }
            if (!player.Inventory.NoRefills) {
                player.RefillDash();
            }
            player.RefillStamina();
            player.TreatNaive = false;
            if (playerData[Player_solid] != null) {
                if (player.DashDir.X != 0f) {
                    playerData["jumpGraceTimer"] = 0.1f;
                    playerData["dreamJump"] = true;
                } else {
                    playerData["jumpGraceTimer"] = 0f;
                }
                //player.dreamBlock.OnPlayerExit(player);
                playerData[Player_solid] = null;
            }
            player.Stop(playerData.Get<SoundSource>("dreamSfxLoop"));
            player.Play(SFX.char_mad_dreamblock_exit, null, 0f);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
        }

        private static int DreamTunnelDashUpdate(this Player player) {
            DynData<Player> playerData = player.GetData();

            Input.Rumble(RumbleStrength.Light, RumbleLength.Medium);
            Vector2 position = player.Position;
            player.NaiveMove(player.Speed * Engine.DeltaTime);
            float dreamDashCanEndTimer = playerData.Get<float>(Player_dreamTunnelDashCanEndTimer);
            if (dreamDashCanEndTimer > 0f) {
                playerData[Player_dreamTunnelDashCanEndTimer] = dreamDashCanEndTimer - Engine.DeltaTime;
            }
            Solid solid = player.CollideFirst<Solid, DreamBlock>();
            if (solid == null) {
                if (player.DreamTunneledIntoDeath()) {
                    player.DreamDashDie();
                } else if (playerData.Get<float>(Player_dreamTunnelDashCanEndTimer) <= 0f) {
                    Celeste.Freeze(0.05f);
                    if (Input.Jump.Pressed && player.DashDir.X != 0f) {
                        playerData["dreamJump"] = true;
                        player.Jump(true, true);
                    } else if (player.DashDir.Y >= 0f || player.DashDir.X != 0f) {
                        if (player.DashDir.X > 0f && player.CollideCheck<DreamBlock>(player.Position - Vector2.UnitX * 5f)) {
                            player.MoveHExact(-5, null, null);
                        } else if (player.DashDir.X < 0f && player.CollideCheck<DreamBlock>(player.Position + Vector2.UnitX * 5f)) {
                            player.MoveHExact(5, null, null);
                        }
                        bool flag = player.ClimbCheck(-1, 0);
                        bool flag2 = player.ClimbCheck(1, 0);
                        int moveX = playerData.Get<int>("moveX");
                        if (Input.Grab.Check && ((moveX == 1 && flag2) || (moveX == -1 && flag))) {
                            player.Facing = (Facings) moveX;
                            if (!SaveData.Instance.Assists.NoGrabbing) {
                                return 1;
                            }
                            player.ClimbTrigger(moveX);
                            player.Speed.X = 0f;
                        }
                    }
                    return 0;
                }
            } else {
                playerData[Player_solid] = solid;
                if (player.Scene.OnInterval(0.1f)) {
                    player.CreateDreamTrail();
                }
                Level level = playerData.Get<Level>("level");
                if (level.OnInterval(0.04f)) {
                    DisplacementRenderer.Burst burst = level.Displacement.AddBurst(player.Center, 0.3f, 0f, 40f, 1f, null, null);
                    burst.WorldClipCollider = solid.Collider;
                    burst.WorldClipPadding = 2;
                }
            }
            return StDreamTunnelDash;
        }

        #endregion

        #endregion

    }
}