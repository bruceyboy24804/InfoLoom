using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Entities;
using Colossal.Logging;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Game.UI;
using Game.Vehicles;
using InfoLoomTwo.Domain.DataDomain.Enums.CompanyPanelEnums;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using DeliveryTruck = Game.Vehicles.DeliveryTruck;
using StorageCompany = Game.Companies.StorageCompany;

namespace InfoLoomTwo.Systems.CommercialSystems.CommercialCompanyDebugData
{
    public struct ResourceInfo
    {
        
        public string ResourceName;
        public int Amount;
        public string Icon;

        public ResourceInfo(string resourceName, int amount, string icon)
        {
            ResourceName = resourceName;
            Amount = amount;
            Icon = icon;
        }
    }

    public struct CommercialCompanyDTO
    {
        public Entity EntityId;
        public string CompanyName;
        public int ServiceAvailable;
        public int MaxService;
        public int TotalEmployees;
        public int MaxWorkers;
        public int VehicleCount;
        public int VehicleCapacity;
        public int ResourceAmount;
        public int TotalEfficiency;
        public EfficiencyFactorInfo[] Factors;
        public float Profitability;
        public int LastTotalWorth;
        public int TotalWages;
        public int ProductionPerDay;
        public float EfficiencyValue;
        public float Concentration;
        public string OutputResourceName;
        public string ResourceIcon; // For backward compatibility
        public string ResourceName; // For backward compatibility
        public ResourceInfo[] Resources; // New field to hold all resources
    }

    public struct EfficiencyFactorInfo
    {
        public Game.Buildings.EfficiencyFactor Factor;
        public int Value;
        public int Result;

        public EfficiencyFactorInfo(Game.Buildings.EfficiencyFactor factor, int value, int result)
        {
            Factor = factor;
            Value = value;
            Result = result;
        }
    }

    // Burst-compatible struct for commercial company data
    public struct CommercialCompanyJobData : IComponentData
    {
        public Entity EntityId;
        public FixedString64Bytes CompanyName;
        public int ServiceAvailable;
        public int MaxService;
        public int TotalEmployees;
        public int MaxWorkers;
        public int VehicleCount;
        public int VehicleCapacity;
        public int TotalEfficiency;
        public float Profitability;
        public int LastTotalWorth;
        public int TotalWages;
        public int ProductionPerDay;
        public float EfficiencyValue;
        public int ResourceCount;
    }

    // Burst-compiled job for processing commercial companies
    [BurstCompile]
    public struct ProcessCommercialCompaniesJob : IJobChunk
    {
        // Required component type handles
        [ReadOnly] public EntityTypeHandle EntityType;
        [ReadOnly] public ComponentTypeHandle<Game.Companies.CompanyData> CompanyDataType;
        [ReadOnly] public ComponentTypeHandle<WorkProvider> WorkProviderType;
        [ReadOnly] public ComponentTypeHandle<PrefabRef> PrefabRefType;
        [ReadOnly] public ComponentTypeHandle<ServiceAvailable> ServiceAvailableType;
        
        // Optional component type handles
        [ReadOnly] public ComponentTypeHandle<PropertyRenter> PropertyRenterType;
        [ReadOnly] public ComponentTypeHandle<Profitability> ProfitabilityType;
        
        // Buffer type handles
        [ReadOnly] public BufferTypeHandle<Employee> EmployeeBufferType;
        [ReadOnly] public BufferTypeHandle<OwnedVehicle> OwnedVehicleBufferType;
        [ReadOnly] public BufferTypeHandle<Resources> ResourcesBufferType;
        
        // Component lookups for related entities
        [ReadOnly] public ComponentLookup<ServiceCompanyData> ServiceCompanyDataLookup;
        [ReadOnly] public ComponentLookup<TransportCompanyData> TransportCompanyDataLookup;
        [ReadOnly] public ComponentLookup<IndustrialProcessData> IndustrialProcessDataLookup;
        [ReadOnly] public ComponentLookup<ResourceData> ResourceDataLookup;
        [ReadOnly] public ComponentLookup<Citizen> CitizenLookup;
        [ReadOnly] public ComponentLookup<DeliveryTruck> DeliveryTruckLookup;
        [ReadOnly] public BufferLookup<Efficiency> EfficiencyLookup;
        
        // Shared data
        [ReadOnly] public EconomyParameterData EconomyParams;
        [ReadOnly] public ResourcePrefabs ResourcePrefabs;
        
