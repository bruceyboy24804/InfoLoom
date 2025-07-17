using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Logging;
using Colossal.UI.Binding;
using Game;
using Game.Economy;
using Game.Rendering;
using Game.Simulation;
using Game.UI;
using InfoLoomTwo.Systems.ResidentialData;
using InfoLoomTwo.Domain;
using InfoLoomTwo.Domain.DataDomain;
using InfoLoomTwo.Domain.DataDomain.Enums.CompanyPanelEnums;
using InfoLoomTwo.Domain.DataDomain.Enums.TradeCostEnums;
using InfoLoomTwo.Extensions;
using InfoLoomTwo.Systems.CommercialSystems.CommercialCompanyDebugData;
using InfoLoomTwo.Systems.CommercialSystems.CommercialDemandData;
using InfoLoomTwo.Systems.CommercialSystems.CommercialProductData;
using InfoLoomTwo.Systems.DemographicsData;
using InfoLoomTwo.Systems.DistrictData;
using InfoLoomTwo.Systems.IndustrialSystems.IndustrialCompanyData;
using InfoLoomTwo.Systems.IndustrialSystems.IndustrialDemandData;
using InfoLoomTwo.Systems.IndustrialSystems.IndustrialProductData;
using InfoLoomTwo.Systems.TradeCostData;
using InfoLoomTwo.Systems.WorkforceData;
using InfoLoomTwo.Systems.WorkplacesData;
using Unity.Entities;
using Unity.Jobs;


namespace InfoLoomTwo.Systems
{
    public partial class InfoLoomUISystem : ExtendedUISystemBase
    {
        
        private const string ModID = "InfoLoomTwo";
        
        private const string InfoLoomMenuOpen = "InfoLoomMenuOpen";
        private const string CommercialMenuOpen = "CommercialMenuOpen";
        private const string IndustrialMenuOpen = "IndustrialMenuOpen";
        private const string DistrictMenuOpen = "DistrictMenuOpen";
        private const string ResidentialMenuOpen = "ResidentialMenuOpen";
        private const string BuildingDemandOpen = "BuildingDemandOpen";
        private const string CommercialDemandOpen = "CommercialDemandOpen";
        private const string CommercialProductsOpen = "CommercialProductsOpen";
        private const string DemographicsOpen = "DemographicsOpen";
        private const string DistrictDataOpen = "DistrictDataOpen";
        private const string IndustrialDemandOpen = "IndustrialDemandOpen";
        private const string IndustrialProductsOpen = "IndustrialProductsOpen";
        private const string ResidentialDemandOpen = "ResidentialDemandOpen";
        private const string TradeCostsOpen = "TradeCostsOpen";
        private const string WorkforceOpen = "WorkforceOpen";
        private const string WorkplacesOpen = "WorkplacesOpen";
        private const string CommercialCompanyDebugOpen = "CommercialCompanyDebugOpen";
        private const string IndustrialCompanyDebugOpen = "IndustrialCompanyDebugOpen";
        private const string HouseholdsDataOpen = "HouseholdsDataOpen";
        
        
        private CommercialCompanyDTO[] m_SortedCompanyData = Array.Empty<CommercialCompanyDTO>();
        private bool m_NeedSort = true;
        private int m_LastDataCount = 0;
        
        /*private const string BuildingDemandData = "BuildingDemandData";
        private const string CommercialData = "CommercialData";
        private const string CommercialDataExRes = "CommercialDataExRes"; 
        private const string CommercialProductsData = "CommercialProductsData";
        private const string DemographicsDataTotals = "DemographicsDataTotals";
        private const string DemographicsDataDetails = "DemographicsDataDetails";
        private const string DemographicsDataOldestCitizen = "DemographicsDataOldestCitizen";
        private const string DistrictData = "DistrictData";
        private const string IndustrialData = "IndustrialData";
        private const string IndustrialDataExRes = "IndustrialDataExRes";
        private const string IndustrialProductsData = "IndustrialProductsData";
        private const string ResidentialData = "ResidentialData";
        private const string TradeCostsData = "TradeCostsData";
        private const string TradeCostsDataImports = "TradeCostsDataImports";
        private const string TradeCostsDataExports = "TradeCostsDataExports";
        private const string WorkforceData = "WorkforceData";
        private const string WorkplacesData = "WorkplacesData";*/
        
        
        
        
        
        //Systems
        private SimulationSystem m_SimulationSystem;
        private NameSystem m_NameSystem;
        private UIUpdateState _uiUpdateState;
        
        private ResidentialDemandSystem m_ResidentialDemandSystem;
        private CommercialDemandSystem m_CommercialDemandSystem;
        private IndustrialDemandSystem m_IndustrialDemandSystem;
        
        private CommercialSystem m_CommercialSystem;
        private CommercialProductsSystem m_CommercialProductsSystem;
        private CommercialCompanyDataSystem m_CommercialCompanyDataSystem;
        private CameraUpdateSystem m_CameraUpdateSystem;
        private Demographics m_Demographics;
        private DistrictDataSystem m_DistrictDataSystem;
        private IndustrialSystem m_IndustrialSystem;
        private IndustrialProductsSystem m_IndustrialProductsSystem;
        private IndustrialCompanySystem m_IndustrialCompanySystem;
        private ResidentialSystem m_ResidentialSystem;
        private TradeCostsSystem m_TradeCostSystem;
        private WorkforceSystem m_WorkforceSystem;
        private WorkplacesSystem m_WorkplacesSystem;
        
        //Bindings
        //BuildingDemandUI
        private ValueBindingHelper<int[]> m_uiBuildingDemand;
        //CommercialDemandDataUI
        private ValueBindingHelper<string[]> m_ExcludedResourcesBinding;
        private ValueBindingHelper<int[]> m_CommercialBinding;
        //CommercialProductsDataUI
        private ValueBindingHelper<CommercialProductsData[]> m_CommercialProductBinding;
        //CommercialCompanyDebugDataUI
        private ValueBindingHelper<CommercialCompanyDTO[]> m_uiDebugData;
        private ValueBinding<bool> _cCDPVBinding;
        private ValueBinding<Entity> m_CameraBinding;
        
        private ValueBindingHelper<CompanyNameEnum> m_CommercialNameSortingBinding;
        private ValueBindingHelper<IndexSortingEnum> m_CommercialIndexSortingBinding;
        private ValueBindingHelper<ServiceUsageEnum> m_CommercialServiceUsageSortingBinding;
        private ValueBindingHelper<EmployeesEnum> m_CommercialEmployeesSortingBinding;
        private ValueBindingHelper<EfficiancyEnum> m_CommercialEfficiencySortingBinding;
        private ValueBindingHelper<ProfitabilityEnum> m_CommercialProfitabilitySortingBinding;
        private ValueBindingHelper<ResourceAmountEnum> m_CommercialResourceAmountSortingBinding;
        
        
        
