using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.CommunalHelper.Entities {
    class Segment {
        // start and end are the points at the tips of the segment, and Dif is the difference between end and start.
        public Vector2 Start, End, Dif;

        public Segment(Vector2 start, Vector2 end) {
            Start = start;
            End = end;
            Dif = end - start;
        }

        public Segment(Vector2 start, Vector2 end, Vector2 offset) : this(start + offset, end + offset) { }

        public void DebugDraw() {
            Draw.Line(Start, End, Color.Red);
        }
    }
}
