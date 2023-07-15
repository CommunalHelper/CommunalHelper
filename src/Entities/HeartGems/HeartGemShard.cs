using MonoMod.Utils;
using System.Collections.Generic;
using XNAColor = Microsoft.Xna.Framework.Color;

namespace Celeste.Mod.CommunalHelper.Entities;

[Tracked]
public class HeartGemShard : Entity
{
    /// <summary>
    /// DynamicData field name for storing HeartGemShards in HeartGem.
    /// </summary>
    public const string HeartGem_HeartGemPieces = "communalHelperGemPieces";
    /// <summary>
    /// Dictionary entry name for storing the HeartGem EntityID in EntityData.<br/>
    /// Also used as DynamicData field name for storing EntityID in HeartGem.
    /// </summary>
    public const string HeartGem_HeartGemID = "communalHelperHeartGemID";

    public static ParticleType P_Burst;

    public HeartGem Heart;
    public bool Collected;

    public XNAColor? Color;

    protected DynamicData heartData;
    protected int index;

    // Separated sprite from outline for cleaner tinting
    private readonly Image sprite;
    private readonly Image outline;
    private readonly HoldableCollider holdableCollider;

    private bool merging;

    private ParticleType shineParticle;
    private VertexLight light;
    private Tween lightTween;

    private readonly Wiggler scaleWiggler;
    private readonly Wiggler moveWiggler;
    private Vector2 moveWiggleDir;
    private readonly Shaker shaker;
    private float timer;
    private float bounceSfxDelay;

    private readonly SoundSource collectSfx;

    public static void InitializeParticles()
    {
        P_Burst = new ParticleType
        {
            Source = GFX.Game["particles/shard"],
            Size = 0.5f,
            Color = new XNAColor(0.8f, 1f, 1f),

            FadeMode = ParticleType.FadeModes.Late,
            LifeMin = 0.3f,
            LifeMax = 0.5f,

            SizeRange = 0.4f,
            SpeedMin = 40f,
            SpeedMax = 60f,
            SpeedMultiplier = 0.2f,
            Direction = Calc.QuarterCircle,
            DirectionRange = Calc.EighthCircle,

            RotationMode = ParticleType.RotationModes.SameAsDirection,
        };
    }

    public HeartGemShard(HeartGem heart, Vector2 position, int index)
        : base(position)
    {
        Heart = heart;
        heartData = new(typeof(HeartGem), Heart);

        this.index = index;

        Depth = Depths.Pickups;

        Collider = new Hitbox(12f, 12f, -6f, -6f);
        Add(holdableCollider = new HoldableCollider(OnHoldable));
        Add(new PlayerCollider(OnPlayer));

        moveWiggler = Wiggler.Create(0.8f, 2f);
        moveWiggler.StartZero = true;
        Add(moveWiggler);
        Add(collectSfx = new SoundSource());

        Add(shaker = new Shaker(on: false));
        shaker.Interval = 0.1f;

        // index % 3 determines which third of the heart this piece looks like
        Add(sprite = new Image(GFX.Game.GetAtlasSubtexturesAt("collectables/CommunalHelper/heartGemShard/shard", index % 3)).CenterOrigin());
        Add(outline = new Image(GFX.Game.GetAtlasSubtexturesAt("collectables/CommunalHelper/heartGemShard/shard_outline", index % 3)).CenterOrigin());
        Add(scaleWiggler = Wiggler.Create(0.5f, 4f, f => sprite.Scale = Vector2.One * (1f + (f * 0.25f))));

        Add(new BloomPoint(Heart.IsFake ? 0f : 0.75f, 16f));
        Add(new MirrorReflection());
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);

        // Hack to determine sprite color without resorting to a switch statement
        XNAColor color = Color ?? (Heart.IsGhost ? new XNAColor(130, 144, 198) : Heart.Get<VertexLight>().Color);
        sprite.Color = color;

        shineParticle = heartData.Get<ParticleType>("shineParticle");
        if (Color != null)
            shineParticle.Color = Color.Value;

