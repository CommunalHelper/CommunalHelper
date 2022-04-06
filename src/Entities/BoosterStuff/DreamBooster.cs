using Celeste.Mod.CommunalHelper.DashStates;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using System.Collections;

namespace Celeste.Mod.CommunalHelper.Entities {
    public abstract class DreamBooster : CustomBooster {
        public abstract class PathRendererBase<T> : Entity where T : DreamBooster  {
            public readonly T Booster;

            public float Alpha;
            public float Percent = 1f;

            private float rainbowLerp;
            protected Color CurrentRainbowColor { get; private set; }

            public PathRendererBase(float alpha, T booster) {
                Booster = booster;
                Alpha = Percent = alpha;

                Depth = Depths.DreamBlocks + 1;
            }

            public void ResetRainbow()
                => CurrentRainbowColor = Util.ColorArrayLerp(rainbowLerp = Calc.Random.Range(0, 8), DreamColors);

            public override void Update() {
                base.Update();
                if (Booster.BoostingPlayer)
                    CurrentRainbowColor = Util.ColorArrayLerp(rainbowLerp += Engine.DeltaTime * 8f, DreamColors);
            }

            public void DrawPathLine(Vector2 pos, Vector2 dir, Vector2 perp, float offset, Player player, Color lerp) {
                float sin = (float) Math.Sin(offset + Scene.TimeActive * 6f) * 0.3f + 1f;

                float highlight = .25f;
                if (player != null) {
                    float dSquared = Vector2.DistanceSquared(player.Center, pos);
                    highlight = dSquared <= 6400f ? Calc.ClampedMap((float)Math.Sqrt(dSquared), 0, 80) : 1;
                }   

                float lineHighlight = (1 - highlight) * 2.5f + 0.75f;
                float alphaHighlight = 1 - Calc.Clamp(highlight, 0.01f, 0.8f);
                Color color = Color.Lerp(Color.White, lerp, 1 - highlight) * alphaHighlight;

                float lineLength = lineHighlight * sin;
                Vector2 lineOffset = perp * lineLength;

                // "Arrow" style
                Vector2 arrowOffset = -dir * lineLength;
                Draw.Line(pos, pos - lineOffset + arrowOffset, color * Alpha);
                Draw.Line(pos + lineOffset + arrowOffset, pos, color * Alpha);
            }
        }

        public static ParticleType P_BurstExplode;

        public static readonly Color BurstColor = Calc.HexToColor("19233b");
        public static readonly Color AppearColor = Calc.HexToColor("4d5f6e");

        // red, orange, yellow, green, cyan, blue, purple, pink.
        public static readonly Color[] DreamColors = new Color[8] {
            Calc.HexToColor("ee3566"),
            Calc.HexToColor("ff7b3d"),
            Calc.HexToColor("efdc65"),
            Calc.HexToColor("44bd4c"),
            Calc.HexToColor("3b9c8a"),
            Calc.HexToColor("30a0e6"),
            Calc.HexToColor("af7fc9"),
            Calc.HexToColor("df6da2")
        };
        public static readonly ParticleType[] DreamParticles = new ParticleType[8];

        public DreamBooster(Vector2 position, bool showPath)
            : base (position, redBoost: true) {
            Depth = Depths.DreamBlocks;

            ReplaceSprite(CommunalHelperModule.SpriteBank.Create("dreamBooster"));
            SetParticleColors(BurstColor, AppearColor);
            SetSoundEvent(
                showPath ? CustomSFX.game_customBoosters_dreamBooster_dreambooster_enter : CustomSFX.game_customBoosters_dreamBooster_dreambooster_enter_cue,
                CustomSFX.game_customBoosters_dreamBooster_dreambooster_move,
                false);
        }

        internal static void InitializeParticles() {
            P_BurstExplode = new ParticleType(P_Burst) {
                Color = BurstColor,
                SpeedMax = 250
            };
            for (int i = 0; i < 8; i++) {
                DreamParticles[i] = new ParticleType(P_Appear) {
                    Color = DreamColors[i],
                    SpeedMax = 60
                };
            }
        }
    }

