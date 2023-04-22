namespace Celeste.Mod.CommunalHelper.Entities.StrawberryJam;

[CustomEntity("CommunalHelper/SJ/BulletTimeController")]
public class BulletTimeController : Entity
{
    float timerate;
    string flag;
    bool active;
    int minDashes;

    public BulletTimeController(EntityData data, Vector2 offset) : base(data.Position + offset)
    {
        Tag = Tags.PauseUpdate;
        timerate = data.Float("timerate", 1);
        flag = data.Attr("flag");
        minDashes = data.Int("minDashes", 1);
    }

    public override void Update()
    {
        base.Update();

        Player player = Scene.Tracker.GetEntity<Player>();
        if (player is { IsIntroState: false, JustRespawned: false } && player.Dashes >= minDashes && (string.IsNullOrEmpty(flag) || SceneAs<Level>().Session.GetFlag(flag)))
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
