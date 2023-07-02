using Celeste.Mod.CommunalHelper.Entities;
using System.Collections.Generic;
using static Celeste.Mod.CommunalHelper.Entities.StationBlockTrack;

namespace Celeste.Mod.CommunalHelper;

public class CommunalHelperSession : EverestModuleSession
{
    public SortedSet<string> SummitGems { get; set; }

    public TrackSwitchState TrackInitialState = TrackSwitchState.On;

    public bool CassetteJumpFix = false;

    public HashSet<RedlessBerry.Info> RedlessBerries { get; set; } = new();

    public bool PlayerWasTired { get; set; } = false;

    // used by expiring dash refills
    public double ExpiringDashRemainingTime { get; set; }
    public float ExpiringDashFlashThreshold { get; set; }

    public bool CanDeployElytra { get; set; }

    public CommunalHelperSession()
    {
        SummitGems = new SortedSet<string>(StringComparer.InvariantCulture);
    }
}
