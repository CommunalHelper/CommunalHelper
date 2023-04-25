using System.Collections.Generic;
using static Celeste.Mod.CommunalHelper.ConnectedMoveBlock;

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

    private readonly List<ConnectedMoveBlock> blocks = new();

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
            Rectangle hitbox = new((int)node.X - 4, (int)node.Y - 4, 8, 8);
            foreach (ConnectedMoveBlock m in scene.CollideAll<ConnectedMoveBlock>(hitbox))
            {
                if (!blocks.Contains(m))
                {
                    blocks.Add(m);
                    m.Group = this;
                }
            }
        }
    }

    public void Trigger()
    {
        foreach (ConnectedMoveBlock block in blocks)
            if (block.State == MovementState.Idling)
                block.GroupSignal = true;
    }

    public bool CanRespawn(ConnectedMoveBlock block)
    {
        switch (respawnBehavior)
        {
            case RespawnBehavior.Immediate:
                return true;

            case RespawnBehavior.Simultaneous:
                foreach (ConnectedMoveBlock m in blocks)
                    if (!m.CheckGroupRespawn)
                        return false;
                return true;

            case RespawnBehavior.Sequential:
                int blockIndex = blocks.IndexOf(block);
                for (int i = 0; i < blockIndex; i++)
                    if (blocks[i].State != MovementState.Idling)
                        return false;
                return true;
        }

        return false;
    }

    public void Respawn()
    {
        if (respawnBehavior != RespawnBehavior.Simultaneous)
            return;

        foreach (ConnectedMoveBlock block in blocks)
            block.CheckGroupRespawn = false;
    }
}
