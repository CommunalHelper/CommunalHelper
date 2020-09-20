using Microsoft.Xna.Framework;
using static Celeste.Mod.CommunalHelper.Entities.DreamTunnelDash;

namespace Celeste.Mod.CommunalHelper.Entities {
    public class DreamTunnelTrigger : DashStateTrigger {

        public DreamTunnelTrigger(EntityData data, Vector2 offset)
            : base(data, offset) { }

        protected override void SetDashState(bool active) {
            HasDreamTunnelDash = active;
        }

    }
}
