using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using Directions = Celeste.Spikes.Directions;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/BouncyPanel")]
    [Tracked]
    public class BouncyPanel : AbstractPanel {

        private string sfx;

        public BouncyPanel(EntityData data, Vector2 offset)
            : base(data, offset) {
            sfx = data.Attr("sfx", SFX.game_assist_dreamblockbounce);
        }

        new internal static void Load() {
            On.Celeste.Player.NormalUpdate += Player_NormalUpdate;
        }

        new internal static void Unload() {
            On.Celeste.Player.NormalUpdate -= Player_NormalUpdate;
        }

        private static int Player_NormalUpdate(On.Celeste.Player.orig_NormalUpdate orig, Player self) {
            int newState = orig(self);

            if (newState == Player.StNormal) {
                // no bounce if the player is going to climb! Climbing should already take priority over bouncing.
                if (!SaveData.Instance.Assists.NoGrabbing && !self.Ducking && self.Stamina >= 20f && Input.GrabCheck && self.ClimbCheck((int) self.Facing)) {
                    return newState;
                }

                Level level = self.SceneAs<Level>();
                Rectangle hitbox = self.Collider.Bounds;

                BouncyPanel bounce = null;

                // check for collision below
                hitbox.Y++;
                if (self.Speed.Y >= 0f) {
                    foreach (BouncyPanel panel in level.CollideAll<BouncyPanel>(hitbox)) {
                        if (panel.Orientation == Directions.Up) {
                            self.SuperBounce(self.Bottom);
                            bounce = panel;
                            break;
                        }
                    }
                }

                if (bounce is null) {
                    // check for collision on the right
                    hitbox.Height--;
                    hitbox.Width++;

                    foreach (BouncyPanel panel in level.CollideAll<BouncyPanel>(hitbox)) {
                        if (panel.Orientation == Directions.Left && self.SideBounce(-1, self.Right, self.CenterY)) {
                            bounce = panel;
                            break;
                        }
                    }
                }

                if (bounce is null) {
                    // check for collision on the left
                    hitbox.X--;

                    foreach (BouncyPanel panel in level.CollideAll<BouncyPanel>(hitbox)) {
                        if (panel.Orientation == Directions.Right && self.SideBounce(1, self.Left, self.CenterY)) {
                            bounce = panel;
                            break;
                        }
                    }
                }

                if (bounce is not null) {
                    Audio.Play(bounce.sfx, self.Center);
                }
            }

            return newState;
        }
    }

}
