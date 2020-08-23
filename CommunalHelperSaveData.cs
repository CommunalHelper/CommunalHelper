using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper {
    public class CommunalHelperSaveData : EverestModuleSaveData {
        public List<string> SummitGems { get; set; }

        public void RegisterSummitGem(string id) {
            if (SummitGems == null)
                SummitGems = new List<string>();
            SummitGems.Add(id);
        }
    }
}
