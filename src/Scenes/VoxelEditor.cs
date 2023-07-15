using Celeste.Mod.CommunalHelper.Utils;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Celeste.Mod.CommunalHelper.Scenes;

public sealed class VoxelEditor : Scene
{
    private static readonly FieldInfo f_Autotiler_lookup
        = typeof(Autotiler).GetField("lookup", BindingFlags.Instance | BindingFlags.NonPublic);

    private readonly IEnumerable<char> availableTilesets;

    private int width, height;
    private RenderTarget2D screen;
    private BasicEffect shader, lineShader;

    private char brush = 'h';

    private readonly int sx, sy, sz;
    private readonly char[,,] voxel;
    private IEnumerable<Tuple<Mesh<VertexPositionNormalTexture>, Texture2D>> voxelMeshes;

    private Tuple<Mesh<VertexPositionNormalTexture>, Texture2D> tile;
    private readonly Mesh<VertexPositionNormalTexture> box;
    private readonly VertexPositionColor[] axes;

    // stores old raycast results
    private int otx = -1, oty = -1, otz = -1;
    private bool prevRaycast = false;

    private Vector2 mouse;

    private float scale = 4;
    private Matrix rotation = Matrix.CreateRotationY(MathHelper.PiOver4 / 3.5f) * Matrix.CreateRotationX(MathHelper.PiOver4 / 2f);

    private float infolerp;

    public VoxelEditor(int sx, int sy, int sz, string model = null)
    {
        var lookup = f_Autotiler_lookup.GetValue(GFX.FGAutotiler);
        availableTilesets = (IEnumerable<char>) lookup.GetType().GetProperty("Keys").GetValue(lookup);

        if (!availableTilesets.Any())
            throw new InvalidOperationException("Cannot open voxel editor as there are no registered tilesets to use");
        
        brush = availableTilesets.First();

        this.sx = sx;
        this.sy = sy;
        this.sz = sz;

        voxel = new char[sz, sy, sx];
        for (int z = 0; z < sz; z++)
            for (int y = 0; y < sy; y++)
                for (int x = 0; x < sx; x++)
                {
                    int index = x + sx * y + sx * sy * z;
                    char tile = (model is not null && index < model.Length) ? model[index] : '0';
                    
                    if (tile is not '0' && !availableTilesets.Contains(tile))
                        throw new InvalidOperationException($"{tile} is an invalid tile ID. Make sure you load the correct level first.");

                    voxel[z, y, x] = tile;
                }

        RemakeMesh();

        box = Shapes.Box_PositionNormalTexture(Vector3.Zero, new Vector3(sx, sy, sz) * 8);
        for (int i = 0; i < box.VertexCount; i++)
            box.Vertices[i].Normal *= -1;

        Vector3 origin = new Vector3(-sx, sy, sz) * 4;
        axes = new VertexPositionColor[]
        {
            new(origin + Vector3.Zero, Color.Red),
            new(origin + Vector3.UnitX * sx * 8, Color.Red),
            new(origin + Vector3.Zero, Color.Lime),
            new(origin + Vector3.UnitY * sy * -8, Color.Lime),
            new(origin + Vector3.Zero, Color.Blue),
            new(origin + Vector3.UnitZ * sz * -8, Color.Blue),
        };
    }

    private bool InVoxelBounds(int x, int y, int z)
        => x >= 0 && x < sx && y >= 0 && y < sy && z >= 0 && z < sz;

    private bool TrySetTile(int x, int y, int z, char c, out char del)
    {
        del = '0';
        if (!InVoxelBounds(x, y, z))
            return false;

        del = voxel[z, y, x];
        voxel[z, y, x] = c;
        return true;
    }

    private void RemakeMesh()
    {
        voxelMeshes = Shapes.TileVoxel(voxel);
    }

    private void DestroyBuffers()
    {
        screen.Dispose();
    }

