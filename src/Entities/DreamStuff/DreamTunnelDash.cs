using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.RuntimeDetour;
using System;
using System.Collections;

namespace Celeste.Mod.CommunalHelper.Entities {
    public static class DreamTunnelDash {

        #region Constants

        internal const float Player_DashSpeed = 240f;
        internal const float Player_ClimbMaxStamina = 110f;
        internal const float Player_DreamDashMinTime = 0.1f;

        #endregion

        public static int StDreamTunnelDash;
        private static bool hasDreamTunnelDash = false;
        public static bool HasDreamTunnelDash {
            get { return hasDreamTunnelDash || CommunalHelperModule.Settings.AlwaysActiveDreamRefillCharge; }
            set { hasDreamTunnelDash = value; }
        }
        public static bool dreamTunnelDashAttacking = false;

        public static Color[] DreamTrailColors;
        public static int DreamTrailColorIndex = 0;

        private static IDetour hook_Player_get_LoseShards;

        public static void Load() {
            On.Celeste.Player.ctor += Player_ctor;
            On.Celeste.Player.DashBegin += Player_DashBegin;
            On.Celeste.Player.DashCoroutine += Player_DashCoroutine;
            On.Celeste.Player.Update += Player_Update;
            On.Celeste.Player.CreateTrail += Player_CreateTrail;
            On.Celeste.Player.OnCollideH += Player_OnCollideH;
            On.Celeste.Player.OnCollideV += Player_OnCollideV;
            On.Celeste.Player.Die += Player_Die;
            On.Celeste.Player.UpdateSprite += Player_UpdateSprite;
            On.Celeste.Player.IsRiding_Solid += Player_IsRiding_Solid;
            On.Celeste.Player.SceneEnd += Player_SceneEnd;
            hook_Player_get_LoseShards = new Hook(typeof(Player).GetProperty("LoseShards").GetGetMethod(),
                typeof(DreamTunnelDash).GetMethod("Player_get_LoseShards", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static));

            On.Celeste.Level.EnforceBounds += Level_EnforceBounds;
        }

        public static void Unload() {
            On.Celeste.Player.ctor -= Player_ctor;
            On.Celeste.Player.DashBegin -= Player_DashBegin;
            On.Celeste.Player.DashCoroutine -= Player_DashCoroutine;
            On.Celeste.Player.Update -= Player_Update;
            On.Celeste.Player.CreateTrail -= Player_CreateTrail;
            On.Celeste.Player.OnCollideH -= Player_OnCollideH;
            On.Celeste.Player.OnCollideV -= Player_OnCollideV;
            On.Celeste.Player.Die -= Player_Die;
            On.Celeste.Player.UpdateSprite -= Player_UpdateSprite;
            On.Celeste.Player.IsRiding_Solid -= Player_IsRiding_Solid;
            On.Celeste.Player.SceneEnd -= Player_SceneEnd;
            hook_Player_get_LoseShards.Dispose();

            On.Celeste.Level.EnforceBounds -= Level_EnforceBounds;
        }

        public static void LoadContent() {
            DreamTrailColors = new Color[5];
            DreamTrailColors[0] = Calc.HexToColor("FFEF11");
            DreamTrailColors[1] = Calc.HexToColor("08A310");
            DreamTrailColors[2] = Calc.HexToColor("FF00D0");
            DreamTrailColors[3] = Calc.HexToColor("5FCDE4");
            DreamTrailColors[4] = Calc.HexToColor("E0564C");
        }

        #region Hooks

        // Adds custom dream tunnel dash state
        private static void Player_ctor(On.Celeste.Player.orig_ctor orig, Player player, Vector2 position, PlayerSpriteMode spriteMode) {
            orig(player, position, spriteMode);
            StDreamTunnelDash = player.StateMachine.AddState(player.DreamTunnelDashUpdate, null, player.DreamTunnelDashBegin, player.DreamTunnelDashEnd);
        }

        // Dream tunnel dash triggering
        private static void Player_DashBegin(On.Celeste.Player.orig_DashBegin orig, Player player) {
            orig(player);
            if (HasDreamTunnelDash) {
                dreamTunnelDashAttacking = true;
                HasDreamTunnelDash = false;

                // Ensures the player enters the dream tunnel dash state if dashing into a fast moving block
                var playerData = player.GetData();
                Vector2 lastAim = playerData.Get<Vector2>("lastAim");
                int dirX = Math.Sign(lastAim.X);
                int dirY = Math.Sign(lastAim.Y);
                Vector2 dir = new Vector2(dirX, dirY);
                if (player.CollideCheck<Solid, DreamBlock>(player.Position + dir)) {
                    player.Speed = player.DashDir = dir;
                    player.MoveHExact(dirX, playerData.Get<Collision>("onCollideH"));
                    player.MoveVExact(dirY, playerData.Get<Collision>("onCollideV"));
                }
            }
        }

