using Celeste.Mod.CommunalHelper.Entities;
using Monocle;
using System;
using System.Collections.Generic;
using static Celeste.Mod.CommunalHelper.Entities.StationBlockTrack;

namespace Celeste.Mod.CommunalHelper {
    public class CommunalHelperSession : EverestModuleSession {
        public SortedSet<string> SummitGems { get; set; }

        public TrackSwitchState TrackInitialState = TrackSwitchState.On;

        public bool CassetteJumpFix = false;

        private int musicBeat;
        public int MusicBeat {
            get => musicBeat;
            set {
                musicBeat = value;
                foreach (MusicSync component in Engine.Scene.Tracker.GetComponents<MusicSync>()) {
                    component.Tick(musicBeat);
                }
            }
        }

        public CommunalHelperSession() {
            SummitGems = new SortedSet<string>(StringComparer.InvariantCulture);
        }
    }
}
