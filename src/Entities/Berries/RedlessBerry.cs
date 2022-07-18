using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CommunalHelper.Entities.Berries {
    [CustomEntity("CommunalHelper/RedlessBerry")]
    [RegisterStrawberry(tracked: false, blocksCollection: false)]
    public class RedlessBerry : Entity, IStrawberry {
        private static readonly Color PulseColorA = Calc.HexToColor("FDBF47");
        private static readonly Color PulseColorB = Calc.HexToColor("FE9130");
        private static readonly Color BrokenColorA = Calc.HexToColor("E4242D");
        private static readonly Color BrokenColorB = Calc.HexToColor("252B42");
        private static readonly Color WarnColor = Color.Red;

        private readonly EntityID id;

        private Vector2 start;

        private float safeLerp = 1f, safeLerpTarget = 1f;
        private float brokenLerp = 0f;
        private float shaking;
        private Vector2 offset;

        private bool broken;
        private bool collected;

        private Follower follower;

        private Sprite fruit, overlay;
        private Wiggler wiggler, rotateWiggler, shakeWiggler;
        private BloomPoint bloom;
        private VertexLight light;
        private Tween lightTween;

        private readonly SoundSource sfx = new(CustomSFX.game_berries_redless_warning);

        public RedlessBerry(EntityData data, Vector2 offset, EntityID id)
            : base(data.Position + offset) {
            Depth = Depths.Pickups;
            Collider = new Hitbox(14f, 14f, -7f, -7f);

            this.id = id;

            Add(new PlayerCollider(OnPlayer));
            Add(follower = new(id) {
                FollowDelay = .3f
            });

            Add(sfx);

            start = Position;
        }

        public override void Added(Scene scene) {
            base.Added(scene);

            // TODO: alternative sprite when SaveData.Instance.CheckStrawberry(ID) returns true
            fruit = CommunalHelperModule.SpriteBank.Create("recolorableStrawberryFruit");
            overlay = CommunalHelperModule.SpriteBank.Create("recolorableStrawberryOverlay");
            Add(fruit, overlay);

            fruit.OnFrameChange = OnAnimate;

            Add(wiggler = Wiggler.Create(.4f, 4f, v => {
                fruit.Scale = overlay.Scale = Vector2.One * (1f + v * 0.35f);
            }));

            Add(rotateWiggler = Wiggler.Create(.5f, 4f, v => {
                fruit.Rotation = overlay.Rotation = v * 30f * (MathHelper.Pi / 180f);
            }));

            Add(shakeWiggler = Wiggler.Create(.8f, 2f, v => {
                fruit.Position.Y = overlay.Position.Y = v * 2f;
            }));

            // TODO: alpha must be .5f if berry is a ghost
            Add(bloom = new BloomPoint(1f, 12f));
            if ((scene as Level).Session.BloomBaseAdd > .1f)
                bloom.Alpha *= 0.5f;
            
            Add(light = new VertexLight(Color.White, 1f, 16, 24));
            Add(lightTween = light.CreatePulseTween());

            UpdateColor();
        }

        private void OnAnimate(string _) {
            if (fruit.CurrentAnimationFrame == 35) {
                lightTween.Start();
                shakeWiggler.Start();
                Audio.Play(SFX.game_gen_strawberry_pulse, Position);
                (Scene as Level).Displacement.AddBurst(Position, .6f, 4f, 28f, .1f);
            }
        }

        private void OnPlayer(Player player) {
            if (follower.Leader is not null || collected)
                return;

            Collidable = false;
            Audio.Play(SFX.game_gen_strawberry_touch, Position);
            player.Leader.GainFollower(follower);
            wiggler.Start();
            Depth = Depths.Top;
        }

        private void Reset() {
            Depth = Depths.Pickups;
            fruit.Play("idle", restart: true);
            overlay.Play("idle", restart: true);
            broken = false;
            Collidable = true;
            sfx.Play(CustomSFX.game_berries_redless_warning);
        }

        private void Detach() {
            if (collected)
                return;

            fruit.Play("idleBroken", restart: true);
            fruit.SetAnimationFrame(35);
            overlay.Play("idle", restart: true);
            overlay.SetAnimationFrame(35);

            sfx.Play(CustomSFX.game_berries_redless_break);
            (Scene as Level).Displacement.AddBurst(Position, .6f, 4f, 28f, .1f);

            shakeWiggler.Start();
            rotateWiggler.Start();

            follower.Leader.LoseFollower(follower);
            safeLerpTarget = brokenLerp = 1f;
            shaking = 1f;
            broken = true;

            Alarm.Set(this, .45f, () => {
                Vector2 difference = (start - Position).SafeNormalize();
                float distance = Vector2.Distance(Position, start);
                float scaleFactor = Calc.ClampedMap(distance, 16f, 120f, 16f, 96f);

                Vector2 control = start + difference * 16f + difference.Perpendicular() * scaleFactor * Calc.Random.Choose(1, -1);
                SimpleCurve curve = new SimpleCurve(Position, start, control);

                Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.SineOut, MathHelper.Max(distance / 100f, .4f), start: true);
                tween.OnUpdate = tween => {
                    Position = curve.GetPoint(tween.Eased);
                };
                tween.OnComplete = _ => Reset();
                Add(tween);
            });

        }

        public void OnCollect() {
            if (collected)
                return;
            collected = true;
        }

        private void UpdateColor() {
            Color safeColor = Color.Lerp(PulseColorA, PulseColorB, Calc.SineMap(Scene.TimeActive * 16f, 0f, 1f));
            Color warnColor = Color.Lerp(PulseColorA, WarnColor, Calc.SineMap(Scene.TimeActive * 60f, 0f, 1f));
            Color result = Color.Lerp(warnColor, safeColor, Ease.CubeOut(safeLerp));
            if (brokenLerp > 0f) {
                Color brokenColor = Color.Lerp(BrokenColorA, BrokenColorB, Ease.CubeOut(Calc.SineMap(Scene.TimeActive * 12f, 0f, 1f)));
                result = Color.Lerp(result, brokenColor, brokenLerp);
            }

            fruit.Color = result;
        }

        public override void Update() {
            base.Update();

            if (follower.Leader?.Entity is Player player) {
                safeLerpTarget = Calc.ClampedMap(player.Stamina, 20f, Player.ClimbMaxStamina);
                if (player.Stamina < Player.ClimbTiredThreshold)
                    Detach();
            }

            if (!broken)
                sfx.Param("intensity", 1 - safeLerp);

            safeLerp = Calc.Approach(safeLerp, safeLerpTarget, Engine.DeltaTime * 2f);
            brokenLerp = Calc.Approach(brokenLerp, broken ? 1f : 0f, Engine.DeltaTime * 2f);
            shaking = Calc.Approach(shaking, 0f, Engine.DeltaTime * 3.125f);
            offset = shaking > 0f ? Calc.Random.ShakeVector() : Vector2.Zero;

            UpdateColor();
        }

        public override void Render() {
            Vector2 original = Position;
            Position += offset;
            base.Render();
            Position = original;
        }
    }
}
