﻿using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CommunalHelper.Triggers {
    [CustomEntity("CommunalHelper/RedlessBerryCollection")]
    [Tracked]
    public class RedlessBerryCollectionTrigger : Trigger {
        public RedlessBerryCollectionTrigger(EntityData data, Vector2 offset)
            : base(data, offset) { }
    }
}
