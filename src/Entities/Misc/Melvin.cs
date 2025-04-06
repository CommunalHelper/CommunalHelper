using Celeste.Mod.CommunalHelper.Components;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ArrowDir = Celeste.Mod.CommunalHelper.Entities.StationBlock.ArrowDir;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/Melvin")]
public class Melvin : Solid
{
    private readonly ParticleType P_Activate;
    private readonly ParticleType P_Attack;

    internal readonly Color fill;

    #region Tiles
    // yeah.
    private readonly MTexture[,] strongBlock = new MTexture[4, 4];
    private readonly MTexture[,] weakBlock = new MTexture[4, 4];
    private readonly MTexture[,] litEdges = new MTexture[4, 4];
    private readonly MTexture[,] insideBlock = new MTexture[2, 2];
    private readonly MTexture[,] strongCorners = new MTexture[2, 2];
    private readonly MTexture[,] weakHCorners = new MTexture[2, 2];
    private readonly MTexture[,] weakVCorners = new MTexture[2, 2];
    private readonly MTexture[,] weakCorners = new MTexture[2, 2];
    private readonly MTexture[,] litHCornersFull = new MTexture[2, 2];
    private readonly MTexture[,] litHCornersCut = new MTexture[2, 2];
    private readonly MTexture[,] litVCornersFull = new MTexture[2, 2];
    private readonly MTexture[,] litVCornersCut = new MTexture[2, 2];
    #endregion

    internal struct MoveState
    {
        public readonly Vector2 From;
        public readonly Vector2 Direction;

        public MoveState(Vector2 from, Vector2 direction)
        {
            From = from;
            Direction = direction;
        }
    }

    internal readonly List<MoveState> returnStack;

    private Level level;

    private SoundSource currentMoveLoopSfx;
    private readonly SoundSource returnLoopSfx;

    internal Vector2 crushDir;
    private ArrowDir dir;
    private bool triggered = false;
    private Vector2 squishScale = Vector2.One;
    private readonly bool weakTop, weakBottom, weakLeft, weakRight;

    private readonly Sprite eye;

    private readonly List<Image> activeTopTiles = new();
    private readonly List<Image> activeBottomTiles = new();
    private readonly List<Image> activeRightTiles = new();
    private readonly List<Image> activeLeftTiles = new();
    private readonly List<Image> tiles = new();
    private float topTilesAlpha, bottomTilesAlpha, leftTilesAlpha, rightTilesAlpha;
    internal EntityData creationData;

    private bool Submerged => Scene.CollideCheck<Water>(new Rectangle((int) (Center.X - 4f), (int) Center.Y, 8, 4));

    private readonly Coroutine attackCoroutine;

    public Melvin(EntityData data, Vector2 offset)
        : this(data.Position + offset, data.Width, data.Height,
              data.Bool("weakTop", false), data.Bool("weakBottom", false), data.Bool("weakLeft", false), data.Bool("weakRight", false),
              data.Attr("spriteDir", ""), data.HexColor("fillColor", Calc.HexToColor("62222b")),
              data.HexColor("activateParticleColor", Calc.HexToColor("e45f7c")), data.HexColor("attackParticleColor", Calc.HexToColor("ffeb6b")), data.HexColor("attackParticleFadeColor", Calc.HexToColor("d39332")))
    { 
        creationData = data;
    }

