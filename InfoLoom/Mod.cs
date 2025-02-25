
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using HarmonyLib;
using InfoLoomTwo.Systems;
using System.Linq;
using Game.UI.InGame;
using InfoLoomTwo.Systems.CommercialSystems.CommercialDemandData;
using InfoLoomTwo.Systems.CommercialSystems.CommercialProductData;
using InfoLoomTwo.Systems.DemographicsData;
using InfoLoomTwo.Systems.DemographicsData.Demographics;
using InfoLoomTwo.Systems.DistrictData;
using InfoLoomTwo.Systems.IndustrialSystems.IndustrialDemandData;
using InfoLoomTwo.Systems.ResidentialData;
using InfoLoomTwo.Systems.WorkforceData;
using InfoLoomTwo.Systems.WorkplacesData;
using InfoLoomTwo.Systems.IndustrialSystems.IndustrialProductData;
using InfoLoomTwo.Systems.TradeCostData;

// Mod namespace
namespace InfoLoomTwo
{
    // Mod class implementing IMod interface
    public class Mod : IMod
    {
        public static readonly string harmonyId = "Bruceyboy24804" + nameof(InfoLoomTwo);
        // Static fields and properties
       public static Setting setting;
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
            //Try to fetch the mod asset from the mod manager
            setting = new Setting(this);
            if (setting == null)
            {
                Log.Error("Failed to initialize settings.");
                return;
            }
            setting.RegisterInOptionsUI();
           AssetDatabase.global.LoadSettings(nameof(InfoLoomTwo), setting, new Setting(this));

             //Load localization
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
            
            updateSystem.UpdateAt<Demographics>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<WorkforceSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<WorkplacesSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<ResidentialSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<CommercialSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<IndustrialSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<CommercialProductsSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<IndustrialProductsSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<TradeCostSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<DistrictDataSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<InfoLoomUISystem>(SystemUpdatePhase.UIUpdate);
            
            

            
            
            
           
        }

        // Method that runs when the mod is disposed of
        public void OnDispose()
        {
            //Log entry for debugging purposes
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
