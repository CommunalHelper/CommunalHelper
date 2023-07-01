using Celeste.Mod.CommunalHelper.Entities;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System.Collections;
using System.Reflection;
using static Celeste.Mod.CommunalHelper.Entities.PlayerSeekerBarrierRenderer;

namespace Celeste.Mod.CommunalHelper.DashStates;

// Add custom playerhair that makes tentacles on head
// Green/Shadow tentacles that lash out from the player (fade in/out) while HasSeekerDash
// Localized glitch/seekerslowdown effect field
public static class SeekerDash
{
    private static bool hasSeekerDash;
    public static bool HasSeekerDash
    {
        get => hasSeekerDash || CommunalHelperModule.Settings.AlwaysActiveSeekerDash;
        set => hasSeekerDash = value;
    }

    public static bool SeekerAttacking => seekerDashAttacking || seekerDashLaunched;

    private static bool seekerDashAttacking;
    private static float seekerDashTimer;
    private static bool seekerDashLaunched;
    /// <summary>jank fix for wavedashing</summary>
    private static bool launchPossible;

    private static readonly MethodInfo m_Player_CallDashEvents = typeof(Player).GetMethod("CallDashEvents", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly MethodInfo m_Player_CorrectDashPrecision = typeof(Player).GetMethod("CorrectDashPrecision", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly MethodInfo m_Seeker_GotBouncedOn = typeof(Seeker).GetMethod("GotBouncedOn", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo f_Seeker_dead = typeof(Seeker).GetField("dead", BindingFlags.NonPublic | BindingFlags.Instance);

    private static IDetour hook_Player_get_CanDash;

    internal static void Load()
    {
        On.Celeste.Player.ctor += Player_ctor;
        hook_Player_get_CanDash = new Hook(
            typeof(Player).GetProperty("CanDash").GetGetMethod(),
            typeof(SeekerDash).GetMethod(nameof(Player_get_CanDash), BindingFlags.NonPublic | BindingFlags.Static));
        On.Celeste.Player.DashBegin += Player_DashBegin;
        IL.Celeste.Player.DashUpdate += Player_DashUpdate;
        On.Celeste.Player.DashEnd += Player_DashEnd;
        IL.Celeste.Player.CallDashEvents += Player_CallDashEvents;
        On.Celeste.Player.Update += Player_Update;
        On.Celeste.Player.OnCollideH += Player_OnCollideH;
        On.Celeste.Player.OnCollideV += Player_OnCollideV;

        // Rendering
        On.Celeste.Player.GetCurrentTrailColor += Player_GetCurrentTrailColor;

        // Player Seeker Hair
        IL.Celeste.Player.ctor += IL_Player_ctor;
        IL.Celeste.Player.TransitionTo += Player_TransitionTo;

        // Interactions
        On.Celeste.DashBlock.OnDashed += DashBlock_OnDashed;
        On.Celeste.TempleCrackedBlock.ctor_EntityID_Vector2_float_float_bool += TempleCrackedBlock_ctor_EntityID_Vector2_float_float_bool;
        On.Celeste.SeekerBarrier.ctor_Vector2_float_float += SeekerBarrier_ctor_Vector2_float_float;

        On.Celeste.Seeker.OnBouncePlayer += Seeker_OnBouncePlayer;
        On.Celeste.Seeker.OnAttackPlayer += Seeker_OnAttackPlayer;

        On.Celeste.AngryOshiro.OnPlayer += AngryOshiro_OnPlayer;
    }

    internal static void Unload()
    {
        On.Celeste.Player.ctor -= Player_ctor;
        hook_Player_get_CanDash.Dispose();
        On.Celeste.Player.DashBegin -= Player_DashBegin;
        IL.Celeste.Player.DashUpdate -= Player_DashUpdate;
        On.Celeste.Player.DashEnd -= Player_DashEnd;
        IL.Celeste.Player.CallDashEvents -= Player_CallDashEvents;
        On.Celeste.Player.Update -= Player_Update;
        On.Celeste.Player.OnCollideH -= Player_OnCollideH;
        On.Celeste.Player.OnCollideV -= Player_OnCollideV;

        On.Celeste.Player.GetCurrentTrailColor -= Player_GetCurrentTrailColor;

        IL.Celeste.Player.ctor -= IL_Player_ctor;
        IL.Celeste.Player.TransitionTo -= Player_TransitionTo;

        On.Celeste.DashBlock.OnDashed -= DashBlock_OnDashed;
        On.Celeste.TempleCrackedBlock.ctor_EntityID_Vector2_float_float_bool -= TempleCrackedBlock_ctor_EntityID_Vector2_float_float_bool;
        On.Celeste.SeekerBarrier.ctor_Vector2_float_float -= SeekerBarrier_ctor_Vector2_float_float;

        On.Celeste.Seeker.OnBouncePlayer -= Seeker_OnBouncePlayer;
        On.Celeste.Seeker.OnAttackPlayer -= Seeker_OnAttackPlayer;

        On.Celeste.AngryOshiro.OnPlayer -= AngryOshiro_OnPlayer;
    }

    #region Hooks

    // Initialize dash state & add Player Seeker Hair
    private static void Player_ctor(On.Celeste.Player.orig_ctor orig, Player self, Vector2 position, PlayerSpriteMode spriteMode)
    {
        orig(self, position, spriteMode);
        HasSeekerDash = seekerDashAttacking = seekerDashLaunched = launchPossible = false;
    }

    private static void IL_Player_ctor(ILContext il)
    {
        ILCursor cursor = new(il);

        cursor.GotoNext(MoveType.After, instr => instr.MatchStfld<Player>(nameof(Player.Sprite)));
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Action<Player>>(player => player.Add(new PlayerSeekerHair()));
    }

    private static void Player_TransitionTo(ILContext il)
    {
        ILCursor cursor = new(il);

        cursor.GotoNext(MoveType.After, instr => instr.MatchCallvirt<Player>(nameof(Player.UpdateHair)));
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Action<Player>>(player =>
        {
            player.Get<PlayerSeekerHair>()?.AfterUpdate(motion: false);
        });
    }

    // Prevent dash if HasSeekerDash and inside SeekerBarrier
    private static bool Player_get_CanDash(Func<Player, bool> orig, Player self)
    {
        if (!orig(self))
            return false;

        if (HasSeekerDash)
        {
            foreach (Entity entity in self.Scene.Tracker.GetEntities<SeekerBarrier>())
            {
                bool collidable = entity.Collidable;
                entity.Collidable = true;
                if (self.CollideCheck(entity))
                {
                    // Tiny bit of feel-good leniency
                    Vector2 aim = self.GetData().Get<Vector2>("lastAim").Sign() * 5;
                    if (!self.CollideCheck(entity, self.Position + aim))
                    {
                        entity.Collidable = collidable;
                        self.MoveHExact((int) aim.X);
                        self.MoveVExact((int) aim.Y);
                        return true;
                    }

                    entity.Collidable = collidable;
                    self.Add(new Coroutine(self.TrappedDash(entity, () => { HasSeekerDash = false; })));
                    return false;
                }
                entity.Collidable = collidable;
            }
        }

        return true;
    }

    // Trigger seekerdash if possible
    private static void Player_DashBegin(On.Celeste.Player.orig_DashBegin orig, Player self)
    {
        orig(self);
        if (HasSeekerDash)
        {
            seekerDashAttacking = true;
            seekerDashTimer = self.GetData().Get<float>("dashAttackTimer");
            HasSeekerDash = false;
        }
    }

    // Replace dash particles
    private static void Player_DashUpdate(ILContext il)
    {
        ILCursor cursor = new(il);
        cursor.GotoNext(MoveType.After, instr => instr.MatchLdsfld<Player>("P_DashB"));
        object loc = cursor.Next.Operand;
        cursor.GotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Ldloc_S && instr.Operand == loc);
        cursor.EmitDelegate<Func<ParticleType, ParticleType>>(type => seekerDashAttacking ? Seeker.P_Attack : type);
    }

    private static void Player_DashEnd(On.Celeste.Player.orig_DashEnd orig, Player self)
    {
        orig(self);
        launchPossible = false;
    }

    // Replace dash sound
    private static void Player_CallDashEvents(ILContext il)
    {
        ILCursor cursor = new(il);
        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr(out string str) && !str.EndsWith("gen")))
        {
            cursor.EmitDelegate<Func<string, string>>(str => seekerDashAttacking ? SFX.game_05_seeker_dash : str);
        }
    }

