using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Celeste.Mod.CommunalHelper.Entities.StationBlockTrack;

namespace Celeste.Mod.CommunalHelper.Entities
{
    [CustomEntity("CommunalHelper/StationBlock")]
    [Tracked(false)]
    class StationBlock : Solid
    {
        public enum Theme
        {
            Normal, Moon
        }

        public static SpriteBank StationBlockSpriteBank;

        private MTexture[,] tileSlices, blockTiles;
        private Sprite arrowSprite;

        private static ParticleType P_BlueSparks;
        private static ParticleType P_PurpleSparks;

        private string arrowDir;
        private float percent = 0f;

        private static readonly string UP = "Up";
        private static readonly string RIGHT = "Right";
        private static readonly string DOWN = "Down";
        private static readonly string LEFT = "Left";

        private bool IsMoving = false;
        private Vector2 scale = Vector2.One;

        private Vector2 MoveDir;
        public bool reverseControls = false;

        private Vector2 offset, hitOffset = Vector2.Zero;
        private SoundSource Sfx;
        public Theme theme = Theme.Moon;

        public bool IsAttachedToTrack = false;
        private StationBlockNode CurrentNode = null;

        public StationBlock(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Width, data.Height, data.Attr("theme", "Normal"), data.Attr("behavior", "Pulling"))
        { }

        public StationBlock(Vector2 position, int width, int height, string theme_, string behavior)
            : base(position, width, height, safe: true)
        {
            base.Depth = -9999;
            Add(new LightOcclude());

            offset = new Vector2(width, height) / 2f;
            int minSize = (int)Calc.Min(width, height);
            string size;
            if (minSize <= 16) size = "small";
            else if (minSize <= 24) size = "medium";
            else size = "big";

            string block, sprite;
            reverseControls = behavior == "Pushing";

            switch (theme_)
            {
                default:
                case "Normal":
                    theme = Theme.Normal;
                    if (reverseControls)
                    {
                        block = "objects/CommunalHelper/stationBlock/blocks/alt_block";
                        sprite = size + "AltStationBlockArrow";
                    }
                    else
                    {
                        block = "objects/CommunalHelper/stationBlock/blocks/block";
                        sprite = size + "StationBlockArrow";
                    }
                    break;

                case "Moon":
                    theme = Theme.Moon;
                    if (reverseControls)
                    {
                        block = "objects/CommunalHelper/stationBlock/blocks/alt_moon_block";
                        sprite = size + "AltMoonStationBlockArrow";
                    }
                    else
                    {
                        block = "objects/CommunalHelper/stationBlock/blocks/moon_block";
                        sprite = size + "MoonStationBlockArrow";
                    }
                    break;
            }

            tileSlices = new MTexture[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    tileSlices[i, j] = GFX.Game[block].GetSubtexture(i * 8, j * 8, 8, 8);
                }
            }

            GenerateTiles();

            arrowSprite = StationBlockSpriteBank.Create(sprite);
            arrowSprite.Position = offset;
            arrowDir = UP;
            Add(arrowSprite);

            SurfaceSoundIndex = 7;
            Add(new Coroutine(Sequence()));
            Add(Sfx = new SoundSource());
            Sfx.Position = offset;
            OnDashCollide = OnDashed;
        }

        private void GenerateTiles()
        {
            int tileWidth = (int)(base.Width / 8f);
            int tileHeight = (int)(base.Height / 8f);
            blockTiles = new MTexture[tileWidth, tileHeight];
            for (int i = 0; i < tileWidth; i++)
            {
                for (int j = 0; j < tileHeight; j++)
                {
                    int x = (i != 0) ? ((i != tileWidth - 1f) ? 1 : 2) : 0;
                    int y = (j != 0) ? ((j != tileHeight - 1f) ? 1 : 2) : 0;
                    blockTiles[i, j] = tileSlices[x, y];
                }
            }
        }

        public void Attach(StationBlockNode node)
        {
            IsAttachedToTrack = true;
            CurrentNode = node;

            if (node.nodeUp != null) arrowDir = UP;
            else if (node.nodeRight != null) arrowDir = RIGHT;
            else if (node.nodeLeft != null) arrowDir = LEFT;
            else if (node.nodeDown != null) arrowDir = DOWN;

            arrowSprite.Play("Idle" + arrowDir, true);
        }

