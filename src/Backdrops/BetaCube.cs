using Celeste.Mod.Backdrops;
using Celeste.Mod.CommunalHelper.Utils;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Backdrops;

[CustomBackdrop("CommunalHelper/BetaCube")]
public class BetaCube : Backdrop
{
    private static BasicEffect shader;
    private VirtualRenderTarget buffer;

    private readonly Mesh<VertexPositionColorTexture> cube;

    private readonly MTexture face;
    private readonly float scale;
    private readonly Color[] colors;

    public BetaCube(BinaryPacker.Element data)
        : base()
    {
        face = GFX.Game[data.Attr("texture", "backdrops/CommunalHelper/betacube")];
        scale = data.AttrFloat("scale", 1.0f);
        colors = data.Attr("colors", "ff172b,fda32c,298ca4,2f25fb")
                     .Split(',')
                     .Select(Calc.HexToColor)
                     .ToArray();

        cube = Shapes.Box(96, 96, 96, face);

        UpdateColors();
    }

    private void UpdateColors()
    {
        cube.Vertices[0].Color = cube.Vertices[8].Color = cube.Vertices[16].Color = Util.ColorArrayLerp(Engine.Scene.TimeActive * 1 - 0, colors);
        cube.Vertices[1].Color = cube.Vertices[12].Color = cube.Vertices[17].Color = Util.ColorArrayLerp(Engine.Scene.TimeActive * 2 + 3, colors);
        cube.Vertices[2].Color = cube.Vertices[11].Color = cube.Vertices[20].Color = Util.ColorArrayLerp(Engine.Scene.TimeActive * 3 - 5, colors);
        cube.Vertices[3].Color = cube.Vertices[14].Color = cube.Vertices[21].Color = Util.ColorArrayLerp(Engine.Scene.TimeActive * 2 + 7, colors);
        cube.Vertices[4].Color = cube.Vertices[9].Color = cube.Vertices[18].Color = Util.ColorArrayLerp(Engine.Scene.TimeActive * -1 - 11, colors);
        cube.Vertices[5].Color = cube.Vertices[13].Color = cube.Vertices[19].Color = Util.ColorArrayLerp(Engine.Scene.TimeActive * -2 + 11, colors);
        cube.Vertices[6].Color = cube.Vertices[10].Color = cube.Vertices[22].Color = Util.ColorArrayLerp(Engine.Scene.TimeActive * -3 - 13, colors);
        cube.Vertices[7].Color = cube.Vertices[15].Color = cube.Vertices[23].Color = Util.ColorArrayLerp(Engine.Scene.TimeActive * -2 + 17, colors);
    }

    public override void Update(Scene scene)
    {
        base.Update(scene);
        UpdateColors();
    }

    public override void BeforeRender(Scene scene)
    {
        base.BeforeRender(scene);

        if (buffer == null || buffer.IsDisposed)
            buffer = VirtualContent.CreateRenderTarget("elytrahelper-betacube", 320, 180, false);

        Engine.Graphics.GraphicsDevice.SetRenderTarget(buffer);
        Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);
        Engine.Graphics.GraphicsDevice.SamplerStates[0] = SamplerState.PointWrap;
        Engine.Instance.GraphicsDevice.RasterizerState = RasterizerState.CullClockwise;

        shader.Texture = face.Texture.Texture_Safe;

        float time = Engine.Scene.TimeActive * 0.6f;
        shader.World = Matrix.CreateFromYawPitchRoll(time, time, time) * Matrix.CreateScale(scale);

        foreach (EffectPass pass in shader.CurrentTechnique.Passes)
        {
            pass.Apply();
            Engine.Instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, cube.Vertices, 0, cube.VertexCount, cube.Indices, 0, cube.Triangles);
        }
    }

    public override void Render(Scene scene)
    {
        base.Render(scene);

        if (buffer is not null && !buffer.IsDisposed)
            Draw.SpriteBatch.Draw(buffer, Vector2.Zero, Color.White);
    }

    public override void Ended(Scene scene)
    {
        base.Ended(scene);

        if (buffer is not null)
        {
            buffer.Dispose();
            buffer = null;
        }

        cube.Dispose();
    }

    internal static void Initialize()
    {
        shader = new(Engine.Graphics.GraphicsDevice)
        {
            TextureEnabled = true,
            VertexColorEnabled = true,
            View = Matrix.CreateLookAt(new(0, 0, 160), Vector3.Zero, Vector3.Up),
            Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(60), Engine.Viewport.AspectRatio, 0.1f, 1000f),
        };
    }

    internal static void Unload()
    {
        shader.Dispose();
    }
}
