using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.CommunalHelper.Entities.DreamStuff {

    [Tracked]
    class DreamMaskComponent : Component {

        private Func<bool> enabledProvider = () => true;

        public bool JellyRendering = false;

        public Sprite Sprite = null;

        public DreamParticle[] Particles;

        public Rectangle ParticleBounds = Rectangle.Empty;

        public float Flash = 0;

        public Vector2 Position => JellyRendering ? Entity.Position : Sprite.Position + Entity.Position;

        public DreamMaskComponent(Sprite sprite, Rectangle particleBounds, Func<bool> isEnabled = null, bool jellyRendering = true) : base(true, true) {
            Sprite = sprite;
            ParticleBounds = particleBounds;
            enabledProvider = isEnabled ?? enabledProvider;
            JellyRendering = jellyRendering;
        }

        public override void EntityAwake() {
            base.EntityAwake();
            Entity.Scene.Tracker.GetEntity<DreamMaskRenderer>().Track(this);

            int w = ParticleBounds.Width;
            int h = ParticleBounds.Height;
            Particles = new DreamParticle[(int) (w / 8f * (h / 8f) * 1.5f)];
            for (int i = 0; i < Particles.Length; i++) {
                Particles[i].Position = new Vector2(Calc.Random.NextFloat(w), Calc.Random.NextFloat(h));
                Particles[i].Layer = Calc.Random.Choose(0, 1, 1, 2, 2, 2);
                Particles[i].TimeOffset = Calc.Random.NextFloat();

                Particles[i].DisabledColor = Color.LightGray * (0.5f + Particles[i].Layer / 2f * 0.5f);
                Particles[i].DisabledColor.A = 255;

                Particles[i].EnabledColor = Particles[i].Layer switch {
                    0 => Calc.Random.Choose(CustomDreamBlock.DreamColors[0], CustomDreamBlock.DreamColors[1], CustomDreamBlock.DreamColors[2]),
                    1 => Calc.Random.Choose(CustomDreamBlock.DreamColors[3], CustomDreamBlock.DreamColors[4], CustomDreamBlock.DreamColors[5]),
                    2 => Calc.Random.Choose(CustomDreamBlock.DreamColors[6], CustomDreamBlock.DreamColors[7], CustomDreamBlock.DreamColors[8]),
                    _ => throw new NotImplementedException()
                };
            }
        }

        public override void EntityRemoved(Scene scene) {
            base.EntityRemoved(scene);
            scene.Tracker.GetEntity<DreamMaskRenderer>().Untrack(this);
        }

        public virtual MTexture CurrentFrame() {
            Sprite mask = Sprite;
            return mask.GetFrame(mask.CurrentAnimationID, mask.CurrentAnimationFrame);
        }

        public bool EnabledParticleColors() => enabledProvider.Invoke();
    }
}