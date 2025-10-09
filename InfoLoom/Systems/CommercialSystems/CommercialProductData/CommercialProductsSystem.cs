using System;
using Game;
using Game.Economy;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Game.City;
using Game.Prefabs;
using Unity.Burst;

namespace InfoLoomTwo.Systems.CommercialSystems.CommercialProductData
{
    [UpdateAfter(typeof(Game.Simulation.CommercialDemandSystem))]
    public partial class CommercialProductsSystem : GameSystemBase
    {
        public struct ProductData
        {
            public Resource ResourceName;
            public int Demand;
            public int Building;
            public int Free;
            public int Companies;
            public int Workers;
            public int WrkPercent;
            public int TaxFactor;
            
            public int CurrentTourists;
            public int AvailableLodging;
            public int RequiredRooms;
            
            public ProductData(Resource resource)
            {
                ResourceName = resource;
                Demand = 0;
                Building = 0;
                Free = 0;
                Companies = 0;
                Workers = 0;
                WrkPercent = 0;
                TaxFactor = 0;
                CurrentTourists = 0;
                AvailableLodging = 0;
                RequiredRooms = 0;
            }
        }

        [BurstCompile]
        private struct CollectProductDataJob : IJob
        {
            [ReadOnly]
            public NativeArray<int> m_ResourceDemands;
            
            [ReadOnly]
            public NativeArray<int> m_BuildingDemands;
            
            [ReadOnly]
            public NativeArray<int> m_ServiceCompanies;
            
            [ReadOnly]
            public NativeArray<int> m_CurrentServiceWorkers;
            
            [ReadOnly]
            public NativeArray<int> m_MaxServiceWorkers;
            
            [ReadOnly]
            public NativeArray<int> m_TaxRates;
            
            [ReadOnly]
            public ComponentLookup<Tourism> m_Tourisms;
            
            [ReadOnly]
            public Entity m_City;
            
            [ReadOnly]
            public DemandParameterData m_DemandParameters;
            
            public NativeArray<ProductData> m_ProductsData;

            public void Execute()
            {
                ResourceIterator iterator = ResourceIterator.GetIterator();
                while (iterator.Next())
                {
                    if (!EconomyUtils.IsCommercialResource(iterator.resource))
                        continue;
                        
                    int resourceIndex = EconomyUtils.GetResourceIndex(iterator.resource);
                    
                    ProductData data = new ProductData(iterator.resource);
                    data.Demand = m_ResourceDemands[resourceIndex];
                    data.Building = m_BuildingDemands[resourceIndex];
                    data.Companies = m_ServiceCompanies[resourceIndex];
                    data.Workers = m_CurrentServiceWorkers[resourceIndex];
                    data.WrkPercent = m_MaxServiceWorkers[resourceIndex] > 0 ? 
                        (100 * m_CurrentServiceWorkers[resourceIndex]) / m_MaxServiceWorkers[resourceIndex] : 0;
                    
                    // Calculate tax factor
                    float taxRate = TaxSystem.GetCommercialTaxRate(iterator.resource, m_TaxRates);
                    data.TaxFactor = Mathf.RoundToInt(-5f * (taxRate - 10f));
                    
                    // Special handling for lodging
                    if (iterator.resource == Resource.Lodging && m_Tourisms.HasComponent(m_City))
                    {
                        var tourism = m_Tourisms[m_City];
                        data.CurrentTourists = tourism.m_CurrentTourists;
                        data.AvailableLodging = tourism.m_Lodging.y;
                        data.RequiredRooms = (int)((float)tourism.m_CurrentTourists * m_DemandParameters.m_HotelRoomPercentRequirement);
                    }
                    
                    m_ProductsData[resourceIndex] = data;
                }
            }
        }

        public bool IsPanelVisible { get; set; }
        
        private Game.Simulation.CommercialDemandSystem m_CommercialDemandSystem;
        private CountCompanyDataSystem m_CountCompanyDataSystem;
        private TaxSystem m_TaxSystem;
        private CitySystem m_CitySystem;
        private EntityQuery m_DemandParameterQuery;
        
        public static NativeArray<ProductData> m_ProductsData;
        private JobHandle m_WriteDependencies;

        protected override void OnCreate()
        {
            base.OnCreate();
            
            m_CommercialDemandSystem = World.GetOrCreateSystemManaged<Game.Simulation.CommercialDemandSystem>();
            m_CountCompanyDataSystem = World.GetOrCreateSystemManaged<CountCompanyDataSystem>();
            m_TaxSystem = World.GetOrCreateSystemManaged<TaxSystem>();
            m_CitySystem = World.GetOrCreateSystemManaged<CitySystem>();
            
            m_DemandParameterQuery = GetEntityQuery(ComponentType.ReadOnly<DemandParameterData>());
            
            int resourceCount = EconomyUtils.ResourceCount;
            m_ProductsData = new NativeArray<ProductData>(resourceCount, Allocator.Persistent);
            
            RequireForUpdate(m_DemandParameterQuery);
        }

        protected override void OnDestroy()
        {
            if (m_ProductsData.IsCreated)
                m_ProductsData.Dispose();
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (!IsPanelVisible) return;

            // Get data from existing systems
            var resourceDemands = m_CommercialDemandSystem.GetResourceDemands(out var deps1);
            var buildingDemands = m_CommercialDemandSystem.GetBuildingDemands(out var deps2);
            var commercialData = m_CountCompanyDataSystem.GetCommercialCompanyDatas(out var deps3);
            var taxRates = m_TaxSystem.GetTaxRates();

            var job = new CollectProductDataJob
            {
                m_ResourceDemands = resourceDemands,
                m_BuildingDemands = buildingDemands,
                m_ServiceCompanies = commercialData.m_ServiceCompanies,
                m_CurrentServiceWorkers = commercialData.m_CurrentServiceWorkers,
                m_MaxServiceWorkers = commercialData.m_MaxServiceWorkers,
                m_TaxRates = taxRates,
                m_Tourisms = SystemAPI.GetComponentLookup<Tourism>(true),
                m_City = m_CitySystem.City,
                m_DemandParameters = m_DemandParameterQuery.GetSingleton<DemandParameterData>(),
                m_ProductsData = m_ProductsData
            };

            // Combine dependencies properly
            Dependency = JobHandle.CombineDependencies(Dependency, deps1);
            Dependency = JobHandle.CombineDependencies(Dependency, deps2);
            Dependency = JobHandle.CombineDependencies(Dependency, deps3);
            
            Dependency = job.Schedule(Dependency);
            m_WriteDependencies = Dependency;

            // Add readers
            m_CommercialDemandSystem.AddReader(Dependency);
            m_TaxSystem.AddReader(Dependency);
        }

        public NativeArray<ProductData> GetProductsData(out JobHandle deps)
        {
            deps = m_WriteDependencies;
            return m_ProductsData;
        }
    }
}