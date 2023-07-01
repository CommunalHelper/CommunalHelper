using MonoMod;
using MonoMod.Utils;
using System.Collections;

namespace Celeste.Mod.CommunalHelper.Entities;

/// <summary>
/// A pseudo-<see cref="DreamBlock"/> class that can be used as a listener for DreamBlock Activation/Deactivation.
/// </summary>
[TrackedAs(typeof(DreamBlock))] // Track it as a DreamBlock for the ActivateDreamBlocksTrigger
[Tracked] // But also track it on it's own as a utility entity
public class DreamBlockDummy : DreamBlock
{
    public Entity Entity;

    public bool PlayerHasDreamDash => Data.Get<bool>("playerHasDreamDash");

    public Func<IEnumerator> OnActivate;
    public Func<IEnumerator> OnFastActivate;
    public Action OnActivateNoRoutine;

    public Func<IEnumerator> OnDeactivate;
    public Func<IEnumerator> OnFastDeactivate;
    public Action OnDeactivateNoRoutine;

    public Action OnSetup;

    public DynamicData Data;

    public DreamBlockDummy(Entity entity)
        : base(Vector2.Zero, 0, 0, null, false, false)
    {
        Collidable = Active = Visible = false;
        Entity = entity;

        Data = new(typeof(DreamBlock), this);
    }

    [MonoModLinkTo("Monocle.Entity", "System.Void Added(Monocle.Scene)")]
    public void base_Added(Scene scene)
    {
        base.Added(scene);
    }

    public override void Added(Scene scene)
    {
        base_Added(scene);
        Data.Set("playerHasDreamDash", SceneAs<Level>().Session.Inventory.DreamDash);
    }

    public override void Update() { }

    public override void Render() { }

    #region Hooks

    internal static void Load()
    {
        On.Celeste.DreamBlock.Activate += DreamBlock_Activate;
        On.Celeste.DreamBlock.FastActivate += DreamBlock_FastActivate;
        On.Celeste.DreamBlock.ActivateNoRoutine += DreamBlock_ActivateNoRoutine;

        On.Celeste.DreamBlock.Deactivate += DreamBlock_Deactivate;
        On.Celeste.DreamBlock.FastDeactivate += DreamBlock_FastDeactivate;
        On.Celeste.DreamBlock.DeactivateNoRoutine += DreamBlock_DeactivateNoRoutine;

        On.Celeste.DreamBlock.Setup += DreamBlock_Setup;
    }

    internal static void Unload()
    {
        On.Celeste.DreamBlock.Activate -= DreamBlock_Activate;
        On.Celeste.DreamBlock.FastActivate -= DreamBlock_FastActivate;
        On.Celeste.DreamBlock.ActivateNoRoutine -= DreamBlock_ActivateNoRoutine;

        On.Celeste.DreamBlock.Deactivate -= DreamBlock_Deactivate;
        On.Celeste.DreamBlock.FastDeactivate -= DreamBlock_FastDeactivate;
        On.Celeste.DreamBlock.DeactivateNoRoutine -= DreamBlock_DeactivateNoRoutine;

        On.Celeste.DreamBlock.Setup -= DreamBlock_Setup;
    }

    private static IEnumerator DreamBlock_Activate(On.Celeste.DreamBlock.orig_Activate orig, DreamBlock self)
    {
        if (self is DreamBlockDummy dummy && dummy.OnActivate != null)
        {
            dummy.Data.Set("playerHasDreamDash", true);
            dummy.Entity.Add(new Coroutine(dummy.OnActivate()));
            return null;
        }
        return orig(self);
    }

    private static IEnumerator DreamBlock_FastActivate(On.Celeste.DreamBlock.orig_FastActivate orig, DreamBlock self)
    {
        if (self is DreamBlockDummy dummy && dummy.OnFastActivate != null)
        {
            dummy.Data.Set("playerHasDreamDash", true);
            dummy.Entity.Add(new Coroutine(dummy.OnFastActivate()));
            return null;
        }
        return orig(self);
    }

    private static void DreamBlock_ActivateNoRoutine(On.Celeste.DreamBlock.orig_ActivateNoRoutine orig, DreamBlock self)
    {
        if (self is DreamBlockDummy dummy && dummy.OnActivateNoRoutine != null)
        {
            dummy.Data.Set("playerHasDreamDash", true);
            dummy.OnActivateNoRoutine();
            return;
        }
        orig(self);
    }

    private static IEnumerator DreamBlock_Deactivate(On.Celeste.DreamBlock.orig_Deactivate orig, DreamBlock self)
    {
        if (self is DreamBlockDummy dummy && dummy.OnDeactivate != null)
        {
            dummy.Data.Set("playerHasDreamDash", false);
            dummy.Entity.Add(new Coroutine(dummy.OnDeactivate()));
            return null;
        }
        return orig(self);
    }

    private static IEnumerator DreamBlock_FastDeactivate(On.Celeste.DreamBlock.orig_FastDeactivate orig, DreamBlock self)
    {
        if (self is DreamBlockDummy dummy && dummy.OnFastDeactivate != null)
        {
            dummy.Data.Set("playerHasDreamDash", false);
            dummy.Entity.Add(new Coroutine(dummy.OnFastDeactivate()));
            return null;
        }
        return orig(self);
    }

    private static void DreamBlock_DeactivateNoRoutine(On.Celeste.DreamBlock.orig_DeactivateNoRoutine orig, DreamBlock self)
    {
        if (self is DreamBlockDummy dummy && dummy.OnDeactivateNoRoutine != null)
        {
            dummy.Data.Set("playerHasDreamDash", false);
            dummy.OnDeactivateNoRoutine();
            return;
        }
        orig(self);
    }

    private static void DreamBlock_Setup(On.Celeste.DreamBlock.orig_Setup orig, DreamBlock self)
    {
        if (self is DreamBlockDummy dummy && dummy.OnSetup != null)
        {
            dummy.OnSetup();
            return;
        }
        orig(self);
    }

    #endregion

}
