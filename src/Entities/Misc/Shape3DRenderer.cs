using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Entities.Misc;

[Tracked]
public sealed class Shape3DRenderer : Entity
{
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
        var samplerState0 = Engine.Graphics.GraphicsDevice.SamplerStates[0];
        var samplerState1 = Engine.Graphics.GraphicsDevice.SamplerStates[1];
        var samplerState2 = Engine.Graphics.GraphicsDevice.SamplerStates[2];

        Draw.SpriteBatch.GraphicsDevice.SetRenderTargets(new(albedo), new(depth), new(normal));
        Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);

        Engine.Instance.GraphicsDevice.SamplerStates[0] = SamplerState.PointWrap;
        Engine.Instance.GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
        Engine.Instance.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        Engine.Instance.GraphicsDevice.BlendState = BlendState.Opaque;

        const float far = 1200;
        Matrix proj = Matrix.CreateOrthographic(320, 180, 0, far);
        Matrix view = Matrix.CreateLookAt(Vector3.Backward * far / 2f, Vector3.Zero, Vector3.Up);

        CommunalHelperGFX.PCTN_MRT.Parameters["view"].SetValue(view);
        CommunalHelperGFX.PCTN_MRT.Parameters["proj"].SetValue(proj);

        Vector2 cam = (Scene as Level).Camera.Position + new Vector2(160, 90);

        foreach (Shape3D shape in shapes)
        {
            if (!shape.Visible || !shape.Entity.Visible)
                continue;

            Vector2 pos = shape.Entity.Position + shape.Position.XY();

            Matrix world = shape.Matrix * Matrix.CreateTranslation(pos.X - cam.X, cam.Y - pos.Y, shape.Position.Z);
            CommunalHelperGFX.PCTN_MRT.Parameters["world"].SetValue(world);

            CommunalHelperGFX.PCTN_MRT.Parameters["tint"].SetValue(shape.Tint);
            CommunalHelperGFX.PCTN_MRT.Parameters["depth_edge_strength"].SetValue(shape.DepthEdgeStrength);
            CommunalHelperGFX.PCTN_MRT.Parameters["normal_edge_strength"].SetValue(shape.NormalEdgeStrength);
            CommunalHelperGFX.PCTN_MRT.Parameters["rainbow"].SetValue(shape.RainbowMix);
            CommunalHelperGFX.PCTN_MRT.Parameters["highlight_lower_bound"].SetValue(shape.HighlightLowerBound);
            CommunalHelperGFX.PCTN_MRT.Parameters["highlight_upper_bound"].SetValue(shape.HighlightUpperBound);
            CommunalHelperGFX.PCTN_MRT.Parameters["highlight_strength"].SetValue(shape.HighlightStrength);

            CommunalHelperGFX.PCTN_MRT.Parameters["atlas_texture"].SetValue(shape.Texture);

            foreach (EffectPass pass in CommunalHelperGFX.PCTN_MRT.CurrentTechnique.Passes)
            {
                pass.Apply();
                shape.Mesh.Draw();
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
                Matrix.CreateOrthographic(Engine.Graphics.GraphicsDevice.Viewport.Width, Engine.Graphics.GraphicsDevice.Viewport.Height, 0, 1) *
                Matrix.CreateTranslation(-1.0f, -1.0f, 0) * Matrix.CreateScale(1, -1, 1)
            );


        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, CommunalHelperGFX.PCTN_COMPOSE);
        Draw.SpriteBatch.Draw(albedo, new Rectangle(0, 0, SCREEN_WIDTH, SCREEN_HEIGHT), Color.White);
        Draw.SpriteBatch.End();

        Engine.Graphics.GraphicsDevice.SamplerStates[0] = samplerState0;
        Engine.Graphics.GraphicsDevice.SamplerStates[1] = samplerState1;
        Engine.Graphics.GraphicsDevice.SamplerStates[2] = samplerState2;
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
