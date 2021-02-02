using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Entities {
    class DynamicTexture {

        private class TextureData {
            public MTexture Texture;
            public Vector2 Offset = Vector2.Zero;
            public Color Color = Color.White;
            public bool Centered = false;
            public float Rotation = 0f;
            public Vector2 Scale = Vector2.One;

            // sprite
            public Sprite Sprite;
            public bool IsSprite => Sprite != null;
        }

        private List<TextureData> textures = new List<TextureData>();

        public void AddTexture(MTexture texture, Vector2 offset, Color color, bool drawCentered = false)
            => textures.Add(new TextureData() {
                Texture = texture,
                Offset = offset,
                Color = color,
                Centered = drawCentered
            });

        public void AddTexture(MTexture texture, Vector2 offset, Color color, Vector2 scale, float rotation = 0f, bool drawCentered = false)
            => textures.Add(new TextureData() {
                Texture = texture,
                Offset = offset,
                Color = color,
                Centered = drawCentered,
                Scale = scale,
                Rotation = rotation
            });

        public void AddLine(Vector2 start, Vector2 end, Color color) 
            => AddTexture(
                Draw.Pixel, 
                start, 
                color, 
                new Vector2(Vector2.Distance(start, end), 1),
                Calc.Angle(start, end));

        public void AddSprite(Sprite sprite, Vector2 offset)
            => textures.Add(new TextureData() {
                Sprite = sprite,
                Offset = offset + sprite.Position
            });

        public void Render(Vector2 at, Vector2 offset, Matrix transform, Rectangle clipRect) {
            // Create our own render target
            //renderTarget = VirtualContent.CreateRenderTarget("dynamic-texture-renderer", clipRect.Width, clipRect.Height);

            // Switch to our render target
            //GameplayRenderer.End();
            //Engine.Instance.GraphicsDevice.SetRenderTarget(renderTarget);
            //Engine.Instance.GraphicsDevice.Clear(Color.Transparent);

            //Draw.SpriteBatch.Begin();

            foreach (TextureData data in textures) {
                // Get the current texture that should be drawn.
                MTexture tex = data.IsSprite ? data.Sprite.GetFrame(data.Sprite.CurrentAnimationID, data.Sprite.CurrentAnimationFrame) : data.Texture;

                Vector2 transformedOffset = Vector2.Transform(data.Offset, transform);
                float rotation = Vector2.Transform(Vector2.UnitX, transform).Angle();
                if (data.Centered) {
                    tex.DrawCentered(at + transformedOffset + offset, data.Color, data.Scale, rotation + data.Rotation);
                } else {
                    tex.Draw(at + transformedOffset + offset, Vector2.Zero, data.Color, data.Scale, rotation + data.Rotation);
                }
            }

            //Draw.SpriteBatch.Draw(renderTarget, clipRect, Color.White);
            //Draw.SpriteBatch.End();
            
            // Switch back to rendering gameplay.
            //Engine.Instance.GraphicsDevice.SetRenderTarget(GameplayBuffers.Gameplay);
            //GameplayRenderer.Begin();
        }
    }
}
