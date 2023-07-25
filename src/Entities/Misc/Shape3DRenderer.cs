using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Entities;

[Tracked]
public sealed class Shape3DRenderer : Entity
{
    private enum Framework { FNA, XNA }

    private static Framework type;

    private const int SCREEN_WIDTH = 320;
    private const int SCREEN_HEIGHT = 180;

    private readonly HashSet<Shape3D> shapes = new();

    private RenderTarget2D albedo, depth, normal, final;

    public Shape3DRenderer(int depth)
    {
        Tag = Tags.Global | Tags.TransitionUpdate;
        Active = false;
        Visible = true;

        Depth = depth;

        Add(new BeforeRenderHook(BeforeRender));

        Logger.Log(LogLevel.Info, nameof(Shape3DRenderer), $"new 3d renderer created, @ depth {depth}");
    }

    public void Track(Shape3D shape) => shapes.Add(shape);
    public void Untrack(Shape3D shape) => shapes.Remove(shape);

    public override void Added(Scene scene)
    {
        base.Added(scene);

        albedo = new RenderTarget2D(Engine.Graphics.GraphicsDevice, SCREEN_WIDTH, SCREEN_HEIGHT, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
        depth = new RenderTarget2D(Engine.Graphics.GraphicsDevice, SCREEN_WIDTH, SCREEN_HEIGHT, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
        normal = new RenderTarget2D(Engine.Graphics.GraphicsDevice, SCREEN_WIDTH, SCREEN_HEIGHT, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
        final = new RenderTarget2D(Engine.Graphics.GraphicsDevice, SCREEN_WIDTH, SCREEN_HEIGHT, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
    }

    public override void Removed(Scene scene)
    {
        base.Removed(scene);
        Clean();
    }

    public override void SceneEnd(Scene scene)
    {
        base.SceneEnd(scene);
        Clean();
    }

    private void Clean()
    {
        albedo?.Dispose();
        albedo = null;
        depth?.Dispose();
        depth = null;
        normal?.Dispose();
        normal = null;
        final?.Dispose();
        final = null;
    }

    private void BeforeRender()
    {
        var prevTexture1 = Engine.Graphics.GraphicsDevice.Textures[1];
        var prevTexture2 = Engine.Graphics.GraphicsDevice.Textures[2];

        // WHAT THE FUCK ????????????????????
        Vector2 renderOffset = type is Framework.FNA
            ? Vector2.Zero
            : -Vector2.One * 0.5f;

        Draw.SpriteBatch.GraphicsDevice.SetRenderTargets(new(albedo), new(depth), new(normal));
        Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);

        Engine.Instance.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
        Engine.Instance.GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
        Engine.Instance.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        Engine.Instance.GraphicsDevice.BlendState = BlendState.Opaque;

        const float far = 1200;
        Matrix proj = Matrix.CreateOrthographic(320, 180, 1, far);
        Matrix view = Matrix.CreateLookAt(Vector3.Backward * far / 2, Vector3.Zero, Vector3.Up);

        CommunalHelperGFX.PCTN_MRT.Parameters["view"].SetValue(view);
        CommunalHelperGFX.PCTN_MRT.Parameters["proj"].SetValue(proj);

        Vector2 cam = (Scene as Level).Camera.Position + new Vector2(160, 90);

        foreach (Shape3D shape in shapes)
        {
            if (!shape.Visible || !shape.Entity.Visible)
                continue;

            Vector2 pos = shape.Entity.Position + shape.Position.XY() + renderOffset;
            if (shape.Entity is Platform platform)
                pos += platform.Shake;

            Matrix world = shape.Matrix * Matrix.CreateTranslation(pos.X - cam.X, cam.Y - pos.Y, shape.Position.Z);
            CommunalHelperGFX.PCTN_MRT.Parameters["world"].SetValue(world);

            CommunalHelperGFX.PCTN_MRT.Parameters["tint"].SetValue(shape.Tint);
            CommunalHelperGFX.PCTN_MRT.Parameters["depth_edge_strength"].SetValue(shape.DepthEdgeStrength);
            CommunalHelperGFX.PCTN_MRT.Parameters["normal_edge_strength"].SetValue(shape.NormalEdgeStrength);
            CommunalHelperGFX.PCTN_MRT.Parameters["rainbow"].SetValue(shape.RainbowMix);
            CommunalHelperGFX.PCTN_MRT.Parameters["highlight_lower_bound"].SetValue(shape.HighlightLowerBound);
            CommunalHelperGFX.PCTN_MRT.Parameters["highlight_upper_bound"].SetValue(shape.HighlightUpperBound);
            CommunalHelperGFX.PCTN_MRT.Parameters["highlight_strength"].SetValue(shape.HighlightStrength);

            var textureParameter = CommunalHelperGFX.PCTN_MRT.Parameters["atlas_texture"];
            textureParameter.SetValue(shape.Texture);

            EffectPass pass = CommunalHelperGFX.PCTN_MRT.CurrentTechnique.Passes[0];
            foreach (var pair in shape.Meshes)
            {
                if (pair.Item2 is not null)
                    textureParameter.SetValue(pair.Item2);
                pass.Apply();
                pair.Item1.Draw();
            }
        }

        Draw.SpriteBatch.GraphicsDevice.SetRenderTarget(final);
        Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);
        
        CommunalHelperGFX.PCTN_COMPOSE.Parameters["albedo_texture"].SetValue(albedo);
        CommunalHelperGFX.PCTN_COMPOSE.Parameters["depth_texture"].SetValue(depth);
        CommunalHelperGFX.PCTN_COMPOSE.Parameters["normal_texture"].SetValue(normal);
        CommunalHelperGFX.PCTN_COMPOSE.Parameters["time"].SetValue(Scene.TimeActive * 10);
        CommunalHelperGFX.PCTN_COMPOSE.Parameters["MatrixTransform"]
            .SetValue(
                Matrix.CreateOrthographic(SCREEN_WIDTH, SCREEN_HEIGHT, 0, 1) *
                Matrix.CreateTranslation(-1.0f, -1.0f, 0) * Matrix.CreateScale(1, -1, 1)
            );

        Engine.Graphics.GraphicsDevice.SamplerStates[0] = SamplerState.PointWrap;
        Engine.Graphics.GraphicsDevice.SamplerStates[1] = SamplerState.PointWrap;
        Engine.Graphics.GraphicsDevice.SamplerStates[2] = SamplerState.PointWrap;
        
        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, CommunalHelperGFX.PCTN_COMPOSE);
        Draw.SpriteBatch.Draw(albedo, renderOffset, Color.White);
        Draw.SpriteBatch.End();

        Engine.Graphics.GraphicsDevice.Textures[1] = prevTexture1;
        Engine.Graphics.GraphicsDevice.Textures[2] = prevTexture2;
        
        Engine.Graphics.GraphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;
        Engine.Graphics.GraphicsDevice.SamplerStates[1] = SamplerState.LinearClamp;
        Engine.Graphics.GraphicsDevice.SamplerStates[2] = SamplerState.LinearClamp;
    }

    public override void Render()
    {
        Vector2 cam = (Scene as Level).Camera.Position;
        Draw.SpriteBatch.Draw(final, cam, Color.White);
    }

    public static Shape3DRenderer Get3DRenderer(Scene scene, int depth)
    {
        var renderer = scene.Tracker.GetEntities<Shape3DRenderer>()
                        .FirstOrDefault(r => r.Depth == depth)
                        as Shape3DRenderer;
        return renderer;
    }

    internal static void Load()
    {
        type = typeof(Game).Assembly.FullName.Contains("FNA")
            ? Framework.FNA
            : Framework.XNA;
        On.Celeste.LevelLoader.LoadingThread += LevelLoader_LoadingThread;
    }

    internal static void Unload()
    {
        On.Celeste.LevelLoader.LoadingThread -= LevelLoader_LoadingThread;
    }

    private static void LevelLoader_LoadingThread(On.Celeste.LevelLoader.orig_LoadingThread orig, LevelLoader self)
    {
        orig(self);

        self.Level.Add(
            new Shape3DRenderer(Depths.BGTerrain),
            new Shape3DRenderer(Depths.FGTerrain)
        );
    }
}
