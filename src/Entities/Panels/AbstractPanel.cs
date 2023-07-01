using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System.Collections.Generic;
using System.Reflection;
using Directions = Celeste.Spikes.Directions;

namespace Celeste.Mod.CommunalHelper.Entities;

[Tracked(true)]
public abstract class AbstractPanel : Entity
{
    protected static int GetSize(EntityData data, Directions dir)
    {
        return dir <= Directions.Down ? data.Width : data.Height;
    }

    public Directions Orientation;

    private readonly bool overrideAllowStaticMovers;

    protected StaticMover staticMover;

    protected int? surfaceSoundIndex = null;

    public Vector2 Start => new(
            X + (Orientation is Directions.Right or Directions.Down ? Width : 0),
            Y + (Orientation is Directions.Left or Directions.Down ? Height : 0));
    public Vector2 End => new(
            X + (Orientation is Directions.Up or Directions.Right ? Width : 0),
            Y + (Orientation is Directions.Right or Directions.Down ? Height : 0));

    protected Vector2 platformShake;

    public float Alpha = 1f;

    protected Level level;

    public AbstractPanel(EntityData data, Vector2 offset)
        : this(data.Position + offset, GetSize(data, data.Enum<Directions>("orientation")), data.Enum<Directions>("orientation"), data.Bool("overrideAllowStaticMovers")) { }

    public AbstractPanel(Vector2 position, float size, Directions orientation, bool overrideAllowStaticMovers)
        : base(position)
    {
        Depth = Depths.FakeWalls;
        Orientation = orientation;
        this.overrideAllowStaticMovers = overrideAllowStaticMovers;

        Collider = orientation switch
        {
            Directions.Up or Directions.Down => new Hitbox(size, 8f),
            Directions.Left or Directions.Right => new Hitbox(8f, size),
            _ => null
        };

        Add(staticMover = new StaticMover
        {
            OnAttach = OnAttach,
            OnShake = v => platformShake += v,
            SolidChecker = IsRiding,
            OnEnable = () => Active = Visible = Collidable = true,
            OnDisable = () => Active = Visible = Collidable = false,
            OnDestroy = Destroy
        });
    }

    protected virtual void OnAttach(Platform platform)
    {
        platform.OnDashCollide = DelegateHelper.ApplyDashCollisionHook(platform.OnDashCollide, OnDashCollide);
    }

    /// <summary>
    /// Does not check dash direction or direct collision with the Panel.
    /// Use CheckDashCollision as needed.
    /// </summary>
    protected virtual DashCollisionResults OnDashCollide(DashCollision orig, Player player, Vector2 dir)
    {
        return orig(player, dir);
    }

    protected bool CheckDashCollision(Player player, Vector2 dir)
    {
        switch (Orientation)
        {
            case Directions.Up when dir.Y > 0:
            case Directions.Down when dir.Y < 0:
            case Directions.Left when dir.X > 0:
            case Directions.Right when dir.X < 0:
                return player.CollideCheck(this, player.Position + dir);
        }
        return false;
    }

