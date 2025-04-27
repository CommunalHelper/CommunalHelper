using Celeste.Mod.Core;
using System.Collections;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/LightningController")]
[Tracked]
public class LightningController : Entity
{
    private class Flash : Entity
    {
        private readonly Color color;
        private float alpha = 1.0f;
        private readonly float opacity;

        public Flash(Color color, float opacity, int depth, float time)
        {
            this.color = color;
            Depth = depth;
            this.opacity = opacity;
            Add(new Coroutine(FlashRoutine(time)));
        }

        public override void Render()
        {
            base.Render();

            Camera cam = (Scene as Level).Camera;
            Draw.Rect(cam.Position - Vector2.One, 320 + 2, 180 + 2, color * alpha * opacity);
        }

        public IEnumerator FlashRoutine(float time)
        {
            float t = time;
            while (t > 0f)
            {
                alpha = t / time;
                yield return null;
                t -= Engine.DeltaTime;
            }

            alpha = 0f;
            RemoveSelf();
        }
    }

    public LightningController(EntityData data, Vector2 _)
        : this(
              data.Float("minDelay", 5.0f),
              data.Float("maxDelay", 10.0f),
              data.Float("startupDelay", 4.0f),
              data.Float("flash", 0.3f),
              data.Float("flashDuration", 0.5f),
              data.Int("depth", 0),
              data.Float("shakeAmount", 0.3f),
              data.Attr("sfx", SFX.game_10_lightning_strike),
              data.Float("probability", 1.0f),
              data.HexColor("color", Color.White),
              data.HexColor("flashColor", Color.White)
          )
    { }

    public LightningController(float minDelay, float maxDelay, float startupDelay, float flash, float flashDuration, int depth, float shakeAmount, string sfx, float probability, Color color, Color flashColor)
        : base()
    {
        Add(new Coroutine(Routine(minDelay, maxDelay, startupDelay, flash, flashDuration, depth, shakeAmount, sfx, probability, color, flashColor)));
    }

    // Don't need to store the controller options in the controller itself.
    private IEnumerator Routine(float minDelay, float maxDelay, float startupDelay, float flash, float flashDuration, int depth, float shakeAmount, string sfx, float probability, Color color, Color flashColor)
    {
        yield return startupDelay;

        if (probability == 0)
            RemoveSelf();

        Level level = Scene as Level;
        Camera cam = level.Camera;

        bool flashes = flashDuration > 0 && flash > 0 && CoreModule.Settings.AllowLightning;

        while (true)
        {
            yield return MathHelper.Lerp(minDelay, maxDelay, Calc.Random.NextFloat());

            if (Calc.Random.NextFloat() >= probability)
                continue;

            Vector2 at = cam.Position + new Vector2(160, -90);
            int seed = Calc.Random.Next();

            level.DirectionalShake(Vector2.UnitY, shakeAmount);
            level.Add(new ColoredLightningStrike(at, color, seed, 300f, 0f, depth));
            if (flashes)
                level.Add(new Flash(flashColor, flash, depth - 1, flashDuration));

            Audio.Play(sfx);
        }
    }
}

public class ColoredLightningStrike : Entity
{
    private class Node
    {
        private readonly ColoredLightningStrike lightning;

        public Vector2 Position;
        public float Size;
        public List<Node> Children = new();

        public Node(ColoredLightningStrike lightning, Vector2 position, float size)
        {
            this.lightning = lightning;
            Position = position;
            Size = size;
        }

        public void Wiggle(Random rand)
        {
            Position.X += rand.Range(-2, 2);
            if (Position.Y != 0f)
                Position.Y += rand.Range(-1, 1);

            foreach (Node child in Children)
                child.Wiggle(rand);
        }

        public void Render(Vector2 offset, float scale)
        {
            float num = Size * scale;
            foreach (Node child in Children)
            {
                Vector2 vector = (child.Position - Position).SafeNormalize();
                Draw.Line(offset + Position, offset + child.Position + vector * num * 0.5f, lightning.color, num);
                child.Render(offset, scale);
            }
        }
    }

    private bool on;
    private float scale;

    private readonly Random rand;
    private readonly float strikeHeight;
    private readonly Color color;

    private Node strike;

    public ColoredLightningStrike(Vector2 position, Color color, int seed, float height, float delay, int depth)
    {
        Position = position;
        Depth = depth;

        rand = new Random(seed);
        strikeHeight = height;
        this.color = color;

        Add(new Coroutine(Routine(delay)));
    }

    private IEnumerator Routine(float delay)
    {
        if (delay > 0f)
            yield return delay;

        scale = 1f;
        GenerateStikeNodes(-1, 10f);

        for (int i = 0; i < 5; i++)
        {
            on = true;
            yield return (1f - i / 5f) * 0.1f;

            scale -= 0.2f;
            on = false;
            strike.Wiggle(rand);

            yield return 0.01f;
        }

        RemoveSelf();
    }

    private void GenerateStikeNodes(int direction, float size, Node parent = null)
    {
        parent ??= strike = new Node(this, Vector2.Zero, size);

        if (parent.Position.Y >= strikeHeight)
            return;

        float xNext = direction * rand.Range(-8, 20);
        float yNext = rand.Range(8, 16);
        float sizeNext = (0.25f + (1f - (parent.Position.Y + yNext) / strikeHeight) * 0.75f) * size;
        Node node = new(this, parent.Position + new Vector2(xNext, yNext), sizeNext);
        parent.Children.Add(node);

        GenerateStikeNodes(direction, size, node);

        if (rand.Chance(0.1f))
        {
            Node node2 = new(this, parent.Position + new Vector2(-xNext, yNext * 1.5f), sizeNext);
            parent.Children.Add(node2);

            GenerateStikeNodes(-direction, size, node2);
        }
    }

    public override void Render()
    {
        if (on)
            strike.Render(Position, scale);
    }
}
