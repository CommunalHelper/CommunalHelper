using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
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
                Depth = Depths.SolidsBelow;
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
                Util.TryGetPlayer(out Player player);
                for (float f = 0f; f < dreamBooster.Length * Percent; f += 6f) {
                    DrawPathLine(f, player, dreamBooster.BoostingPlayer ? GetRainbowColor(RainbowLerp) : Color.White);
                }
                DrawPathLine(dreamBooster.Length * Percent - dreamBooster.Length % 6, null, Color.White);
            }

            private Color GetRainbowColor(float lerp) {
                float m = lerp % DreamColors.Length;
                int fromIndex = (int) Math.Floor(m);
                int toIndex = (fromIndex + 1) % DreamColors.Length;
                float clampedLerp = m - fromIndex;

                return Color.Lerp(DreamColors[fromIndex], DreamColors[toIndex], clampedLerp);
            }

            private void DrawPathLine(float linePos, Player player, Color lerp) {
                Vector2 pos = dreamBooster.Start + dreamBooster.Dir * linePos;
                float sin = (float) Math.Sin(linePos + Scene.TimeActive * 6f) * 0.3f + 1f;

                float highlight = player == null ? 0.25f : Calc.ClampedMap(Vector2.Distance(player.Center, pos), 0, 80);
                float lineHighlight = (1 - highlight) * 3 + 0.75f;
                float alphaHighlight = 1 - Calc.Clamp(highlight, 0.01f, 0.8f);
                Color color = Color.Lerp(Color.White, lerp, 1 - highlight) * alphaHighlight;

                Vector2 lineOffset = perp * lineHighlight * sin;
                Draw.Line(pos + lineOffset, pos - lineOffset, color * Alpha);
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
        }

        public static void Hook() {
            On.Celeste.Player.RedDashCoroutine += Player_RedDashCoroutine;
            On.Celeste.Player.RedDashUpdate += Player_RedDashUpdate;
            On.Celeste.Booster.BoostRoutine += Booster_BoostRoutine;
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
            int result = orig(self);

            if (self.LastBooster is DreamBooster booster) {
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
    }
}
