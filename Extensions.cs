using Celeste.Mod.CommunalHelper.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;

namespace Celeste.Mod.CommunalHelper {
    public static class Extensions {

        #region Vanilla constants
        private const float DashSpeed = 240f;
        private const float ClimbMaxStamina = 110f;
        private const float DreamDashMinTime = 0.1f;
        #endregion

        public static DynData<Player> GetData(this Player player) {
            return new DynData<Player>(player);
        }

        public static Color Mult(this Color color, Color other) {
            color.R = (byte) (color.R * other.R / 256f);
            color.G = (byte) (color.G * other.G / 256f);
            color.B = (byte) (color.B * other.B / 256f);
            color.A = (byte) (color.A * other.A / 256f);
            return color;
        }

        #region DreamBlocks

        public static void CreateTrail(this Player player, Color color) {
            Vector2 scale = new Vector2(Math.Abs(player.Sprite.Scale.X) * (float) player.Facing, player.Sprite.Scale.Y);
            TrailManager.Add(player, scale, color);
        }

        public static void CreateDreamTrail(this Player player) {
            CreateTrail(player, CustomDreamBlockHooks.DreamTrailColors[CustomDreamBlockHooks.DreamTrailColorIndex]);
            ++CustomDreamBlockHooks.DreamTrailColorIndex;
            CustomDreamBlockHooks.DreamTrailColorIndex %= 5;
        }

        internal static bool DreamTunnelDashCheck(this Player player, Vector2 direction) {
            if ((CustomDreamBlockHooks.dreamTunnelDashAttacking || player.DashAttacking && CustomDreamBlockHooks.HasDreamTunnelDash) && Vector2.Dot(player.DashDir, direction) > 0f) {
                Vector2 perpendicular = new Vector2(Math.Abs(direction.Y), Math.Abs(direction.X));

                // Dream block checking
                if (player.CollideCheck<DreamBlock>(player.Position + direction)) {
                    bool decrease = direction.X != 0f ? player.Speed.Y <= 0f : player.Speed.X <= 0f;
                    bool increase = direction.X != 0f ? player.Speed.Y >= 0f : player.Speed.X >= 0f;
                    bool dashedIntoDreamBlock = true;
                    for (int dist = 1; dist <= 4 && dashedIntoDreamBlock; dist++) {
                        for (int dir = -1; dir <= 1; dir += 2) {
                            if (dir == 1 ? increase : decrease) {
                                Vector2 offset = dir * dist * perpendicular;
                                if (!player.CollideCheck<DreamBlock>(player.Position + direction + offset)) {
                                    player.Position += offset;
                                    dashedIntoDreamBlock = false;
                                    break;
                                }
                            }
                        }
                    }
                    if (dashedIntoDreamBlock) {
                        player.Die(-direction);
                        return true;
                    }
                }

                // Solid checking
                if (player.CollideCheck<Solid, DreamBlock>(player.Position + direction)) {
                    bool decrease = direction.X != 0f ? player.Speed.Y <= 0f : player.Speed.X <= 0f;
                    bool increase = direction.X != 0f ? player.Speed.Y >= 0f : player.Speed.X >= 0f;
                    for (int dist = 1; dist <= 4; dist++) {
                        for (int dir = -1; dir <= 1; dir += 2) {
                            if (dir == 1 ? increase : decrease) {
                                Vector2 offset = dir * dist * perpendicular;
                                if (!player.CollideCheck<Solid, DreamBlock>(player.Position + direction + offset)) {
                                    // Actual position offset handled by orig collide
                                    return false;
                                }
                            }
                        }
                    }
                    player.StateMachine.State = CustomDreamBlockHooks.StDreamTunnelDash;
                    CustomDreamBlockHooks.dreamTunnelDashAttacking = false;

                    var playerData = player.GetData();
                    playerData["dashAttackTimer"] = 0f;
                    playerData["gliderBoostTimer"] = 0f;
                    return true;
                }
            }
            return false;
        }

        internal static void DreamTunnelDashBegin(this Player player) {
            var playerData = player.GetData();

            SoundSource dreamSfxLoop = playerData.Get<SoundSource>("dreamSfxLoop");
            if (dreamSfxLoop == null) {
                playerData["dreamSfxLoop"] = dreamSfxLoop = new SoundSource();
                player.Add(dreamSfxLoop);
            }
            player.Speed = player.DashDir * DashSpeed;
            player.TreatNaive = true;

            // Puts the player inside a fast moving solid to ensure they are carried with it
            player.Position.X += Math.Sign(player.DashDir.X);
            player.Position.Y += Math.Sign(player.DashDir.Y);

            player.Depth = Depths.PlayerDreamDashing;
            player.Stamina = ClimbMaxStamina;
            playerData["dreamDashCanEndTimer"] = 0.07f; // jank fix, should be 0.1f
            playerData["dreamJump"] = false;

            player.Play(SFX.char_bad_dreamblock_enter);
            player.Loop(dreamSfxLoop, "event:/char/madeline/dreamblock_travel");
        }

