using Celeste.Mod.CommunalHelper.Utils;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.CommunalHelper.Scenes;

public sealed class VoxelEditor : Scene
{
    private int width, height;
    private RenderTarget2D meshScreen;
    private BasicEffect shader;

    private readonly int sx, sy, sz;
    private readonly char[,,] voxel;
    private Mesh<VertexPositionNormalTexture> mesh, box;

    private Vector2 mouse;

    private float scale = 4;
    private Matrix rotation = Matrix.Identity; //Matrix.CreateRotationY(MathHelper.PiOver4) * Matrix.CreateRotationX(MathHelper.PiOver4 / 2f);

    public VoxelEditor(int sx, int sy, int sz)
    {
        this.sx = sx;
        this.sy = sy;
        this.sz = sz;

        voxel = new char[sz, sy, sx];
        for (int z = 0; z < sz; z++)
            for (int y = 0; y < sy; y++)
                for (int x = 0; x < sx; x++)
                    voxel[z, y, x] = '0';

        RemakeMesh();

        box = Shapes.Box_PositionNormalTexture(Vector3.Zero, new Vector3(sx, sy, sz) * 8);
        for (int i = 0; i < box.VertexCount; i++)
            box.Vertices[i].Normal *= -1;
    }

    private bool TrySetTile(int x, int y, int z, char c, out char del)
    {
        del = '0';
        if (x < 0 || x >= sx ||
            y < 0 || y >= sy ||
            z < 0 || z >= sz)
            return false;

        del = voxel[z, y, x];
        voxel[z, y, x] = c;
        return true;
    }

    private void RemakeMesh()
    {
        mesh = Shapes.TileVoxel(voxel);
    }

    private void DestroyBuffers()
    {
        meshScreen.Dispose();
    }

    private void CreateBuffers()
    {
        width = Engine.Graphics.PreferredBackBufferWidth;
        height = Engine.Graphics.PreferredBackBufferHeight;
        meshScreen = new RenderTarget2D(Engine.Graphics.GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
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
    }

    public override void End()
    {
        base.End();
        DestroyBuffers();
        Engine.Instance.IsMouseVisible = false;
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
        Ray ray = new(from, dir);

        Vector3 s = new(sx, sy, sz);
        float? t = ray.Intersects(new BoundingBox(-s * 4f, s * 4f));
        if (t is null)
            return false;

        // if so, get the position in the voxel of the tile (empty or not) that was hit by the ray

        Vector3 flip = new(1, -1, -1);
        Vector3 hitWorld = (from + dir * t.Value) * flip;
        Vector3 hitVoxel = (hitWorld + s * 4) / 8f;

        dir *= flip;

        // use the 3D variant of DDA to raycast through the voxel
        // stop at first non-empty tile or at opposite bound
        // this way we are always able to place a tile, even in an empty voxel

        static float intbound(float s, float ds)
        {
            if (ds < 0)
                return intbound(-s, -ds);
            return (1 - Util.Mod(s, 1.0f)) / ds;
        }

        x = Calc.Clamp((int) Math.Floor(hitVoxel.X), 0, sx - 1);
        y = Calc.Clamp((int) Math.Floor(hitVoxel.Y), 0, sy - 1);
        z = Calc.Clamp((int) Math.Floor(hitVoxel.Z), 0, sz - 1);
        int stepx = Math.Sign(dir.X), stepy = Math.Sign(dir.Y), stepz = Math.Sign(dir.Z);
        float tx = dir.X != 0.0f ? intbound(hitVoxel.X, dir.X) : float.PositiveInfinity;
        float ty = dir.Y != 0.0f ? intbound(hitVoxel.Y, dir.Y) : float.PositiveInfinity;
        float tz = dir.Z != 0.0f ? intbound(hitVoxel.Z, dir.Z) : float.PositiveInfinity;
        float dtx = 1 / dir.X, dty = 1 / dir.Y, dtz = 1 / dir.Z;
        nx = 0; ny = 0; nz = 0;

        while (true)
        {
            if (x < 0 || x >= sx ||
                y < 0 || y >= sy ||
                z < 0 || z >= sz)
                break;

            if (voxel[z, y, x] is not '0')
                break;

            float min = Calc.Min(tx, ty, tz);
            if (dir.X != 0.0f && min == tx)
            {
                x += stepx;
                tx += dtx;
                nx = -stepx; ny = 0; nz = 0;
            }
            else if (dir.Y != 0.0f && min == ty)
            {
                y += stepy;
                ty += dty;
                nx = 0; ny = -stepy; nz = 0;
            }
            else if (dir.Z != 0.0f && min == tz)
            {
                z += stepz;
                tz += dtz;
                nx = 0; ny = 0; nz = -stepz;
            }
            else break;
        }

        return true;
    }

    public override void Update()
    {
        base.Update();

        {
            Vector2 prev = mouse;
            mouse = new(MInput.Mouse.CurrentState.X, MInput.Mouse.CurrentState.Y);
            Vector2 delta = mouse - prev;

            if (MInput.Mouse.CheckRightButton)
                rotation *= Matrix.CreateFromYawPitchRoll(delta.X / 750, delta.Y / 750, 0f);

            scale *= (float) Math.Pow(2, Math.Sign(MInput.Mouse.WheelDelta));
        }

        const float far = 5000;
        shader.Projection = Matrix.CreateOrthographic(width, height, 1, far);
        shader.View = Matrix.CreateLookAt(Vector3.Backward * far / 2f, Vector3.Zero, Vector3.Up);
        shader.World = Matrix.CreateScale(scale) * rotation;

        if (!Raycast(out int x, out int y, out int z, out int nx, out int ny, out int nz))
            return;

        if (MInput.Mouse.PressedLeftButton)
        {
            bool delete = MInput.Keyboard.Check(Keys.LeftShift);
            if (!delete)
            {
                x += nx;
                y += ny;
                z += nz;
            }

            char c = delete ? '0' : 'h';
            if (TrySetTile(x, y, z, c, out char replaced) && replaced != c)
            {
                RemakeMesh();
                if (SurfaceIndex.TileToIndex.TryGetValue(c, out int index))
                    Audio.Play(SFX.char_mad_grab, "surface_index", index);
                else
                    Audio.Play(SFX.game_assist_dash_aim);
            }
        }
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
        Engine.Instance.GraphicsDevice.Textures[0] = GFX.Game.Sources[0].Texture_Safe;
        Engine.Instance.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        Engine.Instance.GraphicsDevice.BlendState = BlendState.AlphaBlend;

        Draw.SpriteBatch.GraphicsDevice.SetRenderTarget(meshScreen);
        Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);
        shader.CurrentTechnique.Passes[0].Apply();

        mesh.Draw();

        Engine.Instance.GraphicsDevice.Textures[0] = CommunalHelperGFX.Blank;
        Engine.Instance.GraphicsDevice.RasterizerState = RasterizerState.CullClockwise;
        box.Draw();
    }

    public override void Render()
    {
        base.Render();

        Engine.SetViewport(new(0, 0, width, height));

        Draw.SpriteBatch.Begin();
        Draw.SpriteBatch.Draw(meshScreen, new Rectangle(0, 0, width, height), Color.White);
        Draw.SpriteBatch.End();
    }
}
