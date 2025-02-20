using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Colossal.UI.Binding;
using Game;
using Game.Economy;
using Game.Input;
using Game.Prefabs;
using Game.Simulation;
using Game.UI;
using Game.UI.InGame;
using InfoLoomTwo.Domain;
using InfoLoomTwo.Extensions;
using InfoLoomTwo.Systems.CommercialSystems.CommercialDemandData;
using InfoLoomTwo.Systems.CommercialSystems.CommercialProductData;
using InfoLoomTwo.Systems.DemographicsData;
using InfoLoomTwo.Systems.DistrictData;
using InfoLoomTwo.Systems.IndustrialSystems.IndustrialDemandData;
using InfoLoomTwo.Systems.IndustrialSystems.IndustrialProductData;
using InfoLoomTwo.Systems.ResidentialData;
using InfoLoomTwo.Systems.TradeCostData;
using InfoLoomTwo.Systems.WorkforceData;
using InfoLoomTwo.Systems.WorkplacesData;





namespace InfoLoomTwo.Systems
{
    public partial class InfoLoomUISystem : ExtendedUISystemBase
    {
        
        private const string ModID = "InfoLoomTwo";
        
        private const string InfoLoomMenuOpen = "InfoLoomMenuOpen";
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
        private ResidentialDemandSystem m_ResidentialDemandSystem;
        private CommercialDemandSystem m_CommercialDemandSystem;
        private IndustrialDemandSystem m_IndustrialDemandSystem;
        private NameSystem m_NameSystem;
        private UIUpdateState _uiUpdateState;
        private CommercialSystem m_CommercialSystem;
        private CommercialProductsSystem m_CommercialProductsSystem;
        private Demographics m_Demographics;
        private DistrictDataSystem m_DistrictDataSystem;
        
        private IndustrialSystem m_IndustrialSystem;
        private IndustrialProductsSystem m_IndustrialProductsSystem;
        private ResidentialSystem m_ResidentialSystem;
        private TradeCostSystem m_TradeCostSystem;
        private WorkforceSystem m_WorkforceSystem;
        private WorkplacesSystem m_WorkplacesSystem;
        
        //Bindings
        //BuildingDemandUI
        private ValueBindingHelper<int[]> m_uiBuildingDemand;
        //CommercialDemandDataUI
        private ValueBindingHelper<string[]> m_ExcludedResourcesBinding;
        private ValueBindingHelper<int[]> m_CommercialBinding;
        //CommercialProductsDataUI
        private ValueBindingHelper<Domain.CommercialProductsData[]> m_CommercialProductBinding;
        //DemographicsUI
        private GetterValueBinding<int> m_OldCitizenBinding;
        private ValueBindingHelper<Domain.PopulationAtAgeInfo[]> m_PopulationAtAgeInfoBinding;
        private ValueBindingHelper<int[]> m_TotalsBinding;
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
        //ResidentialDemandDataUI
        public ValueBindingHelper<int[]> m_ResidentialBinding;
        //TradeCostsUI
        private List<TradeCostResource> m_Imports = new List<TradeCostResource>();
        private List<TradeCostResource> m_Exports = new List<TradeCostResource>();
        private ValueBindingHelper<List<ResourceTradeCost>> m_TradeCostsBinding;
        private ValueBindingHelper<List<TradeCostResource>> m_ImportsBinding;
        private ValueBindingHelper<List<TradeCostResource>> m_ExportsBinding;
        //WorkforceUI
        private ValueBindingHelper<WorkforcesInfo[]> m_WorkforcesBinder;
        //WorkplacesUI
        private ValueBindingHelper<WorkplacesInfo[]> m_WorkplacesBinder;

        // Panel 
        private ValueBinding<bool> _panelVisibleBinding;
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
        
        
        

        

        

        
        public override GameMode gameMode => GameMode.Game;

