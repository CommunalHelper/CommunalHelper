using Celeste.Mod.CommunalHelper.Entities.Misc;
using Celeste.Mod.CommunalHelper.Utils;
using Microsoft.Xna.Framework.Graphics;
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

    public ShapeshifterPath(EntityData data, Vector2 offset, EntityID id)
        : this
        (
            data.NodesWithPosition(offset),
            data.Easer("easer"),
            data.Float("duration", 2.0f),
            data.Int("rotateYaw"),
            data.Int("rotatePitch"),
            data.Int("rotateRoll"),
            id.ID
        )
    { }

    public ShapeshifterPath(Vector2[] points, Ease.Easer easer, float duration, int yaw, int pitch, int roll, int id)
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

    public Shapeshifter(EntityData data, Vector2 offset, EntityID id)
        : this
        (
            id.ID, data.Position + offset,
            data.Int("voxelWidth", 1), data.Int("voxelHeight", 1), data.Int("voxelDepth", 1),
            data.Attr("model", string.Empty), data.Attr("atlas", null),
            data.Attr("startSound", SFX.game_10_quake_rockbreak),
            data.Attr("finishSound", SFX.game_gen_touchswitch_gate_finish),
            data.Float("startShake", 0.2f),
            data.Float("finishShake", 0.2f)
        )
    { }


    public Shapeshifter
    (
        int id, Vector2 position,
        int width, int height, int depth,
        string model, string atlas = null,
        string startSound = SFX.game_10_quake_rockbreak,
        string finishSound = SFX.game_gen_touchswitch_gate_finish,
        float startShake = 0.2f, float finishShake = 0.2f
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
                    char tile = index < model.Length ? model[index] : '0';
                    voxel[z, y, x] = tile;
                }
            }
        }

        Texture2D texture = string.IsNullOrWhiteSpace(atlas)
            ? GFX.Game.Sources.First().Texture_Safe
            : GFX.Game[atlas].Texture.Texture;

        Add(mesh = new(Shapes.TileVoxel(voxel))
        {
            Texture = texture,
            Depth = Depths.FGTerrain,
            NormalEdgeStrength = 0f,
            DepthEdgeStrength = 0f,
            RainbowMix = 0f
        });

        BuildCollider();

        this.startSound = startSound;
        this.finishSound = finishSound;
        this.startShake = startShake;
        this.finishShake = finishShake;
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
        path ??= FindPath();
        if (path is null || moving)
            return;

        moving = true;
        Add(new Coroutine(Sequence(path)));
    }

    private IEnumerator Sequence(ShapeshifterPath path)
    {
        Level level = Scene as Level;

        Audio.Play(startSound, Position);
        level.Shake(startShake);
        if (startShake > 0.0f)
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
        
        if (path.Yaw != 0f || path.Pitch != 0f || path.Roll != 0f)
        {
            Collidable = false;
            Collider = null;
        }

        float yawa = yaw,
              yawb = yawa + path.Yaw * MathHelper.PiOver2;
        float pitcha = pitch,
              pitchb = pitcha + path.Pitch * MathHelper.PiOver2;
        float rolla = roll,
              rollb = rolla + path.Roll * MathHelper.PiOver2;

        Vector2 offset = Position - path.Start;
        yield return Util.Interpolate(path.Duration, t =>
        {
            float ease = path.Easer(t);

            Vector2 next = path.Curve.GetPointByDistance(ease * path.Curve.Length) + offset;
            if (Collidable)
                MoveTo(next);
            else
                Position = next;

            yaw = MathHelper.Lerp(yawa, yawb, ease);
            pitch = MathHelper.Lerp(pitcha, pitchb, ease);
            roll = MathHelper.Lerp(rolla, rollb, ease);

            mesh.DepthEdgeStrength = Ease.UpDown(t) * 0.8f;
            mesh.NormalEdgeStrength = Ease.UpDown(t) * 0.5f;
        });

        BuildCollider();
        Collidable = true;
        moving = true;

        Audio.Play(finishSound, Position);
        level.Shake(finishShake);
        if (finishShake > 0.0f)
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
    }

    public override void Update()
    {
        base.Update();
        mesh.Matrix = Matrix.CreateFromYawPitchRoll(yaw, pitch, roll);
    }
}
