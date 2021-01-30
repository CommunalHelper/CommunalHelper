using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CommunalHelper.Entities {

    [Tracked(false)]
    class DreamTunnelEntryRenderer : Entity {

        private List<DreamTunnelEntry> list = new List<DreamTunnelEntry>();

        public DreamTunnelEntryRenderer() {
            base.Tag = ((int) Tags.Global | (int) Tags.TransitionUpdate);
            base.Depth = Depths.FakeWalls;
        }

        public void Track(DreamTunnelEntry entity) => list.Add(entity);

        public void Untrack(DreamTunnelEntry entity) => list.Remove(entity);

        public override void Render() {
            foreach(DreamTunnelEntry e in list) {

                Vector2 start = e.shake + e.start;
                Vector2 end = e.shake + e.end;

                Draw.Rect(e.shake.X + e.X, e.shake.Y + e.Y, e.Width, e.Height, e.PlayerHasDreamDash ? CustomDreamBlock.ActiveBackColor : CustomDreamBlock.DisabledBackColor);
                if (e.whiteFill > 0f) {
                    Draw.Rect(e.X + e.shake.X, e.Y + e.shake.Y, e.Width, e.Height * e.whiteHeight, Color.White * e.whiteFill);
                }
                e.WobbleLine(start, end, 0f, false, true);
            }

            foreach (DreamTunnelEntry e in list) {
                e.WobbleLine(e.shake + e.start, e.shake + e.end, 0f, true, false);
            }
        }
    }

    class DreamTunnelEntryHooks {
        
        public static void Hook() {
            On.Celeste.LevelLoader.LoadingThread += LevelLoader_LoadingThread;
        }

        public static void Unhook() {
            On.Celeste.LevelLoader.LoadingThread -= LevelLoader_LoadingThread;
        }

        private static void LevelLoader_LoadingThread(On.Celeste.LevelLoader.orig_LoadingThread orig, LevelLoader self) {
            orig(self);
            self.Level.Add(new DreamTunnelEntryRenderer());
        }
           
    }
}
