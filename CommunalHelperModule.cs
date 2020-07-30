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

		#region Setup
		public override void Load() {
            On.Celeste.Player.DreamDashBegin += modDreamDashBegin;

			DreamRefillHooks.hook();
		}

        public override void Unload() {
            On.Celeste.Player.DreamDashBegin -= modDreamDashBegin;

			DreamRefillHooks.unhook();
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
