using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/DreamJellyfish")]
    [Tracked(true)]
    class DreamJellyfish : Glider {

        private static MethodInfo m_Player_Pickup = typeof(Player).GetMethod("Pickup", BindingFlags.NonPublic | BindingFlags.Instance);

        // Could maybe use CustomDreamBlock.DreamParticle.
        public struct DreamParticle {
            public Vector2 Position;
            public int Layer;
            public Color Color;
            public float TimeOffset;
        }
        public DreamParticle[] Particles;
        public static MTexture[] ParticleTextures;

        public Rectangle particleBounds = new Rectangle(-23, -35, 48, 42);

        private DreamDashCollider dreamDashCollider;
        public bool AllowDreamDash {
            get {
                return dreamDashCollider.Active;
            }
            set {
                dreamDashCollider.Active = value;
            }
        }

        public Sprite Sprite;
        public MTexture CurrentFrame => Sprite.GetFrame(Sprite.CurrentAnimationID, Sprite.CurrentAnimationFrame);

        public DreamJellyfish(EntityData data, Vector2 offset) 
            : this(data.Position + offset, data.Bool("bubble"), data.Bool("tutorial")) { }

        public DreamJellyfish(Vector2 position, bool bubble, bool tutorial)
            : base(position, bubble, tutorial) {
            DynData<Glider> gliderData = new DynData<Glider>(this);

            Sprite oldSprite = gliderData.Get<Sprite>("sprite");
            Remove(oldSprite);
            gliderData["sprite"] = Sprite = CommunalHelperModule.SpriteBank.Create("dreamJellyfish");
            Add(Sprite);

            Add(dreamDashCollider = new DreamDashCollider(new Hitbox(28, 16, -13, -18), OnDreamDashExit));
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);

            int w = particleBounds.Width;
            int h = particleBounds.Height;
            Particles = new DreamParticle[(int) (w / 8f * (h / 8f) * 1.5f)];
            for (int i = 0; i < Particles.Length; i++) {
                Particles[i].Position = new Vector2(Calc.Random.NextFloat(w), Calc.Random.NextFloat(h));
                Particles[i].Layer = Calc.Random.Choose(0, 1, 1, 2, 2, 2);
                Particles[i].TimeOffset = Calc.Random.NextFloat();
                Particles[i].Color = Particles[i].Layer switch {
                    2 => Calc.Random.Choose(Calc.HexToColor("5b6ee1"), Calc.HexToColor("CC3B3B"), Calc.HexToColor("7daa64")),
                    1 => Calc.Random.Choose(Calc.HexToColor("5fcde4"), Calc.HexToColor("7fb25e"), Calc.HexToColor("E0564C")),
                    _ => Calc.Random.Choose(Calc.HexToColor("FFEF11"), Calc.HexToColor("FF00D0"), Calc.HexToColor("08a310")),
                };
            }

            scene.Tracker.GetEntity<DreamJellyfishRenderer>().Track(this);
        }

        public override void Removed(Scene scene) {
            base.Removed(scene);
            scene.Tracker.GetEntity<DreamJellyfishRenderer>().Untrack(this);
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
        }

        private void DisableDreamDash() {
            if (!AllowDreamDash)
                return;
            AllowDreamDash = false;
        }

        public override void Update() {
            base.Update();
            if ((Hold.Holder == null && OnGround()) || (Hold.Holder != null && Hold.Holder.OnGround())) {
                EnableDreamDash();
            }
        }

        public override void Render() {
            Remove(Sprite);
            base.Render();
            Add(Sprite);
        }

        public static void InitializeTextures() {
            ParticleTextures = new MTexture[4] {
                GFX.Game["objects/dreamblock/particles"].GetSubtexture(14, 0, 7, 7),
                GFX.Game["objects/dreamblock/particles"].GetSubtexture(7, 0, 7, 7),
                GFX.Game["objects/dreamblock/particles"].GetSubtexture(0, 0, 7, 7),
                GFX.Game["objects/dreamblock/particles"].GetSubtexture(7, 0, 7, 7),
            };
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