        // Allows downwards diagonal dream tunnel dashing when on the ground 
        private static IEnumerator Player_DashCoroutine(On.Celeste.Player.orig_DashCoroutine orig, Player player) {
            IEnumerator origEnum = orig(player);
            origEnum.MoveNext();
            yield return origEnum.Current;

            bool forceDownwardDiagonalDash = false;
            Vector2 origDashDir = Input.GetAimVector(player.Facing);
            if (player.OnGround() && origDashDir.X != 0f && origDashDir.Y > 0f && dreamTunnelDashAttacking) {
                forceDownwardDiagonalDash = true;
            }
            origEnum.MoveNext();
            if (forceDownwardDiagonalDash) {
                player.DashDir = origDashDir;
                player.Speed = origDashDir * Player_DashSpeed;
                if (player.CanUnDuck) {
                    player.Ducking = false;
                }
            }
            yield return origEnum.Current;

            origEnum.MoveNext();
        }

        // Dream trail creation and dreamTunnelDashAttacking updating
        private static void Player_Update(On.Celeste.Player.orig_Update orig, Player player) {
            orig(player);

            Level level = player.Scene as Level;
            if (HasDreamTunnelDash && level.OnInterval(0.1f)) {
                player.CreateDreamTrail();
            }
            if (!player.DashAttacking) {
                dreamTunnelDashAttacking = false;
            }
        }

        // Dream tunnel dash trail recoloring
        private static void Player_CreateTrail(On.Celeste.Player.orig_CreateTrail orig, Player player) {
            if (dreamTunnelDashAttacking) {
                player.CreateTrail(DreamTrailColors[DreamTrailColorIndex]);
                ++DreamTrailColorIndex;
                DreamTrailColorIndex %= 5;
            } else {
                orig(player);
            }
        }

        // Dream tunnel dash/dashing into dream block detection 
        private static void Player_OnCollideH(On.Celeste.Player.orig_OnCollideH orig, Player player, CollisionData data) {
            if (player.StateMachine.State == StDreamTunnelDash) {
                return;
            }
            if (!player.DreamTunnelDashCheck(new Vector2(Math.Sign(player.Speed.X), 0))) {
                orig(player, data);
            }
        }
        private static void Player_OnCollideV(On.Celeste.Player.orig_OnCollideV orig, Player player, CollisionData data) {
            if (player.StateMachine.State == StDreamTunnelDash) {
                return;
            }

            if (!player.DreamTunnelDashCheck(new Vector2(0, Math.Sign(player.Speed.Y)))) {
                orig(player, data);
            }
        }

        // Kills the player if they dream tunnel dash into the level bounds
        private static void Level_EnforceBounds(On.Celeste.Level.orig_EnforceBounds orig, Level level, Player player) {
            Rectangle bounds = level.Bounds;
            bool canDie = player.StateMachine.State == StDreamTunnelDash && player.CollideCheck<Solid>();
            if (canDie && (player.Right > bounds.Right || player.Left < bounds.Left || player.Top < bounds.Top || player.Bottom > bounds.Bottom)) {
                player.Die(Vector2.Zero);
            } else {
                orig(level, player);
            }
        }

        // Fixes bug with dreamSfx soundsource not being stopped
        private static PlayerDeadBody Player_Die(On.Celeste.Player.orig_Die orig, Player player, Vector2 dir, bool evenIfInvincible = false, bool registerDeathInStats = true) {
            HasDreamTunnelDash = false;
            SoundSource dreamSfxLoop = player.GetData().Get<SoundSource>("dreamSfxLoop");
            if (dreamSfxLoop != null) {
                dreamSfxLoop.Stop();
            }
            return orig(player, dir, evenIfInvincible, registerDeathInStats);
        }

        // Updates sprite for dream tunnel dash state
        private static void Player_UpdateSprite(On.Celeste.Player.orig_UpdateSprite orig, Player player) {
            if (StDreamTunnelDash != 0 && player.StateMachine.State == StDreamTunnelDash) {
                if (player.Sprite.CurrentAnimationID != "dreamDashIn" && player.Sprite.CurrentAnimationID != "dreamDashLoop") {
                    player.Sprite.Play("dreamDashIn");
                }
            } else {
                orig(player);
            }
        }

