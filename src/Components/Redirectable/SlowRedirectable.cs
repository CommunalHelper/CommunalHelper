using MonoMod.Utils;

namespace Celeste.Mod.CommunalHelper.Components;

public abstract class SlowRedirectable : Redirectable
{
    public SlowRedirectable(DynamicData Data) : base(Data) { }

    public abstract void OnPause(Coroutine moveCoroutine, float shakeTime);
    public abstract void OnResume(Coroutine moveCoroutine);

    public abstract void BeforeBreakEffect();
    public abstract void OnRedirectEffect(float shakeTime);
}
