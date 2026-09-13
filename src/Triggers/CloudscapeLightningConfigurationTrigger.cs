using Celeste.Mod.CommunalHelper.Backdrops;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Triggers;

[CustomEntity("CommunalHelper/CloudscapeLightningConfigurationTrigger")]
public class CloudscapeLightningConfigurationTrigger : Trigger
{
    private readonly string tag;
    private readonly bool enable;
    private readonly Color[] lightningColors;
    private readonly Color lightningFlashColor;
    private readonly float lightningMinDelay, lightningMaxDelay;
    private readonly float lightningMinDuration, lightningMaxDuration;
    private readonly float lightningIntensity;

    public CloudscapeLightningConfigurationTrigger(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        tag = data.Attr("tag");
        enable = data.Bool("enable", true);
        lightningColors = data.Attr("lightningColors", "384bc8,7a50d0,c84ddd,3397e2")
                              .Split(',')
                              .Select(str => Calc.HexToColor(str.Trim()))
                              .ToArray();
        lightningFlashColor = Calc.HexToColor(data.Attr("lightningFlashColor").Trim());
        lightningMinDelay = MathHelper.Max(data.Float("lightningMinDelay", 5.0f), 0);
        lightningMaxDelay = MathHelper.Max(data.Float("lightningMaxDelay", 40.0f), 0);
        lightningMinDuration = MathHelper.Max(data.Float("lightningMinDuration", 0.5f), 0);
        lightningMaxDuration = MathHelper.Max(data.Float("lightningMaxDuration", 1.0f), 0);
        lightningIntensity = MathHelper.Clamp(data.Float("lightningIntensity", 0.5f), 0f, 1f);
    }

    public override void OnEnter(Player player)
    {
        base.OnEnter(player);

        if (Scene is not Level level)
            return;

        if (string.IsNullOrEmpty(tag))
        {
            level.Background.Get<Cloudscape>()?.ConfigureLightning(
                enable, lightningColors, lightningFlashColor,
                lightningMinDelay, lightningMaxDelay,
                lightningMinDuration, lightningMaxDuration,
                lightningIntensity
            );
        }
        else
        {
            foreach (Cloudscape cloudscape in level.Background.GetEach<Cloudscape>(tag).Concat(level.Foreground.GetEach<Cloudscape>(tag)))
                cloudscape.ConfigureLightning(
                    enable, lightningColors, lightningFlashColor,
                    lightningMinDelay, lightningMaxDelay,
                    lightningMinDuration, lightningMaxDuration,
                    lightningIntensity
                );
        }
    }
}