        //HouseholdData
        //DemographicsUI
        private GetterValueBinding<int> m_OldCitizenBinding;
        private ValueBindingHelper<PopulationAtAgeInfo[]> m_PopulationAtAgeInfoBinding;
        private ValueBindingHelper<int[]> m_TotalsBinding;
        private ValueBinding<bool> m_DemoStatsToggledOnBinding;
        private ValueBinding<bool> m_DemoAgeGroupingToggledOnBinding;
        private ValueBindingHelper<GroupingStrategy> m_DemoGroupingStrategyBinding;
        //private ValueBindingHelper<Demographics.PopulationGroupData[]> m_GroupedPopulationBinding;  
        //private ValueBindingHelper<Demographics.GroupStrategyInfo[]> m_GroupStrategiesBinding;
//DistrictDataUI
        private RawValueBinding m_uiDistricts;
        private RawValueBinding m_uiResidents;
        private RawValueBinding m_uiEmployees;
        private RawValueBinding m_uiAgeAndEducation;
        //IndustrialDemandDataUI
        private ValueBindingHelper<string[]> m_IndustrialExcludedResourcesBinding;
        private ValueBindingHelper<int[]> m_IndustrialBinding;
        //IndustrialProductsDataUI
        private ValueBindingHelper<IndustrialProductsData[]> m_IndustrialProductBinding;
        //IndustrialCompanyDebugDataUI
        private ValueBindingHelper<IndustrialCompanyDTO[]> m_uiDebugData2;
        private ValueBinding<bool> _iCDVBinding;
        
        private ValueBindingHelper<CompanyNameEnum2> m_IndustrialNameSortingBinding;
        private ValueBindingHelper<IndexSortingEnum2> m_IndustrialIndexSortingBinding;
        private ValueBindingHelper<EmployeesEnum2> m_IndustrialEmployeesSortingBinding;
        private ValueBindingHelper<EfficiancyEnum2> m_IndustrialEfficiencySortingBinding;
        private ValueBindingHelper<ProfitabilityEnum2> m_IndustrialProfitabilitySortingBinding;
        private ValueBindingHelper<ResourceAmountEnum2> m_IndustrialResourceAmountSortingBinding;
        
        //ResidentialDemandDataUI
        public ValueBindingHelper<int[]> m_ResidentialBinding;
        

        //TrafficDataUI
        private RawValueBinding m_uiTrafficData;
        
        //TradeCostsUI
        private ValueBindingHelper<List<ResourceTradeCost>> m_TradeCostsBinding;
        private ValueBindingHelper<BuyCostEnum> m_BuyCostSortingBinding;
        private ValueBindingHelper<SellCostEnum> m_SellCostSortingBinding;
        private ValueBindingHelper<ImportAmountEnum> m_ImportAmountSortingBinding;
        private ValueBindingHelper<ExportAmountEnum> m_ExportAmountSortingBinding;
        private ValueBindingHelper<ProfitEnum> m_ProfitSortingBinding;
        private ValueBindingHelper<ProfitMarginEnum> m_ProfitMarginSortingBinding;
        private ValueBindingHelper<ResourceNameEnum> m_ResourceNameSortingBinding;
        
        
        
        //WorkforceUI
        private ValueBindingHelper<WorkforcesInfo[]> m_WorkforcesBinder;
        //WorkplacesUI
        private ValueBindingHelper<WorkplacesInfo[]> m_WorkplacesBinder;
        
        //Historical data
        private ValueBindingHelper<List<float>> m_ResourceHistoricalDataBinding;

        // Panel 
        private ValueBinding<bool> _panelVisibleBinding;
        private ValueBinding<bool> _commercialPanelVisibleBinding;
        private ValueBinding<bool> _industrialPanelVisibleBinding;
        private ValueBinding<bool> _districtPanelVisibleBinding;
        private ValueBinding<bool> _residentialPanelVisibleBinding;
        private ValueBinding<bool> _bDPVBinding;
        private ValueBinding<bool> _cDPVBinding;
        private ValueBinding<bool> _cPPVBinding;
        private ValueBinding<bool> _dPVBinding;
        private ValueBinding<bool> _dDPVBinding;
        private ValueBinding<bool> _iDPVBinding;
        private ValueBinding<bool> _iPPVBinding;
        private ValueBinding<bool> _rDPVBinding;
        private ValueBinding<bool> _tCPVBinding;
        private ValueBinding<bool> _wFPVBinding;
        private ValueBinding<bool> _wPPVBinding;
        private ValueBinding<bool> _householdsDataVisibleBinding;
        private ValueBinding<bool> _TrafficDataVisibleBinding;
        

        private ILog m_Log;
        
        public override GameMode gameMode => GameMode.Game;

