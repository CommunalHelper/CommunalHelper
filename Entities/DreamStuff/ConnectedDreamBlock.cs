using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/ConnectedDreamBlock")]
    [TrackedAs(typeof(DreamBlock))]
    class ConnectedDreamBlock : DreamBlock {
        private struct SpaceJamTile {
            public int X, Y;
            public int[] Edges;
            public bool Exist;

            public SpaceJamTile(int x, int y, bool exist) {
                X = x;
                Y = y;
                Edges = new int[4];
                for (int i = 0; i < Edges.Length; i++)
                    Edges[i] = -1;

                Exist = exist;
            }

            public bool EdgeExist(Edges edge) {
                return Edges[(int) edge] != -1;
            }

            public bool TryGetEdge(Edges edge, out int result) {
                result = Edges[(int) edge];
                if (result != -1)
                    return true;
                return false;
            }

            public int this[Edges edge] {
                get { return Edges[(int) edge]; }
                set { Edges[(int) edge] = value; }
            }

        }

        private struct SpaceJamEdge {
            public SpaceJamEdge(Vector2 startV, Vector2 endV, float wobbleOff, bool flipNorm, Edges face) {
                start = startV;
                end = endV;
                wobbleOffset = wobbleOff;
                flipNormal = flipNorm;
                facing = face;
            }
            public Vector2 start, end;
            public float wobbleOffset;
            public bool flipNormal;
            public Edges facing;
        }

        private struct SpaceJamCorner {
            public bool
                upright, upleft, downright, downleft,
                inupright, inupleft, indownright, indownleft;

            public int x, y;

            public SpaceJamCorner(int x_, int y_, bool ur, bool ul, bool dr, bool dl, bool iur, bool iul, bool idr, bool idl) {
                x = x_; y = y_;
                upright = ur;
                upleft = ul;
                downright = dr;
                downleft = dl;
                inupright = iur;
                inupleft = iul;
                indownright = idr;
                indownleft = idl;
            }
        }

        private List<SpaceJamEdge> GroupEdges;
        private List<SpaceJamCorner> GroupCorners;
        private Rectangle GroupRect;

        enum Edges {
            North,
            East,
            South,
            West,
        }

        private static readonly Color activeLineColor = (Color) typeof(DreamBlock).GetField("activeLineColor", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        private static readonly Color disabledLineColor = (Color) typeof(DreamBlock).GetField("disabledLineColor", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        private static readonly Color activeBackColor = (Color) typeof(DreamBlock).GetField("activeBackColor", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        private static readonly Color disabledBackColor = (Color) typeof(DreamBlock).GetField("disabledBackColor", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

        private static readonly MethodInfo m_DreamBlock_LineAmplitude = typeof(DreamBlock).GetMethod("LineAmplitude", BindingFlags.NonPublic | BindingFlags.Instance);

        public Point GroupBoundsMin;
        public Point GroupBoundsMax;

        public bool FeatherMode;
        public bool OneUse;
        public bool DoubleRefill;

        private MTexture[] particleTextures;
        private MTexture[] featherTextures;
        private CustomDreamBlock.DreamParticle[] particles;

        private bool shattering = false;
        private float colorLerp = 0.0f;

        private float groupWidth;
        private float groupHeight;

        public bool HasGroup { get; private set; }

        public bool MasterOfGroup { get; private set; }

        private List<ConnectedDreamBlock> group;
        private ConnectedDreamBlock master;

        private DynData<DreamBlock> baseData;

        public ConnectedDreamBlock(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Width, data.Height, data.Bool("featherMode"), data.Bool("oneUse"), data.Bool("doubleRefill", false)) { }

        public ConnectedDreamBlock(Vector2 position, int width, int height, bool featherMode, bool oneUse, bool doubleRefill)
            : base(position, width, height, null, false, false) {
            baseData = new DynData<DreamBlock>(this);

            OneUse = oneUse;
            FeatherMode = featherMode;
            DoubleRefill = doubleRefill;
            SurfaceSoundIndex = 11;
            MTexture particleSheet = GFX.Game[DoubleRefill ? "objects/CommunalHelper/customDreamBlock/particles" : "objects/dreamblock/particles"];
            particleTextures = new MTexture[4]
            {
                particleSheet.GetSubtexture(14, 0, 7, 7),
                particleSheet.GetSubtexture(7, 0, 7, 7),
                particleSheet.GetSubtexture(0, 0, 7, 7),
                particleSheet.GetSubtexture(7, 0, 7, 7)
            };
            featherTextures = new MTexture[3];
            featherTextures[0] = GFX.Game["particles/CommunalHelper/featherBig"];
            featherTextures[1] = GFX.Game["particles/CommunalHelper/featherMedium"];
            featherTextures[2] = GFX.Game["particles/CommunalHelper/featherSmall"];
        }

        public override void Removed(Scene scene) {
            base.Removed(scene);
            Glitch.Value = 0f;
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);

            if (!HasGroup) {
                /* Setup group */
                MasterOfGroup = true;
                GroupBoundsMin = new Point((int) X, (int) Y);
                GroupBoundsMax = new Point((int) Right, (int) Bottom);
                group = new List<ConnectedDreamBlock>();
                GroupEdges = new List<SpaceJamEdge>();
                GroupCorners = new List<SpaceJamCorner>();
                AddToGroupAndFindChildren(this);
                SetupParticles();

                GroupRect = new Rectangle(
                        GroupBoundsMin.X,
                        GroupBoundsMin.Y,
                        GroupBoundsMax.X - GroupBoundsMin.X,
                        GroupBoundsMax.Y - GroupBoundsMin.Y);

                /* Setup Edges of the group */
                int groupTileW = (int) ((GroupBoundsMax.X - GroupBoundsMin.X) / 8.0f);
                int groupTileH = (int) ((GroupBoundsMax.Y - GroupBoundsMin.Y) / 8.0f);
                SpaceJamTile[,] tiles = new SpaceJamTile[(groupTileW + 2), (groupTileH + 2)];
                for (int x = 0; x < groupTileW + 2; x++) {
                    for (int y = 0; y < groupTileH + 2; y++) {
                        tiles[x, y] = new SpaceJamTile(x - 1, y - 1, TileHasGroupDreamBlock(x - 1, y - 1));
                    }
                }
                for (int x = 1; x < groupTileW + 1; x++) {
                    for (int y = 1; y < groupTileH + 1; y++) {
                        if (tiles[x, y].Exist)
                            AutoEdge(tiles, x, y);
                    }
                }

            }
        }

        private void AutoEdge(SpaceJamTile[,] tiles, int x, int y) {
            SpaceJamTile self = tiles[x, y];
            SpaceJamTile nNorth = tiles[x, y - 1];
            SpaceJamTile nEast = tiles[x + 1, y];
            SpaceJamTile nSouth = tiles[x, y + 1];
            SpaceJamTile nWest = tiles[x - 1, y];
            SpaceJamTile nSouthEast = tiles[x + 1, y + 1];
            SpaceJamTile nSouthWest = tiles[x - 1, y + 1];
            SpaceJamTile nNorthEast = tiles[x + 1, y - 1];
            SpaceJamTile nNorthWest = tiles[x - 1, y - 1];

            #region Corner stuff
            bool upright = !nNorth.Exist && !nEast.Exist;
            bool upleft = !nNorth.Exist && !nWest.Exist;
            bool downright = !nSouth.Exist && !nEast.Exist;
            bool downleft = !nSouth.Exist && !nWest.Exist;
            bool inupright = nNorth.Exist && nEast.Exist && !nNorthEast.Exist;
            bool inupleft = nNorth.Exist && nWest.Exist && !nNorthWest.Exist;
            bool indownright = nSouth.Exist && nEast.Exist && !nSouthEast.Exist;
            bool indownleft = nSouth.Exist && nWest.Exist && !nSouthWest.Exist;
            if(upright || upleft || downright || downleft || inupright || inupleft || indownright || indownleft) {
                GroupCorners.Add(new SpaceJamCorner(x - 1, y - 1, upright, upleft, downright, downleft, inupright, inupleft, indownright, indownleft));
            }
            #endregion

            if (!nNorth.Exist) {
                if (nWest.TryGetEdge(Edges.North, out int idx)) {
                    SpaceJamEdge edge = GroupEdges[idx];
                    edge.end.X += 8;
                    GroupEdges[idx] = new SpaceJamEdge(edge.start, edge.end, edge.wobbleOffset, edge.flipNormal, edge.facing);
                    self[Edges.North] = idx;
                } else {
                    SpaceJamEdge newEdge;
                    newEdge.start = TileToPoint(x - 1, y - 1);
                    newEdge.end = newEdge.start;
                    newEdge.end.X += 8;
                    newEdge.wobbleOffset = 0.0f;
                    newEdge.flipNormal = false;
                    newEdge.facing = Edges.North;
                    self[Edges.North] = GroupEdges.Count();
                    GroupEdges.Add(newEdge);
                }
            }

            if (!nEast.Exist) {
                if (nNorth.TryGetEdge(Edges.East, out int idx)) {
                    SpaceJamEdge edge = GroupEdges[idx];
                    edge.end.Y += (nSouth.Exist && nSouthEast.Exist) ? 9 : 8;
                    GroupEdges[idx] = new SpaceJamEdge(edge.start, edge.end, edge.wobbleOffset, edge.flipNormal, edge.facing);
                    self[Edges.East] = idx;
                } else {
                    SpaceJamEdge newEdge;
                    newEdge.start = TileToPoint(x, y - 1);
                    newEdge.end = newEdge.start;
                    newEdge.end.Y += (nSouth.Exist && nSouthEast.Exist) ? 9 : 8;
                    if (nNorth.Exist) {
                        newEdge.start.Y -= 1;
                    }
                    newEdge.wobbleOffset = 0.7f;
                    newEdge.flipNormal = false;
                    newEdge.facing = Edges.East;
                    self[Edges.East] = GroupEdges.Count();
                    GroupEdges.Add(newEdge);
                }
            }

            if (!nSouth.Exist) {
                if (nWest.TryGetEdge(Edges.South, out int idx)) {
                    SpaceJamEdge edge = GroupEdges[idx];
                    edge.end.X += 8;
                    GroupEdges[idx] = new SpaceJamEdge(edge.start, edge.end, edge.wobbleOffset, edge.flipNormal, edge.facing);
                    self[Edges.South] = idx;
                } else {
                    SpaceJamEdge newEdge;
                    newEdge.start = TileToPoint(x - 1, y);
                    newEdge.start.Y -= 1;
                    newEdge.end = newEdge.start;
                    newEdge.end.X += 8;
                    newEdge.wobbleOffset = 1.5f;
                    newEdge.flipNormal = true;
                    newEdge.facing = Edges.South;
                    self[Edges.South] = GroupEdges.Count();
                    GroupEdges.Add(newEdge);
                }
            }

            if (!nWest.Exist) {
                if (nNorth.TryGetEdge(Edges.West, out int idx)) {
                    SpaceJamEdge edge = GroupEdges[idx];
                    edge.end.Y += (nSouth.Exist && nSouthWest.Exist) ? 9 : 8;
                    GroupEdges[idx] = new SpaceJamEdge(edge.start, edge.end, edge.wobbleOffset, edge.flipNormal, edge.facing);
                    self[Edges.West] = idx;
                } else {
                    SpaceJamEdge newEdge;
                    newEdge.start = TileToPoint(x - 1, y - 1);
                    newEdge.start.X += 1;
                    newEdge.end = newEdge.start;
                    newEdge.end.Y += (nSouth.Exist && nSouthWest.Exist) ? 9 : 8;
                    if (nNorth.Exist) {
                        newEdge.start.Y -= 1;
                    }
                    newEdge.wobbleOffset = 2.5f;
                    newEdge.flipNormal = true;
                    newEdge.facing = Edges.West;
                    self[Edges.West] = GroupEdges.Count();
                    GroupEdges.Add(newEdge);
                }
            }
        }

        private void AddToGroupAndFindChildren(ConnectedDreamBlock from) {
            if (from.X < GroupBoundsMin.X) {
                GroupBoundsMin.X = (int) from.X;
            }
            if (from.Y < GroupBoundsMin.Y) {
                GroupBoundsMin.Y = (int) from.Y;
            }
            if (from.Right > GroupBoundsMax.X) {
                GroupBoundsMax.X = (int) from.Right;
            }
            if (from.Bottom > GroupBoundsMax.Y) {
                GroupBoundsMax.Y = (int) from.Bottom;
            }
            from.HasGroup = true;
            group.Add(from);
            if (from != this) {
                from.master = this;
            }
            foreach (DreamBlock block in Scene.Tracker.GetEntities<DreamBlock>()) {
                if (block is ConnectedDreamBlock) {
                    ConnectedDreamBlock connectedBlock = block as ConnectedDreamBlock;
                    if (!connectedBlock.HasGroup && connectedBlock.FeatherMode == from.FeatherMode && 
                        Scene.CollideCheck(new Rectangle((int) from.X, (int) from.Y, (int) from.Width, (int) from.Height), connectedBlock)) {
                        AddToGroupAndFindChildren(connectedBlock);
                    }
                }
            }
        }

        private void SetupParticles() {
            groupWidth = GroupBoundsMax.X - GroupBoundsMin.X;
            groupHeight = GroupBoundsMax.Y - GroupBoundsMin.Y;

            /* Setup particles, will be rendered by the Group Master */
            float countFactor = FeatherMode ? 0.5f : 0.7f;
            particles = new CustomDreamBlock.DreamParticle[(int) (groupWidth / 8f * (groupHeight / 8f) * 0.7f * countFactor)];
            for (int i = 0; i < particles.Length; i++) {
                particles[i].Position = new Vector2(Calc.Random.NextFloat(groupWidth), Calc.Random.NextFloat(groupHeight));
                particles[i].Layer = Calc.Random.Choose(0, 1, 1, 2, 2, 2);
                particles[i].TimeOffset = Calc.Random.NextFloat();

                if (baseData.Get<bool>("playerHasDreamDash")) {
                    if (DoubleRefill) {
                        switch (particles[i].Layer) {
                            case 0:
                                particles[i].Color = Calc.HexToColor("FFD1F9");
                                break;
                            case 1:
                                particles[i].Color = Calc.HexToColor("FC99FF");
                                break;
                            case 2:
                                particles[i].Color = Calc.HexToColor("E269D2");
                                break;
                        }
                    } else {
                        switch (particles[i].Layer) {
                            case 0:
                                particles[i].Color = Calc.Random.Choose(Calc.HexToColor("FFEF11"), Calc.HexToColor("FF00D0"), Calc.HexToColor("08a310"));
                                break;
                            case 1:
                                particles[i].Color = Calc.Random.Choose(Calc.HexToColor("5fcde4"), Calc.HexToColor("7fb25e"), Calc.HexToColor("E0564C"));
                                break;
                            case 2:
                                particles[i].Color = Calc.Random.Choose(Calc.HexToColor("5b6ee1"), Calc.HexToColor("CC3B3B"), Calc.HexToColor("7daa64"));
                                break;
                        }
                    }
                } else {
                    particles[i].Color = Color.LightGray * (0.5f + particles[i].Layer / 2f * 0.5f);
                }

                #region Feather particle stuff
                if (FeatherMode) {
                    particles[i].Speed = Calc.Random.Range(6f, 16f);
                    particles[i].Spin = Calc.Random.Range(8f, 12f) * 0.2f;
                    particles[i].RotationCounter = Calc.Random.NextAngle();
                    particles[i].MaxRotate = Calc.Random.Range(0.3f, 0.6f) * ((float) Math.PI / 2f);
                }
                #endregion
            }
        }

        // base.Update Modified by IL Hook
        public override void Update() {
            base.Update();
            if (MasterOfGroup && FeatherMode) {
                UpdateParticles();
            }
        }

        private void UpdateParticles() {
            if (baseData.Get<bool>("playerHasDreamDash")) {
                for (int i = 0; i < particles.Length; i++) {
                    particles[i].Position.Y += 0.5f * particles[i].Speed * GetLayerScaleFactor(particles[i].Layer) * Engine.DeltaTime;
                    particles[i].RotationCounter += particles[i].Spin * Engine.DeltaTime;
                }
            }
        }

        public override void Render() {
            Camera camera = SceneAs<Level>().Camera;
            Vector2 GroupPosition = new Vector2(GroupBoundsMin.X, GroupBoundsMin.Y);

            DynData<DreamBlock> data = new DynData<DreamBlock>(this);
            float whiteFill = data.Get<float>("whiteFill");
            float whiteHeight = data.Get<float>("whiteHeight");
            Vector2 shake = data.Get<Vector2>("shake");

            if (GroupRect.Right < camera.Left || GroupRect.Left > camera.Right || GroupRect.Bottom < camera.Top || GroupRect.Top > camera.Bottom) {
                return;
            }

            if (MasterOfGroup) {
                Color lineColor = baseData.Get<bool>("playerHasDreamDash") ? activeLineColor : disabledLineColor;
                Color backColor = Color.Lerp(baseData.Get<bool>("playerHasDreamDash") ? activeBackColor : disabledBackColor, Color.White, colorLerp);

                if (whiteFill > 0f) {
                    lineColor = Color.Lerp(lineColor, Color.White, whiteFill);
                    if (whiteHeight == 1f)
                        backColor = Color.Lerp(backColor, Color.White, whiteFill);
                }

                #region Background rendering
                Vector2 cameraPositon = SceneAs<Level>().Camera.Position;
                foreach (ConnectedDreamBlock e in group) {
                    if (e.Right < camera.Left || e.Left > camera.Right || e.Bottom < camera.Top || e.Top > camera.Bottom) {
                        continue;
                    }
                    Draw.Rect(e.Position + shake, e.Width, e.Height, backColor);
                }
                #endregion

                #region Particle rendering
                for (int i = 0; i < particles.Length; i++) {
                    CustomDreamBlock.DreamParticle particle = particles[i];
                    int layer = particle.Layer;
                    Vector2 position = particle.Position + cameraPositon * (0.3f + 0.25f * layer);
                    float rotation = 1.5707963705062866f - 0.8f + (float) Math.Sin(particle.RotationCounter * particle.MaxRotate);
                    if (FeatherMode) {
                        position += Calc.AngleToVector(rotation, 4f);
                    }
                    position = PutInside(position, GroupRect);

                    if (!CheckParticleCollide(position))
                        continue;

                    Color color = Color.Lerp(particle.Color, Color.Black, colorLerp);
                    if (whiteFill > 0f && whiteHeight == 1f)
                        color = Color.Lerp(color, Color.White, whiteFill);

                    if (FeatherMode) {
                        featherTextures[layer].DrawCentered(position + Shake + shake, color, 1, rotation);
                    } else {
                        MTexture particleTexture;
                        switch (layer) {
                            case 0: {
                                int index = (int) ((particle.TimeOffset * 4f + baseData.Get<float>("animTimer")) % 4f);
                                particleTexture = particleTextures[3 - index];
                                break;
                            }
                            case 1: {
                                int index = (int) ((particle.TimeOffset * 2f + baseData.Get<float>("animTimer")) % 2f);
                                particleTexture = particleTextures[1 + index];
                                break;
                            }
                            default:
                                particleTexture = particleTextures[2];
                                break;
                        }
                        particleTexture.DrawCentered(position + Shake + shake, color);
                    }
                }
                #endregion

                #region (De)activation Rendering
                if (whiteFill == 1f && whiteHeight < 1f) {
                    float whiteFillBottom = GroupRect.Y + GroupRect.Height * whiteHeight;
                    foreach (ConnectedDreamBlock block in group) {
                        if (block.Right < camera.Left || block.Left > camera.Right || block.Bottom < camera.Top || block.Top > camera.Bottom) {
                            continue;
                        }
                        if (block.Top <= whiteFillBottom)
                            Draw.Rect(block.Position + shake, block.Width, Calc.Clamp(whiteFillBottom - block.Y, 1f, block.Height), Color.White);
                    }
                }
                #endregion

                #region Edge & Corner Rendering

                if (whiteFill > 0f && whiteHeight < 1f) {
                    backColor = Color.Lerp(backColor, Color.White, whiteFill);
                }

                foreach (SpaceJamCorner corner in GroupCorners) {
                    // Yes.
                    RenderCorner(GroupPosition + shake, corner, lineColor, backColor);
                }

                foreach (SpaceJamEdge edge in GroupEdges) {
                    Vector2 start = edge.start, end = edge.end;
                    if (edge.flipNormal) {
                        start = edge.end;
                        end = edge.start;

                        if (start.X == end.X) {
                            start.X -= 1;
                            end.X -= 1;
                        }
                        if (start.Y == end.Y) {
                            start.Y += 1;
                            end.Y += 1;
                        }
                    }
                    WobbleLine(start + shake, end + shake, edge.wobbleOffset, lineColor, backColor);
                }

                #endregion
            }
        }

        private void RenderCorner(Vector2 position, SpaceJamCorner corner, Color line, Color back) {
            int x = (int)(corner.x * 8 + position.X);
            int y = (int) (corner.y * 8 + position.Y);

            // Simple corners:
            if (corner.upright)
                Draw.Rect(x + 6, y, 2, 2, line);
            if (corner.upleft)
                Draw.Rect(x, y, 2, 2, line);
            if (corner.downright)
                Draw.Rect(x + 6, y + 6, 2, 2, line);
            if (corner.downleft)
                Draw.Rect(x, y + 6, 2, 2, line);

            // Inner corners:
            if (corner.inupright) {
                Draw.Rect(x + 6, y, 4, 3, back);
                Draw.Rect(x + 5, y - 1, 3, 3, back);
                Draw.Line(x + 7, y, x + 10, y, line);
                Draw.Line(x + 7, y, x + 7, y - 1, line);
            }
            if (corner.inupleft) {
                Draw.Rect(x - 2, y, 4, 3, back);
                Draw.Rect(x, y - 1, 3, 3, back);
                Draw.Line(x - 2, y, x, y, line);
                Draw.Line(x, y + 1, x, y - 1, line);
            }
            if (corner.indownright) {
                Draw.Rect(x + 6, y + 5, 4, 3, back);
                Draw.Rect(x + 5, y + 6, 3, 3, back);
                Draw.Line(x + 7, y + 7, x + 10, y + 7, line);
                Draw.Line(x + 8, y + 8, x + 8, y + 9, line);
            }
            if (corner.indownleft) {
                Draw.Rect(x - 2, y + 5, 4, 3, back);
                Draw.Rect(x, y + 6, 3, 3, back);
                Draw.Line(x - 2, y + 7, x + 1, y + 7, line);
                Draw.Line(x + 1, y + 8, x + 1, y + 9, line);
            }
        }

        private void WobbleLine(Vector2 from, Vector2 to, float offset, Color line, Color back) {
            Vector2 vec = to - from;
            float num = vec.Length();
            Vector2 value = Vector2.Normalize(vec);
            Vector2 vector = new Vector2(value.Y, 0f - value.X);

            float scaleFactor = 0f;
            int num2 = 16;
            for (int i = 2; i < num - 2; i += num2) {
                float num3 = MathHelper.Lerp((float) m_DreamBlock_LineAmplitude.Invoke(this, new object[] { baseData.Get<float>("wobbleFrom") + offset, i }), 
                    (float) m_DreamBlock_LineAmplitude.Invoke(this, new object[] { baseData.Get<float>("wobbleTo") + offset, i }), 
                    baseData.Get<float>("wobbleEase"));
                if (i + num2 >= num) {
                    num3 = 0f;
                }
                float num4 = Math.Min(num2, num - 2f - i);
                Vector2 vector2 = from + value * i + vector * scaleFactor;
                Vector2 vector3 = from + value * (i + num4) + vector * num3;
                Draw.Line(vector2 - vector, vector3 - vector, back);
                Draw.Line(vector2 - vector * 2f, vector3 - vector * 2f, back);
                //Draw.Line(vector2 - vector * 3f, vector3 - vector * 3f, back);
                Draw.Line(vector2, vector3, line);
                scaleFactor = num3;
            }
        }

        private void SpawnFastRoutineParticles() {
            if (MasterOfGroup) {
                Level level = SceneAs<Level>();
                foreach (SpaceJamEdge edge in GroupEdges) {
                    float width = edge.end.X - edge.start.X;
                    float centerH = edge.start.X + width / 2f;
                    float height = edge.end.Y - edge.start.Y;
                    float centerV = edge.start.Y + height / 2f;
                    if (edge.facing == Edges.North)
                        level.ParticlesFG.Emit(Strawberry.P_WingsBurst, (int) width, new Vector2(centerH, edge.start.Y), Vector2.UnitX * width / 2f, Color.White, (float) Math.PI);
                    if (edge.facing == Edges.South)
                        level.ParticlesFG.Emit(Strawberry.P_WingsBurst, (int) width, new Vector2(centerH, edge.end.Y), Vector2.UnitX * width / 2f, Color.White, 0f);
                    if (edge.facing == Edges.West)
                        level.ParticlesFG.Emit(Strawberry.P_WingsBurst, (int) height, new Vector2(edge.start.X, centerV), Vector2.UnitY * height / 2f, Color.White, 4.712389f);
                    if (edge.facing == Edges.East)
                        level.ParticlesFG.Emit(Strawberry.P_WingsBurst, (int) height, new Vector2(edge.end.X, centerV), Vector2.UnitY * height / 2f, Color.White, (float) Math.PI / 2f);
                }
            }
        }

        private void SpawnSlowRoutineParticles() {
            if(MasterOfGroup) {
                Level level = SceneAs<Level>();
                Camera camera = level.Camera;

                float whiteHeight = new DynData<DreamBlock>(this).Get<float>("whiteHeight");
                float whiteFillBottom = GroupRect.Y + GroupRect.Height * whiteHeight;

                foreach (ConnectedDreamBlock block in group) {
                    if (block.Right < camera.Left || block.Left > camera.Right || block.Bottom < camera.Top || block.Top > camera.Bottom) {
                        continue;
                    }

                    if (block.Top <= whiteFillBottom && block.Bottom >= whiteFillBottom) {
                        for (int i = 0; i < block.Width; i += 4) {
                            level.ParticlesFG.Emit(Strawberry.P_WingsBurst, new Vector2(block.X + (float) i, whiteFillBottom + 1f));
                        }
                    }
                }
            }
        }


        private bool CheckParticleCollide(Vector2 position) {
            float offset = 2f;
            foreach (ConnectedDreamBlock block in group) {
                if (position.X >= block.X + offset && position.Y >= block.Y + offset && position.X < block.Right - offset && position.Y < block.Bottom - offset) {
                    return true;
                }
            }
            return false;
        }

        private float GetLayerScaleFactor(int layer) {
            return 1 / (0.3f + 0.25f * layer);
        }

        private bool TileHasGroupDreamBlock(int x, int y) {
            Rectangle rect = TileToRectangle(x, y);
            foreach (ConnectedDreamBlock block in group) {
                if (block.CollideRect(rect)) {
                    return true;
                }
            }
            return false;
        }

        private Rectangle TileToRectangle(int x, int y) {
            /* Relative to base.Position */
            Vector2 p = TileToPoint(x, y);
            return new Rectangle((int) p.X, (int) p.Y, 8, 8);
        }

        private Vector2 TileToPoint(int x, int y) {
            return new Vector2(GroupBoundsMin.X + x * 8, GroupBoundsMin.Y + y * 8);
        }

        public void ConnectedFootstepRipple(Vector2 position) {
            if (baseData.Get<bool>("playerHasDreamDash")) {
                ConnectedDreamBlock master = MasterOfGroup ? this : this.master;

                foreach (ConnectedDreamBlock block in master.group) {
                    DisplacementRenderer.Burst burst = (Scene as Level).Displacement.AddBurst(position, 0.5f, 0f, 40f);
                    burst.WorldClipCollider = block.Collider;
                    burst.WorldClipPadding = 1;
                }
            }
        }

        private void OneUseDestroy() {
            Collidable = (Visible = false);
            DisableStaticMovers();
        }

        public void BeginShatter() {
            if (!shattering) {
                shattering = true;
                Audio.Play(CustomSFX.game_connectedDreamBlock_dreamblock_shatter, Position);

                ConnectedDreamBlock master = MasterOfGroup ? this : this.master;
                foreach (ConnectedDreamBlock block in master.group) {
                    block.Add(new Coroutine(block.ShatterSequence()));
                }
            }
        }

        private IEnumerator ShatterSequence() {
            yield return 0.28f;
            while (colorLerp < 2.0f) {
                colorLerp += Engine.DeltaTime * 10.0f;
                yield return null;
            }
            colorLerp = 1.0f;
            yield return 0.05f;
            if (MasterOfGroup) {
                Level level = SceneAs<Level>();
                level.Shake(.65f);
                Vector2 camera = SceneAs<Level>().Camera.Position;
                Rectangle GroupRect = new Rectangle(
                    GroupBoundsMin.X,
                    GroupBoundsMin.Y,
                    GroupBoundsMax.X - GroupBoundsMin.X,
                    GroupBoundsMax.Y - GroupBoundsMin.Y);

                Vector2 centre = new Vector2(GroupRect.Center.X, GroupRect.Center.Y);
                for (int i = 0; i < particles.Length; i++) {
                    Vector2 position = particles[i].Position;
                    position += camera * (0.3f + 0.25f * particles[i].Layer);
                    position = PutInside(position, GroupRect);
                    bool inside = false;
                    foreach (ConnectedDreamBlock block in group) {
                        if (block.CollidePoint(position)) {
                            inside = true;
                            break;
                        }
                    }
                    if (!inside) {
                        continue;
                    }

                    Color flickerColor = Color.Lerp(particles[i].Color, Color.White, 0.6f);
                    ParticleType type = new ParticleType(Lightning.P_Shatter) {
                        ColorMode = ParticleType.ColorModes.Fade,
                        Color = particles[i].Color,
                        Color2 = flickerColor, //Color.Lerp(particles[i].Color, Color.White, 0.5f),
                        Source = FeatherMode ? featherTextures[particles[i].Layer] : particleTextures[2],
                        SpinMax = FeatherMode ? (float) Math.PI : 0,
                        RotationMode = FeatherMode ? ParticleType.RotationModes.Random : ParticleType.RotationModes.None,
                        Direction = (position - centre).Angle()
                    };
                    level.ParticlesFG.Emit(type, 1, position, Vector2.One * 3f);
                }
                foreach (ConnectedDreamBlock jam in group) {
                    jam.OneUseDestroy();
                }

                Glitch.Value = 0.22f;
                while (Glitch.Value > 0.0f) {
                    Glitch.Value -= 0.5f * Engine.DeltaTime;
                    yield return null;
                }
                Glitch.Value = 0.0f;
            }
            RemoveSelf();
        }

        private Vector2 PutInside(Vector2 pos, Rectangle r) {
            while (pos.X < r.X) {
                pos.X += r.Width;
            }
            while (pos.X > r.X + r.Width) {
                pos.X -= r.Width;
            }
            while (pos.Y < r.Y) {
                pos.Y += r.Height;
            }
            while (pos.Y > r.Y + r.Height) {
                pos.Y -= r.Height;
            }
            return pos;
        }

        #region Hooks

        private static Type DreamBlock_FastActivate = typeof(DreamBlock).GetNestedType("<FastActivate>d__13", BindingFlags.NonPublic);
        private static Type DreamBlock_FastDeactivate = typeof(DreamBlock).GetNestedType("<FastDeactivate>d__12", BindingFlags.NonPublic);
        private static ILHook DreamBlock_FastActivate_Hook, DreamBlock_FastDeactivate_Hook;

        private static Type DreamBlock_Activate = typeof(DreamBlock).GetNestedType("<Activate>d__34", BindingFlags.NonPublic);
        private static Type DreamBlock_Deactivate = typeof(DreamBlock).GetNestedType("<Deactivate>d__11", BindingFlags.NonPublic);
        private static ILHook DreamBlock_Activate_Hook, DreamBlock_Deactivate_Hook;

        private static FieldInfo DreamBlock_Routine_F_This;

        public static void Hook() {
            On.Celeste.Player.DreamDashBegin += Player_DreamDashBegin;
            On.Celeste.Player.DreamDashUpdate += Player_DreamDashUpdate;
            On.Celeste.DreamBlock.Setup += DreamBlock_Setup;
            On.Celeste.DreamBlock.OnPlayerExit += DreamBlock_OnPlayerExit;
            On.Celeste.DreamBlock.FootstepRipple += DreamBlock_FootstepRipple;

            DreamBlock_Routine_F_This = DreamBlock_FastActivate.GetField("<>4__this", BindingFlags.Public | BindingFlags.Instance);
            DreamBlock_FastActivate_Hook = new ILHook(DreamBlock_FastActivate.GetMethod("MoveNext", BindingFlags.NonPublic | BindingFlags.Instance), DreamBlockFastRoutine);
            
            DreamBlock_Routine_F_This = DreamBlock_FastDeactivate.GetField("<>4__this", BindingFlags.Public | BindingFlags.Instance);
            DreamBlock_FastDeactivate_Hook = new ILHook(DreamBlock_FastDeactivate.GetMethod("MoveNext", BindingFlags.NonPublic | BindingFlags.Instance), DreamBlockFastRoutine);

            DreamBlock_Routine_F_This = DreamBlock_Activate.GetField("<>4__this", BindingFlags.Public | BindingFlags.Instance);
            DreamBlock_Activate_Hook = new ILHook(DreamBlock_Activate.GetMethod("MoveNext", BindingFlags.NonPublic | BindingFlags.Instance), DreamBlockSlowRoutine);
            
            DreamBlock_Routine_F_This = DreamBlock_Deactivate.GetField("<>4__this", BindingFlags.Public | BindingFlags.Instance);
            DreamBlock_Deactivate_Hook = new ILHook(DreamBlock_Deactivate.GetMethod("MoveNext", BindingFlags.NonPublic | BindingFlags.Instance), DreamBlockSlowRoutine);
        }

        public static void Unhook() {
            On.Celeste.Player.DreamDashBegin -= Player_DreamDashBegin;
            On.Celeste.Player.DreamDashUpdate -= Player_DreamDashUpdate;
            On.Celeste.DreamBlock.Setup -= DreamBlock_Setup;
            On.Celeste.DreamBlock.OnPlayerExit -= DreamBlock_OnPlayerExit;
            On.Celeste.DreamBlock.FootstepRipple -= DreamBlock_FootstepRipple;

            DreamBlock_FastActivate_Hook.Dispose();
            DreamBlock_FastDeactivate_Hook.Dispose();
            DreamBlock_Activate_Hook.Dispose();
            DreamBlock_Deactivate_Hook.Dispose();
        }

        private static void DreamBlockSlowRoutine(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            cursor.GotoNext(instr => instr.OpCode == OpCodes.Ldfld && ((FieldReference) instr.Operand).Name.Contains("level"));
            cursor.GotoNext(MoveType.After, instr => instr.OpCode.ToShortOp() == OpCodes.Brfalse_S);
            object breakTarget = cursor.Prev.Operand;

            // Load DreamBlock object
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, DreamBlock_Routine_F_This);

            // Check is if ConnectedDreamBlock
            cursor.Emit(OpCodes.Isinst, typeof(ConnectedDreamBlock));
            // Skip if it isn't
            cursor.Emit(OpCodes.Brfalse_S, cursor.Next);

            // Load ConnectedDreamBlock object
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, DreamBlock_Routine_F_This);
            cursor.Emit(OpCodes.Castclass, typeof(ConnectedDreamBlock));

            cursor.EmitDelegate<Action<ConnectedDreamBlock>>(block => block.SpawnSlowRoutineParticles());

            // Skip regular particles;
            cursor.Emit(OpCodes.Br, breakTarget);
        }

        private static void DreamBlockFastRoutine(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            cursor.GotoNext(instr => instr.Next.OpCode == OpCodes.Ldfld && ((FieldReference) instr.Next.Operand).Name.Contains("level"));

            // Load DreamBlock object
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, DreamBlock_Routine_F_This);

            // Check is if ConnectedDreamBlock
            cursor.Emit(OpCodes.Isinst, typeof(ConnectedDreamBlock));
            // Skip if it isn't
            cursor.Emit(OpCodes.Brfalse_S, cursor.Next);

            // Load ConnectedDreamBlock object
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, DreamBlock_Routine_F_This);
            cursor.Emit(OpCodes.Castclass, typeof(ConnectedDreamBlock));

            cursor.EmitDelegate<Action<ConnectedDreamBlock>>(block => block.SpawnFastRoutineParticles());

            // Skip regular particles
            cursor.Emit(OpCodes.Br, il.Instrs.Last(instr => instr.Previous?.OpCode == OpCodes.Callvirt && ((MethodReference) instr.Previous?.Operand).Name == "Emit"));
        }

        private static void Player_DreamDashBegin(On.Celeste.Player.orig_DreamDashBegin orig, Player player) {
            orig(player);
            DynData<Player> playerData = getPlayerData(player);
            DreamBlock dreamBlock = playerData.Get<DreamBlock>("dreamBlock");
            if (dreamBlock is ConnectedDreamBlock && (dreamBlock as ConnectedDreamBlock).FeatherMode) {
                SoundSource dreamSfxLoop = playerData.Get<SoundSource>("dreamSfxLoop");
                player.Stop(dreamSfxLoop);
                player.Loop(dreamSfxLoop, "event:/CommunalHelperEvents/game/connectedDreamBlock/dreamblock_fly_travel");
            }

        }

        private static int Player_DreamDashUpdate(On.Celeste.Player.orig_DreamDashUpdate orig, Player player) {
            DynData<Player> playerData = getPlayerData(player);
            DreamBlock dreamBlock = playerData.Get<DreamBlock>("dreamBlock");
            if (dreamBlock is ConnectedDreamBlock && (dreamBlock as ConnectedDreamBlock).FeatherMode) {
                Vector2 input = Input.Aim.Value.SafeNormalize(Vector2.Zero);
                if (input != Vector2.Zero) {
                    Vector2 vector = player.Speed.SafeNormalize(Vector2.Zero);
                    if (vector != Vector2.Zero) {
                        vector = player.DashDir = Vector2.Dot(input, vector) != -0.8f ? vector.RotateTowards(input.Angle(), 5f * Engine.DeltaTime) : vector;
                        player.Speed = vector * 240f;
                    }
                }
            }
            return orig(player);
        }

        private static void DreamBlock_Setup(On.Celeste.DreamBlock.orig_Setup orig, DreamBlock self) {
            if (self is ConnectedDreamBlock block)
                block.SetupParticles();
            else
                orig(self);
        }

        private static void DreamBlock_OnPlayerExit(On.Celeste.DreamBlock.orig_OnPlayerExit orig, DreamBlock dreamBlock, Player player) {
            orig(dreamBlock, player);
            if (dreamBlock is ConnectedDreamBlock) {
                ConnectedDreamBlock customDreamBlock = dreamBlock as ConnectedDreamBlock;
                if (customDreamBlock.OneUse && customDreamBlock.Collidable) {
                    customDreamBlock.BeginShatter();
                }
                if (customDreamBlock.DoubleRefill) {
                    player.Dashes = 2;
                }
            }
        }

        private static void DreamBlock_FootstepRipple(On.Celeste.DreamBlock.orig_FootstepRipple orig, DreamBlock dreamBlock, Vector2 pos) {
            if (dreamBlock is ConnectedDreamBlock) {
                (dreamBlock as ConnectedDreamBlock).ConnectedFootstepRipple(pos);
            } else {
                orig(dreamBlock, pos);
            }
        }

        private static DynData<Player> getPlayerData(Player player) {
            return new DynData<Player>(player);
        }

        #endregion

    }
}
