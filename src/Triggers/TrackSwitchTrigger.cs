using Celeste.Mod.CommunalHelper.Entities;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using TrackSwitchState = Celeste.Mod.CommunalHelper.Entities.StationBlockTrack.TrackSwitchState;

namespace Celeste.Mod.CommunalHelper.Triggers {
    [CustomEntity("CommunalHelper/TrackSwitchTrigger")]
    public class TrackSwitchTrigger : Trigger {

        public enum Modes {
            Alternate, On, Off
        }

        private bool oneUse = true;
        private bool flash = false;
        private bool global = false;
        public Modes Mode;

        public TrackSwitchTrigger(EntityData data, Vector2 offset)
            : base(data, offset) {
            oneUse = data.Bool("oneUse", true);
            flash = data.Bool("flash", false);
            global = data.Bool("globalSwitch", false);
            Mode = data.Enum("mode", Modes.Alternate);
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);

            if (oneUse)
                Collidable = false;

            TrackSwitchState state = Mode switch {
                Modes.On => TrackSwitchState.On,
                Modes.Off => TrackSwitchState.Off,
                _ => TrackSwitchBox.LocalTrackSwitchState.Invert()
            };
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
