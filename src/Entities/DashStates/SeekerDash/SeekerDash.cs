using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Entities {
    public static class SeekerDash {

        private static bool hasSeekerDash = false;
        public static bool HasSeekerDash {
            get { return hasSeekerDash || true;/* || CommunalHelperModule.Settings.AlwaysActiveDreamRefillCharge; */}
            set { hasSeekerDash = value; }
        }

        private static bool seekerDashAttacking;
        private static float seekerDashTimer;

        private static MethodInfo m_Seeker_GotBouncedOn = typeof(Seeker).GetMethod("GotBouncedOn", BindingFlags.NonPublic | BindingFlags.Instance);

        internal static void Load() {
            On.Celeste.Player.ctor += Player_ctor;
            On.Celeste.Player.DashBegin += Player_DashBegin;
            On.Celeste.Player.Update += Player_Update;
            On.Celeste.Player.OnCollideH += Player_OnCollideH;
            On.Celeste.Player.OnCollideV += Player_OnCollideV;
            On.Celeste.Player.GetCurrentTrailColor += Player_GetCurrentTrailColor;

            On.Celeste.DashBlock.OnDashed += DashBlock_OnDashed;
            On.Celeste.TempleCrackedBlock.ctor_EntityID_Vector2_float_float_bool += TempleCrackedBlock_ctor_EntityID_Vector2_float_float_bool;
            On.Celeste.SeekerBarrier.ctor_Vector2_float_float += SeekerBarrier_ctor_Vector2_float_float;

            On.Celeste.Seeker.OnBouncePlayer += Seeker_OnBouncePlayer;
            On.Celeste.Seeker.OnAttackPlayer += Seeker_OnAttackPlayer;
        }

        internal static void Unload() {
            On.Celeste.Player.ctor -= Player_ctor;
            On.Celeste.Player.DashBegin -= Player_DashBegin;
            On.Celeste.Player.Update -= Player_Update;
            On.Celeste.Player.OnCollideH -= Player_OnCollideH;
            On.Celeste.Player.OnCollideV -= Player_OnCollideV;
            On.Celeste.Player.GetCurrentTrailColor -= Player_GetCurrentTrailColor;

            On.Celeste.DashBlock.OnDashed -= DashBlock_OnDashed;
            On.Celeste.TempleCrackedBlock.ctor_EntityID_Vector2_float_float_bool -= TempleCrackedBlock_ctor_EntityID_Vector2_float_float_bool;
            On.Celeste.SeekerBarrier.ctor_Vector2_float_float -= SeekerBarrier_ctor_Vector2_float_float;

            On.Celeste.Seeker.OnBouncePlayer -= Seeker_OnBouncePlayer;
            On.Celeste.Seeker.OnAttackPlayer -= Seeker_OnAttackPlayer;
        }

        private static void Seeker_OnBouncePlayer(On.Celeste.Seeker.orig_OnBouncePlayer orig, Seeker self, Player player) {
            orig(self, player);
        }

        private static void Seeker_OnAttackPlayer(On.Celeste.Seeker.orig_OnAttackPlayer orig, Seeker self, Player player) {
            DynData<Seeker> seekerData = new DynData<Seeker>(self);
            int state = seekerData.Get<StateMachine>("State").State;
            if (seekerDashAttacking) {
                if (state != 4) {
                    player.Bounce(self.Top);
                    m_Seeker_GotBouncedOn.Invoke(self, new object[] { player });
                } else {
                    Entity entity = new Entity(self.Position);
                    DeathEffect deathEffect = new DeathEffect(Color.HotPink, new Vector2?(self.Center - self.Position));
                    deathEffect.OnEnd = delegate () {
                        entity.RemoveSelf();
                    };
                    entity.Add(deathEffect);
                    entity.Depth = Depths.Top;
                    self.Scene.Add(entity);
                    Audio.Play(SFX.game_05_seeker_death, self.Position);
                    self.RemoveSelf();
                    seekerData["dead"] = true;
                }
                return;
            }

            orig(self, player);
        }

        private static void Player_OnCollideH(On.Celeste.Player.orig_OnCollideH orig, Player self, CollisionData data) {
            orig(self, data);

            if (seekerDashAttacking && !(data.Hit is SeekerBarrier)) { // SeekerBarriers handled elsewhere
                float direction;
                float x;
                if (data.Direction.X > 0f) {
                    direction = Calc.HalfCircle;
                    x = self.Right;
                } else {
                    direction = 0f;
                    x = self.Left;
                }
                self.SceneAs<Level>().Particles.Emit(Seeker.P_HitWall, 12, new Vector2(x, self.Y), Vector2.UnitY * 4f, direction);
                Audio.Play(SFX.game_05_seeker_impact_normal, self.Position);
            }
        }

        private static void Player_OnCollideV(On.Celeste.Player.orig_OnCollideV orig, Player self, CollisionData data) {
            orig(self, data);

            if (seekerDashAttacking && !(data.Hit is SeekerBarrier)) { // SeekerBarriers handled elsewhere
                float direction;
                float y;
                if (data.Direction.Y > 0f) {
                    direction = -Calc.QuarterCircle;
                    y = self.Bottom;
                } else {
                    direction = Calc.QuarterCircle;
                    y = self.Top;
                }
                self.SceneAs<Level>().Particles.Emit(Seeker.P_HitWall, 12, new Vector2(self.X, y), Vector2.UnitX * 4f, direction);
                Audio.Play(SFX.game_05_seeker_impact_normal, self.Position);
            }
        }

        private static Color Player_GetCurrentTrailColor(On.Celeste.Player.orig_GetCurrentTrailColor orig, Player self) {
            if (seekerDashAttacking)
                return Seeker.TrailColor;

            return orig(self);
        }

        private static void Player_ctor(On.Celeste.Player.orig_ctor orig, Player self, Vector2 position, PlayerSpriteMode spriteMode) {
            orig(self, position, spriteMode);
            HasSeekerDash = seekerDashAttacking = false;
        }

        private static void Player_DashBegin(On.Celeste.Player.orig_DashBegin orig, Player self) {
            orig(self);
            if (HasSeekerDash) {
                seekerDashAttacking = true;
                seekerDashTimer = self.GetData().Get<float>("dashAttackTimer");
                HasSeekerDash = false;
            }
        }

        private static void Player_Update(On.Celeste.Player.orig_Update orig, Player self) {
            if (seekerDashAttacking)
                self.Scene.Tracker.GetEntities<SeekerBarrier>().ForEach(e => e.Collidable = true);

            orig(self);
            if (seekerDashAttacking)
                self.Scene.Tracker.GetEntities<SeekerBarrier>().ForEach(e => e.Collidable = false);

            float dashAttackTimer = self.GetData().Get<float>("dashAttackTimer");
            if (dashAttackTimer < seekerDashTimer)
                seekerDashTimer = dashAttackTimer;
            else if (seekerDashTimer > 0)
                seekerDashTimer -= Engine.DeltaTime;

            if (seekerDashTimer <= 0f)
                seekerDashAttacking = false;
        }

        private static void TempleCrackedBlock_ctor_EntityID_Vector2_float_float_bool(On.Celeste.TempleCrackedBlock.orig_ctor_EntityID_Vector2_float_float_bool orig, TempleCrackedBlock self, EntityID eid, Vector2 position, float width, float height, bool persistent) {
            orig(self, eid, position, width, height, persistent);
            self.OnDashCollide = new DashCollision((player, dir) => {
                if (seekerDashAttacking) {
                    self.Break(player.Center);
                    return DashCollisionResults.Rebound;
                }
                return DashCollisionResults.NormalCollision;
            });
        }

        private static DashCollisionResults DashBlock_OnDashed(On.Celeste.DashBlock.orig_OnDashed orig, DashBlock self, Player player, Vector2 direction) {
            if (seekerDashAttacking) {
                self.Break(player.Center, direction, true);
                return DashCollisionResults.Rebound;
            }
            return orig(self, player, direction);
        }

        private static void SeekerBarrier_ctor_Vector2_float_float(On.Celeste.SeekerBarrier.orig_ctor_Vector2_float_float orig, SeekerBarrier self, Vector2 position, float width, float height) {
            orig(self, position, width, height);
            self.OnDashCollide = new DashCollision((player, dir) => {
                if (seekerDashAttacking) {
                    Vector2 origin = dir.X > 0 ? player.CenterRight : dir.X < 0 ? player.CenterLeft : dir.Y > 0 ? player.BottomCenter : player.TopCenter;
                    self.SceneAs<Level>().Particles.Emit(Seeker.P_HitWall, 12, origin, new Vector2(dir.Y, dir.X) * 4f, (-dir).Angle());
                    self.OnReflectSeeker();
                    Audio.Play(SFX.game_05_seeker_impact_lightwall, self.Position);
                    return DashCollisionResults.Bounce;
                }
                return DashCollisionResults.Ignore;
            });
        }
    }
}
