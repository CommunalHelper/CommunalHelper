using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Entities {

    /// <summary>
    /// Handles the background and line rendering for DreamTunnelEntries.
    /// Added in a LevelLoader.LoadingThread hook in DreamTunnelEntry.
    /// </summary>
    [Tracked(false)]
    class DreamTunnelEntryRenderer : Entity {

        private class CustomDepthDreamTunnelEntryRenderer : Entity{
            public List<DreamTunnelEntry> list = new List<DreamTunnelEntry>();

            public CustomDepthDreamTunnelEntryRenderer(int depth) {
                Tag = Tags.Global | Tags.TransitionUpdate;
                Depth = depth;
            }

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

        private Dictionary<int, CustomDepthDreamTunnelEntryRenderer> renderers = new Dictionary<int, CustomDepthDreamTunnelEntryRenderer>();

        public DreamTunnelEntryRenderer() {
            Tag = Tags.Global | Tags.TransitionUpdate;
            Depth = Depths.FakeWalls;
        }

        public void Track(DreamTunnelEntry entity, int depth, Level level) {
            // Create new renderer with specific depth if doesn't exist, or get the older one otherwise.
            CustomDepthDreamTunnelEntryRenderer renderer;
            if (renderers.TryGetValue(depth, out CustomDepthDreamTunnelEntryRenderer oldRenderer)) {
                renderer = oldRenderer;
            } else {
                renderers.Add(depth, renderer = new CustomDepthDreamTunnelEntryRenderer(depth));
                level.Add(renderer);
            }

            // Add entity
            renderer.list.Add(entity);
        }

        public void Untrack(DreamTunnelEntry entity, int depth, Level level) {
            if (renderers.TryGetValue(depth, out CustomDepthDreamTunnelEntryRenderer renderer)) {
                renderer.list.Remove(entity);

                if (renderer.list.Count == 0) {
                    // No entity with this renderer's depth exist, get rid of it.
                    renderers.Remove(depth);
                    level.Remove(renderer);
                }
            }
        }
    }
}