        // Ensures that the player is transported by moving solids when dream tunnel dashing through them
        private static bool Player_IsRiding_Solid(On.Celeste.Player.orig_IsRiding_Solid orig, Player player, Solid solid) {
            if (player.StateMachine.State == StDreamTunnelDash) {
                return player.CollideCheck(solid);
            }
            return orig(player, solid);
        }

        private static void Player_SceneEnd(On.Celeste.Player.orig_SceneEnd orig, Player player, Scene scene) {
            orig(player, scene);
            HasDreamTunnelDash = false;
        }

        // Mostly so that StrawberrySeeds don't get reset;
        private delegate bool Player_orig_get_LoseShards(Player self);
        private static bool Player_get_LoseShards(Player_orig_get_LoseShards orig, Player self) {
            return orig(self) && !(self.StateMachine.State == StDreamTunnelDash || self.GetData().Get<bool>("dreamJump"));
        }

        #endregion

        #region Player Extensions

        public static void CreateTrail(this Player player, Color color) {
            Vector2 scale = new Vector2(Math.Abs(player.Sprite.Scale.X) * (float) player.Facing, player.Sprite.Scale.Y);
            TrailManager.Add(player, scale, color);
        }

        public static void CreateDreamTrail(this Player player) {
            CreateTrail(player, DreamTrailColors[DreamTrailColorIndex]);
            ++DreamTrailColorIndex;
            DreamTrailColorIndex %= 5;
        }

        internal static bool DreamTunnelDashCheck(this Player player, Vector2 direction) {
            if ((dreamTunnelDashAttacking || player.DashAttacking && HasDreamTunnelDash) && Vector2.Dot(player.DashDir, direction) > 0f) {
                Vector2 perpendicular = new Vector2(Math.Abs(direction.Y), Math.Abs(direction.X));

                // Dream block checking
                if (player.CollideCheck<DreamBlock>(player.Position + direction)) {
                    bool decrease = direction.X != 0f ? player.Speed.Y <= 0f : player.Speed.X <= 0f;
                    bool increase = direction.X != 0f ? player.Speed.Y >= 0f : player.Speed.X >= 0f;
                    bool dashedIntoDreamBlock = true;
                    for (int dist = 1; dist <= 4 && dashedIntoDreamBlock; dist++) {
                        for (int dir = -1; dir <= 1; dir += 2) {
                            if (dir == 1 ? increase : decrease) {
                                Vector2 offset = dir * dist * perpendicular;
                                if (!player.CollideCheck<DreamBlock>(player.Position + direction + offset)) {
                                    player.Position += offset;
                                    dashedIntoDreamBlock = false;
                                    break;
                                }
                            }
                        }
                    }
                    if (dashedIntoDreamBlock) {
                        player.Die(-direction);
                        return true;
                    }
                }

                // Solid checking
                if (player.CollideCheck<Solid, DreamBlock>(player.Position + direction)) {
                    bool decrease = direction.X != 0f ? player.Speed.Y <= 0f : player.Speed.X <= 0f;
                    bool increase = direction.X != 0f ? player.Speed.Y >= 0f : player.Speed.X >= 0f;
                    for (int dist = 1; dist <= 4; dist++) {
                        for (int dir = -1; dir <= 1; dir += 2) {
                            if (dir == 1 ? increase : decrease) {
                                Vector2 offset = dir * dist * perpendicular;
                                if (!player.CollideCheck<Solid, DreamBlock>(player.Position + direction + offset)) {
                                    // Actual position offset handled by orig collide
                                    return false;
                                }
                            }
                        }
                    }
                    player.StateMachine.State = StDreamTunnelDash;
                    dreamTunnelDashAttacking = false;

                    var playerData = player.GetData();
                    playerData["dashAttackTimer"] = 0f;
                    playerData["gliderBoostTimer"] = 0f;
                    return true;
                }
            }
            return false;
        }

