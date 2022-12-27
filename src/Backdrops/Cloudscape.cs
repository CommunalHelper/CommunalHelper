using Celeste.Mod.Backdrops;
using Celeste.Mod.CommunalHelper.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Backdrops;

[CustomBackdrop("CommunalHelper/Cloudscape")]
public class Cloudscape : Backdrop
{
    private const int LEVEL_OF_DETAIL = 16;
    private const int STRIPE_SIZE = (sizeof(float) * 5) + (sizeof(byte) * 4);

    private static MTexture[] cloudTextures;

    public class Options
    {
        public int Seed { get; set; } = 0;

        public Color[] Colors { get; set; }
            = new[] { Calc.HexToColor("6d8ada"), Calc.HexToColor("aea0c1"), Calc.HexToColor("d9cbbc") };
        public Color Sky { get; set; } = Calc.HexToColor("4f9af7");

        public float InnerRadius { get; set; } = 40.0f;
        public float OuterRadius { get; set; } = 400.0f;
        public int Count { get; set; } = 24;

        public bool Lightning { get; set; } = false;

        public Color[] LightningColors { get; set; }
            = new[] { Calc.HexToColor("384bc8"), Calc.HexToColor("7a50d0"), Calc.HexToColor("c84ddd"), Calc.HexToColor("3397e2") };
        public Color LightningFlashColor { get; set; } = Color.White;

        public float LightningMinDelay { get; set; } = 5.0f;
        public float LightningMaxDelay { get; set; } = 40.0f;
        public float LightningMinDuration { get; set; } = 0.5f;
        public float LightningMaxDuration { get; set; } = 1.0f;
        public float LightningIntensity { get; set; } = 0.4f;

        public Vector2 Offset { get; set; } = Vector2.Zero;
        public Vector2 Parallax { get; set; } = Vector2.One * 0.05f;

        public float InnerDensity { get; set; } = 1.0f;
        public float OuterDensity { get; set; } = 1.0f;
        public float InnerRotation { get; set; } = 0.002f;
        public float OuterRotation { get; set; } = 0.2f;
        public float RotationExponent { get; set; } = 2.0f;

        public Options() { }

        public Options(BinaryPacker.Element child)
        {
            Seed = child.Attr("seed").GetHashCode();

            Colors = child.Attr("colors", "6d8ada,aea0c1,d9cbbc")
                          .Split(',')
                          .Select(str => Calc.HexToColor(str.Trim()))
                          .ToArray();
            Sky = Calc.HexToColor(child.Attr("bgColor", "4f9af7").Trim());

            InnerRadius = MathHelper.Max(child.AttrFloat("innerRadius", 40f), 10);
            OuterRadius = child.AttrFloat("outerRadius", 400f);
            Count = child.AttrInt("rings", 24);

            Lightning = child.AttrBool("lightning", false);
            LightningColors = child.Attr("lightningColors", "384bc8,7a50d0,c84ddd,3397e2")
                                   .Split(',')
                                   .Select(str => Calc.HexToColor(str.Trim()))
                                   .ToArray();
            LightningFlashColor = Calc.HexToColor(child.Attr("lightningFlashColor").Trim());

            LightningMinDelay = MathHelper.Max(child.AttrFloat("lightningMinDelay", 5.0f), 0);
            LightningMaxDelay = MathHelper.Max(child.AttrFloat("lightningMaxDelay", 40.0f), 0);
            LightningMinDuration = MathHelper.Max(child.AttrFloat("lightningMinDuration", 0.5f), 0);
            LightningMaxDuration = MathHelper.Max(child.AttrFloat("lightningMaxDuration", 1.0f), 0);
            LightningIntensity = MathHelper.Clamp(child.AttrFloat("lightningIntensity", 0.5f), 0f, 1f);

            Offset = new Vector2(child.AttrFloat("offsetX"), child.AttrFloat("offsetY"));
            Parallax = new Vector2(child.AttrFloat("parallaxX", 0.05f), child.AttrFloat("parallaxY", 0.05f));

            InnerDensity = MathHelper.Clamp(child.AttrFloat("innerDensity", 1f), 0f, 2f);
            OuterDensity = MathHelper.Clamp(child.AttrFloat("outerDensity", 1f), 0f, 2f);
            InnerRotation = child.AttrFloat("innerRotation", 0.002f);
            OuterRotation = child.AttrFloat("outerRotation", 0.2f);
            RotationExponent = MathHelper.Max(child.AttrFloat("rotationExponent", 2f), 1f);
        }
    }

