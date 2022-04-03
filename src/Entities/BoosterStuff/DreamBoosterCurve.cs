using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/CurvedDreamBooster")]
    public class DreamBoosterCurve : DreamBooster {
        // weird but ok
        public enum CurveMode {
            Quadratic = 2,
            Cubic = 3,
        }
        private readonly CurveMode mode;

        private readonly Vector2[] nodes;
        private readonly int curveCount;

        public DreamBoosterCurve(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.NodesWithPosition(offset), data.Enum<CurveMode>("curve"), !data.Bool("hidePath")) { }
        
        public DreamBoosterCurve(Vector2 position, Vector2[] nodes, CurveMode mode, bool showPath)
            : base(position, showPath) {
            this.mode = mode;

            int l = nodes.Length - 1;
            int count = l - (l % (int)mode);
            curveCount = count / (int) mode;

            this.nodes = new Vector2[count + 1];
            for (int i = 0; i <= count; i++)
                this.nodes[i] = nodes[i];
        }
    }
}
