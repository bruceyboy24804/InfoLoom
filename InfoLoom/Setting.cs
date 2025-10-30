using System;
using System.Linq;
using Colossal;
using Colossal.IO.AssetDatabase;
using Game;
using Game.Modding;
using Game.Settings;
using Game.UI;
using System.Collections.Generic;
using Game.SceneFlow;
using InfoLoomTwo.Bridge;
using InfoLoomTwo.Systems;
using InfoLoomTwo.Systems.DemographicsData;
using Unity.Entities;
using Unity.Mathematics;

namespace InfoLoomTwo
{
    [FileLocation(nameof(InfoLoomTwo))]
    [SettingsUIGroupOrder(SectionsGroup, Data, DemandPanelResources ,ChirpsGroup)]
    [SettingsUIShowGroupName(SectionsGroup, Data, DemandPanelResources ,ChirpsGroup)]
    [SettingsUITabOrder(GeneralTab, CustomChirpsTab)]
    public class Setting : ModSetting
    {
        public const string SectionsGroup = "Sections";
        public const string ChirpsGroup = "Chirps";
        public const string GeneralTab = "General";
        public const string CustomChirpsTab = "Custom Chirps";
        public const string DemandPanelResources = "DemandPanelResources";
        public const string InfoviewChirps = "InfoviewChirps";
        public const string Data = "Data";
        public bool data;
        // ===== General Tab =====
        [SettingsUISection(GeneralTab, SectionsGroup)]
        public bool hideBuildingSection { get; set; }
        
        [SettingsUISection(GeneralTab, SectionsGroup)]
        public bool hideCitizenSection { get; set; }
        
        [SettingsUISection(GeneralTab, SectionsGroup)]
        public bool hideProfitSection { get; set; }
        
        [SettingsUISection(GeneralTab, SectionsGroup)]
        public bool hideDistrictSection { get; set; }
        
        [SettingsUISection(GeneralTab, SectionsGroup)]
        public bool hideRentSection { get; set; }
        [SettingsUISection(GeneralTab, DemandPanelResources)]
        [SettingsUISlider(min = 0f, max = 100f, step = 1f, unit = Unit.kInteger)]
        public int comResDemValue { get; set; }
        
        [SettingsUISection(GeneralTab, DemandPanelResources)]
        [SettingsUISlider(min = 0f, max = 100f, step = 1f, unit = Unit.kInteger)]
        public int indResDemValue { get; set; }
        
        // ===== Custom Chirps Tab =====
        // These settings are hidden if CustomChirps mod is NOT detected
        
        [SettingsUIHideByCondition(typeof(CustomChirpsDetector), nameof(CustomChirpsDetector.IsNotInstalled))]
        [SettingsUISection(CustomChirpsTab, ChirpsGroup)]
        public bool enableUnemploymentChirps { get; set; }

        [SettingsUIHideByCondition(typeof(CustomChirpsDetector), nameof(CustomChirpsDetector.IsNotInstalled))]
        [SettingsUISection(CustomChirpsTab, ChirpsGroup)]
        public bool enableUnderemploymentChirps { get; set; }
        [SettingsUIHideByCondition(typeof(CustomChirpsDetector), nameof(CustomChirpsDetector.IsNotInstalled))]
        [SettingsUISection(CustomChirpsTab, ChirpsGroup)]
        public bool enableDemandChirps { get; set; }
        
        [SettingsUIHideByCondition(typeof(CustomChirpsDetector), nameof(CustomChirpsDetector.IsNotInstalled))]
        [SettingsUISection(CustomChirpsTab, ChirpsGroup)]
        public bool enableHomelessChirps { get; set; }
        
        [SettingsUIHideByCondition(typeof(CustomChirpsDetector), nameof(CustomChirpsDetector.IsNotInstalled))]
        [SettingsUISection(CustomChirpsTab, ChirpsGroup)]
        [SettingsUISlider(min = 0f, max = 100f, step = 0.5f, unit = Unit.kPercentageSingleFraction)]
        public float unemploymentThreshold { get; set; }

        [SettingsUIHideByCondition(typeof(CustomChirpsDetector), nameof(CustomChirpsDetector.IsNotInstalled))]
        [SettingsUISection(CustomChirpsTab, ChirpsGroup)]
        [SettingsUISlider(min = 0f, max = 100f, step = 0.5f, unit = Unit.kPercentageSingleFraction)]
        public float underemploymentThreshold { get; set; }

        [SettingsUIHideByCondition(typeof(CustomChirpsDetector), nameof(CustomChirpsDetector.IsNotInstalled))]
        [SettingsUISection(CustomChirpsTab, ChirpsGroup)]
        [SettingsUISlider(min = 0f, max = 100f, step = 0.5f, unit = Unit.kPercentageSingleFraction)]
        public float homelessThreshold { get; set; }
        
        [SettingsUIHideByCondition(typeof(CustomChirpsDetector), nameof(CustomChirpsDetector.IsNotInstalled))]
        [SettingsUISection(CustomChirpsTab, InfoviewChirps)]
        public bool enableElectrictyChirps { get; set; }
        [SettingsUIHideByCondition(typeof(CustomChirpsDetector), nameof(CustomChirpsDetector.IsNotInstalled))]
        [SettingsUISection(CustomChirpsTab, InfoviewChirps)]
        public bool enableWaterAndSweageChirps { get; set; }
        

        
        
