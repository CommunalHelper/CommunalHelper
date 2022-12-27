using Microsoft.Xna.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.Utils;
using System;

namespace Celeste.Mod.CommunalHelper.Entities;

[Tracked]
internal class DreamDashCollider : Component
{
    public static readonly Color ActiveColor = Color.Teal;
    public static readonly Color InactiveColor = Calc.HexToColor("044f63"); // darker teal

    public Collider Collider;
    public DreamBlockDummy Dummy;

    public Action<Player> OnExit;

    public DreamDashCollider(Collider collider, Action<Player> onExit_player = null)
        : base(active: true, visible: false)
    {
        Collider = collider;
        Dummy = new DreamBlockDummy(Entity);
        OnExit = onExit_player;
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
        {
            player.StateMachine.State = Player.StDreamDash;
        }
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
        self.GetData()[Player_canEnterDreamDashCollider] = true;
    }

    private static void Player_DreamDashEnd(On.Celeste.Player.orig_DreamDashEnd orig, Player self)
    {
        DynData<Player> playerData = self.GetData();
        if ((DreamBlock)playerData["dreamBlock"] is DreamBlockDummy dummy)
        {
            foreach (DreamDashCollider collider in dummy.Entity.Components.GetAll<DreamDashCollider>())
            {
                playerData[Player_canEnterDreamDashCollider] = false;
                collider.OnExit?.Invoke(self);
            }
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
                {
                    if (collider.Check(self))
                    {
                        return collider.Dummy;
                    }
                }
                return dreamBlock;
            });
        }
    }

    #endregion
}
