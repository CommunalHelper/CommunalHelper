using Celeste.Mod.CommunalHelper.Components;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Utils;

namespace Celeste.Mod.CommunalHelper.Entities.StrawberryJam;

[CustomEntity("CommunalHelper/SJ/PelletEmitter")]
public class PelletEmitter : Entity
{
    #region Entity Properties

    public bool CollideWithSolids { get; }
    public int Count { get; }
    public float Delay { get; }
    public float Speed { get; }
    public int CassetteIndex { get; }
    public float WiggleFrequency { get; }
    public float WiggleAmount { get; }
    public bool WiggleHitbox { get; }
    public bool KillPlayer { get; }
    public LaserOrientations Orientation { get; }
        
    #endregion

    public static ParticleType P_BlueTrail;
    public static ParticleType P_PinkTrail;
    
    public static void LoadParticles() {
        P_BlueTrail ??= new ParticleType {
            Source = GFX.Game["particles/blob"],
            Color = Paintbrush.ColorFromCassetteIndex(0),
            Color2 = Calc.HexToColor("7550e8"),
            ColorMode = ParticleType.ColorModes.Fade,
            FadeMode = ParticleType.FadeModes.Late,
            Size = 0.7f,
            SizeRange = 0.25f,
            ScaleOut = true,
            LifeMin = 0.2f,
            LifeMax = 0.4f,
            SpeedMin = 5f,
            SpeedMax = 25f,
            DirectionRange = 0.5f,
        };

        P_PinkTrail ??= new ParticleType(P_BlueTrail) {
            Color = Paintbrush.ColorFromCassetteIndex(1),
        };
    }
    
    public Vector2 Direction { get; }
    public Vector2 Origin { get; }

    public readonly Sprite EmitterSprite;

    public string AnimationKeyPrefix => $"{CassetteIndex switch {0 => "blue", 1 => "pink", _ => "both"}}";

    private string idleAnimationKey => $"{AnimationKeyPrefix}_idle";
    private string chargingAnimationKey => $"{AnimationKeyPrefix}_charging";
    private string firingAnimationKey => $"{AnimationKeyPrefix}_firing";

    private string fireSound(int index) => index == 0 ? CustomSFX.paint_emitter_blue : CustomSFX.paint_emitter_pink;

    private SingletonAudioController sfx;
    private Vector2 shakeOffset;

    public PelletEmitter(EntityData data, Vector2 offset) : base(data.Position + offset) {
        LoadParticles();
        
        const float shotOriginOffset = 12f;
        CassetteIndex = data.Int("cassetteIndex");
        CollideWithSolids = data.Bool("collideWithSolids", true);
        Count = data.Int("pelletCount", 1);
        Delay = data.Float("pelletDelay", 0.25f);
        Speed = data.Float("pelletSpeed", 100f);
        WiggleFrequency = data.Float("wiggleFrequency", 2f);
        WiggleAmount = data.Float("wiggleAmount", 2f);
        WiggleHitbox = data.Bool("wiggleHitbox", false);
        Orientation = data.Enum("orientation", LaserOrientations.Up);
        KillPlayer = data.Bool("killPlayer", true);
        
        Direction = Orientation.Direction();
        Origin = Orientation.Direction() * shotOriginOffset;
        Collider = new Circle(6, Direction.X * 2, Direction.Y * 2);

        EmitterSprite = CommunalHelperGFX.SpriteBank.Create("pelletEmitter");
        EmitterSprite.Rotation = Orientation.Angle() - (float)Math.PI / 2f;
        EmitterSprite.Effects = Orientation is LaserOrientations.Left or LaserOrientations.Down
            ? SpriteEffects.FlipVertically
            : SpriteEffects.None;

        EmitterSprite.Play(idleAnimationKey);

        Add(new StaticMover
        {
            JumpThruChecker = CollideCheck,
            SolidChecker = CollideCheck,
            OnAttach = p => Depth = p.Depth - 1,
            OnEnable = () =>
            {
                Collidable = Visible = Active = true;
                EmitterSprite.Play(idleAnimationKey);
            },
            OnDisable = () => Collidable = Visible = Active = false,
            OnShake = v => shakeOffset += v,
        });
        
        Add(EmitterSprite,
            new LedgeBlocker(),
            new PlayerCollider(OnPlayerCollide),
            new TickingCassetteListener(CassetteIndex) {
                OnTick = (cbm, isSwap) =>
                {
                    if (!isSwap && (CassetteIndex < 0 || CassetteIndex != cbm.currentIndex)) {
                        string key = chargingAnimationKey;
                        var animation = EmitterSprite.Animations[key];
                        animation.Delay = getTickLength() / animation.Frames.Length;
                        EmitterSprite.Play(key);
                    }
                    else if (isSwap && (CassetteIndex < 0 || CassetteIndex == cbm.currentIndex)) {
                        EmitterSprite.Play(firingAnimationKey);
                        Fire(cbm.currentIndex);
                    }
                }
           });
    }

