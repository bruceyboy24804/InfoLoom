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
        public static readonly string harmonyId = "Bruceyboy24804." + nameof(InfoLoom);
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
                log.Info($"Current mod asset at {asset.path}");

            // Initialize settings and register them in the options UI
            setting = new Setting(this);
            setting.RegisterInOptionsUI();
            setting._Hidden = false;

            // Add localization source for English US
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(setting));

            var harmony = new Harmony(harmonyId);
            harmony.PatchAll(typeof(Mod).Assembly);
            var patchedMethods = harmony.GetPatchedMethods().ToArray();
            log.Info($"Plugin {harmonyId} made patches! Patched methods: " + patchedMethods.Length);
            foreach (var patchedMethod in patchedMethods)
            {
                log.Info($"Patched method: {patchedMethod.Module.Name}:{patchedMethod.DeclaringType.Name}.{patchedMethod.Name}");
            }


            // Load settings from the global asset database
            AssetDatabase.global.LoadSettings(nameof(InfoLoom), setting, new Setting(this));

            // Register custom update systems for UI updates
            updateSystem.UpdateAt<PopulationStructureUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<WorkforceInfoLoomUISystem>(SystemUpdatePhase.UIUpdate);


            if (!setting.SeparateConsumption)
            {
                MethodBase mb = typeof(Game.UI.InGame.ProductionUISystem).GetMethod("GetData", BindingFlags.NonPublic | BindingFlags.Instance);
                if (mb != null)
                {
                    log.Info($"Removing {mb.Name} patch from {harmonyId} because Separate Consumption is disabled.");
                    harmony.Unpatch(mb, HarmonyPatchType.Prefix, harmonyId);
                }
                else
                    log.Warn("Cannot remove GetData patch.");
            }
        }

        // Method that runs when the mod is disposed of
        public void OnDispose()
        {
            // Log entry for debugging purposes
            log.Info(nameof(OnDispose));

            // Unregister settings from the options UI if they exist
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
