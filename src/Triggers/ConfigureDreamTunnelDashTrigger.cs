using static Celeste.Mod.CommunalHelper.DashStates.DreamTunnelDash;

namespace Celeste.Mod.CommunalHelper.Triggers;

[CustomEntity("CommunalHelper/ConfigureDreamTunnelDashTrigger")]
public class ConfigureDreamTunnelDashTrigger : Trigger
{
    private readonly DreamTunnelDashConfiguration options;

    public ConfigureDreamTunnelDashTrigger(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        options = new DreamTunnelDashConfiguration()
        {
            AllowRedirect = data.Bool("allowRedirect", false),
            AllowSameDirectionRedirect = data.Bool("allowSameDirectionRedirect", false),
            SameDirectionSpeedMultiplier = data.Float("sameDirectionSpeedMultiplier", 1f),
            UseEntryDirection = data.Bool("useEntryDirection", false),
            SpeedConfiguration = (SpeedConfiguration) data.Int("speedConfiguration", 0),
            CustomSpeed = data.Float("customSpeed", 0f),
            AllowDashCancels = data.Bool("allowDashCancels", false),
            RedirectConsumesNormalDash = data.Bool("redirectConsumesNormalDash", false),
            AllowTransitions = data.Bool("allowTransitions", false),
        };
    }

    public override void OnEnter(Player player)
    {
        CommunalHelperModule.Session.CurrentDreamTunnelDashConfiguration = options;
    }
}
