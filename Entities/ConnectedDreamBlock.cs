using System;
using System.Collections.Generic;
using System.Linq;
using Monocle;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using MonoMod.Utils;
using System.Collections;

namespace Celeste.Mod.CommunalHelper
{
    [CustomEntity("CommunalHelper/ConnectedDreamBlock")]
    [Tracked]

    class ConnectedDreamBlock : Solid
    {
        private struct DreamParticle
        {
            public Vector2 Position;
            public int Layer;
            public Color Color;
            public float TimeOffset;

            // Feather particle stuff
            public float Speed;
            public float Spin;
            public float MaxRotate;
            public float RotationCounter;
        }

        private struct SpaceJamTile
        {
            public SpaceJamTile(int x, int y, bool exist)
            {
                X = x; Y = y;
                Edges = new int[4];

                Edges[NORTH] = -1;
                Edges[EAST] = -1;
                Edges[SOUTH] = -1;
                Edges[WEST] = -1;

                Exist = exist;
            }
            public int X, Y;
            public int[] Edges;
            public bool Exist;

            public bool EdgeExist(int i)
            {
                return Edges[i] != -1;
            }
        }

        private struct SpaceJamEdge
        {
            public SpaceJamEdge(Vector2 startV, Vector2 endV, float wobbleOff, bool flipNorm)
            {
                start = startV;
                end = endV;
                wobbleOffset = wobbleOff;
                flipNormal = flipNorm;
            }
            public Vector2 start, end;
            public float wobbleOffset;
            public bool flipNormal;
        }

        private List<SpaceJamEdge> EdgePool;

        private static readonly int NORTH = 0;
        private static readonly int EAST = 1;
        private static readonly int SOUTH = 2;
        private static readonly int WEST = 3;

        private static readonly Color activeBackColor = Color.Black;
        private static readonly Color disabledBackColor = Calc.HexToColor("1f2e2d");
        private static readonly Color activeLineColor = Color.White;
        private static readonly Color disabledLineColor = Calc.HexToColor("6a8480");

        public float animTimer;

        private MTexture[] particleTextures;
        private MTexture[] petalTextures;
        private bool playerHasDreamDash;
        private DreamParticle[] particles;

        private float wobbleEase;
        private float wobbleFrom = Calc.Random.NextFloat((float)Math.PI * 2f);
        private float wobbleTo = Calc.Random.NextFloat((float)Math.PI * 2f);

        public Point GroupBoundsMin;
        public Point GroupBoundsMax;

        public bool starFlyControl;
        public bool canDreamDash = true;
        public bool oneUse;
        private float colorLerp = 0.0f;

        private float groupWidth, groupHeight;

        public bool HasGroup
        {
            get;
            private set;
        }

        public bool MasterOfGroup
        {
            get;
            private set;
        }

        private List<ConnectedDreamBlock> group;
        private ConnectedDreamBlock master;

        public ConnectedDreamBlock(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Width, data.Height, data.Bool("featherMode"), data.Bool("oneUse")) { }

        public ConnectedDreamBlock(Vector2 position, int width, int height, bool flyControl, bool useOnce)
            : base(position, width, height, safe: true)
        {
            base.Depth = -11000;
            oneUse = useOnce;
            starFlyControl = flyControl;
            SurfaceSoundIndex = 11;
            particleTextures = new MTexture[4]
            {
                GFX.Game["objects/dreamblock/particles"].GetSubtexture(14, 0, 7, 7),
                GFX.Game["objects/dreamblock/particles"].GetSubtexture(7, 0, 7, 7),
                GFX.Game["objects/dreamblock/particles"].GetSubtexture(0, 0, 7, 7),
                GFX.Game["objects/dreamblock/particles"].GetSubtexture(7, 0, 7, 7)
            };
            petalTextures = new MTexture[3];
            petalTextures[0] = GFX.Game["particles/CommunalHelper/petalBig"];
            petalTextures[1] = GFX.Game["particles/CommunalHelper/petalMedium"];
            petalTextures[2] = GFX.Game["particles/CommunalHelper/petalSmall"];
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            playerHasDreamDash = SceneAs<Level>().Session.Inventory.DreamDash;
            if (!playerHasDreamDash)
            {
                Add(new LightOcclude());
            }
        }

