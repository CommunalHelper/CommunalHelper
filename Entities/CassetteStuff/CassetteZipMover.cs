using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;


// TODO
// * Ahorn script
namespace Celeste.Mod.CommunalHelper {
	[CustomEntity("CommunalHelper/CassetteZipMover")]
	[TrackedAs(typeof(CassetteBlock))]
	class CassetteZipMover : CassetteBlock {
		private class CassetteZipMoverPathRenderer : Entity {
			public CassetteZipMover zipMover;

			private Color ropeColor = Calc.HexToColor("bfcfde");
			private Color ropeLightColor = Calc.HexToColor("ffffff");
			private Color ropeColorPressed = Calc.HexToColor("324e69");
			private Color ropeLightColorPressed = Calc.HexToColor("667da5");
			private Color undersideColor;

			private MTexture cog;
			private MTexture cogPressed;
			private MTexture cogWhite;
			private Vector2 from;
			private Vector2 to;
			private Vector2 sparkAdd;

			private float sparkDirFromA;
			private float sparkDirFromB;
			private float sparkDirToA;
			private float sparkDirToB;

			private ParticleType sparkParticle;
			private ParticleType sparkParticlePressed;

			public CassetteZipMoverPathRenderer(CassetteZipMover zipMover) {
				Depth = 9000;
				this.zipMover = zipMover;
                from = this.zipMover.start + new Vector2(this.zipMover.Width / 2f, this.zipMover.Height / 2f);
                to = this.zipMover.target + new Vector2(this.zipMover.Width / 2f, this.zipMover.Height / 2f);
				sparkAdd = (from - to).SafeNormalize(5f).Perpendicular();
				float num = (from - to).Angle();
				sparkDirFromA = num + (float)Math.PI / 8f;
				sparkDirFromB = num - (float)Math.PI / 8f;
				sparkDirToA = num + (float)Math.PI - (float)Math.PI / 8f;
				sparkDirToB = num + (float)Math.PI + (float)Math.PI / 8f;
				cog = GFX.Game["objects/CommunalHelper/cassetteZipMover/cog"];
				cogPressed = GFX.Game["objects/CommunalHelper/cassetteZipMover/cogPressed"];
				cogWhite = GFX.Game["objects/CommunalHelper/cassetteZipMover/cogWhite"];

				ropeColor = ropeColor.Mult(zipMover.color);
				ropeLightColor = ropeLightColor.Mult(zipMover.color);
				ropeColorPressed = ropeColorPressed.Mult(zipMover.color);
				ropeLightColorPressed = ropeLightColorPressed.Mult(zipMover.color);
				undersideColor = ropeColorPressed;

				sparkParticle = new ParticleType(ZipMover.P_Sparks) {
					Color = ropeLightColor
				};
				sparkParticlePressed = new ParticleType(ZipMover.P_Sparks) {
					Color = ropeLightColorPressed
				};
			}

			public void CreateSparks() {
				ParticleType particle = zipMover.Collidable ? sparkParticle : sparkParticlePressed;
				SceneAs<Level>().ParticlesBG.Emit(particle, from + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirFromA);
				SceneAs<Level>().ParticlesBG.Emit(particle, from - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirFromB);
				SceneAs<Level>().ParticlesBG.Emit(particle, to + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirToA);
				SceneAs<Level>().ParticlesBG.Emit(particle, to - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirToB);
			}

            public override void Update() {
                base.Update();
				Depth = zipMover.Collidable ? 9000 : 9010;
            }

            public override void Render() {
				for (int i = 1; i <= zipMover.blockHeight; ++i) {
					DrawCogs(zipMover.blockOffset + Vector2.UnitY * i, undersideColor);
				}
				DrawCogs(zipMover.blockOffset);
			}

			private void DrawCogs(Vector2 offset, Color? colorOverride = null) {
				bool pressed = !zipMover.Collidable;
				Color blockColor = zipMover.color;
				Vector2 vector = (to - from).SafeNormalize();
				Vector2 value = vector.Perpendicular() * 3f;
				Vector2 value2 = -vector.Perpendicular() * 4f;
				float rotation = zipMover.percent * (float)Math.PI * 2f;

				Color color = pressed ? ropeColorPressed : ropeColor;
				Color lightColor = pressed ? ropeLightColorPressed : ropeLightColor;
				Draw.Line(from + value + offset, to + value + offset, colorOverride.HasValue ? colorOverride.Value : color);
				Draw.Line(from + value2 + offset, to + value2 + offset, colorOverride.HasValue ? colorOverride.Value : color);
				for (float num = 4f - zipMover.percent * (float)Math.PI * 8f % 4f; num < (to - from).Length(); num += 4f) {
					Vector2 value3 = from + value + vector.Perpendicular() + vector * num;
					Vector2 value4 = to + value2 - vector * num;
					Draw.Line(value3 + offset, value3 + vector * 2f + offset, colorOverride.HasValue ? colorOverride.Value : lightColor);
					Draw.Line(value4 + offset, value4 - vector * 2f + offset, colorOverride.HasValue ? colorOverride.Value : lightColor);
				}
				MTexture cogTex = colorOverride.HasValue ? cogWhite : pressed ? cogPressed : cog;
				cogTex.DrawCentered(from + offset, colorOverride.HasValue ? colorOverride.Value : blockColor, 1f, rotation);
				cogTex.DrawCentered(to + offset, colorOverride.HasValue ? colorOverride.Value : blockColor, 1f, rotation);
			}
		}

