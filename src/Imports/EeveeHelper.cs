using Celeste.Mod.CommunalHelper.Entities;
using MonoMod.ModInterop;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Imports;

[ModImportName("EeveeHelper")]
public static class EeveeHelper
{
    public static Action<Type, bool, HashSet<string>> RegisterBlacklistedAnchors;

    public static void Initialize()
    {
        typeof(EeveeHelper).ModInterop();

        string[] defaultBlacklistedAnchors = ["Position", "ExactPosition", "TopLeft", "TopCenter", "TopRight", "Center", "CenterLeft", "CenterRight", "BottomLeft", "BottomCenter", "BottomRight"];
        RegisterBlacklistedAnchors?.Invoke(typeof(ConnectedSolid), true, defaultBlacklistedAnchors.Union(["GroupBoundsMin", "GroupBoundsMax", "GroupOffset", "GroupCenter"]).ToHashSet());
        RegisterBlacklistedAnchors?.Invoke(typeof(ConnectedDreamBlock), false, defaultBlacklistedAnchors.Union(["GroupBoundsMin", "GroupBoundsMax", "GroupOffset"]).ToHashSet());
    }
}
