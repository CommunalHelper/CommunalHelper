using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mod.CommunalHelper.Triggers.StrawberryJam;

[Tracked]
[CustomEntity("CommunalHelper/SJ/DashCountTrigger")]
public class DashCountTrigger : Trigger
{
    private readonly int newDashCount;
    private int origDashCount;
    private bool entered;

    public DashCountTrigger(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        newDashCount = data.Int("numberOfDashes", 1);
    }

    public override void OnEnter(Player player)
    {
        base.OnEnter(player);

        if (entered)
            return;

        origDashCount = player.Inventory.Dashes;
        SceneAs<Level>().Session.Inventory.Dashes = newDashCount;
        player.Dashes = newDashCount;
        entered = true;
    }

    public override void Removed(Scene scene)
    {
        base.Removed(scene);

        if (!entered)
            return;

        if (scene.Tracker.GetEntity<Player>() is Player player && player.Inventory.Dashes == newDashCount)
        {
            (scene as Level).Session.Inventory.Dashes = origDashCount;
            player.Dashes = origDashCount;
        }
    }

    public override void SceneEnd(Scene scene)
    {
        base.SceneEnd(scene);

        if (entered)
            (scene as Level).Session.Inventory.Dashes = origDashCount;
    }

    internal static void Load()
    {
        using (new DetourContext { After = { "*" } })
        {
            On.Celeste.PlayerHair.GetHairColor += ModPlayerGetHairColor;
            On.Celeste.Player.GetCurrentTrailColor += ModPlayerGetTrailColor;
            On.Celeste.Player.Die += ModDie;
        }
        On.Celeste.DeathEffect.Draw += ModDraw;
    }

    internal static void Unload()
    {
        On.Celeste.PlayerHair.GetHairColor -= ModPlayerGetHairColor;
        On.Celeste.Player.GetCurrentTrailColor -= ModPlayerGetTrailColor;
        On.Celeste.Player.Die -= ModDie;
        On.Celeste.DeathEffect.Draw -= ModDraw;
    }

    private static Color ModPlayerGetHairColor(On.Celeste.PlayerHair.orig_GetHairColor orig, PlayerHair self, int index)
    {
        if (self.Entity is Player player && player.Scene?.Tracker.GetEntity<DashCountTrigger>() != null)
        {
            return player.Dashes > 0 ? Player.NormalHairColor : Player.UsedHairColor;
        }

        return orig(self, index);
    }

    private static Color ModPlayerGetTrailColor(On.Celeste.Player.orig_GetCurrentTrailColor orig, Player self)
    {
        if (self.Dashes > 0 && self.Scene?.Tracker.GetEntity<DashCountTrigger>() != null)
        {
            return Player.NormalHairColor;
        }

        return orig(self);
    }

    private static PlayerDeadBody ModDie(On.Celeste.Player.orig_Die orig, Player self, Vector2 direction, bool evenIfInvincible = false, bool registerDeathInStats = true)
    {
        if (self.Scene?.Tracker.GetEntity<DashCountTrigger>() != null)
        {
            PlayerDeadBody Deadbody = orig(self, direction, evenIfInvincible, registerDeathInStats);
            if (Deadbody != null)
            {
                Color hairColor = (self.Dashes > 0) ? Player.NormalHairColor : Player.UsedHairColor;
                DynamicData.For(Deadbody).Set("initialHairColor", hairColor);
            }

            return Deadbody;
        }

        return orig(self, direction, evenIfInvincible, registerDeathInStats);
    }

    private static void ModDraw(On.Celeste.DeathEffect.orig_Draw orig, Vector2 position, Color color, float ease)
    {
        if (Engine.Scene is Level level && level.Tracker.GetEntity<DashCountTrigger>() != null && level.Tracker.GetEntity<Player>() is Player player)
        {
            color = player.Dashes > 0 ? Player.NormalHairColor : Player.UsedHairColor;
        }

        orig(position, color, ease);
    }
}
