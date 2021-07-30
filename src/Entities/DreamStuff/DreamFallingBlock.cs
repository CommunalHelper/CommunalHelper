﻿using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/DreamFallingBlock")]
    public class DreamFallingBlock : CustomDreamBlock {
        private class DreamFallingBlockChainRenderer : Entity {
            private DreamFallingBlock block;

            public DreamFallingBlockChainRenderer(DreamFallingBlock dreamFallingBlock) {
                block = dreamFallingBlock;
                Depth = Depths.FGTerrain + 1;
            }

            public override void Render() {
                if (block.chained) {
                    if (block.centeredChain) {
                        Chain.DrawChainLine(new Vector2(block.X + block.Width / 2f, block.startY), new Vector2(block.X + block.Width / 2f, block.Y), block.chainOutline);
                    } else {
                        Chain.DrawChainLine(new Vector2(block.X + 3, block.startY), new Vector2(block.X + 3, block.Y), block.chainOutline);
                        Chain.DrawChainLine(new Vector2(block.X + block.Width - 4, block.startY), new Vector2(block.X + block.Width - 4, block.Y), block.chainOutline);
                    }
                }
            }
        }

        public bool Triggered;
        public float FallDelay;

        private bool noCollide;

        public bool HasStartedFalling { get; private set; }
        private bool hasLanded;

        private DreamFallingBlockChainRenderer chainRenderer;
        private bool chained;
        private bool held;

        private float chainStopY, startY;
        private bool centeredChain;
        private bool chainOutline;

        private bool indicator, indicatorAtStart;
        private float pathLerp;

        private SoundSource rattle;

        public DreamFallingBlock(EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height, data.Bool("featherMode"), data.Bool("oneUse"), GetRefillCount(data), data.Bool("below")) {
            noCollide = data.Bool("noCollide", false);

            chained = data.Bool("chained");

            Add(new Coroutine(Sequence()));
            if (chained) {
                startY = Y;
                chainStopY = startY + data.Int("fallDistance", 64);
                centeredChain = data.Bool("centeredChain") || Width <= 8;
                chainOutline = data.Bool("chainOutline", true);
                indicator = data.Bool("indicator");
                indicatorAtStart = data.Bool("indicatorAtStart");
                pathLerp = Util.ToInt(indicatorAtStart);
                
                Add(rattle = new SoundSource() {
                    Position = Vector2.UnitX * Width / 2f
                });
            }
        }

        public override void OnStaticMoverTrigger(StaticMover sm) => Triggered = true;

        private bool PlayerWaitCheck() {
            if (Triggered) {
                return true;
            }
            if (HasPlayerRider()) {
                return true;
            }
            return CollideCheck<Player>(Position - Vector2.UnitX) || CollideCheck<Player>(Position + Vector2.UnitX);
        }

        private IEnumerator Sequence() {
            while (!Triggered && !HasPlayerRider()) {
                yield return null;
            }
            Triggered = true;

            while (FallDelay > 0f) {
                FallDelay -= Engine.DeltaTime;
                yield return null;
            }

            HasStartedFalling = true;
            Vector2 rattleSoundPos = new Vector2(Center.X, startY);
            while (true) {
                ShakeSfx();
                StartShaking();
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                yield return 0.2f;

                float timer = 0.4f;
                while (timer > 0f && PlayerWaitCheck()) {
                    yield return null;
                    timer -= Engine.DeltaTime;
                }

                StopShaking();
                for (int i = 2; i < Width; i += 4) {
                    if (Scene.CollideCheck<Solid>(TopLeft + new Vector2(i, -2f))) {
                        SceneAs<Level>().Particles.Emit(FallingBlock.P_FallDustA, 2, new Vector2(X + i, Y), Vector2.One * 4f, (float) Math.PI / 2f);
                    }
                    SceneAs<Level>().Particles.Emit(FallingBlock.P_FallDustB, 2, new Vector2(X + i, Y), Vector2.One * 4f);
                }

                if (chained)
                    rattle.Play(CustomSFX.game_chainedFallingBlock_chain_rattle);

                float speed = 0f;
                float maxSpeed = 160f;
                hasLanded = false;
                while (true) {
                    Level level = SceneAs<Level>();
                    speed = Calc.Approach(speed, maxSpeed, 500f * Engine.DeltaTime);
                    MoveV(speed * Engine.DeltaTime);
                    if (hasLanded) {
                        held = chained && Y == chainStopY;
                        break;
                    } else if (Y > chainStopY && chained) {
                        held = true;
                        MoveToY(chainStopY, LiftSpeed.Y);
                        break;
                    }

                    if (!chained && Top > level.Bounds.Bottom + 16 || (Top > level.Bounds.Bottom - 1 && CollideCheck<Solid>(Position + new Vector2(0f, 1f)))) {
                        Collidable = (Visible = false);
                        yield return 0.2f;

                        if (level.Session.MapData.CanTransitionTo(level, new Vector2(Center.X, Bottom + 12f))) {
                            yield return 0.2f;

                            SceneAs<Level>().Shake();
                            Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                        }
                        RemoveSelf();
                        DestroyStaticMovers();
                        yield break;

                    }
                    yield return null;
                }

                ImpactSfx();
                Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                SceneAs<Level>().DirectionalShake(Vector2.UnitY, 0.3f);
                StartShaking();
                LandParticles();

                if (chained) {
                    rattle.Stop();
                    if (held) {
                        Audio.Play(CustomSFX.game_chainedFallingBlock_chain_tighten_block, TopCenter);
                        Audio.Play(CustomSFX.game_chainedFallingBlock_chain_tighten_ceiling, rattleSoundPos);
                    }
                }
                yield return 0.2f;

                StopShaking();
                if (CollideCheck<SolidTiles>(Position + new Vector2(0f, 1f))) {
                    break;
                }

                while (held) {
                    yield return null;
                }

                while (CollideCheck<Platform>(Position + new Vector2(0f, 1f))) {
                    yield return 0.1f;
                }

            }
            Safe = true;
        }

        // Essentially just a copied/stripped version of MoveVExactCollideSolids
        public override void MoveVExact(int move) {
            float y = Y;
            int dir = Math.Sign(move);
            int actualMove = 0;
            Platform platform = null;
            while (move != 0) {
                if (!noCollide) {
                    foreach (Entity entity in Scene.Tracker.GetEntities<DashBlock>()) {
                        if (CollideCheck(entity, Position + Vector2.UnitY * dir)) {
                            ((DashBlock) entity).Break(Center, Vector2.UnitY * dir, true, true);
                            SceneAs<Level>().Shake(0.2f);
                            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                        }
                    }
                }
                platform = noCollide ? CollideFirst<DreamBlock>(Position + Vector2.UnitY * dir) : CollideFirst<Solid>(Position + Vector2.UnitY * dir);
                if (platform != null) {
                    break;
                }
                if (!noCollide && move > 0) {
                    platform = CollideFirstOutside<JumpThru>(Position + Vector2.UnitY * dir);
                    if (platform != null) {
                        break;
                    }
                }
                actualMove += dir;
                move -= dir;
                Y += dir;
            }
            Y = y;
            base.MoveVExact(actualMove);
            hasLanded = platform != null;
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            if (chained)
                scene.Add(chainRenderer = new DreamFallingBlockChainRenderer(this));
        }

        public override void Removed(Scene scene) {
            base.Removed(scene);
            if (chained)
                chainRenderer.RemoveSelf(); 
        }

        public override void Update() {
            base.Update();

            if (chained && Triggered && indicator && !indicatorAtStart)
                pathLerp = Calc.Approach(pathLerp, 1f, Engine.DeltaTime * 2f);
        }

        public override void Render() {
            if (chained && (Triggered || indicatorAtStart) && indicator && !held) {
                float toY = startY + (chainStopY + Height - startY) * Ease.ExpoOut(pathLerp);
                Draw.Rect(X, Y, Width, toY - Y, Color.Black * 0.75f);
            }

            Position += Shake;
            base.Render();
            Position -= Shake;
        }

        private void LandParticles() {
            for (int i = 2; i <= Width; i += 4) {
                if (Scene.CollideCheck<Solid>(BottomLeft + new Vector2(i, 3f))) {
                    SceneAs<Level>().ParticlesFG.Emit(FallingBlock.P_FallDustA, 1, new Vector2(X + i, Bottom), Vector2.One * 4f, -(float) Math.PI / 2f);
                    float direction = (!(i < Width / 2f)) ? 0f : ((float) Math.PI);
                    SceneAs<Level>().ParticlesFG.Emit(FallingBlock.P_LandDust, 1, new Vector2(X + i, Bottom), Vector2.One * 4f, direction);
                }
            }
        }

        private void ShakeSfx() =>
            Audio.Play(SFX.game_gen_fallblock_shake, Center);

        private void ImpactSfx() =>
            Audio.Play(SFX.game_gen_fallblock_impact, BottomCenter);
    }
}
