using Celeste.Mod.CommunalHelper.Components;
using FMOD.Studio;
using System.Collections;
using System.Collections.Generic;
using Directions = Celeste.MoveBlock.Directions;

// TODO
// movement stuff
// Dealing with the period of non collidability when respawning
// fix static movers during respawn
// fix block staying collidable after breaking
namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/CassetteMoveBlock")]
public class CassetteMoveBlock : CustomCassetteBlock
{
    public enum MovementState
    {
        Idling,
        Moving,
        Breaking
    }

    private const float Accel = 300f;
    private const float MoveSpeed = 60f;
    private const float FastMoveSpeed = 75f;
    private const float SteerSpeed = (float) Math.PI * 16f;
    private const float NoSteerTime = 0.2f;
    private const float CrashResetTime = 0.1f;
    private const float CrashStartShakingTime = 0.15f;
    private readonly float crashTime;
    private readonly float regenTime;
    private readonly bool shakeOnCollision;

    private readonly float moveSpeed;
    public Directions Direction;
    private readonly float homeAngle;
    private Vector2 startPosition;
    public MovementState State = MovementState.Idling;

    private float speed;
    private float targetSpeed;
    private float angle;
    private float targetAngle;

    private Player noSquish;

    private Image arrow;
    private Image arrowPressed;
    private Image cross;
    private Image crossPressed;

    private float flash;
    private readonly SoundSource moveSfx;
    private bool triggered;
    private float particleRemainder;

    private readonly ParticleType P_Activate;
    private readonly ParticleType P_Move;
    private readonly ParticleType P_MovePressed;
    private readonly ParticleType P_Break;
    private readonly ParticleType P_BreakPressed;

    public CassetteMoveBlock(Vector2 position, EntityID id, int width, int height, Directions direction, float moveSpeed, int index, float tempo, bool oldConnectionBehavior, Color? overrideColor, float crashTime, float regenTime, bool shakeOnCollision)
        : base(position, id, width, height, index, tempo, true, oldConnectionBehavior, dynamicHitbox: true, overrideColor)
    {
        startPosition = position;
        Direction = direction;
        this.moveSpeed = moveSpeed;

        homeAngle = targetAngle = angle = direction.Angle();

        this.crashTime = crashTime;
        this.regenTime = regenTime;
        this.shakeOnCollision = shakeOnCollision;

        Add(moveSfx = new SoundSource());
        Add(new Coroutine(Controller()));

        P_Activate = new ParticleType(MoveBlock.P_Activate) { Color = color };
        P_Move = new ParticleType(MoveBlock.P_Move) { Color = color };
        P_MovePressed = new ParticleType(MoveBlock.P_Move) { Color = pressedColor };
        P_Break = new ParticleType(MoveBlock.P_Break) { Color = color };
        P_BreakPressed = new ParticleType(MoveBlock.P_Break) { Color = pressedColor };

        Add(new MoveBlockRedirectable(new MonoMod.Utils.DynamicData(this), () => false, () => Direction, dir =>
            {
                int index = (int) Math.Floor(((0f - angle + ((float) Math.PI * 2f)) % ((float) Math.PI * 2f) / ((float) Math.PI * 2f) * 8f) + 0.5f);
                arrow.Texture = GFX.Game.GetAtlasSubtextures("objects/CommunalHelper/cassetteMoveBlock/arrow")[index];
                arrowPressed.Texture = GFX.Game.GetAtlasSubtextures("objects/CommunalHelper/cassetteMoveBlock/arrowPressed")[index];
                Direction = dir;
            }
        ));
    }

    public CassetteMoveBlock(EntityData data, Vector2 offset, EntityID id)
        : this(data.Position + offset, id, data.Width, data.Height, data.Enum("direction", Directions.Left), data.Bool("fast") ? FastMoveSpeed : data.Float("moveSpeed", MoveSpeed), data.Int("index"), data.Float("tempo", 1f), data.Bool("oldConnectionBehavior", true), data.HexColorNullable("customColor"), data.Float("crashTime", 0.15f), data.Float("regenTime", 3f), data.Bool("shakeOnCollision", true))
    {
    }

