using FMOD.Studio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CommunalHelper.Entities.SSC26;
[CustomEntity("CommunalHelper/WeightMagnet")]
[Tracked]
public class WeightMagnet : Entity
{
    private Sprite sprite;
    private MagnetField field;
    private string direction;
    private int fieldDistance = 96;

    public WeightMagnet(EntityData data, Vector2 offset) : base(data.Position + offset)
    {
        Collider = new Hitbox(32, 32, -16, -16);
        Add(sprite = GFX.SpriteBank.Create("communalhelper_weight_magnet"));
        direction = data.Attr("direction", "Down");
        fieldDistance = data.Int("fieldDistance", 96);
        field = new MagnetField(Position, fieldDistance, direction);
        Depth = Depths.Below - 50;

        sprite.Rotation = direction switch
        {
            "Up" => MathF.PI,
            "Down" => 0f,
            "Left" => MathF.PI * 0.5f,
            "Right" => MathF.PI * -0.5f,
            _ => throw new Exception($"Invalid direction {direction} for Magnet at {Position}"),
        };
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);
        scene.Add(field);
    }

    public override void Removed(Scene scene)
    {
        base.Removed(scene);
        field.RemoveSelf();
    }

    public void Break()
    {
        Audio.Play("event:/game/05_mirror_temple/magneticblock_break", Position);
        sprite.Play("break");
        Collidable = false;
        field.RemoveSelf();
        sprite.OnFinish = (anim) =>
        {
            RemoveSelf();
        };

        // Spawn debris
        float step_amt = 6f;
        for (int i = 0; i < Width / step_amt; i++)
        {
            for (int j = 0; j < Height / step_amt; j++)
            {
                int x = (int) (Left + i * step_amt);
                int y = (int) (Top + j * step_amt);
                if (!Scene.CollideCheck<Solid>(new Rectangle(x, y, 8, 8)))
                {
                    Scene.Add(Engine.Pooler.Create<Debris>().Init(TopLeft + new Vector2(4 + i * step_amt, 4 + j * step_amt), '8', playSound: true).BlastFrom(Center));
                }
            }
        }
        // SFX
        EventInstance sfx = Audio.Play("event:/game/general/diamond_touch", Position);
        sfx.setPitch(0.5f);
        sfx.setVolume(1.4f);
    }
}

[Tracked]
public class MagnetField : Entity
{
    private static readonly float acceleration = 1300f;
    private static readonly int width = 32;
    private static MTexture texture = GFX.Game["objects/CommunalHelper/weightAndChain/attractionfield00"];

    public Vector2 Attraction { get; set; }

    public string Direction { get; private set; }

    private static Vector2 GetAttraction(string extendDir)
    {
        return extendDir switch
        {
            "Up" => new Vector2(0, acceleration),
            "Down" => new Vector2(0, -acceleration),
            "Left" => new Vector2(acceleration, 0),
            "Right" => new Vector2(-acceleration, 0),
            _ => Vector2.Zero,
        };
    }

    private static Hitbox GetHitbox(int distance, string extendDir)
    {
        return extendDir switch
        {
            "Up" => new Hitbox(width, distance, -width / 2, -distance),
            "Down" => new Hitbox(width, distance, -width / 2, 0),
            "Left" => new Hitbox(distance, width, -distance, -width / 2),
            "Right" => new Hitbox(distance, width, 0, -width / 2),
            _ => new Hitbox(width, width, -width / 2, -width / 2),
        };
    }

    public MagnetField(Vector2 pos, int distance, string extendDirection) : base(pos)
    {
        Direction = extendDirection;
        Attraction = GetAttraction(extendDirection);
        Collider = GetHitbox(distance, extendDirection);
        Depth = Depths.Below;
    }

    override public void Render()
    {
        base.Render();
        // TODO: prettier rendering

        for (int i = 0; i < (Direction is "Up" or "Down" ? Collider.Height : Collider.Width) / texture.Height; i++)
        {
            Vector2 offset = Direction switch
            {
                "Up" => new Vector2(0, -i * texture.Height),
                "Down" => new Vector2(0, i * texture.Height),
                "Left" => new Vector2(-i * texture.Height, 0),
                "Right" => new Vector2(i * texture.Height, 0),
                _ => Vector2.Zero,
            };
            float rotation = Direction switch
            {
                "Up" => (float) Math.PI,
                "Down" => 0f,
                "Left" => (float) Math.PI / 2f,
                "Right" => -(float) Math.PI / 2f,
                _ => 0f,
            };
            texture.Draw(Position + offset, Vector2.UnitX * 0.5f * texture.Width, Color.White, 1, rotation);
        }
    }
}
