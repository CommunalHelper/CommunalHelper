using Celeste.Mod.CommunalHelper.Components;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/AeroBlockSlingshot")]
public class AeroBlockSlingshot : AeroBlock
{
    // default values
    private const float DefaultLaunchTime = 0.5f;
    private const float DefaultCooldownTime = 0.5f;
    private const float DefaultSetTime = 0.25f;
    private const float DefaultDelayTime = 0.75f;
    private const float DefaultPushSpeed = 35f;
    private const Pushable.MoveActionType DefaultPushActions = Pushable.MoveActionType.Push;
    private const bool DefaultAllowAdjustments = true;
    private const string DefaultStartColor = "4BC0C8";
    private const string DefaultEndColor = "FEAC5E";

    // state values
    private float updateTimer = -1;
    private Color lockColor = Color.White;
    private float lockPercent;
    private Vector2 launchPosition;

    // cached positions
    private readonly Vector2 startPosition;
    private readonly Vector2[] positions;
    private readonly Vector2[] sortedPositions;
    private readonly Vector2 leftPosition;
    private readonly Vector2 rightPosition;

    // entities/components
    private readonly Pushable pushable;
    private readonly SoundSource sfx;
    private readonly StateMachine stateMachine;
    private PathRenderer pathRenderer;
    private AeroScreen_Percentage progressScreen;
    private AeroScreen_Wind windScreen;
    private AeroScreen_Blinker blinkerScreen;

    // config
    public readonly float LaunchTime;
    public readonly float CooldownTime;
    public readonly float SetTime;
    public readonly float DelayTime;
    public readonly float PushSpeed;
    public readonly bool AllowAdjustments;
    public readonly Color StartColor = Calc.HexToColor("4BC0C8");
    public readonly Color EndColor = Calc.HexToColor("FEAC5E");

    private float GetTrackLength() => Position.X > startPosition.X ? rightPosition.X - startPosition.X : startPosition.X - leftPosition.X;
    private float GetPercentFromPosition() => GetTrackLength() == 0 ? 0 : (Position.X - startPosition.X) / GetTrackLength();

    public enum SlingshotStates
    {
        Idle,
        Windup,
        Locked,
        Launch,
        Cooldown,
    }

    public AeroBlockSlingshot(EntityData data, Vector2 offset)
        : this(data.NodesWithPosition(offset), data.Width, data.Height,
            data.Float("launchTime", DefaultLaunchTime),
            data.Float("cooldownTime", DefaultCooldownTime),
            data.Float("setTime", DefaultSetTime),
            data.Float("delayTime", DefaultDelayTime),
            data.Float("pushSpeed", DefaultPushSpeed),
            data.Enum("pushActions", DefaultPushActions),
            data.Bool("allowAdjustments", DefaultAllowAdjustments),
            data.Attr("startColor", DefaultStartColor),
            data.Attr("endColor", DefaultEndColor))
    {
    }

    public AeroBlockSlingshot(Vector2[] positions, int width, int height,
        float launchTime = DefaultLaunchTime,
        float cooldownTime = DefaultCooldownTime,
        float setTime = DefaultSetTime,
        float delayTime = DefaultDelayTime,
        float pushSpeed = DefaultPushSpeed,
        Pushable.MoveActionType pushActions = DefaultPushActions,
        bool allowAdjustments = DefaultAllowAdjustments,
        string startColor = DefaultStartColor,
        string endColor = DefaultEndColor)
        : base(positions[0], width, height)
    {
        LaunchTime = launchTime;
        CooldownTime = cooldownTime;
        SetTime = setTime;
        DelayTime = delayTime;
        PushSpeed = pushSpeed;
        AllowAdjustments = allowAdjustments;
        StartColor = Calc.HexToColor(startColor);
        EndColor = Calc.HexToColor(endColor);

        startPosition = Position;

        for (int i = 0; i < positions.Length; i++)
            positions[i].Y = startPosition.Y;

        this.positions = positions;

        sortedPositions = positions.OrderBy(p => p.X).ToArray();
        leftPosition = sortedPositions.First();
        rightPosition = sortedPositions.Last();

        Add(pushable = new Pushable
        {
            OnPush = OnPush,
            PushCheck = PushCheck,
            MaxPushSpeed = PushSpeed,
            Active = false,
            MoveActions = pushActions,
        });

        Add(sfx = new SoundSource()
        {
            Position = new Vector2(width, height) / 2.0f,
        });

        stateMachine = new StateMachine();
        stateMachine.SetCallbacks((int) SlingshotStates.Idle, IdleUpdate, begin: IdleBegin, end: IdleEnd);
        stateMachine.SetCallbacks((int) SlingshotStates.Windup, WindupUpdate, begin: WindupBegin, end: WindupEnd);
        stateMachine.SetCallbacks((int) SlingshotStates.Locked, LockedUpdate, begin: LockedBegin, end: LockedEnd);
        stateMachine.SetCallbacks((int) SlingshotStates.Launch, LaunchUpdate, begin: LaunchBegin, end: LaunchEnd);
        stateMachine.SetCallbacks((int) SlingshotStates.Cooldown, CooldownUpdate, begin: CooldownBegin, end: CooldownEnd);
        stateMachine.State = (int) SlingshotStates.Idle;
        Add(stateMachine);

        progressScreen = new((int) Width, (int) Height)
        {
            Color = Color.Tomato,
        };
    }

