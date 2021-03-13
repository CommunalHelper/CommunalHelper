using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;

namespace Celeste.Mod.CommunalHelper.Entities {
    public abstract class CustomBooster : Booster {

        protected DynData<Booster> BoosterData;

        public CustomBooster(Vector2 position, bool redBoost)
            : base(position, redBoost) {
            BoosterData = new DynData<Booster>(this);
        }

        protected void ReplaceSprite(Sprite newSprite) {
            Sprite oldSprite = BoosterData.Get<Sprite>("sprite");
            Remove(oldSprite);
            BoosterData["sprite"] = newSprite;
            Add(newSprite);
        }

        #region Hooks

        public static void Unhook() {
            DreamBoosterHooks.Unhook();
        }

        public static void Hook() {
            DreamBoosterHooks.Hook();
        }

        #endregion
    }
}
