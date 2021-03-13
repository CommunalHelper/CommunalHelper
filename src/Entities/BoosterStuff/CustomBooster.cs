using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;

namespace Celeste.Mod.CommunalHelper.Entities {
    public abstract class CustomBooster : Booster {

        protected DynData<Booster> BoosterData;

        private ParticleType p_appear;

        public CustomBooster(Vector2 position, bool redBoost)
            : base(position, redBoost) {
            BoosterData = new DynData<Booster>(this);
        }

        protected void ReplaceSprite(Sprite newSprite) {
            Sprite oldSprite = BoosterData.Get<Sprite>("sprite");
            Remove(oldSprite);
            BoosterData["sprite"] = newSprite;
            Add(newSprite);
        }

        protected void SetParticleColors(Color burstColor, Color appearColor) {
            BoosterData["particleType"] = new ParticleType(P_Burst) {
                Color = burstColor
            };
            p_appear = new ParticleType(P_Appear) {
                Color = appearColor
            };
        }

        #region Hooks

        public static void Unhook() {
            DreamBoosterHooks.Unhook();

            On.Celeste.Booster.AppearParticles -= Booster_AppearParticles;
        }

        public static void Hook() {
            DreamBoosterHooks.Hook();

            On.Celeste.Booster.AppearParticles += Booster_AppearParticles;
        }

        private static void Booster_AppearParticles(On.Celeste.Booster.orig_AppearParticles orig, Booster self) {
            if (self is CustomBooster booster) {
                ParticleSystem particlesBG = self.SceneAs<Level>().ParticlesBG;
                for (int i = 0; i < 360; i += 30) {
                    particlesBG.Emit(booster.p_appear, 1, self.Center, Vector2.One * 2f, i * ((float) Math.PI / 180f));
                }
            } else {
                orig(self);
            }
        }

        #endregion
    }
}
