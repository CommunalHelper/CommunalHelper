using System.Linq;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/DreamSpriteColorController")]
[Tracked]
public class DreamSpriteColorController : Entity
{
    public readonly Color LineColor;
    public readonly Color BackColor;
    public readonly Color[] DreamColors;

    public DreamSpriteColorController(EntityData data, Vector2 offset) : base(data.Position + offset)
    {
        LineColor = data.HexColor("lineColor", Color.White);
        BackColor = data.HexColor("backColor", Color.Black);
        DreamColors = Enumerable.Range(0, 9)
            .Select(i => data.HexColor($"dreamColor{i}", CustomDreamBlock.DreamColors[i]))
            .ToArray();
    }
}