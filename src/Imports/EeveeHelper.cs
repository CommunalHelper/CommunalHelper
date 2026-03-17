using Celeste.Mod.CommunalHelper.Entities;
using MonoMod.ModInterop;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Imports;

[ModImportName("EeveeHelper")]
public static class EeveeHelper
{
    public static Action<Type, bool, HashSet<string>> RegisterWhitelistedAnchors;
    public static Action<Type, bool, HashSet<string>> RegisterBlacklistedAnchors;

    public static void Initialize()
    {
        typeof(EeveeHelper).ModInterop();

        RegisterWhitelistedAnchors?.Invoke(typeof(ConnectedSolid), true, []);
        RegisterWhitelistedAnchors?.Invoke(typeof(ConnectedDreamBlock), true, []);
    }
}
