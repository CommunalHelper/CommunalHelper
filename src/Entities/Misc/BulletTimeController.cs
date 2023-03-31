namespace Celeste.Mod.CommunalHelper.Entities.Misc;

[CustomEntity("CommunalHelper/BulletTimeController")]
public class BulletTimeController : Entity
{
    float timerate;
    string flag;
    bool active;

    public BulletTimeController(EntityData data, Vector2 offset) : base(data.Position + offset)
    {
        Tag = Tags.PauseUpdate;
        timerate = data.Float("speed");
        flag = data.Attr("flag");
    }

    public override void Update()
    {
        base.Update();

        Player player = Scene.Tracker.GetEntity<Player>();
        if (player is { IsIntroState: false, JustRespawned: false, Dashes: > 0 } && (string.IsNullOrEmpty(flag) || SceneAs<Level>().Session.GetFlag(flag)))
        {
            Engine.TimeRate = Scene.Paused ? 1 : timerate;
            active = true;
        }
        else if (active)
        {
            Engine.TimeRate = 1;
            active = false;
        }
    }
}
