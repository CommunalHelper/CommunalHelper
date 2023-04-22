namespace Celeste.Mod.CommunalHelper.Entities.StrawberryJam;

// This controller enables a music parameter when the player is off the ground for a certain amount of time.
// The parameter is turned off when the player lands.
[CustomEntity("CommunalHelper/SJ/AirTimeMusicController")]
public class AirTimeMusicController : Entity
{
    private readonly float airtimeThreshold;
    private readonly string param;

    private float lastGroundTime;
    private Player player;

    public AirTimeMusicController(EntityData data, Vector2 offset)
        : base(data.Position + offset)
    {
        airtimeThreshold = data.Float("activationThreshold");
        param = data.Attr("musicParam", "");
    }

    public override void Update()
    {
        base.Update();

        player ??= Scene.Tracker.GetEntity<Player>();
        if (player is null)
            return;

        if (player.OnSafeGround)
            lastGroundTime = Scene.TimeActive;

        Audio.SetMusicParam(param, Scene.TimeActive - lastGroundTime > airtimeThreshold ? 1 : 0);
    }
}
