using FMOD.Studio;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Entities {
    public static class ElytraState {
        // PlayerSprite.FrameMetadata is never set somewhere else, except when initialized, so keeping the instance here should be okay.
        private static readonly Dictionary<string, PlayerAnimMetadata> PlayerSprite_FrameMetadata
            = (Dictionary<string, PlayerAnimMetadata>) typeof(PlayerSprite).GetField("FrameMetadata", BindingFlags.Static | BindingFlags.NonPublic)
                                                                           .GetValue(null);

        public static int St { get; private set; } = -1;

        private const string CommunalHelper_Player_glideAngle           = "CommunalHelper_Player_glideAngle"; // float
        private const string CommunalHelper_Player_glideSpeed           = "CommunalHelper_Player_glideSpeed"; // float
        private const string CommunalHelper_Player_glideFacing          = "CommunalHelper_Player_glideFacing"; // Facings
        private const string CommunalHelper_Player_glideSfx             = "CommunalHelper_Player_glideSfx"; // EventInstance
        private const string CommunalHelper_Player_glideCanTightenWings = "CommunalHelper_Player_glideCanTightenWings"; // bool

        private const float GlidingStableAngle                  = 0.2f;
        private const float GlidingAngleRange                   = 2f;
        private const float GlidingMinimumSpeed                 = 64f;
        private const float GlidingMaximumSpeed                 = 320f;
        private const float GlidingAcceleration                 = 90f;
        private const float GlidingDeceleration                 = 145f;
        private const float GlidingFastDeceleration             = 220f;
        private const float GlidingInverseSpeedMultAngleFactor  = 480f;

        private const string GlidingAnimation = "CommunalHelper_fly";

        internal static void Load() {
            On.Celeste.Player.ctor += Mod_Player_ctor;

            On.Celeste.Player.Die += Mod_Player_Die;
            On.Celeste.PlayerSprite.ctor += Mod_PlayerSprite_ctor;
            On.Celeste.PlayerSprite.CreateFramesMetadata += PlayerSprite_CreateFramesMetadata;
            On.Celeste.Player.UpdateSprite += Mod_Player_UpdateSprite;

            IL.Celeste.PlayerHair.AfterUpdate += PlayerHair_AfterUpdate;
        }

        internal static void Unload() {
            On.Celeste.Player.ctor -= Mod_Player_ctor;

            On.Celeste.Player.Die -= Mod_Player_Die;
            On.Celeste.PlayerSprite.ctor -= Mod_PlayerSprite_ctor;
            On.Celeste.PlayerSprite.CreateFramesMetadata -= PlayerSprite_CreateFramesMetadata;
            On.Celeste.Player.UpdateSprite -= Mod_Player_UpdateSprite;

            IL.Celeste.PlayerHair.AfterUpdate -= PlayerHair_AfterUpdate;
        }

        private static void PlayerHair_AfterUpdate(ILContext il) {
            ILCursor cursor = new(il);

            cursor.GotoNext(instr => instr.MatchLdloc(0));
            cursor.GotoNext(MoveType.Before, instr => instr.OpCode == OpCodes.Callvirt);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Vector2, PlayerHair, Vector2>>((pos, hair) => {
                Player player = hair.Entity as Player;
                if (player.StateMachine.State != St)
                    return pos;

                PlayerSprite sprite = player.Sprite;
                Vector2 origin = sprite.RenderPosition;
                return (pos - origin).Rotate(sprite.Rotation) + origin;
            });
        }

        private static void PlayerSprite_CreateFramesMetadata(On.Celeste.PlayerSprite.orig_CreateFramesMetadata orig, string sprite) {
            orig(sprite);

            // hair="4,0|4,-1|3,-1|2,-1|2,-1|2,-1"
            PlayerSprite_FrameMetadata[$"characters/{sprite}/CommunalHelper/fly00"] = new() { HasHair = true, HairOffset = new(4,  0), };
            PlayerSprite_FrameMetadata[$"characters/{sprite}/CommunalHelper/fly01"] = new() { HasHair = true, HairOffset = new(4, -1), };
            PlayerSprite_FrameMetadata[$"characters/{sprite}/CommunalHelper/fly02"] = new() { HasHair = true, HairOffset = new(3, -1), };
            PlayerSprite_FrameMetadata[$"characters/{sprite}/CommunalHelper/fly03"] = new() { HasHair = true, HairOffset = new(2, -1), };
            PlayerSprite_FrameMetadata[$"characters/{sprite}/CommunalHelper/fly04"] = new() { HasHair = true, HairOffset = new(2, -1), };
            PlayerSprite_FrameMetadata[$"characters/{sprite}/CommunalHelper/fly05"] = new() { HasHair = true, HairOffset = new(2, -1), };
        }

        private static void Mod_Player_ctor(On.Celeste.Player.orig_ctor orig, Player self, Vector2 position, PlayerSpriteMode spriteMode) {
            orig(self, position, spriteMode);

            St = self.StateMachine.AddState(self.GlideUpdate, self.GlideRoutine, self.GlideBegin, self.GlideEnd);
        }

        private static PlayerDeadBody Mod_Player_Die(On.Celeste.Player.orig_Die orig, Player self, Vector2 direction, bool evenIfInvincible, bool registerDeathInStats) {
            DynData<Player> data = self.GetData();
            if (data.Data.TryGetValue(CommunalHelper_Player_glideSfx, out object value) && value is EventInstance eventInstance)
                Audio.Stop(eventInstance);

            return orig(self, direction, evenIfInvincible, registerDeathInStats);
        }

        private static void Mod_PlayerSprite_ctor(On.Celeste.PlayerSprite.orig_ctor orig, PlayerSprite self, PlayerSpriteMode mode) {
            orig(self, mode);

            self.Animations[GlidingAnimation] = new() {
                Frames = GFX.Game.GetAtlasSubtextures("characters/player_no_backpack/CommunalHelper/fly").ToArray(),
                Delay = 10f,
            };
        }

        private static void Mod_Player_UpdateSprite(On.Celeste.Player.orig_UpdateSprite orig, Player self) {
            orig(self);

            if (self.StateMachine.State == St) {
                self.Sprite.Play(GlidingAnimation);

                // Determining which frame of the custom glide animation should be shown
                DynData<Player> data = self.GetData();
                int frame = 3;
                if (data.Data.TryGetValue(CommunalHelper_Player_glideAngle, out object value)) {
                    float angle = (float) value;
                    float t = (angle - GlidingStableAngle) / (GlidingAngleRange / 2f);
                    if (t < 0)
                        frame -= (int) (t * 2);
                    else
                        frame -= (int) (Ease.CubeIn(t) * 3);
                }

                self.Sprite.SetAnimationFrame(frame);
            }
        }

        public static void GlideBegin(this Player player) {
            DynData<Player> data = player.GetData();

            Facings facing = player.Speed.X != 0f ? (Facings) Math.Sign(player.Speed.X) : player.Facing;
            data[CommunalHelper_Player_glideFacing] = facing;

            Vector2 speed = facing == Facings.Right ? player.Speed : new Vector2(-player.Speed.X, player.Speed.Y);
            data[CommunalHelper_Player_glideAngle] = speed.Angle();
            data[CommunalHelper_Player_glideSpeed] = speed.Length();

            data[CommunalHelper_Player_glideSfx] = Audio.Play(CustomSFX.game_elytra_gliding);
            data[CommunalHelper_Player_glideCanTightenWings] = true;
        }

        public static void GlideEnd(this Player player) {
            player.Sprite.Rotation = 0f;

            EventInstance sfx = (EventInstance) player.GetData()[CommunalHelper_Player_glideSfx];
            if (sfx != null)
                Audio.Stop(sfx);
        }

        public static int GlideUpdate(this Player player) {
            if (player.OnGround())
                return Player.StNormal;
            if (player.ClimbCheck((int) player.Facing))
                return Player.StClimb;

            if (player.CanDash)
                return player.StartDash();


            DynData<Player> data = player.GetData();

            // get previous speed and angle values, and other stuff.
            float halfRange = GlidingAngleRange / 2f;
            float oldAngle, newAngle;
            float oldSpeed, newSpeed;
            oldAngle = (float) data[CommunalHelper_Player_glideAngle];
            oldSpeed = Calc.Max((float) data[CommunalHelper_Player_glideSpeed], GlidingMinimumSpeed);
            // we're not putting the speed below the maximum gliding speed, because we want to accept high speeds on state entry
            // decelerating then reaccelerating will instead make you unable to go past the maximum speed.

            // the rate of change of the angle depends on the speed:
            // the faster you go, the slower you can steer.
            // this was needed because you could go at very high speeds and brutally change your direction,
            // which was unrealistic and not fun to play with.
            // it now allows to read what's ahead, and helps with more precise movement.
            float angleMaxChange = Engine.DeltaTime * GlidingInverseSpeedMultAngleFactor / oldSpeed;

            // determine new angle :
            if (oldSpeed == GlidingMinimumSpeed && Input.Feather.Value.Y < 0f) {
                // force middle angle if gliding up but too slow. 
                newAngle = Calc.Approach(oldAngle, GlidingStableAngle, angleMaxChange);
            } else {
                // new angle is controlled by the player.
                float target = GlidingStableAngle + halfRange * Input.Feather.Value.Y;
                newAngle = Calc.Approach(oldAngle, target, angleMaxChange);
            }
            // clamp angle in case the player entered this state (almost) completely vertically.
            newAngle = Calc.Clamp(newAngle, GlidingStableAngle - halfRange, GlidingStableAngle + halfRange);

            // absolute input value will help determine how much we should speed up / slow down.
            float absYInput = Math.Abs(Input.Feather.Value.Y);

            // determine new speed :
            newSpeed = oldSpeed;
            if (newAngle < GlidingStableAngle) {
                // going above middle angle, slow down.
                // if the player goes at a higher speed than the maximum speed, decelerate faster.
                float decel = oldSpeed < GlidingMaximumSpeed ? GlidingDeceleration : GlidingFastDeceleration;
                newSpeed = Calc.Approach(oldSpeed, GlidingMinimumSpeed, Engine.DeltaTime * decel * absYInput);
            } else if (newAngle > GlidingStableAngle) {
                // speed up, relative to how vertical the player's input is.
                if (oldSpeed < GlidingMaximumSpeed)
                    newSpeed = Calc.Approach(oldSpeed, GlidingMaximumSpeed, Engine.DeltaTime * GlidingAcceleration * absYInput);
            }

            // update new values.
            data[CommunalHelper_Player_glideAngle] = newAngle;
            data[CommunalHelper_Player_glideSpeed] = newSpeed;

            // we were executing all the above code regardless of the player's facing, so we can reverse the speed if we need to.
            player.Speed = Calc.AngleToVector(newAngle, newSpeed);
            if ((Facings) data[CommunalHelper_Player_glideFacing] == Facings.Left)
                player.Speed.X *= -1;

            // player sprite rotation.
            float spriteRotation = (newAngle - GlidingStableAngle) * (newAngle < GlidingStableAngle ? 0.4f : 0.71f) * (int) data[CommunalHelper_Player_glideFacing];
            player.Sprite.Rotation = Calc.Approach(player.Sprite.Rotation, spriteRotation, Engine.DeltaTime * (newAngle < GlidingStableAngle ? 1f : 1.2f));

            // sound stuff.
            EventInstance sfx = (EventInstance) player.GetData()[CommunalHelper_Player_glideSfx];
            if (sfx != null) {
                Audio.SetParameter(sfx, "speed", Calc.ClampedMap(newSpeed, GlidingMinimumSpeed, GlidingMaximumSpeed));
                Audio.SetParameter(sfx, "straight_wings", 1 - Math.Abs((newAngle - GlidingStableAngle) / halfRange));
            }

            if ((bool) data[CommunalHelper_Player_glideCanTightenWings] && newAngle <= GlidingStableAngle - halfRange) {
                data[CommunalHelper_Player_glideCanTightenWings] = false;
                Audio.Play(CustomSFX.game_elytra_wings_tighten);
            } else if (newAngle >= GlidingStableAngle) {
                data[CommunalHelper_Player_glideCanTightenWings] = true;
            }

            return St;
        }

        public static IEnumerator GlideRoutine(this Player player) {
            yield return null;
        }
    }
}
