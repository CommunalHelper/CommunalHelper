using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;

namespace Celeste.Mod.CommunalHelper.Entities {
    [Tracked]
    [CustomEntity("CommunalHelper/CassetteJumpFixController")]
    public class CassetteJumpFixController : Entity {

        private bool enable, persistent;

        public CassetteJumpFixController(EntityData data, Vector2 _) {
            Visible = Collidable = false;

            enable = !data.Bool("off", false);
            persistent = data.Bool("persistent", false);
        }

        public override void Added(Scene scene) {
            base.Added(scene);

            if (persistent) {
                CommunalHelperModule.Session.CassetteJumpFix = enable;
            }
        }

        public static bool MustApply(Scene scene) {
            foreach (CassetteJumpFixController controller in scene.Tracker.GetEntities<CassetteJumpFixController>()) {
                if (controller.enable && !controller.persistent)
                    return true;
            }
            return CommunalHelperModule.Session.CassetteJumpFix;
        }


        public static void Load() {
            On.Celeste.CassetteBlock.ShiftSize += CassetteBlock_ShiftSize;
        }

        public static void Unload() {
            On.Celeste.CassetteBlock.ShiftSize -= CassetteBlock_ShiftSize;
        }

        private static void CassetteBlock_ShiftSize(On.Celeste.CassetteBlock.orig_ShiftSize orig, CassetteBlock self, int amount) {
            if (MustApply(self.Scene)) {
                self.MoveV(amount, 0f);
                var data = new DynData<CassetteBlock>(self);
                data["blockHeight"] = data.Get<int>("blockHeight") - amount;
            } else
                orig(self, amount);
        }
    }
}
