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
        switch (state)
        {
            case DashStates.DreamTunnelDash:
                DreamTunnelDash.DreamTunnelDashCount = 1;
                break;
            case DashStates.DreamTunnelDoubleDash:
                DreamTunnelDash.DreamTunnelDashCount = 2;
                break;
            case DashStates.SeekerDash:
                SeekerDash.HasSeekerDash = enable;
                break;
            default:
                break;
        }
    }
}
