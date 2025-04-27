using Celeste.Mod.CommunalHelper.States;
using static Celeste.Mod.CommunalHelper.States.Elytra;

namespace Celeste.Mod.CommunalHelper.Triggers;

[CustomEntity("CommunalHelper/ConfigureElytraTrigger")]
public class ConfigureElytraTrigger : Trigger
{
    private readonly bool allow, infinite;

    private readonly ElytraConfiguration options;

    public ConfigureElytraTrigger(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        allow = data.Bool("allow", false);
        infinite = data.Bool("infinite", false);

        options = new ElytraConfiguration()
        {
            disableReverseVerticalMomentum = data.Bool("disableReverseVerticalMomentum"),
        };
    }

    public override void OnEnter(Player player)
    {
        CommunalHelperModule.Session.CanDeployElytra = allow;
        CommunalHelperModule.Session.CurrentElytraConfiguration = options;
        player.SetInfiniteElytra(infinite);
    }
}
