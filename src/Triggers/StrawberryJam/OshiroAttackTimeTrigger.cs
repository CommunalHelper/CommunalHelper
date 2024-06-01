using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Triggers.StrawberryJam;

[CustomEntity("CommunalHelper/SJ/OshiroAttackTimeTrigger")]
public class OshiroAttackTimeTrigger : Trigger {
    private bool Enable = true;
    private static ILHook oshiroHook;
    private static BindingFlags privateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
    private static MethodInfo oshiroCoroutineInfo = typeof(AngryOshiro).GetMethod("ChaseCoroutine", privateInstance).GetStateMachineTarget();
    public OshiroAttackTimeTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        Enable = data.Bool("Enable", true);
    }
    public static void Load() {
        oshiroHook = new ILHook(oshiroCoroutineInfo, ModAttackTime);
    }
    public static void Unload() {
        oshiroHook?.Dispose();
        oshiroHook = null;
    }

    private static void ModAttackTime(ILContext il) {
        ILCursor cursor = new ILCursor(il);
        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<AreaKey>("Mode"))) {
            cursor.EmitDelegate<Func<AreaMode, AreaMode>>(orig => CommunalHelperModule.Session.OshiroBsideTimer ? AreaMode.BSide : orig);
            break;
        }
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);
        CommunalHelperModule.Session.OshiroBsideTimer = Enable;
    }
}
