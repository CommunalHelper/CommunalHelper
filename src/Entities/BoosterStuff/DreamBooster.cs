using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using System.Collections;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/DreamBooster")]
    public class DreamBooster : CustomBooster {

        public class DreamBoosterPathRenderer : Entity {
            private DreamBooster dreamBooster;

            public float Alpha;
            public float Percent = 1f;
            public float RainbowLerp;

            private Vector2 perp;

            public DreamBoosterPathRenderer(DreamBooster booster, float alpha) {
                Depth = Depths.DreamBlocks + 1;
                dreamBooster = booster;

                Alpha = Percent = alpha;
                perp = dreamBooster.Dir.Perpendicular();
            }

            public override void Update() {
                base.Update();
                if (dreamBooster.BoostingPlayer)
                    RainbowLerp += Engine.DeltaTime * 8f;
            }

            public override void Render() {
                base.Render();
                if (Alpha <= 0f)
                    return;

                Color color = dreamBooster.BoostingPlayer ? Util.ColorArrayLerp(RainbowLerp, DreamColors) : Color.White;

                Util.TryGetPlayer(out Player player);
                for (float f = 0f; f < dreamBooster.Length * Percent; f += 6f) {
                    DrawPathLine(f, player, color);
                }
                DrawPathLine(dreamBooster.Length * Percent - dreamBooster.Length % 6, null, Color.White);
            }

            private void DrawPathLine(float linePos, Player player, Color lerp) {
                Vector2 pos = dreamBooster.Start + dreamBooster.Dir * linePos;
                float sin = (float) Math.Sin(linePos + Scene.TimeActive * 6f) * 0.3f + 1f;

                float highlight = player == null ? 0.25f : Calc.ClampedMap(Vector2.Distance(player.Center, pos), 0, 80);
                float lineHighlight = (1 - highlight) * 2.5f + 0.75f;
                float alphaHighlight = 1 - Calc.Clamp(highlight, 0.01f, 0.8f);
                Color color = Color.Lerp(Color.White, lerp, 1 - highlight) * alphaHighlight;

                float lineLength = lineHighlight * sin;
                Vector2 lineOffset = perp * lineLength;

                // Single perpendicular short segments
                //Draw.Line(pos + lineOffset, pos - lineOffset, color * Alpha);

                // "Arrow" style
                Vector2 arrowOffset = -dreamBooster.Dir * lineLength;
                Draw.Line(pos, pos - lineOffset + arrowOffset, color * Alpha);
                Draw.Line(pos, pos + lineOffset + arrowOffset, color * Alpha);
            }
        }

        private DreamBoosterPathRenderer pathRenderer;
        private bool showPath = true;

        public float Length;
        public Vector2 Start, Target, Dir;

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

        public DreamBooster(EntityData data, Vector2 offset) 
            : this(data.Position + offset, data.Nodes[0] + offset, !data.Bool("hidePath")) { }

        public DreamBooster(Vector2 position, Vector2 node, bool showPath) 
            : base(position, redBoost: true) {
            Depth = Depths.DreamBlocks;

            Target = node;
            Dir = Calc.SafeNormalize(Target - Position);
            Length = Vector2.Distance(position, Target);
            Start = position;

            this.showPath = showPath;

            ReplaceSprite(CommunalHelperModule.SpriteBank.Create("dreamBooster"));
            SetParticleColors(BurstColor, AppearColor);
            SetSoundEvent(
                showPath ? CustomSFX.game_customBoosters_dreamBooster_dreambooster_enter : CustomSFX.game_customBoosters_dreamBooster_dreambooster_enter_cue,
                CustomSFX.game_customBoosters_dreamBooster_dreambooster_move, 
                false);
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            scene.Add(pathRenderer = new DreamBoosterPathRenderer(this, Util.ToInt(showPath)));
        }

        public override void Removed(Scene scene) {
            base.Removed(scene);
            scene.Remove(pathRenderer);
            pathRenderer = null;
            base.Removed(scene);
        }

        protected override void OnPlayerEnter(Player player) {
            base.OnPlayerEnter(player);
            pathRenderer.RainbowLerp = Calc.Random.Range(0, 8);
            if (!showPath) Add(new Coroutine(HiddenPathReact()));
        }

        private IEnumerator HiddenPathReact() {
            float duration = 0.5f;
            float timer = 0f;

            showPath = true;
            SetSoundEvent(
                CustomSFX.game_customBoosters_dreamBooster_dreambooster_enter,
                CustomSFX.game_customBoosters_dreamBooster_dreambooster_move,
                false);

            ParticleSystem particlesBG = SceneAs<Level>().ParticlesBG;
            while (timer < duration) {
                timer += Engine.DeltaTime;
                pathRenderer.Alpha = pathRenderer.Percent = Ease.SineOut(timer / duration);

                Vector2 pos = Start + Dir * pathRenderer.Percent * Length;

                particlesBG.Emit(DreamParticles[Calc.Random.Range(0, 8)], 2, pos, Vector2.One * 2f, (-Dir).Angle());
                yield return null;
            }
        }

        public override void Render() {
            base.Render();
        }

        public static void InitializeParticles() {
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
            On.Celeste.Booster.BoostRoutine -= Booster_BoostRoutine;
            On.Celeste.Actor.MoveH -= Actor_MoveH;
            On.Celeste.Actor.MoveV -= Actor_MoveV;
            On.Celeste.Player.OnCollideH -= Player_OnCollideH;
            On.Celeste.Player.OnCollideV -= Player_OnCollideV;
        }

        public static void Hook() {
            On.Celeste.Player.RedDashCoroutine += Player_RedDashCoroutine;
            On.Celeste.Player.RedDashUpdate += Player_RedDashUpdate;
            On.Celeste.Booster.BoostRoutine += Booster_BoostRoutine;
            On.Celeste.Actor.MoveH += Actor_MoveH;
            On.Celeste.Actor.MoveV += Actor_MoveV;
            On.Celeste.Player.OnCollideH += Player_OnCollideH;
            On.Celeste.Player.OnCollideV += Player_OnCollideV;
        }

        private static IEnumerator Booster_BoostRoutine(On.Celeste.Booster.orig_BoostRoutine orig, Booster self, Player player, Vector2 dir) {
            IEnumerator origEnum = orig(self, player, dir);
            while (origEnum.MoveNext())
                yield return origEnum.Current;

            // could have done this in Booster.PlayerReleased, but it doesn't pass the player object
            if (self is DreamBooster booster) {
                float angle = booster.Dir.Angle() - 0.5f;
                for (int i = 0; i < 20; i++) {
                    booster.SceneAs<Level>().ParticlesBG.Emit(DreamBooster.P_BurstExplode, 1, player.Center, new Vector2(3f, 3f), angle + Calc.Random.NextFloat());
                }
            }
        }

        private static int Player_RedDashUpdate(On.Celeste.Player.orig_RedDashUpdate orig, Player self) {
            if (self.LastBooster is DreamBooster && !DreamTunnelDash.HasDreamTunnelDash && self.CollideCheck<Solid, DreamBlock>())
                self.LastBooster.Ch9HubTransition = true; // If for whatever reason this becomes an actual option for DreamBoosters, this will need to be changed.

            int result = orig(self);

            if (self.LastBooster is DreamBooster booster) {
                self.LastBooster.Ch9HubTransition = false;
                booster.LoopingSfxParam("dream_tunnel", Util.ToInt(self.CollideCheck<Solid, DreamBlock>()));
                if (Vector2.Distance(self.Center, booster.Start) >= booster.Length) {
                    self.Position = booster.Target;
                    self.SceneAs<Level>().DirectionalShake(booster.Dir, 0.175f);
                    return 0;
                }
            }

            return result;
        }

        private static IEnumerator Player_RedDashCoroutine(On.Celeste.Player.orig_RedDashCoroutine orig, Player self) {
            // get the booster now, it'll be set to null in the coroutine
            Booster currentBooster = self.CurrentBooster;

            // do the entire coroutine, thanks max480 :)
            IEnumerator origEnum = orig(self);
            while (origEnum.MoveNext())
                yield return origEnum.Current;

            if (currentBooster is DreamBooster booster) {
                DynData<Player> playerData = new DynData<Player>(self);
                self.Speed = ((Vector2)(playerData["gliderBoostDir"] = self.DashDir = booster.Dir)) * 240f;
                self.SceneAs<Level>().DirectionalShake(self.DashDir, 0.2f);
                if (self.DashDir.X != 0f) {
                    self.Facing = (Facings) Math.Sign(self.DashDir.X);
                }
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
                DreamBooster booster = self.LastBooster as DreamBooster;
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
