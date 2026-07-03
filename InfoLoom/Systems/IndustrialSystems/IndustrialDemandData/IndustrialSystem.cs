using Colossal.Collections;
using Colossal.Entities;
using Game;
using Game.Buildings;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.UI;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using OutsideConnection = Game.Objects.OutsideConnection;
using StorageCompany = Game.Companies.StorageCompany;
using Mod = InfoLoomTwo.InfoLoomMod;

namespace InfoLoomTwo.Systems.IndustrialSystems.IndustrialDemandData
{
    public partial class IndustrialSystem : UISystemBase
    {
        // m_Results array indices:
        // INDUSTRIAL (0-9):
        //   [0] - Free industrial properties
        //   [1] - Propertyless industrial companies
        //   [2] - Average tax rate (x10)
        //   [3] - Production capacity / local demand (percentage)
        //   [4] - Employee capacity ratio (x1000)
        //   [5] - Free storage properties ✅
        //   [6] - Propertyless storage companies ✅
        //   [7] - Input utilization (percentage) ✅
        //   [8] - Available educated workforce
        //   [9] - Available uneducated workforce
        // OFFICE (10-14):
        //   [10] - Free office properties
        //   [11] - Propertyless office companies
        //   [12] - Average tax rate (x10)
        //   [13] - Production capacity / local demand (percentage)
        //   [14] - Employee capacity ratio (x1000)
        // STORAGE (15):
        //   [15] - Storage company demand ✅

        // Constants
        private const Resource kOfficeResources =
            Resource.Software | Resource.Media | Resource.Telecom | Resource.Financial;

        private const Resource kIndustryResources =
            Resource.ConvenienceFood | Resource.Food | Resource.Timber | Resource.Paper |
            Resource.Furniture | Resource.Vehicles | Resource.Petrochemicals | Resource.Plastics |
            Resource.Metals | Resource.Electronics | Resource.Steel | Resource.Minerals |
            Resource.Concrete | Resource.Machinery | Resource.Chemicals | Resource.Pharmaceuticals |
            Resource.Beverages | Resource.Textiles;

        // System dependencies
        private ResourceSystem m_ResourceSystem;
        private CitySystem m_CitySystem;
        private TaxSystem m_TaxSystem;
        private CountHouseholdDataSystem m_CountHouseholdDataSystem;
        private CountWorkplacesSystem m_CountWorkplacesSystem;
        private CountCompanyDataSystem m_CountCompanyDataSystem;
        private IndustrialDemandSystem m_IndustrialDemandSystem;

        // Queries
        private EntityQuery m_EconomyParameterQuery;
        private EntityQuery m_DemandParameterQuery;
        private EntityQuery m_FreeIndustrialQuery;
        private EntityQuery m_ProcessDataQuery;
        private EntityQuery m_StorageCompanyQuery;

        // Persistent data
        public NativeArray<int> m_ResourceDemands;
        private NativeArray<int> m_FreeProperties;
        public NativeArray<int> m_Results;
        public NativeValue<Resource> m_IncludedResources;
        private NativeArray<int> m_StorageCapacities;

        // Panel state
        public bool IsPanelVisible { get; set; }

        protected override void OnCreate()
        {
            base.OnCreate();

            m_ResourceSystem = World.GetOrCreateSystemManaged<ResourceSystem>();
            m_CitySystem = World.GetOrCreateSystemManaged<CitySystem>();
            m_TaxSystem = World.GetOrCreateSystemManaged<TaxSystem>();
            m_CountHouseholdDataSystem = World.GetOrCreateSystemManaged<CountHouseholdDataSystem>();
            m_CountWorkplacesSystem = World.GetOrCreateSystemManaged<CountWorkplacesSystem>();
            m_CountCompanyDataSystem = World.GetOrCreateSystemManaged<CountCompanyDataSystem>();
            m_IndustrialDemandSystem = World.GetOrCreateSystemManaged<IndustrialDemandSystem>();


            m_EconomyParameterQuery = SystemAPI.QueryBuilder().WithAll<EconomyParameterData>().Build();
            m_DemandParameterQuery = SystemAPI.QueryBuilder().WithAll<DemandParameterData>().Build();

            m_FreeIndustrialQuery = SystemAPI.QueryBuilder().WithAll<IndustrialProperty>()
                .WithAll<PropertyOnMarket, PrefabRef>().WithNone<Abandoned, Destroyed, Deleted, Condemned, Temp>()
                .Build();
            m_ProcessDataQuery = SystemAPI.QueryBuilder().WithAll<IndustrialProcessData>()
                .WithNone<ServiceCompanyData>().Build();
            m_StorageCompanyQuery = SystemAPI.QueryBuilder().WithAll<PrefabRef, StorageCompany>()
                .WithNone<OutsideConnection, Deleted, Temp>().Build();

            var resourceCount = EconomyUtils.ResourceCount;
            m_ResourceDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
            m_FreeProperties = new NativeArray<int>(resourceCount, Allocator.Persistent);
            m_Results = new NativeArray<int>(16, Allocator.Persistent);
            m_IncludedResources = new NativeValue<Resource>(Allocator.Persistent);
            m_StorageCapacities = new NativeArray<int>(resourceCount, Allocator.Persistent);

            RequireForUpdate(m_EconomyParameterQuery);
            RequireForUpdate(m_DemandParameterQuery);
            RequireForUpdate(m_ProcessDataQuery);
        }

