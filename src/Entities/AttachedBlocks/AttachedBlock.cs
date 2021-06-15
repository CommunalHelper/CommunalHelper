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

        public AttachedBlock(Vector2 position, int width, int height, char tileType, bool safe = true) :
            base(position, width, height, safe) {
            this.tileType = tileType;
            SurfaceSoundIndex = SurfaceIndex.TileToIndex[tileType];
            Depth = Depths.FGTerrain - 1;
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

        private void CreateFakeTiles(VirtualMap<MTexture> tileTextures, Rectangle tileBounds) {
            int x = (int) (Left / 8f) - tileBounds.Left;
            int y = (int) (Top / 8f) - tileBounds.Top;

            Remove(fakeTileGrid);
            Add(fakeTileGrid = new TileGrid(8, 8, tilesWidth + 2, tilesHeight + 2) {
                Position = new Vector2(Left - 8f, Top - 8f) - Position,
            });

            bool wasNormalTiling = true;
            for (int j = -1; j < tilesHeight + 1; j++) {
                for (int i = -1; i < tilesWidth + 1; i++) {
                    if (tiles.AnyAround(i, j)) {
                        fakeTileGrid.Tiles[i + 1, j + 1] = tileTextures[x + i, y + j];
                        if (tileTextures[x + i, y + j] != null && tiles[i, j] == '0')
                            wasNormalTiling = false;
                    }
                }
            }
            if (wasNormalTiling)
                // We don't need to refres the fake tiles again, since it was a normal tiling; no fake tiles will be affected.
                Detach(refreshFakeTiles: false);
        }

        protected void Detach(bool refreshFakeTiles = true) {
            if (detached)
                return;
            detached = true;

            Remove(fakeTileGrid);

            Add(normalTileGrid);

            if (refreshFakeTiles) 
                UpdateAllFakeTiles(SceneAs<Level>());
        }

        private static void UpdateAllFakeTiles(Level level) {
            VirtualMap<char> newTileMap = new DynData<SolidTiles>(level.SolidTiles).Get<VirtualMap<char>>("tileTypes").Clone();
            Rectangle tileBounds = level.Session.MapData.TileBounds;

            IEnumerable<AttachedBlock> attachedBlocks = level.Tracker.GetEntities<AttachedBlock>()
                .Cast<AttachedBlock>()
                .Where(e => !e.detached);
            foreach (AttachedBlock block in attachedBlocks) {
                int x = (int) (block.Left / 8f) - tileBounds.Left;
                int y = (int) (block.Top / 8f) - tileBounds.Top;

                for (int j = 0; j < block.tilesHeight; j++)
                    for (int i = 0; i < block.tilesWidth; i++)
                        if (block.tiles[i, j] != '0')
                            newTileMap[x + i, y + j] = block.tileType;
            }

            Calc.PushRandom(level.Session.MapData.LoadSeed);
            VirtualMap<MTexture> newTiles = GFX.FGAutotiler.GenerateMap(newTileMap, paddingIgnoreOutOfLevel: true).TileGrid.Tiles;
            Calc.PopRandom();

            foreach (AttachedBlock block in attachedBlocks)
                block.CreateFakeTiles(newTiles, tileBounds);
        }

        #region Hooks

        internal static void Load() {
            On.Celeste.Level.LoadLevel += Level_LoadLevel;
        }

        internal static void Unload() {
            On.Celeste.Level.LoadLevel -= Level_LoadLevel;
        }

        private static void Level_LoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader) {
            orig(self, playerIntro, isFromLoader);
            UpdateAllFakeTiles(self);
        }

        #endregion

    }
}
