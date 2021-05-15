using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/SolidTilesGroup")]
    class SolidTilesGroup : ConnectedSolid {

        private bool triggered;

        public SolidTilesGroup(EntityData data, Vector2 offset) :
            this(data.Position + offset, data.Width, data.Height) { }

        public SolidTilesGroup(Vector2 position, int width, int height) :
            base(position, width, height, safe: true) {
            OnDashCollide = Dashed;
            Collidable = false;
        }

        public DashCollisionResults Dashed(Player player, Vector2 dir) {
            if (!triggered) {
                triggered = true;
                return DashCollisionResults.Rebound;
            }

            return DashCollisionResults.NormalCollision;
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);

            Level level = SceneAs<Level>();

            // If this entity doesn't affect any solid tiles, then it is useless, so get rid of it.
            if (!level.SolidTiles.Grid.Collide(Collider))
                RemoveSelf();

            Rectangle tileBounds = level.Session.MapData.TileBounds;

            int x = (int) (base.X / 8f) - tileBounds.Left;
            int y = (int) (base.Y / 8f) - tileBounds.Top;
            int w = (int) (Width / 8f);
            int h = (int) (Height / 8f);

            VirtualMap<char> levelTileTypes = new DynData<SolidTiles>(level.SolidTiles).Get<VirtualMap<char>>("tileTypes");
            VirtualMap<char> tileMap = new VirtualMap<char>(w, h, '0');

            for (int j = 0; j < h; j++) {
                for (int i = 0; i < w; i++) {
                    if (Collider.Collide(new Rectangle((int) X + i * 8, (int) Y + j * 8, 8, 8)))
                        tileMap[i, j] = levelTileTypes[i + x, j + y];
                }
            }

            Console.WriteLine(GenerateBetterColliderGrid(tileMap) == null);
        }



        // From FancyTileEntities : https://github.com/catapillie/FancyTileEntities/blob/dev/FancyTileEntities/Extensions.cs#L97
        // It's not perfectly optimized in terms of how many colliders there are, but it's better than nothing.
        public static ColliderList GenerateBetterColliderGrid(VirtualMap<char> tileMap, int cellWidth = 8, int cellHeight = 8) {
            ColliderList colliders = new ColliderList();
            List<Hitbox> prevCollidersOnX = new List<Hitbox>();
            for (int x = 0; x < tileMap.Columns; x++) {
                Hitbox prevCollider = null;
                for (int y = 0; y < tileMap.Rows; y++) {
                    if (tileMap.AnyInSegmentAtTile(x, y) && tileMap[x, y] != '0') {
                        if (prevCollider == null)
                            prevCollider = new Hitbox(cellWidth, cellHeight, x * cellWidth, y * cellHeight);
                        else
                            prevCollider = new Hitbox(cellWidth, prevCollider.Height + cellHeight, prevCollider.Position.X, prevCollider.Position.Y);
                    } else if (prevCollider != null) {
                        bool extendedOnX = false;
                        foreach (Hitbox hitbox in prevCollidersOnX) {
                            if (hitbox.Position.X + hitbox.Width == prevCollider.Position.X &&
                               hitbox.Position.Y == prevCollider.Position.Y &&
                               hitbox.Height == prevCollider.Height) {
                                // Weird check, but hey.
                                extendedOnX = true;
                                hitbox.Width += cellWidth;
                                prevCollider = null;
                                break;
                            }
                        }
                        if (!extendedOnX) {
                            colliders.Add(prevCollider);
                            prevCollidersOnX.Add(prevCollider);
                            prevCollider = null;
                        }
                    }
                }
            }
            return colliders.colliders.Length > 0 ? colliders : null;
        }
    }
}