    public class DreamBoosterHooks {
        public static void Unhook() {
            On.Celeste.Player.RedDashCoroutine -= Player_RedDashCoroutine;
            On.Celeste.Player.RedDashUpdate -= Player_RedDashUpdate;
            IL.Celeste.Player.RedDashUpdate -= Player_RedDashUpdate;
            On.Celeste.Booster.BoostRoutine -= Booster_BoostRoutine;
            On.Celeste.Booster.PlayerBoosted -= Booster_PlayerBoosted;
            On.Celeste.Actor.MoveH -= Actor_MoveH;
            On.Celeste.Actor.MoveV -= Actor_MoveV;
            On.Celeste.Player.OnCollideH -= Player_OnCollideH;
            On.Celeste.Player.OnCollideV -= Player_OnCollideV;
        }

        public static void Hook() {
            On.Celeste.Player.RedDashCoroutine += Player_RedDashCoroutine;
            On.Celeste.Player.RedDashUpdate += Player_RedDashUpdate;
            IL.Celeste.Player.RedDashUpdate += Player_RedDashUpdate;
            On.Celeste.Booster.BoostRoutine += Booster_BoostRoutine;
            On.Celeste.Booster.PlayerBoosted += Booster_PlayerBoosted;
            On.Celeste.Actor.MoveH += Actor_MoveH;
            On.Celeste.Actor.MoveV += Actor_MoveV;
            On.Celeste.Player.OnCollideH += Player_OnCollideH;
            On.Celeste.Player.OnCollideV += Player_OnCollideV;
        }

        private static void Booster_PlayerBoosted(On.Celeste.Booster.orig_PlayerBoosted orig, Booster self, Player player, Vector2 direction) {
            orig(self, player, Vector2.Zero);
        }

        private static IEnumerator Booster_BoostRoutine(On.Celeste.Booster.orig_BoostRoutine orig, Booster self, Player player, Vector2 dir) {
            IEnumerator origEnum = orig(self, player, dir);
            while (origEnum.MoveNext())
                yield return origEnum.Current;

            // could have done this in Booster.PlayerReleased, but it doesn't pass the player object
            if (self is DreamBoosterSegment booster) {
                float angle = booster.Dir.Angle() - 0.5f;
                for (int i = 0; i < 20; i++) {
                    booster.SceneAs<Level>().ParticlesBG.Emit(DreamBooster.P_BurstExplode, 1, player.Center, new Vector2(3f, 3f), angle + Calc.Random.NextFloat());
                }

            }
            if (self is DreamBoosterCurve curve) {
                float angle = curve.EndingSpeed.Angle() - 0.5f;
                for (int i = 0; i < 20; i++) {
                    curve.SceneAs<Level>().ParticlesBG.Emit(DreamBooster.P_BurstExplode, 1, player.Center, new Vector2(3f, 3f), angle + Calc.Random.NextFloat());
                }
            }
        }

        private static int Player_RedDashUpdate(On.Celeste.Player.orig_RedDashUpdate orig, Player self) {
            DreamBooster dreamBooster = self.LastBooster as DreamBooster;
            if (dreamBooster != null) {
                bool inSolid = self.CollideCheck<Solid>();

                // Prevent the player from jumping or dashing out of the DreamBooster. May be reset in IL hook below.
                // If for whatever reason this becomes an actual option for DreamBoosters, this will need to be changed.
                if (inSolid)
                    self.LastBooster.Ch9HubTransition = true;

                dreamBooster.LoopingSfxParam("dream_tunnel", Util.ToInt(inSolid));

                if (dreamBooster is DreamBoosterSegment segment) {
                    if (Vector2.Distance(self.Center, segment.Start) >= segment.Length) {
                        self.Position = segment.Target;
                        self.SceneAs<Level>().DirectionalShake(segment.Dir, 0.175f);
                        return 0;
                    }
                }

                if (dreamBooster is DreamBoosterCurve curve) {
                    Vector2 prev = self.Position;
                    Vector2 next = curve.Travel(out bool end);
                    self.MoveToX(next.X - self.DashDir.X * 4);
                    self.MoveToY(next.Y + 8f);
                    if (end) {
                        self.Speed = curve.EndingSpeed;
                        return 0;
                    }
                }
            }

            int ret = orig(self);

            if (dreamBooster != null)
                self.LastBooster.Ch9HubTransition = false;

            return ret;
        }

        private static void Player_RedDashUpdate(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            // We want to reset this *only* if the player has DreamTunnelDash, since will then allow them to dash.
            // The check for whether the player can jump *just* happened, so that is no longer possible.
            cursor.GotoNext(MoveType.After, instr => instr.MatchLdfld<Booster>("Ch9HubTransition"));
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<Player>>(player => {
                if (player.LastBooster is DreamBooster && DreamTunnelDash.HasDreamTunnelDash)
                    player.LastBooster.Ch9HubTransition = false;
            });
        }

