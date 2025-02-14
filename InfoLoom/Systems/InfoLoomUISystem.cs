using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.UI.Binding;
using Game;
using Game.Economy;
using Game.Input;
using Game.Prefabs;
using Game.Simulation;
using Game.UI;
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
using Unity.Entities;


namespace InfoLoomTwo.Systems
{
    public partial class InfoLoomUISystem : ExtendedUISystemBase
    {
        private const string InfoLoomMenuOpen = "InfoLoomMenuOpen";
        private const string DistrictData = "DistrictData";
        
        
        
        
        //Systems
        private SimulationSystem m_SimulationSystem;
        private ResidentialDemandSystem m_ResidentialDemandSystem;
        private CommercialDemandSystem m_CommercialDemandSystem;
        private IndustrialDemandSystem m_IndustrialDemandSystem;
        private NameSystem m_NameSystem;
        private UIUpdateState _uiUpdateState;
        
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
        private bool[] _panelStates;
        private bool[] PanelStates => _panelStates;
        private GetterValueBinding<bool[]> _PanelStates;

        

        
        public override GameMode gameMode => GameMode.Game;

        protected override void OnCreate()
        {    _uiUpdateState = UIUpdateState.Create(World, 256);
            base.OnCreate();
            m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
            m_ResidentialDemandSystem = base.World.GetOrCreateSystemManaged<ResidentialDemandSystem>();
            m_CommercialDemandSystem = base.World.GetOrCreateSystemManaged<CommercialDemandSystem>();
            m_IndustrialDemandSystem = base.World.GetOrCreateSystemManaged<IndustrialDemandSystem>();
            m_NameSystem = World.GetOrCreateSystemManaged<NameSystem>();
            
            //Panel
            _panelVisibleBinding = new ValueBinding<bool>(Mod.Id, InfoLoomMenuOpen, false);
            AddBinding(_panelVisibleBinding);
            //AddBinding(new TriggerBinding<bool[]>(Mod.Id, InfoLoomMenuOpen, SetInfoLoomMenuVsibility));

            
           

            m_uiBuildingDemand = CreateBinding("ilBuildingDemand", new int[0]);
            //CommercialDemandDataUI
            m_CommercialBinding = CreateBinding("ilCommercial", new int[10]);
            m_ExcludedResourcesBinding = CreateBinding("ilCommercialExRes", new string[0]);

            //CommercialProductsDataUI
            m_CommercialProductBinding = CreateBinding("commercialProducts", Array.Empty<CommercialProductsData>());

            //DemographicsUI
            var demographics = World.GetExistingSystemManaged<Demographics>();
            m_PopulationAtAgeInfoBinding = CreateBinding("StructureDetails", new PopulationAtAgeInfo[0]);
            m_TotalsBinding = CreateBinding("StructureTotals", new int[10]);

            m_OldCitizenBinding = CreateBinding("OldestCitizen", () =>
            {
                var demographics = World.GetExistingSystemManaged<Demographics>();
                return demographics.m_Totals[6];
            });

            //DistrictDataUI
            AddBinding(m_uiDistricts = new RawValueBinding(DistrictData, "ilDistricts", delegate (IJsonWriter writer)
            {
                var outputData = DistrictDataSystem.SharedDistrictOutputData;
                if (outputData == null)
                {
                    writer.ArrayBegin(0);
                    writer.ArrayEnd();
                    return;
                }

                // Filter out any entries with an invalid district entity.
                List<DistrictOutputData> validEntries = new List<DistrictOutputData>();
                for (int i = 0; i < outputData.Length; i++)
                {
                    var data = outputData[i];
                    if (data.districtEntity != Entity.Null)
                        validEntries.Add(data);
                }

                writer.ArrayBegin(validEntries.Count);
                for (int i = 0; i < validEntries.Count; i++)
                {
                    DistrictOutputData data = validEntries[i];
                    Entity entity = data.districtEntity;
                    writer.TypeBegin("InfoLoomTwo.DistrictData");
                    writer.PropertyName("name");
                    m_NameSystem.BindName(writer, entity);
                    writer.PropertyName("residentCount");
                    writer.Write(data.residentCount);
                    writer.PropertyName("petCount");
                    writer.Write(data.petCount);
                    writer.PropertyName("householdCount");
                    writer.Write(data.householdCount);
                    writer.PropertyName("maxHouseholds");
                    writer.Write(data.maxHouseholds);
                    writer.PropertyName("entity");
                    writer.Write(entity);
                    writer.TypeEnd();
                }
                writer.ArrayEnd();
            }));

            //IndustrialDemandDataUI
            m_IndustrialBinding = CreateBinding("ilIndustrial", new int[16]);
            m_IndustrialExcludedResourcesBinding = CreateBinding("ilIndustrialExRes", new string[0]);

            //IndustrialProductsDataUI
            m_IndustrialProductBinding = CreateBinding("industrialProducts", Array.Empty<IndustrialProductsData>());

            //ResidentialDemandDataUI
            m_ResidentialBinding = CreateBinding("ilResidential", new int[18]);

            //TradeCostsUI
            m_TradeCostsBinding = CreateBinding("tradeCosts", new List<ResourceTradeCost>());
            m_ImportsBinding = CreateBinding("imports", new List<TradeCostResource>());
            m_ExportsBinding = CreateBinding("exports", new List<TradeCostResource>());

            //WorkforceUI
            m_WorkforcesBinder = CreateBinding("ilWorkforce", new WorkforcesInfo[0]);

            //WorkplacesUI
            m_WorkplacesBinder = CreateBinding("ilWorkplaces", new WorkplacesInfo[0]);
             _panelStates = new bool[(int)PanelTypes.Count];
            /*AddUpdateBinding( new GetterValueBinding<bool[]>("InfoLoomTwo", "panelState", () => _panelStates, new ArrayWriter<bool>()));
            void Toggle(int panelType) {
                var array = _panelStates;
                array[panelType] = !array[panelType];
                _panelStates.Valie; = (bool[])array.Clone();
                _PanelStates.TriggerUpdate();
            }
            AddBinding(CreateTrigger("setPanelState", () => Toggle));*/
            
        }
        
        

