namespace Celeste.Mod.CommunalHelper.Entities;

public abstract class AeroScreen
{
    public AeroBlock Block { get; internal set; }

    public virtual float Period => 0.0f;
    
    public abstract void Update();
    public abstract void Render();
    public abstract void Finish();
}
