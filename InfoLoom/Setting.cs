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
    [SettingsUIGroupOrder(PanelPositionReset)]
    [SettingsUIShowGroupName(PanelPositionReset)]
    [SettingsUITabOrder(GeneralTab)]
    public class Setting : ModSetting
    {
        public const string PanelPositionReset = "Reset Panel Position";
        
        
        public const string GeneralTab = "General";
        public float2 PanelLocation;
        
        
        
        
        
        public Setting(IMod mod) : base(mod)
        {
            SetDefaults();
        }

        public override void SetDefaults()
        {
           
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
                { m_Setting.GetOptionGroupLocaleID(Setting.PanelPositionReset), "General" },
                
                
            };
        }

        public void Unload()
        {
        }
    }
}
