using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using static Celeste.MoveBlock;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/ChainedKevin")]
    class ChainedKevin : CrushBlock {

        private Directions direction;
        private DynData<CrushBlock> crushBlockData;
        private List<Image> idleImages, activeTopImages, activeRightImages, activeLeftImages, activeBottomImages;

        public ChainedKevin(EntityData data, Vector2 offset)
            : base(data, offset) { }

        #region Hooks

        internal static void Load() {
            On.Celeste.CrushBlock.ctor_EntityData_Vector2 += CrushBlock_ctor_EntityData_Vector2;
            IL.Celeste.CrushBlock.ctor_Vector2_float_float_Axes_bool += CrushBlock_ctor_Vector2_float_float_Axes_bool;
            On.Celeste.CrushBlock.AddImage += CrushBlock_AddImage;
        }

        private static void CrushBlock_ctor_Vector2_float_float_Axes_bool(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            if (cursor.TryGotoNext(instr => instr.MatchLdstr("objects/crushblock/block")) &&
                cursor.TryGotoNext(MoveType.After, instr => instr.MatchStloc(0))) {

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Action<CrushBlock>>(self => {
                    if (self is ChainedKevin chainedKevin) {
                        chainedKevin.crushBlockData = new DynData<CrushBlock>(chainedKevin);

                        chainedKevin.idleImages = chainedKevin.crushBlockData.Get<List<Image>>("idleImages");
                        chainedKevin.activeTopImages = chainedKevin.crushBlockData.Get<List<Image>>("activeTopImages");
                        chainedKevin.activeRightImages = chainedKevin.crushBlockData.Get<List<Image>>("activeRightImages");
                        chainedKevin.activeLeftImages = chainedKevin.crushBlockData.Get<List<Image>>("activeLeftImages");
                        chainedKevin.activeBottomImages = chainedKevin.crushBlockData.Get<List<Image>>("activeBottomImages");
                    }
                });
            }
        }

        internal static void Unload() {
            On.Celeste.CrushBlock.ctor_EntityData_Vector2 -= CrushBlock_ctor_EntityData_Vector2;
            IL.Celeste.CrushBlock.ctor_Vector2_float_float_Axes_bool -= CrushBlock_ctor_Vector2_float_float_Axes_bool;
            On.Celeste.CrushBlock.AddImage -= CrushBlock_AddImage;
        }

        private static void CrushBlock_ctor_EntityData_Vector2(On.Celeste.CrushBlock.orig_ctor_EntityData_Vector2 orig, CrushBlock self, EntityData data, Vector2 offset) {
            if (self is ChainedKevin chainedKevin) {
                chainedKevin.direction = data.Enum("direction", Directions.Right);
            }
            orig(self, data, offset);
        }

        private static void CrushBlock_AddImage(On.Celeste.CrushBlock.orig_AddImage orig, CrushBlock self, MTexture idle, int x, int y, int tx, int ty, int borderX, int borderY) {
            if (self is ChainedKevin chainedKevin) {
                MTexture subtexture = GFX.Game["objects/CommunalHelper/chainedKevin/block" + chainedKevin.direction].GetSubtexture(tx * 8, ty * 8, 8, 8);
                Vector2 vector = new Vector2(x * 8, y * 8);

                if (borderX != 0) {
                    Image image = new Image(subtexture) {
                        Color = Color.Black,
                        Position = vector + new Vector2(borderX, 0f)
                    };
                    self.Add(image);
                }
                if (borderY != 0) {
                    Image image = new Image(subtexture) {
                        Color = Color.Black,
                        Position = vector + new Vector2(0f, borderY)
                    };
                    self.Add(image);
                }

                Image image3 = new Image(subtexture);
                image3.Position = vector;
                self.Add(image3);

                chainedKevin.idleImages.Add(image3);

                if (borderX != 0 || borderY != 0) {
                    if (borderX < 0 && chainedKevin.direction == Directions.Left) {
                        Image image = new Image(GFX.Game["objects/crushblock/lit_left"].GetSubtexture(0, ty * 8, 8, 8)) {
                            Position = vector,
                            Visible = false
                        };
                        chainedKevin.activeLeftImages.Add(image);
                        self.Add(image);
                    } else if (borderX > 0 && chainedKevin.direction == Directions.Right) {
                        Image image = new Image(GFX.Game["objects/crushblock/lit_right"].GetSubtexture(0, ty * 8, 8, 8)) {
                            Position = vector,
                            Visible = false
                        };
                        chainedKevin.activeRightImages.Add(image);
                        self.Add(image);
                    }
                    if (borderY < 0 && chainedKevin.direction == Directions.Up) {
                        Image image = new Image(GFX.Game["objects/crushblock/lit_top"].GetSubtexture(tx * 8, 0, 8, 8)) {
                            Position = vector,
                            Visible = false
                        };
                        chainedKevin.activeTopImages.Add(image);
                        self.Add(image);
                    } else if (borderY > 0 && chainedKevin.direction == Directions.Down) {
                        Image image = new Image(GFX.Game["objects/crushblock/lit_bottom"].GetSubtexture(tx * 8, 0, 8, 8)) {
                            Position = vector,
                            Visible = false
                        };
                        chainedKevin.activeBottomImages.Add(image);
                        self.Add(image);
                    }
                }
            } else {
                orig(self, idle, x, y, tx, ty, borderX, borderY);
            }
        }

        #endregion
    }
}