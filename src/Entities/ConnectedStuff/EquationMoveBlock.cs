﻿using Celeste.Mod.Entities;
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
        float moveTime = 0;
        // TODO: constC? arbitrary constants?
        // 0: y = ax               ->   a
        // 1: y = ax^2 + bx        ->   2ax + b
        // 2: y = ax^3 + bx^2 + x  ->   3ax^2 + 2bx + 1
        // 3: y = a*sin bx         ->   a*b*cos(bx)
        // 4: y = a*cos bx         ->  -a*b*sin(bx)
        // 5: y = ae^bx            ->   abe^bx
        // 6: y = (ax)^b           ->   b(ax)^(b-1)

        public EquationMoveBlock(EntityData data, Vector2 offset)
            : base(data, offset) {
            equation = data.Int("equation", 3);
            constA = data.Float("constantA", 10);
            constB = data.Float("constantB", 0.05f);
        }

        protected override IEnumerator Controller() {
            while (true) {
                triggered = false;
                State = MovementState.Idling;
                moveTime = 0;
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
                    float progress = Direction switch {
                        MoveBlock.Directions.Left or
                        MoveBlock.Directions.Right => X - startPosition.X,
                        MoveBlock.Directions.Up or
                        MoveBlock.Directions.Down => Y - startPosition.Y,
                        _ => 0
                    };
                    // assume we're moving constantly
                    int neg = (Direction is MoveBlock.Directions.Left or MoveBlock.Directions.Up) ? -1 : 1;
                    float xgrad = (float) (neg * (equation is 7 ? constA * Math.Sin(moveTime) : 1));
                    float ygrad = equation switch {
                        0 => constA,
                        1 => 2 * constA * progress + constB,
                        2 => 3 * constA * progress * progress + 2 * constB * progress + 1,
                        3 => (float)(constA * constB * Math.Cos(constB * progress)),
                        4 => (float)(-constA * constB * Math.Sin(constB * progress)),
                        5 => (float)(constA * constB * Math.Pow(Math.E, constB * progress)),
                        6 => (float)(Math.Pow(progress * constA, constB - 1) * constB),
                        7 => (float)(constB * Math.Cos(moveTime)),
                        _ => 1
                    };
                    // tan x = y / x
                    // make y negative because y- is up in Celeste
                    // swap x/y if we're vertical
                    float targetAngle = (float)Math.Atan2((Direction is MoveBlock.Directions.Left or MoveBlock.Directions.Right) ? (-ygrad * neg) : xgrad, (Direction is MoveBlock.Directions.Left or MoveBlock.Directions.Right) ? xgrad : (-ygrad * neg));
                    // and then we resume as normal
                    speed = Calc.Approach(speed, targetSpeed, 300f * Engine.DeltaTime);
                    angle = targetAngle;
                    Vector2 vec = Calc.AngleToVector(angle, speed) * Engine.DeltaTime;
                    bool moveCheck = MoveCheck(vec.XComp()) || MoveCheck(vec.YComp());
                    if (targetAngle == double.NaN)
                        moveCheck = true;
                    Vector2 start = Position;
                    noSquish = Scene.Tracker.GetEntity<Player>();
                    MoveVCollideSolids(vec.Y, thruDashBlocks: false);
                    MoveHCollideSolids(vec.X, thruDashBlocks: false);
                    noSquish = null;
                    moveTime += Engine.DeltaTime;

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
                    if (Left < level.Bounds.Left || Top < level.Bounds.Top || Right > level.Bounds.Right || Top > SceneAs<Level>().Bounds.Bottom + 32) {
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
