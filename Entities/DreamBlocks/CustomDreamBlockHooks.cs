using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;

namespace Celeste.Mod.CommunalHelper.Entities {
    public static class CustomDreamBlockHooks {

        public static int StDreamTunnelDash;
        private static bool hasDreamTunnelDash = false;
        public static bool HasDreamTunnelDash {
            get { return hasDreamTunnelDash || CommunalHelperModule.Settings.AlwaysActiveDreamRefillCharge; }
            set { hasDreamTunnelDash = value; }
        }
        public static bool dreamTunnelDashAttacking = false;

        public static Color[] DreamTrailColors;
        public static int DreamTrailColorIndex = 0;

        internal const float DashSpeed = 240f;

        public static void Load() {
            DreamRefill.Load();
            CustomDreamBlock.Load();

            On.Celeste.Player.ctor += Player_ctor;
            On.Celeste.Player.DashBegin += Player_DashBegin;
            On.Celeste.Player.DashCoroutine += Player_DashCoroutine;
            On.Celeste.Player.Update += Player_Update;
            On.Celeste.Player.CreateTrail += Player_CreateTrail;
            On.Celeste.Player.OnCollideH += Player_OnCollideH;
            On.Celeste.Player.OnCollideV += Player_OnCollideV;

            On.Celeste.Player.Die += Player_Die;
            On.Celeste.Player.UpdateSprite += Player_UpdateSprite;
            On.Celeste.Player.IsRiding_Solid += Player_IsRiding_Solid;
            On.Celeste.Player.SceneEnd += Player_SceneEnd;

            On.Celeste.Level.EnforceBounds += Level_EnforceBounds;
        }

        public static void Unload() {
            DreamRefill.Unload();
            CustomDreamBlock.Load();

            On.Celeste.Player.ctor -= Player_ctor;
            On.Celeste.Player.DashBegin -= Player_DashBegin;
            On.Celeste.Player.DashCoroutine -= Player_DashCoroutine;
            On.Celeste.Player.Update -= Player_Update;
            On.Celeste.Player.CreateTrail -= Player_CreateTrail;
            On.Celeste.Player.OnCollideH -= Player_OnCollideH;
            On.Celeste.Player.OnCollideV -= Player_OnCollideV;
            On.Celeste.Player.Die -= Player_Die;
            On.Celeste.Player.UpdateSprite -= Player_UpdateSprite;
            On.Celeste.Player.IsRiding_Solid -= Player_IsRiding_Solid;
            On.Celeste.Player.SceneEnd -= Player_SceneEnd;

            On.Celeste.Level.EnforceBounds -= Level_EnforceBounds;
        }

        public static void LoadContent() {
            DreamTrailColors = new Color[5];
            DreamTrailColors[0] = Calc.HexToColor("FFEF11");
            DreamTrailColors[1] = Calc.HexToColor("08A310");
            DreamTrailColors[2] = Calc.HexToColor("FF00D0");
            DreamTrailColors[3] = Calc.HexToColor("5FCDE4");
            DreamTrailColors[4] = Calc.HexToColor("E0564C");

            DreamRefill.InitializeParticles();
        }

        // Adds custom dream tunnel dash state
        private static void Player_ctor(On.Celeste.Player.orig_ctor orig, Player player, Vector2 position, PlayerSpriteMode spriteMode) {
            orig(player, position, spriteMode);
            StDreamTunnelDash = player.StateMachine.AddState(player.DreamTunnelDashUpdate, null, player.DreamTunnelDashBegin, player.DreamTunnelDashEnd);
        }

        // Dream tunnel dash triggering
        private static void Player_DashBegin(On.Celeste.Player.orig_DashBegin orig, Player player) {
            orig(player);
            if (HasDreamTunnelDash) {
                dreamTunnelDashAttacking = true;
                HasDreamTunnelDash = false;

                // Ensures the player enters the dream tunnel dash state if dashing into a fast moving block
                var playerData = player.GetData();
                Vector2 lastAim = playerData.Get<Vector2>("lastAim");
                int dirX = Math.Sign(lastAim.X);
                int dirY = Math.Sign(lastAim.Y);
                Vector2 dir = new Vector2(dirX, dirY);
                if (player.CollideCheck<Solid, DreamBlock>(player.Position + dir)) {
                    player.Speed = player.DashDir = dir;
                    player.MoveHExact(dirX, playerData.Get<Collision>("onCollideH"));
                    player.MoveVExact(dirY, playerData.Get<Collision>("onCollideV"));
                }
            }
        }



