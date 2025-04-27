using Celeste.Mod.CommunalHelper.Components;
using MonoMod.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Entities.StrawberryJam;

[Tracked]
[CustomEntity("CommunalHelper/SJ/Paintbrush")]
public class Paintbrush : Entity {
    #region Properties

    public bool CollideWithSolids { get; }
    public bool KillPlayer { get; }
    public int CassetteIndex { get; }
    public bool HalfLength { get; }

    protected int DataWidth { get; }
    protected int DataHeight { get; }

    protected int Size => Orientation.Vertical() ? DataWidth : DataHeight;
    protected int Tiles => Size / tileSize;

    public LaserOrientations Orientation { get; }
    
    #endregion

    private string animationPrefix => CassetteIndex == 0 ? "blue" : "pink";
    private string chargingAnimation => $"{animationPrefix}_charging";
    private string firingAnimation => $"{animationPrefix}_firing";
    private string burstAnimation => $"{animationPrefix}_burst";
    private string idleAnimation => $"{animationPrefix}_idle";
    private string cooldownAnimation => $"{animationPrefix}_cooldown";
    private Vector2 beamOffset => Orientation.Normal() * beamOffsetMultiplier;
    private Color telegraphColor => ColorFromCassetteIndex(CassetteIndex);
    private Color beamFillColor => CassetteIndex == 0 ? Calc.HexToColor("73efe8") : Calc.HexToColor("ff8eae");

    private readonly Sprite largeBrushSprite;
    private readonly Sprite smallBrushSprite;
    private readonly Sprite beamSprite;
    private readonly Sprite paintParticlesSprite;
    private readonly Sprite paintBackSprite;
    private readonly SoundSource rampUpSource;
    private readonly SoundSource fireSource;
    private readonly int[] smallBrushFrames;

    private readonly Collider[] brushHitboxes;
    private readonly Hitbox[] beamHitboxes;
    private readonly ColliderList inactiveColliderList;
    private readonly ColliderList activeColliderList;
    private readonly CassetteListener cassetteListener;
    private LaserState laserState;

    private const float chargeDelayFraction = 0.25f;
    private const float collisionDelaySeconds = 5f / 60f;
    private const float burstTimeSeconds = 0.2f;
    private const float fireSoundDelaySeconds = 10f / 60f;
    private const int beamOffsetMultiplier = 4;
    private const int beamThickness = 12;
    private const float mediumRumbleEffectRange = 8f * 12;
    private const float strongRumbleEffectRange = 8f * 8;
    private const int tileSize = 8;

    private float collisionDelayRemaining;
    private float chargeDelayRemaining;
    private float burstTimeRemaining;
    private float fireSoundDelayRemaining;
    private Vector2 shakeOffset;

    private static ParticleType blueCooldownParticle;
    private static ParticleType pinkCooldownParticle;
    private static ParticleType blueImpactParticle;
    private static ParticleType pinkImpactParticle;
        
    public static Color ColorFromCassetteIndex(int index) => index switch {
        0 => Calc.HexToColor("49aaf0"),
        1 => Calc.HexToColor("f049be"),
        2 => Calc.HexToColor("fcdc3a"),
        3 => Calc.HexToColor("38e04e"),
        _ => Color.White
    };
    
    public static void LoadParticles() {
        blueCooldownParticle ??= new ParticleType(Booster.P_Burst) {
            Source = GFX.Game["particles/blob"],
            Color = Calc.HexToColor("42bfe8"),
            Color2 = Calc.HexToColor("7550e8"),
            ColorMode = ParticleType.ColorModes.Fade,
            LifeMin = 0.5f,
            LifeMax = 0.8f,
            Size = 0.7f,
            SizeRange = 0.25f,
            ScaleOut = true,
            Direction = 5.712389f,
            DirectionRange = 1.17453292f,
            SpeedMin = 40f,
            SpeedMax = 100f,
            SpeedMultiplier = 0.005f,
            Acceleration = Vector2.Zero,
        };

        pinkCooldownParticle ??= new ParticleType(blueCooldownParticle) {
            Color = Calc.HexToColor("e84292"), Color2 = Calc.HexToColor("9c2a70"),
        };

        blueImpactParticle ??= new ParticleType(Booster.P_Burst) {
            Source = GFX.Game["particles/fire"],
            Color = Calc.HexToColor("ffffff"),
            Color2 = Calc.HexToColor("73efe8"),
            ColorMode = ParticleType.ColorModes.Fade,
            LifeMin = 0.3f,
            LifeMax = 0.5f,
            Size = 0.7f,
            SizeRange = 0.25f,
            ScaleOut = true,
            Direction = 4.712389f,
            DirectionRange = 3.14159f,
            SpeedMin = 10f,
            SpeedMax = 80f,
            SpeedMultiplier = 0.005f,
            Acceleration = Vector2.Zero,
        };

        pinkImpactParticle ??= new ParticleType(blueImpactParticle) {Color2 = Calc.HexToColor("ef73bf"),};
    }

