using Celeste.Mod.CommunalHelper.Components;
using Celeste.Mod.CommunalHelper.Utils;
using System.Collections;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/ShapeshifterPath")]
[Tracked]
public sealed class ShapeshifterPath : Entity
{
    public BakedCurve Curve { get; }
    public Vector2 Start { get; }

    public Ease.Easer Easer { get; }
    public float Duration { get; }

    public int Yaw { get; }
    public int Pitch { get; }
    public int Roll { get; }

    public int ID { get; }

    public float QuakeTime { get; set; }
    public float FakeoutTime { get; set; } 
    public float FakeoutDistance { get; set; }

    public ShapeshifterPath(EntityData data, Vector2 offset, EntityID id)
        : this
        (
            id.ID,
            data.NodesWithPosition(offset),
            data.Easer("easer"),
            data.Float("duration", 2.0f),
            data.Int("rotateYaw"),
            data.Int("rotatePitch"),
            data.Int("rotateRoll"),
            data.Float("quakeTime", 0.5f),
            data.Float("fakeoutTime", 0.75f), data.Float("fakeoutDistance", 32.0f)
        )
    { }

    public ShapeshifterPath
    (
        int id,
        Vector2[] points, Ease.Easer easer, float duration,
        int yaw, int pitch, int roll,
        float quakeTime = 0.5f,
        float fakeoutTime = 0.75f, float fakeoutDistance = 32.0f
    )
    {
        if (points.Length is not 4)
            throw new ArgumentException("points must be an array of 4 points", nameof(points));

        Start = points[0];
        Curve = new BakedCurve(points, CurveType.Cubic, 32);

        Easer = easer;
        Duration = duration;

        Yaw = yaw;
        Pitch = pitch;
        Roll = roll;

        ID = id;

        QuakeTime = quakeTime;
        FakeoutTime = fakeoutTime;
        FakeoutDistance = fakeoutDistance;
    }
}

[CustomEntity("CommunalHelper/Shapeshifter")]
[Tracked]
public class Shapeshifter : Solid
{
    public int ID { get; }

    private readonly char[,,] voxel;
    private readonly int width, height, depth;

    private readonly Shape3D mesh;
    private float yaw, pitch, roll;

    private bool moving;

    private readonly string startSound, finishSound;
    private readonly float startShake, finishShake;
    private readonly float rainbowMix;
    
    private readonly SoundSource sfx;

    public Shapeshifter(EntityData data, Vector2 offset, EntityID id)
        : this
        (
            id.ID, data.Position + offset,
            data.Int("voxelWidth", 1), data.Int("voxelHeight", 1), data.Int("voxelDepth", 1),
            data.Attr("model", string.Empty), data.Char("defaultTile", '0'),
            data.Attr("startSound", SFX.game_10_quake_rockbreak),
            data.Attr("finishSound", SFX.game_gen_touchswitch_gate_finish),
            data.Float("startShake", 0.2f), data.Float("finishShake", 0.2f),
            data.Float("rainbowMix", 0.2f),
            data.Int("surfaceSoundIndex", SurfaceIndex.Asphalt)
        )
    { }

