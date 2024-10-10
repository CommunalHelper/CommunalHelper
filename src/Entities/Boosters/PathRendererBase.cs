﻿namespace Celeste.Mod.CommunalHelper.Entities;

public enum PathStyle
{
    Arrow,
    Line,
    DottedLine,
    Point
}

public abstract class PathRendererBase<T> : Entity where T : CustomBooster
{
    public readonly T Booster;

    public float Alpha;

    private readonly PathStyle style;

    private float lerp;

    private Vector2? lastPlayerCenter;
    protected Color Color { get; private set; }

    private readonly Color[] colors;

    public PathRendererBase(float alpha, PathStyle style, Color[] colors, T booster)
    {
        this.style = style;
        Booster = booster;
        Alpha = alpha;

        this.colors = colors;

        Depth = Depths.DreamBlocks + 1;
    }

    public void ResetRainbow()
    {
        Color = Util.ColorArrayLerp(lerp = Calc.Random.Range(0, 8), colors);
    }

    public override void Update()
    {
        base.Update();
        if (Booster.BoostingPlayer)
            Color = Util.ColorArrayLerp(lerp += Engine.DeltaTime * 8f, colors);
    }

    public void DrawPathLine(Vector2 pos, Vector2 dir, Vector2 perp, float offset, Player player, Color lerp, float alpha = 1f)
    {
        float sin = ((float) Math.Sin(offset + (Scene.TimeActive * 6f)) * 0.3f) + 1f;

        float highlight = .25f;
        if (player != null)
            lastPlayerCenter = player.Center;
        if (lastPlayerCenter.HasValue)
        {
            float dSquared = Vector2.DistanceSquared(lastPlayerCenter.Value, pos);
            highlight = dSquared <= 6400f ? Calc.ClampedMap((float) Math.Sqrt(dSquared), 0, 80) : 1;
        }

        float lineHighlight = ((1 - highlight) * 2.5f) + 0.75f;
        float alphaHighlight = 1 - Calc.Clamp(highlight, 0.01f, 0.8f);
        Color color = Color.Lerp(Color.White, lerp, 1 - highlight) * alphaHighlight * Alpha * alpha;

        switch (style)
        {
            case PathStyle.Point:
            {
                Draw.Point(pos + (dir * sin * 0.5f), color);
                break;
            }

            case PathStyle.DottedLine:
            {
                float lineLength = lineHighlight * sin;
                if (lineLength < 1f)
                    Draw.Point(pos, color);
                else
                    Draw.Line(pos, pos + (dir * lineLength), color);
                break;
            }

            case PathStyle.Line:
            {
                float lineLength = lineHighlight * sin;
                Vector2 lineOffset = perp * lineLength;
                Draw.Line(pos + lineOffset, pos - lineOffset, color);
                break;
            }

            case PathStyle.Arrow:
            default:
            {
                float lineLength = lineHighlight * sin;
                Vector2 lineOffset = perp * lineLength;
                if (lineLength < 1f)
                {
                    Draw.Point(pos, color);
                }
                else
                {
                    Draw.Line(pos, pos - lineOffset + (-dir * lineLength), color);
                    Draw.Line(pos, pos + lineOffset + (-dir * (lineLength - 1)), color);
                }
                break;
            }
        }
    }
}
