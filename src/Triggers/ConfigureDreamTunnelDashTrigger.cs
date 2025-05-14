using Celeste.Mod.CommunalHelper.Components;
using System.Linq;
using static Celeste.Mod.CommunalHelper.DashStates.DreamTunnelDash;

namespace Celeste.Mod.CommunalHelper.Triggers;

[CustomEntity("CommunalHelper/ConfigureDreamTunnelDashTrigger")]
[Tracked]
public class ConfigureDreamTunnelDashTrigger : Trigger
{ 
    private readonly DreamTunnelDashConfiguration options;
    
    private readonly bool revertOnLeave;
    private readonly bool revertOnDeath;
    private readonly bool onlyOnce;

    private struct DreamTunnelDashConfigurationChanges
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
    
    private DreamTunnelDashConfigurationChanges? changesNeededToRevert;

    public ConfigureDreamTunnelDashTrigger(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        revertOnLeave = data.Bool("revertOnLeave", false);
        revertOnDeath = data.Bool("revertOnDeath", true);
        onlyOnce = data.Bool("onlyOnce", false);
        
        options = new DreamTunnelDashConfiguration()
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

        string flag = data.Attr("flag");
        if (!string.IsNullOrEmpty(flag)) {
            Add(new FlagToggleComponent(flag, data.Bool("flagInverted")));
        }
    }
    
    private static DreamTunnelDashConfigurationChanges CalculateChangesNeededToRevert(DreamTunnelDashConfiguration from, DreamTunnelDashConfiguration to)
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

    private static DreamTunnelDashConfiguration RevertChanges(DreamTunnelDashConfiguration current, DreamTunnelDashConfigurationChanges? changesNeededToRevert)
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

    public override void OnEnter(Player player)
    {
        changesNeededToRevert = CalculateChangesNeededToRevert(CommunalHelperModule.Session.CurrentDreamTunnelDashConfiguration, options);
        CommunalHelperModule.Session.CurrentDreamTunnelDashConfiguration = options;
        
        if (onlyOnce) {
            RemoveSelf();
        }
    }

    public override void OnLeave(Player player)
    {
        if (revertOnLeave && !player.Dead)
        {
            CommunalHelperModule.Session.CurrentDreamTunnelDashConfiguration = RevertChanges(CommunalHelperModule.Session.CurrentDreamTunnelDashConfiguration, changesNeededToRevert);
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
        foreach (ConfigureDreamTunnelDashTrigger trigger in player.SceneAs<Level>().Tracker.GetEntities<ConfigureDreamTunnelDashTrigger>()
                                                                  .Cast<ConfigureDreamTunnelDashTrigger>()
                                                                  .Where(trigger => trigger.revertOnDeath && trigger.changesNeededToRevert is not null))
        {
            CommunalHelperModule.Session.CurrentDreamTunnelDashConfiguration = RevertChanges(CommunalHelperModule.Session.CurrentDreamTunnelDashConfiguration, trigger.changesNeededToRevert);
        }
    }
    
    #endregion
}
