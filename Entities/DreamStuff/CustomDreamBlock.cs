using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;

namespace Celeste.Mod.CommunalHelper {
    abstract class CustomDreamBlock : DreamBlock {
        private struct DreamParticle {
            public Vector2 Position;
            public int Layer;
            public Color Color;
            public float TimeOffset;

            // Feather particle stuff
            public float Speed;
            public float Spin;
            public float MaxRotate;
            public float RotationCounter;
        }

        private static readonly Color activeBackColor = Color.Black;
        private static readonly Color disabledBackColor = Calc.HexToColor("1f2e2d");
        protected Color activeLineColor = Color.White;
        private static readonly Color disabledLineColor = Calc.HexToColor("6a8480");

        public float animTimer;

        private MTexture[] particleTextures;
        private MTexture[] featherTextures;
        private bool playerHasDreamDash;
        private DreamParticle[] particles;

        private float wobbleEase;
        private float wobbleFrom = Calc.Random.NextFloat((float)Math.PI * 2f);
        private float wobbleTo = Calc.Random.NextFloat((float)Math.PI * 2f);

        public bool featherMode;
        public bool oneUse;
        protected bool shattering = false;
        public float colorLerp = 0.0f;

        protected Vector2 shake = Vector2.Zero;
        private bool shakeToggle = false;
        private ParticleType shakeParticle;
        private float[] particleRemainders = new float[4];

        public CustomDreamBlock(Vector2 position, int width, int height, bool featherMode, bool oneUse, bool altLineColor = false)
            : base(position, width, height, null, false, false) {
            this.featherMode = featherMode;
            this.oneUse = oneUse;
            if (altLineColor) {
                activeLineColor = Calc.HexToColor("FF66D9");
            }
            shakeParticle = new ParticleType(SwitchGate.P_Behind) {
                Color = activeLineColor,
                ColorMode = ParticleType.ColorModes.Static,
                Acceleration = Vector2.Zero,
                DirectionRange = (float)Math.PI / 2
            };

            particleTextures = new MTexture[4]
            {
                GFX.Game["objects/dreamblock/particles"].GetSubtexture(14, 0, 7, 7),
                GFX.Game["objects/dreamblock/particles"].GetSubtexture(7, 0, 7, 7),
                GFX.Game["objects/dreamblock/particles"].GetSubtexture(0, 0, 7, 7),
                GFX.Game["objects/dreamblock/particles"].GetSubtexture(7, 0, 7, 7)
            };
            featherTextures = new MTexture[3];
            featherTextures[0] = GFX.Game["particles/CommunalHelper/featherBig"];
            featherTextures[1] = GFX.Game["particles/CommunalHelper/featherMedium"];
            featherTextures[2] = GFX.Game["particles/CommunalHelper/featherSmall"];
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            playerHasDreamDash = SceneAs<Level>().Session.Inventory.DreamDash;
        }

        public override void Removed(Scene scene) {
            base.Removed(scene);
            Glitch.Value = 0f;
        }

        // Called by implementing classes
        protected void SetupParticles(float canvasWidth, float canvasHeight) {
            float countFactor = featherMode ? 0.5f : 0.7f;
            particles = new DreamParticle[(int)(canvasWidth / 8f * (canvasHeight / 8f) * 0.7f * countFactor)];
            for (int i = 0; i < particles.Length; i++) {
                particles[i].Position = new Vector2(Calc.Random.NextFloat(canvasWidth), Calc.Random.NextFloat(canvasHeight));
                particles[i].Layer = Calc.Random.Choose(0, 1, 1, 2, 2, 2);
                particles[i].TimeOffset = Calc.Random.NextFloat();

                if (playerHasDreamDash) {
                    switch (particles[i].Layer) {
                        case 0:
                            particles[i].Color = Calc.Random.Choose(Calc.HexToColor("FFEF11"), Calc.HexToColor("FF00D0"), Calc.HexToColor("08a310"));
                            break;
                        case 1:
                            particles[i].Color = Calc.Random.Choose(Calc.HexToColor("5fcde4"), Calc.HexToColor("7fb25e"), Calc.HexToColor("E0564C"));
                            break;
                        case 2:
                            particles[i].Color = Calc.Random.Choose(Calc.HexToColor("5b6ee1"), Calc.HexToColor("CC3B3B"), Calc.HexToColor("7daa64"));
                            break;
                    }
                } else {
                    particles[i].Color = Color.LightGray * (0.5f + (float)particles[i].Layer / 2f * 0.5f);
                }

                #region Feather particle stuff
                if (featherMode) {
                    particles[i].Speed = Calc.Random.Range(6f, 16f);
                    particles[i].Spin = Calc.Random.Range(8f, 12f) * 0.2f;
                    particles[i].RotationCounter = Calc.Random.NextAngle();
                    particles[i].MaxRotate = Calc.Random.Range(0.3f, 0.6f) * ((float)Math.PI / 2f);
                }
                #endregion
            }
        }

