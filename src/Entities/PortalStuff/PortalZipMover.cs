using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections;

namespace Celeste.Mod.CommunalHelper.Entities
{
    [CustomEntity("CommunalHelper/PortalZipMover")]
    class PortalZipMover : SlicedSolid {

        private SoundSource sfx = new SoundSource();
        private Vector2 target;

        public PortalZipMover(EntityData data, Vector2 offset) 
            : this(data.Position + offset, data.Width, data.Height)
        { }

        public PortalZipMover(Vector2 position, int width, int height)
            : base(position, width, height, safe: false)
        {

            Add(new Coroutine(Sequence()));
            sfx.Position = new Vector2(width, height) / 2f;
            Add(sfx);
            target = position + new Vector2(64, -16);
        }

        public override void Update()
        {
            base.Update();
        }

        private IEnumerator Sequence() {
            Vector2 start = Position;
            float percent = 0f;
            while (true) {
                yield return .5f;
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
                StartShaking(0.1f);
                yield return 0.1f;
                StopPlayerRunIntoAnimation = false;
                float at2 = 0f;
                while (at2 < 1f) {
                    yield return null;
                    at2 = Calc.Approach(at2, 1f, 2f * Engine.DeltaTime);
                    percent = Ease.SineIn(at2);
                    Vector2 vector = Vector2.Lerp(start, target, percent);
                    MoveTo(vector);
                }
                StartShaking(0.2f);
                Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                SceneAs<Level>().Shake();
                StopPlayerRunIntoAnimation = true;
                yield return 0.5f;
                StopPlayerRunIntoAnimation = false;
                at2 = 0f;
                while (at2 < 1f) {
                    yield return null;
                    at2 = Calc.Approach(at2, 1f, 0.5f * Engine.DeltaTime);
                    percent = 1f - Ease.SineIn(at2);
                    Vector2 position = Vector2.Lerp(target, start, Ease.SineIn(at2));
                    MoveTo(position);
                }
                StopPlayerRunIntoAnimation = true;
                StartShaking(0.2f);
                yield return 0.5f;
            }
        }
    }
}
