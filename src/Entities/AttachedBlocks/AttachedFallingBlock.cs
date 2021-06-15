using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.CommunalHelper.Entities.AttachedBlocks {
    [CustomEntity("CommunalHelper/AttachedFallingBlock")]
    class AttachedFallingBlock : AttachedBlock {

        public AttachedFallingBlock(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Width, data.Height, data.Char("tiletype", '3')) { }

        public AttachedFallingBlock(Vector2 position, int width, int height, char tileType) 
            : base(position, width, height, tileType, safe: false) {
        }

    }
}
