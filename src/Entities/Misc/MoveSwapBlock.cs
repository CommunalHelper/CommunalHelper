using Celeste.Mod.Entities;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Entities {
    [TrackedAs(typeof(SwapBlock))]
    [CustomEntity("CommunalHelper/MoveSwapBlock")]
    public class MoveSwapBlock : SwapBlock {

        #region SwapBlock properties

        private static MethodInfo m_SwapBlock_MoveParticles = typeof(SwapBlock).GetMethod("MoveParticles", BindingFlags.NonPublic | BindingFlags.Instance);

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
        private Sprite middleGreen;
        private Sprite middleRed;
        private DisplacementRenderer.Burst burst;

        #endregion

        #region MoveBlock properties

        private enum MovementState {
            Idling,
            Moving,
            Breaking
        }

        private const float Accel = 300f;
        private const float MoveSpeed = 60f;
        private const float FastMoveSpeed = 75f;
        private const float SteerSpeed = Calc.Circle * 8f;
        private const float MaxAngle = Calc.EighthCircle;
        private const float NoSteerTime = 0.2f;
        private const float CrashTime = 0.15f;
        private const float CrashResetTime = 0.1f;

        private bool canSteer;
        private MoveBlock.Directions direction;
        private float homeAngle;
        private int angleSteerSign;
        private Vector2 startPosition;
        private MovementState state;
        private float moveSpeed;
        private float targetMoveSpeed;
        private float angle;
        private float targetAngle;
        private Player noSquish;
        private List<Image> topButton = new List<Image>();
        private List<Image> leftButton = new List<Image>();
        private List<Image> rightButton = new List<Image>();
        private List<MTexture> arrows = new List<MTexture>();
        private MoveBlockBorder border;
        private Color fillColor = Calc.HexToColor("4baaca");
        private SoundSource moveBlockSfx;
        private bool triggered;
        private Color idleBgFill = Calc.HexToColor("4baaca");
        private Color pressedBgFill = Calc.HexToColor("30b335");
        private Color breakingBgFill = Calc.HexToColor("cc2541");
        private float particleRemainder_Move;

        #endregion

        DynData<SwapBlock> swapBlockData;

        /* This is my jank workaround for not having to hook another thing in. If we just use SwapBlock but remove all of its functionality that is
         * public, we can just replace all of the public code with our own, and anything public is accessible to us as well as the player, so we
         * can just copy the code into this subclass and it should work identically. */
        public MoveSwapBlock(EntityData data, Vector2 offset) 
            : base(data, offset) {
            swapBlockData = new DynData<SwapBlock>(this);

            // Store private SwapBlock variables (that should not be modified) locally
            middleRed = swapBlockData.Get<Sprite>("middleRed");
            middleGreen = swapBlockData.Get<Sprite>("middleGreen");

            // Replaces SwapBlock.OnDash with MoveSwapBlock.OnDash
            Get<DashListener>().OnDash = OnDash;

            //Addition of SwapBlock properties.
            start = Position;
            end = data.Nodes[0] + offset;
            difference = end - start;

            moveRect = swapBlockData.Get<Rectangle>("moveRect");

            maxForwardSpeed = 360f * data.Float("SwapSpeedMult", 1f) / Vector2.Distance(start, end);
            startPosition = Position;

            direction = data.Enum("direction", MoveBlock.Directions.Left);
            canSteer = data.Bool("canSteer", defaultValue: true);
            switch (direction) {
                default:
                    homeAngle = (targetAngle = (angle = 0f));
                    angleSteerSign = 1;
                    break;
                case MoveBlock.Directions.Left:
                    homeAngle = (targetAngle = (angle = (float) Math.PI));
                    angleSteerSign = -1;
                    break;
                case MoveBlock.Directions.Up:
                    homeAngle = (targetAngle = (angle = -(float) Math.PI / 2f));
                    angleSteerSign = 1;
                    break;
                case MoveBlock.Directions.Down:
                    homeAngle = (targetAngle = (angle = (float) Math.PI / 2f));
                    angleSteerSign = -1;
                    break;
            }

            int tilesX = (int) Width / 8;
            int tilesY = (int) Height / 8;
            MTexture baseTexture = GFX.Game["objects/moveBlock/base"];
            MTexture buttonTexture = GFX.Game["objects/moveBlock/button"];
            if (canSteer && (direction == MoveBlock.Directions.Left || direction == MoveBlock.Directions.Right)) {
                for (int x = 0; x < tilesX; x++) {
                    int offsetX = (x != 0) ? ((x < tilesX - 1) ? 1 : 2) : 0;
                    AddImage(buttonTexture.GetSubtexture(offsetX * 8, 0, 8, 8), new Vector2(x * 8, -4f), 0f, new Vector2(1f, 1f), topButton);
                }
                baseTexture = GFX.Game["objects/moveBlock/base_h"];
            } else if (canSteer && (direction == MoveBlock.Directions.Up || direction == MoveBlock.Directions.Down)) {
                for (int y = 0; y < tilesY; y++) {
                    int offsetY = (y != 0) ? ((y < tilesY - 1) ? 1 : 2) : 0;
                    AddImage(buttonTexture.GetSubtexture(offsetY * 8, 0, 8, 8), new Vector2(-4f, y * 8), (float) Math.PI / 2f, new Vector2(1f, -1f), leftButton);
                    AddImage(buttonTexture.GetSubtexture(offsetY * 8, 0, 8, 8), new Vector2((tilesX - 1) * 8 + 4, y * 8), (float) Math.PI / 2f, new Vector2(1f, 1f), rightButton);
                }
                baseTexture = GFX.Game["objects/moveBlock/base_v"];
            }
            arrows = GFX.Game.GetAtlasSubtextures("objects/CommunalHelper/moveSwapBlock/arrow");
            Add(moveBlockSfx = new SoundSource());
            Add(new Coroutine(Controller()));

        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            scene.Add(border = new MoveBlockBorder(this));
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
                        Audio.Position(swapBlockData.Get<EventInstance>("moveSfx"), Center);
                        EventInstance returnSfx = swapBlockData.Get<EventInstance>("returnSfx");
                        Audio.Position(returnSfx, Center);
                        if (Position == start && target == 0) {
                            Audio.SetParameter(returnSfx, "end", 1f);
                            Audio.Play(SFX.game_05_swapblock_return_end, Center);
                        } else if (Position == end && target == 1) {
                            Audio.Play(SFX.game_05_swapblock_move_end, Center);
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

        private void OnDash(Vector2 direction) {
            Swapping = (lerp <= 1f && lerp >= 0f);
            target ^= 1;
            burst = (Scene as Level).Displacement.AddBurst(Center, 0.2f, 0f, 16f);
            if (lerp >= 0.2f) {
                speed = maxForwardSpeed;
            } else {
                speed = MathHelper.Lerp(maxForwardSpeed * 0.333f, maxForwardSpeed, lerp / 0.2f);
            }

            EventInstance returnSfx = swapBlockData.Get<EventInstance>("returnSfx");
            EventInstance moveSfx = swapBlockData.Get<EventInstance>("moveSfx");
            Audio.Stop(returnSfx);
            Audio.Stop(moveSfx);
            if (!Swapping)
                swapBlockData["returnSfx"] = Audio.Play(SFX.game_05_swapblock_move_end, Center);
            else
                swapBlockData["moveSfx"] = Audio.Play(SFX.game_05_swapblock_move, Center);
            

        }

        private void MoveParticles(Vector2 normal) =>
            m_SwapBlock_MoveParticles.Invoke(this, new object[] { normal });

        #region MoveBlock Methods

        private IEnumerator Controller() {

            while (true) {
                triggered = false;
                state = MovementState.Idling;
                while (!triggered && !HasPlayerRider()) {
                    yield return null;
                }

                Audio.Play(SFX.game_04_arrowblock_activate, Position);
                state = MovementState.Moving;
                StartShaking(0.2f);
                ActivateParticles();
                yield return 0.2f;

                targetMoveSpeed = MoveSpeed;
                moveBlockSfx.Play(SFX.game_04_arrowblock_move_loop);
                moveBlockSfx.Param("arrow_stop", 0f);
                StopPlayerRunIntoAnimation = false;
                float crashTimer = CrashTime;
                float crashResetTimer = CrashResetTime;
                float noSteerTimer = NoSteerTime;
                while (true) {
                    if (canSteer) {
                        targetAngle = homeAngle;
                        bool hasPlayer = (direction != MoveBlock.Directions.Right && direction != 0) ? HasPlayerClimbing() : HasPlayerOnTop();
                        if (hasPlayer && noSteerTimer > 0f) {
                            noSteerTimer -= Engine.DeltaTime;
                        }
                        if (hasPlayer) {
                            if (noSteerTimer <= 0f) {
                                if (direction == MoveBlock.Directions.Right || direction == MoveBlock.Directions.Left) {
                                    targetAngle = homeAngle + MaxAngle * angleSteerSign * Input.MoveY.Value;
                                } else {
                                    targetAngle = homeAngle + MaxAngle * angleSteerSign * Input.MoveX.Value;
                                }
                            }
                        } else {
                            noSteerTimer = 0.2f;
                        }
                    }

                    if (Scene.OnInterval(0.02f)) {
                        MoveParticles();
                    }

                    moveSpeed = Calc.Approach(moveSpeed, targetMoveSpeed, Accel * Engine.DeltaTime);
                    angle = Calc.Approach(angle, targetAngle, SteerSpeed * Engine.DeltaTime);

                    Vector2 vector = Calc.AngleToVector(angle, moveSpeed) * Engine.DeltaTime;
                    bool shouldBreak;
                    if (direction == MoveBlock.Directions.Right || direction == MoveBlock.Directions.Left) {
                        shouldBreak = MoveCheck(vector.XComp());

                        noSquish = Scene.Tracker.GetEntity<Player>();
                        MoveVCollideSolids(vector.Y, thruDashBlocks: false);
                        noSquish = null;
                        if (Scene.OnInterval(0.03f)) {
                            if (vector.Y > 0f) {
                                ScrapeParticles(Vector2.UnitY);
                            } else if (vector.Y < 0f) {
                                ScrapeParticles(-Vector2.UnitY);
                            }
                        }
                    } else {
                        shouldBreak = MoveCheck(vector.YComp());
                        noSquish = Scene.Tracker.GetEntity<Player>();
                        MoveHCollideSolids(vector.X, thruDashBlocks: false);
                        noSquish = null;
                        if (Scene.OnInterval(0.03f)) {
                            if (vector.X > 0f) {
                                ScrapeParticles(Vector2.UnitX);
                            } else if (vector.X < 0f) {
                                ScrapeParticles(-Vector2.UnitX);
                            }
                        }
                        if (direction == MoveBlock.Directions.Down && Top > SceneAs<Level>().Bounds.Bottom + 32) {
                            shouldBreak = true;
                        }
                    }

                    if (shouldBreak) {
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

                Audio.Play(SFX.game_04_arrowblock_break, Position);
                moveBlockSfx.Stop();
                state = MovementState.Breaking;
                moveSpeed = (targetMoveSpeed = 0f);
                angle = (targetAngle = homeAngle);

                StartShaking(0.2f);
                StopPlayerRunIntoAnimation = true;
                yield return 0.2f;

                BreakParticles();
                List<MoveBlockDebris> debrisList = new List<MoveBlockDebris>();
                for (int i = 0; i < Width; i += 8) {
                    for (int j = 0; j < Height; j += 8) {
                        Vector2 value = new Vector2(i + 4f, j + 4f);
                        MoveBlockDebris debris = Engine.Pooler.Create<MoveBlockDebris>().Init(Position + value, Center, startPosition + value);
                        debrisList.Add(debris);
                        Scene.Add(debris);
                    }
                }

                MoveStaticMovers(startPosition - Position);
                DisableStaticMovers();

                moveRect.X = (int) startPosition.X;
                moveRect.Y = (int) startPosition.Y;
                Visible = (Collidable = false);
                yield return 2.2f;

                foreach (MoveBlockDebris debris in debrisList) {
                    debris.StopMoving();
                }

                while (CollideCheck<Actor>() || CollideCheck<Solid>()) {
                    yield return null;
                }

                Collidable = true;
                EventInstance instance = Audio.Play(SFX.game_04_arrowblock_reform_begin, debrisList[0].Position);
                Coroutine routine = new Coroutine(SoundFollowsDebrisCenter(instance, debrisList));
                Add(routine);
                foreach (MoveBlockDebris debris in debrisList) {
                    debris.StartShaking();
                }
                yield return 0.2f;

                foreach (MoveBlockDebris debris in debrisList) {
                    debris.ReturnHome(0.65f);
                }
                yield return 0.6f;

                routine.RemoveSelf();
                foreach (MoveBlockDebris debris in debrisList) {
                    debris.RemoveSelf();
                }

                Audio.Play(SFX.game_04_arrowblock_reappear, Position);
                Visible = true;
                EnableStaticMovers();
                moveSpeed = (targetMoveSpeed = 0f);
                angle = (targetAngle = homeAngle);
                noSquish = null;
                fillColor = Calc.HexToColor("4baaca");
                UpdateColors();
            }
        }

        private IEnumerator SoundFollowsDebrisCenter(EventInstance instance, List<MoveBlockDebris> debrisList) {
            while (true) {
                instance.getPlaybackState(out PLAYBACK_STATE pLAYBACK_STATE);
                if (pLAYBACK_STATE != PLAYBACK_STATE.STOPPED) {
                    Vector2 zero = Vector2.Zero;
                    foreach (MoveBlockDebris debris in debrisList) {
                        zero += debris.Position;
                    }
                    zero /= debrisList.Count;
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
            foreach (Image item in leftButton) {
                item.Color = fillColor;
            }
            foreach (Image item in rightButton) {
                item.Color = fillColor;
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
            bool vertical = direction == MoveBlock.Directions.Down || direction == MoveBlock.Directions.Up;
            bool left = (!canSteer || !vertical) && !CollideCheck<Player>(Position - Vector2.UnitX);
            bool right = (!canSteer || !vertical) && !CollideCheck<Player>(Position + Vector2.UnitX);
            bool top = (!canSteer | vertical) && !CollideCheck<Player>(Position - Vector2.UnitY);
            if (left) {
                SceneAs<Level>().ParticlesBG.Emit(MoveBlock.P_Activate, (int) (Height / 2f), CenterLeft, Vector2.UnitY * (Height - 4f) * 0.5f, (float) Math.PI);
            }
            if (right) {
                SceneAs<Level>().ParticlesBG.Emit(MoveBlock.P_Activate, (int) (Height / 2f), CenterRight, Vector2.UnitY * (Height - 4f) * 0.5f, 0f);
            }
            if (top) {
                SceneAs<Level>().ParticlesBG.Emit(MoveBlock.P_Activate, (int) (Width / 2f), TopCenter, Vector2.UnitX * (Width - 4f) * 0.5f, -(float) Math.PI / 2f);
            }
            SceneAs<Level>().ParticlesBG.Emit(MoveBlock.P_Activate, (int) (Width / 2f), BottomCenter, Vector2.UnitX * (Width - 4f) * 0.5f, (float) Math.PI / 2f);
        }

        private void BreakParticles() {
            Vector2 center = Center;
            for (int x = 0; x < Width; x += 4) {
                for (int y = 0; y < Height; y += 4) {
                    Vector2 vector = Position + new Vector2(2 + x, 2 + y);
                    SceneAs<Level>().Particles.Emit(MoveBlock.P_Break, 1, vector, Vector2.One * 2f, (vector - center).Angle());
                }
            }
        }

        private void MoveParticles() {
            Vector2 position;
            Vector2 positionRange;
            float angle;
            float num2;
            if (direction == MoveBlock.Directions.Right) {
                position = CenterLeft + Vector2.UnitX;
                positionRange = Vector2.UnitY * (Height - 4f);
                angle = (float) Math.PI;
                num2 = Height / 32f;
            } else if (direction == MoveBlock.Directions.Left) {
                position = CenterRight;
                positionRange = Vector2.UnitY * (Height - 4f);
                angle = 0f;
                num2 = Height / 32f;
            } else if (direction == MoveBlock.Directions.Down) {
                position = TopCenter + Vector2.UnitY;
                positionRange = Vector2.UnitX * (Width - 4f);
                angle = -(float) Math.PI / 2f;
                num2 = Width / 32f;
            } else {
                position = BottomCenter;
                positionRange = Vector2.UnitX * (Width - 4f);
                angle = (float) Math.PI / 2f;
                num2 = Width / 32f;
            }
            particleRemainder_Move += num2;
            int num3 = (int) particleRemainder_Move;
            particleRemainder_Move -= num3;
            positionRange *= 0.5f;
            if (num3 > 0) {
                SceneAs<Level>().ParticlesBG.Emit(MoveBlock.P_Move, num3, position, positionRange, angle);
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
            } else if (speed.Y != 0f) {
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
            }
            return false;
        }

        #endregion

        #region Hooks

        internal static void Load() {
            On.Celeste.SwapBlock.Render += SwapBlock_Render;
        }

        internal static void Unload() {
            On.Celeste.SwapBlock.Render -= SwapBlock_Render;
        }

        private static void SwapBlock_Render(On.Celeste.SwapBlock.orig_Render orig, SwapBlock self) {
            orig(self);

            if (self is MoveSwapBlock block) {
                foreach (Image image in block.leftButton) {
                    image.Render();
                }
                foreach (Image image in block.rightButton) {
                    image.Render();
                }
                foreach (Image image in block.topButton) {
                    image.Render();
                }

                if (block.state != MovementState.Breaking) {
                    int value = (int) Math.Floor(block.angle / ((float) Math.PI * 2f) * 8f + 0.5f);
                    block.arrows[Calc.Clamp(value, 0, 7)].DrawCentered(block.Center + new Vector2(1, 0), new Color(Color.Black, 0.25f));
                    block.arrows[Calc.Clamp(value, 0, 7)].DrawCentered(block.Center, block.Swapping ? Color.LightGreen : (block.moveSpeed == 0 ? Color.Red : Color.Goldenrod));
                } else {
                    GFX.Game["objects/moveBlock/x"].DrawCentered(block.Center);
                }
            }
        }

        #endregion

    }
}
