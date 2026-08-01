using Celeste.Mod.CommunalHelper.Entities;
using MonoMod.ModInterop;

namespace Celeste.Mod.CommunalHelper.Imports;

/// <summary>
/// Import methods defined here: <see href="https://github.com/wuke32767/CelesteReverseHelper/blob/main/ReverseHelperInterop.cs">ReverseHelperInterop</see>.
/// </summary>
[ModImportName("ReverseHelper.DreamBlock")]
public static class ReverseHelper
{
    public static Action<Type, Action<Entity>, Action<Entity>> RegisterDreamBlockLike;

    public static void Initialize()
    {
        typeof(ReverseHelper).ModInterop();
        
        RegisterDreamBlockLike?.Invoke(typeof(DreamTunnelEntry),
            e => (e as DreamTunnelEntry)!.ActivateNoRoutine(),
            e => (e as DreamTunnelEntry)!.DeactivateNoRoutine());
    }
}
