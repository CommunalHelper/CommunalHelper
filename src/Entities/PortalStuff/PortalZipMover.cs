using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using static Celeste.ZipMover;

namespace Celeste.Mod.CommunalHelper.Entities
{
    [CustomEntity("CommunalHelper/PortalZipMover")]
    class PortalZipMover : SlicedSolid {

        private class ZipMoverPathRenderer : Entity {
            public PortalZipMover block;

            private MTexture cog;
            private Vector2 from, to;
            private Vector2 sparkAdd;

            private float sparkDirFromA;
            private float sparkDirFromB;
            private float sparkDirToA;
            private float sparkDirToB;

            public Rectangle RenderZone;

            private static readonly Color ropeColor = Calc.HexToColor("663931");
            private static readonly Color ropeLightColor = Calc.HexToColor("9b6157");

            public ZipMoverPathRenderer(PortalZipMover zipMover) {
                base.Depth = 5000;
                block = zipMover;
                from = block.start + new Vector2(block.OriginalWidth / 2f, block.OriginalHeight / 2f);
                to = block.target + new Vector2(block.OriginalWidth / 2f, block.OriginalHeight / 2f);
                sparkAdd = (from - to).SafeNormalize(5f).Perpendicular();
                float num = (from - to).Angle();
                sparkDirFromA = num + (float) Math.PI / 8f;
                sparkDirFromB = num - (float) Math.PI / 8f;
                sparkDirToA = num + (float) Math.PI - (float) Math.PI / 8f;
                sparkDirToB = num + (float) Math.PI + (float) Math.PI / 8f;
                if (zipMover.theme == Themes.Moon) {
                    cog = GFX.Game["objects/zipmover/moon/cog"];
                } else {
                    cog = GFX.Game["objects/zipmover/cog"];
                }

                Point a = new Point((int) Math.Min(from.X, to.X), (int) Math.Min(from.Y, to.Y));
                Point b = new Point((int) Math.Max(from.X, to.X), (int) Math.Max(from.Y, to.Y));
                RenderZone = new Rectangle(a.X, a.Y, b.X - a.X, b.Y - a.Y);
                RenderZone.Inflate(8, 8);
            }

            public void CreateSparks() {
                SceneAs<Level>().ParticlesBG.Emit(P_Sparks, from + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirFromA);
                SceneAs<Level>().ParticlesBG.Emit(P_Sparks, from - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirFromB);
                SceneAs<Level>().ParticlesBG.Emit(P_Sparks, to + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirToA);
                SceneAs<Level>().ParticlesBG.Emit(P_Sparks, to - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirToB);
            }

            private DynamicTexture GetDynamicTexture() {
                DynamicTexture res = new DynamicTexture();
                DrawCogs(res, Vector2.UnitY, Color.Black);
                DrawCogs(res, Vector2.Zero);
                return res;
            }

            public override void Render() {
                base.Render();
                block.renderSolid.MapTextureOnColliders(GetDynamicTexture());
            }

            private void DrawCogs(DynamicTexture tex, Vector2 offset, Color? colorOverride = null) {
                Vector2 vector = (to - from).SafeNormalize();
                Vector2 value = vector.Perpendicular() * 3f;
                Vector2 value2 = -vector.Perpendicular() * 4f;
                float rotation = block.percent * (float) Math.PI * 2f;
                Draw.Line(from + value + offset, to + value + offset, colorOverride ?? ropeColor);
                Draw.Line(from + value2 + offset, to + value2 + offset, colorOverride ?? ropeColor);
                for (float num = 4f - block.percent * (float) Math.PI * 8f % 4f; num < (to - from).Length(); num += 4f) {
                    Vector2 value3 = from + value + vector.Perpendicular() + vector * num;
                    Vector2 value4 = to + value2 - vector * num;
                    //Draw.Line(value3 + offset, value3 + vector * 2f + offset, colorOverride ?? ropeLightColor);
                    //Draw.Line(value4 + offset, value4 - vector * 2f + offset, colorOverride ?? ropeLightColor);
                }
                //cog.DrawCentered(from + offset, colorOverride ?? Color.White, 1f, rotation);
                tex.AddTexture(cog, from + offset - new Vector2(RenderZone.X, RenderZone.Y), colorOverride ?? Color.White, true);
                //cog.DrawCentered(to + offset, colorOverride ?? Color.White, 1f, rotation);
                tex.AddTexture(cog, to + offset - new Vector2(RenderZone.X, RenderZone.Y), colorOverride ?? Color.White, true);
            }
        }

