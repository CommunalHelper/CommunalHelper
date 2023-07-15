using Celeste.Mod.CommunalHelper.States;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/ElytraRefillRing")]
public class ElytraRefillRing : ElytraRing
{
    public override string TraversalSFX => CustomSFX.game_elytra_rings_refill;

    public ElytraRefillRing(EntityData data, Vector2 offset)
        : this(
            data.Position + offset,
            data.Nodes[0] + offset
        )
    { }

    public ElytraRefillRing(Vector2 a, Vector2 b)
        : base(a, b, Color.Lime)
    { }

    public override void OnPlayerTraversal(Player player, int sign, bool shake = true)
    {
        base.OnPlayerTraversal(player, sign);
        player.RefillElytra();
    }

    public override void Render()
    {
        base.Render();

        GFX.Game["objects/CommunalHelper/elytraRing/dot"]
           .DrawCentered(Position, Color.LimeGreen * 0.5f, 6f + (float) Math.Sin(Scene.TimeActive * 3), MathHelper.PiOver4 + Direction.Angle());
    }
}