    // Make SeekerBarriers collidable if seekerDashAttacking, handle cooldowns
    private static void Player_Update(On.Celeste.Player.orig_Update orig, Player self)
    {
        static bool ShouldBeCollidable(SeekerBarrier barrier)
        {
            if (barrier is PlayerSeekerBarrier playerBarrier)
            {
                bool seesBarriers = HasSeekerDash || SeekerAttacking;
                return seesBarriers || playerBarrier.WavedashTime > 0;
            }
            return SeekerAttacking;
        }

        self.Scene.Tracker.GetEntities<SeekerBarrier>().ForEach(e => e.Collidable = ShouldBeCollidable((SeekerBarrier) e));

        self.CollideFirst<PlayerSeekerBarrier>()?.MakeGroupUncollidable();

        orig(self);

        self.Scene.Tracker.GetEntities<SeekerBarrier>().ForEach(e => e.Collidable = false);

        DynamicData playerData = self.GetData();

        // launchPossible is a bad hack to fix wavedashing
        bool resetLaunchPossible = false;
        if ((seekerDashTimer > 0 && playerData.Get<bool>("launched")) || launchPossible)
        {
            seekerDashLaunched = true;
            resetLaunchPossible = true;
        }

        float dashAttackTimer = playerData.Get<float>("dashAttackTimer");
        if (dashAttackTimer < seekerDashTimer)
            seekerDashTimer = dashAttackTimer;
        else if (seekerDashTimer > 0)
            seekerDashTimer -= Engine.DeltaTime;

        if (seekerDashTimer <= 0f)
            seekerDashAttacking = false;

        if (seekerDashLaunched && !playerData.Get<bool>("launched") && !launchPossible)
            seekerDashLaunched = false;

        if (resetLaunchPossible)
            launchPossible = false;

        if (HasSeekerDash)
            self.Sprite.Color = Seeker.TrailColor;
        else if (self.Sprite.Color == Seeker.TrailColor)
            self.Sprite.Color = Color.White;
    }