        protected override void OnUpdate()
        {
           if (_uiUpdateState.Advance())
           {
                
            
               bool isOpen(PanelTypes panel) => _panelStates[(int)panel];
            
                
                
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
                

                
                    var commercialSystem = base.World.GetOrCreateSystemManaged<CommercialSystem>();
                    m_CommercialBinding.Value = commercialSystem.m_Results.ToArray();
                    m_ExcludedResourcesBinding.Value =
                    commercialSystem.m_ExcludedResources.value == Resource.NoResource
                    ? new string[0]
                    : ExtractExcludedResources(commercialSystem.m_ExcludedResources.value);
                    
                    string[] ExtractExcludedResources(Resource excludedResources)
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
                

                
                    m_CommercialProductBinding.Value = CommercialProductsSystem.m_DemandData
                    .Where(d => d.Demand > 0 || d.Companies > 0 || d.Building > 0 || d.Free > 0)  // Only show relevant resources
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
                

                
                    var demographics = World.GetExistingSystemManaged<Demographics>();
                    m_PopulationAtAgeInfoBinding.Value = demographics.m_Results.ToArray();
                    m_TotalsBinding.Value = demographics.m_Totals.ToArray();
                    m_OldCitizenBinding.Update();
                
                
                
                     m_uiDistricts.Update();
                
                    
                

                
                     var industrialSystem = base.World.GetOrCreateSystemManaged<IndustrialSystem>();
                    m_IndustrialBinding.Value = industrialSystem.m_Results.ToArray();
                    m_ExcludedResourcesBinding.Value =
                    industrialSystem.m_ExcludedResources.value == Resource.NoResource
                    ? new string[0]
                    : IndustrialExtractExcludedResources(industrialSystem.m_ExcludedResources.value);
                    
                    string[] IndustrialExtractExcludedResources(Resource excludedResources)
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
                

                
                    m_IndustrialProductBinding.Value = IndustrialProductsSystem.m_DemandData
                    .Where(d => d.Demand > 0 || d.Companies > 0 || d.Building > 0 || d.Free > 0)  // Only show relevant resources
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
                
                    ResidentialSystem residentialSystem = base.World.GetOrCreateSystemManaged<ResidentialSystem>();
                    m_ResidentialBinding.Value = residentialSystem.m_Results.ToArray();
                
                    var tradeCostSystem = World.GetOrCreateSystemManaged<TradeCostSystem>();
                    var tradeCosts = tradeCostSystem.GetResourceTradeCosts().ToList();
                    var topImports = tradeCostSystem.GetImports().ToList();
                    var topExports = tradeCostSystem.GetExports().ToList();
                    // Update the binding with the new trade costs
                    m_TradeCostsBinding.Value = tradeCosts;
                    m_ImportsBinding.Value = topImports;
                    m_ExportsBinding.Value = topExports;
                    UpdateImportData();
                    UpdateExportData();
                    void UpdateImportData()
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
                    void UpdateExportData()
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
                
                    var workforcsSystem = base.World.GetOrCreateSystemManaged<WorkforceSystem>();
                    m_WorkforcesBinder.Value = workforcsSystem.m_Results.ToArray();
                
                    var workplacesSystem = base.World.GetOrCreateSystemManaged<WorkplacesSystem>();
                    m_WorkplacesBinder.Value = workplacesSystem.m_Results.ToArray();
                
            
                
                
                base.OnUpdate();
           }
        }
    }
}
      
   