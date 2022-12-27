using System.Collections;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/DreamZipMover")]
public class DreamZipMover : CustomDreamBlock
{
    private const float impactSoundOffset = 0.92f;

    public class PathRenderer : Entity
    {
        private class Segment
        {
            public bool Seen { get; set; }

            private readonly Vector2 from, to;
            private readonly Vector2 dir, twodir, perp;
            private readonly float length;

            private readonly Vector2 lineStartA, lineStartB;
            private readonly Vector2 lineEndA, lineEndB;

            public Rectangle Bounds { get; }

            private readonly Vector2 sparkAdd;

            private readonly float sparkDirStartA, sparkDirStartB;
            private readonly float sparkDirEndA, sparkDirEndB;
            private const float piOverEight = MathHelper.PiOver4 / 2f;
            private const float eightPi = 4 * MathHelper.TwoPi;

            public Segment(Vector2 from, Vector2 to)
            {
                this.from = from;
                this.to = to;

                dir = (to - from).SafeNormalize();
                twodir = 2 * dir;
                perp = dir.Perpendicular();
                length = Vector2.Distance(from, to);

                Vector2 threeperp = 3 * perp;
                Vector2 minusfourperp = -4 * perp;

                lineStartA = from + threeperp;
                lineStartB = from + minusfourperp;
                lineEndA = to + threeperp;
                lineEndB = to + minusfourperp;

                sparkAdd = (from - to).SafeNormalize(5f).Perpendicular();
                float angle = (from - to).Angle();
                sparkDirStartA = angle + piOverEight;
                sparkDirStartB = angle - piOverEight;
                sparkDirEndA = angle + MathHelper.Pi - piOverEight;
                sparkDirEndB = angle + MathHelper.Pi + piOverEight;

                Rectangle b = Util.Rectangle(from, to);
                b.Inflate(10, 10);

                Bounds = b;
            }

            public void Spark(Level level)
            {
                level.ParticlesBG.Emit(ZipMover.P_Sparks, from + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirStartA);
                level.ParticlesBG.Emit(ZipMover.P_Sparks, from - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirStartB);
                level.ParticlesBG.Emit(ZipMover.P_Sparks, to + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirEndA);
                level.ParticlesBG.Emit(ZipMover.P_Sparks, to - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirEndB);
            }

            public void Render(DreamZipMover zipMover, Color rope)
            {
                Draw.Line(lineStartA, lineEndA, rope);
                Draw.Line(lineStartB, lineEndB, rope);

                float shiftProgress = zipMover.percent * eightPi;
                for (float d = shiftProgress % 4f; d < length; d += 4f)
                {
                    Vector2 pos = dir * d;
                    Vector2 teethA = lineStartA + perp + pos;
                    Vector2 teethB = lineEndB - pos;

                    Color color = zipMover.dreamAesthetic ?
                                    (zipMover.PlayerHasDreamDash ?
                                        activeDreamColors[(int)Util.Mod((float)Math.Round((d - shiftProgress) / 4f), 9f)] :
                                        disabledDreamColors[(int)Util.Mod((float)Math.Round((d - shiftProgress) / 4f), 4f)]
                                    ) : ropeLightColor;
                    Draw.Line(teethA, teethA + twodir, color);
                    Draw.Line(teethB, teethB - twodir, color);
                }
            }

            public void RenderShadow(float percent)
            {
                Vector2 startA = lineStartA + Vector2.UnitY;
                Vector2 endB = lineEndB + Vector2.UnitY;

                Draw.Line(startA, lineEndA + Vector2.UnitY, Color.Black);
                Draw.Line(lineStartB + Vector2.UnitY, endB, Color.Black);

                for (float d = 4f - (percent * eightPi % 4f); d < length; d += 4f)
                {
                    Vector2 pos = dir * d;
                    Vector2 teethA = startA + perp + pos;
                    Vector2 teethB = endB - pos;
                    Draw.Line(teethA, teethA + twodir, Color.Black);
                    Draw.Line(teethB, teethB - twodir, Color.Black);
                }
            }
        }

        private readonly Rectangle bounds;
        private readonly Segment[] segments;

        private readonly DreamZipMover zipMover;

        private Level level;

        private readonly Vector2[] nodes;

