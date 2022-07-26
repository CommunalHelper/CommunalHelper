using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/DreamBooster")]
    public class DreamBoosterSegment : DreamBooster {
        public class PathRenderer : PathRendererBase<DreamBoosterSegment> {
            private readonly Vector2 perp;

            public PathRenderer(float alpha, DreamBoosterSegment booster)
                : base(alpha, booster.Style, booster) {
                perp = booster.Dir.Perpendicular();
            }

            public override void Render() {
                base.Render();
                if (Alpha <= 0f)
                    return;

                Color color = Booster.BoostingPlayer ? CurrentRainbowColor : Color.White;

                Util.TryGetPlayer(out Player player);

                for (float f = 0f; f < Booster.Length * Percent; f += 6f)
                    DrawPathLine(Calc.Round(Booster.Start + Booster.Dir * f), Booster.Dir, perp, f, player, color);
                DrawPathLine(Calc.Round(Booster.Target), Booster.Dir, perp, Booster.Length, null, color);
            }
        }

        private PathRenderer pathRenderer;
        private bool showPath = true;

        public float Length;
        public Vector2 Start, Target, Dir;

        public DreamBoosterSegment(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Nodes[0] + offset, !data.Bool("hidePath"), data.Enum("pathStyle", PathStyle.Arrow)) { }

        public DreamBoosterSegment(Vector2 position, Vector2 node, bool showPath, PathStyle style)
            : base(position, showPath, style) {
            Target = node;
            Dir = Calc.SafeNormalize(Target - Position);
            Length = Vector2.Distance(position, Target);
            Start = position;

            this.showPath = showPath;
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            scene.Add(pathRenderer = new PathRenderer(Util.ToInt(showPath), this));
        }

        public override void Removed(Scene scene) {
            base.Removed(scene);
            scene.Remove(pathRenderer);
            pathRenderer = null;
            base.Removed(scene);
        }

        protected override void OnPlayerEnter(Player player) {
            base.OnPlayerEnter(player);
            pathRenderer.ResetRainbow();
            if (!showPath)
                Add(new Coroutine(RevealPathRoutine()));
        }

        private IEnumerator RevealPathRoutine() {
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
                pathRenderer.Alpha = pathRenderer.Percent = Math.Min(Ease.SineOut(timer / duration), 1);

                Vector2 pos = Start + Dir * pathRenderer.Percent * Length;

                particlesBG.Emit(DreamParticles[Calc.Random.Range(0, 8)], 2, pos, Vector2.One * 2f, (-Dir).Angle());
                yield return null;
            }
            pathRenderer.Percent = 1f;
        }
    }
}
