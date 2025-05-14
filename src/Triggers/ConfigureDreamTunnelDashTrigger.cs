using static Celeste.Mod.CommunalHelper.DashStates.DreamTunnelDash;

namespace Celeste.Mod.CommunalHelper.Triggers;

[CustomEntity("CommunalHelper/ConfigureDreamTunnelDashTrigger")]
[TrackedAs(typeof(AbstractConfigureStateTrigger<DreamTunnelDashConfiguration, DreamTunnelDashConfigurationChanges>))]
public class ConfigureDreamTunnelDashTrigger : AbstractConfigureStateTrigger<DreamTunnelDashConfiguration, DreamTunnelDashConfigurationChanges>
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
            BounceOnCollision = data.Bool("bounceOnCollision", false)
        };
    protected override DreamTunnelDashConfiguration GetCurrentOptions(Player player)
        => CommunalHelperModule.Session.CurrentDreamTunnelDashConfiguration;
    protected override void SaveOptions(Player player, DreamTunnelDashConfiguration options)
        => CommunalHelperModule.Session.CurrentDreamTunnelDashConfiguration = options;
    
    protected override DreamTunnelDashConfigurationChanges CalculateChangesNeededToRevert(DreamTunnelDashConfiguration from, DreamTunnelDashConfiguration to)
        => new()
        {
            AllowRedirect = to.AllowRedirect == from.AllowRedirect ? null : from.AllowRedirect,
            AllowSameDirectionRedirect = to.AllowSameDirectionRedirect == from.AllowSameDirectionRedirect ? null : from.AllowSameDirectionRedirect,
            SameDirectionSpeedMultiplier = to.SameDirectionSpeedMultiplier == from.SameDirectionSpeedMultiplier ? null : from.SameDirectionSpeedMultiplier,
            UseEntryDirection = to.UseEntryDirection == from.UseEntryDirection ? null : from.UseEntryDirection,
            SpeedConfiguration = to.SpeedConfiguration == from.SpeedConfiguration ? null : from.SpeedConfiguration,
            CustomSpeed = to.CustomSpeed == from.CustomSpeed ? null : from.CustomSpeed,
            AllowDashCancels = to.AllowDashCancels == from.AllowDashCancels ? null : from.AllowDashCancels,
            RedirectConsumesNormalDash = to.RedirectConsumesNormalDash == from.RedirectConsumesNormalDash ? null : from.RedirectConsumesNormalDash,
            AllowTransitions = to.AllowTransitions == from.AllowTransitions ? null : from.AllowTransitions,
            BounceOnCollision = to.BounceOnCollision == from.BounceOnCollision ? null : from.BounceOnCollision
        };
    protected override DreamTunnelDashConfiguration RevertChanges(DreamTunnelDashConfiguration current, DreamTunnelDashConfigurationChanges? changesNeededToRevert)
        => new()
        {
            AllowRedirect = changesNeededToRevert?.AllowRedirect ?? current.AllowRedirect,
            AllowSameDirectionRedirect = changesNeededToRevert?.AllowSameDirectionRedirect ?? current.AllowSameDirectionRedirect,
            SameDirectionSpeedMultiplier = changesNeededToRevert?.SameDirectionSpeedMultiplier ?? current.SameDirectionSpeedMultiplier,
            UseEntryDirection = changesNeededToRevert?.UseEntryDirection ?? current.UseEntryDirection,
            SpeedConfiguration = changesNeededToRevert?.SpeedConfiguration ?? current.SpeedConfiguration,
            CustomSpeed = changesNeededToRevert?.CustomSpeed ?? current.CustomSpeed,
            AllowDashCancels = changesNeededToRevert?.AllowDashCancels ?? current.AllowDashCancels,
            RedirectConsumesNormalDash = changesNeededToRevert?.RedirectConsumesNormalDash ?? current.RedirectConsumesNormalDash,
            AllowTransitions = changesNeededToRevert?.AllowTransitions ?? current.AllowTransitions,
            BounceOnCollision = changesNeededToRevert?.BounceOnCollision ?? current.BounceOnCollision
        };
}

public struct DreamTunnelDashConfigurationChanges
{
    public bool? AllowRedirect;
    public bool? AllowSameDirectionRedirect;
    public float? SameDirectionSpeedMultiplier;
    public bool? UseEntryDirection;
    public SpeedConfiguration? SpeedConfiguration;
    public float? CustomSpeed;
    public bool? AllowDashCancels;
    public bool? RedirectConsumesNormalDash;
    public bool? AllowTransitions;
    public bool? BounceOnCollision;
}
