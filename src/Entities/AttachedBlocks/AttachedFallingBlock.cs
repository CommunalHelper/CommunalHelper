using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;

namespace Celeste.Mod.CommunalHelper.Entities.AttachedBlocks {
    [CustomEntity("CommunalHelper/AttachedFallingBlock")]
    class AttachedFallingBlock : AttachedBlock {

        private bool triggered;
        private bool climbFall;

        public AttachedFallingBlock(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Width, data.Height, data.Char("tiletype", '3'), data.Bool("smoothDetach"), data.Bool("climbFall", true)) { }

        public AttachedFallingBlock(Vector2 position, int width, int height, char tileType, bool smoothDetach, bool climbFall) 
            : base(position, width, height, tileType, smoothDetach, safe: false) {
            this.climbFall = climbFall;
            Add(new Coroutine(Sequence()));
        }

        public static FallingBlock CreateFinalBossBlock(EntityData data, Vector2 offset) {
            return new FallingBlock(data.Position + offset, 'g', data.Width, data.Height, finalBoss: true, behind: false, climbFall: false);
        }

        public override void OnStaticMoverTrigger(StaticMover sm) {
            triggered = true;
        }

        private bool PlayerFallCheck() {
            if (climbFall) {
                return HasPlayerRider();
            }
            return HasPlayerOnTop();
        }

        private bool PlayerWaitCheck() {
            if (triggered)
                return true;

            if (PlayerFallCheck())
                return true;

            if (climbFall) {
                if (!CollideCheck<Player>(Position - Vector2.UnitX))
                    return CollideCheck<Player>(Position + Vector2.UnitX);
                return true;
            }
            return false;
        }

        private IEnumerator Sequence() {
            while (!triggered && !PlayerFallCheck()) {
                yield return null;
            }

            Detach();

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

                float speed = 0f;
                //float maxSpeed = 160f;
                while (true) {
                    Level level = SceneAs<Level>();
                    speed = Calc.Approach(speed, 160f, 500f * Engine.DeltaTime);
                    if (MoveVCollideSolids(speed * Engine.DeltaTime, thruDashBlocks: true)) {
                        break;
                    }
                    if (Top > level.Bounds.Bottom + 16 || (Top > level.Bounds.Bottom - 1 && CollideCheck<Solid>(Position + new Vector2(0f, 1f)))) {
                        Collidable = Visible = false;
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
                yield return 0.2f;

                StopShaking();
                if (CollideCheck<SolidTiles>(Position + new Vector2(0f, 1f))) {
                    break;
                }

                while (CollideCheck<Platform>(Position + new Vector2(0f, 1f))) {
                    yield return 0.1f;
                }
            }
            Safe = true;
        }

        private void LandParticles() {
            for (int i = 2; i <= Width; i += 4) {
                if (Scene.CollideCheck<Solid>(BottomLeft + new Vector2(i, 3f))) {
                    SceneAs<Level>().ParticlesFG.Emit(FallingBlock.P_FallDustA, 1, new Vector2(X + i, Bottom), Vector2.One * 4f, -(float) Math.PI / 2f);
                    float direction = ((!(i < Width / 2f)) ? 0f : ((float) Math.PI));
                    SceneAs<Level>().ParticlesFG.Emit(FallingBlock.P_LandDust, 1, new Vector2(X + i, Bottom), Vector2.One * 4f, direction);
                }
            }
        }

        private void ShakeSfx() {
            Audio.Play(tileType switch { 
                '3' => SFX.game_01_fallingblock_ice_shake,
                '9' => SFX.game_03_fallingblock_wood_shake,
                'g' => SFX.game_06_fallingblock_boss_shake,
                _ => SFX.game_gen_fallblock_shake,
            }, Center);
        }

        private void ImpactSfx() {
            Audio.Play(tileType switch {
                '3' => SFX.game_01_fallingblock_ice_impact,
                '9' => SFX.game_03_fallingblock_wood_impact,
                'g' => SFX.game_06_fallingblock_boss_impact,
                _ => SFX.game_gen_fallblock_impact,
            }, Center);
        }
    }
}