    private class WarpedCloud
    {
        private readonly Cloudscape backdrop;
        private readonly Mesh<VertexPositionColorTexture> mesh;

        private readonly int index;

        public Color IdleColor { get; set; }
        private Color targetColorA, targetColorB, flashColor;

        private float timer = 0f;
        private float flashDuration = 1f, flashTimer = 0f;
        private float intensity;

        private float oldPercent;

        public WarpedCloud(Cloudscape backdrop, Mesh<VertexPositionColorTexture> mesh, int index, Color idleColor)
        {
            this.backdrop = backdrop;
            this.mesh = mesh;
            this.index = index;
            IdleColor = idleColor;

            timer = Calc.Random.Range(backdrop.lightningMinDelay, backdrop.lightningMaxDelay) * Calc.Random.NextFloat();
        }

        internal void UpdateColors(bool force = false)
        {
            float percent = flashTimer / flashDuration;
            if (oldPercent == percent && !force)
                return;

            float sin = ((float)Math.Sin(percent * 10) + 1) / 2f;
            Color target = Color.Lerp(targetColorA, targetColorB, sin);
            Color lightning = Color.Lerp(IdleColor, target, Ease.BounceIn(percent) * (1 - Ease.CubeIn(percent)));
            Color flash = intensity > 0 ? Color.Lerp(lightning, flashColor, intensity * Ease.ExpoIn(percent)) : lightning;

            int to = LEVEL_OF_DETAIL * 2;
            for (int i = 0; i < to; i++)
                mesh.Vertices[index + i].Color = flash;

            oldPercent = percent;
        }

        public void Update(bool allowLightning)
        {
            if (allowLightning)
            {
                timer -= Engine.DeltaTime;
                if (timer <= 0)
                {
                    timer = Calc.Random.Range(backdrop.lightningMinDelay, backdrop.lightningMaxDelay);
                    flashColor = backdrop.lightningFlashColor;
                    flashTimer = flashDuration = Calc.Random.Range(backdrop.lightningMinDuration, backdrop.lightningMaxDuration);
                    intensity = Settings.Instance.DisableFlashes ? 0 : backdrop.lightningIntensity * Ease.CubeIn(Calc.Random.NextFloat());
                    targetColorA = Util.ColorArrayLerp(Calc.Random.NextFloat() * (backdrop.lightningColors.Length - 1), backdrop.lightningColors);
                    targetColorB = Util.ColorArrayLerp(Calc.Random.NextFloat() * (backdrop.lightningColors.Length - 1), backdrop.lightningColors);
                }
            }

            if (flashTimer > 0)
                flashTimer = Calc.Approach(flashTimer, 0, Engine.DeltaTime);

            UpdateColors();
        }
    }

    private class Ring
    {
        public readonly Mesh<VertexPositionColorTexture> Mesh;

        private readonly WarpedCloud[] clouds;

        private float rotation;
        private readonly float rotationSpeed;

        public float ColorLerp { get; }

        private Matrix matrix;

