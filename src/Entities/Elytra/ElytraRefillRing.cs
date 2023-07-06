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
        : base(a, b)
    { }

    public override void OnPlayerTraversal(Player player)
    {
        base.OnPlayerTraversal(player);

        player.RefillElytra();

        Level level = Scene as Level;
        level.Shake(0.1f);
        Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);

        // particles
        // sound
        Audio.Play(SFX.game_gen_diamond_touch, Position);
    }
}
