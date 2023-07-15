using MonoMod.Utils;

namespace Celeste.Mod.CommunalHelper.Entities;

[Pooled]
public class DreamBlockDebris : Debris
{
    private static readonly Color[] activeParticleColors = new Color[] {
            Calc.HexToColor("FFEF11"),
            Calc.HexToColor("08A310"),
            Calc.HexToColor("FF00D0"),
            Calc.HexToColor("5FCDE4"),
            Calc.HexToColor("E0564C")
    };

    public DreamBlockDummy Block;

    protected Sprite sprite;

    private Color? activePointColor;
    private Color? disabledPointColor;
    private Vector2 pointOffset;

    private readonly DynamicData baseData;

    public DreamBlockDebris()
    {
        baseData = new(typeof(Debris), this);
    }

    public DreamBlockDebris Init(Vector2 pos, DreamBlockDummy block = null)
    {
        orig_Init(pos, '1');

        Block = block ?? new DreamBlockDummy(this);
        Image image = baseData.Get<Image>("image");
        Remove(image);
        Sprite sprite = new(GFX.Game, "objects/CommunalHelper/dreamMoveBlock/");
        float speed = Calc.Random.NextFloat(0.3f) + 0.1f;
        sprite.AddLoop("active", "debris", speed);
        sprite.AddLoop("disabled", "disabledDebris", speed);
        sprite.CenterOrigin();
        sprite.Color = image.Color;
        sprite.Rotation = image.Rotation;
        sprite.Scale = image.Scale;
        sprite.FlipX = image.FlipX;
        sprite.FlipY = image.FlipY;

        Add(sprite);
        sprite.Play(Block.PlayerHasDreamDash ? "active" : "disabled", randomizeFrame: true);
        baseData.Set("image", this.sprite = sprite);

        if (Calc.Random.Next(4) == 0)
        {
            activePointColor = Calc.Random.Choose(activeParticleColors);
            disabledPointColor = Color.LightGray * (0.5f + (Calc.Random.Choose(0, 1, 1, 2, 2, 2) / 2f * 0.5f));
            pointOffset = new Vector2(Calc.Random.Next(-2, 2), Calc.Random.Next(-2, 2));
        }

        return this;
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);

        if (Block.Scene != scene)
            scene.Add(Block);
    }

    public override void Update()
    {
        if (Block.PlayerHasDreamDash)
        {
            if (sprite.CurrentAnimationID == "disabled")
            {
                int frame = sprite.CurrentAnimationFrame;
                sprite.Play("active");
                sprite.SetAnimationFrame(frame);
            }
        }
        else
        {
            if (sprite.CurrentAnimationID == "active")
            {
                int frame = sprite.CurrentAnimationFrame;
                sprite.Play("disabled");
                sprite.SetAnimationFrame(frame);
            }
        }

        base.Update();
        sprite.Color = Color.White * baseData.Get<float>("alpha");
    }

    public override void Render()
    {
        base.Render();
        Color? pointColor = Block.PlayerHasDreamDash ? activePointColor : disabledPointColor;
        if (activePointColor != null)
            Draw.Point(Center + pointOffset, pointColor.Value);
    }

}
