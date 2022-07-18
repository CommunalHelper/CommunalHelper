using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Entities {

    // Originally I made this as a standalone entity for someone's map they were working on, but to make this fully work with DreamTunnelDash I moved it to CommunalHelper
    // I gave them a plugin for the old version when i finished and I'd like to keep some compatability to the old version so they dont have to redo their berries using it
    [CustomEntity("CommunalHelper/DreamStrawberry", "DreamDashListener/DreamDashBerry")]
    public class DreamStrawberry : Strawberry {

        /*
         * Hey! snowii here
         * 
         * If anyone is reading this and wants to touch my unholy code AND has a sprite for this, please add it im begging
         * 
         * Hope someone finds this super niche excuse for an entity useful
         */

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

            // I'd love to say I wrote this, but this is 100% stolen from SC2020 Source
            // Whoever wrote this piece of code so I do not have to, you're my bestie

            //OriginalOnDash = typeof(Strawberry).GetMethod("OnDash", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod);

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

        #region Bad Code
        /*
        public override void Added(Scene scene) {
            sprite.OnFrameChange = OnAnimate;
            Add(wiggler = Wiggler.Create(0.4f, 4f, delegate (float v)
            {
                sprite.Scale = Vector2.One * (1f + v * 0.35f);
            }));
            Add(rotateWiggler = Wiggler.Create(0.5f, 4f, delegate (float v)
            {
                sprite.Rotation = v * 30f * ((float) Math.PI / 180f);
            }));
            Add(bloom = new BloomPoint(1f, 12f));
            Add(light = new VertexLight(Color.White, 1f, 16, 24));
            Add(lightTween = light.CreatePulseTween());
            if ((scene as Level).Session.BloomBaseAdd > 0.1f) {
                bloom.Alpha *= 0.5f;
            }
        }

        public void OnAnimate(string id) {
            if (!flyingAway && id == "flap" && sprite.CurrentAnimationFrame % 9 == 4) {
                Audio.Play("event:/game/general/strawberry_wingflap", Position);
                flapSpeed = -50f;
            }
            int num = 25;
            // Checks if the animation from is 25, runs the funny strawberry noises
            if (sprite.CurrentAnimationFrame == num) {
                lightTween.Start();
                if (!collected && (CollideCheck<FakeWall>() || CollideCheck<Solid>())) {
                    Audio.Play("event:/game/general/strawberry_pulse", Position);
                    SceneAs<Level>().Displacement.AddBurst(Position, 0.6f, 4f, 28f, 0.1f);
                } else {
                    Audio.Play("event:/game/general/strawberry_pulse", Position);
                    SceneAs<Level>().Displacement.AddBurst(Position, 0.6f, 4f, 28f, 0.2f);
                }
            }
        }
        */
        #endregion

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
