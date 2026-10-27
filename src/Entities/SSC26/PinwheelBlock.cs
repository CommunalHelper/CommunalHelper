using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CommunalHelper.Entities.SSC26;

public enum PinwheelSpinDirection
{
    Clockwise,
    CounterClockwise,
    None,
}

[Tracked]
public class PinwheelAnchor : Entity
{
    internal int Group;
    internal List<PinwheelBlock> Blocks = [];
    internal bool Joined { get; private set; } = false;
    private bool joinable = true;

    public int ID = -1;

    public PinwheelAnchor(Vector2 position, int group, int id)
        : base(position)
    {
        Depth = Depths.Solids - 1;
        Collider = new Hitbox(1, 1);
        Group = group;
        ID = id;
    }

    public override void Render()
    {
        base.Render();

        Blocks[0].pinwheelAnchorTexture.DrawCentered(Position);
    }

    public void AddBlock(PinwheelBlock block)
    {
        Blocks.Add(block);
    }

    public void SpinAll(PinwheelSpinDirection dir)
    {
        if (dir == PinwheelSpinDirection.None) return;
        foreach (var block in Blocks)
        {
            block.Spin(dir);
        }
    }

    public PinwheelAnchor Join()
    {
        Joined = true;
        IEnumerable<PinwheelAnchor> anchors = CollideAll<PinwheelAnchor>().Cast<PinwheelAnchor>();
        PinwheelAnchor otherAnchor = anchors.FirstOrDefault(a =>
            a != null
            && a != this
            && a.joinable
            && a.Group == Group
        );
        if (otherAnchor != null && otherAnchor != this)
        {
            joinable = false;
            RemoveSelf();
            foreach (PinwheelBlock block in Blocks)
            {
                otherAnchor.AddBlock(block);
                block.Anchor = otherAnchor;
            }
            return otherAnchor;
        }
        return this;
    }
}

[CustomEntity("CommunalHelper/PinwheelBlock")]
public class PinwheelBlock : Solid
{
    private int group;
    private float rotationPerHit_rad;
    private float moveDuration;
    private string easeType;

    private float lerp = 1f;
    private float armLength;
    private float rotation;
    private float spinStart;
    private float spinEnd;
    private Vector2 anchorPosition;
    private string texturesPath;
    private MTexture[,] ninePatchTexture;
    private MTexture[] connectorTexture;
    private MTexture blockAnchorTexture;
    internal MTexture pinwheelAnchorTexture;

    internal PinwheelAnchor Anchor = null;

    public bool Moving => lerp < 1;
    public int ID => SourceData?.ID ?? -2;

    private static readonly string defaultTexturesPath = "objects/CommunalHelper/pinwheelBlock/";

    public PinwheelBlock(EntityData data, Vector2 offset)
        : base(data.Position + offset, data.Width, data.Height, false)
    {
        Depth = Depths.Solids;
        if ((data.Nodes?.Length ?? 0) == 0)
        {
            throw new Exception("Pinwheel Block requires a node to specify the rotation anchor");
        }
        anchorPosition = data.Nodes[0] + offset;

        group = data.Int("group", 0);
        moveDuration = data.Float("moveTime", 0.75f);
        rotationPerHit_rad = data.Float("rotationDegrees", 180f) * Calc.DegToRad;
        easeType = data.Attr("easeType", "linear").ToLowerInvariant();
        texturesPath = data.Attr("texturesPath", defaultTexturesPath);
        if (texturesPath.Length > 0 && !texturesPath.EndsWith("/")) { texturesPath += "/"; }

        armLength = (Center - anchorPosition).Length();
        rotation = (Center - anchorPosition).Angle();
        spinStart = rotation;
        spinEnd = rotation;
        OnDashCollide = OnDashed;

        ninePatchTexture = LoadNinePatch("block");
        connectorTexture = [.. LoadArmTextures()];
        blockAnchorTexture = LoadSingleTexture("block-anchor");
        pinwheelAnchorTexture = LoadSingleTexture("pinwheel-anchor");
    }

    private MTexture LoadSingleTexture(string textureName)
    {
        string customPath = $"{texturesPath}{textureName}";
        string defaultPath = $"{defaultTexturesPath}{textureName}";
        if (!string.IsNullOrEmpty(customPath) && GFX.Game.Has(customPath))
        {
            return GFX.Game[customPath];
        }
        else
        {
            return GFX.Game[defaultPath];
        }
    }

    private List<MTexture> LoadArmTextures()
    {
        List<MTexture> connTexList = new();
        for (int i = 0; i < 64; i++)
        {
            string tex = $"{texturesPath}connector{i.ToString("D2", new CultureInfo("en-us"))}";
            if (!GFX.Game.Has(tex)) break;
            connTexList.Add(GFX.Game[tex]);
        }
        if (connTexList.Count == 0)
        {
            for (int i = 0; i < 64; i++)
            {
                string tex = $"{defaultTexturesPath}connector{i.ToString("D2", new CultureInfo("en-us"))}";
                if (!GFX.Game.Has(tex)) break;
                connTexList.Add(GFX.Game[tex]);
            }
        }
        return connTexList;
    }

