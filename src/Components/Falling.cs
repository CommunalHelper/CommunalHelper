using System.Collections;

namespace Celeste.Mod.CommunalHelper.Components;

public class Falling : Component
{
    // actions
    public Func<bool> PlayerFallCheck;
    public Func<bool> PlayerWaitCheck;
    public Action ShakeSfx;
    public Action ImpactSfx;
    public Action LandParticles;
    public Action FallParticles;

    // config
    public float FallDelay;
    public bool ShouldRumble = true;
    public bool ClimbFall = true;
    public float FallSpeed = 160f;
    public bool ShouldManageSafe = true;
    public bool FallUp = false;
    public float ShakeTime = 0.4f;
    public bool EndOnSolidTiles = true;
    public bool FallImmediately = false;

    // coroutine properties
    public bool Triggered;
    public bool HasStartedFalling { get; private set; }

    private Coroutine coroutine;

    public new Solid Entity => EntityAs<Solid>();

    public Falling() : base(false, false)
    {
        PlayerFallCheck = OnPlayerFallCheck;
        PlayerWaitCheck = OnPlayerWaitCheck;
        ShakeSfx = OnShakeSfx;
        ImpactSfx = OnImpactSfx;
        LandParticles = OnLandParticles;
        FallParticles = OnFallParticles;
    }

    public override void Added(Entity entity)
    {
        // need to call base first to ensure we balance Add/Remove
        base.Added(entity);

        if (entity is not Solid)
        {
            Util.Log(LogLevel.Warn, $"Attempted to add {nameof(Falling)} to a non-Solid ({entity.GetType().Name})");
            RemoveSelf();
            return;
        }

        Reset();
    }

    public void Reset()
    {
        coroutine?.RemoveSelf();
        Entity?.Add(coroutine = new Coroutine(FallingSequence()));
        if (ShouldManageSafe && Entity is not null)
            Entity.Safe = false;
    }

    private bool OnPlayerFallCheck()
    {
        if (Entity is null)
            return false;
        return ClimbFall ? Entity.HasPlayerRider() : Entity.HasPlayerOnTop();
    }

    private bool OnPlayerWaitCheck()
    {
        if (Entity is null)
            return false;
        if (Triggered || PlayerFallCheck?.Invoke() == true)
            return true;
        if (!ClimbFall)
            return false;
        return Entity.CollideCheck<Player>(Entity.Position - Vector2.UnitX) || Entity.CollideCheck<Player>(Entity.Position + Vector2.UnitX);
    }

    private void OnShakeSfx()
    {
        if (Entity is not null)
            Audio.Play(SFX.game_gen_fallblock_shake, Entity.Center);
    }

    private void OnImpactSfx()
    {
        if (Entity is not null)
            Audio.Play(SFX.game_gen_fallblock_impact, Entity.BottomCenter);
    }

    private void OnFallParticles()
    {
        if (Entity is null)
            return;

        var level = SceneAs<Level>();
        for (int x = 2; x < Entity.Width; x += 4)
        {
            var position = new Vector2(Entity.X + x, Entity.Y);
            var range = Vector2.One * 4f;
            var direction = (float) Math.PI / 2f;
            var offset = new Vector2(x, -2f);
            var check = FallUp ? Entity.BottomLeft - offset : Entity.TopLeft + offset;
            if (level.CollideCheck<Solid>(check))
                level.Particles.Emit(FallingBlock.P_FallDustA, 2, position, range, FallUp ? -direction : direction);
            level.Particles.Emit(FallingBlock.P_FallDustB, 2, position, range);
        }
    }

    private void OnLandParticles()
    {
        if (Entity is null)
            return;

        var level = SceneAs<Level>();
        for (int x = 2; x <= Entity.Width; x += 4)
        {
            var offset = new Vector2(x, 3f);
            var checkPosition = FallUp ? Entity.TopLeft - offset : Entity.BottomLeft + offset;
            if (level.CollideCheck<Solid>(checkPosition))
            {
                var position = new Vector2(Entity.X + x, FallUp ? Entity.Top : Entity.Bottom);
                var range = Vector2.One * 4f;
                var fallDustDirection = -(float) Math.PI / 2f;
                level.ParticlesFG.Emit(FallingBlock.P_FallDustA, 1, position, range, FallUp ? -fallDustDirection : fallDustDirection);
                var landDustDirection = x >= Entity.Width / 2f ? 0f : (float) Math.PI;
                level.ParticlesFG.Emit(FallingBlock.P_LandDust, 1, position, range, FallUp ? -landDustDirection : landDustDirection);
            }
        }
    }

