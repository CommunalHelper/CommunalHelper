using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using System.Runtime.InteropServices;

namespace Celeste.Mod.CommunalHelper.Backdrops;

/// <summary>
/// Custom vertex for Cloudscape meshes. (20 bytes)
/// </summary>
[StructLayout(LayoutKind.Sequential)]
struct CloudscapeVertex : IVertexType
{
    public Vector2 Polar; // { angle, distance }
    public Vector2 Texture; // uv
    public Short2 IndexRing; // { cloud_id, ring_idx }

    public static readonly VertexDeclaration VertexDeclaration = new
    (
        new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
        new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 1),
        new VertexElement(16, VertexElementFormat.Short2, VertexElementUsage.TextureCoordinate, 2)
    );

    VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

    public CloudscapeVertex(float angle, float distance, Vector2 texture, short index, short ring)
    {
        Polar = new(angle, distance);
        Texture = texture;
        IndexRing = new(index, ring);
    }
}
