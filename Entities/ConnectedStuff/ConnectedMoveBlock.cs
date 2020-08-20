using Celeste.Mod.Entities;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CommunalHelper {

    [CustomEntity("CommunalHelper/ConnectedMoveBlock")]
    class ConnectedMoveBlock : ConnectedSolid {

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
                base.Tag = Tags.TransitionUpdate;
                base.Collider = new Hitbox(4f, 4f, -2f, -2f);
                Add(sprite = new Image(Calc.Random.Choose(GFX.Game.GetAtlasSubtextures("objects/moveblock/debris"))));
                sprite.CenterOrigin();
                sprite.FlipX = Calc.Random.Chance(0.5f);
                onCollideH = delegate
                {
                    speed.X = (0f - speed.X) * 0.5f;
                };
                onCollideV = delegate
                {
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
                spin = Calc.Random.Range(3.49065852f, 10.4719753f) * (float) Calc.Random.Choose(1, -1);
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
                    if (shaking && base.Scene.OnInterval(0.05f)) {
                        sprite.X = -1 + Calc.Random.Next(3);
                        sprite.Y = -1 + Calc.Random.Next(3);
                    }
                } else {
                    Position = returnCurve.GetPoint(Ease.CubeOut(returnEase));
                    returnEase = Calc.Approach(returnEase, 1f, Engine.DeltaTime / returnDuration);
                    sprite.Scale = Vector2.One * (1f + returnEase * 0.5f);
                }
                if ((base.Scene as Level).Transitioning) {
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
                if (base.Scene != null) {
                    Camera camera = (base.Scene as Level).Camera;
                    if (base.X < camera.X) {
                        base.X = camera.X - 8f;
                    }
                    if (base.Y < camera.Y) {
                        base.Y = camera.Y - 8f;
                    }
                    if (base.X > camera.X + 320f) {
                        base.X = camera.X + 320f + 8f;
                    }
                    if (base.Y > camera.Y + 180f) {
                        base.Y = camera.Y + 180f + 8f;
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

        // Custom Border Entity
        private class Border : Entity {
            public ConnectedMoveBlock Parent;
            private static Vector2 offset = new Vector2(1, 1);

            public Border(ConnectedMoveBlock parent) {
                Parent = parent;
                base.Depth = 1;
            }

            public override void Update() {
                if (Parent.Scene != base.Scene) {
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


        private enum MovementState {
            Idling,
            Moving,
            Breaking
        }
        private MovementState state;

        private static MTexture[,] edges = new MTexture[3, 3];
        private static MTexture[,] innerCorners = new MTexture[2, 2];
        private static List<MTexture> arrows = new List<MTexture>();

        private static readonly Color idleBgFill = Calc.HexToColor("474070");
        private static readonly Color pressedBgFill = Calc.HexToColor("30b335");
        private static readonly Color breakingBgFill = Calc.HexToColor("cc2541");
        private Color fillColor = idleBgFill;

        private float particleRemainder;

        private Vector2 startPosition;

        private MoveBlock.Directions direction;

        private bool fast;
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
            : this(data.Position + offset, data.Width, data.Height, data.Enum<MoveBlock.Directions>("direction"), data.Bool("fast")) { }

        public ConnectedMoveBlock(Vector2 position, int width, int height, MoveBlock.Directions direction, bool fast)
            : base(position, width, height, safe: false) {

            base.Depth = -1;
            startPosition = position;
            this.direction = direction;
            this.fast = fast;

            switch (direction) {
                default:
                    homeAngle = (targetAngle = (angle = 0f));
                    break;
                case MoveBlock.Directions.Left:
                    homeAngle = (targetAngle = (angle = (float) Math.PI));
                    break;
                case MoveBlock.Directions.Up:
                    homeAngle = (targetAngle = (angle = -(float) Math.PI / 2f));
                    break;
                case MoveBlock.Directions.Down:
                    homeAngle = (targetAngle = (angle = (float) Math.PI / 2f));
                    break;
            }

            Add(moveSfx = new SoundSource());
            Add(new Coroutine(Controller()));
            UpdateColors();
            Add(new LightOcclude(0.5f));
        }

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
                targetSpeed = (fast ? 75f : 60f);
                moveSfx.Play("event:/game/04_cliffside/arrowblock_move");
                moveSfx.Param("arrow_stop", 0f);
                StopPlayerRunIntoAnimation = false;
                float crashTimer = 0.15f;
                float crashResetTimer = 0.1f;
                //float noSteerTimer = 0.2f;
                while (true) {
                    if (Scene.OnInterval(0.02f)) {
                        MoveParticles();
                    }
                    speed = Calc.Approach(speed, targetSpeed, 300f * Engine.DeltaTime);
                    angle = Calc.Approach(angle, targetAngle, (float) Math.PI * 16f * Engine.DeltaTime);
                    Vector2 vec = Calc.AngleToVector(angle, speed) * Engine.DeltaTime;
                    bool flag2;
                    if (direction == MoveBlock.Directions.Right || direction == MoveBlock.Directions.Left) {
                        flag2 = MoveCheck(vec.XComp());
                        noSquish = Scene.Tracker.GetEntity<Player>();
                        MoveVCollideSolids(vec.Y, thruDashBlocks: false);
                        noSquish = null;
                    } else {
                        flag2 = MoveCheck(vec.YComp());
                        noSquish = Scene.Tracker.GetEntity<Player>();
                        MoveHCollideSolids(vec.X, thruDashBlocks: false);
                        noSquish = null;
                        if (direction == MoveBlock.Directions.Down && Top > (float) (SceneAs<Level>().Bounds.Bottom + 32)) {
                            flag2 = true;
                        }
                    }

                    // TODO: Here, do the ScrapeParticleCheck.

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
                    if (Left < (float) level.Bounds.Left || Top < (float) level.Bounds.Top || Right > (float) level.Bounds.Right) {
                        break;
                    }
                    yield return null;
                }
                Audio.Play("event:/game/04_cliffside/arrowblock_break", Position);
                moveSfx.Stop();
                state = MovementState.Breaking;
                speed = (targetSpeed = 0f);
                angle = (targetAngle = homeAngle);
                StartShaking(0.2f);
                StopPlayerRunIntoAnimation = true;
                yield return 0.2f;
                BreakParticles();
                List<Debris> debris = new List<Debris>();
                for (int i = 0; (float) i < Width; i += 8) {
                    for (int j = 0; (float) j < Height; j += 8) {
                        Vector2 value = new Vector2((float) i + 4f, (float) j + 4f);
                        Vector2 pos = value + Position + GroupOffset;
                        if (CollidePoint(pos)) {
                            Debris debris2 = Engine.Pooler.Create<Debris>().Init(pos, GroupCenter, startPosition + GroupOffset + value);
                            debris.Add(debris2);
                            Scene.Add(debris2);
                        }
                    }
                }
                MoveStaticMovers(startPosition - Position);
                DisableStaticMovers();
                Position = startPosition;
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
                speed = (targetSpeed = 0f);
                angle = (targetAngle = homeAngle);
                noSquish = null;
                fillColor = idleBgFill;
                UpdateColors();
                flash = 1f;
            }
        }

        private IEnumerator SoundFollowsDebrisCenter(EventInstance instance, List<Debris> debris) {
            while (true) {
                instance.getPlaybackState(out PLAYBACK_STATE pLAYBACK_STATE);
                if (pLAYBACK_STATE == PLAYBACK_STATE.STOPPED) {
                    break;
                }
                Vector2 zero = Vector2.Zero;
                foreach (Debris debri in debris) {
                    zero += debri.Position;
                }
                zero /= (float) debris.Count;
                Audio.Position(instance, zero);
                yield return null;
            }
        }

        private void UpdateColors() {
            Color value = idleBgFill;
            if (state == MovementState.Moving) {
                value = pressedBgFill;
            } else if (state == MovementState.Breaking) {
                value = breakingBgFill;
            }
            fillColor = Color.Lerp(fillColor, value, 10f * Engine.DeltaTime);
        }

        public override void MoveHExact(int move) {
            if (noSquish != null && ((move < 0 && noSquish.X < base.X) || (move > 0 && noSquish.X > base.X))) {
                while (move != 0 && noSquish.CollideCheck<Solid>(noSquish.Position + Vector2.UnitX * move)) {
                    move -= Math.Sign(move);
                }
            }
            base.MoveHExact(move);
        }

        public override void MoveVExact(int move) {
            if (noSquish != null && move < 0 && noSquish.Y <= base.Y) {
                while (move != 0 && noSquish.CollideCheck<Solid>(noSquish.Position + Vector2.UnitY * move)) {
                    move -= Math.Sign(move);
                }
            }
            base.MoveVExact(move);
        }

        private void ScrapeParticles(Vector2 dir) {
            _ = Collidable;
            Collidable = false;
            if (dir.X != 0f) {
                float x = (!(dir.X > 0f)) ? (base.Left - 1f) : base.Right;
                for (int i = 0; (float) i < base.Height; i += 8) {
                    Vector2 vector = new Vector2(x, base.Top + 4f + (float) i);
                    if (base.Scene.CollideCheck<Solid>(vector)) {
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, vector);
                    }
                }
            } else {
                float y = (!(dir.Y > 0f)) ? (base.Top - 1f) : base.Bottom;
                for (int j = 0; (float) j < base.Width; j += 8) {
                    Vector2 vector2 = new Vector2(base.Left + 4f + (float) j, y);
                    if (base.Scene.CollideCheck<Solid>(vector2)) {
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, vector2);
                    }
                }
            }
            Collidable = true;
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
                bool flag1 = !CollideCheck<Player>(Position - Vector2.UnitX);
                bool flag2 = !CollideCheck<Player>(Position + Vector2.UnitX);
                bool flag3 = !CollideCheck<Player>(Position - Vector2.UnitY);

                if (flag1) {
                    SceneAs<Level>().ParticlesBG.Emit(MoveBlock.P_Activate, (int) (hitbox.Height / 2f), Position + hitbox.CenterLeft, Vector2.UnitY * (hitbox.Height - 4f) * 0.5f, (float) Math.PI);
                }
                if (flag2) {
                    SceneAs<Level>().ParticlesBG.Emit(MoveBlock.P_Activate, (int) (hitbox.Height / 2f), Position + hitbox.CenterRight, Vector2.UnitY * (hitbox.Height - 4f) * 0.5f, 0f);
                }
                if (flag3) {
                    SceneAs<Level>().ParticlesBG.Emit(MoveBlock.P_Activate, (int) (hitbox.Width / 2f), Position + hitbox.TopCenter, Vector2.UnitX * (hitbox.Width - 4f) * 0.5f, -(float) Math.PI / 2f);
                }
                SceneAs<Level>().ParticlesBG.Emit(MoveBlock.P_Activate, (int) (hitbox.Width / 2f), Position + hitbox.BottomCenter, Vector2.UnitX * (hitbox.Width - 4f) * 0.5f, (float) Math.PI / 2f);
            }
        }

        private void BreakParticles() {
            foreach (Hitbox hitbox in Colliders) {

                Vector2 center = Position + hitbox.Center;
                for (int i = 0; (float) i < hitbox.Width; i += 4) {
                    for (int j = 0; (float) j < hitbox.Height; j += 4) {
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
                if (direction == MoveBlock.Directions.Right) {
                    position = hitbox.CenterLeft + Vector2.UnitX;
                    positionRange = Vector2.UnitY * (hitbox.Height - 4f);
                    num = (float) Math.PI;
                    num2 = hitbox.Height / 32f;
                } else if (direction == MoveBlock.Directions.Left) {
                    position = hitbox.CenterRight;
                    positionRange = Vector2.UnitY * (hitbox.Height - 4f);
                    num = 0f;
                    num2 = hitbox.Height / 32f;
                } else if (direction == MoveBlock.Directions.Down) {
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
            base.AutoTile(edges, innerCorners);
            scene.Add(border = new Border(this));
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
            Position += base.Shake;

            foreach (Hitbox hitbox in Colliders) {
                Draw.Rect(hitbox.Position.X + Position.X, hitbox.Position.Y + Position.Y, hitbox.Width, hitbox.Height, fillColor);
            }

            base.Render();

            Draw.Rect(base.MasterCenter.X - 4f, base.MasterCenter.Y - 4f, 8f, 8f, fillColor);
            if (state != MovementState.Breaking) {
                int value = (int) Math.Floor((0f - angle + (float) Math.PI * 2f) % ((float) Math.PI * 2f) / ((float) Math.PI * 2f) * 8f + 0.5f);
                arrows[Calc.Clamp(value, 0, 7)].DrawCentered(MasterCenter);
            } else {
                GFX.Game["objects/moveBlock/x"].DrawCentered(MasterCenter);
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
 