using System.Collections.Generic;
using Game.Companies;
using Game.Economy;
using Game.Objects;
using InfoLoomTwo.Domain;
using Unity.Collections;
using Unity.Entities;

namespace InfoLoomTwo.Systems.TradeCostData
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class TradeCostSystem : SystemBase
    {
        private EntityQuery m_Query;
        private Dictionary<Resource, ResourceTradeCost> m_ResourceTradeCosts;

        protected override void OnCreate()
        {
            m_Query = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<TradeCost>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Game.Objects.OutsideConnection>() }
            });
            m_ResourceTradeCosts = new Dictionary<Resource, ResourceTradeCost>();
        }

        protected override void OnUpdate()
        {
            m_ResourceTradeCosts.Clear();

            // Retrieve all entities matching the query
            NativeArray<Entity> entities = m_Query.ToEntityArray(Allocator.Temp);
            EntityManager entityManager = World.EntityManager;

            foreach (var entity in entities)
            {
                if (!entityManager.HasBuffer<TradeCost>(entity))
                    continue;

                DynamicBuffer<TradeCost> tradeCostBuffer = entityManager.GetBuffer<TradeCost>(entity);

                foreach (var tradeCost in tradeCostBuffer)
                {
                    if (!m_ResourceTradeCosts.TryGetValue(tradeCost.m_Resource, out var resourceTradeCost))
                    {
                        resourceTradeCost = new ResourceTradeCost
                        {
                            Resource = tradeCost.m_Resource.ToString(),
                            Count = 0,
                            BuyCost = 0f,
                            SellCost = 0f
                        };
                    }

                    resourceTradeCost.Count++;
                    resourceTradeCost.BuyCost += tradeCost.m_BuyCost;
                    resourceTradeCost.SellCost += tradeCost.m_SellCost;

                    m_ResourceTradeCosts[tradeCost.m_Resource] = resourceTradeCost;
                }
            }

            entities.Dispose();
        }

        public IEnumerable<ResourceTradeCost> GetResourceTradeCosts()
        {
            return m_ResourceTradeCosts.Values;
        }
    }
}