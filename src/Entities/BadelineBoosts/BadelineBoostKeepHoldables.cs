using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Entities.BadelineBoosters;

[CustomEntity("CommunalHelper/BadelineBoostKeepHoldables")]
public class BadelineBoostKeepHoldables : BadelineBoost
{
    private static readonly MethodInfo m_BoostRoutine =
        typeof(BadelineBoost).GetMethod("BoostRoutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget();

    private static ILHook IL_BoostRoutine;

    public BadelineBoostKeepHoldables(EntityData data, Vector2 offset) : base(data, offset)
    {
    }

    public static void Hook()
    {
        IL_BoostRoutine = new ILHook(m_BoostRoutine, Hook_BoostRoutine);
    }

    public static void Unhook()
    {
        IL_BoostRoutine.Dispose();
    }

    private static void Hook_BoostRoutine(ILContext il)
    {
        // - if (player.Holding != null)
        // + if (this is not BadelineBoostKeepHoldables && player.Holding != null)

        ILCursor cursor = new(il);

        ILLabel noDrop = default;

        cursor.GotoNext(MoveType.After,
            instr => instr.MatchCallvirt<Player>("get_Holding"),
            instr => instr.MatchBrfalse(out noDrop));
        cursor.MoveAfterLabels();

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldfld, m_BoostRoutine.DeclaringType.GetField("<>4__this", BindingFlags.Public | BindingFlags.Instance));
        cursor.Emit(OpCodes.Isinst, typeof(BadelineBoostKeepHoldables));
        cursor.Emit(OpCodes.Brtrue_S, noDrop);
    }
}
