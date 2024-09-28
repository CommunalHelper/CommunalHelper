using Celeste.Mod.CommunalHelper.Components;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/MoveBlockGroup")]
public class MoveBlockGroup : Entity
{
    public enum RespawnBehavior
    {
        Immediate,
        Simultaneous,
        Sequential,
    }

    private static readonly Color defaultColor = Calc.HexToColor("ffae11");

    private readonly Vector2[] nodes;

    public Color Color { get; }
    public bool SyncActivation { get; }
    private readonly RespawnBehavior respawnBehavior;

    private readonly List<GroupableMoveBlock> components = new();

    public MoveBlockGroup(EntityData data, Vector2 offset)
        : this(data.NodesOffset(offset), data.HexColor("color", defaultColor), data.Bool("syncActivation", true), data.Enum("respawnBehavior", RespawnBehavior.Simultaneous))
    { }

    public MoveBlockGroup(Vector2[] nodes, Color color, bool syncActivation = true, RespawnBehavior respawnBehavior = RespawnBehavior.Simultaneous)
    {
        this.nodes = nodes;

        this.Color = color;
        this.SyncActivation = syncActivation;
        this.respawnBehavior = respawnBehavior;
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);

        foreach (Vector2 node in nodes)
        {
            Rectangle hitbox = new((int) node.X - 4, (int) node.Y - 4, 8, 8);
            foreach (GroupableMoveBlock c in scene.Tracker.GetComponents<GroupableMoveBlock>())
            {
                // force CassetteMoveBlocks to be detected as they will be uncollidable until the cassette music starts
                if ((c.Entity is CassetteMoveBlock || c.Entity.Collidable) && c.Entity.CollideRect(hitbox) && !components.Contains(c))
                {
                    components.Add(c);
                    c.Group = this;
                }
            }
        }
    }

    public void Trigger()
    {
        foreach (GroupableMoveBlock component in components)
            if (component.State == GroupableMoveBlock.MovementState.Idling)
                component.GroupTriggerSignal = true;
    }

    public bool CanRespawn(GroupableMoveBlock groupable)
    {
        switch (respawnBehavior)
        {
            case RespawnBehavior.Immediate:
                return true;

            case RespawnBehavior.Simultaneous:
                foreach (GroupableMoveBlock c in components)
                    if (!c.WaitingForRespawn)
                        return false;
                return true;

            case RespawnBehavior.Sequential:
                int componentIndex = components.IndexOf(groupable);
                for (int i = 0; i < componentIndex; i++)
                    if (components[i].State != GroupableMoveBlock.MovementState.Idling)
                        return false;
                return true;
        }

        return false;
    }

    public void Respawn()
    {
        if (respawnBehavior != RespawnBehavior.Simultaneous)
            return;

        foreach (GroupableMoveBlock component in components)
            component.WaitingForRespawn = false;
    }
}