        protected override void OnCreate()
        {    
            base.OnCreate();
            m_Log = Mod.log;
            _uiUpdateState = UIUpdateState.Create(World, 512);
            m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
            m_NameSystem = World.GetOrCreateSystemManaged<NameSystem>();
            m_ResidentialDemandSystem = base.World.GetOrCreateSystemManaged<ResidentialDemandSystem>();
            m_CommercialDemandSystem = base.World.GetOrCreateSystemManaged<CommercialDemandSystem>();
            m_IndustrialDemandSystem = base.World.GetOrCreateSystemManaged<IndustrialDemandSystem>();
            m_CommercialCompanyDataSystem = base.World.GetOrCreateSystemManaged<CommercialCompanyDataSystem>();
            m_DistrictDataSystem = base.World.GetOrCreateSystemManaged<DistrictDataSystem>();
            m_CommercialSystem = base.World.GetOrCreateSystemManaged<CommercialSystem>();
            m_CommercialProductsSystem = base.World.GetOrCreateSystemManaged<CommercialProductsSystem>();
            m_Demographics = base.World.GetOrCreateSystemManaged<Demographics>();
            m_IndustrialSystem = base.World.GetOrCreateSystemManaged<IndustrialSystem>();
            m_IndustrialProductsSystem = base.World.GetOrCreateSystemManaged<IndustrialProductsSystem>();
            m_ResidentialSystem = base.World.GetOrCreateSystemManaged<ResidentialSystem>();
            m_TradeCostSystem = base.World.GetOrCreateSystemManaged<TradeCostsSystem>();
            m_WorkforceSystem = base.World.GetOrCreateSystemManaged<WorkforceSystem>();
            m_WorkplacesSystem = base.World.GetOrCreateSystemManaged<WorkplacesSystem>();
            m_CameraUpdateSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<CameraUpdateSystem>();
            m_IndustrialCompanySystem = World.GetOrCreateSystemManaged<IndustrialCompanySystem>();
            
            
            //InfoLoomMenu
            _panelVisibleBinding = new ValueBinding<bool>(ModID, InfoLoomMenuOpen, false);
            AddBinding(_panelVisibleBinding);
            AddBinding(new TriggerBinding<bool>(ModID, InfoLoomMenuOpen, SetInfoLoomMenuVisibility));
            
            //CommercialMenu
            _commercialPanelVisibleBinding = new ValueBinding<bool>(ModID, CommercialMenuOpen, false);
            AddBinding(_commercialPanelVisibleBinding);
            AddBinding(new TriggerBinding<bool>(ModID, CommercialMenuOpen, SetCommercialMenuVisibility));
            
            //IndustrialMenu
            _industrialPanelVisibleBinding = new ValueBinding<bool>(ModID, IndustrialMenuOpen, false);
            AddBinding(_industrialPanelVisibleBinding);
            AddBinding(new TriggerBinding<bool>(ModID, IndustrialMenuOpen, SetIndustrialMenuVisibility));
            
            //DistrictMenu
            _districtPanelVisibleBinding = new ValueBinding<bool>(ModID, DistrictMenuOpen, false);
            AddBinding(_districtPanelVisibleBinding);
            AddBinding(new TriggerBinding<bool>(ModID, DistrictMenuOpen, SetDistrictMenuVisibility));
            
            //ResidentialMenu
            _residentialPanelVisibleBinding = new ValueBinding<bool>(ModID, ResidentialMenuOpen, false);
            AddBinding(_residentialPanelVisibleBinding);
            AddBinding(new TriggerBinding<bool>(ModID, ResidentialMenuOpen, SetResidentialMenuVisibility));
            
            
            _bDPVBinding = new ValueBinding<bool>(ModID, BuildingDemandOpen, false);
            AddBinding(_bDPVBinding);
            AddBinding(new TriggerBinding<bool>(ModID, BuildingDemandOpen, SetBuildingDemandVisibility));
            
             _cDPVBinding = new ValueBinding<bool>(ModID, CommercialDemandOpen, false);
            AddBinding(_cDPVBinding);
            AddBinding(new TriggerBinding<bool>(ModID, CommercialDemandOpen, SetCommercialDemandVisibility));

             _cPPVBinding = new ValueBinding<bool>(ModID, CommercialProductsOpen, false);
             AddBinding(_cPPVBinding);
             AddBinding(new TriggerBinding<bool>(ModID, CommercialProductsOpen, SetCommercialProductsVisibility));
            
             _dPVBinding = new ValueBinding<bool>(ModID, DemographicsOpen, false);
             AddBinding(_dPVBinding);
             AddBinding(new TriggerBinding<bool>(ModID, DemographicsOpen, SetDemographicsVisibility));
            
             _dDPVBinding = new ValueBinding<bool>(ModID, DistrictDataOpen, false);
             AddBinding(_dDPVBinding);
             AddBinding(new TriggerBinding<bool>(ModID, DistrictDataOpen, SetDistrictDataVisibility));
            
             _iDPVBinding = new ValueBinding<bool>(ModID, IndustrialDemandOpen, false);
             AddBinding(_iDPVBinding);
             AddBinding(new TriggerBinding<bool>(ModID, IndustrialDemandOpen, SetIndustrialDemandVisibility));
            
             _iPPVBinding = new ValueBinding<bool>(ModID, IndustrialProductsOpen, false);
             AddBinding(_iPPVBinding);
             AddBinding(new TriggerBinding<bool>(ModID, IndustrialProductsOpen, SetIndustrialProductsVisibility));
            
             _rDPVBinding = new ValueBinding<bool>(ModID, ResidentialDemandOpen, false);
             AddBinding(_rDPVBinding);
             AddBinding(new TriggerBinding<bool>(ModID, ResidentialDemandOpen, SetResidentialDemandVisibility));
             
             _tCPVBinding = new ValueBinding<bool>(ModID, TradeCostsOpen, false);
             AddBinding(_tCPVBinding);
             AddBinding(new TriggerBinding<bool>(ModID, TradeCostsOpen, SetTradeCostsVisibility));
            
             _wFPVBinding = new ValueBinding<bool>(ModID, WorkforceOpen, false);
             AddBinding(_wFPVBinding);
             AddBinding(new TriggerBinding<bool>(ModID, WorkforceOpen, SetWorkforceVisibility));
            
             _wPPVBinding = new ValueBinding<bool>(ModID, WorkplacesOpen, false);
             AddBinding(_wPPVBinding);
             AddBinding(new TriggerBinding<bool>(ModID, WorkplacesOpen, SetWorkplacesVisibility));
             
             _TrafficDataVisibleBinding = new ValueBinding<bool>(ModID, "TrafficDataVisible", false);
                AddBinding(_TrafficDataVisibleBinding);
               AddBinding(new TriggerBinding<bool>(ModID, "TrafficDataVisible", SetTrafficDataVisibility));
             
             
             
            m_uiBuildingDemand = CreateBinding("BuildingDemandData", new int[0]);
            //CommercialDemandDataUI
            m_CommercialBinding = CreateBinding("CommercialData", new int[10]);
            m_ExcludedResourcesBinding = CreateBinding("CommercialDataExRes", new string[0]);

            //CommercialProductsDataUI
            m_CommercialProductBinding = CreateBinding("CommercialProductsData", Array.Empty<CommercialProductsData>());
            //CommercialCompanyDebugDataUI
            _cCDPVBinding = new ValueBinding<bool>(ModID, CommercialCompanyDebugOpen, false);
             AddBinding(_cCDPVBinding);
             AddBinding(new TriggerBinding<bool>(ModID, CommercialCompanyDebugOpen, SetCommercialCompanyDebugVisibility));
             m_uiDebugData = CreateBinding("CommercialCompanyDebugData", new CommercialCompanyDTO[0]);
             AddBinding(new TriggerBinding<Entity>(ModID, "GoTo", NavigateTo));
             
             m_CommercialIndexSortingBinding = CreateBinding("CommercialIndexSorting", "SetCommercialIndexSorting" , IndexSortingEnum.Off);
             m_CommercialNameSortingBinding = CreateBinding("CommercialNameSorting", "SetCommercialNameSorting", CompanyNameEnum.Off);
             m_CommercialServiceUsageSortingBinding = CreateBinding("CommercialServiceUsageSorting", "SetCommercialServiceUsageSorting", ServiceUsageEnum.Off);
             m_CommercialEmployeesSortingBinding = CreateBinding("CommercialEmployeesSorting", "SetCommercialEmployeesSorting", EmployeesEnum.Off);
             m_CommercialEfficiencySortingBinding = CreateBinding("CommercialEfficiencySorting", "SetCommercialEfficiencySorting", EfficiancyEnum.Off);
             m_CommercialProfitabilitySortingBinding = CreateBinding("CommercialProfitabilitySorting", "SetCommercialProfitabilitySorting", ProfitabilityEnum.Off);
             m_CommercialResourceAmountSortingBinding = CreateBinding("CommercialResourceAmountSorting", "SetCommercialResourceAmountSorting", ResourceAmountEnum.Off);
             
             

             
            //DemographicsUI
            m_PopulationAtAgeInfoBinding = CreateBinding("DemographicsDataDetails", new PopulationAtAgeInfo[0]);
            m_TotalsBinding = CreateBinding("DemographicsDataTotals", new int[10]);
            m_OldCitizenBinding = CreateBinding("DemographicsDataOldestCitizen", () =>
            {
                var demographics = World.GetExistingSystemManaged<Demographics>();
                return demographics.m_Totals[6];
            });
            m_DemoStatsToggledOnBinding = new ValueBinding<bool>(ModID, "DemoStatsToggledOn", false);
             AddBinding(m_DemoStatsToggledOnBinding);
             AddBinding(new TriggerBinding<bool>(ModID, "DemoStatsToggledOn", SetDemoStatsVisibility));
             m_DemoAgeGroupingToggledOnBinding = new ValueBinding<bool>(ModID, "DemoAgeGroupingToggledOn", false);
             AddBinding(m_DemoAgeGroupingToggledOnBinding);
             AddBinding(new TriggerBinding<bool>(ModID, "DemoAgeGroupingToggledOn", SetDemoAgeGroupingVisibility));
             m_DemoGroupingStrategyBinding = CreateBinding("DemoGroupingStrategy", "SetDemoGroupingStrategy",  GroupingStrategy.None);
            //m_GroupedPopulationBinding = CreateBinding("PopulationGroupDatas", new Demographics.PopulationGroupData[0]);

            
            //DistrictDataUI
            // First binding: Basic district information
            AddBinding(m_uiDistricts = new RawValueBinding("InfoLoomTwo", "DistrictData", m_DistrictDataSystem.WriteDistricts));
            


            //IndustrialDemandDataUI
            m_IndustrialBinding = CreateBinding("IndustrialData", new int[16]);
            m_IndustrialExcludedResourcesBinding = CreateBinding("IndustrialDataExRes", new string[0]);

            //IndustrialProductsDataUI
            m_IndustrialProductBinding = CreateBinding("IndustrialProductsData", Array.Empty<IndustrialProductsData>());
            
            //IndustrialCompanyDebugDataUI
            _iCDVBinding = new ValueBinding<bool>(ModID, IndustrialCompanyDebugOpen, false);
             AddBinding(_iCDVBinding);
             AddBinding(new TriggerBinding<bool>(ModID, IndustrialCompanyDebugOpen, SetIndustrialCompanyDebugVisibility));
             m_uiDebugData2 = CreateBinding("IndustrialCompanyDebugData", new IndustrialCompanyDTO[0]);
             
            m_IndustrialIndexSortingBinding = CreateBinding("IndustrialIndexSorting", "SetIndustrialIndexSorting", IndexSortingEnum2.Off);
            m_IndustrialNameSortingBinding = CreateBinding("IndustrialNameSorting", "SetIndustrialNameSorting", CompanyNameEnum2.Off);
            m_IndustrialEmployeesSortingBinding = CreateBinding("IndustrialEmployeesSorting", "SetIndustrialEmployeesSorting", EmployeesEnum2.Off);
            m_IndustrialEfficiencySortingBinding = CreateBinding("IndustrialEfficiencySorting", "SetIndustrialEfficiencySorting", EfficiancyEnum2.Off);
            m_IndustrialProfitabilitySortingBinding = CreateBinding("IndustrialProfitabilitySorting", "SetIndustrialProfitabilitySorting", ProfitabilityEnum2.Off);
            m_IndustrialResourceAmountSortingBinding = CreateBinding("IndustrialResourceAmountSorting", "SetIndustrialResourceAmountSorting", ResourceAmountEnum2.Off);

            //ResidentialDemandDataUI
            m_ResidentialBinding = CreateBinding("ResidentialData", new int[21]);
            
            
            //TradeCostsUI
            m_TradeCostsBinding = CreateBinding("TradeCostsData", new List<ResourceTradeCost>());
            m_BuyCostSortingBinding = CreateBinding("BuyCost", "SetBuyCost", BuyCostEnum.Off);
            m_SellCostSortingBinding = CreateBinding("SellCost", "SetSellCost", SellCostEnum.Off);
            m_ImportAmountSortingBinding = CreateBinding("ImportAmount", "SetImportAmount", ImportAmountEnum.Off);
            m_ExportAmountSortingBinding = CreateBinding("ExportAmount", "SetExportAmount", ExportAmountEnum.Off);
            m_ProfitSortingBinding = CreateBinding("Profit", "SetProfit", ProfitEnum.Off);
            m_ProfitMarginSortingBinding = CreateBinding("ProfitMargin", "SetProfitMargin", ProfitMarginEnum.Off);
            m_ResourceNameSortingBinding = CreateBinding("ResourceName", "SetResourceName", ResourceNameEnum.Off);
            
            
            
            
            

            //WorkforceUI
            m_WorkforcesBinder = CreateBinding("WorkforceData", new WorkforcesInfo[0]);

            //WorkplacesUI
            m_WorkplacesBinder = CreateBinding("WorkplacesData", new WorkplacesInfo[0]);

            //Historical data
            
        }

        

