using Celeste.Mod.CommunalHelper.Entities;
using Celeste.Mod.Entities;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper {

    [CustomEntity("CommunalHelper/ConnectedMoveBlock")]
    public class ConnectedMoveBlock : ConnectedSolid {
        // Custom Border Entity
        private class Border : Entity {
            public ConnectedMoveBlock Parent;
            private static Vector2 offset = new Vector2(1, 1);

            public Border(ConnectedMoveBlock parent) {
                Parent = parent;
                Depth = Depths.Player + 1;
            }

            public override void Update() {
                if (Parent.Scene != Scene) {
                    RemoveSelf();
                }
                base.Update();
            }

            public override void Render() {
                foreach (Hitbox hitbox in Parent.Colliders) {
                    Draw.Rect(hitbox.Position + Parent.Position + Parent.Shake - offset, hitbox.Width + 2f, hitbox.Height + 2f, Color.Black);

                    float num = Parent.flash * 4f;
                    if (Parent.flash > 0f) {
                        Draw.Rect(hitbox.Position + Parent.Position - new Vector2(num, num), hitbox.Width + 2f * num, hitbox.Height + 2f * num, Color.White * Parent.flash);
                    }
                }
            }
        }

        public enum MovementState {
            Idling,
            Moving,
            Breaking
        }
        public MovementState State;

        private static MTexture[,] edges = new MTexture[3, 3];
        private static MTexture[,] innerCorners = new MTexture[2, 2];
        private static List<MTexture> arrows = new List<MTexture>();

        private static readonly Color idleBgFill = Calc.HexToColor("474070");
        private static readonly Color pressedBgFill = Calc.HexToColor("30b335");
        private static readonly Color breakingBgFill = Calc.HexToColor("cc2541");
        private Color fillColor = idleBgFill;

        private float particleRemainder;

        private Vector2 startPosition;

        public MoveBlock.Directions Direction;

        private List<Hitbox> ArrowsList;

        private float moveSpeed;
        private bool triggered;

        private float speed;
        private float targetSpeed;

        private float angle;
        private float targetAngle;
        private float homeAngle;

        private float flash;
        private Border border;

        private Player noSquish;

        private SoundSource moveSfx;

        public ConnectedMoveBlock(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Width, data.Height, data.Enum<MoveBlock.Directions>("direction"), data.Bool("fast") ? 75f : data.Float("moveSpeed", 60f)) { }

        public ConnectedMoveBlock(Vector2 position, int width, int height, MoveBlock.Directions direction, float moveSpeed)
            : base(position, width, height, safe: false) {

            Depth = Depths.Player - 1;
            startPosition = position;
            Direction = direction;
            this.moveSpeed = moveSpeed;

            homeAngle = targetAngle = angle = direction.Angle();
            Add(moveSfx = new SoundSource());
            Add(new Coroutine(Controller()));
            UpdateColors();
            Add(new LightOcclude(0.5f));
        }

        private IEnumerator Controller() {
            while (true) {
                triggered = false;
                State = MovementState.Idling;
                while (!triggered && !HasPlayerRider()) {
                    yield return null;
                }

                Audio.Play(SFX.game_04_arrowblock_activate, Position);
                State = MovementState.Moving;
                StartShaking(0.2f);
                ActivateParticles();
                yield return 0.2f;

                targetSpeed = moveSpeed;
                moveSfx.Play(SFX.game_04_arrowblock_move_loop);
                moveSfx.Param("arrow_stop", 0f);
                StopPlayerRunIntoAnimation = false;
                float crashTimer = 0.15f;
                float crashResetTimer = 0.1f;
                while (true) {
                    if (Scene.OnInterval(0.02f)) {
                        MoveParticles();
                    }
                    speed = Calc.Approach(speed, targetSpeed, 300f * Engine.DeltaTime);
                    angle = Calc.Approach(angle, targetAngle, (float) Math.PI * 16f * Engine.DeltaTime);
                    Vector2 vec = Calc.AngleToVector(angle, speed) * Engine.DeltaTime;
                    bool flag2;
                    Vector2 start = Position;
                    if (Direction is MoveBlock.Directions.Right or MoveBlock.Directions.Left) {
                        flag2 = MoveCheck(vec.XComp());
                        noSquish = Scene.Tracker.GetEntity<Player>();
                        MoveVCollideSolids(vec.Y, thruDashBlocks: false);
                        noSquish = null;
                    } else {
                        flag2 = MoveCheck(vec.YComp());
                        noSquish = Scene.Tracker.GetEntity<Player>();
                        MoveHCollideSolids(vec.X, thruDashBlocks: false);
                        noSquish = null;
                        if (Direction == MoveBlock.Directions.Down && Top > SceneAs<Level>().Bounds.Bottom + 32) {
                            flag2 = true;
                        }
                    }
                    Vector2 move = Position - start;
                    SpawnScrapeParticles(Math.Abs(move.X) != 0, Math.Abs(move.Y) != 0);

                    if (flag2) {
                        moveSfx.Param("arrow_stop", 1f);
                        crashResetTimer = 0.1f;
                        if (!(crashTimer > 0f)) {
                            break;
                        }
                        crashTimer -= Engine.DeltaTime;
                    } else {
                        moveSfx.Param("arrow_stop", 0f);
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
                moveSfx.Stop();
                State = MovementState.Breaking;
                speed = targetSpeed = 0f;
                angle = targetAngle = homeAngle;
                StartShaking(0.2f);
                StopPlayerRunIntoAnimation = true;
                yield return 0.2f;

                BreakParticles();
                List<MoveBlockDebris> debris = new List<MoveBlockDebris>();
                for (int i = 0; i < Width; i += 8) {
                    for (int j = 0; j < Height; j += 8) {
                        Vector2 value = new Vector2(i + 4f, j + 4f);
                        Vector2 pos = value + Position + GroupOffset;
                        if (CollidePoint(pos)) {
                            MoveBlockDebris debris2 = Engine.Pooler.Create<MoveBlockDebris>().Init(pos, GroupCenter, startPosition + GroupOffset + value);
                            debris.Add(debris2);
                            Scene.Add(debris2);
                        }
                    }
                }
                MoveStaticMovers(startPosition - Position);
                DisableStaticMovers();
                Position = startPosition;
                Visible = Collidable = false;
                yield return 2.2f;

                foreach (MoveBlockDebris item in debris) {
                    item.StopMoving();
                }
                while (CollideCheck<Actor>() || CollideCheck<Solid>()) {
                    yield return null;
                }

                Collidable = true;
                EventInstance instance = Audio.Play(SFX.game_04_arrowblock_reform_begin, debris[0].Position);
                Coroutine component;
                Coroutine routine = component = new Coroutine(SoundFollowsDebrisCenter(instance, debris));
                Add(component);
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
                Audio.Play(SFX.game_04_arrowblock_reappear, Position);
                Visible = true;
                EnableStaticMovers();
                speed = targetSpeed = 0f;
                angle = targetAngle = homeAngle;
                noSquish = null;
                fillColor = idleBgFill;
                UpdateColors();
                flash = 1f;
            }
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

        private void UpdateColors() {
            Color value = State switch {
                MovementState.Moving => pressedBgFill,
                MovementState.Breaking => breakingBgFill,
                _ => idleBgFill,
            };
            fillColor = Color.Lerp(fillColor, value, 10f * Engine.DeltaTime);
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
                            if (!CollideCheck<Solid>(Position + value)) {
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

        private void ActivateParticles() {
            foreach (Hitbox hitbox in Colliders) {
                bool left = !CollideCheck<Player>(Position - Vector2.UnitX);
                bool right = !CollideCheck<Player>(Position + Vector2.UnitX);
                bool top = !CollideCheck<Player>(Position - Vector2.UnitY);

                if (left) {
                    SceneAs<Level>().ParticlesBG.Emit(MoveBlock.P_Activate, (int) (hitbox.Height / 2f), Position + hitbox.CenterLeft, Vector2.UnitY * (hitbox.Height - 4f) * 0.5f, (float) Math.PI);
                }
                if (right) {
                    SceneAs<Level>().ParticlesBG.Emit(MoveBlock.P_Activate, (int) (hitbox.Height / 2f), Position + hitbox.CenterRight, Vector2.UnitY * (hitbox.Height - 4f) * 0.5f, 0f);
                }
                if (top) {
                    SceneAs<Level>().ParticlesBG.Emit(MoveBlock.P_Activate, (int) (hitbox.Width / 2f), Position + hitbox.TopCenter, Vector2.UnitX * (hitbox.Width - 4f) * 0.5f, -(float) Math.PI / 2f);
                }
                SceneAs<Level>().ParticlesBG.Emit(MoveBlock.P_Activate, (int) (hitbox.Width / 2f), Position + hitbox.BottomCenter, Vector2.UnitX * (hitbox.Width - 4f) * 0.5f, (float) Math.PI / 2f);
            }
        }

        private void BreakParticles() {
            foreach (Hitbox hitbox in Colliders) {

                Vector2 center = Position + hitbox.Center;
                for (int i = 0; i < hitbox.Width; i += 4) {
                    for (int j = 0; j < hitbox.Height; j += 4) {
                        Vector2 vector = Position + hitbox.Position + new Vector2(2 + i, 2 + j);
                        SceneAs<Level>().Particles.Emit(MoveBlock.P_Break, 1, vector, Vector2.One * 2f, (vector - center).Angle());
                    }
                }

            }
        }

        private void MoveParticles() {
            foreach (Hitbox hitbox in Colliders) {

                Vector2 position;
                Vector2 positionRange;
                float num;
                float num2;
                if (Direction == MoveBlock.Directions.Right) {
                    position = hitbox.CenterLeft + Vector2.UnitX;
                    positionRange = Vector2.UnitY * (hitbox.Height - 4f);
                    num = (float) Math.PI;
                    num2 = hitbox.Height / 32f;
                } else if (Direction == MoveBlock.Directions.Left) {
                    position = hitbox.CenterRight;
                    positionRange = Vector2.UnitY * (hitbox.Height - 4f);
                    num = 0f;
                    num2 = hitbox.Height / 32f;
                } else if (Direction == MoveBlock.Directions.Down) {
                    position = hitbox.TopCenter + Vector2.UnitY;
                    positionRange = Vector2.UnitX * (hitbox.Width - 4f);
                    num = -(float) Math.PI / 2f;
                    num2 = hitbox.Width / 32f;
                } else {
                    position = hitbox.BottomCenter;
                    positionRange = Vector2.UnitX * (hitbox.Width - 4f);
                    num = (float) Math.PI / 2f;
                    num2 = hitbox.Width / 32f;
                }
                particleRemainder += num2;
                int num3 = (int) particleRemainder;
                particleRemainder -= num3;
                positionRange *= 0.5f;
                if (num3 > 0) {
                    SceneAs<Level>().ParticlesBG.Emit(MoveBlock.P_Move, num3, position + Position, positionRange, num);
                }

            }
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            AutoTile(edges, innerCorners);
            scene.Add(border = new Border(this));

            // Get all the colliders that can have an arrow drawn on.
            ArrowsList = new List<Hitbox> { (Hitbox) MasterCollider };
            foreach (Hitbox hitbox in Colliders) {
                if (Math.Min(hitbox.Width, hitbox.Height) >= 24) {
                    ArrowsList.Add(hitbox);
                }
            }
        }

        public override void Update() {
            base.Update();
            if (moveSfx != null && moveSfx.Playing) {
                int num = (int) Math.Floor((0f - (Calc.AngleToVector(angle, 1f) * new Vector2(-1f, 1f)).Angle() + (float) Math.PI * 2f) % ((float) Math.PI * 2f) / ((float) Math.PI * 2f) * 8f + 0.5f);
                moveSfx.Param("arrow_influence", num + 1);
            }
            border.Visible = Visible;
            flash = Calc.Approach(flash, 0f, Engine.DeltaTime * 5f);
            UpdateColors();
        }

        public override void Render() {
            Vector2 position = Position;
            Position += Shake;

            foreach (Hitbox hitbox in Colliders) {
                Draw.Rect(hitbox.Position.X + Position.X, hitbox.Position.Y + Position.Y, hitbox.Width, hitbox.Height, fillColor);
            }

            base.Render();
            int arrowIndex = Calc.Clamp((int) Math.Floor((0f - angle + (float) Math.PI * 2f) % ((float) Math.PI * 2f) / ((float) Math.PI * 2f) * 8f + 0.5f), 0, 7);
            foreach (Hitbox hitbox in ArrowsList) {
                Vector2 vec = hitbox.Center + Position;
                Draw.Rect(vec.X - 4f, vec.Y - 4f, 8f, 8f, fillColor);
                if (State != MovementState.Breaking) {
                    arrows[arrowIndex].DrawCentered(vec);
                } else {
                    GFX.Game["objects/moveBlock/x"].DrawCentered(vec);
                }
            }

            foreach (Image img in Tiles) {
                Draw.Rect(img.Position + Position, 8, 8, Color.White * flash);
            }

            Position = position;
        }

        public static void InitializeTextures() {
            MTexture edgeTiles = GFX.Game["objects/moveBlock/base"];
            MTexture innerTiles = GFX.Game["objects/CommunalHelper/connectedMoveBlock/innerCorners"];
            arrows = GFX.Game.GetAtlasSubtextures("objects/moveBlock/arrow");

            for (int i = 0; i < 3; i++) {
                for (int j = 0; j < 3; j++) {
                    edges[i, j] = edgeTiles.GetSubtexture(i * 8, j * 8, 8, 8);
                }
            }

            for (int i = 0; i < 2; i++) {
                for (int j = 0; j < 2; j++) {
                    innerCorners[i, j] = innerTiles.GetSubtexture(i * 8, j * 8, 8, 8);
                }
            }

        }
    }
}