    public override void Awake(Scene scene)
    {
        int index = (int) Math.Floor(((0f - angle + ((float) Math.PI * 2f)) % ((float) Math.PI * 2f) / ((float) Math.PI * 2f) * 8f) + 0.5f);
        arrow = new Image(GFX.Game.GetAtlasSubtextures("objects/CommunalHelper/cassetteMoveBlock/arrow")[index]);
        arrowPressed = new Image(GFX.Game.GetAtlasSubtextures("objects/CommunalHelper/cassetteMoveBlock/arrowPressed")[index]);
        cross = new Image(GFX.Game["objects/CommunalHelper/cassetteMoveBlock/x"]);
        crossPressed = new Image(GFX.Game["objects/CommunalHelper/cassetteMoveBlock/xPressed"]);

        base.Awake(scene);
        AddCenterSymbol(arrow, arrowPressed);
        AddCenterSymbol(cross, crossPressed);
    }

    private IEnumerator Controller()
    {
        while (true)
        {
            triggered = false;
            State = MovementState.Idling;
            while (!triggered && !HasPlayerRider())
                yield return null;

            Audio.Play(SFX.game_04_arrowblock_activate, Position);
            State = MovementState.Moving;
            StartShaking(0.2f);
            ActivateParticles();
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
                speed = Calc.Approach(speed, targetSpeed, Accel * Engine.DeltaTime);
                angle = Calc.Approach(angle, targetAngle, SteerSpeed * Engine.DeltaTime);
                Vector2 move = Calc.AngleToVector(angle, speed) * Engine.DeltaTime;
                bool hit;
                if (Direction is Directions.Right or Directions.Left)
                {
                    hit = MoveCheck(move.XComp());
                    noSquish = Scene.Tracker.GetEntity<Player>();
                    MoveVCollideSolids(move.Y, thruDashBlocks: false);
                    noSquish = null;
                    if (Scene.OnInterval(0.03f))
                    {
                        if (move.Y > 0f)
                        {
                            ScrapeParticles(Vector2.UnitY);
                        }
                        else if (move.Y < 0f)
                        {
                            ScrapeParticles(-Vector2.UnitY);
                        }
                    }
                }
                else
                {
                    hit = MoveCheck(move.YComp());
                    noSquish = Scene.Tracker.GetEntity<Player>();
                    MoveHCollideSolids(move.X, thruDashBlocks: false);
                    noSquish = null;
                    if (Scene.OnInterval(0.03f))
                    {
                        if (move.X > 0f)
                        {
                            ScrapeParticles(Vector2.UnitX);
                        }
                        else if (move.X < 0f)
                        {
                            ScrapeParticles(-Vector2.UnitX);
                        }
                    }
                    if (Direction == Directions.Down && Top > SceneAs<Level>().Bounds.Bottom + 32)
                    {
                        hit = true;
                    }
                }
                if (hit)
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
                if (Left < level.Bounds.Left || Top < level.Bounds.Top || Right > level.Bounds.Right)
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
            ((MoveBlockRedirectable) Get<Redirectable>())?.ResetBlock();
            List<MoveBlockDebris> debris = new();
            for (int x = 0; x < Width; x += 8)
            {
                for (int y = 0; y < Height; y += 8)
                {
                    Vector2 offset = new(x + 4f, y + 4f);

                    MoveBlockDebris d = Engine.Pooler.Create<MoveBlockDebris>().Init(Position + offset, Center, startPosition + offset, spr =>
                    {
                        spr.Color = Activated ? color : pressedColor;
                    });
                    d.Sprite.Texture = Calc.Random.Choose(GFX.Game.GetAtlasSubtextures("objects/CommunalHelper/cassetteMoveBlock/debris"));
                    debris.Add(d);
                    Scene.Add(d);
                }
            }
            Vector2 newPosition = startPosition + blockOffset;
            SetDisabledStaticMoversVisibility(false);
            SetStaticMoversVisible(false);
            MoveStaticMovers(newPosition - Position);
            Position = newPosition;
            Visible = false;
            Present = false;

            float waitTime = Calc.Clamp(regenTime - 0.8f, 0, float.MaxValue);
            float debrisShakeTime = Calc.Clamp(regenTime - 0.6f, 0, 0.2f);
            float debrisMoveTime = Calc.Clamp(regenTime, 0, 0.6f);

            yield return waitTime;

            foreach (MoveBlockDebris d in debris)
                d.StopMoving();
            while (CollideCheck<Actor>() || CollideCheck<Solid>())
                yield return null;

            Present = true;
            EventInstance sound = Audio.Play(SFX.game_04_arrowblock_reform_begin, debris[0].Position);
            Coroutine component;
            Coroutine routine = component = new Coroutine(SoundFollowsDebrisCenter(sound, debris));
            Add(component);
            foreach (MoveBlockDebris d in debris)
                d.StartShaking();
            yield return debrisShakeTime;

            foreach (MoveBlockDebris d in debris)
                d.ReturnHome(debrisMoveTime + 0.05f);
            yield return debrisMoveTime;

            routine.RemoveSelf();
            foreach (MoveBlockDebris d in debris)
                d.RemoveSelf();
            Audio.Play("event:/game/04_cliffside/arrowblock_reappear", Position);
            Visible = true;
            SetDisabledStaticMoversVisibility(true);
            SetStaticMoversVisible(true);

            if (Collidable)
                EnableStaticMovers();
            else
            {
                // would inexplicably appear turned on
                DisableStaticMovers();
            }

            speed = targetSpeed = 0f;
            angle = targetAngle = homeAngle;
            noSquish = null;
            flash = 1f;
        }
    }

