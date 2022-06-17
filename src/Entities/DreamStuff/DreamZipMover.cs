using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/DreamZipMover")]
    public class DreamZipMover : CustomDreamBlock, IMultiNodeZipMover {
        private ZipMoverPathRenderer pathRenderer;

        private const float impactSoundOffset = 0.92f;

        private Vector2 start;

        private SoundSource sfx;

        private Vector2[] nodes;
        private bool permanent;
        private bool waits;
        private bool ticking;
        private bool dreamAesthetic;
        private bool noReturn;
        private MTexture cross;

        private static readonly Color ropeColor = Calc.HexToColor("663931");
        private static readonly Color ropeLightColor = Calc.HexToColor("9b6157");

        public float Percent { get; set; }

        public DreamZipMover(EntityData data, Vector2 offset)
            : base(data, offset) {
            start = Position;
            nodes = data.NodesWithPosition(offset);

            noReturn = data.Bool("noReturn");
            dreamAesthetic = data.Bool("dreamAesthetic");

            permanent = data.Bool("permanent");
            waits = data.Bool("waiting");
            ticking = data.Bool("ticking");

            Add(new Coroutine(Sequence()));
            Add(new LightOcclude());
            Add(sfx = new SoundSource {
                Position = new Vector2(Width / 2f, Height / 2f)
            });
            cross = GFX.Game["objects/CommunalHelper/dreamMoveBlock/x"];
        }

        public override void Added(Scene scene) {
            base.Added(scene);

            MTexture cog = GFX.Game["objects/zipmover/cog"];
            scene.Add(pathRenderer = new ZipMoverPathRenderer(this, (int)Width, (int)Height, nodes, cog, ropeColor, ropeLightColor));
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
            if (!Scene.OnInterval(0.03f))
                return;

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
            // Infinite.
            while (true) {
                if (!HasPlayerRider()) {
                    yield return null;
                    continue;
                }

                Vector2 from = nodes[0];
                Vector2 to;
                float at2;

                // Player is riding.
                bool shouldCancel = false;
                int i;
                for (i = 1; i < nodes.Length; i++) {
                    to = nodes[i];

                    // Start shaking.
                    sfx.Play(CustomSFX.game_dreamZipMover_start);
                    Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
                    StartShaking(0.1f);
                    yield return 0.1f;

                    // Start moving towards the target.
                    //streetlight.SetAnimationFrame(3);
                    StopPlayerRunIntoAnimation = false;
                    at2 = 0f;
                    bool playedFinishSound = false;
                    while (at2 < 1f) {
                        yield return null;
                        at2 = Calc.Approach(at2, 1f, 2f * Engine.DeltaTime);
                        if (at2 > impactSoundOffset && !playedFinishSound) {
                            Audio.Play(CustomSFX.game_dreamZipMover_impact, Center);
                            playedFinishSound = true;
                        }
                        Percent = Ease.SineIn(at2);
                        Vector2 vector = Vector2.Lerp(from, to, Percent);
                        ScrapeParticlesCheck(to);
                        if (Scene.OnInterval(0.1f)) {
                            pathRenderer.CreateSparks();
                        }
                        MoveTo(vector);
                    }

                    bool last = (i == nodes.Length - 1);

                    // Arrived, will wait for 0.5 secs.
                    StartShaking(0.2f);
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
                                Audio.Play(CustomSFX.game_dreamZipMover_tick, Center);
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
                        ReverseNodes(/*out Vector2 newStart*/);
                        //start = this.start = newStart;
                    } else {

                        for (i -= 2 - (shouldCancel ? 1 : 0); i >= 0; i--) {
                            to = nodes[i];

                            // Goes back to start with a speed that is four times slower.
                            StopPlayerRunIntoAnimation = false;
                            //streetlight.SetAnimationFrame(2);
                            sfx.Play(CustomSFX.game_dreamZipMover_return);
                            at2 = 0f;
                            bool playedFinishSound = false;
                            while (at2 < 1f) {
                                yield return null;
                                at2 = Calc.Approach(at2, 1f, 0.5f * Engine.DeltaTime);
                                if (at2 > impactSoundOffset && !playedFinishSound) {
                                    playedFinishSound = true;
                                    Audio.Play(CustomSFX.game_dreamZipMover_finish, Center);
                                }
                                Percent = 1f - Ease.SineIn(at2);
                                Vector2 position = Vector2.Lerp(from, to, Ease.SineIn(at2));
                                MoveTo(position);
                            }
                            if (i != 0) {
                                from = nodes[i];
                            }

                            StartShaking(0.2f);
                        }
                    }
                    StopPlayerRunIntoAnimation = true;

                    // Done, will not actiavte for 0.5 secs.
                    //streetlight.SetAnimationFrame(1);
                    yield return 0.5f;
                } else {

                    // Done, will never be activated again.
                    StartShaking(0.3f);
                    Audio.Play(CustomSFX.game_dreamZipMover_tick, Center);
                    SceneAs<Level>().Shake(0.15f);
                    //streetlight.SetAnimationFrame(0);
                    while (true) {
                        yield return null;
                    }
                }
            }
        }

        private void ReverseNodes() {
            Array.Reverse(nodes);
        }

        protected override void OneUseDestroy() {
            base.OneUseDestroy();
            Scene.Remove(pathRenderer);
            pathRenderer = null;
            sfx.Stop();
        }

        public void DrawBorder() { }
    }
}