        public override void Awake(Scene scene)
        {
            base.Awake(scene);
        }

        private DashCollisionResults OnDashed(Player player, Vector2 dir)
        {
            if (IsMoving || !IsAttachedToTrack || (player.CollideCheck<Spikes>() && !SaveData.Instance.Assists.Invincible))
            {
                return DashCollisionResults.NormalCollision;
            }
            else
            {
                scale = new Vector2(
                    1f + Math.Abs(dir.Y) * 0.35f - Math.Abs(dir.X) * 0.35f,
                    1f + Math.Abs(dir.X) * 0.35f - Math.Abs(dir.Y) * 0.35f);
                hitOffset = dir * 5f;
            
                MoveDir = reverseControls ? dir : -dir;
                IsMoving = true;
                return DashCollisionResults.Rebound;
            }
        }

        private string GetTurnAnim(string from, Vector2 dirTo)
        {
            string to = DOWN;
            if (dirTo == -Vector2.UnitX) to = LEFT;
            if (dirTo == Vector2.UnitX) to = RIGHT;
            if (dirTo == -Vector2.UnitY) to = UP;

            arrowDir = to;
            return from + "To" + to;
        }

        private void ScrapeParticlesCheck(Vector2 to)
        {
            if (!base.Scene.OnInterval(0.03f))
            {
                return;
            }
            bool flag = to.Y != base.ExactPosition.Y;
            bool flag2 = to.X != base.ExactPosition.X;
            if (flag && !flag2)
            {
                int num = Math.Sign(to.Y - base.ExactPosition.Y);
                Vector2 value = (num != 1) ? base.TopLeft : base.BottomLeft;
                int num2 = 4;
                if (num == 1)
                {
                    num2 = Math.Min((int)base.Height - 12, 20);
                }
                int num3 = (int)base.Height;
                if (num == -1)
                {
                    num3 = Math.Max(16, (int)base.Height - 16);
                }
                if (base.Scene.CollideCheck<Solid>(value + new Vector2(-2f, num * -2)))
                {
                    for (int i = num2; i < num3; i += 8)
                    {
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, base.TopLeft + new Vector2(0f, (float)i + (float)num * 2f), (num == 1) ? (-(float)Math.PI / 4f) : ((float)Math.PI / 4f));
                    }
                }
                if (base.Scene.CollideCheck<Solid>(value + new Vector2(base.Width + 2f, num * -2)))
                {
                    for (int j = num2; j < num3; j += 8)
                    {
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, base.TopRight + new Vector2(-1f, (float)j + (float)num * 2f), (num == 1) ? ((float)Math.PI * -3f / 4f) : ((float)Math.PI * 3f / 4f));
                    }
                }
            }
            else
            {
                if (!flag2 || flag)
                {
                    return;
                }
                int num4 = Math.Sign(to.X - base.ExactPosition.X);
                Vector2 value2 = (num4 != 1) ? base.TopLeft : base.TopRight;
                int num5 = 4;
                if (num4 == 1)
                {
                    num5 = Math.Min((int)base.Width - 12, 20);
                }
                int num6 = (int)base.Width;
                if (num4 == -1)
                {
                    num6 = Math.Max(16, (int)base.Width - 16);
                }
                if (base.Scene.CollideCheck<Solid>(value2 + new Vector2(num4 * -2, -2f)))
                {
                    for (int k = num5; k < num6; k += 8)
                    {
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, base.TopLeft + new Vector2((float)k + (float)num4 * 2f, -1f), (num4 == 1) ? ((float)Math.PI * 3f / 4f) : ((float)Math.PI / 4f));
                    }
                }
                if (base.Scene.CollideCheck<Solid>(value2 + new Vector2(num4 * -2, base.Height + 2f)))
                {
                    for (int l = num5; l < num6; l += 8)
                    {
                        SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, base.BottomLeft + new Vector2((float)l + (float)num4 * 2f, 0f), (num4 == 1) ? ((float)Math.PI * -3f / 4f) : (-(float)Math.PI / 4f));
                    }
                }
            }
        }

        private IEnumerator Sequence()
        {
            while(true)
            {
                while(!IsMoving)
                {
                    yield return null;
                }

                StartShaking(0.2f);
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);

                StationBlockNode nextNode = null;
                StationBlockTrack currentTrack = null;
                float f = 1f;
                if(MoveDir == -Vector2.UnitY && CurrentNode.nodeUp != null)
                {
                    nextNode = CurrentNode.nodeUp; currentTrack = CurrentNode.trackUp; f = -1f;
                } else
                if (MoveDir == Vector2.UnitY && CurrentNode.nodeDown != null)
                {
                    nextNode = CurrentNode.nodeDown; currentTrack = CurrentNode.trackDown;
                } else
                if (MoveDir == -Vector2.UnitX && CurrentNode.nodeLeft != null)
                {
                    nextNode = CurrentNode.nodeLeft; currentTrack = CurrentNode.trackLeft; f = -1f;
                } else
                if (MoveDir == Vector2.UnitX && CurrentNode.nodeRight != null)
                {
                    nextNode = CurrentNode.nodeRight; currentTrack = CurrentNode.trackRight;
                }

                Sfx.Play("event:/CommunalHelperEvents/game/stationBlock/" + (theme == Theme.Normal ? "station" : "moon") + "_block_seq", "travel", nextNode == null ? 0f : 1f);
                if (nextNode != null)
                {
                    Safe = false;
                    string anim = GetTurnAnim(arrowDir, MoveDir);
                    arrowSprite.Play(anim, true);
                     
                    yield return 0.2f;

                    float t = 0f;
                    StopPlayerRunIntoAnimation = false;
                    Vector2 start = CurrentNode.Center - offset;
                    Vector2 target = nextNode.Center - offset;
                    while (t < 1f)
                    {
                        t = Calc.Approach(t, 1f, 2f * Engine.DeltaTime);

                        percent = Ease.SineIn(t);
                        currentTrack.trackOffset = f * percent * 16;
                        CurrentNode.percent = nextNode.percent = currentTrack.percent = percent;

                        Vector2 vector = Vector2.Lerp(start, target, percent);
                        ScrapeParticlesCheck(vector);
                        if (Scene.OnInterval(0.05f))
                        {
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
                } else
                {
                    arrowSprite.Play(arrowDir + "To" + arrowDir, true);
                    yield return 0.25f;
                }
                Safe = true;
                IsMoving = false;
            }
        }

        public override void Render()
        {
            Vector2 position = Position;
            Position += Shake;

            int tileWidth = (int)(base.Width / 8f);
            int tileHeight = (int)(base.Height / 8f);

            for (int i = 0; i < tileWidth; i++)
            {
                for (int j = 0; j < tileHeight; j++)
                {
                    Vector2 vec = new Vector2(base.X + i * 8, base.Y + j * 8) + (Vector2.One * 4f) + hitOffset;
                    vec.X = Center.X + (vec.X - Center.X) * scale.X; vec.Y = Center.Y + (vec.Y - Center.Y) * scale.Y;
                    blockTiles[i, j].DrawCentered(vec, Color.White, scale);
                }
            }

            base.Render();
            Position = position;
        }

        public override void Update()
        {
            base.Update();
            arrowSprite.Scale = scale;
            arrowSprite.Position = (new Vector2(Width, Height) / 2f) + hitOffset;

            scale.X = Calc.Approach(scale.X, 1f, Engine.DeltaTime * 4f);
            scale.Y = Calc.Approach(scale.Y, 1f, Engine.DeltaTime * 4f);
            hitOffset.X = Calc.Approach(hitOffset.X, 0f, Engine.DeltaTime * 15f);
            hitOffset.Y = Calc.Approach(hitOffset.Y, 0f, Engine.DeltaTime * 15f);
        }

        public static void InitializeParticles()
        {
            P_BlueSparks = new ParticleType(ZipMover.P_Sparks);
            P_BlueSparks.Color = Calc.HexToColor("30a0e6");

            P_PurpleSparks = new ParticleType(ZipMover.P_Sparks);
            P_PurpleSparks.Color = Calc.HexToColor("aa51d6");
        }
    }
}
