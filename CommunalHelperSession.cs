using System;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper {
    public class CommunalHelperSession : EverestModuleSession {
        public SortedSet<string> SummitGems { get; set; }

        public CommunalHelperSession() {
            SummitGems = new SortedSet<string>(StringComparer.InvariantCulture);
        }

    }
}
