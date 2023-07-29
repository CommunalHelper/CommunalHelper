using Celeste.Mod.CommunalHelper.Components;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/ElytraDashBlock")]
public class ElytraDashBlock : Solid
{
    private EntityID id;

    private readonly char tiletype;
    private readonly bool blendin;
    private readonly bool permanent;
    private readonly float requiredSpeed;

    private bool broken;

    public ElytraDashBlock(EntityData data, Vector2 offset, EntityID id)
        : this(data.Position + offset, data.Width, data.Height, id, data.Char("tiletype", '3'), data.Bool("blendin"), data.Bool("permanent", false), data.Float("requiredSpeed", 240.0f))
    { }

    public ElytraDashBlock(Vector2 position, float width, float height, EntityID id, char tiletype, bool blendin = true, bool permanent = false, float requiredSpeed = 240.0f)
        : base(position, width, height, safe: true)
    {
        Depth = Depths.FakeWalls + 1;

        this.id = id;
        this.permanent = permanent;
        this.blendin = blendin;
        this.tiletype = tiletype;
        this.requiredSpeed = requiredSpeed;

        SurfaceSoundIndex = SurfaceIndex.TileToIndex[this.tiletype];

        Add(new ElytraCollision(OnElytraCollide));
    }

    private ElytraCollision.Result OnElytraCollide(Player player)
    {
        if (player.Speed.Length() < requiredSpeed)
            return ElytraCollision.Result.Finish;

        Break(player.Center);
        Celeste.Freeze(0.05f);

        // continue flying
        return ElytraCollision.Result.Maintain;
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);

        int w = (int)Width / 8;
        int h = (int)Height / 8;

        TileGrid tileGrid;
        if (blendin)
        {
            Depth = Depths.FGDecals - 1;

            Level level = scene as Level;
            Rectangle tileBounds = level.Session.MapData.TileBounds;
            VirtualMap<char> solidsData = level.SolidsData;

            int x = (int)(X / 8) - tileBounds.Left;
            int y = (int)(Y / 8) - tileBounds.Top;

            tileGrid = GFX.FGAutotiler.GenerateOverlay(tiletype, x, y, w, h, solidsData).TileGrid;
            Add(new EffectCutout());
        }
        else
        {
            tileGrid = GFX.FGAutotiler.GenerateBox(tiletype, w, h).TileGrid;
            Add(new LightOcclude());
        }

        Add(tileGrid);
        Add(new TileInterceptor(tileGrid, highPriority: true));

        if (CollideCheck<Player>())
            RemoveSelf();
    }

    public void Break(Vector2 from)
    {
        if (broken)
            return;
        broken = true;

        string sfx = tiletype switch
        {
            '1' => SFX.game_gen_wallbreak_dirt,
            '3' => SFX.game_gen_wallbreak_ice,
            '9' => SFX.game_gen_wallbreak_wood,
            _ => SFX.game_gen_wallbreak_stone
        };
        Audio.Play(sfx, Center);

        int w = (int)Width / 8;
        int h = (int)Height / 8;
        for (int i = 0; i < w; i++)
        {
            for (int j = 0; j < h; j++)
            {
                Debris debris = Engine.Pooler.Create<Debris>()
                                             .Init(Position + new Vector2(4 + i * 8, 4 + j * 8), tiletype, true)
                                             .BlastFrom(from);
                Scene.Add(debris);
            }
        }

        Collidable = false;
        if (permanent)
            SceneAs<Level>().Session.DoNotLoad.Add(id);
        RemoveSelf();
    }
}
