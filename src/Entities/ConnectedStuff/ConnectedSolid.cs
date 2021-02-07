using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper {
    class ConnectedSolid : Solid {
        public Vector2 GroupBoundsMin, GroupBoundsMax;
        public Vector2 GroupCenter => Position + GroupOffset + (GroupBoundsMax - GroupBoundsMin) / 2f;

        public Hitbox[] Colliders;
        public Collider MasterCollider;

        // Auto-tiling stuff.
        public bool[,] GroupTiles;
        private AutoTileData[,] autoTileData;
        bool wasAutoTiled = false;
        private enum TileType {
            Edge, Corner, InnerCorner, Filler
        }
        private struct AutoTileData {
            public AutoTileData(int x_, int y_, TileType type_) {
                x = x_;
                y = y_;
                type = type_;
            }
            public int x, y;
            public TileType type;
        }

        public Vector2 GroupOffset;

        public int MasterWidth, MasterHeight;
        public Vector2 MasterCenter => MasterCollider.Center + Position;

        public List<Image> Tiles = new List<Image>();
        public List<Image> EdgeTiles = new List<Image>();
        public List<Image> CornerTiles = new List<Image>();
        public List<Image> InnerCornerTiles = new List<Image>();
        public List<Image> FillerTiles = new List<Image>();

        public ConnectedSolid(Vector2 position, int width, int height, bool safe)
            : base(position, width, height, safe) {
            GroupBoundsMin = new Vector2(X, Y);
            GroupBoundsMax = new Vector2(Right, Bottom);
            MasterWidth = width;
            MasterHeight = height;
            MasterCollider = Collider;
        }

        public override void Awake(Scene scene) {


            List<SolidExtension> extensions = new List<SolidExtension>();
            FindExtensions(this, extensions);

            GroupOffset = new Vector2(GroupBoundsMin.X, GroupBoundsMin.Y) - Position;
            Colliders = new Hitbox[extensions.Count + 1];
            for (int i = 0; i < extensions.Count; i++) {
                SolidExtension e = extensions[i];
                Vector2 offset = e.Position - Position;
                Colliders[i] = new Hitbox(e.Width, e.Height, offset.X, offset.Y);
                e.RemoveSelf();
                // You don't want disabled Solids hanging around in the level, so you remove them.
            }
            Colliders[Colliders.Length - 1] = new Hitbox(Collider.Width, Collider.Height);
            Collider = new ColliderList(Colliders);

            base.Awake(scene);

        }

        public static Tuple<MTexture[,], MTexture[,]> SetupCustomTileset(string path) {
            MTexture tileset = GFX.Game["objects/" + path];

            MTexture[,] overrideEdgeTiles = new MTexture[3, 3];
            MTexture[,] overrideInCornersTiles = new MTexture[2, 2];

            for (int i = 0; i < 3; i++) {
                for (int j = 0; j < 3; j++) {
                    overrideEdgeTiles[i, j] = tileset.GetSubtexture(i * 8, j * 8, 8, 8);
                    if (i < 2 && j < 2)
                        overrideInCornersTiles[i, j] = tileset.GetSubtexture(i * 8 + 24, j * 8, 8, 8);
                }
            }
            return new Tuple<MTexture[,], MTexture[,]>(overrideEdgeTiles, overrideInCornersTiles);
        }

        private void FindExtensions(ConnectedSolid master, List<SolidExtension> list) {
            foreach (SolidExtension extension in Scene.Tracker.GetEntities<SolidExtension>()) {
                if (!extension.HasGroup &&
                    (Scene.CollideCheck(new Rectangle((int) X - 1, (int) Y, (int) Width + 2, (int) master.Height), extension) ||
                    Scene.CollideCheck(new Rectangle((int) X, (int) Y - 1, (int) Width, (int) Height + 2), extension))) {
                    extension.AddToGroupAndFindChildren(extension, master, list);
                }
            }
        }

        public void SpawnScrapeParticles(bool doOnX = true, bool doOnY = true) {
            Collidable = false;

            Level level = SceneAs<Level>();
            foreach (Hitbox hitbox in Colliders) {
                if (doOnX) {
                    for (float t = 0; t < hitbox.Width; t += 6) {
                        Vector2 vecTop = Position + hitbox.Position + new Vector2(t, -1);
                        Vector2 vecBottom = Position + hitbox.Position + new Vector2(t, hitbox.Height + 1);
                        if (Scene.CollideCheck<Solid>(vecTop))
                            level.ParticlesFG.Emit(ZipMover.P_Scrape, vecTop, 0);
                        if (Scene.CollideCheck<Solid>(vecBottom))
                            level.ParticlesFG.Emit(ZipMover.P_Scrape, vecBottom, 0);
                    }
                }

                if (doOnY) {
                    for (float t = 0; t < hitbox.Height; t += 6) {
                        Vector2 vecLeft = Position + hitbox.Position + new Vector2(-1, t);
                        Vector2 vecRight = Position + hitbox.Position + new Vector2(hitbox.Width + 1, t);
                        if (Scene.CollideCheck<Solid>(vecLeft))
                            level.ParticlesFG.Emit(ZipMover.P_Scrape, vecLeft, 0);
                        if (Scene.CollideCheck<Solid>(vecRight))
                            level.ParticlesFG.Emit(ZipMover.P_Scrape, vecRight, 0);
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
        public List<Image> AutoTile(MTexture[,] edges, MTexture[,] innerCorners, bool storeTiles = true, bool addAsComponent = true) {

            int tWidth = (int) ((GroupBoundsMax.X - GroupBoundsMin.X) / 8.0f);
            int tHeight = (int) ((GroupBoundsMax.Y - GroupBoundsMin.Y) / 8.0f);

            List<Image> res = new List<Image>();
            if (!wasAutoTiled) {
                autoTileData = new AutoTileData[tWidth, tHeight];
                GroupTiles = new bool[tWidth + 2, tHeight + 2];

                for (int x = 0; x < tWidth + 2; x++) {
                    for (int y = 0; y < tHeight + 2; y++) {
                        GroupTiles[x, y] = TileCollideWithGroup(x - 1, y - 1);
                    }
                }
            }
            for (int x = 1; x < tWidth + 1; x++) {
                for (int y = 1; y < tHeight + 1; y++) {
                    if (GroupTiles[x, y]) {
                        Image tile = AddTile(GroupTiles, x, y, GroupOffset, edges, innerCorners, storeTiles, wasAutoTiled);
                        res.Add(tile);

                        if (addAsComponent)
                            Add(tile);
                    }
                }
            }
            wasAutoTiled = true;
            return res;
        }

        private Image AddTile(bool[,] tiles, int x, int y, Vector2 offset, MTexture[,] edges, MTexture[,] innerCorners, bool storeTiles, bool ignoreTilingLogic) {
            Image image;
            Vector2 tilePos = new Vector2(x - 1, y - 1);
            AutoTileData tileData;

            if (!ignoreTilingLogic) {

                bool up = tiles[x, y - 1];
                bool down = tiles[x, y + 1];
                bool left = tiles[x - 1, y];
                bool right = tiles[x + 1, y];

                bool upleft = tiles[x - 1, y - 1];
                bool upright = tiles[x + 1, y - 1];
                bool downleft = tiles[x - 1, y + 1];
                bool downright = tiles[x + 1, y + 1];

                image = AutoTileTexture(
                    up, down, left, right,
                    upleft, upright, downleft, downright,
                    edges, innerCorners,
                    out tileData);
                autoTileData[(int) tilePos.X, (int) tilePos.Y] = tileData;

            } else {

                tileData = autoTileData[(int) tilePos.X, (int) tilePos.Y];
                image = new Image(
                        tileData.type == TileType.InnerCorner ?
                        innerCorners[tileData.x, tileData.y] :
                        edges[tileData.x, tileData.y]);
            }

            image.Position = (tilePos * 8f) + offset;
            if (storeTiles) {
                Tiles.Add(image);
                switch (tileData.type) {

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

        private Image AutoTileTexture(
            bool up, bool down, bool left, bool right,
            bool upleft, bool upright, bool downleft, bool downright,
            MTexture[,] edges, MTexture[,] innerCorners,
            out AutoTileData data) {
            bool completelyClosed = up && down && left && right;

            data = new AutoTileData(1, 1, TileType.Filler);
            if (!(completelyClosed && upright && upleft && downright && downleft)) {
                if (completelyClosed) {

                    if (!upleft) { data.x = 0; data.y = 0; data.type = TileType.InnerCorner; } else if (!upright) { data.x = 1; data.y = 0; data.type = TileType.InnerCorner; } else if (!downleft) { data.x = 0; data.y = 1; data.type = TileType.InnerCorner; } else if (!downright) { data.x = 1; data.y = 1; data.type = TileType.InnerCorner; }
                } else {

                    if (!up && down && left && right) { data.x = 1; data.y = 0; data.type = TileType.Edge; } else if (up && !down && left && right) { data.x = 1; data.y = 2; data.type = TileType.Edge; } else if (up && down && !left && right) { data.x = 0; data.y = 1; data.type = TileType.Edge; } else if (up && down && left && !right) { data.x = 2; data.y = 1; data.type = TileType.Edge; } else if (!up && down && !left && right) { data.x = 0; data.y = 0; data.type = TileType.Corner; } else if (!up && down && left && !right) { data.x = 2; data.y = 0; data.type = TileType.Corner; } else if (up && !down && !left && right) { data.x = 0; data.y = 2; data.type = TileType.Corner; } else if (up && !down && left && !right) { data.x = 2; data.y = 2; data.type = TileType.Corner; }
                }
            }

            return new Image(
                data.type == TileType.InnerCorner ?
                innerCorners[data.x, data.y] :
                edges[data.x, data.y]);
        }

        private bool TileCollideWithGroup(int x, int y) {
            return CollideRect(new Rectangle((int) GroupBoundsMin.X + x * 8, (int) GroupBoundsMin.Y + y * 8, 8, 8));
        }

        /* 
         * Check for every Hitbox in base.Colliders, so that 
         * the player & other entities don't get sent to the ends of the group.
         */
        public override void MoveHExact(int move) {
            //base.MoveHExact(move);
            GetRiders();
            Player player = Scene.Tracker.GetEntity<Player>();

            HashSet<Actor> riders = new DynData<Solid>(this).Get<HashSet<Actor>>("riders");

            if (player != null && Input.MoveX.Value == Math.Sign(move) && Math.Sign(player.Speed.X) == Math.Sign(move) && !riders.Contains(player) && CollideCheck(player, Position + Vector2.UnitX * move - Vector2.UnitY)) {
                player.MoveV(1f);
            }
            X += move;
            MoveStaticMovers(Vector2.UnitX * move);
            if (Collidable) {
                foreach (Actor entity in Scene.Tracker.GetEntities<Actor>()) {
                    if (entity.AllowPushing) {
                        bool collidable = entity.Collidable;
                        entity.Collidable = true;

                        if (!entity.TreatNaive && CollideCheck(entity, Position)) {
                            foreach (Hitbox hitbox in Colliders) {
                                if (hitbox.Collide(entity)) {
                                    float left = X + hitbox.Left;
                                    float right = X + hitbox.Right;

                                    int moveH = (move <= 0) ? (int) (left - entity.Right) : (int) (right - entity.Left);

                                    Collidable = false;
                                    entity.MoveHExact(moveH, null, this);
                                    entity.LiftSpeed = LiftSpeed;
                                    Collidable = true;
                                }
                            }
                        } else if (riders.Contains(entity)) {
                            Collidable = false;
                            if (entity.TreatNaive) {
                                entity.NaiveMove(Vector2.UnitX * move);
                            } else {
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

        public override void MoveVExact(int move) {
            //base.MoveVExact(move);
            GetRiders();

            HashSet<Actor> riders = new DynData<Solid>(this).Get<HashSet<Actor>>("riders");

            Y += move;
            MoveStaticMovers(Vector2.UnitY * move);
            if (Collidable) {
                foreach (Actor entity in Scene.Tracker.GetEntities<Actor>()) {
                    if (entity.AllowPushing) {
                        bool collidable = entity.Collidable;
                        entity.Collidable = true;
                        if (!entity.TreatNaive && CollideCheck(entity, Position)) {
                            foreach (Hitbox hitbox in Colliders) {

                                if (hitbox.Collide(entity)) {
                                    float top = Y + hitbox.Top;
                                    float bottom = Y + hitbox.Bottom;

                                    int moveV = (move <= 0) ? (int) (top - entity.Bottom) : (int) (bottom - entity.Top);
                                    Collidable = false;
                                    entity.MoveVExact(moveV, entity.SquishCallback, this);
                                    entity.LiftSpeed = LiftSpeed;
                                    Collidable = true;
                                }
                            }
                        } else if (riders.Contains(entity)) {
                            Collidable = false;
                            if (entity.TreatNaive) {
                                entity.NaiveMove(Vector2.UnitY * move);
                            } else {
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
        }
    }
}
