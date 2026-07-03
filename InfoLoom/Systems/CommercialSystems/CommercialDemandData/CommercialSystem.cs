using System.Collections.Generic;
using Colossal.Collections;
using Colossal.Entities;
using Game;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.UI;
using ModsCommon.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Mod = InfoLoomTwo.InfoLoomMod;
namespace InfoLoomTwo.Systems.CommercialSystems.CommercialDemandData
{
    public partial class CommercialSystem : CommonUISystemBase
    {
        [BurstCompile]
        private struct UpdateCommercialDisplayJob : IJob
        {
            // Chunk iteration (free property counting) - matches game's pattern
            [ReadOnly] public NativeList<ArchetypeChunk> CommercialPropertyChunks;
            [ReadOnly] public ComponentTypeHandle<PrefabRef> PrefabType;
            [ReadOnly] public BufferTypeHandle<Renter> RenterType;
            [ReadOnly] public ComponentLookup<BuildingPropertyData> BuildingPropertyDatas;
            [ReadOnly] public ComponentLookup<CommercialCompany> CommercialCompanies;

            // Metrics inputs
            [ReadOnly] public ComponentLookup<ResourceData> ResourceDatas;
            [ReadOnly] public ResourcePrefabs ResourcePrefabs;
            [ReadOnly] public NativeArray<int> TaxRates;
            [ReadOnly] public NativeArray<int> Employables;
            [ReadOnly] public Workplaces FreeWorkplaces;
            [ReadOnly] public DemandParameterData DemandParameters;
            [ReadOnly] public ComponentLookup<Tourism> Tourisms;
            [ReadOnly] public Entity City;
            [ReadOnly] public NativeArray<int> GameDemands;

            // Company data inputs
            [ReadOnly] public NativeArray<int> ServicePropertyless;
            [ReadOnly] public NativeArray<int> CurrentAvailables;
            [ReadOnly] public NativeArray<int> TotalAvailables;
            [ReadOnly] public NativeArray<int> MaxServiceWorkers;
            [ReadOnly] public NativeArray<int> CurrentServiceWorkers;

            public int ComResDemValue;

            // Outputs
            public NativeArray<int> FreeProperties;
            public NativeArray<int> ResourceDemands;
            public NativeArray<int> Results;
            public NativeValue<Resource> IncludedResources;

            public void Execute()
            {
                // Phase 1: count free commercial properties (mirrors game's chunk loop)
                for (var k = 0; k < CommercialPropertyChunks.Length; k++)
                {
                    var chunk = CommercialPropertyChunks[k];
                    var prefabs = chunk.GetNativeArray(ref PrefabType);
                    var renters = chunk.GetBufferAccessor(ref RenterType);

                    for (var i = 0; i < chunk.Count; i++)
                    {
                        if (!BuildingPropertyDatas.HasComponent(prefabs[i].m_Prefab))
                            continue;

                        var hasCommercialTenant = false;
                        var renterBuffer = renters[i];
                        for (var j = 0; j < renterBuffer.Length; j++)
                            if (CommercialCompanies.HasComponent(renterBuffer[j].m_Renter))
                            {
                                hasCommercialTenant = true;
                                break;
                            }

                        if (!hasCommercialTenant)
                        {
                            var buildingData = BuildingPropertyDatas[prefabs[i].m_Prefab];
                            var iter = ResourceIterator.GetIterator();
                            while (iter.Next())
                                if ((buildingData.m_AllowedSold & iter.resource) != Resource.NoResource)
                                    FreeProperties[EconomyUtils.GetResourceIndex(iter.resource)]++;
                            if (buildingData.m_AllowedSold != Resource.NoResource)
                                Results[0]++;
                        }
                    }
                }

                // Phase 2: gather display metrics
                int numStandard = 0, numLeisure = 0;
                float shopStockingStd = 0f, shopStockingLei = 0f;
                float totalTaxRate = 0f, totalEmpCapacity = 0f;
                var resourceCount = 0;
                var sorted = new NativeList<(Resource resource, int demand)>(Allocator.Temp);
                var iterator = ResourceIterator.GetIterator();
                while (iterator.Next())
                {
                    if (!EconomyUtils.IsCommercialResource(iterator.resource))
                        continue;

                    var idx = EconomyUtils.GetResourceIndex(iterator.resource);

                    if (!ResourceDatas.HasComponent(ResourcePrefabs[iterator.resource]))
                        continue;

                    var resourceData = ResourceDatas[ResourcePrefabs[iterator.resource]];

                    ResourceDemands[idx] = GameDemands[idx];
                    Results[1] += ServicePropertyless[idx];

                    var storageRatio = CurrentAvailables[idx] / (1f + TotalAvailables[idx]);

                    var empCapacity = MaxServiceWorkers[idx] == 0
                        ? 0f
                        : (float)CurrentServiceWorkers[idx] / MaxServiceWorkers[idx];

                    // Hotel occupancy — only computed for Lodging resource, stored directly
                    if (iterator.resource == Resource.Lodging && Tourisms.HasComponent(City))
                    {
                        var tourism = Tourisms[City];
                        var requiredRooms =
                            (int)(tourism.m_CurrentTourists * DemandParameters.m_HotelRoomPercentRequirement);
                        Results[5] = requiredRooms > 0
                            ? (int)math.round(100f * tourism.m_Lodging.y / requiredRooms)
                            : 0;
                    }

                    if (resourceData.m_IsLeisure)
                    {
                        numLeisure++;
                        shopStockingLei += storageRatio;
                    }
                    else
                    {
                        numStandard++;
                        shopStockingStd += storageRatio;
                    }

                    totalTaxRate += TaxSystem.GetCommercialTaxRate(iterator.resource, TaxRates);
                    totalEmpCapacity += empCapacity;
                    resourceCount++;

                    sorted.Add((iterator.resource, ResourceDemands[idx]));
                }

                sorted.Sort(new DemandComparer());
                for (int i = 0; i < sorted.Length; i++)
                {
                    var (resource, demand) = sorted[i];
                    if (demand >= ComResDemValue) IncludedResources.value |= resource;
                }
                sorted.Dispose();



                for (var i = 0; i < 5; i++)
                {
                    var available = math.max(0, Employables[i] - FreeWorkplaces[i]);
                    if (i >= 2) Results[8] += available;
                    else Results[9] += available;
                }

                Results[2] = resourceCount > 0 ? (int)math.round(10f * totalTaxRate / resourceCount) : 0;
                Results[3] = numStandard > 0 ? (int)math.round(100f * shopStockingStd / numStandard) : 0;
                Results[4] = numLeisure > 0 ? (int)math.round(100f * shopStockingLei / numLeisure) : 0;
                // Results[5] = hotel occupancy % (set directly in loop above)
                Results[7] = resourceCount > 0 ? (int)math.round(1000f * totalEmpCapacity / resourceCount) : 0;
            }
        }

