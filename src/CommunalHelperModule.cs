using Celeste.Mod.CommunalHelper.Backdrops;
using Celeste.Mod.CommunalHelper.DashStates;
using Celeste.Mod.CommunalHelper.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
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

        public static Atlas CloudscapeAtlas { get; private set; }

        private static Dictionary<EverestModuleMetadata, Action<EverestModule>> optionalDepLoaders;
        private static bool failedLoadingDeps;

        public static bool MaxHelpingHandLoaded { get; private set; }
        public static bool VivHelperLoaded { get; private set; }

        public CommunalHelperModule() {
            Instance = this;
        }

        public override void Load() {
            Everest.Events.Level.OnLoadEntity += Level_OnLoadEntity;
            Everest.Events.CustomBirdTutorial.OnParseCommand += CustomBirdTutorial_OnParseCommand;
            Everest.Events.Level.OnLoadBackdrop += Level_OnLoadBackdrop;

            RegisterOptionalDependencies();
            Everest.Events.Everest.OnRegisterModule += OnRegisterModule;

            DashStateRefill.Load();
            DreamTunnelDash.Load();
            SeekerDash.Load();

            DreamBlockDummy.Load();

            CustomDreamBlock.Load();
            // Individual Dream Blocks hooked in CustomDreamBlock.Load

            DreamDashCollider.Load();

            AbstractPanel.Load();
            // Panel-specific hooks loaded from AbstractPanel.Load

            ConnectedSwapBlockHooks.Hook();
            CustomCassetteBlock.Hook();

            AttachedWallBooster.Hook();
            MoveBlockRedirect.Load();
            MoveBlockRedirectable.Load();
            MoveSwapBlock.Load();

            AbstractInputController.Load();
            // Controller-specific hooks loaded from AbstractInputController.Load
            CassetteJumpFixController.Load();
            // TimedTriggerSpikes hooked in Initialize

            UnderwaterMusicController.Load();

            HeartGemShard.Load();
            CustomSummitGem.Load();

            CustomBooster.Load();

            DreamJellyfish.Load();
            DreamJellyfishRenderer.Load();

            ChainedKevin.Load();

            DreamDashListener.Load();
        }

        public override void Unload() {
            Everest.Events.Level.OnLoadEntity -= Level_OnLoadEntity;
            Everest.Events.CustomBirdTutorial.OnParseCommand -= CustomBirdTutorial_OnParseCommand;
            Everest.Events.Level.OnLoadBackdrop -= Level_OnLoadBackdrop;

            Everest.Events.Everest.OnRegisterModule -= OnRegisterModule;

            DashStateRefill.Unload();
            DreamTunnelDash.Unload();
            SeekerDash.Unload();

            DreamBlockDummy.Unload();

            CustomDreamBlock.Unload();
            // Individual Dream Blocks unhooked in CustomDreamBlock.Unload

            AbstractPanel.Unload();
            DreamDashCollider.Unload();

            ConnectedSwapBlockHooks.Unhook();
            CustomCassetteBlock.Unhook();

            AttachedWallBooster.Unhook();
            MoveBlockRedirect.Unload();
            MoveBlockRedirectable.Unload();
            MoveSwapBlock.Unload();
            AbstractInputController.Unload();
            CassetteJumpFixController.Unload();
            TimedTriggerSpikes.Unload();

            UnderwaterMusicController.Unload();

            HeartGemShard.Unload();
            CustomSummitGem.Unload();

            CustomBooster.Unload();

            DreamJellyfish.Unload();
            DreamJellyfishRenderer.Unload();

            ChainedKevin.Unload();

            DreamDashListener.Unload();

            CloudscapeAtlas.Dispose();
        }

        public override void Initialize() {
            // Because of `Celeste.Tags.Initialize` of all things
            // We create a static CrystalStaticSpinner which needs to access Tags.TransitionUpdate
            // Which wouldn't be loaded in time for EverestModule.Load
            TimedTriggerSpikes.LoadDelayed();

            // Register CustomCassetteBlock types
            CustomCassetteBlock.Initialize();

            // We may hook methods in other mods, so this needs to be done after they're loaded
            AbstractPanel.LoadDelayed();
        }

        public override void LoadContent(bool firstLoad) {
            _SpriteBank = new SpriteBank(GFX.Game, "Graphics/CommunalHelper/Sprites.xml");

            CloudscapeAtlas = Extensions.LoadAtlasFromMod("CommunalHelper:/Graphics/Atlases/CommunalHelper/Cloudscape/atlas", Atlas.AtlasDataFormat.CrunchXml);

            StationBlock.InitializeParticles();
            StationBlockTrack.InitializeTextures();
            TrackSwitchBox.InitializeParticles();

            CassetteZipMover.InitializeTextures();

            DreamTunnelRefill.InitializeParticles();
            DreamTunnelDash.InitializeParticles();

            DreamZipMover.InitializeTextures();
            DreamMoveBlock.InitializeParticles();
            DreamSwitchGate.InitializeParticles();

            ConnectedMoveBlock.InitializeTextures();
            ConnectedSwapBlock.InitializeTextures();

            HeartGemShard.InitializeParticles();

            Melvin.InitializeTextures();
            Melvin.InitializeParticles();

            RailedMoveBlock.InitializeTextures();
            DreamBooster.InitializeParticles();

            DreamJellyfish.InitializeTextures();
            DreamJellyfish.InitializeParticles();

            Chain.InitializeTextures();

            Cloudscape.InitializeTextures();
        }

        protected override void CreateModMenuSectionHeader(TextMenu menu, bool inGame, FMOD.Studio.EventInstance snapshot) {
            base.CreateModMenuSectionHeader(menu, inGame, snapshot);

            if (failedLoadingDeps) {
                menu.Add(new TextMenuExt.SubHeaderExt(Dialog.Clean("communalhelper_failedloadingdeps")) {
                    TextColor = Color.OrangeRed,
                    HeightExtra = 0f,
                });
            }
        }

        internal static bool SavingSettings { get; private set; }
        public override void SaveSettings() {
            SavingSettings = true;
            base.SaveSettings();
            SavingSettings = false;
        }

        private void RegisterOptionalDependencies() {
            failedLoadingDeps = false;
            optionalDepLoaders = new();
            EverestModuleMetadata meta;

            // Hair colors used by CustomDreamBlocks particles
            meta = new EverestModuleMetadata { Name = "MoreDasheline", VersionString = "1.6.3" };
            optionalDepLoaders[meta] = module => {
                Extensions.MoreDasheline_GetHairColor = module.GetType().GetMethod("GetHairColor", new Type[] { typeof(Player), typeof(int) }, throwOnNull: true);
                Extensions.MoreDashelineLoaded = true;
                Util.Log(LogLevel.Info, "MoreDasheline detected: using MoreDasheline hair colors for CustomDreamBlock particles.");
            };
            // MiniHeart used by SummitGemManager
            meta = new EverestModuleMetadata { Name = "CollabUtils2", VersionString = "1.3.8.1" };
            optionalDepLoaders[meta] = module => {
                Extensions.CollabUtils_MiniHeart = module.GetType().Module.GetType("Celeste.Mod.CollabUtils2.Entities.MiniHeart", ignoreCase: false, throwOnError: true);
                Extensions.CollabUtilsLoaded = true;
            };
            // Used for registering custom playerstates for display in CelesteTAS
            meta = new EverestModuleMetadata { Name = "CelesteTAS", VersionString = "3.4.5" };
            optionalDepLoaders[meta] = module => {
                Type t_PlayerStates = module.GetType().Module.GetType("TAS.PlayerStates", ignoreCase: false, throwOnError: true);
                Extensions.CelesteTAS_PlayerStates_Register = t_PlayerStates.GetMethod("Register", BindingFlags.Public | BindingFlags.Static, throwOnNull: true);
                Extensions.CelesteTAS_PlayerStates_Unregister = t_PlayerStates.GetMethod("Unregister", BindingFlags.Public | BindingFlags.Static, throwOnNull: true);
                Extensions.CelesteTASLoaded = true;
            };
            meta = new EverestModuleMetadata { Name = "MaxHelpingHand", VersionString = "1.9.3" };
            optionalDepLoaders[meta] = module => MaxHelpingHandLoaded = true;
            meta = new EverestModuleMetadata { Name = "VivHelper", VersionString = "1.0.28" };
            optionalDepLoaders[meta] = module => VivHelperLoaded = true;

            // Check already loaded modules
            foreach (EverestModuleMetadata dep in optionalDepLoaders.Keys) {
                if (Extensions.TryGetModule(dep, out EverestModule module)) {
                    LoadDependency(module, optionalDepLoaders[dep]);
                }
            }
        }

        private void OnRegisterModule(EverestModule module) {
            foreach (EverestModuleMetadata dep in optionalDepLoaders.Keys) {
                if (Extensions.SatisfiesDependency(dep, module.Metadata)) {
                    LoadDependency(module, optionalDepLoaders[dep]);
                    return;
                }
            }
        }

        private void LoadDependency(EverestModule module, Action<EverestModule> loader) {
            try {
                loader.Invoke(module);
            } catch (Exception e) {
                Util.Log(LogLevel.Error, "Failed loading optional dependency: " + module.Metadata.Name);
                Console.WriteLine(e.ToString());
                // Show something on screen to alert user
                failedLoadingDeps = true;
            }
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

            // Hackfix because backwards compatability for Ahorn plugins
            if (entityData.Name == "CommunalHelper/DreamFallingBlock" && entityData.Bool("chained")) {
                entityData.Name = "CommunalHelper/ChainedDreamFallingBlock";
                return false; // Let the CustomEntity attribute handle it
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

        // Loading custom backdrops
        // When the [CustomBackdrop] attributes becomes available to use in Everest, this will be removed.
        private static Backdrop Level_OnLoadBackdrop(MapData map, BinaryPacker.Element child, BinaryPacker.Element above) {
            string name = child.Name;

            if (name.Equals(Cloudscape.ID, StringComparison.OrdinalIgnoreCase))
                return new Cloudscape(child);

            return null;
        }
    }

    // Don't worry about it
    internal class NoInliningException : Exception { public NoInliningException() : base("Something went horribly wrong.") { } }
}
