using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper {
    abstract class CustomCassetteBlock : CassetteBlock {
		static int[] typeCounts = new int[4];

		protected Color[] colorOptions = new Color[] {
			Calc.HexToColor("49aaf0"),
			Calc.HexToColor("f049be"),
			Calc.HexToColor("fcdc3a"),
			Calc.HexToColor("38e04e")
		};
		protected Color color;
		private int beforeIndex;
		public int blockHeight = 2;
		public Vector2 blockOffset = Vector2.Zero;

		public CustomCassetteBlock(Vector2 position, EntityID id, int width, int height, int index, int typeIndex, float tempo) 
			: base(position, id, width, height, index, tempo) {
			beforeIndex = index;
			color = colorOptions[index];
			Index = typeCounts[typeIndex] * typeCounts.Length * 4 + index + 4;
			++typeCounts[typeIndex];
		}

		public override void Awake(Scene scene) {
			base.Awake(scene);
			Index = beforeIndex;
		}
	}

	class CustomCassetteBlockHooks {
		static bool attemptedLoad = false;
		static bool createdCassetteManager = false;

		public static void Hook() {
            On.Celeste.CassetteBlock.ShiftSize += modShiftSize;
            On.Celeste.Level.LoadLevel += modLoadLevel;
            On.Celeste.Level.LoadCustomEntity += modLoadCustomEntity;
		}

        public static void Unhook() {
            On.Celeste.CassetteBlock.ShiftSize -= modShiftSize;
            On.Celeste.Level.LoadLevel -= modLoadLevel;
            On.Celeste.Level.LoadCustomEntity -= modLoadCustomEntity;
		}

		private static void modShiftSize(On.Celeste.CassetteBlock.orig_ShiftSize orig, CassetteBlock block, int amount) {
			if (block is CustomCassetteBlock) {
				if (block.Activated && block.CollideCheck<Player>()) {
					amount *= -1;
				}
				CustomCassetteBlock customBlock = block as CustomCassetteBlock;
				customBlock.blockHeight -= amount;
				customBlock.blockOffset = (2 - customBlock.blockHeight) * Vector2.UnitY;
			}
			orig(block, amount);
		}

		private static void modLoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level level, Player.IntroTypes introType, bool isFromLoader = false) {
			attemptedLoad = false;
			orig(level, introType, isFromLoader);
		}

		private static bool modLoadCustomEntity(On.Celeste.Level.orig_LoadCustomEntity orig, EntityData entityData, Level level) {
			bool result = orig(entityData, level);
			if (!attemptedLoad) {
				createdCassetteManager = false;
				foreach (EntityData data in level.Session.LevelData.Entities) {
					switch (data.Name) {
						case "CommunalHelper/CassetteZipMover":
						case "CommunalHelper/CassetteMoveBlock":
						case "CommunalHelper/CassetteSwapBlock":
						case "CommunalHelper/CassetteFallingBlock":
							level.HasCassetteBlocks = true;
							if (level.CassetteBlockTempo == 1f) {
								level.CassetteBlockTempo = data.Float("tempo", 1f);
							}
							level.CassetteBlockBeats = Math.Max(data.Int("index", 0) + 1, level.CassetteBlockBeats);

							if (!createdCassetteManager) {
								createdCassetteManager = true;
								CassetteBlockManager manager = level.Tracker.GetEntity<CassetteBlockManager>();
								if (manager == null && ShouldCreateCassetteManager(level)) {
									level.Add(new CassetteBlockManager());
									level.Entities.UpdateLists();
								}
							}
							break;
					}
				}
				attemptedLoad = true;
			}
			return result;
		}

		private static bool ShouldCreateCassetteManager(Level level) {
			if (level.Session.Area.Mode == AreaMode.Normal) {
				return !level.Session.Cassette;
			}
			return true;
		}
	}
}
