using Celeste.Mod.CommunalHelper;
using System.Collections;
using System.Collections.Generic;
using static MonoMod.Cil.RuntimeILReferenceBag;

namespace Celeste.Mod.CommunalHelper.Entities.StrawberryJam
{
    [CustomEntity("CommunalHelper/SJ/DashZipMover")]
    public class DashZipMover : Solid
    {
        private class DashZipMoverPathRenderer : Entity
        {
            public DashZipMover zipMover;

            private MTexture cog;

            private Vector2 from;
            private Vector2 to;

            private Vector2 sparkAdd;
            private float sparkDirFromA;
            private float sparkDirFromB;
            private float sparkDirToA;
            private float sparkDirToB;

            private float length;

            private Color ropeColor = Calc.HexToColor("046e19");
            private Color ropeLightColor = Calc.HexToColor("329415");
            private Color ropeShadow = Calc.HexToColor("003622");

            public DashZipMoverPathRenderer(DashZipMover zipMover, string cogSprite, Color ropeColor, Color ropeLightColor, Color ropeShadowColor)
            {
                Depth = Depths.SolidsBelow;
                this.zipMover = zipMover;

                from = zipMover.start + new Vector2(zipMover.Width / 2f, zipMover.Height / 2f);
                to = zipMover.target + new Vector2(zipMover.Width / 2f, zipMover.Height / 2f);

                sparkAdd = (from - to).SafeNormalize(5f).Perpendicular();
                float angle = (from - to).Angle();
                length = (to - from).Length();

                sparkDirFromA = angle + (float) Math.PI / 8f;
                sparkDirFromB = angle - (float) Math.PI / 8f;
                sparkDirToA = angle + (float) Math.PI - (float) Math.PI / 8f;
                sparkDirToB = angle + (float) Math.PI + (float) Math.PI / 8f;

                cog = GFX.Game[cogSprite];

                this.ropeColor = ropeColor;
                this.ropeLightColor = ropeLightColor;
                ropeShadow = ropeShadowColor;
            }

