using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CommunalHelper
{
    class ConnectedSolid : Solid
    {
        public Point GroupBoundsMin, GroupBoundsMax;

        public Hitbox[] Colliders;

        public bool[,] GroupTiles;
        public Vector2 GroupOffset;

        public int MasterWidth, MasterHeight;

        public ConnectedSolid(Vector2 position, int width, int height, bool safe)
            : base(position, width, height, safe)
        {
            GroupBoundsMin = new Point((int)X, (int)Y);
            GroupBoundsMax = new Point((int)Right, (int)Bottom);
            MasterWidth = width; MasterHeight = height;
        }

        public override void Awake(Scene scene)
        {
            base.Awake(scene);

            List<SolidExtension> extensions = new List<SolidExtension>();
            FindExtensions(this, extensions);

            GroupOffset = new Vector2(GroupBoundsMin.X, GroupBoundsMin.Y) - base.Position;
            Colliders = new Hitbox[extensions.Count + 1];
            for (int i = 0; i < extensions.Count; i++)
            {
                SolidExtension e = extensions[i];
                Vector2 offset = e.Position - Position;
                Colliders[i] = new Hitbox(e.Width, e.Height, offset.X, offset.Y);
            }
            Colliders[Colliders.Length - 1] = new Hitbox(Collider.Width, Collider.Height);
            base.Collider = new ColliderList(Colliders);

            foreach (SolidExtension e in extensions) e.RemoveSelf();
            // You don't want disabled Solids hanging around in the level, so you remove them.
        }

        private void FindExtensions(ConnectedSolid master, List<SolidExtension> list)
        {
            foreach (SolidExtension extension in base.Scene.Tracker.GetEntities<SolidExtension>())
            {
                if (!extension.HasGroup &&
                    (base.Scene.CollideCheck(new Rectangle((int)X - 1, (int)Y, (int)Width + 2, (int)master.Height), extension) ||
                    base.Scene.CollideCheck(new Rectangle((int)X, (int)Y - 1, (int)Width, (int)Height + 2), extension)))
                {
                    extension.AddToGroupAndFindChildren(extension, master, list);
                }
            }
        }

        /// <summary>
        /// Optionnal function to do auto tiling for the entire group, with specified textures.
        /// </summary>
        /// <param name="edges"> Split tiles from a 24x24 texture, for the edges and corners. </param>
        /// <param name="innerCorners"> Split tiles from a 16x16 texture, for the inside turns. </param>
        public void AutoTile(MTexture[,] edges, MTexture[,] innerCorners)
        {
            int tWidth = (int)((GroupBoundsMax.X - GroupBoundsMin.X) / 8.0f);
            int tHeight = (int)((GroupBoundsMax.Y - GroupBoundsMin.Y) / 8.0f);

            GroupTiles = new bool[tWidth + 2, tHeight + 2];
            for (int x = 0; x < tWidth + 2; x++)
            {
                for (int y = 0; y < tHeight + 2; y++)
                {
                    GroupTiles[x, y] = TileCollideWithGroup(x - 1, y - 1);
                }
            }
            GroupOffset = new Vector2(GroupBoundsMin.X, GroupBoundsMin.Y) - base.Position;
            for (int x = 1; x < tWidth + 1; x++)
            {
                for (int y = 1; y < tHeight + 1; y++)
                {
                    if (GroupTiles[x, y]) AddTile(GroupTiles, x, y, GroupOffset, edges, innerCorners);
                }
            }
        }

        private void AddTile(bool[,] tiles, int x, int y, Vector2 offset, MTexture[,] edges, MTexture[,] innerCorners)
        {
            bool up = tiles[x, y - 1];
            bool down = tiles[x, y + 1];
            bool left = tiles[x - 1, y];
            bool right = tiles[x + 1, y];

            bool upleft = tiles[x - 1, y - 1];
            bool upright = tiles[x + 1, y - 1];
            bool downleft = tiles[x - 1, y + 1];
            bool downright = tiles[x + 1, y + 1];

            MTexture texture = AutoTileTexture(
                up, down, left, right,
                upleft, upright, downleft, downright,
                edges, innerCorners);

            if (texture == null) return;

            // Apply the texture
            Image image = new Image(texture)
            {
                Position = (new Vector2(x - 1, y - 1) * 8f) + offset
            };
            Add(image);
        }

        private MTexture AutoTileTexture(bool up, bool down, bool left, bool right, bool upleft, bool upright, bool downleft, bool downright, MTexture[,] edges, MTexture[,] innerCorners)
        {
            if (up && down && left && right && upleft && upright && downleft && downright) return null;

            if (!up && down && left && right) return edges[1, 0];
            if (up && !down && left && right) return edges[1, 2];
            if (up && down && !left && right) return edges[0, 1];
            if (up && down && left && !right) return edges[2, 1];

            if (!up && down && !left && right) return edges[0, 0];
            if (!up && down && left && !right) return edges[2, 0];
            if (up && !down && !left && right) return edges[0, 2];
            if (up && !down && left && !right) return edges[2, 2];

            if (!upleft) return innerCorners[0, 0];
            if (!upright) return innerCorners[1, 0];
            if (!downleft) return innerCorners[0, 1];
            if (!downright) return innerCorners[1, 1];

            return null;
        }

        private bool TileCollideWithGroup(int x, int y)
        {
            return base.CollideRect(new Rectangle((int)GroupBoundsMin.X + x * 8, (int)GroupBoundsMin.Y + y * 8, 8, 8));
        }

        /* 
         * Check for every Hitbox in base.Colliders, so that 
         * the player & other entities don't get sent to the ends of the group.
         */
        public override void MoveHExact(int move)
        {
            //base.MoveHExact(move);
            GetRiders();
            Player player = base.Scene.Tracker.GetEntity<Player>();

            var riders = new DynData<Solid>(this).Get<HashSet<Actor>>("riders");

            if (player != null && Input.MoveX.Value == Math.Sign(move) && Math.Sign(player.Speed.X) == Math.Sign(move) && !riders.Contains(player) && CollideCheck(player, Position + Vector2.UnitX * move - Vector2.UnitY))
            {
                player.MoveV(1f);
            }
            base.X += move;
            MoveStaticMovers(Vector2.UnitX * move);
            if (Collidable)
            {
                foreach (Actor entity in base.Scene.Tracker.GetEntities<Actor>())
                {
                    if (entity.AllowPushing)
                    {
                        bool collidable = entity.Collidable;
                        entity.Collidable = true;

                        if (!entity.TreatNaive && CollideCheck(entity, Position))
                        {
                            foreach(Hitbox hitbox in Colliders)
                            {
                                if (hitbox.Collide(entity))
                                {
                                    float left = X + hitbox.Left;
                                    float right = X + hitbox.Right;

                                    int moveH = (move <= 0) ? (int)(left - entity.Right) : (int)(right - entity.Left);

                                    Collidable = false;
                                    entity.MoveHExact(moveH, null, this);
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
            //base.MoveVExact(move);
            GetRiders();
        
            var riders = new DynData<Solid>(this).Get<HashSet<Actor>>("riders");
        
            base.Y += move;
            MoveStaticMovers(Vector2.UnitY * move);
            if (Collidable)
            {
                foreach (Actor entity in base.Scene.Tracker.GetEntities<Actor>())
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
        
                                    int moveV = (move <= 0) ? (int)(top - entity.Bottom) : (int)(bottom - entity.Top);
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
        }
    }
}
