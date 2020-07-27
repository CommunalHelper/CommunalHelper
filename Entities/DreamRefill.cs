using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;

namespace Celeste.Mod.CommunalHelper {
    [CustomEntity("CommunalHelper/DreamRefill")]
    [Tracked]
    class DreamRefill : Entity {
        public static ParticleType[] shatterPaticles;
		private int shatterParticleIndex = 0;
		public static ParticleType[] regenParticles;
		private int regenParticleIndex = 0;
		public static ParticleType[] glowParticles;
		private int glowParticleIndex = 0;

        private Sprite sprite;
		private Sprite flash;
		private Image outline;
		private Wiggler wiggler;
		private BloomPoint bloom;
		private VertexLight light;

		private Level level;
		private SineWave sine;

		private bool oneUse;
		private float respawnTimer;

		static DreamRefill() {
			shatterPaticles = new ParticleType[] { Refill.P_Shatter, null, null, null };
			regenParticles = new ParticleType[] { Refill.P_Regen, null, null, null }; ;
			glowParticles = new ParticleType[] { Refill.P_Glow, null, null, null }; ;
			ParticleType[][] particles = new ParticleType[][] { shatterPaticles, regenParticles, glowParticles };

			for (int i = 0; i < 3; ++i) {
				ParticleType particle = new ParticleType(particles[i][0]);
				particle.ColorMode = ParticleType.ColorModes.Choose;

				particle.Color = Calc.HexToColor("FFEF11");
				particle.Color2 = Calc.HexToColor("FF00D0");
				particles[i][0] = particle;

				particle = new ParticleType(particle);
				particle.Color = Calc.HexToColor("08a310");
				particle.Color2 = Calc.HexToColor("5fcde4");
				particles[i][1] = particle;

				particle = new ParticleType(particle);
				particle.Color = Calc.HexToColor("7fb25e");
				particle.Color2 = Calc.HexToColor("E0564C");
				particles[i][2] = particle;

				particle = new ParticleType(particle);
				particle.Color = Calc.HexToColor("5b6ee1");
				particle.Color2 = Calc.HexToColor("CC3B3B");
				particles[i][3] = particle;
			}
		}

		public DreamRefill(Vector2 position, bool oneUse)
			: base(position) {
			base.Collider = new Hitbox(16f, 16f, -8f, -8f);
			Add(new PlayerCollider(OnPlayer));
			this.oneUse = oneUse;
			Add(outline = new Image(GFX.Game["objects/refill/outline"]));
			outline.CenterOrigin();
			outline.Visible = false;
			Add(sprite = new Sprite(GFX.Game, "objects/CommunalHelper/dreamRefill/idle"));
			sprite.AddLoop("idle", "", 0.1f);
			sprite.Play("idle");
			sprite.CenterOrigin();
			Add(flash = new Sprite(GFX.Game, "objects/refill/flash"));
			flash.Add("flash", "", 0.05f);
			flash.OnFinish = delegate
			{
				flash.Visible = false;
			};
			flash.CenterOrigin();
			Add(wiggler = Wiggler.Create(1f, 4f, delegate (float v) {
				sprite.Scale = (flash.Scale = Vector2.One * (1f + v * 0.2f));
			}));
			Add(new MirrorReflection());
			Add(bloom = new BloomPoint(0.8f, 16f));
			Add(light = new VertexLight(Color.White, 1f, 16, 48));
			Add(sine = new SineWave(0.6f, 0f));
			sine.Randomize();
			UpdateY();
			base.Depth = -100;
		}

		public DreamRefill(EntityData data, Vector2 offset)
			: this(data.Position + offset, data.Bool("oneUse")) {
		}

		public override void Added(Scene scene) {
			base.Added(scene);
			level = SceneAs<Level>();
		}

		public override void Update() {
			base.Update();
			if (respawnTimer > 0f) {
				respawnTimer -= Engine.DeltaTime;
				if (respawnTimer <= 0f) {
					Respawn();
				}
			} else if (base.Scene.OnInterval(0.1f)) {
				level.ParticlesFG.Emit(glowParticles[glowParticleIndex], 1, Position, Vector2.One * 5f);
				++glowParticleIndex;
				glowParticleIndex %= 4;
			}
			UpdateY();
			light.Alpha = Calc.Approach(light.Alpha, sprite.Visible ? 1f : 0f, 4f * Engine.DeltaTime);
			bloom.Alpha = light.Alpha * 0.8f;
			if (base.Scene.OnInterval(2f) && sprite.Visible) {
				flash.Play("flash", restart: true);
				flash.Visible = true;
			}
		}

		private void Respawn() {
			if (!Collidable) {
				Collidable = true;
				sprite.Visible = true;
				outline.Visible = false;
				base.Depth = -100;
				wiggler.Start();
				Audio.Play("event:/game/general/diamond_return", Position);
				for (int i = 0; i < 16; ++i) {
					level.ParticlesFG.Emit(regenParticles[regenParticleIndex], 1, Position, Vector2.One * 2f);
					++regenParticleIndex;
					regenParticleIndex %= 4;
				}
			}
		}

		private void UpdateY() {
			Sprite obj = flash;
			Sprite obj2 = sprite;
			float num2 = bloom.Y = sine.Value * 2f;
			obj.Y = (obj2.Y = num2);
		}

		public override void Render() {
			if (sprite.Visible) {
				sprite.DrawOutline();
			}
			base.Render();
		}

		private void OnPlayer(Player player) {
			var playerData = CommunalHelperModule.getPlayerData(player);
			if (player.Stamina < 20f || !playerData.Get<bool>("hasDreamTunnelDash")) {
				player.RefillDash();
				player.RefillStamina();
				playerData["hasDreamTunnelDash"] = true;

				Audio.Play("event:/game/general/diamond_touch", Position);
				Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
				Collidable = false;
				Add(new Coroutine(RefillRoutine(player)));
				respawnTimer = 2.5f;
			}
		}

		private IEnumerator RefillRoutine(Player player) {
			Celeste.Freeze(0.05f);
			yield return null;
			level.Shake();
			sprite.Visible = (flash.Visible = false);
			if (!oneUse) {
				outline.Visible = true;
			}
			Depth = 8999;
			yield return 0.05f;
			float angle = player.Speed.Angle();
			for (int i = 0; i < 5; ++i) {
				level.ParticlesFG.Emit(shatterPaticles[shatterParticleIndex], 1, Position, Vector2.One * 4f, angle - (float)Math.PI / 2f);
				++shatterParticleIndex;
				shatterParticleIndex %= 4;
			}
			for (int i = 0; i < 5; ++i) {
				level.ParticlesFG.Emit(shatterPaticles[shatterParticleIndex], 1, Position, Vector2.One * 4f, angle + (float)Math.PI / 2f);
				++shatterParticleIndex;
				shatterParticleIndex %= 4;
			}
			SlashFx.Burst(Position, angle);
			if (oneUse) {
				RemoveSelf();
			}
		}
	}
}
