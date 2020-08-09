using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper {
    public class CommunalHelperModule : EverestModule {
        public static CommunalHelperModule Instance;

		private static DynData<Player> _playerData = null;

		#region Vanilla constants
		private const float DashSpeed = 240f;
        #endregion

        #region Setup
		public override void Load() {
            On.Celeste.Player.DreamDashBegin += modDreamDashBegin;
            On.Celeste.Player.DashCoroutine += modDashCoroutine;

            DreamRefillHooks.Hook();
			CustomDreamBlockHooks.Hook();
            ConnectedDreamBlockHooks.Hook();
			SyncedZipMoverActivationControllerHooks.Hook();
        }

		public override void Unload() {
            On.Celeste.Player.DreamDashBegin -= modDreamDashBegin;
            On.Celeste.Player.DashCoroutine -= modDashCoroutine;

            DreamRefillHooks.Unhook();
			CustomDreamBlockHooks.Unhook();
			ConnectedDreamBlockHooks.Unhook();
			SyncedZipMoverActivationControllerHooks.Unhook();
		}
        #endregion

        #region Ensures the player always properly enters a dream block even when it's moving fast
        private void modDreamDashBegin(On.Celeste.Player.orig_DreamDashBegin orig, Player player) {
            orig(player);
            DreamBlock dreamBlock = getPlayerData(player).Get<DreamBlock>("dreamBlock");
            if (dreamBlock is DreamZipMover || dreamBlock is DreamSwapBlock) {
                player.Position.X += Math.Sign(player.DashDir.X);
                player.Position.Y += Math.Sign(player.DashDir.Y);
            }
        }
        #endregion

        #region Allows downwards diagonal dream tunnel dashing when on the ground 
        private static IEnumerator modDashCoroutine(On.Celeste.Player.orig_DashCoroutine orig, Player player) {
			IEnumerator origEnum = orig(player);
			origEnum.MoveNext();
			yield return origEnum.Current;

			bool forceDownwardDiagonalDash = false;
			Vector2 origDashDir = Input.GetAimVector(player.Facing);
			if (player.OnGround() && origDashDir.X != 0f && origDashDir.Y > 0f && DreamRefillHooks.dreamTunnelDashAttacking) {
				forceDownwardDiagonalDash = true;
			}
			origEnum.MoveNext();
			if (forceDownwardDiagonalDash) {
				player.DashDir = origDashDir;
				player.Speed = origDashDir * DashSpeed;
				if (player.CanUnDuck) {
					player.Ducking = false;
				}
			}
			yield return origEnum.Current;

			origEnum.MoveNext();
		}
        #endregion

        #region Misc
        public static Player getPlayer() {
			return (Engine.Scene as Level).Tracker.GetEntity<Player>();
        }

		public static DynData<Player> getPlayerData(Player player) {
            //if (_playerData != null && _playerData.Get<Level>("level") != null) {
            //    return _playerData;
            //}
            return new DynData<Player>(player);
        }

		public static void log(string str) {
			Logger.Log("Communal Helper", str);
		}
		#endregion
	}

    #region JaThePlayer's state machine extension code
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
    #endregion
}