    public override void Update()
    {
        base.Update();
        updateTimer -= Engine.DeltaTime;
    }

    private void IdleBegin()
    {
        pushable.Active = true;

        if (sfx.Playing)
            sfx.Stop();
    }

    private int IdleUpdate()
    {
        return (int) SlingshotStates.Idle;
    }

    private void IdleEnd()
    {
    }

    private void WindupBegin()
    {
        pushable.Active = true;
        updateTimer = SetTime;

        sfx.Stop();
        sfx.Param("lock", 0f);
        sfx.Play(CustomSFX.game_aero_block_push);

        progressScreen.ShowNumbers = true;

        if (!HasScreenLayer(progressScreen))
            AddScreenLayer(progressScreen);
    }

    private int WindupUpdate()
    {
        var currentPercent = GetPercentFromPosition();
        progressScreen.Percentage = Math.Abs(currentPercent);
        progressScreen.Color = Color.Lerp(StartColor, EndColor, Math.Abs(currentPercent));

        return updateTimer <= 0.0f
            ? HasMoved()
                ? (int) SlingshotStates.Locked
                : (int) SlingshotStates.Idle
            : (int) SlingshotStates.Windup;
    }

    private void WindupEnd()
    {
        progressScreen.ShowNumbers = false;

        if (!HasMoved() && HasScreenLayer(progressScreen))
            RemoveScreenLayer(progressScreen);
    }

    private void LockedBegin()
    {
        pushable.Active = AllowAdjustments;
        updateTimer = DelayTime;
        lockPercent = Math.Abs(GetPercentFromPosition());
        lockColor = progressScreen.Color = Color.Lerp(StartColor, EndColor, lockPercent);

        sfx.Param("lock", 1f);
        if (!sfx.Playing)
            sfx.Play(CustomSFX.game_aero_block_push);

        Audio.Play(CustomSFX.game_aero_block_lock, Center);

        StartShaking(0.2f);
        Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);

