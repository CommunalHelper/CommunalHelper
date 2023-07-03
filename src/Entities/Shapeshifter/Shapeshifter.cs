using Celeste.Mod.CommunalHelper.Entities.Misc;
using Celeste.Mod.CommunalHelper.Utils;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/Shapeshifter")]
public class Shapeshifter : Solid
{
    private readonly char[,,] voxel;
    private readonly int width, height, depth;

    private readonly Shape3D mesh;

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
            Depth = Depths.FGTerrain
        });

        BuildCollider();
    }
    
    private void BuildCollider()
    {
        float yaw = 0.0f, pitch = 0.0f, roll = 0.0f;

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
}