        protected override void OnUpdate()
        {
            if (_bDPVBinding.value )
            {
                
                m_uiBuildingDemand.Value = new int[]
                {
                    m_ResidentialDemandSystem.buildingDemand.x,
                    m_ResidentialDemandSystem.buildingDemand.y,
                    m_ResidentialDemandSystem.buildingDemand.z,
                    m_CommercialDemandSystem.buildingDemand,
                    m_IndustrialDemandSystem.industrialBuildingDemand,
                    m_IndustrialDemandSystem.storageBuildingDemand,
                    m_IndustrialDemandSystem.officeBuildingDemand
                };
            }

            if (_cDPVBinding.value )
            {
                var commercialSystem = base.World.GetOrCreateSystemManaged<CommercialSystem>();
                m_CommercialBinding.Value = commercialSystem.m_Results.ToArray();
                m_ExcludedResourcesBinding.Value =
                    commercialSystem.m_ExcludedResources.value == Resource.NoResource
                    ? new string[0]
                    : ExtractExcludedResources(commercialSystem.m_ExcludedResources.value);
                
                
                m_CommercialSystem.IsPanelVisible = true;
                m_CommercialSystem.ForceUpdateOnce();
                
            }

            if (_cPPVBinding.value )
            {
                m_CommercialProductBinding.Value = CommercialProductsSystem.m_ProductsData
                    .Where(d => d.Demand > 0 || d.Companies > 0 || d.Building > 0 || d.Free > 0)
                    .Select(d => new CommercialProductsData
                    {
                        ResourceName = d.ResourceName.ToString(),
                        Demand = d.Demand,
                        Building = d.Building,
                        Free = d.Free,
                        Companies = d.Companies,
                        Workers = d.Workers,
                        SvcFactor = d.SvcFactor,
                        SvcPercent = d.SvcPercent,
                        ResourceNeedPercent = d.ResourceNeedPercent,
                        ResourceNeedPerCompany = d.ResourceNeedPerCompany,
                        WrkPercent = d.WrkPercent,
                        TaxFactor = d.TaxFactor,
                        CurrentTourists = d.CurrentTourists,
                        AvailableLodging = d.AvailableLodging,
                        RequiredRooms = d.RequiredRooms,
                    })
                    .ToArray();
        
                if (m_CommercialProductBinding.Value.Length == 0 && CommercialProductsSystem.m_ProductsData.Length > 0)
                {
                    m_CommercialProductBinding.Value = new CommercialProductsData[]
                    {
                        new CommercialProductsData
                        {
                            ResourceName = "All",
                            Demand = 0,
                            Building = 0,
                            Free = 0,
                            Companies = 0,
                            Workers = 0,
                            SvcFactor = 0,
                            SvcPercent = 0,
                            ResourceNeedPercent = 0,
                            ResourceNeedPerCompany = 0,
                            WrkPercent = 0,
                            TaxFactor = 0,
                            CurrentTourists = 0,
                            AvailableLodging = 0,
                            RequiredRooms = 0,
                        }
                    };
                }
                m_CommercialProductsSystem.IsPanelVisible = true; 
            }
            if (_cCDPVBinding.value)
            {
                    m_CommercialCompanyDataSystem.IsPanelVisible = true;
                    
                    // Get the current sorting values from bindings
                    CompanyNameEnum companyNameSorting = m_CommercialNameSortingBinding.Value;
                    EfficiancyEnum efficiencySorting = m_CommercialEfficiencySortingBinding.Value;
                    IndexSortingEnum indexSorting = m_CommercialIndexSortingBinding.Value;
                    ServiceUsageEnum serviceUsageSorting = m_CommercialServiceUsageSortingBinding.Value;
                    EmployeesEnum employeesSorting = m_CommercialEmployeesSortingBinding.Value;
                    ProfitabilityEnum profitabilitySorting = m_CommercialProfitabilitySortingBinding.Value;
                    ResourceAmountEnum resourceAmountSorting = m_CommercialResourceAmountSortingBinding.Value;
                    
                    // Set the current sorting values in the data system
                    m_CommercialCompanyDataSystem.m_CurrentIndexSorting = indexSorting;
                    m_CommercialCompanyDataSystem.m_CurrentCompanyNameSorting = companyNameSorting;
                    m_CommercialCompanyDataSystem.m_CurrentServiceUsageSorting = serviceUsageSorting;
                    m_CommercialCompanyDataSystem.m_CurrentEmployeesSorting = employeesSorting;
                    m_CommercialCompanyDataSystem.m_CurrentEfficiencySorting = efficiencySorting;
                    m_CommercialCompanyDataSystem.m_CurrentProfitabilitySorting = profitabilitySorting;
                    m_CommercialCompanyDataSystem.m_CurrentResourceAmountSorting = resourceAmountSorting;
                    
                    // Sort the original array directly using the comparison methods
                    // Each sort will be applied in sequence
                    if (indexSorting != IndexSortingEnum.Off)
                        Array.Sort(m_CommercialCompanyDataSystem.m_CommercialCompanyDTOs, m_CommercialCompanyDataSystem.CompareByIndex);
                    
                    if (companyNameSorting != CompanyNameEnum.Off)
                        Array.Sort(m_CommercialCompanyDataSystem.m_CommercialCompanyDTOs, m_CommercialCompanyDataSystem.CompareByName);
                    
                    if (serviceUsageSorting != ServiceUsageEnum.Off)
                        Array.Sort(m_CommercialCompanyDataSystem.m_CommercialCompanyDTOs, m_CommercialCompanyDataSystem.CompareByServiceUsage);
                    
                    if (employeesSorting != EmployeesEnum.Off)
                        Array.Sort(m_CommercialCompanyDataSystem.m_CommercialCompanyDTOs, m_CommercialCompanyDataSystem.CompareByEmployees);
                    if (resourceAmountSorting != ResourceAmountEnum.Off)
                        Array.Sort(m_CommercialCompanyDataSystem.m_CommercialCompanyDTOs, m_CommercialCompanyDataSystem.CompareByResourceAmount);
                    
                    if (efficiencySorting != EfficiancyEnum.Off)
                        Array.Sort(m_CommercialCompanyDataSystem.m_CommercialCompanyDTOs, m_CommercialCompanyDataSystem.CompareByEfficiency);
                    
                    if (profitabilitySorting != ProfitabilityEnum.Off)
                        Array.Sort(m_CommercialCompanyDataSystem.m_CommercialCompanyDTOs, m_CommercialCompanyDataSystem.CompareByProfitability);
                    
                    // Update UI with the sorted data
                    m_uiDebugData.Value = m_CommercialCompanyDataSystem.m_CommercialCompanyDTOs;
                    
                    // Update the UI to reflect current sorting values
                    m_CommercialNameSortingBinding.UpdateCallback(companyNameSorting);
                    m_CommercialEfficiencySortingBinding.UpdateCallback(efficiencySorting);
                    m_CommercialIndexSortingBinding.UpdateCallback(indexSorting);
                    m_CommercialServiceUsageSortingBinding.UpdateCallback(serviceUsageSorting);
                    m_CommercialEmployeesSortingBinding.UpdateCallback(employeesSorting);
                    m_CommercialProfitabilitySortingBinding.UpdateCallback(profitabilitySorting);
            }
            if (_iCDVBinding.value)
            {
                m_IndustrialCompanySystem.IsPanelVisible = true;
                
                // Get the current sorting values from bindings
                CompanyNameEnum2 companyNameSorting = m_IndustrialNameSortingBinding.Value;
                EfficiancyEnum2 efficiencySorting = m_IndustrialEfficiencySortingBinding.Value;
                IndexSortingEnum2 indexSorting = m_IndustrialIndexSortingBinding.Value;
                EmployeesEnum2 employeesSorting = m_IndustrialEmployeesSortingBinding.Value;
                ProfitabilityEnum2 profitabilitySorting = m_IndustrialProfitabilitySortingBinding.Value;
                ResourceAmountEnum2 resourceAmountSorting = m_IndustrialResourceAmountSortingBinding.Value;
                
                // Set the current sorting values in the data system
                m_IndustrialCompanySystem.m_CurrentIndexSorting = indexSorting;
                m_IndustrialCompanySystem.m_CurrentCompanyNameSorting = companyNameSorting;
                m_IndustrialCompanySystem.m_CurrentEmployeesSorting = employeesSorting;
                m_IndustrialCompanySystem.m_CurrentEfficiencySorting = efficiencySorting;
                m_IndustrialCompanySystem.m_CurrentProfitabilitySorting = profitabilitySorting;
                m_IndustrialCompanySystem.m_CurrentResourceAmountSorting = resourceAmountSorting;
                // Sort the original array directly using the comparison methods
                // Each sort will be applied in sequence
                if (indexSorting != IndexSortingEnum2.Off)
                    Array.Sort(m_IndustrialCompanySystem.m_IndustrialCompanyDTOs, m_IndustrialCompanySystem.CompareByIndex);
                if (companyNameSorting != CompanyNameEnum2.Off)
                    Array.Sort(m_IndustrialCompanySystem.m_IndustrialCompanyDTOs, m_IndustrialCompanySystem.CompareByName);
                if (employeesSorting != EmployeesEnum2.Off)
                    Array.Sort(m_IndustrialCompanySystem.m_IndustrialCompanyDTOs, m_IndustrialCompanySystem.CompareByEmployees);
                if (resourceAmountSorting != ResourceAmountEnum2.Off)
                    Array.Sort(m_IndustrialCompanySystem.m_IndustrialCompanyDTOs, m_IndustrialCompanySystem.CompareByResourceAmount);
                if (efficiencySorting != EfficiancyEnum2.Off)
                    Array.Sort(m_IndustrialCompanySystem.m_IndustrialCompanyDTOs, m_IndustrialCompanySystem.CompareByEfficiency);
                if (profitabilitySorting != ProfitabilityEnum2.Off)
                    Array.Sort(m_IndustrialCompanySystem.m_IndustrialCompanyDTOs, m_IndustrialCompanySystem.CompareByProfitability);
                // Update UI with the sorted data
                m_uiDebugData2.Value = m_IndustrialCompanySystem.m_IndustrialCompanyDTOs;
                
            }
            if (_tCPVBinding.value)
            {
                var tradeCostsSystem = World.GetOrCreateSystemManaged<TradeCostsSystem>();
                BuyCostEnum buyCostSorting = m_BuyCostSortingBinding.Value;
                SellCostEnum sellCostSorting = m_SellCostSortingBinding.Value;
                ImportAmountEnum importAmountSorting = m_ImportAmountSortingBinding.Value;
                ExportAmountEnum exportAmountSorting = m_ExportAmountSortingBinding.Value;
                ProfitEnum profitSorting = m_ProfitSortingBinding.Value;
                ProfitMarginEnum profitMarginSorting = m_ProfitMarginSortingBinding.Value;
                ResourceNameEnum resourceNameSorting = m_ResourceNameSortingBinding.Value;
                
                // Set the current sorting values in the data system
                tradeCostsSystem.m_BuyCostEnum = buyCostSorting;
                tradeCostsSystem.m_sellCostEnum = sellCostSorting;
                tradeCostsSystem.m_importAmountEnum = importAmountSorting;
                tradeCostsSystem.m_exportAmountEnum = exportAmountSorting;
                tradeCostsSystem.m_profitEnum = profitSorting;
                tradeCostsSystem.m_profitMarginEnum = profitMarginSorting;
                tradeCostsSystem.m_resourceNameEnum = resourceNameSorting;
                
                m_BuyCostSortingBinding.UpdateCallback(buyCostSorting);
                m_SellCostSortingBinding.UpdateCallback(sellCostSorting);
                m_ImportAmountSortingBinding.UpdateCallback(importAmountSorting);
                m_ExportAmountSortingBinding.UpdateCallback(exportAmountSorting);
                m_ProfitSortingBinding.UpdateCallback(profitSorting);
                m_ProfitMarginSortingBinding.UpdateCallback(profitMarginSorting);
                m_ResourceNameSortingBinding.UpdateCallback(resourceNameSorting);
                m_TradeCostsBinding.Value = tradeCostsSystem.GetSortedResourceTradeCosts().ToList();
            }
            //m_Log.Debug($"{nameof(InfoLoomUISystem)}.{nameof(OnUpdate)} 2.");
            if (_dPVBinding.value)
            {
                var demographics = World.GetExistingSystemManaged<Demographics>();

                m_PopulationAtAgeInfoBinding.Value = demographics.m_Results.ToArray();

                // Get current grouping strategy

                GroupingStrategy currentStrategy = m_DemoGroupingStrategyBinding.Value;

                // Update the grouped population data binding

                //m_Log.Debug($"{nameof(InfoLoomUISystem)}.{nameof(OnUpdate)} getting population by age group");
                //m_GroupedPopulationBinding.Value = demographics.GetPopulationByAgeGroups(currentStrategy).ToArray();
                
                //m_DemoAgeGroupingToggledOnBinding.TriggerUpdate();
                m_DemoGroupingStrategyBinding.UpdateCallback(currentStrategy);
                
                if (m_DemoStatsToggledOnBinding.value)
                {
                    //m_Log.Debug($"{nameof(InfoLoomUISystem)}.{nameof(OnUpdate)} totals binding");
                    m_TotalsBinding.Value = demographics.m_Totals.ToArray();
                    m_OldCitizenBinding.Update();
                }
                
                m_Demographics.IsPanelVisible = true;
                m_Demographics.ForceUpdateOnce();

                //m_Log.Debug($"{nameof(InfoLoomUISystem)}.{nameof(OnUpdate)} demographics finished");
            }
        
            if (_dDPVBinding.value )
            {
                m_uiDistricts.Update();
                m_DistrictDataSystem.IsPanelVisible = true;
               
            }
        
            if (_iDPVBinding.value )
            {
                var industrialSystem = base.World.GetOrCreateSystemManaged<IndustrialSystem>();
                m_IndustrialBinding.Value = industrialSystem.m_Results.ToArray();
                m_ExcludedResourcesBinding.Value =
                    industrialSystem.m_ExcludedResources.value == Resource.NoResource
                    ? new string[0]
                    : IndustrialExtractExcludedResources(industrialSystem.m_ExcludedResources.value);
                
                m_IndustrialSystem.IsPanelVisible = true;
                m_IndustrialSystem.ForceUpdateOnce();
            }
        
            if (_iPPVBinding.value && _uiUpdateState.Advance())
            {
                m_IndustrialProductBinding.Value = IndustrialProductsSystem.m_DemandData
                    .Where(d => d.Demand > 0 || d.Companies > 0 || d.Building > 0 || d.Free > 0)
                    .Select(d => new IndustrialProductsData
                    {
                        ResourceName = EconomyUtils.GetName(d.ResourceName),
                        Demand = d.Demand,
                        Building = d.Building,
                        Free = d.Free,
                        Companies = d.Companies,
                        Workers = d.Workers,
                        SvcPercent = d.SvcPercent,
                        WrkPercent = d.WrkPercent,
                        TaxFactor = d.TaxFactor,
                        CapPercent = d.CapPercent,
                        CapPerCompany = d.CapPerCompany,
                    }).ToArray();
                if (m_IndustrialProductBinding.Value.Length == 0 && IndustrialProductsSystem.m_DemandData.Length > 0)
                {
                    m_IndustrialProductBinding.Value = new IndustrialProductsData[]
                    {
                        new IndustrialProductsData
                        {
                            ResourceName = "All",
                            Demand = 0,
                            Building = 0,
                            Free = 0,
                            Companies = 0,
                            Workers = 0,
                            SvcPercent = 0,
                            CapPercent = 0,
                            CapPerCompany = 0,
                            WrkPercent = 0,
                            TaxFactor = 0,
                            
                            
                        }
                    };
                }
                m_IndustrialProductsSystem.IsPanelVisible = true;
            }
            if (_rDPVBinding.value)
            {
                ResidentialSystem residentialSystem = base.World.GetOrCreateSystemManaged<ResidentialSystem>();
                m_ResidentialBinding.Value = residentialSystem.m_Results.ToArray();
               
                
                m_ResidentialSystem.IsPanelVisible = true;
               
                
            }
            
        
            if (_wFPVBinding.value)
            {
                var workforceSystem = base.World.GetOrCreateSystemManaged<WorkforceSystem>();
                m_WorkforcesBinder.Value = workforceSystem.m_Results.ToArray();
                
                m_WorkforceSystem.IsPanelVisible = true;
                m_WorkforceSystem.ForceUpdateOnce();
                
            }
        
            if (_wPPVBinding.value )
            {
                var workplacesSystem = base.World.GetOrCreateSystemManaged<WorkplacesSystem>();
                m_WorkplacesBinder.Value = workplacesSystem.m_Results.ToArray();
               
                
                m_WorkplacesSystem.IsPanelVisible = true;
                m_WorkplacesSystem.ForceUpdateOnce();
                
            }
            
            
            base.OnUpdate();
        }
        private void SetInfoLoomMenuVisibility(bool open)
        {
            _panelVisibleBinding.Update(open);
        }
        private void SetCommercialMenuVisibility(bool open)
        {
            _commercialPanelVisibleBinding.Update(open);
        }
        private void SetIndustrialMenuVisibility(bool open)
        {
            _industrialPanelVisibleBinding.Update(open);
        }
        private void SetDistrictMenuVisibility(bool open)
        {
            _districtPanelVisibleBinding.Update(open);
        }
        private void SetResidentialMenuVisibility(bool open)
        {
            _residentialPanelVisibleBinding.Update(open);
        }
        