    private MTexture[,] LoadNinePatch(string textureName)
    {
        MTexture[,] tex = new MTexture[3, 3];
        MTexture texture = LoadSingleTexture(textureName);
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                tex[x, y] = texture.GetSubtexture(x * 8, y * 8, 8, 8);
            }
        }
        return tex;
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);
        scene.Add(Anchor = new PinwheelAnchor(anchorPosition, group, ID));
        Anchor.AddBlock(this);
    }

    public override void Update()
    {
        base.Update();
        if (!Anchor.Joined)
        {
            Anchor = Anchor.Join();
        }
        if (!Moving) return;

        lerp = Calc.Approach(lerp, 1f, Engine.DeltaTime / MathF.Max(moveDuration, 0.01f));
        if (lerp >= 1f)
        {
            // We finish moving
            rotation = spinEnd;
            Audio.Play("event:/game/general/wall_break_stone", Center); // TODO find better SFX for this
                                                                        // TODO momentum leniency
        }
        else
        {
            rotation = GetEasedInterp(lerp, spinStart, spinEnd);
        }
        Vector2 newPos = Anchor.Position + Vector2.UnitX.Rotate(rotation) * armLength;
        MoveToCentered(newPos);
    }

    private DashCollisionResults OnDashed(Player player, Vector2 direction)
    {
        if (Moving) return DashCollisionResults.NormalCollision;
        PinwheelSpinDirection dir = GetSpinDirection(direction);
        if (dir == PinwheelSpinDirection.None) return DashCollisionResults.NormalCollision;
        Celeste.Freeze(0.05f);
        Audio.Play("event:/game/general/wall_break_wood", Center);
        Anchor.SpinAll(dir);
        return DashCollisionResults.Rebound;
    }

    private PinwheelSpinDirection GetSpinDirection(Vector2 dashDirection)
    {
        Vector2 moveDir_clockwise = Vector2.UnitX.Rotate(rotation + MathF.PI * 0.5f);
        float angle_to_clockwise = MathF.Abs(MathF.Acos(Calc.Clamp(Vector2.Dot(dashDirection, moveDir_clockwise), -1f, 1f)));
        if (angle_to_clockwise <= MathF.PI * 3f / 8f) return PinwheelSpinDirection.Clockwise;
        if (angle_to_clockwise >= MathF.PI * 5f / 8f) return PinwheelSpinDirection.CounterClockwise;
        return PinwheelSpinDirection.None;
    }

    public void Spin(PinwheelSpinDirection dir)
    {
        if (Moving || dir == PinwheelSpinDirection.None) return;
        float rot = dir == PinwheelSpinDirection.Clockwise ? rotationPerHit_rad : -rotationPerHit_rad;
        spinStart = rotation;
        spinEnd = rotation + rot;
        lerp = 0f;
    }

    private float GetEasedInterp(float lerp, float min, float max)
    {
        Ease.Easer easer = easeType switch
        {
            "quadin" => Ease.QuadIn,
            "quadout" => Ease.QuadOut,
            "quadinout" => Ease.QuadInOut,
            "cubicin" => Ease.CubeIn,
            "cubicout" => Ease.CubeOut,
            "cubicinout" => Ease.CubeInOut,
            "quartin" => Ease.QuadIn,
            "quartout" => Ease.QuadOut,
            "quartinout" => Ease.QuadInOut,
            "quintin" => Ease.QuintIn,
            "quintout" => Ease.QuintOut,
            "quintinout" => Ease.QuintInOut,
            "sinein" => Ease.SineIn,
            "sineout" => Ease.SineOut,
            "sineinout" => Ease.SineInOut,
            "elasticin" => Ease.ElasticIn,
            "elasticout" => Ease.ElasticOut,
            "elasticinout" => Ease.ElasticInOut,
            "backin" => Ease.BackIn,
            "backout" => Ease.BackOut,
            "backinout" => Ease.BackInOut,
            "bouncein" => Ease.BounceIn,
            "bounceout" => Ease.BounceOut,
            "bounceinout" => Ease.BounceInOut,
            "linear" => Ease.Linear,
            "hybrid" => HybridEase,
            _ => Ease.Linear, // default to linear
        };
        return Calc.ClampedMap(easer(lerp), 0f, 1f, min, max);
    }

    private static Ease.Easer HybridEase = (float t) => (Ease.Linear(t) + Ease.CubeIn(t)) * 0.5f;

    public void MoveToCentered(Vector2 center)
    {
        MoveTo(center - 0.5f * new Vector2(Width, Height));
    }

    public override void Render()
    {
        // Draw nine-patch block
        int tilesX = (int) Width / 8 - 1;
        int tilesY = (int) Height / 8 - 1;
        for (int i = 0; i <= tilesX; i++)
        {
            for (int j = 0; j <= tilesY; j++)
            {
                int patchX = i < tilesX ? Math.Min(i, 1) : 2;
                int patchY = j < tilesY ? Math.Min(j, 1) : 2;
                ninePatchTexture[patchX, patchY].Draw(Position + new Vector2(i * 8, j * 8));
            }
        }
        // Draw connectors
        int connIndex = ((int) (rotation / (MathF.PI * 0.25f) * connectorTexture.Length)) % connectorTexture.Length;
        while (connIndex < 0) connIndex += connectorTexture.Length;
        int numConnectors = (int) MathF.Round(armLength / 16f);
        float offset = armLength - (numConnectors * 16f);
        for (int i = 0; i < numConnectors; i++)
        {
            Vector2 connPos = Anchor.Position + Vector2.UnitX.Rotate(rotation) * (16f * (i + 0.5f) + offset * 0.5f);
            connectorTexture[connIndex].DrawCentered(connPos, Color.White, 1f, rotation);
        }
        // Draw block anchor
        blockAnchorTexture.DrawCentered(Center);

        base.Render();
    }

}
