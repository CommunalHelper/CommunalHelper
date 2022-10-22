using Celeste.Mod.CommunalHelper.DashStates;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Entities {
    [Tracked(false)]
    public class PlayerSeekerBarrierRenderer : Entity {
        private enum Tile {
            Empty, Normal, Spike,
        }

        private static readonly Point[] edgeBuildOffsets = new Point[] {
            new(0, -1), new(0, 1), new(-1, 0), new(1, 0),
        };

        private static readonly Color UncollidableColor = Color.Black * .85f;
        private static readonly Color CollidableColor   = Color.White * .20f;

        private class Edge {
            public PlayerSeekerBarrier Parent;

            public bool Visible;

            public Vector2 A, B;
            public Vector2 Min, Max;
            public Vector2 Normal, Perpendicular;

            public float[] Wave;
            public float Length;

            public readonly bool spiky;

            public Edge(PlayerSeekerBarrier parent, Vector2 a, Vector2 b) {
                Parent = parent;
                Visible = true;
                A = a;
                B = b;
                Min = new Vector2(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));
                Max = new Vector2(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
                Normal = (b - a).SafeNormalize();
                Perpendicular = -Normal.Perpendicular();
                Length = (a - b).Length();
                spiky = parent.Spiky;
            }

            public void UpdateWave(float time) {
                if (Wave == null || Wave.Length <= Length)
                    Wave = new float[(int) Length + 2];

                for (int i = 0; i <= Length; i++)
                    Wave[i] = (spiky ? GetSpikyWaveAt(time, i, Length) : GetWaveAt(time, i, Length));
            }

            private float GetWaveAt(float offset, float along, float length) {
                if (along <= 1f || along >= length - 1f)
                    return 0f;

                if (Parent.Solidify >= 1f)
                    return 0f;

                float num = offset + along * 0.25f;
                float num2 = (float) (Math.Sin(num) * 2.0 + Math.Sin(num * 0.25f));
                return (1f + num2 * Ease.SineInOut(Calc.YoYo(along / length))) * (1f - Parent.Solidify);
            }

            private float GetSpikyWaveAt(float offset, float along, float length) {
                if (along <= 1f || along >= length - 1f)
                    return 0f;

                if (Parent.Solidify >= 1f)
                    return 0f;

                float intensity = (1f - Parent.Solidify);
                float at = offset + along * 0.5f;
                return Util.MappedTriangleWave(at * 0.625f, 0, 7) * Util.PowerBounce(along / length, 2.5f) * intensity;
            }

            public bool InView(ref Rectangle view)
                => view.Left < Parent.X + Max.X &&
                   view.Right > Parent.X + Min.X &&
                   view.Top < Parent.Y + Max.Y &&
                   view.Bottom > Parent.Y + Min.Y;
        }

        private List<PlayerSeekerBarrier> list = new();
        private List<Edge> edges = new();
        private VirtualMap<Tile> tiles;

        private Rectangle levelTileBounds;

        private bool dirty;

        private float lerp = 0f;
        private Color color = UncollidableColor;

        public PlayerSeekerBarrierRenderer() {
            Tag = (int) Tags.Global | (int) Tags.TransitionUpdate;
            Depth = Depths.Player;
            Add(new CustomBloom(OnRenderBloom));
        }

        public void Track(PlayerSeekerBarrier block) {
            list.Add(block);

            if (tiles == null) {
                levelTileBounds = (Scene as Level).TileBounds;
                tiles = new VirtualMap<Tile>(levelTileBounds.Width, levelTileBounds.Height, emptyValue: Tile.Empty);
            }

            Tile tile = block.Spiky ? Tile.Spike : Tile.Normal;
            for (int i = (int) block.X / 8; i < block.Right / 8f; i++)
                for (int j = (int) block.Y / 8; j < block.Bottom / 8f; j++)
                    tiles[i - levelTileBounds.X, j - levelTileBounds.Y] = tile;

            dirty = true;
        }

        public void Untrack(PlayerSeekerBarrier block) {
            list.Remove(block);
            if (list.Count <= 0)
                tiles = null;
            else
                for (int i = (int) block.X / 8; i < block.Right / 8f; i++)
                    for (int j = (int) block.Y / 8; j < block.Bottom / 8f; j++)
                        tiles[i - levelTileBounds.X, j - levelTileBounds.Y] = Tile.Empty;
            dirty = true;
        }

        public override void Update() {
            if (dirty)
                RebuildEdges();
            UpdateEdges();

            bool collidable = SeekerDash.HasSeekerDash || SeekerDash.SeekerAttacking;

            lerp = Calc.Approach(lerp, collidable ? 1 : 0, Engine.DeltaTime * (collidable ? 10f : 4f));
            color = Color.Lerp(UncollidableColor, CollidableColor, lerp);
        }

        public void UpdateEdges() {
            Camera camera = (Scene as Level).Camera;
            Rectangle view = new Rectangle((int) camera.Left - 4, (int) camera.Top - 4, (int) (camera.Right - camera.Left) + 8, (int) (camera.Bottom - camera.Top) + 8);
            
            for (int i = 0; i < edges.Count; i++) {
                if (edges[i].Visible) {
                    if (Scene.OnInterval(0.25f, i * 0.01f) && !edges[i].InView(ref view))
                        edges[i].Visible = false;
                } else if (Scene.OnInterval(0.05f, i * 0.01f) && edges[i].InView(ref view))
                    edges[i].Visible = true;

                if (edges[i].Visible && (Scene.OnInterval(0.05f, i * 0.01f) || edges[i].Wave == null))
                    edges[i].UpdateWave(Scene.TimeActive * 3f);
            }
        }

        private void RebuildEdges() {
            dirty = false;
            edges.Clear();
            if (list.Count <= 0)
                return;

            foreach (PlayerSeekerBarrier barrier in list) {
                for (int i = (int) barrier.X / 8; i < barrier.Right / 8f; i++) {
                    for (int j = (int) barrier.Y / 8; j < barrier.Bottom / 8f; j++) {
                        for (int k = 0; k < edgeBuildOffsets.Length; k++) {
                            Point offset = edgeBuildOffsets[k];
                            Point perp = new Point(-offset.Y, offset.X);

                            Tile current = At(i, j);
                            Tile front = At(i + offset.X, j + offset.Y);
                            Tile side = At(i - perp.X, j - perp.Y);
                            Tile diag = At(i + offset.X - perp.X, j + offset.Y - perp.Y);

                            /*
                             * If the front tile is empty, we are located at the beginning of a new edge if either of these is true:
                             * 1: The side tile is empty, or there's an wall on the side.
                             * 2: The current tile is not empty, there's no wall on the side, and the side tile is occupied and of different type.
                             */

                            bool flag1 = side == Tile.Empty || diag != Tile.Empty;
                            bool flag2 = current != Tile.Empty && diag == Tile.Empty && side != Tile.Empty && side != current;

                            if (front == Tile.Empty && (flag1 || flag2)) {
                                Point at = new Point(i, j);
                                Point to = new Point(i + perp.X, j + perp.Y);
                                Vector2 value = new Vector2(4f) + new Vector2(offset.X - perp.X, offset.Y - perp.Y) * 4f;

                                Tile type = At(i, j);
                                Tile next = At(to.X, to.Y);
                                while (next == type && !Inside(to.X + offset.X, to.Y + offset.Y)) {
                                    next = At(to.X += perp.X, to.Y += perp.Y);
                                }

                                Vector2 a = new Vector2(at.X, at.Y) * 8f + value - barrier.Position;
                                Vector2 b = new Vector2(to.X, to.Y) * 8f + value - barrier.Position;
                                edges.Add(new(barrier, a, b));
                            }
                        }
                    }
                }
            }
        }

        private bool Inside(int tx, int ty) 
            => At(tx, ty) != Tile.Empty;

        private Tile At(int tx, int ty)
            => tiles[tx - levelTileBounds.X, ty - levelTileBounds.Y];

        private void OnRenderBloom() {
            foreach (PlayerSeekerBarrier item in list)
                if (item.Visible)
                    Draw.Rect(item.X, item.Y, item.Width, item.Height, Color.White);

            foreach (Edge edge in edges) {
                if (edge.Visible) {
                    Vector2 value = edge.Parent.Position + edge.A;
                    for (int i = 0; i <= edge.Length; i++) {
                        Vector2 at = value + edge.Normal * i;
                        Draw.Line(at, at + edge.Perpendicular * edge.Wave[i], Color.White);
                    }
                }
            }
        }

        public override void Render() {
            if (list.Count <= 0)
                return;

            foreach (PlayerSeekerBarrier barrier in list)
                if (barrier.Visible)
                    Draw.Rect(barrier.Collider, color);

            if (edges.Count <= 0)
                return;

            foreach (Edge edge in edges) {
                if (edge.Visible) {
                    Vector2 value2 = edge.Parent.Position + edge.A;
                    for (int i = 0; i <= edge.Length; i++) {
                        Vector2 vector = value2 + edge.Normal * i;
                        Draw.Line(vector, vector + edge.Perpendicular * edge.Wave[i], color);
                    }
                }
            }
        }

        #region Hooks

        internal static void Hook() {
            On.Celeste.LevelLoader.LoadingThread += LevelLoader_LoadingThread;
        }

        internal static void Unhook() {
            On.Celeste.LevelLoader.LoadingThread -= LevelLoader_LoadingThread;
        }

        private static void LevelLoader_LoadingThread(On.Celeste.LevelLoader.orig_LoadingThread orig, LevelLoader self) {
            self.Level.Add(new PlayerSeekerBarrierRenderer());
            orig(self);
        }

        #endregion
    }
}
