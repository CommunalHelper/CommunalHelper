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
    public abstract class DashStateRefill : Refill {

        protected string TouchSFX = CustomSFX.game_dreamRefill_dream_refill_touch;
        protected string ReturnSFX = CustomSFX.game_dreamRefill_dream_refill_return;

        protected DynData<Refill> baseData;

        protected DashStateRefill(EntityData data, Vector2 offset)
            : base(data, offset) {
            baseData = new DynData<Refill>(this);

            Get<PlayerCollider>().OnCollide = OnPlayer;

            if (TryCreateCustomSprite(out Sprite sprite)) {
                Remove(baseData.Get<Sprite>("sprite"));
                Add(sprite);
                baseData["sprite"] = sprite;
            }

            if (TryCreateCustomOutline(out Sprite outline)) {
                Remove(baseData.Get<Sprite>("outline"));
                Add(outline);
                baseData["outline"] = outline;
            }

            if (TryCreateCustomFlash(out Sprite flash)) {
                Remove(baseData.Get<Sprite>("flash"));
                Add(flash);
                baseData["flash"] = flash;
            }

        }

        private void OnPlayer(Player player) {
            if (CanActivate(player)) {
                player.RefillDash();
                player.RefillStamina();
                Activated(player);
                Audio.Play(TouchSFX, Position);
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                Collidable = false;
                Add(new Coroutine((IEnumerator) m_Refill_RefillRoutine.Invoke(this, new object[] { player })));
                baseData["respawnTimer"] = 2.5f;
            }
        }

        protected abstract bool CanActivate(Player player);

        protected abstract void Activated(Player player);

        protected virtual bool TryCreateCustomSprite(out Sprite sprite) {
            sprite = null;
            return false;
        }

        protected virtual bool TryCreateCustomOutline(out Sprite sprite) {
            sprite = null;
            return false;
        }

        protected virtual bool TryCreateCustomFlash(out Sprite sprite) {
            sprite = null;
            return false;
        }

        protected virtual void EmitGlowParticles(Level level) {
            level.ParticlesFG.Emit(baseData.Get<ParticleType>("p_glow"), 1, Position, Vector2.One * 5f);
        }

        protected virtual void EmitShatterParticles(Level level, float angle) {
            ParticleType p_shatter = baseData.Get<ParticleType>("p_shatter");
            level.ParticlesFG.Emit(p_shatter, 5, Position, Vector2.One * 4f, angle - Calc.QuarterCircle);
            level.ParticlesFG.Emit(p_shatter, 5, Position, Vector2.One * 4f, angle + Calc.QuarterCircle);
        }

        protected virtual void EmitRegenParticles(Level level) {
            level.ParticlesFG.Emit(baseData.Get<ParticleType>("p_regen"), 16, Position, Vector2.One * 2f);
        }

        #region Hooks

        private static TypeInfo t_DashStateRefill = typeof(DashStateRefill).GetTypeInfo();
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
            PatchRefillParticles(il, refill => refill.EmitGlowParticles(refill.baseData.Get<Level>("level")));
        }

        private static void Refill_Respawn(ILContext il) {
            PatchRefillParticles(il, refill => refill.EmitRegenParticles(refill.baseData.Get<Level>("level")));

            ILCursor cursor = new ILCursor(il);

            cursor.GotoNext(MoveType.After, instr => instr.MatchLdstr(SFX.game_10_pinkdiamond_return));
            // Cursed, apparently:

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<string, Refill, string>>((str, refill) => {
                if (refill is DashStateRefill dashStateRefill)
                    return dashStateRefill.ReturnSFX;
                return str;
            });
        }

        private static void PatchRefillParticles(ILContext il, Action<DashStateRefill> method) {
            ILCursor cursor = new ILCursor(il);

            cursor.GotoNext(instr => instr.Next.MatchLdfld<Refill>("level"));

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Isinst, t_DashStateRefill);
            cursor.Emit(OpCodes.Brfalse_S, cursor.Next);

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Castclass, t_DashStateRefill);
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
            cursor.Emit(OpCodes.Isinst, t_DashStateRefill);
            cursor.Emit(OpCodes.Brfalse_S, cursor.Next);

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, f_this);
            cursor.Emit(OpCodes.Castclass, t_DashStateRefill);

            if (angleLoc.OpCode == OpCodes.Stfld) { // Steam FNA has different il code
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldfld, angleLoc.Operand);
            } else
                cursor.Emit(OpCodes.Ldloc_2);

            cursor.EmitDelegate<Action<DashStateRefill, float>>((refill, angle) => refill.EmitShatterParticles(refill.baseData.Get<Level>("level"), angle));
            cursor.Emit(OpCodes.Br_S, il.Instrs.First(instr => instr.MatchCallvirt<ParticleSystem>("Emit")).Next);
        }

        #endregion

    }
}
