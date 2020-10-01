using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;

namespace Celeste.Mod.CommunalHelper.Entities
{
    [CustomEntity("CommunalHelper/PortalZipMover")]
    class PortalZipMover : SlicedSolid {

        private DynamicTexture dynamicTexture = new DynamicTexture();

        public PortalZipMover(EntityData data, Vector2 offset) 
            : this(data.Position + offset, data.Width, data.Height)
        { }

        public PortalZipMover(Vector2 position, int width, int height)
            : base(position, width, height, safe: false)
        {
            BuildTexture();
        }

        private void BuildTexture() {
            MTexture[,] edges = new MTexture[3, 3];

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
                        dynamicTexture.AddTexture(edges[px, py], new Vector2(x, y));
                    }
                }
            }
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
        }

        public override void Update()
        {
            base.Update();
            Move(new Vector2(32, -8) * (float)Math.Sin(Scene.TimeActive) * Engine.DeltaTime);
        }

        public override void Render() {
            base.Render();
            foreach (SlicedCollider collider in Colliders) {
                Vector2 at = Position + collider.Position + collider.TransformedOrigin;
                Rectangle clipRect = new Rectangle(
                    (int) at.X,
                    (int) at.Y,
                    (int) collider.Width,
                    (int) collider.Height);
                dynamicTexture.Render(at, collider.GraphicalTransform, clipRect);
            }
        }
    }
}