    private void setAnimationSpeed(string key, float totalRunTime) {
        if (largeBrushSprite.Animations.TryGetValue(key, out var emitterAnimation))
            emitterAnimation.Delay = totalRunTime / emitterAnimation.Frames.Length;
        if (paintParticlesSprite.Animations.TryGetValue(key, out var paintAnimation))
            paintAnimation.Delay = totalRunTime / paintAnimation.Frames.Length;
    }

    public LaserState State {
        get => laserState;
        set => setState(value);
    }

    private void setState(LaserState state, bool force = false) {
        if (!force && laserState == state) return;
        laserState = state;
            
        switch (State) {
            case LaserState.Idle:
                largeBrushSprite.Play(idleAnimation);
                Collider = inactiveColliderList;
                break;
                
            case LaserState.Precharge:
                largeBrushSprite.Play(idleAnimation);
                Collider = inactiveColliderList;
                PlayIfInBounds(rampUpSource, CustomSFX.paint_paintbrush_laser_ramp_up);
                break;

            case LaserState.Charging:
                largeBrushSprite.Play(chargingAnimation);
                paintParticlesSprite.Play(chargingAnimation);
                Collider = inactiveColliderList;
                break;

            case LaserState.Burst:
                largeBrushSprite.Play(burstAnimation);
                paintParticlesSprite.Play(burstAnimation);
                beamSprite.Play(burstAnimation);
                Collider = inactiveColliderList;
                collisionDelayRemaining = collisionDelaySeconds;
                burstTimeRemaining = burstTimeSeconds;
                playNearbyEffects();
                break;

            case LaserState.Firing:
                largeBrushSprite.Play(firingAnimation);
                paintParticlesSprite.Play(firingAnimation);
                paintBackSprite.Play(firingAnimation);
                beamSprite.Play(firingAnimation);
                collisionDelayRemaining = 0;
                burstTimeRemaining = 0;
                Collider = activeColliderList;
                break;

            case LaserState.Cooldown:
                largeBrushSprite.Play(cooldownAnimation);
                paintParticlesSprite.Play(cooldownAnimation);
                paintBackSprite.Play(cooldownAnimation);
                Collider = inactiveColliderList;
                emitCooldownParticles();
                if (shouldEndFireSource()) {
                    fireSource.Param("end", 1f);
                }
                break;
        }
    }

    private bool shouldEndFireSource()
    {
        // if this is a full length brush then we should end the firing sound
        if (!HalfLength) return true;

        // go through all the brushes in the scene
        var brushes = SceneAs<Level>().Tracker.GetEntities<Paintbrush>();
        foreach (Paintbrush brush in brushes)
        {
            // if the brush is the same cassette index, is a full length brush, and is currently firing
            if (brush != this && brush.CassetteIndex == CassetteIndex && !brush.HalfLength && brush.State == LaserState.Firing)
            {
                // we don't want to end the firing sound early
                return false;
            }
        }

        // no other matching brushes found, so we're allowed to end the firing sound
        return true;
    }
    
    private void playNearbyEffects() {
        if (Scene.Tracker.Entities.ContainsKey(typeof(Player)) && Scene.Tracker.GetEntity<Player>() is { } player) {
            float distanceSquared = (player.Position - Position).LengthSquared();
            if (distanceSquared <= strongRumbleEffectRange * strongRumbleEffectRange) {
                SceneAs<Level>().Shake(0.2f);
                Input.Rumble(RumbleStrength.Strong, RumbleLength.Short);
            } else if (distanceSquared <= mediumRumbleEffectRange * mediumRumbleEffectRange) {
                SceneAs<Level>().Shake(0.1f);
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
            } else {
                Input.Rumble(RumbleStrength.Light, RumbleLength.Short);
            }
        }
    }

