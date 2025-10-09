using System;
using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.UI;
using System.Collections.Generic;
using InfoLoomTwo.Domain.DataDomain.Enums.CompanyPanelEnums;
using Unity.Mathematics;


namespace InfoLoomTwo
{
    
    [FileLocation(nameof(InfoLoomTwo))]
    [SettingsUIGroupOrder(SectionsGroup)]
    [SettingsUIShowGroupName(SectionsGroup)]
    [SettingsUITabOrder(GeneralTab)]
    public class Setting : ModSetting
    {
        public const string SectionsGroup = "Sections";
        public const string GeneralTab = "General";
        
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
                { m_Setting.GetOptionGroupLocaleID(Setting.SectionsGroup), "sections" },
                
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hideBuildingSection)), "Building Section"  },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hideBuildingSection)), "Toggle on or off to hide the Building Section" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hideCitizenSection)), "Citizen Section"  },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hideCitizenSection)), "Toggle on or off to hide the Citizen Section" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hideProfitSection)), "Profit Section"  },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hideProfitSection)), "Toggle on or off to hide the Profit Section" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hideDistrictSection)), "District Section"  },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hideDistrictSection)), "Toggle on or off to hide the District Section" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hideRentSection)), "Rent Section"  },
                
                
            };
        }

        public void Unload()
        {
        }
    }
}
