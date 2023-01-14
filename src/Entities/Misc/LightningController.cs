using System.Collections;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/LightningController")]
[Tracked]
public class LightningController : Entity
{
    private class Flash : Entity
    {
        private float alpha = 1.0f;
        private readonly float opacity;

        public Flash(float opacity, int depth, float time)
        {
            Depth = depth;
            this.opacity = opacity;
            Add(new Coroutine(FlashRoutine(time)));
        }

        public override void Render()
        {
            base.Render();

            Camera cam = (Scene as Level).Camera;
            Draw.Rect(cam.Position - Vector2.One, 320 + 2, 180 + 2, Color.White * alpha * opacity);
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
              data.Float("probability", 1.0f)
          )
    { }

    public LightningController(float minDelay, float maxDelay, float startupDelay, float flash, float flashDuration, int depth, float shakeAmount, string sfx, float probability)
        : base()
    {
        Add(new Coroutine(Routine(minDelay, maxDelay, startupDelay, flash, flashDuration, depth, shakeAmount, sfx, probability)));
    }

    // Don't need to store the controller options in the controller itself.
    private IEnumerator Routine(float minDelay, float maxDelay, float startupDelay, float flash, float flashDuration, int depth, float shakeAmount, string sfx, float probability)
    {
        yield return startupDelay;

        if (probability == 0)
            RemoveSelf();

        Level level = Scene as Level;
        Camera cam = level.Camera;

        bool flashes = flashDuration > 0 && flash > 0 && !Settings.Instance.DisableFlashes;

        while (true)
        {
            yield return MathHelper.Lerp(minDelay, maxDelay, Calc.Random.NextFloat());

            if (Calc.Random.NextFloat() >= probability)
                continue;

            Vector2 at = cam.Position + new Vector2(160, -90);
            int seed = Calc.Random.Next();

            level.DirectionalShake(Vector2.UnitY, shakeAmount);
            level.Add(new LightningStrike(at, seed, 300f, 0f) { Depth = depth });
            if (flashes)
                level.Add(new Flash(flash, depth - 1, flashDuration));

            Audio.Play(sfx);
        }
    }
}
