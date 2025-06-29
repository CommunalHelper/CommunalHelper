using Celeste.Mod.CommunalHelper.DashStates;
using MonoMod.ModInterop;

namespace Celeste.Mod.CommunalHelper.Imports;

public static class SpeedrunTool
{
    public static void Initialize()
    {
        typeof(SaveLoadImports).ModInterop();

        SaveLoadImports.RegisterStaticTypes?.Invoke(typeof(DreamTunnelDash), [
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

    [ModImportName("SpeedrunTool.SaveLoad")]
    private static class SaveLoadImports
    {
#pragma warning disable CS0649 // Field 'RegisterStaticTypes' is never assigned to, and will always have its default value null
        public static Func<Type, string[], object> RegisterStaticTypes;
#pragma warning restore CS0649
    }
}
