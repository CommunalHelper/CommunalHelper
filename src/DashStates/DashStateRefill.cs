using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.DashStates;

public abstract class DashStateRefill : Refill
{
    protected string TouchSFX = CustomSFX.game_dreamRefill_dream_refill_touch;
    protected string ReturnSFX = CustomSFX.game_dreamRefill_dream_refill_return;

    private readonly float respawnTime;

    protected DynamicData baseData;

    protected DashStateRefill(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        baseData = new(typeof(Refill), this);

        respawnTime = data.Float("respawnTime", 2.5f); // default is 2.5 sec.

        Get<PlayerCollider>().OnCollide = OnPlayer;

        if (TryCreateCustomSprite(out Sprite sprite))
        {
            Remove(baseData.Get<Sprite>("sprite"));
            Add(sprite);
            baseData.Set("sprite", sprite);
        }

        if (TryCreateCustomOutline(out Image outline))
        {
            Remove(baseData.Get<Image>("outline"));
            baseData.Set("outline", outline);
            Add(outline);
        }

        if (TryCreateCustomFlash(out Sprite flash))
        {
            Remove(baseData.Get<Sprite>("flash"));
            Add(flash);
            baseData.Set("flash", flash);
        }
    }

    private void OnPlayer(Player player)
    {
        if (CanActivate(player))
        {
            player.RefillDash();
            player.RefillStamina();
            Activated(player);
            Audio.Play(TouchSFX, Position);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            Collidable = false;
            Add(new Coroutine((IEnumerator) m_Refill_RefillRoutine.Invoke(this, new object[] { player })));
            baseData.Set("respawnTimer", respawnTime);
        }
    }

    protected abstract bool CanActivate(Player player);

    protected abstract void Activated(Player player);

    protected virtual bool TryCreateCustomSprite(out Sprite sprite)
    {
        sprite = null;
        return false;
    }

    protected virtual bool TryCreateCustomOutline(out Image image)
    {
        image = null;
        return false;
    }

    protected virtual bool TryCreateCustomFlash(out Sprite sprite)
    {
        sprite = null;
        return false;
    }

    protected virtual void EmitGlowParticles()
    {
        Level level = baseData.Get<Level>("level");
        level.ParticlesFG.Emit(baseData.Get<ParticleType>("p_glow"), 1, Position, Vector2.One * 5f);
    }

    protected virtual void EmitShatterParticles(float angle)
    {
        Level level = baseData.Get<Level>("level");
        ParticleType p_shatter = baseData.Get<ParticleType>("p_shatter");
        level.ParticlesFG.Emit(p_shatter, 5, Position, Vector2.One * 4f, angle - Calc.QuarterCircle);
        level.ParticlesFG.Emit(p_shatter, 5, Position, Vector2.One * 4f, angle + Calc.QuarterCircle);
    }

    protected virtual void EmitRegenParticles()
    {
        Level level = baseData.Get<Level>("level");
        level.ParticlesFG.Emit(baseData.Get<ParticleType>("p_regen"), 16, Position, Vector2.One * 2f);
    }

    #region Hooks

    private static readonly TypeInfo t_DashStateRefill = typeof(DashStateRefill).GetTypeInfo();
    private static readonly MethodInfo m_Refill_RefillRoutine = typeof(Refill).GetMethod("RefillRoutine", BindingFlags.NonPublic | BindingFlags.Instance);
    private static ILHook hook_Refill_RefillRoutine;

    internal static void Load()
    {
        IL.Celeste.Refill.Update += Refill_Update;
        IL.Celeste.Refill.Respawn += Refill_Respawn;
        hook_Refill_RefillRoutine = new ILHook(m_Refill_RefillRoutine.GetStateMachineTarget(),
            Refill_RefillRoutine);
    }

    internal static void Unload()
    {
        IL.Celeste.Refill.Update -= Refill_Update;
        IL.Celeste.Refill.Respawn -= Refill_Respawn;
        hook_Refill_RefillRoutine.Dispose();
    }

    private static void Refill_Update(ILContext il)
    {
        PatchRefillParticles(il, refill => refill.EmitGlowParticles());
    }

    private static void Refill_Respawn(ILContext il)
    {
        PatchRefillParticles(il, refill => refill.EmitRegenParticles());

        ILCursor cursor = new(il);

        cursor.GotoNext(MoveType.After, instr => instr.MatchLdstr(SFX.game_gen_diamond_return));
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Func<string, Refill, string>>((str, refill) =>
        {
            return refill is DashStateRefill dashStateRefill ? dashStateRefill.ReturnSFX : str;
        });
    }

    private static void PatchRefillParticles(ILContext il, Action<DashStateRefill> method)
    {
        ILCursor cursor = new(il);

        cursor.GotoNext(instr => instr.Next.MatchLdfld<Refill>("level"));

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Func<Refill, bool>>(r =>
        {
            if (r is DashStateRefill refill)
            {
                method.Invoke(refill);
                return true;
            }
            return false;
        });
        cursor.Emit(OpCodes.Brtrue, il.Instrs.First(instr => instr.MatchCallvirt<ParticleSystem>("Emit")).Next);
    }

    private static void Refill_RefillRoutine(ILContext il)
    {
        Type StateMachineType = m_Refill_RefillRoutine.GetStateMachineTarget().DeclaringType;
        FieldInfo f_this = StateMachineType.GetField("<>4__this", BindingFlags.Public | BindingFlags.Instance);
        FieldInfo f_player = StateMachineType.GetField("player", BindingFlags.Public | BindingFlags.Instance);

        ILCursor cursor = new(il);
        cursor.GotoNext(instr => instr.Match(OpCodes.Ldarg_0),
            instr => instr.MatchLdfld(out FieldReference field) && field.Name == "player");
        if (cursor.Prev.OpCode == OpCodes.Ldarg_0) // For SteamFNA
            cursor.Goto(cursor.Prev);

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldfld, f_this);
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldfld, f_player);

        cursor.EmitDelegate<Func<Refill, Player, bool>>((r, player) =>
        {
            if (r is DashStateRefill refill)
            {
                refill.EmitShatterParticles(player.Speed.Angle());
                return true;
            }
            return false;
        });

        cursor.Emit(OpCodes.Brtrue, il.Instrs.First(instr => instr.MatchCallvirt<ParticleSystem>("Emit")).Next);
    }

    #endregion

}
