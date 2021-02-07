using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System.Collections;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/DreamCrumbleWallOnRumble")]
    [Tracked]
    public class DreamCrumbleWallOnRumble : CustomDreamBlock {

        /// <summary>
        /// DynData field for storing the List&lt;<see cref="DreamCrumbleWallOnRumble"/>&gt; in a <see cref="RumbleTrigger"/>
        /// </summary>
        public const string RUMBLETRIGGER_DREAMCRUMBLES = "communalHelperDreamCrumbles";

        private bool persistent;
        private EntityID id;
        public DreamCrumbleWallOnRumble(EntityData data, Vector2 offset, EntityID id)
            : base(data.Position + offset, data.Width, data.Height, data.Bool("featherMode"), data.Bool("oneUse"), GetRefillCount(data), data.Bool("below")) {
            this.id = id;
            persistent = data.Bool("persistent");
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            if (CollideCheck<Player>()) {
                RemoveSelf();
            }
        }

        public void Break() {
            if (Collidable && Scene != null) {
                Audio.Play(SFX.game_10_quake_rockbreak, Position);
                Collidable = false;
                for (int x = 0; x < Width / 8f; x++) {
                    for (int y = 0; y < Height / 8f; y++) {
                        if (!Scene.CollideCheck<Solid>(new Rectangle((int) X + x * 8, (int) Y + y * 8, 8, 8))) {
                            Scene.Add(Engine.Pooler.Create<DreamBlockDebris>().Init(Position + new Vector2(4 + x * 8, 4 + y * 8), this).BlastFrom(TopCenter));
                        }
                    }
                }
                if (persistent) {
                    SceneAs<Level>().Session.DoNotLoad.Add(id);
                }
                RemoveSelf();
            }
        }

        #region Hooks

        internal new static void Load() {
            On.Celeste.RumbleTrigger.Awake += RumbleTrigger_Awake;
            On.Celeste.RumbleTrigger.RumbleRoutine += RumbleTrigger_RumbleRoutine;
        }

        internal new static void Unload() {
            On.Celeste.RumbleTrigger.Awake -= RumbleTrigger_Awake;
            On.Celeste.RumbleTrigger.RumbleRoutine -= RumbleTrigger_RumbleRoutine;
        }

        private static void RumbleTrigger_Awake(On.Celeste.RumbleTrigger.orig_Awake orig, RumbleTrigger self, Scene scene) {
            orig(self, scene);

            DynData<RumbleTrigger> triggerData = new DynData<RumbleTrigger>(self);
            bool constrainHeight = triggerData.Get<bool>("constrainHeight");

            // Reflection is slow
            float top = constrainHeight ? triggerData.Get<float>("top") : 0;
            float bottom = constrainHeight ? triggerData.Get<float>("bottom") : 0;
            float left = triggerData.Get<float>("left");
            float right = triggerData.Get<float>("right");

            bool triggered = triggerData.Get<bool>("persistent") && (scene as Level).Session.GetFlag(triggerData.Get<EntityID>("id").ToString());

            List<DreamCrumbleWallOnRumble> crumbles = new List<DreamCrumbleWallOnRumble>();
            foreach (Entity entity in scene.Tracker.GetEntities<DreamCrumbleWallOnRumble>()) {
                DreamCrumbleWallOnRumble crumble = (DreamCrumbleWallOnRumble) entity;
                if ((!constrainHeight || (crumble.Y >= top && crumble.Y <= bottom)) && crumble.X >= left && crumble.X <= right) {
                    if (triggered)
                        crumble.RemoveSelf();
                    else
                        crumbles.Add(crumble);
                }
            }
            // Slightly unsafe, but only if someone else does some weird stuff with RumbleTriggers
            if (!triggered)
                triggerData[RUMBLETRIGGER_DREAMCRUMBLES] = crumbles;
        }

        private static IEnumerator RumbleTrigger_RumbleRoutine(On.Celeste.RumbleTrigger.orig_RumbleRoutine orig, RumbleTrigger self, float delay) {
            DynData<RumbleTrigger> triggerData = new DynData<RumbleTrigger>(self);

            IEnumerator enumerator = orig(self, delay);
            while (enumerator.MoveNext()) {
                yield return enumerator.Current;
            }

            foreach (DreamCrumbleWallOnRumble crumble in triggerData.Get<List<DreamCrumbleWallOnRumble>>(RUMBLETRIGGER_DREAMCRUMBLES)) {
                crumble.Break();
                yield return 0.05f;
            }
        }

        #endregion

    }
}
