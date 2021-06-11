using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/DreamJellyfish")]
    class DreamJellyfish : Glider {

        private static MethodInfo m_Player_Pickup = typeof(Player).GetMethod("Pickup", BindingFlags.NonPublic | BindingFlags.Instance);

        private DreamDashCollider dreamDashCollider;
        public bool AllowDreamDash {
            get {
                return dreamDashCollider.Active;
            }
            set {
                dreamDashCollider.Active = value;
            }
        }

        public DreamJellyfish(EntityData data, Vector2 offset) 
            : this(data.Position + offset, data.Bool("bubble"), data.Bool("tutorial")) { }

        public DreamJellyfish(Vector2 position, bool bubble, bool tutorial)
            : base(position, bubble, tutorial) {
            Add(dreamDashCollider = new DreamDashCollider(new Hitbox(28, 16, -13, -18), OnDreamDashExit));
        }

        public void OnDreamDashExit(Player player) {
            if (Input.GrabCheck && player.DashDir.Y == -1) {
                // force-allow pickup
                player.GetData()["minHoldTimer"] = 0f;
                Console.WriteLine(new DynData<Holdable>(Hold)["cannotHoldTimer"] = 0);

                if ((bool) m_Player_Pickup.Invoke(player, new object[] { Hold })) {
                    player.StateMachine.State = Player.StPickup;
                }
            }
        }

        #region Hooks

        internal static void Load() {
            On.Celeste.Holdable.Check += Holdable_Check;
            On.Celeste.Player.NormalUpdate += Player_NormalUpdate;
        }

        internal static void Unload() {
            On.Celeste.Holdable.Check -= Holdable_Check;
            On.Celeste.Player.NormalUpdate -= Player_NormalUpdate;
        }

        private static bool Holdable_Check(On.Celeste.Holdable.orig_Check orig, Holdable self, Player player) {
            if (self.Entity is DreamJellyfish jelly && jelly.AllowDreamDash && player.DashAttacking)
                return false;
            return orig(self, player);
        }

        private static int Player_NormalUpdate(On.Celeste.Player.orig_NormalUpdate orig, Player self) {
            int result = orig(self);

            if (self.Holding?.Entity is DreamJellyfish && self.CanDash && Input.MoveY.Value == -1f) {
                self.Drop();
                return self.StartDash();
            }

            return result;
        }

        #endregion
    }
}
