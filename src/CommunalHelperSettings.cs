using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.CommunalHelper;

public class CommunalHelperSettings : EverestModuleSettings
{
    // If saving settings, always return true;
    private bool SaveOverride(bool val)
    {
        return CommunalHelperModule.SavingSettings || val;
    }

    #region AlwaysActiveDreamRefillCharge

    public bool AlwaysActiveDreamRefillCharge { get; set; }

    private bool _dreamDashFeatherMode;
    [SettingIgnore]
    public bool DreamDashFeatherMode
    {
        get => SaveOverride(AlwaysActiveDreamRefillCharge) && _dreamDashFeatherMode;
        set => _dreamDashFeatherMode = value;
    }

    private bool _dreamTunnelIgnoreCollidables;
    [SettingIgnore]
    public bool DreamTunnelIgnoreCollidables
    {
        get => SaveOverride(AlwaysActiveDreamRefillCharge) && _dreamTunnelIgnoreCollidables;
        set => _dreamTunnelIgnoreCollidables = value;
    }

    public void CreateAlwaysActiveDreamRefillChargeEntry(TextMenu menu, bool inGame)
    {
        var featherModeOption = new TextMenu.OnOff(Dialog.Clean("SETTINGS_DREAMTUNNEL_FEATHERMODE"), _dreamDashFeatherMode)
            .Change(v => _dreamDashFeatherMode = v);

        var ignoreCollectiblesOption = new TextMenu.OnOff(Dialog.Clean("SETTINGS_DREAMTUNNEL_IGNORECOLLECTIBLES"), _dreamTunnelIgnoreCollidables)
            .Change(v => _dreamTunnelIgnoreCollidables = v);

        TextMenuExt.OptionSubMenu subMenu = new TextMenuExt.OptionSubMenu(Dialog.Clean("SETTINGS_DREAMTUNNEL_ALWAYSACTIVE"))
            .Add(Dialog.Clean("OPTIONS_OFF"))
            .Add(Dialog.Clean("OPTIONS_ON"), featherModeOption, ignoreCollectiblesOption)
            .Change(i => AlwaysActiveDreamRefillCharge = i == 1)
            .SetInitialSelection(Util.ToInt(AlwaysActiveDreamRefillCharge));

        menu.Add(subMenu);
    }

    #endregion

    [SettingName("Settings_SeekerDash_AlwaysActive")]
    public bool AlwaysActiveSeekerDash { get; set; }

    [DefaultButtonBinding(Buttons.RightShoulder, Keys.Z)]
    public ButtonBinding ActivateSyncedZipMovers { get; set; }
    public bool AllowActivateRebinding { get; set; }

    [DefaultButtonBinding(Buttons.RightShoulder, Keys.Z)]
    public ButtonBinding CycleCassetteBlocks { get; set; }

    [DefaultButtonBinding(Buttons.RightShoulder, Keys.Z)]
    public ButtonBinding ActivateFlagController { get; set; }

    [DefaultButtonBinding(Buttons.LeftShoulder, Keys.W)]
    public ButtonBinding DeployElytra { get; set; }

    public bool RequireGrabToPush { get; set; } = true;
}
