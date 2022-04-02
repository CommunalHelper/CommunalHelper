using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Entities {
    [Tracked]
    public class BeepBlockRenderer : Entity {
        private readonly List<BeepBlock> blocks = new List<BeepBlock>();

        private readonly MTexture mist;
        private VirtualRenderTarget renderTarget;

        public BeepBlockRenderer() {
            Tag = Tags.Global | Tags.TransitionUpdate;
            renderTarget = VirtualContent.CreateRenderTarget("communalhelper-beepblockrenderer", 320, 180);
            mist = GFX.Misc["mist"];
            Depth = 0;
        }

        public void Track(BeepBlock block) {
            blocks.Add(block);
        }

        public void Untrack(BeepBlock block) {
            blocks.Remove(block);
        }

        public override void Removed(Scene scene) {
            Dispose();
            base.Removed(scene);
        }

        public override void SceneEnd(Scene scene) {
            Dispose();
            base.SceneEnd(scene);
        }

        private void Dispose() {
            if (renderTarget != null)
                renderTarget.Dispose();
            renderTarget = null;
        }

        public override void Render() {
            Vector2 camera = SceneAs<Level>().Camera.Position;

            GameplayRenderer.End();
            Engine.Graphics.GraphicsDevice.SetRenderTarget(renderTarget);
            Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, ColorGrade.Effect);

            float n = 10;
            for (int i = 1; i < n; i++) {
                DrawLayer(camera, Calc.AngleToVector(i, i * Scene.TimeActive * 15), i / n, 0.3f + i / (2 * n));
            }

            Draw.SpriteBatch.End();
            Engine.Graphics.GraphicsDevice.SetRenderTarget(GameplayBuffers.Gameplay);
            GameplayRenderer.Begin();

            foreach (BeepBlock block in Scene.Tracker.GetEntities<BeepBlock>()) {
                Draw.Rect(block.Position, block.Width, block.Height, block.Color);
                Vector2 screenPos = Calc.Floor(block.Position - camera);
                Rectangle cropRect = new Rectangle((int) screenPos.X, (int) screenPos.Y, (int) block.Width, (int) block.Height);
                Draw.SpriteBatch.Draw(renderTarget, block.Position, cropRect, Color.White * 1);
            }
        }

        private void DrawLayer(Vector2 camera, Vector2 offset, float parallax, float scale) {
            float w = mist.Width * scale, h = mist.Height * scale;
            Vector2 pos = camera * -parallax + offset;

            Vector2 s = new(w, h);
            pos -= s * (Calc.Floor(pos / s) + Vector2.One);

            Vector2 start = pos;
            do {
                do {
                    mist.Draw(pos, Vector2.Zero, Color.Black, scale);
                    pos.Y += h;
                } while (pos.Y < 180);
                pos.X += w;
                pos.Y = start.Y;
            } while (pos.X < 360);
        }

        #region Hooks

        internal static void Load() {
            On.Celeste.LevelLoader.LoadingThread += LevelLoader_LoadingThread;
        }

        internal static void Unload() {
            On.Celeste.LevelLoader.LoadingThread -= LevelLoader_LoadingThread;
        }

        private static void LevelLoader_LoadingThread(On.Celeste.LevelLoader.orig_LoadingThread orig, LevelLoader self) {
            orig(self);
            self.Level.Add(new BeepBlockRenderer());
        }

        #endregion
    }
}
