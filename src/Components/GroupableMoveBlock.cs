using Celeste.Mod.CommunalHelper.Entities;
using System.Collections;

namespace Celeste.Mod.CommunalHelper.Components;

/// <summary>
/// Component used to allow Move Block Groups to contain any move block entity.
/// </summary>
[Tracked]
public class GroupableMoveBlock : Component
{
    public enum MovementState
    {
        Idling,
        Moving,
        Breaking,
    }

    public MovementState State;

    public MoveBlockGroup Group { get; internal set; }
    public bool GroupTriggerSignal { get; internal set; }
    public bool WaitingForRespawn { get; internal set; }

    public GroupableMoveBlock() : base(true, false) { }

    // Both of these need to be SwapImmediately'ed in order to not introduce an extra frame of delay which would desync with previous behaviour.

    public IEnumerator SyncGroupTriggers()
    {
        if (Group is not null && Group.SyncActivation)
        {
            if (!GroupTriggerSignal)
            {
                Group.Trigger(); // block was manually triggered
            }

            // ensures all moveblock in the group start simultaneously
            while (!GroupTriggerSignal) // wait for signal to come back
                yield return null;
            GroupTriggerSignal = false; // reset
        }
    }

    public IEnumerator WaitForRespawn()
    {
        if (Group is not null)
        {
            WaitingForRespawn = true;
            while (!Group.CanRespawn(this))
                yield return null;
        }
    }

    public Color HighlightColor(Color? color = null)
    {
        Color baseColor = color ?? Color.Transparent;
        return Group is null ?
            baseColor :
            Color.Lerp(baseColor, Group.Color, Calc.SineMap(Scene.TimeActive * 3, 0, 1));
    }
}