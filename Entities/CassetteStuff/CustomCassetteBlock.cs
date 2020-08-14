using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
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
		protected Color pressedColor;

		private int beforeIndex;

		public bool present = true;
		public int blockHeight = 2;
		public Vector2 blockOffset = Vector2.Zero;

		public DynData<CassetteBlock> blockData;

		public CustomCassetteBlock(Vector2 position, EntityID id, int width, int height, int index, int typeIndex, float tempo) 
			: base(position, id, width, height, index, tempo) {
			beforeIndex = index;
			color = colorOptions[index];
			pressedColor = color.Mult(Calc.HexToColor("667da5"));
            Index = typeCounts[typeIndex] * typeCounts.Length * 4 + index + 4;
            ++typeCounts[typeIndex];
		}

        public override void Awake(Scene scene) {
			blockData = new DynData<CassetteBlock>(this);
			base.Awake(scene);
        }

        public void ResetIndex() {
			Index = beforeIndex;
        }

		protected void AddCenterSymbol(Image solid, Image pressed) {
			blockData.Get<List<Image>>("solid").Add(solid);
			blockData.Get<List<Image>>("pressed").Add(pressed);
			List<Image> all = blockData.Get<List<Image>>("all");
			Vector2 origin = blockData.Get<Vector2>("groupOrigin") - Position;
			Vector2 size = new Vector2(Width, Height);

			Vector2 value = (size - new Vector2(solid.Width, solid.Height)) * 0.5f;
			solid.Origin = origin - value;
			solid.Position = origin;
			solid.Color = color;
			Add(solid);
			all.Add(solid);

			value = (size - new Vector2(pressed.Width, pressed.Height)) * 0.5f;
			pressed.Origin = origin - value;
			pressed.Position = origin;
			pressed.Color = color;
			Add(pressed);
			all.Add(pressed);
		}
	}

	class CustomCassetteBlockHooks {
		static bool attemptedLoad = false;
		static bool createdCassetteManager = false;

		public static void Hook() {
            On.Celeste.CassetteBlock.ShiftSize += modShiftSize;
			On.Celeste.CassetteBlock.UpdateVisualState += modUpdateVisualState;
			On.Celeste.Level.LoadLevel += modLoadLevel;
            On.Celeste.Level.LoadCustomEntity += modLoadCustomEntity;
            On.Monocle.EntityList.UpdateLists += modUpdateLists;
		}

        public static void Unhook() {
            On.Celeste.CassetteBlock.ShiftSize -= modShiftSize;
			On.Celeste.CassetteBlock.UpdateVisualState -= modUpdateVisualState;
			On.Celeste.Level.LoadLevel -= modLoadLevel;
            On.Celeste.Level.LoadCustomEntity -= modLoadCustomEntity;
			On.Monocle.EntityList.UpdateLists -= modUpdateLists;
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

		private static void modUpdateVisualState(On.Celeste.CassetteBlock.orig_UpdateVisualState orig, CassetteBlock block) {
			orig(block);
			CustomCassetteBlock customBlock = block as CustomCassetteBlock;
			if (customBlock != null) {
				if (!customBlock.present) {
					customBlock.blockData.Get<Entity>("side").Visible = false;
				}
				if (customBlock is CassetteMoveBlock) {
					(customBlock as CassetteMoveBlock).UpdateSymbol();
                }
            }
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

		private static void modUpdateLists(On.Monocle.EntityList.orig_UpdateLists orig, EntityList list) {
			List<CustomCassetteBlock> blocks = new List<CustomCassetteBlock>();
			var listData = new DynData<EntityList>(list);
			foreach (Entity entity in listData.Get<List<Entity>>("toAdd")) {
				if (entity is CustomCassetteBlock) {
					blocks.Add(entity as CustomCassetteBlock);
                }
            }
			orig(list);
			foreach (var block in blocks) {
				block.ResetIndex();
            }
		}
	}
}
