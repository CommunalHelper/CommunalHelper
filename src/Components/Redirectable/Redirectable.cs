using MonoMod.Utils;
using Directions = Celeste.MoveBlock.Directions;

namespace Celeste.Mod.CommunalHelper.Components;

[Tracked(true)]
public abstract class Redirectable : Component
{
    public DynamicData Data;
    public bool IsRedirectable { get; set; } = true;

    public Redirectable(DynamicData Data) : base(false, false)
    {
        this.Data = Data;
    }

    public abstract float Speed { get; set; }
    public abstract float TargetSpeed { get; set; }
    public abstract Directions Direction { get; set; }
    public abstract float Angle { get; set; }
    public abstract bool CanSteer { get; }

    public abstract void ResetBlock();
    public abstract void MoveTo(Vector2 to);
    public abstract void OnBreak(Coroutine moveCoroutine);
}
