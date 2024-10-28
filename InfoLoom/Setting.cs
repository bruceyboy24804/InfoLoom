using Colossal;
using Colossal.IO.AssetDatabase;
using Colossal.IO.AssetDatabase.Internal;
using Game.Modding;
using Game.Settings;
using Game.UI;
using Game.UI.InGame;
using Game.UI.Widgets;
using InfoLoomBrucey.Systems;
using System.Collections.Generic;
using Unity.Entities;

namespace InfoLoom
{
    [FileLocation(nameof(InfoLoom))]
    [SettingsUIGroupOrder(kOptionsGroup, kButtonGroup)]
    [SettingsUIShowGroupName(kOptionsGroup, kButtonGroup)]
    public class Setting : ModSetting
    {
        public const string kSection = "Main";

        public const string kOptionsGroup = "Options";
        public const string kButtonGroup = "Actions";

        [SettingsUISection(kSection, kOptionsGroup)]
        public bool FeatureCommercialDemand { get; set; }
        public Setting(IMod mod) : base(mod)
        {
            SetDefaults();
        }

        public override void SetDefaults()
        {
            
        }

        /// <summary>
        /// Gets or sets a value indicating whether: Used to force saving of Modsettings if settings would result in empty Json.
        /// </summary>



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

            { m_Setting.GetOptionGroupLocaleID(Setting.kOptionsGroup), "Options" },
            { m_Setting.GetOptionGroupLocaleID(Setting.kButtonGroup), "Actions" },

            { m_Setting.GetOptionLabelLocaleID(nameof(Setting.FeatureCommercialDemand)), "Enable Commercial Demand" },
            { m_Setting.GetOptionDescLocaleID(nameof(Setting.FeatureCommercialDemand)), "Enables modded commercial demand and dedicated UI." },

            };
        }
       

        public void Unload()
        {
        }
    }
}