        public Ring(Cloudscape backdrop, float radius, Color color, float colorLerp, List<WarpedCloud> allClouds, float speed, float density)
        {
            rotation = Calc.Random.NextFloat(MathHelper.TwoPi);
            rotationSpeed = speed;

            ColorLerp = colorLerp;

            Mesh = new(GFX.FxTexture);

            List<WarpedCloud> clouds = new();

            float angle = 0f;
            while (angle < MathHelper.TwoPi)
            {
                MTexture texture = Calc.Random.Choose(cloudTextures);

                int index = Mesh.VertexCount;

                WarpedCloud cloud = new(backdrop, Mesh, index, color);
                clouds.Add(cloud);
                allClouds.Add(cloud);

                float centralAngle = texture.Width / radius;
                float step = centralAngle / LEVEL_OF_DETAIL;
                float halfHeight = texture.Height / 2f;
                for (int i = 0; i < LEVEL_OF_DETAIL; i++)
                {
                    float th = angle + (step * i);

                    float uvx = MathHelper.Lerp(texture.LeftUV, texture.RightUV, (float)i / (LEVEL_OF_DETAIL - 1));
                    VertexPositionColorTexture closerVertex = new(new(Calc.AngleToVector(th, radius - halfHeight), 0), color, new(uvx, texture.TopUV));
                    VertexPositionColorTexture fartherVertex = new(new(Calc.AngleToVector(th, radius + halfHeight), 0), color, new(uvx, texture.BottomUV));
                    Mesh.AddVertices(closerVertex, fartherVertex);

                    if (i != LEVEL_OF_DETAIL - 1)
                    {
                        int o = index + (i * 2);
                        Mesh.AddTriangle(o + 0, o + 1, o + 2);
                        Mesh.AddTriangle(o + 1, o + 2, o + 3);
                    }
                }

                angle += centralAngle / density;
            }

            this.clouds = clouds.ToArray();

            Mesh.Bake();
        }

        public void SetCloudColor(Color color)
        {
            foreach (WarpedCloud cloud in clouds)
            {
                cloud.IdleColor = color;
                cloud.UpdateColors(force: true);
            }
        }

        public void Update(Matrix translation)
        {
            rotation += rotationSpeed * Engine.DeltaTime;
            matrix = Matrix.CreateRotationZ(rotation) * translation;
        }

        public void Render()
        {
            Mesh.Celeste_DrawVertices(matrix);
        }
    }

    private readonly Ring[] rings;
    private readonly WarpedCloud[] clouds;

    private Color sky;

    private VirtualRenderTarget buffer;
    private Matrix matrix;

    private readonly Vector2 offset, parallax;

    private bool lightning;
    private float lightningMinDelay, lightningMaxDelay;
    private float lightningMinDuration, lightningMaxDuration;
    private float lightningIntensity;
    private Color[] lightningColors;
    private Color lightningFlashColor;

    public Cloudscape(BinaryPacker.Element child)
        : this(new Options(child)) { }

    public Cloudscape(Options options) : base()
    {
        sky = options.Sky;

        offset = options.Offset;
        parallax = options.Parallax;

        lightning = options.Lightning;
        lightningMinDelay = options.LightningMinDelay;
        lightningMaxDelay = options.LightningMaxDelay;
        lightningMinDuration = options.LightningMinDuration;
        lightningMaxDuration = options.LightningMaxDuration;
        lightningIntensity = options.LightningIntensity;
        lightningColors = options.LightningColors;
        lightningFlashColor = options.LightningFlashColor;

        Calc.PushRandom(options.Seed);

        List<Ring> rings = new();
        List<WarpedCloud> clouds = new();

        int vertexCount = 0, triangleCount = 0;

        int count = options.Count;
        float a = MathHelper.Min(options.InnerRadius, options.OuterRadius);
        float b = MathHelper.Max(options.InnerRadius, options.OuterRadius);
        float d = b - a;
        float dRotation = options.OuterRotation - options.InnerRotation;
        for (int i = 0; i < count; i++)
        {
            float percent = (float)i / count;

            Color color = Util.ColorArrayLerp(percent * (options.Colors.Length - 1), options.Colors);
            float radius = a + (d * percent);
            float speed = (dRotation * (float)Math.Pow(percent, options.RotationExponent)) + options.InnerRotation;
            float density = MathHelper.Lerp(options.InnerDensity, options.OuterDensity, percent);

            if (density == 0)
                continue;

            Ring ring = new(this, radius, color, percent, clouds, speed, density);

            vertexCount += ring.Mesh.VertexCount;
            triangleCount += ring.Mesh.Triangles;

            rings.Add(ring);
        }

        this.rings = rings.ToArray();
        this.clouds = clouds.ToArray();

        float bytes = STRIPE_SIZE * vertexCount;
        Util.Log(LogLevel.Info, $"Cloudscape meshes baked:");
        Util.Log(LogLevel.Info, $"  * {vertexCount} vertices and {triangleCount} triangles ({triangleCount * 3} indices)");
        Util.Log(LogLevel.Info, $"  * Size of {bytes * 1e-3} kB = {bytes * 1e-6} MB ({bytes}o)");

        Calc.PopRandom();
    }

