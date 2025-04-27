using MonoMod.Utils;

namespace Celeste.Mod.CommunalHelper.Entities.StrawberryJam;

[TrackedAs(typeof(CassetteListener), false)]
public class TickingCassetteListener : CassetteListener
{
    public delegate void OnTickDelegate(CassetteBlockManager cbm, bool isSwap);
    
    // "new" keyword is in case this functionality gets added to the base class in Everest
    public new OnTickDelegate OnTick;

    private int lastTickIndex;
    
    public TickingCassetteListener(int index, float tempo = 1) : base(index, tempo)
    {
        Active = true;
    }

    public override void Update()
    {
        if (SceneAs<Level>()?.Tracker.GetEntity<CassetteBlockManager>() is not { } cassetteBlockManager) return;
        int beatIndex = cassetteBlockManager.beatIndex;
        var data = DynamicData.For(cassetteBlockManager);
        int beatsPerTick = data.Get<int>("beatsPerTick");
        int ticksPerSwap = data.Get<int>("ticksPerSwap");
        int thisTick = beatIndex % (beatsPerTick * ticksPerSwap);
        if (thisTick == lastTickIndex) return;

        lastTickIndex = thisTick;
        if (beatIndex % beatsPerTick == 0) {
            OnTick?.Invoke(cassetteBlockManager, thisTick == 0);
        }
    }
}
