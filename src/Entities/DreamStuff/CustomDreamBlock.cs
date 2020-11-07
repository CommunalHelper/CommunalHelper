using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Entities {
    [TrackedAs(typeof(DreamBlock), true)]
    public abstract class CustomDreamBlock : DreamBlock {
        protected struct DreamParticle {
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

        private static readonly MethodInfo m_DreamBlock_PutInside = typeof(DreamBlock).GetMethod("PutInside", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo m_DreamBlock_WobbleLine = typeof(DreamBlock).GetMethod("WobbleLine", BindingFlags.NonPublic | BindingFlags.Instance);

        protected MTexture[] featherTextures;
        protected DreamParticle[] particles;
        protected MTexture[] doubleRefillStarTextures;

        public bool PlayerHasDreamDash => baseData.Get<bool>("playerHasDreamDash");

        protected Color activeLineColor => baseData.Get<Color>("activeLineColor");
        protected Color disabledLineColor => baseData.Get<Color>("disabledLineColor");
        protected Color activeBackColor => baseData.Get<Color>("activeBackColor");
        protected Color disabledBackColor => baseData.Get<Color>("disabledBackColor");

        public bool FeatherMode;
        protected bool DoubleRefill;
        protected bool shattering = false;
        public float ColorLerp = 0.0f;

        private bool shakeToggle = false;
        private ParticleType shakeParticle;
        private float[] particleRemainders = new float[4];

        protected DynData<DreamBlock> baseData;

        public CustomDreamBlock(Vector2 position, int width, int height, bool featherMode, bool oneUse, bool doubleRefill, bool below)
            : base(position, width, height, null, false, oneUse, below) {
            baseData = new DynData<DreamBlock>(this);
            DoubleRefill = doubleRefill;

            FeatherMode = featherMode;
            //if (altLineColor) { Dropped in favour of symbol
            //    activeLineColor = Calc.HexToColor("FF66D9"); 
            //}
            shakeParticle = new ParticleType(SwitchGate.P_Behind) {
                Color = activeLineColor,
                ColorMode = ParticleType.ColorModes.Static,
                Acceleration = Vector2.Zero,
                DirectionRange = (float) Math.PI / 2
            };

            featherTextures = new MTexture[] {
                GFX.Game["particles/CommunalHelper/featherBig"],
                GFX.Game["particles/CommunalHelper/featherMedium"],
                GFX.Game["particles/CommunalHelper/featherSmall"]
            };

            doubleRefillStarTextures = new MTexture[4] {
                GFX.Game["objects/CommunalHelper/customDreamBlock/particles"].GetSubtexture(14, 0, 7, 7),
                GFX.Game["objects/CommunalHelper/customDreamBlock/particles"].GetSubtexture(7, 0, 7, 7),
                GFX.Game["objects/CommunalHelper/customDreamBlock/particles"].GetSubtexture(0, 0, 7, 7),
                GFX.Game["objects/CommunalHelper/customDreamBlock/particles"].GetSubtexture(7, 0, 7, 7)
            };
        }

        public override void Removed(Scene scene) {
            base.Removed(scene);
            Glitch.Value = 0f;
        }

        public virtual void SetupCustomParticles(float canvasWidth, float canvasHeight) {
            float countFactor = FeatherMode ? 0.5f : 0.7f;
            particles = new DreamParticle[(int) (canvasWidth / 8f * (canvasHeight / 8f) * 0.7f * countFactor)];
            for (int i = 0; i < particles.Length; i++) {
                particles[i].Position = new Vector2(Calc.Random.NextFloat(canvasWidth), Calc.Random.NextFloat(canvasHeight));
                particles[i].Layer = Calc.Random.Choose(0, 1, 1, 2, 2, 2);
                particles[i].TimeOffset = Calc.Random.NextFloat();

                if (PlayerHasDreamDash) {
                    if (DoubleRefill) {
                        switch (particles[i].Layer) {
                            case 0:
                                particles[i].Color = Calc.HexToColor("FFD1F9");
                                break;
                            case 1:
                                particles[i].Color = Calc.HexToColor("FC99FF");
                                break;
                            case 2:
                                particles[i].Color = Calc.HexToColor("E269D2");
                                break;
                        }
                    } else {
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
                    }
                } else {
                    particles[i].Color = Color.LightGray * (0.5f + particles[i].Layer / 2f * 0.5f);
                }

                #region Feather particle stuff

                if (FeatherMode) {
                    particles[i].Speed = Calc.Random.Range(6f, 16f);
                    particles[i].Spin = Calc.Random.Range(8f, 12f) * 0.2f;
                    particles[i].RotationCounter = Calc.Random.NextAngle();
                    particles[i].MaxRotate = Calc.Random.Range(0.3f, 0.6f) * ((float) Math.PI / 2f);
                }

                #endregion

            }
        }

        private void ShakeParticles() {
            Vector2 position;
            Vector2 positionRange;
            float angle;
            float num2;
            for (int i = 0; i < 4; ++i) {
                switch (i) {
                    case 0:
                        position = CenterLeft + Vector2.UnitX;
                        positionRange = Vector2.UnitY * (Height - 4f);
                        angle = (float) Math.PI;
                        num2 = Height / 32f;
                        break;
                    case 1:
                        position = CenterRight;
                        positionRange = Vector2.UnitY * (Height - 4f);
                        angle = 0f;
                        num2 = Height / 32f;
                        break;
                    case 2:
                        position = TopCenter + Vector2.UnitY;
                        positionRange = Vector2.UnitX * (Width - 4f);
                        angle = -(float) Math.PI / 2f;
                        num2 = Width / 32f;
                        break;
                    default:
                        position = BottomCenter;
                        positionRange = Vector2.UnitX * (Width - 4f);
                        angle = (float) Math.PI / 2f;
                        num2 = Width / 32f;
                        break;
                }

                num2 *= 0.25f;
                particleRemainders[i] += num2;
                int num3 = (int) particleRemainders[i];
                particleRemainders[i] -= num3;
                positionRange *= 0.5f;
                if (num3 > 0f) {
                    SceneAs<Level>().ParticlesBG.Emit(shakeParticle, num3, position, positionRange, angle);
                }
            }
        }

        public override void Update() {
            base.Update();

            if (FeatherMode) {
                UpdateParticles();
            }

            if (Visible && PlayerHasDreamDash && baseData.Get<bool>("oneUse") && Scene.OnInterval(0.03f)) {
                Vector2 shake = baseData.Get<Vector2>("shake");
                if (shakeToggle) {
                    shake.X = Calc.Random.Next(-1, 2);
                } else {
                    shake.Y = Calc.Random.Next(-1, 2);
                }
                baseData["shake"] = shake;
                shakeToggle = !shakeToggle;
                if (!shattering)
                    ShakeParticles();
            }
        }

        protected virtual void UpdateParticles() {
            if (PlayerHasDreamDash) {
                for (int i = 0; i < particles.Length; i++) {
                    particles[i].Position.Y += 0.5f * particles[i].Speed * GetLayerScaleFactor(particles[i].Layer) * Engine.DeltaTime;
                    particles[i].RotationCounter += particles[i].Spin * Engine.DeltaTime;
                }
            }
        }

        private float GetLayerScaleFactor(int layer) => 1 / (0.3f + 0.25f * layer);

        protected void WobbleLine(Vector2 from, Vector2 to, float offset) =>
            m_DreamBlock_WobbleLine.Invoke(this, new object[] { from, to, offset });

        public override void Render() {
            Camera camera = SceneAs<Level>().Camera;
            if (Right < camera.Left || Left > camera.Right || Bottom < camera.Top || Top > camera.Bottom) {
                return;
            }

            Vector2 shake = baseData.Get<Vector2>("shake");

            float whiteFill = baseData.Get<float>("whiteFill");
            float whiteHeight = baseData.Get<float>("whiteHeight");

            Color backColor = Color.Lerp(PlayerHasDreamDash ? activeBackColor : disabledBackColor, Color.White, ColorLerp);
            Color lineColor = PlayerHasDreamDash ? activeLineColor : disabledLineColor;

            Draw.Rect(shake.X + X, shake.Y + Y, Width, Height, backColor);

            #region Particles

            Vector2 cameraPositon = camera.Position;
            for (int i = 0; i < particles.Length; i++) {
                DreamParticle particle = particles[i];
                int layer = particle.Layer;
                Vector2 position = particle.Position + cameraPositon * (0.3f + 0.25f * layer);
                float rotation = (float) Math.PI / 2f - 0.8f + (float) Math.Sin(particle.RotationCounter * particle.MaxRotate);
                if (FeatherMode) {
                    position += Calc.AngleToVector(rotation, 4f);
                }
                position = (Vector2) m_DreamBlock_PutInside.Invoke(this, new object[] { position });
                if (!CheckParticleCollide(position))
                    continue;

                Color color = Color.Lerp(particle.Color, Color.Black, ColorLerp);

                if (FeatherMode) {
                    featherTextures[layer].DrawCentered(position, color, 1, rotation);
                } else {
                    MTexture[] particleTextures = DoubleRefill ? doubleRefillStarTextures : baseData.Get<MTexture[]>("particleTextures");
                    MTexture particleTexture;
                    switch (layer) {
                        case 0: {
                            int index = (int) ((particle.TimeOffset * 4f + baseData.Get<float>("animTimer")) % 4f);
                            particleTexture = particleTextures[3 - index];
                            break;
                        }
                        case 1: {
                            int index = (int) ((particle.TimeOffset * 2f + baseData.Get<float>("animTimer")) % 2f);
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

            if (whiteFill > 0f) {
                Draw.Rect(X + shake.X, Y + shake.Y, Width, Height * whiteHeight, Color.White * whiteFill);
            }

            WobbleLine(shake + new Vector2(X, Y), shake + new Vector2(X + Width, Y), 0f);
            WobbleLine(shake + new Vector2(X + Width, Y), shake + new Vector2(X + Width, Y + Height), 0.7f);
            WobbleLine(shake + new Vector2(X + Width, Y + Height), shake + new Vector2(X, Y + Height), 1.5f);
            WobbleLine(shake + new Vector2(X, Y + Height), shake + new Vector2(X, Y), 2.5f);

            Draw.Rect(shake + new Vector2(X, Y), 2f, 2f, lineColor);
            Draw.Rect(shake + new Vector2(X + Width - 2f, Y), 2f, 2f, lineColor);
            Draw.Rect(shake + new Vector2(X, Y + Height - 2f), 2f, 2f, lineColor);
            Draw.Rect(shake + new Vector2(X + Width - 2f, Y + Height - 2f), 2f, 2f, lineColor);
        }

        protected bool CheckParticleCollide(Vector2 position) {
            float offset = 2f;
            return position.X >= X + offset && position.Y >= Y + offset && position.X < Right - offset && position.Y < Bottom - offset;
        }

        protected bool ShatterCheck() => !shattering;

        public virtual void BeginShatter() {
            if (ShatterCheck()) {
                shattering = true;
                Audio.Play(CustomSFX.game_connectedDreamBlock_dreamblock_shatter, Position);
                Add(new Coroutine(ShatterSequence()));
            }
        }

        private IEnumerator ShatterSequence() {
            yield return 0.28f;
            while (ColorLerp < 2.0f) {
                ColorLerp += Engine.DeltaTime * 10.0f;
                yield return null;
            }
            ColorLerp = 1.0f;
            yield return 0.05f;

            Level level = SceneAs<Level>();
            level.Shake(.65f);
            Vector2 camera = level.Camera.Position;

            for (int i = 0; i < particles.Length; i++) {
                Vector2 position = particles[i].Position;
                position += camera * (0.3f + 0.25f * particles[i].Layer);
                position = (Vector2) m_DreamBlock_PutInside.Invoke(this, new object[] { position });

                Color flickerColor = Color.Lerp(particles[i].Color, Color.White, 0.6f);
                ParticleType type = new ParticleType(Lightning.P_Shatter) {
                    ColorMode = ParticleType.ColorModes.Fade,
                    Color = particles[i].Color,
                    Color2 = flickerColor,
                    Source = FeatherMode ? featherTextures[particles[i].Layer] : baseData.Get<MTexture[]>("particleTextures")[2],
                    SpinMax = FeatherMode ? (float) Math.PI : 0,
                    RotationMode = FeatherMode ? ParticleType.RotationModes.Random : ParticleType.RotationModes.None,
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
            Collidable = Visible = false;
            DisableStaticMovers();
        }

        #region Hooks

        internal static void Load() {
            On.Celeste.DreamBlock.Setup += DreamBlock_Setup;
            On.Celeste.DreamBlock.OnPlayerExit += DreamBlock_OnPlayerExit;
            On.Celeste.DreamBlock.OneUseDestroy += DreamBlock_OneUseDestroy;

            On.Celeste.Player.DreamDashBegin += Player_DreamDashBegin;
            On.Celeste.Player.DreamDashUpdate += Player_DreamDashUpdate;

            ConnectedDreamBlock.Hook();
            DreamMoveBlock.Load();
            DreamCrumbleWallOnRumble.Load();
        }

        internal static void Unload() {
            On.Celeste.DreamBlock.Setup -= DreamBlock_Setup;
            On.Celeste.DreamBlock.OnPlayerExit -= DreamBlock_OnPlayerExit;
            On.Celeste.DreamBlock.OneUseDestroy -= DreamBlock_OneUseDestroy;

            On.Celeste.Player.DreamDashBegin -= Player_DreamDashBegin;
            On.Celeste.Player.DreamDashUpdate -= Player_DreamDashUpdate;

            ConnectedDreamBlock.Unhook();
            DreamMoveBlock.Unload();
            DreamCrumbleWallOnRumble.Unload();
        }

        private static void DreamBlock_Setup(On.Celeste.DreamBlock.orig_Setup orig, DreamBlock self) {
            if (self is CustomDreamBlock block)
                block.SetupCustomParticles(block.Width, block.Height);
            else
                orig(self);
        }

        private static void DreamBlock_OnPlayerExit(On.Celeste.DreamBlock.orig_OnPlayerExit orig, DreamBlock dreamBlock, Player player) {
            orig(dreamBlock, player);
            if (dreamBlock is CustomDreamBlock customDreamBlock) {
                if (customDreamBlock.DoubleRefill) {
                    player.Dashes = 2;
                }
            }
        }

        private static void DreamBlock_OneUseDestroy(On.Celeste.DreamBlock.orig_OneUseDestroy orig, DreamBlock self) {
            if (self is CustomDreamBlock customDreamBlock && customDreamBlock.Collidable)
                customDreamBlock.BeginShatter();
            else
                orig(self);
        }

        private static void Player_DreamDashBegin(On.Celeste.Player.orig_DreamDashBegin orig, Player player) {
            orig(player);
            var playerData = player.GetData();
            DreamBlock dreamBlock = playerData.Get<DreamBlock>("dreamBlock");
            if (dreamBlock is CustomDreamBlock customDreamBlock) { 
                if (customDreamBlock.FeatherMode) {
                    SoundSource dreamSfxLoop = playerData.Get<SoundSource>("dreamSfxLoop");
                    player.Stop(dreamSfxLoop);
                    player.Loop(dreamSfxLoop, CustomSFX.game_connectedDreamBlock_dreamblock_fly_travel);
                }

                // Ensures the player always properly enters a dream block even when it's moving fast
                if (customDreamBlock is DreamZipMover || customDreamBlock is DreamSwapBlock) {
                    player.Position.X += Math.Sign(player.DashDir.X);
                    player.Position.Y += Math.Sign(player.DashDir.Y);
                }
            }

        }

        private static int Player_DreamDashUpdate(On.Celeste.Player.orig_DreamDashUpdate orig, Player player) {
            var playerData = player.GetData();
            DreamBlock dreamBlock = playerData.Get<DreamBlock>("dreamBlock");
            if (dreamBlock is CustomDreamBlock customDreamBlock && customDreamBlock.FeatherMode) {
                Vector2 input = Input.Aim.Value.SafeNormalize();
                if (input != Vector2.Zero) {
                    Vector2 vector = player.Speed.SafeNormalize();
                    if (vector != Vector2.Zero) {
                        vector = Vector2.Dot(input, vector) != -0.8f ? vector.RotateTowards(input.Angle(), 5f * Engine.DeltaTime) : vector;
                        vector = vector.CorrectJoystickPrecision();
                        player.DashDir = vector;
                        player.Speed = vector * 240f;
                    }
                }
            }
            return orig(player);
        }

        #endregion

    }
}
