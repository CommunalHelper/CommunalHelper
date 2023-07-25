using MonoMod.Utils;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Entities;

public abstract class AeroBlock : Solid
{
    protected sealed class Outline : Entity
    {
        private readonly AeroBlock parent;

        public Outline(AeroBlock parent)
        {
            this.parent = parent;
            Depth = Depths.SolidsBelow;
        }

        public override void Render()
        {
            Rectangle bounds = parent.Collider.Bounds;
            bounds.X += (int) parent.Shake.X;
            bounds.Y += (int) parent.Shake.Y;
            bounds.Inflate(1, 1);
            Draw.Rect(bounds, outlineColor);
        }
    }

    private static readonly Color screenColor = Color.Black * 0.68f;
    private static readonly Color rasterColor = new(0, 0, 4);
    private static readonly Color noiseColor = Color.White * 0.07f;
    private static readonly Color outlineColor = Color.Lerp(Color.Black, Calc.HexToColor("280606"), 0.5f);

    public static ParticleType P_Steam { get; private set; }

    private Outline outline;
    private static MTexture[] innerCogs;

    private uint noise;
    private readonly HashSet<AeroScreen> screens = new(), removed = new();

    protected readonly HashSet<JumpThru> jumpthrus = new();
    protected readonly HashSet<Solid> sidewaysJumpthruSolids = new();
    private const string AttachedSidewaysJumpThru = "Celeste.Mod.MaxHelpingHand.Entities.AttachedSidewaysJumpThru";

    protected float Rotation { get; set; }

    private Image[,] tiles;

