using Celeste.Mod.CommunalHelper.Entities;
using System.Collections;

namespace Celeste.Mod.CommunalHelper.Components;

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
}