﻿using System;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper {
    public class CommunalHelperSaveData : EverestModuleSaveData {
        public SortedSet<string> SummitGems { get; set; }

        public void RegisterSummitGem(string id) {
            if (SummitGems == null)
                SummitGems = new SortedSet<string>(StringComparer.InvariantCulture);
            SummitGems.Add(id);
        }
    }
}
