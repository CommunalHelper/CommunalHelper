using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.CommunalHelper {
    public class CommunalHelperSettings : EverestModuleSettings {
        [SettingName("Settings_DreamTunnel_AlwaysActive")]
        public bool AlwaysActiveDreamRefillCharge { get; set; }
        public bool DreamTunnelIgnoresCollidables { get; set; }
        public bool DreamDashFeatherMode { get; set; }

        [SettingName("Settings_SeekerDash_AlwaysActive")]
        public bool AlwaysActiveSeekerDash { get; set; }

        [DefaultButtonBinding(Buttons.RightShoulder, Keys.Z)]
        public ButtonBinding ActivateSyncedZipMovers { get; set; }
        public bool AllowActivateRebinding { get; set; }

        [DefaultButtonBinding(Buttons.RightShoulder, Keys.Z)]
        public ButtonBinding CycleCassetteBlocks { get; set; }

        [DefaultButtonBinding(Buttons.RightShoulder, Keys.Z)]
        public ButtonBinding ActivateFlagController { get; set; }
    }
}
