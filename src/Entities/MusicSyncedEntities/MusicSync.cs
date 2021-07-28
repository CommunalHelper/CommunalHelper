using Monocle;
using System;

namespace Celeste.Mod.CommunalHelper.Entities {
    [Tracked]
    public class MusicSync : Component {
        private Action<int> onTick;

        public MusicSync(Action<int> onTick)
            : base(true, false) {
            this.onTick = onTick;
        }

        public void Tick(int beat) {
            if (Active) {
                onTick(beat);
            }
        }
    }
}