using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Entities {

    /// <summary>
    /// Handles the background and line rendering for DreamTunnelEntries
    /// Added in a LevelLoader.LoadingThread hook in DreamTunnelEntry
    /// </summary>
    [Tracked(false)]
    class DreamTunnelEntryRenderer : Entity {

        private List<DreamTunnelEntry> list = new List<DreamTunnelEntry>();

        public DreamTunnelEntryRenderer() {
            Tag = Tags.Global | Tags.TransitionUpdate;
            Depth = Depths.FakeWalls;
        }

        public void Track(DreamTunnelEntry entity) => list.Add(entity);

        public void Untrack(DreamTunnelEntry entity) => list.Remove(entity);

        public override void Render() {
            foreach (DreamTunnelEntry e in list) {
                Vector2 shake = e.Shake;

                Vector2 start = shake + e.Start;
                Vector2 end = shake + e.End;

                Draw.Rect(shake.X + e.X, shake.Y + e.Y, e.Width, e.Height, e.PlayerHasDreamDash ? CustomDreamBlock.ActiveBackColor : CustomDreamBlock.DisabledBackColor * e.Alpha);
                if (e.Whitefill > 0f) {
                    Draw.Rect(e.X + shake.X, e.Y + shake.Y, e.Width, e.Height * e.WhiteHeight, Color.White * e.Whitefill * e.Alpha);
                }
                e.WobbleLine(start, end, 0f, false, true);
            }

            foreach (DreamTunnelEntry e in list) {
                e.WobbleLine(e.Shake + e.Start, e.Shake + e.End, 0f, true, false);
            }
        }

    }
}
