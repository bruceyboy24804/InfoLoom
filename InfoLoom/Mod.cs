using Colossal;
using Game;
using Game.Modding;
using Game.SceneFlow;
using InfoLoomTwo.Extensions;
using InfoLoomTwo.Systems;
using InfoLoomTwo.Systems.ResidentialData;
using InfoLoomTwo.Systems.CommercialSystems.CommercialCompanyDebugData;
using InfoLoomTwo.Systems.CommercialSystems.CommercialDemandData;
using InfoLoomTwo.Systems.DemographicsData;
using InfoLoomTwo.Systems.IndustrialSystems.IndustrialCompanyData;
using InfoLoomTwo.Systems.IndustrialSystems.IndustrialDemandData;
using InfoLoomTwo.Systems.WorkforceData;
using InfoLoomTwo.Systems.WorkplacesData;
using InfoLoomTwo.Systems.Sections;
using InfoLoomTwo.Systems.TradeCostData;
using InfoLoomTwo.Systems.ChirpSystem_s_;
using InfoLoomTwo.Systems.IndustrialSystems.StorageCompanies.Systems;
using InfoLoomTwo.Systems.SankeyUISystems;
using InfoLoomTwo.Exporter;
using Colossal.Logging;
using Unity.Entities;
using Game.Settings;
using InfoLoomTwo.Systems.UI;
using ModsCommon.Mod;

namespace InfoLoomTwo
{
    public sealed class InfoLoomMod : ModsCommonBase<InfoLoomMod>, IMod
    {
        // Static accessors kept for backward compatibility with all callers across the codebase.
        public static Setting setting => Instance?.Settings as Setting;
        public static ILog log => Instance?.Log;


        #region BruceyModBase abstract members

        public override string ModName => nameof(InfoLoomTwo);
        public override string Id => nameof(InfoLoomTwo);
        protected override string UiHostPrefix => "il";

        protected override ModSetting CreateSettings(IMod mod) => new Setting(mod);

        protected override IDictionarySource CreateEnUsLocalization(ModSetting settings)
            => new LocaleEN((Setting)settings);

        protected override void RegisterSystems(UpdateSystem updateSystem)
        {
            updateSystem.UpdateAt<Demographics>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<WorkforceSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<WorkplacesSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<ResidentialSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<CommercialSystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<IndustrialSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<TradeCostsSystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<InfoLoomChirpSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<EffectTrackerSystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<ILEffectsSection>(SystemUpdatePhase.Rendering);
            updateSystem.UpdateAt<CommercialCompanyDataSystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<InfoLoomUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<IndustrialCompanySystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<StoragePropertyCompanies>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<BudgetUISankeySystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<WorkforcePipelineSankeySystem>(SystemUpdatePhase.UIUpdate);

            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<ILCitizenSection>();
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<ILBuildingSection>();
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<ILRentSection>();
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<ILDistrictSection>();
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<ILEffectsSection>();
        }

        #endregion

        protected override void OnAfterLoad(UpdateSystem updateSystem)
        {
            // Load UI panel locale strings (en-US and all other languages) from Locale.json.
            // These are separate from the settings strings handled by CreateEnUsLocalization.
            foreach (var item in new LocaleHelper("InfoLoomTwo.Locale.json").GetAvailableLanguages())
            {
                GameManager.instance.localizationManager.AddSource(item.LocaleId, item);
            }

            DataExporter.EnsureOutputDirectory();
        }
    }
}
