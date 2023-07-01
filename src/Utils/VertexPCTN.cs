using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;

namespace Celeste.Mod.CommunalHelper.Utils;

[StructLayout(LayoutKind.Sequential)]
public struct VertexPCTN : IVertexType
{
    public Vector3 Position;
    public Color Color;
    public Vector2 Texture;
    public Vector3 Normal;

    public VertexPCTN(Vector3 position, Color color, Vector2 texture, Vector3 normal)
    {
        Position = position;
        Color = color;
        Texture = texture;
        Normal = normal;
    }

    public static readonly VertexDeclaration VertexDeclaration = new(
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(sizeof(float) * 3, VertexElementFormat.Color, VertexElementUsage.Color, 0),
        new VertexElement(sizeof(float) * 3 + 4, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
        new VertexElement(sizeof(float) * 3 + 4 + sizeof(float) * 2, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0)
    );

    readonly VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
}