    public void ConfigureColors(Color bg, Color[] gradientFrom, Color[] gradientTo, float lerp)
    {
        sky = bg;
        foreach (Ring ring in rings)
        {
            Color from = Util.ColorArrayLerp(ring.ColorLerp * (gradientFrom.Length - 1), gradientFrom);
            Color to = Util.ColorArrayLerp(ring.ColorLerp * (gradientTo.Length - 1), gradientTo);
            ring.SetCloudColor(Color.Lerp(from, to, lerp));
        }
    }

    public void ConfigureLightning(
        bool enable,
        Color[] lightningColors, Color lightningFlashColor,
        float lightningMinDelay, float lightningMaxDelay,
        float lightningMinDuration, float lightningMaxDuration,
        float lightningIntensity)
    {
        lightning = enable;
        if (lightning)
        {
            this.lightningColors = lightningColors;
            this.lightningFlashColor = lightningFlashColor;
            this.lightningMinDelay = lightningMinDelay;
            this.lightningMaxDelay = lightningMaxDelay;
            this.lightningMinDuration = lightningMinDuration;
            this.lightningMaxDuration = lightningMaxDuration;
            this.lightningIntensity = lightningIntensity;
        }
    }

    public override void Update(Scene scene)
    {
        base.Update(scene);

        if (Visible)
        {
            Vector2 pos = offset - ((scene as Level).Camera.Position * parallax);
            matrix = Matrix.CreateTranslation(pos.X, pos.Y, 0);

            foreach (Ring ring in rings)
                ring.Update(matrix);

            foreach (WarpedCloud cloud in clouds)
                cloud.Update(lightning);
        }
    }

    public override void BeforeRender(Scene scene)
    {
        if (!Visible)
            return;

        base.BeforeRender(scene);

        if (buffer == null || buffer.IsDisposed)
            buffer = VirtualContent.CreateRenderTarget("communalhelper-cloudscape", 320, 180);

        Engine.Graphics.GraphicsDevice.SetRenderTarget(buffer);
        Engine.Graphics.GraphicsDevice.Clear(sky);
        Engine.Instance.GraphicsDevice.SamplerStates[0] = SamplerState.PointWrap;
        Engine.Instance.GraphicsDevice.Textures[0] = CommunalHelperModule.CloudscapeAtlas.Sources[0].Texture_Safe;
        foreach (Ring ring in rings)
            ring.Render();
    }

    public override void Render(Scene scene)
    {
        base.Render(scene);

        if (buffer != null && !buffer.IsDisposed)
            Draw.SpriteBatch.Draw(buffer, Vector2.Zero, Color.White);
    }

    public override void Ended(Scene scene)
    {
        base.Ended(scene);

        if (buffer != null)
        {
            buffer.Dispose();
            buffer = null;
        }

        if (rings != null)
            foreach (Ring ring in rings)
                ring.Mesh.Dispose();
    }

    internal static void InitializeTextures()
    {
        cloudTextures = CommunalHelperModule.CloudscapeAtlas.GetAtlasSubtextures(string.Empty).ToArray();
    }
}
