using Celeste.Mod.CommunalHelper.Entities;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.CommunalHelper.Triggers {
    [CustomEntity("CommunalHelper/StopLightningControllerTrigger")]
    public class StopLightningControllerTrigger : Trigger {
        public StopLightningControllerTrigger(EntityData data, Vector2 offset)
            : base(data, offset) { }

        public override void OnEnter(Player player) {
            base.OnEnter(player);

            foreach (LightningController controller in Scene.Tracker.GetEntities<LightningController>())
                controller.RemoveSelf();

            Collidable = Active = false;
        }
    }
}