        private void SetBuildingDemandVisibility(bool open)
        {
            _bDPVBinding.Update(open);
           
            if (open)
            {
                
                m_uiBuildingDemand.Value = new int[]
                {
                    m_ResidentialDemandSystem.buildingDemand.x,
                    m_ResidentialDemandSystem.buildingDemand.y,
                    m_ResidentialDemandSystem.buildingDemand.z,
                    m_CommercialDemandSystem.buildingDemand,
                    m_IndustrialDemandSystem.industrialBuildingDemand,
                    m_IndustrialDemandSystem.storageBuildingDemand,
                    m_IndustrialDemandSystem.officeBuildingDemand
                };
            }
        }
        
        
        private void SetCommercialDemandVisibility(bool open)
        {
            _cDPVBinding.Update(open);
            m_CommercialSystem.IsPanelVisible = open;
            if (open)
            {
                m_CommercialSystem.ForceUpdateOnce();
                
                var commercialSystem = World.GetOrCreateSystemManaged<CommercialSystem>();
                m_CommercialBinding.Value = commercialSystem.m_Results.ToArray();
                m_ExcludedResourcesBinding.Value = 
                    commercialSystem.m_ExcludedResources.value == Resource.NoResource
                    ? new string[0]
                    : ExtractExcludedResources(commercialSystem.m_ExcludedResources.value);
            }
        }
        private void SetCommercialProductsVisibility(bool open)
        {
            _cPPVBinding.Update(open);
            m_CommercialProductsSystem.IsPanelVisible = open;
            
            if (open)
            {
                
                m_CommercialProductBinding.Value = CommercialProductsSystem.m_ProductsData
                    .Where(d => d.Demand > 0 || d.Companies > 0 || d.Building > 0 || d.Free > 0)
                    .Select(d => new CommercialProductsData
                    {
                        ResourceName = EconomyUtils.GetName(d.ResourceName),
                        Demand = d.Demand,
                        Building = d.Building,
                        Free = d.Free,
                        Companies = d.Companies,
                        Workers = d.Workers,
                        SvcFactor = d.SvcFactor,
                        SvcPercent = d.SvcPercent,
                        ResourceNeedPercent = d.ResourceNeedPercent,
                        ResourceNeedPerCompany = d.ResourceNeedPerCompany,
                        WrkPercent = d.WrkPercent,
                        TaxFactor = d.TaxFactor,
                        CurrentTourists = d.CurrentTourists,
                        AvailableLodging = d.AvailableLodging,
                        RequiredRooms = d.RequiredRooms,
                    }).ToArray();
            }
        }
        