        private static readonly Color[] activeDreamColors = new Color[9];
        private static readonly Color[] disabledDreamColors = new Color[4];

        static PathRenderer()
        {
            activeDreamColors[0] = Calc.HexToColor("FFEF11");
            activeDreamColors[1] = Calc.HexToColor("FF00D0");
            activeDreamColors[2] = Calc.HexToColor("08a310");
            activeDreamColors[3] = Calc.HexToColor("5fcde4");
            activeDreamColors[4] = Calc.HexToColor("7fb25e");
            activeDreamColors[5] = Calc.HexToColor("E0564C");
            activeDreamColors[6] = Calc.HexToColor("5b6ee1");
            activeDreamColors[7] = Calc.HexToColor("CC3B3B");
            activeDreamColors[8] = Calc.HexToColor("7daa64");

            disabledDreamColors[0] = Color.LightGray * 0.5f;
            disabledDreamColors[1] = Color.LightGray * 0.75f;
            disabledDreamColors[2] = Color.LightGray * 1;
            disabledDreamColors[3] = Color.LightGray * 0.75f;
        }

        public PathRenderer(DreamZipMover zipMover, Vector2[] nodes)
        {
            this.zipMover = zipMover;

            this.nodes = new Vector2[nodes.Length];

            Vector2 offset = new(zipMover.Width / 2f, zipMover.Height / 2f);

            Vector2 prev = this.nodes[0] = nodes[0] + offset;
            Vector2 min = prev, max = prev;

            segments = new Segment[nodes.Length - 1];
            for (int i = 0; i < segments.Length; ++i)
            {
                Vector2 node = this.nodes[i + 1] = nodes[i + 1] + offset;
                segments[i] = new(node, prev);

                min = Util.Min(min, node);
                max = Util.Max(max, node);

                prev = node;
            }

            bounds = new((int)min.X, (int)min.Y, (int)(max.X - min.X), (int)(max.Y - min.Y));
            bounds.Inflate(10, 10);

            Depth = Depths.SolidsBelow;
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            level = scene as Level;
        }

        public void CreateSparks()
        {
            foreach (Segment seg in segments)
                seg.Spark(level);
        }

        public override void Render()
        {
            Rectangle cameraBounds = level.Camera.GetBounds();

            if (!cameraBounds.Intersects(bounds))
                return;

            Color dreamRopeColor = zipMover.PlayerHasDreamDash ? ActiveLineColor : DisabledLineColor;
            Color color = Color.Lerp(zipMover.dreamAesthetic ? dreamRopeColor : ropeColor, ActiveLineColor, zipMover.ColorLerp);

            foreach (Segment seg in segments)
                if (seg.Seen = cameraBounds.Intersects(seg.Bounds))
                    seg.RenderShadow(zipMover.percent);

            foreach (Segment seg in segments)
                if (seg.Seen)
                    seg.Render(zipMover, color);

            float rotation = zipMover.percent * MathHelper.TwoPi;
            foreach (Vector2 node in nodes)
            {
                zipMover.cog.DrawCentered(node + Vector2.UnitY, Color.Black, 1f, rotation);
                if (zipMover.ColorLerp > 0f)
                    cogWhite.DrawCentered(node, Color.Lerp(Color.Transparent, ActiveLineColor, zipMover.ColorLerp), 1f, rotation);
                else
                    zipMover.cog.DrawCentered(node, Color.White, 1f, rotation);
            }

            zipMover.DrawBorder();
        }
    }
    private PathRenderer pathRenderer;

    private readonly SoundSource sfx;

    private readonly Vector2[] nodes;
    private readonly bool permanent;
    private readonly bool waits;
    private readonly bool ticking;
    private readonly bool dreamAesthetic;
    private readonly bool noReturn;

    private readonly MTexture cross;
    private MTexture cog;

    private static readonly Color ropeColor = Calc.HexToColor("663931");
    private static readonly Color ropeLightColor = Calc.HexToColor("9b6157");
    private static MTexture cogNormal, cogDream, cogDisabled, cogWhite;

    private float percent;

