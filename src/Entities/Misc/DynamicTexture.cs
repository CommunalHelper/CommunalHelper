using Microsoft.Xna.Framework;
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

        public void AddSprite(Sprite sprite, Vector2 offset)
            => textures.Add(new TextureData() {
                Sprite = sprite,
                Offset = offset
            });

        public void Render(Vector2 at, Vector2 offset, Matrix transform, Rectangle clipRect) {
            Matrix invertedTransform = Matrix.Invert(transform);

            Vector2 vec = Calc.Round(Calc.Abs(Vector2.Transform(new Vector2(clipRect.Width, clipRect.Height), invertedTransform)));
            Vector2 identityOffset = Calc.Round(Vector2.Transform(-offset, invertedTransform));
            Rectangle identityClipRect = new Rectangle((int) (clipRect.X + identityOffset.X), (int)(clipRect.Y + identityOffset.Y), (int) vec.X, (int) vec.Y);

            foreach (TextureData data in textures) {
                MTexture tex = data.IsSprite ? data.Sprite.GetFrame(data.Sprite.CurrentAnimationID, data.Sprite.CurrentAnimationFrame) : data.Texture;
                Vector2 centeredOffset = data.Centered ? new Vector2(tex.Width, tex.Height) / -2 : Vector2.Zero;
                Vector2 position = at + data.Offset + centeredOffset;
                Rectangle rect = new Rectangle((int)position.X, (int)position.Y, tex.Width, tex.Height);
                if (!identityClipRect.Intersects(rect))
                    continue;

                // texture is somewhat visible.
                rect = rect.ClampTo(identityClipRect); // fun fact, i found this ClampTo extension right after writing the code for it, and it was the same.
                rect.X -= (int) position.X;
                rect.Y -= (int) position.Y;
                Vector2 transformedOffset = Vector2.Transform(data.Offset + centeredOffset + new Vector2(rect.X, rect.Y) , transform);
                float rotation = Vector2.Transform(Vector2.UnitX, transform).Angle();

                MTexture mTexture = tex.GetSubtexture(rect);
                mTexture.Draw(at + transformedOffset + offset, Vector2.Zero, data.Color, 1f, rotation);
            }
        }
    }
}
