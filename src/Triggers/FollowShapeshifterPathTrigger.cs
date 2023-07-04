using Celeste.Mod.CommunalHelper.Entities;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Triggers;

[CustomEntity("CommunalHelper/FollowShapeshifterPathTrigger")]
public class FollowShapeshifterPathTrigger : Trigger
{
    private readonly bool once;
    private readonly int pathID, shapeshifterID;

    public FollowShapeshifterPathTrigger(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        once = data.Bool("once", true);
        pathID = data.Int("pathID", -1);
        shapeshifterID = data.Int("shapeshifterID", 0);
    }

    public override void OnEnter(Player player)
    {
        base.OnEnter(player);

        if (once)
            Collidable = false;

        ShapeshifterPath path = pathID >= 0
            ? Scene.Tracker.GetEntities<ShapeshifterPath>()
                           .Cast<ShapeshifterPath>()
                           .FirstOrDefault(sp => sp.ID == pathID)
            : null;

        Scene.Tracker.GetEntities<Shapeshifter>()
                     .Cast<Shapeshifter>()
                     .FirstOrDefault(ss => ss.ID == shapeshifterID)
                     ?.FollowPath(path);
    }
}
