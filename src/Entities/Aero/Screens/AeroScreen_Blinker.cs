namespace Celeste.Mod.CommunalHelper.Entities;

internal class AeroScreen_Blinker : AeroScreen
{
    public override float Period => 0.065f;

    public MTexture Icon { get; set; }
    public Vector2 Offset { get; set; } = Vector2.Zero;
    public Color IconColor { get; set; } = Color.White;
    public Color BackgroundColor { get; set; } = Color.Transparent;
    public string Sound { get; set; } = null;
    public bool Complete { get; set; }

    public float FadeIn { get; set; } = 0.2f;
    public float Hold { get; set; } = 0.2f;
    public float FadeOut { get; set; } = 1.0f;
    public float Wait { get; set; } = 0.0f;

    private float timer, lerp;

    public AeroScreen_Blinker(MTexture icon)
    {
        Icon = icon;
    }

    public override void Update()
    {
        timer = Calc.Approach(timer, 0, Period);
        if (timer == 0.0f)
        {
            if (Complete)
            {
                Block.RemoveScreenLayer(this);
                return;
            }

            timer = FadeIn + Hold + FadeOut + Wait;
            if (!string.IsNullOrEmpty(Sound))
                Audio.Play(Sound, Block.Center);
        }

        float zero = Hold + FadeOut + Wait;

        if (FadeIn >= 0.0f && timer - zero > 0.0f)
            lerp = 1 - (timer - zero) / FadeIn;
        else if (Hold >= 0.0f && timer - (zero -= Hold) > 0.0f)
            lerp = 1f;
        else if (FadeOut >= 0.0f && timer - (zero -= FadeOut) > 0.0f)
            lerp = (timer - zero) / FadeOut;
        else
            lerp = 0.0f;
    }

    public override void Render()
    {
        float ease = Ease.CubeOut(Math.Min(lerp, 1.0f));

        Color bg = Color.Lerp(Color.Transparent, BackgroundColor, ease);
        Draw.Rect(Block.Collider.Bounds, bg);

        Color fg = Color.Lerp(Color.Transparent, IconColor, ease);
        Icon?.Draw(Block.Position + Offset, Vector2.Zero, fg);
    }

    public override void Finish() { }
}
