using Celeste.Mod.CommunalHelper.Utils;

namespace Celeste.Mod.CommunalHelper.Entities;

/// <summary>
/// Base abstract entity class for the rings the player can traverse while gliding.
/// </summary>
[Tracked(Inherited = true)]
public abstract class ElytraRing : Entity
{
    /// <summary>
    /// Extremity A of the ring.
    /// </summary>
    protected Vector2 A { get; }

    /// <summary>
    /// Extremity B of the ring.
    /// </summary>
    protected Vector2 B { get; }

    /// <summary>
    /// Center point of the ring.
    /// </summary>
    protected Vector2 Middle { get; }

    /// <summary>
    /// The direction this ring is pointing towards.
    /// Set to <see cref="Vector2.Zero"/> if the two extremity points are equal.
    /// </summary>
    protected Vector2 Direction { get; }

    /// <summary>
    /// Whether <see cref="OnPlayerTraversal(Player)"/> must be called in the same order the player traversed the rings.
    /// If <c>false</c>, this ring is prioritized in the list of traversed rings.
    /// </summary>
    public virtual bool PreserveTraversalOrder => true;

    /// <summary>
    /// The minimum delay in seconds between two allowed traversals.
    /// </summary>
    public virtual float Delay => 0.04f;
    private float timer;

    private readonly Shape3D front, back;
    private readonly Matrix orientation;

    /// <summary>
    /// Instantiates a ring.
    /// </summary>
    /// <param name="a">Extremity A of the ring.</param>
    /// <param name="b">Extremity B of the ring.</param>
    public ElytraRing(Vector2 a, Vector2 b)
    {
        A = a;
        B = b;
        Middle = (a + b) / 2f;
        Direction = (a - b).Perpendicular().SafeNormalize();

        Position = Middle;

        var mesh = Shapes.HalfRing(Vector2.Distance(a, b), 4.0f);

        Matrix tilt = Matrix.CreateRotationY(0.25f);
        orientation = Matrix.CreateRotationZ(-Direction.Angle());

        Add(front = new Shape3D(mesh)
        {
            Depth = Depths.FGTerrain,
            HighlightStrength = 0.8f,
            NormalEdgeStrength = 0.0f,
            Texture = CommunalHelperGFX.Blank,
            Matrix = tilt * orientation,
        });

        Add(back = new Shape3D(mesh)
        {
            Depth = Depths.BGTerrain,
            HighlightStrength = 0.4f,
            NormalEdgeStrength = 0.0f,
            Texture = CommunalHelperGFX.Blank,
            Matrix = Matrix.CreateRotationX(MathHelper.Pi) * tilt * orientation,
            Tint = new(Vector3.One * 0.5f, 1.0f),
        });
    }

    /// <summary>
    /// Determines whether this ring can be traversed.
    /// Checks for movement segment intersection and delay elapsed.
    /// </summary>
    /// <param name="from">The first point of the movement segment.</param>
    /// <param name="to">The second point of the movement segment.</param>
    /// <returns>The result of the traversal check.</returns>
    public bool CanTraverse(Vector2 from, Vector2 to)
        => timer == 0.0f && Util.SegmentIntersection(from, to, A, B);

    /// <summary>
    /// Called when the player traverses the ring.
    /// </summary>
    /// <param name="player">A reference to the player.</param>
    public virtual void OnPlayerTraversal(Player player)
    {
        timer = Delay;
    }

    public override void Update()
    {
        timer = Calc.Approach(timer, 0.0f, Engine.DeltaTime);

        Matrix m = Matrix.CreateRotationY(0.25f + (float)Math.Sin(Scene.TimeActive * 3f) * 0.1f) * orientation;
        front.Matrix = m;
        back.Matrix = Matrix.CreateRotationX(MathHelper.Pi) * m;

        base.Update();
    }
}
