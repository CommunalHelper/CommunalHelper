namespace Celeste.Mod.CommunalHelper.Components;

[Tracked]
public class DreamTunnelInteraction : Component
{
    public readonly Action<Player> OnPlayerEnter;
    public readonly Action<Player> OnPlayerExit;
    
    public readonly Action<Player> OnPlayerDreamJump;
    private bool activateDreamJump;

    public DreamTunnelInteraction(Action<Player> onPlayerEnter = null, Action<Player> onPlayerExit = null, Action<Player> onPlayerDreamJump = null)
        : base(false, false)
    {
        OnPlayerEnter = onPlayerEnter;
        OnPlayerExit = onPlayerExit;
        OnPlayerDreamJump = onPlayerDreamJump;
        
        activateDreamJump = false;
    }

    public static void ActivateDreamJump(Solid solid)
    {
        foreach (DreamTunnelInteraction interaction in solid.Components.GetAll<DreamTunnelInteraction>())
            interaction.activateDreamJump = true;
    }
    public static void ResetDreamJumpActivation(Player player)
    {
        foreach (DreamTunnelInteraction interaction in player.Scene.Tracker.GetComponents<DreamTunnelInteraction>())
            interaction.activateDreamJump = false;
    }

    public static void OnEnter(Player player, Solid solid)
    {
        foreach (DreamTunnelInteraction interaction in solid.Components.GetAll<DreamTunnelInteraction>())
            interaction.OnPlayerEnter?.Invoke(player);
    }
    public static void OnExit(Player player, Solid solid)
    {
        foreach (DreamTunnelInteraction interaction in solid.Components.GetAll<DreamTunnelInteraction>())
            interaction.OnPlayerExit?.Invoke(player);
    }
    public static void OnDreamJump(Player player)
    {
        foreach (DreamTunnelInteraction interaction in player.Scene.Tracker.GetComponents<DreamTunnelInteraction>())
        {
            if (interaction.activateDreamJump)
                interaction.OnPlayerDreamJump?.Invoke(player);
        }
    }
}