    // Handle solid collisions
    private static void Player_OnCollideH(On.Celeste.Player.orig_OnCollideH orig, Player self, CollisionData data)
    {
        orig(self, data);

        if (SeekerAttacking && data.Hit is not SeekerBarrier)
        { // SeekerBarriers handled elsewhere
            float direction;
            float x;
            if (data.Direction.X > 0f)
            {
                direction = Calc.HalfCircle;
                x = self.Right;
            }
            else
            {
                direction = 0f;
                x = self.Left;
            }
            self.SceneAs<Level>().Particles.Emit(Seeker.P_HitWall, 12, new Vector2(x, self.Y), Vector2.UnitY * 4f, direction);
            Audio.Play(SFX.game_05_seeker_impact_normal, self.Position);
        }
    }

    // Handle solid collisions
    private static void Player_OnCollideV(On.Celeste.Player.orig_OnCollideV orig, Player self, CollisionData data)
    {
        orig(self, data);

        if (SeekerAttacking && data.Hit is not SeekerBarrier)
        { // SeekerBarriers handled elsewhere
            launchPossible = true;
            float direction;
            float y;
            if (data.Direction.Y > 0f)
            {
                direction = -Calc.QuarterCircle;
                y = self.Bottom;
            }
            else
            {
                direction = Calc.QuarterCircle;
                y = self.Top;
            }
            self.SceneAs<Level>().Particles.Emit(Seeker.P_HitWall, 12, new Vector2(self.X, y), Vector2.UnitX * 4f, direction);
            Audio.Play(SFX.game_05_seeker_impact_normal, self.Position);
        }
    }

    // Replace trail color
    private static Color Player_GetCurrentTrailColor(On.Celeste.Player.orig_GetCurrentTrailColor orig, Player self)
    {
        return SeekerAttacking || seekerDashTimer > 0 ? Seeker.TrailColor : orig(self);
    }

