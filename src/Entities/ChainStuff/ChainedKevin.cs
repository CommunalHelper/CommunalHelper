using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using static Celeste.MoveBlock;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/ChainedKevin")]
    class ChainedKevin : CrushBlock {

        private Directions direction;

        public ChainedKevin(EntityData data, Vector2 offset)
            : base(data, offset) { }

        #region Hooks

        internal static void Load() {
            On.Celeste.CrushBlock.ctor_EntityData_Vector2 += CrushBlock_ctor_EntityData_Vector2;
        }

        internal static void Unload() {
            On.Celeste.CrushBlock.ctor_EntityData_Vector2 -= CrushBlock_ctor_EntityData_Vector2;
        }

        private static void CrushBlock_ctor_EntityData_Vector2(On.Celeste.CrushBlock.orig_ctor_EntityData_Vector2 orig, CrushBlock self, EntityData data, Vector2 offset) {
            if (self is ChainedKevin chainedKevin)
                chainedKevin.direction = data.Enum("direction", Directions.Right);

            orig(self, data, offset);
        }

        #endregion
    }
}