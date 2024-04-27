using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Components;

/// <summary>
/// Calculates the collider for the beam part of a laser emitter.
/// </summary>
/// <remarks>
/// The size of the laser hitbox is calculated using a binary search algorithm for performance.
/// </remarks>
public class LaserColliderComponent : Component
{
    public float Thickness { get; set; }
    public bool CollideWithSolids { get; set; }
    public Hitbox Collider { get; } = new Hitbox(0, 0);
    public Vector2 Offset { get; set; }
    public bool CollidedWithScreenBounds { get; private set; }
    public LaserOrientations Orientation { get; set; }

    public LaserColliderComponent() : this(Vector2.Zero)
    {
    }

    public LaserColliderComponent(Vector2 offset) : base(true, false)
    {
        Offset = offset;
    }

    public override void EntityAdded(Scene scene)
    {
        base.EntityAdded(scene);
        UpdateBeam(true);
    }

    public override void EntityAwake()
    {
        base.EntityAwake();
        UpdateBeam(true);
    }

    public override void Update()
    {
        base.Update();
        UpdateBeam();
    }

    private void ResizeHitbox(float size)
    {
        switch (Orientation)
        {
            case LaserOrientations.Up:
                Collider.Width = Thickness;
                Collider.Height = size;
                Collider.BottomCenter = Offset;
                break;

            case LaserOrientations.Down:
                Collider.Width = Thickness;
                Collider.Height = size;
                Collider.TopCenter = Offset;
                break;

            case LaserOrientations.Left:
                Collider.Width = size;
                Collider.Height = Thickness;
                Collider.CenterRight = Offset;
                break;

            case LaserOrientations.Right:
                Collider.Width = size;
                Collider.Height = Thickness;
                Collider.CenterLeft = Offset;
                break;
        }
    }

    public void UpdateBeam(bool fromEntityAdded = false)
    {
        var level = SceneAs<Level>();

        float high = Orientation switch
        {
            LaserOrientations.Up => Entity.Position.Y + Offset.Y - level.Bounds.Top,
            LaserOrientations.Down => level.Bounds.Bottom - Entity.Position.Y - Offset.Y,
            LaserOrientations.Left => Entity.Position.X + Offset.X - level.Bounds.Left,
            LaserOrientations.Right => level.Bounds.Right - Entity.Position.X - Offset.X,
            _ => 0,
        };

        int low = 0, safety = 1000;

        // force non-collidable invisible barriers to be collidable if our entity was just added
        List<Entity> barriers = null;
        if (fromEntityAdded && CollideWithSolids)
        {
            barriers = level.Tracker.GetEntities<InvisibleBarrier>().Where(ib => !ib.Collidable).ToList();
            barriers.ForEach(ib => ib.Collidable = true);
        }

        // first check if the laser hits the edge of the screen
        ResizeHitbox(high);
        CollidedWithScreenBounds = !CollideWithSolids || !SolidCollideCheck();
        if (!CollidedWithScreenBounds)
        {
            // perform a binary search to hit the nearest solid
            while (safety-- > 0)
            {
                int pivot = (int) (low + (high - low) / 2f);
                ResizeHitbox(pivot);
                if (pivot == low)
                    break;
                if (SolidCollideCheck())
                {
                    high = pivot;
                }
                else
                {
                    low = pivot;
                }
            }
        }

        // reset collidable for those we modified
        barriers?.ForEach(ib => ib.Collidable = false);
    }

    private bool SolidCollideCheck()
    {
        var oldCollider = Entity.Collider;
        Entity.Collider = Collider;
        bool didCollide = Entity.CollideCheck<Solid>();
        Entity.Collider = oldCollider;
        return didCollide;
    }
}

public enum LaserOrientations
{
    Up,
    Down,
    Left,
    Right,
}

public static class LaserOrientationsExtensions
{
    public static Vector2 Direction(this LaserOrientations orientation) => orientation switch
    {
        LaserOrientations.Up => -Vector2.UnitY,
        LaserOrientations.Down => Vector2.UnitY,
        LaserOrientations.Left => -Vector2.UnitX,
        LaserOrientations.Right => Vector2.UnitX,
        _ => Vector2.Zero
    };

    public static float Angle(this LaserOrientations orientation) => orientation switch
    {
        LaserOrientations.Up => 0f,
        LaserOrientations.Down => (float) Math.PI,
        LaserOrientations.Left => (float) -Math.PI / 2f,
        LaserOrientations.Right => (float) Math.PI / 2f,
        _ => 0f
    };

    public static Vector2 Normal(this LaserOrientations orientation) => orientation switch
    {
        LaserOrientations.Up => -Vector2.UnitY,
        LaserOrientations.Down => Vector2.UnitY,
        LaserOrientations.Left => -Vector2.UnitX,
        LaserOrientations.Right => Vector2.UnitX,
        _ => Vector2.Zero
    };

    public static float LengthOfHitbox(this LaserOrientations orientation, Hitbox hitbox) =>
        orientation switch
        {
            LaserOrientations.Up => hitbox.Height,
            LaserOrientations.Down => hitbox.Height,
            LaserOrientations.Left => hitbox.Width,
            LaserOrientations.Right => hitbox.Width,
            _ => 0f
        };

    public static float ThicknessOfHitbox(this LaserOrientations orientation, Hitbox hitbox) =>
        orientation switch
        {
            LaserOrientations.Up => hitbox.Width,
            LaserOrientations.Down => hitbox.Width,
            LaserOrientations.Left => hitbox.Height,
            LaserOrientations.Right => hitbox.Height,
            _ => 0f
        };

    public static Vector2 OriginOfHitbox(this LaserOrientations orientation, Hitbox hitbox) =>
        orientation switch
        {
            LaserOrientations.Up => hitbox.BottomCenter,
            LaserOrientations.Down => hitbox.TopCenter,
            LaserOrientations.Left => hitbox.CenterRight,
            LaserOrientations.Right => hitbox.CenterLeft,
            _ => Vector2.Zero
        };

    public static bool Horizontal(this LaserOrientations orientation) =>
        orientation is LaserOrientations.Left or LaserOrientations.Right;

    public static bool Vertical(this LaserOrientations orientation) =>
        orientation is LaserOrientations.Up or LaserOrientations.Down;
}
