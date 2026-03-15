using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Components;

[Tracked]
public class DreamTunnelDangerous(Collider collider = null, Func<bool> isActive = null) : Component(false, false)
{
    public bool Active => isActive?.Invoke() ?? true;

    private bool Check(Player player)
    {
        if (!CommunalHelperModule.Session.CurrentDreamTunnelDashConfiguration.RespectDreamBlockLikes)
            return false;
        
        Collider c = Entity.Collider;
        if (collider is not null)
            Entity.Collider = collider;
        
        bool check = player.CollideCheck(Entity);
        
        Entity.Collider = c;
        return check;
    }

    public static DreamTunnelDangerous CollideFirst(Player player, Vector2? at = null)
    {
        Vector2 position = player.Position;
        if (at is { } a)
            player.Position = a;

        DreamTunnelDangerous result = player.Scene.Tracker.GetComponents<DreamTunnelDangerous>()
                                                          .Cast<DreamTunnelDangerous>()
                                                          .FirstOrDefault(dangerous => dangerous.Check(player));
        
        player.Position = position;
        return result;
    }
    
    public static bool CollideCheck(Player player, Vector2? at = null)
        => CollideFirst(player, at) is not null;

    public static Solid CollideFirstNonDangerous(Player player, Vector2? at = null)
    {
        Vector2 position = player.Position;
        if (at is { } a)
            player.Position = a;

        Solid result = player.Scene.Tracker.GetEntities<Solid>()
                                           .Cast<Solid>()
                                           .FirstOrDefault(solid =>
                                               player.CollideCheck(solid)
                                               && !(solid.Get<DreamTunnelDangerous>() is { } dangerous && dangerous.Check(player)));
        
        player.Position = position;
        return result;
    }

    public static bool CollideCheckNonDangerous(Player player, Vector2? at = null)
        => CollideFirstNonDangerous(player, at) is not null;
    
    #region Hooks

    internal static void Load()
    {
        On.Celeste.DreamBlock.Added += DreamBlock_Added;
    }

    internal static void Unload()
    {
        On.Celeste.DreamBlock.Added -= DreamBlock_Added;
    }

    private static void DreamBlock_Added(On.Celeste.DreamBlock.orig_Added orig, DreamBlock self, Scene scene)
    {
        orig(self, scene);
        
        self.Add(new DreamTunnelDangerous(null, () => self.playerHasDreamDash));
    }
    
    #endregion
}