        // Output data
        public NativeList<CommercialCompanyJobData>.ParallelWriter ResultWriter;
        
        // Cache lookups
        [ReadOnly] public NativeHashMap<Entity, FixedString64Bytes> CompanyNames;
        
        // Burst-compatible default name
        [ReadOnly] public FixedString64Bytes DefaultCompanyName;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            // Early exit if chunk doesn't have required components
            if (!chunk.Has (ref EmployeeBufferType))
                return;

            // Get arrays for the entire chunk
            var entities = chunk.GetNativeArray(EntityType);
            var companyDataArray = chunk.GetNativeArray(ref CompanyDataType);
            var workProviderArray = chunk.GetNativeArray(ref WorkProviderType);
            var prefabRefArray = chunk.GetNativeArray(ref PrefabRefType);
            var serviceAvailableArray = chunk.GetNativeArray(ref ServiceAvailableType);
            
            // Check for optional components at chunk level
            var hasPropertyRenter = chunk.Has(ref PropertyRenterType);
            var hasProfitability = chunk.Has(ref ProfitabilityType);
            var hasOwnedVehicles = chunk.Has(ref OwnedVehicleBufferType);
            var hasResources = chunk.Has(ref ResourcesBufferType);
            
            var propertyRenterArray = hasPropertyRenter ? chunk.GetNativeArray(ref PropertyRenterType) : default;
            var profitabilityArray = hasProfitability ? chunk.GetNativeArray(ref ProfitabilityType) : default;
            
            // Get buffer accessors
            var employeeBufferAccessor = chunk.GetBufferAccessor(ref EmployeeBufferType);
            var ownedVehicleAccessor = hasOwnedVehicles ? chunk.GetBufferAccessor(ref OwnedVehicleBufferType) : default;
            var resourcesAccessor = hasResources ? chunk.GetBufferAccessor(ref ResourcesBufferType) : default;

            // Process all entities in this chunk
            for (int i = 0; i < chunk.Count; i++)
            {
                // Skip enabled mask checking for simplicity - process all entities

                var entity = entities[i];
                var employeeBuffer = employeeBufferAccessor[i];
                
                // Skip companies with no employees
                if (employeeBuffer.Length == 0)
                    continue;

                var companyData = companyDataArray[i];
                var workProvider = workProviderArray[i];
                var prefabRef = prefabRefArray[i];
                var serviceAvailable = serviceAvailableArray[i];
                Entity prefab = prefabRef.m_Prefab;

                // Get company name from cache
                var companyName = CompanyNames.TryGetValue(companyData.m_Brand, out var name) ? 
                    name : DefaultCompanyName;

                // Calculate basic data
                int serviceValue = serviceAvailable.m_ServiceAvailable;
                int maxService = GetMaxService(prefab);
                int activeVehicles = hasOwnedVehicles ? CountActiveVehicles(ownedVehicleAccessor[i]) : 0;
                int maxDeliveryTrucks = GetMaxDeliveryTrucks(prefab);
                int resourceCount = hasResources ? resourcesAccessor[i].Length : 0;

                // Calculate profitability
                float profitabilityValue = 0f;
                int lastTotalWorth = 0;
                if (hasProfitability)
                {
                    var profitability = profitabilityArray[i];
                    profitabilityValue = ((profitability.m_Profitability - 127f) / 127.5f) * 100f;
                    lastTotalWorth = profitability.m_LastTotalWorth;
                }

                // Calculate wages
                int totalWages = CalculateTotalWage(employeeBuffer);

                // Get efficiency data
                float efficiencyValue = 1f;
                int efficiency = 100;
                
                if (hasPropertyRenter)
                {
                    Entity targetEntity = propertyRenterArray[i].m_Property;
                    if (EfficiencyLookup.HasBuffer(targetEntity))
                    {
                        var efficiencyBuffer = EfficiencyLookup[targetEntity];
                        efficiencyValue = BuildingUtils.GetEfficiency(efficiencyBuffer);
                        efficiency = (int)math.round(100f * efficiencyValue);
                    }
                }

                // Calculate production per day
                int productionPerDay = CalculateProductionPerDay(prefab, efficiencyValue, employeeBuffer);

                // Create job data
                var jobData = new CommercialCompanyJobData
                {
                    EntityId = entity,
                    CompanyName = companyName,
                    ServiceAvailable = serviceValue,
                    MaxService = maxService,
                    TotalEmployees = employeeBuffer.Length,
                    MaxWorkers = workProvider.m_MaxWorkers,
                    VehicleCount = activeVehicles,
                    VehicleCapacity = maxDeliveryTrucks,
                    TotalEfficiency = efficiency,
                    Profitability = profitabilityValue,
                    LastTotalWorth = lastTotalWorth,
                    TotalWages = totalWages,
                    ProductionPerDay = productionPerDay,
                    EfficiencyValue = efficiencyValue * 100f,
                    ResourceCount = resourceCount
                };

                ResultWriter.AddNoResize(jobData);
            }
        }

