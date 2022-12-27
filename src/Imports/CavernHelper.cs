using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.ModInterop;
using System;

namespace Celeste.Mod.CommunalHelper.Imports;

/// <summary>
/// Import methods defined here: <see href="https://github.com/CommunalHelper/CavernHelper/blob/dev/Code/CavernInterop.cs">CavernInterop</see>.
/// </summary>
[ModImportName("CavernHelper")]
public static class CavernHelper
{
    public static Func<Action<Vector2>, Collider, Component> GetCrystalBombExplosionCollider;
}
