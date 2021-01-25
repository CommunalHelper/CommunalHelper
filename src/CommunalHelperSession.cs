using System;
using System.Collections.Generic;
using static Celeste.Mod.CommunalHelper.Entities.StationBlockTrack;

namespace Celeste.Mod.CommunalHelper {
    public class CommunalHelperSession : EverestModuleSession {
        public SortedSet<string> SummitGems { get; set; }

        public TrackSwitchState TrackInitialState = TrackSwitchState.On;

        public CommunalHelperSession() {
            SummitGems = new SortedSet<string>(StringComparer.InvariantCulture);
        }

    }
}
