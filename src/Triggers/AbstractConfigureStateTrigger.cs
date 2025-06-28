using Celeste.Mod.CommunalHelper.Components;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Triggers;

// unfortunately, due to this class being generic, `[Tracked(true)]` doesn't really do anything here
// all children of this class need to be marked as `[TrackedAs(typeof(AbstractConfigureStateTrigger<TOptions, TChanges>))]` in order to be tracked properly
[Tracked(true)]
public abstract class AbstractConfigureStateTrigger<TOptions, TChanges> : Trigger
    where TOptions : struct where TChanges : struct
{
    private readonly TOptions options;
    private TChanges? changesNeededToRevert;

    private readonly bool revertOnLeave;
    private readonly bool revertOnDeath;
    private readonly bool onlyOnce;

    public AbstractConfigureStateTrigger(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        revertOnLeave = data.Bool("revertOnLeave", false);
        revertOnDeath = data.Bool("revertOnDeath", true);
        onlyOnce = data.Bool("onlyOnce", false);

        options = GetConfiguredOptions(data);

        string flag = data.Attr("flag");
        if (!string.IsNullOrEmpty(flag))
        {
            Add(new FlagToggleComponent(flag, data.Bool("flagInverted")));
        }
    }

    protected abstract TOptions GetConfiguredOptions(EntityData data);
    protected abstract TOptions GetCurrentOptions(Player player);
    protected abstract void SaveOptions(Player player, TOptions options);

    protected abstract TChanges CalculateChangesNeededToRevert(TOptions from, TOptions to);
    protected abstract TOptions RevertChanges(TOptions current, TChanges? changesNeededToRevert);

    public override void OnEnter(Player player)
    {
        changesNeededToRevert = CalculateChangesNeededToRevert(GetCurrentOptions(player), options);
        SaveOptions(player, options);

        if (onlyOnce)
        {
            RemoveSelf();
        }
    }

    public override void OnLeave(Player player)
    {
        if (revertOnLeave && !player.Dead)
        {
            SaveOptions(player, RevertChanges(options, changesNeededToRevert));
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
        foreach (AbstractConfigureStateTrigger<TOptions, TChanges> trigger in player.SceneAs<Level>().Tracker.GetEntities<AbstractConfigureStateTrigger<TOptions, TChanges>>()
                                                         .Cast<AbstractConfigureStateTrigger<TOptions, TChanges>>()
                                                         .Where(trigger => trigger.revertOnDeath && trigger.changesNeededToRevert is not null))
        {
            trigger.SaveOptions(player, trigger.RevertChanges(trigger.options, trigger.changesNeededToRevert));
        }
    }

    #endregion
}
