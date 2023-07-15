using FMOD.Studio;
using MonoMod.Utils;

namespace Celeste.Mod.CommunalHelper.Triggers;

[CustomEntity("CommunalHelper/SoundAreaTrigger")]
public class SoundAreaTrigger : Trigger
{
    private readonly EventInstance eventInstance;
    private float vol = 0f;

    public SoundAreaTrigger(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        Vector2 sourcePos = data.NodesOffset(offset)[0];
        string path = data.Attr("event");

        SoundSource sound = new(sourcePos - Position, path);
        eventInstance = DynamicData.For(sound).Get<EventInstance>("instance");
        Add(sound);
    }

    public override void Update()
    {
        base.Update();

        vol = Calc.Approach(vol, PlayerIsInside ? 1 : 0, Engine.DeltaTime * 2f);
        eventInstance.setVolume(vol);
    }
}
