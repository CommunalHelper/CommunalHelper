using Celeste.Mod.CommunalHelper.DashStates;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using System;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/PlayerSeekerBarrier")]
    [TrackedAs(typeof(SeekerBarrier))]
    public class PlayerSeekerBarrier : SeekerBarrier {
        private static readonly float UncollidableParticleSpeedFactor   = 1.0f;
        private static readonly float CollidableParticleSpeedFactor     = 0.2f;

        private float speedFactor = UncollidableParticleSpeedFactor;

        // about 6 frames
        public const float WavedashLeniencyTimer = 0.1f;
        public float WavedashTime;

        public PlayerSeekerBarrier(EntityData data, Vector2 offset)
            : base(data, offset) {
            SurfaceSoundIndex = SurfaceIndex.AuroraGlass;
        }

        public override void Update() {
            bool collidable = SeekerDash.HasSeekerDash || SeekerDash.SeekerAttacking;
            float targetSpeed = collidable ? CollidableParticleSpeedFactor : UncollidableParticleSpeedFactor;
            speedFactor = Calc.Approach(speedFactor, targetSpeed, Engine.DeltaTime * (collidable ? 0.5f : 4.0f));

            WavedashTime = Calc.Approach(WavedashTime, 0f, Engine.DeltaTime);

            base.Update();
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            scene.Tracker.GetEntity<PlayerSeekerBarrierRenderer>().Track(this);
        }

        public override void Removed(Scene scene) {
            base.Removed(scene);
            scene.Tracker.GetEntity<PlayerSeekerBarrierRenderer>().Untrack(this);
        }

        #region Hooks

        private static readonly TypeInfo t_PlayerSeekerBarrier
            = typeof(PlayerSeekerBarrier).GetTypeInfo();

        private static readonly MethodInfo m_Draw_Rect
            = typeof(Draw).GetMethod(nameof(Draw.Rect), new[] { typeof(Collider), typeof(Color) });

        private static readonly ConstructorInfo ctor_SeekerBarrierRenderer_Edge
            = typeof(SeekerBarrierRenderer).GetNestedType("Edge", BindingFlags.NonPublic)
                                           .GetConstructor(new[] { typeof(SeekerBarrier), typeof(Vector2), typeof(Vector2) });

        internal static void Hook() {
            On.Celeste.SeekerBarrierRenderer.Track += SeekerBarrierRenderer_Track;
            On.Celeste.SeekerBarrierRenderer.Untrack += SeekerBarrierRenderer_Untrack;
            IL.Celeste.SeekerBarrier.Update += SeekerBarrier_Update;
        }

        internal static void Unhook() {
            On.Celeste.SeekerBarrierRenderer.Track -= SeekerBarrierRenderer_Track;
            On.Celeste.SeekerBarrierRenderer.Untrack -= SeekerBarrierRenderer_Untrack;
            IL.Celeste.SeekerBarrier.Update -= SeekerBarrier_Update;
        }

        private static void SeekerBarrierRenderer_Track(On.Celeste.SeekerBarrierRenderer.orig_Track orig, SeekerBarrierRenderer self, SeekerBarrier block) {
            if (block is PlayerSeekerBarrier)
                return;
            orig(self, block);
        }

        private static void SeekerBarrierRenderer_Untrack(On.Celeste.SeekerBarrierRenderer.orig_Untrack orig, SeekerBarrierRenderer self, SeekerBarrier block) {
            if (block is PlayerSeekerBarrier)
                return;
            orig(self, block);
        }
        private static void SeekerBarrier_Update(ILContext il) {
            ILCursor cursor = new(il);

            cursor.GotoNext(MoveType.After, instr => instr.MatchLdelemR4());
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<float, SeekerBarrier, float>>((speed, barrier) => {
                if (barrier is PlayerSeekerBarrier playerSeekerBarrier)
                    speed *= playerSeekerBarrier.speedFactor;
                return speed;
            });
        }

        #endregion
    }
}
