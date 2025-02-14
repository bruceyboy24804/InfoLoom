using Colossal;
using Colossal.IO.AssetDatabase;
using Colossal.IO.AssetDatabase.Internal;
using Game.Modding;
using Game.Prefabs;
using Game.Settings;
using Game.UI;
using Game.UI.InGame;
using Game.UI.Widgets;
using InfoLoomTwo.Systems;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using InfoLoomTwo.Domain;
//using InfoLoomTwo.Systems.CommercialSystems.CommercialDemandPatch;
using InfoLoomTwo.Systems.CommercialSystems.CommercialProductData;

namespace InfoLoomTwo
{
    
    [FileLocation(nameof(InfoLoomTwo))]
    [SettingsUIGroupOrder(CommercialTax, Other, CustomCommercialDemand)]
    [SettingsUIShowGroupName(CommercialTax, Other, CustomCommercialDemand)]
    public class Setting : ModSetting
    {
        
        
        
        
        [ReadOnly]
        public DemandParameterData m_DemandParameters;
        public static World World { get; set; }
        public const string kSection = "Main";

        public const string CommercialTax = "Commercial Tax";
        public const string Other = "Other";
        public const string CustomCommercialDemand = "Custom Commercial Demand";
        public bool m_FeatureCommercialDemand;
        

        private string TextMaker(string value, string type, string pop = null)
        {
            
            string unit = "";
            switch (type)
            {
                case "commercial":
                    unit = " %";
                    break;
                case "industrial":
                    unit = " %";
                    break;
                default:
                    unit = $" per {pop} citizens";
                    break;
            };
            return $"{value}{unit} per Increment of tax slider"; 
        }


        [SettingsUISection(kSection, CommercialTax)]
        [SettingsUISlider(min = -0.05f, max = -1.0f, step = 0.01f, unit = Unit.kFloatThreeFractions)] // Slider range from -1.0 to 1.0 with a step of 0.1 [SettingsUISlider(min = -0.05, max = 1.0, step = 0.1)]
        public float TaxRateEffect { get; set; } = -0.05f; // Default value

        [SettingsUISection(kSection, CommercialTax)]
        public string CommercialTaxLEffect => TextMaker((Math.Abs(TaxRateEffect) * 100).ToString("F2"), "commercial");

        /*[SettingsUISection(kSection, CustomCommercialDemand)]
        [SettingsUISetter(typeof(Setting), nameof(HideModdedCommercialDemandButton))]
        public bool FeatureCommercialDemand
        {
            get => m_FeatureCommercialDemand;
            set
            {
                m_FeatureCommercialDemand = value;
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<ModifiedCommercialDemandSystem>().Enabled = value;
            }
        }
        
        
        
        [SettingsUISection(kSection, CustomCommercialDemand)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(FeatureCommercialDemand), invert: true)]
        public bool OverrideLodgingDemand { get; set; }
        
        [SettingsUISection(kSection, CustomCommercialDemand)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(FeatureCommercialDemand), invert: true)]
        [SettingsUISlider(min = 0, max = 100, step = 1)]
        public int CustomLodgingDemandValue { get; set; }*/
        /// <summary>
        /// Gets or sets the saved panel position.
        /// </summary>
        [SettingsUIHidden]
        public PanelState[] PanelStates { get; set; }


        public Setting(IMod mod) : base(mod)
        {
            SetDefaults();
           
        }

        public override void SetDefaults()
        {
            TaxRateEffect = -0.05f;
            PanelStates = new PanelState[0];
            //FeatureCommercialDemand = false;
            
            //OverrideLodgingDemand = false;
            //CustomLodgingDemandValue = 0;
            
        }

        /*public void HideModdedCommercialDemandButton(bool value)
        {
            CommercialProductsUISystem commercialProductsUISystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<CommercialProductsUISystem>();
            commercialProductsUISystem.MCDButton = value;
        }*/
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
                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Main" },
                { m_Setting.GetOptionGroupLocaleID(Setting.CommercialTax), "Commercial Tax" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.TaxRateEffect)), "Tax Rate Effect" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.TaxRateEffect)), "The base value that control the strength of the tax rate effect" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CommercialTaxLEffect)), " Tax rate" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CommercialTaxLEffect)), "The current total tax rate to determine the strength of commercial tax when taxing commercial product companies" },
                { m_Setting.GetOptionGroupLocaleID(Setting.Other), "Other" },
                { m_Setting.GetOptionGroupLocaleID(Setting.CustomCommercialDemand), "Custom Commercial Demand" },
                /*{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.FeatureCommercialDemand)), "Enable Commercial Demand" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.FeatureCommercialDemand)), "Enables modded commercial demand and dedicated UI." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverrideLodgingDemand)), "Override Lodging Demand" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverrideLodgingDemand)), "Override lodging demand to be able to sey a custom value" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CustomLodgingDemandValue)), "Custom Lodging Demand" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CustomLodgingDemandValue)), "Set lodging demand to a custom value" },*/
                
               
                
            };
        }

        public void Unload()
        {
        }
    }
}
