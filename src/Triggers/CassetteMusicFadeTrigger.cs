using Celeste.Mod.Entities;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.CommunalHelper.Triggers;

[CustomEntity("CommunalHelper/CassetteMusicFadeTrigger")]
public class CassetteMusicFadeTrigger : MusicFadeTrigger
{
    public CassetteMusicFadeTrigger(EntityData data, Vector2 offset) : base(data, offset) { }

    public override void OnStay(Player player)
    {
        float value = LeftToRight ? Calc.ClampedMap(player.Center.X, Left, Right, FadeA, FadeB) : Calc.ClampedMap(player.Center.Y, Top, Bottom, FadeA, FadeB);

        CassetteBlockManager manager = Scene.Tracker.GetEntity<CassetteBlockManager>();
        if (manager is not null)
        {
            EventInstance sfx = DynamicData.For(manager).Get<EventInstance>("sfx");
            sfx?.setParameterValue(string.IsNullOrEmpty(Parameter) ? "fade" : Parameter, value);
        }
    }
}
