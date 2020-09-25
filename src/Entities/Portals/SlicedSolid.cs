using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Entities
{
	class SlicedSolid : Solid
	{
		public Vector2 MoveSpeed;
		public SinglePortal CurrentPortalStart;

		private Matrix MoveTransform = Matrix.Identity;

		public List<SlicedCollider> Colliders = new List<SlicedCollider>();
		public SlicedCollider OriginalCollider;

		public Vector2 FakePosition;

		private DynData<Platform> platformData;
		private DynData<Solid> solidData;
		private Vector2 fakeMovementCounter = Vector2.Zero;

		public SlicedSolid(Vector2 position, float width, float height, bool safe)
			: base(position, width, height, safe)
		{
			FakePosition = Position;
			Collider = OriginalCollider = new SlicedCollider(width, height);
			platformData = new DynData<Platform>(this);
			solidData = new DynData<Solid>(this);
		}

		private void GenerateNewColliders(Vector2 pushVector, Vector2? overrideSpeed = null)
		{
			if (overrideSpeed.HasValue)
			{
				MoveSpeed = (Vector2)overrideSpeed;
				CurrentPortalStart = null;
			}

			Collider = OriginalCollider;
			OriginalCollider.WorldPosition = Position;

			SlicedCollider startCollider = OriginalCollider.Clone();
			startCollider.WorldPosition = Position;

			Vector2 originalMoveSpeed = MoveSpeed;
			bool checkWithInitSpeed;
			if (checkWithInitSpeed = CurrentPortalStart != null)
			{
				MoveSpeed = CurrentPortalStart.RequiredSpeed();
				if (!CurrentPortalStart.CheckSolidAccess(startCollider, MoveSpeed))
				{
					if (CurrentPortalStart.ColliderBehindSelf(startCollider, out float inDist))
					{
						// Exit Portal
						CurrentPortalStart.MoveSlicedPartToPartner(startCollider, OriginalCollider);
						OriginalCollider = startCollider.CloneSize();
						switch (CurrentPortalStart.Partner.Facing)
						{
							default:
							case PortalFacings.Up:
								startCollider.WorldPosition.Y -= inDist; break;
							case PortalFacings.Down:
								startCollider.WorldPosition.Y += inDist; break;
							case PortalFacings.Left:
								startCollider.WorldPosition.X -= inDist; break;
							case PortalFacings.Right:
								startCollider.WorldPosition.X += inDist; break;
						}

						Position = startCollider.WorldPosition;
						//ClearRemainder();
						MoveTransform = Matrix.Multiply(MoveTransform, CurrentPortalStart.ToPartnerTransform);
						LiftSpeed = Vector2.Transform(LiftSpeed, CurrentPortalStart.ToPartnerTransform);
						pushVector = Vector2.Transform(pushVector, CurrentPortalStart.ToPartnerTransform);
					}
					GenerateNewColliders(pushVector, -CurrentPortalStart.Partner.RequiredSpeed()); // yeah.
					return;
				}
			}
			startCollider.MoveSpeed = MoveSpeed;
			startCollider.TransformedLiftSpeed = LiftSpeed;
			startCollider.PushMove = pushVector;

			Colliders.Clear();
			Colliders.Add(startCollider);

			CurrentPortalStart = null;

			// recursive
			PortalIteration(startCollider, Colliders);


			if (checkWithInitSpeed)
			{
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

		public override void Update()
		{
			base.Update();
		}

		public override void Awake(Scene scene)
		{
			base.Awake(scene);
			GenerateNewColliders(Vector2.Zero);
		}

		public override void Render()
		{
			base.Render();
			foreach (SlicedCollider collider in Colliders)
			{
				Draw.Rect(collider, Color.DeepPink);
			}
			Draw.HollowRect(FakePosition, OriginalCollider.Width, OriginalCollider.Height, Color.Teal);
		}

		private void PortalIteration(SlicedCollider collider, List<SlicedCollider> colliderList)
		{
			foreach (SolidPortal portal in SceneAs<Level>().Tracker.GetEntities<SolidPortal>())
			{
				if (portal.AllowEntrance(collider, collider.MoveSpeed, out SinglePortal enteredPortal))
				{
					if (enteredPortal == CurrentPortalStart) continue;
					PortalTravel(collider, enteredPortal, colliderList);
					break;
				}
			}
		}

		private void PortalTravel(SlicedCollider unsliced, SinglePortal portal, List<SlicedCollider> colliderList)
		{
			int offsetX = 0, offsetY = 0;
			SlicedCollider sliced = portal.Horizontal ?
						SliceHorizontally(portal.Y + portal.SliceOffset, portal.Facing, unsliced, out offsetY) :
						SliceVertically(portal.X + portal.SliceOffset, portal.Facing, unsliced, out offsetX);
			if (sliced != null)
			{
				if (CurrentPortalStart == null) CurrentPortalStart = portal;

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

		private SlicedCollider SliceHorizontally(float lineY, PortalFacings dir, SlicedCollider unsliced, out int worldYOffset)
		{
			worldYOffset = 0;

			lineY -= unsliced.WorldPosition.Y - unsliced.Position.Y;
			if (lineY <= unsliced.Top || lineY >= unsliced.Bottom)
				return null;

			SlicedCollider result;
			if (dir == PortalFacings.Up)
			{
				result = new SlicedCollider(unsliced.Width, unsliced.Bottom - lineY, unsliced.Position.X, lineY - unsliced.Top + unsliced.Position.Y);
				unsliced.Height = lineY - unsliced.Top;
				return result;
			}
			if (dir == PortalFacings.Down)
			{
				worldYOffset = (int)(lineY - unsliced.Top);
				result = new SlicedCollider(unsliced.Width, lineY - unsliced.Top, unsliced.Position.X, unsliced.Position.Y);
				unsliced.Height = unsliced.Bottom - lineY;
				unsliced.Position.Y = lineY - unsliced.Top + unsliced.Position.Y;
				return result;
			}

			return null;
		}

		private SlicedCollider SliceVertically(float lineX, PortalFacings dir, SlicedCollider unsliced, out int worldXOffset)
		{
			worldXOffset = 0;

			lineX -= unsliced.WorldPosition.X - unsliced.Position.X;
			if (lineX <= unsliced.Left || lineX >= unsliced.Right)
				return null;

			SlicedCollider result;
			if (dir == PortalFacings.Left)
			{
				result = new SlicedCollider(unsliced.Right - lineX, unsliced.Height, lineX - unsliced.Left + unsliced.Position.X, unsliced.Position.Y);
				unsliced.Width = lineX - unsliced.Left;
				return result;
			}
			if (dir == PortalFacings.Right)
			{
				worldXOffset = (int)(lineX - unsliced.Left);
				result = new SlicedCollider(lineX - unsliced.Left, unsliced.Height, unsliced.Position.X, unsliced.Position.Y);
				unsliced.Width = unsliced.Right - lineX;
				unsliced.Position.X = lineX - unsliced.Left + unsliced.Position.X;

				return result;
			}

			return null;
		}

		public new void ClearRemainder()
		{
			platformData.Set("movementCounter", fakeMovementCounter = Vector2.Zero);
		}

		public void MoveTransformed(Vector2 initialMove)
		{
			Vector2 move = Vector2.Transform(initialMove, MoveTransform);
			MoveSpeed = move.SafeNormalize(Vector2.Zero);

			if (Engine.DeltaTime == 0f)
			{
				LiftSpeed = Vector2.Zero;
			}
			else
			{
				LiftSpeed = move / Engine.DeltaTime;
			}

			Vector2 movementCounter = platformData.Get<Vector2>("movementCounter");
			platformData.Set("movementCounter", movementCounter += move);
			fakeMovementCounter += initialMove;

			Vector2 vec = Calc.Round(movementCounter);
			Vector2 fakeVec = Calc.Round(fakeMovementCounter);

			if (fakeVec.X != 0)
			{
				fakeMovementCounter.X -= fakeVec.X;
				FakePosition.X += fakeVec.X;
			}
			if (fakeVec.Y != 0)
			{
				fakeMovementCounter.Y -= fakeVec.Y;
				FakePosition.Y += fakeVec.Y;
			}
			if (vec.X != 0) 
			{
				movementCounter.X -= vec.X;
				MoveHExact((int)vec.X);
			}
			if (vec.Y != 0) 
			{ 
				movementCounter.Y -= vec.Y;
				MoveVExact((int)vec.Y);
			}
			GenerateNewColliders(vec);
			platformData.Set("movementCounter", movementCounter);
		}
	}

	class SlicedCollider : Hitbox
	{
		public Vector2 MoveSpeed = Vector2.Zero;
		public Vector2 PushMove;
		public Vector2 TransformedLiftSpeed = Vector2.Zero;

		public Vector2 WorldPosition = Vector2.Zero;

		public float WorldAbsoluteLeft => WorldPosition.X + Left - Position.X;
		public float WorldAbsoluteRight => WorldPosition.X + Right - Position.X;
		public float WorldAbsoluteTop => WorldPosition.Y + Top - Position.Y;
		public float WorldAbsoluteBottom => WorldPosition.Y + Bottom - Position.Y;

		public SlicedCollider(float width, float height, float x = 0f, float y = 0f)
			: base(width, height, x, y)
		{ }

		public new SlicedCollider Clone()
        {
			return new SlicedCollider(Width, Height, Position.X, Position.Y);
        }

		public SlicedCollider CloneSize()
		{
			return new SlicedCollider(Width, Height);
		}

		public bool FakeIntersects(Hitbox hitbox)
		{
			if (WorldAbsoluteLeft < hitbox.AbsoluteRight && WorldAbsoluteRight > hitbox.AbsoluteLeft && WorldAbsoluteBottom > hitbox.AbsoluteTop)
			{
				return WorldAbsoluteTop < hitbox.AbsoluteBottom;
			}
			return false;
		}

	}
}
