using Celeste.Mod.CommunalHelper.States;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/ElytraBoostRing")]
public class ElytraBoostRing : ElytraRing
{
    private readonly float speed, duration;
    private readonly bool refill;

    public override float Delay => 0.1f;

    public ElytraBoostRing(EntityData data, Vector2 offset)
        : this(
            data.Position + offset,
            data.Nodes[0] + offset,
            data.Float("speed", 240.0f),
            data.Float("duration", 0.5f),
            data.Bool("refill", false)
        )
    { }

    public ElytraBoostRing(Vector2 a, Vector2 b, float speed = 240.0f, float duration = 0.5f, bool refill = false)
        : base(a, b)
    {
        this.speed = speed;
        this.duration = duration;
        this.refill = refill;
    }

    public override void OnPlayerTraversal(Player player)
    {
        base.OnPlayerTraversal(player);

        if (Direction == Vector2.Zero)
            return;

        player.ElytraLaunch(Direction * speed, duration);
        if (refill)
            player.RefillElytra();

        Level level = Scene as Level;
        level.DirectionalShake(Direction);
        Input.Rumble(RumbleStrength.Strong, RumbleLength.Short);

        // particles
        // sound
        Audio.Play(SFX.game_06_feather_bubble_renew, Middle);

        Celeste.Freeze(0.05f);

        return;
    }
}
