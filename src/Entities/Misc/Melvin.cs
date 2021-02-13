using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using static Celeste.Mod.CommunalHelper.Entities.StationBlock;

namespace Celeste.Mod.CommunalHelper.Entities {

    [CustomEntity("CommunalHelper/Melvin")]
    class Melvin : Solid {

        public static ParticleType P_Activate;

        private static readonly Color fill = Calc.HexToColor("62222b");

        #region Tiles
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
        #endregion

        private struct MoveState {
            public Vector2 From;
            public Vector2 Direction;

            public MoveState(Vector2 from, Vector2 direction) {
                From = from;
                Direction = direction;
            }
        }

        private List<MoveState> returnStack = new List<MoveState>();

        private Level level;

        private Vector2 crushDir;
        private ArrowDir dir;
        private bool triggered = false;

        private bool weakTop, weakBottom, weakLeft, weakRight;

        private Sprite eye;

        private List<Image> activeTopTiles = new List<Image>();
        private List<Image> activeBottomTiles = new List<Image>();
        private List<Image> activeRightTiles = new List<Image>();
        private List<Image> activeLeftTiles = new List<Image>();
        private List<Image> blockTiles = new List<Image>();
        private float topTilesAlpha, bottomTilesAlpha, leftTilesAlpha, rightTilesAlpha;

        public Melvin(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Width, data.Height,
                  data.Bool("weakTop", false), data.Bool("weakBottom", false), data.Bool("weakLeft", false), data.Bool("weakRight", false)) 
            { }

