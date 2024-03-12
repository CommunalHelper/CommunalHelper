using MonoMod.ModInterop;

namespace Celeste.Mod.CommunalHelper.Imports;

/// <summary>
/// Import methods defined here: <see href="https://github.com/wuke32767/CelesteReverseHelper/blob/main/ReverseHelperInterop.cs">CavernInterop</see>.
/// </summary>
[ModImportName("ReverseHelper.DreamBlock")]
public static class ReverseHelper
{
    public static Action<Type, Action<Entity>, Action<Entity>> RegisterDreamBlockLike;
}
