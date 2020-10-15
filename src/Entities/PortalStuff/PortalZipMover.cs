using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Entities
{
    [CustomEntity("CommunalHelper/PortalZipMover")]
    class PortalZipMover : SlicedSolid {

        private DynamicTexture dynEdges = new DynamicTexture();
        private List<MTexture> innerCogs;
        private float percent = 0;
        private Vector2 target;

        public PortalZipMover(EntityData data, Vector2 offset) 
            : this(data.Position + offset, data.Width, data.Height)
        { }

        public PortalZipMover(Vector2 position, int width, int height)
            : base(position, width, height, safe: false)
        {
            BuildTexture();
            Add(new Coroutine(Sequence()));
            Add(new LightOcclude());
            target = Position + new Vector2(0, -80);
        }

        private IEnumerator Sequence() {
            Vector2 start = Position;
            while (true) {
                if (!HasPlayerRider()) {
                    yield return null;
                    continue;
                }
                Audio.Play("event:/new_content/game/10_farewell/zip_mover", Position);
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
                StartShaking(0.1f);
                yield return 0.1f;
                StopPlayerRunIntoAnimation = false;
                float at2 = 0f;
                while (at2 < 1f) {
                    yield return null;
                    at2 = Calc.Approach(at2, 1f, 2f * Engine.DeltaTime);
                    percent = Ease.SineIn(at2);
                    Vector2 vector = Vector2.Lerp(start, target, percent);
                    MoveTo(vector);
                }
                StartShaking(0.2f);
                Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                SceneAs<Level>().Shake();
                StopPlayerRunIntoAnimation = true;
                yield return 0.5f;
                StopPlayerRunIntoAnimation = false;
                at2 = 0f;
                while (at2 < 1f) {
                    yield return null;
                    at2 = Calc.Approach(at2, 1f, 0.5f * Engine.DeltaTime);
                    percent = 1f - Ease.SineIn(at2);
                    Vector2 position = Vector2.Lerp(target, start, Ease.SineIn(at2));
                    MoveTo(position);
                }
                StopPlayerRunIntoAnimation = true;
                StartShaking(0.2f);
                yield return 0.5f;
            }
        }

        private void BuildTexture() {
            MTexture[,] edges = new MTexture[3, 3];
            innerCogs = GFX.Game.GetAtlasSubtextures("objects/zipmover/moon/innercog");

            for (int i = 0; i < 3; i++) {
                for (int j = 0; j < 3; j++) {
                    edges[i, j] = GFX.Game["objects/zipmover/moon/block"].GetSubtexture(i * 8, j * 8, 8, 8);
                }
            }
            for (int i = 0; i < Width / 8f; i++) {
                for (int j = 0; j < Height / 8f; j++) {
                    int px = (i != 0) ? ((i != Width / 8f - 1f) ? 1 : 2) : 0;
                    int py = (j != 0) ? ((j != Height / 8f - 1f) ? 1 : 2) : 0;
                    if (px != 1 || py != 1) {
                        int x = i * 8, y = j * 8;
                        dynEdges.AddTexture(edges[px, py], new Vector2(x, y), Color.White);
                    }
                }
            }

            MTexture streetlight = GFX.Game["objects/zipmover/moon/light01"];
            dynEdges.AddTexture(streetlight, new Vector2(OriginalWidth / 2f - streetlight.Width / 2f, 0f), Color.White);
        }

        public override void Render() {
            base.Render();

            DynamicTexture dynCogs = new DynamicTexture();
            int num = 1;
            float num2 = 0f;
            int count = innerCogs.Count;
            for (int i = 4; i <= OriginalHeight - 4f; i += 8) {
                int num3 = num;
                for (int j = 4; j <= OriginalWidth - 4f; j += 8) {
                    int index = (int) (mod((num2 + num * percent * (float) Math.PI * 4f) / ((float) Math.PI / 2f), 1f) * count);
                    MTexture mTexture = innerCogs[index];
                    Rectangle rectangle = new Rectangle(0, 0, mTexture.Width, mTexture.Height);
                    Vector2 zero = Vector2.Zero;
                    if (j <= 4) {
                        zero.X = 2f;
                        rectangle.X = 2;
                        rectangle.Width -= 2;
                    } else if (j >= OriginalWidth - 4f) {
                        zero.X = -2f;
                        rectangle.Width -= 2;
                    }
                    if (i <= 4) {
                        zero.Y = 2f;
                        rectangle.Y = 2;
                        rectangle.Height -= 2;
                    } else if (i >= OriginalHeight - 4f) {
                        zero.Y = -2f;
                        rectangle.Height -= 2;
                    }
                    mTexture = mTexture.GetSubtexture(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
                    dynCogs.AddTexture(mTexture, new Vector2(j, i) + zero, Color.White * ((num < 0) ? 0.5f : 1f), true);
                    num = -num;
                    num2 += (float) Math.PI / 3f;
                }
                if (num3 == num) {
                    num = -num;
                }
            }

            Vector2 pos = Position;
            Position += Shake;
            foreach (SlicedCollider collider in Colliders) {
                if(collider.Width >= 4) 
                    Draw.Rect(collider.Position + Position + (Vector2.UnitX * 2), collider.Width - 4, collider.Height, Color.Black);
                if (collider.Height >= 4)
                    Draw.Rect(collider.Position + Position + (Vector2.UnitY * 2), collider.Width, collider.Height - 4, Color.Black);
            }
            MapTextureOnColliders(dynCogs);
            MapTextureOnColliders(dynEdges);
            Position = pos;
        }
        
        private float mod(float x, float m) {
            return (x % m + m) % m;
        }
    }
}
