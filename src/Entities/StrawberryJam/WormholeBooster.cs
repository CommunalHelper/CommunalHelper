using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using System.Collections;

namespace Celeste.Mod.CommunalHelper.Entities.StrawberryJam;

[Tracked]
[CustomEntity("CommunalHelper/SJ/WormholeBooster")]
class WormholeBooster : Booster
{
    public static readonly Color DisplaceColor = Calc.HexToColor("827E00");
    public static ParticleType P_Teleporting { get; private set; }
    public static ParticleType P_WBurst { get; private set; }
    public static ParticleType P_WAppear { get; private set; }
    
    public static bool TeleDeath;
    public static bool CanTeleport;

    public string DeathColor;
    public bool InstantCamera;
    
    private Sprite displacementMask;
    private MTexture displace;
    private Color color;
    private float displaceEase = 1;

    private Sprite _sprite
    {
        get => DynamicData.For(this).Get<Sprite>("sprite");
        set => DynamicData.For(this).Set("sprite", value);
    }
    
    private float _respawnTimer
    {
        get => DynamicData.For(this).Get<float>("respawnTimer");
        set => DynamicData.For(this).Set("respawnTimer", value);
    }

    public WormholeBooster(EntityData data, Vector2 offset)
        : base(data.Position + offset, false)
    {
        DeathColor = data.Attr("deathColor", "61010c");
        InstantCamera = data.Bool("instantCamera", false);

        color = Calc.HexToColor("7800bd");

        displace = GFX.Game["util/CommunalHelper/strawberryJam/wormhole_disp"];
        Add(new DisplacementRenderHook(BlackHoleDisplacement));

        Add(displacementMask = CommunalHelperGFX.SpriteBank.Create("wormholeMask"));
        displacementMask.Stop();
        displacementMask.Visible = false;

        _sprite = CommunalHelperGFX.SpriteBank.CreateOn(_sprite, "wormholeBooster");
        _sprite.Color = color;
        DynamicData.For(this).Set("particleType", P_WBurst);

        TeleDeath = false;
        CanTeleport = true;
    }

    internal static void InitializeParticles()
    {
        P_Teleporting = new ParticleType
        {
            Source = GFX.Game["particles/blob"],
            Color = Calc.HexToColor("8100C1") * 0.2f,
            Color2 = Calc.HexToColor("7800bd") * 0.2f,
            ColorMode = ParticleType.ColorModes.Choose,
            RotationMode = ParticleType.RotationModes.SameAsDirection,
            Size = 0.7f,
            SizeRange = 0.2f,
            DirectionRange = (float) Math.PI / 12f,
            FadeMode = ParticleType.FadeModes.Late,
            LifeMax = 0.2f,
            SpeedMin = 70f,
            SpeedMax = 100f,
            SpeedMultiplier = 1f,
            Acceleration = new Vector2(0f, 10f)
        };
        P_WBurst = new ParticleType(P_Burst) { Color = Calc.HexToColor("7800bd") };
        P_WAppear = new ParticleType(P_Appear) { Color = Calc.HexToColor("8100C1") };
    }

    public static void Load()
    {
        On.Celeste.Booster.AppearParticles += WormholeAppearParticles;
        On.Celeste.Booster.OnPlayer += BoosterOnPlayerHook;
        On.Celeste.Booster.PlayerReleased += BoosterPlayerReleasedHook;
        IL.Celeste.Booster.Render += BoosterRenderHook;
        On.Celeste.Player.BoostCoroutine += PlayerBoostCoroutineHook;
    }

    public static void Unload()
    {
        On.Celeste.Booster.AppearParticles -= WormholeAppearParticles;
        On.Celeste.Booster.OnPlayer -= BoosterOnPlayerHook;
        On.Celeste.Booster.PlayerReleased -= BoosterPlayerReleasedHook;
        IL.Celeste.Booster.Render -= BoosterRenderHook;
        On.Celeste.Player.BoostCoroutine -= PlayerBoostCoroutineHook;
    }

    private static void WormholeAppearParticles(On.Celeste.Booster.orig_AppearParticles orig, Booster self)
    {
        if (self is WormholeBooster)
        {
            ParticleSystem particlesBG = self.SceneAs<Level>().ParticlesBG;
            for (int i = 0; i < 360; i += 30)
            {
                particlesBG.Emit(P_WAppear, 1, self.Center, Vector2.One * 2f, i * ((float) Math.PI / 180f));
            }
        }
        else
        {
            orig(self);
        }
    }

    private static void BoosterRenderHook(ILContext il)
    {
        ILCursor cursor = new(il);
        if (cursor.TryGotoNext(MoveType.Before, instr => instr.OpCode == OpCodes.Brfalse_S))
        {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<bool, Booster, bool>>((orig, self) => self is not WormholeBooster && orig);
        }
    }

    private static void BoosterOnPlayerHook(On.Celeste.Booster.orig_OnPlayer orig, Booster self, Player player)
    {
        if (self is WormholeBooster booster)
        {
            if (CanTeleport && booster._respawnTimer <= 0f && DynamicData.For(self).Get<float>("cannotUseTimer") <= 0f && !booster.BoostingPlayer)
            {
                booster.Add(new Coroutine(TeleDeath ? booster.KillCoroutine(player) : booster.TeleportCoroutine(player)));
            }

            return;
        }

        orig(self, player);
    }

