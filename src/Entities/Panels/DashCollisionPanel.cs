namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/DashCollisionPanel")]
public class DashCollisionPanel : AbstractPanel
{
    public DashCollisionResults dashCollisionOverride;
    public bool overrideCollision;

    public DashCollisionPanel(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        overrideCollision = data.Bool("overrideCollision");
        dashCollisionOverride = data.Enum("dashCollideResult", DashCollisionResults.NormalCollision);
    }

    protected override DashCollisionResults OnDashCollide(DashCollision orig, Player player, Vector2 dir)
    {
        if (CheckDashCollision(player, dir))
        {
            if (!overrideCollision)
                base.OnDashCollide(orig, player, dir);
            return dashCollisionOverride;
        }
        return base.OnDashCollide(orig, player, dir);
    }
}
