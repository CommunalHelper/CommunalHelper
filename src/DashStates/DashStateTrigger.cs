using Microsoft.Xna.Framework;

namespace Celeste.Mod.CommunalHelper.DashStates {
    public abstract class DashStateTrigger : Trigger {

        public enum Modes {
            OneUse = -1,
            Trigger = 0,
            Field = 1
        }

        public Modes Mode;

        protected DashStateTrigger(EntityData data, Vector2 offset)
            : base(data, offset) {
            Mode = data.Enum("mode", Modes.Trigger);
        }

        protected abstract void SetDashState(bool active);

        public override void OnEnter(Player player) {
            base.OnEnter(player);
            if (Mode == Modes.OneUse)
                RemoveSelf();
            SetDashState(true);
        }

        public override void OnStay(Player player) {
            base.OnStay(player);
            if (Mode == Modes.Field)
                SetDashState(true);
        }

        public override void OnLeave(Player player) {
            base.OnLeave(player);
            if (Mode == Modes.Field)
                SetDashState(false);
        }

    }
}
