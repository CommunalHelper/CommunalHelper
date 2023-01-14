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

    private readonly Mode mode;
    private readonly Color[] from, to;
    private readonly Color bgFrom, bgTo;

    private float oldLerp;

    public CloudscapeColorTransitionTrigger(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        mode = data.Enum("mode", Mode.LeftToRight);
        from = data.Attr("colorsFrom", "6d8ada,aea0c1,d9cbbc")
                   .Split(',')
                   .Select(str => Calc.HexToColor(str.Trim()))
                   .ToArray();
        to = data.Attr("colorsTo", "ff0000,00ff00,0000ff")
                 .Split(',')
                 .Select(str => Calc.HexToColor(str.Trim()))
                 .ToArray();
        bgFrom = data.HexColor("bgFrom");
        bgTo = data.HexColor("bgTo");
    }

    public override void OnStay(Player player)
    {
        base.OnStay(player);

        float lerp = mode switch
        {
            Mode.LeftToRight => Calc.ClampedMap(player.X, Left, Right),
            Mode.RightToLeft => 1f - Calc.ClampedMap(player.X, Left, Right),
            Mode.TopToBottom => Calc.ClampedMap(player.Y, Top, Bottom),
            Mode.BottomToTop => 1f - Calc.ClampedMap(player.Y, Top, Bottom),
            _ => 0f,
        };

        if (oldLerp == lerp)
            return;
        oldLerp = lerp;

        Color bg = Color.Lerp(bgFrom, bgTo, lerp);

        ((Scene as Level).Background.Backdrops.FirstOrDefault(b => b is Cloudscape) as Cloudscape)
            ?.ConfigureColors(bg, from, to, lerp);
    }
}