    private IEnumerator SoundFollowsDebrisCenter(EventInstance instance, List<MoveBlockDebris> debris)
    {
        while (true)
        {
            instance.getPlaybackState(out PLAYBACK_STATE state);
            if (state == PLAYBACK_STATE.STOPPED)
            {
                break;
            }
            Vector2 center = Vector2.Zero;
            foreach (MoveBlockDebris d in debris)
            {
                center += d.Position;
            }
            center /= debris.Count;
            Audio.Position(instance, center);
            yield return null;
        }
    }

    public override void Update()
    {
        base.Update();
        if (moveSfx != null && moveSfx.Playing)
        {
            float num = (Calc.AngleToVector(angle, 1f) * new Vector2(-1f, 1f)).Angle();
            int num2 = (int) Math.Floor(((0f - num + ((float) Math.PI * 2f)) % ((float) Math.PI * 2f) / ((float) Math.PI * 2f) * 8f) + 0.5f);
            moveSfx.Param("arrow_influence", num2 + 1);
        }
        flash = Calc.Approach(flash, 0f, Engine.DeltaTime * 5f);
    }

    public override void OnStaticMoverTrigger(StaticMover sm)
    {
        triggered = true;
    }

    public override void MoveHExact(int move)
    {
        if (noSquish != null && ((move < 0 && noSquish.X < X) || (move > 0 && noSquish.X > X)))
        {
            while (move != 0 && noSquish.CollideCheck<Solid>(noSquish.Position + (Vector2.UnitX * move)))
            {
                move -= Math.Sign(move);
            }
        }
        base.MoveHExact(move);
    }

    public override void MoveVExact(int move)
    {
        if (noSquish != null && move < 0 && noSquish.Y <= Y)
        {
            while (move != 0 && noSquish.CollideCheck<Solid>(noSquish.Position + (Vector2.UnitY * move)))
            {
                move -= Math.Sign(move);
            }
        }
        base.MoveVExact(move);
    }

