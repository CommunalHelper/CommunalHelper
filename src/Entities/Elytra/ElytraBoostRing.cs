using Celeste.Mod.CommunalHelper.States;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/ElytraBoostRing")]
public class ElytraBoostRing : ElytraRing
{
    private struct Line
    {
        public SimpleCurve curve;
        public float t;
        public bool inv;
    }

    private readonly Line[] lines;

    private readonly float speed, duration;
    private readonly bool bidirectional;
    private readonly bool refill;

    public override float Delay => 0.1f;
    public override string TraversalSFX => CustomSFX.game_elytra_rings_boost;

    public ElytraBoostRing(EntityData data, Vector2 offset)
        : this(
            data.Position + offset,
            data.Nodes[0] + offset,
            data.Float("speed", 240.0f),
            data.Float("duration", 0.5f),
            data.Bool("refill", false),
            data.Bool("bidirectional", false)
        )
    { }

    public ElytraBoostRing(Vector2 a, Vector2 b, float speed = 240.0f, float duration = 0.5f, bool refill = false, bool bidirectional = false)
        : base(a, b, Color.Teal)
    {
        this.speed = speed;
        this.duration = duration;
        this.bidirectional = bidirectional;
        this.refill = refill;

        int count = (int)Vector2.Distance(a, b) / 5;
        lines = new Line[count];
        for (int i = 0; i < count; i++)
        {
            ResetLine(i);
            lines[i].t = Calc.Random.NextFloat();
        }

        Add(new SoundSource(CustomSFX.game_elytra_rings_booster_ambience));
    }

    private void ResetLine(int index)
    {
        lines[index].t = 0.0f;
        lines[index].inv = bidirectional && Calc.Random.Chance(0.5f);

        Vector2 control = Vector2.Lerp(A, B, Calc.Random.Range(0.1f, 0.9f));

        float along = Calc.Random.Range(-16f, 16f);
        Vector2 a = control - Direction * Calc.Random.Range(16, 32) + Direction.Perpendicular() * along;
        Vector2 b = control + Direction * Calc.Random.Range(16, 32) + Direction.Perpendicular() * along;

        lines[index].curve = new(a, b, control);
    }

    public override void OnPlayerTraversal(Player player, int sign, bool shake = true)
    {
        if (Direction == Vector2.Zero)
            return;

        if (!bidirectional)
            sign = 1;

        base.OnPlayerTraversal(player, sign);
        player.ElytraLaunch(Direction * speed * sign, duration);
        if (refill)
            player.RefillElytra();
    }

    public override void Update()
    {
        base.Update();
        for (int i = 0; i < lines.Length; i++)
        {
            lines[i].t += Engine.DeltaTime * 3.5f;
            if (lines[i].t >= 1.0f)
                ResetLine(i);
        }
    }

    public override void Render()
    {
        base.Render();

        foreach (Line line in lines)
        {
            float t = line.inv ? 1 - line.t : line.t;

            float w = Ease.SineInOut(Ease.UpDown(t));

            Vector2 a = line.curve.GetPoint(t + w * 0.2f);
            Vector2 b = line.curve.GetPoint(t);
            Vector2 c = line.curve.GetPoint(t - w * 0.2f);

            Color color = Color.Lerp(Color.Transparent, Color.White, w * 0.4f);

            Draw.Line(a, b, color);
            Draw.Line(b, c, color);
        }

        float angle = MathHelper.PiOver4 + Direction.Angle();
        var arrow = GFX.Game["objects/CommunalHelper/elytraRing/arrow"];

        if (bidirectional)
        {
            Vector2 along = (B - A).SafeNormalize() * 6;

            float t1 = (Scene.TimeActive * 0.65f + 0.0f) % 1.0f;
            float t2 = (Scene.TimeActive * 0.65f + 0.33f) % 1.0f;
            float t3 = (Scene.TimeActive * 0.65f + 0.66f) % 1.0f;

            arrow.DrawCentered(Position + Direction * (t1 * 2 - 1) * 10 + along, Color.Red * Ease.CubeOut(Ease.UpDown(t1)) * 0.5f, 4f, angle);
            arrow.DrawCentered(Position + Direction * (t2 * 2 - 1) * 10 + along, Color.Red * Ease.CubeOut(Ease.UpDown(t2)) * 0.5f, 4f, angle);
            arrow.DrawCentered(Position + Direction * (t3 * 2 - 1) * 10 + along, Color.Red * Ease.CubeOut(Ease.UpDown(t3)) * 0.5f, 4f, angle);

            arrow.DrawCentered(Position - Direction * (t1 * 2 - 1) * 10 - along, Color.Red * Ease.CubeOut(Ease.UpDown(t1)) * 0.5f, 4f, angle + MathHelper.Pi);
            arrow.DrawCentered(Position - Direction * (t2 * 2 - 1) * 10 - along, Color.Red * Ease.CubeOut(Ease.UpDown(t2)) * 0.5f, 4f, angle + MathHelper.Pi);
            arrow.DrawCentered(Position - Direction * (t3 * 2 - 1) * 10 - along, Color.Red * Ease.CubeOut(Ease.UpDown(t3)) * 0.5f, 4f, angle + MathHelper.Pi);
        }
        else
        {
            float t1 = (Scene.TimeActive * 0.65f + 0.0f) % 1.0f;
            float t2 = (Scene.TimeActive * 0.65f + 0.5f) % 1.0f;

            arrow.DrawCentered(Position + Direction * (t1 * 2 - 1) * 10, Color.Red * Ease.CubeOut(Ease.UpDown(t1)) * 0.5f, 6f, angle);
            arrow.DrawCentered(Position + Direction * (t2 * 2 - 1) * 10, Color.Red * Ease.CubeOut(Ease.UpDown(t2)) * 0.5f, 6f, angle);
        }
    }
}
