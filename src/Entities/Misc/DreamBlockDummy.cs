#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null

using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;

namespace Celeste.Mod.CommunalHelper.Entities {
    [TrackedAs(typeof(DreamBlock))]
    class DreamBlockDummy : DreamBlock {
        public Func<IEnumerator> OnActivate;
        public Func<IEnumerator> OnFastActivate;
        public Action OnActivateNoRoutine;

        public Func<IEnumerator> OnDeactivate;
        public Func<IEnumerator> OnFastDeactivate;
        public Action OnDeactivateNoRoutine;

        public Action OnSetup;

        public DynData<DreamBlock> Data;

        public DreamBlockDummy() 
            : base(Vector2.Zero, 0, 0, null, false, false) {
            Collidable = Active = Visible = false;

            Data = new DynData<DreamBlock>(this);
        }

        public override void Added(Scene scene) { }

        public override void Update() { }

        public override void Render() { }

        #region Hooks

        internal static void Load() {
            On.Celeste.DreamBlock.Activate += DreamBlock_Activate;
            On.Celeste.DreamBlock.FastActivate += DreamBlock_FastActivate;
            On.Celeste.DreamBlock.ActivateNoRoutine += DreamBlock_ActivateNoRoutine;

            On.Celeste.DreamBlock.Deactivate += DreamBlock_Deactivate;
            On.Celeste.DreamBlock.FastDeactivate += DreamBlock_FastDeactivate;
            On.Celeste.DreamBlock.DeactivateNoRoutine += DreamBlock_DeactivateNoRoutine;

            On.Celeste.DreamBlock.Setup += DreamBlock_Setup;
        }

        private static IEnumerator DreamBlock_Activate(On.Celeste.DreamBlock.orig_Activate orig, DreamBlock self) {
            return (self as DreamBlockDummy)?.OnActivate?.Invoke() ?? orig(self);
        }

        private static IEnumerator DreamBlock_FastActivate(On.Celeste.DreamBlock.orig_FastActivate orig, DreamBlock self) {
            return (self as DreamBlockDummy)?.OnFastActivate?.Invoke() ?? orig(self);
        }

        private static void DreamBlock_ActivateNoRoutine(On.Celeste.DreamBlock.orig_ActivateNoRoutine orig, DreamBlock self) {
            if (self is DreamBlockDummy dummy && dummy.OnActivateNoRoutine != null) {
                dummy.OnActivateNoRoutine();
                return;
            }
            orig(self);
        }

        private static IEnumerator DreamBlock_Deactivate(On.Celeste.DreamBlock.orig_Deactivate orig, DreamBlock self) {
            return (self as DreamBlockDummy)?.OnDeactivate?.Invoke() ?? orig(self);
        }

        private static IEnumerator DreamBlock_FastDeactivate(On.Celeste.DreamBlock.orig_FastDeactivate orig, DreamBlock self) {
            return (self as DreamBlockDummy)?.OnFastDeactivate?.Invoke() ?? orig(self);
        }

        private static void DreamBlock_DeactivateNoRoutine(On.Celeste.DreamBlock.orig_DeactivateNoRoutine orig, DreamBlock self) {
            if (self is DreamBlockDummy dummy && dummy.OnDeactivateNoRoutine != null) {
                dummy.OnDeactivateNoRoutine();
                return;
            }
            orig(self);
        }

        private static void DreamBlock_Setup(On.Celeste.DreamBlock.orig_Setup orig, DreamBlock self) {
            if (self is DreamBlockDummy dummy && dummy.OnSetup != null) {
                dummy.OnSetup();
                return;
            }
            orig(self);
        }

        #endregion

    }
}
