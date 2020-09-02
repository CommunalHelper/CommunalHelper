using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/DreamRefill")]
    public class DreamRefill : Refill {

        public new static ParticleType[] P_Shatter;
        private int shatterParticleIndex = 0;
        public new static ParticleType[] P_Regen;
        private int regenParticleIndex = 0;
        public new static ParticleType[] P_Glow;
        private int glowParticleIndex = 0;

        public static void InitializeParticles() {

            P_Shatter = new ParticleType[] { Refill.P_Shatter, null, null, null };
            P_Regen = new ParticleType[] { Refill.P_Regen, null, null, null };
            ;
            P_Glow = new ParticleType[] { Refill.P_Glow, null, null, null };
            ;
            ParticleType[][] particles = new ParticleType[][] { P_Shatter, P_Regen, P_Glow };

            for (int i = 0; i < 3; ++i) {
                ParticleType particle = new ParticleType(particles[i][0]);
                particle.ColorMode = ParticleType.ColorModes.Choose;

                particles[i][0] = new ParticleType(particle) {
                    Color = Calc.HexToColor("FFEF11"),
                    Color2 = Calc.HexToColor("FF00D0")
                };

                particles[i][1] = new ParticleType(particle) {
                    Color = Calc.HexToColor("08a310"),
                    Color2 = Calc.HexToColor("5fcde4")
                };

                particles[i][2] = new ParticleType(particle) {
                    Color = Calc.HexToColor("7fb25e"),
                    Color2 = Calc.HexToColor("E0564C")
                };

                particles[i][3] = new ParticleType(particle) {
                    Color = Calc.HexToColor("5b6ee1"),
                    Color2 = Calc.HexToColor("CC3B3B")
                };
            }
        }

        private DynData<Refill> baseData;

        public DreamRefill(EntityData data, Vector2 offset)
            : base(data.Position + offset, false, data.Bool("oneUse")) {
            baseData = new DynData<Refill>(this);

            Get<PlayerCollider>().OnCollide = OnPlayer;

            Remove(baseData.Get<Sprite>("sprite"));
            Sprite sprite = new Sprite(GFX.Game, "objects/CommunalHelper/dreamRefill/idle");
            sprite.AddLoop("idle", "", 0.1f);
            sprite.Play("idle");
            sprite.CenterOrigin();
            baseData["sprite"] = sprite;
            Add(sprite);

        }

        private void EmitGlowParticles() {
            baseData.Get<Level>("level").ParticlesFG.Emit(P_Glow[glowParticleIndex], 1, Position, Vector2.One * 5f);
            ++glowParticleIndex;
            glowParticleIndex %= 4;
        }

        private void EmitShatterParticles(float angle) {
            for (int i = 0; i < 5; ++i) {
                baseData.Get<Level>("level").ParticlesFG.Emit(P_Shatter[shatterParticleIndex], 1, Position, Vector2.One * 4f, angle - (float) Math.PI / 2f);
                ++shatterParticleIndex;
                shatterParticleIndex %= 4;
            }
            for (int i = 0; i < 5; ++i) {
                baseData.Get<Level>("level").ParticlesFG.Emit(P_Shatter[shatterParticleIndex], 1, Position, Vector2.One * 4f, angle + (float) Math.PI / 2f);
                ++shatterParticleIndex;
                shatterParticleIndex %= 4;
            }
        }

        private void EmitRegenParticles() {
            for (int i = 0; i < 16; ++i) {
                baseData.Get<Level>("level").ParticlesFG.Emit(P_Regen[regenParticleIndex], 1, Position, Vector2.One * 2f);
                ++regenParticleIndex;
                regenParticleIndex %= 4;
            }
        }

        private void OnPlayer(Player player) {
            if (player.Stamina < 20f || !DreamTunnelDash.HasDreamTunnelDash) {
                player.RefillDash();
                player.RefillStamina();
                DreamTunnelDash.HasDreamTunnelDash = true;
                Audio.Play(CustomSFX.game_dreamRefill_dream_refill_touch, Position);
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                Collidable = false;
                Add(new Coroutine((IEnumerator) m_Refill_RefillRoutine.Invoke(this, new object[] { player })));
                baseData["respawnTimer"] = 2.5f;
            }
        }

        #region Hooks

        private static TypeInfo t_DreamRefill = typeof(DreamRefill).GetTypeInfo();
        private static MethodInfo m_Refill_RefillRoutine = typeof(Refill).GetMethod("RefillRoutine", BindingFlags.NonPublic | BindingFlags.Instance);
        private static ILHook hook_Refill_RefillRoutine;

        internal static void Load() {
            IL.Celeste.Refill.Update += Refill_Update;
            IL.Celeste.Refill.Respawn += Refill_Respawn;
            hook_Refill_RefillRoutine = new ILHook(m_Refill_RefillRoutine.GetStateMachineTarget(),
                Refill_RefillRoutine);
        }

        internal static void Unload() {
            IL.Celeste.Refill.Update -= Refill_Update;
            IL.Celeste.Refill.Respawn -= Refill_Respawn;
            hook_Refill_RefillRoutine.Dispose();
        }

        private static void Refill_Update(ILContext il) {
            PatchRefillParticles(il, refill => refill.EmitGlowParticles());
        }

        private static void Refill_Respawn(ILContext il) {
            PatchRefillParticles(il, refill => refill.EmitRegenParticles());

            ILCursor cursor = new ILCursor(il);

            cursor.GotoNext(MoveType.After, instr => instr.MatchLdstr(SFX.game_10_pinkdiamond_return));
            // Cursed, apparently:
            /*
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Refill, string, string>>((refill, str) => {
                if (refill is DreamRefill)
                    return CustomSFX.game_dreamRefill_dream_refill_return;
                return str;
            });
            */
            // But this works fine:
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Isinst, t_DreamRefill);
            cursor.Emit(OpCodes.Brfalse_S, cursor.Next);
            cursor.Emit(OpCodes.Pop);
            cursor.Emit(OpCodes.Ldstr, CustomSFX.game_dreamRefill_dream_refill_return);

        }

        private static void PatchRefillParticles(ILContext il, Action<DreamRefill> method) {
            ILCursor cursor = new ILCursor(il);

            cursor.GotoNext(instr => instr.Next.MatchLdfld<Refill>("level"));

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Isinst, t_DreamRefill);
            cursor.Emit(OpCodes.Brfalse_S, cursor.Next);

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Castclass, t_DreamRefill);
            cursor.EmitDelegate(method);
            cursor.Emit(OpCodes.Br_S, il.Instrs.First(instr => instr.MatchCallvirt<ParticleSystem>("Emit")).Next);
        }

        private static void Refill_RefillRoutine(ILContext il) {
            FieldInfo f_this = m_Refill_RefillRoutine.GetStateMachineTarget().DeclaringType.GetField("<>4__this", BindingFlags.Public | BindingFlags.Instance);

            ILCursor cursor = new ILCursor(il);
            cursor.GotoNext(instr => instr.MatchLdfld<Level>("ParticlesFG"));
            cursor.GotoPrev(instr => instr.OpCode != OpCodes.Ldfld); // A bit messy but eh
            Instruction angleLoc = cursor.Prev;

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, f_this);
            cursor.Emit(OpCodes.Isinst, typeof(DreamRefill).GetTypeInfo());
            cursor.Emit(OpCodes.Brfalse_S, cursor.Next);

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, f_this);
            cursor.Emit(OpCodes.Castclass, t_DreamRefill);

            if (angleLoc.OpCode == OpCodes.Stfld) { // Steam FNA has different il code
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldfld, angleLoc.Operand);
            } else
                cursor.Emit(OpCodes.Ldloc_2);

            cursor.EmitDelegate<Action<DreamRefill, float>>((refill, angle) => refill.EmitShatterParticles(angle));
            cursor.Emit(OpCodes.Br_S, il.Instrs.First(instr => instr.MatchCallvirt<ParticleSystem>("Emit")).Next);
        }

        #endregion

    }
}
