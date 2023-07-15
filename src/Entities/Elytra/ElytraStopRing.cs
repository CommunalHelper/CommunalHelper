using Celeste.Mod.CommunalHelper.States;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/ElytraStopRing")]
public class ElytraStopRing : ElytraRing
{
    private readonly bool refill;

    public override bool PreserveTraversalOrder => false;
    public override string TraversalSFX => CustomSFX.game_elytra_rings_stop;

    public ElytraStopRing(EntityData data, Vector2 offset)
        : this(
            data.Position + offset,
            data.Nodes[0] + offset,
            data.Bool("refill", false)
        )
    { }

    public ElytraStopRing(Vector2 a, Vector2 b, bool refill = false)
        : base(a, b, Color.Tomato)
    {
        this.refill = refill;
    }

    public override void OnPlayerTraversal(Player player, int sign, bool shake = true)
    {
        base.OnPlayerTraversal(player, sign);
        if (refill)
            player.RefillElytra();

        player.StateMachine.State = Player.StNormal;
    }

    public override void Render()
    {
        base.Render();

        GFX.Game["objects/CommunalHelper/elytraRing/x"]
           .DrawCentered(Position, Color.Tomato * 0.5f, 3f + (float)Math.Sin(Scene.TimeActive * 3) / 2f, MathHelper.PiOver4 + Direction.Angle());
    }
}
