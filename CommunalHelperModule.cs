using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Reflection;
using System.Xml.Serialization;

// TODO
// * Fix displacement bursts
// * Fix sprite updating
// * Fix not being able to downwards diagonal dream tunnel dash when standing on a surface
// * Fix being able to tunnel into another room
// * Fix not dieing from colliding with a dream block when dream tunnel dashing
// * add dreamTunnelDash timer so that the dream tunnel dash is effectively consumed

namespace Celeste.Mod.CommunalHelper {
    public class CommunalHelperModule : EverestModule {
        public static CommunalHelperModule Instance;
		private static DynData<Player> _playerData = null;

		#region Vanilla constants
		private const float DashSpeed = 240f;
		private const float ClimbMaxStamina = 110f;
		private const float DreamDashMinTime = 0.1f;
		#endregion

		#region Setup
		public override void Load() {
            On.Celeste.Player.DreamDashBegin += modDreamDashBegin;

			On.Celeste.Player.ctor += modPlayerCtor;
			On.Celeste.Player.OnCollideH += modOnCollideH;
			On.Celeste.Player.OnCollideV += modOnCollideV;
			On.Celeste.Player.OnBoundsH += modOnBoundsH;
			On.Celeste.Player.OnBoundsV += modOnBoundsV;
			On.Celeste.Player.Die += modPlayerDie;
		}

        public override void Unload() {
            On.Celeste.Player.DreamDashBegin -= modDreamDashBegin;

			On.Celeste.Player.ctor -= modPlayerCtor;
			On.Celeste.Player.OnCollideH -= modOnCollideH;
			On.Celeste.Player.OnCollideV -= modOnCollideV;
			On.Celeste.Player.OnBoundsH -= modOnBoundsH;
			On.Celeste.Player.OnBoundsV -= modOnBoundsV;
			On.Celeste.Player.Die -= modPlayerDie;
		}
        #endregion

        #region Ensures the player always properly enters a dream block even when it's moving fast
        private void modDreamDashBegin(On.Celeste.Player.orig_DreamDashBegin orig, Player player) {
            orig(player);
            player.Position.X += Math.Sign(player.DashDir.X);
            player.Position.Y += Math.Sign(player.DashDir.Y);
        }
		#endregion

		#region Dream tunnel dash/refill stuff
		private static int StDreamTunnelDash;

		private void modPlayerCtor(On.Celeste.Player.orig_ctor orig, Player player, Vector2 position, PlayerSpriteMode spriteMode) {
			orig(player, position, spriteMode);

			var update = new Func<int>(DreamTunnelDashUpdate);
			StDreamTunnelDash = player.StateMachine.AddState(update, null, DreamTunnelDashBegin, DreamTunnelDashEnd);

			getPlayerData(player).Set("hasDreamTunnelDash", false);

        }

        #region State machine extension stuff
        private void DreamTunnelDashBegin() {
			Player player = getPlayer();
			var playerData = getPlayerData(player);

			SoundSource dreamSfxLoop = playerData.Get<SoundSource>("dreamSfxLoop");
			if (dreamSfxLoop == null) {
				playerData["dreamSfxLoop"] = dreamSfxLoop = new SoundSource();
				player.Add(dreamSfxLoop);
			}
			player.Speed = player.DashDir * DashSpeed;
			player.TreatNaive = true;

			// Puts player inside solid so that are are immediately carried with it if it is moving
			player.Position.X += Math.Sign(player.DashDir.X);
			player.Position.Y += Math.Sign(player.DashDir.Y);

			player.Depth = Depths.PlayerDreamDashing;
			playerData["dreamDashCanEndTimer"] = DreamDashMinTime;
			player.Stamina = ClimbMaxStamina;
			playerData["dreamJump"] = false;

			player.Play("event:/char/madeline/dreamblock_enter");
			player.Loop(dreamSfxLoop, "event:/char/madeline/dreamblock_travel");
		}

		private void DreamTunnelDashEnd() {
			Player player = getPlayer();
			var playerData = getPlayerData(player);

			player.Depth = 0;
			if (!playerData.Get<bool>("dreamJump")) {
				player.AutoJump = true;
				player.AutoJumpTimer = 0f;
			}
			if (!player.Inventory.NoRefills) {
				player.RefillDash();
			}
			player.RefillStamina();
			player.TreatNaive = false;

			if (player.DashDir.X != 0f) {
				playerData["jumpGraceTimer"] = 0.1f;
				playerData["dreamJump"] = true;
			} else {
				playerData["jumpGraceTimer"] = 0f;
			}

			Dust.Burst(player.Position, player.Speed.Angle(), 16, null);
			player.Stop(playerData.Get<SoundSource>("dreamSfxLoop"));
			player.Play("event:/char/madeline/dreamblock_exit");
			Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
		}

