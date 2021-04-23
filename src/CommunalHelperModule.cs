using Celeste.Mod.CommunalHelper.Entities;
using Celeste.Mod.Entities;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper {
    public class CommunalHelperModule : EverestModule {

        public static CommunalHelperModule Instance;

        public override Type SettingsType => typeof(CommunalHelperSettings);
        public static CommunalHelperSettings Settings => (CommunalHelperSettings) Instance._Settings;

        public override Type SaveDataType => typeof(CommunalHelperSaveData);
        public static CommunalHelperSaveData SaveData => (CommunalHelperSaveData) Instance._SaveData;

        public override Type SessionType => typeof(CommunalHelperSession);
        public static CommunalHelperSession Session => (CommunalHelperSession) Instance._Session;

        public static SpriteBank SpriteBank => Instance._SpriteBank;
        public SpriteBank _SpriteBank;


        public static bool MaxHelpingHandLoaded { get; private set; }
        public static bool VivHelperLoaded { get; private set; }

        public CommunalHelperModule() {
            Instance = this;
        }

        public override void Load() {
            Everest.Events.Level.OnLoadEntity += Level_OnLoadEntity;
            Everest.Events.CustomBirdTutorial.OnParseCommand += CustomBirdTutorial_OnParseCommand;

            DreamTunnelDash.Load();
            DreamRefill.Load();

            CustomDreamBlock.Load();
            // Individual Dream Blocks hooked in CustomDreamBlock.Load

            ConnectedSwapBlockHooks.Hook();
            CustomCassetteBlock.Hook();

            AttachedWallBooster.Hook();
            MoveBlockRedirect.Load();
            MoveBlockRedirectable.Load();
            MoveSwapBlock.Load();
            AbstractController.Load();
            // Controller-specific hooks loaded from AbstractController.Load
            // TimedTriggerSpikes hooked in Initialize

            HeartGemShard.Load();
            CustomSummitGem.Load();

            CustomBooster.Load();
        }

        public override void Unload() {
            Everest.Events.Level.OnLoadEntity -= Level_OnLoadEntity;
            Everest.Events.CustomBirdTutorial.OnParseCommand -= CustomBirdTutorial_OnParseCommand;

            DreamTunnelDash.Unload();
            DreamRefill.Unload();

            CustomDreamBlock.Unload();
            // Individual Dream Blocks unhooked in CustomDreamBlock.Unload

            ConnectedSwapBlockHooks.Unhook();
            CustomCassetteBlock.Unhook();

            AttachedWallBooster.Unhook();
            MoveBlockRedirect.Unload();
            MoveBlockRedirectable.Unload();
            MoveSwapBlock.Unload();
            AbstractController.Unload();
            TimedTriggerSpikes.Unload();

            HeartGemShard.Unload();
            CustomSummitGem.Unload();

            CustomBooster.Unload();
        }

        public override void Initialize() {
            // Because of `Celeste.Tags.Initialize` of all things
            // We create a static CrystalStaticSpinner which needs to access Tags.TransitionUpdate
            // Which wouldn't be loaded in time for EverestModule.Load
            TimedTriggerSpikes.Load();

            // Register CustomCassetteBlock types
            CustomCassetteBlock.Initialize();
        }

        public override void LoadContent(bool firstLoad) {
            _SpriteBank = new SpriteBank(GFX.Game, "Graphics/CommunalHelper/Sprites.xml");

            StationBlock.InitializeParticles();
            TrackSwitchBox.InitializeParticles();

            DreamTunnelDash.LoadContent();
            DreamRefill.InitializeParticles();
            DreamMoveBlock.InitializeParticles();
            DreamSwitchGate.InitializeParticles();

            ConnectedMoveBlock.InitializeTextures();
            ConnectedSwapBlock.InitializeTextures();

            HeartGemShard.InitializeParticles();

            Melvin.InitializeTextures();
            Melvin.InitializeParticles();

            DreamBooster.InitializeParticles();


            EverestModuleMetadata moreDashelineMeta = new EverestModuleMetadata { Name = "MoreDasheline", VersionString = "1.6.3" };
            if (Extensions.TryGetModule(moreDashelineMeta, out EverestModule dashelineModule)) {
                Extensions.MoreDashelineLoaded = true;
                Extensions.MoreDasheline_GetHairColor = dashelineModule.GetType().GetMethod("GetHairColor", new Type[] { typeof(Player), typeof(int) });
                Logger.Log("Communal Helper", "MoreDasheline detected: using MoreDasheline hair colors for CustomDreamBlock particles.");
            }
            EverestModuleMetadata collabUtilsMeta = new EverestModuleMetadata { Name = "CollabUtils2", VersionString = "1.3.8.1" };
            if (Extensions.TryGetModule(collabUtilsMeta, out EverestModule collabModule)) {
                Extensions.CollabUtilsLoaded = true;
                Extensions.CollabUtils_MiniHeart = collabModule.GetType().Module.GetType("Celeste.Mod.CollabUtils2.Entities.MiniHeart");
            }

            MaxHelpingHandLoaded = Everest.Loader.DependencyLoaded(new EverestModuleMetadata { Name = "MaxHelpingHand", VersionString = "1.9.3" });
            VivHelperLoaded = Everest.Loader.DependencyLoaded(new EverestModuleMetadata { Name = "VivHelper", VersionString = "1.0.28" });
        }

        // Loading "custom" entities
        private bool Level_OnLoadEntity(Level level, LevelData levelData, Vector2 offset, EntityData entityData) {
            // Intercept an attempt to load the custom heart (has nodes)
            // Call Level.LoadCustomEntity again incase we skipped over another custom entity handler
            if (entityData.Name == "CommunalHelper/CrystalHeart") {
                entityData.Name = "blackGem";
                entityData.Values[HeartGemShard.HeartGem_HeartGemID] = new EntityID(levelData.Name, entityData.ID);
                return Level.LoadCustomEntity(entityData, level);
            }

            if (entityData.Name == "CommunalHelper/AdventureHelper/CustomCrystalHeart") {
                entityData.Name = "AdventureHelper/CustomCrystalHeart";
                entityData.Values[HeartGemShard.HeartGem_HeartGemID] = new EntityID(levelData.Name, entityData.ID);
                return Level.LoadCustomEntity(entityData, level);
            }

            if (entityData.Name == "CommunalHelper/MaxHelpingHand/DreamFlagSwitchGate") {
                entityData.Name = "CommunalHelper/DreamSwitchGate";
                entityData.Values["isFlagSwitchGate"] = true;
                return Level.LoadCustomEntity(entityData, level);
            }

            // Will be handled later
            if (entityData.Name == "CommunalHelper/ManualCassetteController")
                return true;

            return false;
        }

        private object CustomBirdTutorial_OnParseCommand(string command) {
            // Thank you max480.
            if (command == "CommunalHelperSyncedZipMoverBinding") {
                return Settings.AllowActivateRebinding ?
                    Settings.ActivateSyncedZipMovers.Button : Input.Grab;
            }

            if (command == "CommunalHelperCycleCassetteBlocksBinding")
                return Settings.CycleCassetteBlocks.Button;

            if (command == "CommunalHelperActivateFlagControllerBinding")
                return Settings.ActivateFlagController.Button;

            return null;
        }
    }

    public static class Util {
        public static void Log(string str) {
            Logger.Log("Communal Helper", str);
        }

        public static bool TryGetPlayer(out Player player) {
            player = Engine.Scene?.Tracker?.GetEntity<Player>();
            return player != null;
        }

        private static PropertyInfo[] namedColors = typeof(Color).GetProperties();

        public static Color CopyColor(Color color, float alpha) {
            return new Color(color.R, color.G, color.B, (byte) alpha * 255);
        }

        public static Color CopyColor(Color color, int alpha) {
            return new Color(color.R, color.G, color.B, alpha);
        }

        public static Color ColorArrayLerp(float lerp, params Color[] colors) {
            float m = lerp % colors.Length;
            int fromIndex = (int) Math.Floor(m);
            int toIndex = (fromIndex + 1) % colors.Length;
            float clampedLerp = m - fromIndex;

            return Color.Lerp(colors[fromIndex], colors[toIndex], clampedLerp);
        }

        public static Color TryParseColor(string str, float alpha = 1f) {
            foreach (PropertyInfo prop in namedColors) {
                if (str.Equals(prop.Name)) {
                    return CopyColor((Color) prop.GetValue(null), alpha);
                }
            }
            return CopyColor(Calc.HexToColor(str.Trim('#')), alpha);
        }

        public static int ToInt(bool b) => b ? 1 : 0;

        public static int ToBitFlag(params bool[] b) {
            int ret = 0;
            for (int i = 0; i < b.Length; i++)
                ret |= ToInt(b[i]) << i;
            return ret;
        }

    }

}
