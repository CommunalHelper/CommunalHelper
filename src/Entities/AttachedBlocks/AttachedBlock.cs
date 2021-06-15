using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Entities {
    [Tracked(true)]
    class AttachedBlock : ConnectedSolid {

        private int tilesWidth;
        private int tilesHeight;
        private char tileType;

        private VirtualMap<bool> tiles;
        private VirtualMap<bool> tilesOutline;

        public AttachedBlock(Vector2 position, int width, int height, char tileType, bool safe = true) :
            base(position, width, height, safe) {
            this.tileType = tileType;
            SurfaceSoundIndex = SurfaceIndex.TileToIndex[tileType];
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);

            tilesWidth = (int) (Width / 8f);
            tilesHeight = (int) (Height / 8f);

            tiles = new VirtualMap<bool>(tilesWidth, tilesHeight);
            tilesOutline = new VirtualMap<bool>(tilesWidth + 2, tilesHeight + 2);

            for (int j = 0; j < tilesHeight; j++)
                for (int i = 0; i < tilesWidth; i++)
                    tiles[i, j] = CollideRect(new Rectangle((int) Left + i * 8, (int) Top + j * 8, 8, 8));

            // messy, is true if any of the tiles around the current one are present.
            for (int j = -1; j < tilesHeight + 1; j++)
                for (int i = -1; i < tilesWidth + 1; i++)
                    tilesOutline[i + 1, j + 1] = 
                        !tiles[i, j] &&
                        (tiles[i - 1, j] || tiles[i + 1, j] || tiles[i, j - 1] || tiles[i, j + 1] ||
                        tiles[i - 1, j - 1] || tiles[i - 1, j + 1] || tiles[i + 1, j - 1] || tiles[i + 1, j + 1]);
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

            VirtualMap<char> newTileMap = new DynData<SolidTiles>(self.SolidTiles).Get<VirtualMap<char>>("tileTypes").Clone();
            Rectangle tileBounds = self.Session.MapData.TileBounds;

            List<Entity> attachedBlocks = self.Tracker.GetEntities<AttachedBlock>();
            foreach (AttachedBlock block in attachedBlocks) {
                int x = (int) (block.Left / 8f) - tileBounds.Left;
                int y = (int) (block.Top / 8f) - tileBounds.Top;

                for (int j = 0; j < block.tilesHeight; j++)
                    for (int i = 0; i < block.tilesWidth; i++)
                        if (block.tiles[i, j])
                            newTileMap[x + i, y + j] = block.tileType;
            }

            VirtualMap<MTexture> newTiles = GFX.FGAutotiler.GenerateMap(newTileMap, paddingIgnoreOutOfLevel: true).TileGrid.Tiles;
            self.SolidTiles.Tiles.Tiles = newTiles;
        }

        #endregion

    }
}
