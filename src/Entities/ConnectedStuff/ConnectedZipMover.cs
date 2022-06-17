using Celeste.Mod.CommunalHelper.Entities;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper {
    [CustomEntity("CommunalHelper/ConnectedZipMover")]
    public class ConnectedZipMover : ConnectedSolid, IMultiNodeZipMover {
        public enum Themes {
            Normal,
            Moon,
            Cliffside
        }
        public Themes theme;

        private MTexture[,] edges = new MTexture[3, 3];
        private MTexture[,] innerCorners = new MTexture[2, 2];
        private Sprite streetlight;
        private List<MTexture> innerCogs;
        private MTexture temp = new MTexture();

        private bool drawBlackBorder;

        // Sounds
        private SoundSource sfx;

        // Lightning.
        private BloomPoint bloom;

        // The instance of the PathRenderer Class defined above.
        //private PathRenderer pathRenderer;
        private ZipMoverPathRenderer zipMoverPathRenderer;

        private bool permanent;
        private bool waits;
        private bool ticking;
        public Coroutine seq;

        private string themePath;
        private Color backgroundColor;

        // IMultiNodeZipMover implementation
        public Vector2[] Nodes { get; }
        public float Percent { get; set; }
        float IMultiNodeZipMover.Width => MasterWidth;
        float IMultiNodeZipMover.Height => MasterHeight;
        public Color RopeColor { get; set; } = Calc.HexToColor("663931");
        public Color RopeLightColor { get; set; } = Calc.HexToColor("9b6157");
        public MTexture Cog { get; }

        public ConnectedZipMover(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Width, data.Height, data.NodesWithPosition(offset),
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

            Nodes = nodes;

            theme = themes;
            permanent = perm;
            this.waits = waits;
            this.ticking = ticking;

            seq = new Coroutine(Sequence());
            Add(seq);

            Add(new LightOcclude());

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
                Cog = GFX.Game[customSkin + "/cog"];
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
                        Cog = GFX.Game["objects/zipmover/cog"];
                        themePath = "normal";
                        drawBlackBorder = true;
                        backgroundColor = Color.Black;
                        break;

                    case Themes.Moon:
                        path = "objects/zipmover/moon/light";
                        id = "objects/zipmover/moon/block";
                        key = "objects/zipmover/moon/innercog";
                        corners = "objects/CommunalHelper/zipmover/moon/innerCorners";
                        Cog = GFX.Game["objects/zipmover/moon/cog"];
                        themePath = "moon";
                        drawBlackBorder = false;
                        backgroundColor = Color.Black;
                        break;

                    case Themes.Cliffside:
                        path = "objects/CommunalHelper/connectedZipMover/cliffside/light";
                        id = "objects/CommunalHelper/connectedZipMover/cliffside/block";
                        key = "objects/CommunalHelper/connectedZipMover/cliffside/innercog";
                        corners = "objects/CommunalHelper/connectedZipMover/cliffside/innerCorners";
                        Cog = GFX.Game["objects/CommunalHelper/connectedZipMover/cliffside/cog"];
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
                    RopeColor = Calc.HexToColor(colorList[1]);
                }
                if (colorList.Length > 2) {
                    RopeLightColor = Calc.HexToColor(colorList[2]);
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

            // Creating the Path Renderer.
            scene.Add(zipMoverPathRenderer = new(this, depth: Depths.SolidsBelow));
        }

        public override void Removed(Scene scene) {
            scene.Remove(zipMoverPathRenderer);
            zipMoverPathRenderer = null;
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
                    int index = (int) (Util.Mod((num2 + num * Percent * (float) Math.PI * 4f) / ((float) Math.PI / 2f), 1f) * count);
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
                for (i = 1; i < Nodes.Length; i++) {
                    to = Nodes[i];

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
                        Percent = Ease.SineIn(at2);
                        Vector2 vector = Vector2.Lerp(from, to, Percent);

                        if (Scene.OnInterval(0.1f)) {
                            //pathRenderer.CreateSparks();
                            zipMoverPathRenderer.CreateSparks();                        
                        }

                        MoveTo(vector);

                        if (Scene.OnInterval(0.03f))
                            SpawnScrapeParticles();
                    }

                    bool last = (i == Nodes.Length - 1);

                    // Arrived, will wait for 0.5 secs.
                    StartShaking(0.2f);
                    Audio.Play($"event:/CommunalHelperEvents/game/zipMover/{themePath}/impact", Center);
                    streetlight.SetAnimationFrame(((waits && !last) || (ticking && !last) || (permanent && last)) ? 1 : 2);
                    Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                    SceneAs<Level>().Shake();
                    StopPlayerRunIntoAnimation = true;
                    yield return 0.5f;

                    from = Nodes[i];

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
                        to = (i == -1) ? start : Nodes[i];

                        // Goes back to start with a speed that is four times slower.
                        StopPlayerRunIntoAnimation = false;
                        streetlight.SetAnimationFrame(2);
                        sfx.Play($"event:/CommunalHelperEvents/game/zipMover/{themePath}/return");
                        at2 = 0f;
                        while (at2 < 1f) {
                            yield return null;
                            at2 = Calc.Approach(at2, 1f, 0.5f * Engine.DeltaTime);
                            Percent = 1f - Ease.SineIn(at2);
                            Vector2 position = Vector2.Lerp(from, to, Ease.SineIn(at2));
                            MoveTo(position);
                        }
                        if (i != -1) {
                            from = Nodes[i];
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
