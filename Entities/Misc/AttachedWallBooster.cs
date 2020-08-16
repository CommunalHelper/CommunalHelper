using Celeste.Mod.Entities;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper
{
	[CustomEntity("CommunalHelper/AttachedWallBooster")]
	[Tracked(false)]
    class AttachedWallBooster : Entity
    {
		public Facings Facing;

		private Vector2 Shake = Vector2.Zero;
		private ClimbBlocker climbBlocker;
		private SoundSource idleSfx;

		public bool IceMode;
		private bool notCoreMode;
		private List<Sprite> tiles;

		public AttachedWallBooster(Vector2 position, float height, bool left, bool notCoreMode)
			: base(position)
		{
			base.Tag = Tags.TransitionUpdate;
			base.Depth = 1999;
			this.notCoreMode = notCoreMode;
			if (left)
			{
				Facing = Facings.Left;
				base.Collider = new Hitbox(2f, height);
			}
			else
			{
				Facing = Facings.Right;
				base.Collider = new Hitbox(2f, height, 6f);
			}
			Add(new CoreModeListener(OnChangeMode));
			Add(new StaticMover
			{
				OnShake = OnShake,
				SolidChecker = IsRiding,
				OnEnable = OnEnable,
				OnDisable = OnDisable
			});

			Add(climbBlocker = new ClimbBlocker(edge: false));
			Add(idleSfx = new SoundSource());
			tiles = BuildSprite(left);
		}

		public AttachedWallBooster(EntityData data, Vector2 offset)
			: this(data.Position + offset, data.Height, data.Bool("left"), data.Bool("notCoreMode"))
		{ }

		private List<Sprite> BuildSprite(bool left)
		{
			List<Sprite> list = new List<Sprite>();
			for (int i = 0; (float)i < base.Height; i += 8)
			{
				string id = (i == 0) ? "WallBoosterTop" : ((!((float)(i + 16) > base.Height)) ? "WallBoosterMid" : "WallBoosterBottom");
				Sprite sprite = GFX.SpriteBank.Create(id);
				if (!left)
				{
					sprite.FlipX = true;
					sprite.Position = new Vector2(4f, i);
				}
				else
				{
					sprite.Position = new Vector2(0f, i);
				}
				list.Add(sprite);
				Add(sprite);
			}
			return list;
		}

		public override void Added(Scene scene)
		{
			base.Added(scene);
			Session.CoreModes mode = Session.CoreModes.None;
			if (SceneAs<Level>().CoreMode == Session.CoreModes.Cold || notCoreMode)
			{
				mode = Session.CoreModes.Cold;
			}
			OnChangeMode(mode);
		}

		private void OnChangeMode(Session.CoreModes mode)
		{
			IceMode = (mode == Session.CoreModes.Cold);
			climbBlocker.Blocking = IceMode;
			tiles.ForEach(delegate (Sprite t)
			{
				t.Play(IceMode ? "ice" : "hot");
			});
			if (IceMode)
			{
				idleSfx.Stop();
			}
			else if (!idleSfx.Playing)
			{
				idleSfx.Play("event:/env/local/09_core/conveyor_idle");
			}
		}

		private void SetColor(Color color)
		{
			foreach(Image img in tiles)
			{
				img.Color = color;
			}
		}

		private void OnDisable()
		{
			SetColor(Color.Gray);
			Collidable = false;
		}

		private void OnEnable()
		{
			SetColor(Color.White);
			Collidable = true;
		}

		public override void Update()
		{
			PositionIdleSfx();
			if (!(base.Scene as Level).Transitioning)
			{
				base.Update();
			}
		}

		private void PositionIdleSfx()
		{
			Player entity = base.Scene.Tracker.GetEntity<Player>();
			if (entity != null)
			{
				idleSfx.Position = Calc.ClosestPointOnLine(Position, Position + new Vector2(0f, base.Height), entity.Center) - Position;
				idleSfx.UpdateSfxPosition();
			}
		}

		private bool IsRiding(Solid solid)
		{
			switch (Facing)
			{
				case Facings.Right:
					return CollideCheckOutside(solid, Position + Vector2.UnitX);
				case Facings.Left:
					return CollideCheckOutside(solid, Position - Vector2.UnitX);

				default:
					return false;
			}
		}

		private void OnShake(Vector2 amount)
		{
			Shake += amount;
		}

		public override void Render()
		{
			Vector2 p = Position;
			Position += Shake;
			base.Render();
			Position = p;
		}

	}

	public class AttachedWallBoosterHooks
	{
		private const float WallBoosterSpeed = -160f;

		private static float attachedWallBoosterCurrentSpeed = 0f;
		private static bool attachedWallBoosting = false;
		private static EventInstance conveyorLoopSfx;
		private static float playerWallBoostTimer = 0f;

		public static void Unhook()
		{
			On.Celeste.Player.ClimbBegin -= Player_ClimbBegin;
			On.Celeste.Player.ClimbUpdate -= Player_ClimbUpdate;
			On.Celeste.Player.ClimbEnd -= Player_ClimbEnd;
			On.Celeste.Player.ClimbJump -= Player_ClimbJump;
			On.Celeste.Player.WallJump -= Player_WallJump;
			On.Celeste.Player.Update -= Player_Update;
		}
		public static void Hook()
		{
			On.Celeste.Player.ClimbBegin += Player_ClimbBegin;
			On.Celeste.Player.ClimbUpdate += Player_ClimbUpdate;
			On.Celeste.Player.ClimbEnd += Player_ClimbEnd;
			On.Celeste.Player.ClimbJump += Player_ClimbJump;
			On.Celeste.Player.WallJump += Player_WallJump;
			On.Celeste.Player.Update += Player_Update;
		}

        #region Attached Wall Booster Hooks
        private static void Player_Update(On.Celeste.Player.orig_Update orig, Player self)
		{
			orig(self);
			if (playerWallBoostTimer > 0) playerWallBoostTimer -= Engine.DeltaTime;
		}

		private static void Player_WallJump(On.Celeste.Player.orig_WallJump orig, Player self, int dir)
		{
			PlayerWallBoost(self);
			orig(self, dir);
		}

		private static void Player_ClimbJump(On.Celeste.Player.orig_ClimbJump orig, Player self)
		{
			PlayerWallBoost(self);
			orig(self);
		}

		private static void PlayerWallBoost(Player player)
		{
			if (playerWallBoostTimer > 0)
			{
				player.LiftSpeed += Vector2.UnitY * Calc.Max(attachedWallBoosterCurrentSpeed, -80f);
				attachedWallBoosterCurrentSpeed = playerWallBoostTimer = 0f;
				Console.WriteLine(player.LiftSpeed);
			}
		}

		private static void Player_ClimbEnd(On.Celeste.Player.orig_ClimbEnd orig, Player self)
		{
			orig(self);
			if (attachedWallBoosting)
			{
				attachedWallBoosting = false;
				conveyorLoopSfx.setParameterValue("end", 1f);
				conveyorLoopSfx.release();
			}
		}

		private static void Player_ClimbBegin(On.Celeste.Player.orig_ClimbBegin orig, Player self)
		{
			orig(self);
			attachedWallBoosterCurrentSpeed = 0f; attachedWallBoosting = false;
		}

		private static int Player_ClimbUpdate(On.Celeste.Player.orig_ClimbUpdate orig, Player self)
		{
			if (AttachedWallBoosterCheck(self))
			{
				if (!attachedWallBoosting)
				{
					attachedWallBoosting = true;
					attachedWallBoosterCurrentSpeed = self.Speed.Y;
					conveyorLoopSfx = Audio.Play("event:/game/09_core/conveyor_activate", self.Position, "end", 0f);
				}
				playerWallBoostTimer = .25f;
				Audio.Position(conveyorLoopSfx, self.Position);

				attachedWallBoosterCurrentSpeed = Calc.Approach(attachedWallBoosterCurrentSpeed, WallBoosterSpeed, 600f * Engine.DeltaTime);
				self.Speed.Y = attachedWallBoosterCurrentSpeed;

				Input.Rumble(RumbleStrength.Light, RumbleLength.Short);
			} else if(attachedWallBoosting)
			{
				attachedWallBoosting = false;
				conveyorLoopSfx.setParameterValue("end", 1f);
				conveyorLoopSfx.release();
			}

			return orig(self);
		}

		private static bool AttachedWallBoosterCheck(Player player)
		{
			foreach (AttachedWallBooster wallbooster in player.Scene.Tracker.GetEntities<AttachedWallBooster>())
			{
				if (player.Facing == wallbooster.Facing && player.CollideCheck(wallbooster)) return true;
			}
			return false;
		}
        #endregion
    }
}
