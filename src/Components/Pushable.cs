using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Collections.Generic;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Components;

[Tracked]
public class Pushable : Component
{
    public new Solid Entity => EntityAs<Solid>();

    public MoveActionType MoveActions { get; set; } = MoveActionType.Both;
    public float MaxPushSpeed { get; set; } = -1f;

    public Action<int, MoveActionType> OnPush { get; set; }
    public Func<int, MoveActionType, bool> PushCheck { get; set; }

    public Pushable() : base(true, false)
    {
    }

    public override void Added(Entity entity)
    {
        // need to call base first to ensure we balance Add/Remove
        base.Added(entity);

        if (entity is not Solid)
        {
            Util.Log(LogLevel.Warn, $"Attempted to add {nameof(Pushable)} to a non-Solid ({entity.GetType().Name})");
            RemoveSelf();
        }
    }

    public override void Update()
    {
        if (Entity?.Scene is not Level level || // if no level
            Input.MoveX.Value == 0 || Input.MoveY != 0 || // or not moving left or right
            level.Tracker.GetEntity<Player>() is not { } player || // or no player
            !player.OnGround()) // or player not on ground
            return;

        var climbing = Entity.GetPlayerClimbing() == player;

        // find out which action we're taking
        var moveAction = Input.MoveX.Value == (int) player.Facing ? MoveActionType.Push : MoveActionType.Pull;
        var collideAt = player.Position + Vector2.UnitX * (int) player.Facing;

        // verify that we're allowed to move it
        if (!MoveActions.HasFlag(moveAction) ||
            PushCheck is not null && !PushCheck(Input.MoveX.Value, moveAction))
            return;

        // if we're trying to pull
        if (moveAction == MoveActionType.Pull)
        {
            // we can't pull if we're not grabbing or if it would squish us
            if (!climbing || player.CollideCheck<Solid>(player.Position + Vector2.UnitX * Input.MoveX.Value))
                return;

            // find all the pushables we're facing
            var pushables = player.CollideAllByComponent<Pushable>(collideAt);
            // find the lowest one that supports pulling
            var lowest = LowestPullable(pushables);
            // if it's not us, then bail
            if (lowest != this)
                return;
        }
        // if we're trying to push
        else
        {
            // if we need to hold grab and aren't, bail
            if (CommunalHelperModule.Settings.RequireGrabToPush && !climbing)
                return;

            // we can only push if this pushable is the ONLY solid we collide with
            var solids = player.CollideAll<Solid>(collideAt);
            if (solids.Count != 1 || solids[0] != Entity)
                return;
        }

        // move the entity, maintaining player liftspeed
        // Player.HoldingMaxRun is private and == 70f
        var speed = (MaxPushSpeed < 0 ? 70f : Math.Min(Player.MaxRun, MaxPushSpeed)) * Input.MoveX.Value;
        var liftSpeed = player.LiftSpeed;
        var collide = MoveHNoSquish(level, speed * Engine.DeltaTime, player);
        player.LiftSpeed = liftSpeed;

        if (!collide)
            OnPush?.Invoke(Input.MoveX.Value, moveAction);
    }

    private bool MoveHNoSquish(Level level, float moveH, Player player)
    {
        // pushing
        if (Math.Sign(moveH) == (int) player.Facing)
        {
            var climbing = Entity.GetPlayerClimbing() == player;
            var collide = Entity.MoveHCollideSolidsAndBounds(level, moveH, false);
            if (!collide && !climbing)
                player.MoveH(moveH);
            return collide;
        }

        // pulling
        var sign = Math.Sign(moveH);
        var abs = Math.Abs(moveH);
        while (abs > 0)
        {
            var moveAmount = sign * Math.Min(abs, 1);
            // prevent squishing
            if (player.CollideCheck<Solid>(player.Position + Vector2.UnitX * moveAmount))
                return true;

            if (Entity.MoveHCollideSolids(moveAmount, false))
                return true;

            abs--;
        }

        return false;
    }

    private static Pushable LowestPullable(List<Pushable> pushables)
    {
        var lowestPos = float.MinValue;
        Pushable lowest = null;
        foreach (var p in pushables)
        {
            if (p.Entity is null || !p.MoveActions.HasFlag(MoveActionType.Pull) || p.Entity.Bottom <= lowestPos)
                continue;

            lowestPos = p.Entity.Bottom;
            lowest = p;
        }

        return lowest;
    }

    // ReSharper disable once InconsistentNaming
    private static IDetour hook_Player_orig_UpdateSprite;

    internal static void Load()
    {
        var origUpdateSprite = typeof(Player).GetMethod("orig_UpdateSprite", BindingFlags.Instance | BindingFlags.NonPublic);
        hook_Player_orig_UpdateSprite = new ILHook(origUpdateSprite, Player_orig_UpdateSprite);
    }

    internal static void Unload()
    {
        hook_Player_orig_UpdateSprite?.Dispose();
        hook_Player_orig_UpdateSprite = null;
    }

    private static void Player_orig_UpdateSprite(ILContext il)
    {
        var cursor = new ILCursor(il);

        if (!cursor.TryGotoNext(i => i.MatchCallvirt<StateMachine>("get_State"),
            i => i.MatchLdcI4(1)))
            return;

        if (!cursor.TryGotoNext(i => i.MatchLdarg(0),
            i => i.MatchLdfld<Player>("lastClimbMove")))
            return;

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Func<Player, bool>>(self =>
        {
            if (Input.MoveX.Value == 0 ||
                !self.OnGround() ||
                self.CollideFirstByComponent<Pushable>(self.Position + Vector2.UnitX * (int) self.Facing) is null)
                return false;

            if (self.Sprite.CurrentAnimationID != "push")
                self.Sprite.Play("push");

            var reverseMultiplier = Input.MoveX.Value * (int) self.Facing;

            self.Sprite.Rate = (self.SceneAs<Level>()?.InSpace == true ? 0.5f : 1f) * reverseMultiplier;

            return true;
        });
        cursor.Emit(OpCodes.Brfalse_S, cursor.Next);
        cursor.Emit(OpCodes.Ret);
    }

    [Flags]
    public enum MoveActionType
    {
        None = 0,

        Push = 1 << 0,
        Pull = 1 << 1,

        Both = Push | Pull,
    }
}
