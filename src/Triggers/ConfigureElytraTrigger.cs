using Celeste.Mod.CommunalHelper.States;
using static Celeste.Mod.CommunalHelper.States.Elytra;

namespace Celeste.Mod.CommunalHelper.Triggers;

[CustomEntity("CommunalHelper/ConfigureElytraTrigger")]
[TrackedAs(typeof(AbstractConfigureStateTrigger<ElytraOptions, ElytraChanges>))]
internal class ConfigureElytraTrigger : AbstractConfigureStateTrigger<ConfigureElytraTrigger.ElytraOptions, ConfigureElytraTrigger.ElytraChanges>
{
    // grrr i don't like how this is handled
    public struct ElytraOptions
    {
        public bool Allow;
        public bool Infinite;
        public ElytraConfiguration Configuration;
    }
    public struct ElytraChanges
    {
        public bool? Allow;
        public bool? Infinite;
        public bool? DisableReverseVerticalMomentum;
        public bool? UpdateCooldownInEveryState;
    }
    
    public ConfigureElytraTrigger(EntityData data, Vector2 offset)
        : base(data, offset)
    { }

    protected override ElytraChanges GetConfiguredChanges(EntityData data)
        => new()
        {
            Allow = BoolNullable(data, "allow", false),
            Infinite = BoolNullable(data, "infinite", false),
            DisableReverseVerticalMomentum = BoolNullable(data, "disableReverseVerticalMomentum", false),
            UpdateCooldownInEveryState = BoolNullable(data, "updateCooldownInEveryState", false)
        };
    protected override ElytraOptions ApplyChanges(ElytraOptions options, ElytraChanges changes)
        => new()
        {
            Allow = changes.Allow ?? options.Allow,
            Infinite = changes.Infinite ?? options.Infinite,
            Configuration = new ElytraConfiguration()
            {
                DisableReverseVerticalMomentum = changes.DisableReverseVerticalMomentum ?? options.Configuration.DisableReverseVerticalMomentum,
                UpdateCooldownInEveryState = changes.UpdateCooldownInEveryState ?? options.Configuration.UpdateCooldownInEveryState
            }
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