        public override void Update() {
            base.Update();
            if (playerHasDreamDash && Collidable) {
                animTimer += 6f * Engine.DeltaTime;
                wobbleEase += Engine.DeltaTime * 2f;
                if (wobbleEase > 1f) {
                    wobbleEase = 0f;
                    wobbleFrom = wobbleTo;
                    wobbleTo = Calc.Random.NextFloat((float)Math.PI * 2f);
                }

                if (featherMode) {
                    UpdateParticles();
                }

                if (oneUse && Scene.OnInterval(0.03f)) {
                    if (shakeToggle) {
                        shake.X = Calc.Random.Next(-1, 2);
                    } else {
                        shake.Y = Calc.Random.Next(-1, 2);
                    }
                    shakeToggle = !shakeToggle;
                    if (!shattering) ShakeParticles();
                }
            }
        }

        private void ShakeParticles() {
            Vector2 position;
            Vector2 positionRange;
            float num;
            float num2;
            for (int i = 0; i < 4; ++i) {
                switch(i) {
                    case 0:
                        position = base.CenterLeft + Vector2.UnitX;
                        positionRange = Vector2.UnitY * (base.Height - 4f);
                        num = (float)Math.PI;
                        num2 = base.Height / 32f;
                        break;
                    case 1:
                        position = base.CenterRight;
                        positionRange = Vector2.UnitY * (base.Height - 4f);
                        num = 0f;
                        num2 = base.Height / 32f;
                        break;
                    case 2:
                        position = base.TopCenter + Vector2.UnitY;
                        positionRange = Vector2.UnitX * (base.Width - 4f);
                        num = -(float)Math.PI / 2f;
                        num2 = base.Width / 32f;
                        break;
                    default:
                        position = base.BottomCenter;
                        positionRange = Vector2.UnitX * (base.Width - 4f);
                        num = (float)Math.PI / 2f;
                        num2 = base.Width / 32f;
                        break;
                }

                num2 *= 0.25f;
                particleRemainders[i] += num2;
                int num3 = (int)particleRemainders[i];
                particleRemainders[i] -= num3;
                positionRange *= 0.5f;
                if (num3 > 0f) {
                    SceneAs<Level>().ParticlesBG.Emit(shakeParticle, num3, position, positionRange, num);
                }
            }
        }

        private void UpdateParticles() {
            for (int i = 0; i < particles.Length; i++) {
                particles[i].Position.Y += 0.5f * particles[i].Speed * GetLayerScaleFactor(particles[i].Layer) * Engine.DeltaTime;
                particles[i].RotationCounter += particles[i].Spin * Engine.DeltaTime;
            }
        }

        private float GetLayerScaleFactor(int layer) {
            return 1 / (0.3f + 0.25f * layer);
        }

