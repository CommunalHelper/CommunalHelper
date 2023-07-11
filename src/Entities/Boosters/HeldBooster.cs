using Celeste.Mod.CommunalHelper.Imports;
using FMOD.Studio;
using MonoMod.Utils;
using System.Collections;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/HeldBooster")]
public class HeldBooster : CustomBooster
{
    public class PathRenderer : PathRendererBase<HeldBooster>
    {
        private readonly Vector2 dir, perp;
        private float length, lerp;

        public PathRenderer(float alpha, Vector2 direction, HeldBooster booster)
            : base(alpha, booster.style, PathColors, booster)
        {
            dir = direction;
            perp = direction.Perpendicular();
            Depth = booster.Depth + 1;
        }

        public override void Update()
        {
            base.Update();

            if (Booster.BoostingPlayer)
            {
                lerp = 1f;
                length = Vector2.Distance(Booster.Sprite.Position + Booster.Center, Booster.start);
            }
            else
            {
                lerp = Calc.Approach(lerp, 0f, Engine.DeltaTime);
            }
        }

        public override void Render()
        {
            base.Render();

            if (Alpha <= 0f)
                return;

            Color color = Booster.BoostingPlayer ? Color : Color.White;

            Player player = null;
            if (Booster.proximityPath)
                Util.TryGetPlayer(out player);

            float sineout = Ease.CubeOut(lerp);
            float l = MathHelper.Lerp(128, length, Ease.ExpoInOut(lerp));
            for (float f = 0f; f < l; f += 6f)
            {
                float t = f / l;
                float opacity = MathHelper.Lerp(1 - Ease.QuadOut(t), 1f, sineout);
                DrawPathLine(Calc.Round(Booster.start + (dir * f)), dir, perp, f, player, color, opacity);
            }
        }
    }

    public static readonly Color[] PathColors = new[] {
        Calc.HexToColor("0abd32"),
        Calc.HexToColor("0df235"),
        Calc.HexToColor("32623a"),
        Calc.HexToColor("6ef7ad"),
    };

    public static readonly Color PurpleBurstColor = Calc.HexToColor("7a1053");
    public static readonly Color PurpleAppearColor = Calc.HexToColor("e619c3");
    public static readonly Color GreenBurstColor = Calc.HexToColor("174f21");
    public static readonly Color GreenAppearColor = Calc.HexToColor("0df235");

    public static readonly Color GreenBlink = Color.Red;
    public static readonly Color PurpleBlink = Calc.HexToColor("880000");

    private readonly bool green;

    private readonly Vector2 start;
    private readonly Vector2 dir;

    private Vector2 aim, prevAim;
    private float targetAngle;
    private float anim;
    private bool hasPlayer;

    private readonly Sprite arrow;

    private PathRenderer pathRenderer;
    private readonly PathStyle style;
    private readonly bool proximityPath;

    private readonly float speed;
    private readonly float deathTimer;

    private bool blink;
    private readonly SoundSource blinkSfx = new();
    private readonly bool blinkSoundEnabled;

    public HeldBooster(EntityData data, Vector2 offset)
        : this(data.Position + offset, data.Float("speed", 240f), data.FirstNodeNullable(offset), data.Enum("pathStyle", PathStyle.Arrow), data.Bool("proximityPath", true), data.Float("deathTimer", -1), data.Bool("blinkSfx", true)) { }

