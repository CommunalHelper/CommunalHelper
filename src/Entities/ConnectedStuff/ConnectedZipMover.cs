using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper {
    [CustomEntity("CommunalHelper/ConnectedZipMover")]
    public class ConnectedZipMover : ConnectedSolid {
        public enum Themes {
            Normal,
            Moon,
            Cliffside
        }
        public Themes theme;

        private class PathRenderer : Entity {
            public ConnectedZipMover ConnectedZipMover;
            private MTexture cog;

            private Vector2 from;
            private Vector2 to;
            private Vector2 sparkAdd;

            private float sparkDirFromA;
            private float sparkDirFromB;
            private float sparkDirToA;
            private float sparkDirToB;

            public PathRenderer(ConnectedZipMover advancedZipMover) {
                Depth = Depths.SolidsBelow;
                ConnectedZipMover = advancedZipMover;
                from = GetNodeFrom(ConnectedZipMover.start);
                sparkAdd = (from - to).SafeNormalize(5f).Perpendicular();
                float angle = (from - to).Angle();
                sparkDirFromA = angle + (float) Math.PI / 8f;
                sparkDirFromB = angle - (float) Math.PI / 8f;
                sparkDirToA = angle + (float) Math.PI - (float) Math.PI / 8f;
                sparkDirToB = angle + (float) Math.PI + (float) Math.PI / 8f;
                cog = advancedZipMover.cog;
            }

            public void CreateSparks() {
                from = GetNodeFrom(ConnectedZipMover.start);
                for (int i = 0; i < ConnectedZipMover.targets.Length; i++) {
                    to = GetNodeFrom(ConnectedZipMover.targets[i]);
                    SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, from + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirFromA);
                    SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, from - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirFromB);
                    SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, to + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirToA);
                    SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, to - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirToB);
                    from = GetNodeFrom(ConnectedZipMover.targets[i]);
                }
            }

            private Vector2 GetNodeFrom(Vector2 node) {
                return node + new Vector2(ConnectedZipMover.MasterWidth / 2f, ConnectedZipMover.MasterHeight / 2f);
            }

            public override void Render() {
                /* 
				 * We actually go through two FOR loops. 
				 * Because otherwise the "shadows" would pass over the
				 * previously drawn cogs, which would look a bit weird.
				 */

                // Draw behind, the "shadow", sort of.
                from = GetNodeFrom(ConnectedZipMover.start);
                for (int i = 0; i < ConnectedZipMover.targets.Length; i++) {
                    to = GetNodeFrom(ConnectedZipMover.targets[i]);
                    DrawCogs(Vector2.UnitY, Color.Black);
                    from = GetNodeFrom(ConnectedZipMover.targets[i]);
                }

                // Draw the actual cogs, coloured, above.
                from = GetNodeFrom(ConnectedZipMover.start);
                for (int i = 0; i < ConnectedZipMover.targets.Length; i++) {
                    to = GetNodeFrom(ConnectedZipMover.targets[i]);
                    DrawCogs(Vector2.Zero);
                    from = GetNodeFrom(ConnectedZipMover.targets[i]);
                }

                // Zip Mover's outline, rendered here because of Depth.
                if (ConnectedZipMover.drawBlackBorder) {
                    foreach (Hitbox extension in ConnectedZipMover.AllColliders) {
                        Draw.HollowRect(new Rectangle(
                            (int) (ConnectedZipMover.X + extension.Left - 1f + ConnectedZipMover.Shake.X),
                            (int) (ConnectedZipMover.Y + extension.Top - 1f + ConnectedZipMover.Shake.Y),
                            (int) extension.Width + 2,
                            (int) extension.Height + 2),
                            Color.Black);
                    }
                }
            }

            private void DrawCogs(Vector2 offset, Color? colorOverride = null) {
                Vector2 vector = (to - from).SafeNormalize();
                Vector2 value = vector.Perpendicular() * 3f;
                Vector2 value2 = -vector.Perpendicular() * 4f;
                float rotation = ConnectedZipMover.percent * (float) Math.PI * 2f;
                Draw.Line(from + value + offset, to + value + offset, colorOverride ?? ConnectedZipMover.ropeColor);
                Draw.Line(from + value2 + offset, to + value2 + offset, colorOverride ?? ConnectedZipMover.ropeColor);
                for (float num = 4f - ConnectedZipMover.percent * (float) Math.PI * 8f % 4f; num < (to - from).Length(); num += 4f) {
                    Vector2 value3 = from + value + vector.Perpendicular() + vector * num;
                    Vector2 value4 = to + value2 - vector * num;
                    Draw.Line(value3 + offset, value3 + vector * 2f + offset, colorOverride ?? ConnectedZipMover.ropeLightColor);
                    Draw.Line(value4 + offset, value4 - vector * 2f + offset, colorOverride ?? ConnectedZipMover.ropeLightColor);
                }
                cog.DrawCentered(from + offset, colorOverride ?? Color.White, 1f, rotation);
                cog.DrawCentered(to + offset, colorOverride ?? Color.White, 1f, rotation);
            }
        }

        private MTexture[,] edges = new MTexture[3, 3];
        private MTexture[,] innerCorners = new MTexture[2, 2];
        private Sprite streetlight;
        private List<MTexture> innerCogs;
        private MTexture temp = new MTexture();
        private MTexture cog;

        private bool drawBlackBorder;
        private Vector2 start;
        private float percent;

        private Vector2[] targets, originalNodes;

        // Sounds
        private SoundSource sfx;

        // Lightning.
        private BloomPoint bloom;

        // The instance of the PathRenderer Class defined above.
        private PathRenderer pathRenderer;

        private Color ropeColor = Calc.HexToColor("663931");
        private Color ropeLightColor = Calc.HexToColor("9b6157");

        private bool permanent;
        private bool waits;
        private bool ticking;
        public Coroutine seq;

        private string themePath;
        private Color backgroundColor;

        public ConnectedZipMover(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Width, data.Height, data.Nodes,
                  data.Enum("theme", Themes.Normal),
                  data.Bool("permanent"),
                  data.Bool("waiting"),
                  data.Bool("ticking"),
                  data.Attr("customSkin").Trim(),
                  data.Attr("colors").Trim(),
                  data.Attr("customBlockTexture").Trim()) { }

        public ConnectedZipMover(Vector2 position, int width, int height, Vector2[] nodes, Themes themes, bool perm, bool waits, bool ticking, string customSkin, string colors, string legacyCustomTexture)
            : base(position, width, height, safe: false) {
            Depth = Depths.FGTerrain + 1;

            start = Position;
            targets = new Vector2[nodes.Length];
            theme = themes;
            originalNodes = nodes;
            permanent = perm;
            this.waits = waits;
            this.ticking = ticking;
            seq = new Coroutine(Sequence());
            Add(seq);
            SetLightOcclude();

            string path;
            string id;
            string key;
            string corners;

            SurfaceSoundIndex = SurfaceIndex.Girder;

            if (!string.IsNullOrEmpty(customSkin)) {
                path = customSkin + "/light";
                id = customSkin + "/block";
                key = customSkin + "/innercog";
                corners = customSkin + "/innerCorners";
                cog = GFX.Game[customSkin + "/cog"];
                themePath = "normal";
                backgroundColor = Color.Black;
                if (theme == Themes.Moon)
                    themePath = "moon";
            } else
                switch (theme) {
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
            if (!string.IsNullOrEmpty(colors)) {
                // Comma seperated list of colors
                // First is background color, second is main rope color, third is light rope color
                string[] colorList = colors.Split(',');
                if (colorList.Length > 0) {
                    backgroundColor = Calc.HexToColor(colorList[0]);
                }
                if (colorList.Length > 1) {
                    ropeColor = Calc.HexToColor(colorList[1]);
                }
                if (colorList.Length > 2) {
                    ropeLightColor = Calc.HexToColor(colorList[2]);
                }
            }

            innerCogs = GFX.Game.GetAtlasSubtextures(key);
            streetlight = new Sprite(GFX.Game, path);
            streetlight.Add("frames", "", 1f);
            streetlight.Play("frames");
            streetlight.Active = false;
            streetlight.SetAnimationFrame(1);
            streetlight.Position = new Vector2(Width / 2f - streetlight.Width / 2f, 0f);
            Add(bloom = new BloomPoint(1f, 6f) {
                Position = new Vector2(Width / 2f, 4f)
            });

            if (legacyCustomTexture != "") {
                Tuple<MTexture[,], MTexture[,]> customTiles = SetupCustomTileset(legacyCustomTexture);
                edges = customTiles.Item1;
                innerCorners = customTiles.Item2;
            } else {
                for (int i = 0; i < 3; i++) {
                    for (int j = 0; j < 3; j++) {
                        edges[i, j] = GFX.Game[id].GetSubtexture(i * 8, j * 8, 8, 8);
                    }
                }

                for (int i = 0; i < 2; i++) {
                    for (int j = 0; j < 2; j++) {
                        innerCorners[i, j] = GFX.Game[corners].GetSubtexture(i * 8, j * 8, 8, 8);
                    }
                }
            }

            Add(sfx = new SoundSource() {
                Position = new Vector2(Width, Height) / 2f
            });
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            AutoTile(edges, innerCorners);

            Add(streetlight);
        }

        public override void Added(Scene scene) {
            base.Added(scene);

            // Offset the points to their position, relative to the room's position.
            Rectangle bounds = SceneAs<Level>().Bounds;
            Vector2 levelOffset = new Vector2(bounds.Left, bounds.Top);
            for (int i = 0; i < originalNodes.Length; i++) {
                targets[i] = originalNodes[i] + levelOffset;
            }

            // Creating the Path Renderer.
            scene.Add(pathRenderer = new PathRenderer(this));
        }

        public override void Removed(Scene scene) {
            scene.Remove(pathRenderer);
            pathRenderer = null;
            base.Removed(scene);
        }

        public override void Update() {
            base.Update();

            bloom.Visible = (streetlight.CurrentAnimationFrame != 0);
            bloom.Y = (theme == Themes.Normal) ? streetlight.CurrentAnimationFrame * 3 : (theme == Themes.Cliffside) ? 5 : 9;
        }

        public override void Render() {
            Vector2 originalPosition = Position;
            Position += Shake;

            foreach (Hitbox extension in Colliders) {
                if (theme == Themes.Moon) {
                    Draw.Rect(extension.Left + 2f + X, extension.Top + Y, extension.Width - 4f, extension.Height, backgroundColor);
                    Draw.Rect(extension.Left + X, extension.Top + 2f + Y, extension.Width, extension.Height - 4f, backgroundColor);
                    foreach (Image t in InnerCornerTiles) {
                        Draw.Rect(t.X + X, t.Y + Y, 8, 8, backgroundColor);
                    }
                } else {
                    Draw.Rect(extension.Left + X, extension.Top + Y, extension.Width, extension.Height, backgroundColor);
                }
            }

            int num = 1;
            float num2 = 0f;
            int count = innerCogs.Count;

            float w = GroupBoundsMax.X - GroupBoundsMin.X;
            float h = GroupBoundsMax.Y - GroupBoundsMin.Y;
            Vector2 offset = new Vector2(-8, -8) + GroupOffset;

            for (int i = 4; i <= h + 4; i += 8) {
                int num3 = num;
                for (int j = 4; j <= w + 4; j += 8) {
                    int index = (int) (Util.Mod((num2 + num * percent * (float) Math.PI * 4f) / ((float) Math.PI / 2f), 1f) * count);
                    MTexture mTexture = innerCogs[index];
                    Rectangle rectangle = new Rectangle(0, 0, mTexture.Width, mTexture.Height);
                    Vector2 zero = Vector2.Zero;

                    int x = (j - 4) / 8;
                    int y = (i - 4) / 8;
                    if (GroupTiles[x, y]) {
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
                        mTexture.DrawCentered(Position + new Vector2(j, i) + zero + offset, Color.White * ((num < 0) ? 0.5f : 1f));
                    }

                    num = -num;
                    num2 += (float) Math.PI / 3f;
                }
                if (num3 == num) {
                    num = -num;
                }
            }

            base.Render();
            Position = originalPosition;
        }

        private IEnumerator Sequence() {
            // Infinite.
            Vector2 start = Position;
            while (true) {
                if (!HasPlayerRider()) {
                    yield return null;
                    continue;
                }

                Vector2 from = start;
                Vector2 to;
                float at2;

                // Player is riding.
                bool shouldCancel = false;
                int i;
                for (i = 0; i < targets.Length; i++) {
                    to = targets[i];

                    // Start shaking.
                    sfx.Play($"event:/CommunalHelperEvents/game/zipMover/{themePath}/start");
                    Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
                    StartShaking(0.1f);
                    yield return 0.1f;

                    // Start moving towards the target.
                    streetlight.SetAnimationFrame(3);
                    StopPlayerRunIntoAnimation = false;
                    at2 = 0f;
                    while (at2 < 1f) {
                        yield return null;
                        at2 = Calc.Approach(at2, 1f, 2f * Engine.DeltaTime);
                        percent = Ease.SineIn(at2);
                        Vector2 vector = Vector2.Lerp(from, to, percent);

                        if (Scene.OnInterval(0.1f))
                            pathRenderer.CreateSparks();

                        MoveTo(vector);

                        if (Scene.OnInterval(0.03f))
                            SpawnScrapeParticles();
                    }

                    bool last = (i == targets.Length - 1);

                    // Arrived, will wait for 0.5 secs.
                    StartShaking(0.2f);
                    Audio.Play($"event:/CommunalHelperEvents/game/zipMover/{themePath}/impact", Center);
                    streetlight.SetAnimationFrame(((waits && !last) || (ticking && !last) || (permanent && last)) ? 1 : 2);
                    Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                    SceneAs<Level>().Shake();
                    StopPlayerRunIntoAnimation = true;
                    yield return 0.5f;

                    from = targets[i];

                    if (ticking && !last) {
                        float tickTime = 0.0f;
                        int tickNum = 0;
                        while (!HasPlayerRider() && tickNum < 5) {
                            yield return null;
                            streetlight.SetAnimationFrame(1 - (int) Math.Round(tickTime));


                            tickTime = Calc.Approach(tickTime, 1f, Engine.DeltaTime);
                            if (tickTime >= 1.0f) {
                                tickTime = 0.0f;
                                tickNum++;
                                Audio.Play($"event:/CommunalHelperEvents/game/zipMover/{themePath}/tick", Center);
                                StartShaking(0.1f);
                            }
                        }

                        if (tickNum == 5 && !HasPlayerRider()) {
                            shouldCancel = true;
                            break;
                        }
                    } else if (waits && !last) {
                        streetlight.SetAnimationFrame(1);
                        while (!HasPlayerRider()) {
                            yield return null;
                        }
                    }
                }

                if (!permanent) {
                    for (i -= 2 - (shouldCancel ? 1 : 0); i > -2; i--) {
                        to = (i == -1) ? start : targets[i];

                        // Goes back to start with a speed that is four times slower.
                        StopPlayerRunIntoAnimation = false;
                        streetlight.SetAnimationFrame(2);
                        sfx.Play($"event:/CommunalHelperEvents/game/zipMover/{themePath}/return");
                        at2 = 0f;
                        while (at2 < 1f) {
                            yield return null;
                            at2 = Calc.Approach(at2, 1f, 0.5f * Engine.DeltaTime);
                            percent = 1f - Ease.SineIn(at2);
                            Vector2 position = Vector2.Lerp(from, to, Ease.SineIn(at2));
                            MoveTo(position);
                        }
                        if (i != -1) {
                            from = targets[i];
                        }

                        StartShaking(0.2f);
                        Audio.Play($"event:/CommunalHelperEvents/game/zipMover/{themePath}/finish", Center);
                    }

                    StopPlayerRunIntoAnimation = true;

                    // Done, will not actiavte for 0.5 secs.
                    streetlight.SetAnimationFrame(1);
                    yield return 0.5f;
                } else {

                    // Done, will never be activated again.
                    StartShaking(0.3f);
                    Audio.Play($"event:/CommunalHelperEvents/game/zipMover/{themePath}/finish", Center);
                    Audio.Play($"event:/CommunalHelperEvents/game/zipMover/{themePath}/tick", Center);
                    SceneAs<Level>().Shake(0.15f);
                    streetlight.SetAnimationFrame(0);
                    while (true) {
                        yield return null;
                    }
                }
            }
        }
    }
}