        protected override void OnDestroy()
        {
            m_ResourceDemands.Dispose();
            m_FreeProperties.Dispose();
            m_Results.Dispose();
            m_IncludedResources.Dispose();
            m_StorageCapacities.Dispose();
            base.OnDestroy();
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 512;
        }

        [BurstCompile]
        private struct UpdateIndustrialDisplayJob : IJob
        {
            [ReadOnly] public NativeList<ArchetypeChunk> IndustrialPropertyChunks;
            [ReadOnly] public NativeList<ArchetypeChunk> StorageCompanyChunks;
            [ReadOnly] public NativeList<ArchetypeChunk> ProcessDataChunks;

            [ReadOnly] public EntityTypeHandle EntityType;
            [ReadOnly] public ComponentTypeHandle<PrefabRef> PrefabType;
            [ReadOnly] public BufferTypeHandle<Renter> RenterType;
            [ReadOnly] public ComponentTypeHandle<IndustrialProcessData> ProcessDataType;

            [ReadOnly] public ComponentLookup<BuildingPropertyData> BuildingPropertyDatas;
            [ReadOnly] public ComponentLookup<IndustrialCompany> IndustrialCompanies;
            [ReadOnly] public ComponentLookup<Attached> Attached;
            [ReadOnly] public ComponentLookup<PrefabRef> Prefabs;
            [ReadOnly] public ComponentLookup<IndustrialProcessData> ProcessDatas;
            [ReadOnly] public ComponentLookup<PropertyRenter> PropertyRenters;
            [ReadOnly] public ComponentLookup<StorageLimitData> StorageLimitDatas;
            [ReadOnly] public ComponentLookup<SpawnableBuildingData> SpawnableBuildingDatas;
            [ReadOnly] public ComponentLookup<BuildingData> BuildingDatas;
            [ReadOnly] public ComponentLookup<ResourceData> ResourceDatas;

            [ReadOnly] public ResourcePrefabs ResourcePrefabs;
            [ReadOnly] public NativeArray<int> TaxRates;
            [ReadOnly] public NativeArray<int> Employables;
            [ReadOnly] public Workplaces FreeWorkplaces;

            [ReadOnly] public NativeArray<int> GameResourceDemands;
            [ReadOnly] public NativeArray<int> GameBuildingDemands;

            // IndustrialCompanyDatas fields
            [ReadOnly] public NativeArray<int> CompanyDemand;
            [ReadOnly] public NativeArray<int> CompanyProduction;
            [ReadOnly] public NativeArray<int> MaxProductionWorkers;
            [ReadOnly] public NativeArray<int> CurrentProductionWorkers;
            [ReadOnly] public NativeArray<int> ProductionPropertyless;

            public int IndResDemValue;

            // Outputs
            public NativeArray<int> FreeProperties;
            public NativeArray<int> Results;
            public NativeArray<int> ResourceDemands;
            public NativeValue<Resource> IncludedResources;
            public NativeArray<int> StorageCapacities;

            private const Resource kOfficeResources =
                Resource.Software | Resource.Media | Resource.Telecom | Resource.Financial;

            private const Resource kIndustryResources =
                Resource.ConvenienceFood | Resource.Food | Resource.Timber | Resource.Paper |
                Resource.Furniture | Resource.Vehicles | Resource.Petrochemicals | Resource.Plastics |
                Resource.Metals | Resource.Electronics | Resource.Steel | Resource.Minerals |
                Resource.Concrete | Resource.Machinery | Resource.Chemicals | Resource.Pharmaceuticals |
                Resource.Beverages | Resource.Textiles;

