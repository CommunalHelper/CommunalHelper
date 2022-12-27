using System.Linq;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/InputFlagController")]
public class InputFlagController : AbstractInputController
{
    public bool Activated => OverrideBinding ? Input.Grab.Pressed :
        CommunalHelperModule.Settings.ActivateFlagController.Pressed;

    public string[][] Flags;
    private int flagIndex = 0;

    public bool Toggle;

    public bool ResetFlags;

    public float Delay;
    private float cooldown;

    public bool OverrideBinding;

    public InputFlagController(EntityData data, Vector2 _)
    {
        Flags = data.Attr("flags").Split(';').Select(str => str.Split(',')).ToArray();
        Toggle = data.Bool("toggle", true);
        ResetFlags = data.Bool("resetFlags");
        Delay = data.Float("delay");
        OverrideBinding = data.Bool("grabOverride");
    }

    public override void Update()
    {
        base.Update();

        if (cooldown > 0)
            cooldown -= Engine.DeltaTime;
        else if (Activated)
            Activate();
    }

    public override void FrozenUpdate()
    {
        if (cooldown <= 0 && Activated)
        {
            Activate();
        }
    }

    public void Activate()
    {
        if (flagIndex < Flags.Length)
        {
            string[] flagSet = Flags[flagIndex];
            bool value = true;
            foreach (string flag in flagSet)
            {
                if (Toggle)
                    value = !SceneAs<Level>().Session.GetFlag(flag);

                SceneAs<Level>().Session.SetFlag(flag, value);
            }
            flagIndex++;
            if (Toggle && flagIndex >= Flags.Length)
                flagIndex = 0;
        }
        cooldown = Delay;
    }

    public override void Removed(Scene scene)
    {
        base.Removed(scene);
        if (ResetFlags)
        {
            for (int i = 0; i < Flags.Length; i++)
            {
                foreach (string flag in Flags[i])
                {
                    (scene as Level).Session.SetFlag(flag, false);
                }
            }
        }
    }

}