        protected override void OnCreate()
        {    _uiUpdateState = UIUpdateState.Create(World, 256);
            base.OnCreate();
            m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
            m_ResidentialDemandSystem = base.World.GetOrCreateSystemManaged<ResidentialDemandSystem>();
            m_CommercialDemandSystem = base.World.GetOrCreateSystemManaged<CommercialDemandSystem>();
            m_IndustrialDemandSystem = base.World.GetOrCreateSystemManaged<IndustrialDemandSystem>();
            m_DistrictDataSystem = base.World.GetOrCreateSystemManaged<DistrictDataSystem>();
            
            m_NameSystem = World.GetOrCreateSystemManaged<NameSystem>();
            
            //InfoLoomMenu
            _panelVisibleBinding = new ValueBinding<bool>(ModID, InfoLoomMenuOpen, false);
            AddBinding(_panelVisibleBinding);
            AddBinding(new TriggerBinding<bool>(ModID, InfoLoomMenuOpen, SetInfoLoomMenuVisibility));
            
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
            
            

            m_uiBuildingDemand = CreateBinding("BuildingDemandData", new int[0]);
            //CommercialDemandDataUI
            m_CommercialBinding = CreateBinding("CommercialData", new int[10]);
            m_ExcludedResourcesBinding = CreateBinding("CommercialDataExRes", new string[0]);

            //CommercialProductsDataUI
            m_CommercialProductBinding = CreateBinding("CommercialProductsData", Array.Empty<CommercialProductsData>());

            //DemographicsUI
            m_PopulationAtAgeInfoBinding = CreateBinding("DemographicsDataDetails", new PopulationAtAgeInfo[0]);
            m_TotalsBinding = CreateBinding("DemographicsDataTotals", new int[10]);
            m_OldCitizenBinding = CreateBinding("DemographicsDataOldestCitizen", () =>
            {
                var demographics = World.GetExistingSystemManaged<Demographics>();
                return demographics.m_Totals[6];
            });        

            //DistrictDataUI
            // First binding: Basic district information
            AddBinding(m_uiDistricts = new RawValueBinding("InfoLoomTwo", "DistrictData", m_DistrictDataSystem.WriteDistricts));
            

            // Second binding: Resident statistics
            

            //IndustrialDemandDataUI
            m_IndustrialBinding = CreateBinding("IndustrialData", new int[16]);
            m_IndustrialExcludedResourcesBinding = CreateBinding("IndustrialDataExRes", new string[0]);

            //IndustrialProductsDataUI
            m_IndustrialProductBinding = CreateBinding("IndustrialProductsData", Array.Empty<IndustrialProductsData>());

            //ResidentialDemandDataUI
            m_ResidentialBinding = CreateBinding("ResidentialData", new int[18]);

            //TradeCostsUI
            m_TradeCostsBinding = CreateBinding("TradeCostsData", new List<ResourceTradeCost>());
            m_ImportsBinding = CreateBinding("TradeCostsDataImports", new List<TradeCostResource>());
            m_ExportsBinding = CreateBinding("TradeCostsDataExports", new List<TradeCostResource>());

            //WorkforceUI
            m_WorkforcesBinder = CreateBinding("WorkforceData", new WorkforcesInfo[0]);

            //WorkplacesUI
            m_WorkplacesBinder = CreateBinding("WorkplacesData", new WorkplacesInfo[0]);
    
            
                m_IndustrialBinding = CreateBinding("IndustrialData", new int[16]);
                m_IndustrialExcludedResourcesBinding = CreateBinding("IndustrialDataExRes", new string[0]);

                //IndustrialProductsDataUI
                m_IndustrialProductBinding = CreateBinding("IndustrialProductsData", Array.Empty<IndustrialProductsData>());

                //ResidentialDemandDataUI
                m_ResidentialBinding = CreateBinding("ResidentialData", new int[18]);

                //TradeCostsUI
                m_TradeCostsBinding = CreateBinding("TradeCostsData", new List<ResourceTradeCost>());
                m_ImportsBinding = CreateBinding("TradeCostsDataImports", new List<TradeCostResource>());
                m_ExportsBinding = CreateBinding("TradeCostsDataExports", new List<TradeCostResource>());

                //WorkforceUI
                m_WorkforcesBinder = CreateBinding("WorkforceData", new WorkforcesInfo[0]);

                //WorkplacesUI
                m_WorkplacesBinder = CreateBinding("WorkplacesData", new WorkplacesInfo[0]);
        }
        protected override void OnUpdate()
        {
            if (!_uiUpdateState.Advance())
                return;
        
            if (_bDPVBinding.value)
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
        
            if (_cDPVBinding.value)
            {
                var commercialSystem = base.World.GetOrCreateSystemManaged<CommercialSystem>();
                m_CommercialBinding.Value = commercialSystem.m_Results.ToArray();
                m_ExcludedResourcesBinding.Value =
                    commercialSystem.m_ExcludedResources.value == Resource.NoResource
                    ? new string[0]
                    : ExtractExcludedResources(commercialSystem.m_ExcludedResources.value);
            }
        
            if (_cPPVBinding.value)
            {
                m_CommercialProductBinding.Value = CommercialProductsSystem.m_DemandData
                    .Where(d => d.Demand > 0 || d.Companies > 0 || d.Building > 0 || d.Free > 0)
                    .Select(d => new Domain.CommercialProductsData
                    {
                        ResourceName = d.ResourceName.ToString(),
                        Demand = d.Demand,
                        Building = d.Building,
                        Free = d.Free,
                        Companies = d.Companies,
                        Workers = d.Workers,
                        SvcPercent = d.SvcPercent,
                        CapPercent = d.CapPercent,
                        CapPerCompany = d.CapPerCompany,
                        WrkPercent = d.WrkPercent,
                        TaxFactor = d.TaxFactor
                    })
                    .ToArray();
        
                if (m_CommercialProductBinding.Value.Length == 0 && CommercialProductsSystem.m_DemandData.Length > 0)
                {
                    m_CommercialProductBinding.Value = new Domain.CommercialProductsData[]
                    {
                        new Domain.CommercialProductsData
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
                            TaxFactor = 0
                        }
                    };
                }
            }
        