    private void CreateBuffers()
    {
        width = Engine.Graphics.PreferredBackBufferWidth;
        height = Engine.Graphics.PreferredBackBufferHeight;
        screen = new RenderTarget2D(Engine.Graphics.GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
    }

    public override void Begin()
    {
        base.Begin();
        CreateBuffers();
        Engine.Instance.IsMouseVisible = true;

        const float AMBIENT_LIGHT = 0.35f;

        shader = new(Engine.Graphics.GraphicsDevice)
        {
            TextureEnabled = true,
            LightingEnabled = true,
            AmbientLightColor = Vector3.One * AMBIENT_LIGHT,
        };

        shader.DirectionalLight0.Enabled = true;
        shader.DirectionalLight0.Direction = Vector3.Backward;
        shader.DirectionalLight0.DiffuseColor = Vector3.One * (1 - AMBIENT_LIGHT);

        lineShader = new(Engine.Graphics.GraphicsDevice)
        {
            VertexColorEnabled = true,
        };

        TextInput.OnInput += ReceiveCharacterInput;
    }

    public override void End()
    {
        base.End();

        DestroyBuffers();

        shader.Dispose();
        lineShader.Dispose();

        Engine.Instance.IsMouseVisible = false;
        TextInput.OnInput -= ReceiveCharacterInput;
    }

    private void ReceiveCharacterInput(char c)
    {
        if (!availableTilesets.Contains(c))
            return;

        if (brush != c)
        {
            brush = c;
            RemakePreviewTileMesh(otx, oty, otz);
        }
    }

    private void RemakePreviewTileMesh(int x, int y, int z)
    {
        tile = Shapes.TileVoxel(new char[1, 1, 1] { { { brush } } }).FirstOrDefault();
        Vector3 tilePos = new((-sx / 2.0f + x) * 8 + 4, (sy / 2.0f - y) * 8 - 4, (sz / 2.0f - z) * 8 - 4);
        for (int i = 0; i < tile.Item1.VertexCount; i++)
            tile.Item1.Vertices[i].Position = tile.Item1.Vertices[i].Position * 6.5f / 8 + tilePos;
    }

    // x, y, z = tile coordinates
    // nx, ny, nz = normal vector to tile
    private bool Raycast(out int x, out int y, out int z, out int nx, out int ny, out int nz)
    {
        // raycast to check if the bounds are reached by the mouse
        Vector2 ndc = new(Calc.Map(mouse.X, 0, width, -1, 1), Calc.Map(mouse.Y, 0, height, 1, -1));
        Vector3 Unproject(float z)
        {
            Vector4 pos = Vector4.Transform(Vector4.Transform(new Vector4(ndc, z, 1.0f), Matrix.Invert(shader.Projection)), Matrix.Invert(shader.World * shader.View));
            return new Vector3(pos.X, pos.Y, pos.Z);
        }

        Vector3 from = Unproject(-1.0f);
        Vector3 to = Unproject(1.0f);
        Vector3 dir = (to - from).SafeNormalize();

        if (dir == Vector3.Zero)
            return false;

        Ray ray = new(from, dir);
        Vector3 s = new(sx, sy, sz);
        float? t = ray.Intersects(new BoundingBox(-s * 4f, s * 4f));
        if (t is null)
            return false;

        // if so, get the position in the voxel of the tile (empty or not) that was hit by the ray

        Vector3 flip = new(1, -1, -1);
        Vector3 pos = (((from + dir * t.Value) * flip) + s * 4) / 8f;
        pos.X = Calc.Clamp(pos.X, 0, sx);
        pos.Y = Calc.Clamp(pos.Y, 0, sy);
        pos.Z = Calc.Clamp(pos.Z, 0, sz);

        dir *= flip;

        // use the 3D variant of DDA to raycast through the voxel
        // stop at first non-empty tile or at opposite bound
        // this way we are always able to place a tile, even in an empty voxel

        Vector3 map = pos.Floor();
        Vector3 delta = (Vector3.One / dir).Abs();
        Vector3 dist = new(delta.X * (dir.X < 0 ? (pos.X - map.X) : (map.X + 1.0f - pos.X)),
                           delta.Y * (dir.Y < 0 ? (pos.Y - map.Y) : (map.Y + 1.0f - pos.Y)),
                           delta.Z * (dir.Z < 0 ? (pos.Z - map.Z) : (map.Z + 1.0f - pos.Z)));
        Vector3 step = dir.Sign();

        void Step(out int nx, out int ny, out int nz)
        {
            nx = 0;
            ny = 0;
            nz = 0;

            float min = Calc.Min(dist.X, dist.Y, dist.Z);
            if (dir.X != 0.0f && min == dist.X)
            {
                map.X += step.X;
                dist.X += delta.X;
                nx = (int) -step.X;
            }
            else if (dir.Y != 0.0f && min == dist.Y)
            {
                map.Y += step.Y;
                dist.Y += delta.Y;
                ny = (int) -step.Y;
            }
            else if (dir.Z != 0.0f && min == dist.Z)
            {
                map.Z += step.Z;
                dist.Z += delta.Z;
                nz = (int) -step.Z;
            }
        }

        if (map.X == sx || map.Y == sy || map.Z == sz)
            Step(out nx, out ny, out nz);

        while (true)
        {
            x = (int) map.X;
            y = (int) map.Y;
            z = (int) map.Z;

            if (!InVoxelBounds(x, y, z) || voxel[z, y, x] is not '0')
                break;

            Step(out nx, out ny, out nz);
        }

        return true;
    }

    public override void Update()
    {
        base.Update();

        int x, y, z;
        
        // save voxel
        if (MInput.Keyboard.Pressed(Keys.Enter))
        {
            var sb = new StringBuilder(capacity: sx * sy * sz);
            for (z = 0; z < sz; z++)
                for (y = 0; y < sy; y++)
                    for (x = 0; x < sx; x++)
                        sb.Append(voxel[z, y, x]);

            string final = sb.ToString().TrimEnd('0');
            if (string.IsNullOrWhiteSpace(final))
            {
                Console.WriteLine("------ GENERATED MESH WAS EMPTY, DID NOTHING ------");
                Audio.Play(SFX.ui_main_button_invalid);
            }
            else
            {
                TextInput.SetClipboardText(final);
                Console.WriteLine($"------ GENERATED {sx}x{sy}x{sz} VOXEL COPIED TO CLIPBOARD ------");
                Console.WriteLine(final);
                Audio.Play(SFX.game_02_theoselfie_photo_filter);
            }
        }

        // rotation and scale
        if (MInput.Keyboard.Pressed(Keys.Space))
            rotation = Matrix.CreateRotationY(MathHelper.PiOver4 / 3.5f) * Matrix.CreateRotationX(MathHelper.PiOver4 / 2f);

        Vector2 prev = mouse;
        mouse = new(MInput.Mouse.CurrentState.X, MInput.Mouse.CurrentState.Y);
        Vector2 delta = mouse - prev;

        if (MInput.Mouse.CheckMiddleButton)
            rotation *= Matrix.CreateFromYawPitchRoll(delta.X / 750, delta.Y / 750, 0f);

        scale *= (float) Math.Pow(2, Math.Sign(MInput.Mouse.WheelDelta));

        // matrices update
        const float far = 20000;
        lineShader.Projection = shader.Projection = Matrix.CreateOrthographic(width, height, 1, far);
        lineShader.View = shader.View = Matrix.CreateLookAt(Vector3.Backward * far / 2f, Vector3.Zero, Vector3.Up);
        lineShader.World = shader.World = Matrix.CreateScale(scale) * rotation;

        bool shouldShowInfo = !prevRaycast && !MInput.Keyboard.Check(Keys.Tab);
        infolerp = Calc.Approach(infolerp, shouldShowInfo ? 1 : 0, Engine.DeltaTime * 3f);

        // raycast in voxel
        if (!Raycast(out x, out y, out z, out int nx, out int ny, out int nz))
        {
            prevRaycast = false;
            tile = null;
            return;
        }

        int tx = x + nx;
        int ty = y + ny;
        int tz = z + nz;

        // update preview tile
        if ((tx != otx || ty != oty || tz != otz || !prevRaycast) && InVoxelBounds(tx, ty, tz))
            RemakePreviewTileMesh(tx, ty, tz);

        // place | delete
        if (MInput.Mouse.PressedLeftButton || MInput.Mouse.PressedRightButton)
        {
            bool delete = MInput.Mouse.PressedRightButton;
            if (!delete)
            {
                x += nx;
                y += ny;
                z += nz;
            }

            char c = delete ? '0' : brush;
            if (TrySetTile(x, y, z, c, out char replaced) && replaced != c)
            {
                RemakeMesh();
                if (SurfaceIndex.TileToIndex.TryGetValue(delete ? replaced : c, out int index))
                    Audio.Play(SFX.char_mad_grab, "surface_index", index);
                    Audio.Play(SFX.char_mad_grab, "surface_index", index);
            }
        }

        // update with lastest raycast state
        otx = tx;
        oty = ty;
        otz = tz;
        prevRaycast = true;
    }

    public override void BeforeRender()
    {
        if (Engine.Graphics.PreferredBackBufferWidth != width || Engine.Graphics.PreferredBackBufferHeight != height)
        {
            DestroyBuffers();
            CreateBuffers();
        }

        Engine.Instance.GraphicsDevice.SamplerStates[0] = SamplerState.PointWrap;
        Engine.Instance.GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
        Engine.Instance.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        Engine.Instance.GraphicsDevice.BlendState = BlendState.AlphaBlend;

        Draw.SpriteBatch.GraphicsDevice.SetRenderTarget(screen);
        Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);
        shader.CurrentTechnique.Passes[0].Apply();
        foreach (var pair in voxelMeshes)
        {
            Engine.Instance.GraphicsDevice.Textures[0] = pair.Item2;
            pair.Item1.Draw();
        }

        if (tile is not null)
        {
            Engine.Instance.GraphicsDevice.Textures[0] = tile.Item2;
            tile.Item1.Draw();
        }

        Engine.Instance.GraphicsDevice.Textures[0] = CommunalHelperGFX.Blank;
        Engine.Instance.GraphicsDevice.RasterizerState = RasterizerState.CullClockwise;
        box.Draw();

        lineShader.CurrentTechnique.Passes[0].Apply();
        Engine.Instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, axes, 0, axes.Length / 2);
    }

    public override void Render()
    {
        base.Render();

        Engine.SetViewport(new(0, 0, width, height));

        Draw.SpriteBatch.Begin();
        Draw.SpriteBatch.Draw(screen, new Rectangle(0, 0, width, height), Color.White);
        if (infolerp >= 0.0f)
        {
            string msg = $"""
            Commands:
              * MIDDLE MOUSE: rotate the voxel
              * LEFT|RIGHT CLICK: place|delete tile respectively
              * MOUSE WHEEL: zoom in|out
              * SPACE: reset rotation
              * ENTER: copy voxel string to clipboard + output to log.txt
              * TAB: hides this text
              * COMMAND `overworld`: back to main menu

            Input the tile ID of the tile you want to paint with.

            Info:
              * Currently editing {sx}x{sy}x{sz} voxel
              * Selected tile: '{brush}'
            """;

            Vector2 pos = Vector2.UnitX * -64 * Ease.QuadIn(1 - infolerp);
            ActiveFont.DrawOutline(msg, pos, Vector2.Zero, Vector2.One * 0.5f, Color.White * infolerp, 2f, Color.Black * infolerp);
        }
        Draw.SpriteBatch.End();
    }
}
