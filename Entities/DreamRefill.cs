using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Collections.Generic;

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

				particles[i][0] = new ParticleType(particle) {
					Color = Calc.HexToColor("FFEF11"),
					Color2 = Calc.HexToColor("FF00D0")
				};

				particles[i][1] = new ParticleType(particle) {
					Color = Calc.HexToColor("08a310"),
					Color2 = Calc.HexToColor("5fcde4")
				};

				particles[i][2] = new ParticleType(particle) {
					Color = Calc.HexToColor("7fb25e"),
					Color2 = Calc.HexToColor("E0564C")
				};

				particles[i][3] = new ParticleType(particle) {
					Color = Calc.HexToColor("5b6ee1"),
					Color2 = Calc.HexToColor("CC3B3B")
				};
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
				Audio.Play("event:/CommunalHelperEvents/game/dreamRefill/dream_refill_return", Position);
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
			if (player.Stamina < 20f || !DreamRefillHooks.hasDreamTunnelDash) {
				player.RefillDash();
				player.RefillStamina();
				DreamRefillHooks.hasDreamTunnelDash = true;

				Audio.Play("event:/CommunalHelperEvents/game/dreamRefill/dream_refill_touch", Position);
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

	// TODO 
	// * make the "Dream tunnel dashing into dream block checking" logic check for solid instead of dreamblock, 
	//   and kill only if can't wrap around dreamblock
	// * investigate exit velocity when dream tunnel dashing into a 1 tile thin wall
	// * fix not being carried by a swap block when dream tunnel dashing into it
	class DreamRefillHooks {

		#region Vanilla constants
		private const float DashSpeed = 240f;
		private const float ClimbMaxStamina = 110f;
		private const float DreamDashMinTime = 0.1f;
		#endregion

		public static int StDreamTunnelDash;
		public static bool hasDreamTunnelDash = false;
		public static bool dreamTunnelDashAttacking = false;

		private static Color[] dreamTrailColors;
		private static int dreamTrailColorIndex = 0;

		static DreamRefillHooks()
		{
			dreamTrailColors = new Color[5];
			dreamTrailColors[0] = Calc.HexToColor("FFEF11");
			dreamTrailColors[1] = Calc.HexToColor("08A310");
			dreamTrailColors[2] = Calc.HexToColor("FF00D0");
			dreamTrailColors[3] = Calc.HexToColor("5FCDE4");
			dreamTrailColors[4] = Calc.HexToColor("E0564C");
		}

		public static void hook() {
            On.Celeste.Player.ctor += modPlayerCtor;
            On.Celeste.Player.DashBegin += modDashBegin;
            On.Celeste.Player.Update += modUpdate;
			On.Celeste.Player.CreateTrail += modCreateTrail;
			On.Celeste.Player.OnCollideH += modOnCollideH;
            On.Celeste.Player.OnCollideV += modOnCollideV;
            On.Celeste.Level.EnforceBounds += modLevelEnforceBounds;
            On.Celeste.Player.Die += modDie;
            On.Celeste.Player.UpdateSprite += modUpdateSprite;
			On.Celeste.Player.IsRiding_Solid += modIsRiding;
        }

		public static void unhook() {
            On.Celeste.Player.ctor -= modPlayerCtor;
            On.Celeste.Player.DashBegin -= modDashBegin;
            On.Celeste.Player.Update -= modUpdate;
			On.Celeste.Player.CreateTrail -= modCreateTrail;
			On.Celeste.Player.OnCollideH -= modOnCollideH;
            On.Celeste.Player.OnCollideV -= modOnCollideV;
            On.Celeste.Level.EnforceBounds -= modLevelEnforceBounds;
            On.Celeste.Player.Die -= modDie;
            On.Celeste.Player.UpdateSprite -= modUpdateSprite;
			On.Celeste.Player.IsRiding_Solid -= modIsRiding;
		}

		// Adds custom dream tunnel dash state
		private static void modPlayerCtor(On.Celeste.Player.orig_ctor orig, Player player, Vector2 position, PlayerSpriteMode spriteMode) {
			orig(player, position, spriteMode);

            var update = new Func<int>(DreamTunnelDashUpdate);
            StDreamTunnelDash = player.StateMachine.AddState(update, null, DreamTunnelDashBegin, DreamTunnelDashEnd);
        }

		// Dream tunnel dash triggering
		private static void modDashBegin(On.Celeste.Player.orig_DashBegin orig, Player player) {
			orig(player);
			if (hasDreamTunnelDash) {
				dreamTunnelDashAttacking = true;
				hasDreamTunnelDash = false;
            }
        }

		// Dream trail creation and dreamTunnelDashAttacking updating
		private static void modUpdate(On.Celeste.Player.orig_Update orig, Player player) {
			orig(player);
			
			Level level = player.Scene as Level;
			if (hasDreamTunnelDash && level.OnInterval(0.1f))
			{
				CreateDreamTrail(player);
			}
			if (!player.DashAttacking) {
				dreamTunnelDashAttacking = false;
			}
		}
		private static void CreateTrail(Player player, Color color) {
			Vector2 scale = new Vector2(Math.Abs(player.Sprite.Scale.X) * (float)player.Facing, player.Sprite.Scale.Y);
			TrailManager.Add(player, scale, color);
		}
		private static void CreateDreamTrail(Player player) {
			CreateTrail(player, dreamTrailColors[dreamTrailColorIndex]);
			++dreamTrailColorIndex;
			dreamTrailColorIndex %= 5;
		}

		// Dream tunnel dash trail recoloring
		private static void modCreateTrail(On.Celeste.Player.orig_CreateTrail orig, Player player) {
			if (dreamTunnelDashAttacking) {
				CreateTrail(player, dreamTrailColors[dreamTrailColorIndex]);
				++dreamTrailColorIndex;
				dreamTrailColorIndex %= 5;
			} else {
				orig(player);
            }
        }

		#region State machine extension stuff
		private static void DreamTunnelDashBegin() {
			Player player = getPlayer();
			var playerData = getPlayerData(player);

			SoundSource dreamSfxLoop = playerData.Get<SoundSource>("dreamSfxLoop");
			if (dreamSfxLoop == null) {
				playerData["dreamSfxLoop"] = dreamSfxLoop = new SoundSource();
				player.Add(dreamSfxLoop);
			}
			player.Speed = player.DashDir * DashSpeed;
			player.TreatNaive = true;

            // Puts player inside solid so that are are immediately carried with it if it is moving
            player.Position.X += Math.Sign(player.DashDir.X);
            player.Position.Y += Math.Sign(player.DashDir.Y);

            player.Depth = Depths.PlayerDreamDashing;
			playerData["dreamDashCanEndTimer"] = DreamDashMinTime;
			player.Stamina = ClimbMaxStamina;
			playerData["dreamJump"] = false;

			player.Play("event:/char/madeline/dreamblock_enter");
			player.Loop(dreamSfxLoop, "event:/char/madeline/dreamblock_travel");
		}

		private static void DreamTunnelDashEnd() {
			Player player = getPlayer();
			var playerData = getPlayerData(player);

			player.Depth = 0;
			if (!playerData.Get<bool>("dreamJump")) {
				player.AutoJump = true;
				player.AutoJumpTimer = 0f;
			}
			if (!player.Inventory.NoRefills) {
				player.RefillDash();
			}
			player.RefillStamina();
			player.TreatNaive = false;

			if (player.DashDir.X != 0f) {
				playerData["jumpGraceTimer"] = 0.1f;
				playerData["dreamJump"] = true;
			} else {
				playerData["jumpGraceTimer"] = 0f;
			}

			Dust.Burst(player.Position, player.Speed.Angle(), 16, null);
			player.Stop(playerData.Get<SoundSource>("dreamSfxLoop"));
			player.Play("event:/char/madeline/dreamblock_exit");
			Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
		}

		private static int DreamTunnelDashUpdate() {
			Player player = getPlayer();
			var playerData = getPlayerData(player);

			Input.Rumble(RumbleStrength.Light, RumbleLength.Medium);
			Vector2 position = player.Position;
			player.NaiveMove(player.Speed * Engine.DeltaTime);

			float dreamDashCanEndTimer = playerData.Get<float>("dreamDashCanEndTimer");
			if (dreamDashCanEndTimer > 0f) {
				dreamDashCanEndTimer -= Engine.DeltaTime;
				playerData["dreamDashCanEndTimer"] = dreamDashCanEndTimer;
			}
			if (player.CollideCheck<Solid, DreamBlock>()) {
				if (player.Scene.OnInterval(0.1f)) {
					CreateDreamTrail(player);
				}

				Level level = playerData.Get<Level>("level");
				if (level.OnInterval(0.04f)) {
					level.Displacement.AddBurst(player.Center, 0.3f, 0f, 40f);
				}
			} else {
				if (DreamTunneledIntoDeath(player)) {
					if (SaveData.Instance.Assists.Invincible) {
						player.Position = position;
						player.Speed *= -1f;
						player.Play("event:/game/general/assist_dreamblockbounce");
					} else {
						player.Die(Vector2.Zero);
					}
				} else if (dreamDashCanEndTimer <= 0f) {
					Celeste.Freeze(0.05f);
					if (Input.Jump.Pressed && player.DashDir.X != 0f) {
						playerData["dreamJump"] = true;
						player.Jump();
					} else if (player.DashDir.Y >= 0f || player.DashDir.X != 0f) {
						if (player.DashDir.X > 0f && player.CollideCheck<DreamBlock>(player.Position - Vector2.UnitX * 5f)) {
							player.MoveHExact(-5);
						} else if (player.DashDir.X < 0f && player.CollideCheck<DreamBlock>(player.Position + Vector2.UnitX * 5f)) {
							player.MoveHExact(5);
						}

						int moveX = playerData.Get<int>("moveX");
						if (Input.Grab.Check && player.ClimbCheck(moveX)) {
							player.Facing = (Facings)moveX;
							if (!SaveData.Instance.Assists.NoGrabbing) {
								return Player.StClimb;
							}
							player.ClimbTrigger(moveX);
							player.Speed.X = 0f;
						}
					}
					return Player.StNormal;
				}
			}
			return StDreamTunnelDash;
		}

		private static bool DreamTunneledIntoDeath(Player player) {
			if (player.CollideCheck<DreamBlock>()) {
				for (int i = 1; i <= 5; i++) {
					for (int j = -1; j <= 1; j += 2) {
						for (int k = 1; k <= 5; k++) {
							for (int l = -1; l <= 1; l += 2) {
								Vector2 vector = new Vector2(i * j, k * l);
								if (!player.CollideCheck<DreamBlock>(player.Position + vector)) {
									player.Position += vector;
									return false;
								}
							}
						}
					}
				}
				return true;
			}
			return false;
		}
		#endregion

		// Dream tunnel dash/dashing into dream block detection 
		private static void modOnCollideH(On.Celeste.Player.orig_OnCollideH orig, Player player, CollisionData data) {
			if (player.StateMachine.State == StDreamTunnelDash) {
				return;
			}

			Vector2 moveDir = new Vector2(Math.Sign(player.Speed.X), 0);

			#region Dream tunnel dashing into dream block checking
			if (dreamTunnelDashAttacking || player.DashAttacking && hasDreamTunnelDash) {
				if (player.CollideCheck<DreamBlock>(player.Position + moveDir) && player.Speed.Y == 0f) {
					bool dashedIntoDreamBlock = true;
					for (int dist = 1; dist <= 4; dist++) {
						for (int dir = 1; dir >= -1; dir -= 2) {
							int offset = dist * dir;
							if (!player.CollideCheck<Solid>(player.Position + new Vector2(moveDir.X, offset))) {
								player.MoveVExact(offset);
								player.MoveHExact((int)moveDir.X);
								dashedIntoDreamBlock = false;
								break;
							}
						}
						if (!dashedIntoDreamBlock) {
							break;
                        }
					}
					if (dashedIntoDreamBlock) {
						player.Die(-moveDir);
						return;
                    }
				}
			}
            #endregion

            if (dreamTunnelDashCheck(player, moveDir)) {
				player.StateMachine.State = StDreamTunnelDash;
				dreamTunnelDashAttacking = false;

				var playerData = getPlayerData(player);
				playerData["dashAttackTimer"] = 0f;
				playerData["gliderBoostTimer"] = 0f;
				return;
			}
			if (!player.Dead) {
				orig(player, data);
			}
		}
		private static void modOnCollideV(On.Celeste.Player.orig_OnCollideV orig, Player player, CollisionData data) {
			if (player.StateMachine.State == StDreamTunnelDash) {
				return;
			}

			Vector2 moveDir = new Vector2(0, Math.Sign(player.Speed.Y));

            #region Dream tunnel dashing into dream block checking
            if (dreamTunnelDashAttacking || player.DashAttacking && hasDreamTunnelDash) {
				if (player.CollideCheck<DreamBlock>(player.Position + moveDir) && player.Speed.X == 0f) {
					bool dashedIntoDreamBlock = true;
					for (int dist = 1; dist <= 4; dist++) {
						for (int dir = 1; dir >= -1; dir -= 2) {
							int offset = dist * dir;
							if (!player.CollideCheck<Solid>(player.Position + new Vector2(offset, moveDir.Y))) {
								player.MoveHExact(offset);
								player.MoveVExact((int)moveDir.Y);
								dashedIntoDreamBlock = false;
								break;
							}
						}
						if (!dashedIntoDreamBlock) {
							break;
						}
					}
					if (dashedIntoDreamBlock) {
						player.Die(-moveDir);
						return;
					}
				}
			}
            #endregion

            if (dreamTunnelDashCheck(player, moveDir)) {
				player.StateMachine.State = StDreamTunnelDash;
				dreamTunnelDashAttacking = false;

				var playerData = getPlayerData(player);
				playerData["dashAttackTimer"] = 0f;
				playerData["gliderBoostTimer"] = 0f;
				return;
			}
			if (!player.Dead) {
				orig(player, data);
			}
		}

		private static bool dreamTunnelDashCheck(Player player, Vector2 dir) {
			if (dreamTunnelDashAttacking) {
                if (player.CollideCheck<Solid, DreamBlock>(player.Position + dir)) {
                    if (player.CollideCheck<DreamBlock>(player.Position + dir)) {
                        Vector2 value = new Vector2(Math.Abs(dir.Y), Math.Abs(dir.X));
                        bool flag;
                        bool flag2;
                        if (dir.X != 0f) {
                            flag = (player.Speed.Y <= 0f);
                            flag2 = (player.Speed.Y >= 0f);
                        } else {
                            flag = (player.Speed.X <= 0f);
                            flag2 = (player.Speed.X >= 0f);
                        }
                        if (flag) {
                            for (int num = -1; num >= -4; num--) {
                                Vector2 at = player.Position + dir + value * num;
                                if (!player.CollideCheck<DreamBlock>(at)) {
                                    player.Position += value * num;
                                    return true;
                                }
                            }
                        }
                        if (flag2) {
                            for (int i = 1; i <= 4; i++) {
                                Vector2 at2 = player.Position + dir + value * i;
                                if (!player.CollideCheck<DreamBlock>(at2)) {
                                    player.Position += value * i;
                                    return true;
                                }
                            }
                        }
                        return false;
                    }
                    return true;
                }
            }
			return false;
		}

		// Kills the player if they dream tunnel dash into the level bounds
		private static void modLevelEnforceBounds(On.Celeste.Level.orig_EnforceBounds orig, Level level, Player player) {
			Rectangle bounds = level.Bounds;
			bool canDie = player.StateMachine.State == StDreamTunnelDash && player.CollideCheck<Solid>();
			if (canDie && (player.Right > bounds.Right || player.Left < bounds.Left || player.Top < bounds.Top || player.Bottom > bounds.Bottom)) {
				player.Die(Vector2.Zero);
			} else {
				orig(level, player);
			}
		}

		// Fixes bug with dreamSfx soundsource not being stopped
		private static PlayerDeadBody modDie(On.Celeste.Player.orig_Die orig, Player player, Vector2 dir, bool evenIfInvincible = false, bool registerDeathInStats = true) {
			hasDreamTunnelDash = false;
			SoundSource dreamSfxLoop = getPlayerData(player).Get<SoundSource>("dreamSfxLoop");
			if (dreamSfxLoop != null) {
				dreamSfxLoop.Stop();
			}
			return orig(player, dir, evenIfInvincible, registerDeathInStats);
		}

		// Updates sprite for dream tunnel dash state
		private static void modUpdateSprite(On.Celeste.Player.orig_UpdateSprite orig, Player player) {
			if (StDreamTunnelDash != 0 && player.StateMachine.State == StDreamTunnelDash) {
				if (player.Sprite.CurrentAnimationID != "dreamDashIn" && player.Sprite.CurrentAnimationID != "dreamDashLoop") {
					player.Sprite.Play("dreamDashIn");
				}
			} else {
				orig(player);
			}
        }

		// Ensures that the player is transported by moving solids when dream tunnel dashing through them
		private static bool modIsRiding(On.Celeste.Player.orig_IsRiding_Solid orig, Player player, Solid solid) {
			if (player.StateMachine.State == StDreamTunnelDash) {
				return player.CollideCheck(solid);
            }
			return orig(player, solid);
        }

		#region Misc
		private static Player getPlayer() {
			return CommunalHelperModule.getPlayer();
		}

		public static DynData<Player> getPlayerData(Player player) {
			return CommunalHelperModule.getPlayerData(player);
		}

		private static void log(string str) {
			CommunalHelperModule.log(str);
        }
		#endregion
	}
}
