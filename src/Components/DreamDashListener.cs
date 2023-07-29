namespace Celeste.Mod.CommunalHelper.Components;

[Tracked(false)]
public class DreamDashListener : Component
{
    public Action<Vector2> OnDreamDash;

    public DreamDashListener()
        : base(active: false, visible: false)
    { }

    // The least decompiled and reused code from snowii
    public DreamDashListener(Action<Vector2> onDreamDash)
        : base(active: false, visible: false)
    {
        OnDreamDash = onDreamDash;
    }

    #region Hooks

    // After learning about regions from reading some code here, this is the most useful thing I've ever found for organizing code
    // GOD I LOVE C#

    public static void Load()
    {
        On.Celeste.Player.DreamDashBegin += hook_DreamDashBegin;
    }

    public static void Unload()
    {
        On.Celeste.Player.DreamDashBegin -= hook_DreamDashBegin;
    }

    public static void hook_DreamDashBegin(On.Celeste.Player.orig_DreamDashBegin orig, Player self)
    {
        orig(self);

        // Handles DreamDashListeners whenever you enter the DreamDash State
        foreach (DreamDashListener component in self.Scene.Tracker.GetComponents<DreamDashListener>())
            component.OnDreamDash?.Invoke(self.DashDir);
        return;
    }

    #endregion
}
