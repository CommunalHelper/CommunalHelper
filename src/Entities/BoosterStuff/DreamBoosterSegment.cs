using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/DreamBooster")]
    public class DreamBoosterSegment : DreamBooster {
        public class DreamBoosterPathRenderer : Entity {
            private DreamBoosterSegment dreamBooster;

            public float Alpha;
            public float Percent = 1f;
            public float RainbowLerp;

            private Vector2 perp;

            public DreamBoosterPathRenderer(DreamBoosterSegment booster, float alpha) {
                Depth = Depths.DreamBlocks + 1;
                dreamBooster = booster;

                Alpha = Percent = alpha;
                perp = dreamBooster.Dir.Perpendicular();
            }

            public override void Update() {
                base.Update();
                if (dreamBooster.BoostingPlayer)
                    RainbowLerp += Engine.DeltaTime * 8f;
            }

            public override void Render() {
                base.Render();
                if (Alpha <= 0f)
                    return;

                Color color = dreamBooster.BoostingPlayer ? Util.ColorArrayLerp(RainbowLerp, DreamColors) : Color.White;

                Util.TryGetPlayer(out Player player);
                for (float f = 0f; f < dreamBooster.Length * Percent; f += 6f) {
                    DrawPathLine(f, player, color);
                }
                DrawPathLine(dreamBooster.Length * Percent - dreamBooster.Length % 6, null, Color.White);
            }

            private void DrawPathLine(float linePos, Player player, Color lerp) {
                Vector2 pos = dreamBooster.Start + dreamBooster.Dir * linePos;
                float sin = (float) Math.Sin(linePos + Scene.TimeActive * 6f) * 0.3f + 1f;

                float highlight = player == null ? 0.25f : Calc.ClampedMap(Vector2.Distance(player.Center, pos), 0, 80);
                float lineHighlight = (1 - highlight) * 2.5f + 0.75f;
                float alphaHighlight = 1 - Calc.Clamp(highlight, 0.01f, 0.8f);
                Color color = Color.Lerp(Color.White, lerp, 1 - highlight) * alphaHighlight;

                float lineLength = lineHighlight * sin;
                Vector2 lineOffset = perp * lineLength;

                // Single perpendicular short segments
                //Draw.Line(pos + lineOffset, pos - lineOffset, color * Alpha);

                // "Arrow" style
                Vector2 arrowOffset = -dreamBooster.Dir * lineLength;
                Draw.Line(pos, pos - lineOffset + arrowOffset, color * Alpha);
                Draw.Line(pos, pos + lineOffset + arrowOffset, color * Alpha);
            }
        }

        private DreamBoosterPathRenderer pathRenderer;
        private bool showPath = true;

        public float Length;
        public Vector2 Start, Target, Dir;

        public DreamBoosterSegment(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Nodes[0] + offset, !data.Bool("hidePath")) { }

        public DreamBoosterSegment(Vector2 position, Vector2 node, bool showPath)
            : base(position, showPath) {
            Depth = Depths.DreamBlocks;

            Target = node;
            Dir = Calc.SafeNormalize(Target - Position);
            Length = Vector2.Distance(position, Target);
            Start = position;

            this.showPath = showPath;
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            scene.Add(pathRenderer = new DreamBoosterPathRenderer(this, Util.ToInt(showPath)));
        }

        public override void Removed(Scene scene) {
            base.Removed(scene);
            scene.Remove(pathRenderer);
            pathRenderer = null;
            base.Removed(scene);
        }

        protected override void OnPlayerEnter(Player player) {
            base.OnPlayerEnter(player);
            pathRenderer.RainbowLerp = Calc.Random.Range(0, 8);
            if (!showPath)
                Add(new Coroutine(HiddenPathReact()));
        }

        private IEnumerator HiddenPathReact() {
            float duration = 0.5f;
            float timer = 0f;

            showPath = true;
            SetSoundEvent(
                CustomSFX.game_customBoosters_dreamBooster_dreambooster_enter,
                CustomSFX.game_customBoosters_dreamBooster_dreambooster_move,
                false);

            ParticleSystem particlesBG = SceneAs<Level>().ParticlesBG;
            while (timer < duration) {
                timer += Engine.DeltaTime;
                pathRenderer.Alpha = pathRenderer.Percent = Ease.SineOut(timer / duration);

                Vector2 pos = Start + Dir * pathRenderer.Percent * Length;

                particlesBG.Emit(DreamParticles[Calc.Random.Range(0, 8)], 2, pos, Vector2.One * 2f, (-Dir).Angle());
                yield return null;
            }
        }
    }
}