        private static IEnumerator Player_RedDashCoroutine(On.Celeste.Player.orig_RedDashCoroutine orig, Player self) {
            // get the booster now, it'll be set to null in the coroutine
            Booster currentBooster = self.CurrentBooster;

            // do the entire coroutine, thanks max480 :)
            IEnumerator origEnum = orig(self);
            while (origEnum.MoveNext())
                yield return origEnum.Current;

            if (currentBooster is DreamBoosterSegment segment) {
                DynData<Player> playerData = new DynData<Player>(self);
                self.Speed = ((Vector2) (playerData["gliderBoostDir"] = self.DashDir = segment.Dir)) * 240f;
                self.SceneAs<Level>().DirectionalShake(self.DashDir, 0.2f);
                if (self.DashDir.X != 0f) {
                    self.Facing = (Facings) Math.Sign(self.DashDir.X);
                }
                yield break;
            }
        }

        // A little bit of jank to make use of collision results
        private static bool dreamBoostMove = false;
        // More jank to indicate an actual collision (disabled DreamBlock)
        private static bool dreamBoostStop = false;

        private static bool Actor_MoveH(On.Celeste.Actor.orig_MoveH orig, Actor self, float moveH, Collision onCollide, Solid pusher) {
            if (self is Player player && player.StateMachine.State == Player.StRedDash && player.LastBooster is DreamBooster booster) {
                DynData<Actor> playerData = new DynData<Actor>(player);
                float pos = player.X;
                Vector2 counter = playerData.Get<Vector2>("movementCounter");
                dreamBoostMove = true;
                if (orig(self, moveH, onCollide, pusher) && !dreamBoostStop) {
                    player.X = pos;
                    playerData["movementCounter"] = counter;
                    player.NaiveMove(Vector2.UnitX * moveH);
                }
                dreamBoostStop = false;
                dreamBoostMove = false;
                return false;
            }
            return orig(self, moveH, onCollide, pusher);
        }

        private static bool Actor_MoveV(On.Celeste.Actor.orig_MoveV orig, Actor self, float moveV, Collision onCollide, Solid pusher) {
            if (self is Player player && player.StateMachine.State == Player.StRedDash && player.LastBooster is DreamBooster booster) {
                DynData<Actor> playerData = new DynData<Actor>(player);
                float pos = player.Y;
                Vector2 counter = playerData.Get<Vector2>("movementCounter");
                dreamBoostMove = true;
                if (orig(self, moveV, onCollide, pusher) && !dreamBoostStop) {
                    player.Y = pos;
                    playerData["movementCounter"] = counter;
                    player.NaiveMove(Vector2.UnitY * moveV);
                }
                dreamBoostStop = false;
                dreamBoostMove = false;
                return false;
            }
            return orig(self, moveV, onCollide, pusher);
        }

        private static void Player_OnCollideH(On.Celeste.Player.orig_OnCollideH orig, Player self, CollisionData data) =>
            Player_OnCollide(new Action<Player, CollisionData>(orig), self, data);

        private static void Player_OnCollideV(On.Celeste.Player.orig_OnCollideV orig, Player self, CollisionData data) =>
            Player_OnCollide(new Action<Player, CollisionData>(orig), self, data);

        private static void Player_OnCollide(Action<Player, CollisionData> orig, Player self, CollisionData data) {
            if (dreamBoostMove) {
                if (data.Hit is not DreamBlock block) {
                    EmitDreamBurst(self, data.Hit.Collider);
                    return;
                }

                if (new DynData<DreamBlock>(block).Get<bool>("playerHasDreamDash")) {
                    self.Die(-data.Moved);
                    return;
                } else
                    dreamBoostStop = true;
            }
            orig(self, data);
        }

        private static void EmitDreamBurst(Player player, Collider worldClipCollider) {
            Level level = player.SceneAs<Level>();
            if (level.OnInterval(0.04f)) {
                DisplacementRenderer.Burst burst = level.Displacement.AddBurst(player.Center, 0.3f, 0f, 40f);
                burst.WorldClipCollider = worldClipCollider;
                burst.WorldClipPadding = 2;
            }
        }
    }
}
