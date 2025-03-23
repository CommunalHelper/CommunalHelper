using MonoMod.Utils;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Components;

[TrackedAs(typeof(Holdable))]
internal class DreamHoldable : Holdable
{
    private static readonly MethodInfo m_Player_Pickup = typeof(Player).GetMethod("Pickup", BindingFlags.NonPublic | BindingFlags.Instance);

    public readonly DreamDashCollider DreamDashCollider;
    public bool AllowDreamDash
    {
        get => DreamDashCollider.Active;
        set => DreamDashCollider.Active = value;
    }

    private readonly Action onActivate, onDeactivate;

    public DreamHoldable(Collider dreamDashCollider, Action onActivate = null, Action onDeactivate = null, float cannotHoldDelay = 0.1f)
        : base(cannotHoldDelay)
    {
        DreamDashCollider = new DreamDashCollider(dreamDashCollider, OnDreamDashEnter, OnDreamDashExit);
        this.onActivate = onActivate;
        this.onDeactivate = onDeactivate;
    }

    private void OnDreamDashEnter(Player player)
    {
        DynamicData data = DynamicData.For(player);

        BloomPoint starFlyBloom = data.Get<BloomPoint>("starFlyBloom");

        // Prevents a crash caused by entering the feather fly state while dream dashing through a dream holdable.
        starFlyBloom ??= new(new Vector2(0f, -6f), 0f, 16f) { Visible = false };

        data.Set("starFlyBloom", starFlyBloom);
    }

    public void OnDreamDashExit(Player player)
    {
        DisableDreamDash();
        if (Input.GrabCheck && player.DashDir.Y <= 0 && player.Holding == null)
        {
            // force-allow pickup
            player.GetData().Set("minHoldTimer", 0f);
            cannotHoldTimer = 0f;

            if ((bool) m_Player_Pickup.Invoke(player, new object[] { this }))
            {
                player.StateMachine.State = Player.StPickup;
            }
        }
    }

    private void EnableDreamDash()
    {
        if (AllowDreamDash)
            return;
        AllowDreamDash = true;
        onActivate?.Invoke();
    }

    private void DisableDreamDash()
    {
        if (!AllowDreamDash)
            return;
        AllowDreamDash = false;
        onDeactivate?.Invoke();
    }

    public override void Added(Entity entity)
    {
        base.Added(entity);
        entity.Add(DreamDashCollider);
    }

    public override void Removed(Entity entity)
    {
        base.Removed(entity);
        entity.Remove(DreamDashCollider);
    }

    public override void Update()
    {
        base.Update();

        if ((Holder is null && ((Entity as Actor)?.OnGround() ?? false)) || (Holder is Player player && player.OnGround()))
        {
            EnableDreamDash();
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
        On.Celeste.Holdable.HitSpring += Holdable_HitSpring;

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
            holdable.EnableDreamDash();
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
            holdable.EnableDreamDash();
        return result;
    }

    #endregion
}