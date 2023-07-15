using MonoMod.Cil;
using MonoMod.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Entities;

[TrackedAs(typeof(CassetteBlock), true)]
[CustomEntity("CommunalHelper/CustomCassetteBlock")]
public class CustomCassetteBlock : CassetteBlock
{
    public static List<string> CustomCassetteBlockNames = new();

    public static void Initialize()
    {
        // Overengineered attempt to handle adding a CassetteBlockController when CustomCassetteBlock types are present
        // Actual loading handled in the OnLoadEntity handler
        IEnumerable<Type> customCassetteBlockTypes =
            from module in Everest.Modules
            from type in module.GetType().Assembly.GetTypesSafe()
            where typeof(CustomCassetteBlock).IsAssignableFrom(type)
            select type;

        // This could all be contained in the linq query but that'd be a bit much, no?
        foreach (Type type in customCassetteBlockTypes)
        {
            foreach (CustomEntityAttribute attrib in type.GetCustomAttributes<CustomEntityAttribute>())
            {
                foreach (string idFull in attrib.IDs)
                {
                    string id = idFull.Split('=')[0].Trim();
                    CustomCassetteBlockNames.Add(id);
                }
            }
        }
    }

    protected Color[] colorOptions = new Color[] {
        Calc.HexToColor("49aaf0"),
        Calc.HexToColor("f049be"),
        Calc.HexToColor("fcdc3a"),
        Calc.HexToColor("38e04e")
    };
    protected Color color;
    protected Color pressedColor;

    protected int blockHeight
    {
        get => blockData.Get<int>("blockHeight");
        set => blockData.Set("blockHeight", value);
    }
    /// <summary>
    /// Block offset based on <c>(2 - <see cref="blockHeight"/>)</c>
    /// </summary>
    protected Vector2 blockOffset => Vector2.UnitY * (2 - blockHeight);
    private readonly bool dynamicHitbox;
    private readonly Hitbox[] hitboxes;

    private bool present = true;
    /// <summary>
    /// Whether the block is actually collidable, not just according to cassette state.
    /// </summary>
    public bool Present
    {
        get => present;
        set
        {
            present = value;
            // Update collision immediately without waiting for Update
            Collidable = value && virtualCollidable;
        }
    }
    // Whether the block is collidable according to cassette state
    private bool virtualCollidable = true;

    protected DynamicData blockData;

    public CustomCassetteBlock(EntityData data, Vector2 offset, EntityID id)
        : this(data.Position + offset, id, data.Width, data.Height, data.Int("index"), data.Float("tempo", 1f), false, data.HexColorNullable("customColor")) { }

    public CustomCassetteBlock(Vector2 position, EntityID id, int width, int height, int index, float tempo, bool dynamicHitbox = false, Color? overrideColor = null)
        : base(position, id, width, height, index, tempo)
    {
        blockData = new(typeof(CassetteBlock), this);

        Index = index;
        color = overrideColor ?? colorOptions[index];
        pressedColor = color.Mult(Calc.HexToColor("667da5"));

        blockData.Set("color", color);

        this.dynamicHitbox = dynamicHitbox;
        if (dynamicHitbox)
        {
            hitboxes = new Hitbox[3];
            hitboxes[0] = new Hitbox(Collider.Width, Collider.Height - 2);
            hitboxes[1] = new Hitbox(Collider.Width, Collider.Height - 1);
            hitboxes[2] = Collider as Hitbox;
        }
    }

    public override void Update()
    {
        if (!Present)
        {
            Collidable = virtualCollidable;
        }

        base.Update();
        // Update what the cassette state dictates collision should be
        virtualCollidable = Collidable;

        if (!Present)
        {
            Collidable = false;
            DisableStaticMovers();
        }
    }

    protected void AddCenterSymbol(Image solid, Image pressed)
    {
        blockData.Get<List<Image>>("solid").Add(solid);
        blockData.Get<List<Image>>("pressed").Add(pressed);
        List<Image> all = blockData.Get<List<Image>>("all");
        Vector2 origin = blockData.Get<Vector2>("groupOrigin") - Position;
        Vector2 size = new(Width, Height);

        Vector2 half = (size - new Vector2(solid.Width, solid.Height)) * 0.5f;
        solid.Origin = origin - half;
        solid.Position = origin;
        solid.Color = color;
        Add(solid);
        all.Add(solid);

        half = (size - new Vector2(pressed.Width, pressed.Height)) * 0.5f;
        pressed.Origin = origin - half;
        pressed.Position = origin;
        pressed.Color = color;
        Add(pressed);
        all.Add(pressed);
    }

    public void HandleShiftSize(int amount)
    {
        if (dynamicHitbox)
        {
            Collider = hitboxes[blockHeight - amount];
        }
    }

    public virtual void HandleUpdateVisualState()
    {
        blockData.Get<Entity>("side").Visible &= Visible;
        foreach (StaticMover staticMover in staticMovers)
        {
            staticMover.Visible = Visible;
        }
    }

