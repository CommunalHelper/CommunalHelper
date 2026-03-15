using Celeste.Mod.CommunalHelper.Components;
using Celeste.Mod.CommunalHelper.Entities;
using Celeste.Mod.CommunalHelper.States;
using Celeste.Mod.Helpers;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.DashStates;

public static class DreamTunnelDash
{
    internal class DreamTunnelDashComponent() : Component(false, false)
    {
        public static readonly Color[] DreamTrailColors = [
            Calc.HexToColor("FFEF11"),
            Calc.HexToColor("08A310"),
            Calc.HexToColor("FF00D0"),
            Calc.HexToColor("5FCDE4"),
            Calc.HexToColor("E0564C")
        ];
        public int DreamTrailColorIndex = 0;

        public int DreamTunnelDashCount
        {
            get => CommunalHelperModule.Settings.AlwaysActiveDreamRefillCharge ? 1 : field;
            set;
        }

        public bool NextDashFeather
        {
            get => field || CommunalHelperModule.Settings.DreamDashFeatherMode;
            set;
        }
        public bool FeatherMode;
        
        public bool CanStartDreamTunnelDashAttack;
        public bool DreamTunnelDashAttacking;
        
        public float DreamTunnelDashTimer;
        public float DreamTunnelDashCanEndTimer;
        
        public bool OverrideDreamDashCheck;

        public Solid Solid;

        public void Reset()
        {
            CanStartDreamTunnelDashAttack = false;
            DreamTunnelDashAttacking = false;
            Solid = null;
        }
    }
    
    public struct DreamTunnelDashConfiguration
    {
        public enum SpeedConfigurations
        {
            Default,
            NeverSlowDown,
            UseCustomSpeed,
        }
        
        public bool AllowRedirect;
        public bool AllowSameDirectionRedirect;
        public float SameDirectionSpeedMultiplier;
        public bool UseEntryDirection;
        public SpeedConfigurations SpeedConfiguration;
        public float CustomSpeed;
        public bool AllowDashCancels;
        public bool RedirectConsumesNormalDash;
        public bool AllowTransitions;
        public bool BounceOnCollision;
        public bool RespectBoosters;
        public bool RespectDreamBlockLikes;
        
        public static readonly DreamTunnelDashConfiguration Default = new()
        {
            AllowRedirect = false,
            AllowSameDirectionRedirect = false,
            SameDirectionSpeedMultiplier = 1,
            UseEntryDirection = false,
            SpeedConfiguration = SpeedConfigurations.Default,
            CustomSpeed = 0,
            AllowDashCancels = false,
            RedirectConsumesNormalDash = false,
            AllowTransitions = false,
            BounceOnCollision = false,
            RespectBoosters = false,
            RespectDreamBlockLikes = false
        };
    }

    // Keep a List<Entity> around to not allocate a new one every `CollideAll<T>()` call
    private static readonly List<Entity> DreamTunnelBlockers = [];
    
    #region Hooks

    private static ILHook ilHook_Player_DashCoroutine;
    private static ILHook ilHook_Player_orig_Update;
    private static ILHook ilHook_Player_orig_UpdateSprite;
    
