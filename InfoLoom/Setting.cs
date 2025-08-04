using System;
using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.UI;
using System.Collections.Generic;
using InfoLoomTwo.Domain.DataDomain.Enums.CompanyPanelEnums;
using InfoLoomTwo.Domain.DataDomain.Enums.TradeCostEnums;


namespace InfoLoomTwo
{
    
    [FileLocation(nameof(InfoLoomTwo))]
    [SettingsUIGroupOrder(PanelViewGroup)]
    [SettingsUIShowGroupName(PanelViewGroup)]
    [SettingsUITabOrder(GeneralTab)]
    public class Setting : ModSetting
    {
        public const string PanelViewGroup = "Panel View";
        
        
        public const string GeneralTab = "General";
        
       
        [SettingsUISection(GeneralTab, PanelViewGroup)]
        [SettingsUISlider(min = 0, max = 7, step = 1, unit = Unit.kInteger)]
        public int hideNoColumnsWF { get; set; }
        [SettingsUISection(GeneralTab, PanelViewGroup)]
        [SettingsUISlider(min = 0, max = 12, step = 1, unit = Unit.kInteger)]
        public int  hideNoColumnsWP { get; set; }
        
        public Setting(IMod mod) : base(mod)
        {
            SetDefaults();
        }

        public override void SetDefaults()
        {
           hideNoColumnsWF = 0;
           hideNoColumnsWP = 0;
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
                { m_Setting.GetOptionGroupLocaleID(Setting.PanelViewGroup), "General" },
                
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hideNoColumnsWF)), "Hide Workforce Columns" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hideNoColumnsWF)), "Set the number of columns to hide from right to left" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.hideNoColumnsWP)), "Hide Workplace Columns" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.hideNoColumnsWP)), "Set the number of columns to hide from right to left" },
            };
        }

        public void Unload()
        {
        }
    }
}