        private void SetDemographicsVisibility(bool open)
        {
            _dPVBinding.Update(open);
            m_Demographics.IsPanelVisible = open;
            
            if (open)
            {
                var demographics = World.GetExistingSystemManaged<Demographics>();
                 m_PopulationAtAgeInfoBinding.Value = demographics.m_Results.ToArray();
                 m_DemoAgeGroupingToggledOnBinding.TriggerUpdate();
                if (m_DemoStatsToggledOnBinding.value)
                {
                    m_TotalsBinding.Value = m_Demographics.m_Totals.ToArray();
                    m_OldCitizenBinding.Update();
                }
                
                m_Demographics.ForceUpdateOnce();
            }
        }
        
        private void SetDistrictDataVisibility(bool open)
        {
            _dDPVBinding.Update(open);
            m_DistrictDataSystem.IsPanelVisible = open;

            if (open)
            {
                
                m_uiDistricts.Update();
            }
        }
        
        private void SetIndustrialDemandVisibility(bool open)
        {
            _iDPVBinding.Update(open);
            m_IndustrialSystem.IsPanelVisible = open;
            
            if (open)
            {
                
                m_IndustrialSystem.ForceUpdateOnce();
                var industrialSystem = World.GetOrCreateSystemManaged<IndustrialSystem>();
                m_IndustrialBinding.Value = industrialSystem.m_Results.ToArray();
                m_IndustrialExcludedResourcesBinding.Value =
                    industrialSystem.m_ExcludedResources.value == Resource.NoResource
                    ? new string[0]
                    : IndustrialExtractExcludedResources(industrialSystem.m_ExcludedResources.value);
            }
        }
        