        Add(light = new VertexLight(color, 1f, 32, 64));
        Add(lightTween = light.CreatePulseTween());
    }

    public void Collect(Player player, Level level)
    {
        Collected = true;
        Collidable = false;
        Depth = Depths.NPCs;
        sprite.Color = XNAColor.White;
        shaker.On = true;

        bool allCollected = true;
        foreach (HeartGemShard piece in heartData.Get<List<HeartGemShard>>(HeartGem_HeartGemPieces))
            if (!piece.Collected)
                allCollected = false;

        collectSfx.Play(CustomSFX.game_seedCrystalHeart_shard_collect, "shatter", allCollected ? 0f : 1f);
        Celeste.Freeze(.1f);
        level.Shake(.15f);
        level.Flash(XNAColor.White * .25f);

        if (allCollected)
            Scene.Add(new CSGEN_HeartGemShards(Heart));
    }

    public void OnAllCollected()
    {
        Tag = Tags.FrozenUpdate;
        Depth = Depths.FormationSequences - 2;
        merging = true;
    }

    public void OnPlayer(Player player)
    {
        Level level = Scene as Level;
        if (!Collected && !level.Frozen)
        {
            if (player.DashAttacking)
            {
                Collect(player, level);
                return;
            }

            if (bounceSfxDelay <= 0f)
            {
                if (Heart.IsFake)
                {
                    Audio.Play(SFX.game_10_fakeheart_bounce, Position);
                }
                else
                {
                    Audio.Play(SFX.game_gen_crystalheart_bounce, Position);
                }
                bounceSfxDelay = 0.1f;
            }

            player.PointBounce(Center, 110f);
            scaleWiggler.Start();
            moveWiggler.Start();
            moveWiggleDir = (Center - player.Center).SafeNormalize(Vector2.UnitY);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
        }
    }

    public void OnHoldable(Holdable holdable)
    {
        Player player = Scene.Tracker.GetEntity<Player>();
        if (!Collected && player != null && holdable.Dangerous(holdableCollider))
        {
            Collect(player, Scene as Level);
        }
    }

    public override void Update()
    {
        bounceSfxDelay -= Engine.DeltaTime;
        timer += Engine.DeltaTime;

        sprite.Position = Vector2.UnitY * (!Collected ? (float) Math.Sin(timer * 2f) * 2f : 0);
        sprite.Position += moveWiggleDir * moveWiggler.Value * -4f;
        sprite.Position += shaker.Value;

        // Make sure the outline always matches up with the main sprite
        outline.Position = sprite.Position;
        outline.Scale = sprite.Scale;

        base.Update();

        Level level = SceneAs<Level>();
        if (!Collected)
        {
            if (Scene.OnInterval(0.1f))
                level.Particles.Emit(shineParticle, 1, Center, Vector2.One * 4f);

            if (Scene.OnInterval(3f))
            {
                Audio.Play(SFX.game_gen_seed_pulse, Center, "count", index);
                lightTween.Start();
                level.Displacement.AddBurst(Center + shaker.Value, 0.6f, 8f, 20f, 0.2f);
            }
        }

        if (Collected && !merging && Scene.OnInterval(Calc.Random.Range(0.5f, 0.8f)))
        {
            level.Particles.Emit(P_Burst, 4, Center + shaker.Value, Vector2.One, Calc.Random.NextAngle());
        }
    }

    public void StartSpinAnimation(Vector2 averagePos, Vector2 centerPos, float angleOffset, float time, bool regular)
    {
        shaker.On = false;
        float spinLerp = 0f;
        Vector2 start = Position;
        Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeIn, time / 2f, start: true);
        tween.OnUpdate = t => spinLerp = t.Eased;
        Add(tween);

        tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeInOut, time, start: true);
        tween.OnUpdate = t =>
        {
            float angle = Calc.QuarterCircle + angleOffset - MathHelper.Lerp(0f, (Calc.Circle * 5) + Calc.EighthCircle, t.Eased);
            Vector2 value = Vector2.Lerp(averagePos, centerPos, spinLerp) + Calc.AngleToVector(angle, regular ? 30f : MathHelper.Lerp(30f, 5f, t.Eased));
            Position = Vector2.Lerp(start, value, spinLerp);
        };
        Add(tween);
    }

    public void StartCombineAnimation(Vector2 centerPos, float time, ParticleSystem particleSystem, Level level, bool spin)
    {
        collectSfx.Stop(allowFadeout: false);
        float startAngle = Calc.Angle(centerPos, Position);
        Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.BigBackIn, time, start: true);
        tween.OnUpdate = t =>
        {
            Vector2 oldPos = Center;
            float angle = spin ? MathHelper.Lerp(startAngle, startAngle - Calc.Circle, Ease.CubeIn(t.Percent)) : startAngle;
            float length = MathHelper.Lerp(spin ? 30f : 5f + (45f * t.Percent), 0f, t.Eased);
            Position = centerPos + Calc.AngleToVector(angle, length);

            if (level.OnInterval(.03f))
                particleSystem.Emit(StrawberrySeed.P_Burst, 1, Center, Vector2.One, (Center - oldPos).Angle());

            if (t.Percent > 0.5f)
            {
                level.Shake((t.Percent - .5f) * .5f);
            }
        };
        tween.OnComplete = delegate
        {
            Visible = false;
            for (int i = 0; i < 6; i++)
            {
                float angle = Calc.Random.NextFloat(Calc.Circle);
                particleSystem.Emit(StrawberrySeed.P_Burst, 1, Position + Calc.AngleToVector(angle, 4f), Vector2.Zero, angle);
            }
            RemoveSelf();
        };
        Add(tween);
    }

    #region HeartGem Extensions

    protected static string GotShardFlag(DynamicData heartData)
    {
        return "collected_shards_of_" + heartData.Get(HeartGem_HeartGemID).ToString();
    }

    public static void CollectedPieces(DynamicData heartData)
    {
        HeartGem heart = heartData.Target as HeartGem;

        heart.Visible = true;
        heart.Active = true;
        heart.Collidable = true;
        heartData.Get<BloomPoint>("bloom").Visible = heartData.Get<VertexLight>("light").Visible = true;
        heart.SceneAs<Level>().Session.SetFlag(GotShardFlag(heartData));
    }

    #endregion

    #region Hooks

    internal static void Load()
    {
        On.Celeste.HeartGem.ctor_EntityData_Vector2 += HeartGem_ctor_EntityData_Vector2;
        On.Celeste.HeartGem.Awake += HeartGem_Awake;
    }

    internal static void Unload()
    {
        On.Celeste.HeartGem.ctor_EntityData_Vector2 -= HeartGem_ctor_EntityData_Vector2;
        On.Celeste.HeartGem.Awake -= HeartGem_Awake;
    }

    private static void HeartGem_ctor_EntityData_Vector2(On.Celeste.HeartGem.orig_ctor_EntityData_Vector2 orig, HeartGem self, EntityData data, Vector2 offset)
    {
        orig(self, data, offset);

        // If it hasn't been loaded by our mod, don't even bother with it
        if (data.Has(HeartGem_HeartGemID))
        {

            DynamicData heartData = DynamicData.For(self);
            if (data.Nodes != null && data.Nodes.Length != 0)
            {
                List<HeartGemShard> pieces = new();
                for (int i = 0; i < data.Nodes.Length; i++)
                {
                    HeartGemShard shard = new(self, offset + data.Nodes[i], i);
                    // Just blindly use any color attribute, if available
                    if (data.Has("color"))
                        shard.Color = Calc.HexToColor(data.Attr("color", "00a81f"));
                    pieces.Add(shard);
                }
                heartData.Set(HeartGem_HeartGemPieces, pieces);

            }
            else
                heartData.Set(HeartGem_HeartGemPieces, null);
            heartData.Set(HeartGem_HeartGemID, data.Values[HeartGem_HeartGemID]);


        }
    }

    private static void HeartGem_Awake(On.Celeste.HeartGem.orig_Awake orig, HeartGem self, Scene scene)
    {
        orig(self, scene);

        DynamicData heartData = DynamicData.For(self);
        if (heartData.Data.TryGetValue(HeartGem_HeartGemPieces, out object result))
        {
            if (result is List<HeartGemShard> pieces && pieces.Count > 0 && !(scene as Level).Session.GetFlag(GotShardFlag(heartData)))
            {
                foreach (HeartGemShard piece in pieces)
                {
                    scene.Add(piece);
                }
                self.Visible = false;
                self.Active = false;
                self.Collidable = false;
                heartData.Get<BloomPoint>("bloom").Visible = heartData.Get<VertexLight>("light").Visible = false;
            }
        }
    }

    #endregion

}
