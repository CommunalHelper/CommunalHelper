using Celeste.Mod.CommunalHelper.States;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/ElytraStopRing")]
public class ElytraStopRing : ElytraRing
{
    private readonly bool refill;

    public override bool PreserveTraversalOrder => false;

    public ElytraStopRing(EntityData data, Vector2 offset)
        : this(
            data.Position + offset,
            data.Nodes[0] + offset,
            data.Bool("refill", false)
        )
    { }

    public ElytraStopRing(Vector2 a, Vector2 b, bool refill = false)
        : base(a, b)
    {
        this.refill = refill;
    }

    public override void OnPlayerTraversal(Player player)
    {
        base.OnPlayerTraversal(player);

        if (refill)
            player.RefillElytra();

        Level level = Scene as Level;
        level.Shake(0.1f);
        Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);

        // particles
        // sound
        Audio.Play(SFX.game_06_feather_state_end, Position);

        player.StateMachine.State = Player.StNormal;
    }
}
