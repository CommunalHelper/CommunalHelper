using Celeste.Mod.CommunalHelper.States;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/ElytraBoostRing")]
public class ElytraBoostRing : ElytraRing
{
    private struct Line
    {
        public SimpleCurve curve;
        public float t;
    }

    private readonly Line[] lines;

    private readonly float speed, duration;
    private readonly bool refill;

    public override float Delay => 0.1f;

    public ElytraBoostRing(EntityData data, Vector2 offset)
        : this(
            data.Position + offset,
            data.Nodes[0] + offset,
            data.Float("speed", 240.0f),
            data.Float("duration", 0.5f),
            data.Bool("refill", false)
        )
    { }

    public ElytraBoostRing(Vector2 a, Vector2 b, float speed = 240.0f, float duration = 0.5f, bool refill = false)
        : base(a, b, Color.Teal)
    {
        this.speed = speed;
        this.duration = duration;
        this.refill = refill;

        int count = (int)Vector2.Distance(a, b) / 5;
        lines = new Line[count];
        for (int i = 0; i < count; i++)
        {
            ResetLine(i);
            lines[i].t = Calc.Random.NextFloat();
        }
    }

    private void ResetLine(int index)
    {
        lines[index].t = 0.0f;

        Vector2 control = Vector2.Lerp(A, B, Calc.Random.Range(0.1f, 0.9f));

        float along = Calc.Random.Range(-16f, 16f);
        Vector2 a = control - Direction * Calc.Random.Range(16, 32) + Direction.Perpendicular() * along;
        Vector2 b = control + Direction * Calc.Random.Range(16, 32) + Direction.Perpendicular() * along;

        lines[index].curve = new(a, b, control);
    }

    public override void OnPlayerTraversal(Player player)
    {
        base.OnPlayerTraversal(player);

        if (Direction == Vector2.Zero)
            return;

        player.ElytraLaunch(Direction * speed, duration);
        if (refill)
            player.RefillElytra();

        Level level = Scene as Level;
        level.DirectionalShake(Direction);
        Input.Rumble(RumbleStrength.Strong, RumbleLength.Short);

        // particles
        // sound
        Audio.Play(SFX.game_06_feather_bubble_renew, Middle);

        TravelEffects();

        Celeste.Freeze(0.05f);

        return;
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
            float w = Ease.SineInOut(Ease.UpDown(line.t));

            Vector2 a = line.curve.GetPoint(line.t + w * 0.2f);
            Vector2 b = line.curve.GetPoint(line.t);
            Vector2 c = line.curve.GetPoint(line.t - w * 0.2f);

            Color color = Color.Lerp(Color.Transparent, Color.White, w * 0.4f);

            Draw.Line(a, b, color);
            Draw.Line(b, c, color);
        }

        float angle = MathHelper.PiOver4 + Direction.Angle();
        var arrow = GFX.Game["objects/CommunalHelper/elytraRing/arrow"];

        void DrawArrow(float t)
        {
            arrow.DrawCentered(Position + Direction * (t * 2 - 1) * 10, Color.Red * Ease.CubeOut(Ease.UpDown(t)) * 0.5f, 6f, angle);
        }

        DrawArrow((Scene.TimeActive * 0.65f + 0.0f) % 1.0f);
        DrawArrow((Scene.TimeActive * 0.65f + 0.5f) % 1.0f);
    }
}
