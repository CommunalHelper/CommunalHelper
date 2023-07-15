using Celeste.Mod.CommunalHelper.States;

namespace Celeste.Mod.CommunalHelper.Triggers;

[CustomEntity("CommunalHelper/ConfigureElytraTrigger")]
public class ConfigureElytraTrigger : Trigger
{
    private readonly bool allow, infinite;

    public ConfigureElytraTrigger(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        allow = data.Bool("allow", false);
        infinite = data.Bool("infinite", false);
    }

    public override void OnEnter(Player player)
    {
        CommunalHelperModule.Session.CanDeployElytra = allow;
        player.SetInfiniteElytra(infinite);
    }
}
