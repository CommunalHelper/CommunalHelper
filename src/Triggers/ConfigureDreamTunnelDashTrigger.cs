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
            NewAllowRedirect = to.AllowRedirect == from.AllowRedirect ? null : from.AllowRedirect,
            NewAllowSameDirectionRedirect = to.AllowSameDirectionRedirect == from.AllowSameDirectionRedirect ? null : from.AllowSameDirectionRedirect,
            NewSameDirectionSpeedMultiplier = to.SameDirectionSpeedMultiplier == from.SameDirectionSpeedMultiplier ? null : from.SameDirectionSpeedMultiplier,
            NewUseEntryDirection = to.UseEntryDirection == from.UseEntryDirection ? null : from.UseEntryDirection,
            NewSpeedConfiguration = to.SpeedConfiguration == from.SpeedConfiguration ? null : from.SpeedConfiguration,
            NewCustomSpeed = to.CustomSpeed == from.CustomSpeed ? null : from.CustomSpeed,
            NewAllowDashCancels = to.AllowDashCancels == from.AllowDashCancels ? null : from.AllowDashCancels,
            NewRedirectConsumesNormalDash = to.RedirectConsumesNormalDash == from.RedirectConsumesNormalDash ? null : from.RedirectConsumesNormalDash,
            NewAllowTransitions = to.AllowTransitions == from.AllowTransitions ? null : from.AllowTransitions,
            NewBounceOnCollision = to.BounceOnCollision == from.BounceOnCollision ? null : from.BounceOnCollision
        };
    protected override DreamTunnelDashConfiguration RevertChanges(DreamTunnelDashConfiguration current, DreamTunnelDashConfigurationChanges? changesNeededToRevert)
        => new()
        {
            AllowRedirect = changesNeededToRevert?.NewAllowRedirect ?? current.AllowRedirect,
            AllowSameDirectionRedirect = changesNeededToRevert?.NewAllowSameDirectionRedirect ?? current.AllowSameDirectionRedirect,
            SameDirectionSpeedMultiplier = changesNeededToRevert?.NewSameDirectionSpeedMultiplier ?? current.SameDirectionSpeedMultiplier,
            UseEntryDirection = changesNeededToRevert?.NewUseEntryDirection ?? current.UseEntryDirection,
            SpeedConfiguration = changesNeededToRevert?.NewSpeedConfiguration ?? current.SpeedConfiguration,
            CustomSpeed = changesNeededToRevert?.NewCustomSpeed ?? current.CustomSpeed,
            AllowDashCancels = changesNeededToRevert?.NewAllowDashCancels ?? current.AllowDashCancels,
            RedirectConsumesNormalDash = changesNeededToRevert?.NewRedirectConsumesNormalDash ?? current.RedirectConsumesNormalDash,
            AllowTransitions = changesNeededToRevert?.NewAllowTransitions ?? current.AllowTransitions,
            BounceOnCollision = changesNeededToRevert?.NewBounceOnCollision ?? current.BounceOnCollision
        };
}

public struct DreamTunnelDashConfigurationChanges
{
    public bool? NewAllowRedirect;
    public bool? NewAllowSameDirectionRedirect;
    public float? NewSameDirectionSpeedMultiplier;
    public bool? NewUseEntryDirection;
    public SpeedConfiguration? NewSpeedConfiguration;
    public float? NewCustomSpeed;
    public bool? NewAllowDashCancels;
    public bool? NewRedirectConsumesNormalDash;
    public bool? NewAllowTransitions;
    public bool? NewBounceOnCollision;
}
