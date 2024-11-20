// Organized imports based on usage groups
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.Net;
using Game.SceneFlow;
using Game.Settings;
using Game.Simulation;
using HarmonyLib;
using InfoLoomTwo.Systems;
using System.Linq;

// Mod namespace
namespace InfoLoomTwo
{
    // Mod class implementing IMod interface
    public class Mod : IMod
    {
        public static readonly string harmonyId = "Bruceyboy24804" + nameof(InfoLoomTwo);
        // Static fields and properties
        public static Setting setting;
        public static readonly string Id = "InfoLoomTwo";
        public static Mod Instance { get; private set; }
     
        public static ExecutableAsset modAsset { get; private set; }    
        internal ILog Log { get; private set; }

        // Static logger instance with custom logger name and settings
        public static ILog log = LogManager.GetLogger($"{nameof(InfoLoomTwo)}.{nameof(Mod)}")
            .SetShowsErrorsInUI(false);

        // Method that runs when the mod is loaded
        public void OnLoad(UpdateSystem updateSystem)
        {
            // Log entry for debugging purposes
            log.Info(nameof(OnLoad));
            Instance = this;
            // Try to fetch the mod asset from the mod manager
            setting = new Setting(this);
            if (setting == null)
            {
                Log.Error("Failed to initialize settings.");
                return;
            }
            setting.RegisterInOptionsUI();
           AssetDatabase.global.LoadSettings(nameof(InfoLoomTwo), setting, new Setting(this));

            // Load localization
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(setting));

            var harmony = new Harmony(harmonyId);
            harmony.PatchAll(typeof(Mod).Assembly);
            var patchedMethods = harmony.GetPatchedMethods().ToArray();
            log.Info($"Plugin {harmonyId} made patches! Patched methods: " + patchedMethods.Length);
            foreach (var patchedMethod in patchedMethods)
            {
                log.Info($"Patched method: {patchedMethod.Module.Name}:{patchedMethod.DeclaringType.Name}.{patchedMethod.Name}");
            }
          

            // Register custom update systems for UI updates
            updateSystem.UpdateAt<PopulationStructureUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<WorkforceInfoLoomUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<WorkplacesInfoLoomUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<ResidentialDemandUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<CommercialDemandUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<BuildingDemandUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<IndustrialDemandUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<CommercialUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<CustomCommercialDemandSystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<CustomIndustrialDemandSystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<IndustrialUISystem>(SystemUpdatePhase.UIUpdate);
            
            
           
        }

        // Method that runs when the mod is disposed of
        public void OnDispose()
        {
            // Log entry for debugging purposes
            log.Info(nameof(OnDispose));
            if (setting != null)
            {
                setting.UnregisterInOptionsUI();
                setting = null;
            }

            var harmony = new Harmony(harmonyId);
            harmony.UnpatchAll(harmonyId);

        }
    }
}
