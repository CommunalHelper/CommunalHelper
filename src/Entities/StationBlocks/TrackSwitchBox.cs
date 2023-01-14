using System.Collections;
using static Celeste.Mod.CommunalHelper.Entities.StationBlockTrack;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/TrackSwitchBox")]
public class TrackSwitchBox : Solid
{
    private uint Seed;

    public static ParticleType P_Smash;

    private float circleRadius = 0f, circleOpacity = 0f;

    private readonly Sprite sprite;
    private readonly SineWave sine;
    private Vector2 start;

    private float sink;
    private bool canSwitch = true;
    private readonly bool canFloat = true;
    private readonly bool canBounce = true;

    private float shakeCounter;
    private bool smashParticles = false;

    private Vector2 bounceDir;
    private readonly Wiggler bounce;
    private readonly Shaker shaker;

    private Coroutine pulseRoutine;

    private readonly SoundSource Sfx;

    public static TrackSwitchState LocalTrackSwitchState;

    public static readonly Color OnColor = Calc.HexToColor("318eeb");
    public static readonly Color OffColor = Calc.HexToColor("e03a69");
    private float colorLerp;

    private readonly bool global = false;

    private bool spikesLeft, spikesRight, spikesUp, spikesDown;

    public TrackSwitchBox(Vector2 position, bool global, bool canFloat, bool canBounce)
        : base(position, 32f, 32f, safe: true)
    {

        colorLerp = (LocalTrackSwitchState = CommunalHelperModule.Session.TrackInitialState) == TrackSwitchState.On ? 0f : 1f;

        this.global = global;
        this.canFloat = canFloat;
        this.canBounce = canBounce;

        SurfaceSoundIndex = SurfaceIndex.ZipMover;
        start = Position;
        sprite = CommunalHelperGFX.SpriteBank.Create("trackSwitchBox");
        sprite.Position = new Vector2(Width, Height) / 2f;
        sprite.OnLastFrame = anim =>
        {
            if (anim == "switch")
                canSwitch = true;
        };

        Add(Sfx = new SoundSource()
        {
            Position = new Vector2(Width / 2, Height / 2)
        });

        Add(sprite);
        Add(sine = new SineWave(0.5f, 0f));
        bounce = Wiggler.Create(1f, 0.5f);
        bounce.StartZero = false;
        Add(bounce);
        Add(shaker = new Shaker(on: false));
        OnDashCollide = Dashed;
    }

