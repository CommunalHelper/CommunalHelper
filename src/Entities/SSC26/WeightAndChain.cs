using Celeste.Mod.CommunalHelper.Components;
using FMOD.Studio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CommunalHelper.Entities.SSC26;

[CustomEntity("CommunalHelper/WeightAndChain")]
[Tracked]
internal class WeightAndChain : Actor
{
    internal static void Load()
    {
        Everest.Events.Level.OnLoadLevel += OnLoadLevel;
        On.Celeste.Level.UnloadEntities += OnLevelUnloadEntities;
    }

    internal static void Unload()
    {
        Everest.Events.Level.OnLoadLevel -= OnLoadLevel;
        On.Celeste.Level.UnloadEntities -= OnLevelUnloadEntities;
    }

    private static void OnLevelUnloadEntities(On.Celeste.Level.orig_UnloadEntities orig, Level self, List<Entity> entities)
    {
        DoWeightAndChainDedup(self);
        orig(self, entities);
    }

    private static void OnLoadLevel(Level level, Player.IntroTypes playerIntro, bool isFromLoader)
    {
        if (playerIntro != Player.IntroTypes.Transition) DoWeightAndChainDedup(level);
    }

    internal static void DoWeightAndChainDedup(Level level)
    {
        Player p = level.Tracker.GetEntity<Player>();
        if (p == null) return;
        WeightAndChainComponent component = p.Get<WeightAndChainComponent>();
        if (component == null) p.Add(component = new WeightAndChainComponent());
        component.DeduplicateBalls(level);
    }


    private static readonly MTexture weight = GFX.Game["objects/CommunalHelper/weightAndChain/weight"];
    private static readonly MTexture[] links = {
        GFX.Game["objects/CommunalHelper/weightAndChain/chainlink_0"],
        GFX.Game["objects/CommunalHelper/weightAndChain/chainlink_1"],
    };

    internal enum States
    {
        Inactive,
        Scripted,
        Normal,
        Dead,
    }

    public Vector2 Speed;
    public Holdable Hold;
    internal States State = States.Normal;
    private Level Level;
    private Collision onCollideH;
    private Collision onCollideV;
    private float noGravityTimer;
    private Vector2 prevLiftSpeed;
    private TransitionListener transitionListener;
    internal WeightAndChainComponent BaCComponent;
    public float chainLength = 56f;

    public bool IsTransitioningOut { get; private set; } = false;

    private Player player => BaCComponent?.Entity as Player;
    public Vector2 ChainConnectionRenderOffset => new Vector2(0, -20);
    public Vector2 ChainConnectionRenderPosition => Position + ChainConnectionRenderOffset;
    public Vector2 ChainConnectionPhysicsOffset => new Vector2(0, -6);
    public Vector2 ChainConnectionPhysicsPosition => ExactPosition + ChainConnectionPhysicsOffset;

