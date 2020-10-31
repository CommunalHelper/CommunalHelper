using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/DreamZipMover")]
    public class DreamZipMover : CustomDreamBlock {

        private PathRenderer pathRenderer;

        private Vector2 start;
        private Vector2 target;
        private float percent = 0f;

        private SoundSource sfx;

        private bool dreamAesthetic;
        private bool noReturn;
        private MTexture cross;

        public DreamZipMover(EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height, data.Bool("featherMode"), data.Bool("oneUse"), GetRefillCount(data), data.Bool("below")) {
            start = Position;
            target = data.Nodes[0] + offset;

            noReturn = data.Bool("noReturn");
            dreamAesthetic = data.Bool("dreamAesthetic");

            Add(new Coroutine(Sequence()));
            Add(new LightOcclude());
            Add(sfx = new SoundSource {
                Position = new Vector2(Width / 2f, Height / 2f)
            });
            cross = GFX.Game["objects/CommunalHelper/dreamMoveBlock/x"];
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            scene.Add(pathRenderer = new PathRenderer(this, dreamAesthetic));
        }

        public override void Removed(Scene scene) {
            scene.Remove(pathRenderer);
            pathRenderer = null;
            base.Removed(scene);
        }

        public override void Render() {
            Vector2 position = Position;
            Position += Shake;
            base.Render();
            if (noReturn) {
                cross.DrawCentered(Center + baseData.Get<Vector2>("shake"));
            }
            Position = position;
        }

        private void ScrapeParticlesCheck(Vector2 to) {
            if (!Scene.OnInterval(0.03f)) {
                return;
            }
            bool movingV = to.Y != ExactPosition.Y;
            bool movingH = to.X != ExactPosition.X;
            if (movingV && !movingH) {
                int dir = Math.Sign(to.Y - ExactPosition.Y);
                Vector2 collisionPoint = (dir != 1) ? TopLeft : BottomLeft;
                int particleOffset = 4;
                if (dir == 1) {
                    particleOffset = Math.Min((int) Height - 12, 20);
                }
                int particleHeight = (int) Height;
                if (dir == -1) {
                    particleHeight = Math.Max(16, (int) Height - 16);
                }
                if (Scene.CollideCheck<Solid>(collisionPoint + new Vector2(-2f, dir * -2))) {
                    for (int i = particleOffset; i < particleHeight; i += 8) {
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, TopLeft + new Vector2(0f, i + dir * 2f), (dir == 1) ? (-(float) Math.PI / 4f) : ((float) Math.PI / 4f));
                    }
                }
                if (Scene.CollideCheck<Solid>(collisionPoint + new Vector2(Width + 2f, dir * -2))) {
                    for (int i = particleOffset; i < particleHeight; i += 8) {
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, TopRight + new Vector2(-1f, i + dir * 2f), (dir == 1) ? ((float) Math.PI * -3f / 4f) : ((float) Math.PI * 3f / 4f));
                    }
                }
            } else if (movingH && !movingV) {
                int dir = Math.Sign(to.X - ExactPosition.X);
                Vector2 collisionPoint = (dir != 1) ? TopLeft : TopRight;
                int particleOffset = 4;
                if (dir == 1) {
                    particleOffset = Math.Min((int) Width - 12, 20);
                }
                int particleWidth = (int) Width;
                if (dir == -1) {
                    particleWidth = Math.Max(16, (int) Width - 16);
                }
                if (Scene.CollideCheck<Solid>(collisionPoint + new Vector2(dir * -2, -2f))) {
                    for (int i = particleOffset; i < particleWidth; i += 8) {
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, TopLeft + new Vector2(i + dir * 2f, -1f), (dir == 1) ? ((float) Math.PI * 3f / 4f) : ((float) Math.PI / 4f));
                    }
                }
                if (Scene.CollideCheck<Solid>(collisionPoint + new Vector2(dir * -2, Height + 2f))) {
                    for (int i = particleOffset; i < particleWidth; i += 8) {
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, BottomLeft + new Vector2(i + dir * 2f, 0f), (dir == 1) ? ((float) Math.PI * -3f / 4f) : (-(float) Math.PI / 4f));
                    }
                }
            }
        }

        private IEnumerator Sequence() {
            Vector2 start = Position;
            Vector2 end = target;
            while (true) {
                if (!HasPlayerRider()) {
                    yield return null;
                    continue;
                }
                sfx.Play(PlayerHasDreamDash ? CustomSFX.game_dreamZipMover_dream_zip_mover : SFX.game_01_zipmover);
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
                StartShaking(0.1f);
                yield return 0.1f;

                StopPlayerRunIntoAnimation = false;
                float at = 0f;
                while (at < 1f) {
                    yield return null;
                    at = Calc.Approach(at, 1f, 2f * Engine.DeltaTime);
                    percent = Ease.SineIn(at);
                    Vector2 to = Vector2.Lerp(start, end, percent);
                    ScrapeParticlesCheck(to);
                    if (Scene.OnInterval(0.1f)) {
                        pathRenderer.CreateSparks();
                    }
                    MoveTo(to);
                }
                StartShaking(0.2f);
                Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                SceneAs<Level>().Shake();
                StopPlayerRunIntoAnimation = true;
                yield return 0.5f;

                if (!noReturn) {
                    StopPlayerRunIntoAnimation = false;
                    at = 0f;
                    while (at < 1f) {
                        yield return null;
                        at = Calc.Approach(at, 1f, 0.5f * Engine.DeltaTime);
                        percent = 1f - Ease.SineIn(at);
                        Vector2 to2 = Vector2.Lerp(end, start, Ease.SineIn(at));
                        MoveTo(to2);
                    }
                    StopPlayerRunIntoAnimation = true;
                    StartShaking(0.2f);
                    yield return 0.5f;
                } else {
                    sfx.Stop();
                    Vector2 temp = start;
                    start = end;
                    end = temp;
                }
            }
        }

        protected override void OneUseDestroy() {
            base.OneUseDestroy();
            Scene.Remove(pathRenderer);
            pathRenderer = null;
            sfx.Stop();
        }

        private class PathRenderer : Entity {
            public DreamZipMover DreamZipMover;
            private MTexture cog;
            private MTexture disabledCog;
            private MTexture cogWhite;

            private Vector2 from;
            private Vector2 to;

            private Vector2 sparkAdd;
            private float sparkDirFromA;
            private float sparkDirFromB;
            private float sparkDirToA;
            private float sparkDirToB;

            private static readonly Color ropeColor = Calc.HexToColor("663931");
            private static readonly Color ropeLightColor = Calc.HexToColor("9b6157");

            private static Color[] activeDreamColors = new Color[9];
            private static Color[] disabledDreamColors = new Color[4];

            private bool dreamAesthetic;

            static PathRenderer() {
                activeDreamColors[0] = Calc.HexToColor("FFEF11");
                activeDreamColors[1] = Calc.HexToColor("FF00D0");
                activeDreamColors[2] = Calc.HexToColor("08a310");
                activeDreamColors[3] = Calc.HexToColor("5fcde4");
                activeDreamColors[4] = Calc.HexToColor("7fb25e");
                activeDreamColors[5] = Calc.HexToColor("E0564C");
                activeDreamColors[6] = Calc.HexToColor("5b6ee1");
                activeDreamColors[7] = Calc.HexToColor("CC3B3B");
                activeDreamColors[8] = Calc.HexToColor("7daa64");

                disabledDreamColors[0] = Color.LightGray * 0.5f;
                disabledDreamColors[1] = Color.LightGray * 0.75f;
                disabledDreamColors[2] = Color.LightGray * 1;
                disabledDreamColors[3] = Color.LightGray * 0.75f;
            }

            public PathRenderer(DreamZipMover dreamZipMover, bool dreamAesthetic) {
                Depth = dreamZipMover.Depth + 10;
                DreamZipMover = dreamZipMover;
                this.dreamAesthetic = dreamAesthetic;
                from = DreamZipMover.start + new Vector2(DreamZipMover.Width / 2f, DreamZipMover.Height / 2f);
                to = DreamZipMover.target + new Vector2(DreamZipMover.Width / 2f, DreamZipMover.Height / 2f);
                sparkAdd = (from - to).SafeNormalize(5f).Perpendicular();
                float angle = (from - to).Angle();
                sparkDirFromA = angle + (float) Math.PI / 8f;
                sparkDirFromB = angle - (float) Math.PI / 8f;
                sparkDirToA = angle + (float) Math.PI - (float) Math.PI / 8f;
                sparkDirToB = angle + (float) Math.PI + (float) Math.PI / 8f;
                cog = GFX.Game[dreamAesthetic ? "objects/CommunalHelper/dreamZipMover/cog" : "objects/zipmover/cog"];
                disabledCog = dreamAesthetic ? GFX.Game["objects/CommunalHelper/dreamZipMover/disabledCog"] : cog; // Kinda iffy, but whatever
                cogWhite = GFX.Game["objects/CommunalHelper/dreamZipMover/cogWhite"];
            }

            public void CreateSparks() {
                SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, from + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirFromA);
                SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, from - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirFromB);
                SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, to + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirToA);
                SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, to - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirToB);
            }

            public override void Render() {
                DrawCogs(Vector2.UnitY, Color.Black);
                DrawCogs(Vector2.Zero);
            }

            private void DrawCogs(Vector2 offset, Color? colorOverride = null) {
                bool playerHasDreamDash = DreamZipMover.PlayerHasDreamDash;

                float colorLerp = DreamZipMover.ColorLerp;
                Color colorLerpTarget = DreamZipMover.activeLineColor;
                Vector2 travelDir = (to - from).SafeNormalize();
                Vector2 hOffset1 = travelDir.Perpendicular() * 3f;
                Vector2 hOffset2 = -travelDir.Perpendicular() * 4f;
                float rotation = -DreamZipMover.percent * (float) Math.PI * 2f;

                Color dreamRopeColor = playerHasDreamDash ? colorLerpTarget : DreamZipMover.disabledLineColor;
                Color color = Color.Lerp(dreamAesthetic ?  dreamRopeColor : ropeColor, colorLerpTarget, colorLerp);
                Draw.Line(from + hOffset1 + offset, to + hOffset1 + offset, colorOverride ?? color);
                Draw.Line(from + hOffset2 + offset, to + hOffset2 + offset, colorOverride ?? color);
                float dist = (to - from).Length();
                float shiftProgress = DreamZipMover.percent * (float) Math.PI * 8f;
                for (float lengthProgress = shiftProgress % 4f; lengthProgress < dist; lengthProgress += 4f) {
                    Vector2 value3 = from + hOffset1 + travelDir.Perpendicular() + travelDir * lengthProgress;
                    Vector2 value4 = to + hOffset2 - travelDir * lengthProgress;

                    Color lightColor = ropeLightColor;
                    if (dreamAesthetic) {
                        if (playerHasDreamDash)
                            lightColor = activeDreamColors[(int) mod((float) Math.Round((lengthProgress - shiftProgress) / 4f), 9f)];
                        else
                            lightColor = disabledDreamColors[(int) mod((float) Math.Round((lengthProgress - shiftProgress) / 4f), 4f)];
                    }
                    lightColor = Color.Lerp(lightColor, colorLerpTarget, colorLerp);
                    Draw.Line(value3 + offset, value3 + travelDir * 2f + offset, colorOverride ?? lightColor);
                    Draw.Line(value4 + offset, value4 - travelDir * 2f + offset, colorOverride ?? lightColor);
                }

                (playerHasDreamDash ? cog : disabledCog).DrawCentered(from + offset, colorOverride ?? Color.White, 1f, rotation);
                (playerHasDreamDash ? cog : disabledCog).DrawCentered(to + offset, colorOverride ?? Color.White, 1f, rotation);
                if (colorLerp > 0f && !colorOverride.HasValue) {
                    Color tempColor = Color.Lerp(Color.Transparent, colorLerpTarget, colorLerp);
                    cogWhite.DrawCentered(from + offset, tempColor, 1f, rotation);
                    cogWhite.DrawCentered(to + offset, tempColor, 1f, rotation);
                }
            }

            private float mod(float x, float m) =>
                (x % m + m) % m;
        }

    }
}
