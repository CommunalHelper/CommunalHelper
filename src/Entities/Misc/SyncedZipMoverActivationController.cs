using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/SyncedZipMoverActivationController")]
    [Tracked]
    class SyncedZipMoverActivationController : Entity {
        private Level level;

        private string colorCode;
        private float resetTimer = 0f;
        private float resetTime;
        public static bool ActivatePressed =>
            CommunalHelperModule.Settings.AllowActivateRebinding ?
                CommunalHelperModule.Settings.ActivateSyncedZipMovers.Pressed :
                Input.Grab.Pressed;

        public SyncedZipMoverActivationController(EntityData data, Vector2 offset)
            : base(data.Position + offset) {
            colorCode = data.Attr("colorCode", "000000");
            resetTime = 0.5f + 0.5f / data.Float("zipMoverSpeedMultiplier", 1);

        }

        public override void Added(Scene scene) {
            base.Added(scene);
            level = scene as Level;
        }

        public override void Update() {
            base.Update();
            if (resetTimer > 0) {
                resetTimer -= Engine.DeltaTime;
            } else if (ActivatePressed || level.Session.GetFlag($"ZipMoverSync:{colorCode}")) {
                Activate();
            }
        }

        public void Activate() {
            if (resetTimer <= 0f) {
                level.Session.SetFlag($"ZipMoverSync:{colorCode}");
                resetTimer = resetTime;
            }
        }

        public static void Hook() {
            On.Monocle.Engine.Update += modEngineUpdate;
        }

        public static void Unhook() {
            On.Monocle.Engine.Update -= modEngineUpdate;
        }

        private static void modEngineUpdate(On.Monocle.Engine.orig_Update orig, Engine engine, GameTime gameTime) {
            orig(engine, gameTime);
            if (Engine.FreezeTimer > 0f && ActivatePressed) {
                var engineData = new DynData<Engine>(engine);
                foreach (Entity controller in engineData.Get<Scene>("scene").Tracker.GetEntities<SyncedZipMoverActivationController>()) {
                    (controller as SyncedZipMoverActivationController).Activate();
                }
            }
        }
    }
}