    public TrackSwitchBox(EntityData e, Vector2 levelOffset)
        : this(e.Position + levelOffset, e.Bool("globalSwitch"), e.Bool("floaty"), e.Bool("bounce")) { }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);
        spikesUp = CollideCheck<Spikes>(Position - Vector2.UnitY);
        spikesDown = CollideCheck<Spikes>(Position + Vector2.UnitY);
        spikesLeft = CollideCheck<Spikes>(Position - Vector2.UnitX);
        spikesRight = CollideCheck<Spikes>(Position + Vector2.UnitX);
    }

    public DashCollisionResults Dashed(Player player, Vector2 dir)
    {
        if (!SaveData.Instance.Assists.Invincible)
        {
            if (dir == Vector2.UnitX && spikesLeft)
            {
                return DashCollisionResults.NormalCollision;
            }
            if (dir == -Vector2.UnitX && spikesRight)
            {
                return DashCollisionResults.NormalCollision;
            }
            if (dir == Vector2.UnitY && spikesUp)
            {
                return DashCollisionResults.NormalCollision;
            }
            if (dir == -Vector2.UnitY && spikesDown)
            {
                return DashCollisionResults.NormalCollision;
            }
        }

        if (canSwitch)
        {
            Sfx.Play(CustomSFX.game_trackSwitchBox_smash, "global_switch", global ? 1f : 0f);
            (Scene as Level).DirectionalShake(dir);
            sprite.Scale = new Vector2(1f + (Math.Abs(dir.Y) * 0.4f) - (Math.Abs(dir.X) * 0.4f), 1f + (Math.Abs(dir.X) * 0.4f) - (Math.Abs(dir.Y) * 0.4f));
            shakeCounter = 0.2f;
            shaker.On = true;
            bounceDir = dir;
            bounce.Start();
            smashParticles = true;
            Pulse();
            Add(new Coroutine(SwitchSequence()));
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            canSwitch = false;
            Switch(Scene, LocalTrackSwitchState.Invert(), global);
            return DashCollisionResults.Rebound;
        }

        return DashCollisionResults.NormalCollision;
    }

    public static bool Switch(Scene scene, TrackSwitchState state, bool global = false)
    {
        if (state == LocalTrackSwitchState)
            return false;
        LocalTrackSwitchState = state;
        SwitchTracks(scene, state);
        if (global)
            CommunalHelperModule.Session.TrackInitialState = state;
        return true;
    }

    private void SmashParticles(Vector2 dir)
    {
        float direction;
        Vector2 position;
        Vector2 positionRange;
        int num;
        if (dir == Vector2.UnitX)
        {
            direction = 0f;
            position = CenterRight - (Vector2.UnitX * 12f);
            positionRange = Vector2.UnitY * (Height - 6f) * 0.5f;
            num = (int) (Height / 8f) * 4;
        }
        else if (dir == -Vector2.UnitX)
        {
            direction = (float) Math.PI;
            position = CenterLeft + (Vector2.UnitX * 12f);
            positionRange = Vector2.UnitY * (Height - 6f) * 0.5f;
            num = (int) (Height / 8f) * 4;
        }
        else if (dir == Vector2.UnitY)
        {
            direction = (float) Math.PI / 2f;
            position = BottomCenter - (Vector2.UnitY * 12f);
            positionRange = Vector2.UnitX * (Width - 6f) * 0.5f;
            num = (int) (Width / 8f) * 4;
        }
        else
        {
            direction = -(float) Math.PI / 2f;
            position = TopCenter + (Vector2.UnitY * 12f);
            positionRange = Vector2.UnitX * (Width - 6f) * 0.5f;
            num = (int) (Width / 8f) * 4;
        }
        num += 2;
        SceneAs<Level>().Particles.Emit(P_Smash, num, position, positionRange, direction);
    }

    public override void Update()
    {
        base.Update();
        if (Scene.OnInterval(0.1f))
        {
            Seed++;
        }

        colorLerp = Calc.Approach(colorLerp, LocalTrackSwitchState == TrackSwitchState.On ? 0f : 1f, Engine.DeltaTime);
        circleOpacity = Calc.Approach(circleOpacity, 0f, Engine.DeltaTime);
        circleRadius += (64 - circleRadius) / 4 * Engine.DeltaTime * 20f;

        if (shakeCounter > 0f)
        {
            shakeCounter -= Engine.DeltaTime;
            if (shakeCounter <= 0f)
            {
                shaker.On = false;
                sprite.Scale = Vector2.One * 1.2f;
                sprite.Play("switch");
            }
        }
        if (Collidable)
        {
            bool flag = HasPlayerRider();
            sink = Calc.Approach(sink, flag ? 1 : 0, 2f * Engine.DeltaTime);
            sine.Rate = MathHelper.Lerp(1f, 0.5f, sink);
            Vector2 vector = start;
            if (canFloat)
            {
                vector.Y += (sink * 6f) + (sine.Value * MathHelper.Lerp(4f, 2f, sink));
            }
            if (canBounce)
            {
                vector += bounce.Value * bounceDir * 12f;
            }
            MoveToX(vector.X);
            MoveToY(vector.Y);
            if (smashParticles)
            {
                smashParticles = false;
                SmashParticles(bounceDir.Perpendicular());
                SmashParticles(-bounceDir.Perpendicular());
            }
        }
        sprite.Scale.X = Calc.Approach(sprite.Scale.X, 1f, Engine.DeltaTime * 4f);
        sprite.Scale.Y = Calc.Approach(sprite.Scale.Y, 1f, Engine.DeltaTime * 4f);
        LiftSpeed = Vector2.Zero;
    }

    public override void Render()
    {
        Draw.Circle(Center, circleRadius, Color.White * circleOpacity, 10);
        Vector2 position = sprite.Position;
        sprite.Position += shaker.Value;

        Rectangle rect = new(
            (int) (Center.X + ((X - Center.X) * sprite.Scale.X)),
            (int) (Center.Y + ((Y - Center.Y) * sprite.Scale.Y)),
            (int) (Width * sprite.Scale.X),
            (int) (Height * sprite.Scale.Y));
        rect.Inflate(-1, -1);

        uint seed = Seed;

        Draw.Rect(rect, Color.Lerp(OnColor, OffColor, colorLerp));

        for (int i = rect.Y; (float) i < rect.Bottom; i += 2)
        {
            float scale = 0.05f + ((1f + (float) Math.Sin((i / 16f) + (Scene.TimeActive * 2f))) / 2f * 0.2f);
            Draw.Line(rect.X, i, rect.X + rect.Width, i, Color.White * 0.55f * scale);
        }

        PlaybackBillboard.DrawNoise(rect, ref seed, Color.White * 0.1f);

        base.Render();
        sprite.Position = position;
    }

    private void Pulse()
    {
        pulseRoutine = new Coroutine(Lightning.PulseRoutine(SceneAs<Level>()));
        Add(pulseRoutine);
    }

    private IEnumerator SwitchSequence()
    {
        yield return 0.8f;
        SceneAs<Level>().DirectionalShake(Vector2.UnitX, 0.2f);
        Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
        if (global)
        {
            circleRadius = 0f;
            circleOpacity = .5f;
        }
    }

    public static void InitializeParticles()
    {
        P_Smash = new ParticleType(LightningBreakerBox.P_Smash)
        {
            Color = Calc.HexToColor("ff4076"),
            Color2 = Calc.HexToColor("57c7ff")
        };
    }

}
