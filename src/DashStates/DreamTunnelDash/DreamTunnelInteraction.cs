using Monocle;
using System;

namespace Celeste.Mod.CommunalHelper.DashStates;

public class DreamTunnelInteraction : Component
{
    public Action<Player> OnPlayerEnter;
    public Action<Player> OnPlayerExit;
    public DreamTunnelInteraction(Action<Player> onPlayerEnter, Action<Player> onPlayerExit)
        : base(false, false)
    {
        OnPlayerEnter = onPlayerEnter;
        OnPlayerExit = onPlayerExit;
    }
}
