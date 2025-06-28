namespace Celeste.Mod.CommunalHelper.Components;

public class FlagToggleComponent : Component
{
    public bool Enabled = true;
    
    private readonly string flag;
    private readonly Action onDisable;
    private readonly Action onEnable;
    private readonly bool inverted;

    public FlagToggleComponent(string flag, bool inverted, Action onDisable = null, Action onEnable = null) : base(true, false)
    {
        this.flag = flag;
        this.inverted = inverted;
        this.onDisable = onDisable;
        this.onEnable = onEnable;
    }

    public override void EntityAdded(Scene scene)
    {
        base.EntityAdded(scene);
        UpdateFlag();
    }

    public override void Update()
    {
        base.Update();
        UpdateFlag();
    }

    private void UpdateFlag()
    {
        if ((inverted || SceneAs<Level>().Session.GetFlag(flag) == Enabled)
            && (!inverted || SceneAs<Level>().Session.GetFlag(flag) != Enabled))
            return;
        
        if (Enabled)
        {
            // disable the entity.
            Entity.Visible = Entity.Collidable = false;
            onDisable?.Invoke();
            Enabled = false;
        }
        else
        {
            // enable the entity.
            Entity.Visible = Entity.Collidable = true;
            onEnable?.Invoke();
            Enabled = true;
        }
    }
}
