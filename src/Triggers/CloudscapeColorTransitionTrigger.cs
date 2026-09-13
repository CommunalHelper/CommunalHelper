using Celeste.Mod.CommunalHelper.Backdrops;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Triggers;

[CustomEntity("CommunalHelper/CloudscapeColorTransitionTrigger")]
public class CloudscapeColorTransitionTrigger : Trigger
{
    public enum Mode
    {
        TopToBottom,
        BottomToTop,
        LeftToRight,
        RightToLeft,
    }

    private readonly string tag;
    private readonly Mode mode;
    private readonly Color[] from, to;
    private readonly Color bgFrom, bgTo;

    private float prevLerp;

    public CloudscapeColorTransitionTrigger(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        tag = data.Attr("tag");
        mode = data.Enum("mode", Mode.LeftToRight);
        from = data.Attr("colorsFrom", "6d8adaff,aea0c1ff,d9cbbcff")
                   .Split(',')
                   .Select(str => Util.HexToColorWithAlphaNonPremultiplied(str.Trim()))
                   .ToArray();
        to = data.Attr("colorsTo", "ff0000ff,00ff00ff,0000ffff")
                 .Split(',')
                 .Select(str => Util.HexToColorWithAlphaNonPremultiplied(str.Trim()))
                 .ToArray();
        bgFrom = Util.HexToColorWithAlphaNonPremultiplied(data.Attr("bgFrom", "4f9af7ff").Trim());
        bgTo = Util.HexToColorWithAlphaNonPremultiplied(data.Attr("bgTo", "000000ff").Trim());
    }

    public override void OnStay(Player player)
    {
        base.OnStay(player);

        if (Scene is not Level level)
            return;

        float lerp = mode switch
        {
            Mode.LeftToRight => Calc.ClampedMap(player.X, Left, Right),
            Mode.RightToLeft => 1f - Calc.ClampedMap(player.X, Left, Right),
            Mode.TopToBottom => Calc.ClampedMap(player.Y, Top, Bottom),
            Mode.BottomToTop => 1f - Calc.ClampedMap(player.Y, Top, Bottom),
            _ => 0f,
        };

        if (prevLerp == lerp)
            return;
        prevLerp = lerp;

        Color bg = Color.Lerp(bgFrom, bgTo, lerp);

        if (string.IsNullOrEmpty(tag))
        {
            level.Background.Get<Cloudscape>()?.ConfigureColors(bg, from, to, lerp);
        }
        else
        {
            foreach (Cloudscape cloudscape in level.Background.GetEach<Cloudscape>(tag).Concat(level.Foreground.GetEach<Cloudscape>(tag)))
                cloudscape.ConfigureColors(bg, from, to, lerp);
        }
    }
}
