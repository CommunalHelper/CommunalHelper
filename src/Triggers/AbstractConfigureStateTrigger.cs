using Celeste.Mod.CommunalHelper.Components;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Triggers;

// unfortunately, due to this class being generic, `[Tracked(true)]` doesn't really do anything here
// all children of this class need to be marked as `[TrackedAs(typeof(AbstractConfigureStateTrigger<T>))]` in order to be tracked properly
[Tracked(true)]
internal abstract class AbstractConfigureStateTrigger<T> : Trigger
{
    private readonly T options;

    private readonly bool revertOnLeave;
    private readonly bool revertOnDeath;
    private readonly bool onlyOnce;

    public AbstractConfigureStateTrigger(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        revertOnLeave = data.Bool("revertOnLeave");
        revertOnDeath = data.Bool("revertOnDeath");
        onlyOnce = data.Bool("onlyOnce");

        options = GetConfiguredOptions(data);

        string flag = data.Attr("flag");
        if (!string.IsNullOrEmpty(flag))
        {
            Add(new FlagToggleComponent(flag, data.Bool("flagInverted")));
        }
    }

    protected abstract T GetConfiguredOptions(EntityData data);
    
    protected abstract T GetCurrentOptions(Player player);
    protected abstract void SaveCurrentOptions(Player player, T options);
    
    protected abstract T GetPerRoomOptions();
    protected abstract void SavePerRoomOptions(T options);

    public override void OnEnter(Player player)
    {
        SaveCurrentOptions(player, options);
        if (!revertOnDeath && !revertOnLeave)
            SavePerRoomOptions(options);

        if (onlyOnce)
            RemoveSelf();
    }

    public override void OnLeave(Player player)
    {
        if (revertOnLeave && !player.Dead)
            SaveCurrentOptions(player, GetPerRoomOptions());
    }

    #region Hooks

    internal static void Load()
    {
        Everest.Events.Player.OnSpawn += ResetCurrentOptions;
        Everest.Events.Player.OnDie += ResetCurrentOptions;
        Everest.Events.Level.OnTransitionTo += OnTransitionTo;
    }

    internal static void Unload()
    {
        Everest.Events.Player.OnSpawn -= ResetCurrentOptions;
        Everest.Events.Player.OnDie -= ResetCurrentOptions;
        Everest.Events.Level.OnTransitionTo -= OnTransitionTo;
    }

    private static void ResetCurrentOptions(Player player)
    {
        if (player.Scene.Tracker.GetEntities<AbstractConfigureStateTrigger<T>>()
                                .Cast<AbstractConfigureStateTrigger<T>>()
                                .FirstOrDefault(t => t.revertOnDeath) is { } trigger)
            trigger.SaveCurrentOptions(player, trigger.GetPerRoomOptions());
    }

    private static void OnTransitionTo(Level level, LevelData next, Vector2 direction)
    {
        Player player = level.Tracker.GetEntity<Player>();
        if (player is null)
            return;
        
        if (level.Tracker.GetEntities<AbstractConfigureStateTrigger<T>>()
                         .Cast<AbstractConfigureStateTrigger<T>>()
                         .FirstOrDefault() is { } trigger) 
            trigger.SavePerRoomOptions(trigger.GetCurrentOptions(player));
    }

    #endregion
}
