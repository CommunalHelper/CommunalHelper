using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using static Celeste.Mod.CommunalHelper.DashStates.DreamTunnelDash;

namespace Celeste.Mod.CommunalHelper.DashStates {
    [CustomEntity("CommunalHelper/DreamTunnelTrigger")]
    public class DreamTunnelTrigger : DashStateTrigger {

        public DreamTunnelTrigger(EntityData data, Vector2 offset)
            : base(data, offset) { }

        protected override void SetDashState(bool active) {
            HasDreamTunnelDash = active;
        }

    }
}