        public override void Awake(Scene scene)
        {
            base.Awake(scene);

            if (!HasGroup)
            {
                /* Setup group */
                MasterOfGroup = true;
                GroupBoundsMin = new Point((int)base.X, (int)base.Y);
                GroupBoundsMax = new Point((int)base.Right, (int)base.Bottom);
                group = new List<ConnectedDreamBlock>();
                EdgePool = new List<SpaceJamEdge>();
                AddToGroupAndFindChildren(this);
                SetupParticles();

                /* Setup Edges of the group */
                int groupTileW = (int)((GroupBoundsMax.X - GroupBoundsMin.X) / 8.0f);
                int groupTileH = (int)((GroupBoundsMax.Y - GroupBoundsMin.Y) / 8.0f);
                SpaceJamTile[,] tiles = new SpaceJamTile[(groupTileW + 2), (groupTileH + 2)];
                for (int x = 0; x < groupTileW + 2; x++)
                {
                    for (int y = 0; y < groupTileH + 2; y++)
                    {
                        tiles[x, y] = new SpaceJamTile(x - 1, y - 1, TileHasGroupSpaceJam(x - 1, y - 1));
                    }
                }
                for (int x = 1; x < groupTileW + 1; x++)
                {
                    for (int y = 1; y < groupTileH + 1; y++)
                    {
                        if (tiles[x, y].Exist) AutoEdge(tiles, x, y);
                    }
                }
            }
        }

        private void AutoEdge(SpaceJamTile[,] tiles, int x, int y)
        {
            SpaceJamTile self = tiles[x, y];
            SpaceJamTile nNorth = tiles[x, y - 1];
            SpaceJamTile nEast = tiles[x + 1, y];
            SpaceJamTile nSouth = tiles[x, y + 1];
            SpaceJamTile nWest = tiles[x - 1, y];
            SpaceJamTile nSouthEast = tiles[x + 1, y + 1];
            SpaceJamTile nSouthWest = tiles[x - 1, y + 1];

            if (!nNorth.Exist)
            {
                if (nWest.EdgeExist(NORTH))
                {
                    SpaceJamEdge e = EdgePool[nWest.Edges[NORTH]];
                    e.end.X += 8;
                    EdgePool[nWest.Edges[NORTH]] = new SpaceJamEdge(e.start, e.end, e.wobbleOffset, e.flipNormal);
                    self.Edges[NORTH] = nWest.Edges[NORTH];
                }
                else
                {
                    SpaceJamEdge newEdge;
                    newEdge.start = TileToPoint(x - 1, y - 1);
                    newEdge.end = newEdge.start;
                    newEdge.end.X += 8;
                    newEdge.wobbleOffset = 0.0f;
                    newEdge.flipNormal = false;
                    self.Edges[NORTH] = EdgePool.Count();
                    EdgePool.Add(newEdge);
                }
            }

            if (!nEast.Exist)
            {
                if (nNorth.EdgeExist(EAST))
                {
                    SpaceJamEdge e = EdgePool[nNorth.Edges[EAST]];
                    e.end.Y += (nSouth.Exist && nSouthEast.Exist) ? 9 : 8;
                    EdgePool[nNorth.Edges[EAST]] = new SpaceJamEdge(e.start, e.end, e.wobbleOffset, e.flipNormal);
                    self.Edges[EAST] = nNorth.Edges[EAST];
                }
                else
                {
                    SpaceJamEdge newEdge;
                    newEdge.start = TileToPoint(x, y - 1);
                    newEdge.end = newEdge.start;
                    newEdge.end.Y += (nSouth.Exist && nSouthEast.Exist) ? 9 : 8;
                    if (nNorth.Exist)
                    {
                        newEdge.start.Y -= 1;
                    }
                    newEdge.wobbleOffset = 0.7f;
                    newEdge.flipNormal = false;
                    self.Edges[EAST] = EdgePool.Count();
                    EdgePool.Add(newEdge);
                }
            }

            if (!nSouth.Exist)
            {
                if (nWest.EdgeExist(SOUTH))
                {
                    SpaceJamEdge e = EdgePool[nWest.Edges[SOUTH]];
                    e.end.X += 8;
                    EdgePool[nWest.Edges[SOUTH]] = new SpaceJamEdge(e.start, e.end, e.wobbleOffset, e.flipNormal);
                    self.Edges[SOUTH] = nWest.Edges[SOUTH];
                }
                else
                {
                    SpaceJamEdge newEdge;
                    newEdge.start = TileToPoint(x - 1, y);
                    newEdge.start.Y -= 1;
                    newEdge.end = newEdge.start;
                    newEdge.end.X += 8;
                    newEdge.wobbleOffset = 1.5f;
                    newEdge.flipNormal = true;
                    self.Edges[SOUTH] = EdgePool.Count();
                    EdgePool.Add(newEdge);
                }
            }

            if (!nWest.Exist)
            {
                if (nNorth.EdgeExist(WEST))
                {
                    SpaceJamEdge e = EdgePool[nNorth.Edges[WEST]];
                    e.end.Y += (nSouth.Exist && nSouthWest.Exist) ? 9 : 8;
                    EdgePool[nNorth.Edges[WEST]] = new SpaceJamEdge(e.start, e.end, e.wobbleOffset, e.flipNormal);
                    self.Edges[WEST] = nNorth.Edges[WEST];
                }
                else
                {
                    SpaceJamEdge newEdge;
                    newEdge.start = TileToPoint(x - 1, y - 1);
                    newEdge.start.X += 1;
                    newEdge.end = newEdge.start;
                    newEdge.end.Y += (nSouth.Exist && nSouthWest.Exist) ? 9 : 8;
                    if (nNorth.Exist)
                    {
                        newEdge.start.Y -= 1;
                    }
                    newEdge.wobbleOffset = 2.5f;
                    newEdge.flipNormal = true;
                    self.Edges[WEST] = EdgePool.Count();
                    EdgePool.Add(newEdge);
                }
            }
        }

