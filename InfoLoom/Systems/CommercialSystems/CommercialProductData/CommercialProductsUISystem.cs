using System;
using System.Linq;
using Game;
using Game.Economy;
using InfoLoomTwo.Extensions;
using Unity.Collections;
//using InfoLoomTwo.Systems.CommercialSystems.CommercialDemandPatch;
using InfoLoomTwo.Systems.CommercialSystems.CommercialDemandData;
using Colossal.UI.Binding;
using static InfoLoomTwo.Systems.CommercialSystems.CommercialProductData.CommercialProductsSystem;
using Game.Simulation;
//using InfoLoomTwo.Systems.CommercialSystems.CommercialDemandPatch;

namespace InfoLoomTwo.Systems.CommercialSystems.CommercialProductData
{
    public partial class CommercialProductsUISystem : ExtendedUISystemBase
    {
        private ValueBindingHelper<CommercialProductData[]> m_CommercialProductBinding;
        private ValueBindingHelper<CommercialDemandPatchData[]> m_CommercialDemandPatchBinding;
        public ValueBindingHelper<bool> m_MCDButton;
        
        
        public override GameMode gameMode => GameMode.Game;
        
        // Define a new struct for UI representation
        public struct CommercialProductData
        {
            public string ResourceName;
            public int Demand;
            public int Building;
            public int Free;
            public int Companies;
            public int Workers;
            public int SvcPercent;
            public int CapPercent;
            public int CapPerCompany;
            public int WrkPercent;
            public int TaxFactor;
        }
        public struct CommercialDemandPatchData
        {
            public Resource Resource;
            //public FixedString32Bytes Name; // resource name
            public int Demand; // company demand
            public int Building; // building demand
            public int Free; // free properties
            public int Companies; // num of companies
            public int Workers; // num of workers
            public int SvcFactor; // service availability
            public int SvcPercent;
            public int CapFactor; // sales capacity
            public int CapPercent;
            public int CapPerCompany;
            public int WrkFactor; // employee ratio
            public int WrkPercent;
            public int EduFactor; // educated employees
            public int TaxFactor; // tax factor
                                  //public FixedString512Bytes Details;

            public CommercialDemandPatchData(Resource resource)
            {
                Resource = resource;
                Demand = 0;
                Building = 0;
                Free = 0;
                Companies = 0;
                Workers = 0;
                SvcFactor = 0;
                SvcPercent = 0;
                CapFactor = 0;
                CapPercent = 0;
                CapPerCompany = 0;
                WrkFactor = 0;
                WrkPercent = 0;
                EduFactor = 0;
                TaxFactor = 0;
            }
        }
        

        private const string kGroup = "realEco";

        private SimulationSystem m_SimulationSystem;

        

        
        public bool MCDButton { get => m_MCDButton.Value; set => m_MCDButton.Value = value; }
        protected override void OnCreate()
        {
            base.OnCreate();
            m_CommercialProductBinding = CreateBinding("commercialProducts", Array.Empty<CommercialProductData>());
            m_CommercialDemandPatchBinding = CreateBinding("commercialDemand", Array.Empty<CommercialDemandPatchData>());
            //m_MCDButton = CreateBinding("MCDButton", Mod.setting.FeatureCommercialDemand);
            
            Mod.log.Info("CommercialProductsUISystem created.");
            

        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            // Filter and bind demand data, excluding unused resources
            m_CommercialProductBinding.Value = CommercialProductsSystem.m_DemandData
                .Where(d => d.Demand > 0 || d.Companies > 0 || d.Building > 0 || d.Free > 0)  // Only show relevant resources
                .Select(d => new CommercialProductData
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

            // If no resources are left, show "All" as a fallback
            if (m_CommercialProductBinding.Value.Length == 0 && CommercialProductsSystem.m_DemandData.Length > 0)
            {
                m_CommercialProductBinding.Value = new CommercialProductData[]
                {
                    new CommercialProductData
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

            //m_CommercialDemandPatchBinding.Value = ModifiedCommercialDemandSystem.m_DemandData.ToArray();
            //m_MCDButton.Value = Mod.setting.FeatureCommercialDemand;
             
        }
        
    }
    
}

            