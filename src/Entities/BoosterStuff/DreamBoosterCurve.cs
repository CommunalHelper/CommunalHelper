using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Entities {
    public enum CurveMode {
        Quadratic = 2,
        Cubic = 3,
    }

    public class BakedCurve {
        public Vector2 this[float t] {
            get {
                int m = (int) mode;
                float percent = t % 1;
                int i = Math.Max(0, (int) Math.Floor(t) * m);
                if (t >= curveCount) {
                    percent++;
                    i -= m;
                }

                return mode == CurveMode.Cubic ?
                    GetCurvePoint(points[i], points[i + 1], points[i + 2], points[i + 3], percent) :
                    GetCurvePoint(points[i], points[i + 1], points[i + 2], percent);
            }
        }

        private readonly Vector2[] points;

        // t -> distance
        private readonly float[] lut;
        private readonly int resolution;

        private readonly CurveMode mode;
        private readonly int curveCount;
        public readonly float Length;

        public BakedCurve(Vector2[] nodes, CurveMode mode, int resolution) {
            this.mode = mode;
            this.resolution = resolution;

            int m = (int) mode;
            int l = nodes.Length - 1;
            int max = l - (l % m);
            curveCount = max / m;

            points = new Vector2[max + 1];
            for (int i = 0; i <= max; i++)
                points[i] = nodes[i];

            float step = 1f / resolution;

            lut = new float[curveCount * resolution + 1];

            Vector2 prev = points[0];
            lut[0] = 0f;
            int index = 0;
            if (mode == CurveMode.Cubic) {
                for (float t = step; t < curveCount; t += step) {
                    float percent = t % 1;
                    int i = (int) Math.Floor(t) * 3;

                    Vector2 p = GetCurvePoint(points[i], points[i + 1], points[i + 2], points[i + 3], percent);
                    lut[++index] = Length += Vector2.Distance(prev, p);
                    prev = p;
                }
                Length += Vector2.Distance(prev, points[points.Length - 1]);
            } else {
                for (float t = step; t < curveCount; t += step) {
                    float percent = t % 1;
                    int i = (int) Math.Floor(t) * 2;

                    Vector2 p = GetCurvePoint(points[i], points[i + 1], points[i + 2], percent);
                    lut[++index] = Length += Vector2.Distance(prev, p);
                    prev = p;
                }
                Length += Vector2.Distance(prev, points[points.Length - 1]);
            }
            lut[curveCount * resolution] = Length;
        }

        // quadratic bézier
        private Vector2 GetCurvePoint(Vector2 a, Vector2 b, Vector2 c, float t) {
            float t2 = t * t;
            float mt = 1 - t;
            float mt2 = mt * mt;

            return (mt2 * a) + (2 * mt * t * b) + (t2 * c);
        }

        // cubic bézier
        private Vector2 GetCurvePoint(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t) {
            float t2 = t * t;
            float t3 = t2 * t;
            float mt = 1 - t;
            float mt2 = mt * mt;
            float mt3 = mt2 * mt;

            return (mt3 * a) + (3 * mt2 * t * b) + (3 * mt * t2 * c) + (t3 * d);
        }

        public Vector2 GetByDistance(float distance) {
            distance = Calc.Clamp(distance, 0f, Length);
            float t = 0f;
            for (int i = 0; i < lut.Length; i++) {
                if (lut[i] == distance) {
                    t = i;
                    break;
                }

                if (lut[i] > distance) {
                    int j = i - 1;
                    float dA = lut[i - 1],
                          dJ = lut[i];
                    t = j + (i - j) * (distance - dA) / (dJ - dA);
                    break;
                }
            }

            return this[t / resolution];
        }
    }

    [CustomEntity("CommunalHelper/CurvedDreamBooster")]
    public class DreamBoosterCurve : DreamBooster {
        private readonly BakedCurve curve;
        private float travel = 0;

        public readonly Vector2 EndingSpeed;

        public DreamBoosterCurve(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.NodesWithPosition(offset), data.Enum<CurveMode>("curve"), !data.Bool("hidePath")) { }
        
        public DreamBoosterCurve(Vector2 position, Vector2[] nodes, CurveMode mode, bool showPath)
            : base(position, showPath) {
            curve = new BakedCurve(nodes, mode, 24);
            EndingSpeed = Calc.SafeNormalize(curve.GetByDistance(curve.Length) - curve.GetByDistance(curve.Length - 1f), 240);
        }

        public Vector2 Travel(out bool end) {
            travel += 240f * Engine.DeltaTime; // booster speed constant
            end = travel >= curve.Length;
            return curve.GetByDistance(travel);
        }

        protected override void OnPlayerEnter(Player player) {
            base.OnPlayerEnter(player);
            travel = 0f;
            player.Speed = Vector2.Zero;
            Console.WriteLine("yo");
        }

        public override void Render() {
            for (float d = 0f; d <= curve.Length; d += 8) {
                Draw.Point(curve.GetByDistance(d), Color.Red);
            }
            base.Render();
        }
    }
}
