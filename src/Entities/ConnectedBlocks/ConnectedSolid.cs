using Celeste.Mod.CommunalHelper.Imports;
using MonoMod.Utils;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.CommunalHelper;

public class ConnectedSolid : Solid
{
    public class BGTilesRenderer : Entity
    {
        private readonly ConnectedSolid solid;

        public List<Image> BGTiles = new();

        public BGTilesRenderer(ConnectedSolid solid)
        {
            this.solid = solid;

            // Never rendering above solid.
            Depth = Math.Max(Depths.Player, solid.Depth) + 1;
        }

        public override void Update()
        {
            Visible = solid.Visible;
            base.Update();
        }

        public override void Render()
        {
            Position = solid.Position + solid.Shake;
            base.Render();
        }
    }
    public BGTilesRenderer BGRenderer;

    public Vector2 GroupBoundsMin, GroupBoundsMax;
    public Vector2 GroupCenter => Position + GroupOffset + ((GroupBoundsMax - GroupBoundsMin) / 2f);

    // AllColliders contains colliders that didn't have a hitbox.
    public Hitbox[] Colliders, AllColliders;
    public Collider MasterCollider;

    // Auto-tiling stuff. (AllGroupTiles is similar to AllColliders)
    public bool[,] GroupTiles, AllGroupTiles;
    private AutoTileData[,] autoTileData;
    private bool wasAutoTiled = false;

    private enum TileType
    {
        Edge, Corner, InnerCorner, Filler
    }

    private struct AutoTileData
    {
        public readonly int X, Y;
        public readonly TileType Type;

        public AutoTileData(int x, int y, TileType type)
        {
            X = x;
            Y = y;
            Type = type;
        }
    }

    public Vector2 GroupOffset;

    public int MasterWidth, MasterHeight;
    public Vector2 MasterCenter => MasterCollider.Center + Position;

    public List<Image> Tiles = new();
    public List<Image> EdgeTiles = new();
    public List<Image> CornerTiles = new();
    public List<Image> InnerCornerTiles = new();
    public List<Image> FillerTiles = new();

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
        List<SolidExtension> extensions = new();
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

        int tWidth = (int) ((GroupBoundsMax.X - GroupBoundsMin.X) / 8);
        int tHeight = (int) ((GroupBoundsMax.Y - GroupBoundsMin.Y) / 8);
        GroupTiles = new bool[tWidth + 2, tHeight + 2];
        AllGroupTiles = new bool[tWidth + 2, tHeight + 2];

        Colliders[Colliders.Length - 1] = (Hitbox) Collider;
        AllColliders[AllColliders.Length - 1] = (Hitbox) Collider;

        Collider = new ColliderList(AllColliders);
        for (int x = 0; x < tWidth + 2; x++)
            for (int y = 0; y < tHeight + 2; y++)
                AllGroupTiles[x, y] = TileCollideWithGroup(x - 1, y - 1);

        Collider = new ColliderList(Colliders);
        for (int x = 0; x < tWidth + 2; x++)
            for (int y = 0; y < tHeight + 2; y++)
                GroupTiles[x, y] = TileCollideWithGroup(x - 1, y - 1);

        scene.Add(BGRenderer = new BGTilesRenderer(this));