        private void SetIndustrialProductsVisibility(bool open)
        {
            _iPPVBinding.Update(open);
            m_IndustrialProductsSystem.IsPanelVisible = open;
            
            if (open)
            {
                m_IndustrialProductBinding.Value = IndustrialProductsSystem.m_DemandData
                    .Where(d => d.Demand > 0 || d.Companies > 0 || d.Building > 0 || d.Free > 0)
                    .Select(d => new IndustrialProductsData
                    {
                        ResourceName = EconomyUtils.GetName(d.ResourceName),
                        Demand = d.Demand,
                        Building = d.Building,
                        Free = d.Free,
                        Companies = d.Companies,
                        Workers = d.Workers,
                        SvcPercent = d.SvcPercent,
                        WrkPercent = d.WrkPercent,
                        TaxFactor = d.TaxFactor,
                        CapPercent = d.CapPercent,
                        CapPerCompany = d.CapPerCompany,
                    }).ToArray();
            }
        }
        
        private void SetResidentialDemandVisibility(bool open)
        {
            _rDPVBinding.Update(open);
            m_ResidentialSystem.IsPanelVisible = open;
            
            if (open)
            {
                
                
                var residentialSystem = World.GetOrCreateSystemManaged<ResidentialSystem>();
                m_ResidentialBinding.Value = residentialSystem.m_Results.ToArray();
            }
        }
        private void SetTradeCostsVisibility(bool open)
        {
            _tCPVBinding.Update(open);
            m_TradeCostSystem.IsPanelVisible = open;
            if (open)
            {
                // Get current sorting values and apply them to the system
                BuyCostEnum buyCostSorting = m_BuyCostSortingBinding.Value;
                SellCostEnum sellCostSorting = m_SellCostSortingBinding.Value;
                ImportAmountEnum importAmountSorting = m_ImportAmountSortingBinding.Value;
                ExportAmountEnum exportAmountSorting = m_ExportAmountSortingBinding.Value;
                ProfitEnum profitSorting = m_ProfitSortingBinding.Value;
                ProfitMarginEnum profitMarginSorting = m_ProfitMarginSortingBinding.Value;
                ResourceNameEnum resourceNameSorting = m_ResourceNameSortingBinding.Value;
                
                // Set the current sorting values in the data system
                m_TradeCostSystem.m_BuyCostEnum = buyCostSorting;
                m_TradeCostSystem.m_sellCostEnum = sellCostSorting;
                m_TradeCostSystem.m_importAmountEnum = importAmountSorting;
                m_TradeCostSystem.m_exportAmountEnum = exportAmountSorting;
                m_TradeCostSystem.m_profitEnum = profitSorting;
                m_TradeCostSystem.m_profitMarginEnum = profitMarginSorting;
                m_TradeCostSystem.m_resourceNameEnum = resourceNameSorting;
                
                // Get the sorted data and update UI
                m_TradeCostsBinding.Value = m_TradeCostSystem.GetSortedResourceTradeCosts().ToList();
            }
        }
        