    public Paintbrush(EntityData data, Vector2 offset)
        : base(data.Position + offset) {
        LoadParticles();
        
        CollideWithSolids = data.Bool("collideWithSolids", true);
        KillPlayer = data.Bool("killPlayer", true);
        CassetteIndex = data.Int("cassetteIndex", 0);
        HalfLength = data.Bool("halfLength");
        DataWidth = data.Width;
        DataHeight = data.Height;
        Orientation = data.Enum<LaserOrientations>("orientation");
        Depth = Depths.Above - 1;
        
        largeBrushSprite = configureSprite(CommunalHelperGFX.SpriteBank.Create("paintbrushLargeBrush"));
        smallBrushSprite = configureSprite(CommunalHelperGFX.SpriteBank.Create("paintbrushSmallBrush"));
        paintParticlesSprite = configureSprite(CommunalHelperGFX.SpriteBank.Create("paintbrushPaintParticles"));
        paintBackSprite = configureSprite(CommunalHelperGFX.SpriteBank.Create("paintbrushPaintBack"));
        beamSprite = configureSprite(CommunalHelperGFX.SpriteBank.Create("paintbrushBeam"));
        
        largeBrushSprite.Play(idleAnimation);
        smallBrushSprite.Play(idleAnimation);
        
        beamSprite.Position = beamOffset;
        
        if (smallBrushSprite.Animations.TryGetValue(idleAnimation, out var smallBrushAnimation)) {
            var rnd = new Random((int) Position.LengthSquared());
            smallBrushFrames = Enumerable.Range(0, smallBrushAnimation.Frames.Length).ToArray();
            for (int i = 0; i < smallBrushFrames.Length - 1; i++) {
                int swapIndex = rnd.Next(i, smallBrushFrames.Length);
                if (swapIndex == i) continue;
                (smallBrushFrames[i], smallBrushFrames[swapIndex]) = (smallBrushFrames[swapIndex], smallBrushFrames[i]);
            }
        }
        
        Add(cassetteListener = new TickingCassetteListener(CassetteIndex) {
                OnActivated = () => {
                    State = LaserState.Burst;
                },
                OnDeactivated = () => {
                    State = State == LaserState.Firing ? LaserState.Cooldown : LaserState.Idle;
                },
                OnWillActivate = () => {
                    if (State == LaserState.Charging) {
                        fireSoundDelayRemaining = fireSoundDelaySeconds;
                    }
                },
                OnWillDeactivate = () => {
                    if (State == LaserState.Charging) {
                        fireSoundDelayRemaining = fireSoundDelaySeconds;
                    }
                },
                OnStart = activated => {
                    setState(activated && !HalfLength ? LaserState.Firing : LaserState.Idle, true);
                },
                OnTick = (cbm, isSwap) => {
                    if (isSwap) return;
                    var data = DynamicData.For(cbm);
                    float tempoMult = data.Get<float>("tempoMult");
                    int beatsPerTick = data.Get<int>("beatsPerTick");
                    if (State == LaserState.Firing && HalfLength) {
                        State = LaserState.Cooldown;
                    } else if (State == LaserState.Idle && !cassetteListener.Activated) {
                        var beatLength = (10 / 60f) / tempoMult;
                        var tickLength = beatLength * beatsPerTick;
                        chargeDelayRemaining = chargeDelayFraction * tickLength;
                        setAnimationSpeed(chargingAnimation, tickLength * (1 - chargeDelayFraction));
                        State = LaserState.Precharge;
                    }
                },
            },
            new PlayerCollider(onPlayerCollide),
            new LedgeBlocker(_ => KillPlayer),
            beamSprite,
            paintBackSprite,
            smallBrushSprite,
            largeBrushSprite,
            paintParticlesSprite,
            rampUpSource = new SoundSource(),
            fireSource = new SoundSource()
        );
            
        var brushHitboxList = new List<Collider>();
        var colliderOffset = Orientation.Vertical() ? new Vector2(tileSize, 0) : new Vector2(0, tileSize);
        for (int i = 1; i < Tiles; i += 2) {
            var coll = (Collider) new Circle(6);
            coll.Position = Orientation.Normal() * 2f + colliderOffset * i;
            brushHitboxList.Add(coll);
        }
        
        brushHitboxes = brushHitboxList.ToArray();
        inactiveColliderList = new ColliderList(brushHitboxes);
        
        var components = CreateLaserColliders().ToArray();
        beamHitboxes = components.Select(c => c.Collider).ToArray();
        Add(components.Cast<Component>().ToArray());
        activeColliderList = new ColliderList(brushHitboxes.Concat(beamHitboxes).ToArray());

        Add(new StaticMover
        {
            OnAttach = p => Depth = p.Depth + 1,
            SolidChecker = s => Collide.CheckPoint(s, Position - Orientation.Direction()),
            JumpThruChecker = jt => Collide.CheckPoint(jt, Position - Orientation.Direction()),
            OnEnable = () =>
            {
                Collidable = Visible = Active = true;
                State = LaserState.Idle;
            },
            OnDisable = () =>
            {
                Collidable = Visible = Active = false;
                State = LaserState.Idle;
                fireSource.Stop();
            },
            OnShake = v => shakeOffset += v,
            OnMove = v =>
            {
                Position += v;
                foreach (var collider in components)
                    collider.UpdateBeam();
            }
        });
    }