    public override void Added(Scene scene) {
        base.Added(scene);
            
        sfx = SingletonAudioController.Ensure(scene);
    }

    private float getTickLength() {
        var cbm = Scene.Tracker.GetEntity<CassetteBlockManager>();
        var beatLength = (10 / 60f) / cbm.tempoMult;
        return beatLength * DynamicData.For(cbm).Get<int>("beatsPerTick");
    }

    public void Fire(int? cassetteIndex = null, Action<PelletShot> action = null) {
        for (int i = 0; i < Count; i++) {
            cassetteIndex ??= CassetteIndex;
            var shot = Engine.Pooler.Create<PelletShot>().Init(this, i * Delay, cassetteIndex.Value);
            action?.Invoke(shot);
            Scene.Add(shot);
            if (i == 0) {
                PlayFireSound(cassetteIndex.Value);
            }
        }
    }

    public void PlayFireSound(int index) {
        sfx?.Play(fireSound(index), this, 0.01f);
    }

    private void OnPlayerCollide(Player player)
    {
        if (!KillPlayer) return;
        player.Die((player.Center - Position).SafeNormalize());
    }

    public override void Render()
    {
        Position += shakeOffset;
        base.Render();
        Position -= shakeOffset;
    }

    [Pooled]
    [Tracked]
    public class PelletShot : Entity {
        public bool Dead { get; set; }
        public Vector2 Speed { get; set; }
        public bool CollideWithSolids { get; set; }

        private readonly Sprite projectileSprite;
        private readonly Sprite impactSprite;

        private float delayTimeRemaining;

        private string projectileAnimationKey;
        private string impactAnimationKey;

        private readonly Hitbox killHitbox = new Hitbox(8, 8);
        private readonly SineWave travelSineWave;
        private readonly Wiggler hitWiggler;

        private ParticleType particleType;
        private Vector2 hitDir;
        private int firedCassetteIndex;

        private float wiggleAmount = 2f;

        private PelletEmitter parentEmitter;

        public PelletShot() : base(Vector2.Zero) {
            Depth = Depths.Above;
            Add(projectileSprite = CommunalHelperGFX.SpriteBank.Create("pelletProjectile"),
                impactSprite = CommunalHelperGFX.SpriteBank.Create("pelletImpact"),
                new PlayerCollider(OnPlayerCollide),
                travelSineWave = new SineWave(2f),
                hitWiggler = Wiggler.Create(1.2f, 2f));

            hitWiggler.StartZero = true;
        }

        public PelletShot Init(PelletEmitter emitter, float delay, int cassetteIndex) {
            parentEmitter = emitter;
            delayTimeRemaining = delay;
            Dead = false;
            Speed = emitter.Direction * emitter.Speed;
            Position = emitter.Position + emitter.Origin;
            CollideWithSolids = emitter.CollideWithSolids;
            Collider = killHitbox;
            Collidable = true;

            particleType = cassetteIndex == 0 ? P_BlueTrail : P_PinkTrail;

            impactSprite.Rotation = projectileSprite.Rotation = emitter.EmitterSprite.Rotation;
            impactSprite.Effects = projectileSprite.Effects = emitter.EmitterSprite.Effects;

            firedCassetteIndex = cassetteIndex;
            projectileAnimationKey = impactAnimationKey = cassetteIndex == 0 ? "blue" : "pink";

            impactSprite.Visible = false;
            impactSprite.Stop();

            projectileSprite.Visible = delay == 0;
            projectileSprite.Stop();

            travelSineWave.Frequency = emitter.WiggleFrequency;
            travelSineWave.Active = true;
            travelSineWave.Reset();
            wiggleAmount = emitter.WiggleAmount;

            hitWiggler.StopAndClear();
            hitDir = Vector2.Zero;

            return this;
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            if (delayTimeRemaining <= 0) {
                projectileSprite.Play(projectileAnimationKey, randomizeFrame: true);
            }
        }

