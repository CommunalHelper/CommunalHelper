using Celeste.Mod.CommunalHelper.Entities;
using FMOD.Studio;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using System.Collections;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.States;

public static class Elytra
{
    public static ParticleType P_Deploy { get; private set; }

    private const string f_Player_elytraGlideAngle  = nameof(f_Player_elytraGlideAngle);    // float
    private const string f_Player_elytraGlideSpeed  = nameof(f_Player_elytraGlideSpeed);    // float
    private const string f_Player_elytraGlideFacing = nameof(f_Player_elytraGlideFacing);   // Facings
    private const string f_Player_elytraGlideSfx    = nameof(f_Player_elytraGlideSfx);      // EventInstance
    private const string f_Player_elytraDashes      = nameof(f_Player_elytraDashes);        // int
    private const string f_Player_elytraPrevPos     = nameof(f_Player_elytraPrevPos);       // Vector2
    private const string f_Player_elytraStableTimer = nameof(f_Player_elytraStableTimer);   // float

    private const float STABLE_ANGLE = 0.2f;
    private const float ANGLE_RANGE = 2f;
    private const float MIN_SPEED = 64f;
    private const float MAX_SPEED = 320f;
    private const float ACCEL = 90f;
    private const float DECEL = 165f;
    private const float FAST_DECEL = 220f;
    private const float MAX_ANGLE_CHANGE_INV_SPEED_FACTOR = 480f;
    private const float SPEED_FACING_THRESHOLD = Player.MaxRun + 60f;

    private const string ELYTRA_ANIM = "anim_player_elytra_fly";

    public static void GlideBegin(this Player player)
    {
        DynamicData data = DynamicData.For(player);

        const float squish = 0.4f;
        player.Sprite.Scale.X = 1.0f + squish;
        player.Sprite.Scale.Y = 1.0f - squish;
        player.OverrideHairColor = Player.FlashHairColor;

        // if the horizontal speed is lower than a certain threshold, let the player facing dictate the direction of the gliding
        // otherwise, the gliding facing is determined by the sign of the speed (even if the player is facing the other way).
        Facings facing = player.Facing;
        if (player.Speed.X > SPEED_FACING_THRESHOLD)
            facing = (Facings)Math.Sign(player.Speed.X);
        data.Set(f_Player_elytraGlideFacing, facing);

        // get fliped speed if facing left
        Vector2 speed = facing == Facings.Right
            ? player.Speed
            : new Vector2(-player.Speed.X, player.Speed.Y);

        float angle = speed.Angle();
        float length = speed.Length();

        data.Set(f_Player_elytraPrevPos, player.Center);
        data.Set(f_Player_elytraStableTimer, 0f);

        // set "polar" speed (angle and magnitude)
        data.Set(f_Player_elytraGlideAngle, angle);
        data.Set(f_Player_elytraGlideSpeed, length);

        data.Set(f_Player_elytraGlideSfx, Audio.Play(CustomSFX.game_elytra_gliding));

        Level level = player.Scene as Level;
        level.DirectionalShake(Calc.AngleToVector(angle, 1.0f), 0.1f);

        angle = ClampGlideAngle(angle);
        float direction = facing == Facings.Right
            ? angle
            : MathHelper.Pi - angle;
        for (int _ = 0; _ < 10; _++)
        {
            float deviation = Calc.Random.NextFloat(2.0f) - 1.0f;
            level.ParticlesBG.Emit(P_Deploy, 1, player.Center, Vector2.One * 2, Color.White, direction + deviation * 0.1f);
        }

        Audio.Play(CustomSFX.game_elytra_deploy);
    }

    public static void GlideEnd(this Player player)
    {
        DynamicData data = DynamicData.For(player);
        EventInstance sfx = data.Get<EventInstance>(f_Player_elytraGlideSfx);
        if (sfx is not null)
            Audio.Stop(sfx);
    }

