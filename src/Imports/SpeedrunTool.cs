using Celeste.Mod.CommunalHelper.DashStates;
using MonoMod.ModInterop;

namespace Celeste.Mod.CommunalHelper.Imports;

[ModImportName("SpeedrunTool.SaveLoad")]
public static class SpeedrunTool
{
    public static Func<Type, string[], object> RegisterStaticTypes;
    
    public static void Initialize()
    {
        typeof(SpeedrunTool).ModInterop();
        
        RegisterStaticTypes?.Invoke(typeof(DreamTunnelDash), [
            "St.DreamTunnelDash",
            "hasDreamTunnelDash",
            "dreamTunnelDashCount",
            "dreamTunnelDashAttacking",
            "dreamTunnelDashTimer",
            "nextDashFeather",
            "FeatherMode",
            "overrideDreamDashCheck",
            "DreamTrailColorIndex"
        ]);
    }
}
