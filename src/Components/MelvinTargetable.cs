namespace Celeste.Mod.CommunalHelper.Components;

/// <summary>
/// Component to mark entities which should be attacked by Melvins.
/// </summary>
[Tracked]
public class MelvinTargetable : Component
{
    /// <summary>
    /// The priority of this component.
    /// Players have a priority of 0.
    /// A lower priority value means this component will be checked for attack first, so a value of -1 would be checked before the player, and a value of 1 would be checked after the player.
    /// Ties in priority value are broken by distance.
    /// </summary>
    public readonly int Priority;
    
    public MelvinTargetable(int priority = 0) : base(false, false)
    {
        Priority = priority;
    }
    
    #region Hooks

    internal static void Load()
    {
        Everest.Events.Player.OnSpawn += OnSpawn;
        Everest.Events.AssetReload.OnBeforeReload += OnBeforeReload;
    }

    internal static void Unload()
    {
        Everest.Events.Player.OnSpawn -= OnSpawn;
        Everest.Events.AssetReload.OnBeforeReload -= OnBeforeReload;
    }

    private static void OnSpawn(Player player)
    {
        if (player.Get<MelvinTargetable>() is null)
            player.Add(new MelvinTargetable(0));
    }

    private static void OnBeforeReload(bool silent)
    {
        // Remove component before reload so the player doesn't end up with more than 1
        if (Engine.Scene?.Tracker?.GetEntity<Player>() is Player player)
            player.Remove(player.Get<MelvinTargetable>());
    }
    
    #endregion
}