            if (_dPVBinding.value)
            {
                var demographics = World.GetExistingSystemManaged<Demographics>();
                m_PopulationAtAgeInfoBinding.Value = demographics.m_Results.ToArray();
                m_TotalsBinding.Value = demographics.m_Totals.ToArray();
                m_OldCitizenBinding.Update();
            }
        
            if (_dDPVBinding.value)
            {
                m_uiDistricts.Update();
                
            }
        
            if (_iDPVBinding.value)
            {
                var industrialSystem = base.World.GetOrCreateSystemManaged<IndustrialSystem>();
                m_IndustrialBinding.Value = industrialSystem.m_Results.ToArray();
                m_ExcludedResourcesBinding.Value =
                    industrialSystem.m_ExcludedResources.value == Resource.NoResource
                    ? new string[0]
                    : IndustrialExtractExcludedResources(industrialSystem.m_ExcludedResources.value);
            }
        
            if (_iPPVBinding.value)
            {
                m_IndustrialProductBinding.Value = IndustrialProductsSystem.m_DemandData
                    .Where(d => d.Demand > 0 || d.Companies > 0 || d.Building > 0 || d.Free > 0)
                    .Select(d => new IndustrialProductsData
                    {
                        ResourceName = d.ResourceName.ToString(),
                        Demand = d.Demand,
                        Building = d.Building,
                        Free = d.Free,
                        Companies = d.Companies,
                        Workers = d.Workers,
                        SvcPercent = d.SvcPercent,
                        CapPercent = d.CapPercent,
                        CapPerCompany = d.CapPerCompany,
                        WrkPercent = d.WrkPercent,
                        TaxFactor = d.TaxFactor
                    })
                    .ToArray();
        
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
                            TaxFactor = 0
                        }
                    };
                }
            }
        
            if (_rDPVBinding.value)
            {
                ResidentialSystem residentialSystem = base.World.GetOrCreateSystemManaged<ResidentialSystem>();
                m_ResidentialBinding.Value = residentialSystem.m_Results.ToArray();
            }
        
            if (_tCPVBinding.value)
            {
                var tradeCostSystem = World.GetOrCreateSystemManaged<TradeCostSystem>();
                var tradeCosts = tradeCostSystem.GetResourceTradeCosts().ToList();
                var topImports = tradeCostSystem.GetImports().ToList();
                var topExports = tradeCostSystem.GetExports().ToList();
                m_TradeCostsBinding.Value = tradeCosts;
                m_ImportsBinding.Value = topImports;
                m_ExportsBinding.Value = topExports;
                UpdateImportData();
                UpdateExportData();
            }
        
            if (_wFPVBinding.value)
            {
                var workforceSystem = base.World.GetOrCreateSystemManaged<WorkforceSystem>();
                m_WorkforcesBinder.Value = workforceSystem.m_Results.ToArray();
            }
        
            if (_wPPVBinding.value)
            {
                var workplacesSystem = base.World.GetOrCreateSystemManaged<WorkplacesSystem>();
                m_WorkplacesBinder.Value = workplacesSystem.m_Results.ToArray();
            }
        
            base.OnUpdate();
        }
        private void SetInfoLoomMenuVisibility(bool open)
        {
            _panelVisibleBinding.Update(open);
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
            
            if (open)
            {
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
            
            if (open)
            {
                m_CommercialProductBinding.Value = CommercialProductsSystem.m_DemandData
                    .Where(d => d.Demand > 0 || d.Companies > 0 || d.Building > 0 || d.Free > 0)
                    .Select(d => new Domain.CommercialProductsData
                    {
                        ResourceName = d.ResourceName.ToString(),
                        Demand = d.Demand,
                        Building = d.Building,
                        Free = d.Free,
                        Companies = d.Companies,
                        Workers = d.Workers,
                        SvcPercent = d.SvcPercent,
                        CapPercent = d.CapPercent,
                        CapPerCompany = d.CapPerCompany,
                        WrkPercent = d.WrkPercent,
                        TaxFactor = d.TaxFactor
                    }).ToArray();
            }
        }
        
        private void SetDemographicsVisibility(bool open)
        {
            _dPVBinding.Update(open);
            
            if (open)
            {
                var demographics = World.GetExistingSystemManaged<Demographics>();
                m_PopulationAtAgeInfoBinding.Value = demographics.m_Results.ToArray();
                m_TotalsBinding.Value = demographics.m_Totals.ToArray();
                m_OldCitizenBinding.Update();
            }
        }
        
        private void SetDistrictDataVisibility(bool open)
        {
            _dDPVBinding.Update(open);
            m_DistrictDataSystem.IsPanelVisible = open;

            if (open)
            {
                // Force immediate update when panel opens
                m_DistrictDataSystem.ForceUpdateOnce();
                m_uiDistricts.Update();
            }
        }
        
        private void SetIndustrialDemandVisibility(bool open)
        {
            _iDPVBinding.Update(open);
            
            if (open)
            {
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
            
            if (open)
            {
                m_IndustrialProductBinding.Value = IndustrialProductsSystem.m_DemandData
                    .Where(d => d.Demand > 0 || d.Companies > 0 || d.Building > 0 || d.Free > 0)
                    .Select(d => new IndustrialProductsData
                    {
                        ResourceName = d.ResourceName.ToString(),
                        Demand = d.Demand,
                        Building = d.Building,
                        Free = d.Free,
                        Companies = d.Companies,
                        Workers = d.Workers,
                        SvcPercent = d.SvcPercent,
                        CapPercent = d.CapPercent,
                        CapPerCompany = d.CapPerCompany,
                        WrkPercent = d.WrkPercent,
                        TaxFactor = d.TaxFactor
                    }).ToArray();
            }
        }
        
        private void SetResidentialDemandVisibility(bool open)
        {
            _rDPVBinding.Update(open);
            
            if (open)
            {
                var residentialSystem = World.GetOrCreateSystemManaged<ResidentialSystem>();
                m_ResidentialBinding.Value = residentialSystem.m_Results.ToArray();
            }
        }
        
        private void SetTradeCostsVisibility(bool open)
        {
            _tCPVBinding.Update(open);
            
            if (open)
            {
                var tradeCostSystem = World.GetOrCreateSystemManaged<TradeCostSystem>();
                var tradeCosts = tradeCostSystem.GetResourceTradeCosts().ToList();
                var topImports = tradeCostSystem.GetImports().ToList();
                var topExports = tradeCostSystem.GetExports().ToList();
                
                m_TradeCostsBinding.Value = tradeCosts;
                m_ImportsBinding.Value = topImports;
                m_ExportsBinding.Value = topExports;
                UpdateImportData();
                UpdateExportData();
            }
        }
        
        private void SetWorkforceVisibility(bool open)
        {
            _wFPVBinding.Update(open);
            
            if (open)
            {
                var workforceSystem = World.GetOrCreateSystemManaged<WorkforceSystem>();
                m_WorkforcesBinder.Value = workforceSystem.m_Results.ToArray();
            }
        }
        
        private void SetWorkplacesVisibility(bool open)
        {
            _wPPVBinding.Update(open);
            
            if (open)
            {
                var workplacesSystem = World.GetOrCreateSystemManaged<WorkplacesSystem>();
                m_WorkplacesBinder.Value = workplacesSystem.m_Results.ToArray();
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
        private void UpdateImportData()
        {
            int num = 0;
            int num2 = m_Imports.Count;
            if (m_Imports.Count < num2)
            {
                num2 = m_Imports.Count;
            }
            for (int i = 0; i < num2; i++)
            {
                m_ImportsBinding.Value = m_Imports;
                num += m_Imports[i].Amount;
            }
        }

        private void UpdateExportData()
        {
            int num = 0;
            int num2 = m_Exports.Count;
            if (m_Exports.Count < num2)
            {
                num2 = m_Exports.Count;
            }
            for (int i = 0; i < num2; i++)
            {
                m_ExportsBinding.Value = m_Exports;
                num += m_Exports[i].Amount;
            }
        }
        

        
    }
}

