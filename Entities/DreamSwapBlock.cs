using Celeste.Mod.Entities;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.CommunalHelper {
    [CustomEntity("CommunalHelper/DreamSwapBlock")]
    [TrackedAs(typeof(DreamBlock))]
    class DreamSwapBlock : DreamBlock {
        private class PathRenderer : Entity {
            private DreamSwapBlock block;

            private float timer = 0f;

            public PathRenderer(DreamSwapBlock block)
                : base(block.Position) {
                this.block = block;
                base.Depth = 8999;
                timer = Calc.Random.NextFloat();
            }

            public override void Update() {
                base.Update();
                timer += Engine.DeltaTime * 4f;
            }

            public override void Render() {
                float scale = 0.5f * (0.5f + ((float)Math.Sin(timer) + 1f) * 0.25f);
                block.DrawBlockStyle(new Vector2(block.moveRect.X, block.moveRect.Y), block.moveRect.Width, block.moveRect.Height, block.nineSliceTarget, null, Color.White * scale);
            }
        }

        private const float ReturnTime = 0.8f;

        public Vector2 Direction;
        public bool Swapping;

        private Vector2 start;
        private Vector2 end;
        private float lerp;
        private int target;
        private Rectangle moveRect;

        private float speed;
        private float maxForwardSpeed;
        private float maxBackwardSpeed;
        private float returnTimer;

        private MTexture[,] nineSliceTarget;

        private PathRenderer path;

        private EventInstance moveSfx;
        private EventInstance returnSfx;

        private DisplacementRenderer.Burst burst;
        private float particlesRemainder;

        private static ParticleType[] dreamParticles;
        private int particleIndex = 0;

        static DreamSwapBlock() {
            dreamParticles = new ParticleType[4];
            ParticleType particle = new ParticleType(SwapBlock.P_Move);
            particle.ColorMode = ParticleType.ColorModes.Choose;
            particle.FadeMode = ParticleType.FadeModes.Late;
            particle.LifeMin = 0.6f; particle.LifeMin = 1f;

            particle.Color = Calc.HexToColor("FFEF11");
            particle.Color2 = Calc.HexToColor("FF00D0");
            dreamParticles[0] = particle;

            particle = new ParticleType(particle);
            particle.Color = Calc.HexToColor("08a310");
            particle.Color2 = Calc.HexToColor("5fcde4");
            dreamParticles[1] = particle;

            particle = new ParticleType(particle);
            particle.Color = Calc.HexToColor("7fb25e");
            particle.Color2 = Calc.HexToColor("E0564C");
            dreamParticles[2] = particle;

            particle = new ParticleType(particle);
            particle.Color = Calc.HexToColor("5b6ee1");
            particle.Color2 = Calc.HexToColor("CC3B3B");
            dreamParticles[3] = particle;
        }

        public DreamSwapBlock(Vector2 position, float width, float height, Vector2 node)
            : base(position, width, height, null, false, false) {
            start = Position;
            end = node;
            maxForwardSpeed = 360f / Vector2.Distance(start, end);
            maxBackwardSpeed = maxForwardSpeed * 0.4f;
            Direction.X = Math.Sign(end.X - start.X);
            Direction.Y = Math.Sign(end.Y - start.Y);
            Add(new DashListener {
                OnDash = OnDash
            });
            int num = (int)MathHelper.Min(base.X, node.X);
            int num2 = (int)MathHelper.Min(base.Y, node.Y);
            int num3 = (int)MathHelper.Max(base.X + base.Width, node.X + base.Width);
            int num4 = (int)MathHelper.Max(base.Y + base.Height, node.Y + base.Height);
            moveRect = new Rectangle(num, num2, num3 - num, num4 - num2);
            MTexture targetTexture = GFX.Game["objects/swapblock/target"];
            nineSliceTarget = new MTexture[3, 3];
            for (int i = 0; i < 3; i++) {
                for (int j = 0; j < 3; j++) {
                    nineSliceTarget[i, j] = targetTexture.GetSubtexture(new Rectangle(i * 8, j * 8, 8, 8));
                }
            }
            Add(new LightOcclude(0.2f));
        }

        public DreamSwapBlock(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Width, data.Height, data.Nodes[0] + offset) {
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            scene.Add(path = new PathRenderer(this));
        }

        public override void Removed(Scene scene) {
            base.Removed(scene);
            Audio.Stop(moveSfx);
            Audio.Stop(returnSfx);
        }

        public override void SceneEnd(Scene scene) {
            base.SceneEnd(scene);
            Audio.Stop(moveSfx);
            Audio.Stop(returnSfx);
        }

        private void OnDash(Vector2 direction) {
            Swapping = (lerp < 1f);
            target = 1;
            returnTimer = 0.8f;
            burst = (base.Scene as Level).Displacement.AddBurst(base.Center, 0.2f, 0f, 16f);
            if (lerp >= 0.2f) {
                speed = maxForwardSpeed;
            } else {
                speed = MathHelper.Lerp(maxForwardSpeed * 0.333f, maxForwardSpeed, lerp / 0.2f);
            }
            Audio.Stop(returnSfx);
            Audio.Stop(moveSfx);
            if (!Swapping) {
                Audio.Play("event:/CommunalHelperEvents/game/dreamSwapBlock/dream_swap_block_move_end", base.Center);
                Audio.Stop(moveSfx);
            } else {
                moveSfx = Audio.Play("event:/CommunalHelperEvents/game/dreamSwapBlock/dream_swap_block_move", base.Center);
            }
        }

        public override void Update() {
            base.Update();
            if (returnTimer > 0f) {
                returnTimer -= Engine.DeltaTime;
                if (returnTimer <= 0f) {
                    target = 0;
                    speed = 0f;
                    returnSfx = Audio.Play("event:/CommunalHelperEvents/game/dreamSwapBlock/dream_swap_block_return", base.Center);
                }
            }
            if (burst != null) {
                burst.Position = base.Center;
            }
            if (target == 1) {
                speed = Calc.Approach(speed, maxForwardSpeed, maxForwardSpeed / 0.2f * Engine.DeltaTime);
            } else {
                speed = Calc.Approach(speed, maxBackwardSpeed, maxBackwardSpeed / 1.5f * Engine.DeltaTime);
            }
            float num = lerp;
            lerp = Calc.Approach(lerp, target, speed * Engine.DeltaTime);
            if (lerp == 1) Audio.Stop(moveSfx);
            if (lerp != num) {
                Vector2 liftSpeed = (end - start) * speed;
                Vector2 position = Position;
                if (target == 1) {
                    liftSpeed = (end - start) * maxForwardSpeed;
                }
                if (lerp < num) {
                    liftSpeed *= -1f;
                }
                if (target == 1 && base.Scene.OnInterval(0.02f)) {
                    MoveParticles(end - start);
                }
                MoveTo(Vector2.Lerp(start, end, lerp), liftSpeed);
                if (position != Position) {
                    Audio.Position(moveSfx, base.Center);
                    Audio.Position(returnSfx, base.Center);
                    if (Position == start && target == 0) {
                        Audio.SetParameter(returnSfx, "end", 1f);
                        Audio.Play("event:/CommunalHelperEvents/game/dreamSwapBlock/dream_swap_block_move_end", base.Center);
                    } else if (Position == end && target == 1) {
                        Audio.Play("event:/CommunalHelperEvents/game/dreamSwapBlock/dream_swap_block_return_end", base.Center);
                        Audio.Stop(moveSfx);
                    }
                }
            }
            if (Swapping && lerp >= 1f) {
                Swapping = false;
            }
            StopPlayerRunIntoAnimation = (lerp <= 0f || lerp >= 1f);
        }

        private void MoveParticles(Vector2 normal) {
            Vector2 position;
            Vector2 positionRange;
            float direction;
            float num;
            if (normal.X > 0f) {
                position = base.CenterLeft;
                positionRange = Vector2.UnitY * (base.Height - 6f);
                direction = (float)Math.PI;
                num = Math.Max(2f, base.Height / 14f);
            } else if (normal.X < 0f) {
                position = base.CenterRight;
                positionRange = Vector2.UnitY * (base.Height - 6f);
                direction = 0f;
                num = Math.Max(2f, base.Height / 14f);
            } else if (normal.Y > 0f) {
                position = base.TopCenter;
                positionRange = Vector2.UnitX * (base.Width - 6f);
                direction = -(float)Math.PI / 2f;
                num = Math.Max(2f, base.Width / 14f);
            } else {
                position = base.BottomCenter;
                positionRange = Vector2.UnitX * (base.Width - 6f);
                direction = (float)Math.PI / 2f;
                num = Math.Max(2f, base.Width / 14f);
            }
            particlesRemainder += num;
            int num2 = (int)particlesRemainder;
            particlesRemainder -= num2;
            positionRange *= 0.5f;
            // TODO make dream particles
            SceneAs<Level>().Particles.Emit(dreamParticles[particleIndex], num2, position, positionRange, direction);
            ++particleIndex;
            particleIndex %= 4;
        }

        private void DrawBlockStyle(Vector2 pos, float width, float height, MTexture[,] ninSlice, Sprite middle, Color color) {
            int num = (int)(width / 8f);
            int num2 = (int)(height / 8f);
            ninSlice[0, 0].Draw(pos + new Vector2(0f, 0f), Vector2.Zero, color);
            ninSlice[2, 0].Draw(pos + new Vector2(width - 8f, 0f), Vector2.Zero, color);
            ninSlice[0, 2].Draw(pos + new Vector2(0f, height - 8f), Vector2.Zero, color);
            ninSlice[2, 2].Draw(pos + new Vector2(width - 8f, height - 8f), Vector2.Zero, color);
            for (int i = 1; i < num - 1; i++) {
                ninSlice[1, 0].Draw(pos + new Vector2(i * 8, 0f), Vector2.Zero, color);
                ninSlice[1, 2].Draw(pos + new Vector2(i * 8, height - 8f), Vector2.Zero, color);
            }
            for (int j = 1; j < num2 - 1; j++) {
                ninSlice[0, 1].Draw(pos + new Vector2(0f, j * 8), Vector2.Zero, color);
                ninSlice[2, 1].Draw(pos + new Vector2(width - 8f, j * 8), Vector2.Zero, color);
            }
            for (int k = 1; k < num - 1; k++) {
                for (int l = 1; l < num2 - 1; l++) {
                    ninSlice[1, 1].Draw(pos + new Vector2(k, l) * 8f, Vector2.Zero, color);
                }
            }
            if (middle != null) {
                middle.Color = color;
                middle.RenderPosition = pos + new Vector2(width / 2f, height / 2f);
                middle.Render();
            }
        }
    }
}
