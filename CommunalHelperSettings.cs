using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.CommunalHelper {
    public class CommunalHelperSettings : EverestModuleSettings {
        public bool AlwaysActiveDreamRefillCharge { get; set; }

        [DefaultButtonBinding(Buttons.RightShoulder, Keys.Z)]
        public ButtonBinding ActivateSyncedZipMovers { get; set; }
        public bool AllowActivateRebinding { get; set; }
    }
}