        protected override string ModId => InfoLoomMod.Instance.ModName;

        // System dependencies
        private ResourceSystem m_ResourceSystem;
        private TaxSystem m_TaxSystem;
        private CountHouseholdDataSystem m_CountHouseholdDataSystem;
        private CountWorkplacesSystem m_CountWorkplacesSystem;
        private CitySystem m_CitySystem;
        private CountCompanyDataSystem m_CountCompanyDataSystem;
        private CommercialDemandSystem m_CommercialDemandSystem;

        // Queries
        private EntityQuery m_EconomyParameterQuery;
        private EntityQuery m_DemandParameterQuery;
        private EntityQuery m_FreeCommercialQuery;
        private EntityQuery m_CommercialProcessDataQuery;

        // Persistent data
        public NativeArray<int> m_ResourceDemands;
        private NativeArray<int> m_FreeProperties;
        public NativeArray<int> m_Results;
        public NativeValue<Resource> m_IncludedResources;
        private UIUpdateState _updateState;

        // Panel state
        public bool IsPanelVisible { get; set; }

        protected override void OnCreate()
        {
            base.OnCreate();

            m_ResourceSystem = World.GetOrCreateSystemManaged<ResourceSystem>();
            m_TaxSystem = World.GetOrCreateSystemManaged<TaxSystem>();
            m_CountHouseholdDataSystem = World.GetOrCreateSystemManaged<CountHouseholdDataSystem>();
            m_CountWorkplacesSystem = World.GetOrCreateSystemManaged<CountWorkplacesSystem>();
            m_CitySystem = World.GetOrCreateSystemManaged<CitySystem>();
            m_CountCompanyDataSystem = World.GetOrCreateSystemManaged<CountCompanyDataSystem>();
            m_CommercialDemandSystem = World.GetOrCreateSystemManaged<CommercialDemandSystem>();

            m_EconomyParameterQuery = SystemAPI.QueryBuilder().WithAll<EconomyParameterData>().Build();
            m_DemandParameterQuery = SystemAPI.QueryBuilder().WithAll<DemandParameterData>().Build();
            m_FreeCommercialQuery = SystemAPI.QueryBuilder().WithAll<CommercialProperty>()
                .WithAll<PropertyOnMarket, PrefabRef>().WithNone<Abandoned, Destroyed, Deleted, Condemned, Temp>()
                .Build();
            m_CommercialProcessDataQuery =
                SystemAPI.QueryBuilder().WithAll<IndustrialProcessData, ServiceCompanyData>().Build();

            var resourceCount = EconomyUtils.ResourceCount;
            m_ResourceDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
            m_FreeProperties = new NativeArray<int>(resourceCount, Allocator.Persistent);
            m_Results = new NativeArray<int>(10, Allocator.Persistent);
            m_IncludedResources = new NativeValue<Resource>(Allocator.Persistent);

            RequireForUpdate(m_EconomyParameterQuery);
            RequireForUpdate(m_DemandParameterQuery);
            RequireForUpdate(m_CommercialProcessDataQuery);

            _updateState = UIUpdateState.Create(World, 512);
            Mod.log.Info("CommercialSystem created.");
        }

