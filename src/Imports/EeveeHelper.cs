using Celeste.Mod.CommunalHelper.Entities;
using MonoMod.ModInterop;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Imports;

[ModImportName("EeveeHelper")]
public static class EeveeHelper
{
    public static Action<Type, HashSet<string>> RegisterIgnoredAnchors;

    public static void Initialize()
    {
        typeof(EeveeHelper).ModInterop();

        RegisterIgnoredAnchors?.Invoke(typeof(ConnectedSolid), ["GroupBoundsMin", "GroupBoundsMax", "GroupOffset", "GroupCenter"]);
        RegisterIgnoredAnchors?.Invoke(typeof(ConnectedDreamBlock), ["GroupBoundsMin", "GroupBoundsMax", "GroupOffset"]);
    }
}