    public WeightAndChain(EntityData data, Vector2 offset) : base(data.Position + offset + new Vector2(0, -2))
    {
        if (data.Bool("intro", false))
        {
            State = States.Inactive;
        }
        Depth = Depths.TheoCrystal;
        Collider = new Hitbox(8f, 12f, -4f, -10f);
        onCollideH = OnCollideH;
        onCollideV = OnCollideV;
        LiftSpeedGraceTime = 0.1f;
        Add(new VertexLight(Collider.Center, Color.White, 1f, 32, 64));
        Add(new MirrorReflection());
        Add(Hold = new(0.1f)
        {
            PickupCollider = new Hitbox(40f, 40f, -20f, -26f),
            SlowFall = false,
            SlowRun = true,
            OnPickup = OnPickup,
            OnRelease = OnRelease,
            DangerousCheck = Dangerous,
            OnHitSpring = HitSpring,
            SpeedGetter = () => Speed,
            SpeedSetter = delegate (Vector2 speed)
            {
                Speed = speed;
            }
        });
        Add(transitionListener = new TransitionListener());
        transitionListener.OnOutBegin += OnTransitionOutBegin;
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);
        Level = SceneAs<Level>();
        Depth = Depths.TheoCrystal;
        WeightAndChain held = scene.Tracker.GetEntity<Player>()?.Holding?.Entity as WeightAndChain;
        if (held != null && held != this)
        {
            RemoveSelf();
        }
    }

    public override void Removed(Scene scene)
    {
        base.Removed(scene);
        if (BaCComponent?.ball == this)
        {
            BaCComponent.ball = null;
            BaCComponent = null;
        }
    }

    private void OnTransitionOutBegin()
    {
        if (!TagCheck(Tags.Persistent))
        {
            IsTransitioningOut = true;
        }
    }

    public override void Update()
    {
        base.Update();
        switch (State)
        {
            case States.Inactive:
            case States.Dead:
                return;
            case States.Scripted:
                Hold.cannotHoldTimer = 0.1f;
                break;
            case States.Normal:
                break;
        }
        Depth = Depths.TheoCrystal;

        IEnumerable<MagnetField> fields = CollideAll<MagnetField>().Cast<MagnetField>();
        if (fields.Any())
        {
            if (Hold.IsHeld)
            {
                //player.Speed = Calc.Approach(Speed, Vector2.Zero, 800f * Engine.DeltaTime);
                foreach (MagnetField field in fields)
                {
                    if (field.Direction == "Up")
                    {
                        player.varJumpTimer = 0f;
                        if (player.onGround) continue;
                    }
                    player.Speed += field.Attraction * Engine.DeltaTime;
                }
            }
            else
            {
                Speed = Calc.Approach(Speed, Vector2.Zero, 800f * Engine.DeltaTime);
                foreach (MagnetField field in fields)
                {
                    if (OnGround() && field.Direction == "Up") continue;
                    Speed += field.Attraction * Engine.DeltaTime;
                }
            }
            MoveH(Speed.X * Engine.DeltaTime, onCollideH);
            MoveV(Speed.Y * Engine.DeltaTime, onCollideV);
        }
        else if (Hold.IsHeld)
        {
            prevLiftSpeed = Vector2.Zero;
        }
        else
        {
            if (OnGround())
            {
                float target = (!OnGround(Position + Vector2.UnitX * 3f)) ? 20f : (OnGround(Position - Vector2.UnitX * 3f) ? 0f : (-20f));
                Speed.X = Calc.Approach(Speed.X, target, 800f * Engine.DeltaTime);
                Vector2 liftSpeed = LiftSpeed;
                if (liftSpeed == Vector2.Zero && prevLiftSpeed != Vector2.Zero)
                {
                    Speed = prevLiftSpeed;
                    prevLiftSpeed = Vector2.Zero;
                    Speed.Y = Math.Min(Speed.Y * 0.6f, 0f);
                    if (Speed.X != 0f && Speed.Y == 0f)
                    {
                        Speed.Y = -60f;
                    }
                    if (Speed.Y < 0f)
                    {
                        noGravityTimer = 0.15f;
                    }
                }
                else
                {
                    prevLiftSpeed = liftSpeed;
                    if (liftSpeed.Y < 0f && Speed.Y < 0f)
                    {
                        Speed.Y = 0f;
                    }
                }
            }
            else if (Hold.ShouldHaveGravity)
            {
                float num = 800f;
                if (Math.Abs(Speed.Y) <= 30f)
                {
                    num *= 0.5f;
                }
                float num2 = 350f;
                if (Speed.Y < 0f)
                {
                    num2 *= 0.5f;
                }
                Speed.X = Calc.Approach(Speed.X, 0f, num2 * Engine.DeltaTime);
                if (noGravityTimer > 0f)
                {
                    noGravityTimer -= Engine.DeltaTime;
                }
                else
                {
                    Speed.Y = Calc.Approach(Speed.Y, 200f, num * Engine.DeltaTime);
                }
            }
            MoveH(Speed.X * Engine.DeltaTime, onCollideH);
            MoveV(Speed.Y * Engine.DeltaTime, onCollideV);
            if (State != States.Dead && !IsTransitioningOut)
            {
                EnforceBounds();
            }
        }
        if (State != States.Dead)
        {
            Hold.CheckAgainstColliders();
            foreach (WeightMagnet magnet in CollideAll<WeightMagnet>().Cast<WeightMagnet>())
            {
                magnet.Break();
            }
        }
    }

    public void IntroEnter()
    {
        State = States.Normal;
    }

    private void EnforceBounds()
    {
        if (Center.X > Level.Bounds.Right)
        {
            Right = Level.Bounds.Right;
            Speed.X *= -0.4f;
            MoveH(32f * Engine.DeltaTime);
        }
        else if (Left < Level.Bounds.Left)
        {
            Left = Level.Bounds.Left;
            Speed.X *= -0.4f;
        }
        else if (Top < (Level.Bounds.Top - 4))
        {
            Top = Level.Bounds.Top + 4;
            Speed.Y = 0f;
        }
        else if (Bottom > Level.Bounds.Bottom && SaveData.Instance.Assists.Invincible)
        {
            Bottom = Level.Bounds.Bottom;
            Speed.Y = -300f;
            Audio.Play("event:/game/general/assist_screenbottom", Position);
        }
        if (X < (Level.Bounds.Left + 10))
        {
            MoveH(32f * Engine.DeltaTime);
        }
    }

    public void ExplodeLaunch(Vector2 from)
    {
        if (!Hold.IsHeld)
        {
            Speed = (Center - from).SafeNormalize(120f);
            SlashFx.Burst(Center, Speed.Angle());
        }
    }

    public void HitSpinner(Entity spinner)
    {
        //if (!Hold.IsHeld && Speed.Length() < 0.01f && LiftSpeed.Length() < 0.01f && (previousPosition - ExactPosition).Length() < 0.01f && OnGround())
        //{
        //    int num = Math.Sign(X - spinner.X);
        //    if (num == 0)
        //    {
        //        num = 1;
        //    }
        //    Speed.X = num * 120f;
        //    Speed.Y = -30f;
        //}
    }

    public bool HitSpring(Spring spring)
    {
        if (!Hold.IsHeld)
        {
            if (spring.Orientation == Spring.Orientations.Floor && Speed.Y >= 0f)
            {
                Speed.X *= 0.5f;
                Speed.Y = -160f;
                noGravityTimer = 0.15f;
                return true;
            }
            if (spring.Orientation == Spring.Orientations.WallLeft && Speed.X <= 0f)
            {
                MoveTowardsY(spring.CenterY + 5f, 4f);
                Speed.X = 220f;
                Speed.Y = -120f;
                noGravityTimer = 0.1f;
                return true;
            }
            if (spring.Orientation == Spring.Orientations.WallRight && Speed.X >= 0f)
            {
                MoveTowardsY(spring.CenterY + 5f, 4f);
                Speed.X = -220f;
                Speed.Y = -120f;
                noGravityTimer = 0.1f;
                return true;
            }
        }
        return false;
    }

    private void OnCollideH(CollisionData data)
    {
        if (data.Hit is DashSwitch)
        {
            (data.Hit as DashSwitch).OnDashCollide(null, Vector2.UnitX * Math.Sign(Speed.X));
        }
        Audio.Play("event:/game/05_mirror_temple/crystaltheo_hit_side", Position);
        if (Math.Abs(Speed.X) > 100f)
        {
            ImpactParticles(data.Direction);
        }
        Speed.X *= -0.4f;
    }

    private void OnCollideV(CollisionData data)
    {
        if (data.Hit is DashSwitch)
        {
            (data.Hit as DashSwitch).OnDashCollide(null, Vector2.UnitY * Math.Sign(Speed.Y));
        }
        if (Speed.Y > 0f)
        {
            EventInstance evt = Audio.Play("event:/game/05_mirror_temple/crystaltheo_hit_ground", Position, "crystal_velocity", Calc.ClampedMap(Speed.Y, 0f, 200f));
            evt.setVolume(2f);
            evt.setPitch(0.6f);
        }
        ImpactParticles(data.Direction);
        Speed.Y = 0f;  // No bouncing ever for this one
    }

    private void ImpactParticles(Vector2 dir)
    {
        float direction;
        Vector2 position;
        Vector2 positionRange;
        if (dir.X > 0f)
        {
            direction = MathF.PI;
            position = new Vector2(base.Right, base.Y - 4f);
            positionRange = Vector2.UnitY * 6f;
        }
        else if (dir.X < 0f)
        {
            direction = 0f;
            position = new Vector2(base.Left, base.Y - 4f);
            positionRange = Vector2.UnitY * 6f;
        }
        else if (dir.Y > 0f)
        {
            direction = -MathF.PI / 2f;
            position = new Vector2(base.X, base.Bottom);
            positionRange = Vector2.UnitX * 6f;
        }
        else
        {
            direction = MathF.PI / 2f;
            position = new Vector2(base.X, base.Top);
            positionRange = Vector2.UnitX * 6f;
        }
        Level.Particles.Emit(TheoCrystal.P_Impact, 12, position, positionRange, direction);
    }

    public override bool IsRiding(Solid solid)
    {
        if (Speed.Y == 0f)
        {
            return base.IsRiding(solid);
        }
        return false;
    }

    public override void OnSquish(CollisionData data)
    {
        if (!TrySquishWiggle(data, 3, 3) && !SaveData.Instance.Assists.Invincible)
        {
            Die();
        }
    }

    private void OnPickup()
    {
        Speed = Vector2.Zero;
        AddTag(Tags.Persistent);
    }

    private void OnRelease(Vector2 force)
    {
        RemoveTag(Tags.Persistent);
        if (force.X != 0f && force.Y == 0f)
        {
            force.Y = -0.4f;
        }
        Speed = force * 200f;
        if (Speed != Vector2.Zero)
        {
            noGravityTimer = 0.1f;
        }
    }

    public bool Dangerous(HoldableCollider holdableCollider) => false;

    public void Die()
    {
        if (State != States.Dead)
        {
            State = States.Dead;
            Player player = Level.Tracker.GetEntity<Player>();
            player?.Die(-Vector2.UnitX * (float) player.Facing);
            Audio.Play("event:/char/madeline/death", Position);
            Add(new DeathEffect(Color.ForestGreen, Center - Position));
            Depth = -1000000;
            AllowPushing = false;
        }
    }

    override public void Render()
    {
        base.Render();
        if (State == States.Dead || State == States.Inactive) return;
        weight.DrawCentered(Position + new Vector2(0, -8));

        if (BaCComponent?.Attached != true) return;

        int numlinks = (int) Math.Ceiling(chainLength / 5f);
        float rotation = (player.Center - ChainConnectionRenderPosition).Angle();
        for (int i = 0; i < numlinks; i++)
        {
            int actualIdx = (i & 1) == 0 ? i / 2 : (i + numlinks) / 2;  // links alternate in render order
            float t = (actualIdx + 0.5f) / numlinks;
            Vector2 pos = Vector2.Lerp(player.Center, ChainConnectionRenderPosition, t);
            links[actualIdx % 2].DrawCentered(pos, Color.White, 1, rotation);
        }

        float ringAlpha = Calc.Map(BaCComponent.DistBetweenPlayerAndBall, chainLength * 0.4f, chainLength, 0f, 0.25f);
        Draw.Circle(ChainConnectionPhysicsPosition, chainLength, Color.Silver * ringAlpha, 16);
    }
}
