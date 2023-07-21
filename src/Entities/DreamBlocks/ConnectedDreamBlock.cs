using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/ConnectedDreamBlock")]
[Tracked(true)]
public class ConnectedDreamBlock : CustomDreamBlock
{
    private struct SpaceJamTile
    {
        public readonly int X, Y;
        public readonly int[] Edges;
        public readonly bool Exist;

        public SpaceJamTile(int x, int y, bool exist)
        {
            X = x;
            Y = y;
            Edges = new int[4];
            for (int i = 0; i < Edges.Length; i++)
                Edges[i] = -1;

            Exist = exist;
        }

        public bool EdgeExist(Edges edge)
        {
            return Edges[(int) edge] != -1;
        }

        public bool TryGetEdge(Edges edge, out int result)
        {
            result = Edges[(int) edge];
            return result != -1;
        }

        public int this[Edges edge]
        {
            get => Edges[(int) edge];
            set => Edges[(int) edge] = value;
        }

    }

    private struct SpaceJamEdge
    {
        public SpaceJamEdge(Vector2 startV, Vector2 endV, float wobbleOff, bool flipNorm, Edges face)
        {
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

    private struct SpaceJamCorner
    {
        public readonly bool
            upright, upleft, downright, downleft,
            inupright, inupleft, indownright, indownleft;

        public readonly int x, y;

        public SpaceJamCorner(int x_, int y_, bool ur, bool ul, bool dr, bool dl, bool iur, bool iul, bool idr, bool idl)
        {
            x = x_;
            y = y_;
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

    private enum Edges
    {
        North,
        East,
        South,
        West,
    }

    private static readonly MethodInfo m_DreamBlock_LineAmplitude = typeof(DreamBlock).GetMethod("LineAmplitude", BindingFlags.NonPublic | BindingFlags.Instance);

    public Vector2 GroupBoundsMin;
    public Vector2 GroupBoundsMax;

    public bool HasGroup { get; private set; }

    public bool MasterOfGroup { get; private set; }

    public Dictionary<Platform, Vector2> Moves;
    public List<ConnectedDreamBlock> Group;
    public List<JumpThru> JumpThrus;
    protected ConnectedDreamBlock master;

    protected bool IncludeJumpThrus = false;

    public ConnectedDreamBlock(EntityData data, Vector2 offset)
        : base(data, offset) { }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);

        if (!HasGroup)
        {
            /* Setup group */
            MasterOfGroup = true;

            Moves = new Dictionary<Platform, Vector2>();
            Group = new List<ConnectedDreamBlock>();
            JumpThrus = new List<JumpThru>();

            GroupBoundsMin = new Vector2(X, Y);
            GroupBoundsMax = new Vector2(Right, Bottom);
            GroupEdges = new List<SpaceJamEdge>();
            GroupCorners = new List<SpaceJamCorner>();
            AddToGroupAndFindChildren(this);
            SetupCustomParticles(0, 0); // Parameters are ignored

            GroupRect = new Rectangle(
                (int) GroupBoundsMin.X,
                (int) GroupBoundsMin.Y,
                (int) (GroupBoundsMax.X - GroupBoundsMin.X),
                (int) (GroupBoundsMax.Y - GroupBoundsMin.Y));

            float groupW = GroupBoundsMax.X - GroupBoundsMin.X;
            float groupH = GroupBoundsMax.Y - GroupBoundsMin.Y;

            /* Setup Edges of the group */
            int groupTileW = (int) (groupW / 8.0f);
            int groupTileH = (int) (groupH / 8.0f);
            SpaceJamTile[,] tiles = new SpaceJamTile[(groupTileW + 2), (groupTileH + 2)];
            for (int x = 0; x < groupTileW + 2; x++)
            {
                for (int y = 0; y < groupTileH + 2; y++)
                {
                    tiles[x, y] = new SpaceJamTile(x - 1, y - 1, TileHasGroupDreamBlock(x - 1, y - 1));
                }
            }
            for (int x = 1; x < groupTileW + 1; x++)
            {
                for (int y = 1; y < groupTileH + 1; y++)
                {
                    if (tiles[x, y].Exist)
                        AutoEdge(tiles, x, y);
                }
            }

            Vector2 groupCenter = new(groupW / 2, groupH / 2);
            for (int i = 0; i < GroupEdges.Count; i++)
            {
                SpaceJamEdge edge = GroupEdges[i];
                float angle = Calc.Angle(groupCenter, Vector2.Lerp(edge.start, edge.end, 0.5f)) + Calc.HalfCircle;
                GroupEdges[i] = new SpaceJamEdge(edge.start, edge.end, edge.wobbleOffset + angle, edge.flipNormal, edge.facing);
            }
        }
    }

    public override void SetupCustomParticles(float canvasWidth, float canvasHeight)
    {
        if (MasterOfGroup)
            base.SetupCustomParticles(GroupBoundsMax.X - GroupBoundsMin.X, GroupBoundsMax.Y - GroupBoundsMin.Y);
    }

    protected override void UpdateParticles()
    {
        if (MasterOfGroup)
            base.UpdateParticles();
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
        if (upright || upleft || downright || downleft || inupright || inupleft || indownright || indownleft)
        {
            GroupCorners.Add(new SpaceJamCorner(x - 1, y - 1, upright, upleft, downright, downleft, inupright, inupleft, indownright, indownleft));
        }
        #endregion

        if (!nNorth.Exist)
        {
            if (nWest.TryGetEdge(Edges.North, out int idx))
            {
                SpaceJamEdge edge = GroupEdges[idx];
                edge.end.X += 8;
                GroupEdges[idx] = edge;
                self[Edges.North] = idx;
            }
            else
            {
                SpaceJamEdge newEdge;
                newEdge.end = newEdge.start = TileToPoint(x - 1, y - 1);
                newEdge.end.X += 8;
                newEdge.wobbleOffset = 0.0f;
                newEdge.flipNormal = false;
                newEdge.facing = Edges.North;
                self[Edges.North] = GroupEdges.Count();
                GroupEdges.Add(newEdge);
            }
        }

        if (!nEast.Exist)
        {
            if (nNorth.TryGetEdge(Edges.East, out int idx))
            {
                SpaceJamEdge edge = GroupEdges[idx];
                edge.end.Y += (nSouth.Exist && nSouthEast.Exist) ? 9 : 8;
                GroupEdges[idx] = edge;
                self[Edges.East] = idx;
            }
            else
            {
                SpaceJamEdge newEdge;
                newEdge.end = newEdge.start = TileToPoint(x, y - 1);
                newEdge.end.Y += (nSouth.Exist && nSouthEast.Exist) ? 9 : 8;
                if (nNorth.Exist)
                {
                    newEdge.start.Y -= 1;
                }
                newEdge.wobbleOffset = 0.7f;
                newEdge.flipNormal = false;
                newEdge.facing = Edges.East;
                self[Edges.East] = GroupEdges.Count();
                GroupEdges.Add(newEdge);
            }
        }

        if (!nSouth.Exist)
        {
            if (nWest.TryGetEdge(Edges.South, out int idx))
            {
                SpaceJamEdge edge = GroupEdges[idx];
                edge.end.X += 8;
                GroupEdges[idx] = edge;
                self[Edges.South] = idx;
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
                newEdge.facing = Edges.South;
                self[Edges.South] = GroupEdges.Count();
                GroupEdges.Add(newEdge);
            }
        }

        if (!nWest.Exist)
        {
            if (nNorth.TryGetEdge(Edges.West, out int idx))
            {
                SpaceJamEdge edge = GroupEdges[idx];
                edge.end.Y += (nSouth.Exist && nSouthWest.Exist) ? 9 : 8;
                GroupEdges[idx] = edge;
                self[Edges.West] = idx;
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
                newEdge.facing = Edges.West;
                self[Edges.West] = GroupEdges.Count();
                GroupEdges.Add(newEdge);
            }
        }
    }

    private void AddToGroupAndFindChildren(ConnectedDreamBlock from)
    {
        if (from.X < GroupBoundsMin.X)
        {
            GroupBoundsMin.X = from.X;
        }
        if (from.Y < GroupBoundsMin.Y)
        {
            GroupBoundsMin.Y = from.Y;
        }
        if (from.Right > GroupBoundsMax.X)
        {
            GroupBoundsMax.X = from.Right;
        }
        if (from.Bottom > GroupBoundsMax.Y)
        {
            GroupBoundsMax.Y = from.Bottom;
        }

        from.HasGroup = true;
        from.OnDashCollide = new DashCollision(OnDash);
        Group.Add(from);
        Moves.Add(from, from.Position);
        if (from != this)
        {
            from.master = this;
        }

        if (IncludeJumpThrus)
        {
            foreach (JumpThru jumpThru in Scene.CollideAll<JumpThru>(new Rectangle((int) from.X - 1, (int) from.Y, (int) from.Width + 2, (int) from.Height)))
            {
                if (!JumpThrus.Contains(jumpThru))
                {
                    AddJumpThru(jumpThru);
                }
            }
            foreach (JumpThru jumpThru in Scene.CollideAll<JumpThru>(new Rectangle((int) from.X, (int) from.Y - 1, (int) from.Width, (int) from.Height + 2)))
            {
                if (!JumpThrus.Contains(jumpThru))
                {
                    AddJumpThru(jumpThru);
                }
            }
        }

        foreach (Entity entity in Scene.Tracker.GetEntities<ConnectedDreamBlock>())
        {
            ConnectedDreamBlock connectedBlock = (ConnectedDreamBlock) entity;
            if (!connectedBlock.HasGroup && connectedBlock.FeatherMode == from.FeatherMode &&
                Scene.CollideCheck(new Rectangle((int) from.X, (int) from.Y, (int) from.Width, (int) from.Height), connectedBlock))
            {
                AddToGroupAndFindChildren(connectedBlock);
            }
        }
    }

    private void AddJumpThru(JumpThru jp)
    {
        jp.OnDashCollide = new DashCollision(OnDashJumpThru);
        JumpThrus.Add(jp);
        Moves.Add(jp, jp.Position);
        foreach (Entity entity in Scene.Tracker.GetEntities<ConnectedDreamBlock>())
        {
            ConnectedDreamBlock connectedBlock = (ConnectedDreamBlock) entity;
            if (!connectedBlock.HasGroup && connectedBlock.FeatherMode == FeatherMode &&
                Scene.CollideCheck(new Rectangle((int) jp.X - 1, (int) jp.Y, (int) jp.Width + 2, (int) jp.Height), connectedBlock))
            {
                AddToGroupAndFindChildren(connectedBlock);
            }
        }
    }

    protected virtual DashCollisionResults OnDash(Player player, Vector2 dir)
        => PlayerHasDreamDash
            ? DashCollisionResults.NormalOverride
            : DashCollisionResults.NormalCollision;

    protected virtual DashCollisionResults OnDashJumpThru(Player player, Vector2 dir)
        => PlayerHasDreamDash
            ? DashCollisionResults.NormalOverride
            : DashCollisionResults.NormalCollision;

    public override void Render()
    {
        Camera camera = SceneAs<Level>().Camera;
        Vector2 GroupPosition = new(GroupBoundsMin.X, GroupBoundsMin.Y);

        float whiteFill = baseData.Get<float>("whiteFill");
        float whiteHeight = baseData.Get<float>("whiteHeight");
        Vector2 shake = baseData.Get<Vector2>("shake");

        if (GroupRect.Right < camera.Left || GroupRect.Left > camera.Right || GroupRect.Bottom < camera.Top || GroupRect.Top > camera.Bottom)
        {
            return;
        }

        if (MasterOfGroup)
        {
            Color lineColor = PlayerHasDreamDash ? ActiveLineColor : DisabledLineColor;
            Color backColor = Color.Lerp(PlayerHasDreamDash ? baseData.Get<Color>("activeBackColor") : baseData.Get<Color>("disabledBackColor"), Color.White, ColorLerp);

            if (whiteFill > 0f)
            {
                lineColor = Color.Lerp(lineColor, Color.White, whiteFill);
                if (whiteHeight == 1f)
                    backColor = Color.Lerp(backColor, Color.White, whiteFill);
            }

            #region Background rendering

            Vector2 cameraPositon = SceneAs<Level>().Camera.Position;
            foreach (ConnectedDreamBlock block in Group)
            {
                if (block.Right < camera.Left || block.Left > camera.Right || block.Bottom < camera.Top || block.Top > camera.Bottom)
                {
                    continue;
                }
                Draw.Rect(block.Position + shake, block.Width, block.Height, backColor);
            }

            #endregion

            #region Particle rendering

            for (int i = 0; i < particles.Length; i++)
            {
                DreamParticle particle = particles[i];
                int layer = particle.Layer;
                Vector2 position = particle.Position + (cameraPositon * (0.3f + (0.25f * layer)));
                float rotation = 1.5707963705062866f - 0.8f + (float) Math.Sin(particle.RotationCounter * particle.MaxRotate);
                if (FeatherMode)
                {
                    position += Calc.AngleToVector(rotation, 4f);
                }
                position = PutInside(position, GroupRect);

                bool particleIsInside = false;
                foreach (ConnectedDreamBlock block in Group)
                {
                    if (block.CheckParticleCollide(position))
                    {
                        particleIsInside = true;
                        break;
                    }
                }
                if (!particleIsInside)
                    continue;

                Color color = Color.Lerp(particle.Color, Color.Black, ColorLerp);
                if (whiteFill > 0f && whiteHeight == 1f)
                    color = Color.Lerp(color, Color.White, whiteFill);

                if (FeatherMode)
                {
                    featherTextures[layer].DrawCentered(position + Shake + shake, color, 1, rotation);
                }
                else
                {
                    MTexture[] particleTextures = RefillCount != -1 ? doubleRefillStarTextures : baseData.Get<MTexture[]>("particleTextures");
                    MTexture particleTexture;
                    switch (layer)
                    {
                        case 0:
                        {
                            int index = (int) (((particle.TimeOffset * 4f) + baseData.Get<float>("animTimer")) % 4f);
                            particleTexture = particleTextures[3 - index];
                            break;
                        }
                        case 1:
                        {
                            int index = (int) (((particle.TimeOffset * 2f) + baseData.Get<float>("animTimer")) % 2f);
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

            if (whiteFill == 1f && whiteHeight < 1f)
            {
                float whiteFillBottom = GroupRect.Y + (GroupRect.Height * whiteHeight);
                foreach (ConnectedDreamBlock block in Group)
                {
                    if (block.Right < camera.Left || block.Left > camera.Right || block.Bottom < camera.Top || block.Top > camera.Bottom)
                    {
                        continue;
                    }
                    if (block.Top <= whiteFillBottom)
                        Draw.Rect(block.Position + shake, block.Width, Calc.Clamp(whiteFillBottom - block.Y, 1f, block.Height), Color.White);
                }
            }

            #endregion

            #region Edge & Corner Rendering

            if (whiteFill > 0f && whiteHeight < 1f)
            {
                backColor = Color.Lerp(backColor, Color.White, whiteFill);
            }

            foreach (SpaceJamCorner corner in GroupCorners)
            {
                // Yes.
                RenderCorner(GroupPosition + shake, corner, lineColor, backColor);
            }

            foreach (SpaceJamEdge edge in GroupEdges)
            {
                Vector2 start = edge.start, end = edge.end;
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
                WobbleLine(GroupBoundsMin + start + shake, GroupBoundsMin + end + shake, edge.wobbleOffset, lineColor, backColor);
            }

            #endregion
        }
    }

    private void RenderCorner(Vector2 position, SpaceJamCorner corner, Color line, Color back)
    {
        int x = (int) ((corner.x * 8) + position.X);
        int y = (int) ((corner.y * 8) + position.Y);

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
        if (corner.inupright)
        {
            Draw.Rect(x + 6, y, 4, 3, back);
            Draw.Rect(x + 5, y - 1, 3, 3, back);
            Draw.Line(x + 7, y, x + 10, y, line);
            Draw.Line(x + 7, y, x + 7, y - 1, line);
        }
        if (corner.inupleft)
        {
            Draw.Rect(x - 2, y, 4, 3, back);
            Draw.Rect(x, y - 1, 3, 3, back);
            Draw.Line(x - 2, y, x, y, line);
            Draw.Line(x, y + 1, x, y - 1, line);
        }
        if (corner.indownright)
        {
            Draw.Rect(x + 6, y + 5, 4, 3, back);
            Draw.Rect(x + 5, y + 6, 3, 3, back);
            Draw.Line(x + 7, y + 7, x + 10, y + 7, line);
            Draw.Line(x + 8, y + 8, x + 8, y + 9, line);
        }
        if (corner.indownleft)
        {
            Draw.Rect(x - 2, y + 5, 4, 3, back);
            Draw.Rect(x, y + 6, 3, 3, back);
            Draw.Line(x - 2, y + 7, x + 1, y + 7, line);
            Draw.Line(x + 1, y + 8, x + 1, y + 9, line);
        }
    }

    private void WobbleLine(Vector2 from, Vector2 to, float offset, Color line, Color back)
    {
        Vector2 vec = to - from;
        float length = vec.Length();
        Vector2 value = Vector2.Normalize(vec);
        Vector2 vector = new(value.Y, 0f - value.X);

        float scaleFactor = 0f;
        int increment = 16;
        for (int i = 2; i < length - 2; i += increment)
        {
            float scale = MathHelper.Lerp((float) m_DreamBlock_LineAmplitude.Invoke(this, new object[] { baseData.Get<float>("wobbleFrom") + offset, i }),
                (float) m_DreamBlock_LineAmplitude.Invoke(this, new object[] { baseData.Get<float>("wobbleTo") + offset, i }),
                baseData.Get<float>("wobbleEase"));
            if (i + increment >= length)
            {
                scale = 0f;
            }
            float num4 = Math.Min(increment, length - 2f - i);
            Vector2 vector2 = from + (value * i) + (vector * scaleFactor);
            Vector2 vector3 = from + (value * (i + num4)) + (vector * scale);
            Draw.Line(vector2 - vector, vector3 - vector, back);
            Draw.Line(vector2 - (vector * 2f), vector3 - (vector * 2f), back);
            //Draw.Line(vector2 - vector * 3f, vector3 - vector * 3f, back);
            Draw.Line(vector2, vector3, line);
            scaleFactor = scale;
        }
    }

    public override void MoveHExact(int move)
    {
        base.MoveHExact(move);
        GroupBoundsMax.X += move;
        GroupBoundsMin.X += move;
    }

    public override void MoveVExact(int move)
    {
        base.MoveVExact(move);
        GroupBoundsMax.Y += move;
        GroupBoundsMin.Y += move;
    }

    private void SpawnFastRoutineParticles()
    {
        if (MasterOfGroup)
        {
            Level level = SceneAs<Level>();
            foreach (SpaceJamEdge edge in GroupEdges)
            {
                float width = edge.end.X - edge.start.X;
                float centerH = edge.start.X + (width / 2f);
                float height = edge.end.Y - edge.start.Y;
                float centerV = edge.start.Y + (height / 2f);
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

    private void SpawnSlowRoutineParticles()
    {
        if (MasterOfGroup)
        {
            Level level = SceneAs<Level>();
            Camera camera = level.Camera;

            float whiteHeight = baseData.Get<float>("whiteHeight");
            float whiteFillBottom = GroupRect.Y + (GroupRect.Height * whiteHeight);

            foreach (ConnectedDreamBlock block in Group)
            {
                if (block.Right < camera.Left || block.Left > camera.Right || block.Bottom < camera.Top || block.Top > camera.Bottom)
                {
                    continue;
                }

                if (block.Top <= whiteFillBottom && block.Bottom >= whiteFillBottom)
                {
                    for (int i = 0; i < block.Width; i += 4)
                    {
                        level.ParticlesFG.Emit(Strawberry.P_WingsBurst, new Vector2(block.X + i, whiteFillBottom + 1f));
                    }
                }
            }
        }
    }

    private bool TileHasGroupDreamBlock(int x, int y)
    {
        Rectangle rect = TileToRectangle(x, y);
        rect.Offset((int) GroupBoundsMin.X, (int) GroupBoundsMin.Y);
        foreach (ConnectedDreamBlock block in Group)
        {
            if (block.CollideRect(rect))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Relative to Position
    /// </summary>
    private Rectangle TileToRectangle(int x, int y)
    {
        Vector2 p = TileToPoint(x, y);
        return new Rectangle((int) p.X, (int) p.Y, 8, 8);
    }

    /// <summary>
    /// Relative to Position
    /// </summary>
    private Vector2 TileToPoint(int x, int y)
    {
        return new Vector2(x * 8, y * 8);
    }

    public void ConnectedFootstepRipple(Vector2 position)
    {
        if (PlayerHasDreamDash)
        {
            ConnectedDreamBlock master = MasterOfGroup ? this : this.master;

            foreach (ConnectedDreamBlock block in master.Group)
            {
                DisplacementRenderer.Burst burst = (Scene as Level).Displacement.AddBurst(position, 0.5f, 0f, 40f);
                burst.WorldClipCollider = block.Collider;
                burst.WorldClipPadding = 1;
            }
        }
    }

    public override void BeginShatter()
    {
        if (ShatterCheck())
        {
            Audio.Play(CustomSFX.game_connectedDreamBlock_dreamblock_shatter, Position);

            ConnectedDreamBlock master = MasterOfGroup ? this : this.master;
            foreach (ConnectedDreamBlock block in master.Group)
            {
                block.shattering = true;
                block.Add(new Coroutine(block.ShatterSequence()));
            }
        }
    }

    private IEnumerator ShatterSequence()
    {
        if (QuickDestroy)
        {
            Collidable = false;
            foreach (StaticMover entity in staticMovers)
            {
                entity.Entity.Collidable = false;
            }
        }
        else
        {
            yield return 0.28f;
        }

        while (ColorLerp < 2.0f)
        {
            ColorLerp += Engine.DeltaTime * 10.0f;
            yield return null;
        }

        ColorLerp = 1.0f;
        if (!QuickDestroy)
        {
            yield return 0.05f;
        }

        if (MasterOfGroup)
        {
            Level level = SceneAs<Level>();
            level.Shake(.65f);
            Vector2 camera = level.Camera.Position;
            Rectangle GroupRect = new(
                (int) GroupBoundsMin.X,
                (int) GroupBoundsMin.Y,
                (int) (GroupBoundsMax.X - GroupBoundsMin.X),
                (int) (GroupBoundsMax.Y - GroupBoundsMin.Y));

            Vector2 centre = new(GroupRect.Center.X, GroupRect.Center.Y);
            for (int i = 0; i < particles.Length; i++)
            {
                Vector2 position = particles[i].Position;
                position += camera * (0.3f + (0.25f * particles[i].Layer));
                position = PutInside(position, GroupRect);
                bool inside = false;
                foreach (ConnectedDreamBlock block in Group)
                {
                    if (block.CollidePoint(position))
                    {
                        inside = true;
                        break;
                    }
                }
                if (!inside)
                {
                    continue;
                }

                Color flickerColor = Color.Lerp(particles[i].Color, Color.White, 0.6f);
                ParticleType type = new(Lightning.P_Shatter)
                {
                    ColorMode = ParticleType.ColorModes.Fade,
                    Color = particles[i].Color,
                    Color2 = flickerColor, //Color.Lerp(particles[i].Color, Color.White, 0.5f),
                    Source = FeatherMode ? featherTextures[particles[i].Layer] : baseData.Get<MTexture[]>("particleTextures")[2],
                    SpinMax = FeatherMode ? (float) Math.PI : 0,
                    RotationMode = FeatherMode ? ParticleType.RotationModes.Random : ParticleType.RotationModes.None,
                    Direction = (position - centre).Angle()
                };
                level.ParticlesFG.Emit(type, 1, position, Vector2.One * 3f);
            }

            foreach (ConnectedDreamBlock block in Group)
            {
                block.OneUseDestroy();
            }

            foreach (JumpThru jumpThru in JumpThrus)
            {
                jumpThru.RemoveSelf();
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

    #region Hooks

    private static IDetour hook_DreamBlock_FastActivate, hook_DreamBlock_FastDeactivate;
    private static IDetour hook_DreamBlock_Activate, hook_DreamBlock_Deactivate;

    private static FieldInfo f_DreamBlock_Routine_this;

    public static void Hook()
    {
        On.Celeste.DreamBlock.FootstepRipple += DreamBlock_FootstepRipple;

        Type[] nestedTypes = typeof(DreamBlock).GetNestedTypes(BindingFlags.NonPublic);
        Type nestedType = nestedTypes.First(t => t.Name.StartsWith("<FastActivate>"));
        f_DreamBlock_Routine_this = nestedType.GetField("<>4__this", BindingFlags.Public | BindingFlags.Instance);
        hook_DreamBlock_FastActivate = new ILHook(nestedType.GetMethod("MoveNext", BindingFlags.NonPublic | BindingFlags.Instance), DreamBlockFastRoutine);

        nestedType = nestedTypes.First(t => t.Name.StartsWith("<FastDeactivate>"));
        f_DreamBlock_Routine_this = nestedType.GetField("<>4__this", BindingFlags.Public | BindingFlags.Instance);
        hook_DreamBlock_FastDeactivate = new ILHook(nestedType.GetMethod("MoveNext", BindingFlags.NonPublic | BindingFlags.Instance), DreamBlockFastRoutine);

        nestedType = nestedTypes.First(t => t.Name.StartsWith("<Activate>"));
        f_DreamBlock_Routine_this = nestedType.GetField("<>4__this", BindingFlags.Public | BindingFlags.Instance);
        hook_DreamBlock_Activate = new ILHook(nestedType.GetMethod("MoveNext", BindingFlags.NonPublic | BindingFlags.Instance), DreamBlockSlowRoutine);

        nestedType = nestedTypes.First(t => t.Name.StartsWith("<Deactivate>"));
        f_DreamBlock_Routine_this = nestedType.GetField("<>4__this", BindingFlags.Public | BindingFlags.Instance);
        hook_DreamBlock_Deactivate = new ILHook(nestedType.GetMethod("MoveNext", BindingFlags.NonPublic | BindingFlags.Instance), DreamBlockSlowRoutine);
    }

    public static void Unhook()
    {
        On.Celeste.DreamBlock.FootstepRipple -= DreamBlock_FootstepRipple;

        hook_DreamBlock_FastActivate.Dispose();
        hook_DreamBlock_FastDeactivate.Dispose();
        hook_DreamBlock_Activate.Dispose();
        hook_DreamBlock_Deactivate.Dispose();
    }

    private static void DreamBlockSlowRoutine(ILContext il)
    {
        ILCursor cursor = new(il);
        cursor.GotoNext(instr => instr.OpCode == OpCodes.Ldfld && ((FieldReference) instr.Operand).Name.Contains("level"));
        cursor.GotoNext(MoveType.After, instr => instr.OpCode.ToShortOp() == OpCodes.Brfalse_S);
        object breakTarget = cursor.Prev.Operand;

        // Load DreamBlock object
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldfld, f_DreamBlock_Routine_this);

        cursor.EmitDelegate<Func<DreamBlock, bool>>(block =>
        {
            if (block is ConnectedDreamBlock connected)
            {
                connected.SpawnSlowRoutineParticles();
                return true;
            }
            return false;
        });

        // Skip regular particles;
        cursor.Emit(OpCodes.Brtrue, breakTarget);
    }

    private static void DreamBlockFastRoutine(ILContext il)
    {
        ILCursor cursor = new(il);
        cursor.GotoNext(instr => instr.Next.OpCode == OpCodes.Ldfld && ((FieldReference) instr.Next.Operand).Name.Contains("level"));

        // Load DreamBlock object
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldfld, f_DreamBlock_Routine_this);

        cursor.EmitDelegate<Func<DreamBlock, bool>>(block =>
        {
            if (block is ConnectedDreamBlock connected)
            {
                connected.SpawnFastRoutineParticles();
                return true;
            }
            return false;
        });

        // Skip regular particles
        cursor.Emit(OpCodes.Brtrue, il.Instrs.Last(instr => instr.Previous?.OpCode == OpCodes.Callvirt && ((MethodReference) instr.Previous?.Operand).Name == "Emit"));
    }

    private static void DreamBlock_FootstepRipple(On.Celeste.DreamBlock.orig_FootstepRipple orig, DreamBlock dreamBlock, Vector2 pos)
    {
        if (dreamBlock is ConnectedDreamBlock connectedBlock)
        {
            connectedBlock.ConnectedFootstepRipple(pos);
        }
        else
        {
            orig(dreamBlock, pos);
        }
    }

    #endregion

}
