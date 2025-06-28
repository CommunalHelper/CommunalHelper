using Celeste.Mod.CommunalHelper.Imports;

namespace Celeste.Mod.CommunalHelper.Components;

[TrackedAs(typeof(Holdable))]
internal class DreamHoldable : Holdable
{
    private readonly DreamDashCollider dreamDashCollider;
    public bool AllowDreamDash
    {
        get => dreamDashCollider.Active;
        set
        {
            bool changed = dreamDashCollider.Active != value;
            dreamDashCollider.Active = value;

            if (changed && value)
            {
                onActivate?.Invoke();
            }
            else if (changed && !value)
            {
                onDeactivate?.Invoke();
            }
        }
    }

    private readonly Action onActivate, onDeactivate;

    private Component gravityListener;
    private readonly float colliderTop, invertedColliderTop;

    public DreamHoldable(Collider dreamDashCollider, float cannotHoldDelay = 0.1f, Action onActivate = null, Action onDeactivate = null)
        : base(cannotHoldDelay)
    {
        this.dreamDashCollider = new DreamDashCollider(dreamDashCollider, OnDreamDashEnter, OnDreamDashExit);
        this.onActivate = onActivate;
        this.onDeactivate = onDeactivate;

        colliderTop = dreamDashCollider.Top;
        invertedColliderTop = -dreamDashCollider.Bottom;
    }

    private void OnDreamDashEnter(Player player)
    {
        // Prevents a crash caused by entering the feather fly state while dream dashing through a dream holdable.
        player.starFlyBloom ??= new(new Vector2(0f, -6f), 0f, 16f) { Visible = false };
    }

    private void OnDreamDashExit(Player player)
    {
        AllowDreamDash = false;
        if (Input.GrabCheck && player.DashDir.Y <= 0 && player.Holding is null)
        {
            // force-allow pickup
            player.minHoldTimer = 0f;
            cannotHoldTimer = 0f;

            if (player.Pickup(this))
            {
                player.StateMachine.State = Player.StPickup;
            }
        }
    }

    public override void Added(Entity entity)
    {
        base.Added(entity);

        entity.Add(dreamDashCollider);

        if (entity is Actor actor)
        {
            gravityListener = GravityHelper.CreateGravityListener?.Invoke(actor, (_, value, _) =>
            {
                bool inverted = value == (int) GravityType.Inverted;
                dreamDashCollider.Collider.Top = inverted ? invertedColliderTop : colliderTop;
            });
            if (gravityListener is not null)
                entity.Add(gravityListener);
        }
    }

    public override void Removed(Entity entity)
    {
        base.Removed(entity);

        entity.Remove(dreamDashCollider);

        if (gravityListener is not null)
            entity.Remove(gravityListener);
    }

    public override void Update()
    {
        base.Update();

        if ((Holder is null && ((Entity as Actor)?.OnGround() ?? false)) || (Holder is Player player && player.OnGround()))
        {
            AllowDreamDash = true;
        }
    }

    #region Hooks

    internal static void Load()
    {
        On.Celeste.Holdable.Check += Holdable_Check;
        On.Celeste.Holdable.HitSpring += Holdable_HitSpring;

        On.Celeste.Player.NormalUpdate += Player_NormalUpdate;
        On.Celeste.Player.StartDash += Player_StartDash;
        On.Celeste.Player.SideBounce += Player_SideBounce;
    }

    internal static void Unload()
    {
        On.Celeste.Holdable.Check -= Holdable_Check;
        On.Celeste.Holdable.HitSpring -= Holdable_HitSpring;

        On.Celeste.Player.NormalUpdate -= Player_NormalUpdate;
        On.Celeste.Player.StartDash -= Player_StartDash;
        On.Celeste.Player.SideBounce -= Player_SideBounce;
    }

    private static bool Holdable_Check(On.Celeste.Holdable.orig_Check orig, Holdable self, Player player)
    {
        return (self is DreamHoldable holdable && holdable.AllowDreamDash && player.DashAttacking) ? false : orig(self, player);
    }

    private static bool Holdable_HitSpring(On.Celeste.Holdable.orig_HitSpring orig, Holdable self, Spring spring)
    {
        if (self is DreamHoldable holdable)
            holdable.AllowDreamDash = true;
        return orig(self, spring);
    }

    private static int Player_NormalUpdate(On.Celeste.Player.orig_NormalUpdate orig, Player self)
    {
        int result = orig(self);

        if (self.Holding is DreamHoldable holdable && holdable.AllowDreamDash && self.CanDash && Input.MoveY.Value == -1f)
        {
            self.Drop();
            return self.StartDash();
        }

        return result;
    }

    private static int Player_StartDash(On.Celeste.Player.orig_StartDash orig, Player self)
    {
        if (self.Holding is DreamHoldable && Input.MoveY.Value == -1f)
            self.Drop();
        return orig(self);
    }

    private static bool Player_SideBounce(On.Celeste.Player.orig_SideBounce orig, Player self, int dir, float fromX, float fromY)
    {
        bool result = orig(self, dir, fromX, fromY);
        if (result && self.Holding is DreamHoldable holdable)
            holdable.AllowDreamDash = true;
        return result;
    }

    #endregion
}
