using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CommunalHelper.Entities {
    public interface IMultiNodeZipMover {
        Vector2[] Nodes { get; }
        float Percent { get; set;  }

        float Width { get; }
        float Height { get; }

        Color RopeColor { get; }
        Color RopeLightColor { get; }

        MTexture Cog { get; }
    }

    public class ZipMoverPathRenderer : Entity {
        private class Segment {
            public bool Seen { get; set; }

            private readonly Vector2 from, to;
            public Rectangle Bounds { get; }

            private readonly Vector2 sparkAdd;

            private readonly float sparkDirStartA, sparkDirStartB;
            private readonly float sparkDirEndA, sparkDirEndB;

            const float piOverEight = MathHelper.PiOver4 / 2f;
            const float eightPi = 4 * MathHelper.TwoPi;

            public Segment(Vector2 from, Vector2 to) {
                this.from = from;
                this.to = to;

                sparkAdd = (from - to).SafeNormalize(5f).Perpendicular();
                float angle = (from - to).Angle();
                sparkDirStartA = angle + piOverEight;
                sparkDirStartB = angle - piOverEight;
                sparkDirEndA = angle + MathHelper.Pi - piOverEight;
                sparkDirEndB = angle + MathHelper.Pi + piOverEight;

                Rectangle b = Util.Rectangle(from, to);
                b.Inflate(10, 10);

                Bounds = b;
            }

            public void Spark(Level level) {
                level.ParticlesBG.Emit(ZipMover.P_Sparks, from + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirStartA);
                level.ParticlesBG.Emit(ZipMover.P_Sparks, from - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirStartB);
                level.ParticlesBG.Emit(ZipMover.P_Sparks, to + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirEndA);
                level.ParticlesBG.Emit(ZipMover.P_Sparks, to - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirEndB);
            }

            public void DrawCogs(float percent, Vector2 offset, MTexture cog, Color rope, Color lightRope, Color cogColor) {
                Vector2 vector = (to - from).SafeNormalize();
                Vector2 value = vector.Perpendicular() * 3f;
                Vector2 value2 = -vector.Perpendicular() * 4f;

                float rotation = percent * MathHelper.TwoPi;
                Draw.Line(from + value + offset, to + value + offset, rope);
                Draw.Line(from + value2 + offset, to + value2 + offset, rope);

                for (float num = 4f - percent * eightPi % 4f; num < (to - from).Length(); num += 4f) {
                    Vector2 value3 = from + value + vector.Perpendicular() + vector * num;
                    Vector2 value4 = to + value2 - vector * num;
                    Draw.Line(value3 + offset, value3 + vector * 2f + offset, lightRope);
                    Draw.Line(value4 + offset, value4 - vector * 2f + offset, lightRope);
                }

                cog.DrawCentered(from + offset, cogColor, 1f, rotation);
                cog.DrawCentered(to + offset, cogColor, 1f, rotation);
            }
        }

        private readonly Rectangle bounds;
        private readonly Segment[] segments;

        private readonly IMultiNodeZipMover zipMover;

        private Level level;

        public ZipMoverPathRenderer(IMultiNodeZipMover zipMover, int depth = Depths.BGDecals) {
            this.zipMover = zipMover;

            Vector2 offset = new(zipMover.Width / 2f, zipMover.Height / 2f);

            Vector2 prev = zipMover.Nodes[0] + offset;
            Vector2 min = prev, max = prev;

            segments = new Segment[zipMover.Nodes.Length - 1];
            for (int i = 0; i < segments.Length; ++i) {
                Vector2 node = zipMover.Nodes[i + 1] + offset;
                segments[i] = new(prev, node);

                min = Util.Min(min, node);
                max = Util.Max(max, node);

                prev = node;
            }

            bounds = new((int) min.X, (int) min.Y, (int) (max.X - min.X), (int)(max.Y - min.Y));
            bounds.Inflate(10, 10);

            Depth = depth;
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            level = scene as Level;
        }

        public void CreateSparks() {
            foreach (Segment seg in segments)
                seg.Spark(level);  
        }

        public override void Render() {
            Rectangle cameraBounds = level.Camera.GetBounds();

            if (!cameraBounds.Intersects(bounds))
                return;

            foreach (Segment seg in segments)
                if (seg.Seen = cameraBounds.Intersects(seg.Bounds))
                    seg.DrawCogs(zipMover.Percent, Vector2.One, zipMover.Cog, Color.Black, Color.Black, Color.Black);

            foreach (Segment seg in segments)
                if (seg.Seen)
                    seg.DrawCogs(zipMover.Percent, Vector2.Zero, zipMover.Cog, zipMover.RopeColor, zipMover.RopeLightColor, Color.White);
        }
    }
}
