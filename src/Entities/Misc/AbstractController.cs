using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using System;

namespace Celeste.Mod.CommunalHelper.Entities {
    [Tracked(true)]
    public abstract class AbstractController : Entity {

        public AbstractController() : base() { }

        public AbstractController(Vector2 position) : base(position) { }

        public abstract void FrozenUpdate();

        public static void Load() {
            IL.Monocle.Engine.Update += Engine_Update;

            ManualCassetteController.Load();
        }

        public static void Unload() {
            IL.Monocle.Engine.Update -= Engine_Update;

            ManualCassetteController.Unload();
        }

        private static void Engine_Update(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            /*
            Stick this in the block
            if (FreezeTimer > 0) {
                AbstractController.UpdateControllers(); <--
                ...
            }
            */
            if (cursor.TryGotoNext(instr => instr.MatchLdsfld<Engine>("FreezeTimer"),
                instr => instr.MatchCall<Engine>("get_RawDeltaTime"))) {
                cursor.EmitDelegate<Action>(UpdateControllers);
            }
        }

        private static void UpdateControllers() {
            foreach (AbstractController controller in Engine.Scene.Tracker.GetEntities<AbstractController>()) {
                controller.FrozenUpdate();
            }
        }
    }
}