    public static int GlideUpdate(this Player player)
    {
        if (player.OnGround())
        {
            bool maintain = false;

            Vector2 at = player.Position + Vector2.UnitY;
            foreach (ElytraCollision component in player.CollideAllByComponent<ElytraCollision>(at))
                if (component.Callback is not null)
                    maintain |= component.Callback(player) == ElytraCollision.Result.Maintain;

            if (!maintain)
                return Player.StNormal;
        }

        if (player.ClimbCheck((int)player.Facing))
        {
            bool maintain = false;

            Vector2 at = player.Position + Vector2.UnitX * (int)player.Facing * 2;
            foreach (ElytraCollision component in player.CollideAllByComponent<ElytraCollision>(at))
                if (component.Callback is not null)
                    maintain |= component.Callback(player) == ElytraCollision.Result.Maintain;

            if (!maintain)
                return Player.StClimb;
        }

        if (player.CanDash)
            return player.StartDash();

        // released elytra binding
        if (!CommunalHelperModule.Settings.DeployElytra.Check)
            return Player.StNormal;

        DynamicData data = DynamicData.For(player);

        // get previous speed and angle values, and other stuff.
        float halfRange = ANGLE_RANGE / 2f;
        float oldAngle, newAngle;
        float oldSpeed, newSpeed;
        oldAngle = data.Get<float>(f_Player_elytraGlideAngle);
        oldSpeed = Calc.Max(data.Get<float>(f_Player_elytraGlideSpeed), MIN_SPEED);
        // we're not putting the speed below the maximum gliding speed, because we want to accept high speeds on state entry
        // decelerating then reaccelerating will instead make you unable to go past the maximum speed.

        // the rate of change of the angle depends on the speed:
        // the faster you go, the slower you can steer.
        // this was needed because you could go at very high speeds and brutally change your direction,
        // which was unrealistic and not fun to play with.
        // it now allows to read what's ahead, and helps with more precise movement.
        float angleMaxChange = Engine.DeltaTime * MAX_ANGLE_CHANGE_INV_SPEED_FACTOR / oldSpeed;

        // retrieve stable timer value.
        float stableTimer = data.Get<float>(f_Player_elytraStableTimer);

        if (stableTimer > 0.0f)
        {
            // if the stable timer is still going, then the player is considered "stable",
            // so we don't update its angle.
            newAngle = oldAngle;
        }
        else
        {
            // determine new angle :
            if (oldSpeed == MIN_SPEED && Input.Feather.Value.Y < 0f)
            {
                // force middle angle if gliding up but too slow. 
                newAngle = Calc.Approach(oldAngle, STABLE_ANGLE, angleMaxChange);
            }
            else
            {
                // new angle is controlled by the player.
                float target = STABLE_ANGLE + halfRange * Input.Feather.Value.Y;
                newAngle = Calc.Approach(oldAngle, target, angleMaxChange);
            }
            // clamp angle in case the player entered this state (almost) completely vertically.
            newAngle = Calc.Clamp(newAngle, STABLE_ANGLE - halfRange, STABLE_ANGLE + halfRange);
        }

        // decrement stable timer.
        stableTimer = Calc.Approach(stableTimer, 0f, Engine.DeltaTime);
        data.Set(f_Player_elytraStableTimer, stableTimer);

        // absolute input value will help determine how much we should speed up / slow down.
        float absYInput = Math.Abs(Input.Feather.Value.Y);

        // determine new speed :
        newSpeed = oldSpeed;
        if (newAngle < STABLE_ANGLE)
        {
            // going above middle angle, slow down.
            // if the player goes at a higher speed than the maximum speed, decelerate faster.
            float decel = oldSpeed > MAX_SPEED
                ? FAST_DECEL
                : DECEL;
            newSpeed = Calc.Approach(oldSpeed, MIN_SPEED, Engine.DeltaTime * decel * absYInput);
        }
        else if (newAngle > STABLE_ANGLE)
        {
            // speed up, relative to how vertical the player's input is.

            // if the speed is already greater than the max diving speed, then don't change it.
            // in that case, it's only going to decrease if the player decides to glide up.
            if (oldSpeed < MAX_SPEED)
                newSpeed = Calc.Approach(oldSpeed, MAX_SPEED, Engine.DeltaTime * ACCEL * absYInput);
        }

        // update new values.
        data.Set(f_Player_elytraGlideAngle, newAngle);
        data.Set(f_Player_elytraGlideSpeed, newSpeed);

        // get player elytra facing.
        Facings facing = data.Get<Facings>(f_Player_elytraGlideFacing);

        player.Facing = facing;

        // we were executing all the above code regardless of the player's facing, so we can reverse the speed if we need to.
        player.Speed = Calc.AngleToVector(newAngle, newSpeed);
        if (facing == Facings.Left)
            player.Speed.X *= -1;

        Vector2 oldPosition = data.Get<Vector2>(f_Player_elytraPrevPos);
        data.Set(f_Player_elytraPrevPos, player.Center);

        player.CheckRingTraversal(oldPosition);

        // sound stuff.
        EventInstance sfx = data.Get<EventInstance>(f_Player_elytraGlideSfx);
        if (sfx is not null)
        {
            Audio.SetParameter(sfx, "speed", Calc.ClampedMap(newSpeed, MIN_SPEED, MAX_SPEED));
            Audio.SetParameter(sfx, "straight_wings", 1 - Math.Abs((newAngle - STABLE_ANGLE) / halfRange));
        }

        // allows the state to be changed during the gliding update (for instance, by entity interaction).
        // most of the time, this should be St.Elytra.
        return player.StateMachine.State;
    }

