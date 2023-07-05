using Celeste.Mod.CommunalHelper.Entities;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Triggers;

[CustomEntity("CommunalHelper/FollowShapeshifterPathTrigger")]
public class FollowShapeshifterPathTrigger : Trigger
{
    private readonly bool once;
    private readonly int pathID;
    private readonly int[] shapeshifterIDs;

    public FollowShapeshifterPathTrigger(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        once = data.Bool("once", true);
        pathID = data.Int("pathID", -1);

        HashSet<int> ids = new();
        foreach (string sub in data.Attr("shapeshifterID", string.Empty).Split(','))
        {
            string trimmed = sub.Trim();
            if (int.TryParse(trimmed, out int res))
                ids.Add(res);
            else
                Util.Log(LogLevel.Warn, $"invalid integer in comma-separated list of IDs: {trimmed}");
        }
        shapeshifterIDs = ids.ToArray();
    }

    public override void OnEnter(Player player)
    {
        base.OnEnter(player);

        if (once)
            Collidable = false;

        ShapeshifterPath path = pathID is -1
            ? null
            : Scene.Tracker.GetEntities<ShapeshifterPath>()
                           .Cast<ShapeshifterPath>()
                           .FirstOrDefault(sp => sp.ID == pathID);

        var blocks = Scene.Tracker.GetEntities<Shapeshifter>()
                                  .Cast<Shapeshifter>()
                                  .Where(ss => shapeshifterIDs.Contains(ss.ID));
        foreach (var block in blocks)
            block.FollowPath(path);
    }
}
