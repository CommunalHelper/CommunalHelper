using Celeste.Mod.CommunalHelper.Utils;

namespace Celeste.Mod.CommunalHelper.Entities;

public sealed class AeroScreen_ParticleBurst : AeroScreen
{
    private struct Particle
    {
        public Vector2 pos, vel;
        public float life, lifetime;
    }

    public override float Period => 0.065f;

    public Color Color { get; set; } = Color.White;

    private readonly Particle[] particles;
    private float flash = 1f;

    public AeroScreen_ParticleBurst(AeroBlock block)
    {
        particles = new Particle[(int)(block.Width * 1.2f)];
        for (int i = 0; i < particles.Length; ++i)
        {
            float x = Calc.Random.NextFloat(block.Width);

            particles[i].pos = Vector2.UnitX * x;

            float angle = MathHelper.PiOver2 + MathHelper.WrapAngle(Calc.Random.NextAngle()) * 0.1f;
            float xp = x / block.Width;
            float mag = 0.8f - (float)Math.Sin(xp * Math.PI) * 0.5f;
            particles[i].vel = Calc.AngleToVector(angle, Calc.Random.NextFloat(block.Height * mag * 2f));

            particles[i].life = particles[i].lifetime = 1f + Calc.Random.NextFloat(4f);
        }
    }

    public override void Update()
    {
        bool done = true;

        for (int i = 0; i < particles.Length; ++i)
        {
            particles[i].pos += particles[i].vel * Period;
            particles[i].vel = Calc.Approach(particles[i].vel, -Vector2.UnitY * 150, Period * 30);
            particles[i].life = Calc.Approach(particles[i].life, 0f, Period);

            done &= particles[i].life == 0f;
        }

        flash = Calc.Approach(flash, 0f, Period * 1.2f);

        if (done)
            Block.RemoveScreenLayer(this);
    }

    public override void Render()
    {
        Rectangle bounds = Block.Collider.Bounds;

        MTexture texture = GFX.Game["particles/CommunalHelper/big_circle"];
        Vector2 size = new(texture.Width, texture.Height);

        foreach (Particle particle in particles)
        {
            if (particle.life == 0.0f)
                continue;
            float percent = particle.life / particle.lifetime;

            float scale = Ease.CubeIn(percent);

            Vector2 pos = particle.pos + Block.Position;
            RectangleF rect = new(pos - scale * size / 2f, pos + scale * size / 2f);

            if (!rect.Intersects(bounds))
                continue;
            RectangleF clamped = RectangleF.ClampTo(rect, bounds);

            int tx = (int)(clamped.X - rect.X);
            int ty = (int)(clamped.Y - rect.Y);
            int tw = (int)(clamped.Width / scale);
            int th = (int)(clamped.Height / scale);

            texture.GetSubtexture(tx, ty, tw, th)
                   .DrawCentered(clamped.Center, Color, scale);
        }

        Draw.Rect(bounds, Color * flash);
    }

    public override void Finish() { }
}