        if (!HasScreenLayer(progressScreen))
            AddScreenLayer(progressScreen);
    }

    private int LockedUpdate()
    {
        var t = 1f - Calc.Clamp(updateTimer / DelayTime, 0f, 1f);
        progressScreen.Color = Color.Lerp(Color.White, lockColor, t);
        progressScreen.Percentage = 1 - t;
        return updateTimer <= 0 ? (int) SlingshotStates.Launch : (int) SlingshotStates.Locked;
    }

    private void LockedEnd()
    {
    }

    private void LaunchBegin()
    {
        pushable.Active = false;
        updateTimer = LaunchTime;
        launchPosition = Position;

        Audio.Play(CustomSFX.game_aero_block_ding, Center);
        sfx.Play(CustomSFX.game_aero_block_wind_up);

        if (HasScreenLayer(progressScreen))
            RemoveScreenLayer(progressScreen);

        if (blinkerScreen is not null && HasScreenLayer(blinkerScreen))
            RemoveScreenLayer(blinkerScreen);

        if (windScreen is not null && HasScreenLayer(windScreen))
            RemoveScreenLayer(windScreen);

        AddScreenLayer(blinkerScreen = new AeroScreen_Blinker(null)
        {
            BackgroundColor = lockColor,
            FadeIn = 0.0f,
            Hold = 0.1f,
            FadeOut = 0.5f,
        });

        blinkerScreen.Update();
        blinkerScreen.Complete = true;

        Vector2 windVel = Vector2.UnitX * Math.Sign(startPosition.X - Position.X) * 200;
        AddScreenLayer(windScreen = new((int) Width, (int) Height, windVel)
        {
            Color = lockColor,
            Wind = windVel,
        });
    }

    private int LaunchUpdate()
    {
        if (Scene.OnInterval(0.1f))
            pathRenderer.CreateSparks();

        var t = 1f - Calc.Clamp(updateTimer / LaunchTime, 0f, 1f);
        var percent = Ease.SineIn(t);

        var target = Vector2.Lerp(launchPosition, startPosition, percent);
        MoveJumpthrus(target - ExactPosition);
        MoveTo(target);
        sfx.Param("wind_percent", percent);

        return updateTimer <= 0 ? (int) SlingshotStates.Cooldown : (int) SlingshotStates.Launch;
    }

    private void LaunchEnd()
    {
        var level = SceneAs<Level>();
        level.Shake();

        Audio.Play(CustomSFX.game_aero_block_impact, Center);
        sfx.Play(CustomSFX.game_aero_block_push);
        sfx.Pause();
        StartShaking(0.3f);
        Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);

        if (HasScreenLayer(blinkerScreen))
            RemoveScreenLayer(blinkerScreen);
    }

    private void CooldownBegin()
    {
        pushable.Active = false;
        updateTimer = CooldownTime;

        if (windScreen is not null)
            windScreen.Wind = Vector2.Zero;

        if (sfx.Playing)
            sfx.Stop();

        if (HasScreenLayer(progressScreen))
            RemoveScreenLayer(progressScreen);
    }

    private int CooldownUpdate()
    {
        if (windScreen is not null)
        {
            var t = 1f - Calc.Clamp(updateTimer / CooldownTime, 0f, 1f);
            windScreen.Color = Color.Lerp(lockColor, Color.Transparent, t);
        }

        return updateTimer <= 0 ? (int) SlingshotStates.Idle : (int) SlingshotStates.Cooldown;
    }

    private void CooldownEnd()
    {
        if (windScreen is not null && HasScreenLayer(windScreen))
            RemoveScreenLayer(windScreen);
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);
        scene.Add(pathRenderer = new PathRenderer(this));
    }

    public override void Removed(Scene scene)
    {
        pathRenderer?.RemoveSelf();
        pathRenderer = null;
        base.Removed(scene);
    }

    private bool PushCheck(int moveX, Pushable.MoveActionType moveAction)
    {
        if (moveX > 0 && Position.X < rightPosition.X)
            return true;
        if (moveX < 0 && Position.X > leftPosition.X)
            return true;
        return false;
    }

    private void OnPush(int moveX, Pushable.MoveActionType moveAction)
    {
        stateMachine.State = (int) SlingshotStates.Windup;
        updateTimer = SetTime;

        var speed = (pushable.MaxPushSpeed < 0 ? 70f : Math.Min(Player.MaxRun, pushable.MaxPushSpeed)) * moveX;
        MoveJumpthrus(Vector2.UnitX * speed * Engine.DeltaTime);
    }

    private bool HasMoved() => Math.Abs(Position.X - startPosition.X) > 0.01f;

    private class PathRenderer : Entity
    {
        private static readonly Color ropeColor = Calc.HexToColor("663931");
        private static readonly Color ropeLightColor = Calc.HexToColor("9b6157");

        private readonly AeroBlockSlingshot slingshot;
        private readonly MTexture cog;
        private readonly Vector2 sparkAdd = Vector2.UnitY * 5f;

        public PathRenderer(AeroBlockSlingshot slingshot)
        {
            Depth = 5000;
            this.slingshot = slingshot;
            cog = GFX.Game["objects/zipmover/cog"];
        }

        public void CreateSparks()
        {
            for (int i = 0; i < slingshot.positions.Length; i++)
            {
                var position = slingshot.positions[i] + new Vector2(slingshot.Width / 2, slingshot.Height / 2).Round();
                SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, position + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), -Vector2.UnitY.Angle());
                SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, position - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), Vector2.UnitY.Angle());
            }
        }

        public override void Render()
        {
            DrawCogs(Vector2.UnitY, Color.Black);
            DrawCogs(Vector2.Zero);
            Draw.Rect(new Rectangle((int) (slingshot.X + (double) slingshot.Shake.X - 1.0), (int) (slingshot.Y + (double) slingshot.Shake.Y - 1.0), (int) slingshot.Width + 2, (int) slingshot.Height + 2), Color.Black);
        }

        private void DrawCogs(Vector2 offset, Color? colorOverride = null)
        {
            var percent = slingshot.GetPercentFromPosition();
            offset += new Vector2(slingshot.Width / 2, slingshot.Height / 2).Round();

            for (int i = 0; i < slingshot.sortedPositions.Length - 1; i++)
            {
                var from = slingshot.sortedPositions[i];
                var to = slingshot.sortedPositions[i + 1];
                Vector2 normal = (to - from).SafeNormalize();
                Vector2 normalPerp = normal.Perpendicular() * 3f;
                Vector2 normalNegPerp = -normal.Perpendicular() * 4f;
                float rotation = (float) (percent * Math.PI * 2.0);

                Draw.Line(from + normalPerp + offset, to + normalPerp + offset, colorOverride ?? ropeColor);
                Draw.Line(from + normalNegPerp + offset, to + normalNegPerp + offset, colorOverride ?? ropeColor);

                for (float num = (float) (4.0 - percent * Math.PI * 8.0 % 4.0); num < (double) (to - from).Length(); num += 4f)
                {
                    Vector2 lineOnePosition = from + normalPerp + normal.Perpendicular() + normal * num;
                    Vector2 lineTwoPosition = to + normalNegPerp - normal * num;
                    Draw.Line(lineOnePosition + offset, lineOnePosition + normal * 2f + offset, colorOverride ?? ropeLightColor);
                    Draw.Line(lineTwoPosition + offset, lineTwoPosition - normal * 2f + offset, colorOverride ?? ropeLightColor);
                }

                cog.DrawCentered(from + offset, colorOverride ?? Color.White, 1f, rotation);
                cog.DrawCentered(to + offset, colorOverride ?? Color.White, 1f, rotation);
            }
        }
    }
}
