using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Entities;

public class ManualCassetteController : AbstractInputController
{
    private readonly int startIndex;

    private int roomBeats;
    private int currentIndex;
    private int heldIndex;

    private const string blueFlag = "CH_cas_blue";
    private const string pinkFlag = "CH_cas_rose";
    private const string yellowFlag = "CH_cas_brightsun";
    private const string greenFlag = "CH_cas_malachite";
    private const string heldFlag = "CH_cas_held";


    public ManualCassetteController(EntityData data)
    {
        startIndex = data.Int("startIndex", 0);

        Visible = Collidable = false;
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);
        SetFlag(startIndex);
    }
    public override void Awake(Scene scene)
    {
        base.Awake(scene);

        if (Scene.Tracker.GetEntity<CassetteBlockManager>() is not null)
            throw new Exception("CassetteBlockManager detected in same room as ManualCassetteController");

        roomBeats = SceneAs<Level>().CassetteBlockBeats;

        if (!(startIndex >= 0 && startIndex < roomBeats))
            throw new IndexOutOfRangeException("ManualCassetteController startIndex is outside of the number of CassetteBlock indices present");
        currentIndex = startIndex;

        SetActiveIndex(currentIndex, true);
        SetFlag(currentIndex);
        SetHeldIndex(currentIndex, true);
        SetHeldFlag(currentIndex);
    }

    public override void Update()
    {
        base.Update();
        if (CommunalHelperModule.Settings.CycleCassetteBlocks.Pressed)
            Tick();
        if (CommunalHelperModule.Settings.CycleCassetteBlocks.Check != Convert.ToBoolean(heldIndex))
            HeldTick();
    }

    public override void FrozenUpdate()
    {
        if (CommunalHelperModule.Settings.CycleCassetteBlocks.Pressed)
            Tick();
        if (CommunalHelperModule.Settings.CycleCassetteBlocks.Check != Convert.ToBoolean(heldIndex))
            HeldTick();
    }

    public void Tick()
    {
        currentIndex++;
        currentIndex %= roomBeats;
        SetFlag(currentIndex);
        SetActiveIndex(currentIndex);
        Audio.Play("event:/game/general/cassette_block_switch_" + ((currentIndex % 2) + 1));
        Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
    }

    public void HeldTick()
    {
        heldIndex = 1- heldIndex;
        SetHeldFlag(heldIndex);
        SetHeldIndex(heldIndex);
/*      Audio.Play("event:/game/general/cassette_block_switch_" + ((heldIndex % 2) + 1));
        Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);*/ // I don't want the audio to play twice
    }

    public void SetFlag(int index)
    {
        Session session = SceneAs<Level>().Session;
        session.SetFlag(blueFlag, index == 0);
        session.SetFlag(pinkFlag, index == 1);
        session.SetFlag(yellowFlag, index == 2);
        session.SetFlag(greenFlag, index == 3);
    }

    public void SetHeldFlag(int index)
    {
        Session session = SceneAs<Level>().Session;
        session.SetFlag(heldFlag, index == 1);
    }

    public void SetActiveIndex(int index, bool silent = false)
    {
        foreach (CassetteBlock entity in Scene.Tracker.GetEntities<CassetteBlock>().Cast<CassetteBlock>())
        {
            if (!(entity is CustomCassetteBlock cassette && cassette.Held))
            {
                entity.Activated = entity.Index == index;
                bool activated = entity.Index == index;
                if (silent)
                    entity.SetActivatedSilently(activated);
                else
                    entity.Activated = activated;
            }
        }
    }

    public void SetHeldIndex(int index, bool silent = false)
    {
        foreach (CassetteBlock entity in Scene.Tracker.GetEntities<CassetteBlock>().Cast<CassetteBlock>())
        {
            if (entity is CustomCassetteBlock cassette && cassette.Held)
            {
                entity.Activated = entity.Index == index;
                bool activated = entity.Index == index;
                if (silent)
                    entity.SetActivatedSilently(activated);
                else
                    entity.Activated = activated;
            }            
        }
    }

    private static ILHook hook_Level_orig_LoadLevel;
    private static ILHook hook_TransitionListener_OnOutBegin_Closure;

    internal static new void Load()
    {
        MethodInfo m_TransitionListener_Closure = typeof(CassetteBlockManager).GetMethod("<.ctor>b__10_0", BindingFlags.NonPublic | BindingFlags.Instance);

        hook_TransitionListener_OnOutBegin_Closure = new ILHook(m_TransitionListener_Closure, TransitionListener_OnOutBegin_Closure);
        hook_Level_orig_LoadLevel = new ILHook(typeof(Level).GetMethod("orig_LoadLevel"), Level_orig_LoadLevel);
    }

    internal static new void Unload()
    {
        hook_TransitionListener_OnOutBegin_Closure.Dispose();
        hook_Level_orig_LoadLevel.Dispose();
    }

    private static void Level_orig_LoadLevel(ILContext il)
    {
        /*
        if (HasCassetteBlocks && ShouldCreateCassetteManager)
            // We're just after the `base`
            base.Tracker.GetEntity<CassetteBlockManager>()?.OnLevelStart();
        */

        ILCursor cursor = new(il);
        cursor.GotoNext(instr => instr.MatchCallvirt<CassetteBlockManager>("OnLevelStart"));
        cursor.GotoPrev(instr => instr.MatchCall<Scene>("get_Tracker"));

        // Just gonna borrow the level object for a bit
        cursor.EmitDelegate<Func<Level, Level>>(level =>
        {
            // This could be checked for as part of `Everest.Events.Level.OnLoadEntity` but meh
            EntityData data = level.Session.LevelData.Entities.FirstOrDefault(entityData => entityData.Name == "CommunalHelper/ManualCassetteController");
            if (data is not null)
            {
                level.Tracker.GetEntity<CassetteBlockManager>()?.RemoveSelf();
                level.Add(new ManualCassetteController(data));
                // Lists were just updated so there's no harm in doing it again (hopefully)
                level.Entities.UpdateLists();
            }
            return level;
        });
    }

    private static void TransitionListener_OnOutBegin_Closure(ILContext il)
    {
        // we need to add a null check over Scene before doing anything
        ILCursor cursor = new(il);

        var m_getScene = typeof(Entity).GetProperty("Scene").GetGetMethod(true);

        ILLabel afterReturnLabel = cursor.DefineLabel();

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Callvirt, m_getScene);
        cursor.Emit(OpCodes.Brtrue, afterReturnLabel);
        cursor.Emit(OpCodes.Ret);

        cursor.MarkLabel(afterReturnLabel);
    }
}
