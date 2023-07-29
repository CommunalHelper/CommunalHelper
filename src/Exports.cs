using Celeste.Mod.CommunalHelper.Components;
using Celeste.Mod.CommunalHelper.DashStates;
using MonoMod.ModInterop;

namespace Celeste.Mod.CommunalHelper;

public static class ModExports
{
    internal static void Initialize()
    {
        typeof(DashStates).ModInterop();
    }

    [ModExportName("CommunalHelper.DashStates")]
    public static class DashStates
    {
        #region DreamTunnel

        public static int GetDreamTunnelDashState()
        {
            return DreamTunnelDash.StDreamTunnelDash;
        }

        public static bool HasDreamTunnelDash()
        {
            return DreamTunnelDash.HasDreamTunnelDash;
        }

        public static Component DreamTunnelInteraction(Action<Player> onPlayerEnter, Action<Player> onPlayerExit)
        {
            return new DreamTunnelInteraction(onPlayerEnter, onPlayerExit);
        }

        #endregion

        #region Seeker

        public static bool HasSeekerDash()
        {
            return SeekerDash.HasSeekerDash;
        }

        public static bool IsSeekerDashAttacking()
        {
            return SeekerDash.SeekerAttacking;
        }

        #endregion
    }

}
