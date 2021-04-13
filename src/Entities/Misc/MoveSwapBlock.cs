using Celeste.Mod.Entities;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
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
        private Vector2 end;
        private Vector2 difference;

        private bool doesReturn;
        private bool freezeOnSwap;

        private float maxForwardSpeed;

        private readonly Rectangle startingRect;

        private Sprite middleGreen;
        private Sprite middleRed;
        private Image middleOrange;

        private Image middleArrow;
        private MTexture middleCardinal;
        private MTexture middleDiagonal;

        private Entity path;

        #endregion

        #region MoveBlock properties

        private const float Accel = 300f;
        private const float MoveSpeed = 60f;
        private const float SteerSpeed = Calc.Circle * 8f;
        private const float MaxAngle = Calc.EighthCircle;
        private const float NoSteerTime = 0.2f;
        private const float CrashTime = 0.15f;
        private const float CrashResetTime = 0.1f;

        public enum MovementState {
            Idling,
            Moving,
            Breaking
        }

        public bool Triggered { get; set; }

        public MovementState State { get; protected set; }
        public MoveBlock.Directions MoveDirection { get; protected set; }

        private bool canSteer;

        private float angle;
        private float targetAngle;
        private float homeAngle;
        private int angleSteerSign;

        private Vector2 startPosition;

        private float moveSpeed;
        private float targetMoveSpeed;
        private float moveAcceleration;

        private bool moveSwapPoints;
        private Player noSquish;

        private bool leftPressed;
        private bool rightPressed;
        private bool topPressed;
        private List<Image> topButton = new List<Image>();
        private List<Image> leftButton = new List<Image>();
        private List<Image> rightButton = new List<Image>();

        private SoundSource moveBlockSfx;

        private float particleRemainder;

        #endregion

        protected DynData<SwapBlock> swapBlockData;

        private Chooser<MTexture> debrisTextures;

        public MoveSwapBlock(EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height, data.Nodes[0] + offset, Themes.Normal) {
            swapBlockData = new DynData<SwapBlock>(this);
            Theme = Themes.Moon; // base() gets Normal for the block texture, then we set to Moon to remove the path background

            doesReturn = data.Bool("returns", true);
            freezeOnSwap = data.Bool("freezeOnSwap", true);

            // Replaces SwapBlock.OnDash with MoveSwapBlock.OnDash if this block doesn't return
            DashListener listener = Get<DashListener>();
            Action<Vector2> orig_OnDash = listener.OnDash;
            listener.OnDash = (dir) => {
                if (State == MovementState.Breaking)
                    return;
                else if (doesReturn)
                    orig_OnDash(dir);
                else
                    OnDash(dir);
            };

            // We use some local variable to temporarily store private SwapBlock fields for less reflection
            start = Position;
            end = data.Nodes[0] + offset;
            difference = end - start;

            // Structs are value types
            startingRect = swapBlockData.Get<Rectangle>("moveRect");

            swapBlockData["maxForwardSpeed"] = maxForwardSpeed = 360f * data.Float("swapSpeedMultiplier", 1f) / Vector2.Distance(start, end);
            startPosition = Position;

            // Replace/Add SwapBlock textures
            middleCardinal = GFX.Game["objects/CommunalHelper/moveSwapBlock/midBlockCardinal"];
            middleDiagonal = GFX.Game["objects/CommunalHelper/moveSwapBlock/midBlockDiagonal"];
            Add(middleArrow = new Image(middleCardinal));
            middleArrow.CenterOrigin();

            Remove(swapBlockData.Get<Sprite>("middleGreen"), swapBlockData.Get<Sprite>("middleRed"));
            swapBlockData["middleGreen"] = middleGreen = CommunalHelperModule.SpriteBank.Create("swapBlockLight");
            swapBlockData["middleRed"] = middleRed = CommunalHelperModule.SpriteBank.Create("swapBlockLightRed");
            Add(middleGreen, middleRed);

            Add(middleOrange = new Image(GFX.Game["objects/CommunalHelper/moveSwapBlock/midBlockOrange"]));
            middleOrange.CenterOrigin();

            canSteer = data.Bool("canSteer", false);
            moveAcceleration = data.Float("moveAcceleration", Accel);
            MoveDirection = data.Enum("direction", MoveBlock.Directions.Left);
            switch (MoveDirection) {
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
            MTexture buttonTexture = GFX.Game["objects/moveBlock/button"];
            MTexture buttonPressedTexture = GFX.Game["objects/CommunalHelper/moveSwapBlock/buttonPressed"];
            if (canSteer && (MoveDirection == MoveBlock.Directions.Left || MoveDirection == MoveBlock.Directions.Right)) {
                for (int x = 0; x < tilesX; x++) {
                    int offsetX = (x != 0) ? ((x < tilesX - 1) ? 1 : 2) : 0;
                    AddImage(buttonTexture.GetSubtexture(offsetX * 8, 0, 8, 8), new Vector2(x * 8, -4f), 0f, new Vector2(1f, 1f), topButton);
                    AddImage(buttonPressedTexture.GetSubtexture(offsetX * 8, 0, 8, 8), new Vector2(x * 8, -4f), 0f, new Vector2(1f, 1f), topButton);
                }
            } else if (canSteer && (MoveDirection == MoveBlock.Directions.Up || MoveDirection == MoveBlock.Directions.Down)) {
                for (int y = 0; y < tilesY; y++) {
                    int offsetY = (y != 0) ? ((y < tilesY - 1) ? 1 : 2) : 0;
                    AddImage(buttonTexture.GetSubtexture(offsetY * 8, 0, 8, 8), new Vector2(-4f, y * 8), (float) Math.PI / 2f, new Vector2(1f, -1f), leftButton);
                    AddImage(buttonPressedTexture.GetSubtexture(offsetY * 8, 0, 8, 8), new Vector2(-4f, y * 8), (float) Math.PI / 2f, new Vector2(1f, -1f), leftButton);
                    AddImage(buttonTexture.GetSubtexture(offsetY * 8, 0, 8, 8), new Vector2((tilesX - 1) * 8 + 4, y * 8), (float) Math.PI / 2f, new Vector2(1f, 1f), rightButton);
                    AddImage(buttonPressedTexture.GetSubtexture(offsetY * 8, 0, 8, 8), new Vector2((tilesX - 1) * 8 + 4, y * 8), (float) Math.PI / 2f, new Vector2(1f, 1f), rightButton);
                }
            }
            UpdateColors();

            debrisTextures = new Chooser<MTexture>();
            foreach (MTexture debris in GFX.Game.GetAtlasSubtextures("objects/CommunalHelper/moveSwapBlock/debris"))
                debrisTextures.Add(debris, 1);
            debrisTextures.Add(GFX.Game["objects/CommunalHelper/moveSwapBlock/debrisRed00"], 0.3f);
            debrisTextures.Add(GFX.Game["objects/CommunalHelper/moveSwapBlock/debrisGreen00"], 0.05f);

            Add(moveBlockSfx = new SoundSource());
            Add(new Coroutine(Controller()));

            Add(new MoveBlockRedirectable(new DynamicData(this)) {
                Get_Speed = () => moveSpeed,
                Get_TargetSpeed = () => targetMoveSpeed,
                Get_MoveSfx = () => moveBlockSfx,
                Get_Direction = () => MoveDirection,
                Set_Direction = dir => MoveDirection = dir,
            });
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            path = swapBlockData.Get<Entity>("path");
        }

        // Called via IL Delegate
        public new void Update() {
            DisplacementRenderer.Burst burst = swapBlockData.Get<DisplacementRenderer.Burst>("burst");
            if (burst != null) {
                burst.Position = Center;
            }

            int target = swapBlockData.Get<int>("target");
            swapBlockData["redAlpha"] = Calc.Approach(swapBlockData.Get<float>("redAlpha"), (target != 1) ? 1 : 0, Engine.DeltaTime * 32f);

            float lerp = swapBlockData.Get<float>("lerp");
            if (lerp is 0f or 1f) {
                middleRed.SetAnimationFrame(0);
                middleGreen.SetAnimationFrame(0);
            }

            swapBlockData["speed"] = Calc.Approach(swapBlockData.Get<float>("speed"), maxForwardSpeed, maxForwardSpeed / 0.2f * Engine.DeltaTime);

            Direction = difference * (target == 0 && Swapping ? -1 : 1);

            float previousLerp = lerp;
            swapBlockData["lerp"] = lerp = Calc.Approach(lerp, target, swapBlockData.Get<float>("speed") * Engine.DeltaTime);
            if (lerp != previousLerp) {
                Vector2 liftSpeed = difference * maxForwardSpeed;
                Vector2 position = Position;
                if (lerp < previousLerp) {
                    liftSpeed *= -1f;
                }
                if (Scene.OnInterval(0.02f)) {
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
            StopPlayerRunIntoAnimation = (lerp is <= 0f or >= 1f);
        }

        private void OnDash(Vector2 dir) {
            float lerp = swapBlockData.Get<float>("lerp");
            Swapping = (lerp is <= 1f and >= 0f);
            swapBlockData["target"] = swapBlockData.Get<int>("target") ^ 1;
            swapBlockData["burst"] = (Scene as Level).Displacement.AddBurst(Center, 0.2f, 0f, 16f);
            if (lerp >= 0.2f) {
                swapBlockData["speed"] = maxForwardSpeed;
            } else {
                swapBlockData["speed"] = MathHelper.Lerp(maxForwardSpeed * 0.333f, maxForwardSpeed, lerp / 0.2f);
            }

            Audio.Stop(swapBlockData.Get<EventInstance>("returnSfx"));
            Audio.Stop(swapBlockData.Get<EventInstance>("moveSfx"));
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
                // Defined here because reasons
                Rectangle moveRect;

                Triggered = false;
                State = MovementState.Idling;
                while (!Triggered && !HasPlayerRider()) {
                    yield return null;
                }

                Audio.Play(SFX.game_04_arrowblock_activate, Position);
                State = MovementState.Moving;
                StartShaking(0.2f);
                ActivateParticles();
                yield return 0.2f;

                targetMoveSpeed = MoveSpeed;
                moveBlockSfx.Play(CustomSFX.game_redirectMoveBlock_arrowblock_move);
                moveBlockSfx.Param("arrow_stop", 0f);
                StopPlayerRunIntoAnimation = false;
                float crashTimer = CrashTime;
                float crashResetTimer = CrashResetTime;
                float noSteerTimer = NoSteerTime;
                while (true) {
                    if (!Swapping || !freezeOnSwap) {
                        if (canSteer) {
                            targetAngle = homeAngle;
                            bool hasPlayer = (MoveDirection is not MoveBlock.Directions.Right and not 0) ? HasPlayerClimbing() : HasPlayerOnTop();
                            if (hasPlayer && noSteerTimer > 0f) {
                                noSteerTimer -= Engine.DeltaTime;
                            }
                            if (hasPlayer) {
                                if (noSteerTimer <= 0f) {
                                    if (MoveDirection is MoveBlock.Directions.Right or MoveBlock.Directions.Left) {
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

                        moveSpeed = Calc.Approach(moveSpeed, targetMoveSpeed, moveAcceleration * Engine.DeltaTime);
                        angle = Calc.Approach(angle, targetAngle, SteerSpeed * Engine.DeltaTime);

                        Vector2 vector = Calc.AngleToVector(angle, moveSpeed) * Engine.DeltaTime;
                        bool shouldBreak;
                        moveSwapPoints = true; // Tells MoveExact to move start and end points too
                        if (MoveDirection is MoveBlock.Directions.Right or MoveBlock.Directions.Left) {
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
                            if (MoveDirection == MoveBlock.Directions.Down && Top > SceneAs<Level>().Bounds.Bottom + 32) {
                                shouldBreak = true;
                            }
                        }
                        moveSwapPoints = false;

                        // Structs are value types, not reference types, so we have to copy it back and forth
                        moveRect = swapBlockData.Get<Rectangle>("moveRect");
                        moveRect.X = (int) Math.Min(start.X, end.X);
                        moveRect.Y = (int) Math.Min(start.Y, end.Y);
                        swapBlockData["moveRect"] = moveRect;

                        swapBlockData["start"] = start;
                        swapBlockData["end"] = end;

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
                    }
                    yield return null;
                }

                Audio.Play(SFX.game_04_arrowblock_break, Position);
                moveBlockSfx.Stop();
                State = MovementState.Breaking;
                moveSpeed = (targetMoveSpeed = 0f);
                angle = (targetAngle = homeAngle);

                StartShaking(0.2f);
                StopPlayerRunIntoAnimation = true;
                yield return 0.2f;

                BreakParticles();
                Get<MoveBlockRedirectable>()?.ResetBlock();
                List<MoveBlockDebris> debrisList = new List<MoveBlockDebris>();
                for (int i = 0; i < Width; i += 8) {
                    for (int j = 0; j < Height; j += 8) {
                        Vector2 value = new Vector2(i + 4f, j + 4f);
                        MoveBlockDebris debris = Engine.Pooler.Create<MoveBlockDebris>().Init(Position + value, Center, startPosition + value);
                        debris.Sprite.Texture = debrisTextures.Choose();
                        debrisList.Add(debris);
                        Scene.Add(debris);
                    }
                }

                MoveStaticMovers(startPosition - Position);
                DisableStaticMovers();

                // Reset everything
                swapBlockData["moveRect"] = startingRect;

                swapBlockData["start"] = start = startPosition;
                swapBlockData["end"] = end = startPosition + difference;
                Position = startPosition;

                Audio.Stop(swapBlockData.Get<EventInstance>("returnSfx"));
                Audio.Stop(swapBlockData.Get<EventInstance>("moveSfx"));

                Visible = Collidable = path.Visible = false;
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

                swapBlockData["target"] = 0;
                swapBlockData["lerp"] = 0f;
                swapBlockData["returnTimer"] = 0f;
                swapBlockData["speed"] = 0f;
                Swapping = false;

                Audio.Play(SFX.game_04_arrowblock_reappear, Position);
                Visible = path.Visible = true;
                EnableStaticMovers();
                moveSpeed = (targetMoveSpeed = 0f);
                angle = (targetAngle = homeAngle);
                noSquish = null;
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

        public override void OnStaticMoverTrigger(StaticMover sm) =>
            Triggered = true;

        private void UpdateColors() {
            Color color = Calc.HexToColor("6f98a6");
            if (State == MovementState.Moving) {
                color = Calc.HexToColor("ff7e12");
            } else if (State == MovementState.Breaking) {
                color = Calc.HexToColor("794a94");
            }
            foreach (Image item in topButton) {
                item.Color = color;
            }
            foreach (Image item in leftButton) {
                item.Color = color;
            }
            foreach (Image item in rightButton) {
                item.Color = color;
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
            bool vertical = MoveDirection is MoveBlock.Directions.Down or MoveBlock.Directions.Up;
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
            float particleNum;
            if (MoveDirection == MoveBlock.Directions.Right) {
                position = CenterLeft + Vector2.UnitX;
                positionRange = Vector2.UnitY * (Height - 4f);
                angle = (float) Math.PI;
                particleNum = Height / 32f;
            } else if (MoveDirection == MoveBlock.Directions.Left) {
                position = CenterRight;
                positionRange = Vector2.UnitY * (Height - 4f);
                angle = 0f;
                particleNum = Height / 32f;
            } else if (MoveDirection == MoveBlock.Directions.Down) {
                position = TopCenter + Vector2.UnitY;
                positionRange = Vector2.UnitX * (Width - 4f);
                angle = -(float) Math.PI / 2f;
                particleNum = Width / 32f;
            } else {
                position = BottomCenter;
                positionRange = Vector2.UnitX * (Width - 4f);
                angle = (float) Math.PI / 2f;
                particleNum = Width / 32f;
            }

            particleRemainder += particleNum;
            int particleNumTruncated = (int) particleRemainder;
            particleRemainder -= particleNumTruncated;

            positionRange *= 0.5f;
            if (particleNumTruncated > 0) {
                SceneAs<Level>().ParticlesBG.Emit(MoveBlock.P_Move, particleNumTruncated, position, positionRange, angle);
            }
        }

        private void ScrapeParticles(Vector2 dir) {
            bool collidable = Collidable;
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
            Collidable = collidable;
        }

        public override void MoveHExact(int move) {
            if (noSquish != null && ((move < 0 && noSquish.X < X) || (move > 0 && noSquish.X > X))) {
                while (move != 0 && noSquish.CollideCheck<Solid>(noSquish.Position + Vector2.UnitX * move)) {
                    move -= Math.Sign(move);
                }
            }

            base.MoveHExact(move);

            if (moveSwapPoints) {
                start.X += move;
                end.X += move;
            }
        }

        public override void MoveVExact(int move) {
            if (noSquish != null && move < 0 && noSquish.Y <= Y) {
                while (move != 0 && noSquish.CollideCheck<Solid>(noSquish.Position + Vector2.UnitY * move)) {
                    move -= Math.Sign(move);
                }
            }

            base.MoveVExact(move);

            if (moveSwapPoints) {
                start.Y += move;
                end.Y += move;
            }
        }

        private bool MoveCheck(Vector2 speed) {
            if (speed.X != 0f) {
                if (MoveHCollideSolids(speed.X, thruDashBlocks: false)) {
                    for (int offsetY = 1; offsetY <= 3; offsetY++) {
                        for (int sign = 1; sign >= -1; sign -= 2) {
                            Vector2 value = new Vector2(Math.Sign(speed.X), offsetY * sign);
                            if (!(CollideCheck<Solid>(Position) || CollideCheck<Solid>(Position + value))) {
                                MoveVExact(offsetY * sign);
                                MoveHExact(Math.Sign(speed.X));
                                return false;

                            }
                        }
                    }
                    return true;
                }
            } else if (speed.Y != 0f) {
                if (MoveVCollideSolids(speed.Y, thruDashBlocks: false)) {
                    for (int offsetX = 1; offsetX <= 3; offsetX++) {
                        for (int sign = 1; sign >= -1; sign -= 2) {
                            Vector2 value2 = new Vector2(offsetX * sign, Math.Sign(speed.Y));
                            if (!CollideCheck<Solid>(Position + value2)) {
                                MoveHExact(offsetX * sign);
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
            IL.Celeste.SwapBlock.Update += SwapBlock_Update;
            On.Celeste.SwapBlock.Update += SwapBlock_Update;
            On.Celeste.SwapBlock.Render += SwapBlock_Render;
            On.Celeste.SwapBlock.DrawBlockStyle += SwapBlock_DrawBlockStyle;
        }

        internal static void Unload() {
            IL.Celeste.SwapBlock.Update -= SwapBlock_Update;
            On.Celeste.SwapBlock.Update -= SwapBlock_Update;
            On.Celeste.SwapBlock.Render -= SwapBlock_Render;
            On.Celeste.SwapBlock.DrawBlockStyle -= SwapBlock_DrawBlockStyle;
        }

        // Call MoveSwapBlock.Update instead of SwapBlock.Update
        private static void SwapBlock_Update(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // Move after the base.Update call
            cursor.GotoNext(MoveType.After, instr => instr.MatchCall<Solid>("Update"));

            // Load "this" onto the stack and emit a delegate that takes it and returns a bool
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<SwapBlock, bool>>(entity => {
                if (entity is MoveSwapBlock block) {
                    if (block.State == MovementState.Breaking)
                        return true;

                    if (!block.doesReturn) {
                        block.Update();
                        return true;
                    }
                }
                return false;
            });
            // If the boolean is false, skip to the next vanilla instruction
            cursor.Emit(OpCodes.Brfalse_S, cursor.Next);
            // Else return (stop executing)
            cursor.Emit(OpCodes.Ret);
        }

        // For updating the MoveBlock components
        private static void SwapBlock_Update(On.Celeste.SwapBlock.orig_Update orig, SwapBlock self) {
            orig(self);

            if (self is MoveSwapBlock block) {
                if (block.canSteer) {
                    bool playerLeft = (block.MoveDirection == MoveBlock.Directions.Up || block.MoveDirection == MoveBlock.Directions.Down) && block.CollideCheck<Player>(block.Position + new Vector2(-1f, 0f));
                    bool playerRight = (block.MoveDirection == MoveBlock.Directions.Up || block.MoveDirection == MoveBlock.Directions.Down) && block.CollideCheck<Player>(block.Position + new Vector2(1f, 0f));
                    bool playerTop = (block.MoveDirection == MoveBlock.Directions.Left || block.MoveDirection == MoveBlock.Directions.Right) && block.CollideCheck<Player>(block.Position + new Vector2(0f, -1f));

                    if ((playerLeft && !block.leftPressed) || (playerTop && !block.topPressed) || (playerRight && !block.rightPressed)) {
                        Audio.Play(SFX.game_04_arrowblock_side_depress, block.Position);
                    }
                    if ((!playerLeft && block.leftPressed) || (!playerTop && block.topPressed) || (!playerRight && block.rightPressed)) {
                        Audio.Play(SFX.game_04_arrowblock_side_release, block.Position);
                    }
                    block.leftPressed = playerLeft;
                    block.rightPressed = playerRight;
                    block.topPressed = playerTop;
                }
                block.UpdateColors();
            }
        }

        // Other rendering is handled in the SwapBlock_DrawBlockStyle hook
        private static void SwapBlock_Render(On.Celeste.SwapBlock.orig_Render orig, SwapBlock self) {
            orig(self);
            if (self is MoveSwapBlock block) {
                for (int i = block.leftPressed ? 1 : 0; i < block.leftButton.Count; i += 2) {
                    block.leftButton[i].Render();
                }
                for (int i = block.rightPressed ? 1 : 0; i < block.rightButton.Count; i += 2) {
                    block.rightButton[i].Render();
                }
                for (int i = block.topPressed ? 1 : 0; i < block.topButton.Count; i += 2) {
                    block.topButton[i].Render();
                }
            }
        }

        // Offsets for the "gem" in the center of the SwapBlock, based on the rotation of the arrow texture
        private static Vector2[] middleOffsets = new Vector2[] {
            -Vector2.UnitY,
            new Vector2(1, -1),
            Vector2.UnitX,
            Vector2.One,
            Vector2.UnitY,
            new Vector2(-1, 1),
            -Vector2.UnitX,
            -Vector2.One,
        };

        private static void SwapBlock_DrawBlockStyle(On.Celeste.SwapBlock.orig_DrawBlockStyle orig, SwapBlock self, Vector2 pos, float width, float height, MTexture[,] ninSlice, Sprite middle, Color color) {
            orig(self, pos, width, height, ninSlice, middle, color);

            // DrawBlockStyle is also used by the SwapBlock PathRenderer, which just passes null to the middle argument
            if (self is MoveSwapBlock block && middle != null) {
                if (block.State != MovementState.Breaking) {
                    // This can probably be cleaned up, but I haven't bothered to understand it
                    int value = Calc.Clamp((int) Math.Floor((block.angle + Calc.QuarterCircle * 5) % Calc.Circle / Calc.Circle * 8f + 0.5f), 0, 7);

                    Image middleImage = middle;
                    if (block.State == MovementState.Moving && !block.Swapping && (block.Position == block.start || block.Position == block.end))
                        middleImage = block.middleOrange;


                    block.middleArrow.Texture = value % 2 == 0 ? block.middleCardinal : block.middleDiagonal;
                    block.middleArrow.RenderPosition = pos + new Vector2(width / 2f, height / 2f);
                    block.middleArrow.Rotation = value / 2 * Calc.QuarterCircle;
                    block.middleArrow.Render();

                    middleImage.Color = color;
                    middleImage.RenderPosition = pos + new Vector2(width / 2f, height / 2f) + middleOffsets[value];
                    middleImage.Render();
                } else {
                    block.middleArrow.Texture = GFX.Game["objects/CommunalHelper/moveSwapBlock/midBlockCross"];
                    block.middleArrow.RenderPosition = pos + new Vector2(width / 2f, height / 2f);
                    block.middleArrow.Render();

                    middle.Color = Color.Black;
                    middle.RenderPosition = pos + new Vector2(width / 2f, height / 2f);
                    middle.Render();
                }
            }
        }

        #endregion

    }
}