        // Allows downwards diagonal dream tunnel dashing when on the ground 
        private static IEnumerator Player_DashCoroutine(On.Celeste.Player.orig_DashCoroutine orig, Player player) {
            IEnumerator origEnum = orig(player);
            origEnum.MoveNext();
            yield return origEnum.Current;

            bool forceDownwardDiagonalDash = false;
            Vector2 origDashDir = Input.GetAimVector(player.Facing);
            if (player.OnGround() && origDashDir.X != 0f && origDashDir.Y > 0f && CustomDreamBlockHooks.dreamTunnelDashAttacking) {
                forceDownwardDiagonalDash = true;
            }
            origEnum.MoveNext();
            if (forceDownwardDiagonalDash) {
                player.DashDir = origDashDir;
                player.Speed = origDashDir * DashSpeed;
                if (player.CanUnDuck) {
                    player.Ducking = false;
                }
            }
            yield return origEnum.Current;

            origEnum.MoveNext();
        }

        // Dream trail creation and dreamTunnelDashAttacking updating
        private static void Player_Update(On.Celeste.Player.orig_Update orig, Player player) {
            orig(player);

            Level level = player.Scene as Level;
            if (HasDreamTunnelDash && level.OnInterval(0.1f)) {
                player.CreateDreamTrail();
            }
            if (!player.DashAttacking) {
                dreamTunnelDashAttacking = false;
            }
        }

        // Dream tunnel dash trail recoloring
        private static void Player_CreateTrail(On.Celeste.Player.orig_CreateTrail orig, Player player) {
            if (dreamTunnelDashAttacking) {
                player.CreateTrail(DreamTrailColors[DreamTrailColorIndex]);
                ++DreamTrailColorIndex;
                DreamTrailColorIndex %= 5;
            } else {
                orig(player);
            }
        }

        // Dream tunnel dash/dashing into dream block detection 
        private static void Player_OnCollideH(On.Celeste.Player.orig_OnCollideH orig, Player player, CollisionData data) {
            if (player.StateMachine.State == StDreamTunnelDash) {
                return;
            }
            if (!player.DreamTunnelDashCheck(new Vector2(Math.Sign(player.Speed.X), 0))) {
                orig(player, data);
            }
        }
        private static void Player_OnCollideV(On.Celeste.Player.orig_OnCollideV orig, Player player, CollisionData data) {
            if (player.StateMachine.State == StDreamTunnelDash) {
                return;
            }

            if (!player.DreamTunnelDashCheck(new Vector2(0, Math.Sign(player.Speed.Y)))) {
                orig(player, data);
            }
        }

        // Kills the player if they dream tunnel dash into the level bounds
        private static void Level_EnforceBounds(On.Celeste.Level.orig_EnforceBounds orig, Level level, Player player) {
            Rectangle bounds = level.Bounds;
            bool canDie = player.StateMachine.State == StDreamTunnelDash && player.CollideCheck<Solid>();
            if (canDie && (player.Right > bounds.Right || player.Left < bounds.Left || player.Top < bounds.Top || player.Bottom > bounds.Bottom)) {
                player.Die(Vector2.Zero);
            } else {
                orig(level, player);
            }
        }

        // Fixes bug with dreamSfx soundsource not being stopped
        private static PlayerDeadBody Player_Die(On.Celeste.Player.orig_Die orig, Player player, Vector2 dir, bool evenIfInvincible = false, bool registerDeathInStats = true) {
            HasDreamTunnelDash = false;
            SoundSource dreamSfxLoop = player.GetData().Get<SoundSource>("dreamSfxLoop");
            if (dreamSfxLoop != null) {
                dreamSfxLoop.Stop();
            }
            return orig(player, dir, evenIfInvincible, registerDeathInStats);
        }

        // Updates sprite for dream tunnel dash state
        private static void Player_UpdateSprite(On.Celeste.Player.orig_UpdateSprite orig, Player player) {
            if (StDreamTunnelDash != 0 && player.StateMachine.State == StDreamTunnelDash) {
                if (player.Sprite.CurrentAnimationID != "dreamDashIn" && player.Sprite.CurrentAnimationID != "dreamDashLoop") {
                    player.Sprite.Play("dreamDashIn");
                }
            } else {
                orig(player);
            }
        }

        // Ensures that the player is transported by moving solids when dream tunnel dashing through them
        private static bool Player_IsRiding_Solid(On.Celeste.Player.orig_IsRiding_Solid orig, Player player, Solid solid) {
            if (player.StateMachine.State == StDreamTunnelDash) {
                return player.CollideCheck(solid);
            }
            return orig(player, solid);
        }

        private static void Player_SceneEnd(On.Celeste.Player.orig_SceneEnd orig, Player player, Scene scene) {
            orig(player, scene);
            HasDreamTunnelDash = false;
        }
    }
}