		private int DreamTunnelDashUpdate() {
			Player player = getPlayer();
			var playerData = getPlayerData(player);

			Input.Rumble(RumbleStrength.Light, RumbleLength.Medium);
			Vector2 position = player.Position;
			player.NaiveMove(player.Speed * Engine.DeltaTime);

			float dreamDashCanEndTimer = playerData.Get<float>("dreamDashCanEndTimer");
			if (dreamDashCanEndTimer > 0f) {
				playerData["dreamDashCanEndTimer"] = dreamDashCanEndTimer - Engine.DeltaTime;
			}
			if (player.CollideCheck<Solid>()) {
				if (player.Scene.OnInterval(0.1f)) {
					CreateTrail(player);
				}

				Level level = playerData.Get<Level>("level");
				if (level.OnInterval(0.04f)) {
					level.Displacement.AddBurst(player.Center, 0.3f, 0f, 40f);
				}
			} else {
				if (DreamTunneledIntoDeath(player)) {
					if (SaveData.Instance.Assists.Invincible) {
						player.Position = position;
						player.Speed *= -1f;
						player.Play("event:/game/general/assist_dreamblockbounce");
					} else {
						player.Die(Vector2.Zero);
					}
				} else if (dreamDashCanEndTimer <= 0f) {
					Celeste.Freeze(0.05f);
					if (Input.Jump.Pressed && player.DashDir.X != 0f) {
						playerData["dreamJump"] = true;
						player.Jump();
					} else if (player.DashDir.Y >= 0f || player.DashDir.X != 0f) {
						if (player.DashDir.X > 0f && player.CollideCheck<Solid>(player.Position - Vector2.UnitX * 5f)) {
							player.MoveHExact(-5);
						} else if (player.DashDir.X < 0f && player.CollideCheck<Solid>(player.Position + Vector2.UnitX * 5f)) {
							player.MoveHExact(5);
						}

						int moveX = playerData.Get<int>("moveX");
						if (Input.Grab.Check && player.ClimbCheck(moveX)) {
							player.Facing = (Facings)moveX;
							if (!SaveData.Instance.Assists.NoGrabbing) {
								return Player.StClimb;
							}
							player.ClimbTrigger(moveX);
							player.Speed.X = 0f;
						}
					}
					return Player.StNormal;
				}
			}
			return StDreamTunnelDash;
		}

		private bool DreamTunneledIntoDeath(Player player) {
			if (player.CollideCheck<DreamBlock>()) {
				for (int i = 1; i <= 5; i++) {
					for (int j = -1; j <= 1; j += 2) {
						for (int k = 1; k <= 5; k++) {
							for (int l = -1; l <= 1; l += 2) {
								Vector2 vector = new Vector2(i * j, k * l);
								if (!player.CollideCheck<DreamBlock>(player.Position + vector)) {
									player.Position += vector;
									return false;
								}
							}
						}
					}
				}
				return true;
			}
			return false;
		}

		private void CreateTrail(Player player) {
			Vector2 scale = new Vector2(Math.Abs(player.Sprite.Scale.X) * (float)player.Facing, player.Sprite.Scale.Y);
			TrailManager.Add(player, scale, player.GetCurrentTrailColor());
        }
        #endregion

		private void modOnCollideH(On.Celeste.Player.orig_OnCollideH orig, Player player, CollisionData data) {
			if (player.StateMachine.State == StDreamTunnelDash) {
				return;
            }
			if (dreamTunnelDashCheck(player, Vector2.UnitX * Math.Sign(player.Speed.X))) {
				player.StateMachine.State = StDreamTunnelDash;

				var playerData = getPlayerData(player);
				playerData["dashAttackTimer"] = 0f;
				playerData["gliderBoostTimer"] = 0f;
				return;
			}
			orig(player, data);
        }

		private void modOnCollideV(On.Celeste.Player.orig_OnCollideV orig, Player player, CollisionData data) {
			if (player.StateMachine.State == StDreamTunnelDash) {
				return;
			}
			if (dreamTunnelDashCheck(player, Vector2.UnitY * Math.Sign(player.Speed.Y))) {
				player.StateMachine.State = StDreamTunnelDash;

				var playerData = getPlayerData(player);
				playerData["dashAttackTimer"] = 0f;
				playerData["gliderBoostTimer"] = 0f;
				return;
			}
			orig(player, data);
		}

