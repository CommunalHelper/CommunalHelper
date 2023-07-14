
using Celeste.Mod.CommunalHelper.Triggers;
using System.Collections;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/RedlessBerry")]
[RegisterStrawberry(tracked: false, blocksCollection: false)]
public class RedlessBerry : Entity, IStrawberry
{
    private static readonly Color PulseColorA = Calc.HexToColor("FDBF47");
    private static readonly Color PulseColorB = Calc.HexToColor("FE9130");
    private static readonly Color PulseColorGhostA = Calc.HexToColor("5952A1");
    private static readonly Color PulseColorGhostB = Calc.HexToColor("6569B4");
    private static readonly Color BrokenColorA = Calc.HexToColor("E4242D");
    private static readonly Color BrokenColorB = Calc.HexToColor("252B42");
    private static readonly Color WarnColor = Color.Red;

    public struct Info
    {
        public EntityID ID { get; set; }
        public Vector2 Start { get; set; }

        public Info(EntityID id, Vector2 startPosition)
        {
            ID = id;
            Start = startPosition;
        }

        public override int GetHashCode()
        {
            int hashCode = 1396480991;
            hashCode = (hashCode * -1521134295) + ID.GetHashCode();
            hashCode = (hashCode * -1521134295) + Start.GetHashCode();
            return hashCode;
        }
    }
    private readonly Info info;

    public EntityID ID => info.ID;

    internal bool Given; // given by command

    private readonly bool persistent, persisted;
    private readonly bool winged;
    private bool flyingAway;
    private float flapSpeed;

    private bool ghost;

    private float collectTimer;

    private float safeLerp = 1f, safeLerpTarget = 1f;
    private float brokenLerp = 0f;
    private float shaking;
    private Vector2 offset;

    private bool broken;
    private bool collected;

    private readonly Follower follower;

    private Sprite fruit, overlay;
    private Wiggler wiggler, rotateWiggler, shakeWiggler;
    private BloomPoint bloom;
    private VertexLight light;
    private Tween lightTween;

    private readonly SoundSource sfx = new(CustomSFX.game_berries_redless_warning);
    private readonly SoundSource breakSfx = new();

    public RedlessBerry(EntityData data, Vector2 offset, EntityID id)
        : this(data.Position + offset, id, data.Bool("persistent"), data.Bool("winged"))
    {
        info = new(id, Position);
    }

    public RedlessBerry(Vector2 position, EntityID id, bool persistent = false, bool winged = false)
        : base(position)
    {
        Depth = Depths.Pickups;
        Collider = new Hitbox(14f, 14f, -7f, -7f);

        this.persistent = persistent && !winged;
        this.winged = winged;

        Add(new PlayerCollider(OnPlayer));
        Add(follower = new(id, null, OnLoseLeader)
        {
            FollowDelay = .3f
        });

        Add(sfx, breakSfx);
    }

    public RedlessBerry(Player player, Info info)
        : this(player.Position + new Vector2(-12 * (int) player.Facing, -8f), info.ID, true, false)
    {
        persisted = true;
        this.info = info;
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);

        ghost = SaveData.Instance.CheckStrawberry(info.ID);

        fruit = CommunalHelperGFX.SpriteBank.Create("recolorableStrawberryFruit");
        overlay = CommunalHelperGFX.SpriteBank.Create("recolorableStrawberryOverlay");
        Add(fruit, overlay);

        if (winged)
        {
            fruit.Play("flap");
            overlay.Play("flap");
        }

        fruit.OnFrameChange = OnAnimate;

        Add(wiggler = Wiggler.Create(.4f, 4f, v =>
        {
            fruit.Scale = overlay.Scale = Vector2.One * (1f + (v * 0.35f));
        }));

        Add(rotateWiggler = Wiggler.Create(.5f, 4f, v =>
        {
            fruit.Rotation = overlay.Rotation = v * 30f * (MathHelper.Pi / 180f);
        }));

        Add(shakeWiggler = Wiggler.Create(.8f, 2f, v =>
        {
            fruit.Position.Y = overlay.Position.Y = v * 2f;
        }));

        float bloomAlpha = ghost ? .5f : 1f;
        Add(bloom = new BloomPoint(bloomAlpha, 12f));
        if ((scene as Level).Session.BloomBaseAdd > .1f)
            bloom.Alpha *= 0.5f;

        Add(light = new VertexLight(Color.White, 1f, 16, 24));
        Add(lightTween = light.CreatePulseTween());

        UpdateColor();

        if (persisted && Util.TryGetPlayer(out Player player))
            OnPlayer(player);

