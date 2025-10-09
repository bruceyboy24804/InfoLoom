using Game;
using Game.Economy;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Game.Prefabs;
using Unity.Burst;
using Unity.Mathematics;

namespace InfoLoomTwo.Systems.IndustrialSystems.IndustrialProductData
{
    [UpdateAfter(typeof(IndustrialDemandSystem))]
    public partial class IndustrialProductsSystem : GameSystemBase
    {
        public struct IndustrialDemandData
        {
            public Resource ResourceName;
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

            public IndustrialDemandData(Resource resource)
            {
                ResourceName = resource;
                Demand = Building = Free = Companies = Workers =
                SvcPercent = CapPercent = CapPerCompany = WrkPercent = TaxFactor = 0;
            }
        }

        [BurstCompile]
        private struct CollectIndustrialDataJob : IJob
        {
            [ReadOnly]
            public NativeArray<int> m_ResourceDemands;

            [ReadOnly]
            public NativeArray<int> m_IndustrialBuildingDemands;

            [ReadOnly]
            public NativeArray<int> m_FreeProperties;

            [ReadOnly]
            public NativeArray<int> m_Productions;
            
            [ReadOnly]
            public NativeArray<int> m_IndustrialCompanies;

            [ReadOnly]
            public NativeArray<int> m_CurrentIndustrialWorkers;

            [ReadOnly]
            public NativeArray<int> m_MaxIndustrialWorkers;
            
            [ReadOnly]
            public NativeArray<int> m_TaxRates;
            
            public NativeArray<int> m_Storages;
            [ReadOnly]
            public ComponentLookup<ResourceData> m_ResourceDatas;

            [ReadOnly]
            public ResourcePrefabs m_ResourcePrefabs;

            public NativeArray<IndustrialDemandData> m_DemandData;

            public void Execute()
            {
                ResourceIterator iterator = ResourceIterator.GetIterator();
                while (iterator.Next())
                {
                    if (!ShouldProcessResource(iterator.resource))
                        continue;
                    int resourceIndex = EconomyUtils.GetResourceIndex(iterator.resource);

                    // Check if this is an office resource (weightless) or industrial
                    bool isOfficeResource = false;
                    Entity resourcePrefab = m_ResourcePrefabs[iterator.resource];
                    if (m_ResourceDatas.HasComponent(resourcePrefab))
                    {
                        var resourceData = m_ResourceDatas[resourcePrefab];
                        isOfficeResource = resourceData.m_Weight == 0.0f && resourceData.m_IsProduceable;
                    }

                    int production = m_Productions[resourceIndex];
                    int demand = m_ResourceDemands[resourceIndex];
                    int companies = m_IndustrialCompanies[resourceIndex];

                    
                    // Calculate workforce utilization percentage based on resource type
                    int wrkPercent = m_MaxIndustrialWorkers[resourceIndex] > 0 ? 
                        (100 * m_CurrentIndustrialWorkers[resourceIndex]) / m_MaxIndustrialWorkers[resourceIndex] : 0;
                    int workers = m_CurrentIndustrialWorkers[resourceIndex];
                    
                   

                    // Calculate tax factor
                    float taxRate = isOfficeResource ? 
                        TaxSystem.GetOfficeTaxRate(iterator.resource, m_TaxRates) :
                        TaxSystem.GetIndustrialTaxRate(iterator.resource, m_TaxRates);
                    int taxFactor = Mathf.RoundToInt(-5f * (taxRate - 10f));

                    var data = new IndustrialDemandData(iterator.resource)
                    {
                        Demand = demand,
                        Building = m_IndustrialBuildingDemands[resourceIndex],
                        Free = m_FreeProperties[resourceIndex],
                        Companies = companies,
                        Workers = workers,
                        WrkPercent = wrkPercent,
                        TaxFactor = taxFactor
                    };

                    m_DemandData[resourceIndex] = data;
                }
            }
        }

        public bool IsPanelVisible { get; set; }

        private IndustrialDemandSystem m_IndustrialDemandSystem;
        private ResourceSystem m_ResourceSystem;
        private CountCompanyDataSystem m_CountCompanyDataSystem;
        private TaxSystem m_TaxSystem;

        public static NativeArray<IndustrialDemandData> m_DemandData;
        private JobHandle m_WriteDependencies;