        public Setting(IMod mod) : base(mod)
        {
            SetDefaults();
        }

        public override void SetDefaults()
        {
            hideBuildingSection = false;
            hideCitizenSection = false;
            hideProfitSection = false;
            hideDistrictSection = false;
            hideRentSection = false;
            comResDemValue = 100;
            indResDemValue = 100;
            
            enableUnemploymentChirps = true;
            enableUnderemploymentChirps = true;
            enableHomelessChirps = true;
            enableDemandChirps = true;
            
            unemploymentThreshold = 50f;
            underemploymentThreshold = 5f;
            homelessThreshold = 2f;
            
            enableElectrictyChirps = true;
            enableWaterAndSweageChirps = true;
            
        }
    }
    
    public static class CustomChirpsDetector
    {
        private static bool? _isInstalled = null;
        public static bool IsNotInstalled()
        {
            if (!_isInstalled.HasValue)
            {
                _isInstalled = DetectCustomChirps();
            }
            
            return !_isInstalled.Value;
        }
        public static bool IsInstalled()
        {
            if (!_isInstalled.HasValue)
            {
                _isInstalled = DetectCustomChirps();
            }
            
            return _isInstalled.Value;
        }
        private static bool DetectCustomChirps()
        {
            try
            {
                foreach (var modInfo in GameManager.instance.modManager)
                {
                    string modName = modInfo.asset.name;
                    if (modName.Contains("CustomChirps"))
                    {
                        Mod.log.Debug($"Found: {modName} showing custom chirp settings");
                        return true;
                    }
                }
                Mod.log.Debug("CustomChirps not found - chirp settings will be hidden");
                return false;
            }
            catch (Exception ex)
            {
                Mod.log.Error($"Error detecting CustomChirps: {ex.Message}");
                return false;
            }
        }
    }

    public class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;

        public LocaleEN(Setting setting)
        {
            m_Setting = setting;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "Info Loom" },
                { m_Setting.GetOptionTabLocaleID(Setting.GeneralTab), "General" },
                { m_Setting.GetOptionTabLocaleID(Setting.CustomChirpsTab), "Custom Chirps" },
                { m_Setting.GetOptionTabLocaleID(Setting.DemandPanelResources), "Production Panel Resources" },
                { m_Setting.GetOptionGroupLocaleID(Setting.SectionsGroup), "Sections" },
                { m_Setting.GetOptionGroupLocaleID(Setting.ChirpsGroup), "Chirp Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.Data), "Data" },
                
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hideBuildingSection)), "Hide Building Section"  },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hideBuildingSection)), "Toggle to hide the Building Section" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hideCitizenSection)), "Hide Citizen Section"  },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hideCitizenSection)), "Toggle to hide the Citizen Section" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hideProfitSection)), "Hide Profit Section"  },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hideProfitSection)), "Toggle to hide the Profit Section" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hideDistrictSection)), "Hide District Section"  },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hideDistrictSection)), "Toggle to hide the District Section" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hideRentSection)), "Hide Rent Section"  },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hideRentSection)), "Toggle to hide the Rent Section" },
                
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.enableUnemploymentChirps)), "Enable Unemployment Chirps"  },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.enableUnemploymentChirps)), "Receive chirps when unemployment exceeds threshold" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.enableUnderemploymentChirps)), "Enable Underemployment Chirps"  },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.enableUnderemploymentChirps)), "Receive chirps when underemployment exceeds threshold" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.enableHomelessChirps)), "Enable Homeless Chirps"  },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.enableHomelessChirps)), "Receive chirps when homelessness exceeds threshold" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.enableDemandChirps)), "Enable Commercial Demand Chirps"  },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.enableDemandChirps)), "Receive chirps when there is demand for resources" },
                
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.unemploymentThreshold)), "Unemployment Threshold"  },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.unemploymentThreshold)), "Post chirp when unemployment exceeds this percentage" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.underemploymentThreshold)), "Underemployment Threshold"  },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.underemploymentThreshold)), "Post chirp when underemployment exceeds this percentage" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.homelessThreshold)), "Homeless Threshold"  },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.homelessThreshold)), "Post chirp when homelessness exceeds this percentage" },
                
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.comResDemValue)), "Commercial Resource Demand"  },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.comResDemValue)), "Value which to show resources in the commercial demand panel" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.indResDemValue)), "Industrial Resource Demand"  },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.indResDemValue)), "Value which to show resources in the industrial demand panel" },
                
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.enableElectrictyChirps)), "Enable Electricity Chirps"  },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.enableElectrictyChirps)), "Receive chirps when when you are consuming less electricity than you are producing" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.enableWaterAndSweageChirps)), "Enable Water and Sewage Chirps"  },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.enableWaterAndSweageChirps)), "Receive chirps when when you are consuming less water than you are producing" },
            };
        }

        public void Unload() { }
    }
}