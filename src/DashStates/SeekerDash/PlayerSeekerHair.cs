using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.CommunalHelper.DashStates {
    // Not meant to be added on something that isn't Player
    public class PlayerSeekerHair : Component {
        private static MTexture[] seekerHairSegments;

        // Couldn't find a better name
        private class Braid {
            private readonly Vector2[] nodes = new Vector2[7];
            private readonly Vector2 direction;

            private float wave = Calc.Random.NextAngle();
            private float angleSpeed = 1f;

            private readonly float spasmInterval = Calc.Random.NextFloat(6f);

            public Braid(Vector2 direction) {
                this.direction = direction;
            }

            public void Simulate(Vector2 from, int facing) {
                nodes[0] = from;

                wave += Engine.DeltaTime * 4f * angleSpeed;
                angleSpeed = Calc.Approach(angleSpeed, 1f, Engine.DeltaTime * 3f);

                if (Engine.Scene.OnInterval(spasmInterval))
                    angleSpeed += Calc.Random.Next(1, 4);

                float angle = (float) Math.Sin(Engine.Scene.TimeActive) * .2f;
                Vector2 dir = new Vector2(direction.X * -facing, direction.Y).Rotate(angle);
                Vector2 waveDir = dir.Perpendicular() * 1.125f;

                Vector2 target = nodes[0] + dir + (waveDir * (float) Math.Sin(wave));
                Vector2 current = nodes[0];

                const float step = 3;
                const float stepSq = step * 2f;

                for (int i = 1; i < nodes.Length; i++) {
                    float speed = (1f - (float) i / nodes.Length * 0.5f) * 64f;
                    nodes[i] = Calc.Approach(nodes[i], target, speed * Engine.DeltaTime);

                    if (Vector2.DistanceSquared(nodes[i], current) > stepSq)
                        nodes[i] = current + (nodes[i] - current).SafeNormalize() * step;

                    target = nodes[i] + dir + (waveDir * (float) Math.Sin(wave + i * 0.8f));
                    current = nodes[i];
                }
            }

            public void RenderOutline() {
                for (int i = 0; i < nodes.Length; i++) {
                    Vector2 scale = GetHairScale(i);
                    MTexture hair = seekerHairSegments[i % seekerHairSegments.Length];
                    hair.DrawCentered(nodes[i] + Vector2.UnitX, Color.Black, scale);
                    hair.DrawCentered(nodes[i] - Vector2.UnitX, Color.Black, scale);
                    hair.DrawCentered(nodes[i] + Vector2.UnitY, Color.Black, scale);
                    hair.DrawCentered(nodes[i] - Vector2.UnitY, Color.Black, scale);
                }
            }

            public void Render() {
                for (int i = 0; i < nodes.Length; i++)
                    seekerHairSegments[i % seekerHairSegments.Length].DrawCentered(nodes[i], Seeker.TrailColor, GetHairScale(i));
            }

            private Vector2 GetHairScale(int index)
                => new Vector2(0.25f + (1f - (float) index / nodes.Length) * 0.75f);
        }

        private readonly Braid[] braids = new Braid[] {
            new(Calc.AngleToVector(-.8f, 2f)),
            new(Calc.AngleToVector(-.2f, 2f)),
            new(Calc.AngleToVector( .3f, 2f)),
        };

        public PlayerSeekerHair()
            : base(active: true, visible: true) { }

        public override void Update() {
            Visible = SeekerDash.HasSeekerDash || SeekerDash.SeekerAttacking;
            base.Update();

            Player player = Entity as Player;
            PlayerSprite sprite = player.Sprite;
            int facing = (int) player.Facing;

            Vector2 hairOffset = player.Sprite.HairOffset * new Vector2(facing, 1f);
            Vector2 hairPosition = player.Sprite.RenderPosition + new Vector2(0f, -9f * sprite.Scale.Y) + hairOffset;

            foreach (Braid braid in braids)
                braid.Simulate(hairPosition, (int) player.Facing);
        }

        public override void Render() {
            foreach (Braid braid in braids)
                braid.RenderOutline();
            foreach (Braid braid in braids)
                braid.Render();
        }

        internal static void InitializeTextures() {
            seekerHairSegments = GFX.Game.GetAtlasSubtextures("characters/player/CommunalHelper/seekerhair").ToArray();
        }
    }
}
