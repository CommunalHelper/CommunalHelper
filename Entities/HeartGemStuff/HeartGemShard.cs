using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Entities {
    [Tracked]
    public class HeartGemShard : Entity {

        public const string HeartGem_HeartGemPieces = "communalHelperGemPieces";

        public static ParticleType P_LightBeam;

        public HeartGem Heart;
        public bool Collected;

        protected DynData<HeartGem> heartData;

        private int index;

        private Image sprite;
        private Image outline;
        private HoldableCollider holdableCollider;

        private bool merging;

        private ParticleType shineParticle;
        private VertexLight light;
        private Tween lightTween;

        private Wiggler ScaleWiggler;
        private Wiggler moveWiggler;
        private Vector2 moveWiggleDir;
        private Shaker shaker;
        private float timer;
        private float bounceSfxDelay;

        private SoundSource collectSfx;

        public static void InitializeParticles() {
            P_LightBeam = new ParticleType {
                Source = GFX.Game["particles/shard"],
                Size = 0.5f,
                Color = new Color(0.8f, 1f, 1f),

                FadeMode = ParticleType.FadeModes.Late,
                LifeMin = 0.3f,
                LifeMax = 0.5f,

                SizeRange = 0.4f,
                SpeedMin = 40f,
                SpeedMax = 60f,
                SpeedMultiplier = 0.2f,
                Direction = Calc.QuarterCircle,
                DirectionRange = Calc.EighthCircle,

                RotationMode = ParticleType.RotationModes.SameAsDirection,
            };
        }

        public HeartGemShard(HeartGem heart, Vector2 position, int index)
            : base(position) {
            Heart = heart;
            heartData = new DynData<HeartGem>(Heart);

            this.index = index; 

            Depth = Depths.Pickups;

            Collider = new Hitbox(12f, 12f, -6f, -6f);
            Add(holdableCollider = new HoldableCollider(OnHoldable));
            Add(new PlayerCollider(OnPlayer));

            moveWiggler = Wiggler.Create(0.8f, 2f);
            moveWiggler.StartZero = true;
            Add(moveWiggler);
            Add(collectSfx = new SoundSource());

            Add(shaker = new Shaker(false));
            shaker.Interval = 0.1f;

            Add(sprite = new Image(GFX.Game.GetAtlasSubtexturesAt("collectables/CommunalHelper/heartGemShard/shard", index % 3)).CenterOrigin());
            Add(outline = new Image(GFX.Game.GetAtlasSubtexturesAt("collectables/CommunalHelper/heartGemShard/shard_outline", index % 3)).CenterOrigin());
            Add(ScaleWiggler = Wiggler.Create(0.5f, 4f, f => sprite.Scale = Vector2.One * (1f + f * 0.25f)));

            Add(new BloomPoint(Heart.IsFake ? 0f : 0.75f, 16f));
            Add(new MirrorReflection());
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            Color color = Heart.IsGhost ? new Color(130, 144, 198) : Heart.Get<VertexLight>().Color;
            sprite.Color = color;

            shineParticle = heartData.Get<ParticleType>("shineParticle");

            Add(light = new VertexLight(color, 1f, 32, 64));
            Add(lightTween = light.CreatePulseTween());
        }

        public void Collect(Player player, Level level) {
            Collected = true;
            Collidable = false;
            Depth = Depths.NPCs;
            sprite.Color = Color.White;
            shaker.On = true;

            bool allCollected = true;
            foreach (HeartGemShard piece in heartData.Get<List<HeartGemShard>>(HeartGem_HeartGemPieces))
                if (!piece.Collected)
                    allCollected = false;


            collectSfx.Play(CustomSFX.game_seedCrystalHeart_shard_collect, "shatter", allCollected ? 0f : 1f);
            Celeste.Freeze(.1f);

            level.Shake(.15f);
            level.Flash(Color.White * .25f);
            if (allCollected)
                Scene.Add(new CSGEN_HeartGemShards(Heart));
        }

        public void OnAllCollected() {
            Depth = Depths.Pickups;
            Tag = Tags.FrozenUpdate;
            base.Depth = -2000002;
            merging = true;
        }

        public void OnPlayer(Player player) {
            Level level = (Scene as Level);
            if (!Collected && !level.Frozen) {
                if (player.DashAttacking) {
                    Collect(player, level);
                    return;
                }
                if (bounceSfxDelay <= 0f) {
                    if (Heart.IsFake) {
                        Audio.Play(SFX.game_10_fakeheart_bounce, Position);
                    } else {
                        Audio.Play(SFX.game_gen_crystalheart_bounce, Position);
                    }
                    bounceSfxDelay = 0.1f;
                }
                player.PointBounce(Center, 110f);
                ScaleWiggler.Start();
                moveWiggler.Start();
                moveWiggleDir = (Center - player.Center).SafeNormalize(Vector2.UnitY);
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                
            }
        }

        public void OnHoldable(Holdable holdable) {
            Player player = Scene.Tracker.GetEntity<Player>();
            if (!Collected && player != null && holdable.Dangerous(holdableCollider)) {
                Collect(player, Scene as Level);
            }
        }

        public override void Update() {
            bounceSfxDelay -= Engine.DeltaTime;
            timer += Engine.DeltaTime;
            sprite.Position = Vector2.UnitY * (!Collected ? (float) Math.Sin(timer * 2f) * 2f : 0);
            sprite.Position += moveWiggleDir * moveWiggler.Value * -4f;
            sprite.Position += shaker.Value;

            outline.Position = sprite.Position;
            outline.Scale = sprite.Scale;

            base.Update();

            Level level = SceneAs<Level>();
            if (!Collected) { 
                if (Scene.OnInterval(0.1f))
                    level.Particles.Emit(shineParticle, 1, Center, Vector2.One * 4f);

                if (Scene.OnInterval(3f)) {
                    Audio.Play(SFX.game_gen_seed_pulse, Center, "count", index);
                    lightTween.Start();
                    level.Displacement.AddBurst(Center + shaker.Value, 0.6f, 8f, 20f, 0.2f);
                }
            }

            if (Collected && !merging && Scene.OnInterval(Calc.Random.Range(0.5f, 0.8f))) {
                level.Particles.Emit(P_LightBeam, 4, Center + shaker.Value, Vector2.One, Calc.Random.NextAngle());
            }
        }

        public void StartSpinAnimation(Vector2 averagePos, Vector2 centerPos, float angleOffset, float time, bool regular) {
            shaker.On = false;
            float spinLerp = 0f;
            Vector2 start = Position;
            Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeIn, time / 2f, start: true);
            tween.OnUpdate = delegate (Tween t)
            {
                spinLerp = t.Eased;
            };
            Add(tween);
            tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeInOut, time, start: true);
            tween.OnUpdate = delegate (Tween t)
            {
                float angleRadians = (float) Math.PI / 2f + angleOffset - MathHelper.Lerp(0f, 32.2013245f, t.Eased);
                Vector2 value = Vector2.Lerp(averagePos, centerPos, spinLerp) + Calc.AngleToVector(angleRadians, regular ? 30f : MathHelper.Lerp(30f, 5f, t.Eased));
                Position = Vector2.Lerp(start, value, spinLerp);
            };
            Add(tween);
        }

        public void StartCombineAnimation(Vector2 centerPos, float time, ParticleSystem particleSystem, Level level, bool spin) {
            collectSfx.Stop(allowFadeout: false);
            Vector2 position = Position;
            float startAngle = Calc.Angle(centerPos, position);
            Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.BigBackIn, time, start: true);
            tween.OnUpdate = delegate (Tween t) {
                float tEased = t.Eased;
                Vector2 oldPos = Center;
                float angleRadians = spin ? MathHelper.Lerp(startAngle, startAngle - (float) Math.PI * 2f, Ease.CubeIn(t.Percent)) : startAngle;
                float length = MathHelper.Lerp(spin ? 30f : 5f + 45f * t.Percent, 0f, t.Eased);
                Position = centerPos + Calc.AngleToVector(angleRadians, length);

                if (level.OnInterval(.03f))
                    particleSystem.Emit(StrawberrySeed.P_Burst, 1, Center, Vector2.One, (Center - oldPos).Angle());

                if (t.Percent > 0.5f) {
                    level.Shake((t.Percent - .5f) * .5f);
                }
            };
            tween.OnComplete = delegate
            {
                Visible = false;
                for (int i = 0; i < 6; i++) {
                    float num = Calc.Random.NextFloat((float) Math.PI * 2f);
                    particleSystem.Emit(StrawberrySeed.P_Burst, 1, Position + Calc.AngleToVector(num, 4f), Vector2.Zero, num);
                }
                RemoveSelf();
            };
            Add(tween);
        }

        #region HeartGem Extensions

        public static void CollectedPieces(DynData<HeartGem> heartData) {
            heartData.Target.Visible = true;
            heartData.Target.Active = true;
            heartData.Target.Collidable = true;
            heartData.Get<BloomPoint>("bloom").Visible = heartData.Get<VertexLight>("light").Visible = true;
        }

        public static void CollectedPieces(HeartGem heartGem) {
            CollectedPieces(new DynData<HeartGem>(heartGem));
        }

        #endregion

        #region Hooks

        public static void Load() {
            On.Celeste.HeartGem.ctor_EntityData_Vector2 += HeartGem_ctor_EntityData_Vector2;
            On.Celeste.HeartGem.Awake += HeartGem_Awake;
        }

        public static void Unload() {
            On.Celeste.HeartGem.ctor_EntityData_Vector2 -= HeartGem_ctor_EntityData_Vector2;
            On.Celeste.HeartGem.Awake -= HeartGem_Awake;
        }

        private static void HeartGem_ctor_EntityData_Vector2(On.Celeste.HeartGem.orig_ctor_EntityData_Vector2 orig, HeartGem self, EntityData data, Vector2 offset) {
            orig(self, data, offset);
            DynData<HeartGem> gemData = new DynData<HeartGem>(self);
            if (data.Nodes != null && data.Nodes.Length != 0) {
                List<HeartGemShard> pieces = new List<HeartGemShard>();
                for (int i = 0; i < data.Nodes.Length; i++) {
                    pieces.Add(new HeartGemShard(self, offset + data.Nodes[i], i));
                }
                gemData[HeartGem_HeartGemPieces] = pieces;
            } else
                gemData[HeartGem_HeartGemPieces] = null;

        }

        private static void HeartGem_Awake(On.Celeste.HeartGem.orig_Awake orig, HeartGem self, Scene scene) {
            orig(self, scene);

            DynData<HeartGem> gemData = new DynData<HeartGem>(self);

            List<HeartGemShard> pieces = gemData.Get<List<HeartGemShard>>(HeartGem_HeartGemPieces);
            if (pieces != null && pieces.Count > 0) {
                foreach (HeartGemShard piece in pieces) {
                    scene.Add(piece);
                }
                self.Visible = false;
                self.Active = false;
                self.Collidable = false;
                gemData.Get<BloomPoint>("bloom").Visible = gemData.Get<VertexLight>("light").Visible = false;
            }

        }

        #endregion

    }

}
