using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Entities;
using Colossal.Logging;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Objects;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Simulation;
using Game.UI;
using Game.UI.InGame;
using Game.Vehicles;
using InfoLoomTwo.Domain.DataDomain;
using InfoLoomTwo.Domain.DataDomain.Enums;
using InfoLoomTwo.Domain.DataDomain.Enums.CompanyPanelEnums;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using DeliveryTruck = Game.Vehicles.DeliveryTruck;
using ExtractorCompany = Game.Companies.ExtractorCompany;
using ProcessingCompany = Game.Companies.ProcessingCompany;
using StorageCompany = Game.Companies.StorageCompany;

namespace InfoLoomTwo.Systems.IndustrialSystems.IndustrialCompanyData
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
            writer.PropertyName("resourceName");
            writer.Write(ResourceName);
            writer.PropertyName("amount");
            writer.Write(Amount);
            writer.PropertyName("resourceIcon");
            writer.Write(ResourceIcon);
            writer.PropertyName("isOutput");
            writer.Write(IsOutput);
            writer.TypeEnd();
        }
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

    public struct IndustrialCompanyDTO
    {
        public Entity EntityId;
        public string CompanyName;
        public int TotalEmployees;
        public int MaxWorkers;
        public int VehicleCount;
        public int VehicleCapacity;
        public int ResourceAmount;
        public ProcessResourceInfo[] ProcessResources;
        public int TotalEfficiency;
        public EfficiencyFactorInfo[] Factors;
        public float Profitability;
        public int LastTotalWorth;
        public int TotalWages;
        public int ProductionPerDay;
        public float EfficiencyValue;
        public string OutputResourceName;
        public bool IsExtractor;
        public string ResourceIcon; // For backward compatibility
        public string ResourceName; // For backward compatibility
        public ResourceInfo[] Resources; // New field to hold all resources
    }

    // Burst-compatible struct for industrial company data
    public struct IndustrialCompanyJobData : IComponentData
    {
        public Entity EntityId;
        public FixedString64Bytes CompanyName;
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
        public bool IsExtractor;
        public int ResourceCount;
    }

    // Burst-compiled job for processing industrial companies
    [BurstCompile]
    public struct ProcessIndustrialCompaniesJob : IJobChunk
    {
        // Required component type handles
        [ReadOnly] public EntityTypeHandle EntityType;
        [ReadOnly] public ComponentTypeHandle<Game.Companies.CompanyData> CompanyDataType;
        [ReadOnly] public ComponentTypeHandle<WorkProvider> WorkProviderType;
        [ReadOnly] public ComponentTypeHandle<PrefabRef> PrefabRefType;
        
        // Optional component type handles
        [ReadOnly] public ComponentTypeHandle<PropertyRenter> PropertyRenterType;
        [ReadOnly] public ComponentTypeHandle<Profitability> ProfitabilityType;
        [ReadOnly] public ComponentTypeHandle<Attached> AttachedType;
        
        // Buffer type handles
        [ReadOnly] public BufferTypeHandle<Employee> EmployeeBufferType;
        [ReadOnly] public BufferTypeHandle<OwnedVehicle> OwnedVehicleBufferType;
        [ReadOnly] public BufferTypeHandle<Resources> ResourcesBufferType;
        
        // Component lookups for related entities
        [ReadOnly] public ComponentLookup<TransportCompanyData> TransportCompanyDataLookup;
        [ReadOnly] public ComponentLookup<IndustrialProcessData> IndustrialProcessDataLookup;
        [ReadOnly] public ComponentLookup<ResourceData> ResourceDataLookup;
        [ReadOnly] public ComponentLookup<Citizen> CitizenLookup;
        [ReadOnly] public ComponentLookup<DeliveryTruck> DeliveryTruckLookup;
        [ReadOnly] public ComponentLookup<ExtractorCompany> ExtractorCompanyLookup;
        [ReadOnly] public ComponentLookup<Game.Areas.Extractor> ExtractorLookup;
        [ReadOnly] public ComponentLookup<PrefabRef> PrefabRefLookup;
        [ReadOnly] public ComponentLookup<Game.Prefabs.ExtractorAreaData> ExtractorAreaDataLookup;
        [ReadOnly] public BufferLookup<Efficiency> EfficiencyLookup;
        [ReadOnly] public BufferLookup<Game.Areas.SubArea> SubAreaLookup;
        
        // Shared data
        [ReadOnly] public EconomyParameterData EconomyParams;
        [ReadOnly] public ExtractorParameterData ExtractorParams;
        [ReadOnly] public ResourcePrefabs ResourcePrefabs;
        
        // Output data
        public NativeList<IndustrialCompanyJobData>.ParallelWriter ResultWriter;
        
        // Cache lookups
        [ReadOnly] public NativeHashMap<Entity, FixedString64Bytes> CompanyNames;
        
        // Burst-compatible default name
        [ReadOnly] public FixedString64Bytes DefaultCompanyName;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            // Early exit if chunk doesn't have required components
            if (!chunk.Has(EmployeeBufferType))
                return;

            // Get arrays for the entire chunk
            var entities = chunk.GetNativeArray(EntityType);
            var companyDataArray = chunk.GetNativeArray(ref CompanyDataType);
            var workProviderArray = chunk.GetNativeArray(ref WorkProviderType);
            var prefabRefArray = chunk.GetNativeArray(ref PrefabRefType);
            
            // Check for optional components at chunk level
            var hasPropertyRenter = chunk.Has(ref PropertyRenterType);
            var hasProfitability = chunk.Has(ref ProfitabilityType);
            var hasOwnedVehicles = chunk.Has(ref OwnedVehicleBufferType);
            var hasResources = chunk.Has(ref ResourcesBufferType);
            var hasAttached = chunk.Has(ref AttachedType);
            
            var propertyRenterArray = hasPropertyRenter ? chunk.GetNativeArray(ref PropertyRenterType) : default;
            var profitabilityArray = hasProfitability ? chunk.GetNativeArray(ref ProfitabilityType) : default;
            var attachedArray = hasAttached ? chunk.GetNativeArray(ref AttachedType) : default;
            
            // Get buffer accessors
            var employeeBufferAccessor = chunk.GetBufferAccessor(ref EmployeeBufferType);
            var ownedVehicleAccessor = hasOwnedVehicles ? chunk.GetBufferAccessor(ref OwnedVehicleBufferType) : default;
            var resourcesAccessor = hasResources ? chunk.GetBufferAccessor(ref ResourcesBufferType) : default;

            // Process all entities in this chunk
            for (int i = 0; i < chunk.Count; i++)
            {
                var entity = entities[i];
                var employeeBuffer = employeeBufferAccessor[i];
                
                // Skip companies with no employees
                if (employeeBuffer.Length == 0)
                    continue;

                var companyData = companyDataArray[i];
                var workProvider = workProviderArray[i];
                var prefabRef = prefabRefArray[i];
                Entity prefab = prefabRef.m_Prefab;

                // Get company name from cache
                var companyName = CompanyNames.TryGetValue(companyData.m_Brand, out var name) ? 
                    name : DefaultCompanyName;

                // Calculate basic data
                int activeVehicles = hasOwnedVehicles ? CountActiveVehicles(ownedVehicleAccessor[i]) : 0;
                int maxDeliveryTrucks = GetMaxDeliveryTrucks(prefab);
                int resourceCount = hasResources ? resourcesAccessor[i].Length : 0;
                bool isExtractor = ExtractorCompanyLookup.HasComponent(entity);

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

                // Create job data
                var jobData = new IndustrialCompanyJobData
                {
                    EntityId = entity,
                    CompanyName = companyName,
                    TotalEmployees = employeeBuffer.Length,
                    MaxWorkers = workProvider.m_MaxWorkers,
                    VehicleCount = activeVehicles,
                    VehicleCapacity = maxDeliveryTrucks,
                    TotalEfficiency = 100,
                    Profitability = profitabilityValue,
                    LastTotalWorth = lastTotalWorth,
                    TotalWages = totalWages,
                    ProductionPerDay = 0,
                    EfficiencyValue = 100f,
                    IsExtractor = isExtractor,
                    ResourceCount = resourceCount
                };

                ResultWriter.AddNoResize(jobData);
            }
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
            
            return EconomyUtils.GetCompanyProductionPerDay(
                efficiencyValue,
                true, // isIndustrial
                employeeBuffer,
                industryProcess,
                ResourcePrefabs,
                ref ResourceDataLookup,
                ref CitizenLookup,
                ref EconomyParams);
        }
    }
    
    public partial class IndustrialCompanySystem : GameSystemBase
    {
        public IndexSortingEnum2 m_CurrentIndexSorting = IndexSortingEnum2.Off;
        public CompanyNameEnum2 m_CurrentCompanyNameSorting = CompanyNameEnum2.Off;
        public EmployeesEnum2 m_CurrentEmployeesSorting = EmployeesEnum2.Off;
        public EfficiancyEnum2 m_CurrentEfficiencySorting = EfficiancyEnum2.Off;
        public ProfitabilityEnum2 m_CurrentProfitabilitySorting = ProfitabilityEnum2.Off;
        public ResourceAmountEnum2 m_CurrentResourceAmountSorting = ResourceAmountEnum2.Off;
        
        
        private NameSystem m_NameSystem;
        private ImageSystem m_ImageSystem;
        private ResourceSystem m_ResourceSystem;
        private PrefabSystem m_PrefabSystem;
        private EntityQuery m_IndustrialCompanyQuery;
        private ILog m_Log;
        
        public bool IsPanelVisible;
        public IndustrialCompanyDTO[] m_IndustrialCompanyDTOs;

        // Cached lookups for performance
        private NativeHashMap<Entity, FixedString64Bytes> m_CompanyNameCache;
        private Dictionary<Resource, string> m_ResourceNameCache;
        private Dictionary<Resource, string> m_ResourceIconCache;
        private bool m_CacheInitialized;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = Mod.log;
            m_IndustrialCompanyDTOs = Array.Empty<IndustrialCompanyDTO>();
            m_NameSystem = World.GetOrCreateSystemManaged<NameSystem>();
            m_ImageSystem = World.GetOrCreateSystemManaged<ImageSystem>();
            m_ResourceSystem = World.GetOrCreateSystemManaged<ResourceSystem>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();

            // Define optimized query for industrial companies
            m_IndustrialCompanyQuery = SystemAPI.QueryBuilder()
                .WithAll<IndustrialCompany>()
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

            UpdateIndustrialStatsWithBurstJob();
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

        private void UpdateIndustrialStatsWithBurstJob()
        {
            if (!IsPanelVisible)
                return;

            InitializeCaches();
            UpdateCompanyNameCache();

            // Create result list with estimated capacity
            int estimatedCount = m_IndustrialCompanyQuery.CalculateEntityCount();
            var jobResults = new NativeList<IndustrialCompanyJobData>(estimatedCount, Allocator.TempJob);

            // Create the burst-compiled job
            var job = new ProcessIndustrialCompaniesJob
            {
                // Required component type handles
                EntityType = GetEntityTypeHandle(),
                CompanyDataType = GetComponentTypeHandle<Game.Companies.CompanyData>(true),
                WorkProviderType = GetComponentTypeHandle<WorkProvider>(true),
                PrefabRefType = GetComponentTypeHandle<PrefabRef>(true),
                
                // Optional component type handles
                PropertyRenterType = GetComponentTypeHandle<PropertyRenter>(true),
                ProfitabilityType = GetComponentTypeHandle<Profitability>(true),
                AttachedType = GetComponentTypeHandle<Attached>(true),
                
                // Buffer type handles
                EmployeeBufferType = GetBufferTypeHandle<Employee>(true),
                OwnedVehicleBufferType = GetBufferTypeHandle<OwnedVehicle>(true),
                ResourcesBufferType = GetBufferTypeHandle<Resources>(true),
                
                // Component lookups
                TransportCompanyDataLookup = GetComponentLookup<TransportCompanyData>(true),
                IndustrialProcessDataLookup = GetComponentLookup<IndustrialProcessData>(true),
                ResourceDataLookup = GetComponentLookup<ResourceData>(true),
                CitizenLookup = GetComponentLookup<Citizen>(true),
                DeliveryTruckLookup = GetComponentLookup<DeliveryTruck>(true),
                ExtractorCompanyLookup = GetComponentLookup<ExtractorCompany>(true),
                ExtractorLookup = GetComponentLookup<Game.Areas.Extractor>(true),
                PrefabRefLookup = GetComponentLookup<PrefabRef>(true),
                ExtractorAreaDataLookup = GetComponentLookup<Game.Prefabs.ExtractorAreaData>(true),
                EfficiencyLookup = GetBufferLookup<Efficiency>(true),
                SubAreaLookup = GetBufferLookup<Game.Areas.SubArea>(true),
                
                // Shared data
                EconomyParams = SystemAPI.GetSingleton<EconomyParameterData>(),
                ExtractorParams = SystemAPI.GetSingleton<ExtractorParameterData>(),
                ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
                
                // Cache and output
                CompanyNames = m_CompanyNameCache,
                ResultWriter = jobResults.AsParallelWriter(),
                DefaultCompanyName = new FixedString64Bytes("Unknown Company")
            };

            // Schedule the job to run in parallel across multiple chunks
            var jobHandle = job.ScheduleParallel(m_IndustrialCompanyQuery, Dependency);
            
            // Complete the job and convert results
            jobHandle.Complete();
            
            // Convert job results to final DTOs
            ConvertJobResultsToDTO(jobResults);
            
            jobResults.Dispose();
        }

        private void UpdateCompanyNameCache()
        {
            var entities = m_IndustrialCompanyQuery.ToEntityArray(Allocator.Temp);
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

        private void ConvertJobResultsToDTO(NativeList<IndustrialCompanyJobData> jobResults)
        {
            var companies = new List<IndustrialCompanyDTO>(jobResults.Length);
            var resourcesBufferLookup = GetBufferLookup<Resources>(true);
            var efficiencyBufferLookup = GetBufferLookup<Efficiency>(true);
            var propertyRenterLookup = GetComponentLookup<PropertyRenter>(true);
            var prefabRefLookup = GetComponentLookup<PrefabRef>(true);
            var industrialProcessLookup = GetComponentLookup<IndustrialProcessData>(true);

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

                // Get process resources
                var processList = new List<ProcessResourceInfo>();
                string outputResourceName = "";
                string outputResourceIcon = "";
                
                if (prefabRefLookup.TryGetComponent(jobData.EntityId, out var prefabRef) &&
                    industrialProcessLookup.TryGetComponent(prefabRef.m_Prefab, out var processData))
                {
                    // Output
                    if (processData.m_Output.m_Resource != Resource.NoResource)
                    {
                        string resourceName = EconomyUtils.GetName(processData.m_Output.m_Resource).ToString();
                        outputResourceIcon = GetResourceIconPath(processData.m_Output.m_Resource);
                        outputResourceName = resourceName;
                        processList.Add(new ProcessResourceInfo {
                            ResourceName = resourceName,
                            ResourceIcon = outputResourceIcon,
                            Amount = processData.m_Output.m_Amount,
                            IsOutput = true
                        });
                    }

                    // Input 1
                    if (processData.m_Input1.m_Resource != Resource.NoResource)
                    {
                        processList.Add(new ProcessResourceInfo {
                            ResourceName = EconomyUtils.GetName(processData.m_Input1.m_Resource).ToString(),
                            Amount = processData.m_Input1.m_Amount,
                            ResourceIcon = GetResourceIconPath(processData.m_Input1.m_Resource),
                            IsOutput = false
                        });
                    }

                    // Input 2
                    if (processData.m_Input2.m_Resource != Resource.NoResource)
                    {
                        processList.Add(new ProcessResourceInfo {
                            ResourceName = EconomyUtils.GetName(processData.m_Input2.m_Resource).ToString(),
                            Amount = processData.m_Input2.m_Amount,
                            ResourceIcon = GetResourceIconPath(processData.m_Input2.m_Resource),
                            IsOutput = false
                        });
                    }
                }

                // Get efficiency factors
                EfficiencyFactorInfo[] factors = Array.Empty<EfficiencyFactorInfo>();
                if (propertyRenterLookup.HasComponent(jobData.EntityId))
                {
                    var targetEntity = propertyRenterLookup[jobData.EntityId].m_Property;
                    factors = GetEfficiencyFactors(targetEntity, efficiencyBufferLookup);
                }

                // Create DTO
                var dto = new IndustrialCompanyDTO
                {
                    EntityId = jobData.EntityId,
                    CompanyName = jobData.CompanyName.ToString(),
                    TotalEmployees = jobData.TotalEmployees,
                    MaxWorkers = jobData.MaxWorkers,
                    VehicleCount = jobData.VehicleCount,
                    VehicleCapacity = jobData.VehicleCapacity,
                    ResourceAmount = jobData.ResourceCount,
                    ProcessResources = processList.ToArray(),
                    TotalEfficiency = jobData.TotalEfficiency,
                    Factors = factors,
                    Profitability = jobData.Profitability,
                    LastTotalWorth = jobData.LastTotalWorth,
                    TotalWages = jobData.TotalWages,
                    ProductionPerDay = jobData.ProductionPerDay,
                    EfficiencyValue = jobData.EfficiencyValue,
                    OutputResourceName = outputResourceName,
                    IsExtractor = jobData.IsExtractor,
                    ResourceIcon = outputResourceIcon,
                    ResourceName = outputResourceName,
                    Resources = resources.ToArray()
                };

                companies.Add(dto);
            }
            m_IndustrialCompanyDTOs = companies.ToArray();
        }

        private string GetFormattedResourceName(Resource resource)
        {
            if (m_ResourceNameCache.TryGetValue(resource, out var name))
                return name;

            var resourceName = EconomyUtils.GetName(resource).ToString();
            m_ResourceNameCache[resource] = resourceName;
            return resourceName;
        }

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

        private EfficiencyFactorInfo[] GetEfficiencyFactors(Entity targetEntity, BufferLookup<Efficiency> efficiencyBufferLookup)
        {
            if (!efficiencyBufferLookup.HasBuffer(targetEntity))
                return Array.Empty<EfficiencyFactorInfo>();
                
            var buffer = efficiencyBufferLookup[targetEntity];
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
        public int CompareByIndex(IndustrialCompanyDTO x, IndustrialCompanyDTO y)
        {
            switch (m_CurrentIndexSorting)
            {
                case IndexSortingEnum2.Ascending:
                    return x.EntityId.Index.CompareTo(y.EntityId.Index);
                case IndexSortingEnum2.Descending:
                    return y.EntityId.Index.CompareTo(x.EntityId.Index);
                default:
                    return 0;
            }
        }
        public int CompareByName(IndustrialCompanyDTO x, IndustrialCompanyDTO y)
        {
            switch (m_CurrentCompanyNameSorting)
            {
                case CompanyNameEnum2.Ascending:
                    return string.Compare(x.CompanyName, y.CompanyName, StringComparison.OrdinalIgnoreCase);
                case CompanyNameEnum2.Descending:
                    return string.Compare(y.CompanyName, x.CompanyName, StringComparison.OrdinalIgnoreCase);
                default:
                    return 0;
            }
        }
        public int CompareByEmployees(IndustrialCompanyDTO x, IndustrialCompanyDTO y)
        {
            switch (m_CurrentEmployeesSorting)
            {
                case EmployeesEnum2.Ascending:
                    return x.TotalEmployees.CompareTo(y.TotalEmployees);
                case EmployeesEnum2.Descending:
                    return y.TotalEmployees.CompareTo(x.TotalEmployees);
                default:
                    return 0;
            }
        }
        public int CompareByEfficiency(IndustrialCompanyDTO x, IndustrialCompanyDTO y)
        {
            switch (m_CurrentEfficiencySorting)
            {
                case EfficiancyEnum2.Ascending:
                    return x.TotalEfficiency.CompareTo(y.TotalEfficiency);
                case EfficiancyEnum2.Descending:
                    return y.TotalEfficiency.CompareTo(x.TotalEfficiency);
                default:
                    return 0;
            }
        }
        public int CompareByProfitability(IndustrialCompanyDTO x, IndustrialCompanyDTO y)
        {
            switch (m_CurrentProfitabilitySorting)
            {
                case ProfitabilityEnum2.Ascending:
                    return x.Profitability.CompareTo(y.Profitability);
                case ProfitabilityEnum2.Descending:
                    return y.Profitability.CompareTo(x.Profitability);
                default:
                    return 0;
            }
        }
        public int CompareByResourceAmount(IndustrialCompanyDTO x, IndustrialCompanyDTO y)
        {
            switch (m_CurrentResourceAmountSorting)
            {
                case ResourceAmountEnum2.Ascending:
                    return x.ResourceAmount.CompareTo(y.ResourceAmount);
                case ResourceAmountEnum2.Descending:
                    return y.ResourceAmount.CompareTo(x.ResourceAmount);
                default:
                    return 0;
            }
        }
            
    }
}