    private static void BoosterPlayerReleasedHook(On.Celeste.Booster.orig_PlayerReleased orig, Booster self)
    {
        orig(self);
        if (self is WormholeBooster)
        {
            CanTeleport = true;
        }
    }

    private static IEnumerator PlayerBoostCoroutineHook(On.Celeste.Player.orig_BoostCoroutine orig, Player self)
    {
        if (self.CurrentBooster is WormholeBooster)
        {
            yield return 0.45f;
            self.StateMachine.State = Player.StDash;
        }
        else
        {
            IEnumerator original = orig(self);
            while (original.MoveNext())
            {
                yield return original.Current;
            }
        }
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);
        DynamicData.For(this).Get<Entity>("outline").RemoveSelf();
    }

    public override void Update()
    {
        base.Update();

        _sprite.Color = color;

        if (_respawnTimer > 0.1f)
        {
            _respawnTimer = 0.1f;
        }

        TeleDeath = Scene.Tracker.CountEntities<WormholeBooster>() == 1;
        if (TeleDeath)
        {
            color = Color.Lerp(color, Calc.HexToColor(DeathColor), 3f * Engine.DeltaTime);
        }
    }

    public IEnumerator TeleportCoroutine(Player player)
    {
        WormholeBooster nearest = FindNearestBooster();
        if (nearest == null)
        {
            yield break;
        }

        CanTeleport = false;

        Level level = SceneAs<Level>();
        level.Add(new WBTrailManager(Position, nearest.Position));
        Audio.Play("event:/char/badeline/disappear", nearest.Position);
        _sprite.Visible = false;
        Collidable = false;

        Vector2 target = (nearest.Center - player.Collider.Center).Floor();
        player.CameraAnchorLerp = Vector2.Zero;
        Vector2 cameraTo = level.GetFullCameraTargetAt(player, target);
        player.Position = target;
        player.Boost(nearest);

        if (InstantCamera)
        {
            level.Camera.Position = cameraTo;
        }
        else
        {
            Tag |= Tags.FrozenUpdate;
            level.Frozen = true;
            float distance = Vector2.Distance(level.Camera.Position, cameraTo);
            float catchup = Calc.Clamp(distance / 5f, 8f, 600f);
            while (!level.InsideCamera(target, player.Height * 2))
            {
                Vector2 current = level.Camera.Position;
                level.Camera.Position += (cameraTo - current) * (1f - (float) Math.Pow((double) (0.01f / catchup), (double) Engine.DeltaTime));
                yield return null;
            }

            level.Frozen = false;
        }

        RemoveSelf();
    }

    public IEnumerator KillCoroutine(Player player)
    {
        player.StateMachine.State = Player.StDummy;
        player.DummyGravity = false;
        player.Collidable = false;
        DynamicData.For(player).Set("varJumpTimer", 0f);
        player.Speed = Vector2.Zero;
        Collidable = false;
        Vector2 target = Center - player.Collider.Center;
        for (float p = 0; p < 1f; p += Engine.DeltaTime / 0.75f)
        {
            _sprite.Scale = Vector2.Lerp(Vector2.One, Vector2.Zero, Ease.CubeIn(p));
            if (player.Position != target)
            {
                player.Position = Calc.Approach(player.Position, target, 80f * Engine.DeltaTime);
            }
            else
            {
                player.Visible = false;
            }

            if (Input.DashPressed)
            {
                _sprite.Scale = Vector2.Zero;
                break;
            }

            yield return null;
        }

        Audio.Play("event:/char/badeline/disappear", player.Position);
        player.Die(Vector2.Zero);
        player.StateMachine.State = Player.StNormal;
        player.Visible = true;
        player.Collidable = true;
        RemoveSelf();
    }

    private void BlackHoleDisplacement()
    {
        if (!_sprite.Visible)
        {
            displaceEase -= 8 * Engine.RawDeltaTime;
        }

        displaceEase = Calc.Clamp(displaceEase, 0, 1);
        displace.Draw(Position + _sprite.Position, displace.Center, Color.White * displaceEase, 0.2f);
        displacementMask.GetFrame(_sprite.CurrentAnimationID, _sprite.CurrentAnimationFrame).Draw(Position + _sprite.Position - new Vector2(16), Vector2.Zero, DisplaceColor * displaceEase);
    }

    private WormholeBooster FindNearestBooster()
    {
        WormholeBooster closest = null;
        float shortestDistance = float.MaxValue;
        foreach (WormholeBooster booster in Scene.Tracker.GetEntities<WormholeBooster>())
        {
            if (this == booster)
            {
                continue;
            }

            float currDistance = (booster.Position - Position).LengthSquared();
            if (currDistance < shortestDistance)
            {
                closest = booster;
                shortestDistance = currDistance;
            }
        }

        return closest;
    }

    private class WBTrailManager : Entity
    {
        private Tween tween;

        public WBTrailManager(Vector2 from, Vector2 to) : base(from)
        {
            Tag = Tags.FrozenUpdate;
            Add(tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.Linear, 0.2f));
            tween.OnUpdate = t =>
            {
                Vector2 where = Vector2.Lerp(Position, to, Calc.Max(t.Eased - 0.07f, 0));
                SceneAs<Level>().Particles.Emit(P_Teleporting, 8, where, Vector2.Zero, (to - Position).Angle());
            };
            tween.OnComplete = _ => RemoveSelf();
        }

        public override void Awake(Scene scene)
        {
            tween.Start();
        }
    }
}
