using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Utils;

internal class Shapes
{
    // φ, the golden ratio.
    private static readonly float phi = (float) ((1 + Math.Sqrt(5)) / 2);

    // icosahedron, aka 0-icosphere.
    private static readonly Vector3[] icosahedron_vertices;
    private static readonly int[] icosahedron_indices;

    // 1-icosphere, an icosahedron subdivided 1 times.
    private static readonly Vector3[] icosphere1_vertices;
    private static readonly int[] icosphere1_indices;

    static Shapes()
    {
        Tuple<Vector3[], int[]> data;

        data = GenerateIcosphereGeometry(0);
        icosahedron_vertices = data.Item1;
        icosahedron_indices = data.Item2;

        data = GenerateIcosphereGeometry(1);
        icosphere1_vertices = data.Item1;
        icosphere1_indices = data.Item2;
    }

    public static Mesh<VertexPCTN> Gear(float teeth, float depth, float slope, float innerRadius, float thickness, float scale, Color color)
    {
        Mesh<VertexPCTN> mesh = new();

        Vector2 uv = Vector2.Zero;

        float alpha = -slope / (2 * slope - 1) * MathHelper.Pi / teeth;
        float beta = (1 - slope) / (2 * slope - 1) * MathHelper.Pi / teeth;

        thickness /= 2f;

        int o = mesh.VertexCount;
        for (int i = 0; i < teeth; ++i)
        {
            float angle = MathHelper.TwoPi * i / teeth;
            float nextangle = MathHelper.TwoPi * (i + 1) / teeth;

            // front face right teeth slope
            mesh.AddVertex(new(new(scale * Calc.AngleToVector(beta + angle, innerRadius), -thickness), color, uv, Vector3.Backward));
            mesh.AddVertex(new(new(scale * Calc.AngleToVector(alpha + angle, innerRadius), -thickness), color, uv, Vector3.Backward));
            mesh.AddVertex(new(new(scale * Calc.AngleToVector(beta + angle, 1 + depth), -thickness), color, uv, Vector3.Backward));
            mesh.AddVertex(new(new(scale * Calc.AngleToVector(alpha + angle, 1), -thickness), color, uv, Vector3.Backward));
            mesh.AddTriangle(o + 0, o + 1, o + 2);
            mesh.AddTriangle(o + 1, o + 3, o + 2);

            // front face left teeth slope
            mesh.AddVertex(new(new(scale * Calc.AngleToVector(-alpha + angle, innerRadius), -thickness), color, uv, Vector3.Backward));
            mesh.AddVertex(new(new(scale * Calc.AngleToVector(-beta + angle, innerRadius), -thickness), color, uv, Vector3.Backward));
            mesh.AddVertex(new(new(scale * Calc.AngleToVector(-alpha + angle, 1), -thickness), color, uv, Vector3.Backward));
            mesh.AddVertex(new(new(scale * Calc.AngleToVector(-beta + angle, 1 + depth), -thickness), color, uv, Vector3.Backward));
            mesh.AddTriangle(o + 4, o + 5, o + 6);
            mesh.AddTriangle(o + 5, o + 7, o + 6);

            // front face teeth joint
            mesh.AddTriangle(o + 2, o + 5, o + 0);
            mesh.AddTriangle(o + 5, o + 2, o + 7);

            // front face hole joint
            mesh.AddVertex(new(new(scale * Calc.AngleToVector(alpha + nextangle, innerRadius), -thickness), color, uv, Vector3.Backward));
            mesh.AddVertex(new(new(scale * Calc.AngleToVector(alpha + nextangle, 1), -thickness), color, uv, Vector3.Backward));
            mesh.AddTriangle(o + 6, o + 8, o + 4);
            mesh.AddTriangle(o + 6, o + 9, o + 8);

            o += 10;

            // back face right teeth slope
            mesh.AddVertex(new(new(scale * Calc.AngleToVector(beta + angle, innerRadius), thickness), color, uv, Vector3.Forward));
            mesh.AddVertex(new(new(scale * Calc.AngleToVector(alpha + angle, innerRadius), thickness), color, uv, Vector3.Forward));
            mesh.AddVertex(new(new(scale * Calc.AngleToVector(beta + angle, 1 + depth), thickness), color, uv, Vector3.Forward));
            mesh.AddVertex(new(new(scale * Calc.AngleToVector(alpha + angle, 1), thickness), color, uv, Vector3.Forward));
            mesh.AddTriangle(o + 0, o + 2, o + 1);
            mesh.AddTriangle(o + 1, o + 2, o + 3);

            // back face left teeth slope
            mesh.AddVertex(new(new(scale * Calc.AngleToVector(-alpha + angle, innerRadius), thickness), color, uv, Vector3.Forward));
            mesh.AddVertex(new(new(scale * Calc.AngleToVector(-beta + angle, innerRadius), thickness), color, uv, Vector3.Forward));
            mesh.AddVertex(new(new(scale * Calc.AngleToVector(-alpha + angle, 1), thickness), color, uv, Vector3.Forward));
            mesh.AddVertex(new(new(scale * Calc.AngleToVector(-beta + angle, 1 + depth), thickness), color, uv, Vector3.Forward));
            mesh.AddTriangle(o + 4, o + 6, o + 5);
            mesh.AddTriangle(o + 5, o + 6, o + 7);

            // back face teeth joint
            mesh.AddTriangle(o + 2, o + 0, o + 5);
            mesh.AddTriangle(o + 5, o + 7, o + 2);

            // back face hole joint
            mesh.AddVertex(new(new(scale * Calc.AngleToVector(alpha + nextangle, innerRadius), thickness), color, uv, Vector3.Forward));
            mesh.AddVertex(new(new(scale * Calc.AngleToVector(alpha + nextangle, 1), thickness), color, uv, Vector3.Forward));
            mesh.AddTriangle(o + 6, o + 4, o + 8);
            mesh.AddTriangle(o + 6, o + 8, o + 9);

            o += 10;

            Vector3 a, b, c, d, normal;

            // side face right teeth slope
            a = new(scale * Calc.AngleToVector(beta + angle, 1 + depth), -thickness);
            b = new(scale * Calc.AngleToVector(alpha + angle, 1), -thickness);
            c = new(scale * Calc.AngleToVector(beta + angle, 1 + depth), thickness);
            d = new(scale * Calc.AngleToVector(alpha + angle, 1), thickness);
            normal = Vector3.Cross(b - a, d - a);
            mesh.AddVertex(new(a, color, uv, normal));
            mesh.AddVertex(new(b, color, uv, normal));
            mesh.AddVertex(new(c, color, uv, normal));
            mesh.AddVertex(new(d, color, uv, normal));
            mesh.AddTriangle(o + 0, o + 1, o + 2);
            mesh.AddTriangle(o + 1, o + 3, o + 2);
            o += 4;

            // side face left teeth slope
            a = new(scale * Calc.AngleToVector(-beta + angle, 1 + depth), -thickness);
            b = new(scale * Calc.AngleToVector(-alpha + angle, 1), -thickness);
            c = new(scale * Calc.AngleToVector(-beta + angle, 1 + depth), thickness);
            d = new(scale * Calc.AngleToVector(-alpha + angle, 1), thickness);
            normal = -Vector3.Cross(b - a, d - a);
            mesh.AddVertex(new(a, color, uv, normal));
            mesh.AddVertex(new(b, color, uv, normal));
            mesh.AddVertex(new(c, color, uv, normal));
            mesh.AddVertex(new(d, color, uv, normal));
            mesh.AddTriangle(o + 0, o + 2, o + 1);
            mesh.AddTriangle(o + 1, o + 2, o + 3);
            o += 4;

            // side face teeth joint
            a = new(scale * Calc.AngleToVector(beta + angle, 1 + depth), -thickness);
            b = new(scale * Calc.AngleToVector(-beta + angle, 1 + depth), -thickness);
            c = new(scale * Calc.AngleToVector(beta + angle, 1 + depth), thickness);
            d = new(scale * Calc.AngleToVector(-beta + angle, 1 + depth), thickness);
            normal = -Vector3.Cross(b - a, d - a);
            mesh.AddVertex(new(a, color, uv, normal));
            mesh.AddVertex(new(b, color, uv, normal));
            mesh.AddVertex(new(c, color, uv, normal));
            mesh.AddVertex(new(d, color, uv, normal));
            mesh.AddTriangle(o + 0, o + 2, o + 1);
            mesh.AddTriangle(o + 1, o + 2, o + 3);
            o += 4;

            // side face hole joint
            a = new(scale * Calc.AngleToVector(alpha + nextangle, 1), -thickness);
            b = new(scale * Calc.AngleToVector(-alpha + angle, 1), -thickness);
            c = new(scale * Calc.AngleToVector(alpha + nextangle, 1), thickness);
            d = new(scale * Calc.AngleToVector(-alpha + angle, 1), thickness);
            normal = Vector3.Cross(b - a, d - a);
            mesh.AddVertex(new(a, color, uv, normal));
            mesh.AddVertex(new(b, color, uv, normal));
            mesh.AddVertex(new(c, color, uv, normal));
            mesh.AddVertex(new(d, color, uv, normal));
            mesh.AddTriangle(o + 0, o + 1, o + 2);
            mesh.AddTriangle(o + 1, o + 3, o + 2);
            o += 4;

            // side face middle right joint
            a = new(scale * Calc.AngleToVector(beta + angle, innerRadius), -thickness);
            b = new(scale * Calc.AngleToVector(alpha + angle, innerRadius), -thickness);
            c = new(scale * Calc.AngleToVector(beta + angle, innerRadius), thickness);
            d = new(scale * Calc.AngleToVector(alpha + angle, innerRadius), thickness);
            normal = -Vector3.Cross(b - a, d - a);
            mesh.AddVertex(new(a, color, uv, normal));
            mesh.AddVertex(new(b, color, uv, normal));
            mesh.AddVertex(new(c, color, uv, normal));
            mesh.AddVertex(new(d, color, uv, normal));
            mesh.AddTriangle(o + 0, o + 2, o + 1);
            mesh.AddTriangle(o + 1, o + 2, o + 3);
            o += 4;

            // side face middle left joint
            a = new(scale * Calc.AngleToVector(-alpha + angle, innerRadius), -thickness);
            b = new(scale * Calc.AngleToVector(-beta + angle, innerRadius), -thickness);
            c = new(scale * Calc.AngleToVector(-alpha + angle, innerRadius), thickness);
            d = new(scale * Calc.AngleToVector(-beta + angle, innerRadius), thickness);
            normal = -Vector3.Cross(b - a, d - a);
            mesh.AddVertex(new(a, color, uv, normal));
            mesh.AddVertex(new(b, color, uv, normal));
            mesh.AddVertex(new(c, color, uv, normal));
            mesh.AddVertex(new(d, color, uv, normal));
            mesh.AddTriangle(o + 0, o + 2, o + 1);
            mesh.AddTriangle(o + 1, o + 2, o + 3);
            o += 4;

            // side face middle center joint
            a = new(scale * Calc.AngleToVector(beta + angle, innerRadius), -thickness);
            b = new(scale * Calc.AngleToVector(beta + angle, innerRadius), thickness);
            c = new(scale * Calc.AngleToVector(-beta + angle, innerRadius), -thickness);
            d = new(scale * Calc.AngleToVector(-beta + angle, innerRadius), thickness);
            normal = -Vector3.Cross(b - a, d - a);
            mesh.AddVertex(new(a, color, uv, normal));
            mesh.AddVertex(new(b, color, uv, normal));
            mesh.AddVertex(new(c, color, uv, normal));
            mesh.AddVertex(new(d, color, uv, normal));
            mesh.AddTriangle(o + 0, o + 2, o + 1);
            mesh.AddTriangle(o + 1, o + 2, o + 3);
            o += 4;

            // side face middle transition joint
            a = new(scale * Calc.AngleToVector(-alpha + angle, innerRadius), -thickness);
            b = new(scale * Calc.AngleToVector(alpha + nextangle, innerRadius), -thickness);
            c = new(scale * Calc.AngleToVector(-alpha + angle, innerRadius), thickness);
            d = new(scale * Calc.AngleToVector(alpha + nextangle, innerRadius), thickness);
            normal = Vector3.Cross(b - a, d - a);
            mesh.AddVertex(new(a, color, uv, normal));
            mesh.AddVertex(new(b, color, uv, normal));
            mesh.AddVertex(new(c, color, uv, normal));
            mesh.AddVertex(new(d, color, uv, normal));
            mesh.AddTriangle(o + 0, o + 1, o + 2);
            mesh.AddTriangle(o + 1, o + 3, o + 2);
            o += 4;
        }

        mesh.Bake();
        return mesh;
    }