        public Melvin(Vector2 position, int width, int height, bool up, bool down, bool left, bool right)
            : base(position, width, height, safe: false) {

            weakTop = up;
            weakBottom = down;
            weakLeft = left;
            weakRight = right;

            SetupTiles();

            eye = CommunalHelperModule.SpriteBank.Create("melvinEye");
            eye.Position = new Vector2(width / 2, height / 2);
            Add(eye);

            Add(new LightOcclude(0.2f));

            Add(new Coroutine(Sequence()));
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            level = SceneAs<Level>();
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
                        blockTiles.Add(tile);
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
                            blockTiles.Add(edgeTile);
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

        private void CreateMoveState() {
            bool flag = true;
            if (returnStack.Count > 0) {
                MoveState moveState = returnStack[returnStack.Count - 1];
                if (moveState.Direction == crushDir || moveState.Direction == -crushDir) {
                    flag = false;
                }
            }
            if (flag) {
                returnStack.Add(new MoveState(Position, crushDir));
            }
        }

        private IEnumerator Sequence() {
            while(true) {
                while(!triggered) {
                    yield return null;
                }
                CreateMoveState();
                string animDir = Enum.GetName(typeof(ArrowDir), dir);

                ActivateTiles(dir);
                ActivateParticles();
                Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                StartShaking(0.4f);

                Audio.Play("event:/game/06_reflection/crushblock_activate", base.Center);
                eye.Play("target", true);
                yield return .4f;

                eye.Play("target" + animDir, true);

                StopPlayerRunIntoAnimation = false;
                float speed = 0f;
                while (true) {
                    speed = Calc.Approach(speed, 240f, 500f * Engine.DeltaTime);

                    bool flag = ((crushDir.X == 0f) ? MoveVCheck(speed * crushDir.Y * Engine.DeltaTime) : MoveHCheck(speed * crushDir.X * Engine.DeltaTime));
                    if (Top >= (float) (level.Bounds.Bottom + 32)) {
                        RemoveSelf();
                        yield break;
                    }
                    if (flag) {
                        break;
                    }
                    if (Scene.OnInterval(0.02f)) {
                        Vector2 position;
                        float direction;
                        if (crushDir == Vector2.UnitX) {
                            position = new Vector2(Left + 1f, Calc.Random.Range(Top + 3f, Bottom - 3f));
                            direction = (float) Math.PI;
                        } else if (crushDir == -Vector2.UnitX) {
                            position = new Vector2(Right - 1f, Calc.Random.Range(Top + 3f, Bottom - 3f));
                            direction = 0f;
                        } else if (crushDir == Vector2.UnitY) {
                            position = new Vector2(Calc.Random.Range(Left + 3f, Right - 3f), Top + 1f);
                            direction = -(float) Math.PI / 2f;
                        } else {
                            position = new Vector2(Calc.Random.Range(Left + 3f, Right - 3f), Bottom - 1f);
                            direction = (float) Math.PI / 2f;
                        }
                        level.Particles.Emit(SwitchGate.P_Behind, position, direction);
                    }
                    yield return null;
                }

                FallingBlock fallingBlock = CollideFirst<FallingBlock>(Position + crushDir);
                if (fallingBlock != null) {
                    fallingBlock.Triggered = true;
                }
                if (crushDir == -Vector2.UnitX) {
                    Vector2 value = new Vector2(0f, 2f);
                    for (int i = 0; (float) i < Height / 8f; i++) {
                        Vector2 vector = new Vector2(Left - 1f, Top + 4f + (float) (i * 8));
                        if (!Scene.CollideCheck<Water>(vector) && Scene.CollideCheck<Solid>(vector)) {
                            SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, vector + value, 0f);
                            SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, vector - value, 0f);
                        }
                    }
                } else if (crushDir == Vector2.UnitX) {
                    Vector2 value2 = new Vector2(0f, 2f);
                    for (int j = 0; (float) j < Height / 8f; j++) {
                        Vector2 vector2 = new Vector2(Right + 1f, Top + 4f + (float) (j * 8));
                        if (!Scene.CollideCheck<Water>(vector2) && Scene.CollideCheck<Solid>(vector2)) {
                            SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, vector2 + value2, (float) Math.PI);
                            SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, vector2 - value2, (float) Math.PI);
                        }
                    }
                } else if (crushDir == -Vector2.UnitY) {
                    Vector2 value3 = new Vector2(2f, 0f);
                    for (int k = 0; (float) k < Width / 8f; k++) {
                        Vector2 vector3 = new Vector2(Left + 4f + (float) (k * 8), Top - 1f);
                        if (!Scene.CollideCheck<Water>(vector3) && Scene.CollideCheck<Solid>(vector3)) {
                            SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, vector3 + value3, (float) Math.PI / 2f);
                            SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, vector3 - value3, (float) Math.PI / 2f);
                        }
                    }
                } else if (crushDir == Vector2.UnitY) {
                    Vector2 value4 = new Vector2(2f, 0f);
                    for (int l = 0; (float) l < Width / 8f; l++) {
                        Vector2 vector4 = new Vector2(Left + 4f + (float) (l * 8), Bottom + 1f);
                        if (!Scene.CollideCheck<Water>(vector4) && Scene.CollideCheck<Solid>(vector4)) {
                            SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, vector4 + value4, -(float) Math.PI / 2f);
                            SceneAs<Level>().ParticlesFG.Emit(CrushBlock.P_Impact, vector4 - value4, -(float) Math.PI / 2f);
                        }
                    }
                }

                Audio.Play("event:/game/06_reflection/crushblock_impact", Center);
                level.DirectionalShake(crushDir);
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                StartShaking(0.4f);
                StopPlayerRunIntoAnimation = true;
                eye.Play("targetReverse" + animDir, true);
                crushDir = Vector2.Zero;
                yield return .4f;

                speed = 0f;
                float waypointSfxDelay = 0f;
                while (returnStack.Count > 0) {
                    yield return null;
                    StopPlayerRunIntoAnimation = false;
                    MoveState moveState = returnStack[returnStack.Count - 1];
                    speed = Calc.Approach(speed, 60f, 160f * Engine.DeltaTime);
                    waypointSfxDelay -= Engine.DeltaTime;
                    if (moveState.Direction.X != 0f) {
                        MoveTowardsX(moveState.From.X, speed * Engine.DeltaTime);
                    }
                    if (moveState.Direction.Y != 0f) {
                        MoveTowardsY(moveState.From.Y, speed * Engine.DeltaTime);
                    }
                    if ((moveState.Direction.X != 0f && ExactPosition.X != moveState.From.X) || (moveState.Direction.Y != 0f && ExactPosition.Y != moveState.From.Y)) {
                        continue;
                    }
                    speed = 0f;
                    returnStack.RemoveAt(returnStack.Count - 1);
                    StopPlayerRunIntoAnimation = true;
                    if (returnStack.Count <= 0) {
                        if (waypointSfxDelay <= 0f) {
                            Audio.Play("event:/game/06_reflection/crushblock_rest", Center);
                        }
                    } else if (waypointSfxDelay <= 0f) {
                        Audio.Play("event:/game/06_reflection/crushblock_rest_waypoint", Center);
                    }
                    waypointSfxDelay = 0.1f;
                    StartShaking(0.2f);
                    yield return 0.2f;
                }

                triggered = false;
            }
        }

        private bool MoveHCheck(float amount) {
            if (MoveHCollideSolidsAndBounds(level, amount, thruDashBlocks: true)) {
                if (amount < 0f && base.Left <= (float) level.Bounds.Left) {
                    return true;
                }
                if (amount > 0f && base.Right >= (float) level.Bounds.Right) {
                    return true;
                }
                for (int i = 1; i <= 4; i++) {
                    for (int num = 1; num >= -1; num -= 2) {
                        Vector2 value = new Vector2(Math.Sign(amount), i * num);
                        if (!CollideCheck<Solid>(Position + value)) {
                            MoveVExact(i * num);
                            MoveHExact(Math.Sign(amount));
                            return false;
                        }
                    }
                }
                return true;
            }
            return false;
        }

        private bool MoveVCheck(float amount) {
            if (MoveVCollideSolidsAndBounds(level, amount, thruDashBlocks: true, null, checkBottom: false)) {
                if (amount < 0f && base.Top <= (float) level.Bounds.Top) {
                    return true;
                }
                for (int i = 1; i <= 4; i++) {
                    for (int num = 1; num >= -1; num -= 2) {
                        Vector2 value = new Vector2(i * num, Math.Sign(amount));
                        if (!CollideCheck<Solid>(Position + value)) {
                            MoveHExact(i * num);
                            MoveVExact(Math.Sign(amount));
                            return false;
                        }
                    }
                }
                return true;
            }
            return false;
        }

        private void ActivateParticles() {
            float direction;
            Vector2 position;
            Vector2 positionRange;
            int num;
            if (dir == ArrowDir.Right) {
                direction = 0f;
                position = base.CenterRight - Vector2.UnitX;
                positionRange = Vector2.UnitY * (base.Height - 2f) * 0.5f;
                num = (int) (base.Height / 8f) * 4;
            } else if (dir == ArrowDir.Left) {
                direction = (float) Math.PI;
                position = base.CenterLeft + Vector2.UnitX;
                positionRange = Vector2.UnitY * (base.Height - 2f) * 0.5f;
                num = (int) (base.Height / 8f) * 4;
            } else if (dir == ArrowDir.Down) {
                direction = (float) Math.PI / 2f;
                position = base.BottomCenter - Vector2.UnitY;
                positionRange = Vector2.UnitX * (base.Width - 2f) * 0.5f;
                num = (int) (base.Width / 8f) * 4;
            } else {
                direction = -(float) Math.PI / 2f;
                position = base.TopCenter + Vector2.UnitY;
                positionRange = Vector2.UnitX * (base.Width - 2f) * 0.5f;
                num = (int) (base.Width / 8f) * 4;
            }
            num += 2;
            level.Particles.Emit(P_Activate, num, position, positionRange, direction);
        }

        private void ActivateTiles(ArrowDir dir) {
            switch (dir) {
                case ArrowDir.Up:
                    topTilesAlpha = 1f;
                    break;
                case ArrowDir.Down:
                    bottomTilesAlpha = 1f;
                    break;
                case ArrowDir.Left:
                    leftTilesAlpha = 1f;
                    break;
                case ArrowDir.Right:
                    rightTilesAlpha = 1f;
                    break;

                default:
                    break;
            }
        }
             
        private void UpdateActiveTiles() {
            topTilesAlpha = Calc.Approach(topTilesAlpha, triggered && dir == ArrowDir.Up && crushDir != Vector2.Zero ? 1f : 0f, Engine.DeltaTime * 2f);
            bottomTilesAlpha = Calc.Approach(bottomTilesAlpha, triggered && dir == ArrowDir.Down && crushDir != Vector2.Zero ? 1f : 0f, Engine.DeltaTime * 2f);
            leftTilesAlpha = Calc.Approach(leftTilesAlpha, triggered && dir == ArrowDir.Left && crushDir != Vector2.Zero ? 1f : 0f, Engine.DeltaTime * 2f);
            rightTilesAlpha = Calc.Approach(rightTilesAlpha, triggered && dir == ArrowDir.Right && crushDir != Vector2.Zero ? 1f : 0f, Engine.DeltaTime * 2f);

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

        private bool IsPlayerSeen(Rectangle rect, ArrowDir dir) {
            if(dir == ArrowDir.Up || dir == ArrowDir.Down) {
                for (int i = 0; i < rect.Width; i++) {
                    Rectangle lineRect = new Rectangle(rect.X + i, rect.Y, 1, rect.Height);
                    if (!Scene.CollideCheck<Solid>(lineRect))
                        return true;
                }
                return false;
            } else {
                for(int i = 0; i < rect.Height; i++) {
                    Rectangle lineRect = new Rectangle(rect.X, rect.Y + i, rect.Width, 1);
                    if (!Scene.CollideCheck<Solid>(lineRect))
                        return true;
                }
                return false;
            }
        }

        public override void Update() {
            base.Update();

            if (!triggered && Util.TryGetPlayer(out Player player)) {
                bool detectedPlayer = false;
                Rectangle toPlayerRect = new Rectangle();
                if (player.Center.Y > Y && player.Center.Y < Y + Height) {
                    int y1 = (int) Math.Max(player.Top, Top);
                    int y2 = (int) Math.Min(player.Bottom, Bottom);
                    if (player.Center.X > X + Width) {
                        // right
                        detectedPlayer = true;
                        crushDir = Vector2.UnitX;
                        dir = ArrowDir.Right;
                        toPlayerRect = new Rectangle((int) (X + Width), y1, (int) (player.Left - X - Width), y2 - y1);
                    }
                    if (player.Center.X < X) {
                        // left
                        detectedPlayer = true;
                        crushDir = -Vector2.UnitX;
                        dir = ArrowDir.Left;
                        toPlayerRect = new Rectangle((int) (player.Right), y1, (int) (X - player.Right), y2 - y1);
                    }
                }
                if (player.Center.X > X && player.Center.X < X + Width) {
                    int x1 = (int) Math.Max(player.Left, Left);
                    int x2 = (int) Math.Min(player.Right, Right);
                    if (player.Center.Y < Y) {
                        // top
                        detectedPlayer = true;
                        crushDir = -Vector2.UnitY;
                        dir = ArrowDir.Up;
                        toPlayerRect = new Rectangle(x1, (int) (player.Bottom), x2 - x1, (int) (Y - player.Bottom));
                    }
                    if (player.Center.Y > Y + Height) {
                        // bottom
                        detectedPlayer = true;
                        crushDir = Vector2.UnitY;
                        dir = ArrowDir.Down;
                        toPlayerRect = new Rectangle(x1, (int) (Y + Height), x2 - x1, (int) (player.Top - Y - Height));
                    }
                }
                if (detectedPlayer && IsPlayerSeen(toPlayerRect, dir)) {
                    triggered = true;
                }
            }

            UpdateActiveTiles();
        }

        public override void Render() {
            Vector2 position = Position;
            Position += Shake * (triggered ? crushDir : Vector2.One);
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

        public static void InitializeParticles() {
            P_Activate = new ParticleType(CrushBlock.P_Activate) {
                Color = Calc.HexToColor("e45f7c")
        };
        }
    }
}
