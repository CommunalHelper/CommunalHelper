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
        : base(a, b, Color.Tomato)
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
        Audio.Play(SFX.game_06_feather_state_end, Position);
        TravelEffects();

        Celeste.Freeze(0.05f);

        player.StateMachine.State = Player.StNormal;
    }

    public override void Render()
    {
        base.Render();

        GFX.Game["objects/CommunalHelper/elytraRing/x"]
           .DrawCentered(Position, Color.Tomato * 0.5f, 6f + (float)Math.Sin(Scene.TimeActive * 3), MathHelper.PiOver4 + Direction.Angle());
    }
}
