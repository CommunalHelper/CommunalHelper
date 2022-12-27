using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System.Linq;
using static Celeste.Session;

namespace Celeste.Mod.CommunalHelper.Entities.Misc;

[CustomEntity("CommunalHelper/CoreModeMusicController")]
[Tracked]
public class CoreModeMusicController : Entity
{
    private readonly string[] parameters;
    private readonly float hot, cold, none;
    private readonly bool disable;
    private CoreModes? previousCoreMode;

    public CoreModeMusicController(EntityData data, Vector2 _)
    {
        parameters = data.Attr("params").Split(',');
        hot = data.Float("hot", 1.0f);
        cold = data.Float("cold", 0.0f);
        none = data.Float("none", 0.0f);

        if (!(disable = data.Bool("disable")))
            Tag = Tags.TransitionUpdate | Tags.Persistent;

        Visible = false;
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);
        if (disable)
        {
            foreach (CoreModeMusicController controller in Scene.Tracker.GetEntities<CoreModeMusicController>())
                if (!controller.disable && controller.parameters.SequenceEqual(parameters))
                    controller.RemoveSelf();
            RemoveSelf();
        }
    }

    public override void Update()
    {
        base.Update();
        CoreModes current = SceneAs<Level>().CoreMode;
        if (previousCoreMode != current)
        {
            float value = current switch
            {
                CoreModes.Hot => hot,
                CoreModes.Cold => cold,
                _ => none,
            };
            foreach (string param in parameters)
                Audio.SetMusicParam(param, value);
            previousCoreMode = current;
        }
    }
}
