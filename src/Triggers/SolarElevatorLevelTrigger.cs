using Celeste.Mod.CommunalHelper.Entities.StrawberryJam;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Triggers;

[CustomEntity("CommunalHelper/SolarElevatorLevelTrigger")]
public class SolarElevatorLevelTrigger : Trigger
{
    private readonly int id;
    private readonly SolarElevator.StartPosition position;

    public SolarElevatorLevelTrigger(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        id = data.Int("elevatorID", 0);
        position = data.Enum("position", SolarElevator.StartPosition.Closest);
    }

    public override void OnEnter(Player player)
        => Scene.Tracker.GetEntities<SolarElevator>()
                        .Cast<SolarElevator>()
                        .FirstOrDefault(se => se.ID.ID == id)?
                        .SetLevel(position);
}
