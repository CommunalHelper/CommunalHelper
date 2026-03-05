using Celeste.Mod.CommunalHelper.Components;

namespace Celeste.Mod.CommunalHelper.DashStates;

[CustomEntity("CommunalHelper/DashStateTrigger")]
public class DashStateTrigger : Trigger
{
    private enum LegacyModes
    {
        OneUse = -1,
        Trigger = 0,
        Field = 1
    }

    private enum Modes
    {
        Enable,
        Disable,
        Field
    }
    private readonly Modes mode;
    private readonly bool oneUse;
    
    private readonly DashStates dashState;

    public DashStateTrigger(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        if (data.Has("mode"))
        {
            LegacyModes legacyMode = data.Enum("mode", LegacyModes.Trigger);
            (mode, oneUse) = legacyMode switch
            {
                LegacyModes.OneUse => (Modes.Enable, true),
                LegacyModes.Trigger => (Modes.Enable, false),
                LegacyModes.Field => (Modes.Field, false),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        else
        {
            mode = data.Enum("triggerMode", Modes.Enable);
            oneUse = data.Bool("oneUse");
        }
        
        dashState = data.Enum("dashState", DashStates.DreamTunnelDash);

        string flag = data.Attr("flag");
        if (!string.IsNullOrEmpty(flag))
            Add(new FlagToggleComponent(flag, data.Bool("flagInverted")));
    }
    
    public override void OnEnter(Player player)
    {
        base.OnEnter(player);
        
        dashState.SetEnabled(mode != Modes.Disable);
        if (oneUse)
            RemoveSelf();
    }

    public override void OnStay(Player player)
    {
        base.OnStay(player);
        
        if (mode == Modes.Field)
            dashState.SetEnabled(true);
    }

    public override void OnLeave(Player player)
    {
        base.OnLeave(player);
        
        if (mode == Modes.Field)
            dashState.SetEnabled(false);
    }
}
