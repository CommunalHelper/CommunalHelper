using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/SurfaceSoundPanel")]
    public class SurfaceSoundPanel : AbstractPanel {

        public SurfaceSoundPanel(EntityData data, Vector2 offset)
            : base(data, offset) {
            surfaceSoundIndex = data.Int("soundIndex", SurfaceIndex.DreamBlockInactive);
        }
    }
}
