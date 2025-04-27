using MonoMod.Cil;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/UnderwaterMusicController")]
[Tracked]
public class UnderwaterMusicController : Entity
{
    public bool Enable;
    public bool DashSFX;
    public string Flag;

    public UnderwaterMusicController(EntityData data, Vector2 _)
    {
        Enable = data.Bool("enable");
        DashSFX = data.Bool("dashSFX");
        Flag = data.Attr("flag", "");
    }

    internal static void Load()
    {
        On.Celeste.Player.UnderwaterMusicCheck += Player_UnderwaterMusicCheck;
        IL.Celeste.Player.CallDashEvents += Player_CallDashEvents;
    }

    internal static void Unload()
    {
        On.Celeste.Player.UnderwaterMusicCheck -= Player_UnderwaterMusicCheck;
        IL.Celeste.Player.CallDashEvents -= Player_CallDashEvents;
    }

    public bool CheckEnabled()
    {
        return Enable && (string.IsNullOrWhiteSpace(Flag) || SceneAs<Level>().Session.GetFlag(Flag));
    }

    private static bool Player_UnderwaterMusicCheck(On.Celeste.Player.orig_UnderwaterMusicCheck orig, Player self)
    {
        return self.SceneAs<Level>().Tracker.GetEntity<UnderwaterMusicController>()?.CheckEnabled() ?? orig(self);
    }

    private static void Player_CallDashEvents(ILContext il)
    {
        ILCursor cursor = new(il);
        if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<Player>("SwimCheck")))
        {
            cursor.EmitDelegate<Func<bool, bool>>(orig =>
            {
                UnderwaterMusicController controller = (Engine.Scene as Level)?.Tracker?.GetEntity<UnderwaterMusicController>();
                return (controller is null || !controller.DashSFX) ? orig : controller.CheckEnabled();
            });
        }
    }

}
