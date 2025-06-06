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
    [SettingsUIGroupOrder(GeneralGroup, CommercialCompanyPanelGroup, IndustrialCompanyPanelGroup, TradeCostGroup)]
    [SettingsUIShowGroupName(GeneralGroup, CommercialCompanyPanelGroup, IndustrialCompanyPanelGroup, TradeCostGroup)]
    [SettingsUITabOrder(GeneralTab, SortingTab)]
    public class Setting : ModSetting
    {
        public const string GeneralGroup = "General";
        public const string CommercialCompanyPanelGroup = "Commercial Companies";
        public const string IndustrialCompanyPanelGroup = "Industrial Companies";
        public const string TradeCostGroup = "Trade Cost";
        
        
        public const string GeneralTab = "General";
        public const string SortingTab = "Sorting";
       
        public bool customUpdateInterval;
        

       


        

        
        [SettingsUISection(GeneralTab, GeneralGroup)]
        public bool CustomUpdateInterval
        {
            get => customUpdateInterval;
            set
            {
                customUpdateInterval = value;
                if (!value)
                {
                    UpdateInterval = 512; // Reset to default when disabling
                }
            }
        }
        [SettingsUISection(GeneralTab, GeneralGroup)]
        [SettingsUIDisableByCondition(typeof(Setting), nameof(CustomUpdateInterval) ,true)]
        
        [SettingsUISlider(min = 512, max = 262144, step = 8, unit = Unit.kInteger) ]
        public int UpdateInterval { get; set; }
        
        [SettingsUISection(SortingTab, CommercialCompanyPanelGroup)]
        public IndexSortingEnum CommercialIndexSorting { get; set; }
        [SettingsUISection(SortingTab, CommercialCompanyPanelGroup)]
        public CompanyNameEnum CommercialNameSorting { get; set; }
        [SettingsUISection(SortingTab, CommercialCompanyPanelGroup)]
        public ServiceUsageEnum CommercialServiceUsageSorting { get; set; }
        [SettingsUISection(SortingTab, CommercialCompanyPanelGroup)]
        public EmployeesEnum CommercialEmployeesSorting { get; set; }
        [SettingsUISection(SortingTab, CommercialCompanyPanelGroup)]
        public EfficiancyEnum CommercialEfficiencySorting { get; set; }
        [SettingsUISection(SortingTab, CommercialCompanyPanelGroup)]
        public ProfitabilityEnum CommercialProfitabilitySorting { get; set; }
        
        [SettingsUISection(SortingTab, IndustrialCompanyPanelGroup)]
        public IndexSortingEnum2 IndustrialIndexSorting { get; set; }
        [SettingsUISection(SortingTab, IndustrialCompanyPanelGroup)]
        public CompanyNameEnum2 IndustrialNameSorting { get; set; }
        [SettingsUISection(SortingTab, IndustrialCompanyPanelGroup)]
        public EmployeesEnum2 IndustrialEmployeesSorting { get; set; }
        [SettingsUISection(SortingTab, IndustrialCompanyPanelGroup)]
        public EfficiancyEnum2 IndustrialEfficiencySorting { get; set; }
        [SettingsUISection(SortingTab, IndustrialCompanyPanelGroup)]
        public ProfitabilityEnum2 IndustrialProfitabilitySorting { get; set; }
        
        [SettingsUISection(SortingTab, TradeCostGroup)]
        public ResourceNameEnum ResourceName { get; set; }
        [SettingsUISection(SortingTab, TradeCostGroup)]
        public BuyCostEnum BuyCost { get; set; }
        [SettingsUISection(SortingTab, TradeCostGroup)]
        public SellCostEnum SellCost { get; set; }
        [SettingsUISection(SortingTab, TradeCostGroup)]
        public ProfitEnum Profit { get; set; }
        [SettingsUISection(SortingTab, TradeCostGroup)]
        public ProfitMarginEnum ProfitMargin { get; set; }
        [SettingsUISection(SortingTab, TradeCostGroup)]
        public ImportAmountEnum ImportAmount { get; set; }
        [SettingsUISection(SortingTab, TradeCostGroup)]
        public ExportAmountEnum ExportAmount { get; set; }

        public Setting(IMod mod) : base(mod)
        {
            SetDefaults();
        }

        public override void SetDefaults()
        {
            CustomUpdateInterval = false;
            UpdateInterval = 512; 
            CommercialIndexSorting = IndexSortingEnum.Off;
            CommercialNameSorting = CompanyNameEnum.Off;
            CommercialServiceUsageSorting = ServiceUsageEnum.Off;
            CommercialEmployeesSorting = EmployeesEnum.Off;
            CommercialEfficiencySorting = EfficiancyEnum.Off;
            CommercialProfitabilitySorting = ProfitabilityEnum.Off;
            IndustrialIndexSorting = IndexSortingEnum2.Off;
            IndustrialNameSorting = CompanyNameEnum2.Off;
            IndustrialEmployeesSorting = EmployeesEnum2.Off;
            IndustrialEfficiencySorting = EfficiancyEnum2.Off;
            IndustrialProfitabilitySorting = ProfitabilityEnum2.Off;
            ResourceName = ResourceNameEnum.Off;
            BuyCost = BuyCostEnum.Off;
            SellCost = SellCostEnum.Off;
            Profit = ProfitEnum.Off;
            ProfitMargin = ProfitMarginEnum.Off;
            ImportAmount = ImportAmountEnum.Off;
            ExportAmount = ExportAmountEnum.Off;
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
                { m_Setting.GetOptionGroupLocaleID(Setting.GeneralGroup), "General" },
                { m_Setting.GetOptionGroupLocaleID(Setting.CommercialCompanyPanelGroup), "Commercial Company Panel" },
                { m_Setting.GetOptionGroupLocaleID(Setting.IndustrialCompanyPanelGroup), "Industrial Company Panel" },
                { m_Setting.GetOptionGroupLocaleID(Setting.TradeCostGroup), "Trade Cost" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CustomUpdateInterval)), "Enable Custom Update Interval" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CustomUpdateInterval)), "Enables the custom update interval for InfoLoom systems. If disabled, the default update interval will be used" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.UpdateInterval)), "System Update Interval" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.UpdateInterval)), "Adjust the speed at which InfoLoom systems update. Higher number means updates happen more frequently" },
                { m_Setting.GetOptionTabLocaleID(Setting.SortingTab), "Sorting Options" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CommercialIndexSorting)), "Index Sorting" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CommercialIndexSorting)), "Sorts the index of commercial companies by their index" },
                { m_Setting.GetEnumValueLocaleID(IndexSortingEnum.Off), "Off" },
                { m_Setting.GetEnumValueLocaleID(IndexSortingEnum.Ascending), "Ascending" },
                { m_Setting.GetEnumValueLocaleID(IndexSortingEnum.Descending), "Descending" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CommercialNameSorting)), "Name Sorting" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CommercialNameSorting)), "Sorts by the name of companies " },
                { m_Setting.GetEnumValueLocaleID(CompanyNameEnum.Off), "Off" },
                { m_Setting.GetEnumValueLocaleID(CompanyNameEnum.Ascending), "Ascending" },
                { m_Setting.GetEnumValueLocaleID(CompanyNameEnum.Descending), "Descending" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CommercialServiceUsageSorting)), "Service Usage Sorting" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CommercialServiceUsageSorting)), "Sorts by the service usage of companies" },
                { m_Setting.GetEnumValueLocaleID(ServiceUsageEnum.Off), "Off" },
                { m_Setting.GetEnumValueLocaleID(ServiceUsageEnum.Ascending), "Ascending" },
                { m_Setting.GetEnumValueLocaleID(ServiceUsageEnum.Descending), "Descending" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CommercialEmployeesSorting)), "Employees Sorting" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CommercialEmployeesSorting)), "Sorts by the number of employees in companies" },
                { m_Setting.GetEnumValueLocaleID(EmployeesEnum.Off), "Off" },
                { m_Setting.GetEnumValueLocaleID(EmployeesEnum.Ascending), "Ascending" },
                { m_Setting.GetEnumValueLocaleID(EmployeesEnum.Descending), "Descending" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CommercialEfficiencySorting)), "Efficiency Sorting" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CommercialEfficiencySorting)), "Sorts by the efficiency of companies" },
                { m_Setting.GetEnumValueLocaleID(EfficiancyEnum.Off), "Off" },
                { m_Setting.GetEnumValueLocaleID(EfficiancyEnum.Ascending), "Ascending" },
                { m_Setting.GetEnumValueLocaleID(EfficiancyEnum.Descending), "Descending" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CommercialProfitabilitySorting)), "Profitability Sorting" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CommercialProfitabilitySorting)), "Sorts by the profitability of companies" },
                { m_Setting.GetEnumValueLocaleID(ProfitabilityEnum.Off), "Off" },
                { m_Setting.GetEnumValueLocaleID(ProfitabilityEnum.Ascending), "Ascending" },
                { m_Setting.GetEnumValueLocaleID(ProfitabilityEnum.Descending), "Descending" },
                
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IndustrialIndexSorting)), "Index Sorting" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IndustrialIndexSorting)), "Sorts the index of industrial companies by their index" },
                { m_Setting.GetEnumValueLocaleID(IndexSortingEnum2.Off), "Off" },
                { m_Setting.GetEnumValueLocaleID(IndexSortingEnum2.Ascending), "Ascending" },
                { m_Setting.GetEnumValueLocaleID(IndexSortingEnum2.Descending), "Descending" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IndustrialNameSorting)), "Name Sorting" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IndustrialNameSorting)), "Sorts by the name of companies" },
                { m_Setting.GetEnumValueLocaleID(CompanyNameEnum2.Off), "Off" },
                { m_Setting.GetEnumValueLocaleID(CompanyNameEnum2.Ascending), "Ascending" },
                { m_Setting.GetEnumValueLocaleID(CompanyNameEnum2.Descending), "Descending" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IndustrialEmployeesSorting)), "Employees Sorting" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IndustrialEmployeesSorting)), "Sorts by the number of employees in companies" },
                { m_Setting.GetEnumValueLocaleID(EmployeesEnum2.Off), "Off" },
                { m_Setting.GetEnumValueLocaleID(EmployeesEnum2.Ascending), "Ascending" },
                { m_Setting.GetEnumValueLocaleID(EmployeesEnum2.Descending), "Descending" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IndustrialEfficiencySorting)), "Efficiency Sorting" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IndustrialEfficiencySorting)), "Sorts by the efficiency of companies" },
                { m_Setting.GetEnumValueLocaleID(EfficiancyEnum2.Off), "Off" },
                { m_Setting.GetEnumValueLocaleID(EfficiancyEnum2.Ascending), "Ascending" },
                { m_Setting.GetEnumValueLocaleID(EfficiancyEnum2.Descending), "Descending" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IndustrialProfitabilitySorting)), "Profitability Sorting" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IndustrialProfitabilitySorting)), "Sorts by the profitability of companies" },
                { m_Setting.GetEnumValueLocaleID(ProfitabilityEnum2.Off), "Off" },
                { m_Setting.GetEnumValueLocaleID(ProfitabilityEnum2.Ascending), "Ascending" },
                { m_Setting.GetEnumValueLocaleID(ProfitabilityEnum2.Descending), "Descending" },
                
               
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResourceName)), "Resource Name" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ResourceName)), "Sorts by the name of the resource" },
                { m_Setting.GetEnumValueLocaleID(ResourceNameEnum.Off), "Off" },
                { m_Setting.GetEnumValueLocaleID(ResourceNameEnum.Ascending), "Ascending" },
                { m_Setting.GetEnumValueLocaleID(ResourceNameEnum.Descending), "Descending" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.BuyCost)), "Buy Cost" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.BuyCost)), "Sorts by the buy cost of the resource" },
                { m_Setting.GetEnumValueLocaleID(BuyCostEnum.Off), "Off" },
                { m_Setting.GetEnumValueLocaleID(BuyCostEnum.Ascending), "Ascending" },
                { m_Setting.GetEnumValueLocaleID(BuyCostEnum.Descending), "Descending" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.SellCost)), "Sell Cost" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.SellCost)), "Sorts by the sell cost of the resource" },
                { m_Setting.GetEnumValueLocaleID(SellCostEnum.Off), "Off" },
                { m_Setting.GetEnumValueLocaleID(SellCostEnum.Ascending), "Ascending" },
                { m_Setting.GetEnumValueLocaleID(SellCostEnum.Descending), "Descending" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.Profit)), "Profit" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.Profit)), "Sorts by the profit made from the resource" },
                { m_Setting.GetEnumValueLocaleID(ProfitEnum.Off), "Off" },
                { m_Setting.GetEnumValueLocaleID(ProfitEnum.Ascending), "Ascending" },
                { m_Setting.GetEnumValueLocaleID(ProfitEnum.Descending), "Descending" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ProfitMargin)), "Profit Margin" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ProfitMargin)), "Sorts by the profit margin of the resource" },
                { m_Setting.GetEnumValueLocaleID(ProfitMarginEnum.Off), "Off" },
                { m_Setting.GetEnumValueLocaleID(ProfitMarginEnum.Ascending), "Ascending" },
                { m_Setting.GetEnumValueLocaleID(ProfitMarginEnum.Descending), "Descending" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ImportAmount)), "Import Amount" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ImportAmount)), "Sorts by the amount imported of the resource" },
                { m_Setting.GetEnumValueLocaleID(ImportAmountEnum.Off), "Off" },
                { m_Setting.GetEnumValueLocaleID(ImportAmountEnum.Ascending), "Ascending" },
                { m_Setting.GetEnumValueLocaleID(ImportAmountEnum.Descending), "Descending" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ExportAmount)), "Export Amount" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ExportAmount)), "Sorts by the amount exported of the resource" },
                { m_Setting.GetEnumValueLocaleID(ExportAmountEnum.Off), "Off" },
                { m_Setting.GetEnumValueLocaleID(ExportAmountEnum.Ascending), "Ascending" },
                { m_Setting.GetEnumValueLocaleID(ExportAmountEnum.Descending), "Descending" },
            };
        }

        public void Unload()
        {
        }
    }
}