        internal static void DreamTunnelDashBegin(this Player player) {
            var playerData = player.GetData();

            SoundSource dreamSfxLoop = playerData.Get<SoundSource>("dreamSfxLoop");
            if (dreamSfxLoop == null) {
                playerData["dreamSfxLoop"] = dreamSfxLoop = new SoundSource();
                player.Add(dreamSfxLoop);
            }
            player.Speed = player.DashDir * Player_DashSpeed;
            player.TreatNaive = true;

            // Puts the player inside a fast moving solid to ensure they are carried with it
            player.Position.X += Math.Sign(player.DashDir.X);
            player.Position.Y += Math.Sign(player.DashDir.Y);

            player.Depth = Depths.PlayerDreamDashing;
            player.Stamina = Player_ClimbMaxStamina;
            playerData["dreamDashCanEndTimer"] = 0.07f; // jank fix, should be 0.1f
            playerData["dreamJump"] = false;

            player.Play(SFX.char_bad_dreamblock_enter);
            player.Loop(dreamSfxLoop, SFX.char_mad_dreamblock_travel);
        }

        internal static void DreamTunnelDashEnd(this Player player) {
            var playerData = player.GetData();

            player.Depth = 0;
            if (!playerData.Get<bool>("dreamJump")) {
                player.AutoJump = true;
                player.AutoJumpTimer = 0f;
            }
            if (!player.Inventory.NoRefills) {
                player.RefillDash();
            }
            player.RefillStamina();
            player.TreatNaive = false;

            if (player.DashDir.X != 0f) {
                playerData["jumpGraceTimer"] = 0.1f;
                playerData["dreamJump"] = true;
            } else {
                playerData["jumpGraceTimer"] = 0f;
            }

            Dust.Burst(player.Position, player.Speed.Angle(), 16, null);
            player.Stop(playerData.Get<SoundSource>("dreamSfxLoop"));
            player.Play(SFX.char_mad_dreamblock_exit);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
        }

        internal static int DreamTunnelDashUpdate(this Player player) {
            var playerData = player.GetData();

            Input.Rumble(RumbleStrength.Light, RumbleLength.Medium);
            Vector2 position = player.Position;
            player.NaiveMove(player.Speed * Engine.DeltaTime);

            float dreamDashCanEndTimer = playerData.Get<float>("dreamDashCanEndTimer");
            if (dreamDashCanEndTimer > 0f) {
                dreamDashCanEndTimer -= Engine.DeltaTime;
                playerData["dreamDashCanEndTimer"] = dreamDashCanEndTimer;
            }
            if (player.CollideCheck<Solid, DreamBlock>()) {
                if (player.Scene.OnInterval(0.1f)) {
                    CreateDreamTrail(player);
                }

                Level level = playerData.Get<Level>("level");
                if (level.OnInterval(0.04f)) {
                    level.Displacement.AddBurst(player.Center, 0.3f, 0f, 40f);
                }
            } else {
                if (DreamTunneledIntoDeath(player)) {
                    if (SaveData.Instance.Assists.Invincible) {
                        player.Position = position;
                        player.Speed *= -1f;
                        player.Play(SFX.game_assist_dreamblockbounce);
                    } else {
                        player.Die(Vector2.Zero);
                    }
                } else if (dreamDashCanEndTimer <= 0f) {
                    Celeste.Freeze(0.05f);
                    if (Input.Jump.Pressed && player.DashDir.X != 0f) {
                        playerData["dreamJump"] = true;
                        player.Jump();
                    } else if (player.DashDir.Y >= 0f || player.DashDir.X != 0f) {
                        if (player.DashDir.X > 0f && player.CollideCheck<DreamBlock>(player.Position - Vector2.UnitX * 5f)) {
                            player.MoveHExact(-5);
                        } else if (player.DashDir.X < 0f && player.CollideCheck<DreamBlock>(player.Position + Vector2.UnitX * 5f)) {
                            player.MoveHExact(5);
                        }

                        int moveX = playerData.Get<int>("moveX");
                        if (Input.Grab.Check && player.ClimbCheck(moveX)) {
                            player.Facing = (Facings) moveX;
                            if (!SaveData.Instance.Assists.NoGrabbing) {
                                return Player.StClimb;
                            }
                            player.ClimbTrigger(moveX);
                            player.Speed.X = 0f;
                        }
                    }
                    return Player.StNormal;
                }
            }
            return StDreamTunnelDash;
        }

        private static bool DreamTunneledIntoDeath(Player player) {
            if (player.CollideCheck<DreamBlock>()) {
                for (int i = 1; i <= 5; i++) {
                    for (int j = -1; j <= 1; j += 2) {
                        for (int k = 1; k <= 5; k++) {
                            for (int l = -1; l <= 1; l += 2) {
                                Vector2 vector = new Vector2(i * j, k * l);
                                if (!player.CollideCheck<DreamBlock>(player.Position + vector)) {
                                    player.Position += vector;
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

        #endregion

    }
}