        base.Awake(scene);
    }

    public override void Removed(Scene scene)
    {
        base.Removed(scene);
        BGRenderer.RemoveSelf();
    }

    public static Tuple<MTexture[,], MTexture[,]> SetupCustomTileset(string path)
    {
        MTexture tileset = GFX.Game["objects/" + path];

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

    private void FindExtensions(List<SolidExtension> list)
    {
        foreach (SolidExtension extension in Scene.Tracker.GetEntities<SolidExtension>())
        {
            if (!extension.HasGroup &&
                (Scene.CollideCheck(new Rectangle((int) X - 1, (int) Y, (int) Width + 2, (int) Height), extension) ||
                Scene.CollideCheck(new Rectangle((int) X, (int) Y - 1, (int) Width, (int) Height + 2), extension)))
            {
                extension.AddToGroupAndFindChildren(extension, this, list);
            }
        }
    }

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
                    if (Scene.CollideCheck<Solid>(vecTop))
                        level.ParticlesFG.Emit(ZipMover.P_Scrape, vecTop);
                    if (Scene.CollideCheck<Solid>(vecBottom))
                        level.ParticlesFG.Emit(ZipMover.P_Scrape, vecBottom);
                }
            }

            if (doOnY)
            {
                for (float t = 0; t < hitbox.Height; t += 8)
                {
                    Vector2 vecLeft = Position + hitbox.Position + new Vector2(-1, t);
                    Vector2 vecRight = Position + hitbox.Position + new Vector2(hitbox.Width + 1, t);
                    if (Scene.CollideCheck<Solid>(vecLeft))
                        level.ParticlesFG.Emit(ZipMover.P_Scrape, vecLeft);
                    if (Scene.CollideCheck<Solid>(vecRight))
                        level.ParticlesFG.Emit(ZipMover.P_Scrape, vecRight);
                }
            }
        }

        Collidable = true;
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
        int tWidth = (int) ((GroupBoundsMax.X - GroupBoundsMin.X) / 8);
        int tHeight = (int) ((GroupBoundsMax.Y - GroupBoundsMin.Y) / 8);

        List<Image> res = new();
        bgTiles = new List<Image>();

        if (!wasAutoTiled)
        {
            autoTileData = new AutoTileData[tWidth, tHeight];
        }

        for (int x = 1; x < tWidth + 1; x++)
        {
            for (int y = 1; y < tHeight + 1; y++)
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

    private Image AddTile(bool[,] tiles, int x, int y, Vector2 offset, MTexture[,] edges, MTexture[,] innerCorners, bool storeTiles, bool ignoreTilingLogic)
    {
        Image image;
        Vector2 tilePos = new(x - 1, y - 1);
        AutoTileData tileData;

        if (!ignoreTilingLogic)
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
            autoTileData[(int) tilePos.X, (int) tilePos.Y] = tileData;
        }
        else
        {
            tileData = autoTileData[(int) tilePos.X, (int) tilePos.Y];
            image = new Image(
                    tileData.Type == TileType.InnerCorner ?
                    innerCorners[tileData.X, tileData.Y] :
                    edges[tileData.X, tileData.Y]);
        }

        image.Position = (tilePos * 8f) + offset;
        if (storeTiles)
        {
            Tiles.Add(image);
            switch (tileData.Type)
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

    private Image AutoTileTexture(Sides sides, Corners corners, MTexture[,] edges, MTexture[,] innerCorners, out AutoTileData data)
    {
        data = sides == Sides.All
            ? corners switch
            {
                Corners.All ^ Corners.UpLeft => new AutoTileData(0, 0, TileType.InnerCorner),
                Corners.All ^ Corners.UpRight => new AutoTileData(1, 0, TileType.InnerCorner),
                Corners.All ^ Corners.DownLeft => new AutoTileData(0, 1, TileType.InnerCorner),
                Corners.All ^ Corners.DownRight => new AutoTileData(1, 1, TileType.InnerCorner),
                _ => new AutoTileData(1, 1, TileType.Filler)
            }
            : sides switch
            {
                Sides.All ^ Sides.Up => new AutoTileData(1, 0, TileType.Edge),
                Sides.All ^ Sides.Down => new AutoTileData(1, 2, TileType.Edge),
                Sides.All ^ Sides.Left => new AutoTileData(0, 1, TileType.Edge),
                Sides.All ^ Sides.Right => new AutoTileData(2, 1, TileType.Edge),
                Sides.Down | Sides.Right => new AutoTileData(0, 0, TileType.Corner),
                Sides.Down | Sides.Left => new AutoTileData(2, 0, TileType.Corner),
                Sides.Up | Sides.Right => new AutoTileData(0, 2, TileType.Corner),
                Sides.Up | Sides.Left => new AutoTileData(2, 2, TileType.Corner),
                _ => new AutoTileData(1, 1, TileType.Filler)
            };

        return new Image(
            data.Type == TileType.InnerCorner ?
            innerCorners[data.X, data.Y] :
            edges[data.X, data.Y]);
    }

    private bool TileCollideWithGroup(int x, int y)
    {
        return CollideRect(new Rectangle((int) GroupBoundsMin.X + (x * 8), (int) GroupBoundsMin.Y + (y * 8), 8, 8));
    }

    /* 
     * Check for every Hitbox in base.Colliders, so that 
     * the player & other entities don't get sent to the ends of the group.
     */
    public override void MoveHExact(int move)
    {
        //base.MoveHExact(move);
        GetRiders();
        Player player = Scene.Tracker.GetEntity<Player>();

        HashSet<Actor> riders = data.Get<HashSet<Actor>>("riders");

        if (player != null && Input.MoveX.Value == Math.Sign(move) && Math.Sign(player.Speed.X) == Math.Sign(move) && !riders.Contains(player) && CollideCheck(player, Position + (Vector2.UnitX * move) - Vector2.UnitY))
        {
            player.MoveV(1f);
        }
        X += move;
        MoveStaticMovers(Vector2.UnitX * move);
        if (Collidable)
        {
            foreach (Actor entity in Scene.Tracker.GetEntities<Actor>())
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

        //base.MoveVExact(move);
        GetRiders();

        HashSet<Actor> riders = data.Get<HashSet<Actor>>("riders");

        Y += move;
        MoveStaticMovers(Vector2.UnitY * move);
        if (Collidable)
        {
            foreach (Actor entity in Scene.Tracker.GetEntities<Actor>())
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
}
