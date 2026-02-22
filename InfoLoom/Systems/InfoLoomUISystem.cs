using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Entities;
using Colossal.Logging;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game;
using Game.Areas;
using Game.Economy;
using Game.Prefabs;
using Game.Rendering;
using Game.Simulation;
using Game.UI;
using InfoLoomTwo.Systems.ResidentialData;
using InfoLoomTwo.Domain;
using InfoLoomTwo.Domain.DataDomain;
using InfoLoomTwo.Domain.DataDomain.Enums;
using InfoLoomTwo.Domain.DataDomain.Enums.CompanyPanelEnums;
using InfoLoomTwo.Extensions;
using InfoLoomTwo.Systems.CommercialSystems.CommercialCompanyDebugData;
using InfoLoomTwo.Systems.CommercialSystems.CommercialDemandData;
using InfoLoomTwo.Systems.DemographicsData;
using InfoLoomTwo.Systems.IndustrialSystems.IndustrialCompanyData;
using InfoLoomTwo.Systems.IndustrialSystems.IndustrialCompanyData.IndustrialCompanyDomain;
using InfoLoomTwo.Systems.IndustrialSystems.IndustrialDemandData;
using InfoLoomTwo.Systems.TradeCostData;
using InfoLoomTwo.Systems.WorkforceData;
using InfoLoomTwo.Systems.WorkplacesData;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

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
        private const string WorkforceOpen = "WorkforceOpen";
        private const string WorkplacesOpen = "WorkplacesOpen";
        private const string CommercialCompanyDebugOpen = "CommercialCompanyDebugOpen";
        private const string IndustrialCompanyDebugOpen = "IndustrialCompanyDebugOpen";
        private const string EffectsOpen = "EffectsOpen";
        public static Entity CityWide { get; } = Entity.Null;
        public Entity selectedDistrict { get; set; } = CityWide;
        private EntityQuery m_DistrictQuery;
        private DistrictInfos _DistrictInfos = new DistrictInfos();
        private RawValueBinding m_DistrictInfos;
        
        public static Resource ShowAllResource { get; } = Resource.All;
        public Resource selectedResource { get; set; } = ShowAllResource;
        private EntityQuery m_ResourceQuery;
        
        private RawValueBinding m_ResourceInfos;
        private ValueBindingHelper<float> positionXBinding;
        private ValueBindingHelper<float> positionYBinding;
        
        
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
        private ResourceSystem m_ResourceSystem; // new
        private ImageSystem m_ImageSystem;       // new
        private ResidentialDemandSystem m_ResidentialDemandSystem;
        private CommercialDemandSystem m_CommercialDemandSystem;
        private IndustrialDemandSystem m_IndustrialDemandSystem;
        
        private CommercialSystem m_CommercialSystem;
        private CommercialCompanyDataSystem m_CommercialCompanyDataSystem;
        private CameraUpdateSystem m_CameraUpdateSystem;
        private Demographics m_Demographics;
        private IndustrialSystem m_IndustrialSystem;
        private IndustrialCompanySystem m_IndustrialCompanySystem;
        private ResidentialSystem m_ResidentialSystem;
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
        
        private ValueBindingHelper<SortingEnum> m_CommercialNameSortingBinding;
        private ValueBindingHelper<SortingEnum> m_CommercialIndexSortingBinding;
        private ValueBindingHelper<SortingEnum> m_CommercialServiceUsageSortingBinding;
        private ValueBindingHelper<SortingEnum> m_CommercialEmployeesSortingBinding;
        private ValueBindingHelper<SortingEnum> m_CommercialEfficiencySortingBinding;
        private ValueBindingHelper<SortingEnum> m_CommercialProfitabilitySortingBinding;
        private ValueBindingHelper<SortingEnum> m_CommercialResourceAmountSortingBinding;
        private ValueBindingHelper<SortingEnum> m_CommercialMoneySortingBinding;
        private ValueBindingHelper<SortingEnum> m_CommercialInput1SortingBinding;
        private ValueBindingHelper<SortingEnum> m_CommercialInput2SortingBinding;
        private ValueBindingHelper<SortingEnum> m_CommercialOutputSortingBinding;
        private ValueBindingHelper<SortingEnum> m_CommercialMaintenanceSortingBinding;
        
        private ValueBindingHelper<string[]> listOfCCompanyNamesBinding;
        private ValueBindingHelper<string> m_SelectedCCompanyNameBinding;
        private ValueBindingHelper<string[]> listOfCInput1ResourcesBinding;
        private ValueBindingHelper<string[]> listOfCOutputResourcesBinding;
        private ValueBindingHelper<string> m_SelectedCInput1ResourceBinding;
        private ValueBindingHelper<string> m_SelectedCOutputResourceBinding;
        
        public CommercialCompanyDTO[] m_FilteredCommercialCompanies;
        private CommercialCompanyDTO[] m_CommercialResourceFilteredCompanies; // Base set after resource filtering, before company name filtering
        private string m_LastSelectedCCompanyName = "";
        private SortingEnum m_LastCommercialIndexSorting = SortingEnum.Off;
        private SortingEnum m_LastCommerciaNameSorting = SortingEnum.Off;
        private SortingEnum m_LastCommercialEmployeesSorting = SortingEnum.Off;
        private SortingEnum m_LastCommercialEfficiencySorting = SortingEnum.Off;
        private SortingEnum m_LastCommercialProfitabilitySorting = SortingEnum.Off;
        private SortingEnum m_LastCommercialResourceAmountSorting = SortingEnum.Off;
        private SortingEnum m_LastCommercialMoneySorting = SortingEnum.Off;
        private SortingEnum m_LastCommercialInput1Sorting = SortingEnum.Off;
        private SortingEnum m_LastCommercialOutputSorting = SortingEnum.Off;
        private SortingEnum m_LastCommercialMaintenanceSorting = SortingEnum.Off;
        private SortingEnum m_LastCommercialServiceUsageSorting = SortingEnum.Off;
        private string m_LastSelectedCInput1Resource = "";
        private string m_LastSelectedCOutputResource = "";
        private int m_LastCommercialDataCount = 0;
        
        
        
        
        
        
        //HouseholdData
        //DemographicsUI
       public GetterValueBinding<int> m_OldCitizenBinding;
        private ValueBindingHelper<int[]> m_TotalsBinding;
        public ValueBinding<bool> m_DemoStatsToggledOnBinding;
        private ValueBinding<bool> m_DemoAgeGroupingToggledOnBinding;
        private ValueBindingHelper<GroupingStrategy> m_DemoGroupingStrategyBinding;
         private ValueBinding<Entity> m_SelectedDistrict;
        private ValueBindingHelper<Demographics1> m_Demographics1Binding;
        private ValueBindingHelper<Demographics2> m_Demographics2Binding;
        private ValueBindingHelper<bool> _RefreshDataBinding;
        private ValueBindingHelper<int[]> m_DemographicsLifecycleTotalsBinding;        
        
        private ValueBindingHelper<PopulationDetailedGroupInfo[]> m_DemographicsDetailedGroupDetailsBinding;
        private ValueBindingHelper<PopulationFiveYearGroupInfo[]> m_DemographicsFiveYearDetailsBinding;
        private ValueBindingHelper<PopulationTenYearGroupInfo[]> m_DemographicsTenYearDetailsBinding;
        private ValueBindingHelper<PopulationLifecycleInfo[]> m_DemographicsLifecycleDetailsBinding;
         
         
         
         
         private ValueBinding<int> m_SelectedResource;

        private RawValueBinding m_DistrictInfosBinding;