            public void Execute()
            {
                const int kStorageCompanyEstimateLimit = 864000;
                const int kStorageProductionDemand = 2000;

                // Phase 1: Count free industrial/office/storage properties
                for (var ci = 0; ci < IndustrialPropertyChunks.Length; ci++)
                {
                    var chunk = IndustrialPropertyChunks[ci];
                    var prefabRefs = chunk.GetNativeArray(ref PrefabType);
                    var renters = chunk.GetBufferAccessor(ref RenterType);
                    var entities = chunk.GetNativeArray(EntityType);

                    for (var i = 0; i < chunk.Count; i++)
                    {
                        if (!BuildingPropertyDatas.HasComponent(prefabRefs[i].m_Prefab))
                            continue;

                        var hasTenant = false;
                        var renterBuffer = renters[i];
                        for (var j = 0; j < renterBuffer.Length; j++)
                            if (IndustrialCompanies.HasComponent(renterBuffer[j].m_Renter))
                            {
                                hasTenant = true;
                                break;
                            }

                        if (!hasTenant)
                        {
                            var buildingData = BuildingPropertyDatas[prefabRefs[i].m_Prefab];
                            if (Attached.TryGetComponent(entities[i], out var attachedData) &&
                                Prefabs.TryGetComponent(attachedData.m_Parent, out var parentPrefab) &&
                                BuildingPropertyDatas.TryGetComponent(parentPrefab.m_Prefab,
                                    out var parentBuildingData))
                                buildingData.m_AllowedManufactured &= parentBuildingData.m_AllowedManufactured;

                            var iter = ResourceIterator.GetIterator();
                            while (iter.Next())
                                if ((buildingData.m_AllowedManufactured & iter.resource) != Resource.NoResource)
                                    FreeProperties[EconomyUtils.GetResourceIndex(iter.resource)]++;

                            if ((buildingData.m_AllowedManufactured & kIndustryResources) != Resource.NoResource)
                                Results[0]++;
                            if ((buildingData.m_AllowedManufactured & kOfficeResources) != Resource.NoResource)
                                Results[10]++;
                            if (buildingData.m_AllowedStored != Resource.NoResource)
                                Results[5]++;
                        }
                    }
                }

                // Phase 2: Count storage companies and accumulate capacities
                var storageCompaniesWithoutProperty = 0;
                for (var ci = 0; ci < StorageCompanyChunks.Length; ci++)
                {
                    var chunk = StorageCompanyChunks[ci];
                    var prefabs = chunk.GetNativeArray(ref PrefabType);
                    var entities = chunk.GetNativeArray(EntityType);

                    for (var i = 0; i < chunk.Count; i++)
                    {
                        var companyPrefab = prefabs[i].m_Prefab;
                        var entity = entities[i];

                        if (!ProcessDatas.HasComponent(companyPrefab))
                            continue;

                        var resourceIdx =
                            EconomyUtils.GetResourceIndex(ProcessDatas[companyPrefab].m_Output.m_Resource);
                        var hasValidProperty = PropertyRenters.HasComponent(entity) &&
                                               Prefabs.HasComponent(PropertyRenters[entity].m_Property);

                        if (!hasValidProperty)
                        {
                            storageCompaniesWithoutProperty++;
                            StorageCapacities[resourceIdx] += kStorageCompanyEstimateLimit;
                        }
                        else
                        {
                            var property = PropertyRenters[entity].m_Property;
                            var propertyPrefab = Prefabs[property].m_Prefab;
                            if (StorageLimitDatas.HasComponent(companyPrefab) &&
                                SpawnableBuildingDatas.HasComponent(propertyPrefab) &&
                                BuildingDatas.HasComponent(propertyPrefab))
                                StorageCapacities[resourceIdx] += StorageLimitDatas[companyPrefab]
                                    .GetAdjustedLimitForWarehouse(
                                        SpawnableBuildingDatas[propertyPrefab], BuildingDatas[propertyPrefab]);
                        }
                    }
                }

                Results[6] = storageCompaniesWithoutProperty;

                // Phase 3: Gather display metrics per resource
                int numIndustry = 0, numOffice = 0;
                float prodCapInd = 0f, prodCapOff = 0f;
                float empCapInd = 0f, empCapOff = 0f;
                float taxRateInd = 0f, taxRateOff = 0f;
                var inputUtil = 0f;
                var numInputs = 0;
                var storageCompanyDemand = 0;

                var iterator = ResourceIterator.GetIterator();
                while (iterator.Next())
                {
                    var idx = EconomyUtils.GetResourceIndex(iterator.resource);
                    if (!ResourceDatas.HasComponent(ResourcePrefabs[iterator.resource]))
                        continue;

                    var resourceData = ResourceDatas[ResourcePrefabs[iterator.resource]];
                    var isTradable = resourceData.m_IsTradable;
                    var isOffice = resourceData.m_Weight == 0f;
                    var isMaterial = resourceData.m_IsMaterial;

                    if (isTradable && !isOffice && CompanyDemand[idx] > kStorageProductionDemand
                        && StorageCapacities[idx] < CompanyDemand[idx])
                        storageCompanyDemand++;

                    if (!resourceData.m_IsProduceable || isMaterial)
                        continue;

                    ResourceDemands[idx] = GameResourceDemands[idx];

                    var productionCapacity = CompanyProduction[idx] == 0
                        ? 0f
                        : math.min(4f, CompanyProduction[idx] / (CompanyDemand[idx] + 1f));
                    var empCapacity = MaxProductionWorkers[idx] == 0
                        ? 0f
                        : (float)CurrentProductionWorkers[idx] / MaxProductionWorkers[idx];
                    var taxRate = isOffice
                        ? TaxSystem.GetOfficeTaxRate(iterator.resource, TaxRates)
                        : TaxSystem.GetIndustrialTaxRate(iterator.resource, TaxRates);

                    if (!isOffice)
                        for (var ci = 0; ci < ProcessDataChunks.Length; ci++)
                        {
                            var chunk = ProcessDataChunks[ci];
                            var processes = chunk.GetNativeArray(ref ProcessDataType);
                            for (var i = 0; i < processes.Length; i++)
                            {
                                var process = processes[i];
                                if (process.m_Output.m_Resource == iterator.resource &&
                                    process.m_Input1.m_Resource != iterator.resource)
                                {
                                    if (process.m_Input1.m_Amount != 0)
                                    {
                                        var inputIdx = EconomyUtils.GetResourceIndex(process.m_Input1.m_Resource);
                                        inputUtil += math.min(4f,
                                            CompanyDemand[inputIdx] / (CompanyProduction[inputIdx] + 1f));
                                        numInputs++;
                                    }

                                    if (process.m_Input2.m_Amount != 0)
                                    {
                                        var inputIdx = EconomyUtils.GetResourceIndex(process.m_Input2.m_Resource);
                                        inputUtil += math.min(4f,
                                            CompanyDemand[inputIdx] / (CompanyProduction[inputIdx] + 1f));
                                        numInputs++;
                                    }
                                }
                            }
                        }

                    if (isOffice)
                    {
                        numOffice++;
                        prodCapOff += productionCapacity;
                        empCapOff += empCapacity;
                        taxRateOff += taxRate;
                        Results[11] += ProductionPropertyless[idx];
                    }
                    else
                    {
                        numIndustry++;
                        prodCapInd += productionCapacity;
                        empCapInd += empCapacity;
                        taxRateInd += taxRate;
                        Results[1] += ProductionPropertyless[idx];
                    }

                    if (GameBuildingDemands[idx] >= IndResDemValue)
                        IncludedResources.value |= iterator.resource;
                }

                for (var i = 0; i < 5; i++)
                {
                    var available = math.max(0, Employables[i] - FreeWorkplaces[i]);
                    if (i >= 2) Results[8] += available;
                    else Results[9] += available;
                }

                Results[2] = numIndustry > 0 ? (int)math.round(10f * taxRateInd / numIndustry) : 0;
                Results[3] = numIndustry > 0 ? (int)math.round(100f * prodCapInd / numIndustry) : 0;
                Results[4] = numIndustry > 0 ? (int)math.round(1000f * empCapInd / numIndustry) : 0;
                Results[7] = numInputs > 0 ? (int)math.round(100f * inputUtil / numInputs) : 0;
                Results[12] = numOffice > 0 ? (int)math.round(10f * taxRateOff / numOffice) : 0;
                Results[13] = numOffice > 0 ? (int)math.round(100f * prodCapOff / numOffice) : 0;
                Results[14] = numOffice > 0 ? (int)math.round(1000f * empCapOff / numOffice) : 0;
                Results[15] = storageCompanyDemand;
            }
        }

