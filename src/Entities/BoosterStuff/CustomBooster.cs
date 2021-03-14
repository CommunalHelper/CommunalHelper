using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;

namespace Celeste.Mod.CommunalHelper.Entities {
    public abstract class CustomBooster : Booster {

        protected DynData<Booster> BoosterData;

        public ParticleType P_CustomAppear, P_CustomBurst;

        private bool hasCustomSounds;
        private string enterSoundEvent, moveSoundEvent;
        private bool playMoveEventEnd;

        public bool RedBoost => BoosterData.Get<bool>("red");

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
            BoosterData["particleType"] = P_CustomBurst = new ParticleType(P_Burst) {
                Color = burstColor
            };
            P_CustomAppear = new ParticleType(P_Appear) {
                Color = appearColor
            };
        }

        protected void SetSoundEvent(string enterSound, string moveSound, bool playMoveEnd = false) {
            enterSoundEvent = enterSound;
            moveSoundEvent = moveSound;
            playMoveEventEnd = playMoveEnd;
            hasCustomSounds = true;
        }

        #region Hooks

        public static void Unload() {
            DreamBoosterHooks.Unhook();

            On.Celeste.Booster.AppearParticles -= Booster_AppearParticles;
            On.Celeste.Booster.OnPlayer -= Booster_OnPlayer;
            On.Celeste.Booster.PlayerBoosted -= Booster_PlayerBoosted;
        }

        public static void Load() {
            DreamBoosterHooks.Hook();

            On.Celeste.Booster.AppearParticles += Booster_AppearParticles;
            On.Celeste.Booster.OnPlayer += Booster_OnPlayer;
            On.Celeste.Booster.PlayerBoosted += Booster_PlayerBoosted;
        }

        private static void Booster_PlayerBoosted(On.Celeste.Booster.orig_PlayerBoosted orig, Booster self, Player player, Vector2 direction) {
            orig(self, player, direction);
            if (self is CustomBooster booster && booster.hasCustomSounds && booster.RedBoost) {
                booster.BoosterData.Get<SoundSource>("loopingSfx").Play(booster.moveSoundEvent);
            }
        }

        private static void Booster_OnPlayer(On.Celeste.Booster.orig_OnPlayer orig, Booster self, Player player) {
            if (self is CustomBooster booster && booster.hasCustomSounds) {
                if (booster.BoosterData.Get<float>("respawnTimer") <= 0f && booster.BoosterData.Get<float>("cannotUseTimer") <= 0f && !self.BoostingPlayer) {
                    booster.BoosterData["cannotUseTimer"] = 0.45f;
                    if (booster.RedBoost) {
                        player.RedBoost(self);
                    } else {
                        player.Boost(self);
                    }
                    Audio.Play(booster.enterSoundEvent, self.Position);
                    booster.BoosterData.Get<Wiggler>("wiggler").Start();
                    Sprite sprite = booster.BoosterData.Get<Sprite>("sprite");
                    sprite.Play("inside");
                    sprite.FlipX = player.Facing == Facings.Left;
                }
            } else {
                orig(self, player);
            }
        }

        private static void Booster_AppearParticles(On.Celeste.Booster.orig_AppearParticles orig, Booster self) {
            if (self is CustomBooster booster) {
                ParticleSystem particlesBG = self.SceneAs<Level>().ParticlesBG;
                for (int i = 0; i < 360; i += 30) {
                    particlesBG.Emit(booster.P_CustomAppear, 1, self.Center, Vector2.One * 2f, i * ((float) Math.PI / 180f));
                }
            } else {
                orig(self);
            }
        }

        #endregion
    }
}