    private static void CheckRingTraversal(this Player player, Vector2 from)
    {
        Vector2 at = player.Center;
        var rings = player.Scene.Tracker.GetEntities<ElytraRing>()
                                        .Cast<ElytraRing>()
                                        .Where(ring => ring.CanTraverse(from, at))
                                        .OrderBy(ring => ring.PreserveTraversalOrder ? Vector2.DistanceSquared(from, ring.Position) : -1.0f);
        
        foreach (ElytraRing ring in rings)
            ring.OnPlayerTraversal(player);
    }

    public static IEnumerator GlideRoutine(this Player _)
    {
        yield return null;
    }

    public static void RefillElytra(this Player player)
    {
        DynamicData data = DynamicData.For(player);
        int elytraDashes = data.Get<int>(f_Player_elytraDashes);
        if (elytraDashes < 1)
        {
            data.Set(f_Player_elytraDashes, 1);
            player.FlashHair();
            Audio.Play(CustomSFX.game_elytra_refill, player.Center);
        }
    }

    public static bool CanRefillElytra(this Player player)
    {
        DynamicData data = DynamicData.For(player);
        return data.Get<int>(f_Player_elytraDashes) < 1;
    }

    public static void ElytraLaunch(this Player player, Vector2 speed, float duration = 0.5f)
    {
        if (speed == Vector2.Zero)
            return;

        int sign = Math.Sign(speed.X);
        speed.X = Math.Abs(speed.X);

        float angle = Calc.Clamp(speed.Angle(), -MathHelper.PiOver2, MathHelper.PiOver2);
        float length = speed.Length();

        DynamicData data = DynamicData.For(player);
        data.Set(f_Player_elytraGlideFacing, player.Facing = (Facings)sign);
        data.Set(f_Player_elytraGlideAngle, angle);
        data.Set(f_Player_elytraGlideSpeed, length);
        data.Set(f_Player_elytraStableTimer, duration);
    }

    private static float ClampGlideAngle(float angle)
        => Calc.Clamp(angle, STABLE_ANGLE - ANGLE_RANGE / 2f, STABLE_ANGLE);

    internal static void Initialize()
    {
        P_Deploy = new(ParticleTypes.Chimney)
        {
            LifeMin = 1f,
            LifeMax = 3f,
            SizeRange = 0.8f,
            Acceleration = Vector2.UnitY,
            SpeedMin = 10f,
            SpeedMax = 60f,
            DirectionRange = MathHelper.PiOver4,
        };
    }

    internal static void Load()
    {
        On.Celeste.Player.Die += Mod_Player_Die;
        On.Celeste.PlayerSprite.ctor += Mod_PlayerSprite_ctor;
        On.Celeste.Player.UpdateHair += Player_UpdateHair;
        On.Celeste.Player.UpdateSprite += Mod_Player_UpdateSprite;
        On.Celeste.Player.NormalUpdate += Mod_Player_NormalUpdate;
        On.Celeste.Player.OnCollideH += Mod_Player_OnCollideH;
        On.Celeste.Player.OnCollideV += Mod_Player_OnCollideV;
        On.Celeste.Player.ctor += Mod_Player_ctor;
        On.Celeste.Player.RefillDash += Player_RefillDash;
        IL.Celeste.Player.UseRefill += Player_UseRefill;
    }

    internal static void Unload()
    {
        On.Celeste.Player.Die -= Mod_Player_Die;
        On.Celeste.PlayerSprite.ctor -= Mod_PlayerSprite_ctor;
        On.Celeste.Player.UpdateHair -= Player_UpdateHair;
        On.Celeste.Player.UpdateSprite -= Mod_Player_UpdateSprite;
        On.Celeste.Player.NormalUpdate -= Mod_Player_NormalUpdate;
        On.Celeste.Player.OnCollideH -= Mod_Player_OnCollideH;
        On.Celeste.Player.OnCollideV -= Mod_Player_OnCollideV;
        On.Celeste.Player.ctor -= Mod_Player_ctor;
        On.Celeste.Player.RefillDash -= Player_RefillDash;
        IL.Celeste.Player.UseRefill -= Player_UseRefill;
    }

    private static void Player_UseRefill(ILContext il)
    {
        ILCursor cursor = new(il);

        cursor.GotoNext(instr => instr.MatchCallvirt<Player>(nameof(Player.RefillStamina)));

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate(RefillElytra);

        cursor.GotoPrev(instr => instr.MatchStfld<Player>(nameof(Player.Dashes)));
        cursor.GotoPrev(instr => instr.MatchLdarg(0));

        ILLabel label = cursor.MarkLabel();

        cursor.GotoPrev(instr => instr.MatchLdfld<Player>(nameof(Player.Stamina)));
        cursor.GotoPrev(instr => instr.MatchLdarg(0));

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate(CanRefillElytra);
        cursor.Emit(OpCodes.Brtrue_S, label);
    }