        private void SetWorkforceVisibility(bool open)
        {
            _wFPVBinding.Update(open);
            m_WorkforceSystem.IsPanelVisible = open;
            
            if (open)
            {
               
                m_WorkforceSystem.ForceUpdateOnce();
                var workforceSystem = World.GetOrCreateSystemManaged<WorkforceSystem>();
                m_WorkforcesBinder.Value = workforceSystem.m_Results.ToArray();
            }
        }
        
        private void SetWorkplacesVisibility(bool open)
        {
            _wPPVBinding.Update(open);
            m_WorkplacesSystem.IsPanelVisible = open;
            
            if (open)
            {
                
                m_WorkplacesSystem.ForceUpdateOnce();
                var workplacesSystem = World.GetOrCreateSystemManaged<WorkplacesSystem>();
                m_WorkplacesBinder.Value = workplacesSystem.m_Results.ToArray();
            }
        }
        private void SetDemoStatsVisibility(bool on)
        {
            m_DemoStatsToggledOnBinding.Update(on);
            if (on)
            {
                if (m_DemoStatsToggledOnBinding.value)
                {
                    m_TotalsBinding.Value = m_Demographics.m_Totals.ToArray();
                    m_OldCitizenBinding.Update();
                }
            }
        }
        private void SetDemoAgeGroupingVisibility(bool on) {m_DemoAgeGroupingToggledOnBinding.Update(on);}
        private void SetCommercialCompanyDebugVisibility(bool open)
        {
            _cCDPVBinding.Update(open);
            m_CommercialCompanyDataSystem.IsPanelVisible = open;

            if (open)
            {
                m_uiDebugData.Value = m_CommercialCompanyDataSystem.m_CommercialCompanyDTOs;
            }
        }
        private void SetIndustrialCompanyDebugVisibility(bool open)
        {
            _iCDVBinding.Update(open);
            m_IndustrialCompanySystem.IsPanelVisible = open;

            if (open)
            {
                m_uiDebugData2.Value = m_IndustrialCompanySystem.m_IndustrialCompanyDTOs;
            }
        }    
        private string[] ExtractExcludedResources(Resource excludedResources)
        {
            List<string> excludedResourceNames = new List<string>();
            if (excludedResources == Resource.All)
            {
                return new string[] { Resource.All.ToString() };
            }
            Resource[] resources = (Resource[])Enum.GetValues(typeof(Resource));
            for (int i = 0; i < resources.Length; i++)
            {
                Resource resource = resources[i];
                if ((excludedResources & resource) != 0 &&
                    resource != Resource.NoResource &&
                    resource != Resource.All)
                {
                    excludedResourceNames.Add(resource.ToString());
                }
            }

            return excludedResourceNames.ToArray();
        }

        private string[] IndustrialExtractExcludedResources(Resource excludedResources)
        {
            List<string> excludedResourceNames = new List<string>();
            if (excludedResources == Resource.All)
            {
                return new string[] { Resource.All.ToString() };
            }
            Resource[] resources = (Resource[])Enum.GetValues(typeof(Resource));
            for (int i = 0; resources.Length > i; i++)
            {
                Resource resource = resources[i];
                if ((excludedResources & resource) != 0 &&
                    resource != Resource.NoResource &&
                    resource != Resource.All)
                {
                    excludedResourceNames.Add(resource.ToString());
                }
            }

            return excludedResourceNames.ToArray();
        }
        private void SetDemoGroupingStrategy(GroupingStrategy strategy)
        {
            m_DemoGroupingStrategyBinding.Value = strategy;
            
            // Apply the selected strategy in the Demographics system
            var demographics = World.GetExistingSystemManaged<Demographics>();
            demographics.UpdateStrategy(strategy);
            
            // Update UI with grouped data (needs a new binding)
            UpdateGroupedDemographicsData(strategy);
        }
        private void UpdateGroupedDemographicsData(GroupingStrategy strategy)
        {
            var demographics = World.GetExistingSystemManaged<Demographics>();
            //m_GroupedPopulationBinding.Value = demographics.GetPopulationByAgeGroups(strategy).ToArray();
        }
        public void NavigateTo(Entity entity)
        {
            if (m_CameraUpdateSystem.orbitCameraController != null && entity != Entity.Null)
            {
                m_CameraUpdateSystem.orbitCameraController.followedEntity = entity;
                m_CameraUpdateSystem.orbitCameraController.TryMatchPosition(m_CameraUpdateSystem.activeCameraController);
               m_CameraUpdateSystem.activeCameraController = m_CameraUpdateSystem.orbitCameraController;
            }
        }
        private void SetTrafficDataVisibility(bool open)
        {
            _TrafficDataVisibleBinding.Update(open);
            if (open)
            {
                m_uiTrafficData.Update();
            }
        }
    }
}
