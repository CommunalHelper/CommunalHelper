using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/DashCollisionPanel")]
    public class DashCollisionPanel : AbstractPanel {

        #region Loading

        public static Entity Load(Level level, LevelData levelData, Vector2 offset, EntityData entityData) {
            Spikes.Directions orientation = entityData.Enum<Spikes.Directions>("orientation");
            return new DashCollisionPanel(entityData, offset, orientation);
        }

        #endregion

        public DashCollisionResults dashCollisionOverride;
        public bool overrideCollision;

        public DashCollisionPanel(EntityData data, Vector2 offset, Spikes.Directions orientation)
            : base(data.Position + offset, GetSize(data, orientation), orientation, false) {
            overrideCollision = data.Bool("overrideCollision");
            dashCollisionOverride = data.Enum("dashCollideResult", DashCollisionResults.NormalCollision);
        }

        protected override DashCollisionResults OnDashCollide(Player player, Vector2 dir) {
            if (!overrideCollision)
                base.OnDashCollide(player, dir);
            return dashCollisionOverride;
        }
    }
}
