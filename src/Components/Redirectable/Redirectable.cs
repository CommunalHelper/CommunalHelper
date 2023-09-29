using Directions = Celeste.MoveBlock.Directions;

namespace Celeste.Mod.CommunalHelper.Components;

[Tracked(true)]
public abstract class Redirectable : Component
{
    public Redirectable() : base(false, false) { }

    public bool CanRedirect { get; set; } = true;
    public abstract float Speed { get; set; }
    public abstract float TargetSpeed { get; set; }
    public abstract Directions Direction { get; set; }
    public abstract float Angle { get; set; }
    public abstract bool CanSteer { get; }

    public Directions InitialDirection;
    public float InitialAngle;

    public abstract void MoveTo(Vector2 to);
    public abstract void OnPause(Coroutine moveCoroutine, float shakeTime);
    public abstract void OnResume(Coroutine moveCoroutine);
    public abstract void OnBreak(Coroutine moveCoroutine);

    public abstract void BeforeBreakEffect();
    public abstract void OnRedirectEffect(float shakeTime);


    private bool boolInitialized = false;

    protected abstract float GetInitialAngle();
    protected abstract Directions GetInitialDirection();
    public void InitializeInitialValues()
    {
        if (!boolInitialized)
        {
            InitialDirection = GetInitialDirection();
            InitialAngle = GetInitialAngle();
            boolInitialized = true;
        }
    }
}
