using Celeste.Mod.CommunalHelper.Imports;
using Celeste.Mod.Helpers;
using MonoMod.Utils;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Entities;

public class ConnectedSolid : Solid
{
    public class BGTilesRenderer : Entity
    {
        private readonly ConnectedSolid solid;

        public List<Image> BGTiles = new();

        public BGTilesRenderer(ConnectedSolid solid)
        {
            this.solid = solid;

            // Never render above the solid.
            Depth = Math.Max(Depths.Player, solid.Depth) + 1;
        }

        public override void Render()
        {
            if (!solid.Visible)
                return;
            
            Vector2 position = Position;
            Position = solid.Position + solid.Shake;
            
            base.Render();
            solid.BGRender();

            Position = position;
        }
    }
    public BGTilesRenderer BGRenderer;

    public Vector2 GroupBoundsMin, GroupBoundsMax;
    public Vector2 GroupCenter => Position + GroupOffset + ((GroupBoundsMax - GroupBoundsMin) / 2f);

    public Rectangle GetGroupBoundsAt(Vector2 position)
    {
        Vector2 pos = position + GroupOffset + Shake;
        Vector2 size = GroupBoundsMax - GroupBoundsMin;

        return new Rectangle((int) pos.X, (int) pos.Y, (int) size.X, (int) size.Y);
    }

    // AllColliders contains colliders that didn't have a hitbox.
    public Hitbox[] Colliders, AllColliders;
    public Collider MasterCollider;

    // Auto-tiling stuff. (AllGroupTiles is similar to AllColliders)
    public int TileWidth, TileHeight;
    public bool[,] GroupTiles, AllGroupTiles;
    private AutoTileData[,] autoTileData;
    private bool wasAutoTiled = false;

    public Vector2 GroupOffset;

    public int MasterWidth, MasterHeight;
    public Vector2 MasterCenter => MasterCollider.Center + Position;

    public readonly List<Image> Tiles = [];
    public readonly List<Image> EdgeTiles = [];
    public readonly List<Image> CornerTiles = [];
    public readonly List<Image> InnerCornerTiles = [];
    public readonly List<Image> FillerTiles = [];

    private readonly DynamicData data;

    public ConnectedSolid(Vector2 position, int width, int height, bool safe)
        : base(position, width, height, safe)
    {
        GroupBoundsMin = new Vector2(X, Y);
        GroupBoundsMax = new Vector2(Right, Bottom);
        MasterWidth = width;
        MasterHeight = height;
        MasterCollider = Collider;

        data = new(typeof(Solid), this);
    }

    public override void Awake(Scene scene)
    {
        List<SolidExtension> extensions = [];
        FindExtensions(extensions);

        GroupOffset = new Vector2(GroupBoundsMin.X, GroupBoundsMin.Y) - Position;
        Colliders = new Hitbox[extensions.Count(ext => ext.HasHitbox) + 1];
        AllColliders = new Hitbox[extensions.Count + 1];

        int j = 0;
        for (int i = 0; i < extensions.Count; i++)
        {
            SolidExtension e = extensions[i];
            Vector2 offset = e.Position - Position;
            Hitbox hitbox = new(e.Width, e.Height, offset.X, offset.Y);
            if (e.HasHitbox)
            {
                Colliders[j] = hitbox;
                j++;
            }
            AllColliders[i] = hitbox;
            e.RemoveSelf();
            // You don't want disabled Solids hanging around in the level, so you remove them.
        }

        TileWidth = (int) ((GroupBoundsMax.X - GroupBoundsMin.X) / 8);
        TileHeight = (int) ((GroupBoundsMax.Y - GroupBoundsMin.Y) / 8);
        GroupTiles = new bool[TileWidth + 2, TileHeight + 2];
        AllGroupTiles = new bool[TileWidth + 2, TileHeight + 2];

        Colliders[^1] = (Hitbox) Collider;
        AllColliders[^1] = (Hitbox) Collider;

        Collider = new ColliderList(AllColliders);
        for (int x = 0; x < TileWidth + 2; x++)
            for (int y = 0; y < TileHeight + 2; y++)
                AllGroupTiles[x, y] = TileCollideWithGroup(x - 1, y - 1);

        Collider = new ColliderList(Colliders);
        for (int x = 0; x < TileWidth + 2; x++)
            for (int y = 0; y < TileHeight + 2; y++)
                GroupTiles[x, y] = TileCollideWithGroup(x - 1, y - 1);

        scene.Add(BGRenderer = new BGTilesRenderer(this));

        base.Awake(scene);
    }

