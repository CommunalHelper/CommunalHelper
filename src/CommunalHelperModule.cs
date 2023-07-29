global using Celeste.Mod.Entities;
global using Microsoft.Xna.Framework;
global using Monocle;
global using System;
using Celeste.Mod.CommunalHelper.Backdrops;
using Celeste.Mod.CommunalHelper.Components;
using Celeste.Mod.CommunalHelper.DashStates;
using Celeste.Mod.CommunalHelper.Entities;
using Celeste.Mod.CommunalHelper.Entities.Misc;
using Celeste.Mod.CommunalHelper.Entities.StrawberryJam;
using Celeste.Mod.CommunalHelper.States;
using Celeste.Mod.CommunalHelper.Triggers.StrawberryJam;
using MonoMod.ModInterop;

namespace Celeste.Mod.CommunalHelper;

public class CommunalHelperModule : EverestModule
{
    public static CommunalHelperModule Instance;

    public override Type SettingsType => typeof(CommunalHelperSettings);
    public static CommunalHelperSettings Settings => (CommunalHelperSettings) Instance._Settings;

    public override Type SaveDataType => typeof(CommunalHelperSaveData);
    public static CommunalHelperSaveData SaveData => (CommunalHelperSaveData) Instance._SaveData;

    public override Type SessionType => typeof(CommunalHelperSession);
    public static CommunalHelperSession Session => (CommunalHelperSession) Instance._Session;

    public CommunalHelperModule()
    {
        Instance = this;
    }

    public override void Load()
    {
        Everest.Events.Level.OnLoadEntity += Level_OnLoadEntity;
        Everest.Events.CustomBirdTutorial.OnParseCommand += CustomBirdTutorial_OnParseCommand;

        OptionalDependencies.Load();

        DashStateRefill.Load();
        DreamTunnelDash.Load();
        SeekerDash.Load();

        DreamBlockDummy.Load();

        CustomDreamBlock.Load();
        ConnectedTempleCrackedBlock.Load();

        // Individual Dream Blocks hooked in CustomDreamBlock.Load

        DreamDashCollider.Load();

        AbstractPanel.Load();
        // Panel-specific hooks loaded from AbstractPanel.Load

        ConnectedSwapBlockHooks.Hook();
        CustomCassetteBlock.Hook();

        AttachedWallBooster.Hook();
        MoveBlockRedirect.Load();
        Redirectable.Load();
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
        DreamStrawberry.Hook();

        RedlessBerry.Hook();

        PlayerSeekerBarrier.Hook();
        PlayerSeekerBarrierRenderer.Hook();
        
        ShowHitboxTrigger.Load();
        GrabTempleGate.Hook();
        SolarElevator.Hook();
        ExplodingStrawberry.Load();
        ExpiringDashRefill.Load();
        WormholeBooster.Load();

        AeroBlockCharged.Load();

        Shape3DRenderer.Load();

        St.Load();

        CommunalHelperGFX.Load();
        Pushable.Load();

        #region Imports

        typeof(Imports.CavernHelper).ModInterop();
        typeof(Imports.GravityHelper).ModInterop();

        #endregion

        ModExports.Initialize();
    }

    public override void Unload()
    {
        Everest.Events.Level.OnLoadEntity -= Level_OnLoadEntity;
        Everest.Events.CustomBirdTutorial.OnParseCommand -= CustomBirdTutorial_OnParseCommand;

        OptionalDependencies.Unload();

        DashStateRefill.Unload();
        DreamTunnelDash.Unload();
        SeekerDash.Unload();

        DreamBlockDummy.Unload();

        CustomDreamBlock.Unload();
        ConnectedTempleCrackedBlock.Unload();

        // Individual Dream Blocks unhooked in CustomDreamBlock.Unload

        AbstractPanel.Unload();
        DreamDashCollider.Unload();

        ConnectedSwapBlockHooks.Unhook();
        CustomCassetteBlock.Unhook();

        AttachedWallBooster.Unhook();
        MoveBlockRedirect.Unload();
        Redirectable.Unload();
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
        DreamStrawberry.Unhook();

        RedlessBerry.Unhook();

        PlayerSeekerBarrier.Unhook();
        PlayerSeekerBarrierRenderer.Unhook();
        
        ShowHitboxTrigger.Unload();
        GrabTempleGate.Unhook();
        SolarElevator.Unhook();
        ExplodingStrawberry.Unload();
        ExpiringDashRefill.Unload();
        WormholeBooster.Unload();

        AeroBlockCharged.Unload();

        Shape3DRenderer.Unload();

        St.Unload();

        Cloudscape.Unload();
        BetaCube.Unload();

        CommunalHelperGFX.Unload();
        Pushable.Unload();
    }

