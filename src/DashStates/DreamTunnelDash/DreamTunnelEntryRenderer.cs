using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Entities;

/// <summary>
/// Handles the background and line rendering for DreamTunnelEntries.
/// Added in a LevelLoader.LoadingThread hook in DreamTunnelEntry.
/// </summary>
[Tracked(false)]
public class DreamTunnelEntryRenderer : Entity
{
    private class CustomDepthRenderer : Entity
    {
        public List<DreamTunnelEntry> list = new();

        public CustomDepthRenderer(int depth)
        {
            Tag = Tags.Global | Tags.TransitionUpdate;
            Depth = depth;
        }

        public override void Render()
        {
            foreach (DreamTunnelEntry e in list)
            {
                Vector2 shake = e.Shake;

                Vector2 start = shake + e.Start;
                Vector2 end = shake + e.End;

                Draw.Rect(shake.X + e.X, shake.Y + e.Y, e.Width, e.Height, e.PlayerHasDreamDash ? CustomDreamBlock.ActiveBackColor : CustomDreamBlock.DisabledBackColor * e.Alpha);
                if (e.Whitefill > 0f)
                {
                    Draw.Rect(e.X + shake.X, e.Y + shake.Y, e.Width, e.Height * e.WhiteHeight, Color.White * e.Whitefill * e.Alpha);
                }
                e.WobbleLine(start, end, 0f, false, true);
            }

            foreach (DreamTunnelEntry e in list)
            {
                e.WobbleLine(e.Shake + e.Start, e.Shake + e.End, 0f, true, false);
            }
        }
    }

    private readonly Dictionary<int, CustomDepthRenderer> renderers = new();

    public DreamTunnelEntryRenderer()
    {
        Tag = Tags.Global | Tags.TransitionUpdate;
    }

    public void Track(DreamTunnelEntry entity, int depth)
    {
        // Create new renderer with specific depth if doesn't exist, or get the older one otherwise.
        if (!renderers.TryGetValue(depth, out CustomDepthRenderer renderer))
        {
            renderers.Add(depth, renderer = new CustomDepthRenderer(depth));
            entity.Scene.Add(renderer);
        }

        // Add entity
        renderer.list.Add(entity);
    }

    public void Untrack(DreamTunnelEntry entity, int depth)
    {
        if (renderers.TryGetValue(depth, out CustomDepthRenderer renderer))
        {
            renderer.list.Remove(entity);

            if (renderer.list.Count == 0)
            {
                // No entity with this renderer's depth exist, get rid of it.
                renderers.Remove(depth);
                renderer.RemoveSelf();
            }
        }
    }
}
