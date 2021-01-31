using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.CommunalHelper.Entities
{
    public enum PortalFacings
    {
        Up, Right, Down, Left
    }

    [CustomEntity("CommunalHelper/SolidPortal")]
    [Tracked]
    class SolidPortal : Entity
    {
        public SinglePortal Portal1, Portal2;

        public SolidPortal(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Nodes[0] + offset, data.Width, data.Height, data.Enum("facingEntrance", PortalFacings.Up), data.Enum("facingExit", PortalFacings.Up))
        { }
        
        public SolidPortal(Vector2 position, Vector2 node, int width, int height, PortalFacings portalFacing1, PortalFacings portalFacing2)
            : base(position)
        {
            Portal1 = new SinglePortal(position, width, height, portalFacing1, this);
            bool flipDimensions = Portal1.Horizontal != (portalFacing2 == PortalFacings.Up || portalFacing2 == PortalFacings.Down);
            Portal2 = new SinglePortal(node, flipDimensions ? height : width, flipDimensions ? width : height, portalFacing2, this);

            Portal1.SetPartner(Portal2); Portal2.SetPartner(Portal1);
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            scene.Add(Portal1, Portal2);
        }

        public override void Removed(Scene scene)
        {
            base.Removed(scene);
            Portal1.RemoveSelf();
            Portal2.RemoveSelf();
        }

        public bool AllowEntrance(SlicedCollider collider, Vector2 speed, bool fitPortal, out SinglePortal portal)
        {
            if(Portal1.CheckSolidAccess(collider, speed, fitPortal))
            {
                portal = Portal1; return true;
            }
            if (Portal2.CheckSolidAccess(collider, speed, fitPortal))
            {
                portal = Portal2; return true;
            }
            portal = null;
            return false;
        }

        private static readonly Matrix FlipH = Matrix.CreateScale(-1, 1, 0);
        private static readonly Matrix FlipV = Matrix.CreateScale(1, -1, 0);
        private static readonly Matrix TurnRight = Matrix.CreateRotationZ((float)Math.PI / 2f);
        private static readonly Matrix TurnLeft = Matrix.CreateRotationZ((float)Math.PI / -2f);
        private static readonly Matrix TurnHalf = Matrix.CreateRotationZ((float)Math.PI);
        private static readonly Matrix TurnRightFlipH = Matrix.Multiply(TurnRight, FlipH);
        private static readonly Matrix TurnLeftFlipV = Matrix.Multiply(TurnLeft, FlipV);

        // yeah......
        public static void GetPortalTransform(SinglePortal a, SinglePortal b, out Matrix motionTransform)
        {
            motionTransform = Matrix.Identity;
            if (a.Facing == b.OppositeFacing)
                return;

            if (a.Facing == b.Facing) {
                motionTransform = TurnHalf;
                return;
            }

            if (a.Facing == PortalFacings.Left)
            {
                if (b.Facing == PortalFacings.Down) {
                    motionTransform = TurnRight;
                }
                if (b.Facing == PortalFacings.Up) {
                    motionTransform = TurnLeft;
                }
            }
            if (a.Facing == PortalFacings.Down)
            {
                if (b.Facing == PortalFacings.Right) {
                    motionTransform = TurnRight;
                }
               
                if (b.Facing == PortalFacings.Left) {
                    motionTransform = TurnLeft;
                }
               
            }
            if (a.Facing == PortalFacings.Right)
            {
                if (b.Facing == PortalFacings.Down) {
                    motionTransform = TurnLeft;
                }
                
                if (b.Facing == PortalFacings.Up) {
                    motionTransform = TurnRight;
                }

            }
            if (a.Facing == PortalFacings.Up)
            {
                if (b.Facing == PortalFacings.Right) {
                    motionTransform = TurnLeft;
                }
                if (b.Facing == PortalFacings.Left) {
                    motionTransform = TurnRight;
                }
                
            }
        }
    }

    [Tracked]
    class SinglePortal : Entity
    {
        public PortalFacings Facing, OppositeFacing;

        public SolidPortal Parent;
        public SinglePortal Partner;

        public Vector2 AnchorPoint;
        public bool Horizontal = false;
        public int SliceOffset = 0;

        public Matrix ToPartnerTransform;

        public SinglePortal(Vector2 position, int width, int height, PortalFacings facing, SolidPortal parent)
            : base(position)
        {
            Facing = facing;
            Parent = parent;

            AnchorPoint = Position;
            switch (facing)
            {
                default:
                case PortalFacings.Up:
                    OppositeFacing = PortalFacings.Down;
                    SliceOffset = 8;
                    Horizontal = true;
                    base.Collider = new Hitbox(width, 1, 0, 7);
                    AnchorPoint.Y += 7;
                    break;
                case PortalFacings.Down:
                    OppositeFacing = PortalFacings.Up;
                    Horizontal = true;
                    base.Collider = new Hitbox(width, 1);
                    break;
                case PortalFacings.Left:
                    OppositeFacing = PortalFacings.Right;
                    SliceOffset = 8;
                    base.Collider = new Hitbox(1, height, 7);
                    AnchorPoint.X += 7;
                    break;
                case PortalFacings.Right:
                    OppositeFacing = PortalFacings.Left;
                    base.Collider = new Hitbox(1, height);
                    break;
            }
        }

        public void SetPartner(SinglePortal partner)
        {
            Partner = partner;
            SolidPortal.GetPortalTransform(this, partner, out Matrix motionTransform);
            ToPartnerTransform = motionTransform;
        }

        public bool ColliderBehindSelf(SlicedCollider collider, out float offset)
        {
            switch (Facing)
            {
                default:
                case PortalFacings.Up:
                    offset = collider.WorldAbsoluteTop - Top - 1;
                    return collider.WorldAbsoluteTop >= Bottom;
                case PortalFacings.Down:
                    offset = Bottom - collider.WorldAbsoluteBottom - 1;
                    return collider.WorldAbsoluteBottom <= Top;
                case PortalFacings.Left:
                    offset = collider.WorldAbsoluteLeft - Left - 1;
                    return collider.WorldAbsoluteLeft >= Right;
                case PortalFacings.Right:
                    offset = Right - collider.WorldAbsoluteRight - 1;
                    return collider.WorldAbsoluteRight <= Left;
            }
        }

        public bool CheckSolidAccess(SlicedCollider hitbox, Vector2 speed, bool fitPortal)
        {
            
            if (!hitbox.FakeIntersects((Hitbox)Collider)) return false;
            if (fitPortal) {
                switch (Facing) {
                    default:
                    case PortalFacings.Up:
                        return hitbox.WorldAbsoluteLeft >= Left && hitbox.WorldAbsoluteRight <= Right && speed.Y > 0;

                    case PortalFacings.Down:
                        return hitbox.WorldAbsoluteLeft >= Left && hitbox.WorldAbsoluteRight <= Right && speed.Y < 0;

                    case PortalFacings.Left:
                        return hitbox.WorldAbsoluteTop >= Top && hitbox.WorldAbsoluteBottom <= Bottom && speed.X > 0;

                    case PortalFacings.Right:
                        return hitbox.WorldAbsoluteTop >= Top && hitbox.WorldAbsoluteBottom <= Bottom && speed.X < 0;
                }
            }
            return true;
        }

        public void MoveSlicedPartToPartner(SlicedCollider collider, SlicedCollider from, Vector2 renderOffset)
        {
            float sizeOnPortal = Horizontal ? collider.Width : collider.Height;
            float sizeFromPortal = Horizontal ? collider.Height : collider.Width;

            float positionOnPortal = Horizontal ? AnchorPoint.X - collider.WorldPosition.X : AnchorPoint.Y - collider.WorldPosition.Y;
            
            float newWidth = Partner.Horizontal ? sizeOnPortal : sizeFromPortal;
            float newHeight = Partner.Horizontal ? sizeFromPortal : sizeOnPortal;
            collider.RenderOffset -= renderOffset;

            // there are so many cases to cover, this is hell on earth.
            switch (Partner.Facing)
            {
                default:
                case PortalFacings.Right:
                    collider.CutLeft = true;
                    if (Facing == PortalFacings.Right) {
                        collider.WorldPosition = Partner.AnchorPoint - new Vector2(0, -Height - positionOnPortal) - new Vector2(0, newHeight);
                    } else if (Facing == PortalFacings.Up) {
                        collider.WorldPosition = Partner.AnchorPoint + new Vector2(0, positionOnPortal) + new Vector2(0, Partner.Height - newHeight);
                    } else {
                        collider.WorldPosition = Partner.AnchorPoint - new Vector2(0, positionOnPortal);
                    }
                    break;
                case PortalFacings.Down:
                    collider.CutTop = true;
                    if (Facing == PortalFacings.Left) {
                        collider.WorldPosition = Partner.AnchorPoint + new Vector2(positionOnPortal, 0) - new Vector2(newWidth - Partner.Width, 0);
                    } else if (Facing == PortalFacings.Down) {
                        collider.WorldPosition = Partner.AnchorPoint + new Vector2(positionOnPortal, 0) + new Vector2(Width - newWidth, 0);
                    } else {
                        collider.WorldPosition = Partner.AnchorPoint - new Vector2(positionOnPortal, 0);
                    }
                    break;
                case PortalFacings.Left:
                    collider.CutRight = true;
                    if (Facing == PortalFacings.Down) {
                        collider.WorldPosition = Partner.AnchorPoint - new Vector2(-1, -Width - positionOnPortal) - new Vector2(newWidth, newHeight);
                    } else if (Facing == PortalFacings.Left) {
                        collider.WorldPosition = Partner.AnchorPoint - new Vector2(-1, -Height - positionOnPortal) - new Vector2(newWidth, newHeight);
                    } else {
                        collider.WorldPosition = Partner.AnchorPoint - new Vector2(-1, positionOnPortal) - new Vector2(newWidth, 0);
                    }
                    break;
                case PortalFacings.Up:
                    collider.CutBottom = true;
                    if (Facing == PortalFacings.Right) {
                        collider.WorldPosition = Partner.AnchorPoint + new Vector2(positionOnPortal, 1) - new Vector2(newWidth - Partner.Width, newHeight);
                    } else if (Facing == PortalFacings.Up) {
                        collider.WorldPosition = Partner.AnchorPoint + new Vector2(positionOnPortal, 1) + new Vector2(Width - newWidth, -newHeight);
                    } else {
                        collider.WorldPosition = Partner.AnchorPoint - new Vector2(positionOnPortal, -1) - new Vector2(0, newHeight);
                    }
                    break;
            }


            collider.Width = newWidth; collider.Height = newHeight;
            collider.Position = collider.WorldPosition - from.WorldPosition;
        }

        public Vector2 RequiredSpeed()
        {
            switch (Facing)
            {
                default:
                case PortalFacings.Right:
                    return -Vector2.UnitX;
                case PortalFacings.Down:
                    return -Vector2.UnitY;
                case PortalFacings.Left:
                    return Vector2.UnitX;
                case PortalFacings.Up:
                    return Vector2.UnitY;
            }
        }

        public override void Render()
        {
            base.Render();
            switch (Facing)
            {
                default:
                case PortalFacings.Up:
                    Draw.Rect(Position.X, Position.Y + 6, Width, 2, Color.Green);
                    Draw.Rect(Position.X, Position.Y, 2, 8, Color.Green);
                    Draw.Rect(Position.X + Width - 2, Position.Y, 2, 8, Color.Green);
                    break;
                case PortalFacings.Down:
                    Draw.Rect(Position.X, Position.Y, Width, 2, Color.Green);
                    Draw.Rect(Position.X, Position.Y, 2, 8, Color.Green);
                    Draw.Rect(Position.X + Width - 2, Position.Y, 2, 8, Color.Green);
                    break;
                case PortalFacings.Left:
                    Draw.Rect(Position.X + 6, Position.Y, 2, Height, Color.Green);
                    Draw.Rect(Position.X, Position.Y, 8, 2, Color.Green);
                    Draw.Rect(Position.X, Position.Y + Height - 2, 8, 2, Color.Green);
                    break;
                case PortalFacings.Right:
                    Draw.Rect(Position.X, Position.Y, 2, Height, Color.Green);
                    Draw.Rect(Position.X, Position.Y, 8, 2, Color.Green);
                    Draw.Rect(Position.X, Position.Y + Height - 2, 8, 2, Color.Green);
                    break;
            }

            Draw.Point(AnchorPoint, Color.Purple);

        }
    }
}
