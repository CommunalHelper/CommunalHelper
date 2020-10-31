using Celeste.Mod.CommunalHelper.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
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
        
        public CommunalHelperModule() {
            Instance = this;
        }

        public override void Load() {
            Everest.Events.Level.OnLoadEntity += Level_OnLoadEntity;
            Everest.Events.CustomBirdTutorial.OnParseCommand += CustomBirdTutorial_OnParseCommand;

            DreamTunnelDash.Load();
            DreamRefill.Load();

            CustomDreamBlock.Load();
            ConnectedDreamBlock.Hook();
            ConnectedSwapBlockHooks.Hook();
            CustomCassetteBlockHooks.Hook();

            AttachedWallBooster.Hook();
            MoveBlockRedirect.Load();
            MoveSwapBlock.Load();
            SyncedZipMoverActivationControllerHooks.Hook();

            HeartGemShard.Load();
            CustomSummitGem.Load();
        }

        public override void Unload() {
            Everest.Events.Level.OnLoadEntity -= Level_OnLoadEntity;
            Everest.Events.CustomBirdTutorial.OnParseCommand -= CustomBirdTutorial_OnParseCommand;

            DreamTunnelDash.Unload();
            DreamRefill.Unload();

            CustomDreamBlock.Unload();
            ConnectedDreamBlock.Unhook();
            ConnectedSwapBlockHooks.Unhook();
            CustomCassetteBlockHooks.Unhook();

			AttachedWallBooster.Unhook();
            MoveBlockRedirect.Unload();
            MoveSwapBlock.Unload();
            SyncedZipMoverActivationControllerHooks.Unhook();

            HeartGemShard.Unload();
            CustomSummitGem.Unload();
        }

		public override void LoadContent(bool firstLoad) {
            _SpriteBank = new SpriteBank(GFX.Game, "Graphics/CommunalHelper/Sprites.xml");

			StationBlock.InitializeParticles();

            DreamTunnelDash.LoadContent();
            DreamRefill.InitializeParticles();
            DreamMoveBlock.InitializeParticles();
            DreamSwitchGate.InitializeParticles();
            
            ConnectedMoveBlock.InitializeTextures();
            ConnectedSwapBlock.InitializeTextures();

            HeartGemShard.InitializeParticles();
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

            return false;
        }

        private object CustomBirdTutorial_OnParseCommand(string command) {
            // Thank you max480.
            if (command == "CommunalHelperSyncedZipMoverBinding") {
                return Settings.AllowActivateRebinding ?
                    Settings.ActivateSyncedZipMovers.Button : Input.Grab;
            }
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

        public static Color TryParseColor(string str, float alpha = 1f) {
            foreach (PropertyInfo prop in namedColors) { 
                if (str.Equals(prop.Name)) { 
                    return new Color((Color) prop.GetValue(null), alpha); 
                } 
            }
            return new Color(Calc.HexToColor(str.Trim('#')), alpha);
        }
	}

}
