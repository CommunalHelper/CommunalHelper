using MonoMod.Utils;

namespace Celeste.Mod.CommunalHelper.Entities;

[Tracked]
[CustomEntity("CommunalHelper/CassetteJumpFixController")]
public class CassetteJumpFixController : Entity
{
    private readonly bool enable, persistent;

    public CassetteJumpFixController(EntityData data, Vector2 _)
    {
        Visible = Collidable = false;

        enable = !data.Bool("off", false);
        persistent = data.Bool("persistent", false);
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);

        if (persistent || !enable)
        {
            CommunalHelperModule.Session.CassetteJumpFix = enable;
        }
    }

    public static bool MustApply(Scene scene)
    {
        int debug = 0;
        try
        {
            foreach (CassetteJumpFixController controller in scene.Tracker.GetEntities<CassetteJumpFixController>())
            {
                debug = 1;
                if (controller.enable && !controller.persistent)
                    debug = 2;
                return true;
            }
            debug = 3;
            return CommunalHelperModule.Session.CassetteJumpFix;
        }
        catch (System.NullReferenceException e)
        {
            throw new System.NullReferenceException($"CommunalHelper: Encountered NRE at {debug} in {nameof(MustApply)}", e);
        }
    }


    public static void Load()
    {
        On.Celeste.CassetteBlock.ShiftSize += CassetteBlock_ShiftSize;
    }

    public static void Unload()
    {
        On.Celeste.CassetteBlock.ShiftSize -= CassetteBlock_ShiftSize;
    }

    private static void CassetteBlock_ShiftSize(On.Celeste.CassetteBlock.orig_ShiftSize orig, CassetteBlock self, int amount)
    {
        if (MustApply(self.Scene))
        {
            self.MoveV(amount, 0f);
            DynamicData data = DynamicData.For(self);
            data.Set("blockHeight", data.Get<int>("blockHeight") - amount);
        }
        else
            orig(self, amount);
    }
}
