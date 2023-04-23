using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;

namespace Celeste.Mod.CommunalHelper.Backdrops;

/// <summary>
/// Custom vertex for Cloudscape meshes. (18 bytes)
/// </summary>
[StructLayout(LayoutKind.Sequential)]
struct CloudscapeVertex : IVertexType
{
    public float Angle, Distance;
    public Vector2 Texture;
    public short Index;

    public static readonly VertexDeclaration VertexDeclaration = new
    (
        new VertexElement(0, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 0),
        new VertexElement(4, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 1),
        new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 2),
        new VertexElement(16, VertexElementFormat.Short2, VertexElementUsage.TextureCoordinate, 3)
    );

    VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

    public CloudscapeVertex(float angle, float distance, Vector2 texture, short index)
    {
        Angle = angle;
        Distance = distance;
        Texture = texture;
        Index = index;
    }
}
