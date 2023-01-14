using System.Collections;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper;

[CustomEntity("CommunalHelper/ConnectedZipMover")]
public class ConnectedZipMover : ConnectedSolid
{
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

            public void Render(float percent, Color rope, Color lightRope)
            {
                Draw.Line(lineStartA, lineEndA, rope);
                Draw.Line(lineStartB, lineEndB, rope);

                for (float d = 4f - (percent * eightPi % 4f); d < length; d += 4f)
                {
                    Vector2 pos = dir * d;
                    Vector2 teethA = lineStartA + perp + pos;
                    Vector2 teethB = lineEndB - pos;
                    Draw.Line(teethA, teethA + twodir, lightRope);
                    Draw.Line(teethB, teethB - twodir, lightRope);
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

        private readonly ConnectedZipMover zipMover;

        private Level level;

        private readonly Color color, lightColor;

        private readonly Vector2[] nodes;

        public PathRenderer(ConnectedZipMover zipMover, Vector2[] nodes, Color color, Color lightColor)
        {
            this.zipMover = zipMover;

            this.nodes = new Vector2[nodes.Length];

            Vector2 offset = new(zipMover.MasterWidth / 2f, zipMover.MasterHeight / 2f);

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

            this.color = color;
            this.lightColor = lightColor;

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

            foreach (Segment seg in segments)
                if (seg.Seen = cameraBounds.Intersects(seg.Bounds))
                    seg.RenderShadow(zipMover.percent);

            foreach (Segment seg in segments)
                if (seg.Seen)
                    seg.Render(zipMover.percent, color, lightColor);

            float rotation = zipMover.percent * MathHelper.TwoPi;
            foreach (Vector2 node in nodes)
            {
                zipMover.cog.DrawCentered(node + Vector2.UnitY, Color.Black, 1f, rotation);
                zipMover.cog.DrawCentered(node, Color.White, 1f, rotation);
            }

            zipMover.DrawBorder();
        }
    }
    private PathRenderer pathRenderer;

    public enum Themes
    {
        Normal,
        Moon,
        Cliffside
    }
    public readonly Themes theme;

    private readonly MTexture[,] edges = new MTexture[3, 3];
    private readonly MTexture[,] innerCorners = new MTexture[2, 2];
    private readonly Sprite streetlight;
    private readonly List<MTexture> innerCogs;
    private readonly MTexture temp = new();

    private readonly bool drawBlackBorder;

    private readonly SoundSource sfx;
    private readonly BloomPoint bloom;

    private readonly bool permanent;
    private readonly bool waits;
    private readonly bool ticking;
    private readonly Coroutine seq;

    private readonly string themePath;
    private Color backgroundColor;

    private readonly Vector2[] nodes;

    private readonly Color ropeColor = Calc.HexToColor("663931");
    private readonly Color ropeLightColor = Calc.HexToColor("9b6157");
    private readonly MTexture cog;

    private float percent;

    public ConnectedZipMover(EntityData data, Vector2 offset)
        : this(data.Position + offset, data.Width, data.Height, data.NodesWithPosition(offset),
              data.Enum("theme", Themes.Normal),
              data.Bool("permanent"),
              data.Bool("waiting"),
              data.Bool("ticking"),
              data.Attr("customSkin").Trim(),
              data.Attr("colors").Trim(),
              data.Attr("customBlockTexture").Trim())
    { }

    public ConnectedZipMover(Vector2 position, int width, int height, Vector2[] nodes, Themes theme, bool permanent, bool waits, bool ticking, string customSkin, string colors, string legacyCustomTexture)
        : base(position, width, height, safe: false)
    {
        Depth = Depths.FGTerrain + 1;

        this.nodes = nodes;

        this.theme = theme;
        this.permanent = permanent;
        this.waits = waits;
        this.ticking = ticking;

        Add(seq = new Coroutine(Sequence()));
        Add(new LightOcclude());

        SurfaceSoundIndex = SurfaceIndex.Girder;

        string path, id, key, corners;
        if (!string.IsNullOrEmpty(customSkin))
        {
            path = customSkin + "/light";
            id = customSkin + "/block";
            key = customSkin + "/innercog";
            corners = customSkin + "/innerCorners";
            cog = GFX.Game[customSkin + "/cog"];
            themePath = "normal";
            backgroundColor = Color.Black;
            if (this.theme == Themes.Moon)
                themePath = "moon";
        }
        else
        {
            switch (this.theme)
            {
                default:
                case Themes.Normal:
                    path = "objects/zipmover/light";
                    id = "objects/zipmover/block";
                    key = "objects/zipmover/innercog";
                    corners = "objects/CommunalHelper/zipmover/innerCorners";
                    cog = GFX.Game["objects/zipmover/cog"];
                    themePath = "normal";
                    drawBlackBorder = true;
                    backgroundColor = Color.Black;
                    break;

                case Themes.Moon:
                    path = "objects/zipmover/moon/light";
                    id = "objects/zipmover/moon/block";
                    key = "objects/zipmover/moon/innercog";
                    corners = "objects/CommunalHelper/zipmover/moon/innerCorners";
                    cog = GFX.Game["objects/zipmover/moon/cog"];
                    themePath = "moon";
                    drawBlackBorder = false;
                    backgroundColor = Color.Black;
                    break;

                case Themes.Cliffside:
                    path = "objects/CommunalHelper/connectedZipMover/cliffside/light";
                    id = "objects/CommunalHelper/connectedZipMover/cliffside/block";
                    key = "objects/CommunalHelper/connectedZipMover/cliffside/innercog";
                    corners = "objects/CommunalHelper/connectedZipMover/cliffside/innerCorners";
                    cog = GFX.Game["objects/CommunalHelper/connectedZipMover/cliffside/cog"];
                    themePath = "normal";
                    drawBlackBorder = true;
                    backgroundColor = Calc.HexToColor("171018");
                    break;
            }
        }

        if (!string.IsNullOrEmpty(colors))
        {
            // Comma seperated list of colors
            // First is background color, second is main rope color, third is light rope color
            string[] colorList = colors.Split(',');
            if (colorList.Length > 0)
                backgroundColor = Calc.HexToColor(colorList[0]);
            if (colorList.Length > 1)
                ropeColor = Calc.HexToColor(colorList[1]);
            if (colorList.Length > 2)
                ropeLightColor = Calc.HexToColor(colorList[2]);
        }

        innerCogs = GFX.Game.GetAtlasSubtextures(key);
        streetlight = new Sprite(GFX.Game, path)
        {
            Active = false
        };
        streetlight.Add("frames", "", 1f);
        streetlight.Play("frames");
        streetlight.SetAnimationFrame(1);
        streetlight.Position = new Vector2((Width / 2f) - (streetlight.Width / 2f), 0f);
        Add(bloom = new BloomPoint(1f, 6f)
        {
            Position = new Vector2(Width / 2f, 4f)
        });

        if (legacyCustomTexture != "")
        {
            Tuple<MTexture[,], MTexture[,]> customTiles = SetupCustomTileset(legacyCustomTexture);
            edges = customTiles.Item1;
            innerCorners = customTiles.Item2;
        }
        else
        {
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    edges[i, j] = GFX.Game[id].GetSubtexture(i * 8, j * 8, 8, 8);

            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 2; j++)
                    innerCorners[i, j] = GFX.Game[corners].GetSubtexture(i * 8, j * 8, 8, 8);
        }

        Add(sfx = new SoundSource()
        {
            Position = new Vector2(Width, Height) / 2f
        });
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);
        AutoTile(edges, innerCorners);

