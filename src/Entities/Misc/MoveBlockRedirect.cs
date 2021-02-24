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

        public MoveBlock.Directions Direction;
        public bool FastRedirect;
        private bool oneUse, deleteBlock;

        private float angle;
        private MoveBlock currentBlock;
        private float maskAlpha;
        private List<Image> borders;

        private MoveBlock lastMoveBlock;

        private Icon icon;

        public MoveBlockRedirect(EntityData data, Vector2 offset)
            : base(data.Position + offset) {
            Depth = Depths.Above;
            Collider = new Hitbox(data.Width, data.Height);

            FastRedirect = data.Bool("fastRedirect");
            oneUse = data.Bool("oneUse");
            deleteBlock = data.Bool("deleteBlock") || (operation == Operation.Multiply && modifier == 0f);

            operation = data.Enum("operation", Operation.Add);
            modifier = Math.Abs(data.Float("modifier"));

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
            string iconTexture = "arrow";
            startColor = DefaultColor;

            if (deleteBlock) {
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

        private static Vector2 MoveBlockDirectionToVector(MoveBlock.Directions dir, float factor = 1f) {
            Vector2 result = dir switch {
                MoveBlock.Directions.Up => -Vector2.UnitY,
                MoveBlock.Directions.Down => Vector2.UnitY,
                MoveBlock.Directions.Left => -Vector2.UnitX,
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
            if(deleteBlock) {
                block.Remove(block.Get<Coroutine>());
                block.Add(new Coroutine(BreakBlock(block, blockData)));
            } else {
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
            }
            block.X = X;
            block.Y = Y;
        }

        private IEnumerator BreakBlock(MoveBlock self, DynData<MoveBlock> blockData) {
            Audio.Play("event:/game/04_cliffside/arrowblock_break", self.Position);
            blockData.Get<SoundSource>("moveSfx").Stop();

            //state = MovementState.Breaking;
            blockData["speed"] = blockData["targetSpeed"] = 0f;
            blockData["angle"] = blockData["targetAngle"] = blockData.Get<float>("homeAngle");

            self.StartShaking(0.2f);
            self.StopPlayerRunIntoAnimation = true;
            yield return 0.2f;

            m_MoveBlock_BreakParticles.Invoke(self, new object[] { });
            Vector2 startPosition = blockData.Get<Vector2>("startPosition");
            List<MoveBlockDebris> debris = new List<MoveBlockDebris>();
            for (int i = 0; i < Width; i += 8) {
                for (int j = 0; j < Height; j += 8) {
                    Vector2 value = new Vector2(i + 4f, j + 4f);
                    MoveBlockDebris debris2 = Engine.Pooler.Create<MoveBlockDebris>().Init(Position + value, self.Center, startPosition + value);
                    debris.Add(debris2);
                    Scene.Add(debris2);
                }
            }

            self.MoveStaticMovers(startPosition - self.Position);
            self.DisableStaticMovers();
            self.Position = startPosition;
            self.Visible = (self.Collidable = false);
            currentBlock = null;
            yield return 2.2f;
            foreach (MoveBlockDebris item in debris) {
                item.StopMoving();
            }
            while (self.CollideCheck<Actor>() || self.CollideCheck<Solid>()) {
                yield return null;
            }
            self.Collidable = true;

            EventInstance instance = Audio.Play("event:/game/04_cliffside/arrowblock_reform_begin", debris[0].Position);
            Coroutine routine = new Coroutine(SoundFollowsDebrisCenter(instance, debris));
            
            self.Add(routine);
            foreach (MoveBlockDebris item2 in debris) {
                item2.StartShaking();
            }
            yield return 0.2f;
            foreach (MoveBlockDebris item3 in debris) {
                item3.ReturnHome(0.65f);
            }
            yield return 0.6f;
            routine.RemoveSelf();
            foreach (MoveBlockDebris item4 in debris) {
                item4.RemoveSelf();
            }

            Audio.Play("event:/game/04_cliffside/arrowblock_reappear", Position);
            self.Visible = true;
            self.EnableStaticMovers();
            blockData["speed"] = blockData["targetSpeed"] = 0f;
            blockData["angle"] = blockData["targetAngle"] = blockData.Get<float>("homeAngle");
            blockData["noSquish"] = null;
            blockData["fillColor"] = UsedColor; // same color, yes i know
            m_MoveBlock_UpdateColors.Invoke(self, new object[] { });
            blockData["flash"] = 1f;
            blockData["triggered"] = false;

            // "jump" back at the beginning of the Controller coroutine, cursed
            IEnumerator controller;
            self.Add(new Coroutine(controller = (IEnumerator) Activator.CreateInstance(t_MoveBlock_Controller, 0)));
            f_MoveBlock_Controller_this.SetValue(controller, self);
        }

        private IEnumerator SoundFollowsDebrisCenter(EventInstance instance, List<MoveBlockDebris> debris) {
            while (true) {
                instance.getPlaybackState(out PLAYBACK_STATE pLAYBACK_STATE);
                if (pLAYBACK_STATE == PLAYBACK_STATE.STOPPED) {
                    break;
                }
                Vector2 zero = Vector2.Zero;
                foreach (MoveBlockDebris debri in debris) {
                    zero += debri.Position;
                }
                zero /= debris.Count;
                Audio.Position(instance, zero);
                yield return null;
            }
        }

        private IEnumerator RedirectRoutine(MoveBlock block) {
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
            block.Add(new Coroutine(controller = (IEnumerator) Activator.CreateInstance(t_MoveBlock_Controller, 3)));
            f_MoveBlock_Controller_this.SetValue(controller, block);

            // Wait for the moveblock to continue before resetting
            yield return null;
            currentBlock = null;
            if (oneUse)
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