        private void AddToGroupAndFindChildren(ConnectedDreamBlock from)
        {
            if (from.X < (float)GroupBoundsMin.X)
            {
                GroupBoundsMin.X = (int)from.X;
            }
            if (from.Y < (float)GroupBoundsMin.Y)
            {
                GroupBoundsMin.Y = (int)from.Y;
            }
            if (from.Right > (float)GroupBoundsMax.X)
            {
                GroupBoundsMax.X = (int)from.Right;
            }
            if (from.Bottom > (float)GroupBoundsMax.Y)
            {
                GroupBoundsMax.Y = (int)from.Bottom;
            }
            from.HasGroup = true;
            group.Add(from);
            if (from != this)
            {
                from.master = this;
            }
            foreach (ConnectedDreamBlock e in base.Scene.Tracker.GetEntities<ConnectedDreamBlock>())
            {
                if (!e.HasGroup && e.starFlyControl == from.starFlyControl && base.Scene.CollideCheck(new Rectangle((int)from.X, (int)from.Y, (int)from.Width, (int)from.Height), e))
                {
                    AddToGroupAndFindChildren(e);
                }
            }
        }

        private void SetupParticles()
        {
            groupWidth = GroupBoundsMax.X - GroupBoundsMin.X; 
            groupHeight = GroupBoundsMax.Y - GroupBoundsMin.Y;

            /* Setup particles, will be rendered by the Group Master */
            float countFactor = starFlyControl ? 0.5f : 0.7f;
            particles = new DreamParticle[(int)(groupWidth / 8f * (groupHeight / 8f) * 0.7f * countFactor)];
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].Position = new Vector2(Calc.Random.NextFloat(groupWidth), Calc.Random.NextFloat(groupHeight));
                particles[i].Layer = Calc.Random.Choose(0, 1, 1, 2, 2, 2);
                particles[i].TimeOffset = Calc.Random.NextFloat();

                if (playerHasDreamDash)
                {
                    switch (particles[i].Layer)
                    {
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
                } else {
                    particles[i].Color = Color.LightGray * (0.5f + (float)particles[i].Layer / 2f * 0.5f);
                }

                #region Feather particle stuff
                if (starFlyControl) {
                    particles[i].Speed = Calc.Random.Range(6f, 16f);
                    particles[i].Spin = Calc.Random.Range(8f, 12f) * 0.2f;
                    particles[i].RotationCounter = Calc.Random.NextAngle();
                    particles[i].MaxRotate = Calc.Random.Range(0.3f, 0.6f) * ((float)Math.PI / 2f);
                }
                #endregion
            }
        }

        public override void Update()
        {
            base.Update();
            if (playerHasDreamDash)
            {
                if (MasterOfGroup)
                {
                    animTimer += 6f * Engine.DeltaTime;

                    wobbleEase += Engine.DeltaTime * 2f;
                    if (wobbleEase > 1f)
                    {
                        wobbleEase = 0f;
                        wobbleFrom = wobbleTo;
                        wobbleTo = Calc.Random.NextFloat((float)Math.PI * 2f);
                    }

                    if (starFlyControl) {
                        UpdateParticles();
                    }
                }
                SurfaceSoundIndex = 12;
            }
        }

