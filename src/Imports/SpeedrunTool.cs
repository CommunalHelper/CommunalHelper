using Celeste.Mod.CommunalHelper.States;
using MonoMod.ModInterop;

namespace Celeste.Mod.CommunalHelper.Imports;

public static class SpeedrunTool
{
    public static void Initialize()
    {
        typeof(SaveLoadImports).ModInterop();

        // state fields
        // todo: eventually migrate to using components on the player to store this data
        SaveLoadImports.RegisterStaticTypes?.Invoke(typeof(DashStates.DreamTunnelDash), [
            "dreamTunnelDashCount",
            "canStartDreamTunnelDashAttack",
            "dreamTunnelDashAttacking",
            "dreamTunnelDashTimer",
            "nextDashFeather",
            "FeatherMode",
            "overrideDreamDashCheck",
            "DreamTrailColorIndex"
        ]);
        SaveLoadImports.RegisterStaticTypes?.Invoke(typeof(DashStates.SeekerDash), [
            "hasSeekerDash",
            "seekerDashAttacking",
            "seekerDashTimer",
            "seekerDashLaunched",
            "launchPossible"
        ]);
        SaveLoadImports.RegisterStaticTypes?.Invoke(typeof(Elytra), [
            "elytraToggle"
        ]);

        // state indices
        SaveLoadImports.RegisterStaticTypes?.Invoke(typeof(St), [
            "DreamTunnelDash",
            "Elytra"
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
