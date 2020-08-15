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

        private static MethodInfo m_DreamBlock_PutInside = typeof(DreamBlock).GetMethod("PutInside", BindingFlags.NonPublic | BindingFlags.Instance);

        private MTexture[] featherTextures;
        private DreamParticle[] particles;

        public bool FeatherMode;
        protected bool shattering = false;
        public float ColorLerp = 0.0f;

        private bool shakeToggle = false;
        private ParticleType shakeParticle;
        private float[] particleRemainders = new float[4];

        protected DynData<DreamBlock> baseData;

        public CustomDreamBlock(Vector2 position, int width, int height, bool featherMode, bool oneUse)
            : base(position, width, height, null, false, oneUse) {
            baseData = new DynData<DreamBlock>(this);

            FeatherMode = featherMode;
            //if (altLineColor) { Dropped in favour of symbol
            //    activeLineColor = Calc.HexToColor("FF66D9"); 
            //}
            shakeParticle = new ParticleType(SwitchGate.P_Behind) {
                Color = baseData.Get<Color>("activeLineColor"),
                ColorMode = ParticleType.ColorModes.Static,
                Acceleration = Vector2.Zero,
                DirectionRange = (float) Math.PI / 2
            };

            featherTextures = new MTexture[] {
                GFX.Game["particles/CommunalHelper/featherBig"],
                GFX.Game["particles/CommunalHelper/featherMedium"],
                GFX.Game["particles/CommunalHelper/featherSmall"]
            };
        }

        public override void Removed(Scene scene) {
            base.Removed(scene);
            Glitch.Value = 0f;
        }

        public void SetupCustomParticles(float canvasWidth, float canvasHeight) {
            float countFactor = FeatherMode ? 0.5f : 0.7f;
            particles = new DreamParticle[(int) (canvasWidth / 8f * (canvasHeight / 8f) * 0.7f * countFactor)];
            for (int i = 0; i < particles.Length; i++) {
                particles[i].Position = new Vector2(Calc.Random.NextFloat(canvasWidth), Calc.Random.NextFloat(canvasHeight));
                particles[i].Layer = Calc.Random.Choose(0, 1, 1, 2, 2, 2);
                particles[i].TimeOffset = Calc.Random.NextFloat();

                if (baseData.Get<bool>("playerHasDreamDash")) {
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
            float num;
            float num2;
            for (int i = 0; i < 4; ++i) {
                switch (i) {
                    case 0:
                        position = CenterLeft + Vector2.UnitX;
                        positionRange = Vector2.UnitY * (Height - 4f);
                        num = (float) Math.PI;
                        num2 = Height / 32f;
                        break;
                    case 1:
                        position = CenterRight;
                        positionRange = Vector2.UnitY * (Height - 4f);
                        num = 0f;
                        num2 = Height / 32f;
                        break;
                    case 2:
                        position = TopCenter + Vector2.UnitY;
                        positionRange = Vector2.UnitX * (Width - 4f);
                        num = -(float) Math.PI / 2f;
                        num2 = Width / 32f;
                        break;
                    default:
                        position = BottomCenter;
                        positionRange = Vector2.UnitX * (Width - 4f);
                        num = (float) Math.PI / 2f;
                        num2 = Width / 32f;
                        break;
                }

                num2 *= 0.25f;
                particleRemainders[i] += num2;
                int num3 = (int) particleRemainders[i];
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

        private void RenderParticles() {
            Vector2 cameraPositon = SceneAs<Level>().Camera.Position;
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
                    MTexture[] particleTextures = baseData.Get<MTexture[]>("particleTextures");
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
        }

        protected bool CheckParticleCollide(Vector2 position) {
            float offset = 2f;
            return position.X >= X + offset && position.Y >= Y + offset && position.X < Right - offset && position.Y < Bottom - offset;
        }

        protected virtual bool ShatterCheck() {
            return !shattering;
        }

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
            Vector2 camera = SceneAs<Level>().Camera.Position;

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
            Collidable = (Visible = false);
            DisableStaticMovers();
        }

        #region Hooks

        internal static void Load() {
            On.Celeste.DreamBlock.Update += DreamBlock_Update;
            IL.Celeste.DreamBlock.Render += DreamBlock_Render;
            On.Celeste.DreamBlock.Setup += DreamBlock_Setup;
            On.Celeste.DreamBlock.OnPlayerExit += DreamBlock_OnPlayerExit;

            On.Celeste.Player.DreamDashBegin += Player_DreamDashBegin;
            On.Celeste.Player.DreamDashUpdate += Player_DreamDashUpdate;
        }

        internal static void Unload() {
            On.Celeste.DreamBlock.Update -= DreamBlock_Update;
            IL.Celeste.DreamBlock.Render -= DreamBlock_Render;
            On.Celeste.DreamBlock.Setup -= DreamBlock_Setup;
            On.Celeste.DreamBlock.OnPlayerExit -= DreamBlock_OnPlayerExit;

            On.Celeste.Player.DreamDashBegin -= Player_DreamDashBegin;
            On.Celeste.Player.DreamDashUpdate -= Player_DreamDashUpdate;
        }

        private static void DreamBlock_Update(On.Celeste.DreamBlock.orig_Update orig, DreamBlock self) {
            orig(self);
            if (self is CustomDreamBlock block) {
                if (block.FeatherMode) {
                    block.UpdateParticles();
                }

                if (block.baseData.Get<bool>("oneUse") && block.Scene.OnInterval(0.03f)) {
                    Vector2 shake = block.baseData.Get<Vector2>("shake");
                    if (block.shakeToggle) {
                        shake.X = Calc.Random.Next(-1, 2);
                    } else {
                        shake.Y = Calc.Random.Next(-1, 2);
                    }
                    block.baseData["shake"] = shake;
                    block.shakeToggle = !block.shakeToggle;
                    if (!block.shattering)
                        block.ShakeParticles();
                }
            }
        }

        private static void DreamBlock_Render(ILContext il) {
            TypeInfo t_CustomDreamBlock = typeof(CustomDreamBlock).GetTypeInfo();

            ILCursor cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After, instr => instr.MatchCall("Monocle.Draw", "Rect"));

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Isinst, t_CustomDreamBlock);
            cursor.Emit(OpCodes.Brfalse_S, cursor.Next);

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Castclass, t_CustomDreamBlock);
            cursor.EmitDelegate<Action<CustomDreamBlock>>(block => block.RenderParticles());
            cursor.Emit(OpCodes.Br, il.Instrs.First(instr => instr.Next.MatchLdfld<DreamBlock>("whiteFill")));

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
                if (customDreamBlock.baseData.Get<bool>("oneUse") && customDreamBlock.Collidable) {
                    customDreamBlock.BeginShatter();
                }
            }
        }

        private static void Player_DreamDashBegin(On.Celeste.Player.orig_DreamDashBegin orig, Player player) {
            orig(player);
            var playerData = player.GetData();
            DreamBlock dreamBlock = playerData.Get<DreamBlock>("dreamBlock");
            if (dreamBlock is CustomDreamBlock && (dreamBlock as CustomDreamBlock).FeatherMode) {
                SoundSource dreamSfxLoop = playerData.Get<SoundSource>("dreamSfxLoop");
                player.Stop(dreamSfxLoop);
                player.Loop(dreamSfxLoop, "event:/CommunalHelperEvents/game/connectedDreamBlock/dreamblock_fly_travel");

                // Ensures the player always properly enters a dream block even when it's moving fast
                if (dreamBlock is DreamZipMover || dreamBlock is DreamSwapBlock) {
                    player.Position.X += Math.Sign(player.DashDir.X);
                    player.Position.Y += Math.Sign(player.DashDir.Y);
                }
            }

        }

        private static int Player_DreamDashUpdate(On.Celeste.Player.orig_DreamDashUpdate orig, Player player) {
            var playerData = player.GetData();
            DreamBlock block = playerData.Get<DreamBlock>("dreamBlock");
            if (block is CustomDreamBlock dreamBlock && dreamBlock.FeatherMode) {
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

        #endregion

    }
}
