﻿using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Entities {
    class SlicedSolid : Solid {
        public Vector2 MoveSpeed;
        public SinglePortal CurrentPortalStart;

        private Matrix MoveTransform = Matrix.Identity;

        public List<SlicedCollider> Colliders = new List<SlicedCollider>();
        public SlicedCollider OriginalCollider;

        public Vector2 FakePosition;
        private DynData<Platform> platformData;

        public SlicedSolid(Vector2 position, float width, float height, bool safe)
            : base(position, width, height, safe) {
            FakePosition = Position;
            Collider = OriginalCollider = new SlicedCollider(width, height);
            platformData = new DynData<Platform>(this);
        }

        private void GenerateNewColliders(Vector2 pushVector, Vector2? overrideSpeed = null, 
            bool cutTop = false, bool cutRight = false, bool cutBottom = false, bool cutLeft = false) {
            if (overrideSpeed.HasValue) {
                MoveSpeed = (Vector2) overrideSpeed;
                CurrentPortalStart = null;
            }

            Collider = OriginalCollider;
            OriginalCollider.WorldPosition = Position;

            SlicedCollider startCollider = OriginalCollider.Clone();
            startCollider.WorldPosition = Position;

            Vector2 originalMoveSpeed = MoveSpeed;
            bool checkWithInitSpeed;
            if (checkWithInitSpeed = CurrentPortalStart != null) {
                MoveSpeed = CurrentPortalStart.RequiredSpeed();
                if (!CurrentPortalStart.CheckSolidAccess(startCollider, MoveSpeed)) {
                    if (CurrentPortalStart.ColliderBehindSelf(startCollider, out float inDist)) {
                        // Exit Portal
                        CurrentPortalStart.MoveSlicedPartToPartner(startCollider, OriginalCollider);
                        OriginalCollider = startCollider.CloneSize();
                        switch (CurrentPortalStart.Partner.Facing) {
                            default:
                            case PortalFacings.Up:
                                cutBottom = true;
                                startCollider.WorldPosition.Y -= inDist;
                                break;
                            case PortalFacings.Down:
                                cutTop = true;
                                startCollider.WorldPosition.Y += inDist;
                                break;
                            case PortalFacings.Left:
                                cutRight = true;
                                startCollider.WorldPosition.X -= inDist;
                                break;
                            case PortalFacings.Right:
                                cutLeft = true;
                                startCollider.WorldPosition.X += inDist;
                                break;
                        }

                        Position = startCollider.WorldPosition;
                        MoveTransform = Matrix.Multiply(MoveTransform, CurrentPortalStart.ToPartnerTransform);
                        LiftSpeed = Vector2.Transform(LiftSpeed, CurrentPortalStart.ToPartnerTransform);
                        pushVector = Vector2.Transform(pushVector, CurrentPortalStart.ToPartnerTransform);
                    }
                    GenerateNewColliders(pushVector, -CurrentPortalStart.Partner.RequiredSpeed(), cutTop, cutRight, cutBottom, cutLeft); // yeah.
                    return;
                }
            }
            startCollider.MoveSpeed = MoveSpeed;
            startCollider.TransformedLiftSpeed = LiftSpeed;
            startCollider.PushMove = pushVector;
            startCollider.CutTop = cutTop;
            startCollider.CutBottom = cutBottom;
            startCollider.CutRight = cutRight;
            startCollider.CutLeft = cutLeft;

            Colliders.Clear();
            Colliders.Add(startCollider);

            CurrentPortalStart = null;

            // recursive
            PortalIteration(startCollider, Colliders);


            if (checkWithInitSpeed) {
                startCollider.MoveSpeed = -MoveSpeed;
                PortalIteration(startCollider, Colliders);
            }

            startCollider.MoveSpeed = originalMoveSpeed;

            SlicedCollider[] finalColliders = new SlicedCollider[Colliders.Count];
            for (int i = 0; i < finalColliders.Length; i++)
                finalColliders[i] = Colliders[i];

            Collider = new ColliderList(finalColliders);

            MoveSpeed = Vector2.Zero;
        }

        public override void Update() {
            base.Update();
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            GenerateNewColliders(Vector2.Zero);
        }

        public override void Render() {
            base.Render();
            foreach (SlicedCollider collider in Colliders) {

                if (collider.CutBottom)
                    Draw.Rect(Position + collider.Position + Vector2.UnitY + Shake, collider.Width, collider.Height, Color.Lerp(Color.DeepPink, Color.Black, .5f));
                if (collider.CutTop)
                    Draw.Rect(Position + collider.Position - Vector2.UnitY + Shake, collider.Width, collider.Height, Color.Lerp(Color.DeepPink, Color.Black, .5f));
                if (collider.CutRight)
                    Draw.Rect(Position + collider.Position + Vector2.UnitX + Shake, collider.Width, collider.Height, Color.Lerp(Color.DeepPink, Color.Black, .5f));
                if (collider.CutLeft)
                    Draw.Rect(Position + collider.Position - Vector2.UnitX + Shake, collider.Width, collider.Height, Color.Lerp(Color.DeepPink, Color.Black, .5f));

                Draw.Rect(Position + collider.Position + Shake, collider.Width, collider.Height, Color.DeepPink);
            }
        }

        private void PortalIteration(SlicedCollider collider, List<SlicedCollider> colliderList) {
            foreach (SolidPortal portal in SceneAs<Level>().Tracker.GetEntities<SolidPortal>()) {
                if (portal.AllowEntrance(collider, collider.MoveSpeed, out SinglePortal enteredPortal)) {
                    PortalTravel(collider, enteredPortal, colliderList);
                    break;
                }
            }
        }

        private void PortalTravel(SlicedCollider unsliced, SinglePortal portal, List<SlicedCollider> colliderList) {
            int offsetX = 0, offsetY = 0;
            SlicedCollider sliced = portal.Horizontal ?
                        SliceHorizontally(portal.Y + portal.SliceOffset, portal.Facing, unsliced, out offsetY) :
                        SliceVertically(portal.X + portal.SliceOffset, portal.Facing, unsliced, out offsetX);
            if (sliced != null) {
                if (CurrentPortalStart == null)
                    CurrentPortalStart = portal;

                sliced.WorldPosition = unsliced.WorldPosition;
                portal.MoveSlicedPartToPartner(sliced, OriginalCollider);

                // transformation
                sliced.MoveSpeed = -portal.Partner.RequiredSpeed();
                sliced.TransformedLiftSpeed = Vector2.Transform(unsliced.TransformedLiftSpeed, portal.ToPartnerTransform);
                sliced.PushMove = Vector2.Transform(unsliced.PushMove, portal.ToPartnerTransform);

                unsliced.WorldPosition += new Vector2(offsetX, offsetY);

                colliderList.Add(sliced);
                PortalIteration(sliced, colliderList);
            }
        }

        private SlicedCollider SliceHorizontally(float lineY, PortalFacings dir, SlicedCollider unsliced, out int worldYOffset) {
            worldYOffset = 0;

            lineY -= unsliced.WorldPosition.Y - unsliced.Position.Y;
            if (lineY <= unsliced.Top || lineY >= unsliced.Bottom)
                return null;

            SlicedCollider result;
            if (dir == PortalFacings.Up) {
                result = new SlicedCollider(unsliced.Width, unsliced.Bottom - lineY, unsliced.Position.X, lineY - unsliced.Top + unsliced.Position.Y);
                unsliced.Height = lineY - unsliced.Top;
                unsliced.CutBottom = true;
                return result;
            }
            if (dir == PortalFacings.Down) {
                worldYOffset = (int) (lineY - unsliced.Top);
                result = new SlicedCollider(unsliced.Width, lineY - unsliced.Top, unsliced.Position.X, unsliced.Position.Y);
                unsliced.Height = unsliced.Bottom - lineY;
                unsliced.Position.Y = lineY - unsliced.Top + unsliced.Position.Y;
                unsliced.CutTop = true;
                return result;
            }

            return null;
        }

        private SlicedCollider SliceVertically(float lineX, PortalFacings dir, SlicedCollider unsliced, out int worldXOffset) {
            worldXOffset = 0;

            lineX -= unsliced.WorldPosition.X - unsliced.Position.X;
            if (lineX <= unsliced.Left || lineX >= unsliced.Right)
                return null;

            SlicedCollider result;
            if (dir == PortalFacings.Left) {
                result = new SlicedCollider(unsliced.Right - lineX, unsliced.Height, lineX - unsliced.Left + unsliced.Position.X, unsliced.Position.Y);
                unsliced.Width = lineX - unsliced.Left;
                unsliced.CutRight = true;
                return result;
            }
            if (dir == PortalFacings.Right) {
                worldXOffset = (int) (lineX - unsliced.Left);
                result = new SlicedCollider(lineX - unsliced.Left, unsliced.Height, unsliced.Position.X, unsliced.Position.Y);
                unsliced.Width = unsliced.Right - lineX;
                unsliced.Position.X = lineX - unsliced.Left + unsliced.Position.X;
                unsliced.CutLeft = true;
                return result;
            }

            return null;
        }

        public new void MoveTo(Vector2 target) {
            Move(target - FakePosition);
        }

        public void Move(Vector2 initialMove) {
            Vector2 move = Vector2.Transform(initialMove, MoveTransform);
            MoveSpeed = move.SafeNormalize(Vector2.Zero);

            if (Engine.DeltaTime == 0f) {
                LiftSpeed = Vector2.Zero;
            } else {
                LiftSpeed = move / Engine.DeltaTime;
            }

            Vector2 movementCounter = platformData.Get<Vector2>("movementCounter");
            platformData.Set("movementCounter", movementCounter += initialMove);

            Vector2 vec = Calc.Round(movementCounter);
            Vector2 vecTransformed = Calc.Round(Vector2.Transform(movementCounter, MoveTransform));

            if (vec.X != 0) {
                movementCounter.X -= vec.X;
                FakePosition.X += vec.X;
            }
            if (vec.Y != 0) {
                movementCounter.Y -= vec.Y;
                FakePosition.Y += vec.Y;
            }

            if (vecTransformed.X != 0)
                X += (int) vecTransformed.X;
            if (vecTransformed.Y != 0)
                Y += (int) vecTransformed.Y;

            GenerateNewColliders(vecTransformed);
            FakeColliderMove();
            platformData.Set("movementCounter", movementCounter);
        }

        public void FakeColliderMove() {
            foreach (SlicedCollider collider in Colliders) {
                collider.FakeMove(SceneAs<Level>(), collider.Position + Position);
            }
        }
    }

    class SlicedCollider : Hitbox {
        public Vector2 MoveSpeed = Vector2.Zero;
        public Vector2 PushMove;
        public Vector2 TransformedLiftSpeed = Vector2.Zero;

        public Vector2 WorldPosition = Vector2.Zero;

        public bool 
            CutTop = false, 
            CutRight = false, 
            CutBottom = false, 
            CutLeft = false;

        public float WorldAbsoluteLeft => WorldPosition.X + Left - Position.X;
        public float WorldAbsoluteRight => WorldPosition.X + Right - Position.X;
        public float WorldAbsoluteTop => WorldPosition.Y + Top - Position.Y;
        public float WorldAbsoluteBottom => WorldPosition.Y + Bottom - Position.Y;

        public SlicedCollider(float width, float height, float x = 0f, float y = 0f)
            : base(width, height, x, y) { }

        public new SlicedCollider Clone() {
            return new SlicedCollider(Width, Height, Position.X, Position.Y);
        }

        public SlicedCollider CloneSize() {
            return new SlicedCollider(Width, Height);
        }

        public bool FakeIntersects(Hitbox hitbox) {
            if (WorldAbsoluteLeft < hitbox.AbsoluteRight && WorldAbsoluteRight > hitbox.AbsoluteLeft && WorldAbsoluteBottom > hitbox.AbsoluteTop) {
                return WorldAbsoluteTop < hitbox.AbsoluteBottom;
            }
            return false;
        }

        public void FakeMove(Scene scene, Vector2 at) {
            Solid mover = new FakeSolid(at - PushMove, Width, Height, true, this);
            mover.Added(scene);
            mover.MoveTo(at, TransformedLiftSpeed);
        }

        private class FakeSolid : Solid {

            private SlicedCollider parent;
            public FakeSolid(Vector2 position, float width, float height, bool safe, SlicedCollider parent) 
                : base(position, width, height, safe) {
                this.parent = parent;
            }

            public override void MoveHExact(int move) {
                GetRiders();
                float right = base.Right;
                float left = base.Left;
                Player player = base.Scene.Tracker.GetEntity<Player>();
                HashSet<Actor> riders = new DynData<Solid>(this).Get<HashSet<Actor>>("riders");
                if (player != null && Input.MoveX.Value == Math.Sign(move) && Math.Sign(player.Speed.X) == Math.Sign(move) && !riders.Contains(player) && CollideCheck(player, Position + Vector2.UnitX * move - Vector2.UnitY)) {
                    player.MoveV(1f);
                }
                base.X += move;
                MoveStaticMovers(Vector2.UnitX * move);
                if (Collidable) {
                    foreach (Actor entity in base.Scene.Tracker.GetEntities<Actor>()) {
                        if (!entity.AllowPushing) {
                            continue;
                        }
                        bool collidable = entity.Collidable;
                        entity.Collidable = true;
                        if (!entity.TreatNaive && CollideCheck(entity, Position)) {
                            int moveH = (move <= 0) ? (move - (int) (entity.Right - left)) : (move - (int) (entity.Left - right));
                            Collidable = false;
                            entity.MoveHExact(moveH, entity.SquishCallback, this);
                            Console.WriteLine(entity.LiftSpeed = LiftSpeed);
                            Collidable = true;
                        } else if (riders.Contains(entity) && !(entity.Bottom <= Top && parent.CutTop)) {
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
                riders.Clear();
            }

            public override void MoveVExact(int move) {
                GetRiders();
                float bottom = base.Bottom;
                float top = base.Top;
                HashSet<Actor> riders = new DynData<Solid>(this).Get<HashSet<Actor>>("riders");
                base.Y += move;
                MoveStaticMovers(Vector2.UnitY * move);
                if (Collidable) {
                    foreach (Actor entity in base.Scene.Tracker.GetEntities<Actor>()) {
                        if (!entity.AllowPushing) {
                            continue;
                        }
                        bool collidable = entity.Collidable;
                        entity.Collidable = true;
                        if (!entity.TreatNaive && CollideCheck(entity, Position)) {
                            int moveV = (move <= 0) ? (move - (int) (entity.Bottom - top)) : (move - (int) (entity.Top - bottom));
                            Collidable = false;
                            entity.MoveVExact(moveV, entity.SquishCallback, this);
                            entity.LiftSpeed = LiftSpeed;
                            Collidable = true;
                        } else if (riders.Contains(entity) &&
                            !((entity.Left >= Right && parent.CutRight) || (entity.Right <= Left && parent.CutLeft))) {

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
                riders.Clear();
            }
        }
    }
}
