using Celeste.Mod.CommunalHelper.Components;
using Celeste.Mod.CommunalHelper.States;
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

    internal const string Player_dreamTunnelDashCanEndTimer = "communalHelperDreamTunnelDashCanEndTimer";
    internal const string Player_solid = "communalHelperSolid";

    #endregion

    private static int dreamTunnelDashCount = 0;
    public static int DreamTunnelDashCount
    {
        get => CommunalHelperModule.Settings.AlwaysActiveDreamRefillCharge ? 1 : dreamTunnelDashCount;
        set => dreamTunnelDashCount = value;
    }
    private static bool canStartDreamTunnelDashAttack = false;
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


    public enum SpeedConfiguration
    {
        Default,
        NeverSlowDown,
        UseCustomSpeed,
    }

    public struct DreamTunnelDashConfiguration
    {
        public bool AllowRedirect;
        public bool AllowSameDirectionRedirect;
        public float SameDirectionSpeedMultiplier;
        public bool UseEntryDirection;
        public SpeedConfiguration SpeedConfiguration;
        public float CustomSpeed;
        public bool AllowDashCancels;
        public bool RedirectConsumesNormalDash;
        public bool AllowTransitions;
    }

    public static readonly DreamTunnelDashConfiguration DefaultDreamTunnelDashConfiguration = new()
    {
        AllowRedirect = false,
        AllowSameDirectionRedirect = false,
        SameDirectionSpeedMultiplier = 1,
        UseEntryDirection = false,
        SpeedConfiguration = SpeedConfiguration.Default,
        CustomSpeed = 0,
        AllowDashCancels = false,
        RedirectConsumesNormalDash = false,
        AllowTransitions = false,
    };


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
        IL.Celeste.Player.BeforeUpTransition += Player_BeforeUpTransition;
        IL.Celeste.Player.BeforeDownTransition += Player_BeforeDownTransition;
        IL.Celeste.Player.TransitionTo += Player_TransitionTo;
        hook_Player_orig_Update = new ILHook(
            typeof(Player).GetMethod("orig_Update"),
            Player_orig_Update);
        hook_Player_orig_UpdateSprite = new ILHook(
            typeof(Player).GetMethod("orig_UpdateSprite", BindingFlags.NonPublic | BindingFlags.Instance),
            State_DreamDashEqual);

        IL.Celeste.Level.EnforceBounds += Level_EnforceBounds;
        On.Celeste.Level.Reload += Level_Reload;
        On.Celeste.LevelLoader.StartLevel += LevelLoader_StartLevel;
        On.Celeste.Player.OnBoundsH += Player_OnBoundsH;
        On.Celeste.Player.OnBoundsV += Player_OnBoundsV;

        IL.Celeste.FakeWall.Update += State_DreamDashNotEqual;
        IL.Celeste.Spring.OnCollide += State_DreamDashEqual;
        IL.Celeste.Solid.Update += State_DreamDashNotEqual;
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
        IL.Celeste.Player.BeforeUpTransition -= Player_BeforeUpTransition;
        IL.Celeste.Player.BeforeDownTransition -= Player_BeforeDownTransition;
        IL.Celeste.Player.TransitionTo -= Player_TransitionTo;
        hook_Player_orig_Update.Dispose();
        hook_Player_orig_UpdateSprite.Dispose();

        IL.Celeste.Level.EnforceBounds -= Level_EnforceBounds;
        On.Celeste.Level.Reload -= Level_Reload;
        On.Celeste.LevelLoader.StartLevel -= LevelLoader_StartLevel;
        On.Celeste.Player.OnBoundsH -= Player_OnBoundsH;
        On.Celeste.Player.OnBoundsV -= Player_OnBoundsV;

        IL.Celeste.FakeWall.Update -= State_DreamDashNotEqual;
        IL.Celeste.Spring.OnCollide -= State_DreamDashEqual;
        IL.Celeste.Solid.Update -= State_DreamDashNotEqual;
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
        canStartDreamTunnelDashAttack = false;
        dreamTunnelDashAttacking = false;
    }

    private static void StartDreamTunnelDashAttacking(Player player, Vector2? checkDirFromDashCoroutine = null)
    {
        if (canStartDreamTunnelDashAttack)
        {
            dreamTunnelDashAttacking = true;
            dreamTunnelDashTimer = player.GetData().Get<float>("dashAttackTimer");

            // Ensures the player enters the dream tunnel dash state if dashing into a fast moving block
            // Because of how it works, it removes dashdir leniency if the solid is entered and AllowDashCancels is off :(
            DynamicData playerData = player.GetData();
            Vector2 checkDir = checkDirFromDashCoroutine ?? Input.GetAimVector(player.Facing);
            Vector2 dir = checkDir.Sign();
            if (!player.CollideCheck<Solid, DreamBlock>() && player.CollideCheck<Solid, DreamBlock>(player.Position + dir))
            {
                if (checkDirFromDashCoroutine is null) player.Speed = player.DashDir = checkDir;
                player.MoveHExact((int) dir.X, playerData.Get<Collision>("onCollideH"));
                player.MoveVExact((int) dir.Y, playerData.Get<Collision>("onCollideV"));
            }
        }

        canStartDreamTunnelDashAttack = false;
    }

    private static void UseDreamTunnelDash()
    {
        if (DreamTunnelDashCount > 0)
        {
            canStartDreamTunnelDashAttack = true;

            if (NextDashFeather)
            {
                FeatherMode = true;
                NextDashFeather = false;
            }
            DreamTunnelDashCount--;
        }
        else
        {
            canStartDreamTunnelDashAttack = false;
        }
    }

    private static void Player_DashBegin(On.Celeste.Player.orig_DashBegin orig, Player self)
    {
        orig(self);

        UseDreamTunnelDash();
        if (!CommunalHelperModule.Session.CurrentDreamTunnelDashConfiguration.AllowDashCancels)
            StartDreamTunnelDashAttacking(self);
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
        ILCursor cursor = new(il);

        /*
         * start the player dream tunnel dash later if needed to allow cancelling it
         * this replicates vanilla behavior of being able to instant hyper for example on dream blocks
         * inserts a call to StartDreamTunnelDashAttacking (without overriding Speed and DashDir) after the call to CallDashEvents if the current dream dash config allows dash cancels
         * is this the best place to put this logic?
         */
        ILLabel afterStartDashAttack = cursor.DefineLabel();
        cursor.GotoNext(MoveType.After, instr => instr.MatchCallvirt<Player>("CallDashEvents"));
        cursor.EmitDelegate(() => CommunalHelperModule.Session.CurrentDreamTunnelDashConfiguration.AllowDashCancels);
        cursor.Emit(OpCodes.Brfalse, afterStartDashAttack);
        cursor.Emit(OpCodes.Ldloc_1);
        cursor.EmitDelegate<Action<Player>>(player => StartDreamTunnelDashAttacking(player, player.DashDir));
        cursor.MarkLabel(afterStartDashAttack);

        /*
         * adds a check for !dreamTunnelDashAttacking to
         * if (player.onGround && player.DashDir.X != 0f && player.DashDir.Y > 0f && player.Speed.Y > 0f &&
         *  (!player.Inventory.DreamDash || !player.CollideCheck<DreamBlock>(player.Position + Vector2.UnitY)))
         */
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

        if (DreamTunnelDashCount > 0 && self.Scene.OnInterval(0.1f / DreamTunnelDashCount))
            self.CreateDreamTrail();
    }

    // St.DreamTunnelDash check handled in IL hook
    private static void Player_OnCollideH(On.Celeste.Player.orig_OnCollideH orig, Player self, CollisionData data)
    {
        if (!self.DreamTunnelDashCheck(Vector2.UnitX * Math.Sign(self.Speed.X)))
            orig(self, data);
    }

    // St.DreamTunnelDash check handled in IL hook
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
            State_DreamDashNotEqual(il);
    }

    private static void Player_BeforeUpTransition(ILContext il)
    {
        ILCursor cursor = new(il); 
        
        CheckState(cursor, Player.StRedDash, false);
        CheckState(cursor, Player.StRedDash, false);
    }

    private static void Player_BeforeDownTransition(ILContext il)
    {
        ILCursor cursor = new(il);
        
        CheckState(cursor, Player.StRedDash, false);
    }

    private static void Player_TransitionTo(ILContext il)
    {
        ILCursor cursor = new(il);

        if (cursor.TryGotoNext(MoveType.Before, instr => instr.MatchCall<Actor>("MoveTowardsX")))
        {
            UseInsteadIfDreamTunnelDashing(cursor, NaiveMoveTowardsX);
        }
        if (cursor.TryGotoNext(MoveType.Before, instr => instr.MatchCall<Actor>("MoveTowardsY")))
        {
            UseInsteadIfDreamTunnelDashing(cursor, NaiveMoveTowardsY);
        }
        return;

        // utility to replace one method call with another of the same signature if the player is dream tunnel dashing
        static void UseInsteadIfDreamTunnelDashing<T>(ILCursor cursor, T cb) where T : Delegate
        {
            ILLabel normalCall = cursor.DefineLabel();
            ILLabel afterNormalCall = cursor.DefineLabel();
        
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Player, bool>>(player => player.StateMachine.State == St.DreamTunnelDash);
            cursor.Emit(OpCodes.Brfalse_S, normalCall);
            cursor.EmitDelegate(cb);
            cursor.Emit(OpCodes.Br_S, afterNormalCall);
            cursor.MarkLabel(normalCall);
            // normal method call would be here
            cursor.Index++;
            cursor.MarkLabel(afterNormalCall);
        }
    }
    
    private static void NaiveMoveTowardsX(Player player, float targetX, float maxAmount, Collision _)
    {
        float toX = Calc.Approach(player.ExactPosition.X, targetX, maxAmount);
        float moveX = (float) ((double) toX - player.Position.X - player.movementCounter.X);
        player.NaiveMove(Vector2.UnitX * moveX);
    }
    
    private static void NaiveMoveTowardsY(Player player, float targetY, float maxAmount, Collision _)
    {
        float toY = Calc.Approach(player.ExactPosition.Y, targetY, maxAmount);
        float moveY = (float) ((double) toY - player.Position.Y - player.movementCounter.Y);
        player.NaiveMove(Vector2.UnitY * moveY);
    }

    // Utilities to patch any method that checks the player's State
    /// <summary>
    /// Use if decompilation says <c>State==9</c>.
    /// </summary>
    private static readonly ILContext.Manipulator State_DreamDashEqual = il => CheckState(new ILCursor(il), Player.StDreamDash, true);
    /// <summary>
    /// Use if decompilation says <c>State!=9</c>.
    /// </summary>
    private static readonly ILContext.Manipulator State_DreamDashNotEqual = il => CheckState(new ILCursor(il), Player.StDreamDash, false);

    /// <summary>
    /// Patch any method that checks the player's state.
    /// </summary>
    /// <remarks>Checks for <c>ldc.i4.s &lt;state&gt;</c></remarks>
    /// <param name="cursor">The ILCursor to use</param>
    /// <param name="state">The state to check for</param>
    /// <param name="equal">Whether the decompilation says <c>State == &lt;state&gt;</c></param>
    private static void CheckState(ILCursor cursor, int state, bool equal)
    {
        // essentially, we want to perform these conversions:
        // `player.StateMachine.State == state` -> `player.StateMachine.State == state || player.StateMachine.State == St.DreamTunnelDash`
        // `player.StateMachine.State != state` -> `player.StateMachine.State != state && player.StateMachine.State != St.DreamTunnelDash`
        
        // variables to grab stuff later
        Instruction afterMatch = null;
        
        bool matchedBeqOrBne = false, matchedCeq = false;
        ILLabel failedCheck = null;
        Instruction ceqInstr = null;
        
        if (!cursor.TryGotoNextFirstFitReversed(MoveType.AfterLabel, 0x10,
            instr => instr.MatchLdfld<Player>("StateMachine"),
            instr => instr.MatchCallvirt<StateMachine>("get_State"),
            instr => instr.MatchLdcI4(state),
            instr =>
            {
                // we grab a lot of stuff here: the instruction directly after this match, whether we matched a beq/bne.un or a ceq,
                // the "fail state" label of the beq/bne.un (if we matched one of those) and the actual ceq instruction (if we matched that).
                
                afterMatch = instr.Next;
                // equality checks usually use bne.un (for ==) or beq (for !=) to branch past the block of the if statement if the values don't match
                matchedBeqOrBne = equal ? instr.MatchBneUn(out failedCheck) : instr.MatchBeq(out failedCheck);
                matchedCeq = (ceqInstr = instr).MatchCeq();
                return matchedBeqOrBne || matchedCeq;
            }))
            return;
        
        // beq and bne.un work with labels, whereas ceq just leaves a bool so we need to deal with them differently
        if (matchedBeqOrBne)
        {
            // labels for cleaning up duplicate player on stack
            ILLabel cleanUpPlayer = cursor.DefineLabel(), pastCleanUpPlayer = cursor.DefineLabel();
            
            // duplicate player on stack
            cursor.Emit(OpCodes.Dup);
            // check if player is dream tunnel dashing and if so, short-circuit (while also cleaning up the other, now unnecessary duplicate player)
            cursor.EmitDelegate<Func<Player, bool>>(player => player.StateMachine.State == St.DreamTunnelDash);
            cursor.Emit(OpCodes.Brtrue, cleanUpPlayer);
            // else, continue with check as normal
            
            // where we short-circuit to depends on whether we check for equality or not.
            // for equality, we should short-circuit to the "block of the if statement" (past our current condition, whether it be the actual block or another condition),
            // since our desired behaviour is `player.StateMachine.State == state || player.StateMachine.State == St.DreamTunnelDash` and the first part of that or has been satisfied,
            // just like how `if (true || condition()) { ... }` should immediately skip checking `condition()` (since `true` or anything is `true`) and run the block inside the if.
            // for inequality, it's the other way around: we should short-circuit immediately past the rest of the if statement, since our desired behaviour will be
            // `player.StateMachine.State != state && player.StateMachine.State != St.DreamTunnelDash` and we know the first condition of that and is false, just like how
            // `if (false && condition()) { ... }` should immediately skip checking `condition()` (since `false` and anything is `false`) and never run the block inside the if.
            // our `afterMatch` label points to after our current condition, and our `failedCheck` label points to after the rest of the statement.
            cursor.Goto(equal ? afterMatch : failedCheck.Target);
            // extra player cleanup, we branch over it in normal behaviour but jump into it if needed (see above)
            cursor.Emit(OpCodes.Br, pastCleanUpPlayer);
            cursor.Emit(OpCodes.Pop);
            cursor.MarkLabel(pastCleanUpPlayer);
            cursor.Index--;
            cursor.MarkLabel(cleanUpPlayer);
        }
        else if (matchedCeq)
        {
            // duplicate player on stack
            cursor.Emit(OpCodes.Dup);
            // go to the ceq instruction and modify the value it returns
            cursor.Goto(ceqInstr, MoveType.After);
            // our desired value for what the ceq instruction returns depends on whether we check equality or not. but we don't control that, the brtrue/brfalse after the ceq does.
            // to solve this, our desired conditions can be shown equivalent to `player.StateMachine.State == state || player.StateMachine.State == St.DreamTunnelDash` (for equality) and
            // `!(player.StateMachine.State == state || player.StateMachine.State == St.DreamTunnelDash)` (for inequality). notice how the desired condition for inequality is simply
            // the inverse of the one for equality. this is great, because the brtrue/brfalse after the ceq will do the required inversion (or lack thereof) for the check, and all
            // we need to do is calculate the thing on the inside. conveniently, the ceq is already checking for the left term (so that is its return value), and we just need to or it with the right.
            cursor.EmitDelegate<Func<Player, bool, bool>>((player, orig) => orig || player.StateMachine.State == St.DreamTunnelDash);
        }

        // go to after the current match to continue with the il hook (no infinite loops!)
        cursor.Goto(afterMatch, MoveType.After);
    }

    private static void Player_orig_Update(ILContext il)
    {
        ILCursor cursor = new(il);
        CheckState(cursor, Player.StDreamDash, true);
        CheckState(cursor, Player.StDreamDash, false);
        CheckState(cursor, Player.StDreamDash, false);
        // Not used because we DO want to enforce Level bounds.
        //Check_State_DreamDash(cursor, false, true);
    }
    
    private static void Level_EnforceBounds(ILContext il)
    {
        ILCursor cursor = new(il);
        
        // Kill the player if they attempt to DreamTunnel out of the level and transitions are not enabled
        if (cursor.TryGotoNext(MoveType.After,
            instr => instr.MatchLdarg(0),
            instr => instr.MatchLdfld<Level>("transition"),
            instr => instr.MatchBrfalse(out ILLabel _),
            instr => instr.MatchRet()))
        {
            ILLabel afterReturn = cursor.DefineLabel();
            
            cursor.MoveAfterLabels();
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Func<Level, Player, bool>>((self, player) =>
            {
                if (player.StateMachine.State != St.DreamTunnelDash || CommunalHelperModule.Session.CurrentDreamTunnelDashConfiguration.AllowTransitions)
                    return false;
                
                Rectangle bounds = self.Bounds;
                if (player.Right <= bounds.Right && player.Left >= bounds.Left && player.Top >= bounds.Top && player.Bottom <= bounds.Bottom)
                    return false;
                
                player.DreamDashDie(player.Position);
                return true;
            });
            cursor.Emit(OpCodes.Brfalse_S, afterReturn);
            cursor.Emit(OpCodes.Ret);
            cursor.MarkLabel(afterReturn);
        }
        
        // Ignore check for solids on down transition if dream tunnel dashing
        if (cursor.TryGotoNext(MoveType.After,
            instr => instr.MatchCallvirt<Entity>("CollideCheck")))
        {
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Func<Player, bool>>(player => player.StateMachine.State != St.DreamTunnelDash);
            cursor.Emit(OpCodes.And);
        }
    }

    private static void Level_Reload(On.Celeste.Level.orig_Reload orig, Level self)
    {
        DreamTunnelDashCount = 0;
        dreamTunnelDashAttacking = false;
        orig(self);
    }

    private static void LevelLoader_StartLevel(On.Celeste.LevelLoader.orig_StartLevel orig, LevelLoader self)
    {
        DreamTunnelDashCount = 0;
        dreamTunnelDashAttacking = false;
        orig(self);
    }

    // Handles cases with locked camera
    private static void Player_OnBoundsH(On.Celeste.Player.orig_OnBoundsH orig, Player self)
    {
        if (self.StateMachine.State == St.DreamTunnelDash)
        {
            self.DreamDashDie(self.Position);
            return;
        }

        orig(self);
    }

    // Handles cases with locked camera
    private static void Player_OnBoundsV(On.Celeste.Player.orig_OnBoundsV orig, Player self)
    {
        if (self.StateMachine.State == St.DreamTunnelDash)
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

    internal static bool DreamTunneledIntoDeath(this Player player)
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
                player.StateMachine.State = St.DreamTunnelDash;
                solid.Components.GetAll<DreamTunnelInteraction>().ToList().ForEach(i => i.OnPlayerEnter(player));
                playerData.Set(Player_solid, solid);
                playerData.Set("dashAttackTimer", 0f);
                playerData.Set("gliderBoostTimer", 0f);
                return true;
            }
            else if (solid is DashSwitch)
            {
                // Why is this necessary? Good question!
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

    #endregion

}
