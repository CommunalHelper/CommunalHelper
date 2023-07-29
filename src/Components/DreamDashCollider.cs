using Celeste.Mod.CommunalHelper.Entities;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;

namespace Celeste.Mod.CommunalHelper.Components;

[Tracked]
internal class DreamDashCollider : Component
{
    // Used as a dream block dummy, but which stores a DreamDashCollider property.
    // The reason for this is I didn't want to store a DreamDashCollider inside DreamBlockDummy.
    internal sealed class ColliderDummy : DreamBlockDummy
    {
        public DreamDashCollider DreamDashCollider { get; }
        public ColliderDummy(Entity entity, DreamDashCollider collider)
            : base(entity)
        {
            DreamDashCollider = collider;
        }
    }

    public static readonly Color ActiveColor = Color.Teal;
    public static readonly Color InactiveColor = Calc.HexToColor("044f63"); // darker teal

    public Collider Collider;
    public ColliderDummy Dummy;

    public Action<Player> OnEnter, OnExit;

    public DreamDashCollider(Collider collider, Action<Player> onEnter = null, Action<Player> onExit = null)
        : base(active: true, visible: false)
    {
        Collider = collider;
        Dummy = new(Entity, this);
        OnEnter = onEnter;
        OnExit = onExit;
    }

    public override void Added(Entity entity)
    {
        base.Added(entity);
        Dummy.Entity = entity;
    }

    /// <summary>
    /// Checks if the player is colliding with this component.
    /// </summary>
    /// <param name="player">The player instance.</param>
    private bool Check(Player player)
    {
        if (Active && Collider is not null && Entity != null &&
            player.GetData().Data.TryGetValue(Player_canEnterDreamDashCollider, out object canEnter) && canEnter.Equals(true))
        {

            Collider collider = Entity.Collider;

            Entity.Collider = Collider;
            bool check = player.CollideCheck(Entity);
            Entity.Collider = collider;

            return check;
        }
        return false;
    }

    public override void Update()
    {
        base.Update();
        if (Util.TryGetPlayer(out Player player) && Check(player) && player.DashAttacking && player.Speed != Vector2.Zero && player.StateMachine.State != Player.StDreamDash)
            player.StateMachine.State = Player.StDreamDash;
    }

    public override void DebugRender(Camera camera)
    {
        if (Collider != null)
        {
            Collider collider = Entity.Collider;

            Entity.Collider = Collider;
            Collider.Render(camera, Active ? ActiveColor : InactiveColor);
            Entity.Collider = collider;
        }
    }

    #region Hooks

    public static readonly string Player_canEnterDreamDashCollider = "communalHelperCanEnterDreamDashCollider";

    internal static void Load()
    {
        IL.Celeste.Player.DreamDashUpdate += Player_DreamDashUpdate;
        On.Celeste.Player.DashBegin += Player_DashBegin;
        On.Celeste.Player.DreamDashEnd += Player_DreamDashEnd;
    }

    internal static void Unload()
    {
        IL.Celeste.Player.DreamDashUpdate -= Player_DreamDashUpdate;
        On.Celeste.Player.DashBegin -= Player_DashBegin;
        On.Celeste.Player.DreamDashEnd -= Player_DreamDashEnd;
    }

    private static void Player_DashBegin(On.Celeste.Player.orig_DashBegin orig, Player self)
    {
        orig(self);
        self.GetData().Set(Player_canEnterDreamDashCollider, true);
    }

    private static void Player_DreamDashEnd(On.Celeste.Player.orig_DreamDashEnd orig, Player self)
    {
        DynamicData playerData = self.GetData();
        if (playerData.Get<DreamBlock>("dreamBlock") is DreamBlockDummy dummy)
            foreach (DreamDashCollider collider in dummy.Entity.Components.GetAll<DreamDashCollider>())
            {
                playerData.Set(Player_canEnterDreamDashCollider, false);
                collider.OnExit?.Invoke(self);
            }
        orig(self);
    }

    private static void Player_DreamDashUpdate(ILContext il)
    {
        ILCursor cursor = new(il);
        if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall(out MethodReference m) && m.Name == "CollideFirst"))
        {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<DreamBlock, Player, DreamBlock>>((dreamBlock, self) =>
            {
                foreach (DreamDashCollider collider in self.Scene.Tracker.GetComponents<DreamDashCollider>())
                    if (collider.Check(self))
                        return collider.Dummy;
                return dreamBlock;
            });
        }

        cursor.GotoNext(instr => instr.MatchStfld<Player>("dreamBlock"));

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Func<DreamBlock, Player, DreamBlock>>((dreamBlock, self) =>
        {
            DynamicData data = DynamicData.For(self);

            DreamBlock oldDreamBlock = data.Get<DreamBlock>("dreamBlock");
            if (dreamBlock != oldDreamBlock && dreamBlock is ColliderDummy dummy)
                dummy.DreamDashCollider.OnEnter?.Invoke(self);

            return dreamBlock;
        });
    }

    #endregion
}
