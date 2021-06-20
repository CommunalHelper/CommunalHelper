using Celeste.Mod.Entities;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/Chain")]
    public class Chain : Entity {

        public static MTexture ChainTexture;

        private MTexture segment;
        private bool outline;

        public ChainNode[] Nodes;
        protected Func<Vector2> attachedStartGetter, attachedEndGetter;

        protected float distanceConstraint;

        public bool AllowPlayerInteraction;
        public bool Tight;
        private bool placed, attached, canShatter;

        private EventInstance sfx;
        private Vector2 sfxPos;

        protected Solid StartSolid, EndSolid;

        public Chain(EntityData data, Vector2 offset)
            : this(ChainTexture, data.Bool("outline", true), (int) (Vector2.Distance(data.Position + offset, data.NodesOffset(offset)[0]) / 8 + 1 + data.Int("extraJoints")), 8, () => data.Position + offset, () => data.Nodes[0] + offset) {
            AllowPlayerInteraction = true;
            placed = true;
        }

        public Chain(MTexture segment, bool outline, int nodeCount, float distanceConstraint, Func<Vector2> attachedStartGetter, Func<Vector2> attachedEndGetter, bool canShatter = true)
            : base(attachedStartGetter()) {

            Nodes = new ChainNode[nodeCount];
            this.attachedStartGetter = attachedStartGetter;
            this.attachedEndGetter = attachedEndGetter;
            this.distanceConstraint = distanceConstraint;

            this.segment = segment;
            this.outline = outline;
            this.canShatter = canShatter;

            Tighten();
            UpdateChain();

            sfx = Audio.Play(CustomSFX.game_chain_move);
        }

        public void Tighten(bool instantly = true) {
            Vector2 from = attachedStartGetter != null ? attachedStartGetter() : Position;
            Vector2 to = attachedEndGetter != null ? attachedEndGetter() : Position;

            for (int i = 0; i < Nodes.Length; i++) {
                Vector2 newPos = Vector2.Lerp(from, to, i / (Nodes.Length - 1));
                if (instantly) {
                    Nodes[i].Position = newPos;
                    Nodes[i].Velocity *= 0.2f;
                } else {
                    Nodes[i].Velocity = (newPos - Position) * 75f;
                }
            }
        }

        private void AttachedEndsToSolids(Scene scene) {
            if (attached)
                return;
            attached = true;

            Vector2 start = Nodes[0].Position;
            Vector2 end = Nodes[Nodes.Length - 1].Position;

            StartSolid = scene.CollideFirst<Solid>(new Rectangle((int) start.X - 2, (int) start.Y - 2, 4, 4));
            EndSolid = scene.CollideFirst<Solid>(new Rectangle((int) end.X - 2, (int) end.Y - 2, 4, 4));

            if (StartSolid != null) {
                Vector2 offset = start - StartSolid.Position;
                attachedStartGetter = () => StartSolid.Position + offset;
            } else {
                DetachStart();
            }

            if (EndSolid != null) {
                Vector2 offset = end - EndSolid.Position;
                attachedEndGetter = () => EndSolid.Position + offset;
            } else {
                DetachEnd();
            }

            if (attachedStartGetter == null && attachedEndGetter == null)
                RemoveSelf();
        }

        private void UpdateSfx(Vector2[] oldPositions) {
            float intensity = 0f;
            if (Nodes.Length > 0 && Nodes.Length == oldPositions.Length && sfx != null && Util.TryGetPlayer(out Player player)) {
                float minDistSqr = float.MaxValue;

                Vector2 averageOffset = Vector2.Zero;
                int eff = Nodes.Length;
                for (int i = 0; i < Nodes.Length; i++) {
                    float lengthSqr = (player.Center - Nodes[i].Position).LengthSquared();
                    if (lengthSqr < minDistSqr) {
                        minDistSqr = lengthSqr;
                        sfxPos = Nodes[i].Position;
                    }

                    Vector2 offset = Nodes[i].Position - oldPositions[i];
                    if (offset.LengthSquared() < 0.025f && eff > 1)
                        eff--;
                    averageOffset += offset;
                }
                averageOffset /= eff;

                intensity = Tight ? 0f : Calc.ClampedMap(averageOffset.Length(), 0f, 2f);
            }
            Audio.SetParameter(sfx, "intensity", intensity);
            Audio.Position(sfx, sfxPos);
        }

        public override void Removed(Scene scene) {
            base.Removed(scene);
            Audio.Stop(sfx);
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            if (placed) {
                AttachedEndsToSolids(scene);
            }
        }

        public override void Update() {
            base.Update();

            Vector2[] oldPositions = Nodes.Select(node => node.Position).ToArray();
            UpdateChain();
            UpdateSfx(oldPositions);

            if (canShatter && Vector2.Distance(Nodes[0].Position, Nodes[Nodes.Length - 1].Position) > (Nodes.Length + 1) * distanceConstraint) {
                BreakInHalf();
            }

            if (AllowPlayerInteraction && !Tight) {
                if (Util.TryGetPlayer(out Player player)) {
                    for (int i = attachedStartGetter != null ? 1 : 0; i < Nodes.Length - (attachedEndGetter != null ? 1 : 0); i++) {
                        if (player.CollidePoint(Nodes[i].Position)) {
                            Nodes[i].Acceleration += player.Speed * 8f;
                        }
                    }
                }
            }
        }

        protected virtual void BreakInHalf() {
            RemoveSelf();
            Vector2 middleNode = Nodes[Nodes.Length / 2].Position;

            Chain a, b;
            Scene.Add(a = new Chain(ChainTexture, true, Nodes.Length / 2, 8, () => middleNode, attachedStartGetter) {
                AllowPlayerInteraction = true,
            });
            a.AttachedEndsToSolids(Scene);
            a.ShakeImpulse();

            Scene.Add(b = new Chain(ChainTexture, true, Nodes.Length / 2, 8, () => middleNode, attachedEndGetter) {
                AllowPlayerInteraction = true,
            });
            b.AttachedEndsToSolids(Scene);
            b.ShakeImpulse();

            Audio.Play(CustomSFX.game_chainedFallingBlock_chain_tighten_block, middleNode);

            Level level = SceneAs<Level>();
            for (int i = 0; i < 10; i++)
                level.ParticlesFG.Emit(ZipMover.P_Sparks, middleNode, Calc.Random.NextAngle());
        }

        private void UpdateChain() {
            bool startAttached = attachedStartGetter != null;
            bool endAttached = attachedEndGetter != null;
            if (startAttached) {
                Nodes[0].Position = attachedStartGetter();
                Nodes[0].Velocity = Vector2.Zero;
            }
            if (endAttached) {
                Nodes[Nodes.Length - 1].Position = attachedEndGetter();
                Nodes[Nodes.Length - 1].Velocity = Vector2.Zero;
            }

            for (int i = 0; i < Nodes.Length; i++) {
                Nodes[i].Acceleration += Vector2.UnitY * 220f;
                if (Scene is not null && !Tight)
                    Nodes[i].Acceleration += SceneAs<Level>().Wind;
                Nodes[i].UpdateStep();
            }

            if (!startAttached && !endAttached) {
                for (int i = 1; i < Nodes.Length; i++)
                    Nodes[i].ConstraintTo(Nodes[i - 1].Position, distanceConstraint, Tight);
                for (int i = Nodes.Length - 2; i >= 0; i--)
                    Nodes[i].ConstraintTo(Nodes[i + 1].Position, distanceConstraint, Tight);
            } else {
                if (startAttached) {
                    for (int i = 1; i < Nodes.Length - (endAttached ? 1 : 0); i++)
                        Nodes[i].ConstraintTo(Nodes[i - 1].Position, distanceConstraint, Tight);
                }
                if (endAttached) {
                    for (int i = Nodes.Length - 2; i >= (startAttached ? 1 : 0); i--)
                        Nodes[i].ConstraintTo(Nodes[i + 1].Position, distanceConstraint, Tight);
                }
            }
        }

        public void DetachStart() {
            attachedStartGetter = null;
        }

        public void DetachEnd() {
            attachedEndGetter = null;
        }

        public void FakeShake() {
            for (int i = attachedStartGetter != null ? 1 : 0; i < Nodes.Length - (attachedEndGetter != null ? 1 : 0); i++) {
                Nodes[i].Position += Util.RandomDir(4f);
            }
        }

        public void ShakeImpulse(float strength = 10000f) {
            for (int i = attachedStartGetter != null ? 1 : 0; i < Nodes.Length - (attachedEndGetter != null ? 1 : 0); i++) {
                Nodes[i].Acceleration += Util.RandomDir(strength);
            }
        }

        public override void Render() {
            base.Render();
            if (outline) {
                for (int i = 0; i < Nodes.Length - 1; i++) {
                    if (Calc.Round(Nodes[i].Position) == Calc.Round(Nodes[i + 1].Position)) {
                        continue;
                    }
                    float yScale = Vector2.Distance(Nodes[i].Position, Nodes[i + 1].Position) / distanceConstraint;
                    Vector2 mid = (Nodes[i].Position + Nodes[i + 1].Position) * 0.5f;
                    float angle = Calc.Angle(Nodes[i].Position, Nodes[i + 1].Position) - MathHelper.PiOver2;
                    segment.DrawOutlineCentered(mid, Color.White, new Vector2(1f, yScale), angle);
                }
            }
            for (int i = 0; i < Nodes.Length - 1; i++) {
                if (Calc.Round(Nodes[i].Position) == Calc.Round(Nodes[i + 1].Position)) {
                    continue;
                }
                float yScale = Vector2.Distance(Nodes[i].Position, Nodes[i + 1].Position) / distanceConstraint;
                Vector2 mid = (Nodes[i].Position + Nodes[i + 1].Position) * 0.5f;
                float angle = Calc.Angle(Nodes[i].Position, Nodes[i + 1].Position) - MathHelper.PiOver2;
                segment.DrawCentered(mid, Color.White, new Vector2(1f, yScale), angle);
            }
            ChainTexture = GFX.Game["objects/hanginglamp"].GetSubtexture(0, 8, 8, 8);
        }

        public static void InitializeTextures() {
            ChainTexture = GFX.Game["objects/hanginglamp"].GetSubtexture(0, 8, 8, 8);
        }
    }

    public struct ChainNode {

        public Vector2 Position, Velocity, Acceleration;

        public void UpdateStep() {
            Velocity += Acceleration * Engine.DeltaTime;
            Position += Velocity * Engine.DeltaTime;
            Velocity *= 1 - Engine.DeltaTime;
            Acceleration = Vector2.Zero;
        }

        public void ConstraintTo(Vector2 to, float distance, bool cancelAcceleration) {
            if (Vector2.Distance(to, Position) > distance) {
                Vector2 from = Position;
                Vector2 dir = from - to;
                dir.Normalize();
                Position = to + dir * distance;
                if (!cancelAcceleration) {
                    Vector2 accel = Position - from;
                    accel.X = Calc.Clamp(accel.X, -2f, 2f);
                    accel.Y = Calc.Clamp(accel.Y, -2f, 2f);
                    Acceleration += accel * 210f;
                }
            }
        }
    }
}
