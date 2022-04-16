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

            private ParticleType sparkParticle;
            private ParticleType sparkParticlePressed;

            public PathRenderer(CassetteZipMover zipMover) {
                Depth = Depths.BGDecals;
                this.zipMover = zipMover;

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
                ParticleType particle = zipMover.Collidable ? sparkParticle : sparkParticlePressed;
                ParticleSystem particlesBG = SceneAs<Level>().ParticlesBG;

                // First Node
                Vector2 node = GetNodeFrom(zipMover.start, true);
                Vector2 next = GetNodeFrom(zipMover.nodes[1], true);

                float angle = Calc.Angle(node, next);
                float sparkDir = angle + Calc.QuarterCircle;
                Vector2 sparkAdd = Calc.AngleToVector(sparkDir, 5f);

                particlesBG.Emit(particle, node + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDir + Calc.EighthCircle);
                particlesBG.Emit(particle, node - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDir + Calc.HalfCircle - Calc.EighthCircle);

                // Mid Nodes
                for (int i = 2; i < zipMover.nodes.Length; i++) {
                    node = next;
                    next = GetNodeFrom(zipMover.nodes[i], true);

                    // Half-way angle between previous and next nodes
                    float lastAngle = angle;
                    angle = Calc.Angle(node, next);
                    sparkDir = Calc.AngleLerp(lastAngle, angle, 0.5f) - Calc.QuarterCircle;
                    sparkAdd = Calc.AngleToVector(sparkDir, 5f);

                    particlesBG.Emit(particle, node + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDir);
                    particlesBG.Emit(particle, node - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDir + Calc.HalfCircle);
                }

                // Last Node
                node = next;

                sparkDir = angle - Calc.QuarterCircle;
                sparkAdd = Calc.AngleToVector(sparkDir, 5f);

                particlesBG.Emit(particle, node + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDir + Calc.EighthCircle);
                particlesBG.Emit(particle, node - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDir + Calc.HalfCircle - Calc.EighthCircle);
            }

            private Vector2 GetNodeFrom(Vector2 node, bool offsetBlockHeight = false) {
                Vector2 ret = node + new Vector2(zipMover.Width / 2f, zipMover.Height / 2f);
                if (offsetBlockHeight) {
                    ret += zipMover.blockOffset;
                }
                return ret;
            }

            public override void Update() {
                base.Update();
                Depth = zipMover.Collidable ? Depths.BGDecals : Depths.BGDecals + 10;
            }

            public override void Render() {
                int blockHeight = zipMover.blockHeight;
                // Draw the "drop shadow" when active
                if (blockHeight != 0) {
                    from = GetNodeFrom(zipMover.start);
                    for (int j = 1; j < zipMover.nodes.Length; j++) {
                        for (int i = 1; i <= blockHeight; ++i) {
                            to = GetNodeFrom(zipMover.nodes[j]);
                            DrawCogs(Vector2.UnitY * i, undersideColor);
                            from = to;
                        }
                    }
                }

                from = GetNodeFrom(zipMover.start);
                for (int j = 1; j < zipMover.nodes.Length; j++) {
                    to = GetNodeFrom(zipMover.nodes[j]);
                    DrawCogs(zipMover.blockOffset);
                    from = to;
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

        /// <summary>
        /// Entity nodes with start Position as the first element
        /// </summary>
        protected Vector2[] nodes;

        private bool permanent;
        private bool waits;
        private bool ticking;
        private bool noReturn;

        public CassetteZipMover(Vector2 position, EntityID id, int width, int height, Vector2[] nodes, int index, float tempo, bool noReturn, bool perm, bool waits, bool ticking, Color? overrideColor)
            : base(position, id, width, height, index, tempo, false, overrideColor) {
            start = Position;
            this.noReturn = noReturn;
            permanent = perm;
            this.waits = waits;
            this.ticking = ticking;

            this.nodes = nodes;

            Add(new Coroutine(Sequence()));

            sfx.Position = new Vector2(Width, Height) / 2f;
            Add(sfx);
        }

        public CassetteZipMover(EntityData data, Vector2 offset, EntityID id)
            : this(data.Position + offset, id, data.Width, data.Height, data.NodesWithPosition(offset), data.Int("index"), data.Float("tempo", 1f),
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
                // Zeroth node is the origin
                for (i = 1; i < nodes.Length; i++) {
                    to = nodes[i];

                    // Start shaking.
                    sfx.Play(CustomSFX.game_zipMover_normal_start);
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
                        vector = FixCassetteY(vector);
                        ScrapeParticlesCheck(to);
                        if (Scene.OnInterval(0.1f)) {
                            pathRenderer.CreateSparks();
                        }
                        MoveTo(vector);
                    }

                    bool last = (i == nodes.Length - 1);

                    // Arrived, will wait for 0.5 secs.
                    StartShaking(0.2f);
                    Audio.Play(CustomSFX.game_zipMover_normal_impact, Center);
                    //streetlight.SetAnimationFrame(((waits && !last) || (ticking && !last) || (permanent && last)) ? 1 : 2);
                    Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                    SceneAs<Level>().Shake();
                    StopPlayerRunIntoAnimation = true;
                    yield return 0.5f;

                    from = nodes[i];

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
                                sfx.Play(CustomSFX.game_zipMover_normal_tick);
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
                        start = newStart;
                    } else {
                        for (i -= 2 - (shouldCancel ? 1 : 0); i > -1; i--) {

                            to = (i == 0) ? start : nodes[i];

                            // Goes back to start with a speed that is four times slower.
                            StopPlayerRunIntoAnimation = false;
                            //streetlight.SetAnimationFrame(2);
                            sfx.Play(CustomSFX.game_zipMover_normal_return);
                            at2 = 0f;
                            while (at2 < 1f) {
                                yield return null;
                                at2 = Calc.Approach(at2, 1f, 0.5f * Engine.DeltaTime);
                                percent = 1f - Ease.SineIn(at2);
                                Vector2 position = Vector2.Lerp(from, to, Ease.SineIn(at2));
                                position = FixCassetteY(position);
                                MoveTo(position);
                            }
                            if (i != 0) {
                                from = nodes[i];
                            }

                            StartShaking(0.2f);
                            Audio.Play(CustomSFX.game_zipMover_normal_finish, Center);
                        }
                    }
                    StopPlayerRunIntoAnimation = true;

                    // Done, will not actiavte for 0.5 secs.
                    //streetlight.SetAnimationFrame(1);
                    yield return 0.5f;
                } else {

                    // Done, will never be activated again.
                    StartShaking(0.3f);
                    Audio.Play(CustomSFX.game_zipMover_normal_finish, Center);
                    Audio.Play(CustomSFX.game_zipMover_normal_tick, Center);
                    SceneAs<Level>().Shake(0.15f);
                    //streetlight.SetAnimationFrame(0);
                    while (true) {
                        yield return null;
                    }
                }
            }
        }

        private Vector2 FixCassetteY(Vector2 vec) {
            return vec + blockOffset;
        }

        private void ReverseNodes(out Vector2 newStart) {
            Array.Reverse(nodes);
            newStart = nodes[0];
        }
    }
}
