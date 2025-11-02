using Celeste.Mod.CommunalHelper.States;
using static Celeste.Mod.CommunalHelper.States.Elytra;

namespace Celeste.Mod.CommunalHelper.Triggers;

[CustomEntity("CommunalHelper/ConfigureElytraTrigger")]
[TrackedAs(typeof(AbstractConfigureStateTrigger<ElytraOptions>))]
internal class ConfigureElytraTrigger : AbstractConfigureStateTrigger<ElytraOptions>
{
    public ConfigureElytraTrigger(EntityData data, Vector2 offset)
        : base(data, offset)
    { }

    protected override ElytraOptions GetConfiguredOptions(EntityData data)
        => new()
        {
            Allow = data.Bool("allow", false),
            Infinite = data.Bool("infinite", false),
            Configuration = new ElytraConfiguration
            {
                DisableReverseVerticalMomentum = data.Bool("disableReverseVerticalMomentum", false),
                UpdateCooldownInEveryState = data.Bool("updateCooldownInEveryState", false),
            },
        };

    protected override ElytraOptions GetCurrentOptions(Player player)
        => new()
        {
            Allow = CommunalHelperModule.Session.CanDeployElytra,
            Infinite = player.HasInfiniteElytra(),
            Configuration = CommunalHelperModule.Session.CurrentElytraConfiguration,
        };
    protected override void SaveCurrentOptions(Player player, ElytraOptions options)
    {
        CommunalHelperModule.Session.CanDeployElytra = options.Allow;
        player.SetInfiniteElytra(options.Infinite);
        CommunalHelperModule.Session.CurrentElytraConfiguration = options.Configuration;
    }

    protected override ElytraOptions GetPerRoomOptions()
        => new()
        {
            Allow = CommunalHelperModule.Session.PerRoomCanDeployElytra,
            Infinite = CommunalHelperModule.Session.PerRoomHasInfiniteElytra,
            Configuration = CommunalHelperModule.Session.PerRoomElytraConfiguration,
        };
    protected override void SavePerRoomOptions(ElytraOptions options)
    {
        CommunalHelperModule.Session.PerRoomCanDeployElytra = options.Allow;
        CommunalHelperModule.Session.PerRoomHasInfiniteElytra = options.Infinite;
        CommunalHelperModule.Session.PerRoomElytraConfiguration = options.Configuration;
    }
}

internal struct ElytraOptions
{
    public bool Allow;
    public bool Infinite;
    public ElytraConfiguration Configuration;
}
