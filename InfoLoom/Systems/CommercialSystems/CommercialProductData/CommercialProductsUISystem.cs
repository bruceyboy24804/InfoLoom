using System;
using System.Linq;
using Game;
using Game.Economy;
using InfoLoomTwo.Extensions;
using Unity.Collections;

namespace InfoLoomTwo.Systems.CommercialSystems.CommercialProductData
{
    public partial class CommercialProductsUISystem : ExtendedUISystemBase
    {
        private ValueBindingHelper<CommercialProductData[]> m_CommercialProductBinding;
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

        protected override void OnCreate()
        {
            base.OnCreate();
            m_CommercialProductBinding = CreateBinding("commercialProducts", Array.Empty<CommercialProductData>());
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
        }

    }
}