    private Sprite configureSprite(Sprite sprite) {
        sprite.Scale = Orientation is LaserOrientations.Left or LaserOrientations.Up
            ? new Vector2(-1, 1)
            : Vector2.One;
        sprite.Rotation = Orientation is LaserOrientations.Up or LaserOrientations.Down
            ? (float) Math.PI / 2f
            : 0f;
        return sprite;
    }

    protected virtual IEnumerable<LaserColliderComponent> CreateLaserColliders() {
        var offset = Orientation.Vertical() ? new Vector2(tileSize, 0) : new Vector2(0, tileSize);

        if (Tiles == 2) {
            return new[] {
                new LaserColliderComponent {
                    CollideWithSolids = CollideWithSolids,
                    Thickness = beamThickness,
                    Offset = offset + beamOffset,
                    Orientation = Orientation,
                }
            };
        }

        var start = offset / 2;
        return Enumerable.Range(0, Tiles).Select(i => new LaserColliderComponent {
            CollideWithSolids = CollideWithSolids,
            Thickness = tileSize,
            Offset = start + offset * i,
            Orientation = Orientation,
        });
    }

    public override void Added(Scene scene) {
        base.Added(scene);
        Add(new Coroutine(impactParticlesSequence()));
    }

    private void onPlayerCollide(Player player) {
        if (KillPlayer) {
            Vector2 direction;
            if (Orientation.Horizontal())
                direction = player.Center.Y <= Position.Y ? -Vector2.UnitY : Vector2.UnitY;
            else
                direction = player.Center.X <= Position.X ? -Vector2.UnitX : Vector2.UnitX;

            player.Die(direction);
        }
    }

    public override void Update() {
        base.Update();

        if (State == LaserState.Precharge && chargeDelayRemaining > 0) {
            chargeDelayRemaining -= Engine.DeltaTime;
            if (chargeDelayRemaining <= 0)
                State = LaserState.Charging;
        }

        if (collisionDelayRemaining > 0) {
            collisionDelayRemaining -= Engine.DeltaTime;
            if (collisionDelayRemaining <= 0)
                Collider = activeColliderList;
        }

        if (fireSoundDelayRemaining > 0) {
            fireSoundDelayRemaining -= Engine.DeltaTime;
            if (fireSoundDelayRemaining <= 0 && State >= LaserState.Charging && State <= LaserState.Firing) {
                PlayIfInBounds(fireSource, CassetteIndex == 0 ? CustomSFX.paint_paintbrush_laser_blue : CustomSFX.paint_paintbrush_laser_pink);
            }
        }
            
        if (State == LaserState.Burst && burstTimeRemaining > 0) {
            burstTimeRemaining -= Engine.DeltaTime;
            if (burstTimeRemaining <= 0)
                State = LaserState.Firing;
        }

        if (State == LaserState.Cooldown && !paintParticlesSprite.Animating) {
            State = LaserState.Idle;
        }
    }

