using Celeste.Mod.Entities;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Entities {
    [TrackedAs(typeof(SwapBlock))]
    [CustomEntity("CommunalHelper/MoveSwapBlock")]
    public class MoveSwapBlock : SwapBlock {
        #region SwapBlock properties
        private class PathRenderer : Entity {
            private MoveSwapBlock block;

            private MTexture pathTexture;

            private MTexture clipTexture = new MTexture();

            private float timer;

            public PathRenderer(MoveSwapBlock block)
                : base(block.Position) {
                this.block = block;
                Depth = 8999;
                pathTexture = GFX.Game["objects/swapblock/path" + ((block.difference.X == 0) ? "V" : "H")];
                timer = Calc.Random.NextFloat();
            }

            public override void Update() {
                base.Update();
                timer += Engine.DeltaTime * 4f;
            }

            public override void Render() {
                if (block.state != MovementState.Breaking) {
                    if (block.Theme != Themes.Moon) {
                        for (int i = block.moveRect.Left; i < block.moveRect.Right; i += pathTexture.Width) {
                            for (int j = block.moveRect.Top; j < block.moveRect.Bottom; j += pathTexture.Height) {
                                pathTexture.GetSubtexture(0, 0, Math.Min(pathTexture.Width, block.moveRect.Right - i), Math.Min(pathTexture.Height, block.moveRect.Bottom - j), clipTexture);
                                clipTexture.DrawCentered(new Vector2(i + clipTexture.Width / 2, j + clipTexture.Height / 2), Color.White);
                            }
                        }
                    }
                    float scale = 0.5f * (0.5f + ((float) Math.Sin(timer) + 1f) * 0.25f);
                    block.DrawBlockStyle(new Vector2(block.moveRect.X, block.moveRect.Y), block.moveRect.Width, block.moveRect.Height, block.nineSliceTarget, null, (block.Swapping ? Color.LightGreen : Color.White) * scale);
                }
            }
        }
        private Vector2 start;
        private Vector2 difference;
        private Vector2 end;
        private float lerp;
        private int target;
        private Rectangle moveRect;
        private float speed;
        private float maxForwardSpeed;
        //returnTimer removed because of ToggleSwap function
        private float redAlpha = 1f;
        private MTexture[,] nineSliceGreen;
        private MTexture[,] nineSliceRed;
        private MTexture[,] nineSliceTarget;
        private Sprite middleGreen;
        private Sprite middleRed;
        private PathRenderer path;
        private EventInstance moveSfx;
        private string moveSFX; //custom
        private EventInstance returnSfx;
        private string returnSFX; //custom
        private DisplacementRenderer.Burst burst;
        private float particlesRemainder_Swap;

        private int[] rectData;
        #endregion

        #region MoveBlock properties
        public enum Directions {
            Left,
            Right,
            Up,
            Down
        }

        private enum MovementState {
            Idling,
            Moving,
            Breaking
        }

        private class Border : Entity {
            public MoveSwapBlock Parent;

            public Border(MoveSwapBlock parent) {
                Parent = parent;
                Depth = 1;
            }

            public override void Update() {
                if (Parent.Scene != Scene) {
                    RemoveSelf();
                }
                base.Update();
            }

            public override void Render() {
                Draw.Rect(Parent.X + Parent.Shake.X - 1f, Parent.Y + Parent.Shake.Y - 1f, Parent.Width + 2f, Parent.Height + 2f, Color.Black);
            }
        }

        [Pooled]
        private class Debris : Actor {
            private Image sprite;

            private Vector2 home;

            private Vector2 speed;

            private bool shaking;

            private bool returning;

            private float returnEase;

            private float returnDuration;

            private SimpleCurve returnCurve;

            private bool firstHit;

            private float alpha;

            private Collision onCollideH;

            private Collision onCollideV;

            private float spin;

            public Debris()
                : base(Vector2.Zero) {
                Tag = Tags.TransitionUpdate;
                Collider = new Hitbox(4f, 4f, -2f, -2f);
                Add(sprite = new Image(Calc.Random.Choose(GFX.Game.GetAtlasSubtextures("objects/moveblock/debris"))));
                sprite.CenterOrigin();
                sprite.FlipX = Calc.Random.Chance(0.5f);
                onCollideH = delegate {
                    speed.X = (0f - speed.X) * 0.5f;
                };
                onCollideV = delegate {
                    if (firstHit || speed.Y > 50f) {
                        Audio.Play("event:/game/general/debris_stone", Position, "debris_velocity", Calc.ClampedMap(speed.Y, 0f, 600f));
                    }
                    if (speed.Y > 0f && speed.Y < 40f) {
                        speed.Y = 0f;
                    } else {
                        speed.Y = (0f - speed.Y) * 0.25f;
                    }
                    firstHit = false;
                };
            }

            protected override void OnSquish(CollisionData data) {
            }

            public Debris Init(Vector2 position, Vector2 center, Vector2 returnTo) {
                Collidable = true;
                Position = position;
                speed = (position - center).SafeNormalize(60f + Calc.Random.NextFloat(60f));
                home = returnTo;
                sprite.Position = Vector2.Zero;
                sprite.Rotation = Calc.Random.NextAngle();
                returning = false;
                shaking = false;
                sprite.Scale.X = 1f;
                sprite.Scale.Y = 1f;
                sprite.Color = Color.White;
                alpha = 1f;
                firstHit = false;
                spin = Calc.Random.Range(3.49065852f, 10.4719753f) * Calc.Random.Choose(1, -1);
                return this;
            }

            public override void Update() {
                base.Update();
                if (!returning) {
                    if (Collidable) {
                        speed.X = Calc.Approach(speed.X, 0f, Engine.DeltaTime * 100f);
                        if (!OnGround()) {
                            speed.Y += 400f * Engine.DeltaTime;
                        }
                        MoveH(speed.X * Engine.DeltaTime, onCollideH);
                        MoveV(speed.Y * Engine.DeltaTime, onCollideV);
                    }
                    if (shaking && Scene.OnInterval(0.05f)) {
                        sprite.X = -1 + Calc.Random.Next(3);
                        sprite.Y = -1 + Calc.Random.Next(3);
                    }
                } else {
                    Position = returnCurve.GetPoint(Ease.CubeOut(returnEase));
                    returnEase = Calc.Approach(returnEase, 1f, Engine.DeltaTime / returnDuration);
                    sprite.Scale = Vector2.One * (1f + returnEase * 0.5f);
                }
                if ((Scene as Level).Transitioning) {
                    alpha = Calc.Approach(alpha, 0f, Engine.DeltaTime * 4f);
                    sprite.Color = Color.White * alpha;
                }
                sprite.Rotation += spin * Calc.ClampedMap(Math.Abs(speed.Y), 50f, 150f) * Engine.DeltaTime;
            }

            public void StopMoving() {
                Collidable = false;
            }

            public void StartShaking() {
                shaking = true;
            }

            public void ReturnHome(float duration) {
                if (Scene != null) {
                    Camera camera = (Scene as Level).Camera;
                    if (X < camera.X) {
                        X = camera.X - 8f;
                    }
                    if (Y < camera.Y) {
                        Y = camera.Y - 8f;
                    }
                    if (X > camera.X + 320f) {
                        X = camera.X + 320f + 8f;
                    }
                    if (Y > camera.Y + 180f) {
                        Y = camera.Y + 180f + 8f;
                    }
                }
                returning = true;
                returnEase = 0f;
                returnDuration = duration;
                Vector2 vector = (home - Position).SafeNormalize();
                Vector2 control = (Position + home) / 2f + new Vector2(vector.Y, 0f - vector.X) * (Calc.Random.NextFloat(16f) + 16f) * Calc.Random.Facing();
                returnCurve = new SimpleCurve(Position, home, control);
            }
        }

        private float Accel = 300f;
        private float MoveSpeed = 60f;
        private float FastMoveSpeed = 75f;
        private float SteerSpeed = (float) Math.PI * 16f;
        private float MaxAngle = (float) Math.PI / 4f;
        private float NoSteerTime = 0.2f;
        private float CrashTime = 0.15f;
        private float CrashResetTime = 0.1f;
        private float RegenTime = 3f;
        private bool canSteer;
        private Directions direction;
        private float homeAngle;
        private int angleSteerSign;
        private Vector2 startPosition;
        private MovementState state;
        private bool leftPressed;
        private bool rightPressed;
        private bool topPressed;
        private float moveSpeed;
        private float targetMoveSpeed;
        private float angle;
        private float targetAngle;
        private Player noSquish;
        private List<Image> body = new List<Image>();
        private List<Image> topButton = new List<Image>();
        private List<Image> leftButton = new List<Image>();
        private List<Image> rightButton = new List<Image>();
        private List<MTexture> arrows = new List<MTexture>();
        private Border border;
        private Color fillColor = Calc.HexToColor("4baaca");
        private float flash;
        private SoundSource moveBlockSfx;
        private bool triggered;
        private Color idleBgFill = Calc.HexToColor("4baaca");
        private Color pressedBgFill = Calc.HexToColor("30b335");
        private Color breakingBgFill = Calc.HexToColor("cc2541");
        private float particleRemainder_Move;

        private bool flag2; //When set to true this breaks the moveswapblock
        #endregion

        /* This is my jank workaround for not having to hook another thing in. If we just use SwapBlock but remove all of its functionality that is
         * public, we can just replace all of the public code with our own, and anything public is accessible to us as well as the player, so we
         * can just copy the code into this subclass and it should work identically. */
        public MoveSwapBlock(EntityData data, Vector2 offset) 
            : base(data, offset) {
            DynData<SwapBlock> baseData = new DynData<SwapBlock>(this);
            //This is faster than regetting the subtextures and removing the previous ones.
            nineSliceGreen = baseData.Get<MTexture[,]>("nineSliceGreen");
            nineSliceRed = baseData.Get<MTexture[,]>("nineSliceRed");
            nineSliceTarget = baseData.Get<MTexture[,]>("nineSliceTarget");
            //Removes all previously added sprites to SwapBlock, we won't be able to access these where we need to because they are private.
            //So instead of calling DynData each time we just replace them here and then use Render with our private sprites.
            Remove(baseData.Get<Sprite>("middleGreen"));
            Remove(baseData.Get<Sprite>("middleRed"));
            if (Theme == Themes.Normal) {
                Add(middleGreen = GFX.SpriteBank.Create("swapBlockLight"));
                Add(middleRed = GFX.SpriteBank.Create("swapBlockLightRed"));
            } else if (Theme == Themes.Moon) {
                Add(middleGreen = GFX.SpriteBank.Create("swapBlockLightMoon"));
                Add(middleRed = GFX.SpriteBank.Create("swapBlockLightRedMoon"));
            }
            //Removes OnDash and adds OnDash with custom speeds and changes it to ToggleSwap.
            Remove(Get<DashListener>());
            Add(new DashListener { OnDash = CustomOnDash });
            //Addition of SwapBlock properties.
            start = Position;
            end = data.Nodes[0] + offset;
            difference = end - start;
            int num = (int) MathHelper.Min(X, end.X);
            int num2 = (int) MathHelper.Min(Y, end.Y);
            int num3 = (int) MathHelper.Max(X + Width, end.X + Width);
            int num4 = (int) MathHelper.Max(Y + Height, end.Y + Height);
            moveRect = new Rectangle(num, num2, num3 - num, num4 - num2);
            rectData = new int[] { num, num2, num3 - num, num4 - num2 };
            maxForwardSpeed = 360f * data.Float("SwapSpeedMult", 1f) / Vector2.Distance(start, end);
            flag2 = false;
            startPosition = Position;
            direction = data.Enum("direction", Directions.Left);
            canSteer = data.Bool("canSteer", defaultValue: true);
            switch (direction) {
                default:
                    homeAngle = (targetAngle = (angle = 0f));
                    angleSteerSign = 1;
                    break;
                case Directions.Left:
                    homeAngle = (targetAngle = (angle = (float) Math.PI));
                    angleSteerSign = -1;
                    break;
                case Directions.Up:
                    homeAngle = (targetAngle = (angle = -(float) Math.PI / 2f));
                    angleSteerSign = 1;
                    break;
                case Directions.Down:
                    homeAngle = (targetAngle = (angle = (float) Math.PI / 2f));
                    angleSteerSign = -1;
                    break;
            }
            int numA = (int) Width / 8;
            int numB = (int) Height / 8;
            MTexture mTexture = GFX.Game["objects/moveBlock/base"];
            MTexture mTexture2 = GFX.Game["objects/moveBlock/button"];
            if (canSteer && (direction == Directions.Left || direction == Directions.Right)) {
                for (int i = 0; i < numA; i++) {
                    int numC = (i != 0) ? ((i < numA - 1) ? 1 : 2) : 0;
                    AddImage(mTexture2.GetSubtexture(numC * 8, 0, 8, 8), new Vector2(i * 8, -4f), 0f, new Vector2(1f, 1f), topButton);
                }
                mTexture = GFX.Game["objects/moveBlock/base_h"];
            } else if (canSteer && (direction == Directions.Up || direction == Directions.Down)) {
                for (int j = 0; j < numB; j++) {
                    int numD = (j != 0) ? ((j < numB - 1) ? 1 : 2) : 0;
                    AddImage(mTexture2.GetSubtexture(numD * 8, 0, 8, 8), new Vector2(-4f, j * 8), (float) Math.PI / 2f, new Vector2(1f, -1f), leftButton);
                    AddImage(mTexture2.GetSubtexture(numD * 8, 0, 8, 8), new Vector2((numA - 1) * 8 + 4, j * 8), (float) Math.PI / 2f, new Vector2(1f, 1f), rightButton);
                }
                mTexture = GFX.Game["objects/moveBlock/base_v"];
            }
            arrows = GFX.Game.GetAtlasSubtextures("objects/CommunalHelper/moveSwapBlock/arrow");
            Add(moveBlockSfx = new SoundSource());
            Add(new Coroutine(Controller()));

        }

        //For the following overriden methods, we're just copying the code we need and calling the base.base.Method() instead.
        public override void Awake(Scene scene) {
            //Calls Solid.Awake(scene)
            var ptr = typeof(Solid).GetMethod("Awake").MethodHandle.GetFunctionPointer();
            var baseFunc = (Action<Scene>) Activator.CreateInstance(typeof(Action<Scene>), this, ptr);
            baseFunc(scene);
            //Adds SwapBlock-Path and MoveBlock-Border
            scene.Add(path = new PathRenderer(this));
            scene.Add(border = new Border(this));

        }
        public override void Removed(Scene scene) {
            //Calls Solid.Removed(scene)
            var ptr = typeof(Solid).GetMethod("Removed").MethodHandle.GetFunctionPointer();
            var baseFunc = (Action<Scene>) Activator.CreateInstance(typeof(Action<Scene>), this, ptr);
            baseFunc(scene);
            Audio.Stop(moveSfx);
            Audio.Stop(returnSfx);
        }

        public override void SceneEnd(Scene scene) {
            //Calls Solid.SceneEnd(scene)
            var ptr = typeof(Solid).GetMethod("SceneEnd").MethodHandle.GetFunctionPointer();
            var baseFunc = (Action<Scene>) Activator.CreateInstance(typeof(Action<Scene>), this, ptr);
            baseFunc(scene);
            Audio.Stop(moveSfx);
            Audio.Stop(returnSfx);
        }

        public override void Update() {
            //calls Solid.Update(), we want to completely skip SwapBlock.Update and use our own
            var ptr = typeof(Solid).GetMethod("Update").MethodHandle.GetFunctionPointer();
            var baseUpdate = (Action) Activator.CreateInstance(typeof(Action), this, ptr);
            baseUpdate();

            if (state != MovementState.Breaking) {
                //Default SwapBlock.Update() code, we need this here as we are modifying the variables to be private in this class.
                if (burst != null) {
                    burst.Position = Center;
                }
                redAlpha = Calc.Approach(redAlpha, (target != 1) ? 1 : 0, Engine.DeltaTime * 32f);
                if (target == 0 && lerp == 0f) {
                    middleRed.SetAnimationFrame(0);
                    middleGreen.SetAnimationFrame(0);
                }
                speed = Calc.Approach(speed, maxForwardSpeed, maxForwardSpeed / 0.2f * Engine.DeltaTime);
                Direction = difference * (target == 0 && Swapping ? -1 : 1);
                float num = lerp;
                lerp = Calc.Approach(lerp, target, speed * Engine.DeltaTime);
                if (lerp == num) {
                    start = target == 1 ? Position - difference : Position;
                    end = target == 0 ? Position + difference : Position;
                    moveRect.X = (int) Math.Min(start.X, end.X);
                    moveRect.Y = (int) Math.Min(start.Y, end.Y);
                } else {

                    Vector2 liftSpeed = difference * maxForwardSpeed;
                    Vector2 position = Position;
                    if (lerp < num) {
                        liftSpeed *= -1f;
                    }
                    if (target == 1 && Scene.OnInterval(0.02f)) {
                        MoveParticles(difference);
                    }

                    MoveTo(Vector2.Lerp(start, end, lerp), liftSpeed);
                    if (position != Position) {
                        Audio.Position(moveSfx, Center);
                        Audio.Position(returnSfx, Center);
                        if (Position == start && target == 0) {
                            Audio.SetParameter(returnSfx, "end", 1f);
                            Audio.Play("event:/game/05_mirror_temple/swapblock_return_end", Center);
                        } else if (Position == end && target == 1) {
                            Audio.Play("event:/game/05_mirror_temple/swapblock_move_end", Center);
                        }
                    }
                }
                if (Swapping && (lerp >= 1f || lerp <= 0f)) {
                    Swapping = false;
                }
                StopPlayerRunIntoAnimation = (lerp <= 0f || lerp >= 1f);
            } else {
                target = 0;
                lerp = 0;
                Add(new Coroutine(DelayReturnPosition()));
            }
        }

        private IEnumerator DelayReturnPosition() {
            yield return 0.25f;
            start = startPosition;
            end = startPosition + difference;
            Position = start;
        }

        private void CustomOnDash(Vector2 direction) {
            Swapping = (lerp <= 1f && lerp >= 0f);
            target = (target + 1) % 2;
            burst = (Scene as Level).Displacement.AddBurst(Center, 0.2f, 0f, 16f);
            if (lerp >= 0.2f) {
                speed = maxForwardSpeed;
            } else {
                speed = MathHelper.Lerp(maxForwardSpeed * 0.333f, maxForwardSpeed, lerp / 0.2f);
            }
            Audio.Stop(returnSfx);
            Audio.Stop(moveSfx);
            if (!Swapping) {
                returnSfx = Audio.Play("event:/game/05_mirror_temple/swapblock_move_end", Center);
            } else {
                moveSfx = Audio.Play("event:/game/05_mirror_temple/swapblock_move", Center);
            }

        }

        public override void Render() {

            Vector2 vector = Position + Shake;
            if (lerp != target && speed > 0f) {
                Vector2 value = difference.SafeNormalize();
                if (target == 1) {
                    value *= -1f;
                }
                float num2 = speed / maxForwardSpeed;
                float num3 = 16f * num2;
                for (int i = 2; i < num3; i += 2) {
                    DrawBlockStyle(vector + value * i, Width, Height, nineSliceGreen, middleGreen, Color.White * (1f - i / num3));
                }
            }
            if (redAlpha < 1f) {
                DrawBlockStyle(vector, Width, Height, nineSliceGreen, middleGreen, Color.White);
            }
            if (redAlpha > 0f) {
                DrawBlockStyle(vector, Width, Height, nineSliceRed, middleRed, Color.White * redAlpha);
            }

            foreach (Image item in leftButton) {
                item.Render();
            }
            foreach (Image item2 in rightButton) {
                item2.Render();
            }
            foreach (Image item3 in topButton) {
                item3.Render();
            }
            if (state != MovementState.Breaking) {
                int value = (int) Math.Floor(angle / ((float) Math.PI * 2f) * 8f + 0.5f);
                arrows[Calc.Clamp(value, 0, 7)].DrawCentered(Center + new Vector2(1, 0), new Color(Color.Black, 0.25f));
                arrows[Calc.Clamp(value, 0, 7)].DrawCentered(Center, Swapping ? Color.LightGreen : (moveSpeed == 0 ? Color.Red : Color.Goldenrod));
            } else {
                GFX.Game["objects/moveBlock/x"].DrawCentered(Center);
            }

        }

        #region SwapBlock Methods
        private void MoveParticles(Vector2 normal) {
            Vector2 position;
            Vector2 positionRange;
            float direction;
            float num;
            if (normal.X > 0f) {
                position = CenterLeft;
                positionRange = Vector2.UnitY * (Height - 6f);
                direction = (float) Math.PI;
                num = Math.Max(2f, Height / 14f);
            } else if (normal.X < 0f) {
                position = CenterRight;
                positionRange = Vector2.UnitY * (Height - 6f);
                direction = 0f;
                num = Math.Max(2f, Height / 14f);
            } else if (normal.Y > 0f) {
                position = TopCenter;
                positionRange = Vector2.UnitX * (Width - 6f);
                direction = -(float) Math.PI / 2f;
                num = Math.Max(2f, Width / 14f);
            } else {
                position = BottomCenter;
                positionRange = Vector2.UnitX * (Width - 6f);
                direction = (float) Math.PI / 2f;
                num = Math.Max(2f, Width / 14f);
            }
            particlesRemainder_Swap += num;
            int num2 = (int) particlesRemainder_Swap;
            particlesRemainder_Swap -= num2;
            positionRange *= 0.5f;
            SceneAs<Level>().Particles.Emit(P_Move, num2, position, positionRange, direction);
        }

        private void DrawBlockStyle(Vector2 pos, float width, float height, MTexture[,] ninSlice, Sprite middle, Color color) {
            int num = (int) (width / 8f);
            int num2 = (int) (height / 8f);
            ninSlice[0, 0].Draw(pos + new Vector2(0f, 0f), Vector2.Zero, color);
            ninSlice[2, 0].Draw(pos + new Vector2(width - 8f, 0f), Vector2.Zero, color);
            ninSlice[0, 2].Draw(pos + new Vector2(0f, height - 8f), Vector2.Zero, color);
            ninSlice[2, 2].Draw(pos + new Vector2(width - 8f, height - 8f), Vector2.Zero, color);
            for (int i = 1; i < num - 1; i++) {
                ninSlice[1, 0].Draw(pos + new Vector2(i * 8, 0f), Vector2.Zero, color);
                ninSlice[1, 2].Draw(pos + new Vector2(i * 8, height - 8f), Vector2.Zero, color);
            }
            for (int j = 1; j < num2 - 1; j++) {
                ninSlice[0, 1].Draw(pos + new Vector2(0f, j * 8), Vector2.Zero, color);
                ninSlice[2, 1].Draw(pos + new Vector2(width - 8f, j * 8), Vector2.Zero, color);
            }
            for (int k = 1; k < num - 1; k++) {
                for (int l = 1; l < num2 - 1; l++) {
                    ninSlice[1, 1].Draw(pos + new Vector2(k, l) * 8f, Vector2.Zero, color);
                }
            }
            if (middle != null) {
                middle.Color = color;
                middle.RenderPosition = pos + new Vector2(width / 2f, height / 2f);
                middle.Render();
            }
        }

        #endregion
        #region MoveBlock Methods
        private IEnumerator Controller() {

            while (true) {
                triggered = false;
                state = MovementState.Idling;
                while (!triggered && !HasPlayerRider()) {
                    yield return null;
                }
                Audio.Play("event:/game/04_cliffside/arrowblock_activate", Position);
                state = MovementState.Moving;
                StartShaking(0.2f);
                ActivateParticles();
                yield return 0.2f;
                targetMoveSpeed = MoveSpeed;
                moveBlockSfx.Play("event:/game/04_cliffside/arrowblock_move");
                moveBlockSfx.Param("arrow_stop", 0f);
                StopPlayerRunIntoAnimation = false;
                float crashTimer = 0.15f;
                float crashResetTimer = 0.1f;
                float noSteerTimer = 0.2f;
                while (true) {
                    if (canSteer) {
                        targetAngle = homeAngle;
                        bool flag = (direction != Directions.Right && direction != 0) ? HasPlayerClimbing() : HasPlayerOnTop();
                        if (flag && noSteerTimer > 0f) {
                            noSteerTimer -= Engine.DeltaTime;
                        }
                        if (flag) {
                            if (noSteerTimer <= 0f) {
                                if (direction == Directions.Right || direction == Directions.Left) {
                                    targetAngle = homeAngle + (float) Math.PI / 4f * angleSteerSign * Input.MoveY.Value;
                                } else {
                                    targetAngle = homeAngle + (float) Math.PI / 4f * angleSteerSign * Input.MoveX.Value;
                                }
                            }
                        } else {
                            noSteerTimer = 0.2f;
                        }
                    }
                    if (Scene.OnInterval(0.02f)) {
                        MoveParticles();
                    }
                    moveSpeed = Calc.Approach(moveSpeed, targetMoveSpeed, 300f * Engine.DeltaTime);
                    angle = Calc.Approach(angle, targetAngle, (float) Math.PI * 16f * Engine.DeltaTime);
                    Vector2 vec = Calc.AngleToVector(angle, moveSpeed) * Engine.DeltaTime;
                    if (direction == Directions.Right || direction == Directions.Left) {
                        flag2 = MoveCheck(vec.XComp());

                        noSquish = Scene.Tracker.GetEntity<Player>();
                        MoveVCollideSolids(vec.Y, thruDashBlocks: false);
                        noSquish = null;
                        if (Scene.OnInterval(0.03f)) {
                            if (vec.Y > 0f) {
                                ScrapeParticles(Vector2.UnitY);
                            } else if (vec.Y < 0f) {
                                ScrapeParticles(-Vector2.UnitY);
                            }
                        }
                    } else {
                        flag2 = MoveCheck(vec.YComp());
                        noSquish = Scene.Tracker.GetEntity<Player>();
                        MoveHCollideSolids(vec.X, thruDashBlocks: false);
                        noSquish = null;
                        if (Scene.OnInterval(0.03f)) {
                            if (vec.X > 0f) {
                                ScrapeParticles(Vector2.UnitX);
                            } else if (vec.X < 0f) {
                                ScrapeParticles(-Vector2.UnitX);
                            }
                        }
                        if (direction == Directions.Down && Top > SceneAs<Level>().Bounds.Bottom + 32) {
                            flag2 = true;
                        }
                    }
                    if (flag2) {
                        moveBlockSfx.Param("arrow_stop", 1f);
                        crashResetTimer = 0.1f;
                        if (!(crashTimer > 0f)) {
                            break;
                        }
                        crashTimer -= Engine.DeltaTime;
                    } else {
                        moveBlockSfx.Param("arrow_stop", 0f);
                        if (crashResetTimer > 0f) {
                            crashResetTimer -= Engine.DeltaTime;
                        } else {
                            crashTimer = 0.15f;
                        }
                    }
                    Level level = Scene as Level;
                    if (Left < level.Bounds.Left || Top < level.Bounds.Top || Right > level.Bounds.Right) {
                        break;
                    }
                    yield return null;
                }
                Audio.Play("event:/game/04_cliffside/arrowblock_break", Position);
                moveBlockSfx.Stop();
                state = MovementState.Breaking;
                moveSpeed = (targetMoveSpeed = 0f);
                angle = (targetAngle = homeAngle);
                StartShaking(0.2f);
                StopPlayerRunIntoAnimation = true;
                yield return 0.2f;
                BreakParticles();
                List<Debris> debris = new List<Debris>();
                for (int i = 0; i < Width; i += 8) {
                    for (int j = 0; j < Height; j += 8) {
                        Vector2 value = new Vector2(i + 4f, j + 4f);
                        Debris debris2 = Engine.Pooler.Create<Debris>().Init(Position + value, Center, startPosition + value);
                        debris.Add(debris2);
                        Scene.Add(debris2);
                    }
                }
                MoveStaticMovers(startPosition - Position);
                DisableStaticMovers();



                moveRect = new Rectangle(rectData[0], rectData[1], rectData[2], rectData[3]);
                Visible = (Collidable = false);
                yield return 2.2f;
                foreach (Debris item in debris) {
                    item.StopMoving();
                }
                while (CollideCheck<Actor>() || CollideCheck<Solid>()) {
                    yield return null;
                }
                Collidable = true;
                EventInstance instance = Audio.Play("event:/game/04_cliffside/arrowblock_reform_begin", debris[0].Position);
                Coroutine component;
                Coroutine routine = component = new Coroutine(SoundFollowsDebrisCenter(instance, debris));
                Add(component);
                foreach (Debris item2 in debris) {
                    item2.StartShaking();
                }
                yield return 0.2f;
                foreach (Debris item3 in debris) {
                    item3.ReturnHome(0.65f);
                }
                yield return 0.6f;
                routine.RemoveSelf();
                foreach (Debris item4 in debris) {
                    item4.RemoveSelf();
                }
                Audio.Play("event:/game/04_cliffside/arrowblock_reappear", Position);
                Visible = true;
                EnableStaticMovers();
                moveSpeed = (targetMoveSpeed = 0f);
                angle = (targetAngle = homeAngle);
                noSquish = null;
                fillColor = Calc.HexToColor("4baaca");
                UpdateColors();
                flash = 1f;
            }
        }

        private IEnumerator SoundFollowsDebrisCenter(EventInstance instance, List<Debris> debris) {
            while (true) {
                instance.getPlaybackState(out PLAYBACK_STATE pLAYBACK_STATE);
                if (pLAYBACK_STATE != PLAYBACK_STATE.STOPPED) {
                    Vector2 zero = Vector2.Zero;
                    foreach (Debris debri in debris) {
                        zero += debri.Position;
                    }
                    zero /= debris.Count;
                    Audio.Position(instance, zero);
                    yield return null;
                    continue;
                }
                break;
            }
        }

        public override void OnStaticMoverTrigger(StaticMover sm) {
            triggered = true;
        }

        private void UpdateColors() {
            Color value = Calc.HexToColor("7f92a3");
            if (state == MovementState.Moving) {
                value = Calc.HexToColor("7f92a3");
            } else if (state == MovementState.Breaking) {
                value = Calc.HexToColor("3c244a");
            }
            fillColor = Calc.HexToColor("6f98a6");
            foreach (Image item in topButton) {
                item.Color = fillColor;
            }
            foreach (Image item2 in leftButton) {
                item2.Color = fillColor;
            }
            foreach (Image item3 in rightButton) {
                item3.Color = fillColor;
            }
        }

        private void AddImage(MTexture tex, Vector2 position, float rotation, Vector2 scale, List<Image> addTo) {
            Image image = new Image(tex);
            image.Position = position + new Vector2(4f, 4f);
            image.CenterOrigin();
            image.Rotation = rotation;
            image.Scale = scale;
            Add(image);
            addTo?.Add(image);
        }

        private void ActivateParticles() {
            bool flag = direction == Directions.Down || direction == Directions.Up;
            bool num = (!canSteer || !flag) && !CollideCheck<Player>(Position - Vector2.UnitX);
            bool flag2 = (!canSteer || !flag) && !CollideCheck<Player>(Position + Vector2.UnitX);
            bool flag3 = (!canSteer | flag) && !CollideCheck<Player>(Position - Vector2.UnitY);
            if (num) {
                SceneAs<Level>().ParticlesBG.Emit(MoveBlock.P_Activate, (int) (Height / 2f), CenterLeft, Vector2.UnitY * (Height - 4f) * 0.5f, (float) Math.PI);
            }
            if (flag2) {
                SceneAs<Level>().ParticlesBG.Emit(MoveBlock.P_Activate, (int) (Height / 2f), CenterRight, Vector2.UnitY * (Height - 4f) * 0.5f, 0f);
            }
            if (flag3) {
                SceneAs<Level>().ParticlesBG.Emit(MoveBlock.P_Activate, (int) (Width / 2f), TopCenter, Vector2.UnitX * (Width - 4f) * 0.5f, -(float) Math.PI / 2f);
            }
            SceneAs<Level>().ParticlesBG.Emit(MoveBlock.P_Activate, (int) (Width / 2f), BottomCenter, Vector2.UnitX * (Width - 4f) * 0.5f, (float) Math.PI / 2f);
        }

        private void BreakParticles() {
            Vector2 center = Center;
            for (int i = 0; i < Width; i += 4) {
                for (int j = 0; j < Height; j += 4) {
                    Vector2 vector = Position + new Vector2(2 + i, 2 + j);
                    SceneAs<Level>().Particles.Emit(MoveBlock.P_Break, 1, vector, Vector2.One * 2f, (vector - center).Angle());
                }
            }
        }

        private void MoveParticles() {
            Vector2 position;
            Vector2 positionRange;
            float num;
            float num2;
            if (direction == Directions.Right) {
                position = CenterLeft + Vector2.UnitX;
                positionRange = Vector2.UnitY * (Height - 4f);
                num = (float) Math.PI;
                num2 = Height / 32f;
            } else if (direction == Directions.Left) {
                position = CenterRight;
                positionRange = Vector2.UnitY * (Height - 4f);
                num = 0f;
                num2 = Height / 32f;
            } else if (direction == Directions.Down) {
                position = TopCenter + Vector2.UnitY;
                positionRange = Vector2.UnitX * (Width - 4f);
                num = -(float) Math.PI / 2f;
                num2 = Width / 32f;
            } else {
                position = BottomCenter;
                positionRange = Vector2.UnitX * (Width - 4f);
                num = (float) Math.PI / 2f;
                num2 = Width / 32f;
            }
            particleRemainder_Move += num2;
            int num3 = (int) particleRemainder_Move;
            particleRemainder_Move -= num3;
            positionRange *= 0.5f;
            if (num3 > 0) {
                SceneAs<Level>().ParticlesBG.Emit(MoveBlock.P_Move, num3, position, positionRange, num);
            }
        }

        private void ScrapeParticles(Vector2 dir) {
            _ = Collidable;
            Collidable = false;
            if (dir.X != 0f) {
                float x = (!(dir.X > 0f)) ? (Left - 1f) : Right;
                for (int i = 0; i < Height; i += 8) {
                    Vector2 vector = new Vector2(x, Top + 4f + i);
                    if (Scene.CollideCheck<Solid>(vector)) {
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, vector);
                    }
                }
            } else {
                float y = (!(dir.Y > 0f)) ? (Top - 1f) : Bottom;
                for (int j = 0; j < Width; j += 8) {
                    Vector2 vector2 = new Vector2(Left + 4f + j, y);
                    if (Scene.CollideCheck<Solid>(vector2)) {
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, vector2);
                    }
                }
            }
            Collidable = true;
        }

        public override void MoveHExact(int move) {
            if (noSquish != null && ((move < 0 && noSquish.X < X) || (move > 0 && noSquish.X > X))) {
                while (move != 0 && noSquish.CollideCheck<Solid>(noSquish.Position + Vector2.UnitX * move)) {
                    move -= Math.Sign(move);
                }
            }
            base.MoveHExact(move);
        }

        public override void MoveVExact(int move) {
            if (noSquish != null && move < 0 && noSquish.Y <= Y) {
                while (move != 0 && noSquish.CollideCheck<Solid>(noSquish.Position + Vector2.UnitY * move)) {
                    move -= Math.Sign(move);
                }
            }
            base.MoveVExact(move);
        }

        private bool MoveCheck(Vector2 speed) {
            if (speed.X != 0f) {
                if (MoveHCollideSolids(speed.X, thruDashBlocks: false)) {
                    for (int i = 1; i <= 3; i++) {
                        for (int num = 1; num >= -1; num -= 2) {
                            Vector2 value = new Vector2(Math.Sign(speed.X), i * num);
                            if (!(CollideCheck<Solid>(Position) || CollideCheck<Solid>(Position + value))) {
                                MoveVExact(i * num);
                                MoveHExact(Math.Sign(speed.X));
                                return false;

                            }
                        }
                    }
                    return true;
                }
                return false;
            }
            if (speed.Y != 0f) {
                if (MoveVCollideSolids(speed.Y, thruDashBlocks: false)) {
                    for (int j = 1; j <= 3; j++) {
                        for (int num2 = 1; num2 >= -1; num2 -= 2) {
                            Vector2 value2 = new Vector2(j * num2, Math.Sign(speed.Y));
                            if (!CollideCheck<Solid>(Position + value2)) {
                                MoveHExact(j * num2);
                                MoveVExact(Math.Sign(speed.Y));
                                return false;
                            }
                        }
                    }
                    return true;
                }
                return false;
            }
            return false;
        }
        #endregion
    }
}
