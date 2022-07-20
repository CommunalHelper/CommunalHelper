using Celeste.Mod.CommunalHelper.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Backdrops {
    public class Cloudscape : Backdrop {
        public const string ID = "CommunalHelper/Cloudscape";

        private static MTexture[] cloudTextures;

        private const int LEVEL_OF_DETAIL = 16;
        private const int STRIPE_SIZE = sizeof(float) * 5 + sizeof(byte) * 4;

        private class WarpedCloud {
            private readonly Cloudscape backdrop;
            private readonly Mesh<VertexPositionColorTexture> mesh;

            private readonly int index;
            private readonly Color idleColor;
            private Color targetColorA, targetColorB;

            private float timer = 0f;
            private float flashDuration = 1f, flashTimer = 0f;
            private float intensity;

            private float oldPercent;

            public WarpedCloud(Cloudscape backdrop, Mesh<VertexPositionColorTexture> mesh, int index, Color idleColor) {
                this.backdrop = backdrop;
                this.mesh = mesh;
                this.index = index;
                this.idleColor = idleColor;

                timer = Calc.Random.Range(backdrop.lightningMinDelay, backdrop.lightningMaxDelay) * Calc.Random.NextFloat();
            }

            private void UpdateColors() {
                float percent = flashTimer / flashDuration;
                if (oldPercent == percent)
                    return;

                float sin = ((float) Math.Sin(percent * 10) + 1) / 2f;
                Color target = Color.Lerp(targetColorA, targetColorB, sin);
                Color lightning = Color.Lerp(idleColor, target, Ease.BounceIn(percent) * (1 - Ease.CubeIn(percent)));
                Color flash = intensity > 0 ? Color.Lerp(lightning, backdrop.lightningFlashColor, intensity * Ease.ExpoIn(percent)) : lightning;

                int to = LEVEL_OF_DETAIL * 2;
                for (int i = 0; i < to; i++)
                    mesh.Vertices[index + i].Color = flash;

                oldPercent = percent;
            }

            public void Update() {
                timer -= Engine.DeltaTime;
                if (timer <= 0) {
                    timer = Calc.Random.Range(backdrop.lightningMinDelay, backdrop.lightningMaxDelay);
                    flashTimer = flashDuration = Calc.Random.Range(backdrop.lightningMinDuration, backdrop.lightningMaxDuration);
                    intensity = Settings.Instance.DisableFlashes ? 0 : backdrop.lightningIntensity * Ease.CubeIn(Calc.Random.NextFloat());
                    targetColorA = Util.ColorArrayLerp(Calc.Random.NextFloat() * (backdrop.lightningColors.Length - 1), backdrop.lightningColors);
                    targetColorB = Util.ColorArrayLerp(Calc.Random.NextFloat() * (backdrop.lightningColors.Length - 1), backdrop.lightningColors);
                }

                if (flashTimer > 0)
                    flashTimer = Calc.Approach(flashTimer, 0, Engine.DeltaTime);

                UpdateColors();
            }
        }

        private class Ring {
            public readonly Mesh<VertexPositionColorTexture> Mesh = new(GFX.FxTexture);

            private float rotation;
            public float RotationalVelocity;

            private Matrix matrix;

            public Ring(Cloudscape backdrop, float radius, Color color, List<WarpedCloud> clouds) {
                rotation = Calc.Random.NextFloat(MathHelper.TwoPi);

                float angle = 0f;
                while (angle < MathHelper.TwoPi) {
                    MTexture texture = Calc.Random.Choose(cloudTextures);

                    int index = Mesh.VertexCount;
                    clouds.Add(new(backdrop, Mesh, index, color));

                    float centralAngle = texture.Width / radius;
                    float step = centralAngle / LEVEL_OF_DETAIL;
                    float halfHeight = texture.Height / 2f;
                    for (int i = 0; i < LEVEL_OF_DETAIL; i++) {
                        float th = angle + step * i;

                        float uvx = MathHelper.Lerp(texture.LeftUV, texture.RightUV, (float) i / (LEVEL_OF_DETAIL - 1));
                        VertexPositionColorTexture closerVertex = new(new(Calc.AngleToVector(th, radius - halfHeight), 0), color, new(uvx, texture.TopUV));
                        VertexPositionColorTexture fartherVertex = new(new(Calc.AngleToVector(th, radius + halfHeight), 0), color, new(uvx, texture.BottomUV));
                        Mesh.AddVertices(closerVertex, fartherVertex);

                        if (i != LEVEL_OF_DETAIL - 1) {
                            int o = index + i * 2;
                            Mesh.AddTriangle(o + 0, o + 1, o + 2);
                            Mesh.AddTriangle(o + 1, o + 2, o + 3);
                        }
                    }

                    angle += centralAngle;
                }

                Mesh.Bake();
            }

            public void Update(Matrix translation) {
                rotation += RotationalVelocity * Engine.DeltaTime;
                matrix = Matrix.CreateRotationZ(rotation) * translation;
            }

            public void Render() {
                Mesh.Celeste_DrawVertices(matrix);
            }

            public void DisposeMesh()
                => Mesh.Dispose();
        }

        private readonly Ring[] rings;
        private readonly List<WarpedCloud> clouds = new();

        private readonly Color sky;

        private VirtualRenderTarget buffer;
        private Matrix matrix;

        public bool Lightning;
        private readonly float lightningMinDelay, lightningMaxDelay;
        private readonly float lightningMinDuration, lightningMaxDuration;
        private readonly float lightningIntensity;
        private readonly Color[] lightningColors;
        private readonly Color lightningFlashColor;

        public Cloudscape(BinaryPacker.Element child) : this(
            child.Attr("seed"),
            child.Attr("colors").Split(',').Select(str => Calc.HexToColor(str.Trim())).ToArray(),
            Calc.HexToColor(child.Attr("bgColor").Trim()),
            MathHelper.Max(10, child.AttrFloat("innerRadius", 40f)),
            child.AttrFloat("outerRadius", 400f),
            child.AttrInt("rings", 24),
            child.AttrBool("lightning"),
            child.Attr("lightningColors").Split(',').Select(str => Calc.HexToColor(str.Trim())).ToArray(),
            Calc.HexToColor(child.Attr("lightningFlashColor").Trim()),
            MathHelper.Max(0, child.AttrFloat("lightningMinDelay", 5.0f)),
            MathHelper.Max(0, child.AttrFloat("lightningMaxDelay", 40.0f)),
            MathHelper.Max(0, child.AttrFloat("lightningMinDuration", 0.5f)),
            MathHelper.Max(0, child.AttrFloat("lightningMaxDuration", 1.0f)),
            MathHelper.Clamp(child.AttrFloat("lightningIntensity", 0.5f), 0f, 1f)
        ) { }

        public Cloudscape(
                string seed,
                Color[] colors, Color sky,
                float innerRadius, float outerRadius,
                int count,
                bool lightning,
                Color[] lightningColors,
                Color lightningFlashColor,
                float lightningMinDelay, float lightningMaxDelay,
                float lightningMinDuration, float lightningMaxDuration,
                float lightningIntensity
            ) : base() {
            this.sky = sky;

            Lightning = lightning;
            this.lightningMinDelay = lightningMinDelay;
            this.lightningMaxDelay = lightningMaxDelay;
            this.lightningMinDuration = lightningMinDuration;
            this.lightningMaxDuration = lightningMaxDuration;
            this.lightningIntensity = lightningIntensity;
            this.lightningColors = lightningColors;
            this.lightningFlashColor = lightningFlashColor;

            Calc.PushRandom(seed.GetHashCode());

            rings = new Ring[count];

            int vertexCount = 0, triangleCount = 0;

            float a = MathHelper.Min(innerRadius, outerRadius);
            float b = MathHelper.Max(innerRadius, outerRadius);
            float d = b - a;
            for (int i = 0; i < count; i++) {
                float percent = (float) i / count;
                Color color = Util.ColorArrayLerp(percent * (colors.Length - 1), colors);

                float radius = a + d * percent;
                Ring ring = new(this, radius, color, clouds);
                ring.RotationalVelocity = (float) Math.Pow(radius * 0.001f, 2f);

                vertexCount += ring.Mesh.VertexCount;
                triangleCount += ring.Mesh.Triangles;

                rings[i] = ring;
            }

            float bytes = STRIPE_SIZE * vertexCount;
            Util.Log(LogLevel.Info, $"Cloudscape mesh baked:");
            Util.Log(LogLevel.Info, $"  * {vertexCount} vertices and {triangleCount} triangles ({triangleCount * 3} indices)");
            Util.Log(LogLevel.Info, $"  * Size of {bytes * 1e-3} kB = {bytes * 1e-6} MB ({bytes}o)");

            Calc.PopRandom();
        }

        public override void Update(Scene scene) {
            base.Update(scene);

            if (Visible) {
                Vector2 parallax = -(scene as Level).Camera.Position * 0.05f;
                matrix = Matrix.CreateTranslation(parallax.X, parallax.Y, 0);

                foreach (Ring ring in rings)
                    ring.Update(matrix);

                if (Lightning)
                    foreach (WarpedCloud cloud in clouds)
                        cloud.Update();
            }
        }

        public override void BeforeRender(Scene scene) {
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

        public override void Render(Scene scene) {
            base.Render(scene);

            if (buffer != null && !buffer.IsDisposed)
                Draw.SpriteBatch.Draw(buffer, Vector2.Zero, Color.White);
        }

        public override void Ended(Scene scene) {
            base.Ended(scene);

            if (buffer != null) {
                buffer.Dispose();
                buffer = null;
            }

            if (rings != null)
                foreach (Ring ring in rings)
                    ring.DisposeMesh();
        }

        internal static void InitializeTextures() {
            cloudTextures = CommunalHelperModule.CloudscapeAtlas.GetAtlasSubtextures(string.Empty).ToArray();
        }
    }
}