        if (winged && CommunalHelperModule.Session.PlayerWasTired)
            RemoveSelf();
    }

    private void OnAnimate(string id)
    {
        if (!flyingAway && id == "flap" && fruit.CurrentAnimationFrame % 9 == 4)
        {
            Audio.Play("event:/game/general/strawberry_wingflap", Position);
            flapSpeed = -50f;
        }

        int pulseFrame = winged ? 25 : 35;
        if (fruit.CurrentAnimationFrame == pulseFrame)
        {
            lightTween.Start();
            shakeWiggler.Start();
            Audio.Play(SFX.game_gen_strawberry_pulse, Position);
            (Scene as Level).Displacement.AddBurst(Position, .6f, 4f, 28f, .1f);
        }
    }

    private void OnPlayer(Player player)
    {
        if (follower.Leader is not null || collected)
            return;

        Collidable = false;
        player.Leader.GainFollower(follower);
        Depth = Depths.Top;

        if (winged)
        {
            fruit.Play("idle");
            overlay.Play("idle");
        }

        if (!persisted)
        {
            Audio.Play(SFX.game_gen_strawberry_touch, Position);
            wiggler.Start();
        }

        if (persistent && !persisted)
        {
            Session session = SceneAs<Level>().Session;
            session.DoNotLoad.Add(info.ID);
            session.UpdateLevelStartDashes();
            CommunalHelperModule.Session.RedlessBerries.Add(info);
        }
    }

    private void Reset()
    {
        Depth = Depths.Pickups;
        fruit.Play("idle", restart: true);
        overlay.Play("idle", restart: true);
        broken = false;
        Collidable = true;
        sfx.Play(CustomSFX.game_berries_redless_warning);
    }

    private void OnLoseLeader()
    {
        if (collected || (persistent && Util.TryGetPlayer(out Player player) && player.Dead))
            return;
        Detach();
    }

    private void Detach()
    {
        if (collected)
            return;

        if (persistent)
        {
            Session session = SceneAs<Level>().Session;
            session.DoNotLoad.Remove(info.ID);
            CommunalHelperModule.Session.RedlessBerries.Remove(info);
        }

        if (!winged)
        {
            fruit.Play("idleBroken", restart: true);
            fruit.SetAnimationFrame(35);
            overlay.Play("idle", restart: true);
            overlay.SetAnimationFrame(35);
        }

        breakSfx.Play(CustomSFX.game_berries_redless_break);
        sfx.Stop();
        (Scene as Level).Displacement.AddBurst(Position, .6f, 4f, 28f, .1f);

        shakeWiggler.Start();
        rotateWiggler.Start();

        safeLerpTarget = brokenLerp = 1f;
        shaking = 1f;
        broken = true;

        if (winged || Given)
            return;

        Vector2 start = info.Start;
        Alarm.Set(this, .45f, () =>
        {
            Vector2 difference = (start - Position).SafeNormalize();
            float distance = Vector2.Distance(Position, start);
            float scaleFactor = Calc.ClampedMap(distance, 16f, 120f, 16f, 96f);

            Vector2 control = start + (difference * 16f) + (difference.Perpendicular() * scaleFactor * Calc.Random.Choose(1, -1));
            SimpleCurve curve = new(Position, start, control);

            Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.SineOut, MathHelper.Max(distance / 100f, .4f), start: true);
            tween.OnUpdate = tween =>
            {
                Position = curve.GetPoint(tween.Eased);
            };
            tween.OnComplete = _ => Reset();
            Add(tween);
        });
    }

    private void FlyAway()
    {
        Collidable = false;
        Depth = Depths.Top;
        Add(new Coroutine(FlyAwayRoutine()));
        flyingAway = true;
    }

    public void OnCollect()
    {
        if (collected)
            return;
        collected = true;

        int collectIndex = 0;
        collected = true;
        if (follower.Leader is not null)
        {
            Player obj = follower.Leader.Entity as Player;
            collectIndex = obj.StrawberryCollectIndex++;
            obj.StrawberryCollectResetTimer = 2.5f;
            follower.Leader.LoseFollower(follower);
        }

        SaveData.Instance.AddStrawberry(info.ID, false);
        CommunalHelperModule.Session.RedlessBerries.Remove(info);

        Session session = (Scene as Level).Session;
        session.DoNotLoad.Add(info.ID);
        session.Strawberries.Add(info.ID);
        session.UpdateLevelStartDashes();
        Add(new Coroutine(CollectRoutine(collectIndex)));
    }

    private void UpdateColor()
    {
        Color pulseA = ghost ? PulseColorGhostA : PulseColorA;
        Color pulseB = ghost ? PulseColorGhostB : PulseColorB;

        Color safeColor = Color.Lerp(pulseA, pulseB, Calc.SineMap(Scene.TimeActive * 16f, 0f, 1f));
        Color warnColor = Color.Lerp(pulseA, WarnColor, Calc.SineMap(Scene.TimeActive * 60f, 0f, 1f));
        Color result = Color.Lerp(warnColor, safeColor, Ease.CubeOut(safeLerp));

        if (brokenLerp > 0f)
        {
            Color brokenColor = Color.Lerp(BrokenColorA, BrokenColorB, Ease.CubeOut(Calc.SineMap(Scene.TimeActive * 12f, 0f, 1f)));
            result = Color.Lerp(result, brokenColor, brokenLerp);
        }

        fruit.Color = result;
    }

    public override void Update()
    {
        base.Update();

        if (!collected)
        {
            if (follower.Leader?.Entity is Player player)
            {
                safeLerpTarget = Calc.ClampedMap(player.Stamina, 20f, Player.ClimbMaxStamina);
                if (player.Stamina < Player.ClimbTiredThreshold)
                {
                    follower.Leader.LoseFollower(follower);
                    if (winged || Given)
                        FlyAway(); // OnLoseFollower takes care of detaching, so just fly away.
                }

                if (follower.DelayTimer <= 0f && StrawberryRegistry.IsFirstStrawberry(this))
                {
                    bool collecting = !player.StrawberriesBlocked && player.StateMachine.State != Player.StIntroJump;
                    collecting &= (winged && player.OnSafeGround) || player.CollideCheck<RedlessBerryCollectionTrigger>() || (Scene as Level).Completed;

                    if (collecting)
                    {
                        collectTimer += Engine.DeltaTime;
                        if (collectTimer > 0.15f)
                            OnCollect();
                    }
                    else
                        collectTimer = Math.Min(collectTimer, 0f);
                }
            }
            else
            {
                if (winged || Given)
                {
                    Y += flapSpeed * Engine.DeltaTime;
                    if (flyingAway)
                    {
                        if (Y < SceneAs<Level>().Bounds.Top - 16)
                            RemoveSelf();
                    }
                    else if (!Given)
                    {
                        flapSpeed = Calc.Approach(flapSpeed, 20f, 170f * Engine.DeltaTime);
                        if (Y < info.Start.Y - 5f)
                            Y = info.Start.Y - 5f;
                        else if (Y > info.Start.Y + 5f)
                            Y = info.Start.Y + 5f;

                        bool hasPlayer = Util.TryGetPlayer(out player);
                        if (hasPlayer)
                        {
                            safeLerpTarget = Calc.ClampedMap(player.Stamina, 20f, Player.ClimbMaxStamina);
                            if (player.Stamina < Player.ClimbTiredThreshold)
                            {
                                Detach();
                                FlyAway();
                            }
                        }
                    }
                }
            }
        }

        if (!broken)
            sfx.Param("intensity", 1 - safeLerp);

        safeLerp = Calc.Approach(safeLerp, safeLerpTarget, Engine.DeltaTime * 2f);
        brokenLerp = Calc.Approach(brokenLerp, broken ? 1f : 0f, Engine.DeltaTime * 2f);
        shaking = Calc.Approach(shaking, 0f, Engine.DeltaTime * 3.125f);
        offset = shaking > 0f ? Calc.Random.ShakeVector() : Vector2.Zero;

        UpdateColor();
    }

    public override void Render()
    {
        Vector2 original = Position;
        Position += offset;
        base.Render();
        Position = original;
    }

    private IEnumerator FlyAwayRoutine()
    {
        rotateWiggler.Start();
        flapSpeed = -200f;

        Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeOut, 0.25f, start: true);
        tween.OnUpdate = t => flapSpeed = MathHelper.Lerp(-200f, 0f, t.Eased);
        Add(tween);
        yield return 0.1f;

        Audio.Play(SFX.game_gen_strawberry_laugh, Position);
        yield return 0.2f;

        if (!follower.HasLeader)
            Audio.Play(SFX.game_gen_strawberry_flyaway, Position);

        tween = Tween.Create(Tween.TweenMode.Oneshot, null, 0.5f, start: true);
        tween.OnUpdate = t => flapSpeed = MathHelper.Lerp(0f, -200f, t.Eased);
        Add(tween);
    }

    private IEnumerator CollectRoutine(int collectIndex)
    {
        Tag = Tags.TransitionUpdate;
        Depth = Depths.FormationSequences - 10;
        Audio.Play(SFX.game_gen_strawberry_get, Position, "colour", 0, "count", collectIndex);
        Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
        fruit.Play("hidden");
        overlay.Play("collect");
        while (overlay.Animating)
            yield return null;

        Scene.Add(new StrawberryPoints(Position, false, collectIndex, false));
        RemoveSelf();
    }

    #region Hooks

    internal static void Hook()
    {
        On.Celeste.Level.LoadLevel += Mod_Level_LoadLevel;
        On.Celeste.Player.Update += Player_Update;
    }

    internal static void Unhook()
    {
        On.Celeste.Level.LoadLevel -= Mod_Level_LoadLevel;
        On.Celeste.Player.Update -= Player_Update;
    }

    private static void Mod_Level_LoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader)
    {
        orig(self, playerIntro, isFromLoader);

        if (playerIntro != Player.IntroTypes.Transition)
        {
            Player player = self.Tracker.GetEntity<Player>();
            foreach (Info info in CommunalHelperModule.Session.RedlessBerries)
            {
                self.Add(new RedlessBerry(player, info));
            }
        }
    }

    private static void Player_Update(On.Celeste.Player.orig_Update orig, Player self)
    {
        orig(self);

        CommunalHelperSession session = CommunalHelperModule.Session;
        if (self.Stamina < Player.ClimbTiredThreshold && !session.PlayerWasTired)
            session.PlayerWasTired = true;
    }

    #endregion
}
