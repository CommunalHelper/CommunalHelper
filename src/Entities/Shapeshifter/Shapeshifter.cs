using Celeste.Mod.CommunalHelper.Entities.Misc;
using Celeste.Mod.CommunalHelper.Utils;
using Microsoft.Xna.Framework.Graphics;
using System.Collections;
using System.Linq;
using System.Threading;

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

    public ShapeshifterPath(EntityData data, Vector2 offset)
        : this
        (
            data.NodesWithPosition(offset),
            data.Easer("easer"),
            data.Float("duration", 2.0f),
            data.Int("rotateYaw"),
            data.Int("rotatePitch"),
            data.Int("rotateRoll")
        )
    { }

    public ShapeshifterPath(Vector2[] points, Ease.Easer easer, float duration, int yaw, int pitch, int roll)
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
    }
}

[CustomEntity("CommunalHelper/Shapeshifter")]
public class Shapeshifter : Solid
{
    private readonly char[,,] voxel;
    private readonly int width, height, depth;

    private readonly Shape3D mesh;
    private float yaw, pitch, roll;

    private ShapeshifterPath path;

    public Shapeshifter(EntityData data, Vector2 offset)
        : this
        (
            data.Position + offset,
            data.Int("voxelWidth", 1),
            data.Int("voxelHeight", 1),
            data.Int("voxelDepth", 1),
            data.Attr("model", string.Empty),
            data.Attr("atlas", null)
        )
    { }

    public Shapeshifter(Vector2 position, int width, int height, int depth, string model, string atlas = null)
        : base(position, 0, 0, safe: true)
    {
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

        OnDashCollide = (player, dir) =>
        {
            if (path is not null)
                Add(new Coroutine(Sequence()));
            return DashCollisionResults.Rebound;
        };

        BuildCollider();
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

    private IEnumerator Sequence()
    {
        if (path.Yaw != 0f || path.Pitch != 0f || path.Roll != 0f)
        {
            Collidable = false;
            Collider = null;
        }

        mesh.DepthEdgeStrength = 0.8f;
        mesh.NormalEdgeStrength = 0.5f;
        //mesh.RainbowMix = 0.2f;

        Vector2 offset = Position - path.Start;
        yield return Util.Interpolate(path.Duration, t =>
        {
            float ease = path.Easer(t);

            Vector2 next = path.Curve.GetPointByDistance(ease * path.Curve.Length) + offset;
            if (Collidable)
                MoveTo(next);
            else
                Position = next;

            yaw = path.Yaw * MathHelper.PiOver2 * ease;
            pitch = path.Pitch * MathHelper.PiOver2 * ease;
            roll = path.Roll * MathHelper.PiOver2 * ease;
        });

        BuildCollider();
        Collidable = true;
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);

        var bounds = Collider.Bounds;
        var paths = Scene.Tracker.GetEntities<ShapeshifterPath>()
                                 .Cast<ShapeshifterPath>();
        foreach (ShapeshifterPath path in paths)
        {
            var ptRect = new Rectangle((int)path.Start.X - 2, (int)path.Start.Y - 2, 4, 4);
            if (bounds.Intersects(ptRect))
                this.path = path;
        }
    }

    public override void Update()
    {
        base.Update();
        mesh.Matrix = Matrix.CreateFromYawPitchRoll(yaw, pitch, roll);
    }
}
