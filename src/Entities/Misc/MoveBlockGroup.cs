using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/MoveBlockGroup")]
public class MoveBlockGroup : Entity
{
    private static readonly Color defaultColor = Calc.HexToColor("ffae11");

    private readonly Vector2[] nodes;
    private readonly Color color;

    private readonly HashSet<ConnectedMoveBlock> blocks = new();

    public MoveBlockGroup(EntityData data, Vector2 offset)
        : this(data.NodesOffset(offset), data.HexColor("color", defaultColor))
    { }

    public MoveBlockGroup(Vector2[] nodes, Color color)
    {
        this.nodes = nodes;
        this.color = color;
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
            block.GroupSignal = true;
    }
}
