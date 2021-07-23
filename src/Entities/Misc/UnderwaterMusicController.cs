using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using System;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/UnderwaterMusicController")]
    [Tracked]
    public class UnderwaterMusicController : Entity {

        public bool Enable;
        public bool DashSFX;

        public UnderwaterMusicController(EntityData data, Vector2 _) {
            Enable = data.Bool("enable");
            DashSFX = data.Bool("dashSFX");
        }

        internal static void Load() {
            On.Celeste.Player.UnderwaterMusicCheck += Player_UnderwaterMusicCheck;
            IL.Celeste.Player.CallDashEvents += Player_CallDashEvents;
        }

        internal static void Unload() {
            On.Celeste.Player.UnderwaterMusicCheck -= Player_UnderwaterMusicCheck;
            IL.Celeste.Player.CallDashEvents -= Player_CallDashEvents;
        }

        private static bool Player_UnderwaterMusicCheck(On.Celeste.Player.orig_UnderwaterMusicCheck orig, Player self) {
            return self.SceneAs<Level>().Tracker.GetEntity<UnderwaterMusicController>()?.Enable ?? orig(self);
        }

        private static void Player_CallDashEvents(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<Player>("SwimCheck"))) {
                cursor.EmitDelegate<Func<bool, bool>>(s => {
                    var controller = (Engine.Scene as Level)?.Tracker?.GetEntity<UnderwaterMusicController>();
                    if (controller is null || !controller.DashSFX)
                        return s;
                    else
                        return controller.Enable;
                });
            }
        }

    }
}
