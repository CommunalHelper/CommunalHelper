using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/CassetteZipMover")]
    public class CassetteZipMover : CustomCassetteBlock {
        private class PathRenderer : Entity {
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

            public PathRenderer(CassetteZipMover zipMover) {
                Depth = Depths.BGDecals;
                this.zipMover = zipMover;

                from = this.zipMover.start + new Vector2(this.zipMover.Width / 2f, this.zipMover.Height / 2f);
                to = this.zipMover.targets[0] + new Vector2(this.zipMover.Width / 2f, this.zipMover.Height / 2f);
                sparkAdd = (from - to).SafeNormalize(5f).Perpendicular();
                float angle = (from - to).Angle();
                sparkDirFromA = angle + (float) Math.PI / 8f;
                sparkDirFromB = angle - (float) Math.PI / 8f;
                sparkDirToA = angle + (float) Math.PI - (float) Math.PI / 8f;
                sparkDirToB = angle + (float) Math.PI + (float) Math.PI / 8f;

                cog = GFX.Game["objects/CommunalHelper/cassetteZipMover/cog"];
                cogPressed = GFX.Game["objects/CommunalHelper/cassetteZipMover/cogPressed"];
                cogWhite = GFX.Game["objects/CommunalHelper/cassetteZipMover/cogWhite"];

                ropeColor = ropeColor.Mult(zipMover.color);
                ropeLightColor = ropeLightColor.Mult(zipMover.color);
                ropeColorPressed = ropeColorPressed.Mult(zipMover.color);
                ropeLightColorPressed = ropeLightColorPressed.Mult(zipMover.color);
                undersideColor = ropeColorPressed;

                sparkParticle = new ParticleType(ZipMover.P_Sparks) { Color = ropeLightColor };
                sparkParticlePressed = new ParticleType(ZipMover.P_Sparks) { Color = ropeLightColorPressed };
            }

            public void CreateSparks() {
                from = GetNodeFrom(zipMover.start);
                for (int i = 0; i < zipMover.targets.Length; i++) {
                    to = GetNodeFrom(zipMover.targets[i]);
                    ParticleType particle = zipMover.Collidable ? sparkParticle : sparkParticlePressed;
                    SceneAs<Level>().ParticlesBG.Emit(particle, from + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirFromA);
                    SceneAs<Level>().ParticlesBG.Emit(particle, from - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirFromB);
                    SceneAs<Level>().ParticlesBG.Emit(particle, to + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirToA);
                    SceneAs<Level>().ParticlesBG.Emit(particle, to - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirToB);
                    from = GetNodeFrom(zipMover.targets[i]);
                }
            }

            private Vector2 GetNodeFrom(Vector2 node) {
                return node + new Vector2(zipMover.Width / 2f, zipMover.Height / 2f);
            }

            public override void Update() {
                base.Update();
                Depth = zipMover.Collidable ? Depths.BGDecals : Depths.BGDecals + 10;
            }

            public override void Render() {
                from = GetNodeFrom(zipMover.start);
                for (int j = 0; j < zipMover.targets.Length; j++) {
                    for (int i = 1; i <= zipMover.blockHeight; ++i) {
                        to = GetNodeFrom(zipMover.targets[j]);
                        DrawCogs(zipMover.blockOffset + Vector2.UnitY * i, undersideColor);
                        from = GetNodeFrom(zipMover.targets[j]);
                    }
                }
                from = GetNodeFrom(zipMover.start);
                for (int j = 0; j < zipMover.targets.Length; j++) {
                    to = GetNodeFrom(zipMover.targets[j]);
                    DrawCogs(zipMover.blockOffset);
                    from = GetNodeFrom(zipMover.targets[j]);
                }

            }

            private void DrawCogs(Vector2 offset, Color? colorOverride = null) {
                bool pressed = !zipMover.Collidable;
                Color blockColor = zipMover.color;
                Vector2 vector = (to - from).SafeNormalize();
                Vector2 value = vector.Perpendicular() * 3f;
                Vector2 value2 = -vector.Perpendicular() * 4f;
                float rotation = zipMover.percent * (float) Math.PI * 2f;

                Color color = pressed ? ropeColorPressed : ropeColor;
                Color lightColor = pressed ? ropeLightColorPressed : ropeLightColor;
                Draw.Line(from + value + offset, to + value + offset, colorOverride ?? color);
                Draw.Line(from + value2 + offset, to + value2 + offset, colorOverride ?? color);
                for (float num = 4f - zipMover.percent * (float) Math.PI * 8f % 4f; num < (to - from).Length(); num += 4f) {
                    Vector2 value3 = from + value + vector.Perpendicular() + vector * num;
                    Vector2 value4 = to + value2 - vector * num;
                    Draw.Line(value3 + offset, value3 + vector * 2f + offset, colorOverride ?? lightColor);
                    Draw.Line(value4 + offset, value4 - vector * 2f + offset, colorOverride ?? lightColor);
                }
                MTexture cogTex = colorOverride.HasValue ? cogWhite : pressed ? cogPressed : cog;
                cogTex.DrawCentered(from + offset, colorOverride ?? blockColor, 1f, rotation);
                cogTex.DrawCentered(to + offset, colorOverride ?? blockColor, 1f, rotation);
            }
        }

        private PathRenderer pathRenderer;

        private Vector2 start;
        private float percent = 0f;

        private SoundSource sfx = new SoundSource();
        private SoundSource altSfx = new SoundSource();

        private Vector2[] targets, points, originalNodes;
        private bool permanent;
        private bool waits;
        private bool ticking;
        private bool noReturn;

        public CassetteZipMover(Vector2 position, EntityID id, int width, int height, Vector2[] targets, int index, float tempo, bool noReturn, bool perm, bool waits, bool ticking, Color? overrideColor)
            : base(position, id, width, height, index, tempo, false, overrideColor) {
            start = Position;
            this.noReturn = noReturn;
            permanent = perm;
            this.waits = waits;
            this.ticking = ticking;

            this.targets = new Vector2[targets.Length];
            points = new Vector2[targets.Length + 1];
            points[0] = Position;
            originalNodes = targets;

            Add(new Coroutine(Sequence()));

            sfx.Position = new Vector2(Width, Height) / 2f;
            Add(sfx);
            altSfx.Position = sfx.Position;
            Add(altSfx);
        }

        public CassetteZipMover(EntityData data, Vector2 offset, EntityID id)
            : this(data.Position + offset, id, data.Width, data.Height, data.Nodes, data.Int("index"), data.Float("tempo", 1f),
                  data.Bool("noReturn", false),
                  data.Bool("permanent"),
                  data.Bool("waiting"),
                  data.Bool("ticking"),
                  data.HexColorNullable("customColor")) {
        }

        public override void Awake(Scene scene) {
            Image cross = new Image(GFX.Game["objects/CommunalHelper/cassetteMoveBlock/x"]);
            Image crossPressed = new Image(GFX.Game["objects/CommunalHelper/cassetteMoveBlock/xPressed"]);

            base.Awake(scene);
            if (noReturn) {
                AddCenterSymbol(cross, crossPressed);
            }
        }

        public override void Added(Scene scene) {
            base.Added(scene);

            // Offset the points to their position, relative to the room's position.
            Rectangle bounds = SceneAs<Level>().Bounds;
            Vector2 levelOffset = new Vector2(bounds.Left, bounds.Top);
            for (int i = 0; i < originalNodes.Length; i++) {
                targets[i] = originalNodes[i] + levelOffset;
                points[i + 1] = targets[i];
            }

            scene.Add(pathRenderer = new PathRenderer(this));
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
            Position = position;
        }

        private void ScrapeParticlesCheck(Vector2 to) {
            if (!Scene.OnInterval(0.03f))
                return;

            bool movedV = to.Y != ExactPosition.Y;
            bool movedH = to.X != ExactPosition.X;
            if (movedV && !movedH) {
                int dir = Math.Sign(to.Y - ExactPosition.Y);
                Vector2 origin = (dir != 1) ? TopLeft : BottomLeft;
                int start = dir == 1 ? Math.Min((int) Height - 12, 20) : 4;
                int end = dir == -1 ? Math.Max(16, (int) Height - 16) : (int) Height;

                if (Scene.CollideCheck<Solid>(origin + new Vector2(-2f, dir * -2))) {
                    for (int i = start; i < end; i += 8) {
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, TopLeft + new Vector2(0f, i + dir * 2f), (dir == 1) ? (-(float) Math.PI / 4f) : ((float) Math.PI / 4f));
                    }
                }
                if (Scene.CollideCheck<Solid>(origin + new Vector2(Width + 2f, dir * -2))) {
                    for (int j = start; j < end; j += 8) {
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, TopRight + new Vector2(-1f, j + dir * 2f), (dir == 1) ? ((float) Math.PI * -3f / 4f) : ((float) Math.PI * 3f / 4f));
                    }
                }
            } else if (movedH && !movedV) {
                int dir = Math.Sign(to.X - ExactPosition.X);
                Vector2 origin = (dir != 1) ? TopLeft : TopRight;
                int start = dir == 1 ? Math.Min((int) Width - 12, 20) : 4;
                int end = dir == -1 ? Math.Max(16, (int) Width - 16) : (int) Width;

                if (Scene.CollideCheck<Solid>(origin + new Vector2(dir * -2, -2f))) {
                    for (int k = start; k < end; k += 8) {
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, TopLeft + new Vector2(k + dir * 2f, -1f), (dir == 1) ? ((float) Math.PI * 3f / 4f) : ((float) Math.PI / 4f));
                    }
                }
                if (Scene.CollideCheck<Solid>(origin + new Vector2(dir * -2, Height + 2f))) {
                    for (int l = start; l < end; l += 8) {
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, BottomLeft + new Vector2(l + dir * 2f, 0f), (dir == 1) ? ((float) Math.PI * -3f / 4f) : (-(float) Math.PI / 4f));
                    }
                }
            }
        }

        private IEnumerator Sequence() {
            // Infinite.
            Vector2 start = Position;
            while (true) {
                if (!HasPlayerRider()) {
                    yield return null;
                    continue;
                }

                Vector2 from = start;
                Vector2 to;
                float at2;

                // Player is riding.
                bool shouldCancel = false;
                int i;
                for (i = 0; i < targets.Length; i++) {
                    to = targets[i];

                    // Start shaking.
                    sfx.Play(CustomSFX.game_connectedZipMover_normal_zip_mover_start);
                    Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
                    StartShaking(0.1f);
                    yield return 0.1f;

                    // Start moving towards the target.
                    //streetlight.SetAnimationFrame(3);
                    StopPlayerRunIntoAnimation = false;
                    at2 = 0f;
                    while (at2 < 1f) {
                        yield return null;
                        at2 = Calc.Approach(at2, 1f, 2f * Engine.DeltaTime);
                        percent = Ease.SineIn(at2);
                        Vector2 vector = Vector2.Lerp(from, to, percent);
                        ScrapeParticlesCheck(to);
                        if (Scene.OnInterval(0.1f)) {
                            pathRenderer.CreateSparks();
                        }
                        MoveTo(vector);
                    }

                    bool last = (i == targets.Length - 1);

                    // Arrived, will wait for 0.5 secs.
                    StartShaking(0.2f);
                    //streetlight.SetAnimationFrame(((waits && !last) || (ticking && !last) || (permanent && last)) ? 1 : 2);
                    Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                    SceneAs<Level>().Shake();
                    StopPlayerRunIntoAnimation = true;
                    yield return 0.5f;

                    from = targets[i];

                    if (ticking && !last) {
                        float tickTime = 0.0f;
                        int tickNum = 0;
                        while (!HasPlayerRider() && tickNum < 5) {
                            yield return null;
                            //streetlight.SetAnimationFrame(1 - (int) Math.Round(tickTime));


                            tickTime = Calc.Approach(tickTime, 1f, Engine.DeltaTime);
                            if (tickTime >= 1.0f) {
                                tickTime = 0.0f;
                                tickNum++;
                                sfx.Play(CustomSFX.game_connectedZipMover_normal_zip_mover_tick);
                                StartShaking(0.1f);
                            }
                        }

                        if (tickNum == 5 && !HasPlayerRider()) {
                            shouldCancel = true;
                            break;
                        }
                    } else if (waits && !last) {
                        //streetlight.SetAnimationFrame(1);
                        while (!HasPlayerRider()) {
                            yield return null;
                        }
                    }
                }

                if (!permanent) {
                    if (noReturn) {
                        ReverseNodes(out Vector2 newStart);
                        start = this.start = newStart;
                    } else {
                        for (i -= 2 - (shouldCancel ? 1 : 0); i > -2; i--) {
                            to = (i == -1) ? start : targets[i];

                            // Goes back to start with a speed that is four times slower.
                            StopPlayerRunIntoAnimation = false;
                            //streetlight.SetAnimationFrame(2);
                            sfx.Play(CustomSFX.game_connectedZipMover_normal_zip_mover_return);
                            at2 = 0f;
                            while (at2 < 1f) {
                                yield return null;
                                at2 = Calc.Approach(at2, 1f, 0.5f * Engine.DeltaTime);
                                percent = 1f - Ease.SineIn(at2);
                                Vector2 position = Vector2.Lerp(from, to, Ease.SineIn(at2));
                                MoveTo(position);
                            }
                            if (i != -1) {
                                from = targets[i];
                            }

                            StartShaking(0.2f);
                            altSfx.Play(CustomSFX.game_connectedZipMover_normal_zip_mover_finish);
                        }
                    }
                    StopPlayerRunIntoAnimation = true;

                    // Done, will not actiavte for 0.5 secs.
                    //streetlight.SetAnimationFrame(1);
                    yield return 0.5f;
                } else {

                    // Done, will never be activated again.
                    StartShaking(0.3f);
                    altSfx.Play(CustomSFX.game_connectedZipMover_normal_zip_mover_finish);
                    sfx.Play(CustomSFX.game_connectedZipMover_normal_zip_mover_tick);
                    SceneAs<Level>().Shake(0.15f);
                    //streetlight.SetAnimationFrame(0);
                    while (true) {
                        yield return null;
                    }
                }
            }
        }

        private void ReverseNodes(out Vector2 newStart) {
            Array.Reverse(points);
            for (int i = 0; i < points.Length - 1; i++) {
                targets[i] = points[i + 1];
            }
            newStart = points[0];
        }
    }
}
