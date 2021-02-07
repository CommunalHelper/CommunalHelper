using Celeste.Mod.CommunalHelper;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;

namespace Celeste.Mod.PillieHelper
{
	[CustomEntity("CommunalHelper/ShieldedRefill")]
    class ShieldedRefill : Entity
    {
		private Sprite sprite;
		private Sprite flash;
		private Image outline;

		private Wiggler wiggler;
		private Wiggler shieldRadiusWiggle, moveWiggle;
		private Vector2 moveWiggleDir;

		private BloomPoint bloom;
		private VertexLight light;
		private SineWave sine;

		private Level level;

		private bool twoDashes;
		private bool oneUse;
		private float respawnTimer;
		private float bubbleScale = 8f;
		private bool bounceBubble;

		private ParticleType p_shatter;
		private ParticleType p_regen;
		private ParticleType p_glow;

		public ShieldedRefill(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Bool("twoDashes"), data.Bool("oneUse"), data.Bool("bubbleRepel"))
        { }

        public ShieldedRefill(Vector2 position, bool twoDashes, bool oneUse, bool repell)
            : base(position)
		{
			base.Collider = new Hitbox(16f, 16f, -8f, -8f);
			Add(new PlayerCollider(OnPlayer));
			this.twoDashes = twoDashes;
			bounceBubble = repell;
			this.oneUse = oneUse;
			string str;
			if (twoDashes)
			{
				str = "objects/refillTwo/";
				p_shatter = Refill.P_ShatterTwo;
				p_regen = Refill.P_RegenTwo;
				p_glow = Refill.P_GlowTwo;
			}
			else
			{
				str = "objects/refill/";
				p_shatter = Refill.P_Shatter;
				p_regen = Refill.P_Regen;
				p_glow = Refill.P_Glow;
			}
			Add(outline = new Image(GFX.Game[str + "outline"]));
			outline.CenterOrigin();
			outline.Visible = false;
			Add(sprite = new Sprite(GFX.Game, str + "idle"));
			sprite.AddLoop("idle", "", 0.1f);
			sprite.Play("idle");
			sprite.CenterOrigin();
			Add(flash = new Sprite(GFX.Game, str + "flash"));
			flash.Add("flash", "", 0.05f);
			flash.OnFinish = delegate
			{
				flash.Visible = false;
			};
			flash.CenterOrigin();
			Add(wiggler = Wiggler.Create(1f, 4f, delegate (float v)
			{
				sprite.Scale = (flash.Scale = Vector2.One * (1f + v * 0.2f));
			}));

			shieldRadiusWiggle = Wiggler.Create(1f, 4f, delegate (float v)
			{
				bubbleScale = 8f + v;
			});
			shieldRadiusWiggle.StartZero = true;
			Add(shieldRadiusWiggle);

			moveWiggle = Wiggler.Create(0.8f, 2f);
			moveWiggle.StartZero = true;
			Add(moveWiggle);

			Add(new MirrorReflection());
			Add(bloom = new BloomPoint(0.8f, 16f));
			Add(light = new VertexLight(Color.White, 1f, 16, 48));
			Add(sine = new SineWave(0.6f, 0f));
			sine.Randomize();
			UpdateY();
			base.Depth = -100;
		}

		public override void Added(Scene scene)
		{
			base.Added(scene);
			level = SceneAs<Level>();
		}

		public override void Update()
		{
			base.Update();
			if (respawnTimer > 0f)
			{
				respawnTimer -= Engine.DeltaTime;
				if (respawnTimer <= 0f)
				{
					Respawn();
				}
			}
			else if (base.Scene.OnInterval(0.1f))
			{
				level.ParticlesFG.Emit(p_glow, 1, Position, Vector2.One * 5f);
			}
			UpdateY();
			light.Alpha = Calc.Approach(light.Alpha, sprite.Visible ? 1f : 0f, 4f * Engine.DeltaTime);
			bloom.Alpha = light.Alpha * 0.8f;
			if (base.Scene.OnInterval(2f) && sprite.Visible)
			{
				flash.Play("flash", restart: true);
				flash.Visible = true;
			}
		}

