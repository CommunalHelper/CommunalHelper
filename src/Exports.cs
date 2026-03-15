using Celeste.Mod.CommunalHelper.Components;
using Celeste.Mod.CommunalHelper.DashStates;
using Celeste.Mod.CommunalHelper.States;
using MonoMod.ModInterop;
using DreamTunnelDash = Celeste.Mod.CommunalHelper.DashStates.DreamTunnelDash;

namespace Celeste.Mod.CommunalHelper;

public static class ModExports
{
    internal static void Initialize()
    {
        typeof(DashStates).ModInterop();
        typeof(Entities).ModInterop();
    }

    [ModExportName("CommunalHelper.DashStates")]
    public static class DashStates
    {
        #region DreamTunnel

        public static int GetDreamTunnelDashState()
        {
            return St.DreamTunnelDash;
        }

        public static bool HasDreamTunnelDash()
        {
            return Util.TryGetPlayer(out Player player) && (player.Get<DreamTunnelDash.DreamTunnelDashComponent>()?.DreamTunnelDashCount ?? 0) > 0;
        }

        public static int GetDreamTunnelDashCount()
        {
            return Util.TryGetPlayer(out Player player) ? player.Get<DreamTunnelDash.DreamTunnelDashComponent>()?.DreamTunnelDashCount ?? 0 : 0;
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

    [ModExportName("CommunalHelper.Entities")]
    public static class Entities
    {
        #region Misc
        
        #region Melvin

        public static Component MelvinTargetable(int priority)
        {
            return new MelvinTargetable(priority);
        }

        #endregion
        
        #endregion
    }
}