    public override void Removed(Scene scene)
    {
        base.Removed(scene);
        BGRenderer.RemoveSelf();
    }
    
    public virtual void BGRender() { }
    
    #region Setup

    private void FindExtensions(List<SolidExtension> list)
    {
        foreach (SolidExtension extension in Scene.Tracker.GetEntities<SolidExtension>().Cast<SolidExtension>())
        {
            if (!extension.HasGroup &&
                (Scene.CollideCheck(new Rectangle((int) X - 1, (int) Y, (int) Width + 2, (int) Height), extension) ||
                Scene.CollideCheck(new Rectangle((int) X, (int) Y - 1, (int) Width, (int) Height + 2), extension)))
            {
                extension.AddToGroupAndFindChildren(extension, this, list);
            }
        }
    }
    
    private bool TileCollideWithGroup(int x, int y)
    {
        return CollideRect(new Rectangle((int) GroupBoundsMin.X + (x * 8), (int) GroupBoundsMin.Y + (y * 8), 8, 8));
    }
    
    #endregion

    #region Particles
    
    public void SpawnScrapeParticles(bool doOnX = true, bool doOnY = true)
    {
        Collidable = false;

        Level level = SceneAs<Level>();
        foreach (Hitbox hitbox in Colliders)
        {
            if (doOnX)
            {
                for (float t = 0; t < hitbox.Width; t += 8)
                {
                    Vector2 vecTop = Position + hitbox.Position + new Vector2(t, -1);
                    Vector2 vecBottom = Position + hitbox.Position + new Vector2(t, hitbox.Height + 1);
                    if (Scene.CollideCheckWhere<Solid>(NotAttached, vecTop))
                        level.ParticlesFG.Emit(ZipMover.P_Scrape, vecTop);
                    if (Scene.CollideCheckWhere<Solid>(NotAttached, vecBottom))
                        level.ParticlesFG.Emit(ZipMover.P_Scrape, vecBottom);
                }
            }

            if (doOnY)
            {
                for (float t = 0; t < hitbox.Height; t += 8)
                {
                    Vector2 vecLeft = Position + hitbox.Position + new Vector2(-1, t);
                    Vector2 vecRight = Position + hitbox.Position + new Vector2(hitbox.Width + 1, t);
                    if (Scene.CollideCheckWhere<Solid>(NotAttached, vecLeft))
                        level.ParticlesFG.Emit(ZipMover.P_Scrape, vecLeft);
                    if (Scene.CollideCheckWhere<Solid>(NotAttached, vecRight))
                        level.ParticlesFG.Emit(ZipMover.P_Scrape, vecRight);
                }
            }
        }

        Collidable = true;
        return;
        
        bool NotAttached(Solid solid)
            => solid != this
               && staticMovers.All(s => solid != s.Entity);
    }
    
