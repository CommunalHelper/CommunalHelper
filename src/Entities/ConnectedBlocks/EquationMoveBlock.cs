using FMOD.Studio;
using System.Collections;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Entities.ConnectedStuff;

[CustomEntity("CommunalHelper/EquationMoveBlock")]
internal class EquationMoveBlock : ConnectedMoveBlock
{
    private readonly int equation = 3;
    private readonly float constA = 8, constB = 0.1f;
    private float moveTime = 0;
    // TODO: constC? arbitrary constants?
    // 0: y = ax               ->   a
    // 1: y = ax^2 + bx        ->   2ax + b
    // 2: y = ax^3 + bx^2 + x  ->   3ax^2 + 2bx + 1
    // 3: y = a*sin bx         ->   a*b*cos(bx)
    // 4: y = a*cos bx         ->  -a*b*sin(bx)
    // 5: y = ae^bx            ->   abe^bx
    // 6: y = (ax)^b           ->   b(ax)^(b-1)

    public EquationMoveBlock(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        equation = data.Int("equation", 3);
        constA = data.Float("constantA", 10);
        constB = data.Float("constantB", 0.05f);
    }

    protected override IEnumerator Controller()
    {
        // If we're waiting for flags before becoming visible, start off invisible.
        bool startInvisible = false;
        if (WaitForFlags)
        {
            yield return null;
            startInvisible = AnySetEnabled(BreakerFlags) && WaitForFlags;
        }
        if (startInvisible)
            Visible = Collidable = false;
        while (true)
        {
            bool startingBroken = false, startingByActivator = false;
            curMoveCheck = false;
            triggered = false;
            State = MovementState.Idling;
            while (!triggered && !startingByActivator && !startingBroken)
            {
                if (startInvisible && !AnySetEnabled(BreakerFlags))
                {
                    goto Rebuild;
                }
                yield return null;
                startingBroken = AnySetEnabled(BreakerFlags) && !startInvisible;
                startingByActivator = AnySetEnabled(ActivatorFlags);
            }

            Audio.Play(SFX.game_04_arrowblock_activate, Position);
            State = MovementState.Moving;
            StartShaking(0.2f);
            ActivateParticles();
            if (!startingBroken)
                foreach (string flag in OnActivateFlags)
                {
                    if (flag.Length > 0)
                    {
                        if (flag.StartsWith("!"))
                        {
                            SceneAs<Level>().Session.SetFlag(flag.Substring(1), false);
                        }
                        else if (flag.StartsWith("~"))
                        {
                            SceneAs<Level>().Session.SetFlag(flag.Substring(1), SceneAs<Level>().Session.GetFlag(flag.Substring(1)));
                        }
                        else
                            SceneAs<Level>().Session.SetFlag(flag);
                    }
                }
            yield return 0.2f;

            targetSpeed = moveSpeed;
            moveSfx.Play(SFX.game_04_arrowblock_move_loop);
            moveSfx.Param("arrow_stop", 0f);
            StopPlayerRunIntoAnimation = false;
            float crashTimer = crashTime;
            float crashResetTimer = CrashResetTime;
            float crashStartShakingTimer = CrashStartShakingTime;
            while (true)
            {
                if (Scene.OnInterval(0.02f))
                {
                    MoveParticles();
                }
                // use gradients to decide on an angle
                float progress = Direction switch
                {
                    MoveBlock.Directions.Left or
                    MoveBlock.Directions.Right => X - startPosition.X,
                    MoveBlock.Directions.Up or
                    MoveBlock.Directions.Down => Y - startPosition.Y,
                    _ => 0
                };
                // assume we're moving constantly
                int neg = (Direction is MoveBlock.Directions.Left or MoveBlock.Directions.Up) ? -1 : 1;
                float xgrad = (float) (neg * (equation is 7 ? constA * Math.Sin(moveTime) : 1));
                float ygrad = equation switch
                {
                    0 => constA,
                    1 => (2 * constA * progress) + constB,
                    2 => (3 * constA * progress * progress) + (2 * constB * progress) + 1,
                    3 => (float) (constA * constB * Math.Cos(constB * progress)),
                    4 => (float) (-constA * constB * Math.Sin(constB * progress)),
                    5 => (float) (constA * constB * Math.Pow(Math.E, constB * progress)),
                    6 => (float) (Math.Pow(progress * constA, constB - 1) * constB),
                    7 => (float) (constB * Math.Cos(moveTime)),
                    _ => 1
                };
                // tan x = y / x
                // make y negative because y- is up in Celeste
                // swap x/y if we're vertical
                float targetAngle = (float) Math.Atan2((Direction is MoveBlock.Directions.Left or MoveBlock.Directions.Right) ? (-ygrad * neg) : xgrad, (Direction is MoveBlock.Directions.Left or MoveBlock.Directions.Right) ? xgrad : (-ygrad * neg));
                // and then we resume as normal
                speed = startingBroken ? 0 : Calc.Approach(speed, targetSpeed, 300f * Engine.DeltaTime);
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
                if (Scene.OnInterval(0.03f))
                    SpawnScrapeParticles(Math.Abs(move.X) != 0, Math.Abs(move.Y) != 0);

                curMoveCheck = moveCheck;

                if (startingBroken || AnySetEnabled(BreakerFlags) || targetAngle == double.NaN)
                {
                    moveSfx.Param("arrow_stop", crashTimer > 0.15f ? 0.5f : 1f);
                    crashResetTimer = CrashResetTime;
                    if (crashStartShakingTimer < 0f && shakeOnCollision)
                        StartShaking();
                    if (!(crashTimer > 0f))
                    {
                        break;
                    }
                    crashTimer -= Engine.DeltaTime;
                    crashStartShakingTimer -= Engine.DeltaTime;
                }
                else
                {
                    moveSfx.Param("arrow_stop", 0f);
                    if (crashResetTimer > 0f)
                    {
                        crashResetTimer -= Engine.DeltaTime;
                    }
                    else
                    {
                        StopShaking();
                        crashTimer = crashTime;
                        crashStartShakingTimer = CrashStartShakingTime;
                    }
                }
                Level level = Scene as Level;
                if (Left < level.Bounds.Left || Top < level.Bounds.Top || Right > level.Bounds.Right || Top > SceneAs<Level>().Bounds.Bottom + 32)
                {
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

            List<MoveBlockDebris> debris = new();
            int tWidth = (int) ((GroupBoundsMax.X - GroupBoundsMin.X) / 8);
            int tHeight = (int) ((GroupBoundsMax.Y - GroupBoundsMin.Y) / 8);

            for (int i = 0; i < tWidth; i++)
            {
                for (int j = 0; j < tHeight; j++)
                {
                    if (AllGroupTiles[i, j])
                    {
                        Vector2 value = new((i * 8) + 4, (j * 8) + 4);
                        Vector2 pos = value + Position + GroupOffset;
                        MoveBlockDebris debris2 = Engine.Pooler.Create<MoveBlockDebris>().Init(pos, GroupCenter, startPosition + GroupOffset + value);
                        debris.Add(debris2);
                        Scene.Add(debris2);
                    }
                }
            }
            MoveStaticMovers(startPosition - Position);
            DisableStaticMovers();

            bool shouldProcessBreakFlags = true;
            if (BarrierBlocksFlags)
            {
                bool colliding = false;
                foreach (SeekerBarrier entity in Scene.Tracker.GetEntities<SeekerBarrier>())
                {
                    entity.Collidable = true;
                    colliding |= CollideCheck(entity);
                    entity.Collidable = false;
                }
                shouldProcessBreakFlags = !colliding;
            }

            Position = startPosition;
            Visible = Collidable = false;

            float waitTime = Calc.Clamp(regenTime - 0.8f, 0, float.MaxValue);
            float debrisShakeTime = Calc.Clamp(regenTime - 0.6f, 0, 0.2f);
            float debrisMoveTime = Calc.Clamp(regenTime, 0, 0.6f);

            if (shouldProcessBreakFlags)
                foreach (string flag in OnBreakFlags)
                {
                    if (flag.Length > 0)
                    {
                        if (flag.StartsWith("!"))
                        {
                            SceneAs<Level>().Session.SetFlag(flag.Substring(1), false);
                        }
                        else if (flag.StartsWith("~"))
                        {
                            SceneAs<Level>().Session.SetFlag(flag.Substring(1), SceneAs<Level>().Session.GetFlag(flag.Substring(1)));
                        }
                        else
                            SceneAs<Level>().Session.SetFlag(flag);
                    }
                }
            curMoveCheck = false;
            yield return waitTime;

            foreach (MoveBlockDebris item in debris)
            {
                item.StopMoving();
            }
            while (CollideCheck<Actor>() || CollideCheck<Solid>() || AnySetEnabled(BreakerFlags))
            {
                yield return null;
            }

            Collidable = true;
            EventInstance instance = Audio.Play(SFX.game_04_arrowblock_reform_begin, debris[0].Position);
            Coroutine component;
            Coroutine routine = component = new Coroutine(SoundFollowsDebrisCenter(instance, debris));
            Add(component);
            foreach (MoveBlockDebris item2 in debris)
            {
                item2.StartShaking();
            }
            yield return debrisShakeTime;

            foreach (MoveBlockDebris item3 in debris)
            {
                item3.ReturnHome(debrisMoveTime + 0.05f);
            }
            yield return debrisMoveTime;

            routine.RemoveSelf();
            foreach (MoveBlockDebris item4 in debris)
            {
                item4.RemoveSelf();
            }
        Rebuild:
            Audio.Play(SFX.game_04_arrowblock_reappear, Position);
            Visible = true;
            Collidable = true;
            EnableStaticMovers();
            speed = targetSpeed = 0f;
            angle = targetAngle = homeAngle;
            noSquish = null;
            fillColor = idleBgFill;
            UpdateColors();
            flash = 1f;
            startInvisible = false;
        }
    }
}
