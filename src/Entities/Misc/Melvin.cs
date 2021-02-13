using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Entities {

    [CustomEntity("CommunalHelper/Melvin")]
    class Melvin : Solid {

        private static readonly Color fill = Calc.HexToColor("62222b");

        // yeah.
        private static readonly MTexture[,] strongBlock = new MTexture[4, 4];
        private static readonly MTexture[,] weakBlock = new MTexture[4, 4];
        private static readonly MTexture[,] litEdges = new MTexture[4, 4];
        private static readonly MTexture[,] insideBlock = new MTexture[2, 2];
        private static readonly MTexture[,] strongCorners = new MTexture[2, 2];
        private static readonly MTexture[,] weakHCorners = new MTexture[2, 2];
        private static readonly MTexture[,] weakVCorners = new MTexture[2, 2];
        private static readonly MTexture[,] weakCorners = new MTexture[2, 2];
        private static readonly MTexture[,] litHCornersFull = new MTexture[2, 2];
        private static readonly MTexture[,] litHCornersCut = new MTexture[2, 2];
        private static readonly MTexture[,] litVCornersFull = new MTexture[2, 2];
        private static readonly MTexture[,] litVCornersCut = new MTexture[2, 2];

        private Vector2 crushDir;
        private bool triggered = false;

        private bool weakTop, weakBottom, weakLeft, weakRight;

        private Sprite eye;

        private List<Image> activeTopTiles = new List<Image>();
        private List<Image> activeBottomTiles = new List<Image>();
        private List<Image> activeRightTiles = new List<Image>();
        private List<Image> activeLeftTiles = new List<Image>();
        private float topTilesAlpha, bottomTilesAlpha, leftTilesAlpha, rightTilesAlpha;

        public Melvin(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Width, data.Height,
                  data.Bool("weakTop", false), data.Bool("weakBottom", false), data.Bool("weakLeft", false), data.Bool("weakRight", false)) 
            { }

        public Melvin(Vector2 position, int width, int height, bool up, bool down, bool left, bool right)
            : base(position, width, height, safe: false) {

            int w = (int) (base.Width / 8f);
            int h = (int) (base.Height / 8f);

            weakTop = up;
            weakBottom = down;
            weakLeft = left;
            weakRight = right;

            SetupTiles();

            eye = CommunalHelperModule.SpriteBank.Create("melvinEye");
            eye.Position = new Vector2(width / 2, height / 2);
            Add(eye);

            OnDashCollide = OnDashed;
            Add(new LightOcclude(0.2f));

            Add(new Coroutine(Sequence()));
        }

        private void SetupTiles() {
            int w = (int) (Width / 8);
            int h = (int) (Height / 8);

            // middle & edges
            for(int i = 0; i < w; i++) {
                for(int j = 0; j < h; j++) {
                    bool left = i == 0;
                    bool right = i == w - 1;
                    bool top = j == 0;
                    bool bottom = j == h - 1;
                    bool edge = left || right || top || bottom;
                    bool corner = (left || right) && (top || bottom);

                    int rx = Calc.Random.Choose(0, 1);
                    int ry = Calc.Random.Choose(0, 1);
                    Vector2 pos = new Vector2(i * 8, j * 8); 

                    if (!edge) {
                        // middle
                        Image tile = new Image(insideBlock[rx, ry]);
                        tile.Position = pos + new Vector2(Calc.Random.Range(-1, 2), Calc.Random.Range(-1, 2));
                        Add(tile);
                    } else if(!corner) {
                        // edges
                        Image edgeTile = null, litEdgeTile = null;
                        if (right) {
                            edgeTile = new Image((weakRight ? weakBlock : strongBlock)[3, 1 + ry]);
                            litEdgeTile = weakRight ? new Image(litEdges[3, 1 + ry]) : null;
                        }
                        if (left) {
                            edgeTile = new Image((weakLeft ? weakBlock : strongBlock)[0, 1 + ry]);
                            litEdgeTile = weakLeft ? new Image(litEdges[0, 1 + ry]) : null;
                        }
                        if (top) {
                            edgeTile = new Image((weakTop ? weakBlock : strongBlock)[1 + rx, 0]);
                            litEdgeTile = weakTop ? new Image(litEdges[1 + rx, 0]) : null;
                        }
                        if (bottom) {
                            edgeTile = new Image((weakBottom ? weakBlock : strongBlock)[1 + rx, 3]);
                            litEdgeTile = weakBottom ? new Image(litEdges[1 + rx, 3]) : null;
                        }

                        if (edgeTile != null) {
                            edgeTile.Position = pos;
                            Add(edgeTile);
                        }
                        if (litEdgeTile != null) {
                            litEdgeTile.Position = pos;
                            litEdgeTile.Color = Color.Transparent;

                            if (right) activeRightTiles.Add(litEdgeTile);
                            if (left) activeLeftTiles.Add(litEdgeTile);
                            if (bottom) activeBottomTiles.Add(litEdgeTile);
                            if (top) activeTopTiles.Add(litEdgeTile);
                            Add(litEdgeTile);
                        }
                    } else {
                        // corners
                        Image cornerTile = null, litCornerTile1 = null, litCornerTile2 = null;
                        if (left && top) {
                            if(weakTop && weakLeft) {
                                cornerTile = new Image(weakCorners[0, 0]);
                                activeLeftTiles.Add(litCornerTile1 = new Image(litHCornersFull[0, 0]));
                                activeTopTiles.Add(litCornerTile2 = new Image(litVCornersFull[0, 0]));
                            } else if(!weakTop && weakLeft) {
                                cornerTile = new Image(weakHCorners[0, 0]);
                                activeLeftTiles.Add(litCornerTile1 = new Image(litHCornersCut[0, 0]));
                            } else if (weakTop && !weakLeft) {
                                cornerTile = new Image(weakVCorners[0, 0]);
                                activeTopTiles.Add(litCornerTile1 = new Image(litVCornersCut[0, 0]));
                            } else {
                                cornerTile = new Image(strongCorners[0, 0]);
                            }
                        }
                        if (right && top) {
                            if (weakTop && weakRight) {
                                cornerTile = new Image(weakCorners[1, 0]);
                                activeRightTiles.Add(litCornerTile1 = new Image(litHCornersFull[1, 0]));
                                activeTopTiles.Add(litCornerTile2 = new Image(litVCornersFull[1, 0]));
                            } else if (!weakTop && weakRight) {
                                cornerTile = new Image(weakHCorners[1, 0]);
                                activeRightTiles.Add(litCornerTile1 = new Image(litHCornersCut[1, 0]));
                            } else if (weakTop && !weakRight) {
                                cornerTile = new Image(weakVCorners[1, 0]);
                                activeTopTiles.Add(litCornerTile1 = new Image(litVCornersCut[1, 0]));
                            } else {
                                cornerTile = new Image(strongCorners[1, 0]);
                            }
                        }
                        if (left && bottom) {
                            if (weakBottom && weakLeft) {
                                cornerTile = new Image(weakCorners[0, 1]);
                                activeLeftTiles.Add(litCornerTile1 = new Image(litHCornersFull[0, 1]));
                                activeBottomTiles.Add(litCornerTile2 = new Image(litVCornersFull[0, 1]));
                            } else if (!weakBottom && weakLeft) {
                                cornerTile = new Image(weakHCorners[0, 1]);
                                activeLeftTiles.Add(litCornerTile1 = new Image(litHCornersCut[0, 1]));
                            } else if (weakBottom && !weakLeft) {
                                cornerTile = new Image(weakVCorners[0, 1]);
                                activeBottomTiles.Add(litCornerTile1 = new Image(litVCornersCut[0, 1]));
                            } else {
                                cornerTile = new Image(strongCorners[0, 1]);
                            }
                        }
                        if (right && bottom) {
                            if (weakBottom && weakRight) {
                                cornerTile = new Image(weakCorners[1, 1]);
                                activeRightTiles.Add(litCornerTile1 = new Image(litHCornersFull[1, 1]));
                                activeBottomTiles.Add(litCornerTile2 = new Image(litVCornersFull[1, 1]));
                            } else if (!weakBottom && weakRight) {
                                cornerTile = new Image(weakHCorners[1, 1]);
                                activeRightTiles.Add(litCornerTile1 = new Image(litHCornersCut[1, 1]));
                            } else if (weakBottom && !weakRight) {
                                cornerTile = new Image(weakVCorners[1, 1]);
                                activeBottomTiles.Add(litCornerTile1 = new Image(litVCornersCut[1, 1]));
                            } else {
                                cornerTile = new Image(strongCorners[1, 1]);
                            }
                        }

                        if (cornerTile != null) {
                            cornerTile.Position = pos;
                            Add(cornerTile);
                        }
                        if (litCornerTile1 != null) {
                            litCornerTile1.Position = pos;
                            litCornerTile1.Color = Color.Transparent;
                            Add(litCornerTile1);
                        }
                        if (litCornerTile2 != null) {
                            litCornerTile2.Position = pos;
                            litCornerTile2.Color = Color.Transparent;
                            Add(litCornerTile2);
                        }
                    }
                }
            }
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

        public static void InitializeTextures() {
            MTexture strongBlockTexture = GFX.Game["objects/CommunalHelper/melvin/block_strong"];
            MTexture weakBlockTexture = GFX.Game["objects/CommunalHelper/melvin/block_weak"];
            MTexture litEdgesTexture = GFX.Game["objects/CommunalHelper/melvin/lit_edges"];
            MTexture strongCornersTexture = GFX.Game["objects/CommunalHelper/melvin/corners_strong"];
            MTexture weakHCornersTexture = GFX.Game["objects/CommunalHelper/melvin/corners_weak_h"];
            MTexture weakVCornersTexture = GFX.Game["objects/CommunalHelper/melvin/corners_weak_v"];
            MTexture weakCornerTextures = GFX.Game["objects/CommunalHelper/melvin/corners_weak"];
            MTexture insideBlockTexture = GFX.Game["objects/CommunalHelper/melvin/inside"];
            MTexture litHCornersFullTexture = GFX.Game["objects/CommunalHelper/melvin/lit_corners_h_full"];
            MTexture litHCornersCutTexture = GFX.Game["objects/CommunalHelper/melvin/lit_corners_h_cut"];
            MTexture litVCornersFullTexture = GFX.Game["objects/CommunalHelper/melvin/lit_corners_v_full"];
            MTexture litVCornersCutTexture = GFX.Game["objects/CommunalHelper/melvin/lit_corners_v_cut"];

            for (int i = 0; i < 4; i++) {
                for(int j = 0; j < 4; j++) {
                    int tx = i * 8;
                    int ty = j * 8;

                    strongBlock[i, j] = strongBlockTexture.GetSubtexture(tx, ty, 8, 8);
                    weakBlock[i, j] = weakBlockTexture.GetSubtexture(tx, ty, 8, 8);
                    litEdges[i, j] = litEdgesTexture.GetSubtexture(tx, ty, 8, 8);
                    if (i < 2 && j < 2) {
                        insideBlock[i, j] = insideBlockTexture.GetSubtexture(tx, ty, 8, 8);
                        strongCorners[i, j] = strongCornersTexture.GetSubtexture(tx, ty, 8, 8);
                        weakCorners[i, j] = weakCornerTextures.GetSubtexture(tx, ty, 8, 8);
                        weakHCorners[i, j] = weakHCornersTexture.GetSubtexture(tx, ty, 8, 8);
                        weakVCorners[i, j] = weakVCornersTexture.GetSubtexture(tx, ty, 8, 8);
                        litHCornersFull[i, j] = litHCornersFullTexture.GetSubtexture(tx, ty, 8, 8);
                        litHCornersCut[i, j] = litHCornersCutTexture.GetSubtexture(tx, ty, 8, 8);
                        litVCornersFull[i, j] = litVCornersFullTexture.GetSubtexture(tx, ty, 8, 8);
                        litVCornersCut[i, j] = litVCornersCutTexture.GetSubtexture(tx, ty, 8, 8);
                    }
                }
            }
        }

    }
}