        [BurstCompile]
        private int GetMaxService(Entity prefab)
        {
            return ServiceCompanyDataLookup.TryGetComponent(prefab, out var serviceData) ? 
                serviceData.m_MaxService : 0;
        }

        [BurstCompile]
        private int GetMaxDeliveryTrucks(Entity prefab)
        {
            return TransportCompanyDataLookup.TryGetComponent(prefab, out var transportData) ? 
                transportData.m_MaxTransports : 0;
        }

        [BurstCompile]
        private int CountActiveVehicles(DynamicBuffer<OwnedVehicle> vehicleBuffer)
        {
            int count = 0;
            for (int i = 0; i < vehicleBuffer.Length; i++)
            {
                if (DeliveryTruckLookup.HasComponent(vehicleBuffer[i].m_Vehicle))
                    count++;
            }
            return count;
        }

        [BurstCompile]
        private int CalculateTotalWage(DynamicBuffer<Employee> employeeBuffer)
        {
            return EconomyUtils.CalculateTotalWage(employeeBuffer, ref EconomyParams);
        }

        [BurstCompile]
        private int CalculateProductionPerDay(Entity prefab, float efficiencyValue, DynamicBuffer<Employee> employeeBuffer)
        {
            if (!IndustrialProcessDataLookup.TryGetComponent(prefab, out var industryProcess))
                return 0;

            // Commercial companies are not industrial, so pass false for isIndustrial
            // Get the resource data for the output resource
            var resourceData = ResourceDataLookup[ResourcePrefabs[industryProcess.m_Output.m_Resource]];
            
            return EconomyUtils.GetCompanyProductionPerDay(
                efficiencyValue,
                false, // isIndustrial parameter (commercial companies)
                employeeBuffer,
                industryProcess,
                resourceData,
                ref CitizenLookup,
                ref EconomyParams);
        }
    }

    public partial class CommercialCompanyDataSystem : GameSystemBase
    {
        public IndexSortingEnum m_CurrentIndexSorting = IndexSortingEnum.Off;
        public CompanyNameEnum m_CurrentCompanyNameSorting = CompanyNameEnum.Off;
        public ServiceUsageEnum m_CurrentServiceUsageSorting = ServiceUsageEnum.Off;
        public EmployeesEnum m_CurrentEmployeesSorting = EmployeesEnum.Off;
        public EfficiancyEnum m_CurrentEfficiencySorting = EfficiancyEnum.Off;
        public ProfitabilityEnum m_CurrentProfitabilitySorting = ProfitabilityEnum.Off;
        public ResourceAmountEnum m_CurrentResourceAmountSorting = ResourceAmountEnum.Off;
        public CommercialCompanyDTO[] m_SortedCommercialCompanyDTOs;
        private NameSystem m_NameSystem;
        private ImageSystem m_ImageSystem;
        private ResourceSystem m_ResourceSystem;
        private PrefabSystem m_PrefabSystem;
        private EntityQuery m_CommercialCompanyQuery;
        private ILog m_Log;
        
        public bool IsPanelVisible;
        public CommercialCompanyDTO[] m_CommercialCompanyDTOs;

        // Cached lookups for performance
        private NativeHashMap<Entity, FixedString64Bytes> m_CompanyNameCache;
        private Dictionary<Resource, string> m_ResourceNameCache;
        private Dictionary<Resource, string> m_ResourceIconCache;
        private bool m_CacheInitialized;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = Mod.log;
            m_CommercialCompanyDTOs = Array.Empty<CommercialCompanyDTO>();
            m_NameSystem = World.GetOrCreateSystemManaged<NameSystem>();
            m_ImageSystem = World.GetOrCreateSystemManaged<ImageSystem>();
            m_ResourceSystem = World.GetOrCreateSystemManaged<ResourceSystem>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();

            // Define optimized query for commercial companies
            m_CommercialCompanyQuery = SystemAPI.QueryBuilder()
                .WithAll<CommercialCompany>()
                .WithNone<StorageCompany>()
                .Build();

            // Initialize caches
            m_CompanyNameCache = new NativeHashMap<Entity, FixedString64Bytes>(1000, Allocator.Persistent);
            m_ResourceNameCache = new Dictionary<Resource, string>();
            m_ResourceIconCache = new Dictionary<Resource, string>();
        }

        protected override void OnDestroy()
        {
            if (m_CompanyNameCache.IsCreated) 
                m_CompanyNameCache.Dispose();
            base.OnDestroy();
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 1024;
        }

        protected override void OnUpdate()
        {
            if (!IsPanelVisible)
                return;

            UpdateCommercialStatsWithBurstJob();
        }

        protected override void OnGameLoaded(Context serializationContext)
        {
            base.OnGameLoaded(serializationContext);
            InitializeCaches();
        }

        private void InitializeCaches()
        {
            if (m_CacheInitialized)
                return;

            // Initialize resource name and icon caches
            for (int i = 0; i < 64; i++)
            {
                var resource = (Resource)i;
                if (resource == Resource.NoResource) continue;
                var resourceName = GetFormattedResourceName(resource);
                var iconPath = GetResourceIconPath(resource);
                m_ResourceNameCache[resource] = resourceName;
                m_ResourceIconCache[resource] = iconPath;
                
                
            }

            m_CacheInitialized = true;
        }

        private void UpdateCommercialStatsWithBurstJob()
        {
            if (!IsPanelVisible)
                return;

            InitializeCaches();
            UpdateCompanyNameCache();

            // Create result list with estimated capacity
            int estimatedCount = m_CommercialCompanyQuery.CalculateEntityCount();
            var jobResults = new NativeList<CommercialCompanyJobData>(estimatedCount, Allocator.TempJob);

            // Create the burst-compiled job
            var job = new ProcessCommercialCompaniesJob
            {
                // Required component type handles
                EntityType = GetEntityTypeHandle(),
                CompanyDataType = GetComponentTypeHandle<Game.Companies.CompanyData>(true),
                WorkProviderType = GetComponentTypeHandle<WorkProvider>(true),
                PrefabRefType = GetComponentTypeHandle<PrefabRef>(true),
                ServiceAvailableType = GetComponentTypeHandle<ServiceAvailable>(true),
                
                // Optional component type handles
                PropertyRenterType = GetComponentTypeHandle<PropertyRenter>(true),
                ProfitabilityType = GetComponentTypeHandle<Profitability>(true),
                
                // Buffer type handles
                EmployeeBufferType = GetBufferTypeHandle<Employee>(true),
                OwnedVehicleBufferType = GetBufferTypeHandle<OwnedVehicle>(true),
                ResourcesBufferType = GetBufferTypeHandle<Resources>(true),
                
                // Component lookups
                ServiceCompanyDataLookup = GetComponentLookup<ServiceCompanyData>(true),
                TransportCompanyDataLookup = GetComponentLookup<TransportCompanyData>(true),
                IndustrialProcessDataLookup = GetComponentLookup<IndustrialProcessData>(true),
                ResourceDataLookup = GetComponentLookup<ResourceData>(true),
                CitizenLookup = GetComponentLookup<Citizen>(true),
                DeliveryTruckLookup = GetComponentLookup<DeliveryTruck>(true),
                EfficiencyLookup = GetBufferLookup<Efficiency>(true),
                
                // Shared data
                EconomyParams = SystemAPI.GetSingleton<EconomyParameterData>(),
                ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
                
                // Cache and output
                CompanyNames = m_CompanyNameCache,
                ResultWriter = jobResults.AsParallelWriter()
            };

            // Schedule the job to run in parallel across multiple chunks
            var jobHandle = job.ScheduleParallel(m_CommercialCompanyQuery, Dependency);
            
            // Complete the job and convert results
            jobHandle.Complete();
            
            // Convert job results to final DTOs
            ConvertJobResultsToDTO(jobResults);
            
            jobResults.Dispose();
        }

        private void UpdateCompanyNameCache()
        {
            var entities = m_CommercialCompanyQuery.ToEntityArray(Allocator.Temp);
            var companyDataLookup = GetComponentLookup<Game.Companies.CompanyData>(true);

            try
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    if (companyDataLookup.TryGetComponent(entity, out var companyData))
                    {
                        if (!m_CompanyNameCache.ContainsKey(companyData.m_Brand))
                        {
                            var companyName = m_NameSystem.GetRenderedLabelName(companyData.m_Brand);
                            var fixedName = new FixedString64Bytes(companyName);
                            m_CompanyNameCache.TryAdd(companyData.m_Brand, fixedName);
                        }
                    }
                }
            }
            finally
            {
                entities.Dispose();
            }
        }

        private void ConvertJobResultsToDTO(NativeList<CommercialCompanyJobData> jobResults)
        {
            var companies = new List<CommercialCompanyDTO>(jobResults.Length);
            var resourcesBufferLookup = GetBufferLookup<Resources>(true);
            var efficiencyBufferLookup = GetBufferLookup<Efficiency>(true);
            var propertyRenterLookup = GetComponentLookup<PropertyRenter>(true);

            for (int i = 0; i < jobResults.Length; i++)
            {
                var jobData = jobResults[i];
                
                // Get detailed resource information
                var resources = new List<ResourceInfo>();
                if (resourcesBufferLookup.HasBuffer(jobData.EntityId))
                {
                    var resourceBuffer = resourcesBufferLookup[jobData.EntityId];
                    for (int r = 0; r < resourceBuffer.Length; r++)
                    {
                        var resource = resourceBuffer[r];
                        string resourceName = GetFormattedResourceName(resource.m_Resource);
                        string resourceIcon = GetResourceIconPath(resource.m_Resource);
                        
                        resources.Add(new ResourceInfo(
                            resourceName,
                            resource.m_Amount,
                            resourceIcon
                        ));
                    }
                }

                // Get efficiency factors
                EfficiencyFactorInfo[] factors = Array.Empty<EfficiencyFactorInfo>();
                if (propertyRenterLookup.HasComponent(jobData.EntityId))
                {
                    var targetEntity = propertyRenterLookup[jobData.EntityId].m_Property;
                    factors = GetEfficiencyFactors(targetEntity, efficiencyBufferLookup);
                }

                // Create final DTO
                var companyDTO = new CommercialCompanyDTO
                {
                    EntityId = jobData.EntityId,
                    CompanyName = jobData.CompanyName.ToString(),
                    ServiceAvailable = jobData.ServiceAvailable,
                    MaxService = jobData.MaxService,
                    TotalEmployees = jobData.TotalEmployees,
                    MaxWorkers = jobData.MaxWorkers,
                    VehicleCount = jobData.VehicleCount,
                    VehicleCapacity = jobData.VehicleCapacity,
                    ResourceAmount = 0, // Legacy field
                    TotalEfficiency = jobData.TotalEfficiency,
                    Factors = factors,
                    Profitability = jobData.Profitability,
                    LastTotalWorth = jobData.LastTotalWorth,
                    TotalWages = jobData.TotalWages,
                    ProductionPerDay = jobData.ProductionPerDay,
                    EfficiencyValue = jobData.EfficiencyValue,
                    Concentration = 0f, // Calculate if needed
                    OutputResourceName = "", // Calculate if needed
                    ResourceIcon = "",
                    ResourceName = "None", // Legacy field
                    Resources = resources.ToArray()
                };
                companies.Add(companyDTO);
            }
            m_CommercialCompanyDTOs = companies.ToArray();
        }
        


        // Helper methods
        private string GetResourceIconPath(Resource resource)
        {
            if (resource == Resource.Money)
            {
                return "Media/Game/Icons/Money.svg";
            }
            
            Entity resourcePrefab = m_ResourceSystem.GetPrefab(resource);
            string icon = m_ImageSystem.GetIconOrGroupIcon(resourcePrefab);
            return icon;
        }

        private string GetFormattedResourceName(Resource resource)
        {
            if (resource == Resource.NoResource)
                return string.Empty;
                
            string resourceName = EconomyUtils.GetName(resource);
            if (string.IsNullOrEmpty(resourceName))
                return string.Empty;
                
            return char.ToUpper(resourceName[0]) + resourceName.Substring(1);
        }
        
        private EfficiencyFactorInfo[] GetEfficiencyFactors(Entity entity, BufferLookup<Efficiency> efficiencyLookup)
        {
            if (!efficiencyLookup.HasBuffer(entity))
                return Array.Empty<EfficiencyFactorInfo>();
                
            var buffer = efficiencyLookup[entity];
            if (buffer.Length == 0)
                return Array.Empty<EfficiencyFactorInfo>();
                
            using var sortedEfficiencies = buffer.ToNativeArray(Allocator.Temp);
            sortedEfficiencies.Sort();
            
            var tempFactors = new List<EfficiencyFactorInfo>();
            var totalEfficiency = (int)math.round(100f * BuildingUtils.GetEfficiency(buffer));
            
            if (totalEfficiency > 0)
            {
                float cumulativeEffect = 100f;
                for (int i = 0; i < sortedEfficiencies.Length; i++)
                {
                    var item = sortedEfficiencies[i];
                    float efficiency = math.max(0f, item.m_Efficiency);
                    cumulativeEffect *= efficiency;
                    
                    int percentageChange = math.max(-99, (int)math.round(100f * efficiency) - 100);
                    int result = math.max(1, (int)math.round(cumulativeEffect));
                    
                    if (percentageChange != 0)
                    {
                        tempFactors.Add(new EfficiencyFactorInfo(item.m_Factor, percentageChange, result));
                    }
                }
            }
            else
            {
                for (int i = 0; i < sortedEfficiencies.Length; i++)
                {
                    var item = sortedEfficiencies[i];
                    if (math.max(0f, item.m_Efficiency) == 0f)
                    {
                        tempFactors.Add(new EfficiencyFactorInfo(item.m_Factor, -100, -100));
                        if ((int)item.m_Factor <= 3)
                        {
                            break;
                        }
                    }
                }
            }
            
            return tempFactors.ToArray();
        }
        public int CompareByIndex(CommercialCompanyDTO x, CommercialCompanyDTO y)
{
    switch (m_CurrentIndexSorting)
    {
        case IndexSortingEnum.Ascending:
            return x.EntityId.Index.CompareTo(y.EntityId.Index);
        case IndexSortingEnum.Descending:
            return y.EntityId.Index.CompareTo(x.EntityId.Index);
        default:
            return 0;
    }
}

