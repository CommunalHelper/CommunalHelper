using Celeste.Mod.CommunalHelper.Components;
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
    public Vector2 A { get; }

    /// <summary>
    /// Extremity B of the ring.
    /// </summary>
    public Vector2 B { get; }

    /// <summary>
    /// Center point of the ring.
    /// </summary>
    public Vector2 Middle { get; }

    /// <summary>
    /// The direction this ring is pointing towards.
    /// Set to <see cref="Vector2.Zero"/> if the two extremity points are equal.
    /// </summary>
    public Vector2 Direction { get; }

    /// <summary>
    /// Whether <see cref="OnPlayerTraversal(Player)"/> must be called in the same order the player traversed the rings.
    /// If <c>false</c>, this ring is prioritized in the list of traversed rings.
    /// </summary>
    public virtual bool PreserveTraversalOrder => true;

    /// <summary>
    /// The sound effect played when this ring is crossed. Defaults to <see cref="SFX.NONE"/>.
    /// </summary>
    public virtual string TraversalSFX => SFX.NONE;

    /// <summary>
    /// The minimum delay in seconds between two allowed traversals.
    /// </summary>
    public virtual float Delay => 0.04f;
    private float timer;

    private readonly Shape3D front, back;
    private readonly Matrix orientation;

    private float rotation;
    private float saturation;

    private float travelLerp = 0.0f;

    private readonly ParticleType p_traversal;

    /// <summary>
    /// Instantiates a ring.
    /// </summary>
    /// <param name="a">Extremity A of the ring.</param>
    /// <param name="b">Extremity B of the ring.</param>
    public ElytraRing(Vector2 a, Vector2 b, Color color)
    {
        A = a;
        B = b;
        Middle = (a + b) / 2f;
        Direction = (a - b).Perpendicular().SafeNormalize();

        Position = Middle;

        var mesh = Shapes.HalfRing(Vector2.Distance(a, b), 4.0f, color);

        Matrix tilt = Matrix.CreateRotationY(0.25f);
        orientation = Matrix.CreateRotationZ(-Direction.Angle());

        Add(front = new Shape3D(mesh)
        {
            Depth = Depths.FGTerrain,
            HighlightStrength = 0.8f,
            NormalEdgeStrength = 0.0f,
            RainbowMix = 0.1f,
            Texture = CommunalHelperGFX.Blank,
            Matrix = tilt * orientation,
        });

        Add(back = new Shape3D(mesh)
        {
            Depth = Depths.BGTerrain,
            HighlightStrength = 0.4f,
            NormalEdgeStrength = 0.0f,
            RainbowMix = 0.1f,
            Texture = CommunalHelperGFX.Blank,
            Matrix = Matrix.CreateRotationX(MathHelper.Pi) * tilt * orientation,
            Tint = new(Vector3.One * 0.5f, 1.0f),
        });

        p_traversal = new(LightningBreakerBox.P_Smash)
        {
            Color = Color.Lerp(color, Color.White, 0.25f),
            Color2 = Color.Lerp(color, Color.White, 0.75f),
            SpeedMin = 50f,
            SpeedMax = 100f,
            SpeedMultiplier = 0.15f
        };
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
    /// <param name="sign">The sign of the traversal. +1 for crossing the ring in its direction, -1 for crossing it in the opposite direction.</param>
    public virtual void OnPlayerTraversal(Player player, int sign, bool shake = true)
    {
        timer = Delay;
        travelLerp = 1.0f;

        Level level = Scene as Level;
        
        if (shake)
        {
            level.Shake(0.1f);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
            Audio.Play(TraversalSFX, player.Center);
            Celeste.Freeze(0.05f);
        }

        float length = Vector2.Distance(A, B);
        float angle = (Direction * sign).Angle();
        for (float d = 0.0f; d <= length; d += 4.0f)
        {
            Vector2 at = Vector2.Lerp(A, B, d / length);
            level.Particles.Emit(p_traversal, at, angle);
        }
    }

    public override void Update()
    {
        timer = Calc.Approach(timer, 0.0f, Engine.DeltaTime);

        Matrix m = Matrix.CreateRotationY(rotation + 0.25f + (float)Math.Sin(Scene.TimeActive * 3f) * 0.1f) * orientation;
        front.Matrix = m;
        back.Matrix = Matrix.CreateRotationX(MathHelper.Pi) * m;

        front.Tint = new Vector4(Vector3.One * (1 + saturation), 1.0f);
        back.Tint = new Vector4(Vector3.One * (1 + saturation) / 2.0f, 1.0f);

        front.RainbowMix = back.RainbowMix = 0.1f + travelLerp * 0.3f;

        front.HighlightStrength = 0.8f + travelLerp * 0.2f;
        back.HighlightStrength = 0.4f + travelLerp * 0.4f;

        travelLerp = Calc.Approach(travelLerp, 0.0f, Engine.DeltaTime);
        saturation = travelLerp * 1.5f;
        rotation = Ease.BackIn(travelLerp) * MathHelper.PiOver2;

        base.Update();
    }
}
