using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using Node = Celeste.Mod.CommunalHelper.Entities.StationBlockTrack.Node;
using TrackSwitchState = Celeste.Mod.CommunalHelper.Entities.StationBlockTrack.TrackSwitchState;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/StationBlock")]
    [Tracked(false)]
    public class StationBlock : Solid {
        public enum Themes {
            Normal, Moon
        }

        private MTexture[,] tileSlices, blockTiles, buttonTiles;
        private Sprite arrowSprite;

        private bool allowWavedash = false;

        public MTexture CustomNode = null, CustomTrackV = null, CustomTrackH = null;

        private static ParticleType P_BlueSparks;
        private static ParticleType P_PurpleSparks;
        private ParticleType p_sparks;

        private ArrowDir arrowDir;
        private float percent = 0f;

        public enum ArrowDir {
            Up,
            Right,
            Down,
            Left,
        }

        private bool IsMoving = false;
        private Vector2 scale = Vector2.One;

        private Vector2 MoveDir;
        public bool reverseControls = false;

        private Vector2 offset;
        private Vector2 hitOffset;
        private SoundSource Sfx;
        public Themes Theme = Themes.Moon;

        private static readonly Color activatedButton = Calc.HexToColor("f25eff");
        private static readonly Color deactivatedButton = Calc.HexToColor("5bf75b");

        private Color buttonColor, buttonPressedColor;
        private float colorLerp = 0.0f;

        private float speedFactor = 1f;

        public bool IsAttachedToTrack = false;
        private Node CurrentNode = null;

        private bool dashCornerCorrection;

        public StationBlock(EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height, safe: true) {
            Depth = Depths.FGTerrain + 1;
            Add(new LightOcclude());

            this.offset = new Vector2(Width, Height) / 2f;
            allowWavedash = data.Bool("allowWavedash", false);
            speedFactor = Calc.Clamp(data.Float("speedFactor", 1f), .1f, 2f);

            buttonColor = data.HexColor("wavedashButtonColor", deactivatedButton);
            buttonPressedColor = data.HexColor("wavedashButtonPressedColor", activatedButton);

            dashCornerCorrection = data.Bool("dashCornerCorrection", false);

            int minSize = (int) Calc.Min(Width, Height);
            string size;
            if (minSize <= 16)
                size = "small";
            else if (minSize <= 24)
                size = "medium";
            else
                size = "big";

            string block = "objects/CommunalHelper/stationBlock/blocks/";
            string sprite;
            reverseControls = data.Attr("behavior", "Pulling") == "Pushing";
            Theme = data.Enum<Themes>("theme");

            string customBlockPath = data.Attr("customBlockPath").Trim();
            string customArrowPath = data.Attr("customArrowPath").Trim();
            string customTrackPath = data.Attr("customTrackPath").Trim();

            switch (Theme) {
                default:
                case Themes.Normal:
                    if (reverseControls) {
                        block += "alt_block";
                        sprite = size + "AltStationBlockArrow";
                    } else {
                        block += "block";
                        sprite = size + "StationBlockArrow";
                    }
                    break;

                case Themes.Moon:
                    Theme = Themes.Moon;
                    if (reverseControls) {
                        block += "alt_moon_block";
                        sprite = size + "AltMoonStationBlockArrow";
                    } else {
                        block += "moon_block";
                        sprite = size + "MoonStationBlockArrow";
                    }
                    break;
            }

            if (allowWavedash)
                block += "_button";

            MTexture customBlock = null;
            Sprite customArrow = null;

            p_sparks = Theme == Themes.Normal ? ZipMover.P_Sparks : (reverseControls ? P_PurpleSparks : P_BlueSparks);

            if (customBlockPath != "") {
                customBlock = GFX.Game["objects/" + customBlockPath];
                p_sparks = ZipMover.P_Sparks;
            }
            if (customArrowPath != "") {
                customArrow = LookForCustomSprite("objects/" + customArrowPath, size);
            }
            if (customTrackPath != "") {
                CustomNode = GFX.Game["objects/" + customTrackPath + "/node"];
                CustomTrackV = GFX.Game["objects/" + customTrackPath + "/trackv"];
                CustomTrackH = GFX.Game["objects/" + customTrackPath + "/trackh"];
            }

            tileSlices = new MTexture[3, 3];
            for (int i = 0; i < 3; i++) {
                for (int j = 0; j < 3; j++) {
                    tileSlices[i, j] = (customBlock ?? GFX.Game[block]).GetSubtexture(i * 8, j * 8, 8, 8);
                }
            }

            /* 
             * the first dimension here represents the part of the button (left, middle, right), 
             * the second (0 or 1) will represent whether we want to draw the outline or the front.
             */
            buttonTiles = new MTexture[3, 2];
            for (int i = 0; i < 3; i++) {
                buttonTiles[i, 0] = GFX.Game["objects/CommunalHelper/stationBlock/button_outline"].GetSubtexture(i * 8, 0, 8, 8);
                buttonTiles[i, 1] = GFX.Game["objects/CommunalHelper/stationBlock/button"].GetSubtexture(i * 8, 0, 8, 8);
            }

            GenerateTiles();

            arrowSprite = customArrow ?? CommunalHelperModule.SpriteBank.Create(sprite);
            arrowDir = ArrowDir.Up;
            arrowSprite.Position = new Vector2(Width / 2, Height / 2);
            Add(arrowSprite);

            SurfaceSoundIndex = SurfaceIndex.Girder;
            Add(new Coroutine(Sequence()));
            Add(Sfx = new SoundSource());
            Sfx.Position = this.offset;
            OnDashCollide = OnDashed;
        }

        private static Sprite LookForCustomSprite(string path, string image) {
            path += "/";

            float delay;
            switch (image) {
                default:
                case "big":
                    delay = 0.06f;
                    break;
                case "medium":
                    image = "med";
                    delay = 0.05f;
                    break;
                case "small":
                    delay = 0.04f;
                    break;
            }

            Sprite sprite = new Sprite(GFX.Game, path);

            sprite.JustifyOrigin(.5f, .5f);

            // <Loop id="IdleUp" path="small" frames="0" delay="0.04"/>
            // <Loop id="IdleRight" path="small" frames="4" delay="0.04"/>
            // <Loop id="IdleDown" path="small" frames="8" delay="0.04"/>
            // <Loop id="IdleLeft" path="small" frames="12" delay="0.04"/>
            sprite.Add("IdleUp", image, delay, 0);
            sprite.Add("IdleRight", image, delay, 4);
            sprite.Add("IdleDown", image, delay, 8);
            sprite.Add("IdleLeft", image, delay, 12);

            // <Anim id="UpToUp" path="small" frames="0,1,0,15,0" delay="0.04" goto="IdleUp"/>
            // <Anim id="RightToRight" path="small" frames="4,3,4,5,4" delay="0.04" goto="IdleRight"/>
            // <Anim id="DownToDown" path="small" frames="8,9,8,7,8" delay="0.04" goto="IdleDown"/>
            // <Anim id="LeftToLeft" path="small" frames="12,13,12,11,12" delay="0.04" goto="IdleLeft"/>
            sprite.Add("UpToUp", image, delay, "IdleUp", 0, 1, 0, 15, 0);
            sprite.Add("RightToRight", image, delay, "IdleRight", 4, 3, 4, 5, 4);
            sprite.Add("DownToDown", image, delay, "IdleDown", 8, 9, 8, 7, 8);
            sprite.Add("LeftToLeft", image, delay, "IdleLeft", 12, 13, 12, 11, 12);

            // <Anim id="UpToRight" path="small" frames="0-4" delay="0.04" goto="IdleRight"/>
            // <Anim id="UpToDown" path="small" frames="0,1,2,6,7,8" delay="0.04" goto="IdleDown"/>
            // <Anim id="UpToLeft" path="small" frames="0,15,14,13,12" delay="0.04" goto="IdleLeft"/>
            sprite.Add("UpToRight", image, delay, "IdleRight", 0, 1, 2, 3, 4);
            sprite.Add("UpToDown", image, delay, "IdleDown", 0, 1, 2, 6, 7, 8);
            sprite.Add("UpToLeft", image, delay, "IdleLeft", 0, 15, 14, 13, 12);

            // <Anim id="RightToUp" path="small" frames="4,3,2,1,0" delay="0.04" goto="IdleUp"/>
            // <Anim id="RightToDown" path="small" frames="4-8" delay="0.04" goto="IdleDown"/>
            // <Anim id="RightToLeft" path="small" frames="4,5,6,10,11,12" delay="0.04" goto="IdleLeft"/>
            sprite.Add("RightToUp", image, delay, "IdleUp", 4, 3, 2, 1, 0);
            sprite.Add("RightToDown", image, delay, "IdleDown", 4, 5, 6, 7, 8);
            sprite.Add("RightToLeft", image, delay, "IdleLeft", 4, 5, 6, 10, 11, 12);

            // <Anim id="DownToRight" path="small" frames="8,7,6,5,4" delay="0.04" goto="IdleRight"/>
            // <Anim id="DownToUp" path="small" frames="8,9,10,14,15,0" delay="0.04" goto="IdleUp"/>
            // <Anim id="DownToLeft" path="small" frames="8-12" delay="0.04" goto="IdleLeft"/>
            sprite.Add("DownToRight", image, delay, "IdleRight", 8, 7, 6, 5, 4);
            sprite.Add("DownToUp", image, delay, "IdleUp", 8, 9, 10, 14, 15, 0);
            sprite.Add("DownToLeft", image, delay, "IdleLeft", 8, 9, 10, 11, 12);

            // <Anim id="LeftToRight" path="small" frames="12,13,14,2,3,4" delay="0.04" goto="IdleRight"/>
            // <Anim id="LeftToUp" path="small" frames="12,13,14,15,0" delay="0.04" goto="IdleUp"/>
            // <Anim id="LeftToDown" path="small" frames="12,11,10,9,8" delay="0.04" goto="IdleDown"/>
            sprite.Add("LeftToRight", image, delay, "IdleRight", 12, 13, 14, 2, 3, 4);
            sprite.Add("LeftToUp", image, delay, "IdleUp", 12, 13, 14, 15, 0);
            sprite.Add("LeftToDown", image, delay, "IdleDown", 12, 11, 10, 9, 8);

            sprite.CenterOrigin();

            return sprite;
        }

        private void GenerateTiles() {
            int tileWidth = (int) (Width / 8f);
            int tileHeight = (int) (Height / 8f);
            blockTiles = new MTexture[tileWidth, tileHeight];
            for (int i = 0; i < tileWidth; i++) {
                for (int j = 0; j < tileHeight; j++) {
                    int x = (i != 0) ? ((i != tileWidth - 1f) ? 1 : 2) : 0;
                    int y = (j != 0) ? ((j != tileHeight - 1f) ? 1 : 2) : 0;
                    blockTiles[i, j] = tileSlices[x, y];
                }
            }
        }

        public void Attach(Node node) {
            IsAttachedToTrack = true;
            CurrentNode = node;

            if (node.nodeUp != null)
                arrowDir = ArrowDir.Up;
            else if (node.nodeRight != null)
                arrowDir = ArrowDir.Right;
            else if (node.nodeLeft != null)
                arrowDir = ArrowDir.Left;
            else if (node.nodeDown != null)
                arrowDir = ArrowDir.Down;

            arrowSprite.Play("Idle" + Enum.GetName(typeof(ArrowDir), arrowDir), true);
        }

        private DashCollisionResults OnDashed(Player player, Vector2 dir) {
            // Weird, lame fix, but eh.
            if (player.StateMachine.State == 5)
                player.StateMachine.State = 0;

            // Easier wall bounces.
            if ((player.Left >= Right - 4f || player.Right < Left + 4f) && dir.Y == -1 && dashCornerCorrection) {
                return DashCollisionResults.NormalCollision;
            }

            if (IsMoving || !IsAttachedToTrack || (player.CollideCheck<Spikes>() && !SaveData.Instance.Assists.Invincible)) {
                return DashCollisionResults.NormalCollision;
            } else {
                scale = new Vector2(
                    1f + Math.Abs(dir.Y) * 0.35f - Math.Abs(dir.X) * 0.35f,
                    1f + Math.Abs(dir.X) * 0.35f - Math.Abs(dir.Y) * 0.35f);
                hitOffset = dir * 5f;

                MoveDir = reverseControls ? dir : -dir;
                IsMoving = true;
                if (allowWavedash && dir.Y == 1) {
                    colorLerp = 1f;
                    return DashCollisionResults.NormalCollision;
                }
                return DashCollisionResults.Rebound;
            }
        }

        private string GetTurnAnim(ArrowDir from, Vector2 dirTo) {
            ArrowDir to = ArrowDir.Down;
            if (dirTo == -Vector2.UnitX)
                to = ArrowDir.Left;
            if (dirTo == Vector2.UnitX)
                to = ArrowDir.Right;
            if (dirTo == -Vector2.UnitY)
                to = ArrowDir.Up;

            arrowDir = to;
            return GetAnimName(from, to);
        }

        private string GetAnimName(ArrowDir from, ArrowDir to) {
            return Enum.GetName(typeof(ArrowDir), from) + "To" + Enum.GetName(typeof(ArrowDir), to);
        }

        private void ScrapeParticlesCheck(Vector2 to) {
            if (!Scene.OnInterval(0.03f)) {
                return;
            }
            bool flag = to.Y != ExactPosition.Y;
            bool flag2 = to.X != ExactPosition.X;
            if (flag && !flag2) {
                int num = Math.Sign(to.Y - ExactPosition.Y);
                Vector2 value = (num != 1) ? TopLeft : BottomLeft;
                int num2 = 4;
                if (num == 1) {
                    num2 = Math.Min((int) Height - 12, 20);
                }
                int num3 = (int) Height;
                if (num == -1) {
                    num3 = Math.Max(16, (int) Height - 16);
                }
                if (Scene.CollideCheck<Solid>(value + new Vector2(-2f, num * -2))) {
                    for (int i = num2; i < num3; i += 8) {
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, TopLeft + new Vector2(0f, i + num * 2f), (num == 1) ? (-(float) Math.PI / 4f) : ((float) Math.PI / 4f));
                    }
                }
                if (Scene.CollideCheck<Solid>(value + new Vector2(Width + 2f, num * -2))) {
                    for (int j = num2; j < num3; j += 8) {
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, TopRight + new Vector2(-1f, j + num * 2f), (num == 1) ? ((float) Math.PI * -3f / 4f) : ((float) Math.PI * 3f / 4f));
                    }
                }
            } else {
                if (!flag2 || flag) {
                    return;
                }
                int num4 = Math.Sign(to.X - ExactPosition.X);
                Vector2 value2 = (num4 != 1) ? TopLeft : TopRight;
                int num5 = 4;
                if (num4 == 1) {
                    num5 = Math.Min((int) Width - 12, 20);
                }
                int num6 = (int) Width;
                if (num4 == -1) {
                    num6 = Math.Max(16, (int) Width - 16);
                }
                if (Scene.CollideCheck<Solid>(value2 + new Vector2(num4 * -2, -2f))) {
                    for (int k = num5; k < num6; k += 8) {
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, TopLeft + new Vector2(k + num4 * 2f, -1f), (num4 == 1) ? ((float) Math.PI * 3f / 4f) : ((float) Math.PI / 4f));
                    }
                }
                if (Scene.CollideCheck<Solid>(value2 + new Vector2(num4 * -2, Height + 2f))) {
                    for (int l = num5; l < num6; l += 8) {
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, BottomLeft + new Vector2(l + num4 * 2f, 0f), (num4 == 1) ? ((float) Math.PI * -3f / 4f) : (-(float) Math.PI / 4f));
                    }
                }
            }
        }

        private IEnumerator Sequence() {
            while (true) {
                while (!IsMoving) {
                    yield return null;
                }

                StartShaking(0.2f);
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);

                Node nextNode = null;
                StationBlockTrack currentTrack = null;
                float f = 1f;
                if (MoveDir == -Vector2.UnitY && CurrentNode.nodeUp != null) {
                    nextNode = CurrentNode.nodeUp;
                    currentTrack = CurrentNode.trackUp;
                    f = -1f;
                } else
                if (MoveDir == Vector2.UnitY && CurrentNode.nodeDown != null) {
                    nextNode = CurrentNode.nodeDown;
                    currentTrack = CurrentNode.trackDown;
                } else
                if (MoveDir == -Vector2.UnitX && CurrentNode.nodeLeft != null) {
                    nextNode = CurrentNode.nodeLeft;
                    currentTrack = CurrentNode.trackLeft;
                    f = -1f;
                } else
                if (MoveDir == Vector2.UnitX && CurrentNode.nodeRight != null) {
                    nextNode = CurrentNode.nodeRight;
                    currentTrack = CurrentNode.trackRight;
                }

                bool travel = nextNode != null && currentTrack.SwitchState != TrackSwitchState.Off;
                Sfx.Play("event:/CommunalHelperEvents/game/stationBlock/" + (Theme == Themes.Normal ? "station" : "moon") + "_block_seq", "travel", travel ? 1f : 0f);
                if (travel) {
                    Safe = false;

                    arrowSprite.Play(GetTurnAnim(arrowDir, MoveDir), true);

                    yield return 0.2f;

                    float t = 0f;
                    StopPlayerRunIntoAnimation = false;
                    Vector2 start = CurrentNode.Center - offset;
                    Vector2 target = nextNode.Center - offset;
                    while (t < 1f) {
                        t = Calc.Approach(t, 1f, speedFactor * 2f * Engine.DeltaTime);

                        percent = Ease.SineIn(t);
                        currentTrack.trackOffset = f * percent * 16;
                        CurrentNode.percent = nextNode.percent = currentTrack.percent = percent;

                        Vector2 vector = Vector2.Lerp(start, target, percent);
                        ScrapeParticlesCheck(vector);
                        if (Scene.OnInterval(0.05f)) {
                            currentTrack.CreateSparks(Center, p_sparks);
                        }

                        MoveTo(vector);
                        yield return null;
                    }
                    StartShaking(0.2f);
                    Sfx.Param(Theme == Themes.Moon ? "end_moon" : "end", 1);
                    Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                    SceneAs<Level>().Shake(0.2f);
                    StopPlayerRunIntoAnimation = true;
                    currentTrack.trackOffset = 0f;
                    CurrentNode.percent = nextNode.percent = currentTrack.percent = percent = 0f;
                    CurrentNode = nextNode;
                } else {
                    arrowSprite.Play(GetAnimName(arrowDir, arrowDir), true);
                    yield return 0.25f;
                }
                Safe = true;
                IsMoving = false;
            }
        }

        public override void Render() {
            Vector2 position = Position;
            Position += Shake;

            int tileWidth = (int) (Width / 8f);
            int tileHeight = (int) (Height / 8f);

            for (int i = 0; i < tileWidth; i++) {
                for (int j = 0; j < tileHeight; j++) {
                    Vector2 vec = new Vector2(X + i * 8, Y + j * 8) + (Vector2.One * 4f) + hitOffset;
                    vec.X = Center.X + (vec.X - Center.X) * scale.X;
                    vec.Y = Center.Y + (vec.Y - Center.Y) * scale.Y;

                    // Button rendering
                    if (j == 0 && allowWavedash) {
                        int tx = i == 0 ? 0 : (i == tileWidth - 1 ? 2 : 1);
                        Vector2 vec2 = new Vector2(X + i * 8, Y - 4 + (HasPlayerOnTop() ? 1 : 0)) + (Vector2.One * 4f) + hitOffset;
                        vec2.X = Center.X + (vec2.X - Center.X) * scale.X;
                        vec2.Y = Center.Y + (vec2.Y - Center.Y) * scale.Y;
                        Color c = Color.Lerp(buttonColor, buttonPressedColor, colorLerp);
                        buttonTiles[tx, 0].DrawCentered(vec2, c, scale);
                        buttonTiles[tx, 1].DrawCentered(vec2, c, scale);
                    }

                    blockTiles[i, j].DrawCentered(vec, Color.White, scale);
                }
            }

            base.Render();
            Position = position;
        }

        public override void Update() {
            base.Update();
            arrowSprite.Scale = scale;
            arrowSprite.Position = hitOffset + offset;

            colorLerp = Calc.Approach(colorLerp, 0f, 1.25f * Engine.DeltaTime);

            scale.X = Calc.Approach(scale.X, 1f, Engine.DeltaTime * 4f);
            scale.Y = Calc.Approach(scale.Y, 1f, Engine.DeltaTime * 4f);
            hitOffset.X = Calc.Approach(hitOffset.X, 0f, Engine.DeltaTime * 15f);
            hitOffset.Y = Calc.Approach(hitOffset.Y, 0f, Engine.DeltaTime * 15f);
        }

        public static void InitializeParticles() {
            P_BlueSparks = new ParticleType(ZipMover.P_Sparks);
            P_BlueSparks.Color = Calc.HexToColor("30a0e6");

            P_PurpleSparks = new ParticleType(ZipMover.P_Sparks);
            P_PurpleSparks.Color = Calc.HexToColor("aa51d6");
        }
    }
}