public int CompareByName(CommercialCompanyDTO x, CommercialCompanyDTO y)
{
    switch (m_CurrentCompanyNameSorting)
    {
        case CompanyNameEnum.Ascending:
            return string.Compare(x.CompanyName, y.CompanyName, StringComparison.OrdinalIgnoreCase);
        case CompanyNameEnum.Descending:
            return string.Compare(y.CompanyName, x.CompanyName, StringComparison.OrdinalIgnoreCase);
        default:
            return 0;
    }
}

public int CompareByServiceUsage(CommercialCompanyDTO x, CommercialCompanyDTO y)
{
    switch (m_CurrentServiceUsageSorting)
    {
        case ServiceUsageEnum.Ascending:
            return x.ServiceAvailable.CompareTo(y.ServiceAvailable);
        case ServiceUsageEnum.Descending:
            return y.ServiceAvailable.CompareTo(x.ServiceAvailable);
        default:
            return 0;
    }
}

public int CompareByEmployees(CommercialCompanyDTO x, CommercialCompanyDTO y)
{
    switch (m_CurrentEmployeesSorting)
    {
        case EmployeesEnum.Ascending:
            return x.TotalEmployees.CompareTo(y.TotalEmployees);
        case EmployeesEnum.Descending:
            return y.TotalEmployees.CompareTo(x.TotalEmployees);
        default:
            return 0;
    }
}

