using Celeste.Mod.CommunalHelper.Components;
using Celeste.Mod.CommunalHelper.DashStates;
using Celeste.Mod.CommunalHelper.States;
using Celeste.Mod.CommunalHelper.Utils;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.ModInterop;
using System.Collections.Generic;
using DreamTunnelDash = Celeste.Mod.CommunalHelper.DashStates.DreamTunnelDash;

namespace Celeste.Mod.CommunalHelper;

public static class ModExports
{
    internal static void Initialize()
    {
        typeof(DashStates).ModInterop();
        typeof(Entities).ModInterop();
        typeof(Shape3DExports).ModInterop();
    }

    [ModExportName("CommunalHelper.DashStates")]
    public static class DashStates
    {
        #region DreamTunnel

        public static int GetDreamTunnelDashState()
        {
            return St.DreamTunnelDash;
        }

        public static bool HasDreamTunnelDash()
        {
            return DreamTunnelDash.DreamTunnelDashCount > 0;
        }

        public static int GetDreamTunnelDashCount()
        {
            return DreamTunnelDash.DreamTunnelDashCount;
        }

        public static Component DreamTunnelInteraction(Action<Player> onPlayerEnter, Action<Player> onPlayerExit)
        {
            return new DreamTunnelInteraction(onPlayerEnter, onPlayerExit);
        }

        #endregion

        #region Seeker

        public static bool HasSeekerDash()
        {
            return SeekerDash.HasSeekerDash;
        }

        public static bool IsSeekerDashAttacking()
        {
            return SeekerDash.SeekerAttacking;
        }

        #endregion
    }

    [ModExportName("CommunalHelper.Entities")]
    public static class Entities
    {
        #region Misc

        #region Melvin

        public static Component MelvinTargetable(int priority)
        {
            return new MelvinTargetable(priority);
        }

        #endregion

        #endregion
    }

    [ModExportName("CommunalHelper.Shape3DExports")]
    public static class Shape3DExports
    {
        #region Shape3D

        public static Component CreateShape3D(object mesh)
        {
            if (mesh is Mesh<VertexPCTN>)
            {
                return new Shape3D(mesh as Mesh<VertexPCTN>)
                {
                    //Whatever the default Texture is, it gives shapes a blue-ish tint. This fixes that issue
                    Texture = CommunalHelperGFX.Blank
                };
            }
            else if (mesh is IEnumerable<Tuple<Mesh<VertexPCTN>, Texture2D>>)
            {
                return new Shape3D(mesh as IEnumerable<Tuple<Mesh<VertexPCTN>, Texture2D>>)
                {
                    //Whatever the default Texture is, it gives shapes a blue-ish tint. This fixes that issue
                    Texture = CommunalHelperGFX.Blank
                };
            }
            throw new ArgumentException($@"Argument ""mesh"" was of type ""{mesh.GetType()}"", instead of the expected types ""{typeof(Mesh<VertexPCTN>)}"" or ""{typeof(IEnumerable<Tuple<Mesh<VertexPCTN>, Texture2D>>)}""");
        }

        public static float GetRainbowMix(Component shape3d)
        {
            if (shape3d is Shape3D shape)
            {
                return shape.RainbowMix;
            }
            throw new ArgumentException($@"Argument ""shape3d"" was of type ""{shape3d.GetType()}"", instead of the expected type ""{typeof(Shape3D)}""");
        }
        public static void SetRainbowMix(Component shape3d, float value)
        {
            if (shape3d is Shape3D shape)
            {
                shape.RainbowMix = value; 
                return;
            }
            throw new ArgumentException($@"Argument ""shape3d"" was of type ""{shape3d.GetType()}"", instead of the expected type ""{typeof(Shape3D)}""");
        }

        public static Texture2D GetTexture(Component shape3d)
        {
            if (shape3d is Shape3D shape)
            {
                return shape.Texture;
            }
            throw new ArgumentException($@"Argument ""shape3d"" was of type ""{shape3d.GetType()}"", instead of the expected type ""{typeof(Shape3D)}""");
        }
        public static void SetTexture(Component shape3d, Texture2D texture)
        {
            if (shape3d is Shape3D shape)
            {
                shape.Texture = texture;
                return;
            }
            throw new ArgumentException($@"Argument ""shape3d"" was of type ""{shape3d.GetType()}"", instead of the expected type ""{typeof(Shape3D)}""");
        }

