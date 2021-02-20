using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/MoveBlockRedirect")]
    public class MoveBlockRedirect : Entity {

        internal const string MoveBlock_InitialAngle = "communalHelperInitialAngle";
        internal const string MoveBlock_InitialDirection = "communalHelperInitialDirection";

        public static readonly Color Mask = new Color(200, 180, 190);

        private static readonly FieldInfo f_MoveBlock_canSteer = typeof(MoveBlock).GetField("canSteer", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly Type t_MoveBlock_Controller = typeof(MoveBlock).GetNestedType("<Controller>d__45", BindingFlags.NonPublic);
        private static readonly FieldInfo f_MoveBlock_Controller_this = t_MoveBlock_Controller.GetField("<>4__this", BindingFlags.Public | BindingFlags.Instance);

        public MoveBlock.Directions Direction;
        public bool FastRedirect;

        private float angle;
        private MoveBlock currentBlock;
        private float maskAlpha;
        private List<Image> borders;

        private MoveBlock lastMoveBlock;

        public MoveBlockRedirect(EntityData data, Vector2 offset)
            : base(data.Position + offset) {
            Depth = Depths.Above;
            Collider = new Hitbox(data.Width, data.Height);

            FastRedirect = data.Bool("fastRedirect");

            if (float.TryParse(data.Attr("direction"), out float fAngle))
                angle = fAngle;
            else {
                Direction = data.Enum<MoveBlock.Directions>("direction");
                angle = Direction switch {
                    MoveBlock.Directions.Left => Calc.HalfCircle,
                    MoveBlock.Directions.Up => -Calc.QuarterCircle,
                    MoveBlock.Directions.Down => Calc.QuarterCircle,
                    _ => 0f,
                };
            }

            AddTextures();
        }

        private void AddTextures() {
            borders = new List<Image>();

            // Add Corners
            for (int i = 0; i < 4; i++) {
                Image image = new Image(GFX.Game["objects/CommunalHelper/moveBlockRedirect/corner"]);
                image.Rotation = Calc.QuarterCircle * i;
                image.Position = i switch {
                    0 => image.Position,
                    1 => Vector2.UnitX * Width,
                    2 => new Vector2(Width, Height),
                    3 => Vector2.UnitY * Height,
                    _ => throw new NotImplementedException()
                };
                image.CenterOrigin();
                borders.Add(image);
                Add(image);
            }

            // Top / Bottom
            for (int i = 16; i <= Width / 2; i += 16) {
                AddImage(GFX.Game["objects/CommunalHelper/moveBlockRedirect/side"], Vector2.UnitX * i, Calc.QuarterCircle, borders);
                AddImage(GFX.Game["objects/CommunalHelper/moveBlockRedirect/side"], Vector2.UnitX * (Width - i), Calc.QuarterCircle, borders);

                AddImage(GFX.Game["objects/CommunalHelper/moveBlockRedirect/side"], new Vector2(i, Height), -Calc.QuarterCircle, borders);
                AddImage(GFX.Game["objects/CommunalHelper/moveBlockRedirect/side"], new Vector2(Width - i, Height), -Calc.QuarterCircle, borders);
            }

            // Left / Right
            for (int i = 16; i <= Height / 2; i += 16) {
                AddImage(GFX.Game["objects/CommunalHelper/moveBlockRedirect/side"], Vector2.UnitY * i, 0f, borders);
                AddImage(GFX.Game["objects/CommunalHelper/moveBlockRedirect/side"], Vector2.UnitY * (Height - i), 0f, borders);
                AddImage(GFX.Game["objects/CommunalHelper/moveBlockRedirect/side"], new Vector2(Width, i), Calc.HalfCircle, borders);
                AddImage(GFX.Game["objects/CommunalHelper/moveBlockRedirect/side"], new Vector2(Width, Height - i), Calc.HalfCircle, borders);
            }

            // Unused in favor of large arrow
            /*
            int x = 8;
            for (int y = 8; y <= Height; y += 8) {
                for (; x <= Width; x += 16) {
                    Image image = new Image(GFX.Game["objects/CommunalHelper/moveBlockRedirect/arrow"]);
                    image.Position = new Vector2(x, y);
                    image.Color = new Color(100, 80, 120) * 0.5f;
                    image.Rotation = angle;
                    arrows.Add(image);
                    Add(image);
                }
                x = ((y / 8) % 2 == 0) ? 8 : 16;
            }
            */

        }

        private void AddImage(MTexture texture, Vector2 position, float rotation, List<Image> addTo) {
            Image image = new Image(texture);
            image.Rotation = rotation;
            image.Position = position;
            image.CenterOrigin();
            Add(image);
            addTo?.Add(image);
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            scene.Add(new Arrow(Center, angle));
        }

        private static Vector2 MoveBlockDirectionToVector(MoveBlock.Directions dir, float factor = 1f) {
            Vector2 result = dir switch {
                MoveBlock.Directions.Up => -Vector2.UnitY,
                MoveBlock.Directions.Down => Vector2.UnitY,
                MoveBlock.Directions.Left => -Vector2.UnitX,
                _ => Vector2.UnitX
            };

            return result * factor;
        }

        public override void Update() {
            base.Update();

            MoveBlock moveBlock = CollideAll<Solid>().FirstOrDefault(e => e is MoveBlock) as MoveBlock;

            if (lastMoveBlock != null && !CollideCheck(lastMoveBlock)) {
                lastMoveBlock = null;
            } else {
                if (moveBlock == lastMoveBlock)
                    return;
            }

            if (moveBlock != null && !(bool) f_MoveBlock_canSteer.GetValue(moveBlock) &&
                moveBlock.Width == Width && moveBlock.Height == Height) {

                DynData<MoveBlock> blockData = new DynData<MoveBlock>(moveBlock);
                if (!Collider.Contains(moveBlock.Collider, 0.001f)) {
                    MoveBlock.Directions dir = blockData.Get<MoveBlock.Directions>("direction");
                    Vector2 prevPosOffset = -MoveBlockDirectionToVector(dir, blockData.Get<float>("speed"));

                    float edgeMin;
                    float edgeMax;
                    bool wentThrough = false;
                    if (dir == MoveBlock.Directions.Down || dir == MoveBlock.Directions.Up) {
                        edgeMin = Math.Min(moveBlock.Top, moveBlock.Top + prevPosOffset.Y);
                        edgeMax = Math.Max(moveBlock.Bottom, moveBlock.Bottom + prevPosOffset.Y);
                        wentThrough = X == moveBlock.X && edgeMin <= Top && edgeMax >= Bottom;
                    } else {
                        edgeMin = Math.Min(moveBlock.Left, moveBlock.Left + prevPosOffset.X);
                        edgeMax = Math.Max(moveBlock.Right, moveBlock.Right + prevPosOffset.X);
                        wentThrough = Y == moveBlock.Y && edgeMin <= Left && edgeMax >= Right;
                    }

                    if (!wentThrough)
                        return;
                }


                if (FastRedirect)
                    SetBlockData(blockData);
                else if (currentBlock == null) {
                    currentBlock = moveBlock;
                    moveBlock.Remove(moveBlock.Get<Coroutine>());
                    Add(new Coroutine(RedirectRoutine(moveBlock)));
                }
            }
        }

        private void SetBlockData(DynData<MoveBlock> blockData) {
            if (!blockData.Data.ContainsKey(MoveBlock_InitialAngle)) {
                blockData[MoveBlock_InitialAngle] = blockData["homeAngle"];
                blockData[MoveBlock_InitialDirection] = blockData["direction"];
            }
            blockData["angle"] = blockData["targetAngle"] = blockData["homeAngle"] = angle;
            blockData["direction"] = Direction;
            blockData.Target.X = X;
            blockData.Target.Y = Y;
            lastMoveBlock = blockData.Target;
        }

        private IEnumerator RedirectRoutine(MoveBlock block) {
            DynData<MoveBlock> blockData = new DynData<MoveBlock>(block);
            float duration = 1f;

            block.MoveTo(Position);

            SoundSource moveSfx = blockData.Get<SoundSource>("moveSfx");
            moveSfx.Param("redirect_slowdown", 1f);

            foreach (Image img in borders)
                img.Texture = GFX.Game[img.Texture.AtlasPath + "_active"];

            block.StartShaking(0.2f);

            float timer = 0f;
            while (timer < duration) {
                timer += Engine.DeltaTime;
                maskAlpha = Ease.BounceIn(timer / duration);
                yield return null;
            }

            SetBlockData(blockData);

            while (timer > 0.2f) {
                timer -= Engine.DeltaTime;
                maskAlpha = Ease.BounceIn(timer / duration);
                yield return null;
            }

            block.StartShaking(0.18f);
            moveSfx.Param("redirect_slowdown", 0f);

            while (timer > 0) {
                timer -= Engine.DeltaTime;
                maskAlpha = Ease.BounceIn(timer / duration);
                yield return null;
            }

            foreach (Image img in borders) {
                string path = img.Texture.AtlasPath;
                img.Texture = GFX.Game[path.Substring(0, path.Length - 7)];
            }

            // Absolutely cursed, starts the Controller routine after a certain number of yields
            IEnumerator controller;
            block.Add(new Coroutine(controller = (IEnumerator) Activator.CreateInstance(t_MoveBlock_Controller, 3)));
            f_MoveBlock_Controller_this.SetValue(controller, block);

            // Wait for the moveblock to continue before resetting
            yield return null;
            currentBlock = null;
        }

        public override void Render() {
            Draw.Rect(X - 1, Y - 1, Width + 2, Height + 2, Mask * maskAlpha);
            base.Render();

        }

        private class Arrow : Entity {
            public Image Sprite;
            public Arrow(Vector2 position, float rotation)
                : base(position) {
                Depth = Depths.Below;
                Add(Sprite = new Image(GFX.Game["objects/CommunalHelper/moveBlockRedirect/bigarrow"]));
                Sprite.CenterOrigin();
                Sprite.Rotation = rotation;
            }
        }

        #region Hooks

        private static IDetour hook_MoveBlock_Controller;

        internal static void Load() {
            hook_MoveBlock_Controller = new ILHook(typeof(MoveBlock).GetMethod("Controller", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget(),
                MoveBlock_Controller);
            On.Celeste.MoveBlock.BreakParticles += MoveBlock_BreakParticles;
        }

        internal static void Unload() {
            hook_MoveBlock_Controller.Dispose();
            On.Celeste.MoveBlock.BreakParticles -= MoveBlock_BreakParticles;
        }

        private static void MoveBlock_Controller(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            while (cursor.TryGotoNext(instr => instr.MatchLdstr(SFX.game_04_arrowblock_move_loop)))
                cursor.Remove().Emit(OpCodes.Ldstr, CustomSFX.game_redirectMoveBlock_arrowblock_move);
        }

        private static void MoveBlock_BreakParticles(On.Celeste.MoveBlock.orig_BreakParticles orig, MoveBlock self) {
            orig(self);
            DynData<MoveBlock> blockData = new DynData<MoveBlock>(self);
            if (blockData.Data.TryGetValue(MoveBlock_InitialAngle, out object angle)) {
                blockData["angle"] = blockData["targetAngle"] = blockData["homeAngle"] = angle;
                blockData["direction"] = blockData[MoveBlock_InitialDirection];
            }
        }

        #endregion

    }
}
