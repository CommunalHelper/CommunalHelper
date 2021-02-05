using Celeste.Mod.CommunalHelper.Entities;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.CommunalHelper.Triggers {
    [CustomEntity("CommunalHelper/TrackSwitchTrigger")]
    class TrackSwitchTrigger : Trigger {

        public enum Modes {
            Alternate, On, Off
        }

        private bool oneUse = true;
        private bool flash = false;
        private bool global = false;
        private Modes mode;

        public TrackSwitchTrigger(EntityData data, Vector2 offset) 
            : base(data,offset) {
            oneUse = data.Bool("oneUse", true);
            flash = data.Bool("flash", false);
            global = data.Bool("globalSwitch", false);
            mode = data.Enum("mode", Modes.Alternate);
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);

            if (oneUse)
                Collidable = false;
            StationBlockTrack.TrackSwitchState state;

            switch (mode) {

                default:
                case Modes.Alternate:
                    state = TrackSwitchBox.LocalTrackSwitchState == StationBlockTrack.TrackSwitchState.On ?
                        StationBlockTrack.TrackSwitchState.Off :
                        StationBlockTrack.TrackSwitchState.On;
                    break;

                case Modes.On:
                    state = StationBlockTrack.TrackSwitchState.On;
                    break;

                case Modes.Off:
                    state = StationBlockTrack.TrackSwitchState.Off;
                    break;
                    
            }
            // switches
            bool switched = TrackSwitchBox.Switch(Scene, state, global);

            if (flash && switched)
                Pulse();
        }


        private void Pulse() {
            SceneAs<Level>().Shake(.2f);
            Add(new Coroutine(Lightning.PulseRoutine(SceneAs<Level>())));
        }
    }
}