    // Break TempleCrackedBlocks if SeekerAttacking
    private static void TempleCrackedBlock_ctor_EntityID_Vector2_float_float_bool(On.Celeste.TempleCrackedBlock.orig_ctor_EntityID_Vector2_float_float_bool orig, TempleCrackedBlock self, EntityID eid, Vector2 position, float width, float height, bool persistent)
    {
        orig(self, eid, position, width, height, persistent);
        self.OnDashCollide = new DashCollision((player, dir) =>
        {
            if (SeekerAttacking)
            {
                self.Break(player.Center);
                return DashCollisionResults.Rebound;
            }
            return DashCollisionResults.NormalCollision;
        });
    }

    // Break ALL DashBlocks if SeekerAttacking
    private static DashCollisionResults DashBlock_OnDashed(On.Celeste.DashBlock.orig_OnDashed orig, DashBlock self, Player player, Vector2 direction)
    {
        if (SeekerAttacking)
        {
            self.Break(player.Center, direction, true);
            return DashCollisionResults.Rebound;
        }
        return orig(self, player, direction);
    }

    // Add DashCollision component to SeekerBarriers for seekerDashAttacking
    private static void SeekerBarrier_ctor_Vector2_float_float(On.Celeste.SeekerBarrier.orig_ctor_Vector2_float_float orig, SeekerBarrier self, Vector2 position, float width, float height)
    {
        orig(self, position, width, height);

        self.OnDashCollide = new DashCollision((player, dir) =>
        {
            // Allow for more lenient wallbounces against seeker barriers
            if ((player.Left >= self.Right - 5f || player.Right < self.Left + 5f) && Math.Abs(dir.Y) == 1)
                return DashCollisionResults.NormalCollision;

            // Allow wavedashes
            if (self is PlayerSeekerBarrier barrier && dir.Y > 0)
            {
                barrier.WavedashTime = PlayerSeekerBarrier.WavedashLeniencyTimer;
                return DashCollisionResults.NormalCollision;
            }

            if (seekerDashAttacking)
            {
                Vector2 origin = dir.X > 0 ? player.CenterRight : dir.X < 0 ? player.CenterLeft : dir.Y > 0 ? player.BottomCenter : player.TopCenter;
                self.SceneAs<Level>().Particles.Emit(Seeker.P_HitWall, 12, origin, new Vector2(dir.Y, dir.X) * 4f, (-dir).Angle());
                self.OnReflectSeeker();
                Audio.Play(SFX.game_05_seeker_impact_lightwall, self.Position);

                // Rare case where the player hits the seeker barrier before colliding with the spikes.
                // When this happens, the player is sent back and doesn't collide with the spikes, and doesn't die.
                if (self is PlayerSeekerBarrier playerSeekerBarrier)
                {
                    // Check if the player is dashing up and the barrier has down spikes.
                    bool up = dir.Y < 0 && playerSeekerBarrier.TileSpikes.HasFlag(Tiling.SpikeDown);
                    // Check if the player is dashing down and the barrier has up spikes.
                    bool down = dir.Y > 0 && playerSeekerBarrier.TileSpikes.HasFlag(Tiling.SpikeUp);
                    // Check if the player is dashing left and the barrier has right spikes.
                    bool left = dir.X < 0 && playerSeekerBarrier.TileSpikes.HasFlag(Tiling.SpikeRight);
                    // Check if the player is dashing right and the barrier has left spikes.
                    bool right = dir.X > 0 && playerSeekerBarrier.TileSpikes.HasFlag(Tiling.SpikeLeft);
                    // If any of these is true, then the player could be touching spikes, let's not bounce out of them.
                    if (up || down || left || right)
                        return DashCollisionResults.NormalOverride;
                }

                return DashCollisionResults.Bounce;
            }
            return DashCollisionResults.NormalOverride;
        });
    }

    // Kill the seeker if player was launched from seekerdash
    private static void Seeker_OnBouncePlayer(On.Celeste.Seeker.orig_OnBouncePlayer orig, Seeker self, Player player)
    {
        if (seekerDashLaunched && !(bool) f_Seeker_dead.GetValue(self))
        {
            self.Killed(player);
            player.Bounce(Math.Min(player.Y, self.CenterY));
            return;
        }
        orig(self, player);
    }