		private bool dreamTunnelDashCheck(Player player, Vector2 dir) {
			if (player.DashAttacking && (dir.X == (float)Math.Sign(player.DashDir.X) || dir.Y == (float)Math.Sign(player.DashDir.Y))) {
				var playerData = getPlayerData(player);
				if (player.CollideCheck<Solid>(player.Position + dir) && playerData.Get<bool>("hasDreamTunnelDash")) {
					return true;
                }
				
				//DreamBlock dreamBlock = player.CollideFirst<DreamBlock>(player.Position + dir);
				//if (dreamBlock != null) {
				//	if (player.CollideCheck<Solid, DreamBlock>(player.Position + dir)) {
				//		Vector2 value = new Vector2(Math.Abs(dir.Y), Math.Abs(dir.X));
				//		bool flag;
				//		bool flag2;
				//		if (dir.X != 0f) {
				//			flag = (player.Speed.Y <= 0f);
				//			flag2 = (player.Speed.Y >= 0f);
				//		} else {
				//			flag = (player.Speed.X <= 0f);
				//			flag2 = (player.Speed.X >= 0f);
				//		}
				//		if (flag) {
				//			for (int num = -1; num >= -4; num--) {
				//				Vector2 at = player.Position + dir + value * num;
				//				if (!player.CollideCheck<Solid, DreamBlock>(at)) {
				//					player.Position += value * num;
				//					return true;
				//				}
				//			}
				//		}
				//		if (flag2) {
				//			for (int i = 1; i <= 4; i++) {
				//				Vector2 at2 = player.Position + dir + value * i;
				//				if (!player.CollideCheck<Solid, DreamBlock>(at2)) {
				//					player.Position += value * i;
				//					return true;
				//				}
				//			}
				//		}
				//		return false;
				//	}
				//	return true;
				//}
			}
			return false;
		}

		private void modOnBoundsH(On.Celeste.Player.orig_OnBoundsH orig, Player player) {
			orig(player);
			if (player.StateMachine.State == StDreamTunnelDash) {
				player.Die(Vector2.Zero);
            }
        }

		private void modOnBoundsV(On.Celeste.Player.orig_OnBoundsV orig, Player player) {
			orig(player);
			if (player.StateMachine.State == StDreamTunnelDash) {
				player.Die(Vector2.Zero);
			}
		}

		private PlayerDeadBody modPlayerDie(On.Celeste.Player.orig_Die orig, Player player, Vector2 dir, bool evenIfInvincible = false, bool registerDeathInStats = true) {
			SoundSource dreamSfxLoop = getPlayerData(player).Get<SoundSource>("dreamSfxLoop");
			if (dreamSfxLoop != null) {
				dreamSfxLoop.Stop();
            }
			return orig(player, dir, evenIfInvincible, registerDeathInStats);
        }

		#endregion

		#region Misc
		private Player getPlayer() {
			return (Engine.Scene as Level).Tracker.GetEntity<Player>();
        }

		public static DynData<Player> getPlayerData(Player player) {
			if (_playerData != null && _playerData.Get<Level>("level") != null) {
				return _playerData;
            }
			return _playerData = new DynData<Player>(player);
        }
        #endregion
    }

    // JaThePlayer's code
    internal static class StateMachineExt {
		/// <summary>
		/// Adds a state to a StateMachine
		/// </summary>
		/// <returns>The index of the new state</returns>
		public static int AddState(this StateMachine machine, Func<int> onUpdate, Func<IEnumerator> coroutine = null, Action begin = null, Action end = null) {
			Action[] begins = (Action[])StateMachine_begins.GetValue(machine);
			Func<int>[] updates = (Func<int>[])StateMachine_updates.GetValue(machine);
			Action[] ends = (Action[])StateMachine_ends.GetValue(machine);
			Func<IEnumerator>[] coroutines = (Func<IEnumerator>[])StateMachine_coroutines.GetValue(machine);
			int nextIndex = begins.Length;
			// Now let's expand the arrays
			Array.Resize(ref begins, begins.Length + 1);
			Array.Resize(ref updates, begins.Length + 1);
			Array.Resize(ref ends, begins.Length + 1);
			Array.Resize(ref coroutines, coroutines.Length + 1);
			// Store the resized arrays back into the machine
			StateMachine_begins.SetValue(machine, begins);
			StateMachine_updates.SetValue(machine, updates);
			StateMachine_ends.SetValue(machine, ends);
			StateMachine_coroutines.SetValue(machine, coroutines);
			// And now we add the new functions
			machine.SetCallbacks(nextIndex, onUpdate, coroutine, begin, end);
			return nextIndex;
		}
		private static FieldInfo StateMachine_begins = typeof(StateMachine).GetField("begins", BindingFlags.Instance | BindingFlags.NonPublic);
		private static FieldInfo StateMachine_updates = typeof(StateMachine).GetField("updates", BindingFlags.Instance | BindingFlags.NonPublic);
		private static FieldInfo StateMachine_ends = typeof(StateMachine).GetField("ends", BindingFlags.Instance | BindingFlags.NonPublic);
		private static FieldInfo StateMachine_coroutines = typeof(StateMachine).GetField("coroutines", BindingFlags.Instance | BindingFlags.NonPublic);
	}
}