//DistrictDataUI
        
        //IndustrialDemandDataUI
        private ValueBindingHelper<string[]> m_IndustrialExcludedResourcesBinding;
        private ValueBindingHelper<int[]> m_IndustrialBinding;
        //IndustrialProductsDataUI
        //IndustrialCompanyDebugDataUI
        private ValueBindingHelper<IndustrialCompanyDTO[]> m_uiDebugData2;
        private ValueBinding<bool> _iCDVBinding;
        private ValueBindingHelper<SortingEnum> m_IndustrialNameSortingBinding;
        private ValueBindingHelper<SortingEnum> m_IndustrialIndexSortingBinding;
        private ValueBindingHelper<SortingEnum> m_IndustrialEmployeesSortingBinding;
        private ValueBindingHelper<SortingEnum> m_IndustrialEfficiencySortingBinding;
        private ValueBindingHelper<SortingEnum> m_IndustrialProfitabilitySortingBinding;
        private ValueBindingHelper<SortingEnum> m_IndustrialResourceAmountSortingBinding;
        private ValueBindingHelper<SortingEnum> m_IndustrialMoneySortingBinding;
        private ValueBindingHelper<SortingEnum> m_IndustrialInput1SortingBinding;
        private ValueBindingHelper<SortingEnum> m_IndustrialInput2SortingBinding;
        private ValueBindingHelper<SortingEnum> m_IndustrialOutputSortingBinding;
        private ValueBindingHelper<SortingEnum> m_IndustrialMaintenanceSortingBinding;
        private ValueBindingHelper<string[]> listOfCompanyNamesBinding;
        private ValueBindingHelper<string> m_SelectedCompanyNameBinding;
        private ValueBindingHelper<string[]> listOfInput1ResourcesBinding;
        private ValueBindingHelper<string[]> listOfInput2ResourcesBinding;
        private ValueBindingHelper<string[]> listOfOutputResourcesBinding;
        private ValueBindingHelper<string> m_SelectedInput1ResourceBinding;
        private ValueBindingHelper<string> m_SelectedInput2ResourceBinding;
        private ValueBindingHelper<string> m_SelectedOutputResourceBinding;
        
        public IndustrialCompanyDTO[] m_FilteredIndustrialCompanies;
        private IndustrialCompanyDTO[] m_ResourceFilteredCompanies; // Base set after resource filtering, before company name filtering
        private string m_LastSelectedCompanyName = "";
        private SortingEnum m_LastIndustrialIndexSorting = SortingEnum.Off;
        private SortingEnum m_LastIndustrialNameSorting = SortingEnum.Off;
        private SortingEnum m_LastIndustrialEmployeesSorting = SortingEnum.Off;
        private SortingEnum m_LastIndustrialEfficiencySorting = SortingEnum.Off;
        private SortingEnum m_LastIndustrialProfitabilitySorting = SortingEnum.Off;
        private SortingEnum m_LastIndustrialResourceAmountSorting = SortingEnum.Off;
        private SortingEnum m_LastIndustrialMoneySorting = SortingEnum.Off;
        private SortingEnum m_LastIndustrialInput1Sorting = SortingEnum.Off;
        private SortingEnum m_LastIndustrialInput2Sorting = SortingEnum.Off;
        private SortingEnum m_LastIndustrialOutputSorting = SortingEnum.Off;
        private SortingEnum m_LastIndustrialMaintenanceSorting = SortingEnum.Off;
        private string m_LastSelectedInput1Resource = "";
        private string m_LastSelectedInput2Resource = "";
        private string m_LastSelectedOutputResource = "";
        private int m_LastIndustrialDataCount = 0;
        
        //ResidentialDemandDataUI
        public ValueBindingHelper<float[]> m_ResidentialBinding;
        

        //TrafficDataUI
        private RawValueBinding m_uiTrafficData;
        
        
        
        
        //WorkforceUI
        private ValueBindingHelper<WorkforcesInfo[]> m_WorkforcesBinder;
        //private ValueBindingHelper<int> hideColumnsBindingWF;
       
        
        
        
        //WorkplacesUI
        private ValueBindingHelper<WorkplacesInfo[]> m_WorkplacesBinder;
        //private ValueBindingHelper<int> hideColumnsBindingWP;
        
        
        
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
        
        private ValueBinding<bool> _iDPVBinding;
        private ValueBinding<bool> _iPPVBinding;
        private ValueBinding<bool> _rDPVBinding;
        private ValueBinding<bool> _wFPVBinding;
        private ValueBinding<bool> _wPPVBinding;
        private ValueBinding<bool> _householdsDataVisibleBinding;
        private ValueBinding<bool> _TrafficDataVisibleBinding;
        private ValueBinding<bool> _effectsVisibleBinding;
        
        
        private ILog m_Log;
        
        public override GameMode gameMode => GameMode.Game;

        protected override void OnCreate()
        {    
            base.OnCreate();
            m_Log = Mod.log;
            _uiUpdateState = UIUpdateState.Create(World, 512);
            m_DistrictQuery = GetEntityQuery(ComponentType.ReadOnly<District>());
            m_ResourceQuery = GetEntityQuery(ComponentType.ReadOnly<Resources>());
            m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
            m_NameSystem = World.GetOrCreateSystemManaged<NameSystem>();
            m_ResidentialDemandSystem = base.World.GetOrCreateSystemManaged<ResidentialDemandSystem>();
            m_CommercialDemandSystem = base.World.GetOrCreateSystemManaged<CommercialDemandSystem>();
            m_IndustrialDemandSystem = base.World.GetOrCreateSystemManaged<IndustrialDemandSystem>();
            m_CommercialCompanyDataSystem = base.World.GetOrCreateSystemManaged<CommercialCompanyDataSystem>();
            m_CommercialSystem = base.World.GetOrCreateSystemManaged<CommercialSystem>();
            m_Demographics = base.World.GetOrCreateSystemManaged<Demographics>();
            m_IndustrialSystem = base.World.GetOrCreateSystemManaged<IndustrialSystem>();
            m_ResidentialSystem = base.World.GetOrCreateSystemManaged<ResidentialSystem>();
            m_WorkforceSystem = base.World.GetOrCreateSystemManaged<WorkforceSystem>();
            m_WorkplacesSystem = base.World.GetOrCreateSystemManaged<WorkplacesSystem>();
            m_CameraUpdateSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<CameraUpdateSystem>();
            m_IndustrialCompanySystem = World.GetOrCreateSystemManaged<IndustrialCompanySystem>();
            m_ResourceSystem = World.GetOrCreateSystemManaged<ResourceSystem>();
            m_ImageSystem = World.GetOrCreateSystemManaged<ImageSystem>();
            _DistrictInfos = new DistrictInfos();
            
            
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
            
             _dPVBinding = new ValueBinding<bool>(ModID, DemographicsOpen, false);
             AddBinding(_dPVBinding);
             AddBinding(new TriggerBinding<bool>(ModID, DemographicsOpen, SetDemographicsVisibility));
             
             _iDPVBinding = new ValueBinding<bool>(ModID, IndustrialDemandOpen, false);
             AddBinding(_iDPVBinding);
             AddBinding(new TriggerBinding<bool>(ModID, IndustrialDemandOpen, SetIndustrialDemandVisibility));
            
            
             _rDPVBinding = new ValueBinding<bool>(ModID, ResidentialDemandOpen, false);
             AddBinding(_rDPVBinding);
             AddBinding(new TriggerBinding<bool>(ModID, ResidentialDemandOpen, SetResidentialDemandVisibility));
             
             _wFPVBinding = new ValueBinding<bool>(ModID, WorkforceOpen, false);
             AddBinding(_wFPVBinding);
             AddBinding(new TriggerBinding<bool>(ModID, WorkforceOpen, SetWorkforceVisibility));
            
             _wPPVBinding = new ValueBinding<bool>(ModID, WorkplacesOpen, false);
             AddBinding(_wPPVBinding);
             AddBinding(new TriggerBinding<bool>(ModID, WorkplacesOpen, SetWorkplacesVisibility));
             
             _TrafficDataVisibleBinding = new ValueBinding<bool>(ModID, "TrafficDataVisible", false);
                AddBinding(_TrafficDataVisibleBinding);
               AddBinding(new TriggerBinding<bool>(ModID, "TrafficDataVisible", SetTrafficDataVisibility));

             _effectsVisibleBinding = new ValueBinding<bool>(ModID, EffectsOpen, false);
             AddBinding(_effectsVisibleBinding);
             AddBinding(new TriggerBinding<bool>(ModID, EffectsOpen, SetEffectsVisibility));
             
              

            positionXBinding = CreateBinding("LoadPositionX", 0f);
            positionYBinding = CreateBinding("LoadPositionY", 0f);
             
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
             
             m_CommercialIndexSortingBinding = CreateBinding("CommercialIndexSorting", "SetCommercialIndexSorting", SortingEnum.Off);
             m_CommercialNameSortingBinding = CreateBinding("CommercialNameSorting", "SetCommercialNameSorting", SortingEnum.Off);
             m_CommercialServiceUsageSortingBinding = CreateBinding("CommercialServiceUsageSorting", "SetCommercialServiceUsageSorting", SortingEnum.Off);
             m_CommercialEmployeesSortingBinding = CreateBinding("CommercialEmployeesSorting", "SetCommercialEmployeesSorting", SortingEnum.Off);
             m_CommercialEfficiencySortingBinding = CreateBinding("CommercialEfficiencySorting", "SetCommercialEfficiencySorting", SortingEnum.Off);
             m_CommercialProfitabilitySortingBinding = CreateBinding("CommercialProfitabilitySorting", "SetCommercialProfitabilitySorting", SortingEnum.Off);
             m_CommercialResourceAmountSortingBinding = CreateBinding("CommercialResourceAmountSorting", "SetCommercialResourceAmountSorting", SortingEnum.Off);
             m_CommercialMoneySortingBinding = CreateBinding("CommercialMoneySorting", "SetCommercialMoneySorting", SortingEnum.Off);
             m_CommercialInput1SortingBinding = CreateBinding("CommercialInput1Sorting", "SetCommercialInput1Sorting", SortingEnum.Off);
             m_CommercialInput2SortingBinding = CreateBinding("CommercialInput2Sorting", "SetCommercialInput2Sorting", SortingEnum.Off);
             m_CommercialOutputSortingBinding = CreateBinding("CommercialOutputSorting", "SetCommercialOutputSorting", SortingEnum.Off);
             m_CommercialMaintenanceSortingBinding = CreateBinding("CommercialMaintenanceSorting", "SetCommercialMaintenanceSorting", SortingEnum.Off);
             listOfCCompanyNamesBinding = CreateBinding("listOfCommercialCompanyNames",  new string[0]);
             m_SelectedCCompanyNameBinding = CreateBinding("selectedCommercialCompanyName", "SetSelectedCommercialCompanyName", "All Companies", SetSelectedCommercialCompanyName);
             listOfCInput1ResourcesBinding = CreateBinding("listOfCommercialInput1Resources",  new string[0]);
             listOfCOutputResourcesBinding = CreateBinding("listOfCommercialOutputResources",  new string[0]);  
             m_SelectedCInput1ResourceBinding = CreateBinding("selectedCommercialInput1Resource", "SetSelectedCommercialInput1Resource", "All", SetSelectedCommercialInput1Resource);
            m_SelectedCOutputResourceBinding = CreateBinding("selectedCommercialOutputResource", "SetSelectedCommercialOutputResource", "All", SetSelectedCommercialOutputResource);

             
            //DemographicsUI
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
            AddBinding(m_SelectedDistrict = new ValueBinding<Entity>(ModID, "selectedDistrict", CityWide));
            AddBinding(new TriggerBinding<Entity>(ModID, "selectedDistrictChanged", SelectedDistrictChanged));
            AddBinding(m_DistrictInfos = new RawValueBinding(ModID, "districtInfos", UpdateDistrictInfos));
            m_Demographics1Binding = CreateBinding("Demographics1", "SetDemographics1", Demographics1.All);
            m_Demographics2Binding = CreateBinding("Demographics2", "SetDemographics2", Demographics2.All);
            _RefreshDataBinding = CreateBinding("demographics", "updateDemographics", false, UpdateDemographicsData);
            m_DemographicsLifecycleTotalsBinding = CreateBinding("DemographicsLifecycleTotals", new int[4]);            
			m_DemographicsLifecycleDetailsBinding = CreateBinding("DemographicsLifecycleDetails", new PopulationLifecycleInfo[0]);
            m_DemographicsDetailedGroupDetailsBinding = CreateBinding("DemographicsDetailedData", new PopulationDetailedGroupInfo[0]);
            m_DemographicsFiveYearDetailsBinding = CreateBinding("DemographicsFiveYearDetails", new PopulationFiveYearGroupInfo[0]);
            m_DemographicsTenYearDetailsBinding = CreateBinding("DemographicsTenYearDetails", new PopulationTenYearGroupInfo[0]);
            
			
            
            
            
            //DistrictDataUI
            // First binding: Basic district information
            


            //IndustrialDemandDataUI
            m_IndustrialBinding = CreateBinding("IndustrialData", new int[16]);
            m_IndustrialExcludedResourcesBinding = CreateBinding("IndustrialDataExRes", new string[0]);

            //IndustrialProductsDataUI
            
            //IndustrialCompanyDebugDataUI
            _iCDVBinding = new ValueBinding<bool>(ModID, IndustrialCompanyDebugOpen, false);
             AddBinding(_iCDVBinding);
             AddBinding(new TriggerBinding<bool>(ModID, IndustrialCompanyDebugOpen, SetIndustrialCompanyDebugVisibility));
             m_uiDebugData2 = CreateBinding("IndustrialCompanyDebugData", new IndustrialCompanyDTO[0]);
             
            m_IndustrialIndexSortingBinding = CreateBinding("IndustrialIndexSorting", "SetIndustrialIndexSorting", SortingEnum.Off);
            m_IndustrialNameSortingBinding = CreateBinding("IndustrialNameSorting", "SetIndustrialNameSorting", SortingEnum.Off);
            m_IndustrialEmployeesSortingBinding = CreateBinding("IndustrialEmployeesSorting", "SetIndustrialEmployeesSorting", SortingEnum.Off);
            m_IndustrialEfficiencySortingBinding = CreateBinding("IndustrialEfficiencySorting", "SetIndustrialEfficiencySorting", SortingEnum.Off);
            m_IndustrialProfitabilitySortingBinding = CreateBinding("IndustrialProfitabilitySorting", "SetIndustrialProfitabilitySorting", SortingEnum.Off);
            m_IndustrialResourceAmountSortingBinding = CreateBinding("IndustrialResourceAmountSorting", "SetIndustrialResourceAmountSorting", SortingEnum.Off);
            m_IndustrialMoneySortingBinding = CreateBinding("IndustrialMoneySorting", "SetIndustrialMoneySorting", SortingEnum.Off);
            m_IndustrialInput1SortingBinding = CreateBinding("IndustrialInput1Sorting", "SetIndustrialInput1Sorting", SortingEnum.Off);
            m_IndustrialInput2SortingBinding = CreateBinding("IndustrialInput2Sorting", "SetIndustrialInput2Sorting", SortingEnum.Off);
            m_IndustrialOutputSortingBinding = CreateBinding("IndustrialOutputSorting", "SetIndustrialOutputSorting", SortingEnum.Off);
            m_IndustrialMaintenanceSortingBinding = CreateBinding("IndustrialMaintenanceSorting", "SetIndustrialMaintenanceSorting", SortingEnum.Off);
            listOfCompanyNamesBinding = CreateBinding("listOfCompanyNames",  new string[0]);
            m_SelectedCompanyNameBinding = CreateBinding("selectedCompanyName", "SetSelectedCompanyName", "All Companies", SetSelectedCompanyName);
            listOfInput1ResourcesBinding = CreateBinding("listOfInput1Resources",  new string[0]);
            listOfInput2ResourcesBinding = CreateBinding("listOfInput2Resources",  new string[0]);
            listOfOutputResourcesBinding = CreateBinding("listOfOutputResources",  new string[0]);
            m_SelectedInput1ResourceBinding = CreateBinding("selectedInput1Resource", "SetSelectedInput1Resource", "All", SetSelectedInput1Resource);
            m_SelectedInput2ResourceBinding = CreateBinding("selectedInput2Resource", "SetSelectedInput2Resource", "All", SetSelectedInput2Resource);
            m_SelectedOutputResourceBinding = CreateBinding("selectedOutputResource", "SetSelectedOutputResource", "All", SetSelectedOutputResource);
            
            //ResidentialDemandDataUI
            m_ResidentialBinding = CreateBinding("ResidentialData", new float[21]);
            
            //WorkforceUI
            m_WorkforcesBinder = CreateBinding("WorkforceData", new WorkforcesInfo[0]);
                
            //WorkplacesUI
            m_WorkplacesBinder = CreateBinding("WorkplacesData", new WorkplacesInfo[0]);
            //hideColumnsBindingWP = CreateBinding("ShowExtraWorkplaces", 0);

           
            
        }
        
        

        protected override void OnUpdate()
        {
            CheckForDistrictChange();
            //CheckForResource();
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
                    commercialSystem.m_IncludedResources.value == Resource.NoResource
                    ? new string[0]
                    : UIUtil.ExtractExcludedResources(commercialSystem.m_IncludedResources.value);
                m_CommercialSystem.IsPanelVisible = true;
            }

            
            if (_cCDPVBinding.value)
            {
                    m_CommercialCompanyDataSystem.IsPanelVisible = true;
                    
                    int currentDataCount = m_CommercialCompanyDataSystem.m_CommercialCompanyDTOs?.Length ?? 0;
                    if (currentDataCount != m_LastCommercialDataCount)
                    {
                        listOfCCompanyNamesBinding.Value = m_CommercialCompanyDataSystem.GetAllCompanyNames();
                        listOfCInput1ResourcesBinding.Value = m_CommercialCompanyDataSystem.GetAllInput1Resources();
                        listOfCOutputResourcesBinding.Value = m_CommercialCompanyDataSystem.GetAllOutputResources();
                        m_LastCommercialDataCount = currentDataCount;
                    }
                    
                    // Get the current sorting values from bindings
                    SortingEnum companyNameSorting = m_CommercialNameSortingBinding.Value;
                    SortingEnum efficiencySorting = m_CommercialEfficiencySortingBinding.Value;
                    SortingEnum indexSorting = m_CommercialIndexSortingBinding.Value;
                    SortingEnum serviceUsageSorting = m_CommercialServiceUsageSortingBinding.Value;
                    SortingEnum employeesSorting = m_CommercialEmployeesSortingBinding.Value;
                    SortingEnum profitabilitySorting = m_CommercialProfitabilitySortingBinding.Value;
                    SortingEnum resourceAmountSorting = m_CommercialResourceAmountSortingBinding.Value;
                    SortingEnum moneySorting = m_CommercialMoneySortingBinding.Value;
                    SortingEnum input1Sorting = m_CommercialInput1SortingBinding.Value;
                    SortingEnum input2Sorting = m_CommercialInput2SortingBinding.Value;
                    SortingEnum outputSorting = m_CommercialOutputSortingBinding.Value;
                    SortingEnum maintenanceSorting = m_CommercialMaintenanceSortingBinding.Value;
                    
                    
                    string selectedCompanyName = m_SelectedCCompanyNameBinding.Value;
                    string selectedInput1Resource = m_SelectedCInput1ResourceBinding.Value;
                    string selectedOutputResource = m_SelectedCOutputResourceBinding.Value;
                    
                     bool sortingChanged = 
                    companyNameSorting != m_LastCommerciaNameSorting ||
                    efficiencySorting != m_LastCommercialEfficiencySorting ||
                    indexSorting != m_LastCommercialIndexSorting ||
                    employeesSorting != m_LastCommercialEmployeesSorting ||
                    profitabilitySorting != m_LastCommercialProfitabilitySorting ||
                    resourceAmountSorting != m_LastCommercialResourceAmountSorting ||
                    moneySorting != m_LastCommercialMoneySorting ||
                    input1Sorting != m_LastCommercialInput1Sorting ||
                    outputSorting != m_LastCommercialOutputSorting ||
                    maintenanceSorting != m_LastCommercialMaintenanceSorting ||
                    selectedCompanyName != m_LastSelectedCCompanyName || 
                    selectedInput1Resource != m_LastSelectedCInput1Resource ||
                    selectedOutputResource != m_LastSelectedCOutputResource;
                    
                     if(sortingChanged)
                     {
                         // Set the current sorting values in the data system
                        m_CommercialCompanyDataSystem.m_CurrentIndexSorting = indexSorting;
                        m_CommercialCompanyDataSystem.m_CurrentCompanyNameSorting = companyNameSorting;
                        m_CommercialCompanyDataSystem.m_CurrentServiceUsageSorting = serviceUsageSorting;
                        m_CommercialCompanyDataSystem.m_CurrentEmployeesSorting = employeesSorting;
                        m_CommercialCompanyDataSystem.m_CurrentEfficiencySorting = efficiencySorting;
                        m_CommercialCompanyDataSystem.m_CurrentProfitabilitySorting = profitabilitySorting;
                        m_CommercialCompanyDataSystem.m_CurrentResourceAmountSorting = resourceAmountSorting;
                        m_CommercialCompanyDataSystem.m_CurrentMoneySorting = moneySorting;
                        m_CommercialCompanyDataSystem.m_CurrentInput1Sorting = input1Sorting;
                        m_CommercialCompanyDataSystem.m_CurrentInput2Sorting = input2Sorting;
                        m_CommercialCompanyDataSystem.m_CurrentOutputSorting = outputSorting;
                        m_CommercialCompanyDataSystem.m_CurrentMaintenanceSorting = maintenanceSorting;
                        
                        // Update the UI to reflect current sorting values
                        m_CommercialNameSortingBinding.UpdateCallback(companyNameSorting);
                        m_CommercialEfficiencySortingBinding.UpdateCallback(efficiencySorting);
                        m_CommercialIndexSortingBinding.UpdateCallback(indexSorting);
                        m_CommercialServiceUsageSortingBinding.UpdateCallback(serviceUsageSorting);
                        m_CommercialEmployeesSortingBinding.UpdateCallback(employeesSorting);
                        m_CommercialProfitabilitySortingBinding.UpdateCallback(profitabilitySorting);
                        m_CommercialResourceAmountSortingBinding.UpdateCallback(resourceAmountSorting);
                        m_CommercialMoneySortingBinding.UpdateCallback(moneySorting);
                        m_CommercialInput1SortingBinding.UpdateCallback(input1Sorting);
                        m_CommercialInput2SortingBinding.UpdateCallback(input2Sorting);
                        m_CommercialOutputSortingBinding.UpdateCallback(outputSorting);
                        m_CommercialMaintenanceSortingBinding.UpdateCallback(maintenanceSorting);
                        
                        ApplyCommercialSortingToDisplayedData();
                        
                        m_LastCommerciaNameSorting = companyNameSorting;
                        m_LastCommercialEfficiencySorting = efficiencySorting;
                        m_LastCommercialIndexSorting = indexSorting;
                        m_LastCommercialServiceUsageSorting = serviceUsageSorting;
                        m_LastCommercialEmployeesSorting = employeesSorting;
                        m_LastCommercialProfitabilitySorting = profitabilitySorting;
                        m_LastCommercialResourceAmountSorting = resourceAmountSorting;
                        m_LastCommercialMoneySorting = moneySorting;
                        m_LastCommercialInput1Sorting = input1Sorting;
                        m_LastCommercialOutputSorting = outputSorting;
                        m_LastCommercialMaintenanceSorting = maintenanceSorting;
                        m_LastSelectedCCompanyName = selectedCompanyName;
                        m_LastSelectedCInput1Resource = selectedInput1Resource;
                        m_LastSelectedCOutputResource = selectedOutputResource;
                     }
                    
                    
                    
            }
            if (_iCDVBinding.value)
            {
                m_IndustrialCompanySystem.IsPanelVisible = true;
                
                // Only update company names and resource lists when data count changes
                int currentDataCount = m_IndustrialCompanySystem.m_IndustrialCompanyDTOs?.Length ?? 0;
                if (currentDataCount != m_LastIndustrialDataCount)
                {
                    listOfCompanyNamesBinding.Value = m_IndustrialCompanySystem.GetAllCompanyNames();
                    listOfInput1ResourcesBinding.Value = m_IndustrialCompanySystem.GetAllInput1Resources();
                    listOfInput2ResourcesBinding.Value = m_IndustrialCompanySystem.GetAllInput2Resources();
                    listOfOutputResourcesBinding.Value = m_IndustrialCompanySystem.GetAllOutputResources();
                    m_LastIndustrialDataCount = currentDataCount;
                }
                
                // Get current sorting values
                SortingEnum companyNameSorting = m_IndustrialNameSortingBinding.Value;
                SortingEnum efficiencySorting = m_IndustrialEfficiencySortingBinding.Value;
                SortingEnum indexSorting = m_IndustrialIndexSortingBinding.Value;
                SortingEnum employeesSorting = m_IndustrialEmployeesSortingBinding.Value;
                SortingEnum profitabilitySorting = m_IndustrialProfitabilitySortingBinding.Value;
                SortingEnum resourceAmountSorting = m_IndustrialResourceAmountSortingBinding.Value;
                SortingEnum moneySorting = m_IndustrialMoneySortingBinding.Value;
                SortingEnum input1Sorting = m_IndustrialInput1SortingBinding.Value;
                SortingEnum input2Sorting = m_IndustrialInput2SortingBinding.Value;
                SortingEnum outputSorting = m_IndustrialOutputSortingBinding.Value;
                SortingEnum maintenanceSorting = m_IndustrialMaintenanceSortingBinding.Value;
                
                // Get current filter values
                string selectedCompanyName = m_SelectedCompanyNameBinding.Value;
                string selectedInput1Resource = m_SelectedInput1ResourceBinding.Value;
                string selectedInput2Resource = m_SelectedInput2ResourceBinding.Value;
                string selectedOutputResource = m_SelectedOutputResourceBinding.Value;
                
                // Check if any sorting criteria or filters changed
                bool sortingChanged = 
                    companyNameSorting != m_LastIndustrialNameSorting ||
                    efficiencySorting != m_LastIndustrialEfficiencySorting ||
                    indexSorting != m_LastIndustrialIndexSorting ||
                    employeesSorting != m_LastIndustrialEmployeesSorting ||
                    profitabilitySorting != m_LastIndustrialProfitabilitySorting ||
                    resourceAmountSorting != m_LastIndustrialResourceAmountSorting ||
                    moneySorting != m_LastIndustrialMoneySorting ||
                    input1Sorting != m_LastIndustrialInput1Sorting ||
                    input2Sorting != m_LastIndustrialInput2Sorting ||
                    outputSorting != m_LastIndustrialOutputSorting ||
                    maintenanceSorting != m_LastIndustrialMaintenanceSorting ||
                    selectedCompanyName != m_LastSelectedCompanyName || 
                    selectedInput1Resource != m_LastSelectedInput1Resource ||
                    selectedInput2Resource != m_LastSelectedInput2Resource ||
                    selectedOutputResource != m_LastSelectedOutputResource;
                
                // Only update and sort if something changed
                if (sortingChanged)
                {
                    // Set the current sorting values in the data system
                    m_IndustrialCompanySystem.m_CurrentIndexSorting = indexSorting;
                    m_IndustrialCompanySystem.m_CurrentCompanyNameSorting = companyNameSorting;
                    m_IndustrialCompanySystem.m_CurrentEmployeesSorting = employeesSorting;
                    m_IndustrialCompanySystem.m_CurrentEfficiencySorting = efficiencySorting;
                    m_IndustrialCompanySystem.m_CurrentProfitabilitySorting = profitabilitySorting;
                    m_IndustrialCompanySystem.m_CurrentResourceAmountSorting = resourceAmountSorting;
                    m_IndustrialCompanySystem.m_CurrentMoneySorting = moneySorting;
                    m_IndustrialCompanySystem.m_CurrentInput1Sorting = input1Sorting;
                    m_IndustrialCompanySystem.m_CurrentInput2Sorting = input2Sorting;
                    m_IndustrialCompanySystem.m_CurrentOutputSorting = outputSorting;
                    m_IndustrialCompanySystem.m_CurrentMaintenanceSorting = maintenanceSorting;
                    
                    // Update callbacks
                    m_IndustrialIndexSortingBinding.UpdateCallback(indexSorting);
                    m_IndustrialNameSortingBinding.UpdateCallback(companyNameSorting);
                    m_IndustrialEmployeesSortingBinding.UpdateCallback(employeesSorting);
                    m_IndustrialEfficiencySortingBinding.UpdateCallback(efficiencySorting);
                    m_IndustrialProfitabilitySortingBinding.UpdateCallback(profitabilitySorting);
                    m_IndustrialResourceAmountSortingBinding.UpdateCallback(resourceAmountSorting);
                    m_IndustrialMoneySortingBinding.UpdateCallback(moneySorting);
                    m_IndustrialInput1SortingBinding.UpdateCallback(input1Sorting);
                    m_IndustrialInput2SortingBinding.UpdateCallback(input2Sorting);
                    m_IndustrialOutputSortingBinding.UpdateCallback(outputSorting);
                    m_IndustrialMaintenanceSortingBinding.UpdateCallback(maintenanceSorting);
                    
                    // Update with sorted data
                    ApplySortingToDisplayedData();
                    
                    // Cache current values
                    m_LastIndustrialIndexSorting = indexSorting;
                    m_LastIndustrialNameSorting = companyNameSorting;
                    m_LastIndustrialEmployeesSorting = employeesSorting;
                    m_LastIndustrialEfficiencySorting = efficiencySorting;
                    m_LastIndustrialProfitabilitySorting = profitabilitySorting;
                    m_LastIndustrialResourceAmountSorting = resourceAmountSorting;
                    m_LastIndustrialMoneySorting = moneySorting;
                    m_LastIndustrialInput1Sorting = input1Sorting;
                    m_LastIndustrialInput2Sorting = input2Sorting;
                    m_LastIndustrialOutputSorting = outputSorting;
                    m_LastIndustrialMaintenanceSorting = maintenanceSorting;
                    m_LastSelectedCompanyName = selectedCompanyName;
                    m_LastSelectedInput1Resource = selectedInput1Resource;
                    m_LastSelectedInput2Resource = selectedInput2Resource;
                    m_LastSelectedOutputResource = selectedOutputResource;
                }
            }

            //m_Log.Debug($"{nameof(InfoLoomUISystem)}.{nameof(OnUpdate)} 2.");
            if (_dPVBinding.value)
            {
                
                var demographics = World.GetExistingSystemManaged<Demographics>();

                GroupingStrategy currentStrategy = m_DemoGroupingStrategyBinding.Value;
                m_DemographicsLifecycleTotalsBinding.Value = demographics.m_LifecycleTotals.ToArray();
				m_DemographicsLifecycleDetailsBinding.Value = demographics.m_LifecycleDetails.ToArray();
                m_DemographicsDetailedGroupDetailsBinding.Value = demographics.m_Results.ToArray();
                m_DemographicsFiveYearDetailsBinding.Value = demographics.m_FiveYearDetails.ToArray();
                m_DemographicsTenYearDetailsBinding.Value = demographics.m_TenYearDetails.ToArray();
                m_DemoGroupingStrategyBinding.UpdateCallback(currentStrategy);
                if (m_DemoStatsToggledOnBinding.value)
                {
                    m_TotalsBinding.Value = demographics.m_Totals.ToArray();
                    m_OldCitizenBinding.Update();
                }
                m_Demographics.IsPanelVisible = true;
            }
            if (_iDPVBinding.value )
            {
                var industrialSystem = base.World.GetOrCreateSystemManaged<IndustrialSystem>();
                m_IndustrialBinding.Value = industrialSystem.m_Results.ToArray();
                m_ExcludedResourcesBinding.Value =
                    industrialSystem.m_IncludedResources.value == Resource.NoResource
                    ? new string[0]
                    : UIUtil.IndustrialExtractExcludedResources(industrialSystem.m_IncludedResources.value);
                
                m_IndustrialSystem.IsPanelVisible = true;
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
                //hideColumnsBindingWP.Value = Mod.setting.hideNoColumnsWP;
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
                var commercialSystem = World.GetOrCreateSystemManaged<CommercialSystem>();
                m_CommercialBinding.Value = commercialSystem.m_Results.ToArray();
                m_ExcludedResourcesBinding.Value = 
                    commercialSystem.m_IncludedResources.value == Resource.NoResource
                    ? new string[0]
                    : UIUtil.ExtractExcludedResources(commercialSystem.m_IncludedResources.value);
            }
        }
        
        
        private void SetDemographicsVisibility(bool open)
        {
            _dPVBinding.Update(open);
            m_Demographics.IsPanelVisible = open;
            
            if (open)
            {
                var demographics = World.GetExistingSystemManaged<Demographics>();
                m_DemographicsLifecycleTotalsBinding.Value = demographics.m_LifecycleTotals.ToArray();
                m_DemographicsLifecycleDetailsBinding.Value = demographics.m_LifecycleDetails.ToArray();
                m_DemographicsDetailedGroupDetailsBinding.Value = demographics.m_Results.ToArray();
                m_DemographicsFiveYearDetailsBinding.Value = demographics.m_FiveYearDetails.ToArray();
                m_DemographicsTenYearDetailsBinding.Value = demographics.m_TenYearDetails.ToArray();
                m_DemoAgeGroupingToggledOnBinding.TriggerUpdate();
                if (m_DemoStatsToggledOnBinding.value)
                {
                    m_TotalsBinding.Value = demographics.m_Totals.ToArray();
                    m_OldCitizenBinding.Update();
                }
            }
        }
        private void SetIndustrialDemandVisibility(bool open)
        {
            _iDPVBinding.Update(open);
            m_IndustrialSystem.IsPanelVisible = open;
            
            if (open)
            {
                
                var industrialSystem = World.GetOrCreateSystemManaged<IndustrialSystem>();
                m_IndustrialBinding.Value = industrialSystem.m_Results.ToArray();
                m_IndustrialExcludedResourcesBinding.Value =
                    industrialSystem.m_IncludedResources.value == Resource.NoResource
                    ? new string[0]
                    : UIUtil.IndustrialExtractExcludedResources(industrialSystem.m_IncludedResources.value);
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
                m_CommercialCompanyDataSystem.UpdateCommercialStatsWithBurstJob();
                var allCompanies = m_CommercialCompanyDataSystem.GetAllCompanies();
                m_uiDebugData.Value = allCompanies;
                
                listOfCInput1ResourcesBinding.Value = m_CommercialCompanyDataSystem.GetAllInput1Resources();
                listOfCOutputResourcesBinding.Value = m_CommercialCompanyDataSystem.GetAllOutputResources();
                
                listOfCCompanyNamesBinding.Value = m_CommercialCompanyDataSystem.GetAllCompanyNames();
                
                m_SelectedCInput1ResourceBinding.Value = "All";
                m_SelectedCOutputResourceBinding.Value = "All";
                m_SelectedCCompanyNameBinding.Value = "All Companies";
                
                // Clear any previous filtered state
                m_FilteredCommercialCompanies = null;
                m_CommercialResourceFilteredCompanies = null;
            }
        }
        private void SetIndustrialCompanyDebugVisibility(bool open)
        {
            _iCDVBinding.Update(open);
            m_IndustrialCompanySystem.IsPanelVisible = open;

            if (open)
            {
                m_IndustrialCompanySystem.UpdateIndustrialStatsWithBurstJob();
                
                // IMPORTANT: Get all companies first to establish baseline
                var allCompanies = m_IndustrialCompanySystem.GetAllCompanies();
                m_uiDebugData2.Value = allCompanies;
                
                // Populate resource filters from ALL companies
                listOfInput1ResourcesBinding.Value = m_IndustrialCompanySystem.GetAllInput1Resources();
                listOfInput2ResourcesBinding.Value = m_IndustrialCompanySystem.GetAllInput2Resources();
                listOfOutputResourcesBinding.Value = m_IndustrialCompanySystem.GetAllOutputResources();
                
                // Populate company names from ALL companies
                listOfCompanyNamesBinding.Value = m_IndustrialCompanySystem.GetAllCompanyNames();
                
                // Reset all filters to initial state
                m_SelectedInput1ResourceBinding.Value = "All";
                m_SelectedInput2ResourceBinding.Value = "All";
                m_SelectedOutputResourceBinding.Value = "All";
                m_SelectedCompanyNameBinding.Value = "All Companies";
                
                // Clear any previous filtered state
                m_FilteredIndustrialCompanies = null;
                m_ResourceFilteredCompanies = null;
            }
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
        private void SetEffectsVisibility(bool open)
        {
            _effectsVisibleBinding.Update(open);
        }
        private void CheckForDistrictChange()
        {
            // Get district infos and check for changes
            bool foundSelectedDistrict = (selectedDistrict == CityWide);
            DistrictInfos districtInfos = new DistrictInfos();

            NativeArray<Entity> districtEntities = m_DistrictQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity districtEntity in districtEntities)
            {
                string districtName = m_NameSystem.GetRenderedLabelName(districtEntity);
                if (districtName != "Assets.DISTRICT_NAME")
                {
                    districtInfos.Add(new DistrictInfo(districtEntity, districtName));
                    if (districtEntity == selectedDistrict)
                    {
                        foundSelectedDistrict = true;
                    }
                }
            }

            if (!foundSelectedDistrict)
            {
                selectedDistrict = CityWide;
                m_SelectedDistrict.Update(selectedDistrict);
            }

            districtInfos.Sort();
            districtInfos.Insert(0, new DistrictInfo(CityWide, "City Wide"));

            // Check if district infos have changed
            bool districtsChanged = false;
            if (districtInfos.Count != _DistrictInfos.Count)
            {
                districtsChanged = true;
            }
            else
            {
                for (int i = 0; i < districtInfos.Count; i++)
                {
                    if (districtInfos[i].entity != _DistrictInfos[i].entity || districtInfos[i].name != _DistrictInfos[i].name)
                    {
                        districtsChanged = true;
                        break;
                    }
                }
            }

            if (districtsChanged)
            {
                _DistrictInfos = districtInfos;
                m_DistrictInfos.Update();
            }

            districtEntities.Dispose();
        }
        private void UpdateDistrictInfos(IJsonWriter writer)
        {
            _DistrictInfos.Write(writer);
        }
        private void SelectedDistrictChanged(Entity newDistrict)
        {
            selectedDistrict = newDistrict;
            m_SelectedDistrict.Update(selectedDistrict);
            if (_dPVBinding.value)
            {var demographics = World.GetExistingSystemManaged<Demographics>();
                demographics.SetSelectedDistrict(newDistrict);
                m_DemographicsLifecycleTotalsBinding.Value = demographics.m_LifecycleTotals.ToArray();
                m_DemographicsLifecycleDetailsBinding.Value = demographics.m_LifecycleDetails.ToArray();
                m_DemographicsDetailedGroupDetailsBinding.Value = demographics.m_Results.ToArray();
                m_DemographicsFiveYearDetailsBinding.Value = demographics.m_FiveYearDetails.ToArray();
                m_DemographicsTenYearDetailsBinding.Value = demographics.m_TenYearDetails.ToArray();
                if (m_DemoStatsToggledOnBinding.value)
                {
                    m_TotalsBinding.Value = demographics.m_Totals.ToArray();
                    m_OldCitizenBinding.Update();
                }
            }
            if (_wFPVBinding.value)
            {
                 var workforceSystem = World.GetOrCreateSystemManaged<WorkforceSystem>();
                workforceSystem.SetSelectedDistrict(newDistrict);
                m_WorkforcesBinder.Value = workforceSystem.m_Results.ToArray();
            }
            if (_wPPVBinding.value)
            {
                var workplacesSystem = World.GetOrCreateSystemManaged<WorkplacesSystem>();
                workplacesSystem.SetSelectedDistrict(newDistrict);
                m_WorkplacesBinder.Value = workplacesSystem.m_Results.ToArray();
            }
        }
        #region Industrial Company Filtering
        
        private void SetSelectedCompanyName(string companyName)
        {
            Mod.log.Info($"SetSelectedCompanyName called with: {companyName}");
            m_SelectedCompanyNameBinding.Value = companyName;

            if (string.IsNullOrEmpty(companyName) || companyName == "All Companies")
            {
                m_FilteredIndustrialCompanies = m_ResourceFilteredCompanies;
                ApplySortingToDisplayedData();
                Mod.log.Info("Showing all companies from resource-filtered base");
                return;
            }

            var baseSet = m_ResourceFilteredCompanies ?? m_IndustrialCompanySystem.m_IndustrialCompanyDTOs;
            var filteredByName = baseSet.Where(c => c.CompanyName == companyName).ToArray();
            m_FilteredIndustrialCompanies = filteredByName;
            ApplySortingToDisplayedData();
            Mod.log.Info($"Filtered to {filteredByName.Length} companies with name: {companyName}");
        }

        private void SetSelectedInput1Resource(string resourceName)
        {
            m_SelectedInput1ResourceBinding.Value = resourceName;

            if (!string.IsNullOrEmpty(resourceName) && resourceName != "All")
            {
                m_SelectedInput2ResourceBinding.Value = "All";
                m_SelectedOutputResourceBinding.Value = "All";
            }

            ApplyResourceFilters();
        }

        private void SetSelectedInput2Resource(string resourceName)
        {
            m_SelectedInput2ResourceBinding.Value = resourceName;

            if (!string.IsNullOrEmpty(resourceName) && resourceName != "All")
            {
                m_SelectedInput1ResourceBinding.Value = "All";
                m_SelectedOutputResourceBinding.Value = "All";
            }

            ApplyResourceFilters();
        }

        private void SetSelectedOutputResource(string resourceName)
        {
            m_SelectedOutputResourceBinding.Value = resourceName;

            if (!string.IsNullOrEmpty(resourceName) && resourceName != "All")
            {
                m_SelectedInput1ResourceBinding.Value = "All";
                m_SelectedInput2ResourceBinding.Value = "All";
            }

            ApplyResourceFilters();
        }

        private void ApplyResourceFilters()
        {
            if (m_IndustrialCompanySystem.m_IndustrialCompanyDTOs == null ||
                m_IndustrialCompanySystem.m_IndustrialCompanyDTOs.Length == 0)
            {
                Mod.log.Warn("ApplyResourceFilters called but no company data available yet");
                return;
            }

            string input1Filter = m_SelectedInput1ResourceBinding.Value ?? "All";
            string input2Filter = m_SelectedInput2ResourceBinding.Value ?? "All";
            string outputFilter = m_SelectedOutputResourceBinding.Value ?? "All";

            Mod.log.Info($"Applying resource filters - Input1: {input1Filter}, Input2: {input2Filter}, Output: {outputFilter}");

            string activeFilter = null;
            string activeFilterType = null;

            if (!string.IsNullOrEmpty(input1Filter) && input1Filter != "All")
            {
                activeFilter = input1Filter;
                activeFilterType = "Input1";
            }
            else if (!string.IsNullOrEmpty(input2Filter) && input2Filter != "All")
            {
                activeFilter = input2Filter;
                activeFilterType = "Input2";
            }
            else if (!string.IsNullOrEmpty(outputFilter) && outputFilter != "All")
            {
                activeFilter = outputFilter;
                activeFilterType = "Output";
            }

            if (activeFilter == null)
            {
                m_ResourceFilteredCompanies = null;
                m_FilteredIndustrialCompanies = null;
                m_uiDebugData2.Value = m_IndustrialCompanySystem.m_IndustrialCompanyDTOs;
                UpdateIndustrialCompanyNameList(m_IndustrialCompanySystem.m_IndustrialCompanyDTOs);
                Mod.log.Info("Cleared all resource filters, showing all companies");
                return;
            }

            var filtered = new List<IndustrialCompanyDTO>();
            foreach (var company in m_IndustrialCompanySystem.m_IndustrialCompanyDTOs)
            {
                bool matches = false;

                switch (activeFilterType)
                {
                    case "Input1":
                        if (company.Input1Resources != null)
                        {
                            foreach (var res in company.Input1Resources)
                            {
                                if (res.ResourceName == activeFilter)
                                {
                                    matches = true;
                                    break;
                                }
                            }
                        }
                        break;

                    case "Input2":
                        if (company.Input2Resources != null)
                        {
                            foreach (var res in company.Input2Resources)
                            {
                                if (res.ResourceName == activeFilter)
                                {
                                    matches = true;
                                    break;
                                }
                            }
                        }
                        break;

                    case "Output":
                        if (company.OutputResources != null)
                        {
                            foreach (var res in company.OutputResources)
                            {
                                if (res.ResourceName == activeFilter)
                                {
                                    matches = true;
                                    break;
                                }
                            }
                        }
                        break;
                }

                if (matches)
                {
                    filtered.Add(company);
                }
            }

            m_ResourceFilteredCompanies = filtered.ToArray();
            m_FilteredIndustrialCompanies = m_ResourceFilteredCompanies;
            Mod.log.Info($"Filtered to {filtered.Count} companies using {activeFilterType} filter: {activeFilter}");
            m_uiDebugData2.Value = m_FilteredIndustrialCompanies;
            UpdateIndustrialCompanyNameList(m_FilteredIndustrialCompanies);
        }

        private void UpdateIndustrialCompanyNameList(IndustrialCompanyDTO[] companies)
        {
            var uniqueCompanyNames = new HashSet<string>();
            
            foreach (var company in companies)
            {
                if (!string.IsNullOrEmpty(company.CompanyName))
                {
                    uniqueCompanyNames.Add(company.CompanyName);
                }
            }
            
            var sortedNames = uniqueCompanyNames.OrderBy(name => name).ToList();
            sortedNames.Insert(0, "All Companies");
            listOfCompanyNamesBinding.Value = sortedNames.ToArray();
            Mod.log.Info($"Updated company name list with {uniqueCompanyNames.Count} unique companies");
        }

        private void ApplySortingToDisplayedData()
        {
            IndustrialCompanyDTO[] dataToSort = m_FilteredIndustrialCompanies ?? m_IndustrialCompanySystem.m_IndustrialCompanyDTOs;
            
            if (dataToSort != null && dataToSort.Length > 0)
            {
                List<IndustrialCompanyDTO> companiesList = new List<IndustrialCompanyDTO>(dataToSort);
                m_IndustrialCompanySystem.ApplySorts(companiesList);
                m_uiDebugData2.Value = companiesList.ToArray();
            }
        }

        #endregion

        #region Commercial Company Filtering

        private void SetSelectedCommercialCompanyName(string companyName)
        {
            Mod.log.Info($"SetSelectedCommercialCompanyName called with: {companyName}");
            m_SelectedCCompanyNameBinding.Value = companyName;

            if (string.IsNullOrEmpty(companyName) || companyName == "All Companies")
            {
                m_FilteredCommercialCompanies = m_CommercialResourceFilteredCompanies;
                ApplyCommercialSortingToDisplayedData();
                Mod.log.Info("Showing all companies from resource-filtered base");
                return;
            }

            var baseSet = m_CommercialResourceFilteredCompanies ?? m_CommercialCompanyDataSystem.m_CommercialCompanyDTOs;
            var filteredByName = baseSet.Where(c => c.CompanyName == companyName).ToArray();
            m_FilteredCommercialCompanies = filteredByName;
            ApplyCommercialSortingToDisplayedData();
            Mod.log.Info($"Filtered to {filteredByName.Length} companies with name: {companyName}");
        }

        private void SetSelectedCommercialInput1Resource(string resourceName)
        {
            m_SelectedCInput1ResourceBinding.Value = resourceName;
            if (!string.IsNullOrEmpty(resourceName) && resourceName != "All")
            {
                m_SelectedCOutputResourceBinding.Value = "All";
            }
            ApplyCommercialResourceFilters();
        }

        private void SetSelectedCommercialOutputResource(string resourceName)
        {
            m_SelectedCOutputResourceBinding.Value = resourceName;
            if (!string.IsNullOrEmpty(resourceName) && resourceName != "All")
            {
                m_SelectedCInput1ResourceBinding.Value = "All";
            }
            ApplyCommercialResourceFilters();
        }

        private void ApplyCommercialResourceFilters()
        {
            if (m_CommercialCompanyDataSystem.m_CommercialCompanyDTOs == null ||
                m_CommercialCompanyDataSystem.m_CommercialCompanyDTOs.Length == 0)
            {
                Mod.log.Warn("ApplyResourceFilters called but no company data available yet");
                return;
            }
            
            string input1Filter = m_SelectedCInput1ResourceBinding.Value ?? "All";
            string outputFilter = m_SelectedCOutputResourceBinding.Value ?? "All";

            Mod.log.Info($"Applying commercial resource filters - Input1: {input1Filter}, Output: {outputFilter}");

            
            string activeFilter = null;
            string activeFilterType = null;

            if (!string.IsNullOrEmpty(input1Filter) && input1Filter != "All")
            {
                activeFilter = input1Filter;
                activeFilterType = "Input1";
            }
            else if (!string.IsNullOrEmpty(outputFilter) && outputFilter != "All")
            {
                activeFilter = outputFilter;
                activeFilterType = "Output";
            }
            if (activeFilter == null)
            {
                m_CommercialResourceFilteredCompanies = null;
                m_FilteredCommercialCompanies = null;
                m_uiDebugData.Value = m_CommercialCompanyDataSystem.m_CommercialCompanyDTOs;
                UpdateCommercialCompanyNameList(m_CommercialCompanyDataSystem.m_CommercialCompanyDTOs);
                Mod.log.Info("Cleared all resource filters, showing all companies");
                return;
            }
            var filtered = new List<CommercialCompanyDTO>();
            foreach (var company in m_CommercialCompanyDataSystem.m_CommercialCompanyDTOs)
            {
                bool matches = false;

                switch (activeFilterType)
                {
                    case "Input1":
                        if (company.Input1Resources != null)
                        {
                            foreach (var res in company.Input1Resources)
                            {
                                if (res.ResourceName == activeFilter)
                                {
                                    matches = true;
                                    break;
                                }
                            }
                        }
                        break;
                    
                    case "Output":
                        if (company.OutputResources != null)
                        {
                            foreach (var res in company.OutputResources)
                            {
                                if (res.ResourceName == activeFilter)
                                {
                                    matches = true;
                                    break;
                                }
                            }
                        }
                        break;
                }

                if (matches)
                {
                    filtered.Add(company);
                }
            }
            m_CommercialResourceFilteredCompanies = filtered.ToArray();
            m_FilteredCommercialCompanies = m_CommercialResourceFilteredCompanies;
            Mod.log.Info($"Filtered to {filtered.Count} companies using {activeFilterType} filter: {activeFilter}");
            m_uiDebugData.Value = m_FilteredCommercialCompanies;
            UpdateCommercialCompanyNameList(m_FilteredCommercialCompanies);

            
        }
        private void UpdateCommercialCompanyNameList(CommercialCompanyDTO[] companies)
        {
            var uniqueCompanyNames = new HashSet<string>();
            
            foreach (var company in companies)
            {
                if (!string.IsNullOrEmpty(company.CompanyName))
                {
                    uniqueCompanyNames.Add(company.CompanyName);
                }
            }
            
            var sortedNames = uniqueCompanyNames.OrderBy(name => name).ToList();
            sortedNames.Insert(0, "All Companies");
            listOfCCompanyNamesBinding.Value = sortedNames.ToArray();
            Mod.log.Info($"Updated commercial company name list with {uniqueCompanyNames.Count} unique companies");
        }
        private void ApplyCommercialSortingToDisplayedData()
        {
            CommercialCompanyDTO[] dataToSort = m_FilteredCommercialCompanies ?? m_CommercialCompanyDataSystem.m_CommercialCompanyDTOs;

            if (dataToSort != null && dataToSort.Length > 0)
            {
                List<CommercialCompanyDTO> companiesList = new List<CommercialCompanyDTO>(dataToSort);
                m_CommercialCompanyDataSystem.ApplySorts(companiesList);
                m_uiDebugData.Value = companiesList.ToArray();
            }
        }

        #endregion
        private void UpdateDemographicsData(bool buttonPressed)
        {
            _RefreshDataBinding.Value = buttonPressed;
            if(buttonPressed)
            {
                // Force immediate demographics update
                m_Demographics.UpdateDemographics();
				var demographics = World.GetExistingSystemManaged<Demographics>();
				m_DemographicsLifecycleTotalsBinding.Value = demographics.m_LifecycleTotals.ToArray();
				m_DemographicsLifecycleDetailsBinding.Value = demographics.m_LifecycleDetails.ToArray();
                m_DemographicsDetailedGroupDetailsBinding.Value = demographics.m_Results.ToArray();
                m_DemographicsFiveYearDetailsBinding.Value = demographics.m_FiveYearDetails.ToArray();
                m_DemographicsTenYearDetailsBinding.Value = demographics.m_TenYearDetails.ToArray();
				if (m_DemoStatsToggledOnBinding.value)
				{
					m_TotalsBinding.Value = demographics.m_Totals.ToArray();
				}
                
                // Immediately push the updated data to UI bindings
                m_DemographicsLifecycleTotalsBinding.Binding.TriggerUpdate();
				m_DemographicsLifecycleDetailsBinding.Binding.TriggerUpdate();
                m_DemographicsDetailedGroupDetailsBinding.Binding.TriggerUpdate();
                m_DemographicsFiveYearDetailsBinding.Binding.TriggerUpdate();
                m_DemographicsTenYearDetailsBinding.Binding.TriggerUpdate();
                m_TotalsBinding.Binding.TriggerUpdate();
                m_OldCitizenBinding.Update();
                
                //m_Log.Info("Demographics data manually refreshed and bindings updated");
            }
        }
        
    }
}