    public override void Render() {
        // telegraphs should always be accurate, so don't shake them
        if (State is LaserState.Charging) {
            foreach (var hitbox in beamHitboxes)
                renderTelegraph(hitbox);
        }
        
        Position += shakeOffset;
        
        if (State is LaserState.Burst or LaserState.Firing) {
            for (int i = 0; i < beamHitboxes.Length; i++)
                renderBeam(beamHitboxes[i], i);
        }

        var offset = Orientation.Vertical() ? new Vector2(tileSize, 0) : new Vector2(0, tileSize);

        if (smallBrushSprite.Animations.TryGetValue(idleAnimation, out var smallBrushAnimation)) {
            int smallBrushFrameIndex = 0;
            for (int i = 2; i < Tiles; i += 2, smallBrushFrameIndex++) {
                smallBrushFrameIndex %= smallBrushFrames.Length;
                var smallBrushFrame = smallBrushAnimation.Frames[smallBrushFrames[smallBrushFrameIndex]];
                smallBrushFrame.Draw(Position + offset * i, smallBrushSprite.Origin, smallBrushSprite.Color, smallBrushSprite.Scale, smallBrushSprite.Rotation);
            }
        }

        if (paintBackSprite.CurrentAnimationID != string.Empty && State is LaserState.Firing or LaserState.Cooldown) {
            var firstHalf = Orientation.Horizontal() ? offset : offset * (Tiles - 1);
            var secondHalf = Orientation.Vertical() ? offset : offset * (Tiles - 1);
            var paintBackFrame = paintBackSprite.GetFrame(paintBackSprite.CurrentAnimationID, paintBackSprite.CurrentAnimationFrame);
            var paintBackTopHalf = paintBackFrame.GetSubtexture(new Rectangle(0, 0, paintBackFrame.Width, paintBackFrame.Height / 2));
            var paintBackBottomHalf = paintBackFrame.GetSubtexture(new Rectangle(0, paintBackFrame.Height / 2, paintBackFrame.Width, paintBackFrame.Height / 2));
            paintBackTopHalf.Draw(Position + firstHalf, new Vector2(0, paintBackTopHalf.Height), beamSprite.Color, beamSprite.Scale, beamSprite.Rotation);
            paintBackBottomHalf.Draw(Position + secondHalf, Vector2.Zero, beamSprite.Color, beamSprite.Scale, beamSprite.Rotation);
        }

        for (int i = 1; i < Tiles; i += 2) {
            largeBrushSprite.Position = offset * i;
            largeBrushSprite.Render();
        }

        if (State != LaserState.Idle) {
            for (int i = 1; i < Tiles; i += 2) {
                paintParticlesSprite.Position = offset * i;
                paintParticlesSprite.Render();
            }
        }

        Position -= shakeOffset;
    }

    private void renderBeam(Hitbox beamHitbox, int index) {
        if (beamSprite.CurrentAnimationID == string.Empty)
            return;

        var frame = beamSprite.GetFrame(beamSprite.CurrentAnimationID, beamSprite.CurrentAnimationFrame);
        float length = Math.Abs(Orientation switch {
            LaserOrientations.Up => beamSprite.Y - beamHitbox.Top,
            LaserOrientations.Down => beamSprite.Y - beamHitbox.Bottom,
            LaserOrientations.Left => beamSprite.X - beamHitbox.Left,
            LaserOrientations.Right => beamSprite.X - beamHitbox.Right,
            _ => 0,
        });

        var startPosition = Position + beamSprite.Position + (Orientation.Vertical()
            ? new Vector2(beamHitbox.CenterX, 0)
            : new Vector2(0, beamHitbox.CenterY));

        var frameOffset = Orientation.Normal() * frame.Width;
        var origin = beamSprite.Origin;

        if (beamHitboxes.Length > 1) {
            if (index == 0 && Orientation.Horizontal() || index == beamHitboxes.Length - 1 && Orientation.Vertical())
                frame = frame.GetSubtexture(new Rectangle(0, 0, frame.Width, frame.Height / 2));
            else if (index == 0 && Orientation.Vertical() || index == beamHitboxes.Length - 1 && Orientation.Horizontal())
                frame = frame.GetSubtexture(new Rectangle(0, frame.Height / 2, frame.Width, frame.Height / 2));
            else {
                var rectTopLeft = Position + beamHitbox.TopLeft;
                var color = State == LaserState.Burst && beamSprite.CurrentAnimationFrame == 0 ? Color.White : beamFillColor;
                Draw.Rect(rectTopLeft.X, rectTopLeft.Y, beamHitbox.Width, beamHitbox.Height, color);
                return;
            }
            origin = new Vector2(0, tileSize / 2f);
        }

        int count = (int) Math.Ceiling(length / frame.Width);
        int remainder = (int) length % frame.Width;

        for (int i = 0; i < count; i++) {
            var position = startPosition + i * frameOffset;
            int width = i == count - 1 && remainder != 0 ? remainder : frame.Width;
            frame.Draw(position, origin, beamSprite.Color, beamSprite.Scale, beamSprite.Rotation , new Rectangle(0, 0, width, frame.Height));
        }
    }