    public HeldBooster(Vector2 position, float speed = 240f, Vector2? node = null, PathStyle style = PathStyle.Arrow, bool proximityPath = true, float deathTimer = -1, bool blinkSoundEnabled = true)
        : base(position, redBoost: true)
    {
        green = node is not null && node.Value != position;

        ReplaceSprite(CommunalHelperGFX.SpriteBank.Create(green ? "greenHeldBooster" : "purpleHeldBooster"));

        Add(arrow = CommunalHelperGFX.SpriteBank.Create("heldBoosterArrow"));
        arrow.Visible = false;

        SetParticleColors(
            green ? GreenBurstColor : PurpleBurstColor,
            green ? GreenAppearColor : PurpleAppearColor
        );

        SetSoundEvent(
            SFX.game_05_redbooster_enter,
            CustomSFX.game_customBoosters_heldBooster_move,
            playMoveEnd: true
        );

        MovementInBubbleFactor = green ? 0 : 1.5f;

        start = position;
        dir = ((node ?? Vector2.Zero) - start).SafeNormalize();

        this.speed = speed;
        this.deathTimer = deathTimer;

        this.style = style;
        this.proximityPath = proximityPath;

        Add(blinkSfx);
        this.blinkSoundEnabled = blinkSoundEnabled;
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);
        if (green) scene.Add(pathRenderer = new PathRenderer(1f, dir, this));
    }

    public override void Removed(Scene scene)
    {
        base.Removed(scene);
        if (green) scene.Remove(pathRenderer);
        pathRenderer = null;
    }

    protected override void OnPlayerEnter(Player player)
    {
        base.OnPlayerEnter(player);
        Collidable = false;
        hasPlayer = true;
        blink = false;

        if (green)
            anim = 1f;
        else
            SetAim(Vector2.UnitX * (int) player.Facing, pulse: true);

        arrow.Play("inside");
    }

    protected override void OnPlayerExit(Player player)
    {
        base.OnPlayerExit(player);
        Collidable = true;
        hasPlayer = false;

        arrow.Play("pop");
    }

    // prevents held boosters from starting automatically (which red boosters do after 0.25 seconds).
    protected override IEnumerator BoostCoroutine(Player player)
    {
        if (deathTimer <= 0f)
        {
            // deathTimer is ignored, player has no time constraint, so we wait infinitely.
            while (true)
                yield return null;
        }

        EventInstance sound = null;
        if (blinkSoundEnabled)
        {
            blinkSfx.Play(CustomSFX.game_customBoosters_heldBooster_blink);
            sound = DynamicData.For(blinkSfx).Get<EventInstance>("instance");
            sound.setVolume(0.5f);
        }

        bool prevBlink = blink;
        float t = 0f;
        while (t < deathTimer)
        {
            float percent = t / deathTimer;
            float ease = Ease.QuadIn(percent);

            float largestBlink = Settings.Instance.DisableFlashes ? 0.5f : 0.25f;
            float smallestBlink = Settings.Instance.DisableFlashes ? 0.25f : 0.13f;
            float blinkDuration = MathHelper.Lerp(largestBlink, smallestBlink, ease);
            blink = Util.Blink(t, blinkDuration);

            if (blinkSoundEnabled && prevBlink != blink)
            {
                if (blink)
                {
                    int ms = (int) (ease * 60);
                    sound.setTimelinePosition(ms);
                }
                prevBlink = blink;
            }

            t += Engine.DeltaTime;
            yield return null;
        }

        blink = true;
        if (blinkSoundEnabled)
            blinkSfx.Stop();

        player.Die(Vector2.Zero);

        Audio.Play(SFX.game_05_redbooster_end);

        yield break;
    }

    protected override int? RedDashUpdateAfter(Player player)
    {
        return !Input.Dash.Check ? Player.StNormal : null;
    }

    protected override IEnumerator RedDashCoroutineAfter(Player player)
    {
        blink = false;

        DynamicData data = new(player);

        Vector2 direction = green ? dir : aim;

        aim = direction;
        anim = 1.75f;

        player.DashDir = direction;
        data.Set("gliderBoostDir", direction);
        player.Speed = direction * speed;

        // If the player is inverted, invert its vertical speed so that it moves in the same direction no matter what.
        if (GravityHelper.IsPlayerInverted?.Invoke() ?? false)
            player.Speed.Y *= -1f;

        player.SceneAs<Level>().DirectionalShake(player.DashDir, 0.2f);
        if (player.DashDir.X != 0f)
            player.Facing = (Facings) Math.Sign(player.DashDir.X);

        yield break;
    }

    private void SetAim(Vector2 v, bool pulse = false)
    {
        if (v == Vector2.Zero)
            return;

        v.Normalize();
        aim = v;
        targetAngle = v.Angle();

        if (pulse)
            anim = 1f;
    }

    protected override void OnRespawn()
    {
        arrow.Play("loop", restart: true);
        blink = false;
    }

    public override void Update()
    {
        base.Update();

        if (!green && hasPlayer && !BoostingPlayer)
        {
            Vector2 v = Input.Aim.Value == Vector2.Zero ? Vector2.Zero : Input.GetAimVector();
            SetAim(v, v != prevAim);
            prevAim = v;
        }

        anim = Calc.Approach(anim, 0f, Engine.DeltaTime * 2f);

        Sprite.Color = blink
            ? green
                ? GreenBlink
                : PurpleBlink
            : Color.White;
    }

    public override void Render()
    {
        Sprite sprite = Sprite;

        float ease = Ease.BounceIn(anim);

        Vector2 offset = aim * ease;

        bool inside = sprite.CurrentAnimationID is "inside";
        float verticalCorrection = inside ? 3 : 2;
        Vector2 pos = Center + sprite.Position + offset - new Vector2(0, verticalCorrection);

        float angle = green
            ? dir.Angle()
            : targetAngle;

        Vector2 scale = new(1 + (ease * 0.25f), 1 - (ease * 0.25f));

        bool greenFlag = true;
        bool purpleFlag = sprite.CurrentAnimationID is not "loop";

        bool visibleArrow = (green && greenFlag) || (!green && purpleFlag);
        bool pop = sprite.CurrentAnimationID is "pop";

        if (visibleArrow && !pop)
        {
            arrow.Texture.DrawCentered(pos + Vector2.UnitX, Color.Black, scale, angle);
            arrow.Texture.DrawCentered(pos - Vector2.UnitX, Color.Black, scale, angle);
            arrow.Texture.DrawCentered(pos + Vector2.UnitY, Color.Black, scale, angle);
            arrow.Texture.DrawCentered(pos - Vector2.UnitY, Color.Black, scale, angle);
        }

        base.Render();

        if (visibleArrow)
        {
            arrow.Texture.DrawCentered(pos, blink ? Color.Red : Color.White, scale, angle);
        }
    }
}
