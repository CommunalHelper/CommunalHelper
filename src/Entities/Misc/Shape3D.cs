using Celeste.Mod.CommunalHelper.Utils;
using Microsoft.Xna.Framework.Graphics;

namespace Celeste.Mod.CommunalHelper.Entities;

public sealed class Shape3D : Component
{
    internal readonly Mesh<VertexPCTN> Mesh = new();

    public Vector3 Position { get; set; }

    public Matrix Matrix { get; set; } = Matrix.Identity;
    public Texture2D Texture { get; set; } = GFX.Game.Sources[0].Texture_Safe;

    public float DepthEdgeStrength { get; set; } = 0.4f;
    public float NormalEdgeStrength { get; set; } = 0.2f;
    public float RainbowMix { get; set; } = 0.0f;

    public float HighlightLowerBound { get; set; } = 0.8f;
    public float HighlightUpperBound { get; set; } = 1.0f;
    public float HighlightStrength { get; set; } = 0.0f;

    private int depth = 0;
    public int Depth
    {
        get => depth;
        set
        {
            int prev = depth;
            depth = value;

            if (prev == depth)
                return;

            Scene scene = Entity?.Scene;
            if (scene is not null)
            {
                Shape3DRenderer.Get3DRenderer(scene, prev)?.Untrack(this);
                Shape3DRenderer.Get3DRenderer(scene, depth)?.Track(this);
            }
        }
    }

    /// <summary>
    /// Can be outside of the [<c>0.0</c>, <c>1.0</c>] range, so that it can brighten up colors.
    /// </summary>
    public Vector4 Tint { get; set; } = Vector4.One;

    public Shape3D(Mesh<VertexPCTN> mesh)
        : base(active: false, visible: true)
    {
        Mesh = mesh;
    }

    private void TrackSelf(Scene scene) => Shape3DRenderer.Get3DRenderer(scene, Depth)?.Track(this);
    private void UntrackSelf(Scene scene) => Shape3DRenderer.Get3DRenderer(scene, Depth)?.Untrack(this);

    public override void EntityAdded(Scene scene)
    {
        base.EntityAdded(scene);
        TrackSelf(scene);
    }

    public override void Removed(Entity entity)
    {
        base.Removed(entity);
        UntrackSelf(entity.Scene);
    }

    public override void EntityRemoved(Scene scene)
    {
        base.EntityRemoved(scene);
        UntrackSelf(scene);
    }

    public void SetTint(Color color)
    {
        Tint = new(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
    }
}
