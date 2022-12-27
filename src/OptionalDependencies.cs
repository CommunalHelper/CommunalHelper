using System;
using System.Collections.Generic;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper;

internal static class OptionalDependencies
{
    public static Dictionary<EverestModuleMetadata, Action<EverestModule>> optionalDepLoaders;
    public static bool failedLoadingDeps { get; private set; }

    public static bool MaxHelpingHandLoaded { get; private set; }
    public static bool VivHelperLoaded { get; private set; }

    internal static void Load()
    {
        RegisterOptionalDependencies();
        Everest.Events.Everest.OnRegisterModule += OnRegisterModule;
    }

    internal static void Unload()
    {
        Everest.Events.Everest.OnRegisterModule -= OnRegisterModule;
    }

    private static void RegisterOptionalDependencies()
    {
        failedLoadingDeps = false;
        optionalDepLoaders = new();
        EverestModuleMetadata meta;

        // Hair colors used by CustomDreamBlocks particles
        meta = new EverestModuleMetadata { Name = "MoreDasheline", VersionString = "1.6.3" };
        optionalDepLoaders[meta] = module =>
        {
            Extensions.MoreDasheline_GetHairColor = module.GetType().GetMethod("GetHairColor", new Type[] { typeof(Player), typeof(int) }, throwOnNull: true);
            Extensions.MoreDashelineLoaded = true;
            Util.Log(LogLevel.Info, "MoreDasheline detected: using MoreDasheline hair colors for CustomDreamBlock particles.");
        };
        // MiniHeart used by SummitGemManager
        meta = new EverestModuleMetadata { Name = "CollabUtils2", VersionString = "1.3.8.1" };
        optionalDepLoaders[meta] = module =>
        {
            Extensions.CollabUtils_MiniHeart = module.GetType().Module.GetType("Celeste.Mod.CollabUtils2.Entities.MiniHeart", ignoreCase: false, throwOnError: true);
            Extensions.CollabUtilsLoaded = true;
        };
        // Used for registering custom playerstates for display in CelesteTAS
        meta = new EverestModuleMetadata { Name = "CelesteTAS", VersionString = "3.4.5" };
        optionalDepLoaders[meta] = module =>
        {
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
        foreach (EverestModuleMetadata dep in optionalDepLoaders.Keys)
        {
            if (Extensions.TryGetModule(dep, out EverestModule module))
            {
                LoadDependency(module, optionalDepLoaders[dep]);
            }
        }
    }

    private static void OnRegisterModule(EverestModule module)
    {
        foreach (EverestModuleMetadata dep in optionalDepLoaders.Keys)
        {
            if (Extensions.SatisfiesDependency(dep, module.Metadata))
            {
                LoadDependency(module, optionalDepLoaders[dep]);
                return;
            }
        }
    }

    private static void LoadDependency(EverestModule module, Action<EverestModule> loader)
    {
        try
        {
            loader.Invoke(module);
        }
        catch (Exception e)
        {
            Util.Log(LogLevel.Error, "Failed loading optional dependency: " + module.Metadata.Name);
            Console.WriteLine(e.ToString());
            // Show something on screen to alert user
            failedLoadingDeps = true;
        }
    }
}
