using Celeste.Mod.CommunalHelper.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;

namespace Celeste.Mod.CommunalHelper {
    public static class Extensions {

        public static DynData<Player> GetData(this Player player) {
            return new DynData<Player>(player);
        }

        public static Color Mult(this Color color, Color other) {
            color.R = (byte) (color.R * other.R / 256f);
            color.G = (byte) (color.G * other.G / 256f);
            color.B = (byte) (color.B * other.B / 256f);
            color.A = (byte) (color.A * other.A / 256f);
            return color;
        }

        // Dream Tunnel Dash related extension methods located in DreamTunnelDash.cs

        #region WallBoosters

        public static bool AttachedWallBoosterCheck(this Player player) {
            foreach (AttachedWallBooster wallbooster in player.Scene.Tracker.GetEntities<AttachedWallBooster>()) {
                if (player.Facing == wallbooster.Facing && player.CollideCheck(wallbooster))
                    return true;
            }
            return false;
        }

        #endregion

        public static void PointBounce(this Player player, Vector2 from, float force) {
            if (player.StateMachine.State == 2) {
                player.StateMachine.State = 0;
            }
            if (player.StateMachine.State == 4 && player.CurrentBooster != null) {
                player.CurrentBooster.PlayerReleased();
            }
            player.RefillDash();
            player.RefillStamina();
            Vector2 vector = (player.Center - from).SafeNormalize();
            if (vector.Y > -0.2f && vector.Y <= 0.4f) {
                vector.Y = -0.2f;
            }
            player.Speed = vector * force;
            player.Speed.X *= 1.5f;
            if (Math.Abs(player.Speed.X) < 100f) {
                if (player.Speed.X == 0f) {
                    player.Speed.X = -(float) player.Facing * 100f;
                    return;
                }
                player.Speed.X = Math.Sign(player.Speed.X) * 100f;
            }
        }

        public static void PointBounce(this Holdable holdable, Vector2 from) {
            Vector2 vector = (holdable.Entity.Center - from).SafeNormalize();
            if (vector.Y > -0.2f && vector.Y <= 0.4f) {
                vector.Y = -0.2f;
            }
            holdable.Release(vector);
        }

    }
}
