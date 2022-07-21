using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using static Celeste.Mod.CommunalHelper.DashStates.SeekerDash;

namespace Celeste.Mod.CommunalHelper.DashStates {
    [CustomEntity("CommunalHelper/SeekerDashRefill")]
    class SeekerDashRefill : DashStateRefill {
        public SeekerDashRefill(EntityData data, Vector2 offset) 
            : base(data, offset) {
        }

        protected override void Activated(Player player) =>
            DashStates.SeekerDash.SetEnabled(true);

        protected override bool CanActivate(Player player) {
            return !HasSeekerDash;
        }

        protected override bool TryCreateCustomSprite(out Sprite sprite) {
            sprite = new Sprite(GFX.Game, "objects/CommunalHelper/seekerDashRefill/idle");
            sprite.AddLoop("idle", "", 0.1f);
            sprite.Play("idle");
            sprite.CenterOrigin();
            return true;
        }

        protected override bool TryCreateCustomOutline(out Image image) {
            image = new(GFX.Game["objects/CommunalHelper/seekerDashRefill/outline"]) {
                Visible = false,
            };
            image.CenterOrigin();
            return true;
        }
    }
}
