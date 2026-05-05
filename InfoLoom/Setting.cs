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
    [SettingsUIGroupOrder(SectionsGroup, Data, DemandPanelResources, DemographicsGroup, EffectGroup, ChirpsGroup, ExportGroup, ExportWorkforceGroup, ExportDemographicsGroup, ExportWorkplacesGroup)]
    [SettingsUIShowGroupName(SectionsGroup, Data, DemandPanelResources, DemographicsGroup, EffectGroup, ChirpsGroup, ExportGroup, ExportWorkforceGroup, ExportDemographicsGroup, ExportWorkplacesGroup)]
    [SettingsUITabOrder(GeneralTab, CustomChirpsTab, ExportTab)]
    public class Setting : ModSetting
    {
        public const string SectionsGroup = "Sections";
        public const string ChirpsGroup = "Chirps";
        public const string GeneralTab = "General";
        public const string CustomChirpsTab = "Custom Chirps";
        public const string DemandPanelResources = "DemandPanelResources";
        public const string InfoviewChirps = "InfoviewChirps";
        public const string Data = "Data";
        public const string DemographicsGroup = "Demographics";
        public const string EffectGroup = "EffectGroup";
        public const string ExportTab = "Export";
        public const string ExportGroup = "ExportSettings";
        public const string ExportWorkforceGroup = "ExportWorkforce";
        public const string ExportDemographicsGroup = "ExportDemographics";
        public const string ExportWorkplacesGroup = "ExportWorkplaces";
        
        // ===== General Tab =====
        [SettingsUISection(GeneralTab, SectionsGroup)]
        public bool hideBuildingSection { get; set; }
        
        [SettingsUISection(GeneralTab, SectionsGroup)]
        public bool hideCitizenSection { get; set; }
        
        [SettingsUISection(GeneralTab, SectionsGroup)]
        public bool hideDistrictSection { get; set; }
        
        [SettingsUISection(GeneralTab, SectionsGroup)]
        public bool hideRentSection { get; set; }
        [SettingsUISection(GeneralTab, EffectGroup)]
        public bool showEffectsButton { get; set; }

        [SettingsUIHidden]
        public int crimeColorR { get; set; }
        [SettingsUIHidden]
        public int crimeColorG { get; set; }
        [SettingsUIHidden]
        public int crimeColorB { get; set; }
        [SettingsUIHidden]
        public int crimeColorA { get; set; }
        [SettingsUIHidden]
        public int wellbeingColorR { get; set; }
        [SettingsUIHidden]
        public int wellbeingColorG { get; set; }
        [SettingsUIHidden]
        public int wellbeingColorB { get; set; }
        [SettingsUIHidden]
        public int wellbeingColorA { get; set; }
        [SettingsUIHidden]
        public int healthColorR { get; set; }
        [SettingsUIHidden]
        public int healthColorG { get; set; }
        [SettingsUIHidden]
        public int healthColorB { get; set; }
        [SettingsUIHidden]
        public int healthColorA { get; set; }
        [SettingsUIHidden]
        public int fireHazardColorR { get; set; }
        [SettingsUIHidden]
        public int fireHazardColorG { get; set; }
        [SettingsUIHidden]
        public int fireHazardColorB { get; set; }
        [SettingsUIHidden]
        public int fireHazardColorA { get; set; }
        [SettingsUIHidden]
        public int fireResponseColorR { get; set; }
        [SettingsUIHidden]
        public int fireResponseColorG { get; set; }
        [SettingsUIHidden]
        public int fireResponseColorB { get; set; }
        [SettingsUIHidden]
        public int fireResponseColorA { get; set; }
        [SettingsUISection(GeneralTab, DemandPanelResources)]
        [SettingsUISlider(min = 0f, max = 100f, step = 1f, unit = Unit.kInteger)]
        public int comResDemValue { get; set; }
        
        [SettingsUISection(GeneralTab, DemandPanelResources)]
        [SettingsUISlider(min = 0f, max = 100f, step = 1f, unit = Unit.kInteger)]
        public int indResDemValue { get; set; }
        
        // ===== Demographics Age Limits =====
        [SettingsUISection(GeneralTab, DemographicsGroup)]
        [SettingsUISlider(min = 1, max = 50, step = 1, unit = Unit.kInteger)]
        public int teenAgeLimit { get; set; }
        
        [SettingsUISection(GeneralTab, DemographicsGroup)]
        [SettingsUISlider(min = 1, max = 100, step = 1, unit = Unit.kInteger)]
        public int adultAgeLimit { get; set; }
        
        [SettingsUISection(GeneralTab, DemographicsGroup)]
        [SettingsUISlider(min = 1, max = 150, step = 1, unit = Unit.kInteger)]
        public int elderAgeLimit { get; set; }
        
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
        

        [SettingsUIHidden]
        public float2 panelPosition { get; set; }
        
        // ===== Export Tab – Shared Settings =====
        [SettingsUISection(ExportTab, ExportGroup)]
        [SettingsUIMultilineText]
        public string ExportOutputPathDisplay => string.Empty;

        [SettingsUISection(ExportTab, ExportGroup)]
        [SettingsUISlider(min = 1f, max = 20f, step = 1f, unit = Unit.kInteger)]
        public int exportFilesRetentionCount { get; set; }

        [SettingsUISection(ExportTab, ExportGroup)]
        public bool exportReplaceExisting { get; set; }

        [SettingsUIButton]
        [SettingsUISection(ExportTab, ExportGroup)]
        public bool ExportAllButton
        {
            set => InfoLoomTwo.Exporter.DataExporter.ExportAll();
        }

        // ===== Export Tab – Workforce =====
        [SettingsUISection(ExportTab, ExportWorkforceGroup)]
        public bool exportWorkforce { get; set; }

        /// <summary>When true, ignores the current district selection in the UI and always exports city-wide workforce data.</summary>
        [SettingsUISection(ExportTab, ExportWorkforceGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsWorkforceExportDisabled))]
        public bool exportWorkforceCityWide { get; set; }

        [SettingsUIButton]
        [SettingsUISection(ExportTab, ExportWorkforceGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsWorkforceExportDisabled))]
        public bool ExportWorkforceButton
        {
            set => InfoLoomTwo.Exporter.DataExporter.ExportWorkforce();
        }

        // ===== Export Tab – Demographics =====
        [SettingsUISection(ExportTab, ExportDemographicsGroup)]
        public bool exportDemographics { get; set; }

        /// <summary>Export per-year (single-age) detail CSV — mirrors UI's finest-grained view.</summary>
        [SettingsUISection(ExportTab, ExportDemographicsGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsDemographicsExportDisabled))]
        public bool exportDemoPerAge { get; set; }

        /// <summary>Export 5-year grouped CSV — mirrors UI's Five-Year grouping strategy.</summary>
        [SettingsUISection(ExportTab, ExportDemographicsGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsDemographicsExportDisabled))]
        public bool exportDemoFiveYear { get; set; }

        /// <summary>Export 10-year grouped CSV — mirrors UI's Ten-Year grouping strategy.</summary>
        [SettingsUISection(ExportTab, ExportDemographicsGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsDemographicsExportDisabled))]
        public bool exportDemoTenYear { get; set; }

        /// <summary>Export lifecycle (Child/Teen/Adult/Elderly) grouped CSV — mirrors UI's Lifecycle grouping.</summary>
        [SettingsUISection(ExportTab, ExportDemographicsGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsDemographicsExportDisabled))]
        public bool exportDemoLifecycle { get; set; }

        /// <summary>Export city-wide totals CSV (AllCitizens, Workers, Tourists, etc.).</summary>
        [SettingsUISection(ExportTab, ExportDemographicsGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsDemographicsExportDisabled))]
        public bool exportDemoTotals { get; set; }

        /// <summary>When true, ignores the current district selection and always exports city-wide data.</summary>
        [SettingsUISection(ExportTab, ExportDemographicsGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsDemographicsExportDisabled))]
        public bool exportDemoCityWide { get; set; }

        /// <summary>Include employment columns — Work, School1-4, Unemployed, Retired (mirrors Employment chart type).</summary>
        [SettingsUISection(ExportTab, ExportDemographicsGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsDemographicsExportDisabled))]
        public bool exportDemoEmploymentCols { get; set; }

        /// <summary>Include education columns — Uneducated, PoorlyEducated, Educated, WellEducated, HighlyEducated (mirrors Education chart type).</summary>
        [SettingsUISection(ExportTab, ExportDemographicsGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsDemographicsExportDisabled))]
        public bool exportDemoEducationCols { get; set; }

        [SettingsUIButton]
        [SettingsUISection(ExportTab, ExportDemographicsGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsDemographicsExportDisabled))]
        public bool ExportDemographicsButton
        {
            set => InfoLoomTwo.Exporter.DataExporter.ExportDemographics();
        }

        // ===== Export Tab – Workplaces =====
        [SettingsUISection(ExportTab, ExportWorkplacesGroup)]
        public bool exportWorkplaces { get; set; }

        /// <summary>When true, ignores the current district selection in the UI and always exports city-wide workplaces data.</summary>
        [SettingsUISection(ExportTab, ExportWorkplacesGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsWorkplacesExportDisabled))]
        public bool exportWorkplacesCityWide { get; set; }

        [SettingsUIButton]
        [SettingsUISection(ExportTab, ExportWorkplacesGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(IsWorkplacesExportDisabled))]
        public bool ExportWorkplacesButton
        {
            set => InfoLoomTwo.Exporter.DataExporter.ExportWorkplaces();
        }

        public bool IsWorkforceExportDisabled() => !exportWorkforce;
        public bool IsDemographicsExportDisabled() => !exportDemographics;
        public bool IsWorkplacesExportDisabled() => !exportWorkplaces;
        
        public Setting(IMod mod) : base(mod)
        {
            SetDefaults();
        }

        public override void SetDefaults()
        {
            hideBuildingSection = false;
            hideCitizenSection = false;
            hideDistrictSection = false;
            hideRentSection = false;
            showEffectsButton = true;
            crimeColorR = 255; crimeColorG = 50; crimeColorB = 50; crimeColorA = 230;
            wellbeingColorR = 50; wellbeingColorG = 200; wellbeingColorB = 50; wellbeingColorA = 230;
            healthColorR = 50; healthColorG = 150; healthColorB = 255; healthColorA = 230;
            fireHazardColorR = 255; fireHazardColorG = 128; fireHazardColorB = 0; fireHazardColorA = 230;
            fireResponseColorR = 255; fireResponseColorG = 200; fireResponseColorB = 0; fireResponseColorA = 230;
            comResDemValue = 100;
            indResDemValue = 100;
            
            // Default age limits matching vanilla game
            teenAgeLimit = 21;
            adultAgeLimit = 36;
            elderAgeLimit = 84;
            
            enableUnemploymentChirps = true;
            enableUnderemploymentChirps = true;
            enableHomelessChirps = true;
            enableDemandChirps = true;
            
            unemploymentThreshold = 50f;
            underemploymentThreshold = 5f;
            homelessThreshold = 2f;
            
            enableElectrictyChirps = true;
            enableWaterAndSweageChirps = true;
            panelPosition= new float2(0.5f, 0.5f);
            
            exportFilesRetentionCount = 5;
            exportReplaceExisting = false;
            exportWorkforce = true;
            exportWorkforceCityWide = true;
            exportDemographics = true;
            exportDemoPerAge = true;
            exportDemoFiveYear = false;
            exportDemoTenYear = false;
            exportDemoLifecycle = false;
            exportDemoTotals = false;
            exportDemoCityWide = true;
            exportDemoEmploymentCols = true;
            exportDemoEducationCols = true;
            exportWorkplaces = true;
            exportWorkplacesCityWide = true;
            
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
                { m_Setting.GetOptionGroupLocaleID(Setting.DemandPanelResources), "Production Panel Resources" },
                { m_Setting.GetOptionGroupLocaleID(Setting.SectionsGroup), "Sections" },
                { m_Setting.GetOptionGroupLocaleID(Setting.ChirpsGroup), "Chirp Settings" },
                { m_Setting.GetOptionGroupLocaleID(Setting.Data), "Data" },
                { m_Setting.GetOptionGroupLocaleID(Setting.DemographicsGroup), "Demographics Age Limits" },
                { m_Setting.GetOptionGroupLocaleID(Setting.EffectGroup), "Effect Settings" },
                
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hideBuildingSection)), "Hide Building Section"  },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hideBuildingSection)), "Toggle to hide the Building Section" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hideCitizenSection)), "Hide Citizen Section"  },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hideCitizenSection)), "Toggle to hide the Citizen Section" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hideDistrictSection)), "Hide District Section"  },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hideDistrictSection)), "Toggle to hide the District Section" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hideRentSection)), "Hide Rent Section"  },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hideRentSection)), "Toggle to hide the Rent Section" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.showEffectsButton)), "Show Effects Button"  },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.showEffectsButton)), "Toggle to show the Effects Button"},
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
                
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.teenAgeLimit)), "Teen Age Limit (days)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.teenAgeLimit)), "Age in days when children become teens. Match this with your aging mod settings." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.adultAgeLimit)), "Adult Age Limit (days)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.adultAgeLimit)), "Age in days when teens become adults. Match this with your aging mod settings." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.elderAgeLimit)), "Elder Age Limit (days)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.elderAgeLimit)), "Age in days when adults become elders. Match this with your aging mod settings." },
                
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.enableElectrictyChirps)), "Enable Electricity Chirps"  },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.enableElectrictyChirps)), "Receive chirps when when you are consuming less electricity than you are producing" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.enableWaterAndSweageChirps)), "Enable Water and Sewage Chirps"  },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.enableWaterAndSweageChirps)), "Receive chirps when when you are consuming less water than you are producing" },
                
                // Export tab
                { m_Setting.GetOptionTabLocaleID(Setting.ExportTab), "Export" },
                { m_Setting.GetOptionGroupLocaleID(Setting.ExportGroup), "General" },
                { m_Setting.GetOptionGroupLocaleID(Setting.ExportWorkforceGroup), "Workforce" },
                { m_Setting.GetOptionGroupLocaleID(Setting.ExportDemographicsGroup), "Demographics" },
                { m_Setting.GetOptionGroupLocaleID(Setting.ExportWorkplacesGroup), "Workplaces" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ExportOutputPathDisplay)), "Output Folder:\n" + System.IO.Path.Combine(Colossal.PSI.Environment.EnvPath.kUserDataPath, "ModsData", "InfoLoomTwo") },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.exportFilesRetentionCount)), "Files to Keep Per Data Type" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.exportFilesRetentionCount)), "Maximum number of CSV files to retain for each data type. Oldest are deleted first." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.exportReplaceExisting)), "Replace Existing File" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.exportReplaceExisting)), "When enabled, overwrites a single file per data type (e.g. workforce.csv) instead of creating a new timestamped file each export." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ExportAllButton)), "Export All Now" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ExportAllButton)), "Export all enabled data types immediately." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.exportWorkforce)), "Include in Export" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.exportWorkforce)), "Include workforce data when exporting." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.exportWorkforceCityWide)), "Always Export City-Wide" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.exportWorkforceCityWide)), "When enabled, the export always uses city-wide workforce data regardless of which district is selected in the Workforce UI. When disabled, the export reflects whichever district (or city-wide) is currently active." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ExportWorkforceButton)), "Export Workforce Now" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ExportWorkforceButton)), "Export workforce CSV immediately." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.exportDemographics)), "Include in Export" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.exportDemographics)), "Include demographics data when exporting. All enabled sections below are written into a single demographics.csv, identified by the 'grouping' column." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.exportDemoPerAge)), "Include Per-Age Rows (1-year groups)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.exportDemoPerAge)), "Adds 120 rows (one per year of age, 0-119) to the demographics CSV. Matches the finest-grained view available in the Demographics UI." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.exportDemoFiveYear)), "Include 5-Year Group Rows" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.exportDemoFiveYear)), "Adds rows for each 5-year age band to the demographics CSV. Matches the Five-Year grouping in the Demographics UI." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.exportDemoTenYear)), "Include 10-Year Group Rows" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.exportDemoTenYear)), "Adds rows for each 10-year age band to the demographics CSV. Matches the Ten-Year grouping in the Demographics UI." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.exportDemoLifecycle)), "Include Lifecycle Group Rows (Child/Teen/Adult/Elderly)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.exportDemoLifecycle)), "Adds four rows (Child, Teen, Adult, Elderly) to the demographics CSV. Matches the Lifecycle grouping in the Demographics UI." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.exportDemoTotals)), "Include City Totals Rows" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.exportDemoTotals)), "Adds city-wide summary rows to the demographics CSV: AllCitizens, Workers, Tourists, Commuters, Students, and more." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.exportDemoCityWide)), "Always Export City-Wide" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.exportDemoCityWide)), "When enabled, the export always uses city-wide data regardless of which district is selected in the Demographics UI. When disabled, the export reflects whichever district (or city-wide) is currently active in the UI." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.exportDemoEmploymentCols)), "Include Employment Columns" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.exportDemoEmploymentCols)), "Adds Work, School (Elementary/High/College/University), Unemployed, and Retired columns to each row. Corresponds to the Employment chart type in the Demographics UI." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.exportDemoEducationCols)), "Include Education Columns" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.exportDemoEducationCols)), "Adds Uneducated, Poorly Educated, Educated, Well Educated, and Highly Educated columns to each row. Corresponds to the Education chart type in the Demographics UI." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ExportDemographicsButton)), "Export Demographics Now" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ExportDemographicsButton)), "Writes all enabled demographics sections into a single demographics.csv immediately." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.exportWorkplaces)), "Include in Export" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.exportWorkplaces)), "Include workplaces data when exporting." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.exportWorkplacesCityWide)), "Always Export City-Wide" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.exportWorkplacesCityWide)), "When enabled, the export always uses city-wide workplaces data regardless of which district is selected in the Workplaces UI. When disabled, the export reflects whichever district (or city-wide) is currently active." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ExportWorkplacesButton)), "Export Workplaces Now" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ExportWorkplacesButton)), "Export workplaces CSV immediately." },
            };
        }

        public void Unload() { }
    }
}