using Celeste.Mod.CommunalHelper.DashStates;
using MonoMod.ModInterop;

namespace Celeste.Mod.CommunalHelper.Imports;
public static class SpeedrunTool
{
    public static void Initialize()
    {
        typeof(SaveLoadImports).ModInterop();

        SaveLoadImports.RegisterStaticTypes?.Invoke(typeof(DreamTunnelDash), new string[9] {
            "StDreamTunnelDash",
            "hasDreamTunnelDash",
            "dreamTunnelDashCount",
            "dreamTunnelDashAttacking",
            "dreamTunnelDashTimer",
            "nextDashFeather",
            "FeatherMode",
            "overrideDreamDashCheck",
            "DreamTrailColorIndex"
        });
    }

    [ModImportName("SpeedrunTool.SaveLoad")]
    private static class SaveLoadImports
    {
        public static Func<Type, string[], object> RegisterStaticTypes;
    }
}
