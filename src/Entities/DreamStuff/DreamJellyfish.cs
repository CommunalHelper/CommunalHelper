using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/DreamJellyfish")]
    class DreamJellyfish : Glider {

        public DreamJellyfish(EntityData data, Vector2 offset) 
            : this(data.Position + offset, data.Bool("bubble"), data.Bool("tutorial")) { }

        public DreamJellyfish(Vector2 position, bool bubble, bool tutorial)
            : base(position, bubble, tutorial) {
            Add(new DreamDashCollider(new Hitbox(28, 12, -13, -18)) {
                Active = false,
            });
        }
    }
}
