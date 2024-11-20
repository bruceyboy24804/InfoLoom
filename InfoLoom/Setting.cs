﻿using Colossal;
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

namespace InfoLoomTwo
{
    
    [FileLocation(nameof(InfoLoomTwo))]
    [SettingsUIGroupOrder(CommercialTax, Other)]
    [SettingsUIShowGroupName(CommercialTax, Other)]
    public class Setting : ModSetting
    {
        [ReadOnly]
        public DemandParameterData m_DemandParameters;
        public static World World { get; set; }
        public const string kSection = "Main";

        public const string CommercialTax = "Commercial Tax";
        public const string Other = "Other";

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

        [SettingsUISection(kSection, Other)]
        [SettingsUISlider(min = 100f, max = 200f, step = 1f)]
        
        public float AgeCapSetting { get; set; } = 100f;

        /*[SettingsUISection(kSection, IndustrialTax)]
        [SettingsUISlider(min = -5f, max = -10f, step = 0.1f, unit = Unit.kFloatThreeFractions)] // Slider range from -1.0 to 1.0 with a step of 0.1 [SettingsUISlider(min = -0.05, max = 1.0, step = 0.1)]
        public float TaxRateEffect2 { get; set; } = -5f; // Default value

        [SettingsUISection(kSection, IndustrialTax)]
        public string IndustrialTaxLEffect => TextMaker((Math.Abs(TaxRateEffect2) * 100).ToString("F2"), "industrial");*/


        public Setting(IMod mod) : base(mod)
        {
            SetDefaults();
        }

        public override void SetDefaults()
        {
            TaxRateEffect = -0.05f;
            AgeCapSetting = 100f;
           
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
                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Main" },
                { m_Setting.GetOptionGroupLocaleID(Setting.CommercialTax), "Commercial Tax" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.TaxRateEffect)), "Tax Rate Effect" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.TaxRateEffect)), "The base value that control the strength of the tax rate effect" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CommercialTaxLEffect)), " Tax rate" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CommercialTaxLEffect)), "The current total tax rate to determine the strength of commercial tax when taxing commercial product companies" },
                { m_Setting.GetOptionGroupLocaleID(Setting.Other), "Other" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.AgeCapSetting)), "Maximum chart age" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.AgeCapSetting)), "The maximum age that sets the demographic cap" },
               
                
            };
        }

        public void Unload()
        {
        }
    }
}