        private EntityQuery m_EconomyParameterQuery;
        private EntityQuery m_DemandParameterQuery;

        private NativeArray<int> _FreeProperties;
        private NativeArray<int> _Storages;
        protected override void OnCreate()
        {
            base.OnCreate();
            
            m_IndustrialDemandSystem = World.GetOrCreateSystemManaged<IndustrialDemandSystem>();
            m_ResourceSystem = World.GetOrCreateSystemManaged<ResourceSystem>();
            m_CountCompanyDataSystem = World.GetOrCreateSystemManaged<CountCompanyDataSystem>();
            m_TaxSystem = World.GetOrCreateSystemManaged<TaxSystem>();
            
            m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
            m_DemandParameterQuery = GetEntityQuery(ComponentType.ReadOnly<DemandParameterData>());

            int resourceCount = EconomyUtils.ResourceCount;
            m_DemandData = new NativeArray<IndustrialDemandData>(resourceCount, Allocator.Persistent);
            _FreeProperties = new NativeArray<int>(resourceCount, Allocator.Persistent);
            _Storages = new NativeArray<int>(resourceCount, Allocator.Persistent);
            RequireForUpdate(m_EconomyParameterQuery);
            RequireForUpdate(m_DemandParameterQuery);
        }

        protected override void OnDestroy()
        {
            if (m_DemandData.IsCreated)
                m_DemandData.Dispose();
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (!IsPanelVisible) return;

            // Get data from existing systems
            var resourceDemands = m_IndustrialDemandSystem.GetIndustrialResourceDemands(out var deps1);
            var industrialBuildingDemands = m_IndustrialDemandSystem.GetBuildingDemands(out var deps2);
            CountCompanyDataSystem.IndustrialCompanyDatas industrialData = m_CountCompanyDataSystem.GetIndustrialCompanyDatas(out var deps3);
            var taxRates = m_TaxSystem.GetTaxRates();

            var job = new CollectIndustrialDataJob
            {
                m_ResourceDemands = resourceDemands,
                m_IndustrialBuildingDemands = industrialBuildingDemands,
                m_FreeProperties = _FreeProperties,
                m_Storages = _Storages,
                m_Productions = industrialData.m_Production,
                m_IndustrialCompanies = industrialData.m_ProductionCompanies,
                m_CurrentIndustrialWorkers = industrialData.m_CurrentProductionWorkers,
                m_MaxIndustrialWorkers = industrialData.m_MaxProductionWorkers,
                m_TaxRates = taxRates,
                m_ResourceDatas = SystemAPI.GetComponentLookup<ResourceData>(true),
                m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
                m_DemandData = m_DemandData
            };

            // Combine dependencies properly
            Dependency = JobHandle.CombineDependencies(Dependency, deps1);
            Dependency = JobHandle.CombineDependencies(Dependency, deps2);
            Dependency = JobHandle.CombineDependencies(Dependency, deps3);

            Dependency = job.Schedule(Dependency);
            m_WriteDependencies = Dependency;

            // Add readers
            m_IndustrialDemandSystem.AddReader(Dependency);
            m_CountCompanyDataSystem.AddReader(Dependency);
            m_ResourceSystem.AddPrefabsReader(Dependency);
            m_TaxSystem.AddReader(Dependency);
        }

        public NativeArray<IndustrialDemandData> GetDemandData(out JobHandle deps)
        {
            deps = m_WriteDependencies;
            return m_DemandData;
        }

        public IndustrialDemandData GetDemandDataForResource(Resource resource)
        {
            int index = EconomyUtils.GetResourceIndex(resource);
            if (index >= 0 && index < m_DemandData.Length)
            {
                return m_DemandData[index];
            }
            return new IndustrialDemandData(resource);
        }
         private static bool ShouldProcessResource(Resource resource)
        {
            return resource != Resource.Money &&
                   resource != Resource.NoResource && 
                   resource != Resource.UnsortedMail && 
                   resource != Resource.OutgoingMail &&
                   resource != Resource.LocalMail &&
                   resource != Resource.Garbage &&
                   resource != Resource.Lodging &&
                   resource != Resource.Entertainment &&
                   resource != Resource.Recreation;
        }
    }
}
