namespace Celeste.Mod.CommunalHelper.Entities;

public class AeroScreen_Percentage : AeroScreen
{
    public override float Period => 0.065f;

    public float Percentage { get; set; } = 0.0f;
    public Color Color { get; set; } = Color.White;
    public Color BackgroundColor { get; set; } = Color.Transparent;
    public bool ShowNumbers { get; set; } = true;

    private Color fg, bg;
    private float p;

    private struct LinePoint
    {
        public Vector2 pos;
        public float d;
    }

    private readonly LinePoint[] line = new LinePoint[14];
    private readonly float length;

    private readonly MTexture[] numbers;

    public AeroScreen_Percentage(int width, int height)
    {
        numbers = GFX.Game.GetAtlasSubtextures("objects/CommunalHelper/aero_block/numbers/number").ToArray();

        line[0].pos = new(width / 2, 2);
        line[1].pos = new(4, 2);
        line[2].pos = new(4, 5);
        line[3].pos = new(2, 5);
        line[4].pos = new(2, height - 5);
        line[5].pos = new(4, height - 5);
        line[6].pos = new(4, height - 3);
        line[7].pos = new(width - 5, height - 3);
        line[8].pos = new(width - 5, height - 5);
        line[9].pos = new(width - 3, height - 5);
        line[10].pos = new(width - 3, 5);
        line[11].pos = new(width - 5, 5);
        line[12].pos = new(width - 5, 2);
        line[13].pos = new(width / 2, 2);

        for (int i = 0; i < 14; i++)
        {
            Vector2 a = line[i].pos;
            Vector2 b = line[(i + 1) % line.Length].pos;
            line[i].d = length;
            length += Vector2.Distance(a, b);
        }

        fg = Color;
        bg = BackgroundColor;
        p = Calc.Clamp(Percentage, 0, 1);
    }

    public override void Update()
    {
        fg = Color;
        bg = BackgroundColor;
        p = Calc.Clamp(Percentage, 0, 1);
    }

    public override void Render()
    {
        Draw.Rect(Block.Collider.Bounds, bg);

        if (p > 0.0f)
        {
            for (int i = 0; i < line.Length - 1; i++)
            {
                if (line[i].d / length > p)
                    continue;

                LinePoint pa = line[i];
                LinePoint pb = line[i + 1];

                float currentDistance = MathHelper.Clamp(p * length, pa.d, pb.d);
                float linePercent = (currentDistance - pa.d) / (pb.d - pa.d);

                Vector2 a = Block.Position + pa.pos;
                Vector2 b = Vector2.Lerp(a, Block.Position + pb.pos, linePercent);
                Vector2 d = b - a;
                if (d.X == 0)
                {
                    // line is vertical
                    float top = Math.Min(a.Y, b.Y);
                    float bottom = Math.Max(a.Y, b.Y) + 1;
                    Draw.Rect(a.X, top, 1, bottom - top, fg);
                }
                else if (d.Y == 0)
                {
                    // line is horizontal
                    float left = Math.Min(a.X, b.X);
                    float right = Math.Max(a.X, b.X) + 1;
                    Draw.Rect(left, a.Y, right - left, 1, fg);
                }
            }
        }

        if (!ShowNumbers)
            return;

        int n = (int)(p * 100);
        Vector2 anchor = Block.Center;

        if (n < 10)
        {
            numbers[n].Draw(anchor - new Vector2(1, 2), Vector2.Zero, fg);
        }
        else if (n < 100)
        {
            numbers[n / 10].Draw(anchor + new Vector2(-3, -2), Vector2.Zero, fg);
            numbers[n % 10].Draw(anchor + new Vector2(1, -2), Vector2.Zero, fg);
        }
        else
        {
            numbers[1].Draw(anchor + new Vector2(-6,-2), Vector2.Zero, fg);
            numbers[0].Draw(anchor + new Vector2(-2,-2), Vector2.Zero, fg);
            numbers[0].Draw(anchor + new Vector2(2,-2), Vector2.Zero, fg);
        }
    }

    public override void Finish() { }
}