		private CassetteZipMoverPathRenderer pathRenderer;

		private Vector2 start;
		private Vector2 target;
		private float percent = 0f;

		private SoundSource sfx = new SoundSource();

		private Color[] colorOptions = new Color[] { 
			Calc.HexToColor("49aaf0"), 
			Calc.HexToColor("f049be"), 
			Calc.HexToColor("fcdc3a"), 
			Calc.HexToColor("38e04e")
		};
		public Color color;
		private static int blockIndex = 4;
		private int beforeIndex;
		public int blockHeight = 2;
		public Vector2 blockOffset = Vector2.Zero;

		public CassetteZipMover(Vector2 position, EntityID id, int width, int height, Vector2 target, int index, float tempo)
			: base(position, id, width, height, index, tempo) {
			base.Depth = -9999;
			start = Position;
			this.target = target;
			beforeIndex = index;
			color = colorOptions[index];
			Index = blockIndex + index;
			blockIndex += 12;
			Add(new Coroutine(Sequence()));
			Add(new LightOcclude());
			sfx.Position = new Vector2(base.Width, base.Height) / 2f;
			Add(sfx);
		}

		public CassetteZipMover(EntityData data, Vector2 offset, EntityID id)
			: this(data.Position + offset, id, data.Width, data.Height, data.Nodes[0] + offset, data.Int("index"), data.Float("tempo", 1f)) {
		}

		public override void Added(Scene scene) {
			base.Added(scene);
			scene.Add(pathRenderer = new CassetteZipMoverPathRenderer(this));
		}

        public override void Awake(Scene scene) {
			base.Awake(scene);
			Index = beforeIndex;
        }

        public override void Removed(Scene scene) {
			scene.Remove(pathRenderer);
			pathRenderer = null;
			base.Removed(scene);
		}

        public override void Render() {
			Vector2 position = Position;
			Position += base.Shake;
			//Draw.Rect(base.X + 1f, base.Y + 1f, base.Width - 2f, base.Height - 2f, Color.Black);
			//int num = 1;
			//float num2 = 0f;
			//int count = innerCogs.Count;
			//for (int i = 4; (float)i <= base.Height - 4f; i += 8) {
			//	int num3 = num;
			//	for (int j = 4; (float)j <= base.Width - 4f; j += 8) {
			//		int index = (int)(mod((num2 + (float)num * percent * (float)Math.PI * 4f) / ((float)Math.PI / 2f), 1f) * (float)count);
			//		MTexture mTexture = innerCogs[index];
			//		Rectangle rectangle = new Rectangle(0, 0, mTexture.Width, mTexture.Height);
			//		Vector2 zero = Vector2.Zero;
			//		if (j <= 4) {
			//			zero.X = 2f;
			//			rectangle.X = 2;
			//			rectangle.Width -= 2;
			//		} else if ((float)j >= base.Width - 4f) {
			//			zero.X = -2f;
			//			rectangle.Width -= 2;
			//		}
			//		if (i <= 4) {
			//			zero.Y = 2f;
			//			rectangle.Y = 2;
			//			rectangle.Height -= 2;
			//		} else if ((float)i >= base.Height - 4f) {
			//			zero.Y = -2f;
			//			rectangle.Height -= 2;
			//		}
			//		mTexture = mTexture.GetSubtexture(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, temp);
			//		mTexture.DrawCentered(Position + new Vector2(j, i) + zero, Color.White * ((num < 0) ? 0.5f : 1f));
			//		num = -num;
			//		num2 += (float)Math.PI / 3f;
			//	}
			//	if (num3 == num) {
			//		num = -num;
			//	}
			//}
			//for (int k = 0; (float)k < base.Width / 8f; k++) {
			//	for (int l = 0; (float)l < base.Height / 8f; l++) {
			//		int num4 = (k != 0) ? (((float)k != base.Width / 8f - 1f) ? 1 : 2) : 0;
			//		int num5 = (l != 0) ? (((float)l != base.Height / 8f - 1f) ? 1 : 2) : 0;
			//		if (num4 != 1 || num5 != 1) {
			//			edges[num4, num5].Draw(new Vector2(base.X + (float)(k * 8), base.Y + (float)(l * 8)));
			//		}
			//	}
			//}
			base.Render();
			Position = position;
		}

