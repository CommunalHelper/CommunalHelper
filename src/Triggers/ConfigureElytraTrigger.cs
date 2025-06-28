using Celeste.Mod.CommunalHelper.States;
using static Celeste.Mod.CommunalHelper.States.Elytra;

namespace Celeste.Mod.CommunalHelper.Triggers;

[CustomEntity("CommunalHelper/ConfigureElytraTrigger")]
[TrackedAs(typeof(AbstractConfigureStateTrigger<ElytraOptions, ElytraOptionsChanges>))]
internal class ConfigureElytraTrigger : AbstractConfigureStateTrigger<ElytraOptions, ElytraOptionsChanges>
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
                DisableReverseVerticalMomentum = data.Bool("disableReverseVerticalMomentum"),
            },
        };
    protected override ElytraOptions GetCurrentOptions(Player player)
        => new()
        {
            Allow = CommunalHelperModule.Session.CanDeployElytra,
            Infinite = player.HasInfiniteElytra(),
            Configuration = CommunalHelperModule.Session.CurrentElytraConfiguration,
        };
    protected override void SaveOptions(Player player, ElytraOptions options)
    {
        CommunalHelperModule.Session.CanDeployElytra = options.Allow;
        player.SetInfiniteElytra(options.Infinite);
        CommunalHelperModule.Session.CurrentElytraConfiguration = options.Configuration;
    }

    protected override ElytraOptionsChanges CalculateChangesNeededToRevert(ElytraOptions from, ElytraOptions to)
        => new()
        {
            NewAllow = to.Allow == from.Allow ? null : from.Allow,
            NewInfinite = to.Infinite == from.Infinite ? null : from.Infinite,
            NewConfiguration = new ElytraOptionsChanges.ElytraConfigurationChanges
            {
                NewDisableReverseVerticalMomentum = to.Configuration.DisableReverseVerticalMomentum == from.Configuration.DisableReverseVerticalMomentum ? null : from.Configuration.DisableReverseVerticalMomentum
            }
        };
    protected override ElytraOptions RevertChanges(ElytraOptions current, ElytraOptionsChanges? changesNeededToRevert)
        => new()
        {
            Allow = changesNeededToRevert?.NewAllow ?? current.Allow,
            Infinite = changesNeededToRevert?.NewInfinite ?? current.Infinite,
            Configuration = new ElytraConfiguration
            {
                DisableReverseVerticalMomentum = changesNeededToRevert?.NewConfiguration.NewDisableReverseVerticalMomentum ?? current.Configuration.DisableReverseVerticalMomentum
            }
        };
}

public struct ElytraOptions
{
    public bool Allow;
    public bool Infinite;
    public ElytraConfiguration Configuration;
}

public struct ElytraOptionsChanges
{
    public struct ElytraConfigurationChanges
    {
        public bool? NewDisableReverseVerticalMomentum;
    }

    public bool? NewAllow;
    public bool? NewInfinite;
    public ElytraConfigurationChanges NewConfiguration;
}
