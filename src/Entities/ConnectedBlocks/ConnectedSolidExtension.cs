using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper;

[CustomEntity("CommunalHelper/SolidExtension")]
[Tracked(false)]
public class SolidExtension : Solid
{
    public bool HasGroup { get; private set; }

    public bool HasHitbox { get; private set; }

    public SolidExtension(EntityData data, Vector2 offset)
        : this(data.Position + offset, data.Width, data.Height, data.Bool("collidable", true)) { }

    public SolidExtension(Vector2 position, int width, int height, bool collidable)
        : base(position, width, height, safe: false)
    {
        HasHitbox = collidable;
    }

    public void AddToGroupAndFindChildren(SolidExtension from, ConnectedSolid master, List<SolidExtension> extensions)
    {
        from.HasGroup = true;
        extensions.Add(from);

        if (from.X < master.GroupBoundsMin.X)
        {
            master.GroupBoundsMin.X = (int)from.X;
        }
        if (from.Y < master.GroupBoundsMin.Y)
        {
            master.GroupBoundsMin.Y = (int)from.Y;
        }
        if (from.Right > master.GroupBoundsMax.X)
        {
            master.GroupBoundsMax.X = (int)from.Right;
        }
        if (from.Bottom > master.GroupBoundsMax.Y)
        {
            master.GroupBoundsMax.Y = (int)from.Bottom;
        }

        foreach (SolidExtension extention in Scene.Tracker.GetEntities<SolidExtension>())
        {
            if (!extention.HasGroup &&
                (Scene.CollideCheck(new Rectangle((int)from.X - 1, (int)from.Y, (int)from.Width + 2, (int)from.Height), extention) ||
                Scene.CollideCheck(new Rectangle((int)from.X, (int)from.Y - 1, (int)from.Width, (int)from.Height + 2), extention) ||
                Scene.CollideCheck(new Rectangle((int)master.X - 1, (int)master.Y, (int)master.Width + 2, (int)master.Height), extention) ||
                Scene.CollideCheck(new Rectangle((int)master.X, (int)master.Y - 1, (int)master.Width, (int)master.Height + 2), extention)))
            {
                AddToGroupAndFindChildren(extention, master, extensions);
            }
        }
    }

    public override void Update()
    {
        base.Update();
        if (!HasGroup)
        {
            RemoveSelf();
        }
    }
}