        public override void Render() {
            Position += shake;
            Camera camera = SceneAs<Level>().Camera;
            if (base.Right < camera.Left || base.Left > camera.Right || base.Bottom < camera.Top || base.Top > camera.Bottom) {
                return;
            }

            Color backColor = Color.Lerp(playerHasDreamDash ? activeBackColor : disabledBackColor, activeLineColor, colorLerp);
            Draw.Rect(base.X, base.Y, base.Width, base.Height, backColor);

            #region Particle rendering
            Vector2 cameraPositon = SceneAs<Level>().Camera.Position;
            for (int i = 0; i < particles.Length; i++) {
                DreamParticle particle = particles[i];
                int layer = particle.Layer;
                Vector2 position = particle.Position + cameraPositon * (0.3f + 0.25f * layer);
                float rotation = 1.5707963705062866f - 0.8f + (float)Math.Sin(particle.RotationCounter * particle.MaxRotate);
                if (featherMode) {
                    position += Calc.AngleToVector(rotation, 4f);
                }
                position = PutInside(position, new Rectangle((int)X, (int)Y, (int)Width, (int)Height));
                if (!CheckParticleCollide(position)) continue;

                Color color = Color.Lerp(particle.Color, Color.Black, colorLerp);

                if (featherMode) {
                    featherTextures[layer].DrawCentered(position, color, 1, rotation);
                } else {
                    MTexture particleTexture;
                    switch (layer) {
                        case 0: {
                                int index = (int)((particle.TimeOffset * 4f + animTimer) % 4f);
                                particleTexture = particleTextures[3 - index];
                                break;
                            }
                        case 1: {
                                int index = (int)((particle.TimeOffset * 2f + animTimer) % 2f);
                                particleTexture = particleTextures[1 + index];
                                break;
                            }
                        default:
                            particleTexture = particleTextures[2];
                            break;
                    }
                    particleTexture.DrawCentered(position, color);
                }
            }
            #endregion

            WobbleLine(new Vector2(base.X, base.Y), new Vector2(base.X + base.Width, base.Y), 0f);
            WobbleLine(new Vector2(base.X + base.Width, base.Y), new Vector2(base.X + base.Width, base.Y + base.Height), 0.7f);
            WobbleLine(new Vector2(base.X + base.Width, base.Y + base.Height), new Vector2(base.X, base.Y + base.Height), 1.5f);
            WobbleLine(new Vector2(base.X, base.Y + base.Height), new Vector2(base.X, base.Y), 2.5f);
            Draw.Rect(new Vector2(base.X, base.Y), 2f, 2f, playerHasDreamDash ? activeLineColor : disabledLineColor);
            Draw.Rect(new Vector2(base.X + base.Width - 2f, base.Y), 2f, 2f, playerHasDreamDash ? activeLineColor : disabledLineColor);
            Draw.Rect(new Vector2(base.X, base.Y + base.Height - 2f), 2f, 2f, playerHasDreamDash ? activeLineColor : disabledLineColor);
            Draw.Rect(new Vector2(base.X + base.Width - 2f, base.Y + base.Height - 2f), 2f, 2f, playerHasDreamDash ? activeLineColor : disabledLineColor);
            Position -= shake;
        }

        protected void WobbleLine(Vector2 from, Vector2 to, float offset) {
            Color lineColor = playerHasDreamDash ? activeLineColor : disabledLineColor;
            Color backColor = Color.Lerp(playerHasDreamDash ? activeBackColor : disabledBackColor, activeLineColor, colorLerp);

            float num = (to - from).Length();
            Vector2 value = Vector2.Normalize(to - from);
            Vector2 vector = new Vector2(value.Y, 0f - value.X);
            float scaleFactor = 0f;
            int num2 = 16;
            for (int i = 2; (float)i < num - 2f; i += num2) {
                float num3 = Lerp(LineAmplitude(wobbleFrom + offset, i), LineAmplitude(wobbleTo + offset, i), wobbleEase);
                if ((float)(i + num2) >= num) {
                    num3 = 0f;
                }
                float num4 = Math.Min(num2, num - 2f - (float)i);
                Vector2 vector2 = from + value * i + vector * scaleFactor;
                Vector2 vector3 = from + value * ((float)i + num4) + vector * num3;
                Draw.Line(vector2 - vector, vector3 - vector, backColor);
                Draw.Line(vector2 - vector * 2f, vector3 - vector * 2f, backColor);
                Draw.Line(vector2 - vector * 3f, vector3 - vector * 3f, backColor);
                Draw.Line(vector2, vector3, lineColor);
                scaleFactor = num3;
            }
        }

        private float LineAmplitude(float seed, float index) {
            return (float)(Math.Sin((double)(seed + index / 16f) + Math.Sin(seed * 2f + index / 32f) * 6.2831854820251465) + 1.0) * 1.5f;
        }

        private float Lerp(float a, float b, float percent) {
            return a + (b - a) * percent;
        }

        protected bool CheckParticleCollide(Vector2 position) {
            float offset = 2f;
            return position.X >= X + offset && position.Y >= Y + offset && position.X < Right - offset && position.Y < Bottom - offset;
        }

        public virtual void BeginShatter() {
            if (ShatterCheck()) {
                shattering = true;
                Audio.Play("event:/CommunalHelperEvents/game/connectedDreamBlock/dreamblock_shatter", Position);
                Add(new Coroutine(ShatterSeq()));
            }
        }

        protected virtual bool ShatterCheck() {
            return !shattering;
        }

