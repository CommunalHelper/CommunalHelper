using Celeste.Mod.CommunalHelper.Components;
using Celeste.Mod.CommunalHelper.States;
using System.Linq;
using static Celeste.Mod.CommunalHelper.States.Elytra;

namespace Celeste.Mod.CommunalHelper.Triggers;

[CustomEntity("CommunalHelper/ConfigureElytraTrigger")]
[Tracked]
public class ConfigureElytraTrigger : Trigger
{
    private readonly bool allow, infinite;
    private readonly ElytraConfiguration options;
    
    private readonly bool revertOnLeave;
    private readonly bool revertOnDeath;
    private readonly bool onlyOnce;
    
    private struct ElytraConfigurationChanges
    {
        public bool? Allow;
        public bool? Infinite;
        public bool? DisableReverseVerticalMomentum;
    }
    
    private ElytraConfigurationChanges? changesNeededToRevert;

    public ConfigureElytraTrigger(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        revertOnLeave = data.Bool("revertOnLeave", false);
        revertOnDeath = data.Bool("revertOnDeath", true);
        onlyOnce = data.Bool("onlyOnce", false);
        
        allow = data.Bool("allow", false);
        infinite = data.Bool("infinite", false);

        options = new ElytraConfiguration()
        {
            DisableReverseVerticalMomentum = data.Bool("disableReverseVerticalMomentum"),
        };
        
        string flag = data.Attr("flag");
        if (!string.IsNullOrEmpty(flag)) {
            Add(new FlagToggleComponent(flag, data.Bool("flagInverted")));
        }
    }
    
    private static ElytraConfigurationChanges CalculateChangesNeededToRevert((bool allow, bool infinite, ElytraConfiguration options) from, (bool allow, bool infinite, ElytraConfiguration options) to)
        => new()
        {
            Allow = to.allow == from.allow ? null : from.allow,
            Infinite = to.infinite == from.infinite ? null : from.infinite,
            DisableReverseVerticalMomentum = to.options.DisableReverseVerticalMomentum == from.options.DisableReverseVerticalMomentum ? null : from.options.DisableReverseVerticalMomentum
        };

    private static (bool, bool, ElytraConfiguration) RevertChanges((bool allow, bool infinite, ElytraConfiguration options) current, ElytraConfigurationChanges? changesNeededToRevert)
        => (changesNeededToRevert?.Allow ?? current.allow, changesNeededToRevert?.Infinite ?? current.infinite, new ElytraConfiguration()
        {
            DisableReverseVerticalMomentum = changesNeededToRevert?.DisableReverseVerticalMomentum ?? current.options.DisableReverseVerticalMomentum
        });

    private static void SaveChanges(Player player, bool allow, bool infinite, ElytraConfiguration options)
    {
        CommunalHelperModule.Session.CanDeployElytra = allow;
        CommunalHelperModule.Session.CurrentElytraConfiguration = options;
        player.SetInfiniteElytra(infinite);
    }

    public override void OnEnter(Player player)
    {
        changesNeededToRevert = CalculateChangesNeededToRevert(
            (CommunalHelperModule.Session.CanDeployElytra, player.HasInfiniteElytra(), CommunalHelperModule.Session.CurrentElytraConfiguration),
            (allow, infinite, options));
        SaveChanges(player, allow, infinite, options);
        
        if (onlyOnce) {
            RemoveSelf();
        }
    }
    
    public override void OnLeave(Player player)
    {
        if (revertOnLeave && !player.Dead)
        {
            (bool newAllow, bool newInfinite, ElytraConfiguration newOptions) = RevertChanges((allow, infinite, options), changesNeededToRevert);
            SaveChanges(player, newAllow, newInfinite, newOptions);
        }
    }
    
    #region Hooks

    internal static void Load()
    {
        Everest.Events.Player.OnDie += OnDie;
    }

    internal static void Unload()
    {
        Everest.Events.Player.OnDie -= OnDie;
    }

    private static void OnDie(Player player)
    {
        foreach (ConfigureElytraTrigger trigger in player.SceneAs<Level>().Tracker.GetEntities<ConfigureElytraTrigger>()
                                                                  .Cast<ConfigureElytraTrigger>()
                                                                  .Where(trigger => trigger.revertOnDeath && trigger.changesNeededToRevert is not null))
        {
            (bool newAllow, bool newInfinite, ElytraConfiguration newOptions) = RevertChanges((trigger.allow, trigger.infinite, trigger.options), trigger.changesNeededToRevert);
            SaveChanges(player, newAllow, newInfinite, newOptions);
        }
    }
    
    #endregion
}
