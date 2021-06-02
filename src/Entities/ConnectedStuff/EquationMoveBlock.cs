using Celeste.Mod.Entities;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Entities.ConnectedStuff {

    [CustomEntity("CommunalHelper/EquationMoveBlock")]
    class EquationMoveBlock : ConnectedMoveBlock {

        int equation = 3;
        float constA = 8, constB = 0.1f;
        // TODO: constC? arbitrary constants?
        // 0: y = ax               ->   a
        // 1: y = ax^2 + bx        ->   2ax + b
        // 2: y = ax^3 + bx^2 + x  ->   3ax^2 + 2bx + 1
        // 3: y = a*sin bx         ->   a*b*cos(bx)
        // 4: y = a*cos bx         ->  -a*b*sin(bx)
        // 5: y = ae^bx            ->   abe^bx
        // 6: y = ax^b             ->   abx^(b-1)

        public EquationMoveBlock(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Width, data.Height, data.Enum<MoveBlock.Directions>("direction"), data.Bool("fast") ? 75f : data.Float("moveSpeed", 60f), data.Int("equation", 3), data.Float("constantA", 10), data.Float("constantB", 0.05f)) { }

        public EquationMoveBlock(Vector2 position, int width, int height, MoveBlock.Directions direction, float moveSpeed, int equation, float constA, float constB)
            : base(position, width, height, direction, moveSpeed) {
            this.equation = equation;
            this.constA = constA;
            this.constB = constB;
        }

        protected override IEnumerator Controller() {
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
                    // use gradients to decide on an angle
                    float xdiff = X - startPosition.X;
                    // assume we're moving directly right
                    float xgrad = Direction == MoveBlock.Directions.Left? -1 : 1;
                    float ygrad = equation switch {
                        0 => constA,
                        1 => 2 * constA * xdiff + constB,
                        2 => 3 * constA * xdiff * xdiff + 2 * constB * xdiff + 1,
                        3 => (float)(constA * constB * Math.Cos(constB * xdiff)),
                        4 => (float)(-constA * constB * Math.Sin(constB * xdiff)),
                        5 => (float)(constA * constB * Math.Pow(Math.E, constB * xdiff)),
                        6 => (float)(Math.Pow(xdiff, constB - 1) * constA * constB),
                        _ => 1
                    };
                    // tan x = y / x
                    float targetAngle = (float)Math.Atan2(ygrad * xgrad, xgrad);
                    // and then we resume as normal
                    speed = Calc.Approach(speed, targetSpeed, 300f * Engine.DeltaTime);
                    angle = Calc.Approach(angle, targetAngle, (float) Math.PI * 16f * Engine.DeltaTime);
                    Vector2 vec = Calc.AngleToVector(angle, speed) * Engine.DeltaTime;
                    bool moveCheck = MoveCheck(vec.XComp()) || MoveCheck(vec.YComp());
                    Vector2 start = Position;
                    noSquish = Scene.Tracker.GetEntity<Player>();
                    MoveVCollideSolids(vec.Y, thruDashBlocks: false);
                    MoveHCollideSolids(vec.X, thruDashBlocks: false);
                    noSquish = null;

                    Vector2 move = Position - start;
                    SpawnScrapeParticles(Math.Abs(move.X) != 0, Math.Abs(move.Y) != 0);

                    if (moveCheck) {
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
    }
}
