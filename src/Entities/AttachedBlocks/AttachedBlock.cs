using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Entities {
    [Tracked(true)]
    class AttachedBlock : ConnectedSolid {

        private int tilesWidth, tilesHeight;
        protected char tileType;

        private VirtualMap<char> tiles;
        private TileGrid fakeTileGrid, normalTileGrid;

        private bool detached;
        private bool smoothDetach;

        private VirtualMap<MTexture> levelFakeTiles;

        public AttachedBlock(Vector2 position, int width, int height, char tileType, bool smoothDetach, bool safe = true) :
            base(position, width, height, safe) {
            this.tileType = tileType;
            this.smoothDetach = smoothDetach;

            SurfaceSoundIndex = SurfaceIndex.TileToIndex[tileType];
            Depth = Depths.FGTerrain - 1;
        }

        public override void OnShake(Vector2 amount) {
            base.OnShake(amount);
            normalTileGrid.Position += amount;
            fakeTileGrid.Position += amount;
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);

            tilesWidth = (int) (Width / 8f);
            tilesHeight = (int) (Height / 8f);

            tiles = new VirtualMap<char>(tilesWidth, tilesHeight, '0');

            for (int j = 0; j < tilesHeight; j++)
                for (int i = 0; i < tilesWidth; i++)
                    tiles[i, j] = CollideRect(new Rectangle((int) Left + i * 8, (int) Top + j * 8, 8, 8)) ? tileType : '0';

            normalTileGrid = GFX.FGAutotiler.GenerateMap(tiles, new Autotiler.Behaviour {
                EdgesExtend = false,
                EdgesIgnoreOutOfLevel = false,
                PaddingIgnoreOutOfLevel = false
            }).TileGrid;
            normalTileGrid.Position = new Vector2(Left, Top) - Position;
        }

        private void CreateFakeTiles(VirtualMap<MTexture> tileTextures, VirtualMap<MTexture> tileTexturesExcludeDetached, Rectangle tileBounds, bool onlyFakeTiles) {
            int x = (int) (Left / 8f) - tileBounds.Left;
            int y = (int) (Top / 8f) - tileBounds.Top;

            Remove(fakeTileGrid);
            VirtualMap<MTexture> oldTiles = fakeTileGrid?.Tiles;

            Add(fakeTileGrid = new TileGrid(8, 8, tilesWidth + 2, tilesHeight + 2) {
                Position = new Vector2(Left - 8f, Top - 8f) - Position,
            });

            bool wasNormalTiling = true;
            for (int j = -1; j < tilesHeight + 1; j++) {
                for (int i = -1; i < tilesWidth + 1; i++) {
                    if (tiles.AnyAround(i, j) && tileTexturesExcludeDetached[x + i, y + j] != null) {
                        if (tileTextures[x + i, y + j] != null && tiles[i, j] == '0') {
                            wasNormalTiling = false;
                        }

                        fakeTileGrid.Tiles[i + 1, j + 1] = tiles[i, j] != '0' && onlyFakeTiles && levelFakeTiles[x + i, y + j] == null ? 
                            oldTiles?[i + 1, j + 1] :
                            (smoothDetach ? tileTexturesExcludeDetached : tileTextures)[x + i, y + j];
                    }
                }
            }
            if (wasNormalTiling)
                // We don't need to refresh the fake tiles again, since it was a normal tiling; no fake tiles will be affected.
                Detach(refreshFakeTiles: false);
        }

        protected void Detach(bool refreshFakeTiles = true) {
            if (detached)
                return;
            detached = true;
            Level level = SceneAs<Level>();

            Remove(fakeTileGrid);

            if (!smoothDetach) {
                for (int j = 0; j < tilesHeight; j++)
                    for (int i = 0; i < tilesWidth; i++)
                            normalTileGrid.Tiles[i, j] = tiles[i, j] != '0' ? fakeTileGrid.Tiles[i + 1, j + 1] : null;
                
                Rectangle tileBounds = level.Session.MapData.TileBounds;
                int x = (int) (Left / 8f) - tileBounds.Left;
                int y = (int) (Top / 8f) - tileBounds.Top;

                // We discarded the fake tiles around the actual block(s), so we have to render them elsewhere.
                for (int j = -1; j < tilesHeight + 1; j++)
                    for (int i = -1; i < tilesWidth + 1; i++)
                        if (fakeTileGrid.Tiles[i + 1, j + 1] != null && tiles[i, j] == '0' && level.SolidTiles.Grid[x + i, y + j])
                            levelFakeTiles[x + i, y + j] = fakeTileGrid.Tiles[i + 1, j + 1];
            }

            Add(normalTileGrid);

            if (refreshFakeTiles) 
                UpdateAllFakeTiles(level, updateByOther: true);
        }

        private static void UpdateAllFakeTiles(Level level, bool updateByOther = false) {
            VirtualMap<char> newTileMap = new DynData<SolidTiles>(level.SolidTiles).Get<VirtualMap<char>>("tileTypes").Clone();
            VirtualMap<char> newTileMapExcludeDetached = newTileMap.Clone();
            Rectangle tileBounds = level.Session.MapData.TileBounds;

            IEnumerable<AttachedBlock> attachedBlocks = level.Tracker.GetEntities<AttachedBlock>()
                .Cast<AttachedBlock>()
                .Where(e => !e.detached);

            foreach (AttachedBlock block in level.Tracker.GetEntities<AttachedBlock>()) {
                int x = (int) (block.Left / 8f) - tileBounds.Left;
                int y = (int) (block.Top / 8f) - tileBounds.Top;

                for (int j = 0; j < block.tilesHeight; j++) {
                    for (int i = 0; i < block.tilesWidth; i++) {
                        if (block.tiles[i, j] != '0') {
                            newTileMap[x + i, y + j] = block.tileType;
                            if (!block.detached)
                                newTileMapExcludeDetached[x + i, y + j] = block.tileType;
                        }
                    }
                }
            }

            // TODO: don't autotile the entire level, only the room.
            Calc.PushRandom(level.Session.MapData.LoadSeed);
            VirtualMap<MTexture> newTiles = GFX.FGAutotiler.GenerateMap(newTileMap, paddingIgnoreOutOfLevel: true).TileGrid.Tiles;
            VirtualMap<MTexture> newTilesExludeDetached = GFX.FGAutotiler.GenerateMap(newTileMapExcludeDetached, paddingIgnoreOutOfLevel: true).TileGrid.Tiles;
            Calc.PopRandom();

            foreach (AttachedBlock block in attachedBlocks)
                block.CreateFakeTiles(newTiles, newTilesExludeDetached, tileBounds, updateByOther && !block.smoothDetach);
        }

        #region Hooks

        private const string TileGrid_fakeTiles = "communalHelperFakeTiles";

        internal static void Load() {
            On.Celeste.Level.LoadLevel += Level_LoadLevel;
            On.Monocle.TileGrid.RenderAt += TileGrid_RenderAt;
        }

        internal static void Unload() {
            On.Celeste.Level.LoadLevel -= Level_LoadLevel;
            On.Monocle.TileGrid.RenderAt -= TileGrid_RenderAt;
        }

        private static void Level_LoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader) {
            orig(self, playerIntro, isFromLoader);

            DynData<TileGrid> tileGridData = new DynData<TileGrid>(self.SolidTiles.Tiles);
            VirtualMap<MTexture> levelTileGrid;

            if (!tileGridData.Data.ContainsKey(TileGrid_fakeTiles))
                tileGridData[TileGrid_fakeTiles] = levelTileGrid = new VirtualMap<MTexture>(self.SolidTiles.Tiles.TilesX, self.SolidTiles.Tiles.TilesY);
            else
                levelTileGrid = (VirtualMap<MTexture>) tileGridData[TileGrid_fakeTiles];

            foreach (AttachedBlock block in self.Tracker.GetEntities<AttachedBlock>())
                block.levelFakeTiles = levelTileGrid;

            UpdateAllFakeTiles(self);
        }

        private static void TileGrid_RenderAt(On.Monocle.TileGrid.orig_RenderAt orig, TileGrid self, Vector2 position) {
            DynData<TileGrid> data = new DynData<TileGrid>(self);
            if (data.Data.ContainsKey(TileGrid_fakeTiles)) {
                if (self.Alpha <= 0f) {
                    return;
                }

                VirtualMap<MTexture> fakeTiles = data.Get<VirtualMap<MTexture>>(TileGrid_fakeTiles);

                Rectangle clippedRenderTiles = self.GetClippedRenderTiles();
                Color color = self.Color * self.Alpha;

                for (int i = clippedRenderTiles.Left; i < clippedRenderTiles.Right; i++) {
                    for (int j = clippedRenderTiles.Top; j < clippedRenderTiles.Bottom; j++) {
                        MTexture texture = fakeTiles[i, j] ?? self.Tiles[i, j];
                        texture?.Draw(position + new Vector2(i * self.TileWidth, j * self.TileHeight), Vector2.Zero, color);
                    }
                }
            } else
                orig(self, position);
        }

        #endregion

    }
}
