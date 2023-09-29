using MonoMod.Utils;
using Directions = Celeste.MoveBlock.Directions;

namespace Celeste.Mod.CommunalHelper.Components;

[Tracked(true)]
public abstract class Redirectable : Component
{
    public Redirectable(DynamicData Data) : base(false, false)
    {
        this.Data = Data;
    }

    public bool IsRedirectable { get; set; } = true;
    public abstract float Speed { get; set; }
    public abstract float TargetSpeed { get; set; }
    public abstract Directions Direction { get; set; }
    public abstract float Angle { get; set; }
    public abstract bool CanSteer { get; }

    public Directions InitialDirection;
    public float InitialAngle;
    public DynamicData Data;

    public abstract void MoveTo(Vector2 to);
    public abstract void OnBreak(Coroutine moveCoroutine);


    private bool initialized = false;
    protected abstract float GetInitialAngle();
    protected abstract Directions GetInitialDirection();
    public void SaveInitialValues()
    {
        if (!initialized)
        {
            InitialDirection = GetInitialDirection();
            InitialAngle = GetInitialAngle();
            initialized = true;
        }
    }
}