		private void ScrapeParticlesCheck(Vector2 to) {
			if (!base.Scene.OnInterval(0.03f)) {
				return;
			}
			bool flag = to.Y != base.ExactPosition.Y;
			bool flag2 = to.X != base.ExactPosition.X;
			if (flag && !flag2) {
				int num = Math.Sign(to.Y - base.ExactPosition.Y);
				Vector2 value = (num != 1) ? base.TopLeft : base.BottomLeft;
				int num2 = 4;
				if (num == 1) {
					num2 = Math.Min((int)base.Height - 12, 20);
				}
				int num3 = (int)base.Height;
				if (num == -1) {
					num3 = Math.Max(16, (int)base.Height - 16);
				}
				if (base.Scene.CollideCheck<Solid>(value + new Vector2(-2f, num * -2))) {
					for (int i = num2; i < num3; i += 8) {
						SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, base.TopLeft + new Vector2(0f, (float)i + (float)num * 2f), (num == 1) ? (-(float)Math.PI / 4f) : ((float)Math.PI / 4f));
					}
				}
				if (base.Scene.CollideCheck<Solid>(value + new Vector2(base.Width + 2f, num * -2))) {
					for (int j = num2; j < num3; j += 8) {
						SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, base.TopRight + new Vector2(-1f, (float)j + (float)num * 2f), (num == 1) ? ((float)Math.PI * -3f / 4f) : ((float)Math.PI * 3f / 4f));
					}
				}
			} else {
				if (!flag2 || flag) {
					return;
				}
				int num4 = Math.Sign(to.X - base.ExactPosition.X);
				Vector2 value2 = (num4 != 1) ? base.TopLeft : base.TopRight;
				int num5 = 4;
				if (num4 == 1) {
					num5 = Math.Min((int)base.Width - 12, 20);
				}
				int num6 = (int)base.Width;
				if (num4 == -1) {
					num6 = Math.Max(16, (int)base.Width - 16);
				}
				if (base.Scene.CollideCheck<Solid>(value2 + new Vector2(num4 * -2, -2f))) {
					for (int k = num5; k < num6; k += 8) {
						SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, base.TopLeft + new Vector2((float)k + (float)num4 * 2f, -1f), (num4 == 1) ? ((float)Math.PI * 3f / 4f) : ((float)Math.PI / 4f));
					}
				}
				if (base.Scene.CollideCheck<Solid>(value2 + new Vector2(num4 * -2, base.Height + 2f))) {
					for (int l = num5; l < num6; l += 8) {
						SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, base.BottomLeft + new Vector2((float)l + (float)num4 * 2f, 0f), (num4 == 1) ? ((float)Math.PI * -3f / 4f) : (-(float)Math.PI / 4f));
					}
				}
			}
		}

		private IEnumerator Sequence() {
			while (true) {
				if (!HasPlayerRider()) {
					yield return null;
					continue;
				}
				sfx.Play("event:/game/01_forsaken_city/zip_mover");
				Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
				StartShaking(0.1f);
				yield return 0.1f;
				StopPlayerRunIntoAnimation = false;
				float at2 = 0f;
				while (at2 < 1f) {
					yield return null;
					at2 = Calc.Approach(at2, 1f, 2f * Engine.DeltaTime);
					percent = Ease.SineIn(at2);
					Vector2 to = Vector2.Lerp(start, target, percent);
					ScrapeParticlesCheck(to);
					if (Scene.OnInterval(0.1f)) {
						pathRenderer.CreateSparks();
					}
					Vector2 diff = to - (Position - blockOffset);
					MoveH(diff.X);
					MoveV(diff.Y);
				}
				StartShaking(0.2f);
				Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
				SceneAs<Level>().Shake();
				StopPlayerRunIntoAnimation = true;
				yield return 0.5f;
				StopPlayerRunIntoAnimation = false;
				float at = 0f;
				while (at < 1f) {
					yield return null;
					at = Calc.Approach(at, 1f, 0.5f * Engine.DeltaTime);
					percent = 1f - Ease.SineIn(at);
					Vector2 to = Vector2.Lerp(target, start, Ease.SineIn(at));
					Vector2 diff = to - (Position - blockOffset);
					MoveH(diff.X);
					MoveV(diff.Y);
				}
				StopPlayerRunIntoAnimation = true;
				StartShaking(0.2f);
				yield return 0.5f;
			}
		}

		private float mod(float x, float m) {
			return (x % m + m) % m;
		}
	}

	class CassetteZipMoverHooks {
		public static void Hook() {
			On.Celeste.CassetteBlock.ShiftSize += modShiftSize;
		}

		public static void Unhook() {
			On.Celeste.CassetteBlock.ShiftSize -= modShiftSize;
		}

		private static void modShiftSize(On.Celeste.CassetteBlock.orig_ShiftSize orig, CassetteBlock block, int amount) {
			if (block is CassetteZipMover) {
				if (block.Activated && block.CollideCheck<Player>()) {
					amount *= -1;
				}
				CassetteZipMover zipMover = block as CassetteZipMover;
				zipMover.blockHeight -= amount;
				zipMover.blockOffset = (2 - zipMover.blockHeight) * Vector2.UnitY;
            }
			orig(block, amount);
		}
    }
}
