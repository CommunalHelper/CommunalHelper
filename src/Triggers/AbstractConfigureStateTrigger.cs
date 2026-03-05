using Celeste.Mod.CommunalHelper.Components;
using System.ComponentModel;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Triggers;

// unfortunately, due to this class being generic, `[Tracked(true)]` doesn't really do anything here
// all children of this class need to be marked as `[TrackedAs(typeof(AbstractConfigureStateTrigger<TOptions, TChanges>))]` (with the correct type parameters) in order to be tracked properly
[Tracked(true)]
internal abstract class AbstractConfigureStateTrigger<TOptions, TChanges> : Trigger
{
    #region Utilities
    
    protected enum Actions
    {
        None,
        Enable,
        Disable
    }

    protected static bool? BoolNullable(EntityData data, string key, bool? defaultValue = null)
    {
        if (!(data.Values?.TryGetValue(key, out object obj) ?? false))
            return defaultValue;
        
        if (obj is bool result1)
            return result1;
        if (bool.TryParse(obj.ToString(), out bool result2))
            return result2;

        if (obj is Actions action1)
            return ToNullableBool(action1);
        if (Enum.TryParse(obj.ToString(), out Actions action2))
            return ToNullableBool(action2);
        
        return defaultValue;
        
        static bool? ToNullableBool(Actions action)
            => action switch
            {
                Actions.None => null,
                Actions.Enable => true,
                Actions.Disable => false,
                _ => null
            };
    }

    protected static int? IntNullable(EntityData data, string key, int? defaultValue = null)
    {
        if (!(data.Values?.TryGetValue(key, out object obj) ?? false))
            return defaultValue;
        
        if (obj is int result1)
            return result1;
        
        string s = obj.ToString();
        if (string.IsNullOrEmpty(s))
            return null;
        if (int.TryParse(s, out int result2))
            return result2;
        
        return defaultValue;
    }
    
    protected static float? FloatNullable(EntityData data, string key, float? defaultValue = null)
    {
        if (!(data.Values?.TryGetValue(key, out object obj) ?? false))
            return defaultValue;
        
        if (obj is float result1)
            return result1;
        
        string s = obj.ToString();
        if (string.IsNullOrEmpty(s))
            return null;
        if (float.TryParse(s, out float result2))
            return result2;
        
        return defaultValue;
    }
    
    #endregion
    
    private readonly TChanges changes;

    private readonly bool revertOnLeave;
    private readonly bool revertOnDeath;
    private readonly bool onlyOnce;

    public AbstractConfigureStateTrigger(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        revertOnLeave = data.Bool("revertOnLeave");
        revertOnDeath = data.Bool("revertOnDeath");
        onlyOnce = data.Bool("onlyOnce");

        changes = GetConfiguredChanges(data);

        string flag = data.Attr("flag");
        if (!string.IsNullOrEmpty(flag))
        {
            Add(new FlagToggleComponent(flag, data.Bool("flagInverted")));
        }
    }

    protected abstract TChanges GetConfiguredChanges(EntityData data);
    protected abstract TOptions ApplyChanges(TOptions options, TChanges changes);
    
    protected abstract TOptions GetCurrentOptions(Player player);
    protected abstract void SaveCurrentOptions(Player player, TOptions options);
    
    protected abstract TOptions GetPerRoomOptions();
    protected abstract void SavePerRoomOptions(TOptions options);

    public override void OnEnter(Player player)
    {
        TOptions newOptions = ApplyChanges(GetCurrentOptions(player), changes);
        SaveCurrentOptions(player, newOptions);
        if (!revertOnDeath && !revertOnLeave)
            SavePerRoomOptions(newOptions);

        if (onlyOnce)
            RemoveSelf();
    }

    public override void OnLeave(Player player)
    {
        if (revertOnLeave && !player.Dead) // hmm
            SaveCurrentOptions(player, GetPerRoomOptions());
    }

    #region Hooks

    internal static void Load()
    {
        Everest.Events.Player.OnSpawn += ResetCurrentOptions;
        Everest.Events.Player.OnDie += ResetCurrentOptions;
        Everest.Events.Level.OnLoadLevel += OnLoadLevel;
    }

    internal static void Unload()
    {
        Everest.Events.Player.OnSpawn -= ResetCurrentOptions;
        Everest.Events.Player.OnDie -= ResetCurrentOptions;
        Everest.Events.Level.OnLoadLevel -= OnLoadLevel;
    }

    private static void ResetCurrentOptions(Player player)
    {
        if (player.Scene.Tracker.GetEntities<AbstractConfigureStateTrigger<TOptions, TChanges>>()
                                .Cast<AbstractConfigureStateTrigger<TOptions, TChanges>>()
                                .FirstOrDefault(t => t.revertOnDeath) is { } trigger)
            trigger.SaveCurrentOptions(player, trigger.GetPerRoomOptions());
    }

    private static void OnLoadLevel(Level level, Player.IntroTypes introType, bool isFromLoader)
    {
        Player player = level.Tracker.GetEntity<Player>();
        if (player is null)
            return;
        
        if (level.Tracker.GetEntities<AbstractConfigureStateTrigger<TOptions, TChanges>>()
                         .Cast<AbstractConfigureStateTrigger<TOptions, TChanges>>()
                         .FirstOrDefault() is { } trigger) 
            trigger.SavePerRoomOptions(trigger.GetCurrentOptions(player));
    }

    #endregion
}