    public DreamZipMover(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        nodes = data.NodesWithPosition(offset);

        noReturn = data.Bool("noReturn");
        dreamAesthetic = data.Bool("dreamAesthetic");

        permanent = data.Bool("permanent");
        waits = data.Bool("waiting");
        ticking = data.Bool("ticking");

        Add(new Coroutine(Sequence()));
        Add(new LightOcclude());
        Add(sfx = new SoundSource
        {
            Position = new Vector2(Width / 2f, Height / 2f)
        });
        cross = GFX.Game["objects/CommunalHelper/dreamMoveBlock/x"];

        UpdateCogTexture();
    }

    private void UpdateCogTexture()
    {
        cog = dreamAesthetic ? (PlayerHasDreamDash ? cogDream : cogDisabled) : cogNormal;
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);

        scene.Add(pathRenderer = new(this, nodes));
    }

    public override void Removed(Scene scene)
    {
        scene.Remove(pathRenderer);
        pathRenderer = null;
        base.Removed(scene);
    }

    public override void Render()
    {
        Vector2 position = Position;
        Position += Shake;

        base.Render();

        if (noReturn)
            cross.DrawCentered(Center + baseData.Get<Vector2>("shake"));

        Position = position;
    }

    private void ScrapeParticlesCheck(Vector2 to)
    {
        const float threePiOverFour = 3f * MathHelper.Pi / 4f;
        const float piOverFour = MathHelper.PiOver4;

        if (!Scene.OnInterval(0.03f))
            return;

        bool movingV = to.Y != ExactPosition.Y;
        bool movingH = to.X != ExactPosition.X;

        if (movingV && !movingH)
        {
            int dir = Math.Sign(to.Y - ExactPosition.Y);
            Vector2 collisionPoint = (dir != 1) ? TopLeft : BottomLeft;
            int particleOffset = 4;

            if (dir == 1)
                particleOffset = Math.Min((int)Height - 12, 20);

            int particleHeight = (int)Height;
            if (dir == -1)
                particleHeight = Math.Max(16, (int)Height - 16);

            if (Scene.CollideCheck<Solid>(collisionPoint + new Vector2(-2f, dir * -2)))
                for (int i = particleOffset; i < particleHeight; i += 8)
                    SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, TopLeft + new Vector2(0f, i + (dir * 2f)), (dir == 1) ? -piOverFour : piOverFour);

            if (Scene.CollideCheck<Solid>(collisionPoint + new Vector2(Width + 2f, dir * -2)))
                for (int i = particleOffset; i < particleHeight; i += 8)
                    SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, TopRight + new Vector2(-1f, i + (dir * 2f)), (dir == 1) ? -threePiOverFour : threePiOverFour);

        }
        else if (movingH && !movingV)
        {
            int dir = Math.Sign(to.X - ExactPosition.X);
            Vector2 collisionPoint = (dir != 1) ? TopLeft : TopRight;
            int particleOffset = 4;

            if (dir == 1)
                particleOffset = Math.Min((int)Width - 12, 20);

            int particleWidth = (int)Width;
            if (dir == -1)
                particleWidth = Math.Max(16, (int)Width - 16);

            if (Scene.CollideCheck<Solid>(collisionPoint + new Vector2(dir * -2, -2f)))
                for (int i = particleOffset; i < particleWidth; i += 8)
                    SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, TopLeft + new Vector2(i + (dir * 2f), -1f), (dir == 1) ? threePiOverFour : piOverFour);

            if (Scene.CollideCheck<Solid>(collisionPoint + new Vector2(dir * -2, Height + 2f)))
                for (int i = particleOffset; i < particleWidth; i += 8)
                    SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, BottomLeft + new Vector2(i + (dir * 2f), 0f), (dir == 1) ? -threePiOverFour : -piOverFour);
        }
    }

    private IEnumerator Sequence()
    {
        // Infinite.
        while (true)
        {
            if (!HasPlayerRider())
            {
                yield return null;
                continue;
            }

            Vector2 from = nodes[0];
            Vector2 to;
            float at;

            // Player is riding.
            bool shouldCancel = false;
            int i;
            for (i = 1; i < nodes.Length; i++)
            {
                to = nodes[i];

                // Start shaking.
                sfx.Play(CustomSFX.game_dreamZipMover_start);
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
                StartShaking(0.1f);
                yield return 0.1f;

                // Start moving towards the target.
                StopPlayerRunIntoAnimation = false;
                at = 0f;
                bool playedFinishSound = false;
                while (at < 1f)
                {
                    yield return null;
                    at = Calc.Approach(at, 1f, 2f * Engine.DeltaTime);
                    if (at > impactSoundOffset && !playedFinishSound)
                    {
                        Audio.Play(CustomSFX.game_dreamZipMover_impact, Center);
                        playedFinishSound = true;
                    }
                    percent = Ease.SineIn(at);
                    Vector2 vector = Vector2.Lerp(from, to, percent);
                    ScrapeParticlesCheck(to);

                    if (Scene.OnInterval(0.1f))
                        pathRenderer?.CreateSparks();

                    MoveTo(vector);
                }

                bool last = i == nodes.Length - 1;

                // Arrived, will wait for 0.5 secs.
                StartShaking(0.2f);
                Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                SceneAs<Level>().Shake();
                StopPlayerRunIntoAnimation = true;
                yield return 0.5f;

                from = nodes[i];

                if (ticking && !last)
                {
                    float tickTime = 0.0f;
                    int tickNum = 0;
                    while (!HasPlayerRider() && tickNum < 5)
                    {
                        yield return null;

                        tickTime = Calc.Approach(tickTime, 1f, Engine.DeltaTime);
                        if (tickTime >= 1.0f)
                        {
                            tickTime = 0.0f;
                            tickNum++;
                            Audio.Play(CustomSFX.game_dreamZipMover_tick, Center);
                            StartShaking(0.1f);
                        }
                    }

                    if (tickNum == 5 && !HasPlayerRider())
                    {
                        shouldCancel = true;
                        break;
                    }
                }
                else if (waits && !last)
                {
                    while (!HasPlayerRider())
                        yield return null;
                }
            }

            if (!permanent)
            {
                if (noReturn)
                {
                    Array.Reverse(nodes);
                }
                else
                {
                    for (i -= 2 - (shouldCancel ? 1 : 0); i >= 0; i--)
                    {
                        to = nodes[i];

                        // Goes back to start with a speed that is four times slower.
                        StopPlayerRunIntoAnimation = false;
                        sfx.Play(CustomSFX.game_dreamZipMover_return);
                        at = 0f;
                        bool playedFinishSound = false;
                        while (at < 1f)
                        {
                            yield return null;
                            at = Calc.Approach(at, 1f, 0.5f * Engine.DeltaTime);
                            percent = 1f - Ease.SineIn(at);

                            Vector2 position = Vector2.Lerp(from, to, Ease.SineIn(at));
                            MoveTo(position);

                            if (at > impactSoundOffset && !playedFinishSound)
                            {
                                playedFinishSound = true;
                                Audio.Play(CustomSFX.game_dreamZipMover_finish, Center);
                            }
                        }

                        if (i != 0)
                            from = nodes[i];

                        StartShaking(0.2f);
                    }
                }
                StopPlayerRunIntoAnimation = true;

                // Done, will not activate for 0.5 secs.
                yield return 0.5f;
            }
            else
            {
                // Done, will never be activated again.
                StartShaking(0.3f);
                Audio.Play(CustomSFX.game_dreamZipMover_tick, Center);
                SceneAs<Level>().Shake(0.15f);
                while (true)
                    yield return null;
            }
        }
    }

    protected override void OneUseDestroy()
    {
        base.OneUseDestroy();
        Scene.Remove(pathRenderer);
        pathRenderer = null;
        sfx.Stop();
    }

    public override void Update()
    {
        base.Update();
        UpdateCogTexture();
    }

    public void DrawBorder() { }

    public void DrawCog(Vector2 node, float rotation)
    {
        cog.DrawCentered(node + Vector2.UnitY, Color.Black, 1f, rotation);
        cog.DrawCentered(node, Color.White, 1f, rotation);
    }

    internal static void InitializeTextures()
    {
        cogNormal = GFX.Game["objects/zipmover/cog"];
        cogDream = GFX.Game["objects/CommunalHelper/dreamZipMover/cog"];
        cogDisabled = GFX.Game["objects/CommunalHelper/dreamZipMover/disabledCog"];
        cogWhite = GFX.Game["objects/CommunalHelper/dreamZipMover/cogWhite"];
    }
}