        private IEnumerator ShatterSeq() {
            yield return 0.28f;
            while (colorLerp < 2.0f) {
                colorLerp += Engine.DeltaTime * 10.0f;
                yield return null;
            }
            colorLerp = 1.0f;
            yield return 0.05f;

            Level level = SceneAs<Level>();
            level.Shake(.65f);
            Vector2 camera = SceneAs<Level>().Camera.Position;

            for (int i = 0; i < particles.Length; i++) {
                Vector2 position = particles[i].Position;
                position += camera * (0.3f + 0.25f * particles[i].Layer);
                position = PutInside(position, new Rectangle((int)X, (int)Y, (int)Width, (int)Height));

                Color flickerColor = Color.Lerp(particles[i].Color, Color.White, 0.6f);
                ParticleType type = new ParticleType(Lightning.P_Shatter) {
                    ColorMode = ParticleType.ColorModes.Fade,
                    Color = particles[i].Color,
                    Color2 = flickerColor,
                    Source = featherMode ? featherTextures[particles[i].Layer] : particleTextures[2],
                    SpinMax = featherMode ? (float)Math.PI : 0,
                    RotationMode = featherMode ? ParticleType.RotationModes.Random : ParticleType.RotationModes.None,
                    Direction = (position - Center).Angle()
                };
                level.ParticlesFG.Emit(type, 1, position, Vector2.One * 3f);
            }
            OneUseDestroy();

            Glitch.Value = 0.22f;
            while (Glitch.Value > 0.0f) {
                Glitch.Value -= 0.5f * Engine.DeltaTime;
                yield return null;
            }
            Glitch.Value = 0.0f;
            RemoveSelf();
        }

        protected virtual void OneUseDestroy() {
            Collidable = (Visible = false);
            DisableStaticMovers();
        }

        private Vector2 PutInside(Vector2 pos, Rectangle r) {
            while (pos.X < r.X) {
                pos.X += r.Width;
            }
            while (pos.X > r.X + r.Width) {
                pos.X -= r.Width;
            }
            while (pos.Y < r.Y) {
                pos.Y += r.Height;
            }
            while (pos.Y > r.Y + r.Height) {
                pos.Y -= r.Height;
            }
            return pos;
        }
    }

    class CustomDreamBlockHooks {
        public static void Hook() {
            On.Celeste.Player.DreamDashBegin += modDreamDashBegin;
            On.Celeste.Player.DreamDashUpdate += modDreamDashUpdate;
            On.Celeste.DreamBlock.OnPlayerExit += modOnPlayerExit;
        }

        public static void Unhook() {
            On.Celeste.Player.DreamDashBegin -= modDreamDashBegin;
            On.Celeste.Player.DreamDashUpdate -= modDreamDashUpdate;
            On.Celeste.DreamBlock.OnPlayerExit -= modOnPlayerExit;
        }

        private static void modDreamDashBegin(On.Celeste.Player.orig_DreamDashBegin orig, Player player) {
            orig(player);
            var playerData = getPlayerData(player);
            DreamBlock dreamBlock = playerData.Get<DreamBlock>("dreamBlock");
            if (dreamBlock is CustomDreamBlock && (dreamBlock as CustomDreamBlock).featherMode) {
                SoundSource dreamSfxLoop = playerData.Get<SoundSource>("dreamSfxLoop");
                player.Stop(dreamSfxLoop);
                player.Loop(dreamSfxLoop, "event:/CommunalHelperEvents/game/connectedDreamBlock/dreamblock_fly_travel");
            }

        }

        private static int modDreamDashUpdate(On.Celeste.Player.orig_DreamDashUpdate orig, Player player) {
            var playerData = getPlayerData(player);
            DreamBlock dreamBlock = playerData.Get<DreamBlock>("dreamBlock");
            if (dreamBlock is CustomDreamBlock && (dreamBlock as CustomDreamBlock).featherMode) {
                Vector2 input = Input.Aim.Value.SafeNormalize(Vector2.Zero);
                if (input != Vector2.Zero) {
                    Vector2 vector = player.Speed.SafeNormalize(Vector2.Zero);
                    if (vector != Vector2.Zero) {
                        vector = Vector2.Dot(input, vector) != -0.8f ? vector.RotateTowards(input.Angle(), 5f * Engine.DeltaTime) : vector;
                        player.Speed = vector * 240f;
                    }
                }
            }
            return orig(player);
        }

        private static void modOnPlayerExit(On.Celeste.DreamBlock.orig_OnPlayerExit orig, DreamBlock dreamBlock, Player player) {
            orig(dreamBlock, player);
            if (dreamBlock is CustomDreamBlock) {
                CustomDreamBlock customDreamBlock = dreamBlock as CustomDreamBlock;
                if (customDreamBlock.oneUse && customDreamBlock.Collidable) {
                    customDreamBlock.BeginShatter();
                }
            }
        }

        private static DynData<Player> getPlayerData(Player player) {
            return new DynData<Player>(player);
        }
    }
}
