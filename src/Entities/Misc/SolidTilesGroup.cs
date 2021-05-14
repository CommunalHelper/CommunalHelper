using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/SolidTilesGroup")]
    class SolidTilesGroup : ConnectedSolid {

        public SolidTilesGroup(EntityData data, Vector2 offset) :
            this(data.Position + offset, data.Width, data.Height) { }

        public SolidTilesGroup(Vector2 position, int width, int height) :
            base(position, width, height, safe: true) {
            Collidable = false;
        }


    }
}
