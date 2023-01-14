namespace Celeste.Mod.CommunalHelper.DashStates;

public enum DashStates
{
    DreamTunnelDash,
    SeekerDash,
}

public static class DashStatesExt
{
    public static void SetEnabled(this DashStates state, bool enable)
    {
        switch (state)
        {
            case DashStates.DreamTunnelDash:
                DreamTunnelDash.HasDreamTunnelDash = enable;
                break;
            case DashStates.SeekerDash:
                SeekerDash.HasSeekerDash = enable;
                break;
            default:
                break;
        }
    }
}
