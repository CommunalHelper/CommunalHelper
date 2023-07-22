namespace Celeste.Mod.CommunalHelper.Entities;

public sealed class AeroScreen_Wind : AeroScreen
{
    public override float Period => 0.045f;

    private struct Particle
    {
        public Vector2 last, pos, vel;
        public float depth;
    }

    public Vector2 Wind { get; set; }
    public Color Color { get; set; }

    private readonly int width, height;
    private readonly Particle[] particles;

    public AeroScreen_Wind(int width, int height, Vector2? velocity = null)
    {
        this.width = width;
        this.height = height;

        Vector2 vel = velocity ?? Vector2.Zero;
        int count = width * height / 48;
        particles = new Particle[count];
        for (int i = 0; i < count; i++)
        {
            particles[i].last = particles[i].pos = new Vector2(Calc.Random.Range(1, width - 1), Calc.Random.Range(1, height - 1));
            particles[i].vel = vel;
            particles[i].depth = Calc.Random.NextFloat() * 0.75f + 0.25f;
        }
    }

    public void MulitplyVelocities(float factor)
    {
        for (int i = 0; i < particles.Length; i++)
            particles[i].vel *= factor;
    }

    public override void Update()
    {
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].vel = Calc.Approach(particles[i].vel, Wind, Period * 200);

            particles[i].last = particles[i].pos;
            particles[i].pos += particles[i].depth * particles[i].vel * Period;

            // wrap
            Vector2 unwrapped = particles[i].pos;
            particles[i].pos.X = Util.Mod(particles[i].pos.X, width - 1);
            particles[i].pos.Y = Util.Mod(particles[i].pos.Y, height - 1);
            if (unwrapped != particles[i].pos)
                particles[i].last = particles[i].pos;
        }
    }

    public override void Render()
    {
        foreach (Particle particle in particles)
        {
            Color color = Color.Lerp(Color.Transparent, Color, particle.depth);
            if (Vector2.DistanceSquared(particle.last, particle.pos) <= 1.0f)
                Draw.Point(particle.pos + Block.Position, color);
            else
                Draw.Line(particle.last + Block.Position, particle.pos + Block.Position, color);
        }
    }

    public override void Finish() { }
}
