using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CommunalHelper {
    [CustomEntity("CommunalHelper/SyncedZipMoverActivationController")]
    class SyncedZipMoverActivationController : Entity {
		private Level level;

		private string colorCode; 
		private float resetTimer = 0f;
		private  float resetTime;

		public SyncedZipMoverActivationController(Vector2 position, string colorCode, float zipMoverSpeedMult)
			: base(position) {
			this.colorCode = colorCode;
			this.resetTime = 0.6f + 0.5f / zipMoverSpeedMult;
		}

		public SyncedZipMoverActivationController(EntityData data, Vector2 offset)
			: this(data.Position + offset, data.Attr("colorCode", "000000"), data.Float("zipMoverSpeedMultiplier", 1)) {
		}

		public override void Added(Scene scene) {
			base.Added(scene);
			level = scene as Level;
		}

		public override void Removed(Scene scene) {
			base.Removed(scene);
		}

		public override void Update() {
			base.Update();
			if (resetTimer > 0) {
				resetTimer -= Engine.DeltaTime;
			} else if (Input.Grab.Pressed) {
				level.Session.SetFlag($"ZipMoverSync:{colorCode}");
				resetTimer = resetTime;
			}
		}
	}
}
