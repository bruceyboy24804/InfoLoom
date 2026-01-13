using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Entities;
using Colossal.Logging;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Game.UI;
using Game.Vehicles;
using InfoLoomTwo.Domain.DataDomain.Enums.CompanyPanelEnums;
using InfoLoomTwo.Systems.IndustrialSystems.IndustrialCompanyData;
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
    public struct ProcessResourceInfo : IJsonWritable
    {
        public string ResourceName;
        public int Amount;
        public string ResourceIcon;
        public bool IsOutput;

        public void Write(IJsonWriter writer)
        {
            writer.TypeBegin(typeof(ProcessResourceInfo).FullName);
            writer.PropertyName("resourceName"); writer.Write(ResourceName);
            writer.PropertyName("amount"); writer.Write(Amount);
            writer.PropertyName("resourceIcon"); writer.Write(ResourceIcon);
            writer.PropertyName("isOutput"); writer.Write(IsOutput);
            writer.TypeEnd();
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
        
        public int MoneyAmount;
        public ResourceInfo[] Input1Resources;
        public ResourceInfo[] OutputResources;
        public ResourceInfo[] MaintenanceResources;
        public ProcessResourceInfo[] ProcessResources;

       public int Income;
       public int Worth;
       public int Profit;
       public int WagePaid;
       public int RentPaid;
       public int ElectricityPaid;
       public int WaterPaid;
       public int SewagePaid;
       public int GarbagePaid;
       public int TaxPaid;
       public int ResourcesBoughtPaid;
       public int CurrentCustomers;
       public int MonthlyCustomers;
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
        public Entity Brand;        
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
        public bool HasStatistics;
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
        [ReadOnly] public ComponentLookup<CompanyStatisticData> CompanyStatisticDataLookup;
        
        // Output data
        public NativeList<CommercialCompanyJobData>.ParallelWriter ResultWriter;
        
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
                    Brand = companyData.m_Brand,
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
                    ResourceCount = resourceCount,
                    
                };
                jobData.HasStatistics = CompanyStatisticDataLookup.HasComponent(entity);
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

            // Get the resource data for the output resource
            var resourceData = ResourceDataLookup[ResourcePrefabs[industryProcess.m_Output.m_Resource]];

            // Get service availability data for commercial companies
            ServiceAvailable serviceAvailable = default;
            ServiceCompanyData serviceCompanyData = default;
            
            if (ServiceCompanyDataLookup.HasComponent(prefab))
            {
                serviceCompanyData = ServiceCompanyDataLookup[prefab];
            }

            return EconomyUtils.GetCompanyProductionPerDay(
                efficiencyValue,
                false, // isIndustrial parameter (commercial companies)
                employeeBuffer,
                industryProcess,
                resourceData,
                ref CitizenLookup,
                ref EconomyParams,
                serviceAvailable,
                serviceCompanyData
            );
        }
        

    }

    /// <summary>
    /// System for collecting and processing commercial company data with lazy conversion.
    /// 
    /// Performance optimizations:
    /// 1. Burst-compiled job collects lightweight data from entities
    /// 2. Results cached in NativeArray for reuse
    /// 3. Sorting performed on lightweight data before expensive DTO conversion
    /// 4. DTOs converted on-demand and cached per entity
    /// 5. Resource names/icons pre-cached to avoid repeated lookups
    /// 6. Company name cache updated only when entity count changes
    /// 
    /// For best performance with large datasets:
    /// - Use GetCompanyRange(start, count) for pagination/virtualization
    /// - Call RefreshDTOArray() only when sort criteria changes
    /// </summary>
    public partial class CommercialCompanyDataSystem : GameSystemBase
    {
        public SortingEnum m_CurrentIndexSorting = SortingEnum.Off;
        public SortingEnum m_CurrentCompanyNameSorting = SortingEnum.Off;
        public SortingEnum m_CurrentServiceUsageSorting = SortingEnum.Off;
        public SortingEnum m_CurrentEmployeesSorting = SortingEnum.Off;
        public SortingEnum m_CurrentEfficiencySorting = SortingEnum.Off;
        public SortingEnum m_CurrentProfitabilitySorting = SortingEnum.Off;
        public SortingEnum m_CurrentResourceAmountSorting = SortingEnum.Off;
        public SortingEnum m_CurrentMoneySorting = SortingEnum.Off;
        public SortingEnum m_CurrentInput1Sorting = SortingEnum.Off;
        public SortingEnum m_CurrentInput2Sorting = SortingEnum.Off;
        public SortingEnum m_CurrentOutputSorting = SortingEnum.Off;
        public SortingEnum m_CurrentMaintenanceSorting = SortingEnum.Off;
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
        public Dictionary<Entity, string> m_CompanyNameCache;
        private Dictionary<Resource, string> m_ResourceNameCache;
        private Dictionary<Resource, string> m_ResourceIconCache;
        private bool m_CacheInitialized;
        private int m_LastCompanyCount;
        private bool m_ForceCompanyNameCacheUpdate;
        
        // Lazy conversion cache
        private NativeArray<CommercialCompanyJobData> m_CachedJobData;
        private Dictionary<Entity, CommercialCompanyDTO> m_ConvertedDTOCache;
        private bool m_JobDataValid;

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
                .WithAll<CommercialCompany, PropertyRenter>()
                .WithNone<StorageCompany, MovingAway>()
                .Build();

            // Initialize caches
            m_CompanyNameCache = new Dictionary<Entity, string>();
            m_ResourceNameCache = new Dictionary<Resource, string>();
            m_ResourceIconCache = new Dictionary<Resource, string>();
            m_LastCompanyCount = 0;
            m_ForceCompanyNameCacheUpdate = false;
            m_ConvertedDTOCache = new Dictionary<Entity, CommercialCompanyDTO>();
            m_JobDataValid = false;
        }

        protected override void OnDestroy()
        {
            if (m_CachedJobData.IsCreated)
                m_CachedJobData.Dispose();
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
            
            // Auto-populate the DTO array after job completes for backward compatibility
            // This can be skipped if your UI uses GetCompanyRange() for virtualization
            if (m_JobDataValid)
            {
                GetAllCompanies();
            }
        }

        protected override void OnGameLoaded(Context serializationContext)
        {
            base.OnGameLoaded(serializationContext);
            InitializeCaches();
            m_CompanyNameCache.Clear();
            m_ForceCompanyNameCacheUpdate = true;
            m_LastCompanyCount = 0;
            m_JobDataValid = false;
            m_ConvertedDTOCache.Clear();
        }

        private void InitializeCaches()
        {
            if (m_CacheInitialized)
                return;

            // Pre-populate all resource names and icons to avoid repeated lookups
            for (int i = 0; i < 64; i++)
            {
                var resource = (Resource)i;
                if (resource == Resource.NoResource) continue;
                
                if (!m_ResourceNameCache.ContainsKey(resource))
                {
                    var resourceName = EconomyUtils.GetName(resource).ToString();
                    m_ResourceNameCache[resource] = resourceName;
                }
                
                if (!m_ResourceIconCache.ContainsKey(resource))
                {
                    string icon;
                    if (resource == Resource.Money)
                    {
                        icon = "Media/Game/Icons/Money.svg";
                    }
                    else
                    {
                        Entity resourcePrefab = m_ResourceSystem.GetPrefab(resource);
                        icon = m_ImageSystem.GetIconOrGroupIcon(resourcePrefab);
                    }
                    m_ResourceIconCache[resource] = icon;
                }
            }

            m_CacheInitialized = true;
        }

        public void UpdateCommercialStatsWithBurstJob()
        {
            if (!IsPanelVisible)
                return;

            InitializeCaches();
            UpdateCompanyNameCacheIfNeeded();

            // Create result list with estimated capacity
            int estimatedCount = m_CommercialCompanyQuery.CalculateEntityCount();
            var jobResults = new NativeList<CommercialCompanyJobData>(estimatedCount, Allocator.TempJob);

            // Create the burst-compiled job
            var job = new ProcessCommercialCompaniesJob
            {
                // Required component type handles
                EntityType = SystemAPI.GetEntityTypeHandle(),
                CompanyDataType = SystemAPI.GetComponentTypeHandle<Game.Companies.CompanyData>(true),
                WorkProviderType = SystemAPI.GetComponentTypeHandle<WorkProvider>(true),
                PrefabRefType = SystemAPI.GetComponentTypeHandle<PrefabRef>(true),
                ServiceAvailableType = SystemAPI.GetComponentTypeHandle<ServiceAvailable>(true),
                
                // Optional component type handles
                PropertyRenterType = SystemAPI.GetComponentTypeHandle<PropertyRenter>(true),
                ProfitabilityType = SystemAPI.GetComponentTypeHandle<Profitability>(true),
                
                // Buffer type handles
                EmployeeBufferType = SystemAPI.GetBufferTypeHandle<Employee>(true),
                OwnedVehicleBufferType = SystemAPI.GetBufferTypeHandle<OwnedVehicle>(true),
                ResourcesBufferType = SystemAPI.GetBufferTypeHandle<Resources>(true),
                CompanyStatisticDataLookup = GetComponentLookup<CompanyStatisticData>(true),
                
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
               
                ResultWriter = jobResults.AsParallelWriter()
            };

            // Schedule the job to run in parallel across multiple chunks
            var jobHandle = job.ScheduleParallel(m_CommercialCompanyQuery, Dependency);
            
            // Complete the job
            jobHandle.Complete();
            
            // Cache the job results instead of immediate conversion
            CacheJobResults(jobResults);
            
            // Clear converted DTO cache since we have new data
            m_ConvertedDTOCache.Clear();
            m_JobDataValid = true;
            
            jobResults.Dispose();
        }

        private void CacheJobResults(NativeList<CommercialCompanyJobData> jobResults)
        {
            // Dispose old cached data if exists
            if (m_CachedJobData.IsCreated)
                m_CachedJobData.Dispose();
            
            // Store new job data in persistent array
            m_CachedJobData = new NativeArray<CommercialCompanyJobData>(jobResults.Length, Allocator.Persistent);
            jobResults.AsArray().CopyTo(m_CachedJobData);
        }

        private void UpdateCompanyNameCacheIfNeeded()
        {
            int currentCount = m_CommercialCompanyQuery.CalculateEntityCount();
            
            // Only update if company count changed or forced
            if (!m_ForceCompanyNameCacheUpdate && currentCount == m_LastCompanyCount)
                return;
            
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
                            m_CompanyNameCache[companyData.m_Brand] = companyName;
                        }
                    }
                }
                
                m_LastCompanyCount = currentCount;
                m_ForceCompanyNameCacheUpdate = false;
            }
            finally
            {
                entities.Dispose();
            }
        }

        // Get all companies with lazy conversion and sorting
        public CommercialCompanyDTO[] GetAllCompanies()
        {
            if (!m_JobDataValid || !m_CachedJobData.IsCreated)
                return m_CommercialCompanyDTOs;
            
            var companies = new List<CommercialCompanyDTO>(m_CachedJobData.Length);
            for (int i = 0; i < m_CachedJobData.Length; i++)
            {
                var jobData = m_CachedJobData[i];
                companies.Add(ConvertJobDataToDTO(jobData));
            }
            
            ApplySorts(companies);
            m_CommercialCompanyDTOs = companies.ToArray();
            return m_CommercialCompanyDTOs;
        }
        
        // Apply sorts that require full DTO data (resources)
         public void ApplySorts(List<CommercialCompanyDTO> companies)
        {
            IOrderedEnumerable<CommercialCompanyDTO> ordered = null;

            if (m_CurrentCompanyNameSorting == SortingEnum.Ascending)
                ordered = companies.OrderBy(x => x.CompanyName);
            else if (m_CurrentCompanyNameSorting == SortingEnum.Descending)
                ordered = companies.OrderByDescending(x => x.CompanyName);

            if (m_CurrentEmployeesSorting == SortingEnum.Ascending)
                ordered = ordered == null ? companies.OrderBy(x => x.TotalEmployees) : ordered.ThenBy(x => x.TotalEmployees);
            else if (m_CurrentEmployeesSorting == SortingEnum.Descending)
                ordered = ordered == null ? companies.OrderByDescending(x => x.TotalEmployees) : ordered.ThenByDescending(x => x.TotalEmployees);

            if (m_CurrentMoneySorting == SortingEnum.Ascending)
                ordered = ordered == null ? companies.OrderBy(x => x.MoneyAmount) : ordered.ThenBy(x => x.MoneyAmount);
            else if (m_CurrentMoneySorting == SortingEnum.Descending)
                ordered = ordered == null ? companies.OrderByDescending(x => x.MoneyAmount) : ordered.ThenByDescending(x => x.MoneyAmount);

            if (m_CurrentInput1Sorting == SortingEnum.Ascending)
                ordered = ordered == null ? companies.OrderBy(x => SumResourceAmounts(x.Input1Resources)) : ordered.ThenBy(x => SumResourceAmounts(x.Input1Resources));
            else if (m_CurrentInput1Sorting == SortingEnum.Descending)
                ordered = ordered == null ? companies.OrderByDescending(x => SumResourceAmounts(x.Input1Resources)) : ordered.ThenByDescending(x => SumResourceAmounts(x.Input1Resources));
            
            if (m_CurrentOutputSorting == SortingEnum.Ascending)
                ordered = ordered == null ? companies.OrderBy(x => SumResourceAmounts(x.OutputResources)) : ordered.ThenBy(x => SumResourceAmounts(x.OutputResources));
            else if (m_CurrentOutputSorting == SortingEnum.Descending)
                ordered = ordered == null ? companies.OrderByDescending(x => SumResourceAmounts(x.OutputResources)) : ordered.ThenByDescending(x => SumResourceAmounts(x.OutputResources));

            if (m_CurrentMaintenanceSorting == SortingEnum.Ascending)
                ordered = ordered == null ? companies.OrderBy(x => SumResourceAmounts(x.MaintenanceResources)) : ordered.ThenBy(x => SumResourceAmounts(x.MaintenanceResources));
            else if (m_CurrentMaintenanceSorting == SortingEnum.Descending)
                ordered = ordered == null ? companies.OrderByDescending(x => SumResourceAmounts(x.MaintenanceResources)) : ordered.ThenByDescending(x => SumResourceAmounts(x.MaintenanceResources));

            if (m_CurrentEfficiencySorting == SortingEnum.Ascending)
                ordered = ordered == null ? companies.OrderBy(x => x.EfficiencyValue) : ordered.ThenBy(x => x.EfficiencyValue);
            else if (m_CurrentEfficiencySorting == SortingEnum.Descending)
                ordered = ordered == null ? companies.OrderByDescending(x => x.EfficiencyValue) : ordered.ThenByDescending(x => x.EfficiencyValue);

            if (m_CurrentProfitabilitySorting == SortingEnum.Ascending)
                ordered = ordered == null ? companies.OrderBy(x => x.Profitability) : ordered.ThenBy(x => x.Profitability);
            else if (m_CurrentProfitabilitySorting == SortingEnum.Descending)
                ordered = ordered == null ? companies.OrderByDescending(x => x.Profitability) : ordered.ThenByDescending(x => x.Profitability);

            if (m_CurrentIndexSorting == SortingEnum.Ascending)
                ordered = ordered == null ? companies.OrderBy(x => x.EntityId.Index) : ordered.ThenBy(x => x.EntityId.Index);
            else if (m_CurrentIndexSorting == SortingEnum.Descending)
                ordered = ordered == null ? companies.OrderByDescending(x => x.EntityId.Index) : ordered.ThenByDescending(x => x.EntityId.Index);

            if (m_CurrentResourceAmountSorting == SortingEnum.Ascending)
                ordered = ordered == null ? companies.OrderBy(x => x.ResourceAmount) : ordered.ThenBy(x => x.ResourceAmount);
            else if (m_CurrentResourceAmountSorting == SortingEnum.Descending)
                ordered = ordered == null ? companies.OrderByDescending(x => x.ResourceAmount) : ordered.ThenByDescending(x => x.ResourceAmount);

            if (ordered != null)
            {
                var sorted = ordered.ToList();
                companies.Clear();
                companies.AddRange(sorted);
            }
        }
        
        
        
        
        
        private CommercialCompanyDTO ConvertJobDataToDTO(CommercialCompanyJobData jobData)
        {
            // Check cache first
            if (m_ConvertedDTOCache.TryGetValue(jobData.EntityId, out var cached))
                return cached;
            
            // Do the expensive conversion
            var resourcesBufferLookup = GetBufferLookup<Resources>(true);
            var efficiencyBufferLookup = GetBufferLookup<Efficiency>(true);
            var propertyRenterLookup = GetComponentLookup<PropertyRenter>(true);
            var prefabRefLookup = GetComponentLookup<PrefabRef>(true);
            var commercialProcessLookup = GetComponentLookup<IndustrialProcessData>(true);
            
            Resource processInput1 = Resource.NoResource;
            Resource processInput2 = Resource.NoResource;
            Resource processOutput = Resource.NoResource;
            
            if (prefabRefLookup.TryGetComponent(jobData.EntityId, out var prefabRef) &&
                commercialProcessLookup.TryGetComponent(prefabRef.m_Prefab, out var proc))
            {
                processInput1 = proc.m_Input1.m_Resource;
                processInput2 = proc.m_Input2.m_Resource;
                processOutput = proc.m_Output.m_Resource;
            }
            
            ClassifyResources(jobData.EntityId, resourcesBufferLookup,
                processInput1, processInput2, processOutput,
                out var input1Resources, out var input2Resources, out var outputResources, out var maintenanceResources, out int moneyAmount);
            var (processList, outputResourceName, outputResourceIcon) = GetProcessInfo(jobData.EntityId, prefabRefLookup, commercialProcessLookup);
            
            // Get efficiency factors
            EfficiencyFactorInfo[] factors = Array.Empty<EfficiencyFactorInfo>();
            if (propertyRenterLookup.HasComponent(jobData.EntityId))
            {
                var targetEntity = propertyRenterLookup[jobData.EntityId].m_Property;
                factors = GetEfficiencyFactors(targetEntity, efficiencyBufferLookup);
            }
            
            string companyNameString;
            if (m_CompanyNameCache.TryGetValue(jobData.Brand, out var fixedName))
                companyNameString = fixedName.ToString();
            else
                companyNameString = "Unknown Company";
            
            int income = 0;
            int worth = 0;
            int profit = 0;
            int wagePaid = 0;
            int rentPaid = 0;
            int electricityPaid = 0;
            int waterPaid = 0;
            int sewagePaid = 0;
            int garbagePaid = 0;
            int taxPaid = 0;
            int resourcesBoughtPaid = 0;
            int currentCustomers = 0;
            int monthlyCustomers = 0;
       
            if(jobData.HasStatistics)
            {
                var statisticsLookup = SystemAPI.GetComponentLookup<CompanyStatisticData>(true);
                if(statisticsLookup.TryGetComponent(jobData.EntityId, out var statistics))
                {
                    income = statistics.m_Income;
                    worth = statistics.m_Worth;
                    profit = statistics.m_Profit;
                    wagePaid = statistics.m_WagePaid;
                    rentPaid = statistics.m_RentPaid;
                    electricityPaid = statistics.m_ElectricityPaid;
                    waterPaid = statistics.m_WaterPaid;
                    sewagePaid = statistics.m_SewagePaid;
                    garbagePaid = statistics.m_GarbagePaid;
                    taxPaid = statistics.m_TaxPaid;
                    resourcesBoughtPaid = statistics.m_CostBuyResource;
                    currentCustomers = statistics.m_CurrentNumberOfCustomers;
                    monthlyCustomers = statistics.m_MonthlyCustomerCount;
                } 
            }
            
            var companyDTO = new CommercialCompanyDTO
            {
                EntityId = jobData.EntityId,
                CompanyName = companyNameString,
                ServiceAvailable = jobData.ServiceAvailable,
                MaxService = jobData.MaxService,
                TotalEmployees = jobData.TotalEmployees,
                MaxWorkers = jobData.MaxWorkers,
                VehicleCount = jobData.VehicleCount,
                VehicleCapacity = jobData.VehicleCapacity,
                ResourceAmount = 0,
                TotalEfficiency = jobData.TotalEfficiency,
                Factors = factors,
                Profitability = jobData.Profitability,
                LastTotalWorth = jobData.LastTotalWorth,
                TotalWages = jobData.TotalWages,
                ProductionPerDay = jobData.ProductionPerDay,
                EfficiencyValue = jobData.EfficiencyValue,
                Concentration = 0f,
                OutputResourceName = outputResourceName,
                ResourceIcon = outputResourceIcon,
                ResourceName = "None",
                MoneyAmount = moneyAmount,
                Input1Resources = input1Resources,
                OutputResources = outputResources,
                MaintenanceResources = maintenanceResources,
                ProcessResources = processList,
                Income = income,
                Worth = worth,
                Profit = profit,
                WagePaid = wagePaid,
                RentPaid = rentPaid,
                ElectricityPaid = electricityPaid,
                WaterPaid = waterPaid,
                SewagePaid = sewagePaid,
                GarbagePaid = garbagePaid,
                TaxPaid = taxPaid,
                ResourcesBoughtPaid = resourcesBoughtPaid,
                CurrentCustomers = currentCustomers,
                MonthlyCustomers = monthlyCustomers,
            };
            
            // Cache the result
            m_ConvertedDTOCache[jobData.EntityId] = companyDTO;
            return companyDTO;
        }
        
        // Get sorted indices based on current sorting settings
        
        private void ClassifyResources(Entity entity, BufferLookup<Resources> resourcesBufferLookup,
            Resource input1, Resource input2, Resource output,
            out ResourceInfo[] input1List, out ResourceInfo[] input2List, out ResourceInfo[] outputList, out ResourceInfo[] maintenanceList, out int money)
        {
            money = 0;

            if (!resourcesBufferLookup.HasBuffer(entity))
            {
                input1List = Array.Empty<ResourceInfo>();
                input2List = Array.Empty<ResourceInfo>();
                outputList = Array.Empty<ResourceInfo>();
                maintenanceList = Array.Empty<ResourceInfo>();
                return;
            }

            var buffer = resourcesBufferLookup[entity];
            int bufferLength = buffer.Length;
            
            // Pre-allocate arrays with max possible size to avoid List allocations
            // Most companies have few resources, so allocate small fixed arrays on stack-like behavior
            var input1Array = new ResourceInfo[bufferLength];
            var input2Array = new ResourceInfo[bufferLength];
            var outputArray = new ResourceInfo[bufferLength];
            var maintenanceArray = new ResourceInfo[bufferLength];
            
            int in1Count = 0;
            int in2Count = 0;
            int outCount = 0;
            int maintCount = 0;

            for (int r = 0; r < bufferLength; r++)
            {
                var resource = buffer[r];
                if (resource.m_Resource == Resource.Money)
                {
                    money = resource.m_Amount;
                    continue;
                }
                
                // Use cached values with fallback if not pre-populated
                if (!m_ResourceNameCache.TryGetValue(resource.m_Resource, out string name))
                {
                    name = EconomyUtils.GetName(resource.m_Resource).ToString();
                    m_ResourceNameCache[resource.m_Resource] = name;
                }
                
                if (!m_ResourceIconCache.TryGetValue(resource.m_Resource, out string icon))
                {
                    if (resource.m_Resource == Resource.Money)
                    {
                        icon = "Media/Game/Icons/Money.svg";
                    }
                    else
                    {
                        Entity resourcePrefab = m_ResourceSystem.GetPrefab(resource.m_Resource);
                        icon = m_ImageSystem.GetIconOrGroupIcon(resourcePrefab);
                    }
                    m_ResourceIconCache[resource.m_Resource] = icon;
                }
                
                var info = new ResourceInfo(name, resource.m_Amount, icon);

                bool added = false;
                if (resource.m_Resource == input1 && input1 != Resource.NoResource) 
                { 
                    input1Array[in1Count++] = info; 
                    added = true; 
                }
                if (resource.m_Resource == input2 && input2 != Resource.NoResource) 
                { 
                    input2Array[in2Count++] = info; 
                    added = true; 
                }
                if (resource.m_Resource == output && output != Resource.NoResource) 
                { 
                    outputArray[outCount++] = info; 
                    added = true; 
                }
                if (!added) 
                {
                    maintenanceArray[maintCount++] = info;
                }
            }

            // Trim arrays to actual size
            input1List = in1Count == 0 ? Array.Empty<ResourceInfo>() : TrimArray(input1Array, in1Count);
            input2List = in2Count == 0 ? Array.Empty<ResourceInfo>() : TrimArray(input2Array, in2Count);
            outputList = outCount == 0 ? Array.Empty<ResourceInfo>() : TrimArray(outputArray, outCount);
            maintenanceList = maintCount == 0 ? Array.Empty<ResourceInfo>() : TrimArray(maintenanceArray, maintCount);
        }

        private ResourceInfo[] TrimArray(ResourceInfo[] source, int length)
        {
            var result = new ResourceInfo[length];
            Array.Copy(source, result, length);
            return result;
        }
        private (ProcessResourceInfo[] list, string outputName, string outputIcon) GetProcessInfo(Entity entity,
            ComponentLookup<PrefabRef> prefabRefLookup, ComponentLookup<IndustrialProcessData> industrialProcessLookup)
        {
            var list = new List<ProcessResourceInfo>();
            string outName = "";
            string outIcon = "";

            if (prefabRefLookup.TryGetComponent(entity, out var prefabRef) &&
                industrialProcessLookup.TryGetComponent(prefabRef.m_Prefab, out var processData))
            {
                if (processData.m_Output.m_Resource != Resource.NoResource)
                {
                    outName = GetCachedResourceName(processData.m_Output.m_Resource);
                    outIcon = GetCachedResourceIcon(processData.m_Output.m_Resource);
                    list.Add(new ProcessResourceInfo
                    {
                        ResourceName = outName,
                        ResourceIcon = outIcon,
                        Amount = processData.m_Output.m_Amount,
                        IsOutput = true
                    });
                }

                if (processData.m_Input1.m_Resource != Resource.NoResource)
                {
                    list.Add(new ProcessResourceInfo
                    {
                        ResourceName = GetCachedResourceName(processData.m_Input1.m_Resource),
                        Amount = processData.m_Input1.m_Amount,
                        ResourceIcon = GetCachedResourceIcon(processData.m_Input1.m_Resource),
                        IsOutput = false
                    });
                }

                if (processData.m_Input2.m_Resource != Resource.NoResource)
                {
                    list.Add(new ProcessResourceInfo
                    {
                        ResourceName = GetCachedResourceName(processData.m_Input2.m_Resource),
                        Amount = processData.m_Input2.m_Amount,
                        ResourceIcon = GetCachedResourceIcon(processData.m_Input2.m_Resource),
                        IsOutput = false
                    });
                }
            }

            return (list.ToArray(), outName, outIcon);
        }
        
        private static int SumResourceAmounts(ResourceInfo[] arr)
        {
            if (arr == null || arr.Length == 0) return 0;
            int s = 0;
            for (int i = 0; i < arr.Length; i++) s += arr[i].Amount;
            return s;
        }

        private string GetCachedResourceName(Resource resource)
        {
            if (!m_ResourceNameCache.TryGetValue(resource, out string name))
            {
                name = EconomyUtils.GetName(resource).ToString();
                m_ResourceNameCache[resource] = name;
            }
            return name;
        }

        private string GetCachedResourceIcon(Resource resource)
        {
            if (resource == Resource.Money)
                return "Media/Game/Icons/Money.svg";
            
            if (!m_ResourceIconCache.TryGetValue(resource, out string icon))
            {
                Entity resourcePrefab = m_ResourceSystem.GetPrefab(resource);
                icon = m_ImageSystem.GetIconOrGroupIcon(resourcePrefab);
                m_ResourceIconCache[resource] = icon;
            }
            return icon;
        }
        
        private EfficiencyFactorInfo[] GetEfficiencyFactors(Entity targetEntity, BufferLookup<Efficiency> efficiencyBufferLookup)
        {
            if (!efficiencyBufferLookup.HasBuffer(targetEntity)) return Array.Empty<EfficiencyFactorInfo>();

            var buffer = efficiencyBufferLookup[targetEntity];
            if (buffer.Length == 0) return Array.Empty<EfficiencyFactorInfo>();

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
                        if ((int)item.m_Factor <= 3) break;
                    }
                }
            }

            return tempFactors.ToArray();
        }
        public string[] GetAllCompanyNames()
        {
            var uniqueCompanyNames = new HashSet<string>();
            for (int i = 0; i < m_CachedJobData.Length; i++)
            {
                var jobData = m_CachedJobData[i];
                
                if (m_CompanyNameCache.TryGetValue(jobData.Brand, out var cachedName))
                {
                    uniqueCompanyNames.Add(cachedName);
                }
            }
            var sortedNames = uniqueCompanyNames.OrderBy(name => name).ToList();
            sortedNames.Insert(0, "All Companies");
            return sortedNames.ToArray();
        }

        public CommercialCompanyDTO[] GetCompaniesByBrand(Entity brandEntity)
        {
            if (!m_JobDataValid || !m_CachedJobData.IsCreated)
                return Array.Empty<CommercialCompanyDTO>();
            
            var matchingCompanies = new List<CommercialCompanyDTO>();
            
            for (int i = 0; i < m_CachedJobData.Length; i++)
            {
                var jobData = m_CachedJobData[i];
                
                if (jobData.Brand.Equals(brandEntity))
                {
                    matchingCompanies.Add(ConvertJobDataToDTO(jobData));
                }
            }

            // Apply any active sorting to filtered results
            ApplySorts(matchingCompanies);
            
            return matchingCompanies.ToArray();
        }
        public enum ResourceFilter
        {
            Wood, Grain, Livestock, Fish, Vegetables, Cotton,
            Oil, Ore, Coal, Stone, Metals, Steel, Minerals,
            Concrete, Machinery, Petrochemicals, Chemicals,
            Plastics, Pharmaceuticals, Electronics, Vehicles,
            Beverages, ConvenienceFood, Food, Textiles,
            Timber, Paper, Furniture, Software, Telecom,
            Financial, Media, Lodging, Meals, Entertainment, Recreation, ShowAll,
        }
        private static readonly Dictionary<ResourceFilter, Resource> ResourceNames = new Dictionary<ResourceFilter, Resource>()
        {
            { ResourceFilter.Wood, Resource.Wood },
            { ResourceFilter.Grain, Resource.Grain },
            { ResourceFilter.Livestock, Resource.Livestock },
            { ResourceFilter.Fish, Resource.Fish },
            { ResourceFilter.Vegetables, Resource.Vegetables },
            { ResourceFilter.Cotton, Resource.Cotton },
            { ResourceFilter.Oil, Resource.Oil },
            { ResourceFilter.Ore, Resource.Ore },
            { ResourceFilter.Coal, Resource.Coal },
            { ResourceFilter.Stone, Resource.Stone },
            { ResourceFilter.Metals, Resource.Metals },
            { ResourceFilter.Steel, Resource.Steel },
            { ResourceFilter.Minerals, Resource.Minerals },
            { ResourceFilter.Concrete, Resource.Concrete },
            { ResourceFilter.Machinery, Resource.Machinery },
            { ResourceFilter.Petrochemicals, Resource.Petrochemicals },
            { ResourceFilter.Chemicals, Resource.Chemicals },
            { ResourceFilter.Plastics, Resource.Plastics },
            { ResourceFilter.Pharmaceuticals, Resource.Pharmaceuticals },
            { ResourceFilter.Electronics, Resource.Electronics },
            { ResourceFilter.Vehicles, Resource.Vehicles },
            { ResourceFilter.Beverages, Resource.Beverages },
            { ResourceFilter.ConvenienceFood, Resource.ConvenienceFood },
            { ResourceFilter.Food, Resource.Food },
            { ResourceFilter.Textiles, Resource.Textiles },
            { ResourceFilter.Timber, Resource.Timber },
            { ResourceFilter.Paper, Resource.Paper },
            { ResourceFilter.Furniture, Resource.Furniture },
            { ResourceFilter.Software, Resource.Software },
            { ResourceFilter.Telecom, Resource.Telecom },
            { ResourceFilter.Financial, Resource.Financial },
            { ResourceFilter.Media, Resource.Media },
            { ResourceFilter.Lodging, Resource.Lodging },
            { ResourceFilter.Meals, Resource.Meals },
            { ResourceFilter.Entertainment, Resource.Entertainment },
            { ResourceFilter.Recreation, Resource.Recreation },
            { ResourceFilter.ShowAll, Resource.NoResource },
        };
        public string[] GetAllInput1Resources()
        {
            var resourceNames = new HashSet<string>();
            foreach (var company in m_CommercialCompanyDTOs)
            {
                if (company.Input1Resources != null)
                {
                    foreach (var res in company.Input1Resources)
                    {
                        resourceNames.Add(res.ResourceName);
                    }
                }
            }
            var sortedNames = resourceNames.OrderBy(name => name).ToList();
            sortedNames.Insert(0, "All");
            return sortedNames.ToArray();
        }
        public string[] GetAllOutputResources()
        {
            var resourceNames = new HashSet<string>();
            foreach (var company in m_CommercialCompanyDTOs)
            {
                if (company.OutputResources != null)
                {
                    foreach (var res in company.OutputResources)
                    {
                        resourceNames.Add(res.ResourceName);
                    }
                }
            }
            var sortedNames = resourceNames.OrderBy(name => name).ToList();
            sortedNames.Insert(0, "All");
            return sortedNames.ToArray();
        }
    }
    
}
