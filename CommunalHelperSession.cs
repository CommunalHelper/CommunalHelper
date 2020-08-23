using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper {
    public class CommunalHelperSession : EverestModuleSession {
        public List<string> SummitGems { get; set; }

        public CommunalHelperSession() {
            SummitGems = new List<string>();
        }

    }
}
