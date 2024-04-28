using System.Collections;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/CassetteZipMover")]
public class CassetteZipMover : CustomCassetteBlock
{
    public class PathRenderer : Entity
    {
        private static Color baseRopeColor = Calc.HexToColor("bfcfde");
        private static Color baseRopeLightColor = Calc.HexToColor("ffffff");
        private static Color baseRopeColorPressed = Calc.HexToColor("324e69");
        private static Color baseRopeLightColorPressed = Calc.HexToColor("667da5");

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

            public void Spark(Level level, ParticleType p)
            {
                level.ParticlesBG.Emit(p, from + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirStartA);
                level.ParticlesBG.Emit(p, from - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirStartB);
                level.ParticlesBG.Emit(p, to + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirEndA);
                level.ParticlesBG.Emit(p, to - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirEndB);
            }

            public void Render(float percent, Vector2 offset, Color rope, Color lightRope)
            {
                Vector2 startA = lineStartA + offset;
                Vector2 endB = lineEndB + offset;

                Draw.Line(startA, lineEndA + offset, rope);
                Draw.Line(lineStartB + offset, endB, rope);

                for (float d = 4f - (percent * eightPi % 4f); d < length; d += 4f)
                {
                    Vector2 pos = dir * d;
                    Vector2 teethA = startA + perp + pos;
                    Vector2 teethB = endB - pos;
                    Draw.Line(teethA, teethA + twodir, lightRope);
                    Draw.Line(teethB, teethB - twodir, lightRope);
                }
            }
        }

        private readonly Rectangle bounds;
        private readonly Segment[] segments;

        private readonly CassetteZipMover zipMover;

        private Level level;

        private readonly Color ropeColor, ropeLightColor, ropeColorPressed, ropeLightColorPressed, undersideColor;
        private readonly ParticleType sparkParticle, sparkParticlePressed;

        private readonly Vector2[] nodes;

        public PathRenderer(CassetteZipMover zipMover, Vector2[] nodes)
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

            bounds = new((int) min.X, (int) min.Y, (int) (max.X - min.X), (int) (max.Y - min.Y));
            bounds.Inflate(10, 10);

            ropeColor = baseRopeColor.Mult(zipMover.color);
            ropeLightColor = baseRopeLightColor.Mult(zipMover.color);
            ropeColorPressed = baseRopeColorPressed.Mult(zipMover.color);
            ropeLightColorPressed = baseRopeLightColorPressed.Mult(zipMover.color);
            undersideColor = ropeColorPressed;

            sparkParticle = new ParticleType(ZipMover.P_Sparks) { Color = ropeLightColor };
            sparkParticlePressed = new ParticleType(ZipMover.P_Sparks) { Color = ropeLightColorPressed };

