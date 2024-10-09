// Organized imports based on usage groups
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Game.Settings;
using HarmonyLib;
using InfoLoomBrucey.Systems;
using System.Linq;
using System.Reflection;
using UnityEngine;


namespace InfoLoomBrucey
{
    // Mod class implementing IMod interface
    public class Mod : IMod
    {
       
        // Static fields and properties
        public static Setting setting;
        public static readonly string Id = "InfoLoom";

        public static Mod Instance { get; private set; }
        public static ExecutableAsset modAsset { get; private set; }
        internal ILog Log { get; private set; }

        // Static logger instance with custom logger name and settings
        public static ILog log = LogManager.GetLogger($"{nameof(InfoLoom)}.{nameof(Mod)}")
            .SetShowsErrorsInUI(false);

        

        // Method that runs when the mod is loaded
        public void OnLoad(UpdateSystem updateSystem)
        {
            // Log entry for debugging purposes
            log.Info(nameof(OnLoad));

            // Try to fetch the mod asset from the mod manager
            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
            {
                log.Info($"Current mod asset at {asset.path}");
                modAsset = asset;
            }
           
          
            

            // Register custom update systems for UI updates
            updateSystem.UpdateAt<PopulationStructureUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<WorkforceInfoLoomUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<WorkplacesInfoLoomUISystem>(SystemUpdatePhase.UIUpdate);
           
        }

        // Method that runs when the mod is disposed of
        public void OnDispose()
        {
            // Log entry for debugging purposes
            log.Info(nameof(OnDispose));
          

        }
    }
}