    private bool MoveCheck(Vector2 speed)
    {
        if (speed.X != 0f)
        {
            if (MoveHCollideSolids(speed.X, thruDashBlocks: false))
            {
                for (int i = 1; i <= 3; i++)
                {
                    for (int num = 1; num >= -1; num -= 2)
                    {
                        Vector2 value = new(Math.Sign(speed.X), i * num);
                        if (!CollideCheck<Solid>(Position + value))
                        {
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
        if (speed.Y != 0f)
        {
            if (MoveVCollideSolids(speed.Y, thruDashBlocks: false))
            {
                for (int j = 1; j <= 3; j++)
                {
                    for (int num2 = 1; num2 >= -1; num2 -= 2)
                    {
                        Vector2 value2 = new(j * num2, Math.Sign(speed.Y));
                        if (!CollideCheck<Solid>(Position + value2))
                        {
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

    public override void Render()
    {
        Vector2 position = Position;
        Position += Shake;
        base.Render();
        float num = flash * 4f;
        Draw.Rect(X - num, Y - num, Width + (num * 2f), Height + (num * 2f), Color.White * flash);
        Position = position;
    }

    public override void HandleUpdateVisualState()
    {
        base.HandleUpdateVisualState();
        bool crossVisible = State == MovementState.Breaking;
        arrow.Visible &= !crossVisible;
        arrowPressed.Visible &= !crossVisible;
        cross.Visible &= crossVisible;
        crossPressed.Visible &= crossVisible;
    }

    private void ActivateParticles()
    {
        bool left = !CollideCheck<Player>(Position - Vector2.UnitX);
        bool right = !CollideCheck<Player>(Position + Vector2.UnitX);
        bool top = !CollideCheck<Player>(Position - Vector2.UnitY);
        if (left)
        {
            SceneAs<Level>().ParticlesBG.Emit(P_Activate, (int) (Height / 2f), CenterLeft, Vector2.UnitY * (Height - 4f) * 0.5f, (float) Math.PI);
        }
        if (right)
        {
            SceneAs<Level>().ParticlesBG.Emit(P_Activate, (int) (Height / 2f), CenterRight, Vector2.UnitY * (Height - 4f) * 0.5f, 0f);
        }
        if (top)
        {
            SceneAs<Level>().ParticlesBG.Emit(P_Activate, (int) (Width / 2f), TopCenter, Vector2.UnitX * (Width - 4f) * 0.5f, -(float) Math.PI / 2f);
        }
        SceneAs<Level>().ParticlesBG.Emit(P_Activate, (int) (Width / 2f), BottomCenter, Vector2.UnitX * (Width - 4f) * 0.5f, (float) Math.PI / 2f);
    }

    private void BreakParticles()
    {
        Vector2 center = Center;
        ParticleType particle = Collidable ? P_Break : P_BreakPressed;
        for (int i = 0; i < Width; i += 4)
        {
            for (int j = 0; j < Height; j += 4)
            {
                Vector2 vector = Position + new Vector2(2 + i, 2 + j);
                SceneAs<Level>().Particles.Emit(particle, 1, vector, Vector2.One * 2f, (vector - center).Angle());
            }
        }
    }

    private void MoveParticles()
    {
        Vector2 position;
        Vector2 positionRange;
        float num;
        float num2;
        if (Direction == Directions.Right)
        {
            position = CenterLeft + Vector2.UnitX;
            positionRange = Vector2.UnitY * (Height - 4f);
            num = (float) Math.PI;
            num2 = Height / 32f;
        }
        else if (Direction == Directions.Left)
        {
            position = CenterRight;
            positionRange = Vector2.UnitY * (Height - 4f);
            num = 0f;
            num2 = Height / 32f;
        }
        else if (Direction == Directions.Down)
        {
            position = TopCenter + Vector2.UnitY;
            positionRange = Vector2.UnitX * (Width - 4f);
            num = -(float) Math.PI / 2f;
            num2 = Width / 32f;
        }
        else
        {
            position = BottomCenter;
            positionRange = Vector2.UnitX * (Width - 4f);
            num = (float) Math.PI / 2f;
            num2 = Width / 32f;
        }
        particleRemainder += num2;
        int num3 = (int) particleRemainder;
        particleRemainder -= num3;
        positionRange *= 0.5f;
        if (num3 > 0)
        {
            SceneAs<Level>().ParticlesBG.Emit(Collidable ? P_Move : P_MovePressed, num3, position, positionRange, num);
        }
    }

    private void ScrapeParticles(Vector2 dir)
    {
        if (Collidable)
        {
            Collidable = false;
            if (dir.X != 0f)
            {
                float x = (!(dir.X > 0f)) ? (Left - 1f) : Right;
                for (int i = 0; i < Height; i += 8)
                {
                    Vector2 vector = new(x, Top + 4f + i);
                    if (Scene.CollideCheck<Solid>(vector))
                    {
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, vector);
                    }
                }
            }
            else
            {
                float y = (!(dir.Y > 0f)) ? (Top - 1f) : Bottom;
                for (int j = 0; j < Width; j += 8)
                {
                    Vector2 vector2 = new(Left + 4f + j, y);
                    if (Scene.CollideCheck<Solid>(vector2))
                    {
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, vector2);
                    }
                }
            }
            Collidable = true;
        }
    }
}
