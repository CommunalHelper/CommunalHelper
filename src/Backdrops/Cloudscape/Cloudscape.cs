using Celeste.Mod.Backdrops;
using Celeste.Mod.CommunalHelper.Utils;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Backdrops;

[CustomBackdrop("CommunalHelper/Cloudscape")]
public class Cloudscape : Backdrop
{
    private const uint LEVEL_OF_DETAIL = 16;

    private static MTexture[] cloudTextures;

    // using one buffer even if there are multiple cloudscapes;
    // cleared when rendering another cloudscape.
    private static VirtualRenderTarget buffer;

    public class Options
    {
        public int Seed { get; } = 0;

        public Color[] Colors { get; } = new[]
        {
            Calc.HexToColor("6d8ada"),
            Calc.HexToColor("aea0c1"),
            Calc.HexToColor("d9cbbc"),
        };
        public Color Sky { get; } = Calc.HexToColor("4f9af7");

        public float InnerRadius { get; } = 40.0f;
        public float OuterRadius { get; } = 400.0f;
        public int Count { get; } = 24;

        public bool Lightning { get; } = false;

        public Color[] LightningColors { get; } = new[]
        {
            Calc.HexToColor("384bc8"),
            Calc.HexToColor("7a50d0"),
            Calc.HexToColor("c84ddd"), 
            Calc.HexToColor("3397e2"),
        };
        public Color LightningFlashColor { get; } = Color.White;

        public float LightningMinDelay { get; } = 5.0f;
        public float LightningMaxDelay { get; } = 40.0f;
        public float LightningMinDuration { get; } = 0.5f;
        public float LightningMaxDuration { get; } = 1.0f;
        public float LightningIntensity { get; } = 0.4f;

        public Vector2 Offset { get; } = Vector2.Zero;
        public Vector2 Parallax { get; } = Vector2.One * 0.05f;

        public float InnerDensity { get; } = 1.0f;
        public float OuterDensity { get; } = 1.0f;
        public float InnerRotation { get; } = 0.002f;
        public float OuterRotation { get; } = 0.2f;
        public float RotationExponent { get; } = 2.0f;

        public bool HasBackgroundColor { get; } = true;
        public bool Additive { get; } = false;

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

