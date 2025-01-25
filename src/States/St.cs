namespace Celeste.Mod.CommunalHelper.States;

public static class St
{
    public static int DreamTunnelDash { get; private set; } = -1;
    public static int Elytra { get; private set; } = -1;

    internal static void Initialize()
    {
        States.Elytra.Initialize();
    }

    internal static void Load()
    {
        Everest.Events.Player.OnRegisterStates += RegisterPlayerStates;

        States.Elytra.Load();
    }

    internal static void Unload()
    {
        States.Elytra.Unload();
    }

    private static void RegisterPlayerStates(Player player)
    {
        DreamTunnelDash = player.AddState("StDreamTunnelDash", States.DreamTunnelDash.DreamTunnelDashUpdate, null, States.DreamTunnelDash.DreamTunnelDashBegin, States.DreamTunnelDash.DreamTunnelDashEnd);
        Elytra = player.AddState("StElytra", States.Elytra.GlideUpdate, States.Elytra.GlideRoutine, States.Elytra.GlideBegin, States.Elytra.GlideEnd);
    }
}
