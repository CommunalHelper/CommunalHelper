using static Celeste.Mod.CommunalHelper.DashStates.DreamTunnelDash;

namespace Celeste.Mod.CommunalHelper.Triggers;

[CustomEntity("CommunalHelper/ConfigureDreamTunnelDashTrigger")]
[TrackedAs(typeof(AbstractConfigureStateTrigger<DreamTunnelDashConfiguration, DreamTunnelDashChanges>))]
internal class ConfigureDreamTunnelDashTrigger : AbstractConfigureStateTrigger<DreamTunnelDashConfiguration, ConfigureDreamTunnelDashTrigger.DreamTunnelDashChanges>
{
    public struct DreamTunnelDashChanges
    {
        public bool? AllowRedirect;
        public bool? AllowSameDirectionRedirect;
        public float? SameDirectionSpeedMultiplier;
        public bool? UseEntryDirection;
        public DreamTunnelDashConfiguration.SpeedConfigurations? SpeedConfiguration;
        public float? CustomSpeed;
        public bool? AllowDashCancels;
        public bool? RedirectConsumesNormalDash;
        public bool? AllowTransitions;
        public bool? BounceOnCollision;
        public bool? RespectBoosters;
    }
    
    public ConfigureDreamTunnelDashTrigger(EntityData data, Vector2 offset)
        : base(data, offset)
    { }

    protected override DreamTunnelDashChanges GetConfiguredChanges(EntityData data)
        => new()
        {
            AllowRedirect = BoolNullable(data, "allowRedirect", false),
            AllowSameDirectionRedirect = BoolNullable(data, "allowSameDirectionRedirect", false),
            SameDirectionSpeedMultiplier = data.Float("sameDirectionSpeedMultiplier", 1f),
            UseEntryDirection = BoolNullable(data, "useEntryDirection", false),
            SpeedConfiguration = IntNullable(data, "speedConfiguration", 0) is { } value ? (DreamTunnelDashConfiguration.SpeedConfigurations) value : null, // `as` doesn't work
            CustomSpeed = FloatNullable(data, "customSpeed", 0f),
            AllowDashCancels = BoolNullable(data, "allowDashCancels", false),
            RedirectConsumesNormalDash = BoolNullable(data, "redirectConsumesNormalDash", false),
            AllowTransitions = BoolNullable(data, "allowTransitions", false),
            BounceOnCollision = BoolNullable(data, "bounceOnCollision", false),
            RespectBoosters = BoolNullable(data, "respectBoosters", false)
        };
    // type safety is great except when it makes me do shit like this
    protected override DreamTunnelDashConfiguration ApplyChanges(DreamTunnelDashConfiguration options, DreamTunnelDashChanges changes)
        => new()
        {
            AllowRedirect = changes.AllowRedirect ?? options.AllowRedirect,
            AllowSameDirectionRedirect = changes.AllowSameDirectionRedirect ?? options.AllowSameDirectionRedirect,
            SameDirectionSpeedMultiplier = changes.SameDirectionSpeedMultiplier ?? options.SameDirectionSpeedMultiplier,
            UseEntryDirection = changes.UseEntryDirection ?? options.UseEntryDirection,
            SpeedConfiguration = changes.SpeedConfiguration ?? options.SpeedConfiguration,
            CustomSpeed = changes.CustomSpeed ?? options.CustomSpeed,
            AllowDashCancels = changes.AllowDashCancels ?? options.AllowDashCancels,
            RedirectConsumesNormalDash = changes.RedirectConsumesNormalDash ?? options.RedirectConsumesNormalDash,
            AllowTransitions = changes.AllowTransitions ?? options.AllowTransitions,
            BounceOnCollision = changes.BounceOnCollision ?? options.BounceOnCollision,
            RespectBoosters = changes.RespectBoosters ?? options.RespectBoosters
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
