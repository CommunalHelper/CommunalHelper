using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;

namespace Celeste.Mod.CommunalHelper {
	[CustomEntity("CommunalHelper/DreamFallingBlock")]
	[TrackedAs(typeof(DreamBlock))]
	class DreamFallingBlock : CustomDreamBlock {
		public bool Triggered;
		public float FallDelay;

		public bool HasStartedFalling {
			get;
			private set;
		}

		public DreamFallingBlock(Vector2 position, int width, int height, bool featherMode, bool oneUse)
			: base(position, width, height, featherMode, oneUse) {
			Add(new Coroutine(Sequence()));
		}

		public DreamFallingBlock(EntityData data, Vector2 offset)
			: this(data.Position + offset, data.Width, data.Height, data.Bool("featherMode", false), data.Bool("oneUse", false)) {
		}

        public override void Awake(Scene scene) {
            base.Awake(scene);
			SetupParticles(Width, Height);
        }

        public override void OnStaticMoverTrigger(StaticMover sm) {
			Triggered = true;
		}

		private bool PlayerWaitCheck() {
			if (Triggered) {
				return true;
			}
			if (HasPlayerRider()) {
				return true;
			}
			return CollideCheck<Player>(Position - Vector2.UnitX) || CollideCheck<Player>(Position + Vector2.UnitX);
		}

		private IEnumerator Sequence() {
			while (!Triggered && !HasPlayerRider()) {
				yield return null;
			}
			while (FallDelay > 0f) {
				FallDelay -= Engine.DeltaTime;
				yield return null;
			}
			HasStartedFalling = true;
			while (true) {
				ShakeSfx();
				StartShaking();
				Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
				yield return 0.2f;
				float timer = 0.4f;
				while (timer > 0f && PlayerWaitCheck()) {
					yield return null;
					timer -= Engine.DeltaTime;
				}
				StopShaking();
				for (int i = 2; (float)i < Width; i += 4) {
					if (Scene.CollideCheck<Solid>(TopLeft + new Vector2(i, -2f))) {
						SceneAs<Level>().Particles.Emit(FallingBlock.P_FallDustA, 2, new Vector2(X + (float)i, Y), Vector2.One * 4f, (float)Math.PI / 2f);
					}
					SceneAs<Level>().Particles.Emit(FallingBlock.P_FallDustB, 2, new Vector2(X + (float)i, Y), Vector2.One * 4f);
				}
				float speed = 0f;
				float maxSpeed = 160f;
				while (true) {
					Level level = SceneAs<Level>();
					speed = Calc.Approach(speed, maxSpeed, 500f * Engine.DeltaTime);
					if (MoveVCollideSolids(speed * Engine.DeltaTime, thruDashBlocks: true)) {
						break;
					}
					if (Top > (float)(level.Bounds.Bottom + 16) || (Top > (float)(level.Bounds.Bottom - 1) && CollideCheck<Solid>(Position + new Vector2(0f, 1f)))) {
						Collidable = (Visible = false);
						yield return 0.2f;
						if (level.Session.MapData.CanTransitionTo(level, new Vector2(Center.X, Bottom + 12f))) {
							yield return 0.2f;
							SceneAs<Level>().Shake();
							Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
						}
						RemoveSelf();
						DestroyStaticMovers();
						yield break;
					}
					yield return null;
				}
				ImpactSfx();
				Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
				SceneAs<Level>().DirectionalShake(Vector2.UnitY, 0.3f);
				StartShaking();
				LandParticles();
				yield return 0.2f;
				StopShaking();
				if (CollideCheck<SolidTiles>(Position + new Vector2(0f, 1f))) {
					break;
				}
				while (CollideCheck<Platform>(Position + new Vector2(0f, 1f))) {
					yield return 0.1f;
				}
			}
			Safe = true;
		}

        public override void Render() {
			Position += Shake;
            base.Render();
			Position -= Shake;
        }

        private void LandParticles() {
			for (int i = 2; (float)i <= base.Width; i += 4) {
				if (base.Scene.CollideCheck<Solid>(base.BottomLeft + new Vector2(i, 3f))) {
					SceneAs<Level>().ParticlesFG.Emit(FallingBlock.P_FallDustA, 1, new Vector2(base.X + (float)i, base.Bottom), Vector2.One * 4f, -(float)Math.PI / 2f);
					float direction = (!((float)i < base.Width / 2f)) ? 0f : ((float)Math.PI);
					SceneAs<Level>().ParticlesFG.Emit(FallingBlock.P_LandDust, 1, new Vector2(base.X + (float)i, base.Bottom), Vector2.One * 4f, direction);
				}
			}
		}

		private void ShakeSfx() {
			Audio.Play("event:/game/general/fallblock_shake", base.Center);
		}

		private void ImpactSfx() {
			Audio.Play("event:/game/general/fallblock_impact", base.BottomCenter);
		}
	}
}
