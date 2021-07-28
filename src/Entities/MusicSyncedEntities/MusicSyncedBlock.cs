using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/BeepBlock")]
    public class MusicSyncedBlock : Solid {
        private float alpha = 0f;

        private bool red;

        public MusicSyncedBlock(EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height, safe: false) {
            Add(new MusicSync(Tick));
        }

        public void Tick(int beat) {
            Level level = SceneAs<Level>();
            if (!level.Paused) {
                if (WillSwitch(beat)) {
                    red = !red;
                    Audio.Play(red ? SFX.game_09_switch_to_cold : SFX.game_09_switch_to_hot);
                    alpha = 1f;
                    level.Shake(0.2f);
                    StartShaking(0.1f);
                } else if (WillSwitch(beat + 1) || WillSwitch(beat + 2) || WillSwitch(beat + 3)) {
                    alpha = 1f;
                    Audio.Play(red ? SFX.game_gen_cassetteblock_switch_1 : SFX.game_gen_cassetteblock_switch_2);
                    StartShaking(0.1f);
                }
            }
        }

        private bool WillSwitch(int beatAt) => (beatAt - 1) % 8 == 0;

        public override void Update() {
            base.Update();
            alpha = Calc.Approach(alpha, 0f, Engine.RawDeltaTime * 1.5f);
        }

        public override void Render() {
            Vector2 position = Position;
            Position += Shake;

            base.Render();
            Color c = Color.Lerp(red ? Color.MediumVioletRed : Color.Teal, Color.Black, 1 - alpha);

            Draw.Rect(Position, Width, Height, c);

            Position = position;
        }
    }
}