    // Bop the seeker if SeekerAttacking, kill the seeker if player was launched from seekerdash
    private static void Seeker_OnAttackPlayer(On.Celeste.Seeker.orig_OnAttackPlayer orig, Seeker self, Player player)
    {
        DynamicData seekerData = DynamicData.For(self);
        int state = seekerData.Get<StateMachine>("State").State;
        if (SeekerAttacking)
        {
            if (seekerDashLaunched && !seekerData.Get<bool>("dead"))
            {
                self.Killed(player);
                player.Bounce(Math.Min(player.Y, self.CenterY));
                return;
            }

            if (state != 4)
            {
                player.Bounce(self.Top);
                m_Seeker_GotBouncedOn.Invoke(self, new object[] { player });
            }
            return;
        }

        orig(self, player);
    }

    // Bop Oshiro if SeekerAttacking
    private static void AngryOshiro_OnPlayer(On.Celeste.AngryOshiro.orig_OnPlayer orig, AngryOshiro self, Player player)
    {
        if (SeekerAttacking)
        {
            DynamicData oshiroData = DynamicData.For(self);
            Audio.Play(SFX.game_gen_thing_booped, self.Position);
            Celeste.Freeze(0.2f);
            player.Bounce(player.CenterY + 2f);
            oshiroData.Get<StateMachine>("state").State = 5;
            oshiroData.Get<SoundSource>("prechargeSfx").Stop();
            oshiroData.Get<SoundSource>("chargeSfx").Stop();
            return;
        }

        orig(self, player);
    }

    #endregion

    #region Extensions 

    /// <summary>
    /// Kill the seeker, with added <see cref="SlashFx"/> and <see cref="Celeste.Freeze(float)"/>.<br/>
    /// Does not check seeker status.
    /// </summary>
    /// <param name="seeker"></param>
    /// <param name="by">Actor that killed the seeker.</param>
    private static void Killed(this Seeker seeker, Actor by)
    {
        SlashFx.Burst(seeker.Center, Calc.Angle(by.Center, seeker.Center));
        Celeste.Freeze(0.08f);

        Entity entity = new(seeker.Position);
        DeathEffect deathEffect = new(Color.HotPink, new Vector2?(seeker.Center - seeker.Position))
        {
            OnEnd = entity.RemoveSelf
        };
        entity.Add(deathEffect);
        entity.Depth = Depths.Top;
        seeker.Scene.Add(entity);

        Audio.Play(SFX.game_05_seeker_death, seeker.Position);
        seeker.RemoveSelf();
        f_Seeker_dead.SetValue(seeker, true);
    }

    /// <summary>
    /// Perform a "dash" that does not move the player or set the player state to StDash
    /// </summary>
    /// <param name="player"></param>
    /// <param name="barrier">The entity trapping the player.</param>
    /// <param name="onEnd">Action to perform on completion.</param>
    private static IEnumerator TrappedDash(this Player player, Entity barrier, Action onEnd = null)
    {
        DynamicData playerData = player.GetData();

        player.Dashes = Math.Max(0, player.Dashes - 1);
        Input.Dash.ConsumeBuffer();

        if (Engine.TimeRate > 0.25f)
        {
            Celeste.Freeze(0.05f);
        }

        playerData.Set("dashCooldownTimer", 0.2f);
        Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
        player.Speed = -Vector2.UnitY * 5f;
        yield return null;

        if (barrier is SeekerBarrier seekerBarrier)
        {
            seekerBarrier.OnReflectSeeker();
            Audio.Play(SFX.game_05_seeker_impact_lightwall, player.Position);
        }
        DisplacementRenderer.Burst burst = barrier.SceneAs<Level>().Displacement.AddBurst(player.Center, 0.8f, 8f, 64f, 0.5f, Ease.QuadOut, Ease.QuadOut);
        burst.WorldClipCollider = barrier.Collider;

        player.DashDir = (Vector2) m_Player_CorrectDashPrecision.Invoke(player, new object[] { playerData.Get<Vector2>("lastAim") });

        player.SceneAs<Level>().DirectionalShake(player.DashDir, 0.2f);
        m_Player_CallDashEvents.Invoke(player, null);

        if (player.StateMachine.PreviousState == 19)
        {
            player.SceneAs<Level>().Particles.Emit(FlyFeather.P_Boost, 12, player.Center, Vector2.One * 4f, (-player.DashDir).Angle());
        }
        player.CreateTrail(Seeker.TrailColor);
        yield return 0.15f;

        onEnd?.Invoke();
        player.StateMachine.State = Player.StNormal;
    }

    #endregion

}