    public void SpawnScrapeParticlesExcluding(Type[] ignores, bool doOnX = true, bool doOnY = true)
    {
        Collidable = false;

        Level level = SceneAs<Level>();
        foreach (Hitbox hitbox in Colliders)
        {
            if (doOnX)
            {
                for (float t = 0; t < hitbox.Width; t += 8)
                {
                    Vector2 vecTop = Position + hitbox.Position + new Vector2(t, -1);
                    Vector2 vecBottom = Position + hitbox.Position + new Vector2(t, hitbox.Height + 1);
                    if (Scene.CollideCheckWhere<Solid>(NotIgnoredAndNotAttached, vecTop))
                        level.ParticlesFG.Emit(ZipMover.P_Scrape, vecTop);
                    if (Scene.CollideCheckWhere<Solid>(NotIgnoredAndNotAttached, vecBottom))
                        level.ParticlesFG.Emit(ZipMover.P_Scrape, vecBottom);
                }
            }

            if (doOnY)
            {
                for (float t = 0; t < hitbox.Height; t += 8)
                {
                    Vector2 vecLeft = Position + hitbox.Position + new Vector2(-1, t);
                    Vector2 vecRight = Position + hitbox.Position + new Vector2(hitbox.Width + 1, t);
                    if (Scene.CollideCheckWhere<Solid>(NotIgnoredAndNotAttached, vecLeft))
                        level.ParticlesFG.Emit(ZipMover.P_Scrape, vecLeft);
                    if (Scene.CollideCheckWhere<Solid>(NotIgnoredAndNotAttached, vecRight))
                        level.ParticlesFG.Emit(ZipMover.P_Scrape, vecRight);
                }
            }
        }

        Collidable = true;
        return;

        bool NotIgnoredAndNotAttached(Solid solid)
            => !ignores.Contains(solid.GetType())
               && solid != this
               && staticMovers.All(s => solid != s.Entity);
    }
    
    #endregion

    #region Auto-tiling
    
    [Flags]
    private enum TileType
    {
        Filler = 0 << 2,
        Edge = 1 << 2,
        Corner = 2 << 2,
        InnerCorner = 3 << 2,
        
        TopEdge = Edge | 0,
        BottomEdge = Edge | 1,
        LeftEdge = Edge | 2,
        RightEdge = Edge | 3,
        
        TopLeftCorner = Corner | 0,
        TopRightCorner = Corner | 1,
        BottomLeftCorner = Corner | 2,
        BottomRightCorner = Corner | 3,
        
        TopLeftInnerCorner = InnerCorner | 0,
        TopRightInnerCorner = InnerCorner | 1,
        BottomLeftInnerCorner = InnerCorner | 2,
        BottomRightInnerCorner = InnerCorner | 3,
        
        TypeMask = 0b1100,
        VariantMask = 0b0011
    }

    private readonly struct AutoTileData(int textureXIndex, int textureYIndex, TileType type)
    {
        public readonly int TextureXIndex = textureXIndex, TextureYIndex = textureYIndex;

        public readonly TileType Type = type;
    }
    
    [Flags]
    private enum Sides
    {
        Up = 1,
        Down = 2,
        Left = 4,
        Right = 8,
        All = Up | Down | Left | Right
    }

    [Flags]
    private enum Corners
    {
        UpLeft = 1,
        UpRight = 2,
        DownLeft = 4,
        DownRight = 8,
        All = UpLeft | UpRight | DownLeft | DownRight
    }
    