    public AeroBlock(Vector2 position, int width, int height)
        : base(position, width, height, safe: false)
    {
        Depth = Depths.Solids;
        SurfaceSoundIndex = SurfaceIndex.ResortBoxes;

        Add(new SoundSource(CustomSFX.game_aero_block_static)
        {
            Position = new Vector2(width, height) / 2f,
        });
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);
        scene.Add(outline = new(this));
        RemakeBlockTiles(force: false);
    }

    protected void RemakeBlockTiles(string path = "objects/CommunalHelper/aero_block/blocks/nnn", bool force = true)
    {
        if (tiles is not null)
        {
            if (!force)
                return;

            foreach (Image image in tiles)
                Remove(image);
        }

        int w = (int) (Width / 8);
        int h = (int) (Height / 8);

        tiles = new Image[w, h];

        MTexture nineSlice = GFX.Game[path];
        for (int i = 0; i < w; i++)
        {
            for (int j = 0; j < h; j++)
            {
                int tx = i == 0 ? 0 : (i == w - 1 ? 16 : 8);
                int ty = j == 0 ? 0 : (j == h - 1 ? 16 : 8);
                if (tx is not 8 || ty is not 8)
                    Add(tiles[i, j] = new Image(nineSlice.GetSubtexture(tx, ty, 8, 8))
                    {
                        Position = new(i * 8, j * 8),
                    });
            }
        }
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);

        foreach (JumpThru jt in scene.Tracker.GetEntities<JumpThru>().Cast<JumpThru>())
            if (jt.CollideCheckOutside(this, jt.Position - Vector2.UnitX) || jt.CollideCheckOutside(this, jt.Position + Vector2.UnitX))
                jumpthrus.Add(jt);

        foreach (StaticMover sm in staticMovers)
        {
            if (sm.Entity.GetType().ToString() is not AttachedSidewaysJumpThru)
                continue;
            DynamicData data = DynamicData.For(sm.Entity);
            Solid solid = data.Get<Solid>("playerInteractingSolid");
            sidewaysJumpthruSolids.Add(solid);
        }
    }

    public override void Removed(Scene scene)
    {
        scene.Remove(outline);
        outline = null;

        base.Removed(scene);
    }

    public bool PlayerRiding()
    {
        foreach (JumpThru jt in jumpthrus)
            if (jt.HasPlayerRider())
                return true;
        foreach (Solid solid in sidewaysJumpthruSolids)
        {
            solid.Collidable = true;
            bool check = solid.HasPlayerRider();
            solid.Collidable = false;
            if (check is not false)
                return check;
        }
        return HasPlayerRider();
    }

    public void AddScreenLayer(AeroScreen screen)
    {
        screen.Block = this;
        screens.Add(screen);
    }

    public void RemoveScreenLayer(AeroScreen screen)
        => removed.Add(screen);

    public bool HasScreenLayer(AeroScreen screen)
        => screens.Contains(screen);

    public void FlushScreenLayerRemoval()
    {
        foreach (AeroScreen screen in removed)
        {
            screens.Remove(screen);
            screen.Finish();
        }
        removed.Clear();
    }

    public void MoveJumpthrus(Vector2 move)
    {
        foreach (JumpThru jt in jumpthrus)
        {
            jt.MoveH(move.X);
            jt.MoveV(move.Y);
        }
    }

    public void MoveJumpthrus(Vector2 move, Vector2 liftspeed)
    {
        foreach (JumpThru jt in jumpthrus)
        {
            jt.MoveH(move.X, liftspeed.X);
            jt.MoveV(move.Y, liftspeed.Y);
        }
    }

    public override void Update()
    {
        if (Scene.OnInterval(0.1f))
            ++noise;

        foreach (AeroScreen screen in screens)
            if (screen.Period <= 0.0f || Scene.OnInterval(screen.Period))
                screen.Update();
        FlushScreenLayerRemoval();

        base.Update();
    }

    public override void Render()
    {
        Position += Shake;
        Rectangle bounds = Collider.Bounds;

        Draw.Rect(bounds, Color.Black);

        int rot = (int) (Rotation * 5);
        int w = (int) Width / 8;
        int h = (int) Height / 8;
        for (int i = 0; i < w; i++)
        {
            for (int j = 0; j < h; j++)
            {
                int index = i * h + j + rot;
                MTexture tex = innerCogs[index % innerCogs.Length];

                Rectangle crop = new(0, 0, tex.Width, tex.Height);
                Vector2 offset = Vector2.Zero;

                if (i == 0)
                {
                    offset.X = 2;
                    crop.X = 2;
                    crop.Width -= 2;
                }
                else if (i == w - 1)
                {
                    offset.X = -2;
                    crop.Width -= 2;
                }

                if (j == 0)
                {
                    offset.Y = 2;
                    crop.Y = 2;
                    crop.Height -= 2;
                }
                else if (j == h - 1)
                {
                    offset.Y = -2;
                    crop.Height -= 2;
                }

                Color color = Color.White;
                if ((i + j) % 2 == 0)
                    color *= 0.75f;

                Vector2 at = new(i * 8 + 4, j * 8 + 4);
                tex.GetSubtexture(crop).DrawCentered(Position + at + offset, color);
            }
        }

        Draw.Rect(bounds, screenColor);

        foreach (AeroScreen screen in screens)
            screen.Render();

        uint seed = noise;
        PlaybackBillboard.DrawNoise(bounds, ref seed, noiseColor);
        for (int i = (int) Y; i < Bottom; i += 2)
        {
            float num = 0.2f + (1f + (float) Math.Sin(i / 16f + Scene.TimeActive * 5f)) / 2f * 0.2f;
            Draw.Line(X, i, X + Width, i, rasterColor * num);
        }

        base.Render();

        Position -= Shake;
    }

    internal static void Initialize()
    {
        P_Steam = new(ParticleTypes.Chimney)
        {
            LifeMin = 1f,
            LifeMax = 3f,
            SizeRange = 0.8f,
            Acceleration = Vector2.UnitY,
            SpeedMin = 4f,
            SpeedMax = 30f,
            DirectionRange = MathHelper.PiOver4,
        };

        //AeroBlockFailure.P_PurpleSmash = new(CrushBlock.P_Activate)
        //{
        //    Color = Color.MediumPurple,
        //    Color2 = Color.Lerp(Color.MediumPurple, Color.White, 0.45f),
        //};
    }

    internal static void LoadContent()
    {
        innerCogs = GFX.Game.GetAtlasSubtextures("objects/CommunalHelper/aero_block/innercogs/").ToArray();

        AeroBlockCharged.ButtonFillTexture = GFX.Game["objects/CommunalHelper/aero_block/button/fill"];
        AeroBlockCharged.ButtonOutlineTexture = GFX.Game["objects/CommunalHelper/aero_block/button/outline"];
    }
}

