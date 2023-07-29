using Celeste.Mod.CommunalHelper.Components;
using Celeste.Mod.CommunalHelper.Imports;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.DashStates;

public static class DreamTunnelDash
{
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

    public static int StDreamTunnelDash = -1;
    private static bool hasDreamTunnelDash;
    public static bool HasDreamTunnelDash
    {
        get => hasDreamTunnelDash || CommunalHelperModule.Settings.AlwaysActiveDreamRefillCharge;
        set => hasDreamTunnelDash = value;
    }
    private static bool dreamTunnelDashAttacking;
    private static float dreamTunnelDashTimer;

    private static bool nextDashFeather;
    public static bool NextDashFeather
    {
        get => nextDashFeather || CommunalHelperModule.Settings.DreamDashFeatherMode;
        set => nextDashFeather = value;
    }
    public static bool FeatherMode { get; private set; }

    private static bool overrideDreamDashCheck;

    public static Color[] DreamTrailColors;
    public static int DreamTrailColorIndex = 0;


    private static IDetour hook_Player_DashCoroutine;
    private static IDetour hook_Player_orig_Update;
    private static IDetour hook_Player_orig_UpdateSprite;

    public static void Load()
    {
        On.Celeste.Player.ctor += Player_ctor;
        On.Celeste.Player.DashBegin += Player_DashBegin;
        On.Celeste.Player.CreateTrail += Player_CreateTrail;
        On.Celeste.Player.OnCollideH += Player_OnCollideH;
        On.Celeste.Player.OnCollideV += Player_OnCollideV;
        On.Celeste.Player.DreamDashCheck += Player_DreamDashCheck;
        On.Celeste.Player.Update += Player_Update;
        On.Celeste.Player.Die += Player_Die;
        hook_Player_DashCoroutine = new ILHook(
            typeof(Player).GetMethod("DashCoroutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget(),
            Player_DashCoroutine);

        IL.Celeste.Player.IsRiding_Solid += State_DreamDashEqual;
        IL.Celeste.Player.IsRiding_JumpThru += Player_IsRiding_JumpThru;
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
        IL.Celeste.Solid.Update += State_DreamDashNotEqual_And;
    }

    public static void Unload()
    {
        On.Celeste.Player.ctor -= Player_ctor;
        On.Celeste.Player.DashBegin -= Player_DashBegin;
        On.Celeste.Player.CreateTrail -= Player_CreateTrail;
        On.Celeste.Player.OnCollideH -= Player_OnCollideH;
        On.Celeste.Player.OnCollideV -= Player_OnCollideV;
        On.Celeste.Player.DreamDashCheck -= Player_DreamDashCheck;
        On.Celeste.Player.Update -= Player_Update;
        On.Celeste.Player.Die -= Player_Die;

        hook_Player_DashCoroutine.Dispose();
        IL.Celeste.Player.IsRiding_Solid -= State_DreamDashEqual;
        IL.Celeste.Player.IsRiding_JumpThru -= Player_IsRiding_JumpThru;
        IL.Celeste.Player.OnCollideH -= State_DreamDashEqual;
        IL.Celeste.Player.OnCollideV -= State_DreamDashEqual;
        hook_Player_orig_Update.Dispose();
        hook_Player_orig_UpdateSprite.Dispose();

        On.Celeste.Level.EnforceBounds -= Level_EnforceBounds;
        On.Celeste.Player.OnBoundsH -= Player_OnBoundsH;
        On.Celeste.Player.OnBoundsV -= Player_OnBoundsV;

        IL.Celeste.FakeWall.Update -= State_DreamDashNotEqual;
        IL.Celeste.Spring.OnCollide -= State_DreamDashEqual;
        IL.Celeste.Solid.Update -= State_DreamDashNotEqual_And;

        if (StDreamTunnelDash != -1)
            Extensions.UnregisterState(StDreamTunnelDash);
    }

    public static void InitializeParticles()
    {
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
    private static void Player_ctor(On.Celeste.Player.orig_ctor orig, Player player, Vector2 position, PlayerSpriteMode spriteMode)
    {
        orig(player, position, spriteMode);
        HasDreamTunnelDash = dreamTunnelDashAttacking = false;

        if (StDreamTunnelDash != -1)
            Extensions.UnregisterState(StDreamTunnelDash);
        StDreamTunnelDash = player.StateMachine.AddState(player.DreamTunnelDashUpdate, null, player.DreamTunnelDashBegin, player.DreamTunnelDashEnd);
        Extensions.RegisterState(StDreamTunnelDash, "StDreamTunnelDash");
    }

    private static void Player_DashBegin(On.Celeste.Player.orig_DashBegin orig, Player self)
    {
        orig(self);

        if (HasDreamTunnelDash)
        {
            dreamTunnelDashAttacking = true;
            dreamTunnelDashTimer = self.GetData().Get<float>("dashAttackTimer");

            // Ensures the player enters the dream tunnel dash state if dashing into a fast moving block
            // Because of how it works, it removes dashdir leniency :(
            DynamicData playerData = self.GetData();
            Vector2 lastAim = Input.GetAimVector(self.Facing);
            Vector2 dir = lastAim.Sign();
            if (!self.CollideCheck<Solid, DreamBlock>() && self.CollideCheck<Solid, DreamBlock>(self.Position + dir))
            {
                self.Speed = self.DashDir = lastAim;
                self.MoveHExact((int) dir.X, playerData.Get<Collision>("onCollideH"));
                self.MoveVExact((int) dir.Y, playerData.Get<Collision>("onCollideV"));
            }

            if (NextDashFeather)
            {
                FeatherMode = true;
                NextDashFeather = false;
            }
        }
        HasDreamTunnelDash = false;
    }

    // DreamTunnelDash trail recoloring
    private static void Player_CreateTrail(On.Celeste.Player.orig_CreateTrail orig, Player player)
    {
        if (dreamTunnelDashAttacking)
        {
            player.CreateDreamTrail();
        }
        else
        {
            orig(player);
        }
    }

    private static void Player_DashCoroutine(ILContext il)
    {
        /*
         * adds a check for !HasDreamTunnelDash to
         * if (player.onGround && player.DashDir.X != 0f && player.DashDir.Y > 0f && player.Speed.Y > 0f && 
         *  (!player.Inventory.DreamDash || !player.CollideCheck<DreamBlock>(player.Position + Vector2.UnitY)))
         */
        ILCursor cursor = new(il);
        cursor.GotoNext(MoveType.After, instr => instr.MatchLdfld<Player>("onGround"));
        cursor.Emit(cursor.Next.OpCode, cursor.Next.Operand);
        cursor.Emit(OpCodes.Ldsfld, typeof(DreamTunnelDash).GetField(nameof(dreamTunnelDashAttacking), BindingFlags.NonPublic | BindingFlags.Static));
        cursor.Next.OpCode = OpCodes.Brtrue;
    }

    private static void Player_Update(On.Celeste.Player.orig_Update orig, Player self)
    {
        orig(self);

        float dashAttackTimer = self.GetData().Get<float>("dashAttackTimer");
        if (dashAttackTimer < dreamTunnelDashTimer)
            dreamTunnelDashTimer = dashAttackTimer;
        else if (dreamTunnelDashTimer > 0)
            dreamTunnelDashTimer -= Engine.DeltaTime;

        if (dreamTunnelDashTimer <= 0f)
            dreamTunnelDashAttacking = false;

        if (HasDreamTunnelDash && self.Scene.OnInterval(0.1f))
            self.CreateDreamTrail();
    }

    // StDreamTunnelDash check handled in IL hook
    private static void Player_OnCollideH(On.Celeste.Player.orig_OnCollideH orig, Player self, CollisionData data)
    {
        if (!self.DreamTunnelDashCheck(Vector2.UnitX * Math.Sign(self.Speed.X)))
            orig(self, data);
    }

    // StDreamTunnelDash check handled in IL hook
    private static void Player_OnCollideV(On.Celeste.Player.orig_OnCollideV orig, Player self, CollisionData data)
    {
        if (!self.DreamTunnelDashCheck(Vector2.UnitY * Math.Sign(self.Speed.Y)))
            orig(self, data);
    }

    // Unused in favour of original behaviour
    /*
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
    */

    private static bool Player_DreamDashCheck(On.Celeste.Player.orig_DreamDashCheck orig, Player self, Vector2 dir)
    {
        return overrideDreamDashCheck ? (overrideDreamDashCheck = false) : orig(self, dir);
    }

    // Fixes bug with dreamSfx soundsource not being stopped
    private static PlayerDeadBody Player_Die(On.Celeste.Player.orig_Die orig, Player self, Vector2 dir, bool evenIfInvincible, bool registerDeathInStats)
    {
        SoundSource dreamSfxLoop = self.GetData().Get<SoundSource>("dreamSfxLoop");
        dreamSfxLoop?.Stop();

        return orig(self, dir, evenIfInvincible, registerDeathInStats);
    }

    private static void Player_IsRiding_JumpThru(ILContext il)
    {
        if (il.Instrs[0].OpCode == OpCodes.Nop)
            State_DreamDashEqual(il);
        else
            State_DreamDashNotEqual_And(il);
    }

    // Patch any method that checks the player's State
    /// <summary>
    /// Use if decompilation says <c>State==9</c> and NOT followed by <c>&amp;&amp;</c>.
    /// </summary>
    private static readonly ILContext.Manipulator State_DreamDashEqual = il => Check_State_DreamDash(new ILCursor(il), true);
    /// <summary>
    /// Use if decompilation says <c>State!=9</c> and NOT followed by <c>&amp;&amp;</c>.
    /// </summary>
    private static readonly ILContext.Manipulator State_DreamDashNotEqual = il => Check_State_DreamDash(new ILCursor(il), false);
    /// <summary>
    /// Use if decompilation says <c>State==9</c> and IS followed by <c>&amp;&amp;</c>.
    /// </summary>
    private static readonly ILContext.Manipulator State_DreamDashEqual_And = il => Check_State_DreamDash(new ILCursor(il), true, true);
    /// <summary>
    /// Use if decompilation says <c>State!=9</c> and IS followed by <c>&amp;&amp;</c>.
    /// </summary>
    private static readonly ILContext.Manipulator State_DreamDashNotEqual_And = il => Check_State_DreamDash(new ILCursor(il), false, true);
    /// <summary>
    /// Patch any method that checks the player's state.
    /// </summary>
    /// <remarks>Checks for <c>ldc.i4.s 9</c></remarks>
    /// <param name="cursor"></param>
    /// <param name="equal">Whether the decompilation says State == 9</param>
    /// <param name="and">Whether the check is followed by <c>&amp;&amp;</c></param>
    private static void Check_State_DreamDash(ILCursor cursor, bool equal, bool and = false)
    {
        if (cursor.TryGotoNext(instr => instr.MatchLdcI4(Player.StDreamDash) &&
            instr.Previous != null && instr.Previous.MatchCallvirt<StateMachine>("get_State")))
        {
            Instruction idx = cursor.Next;
            // Duplicate the Player State
            cursor.Emit(OpCodes.Dup);
            // Check whether the state matches StDreamTunnelDash AND we want them to match
            cursor.EmitDelegate<Func<int, bool>>(st => st == StDreamTunnelDash == equal ^ and);
            // If not, skip the rest of the emitted instructions
            cursor.Emit(OpCodes.Brfalse_S, cursor.Next);

            // Else
            // Duplicated Player State value will be unused, so it must be trashed
            cursor.Emit(OpCodes.Pop);

            // Retrieve the next break instruction that checks equality
            Instruction breakInstr = cursor.Clone().GotoNext(instr => instr.Match(OpCodes.Beq_S) || instr.Match(OpCodes.Bne_Un_S) || instr.Match(OpCodes.Ceq)).Next;

            // For SteamFNA, if there is a check for equality just break to after it after pushing the appropriate value to the stack
            if (breakInstr.OpCode == OpCodes.Ceq)
            {
                cursor.Emit(equal ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                cursor.Emit(OpCodes.Br_S, breakInstr.Next);
            }
            // If our intended behaviour matches what the break instruction is checking for, break to its target
            else if (breakInstr.OpCode == OpCodes.Beq_S == equal ^ and)
                cursor.Emit(OpCodes.Br_S, breakInstr.Operand);
            // Otherwise, break to after the break instruction (skip it)
            else
                cursor.Emit(OpCodes.Br_S, breakInstr.Next);

            cursor.Goto(idx, MoveType.After);
        }
    }

    private static void Player_orig_Update(ILContext il)
    {
        ILCursor cursor = new(il);
        Check_State_DreamDash(cursor, true);
        Check_State_DreamDash(cursor, false, true);
        Check_State_DreamDash(cursor, false, true);
        // Not used because we DO want to enforce Level bounds.
        //Check_State_DreamDash(cursor, false, true);
    }

    // Kill the player if they attempt to DreamTunnel out of the level
    private static void Level_EnforceBounds(On.Celeste.Level.orig_EnforceBounds orig, Level self, Player player)
    {
        if (DynamicData.For(self).Get<Coroutine>("transition") is not null)
            return;

        if (player.StateMachine.State == StDreamTunnelDash)
        {
            Rectangle bounds = self.Bounds;
            if (player.Right > bounds.Right || player.Left < bounds.Left || player.Top < bounds.Top || player.Bottom > bounds.Bottom)
            {
                player.DreamDashDie(player.Position);
                return;
            }
            // Continue here, since it may be caught be player.OnBoundsH/OnBoundsV
        }

        orig(self, player);
    }

    // Handles cases with locked camera
    private static void Player_OnBoundsH(On.Celeste.Player.orig_OnBoundsH orig, Player self)
    {
        if (self.StateMachine.State == StDreamTunnelDash)
        {
            self.DreamDashDie(self.Position);
            return;
        }

        orig(self);
    }

    // Handles cases with locked camera
    private static void Player_OnBoundsV(On.Celeste.Player.orig_OnBoundsV orig, Player self)
    {
        if (self.StateMachine.State == StDreamTunnelDash)
        {
            self.DreamDashDie(self.Position);
            return;
        }

        orig(self);
    }

    #endregion

    #region Extensions

    public static void CreateTrail(this Player player, Color color)
    {
        Vector2 scale = new(Math.Abs(player.Sprite.Scale.X) * (float) player.Facing, player.Sprite.Scale.Y);

        if (player.IsInverted())
            scale.Y *= -1.0f;

        TrailManager.Add(player, scale, color);
    }

    public static void CreateDreamTrail(this Player player)
    {
        player.CreateTrail(DreamTrailColors[DreamTrailColorIndex]);
        ++DreamTrailColorIndex;
        DreamTrailColorIndex %= 5;
    }

    public static bool DreamDashDie(this Player player, Vector2 previousPos, bool evenIfInvincible = false)
    {
        if (!evenIfInvincible && SaveData.Instance.Assists.Invincible)
        {
            player.Position = previousPos;
            player.Speed *= -1f;
            player.Play(SFX.game_assist_dreamblockbounce, null, 0f);
            return false;
        }

        player.Die(Vector2.Zero, evenIfInvincible, true);
        return true;
    }

    private static bool DreamTunneledIntoDeath(this Player player)
    {
        if (player.CollideCheck<DreamBlock>())
        {
            for (int x = 1; x <= 5; x++)
            {
                for (int signX = -1; signX <= 1; signX += 2)
                {
                    for (int y = 1; y <= 5; y++)
                    {
                        for (int signY = -1; signY <= 1; signY += 2)
                        {
                            Vector2 value = new(x * signX, y * signY);
                            if (!player.CollideCheck<DreamBlock>(player.Position + value))
                            {
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

    private static bool DreamTunnelDashCheck(this Player player, Vector2 dir)
    {
        Vector2 dashdir = player.DashDir;
        if (player.IsInverted())    
        {
            dir.Y *= -1;
            dashdir.Y *= -1;
        }

        if (dreamTunnelDashAttacking && player.DashAttacking && (dir.X == Math.Sign(dashdir.X) || dir.Y == Math.Sign(dashdir.Y)))
        {
            Rectangle bounds = player.SceneAs<Level>().Bounds;
            if (player.Left + dir.X < bounds.Left || player.Right + dir.X > bounds.Right || player.Top + dir.Y < bounds.Top || player.Bottom + dir.Y > bounds.Bottom)
                return false;

            Solid solid = null;

            // Check for dream blocks first, then for solids
            DreamBlock block = player.CollideFirst<DreamBlock>(player.Position + dir);
            if (block != null)
            {
                Vector2 side = new(Math.Abs(dir.Y), Math.Abs(dir.X));

                bool dashedIntoDreamBlock = true;
                bool checkNegative = dir.X != 0f ? player.Speed.Y <= 0f : player.Speed.X <= 0f;
                bool checkPositive = dir.X != 0f ? player.Speed.Y >= 0f : player.Speed.X >= 0f;
                if (checkNegative)
                {
                    for (int i = -1; i >= -Player_DashCornerCorrection; i--)
                    {
                        Vector2 at = player.Position + dir + (side * i);
                        if (!player.CollideCheck<DreamBlock>(at) && (solid = player.CollideFirst<Solid, DreamBlock>(at)) != null)
                        {
                            player.Position += side * i;
                            dashedIntoDreamBlock = false;
                            goto CheckDreamBlock;
                        }
                    }
                }

                if (checkPositive)
                {
                    for (int i = 1; i <= Player_DashCornerCorrection; i++)
                    {
                        Vector2 at = player.Position + dir + (side * i);
                        if (!player.CollideCheck<DreamBlock>(at) && (solid = player.CollideFirst<Solid, DreamBlock>(at)) != null)
                        {
                            player.Position += side * i;
                            dashedIntoDreamBlock = false;
                            goto CheckDreamBlock;
                        }
                    }
                }

            CheckDreamBlock:
                if (dashedIntoDreamBlock)
                {
                    if (DynamicData.For(block).Get<bool>("playerHasDreamDash"))
                        player.Die(-dir);

                    dreamTunnelDashAttacking = false;
                    overrideDreamDashCheck = true;
                    return false;
                }
            }

            solid ??= player.CollideFirst<Solid, DreamBlock>(player.Position + dir);
            // Don't dash through if it has a dash collide action, unless it's a farewell floaty block
            // or a DashBlock which is only breakable by a Kevin (canDash is false)
            if (solid != null && (!CommunalHelperModule.Settings.DreamTunnelIgnoreCollidables
                || solid.OnDashCollide == null
                || solid is FloatySpaceBlock
                || (solid is DashBlock b && !DynamicData.For(b).Get<bool>("canDash"))))
            {
                DynamicData playerData = player.GetData();
                player.StateMachine.State = StDreamTunnelDash;
                solid.Components.GetAll<DreamTunnelInteraction>().ToList().ForEach(i => i.OnPlayerEnter(player));
                playerData.Set(Player_solid, solid);
                playerData.Set("dashAttackTimer", 0);
                playerData.Set("gliderBoostTimer", 0);
                return true;
            }
            else if (solid is DashSwitch)
            {
                // Why is this necesarry? Good question!
                // I don't know the answer, but for some reason, Celeste registers
                // dashing into a button upwards as colliding with both the button and the
                // tile behind it. In order to prevent this from making you dash
                // through a wall after hitting a button, I disable the dream
                // tunnel after hitting a button.
                dreamTunnelDashAttacking = false;
            }
        }
        return false;
    }

    #region DreamTunnelState

    private static readonly PropertyInfo p_Player_StartedDashing
        = typeof(Player).GetProperty(nameof(Player.StartedDashing), BindingFlags.Instance | BindingFlags.Public);

    private static void DreamTunnelDashBegin(this Player player)
    {
        DynamicData playerData = player.GetData();

        p_Player_StartedDashing.SetValue(player, false);

        SoundSource dreamSfxLoop = playerData.Get<SoundSource>("dreamSfxLoop");
        if (dreamSfxLoop == null)
        {
            dreamSfxLoop = new SoundSource();
            player.Add(dreamSfxLoop);
            playerData.Set("dreamSfxLoop", dreamSfxLoop);
        }

        // Extra correction for fast moving solids, this does not cause issues with dashdir leniency
        Vector2 dir = player.DashDir.Sign();
        if (!player.CollideCheck<Solid, DreamBlock>() && player.CollideCheck<Solid, DreamBlock>(player.Position + dir))
        {
            player.NaiveMove(dir);
        }

        // Hackfix to unduck when downdiagonal dashing next to solid, caused by forcing the player into the solid as part of fast-moving solid correction
        if (player.DashDir.Y > 0)
            player.Ducking = false;

        player.Speed = player.DashDir * Player_DashSpeed;
        player.TreatNaive = true;
        player.Depth = Depths.PlayerDreamDashing;
        playerData.Set(Player_dreamTunnelDashCanEndTimer, 0.1f);
        player.Stamina = Player_ClimbMaxStamina;
        playerData.Set("dreamJump", false);
        player.Play(SFX.char_mad_dreamblock_enter, null, 0f);
        if (FeatherMode)
            player.Loop(dreamSfxLoop, CustomSFX.game_connectedDreamBlock_dreamblock_fly_travel);
        else
            player.Loop(dreamSfxLoop, SFX.char_mad_dreamblock_travel);

        // Allows DreamDashListener to also work from here, as this is basically a dream block, right?
        foreach (DreamDashListener component in player.Scene.Tracker.GetComponents<DreamDashListener>())
        {
            component.OnDreamDash?.Invoke(player.DashDir);
        }
    }

    private static void DreamTunnelDashEnd(this Player player)
    {
        DynamicData playerData = player.GetData();

        player.Depth = Depths.Player;
        if (!playerData.Get<bool>("dreamJump"))
        {
            player.AutoJump = true;
            player.AutoJumpTimer = 0f;
        }
        if (!player.Inventory.NoRefills)
        {
            player.RefillDash();
        }
        player.RefillStamina();
        player.TreatNaive = false;
        Solid solid = playerData.Get<Solid>(Player_solid);
        if (solid != null)
        {
            if (player.DashDir.X != 0f)
            {
                playerData.Set("jumpGraceTimer", 0.1f);
                playerData.Set("dreamJump", true);
            }
            else
            {
                playerData.Set("jumpGraceTimer", 0f);
            }
            solid.Components.GetAll<DreamTunnelInteraction>().ToList().ForEach(i => i.OnPlayerExit(player));
            solid = null;
        }
        player.Stop(playerData.Get<SoundSource>("dreamSfxLoop"));
        player.Play(SFX.char_mad_dreamblock_exit, null, 0f);
        Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
    }

    private static int DreamTunnelDashUpdate(this Player player)
    {
        DynamicData playerData = player.GetData();

        if (FeatherMode)
        {
            Vector2 input = Input.Aim.Value.SafeNormalize();
            if (input != Vector2.Zero)
            {
                Vector2 vector = player.Speed.SafeNormalize();
                if (vector != Vector2.Zero)
                {
                    vector = Vector2.Dot(input, vector) != -0.8f ? vector.RotateTowards(input.Angle(), 5f * Engine.DeltaTime) : vector;
                    vector = vector.CorrectJoystickPrecision();
                    player.DashDir = vector;
                    player.Speed = vector * 240f;
                }
            }
        }

        Input.Rumble(RumbleStrength.Light, RumbleLength.Medium);
        Vector2 position = player.Position;

        Vector2 factor = Vector2.One;
        if (player.IsInverted())
            factor.Y = -1;
        player.NaiveMove(player.Speed * factor * Engine.DeltaTime);

        float dreamDashCanEndTimer = playerData.Get<float>(Player_dreamTunnelDashCanEndTimer);
        if (dreamDashCanEndTimer > 0f)
        {
            playerData.Set(Player_dreamTunnelDashCanEndTimer, dreamDashCanEndTimer - Engine.DeltaTime);
        }
        Solid solid = player.CollideFirst<Solid, DreamBlock>();
        if (solid == null)
        {
            if (player.DreamTunneledIntoDeath())
            {
                player.DreamDashDie(position);
            }
            else if (playerData.Get<float>(Player_dreamTunnelDashCanEndTimer) <= 0f)
            {
                Celeste.Freeze(0.05f);
                if (Input.Jump.Pressed && player.DashDir.X != 0f)
                {
                    playerData.Set("dreamJump", true);
                    player.Jump(true, true);
                }
                else if (player.DashDir.Y >= 0f || player.DashDir.X != 0f)
                {
                    if (player.DashDir.X > 0f && player.CollideCheck<DreamBlock>(player.Position - (Vector2.UnitX * 5f)))
                    {
                        player.MoveHExact(-5, null, null);
                    }
                    else if (player.DashDir.X < 0f && player.CollideCheck<DreamBlock>(player.Position + (Vector2.UnitX * 5f)))
                    {
                        player.MoveHExact(5, null, null);
                    }
                    bool flag = player.ClimbCheck(-1, 0);
                    bool flag2 = player.ClimbCheck(1, 0);
                    int moveX = playerData.Get<int>("moveX");
                    if (Input.GrabCheck && ((moveX == 1 && flag2) || (moveX == -1 && flag)))
                    {
                        player.Facing = (Facings) moveX;
                        if (!SaveData.Instance.Assists.NoGrabbing)
                        {
                            return Player.StClimb;
                        }
                        player.ClimbTrigger(moveX);
                        player.Speed.X = 0f;
                    }
                }
                return Player.StNormal;
            }
        }
        else
        {
            playerData.Set(Player_solid, solid);
            if (player.Scene.OnInterval(0.1f))
            {
                player.CreateDreamTrail();
            }
            Level level = playerData.Get<Level>("level");
            if (level.OnInterval(0.04f))
            {
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
