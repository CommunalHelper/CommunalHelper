using Celeste.Mod.CommunalHelper.Entities;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;

namespace Celeste.Mod.CommunalHelper {

    [CustomEntity("CommunalHelper/DreamSwitchGate")]
    class DreamSwitchGate : CustomDreamBlock {
        private static readonly Color inactiveColor = Calc.HexToColor("5fcde4");
        private static readonly Color activeColor = Color.White;
        private static readonly Color finishColor = Calc.HexToColor("f141df");
        private static ParticleType[] P_BehindDreamParticles;

        private bool permanent;

        private Sprite icon;
        private Wiggler wiggler;

        private Vector2 node;

        private SoundSource openSfx;

        public DreamSwitchGate(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Width, data.Height, data.Nodes[0] + offset, data.Bool("oneUse"), data.Bool("featherMode"), GetRefillCount(data), data.Bool("below"), data.Bool("permanent")) { }

        public DreamSwitchGate(Vector2 position, int width, int height, Vector2 node, bool oneUse, bool featherMode, int refillCount, bool below, bool permanent)
            : base(position, width, height, featherMode, oneUse, refillCount, below) {

            this.permanent = permanent;
            this.node = node;
            icon = new Sprite(GFX.Game, "objects/switchgate/icon");
            icon.Add("spin", "", 0.1f, "spin");
            icon.Play("spin");
            icon.Rate = 0f;
            icon.Color = inactiveColor;
            icon.CenterOrigin();
            Add(wiggler = Wiggler.Create(0.5f, 4f, scale => {
                icon.Scale = Vector2.One * (1f + scale);
            }));

            Add(openSfx = new SoundSource());
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            if (Switch.CheckLevelFlag(SceneAs<Level>())) {
                MoveTo(node);
                icon.Rate = 0f;
                icon.SetAnimationFrame(0);
                icon.Color = finishColor;
            } else {
                Add(new Coroutine(Sequence(node)));
            }
        }

        public override void Render() {
            Vector2 position = Position;
            Position += Shake;
            base.Render();
            icon.Position = Center;
            icon.DrawOutline();
            icon.Render();

            // Redraw whiteFill over icon
            float whiteFill = baseData.Get<float>("whiteFill");
            if (whiteFill > 0)
                Draw.Rect(Position, Width, Height * baseData.Get<float>("whiteHeight"), Color.White * whiteFill);

            Position = position;
        }

        private IEnumerator Sequence(Vector2 node) {
            this.node = node;

            Vector2 start = Position;
            while (!Switch.Check(Scene)) {
                yield return null;
            }

            if (permanent) {
                Switch.SetLevelFlag(SceneAs<Level>());
            }
            yield return 0.1f;

            openSfx.Play(SFX.game_gen_touchswitch_gate_open);
            StartShaking(0.5f);
            while (icon.Rate < 1f) {
                icon.Color = Color.Lerp(inactiveColor, activeColor, icon.Rate);
                icon.Rate += Engine.DeltaTime * 2f;
                yield return null;
            }

            yield return 0.1f;


            int particleAt = 0;
            Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeOut, 2f, start: true);
            tween.OnUpdate = t => {
                MoveTo(Vector2.Lerp(start, node, t.Eased));
                if (Scene.OnInterval(0.1f)) {
                    particleAt++;
                    particleAt %= 2;
                    for (int n = 0; n < Width / 8f; n++) {
                        for (int num2 = 0; num2 < Height / 8f;
                        num2++) {
                            if ((n + num2) % 2 == particleAt) {
                                ParticleType pType = Calc.Random.Choose(P_BehindDreamParticles);
                                SceneAs<Level>().ParticlesBG.Emit(pType, Position + new Vector2(n * 8, num2 * 8) + Calc.Random.Range(Vector2.One * 2f, Vector2.One * 6f));
                            }
                        }
                    }
                }
            };
            Add(tween);
            yield return 1.8f;