        Add(streetlight);
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);

        scene.Add(pathRenderer = new(this, nodes, ropeColor, ropeLightColor));
    }

    public override void Removed(Scene scene)
    {
        scene.Remove(pathRenderer);
        pathRenderer = null;
        base.Removed(scene);
    }

    public override void Update()
    {
        base.Update();

        bloom.Visible = streetlight.CurrentAnimationFrame != 0;
        bloom.Y = (theme == Themes.Normal) ? streetlight.CurrentAnimationFrame * 3 : (theme == Themes.Cliffside) ? 5 : 9;
    }

    public override void Render()
    {
        Vector2 originalPosition = Position;
        Position += Shake;

        foreach (Hitbox extension in Colliders)
        {
            if (theme == Themes.Moon)
            {
                Draw.Rect(extension.Left + 2f + X, extension.Top + Y, extension.Width - 4f, extension.Height, backgroundColor);
                Draw.Rect(extension.Left + X, extension.Top + 2f + Y, extension.Width, extension.Height - 4f, backgroundColor);
                foreach (Image t in InnerCornerTiles)
                    Draw.Rect(t.X + X, t.Y + Y, 8, 8, backgroundColor);
            }
            else
                Draw.Rect(extension.Left + X, extension.Top + Y, extension.Width, extension.Height, backgroundColor);
        }

        int n = 1;
        float rot = 0f;
        int count = innerCogs.Count;

        float w = GroupBoundsMax.X - GroupBoundsMin.X;
        float h = GroupBoundsMax.Y - GroupBoundsMin.Y;
        Vector2 offset = new Vector2(-8, -8) + GroupOffset;

        for (int i = 4; i <= h + 4; i += 8)
        {
            int oldN = n;
            for (int j = 4; j <= w + 4; j += 8)
            {
                int index = (int) (Util.Mod((rot + (n * percent * (float) Math.PI * 4f)) / ((float) Math.PI / 2f), 1f) * count);
                MTexture mTexture = innerCogs[index];
                Rectangle rectangle = new(0, 0, mTexture.Width, mTexture.Height);
                Vector2 zero = Vector2.Zero;

                int x = (j - 4) / 8;
                int y = (i - 4) / 8;
                if (GroupTiles[x, y])
                {
                    // Rescaling the SubTexture Rectangle if the current cog can be rendered outside the Zip Mover

                    if (!GroupTiles[x - 1, y]) // Left
                    {
                        zero.X = 2f;
                        rectangle.X = 2;
                        rectangle.Width -= 2;
                    }
                    if (!GroupTiles[x + 1, y]) // Right
                    {
                        zero.X = -2f;
                        rectangle.Width -= 2;
                    }
                    if (!GroupTiles[x, y - 1]) // Up
                    {
                        zero.Y = 2f;
                        rectangle.Y = 2;
                        rectangle.Height -= 2;
                    }
                    if (!GroupTiles[x, y + 1]) // Down
                    {
                        zero.Y = -2f;
                        rectangle.Height -= 2;
                    }

                    mTexture = mTexture.GetSubtexture(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, temp);
                    mTexture.DrawCentered(Position + new Vector2(j, i) + zero + offset, Color.White * ((n < 0) ? 0.5f : 1f));
                }

                n *= -1;
                rot += MathHelper.Pi / 3f;
            }

            // Ensures the checkboard pattern for innercogs
            if (oldN == n)
                n *= -1;
        }

        base.Render();
        Position = originalPosition;
    }

    public void DrawBorder()
    {
        if (drawBlackBorder)
            foreach (Hitbox extension in AllColliders)
                Draw.HollowRect(new Rectangle(
                    (int) (X + extension.Left - 1f + Shake.X),
                    (int) (Y + extension.Top - 1f + Shake.Y),
                    (int) extension.Width + 2,
                    (int) extension.Height + 2),
                    Color.Black);
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
                sfx.Play($"event:/CommunalHelperEvents/game/zipMover/{themePath}/start");
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
                StartShaking(0.1f);
                yield return 0.1f;

                // Start moving towards the target.
                streetlight.SetAnimationFrame(3);
                StopPlayerRunIntoAnimation = false;
                at = 0f;
                while (at < 1f)
                {
                    yield return null;
                    at = Calc.Approach(at, 1f, 2f * Engine.DeltaTime);
                    percent = Ease.SineIn(at);
                    Vector2 vector = Vector2.Lerp(from, to, percent);

                    if (Scene.OnInterval(0.1f))
                        pathRenderer.CreateSparks();

                    MoveTo(vector);

                    if (Scene.OnInterval(0.03f))
                        SpawnScrapeParticles();
                }

                bool last = i == nodes.Length - 1;

                // Arrived, will wait for 0.5 secs.
                StartShaking(0.2f);
                Audio.Play($"event:/CommunalHelperEvents/game/zipMover/{themePath}/impact", Center);
                streetlight.SetAnimationFrame(((waits && !last) || (ticking && !last) || (permanent && last)) ? 1 : 2);
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
                        streetlight.SetAnimationFrame(1 - (int) Math.Round(tickTime));


                        tickTime = Calc.Approach(tickTime, 1f, Engine.DeltaTime);
                        if (tickTime >= 1.0f)
                        {
                            tickTime = 0.0f;
                            ++tickNum;
                            Audio.Play($"event:/CommunalHelperEvents/game/zipMover/{themePath}/tick", Center);
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
                    streetlight.SetAnimationFrame(1);
                    while (!HasPlayerRider())
                        yield return null;
                }
            }

            if (!permanent)
            {
                for (i -= 2 - (shouldCancel ? 1 : 0); i >= 0; i--)
                {
                    to = nodes[i];

                    // Goes back to start with a speed that is four times slower.
                    StopPlayerRunIntoAnimation = false;
                    streetlight.SetAnimationFrame(2);
                    sfx.Play($"event:/CommunalHelperEvents/game/zipMover/{themePath}/return");
                    at = 0f;
                    while (at < 1f)
                    {
                        yield return null;
                        at = Calc.Approach(at, 1f, 0.5f * Engine.DeltaTime);
                        percent = 1f - Ease.SineIn(at);

                        Vector2 position = Vector2.Lerp(from, to, Ease.SineIn(at));
                        MoveTo(position);
                    }

                    if (i != 0)
                        from = nodes[i];

                    StartShaking(0.2f);
                    Audio.Play($"event:/CommunalHelperEvents/game/zipMover/{themePath}/finish", Center);
                }

                StopPlayerRunIntoAnimation = true;

                // Done, will not activate for 0.5 secs.
                streetlight.SetAnimationFrame(1);
                yield return 0.5f;
            }
            else
            {
                // Done, will never be activated again.
                StartShaking(0.3f);
                Audio.Play($"event:/CommunalHelperEvents/game/zipMover/{themePath}/finish", Center);
                Audio.Play($"event:/CommunalHelperEvents/game/zipMover/{themePath}/tick", Center);
                SceneAs<Level>().Shake(0.15f);
                streetlight.SetAnimationFrame(0);
                while (true)
                    yield return null;
            }
        }
    }
}
