using Celeste.Mod.Entities;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/CassetteSwapBlock")]
    public class CassetteSwapBlock : CustomCassetteBlock {
        private class PathRenderer : Entity {
            private CassetteSwapBlock block;
            private Color pathColor;
            private Color pathColorPressed;

            public PathRenderer(CassetteSwapBlock block)
                : base(block.Position) {
                this.block = block;
                Depth = Depths.BGDecals;
                pathColor = block.color.Mult(Calc.HexToColor("e0e7ea"));
                pathColorPressed = block.pressedColor.Mult(Calc.HexToColor("e0e7ea"));
            }

            public override void Update() {
                base.Update();
                Depth = block.Collidable ? Depths.BGDecals : 9010;
            }

            public override void Render() {
                Vector2 position = new Vector2(block.moveRect.X, block.moveRect.Y) + block.blockOffset;
                for (int i = 1; i <= block.blockHeight; ++i)
                    DrawTarget(position + Vector2.UnitY * i, pathColorPressed);
                DrawTarget(position, block.Collidable ? pathColor : pathColorPressed);

            }

            private void DrawTarget(Vector2 position, Color color) {
                block.DrawBlockStyle(position, block.moveRect.Width, block.moveRect.Height, block.nineSliceTarget, null, color);
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

        private EventInstance moveSfx;
        private EventInstance returnSfx;
        private float particlesRemainder;
        private ParticleType moveParticle;
        private ParticleType moveParticlePressed;

        private bool noReturn;

        public CassetteSwapBlock(Vector2 position, EntityID id, int width, int height, Vector2 node, int index, float tempo, bool noReturn)
            : base(position, id, width, height, index, tempo) {
            start = Position;
            end = node;
            this.noReturn = noReturn;
            maxForwardSpeed = 360f / Vector2.Distance(start, end);
            maxBackwardSpeed = maxForwardSpeed * 0.4f;
            Direction.X = Math.Sign(end.X - start.X);
            Direction.Y = Math.Sign(end.Y - start.Y);

            Add(new DashListener { OnDash = OnDash });

            int minX = (int) MathHelper.Min(X, node.X);
            int minY = (int) MathHelper.Min(Y, node.Y);
            int maxX = (int) MathHelper.Max(X + Width, node.X + Width);
            int maxY = (int) MathHelper.Max(Y + Height, node.Y + Height);
            moveRect = new Rectangle(minX, minY, maxX - minX, maxY - minY);

            MTexture mTexture3 = GFX.Game["objects/swapblock/target"];
            nineSliceTarget = new MTexture[3, 3];
            for (int i = 0; i < 3; i++) {
                for (int j = 0; j < 3; j++) {
                    nineSliceTarget[i, j] = mTexture3.GetSubtexture(new Rectangle(i * 8, j * 8, 8, 8));
                }
            }

            moveParticle = new ParticleType(SwapBlock.P_Move) {
                Color = color,
                Color2 = color
            };
            moveParticlePressed = new ParticleType(SwapBlock.P_Move) {
                Color = pressedColor,
                Color2 = pressedColor
            };
        }

        public CassetteSwapBlock(EntityData data, Vector2 offset, EntityID id)
            : this(data.Position + offset, id, data.Width, data.Height, data.Nodes[0] + offset, data.Int("index"), data.Float("tempo", 1f), data.Bool("noReturn", false)) {
        }

        public override void Awake(Scene scene) {
            Image cross = new Image(GFX.Game["objects/CommunalHelper/cassetteMoveBlock/x"]);
            Image crossPressed = new Image(GFX.Game["objects/CommunalHelper/cassetteMoveBlock/xPressed"]);

            base.Awake(scene);
            if (noReturn) {
                AddCenterSymbol(cross, crossPressed);
            }
            scene.Add(new PathRenderer(this));
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
            if (noReturn) {
                Swapping = true;
                target = 1 - target;
                float relativeLerp = target == 1 ? lerp : 1 - lerp;
                if (relativeLerp >= 0.2f) {
                    speed = maxForwardSpeed;
                } else {
                    speed = MathHelper.Lerp(maxForwardSpeed * 0.333f, maxForwardSpeed, relativeLerp / 0.2f);
                }

                Audio.Stop(moveSfx);
                moveSfx = Audio.Play(SFX.game_05_swapblock_move, Center);
            } else {
                Swapping = (lerp < 1f);
                target = 1;
                returnTimer = ReturnTime;
                if (lerp >= 0.2f) {
                    speed = maxForwardSpeed;
                } else {
                    speed = MathHelper.Lerp(maxForwardSpeed * 0.333f, maxForwardSpeed, lerp / 0.2f);
                }

                Audio.Stop(returnSfx);
                Audio.Stop(moveSfx);
                if (!Swapping) {
                    Audio.Play(SFX.game_05_swapblock_move_end, Center);
                } else {
                    moveSfx = Audio.Play(SFX.game_05_swapblock_move, Center);
                }
            }
        }

        public override void Update() {
            base.Update();
            if (noReturn) {
                #region noReturn

                speed = Calc.Approach(speed, maxForwardSpeed, maxForwardSpeed / 0.2f * Engine.DeltaTime);
                float initialLerp = lerp;
                lerp = Calc.Approach(lerp, target, speed * Engine.DeltaTime);
                if (lerp is 0 or 1)
                    Audio.Stop(moveSfx);
                if (lerp != initialLerp) {
                    Vector2 liftSpeed = (end - start) * speed;
                    Vector2 position = Position;
                    if (target == 1) {
                        liftSpeed = (end - start) * maxForwardSpeed;
                    }
                    if (lerp < initialLerp) {
                        liftSpeed *= -1f;
                    }
                    if (Scene.OnInterval(0.02f)) {
                        // Allows move particles in both directions
                        MoveParticles((end - start) * (target - 0.5f) * 2);
                    }
                    Vector2 to = Vector2.Lerp(start, end, lerp);
                    Vector2 diff = to - (ExactPosition - blockOffset);
                    MoveH(diff.X, liftSpeed.X);
                    MoveV(diff.Y, liftSpeed.Y);

                    if (position != Position) {
                        Audio.Position(moveSfx, Center);
                        if (Position - blockOffset == start || Position == end) {
                            Audio.Stop(moveSfx);
                            Audio.Play(SFX.game_05_swapblock_move_end, Center);
                        }
                    }
                }
                if (Swapping && lerp >= 1f) {
                    Swapping = false;
                }
                StopPlayerRunIntoAnimation = lerp is <= 0f or >= 1f;

                #endregion
            } else {
                #region return

                if (returnTimer > 0f) {
                    returnTimer -= Engine.DeltaTime;
                    if (returnTimer <= 0f) {
                        target = 0;
                        speed = 0f;
                        returnSfx = Audio.Play(SFX.game_05_swapblock_return, Center);
                    }
                }
                if (target == 1) {
                    speed = Calc.Approach(speed, maxForwardSpeed, maxForwardSpeed / 0.2f * Engine.DeltaTime);
                } else {
                    speed = Calc.Approach(speed, maxBackwardSpeed, maxBackwardSpeed / 1.5f * Engine.DeltaTime);
                }
                float initialLerp = lerp;
                lerp = Calc.Approach(lerp, target, speed * Engine.DeltaTime);
                if (lerp == 1)
                    Audio.Stop(moveSfx);
                if (lerp != initialLerp) {
                    Vector2 liftSpeed = (end - start) * speed;
                    Vector2 position = Position;
                    if (target == 1) {
                        liftSpeed = (end - start) * maxForwardSpeed;
                    }
                    if (lerp < initialLerp) {
                        liftSpeed *= -1f;
                    }
                    if (target == 1 && Scene.OnInterval(0.02f)) {
                        MoveParticles(end - start);
                    }
                    Vector2 to = Vector2.Lerp(start, end, lerp);
                    Vector2 diff = to - (ExactPosition - blockOffset);
                    MoveH(diff.X, liftSpeed.X);
                    MoveV(diff.Y, liftSpeed.Y);

                    if (position != Position) {
                        Audio.Position(moveSfx, Center);
                        Audio.Position(returnSfx, Center);
                        if (Position - blockOffset == start && target == 0) {
                            Audio.SetParameter(returnSfx, "end", 1f);
                            Audio.Play(SFX.game_05_swapblock_return_end, Center);
                        } else if (Position - blockOffset == end && target == 1) {
                            Audio.Play(SFX.game_05_swapblock_move_end, Center);
                            Audio.Stop(moveSfx);
                        }
                    }
                }
                if (Swapping && lerp >= 1f) {
                    Swapping = false;
                }
                StopPlayerRunIntoAnimation = lerp is <= 0f or >= 1f;

                #endregion
            }
        }

        private void MoveParticles(Vector2 normal) {
            Vector2 position;
            Vector2 positionRange;
            float direction;
            float num;
            if (normal.X > 0f) {
                position = CenterLeft;
                positionRange = Vector2.UnitY * (Height - 6f);
                direction = (float) Math.PI;
                num = Math.Max(2f, Height / 14f);
            } else if (normal.X < 0f) {
                position = CenterRight;
                positionRange = Vector2.UnitY * (Height - 6f);
                direction = 0f;
                num = Math.Max(2f, Height / 14f);
            } else if (normal.Y > 0f) {
                position = TopCenter;
                positionRange = Vector2.UnitX * (Width - 6f);
                direction = -(float) Math.PI / 2f;
                num = Math.Max(2f, Width / 14f);
            } else {
                position = BottomCenter;
                positionRange = Vector2.UnitX * (Width - 6f);
                direction = (float) Math.PI / 2f;
                num = Math.Max(2f, Width / 14f);
            }
            particlesRemainder += num;
            int num2 = (int) particlesRemainder;
            particlesRemainder -= num2;
            positionRange *= 0.5f;
            SceneAs<Level>().Particles.Emit(Collidable ? moveParticle : moveParticlePressed, num2, position, positionRange, direction);
        }

        private void DrawBlockStyle(Vector2 pos, float width, float height, MTexture[,] ninSlice, Sprite middle, Color color) {
            int tilesX = (int) (width / 8f);
            int tilesY = (int) (height / 8f);
            ninSlice[0, 0].Draw(pos + new Vector2(0f, 0f), Vector2.Zero, color);
            ninSlice[2, 0].Draw(pos + new Vector2(width - 8f, 0f), Vector2.Zero, color);
            ninSlice[0, 2].Draw(pos + new Vector2(0f, height - 8f), Vector2.Zero, color);
            ninSlice[2, 2].Draw(pos + new Vector2(width - 8f, height - 8f), Vector2.Zero, color);
            for (int i = 1; i < tilesX - 1; i++) {
                ninSlice[1, 0].Draw(pos + new Vector2(i * 8, 0f), Vector2.Zero, color);
                ninSlice[1, 2].Draw(pos + new Vector2(i * 8, height - 8f), Vector2.Zero, color);
            }
            for (int j = 1; j < tilesY - 1; j++) {
                ninSlice[0, 1].Draw(pos + new Vector2(0f, j * 8), Vector2.Zero, color);
                ninSlice[2, 1].Draw(pos + new Vector2(width - 8f, j * 8), Vector2.Zero, color);
            }
            for (int k = 1; k < tilesX - 1; k++) {
                for (int l = 1; l < tilesY - 1; l++) {
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
