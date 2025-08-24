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
using InfoLoomTwo.Systems.CommercialSystems.CommercialProductData;
using InfoLoomTwo.Systems.DemographicsData;
using InfoLoomTwo.Systems.DistrictData;
using InfoLoomTwo.Systems.IndustrialSystems.IndustrialCompanyData;
using InfoLoomTwo.Systems.IndustrialSystems.IndustrialCompanyData.IndustrialCompanyDomain;
using InfoLoomTwo.Systems.IndustrialSystems.IndustrialDemandData;
using InfoLoomTwo.Systems.IndustrialSystems.IndustrialProductData;
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
        private ResourceSystem m_ResourceSystem; // new
        private ImageSystem m_ImageSystem;       // new
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
        
        
        
        //HouseholdData
        //DemographicsUI
        public GetterValueBinding<int> m_OldCitizenBinding;
        public ValueBindingHelper<PopulationAtAgeInfo[]> m_PopulationAtAgeInfoBinding;
        public ValueBindingHelper<int[]> m_TotalsBinding;
        public ValueBinding<bool> m_DemoStatsToggledOnBinding;
        private ValueBinding<bool> m_DemoAgeGroupingToggledOnBinding;
        private ValueBindingHelper<GroupingStrategy> m_DemoGroupingStrategyBinding;
         private ValueBinding<Entity> m_SelectedDistrict;
        private ValueBindingHelper<Demographics1> m_Demographics1Binding;
        private ValueBindingHelper<Demographics2> m_Demographics2Binding;
         
         
         
         
         private ValueBinding<int> m_SelectedResource;

        private RawValueBinding m_DistrictInfosBinding;
//DistrictDataUI
        
        //IndustrialDemandDataUI
        private ValueBindingHelper<string[]> m_IndustrialExcludedResourcesBinding;
        private ValueBindingHelper<int[]> m_IndustrialBinding;
        //IndustrialProductsDataUI
        private ValueBindingHelper<IndustrialProductsData[]> m_IndustrialProductBinding;
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
            m_DistrictDataSystem = base.World.GetOrCreateSystemManaged<DistrictDataSystem>();
            m_CommercialSystem = base.World.GetOrCreateSystemManaged<CommercialSystem>();
            m_CommercialProductsSystem = base.World.GetOrCreateSystemManaged<CommercialProductsSystem>();
            m_Demographics = base.World.GetOrCreateSystemManaged<Demographics>();
            m_IndustrialSystem = base.World.GetOrCreateSystemManaged<IndustrialSystem>();
            m_IndustrialProductsSystem = base.World.GetOrCreateSystemManaged<IndustrialProductsSystem>();
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

             _cPPVBinding = new ValueBinding<bool>(ModID, CommercialProductsOpen, false);
             AddBinding(_cPPVBinding);
             AddBinding(new TriggerBinding<bool>(ModID, CommercialProductsOpen, SetCommercialProductsVisibility));
            
             _dPVBinding = new ValueBinding<bool>(ModID, DemographicsOpen, false);
             AddBinding(_dPVBinding);
             AddBinding(new TriggerBinding<bool>(ModID, DemographicsOpen, SetDemographicsVisibility));
             
             _iDPVBinding = new ValueBinding<bool>(ModID, IndustrialDemandOpen, false);
             AddBinding(_iDPVBinding);
             AddBinding(new TriggerBinding<bool>(ModID, IndustrialDemandOpen, SetIndustrialDemandVisibility));
            
             _iPPVBinding = new ValueBinding<bool>(ModID, IndustrialProductsOpen, false);
             AddBinding(_iPPVBinding);
             AddBinding(new TriggerBinding<bool>(ModID, IndustrialProductsOpen, SetIndustrialProductsVisibility));
            
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
            AddBinding(m_SelectedDistrict = new ValueBinding<Entity>(ModID, "selectedDistrict", CityWide));
            AddBinding(new TriggerBinding<Entity>(ModID, "selectedDistrictChanged", SelectedDistrictChanged));
            AddBinding(m_DistrictInfos = new RawValueBinding(ModID, "districtInfos", UpdateDistrictInfos));
            m_Demographics1Binding = CreateBinding("Demographics1", "SetDemographics1", Demographics1.All);
            m_Demographics2Binding = CreateBinding("Demographics2", "SetDemographics2", Demographics2.All);
            
            
            
            
            //DistrictDataUI
            // First binding: Basic district information
            


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
                    
                    // Update UI with the sorted data
                    m_uiDebugData.Value = m_CommercialCompanyDataSystem.m_CommercialCompanyDTOs;
            }
            if (_iCDVBinding.value)
            {
                m_IndustrialCompanySystem.IsPanelVisible = true;
                
                // Get the current sorting values from bindings
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
                SortingEnum maintenance2Sorting = m_IndustrialMaintenanceSortingBinding.Value;
                
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
                m_IndustrialCompanySystem.m_CurrentMaintenanceSorting = maintenance2Sorting;
                
                // Each sort will be applied in sequence
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
                m_IndustrialMaintenanceSortingBinding.UpdateCallback(maintenance2Sorting);
                // Update UI with the sorted data
                m_uiDebugData2.Value = m_IndustrialCompanySystem.m_IndustrialCompanyDTOs;
                
            }
            //m_Log.Debug($"{nameof(InfoLoomUISystem)}.{nameof(OnUpdate)} 2.");
            if (_dPVBinding.value)
            {
                
                var demographics = World.GetExistingSystemManaged<Demographics>();

                m_PopulationAtAgeInfoBinding.Value = demographics.m_Results.ToArray();
                GroupingStrategy currentStrategy = m_DemoGroupingStrategyBinding.Value;
                m_DemoGroupingStrategyBinding.UpdateCallback(currentStrategy);
                if (m_DemoStatsToggledOnBinding.value)
                {
                    m_TotalsBinding.Value = demographics.m_Totals.ToArray();
                    m_OldCitizenBinding.Update();
                }
                
                m_Demographics.IsPanelVisible = true;
                m_Demographics.ForceUpdateOnce();

                //m_Log.Debug($"{nameof(InfoLoomUISystem)}.{nameof(OnUpdate)} demographics finished");
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
                m_PopulationAtAgeInfoBinding.Value = demographics.m_Results.ToArray();
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
        
         

    }
}
