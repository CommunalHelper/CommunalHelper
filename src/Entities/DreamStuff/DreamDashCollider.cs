using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CommunalHelper.Entities {
    [Tracked]
    class DreamDashCollider : Component {
        public static readonly Color ActiveColor = Color.Teal;
        public static readonly Color InactiveColor = Calc.HexToColor("044f63"); // darker teal

        public Collider Collider;

        public DreamDashCollider(Collider collider)
            : base(active: true, visible: false) {
            Collider = collider;
        }

        /// <summary>
        /// Checks if the player is colliding with this component's associated entity.
        /// </summary>
        /// <param name="player">The player instance.</param>
        /// <returns></returns>
        public bool Check(Player player) {
            Collider collider = Entity.Collider;

            Entity.Collider = Collider;
            bool check = player.CollideCheck(Entity);
            Entity.Collider = collider;

            return check;
        }

        public override void DebugRender(Camera camera) {
            if (Collider != null) {
                Collider collider = Entity.Collider;
                
                Entity.Collider = Collider;
                Collider.Render(camera, Active ? ActiveColor : InactiveColor);
                Entity.Collider = collider;
            }
        }
    }
}