public int CompareByEfficiency(CommercialCompanyDTO x, CommercialCompanyDTO y)
{
    switch (m_CurrentEfficiencySorting)
    {
        case EfficiancyEnum.Ascending:
            return x.TotalEfficiency.CompareTo(y.TotalEfficiency);
        case EfficiancyEnum.Descending:
            return y.TotalEfficiency.CompareTo(x.TotalEfficiency);
        default:
            return 0;
    }
}

        public int CompareByProfitability(CommercialCompanyDTO x, CommercialCompanyDTO y)
        {
            switch (m_CurrentProfitabilitySorting)
            {
                case ProfitabilityEnum.Ascending:
                    return x.Profitability.CompareTo(y.Profitability);
                case ProfitabilityEnum.Descending:
                    return y.Profitability.CompareTo(x.Profitability);
                default:
                    return 0;
            }
        }
        public int CompareByResourceAmount(CommercialCompanyDTO x, CommercialCompanyDTO y)
        {
            switch (m_CurrentResourceAmountSorting)
            {
                case ResourceAmountEnum.Ascending:
                    return x.ResourceAmount.CompareTo(y.ResourceAmount);
                case ResourceAmountEnum.Descending:
                    return y.ResourceAmount.CompareTo(x.ResourceAmount);
                default:
                    return 0;
            }
        }
    }
    
}
