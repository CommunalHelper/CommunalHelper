using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using static Celeste.CrushBlock;

namespace Celeste.Mod.CommunalHelper.Entities {

    [CustomEntity("CommunalHelper/Melvin")]
    class Melvin : Solid {

        private static readonly Color fill = Calc.HexToColor("62222b");

        private Axes axis;
        private Vector2 crushDir;
        private bool triggered = false;

        private Sprite eye;

        private List<Image> activeTopTiles = new List<Image>();
        private List<Image> activeBottomTiles = new List<Image>();
        private List<Image> activeRightTiles = new List<Image>();
        private List<Image> activeLeftTiles = new List<Image>();
        private float topTilesAlpha, bottomTilesAlpha, leftTilesAlpha, rightTilesAlpha;

        public Melvin(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Width, data.Height, data.Enum("axes", Axes.Both)) { }

        public Melvin(Vector2 position, int width, int height, Axes axis)
            : base(position, width, height, safe: false) {

            int w = (int) (base.Width / 8f);
            int h = (int) (base.Height / 8f);

            this.axis = axis;

            string blockAxis = axis switch {
                Axes.Vertical => "surf_V",
                Axes.Horizontal => "surf_H",
                _ => "surf_A"
            };

            MTexture litUp = GFX.Game["objects/CommunalHelper/melvin/lit_up_" + (axis == Axes.Both ? "full" : "cut")];
            MTexture litDown = GFX.Game["objects/CommunalHelper/melvin/lit_down_" + (axis == Axes.Both ? "full" : "cut")];
            MTexture litLeft = GFX.Game["objects/CommunalHelper/melvin/lit_left_" + (axis == Axes.Both ? "full" : "cut")];
            MTexture litRight = GFX.Game["objects/CommunalHelper/melvin/lit_right_" + (axis == Axes.Both ? "full" : "cut")];

            MTexture block = GFX.Game["objects/CommunalHelper/melvin/" + blockAxis];
            MTexture inside = GFX.Game["objects/CommunalHelper/melvin/inside"];

            for (int i = 0; i < w; i++) {
                for (int j = 0; j < h; j++) {
                    bool horizontalEdge = i == 0 || i == w - 1; // left and right edges
                    bool verticalEdge = j == 0 || j == h - 1; // bottom and top edges
                    bool edge = horizontalEdge || verticalEdge; // both
                    bool corner = horizontalEdge && verticalEdge; // corner

                    int tx = (i == 0) ? 0 : ((i == w - 1) ? 3 : Calc.Random.Choose(1, 2));
                    int ty = (j == 0) ? 0 : ((j == h - 1) ? 3 : Calc.Random.Choose(1, 2));
                    if (!edge) { tx -= 1; ty -= 1; }

                    Image image = new Image((edge ? block : inside).GetSubtexture(tx * 8, ty * 8, 8, 8));
                    image.Position = new Vector2(i * 8, j * 8);
                    if (!edge)
                        image.Position += new Vector2(Calc.Random.Choose(-1, 1), Calc.Random.Choose(-1, 1)); // randomness looks cool!
                    Add(image);

                    if (edge) {
                        if ((corner && axis == Axes.Both) || (horizontalEdge && (axis == Axes.Horizontal || axis == Axes.Both))) {
                            bool left = i == 0;
                            Image litEdge = new Image((left ? litLeft : litRight).GetSubtexture(0, ty * 8, 8, 8)) {
                                Position = image.Position,
                                Color = Color.Transparent
                            };
                            (left ? activeLeftTiles : activeRightTiles).Add(litEdge);
                            Add(litEdge);
                        }
                        if ((corner && axis == Axes.Both) || (verticalEdge && (axis == Axes.Vertical || axis == Axes.Both))) {
                            bool top = j == 0;
                            Image litEdge = new Image((top ? litUp : litDown).GetSubtexture(tx * 8, 0, 8, 8)) {
                                Position = image.Position,
                                Color = Color.Transparent
                            };
                            (top ? activeTopTiles : activeBottomTiles).Add(litEdge);
                            Add(litEdge);
                        }
                    }
                }
            }

            eye = CommunalHelperModule.SpriteBank.Create("melvinEye");
            eye.Position = new Vector2(width / 2, height / 2);
            Add(eye);

            OnDashCollide = OnDashed;
            Add(new LightOcclude(0.2f));
            //Add(new BloomPoint(new Vector2(width / 2, height / 2), .5f, (width + height) / 2)); // experimental bloom

            Add(new Coroutine(Sequence()));
        }

        public DashCollisionResults OnDashed(Player player, Vector2 dir) {
            if (!triggered) {
                ActivateTiles(dir.Y == 1, dir.Y == -1, dir.X == 1, dir.X == -1);
                Audio.Play("event:/game/06_reflection/crushblock_activate", base.Center);
                eye.Play("target", true);
                triggered = true;
                crushDir = -dir;
            }
            return DashCollisionResults.Rebound;
        }

        private string GetAnimFromDirection(string prefix, Vector2 dir) {
            if (dir.Y == 1)
                return prefix + "Down";
            if (dir.Y == -1)
                return prefix + "Up";
            if (dir.X == -1)
                return prefix + "Left";
            return prefix + "Right";
        }

        private IEnumerator Sequence() {
            while(true) {
                while(!triggered) {
                    yield return null;
                }

                yield return .4f;

                eye.Play(GetAnimFromDirection("target", crushDir), true);
                yield return 2f;

                eye.Play(GetAnimFromDirection("targetReverse", crushDir), true);
                triggered = false;
            }
        }

        private void ActivateTiles(bool up, bool down, bool left, bool right) {
            if (up)
                topTilesAlpha = 1f;
            if (down)
                bottomTilesAlpha = 1f;
            if (left)
                leftTilesAlpha = 1f;
            if (right)
                rightTilesAlpha = 1f;
        }
             
        private void UpdateActiveTiles() {
            topTilesAlpha = Calc.Approach(topTilesAlpha, 0f, Engine.DeltaTime * 0.5f);
            bottomTilesAlpha = Calc.Approach(bottomTilesAlpha, 0f, Engine.DeltaTime * 0.5f);
            leftTilesAlpha = Calc.Approach(leftTilesAlpha, 0f, Engine.DeltaTime * 0.5f);
            rightTilesAlpha = Calc.Approach(rightTilesAlpha, 0f, Engine.DeltaTime * 0.5f);

            foreach (Image tile in activeTopTiles) {
                tile.Color = Color.White * topTilesAlpha;
            }
            foreach (Image tile in activeBottomTiles) {
                tile.Color = Color.White * bottomTilesAlpha;
            }
            foreach (Image tile in activeLeftTiles) {
                tile.Color = Color.White * leftTilesAlpha;
            }
            foreach (Image tile in activeRightTiles) {
                tile.Color = Color.White * rightTilesAlpha;
            }
        }

        public override void Update() {
            base.Update();
            UpdateActiveTiles();
        }

        public override void Render() {
            Vector2 position = Position;
            Position += Shake;
            Draw.Rect(base.X + 2f, base.Y + 2f, base.Width - 4f, base.Height - 4f, fill);
            base.Render();

            Position = position;
        }

    }
}
