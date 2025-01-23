using System;
using System.Linq;
using Game;
using Game.Economy;
using InfoLoomTwo.Extensions;
using Unity.Collections;

namespace InfoLoomTwo.Systems.IndustrialSystems.IndustrialProductData
{
    public partial class IndustrialProductsUISystem : ExtendedUISystemBase
    {
        private ValueBindingHelper<IndustrialProductData[]> m_IndustrialProductBinding;
        public override GameMode gameMode => GameMode.Game;

        // Define a new struct for UI representation
        public struct IndustrialProductData
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
            m_IndustrialProductBinding = CreateBinding("industrialProducts", Array.Empty<IndustrialProductData>());
            Mod.log.Info("IndustrialProductsUISystem created.");
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            // Filter and bind demand data, excluding unused resources
            m_IndustrialProductBinding.Value = IndustrialProductsSystem.m_DemandData
                .Where(d => d.Demand > 0 || d.Companies > 0 || d.Building > 0 || d.Free > 0)  // Only show relevant resources
                .Select(d => new IndustrialProductData
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
            if (m_IndustrialProductBinding.Value.Length == 0 && IndustrialProductsSystem.m_DemandData.Length > 0)
            {
                m_IndustrialProductBinding.Value = new IndustrialProductData[]
                {
                    new IndustrialProductData
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