    public Shapeshifter
    (
        int id, Vector2 position,
        int width, int height, int depth,
        string model, char defaultTile = '0',
        string startSound = SFX.game_10_quake_rockbreak,
        string finishSound = SFX.game_gen_touchswitch_gate_finish,
        float startShake = 0.2f, float finishShake = 0.2f,
        float rainbowMix = 0.2f,
        int surfaceSoundIndex = SurfaceIndex.Asphalt
    )
        : base(position, 0, 0, safe: true)
    {
        ID = id;

        this.width = width;
        this.height = height;
        this.depth = depth;

        voxel = new char[depth, height, width];
        for (int z = 0; z < depth; z++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = x + width * y + width * height * z;
                    char tile = index < model.Length ? model[index] : defaultTile;
                    voxel[z, y, x] = tile;
                }
            }
        }

        Add(mesh = new(Shapes.TileVoxelPCTN(voxel))
        {
            Depth = Depths.FGTerrain,
            NormalEdgeStrength = 0f,
            DepthEdgeStrength = 0f,
            RainbowMix = 0f,
        });

        BuildCollider();

        this.startSound = startSound;
        this.finishSound = finishSound;
        this.startShake = startShake;
        this.finishShake = finishShake;
        this.rainbowMix = rainbowMix;

        Add(sfx = new(CustomSFX.game_shapeshifter_move));
        sfx.Pause();

        SurfaceSoundIndex = surfaceSoundIndex;
    }

    private void BuildCollider()
    {
        int qYaw = Util.Mod((int) Math.Round(yaw / MathHelper.PiOver2), 4);
        int qPitch = Util.Mod((int) Math.Round(pitch / MathHelper.PiOver2), 4);
        int qRoll = Util.Mod((int) Math.Round(roll / MathHelper.PiOver2), 4);

        bool tYaw = qYaw % 2 == 1;
        bool tPitch = qPitch % 2 == 1;
        bool tRoll = qRoll % 2 == 1;
        int w = !tPitch && tYaw ? depth : ((tYaw && !tRoll) || (!tYaw && tRoll) ? height : width);
        int h = tPitch ? depth : (tRoll ? width : height);

        char[,,] voxel = this.voxel;
        if (qRoll == 1) voxel = voxel.RotateZClockwise();
        else if (qRoll == 2) voxel = voxel.MirrorAboutZ();
        else if (qRoll == 3) voxel = voxel.RotateZCounterclockwise();

        if (qPitch == 1) voxel = voxel.RotateXClockwise();
        else if (qPitch == 2) voxel = voxel.MirrorAboutX();
        else if (qPitch == 3) voxel = voxel.RotateXCounterclockwise();

        if (qYaw == 1) voxel = voxel.RotateYClockwise();
        else if (qYaw == 2) voxel = voxel.MirrorAboutY();
        else if (qYaw == 3) voxel = voxel.RotateYCounterclockwise();

        int sx = voxel.GetLength(2);
        int sy = voxel.GetLength(1);
        int sz = voxel.GetLength(0);
        bool[,] tilemap = new bool[sx, sy];

        for (int z = 0; z < sz; z++)
            for (int y = 0; y < sy; y++)
                for (int x = 0; x < sx; x++)
                    tilemap[x, y] |= voxel[z, y, x] != '0';

        Collider = Util.GenerateColliderGrid(tilemap);

        if (Collider is null)
            return;

        // Fix offset
        Vector2 offset = new Vector2(w, h) * -4.0f;
        foreach (Collider hitbox in (Collider as ColliderList).colliders)
            hitbox.Position += offset;

        //sfx.Position = Center - Position;
    }

    private ShapeshifterPath FindPath()
    {
        if (Collider is null)
            return null;

        var bounds = Collider.Bounds;
        var paths = Scene.Tracker.GetEntities<ShapeshifterPath>()
                                 .Cast<ShapeshifterPath>();
        foreach (ShapeshifterPath path in paths)
        {
            var ptRect = new Rectangle((int) path.Start.X - 2, (int) path.Start.Y - 2, 4, 4);
            if (bounds.Intersects(ptRect))
                return path;
        }

        return null;
    }

    internal void FollowPath(ShapeshifterPath path)
    {
        if (moving)
            return;

        path ??= FindPath();
        if (path is null)
            return;

        moving = true;
        Add(new Coroutine(Sequence(path)));
    }

    private IEnumerator Sequence(ShapeshifterPath path)
    {
        Level level = Scene as Level;

        if (path.QuakeTime > 0.0f)
        {
            var quakeSfx = Audio.Play(CustomSFX.game_shapeshifter_shake, Position);
            StartShaking(path.QuakeTime);
            yield return path.QuakeTime;
            quakeSfx.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        }

        Audio.Play(startSound, Position);
        if (startShake > 0.0f)
        {
            StartShaking(startShake);
            level.Shake(startShake);
        }
        if (startShake > 0.0f)
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
        
        if (path.Yaw != 0f || path.Pitch != 0f || path.Roll != 0f)
        {
            Collidable = false;
            Collider = null;
        }

        Vector2 offset = Position - path.Start;

        float pathYaw = path.Yaw * MathHelper.PiOver2;
        float pathPitch = path.Pitch * MathHelper.PiOver2;
        float pathRoll = path.Roll * MathHelper.PiOver2;

        float finalYaw = yaw + pathYaw;
        float finalPitch = pitch + pathPitch;
        float finalRoll = roll + pathRoll;

        float distance = 0.0f;

        IEnumerator Travel
        (
            float duration,
            Ease.Easer easer,
            float distanceTo,
            float yawTo,
            float pitchTo,
            float rollTo,
            Action<float, float, float> travelCallback_t_ease_moveSpeed
        )
        {
            Vector2 last = ExactPosition;
            float distanceFrom = distance;
            float yawFrom = yaw, pitchFrom = pitch, rollFrom = roll;
            return Util.Interpolate(duration, t =>
            {
                float ease = easer(t);

                distance = MathHelper.Lerp(distanceFrom, distanceTo, ease);
                Vector2 next = path.Curve.GetPointByDistance(distance) + offset;
                if (Collidable)
                    MoveTo(next);
                else
                    Position = next;

                Vector2 d = ExactPosition - last;
                float moveSpeed = Calc.ClampedMap(d.Length(), 0.0f, 7.5f);
                last = ExactPosition;

                yaw = MathHelper.Lerp(yawFrom, yawTo, ease);
                pitch = MathHelper.Lerp(pitchFrom, pitchTo, ease);
                roll = MathHelper.Lerp(rollFrom, rollTo, ease);

                travelCallback_t_ease_moveSpeed?.Invoke(t, ease, moveSpeed);
            });
        }

        if (path.FakeoutTime > 0.0f)
            yield return Travel
            (
                path.FakeoutTime, Ease.CubeOut, path.FakeoutDistance,
                yaw - pathYaw / 4f, pitch - pathPitch / 4f, roll - pathRoll / 4f,
                (t, _, _) =>
                {
                    mesh.DepthEdgeStrength = t * 0.8f;
                    mesh.NormalEdgeStrength = t * 0.5f;
                    mesh.RainbowMix = t * rainbowMix;
                }
            );

        sfx.Resume();
        yield return Travel
        (
            path.Duration, path.Easer, path.Curve.Length,
            finalYaw, finalPitch, finalRoll,
            (t, ease, moveSpeed) =>
            {
                float meshLerp = Ease.CubeOut(path.FakeoutTime > 0.0f ? (1 - t) : Ease.UpDown(t));
                mesh.DepthEdgeStrength = meshLerp * 0.8f;
                mesh.NormalEdgeStrength = meshLerp * 0.5f;
                mesh.RainbowMix = meshLerp * rainbowMix;

                sfx.Param("move_speed", moveSpeed);
                sfx.Param("move_percent", ease);
            }
        );

        BuildCollider();
        while (true)
        {
            Collidable = true;
            if (!CollideCheck<Actor>())
                break;
            Collidable = false;
            yield return null;
        }
        moving = false;

        sfx.Pause();

        Audio.Play(finishSound, Position);
        if (finishShake > 0.0f)
        {
            StartShaking(finishShake);
            level.Shake(finishShake);
        }
        if (finishShake > 0.0f)
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
    }

    public override void Update()
    {
        base.Update();
        mesh.Matrix = Matrix.CreateFromYawPitchRoll(yaw, pitch, roll);
    }
}
