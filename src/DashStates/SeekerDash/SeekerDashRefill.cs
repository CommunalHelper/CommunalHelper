using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using static Celeste.Mod.CommunalHelper.DashStates.SeekerDash;

namespace Celeste.Mod.CommunalHelper.DashStates {
    [CustomEntity("CommunalHelper/SeekerDashRefill")]
    class SeekerDashRefill : DashStateRefill {
        public SeekerDashRefill(EntityData data, Vector2 offset) 
            : base(data, offset) {
        }

        protected override void Activated(Player player) {
            HasSeekerDash = true;
        }

        protected override bool CanActivate(Player player) {
            return !HasSeekerDash;
        }
    }
}
