using System;
using System.Collections.Generic;
using System.Linq;
using Monocle;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using MonoMod.Utils;
using System.Collections;
using FMOD;

namespace Celeste.Mod.CommunalHelper
{
    [CustomEntity("CommunalHelper/ConnectedDreamBlock")]
    [TrackedAs(typeof(DreamBlock))]

    class ConnectedDreamBlock : DreamBlock
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

        public bool featherMode;
        public bool canDreamDash = true;
        public bool oneUse;
        private bool shattering = false;
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

        public ConnectedDreamBlock(Vector2 position, int width, int height, bool featherMode, bool useOnce)
            : base(position, width, height, null, false, false)
        {
            oneUse = useOnce;
            this.featherMode = featherMode;
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
            foreach (DreamBlock block in Scene.Tracker.GetEntities<DreamBlock>())
            {
                if (block is ConnectedDreamBlock) {
                    ConnectedDreamBlock connectedBlock = block as ConnectedDreamBlock;
                    if (!connectedBlock.HasGroup && connectedBlock.featherMode == from.featherMode && base.Scene.CollideCheck(new Rectangle((int)from.X, (int)from.Y, (int)from.Width, (int)from.Height), connectedBlock)) {
                        AddToGroupAndFindChildren(connectedBlock);
                    }
                }
            }
        }

        private void SetupParticles()
        {
            groupWidth = GroupBoundsMax.X - GroupBoundsMin.X; 
            groupHeight = GroupBoundsMax.Y - GroupBoundsMin.Y;

            /* Setup particles, will be rendered by the Group Master */
            float countFactor = featherMode ? 0.5f : 0.7f;
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
                if (featherMode) {
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

                    if (featherMode) {
                        UpdateParticles();
                    }
                }
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
                    if (featherMode) {
                        position += Calc.AngleToVector(rotation, 4f);
                    }
                    position = PutInside(position, groupRect);

                    if (!CheckGroupParticleCollide(position)) continue;

                    Color color = Color.Lerp(particle.Color, Color.Black, colorLerp);

                    if (featherMode)
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

        public void ConnectedFootstepRipple(Vector2 position)
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

        private void OneUseDestroy()
        {
            Collidable = (Visible = false);
            DisableStaticMovers();
        }

        public void BeginShatter()
        {
            if (!shattering) {
                shattering = true;
                Audio.Play("event:/CommunalHelperEvents/game/connectedDreamBlock/dreamblock_shatter", Position);

                ConnectedDreamBlock mastr = MasterOfGroup ? this : master;
                foreach (ConnectedDreamBlock jam in mastr.group) {
                    jam.Add(new Coroutine(jam.ShatterSeq()));
                    jam.canDreamDash = false;
                }
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

                Vector2 centre = new Vector2(groupRect.Center.X, groupRect.Center.Y);
                for (int i = 0; i < particles.Length; i++)
                {
                    Vector2 position = particles[i].Position;
                    position += camera * (0.3f + 0.25f * particles[i].Layer);
                    position = PutInside(position, groupRect);
                    bool inside = false;
                    foreach(ConnectedDreamBlock block in group) {
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
                        Source = featherMode ? petalTextures[particles[i].Layer] : particleTextures[2],
                        SpinMax = featherMode ? (float)Math.PI : 0,
                        RotationMode = featherMode ? ParticleType.RotationModes.Random : ParticleType.RotationModes.None,
                        Direction = (position - centre).Angle()
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
        public static void Hook()
        {
            On.Celeste.DreamBlock.FootstepRipple += modFootstepRipple;
            On.Celeste.DreamBlock.OnPlayerExit += modOnPlayerExit;
            On.Celeste.Player.DreamDashBegin += modDreamDashBegin;
            On.Celeste.Player.DreamDashUpdate += modDreamDashUpdate;
        }

        public static void Unhook()
        {
            On.Celeste.DreamBlock.FootstepRipple -= modFootstepRipple;
            On.Celeste.DreamBlock.OnPlayerExit -= modOnPlayerExit;
            On.Celeste.Player.DreamDashBegin -= modDreamDashBegin;
            On.Celeste.Player.DreamDashUpdate -= modDreamDashUpdate;
        }

        private static void modOnPlayerExit(On.Celeste.DreamBlock.orig_OnPlayerExit orig, DreamBlock dreamBlock, Player player) {
            orig(dreamBlock, player);
            if (dreamBlock is ConnectedDreamBlock) {
                ConnectedDreamBlock connectedDreamBlock = dreamBlock as ConnectedDreamBlock;
                if (connectedDreamBlock.oneUse) {
                    connectedDreamBlock.BeginShatter();
                }
            }
        }

        private static void modFootstepRipple(On.Celeste.DreamBlock.orig_FootstepRipple orig, DreamBlock dreamBlock, Vector2 pos) {
            if (dreamBlock is ConnectedDreamBlock) {
                (dreamBlock as ConnectedDreamBlock).ConnectedFootstepRipple(pos);
            } else {
                orig(dreamBlock, pos);
            }
        }

        private static void modDreamDashBegin(On.Celeste.Player.orig_DreamDashBegin orig, Player player) {
            orig(player);
            var playerData = getPlayerData(player);
            DreamBlock dreamBlock = playerData.Get<DreamBlock>("dreamBlock");
            if (dreamBlock is ConnectedDreamBlock && (dreamBlock as ConnectedDreamBlock).featherMode) {
                SoundSource dreamSfxLoop = playerData.Get<SoundSource>("dreamSfxLoop");
                player.Stop(dreamSfxLoop);
                player.Loop(dreamSfxLoop, "event:/CommunalHelperEvents/game/connectedDreamBlock/dreamblock_fly_travel");
            }

        }

        private static int modDreamDashUpdate(On.Celeste.Player.orig_DreamDashUpdate orig, Player player) {
            var playerData = getPlayerData(player);
            DreamBlock dreamBlock = playerData.Get<DreamBlock>("dreamBlock");
            if (dreamBlock is ConnectedDreamBlock && (dreamBlock as ConnectedDreamBlock).featherMode) {
                Vector2 input = Input.Aim.Value.SafeNormalize(Vector2.Zero);
                if (input != Vector2.Zero) {
                    Vector2 vector = player.Speed.SafeNormalize(Vector2.Zero);
                    if (vector != Vector2.Zero) {
                        vector = Vector2.Dot(input, vector) != -0.8f ? vector.RotateTowards(input.Angle(), 5f * Engine.DeltaTime) : vector;
                        playerData.Set("dreamDashLastDir", vector);
                        player.Speed = vector * 240f;
                    }
                }
            }
            return orig(player);
        }

        private static DynData<Player> getPlayerData(Player player) {
            return new DynData<Player>(player);
        }
    }
}