        internal static void DreamTunnelDashEnd(this Player player) {
            var playerData = player.GetData();

            player.Depth = 0;
            if (!playerData.Get<bool>("dreamJump")) {
                player.AutoJump = true;
                player.AutoJumpTimer = 0f;
            }
            if (!player.Inventory.NoRefills) {
                player.RefillDash();
            }
            player.RefillStamina();
            player.TreatNaive = false;

            if (player.DashDir.X != 0f) {
                playerData["jumpGraceTimer"] = 0.1f;
                playerData["dreamJump"] = true;
            } else {
                playerData["jumpGraceTimer"] = 0f;
            }

            Dust.Burst(player.Position, player.Speed.Angle(), 16, null);
            player.Stop(playerData.Get<SoundSource>("dreamSfxLoop"));
            player.Play("event:/char/madeline/dreamblock_exit");
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
        }

        internal static int DreamTunnelDashUpdate(this Player player) {
            var playerData = player.GetData();

            Input.Rumble(RumbleStrength.Light, RumbleLength.Medium);
            Vector2 position = player.Position;
            player.NaiveMove(player.Speed * Engine.DeltaTime);

            float dreamDashCanEndTimer = playerData.Get<float>("dreamDashCanEndTimer");
            if (dreamDashCanEndTimer > 0f) {
                dreamDashCanEndTimer -= Engine.DeltaTime;
                playerData["dreamDashCanEndTimer"] = dreamDashCanEndTimer;
            }
            if (player.CollideCheck<Solid, DreamBlock>()) {
                if (player.Scene.OnInterval(0.1f)) {
                    CreateDreamTrail(player);
                }

                Level level = playerData.Get<Level>("level");
                if (level.OnInterval(0.04f)) {
                    level.Displacement.AddBurst(player.Center, 0.3f, 0f, 40f);
                }
            } else {
                if (DreamTunneledIntoDeath(player)) {
                    if (SaveData.Instance.Assists.Invincible) {
                        player.Position = position;
                        player.Speed *= -1f;
                        player.Play(global::Celeste.SFX.game_assist_dreamblockbounce);
                    } else {
                        player.Die(Vector2.Zero);
                    }
                } else if (dreamDashCanEndTimer <= 0f) {
                    Celeste.Freeze(0.05f);
                    if (Input.Jump.Pressed && player.DashDir.X != 0f) {
                        playerData["dreamJump"] = true;
                        player.Jump();
                    } else if (player.DashDir.Y >= 0f || player.DashDir.X != 0f) {
                        if (player.DashDir.X > 0f && player.CollideCheck<DreamBlock>(player.Position - Vector2.UnitX * 5f)) {
                            player.MoveHExact(-5);
                        } else if (player.DashDir.X < 0f && player.CollideCheck<DreamBlock>(player.Position + Vector2.UnitX * 5f)) {
                            player.MoveHExact(5);
                        }

                        int moveX = playerData.Get<int>("moveX");
                        if (Input.Grab.Check && player.ClimbCheck(moveX)) {
                            player.Facing = (Facings) moveX;
                            if (!SaveData.Instance.Assists.NoGrabbing) {
                                return Player.StClimb;
                            }
                            player.ClimbTrigger(moveX);
                            player.Speed.X = 0f;
                        }
                    }
                    return Player.StNormal;
                }
            }
            return CustomDreamBlockHooks.StDreamTunnelDash;
        }

        private static bool DreamTunneledIntoDeath(Player player) {
            if (player.CollideCheck<DreamBlock>()) {
                for (int i = 1; i <= 5; i++) {
                    for (int j = -1; j <= 1; j += 2) {
                        for (int k = 1; k <= 5; k++) {
                            for (int l = -1; l <= 1; l += 2) {
                                Vector2 vector = new Vector2(i * j, k * l);
                                if (!player.CollideCheck<DreamBlock>(player.Position + vector)) {
                                    player.Position += vector;
                                    return false;
                                }
                            }
                        }
                    }
                }
                return true;
            }
            return false;
        }

        #endregion

        #region WallBoosters

        public static bool AttachedWallBoosterCheck(this Player player) {
            foreach (AttachedWallBooster wallbooster in player.Scene.Tracker.GetEntities<AttachedWallBooster>()) {
                if (player.Facing == wallbooster.Facing && player.CollideCheck(wallbooster))
                    return true;
            }
            return false;
        }

        #endregion

    }
}