            Depth = Depths.SolidsBelow;
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            level = scene as Level;
        }

        public void CreateSparks()
        {
            ParticleType p = zipMover.Collidable ? sparkParticle : sparkParticlePressed;
            foreach (Segment seg in segments)
                seg.Spark(level, p);
        }

        public override void Update()
        {
            Depth = zipMover.Collidable ? Depths.BGDecals : Depths.BGDecals + 10;
        }

        public override void Render()
        {
            Rectangle cameraBounds = level.Camera.GetBounds();

            if (!cameraBounds.Intersects(bounds))
                return;

            bool on = zipMover.Collidable;
            Color color = on ? ropeColor : ropeColorPressed;
            Color lightColor = on ? ropeLightColor : ropeLightColorPressed;

            foreach (Segment seg in segments)
                seg.Seen = cameraBounds.Intersects(seg.Bounds);

            for (int i = 1; i <= zipMover.blockHeight; ++i)
            {
                Vector2 o = new(0, i + zipMover.blockOffset.Y);
                foreach (Segment seg in segments)
                    if (seg.Seen)
                        seg.Render(zipMover.percent, o, undersideColor, undersideColor);
            }

            foreach (Segment seg in segments)
                if (seg.Seen)
                    seg.Render(zipMover.percent, zipMover.blockOffset, color, lightColor);

            float rotation = zipMover.percent * MathHelper.TwoPi;
            MTexture cogTex = on ? cog : cogPressed;
            foreach (Vector2 node in nodes)
            {
                for (int i = 1; i <= zipMover.blockHeight; ++i)
                {
                    Vector2 o = new(0, i + zipMover.blockOffset.Y);
                    cogWhite.DrawCentered(node + o, undersideColor, 1f, rotation);
                }
                cogTex.DrawCentered(node + zipMover.blockOffset, zipMover.color, 1f, rotation);
            }
        }
    }
    private PathRenderer pathRenderer;

    private float percent = 0f;

    private readonly SoundSource sfx = new();

    /// <summary>
    /// Entity nodes with start Position as the first element
    /// </summary>
    protected readonly Vector2[] nodes;

    private readonly bool permanent;
    private readonly bool waits;
    private readonly bool ticking;
    private readonly bool noReturn;

    private static MTexture cog, cogPressed, cogWhite;

    public CassetteZipMover(Vector2 position, EntityID id, int width, int height, Vector2[] nodes, int index, float tempo, bool oldConnectionBehavior, bool noReturn, bool perm, bool waits, bool ticking, Color? overrideColor)
        : base(position, id, width, height, index, tempo, true, oldConnectionBehavior, false, overrideColor)
    {
        this.noReturn = noReturn;
        permanent = perm;
        this.waits = waits;
        this.ticking = ticking;

        this.nodes = nodes;

        Add(new Coroutine(Sequence()));

        sfx.Position = new Vector2(Width, Height) / 2f;
        Add(sfx);
    }

    public CassetteZipMover(EntityData data, Vector2 offset, EntityID id)
        : this(data.Position + offset, id, data.Width, data.Height, data.NodesWithPosition(offset), data.Int("index"), data.Float("tempo", 1f), data.Bool("oldConnectionBehavior"),
              data.Bool("noReturn", false),
              data.Bool("permanent"),
              data.Bool("waiting"),
              data.Bool("ticking"),
              data.HexColorNullable("customColor"))
    {
    }

    public override void Awake(Scene scene)
    {
        Image cross = new(GFX.Game["objects/CommunalHelper/cassetteMoveBlock/x"]);
        Image crossPressed = new(GFX.Game["objects/CommunalHelper/cassetteMoveBlock/xPressed"]);

        base.Awake(scene);
        if (noReturn)
            AddCenterSymbol(cross, crossPressed);
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
        Position = position;
    }

    private void ScrapeParticlesCheck(Vector2 to)
    {
        const float threePiOverFour = 3f * MathHelper.Pi / 4f;
        const float piOverFour = MathHelper.PiOver4;

        if (!Scene.OnInterval(0.03f))
            return;

        bool movedV = to.Y != ExactPosition.Y;
        bool movedH = to.X != ExactPosition.X;

        if (movedV && !movedH)
        {
            int dir = Math.Sign(to.Y - ExactPosition.Y);
            Vector2 origin = (dir != 1) ? TopLeft : BottomLeft;

            int start = dir == 1 ? Math.Min((int) Height - 12, 20) : 4;
            int end = dir == -1 ? Math.Max(16, (int) Height - 16) : (int) Height;

            if (Scene.CollideCheck<Solid>(origin + new Vector2(-2f, dir * -2)))
                for (int i = start; i < end; i += 8)
                    SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, TopLeft + new Vector2(0f, i + (dir * 2f)), (dir == 1) ? -piOverFour : piOverFour);

            if (Scene.CollideCheck<Solid>(origin + new Vector2(Width + 2f, dir * -2)))
                for (int j = start; j < end; j += 8)
                    SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, TopRight + new Vector2(-1f, j + (dir * 2f)), (dir == 1) ? -threePiOverFour : threePiOverFour);

        }
        else if (movedH && !movedV)
        {
            int dir = Math.Sign(to.X - ExactPosition.X);
            Vector2 origin = (dir != 1) ? TopLeft : TopRight;

            int start = dir == 1 ? Math.Min((int) Width - 12, 20) : 4;
            int end = dir == -1 ? Math.Max(16, (int) Width - 16) : (int) Width;

            if (Scene.CollideCheck<Solid>(origin + new Vector2(dir * -2, -2f)))
                for (int k = start; k < end; k += 8)
                    SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, TopLeft + new Vector2(k + (dir * 2f), -1f), (dir == 1) ? threePiOverFour : piOverFour);

            if (Scene.CollideCheck<Solid>(origin + new Vector2(dir * -2, Height + 2f)))
                for (int l = start; l < end; l += 8)
                    SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, BottomLeft + new Vector2(l + (dir * 2f), 0f), (dir == 1) ? -threePiOverFour : -piOverFour);
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
                sfx.Play(CustomSFX.game_zipMover_normal_start);
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
                StartShaking(0.1f);
                yield return 0.1f;

                // Start moving towards the target.
                StopPlayerRunIntoAnimation = false;
                at = 0f;
                while (at < 1f)
                {
                    yield return null;
                    at = Calc.Approach(at, 1f, 2f * Engine.DeltaTime);
                    percent = Ease.SineIn(at);
                    Vector2 vector = Vector2.Lerp(from, to, percent);
                    vector = FixCassetteY(vector);

                    ScrapeParticlesCheck(to);
                    if (Scene.OnInterval(0.1f))
                        pathRenderer.CreateSparks();

                    MoveTo(vector);
                }

                bool last = i == nodes.Length - 1;

                // Arrived, will wait for 0.5 secs.
                StartShaking(0.2f);
                Audio.Play(CustomSFX.game_zipMover_normal_impact, Center);
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
                            sfx.Play(CustomSFX.game_zipMover_normal_tick);
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
                    for (i -= 2 - (shouldCancel ? 1 : 0); i >= 0; --i)
                    {
                        to = nodes[i];

                        // Goes back to start with a speed that is four times slower.
                        StopPlayerRunIntoAnimation = false;
                        //streetlight.SetAnimationFrame(2);
                        sfx.Play(CustomSFX.game_zipMover_normal_return);
                        at = 0f;
                        while (at < 1f)
                        {
                            yield return null;
                            at = Calc.Approach(at, 1f, 0.5f * Engine.DeltaTime);
                            percent = 1f - Ease.SineIn(at);
                            Vector2 position = Vector2.Lerp(from, to, Ease.SineIn(at));
                            position = FixCassetteY(position);

                            MoveTo(position);
                        }

                        if (i != 0)
                            from = nodes[i];

                        StartShaking(0.2f);
                        Audio.Play(CustomSFX.game_zipMover_normal_finish, Center);
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
                Audio.Play(CustomSFX.game_zipMover_normal_finish, Center);
                Audio.Play(CustomSFX.game_zipMover_normal_tick, Center);
                SceneAs<Level>().Shake(0.15f);
                while (true)
                    yield return null;
            }
        }
    }

    private Vector2 FixCassetteY(Vector2 vec)
    {
        return vec + blockOffset;
    }

    internal static void InitializeTextures()
    {
        cog = GFX.Game["objects/CommunalHelper/cassetteZipMover/cog"];
        cogPressed = GFX.Game["objects/CommunalHelper/cassetteZipMover/cogPressed"];
        cogWhite = GFX.Game["objects/CommunalHelper/cassetteZipMover/cogWhite"];
    }
}
