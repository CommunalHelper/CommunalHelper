using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Entities {

    [CustomEntity("CommunalHelper/RailedMoveBlock")]
    class RailedMoveBlock : Solid {

        private class RailedMoveBlockPathRenderer : Entity {
            public RailedMoveBlock block;

            private MTexture cog;

            private Vector2 from;
            private Vector2 to;
            private Vector2 sparkAdd;

            private float sparkDirFromA;
            private float sparkDirFromB;
            private float sparkDirToA;
            private float sparkDirToB;

            public RailedMoveBlockPathRenderer(RailedMoveBlock zipMover) {
                base.Depth = 5000;
                block = zipMover;

                from = block.start + new Vector2(block.Width / 2f, block.Height / 2f);
                to = block.target + new Vector2(block.Width / 2f, block.Height / 2f);

                sparkAdd = (from - to).SafeNormalize(5f).Perpendicular();

                float num = (from - to).Angle();
                sparkDirFromA = num + (float) Math.PI / 8f;
                sparkDirFromB = num - (float) Math.PI / 8f;
                sparkDirToA = num + (float) Math.PI - (float) Math.PI / 8f;
                sparkDirToB = num + (float) Math.PI + (float) Math.PI / 8f;

                cog = GFX.Game["objects/zipmover/cog"];
            }

            public void CreateSparks() {
                SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, from + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirFromA);
                SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, from - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirFromB);
                SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, to + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirToA);
                SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, to - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirToB);
            }

            public override void Render() {
                DrawCogs(Vector2.UnitY, Color.Black);
                DrawCogs(Vector2.Zero);
            }

            private void DrawCogs(Vector2 offset, Color? colorOverride = null) {
                Vector2 vector = (to - from).SafeNormalize();
                Vector2 value = vector.Perpendicular() * 3f;
                Vector2 value2 = -vector.Perpendicular() * 4f;

                float rotation = block.percent * (float) Math.PI * 2f;

                Draw.Line(from + value + offset, to + value + offset, colorOverride ?? block.fillColor);
                Draw.Line(from + value2 + offset, to + value2 + offset, colorOverride ?? block.fillColor);

                Color highlightColor = Color.Lerp(block.fillColor, Color.White, 0.15f);

                for (float num = 4f - block.percent * (float) Math.PI * 8f % 4f; num < (to - from).Length(); num += 4f) {
                    Vector2 value3 = from + value + vector.Perpendicular() + vector * num;
                    Vector2 value4 = to + value2 - vector * num;

                    Draw.Line(value3 + offset, value3 + vector * 2f + offset, colorOverride ?? highlightColor);
                    Draw.Line(value4 + offset, value4 - vector * 2f + offset, colorOverride ?? highlightColor);
                }
                cog.DrawCentered(from + offset, colorOverride ?? Color.White, 1f, rotation);
                cog.DrawCentered(to + offset, colorOverride ?? Color.White, 1f, rotation);
            }
        }
        private RailedMoveBlockPathRenderer pathRenderer;

        private class Border : Entity {
            public RailedMoveBlock Parent;

            public Border(RailedMoveBlock parent) {
                Parent = parent;
                base.Depth = 1;
            }

            public override void Update() {
                if (Parent.Scene != base.Scene) {
                    RemoveSelf();
                }
                base.Update();
            }

            public override void Render() {
                Draw.Rect(Parent.X + Parent.Shake.X - 1f, Parent.Y + Parent.Shake.Y - 1f, Parent.Width + 2f, Parent.Height + 2f, Color.Black);
            }
        }
        private Border border;

        private List<Image> body = new List<Image>();

        private Color fillColor = IdleBgFill;
        public static readonly Color IdleBgFill = Calc.HexToColor("474070");
        public static readonly Color MoveBgFill = Calc.HexToColor("30b335");
        public static readonly Color StopBgFill = Calc.HexToColor("cc2541");

        private Vector2 start, target;
        float percent;

        public RailedMoveBlock(EntityData data, Vector2 offset) 
            : this(data.Position + offset, data.Width, data.Height, data.NodesOffset(offset)[0]) { }

        public RailedMoveBlock(Vector2 position, int width, int height, Vector2 node) 
            : base(position, width, height, safe: false) {
            start = position;
            target = node;

            int tilesWidth = width / 8;
            int tilesHeight = height / 8;

            MTexture block = GFX.Game["objects/moveBlock/base"];

            for (int i = 0; i < tilesWidth; i++) {
                for (int j = 0; j < tilesHeight; j++) {
                    int tx = ((i != 0) ? ((i < tilesWidth - 1) ? 1 : 2) : 0);
                    int ty = ((j != 0) ? ((j < tilesHeight - 1) ? 1 : 2) : 0);
                    AddImage(block.GetSubtexture(tx * 8, ty * 8, 8, 8), new Vector2(i, j) * 8f, new Vector2(1f, 1f), body);
                }
            }

            Add(new LightOcclude(0.5f));
        }

        private void AddImage(MTexture tex, Vector2 position, Vector2 scale, List<Image> addTo) {
            Image image = new Image(tex);
            image.Position = position + new Vector2(4f, 4f);
            image.CenterOrigin();
            image.Scale = scale;
            Add(image);
            addTo?.Add(image);
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);

            scene.Add(border = new Border(this));
            scene.Add(pathRenderer = new RailedMoveBlockPathRenderer(this));
        }

        public override void Removed(Scene scene) {
            scene.Remove(pathRenderer);
            pathRenderer = null;

            scene.Remove(border);
            border = null;

            base.Removed(scene);
        }

        public override void Update() {
            base.Update();

            Color newFillColor = IdleBgFill;

            Player player = GetPlayerRider();
            if (player != null) {
                newFillColor = MoveBgFill;
            }

            fillColor = Color.Lerp(fillColor, newFillColor, 10f * Engine.DeltaTime);
        }

        public override void Render() {
            Vector2 position = Position;
            Position += base.Shake;

            Draw.Rect(base.X + 3f, base.Y + 3f, base.Width - 6f, base.Height - 6f, fillColor);
            foreach (Image item4 in body) {
                item4.Render();
            }

            Position = position;
        }

    }
}
