using System;
using System.Collections.Generic;
using System.Linq;
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
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class TradeCostSystem : SystemBase
    {
        private EntityQuery m_Query;
        private EntityQuery m_ResourceQuery;
        private Dictionary<Resource, ResourceTradeCost> m_ResourceTradeCosts;
        private CommercialDemandSystem m_CommercialDemandSystem;
        private IndustrialDemandSystem m_IndustrialDemandSystem;
        private CountCompanyDataSystem m_CountCompanyDataSystem;
        private PrefabSystem m_PrefabSystem;
        public List<TradeCostResource> m_Imports;
        public List<TradeCostResource> m_Exports;

        protected override void OnCreate()
        {
            m_CommercialDemandSystem = base.World.GetOrCreateSystemManaged<CommercialDemandSystem>();
            m_IndustrialDemandSystem = base.World.GetOrCreateSystemManaged<IndustrialDemandSystem>();
            m_CountCompanyDataSystem = base.World.GetOrCreateSystemManaged<CountCompanyDataSystem>();
            m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
            m_ResourceQuery = GetEntityQuery(ComponentType.ReadOnly<ResourceData>(), ComponentType.ReadOnly<TaxableResourceData>(), ComponentType.ReadOnly<PrefabData>());
            m_Imports = new List<TradeCostResource>(41);
            m_Exports = new List<TradeCostResource>(41);
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

		    for (int i = 0; i < entities.Length; i++)
		    {
		        var entity = entities[i];
		        if (!entityManager.HasBuffer<TradeCost>(entity))
		            continue;

		        DynamicBuffer<TradeCost> tradeCostBuffer = entityManager.GetBuffer<TradeCost>(entity);

		        for (int j = 0; j < tradeCostBuffer.Length; j++)
		        {
		            var tradeCost = tradeCostBuffer[j];
		            if (!m_ResourceTradeCosts.TryGetValue(tradeCost.m_Resource, out var resourceTradeCost))
		            {
		                resourceTradeCost = new ResourceTradeCost
		                {
		                    Resource = tradeCost.m_Resource.ToString(),
		                    Count = 0,
		                    BuyCost = 0f,
		                    SellCost = 0f,
		                };
		            }

		            resourceTradeCost.Count++;
		            resourceTradeCost.BuyCost += tradeCost.m_BuyCost;
		            resourceTradeCost.SellCost += tradeCost.m_SellCost;

		            m_ResourceTradeCosts[tradeCost.m_Resource] = resourceTradeCost;
		        }
		    }

		    entities.Dispose();

		    UpdateCache();
		    UpdateExportData();
		    UpdateImportData();
		}


        public IEnumerable<ResourceTradeCost> GetResourceTradeCosts()
        {
            return m_ResourceTradeCosts.Values;
        }
        private void UpdateCache()
		{
			NativeArray<Entity> nativeArray = m_ResourceQuery.ToEntityArray(Allocator.TempJob);
			NativeArray<PrefabData> nativeArray2 = m_ResourceQuery.ToComponentDataArray<PrefabData>(Allocator.TempJob);
			JobHandle deps;
			NativeArray<int> production = m_CountCompanyDataSystem.GetProduction(out deps);
			JobHandle deps2;
			NativeArray<int> consumption = m_IndustrialDemandSystem.GetConsumption(out deps2);
			JobHandle deps3;
			NativeArray<int> consumption2 = m_CommercialDemandSystem.GetConsumption(out deps3);
			JobHandle.CompleteAll(ref deps, ref deps2, ref deps3);
			m_Imports.Clear();
			m_Exports.Clear();
			try
			{
				for (int i = 0; i < nativeArray.Length; i++)
		        {
		            ResourcePrefab prefab = m_PrefabSystem.GetPrefab<ResourcePrefab>(nativeArray2[i]);
		            int resourceIndex = EconomyUtils.GetResourceIndex(EconomyUtils.GetResource(prefab.m_Resource));
		            int num = production[resourceIndex];
		            int num2 = consumption[resourceIndex];
		            int num3 = consumption2[resourceIndex];
		            int num4 = math.min(num2 + num3, num);
		            int num5 = math.min(num2, num4);
		            int num6 = num2 - num5;
		            int num7 = math.min(num3, num4 - num5);
		            
		            int amount = RoundAndTruncateToFiveDigits(num3 - num7 + num6);
		            int amount2 = RoundAndTruncateToFiveDigits(num - num4);
		            
		            m_Imports.Add(new TradeCostResource(prefab.name, amount));
		            m_Exports.Add(new TradeCostResource(prefab.name, amount2));
		        }
				
			}
			finally
			{
				nativeArray.Dispose();
				nativeArray2.Dispose();
			}
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
				
				num += m_Exports[i].Amount;
			}
			
		}
		public List<TradeCostResource> GetImports()
        {
            return m_Imports.ToList();
        }

		public  List<TradeCostResource> GetExports()
		{
			return m_Exports.ToList();
		}
    }
}