    public static Mesh<VertexPositionNormalTexture> Box_PositionNormalTexture(Vector3 pos, Vector3 size)
    {
        Mesh<VertexPositionNormalTexture> mesh = new();

        size.X /= 2f;
        size.Y /= 2f;
        size.Z /= 2f;

        Vector3 v0 = pos + new Vector3(-size.X, -size.Y, -size.Z);
        Vector3 v1 = pos + new Vector3(size.X, -size.Y, -size.Z);
        Vector3 v2 = pos + new Vector3(-size.X, size.Y, -size.Z);
        Vector3 v3 = pos + new Vector3(size.X, size.Y, -size.Z);
        Vector3 v4 = pos + new Vector3(-size.X, -size.Y, size.Z);
        Vector3 v5 = pos + new Vector3(size.X, -size.Y, size.Z);
        Vector3 v6 = pos + new Vector3(-size.X, size.Y, size.Z);
        Vector3 v7 = pos + new Vector3(size.X, size.Y, size.Z);
        
        // i: 0, 1, 2, 3
        mesh.AddVertex(new(v0, Vector3.Backward, Vector2.Zero));
        mesh.AddVertex(new(v1, Vector3.Backward, Vector2.Zero));
        mesh.AddVertex(new(v2, Vector3.Backward, Vector2.Zero));
        mesh.AddVertex(new(v3, Vector3.Backward, Vector2.Zero));
        mesh.AddTriangle(2, 0, 1);
        mesh.AddTriangle(2, 1, 3);

        // i: 4, 5, 6, 7
        mesh.AddVertex(new(v4, Vector3.Forward, Vector2.Zero));
        mesh.AddVertex(new(v5, Vector3.Forward, Vector2.Zero));
        mesh.AddVertex(new(v6, Vector3.Forward, Vector2.Zero));
        mesh.AddVertex(new(v7, Vector3.Forward, Vector2.Zero));
        mesh.AddTriangle(5, 4, 6);
        mesh.AddTriangle(7, 5, 6);

        // i: 8, 9, 10, 11
        mesh.AddVertex(new(v0, Vector3.Right, Vector2.Zero));
        mesh.AddVertex(new(v4, Vector3.Right, Vector2.Zero));
        mesh.AddVertex(new(v6, Vector3.Right, Vector2.Zero));
        mesh.AddVertex(new(v2, Vector3.Right, Vector2.Zero));
        mesh.AddTriangle(9, 8, 10);
        mesh.AddTriangle(11, 10, 8);

        // i: 12, 13, 14, 15
        mesh.AddVertex(new(v1, Vector3.Left, Vector2.Zero));
        mesh.AddVertex(new(v5, Vector3.Left, Vector2.Zero));
        mesh.AddVertex(new(v3, Vector3.Left, Vector2.Zero));
        mesh.AddVertex(new(v7, Vector3.Left, Vector2.Zero));
        mesh.AddTriangle(14, 12, 13);
        mesh.AddTriangle(14, 13, 15);

        // i: 16, 17, 18, 19
        mesh.AddVertex(new(v0, Vector3.Up, Vector2.Zero));
        mesh.AddVertex(new(v1, Vector3.Up, Vector2.Zero));
        mesh.AddVertex(new(v4, Vector3.Up, Vector2.Zero));
        mesh.AddVertex(new(v5, Vector3.Up, Vector2.Zero));
        mesh.AddTriangle(17, 16, 18);
        mesh.AddTriangle(19, 17, 18);

        // i: 20, 21, 22, 23
        mesh.AddVertex(new(v2, Vector3.Down, Vector2.Zero));
        mesh.AddVertex(new(v3, Vector3.Down, Vector2.Zero));
        mesh.AddVertex(new(v6, Vector3.Down, Vector2.Zero));
        mesh.AddVertex(new(v7, Vector3.Down, Vector2.Zero));
        mesh.AddTriangle(22, 20, 21);
        mesh.AddTriangle(22, 21, 23);

        mesh.Bake();
        return mesh;
    }