        public void Destroy(float delay = 0) {
            parentEmitter = null;
            projectileSprite.Stop();
            impactSprite.Stop();
            Dead = true;
            RemoveSelf();
        }

        public override void Update() {
            base.Update();

            // fast fail if the pooled shot is no longer alive
            if (Dead) return;

            // only show the impact sprite if it's animating
            impactSprite.Visible = impactSprite.Animating;

            // if we're not collidable and no longer animating the impact, destroy
            if (!Collidable) {
                if (!impactSprite.Animating) {
                    Destroy();
                }
                return;
            }

            // update collider and projectile sprite with sinewave
            var newCentre = Speed.Perpendicular().SafeNormalize() * travelSineWave.Value * wiggleAmount;
            newCentre += hitDir * hitWiggler.Value * 8f;

            killHitbox.Center = parentEmitter.WiggleHitbox ? newCentre : Vector2.Zero;
            projectileSprite.Position = newCentre;

            if (Scene is not Level level) return;
            
            // delayed init
            if (delayTimeRemaining > 0) {
                delayTimeRemaining -= Engine.DeltaTime;
                if (delayTimeRemaining > 0) {
                    return;
                }

                projectileSprite.Visible = true;
                projectileSprite.Play(projectileAnimationKey);
                parentEmitter?.PlayFireSound(firedCassetteIndex);
            }

            Move();

            if (level.OnInterval(0.05f)) {
                level.ParticlesBG.Emit(particleType, 1, Center, Vector2.One * 2f, (-Speed).Angle());
            }

            // destroy the shot if it leaves the room bounds
            if (!level.IsInBounds(this)) {
                Destroy();
            }
        }

        private void Move() {
            var delta = Speed * Engine.DeltaTime;
            var target = Position + delta;

            // check whether the target position would trigger a new solid collision
            if (CollideWithSolids && CollideCheckOutside<Solid>(target)) {
                var normal = delta.SafeNormalize();

                // snap the current position away from the solid
                if (normal.X != 0) {
                    Position.X = normal.X < 0 ? (float) Math.Ceiling(Position.X) : (float) Math.Floor(Position.X);
                }
                if (normal.Y != 0) {
                    Position.Y = normal.Y < 0 ? (float) Math.Ceiling(Position.Y) : (float) Math.Floor(Position.Y);
                }

                // move one pixel at a time to find the exact collision point (with safety counter)
                int safety = 50;
                while (safety-- > 0) {
                    // if it collided...
                    var solid = CollideFirst<Solid>(Position + normal);
                    if (solid != null) {
                        // snap the shot to the collision point
                        if (normal.X < 0) {
                            Position.X = (float) Math.Floor(Collider.AbsoluteLeft);
                            Position.Y = (float) Math.Round(Collider.CenterY + Position.Y);
                        } else if (normal.X > 0) {
                            Position.X = (float) Math.Ceiling(Collider.AbsoluteRight);
                            Position.Y = (float) Math.Round(Collider.CenterY + Position.Y);
                        } else if (normal.Y < 0) {
                            Position.Y = (float) Math.Floor(Collider.AbsoluteTop);
                            Position.X = (float) Math.Round(Collider.CenterX + Position.X);
                        } else if (normal.Y > 0) {
                            Position.Y = (float) Math.Ceiling(Collider.AbsoluteBottom);
                            Position.X = (float) Math.Round(Collider.CenterX + Position.X);
                        }

                        // stop... hammer time!
                        Speed = Vector2.Zero;

                        // trigger the impact animation
                        Impact(solid is not SolidTiles);
                        return;
                    }

                    Position += normal;
                }
            }

            Position = target;
        }

        private void Impact(bool air) {
            projectileSprite.Stop();
            projectileSprite.Visible = false;
            impactSprite.Play(air ? $"{impactAnimationKey}_air" : impactAnimationKey);
            impactSprite.Visible = true;
            Collidable = false;
            killHitbox.Center = projectileSprite.Position = Vector2.Zero;
            
            if (Scene is Level level && level.Camera.Collides(this)) {
                Audio.Play(CustomSFX.paint_emitter_impact, Center);
            }
        }

        private void OnPlayerCollide(Player player) {
            var direction = (player.Center - Position).SafeNormalize();
            if (player.Die(direction) == null) return;
            Speed = Vector2.Zero;
            travelSineWave.Active = false;
            hitDir = direction;
            hitWiggler.Start();
        }
    }
}
