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


namespace InfoLoomTwo
{
    
    [FileLocation(nameof(InfoLoomTwo))]
    [SettingsUIGroupOrder(CommercialTax, Other)]
    [SettingsUIShowGroupName(CommercialTax, Other)]
    public class Setting : ModSetting
    {
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
        [SettingsUISlider(min = -0.05f, max = -1.0f, step = 0.01f, unit = Unit.kFloatThreeFractions)] 
        public float TaxRateEffect { get; set; } = -0.05f; // Default value

        [SettingsUISection(kSection, CommercialTax)]
        public string CommercialTaxLEffect => TextMaker((Math.Abs(TaxRateEffect) * 100).ToString("F2"), "commercial");

        
        


        public Setting(IMod mod) : base(mod)
        {
            SetDefaults();
           
        }

        public override void SetDefaults()
        {
            TaxRateEffect = -0.05f;
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
                
            };
        }

        public void Unload()
        {
        }
    }
}
