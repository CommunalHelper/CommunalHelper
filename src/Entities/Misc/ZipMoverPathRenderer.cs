using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CommunalHelper.Entities {
    public interface IMultiNodeZipMover {
        Vector2[] Nodes { get; }
    }

    public class ZipMoverPathRenderer : Entity {

        public ZipMoverPathRenderer(int depth = Depths.BGDecals) {
            Depth = depth;
        }

        public void CreateSparks() {
        }
    }
}
