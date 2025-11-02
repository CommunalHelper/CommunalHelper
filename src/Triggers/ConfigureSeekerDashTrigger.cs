using static Celeste.Mod.CommunalHelper.DashStates.SeekerDash;

namespace Celeste.Mod.CommunalHelper.Triggers;

[CustomEntity("CommunalHelper/ConfigureSeekerDashTrigger")]
[TrackedAs(typeof(AbstractConfigureStateTrigger<SeekerDashConfiguration>))]
internal class ConfigureSeekerDashTrigger : AbstractConfigureStateTrigger<SeekerDashConfiguration>
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
    protected override void SaveCurrentOptions(Player player, SeekerDashConfiguration options)
        => CommunalHelperModule.Session.CurrentSeekerDashConfiguration = options;
    
    protected override SeekerDashConfiguration GetPerRoomOptions()
        => CommunalHelperModule.Session.PerRoomSeekerDashConfiguration;
    protected override void SavePerRoomOptions(SeekerDashConfiguration options)
        => CommunalHelperModule.Session.PerRoomSeekerDashConfiguration = options;
}
