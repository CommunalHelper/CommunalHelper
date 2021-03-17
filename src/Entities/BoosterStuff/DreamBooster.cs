﻿using Celeste.Mod.Entities;
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

            public DreamBoosterPathRenderer(DreamBooster booster, float alpha) {
                Depth = Depths.SolidsBelow;
                dreamBooster = booster;

                Alpha = alpha;
            }

            public override void Render() {
                base.Render();
                if (Alpha <= 0f)
                    return;

                Vector2 perp = dreamBooster.Dir.Perpendicular();
                for (float f = 0f; f < dreamBooster.Length; f += 6f) {
                    Vector2 pos = dreamBooster.Start + dreamBooster.Dir * f;

                    float highlight = Util.TryGetPlayer(out Player player) ? Calc.ClampedMap(Vector2.Distance(player.Center, pos), 0, 80) : 1f;
                    float lineHighlight = (1 - highlight) * 3 + 0.75f;
                    float alphaHighlight = 1 - Calc.Clamp(highlight, 0.01f, 0.8f);
                    Color color = Color.White * alphaHighlight;

                    Draw.Line(pos + perp * lineHighlight, pos - perp * lineHighlight, color * Alpha);

                }
            }
        }

        private DreamBoosterPathRenderer pathRenderer;
        private bool showPath = true;

        public float Length;
        public Vector2 Start, Target, Dir;

        public static ParticleType P_BurstExplode;

        public static readonly Color BurstColor = Calc.HexToColor("19233b");
        public static readonly Color AppearColor = Calc.HexToColor("4d5f6e");

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
                true);
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
            // TODO: path reacts to player entering in some way, cool effects :))
        }

        public override void Render() {
            base.Render();
        }

        public static void InitializeParticles() {
            P_BurstExplode = new ParticleType(P_Burst) {
                Color = BurstColor,
                SpeedMax = 250
            };
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