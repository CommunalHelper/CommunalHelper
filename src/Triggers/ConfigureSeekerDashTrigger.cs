using static Celeste.Mod.CommunalHelper.DashStates.SeekerDash;

namespace Celeste.Mod.CommunalHelper.Triggers;

[CustomEntity("CommunalHelper/ConfigureSeekerDashTrigger")]
[TrackedAs(typeof(AbstractConfigureStateTrigger<SeekerDashConfiguration, SeekerDashChanges>))]
internal class ConfigureSeekerDashTrigger : AbstractConfigureStateTrigger<SeekerDashConfiguration, ConfigureSeekerDashTrigger.SeekerDashChanges>
{
    public struct SeekerDashChanges
    {
        public bool? RespectBoosters;
    }
    
    public ConfigureSeekerDashTrigger(EntityData data, Vector2 offset)
        : base(data, offset)
    { }

    protected override SeekerDashChanges GetConfiguredChanges(EntityData data)
        => new()
        {
            RespectBoosters = BoolNullable(data, "respectBoosters", false)
        };
    protected override SeekerDashConfiguration ApplyChanges(SeekerDashConfiguration options, SeekerDashChanges changes)
        => new()
        {
            RespectBoosters = changes.RespectBoosters ?? options.RespectBoosters,
        };

    protected override SeekerDashConfiguration GetCurrentOptions(Player player)
        => CommunalHelperModule.Session.CurrentSeekerDashConfiguration;
    protected override void SaveCurrentOptions(Player player, SeekerDashConfiguration options)
        => CommunalHelperModule.Session.CurrentSeekerDashConfiguration = options;
    
    protected override SeekerDashConfiguration GetPerRoomOptions()
        => CommunalHelperModule.Session.PerRoomSeekerDashConfiguration;
    protected override void SavePerRoomOptions(SeekerDashConfiguration options)
        => CommunalHelperModule.Session.PerRoomSeekerDashConfiguration = options;
}
