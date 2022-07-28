﻿using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Reflection;

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

            P_CustomAppear = P_Appear;
            P_CustomBurst = redBoost ? P_BurstRed : P_Burst;
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

        public void LoopingSfxParam(string path, float value) {
            BoosterData.Get<SoundSource>("loopingSfx").Param(path, value);
        }

        protected virtual void OnPlayerEnter(Player player) { }
        protected virtual void OnPlayerExit(Player player) { }

        /// <summary>
        /// Executed before <see cref="Player"/>.RedDashUpdate, can be used to return a different <see cref="Player"/> state ID.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns>
        /// An optional <see cref="Player"/> state ID. If set, it will be the returned <see cref="Player"/> state.<br/>
        /// Note: <see cref="RedDashUpdateAfter(Player)"/> takes priority over this method on which <see cref="Player"/> state is returned.
        /// </returns>
        protected virtual int? RedDashUpdateBefore(Player player) => null;
        /// <summary>
        /// Executed after <see cref="Player"/>.RedDashUpdate, can be used to return a different <see cref="Player"/> state ID.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns>An optional <see cref="Player"/> state ID. If set, it will be the returned <see cref="Player"/> state.<br/></returns>
        protected virtual int? RedDashUpdateAfter(Player player) => null;

        #region Hooks

        private static readonly MethodInfo m_Player_orig_Update
            = typeof(Player).GetMethod("orig_Update", BindingFlags.Public | BindingFlags.Instance);

        private static ILHook IL_Player_orig_Update;

        public static void Load() {
            DreamBoosterHooks.Hook();

            On.Celeste.Booster.AppearParticles += Booster_AppearParticles;
            On.Celeste.Booster.OnPlayer += Booster_OnPlayer;
            On.Celeste.Booster.PlayerBoosted += Booster_PlayerBoosted;
            On.Celeste.Booster.PlayerReleased += Booster_PlayerReleased;
            On.Celeste.Booster.BoostRoutine += Booster_BoostRoutine;

            On.Celeste.Player.RedDashUpdate += Player_RedDashUpdate;

            IL_Player_orig_Update = new ILHook(m_Player_orig_Update, Player_orig_Update);
        }

        public static void Unload() {
            DreamBoosterHooks.Unhook();

            On.Celeste.Booster.AppearParticles -= Booster_AppearParticles;
            On.Celeste.Booster.OnPlayer -= Booster_OnPlayer;
            On.Celeste.Booster.PlayerBoosted -= Booster_PlayerBoosted;
            On.Celeste.Booster.PlayerReleased -= Booster_PlayerReleased;
            On.Celeste.Booster.BoostRoutine -= Booster_BoostRoutine;

            On.Celeste.Player.RedDashUpdate -= Player_RedDashUpdate;

            IL_Player_orig_Update.Dispose();
        }

        private static IEnumerator Booster_BoostRoutine(On.Celeste.Booster.orig_BoostRoutine orig, Booster self, Player player, Vector2 dir) {
            IEnumerator origEnum = orig(self, player, dir);
            while (origEnum.MoveNext())
                yield return origEnum.Current;

            if (self is CustomBooster booster)
                booster.OnPlayerExit(player);
        }

        private static void Booster_PlayerReleased(On.Celeste.Booster.orig_PlayerReleased orig, Booster self) {
            orig(self);
            if (self is CustomBooster booster && booster.RedBoost && booster.hasCustomSounds && booster.playMoveEventEnd) {
                booster.BoosterData.Get<SoundSource>("loopingSfx").Play(booster.moveSoundEvent, "end", 1f);
            }
        }

        private static void Booster_PlayerBoosted(On.Celeste.Booster.orig_PlayerBoosted orig, Booster self, Player player, Vector2 direction) {
            orig(self, player, direction);
            if (self is CustomBooster booster && booster.hasCustomSounds && booster.RedBoost) {
                booster.BoosterData.Get<SoundSource>("loopingSfx").Play(booster.moveSoundEvent);
            }
        }

        private static void Booster_OnPlayer(On.Celeste.Booster.orig_OnPlayer orig, Booster self, Player player) {
            if (self is CustomBooster booster) {
                bool justEntered = booster.BoosterData.Get<float>("respawnTimer") <= 0f && booster.BoosterData.Get<float>("cannotUseTimer") <= 0f && !self.BoostingPlayer;
                if (booster.hasCustomSounds) {
                    if (justEntered) {
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
                if (justEntered)
                    booster.OnPlayerEnter(player);
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

        private static int Player_RedDashUpdate(On.Celeste.Player.orig_RedDashUpdate orig, Player self) {
            if (self.LastBooster is not CustomBooster booster)
                return orig(self);

            // execute RedDashUpdateBefore, store its potential replacement for returned state
            int? pre = booster.RedDashUpdateBefore(self);
            // original update
            int res = orig(self);
            // execute RedDashUpdateAfter, store its potential replacement for returned state
            int? post = booster.RedDashUpdateAfter(self);

            // return the 'latest' returned state.
            // 'post' takes priority first, then 'pre', and lastly the original result.
            return post ?? pre ?? res;
        }

        // Related to all curved boosters:
        // Makes the player unaffected by its own speed if it is inside a curved booster.
        // This allows us to set the player's speed, so that stuff like spikes-player checks work correctly.
        // But in reality, we are moving the player ourselves to the position we desire.
        private static void Player_orig_Update(ILContext il) {
            ILCursor cursor = new(il);

            ILLabel label = null;

            cursor.GotoNext(instr => instr.MatchCall<Actor>(nameof(Actor.Update)));

            // Prevent player being affected by its horizontal speed if UnaffectedBySpeed(Player) returns true.
            cursor.GotoNext(instr => instr.MatchCall<Actor>(nameof(Actor.MoveH)));
            cursor.GotoPrev(MoveType.After, instr => instr.MatchBeq(out label));
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Predicate<Player>>(UnaffectedBySpeed);
            cursor.Emit(OpCodes.Brtrue_S, label);

            // Prevent player being affected by its vertical speed if UnaffectedBySpeed(Player) returns true.
            cursor.GotoNext(instr => instr.MatchCall<Actor>(nameof(Actor.MoveV)));
            cursor.GotoPrev(MoveType.After, instr => instr.MatchBeq(out label));
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Predicate<Player>>(UnaffectedBySpeed);
            cursor.Emit(OpCodes.Brtrue_S, label);
        }

        private static bool UnaffectedBySpeed(Player player)
            => player.StateMachine.State == Player.StRedDash && player.LastBooster is DreamBoosterCurve or CurvedBooster;

        #endregion
    }
}
