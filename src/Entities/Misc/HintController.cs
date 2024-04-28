using MonoMod.Cil;
using System.Collections;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/HintController")]
public class HintController : Entity
{
    public string TitleDialog { get; private set; }
    public string[] DialogIds { get; }
    public bool[] SingleUses { get; }
    public string SelectorCounter { get; }
    public bool SelectNextHint { get; }

    private int CurrentSelector
    {
        get => string.IsNullOrWhiteSpace(SelectorCounter) ? 0 : (Engine.Scene as Level)?.Session.GetCounter(SelectorCounter) ?? 0;
        set
        {
            if (!string.IsNullOrWhiteSpace(SelectorCounter))
            {
                (Engine.Scene as Level)?.Session.SetCounter(SelectorCounter, value);
            }
        }
    }

    private string CurrentDialogId => DialogIds.ElementAtOrDefault(CurrentSelector) ?? string.Empty;
    private bool CurrentSingleUse => SingleUses.ElementAtOrDefault(CurrentSelector);
    private string CurrentUsedFlag => $"CommunalHelper/HintController:Used:{(Engine.Scene as Level)?.Session.Level ?? string.Empty}:{CurrentSelector}";

    private bool CurrentIsUsed
    {
        get => !string.IsNullOrWhiteSpace(CurrentUsedFlag) && ((Engine.Scene as Level)?.Session.GetFlag(CurrentUsedFlag) ?? false);
        set
        {
            if (!string.IsNullOrWhiteSpace(CurrentUsedFlag))
            {
                (Engine.Scene as Level)?.Session.SetFlag(CurrentUsedFlag, value);
            }
        }
    }

    private static bool showingHint;

    private static readonly char[] CommaSeparator = { ',' };

    public HintController(EntityData data, Vector2 offset)
        : base(data.Position + offset)
    {
        TitleDialog = data.Attr("titleDialog", "");
        if (string.IsNullOrWhiteSpace(TitleDialog)) TitleDialog = "communalhelper_entities_hint_controller_menu";

        DialogIds = data.Attr("dialogIds")
            .Split(CommaSeparator, StringSplitOptions.RemoveEmptyEntries)
            .ToArray();

        // comma separated list of ints where 0 means multiple use and anything else means single use: "0,1,0"
        SingleUses = data.Attr("singleUses")
            .Split(CommaSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => !int.TryParse(x, out var value) || value != 0)
            .ToArray();

        SelectorCounter = data.Attr("selectorCounter");
        SelectNextHint = data.Bool("selectNextHint");

        Tag = Tags.PauseUpdate;
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);

        if (SelectNextHint && !string.IsNullOrWhiteSpace(SelectorCounter))
        {
            CurrentSelector = 0;
        }
    }

    internal static void Load()
    {
        Everest.Events.Level.OnCreatePauseMenuButtons += Level_OnCreatePauseMenuButtons;
        IL.Celeste.Textbox.Render += Textbox_Render_Update;
        IL.Celeste.Textbox.Update += Textbox_Render_Update;
    }

    internal static void Unload()
    {
        Everest.Events.Level.OnCreatePauseMenuButtons -= Level_OnCreatePauseMenuButtons;
        IL.Celeste.Textbox.Render -= Textbox_Render_Update;
        IL.Celeste.Textbox.Update -= Textbox_Render_Update;
    }

    private static void Textbox_Render_Update(ILContext il)
    {
        var cursor = new ILCursor(il);
        if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<Level>("get_FrozenOrPaused")))
        {
            // force update and render if showing hint
            cursor.EmitDelegate<Func<bool, bool>>(fop => !showingHint && fop);
        }
        else
        {
            Util.Log("Failed to add IL hook!");
        }
    }

    private static void Level_OnCreatePauseMenuButtons(Level level, TextMenu menu, bool minimal)
    {
        int retryIndex = menu.Items.FindIndex(item =>
            item.GetType() == typeof(TextMenu.Button) && ((TextMenu.Button) item).Label == Dialog.Clean("menu_pause_retry"));

        if (retryIndex < 0)
        {
            return;
        }

        var hintControllers = level.Entities.FindAll<HintController>();
        int index = retryIndex;
        if (hintControllers != null && hintControllers.Count > 0)
        {
            foreach (HintController hintController in hintControllers)
            {
                menu.Insert(++index, new TextMenu.Button(Dialog.Clean(hintController.TitleDialog))
                {
                    OnPressed = () =>
                    {
                        menu.OnCancel();
                        hintController.ShowHint();
                    },
                    Disabled = hintController.CurrentSingleUse && hintController.CurrentIsUsed,
                });
            }
        }
    }

    private void ShowHint()
    {
        if (Scene is Level level)
        {
            level.Paused = true;
            CurrentIsUsed = true;
        }

        Add(new Coroutine(ShowHintSequence()));
    }

    private IEnumerator ShowHintSequence()
    {
        showingHint = true;
        yield return Textbox.Say(CurrentDialogId);
        showingHint = false;

        if (SelectNextHint && DialogIds.Length > 0 && !string.IsNullOrWhiteSpace(SelectorCounter))
        {
            CurrentSelector = (CurrentSelector + 1) % DialogIds.Length;
        }

        if (Scene is Level level)
        {
            level.Paused = false;
        }
    }
}