    private void renderTelegraph(Hitbox beamHitbox) {
        float animationProgress = (float)largeBrushSprite.CurrentAnimationFrame / largeBrushSprite.CurrentAnimationTotalFrames;
        int hitboxThickness = (int) Orientation.ThicknessOfHitbox(beamHitbox);
        int lerped = (int)Calc.LerpClamp(0, hitboxThickness, Ease.QuintOut(animationProgress));
        int thickness = Math.Min(lerped + 2, hitboxThickness);
        thickness -= thickness % 2;

        var rect = Orientation.Vertical()
            ? new Rectangle((int) (X + beamHitbox.CenterX) - thickness / 2, (int) (Y + beamHitbox.Top), thickness, (int) beamHitbox.Height)
            : new Rectangle((int) (X + beamHitbox.Left), (int) (Y + beamHitbox.CenterY) - thickness / 2, (int) beamHitbox.Width, thickness);

        Draw.Rect(rect, telegraphColor * 0.3f);
    }

    private void emitCooldownParticles() {
        int amount = beamHitboxes.Length == 1 ? 3 : 1;

        foreach (var laserHitbox in beamHitboxes) {
            var level = SceneAs<Level>();
            int length = (int) Orientation.LengthOfHitbox(laserHitbox) - beamOffsetMultiplier;
            var offset = Orientation.Normal();
            float angle = Orientation.Angle() - (float) Math.PI / 2f;
            var startPos =  Position + Orientation.OriginOfHitbox(laserHitbox) + beamOffset * 2;
            var particle = CassetteIndex == 0 ? blueCooldownParticle : pinkCooldownParticle;

            for (int i = 0; i < length; i += Calc.Random.Next(8, 16)) {
                level.ParticlesBG.Emit(particle, amount, startPos + offset * i, Vector2.Zero, angle);
            }
        }
    }

    private void emitImpactParticles(Hitbox laserHitbox) {
        var level = SceneAs<Level>();
        var particle = CassetteIndex == 0 ? blueImpactParticle : pinkImpactParticle;
        var offset = Orientation.Vertical() ? Vector2.UnitX : Vector2.UnitY;
        float angle = Orientation.Angle() + (float)Math.PI / 2f;

        int thickness = (int) Orientation.ThicknessOfHitbox(laserHitbox);
        var startPos = new Vector2(Orientation == LaserOrientations.Right ? laserHitbox.Right + X : laserHitbox.Left + X,
            Orientation == LaserOrientations.Down ? laserHitbox.Bottom + Y: laserHitbox.Top + Y);

        const int particleCount = 3;
        level.ParticlesFG.Emit(particle, particleCount, startPos, Vector2.Zero, angle);
        level.ParticlesFG.Emit(particle, particleCount, startPos + offset * thickness / 2, Vector2.Zero, angle);
        level.ParticlesFG.Emit(particle, particleCount, startPos + offset * thickness, Vector2.Zero, angle);
    }

    private IEnumerator impactParticlesSequence() {
        var laserColliders = Components.GetAll<LaserColliderComponent>().ToArray();

        while (Scene != null) {
            if (State != LaserState.Firing && State != LaserState.Burst) {
                yield return null;
                continue;
            }

            object yieldValue = null;
            foreach (var laser in laserColliders) {
                if (!laser.CollidedWithScreenBounds) {
                    yieldValue = 0.1f;
                    emitImpactParticles(laser.Collider);
                }
            }

            yield return yieldValue;
        }
    }

    private void PlayIfInBounds(SoundSource source, string path) {
        if (SceneAs<Level>().Camera.Collides(this, activeColliderList)) {
            source.Play(path);
        }
    }

    public enum LaserState {
        /// <summary>
        /// The laser is currently off.
        /// Collision = off.
        /// </summary>
        Idle,

        /// <summary>
        /// The laser is preparing to play the charge animation.
        /// It will wait a fraction of a tick before moving to the <see cref="Charging"/> state.
        /// <see cref="Paintbrush.chargeDelayFraction"/>
        /// Collision = off.
        /// </summary>
        Precharge,

        /// <summary>
        /// The laser is playing the charge animation.
        /// Starts shortly after the tick prior to the laser firing, and lasts for the rest of the tick.
        /// The telegraph beam should be displayed.
        /// Collision = off.
        /// </summary>
        Charging,

        /// <summary>
        /// The laser is playing the burst animation.
        /// Collision = off.
        /// </summary>
        Burst,

        /// <summary>
        /// The laser is firing.
        /// Starts shortly after the cassette swap.
        /// <see cref="Paintbrush.collisionDelaySeconds"/>
        /// Collision = on.
        /// </summary>
        Firing,

        Cooldown,
    }
}
