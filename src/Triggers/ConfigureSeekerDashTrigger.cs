using static Celeste.Mod.CommunalHelper.DashStates.SeekerDash;

namespace Celeste.Mod.CommunalHelper.Triggers;

[CustomEntity("CommunalHelper/ConfigureSeekerDashTrigger")]
[TrackedAs(typeof(AbstractConfigureStateTrigger<SeekerDashConfiguration, SeekerDashConfigurationChanges>))]
public class ConfigureSeekerDashTrigger : AbstractConfigureStateTrigger<SeekerDashConfiguration, SeekerDashConfigurationChanges>
{
    public ConfigureSeekerDashTrigger(EntityData data, Vector2 offset)
        : base(data, offset)
    { }

    protected override SeekerDashConfiguration GetConfiguredOptions(EntityData data)
        => new()
        {
            RespectBoosters = data.Bool("respectBoosters", false)
        };
    protected override SeekerDashConfiguration GetCurrentOptions(Player player)
        => CommunalHelperModule.Session.CurrentSeekerDashConfiguration;
    protected override void SaveOptions(Player player, SeekerDashConfiguration options)
        => CommunalHelperModule.Session.CurrentSeekerDashConfiguration = options;

    protected override SeekerDashConfigurationChanges CalculateChangesNeededToRevert(SeekerDashConfiguration from, SeekerDashConfiguration to)
        => new()
        {
            NewRespectBoosters = to.RespectBoosters == from.RespectBoosters ? null : from.RespectBoosters
        };
    protected override SeekerDashConfiguration RevertChanges(SeekerDashConfiguration current, SeekerDashConfigurationChanges? changesNeededToRevert)
        => new()
        {
            RespectBoosters = changesNeededToRevert?.NewRespectBoosters ?? current.RespectBoosters
        };
}

public struct SeekerDashConfigurationChanges
{
    public bool? NewRespectBoosters;
}