    public static Mesh<VertexPositionColorTexture> Box(float sx, float sy, float sz, MTexture texture)
        => Box(sx, sy, sz, texture, texture, texture, texture, texture, texture);

    public static Mesh<VertexPositionColorTexture> Box(float sx, float sy, float sz, MTexture back, MTexture front, MTexture left, MTexture right, MTexture top, MTexture bottom)
    {
        Mesh<VertexPositionColorTexture> mesh = new();

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

    public static Mesh<VertexPCTN> Box(Vector3 offset, float sx, float sy, float sz, MTexture texture, Color color)
        => Box(offset, sx, sy, sz, texture, texture, texture, texture, texture, texture, color);

    public static IEnumerable<Tuple<Mesh<VertexPCTN>, Texture2D>> TileVoxelPCTN(char[,,] voxel)
    {
        Dictionary<Texture2D, Mesh<VertexPCTN>> meshes = new();
        Mesh<VertexPCTN> MeshByTexture(Texture2D texture)
        {
            if (meshes.TryGetValue(texture, out var mesh))
                return mesh;
            meshes[texture] = mesh = new();
            return mesh;
        }

        int sx = voxel.GetLength(2);
        int sy = voxel.GetLength(1);
        int sz = voxel.GetLength(0);
        Vector3 offset = new(sx * -4, sy * 4, sz * 4);

        Autotiler.Behaviour autotilerOptions = new();
        for (int z = 0; z < sz; z++)
        {
            VirtualMap<char> slice = new(sx, sy, '0');
            for (int y = 0; y < sy; y++)
                for (int x = 0; x < sx; x++)
                    slice[x, y] = voxel[z, y, x];
            VirtualMap<MTexture> tiles = GFX.FGAutotiler.GenerateMap(slice, autotilerOptions).TileGrid.Tiles;

            for (int y = 0; y < sy; y++)
            {
                for (int x = 0; x < sx; x++)
                {
                    MTexture texture = tiles[x, y];
                    if (texture is null) continue;

                    var mesh = MeshByTexture(texture.Texture.Texture_Safe);

                    if (z - 1 < 0 || voxel[z - 1, y, x] == '0')
                        mesh.AddQuadInverted
                        (
                            new(new Vector3(x * 8, y * -8, z * -8) + offset, Color.White, new(texture.LeftUV, texture.TopUV), Vector3.Forward),
                            new(new Vector3(x * 8 + 8, y * -8, z * -8) + offset, Color.White, new(texture.RightUV, texture.TopUV), Vector3.Forward),
                            new(new Vector3(x * 8, y * -8 - 8, z * -8) + offset, Color.White, new(texture.LeftUV, texture.BottomUV), Vector3.Forward),
                            new(new Vector3(x * 8 + 8, y * -8 - 8, z * -8) + offset, Color.White, new(texture.RightUV, texture.BottomUV), Vector3.Forward)
                        );

                    if (z + 1 == sz || voxel[z + 1, y, x] == '0')
                        mesh.AddQuad
                        (
                            new(new Vector3(x * 8, y * -8, z * -8 - 8) + offset, Color.White, new(texture.LeftUV, texture.TopUV), Vector3.Backward),
                            new(new Vector3(x * 8 + 8, y * -8, z * -8 - 8) + offset, Color.White, new(texture.RightUV, texture.TopUV), Vector3.Backward),
                            new(new Vector3(x * 8, y * -8 - 8, z * -8 - 8) + offset, Color.White, new(texture.LeftUV, texture.BottomUV), Vector3.Backward),
                            new(new Vector3(x * 8 + 8, y * -8 - 8, z * -8 - 8) + offset, Color.White, new(texture.RightUV, texture.BottomUV), Vector3.Backward)
                        );
                }
            }
        }

        for (int x = 0; x < sx; x++)
        {
            VirtualMap<char> slice = new(sz, sy, '0');
            for (int y = 0; y < sy; y++)
                for (int z = 0; z < sz; z++)
                    slice[z, y] = voxel[z, y, x];
            VirtualMap<MTexture> tiles = GFX.FGAutotiler.GenerateMap(slice, autotilerOptions).TileGrid.Tiles;

            for (int y = 0; y < sy; y++)
            {
                for (int z = 0; z < sz; z++)
                {
                    MTexture texture = tiles[z, y];
                    if (texture is null) continue;

                    var mesh = MeshByTexture(texture.Texture.Texture_Safe);

                    if (x - 1 < 0 || voxel[z, y, x - 1] == '0')
                        mesh.AddQuad
                        (
                            new(new Vector3(x * 8, y * -8, z * -8) + offset, Color.White, new(texture.LeftUV, texture.TopUV), Vector3.Right),
                            new(new Vector3(x * 8, y * -8, z * -8 - 8) + offset, Color.White, new(texture.RightUV, texture.TopUV), Vector3.Right),
                            new(new Vector3(x * 8, y * -8 - 8, z * -8) + offset, Color.White, new(texture.LeftUV, texture.BottomUV), Vector3.Right),
                            new(new Vector3(x * 8, y * -8 - 8, z * -8 - 8) + offset, Color.White, new(texture.RightUV, texture.BottomUV), Vector3.Right)
                        );

                    if (x + 1 == sx || voxel[z, y, x + 1] == '0')
                        mesh.AddQuadInverted
                        (
                            new(new Vector3(x * 8 + 8, y * -8, z * -8) + offset, Color.White, new(texture.LeftUV, texture.TopUV), Vector3.Left),
                            new(new Vector3(x * 8 + 8, y * -8, z * -8 - 8) + offset, Color.White, new(texture.RightUV, texture.TopUV), Vector3.Left),
                            new(new Vector3(x * 8 + 8, y * -8 - 8, z * -8) + offset, Color.White, new(texture.LeftUV, texture.BottomUV), Vector3.Left),
                            new(new Vector3(x * 8 + 8, y * -8 - 8, z * -8 - 8) + offset, Color.White, new(texture.RightUV, texture.BottomUV), Vector3.Left)
                        );
                }
            }
        }

        for (int y = 0; y < sy; y++)
        {
            VirtualMap<char> slice = new(sz, sx, '0');
            for (int z = 0; z < sz; z++)
                for (int x = 0; x < sx; x++)
                    slice[z, x] = voxel[z, y, x];
            VirtualMap<MTexture> tiles = GFX.FGAutotiler.GenerateMap(slice, autotilerOptions).TileGrid.Tiles;

            for (int z = 0; z < sz; z++)
            {
                for (int x = 0; x < sx; x++)
                {
                    MTexture texture = tiles[z, x];
                    if (texture is null) continue;

                    var mesh = MeshByTexture(texture.Texture.Texture_Safe);

                    if (y - 1 < 0 || voxel[z, y - 1, x] == '0')
                        mesh.AddQuad(
                            new(new Vector3(x * 8, y * -8, z * -8) + offset, Color.White, new(texture.LeftUV, texture.TopUV), Vector3.Down),
                            new(new Vector3(x * 8 + 8, y * -8, z * -8) + offset, Color.White, new(texture.LeftUV, texture.BottomUV), Vector3.Down),
                            new(new Vector3(x * 8, y * -8, z * -8 - 8) + offset, Color.White, new(texture.RightUV, texture.TopUV), Vector3.Down),
                            new(new Vector3(x * 8 + 8, y * -8, z * -8 - 8) + offset, Color.White, new(texture.RightUV, texture.BottomUV), Vector3.Down)
                        );

                    if (y + 1 == sy || voxel[z, y + 1, x] == '0')
                        mesh.AddQuadInverted(
                            new(new Vector3(x * 8, y * -8 - 8, z * -8) + offset, Color.White, new(texture.LeftUV, texture.TopUV), Vector3.Up),
                            new(new Vector3(x * 8 + 8, y * -8 - 8, z * -8) + offset, Color.White, new(texture.LeftUV, texture.BottomUV), Vector3.Up),
                            new(new Vector3(x * 8, y * -8 - 8, z * -8 - 8) + offset, Color.White, new(texture.RightUV, texture.TopUV), Vector3.Up),
                            new(new Vector3(x * 8 + 8, y * -8 - 8, z * -8 - 8) + offset, Color.White, new(texture.RightUV, texture.BottomUV), Vector3.Up)
                        );
                }
            }
        }

        return meshes.Select(kv =>
        {
            kv.Value.Bake();
            return Tuple.Create(kv.Value, kv.Key);
        }).ToArray();
    }

    public static IEnumerable<Tuple<Mesh<VertexPositionNormalTexture>, Texture2D>> TileVoxel(char[,,] voxel)
    {
        Dictionary<Texture2D, Mesh<VertexPositionNormalTexture>> meshes = new();
        Mesh<VertexPositionNormalTexture> MeshByTexture(Texture2D texture)
        {
            if (meshes.TryGetValue(texture, out var mesh))
                return mesh;
            meshes[texture] = mesh = new();
            return mesh;
        }

        int sx = voxel.GetLength(2);
        int sy = voxel.GetLength(1);
        int sz = voxel.GetLength(0);
        Vector3 offset = new(sx * -4, sy * 4, sz * 4);

        Autotiler.Behaviour autotilerOptions = new();
        for (int z = 0; z < sz; z++)
        {
            VirtualMap<char> slice = new(sx, sy, '0');
            for (int y = 0; y < sy; y++)
                for (int x = 0; x < sx; x++)
                    slice[x, y] = voxel[z, y, x];
            VirtualMap<MTexture> tiles = GFX.FGAutotiler.GenerateMap(slice, autotilerOptions).TileGrid.Tiles;

            for (int y = 0; y < sy; y++)
            {
                for (int x = 0; x < sx; x++)
                {
                    MTexture texture = tiles[x, y];
                    if (texture is null) continue;

                    var mesh = MeshByTexture(texture.Texture.Texture_Safe);

                    if (z - 1 < 0 || voxel[z - 1, y, x] == '0')
                        mesh.AddQuadInverted
                        (
                            new(new Vector3(x * 8, y * -8, z * -8) + offset, Vector3.Forward, new(texture.LeftUV, texture.TopUV)),
                            new(new Vector3(x * 8 + 8, y * -8, z * -8) + offset, Vector3.Forward, new(texture.RightUV, texture.TopUV)),
                            new(new Vector3(x * 8, y * -8 - 8, z * -8) + offset, Vector3.Forward, new(texture.LeftUV, texture.BottomUV)),
                            new(new Vector3(x * 8 + 8, y * -8 - 8, z * -8) + offset, Vector3.Forward, new(texture.RightUV, texture.BottomUV))
                        );

                    if (z + 1 == sz || voxel[z + 1, y, x] == '0')
                        mesh.AddQuad
                        (
                            new(new Vector3(x * 8, y * -8, z * -8 - 8) + offset, Vector3.Backward, new(texture.LeftUV, texture.TopUV)),
                            new(new Vector3(x * 8 + 8, y * -8, z * -8 - 8) + offset, Vector3.Backward, new(texture.RightUV, texture.TopUV)),
                            new(new Vector3(x * 8, y * -8 - 8, z * -8 - 8) + offset, Vector3.Backward, new(texture.LeftUV, texture.BottomUV)),
                            new(new Vector3(x * 8 + 8, y * -8 - 8, z * -8 - 8) + offset, Vector3.Backward, new(texture.RightUV, texture.BottomUV))
                        );
                }
            }
        }

        for (int x = 0; x < sx; x++)
        {
            VirtualMap<char> slice = new(sz, sy, '0');
            for (int y = 0; y < sy; y++)
                for (int z = 0; z < sz; z++)
                    slice[z, y] = voxel[z, y, x];
            VirtualMap<MTexture> tiles = GFX.FGAutotiler.GenerateMap(slice, autotilerOptions).TileGrid.Tiles;

            for (int y = 0; y < sy; y++)
            {
                for (int z = 0; z < sz; z++)
                {
                    MTexture texture = tiles[z, y];
                    if (texture is null) continue;

                    var mesh = MeshByTexture(texture.Texture.Texture_Safe);

                    if (x - 1 < 0 || voxel[z, y, x - 1] == '0')
                        mesh.AddQuad
                        (
                            new(new Vector3(x * 8, y * -8, z * -8) + offset, Vector3.Right, new(texture.LeftUV, texture.TopUV)),
                            new(new Vector3(x * 8, y * -8, z * -8 - 8) + offset, Vector3.Right, new(texture.RightUV, texture.TopUV)),
                            new(new Vector3(x * 8, y * -8 - 8, z * -8) + offset, Vector3.Right, new(texture.LeftUV, texture.BottomUV)),
                            new(new Vector3(x * 8, y * -8 - 8, z * -8 - 8) + offset, Vector3.Right, new(texture.RightUV, texture.BottomUV))
                        );

                    if (x + 1 == sx || voxel[z, y, x + 1] == '0')
                        mesh.AddQuadInverted
                        (
                            new(new Vector3(x * 8 + 8, y * -8, z * -8) + offset, Vector3.Left, new(texture.LeftUV, texture.TopUV)),
                            new(new Vector3(x * 8 + 8, y * -8, z * -8 - 8) + offset, Vector3.Left, new(texture.RightUV, texture.TopUV)),
                            new(new Vector3(x * 8 + 8, y * -8 - 8, z * -8) + offset, Vector3.Left, new(texture.LeftUV, texture.BottomUV)),
                            new(new Vector3(x * 8 + 8, y * -8 - 8, z * -8 - 8) + offset, Vector3.Left, new(texture.RightUV, texture.BottomUV))
                        );
                }
            }
        }

        for (int y = 0; y < sy; y++)
        {
            VirtualMap<char> slice = new(sz, sx, '0');
            for (int z = 0; z < sz; z++)
                for (int x = 0; x < sx; x++)
                    slice[z, x] = voxel[z, y, x];
            VirtualMap<MTexture> tiles = GFX.FGAutotiler.GenerateMap(slice, autotilerOptions).TileGrid.Tiles;

            for (int z = 0; z < sz; z++)
            {
                for (int x = 0; x < sx; x++)
                {
                    MTexture texture = tiles[z, x];
                    if (texture is null) continue;

                    var mesh = MeshByTexture(texture.Texture.Texture_Safe);

                    if (y - 1 < 0 || voxel[z, y - 1, x] == '0')
                        mesh.AddQuad(
                            new(new Vector3(x * 8, y * -8, z * -8) + offset, Vector3.Down, new(texture.LeftUV, texture.TopUV)),
                            new(new Vector3(x * 8 + 8, y * -8, z * -8) + offset, Vector3.Down, new(texture.LeftUV, texture.BottomUV)),
                            new(new Vector3(x * 8, y * -8, z * -8 - 8) + offset, Vector3.Down, new(texture.RightUV, texture.TopUV)),
                            new(new Vector3(x * 8 + 8, y * -8, z * -8 - 8) + offset, Vector3.Down, new(texture.RightUV, texture.BottomUV))
                        );

                    if (y + 1 == sy || voxel[z, y + 1, x] == '0')
                        mesh.AddQuadInverted(
                            new(new Vector3(x * 8, y * -8 - 8, z * -8) + offset, Vector3.Up, new(texture.LeftUV, texture.TopUV)),
                            new(new Vector3(x * 8 + 8, y * -8 - 8, z * -8) + offset, Vector3.Up, new(texture.LeftUV, texture.BottomUV)),
                            new(new Vector3(x * 8, y * -8 - 8, z * -8 - 8) + offset, Vector3.Up, new(texture.RightUV, texture.TopUV)),
                            new(new Vector3(x * 8 + 8, y * -8 - 8, z * -8 - 8) + offset, Vector3.Up, new(texture.RightUV, texture.BottomUV))
                        );
                }
            }
        }

        return meshes.Select(kv =>
        {
            kv.Value.Bake();
            return Tuple.Create(kv.Value, kv.Key);
        }).ToArray();
    }

    public static Mesh<VertexPCTN> Box(
        Vector3 offset,
        float sx, float sy, float sz,
        MTexture back, MTexture front,
        MTexture left, MTexture right,
        MTexture top, MTexture bottom,
        Color color)
    {
        Mesh<VertexPCTN> mesh = new();

        int o = 0;

        sx /= 2f;
        sy /= 2f;
        sz /= 2f;

        Vector3 v0 = offset + new Vector3(-sx, -sy, -sz);
        Vector3 v1 = offset + new Vector3(sx, -sy, -sz);
        Vector3 v2 = offset + new Vector3(-sx, sy, -sz);
        Vector3 v3 = offset + new Vector3(sx, sy, -sz);
        Vector3 v4 = offset + new Vector3(-sx, -sy, sz);
        Vector3 v5 = offset + new Vector3(sx, -sy, sz);
        Vector3 v6 = offset + new Vector3(-sx, sy, sz);
        Vector3 v7 = offset + new Vector3(sx, sy, sz);

        // i: 0, 1, 2, 3
        mesh.AddVertex(new(v0, color, new(back.RightUV, back.BottomUV), Vector3.Backward));
        mesh.AddVertex(new(v1, color, new(back.LeftUV, back.BottomUV), Vector3.Backward));
        mesh.AddVertex(new(v2, color, new(back.RightUV, back.TopUV), Vector3.Backward));
        mesh.AddVertex(new(v3, color, new(back.LeftUV, back.TopUV), Vector3.Backward));
        mesh.AddTriangle(o + 2, o + 0, o + 1);
        mesh.AddTriangle(o + 2, o + 1, o + 3);

        // i: 4, 5, 6, 7
        mesh.AddVertex(new(v4, color, new(front.LeftUV, front.BottomUV), Vector3.Forward));
        mesh.AddVertex(new(v5, color, new(front.RightUV, front.BottomUV), Vector3.Forward));
        mesh.AddVertex(new(v6, color, new(front.LeftUV, front.TopUV), Vector3.Forward));
        mesh.AddVertex(new(v7, color, new(front.RightUV, front.TopUV), Vector3.Forward));
        mesh.AddTriangle(o + 5, o + 4, o + 6);
        mesh.AddTriangle(o + 7, o + 5, o + 6);

        // i: 8, 9, 10, 11
        mesh.AddVertex(new(v0, color, new(left.LeftUV, left.BottomUV), Vector3.Right));
        mesh.AddVertex(new(v4, color, new(left.RightUV, left.BottomUV), Vector3.Right));
        mesh.AddVertex(new(v6, color, new(left.RightUV, left.TopUV), Vector3.Right));
        mesh.AddVertex(new(v2, color, new(left.LeftUV, left.TopUV), Vector3.Right));
        mesh.AddTriangle(o + 9, o + 8, o + 10);
        mesh.AddTriangle(o + 11, o + 10, o + 8);

        // i: 12, 13, 14, 15
        mesh.AddVertex(new(v1, color, new(right.RightUV, right.BottomUV), Vector3.Left));
        mesh.AddVertex(new(v5, color, new(right.LeftUV, right.BottomUV), Vector3.Left));
        mesh.AddVertex(new(v3, color, new(right.RightUV, right.TopUV), Vector3.Left));
        mesh.AddVertex(new(v7, color, new(right.LeftUV, right.TopUV), Vector3.Left));
        mesh.AddTriangle(o + 14, o + 12, o + 13);
        mesh.AddTriangle(o + 14, o + 13, o + 15);

        // i: 16, 17, 18, 19
        mesh.AddVertex(new(v0, color, new(bottom.LeftUV, bottom.BottomUV), Vector3.Up));
        mesh.AddVertex(new(v1, color, new(bottom.RightUV, bottom.BottomUV), Vector3.Up));
        mesh.AddVertex(new(v4, color, new(bottom.LeftUV, bottom.TopUV), Vector3.Up));
        mesh.AddVertex(new(v5, color, new(bottom.RightUV, bottom.TopUV), Vector3.Up));
        mesh.AddTriangle(o + 17, o + 16, o + 18);
        mesh.AddTriangle(o + 19, o + 17, o + 18);

        // i: 20, 21, 22, 23
        mesh.AddVertex(new(v2, color, new(top.LeftUV, top.TopUV), Vector3.Down));
        mesh.AddVertex(new(v3, color, new(top.RightUV, top.TopUV), Vector3.Down));
        mesh.AddVertex(new(v6, color, new(top.LeftUV, top.BottomUV), Vector3.Down));
        mesh.AddVertex(new(v7, color, new(top.RightUV, top.BottomUV), Vector3.Down));
        mesh.AddTriangle(o + 22, o + 20, o + 21);
        mesh.AddTriangle(o + 22, o + 21, o + 23);

        mesh.Bake();
        return mesh;
    }

    private static Mesh<VertexPCTN> BuildMesh(Vector3[] vertices, int[] indices, Color color, float rainbow = 0f, float scale = 1f)
    {
        Mesh<VertexPCTN> mesh = new();
        for (int i = 0; i < indices.Length; i += 3)
        {
            Vector3 v1 = vertices[indices[i]];
            Vector3 v2 = vertices[indices[i + 1]];
            Vector3 v3 = vertices[indices[i + 2]];

            Vector3 normal = Vector3.Normalize(Vector3.Cross(v3 - v1, v2 - v1));
            Vector3 n = (normal + Vector3.One) / 2f;
            Color c = Color.Lerp(color, new(n.X, n.Y, n.Z, 1.0f), rainbow);

            mesh.AddTriangle(i, i + 2, i + 1);
            mesh.AddVertices(
                new VertexPCTN(v1 * scale, c, Vector2.Zero, normal),
                new VertexPCTN(v2 * scale, c, Vector2.Zero, normal),
                new VertexPCTN(v3 * scale, c, Vector2.Zero, normal)
            );
        }

        mesh.Bake();
        return mesh;
    }

    public static Mesh<VertexPCTN> Icosahedron(Color color, float rainbow = 0f, float scale = 1f)
        => BuildMesh(icosahedron_vertices, icosahedron_indices, color, rainbow, scale);

    public static Mesh<VertexPCTN> Icosphere1(Color color, float rainbow = 0f, float scale = 1f)
        => BuildMesh(icosphere1_vertices, icosphere1_indices, color, rainbow, scale);

    public static Mesh<VertexPCTN> Rock(Color color, float rainbow = 0f, float scale = 1f)
    {
        var vertices = icosahedron_vertices.Select(v =>
        {
            float p = 1 + (Calc.Random.NextFloat() * 2 - 1) * 0.6f;
            return v * p /* * new Vector3(0.75f * p, 1.25f, 0.75f * p) */;
        }).ToArray();
        return BuildMesh(vertices, icosahedron_indices, color, rainbow, scale);
    }

    public static Mesh<VertexPCTN> HalfRing(float height, float thickness, Color color)
    {
        const int circSub = 16;
        const int ringSub = 4;
        const int len = circSub * ringSub;

        thickness /= 2.0f;

        Vector3[] vertices = new Vector3[(circSub + 1) * ringSub];
        int[] indices = new int[6 * len];

        static Vector3 Circle(Vector3 x, Vector3 y, float angle)
            => (float) Math.Cos(angle) * x + (float) Math.Sin(angle) * y;

        int index = 0;
        for (int i = 0; i <= circSub; i++)
        {
            Vector3 axis = Circle(Vector3.UnitZ * 16, Vector3.UnitY * height / 2f, MathHelper.Pi * i / circSub - MathHelper.PiOver2);
            for (int j = 0; j < ringSub; j++)
            {
                vertices[i * ringSub + j] = axis + Circle(axis.SafeNormalize(), Vector3.UnitX, MathHelper.TwoPi * j / ringSub) * thickness;

                if (i == circSub)
                    continue;

                int a = i * ringSub + ((j + 0) % ringSub) + 0 * ringSub;
                int b = i * ringSub + ((j + 1) % ringSub) + 0 * ringSub;
                int c = i * ringSub + ((j + 0) % ringSub) + 1 * ringSub;
                int d = i * ringSub + ((j + 1) % ringSub) + 1 * ringSub;

                indices[index++] = a;
                indices[index++] = b;
                indices[index++] = c;
                indices[index++] = b;
                indices[index++] = d;
                indices[index++] = c;
            }
        }

        return BuildMesh(vertices, indices, color, 0.1f);
    }

    /// <summary>
    /// Generates vertices and indices for an n-icosphere.
    /// </summary>
    /// <param name="n">The number of subdivisions.</param>
    /// <returns>A tuple containing the vertices in a <see cref="Vector3[]"/> array, and the indices in an <see cref="int[]"/> array.</returns>
    public static Tuple<Vector3[], int[]> GenerateIcosphereGeometry(int n)
    {
        List<Vector3> vertices = new()
        {
            new Vector3(-1,  phi, 0),
            new Vector3( 1,  phi, 0),
            new Vector3(-1, -phi, 0),
            new Vector3( 1, -phi, 0),
            new Vector3( 0, -1,  phi),
            new Vector3( 0,  1,  phi),
            new Vector3( 0, -1, -phi),
            new Vector3( 0,  1, -phi),
            new Vector3( phi,  0, -1),
            new Vector3( phi,  0,  1),
            new Vector3(-phi,  0, -1),
            new Vector3(-phi,  0,  1)
        };
        List<int[]> faces = new()
        {
            new int[] { 0, 11, 5 },
            new int[] { 0, 5, 1 },
            new int[] { 0, 1, 7 },
            new int[] { 0, 7, 10 },
            new int[] { 0, 10, 11 },
            new int[] { 1, 5, 9 },
            new int[] { 5, 11, 4 },
            new int[] { 11, 10, 2 },
            new int[] { 10, 7, 6 },
            new int[] { 7, 1, 8 },
            new int[] { 3, 9, 4 },
            new int[] { 3, 4, 2 },
            new int[] { 3, 2, 6 },
            new int[] { 3, 6, 8 },
            new int[] { 3, 8, 9 },
            new int[] { 4, 9, 5 },
            new int[] { 2, 4, 11 },
            new int[] { 6, 2, 10 },
            new int[] { 8, 6, 7 },
            new int[] { 9, 8, 1 }
        };

        // subdivision
        for (int _ = 0; _ < n; ++_)
        {
            List<int[]> newFaces = new();
            Dictionary<long, int> cache = new();

            int GetMiddlePointIndex(int a, int b)
            {
                // check if the middle point has already been computed
                bool inverted = a < b;
                long lower = inverted ? a : b;
                long upper = inverted ? b : a;
                long key = (lower << 32) + upper;
                if (cache.TryGetValue(key, out int index))
                    return index;

                // if the middle point hasn't been computed yet, compute it
                Vector3 va = vertices[a];
                Vector3 vb = vertices[b];
                Vector3 mid = new Vector3(va.X + vb.X, va.Y + vb.Y, va.Z + vb.Z) / 2f;

                // add the new vertex to the list and return its index, which we also cache here
                index = vertices.Count;
                vertices.Add(mid);
                cache.Add(key, index);
                return index;
            }

            foreach (int[] face in faces)
            {
                // current face (triangle) indices
                int a = face[0];
                int b = face[1];
                int c = face[2];

                // get indices of midpoints / create them if new
                int ab = GetMiddlePointIndex(a, b);
                int bc = GetMiddlePointIndex(b, c);
                int ca = GetMiddlePointIndex(c, a);

                // append 4 new faces
                newFaces.Add(new int[] { a, ab, ca });
                newFaces.Add(new int[] { b, bc, ab });
                newFaces.Add(new int[] { c, ca, bc });
                newFaces.Add(new int[] { ab, bc, ca });
            }

            // replace the intial indices with the subdivision result
            faces = newFaces;
        }

        // normalize
        for (int i = 0; i < vertices.Count; i++)
            vertices[i] = Vector3.Normalize(vertices[i]);

        return Tuple.Create(vertices.ToArray(), faces.SelectMany(_ => _).ToArray());
    }
}