    public static Tuple<MTexture[,], MTexture[,]> SetupCustomTileset(string path, bool withPrefix)
    {
        // why the fuck does it default to having the objects/ prefix?? hello???
        MTexture tileset = GFX.Game[(withPrefix ? "objects/" : "") + path];

        MTexture[,] overrideEdgeTiles = new MTexture[3, 3];
        MTexture[,] overrideInCornersTiles = new MTexture[2, 2];

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                overrideEdgeTiles[i, j] = tileset.GetSubtexture(i * 8, j * 8, 8, 8);
                if (i < 2 && j < 2)
                    overrideInCornersTiles[i, j] = tileset.GetSubtexture((i * 8) + 24, j * 8, 8, 8);
            }
        }
        return new Tuple<MTexture[,], MTexture[,]>(overrideEdgeTiles, overrideInCornersTiles);
    }
    
    /// <summary>
    /// Optionnal function to do auto-tiling for the entire group, with specified textures.
    /// If is called more than once, the auto-tiling logic will not be performed, but will use the data from previous calls to pick the right tiles.
    /// </summary>
    /// <returns>
    ///     A list of Images, in case tile manipulation is needed.
    /// </returns>
    /// <param name="edges"> Split tiles from a 24x24 texture, for the edges and corners. </param>
    /// <param name="innerCorners"> Split tiles from a 16x16 texture, for the inside turns. </param>
    /// <param name="addAsComponent"> Whether all the tiles should be added as components to this entity, therefore rendered automatically. </param>
    public List<Image> AutoTile(MTexture[,] edges, MTexture[,] innerCorners, bool storeTiles = true, bool addAsComponent = true)
    {
        return AutoTile(edges, innerCorners, out _, storeTiles, addAsComponent);
    }

    public List<Image> AutoTile(MTexture[,] edges, MTexture[,] innerCorners, out List<Image> bgTiles, bool storeTiles = true, bool addAsComponent = true)
    {
        List<Image> res = [];
        bgTiles = [];

        if (!wasAutoTiled)
        {
            autoTileData = new AutoTileData[TileWidth, TileHeight];
        }

        for (int x = 1; x < TileWidth + 1; x++)
        {
            for (int y = 1; y < TileHeight + 1; y++)
            {
                bool uncollidable = AllGroupTiles[x, y] && !GroupTiles[x, y];
                bool[,] tiles = uncollidable ? AllGroupTiles : GroupTiles;
                if (tiles[x, y])
                {
                    Image tile = AddTile(tiles, x, y, GroupOffset, edges, innerCorners, storeTiles, wasAutoTiled);
                    res.Add(tile);

                    if (addAsComponent)
                    {
                        if (uncollidable)
                        {
                            BGRenderer.Add(tile);
                        }
                        else
                        {
                            Add(tile);
                        }
                    }

                    if (uncollidable)
                    {
                        bgTiles.Add(tile);
                        tile.Color = Color.Gray;
                    }
                }
            }
        }
        wasAutoTiled = true;
        return res;
    }

    private Image AddTile(bool[,] tiles, int x, int y, Vector2 offset, MTexture[,] edges, MTexture[,] innerCorners, bool storeTiles, bool recalculateTile)
    {
        Image image;
        AutoTileData tileData;

        if (!recalculateTile)
        {
            bool up = tiles[x, y - 1];
            bool down = tiles[x, y + 1];
            bool left = tiles[x - 1, y];
            bool right = tiles[x + 1, y];

            bool upleft = tiles[x - 1, y - 1];
            bool upright = tiles[x + 1, y - 1];
            bool downleft = tiles[x - 1, y + 1];
            bool downright = tiles[x + 1, y + 1];

            image = AutoTileTexture(
                (Sides) Util.ToBitFlag(up, down, left, right),
                (Corners) Util.ToBitFlag(upleft, upright, downleft, downright),
                edges, innerCorners, out tileData);
            autoTileData[x - 1, y - 1] = tileData;
        }
        else
        {
            tileData = autoTileData[x - 1, y - 1];
            image = new Image(
                    (tileData.Type & TileType.TypeMask) == TileType.InnerCorner ?
                    innerCorners[tileData.TextureXIndex, tileData.TextureYIndex] :
                    edges[tileData.TextureXIndex, tileData.TextureYIndex]);
        }

        image.Position = new Vector2((x - 1) * 8f, (y - 1) * 8f) + offset;
        if (storeTiles)
        {
            Tiles.Add(image);
            switch (tileData.Type & TileType.TypeMask)
            {
                default:
                case TileType.Filler:
                    FillerTiles.Add(image);
                    break;
                
                case TileType.Edge:
                    EdgeTiles.Add(image);
                    break;
                
                case TileType.Corner:
                    CornerTiles.Add(image);
                    break;
                
                case TileType.InnerCorner:
                    InnerCornerTiles.Add(image);
                    break;
            }
        }
        return image;
    }

    private static Image AutoTileTexture(Sides sides, Corners corners, MTexture[,] edges, MTexture[,] innerCorners, out AutoTileData data)
    {
        data = sides == Sides.All
            ? corners switch
            {
                Corners.All ^ Corners.UpLeft => new AutoTileData(0, 0, TileType.TopLeftInnerCorner),
                Corners.All ^ Corners.UpRight => new AutoTileData(1, 0, TileType.TopRightInnerCorner),
                Corners.All ^ Corners.DownLeft => new AutoTileData(0, 1, TileType.BottomLeftInnerCorner),
                Corners.All ^ Corners.DownRight => new AutoTileData(1, 1, TileType.BottomRightInnerCorner),
                _ => new AutoTileData(1, 1, TileType.Filler)
            }
            : sides switch
            {
                Sides.All ^ Sides.Up => new AutoTileData(1, 0, TileType.TopEdge),
                Sides.All ^ Sides.Down => new AutoTileData(1, 2, TileType.BottomEdge),
                Sides.All ^ Sides.Left => new AutoTileData(0, 1, TileType.LeftEdge),
                Sides.All ^ Sides.Right => new AutoTileData(2, 1, TileType.RightEdge),
                Sides.Down | Sides.Right => new AutoTileData(0, 0, TileType.TopLeftCorner),
                Sides.Down | Sides.Left => new AutoTileData(2, 0, TileType.TopRightCorner),
                Sides.Up | Sides.Right => new AutoTileData(0, 2, TileType.BottomLeftCorner),
                Sides.Up | Sides.Left => new AutoTileData(2, 2, TileType.BottomRightCorner),
                _ => new AutoTileData(1, 1, TileType.Filler)
            };

        return new Image(
            (data.Type & TileType.TypeMask) == TileType.InnerCorner ?
                innerCorners[data.TextureXIndex, data.TextureYIndex] :
                edges[data.TextureXIndex, data.TextureYIndex]);
    }
    
    #endregion

    #region Movement
    
    /* 
     * Check for every Hitbox in base.Colliders, so that 
     * the player & other entities don't get sent to the ends of the group.
     */
    public override void MoveHExact(int move)
    {
        //base.MoveHExact(move);
        GetRiders();
        Player player = Scene.Tracker.GetEntity<Player>();

        if (player is not null && Input.MoveX.Value == Math.Sign(move) && Math.Sign(player.Speed.X) == Math.Sign(move) && !riders.Contains(player) && CollideCheck(player, Position + (Vector2.UnitX * move) - Vector2.UnitY))
        {
            player.MoveV(1f);
        }
        X += move;
        MoveStaticMovers(Vector2.UnitX * move);
        if (Collidable)
        {
            foreach (Actor entity in Scene.Tracker.GetEntities<Actor>().Cast<Actor>())
            {
                if (entity.AllowPushing)
                {
                    bool collidable = entity.Collidable;
                    entity.Collidable = true;

                    if (!entity.TreatNaive && CollideCheck(entity, Position))
                    {
                        foreach (Hitbox hitbox in Colliders)
                        {
                            if (hitbox.Collide(entity))
                            {
                                float left = X + hitbox.Left;
                                float right = X + hitbox.Right;

                                int moveH = (move <= 0) ? (int) (left - entity.Right) : (int) (right - entity.Left);

                                Collidable = false;
                                entity.MoveHExact(moveH, entity.SquishCallback, this);
                                entity.LiftSpeed = LiftSpeed;
                                Collidable = true;
                            }
                        }
                    }
                    else if (riders.Contains(entity))
                    {
                        Collidable = false;
                        if (entity.TreatNaive)
                        {
                            entity.NaiveMove(Vector2.UnitX * move);
                        }
                        else
                        {
                            entity.MoveHExact(move);
                        }
                        entity.LiftSpeed = LiftSpeed;
                        Collidable = true;
                    }
                    entity.Collidable = collidable;
                }
            }
        }
        riders.Clear();
    }

    public override void MoveVExact(int move)
    {
        GravityHelper.BeginOverride?.Invoke();

        GetRiders();

        Y += move;
        MoveStaticMovers(Vector2.UnitY * move);
        if (Collidable)
        {
            foreach (Actor entity in Scene.Tracker.GetEntities<Actor>().Cast<Actor>())
            {
                if (entity.AllowPushing)
                {
                    bool collidable = entity.Collidable;
                    entity.Collidable = true;
                    if (!entity.TreatNaive && CollideCheck(entity, Position))
                    {
                        foreach (Hitbox hitbox in Colliders)
                        {
                            if (hitbox.Collide(entity))
                            {
                                float top = Y + hitbox.Top;
                                float bottom = Y + hitbox.Bottom;

                                int moveV = (move <= 0) ? (int) (top - entity.Bottom) : (int) (bottom - entity.Top);
                                Collidable = false;
                                entity.MoveVExact(moveV, entity.SquishCallback, this);
                                entity.LiftSpeed = LiftSpeed;
                                Collidable = true;
                            }
                        }
                    }
                    else if (riders.Contains(entity))
                    {
                        Collidable = false;
                        if (entity.TreatNaive)
                        {
                            entity.NaiveMove(Vector2.UnitY * move);
                        }
                        else
                        {
                            entity.MoveVExact(move);
                        }
                        entity.LiftSpeed = LiftSpeed;
                        Collidable = true;
                    }
                    entity.Collidable = collidable;
                }
            }
        }
        riders.Clear();

        GravityHelper.EndOverride?.Invoke();
    }
    
    #endregion

    #region Culling
    
    public bool IsGroupVisible() => IsGroupVisibleAt(Position);

    public bool IsGroupVisibleAt(Vector2 pos)
    {
        Rectangle bounds = GetGroupBoundsAt(pos);
        return CullHelper.IsRectangleVisible(bounds.X, bounds.Y, bounds.Width, bounds.Height);
    }
    
    #endregion
    
    #region Misc
    
    public void AddLightOccluders(float alpha = 1f)
    {
        foreach (Hitbox hitbox in Colliders)
            Add(new LightOcclude(new Rectangle((int) hitbox.Position.X, (int) hitbox.Position.Y, (int) hitbox.Width, (int) hitbox.Height), alpha));
    }
    
    // maybe i shouldn't be worrying about how many `Draw.Rect` calls this is
    public void DrawFill(Color color, float inset)
    {
        if (!wasAutoTiled)
            return;

        inset = Calc.Clamp(inset, 0f, 8f);
        switch (Colliders.Length)
        {
            case <= 0:
                break;
            
            case 1:
                Hitbox hitbox = Colliders[0];
                if (hitbox.Width <= inset * 2f || hitbox.Height <= inset * 2f)
                    break;
                
                Draw.Rect(hitbox.Left + X + inset, hitbox.Top + Y + inset, hitbox.Width - inset * 2f, hitbox.Height - inset * 2f, color);
                break;
            
            default:
                for (int i = 0; i < TileWidth; i++)
                for (int j = 0; j < TileHeight; j++)
                {
                    Vector2 tilePosition = Position + GroupOffset + new Vector2(i * 8f, j * 8f);
                    AutoTileData data = autoTileData[i, j];
                    if (!GroupTiles[i + 1, j + 1])
                        continue;
                
                    switch (data.Type)
                    {
                        default:
                        case TileType.Filler:
                            Draw.Rect(tilePosition, 8f, 8f, color);
                            break;
                
                        case TileType.TopEdge:
                            Draw.Rect(tilePosition + Vector2.UnitY * inset, 8f, 8f - inset, color);
                            break;
                        case TileType.BottomEdge:
                            Draw.Rect(tilePosition, 8f, 8f - inset, color);
                            break;
                        case TileType.LeftEdge: 
                            Draw.Rect(tilePosition + Vector2.UnitX * inset, 8f - inset, 8f, color);
                            break;
                        case TileType.RightEdge:
                            Draw.Rect(tilePosition, 8f - inset, 8f, color);
                            break;

                        case TileType.TopLeftCorner:
                            Draw.Rect(tilePosition + Vector2.One * inset, 8f - inset, 8f - inset, color);
                            break;
                        case TileType.TopRightCorner:
                            Draw.Rect(tilePosition + Vector2.UnitY * inset, 8f - inset, 8f - inset, color);
                            break;
                        case TileType.BottomLeftCorner:
                            Draw.Rect(tilePosition + Vector2.UnitX * inset, 8f - inset, 8f - inset, color);
                            break;
                        case TileType.BottomRightCorner:
                            Draw.Rect(tilePosition, 8f - inset, 8f - inset, color);
                            break;

                        case TileType.TopLeftInnerCorner:
                            Draw.Rect(tilePosition + Vector2.UnitX * inset, 8f - inset, 8f, color);
                            Draw.Rect(tilePosition + Vector2.UnitY * inset, 8f, 8f - inset, color);
                            break;
                        case TileType.TopRightInnerCorner:
                            Draw.Rect(tilePosition, 8f - inset, 8f, color);
                            Draw.Rect(tilePosition + Vector2.UnitY * inset, 8f, 8f - inset, color);
                            break;
                        case TileType.BottomLeftInnerCorner:
                            Draw.Rect(tilePosition + Vector2.UnitX * inset, 8f - inset, 8f, color);
                            Draw.Rect(tilePosition, 8f, 8f - inset, color);
                            break;
                        case TileType.BottomRightInnerCorner:
                            Draw.Rect(tilePosition, 8f - inset, 8f, color);
                            Draw.Rect(tilePosition, 8f, 8f - inset, color);
                            break;
                    }
                }
                break;
        }
    }
    
    public void DrawOutline(Color color, float outset, float thickness)
    {
        if (!wasAutoTiled)
            return;

        outset = Calc.Clamp(outset, 0f, 8f);
        thickness = Calc.Clamp(thickness, 0f, 8f);
        switch (AllColliders.Length)
        {
            case <= 0:
                break;
            
            case 1:
                Hitbox hitbox = AllColliders[0];
                Draw.Rect(hitbox.Left + X - outset, hitbox.Top + Y - outset, hitbox.Width + 2f * outset, thickness, color);
                Draw.Rect(hitbox.Left + X - outset, hitbox.Bottom + Y + outset - thickness, hitbox.Width + 2f * outset, thickness, color);
                Draw.Rect(hitbox.Left + X - outset, hitbox.Top + Y - outset, thickness, hitbox.Height + 2f * outset, color);
                Draw.Rect(hitbox.Right + X + outset - thickness, hitbox.Top + Y - outset, thickness, hitbox.Height + 2f * outset, color);
                break;
            
            default:
                for (int i = 0; i < TileWidth; i++)
                for (int j = 0; j < TileHeight; j++)
                {
                    Vector2 tilePosition = Position + GroupOffset + new Vector2(i * 8f, j * 8f);
                    AutoTileData data = autoTileData[i, j];
                    if (!AllGroupTiles[i + 1, j + 1])
                        continue;
                
                    switch (data.Type)
                    {
                        default:
                        case TileType.Filler:
                            break;
                
                        case TileType.TopEdge:
                            Draw.Rect(tilePosition - Vector2.UnitY * outset, 8f, thickness, color);
                            break;
                        case TileType.BottomEdge:
                            Draw.Rect(tilePosition + Vector2.UnitY * (8f + outset - thickness), 8f, thickness, color);
                            break;
                        case TileType.LeftEdge: 
                            Draw.Rect(tilePosition - Vector2.UnitX * outset, thickness, 8f, color);
                            break;
                        case TileType.RightEdge:
                            Draw.Rect(tilePosition + Vector2.UnitX * (8f + outset - thickness), thickness, 8f, color);
                            break;

                        case TileType.TopLeftCorner:
                            Draw.Rect(tilePosition - Vector2.One * outset, 8f + outset, thickness, color);
                            Draw.Rect(tilePosition - Vector2.One * outset, thickness, 8f + outset, color);
                            break;
                        case TileType.TopRightCorner:
                            Draw.Rect(tilePosition - Vector2.UnitY * outset, 8f + outset, thickness, color);
                            Draw.Rect(tilePosition - Vector2.UnitY * outset + Vector2.UnitX * (8f + outset - thickness), thickness, 8f + outset, color);
                            break;
                        case TileType.BottomLeftCorner:
                            Draw.Rect(tilePosition - Vector2.UnitX * outset + Vector2.UnitY * (8f + outset - thickness), 8f + outset, thickness, color);
                            Draw.Rect(tilePosition - Vector2.UnitX * outset, thickness, 8f + outset, color);
                            break;
                        case TileType.BottomRightCorner:
                            Draw.Rect(tilePosition + Vector2.UnitY * (8f + outset - thickness), 8f + outset, thickness, color);
                            Draw.Rect(tilePosition + Vector2.UnitX * (8f + outset - thickness), thickness, 8f + outset, color);
                            break;
                    }
                }
                break;
        }
    }
    
    #endregion
}
