using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using static Celeste.Mod.CommunalHelper.DashStates.SeekerDash;

namespace Celeste.Mod.CommunalHelper.DashStates {
    [CustomEntity("CommunalHelper/SeekerDashTrigger")]
    public class SeekerDashTrigger : DashStateTrigger {
        public SeekerDashTrigger(EntityData data, Vector2 offset) 
            : base(data, offset) {
        }

        protected override void SetDashState(bool active) {
            HasSeekerDash = active;
        }
    }
}
