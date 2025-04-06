namespace Celeste.Mod.CommunalHelper.Components;

/// <summary>
/// Component to mark entities which should be attacked by Melvins.
/// </summary>
[Tracked]
public class MelvinTargetable : Component
{
    public MelvinTargetable() : base(false, false)
    {
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
            player.Add(new MelvinTargetable());
    }

    private static void OnBeforeReload(bool silent)
    {
        // Remove component before reload so the player doesn't end up with more than 1
        if (Engine.Scene?.Tracker?.GetEntity<Player>() is Player player)
            player.Remove(player.Get<MelvinTargetable>());
    }
    
    #endregion
}
