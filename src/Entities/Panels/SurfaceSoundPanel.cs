using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/SurfaceSoundPanel")]
    public class SurfaceSoundPanel : AbstractPanel {

        #region Loading

        public static Entity Load(Level level, LevelData levelData, Vector2 offset, EntityData entityData) {
            Spikes.Directions orientation = entityData.Enum<Spikes.Directions>("orientation");
            return new SurfaceSoundPanel(entityData.Position + offset, GetSize(entityData, orientation), orientation, entityData.Int("soundIndex", SurfaceIndex.DreamBlockInactive));
        }

        #endregion

        public SurfaceSoundPanel(Vector2 position, float size, Spikes.Directions orientation, int soundIndex) 
            : base(position, size, orientation, false) {
            surfaceSoundIndex = soundIndex;
        }
    }
}
