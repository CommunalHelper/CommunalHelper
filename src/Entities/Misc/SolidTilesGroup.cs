using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/SolidTilesGroup")]
    class SolidTilesGroup : ConnectedSolid {

        private Level level;
        private VirtualMap<char> tileMap;

        public SolidTilesGroup(EntityData data, Vector2 offset) :
            this(data.Position + offset, data.Width, data.Height) { }

        public SolidTilesGroup(Vector2 position, int width, int height) :
            base(position, width, height, safe: true) {
            Add(new LightOcclude());
            OnDashCollide = Dashed;
        }

        private DashCollisionResults Dashed(Player player, Vector2 dir) {
            Detach(true);
            return DashCollisionResults.Rebound;
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);

            level = SceneAs<Level>();

            // If this entity doesn't affect any solid tiles, then it is useless, so get rid of it.
            if (!level.SolidTiles.Grid.Collide(Collider))
                RemoveSelf();

            Rectangle tileBounds = level.Session.MapData.TileBounds;

            int x = (int) (Left / 8f) - tileBounds.Left;
            int y = (int) (Top / 8f) - tileBounds.Top;
            int w = (int) (Width / 8f);
            int h = (int) (Height / 8f);

            VirtualMap<char> levelTileTypes = new DynData<SolidTiles>(level.SolidTiles).Get<VirtualMap<char>>("tileTypes");
            tileMap = new VirtualMap<char>(w, h, '0');

            for (int j = 0; j < h; j++) {
                for (int i = 0; i < w; i++) {
                    if (CollideRect(new Rectangle((int) Left + i * 8, (int) Top + j * 8, 8, 8))) {
                        tileMap[i, j] = levelTileTypes[i + x, j + y];
                        level.SolidTiles.Grid.Data[i + x, y + j] = false;
                    }
                }
            }

            SetNewColliderList(GenerateColliderGrid(tileMap), TopLeft);
        }

        public void Detach(bool autotile = true) {
            Rectangle tileBounds = level.Session.MapData.TileBounds;

            int x = (int) (X / 8f) - tileBounds.Left;
            int y = (int) (Y / 8f) - tileBounds.Top;
            int w = tileMap.Columns;
            int h = tileMap.Rows;

            if (autotile) {
                Add(GFX.FGAutotiler.GenerateMap(tileMap, new Autotiler.Behaviour {
                    EdgesExtend = false,
                    EdgesIgnoreOutOfLevel = false,
                    PaddingIgnoreOutOfLevel = false
                }).TileGrid);

                VirtualMap<char> levelTileMap = new DynData<SolidTiles>(level.SolidTiles).Get<VirtualMap<char>>("tileTypes");

                for (int j = 0; j < h; j++) {
                    for (int i = 0; i < w; i++) {
                        if (tileMap[i, j] != '0') {
                            levelTileMap[i + x, j + y] = '0';
                        }
                    }
                }

                level.SolidTiles.Tiles.Tiles = GFX.FGAutotiler.GenerateMap(levelTileMap, paddingIgnoreOutOfLevel: true).TileGrid.Tiles;
            } else {
                TileGrid tileGrid = new TileGrid(8, 8, w, h);

                for (int j = 0; j < h; j++) {
                    for (int i = 0; i < w; i++) {
                        if (tileMap[i, j] != '0') {
                            tileGrid.Tiles[i, j] = level.SolidTiles.Tiles.Tiles[i + x, j + y];
                            level.SolidTiles.Tiles.Tiles[i + x, j + y] = null;
                        }
                    }
                }

                Add(tileGrid);
            }
        }

        public static ColliderList GenerateColliderGrid(VirtualMap<char> tileMap, int cellWidth = 8, int cellHeight = 8) {
            ColliderList colliders = new ColliderList();
            VirtualMap<char> temp = tileMap.Clone();

            for (int x = 0; x < tileMap.Columns; x++) {
                List<Hitbox> prevColliders = new List<Hitbox>();
                Hitbox currentPrevCollider = null;
                for (int y = 0; y < tileMap.Rows + 1; y++) {

                    // basic vertical expansion of the colliders.
                    if (temp[x, y] != '0') {
                        temp[x, y] = '0';
                        if (currentPrevCollider == null) {
                            currentPrevCollider = new Hitbox(cellWidth, cellHeight, x * cellWidth, y * cellHeight);
                        } else {
                            currentPrevCollider.Height += cellHeight;
                        }
                    } else if (currentPrevCollider != null) {
                        prevColliders.Add((Hitbox) currentPrevCollider.Clone());
                        currentPrevCollider = null;
                    }
                }

                // once we are done with them, we can extend them horizontally to the right as much as possible.
                if (prevColliders.Count > 0) {
                    foreach (Hitbox prevCollider in prevColliders) {
                        int cx = (int) prevCollider.Position.X / cellWidth;
                        int cy = (int) prevCollider.Position.Y / cellHeight;
                        int cw = (int) prevCollider.Width / cellWidth;
                        int ch = (int) prevCollider.Height / cellHeight;

                        while (cx + cw < temp.Columns) {
                            bool canExtend = true;
                            for (int j = cy; j < cy + ch; j++) {
                                if (temp[cx + cw, j] == '0') {
                                    canExtend = false;
                                    break;
                                }
                            }
                            if (canExtend) {
                                for (int j = cy; j < cy + ch; j++) {
                                    temp[cx + cw, j] = '0';
                                }
                                prevCollider.Width += cellWidth;
                                cw++;
                            } else
                                break;
                        }

                        colliders.Add(prevCollider);
                    }
                }
            }

            return colliders.colliders.Length > 0 ? colliders : null;
        }
    }

}