    public override void Initialize()
    {
        // Because of `Celeste.Tags.Initialize` of all things
        // We create a static CrystalStaticSpinner which needs to access Tags.TransitionUpdate
        // Which wouldn't be loaded in time for EverestModule.Load
        TimedTriggerSpikes.LoadDelayed();

        // Register CustomCassetteBlock types
        CustomCassetteBlock.Initialize();

        // We may hook methods in other mods, so this needs to be done after they're loaded
        AbstractPanel.LoadDelayed();

        AeroBlock.Initialize();

        BetaCube.Initialize();

        /*
         * Some Communal Helper mechanics don't work well with Gravity Helper.
         * To fix this, Gravity Helper has implemented hooks that patch some of Communal Helper's methods.
         * From now on though, we'll be supporting Gravity Helper with the methods it exports, and fix quirks ourselves.
         * So, we need to call RegisterModSupportBlacklist, which will discard hooks implemented in Gravity Helper.
         */
        Imports.GravityHelper.RegisterModSupportBlacklist?.Invoke("CommunalHelper");
    }

    public override void LoadContent(bool firstLoad)
    {
        CommunalHelperGFX.LoadContent();

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
        CurvedBooster.InitializeParticles();

        DreamJellyfish.InitializeTextures();
        DreamJellyfish.InitializeParticles();

        Chain.InitializeTextures();

        PlayerSeekerHair.InitializeTextures();

        WormholeBooster.InitializeParticles();

        AeroBlock.LoadContent();

        St.Initialize();

        Cloudscape.Initalize();
    }

    protected override void CreateModMenuSectionHeader(TextMenu menu, bool inGame, FMOD.Studio.EventInstance snapshot)
    {
        base.CreateModMenuSectionHeader(menu, inGame, snapshot);

        if (!OptionalDependencies.failedLoadingDeps)
            return;

        menu.Add(new TextMenuExt.SubHeaderExt(Dialog.Clean("communalhelper_failedloadingdeps"))
        {
            TextColor = Color.OrangeRed,
            HeightExtra = 0f,
        });
    }

    internal static bool SavingSettings { get; private set; }
    public override void SaveSettings()
    {
        SavingSettings = true;
        base.SaveSettings();
        SavingSettings = false;
    }

    // Loading "custom" entities
    private bool Level_OnLoadEntity(Level level, LevelData levelData, Vector2 offset, EntityData entityData)
    {
        // Intercept an attempt to load the custom heart (has nodes)
        // Call Level.LoadCustomEntity again incase we skipped over another custom entity handler
        if (entityData.Name == "CommunalHelper/CrystalHeart")
        {
            entityData.Name = "blackGem";
            entityData.Values[HeartGemShard.HeartGem_HeartGemID] = new EntityID(levelData.Name, entityData.ID);
            return Level.LoadCustomEntity(entityData, level);
        }

        if (entityData.Name == "CommunalHelper/AdventureHelper/CustomCrystalHeart")
        {
            entityData.Name = "AdventureHelper/CustomCrystalHeart";
            entityData.Values[HeartGemShard.HeartGem_HeartGemID] = new EntityID(levelData.Name, entityData.ID);
            return Level.LoadCustomEntity(entityData, level);
        }

        if (entityData.Name == "CommunalHelper/MaxHelpingHand/DreamFlagSwitchGate")
        {
            entityData.Name = "CommunalHelper/DreamSwitchGate";
            entityData.Values["isFlagSwitchGate"] = true;
            return Level.LoadCustomEntity(entityData, level);
        }

        // Hackfix because backwards compatability for Ahorn plugins
        if (entityData.Name == "CommunalHelper/DreamFallingBlock" && entityData.Bool("chained"))
        {
            entityData.Name = "CommunalHelper/ChainedDreamFallingBlock";
            return false; // Let the CustomEntity attribute handle it
        }

        // Will be handled later
        return entityData.Name == "CommunalHelper/ManualCassetteController";
    }

    private object CustomBirdTutorial_OnParseCommand(string command)
    {
        // Thank you maddie.
        if (command == "CommunalHelperSyncedZipMoverBinding")
        {
            return Settings.AllowActivateRebinding ?
                Settings.ActivateSyncedZipMovers.Button : Input.Grab;
        }

        return command == "CommunalHelperCycleCassetteBlocksBinding"
            ? Settings.CycleCassetteBlocks.Button
            : command == "CommunalHelperActivateFlagControllerBinding" ? Settings.ActivateFlagController.Button : (object) null;
    }
}

// Don't worry about it
internal class NoInliningException : Exception { public NoInliningException() : base("Something went horribly wrong.") { } }
