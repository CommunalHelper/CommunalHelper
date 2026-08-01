using Directions = Celeste.Spikes.Directions;

namespace Celeste.Mod.CommunalHelper.Entities;

// Note: Inheriting from this class will cause the OnDashCollide function of the attached platform to become non-null if it was previously null
// (returning DashCollisionResults.NormalCollision as a default)
// It may cause side effects such as a red bubble not exiting when colliding with the platform.
public abstract class AbstractDashCollisionPanel : AbstractPanel
{
    protected AbstractDashCollisionPanel(EntityData data, Vector2 offset) : base(data, offset)
    {
    }

    protected AbstractDashCollisionPanel(Vector2 position, float size, Directions orientation, bool overrideAllowStaticMovers) : base(position, size, orientation, overrideAllowStaticMovers)
    {
    }

    protected override void OnAttach(Platform platform)
    {
        base.OnAttach(platform);
        platform.OnDashCollide = DelegateHelper.ApplyDashCollisionHook(platform.OnDashCollide, OnDashCollide);
    }

    /// <summary>
    /// Does not check dash direction or direct collision with the Panel.
    /// Use CheckDashCollision as needed.
    /// </summary>
    protected virtual DashCollisionResults OnDashCollide(DashCollision orig, Player player, Vector2 dir)
    {
        return orig(player, dir);
    }
    
    protected bool CheckDashCollision(Player player, Vector2 dir)
    {
        switch (Orientation)
        {
            case Directions.Up when dir.Y > 0:
            case Directions.Down when dir.Y < 0:
            case Directions.Left when dir.X > 0:
            case Directions.Right when dir.X < 0:
                return player.CollideCheck(this, player.Position + dir);
        }
        return false;
    }
    
    public override void Removed(Scene scene)
    {
        if (staticMover.Platform is not null && (staticMover.Platform.TagCheck(Tags.Global) || staticMover.Platform.TagCheck(Tags.Persistent)))
            DelegateHelper.RemoveDashCollisionHook(staticMover.Platform.OnDashCollide, OnDashCollide);

        base.Removed(scene);
    }
}
