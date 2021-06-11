using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/DreamJellyfish")]
    [TrackedAs(typeof(Glider))]
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

        private Sprite sprite;

        public DreamJellyfish(EntityData data, Vector2 offset) 
            : this(data.Position + offset, data.Bool("bubble"), data.Bool("tutorial")) { }

        public DreamJellyfish(Vector2 position, bool bubble, bool tutorial)
            : base(position, bubble, tutorial) {
            DynData<Glider> gliderData = new DynData<Glider>(this);
            sprite = gliderData.Get<Sprite>("sprite");

            Add(dreamDashCollider = new DreamDashCollider(new Hitbox(28, 16, -13, -18), OnDreamDashExit));
        }

        public void OnDreamDashExit(Player player) {
            DisableDreamDash();
            if (Input.GrabCheck && player.DashDir.Y <= 0) {
                // force-allow pickup
                player.GetData()["minHoldTimer"] = 0f;
                new DynData<Holdable>(Hold)["cannotHoldTimer"] = 0;

                if ((bool) m_Player_Pickup.Invoke(player, new object[] { Hold })) {
                    player.StateMachine.State = Player.StPickup;
                }
            }
        }

        private void EnableDreamDash() {
            if (AllowDreamDash)
                return;
            AllowDreamDash = true;
            sprite.SetColor(Color.White);
        }

        private void DisableDreamDash() {
            if (!AllowDreamDash)
                return;
            AllowDreamDash = false;
            sprite.SetColor(Color.LightSlateGray);
        }

        public override void Update() {
            base.Update();
            if ((Hold.Holder == null && OnGround()) || (Hold.Holder != null && Hold.Holder.OnGround())) {
                EnableDreamDash();
            }
        }

        #region Hooks

        internal static void Load() {
            On.Celeste.Holdable.Check += Holdable_Check;
            On.Celeste.Player.NormalUpdate += Player_NormalUpdate;
            On.Celeste.Player.StartDash += Player_StartDash;

            On.Celeste.Glider.HitSpring += Glider_HitSpring;
            On.Celeste.Player.SideBounce += Player_SideBounce;
        }

        internal static void Unload() {
            On.Celeste.Holdable.Check -= Holdable_Check;
            On.Celeste.Player.NormalUpdate -= Player_NormalUpdate;
            On.Celeste.Player.StartDash -= Player_StartDash;

            On.Celeste.Glider.HitSpring -= Glider_HitSpring;
            On.Celeste.Player.SideBounce -= Player_SideBounce;
        }

        private static bool Player_SideBounce(On.Celeste.Player.orig_SideBounce orig, Player self, int dir, float fromX, float fromY) {
            bool result = orig(self, dir, fromX, fromY);
            if (result && self.Holding?.Entity is DreamJellyfish jelly)
                jelly.EnableDreamDash();
            return result;
        }

        private static bool Glider_HitSpring(On.Celeste.Glider.orig_HitSpring orig, Glider self, Spring spring) {
            if (self is DreamJellyfish jelly)
                jelly.EnableDreamDash();
            return orig(self, spring);
        }

        private static int Player_StartDash(On.Celeste.Player.orig_StartDash orig, Player self) {
            if (self.Holding?.Entity is DreamJellyfish && Input.MoveY.Value == -1f)
                self.Drop();
            return orig(self);
        }

        private static bool Holdable_Check(On.Celeste.Holdable.orig_Check orig, Holdable self, Player player) {
            if (self.Entity is DreamJellyfish jelly && jelly.AllowDreamDash && player.DashAttacking)
                return false;
            return orig(self, player);
        }

        private static int Player_NormalUpdate(On.Celeste.Player.orig_NormalUpdate orig, Player self) {
            int result = orig(self);

            if (self.Holding?.Entity is DreamJellyfish jelly && jelly.AllowDreamDash && self.CanDash && Input.MoveY.Value == -1f) {
                self.Drop();
                return self.StartDash();
            }

            return result;
        }

        #endregion
    }
}