    /// <summary>
    /// Makes static movers (Spikes and Springs) appear or disappear when they are disabled.
    /// </summary>
    /// <param name="visible">Whether disabled static movers should be visible.</param>
    protected void SetDisabledStaticMoversVisibility(bool visible)
    {
        foreach (StaticMover staticMover in staticMovers)
        {
            if (staticMover.Entity is Spikes spikes)
                spikes.VisibleWhenDisabled = visible;

            if (staticMover.Entity is Spring spring)
                spring.VisibleWhenDisabled = visible;
        }
    }

    protected void SetStaticMoversVisible(bool visible)
    {
        foreach (StaticMover staticMover in staticMovers)
        {
            staticMover.Entity.Visible = visible;
        }
    }

    #region Hooks

    private static bool createdCassetteManager = false;

    internal static void Hook()
    {
        On.Celeste.CassetteBlock.ShiftSize += CassetteBlock_ShiftSize;
        On.Celeste.CassetteBlock.UpdateVisualState += CassetteBlock_UpdateVisualState;
        IL.Celeste.CassetteBlock.Update += CassetteBlock_Update;
        On.Celeste.Level.LoadLevel += Level_LoadLevel;
        Everest.Events.Level.OnLoadEntity += Level_OnLoadEntity;

        // Fix static movers getting enabled by Platform.EnableStaticMovers when CustomCassetteBlock is not visible.
        On.Celeste.Platform.EnableStaticMovers += Platform_EnableStaticMovers;
    }

    internal static void Unhook()
    {
        On.Celeste.CassetteBlock.ShiftSize -= CassetteBlock_ShiftSize;
        On.Celeste.CassetteBlock.UpdateVisualState -= CassetteBlock_UpdateVisualState;
        IL.Celeste.CassetteBlock.Update -= CassetteBlock_Update;
        On.Celeste.Level.LoadLevel -= Level_LoadLevel;
        Everest.Events.Level.OnLoadEntity -= Level_OnLoadEntity;

        On.Celeste.Platform.EnableStaticMovers -= Platform_EnableStaticMovers;
    }

    private static void Platform_EnableStaticMovers(On.Celeste.Platform.orig_EnableStaticMovers orig, Platform self)
    {
        if (self is CustomCassetteBlock && !self.Visible)
            return; // do nothing
        orig(self);
    }

    private static void CassetteBlock_ShiftSize(On.Celeste.CassetteBlock.orig_ShiftSize orig, CassetteBlock block, int amount)
    {
        bool shift = true;
        if (block is CustomCassetteBlock cassetteBlock)
        {
            if (block.Activated && block.CollideCheck<Player>())
            {
                amount *= -1;
            }
            int newBlockHeight = cassetteBlock.blockHeight - amount;
            if (newBlockHeight is > 2 or < 0)
            {
                shift = false;
            }
            else
            {
                cassetteBlock.HandleShiftSize(amount);
            }
        }

        if (shift)
            orig(block, amount);
    }

    private static void CassetteBlock_UpdateVisualState(On.Celeste.CassetteBlock.orig_UpdateVisualState orig, CassetteBlock block)
    {
        orig(block);
        (block as CustomCassetteBlock)?.HandleUpdateVisualState();
    }

    private static void CassetteBlock_Update(ILContext il)
    {
        ILCursor cursor = new(il);
        Util.Log("Emitting instructions after `ldfld CassetteBlock.group` in `CassetteBlock.Update` to remove blocks from `group` if their Scene is `null`");
        cursor.GotoNext(MoveType.After, instr => instr.MatchLdfld<CassetteBlock>("group"));
        cursor.EmitDelegate<Func<List<CassetteBlock>, List<CassetteBlock>>>(group =>
        {
            group.RemoveAll(block => block.Scene is null); // Assume that the block has been removed from the scene.
            return group;
        });
    }

    private static void Level_LoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level level, Player.IntroTypes introType, bool isFromLoader = false)
    {
        createdCassetteManager = false;
        orig(level, introType, isFromLoader);
    }

    private static readonly MethodInfo m_Level_get_ShouldCreateCassetteManager = typeof(Level).GetProperty("ShouldCreateCassetteManager", BindingFlags.NonPublic | BindingFlags.Instance).GetGetMethod(true);

    private static bool Level_OnLoadEntity(Level level, LevelData levelData, Vector2 offset, EntityData entityData)
    {
        if (CustomCassetteBlockNames.Contains(entityData.Name))
        {
            level.HasCassetteBlocks = true;
            if (level.CassetteBlockTempo == 1f)
            {
                level.CassetteBlockTempo = entityData.Float("tempo", 1f);
            }
            level.CassetteBlockBeats = Math.Max(entityData.Int("index", 0) + 1, level.CassetteBlockBeats);

            if (!createdCassetteManager)
            {
                createdCassetteManager = true;
                if (level.Tracker.GetEntity<CassetteBlockManager>() == null && (bool) m_Level_get_ShouldCreateCassetteManager.Invoke(level, null))
                {
                    if (!level.Entities.ToAdd.Any(e => e is CassetteBlockManager))
                    {
                        level.Entities.ForceAdd(new CassetteBlockManager());
                    }
                }
            }
        }
        return false; // Let the CustomEntity attribute handle actually adding the entities
    }

    #endregion

}
