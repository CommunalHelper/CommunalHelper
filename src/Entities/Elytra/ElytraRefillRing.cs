using Celeste.Mod.CommunalHelper.States;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/ElytraRefillRing")]
public class ElytraRefillRing : ElytraRing
{
    public ElytraRefillRing(EntityData data, Vector2 offset)
        : this(
            data.Position + offset,
            data.Nodes[0] + offset
        )
    { }

    public ElytraRefillRing(Vector2 a, Vector2 b)
        : base(a, b, Color.Lime)
    { }

    public override void OnPlayerTraversal(Player player)
    {
        base.OnPlayerTraversal(player);

        player.RefillElytra();

        Level level = Scene as Level;
        level.Shake(0.1f);
        Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
        Audio.Play(SFX.game_gen_diamond_touch, Position);
        TravelEffects();

        Celeste.Freeze(0.05f);
    }

    public override void Render()
    {
        base.Render();

        GFX.Game["objects/CommunalHelper/elytraRing/dot"]
           .DrawCentered(Position, Color.LimeGreen * 0.5f, 12f + (float) Math.Sin(Scene.TimeActive * 3) * 2, MathHelper.PiOver4 + Direction.Angle());
    }
}
