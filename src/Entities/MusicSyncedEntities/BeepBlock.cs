using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/BeepBlock")]
    [Tracked]
    public class BeepBlock : Solid {
        private float fade;
        private float lightFade;

        public Color Color { get; private set; }

        private BeepBlockRenderer renderer;

        public BeepBlock(EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height, safe: false) {
            Add(new MusicSync(Tick));
            Add(new LightOcclude());
            Depth = -1;
        }

        public void Tick(int beat) {
            Level level = SceneAs<Level>();
            if (!level.Paused) {
                if (WillSwitch(beat)) {
                    fade = 1.5f;
                    level.Shake(0.2f);
                    StartShaking(0.1f);
                    Player p = GetPlayerRider();
                    if (p != null)
                        p.LiftSpeed += Vector2.UnitX * 240;
                } else if (WillSwitch(beat + 1) || WillSwitch(beat + 2) || WillSwitch(beat + 3)) {
                    fade = 0.5f;
                    StartShaking(0.1f);
                } else {
                    lightFade = 1f;
                }
            }
        }

        private bool WillSwitch(int beatAt) => (beatAt - 1) % 8 == 0;

        public override void Awake(Scene scene) {
            base.Awake(scene);
            renderer = scene.Tracker.GetEntity<BeepBlockRenderer>();
            renderer.Track(this);
        }

        public override void Removed(Scene scene) {
            base.Removed(scene);
            renderer.Untrack(this);
        }

        public override void Update() {
            base.Update();
            fade = Calc.Approach(fade, 0f, Engine.RawDeltaTime * 1.5f);
            lightFade = Calc.Approach(lightFade, 0f, Engine.RawDeltaTime * 4f);

            Color = Color.Lerp(Color.Lerp(Color.MidnightBlue, Color.Firebrick, fade), Color.White, lightFade * 0.1f);
        }
    }
}