        protected override void OnUpdate()
        {
            if (!IsPanelVisible)
                return;

            m_IncludedResources.value = Resource.NoResource;
            m_Results.Fill(0);
            m_ResourceDemands.Fill(0);
            m_FreeProperties.Fill(0);
            m_StorageCapacities.Fill(0);

            JobHandle companyDeps;
            var industrialData = m_CountCompanyDataSystem.GetIndustrialCompanyDatas(out companyDeps);

            JobHandle employDeps;
            var employables = m_CountHouseholdDataSystem.GetEmployables(out employDeps);

            var demandSystem = World.GetExistingSystemManaged<IndustrialDemandSystem>();
            JobHandle resourceDemandDeps = default, buildingDemandDeps = default;
            NativeArray<int> gameResourceDemands, gameBuildingDemands;
            if (demandSystem != null)
            {
                gameResourceDemands = demandSystem.GetResourceDemands(out resourceDemandDeps);
                gameBuildingDemands = demandSystem.GetBuildingDemands(out buildingDemandDeps);
            }
            else
            {
                gameResourceDemands =
                    new NativeArray<int>(EconomyUtils.ResourceCount, World.UpdateAllocator.ToAllocator);
                gameBuildingDemands =
                    new NativeArray<int>(EconomyUtils.ResourceCount, World.UpdateAllocator.ToAllocator);
            }

            var job = default(UpdateIndustrialDisplayJob);
            job.IndustrialPropertyChunks =
                m_FreeIndustrialQuery.ToArchetypeChunkListAsync(World.UpdateAllocator.ToAllocator,
                    out var indChunksHandle);
            job.StorageCompanyChunks =
                m_StorageCompanyQuery.ToArchetypeChunkListAsync(World.UpdateAllocator.ToAllocator,
                    out var storChunksHandle);
            job.ProcessDataChunks =
                m_ProcessDataQuery.ToArchetypeChunkListAsync(World.UpdateAllocator.ToAllocator,
                    out var procChunksHandle);
            job.EntityType = SystemAPI.GetEntityTypeHandle();
            job.PrefabType = SystemAPI.GetComponentTypeHandle<PrefabRef>(true);
            job.RenterType = SystemAPI.GetBufferTypeHandle<Renter>(true);
            job.ProcessDataType = SystemAPI.GetComponentTypeHandle<IndustrialProcessData>(true);
            job.BuildingPropertyDatas = SystemAPI.GetComponentLookup<BuildingPropertyData>(true);
            job.IndustrialCompanies = SystemAPI.GetComponentLookup<IndustrialCompany>(true);
            job.Attached = SystemAPI.GetComponentLookup<Attached>(true);
            job.Prefabs = SystemAPI.GetComponentLookup<PrefabRef>(true);
            job.ProcessDatas = SystemAPI.GetComponentLookup<IndustrialProcessData>(true);
            job.PropertyRenters = SystemAPI.GetComponentLookup<PropertyRenter>(true);
            job.StorageLimitDatas = SystemAPI.GetComponentLookup<StorageLimitData>(true);
            job.SpawnableBuildingDatas = SystemAPI.GetComponentLookup<SpawnableBuildingData>(true);
            job.BuildingDatas = SystemAPI.GetComponentLookup<BuildingData>(true);
            job.ResourceDatas = SystemAPI.GetComponentLookup<ResourceData>(true);
            job.ResourcePrefabs = m_ResourceSystem.GetPrefabs();
            job.TaxRates = m_TaxSystem.GetTaxRates();
            job.Employables = employables;
            job.FreeWorkplaces = m_CountWorkplacesSystem.GetFreeWorkplaces();
            job.GameResourceDemands = gameResourceDemands;
            job.GameBuildingDemands = gameBuildingDemands;
            job.CompanyDemand = industrialData.m_Demand;
            job.CompanyProduction = industrialData.m_Production;
            job.MaxProductionWorkers = industrialData.m_MaxProductionWorkers;
            job.CurrentProductionWorkers = industrialData.m_CurrentProductionWorkers;
            job.ProductionPropertyless = industrialData.m_ProductionPropertyless;
            job.IndResDemValue = Mod.setting.indResDemValue;
            job.FreeProperties = m_FreeProperties;
            job.Results = m_Results;
            job.ResourceDemands = m_ResourceDemands;
            job.IncludedResources = m_IncludedResources;
            job.StorageCapacities = m_StorageCapacities;
            var jobData = job;

            Dependency =
                jobData.Schedule(JobUtils.CombineDependencies(Dependency, indChunksHandle, storChunksHandle,
                    procChunksHandle));

            m_CountHouseholdDataSystem.AddHouseholdDataReader(Dependency);
            m_ResourceSystem.AddPrefabsReader(Dependency);
            m_TaxSystem.AddReader(Dependency);
            m_IndustrialDemandSystem.AddReader(Dependency);

            Dependency.Complete();
        }
    }
}