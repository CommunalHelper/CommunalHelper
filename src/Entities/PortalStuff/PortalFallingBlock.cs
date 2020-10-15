using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;

namespace Celeste.Mod.CommunalHelper.Entities {

    [CustomEntity("CommunalHelper/PortalFallingBlock")]
    [Tracked(false)]
    class PortalFallingBlock : SlicedSolid {

        public bool Triggered;

        public float FallDelay;

        private char TileType;

        private bool finalBoss;

        private bool climbFall;

        private DynamicTexture dynTexture = new DynamicTexture();

        public bool HasStartedFalling {
            get;
            private set;
        }

        public PortalFallingBlock(Vector2 position, char tile, int width, int height, bool finalBoss, bool behind, bool climbFall)
            : base(position, width, height, safe: false) {
            this.finalBoss = finalBoss;
            this.climbFall = climbFall;
            int newSeed = Calc.Random.Next();
            Calc.PushRandom(newSeed);

            int w = width / 8, h = height / 8;
            VirtualMap<MTexture> tiles = GFX.FGAutotiler.GenerateBox(tile, w, h).TileGrid.Tiles;
            for (int i = 0; i < w; i++)
                for (int j = 0; j < h; j++)
                    dynTexture.AddTexture(tiles[i, j], new Vector2(i * 8, j * 8), Color.White);


            Calc.PopRandom();
            Add(new Coroutine(Sequence()));
            Add(new LightOcclude());
            TileType = tile;
            SurfaceSoundIndex = SurfaceIndex.TileToIndex[tile];
            if (behind) {
                base.Depth = 5000;
            }
        }

        public PortalFallingBlock(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Char("tiletype", '3'), data.Width, data.Height, finalBoss: false, data.Bool("behind"), data.Bool("climbFall", defaultValue: true)) {
        }

        public static FallingBlock CreateFinalBossBlock(EntityData data, Vector2 offset) {
            return new FallingBlock(data.Position + offset, 'g', data.Width, data.Height, finalBoss: true, behind: false, climbFall: false);
        }

        public override void OnStaticMoverTrigger(StaticMover sm) {
            if (!finalBoss) {
                Triggered = true;
            }
        }

        private bool PlayerFallCheck() {
            if (climbFall) {
                return HasPlayerRider();
            }
            return HasPlayerOnTop();
        }

        private bool PlayerWaitCheck() {
            if (Triggered) {
                return true;
            }
            if (PlayerFallCheck()) {
                return true;
            }
            if (climbFall) {
                if (!CollideCheck<Player>(Position - Vector2.UnitX)) {
                    return CollideCheck<Player>(Position + Vector2.UnitX);
                }
                return true;
            }
            return false;
        }

        public override void Render() {

            base.Render();
            Vector2 pos = Position;
            Position += Shake;
            MapTextureOnColliders(dynTexture);
            Position = pos;
        }

        private IEnumerator Sequence() {
            while (!Triggered && (finalBoss || !PlayerFallCheck())) {
                yield return null;
            }
            while (FallDelay > 0f) {
                FallDelay -= Engine.DeltaTime;
                yield return null;
            }
            HasStartedFalling = true;
            while (true) {
                ShakeSfx();
                StartShaking();
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                yield return 0.2f;
                float timer = 0.4f;
                if (finalBoss) {
                    timer = 0.2f;
                }
                while (timer > 0f && PlayerWaitCheck()) {
                    yield return null;
                    timer -= Engine.DeltaTime;
                }
                StopShaking();
                for (int i = 2; (float) i < Width; i += 4) {
                    if (Scene.CollideCheck<Solid>(TopLeft + new Vector2(i, -2f))) {
                        SceneAs<Level>().Particles.Emit(FallingBlock.P_FallDustA, 2, new Vector2(X + (float) i, Y), Vector2.One * 4f, (float) Math.PI / 2f);
                    }
                    SceneAs<Level>().Particles.Emit(FallingBlock.P_FallDustB, 2, new Vector2(X + (float) i, Y), Vector2.One * 4f);
                }
                float speed = 0f;
                float maxSpeed = finalBoss ? 130f : 160f;
                while (true) {
                    Level level = SceneAs<Level>();
                    speed = Calc.Approach(speed, maxSpeed, 500f * Engine.DeltaTime);
                    Move(speed * Vector2.UnitY * Engine.DeltaTime);
                    if (CollideCheck<Solid>())
                        break;
                    if (Top > (float) (level.Bounds.Bottom + 16) || (Top > (float) (level.Bounds.Bottom - 1) && CollideCheck<Solid>(Position + new Vector2(0f, 1f)))) {
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
                while (CollideCheck<Solid>())
                    Move(-Vector2.UnitY);
                ImpactSfx();
                Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                SceneAs<Level>().DirectionalShake(Vector2.UnitY, finalBoss ? 0.2f : 0.3f);
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
            for (int i = 2; (float) i <= base.Width; i += 4) {
                if (base.Scene.CollideCheck<Solid>(base.BottomLeft + new Vector2(i, 3f))) {
                    SceneAs<Level>().ParticlesFG.Emit(FallingBlock.P_FallDustA, 1, new Vector2(base.X + (float) i, base.Bottom), Vector2.One * 4f, -(float) Math.PI / 2f);
                    float direction = (!((float) i < base.Width / 2f)) ? 0f : ((float) Math.PI);
                    SceneAs<Level>().ParticlesFG.Emit(FallingBlock.P_LandDust, 1, new Vector2(base.X + (float) i, base.Bottom), Vector2.One * 4f, direction);
                }
            }
        }

        private void ShakeSfx() {
            if (TileType == '3') {
                Audio.Play("event:/game/01_forsaken_city/fallblock_ice_shake", base.Center);
            } else if (TileType == '9') {
                Audio.Play("event:/game/03_resort/fallblock_wood_shake", base.Center);
            } else if (TileType == 'g') {
                Audio.Play("event:/game/06_reflection/fallblock_boss_shake", base.Center);
            } else {
                Audio.Play("event:/game/general/fallblock_shake", base.Center);
            }
        }

        private void ImpactSfx() {
            if (TileType == '3') {
                Audio.Play("event:/game/01_forsaken_city/fallblock_ice_impact", base.BottomCenter);
            } else if (TileType == '9') {
                Audio.Play("event:/game/03_resort/fallblock_wood_impact", base.BottomCenter);
            } else if (TileType == 'g') {
                Audio.Play("event:/game/06_reflection/fallblock_boss_impact", base.BottomCenter);
            } else {
                Audio.Play("event:/game/general/fallblock_impact", base.BottomCenter);
            }
        }
    }
}
