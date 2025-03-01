using System;
using System.Collections.Generic;
using System.Linq;
using Game;
using Game.Companies;
using Game.Economy;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using InfoLoomTwo.Domain;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace InfoLoomTwo.Systems.TradeCostData
{
    public partial class TradeCostSystem : GameSystemBase
    {
        private EntityQuery m_Query;
        private EntityQuery m_ResourceQuery;
        private Dictionary<Resource, ResourceTradeCost> m_ResourceTradeCosts;
        private CommercialDemandSystem m_CommercialDemandSystem;
        private IndustrialDemandSystem m_IndustrialDemandSystem;
        private CountCompanyDataSystem m_CountCompanyDataSystem;
        private PrefabSystem m_PrefabSystem;
        private SimulationSystem m_SimulationSystem;
        public bool IsPanelVisible { get; set; }
        public bool ForceUpdate { get; private set; }
        public void ForceUpdateOnce()
        {
            ForceUpdate = true;
        }
        private int m_TotalImports;
        private int m_TotalExports;
        public int TotalImports => m_TotalImports;
        public int TotalExports => m_TotalExports;
        
        protected override void OnCreate()
        {
            m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
            m_CommercialDemandSystem = base.World.GetOrCreateSystemManaged<CommercialDemandSystem>();
            m_IndustrialDemandSystem = base.World.GetOrCreateSystemManaged<IndustrialDemandSystem>();
            m_CountCompanyDataSystem = base.World.GetOrCreateSystemManaged<CountCompanyDataSystem>();
            m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
            m_ResourceQuery = GetEntityQuery(ComponentType.ReadOnly<ResourceData>(), ComponentType.ReadOnly<TaxableResourceData>(), ComponentType.ReadOnly<PrefabData>());
            m_Query = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<TradeCost>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Game.Objects.OutsideConnection>() }
            });
            m_ResourceTradeCosts = new Dictionary<Resource, ResourceTradeCost>();
        }
        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 256;
        }
        protected override void OnUpdate()
        {
            if (!ShouldUpdate())
                return;

            UpdateTradeCosts();
            CalculateTotals();
        }

        private bool ShouldUpdate() => 
            IsPanelVisible && (ForceUpdate);

        private void UpdateTradeCosts()
        {
            m_ResourceTradeCosts.Clear();
            
            NativeArray<int> production = m_CountCompanyDataSystem.GetProduction(out var deps);
            NativeArray<int> industrialConsumption = m_IndustrialDemandSystem.GetConsumption(out var deps2);
            NativeArray<int> commercialConsumption = m_CommercialDemandSystem.GetConsumption(out var deps3);
            
            JobHandle.CompleteAll(ref deps, ref deps2, ref deps3);

            using var entities = m_Query.ToEntityArray(Allocator.Temp);
            using var resourceEntities = m_ResourceQuery.ToEntityArray(Allocator.TempJob);
            using var prefabData = m_ResourceQuery.ToComponentDataArray<PrefabData>(Allocator.TempJob);
            
            // Process trade costs and calculate averages
            for (int entityIndex = 0; entityIndex < entities.Length; entityIndex++)
            {
                var entity = entities[entityIndex];
                if (!EntityManager.HasBuffer<TradeCost>(entity))
                    continue;

                var tradeCostBuffer = EntityManager.GetBuffer<TradeCost>(entity);
                for (int costIndex = 0; costIndex < tradeCostBuffer.Length; costIndex++)
                {
                    var tradeCost = tradeCostBuffer[costIndex];
                    if (!m_ResourceTradeCosts.TryGetValue(tradeCost.m_Resource, out var resourceTradeCost))
                    {
                        resourceTradeCost = new ResourceTradeCost
                        {
                            Resource = tradeCost.m_Resource.ToString(),
                            Count = 0,
                            BuyCost = 0f,
                            SellCost = 0f,
                            ImportAmount = 0,
                            ExportAmount = 0
                        };
                        m_ResourceTradeCosts[tradeCost.m_Resource] = resourceTradeCost;
                    }

                    resourceTradeCost.Count++;
                    resourceTradeCost.BuyCost += tradeCost.m_BuyCost;
                    resourceTradeCost.SellCost += tradeCost.m_SellCost;
                }
            }

            // Calculate averages for buy/sell costs
            var resourceKeys = m_ResourceTradeCosts.Keys.ToArray();
            for (int i = 0; i < resourceKeys.Length; i++)
            {
                var resource = m_ResourceTradeCosts[resourceKeys[i]];
                if (resource.Count > 0)
                {
                    resource.BuyCost = (float)Math.Round(resource.BuyCost / resource.Count, 2);
                    resource.SellCost = (float)Math.Round(resource.SellCost / resource.Count, 2);
                }
            }

            // Update import/export amounts
            for (int i = 0; i < resourceEntities.Length; i++)
            {
                var prefab = m_PrefabSystem.GetPrefab<ResourcePrefab>(prefabData[i]);
                var resource = EconomyUtils.GetResource(prefab.m_Resource);
                var resourceIndex = EconomyUtils.GetResourceIndex(resource);

                var (importAmount, exportAmount) = CalculateTradeAmounts(
                    production[resourceIndex],
                    industrialConsumption[resourceIndex],
                    commercialConsumption[resourceIndex]);

                if (!m_ResourceTradeCosts.TryGetValue(resource, out var resourceTradeCost))
                {
                    resourceTradeCost = new ResourceTradeCost
                    {
                        Resource = resource.ToString(),
                        Count = 0,
                        BuyCost = 0f,
                        SellCost = 0f
                    };
                    m_ResourceTradeCosts[resource] = resourceTradeCost;
                }

                resourceTradeCost.ImportAmount = importAmount;
                resourceTradeCost.ExportAmount = exportAmount;
            }
            
            ForceUpdate = false;
        }

        private void CalculateTotals()
        {
            m_TotalImports = m_ResourceTradeCosts.Values.Sum(x => x.ImportAmount);
            m_TotalExports = m_ResourceTradeCosts.Values.Sum(x => x.ExportAmount);
        }

        private (int importAmount, int exportAmount) CalculateTradeAmounts(int production, int industrialDemand, int commercialDemand)
        {
            var totalDemand = industrialDemand + commercialDemand;
            var satisfiedDemand = math.min(totalDemand, production);
            var satisfiedIndustrial = math.min(industrialDemand, satisfiedDemand);
            var unsatisfiedIndustrial = industrialDemand - satisfiedIndustrial;
            var satisfiedCommercial = math.min(commercialDemand, satisfiedDemand - satisfiedIndustrial);

            var importAmount = RoundAndTruncateToFiveDigits(commercialDemand - satisfiedCommercial + unsatisfiedIndustrial);
            var exportAmount = RoundAndTruncateToFiveDigits(production - satisfiedDemand);

            return (importAmount, exportAmount);
        }

        private int RoundAndTruncateToFiveDigits(int value)
        {
            if (value >= 100000)
            {
                int digits = (int)Math.Floor(Math.Log10(value)) + 1;
                int scale = (int)Math.Pow(10, digits - 5);
                return ((value + scale / 2) / scale) * scale / 10;
            }
            return value;
        }

        public IEnumerable<ResourceTradeCost> GetResourceTradeCosts()
        {
            return m_ResourceTradeCosts.Values;
        }
    }
}