            HasBackgroundColor = child.AttrBool("hasBackgroundColor", true);
            Additive = child.AttrBool("additive", false);
        }
    }

    private class WarpedCloud
    {
        private readonly Cloudscape parent;

        public Color IdleColor { get; set; }
        private Color targetColorA, targetColorB, flashColor;

        private float timer = 0f;
        private float flashDuration = 1f, flashTimer = 0f;
        private float intensity;

        private float oldPercent;

        private Color color;

        public WarpedCloud(Cloudscape parent, Color idleColor)
        {
            this.parent = parent;
            IdleColor = color = idleColor;
            timer = Calc.Random.Range(parent.lightningMinDelay, parent.lightningMaxDelay) * Calc.Random.NextFloat();
        }

        public Color CalculateColor(bool force = false)
        {
            float percent = flashTimer / flashDuration;
            if (oldPercent == percent && !force)
                return color;

            float sin = ((float) Math.Sin(percent * 10) + 1) / 2f;
            Color target = Color.Lerp(targetColorA, targetColorB, sin);
            Color lightning = Color.Lerp(IdleColor, target, Ease.BounceIn(percent) * (1 - Ease.CubeIn(percent)));
            color = intensity > 0 ? Color.Lerp(lightning, flashColor, intensity * Ease.ExpoIn(percent)) : lightning;

            oldPercent = percent;

            return color;
        }

        public void Update(bool allowLightning)
        {
            if (allowLightning)
            {
                timer -= Engine.DeltaTime;
                if (timer <= 0)
                {
                    timer = Calc.Random.Range(parent.lightningMinDelay, parent.lightningMaxDelay);
                    flashColor = parent.lightningFlashColor;
                    flashTimer = flashDuration = Calc.Random.Range(parent.lightningMinDuration, parent.lightningMaxDuration);
                    intensity = Settings.Instance.DisableFlashes ? 0 : parent.lightningIntensity * Ease.CubeIn(Calc.Random.NextFloat());
                    targetColorA = Util.ColorArrayLerp(Calc.Random.NextFloat() * (parent.lightningColors.Length - 1), parent.lightningColors);
                    targetColorB = Util.ColorArrayLerp(Calc.Random.NextFloat() * (parent.lightningColors.Length - 1), parent.lightningColors);
                }
            }

            if (flashTimer > 0)
                flashTimer = Calc.Approach(flashTimer, 0, Engine.DeltaTime);
        }
    }

    private class Ring
    {
        public float Lerp { get; }
        private readonly WarpedCloud[] clouds;

        public Ring(float lerp, WarpedCloud[] clouds)
        {
            Lerp = lerp;
            this.clouds = clouds;
        }

        public void ApplyIdleColor(Color color, Color[] array)
        {
            for (int i = 0; i < clouds.Length; i++)
            {
                var cloud = clouds[i];
                cloud.IdleColor = color;
                array[i] = cloud.CalculateColor(force: true);
            }
        }
    }

    private Color sky;

    private readonly Vector2 offset, parallax;
    private Vector2 translate;

    private bool lightning;
    private float lightningMinDelay, lightningMaxDelay;
    private float lightningMinDuration, lightningMaxDuration;
    private float lightningIntensity;
    private Color[] lightningColors;
    private Color lightningFlashColor;

    private readonly float innerRotation, outerRotation;
    private readonly float rotationExponent;

    private readonly BlendState blend;

    private Texture2D colorBuffer;
    private readonly Mesh<CloudscapeVertex> mesh;
    private readonly WarpedCloud[] clouds;
    private readonly Ring[] rings;

    private readonly Color[] colors;

    public Cloudscape(BinaryPacker.Element child)
        : this(new Options(child)) { }

    public Cloudscape(Options options)
        : base()
    {
        UseSpritebatch = false;

        sky = options.HasBackgroundColor
            ? options.Sky
            : Color.Transparent;

        blend = options.Additive
            ? BlendState.Additive
            : BlendState.AlphaBlend;

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

        innerRotation = options.InnerRotation;
        outerRotation = options.OuterRotation;
        rotationExponent = options.RotationExponent;

        Calc.PushRandom(options.Seed);

        mesh = new();

        List<WarpedCloud> clouds = new();
        List<Ring> rings = new();

        int count = options.Count;

        float a = MathHelper.Min(options.InnerRadius, options.OuterRadius);
        float b = MathHelper.Max(options.InnerRadius, options.OuterRadius);
        float d = b - a;

        short id = 0; // cloud ID for color lookup

        // ring iteration
        for (short r = 0; r < count; r++)
        {
            float percent = (float) r / count;

            Color color = Util.ColorArrayLerp(percent * (options.Colors.Length - 1), options.Colors);
            float radius = a + (d * percent);
            float density = MathHelper.Lerp(options.InnerDensity, options.OuterDensity, percent);

            if (density == 0)
                continue;

            List<WarpedCloud> cloudsInRing = new();

            float rotation = Calc.Random.NextFloat(MathHelper.TwoPi);

            // cloud iteration
            float angle = 0f;
            while (angle < MathHelper.TwoPi)
            {
                WarpedCloud cloud = new(this, color);
                clouds.Add(cloud);
                cloudsInRing.Add(cloud);

                int index = mesh.VertexCount;

                MTexture texture = Calc.Random.Choose(cloudTextures);
                float halfHeight = texture.Height / 2f;

                float centralAngle = texture.Width / radius;
                float step = centralAngle / LEVEL_OF_DETAIL;

                for (int i = 0; i < LEVEL_OF_DETAIL; i++)
                {
                    float th = rotation + angle + (step * i);

                    // custom vertices hold polar coordinates. cartesian coordinates are computed in the shader.
                    float uvx = MathHelper.Lerp(texture.LeftUV, texture.RightUV, (float) i / (LEVEL_OF_DETAIL - 1));
                    CloudscapeVertex closer = new(th, radius - halfHeight, new(uvx, texture.TopUV), id, r);
                    CloudscapeVertex farther = new(th, radius + halfHeight, new(uvx, texture.BottomUV), id, r);
                    mesh.AddVertices(closer, farther);

                    if (i != LEVEL_OF_DETAIL - 1)
                    {
                        int o = index + (i * 2);
                        mesh.AddTriangle(o + 0, o + 1, o + 2);
                        mesh.AddTriangle(o + 1, o + 2, o + 3);
                    }
                }

                ++id;
                angle += centralAngle / density;
            }

            // add ring to regroup clouds
            rings.Add(new(percent, cloudsInRing.ToArray()));
        }

        mesh.Bake();

        this.clouds = clouds.ToArray();
        this.rings = rings.ToArray();

        EnsureValidColorBuffer();
        colors = new Color[this.clouds.Length];

        Calc.PopRandom();

        int bytes = mesh.VertexCount * CloudscapeVertex.VertexDeclaration.VertexStride;
        Util.Log(LogLevel.Info, $"[NEW-IMPL] Cloudscape meshes baked:");
        Util.Log(LogLevel.Info, $"  * {mesh.VertexCount} vertices and {mesh.Triangles} triangles ({mesh.Triangles * 3} indices)");
        Util.Log(LogLevel.Info, $"  * Size of {bytes * 1e-3} kB = {bytes * 1e-6} MB ({bytes}o)");
    }

    private void EnsureValidColorBuffer()
    {
        if (colorBuffer is null || colorBuffer.IsDisposed)
            colorBuffer = new(Engine.Graphics.GraphicsDevice, clouds.Length, 1);
    }

    public void ConfigureColors(Color bg, Color[] gradientFrom, Color[] gradientTo, float lerp)
    {
        sky = bg;
        foreach (Ring ring in rings)
        {
            Color from = Util.ColorArrayLerp(ring.Lerp * (gradientFrom.Length - 1), gradientFrom);
            Color to = Util.ColorArrayLerp(ring.Lerp * (gradientTo.Length - 1), gradientTo);
            ring.ApplyIdleColor(Color.Lerp(from, to, lerp), colors);
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
        if (!lightning)
            return;

        this.lightningColors = lightningColors;
        this.lightningFlashColor = lightningFlashColor;
        this.lightningMinDelay = lightningMinDelay;
        this.lightningMaxDelay = lightningMaxDelay;
        this.lightningMinDuration = lightningMinDuration;
        this.lightningMaxDuration = lightningMaxDuration;
        this.lightningIntensity = lightningIntensity;
    }

    public override void Update(Scene scene)
    {
        base.Update(scene);

        if (!Visible)
            return;

        translate = offset - (scene as Level).Camera.Position * parallax;

        // calculate colors once for each cloud, and store them in the color buffer texture.
        // it will be sent to the gpu so it can be sampled, instead of changing the color of each vertex (old & slow method)
        for (int i = 0; i < clouds.Length; i++)
        {
            var cloud = clouds[i];
            cloud.Update(lightning);
            colors[i] = cloud.CalculateColor();
        }
    }

    public override void BeforeRender(Scene scene)
    {
        base.BeforeRender(scene);
        
        EnsureValidColorBuffer();
        colorBuffer.SetData(colors);
    }

    public override void Render(Scene scene)
    {
        // assuming that GameplayBuffers.Level is the buffer the styleground is being rendered to is wrong.
        // in some cases, (with styleground masks for instance), the backdrop is redirected to be rendered onto another buffer.
        // so we can use GraphicsDevice.GetRenderTargets and select the first one to render it here.
        // in most cases, that'll just end up being GameplayBuffers.Level anyway.
        RenderTarget2D rt = GameplayBuffers.Level;
        RenderTargetBinding[] renderTargets = Engine.Graphics.GraphicsDevice.GetRenderTargets();
        if (renderTargets.Length > 0)
            rt = renderTargets[0].RenderTarget as RenderTarget2D ?? rt;

        Engine.Graphics.GraphicsDevice.SetRenderTarget(buffer);
        Engine.Graphics.GraphicsDevice.Clear(sky);
        Engine.Graphics.GraphicsDevice.BlendState = BlendState.AlphaBlend;

        EffectParameterCollection parameters = CommunalHelperGFX.CloudscapeShader.Parameters;

        // passing the textures to the shader via Graphics.Textures does not work on XNA.
        parameters["atlas_texture"].SetValue(CommunalHelperGFX.CloudscapeAtlas.Sources[0].Texture_Safe);
        parameters["color_texture"].SetValue(colorBuffer);

        // rotation is calculated in the shader, since we're only doing one draw call now.
        parameters["ring_count"].SetValue(rings.Length);
        parameters["color_buffer_size"].SetValue(colorBuffer.Width);
        parameters["offset"].SetValue(translate);
        parameters["inner_rotation"].SetValue(innerRotation);
        parameters["outer_rotation"].SetValue(outerRotation);
        parameters["rotation_exponent"].SetValue(rotationExponent);
        parameters["time"].SetValue(scene.TimeActive);

        var technique = CommunalHelperGFX.CloudscapeShader.Techniques[0];
        foreach (var pass in technique.Passes)
        {
            pass.Apply();
            mesh.Draw();
        }

        // important because used by some vanilla celeste shader
        Engine.Graphics.GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;
        Engine.Graphics.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

        // present onto RT
        Engine.Instance.GraphicsDevice.SetRenderTarget(rt);

        BackdropRenderer renderer = (scene as Level).Background;
        renderer.StartSpritebatch(blend);
        Draw.SpriteBatch.Draw(buffer, Vector2.Zero, Color.White);
        renderer.EndSpritebatch();
    }

    public override void Ended(Scene scene)
    {
        base.Ended(scene);

        colorBuffer.Dispose();
        colorBuffer = null;
    }

    internal static void Initalize()
    {
        cloudTextures = CommunalHelperGFX.CloudscapeAtlas.GetAtlasSubtextures(string.Empty).ToArray();
        buffer = VirtualContent.CreateRenderTarget("communal_helper/shared_cloudscape_buffer", 320, 180);
    }

    internal static void Unload()
    {
        buffer.Dispose();
        buffer = null;
    }
}