        public static int GetDepth(Component shape3d)
        {
            if (shape3d is Shape3D shape)
            {
                return shape.Depth;
            }
            throw new ArgumentException($@"Argument ""shape3d"" was of type ""{shape3d.GetType()}"", instead of the expected type ""{typeof(Shape3D)}""");
        }
        public static void SetDepth(Component shape3d, int value)
        {
            if (shape3d is Shape3D shape)
            {
                shape.Depth = value;
                return;
            }
            throw new ArgumentException($@"Argument ""shape3d"" was of type ""{shape3d.GetType()}"", instead of the expected type ""{typeof(Shape3D)}""");
        }

        public static Vector4 GetTint(Component shape3d)
        {
            if (shape3d is Shape3D shape)
            {
                return shape.Tint;
            }
            throw new ArgumentException($@"Argument ""shape3d"" was of type ""{shape3d.GetType()}"", instead of the expected type ""{typeof(Shape3D)}""");
        }
        public static void SetTint(Component shape3d, Vector4 tint)
        {
            if (shape3d is Shape3D shape)
            {
                shape.Tint = tint;
                return;
            }
            throw new ArgumentException($@"Argument ""shape3d"" was of type ""{shape3d.GetType()}"", instead of the expected type ""{typeof(Shape3D)}""");
        }

        public static void SetTint_Color(Component shape3d, Color tint)
        {
            if (shape3d is Shape3D shape)
            {
                shape.SetTint(tint);
                return;
            }
            throw new ArgumentException($@"Argument ""shape3d"" was of type ""{shape3d.GetType()}"", instead of the expected type ""{typeof(Shape3D)}""");
        }

        public static Vector3 GetPosition(Component shape3d)
        {
            if (shape3d is Shape3D shape)
            {
                return shape.Position;
            }
            throw new ArgumentException($@"Argument ""shape3d"" was of type ""{shape3d.GetType()}"", instead of the expected type ""{typeof(Shape3D)}""");
        }
        public static void SetPosition(Component shape3d, Vector3 pos)
        {
            if (shape3d is Shape3D shape)
            {
                shape.Position = pos;
                return;
            }
            throw new ArgumentException($@"Argument ""shape3d"" was of type ""{shape3d.GetType()}"", instead of the expected type ""{typeof(Shape3D)}""");
        }

        public static float GetHighlightStrength(Component shape3d)
        {
            if (shape3d is Shape3D shape)
            {
                return shape.HighlightStrength;
            }
            throw new ArgumentException($@"Argument ""shape3d"" was of type ""{shape3d.GetType()}"", instead of the expected type ""{typeof(Shape3D)}""");
        }
        public static void SetHighlightStrength(Component shape3d, float value)
        {
            if (shape3d is Shape3D shape)
            {
                shape.HighlightStrength = value;
                return;
            }
            throw new ArgumentException($@"Argument ""shape3d"" was of type ""{shape3d.GetType()}"", instead of the expected type ""{typeof(Shape3D)}""");
        }

        public static float GetDepthEdgeStrength(Component shape3d)
        {
            if (shape3d is Shape3D shape)
            {
                return shape.DepthEdgeStrength;
            }
            throw new ArgumentException($@"Argument ""shape3d"" was of type ""{shape3d.GetType()}"", instead of the expected type ""{typeof(Shape3D)}""");
        }
        public static void SetDepthEdgeStrength(Component shape3d, float value)
        {
            if (shape3d is Shape3D shape)
            {
                shape.DepthEdgeStrength = value;
                return;
            }
            throw new ArgumentException($@"Argument ""shape3d"" was of type ""{shape3d.GetType()}"", instead of the expected type ""{typeof(Shape3D)}""");
        }

        public static float GetNormalEdgeStrength(Component shape3d)
        {
            if (shape3d is Shape3D shape)
            {
                return shape.NormalEdgeStrength;
            }
            throw new ArgumentException($@"Argument ""shape3d"" was of type ""{shape3d.GetType()}"", instead of the expected type ""{typeof(Shape3D)}""");
        }
        public static void SetNormalEdgeStrength(Component shape3d, float value)
        {
            if (shape3d is Shape3D shape)
            {
                shape.NormalEdgeStrength = value;
                return;
            }
            throw new ArgumentException($@"Argument ""shape3d"" was of type ""{shape3d.GetType()}"", instead of the expected type ""{typeof(Shape3D)}""");
        }

