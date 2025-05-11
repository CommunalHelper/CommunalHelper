namespace Celeste.Mod.CommunalHelper.Entities;

[Tracked]
[CustomEntity("CommunalHelper/DreamTunnelBlocker")]
public class DreamTunnelBlocker : Entity
{
    public bool BlockDreamTunnelDashes;
    public bool BlockDreamDashes;

    /// Blocking behavior handled in <see cref="DashStates.DreamTunnelDash.DreamTunnelDashCheck" />
    /// and <see cref="DashStates.DreamTunnelDash.Player_DreamDashCheck"/>
    public DreamTunnelBlocker(EntityData data, Vector2 offset) : base(data.Position + offset)
    {
        Collider = new Hitbox(data.Width, data.Height);
        Depth = Depths.FakeWalls + 2000;

        BlockDreamTunnelDashes = data.Bool("blockDreamTunnelDashes", true);
        BlockDreamDashes = data.Bool("blockDreamDashes", false);
    }
}