    internal static void Load()
    {
        // everest events
        Everest.Events.Player.OnSpawn += OnSpawn;
        Everest.Events.Player.OnDie += OnDie;
        Everest.Events.AssetReload.OnBeforeReload += OnBeforeReload;
        
        // player on hooks
        On.Celeste.Player.DashBegin += Player_DashBegin;
        On.Celeste.Player.CreateTrail += Player_CreateTrail;
        On.Celeste.Player.OnCollideH += Player_OnCollideH;
        On.Celeste.Player.OnCollideV += Player_OnCollideV;
        On.Celeste.Player.OnBoundsH += Player_OnBoundsH;
        On.Celeste.Player.OnBoundsV += Player_OnBoundsV;
        On.Celeste.Player.DreamDashCheck += Player_DreamDashCheck;
        On.Celeste.Player.Update += Player_Update;
        
        // player il hooks
        IL.Celeste.Player.IsRiding_Solid += State_DreamDashEqual;
        IL.Celeste.Player.IsRiding_JumpThru += State_DreamDashEqual;
        IL.Celeste.Player.OnCollideH += State_DreamDashEqual;
        IL.Celeste.Player.OnCollideV += State_DreamDashEqual;
        IL.Celeste.Player.BeforeUpTransition += Player_BeforeUpTransition;
        IL.Celeste.Player.BeforeDownTransition += Player_BeforeDownTransition;
        IL.Celeste.Player.TransitionTo += Player_TransitionTo;
        ilHook_Player_DashCoroutine = new ILHook(
            typeof(Player).GetMethod("DashCoroutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget(),
            Player_DashCoroutine);
        ilHook_Player_orig_Update = new ILHook(
            typeof(Player).GetMethod("orig_Update"),
            Player_orig_Update);
        ilHook_Player_orig_UpdateSprite = new ILHook(
            typeof(Player).GetMethod("orig_UpdateSprite", BindingFlags.NonPublic | BindingFlags.Instance),
            State_DreamDashEqual);

        // other hooks
        IL.Celeste.Level.EnforceBounds += Level_EnforceBounds;
        IL.Celeste.FakeWall.Update += State_DreamDashNotEqual;
        IL.Celeste.Spring.OnCollide += State_DreamDashEqual_ShortCircuit;
        IL.Celeste.Solid.Update += State_DreamDashNotEqual;
    }

    internal static void Unload()
    {
        Everest.Events.Player.OnSpawn -= OnSpawn;
        Everest.Events.Player.OnDie -= OnDie;
        Everest.Events.AssetReload.OnBeforeReload -= OnBeforeReload;
        
        On.Celeste.Player.DashBegin -= Player_DashBegin;
        On.Celeste.Player.CreateTrail -= Player_CreateTrail;
        On.Celeste.Player.OnCollideH -= Player_OnCollideH;
        On.Celeste.Player.OnCollideV -= Player_OnCollideV;
        On.Celeste.Player.OnBoundsH -= Player_OnBoundsH;
        On.Celeste.Player.OnBoundsV -= Player_OnBoundsV;
        On.Celeste.Player.DreamDashCheck -= Player_DreamDashCheck;
        On.Celeste.Player.Update -= Player_Update;
        
        IL.Celeste.Player.IsRiding_Solid -= State_DreamDashEqual;
        IL.Celeste.Player.IsRiding_JumpThru -= State_DreamDashEqual;
        IL.Celeste.Player.OnCollideH -= State_DreamDashEqual;
        IL.Celeste.Player.OnCollideV -= State_DreamDashEqual;
        IL.Celeste.Player.BeforeUpTransition -= Player_BeforeUpTransition;
        IL.Celeste.Player.BeforeDownTransition -= Player_BeforeDownTransition;
        IL.Celeste.Player.TransitionTo -= Player_TransitionTo;
        ilHook_Player_DashCoroutine.Dispose();
        ilHook_Player_DashCoroutine = null;
        ilHook_Player_orig_Update.Dispose();
        ilHook_Player_orig_Update = null;
        ilHook_Player_orig_UpdateSprite.Dispose();
        ilHook_Player_orig_UpdateSprite = null;

        IL.Celeste.Level.EnforceBounds -= Level_EnforceBounds;
        IL.Celeste.FakeWall.Update -= State_DreamDashNotEqual;
        IL.Celeste.Spring.OnCollide -= State_DreamDashEqual_ShortCircuit;
        IL.Celeste.Solid.Update -= State_DreamDashNotEqual;
    }
    
    #region Everest Events
    
    private static void OnSpawn(Player player)
    {
        if (player.Get<DreamTunnelDashComponent>() is null)
            player.Add(new DreamTunnelDashComponent());
    }

    private static void OnDie(Player player)
        => player.dreamSfxLoop?.Stop();

    // remove component before reload so the player can't end up with more than 1
    private static void OnBeforeReload(bool silent)
    {
        if (Engine.Scene?.Tracker?.GetEntity<Player>() is { } player && player.Get<DreamTunnelDashComponent>() is { } component)
            player.Remove(component);
    }
    
    #endregion
    
    #region Player
    
    #region On

    private static void Player_DashBegin(On.Celeste.Player.orig_DashBegin orig, Player self)
    {
        orig(self);
        
        if (CommunalHelperModule.Session.CurrentDreamTunnelDashConfiguration.RespectBoosters && self.CurrentBooster is not null)
            return;

        self.UseDreamTunnelDash();
        if (!CommunalHelperModule.Session.CurrentDreamTunnelDashConfiguration.AllowDashCancels)
            self.StartDreamTunnelDashAttacking();
    }

    // DreamTunnelDash trail recoloring
    private static void Player_CreateTrail(On.Celeste.Player.orig_CreateTrail orig, Player self)
    {
        if (self.Get<DreamTunnelDashComponent>() is not { DreamTunnelDashAttacking: true })
        {
            orig(self);
            return;
        }
        
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
    
    // Handles cases with locked camera
    private static bool Player_DreamDashCheck(On.Celeste.Player.orig_DreamDashCheck orig, Player self, Vector2 dir)
    {
        if (self.Get<DreamTunnelDashComponent>() is not { } component)
            return orig(self, dir);
        
        if (component.OverrideDreamDashCheck)
            return component.OverrideDreamDashCheck = false;

        // Don't enter StDreamDash if there's a blocker
        if (self.IsDreamDashBlocked(self.Position + dir))
            return false;

        return orig(self, dir);
    }
    
    private static void Player_Update(On.Celeste.Player.orig_Update orig, Player self)
    {
        orig(self);
        
        if (self.Get<DreamTunnelDashComponent>() is not { } component)
            return;

        if (self.dashAttackTimer < component.DreamTunnelDashTimer)
            component.DreamTunnelDashTimer = self.dashAttackTimer;
        else if (component.DreamTunnelDashTimer > 0)
            component.DreamTunnelDashTimer -= Engine.DeltaTime;

        if (component.DreamTunnelDashTimer <= 0f)
            component.DreamTunnelDashAttacking = false;

        if (component.DreamTunnelDashCount > 0 && self.Scene.OnInterval(0.1f / component.DreamTunnelDashCount))
            self.CreateDreamTrail();
    }
    
    #endregion
    
    #region IL

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

    private static void Player_BeforeUpTransition(ILContext il)
    {
        ILCursor cursor = new(il);

        CheckState(cursor, Player.StRedDash, false, false);
        CheckState(cursor, Player.StRedDash, false, false);
    }

    private static void Player_BeforeDownTransition(ILContext il)
    {
        ILCursor cursor = new(il);

        CheckState(cursor, Player.StRedDash, false, false);
    }

    private static void Player_TransitionTo(ILContext il)
    {
        ILCursor cursor = new(il);

        if (cursor.TryGotoNext(MoveType.Before, instr => instr.MatchCall<Actor>("MoveTowardsX")))
            UseInsteadIfDreamTunnelDashing(cursor, NaiveMoveTowardsX);
        if (cursor.TryGotoNext(MoveType.Before, instr => instr.MatchCall<Actor>("MoveTowardsY")))
            UseInsteadIfDreamTunnelDashing(cursor, NaiveMoveTowardsY);

        return;
        
        static void NaiveMoveTowardsX(Player player, float targetX, float maxAmount, Collision _)
        {
            float toX = Calc.Approach(player.ExactPosition.X, targetX, maxAmount);
            float moveX = (float) ((double) toX - player.Position.X - player.movementCounter.X);
            player.NaiveMove(Vector2.UnitX * moveX);
        }

        static void NaiveMoveTowardsY(Player player, float targetY, float maxAmount, Collision _)
        {
            float toY = Calc.Approach(player.ExactPosition.Y, targetY, maxAmount);
            float moveY = (float) ((double) toY - player.Position.Y - player.movementCounter.Y);
            player.NaiveMove(Vector2.UnitY * moveY);
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
        cursor.GotoNext(MoveType.After, instr => instr.MatchCallvirt<Player>("CallDashEvents"));
        cursor.Emit(OpCodes.Ldloc_1);
        cursor.EmitDelegate(PlayerStartDreamTunnelDashAttacking);

        /*
         * adds a check for !dreamTunnelDashAttacking to
         * if (player.onGround && player.DashDir.X != 0f && player.DashDir.Y > 0f && player.Speed.Y > 0f &&
         *  (!player.Inventory.DreamDash || !player.CollideCheck<DreamBlock>(player.Position + Vector2.UnitY)))
         */
        cursor.GotoNext(MoveType.After, instr => instr.MatchLdfld<Player>("onGround"));
        cursor.Emit(cursor.Next.OpCode, cursor.Next.Operand);
        cursor.Emit(OpCodes.Ldloc_1);
        cursor.EmitDelegate(PlayerIsDreamTunnelDashAttacking);
        cursor.Next.OpCode = OpCodes.Brtrue; // hmm

        return;

        static void PlayerStartDreamTunnelDashAttacking(Player player)
        {
            if (CommunalHelperModule.Session.CurrentDreamTunnelDashConfiguration.RespectBoosters && player.CurrentBooster is not null)
                return;
        
            if (CommunalHelperModule.Session.CurrentDreamTunnelDashConfiguration.AllowDashCancels)
                player.StartDreamTunnelDashAttacking(player.DashDir);
        }

        static bool PlayerIsDreamTunnelDashAttacking(Player player)
            => player.Get<DreamTunnelDashComponent>() is { DreamTunnelDashAttacking: true };
    }
    
    private static void Player_orig_Update(ILContext il)
    {
        ILCursor cursor = new(il);
        CheckState(cursor, Player.StDreamDash, true, false);
        CheckState(cursor, Player.StDreamDash, false, false);
        CheckState(cursor, Player.StDreamDash, false, false);
        // Not used because we DO want to enforce Level bounds.
        // CheckState(cursor, Player.StDreamDash, false, false);
    }
    
    #endregion
    
    #endregion
    
    #region Other

    private static void Level_EnforceBounds(ILContext il)
    {
        ILCursor cursor = new(il);

        // Kill the player if they attempt to dream tunnel dash out of the level and transitions are not enabled
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
            cursor.EmitDelegate(ShouldReturnEarly);
            cursor.Emit(OpCodes.Brfalse_S, afterReturn);
            cursor.Emit(OpCodes.Ret);
            cursor.MarkLabel(afterReturn);
        }

        // Ignore check for solids on down transition if dream tunnel dashing
        if (cursor.TryGotoNext(MoveType.After,
            instr => instr.MatchCallvirt<Entity>("CollideCheck")))
        {
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate(PlayerIsNotDreamTunnelDashing);
            cursor.Emit(OpCodes.And);
        }

        return;

        static bool ShouldReturnEarly(Level level, Player player)
        {
            if (player.StateMachine.State != St.DreamTunnelDash || CommunalHelperModule.Session.CurrentDreamTunnelDashConfiguration.AllowTransitions)
                return false;

            Rectangle bounds = level.Bounds;
            if (player.Right <= bounds.Right && player.Left >= bounds.Left && player.Top >= bounds.Top && player.Bottom <= bounds.Bottom)
                return false;

            player.DreamDashDie(player.Position);
            return true;
        }
    }
    
    #endregion

    #endregion
    
    #region Utils
    
    // Utilities to check the player's `State`.
    private static bool PlayerIsDreamTunnelDashing(Player player)
        => player.StateMachine.State == St.DreamTunnelDash;
    private static bool PlayerIsNotDreamTunnelDashing(Player player)
        => player.StateMachine.State != St.DreamTunnelDash;

    // Utilities to patch any method that checks the player's `State`.
    /// <summary>
    /// Use if decompilation says <c>State == 9</c>.
    /// </summary>
    private static readonly ILContext.Manipulator State_DreamDashEqual = il => CheckState(new ILCursor(il), Player.StDreamDash, true, false);
    /// <summary>
    /// Use if decompilation says <c>State != 9</c>.
    /// </summary>
    private static readonly ILContext.Manipulator State_DreamDashNotEqual = il => CheckState(new ILCursor(il), Player.StDreamDash, false, false);
    /// <summary>
    /// Use if decompilation says <c>State == 9</c> and the check is able to short-circuit (e.g. is followed by <c>||</c> but not preceded by <c>&amp;&amp;</c>).
    /// </summary>
    private static readonly ILContext.Manipulator State_DreamDashEqual_ShortCircuit = il => CheckState(new ILCursor(il), Player.StDreamDash, true, true);
    /// <summary>
    /// Use if decompilation says <c>State != 9</c> and the check is able to short-circuit (e.g. is followed by <c>||</c> but not preceded by <c>&amp;&amp;</c>).
    /// </summary>
    private static readonly ILContext.Manipulator State_DreamDashNotEqual_ShortCircuit = il => CheckState(new ILCursor(il), Player.StDreamDash, false, true);

    /// <summary>
    /// Patch any method that checks the player's <c>State</c>.
    /// </summary>
    /// <remarks>Checks for <c>ldc.i4.s &lt;state&gt;</c>.</remarks>
    /// <param name="cursor">The ILCursor to use</param>
    /// <param name="state">The state to check for</param>
    /// <param name="equal">Whether to check for <c>== &lt;state&gt;</c> or <c>!= &lt;state&gt;</c></param>
    /// <param name="canShortCircuit">Whether the state check to match can short-circuit.</param>
    private static void CheckState(ILCursor cursor, int state, bool equal, bool canShortCircuit)
    {
        // essentially, we want to perform these conversions:
        //  - `player.StateMachine.State == <state>` -> `player.StateMachine.State == St.DreamTunnelDash || player.StateMachine.State == <state>`
        //  - `player.StateMachine.State != <state>` -> `player.StateMachine.State != St.DreamTunnelDash && player.StateMachine.State != <state>`
        // the dream tunnel dash check comes before the normal check because 1. it's more predictable to implement and 2. vanilla has no state checks
        // that cause side effects and would thus be broken by our short-circuiting method.

        // go to before the state check
        if (!cursor.TryGotoNextFirstFitReversed(MoveType.AfterLabel, 0x10,
            instr => instr.MatchLdfld<Player>("StateMachine"),
            instr => instr.MatchCallvirt<StateMachine>("get_State"),
            instr => instr.MatchLdcI4(state)))
            return;

        ILLabel failedCheck = null;

        // retrieve "fail state" label of the current check
        // equality checks usually use `bne.un` (for `==`) or `beq` (for `!=`) in order to branch past the block of the if statement when the values don't match
        // except when they are capable of short-circuiting, in which case the opposite branch instruction is used
        // we don't check for `ceq`s because they are used for state checks a grand total of 3 times in vanilla
        // (in `FlyFeather::OnPlayer`, `Lookout::Update` and `Player::<.ctor>b__280_0` for those curious)
        ILCursor cloned = cursor.Clone();
        if (!cloned.TryGotoNext(MoveType.After, instr => equal ^ canShortCircuit ? instr.MatchBneUn(out failedCheck) : instr.MatchBeq(out failedCheck)))
            return;
        // also get the instruction after the current check
        Instruction afterMatch = cloned.Next!;

        // labels for cleaning up duplicate player left on stack
        ILLabel cleanUpPlayer = cursor.DefineLabel(), pastCleanUpPlayer = cursor.DefineLabel();

        // duplicate player on stack
        cursor.Emit(OpCodes.Dup);
        // check if player is dream tunnel dashing and if so, short-circuit (while also popping the other, now unnecessary duplicate player off the stack)
        cursor.EmitDelegate(PlayerIsDreamTunnelDashing);
        cursor.Emit(OpCodes.Brtrue, cleanUpPlayer);
        // else, continue with check as normal

        // where we short-circuit to depends on whether we check for equality and whether the check we're modding needs to short-circuit.
        // in the non-short-circuiting case:
        // for equality, we should short-circuit to the "block" of the if statement (past our current condition, whether it be the actual block or another condition),
        // since our desired behaviour is `player.StateMachine.State == state || player.StateMachine.State == St.DreamTunnelDash` and the first term of that or has been satisfied,
        // just like how `if (true || condition()) { ... }` should immediately skip checking `condition()` (since `true` or anything is `true`) and run the block inside the if.
        // for inequality, it's the other way around: we should short-circuit past the rest of the if statement (skipping the block), since our desired behaviour will be
        // `player.StateMachine.State != state && player.StateMachine.State != St.DreamTunnelDash` and we know the first condition of that and is false, just like how
        // `if (false && condition()) { ... }` should immediately skip checking `condition()` (since `false` and anything is `false`) and never run the block inside the if.
        // in the short-circuiting case:
        // for equality, we should also short-circuit to the "block" of the if statement, although this is now given by the `failedCheck` label instead of `afterMatch`.
        // for inequality, if the state check returns true (i.e. `player.StateMachine.State != state` is false), then the entire check should be false, and we can jump ahead
        // to the next condition in the sequence (our `afterMatch` label).
        // our `afterMatch` label points to after our current condition, and our `failedCheck` label points to after the rest of the statement.
        cursor.Goto(equal ^ canShortCircuit ? afterMatch : failedCheck.Target);
        // extra player cleanup, we skip over it in normal behaviour but jump into it if needed (see above)
        cursor.Emit(OpCodes.Br, pastCleanUpPlayer);
        cursor.Emit(OpCodes.Pop);
        cursor.MarkLabel(pastCleanUpPlayer);
        cursor.Index--;
        // mark label to short-circuit to
        cursor.MarkLabel(cleanUpPlayer);
        
        // go to after the current match to continue with the il hook (no infinite loops!)
        cursor.Goto(afterMatch, MoveType.After);
    }
    
    // utility to replace one method call with another of the same signature if the player is dream tunnel dashing
    private static void UseInsteadIfDreamTunnelDashing<T>(ILCursor cursor, T cb) where T : Delegate
    {
        ILLabel normalCall = cursor.DefineLabel();
        ILLabel afterNormalCall = cursor.DefineLabel();

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate(PlayerIsDreamTunnelDashing);
        cursor.Emit(OpCodes.Brfalse_S, normalCall);
        cursor.EmitDelegate(cb);
        cursor.Emit(OpCodes.Br_S, afterNormalCall);
        cursor.MarkLabel(normalCall);
        // normal method call would be here
        cursor.Index++;
        cursor.MarkLabel(afterNormalCall);
    }
    
    #endregion

    #region Extensions
    
    private static void StartDreamTunnelDashAttacking(this Player player, Vector2? checkDirFromDashCoroutine = null)
    {
        if (player.Get<DreamTunnelDashComponent>() is not { } component)
            return;
        
        if (component.CanStartDreamTunnelDashAttack)
        {
            component.DreamTunnelDashAttacking = true;
            component.DreamTunnelDashTimer = player.dashAttackTimer;

            // Ensures the player enters the dream tunnel dash state if dashing into a fast moving block
            // Because of how it works, it removes dashdir leniency if the solid is entered and AllowDashCancels is off :(
            DynamicData playerData = player.GetData();
            Vector2 checkDir = checkDirFromDashCoroutine ?? Input.GetAimVector(player.Facing);
            Vector2 dir = checkDir.Sign();
            if (!DreamTunnelDangerous.CollideCheckNonDangerous(player)
                && DreamTunnelDangerous.CollideCheckNonDangerous(player, player.Position + dir))
            {
                if (checkDirFromDashCoroutine is null) player.Speed = player.DashDir = checkDir;
                player.MoveHExact((int) dir.X, player.onCollideH);
                player.MoveVExact((int) dir.Y, player.onCollideV);
            }
        }

        component.CanStartDreamTunnelDashAttack = false;
    }

    private static void UseDreamTunnelDash(this Player player)
    {
        if (player.Get<DreamTunnelDashComponent>() is not { } component)
            return;
        
        if (component.DreamTunnelDashCount > 0)
        {
            component.CanStartDreamTunnelDashAttack = true;

            if (component.NextDashFeather)
            {
                component.FeatherMode = true;
                component.NextDashFeather = false;
            }
            component.DreamTunnelDashCount--;
        }
        else
            component.CanStartDreamTunnelDashAttack = false;
    }

    public static void CreateTrail(this Player player, Color color)
    {
        Vector2 scale = new(Math.Abs(player.Sprite.Scale.X) * (float) player.Facing, player.Sprite.Scale.Y);

        if (player.IsInverted())
            scale.Y *= -1.0f;

        TrailManager.Add(player, scale, color);
    }

    public static void CreateDreamTrail(this Player player)
    {
        if (player.Get<DreamTunnelDashComponent>() is not { } component)
            return;
        
        player.CreateTrail(DreamTunnelDashComponent.DreamTrailColors[component.DreamTrailColorIndex]);
        component.DreamTrailColorIndex++;
        component.DreamTrailColorIndex %= DreamTunnelDashComponent.DreamTrailColors.Length;
    }

    public static void DreamDashDie(this Player player, Vector2 previousPos, bool evenIfInvincible = false)
    {
        if (!evenIfInvincible && SaveData.Instance.Assists.Invincible)
        {
            player.Position = previousPos;
            player.Speed *= -1f;
            player.Play(SFX.game_assist_dreamblockbounce);
        }

        player.Die(Vector2.Zero, evenIfInvincible);
    }

    internal static bool DreamTunneledIntoDeath(this Player player)
    {
        if (!DreamTunnelDangerous.CollideCheck(player))
            return false;
        
        for (int x = 1; x <= 5; x++)
        {
            for (int signX = -1; signX <= 1; signX += 2)
            {
                for (int y = 1; y <= 5; y++)
                {
                    for (int signY = -1; signY <= 1; signY += 2)
                    {
                        Vector2 value = new(x * signX, y * signY);
                        if (DreamTunnelDangerous.CollideCheck(player, player.Position + value))
                            continue;
                            
                        player.Position += value;
                        return false;
                    }
                }
            }
        }
        return true;
    }

    private static bool DreamTunnelDashCheck(this Player player, Vector2 dir)
    {
        if (player.Get<DreamTunnelDashComponent>() is not { } component)
            return false;
        
        Vector2 dashdir = player.DashDir;
        if (player.IsInverted())
        {
            dir.Y *= -1;
            dashdir.Y *= -1;
        }

        if (!component.DreamTunnelDashAttacking
            || !player.DashAttacking
            || (dir.X != Math.Sign(dashdir.X) && dir.Y != Math.Sign(dashdir.Y)))
            return false;
        
        Rectangle bounds = player.SceneAs<Level>().Bounds;
        if (player.Left + dir.X < bounds.Left || player.Right + dir.X > bounds.Right || player.Top + dir.Y < bounds.Top || player.Bottom + dir.Y > bounds.Bottom)
            return false;

        // Check if we're colliding with a DreamTunnelBlocker
        if (player.IsDreamTunnelDashBlocked(player.Position + dir))
            return false;

        Solid solid = null;

        // Check for dream blocks first, then for solids
        DreamTunnelDangerous dangerous = DreamTunnelDangerous.CollideFirst(player, player.Position + dir);
        if (dangerous is not null)
        {
            Vector2 side = new(Math.Abs(dir.Y), Math.Abs(dir.X));

            bool dashedIntoDreamBlock = true;
            bool checkNegative = dir.X != 0f ? player.Speed.Y <= 0f : player.Speed.X <= 0f;
            bool checkPositive = dir.X != 0f ? player.Speed.Y >= 0f : player.Speed.X >= 0f;
            if (checkNegative)
            {
                for (int i = -1; i >= -Player.DashCornerCorrection; i--)
                {
                    Vector2 at = player.Position + dir + (side * i);
                    if (DreamTunnelDangerous.CollideCheck(player, at) || (solid = DreamTunnelDangerous.CollideFirstNonDangerous(player, at)) is null)
                        continue;
                        
                    player.Position += side * i;
                    dashedIntoDreamBlock = false;
                    goto CheckDreamBlock;
                }
            }

            if (checkPositive)
            {
                for (int i = 1; i <= Player.DashCornerCorrection; i++)
                {
                    Vector2 at = player.Position + dir + (side * i);
                    if (DreamTunnelDangerous.CollideCheck(player, at) || (solid = DreamTunnelDangerous.CollideFirstNonDangerous(player, at)) is null)
                        continue;
                        
                    player.Position += side * i;
                    dashedIntoDreamBlock = false;
                    goto CheckDreamBlock;
                }
            }

        CheckDreamBlock:
            if (dashedIntoDreamBlock)
            {
                if (dangerous.Active)
                    player.Die(-dir);

                component.DreamTunnelDashAttacking = false;
                component.OverrideDreamDashCheck = true;
                return false;
            }
        }

        solid ??= DreamTunnelDangerous.CollideFirstNonDangerous(player, player.Position + dir);
        // Don't dash through if it has a dash collide action, unless it's a farewell floaty block
        // or a DashBlock which is only breakable by a Kevin (canDash is false)
        if (solid is not null && (!CommunalHelperModule.Settings.DreamTunnelIgnoreCollidables
            || solid.OnDashCollide is null
            || solid is FloatySpaceBlock
            || (solid is DashBlock b && !DynamicData.For(b).Get<bool>("canDash"))))
        {
            player.StateMachine.State = St.DreamTunnelDash;
            solid.Components.GetAll<DreamTunnelInteraction>().ToList().ForEach(i => i.OnPlayerEnter(player));
            component.Solid = solid;
            
            player.dashAttackTimer = 0f;
            player.gliderBoostTimer = 0f;
            return true;
        }
        if (solid is DashSwitch)
        {
            // Why is this necessary? Good question!
            // I don't know the answer, but for some reason, Celeste registers
            // dashing into a button upwards as colliding with both the button and the
            // tile behind it. In order to prevent this from making you dash
            // through a wall after hitting a button, I disable the dream
            // tunnel after hitting a button.
            component.DreamTunnelDashAttacking = false;
        }
        return false;
    }

    private static (bool blockDreamTunnelDashes, bool blockDreamDashes) GetBlockerConfiguration(this Player player, Vector2 position)
    {
        bool blockDreamTunnelDashes = false;
        bool blockDreamDashes = false;

        foreach (Entity e in player.CollideAll<DreamTunnelBlocker>(position, DreamTunnelBlockers))
        {
            if (e is DreamTunnelBlocker { BlockDreamTunnelDashes: true })
                blockDreamTunnelDashes = true;
            if (e is DreamTunnelBlocker { BlockDreamDashes: true })
                blockDreamDashes = true;

            if (blockDreamTunnelDashes && blockDreamDashes)
                break;
        }

        return (blockDreamTunnelDashes, blockDreamDashes);
    }

    private static bool IsDreamTunnelDashBlocked(this Player player, Vector2 position)
    {
        return player.CollideAll<DreamTunnelBlocker>(position, DreamTunnelBlockers)
            .Any(e => e is DreamTunnelBlocker { BlockDreamTunnelDashes: true });
    }

    private static bool IsDreamDashBlocked(this Player player, Vector2 position)
    {
        return player.CollideAll<DreamTunnelBlocker>(position, DreamTunnelBlockers)
            .Any(e => e is DreamTunnelBlocker { BlockDreamDashes: true });
    }

    #endregion
}
