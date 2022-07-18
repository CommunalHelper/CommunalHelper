using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CommunalHelper.Entities.Berries {
    [CustomEntity("CommunalHelper/RedlessBerry")]
    [RegisterStrawberry(tracked: false, blocksCollection: false)]
    public class RedlessBerry : Entity, IStrawberry {
        private static readonly Color PulseColorA = Calc.HexToColor("FDBF47");
        private static readonly Color PulseColorB = Calc.HexToColor("FE9130");

        private readonly EntityID id;

        private float warnLerp = 1f;
        private float warnLerpTarget = 1f;

        private bool collected;

        private Follower follower;

        private Sprite fruit, overlay;
        private Wiggler wiggler, rotateWiggler, shakeWiggler;
        private BloomPoint bloom;
        private VertexLight light;
        private Tween lightTween;

        public RedlessBerry(EntityData data, Vector2 offset, EntityID id)
            : base(data.Position + offset) {
            Depth = Depths.Pickups;
            Collider = new Hitbox(14f, 14f, -7f, -7f);

            this.id = id;

            Add(new PlayerCollider(OnPlayer));
            Add(follower = new(id) {
                FollowDelay = .3f
            });
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
        }

        private void OnAnimate(string _) {
            if (fruit.CurrentAnimationFrame == 35)
                OnPulse();
        }

        private void OnPulse() {
            lightTween.Start();
            shakeWiggler.Start();
            Audio.Play(SFX.game_gen_strawberry_pulse, Position);
            (Scene as Level).Displacement.AddBurst(Position, .6f, 4f, 28f, .1f);
        }

        private void OnPlayer(Player player) {
            if (follower.Leader is not null || collected)
                return;

            Audio.Play(SFX.game_gen_strawberry_touch, Position);
            player.Leader.GainFollower(follower);
            wiggler.Start();
            Depth = Depths.Top;
        }

        public void OnCollect() {
            if (collected)
                return;
            collected = true;
        }

        public override void Update() {
            base.Update();

            if (follower.Leader?.Entity is Player player)
                warnLerpTarget = Calc.ClampedMap(player.Stamina, 20f, Player.ClimbMaxStamina);

            warnLerp = Calc.Approach(warnLerp, warnLerpTarget, Engine.DeltaTime * 2f);

            Color safeColor = Color.Lerp(PulseColorA, PulseColorB, Calc.SineMap(Scene.TimeActive * 16f, 0f, 1f));
            Color warnColor = Color.Lerp(PulseColorA, Color.Red, Calc.SineMap(Scene.TimeActive * 60f, 0f, 1f));
            fruit.Color = Color.Lerp(warnColor, safeColor, Ease.CubeOut(warnLerp));
        }
    }
}