    private static bool Player_RefillDash(On.Celeste.Player.orig_RefillDash orig, Player self)
    {
        self.RefillElytra();
        return orig(self);
    }

    private static void Player_UpdateHair(On.Celeste.Player.orig_UpdateHair orig, Player self, bool applyGravity)
    {
        orig(self, applyGravity);
        
        DynamicData data = DynamicData.For(self);
        int elytraDashes = data.Get<int>(f_Player_elytraDashes);
        if (elytraDashes < 1)
        {
            Color color = self.Sprite.Mode == PlayerSpriteMode.Badeline
                ? Player.UsedBadelineHairColor
                : Player.UsedHairColor;
            self.OverrideHairColor = Color.Lerp(self.Hair.Color, color, 6f * Engine.DeltaTime);
        }
        else
            self.OverrideHairColor = null;
    }

    private static void Mod_Player_OnCollideH(On.Celeste.Player.orig_OnCollideH orig, Player self, CollisionData data)
    {
        if (self.StateMachine.State == St.Elytra)
        {
            ElytraCollision component = data.Hit.Get<ElytraCollision>();
            component?.Callback?.Invoke(self);
            return;
        }

        orig(self, data);
    }

    private static void Mod_Player_OnCollideV(On.Celeste.Player.orig_OnCollideV orig, Player self, CollisionData data)
    {
        if (self.StateMachine.State == St.Elytra)
        {
            ElytraCollision component = data.Hit.Get<ElytraCollision>();
            component?.Callback?.Invoke(self);
            return;
        }

        orig(self, data);
    }

    private static void Mod_Player_ctor(On.Celeste.Player.orig_ctor orig, Player self, Vector2 position, PlayerSpriteMode spriteMode)
    {
        orig(self, position, spriteMode);

        DynamicData data = DynamicData.For(self);
        data.Set(f_Player_elytraDashes, 1);
    }

    private static int Mod_Player_NormalUpdate(On.Celeste.Player.orig_NormalUpdate orig, Player self)
    {
        int next = orig(self);

        if (self.OnGround())
        {
            self.RefillElytra();
        }
        else
        {
            if (CommunalHelperModule.Session.CanDeployElytra && CommunalHelperModule.Settings.DeployElytra.Pressed)
            {
                CommunalHelperModule.Settings.DeployElytra.ConsumePress();
                
                DynamicData data = DynamicData.For(self);
                int elytraDashes = data.Get<int>(f_Player_elytraDashes);
                if (elytraDashes > 0)
                {
                    data.Set(f_Player_elytraDashes, --elytraDashes);
                    return St.Elytra;
                }
            }
        }

        return next;
    }

    private static PlayerDeadBody Mod_Player_Die(On.Celeste.Player.orig_Die orig, Player self, Vector2 direction, bool evenIfInvincible, bool registerDeathInStats)
    {
        DynamicData data = DynamicData.For(self);
        if (data.Data.TryGetValue(f_Player_elytraGlideSfx, out var value) && value is EventInstance eventInstance)
            Audio.Stop(eventInstance);

        return orig(self, direction, evenIfInvincible, registerDeathInStats);
    }

    private static void Mod_PlayerSprite_ctor(On.Celeste.PlayerSprite.orig_ctor orig, PlayerSprite self, PlayerSpriteMode mode)
    {
        orig(self, mode);

        self.Animations[ELYTRA_ANIM] = new()
        {
            Frames = GFX.Game.GetAtlasSubtextures("characters/player_no_backpack/CommunalHelper/fly").ToArray(),
            Delay = 10f,
        };
    }   

    private static void Mod_Player_UpdateSprite(On.Celeste.Player.orig_UpdateSprite orig, Player self)
    {
        orig(self);

        if (self.StateMachine.State != St.Elytra)
            return;

        const int STABLE_FRAME = 6;
        const int FRAME_COUNT = 9;

        self.Sprite.Play(ELYTRA_ANIM);

        DynamicData data = DynamicData.For(self);
        int frame = STABLE_FRAME;
        if (data.Data.TryGetValue(f_Player_elytraGlideAngle, out var value))
        {
            float angle = (float)value;
            float t = (angle - STABLE_ANGLE) / (ANGLE_RANGE / 2f);
            if (t < 0)
                frame -= (int)(t * (FRAME_COUNT - STABLE_FRAME - 1));
            else
                frame -= (int)(t * STABLE_FRAME);
        }

        self.Sprite.SetAnimationFrame(Calc.Clamp(frame, 0, FRAME_COUNT - 1));
    }
}