        protected override void OnDestroy()
        {
            m_ResourceDemands.Dispose();
            m_FreeProperties.Dispose();
            m_Results.Dispose();
            m_IncludedResources.Dispose();
            base.OnDestroy();
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 512;
        }

        protected override void OnUpdate()
        {
            if (!IsPanelVisible)
                return;

            if (!_updateState.Advance())
                return;
            m_IncludedResources.value = Resource.NoResource;
            m_Results.Fill(0);
            m_ResourceDemands.Fill(0);
            m_FreeProperties.Fill(0);

            JobHandle companyDeps;
            var commercialData = m_CountCompanyDataSystem.GetCommercialCompanyDatas(out companyDeps);

            JobHandle employDeps;
            var employables = m_CountHouseholdDataSystem.GetEmployables(out employDeps);

            // Read game demands — register us as a reader, get the write-dep handle
            JobHandle demandDeps = default;
            NativeArray<int> gameDemands = default;
            var demandSystem = World.GetExistingSystemManaged<CommercialDemandSystem>();
            if (demandSystem != null)
                gameDemands = demandSystem.GetResourceDemands(out demandDeps);
            else
                gameDemands = new NativeArray<int>(EconomyUtils.ResourceCount, World.UpdateAllocator.ToAllocator);

            // Build and schedule the single combined job — mirrors game's OnUpdate pattern
            var job = default(UpdateCommercialDisplayJob);
            job.CommercialPropertyChunks =
                m_FreeCommercialQuery.ToArchetypeChunkListAsync(World.UpdateAllocator.ToAllocator,
                    out var chunksHandle);
            job.PrefabType = SystemAPI.GetComponentTypeHandle<PrefabRef>(true);
            job.RenterType = SystemAPI.GetBufferTypeHandle<Renter>(true);
            job.BuildingPropertyDatas = SystemAPI.GetComponentLookup<BuildingPropertyData>(true);
            job.CommercialCompanies = SystemAPI.GetComponentLookup<CommercialCompany>(true);
            job.ResourceDatas = SystemAPI.GetComponentLookup<ResourceData>(true);
            job.ResourcePrefabs = m_ResourceSystem.GetPrefabs();
            job.TaxRates = m_TaxSystem.GetTaxRates();
            job.Employables = employables;
            job.FreeWorkplaces = m_CountWorkplacesSystem.GetFreeWorkplaces();
            job.DemandParameters = m_DemandParameterQuery.GetSingleton<DemandParameterData>();
            job.Tourisms = SystemAPI.GetComponentLookup<Tourism>(true);
            job.City = m_CitySystem.City;
            job.GameDemands = gameDemands;
            job.ServicePropertyless = commercialData.m_ServicePropertyless;
            job.CurrentAvailables = commercialData.m_CurrentAvailables;
            job.TotalAvailables = commercialData.m_TotalAvailables;
            job.MaxServiceWorkers = commercialData.m_MaxServiceWorkers;
            job.CurrentServiceWorkers = commercialData.m_CurrentServiceWorkers;
            job.ComResDemValue = Mod.setting.comResDemValue;
            job.FreeProperties = m_FreeProperties;
            job.ResourceDemands = m_ResourceDemands;
            job.Results = m_Results;
            job.IncludedResources = m_IncludedResources;
            var jobData = job;
            Dependency =
                jobData.Schedule(JobUtils.CombineDependencies(Dependency, chunksHandle, companyDeps, employDeps,
                    demandDeps));

            // Register as reader with systems whose data we read (matches game pattern)
            m_CountHouseholdDataSystem.AddHouseholdDataReader(Dependency);
            m_ResourceSystem.AddPrefabsReader(Dependency);
            m_TaxSystem.AddReader(Dependency);
            m_CommercialDemandSystem.AddReader(Dependency);

            // Complete for UI binding reads this frame
            Dependency.Complete();
        }
        public struct DemandComparer : IComparer<(Resource resource, int demand)>
        {
            public int Compare((Resource resource, int demand) a,
                               (Resource resource, int demand) b)
                => b.demand.CompareTo(a.demand); // descending
        }
    }
}