        private void UpdateParticles()
        {
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].Position.Y += 0.5f * particles[i].Speed * GetLayerScaleFactor(particles[i].Layer) * Engine.DeltaTime;
                particles[i].RotationCounter += particles[i].Spin * Engine.DeltaTime;
            }
        }

        public override void Render()
        {
            Camera camera = SceneAs<Level>().Camera;
            Rectangle groupRect = new Rectangle(
                    GroupBoundsMin.X,
                    GroupBoundsMin.Y,
                    GroupBoundsMax.X - GroupBoundsMin.X,
                    GroupBoundsMax.Y - GroupBoundsMin.Y);

            if (groupRect.Right < camera.Left || groupRect.Left > camera.Right || groupRect.Bottom < camera.Top || groupRect.Top > camera.Bottom)
            {
                return;
            }

            if (MasterOfGroup)
            {
                Color lineColor = Color.Lerp(playerHasDreamDash ? activeLineColor : disabledLineColor, Color.Black, colorLerp);
                Color backColor = Color.Lerp(playerHasDreamDash ? activeBackColor : disabledBackColor, Color.White, colorLerp);

                #region Background rendering
                Vector2 cameraPositon = SceneAs<Level>().Camera.Position;
                foreach (ConnectedDreamBlock e in group)
                {
                    if (e.Right < camera.Left || e.Left > camera.Right || e.Bottom < camera.Top || e.Top > camera.Bottom)
                    {
                        continue;
                    }
                    Draw.Rect(e.Collider, backColor);
                }
                #endregion

                #region Particlue rendering
                for (int i = 0; i < particles.Length; i++)
                {
                    DreamParticle particle = particles[i];
                    int layer = particle.Layer;
                    Vector2 position = particle.Position + cameraPositon * (0.3f + 0.25f * layer);
                    float rotation = 1.5707963705062866f - 0.8f + (float)Math.Sin(particle.RotationCounter * particle.MaxRotate);
                    if (starFlyControl) {
                        position += Calc.AngleToVector(rotation, 4f);
                    }
                    position = PutInside(position, groupRect);

                    if (!CheckGroupParticleCollide(position)) continue;

                    Color color = Color.Lerp(particle.Color, Color.Black, colorLerp);

                    if (starFlyControl)
                    {
                        petalTextures[layer].DrawCentered(position + Shake, color, 1, rotation);
                    } 
                    else
                    {
                        MTexture particleTexture;
                        switch (layer)
                        {
                            case 0:
                                {
                                    int index = (int)((particle.TimeOffset * 4f + animTimer) % 4f);
                                    particleTexture = particleTextures[3 - index];
                                    break;
                                }
                            case 1:
                                {
                                    int index = (int)((particle.TimeOffset * 2f + animTimer) % 2f);
                                    particleTexture = particleTextures[1 + index];
                                    break;
                                }
                            default:
                                particleTexture = particleTextures[2];
                                break;
                        }
                        particleTexture.DrawCentered(position + Shake, color);
                    }
                }
                #endregion

                #region Edge Rendering
                foreach (ConnectedDreamBlock e in group)
                {
                    if (e.Right < camera.Left || e.Left > camera.Right || e.Bottom < camera.Top || e.Top > camera.Bottom)
                    {
                        continue;
                    }
                }
                foreach (SpaceJamEdge edge in EdgePool)
                {
                    Draw.Line(edge.start, edge.end, lineColor);
                    Vector2 start, end;
                    if (edge.flipNormal)
                    {
                        start = edge.end;
                        end = edge.start;


                        if (start.X == end.X)
                        {
                            start.X -= 1;
                            end.X -= 1;
                        }
                        if (start.Y == end.Y)
                        {
                            start.Y += 1;
                            end.Y += 1;
                        }
                    }
                    else
                    {
                        start = edge.start;
                        end = edge.end;
                    }
                    WobbleLine(start, end, edge.wobbleOffset, lineColor, backColor);
                }
                #endregion
            }
        }

        private void WobbleLine(Vector2 from, Vector2 to, float offset, Color line, Color back)
        {
            float num = (to - from).Length();
            Vector2 value = Vector2.Normalize(to - from);
            Vector2 vector = new Vector2(value.Y, 0f - value.X);
            float scaleFactor = 0f;
            int num2 = 16;
            for (int i = 2; (float)i < num - 2f; i += num2)
            {
                float num3 = Lerp(LineAmplitude(wobbleFrom + offset, i), LineAmplitude(wobbleTo + offset, i), wobbleEase);
                if ((float)(i + num2) >= num)
                {
                    num3 = 0f;
                }
                float num4 = Math.Min(num2, num - 2f - (float)i);
                Vector2 vector2 = from + value * i + vector * scaleFactor;
                Vector2 vector3 = from + value * ((float)i + num4) + vector * num3;
                Draw.Line(vector2 - vector, vector3 - vector, back);
                Draw.Line(vector2 - vector * 2f, vector3 - vector * 2f, back);
                Draw.Line(vector2 - vector * 3f, vector3 - vector * 3f, back);
                Draw.Line(vector2, vector3, line);
                scaleFactor = num3;
            }
        }

        private float LineAmplitude(float seed, float index)
        {
            return (float)(Math.Sin((double)(seed + index / 16f) + Math.Sin(seed * 2f + index / 32f) * 6.2831854820251465) + 1.0) * 1.5f;
        }

        private float Lerp(float a, float b, float percent)
        {
            return a + (b - a) * percent;
        }

        private bool CheckGroupParticleCollide(Vector2 position)
        {
            float offset = 2f;
            foreach (ConnectedDreamBlock e in group)
            {
                if (position.X >= e.X + offset && position.Y >= e.Y + offset && position.X < e.Right - offset && position.Y < e.Bottom - offset)
                {
                    return true;
                }
            }
            return false;
        }

        private float GetLayerScaleFactor(int layer) {
            return 1/ (0.3f + 0.25f * layer);
        }

        private bool TileHasGroupSpaceJam(int x, int y)
        {
            Rectangle r = TileToRectangle(x, y);
            foreach (ConnectedDreamBlock j in group)
            {
                if (j.CollideRect(r))
                {
                    return true;
                }
            }
            return false;
        }

        private Rectangle TileToRectangle(int x, int y)
        {
            /* Relative to base.Position */
            Vector2 p = TileToPoint(x, y);
            return new Rectangle((int)p.X, (int)p.Y, 8, 8);
        }

        private Vector2 TileToPoint(int x, int y)
        {
            return new Vector2((int)GroupBoundsMin.X + x * 8, (int)GroupBoundsMin.Y + y * 8);
        }

        public void FootstepRipple(Vector2 position)
        {
            if (playerHasDreamDash)
            {
                ConnectedDreamBlock mastr = MasterOfGroup ? this : master;

                foreach (ConnectedDreamBlock e in mastr.group)
                {
                    DisplacementRenderer.Burst burst = (base.Scene as Level).Displacement.AddBurst(position, 0.5f, 0f, 40f);
                    burst.WorldClipCollider = e.Collider;
                    burst.WorldClipPadding = 1;
                }
            }
        }

        public static void CreateTrail(Player player)
        {
            Vector2 scale = new Vector2(Math.Abs(player.Sprite.Scale.X) * (float)player.Facing, player.Sprite.Scale.Y);
            TrailManager.Add(player, scale, player.GetCurrentTrailColor(), 1f);
        }

        public void OnPlayerExit(Player player)
        {
            Dust.Burst(player.Position, player.Speed.Angle(), 16, null);
            Vector2 value = Vector2.Zero;
            if (CollideCheck(player, Position + Vector2.UnitX * 4f))
            {
                value = Vector2.UnitX;
            }
            else if (CollideCheck(player, Position - Vector2.UnitX * 4f))
            {
                value = -Vector2.UnitX;
            }
            else if (CollideCheck(player, Position + Vector2.UnitY * 4f))
            {
                value = Vector2.UnitY;
            }
            else if (CollideCheck(player, Position - Vector2.UnitY * 4f))
            {
                value = -Vector2.UnitY;
            }
            _ = (value != Vector2.Zero);
        }

        private void OneUseDestroy()
        {
            Collidable = (Visible = false);
            DisableStaticMovers();
        }

        public void BeginShatter()
        {
            ConnectedDreamBlock mastr = MasterOfGroup ? this : master;
            foreach (ConnectedDreamBlock jam in mastr.group)
            {
                jam.Add(new Coroutine(jam.ShatterSeq()));
                jam.canDreamDash = false;
            }
        }

        private IEnumerator ShatterSeq()
        {
            yield return 0.28f;
            while (colorLerp < 2.0f)
            {
                colorLerp += Engine.DeltaTime * 10.0f;
                yield return null;
            }
            colorLerp = 1.0f;
            yield return 0.05f;
            if (MasterOfGroup)
            {
                Level level = SceneAs<Level>();
                level.Shake(.65f);
                Vector2 camera = SceneAs<Level>().Camera.Position;
                Rectangle groupRect = new Rectangle(
                    GroupBoundsMin.X,
                    GroupBoundsMin.Y,
                    GroupBoundsMax.X - GroupBoundsMin.X,
                    GroupBoundsMax.Y - GroupBoundsMin.Y);

                for (int i = 0; i < particles.Length; i++)
                {
                    Vector2 position = particles[i].Position;
                    position += camera * (0.3f + 0.25f * particles[i].Layer);
                    position = PutInside(position, groupRect);
                    if (!Scene.CollideCheck<ConnectedDreamBlock>(position))
                    {
                        continue;
                    }
                    ParticleType type = new ParticleType(Lightning.P_Shatter)
                    {
                        Color = particles[i].Color,
                        Color2 = Color.Lerp(particles[i].Color, Color.White, 0.5f)
                    };
                    level.ParticlesFG.Emit(type, 1, position, Vector2.One * 3f);
                }
                foreach (ConnectedDreamBlock jam in group)
                {
                    jam.OneUseDestroy();
                }

                Glitch.Value = 0.22f;
                while (Glitch.Value > 0.0f)
                {
                    Glitch.Value -= 0.5f * Engine.DeltaTime;
                    yield return null;
                }
                Glitch.Value = 0.0f;
            }
            RemoveSelf();
        }

        private Vector2 PutInside(Vector2 pos, Rectangle r)
        {
            while (pos.X < r.X)
            {
                pos.X += r.Width;
            }
            while (pos.X > r.X + r.Width)
            {
                pos.X -= r.Width;
            }
            while (pos.Y < r.Y)
            {
                pos.Y += r.Height;
            }
            while (pos.Y > r.Y + r.Height)
            {
                pos.Y -= r.Height;
            }
            return pos;
        }
    }

    class ConnectedDreamBlockHooks
    {
        private static int ConnectedDreamBlockState;
        private static ConnectedDreamBlock connectedDreamBlock;


        /* Connected Space Jam Behavior & Changes */
        private static bool DreamDashCheck(Player player, Vector2 dir)
        {
            DynData<Player> data = new DynData<Player>(player);
            bool flag = player.Inventory.DreamDash && player.DashAttacking && (dir.X == (float)Math.Sign(player.DashDir.X) || dir.Y == (float)Math.Sign(player.DashDir.Y));
            if (flag)
            {
                ConnectedDreamBlock dreamBlock = player.CollideFirst<ConnectedDreamBlock>(player.Position + dir);
                bool flag2 = dreamBlock != null;

                if (flag2)
                {
                    if (!dreamBlock.canDreamDash) return false;

                    bool flag3 = player.CollideCheck<Solid, ConnectedDreamBlock>(player.Position + dir);
                    if (flag3)
                    {
                        Vector2 value = new Vector2(Math.Abs(dir.Y), Math.Abs(dir.X));
                        bool flag4 = dir.X != 0f;
                        bool flag5;
                        bool flag6;
                        if (flag4)
                        {
                            flag5 = (player.Speed.Y <= 0f);
                            flag6 = (player.Speed.Y >= 0f);
                        }
                        else
                        {
                            flag5 = (player.Speed.X <= 0f);
                            flag6 = (player.Speed.X >= 0f);
                        }
                        if (flag5)
                        {
                            for (int i = -1; i >= -4; i--)
                            {
                                Vector2 at = player.Position + dir + value * (float)i;
                                bool flag8 = !player.CollideCheck<Solid, ConnectedDreamBlock>(at);
                                if (flag8)
                                {
                                    player.Position += value * (float)i;
                                    connectedDreamBlock = dreamBlock;
                                    return true;
                                }
                            }
                        }
                        if (flag6)
                        {
                            for (int j = 1; j <= 4; j++)
                            {
                                Vector2 at2 = player.Position + dir + value * (float)j;
                                bool flag10 = !player.CollideCheck<Solid, ConnectedDreamBlock>(at2);
                                if (flag10)
                                {
                                    player.Position += value * (float)j;
                                    connectedDreamBlock = dreamBlock;
                                    return true;
                                }
                            }
                        }
                        return false;
                    }
                    connectedDreamBlock = dreamBlock;
                    return true;
                }
            }
            return false;
        }

        private static int DreamDashUpdate()
        {
            Player player = CommunalHelperModule.getPlayer();
            DynData<Player> data = new DynData<Player>(player);

            // Star Fly Controls
            if (connectedDreamBlock.starFlyControl)
            {
                Vector2 input = Input.Aim.Value.SafeNormalize(Vector2.Zero);
                if (input != Vector2.Zero)
                {
                    Vector2 vector = player.Speed.SafeNormalize(Vector2.Zero);
                    if (vector != Vector2.Zero)
                    {
                        vector = Vector2.Dot(input, vector) != -0.8f ? vector.RotateTowards(input.Angle(), 5f * Engine.DeltaTime) : vector;
                        data.Set("dreamDashLastDir", vector);
                        player.Speed = vector * 240f;
                    }
                }
            }

            Input.Rumble(RumbleStrength.Light, RumbleLength.Medium);
            Vector2 position = player.Position;
            player.NaiveMove(player.Speed * Engine.DeltaTime);
            float dreamDashCanEndTimer = data.Get<float>("dreamDashCanEndTimer");
            bool flag = dreamDashCanEndTimer > 0f;
            if (flag)
            {
                data.Set<float>("dreamDashCanEndTimer", dreamDashCanEndTimer -= Engine.DeltaTime);
            }
            ConnectedDreamBlock dreamBlock = player.CollideFirst<ConnectedDreamBlock>();
            if (dreamBlock == null)
            {
                if (DreamDashedIntoSolid(player))
                {
                    bool invincible = SaveData.Instance.Assists.Invincible;
                    if (invincible)
                    {
                        player.Position = position;
                        player.Speed *= -1f;
                        player.Play("event:/game/general/assist_dreamblockbounce", null, 0f);
                    }
                    else
                    {
                        player.Die(Vector2.Zero, false, true);
                    }
                }
                else
                {
                    if (dreamDashCanEndTimer <= 0f)
                    {
                        Celeste.Freeze(0.05f);
                        bool flag5 = Input.Jump.Pressed && player.DashDir.X != 0f;
                        if (flag5)
                        {
                            data.Set("dreamJump", true);
                            player.Jump(true, true);
                        }

                        return 0;
                    }
                }
            }
            else
            {
                // new property
                data.Set("connectedSpaceJam", dreamBlock);
                if (player.Scene.OnInterval(0.1f))
                {
                    ConnectedDreamBlock.CreateTrail(player);
                }
                if (player.SceneAs<Level>().OnInterval(0.04f))
                {
                    DisplacementRenderer.Burst burst = player.SceneAs<Level>().Displacement.AddBurst(player.Center, 0.3f, 0f, 40f, 1f, null, null);
                    burst.WorldClipCollider = dreamBlock.Collider;
                    burst.WorldClipPadding = 2;
                }
            }
            return ConnectedDreamBlockState;
        }

        private static void DreamDashBegin()
        {
            Player player = CommunalHelperModule.getPlayer();
            DynData<Player> data = new DynData<Player>(player);
            SoundSource dreamSfxLoop = data.Get<SoundSource>("dreamSfxLoop");
            bool flag = dreamSfxLoop == null;
            if (flag)
            {
                dreamSfxLoop = new SoundSource();
                player.Add(dreamSfxLoop);
                data.Set("dreamSfxLoop", dreamSfxLoop);
            }
            player.Speed = player.DashDir * 240f;
            data.Set("dreamDashLastDir", player.Speed);
            player.TreatNaive = true;
            player.Depth = -12000;
            data.Set("dreamDashCanEndTimer", 0.1f);
            player.Stamina = 110f;
            data.Set("dreamJump", false);
            player.Play("event:/char/madeline/dreamblock_enter", null, 0f);
            if (connectedDreamBlock.starFlyControl)
            {
                player.Loop(dreamSfxLoop, "event:/CommunalHelperEvents/game/connectedDreamBlock/dreamblock_fly_travel");
            }
            else
            {
                player.Loop(dreamSfxLoop, "event:/char/madeline/dreamblock_travel");
            }
        }

        private static void DreamDashEnd()
        {
            Player player = CommunalHelperModule.getPlayer();
            DynData<Player> data = new DynData<Player>(player);
            player.Depth = 0;
            if (!data.Get<bool>("dreamJump"))
            {
                player.AutoJump = true;
                player.AutoJumpTimer = 0f;
            }
            bool flag2 = !player.Inventory.NoRefills;
            if (flag2)
            {
                player.RefillDash();
            }
            player.RefillStamina();
            player.TreatNaive = false;
            ConnectedDreamBlock dreamBlock = connectedDreamBlock;
            if (dreamBlock != null)
            {
                bool flag4 = player.DashDir.X != 0f;
                if (flag4)
                {
                    data.Set("jumpGraceTimer", 0.1f);
                    data.Set("dreamJump", true);
                }
                else
                {
                    data.Set("jumpGraceTimer", 0f);
                }
                dreamBlock.OnPlayerExit(player);
                data.Set<ConnectedDreamBlock>("connectedSpaceJam", null);
            }
            player.Stop(data.Get<SoundSource>("dreamSfxLoop"));
            player.Play("event:/char/madeline/dreamblock_exit", null, 0f);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
            if (dreamBlock.oneUse)
            {
                dreamBlock.BeginShatter();
                Audio.Play("event:/CommunalHelperEvents/game/connectedDreamBlock/dreamblock_shatter", player.Position);
            }
        }

        private static bool DreamDashedIntoSolid(Player player)
        {
            bool flag = player.CollideCheck<Solid>();
            bool result;
            if (flag)
            {
                for (int i = 1; i <= 5; i++)
                {
                    for (int j = -1; j <= 1; j += 2)
                    {
                        for (int k = 1; k <= 5; k++)
                        {
                            for (int l = -1; l <= 1; l += 2)
                            {
                                Vector2 value = new Vector2((float)(i * j), (float)(k * l));
                                bool flag2 = !player.CollideCheck<Solid>(player.Position + value);
                                if (flag2)
                                {
                                    player.Position += value;
                                    return false;
                                }
                            }
                        }
                    }
                }
                result = true;
            }
            else
            {
                result = false;
            }
            return result;
        }


        // Hooking stuff. Collaboration :)
        public static void Hook()
        {
            On.Celeste.Player.ctor += Player_ctor;

            On.Celeste.Player.OnCollideV += Player_OnCollideV;
            On.Celeste.Player.OnCollideH += Player_OnCollideH;
            On.Celeste.Player.UpdateSprite += Player_UpdateSprite;
            On.Celeste.Player.RefillDash += Player_RefillDash;
            On.Celeste.Player.ClimbBegin += Player_ClimbBegin;
            On.Celeste.Player.WallJump += Player_WallJump;
        }

        public static void Unhook()
        {
            On.Celeste.Player.ctor -= Player_ctor;

            On.Celeste.Player.OnCollideV -= Player_OnCollideV;
            On.Celeste.Player.OnCollideH -= Player_OnCollideH;
            On.Celeste.Player.UpdateSprite -= Player_UpdateSprite;
            On.Celeste.Player.RefillDash -= Player_RefillDash;
            On.Celeste.Player.ClimbBegin -= Player_ClimbBegin;
            On.Celeste.Player.WallJump -= Player_WallJump;
        }

        private static bool Player_RefillDash(On.Celeste.Player.orig_RefillDash orig, Player self)
        {
            if (self.StateMachine.State != ConnectedDreamBlockState)
                return orig(self);
            return false;
        }

        private static void Player_UpdateSprite(On.Celeste.Player.orig_UpdateSprite orig, Player self)
        {
            if (ConnectedDreamBlockState != 0 && self.StateMachine.State == ConnectedDreamBlockState)
            {
                if (self.Sprite.CurrentAnimationID != "dreamDashIn" && self.Sprite.CurrentAnimationID != "dreamDashLoop")
                {
                    self.Sprite.Play("dreamDashIn", false, false);
                }
            }
            else
            {
                orig(self);
            }
        }

        private static void Player_OnCollideH(On.Celeste.Player.orig_OnCollideH orig, Player self, CollisionData data)
        {
            if (self.StateMachine.State == 2 || self.StateMachine.State == 5)
            {
                bool flag14 = DreamDashCheck(self, Vector2.UnitX * (float)Math.Sign(self.Speed.X));
                if (flag14)
                {
                    self.StateMachine.State = ConnectedDreamBlockState;
                    DynData<Player> ddata = new DynData<Player>(self);
                    ddata.Set("dashAttackTimer", 0f);
                    ddata.Set("gliderBoostTimer", 0f);
                    return;
                }
            }
            if (self.StateMachine.State != ConnectedDreamBlockState)
            {
                orig(self, data);
            }


        }

        private static void Player_OnCollideV(On.Celeste.Player.orig_OnCollideV orig, Player self, CollisionData data)
        {
            DynData<Player> ddata = new DynData<Player>(self);
            if (
            !(self.StateMachine.State == 19) &&
            !(self.StateMachine.State == 3) &&
            !(self.StateMachine.State == 9) &&
            self.Speed.Y > 0f &&
            !((self.StateMachine.State == 2 || self.StateMachine.State == 5) && !ddata.Get<bool>("dashStartedOnGround")) &&
            !(self.StateMachine.State == 1))
            {
                Platform platformByPriority = SurfaceIndex.GetPlatformByPriority(
                    self.CollideAll<Platform>(self.Position + new Vector2(0f, 1f), ddata.Get<List<Entity>>("temp")));
                if (platformByPriority != null)
                {
                    if (platformByPriority is ConnectedDreamBlock)
                    {
                        (platformByPriority as ConnectedDreamBlock).FootstepRipple(self.Position);
                    }
                }
            }
            if (self.StateMachine.State == 2 || self.StateMachine.State == 5)
            {
                bool flag14 = DreamDashCheck(self, Vector2.UnitY * (float)Math.Sign(self.Speed.Y));
                if (flag14)
                {
                    self.StateMachine.State = ConnectedDreamBlockState;
                    ddata.Set("dashAttackTimer", 0f);
                    ddata.Set("gliderBoostTimer", 0f);
                    return;
                }
            }
            if (self.StateMachine.State != ConnectedDreamBlockState)
            {
                orig(self, data);
            }
        }

        private static void Player_ctor(On.Celeste.Player.orig_ctor orig, Player self, Vector2 position, PlayerSpriteMode spriteMode)
        {
            orig(self, position, spriteMode);
            ConnectedDreamBlockState = self.StateMachine.AddState(new Func<int>(DreamDashUpdate), null, DreamDashBegin, DreamDashEnd);
        }

        private static void Player_WallJump(On.Celeste.Player.orig_WallJump orig, Player self, int dir)
        {
            DynData<Player> ddata = new DynData<Player>(self);
            Platform platformByPriority = SurfaceIndex.GetPlatformByPriority(self.CollideAll<Solid>(self.Position - Vector2.UnitX * dir * 4f, ddata.Get<List<Entity>>("temp")));
            if (platformByPriority != null)
            {
                if (platformByPriority is ConnectedDreamBlock)
                {
                    (platformByPriority as ConnectedDreamBlock).FootstepRipple(self.Position + new Vector2(dir * 3, -4f));
                }
            }
            orig(self, dir);
        }

        private static void Player_ClimbBegin(On.Celeste.Player.orig_ClimbBegin orig, Player self)
        {

            DynData<Player> ddata = new DynData<Player>(self);
            Platform platformByPriority = SurfaceIndex.GetPlatformByPriority(self.CollideAll<Solid>(self.Position + Vector2.UnitX * (float)self.Facing, ddata.Get<List<Entity>>("temp")));
            if (platformByPriority != null)
            {
                if (platformByPriority is ConnectedDreamBlock)
                {
                    (platformByPriority as ConnectedDreamBlock).FootstepRipple(self.Position + new Vector2((int)self.Facing * 3, -4f));
                }
            }

            orig(self);
        }
    }
}