    public Melvin(Vector2 position, int width, int height,
        bool up, bool down, bool left, bool right,
        string spriteDir, Color fillColor,
        Color activateParticleColor, Color attackParticleColor, Color attackParticleColor2)
        : base(position, width, height, safe: false)
    {
        returnStack = new List<MoveState>();

        weakTop = up;
        weakBottom = down;
        weakLeft = left;
        weakRight = right;

        bool useCustomSpriteDir = !string.IsNullOrWhiteSpace(spriteDir);
        SetupTextures(useCustomSpriteDir ? spriteDir : "objects/CommunalHelper/melvin");
        SetupTiles();
        Add(eye = useCustomSpriteDir ? SetupEye(spriteDir) : CommunalHelperGFX.SpriteBank.Create("melvinEye"));
        eye.Position = new Vector2(width / 2, height / 2);

        fill = fillColor;
        
        P_Activate = new ParticleType(CrushBlock.P_Activate)
        {
            Color = activateParticleColor
        };
        P_Attack = new ParticleType(SwitchGate.P_Behind)
        {
            Color = attackParticleColor,
            Color2 = attackParticleColor2
        };

        OnDashCollide = OnDashed;

        Add(new LightOcclude(0.2f));
        Add(returnLoopSfx = new SoundSource());
        Add(attackCoroutine = new Coroutine(false));

        Add(new WaterInteraction(new Rectangle(0, 0, width, height), () => crushDir != Vector2.Zero));
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);
        level = SceneAs<Level>();
    }
    
    public void SetupTextures(string path)
    {
        MTexture strongBlockTexture = GFX.Game[path + "/block_strong"];
        MTexture weakBlockTexture = GFX.Game[path + "/block_weak"];
        MTexture litEdgesTexture = GFX.Game[path + "/lit_edges"];
        MTexture weakHCornersTexture = GFX.Game[path + "/corners_weak_h"];
        MTexture weakVCornersTexture = GFX.Game[path + "/corners_weak_v"];
        MTexture insideBlockTexture = GFX.Game[path + "/inside"];
        MTexture litHCornersFullTexture = GFX.Game[path + "/lit_corners_h_full"];
        MTexture litHCornersCutTexture = GFX.Game[path + "/lit_corners_h_cut"];
        MTexture litVCornersFullTexture = GFX.Game[path + "/lit_corners_v_full"];
        MTexture litVCornersCutTexture = GFX.Game[path + "/lit_corners_v_cut"];

        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                int tx = i * 8;
                int ty = j * 8;

                strongBlock[i, j] = strongBlockTexture.GetSubtexture(tx, ty, 8, 8);
                weakBlock[i, j] = weakBlockTexture.GetSubtexture(tx, ty, 8, 8);
                litEdges[i, j] = litEdgesTexture.GetSubtexture(tx, ty, 8, 8);
                if (i < 2 && j < 2)
                {
                    int tx3 = 3 * tx;
                    int ty3 = 3 * ty;
                    insideBlock[i, j] = insideBlockTexture.GetSubtexture(tx, ty, 8, 8);
                    weakHCorners[i, j] = weakHCornersTexture.GetSubtexture(tx3, ty3, 8, 8);
                    weakVCorners[i, j] = weakVCornersTexture.GetSubtexture(tx3, ty3, 8, 8);
                    litHCornersFull[i, j] = litHCornersFullTexture.GetSubtexture(tx3, ty3, 8, 8);
                    litHCornersCut[i, j] = litHCornersCutTexture.GetSubtexture(tx3, ty3, 8, 8);
                    litVCornersFull[i, j] = litVCornersFullTexture.GetSubtexture(tx3, ty3, 8, 8);
                    litVCornersCut[i, j] = litVCornersCutTexture.GetSubtexture(tx3, ty3, 8, 8);
                }
                if ((i == 0 || i == 3) && (j == 0 || j == 3))
                {
                    int i_ = i == 0 ? 0 : 1;
                    int j_ = j == 0 ? 0 : 1;
                    strongCorners[i_, j_] = strongBlock[i, j];
                    weakCorners[i_, j_] = weakBlock[i, j];
                }
            }
        }
    }

    private void SetupTiles()
    {
        int w = (int) (Width / 8);
        int h = (int) (Height / 8);

        // middle & edges
        for (int i = 0; i < w; i++)
        {
            for (int j = 0; j < h; j++)
            {
                bool left = i == 0;
                bool right = i == w - 1;
                bool top = j == 0;
                bool bottom = j == h - 1;
                bool edge = left || right || top || bottom;
                bool corner = (left || right) && (top || bottom);

                int rx = Calc.Random.Choose(0, 1);
                int ry = Calc.Random.Choose(0, 1);
                Vector2 pos = new(i * 8, j * 8);

                if (!edge)
                {
                    // middle
                    Image tile = new(insideBlock[rx, ry])
                    {
                        Position = pos + new Vector2(Calc.Random.Range(-1, 2), Calc.Random.Range(-1, 2))
                    };
                    //Add(tile);
                    tiles.Add(tile);
                }
                else if (!corner)
                {
                    // edges
                    Image edgeTile = null, litEdgeTile = null;
                    if (right)
                    {
                        edgeTile = new Image((weakRight ? weakBlock : strongBlock)[3, 1 + ry]);
                        litEdgeTile = weakRight ? new Image(litEdges[3, 1 + ry]) : null;
                    }
                    if (left)
                    {
                        edgeTile = new Image((weakLeft ? weakBlock : strongBlock)[0, 1 + ry]);
                        litEdgeTile = weakLeft ? new Image(litEdges[0, 1 + ry]) : null;
                    }
                    if (top)
                    {
                        edgeTile = new Image((weakTop ? weakBlock : strongBlock)[1 + rx, 0]);
                        litEdgeTile = weakTop ? new Image(litEdges[1 + rx, 0]) : null;
                    }
                    if (bottom)
                    {
                        edgeTile = new Image((weakBottom ? weakBlock : strongBlock)[1 + rx, 3]);
                        litEdgeTile = weakBottom ? new Image(litEdges[1 + rx, 3]) : null;
                    }

                    if (edgeTile != null)
                    {
                        edgeTile.Position = pos;
                        //Add(edgeTile);
                        tiles.Add(edgeTile);
                    }
                    if (litEdgeTile != null)
                    {
                        litEdgeTile.Position = pos;
                        litEdgeTile.Color = Color.Transparent;

                        if (right)
                            activeRightTiles.Add(litEdgeTile);
                        if (left)
                            activeLeftTiles.Add(litEdgeTile);
                        if (bottom)
                            activeBottomTiles.Add(litEdgeTile);
                        if (top)
                            activeTopTiles.Add(litEdgeTile);
                        tiles.Add(litEdgeTile);
                        //Add(litEdgeTile);
                    }
                }
                else
                {
                    // corners
                    Image cornerTile = null, litCornerTile1 = null, litCornerTile2 = null;
                    if (left && top)
                    {
                        if (weakTop && weakLeft)
                        {
                            cornerTile = new Image(weakCorners[0, 0]);
                            activeLeftTiles.Add(litCornerTile1 = new Image(litHCornersFull[0, 0]));
                            activeTopTiles.Add(litCornerTile2 = new Image(litVCornersFull[0, 0]));
                        }
                        else if (!weakTop && weakLeft)
                        {
                            cornerTile = new Image(weakHCorners[0, 0]);
                            activeLeftTiles.Add(litCornerTile1 = new Image(litHCornersCut[0, 0]));
                        }
                        else if (weakTop && !weakLeft)
                        {
                            cornerTile = new Image(weakVCorners[0, 0]);
                            activeTopTiles.Add(litCornerTile1 = new Image(litVCornersCut[0, 0]));
                        }
                        else
                        {
                            cornerTile = new Image(strongCorners[0, 0]);
                        }
                    }
                    if (right && top)
                    {
                        if (weakTop && weakRight)
                        {
                            cornerTile = new Image(weakCorners[1, 0]);
                            activeRightTiles.Add(litCornerTile1 = new Image(litHCornersFull[1, 0]));
                            activeTopTiles.Add(litCornerTile2 = new Image(litVCornersFull[1, 0]));
                        }
                        else if (!weakTop && weakRight)
                        {
                            cornerTile = new Image(weakHCorners[1, 0]);
                            activeRightTiles.Add(litCornerTile1 = new Image(litHCornersCut[1, 0]));
                        }
                        else if (weakTop && !weakRight)
                        {
                            cornerTile = new Image(weakVCorners[1, 0]);
                            activeTopTiles.Add(litCornerTile1 = new Image(litVCornersCut[1, 0]));
                        }
                        else
                        {
                            cornerTile = new Image(strongCorners[1, 0]);
                        }
                    }
                    if (left && bottom)
                    {
                        if (weakBottom && weakLeft)
                        {
                            cornerTile = new Image(weakCorners[0, 1]);
                            activeLeftTiles.Add(litCornerTile1 = new Image(litHCornersFull[0, 1]));
                            activeBottomTiles.Add(litCornerTile2 = new Image(litVCornersFull[0, 1]));
                        }
                        else if (!weakBottom && weakLeft)
                        {
                            cornerTile = new Image(weakHCorners[0, 1]);
                            activeLeftTiles.Add(litCornerTile1 = new Image(litHCornersCut[0, 1]));
                        }
                        else if (weakBottom && !weakLeft)
                        {
                            cornerTile = new Image(weakVCorners[0, 1]);
                            activeBottomTiles.Add(litCornerTile1 = new Image(litVCornersCut[0, 1]));
                        }
                        else
                        {
                            cornerTile = new Image(strongCorners[0, 1]);
                        }
                    }
                    if (right && bottom)
                    {
                        if (weakBottom && weakRight)
                        {
                            cornerTile = new Image(weakCorners[1, 1]);
                            activeRightTiles.Add(litCornerTile1 = new Image(litHCornersFull[1, 1]));
                            activeBottomTiles.Add(litCornerTile2 = new Image(litVCornersFull[1, 1]));
                        }
                        else if (!weakBottom && weakRight)
                        {
                            cornerTile = new Image(weakHCorners[1, 1]);
                            activeRightTiles.Add(litCornerTile1 = new Image(litHCornersCut[1, 1]));
                        }
                        else if (weakBottom && !weakRight)
                        {
                            cornerTile = new Image(weakVCorners[1, 1]);
                            activeBottomTiles.Add(litCornerTile1 = new Image(litVCornersCut[1, 1]));
                        }
                        else
                        {
                            cornerTile = new Image(strongCorners[1, 1]);
                        }
                    }

                    if (cornerTile != null)
                    {
                        cornerTile.Position = pos;
                        tiles.Add(cornerTile);
                        //Add(cornerTile);
                    }
                    if (litCornerTile1 != null)
                    {
                        litCornerTile1.Position = pos;
                        litCornerTile1.Color = Color.Transparent;
                        tiles.Add(litCornerTile1);
                        //Add(litCornerTile1);
                    }
                    if (litCornerTile2 != null)
                    {
                        litCornerTile2.Position = pos;
                        litCornerTile2.Color = Color.Transparent;
                        tiles.Add(litCornerTile2);
                        //Add(litCornerTile2);
                    }
                }
            }
        }
    }

    private static Sprite SetupEye(string path)
    {
        Sprite eye = new Sprite(GFX.Game, path + "/eye/");

        // <Loop id="idle" path="idle_small" delay="0.08" frames="0"/>
        eye.AddLoop("idle", "idle_small", 0.08f, 0);
        // <Anim id="target" path="target_small" delay="0.1" frames="0-4"/>
        eye.Add("target", "target_small", 0.01f, 0, 1, 2, 3, 4);

        // <Anim id="targetUp" path="targetUp_small" delay="0.06" frames="0-2"/>
        eye.Add("targetUp", "targetUp_small", 0.06f, 0, 1, 2);
        // <Anim id="targetDown" path="targetDown_small" delay="0.06" frames="0-2"/>
        eye.Add("targetDown", "targetDown_small", 0.06f, 0, 1, 2);
        // <Anim id="targetLeft" path="targetLeft_small" delay="0.06" frames="0-2"/>
        eye.Add("targetLeft", "targetLeft_small", 0.06f, 0, 1, 2);
        // <Anim id="targetRight" path="targetRight_small" delay="0.06" frames="0-2"/>
        eye.Add("targetRight", "targetRight_small", 0.06f, 0, 1, 2);

        // <Anim id="targetReverseUp" path="targetUp_small" delay="0.25" frames="2,1,0" goto="idle"/>
        eye.Add("targetReverseUp", "targetUp_small", 0.25f, "idle", 2, 1, 0);
        // <Anim id="targetReverseDown" path="targetDown_small" delay="0.25" frames="2,1,0" goto="idle"/>
        eye.Add("targetReverseDown", "targetDown_small", 0.25f, "idle", 2, 1, 0);
        // <Anim id="targetReverseLeft" path="targetLeft_small" delay="0.25" frames="2,1,0" goto="idle"/>
        eye.Add("targetReverseLeft", "targetLeft_small", 0.25f, "idle", 2, 1, 0);
        // <Anim id="targetReverseRight" path="targetRight_small" delay="0.25" frames="2,1,0" goto="idle"/>
        eye.Add("targetReverseRight", "targetRight_small", 0.25f, "idle", 2, 1, 0);

        eye.Justify = Vector2.One * 0.5f;
        eye.Play("idle");
        
        return eye;
    }

    private void CreateMoveState()
    {
        bool flag = true;
        if (returnStack.Count > 0)
        {
            MoveState moveState = returnStack[returnStack.Count - 1];
            if (moveState.Direction == crushDir || moveState.Direction == -crushDir)
            {
                flag = false;
            }
        }
        if (flag)
        {
            returnStack.Add(new MoveState(Position, crushDir));
        }
    }

    private DashCollisionResults OnDashed(Player player, Vector2 direction)
    {
        bool playerAttacked = false;
        if (direction == Vector2.UnitX && weakLeft)
        {
            // left side
            playerAttacked = true;
            dir = ArrowDir.Right;
        }
        else if (direction == -Vector2.UnitX && weakRight)
        {
            // right side
            playerAttacked = true;
            dir = ArrowDir.Left;
        }
        else if (direction == Vector2.UnitY && weakTop)
        {
            // top side
            playerAttacked = true;
            dir = ArrowDir.Down;
        }
        else if (direction == -Vector2.UnitY && weakBottom)
        {
            // bottom side
            playerAttacked = true;
            dir = ArrowDir.Up;
        }

        if (playerAttacked)
        {
            squishScale = new Vector2(1f + (Math.Abs(direction.Y) * 0.4f) - (Math.Abs(direction.X) * 0.4f), 1f + (Math.Abs(direction.X) * 0.4f) - (Math.Abs(direction.Y) * 0.4f));
            crushDir = direction;
            Attack(true);
            return DashCollisionResults.Rebound;
        }
        return DashCollisionResults.NormalCollision;
    }

    internal void Attack(bool hurt)
    {
        triggered = true;

        if (currentMoveLoopSfx != null)
        {
            currentMoveLoopSfx.Param("end", 1f);
            SoundSource sfx = currentMoveLoopSfx;
            Alarm.Set(this, 0.5f, sfx.RemoveSelf);
        }
        Add(currentMoveLoopSfx = new SoundSource());
        currentMoveLoopSfx.Position = new Vector2(Width, Height) / 2f;

        attackCoroutine.Replace(AttackSequence(hurt));
    }

    private IEnumerator AttackSequence(bool hurt)
    {
        CreateMoveState();
        string animDir = Enum.GetName(typeof(ArrowDir), dir);

        ActivateTiles(dir);
        ActivateParticles();
        Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
        StartShaking(0.4f);

        Audio.Play(CustomSFX.game_melvin_seen_player, Center, "hurt", Util.ToInt(hurt));
        eye.Play("target", true);
        yield return .3f;
        currentMoveLoopSfx.Play(CustomSFX.game_melvin_move_loop);
        yield return .3f;

        eye.Play("target" + animDir, true);

        StopPlayerRunIntoAnimation = false;
        float speed = 0f;
        while (true)
        {
            speed = Calc.Approach(speed, 240f, 500f * Engine.DeltaTime);

            bool flag = (crushDir.X == 0f) ? MoveVCheck(speed * crushDir.Y * Engine.DeltaTime) : MoveHCheck(speed * crushDir.X * Engine.DeltaTime);
            if (Top >= (level.Bounds.Bottom + 32))
            {
                RemoveSelf();
                yield break;
            }
            if (flag)
            {
                break;
            }
            if (Scene.OnInterval(0.02f))
            {
                Vector2 position;
                float direction;
                if (crushDir == Vector2.UnitX)
                {
                    position = new Vector2(Left + 1f, Calc.Random.Range(Top + 3f, Bottom - 3f));
                    direction = (float) Math.PI;
                }
                else if (crushDir == -Vector2.UnitX)
                {
                    position = new Vector2(Right - 1f, Calc.Random.Range(Top + 3f, Bottom - 3f));
                    direction = 0f;
                }
                else if (crushDir == Vector2.UnitY)
                {
                    position = new Vector2(Calc.Random.Range(Left + 3f, Right - 3f), Top + 1f);
                    direction = -(float) Math.PI / 2f;
                }
                else
                {
                    position = new Vector2(Calc.Random.Range(Left + 3f, Right - 3f), Bottom - 1f);
                    direction = (float) Math.PI / 2f;
                }
                level.Particles.Emit(P_Attack, position, direction);
            }
            yield return null;
        }

        FallingBlock fallingBlock = CollideFirst<FallingBlock>(Position + crushDir);
        if (fallingBlock != null)
        {
            fallingBlock.Triggered = true;
        }
        if (crushDir == -Vector2.UnitX)
        {
            Vector2 offset = new(0f, 2f);
            for (int y = 0; y < Height / 8f; y++)
            {
                Vector2 pos = new(Left - 1f, Top + 4f + (y * 8));
                if (!Scene.CollideCheck<Water>(pos) && Scene.CollideCheck<Solid>(pos))
                {
                    SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, pos + offset, 0f);
                    SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, pos - offset, 0f);
                }
            }
        }
        else if (crushDir == Vector2.UnitX)
        {
            Vector2 offset = new(0f, 2f);
            for (int y = 0; y < Height / 8f; y++)
            {
                Vector2 pos = new(Right + 1f, Top + 4f + (y * 8));
                if (!Scene.CollideCheck<Water>(pos) && Scene.CollideCheck<Solid>(pos))
                {
                    SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, pos + offset, (float) Math.PI);
                    SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, pos - offset, (float) Math.PI);
                }
            }
        }
        else if (crushDir == -Vector2.UnitY)
        {
            Vector2 offset = new(2f, 0f);
            for (int x = 0; x < Width / 8f; x++)
            {
                Vector2 pos = new(Left + 4f + (x * 8), Top - 1f);
                if (!Scene.CollideCheck<Water>(pos) && Scene.CollideCheck<Solid>(pos))
                {
                    SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, pos + offset, (float) Math.PI / 2f);
                    SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, pos - offset, (float) Math.PI / 2f);
                }
            }
        }
        else if (crushDir == Vector2.UnitY)
        {
            Vector2 offset = new(2f, 0f);
            for (int x = 0; x < Width / 8f; x++)
            {
                Vector2 pos = new(Left + 4f + (x * 8), Bottom + 1f);
                if (!Scene.CollideCheck<Water>(pos) && Scene.CollideCheck<Solid>(pos))
                {
                    SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, pos + offset, -(float) Math.PI / 2f);
                    SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, pos - offset, -(float) Math.PI / 2f);
                }
            }
        }

        Audio.Play(CustomSFX.game_melvin_impact, Center);
        level.DirectionalShake(crushDir);
        Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
        StartShaking(0.4f);
        StopPlayerRunIntoAnimation = true;

        SoundSource sfx = currentMoveLoopSfx;
        currentMoveLoopSfx.Param("end", 1f);
        currentMoveLoopSfx = null;
        Alarm.Set(this, 0.5f, sfx.RemoveSelf);

        eye.Play("targetReverse" + animDir, true);
        crushDir = Vector2.Zero;
        returnLoopSfx.Play(SFX.game_06_crushblock_return_loop);
        yield return .4f;

        speed = 0f;
        float waypointSfxDelay = 0f;
        while (returnStack.Count > 0)
        {
            yield return null;
            StopPlayerRunIntoAnimation = false;
            MoveState moveState = returnStack[returnStack.Count - 1];
            speed = Calc.Approach(speed, 60f, 160f * Engine.DeltaTime);
            waypointSfxDelay -= Engine.DeltaTime;
            if (moveState.Direction.X != 0f)
            {
                MoveTowardsX(moveState.From.X, speed * Engine.DeltaTime);
            }
            if (moveState.Direction.Y != 0f)
            {
                MoveTowardsY(moveState.From.Y, speed * Engine.DeltaTime);
            }
            if ((moveState.Direction.X != 0f && ExactPosition.X != moveState.From.X) || (moveState.Direction.Y != 0f && ExactPosition.Y != moveState.From.Y))
            {
                continue;
            }

            speed = 0f;
            returnStack.RemoveAt(returnStack.Count - 1);
            StopPlayerRunIntoAnimation = true;
            if (returnStack.Count <= 0)
            {
                if (waypointSfxDelay <= 0f)
                {
                    returnLoopSfx.Stop();
                    Audio.Play(SFX.game_06_crushblock_rest, Center);
                }
            }
            else if (waypointSfxDelay <= 0f)
            {
                Audio.Play(SFX.game_06_crushblock_rest_waypoint, Center);
            }
            waypointSfxDelay = 0.1f;
            StartShaking(0.2f);
            yield return 0.2f;
        }

        triggered = false;
    }

    private void OnCollideSolid(Vector2 vec1, Vector2 vec2, Platform platform)
    {
        if (platform is SeekerBarrier seekerBarrier)
        {
            seekerBarrier.OnReflectSeeker();
            Audio.Play("event:/game/05_mirror_temple/seeker_hit_lightwall", Center);
        }
    }

    private bool MoveHCheck(float amount)
    {
        if (MoveHCollideSolidsAndBounds(level, amount, thruDashBlocks: true, OnCollideSolid))
        {
            if (amount < 0f && Left <= level.Bounds.Left)
            {
                return true;
            }
            if (amount > 0f && Right >= level.Bounds.Right)
            {
                return true;
            }
            for (int i = 1; i <= 4; i++)
            {
                for (int num = 1; num >= -1; num -= 2)
                {
                    Vector2 value = new(Math.Sign(amount), i * num);
                    if (!CollideCheck<Solid>(Position + value))
                    {
                        MoveVExact(i * num);
                        MoveHExact(Math.Sign(amount));
                        return false;
                    }
                }
            }
            return true;
        }
        return false;
    }

    private bool MoveVCheck(float amount)
    {
        if (MoveVCollideSolidsAndBounds(level, amount, thruDashBlocks: true, OnCollideSolid, checkBottom: false))
        {
            if (amount < 0f && Top <= level.Bounds.Top)
            {
                return true;
            }
            for (int i = 1; i <= 4; i++)
            {
                for (int num = 1; num >= -1; num -= 2)
                {
                    Vector2 value = new(i * num, Math.Sign(amount));
                    if (!CollideCheck<Solid>(Position + value))
                    {
                        MoveHExact(i * num);
                        MoveVExact(Math.Sign(amount));
                        return false;
                    }
                }
            }
            return true;
        }
        return false;
    }

    public override void MoveVExact(int move)
    {
        bool before = SetSeekerBarriersCollidable(false);
        base.MoveVExact(move);
        SetSeekerBarriersCollidable(before);
    }

    public override void MoveHExact(int move)
    {
        bool before = SetSeekerBarriersCollidable(false);
        base.MoveHExact(move);
        SetSeekerBarriersCollidable(before);
    }

    // returns collidable field before calling this function
    private bool SetSeekerBarriersCollidable(bool collidable)
    {
        bool before = !collidable;
        foreach (SeekerBarrier entity in Scene.Tracker.GetEntities<SeekerBarrier>())
        {
            before = entity.Collidable;
            entity.Collidable = collidable;
        }
        return before;
    }

    private void ActivateParticles()
    {
        float direction;
        Vector2 position;
        Vector2 positionRange;
        int amount;
        if (dir == ArrowDir.Right)
        {
            direction = 0f;
            position = CenterRight - Vector2.UnitX;
            positionRange = Vector2.UnitY * (Height - 2f) * 0.5f;
            amount = (int) (Height / 8f) * 4;
        }
        else if (dir == ArrowDir.Left)
        {
            direction = (float) Math.PI;
            position = CenterLeft + Vector2.UnitX;
            positionRange = Vector2.UnitY * (Height - 2f) * 0.5f;
            amount = (int) (Height / 8f) * 4;
        }
        else if (dir == ArrowDir.Down)
        {
            direction = (float) Math.PI / 2f;
            position = BottomCenter - Vector2.UnitY;
            positionRange = Vector2.UnitX * (Width - 2f) * 0.5f;
            amount = (int) (Width / 8f) * 4;
        }
        else
        {
            direction = -(float) Math.PI / 2f;
            position = TopCenter + Vector2.UnitY;
            positionRange = Vector2.UnitX * (Width - 2f) * 0.5f;
            amount = (int) (Width / 8f) * 4;
        }
        amount += 2;
        level.Particles.Emit(P_Activate, amount, position, positionRange, direction);
    }

    private void ActivateTiles(ArrowDir dir)
    {
        switch (dir)
        {
            case ArrowDir.Up:
                topTilesAlpha = 1f;
                break;
            case ArrowDir.Down:
                bottomTilesAlpha = 1f;
                break;
            case ArrowDir.Left:
                leftTilesAlpha = 1f;
                break;
            case ArrowDir.Right:
                rightTilesAlpha = 1f;
                break;

            default:
                break;
        }
    }

    private void UpdateActiveTiles()
    {
        topTilesAlpha = Calc.Approach(topTilesAlpha, triggered && dir == ArrowDir.Up && crushDir != Vector2.Zero ? 1f : 0f, Engine.DeltaTime * 2f);
        bottomTilesAlpha = Calc.Approach(bottomTilesAlpha, triggered && dir == ArrowDir.Down && crushDir != Vector2.Zero ? 1f : 0f, Engine.DeltaTime * 2f);
        leftTilesAlpha = Calc.Approach(leftTilesAlpha, triggered && dir == ArrowDir.Left && crushDir != Vector2.Zero ? 1f : 0f, Engine.DeltaTime * 2f);
        rightTilesAlpha = Calc.Approach(rightTilesAlpha, triggered && dir == ArrowDir.Right && crushDir != Vector2.Zero ? 1f : 0f, Engine.DeltaTime * 2f);

        foreach (Image tile in activeTopTiles)
        {
            tile.Color = Color.White * topTilesAlpha;
        }
        foreach (Image tile in activeBottomTiles)
        {
            tile.Color = Color.White * bottomTilesAlpha;
        }
        foreach (Image tile in activeLeftTiles)
        {
            tile.Color = Color.White * leftTilesAlpha;
        }
        foreach (Image tile in activeRightTiles)
        {
            tile.Color = Color.White * rightTilesAlpha;
        }
    }

    private bool IsPlayerSeen(Rectangle rect, ArrowDir dir)
    {
        if (dir is ArrowDir.Up or ArrowDir.Down)
        {
            for (int i = 0; i < rect.Width; i++)
            {
                Rectangle lineRect = new(rect.X + i, rect.Y, 1, rect.Height);
                if (!Scene.CollideCheck<Solid>(lineRect))
                    return true;
            }
            return false;
        }
        else
        {
            for (int i = 0; i < rect.Height; i++)
            {
                Rectangle lineRect = new(rect.X, rect.Y + i, rect.Width, 1);
                if (!Scene.CollideCheck<Solid>(lineRect))
                    return true;
            }
            return false;
        }
    }

    private IEnumerable<Entity> GetTargetsByDistance()
    {
        List<Component> targets = SceneAs<Level>().Tracker.GetComponents<MelvinTargetable>();
        targets.Sort((target1, target2) => Vector2.DistanceSquared(Center, target1.Entity.Position).CompareTo(Vector2.DistanceSquared(Center, target2.Entity.Position)));
        return targets.Select(t => t.Entity);
    }

    public override void Update()
    {
        SetSeekerBarriersCollidable(true);
        base.Update();

        eye.Scale = squishScale;
        squishScale = Calc.Approach(squishScale, Vector2.One, Engine.DeltaTime * 4f);

        if (!triggered)
        {
            foreach (Entity target in GetTargetsByDistance())
            {
                bool detectedTarget = false;
                Rectangle toTargetRect = new();
                if (target.Center.Y > Y && target.Center.Y < Y + Height)
                {
                    int y1 = (int) Math.Max(target.Top, Top);
                    int y2 = (int) Math.Min(target.Bottom, Bottom);
                    if (target.Center.X > X + Width)
                    {
                        // right
                        detectedTarget = true;
                        crushDir = Vector2.UnitX;
                        dir = ArrowDir.Right;
                        toTargetRect = new Rectangle((int) (X + Width), y1, (int) (target.Left - X - Width), y2 - y1);
                    }
                    if (target.Center.X < X)
                    {
                        // left
                        detectedTarget = true;
                        crushDir = -Vector2.UnitX;
                        dir = ArrowDir.Left;
                        toTargetRect = new Rectangle((int) target.Right, y1, (int) (X - target.Right), y2 - y1);
                    }
                }
                if (target.Center.X > X && target.Center.X < X + Width)
                {
                    int x1 = (int) Math.Max(target.Left, Left);
                    int x2 = (int) Math.Min(target.Right, Right);
                    if (target.Center.Y < Y)
                    {
                        // top
                        detectedTarget = true;
                        crushDir = -Vector2.UnitY;
                        dir = ArrowDir.Up;
                        toTargetRect = new Rectangle(x1, (int) target.Bottom, x2 - x1, (int) (Y - target.Bottom));
                    }
                    if (target.Center.Y > Y + Height)
                    {
                        // bottom
                        detectedTarget = true;
                        crushDir = Vector2.UnitY;
                        dir = ArrowDir.Down;
                        toTargetRect = new Rectangle(x1, (int) (Y + Height), x2 - x1, (int) (target.Top - Y - Height));
                    }
                }
                
                if (!detectedTarget || !IsPlayerSeen(toTargetRect, dir))
                    continue;
                
                Attack(false);
                break;
            }
        }
        SetSeekerBarriersCollidable(false);

        UpdateActiveTiles();

        currentMoveLoopSfx?.Param("submerged", Submerged ? 1 : 0);
        returnLoopSfx?.Param("submerged", Submerged ? 1 : 0);
    }

    public override void Render()
    {
        Vector2 position = Position;
        Position += Shake;

        Rectangle rect = new(
            (int) (Center.X + ((X + 2 - Center.X) * squishScale.X)),
            (int) (Center.Y + ((Y + 2 - Center.Y) * squishScale.Y)),
            (int) ((Width - 4) * squishScale.X),
            (int) ((Height - 4) * squishScale.Y));

        Draw.Rect(rect, fill);

        foreach (Image img in tiles)
        {
            Vector2 pos = Position + img.Position + new Vector2(4, 4);
            pos = Center + ((pos - Center) * squishScale);
            img.Texture?.DrawCentered(pos, img.Color, squishScale);
        }

        base.Render();
        Position = position;
    }
}
