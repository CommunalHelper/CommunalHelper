using Celeste.Mod.Entities;
using FMOD.Studio;
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
using Directions = Celeste.MoveBlock.Directions;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/MoveBlockRedirect")]
    public class MoveBlockRedirect : Entity {

        public enum Operation {
            Add, Subtract, Multiply
        }
        private Operation operation;
        private float modifier;

        internal const string MoveBlock_InitialAngle = "communalHelperInitialAngle";
        internal const string MoveBlock_InitialDirection = "communalHelperInitialDirection";

        public static readonly Color Mask = new Color(200, 180, 190);
        public static readonly Color UsedColor = Calc.HexToColor("474070"); // From MoveBlock
        public static readonly Color DeleteColor = Calc.HexToColor("cc2541");
        public static readonly Color DefaultColor = Calc.HexToColor("fbce36");
        public static readonly Color FasterColor = Calc.HexToColor("29c32f");
        public static readonly Color SlowerColor = Calc.HexToColor("1c5bb3");

        private static readonly FieldInfo f_MoveBlock_canSteer = typeof(MoveBlock).GetField("canSteer", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly Type t_MoveBlock_Controller = typeof(MoveBlock).GetNestedType("<Controller>d__45", BindingFlags.NonPublic);
        private static readonly FieldInfo f_MoveBlock_Controller_this = t_MoveBlock_Controller.GetField("<>4__this", BindingFlags.Public | BindingFlags.Instance);
        private static readonly MethodInfo m_MoveBlock_BreakParticles = typeof(MoveBlock).GetMethod("BreakParticles", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo m_MoveBlock_UpdateColors = typeof(MoveBlock).GetMethod("UpdateColors", BindingFlags.NonPublic | BindingFlags.Instance);

        private Color startColor;

        public Directions Direction;
        public bool FastRedirect;
        public bool OneUse, DeleteBlock;

        private float angle;
        private float maskAlpha;
        private List<Image> borders;

        private MoveBlock lastMoveBlock;

        private Icon icon;

        public MoveBlockRedirect(EntityData data, Vector2 offset)
            : base(data.Position + offset) {
            Depth = Depths.Above;
            Collider = new Hitbox(data.Width, data.Height);

            FastRedirect = data.Bool("fastRedirect");
            OneUse = data.Bool("oneUse");
            DeleteBlock = data.Bool("deleteBlock") || (operation == Operation.Multiply && modifier == 0f);

            operation = data.Enum("operation", Operation.Add);
            modifier = Math.Abs(data.Float("modifier"));

            if (float.TryParse(data.Attr("direction"), out float fAngle))
                angle = fAngle;
            else {
                Direction = data.Enum<Directions>("direction");
                angle = Direction switch {
                    Directions.Left => Calc.HalfCircle,
                    Directions.Up => -Calc.QuarterCircle,
                    Directions.Down => Calc.QuarterCircle,
                    _ => 0f,
                };
            }

            AddTextures();
        }

        private void AddTextures() {
            borders = new List<Image>();

            MTexture block = GFX.Game["objects/CommunalHelper/moveBlockRedirect/block"];

            int w = (int) (Width / 8f);
            int h = (int) (Height / 8f);
            for (int i = -1; i <= w; i++) {
                for (int j = -1; j <= h; j++) {
                    int tx = (i == -1) ? 0 : ((i == w) ? 16 : 8);
                    int ty = (j == -1) ? 0 : ((j == h) ? 16 : 8);
                    AddImage(block.GetSubtexture(tx, ty, 8, 8), new Vector2(i, j) * 8, borders);
                }
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

        private void AddImage(MTexture texture, Vector2 position, List<Image> addTo) {
            Image image = new Image(texture);
            image.Position = position;
            Add(image);
            addTo?.Add(image);
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            string iconTexture = "arrow";
            startColor = DefaultColor;

            if (DeleteBlock) {
                iconTexture = "x";
                startColor = DeleteColor;
            } else {
                if ((operation == Operation.Add && modifier != 0f) || (operation == Operation.Multiply && modifier > 1f)) {
                    iconTexture = "fast";
                    startColor = FasterColor;
                } else if ((operation == Operation.Subtract && modifier != 0f) || (operation == Operation.Multiply && modifier < 1f)) {
                    iconTexture = "slow";
                    startColor = SlowerColor;
                }
            }
            scene.Add(icon = new Icon(Center, angle, iconTexture));
            UpdateColors();
        }

        private static Vector2 MoveBlockDirectionToVector(Directions dir, float factor = 1f) {
            Vector2 result = dir switch {
                Directions.Up => -Vector2.UnitY,
                Directions.Down => Vector2.UnitY,
                Directions.Left => -Vector2.UnitX,
                _ => Vector2.UnitX
            };

            return result * factor;
        }

        private void UpdateColors() {
            Color currentColor = Color.Lerp(startColor, UsedColor, maskAlpha);
            icon.Sprite.Color = currentColor;
            foreach(Image image in borders) {
                image.Color = currentColor;
            }
        }

        public override void Update() {
            base.Update();
            UpdateColors();

            if (lastMoveBlock != null && !CollideCheck(lastMoveBlock))
                lastMoveBlock = null;

            MoveBlock moveBlock = CollideAll<Solid>().FirstOrDefault(e => e is MoveBlock) as MoveBlock;

            if (moveBlock != null && moveBlock != lastMoveBlock && !(bool) f_MoveBlock_canSteer.GetValue(moveBlock) &&
                moveBlock.Width == Width && moveBlock.Height == Height) {

                DynData<MoveBlock> blockData = new DynData<MoveBlock>(moveBlock);
                if (!Collider.Contains(moveBlock.Collider, 0.001f)) {
                    Directions dir = blockData.Get<Directions>("direction");
                    Vector2 prevPosOffset = -MoveBlockDirectionToVector(dir, blockData.Get<float>("speed"));

                    float edgeMin;
                    float edgeMax;
                    bool wentThrough = false;
                    if (dir is Directions.Down or Directions.Up) {
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

                lastMoveBlock = moveBlock;

                if (DeleteBlock) {
                    Coroutine routine = moveBlock.Get<Coroutine>();
                    moveBlock.Remove(routine);
                    moveBlock.Add(new Coroutine(BreakBlock(blockData, routine, FastRedirect)));
                } else {
                    if (FastRedirect) {
                        SetBlockData(blockData);
                    } else {
                        Coroutine routine = moveBlock.Get<Coroutine>();
                        moveBlock.Remove(routine);
                        Add(new Coroutine(RedirectRoutine(moveBlock, routine)));
                    }
                }
            }
        }

        public override void Removed(Scene scene) {
            base.Removed(scene);
            icon.RemoveSelf();
        }

        private void SetBlockData(DynData<MoveBlock> blockData) {
            if (!blockData.Data.ContainsKey(MoveBlock_InitialAngle)) {
                blockData[MoveBlock_InitialAngle] = blockData["homeAngle"];
                blockData[MoveBlock_InitialDirection] = blockData["direction"];
            }

            MoveBlock block = blockData.Target; 
            blockData["angle"] = blockData["targetAngle"] = blockData["homeAngle"] = angle;
            blockData["direction"] = Direction;

            float newSpeed = blockData.Get<float>("targetSpeed");
            newSpeed = operation switch {
                Operation.Add => newSpeed + modifier,
                Operation.Subtract => newSpeed - modifier,
                Operation.Multiply => newSpeed * modifier,
                _ => newSpeed
            };

            blockData["targetSpeed"] = newSpeed;
            lastMoveBlock = block;
            block.X = X;
            block.Y = Y;
        }

        private IEnumerator BreakBlock(DynData<MoveBlock> blockData, Coroutine orig, bool fast) {
            string breakSFX = fast ? CustomSFX.game_redirectMoveBlock_arrowblock_break_fast : SFX.game_04_arrowblock_break;
            Audio.Play(breakSFX, blockData.Target.Position);
            blockData.Get<SoundSource>("moveSfx").Stop();

            //state = MovementState.Breaking;
            blockData["speed"] = blockData["targetSpeed"] = 0f;
            blockData["angle"] = blockData["targetAngle"] = blockData.Get<float>("homeAngle");

            blockData.Target.StartShaking(0.2f);
            blockData.Target.StopPlayerRunIntoAnimation = true;

            float duration = fast ? 0f : 0.2f;
            float timer = 0f;
            while (timer < duration) {
                timer += Engine.DeltaTime;
                maskAlpha = Ease.BounceIn(timer / duration);
                yield return null;
            }

            // Absolutely cursed beyond belief
            IEnumerator controller;
            orig.Replace(controller = (IEnumerator) Activator.CreateInstance(t_MoveBlock_Controller, 4));
            f_MoveBlock_Controller_this.SetValue(controller, blockData.Target);
            blockData.Target.Add(orig);

            yield return null;
            maskAlpha = 0;
        }

        private IEnumerator RedirectRoutine(MoveBlock block, Coroutine orig) {
            DynData<MoveBlock> blockData = new DynData<MoveBlock>(block);
            float duration = 1f;

            block.MoveTo(Position);

            SoundSource moveSfx = blockData.Get<SoundSource>("moveSfx");
            moveSfx.Param("redirect_slowdown", 1f);

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
                float percent = timer / duration;
                maskAlpha = Ease.BounceIn(percent);
                yield return null;
            }

            block.StartShaking(0.18f);
            moveSfx.Param("redirect_slowdown", 0f);

            while (timer > 0) {
                timer -= Engine.DeltaTime;
                maskAlpha = Ease.BounceIn(timer / duration);
                yield return null;
            }

            // Absolutely cursed, starts the Controller routine after a certain number of yields
            IEnumerator controller;
            orig.Replace(controller = (IEnumerator) Activator.CreateInstance(t_MoveBlock_Controller, 3));
            f_MoveBlock_Controller_this.SetValue(controller, blockData.Target);
            blockData.Target.Add(orig);

            // Wait for the moveblock to continue before resetting
            if (OneUse)
                RemoveSelf();
        }

        public override void Render() {
            Draw.Rect(X - 1, Y - 1, Width + 2, Height + 2, Mask * maskAlpha);
            base.Render();

        }

        private class Icon : Entity {
            public Image Sprite;
            public Icon(Vector2 position, float rotation, string icon)
                : base(position) {
                Depth = Depths.Below;
                Add(Sprite = new Image(GFX.Game["objects/CommunalHelper/moveBlockRedirect/" + icon]));
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

            Logger.Log("CommunalHelper", "Replacing MoveBlock SFX with near-identical custom event that supports a \"redirect_slowdown\" param");
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