    // Make sure at least one side aligns, and the rest are contained within the solid
    private bool IsRiding(Solid solid)
    {
        return Orientation switch
        {
            Directions.Up => this.CollideCheckOutsideInside(solid, TopCenter - (Vector2.UnitY * Height)),
            Directions.Down => this.CollideCheckOutsideInside(solid, BottomCenter + Vector2.UnitY),
            Directions.Left => this.CollideCheckOutsideInside(solid, CenterLeft - (Vector2.UnitX * Width)),
            Directions.Right => this.CollideCheckOutsideInside(solid, CenterRight + Vector2.UnitX),
            _ => false,
        };
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);
        level = scene as Level;
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);

        if (overrideAllowStaticMovers && staticMover.Platform is null)
        {
            foreach (Entity entity in scene.GetEntitiesByTagMask(Tags.Global | Tags.Persistent))
            {
                if (entity is Solid solid && entity.Scene == scene)
                {
                    ForceAttachStaticMovers(solid, scene);
                }
            }
        }
    }

    protected virtual void Destroy()
    {
        Collidable = false;
        Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.Linear, 1, true);
        tween.OnUpdate = t => Alpha = 1 - t.Percent;
        tween.OnComplete = delegate { RemoveSelf(); };
        Add(tween);
    }

    public override void Update()
    {
        if (staticMover.Platform == null)
        {
            RemoveSelf();
            return;
        }

        base.Update();
    }

    public override void Removed(Scene scene)
    {
        if (staticMover.Platform is not null && (staticMover.Platform.TagCheck(Tags.Global) || staticMover.Platform.TagCheck(Tags.Persistent)))
            DelegateHelper.RemoveDashCollisionHook(staticMover.Platform.OnDashCollide, OnDashCollide);

        base.Removed(scene);
    }

    #region Hooks

    private static readonly List<IDetour> hook_Platform_GetLandOrStepSoundIndex = new();
    private static readonly List<IDetour> hook_Platform_GetWallSoundIndex = new();

    internal static void Load()
    {
        On.Celeste.Solid.Awake += Solid_Awake;

        On.Celeste.DashBlock.Break_Vector2_Vector2_bool_bool += DashBlock_Break_Vector2_Vector2_bool_bool;

        DreamTunnelEntry.Load();
        FrictionlessPanel.Load();
        BouncyPanel.Load();
    }

    private static readonly HashSet<string> hooked = new();

    private static void LogHooked(string fullname)
    {
        if (!hooked.Contains(fullname))
        {
            Logger.Log(LogLevel.Debug, "CommunalHelper", $"Hooking {fullname} to override when AbstractPanel present.");
            hooked.Add(fullname);
        }
    }

    internal static void LoadDelayed()
    {
        // Land and Step sound are identical
        MethodInfo Platform_GetLandOrStepSoundIndex = typeof(AbstractPanel).GetMethod(nameof(AbstractPanel.Platform_GetLandOrStepSoundIndex), BindingFlags.NonPublic | BindingFlags.Static);
        foreach (MethodInfo method in typeof(Platform).GetMethod("GetLandSoundIndex").GetOverrides(true))
        {
            LogHooked(method.GetFullName());
            hook_Platform_GetLandOrStepSoundIndex.Add(
                new Hook(method, Platform_GetLandOrStepSoundIndex)
            );
        }
        foreach (MethodInfo method in typeof(Platform).GetMethod("GetStepSoundIndex").GetOverrides(true))
        {
            LogHooked(method.GetFullName());
            hook_Platform_GetLandOrStepSoundIndex.Add(
                new Hook(method, Platform_GetLandOrStepSoundIndex)
            );
        }
        MethodInfo Platform_GetWallSoundIndex = typeof(AbstractPanel).GetMethod(nameof(AbstractPanel.Platform_GetWallSoundIndex), BindingFlags.NonPublic | BindingFlags.Static);
        foreach (MethodInfo method in typeof(Platform).GetMethod("GetWallSoundIndex").GetOverrides(true))
        {
            LogHooked(method.GetFullName());
            hook_Platform_GetWallSoundIndex.Add(
                new Hook(method, Platform_GetWallSoundIndex)
            );
        }
    }

    internal static void Unload()
    {
        hook_Platform_GetLandOrStepSoundIndex.ForEach(h => h.Dispose());
        hook_Platform_GetWallSoundIndex.ForEach(h => h.Dispose());

        On.Celeste.Solid.Awake -= Solid_Awake;

        On.Celeste.DashBlock.Break_Vector2_Vector2_bool_bool -= DashBlock_Break_Vector2_Vector2_bool_bool;

        DreamTunnelEntry.Unload();
        FrictionlessPanel.Unload();
        BouncyPanel.Unload();
    }

    private static int Platform_GetLandOrStepSoundIndex(Func<Platform, Entity, int> orig, Platform self, Entity entity)
    {
        foreach (StaticMover sm in DynamicData.For(self).Get<List<StaticMover>>("staticMovers"))
        {
            if (sm.Entity is AbstractPanel panel && panel.surfaceSoundIndex != null && panel.Orientation == Directions.Up && entity.CollideCheck(panel, entity.Position + Vector2.UnitY))
            {
                return (int) panel.surfaceSoundIndex;
            }
        }
        return orig(self, entity);
    }

    private static int Platform_GetWallSoundIndex(Func<Platform, Player, int, int> orig, Platform self, Player player, int side)
    {
        foreach (StaticMover sm in DynamicData.For(self).Get<List<StaticMover>>("staticMovers"))
        {
            if (sm.Entity is AbstractPanel panel && panel.surfaceSoundIndex != null)
            {
                if (side == (int) Facings.Left && panel.Orientation == Directions.Right && player.CollideCheck(panel, player.Position - Vector2.UnitX))
                {
                    return (int) panel.surfaceSoundIndex;
                }
                if (side == (int) Facings.Right && panel.Orientation == Directions.Left && player.CollideCheck(panel, player.Position + Vector2.UnitX))
                {
                    return (int) panel.surfaceSoundIndex;
                }
            }
        }
        return orig(self, player, side);
    }

    private static void Solid_Awake(On.Celeste.Solid.orig_Awake orig, Solid self, Scene scene)
    {
        orig(self, scene);

        if (!self.AllowStaticMovers)
            ForceAttachStaticMovers(self, scene);
    }

    private static void ForceAttachStaticMovers(Solid solid, Scene scene)
    {
        bool collidable = solid.Collidable;
        solid.Collidable = true;
        DynamicData solidData = null;
        foreach (AbstractPanel panel in scene.Tracker.GetEntities<AbstractPanel>())
        {
            StaticMover staticMover = panel.staticMover;
            if (panel.overrideAllowStaticMovers && staticMover.Platform == null && staticMover.IsRiding(solid))
            {
                solidData ??= DynamicData.For(solid);
                solidData.Get<List<StaticMover>>("staticMovers").Add(staticMover);
                staticMover.Platform = solid;
                staticMover.OnAttach?.Invoke(solid);
            }
        }
        solid.Collidable = collidable;
    }

    private static readonly FieldInfo f_Platform_staticMovers = typeof(Platform).GetField("staticMovers", BindingFlags.NonPublic | BindingFlags.Instance);

    private static void DashBlock_Break_Vector2_Vector2_bool_bool(On.Celeste.DashBlock.orig_Break_Vector2_Vector2_bool_bool orig, DashBlock self, Vector2 from, Vector2 direction, bool playSound, bool playDebrisSound)
    {
        orig(self, from, direction, playSound, playDebrisSound);
        List<StaticMover> staticMovers = (List<StaticMover>) f_Platform_staticMovers.GetValue(self);
        foreach (StaticMover mover in staticMovers)
        {
            if (mover.Entity is AbstractPanel)
                mover.OnDestroy();
        }
    }

    #endregion

}
