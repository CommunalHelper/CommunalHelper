using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/StationBlock")]
    [Tracked(false)]
    class StationBlock : Solid {
        public enum Theme {
            Normal, Moon
        }

        public static SpriteBank StationBlockSpriteBank;

        private MTexture[,] tileSlices, blockTiles;
        private Sprite arrowSprite;

        private static ParticleType P_BlueSparks;
        private static ParticleType P_PurpleSparks;

        private ArrowDir arrowDir;
        private float percent = 0f;

        private enum ArrowDir {
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
        public Theme theme = Theme.Moon;

        public bool IsAttachedToTrack = false;
        private StationBlockTrack.Node CurrentNode = null;

        public StationBlock(EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height, safe: true) {
            Depth = -9999;
            Add(new LightOcclude());

            this.offset = new Vector2(Width, Height) / 2f;

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
            theme = data.Enum<Theme>("theme");

            switch (theme) {
                default:
                case Theme.Normal:
                    if (reverseControls) {
                        block += "alt_block";
                        sprite = size + "AltStationBlockArrow";
                    } else {
                        block += "block";
                        sprite = size + "StationBlockArrow";
                    }
                    break;

                case Theme.Moon:
                    theme = Theme.Moon;
                    if (reverseControls) {
                        block += "alt_moon_block";
                        sprite = size + "AltMoonStationBlockArrow";
                    } else {
                        block += "moon_block";
                        sprite = size + "MoonStationBlockArrow";
                    }
                    break;
            }

            tileSlices = new MTexture[3, 3];
            for (int i = 0; i < 3; i++) {
                for (int j = 0; j < 3; j++) {
                    tileSlices[i, j] = GFX.Game[block].GetSubtexture(i * 8, j * 8, 8, 8);
                }
            }

            GenerateTiles();

            arrowSprite = StationBlockSpriteBank.Create(sprite);
            arrowSprite.Position = this.offset;
            arrowDir = ArrowDir.Up;
            Add(arrowSprite);

            SurfaceSoundIndex = SurfaceIndex.Girder;
            Add(new Coroutine(Sequence()));
            Add(Sfx = new SoundSource());
            Sfx.Position = this.offset;
            OnDashCollide = OnDashed;
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

        public void Attach(StationBlockTrack.Node node) {
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

        public override void Awake(Scene scene) {
            base.Awake(scene);
        }

        private DashCollisionResults OnDashed(Player player, Vector2 dir) {
            // Weird, lame fix, but eh.
            if (player.StateMachine.State == 5)
                player.StateMachine.State = 0;

            if (IsMoving || !IsAttachedToTrack || (player.CollideCheck<Spikes>() && !SaveData.Instance.Assists.Invincible)) {
                return DashCollisionResults.NormalCollision;
            } else {
                scale = new Vector2(
                    1f + Math.Abs(dir.Y) * 0.35f - Math.Abs(dir.X) * 0.35f,
                    1f + Math.Abs(dir.X) * 0.35f - Math.Abs(dir.Y) * 0.35f);
                hitOffset = dir * 5f;

                MoveDir = reverseControls ? dir : -dir;
                IsMoving = true;
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

                StationBlockTrack.Node nextNode = null;
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

                Sfx.Play("event:/CommunalHelperEvents/game/stationBlock/" + (theme == Theme.Normal ? "station" : "moon") + "_block_seq", "travel", nextNode == null ? 0f : 1f);
                if (nextNode != null) {
                    Safe = false;

                    arrowSprite.Play(GetTurnAnim(arrowDir, MoveDir), true);

                    yield return 0.2f;

                    float t = 0f;
                    StopPlayerRunIntoAnimation = false;
                    Vector2 start = CurrentNode.Center - offset;
                    Vector2 target = nextNode.Center - offset;
                    while (t < 1f) {
                        t = Calc.Approach(t, 1f, 2f * Engine.DeltaTime);

                        percent = Ease.SineIn(t);
                        currentTrack.trackOffset = f * percent * 16;
                        CurrentNode.percent = nextNode.percent = currentTrack.percent = percent;

                        Vector2 vector = Vector2.Lerp(start, target, percent);
                        ScrapeParticlesCheck(vector);
                        if (Scene.OnInterval(0.05f)) {
                            currentTrack.CreateSparks(Center, theme == Theme.Normal ? ZipMover.P_Sparks : (reverseControls ? P_PurpleSparks : P_BlueSparks));
                        }

                        MoveTo(vector);
                        yield return null;
                    }
                    StartShaking(0.2f);
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
                    blockTiles[i, j].DrawCentered(vec, Color.White, scale);
                }
            }

            base.Render();
            Position = position;
        }

        public override void Update() {
            base.Update();
            arrowSprite.Scale = scale;
            arrowSprite.Position = (new Vector2(Width, Height) / 2f) + hitOffset;

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
