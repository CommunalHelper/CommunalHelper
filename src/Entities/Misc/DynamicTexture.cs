using FMOD;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Entities {
    class DynamicTexture {

        private class TextureData {
            public MTexture Texture;
            public Vector2 Offset;
        }

        private List<TextureData> textures = new List<TextureData>();

        public void AddTexture(MTexture texture, Vector2 offset) 
            => textures.Add(new TextureData()  { 
                Texture = texture, Offset = offset 
            });

        public void Render(Vector2 at, Matrix transform) {
            foreach (TextureData data in textures) {
                Vector2 offset = Vector2.Transform(data.Offset, transform);
                float rotation = Vector2.Transform(Vector2.UnitX, transform).Angle();
                data.Texture.Draw(at + offset, Vector2.Zero, Color.White, 1f, rotation);
            }

        }

        public void Render(Vector2 at, Matrix transform, Rectangle clipRect) {

            Vector2 vec = Calc.Round(Calc.Abs(Vector2.Transform(new Vector2(clipRect.Width, clipRect.Height), Matrix.Invert(transform))));
            Rectangle identityClipRect = new Rectangle(clipRect.X, clipRect.Y, (int) vec.X, (int) vec.Y);
            foreach (TextureData data in textures) {
                Vector2 position = at + data.Offset;
                Rectangle rect = new Rectangle((int)position.X, (int)position.Y, data.Texture.Width, data.Texture.Height);
                rect = rect.ClampTo(identityClipRect); // fun fact, i found this ClampTo extension right after writing the code for it, and it was the same.
                rect.X -= (int) position.X;
                rect.Y -= (int) position.Y;
                Vector2 offset = Vector2.Transform(data.Offset + new Vector2(rect.X, rect.Y), transform);
                float rotation = Vector2.Transform(Vector2.UnitX, transform).Angle();
                data.Texture.GetSubtexture(rect).Draw(at + offset, Vector2.Zero, Color.White, 1f, rotation);
            }
        }
    }
}
