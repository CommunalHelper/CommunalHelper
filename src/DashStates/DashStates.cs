namespace Celeste.Mod.CommunalHelper.DashStates;

public enum DashStates
{
    DreamTunnelDash,
    DreamTunnelDoubleDash,
    SeekerDash,
}

public static class DashStatesExt
{
    public static void SetEnabled(this DashStates state, bool enable)
    {
        DreamTunnelDash.DreamTunnelDashComponent dreamTunnelDashComponent
            = Util.TryGetPlayer(out Player player) ? player.Get<DreamTunnelDash.DreamTunnelDashComponent>() : null;
        
        switch (state)
        {
            case DashStates.DreamTunnelDash when dreamTunnelDashComponent is not null:
                dreamTunnelDashComponent.DreamTunnelDashCount = 1;
                break;
            case DashStates.DreamTunnelDoubleDash when dreamTunnelDashComponent is not null:
                dreamTunnelDashComponent.DreamTunnelDashCount = 2;
                break;
            case DashStates.SeekerDash:
                SeekerDash.HasSeekerDash = enable;
                break;
        }
    }
}
