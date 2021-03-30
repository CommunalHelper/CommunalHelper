using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Entities {
    [TrackedAs(typeof(CassetteBlock), true)]
    [CustomEntity("CommunalHelper/CustomCassetteBlock")]
    public class CustomCassetteBlock : CassetteBlock {
        public static List<string> CustomCassetteBlockNames = new List<string>();

        public static void Initialize() {
            // Overengineered attempt to handle CustomCassetteBlock types
            IEnumerable<Type> customCassetteBlockTypes =
                from module in Everest.Modules
                from type in module.GetType().Assembly.GetTypesSafe()
                where (type.IsSubclassOf(typeof(CustomCassetteBlock)) || type == typeof(CustomCassetteBlock))
                select type;

            // This could all be contained in the linq query but that'd be a bit much, no?
            foreach (Type type in customCassetteBlockTypes) {
                foreach (CustomEntityAttribute attrib in type.GetCustomAttributes<CustomEntityAttribute>()) {
                    foreach (string idFull in attrib.IDs) {
                        string id = idFull.Split('=')[0].Trim();
                        CustomCassetteBlockNames.Add(id);
                    }
                }
            }
        }

        protected Color[] colorOptions = new Color[] {
            Calc.HexToColor("49aaf0"),
            Calc.HexToColor("f049be"),
            Calc.HexToColor("fcdc3a"),
            Calc.HexToColor("38e04e")
        };
        protected Color color;
        protected Color pressedColor;

        public int blockHeight = 2;
        protected Vector2 blockOffset = Vector2.Zero;
        private bool dynamicHitbox;
        private Hitbox[] hitboxes;

        public bool present = true;
        public bool virtualCollidable = true;

        public DynData<CassetteBlock> blockData;

        public CustomCassetteBlock(Vector2 position, EntityID id, int width, int height, int index, float tempo, bool dynamicHitbox = false, Color? overrideColor = null)
            : base(position, id, width, height, index, tempo) {
            blockData = new DynData<CassetteBlock>(this);

            Index = index;
            color = overrideColor ?? colorOptions[index];
            pressedColor = color.Mult(Calc.HexToColor("667da5"));

            this.dynamicHitbox = dynamicHitbox;
            if (dynamicHitbox) {
                hitboxes = new Hitbox[3];
                hitboxes[0] = new Hitbox(Collider.Width, Collider.Height - 2);
                hitboxes[1] = new Hitbox(Collider.Width, Collider.Height - 1);
                hitboxes[2] = Collider as Hitbox;
            }
        }

        public override void Update() {
            if (!present) {
                Collidable = virtualCollidable;
            }
            base.Update();
            virtualCollidable = Collidable;
            if (!present) {
                Collidable = false;
                DisableStaticMovers();
            }
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

        public void HandleShiftSize(int amount) {
            blockHeight -= amount;
            blockOffset = (2 - blockHeight) * Vector2.UnitY;
            if (dynamicHitbox) {
                Collider = hitboxes[blockHeight];
            }
        }

        public virtual void HandleUpdateVisualState() {
            blockData.Get<Entity>("side").Visible &= Visible;
            foreach (StaticMover staticMover in staticMovers) {
                staticMover.Visible = Visible;
            }
        }

        protected void UpdatePresent(bool present) {
            this.present = present;
            Collidable = present && virtualCollidable;
        }

        #region Hooks

        private static bool createdCassetteManager = false;

        public static void Hook() {
            On.Celeste.CassetteBlock.ShiftSize += CassetteBlock_ShiftSize;
            On.Celeste.CassetteBlock.UpdateVisualState += CassetteBlock_UpdateVisualState;
            On.Celeste.Level.LoadLevel += Level_LoadLevel;
            Everest.Events.Level.OnLoadEntity += Level_OnLoadEntity;
        }

        public static void Unhook() {
            On.Celeste.CassetteBlock.ShiftSize -= CassetteBlock_ShiftSize;
            On.Celeste.CassetteBlock.UpdateVisualState -= CassetteBlock_UpdateVisualState;
            On.Celeste.Level.LoadLevel -= Level_LoadLevel;
            Everest.Events.Level.OnLoadEntity -= Level_OnLoadEntity;
        }

        private static void CassetteBlock_ShiftSize(On.Celeste.CassetteBlock.orig_ShiftSize orig, CassetteBlock block, int amount) {
            bool shift = true;
            if (block is CustomCassetteBlock cassetteBlock) {
                if (block.Activated && block.CollideCheck<Player>()) {
                    amount *= -1;
                }
                int newBlockHeight = cassetteBlock.blockHeight - amount;
                if (newBlockHeight is > 2 or < 0) {
                    shift = false;
                } else {
                    cassetteBlock.HandleShiftSize(amount);
                }
            }
            if (shift) {
                orig(block, amount);
            }
        }

        private static void CassetteBlock_UpdateVisualState(On.Celeste.CassetteBlock.orig_UpdateVisualState orig, CassetteBlock block) {
            orig(block);
            (block as CustomCassetteBlock)?.HandleUpdateVisualState();
        }

        private static void Level_LoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level level, Player.IntroTypes introType, bool isFromLoader = false) {
            createdCassetteManager = false;
            orig(level, introType, isFromLoader);
        }

        private static MethodInfo m_Level_get_ShouldCreateCassetteManager = typeof(Level).GetProperty("ShouldCreateCassetteManager", BindingFlags.NonPublic | BindingFlags.Instance).GetGetMethod(true);
        private static FieldInfo f_EntityList_toAdd = typeof(EntityList).GetField("toAdd", BindingFlags.NonPublic | BindingFlags.Instance);

        private static bool Level_OnLoadEntity(Level level, LevelData levelData, Vector2 offset, EntityData entityData) {
            if (CustomCassetteBlockNames.Contains(entityData.Name)) {
                level.HasCassetteBlocks = true;
                if (level.CassetteBlockTempo == 1f) {
                    level.CassetteBlockTempo = entityData.Float("tempo", 1f);
                }
                level.CassetteBlockBeats = Math.Max(entityData.Int("index", 0) + 1, level.CassetteBlockBeats);

                if (!createdCassetteManager) {
                    createdCassetteManager = true;
                    if (level.Tracker.GetEntity<CassetteBlockManager>() == null && (bool) m_Level_get_ShouldCreateCassetteManager.Invoke(level, null)) {
                        List<Entity> toAdd = (List<Entity>) f_EntityList_toAdd.GetValue(level.Entities);
                        if (!toAdd.Any(e => e is CassetteBlockManager)) {
                            level.Entities.ForceAdd(new CassetteBlockManager());
                        }
                    }
                }
            }
            return false; // Let the CustomEntity attribute handle actually adding the entities
        }

        #endregion

    }
}
