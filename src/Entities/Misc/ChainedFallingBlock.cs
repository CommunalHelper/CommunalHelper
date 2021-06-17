using Celeste.Mod.Entities;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/ChainedFallingBlock")]
    class ChainedFallingBlock : Solid {

        public struct ChainNode {

            public Vector2 Position, Velocity, Acceleration;

            public void UpdateStep() {
                Velocity += Acceleration * Engine.DeltaTime;
                Position += Velocity * Engine.DeltaTime;
                Acceleration = Vector2.Zero;
            }

            public void ConstraintTo(ChainNode other, float distance) {
                if (Vector2.Distance(other.Position, Position) > distance) {
                    Vector2 from = Position;
                    Vector2 dir = from - other.Position;
                    dir.Normalize();
                    Position = other.Position + dir * distance;
                    Vector2 accel = Position - from;
                    Acceleration += accel * 200f;
                }
            }
        }

        public class Chain : Entity {

            public ChainNode[] Nodes;
            private Func<Vector2> attachedEndGetter;

            private float distanceConstraint;

            private bool outline;

            public Chain(Vector2 position, bool outline, int nodeCount, float distanceConstraint, Func<Vector2> attachedEndGetter) 
                : base(position) {

                Nodes = new ChainNode[nodeCount];
                this.attachedEndGetter = attachedEndGetter;
                this.distanceConstraint = distanceConstraint;

                this.outline = outline;

                for (int i = 0; i < nodeCount - 1; i++) {
                    Nodes[i].Position = Position;
                }
                Nodes[nodeCount - 1].Position = attachedEndGetter();
                ChainUpdate();
            }

            public override void Update() {
                base.Update();
                if (Util.TryGetPlayer(out Player player)) {
                    for (int i = 1; i < Nodes.Length - 1; i++) {
                        if (player.CollidePoint(Nodes[i].Position)) {
                            Nodes[i].Acceleration += player.Speed * 3f;
                        }
                    }
                }
            }

            public void ChainUpdate() {
                Nodes[Nodes.Length - 1].Position = attachedEndGetter();
                Nodes[Nodes.Length - 1].Velocity = Vector2.Zero;

                for (int i = 1; i < Nodes.Length; i++) {
                    // gravity
                    if (i < Nodes.Length - 1)
                        Nodes[i].Acceleration += Vector2.UnitY * 160f;

                    Nodes[i].UpdateStep();
                }

                for (int i = 1; i < Nodes.Length - 1; i++)
                    Nodes[i].ConstraintTo(Nodes[i - 1], distanceConstraint);
                for (int i = Nodes.Length - 2; i > 0; i--)
                    Nodes[i].ConstraintTo(Nodes[i + 1], distanceConstraint);
            }

            public override void Render() {
                base.Render();
                if (outline) {
                    for (int i = 0; i < Nodes.Length - 1; i++) {
                        if (Calc.Round(Nodes[i].Position) == Calc.Round(Nodes[i + 1].Position)) {
                            continue;
                        }
                        Vector2 mid = (Nodes[i].Position + Nodes[i + 1].Position) * 0.5f;
                        float angle = Calc.Angle(Nodes[i].Position, Nodes[i + 1].Position) - MathHelper.PiOver2;
                        chain.DrawOutlineCentered(mid, Color.White, 1f, angle);
                    }
                }
                for (int i = 0; i < Nodes.Length - 1; i++) {
                    if (Calc.Round(Nodes[i].Position) == Calc.Round(Nodes[i + 1].Position)) {
                        continue;
                    }
                    Vector2 mid = (Nodes[i].Position + Nodes[i + 1].Position) * 0.5f;
                    float angle = Calc.Angle(Nodes[i].Position, Nodes[i + 1].Position) - MathHelper.PiOver2;
                    chain.DrawCentered(mid, Color.White, 1f, angle);
                }
            }
        }
        private Chain chainA, chainB;

        private char tileType;
        private TileGrid tiles;

        private bool triggered;
        private bool climbFall;
        private bool held;
        private bool chainShattered;

        private float chainStopY, startY;
        private bool centeredChain;
        private bool chainOutline;

        private bool indicator, indicatorAtStart;
        private float pathLerp;

        private EventInstance rattle;

        private static MTexture chain, chainEnd;

        public ChainedFallingBlock(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Width, data.Height, data.Char("tiletype", '3'), data.Bool("climbFall", true), data.Bool("behind"), data.Int("fallDistance"), data.Bool("centeredChain"), data.Bool("chainOutline", true), data.Bool("indicator"), data.Bool("indicatorAtStart")) { }

        public ChainedFallingBlock(Vector2 position, int width, int height, char tileType, bool climbFall, bool behind, int maxFallDistance, bool centeredChain, bool chainOutline, bool indicator, bool indicatorAtStart)
            : base(position, width, height, safe: false) {
            this.climbFall = climbFall;
            this.tileType = tileType;

            startY = Y;
            chainStopY = startY + maxFallDistance;
            this.centeredChain = centeredChain || Width <= 8;
            this.chainOutline = chainOutline;
            this.indicator = indicator;
            this.indicatorAtStart = indicatorAtStart;
            pathLerp = Util.ToInt(indicatorAtStart);

            Calc.PushRandom(Calc.Random.Next());
            Add(tiles = GFX.FGAutotiler.GenerateBox(tileType, width / 8, height / 8).TileGrid);
            Calc.PopRandom();

            Add(new Coroutine(Sequence()));
            Add(new LightOcclude());
            Add(new TileInterceptor(tiles, highPriority: false));

            SurfaceSoundIndex = SurfaceIndex.TileToIndex[tileType];
            if (behind)
                Depth = Depths.SolidsBelow;
        }

        public override void OnShake(Vector2 amount) {
            base.OnShake(amount);
            tiles.Position += amount;
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
            triggered = true;

            Vector2 rattleSoundPos = new Vector2(Center.X, startY);
            bool addChains = true;
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
                FallParticles();
                if (addChains) {
                    AddChains();
                    addChains = false;
                }

                rattle = Audio.Play(CustomSFX.game_chainedFallingBlock_chain_rattle, rattleSoundPos);

                float speed = 0f;
                //float maxSpeed = 160f;
                while (true) {
                    Level level = SceneAs<Level>();
                    speed = Calc.Approach(speed, 160f, 500f * Engine.DeltaTime);
                    if (MoveVCollideSolids(speed * Engine.DeltaTime, thruDashBlocks: true)) {
                        held = Y == chainStopY;
                        break;
                    } else if (Y > chainStopY && !chainShattered) {
                        held = true;
                        MoveToY(chainStopY, LiftSpeed.Y);
                        break;
                    }
                    Audio.Position(rattle, rattleSoundPos);
                    yield return null;
                }

                ImpactSfx();
                Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                SceneAs<Level>().DirectionalShake(Vector2.UnitY, 0.3f);
                StartShaking();
                LandParticles();
                ImpactChainShake();
                Audio.Stop(rattle);
                if (held) {
                    Audio.Play(CustomSFX.game_chainedFallingBlock_chain_tighten_block, TopCenter);
                    Audio.Play(CustomSFX.game_chainedFallingBlock_chain_tighten_ceiling, rattleSoundPos);
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

        private void LandParticles() {
            for (int i = 2; i <= Width; i += 4) {
                if (Scene.CollideCheck<Solid>(BottomLeft + new Vector2(i, 3f))) {
                    SceneAs<Level>().ParticlesFG.Emit(FallingBlock.P_FallDustA, 1, new Vector2(X + i, Bottom), Vector2.One * 4f, -(float) Math.PI / 2f);
                    float direction = (!(i < Width / 2f)) ? 0f : ((float) Math.PI);
                    SceneAs<Level>().ParticlesFG.Emit(FallingBlock.P_LandDust, 1, new Vector2(X + i, Bottom), Vector2.One * 4f, direction);;
                }
            }
        }

        private void FallParticles() {
            for (int i = 2; i < Width; i += 4) {
                if (Scene.CollideCheck<Solid>(TopLeft + new Vector2(i, -2f))) {
                    SceneAs<Level>().Particles.Emit(FallingBlock.P_FallDustA, 2, new Vector2(X + i, Y), Vector2.One * 4f, (float) Math.PI / 2f);
                }
                SceneAs<Level>().Particles.Emit(FallingBlock.P_FallDustB, 2, new Vector2(X + i, Y), Vector2.One * 4f);
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
            // Some impacts weren't as attenuated like the game_gen_fallblock_impact event,
            // and it was inconsistent with the fact that you can hear the chain tighten but not the block impact.
            // So custom impact sounds for all specific variants with matching distance attenuation effects were added.
            Audio.Play(tileType switch {
                '3' => CustomSFX.game_chainedFallingBlock_attenuatedImpacts_ice_impact,
                '9' => CustomSFX.game_chainedFallingBlock_attenuatedImpacts_wood_impact,
                'g' => CustomSFX.game_chainedFallingBlock_attenuatedImpacts_boss_impact,
                _ => SFX.game_gen_fallblock_impact,
            }, Center);
        }

        private void AddChains() {
            if (centeredChain) {
                Scene.Add(chainA = new Chain(new Vector2(Center.X, Y), chainOutline, (int) ((chainStopY - startY) / 8) + 1, 8, () => new Vector2(Center.X, Y)));
            } else {
                Scene.Add(chainA = new Chain(new Vector2(X + 4, Y), chainOutline, (int) ((chainStopY - startY) / 8) + 1, 8, () => new Vector2(X + 4, Y)));
                Scene.Add(chainB = new Chain(new Vector2(Right - 4, Y), chainOutline, (int) ((chainStopY - startY) / 8) + 1, 8, () => new Vector2(Right - 4, Y)));
            }
        }
        
        private void ImpactChainShake() {
            if (chainA != null) {
                for (int i = 1; i < chainA.Nodes.Length - 1; i++) {
                    chainA.Nodes[i].Acceleration += new Vector2(Calc.Random.NextFloat(2f) - 1f, Calc.Random.NextFloat(2f) - 1f) * 3000f;
                }
            }
            if (chainB != null) {
                for (int i = 1; i < chainB.Nodes.Length - 1; i++) {
                    chainB.Nodes[i].Acceleration += new Vector2(Calc.Random.NextFloat(2f) - 1f, Calc.Random.NextFloat(2f) - 1f) * 3000f;
                }
            }
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
        }

        public override void Removed(Scene scene) {
            base.Removed(scene);
            Audio.Stop(rattle);

            chainA?.RemoveSelf();
            chainB?.RemoveSelf();
        }

        public override void Update() {
            base.Update();

            chainA?.ChainUpdate();
            chainB?.ChainUpdate();

            if (triggered && indicator && !indicatorAtStart)
                pathLerp = Calc.Approach(pathLerp, 1f, Engine.DeltaTime * 2f);
        }

        public override void Render() {
            if ((triggered || indicatorAtStart) && indicator && !held && !chainShattered) {
                float toY = startY + (chainStopY + Height - startY) * Ease.ExpoOut(pathLerp);
                Draw.Rect(X, Y, Width, toY - Y, Color.Black * 0.75f);
            }

            base.Render();
        }

        public static void InitializeTextures() {
            MTexture texture = GFX.Game["objects/hanginglamp"];
            chainEnd = texture.GetSubtexture(0, 0, 8, 8);
            chain = texture.GetSubtexture(0, 8, 8, 8);
        }
    }
}
