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
    }

    private static readonly Color defaultColor = Calc.HexToColor("ffae11");

    private readonly Vector2[] nodes;

    private readonly Color color;
    private readonly RespawnBehavior respawnBehavior;

    private readonly HashSet<ConnectedMoveBlock> blocks = new();

    public MoveBlockGroup(EntityData data, Vector2 offset)
        : this(data.NodesOffset(offset), data.HexColor("color", defaultColor), data.Enum("respawnBehavior", RespawnBehavior.Simultaneous))
    { }

    public MoveBlockGroup(Vector2[] nodes, Color color, RespawnBehavior respawnBehavior = RespawnBehavior.Simultaneous)
    {
        this.nodes = nodes;

        this.color = color;
        this.respawnBehavior = respawnBehavior;
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);

        // Find affected Connected Move Blocks
        foreach (Vector2 node in nodes)
        {
            Rectangle hitbox = new((int)node.X - 4, (int)node.Y - 4, 8, 8);
            foreach (ConnectedMoveBlock m in scene.CollideAll<ConnectedMoveBlock>(hitbox))
                blocks.Add(m);
        }

        foreach (ConnectedMoveBlock block in blocks)
            block.SetGroup(this);
    }

    public void Trigger()
    {
        foreach (ConnectedMoveBlock block in blocks)
            if (block.State == MovementState.Idling)
                block.GroupSignal = true;
    }

    public bool CanRespawn()
    {
        if (respawnBehavior == RespawnBehavior.Immediate)
            return true;

        foreach (ConnectedMoveBlock block in blocks)
            if (!block.CheckGroupRespawn)
                return false;
        return true;
    }

    public void Respawn()
    {
        if (respawnBehavior != RespawnBehavior.Simultaneous)
            return;

        foreach (ConnectedMoveBlock block in blocks)
            block.CheckGroupRespawn = false;
    }
}
