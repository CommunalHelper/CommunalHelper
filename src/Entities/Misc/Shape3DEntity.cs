using Celeste.Mod.CommunalHelper.Components;
using Celeste.Mod.CommunalHelper.Utils;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/Shape3DEntity")]
public class Shape3DEntity : Entity
{
    private readonly Vector3 pos;
    private readonly Vector3 scale;
    
    private float yaw, pitch, roll;
    private readonly float speedYaw, speedPitch, speedRoll;

    private readonly Shape3D shape3D;
    
    public Shape3DEntity(EntityData data, Vector2 offset)
        : this(data.Position + offset, data.Int("depth"),
            BuildMeshFromParameters(data.Attr("modelParameters")), data.Attr("modelTexture"),
            data.Vector3("position", Vector3.Zero), data.Vector3("scale", Vector3.One),
            data.Float("yaw") * Calc.DegToRad, data.Float("pitch") * Calc.DegToRad, data.Float("roll") * Calc.DegToRad,
            data.Float("speedYaw") * Calc.DegToRad, data.Float("speedPitch") * Calc.DegToRad, data.Float("speedRoll") * Calc.DegToRad,
            data.HexColor("tint", Color.White), data.Float("highlightStrength"), data.Float("rainbowMix"))
    { }

    private static Mesh<VertexPCTN> BuildMeshFromParameters(string parameters)
    {
        if (string.IsNullOrEmpty(parameters))
            return null;
        
        string[] parts = parameters.Split(":", StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || string.IsNullOrEmpty(parts[0]) || string.IsNullOrEmpty(parts[1]))
            return null;

        string[] args = parts[1].Split(",", StringSplitOptions.TrimEntries);
        // god i wish we had macros in c#
        return parts[0] switch
        {
            "gear" => args.Length == 7
                    && int.TryParse(args[0], out int teeth)
                    && float.TryParse(args[1], out float depth)
                    && float.TryParse(args[2], out float slope)
                    && float.TryParse(args[3], out float innerRadius)
                    && float.TryParse(args[4], out float thickness)
                    && float.TryParse(args[5], out float scale)
                ? Shapes.Gear(teeth, depth, slope, innerRadius, thickness, scale, Calc.HexToColor(args[6]))
                : null,
            "box" => args.Length == 4
                    && float.TryParse(args[0], out float sx)
                    && float.TryParse(args[1], out float sy)
                    && float.TryParse(args[2], out float sz)
                ? Shapes.Box(Vector3.Zero, sx, sy, sz, new MTexture { LeftUV = 0f, TopUV = 0f, RightUV = 1f, BottomUV = 1f }, Calc.HexToColor(args[3]))
                : null,
            "icosahedron" => args.Length == 3
                    && float.TryParse(args[1], out float rainbow)
                    && float.TryParse(args[2], out float scale)
                ? Shapes.Icosahedron(Calc.HexToColor(args[0]), rainbow, scale)
                : null,
            "icosphere" => args.Length == 4
                    && int.TryParse(args[0], out int subdivisions)
                    && float.TryParse(args[2], out float rainbow)
                    && float.TryParse(args[3], out float scale)
                    && Shapes.GenerateIcosphereGeometry(subdivisions) is { } geometry
                ? Shapes.BuildMesh(geometry.Item1, geometry.Item2, Calc.HexToColor(args[1]), rainbow, scale)
                : null,
            "rock" => args.Length == 3
                    && float.TryParse(args[1], out float rainbow)
                    && float.TryParse(args[2], out float scale) 
                ? Shapes.Rock(Calc.HexToColor(args[0]), rainbow, scale)
                : null,
            "halfring" => args.Length == 3
                    && float.TryParse(args[0], out float height)
                    && float.TryParse(args[1], out float thickness) 
                ? Shapes.HalfRing(height, thickness, Calc.HexToColor(args[2]))
                : null,
            "obj" => args.Length == 1
                ? Shapes.Obj(args[0])
                : null,
            _ => null
        };
    }

    public Shape3DEntity(Vector2 position, int depth,
        Mesh<VertexPCTN> mesh, string modelTexture,
        Vector3 pos, Vector3 scale,
        float yaw, float pitch, float roll,
        float speedYaw, float speedPitch, float speedRoll,
        Color tint, float highlightStrength, float rainbowMix) : base(position)
    {
        Depth = depth;

        this.pos = pos;
        this.scale = scale;
        
        this.yaw = yaw;
        this.pitch = pitch;
        this.roll = roll;
        this.speedYaw = speedYaw;
        this.speedPitch = speedPitch;
        this.speedRoll = speedRoll;

        if (mesh is not null && Shapes.Texture(modelTexture) is { } texture)
            Add(shape3D = new Shape3D([Tuple.Create(mesh, texture)])
            {
                Depth = depth,
                Matrix = CreateTransformation(this.pos, this.scale, this.yaw, this.pitch, this.roll),
                Tint = tint.ToVector4(),
                HighlightStrength = highlightStrength,
                RainbowMix = rainbowMix
            });
    }

    private static Matrix CreateTransformation(Vector3 pos, Vector3 scale, float yaw, float pitch, float roll)
        => Matrix.CreateScale(scale) * Matrix.CreateFromYawPitchRoll(yaw, pitch, roll) * Matrix.CreateTranslation(pos);

    public override void Update()
    {
        yaw += speedYaw * Engine.DeltaTime;
        yaw %= MathF.Tau;
        pitch += speedPitch * Engine.DeltaTime;
        pitch %= MathF.Tau;
        roll += speedRoll * Engine.DeltaTime;
        roll %= MathF.Tau;

        if (shape3D is not null)
            shape3D.Matrix = CreateTransformation(pos, scale, yaw, pitch, roll);
    }
}