        public static float GetHighlightUpperBound(Component shape3d)
        {
            if (shape3d is Shape3D shape)
            {
                return shape.HighlightUpperBound;
            }
            throw new ArgumentException($@"Argument ""shape3d"" was of type ""{shape3d.GetType()}"", instead of the expected type ""{typeof(Shape3D)}""");
        }
        public static void SetHighlightUpperBound(Component shape3d, float value)
        {
            if (shape3d is Shape3D shape)
            {
                shape.HighlightUpperBound = value;
                return;
            }
            throw new ArgumentException($@"Argument ""shape3d"" was of type ""{shape3d.GetType()}"", instead of the expected type ""{typeof(Shape3D)}""");
        }

        public static float GetHighlightLowerBound(Component shape3d)
        {
            if (shape3d is Shape3D shape)
            {
                return shape.HighlightLowerBound;
            }
            throw new ArgumentException($@"Argument ""shape3d"" was of type ""{shape3d.GetType()}"", instead of the expected type ""{typeof(Shape3D)}""");
        }
        public static void SetHighlightLowerBound(Component shape3d, float value)
        {
            if (shape3d is Shape3D shape)
            {
                shape.HighlightLowerBound = value;
                return;
            }
            throw new ArgumentException($@"Argument ""shape3d"" was of type ""{shape3d.GetType()}"", instead of the expected type ""{typeof(Shape3D)}""");
        }

        public static Matrix GetMatrix(Component shape3d)
        {
            if (shape3d is Shape3D shape)
            {
                return shape.Matrix;
            }
            throw new ArgumentException($@"Argument ""shape3d"" was of type ""{shape3d.GetType()}"", instead of the expected type ""{typeof(Shape3D)}""");
        }
        public static void SetMatrix(Component shape3d, Matrix matrix)
        {
            if (shape3d is Shape3D shape)
            {
                shape.Matrix = matrix;
                return;
            }
            throw new ArgumentException($@"Argument ""shape3d"" was of type ""{shape3d.GetType()}"", instead of the expected type ""{typeof(Shape3D)}""");
        }

        #endregion

        #region Shapes

        public static object CreateBox(float sx, float sy, float sz, MTexture texture)
        {
            return Shapes.Box(sx, sy, sz, texture);
        }
        public static object CreateBox_Offset_Color(Vector3 offset, float sx, float sy, float sz, MTexture texture, Color tint)
        {
            return Shapes.Box(offset, sx, sy, sz, texture, tint);
        }
        public static object CreateBox_SideTextures(float sx, float sy, float sz, MTexture back, MTexture front, MTexture left, MTexture right, MTexture top, MTexture bottom)
        {
            return Shapes.Box(sx, sy, sz, back, front, left, right, top, bottom);
        }
        public static object CreateBox_Offset_Color_SideTextures(Vector3 offset, float sx, float sy, float sz, MTexture back, MTexture front, MTexture left, MTexture right, MTexture top, MTexture bottom, Color tint)
        {
            return Shapes.Box(offset, sx, sy, sz, back, front, left, right, top, bottom, tint);
        }
        public static object CreateGear(float teeth, float depth, float slope, float innerRadius, float thickness, float scale, Color color)
        {
            return Shapes.Gear(teeth, depth, slope, innerRadius, thickness, scale, color);
        }
        public static object CreateTileVoxel(char[,,] voxel)
        {
            return Shapes.TileVoxelPCTN(voxel);
        }
        public static object CreateArbitraryMesh(Vector3[] vertices, int[] indices, Color color, float rainbow = 0f, float scale = 1f)
        {
            return Shapes.BuildMesh(vertices, indices, color, rainbow, scale);
        }
        public static object CreateIcosahedron(Color color, float rainbow = 0f, float scale = 1f)
        {
            return Shapes.Icosahedron(color, rainbow, scale);
        }
        public static object CreateIcosphere1(Color color, float rainbow = 0f, float scale = 1f)
        {
            return Shapes.Icosphere1(color, rainbow, scale);
        }
        public static object CreateRock(Color color, float rainbow = 0f, float scale = 1f)
        {
            return Shapes.Rock(color, rainbow, scale);
        }
        public static object CreateHalfRing(float height, float thickness, Color color)
        {
            return Shapes.HalfRing(height, thickness, color);
        }

        #endregion
    }
}