            public void CreateSparks()
            {
                SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, from + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirFromA);
                SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, from - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirFromB);
                SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, to + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirToA);
                SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, to - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirToB);
            }

            public override void Render()
            {
                if (length != 0f)
                {
                    DrawCogs(Vector2.UnitY, ropeShadow);
                    DrawCogs(Vector2.Zero);
                }
                if (zipMover.drawBlackBorder)
                {
                    Rectangle outline = new Rectangle(
                        (int) (Math.Round(zipMover.X - ((zipMover.scale.X - 1) * zipMover.Width) / 2f) + zipMover.Shake.X),
                        (int) (Math.Round(zipMover.Y - ((zipMover.scale.Y - 1) * zipMover.Height) / 2f) + zipMover.Shake.Y),
                        (int) Math.Ceiling(zipMover.Width * zipMover.scale.X), // TODO: this rectangle's width/height is *ever so slighly* off. help.
                        (int) Math.Ceiling(zipMover.Height * zipMover.scale.Y)
                    );
                    outline.Inflate(1, 1);
                    Draw.Rect(outline, Color.Black);
                }
            }

            private void DrawCogs(Vector2 offset, Color? colorOverride = null)
            {
                Vector2 vector = (to - from).SafeNormalize();
                Vector2 value = vector.Perpendicular() * 3f;
                Vector2 value2 = -vector.Perpendicular() * 4f;

                float rotation = zipMover.percent * (float) Math.PI * 2f;
                Vector2 perp = vector.Perpendicular();
                Vector2 perpNormalized = vector.Perpendicular();

                Vector2 p1from = from + value + perp + offset;
                Vector2 p2from = to + value2 + offset;
                for (float num = 4f - zipMover.percent * (float) Math.PI * 8f % 4f; num < length; num += 4f)
                {

                    float progress = num / length;
                    float sinAmount = progress * (1 - progress) * 8;
                    Vector2 sinOffset = perpNormalized * (float) Math.Sin(num) * sinAmount;

                    Vector2 p1to = from + value + perp + vector * num + sinOffset + offset;
                    Vector2 p2to = to + value2 - vector * num + sinOffset + offset;

                    // Thicker vine rope, in the back, sort of outline
                    if (colorOverride != null)
                    {
                        Draw.Line(p1from, p1to, (Color) colorOverride, 3);
                        Draw.Line(p2from, p2to, (Color) colorOverride, 3);
                    }

                    // Main "vine rope"
                    Draw.Line(p1from, p1to, colorOverride ?? ropeColor);
                    Draw.Line(p2from, p2to, colorOverride ?? ropeColor);

                    // Leaves
                    Draw.Line(p1to, p1to + vector * 4f, colorOverride ?? ropeLightColor);
                    Draw.Line(p2to, p2to - vector * 4f, colorOverride ?? ropeLightColor);

                    p1from = p1to;
                    p2from = p2to;
                }

                cog.DrawCentered(from + offset, colorOverride ?? Color.White, 1f, rotation);
                cog.DrawCentered(to + offset, colorOverride ?? Color.White, 1f, rotation);
            }
        }

        private MTexture[,] edges = new MTexture[3, 3];

        private Sprite streetlight;
        private BloomPoint bloom;

        private DashZipMoverPathRenderer pathRenderer;
        private List<MTexture> innerCogs;
        private MTexture temp = new MTexture();

        private Vector2 start;
        private Vector2 target;
        private float percent;
        private bool triggered;

        private Vector2 scale = Vector2.One;

        private SoundSource sfx = new SoundSource();

        private bool drawBlackBorder;

        private string soundEvent;

        public DashZipMover(Vector2 position, int width, int height, Vector2 target, string spritePath, bool drawBlackBorder, Color ropeColor, Color ropeLightColor, Color ropeShadowColor, string sound)
            : base(position, width, height, safe: false)
        {
            Depth = Depths.FGTerrain + 1;
            start = Position;
            this.target = target;

            Add(new Coroutine(Sequence()));
            Add(new LightOcclude());

            string path = spritePath + "light";
            string id = spritePath + "block";
            string key = spritePath + "innercog";

            this.drawBlackBorder = drawBlackBorder;

            innerCogs = GFX.Game.GetAtlasSubtextures(key);

            Add(streetlight = new Sprite(GFX.Game, path));
            streetlight.Add("frames", "", 1f);
            streetlight.Play("frames");
            streetlight.Active = false;
            streetlight.SetAnimationFrame(1);
            streetlight.Position = new Vector2(Width / 2f - streetlight.Width / 2f, 0f);

            Add(bloom = new BloomPoint(1f, 6f));
            bloom.Position = new Vector2(Width / 2f, 10f);

            for (int x = 0; x < 3; x++)
                for (int y = 0; y < 3; y++)
                    edges[x, y] = GFX.Game[id].GetSubtexture(x * 8, y * 8, 8, 8);

            SurfaceSoundIndex = SurfaceIndex.Girder;

            OnDashCollide = OnDashed;

            sfx.Position = new Vector2(Width, Height) / 2f;
            Add(sfx);

            pathRenderer = new DashZipMoverPathRenderer(this, spritePath + "cog", ropeColor, ropeLightColor, ropeShadowColor);

            soundEvent = sound;
        }

        public DashZipMover(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Width, data.Height, data.Nodes[0] + offset, data.Attr("spritePath", "objects/CommunalHelper/strawberryJam/dashZipMover/"), data.Bool("drawBlackBorder", false), Calc.HexToColor(data.Attr("ropeColor", "046e19")), Calc.HexToColor(data.Attr("ropeLightColor", "329415")), Calc.HexToColor(data.Attr("ropeShadowColor", "003622")), data.Attr("soundEvent", "event:/CommunalHelperEvents/game/strawberryJam/game/dash_zip_mover/zip_mover"))
        {
        }

        public DashCollisionResults OnDashed(Player player, Vector2 dir)
        {
            if (!triggered)
            {
                triggered = true;

                scale = new Vector2(1f + Math.Abs(dir.Y) * 0.4f - Math.Abs(dir.X) * 0.4f, 1f + Math.Abs(dir.X) * 0.4f - Math.Abs(dir.Y) * 0.4f);

                return DashCollisionResults.Rebound;
            }

            return DashCollisionResults.NormalCollision;
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            scene.Add(pathRenderer);
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

            scale = Calc.Approach(scale, Vector2.One, 3f * Engine.DeltaTime);

            streetlight.Scale = scale;
            Vector2 zeroCenter = new Vector2(Width, Height) / 2f;
            streetlight.Position = zeroCenter + (new Vector2(zeroCenter.X - streetlight.Width / 2f, 0) - zeroCenter) * scale;
        }

        public override void Render()
        {
            Vector2 position = Position;
            Position += Shake;

            Rectangle rect = new Rectangle(
                (int) (Center.X + (X + 2 - Center.X) * scale.X),
                (int) (Center.Y + (Y + 2 - Center.Y) * scale.Y),
                (int) ((Width - 4) * scale.X),
                (int) ((Height - 4) * scale.Y));

            Draw.Rect(rect, Color.Black);

            int offset = 1;
            float angle = 0f;
            int count = innerCogs.Count;

            for (int x = 4; x <= Height - 4f; x += 8)
            {
                int prevOffset = offset;
                for (int y = 4; y <= Width - 4f; y += 8)
                {
                    int index = (int) (Util.Mod((angle + offset * percent * (float) Math.PI * 4f) / ((float) Math.PI / 2f), 1f) * count);

                    MTexture innerCog = innerCogs[index];
                    Rectangle rectangle = new Rectangle(0, 0, innerCog.Width, innerCog.Height);
                    Vector2 zero = Vector2.Zero;

                    if (y <= 4)
                    {
                        zero.X = 2f;
                        rectangle.X = 2;
                        rectangle.Width -= 2;
                    }
                    else if (y >= Width - 4f)
                    {
                        zero.X = -2f;
                        rectangle.Width -= 2;
                    }

                    if (x <= 4)
                    {
                        zero.Y = 2f;
                        rectangle.Y = 2;
                        rectangle.Height -= 2;
                    }
                    else if (x >= Height - 4f)
                    {
                        zero.Y = -2f;
                        rectangle.Height -= 2;
                    }

                    innerCog = innerCog.GetSubtexture(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, temp);
                    Vector2 pos = Center + (Position + new Vector2(y, x) + zero - Center) * scale;
                    innerCog.DrawCentered(pos, Color.White * (offset < 0 ? 0.5f : 1f), scale);

                    offset = -offset;
                    angle += (float) Math.PI / 3f;
                }
                if (prevOffset == offset)
                    offset = -offset;
            }

            for (int x = 0; x < Width / 8f; x++)
                for (int y = 0; y < Height / 8f; y++)
                {
                    int edgeX = x != 0 ? x != Width / 8f - 1f ? 1 : 2 : 0;
                    int edgeY = y != 0 ? y != Height / 8f - 1f ? 1 : 2 : 0;

                    if (edgeX != 1 || edgeY != 1)
                    {
                        Vector2 pos = Center + (new Vector2(X + x * 8 + 4, Y + y * 8 + 4) - Center) * scale;
                        edges[edgeX, edgeY].DrawCentered(pos, Color.White, scale);
                    }
                }

            base.Render();

            Position = position;
        }

        private void ScrapeParticlesCheck(Vector2 to)
        {
            if (!Scene.OnInterval(0.03f))
                return;

            bool movedV = to.Y != ExactPosition.Y;
            bool movedY = to.X != ExactPosition.X;

            if (movedV && !movedY)
            {
                int dir = Math.Sign(to.Y - ExactPosition.Y);
                Vector2 collisionPoint = dir != 1 ? TopLeft : BottomLeft;

                int particleOffset = 4;
                if (dir == 1)
                    particleOffset = Math.Min((int) Height - 12, 20);

                int particleHeight = (int) Height;
                if (dir == -1)
                    particleHeight = Math.Max(16, (int) Height - 16);

                if (Scene.CollideCheck<Solid>(collisionPoint + new Vector2(-2f, dir * -2)))
                    for (int y = particleOffset; y < particleHeight; y += 8)
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, TopLeft + new Vector2(0f, y + dir * 2f), dir == 1 ? -(float) Math.PI / 4f : (float) Math.PI / 4f);

                if (Scene.CollideCheck<Solid>(collisionPoint + new Vector2(Width + 2f, dir * -2)))
                    for (int y = particleOffset; y < particleHeight; y += 8)
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, TopRight + new Vector2(-1f, y + dir * 2f), dir == 1 ? (float) Math.PI * -3f / 4f : (float) Math.PI * 3f / 4f);

            }
            else if (movedY && !movedV)
            {
                int dir = Math.Sign(to.X - ExactPosition.X);
                Vector2 collisionPoint = dir != 1 ? TopLeft : TopRight;

                int particleOffset = 4;
                if (dir == 1)
                    particleOffset = Math.Min((int) Width - 12, 20);

                int particleWidth = (int) Width;
                if (dir == -1)
                    particleWidth = Math.Max(16, (int) Width - 16);

                if (Scene.CollideCheck<Solid>(collisionPoint + new Vector2(dir * -2, -2f)))
                    for (int x = particleOffset; x < particleWidth; x += 8)
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, TopLeft + new Vector2(x + dir * 2f, -1f), dir == 1 ? (float) Math.PI * 3f / 4f : (float) Math.PI / 4f);

                if (Scene.CollideCheck<Solid>(collisionPoint + new Vector2(dir * -2, Height + 2f)))
                    for (int x = particleOffset; x < particleWidth; x += 8)
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, BottomLeft + new Vector2(x + dir * 2f, 0f), dir == 1 ? (float) Math.PI * -3f / 4f : -(float) Math.PI / 4f);
            }
        }

        private IEnumerator Sequence()
        {
            Vector2 start = Position;

            while (true)
            {
                if (!triggered)
                {
                    yield return null;
                    continue;
                }

                sfx.Play(soundEvent);

                Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
                StartShaking(0.1f);
                yield return 0.1f;

                streetlight.SetAnimationFrame(3);
                StopPlayerRunIntoAnimation = false;

                float at2 = 0f;

                while (at2 < 1f)
                {
                    yield return null;
                    at2 = Calc.Approach(at2, 1f, 2f * Engine.DeltaTime);
                    percent = Ease.SineIn(at2);
                    Vector2 vector = Vector2.Lerp(start, target, percent);
                    ScrapeParticlesCheck(vector);
                    if (Scene.OnInterval(0.1f))
                        pathRenderer.CreateSparks();
                    MoveTo(vector);
                }

                StartShaking(0.2f);
                Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                streetlight.SetAnimationFrame(2);
                SceneAs<Level>().Shake();
                StopPlayerRunIntoAnimation = true;
                yield return 0.5f;

                StopPlayerRunIntoAnimation = false;
                streetlight.SetAnimationFrame(1);
                triggered = false;
                target = start;
                start = Position;
                sfx.Stop();
            }
        }
    }
}
