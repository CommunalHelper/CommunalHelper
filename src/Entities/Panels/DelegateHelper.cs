using Microsoft.Xna.Framework;
using MonoMod.Utils;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper;

public static class DelegateHelper
{
    public const string DASHCOLLISIONHOOK_TAG = "CommunalHelper_DashCollisionHook_Hooks";

    public delegate DashCollisionResults DashCollisionHook(DashCollision orig, Player player, Vector2 direction);

    private static DashCollisionResults DashCollisionDefault(Player player, Vector2 orig)
    {
        return DashCollisionResults.NormalCollision;
    }

    private static DashCollision CreateDashCollisionHook(DashCollision orig, DashCollisionHook init = null)
    {
        orig ??= DashCollisionDefault;
        List<DashCollisionHook> hooks = new();
        DashCollision handler = (Player player, Vector2 direction) =>
        {
            int i = 0;
            DashCollisionResults Trampoline(DashCollision orig, Player player, Vector2 direction)
            {
                if (hooks.Count <= i)
                {
                    return orig(player, direction);
                }
                DashCollisionHook current = hooks[i++];
                return Trampoline((Player player, Vector2 direction) => current.Invoke(orig, player, direction), player, direction);
            }
            return Trampoline(orig, player, direction);
        };
        DynamicData.For(handler).Add(DASHCOLLISIONHOOK_TAG, hooks);
        if (init is not null)
            hooks.Add(init);
        return handler;
    }

    /// <returns>Handler to be assigned to Platform.OnDashCollide</returns>
    public static DashCollision ApplyDashCollisionHook(DashCollision orig, DashCollisionHook hook)
    {
        if (hook is null)
            throw new ArgumentNullException("hook");

        if (orig is not null && DynamicData.For(orig).TryGet(DASHCOLLISIONHOOK_TAG, out List<DashCollisionHook> hooks))
        {
            hooks.Add(hook);
            return orig;
        }
        else
        {
            DashCollision handler = CreateDashCollisionHook(orig, hook);
            return handler;
        }
    }

    public static void RemoveDashCollisionHook(DashCollision orig, DashCollisionHook hook)
    {
        if (hook is null)
            throw new ArgumentNullException("hook");

        if (orig is not null && DynamicData.For(orig).TryGet(DASHCOLLISIONHOOK_TAG, out List<DashCollisionHook> hooks))
        {
            hooks.Remove(hook);
        }
        else
        {
            Logger.Log(LogLevel.Warn, "CommunalHelper", $"[DelegateHelper] Attempted to remove hook `{hook.Method?.GetFullName()}` from un-handled DashCollision");
        }
    }
}
