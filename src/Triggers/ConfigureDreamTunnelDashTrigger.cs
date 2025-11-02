using static Celeste.Mod.CommunalHelper.DashStates.DreamTunnelDash;

namespace Celeste.Mod.CommunalHelper.Triggers;

[CustomEntity("CommunalHelper/ConfigureDreamTunnelDashTrigger")]
[TrackedAs(typeof(AbstractConfigureStateTrigger<DreamTunnelDashConfiguration>))]
internal class ConfigureDreamTunnelDashTrigger : AbstractConfigureStateTrigger<DreamTunnelDashConfiguration>
{
    public ConfigureDreamTunnelDashTrigger(EntityData data, Vector2 offset)
        : base(data, offset)
    { }

    protected override DreamTunnelDashConfiguration GetConfiguredOptions(EntityData data)
        => new()
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
            BounceOnCollision = data.Bool("bounceOnCollision", false),
            RespectBoosters = data.Bool("respectBoosters", false)
        };
    
    protected override DreamTunnelDashConfiguration GetCurrentOptions(Player player)
        => CommunalHelperModule.Session.CurrentDreamTunnelDashConfiguration;
    protected override void SaveCurrentOptions(Player player, DreamTunnelDashConfiguration options)
        => CommunalHelperModule.Session.CurrentDreamTunnelDashConfiguration = options;
    
    protected override DreamTunnelDashConfiguration GetPerRoomOptions()
        => CommunalHelperModule.Session.PerRoomDreamTunnelDashConfiguration;
    protected override void SavePerRoomOptions(DreamTunnelDashConfiguration options)
        => CommunalHelperModule.Session.PerRoomDreamTunnelDashConfiguration = options;
}