    private IEnumerator FallingSequence()
    {
        // cache things
        var self = this;
        var entity = self.Entity;
        var level = entity?.SceneAs<Level>();

        // unlikely but safety
        if (entity is null)
            yield break;

        // reset things
        if (self.ShouldManageSafe) entity.Safe = false;
        self.Triggered = self.FallImmediately;
        self.HasStartedFalling = false;

        // wait until we should fall
        while (!self.Triggered && self.PlayerFallCheck?.Invoke() != true)
            yield return null;

        if (FallImmediately)
        {
            // wait until we can fall
            while (entity.CollideCheck<Platform>(entity.Position + (FallUp ? -Vector2.UnitY : Vector2.UnitY)))
                yield return 0.1f;
        }
        else
        {
            // wait for the delay
            float fallDelayRemaining = self.FallDelay;
            while (fallDelayRemaining > 0)
            {
                fallDelayRemaining -= Engine.DeltaTime;
                yield return null;
            }
        }

        self.HasStartedFalling = true;

        // loop forever
        while (true)
        {
            if (ShakeTime > 0)
            {
                // start shaking
                self.ShakeSfx?.Invoke();
                entity.StartShaking();
                if (self.ShouldRumble) Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);

                // shake for a while
                for (float timer = ShakeTime; timer > 0 && self.PlayerWaitCheck?.Invoke() != false; timer -= Engine.DeltaTime)
                    yield return null;

                // stop shaking
                entity.StopShaking();
            }

            // particles
            self.FallParticles?.Invoke();

            // fall
            float speed = 0f;
            float maxSpeed = self.FallSpeed;
            while (true)
            {
                // update the speed
                speed = Calc.Approach(speed, maxSpeed, 500f * Engine.DeltaTime);
                // try to move
                if (!entity.MoveVCollideSolids(speed * Engine.DeltaTime * (FallUp ? -1 : 1), true))
                {
                    // if we've fallen out the bottom of the screen, we should remove the entity
                    // otherwise yield for a frame and loop
                    if (!FallUp && entity.Top <= level.Bounds.Bottom + 16 && (entity.Top <= level.Bounds.Bottom - 1 || !entity.CollideCheck<Solid>(entity.Position + Vector2.UnitY)) ||
                        FallUp && entity.Bottom >= level.Bounds.Top - 16 && (entity.Bottom >= level.Bounds.Top + 1 || !entity.CollideCheck<Solid>(entity.Position - Vector2.UnitY)))
                        yield return null;
                    else
                    {
                        // we've fallen out of the screen and should remove the entity
                        entity.Collidable = entity.Visible = false;
                        yield return 0.2f;
                        if (level.Session.MapData.CanTransitionTo(level, new Vector2(entity.Center.X, FallUp ? (entity.Top - 12f) : (entity.Bottom + 12f))))
                        {
                            yield return 0.2f;
                            level.Shake();
                            if (ShouldRumble) Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                        }

                        entity.RemoveSelf();
                        entity.DestroyStaticMovers();
                        yield break;
                    }
                }
                else
                {
                    // if we hit something, break
                    break;
                }
            }

            // impact effects
            self.ImpactSfx?.Invoke();
            if (self.ShouldRumble) Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
            level.DirectionalShake(FallUp ? -Vector2.UnitY : Vector2.UnitY);
            entity.StartShaking();
            self.LandParticles?.Invoke();
            yield return 0.2f;
            entity.StopShaking();

            // if it's hit the fg tiles then make it safe and end
            if (EndOnSolidTiles && entity.CollideCheck<SolidTiles>(entity.Position + (FallUp ? -Vector2.UnitY : Vector2.UnitY)))
            {
                entity.Safe |= self.ShouldManageSafe;
                yield break;
            }

            // wait until we can fall again
            while (entity.CollideCheck<Platform>(entity.Position + (FallUp ? -Vector2.UnitY : Vector2.UnitY)))
                yield return 0.1f;
        }
    }
}