		private void Respawn()
		{
			if (!Collidable)
			{
				Collidable = true;
				sprite.Visible = true;
				outline.Visible = false;
				base.Depth = -100;
				wiggler.Start();
				shieldRadiusWiggle.Start();
				Audio.Play(twoDashes ? CustomSFX.game_shieldedRefill_pinkdiamond_return : CustomSFX.game_shieldedRefill_diamond_return, Position);
				level.ParticlesFG.Emit(p_regen, 16, Position, Vector2.One * 2f);
			}
		}

		private void UpdateY()
		{
			flash.Position = sprite.Position = bloom.Position = 
				Vector2.UnitY * sine.Value * 2f + (moveWiggleDir * moveWiggle.Value * -8f);
		}

		public override void Render()
		{
			if (sprite.Visible)
			{
				Draw.Circle(sprite.RenderPosition, bubbleScale, Color.White, 10);
				sprite.DrawOutline();
			}
			base.Render();
		}

		private void OnPlayer(Player player)
		{
			if (player.DashAttacking)
			{
				if (player.UseRefill(twoDashes))
				{
					Audio.Play(twoDashes ? "event:/new_content/game/10_farewell/pinkdiamond_touch" : "event:/game/general/diamond_touch", Position);
					Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
					Collidable = false;
					Add(new Coroutine(RefillRoutine(player)));
					respawnTimer = 2.5f;
				} 
			}
			else if(bounceBubble)
			{
				PlayerPointBounce(player, base.Center, refillPlayer: false);
				moveWiggle.Start();
				shieldRadiusWiggle.Start();
				moveWiggleDir = (base.Center - player.Center).SafeNormalize(Vector2.UnitY);
				Audio.Play("event:/game/06_reflection/feather_bubble_bounce", Position);
				Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
				return;
			}
		}

		private IEnumerator RefillRoutine(Player player)
		{
			Celeste.Freeze(0.05f);
			Audio.Play(CustomSFX.game_shieldedRefill_diamondbubble_pop, Position);
			level.ParticlesFG.Emit(Player.P_CassetteFly, 6, Center, Vector2.One * 5f);
			yield return null;
			level.Shake();
			sprite.Visible = (flash.Visible = false);
			if (!oneUse)
			{
				outline.Visible = true;
			}
			Depth = 8999;
			yield return 0.05f;
			float num = player.Speed.Angle();
			level.ParticlesFG.Emit(p_shatter, 5, Position, Vector2.One * 4f, num - (float)Math.PI / 2f);
			level.ParticlesFG.Emit(p_shatter, 5, Position, Vector2.One * 4f, num + (float)Math.PI / 2f);
			SlashFx.Burst(Position, num);
			if (oneUse)
			{
				RemoveSelf();
			}
		}

		public static void PlayerPointBounce(Player player, Vector2 from, bool refillPlayer = false)
		{
			if (player.StateMachine.State == 2)
			{
				player.StateMachine.State = 0;
			}
			if (player.StateMachine.State == 4 && player.CurrentBooster != null)
			{
				player.CurrentBooster.PlayerReleased();
			}
			if(refillPlayer)
			{
				player.RefillDash(); player.RefillStamina();
			}
			Vector2 value = (player.Center - from).SafeNormalize();
			if (value.Y is > (-0.2f) and <= 0.4f)
			{
				value.Y = -0.2f;
			}
			player.Speed = value * 200;
			if (Math.Abs(player.Speed.X) < 80f)
			{
				if (player.Speed.X == 0f)
				{
					player.Speed.X = (float)(0 - player.Facing) * 80;
				}
				else
				{
					player.Speed.X = (float)Math.Sign(player.Speed.X) * 80;
				}
			}
		}
	}
}
