using Microsoft.Xna.Framework.Graphics;

namespace Celeste.Mod.CommunalHelper.Utils;

internal class Shapes
{
    public static Mesh<VertexPositionColorTexture> Box(float sx, float sy, float sz, MTexture texture)
        => Box(sx, sy, sz, texture, texture, texture, texture, texture, texture);

    public static Mesh<VertexPositionColorTexture> Box(float sx, float sy, float sz, MTexture back, MTexture front, MTexture left, MTexture right, MTexture top, MTexture bottom)
    {
        Mesh<VertexPositionColorTexture> mesh = new(null);

        sx /= 2f;
        sy /= 2f;
        sz /= 2f;

        Vector3 v0 = new(-sx, -sy, -sz);
        Vector3 v1 = new(sx, -sy, -sz);
        Vector3 v2 = new(-sx, sy, -sz);
        Vector3 v3 = new(sx, sy, -sz);
        Vector3 v4 = new(-sx, -sy, sz);
        Vector3 v5 = new(sx, -sy, sz);
        Vector3 v6 = new(-sx, sy, sz);
        Vector3 v7 = new(sx, sy, sz);

        // i: 0, 1, 2, 3
        mesh.AddVertex(new(v0, Color.White, new(back.RightUV, back.BottomUV)));
        mesh.AddVertex(new(v1, Color.White, new(back.LeftUV, back.BottomUV)));
        mesh.AddVertex(new(v2, Color.White, new(back.RightUV, back.TopUV)));
        mesh.AddVertex(new(v3, Color.White, new(back.LeftUV, back.TopUV)));
        mesh.AddTriangle(0, 2, 1);
        mesh.AddTriangle(1, 2, 3);

        // i: 4, 5, 6, 7
        mesh.AddVertex(new(v4, Color.White, new(front.LeftUV, front.BottomUV)));
        mesh.AddVertex(new(v5, Color.White, new(front.RightUV, front.BottomUV)));
        mesh.AddVertex(new(v6, Color.White, new(front.LeftUV, front.TopUV)));
        mesh.AddVertex(new(v7, Color.White, new(front.RightUV, front.TopUV)));
        mesh.AddTriangle(4, 5, 6);
        mesh.AddTriangle(5, 7, 6);

        // i: 8, 9, 10, 11
        mesh.AddVertex(new(v0, Color.White, new(left.LeftUV, left.BottomUV)));
        mesh.AddVertex(new(v4, Color.White, new(left.RightUV, left.BottomUV)));
        mesh.AddVertex(new(v6, Color.White, new(left.RightUV, left.TopUV)));
        mesh.AddVertex(new(v2, Color.White, new(left.LeftUV, left.TopUV)));
        mesh.AddTriangle(8, 9, 10);
        mesh.AddTriangle(10, 11, 8);

        // i: 12, 13, 14, 15
        mesh.AddVertex(new(v1, Color.White, new(right.RightUV, right.BottomUV)));
        mesh.AddVertex(new(v5, Color.White, new(right.LeftUV, right.BottomUV)));
        mesh.AddVertex(new(v3, Color.White, new(right.RightUV, right.TopUV)));
        mesh.AddVertex(new(v7, Color.White, new(right.LeftUV, right.TopUV)));
        mesh.AddTriangle(12, 14, 13);
        mesh.AddTriangle(13, 14, 15);

        // i: 16, 17, 18, 19
        mesh.AddVertex(new(v0, Color.White, new(bottom.LeftUV, bottom.BottomUV)));
        mesh.AddVertex(new(v1, Color.White, new(bottom.RightUV, bottom.BottomUV)));
        mesh.AddVertex(new(v4, Color.White, new(bottom.LeftUV, bottom.TopUV)));
        mesh.AddVertex(new(v5, Color.White, new(bottom.RightUV, bottom.TopUV)));
        mesh.AddTriangle(16, 17, 18);
        mesh.AddTriangle(17, 19, 18);

        // i: 20, 21, 22, 23
        mesh.AddVertex(new(v2, Color.White, new(top.LeftUV, top.TopUV)));
        mesh.AddVertex(new(v3, Color.White, new(top.RightUV, top.TopUV)));
        mesh.AddVertex(new(v6, Color.White, new(top.LeftUV, top.BottomUV)));
        mesh.AddVertex(new(v7, Color.White, new(top.RightUV, top.BottomUV)));
        mesh.AddTriangle(20, 22, 21);
        mesh.AddTriangle(21, 22, 23);

        mesh.Bake();
        return mesh;
    }
}
