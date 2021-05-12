using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/SyncedZipMoverActivationController")]
    public class SyncedZipMoverActivationController : AbstractInputController {
        private Level level;

        public string ColorCode;
        private float resetTimer = 0f;
        private float resetTime;
        public static bool ActivatePressed =>
            CommunalHelperModule.Settings.AllowActivateRebinding ?
                CommunalHelperModule.Settings.ActivateSyncedZipMovers.Pressed :
                Input.Grab.Pressed;

        public SyncedZipMoverActivationController(EntityData data, Vector2 offset)
            : base(data.Position + offset) {
            ColorCode = data.Attr("colorCode", "000000");
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
            } else if (ActivatePressed || level.Session.GetFlag($"ZipMoverSync:{ColorCode}")) {
                Activate();
            }
        }

        public override void FrozenUpdate() {
            if (resetTimer <= 0 && ActivatePressed) {
                Activate();
            }
        }

        public void Activate() {
            if (resetTimer <= 0f) {
                level.Session.SetFlag($"ZipMoverSync:{ColorCode}");
                resetTimer = resetTime;
            }
        }
    }
}
