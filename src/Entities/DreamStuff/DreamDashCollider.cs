using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Entities {
    [Tracked]
    class DreamDashCollider : Component {
        public static readonly Color ActiveColor = Color.Teal;
        public static readonly Color InactiveColor = Calc.HexToColor("044f63"); // darker teal

        public Collider Collider;

        public Action<Player, bool> OnExit;

        public DreamDashCollider(Collider collider, Action<Player, bool> onExit_player_dreamJumped = null)
            : base(active: true, visible: false) {
            Collider = collider;
            OnExit = onExit_player_dreamJumped;
        }

        /// <summary>
        /// Checks if the player is colliding with this component's associated entity.
        /// </summary>
        /// <param name="player">The player instance.</param>
        /// <returns></returns>
        public bool Check(Player player) {
            Collider collider = Entity.Collider;

            Entity.Collider = Collider;
            bool check = player.CollideCheck(Entity);
            Entity.Collider = collider;

            return check;
        }

        public override void DebugRender(Camera camera) {
            if (Collider != null && Active) {
                Collider collider = Entity.Collider;
                
                Entity.Collider = Collider;
                Collider.Render(camera, Active ? ActiveColor : InactiveColor);
                Entity.Collider = collider;
            }
        }
    }

    public static class ColliderDreamDash {

        #region Hooks

        #region CommunalHelper Constants

        private const string Player_dreamDashCollider = "communaldreamDashCollider";

        #endregion

        public static int StColliderDreamDashState;

        private static MethodInfo m_Player_DreamDashedIntoSolid = typeof(Player).GetMethod("DreamDashedIntoSolid", BindingFlags.NonPublic | BindingFlags.Instance);
        private static MethodInfo m_Player_CreateTrail = typeof(Player).GetMethod("CreateTrail", BindingFlags.NonPublic | BindingFlags.Instance);

        internal static void Load() {
            On.Celeste.Player.ctor += Player_ctor;
            On.Celeste.Player.DashUpdate += Player_DashUpdate;
        }

        internal static void Unload() {
            On.Celeste.Player.ctor -= Player_ctor;
            On.Celeste.Player.DashUpdate -= Player_DashUpdate;
        }

        private static int Player_DashUpdate(On.Celeste.Player.orig_DashUpdate orig, Player self) {
            int result = orig(self);

            if (self.PlayerInDreamDashCollider() && self.DashDir != Vector2.Zero)
                return StColliderDreamDashState;

            return result;
        }

        private static void Player_ctor(On.Celeste.Player.orig_ctor orig, Player self, Vector2 position, PlayerSpriteMode spriteMode) {
            orig(self, position, spriteMode);

            // DreamDashCollider State
            StColliderDreamDashState = self.StateMachine.AddState(self.ColliderDreamDashUpdate, null, self.ColliderDreamDashBegin, self.ColliderDreamDashEnd);
        }

        private static void ColliderDreamDashBegin(this Player player) {
            DynData<Player> playerData = player.GetData();

            if (playerData["dreamSfxLoop"] == null) {
                SoundSource dreamSfxLoop = new SoundSource();
                player.Add(dreamSfxLoop);
                playerData["dreamSfxLoop"] = dreamSfxLoop;
            }

            player.Speed = player.DashDir * 240f;
            player.TreatNaive = true;

            player.Depth = Depths.PlayerDreamDashing;
            player.Stamina = 110f;
            playerData["dreamJump"] = false;

            player.Play(SFX.char_mad_dreamblock_enter, null, 0f);
            player.Loop(playerData.Get<SoundSource>("dreamSfxLoop"), SFX.char_mad_dreamblock_travel);
        }

        private static void ColliderDreamDashEnd(this Player player) {
            DynData<Player> playerData = player.GetData();

            player.Depth = Depths.Player;

            bool dreamJump = true;
            if (!playerData.Get<bool>("dreamJump")) {
                player.AutoJump = true;
                player.AutoJumpTimer = 0f;
                dreamJump = false;
            }

            if (!player.Inventory.NoRefills) {
                player.RefillDash();
            }

            player.RefillStamina();
            player.TreatNaive = false;

            DreamDashCollider component = playerData.Get<DreamDashCollider>(Player_dreamDashCollider);
            if (component != null) {
                if (player.DashDir.X != 0f) {
                    playerData["jumpGraceTimer"] = 0.1f;
                    playerData["dreamJump"] = true;
                } else {
                    playerData["jumpGraceTimer"] = 0f;
                }
                if (component.OnExit is not null)
                    component.OnExit(player, dreamJump);
                playerData[Player_dreamDashCollider] = null;
            }

            player.Stop(playerData.Get<SoundSource>("dreamSfxLoop"));
            player.Play(SFX.char_mad_dreamblock_exit, null, 0f);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
        }

        private static int ColliderDreamDashUpdate(this Player player) {
            DynData<Player> playerData = player.GetData();

            Input.Rumble(RumbleStrength.Light, RumbleLength.Medium);

            Vector2 position = player.Position;

            float dreamDashCanEndTimer = playerData.Get<float>("dreamDashCanEndTimer");
            if (dreamDashCanEndTimer > 0f) {
                playerData["dreamDashCanEndTimer"] = dreamDashCanEndTimer - Engine.DeltaTime;
            }

            player.PlayerInDreamDashCollider(out DreamDashCollider component);
            if (component == null) {
                if ((bool) m_Player_DreamDashedIntoSolid.Invoke(player, new object[] { })) {
                    if (SaveData.Instance.Assists.Invincible) {
                        player.Position = position;
                        player.Speed *= -1f;
                        player.Play(SFX.game_assist_dreamblockbounce);
                    } else {
                        player.Die(Vector2.Zero);
                    }
                } else if (dreamDashCanEndTimer <= 0f) {
                    Celeste.Freeze(0.05f);

                    if (Input.Jump.Pressed && player.DashDir.X != 0f) {
                        playerData["dreamJump"] = true;
                        player.Jump();
                    } else if (player.DashDir.Y >= 0f || player.DashDir.X != 0f) {
                        if (player.DashDir.X > 0f && player.CollideCheck<Solid>(player.Position - Vector2.UnitX * 5f)) {
                            player.MoveHExact(-5);
                        } else if (player.DashDir.X < 0f && player.CollideCheck<Solid>(player.Position + Vector2.UnitX * 5f)) {
                            player.MoveHExact(5);
                        }
                    }
                    return Player.StNormal;
                }
            } else {
                playerData[Player_dreamDashCollider] = component;

                if (player.Scene.OnInterval(0.1f)) {
                    m_Player_CreateTrail.Invoke(player, new object[] { });
                }

                Level level = player.SceneAs<Level>();
                if (level.OnInterval(0.04f)) {
                    DisplacementRenderer.Burst burst = level.Displacement.AddBurst(player.Center, 0.3f, 0f, 40f);
                    burst.WorldClipPadding = 2;
                }
            }
            return StColliderDreamDashState;
        }

        private static bool PlayerInDreamDashCollider(this Player player, out DreamDashCollider firstComponent) {
            foreach (DreamDashCollider component in player.Scene.Tracker.GetComponents<DreamDashCollider>()) {
                if (component.Check(player)) {
                    firstComponent = component;
                    return true;
                }
            }
            firstComponent = null;
            return false;
        }

        private static bool PlayerInDreamDashCollider(this Player player) {
            foreach (DreamDashCollider component in player.Scene.Tracker.GetComponents<DreamDashCollider>()) {
                if (component.Check(player))
                    return true;
            }
            return false;
        }

        #endregion
    }
}