        private Themes theme;

        private DynamicTexture dynEdges = new DynamicTexture();
        private List<MTexture> innerCogs;
        private float percent = 0;
        private Vector2 target, start;
        private Sprite streetlight;

        private ZipMoverPathRenderer pathRenderer;
        private SlicedSolid renderSolid;

        public PortalZipMover(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Width, data.Height, data.Nodes[0] + offset, data.Enum("theme", Themes.Normal))
        { }

        public PortalZipMover(Vector2 position, int width, int height, Vector2 node, Themes theme)
            : base(position, width, height, safe: false)
        {
            this.theme = theme;

            string path;
            string id;
            string key;

            if (theme == Themes.Moon) {
                path = "objects/zipmover/moon/light";
                id = "objects/zipmover/moon/block";
                key = "objects/zipmover/moon/innercog";
            } else {
                path = "objects/zipmover/light";
                id = "objects/zipmover/block";
                key = "objects/zipmover/innercog";
            }

            innerCogs = GFX.Game.GetAtlasSubtextures(key);
            streetlight = new Sprite(GFX.Game, path);
            streetlight.Add("frames", "", 1f);
            streetlight.Play("frames");
            streetlight.Active = false;
            streetlight.SetAnimationFrame(1);
            streetlight.Position = new Vector2(base.Width / 2f - streetlight.Width / 2f, 0f);
            
            BuildTexture(id, streetlight);
            Add(new Coroutine(Sequence()));
            Add(new LightOcclude());
            target = node;
            start = position;
            SurfaceSoundIndex = 7;
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            scene.Add(pathRenderer = new ZipMoverPathRenderer(this));

            if (start != target) {
                Vector2 dir = Vector2.Normalize(target - start);
                Rectangle rect = pathRenderer.RenderZone;
                scene.Add(renderSolid = new SlicedSolid(new Vector2(rect.X, rect.Y) - dir * 2, rect.Width, rect.Height, false));
                renderSolid.FitPortalCondition = false;
                renderSolid.Collidable = false;
                renderSolid.Added(scene);
                renderSolid.Move(dir * 2);
            }
        }

        public override void Removed(Scene scene) {
            scene.Remove(pathRenderer);
            pathRenderer = null;
            base.Removed(scene);
        }


        private IEnumerator Sequence() {
            while (true) {
                if (!HasPlayerRider()) {
                    yield return null;
                    continue;
                }
                Audio.Play("event:/new_content/game/10_farewell/zip_mover", Position);
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
                StartShaking(0.1f);
                yield return 0.1f;
                streetlight.SetAnimationFrame(3);
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
                streetlight.SetAnimationFrame(2);
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
                streetlight.SetAnimationFrame(1);
                yield return 0.5f;
            }
        }

        private void BuildTexture(string blockid, Sprite streetlight) {
            MTexture[,] edges = new MTexture[3, 3];

            for (int i = 0; i < 3; i++) {
                for (int j = 0; j < 3; j++) {
                    edges[i, j] = GFX.Game[blockid].GetSubtexture(i * 8, j * 8, 8, 8);
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

            dynEdges.AddSprite(streetlight, Vector2.Zero);
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

            //Draw.HollowRect(pathRenderer.RenderZone, Color.Blue);
        }
        
        private float mod(float x, float m) {
            return (x % m + m) % m;
        }
    }
}
