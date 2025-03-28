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
using InfoLoom.Systems;
using InfoLoomTwo.Domain;
using InfoLoomTwo.Extensions;
using InfoLoomTwo.Systems.CommercialSystems.CommercialDemandData;
using InfoLoomTwo.Systems.CommercialSystems.CommercialProductData;
using InfoLoomTwo.Systems.DemographicsData;
using InfoLoomTwo.Systems.DemographicsData.Demographics;
using InfoLoomTwo.Systems.DistrictData;
using InfoLoomTwo.Systems.IndustrialSystems.IndustrialDemandData;
using InfoLoomTwo.Systems.IndustrialSystems.IndustrialProductData;
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
        private NameSystem m_NameSystem;
        private UIUpdateState _uiUpdateState;
        
        private ResidentialDemandSystem m_ResidentialDemandSystem;
        private CommercialDemandSystem m_CommercialDemandSystem;
        private IndustrialDemandSystem m_IndustrialDemandSystem;
        
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
        private ValueBindingHelper<List<ResourceTradeCost>> m_TradeCostsBinding;
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
        {    
            base.OnCreate();
            _uiUpdateState = UIUpdateState.Create(World, 256);
            m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
            m_NameSystem = World.GetOrCreateSystemManaged<NameSystem>();
            m_ResidentialDemandSystem = base.World.GetOrCreateSystemManaged<ResidentialDemandSystem>();
            m_CommercialDemandSystem = base.World.GetOrCreateSystemManaged<CommercialDemandSystem>();
            m_IndustrialDemandSystem = base.World.GetOrCreateSystemManaged<IndustrialDemandSystem>();
            
            
            m_DistrictDataSystem = base.World.GetOrCreateSystemManaged<DistrictDataSystem>();
            m_CommercialSystem = base.World.GetOrCreateSystemManaged<CommercialSystem>();
            m_CommercialProductsSystem = base.World.GetOrCreateSystemManaged<CommercialProductsSystem>();
            m_Demographics = base.World.GetOrCreateSystemManaged<Demographics>();
            m_IndustrialSystem = base.World.GetOrCreateSystemManaged<IndustrialSystem>();
            m_IndustrialProductsSystem = base.World.GetOrCreateSystemManaged<IndustrialProductsSystem>();
            m_ResidentialSystem = base.World.GetOrCreateSystemManaged<ResidentialSystem>();
            m_TradeCostSystem = base.World.GetOrCreateSystemManaged<TradeCostSystem>();
            m_WorkforceSystem = base.World.GetOrCreateSystemManaged<WorkforceSystem>();
            m_WorkplacesSystem = base.World.GetOrCreateSystemManaged<WorkplacesSystem>();
            
            
            
            
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
                //WorkforceUI
                m_WorkforcesBinder = CreateBinding("WorkforceData", new WorkforcesInfo[0]);

                //WorkplacesUI
                m_WorkplacesBinder = CreateBinding("WorkplacesData", new WorkplacesInfo[0]);
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
                m_CommercialProductsSystem.IsPanelVisible = true; 
                m_CommercialProductsSystem.ForceUpdateOnce();
                
            }
        
            if (_dPVBinding.value )
            {
                var demographics = World.GetExistingSystemManaged<Demographics>();
                m_PopulationAtAgeInfoBinding.Value = demographics.m_Results.ToArray();
                m_TotalsBinding.Value = demographics.m_Totals.ToArray();
                m_OldCitizenBinding.Update();
                m_Demographics.IsPanelVisible = true;
                m_Demographics.ForceUpdateOnce();
                
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
                
                
                m_IndustrialProductsSystem.IsPanelVisible = true;
                m_IndustrialProductsSystem.ForceUpdateOnce();
                
            }
        
            if (_rDPVBinding.value)
            {
                ResidentialSystem residentialSystem = base.World.GetOrCreateSystemManaged<ResidentialSystem>();
                m_ResidentialBinding.Value = residentialSystem.m_Results.ToArray();
               
                
                m_ResidentialSystem.IsPanelVisible = true;
               
                
            }
        
            if (_tCPVBinding.value )
            {
                var tradeCostSystem = World.GetOrCreateSystemManaged<TradeCostSystem>();
                m_TradeCostsBinding.Value = tradeCostSystem.GetResourceTradeCosts().ToList();
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
                
                m_CommercialProductsSystem.ForceUpdateOnce();
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
            m_Demographics.IsPanelVisible = open;
            
            if (open)
            {
                
                m_Demographics.ForceUpdateOnce();
                
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
               
                m_IndustrialProductsSystem.ForceUpdateOnce();
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
                m_TradeCostSystem.ForceUpdateOnce();
                var tradeCostSystem = World.GetOrCreateSystemManaged<TradeCostSystem>();
                m_TradeCostsBinding.Value = tradeCostSystem.GetResourceTradeCosts().ToList();
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
        
        

        
    }
}

