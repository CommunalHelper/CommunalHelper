using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod;
using MonoMod.Utils;
using System;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Entities {

    // Originally I made this as a standalone entity for someone's map they were working on, but to make this fully work with DreamTunnelDash I moved it to CommunalHelper
    // I gave them a plugin for the old version when i finished and I'd like to keep some compatability to the old version so they dont have to redo their berries using it
    [CustomEntity("CommunalHelper/DreamStrawberry", "DreamDashListener/DreamDashBerry")]
    public class DreamStrawberry : Strawberry {
        // Original OnDash method from Celeste.Strawberry
        private static readonly MethodInfo m_Strawberry_OnDash = typeof(Strawberry).GetMethod("OnDash", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod);

        public static Color[] DreamTrailColors = new Color[] {
            Calc.HexToColor("FFEF11"),
            Calc.HexToColor("08A310"),
            Calc.HexToColor("FF00D0"),
            Calc.HexToColor("5FCDE4"),
            Calc.HexToColor("E0564C")
        };

        public DynamicData dreamStrawberryData;

        public static int DreamTrailColorIndex = 0;

        public DreamStrawberry(EntityData data, Vector2 offset, EntityID id) : base(FixData(data), offset, id) {
            // Removes any default DashListeners from the strawberry as we do not want to use those
            foreach (Component comp in Components.ToArray()) {
                if (comp is DashListener)
                    Components.Remove(comp);
            }

            // To account for the DashListeners I just brutally murdered, we add a DreamDashListener instead
            // as we want it to activate from dream blocks and not normal player dashes
            Add(new DreamDashListener {
                OnDreamDash = new Action<Vector2>(OnDreamBerryDash)
            });

            dreamStrawberryData = DynamicData.For(this);
        }

        public override void Update() {
            base.Update();

            // Creates and updates the dream trail
            if (Scene.OnInterval(0.1f))
                CreateDreamTrail();
        }

        // Calls the original OnDash from our DreamDashListener
        public void OnDreamBerryDash(Vector2 dir) {
            m_Strawberry_OnDash.Invoke(this, new object[] { dir });
        }

        // Code to create a trail for the berry to make it separate from normal berries
        public void CreateDreamTrail() {
            Sprite berrySprite = dreamStrawberryData.Get<Sprite>("sprite");
            Vector2 scale = new Vector2(Math.Abs(berrySprite.Scale.X), berrySprite.Scale.Y);
            TrailManager.Add(this, scale, DreamTrailColors[DreamTrailColorIndex]);
            ++DreamTrailColorIndex;
            DreamTrailColorIndex %= 5;
        }

        // Fixes the entity data of the strawberry, specifically setting winged to true so you don't have to.
        private static EntityData FixData(EntityData data) {
            data.Values["winged"] = true;
            return data;
        }
    }
}
