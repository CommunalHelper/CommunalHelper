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

        void DrawBlackBorder();
    }

    public class ZipMoverPathRenderer : Entity {
        private class Segment {
            public bool Seen { get; set; }

            private readonly Vector2 from, to;
            private readonly Vector2 dir, twodir, perp;
            private float length;

            private readonly Vector2 lineStartA, lineStartB; 
            private readonly Vector2 lineEndA, lineEndB; 

            public Rectangle Bounds { get; }

            private readonly Vector2 sparkAdd;

            private readonly float sparkDirStartA, sparkDirStartB;
            private readonly float sparkDirEndA, sparkDirEndB;

            const float piOverEight = MathHelper.PiOver4 / 2f;
            const float eightPi = 4 * MathHelper.TwoPi;

            public Segment(Vector2 from, Vector2 to) {
                this.from = from;
                this.to = to;

                dir = (to - from).SafeNormalize();
                twodir = 2 * dir;
                perp = dir.Perpendicular();
                length = Vector2.Distance(from, to);

                Vector2 threeperp = 3 * perp;
                Vector2 minusfourperp = -4 * perp;

                lineStartA = from + threeperp;
                lineStartB = from + minusfourperp;
                lineEndA = to + threeperp;
                lineEndB = to + minusfourperp;

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

            public void DrawCogs(float percent, MTexture cog, Color rope, Color lightRope) {
                float rotation = percent * MathHelper.TwoPi;
                Draw.Line(lineStartA, lineEndA, rope);
                Draw.Line(lineStartB, lineEndB, rope);

                for (float d = 4f - percent * eightPi % 4f; d < length; d += 4f) {
                    Vector2 teethA = lineStartA + perp + dir * d;
                    Vector2 teethB = lineEndB - dir * d;
                    Draw.Line(teethA, teethA + twodir, lightRope);
                    Draw.Line(teethB, teethB - twodir, lightRope);
                }

                cog.DrawCentered(from, Color.White, 1f, rotation);
                cog.DrawCentered(to, Color.White, 1f, rotation);
            }

            public void DrawShadow(float percent, MTexture cog) {
                float rotation = percent * MathHelper.TwoPi;

                Vector2 startA = lineStartA + Vector2.UnitY;
                Vector2 endB = lineEndB + Vector2.UnitY;

                Draw.Line(startA, lineEndA + Vector2.UnitY, Color.Black);
                Draw.Line(lineStartB + Vector2.UnitY, endB, Color.Black);

                for (float d = 4f - percent * eightPi % 4f; d < length; d += 4f) {
                    Vector2 teethA = startA + perp + dir * d;
                    Vector2 teethB = endB - dir * d;
                    Draw.Line(teethA, teethA + twodir, Color.Black);
                    Draw.Line(teethB, teethB - twodir, Color.Black);
                }

                cog.DrawCentered(from + Vector2.UnitY, Color.Black, 1f, rotation);
                cog.DrawCentered(to + Vector2.UnitY, Color.Black, 1f, rotation);
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
                    seg.DrawShadow(zipMover.Percent, zipMover.Cog);

            foreach (Segment seg in segments)
                if (seg.Seen)
                    seg.DrawCogs(zipMover.Percent, zipMover.Cog, zipMover.RopeColor, zipMover.RopeLightColor);

            zipMover.DrawBlackBorder();
        }
    }
}