            bool collidable = Collidable;
            Collidable = false;
            if (node.X <= start.X) {
                Vector2 value = new Vector2(0f, 2f);
                for (int i = 0; i < Height / 8f; i++) {
                    Vector2 vector = new Vector2(Left - 1f, Top + 4f + i * 8);
                    Vector2 point = vector + Vector2.UnitX;
                    if (Scene.CollideCheck<Solid>(vector) && !Scene.CollideCheck<Solid>(point)) {
                        SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, vector + value, (float) Math.PI);
                        SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, vector - value, (float) Math.PI);
                    }
                }
            }
            if (node.X >= start.X) {
                Vector2 value = new Vector2(0f, 2f);
                for (int j = 0; j < Height / 8f; j++) {
                    Vector2 vector = new Vector2(Right + 1f, Top + 4f + j * 8);
                    Vector2 point = vector - Vector2.UnitX * 2f;
                    if (Scene.CollideCheck<Solid>(vector) && !Scene.CollideCheck<Solid>(point)) {
                        SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, vector + value, 0f);
                        SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, vector - value, 0f);
                    }
                }
            }
            if (node.Y <= start.Y) {
                Vector2 value = new Vector2(2f, 0f);
                for (int k = 0; k < Width / 8f; k++) {
                    Vector2 vectpr = new Vector2(Left + 4f + k * 8, Top - 1f);
                    Vector2 point = vectpr + Vector2.UnitY;
                    if (Scene.CollideCheck<Solid>(vectpr) && !Scene.CollideCheck<Solid>(point)) {
                        SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, vectpr + value, -(float) Math.PI / 2f);
                        SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, vectpr - value, -(float) Math.PI / 2f);
                    }
                }
            }
            if (node.Y >= start.Y) {
                Vector2 value = new Vector2(2f, 0f);
                for (int l = 0; l < Width / 8f; l++) {
                    Vector2 vector = new Vector2(Left + 4f + l * 8, Bottom + 1f);
                    Vector2 point = vector - Vector2.UnitY * 2f;
                    if (Scene.CollideCheck<Solid>(vector) && !Scene.CollideCheck<Solid>(point)) {
                        SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, vector + value, (float) Math.PI / 2f);
                        SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, vector - value, (float) Math.PI / 2f);
                    }
                }
            }
            Collidable = collidable;
            Audio.Play(SFX.game_gen_touchswitch_gate_finish, Position);
            StartShaking(0.2f);
            while (icon.Rate > 0f) {
                icon.Color = Color.Lerp(activeColor, finishColor, 1f - icon.Rate);
                icon.Rate -= Engine.DeltaTime * 4f;
                yield return null;
            }

            icon.Rate = 0f;
            icon.SetAnimationFrame(0);
            wiggler.Start();
            bool collidable2 = Collidable;
            Collidable = false;
            if (!Scene.CollideCheck<Solid>(Center)) {
                for (int m = 0; m < 32; m++) {
                    float num = Calc.Random.NextFloat((float) Math.PI * 2f);
                    SceneAs<Level>().ParticlesFG.Emit(TouchSwitch.P_Fire, Center + Calc.AngleToVector(num, 4f), num);
                }
            }
            Collidable = collidable2;
        }

        public static void InitializeParticles() {
            P_BehindDreamParticles = new ParticleType[4];
            // Color Codes : FFEF11, FF00D0, 08a310, 5fcde4, 7fb25e, E0564C, 5b6ee1, CC3B3B

            ParticleType particle = new ParticleType(SwitchGate.P_Behind);
            particle.ColorMode = ParticleType.ColorModes.Choose;
            for (int i = 0; i < 4; i++) {
                P_BehindDreamParticles[i] = new ParticleType(particle);
            }
        }

        public override void SetupCustomParticles(float canvasWidth, float canvasHeight) {
            base.SetupCustomParticles(canvasWidth, canvasHeight);
            if (PlayerHasDreamDash) {
                P_BehindDreamParticles[0].Color = Calc.HexToColor("FFEF11");
                P_BehindDreamParticles[0].Color2 = Calc.HexToColor("FF00D0");

                P_BehindDreamParticles[1].Color = Calc.HexToColor("08a310");
                P_BehindDreamParticles[1].Color2 = Calc.HexToColor("5fcde4");

                P_BehindDreamParticles[2].Color = Calc.HexToColor("7fb25e");
                P_BehindDreamParticles[2].Color2 = Calc.HexToColor("E0564C");

                P_BehindDreamParticles[3].Color = Calc.HexToColor("5b6ee1");
                P_BehindDreamParticles[3].Color2 = Calc.HexToColor("CC3B3B");
            } else {
                for (int i = 0; i < 4; i++) {
                    P_BehindDreamParticles[i].Color = Color.LightGray * 0.5f;
                    P_BehindDreamParticles[i].Color2 = Color.LightGray * 0.75f;
                }
            }
        }
    }
}
