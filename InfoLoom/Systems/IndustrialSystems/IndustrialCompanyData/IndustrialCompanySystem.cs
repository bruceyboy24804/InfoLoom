using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Entities;
using Colossal.Logging;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game;
using Game.Agents;
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
using InfoLoomTwo.Systems.IndustrialSystems.IndustrialCompanyData.IndustrialCompanyDomain;
using InfoLoomTwo.Systems.IndustrialSystems.IndustrialCompanyData.Jobs;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Jobs.LowLevel.Unsafe;
using DeliveryTruck = Game.Vehicles.DeliveryTruck;
using ExtractorCompany = Game.Companies.ExtractorCompany;
using ProcessingCompany = Game.Companies.ProcessingCompany;
using StorageCompany = Game.Companies.StorageCompany;
using ResourceInfo = InfoLoomTwo.Systems.IndustrialSystems.IndustrialCompanyData.IndustrialCompanyDomain.ResourceInfo;

namespace InfoLoomTwo.Systems.IndustrialSystems.IndustrialCompanyData
{
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

    /// <summary>
    /// System for collecting and processing industrial company data with lazy conversion.
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
    public partial class IndustrialCompanySystem : GameSystemBase
    {
        private NativeArray<int> results;
        public SortingEnum m_CurrentIndexSorting = SortingEnum.Off;
        public SortingEnum m_CurrentCompanyNameSorting = SortingEnum.Off;
        public SortingEnum m_CurrentEmployeesSorting = SortingEnum.Off;
        public SortingEnum m_CurrentEfficiencySorting = SortingEnum.Off;
        public SortingEnum m_CurrentProfitabilitySorting = SortingEnum.Off;
        public SortingEnum m_CurrentResourceAmountSorting = SortingEnum.Off;
        public SortingEnum m_CurrentMoneySorting = SortingEnum.Off;
        public SortingEnum m_CurrentInput1Sorting = SortingEnum.Off;
        public SortingEnum m_CurrentInput2Sorting = SortingEnum.Off;
        public SortingEnum m_CurrentOutputSorting = SortingEnum.Off;
        public SortingEnum m_CurrentMaintenanceSorting = SortingEnum.Off;
        public ResourceFilter m_CurrentResourceFilter = ResourceFilter.ShowAll;
        private static InfoLoomUISystem m_InfoloomUI;
        private NameSystem m_NameSystem;
        private ImageSystem m_ImageSystem;
        private ResourceSystem m_ResourceSystem;
        private PrefabSystem m_PrefabSystem;
        private EntityQuery m_IndustrialCompanyQuery;
        private ILog m_Log;

        public bool IsPanelVisible;
        public IndustrialCompanyDTO[] m_IndustrialCompanyDTOs;

        public Dictionary<Entity, string> m_CompanyNameCache;
        private Dictionary<Resource, string> m_ResourceNameCache;
        private Dictionary<Resource, string> m_ResourceIconCache;
        private bool m_CacheInitialized;
        private int m_LastCompanyCount;
        private bool m_ForceCompanyNameCacheUpdate;
        
        // Lazy conversion cache
        private NativeArray<IndustrialCompanyJobData> m_CachedJobData;
        private Dictionary<Entity, IndustrialCompanyDTO> m_ConvertedDTOCache;
        private bool m_JobDataValid;
        
        public Resource SelectedResource { get; set; } = Resource.NoResource;
        private JobHandle m_JobHandle;
        private NativeList<IndustrialCompanyJobData> m_JobResults;
        private int m_JobWaitFrames = 0;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = Mod.log;
            m_IndustrialCompanyDTOs = Array.Empty<IndustrialCompanyDTO>();
            m_NameSystem = World.GetOrCreateSystemManaged<NameSystem>();
            m_ImageSystem = World.GetOrCreateSystemManaged<ImageSystem>();
            m_ResourceSystem = World.GetOrCreateSystemManaged<ResourceSystem>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();

            m_IndustrialCompanyQuery = SystemAPI.QueryBuilder()
                .WithAll<IndustrialCompany, PropertyRenter>()
                .WithNone<StorageCompany, MovingAway>()
                .Build();

            m_CompanyNameCache = new Dictionary<Entity, string>();
            m_ResourceNameCache = new Dictionary<Resource, string>();
            m_ResourceIconCache = new Dictionary<Resource, string>();
            m_LastCompanyCount = 0;
            m_ForceCompanyNameCacheUpdate = false;
            m_ConvertedDTOCache = new Dictionary<Entity, IndustrialCompanyDTO>();
            m_JobDataValid = false;
            results = new NativeArray<int>(13, Allocator.Persistent);
            m_InfoloomUI = World.GetOrCreateSystemManaged<InfoLoomUISystem>();
        }

       protected override void OnDestroy()
        { 
            m_JobResults.Dispose();
            m_CachedJobData.Dispose();
            results.Dispose();
            base.OnDestroy();
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 1024;

        protected override void OnUpdate()
        {
            if (!IsPanelVisible) return;
            UpdateIndustrialStatsWithBurstJob();
            
            // Auto-populate the DTO array after job completes for backward compatibility
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
            if (m_CacheInitialized) return;

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

        public void UpdateIndustrialStatsWithBurstJob()
        {
            if (!IsPanelVisible) return;
            InitializeCaches();
            UpdateCompanyNameCacheIfNeeded();

            int estimatedCount = m_IndustrialCompanyQuery.CalculateEntityCount();
            if (m_JobResults.IsCreated)
            {
                m_JobResults.Dispose();
            }
            m_JobResults = new NativeList<IndustrialCompanyJobData>(Math.Max(1, estimatedCount), Allocator.TempJob);

            ProcessIndustrialCompaniesJob job = new ProcessIndustrialCompaniesJob
            {
                EntityType = SystemAPI.GetEntityTypeHandle(),
                CompanyDataType = SystemAPI.GetComponentTypeHandle<Game.Companies.CompanyData>(true),
                WorkProviderType = SystemAPI.GetComponentTypeHandle<WorkProvider>(true),
                PrefabRefType = SystemAPI.GetComponentTypeHandle<PrefabRef>(true),

                PropertyRenterType = SystemAPI.GetComponentTypeHandle<PropertyRenter>(true),
                ProfitabilityType = SystemAPI.GetComponentTypeHandle<Profitability>(true),
                AttachedType = SystemAPI.GetComponentTypeHandle<Attached>(true),

                EmployeeBufferType = SystemAPI.GetBufferTypeHandle<Employee>(true),
                OwnedVehicleBufferType = SystemAPI.GetBufferTypeHandle<OwnedVehicle>(true),
                ResourcesBufferType = SystemAPI.GetBufferTypeHandle<Resources>(true),
                CompanyStatisticDataLookup = GetComponentLookup<CompanyStatisticData>(true),

                TransportCompanyDataLookup = SystemAPI.GetComponentLookup<TransportCompanyData>(true),
                IndustrialProcessDataLookup = SystemAPI.GetComponentLookup<IndustrialProcessData>(true),
                ResourceDataLookup = SystemAPI.GetComponentLookup<ResourceData>(true),
                CitizenLookup = SystemAPI.GetComponentLookup<Citizen>(true),
                DeliveryTruckLookup = SystemAPI.GetComponentLookup<DeliveryTruck>(true),
                ExtractorCompanyLookup = SystemAPI.GetComponentLookup<ExtractorCompany>(true),
                ExtractorLookup = SystemAPI.GetComponentLookup<Game.Areas.Extractor>(true),
                PrefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(true),
                ExtractorAreaDataLookup = SystemAPI.GetComponentLookup<Game.Prefabs.ExtractorAreaData>(true),
                EfficiencyLookup = SystemAPI.GetBufferLookup<Efficiency>(true),
                SubAreaLookup = SystemAPI.GetBufferLookup<Game.Areas.SubArea>(true),

                EconomyParams = SystemAPI.GetSingleton<EconomyParameterData>(),
                ExtractorParams = SystemAPI.GetSingleton<ExtractorParameterData>(),
                ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
                ResultWriter = m_JobResults.AsParallelWriter()
                
            };
            var jobHandle = job.Schedule(m_IndustrialCompanyQuery, Dependency);
            jobHandle.Complete();
            
            // Cache the job results instead of immediate conversion
            CacheJobResults(m_JobResults);
            
            // Clear converted DTO cache since we have new data
            m_ConvertedDTOCache.Clear();
            m_JobDataValid = true;
            
            m_JobResults.Dispose();
        }

        private void CacheJobResults(NativeList<IndustrialCompanyJobData> jobResults)
        {
            // Dispose old cached data if exists
            if (m_CachedJobData.IsCreated)
                m_CachedJobData.Dispose();
            
            // Store new job data in persistent array
            m_CachedJobData = new NativeArray<IndustrialCompanyJobData>(jobResults.Length, Allocator.Persistent);
            jobResults.AsArray().CopyTo(m_CachedJobData);
        }

        private void UpdateCompanyNameCacheIfNeeded()
        {
            int currentCount = m_IndustrialCompanyQuery.CalculateEntityCount();
            
            // Only update if company count changed or forced
            if (!m_ForceCompanyNameCacheUpdate && currentCount == m_LastCompanyCount)
                return;
            
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

                if (resource.m_Resource == input1 && input1 != Resource.NoResource) 
                {
                    input1Array[in1Count++] = info;
                }
                else if (resource.m_Resource == input2 && input2 != Resource.NoResource) 
                {
                    input2Array[in2Count++] = info;
                }
                else if (resource.m_Resource == output && output != Resource.NoResource) 
                {
                    outputArray[outCount++] = info;
                }
                else 
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
        public void ApplySorts(List<IndustrialCompanyDTO> companies)
        {
            IOrderedEnumerable<IndustrialCompanyDTO> ordered = null;

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

            if (m_CurrentInput2Sorting == SortingEnum.Ascending)
                ordered = ordered == null ? companies.OrderBy(x => SumResourceAmounts(x.Input2Resources)) : ordered.ThenBy(x => SumResourceAmounts(x.Input2Resources));
            else if (m_CurrentInput2Sorting == SortingEnum.Descending)
                ordered = ordered == null ? companies.OrderByDescending(x => SumResourceAmounts(x.Input2Resources)) : ordered.ThenByDescending(x => SumResourceAmounts(x.Input2Resources));

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
        // Get all companies with lazy conversion and sorting
        
        
        // Convert a single job data to DTO with caching
        private IndustrialCompanyDTO ConvertJobDataToDTO(IndustrialCompanyJobData jobData)
        {
            // Check cache first
            if (m_ConvertedDTOCache.TryGetValue(jobData.EntityId, out var cached))
                return cached;
            
            // Do the expensive conversion
            var resourcesBufferLookup = GetBufferLookup<Resources>(true);
            var efficiencyBufferLookup = GetBufferLookup<Efficiency>(true);
            var propertyRenterLookup = GetComponentLookup<PropertyRenter>(true);
            var prefabRefLookup = GetComponentLookup<PrefabRef>(true);
            var industrialProcessLookup = GetComponentLookup<IndustrialProcessData>(true);
            
            Resource processInput1 = Resource.NoResource;
            Resource processInput2 = Resource.NoResource;
            Resource processOutput = Resource.NoResource;
            if (prefabRefLookup.TryGetComponent(jobData.EntityId, out var prefabRef) &&
                industrialProcessLookup.TryGetComponent(prefabRef.m_Prefab, out var proc))
            {
                processInput1 = proc.m_Input1.m_Resource;
                processInput2 = proc.m_Input2.m_Resource;
                processOutput = proc.m_Output.m_Resource;
            }
            
            ClassifyResources(jobData.EntityId, resourcesBufferLookup,
                processInput1, processInput2, processOutput,
                out var input1Resources, out var input2Resources, out var outputResources, out var maintenanceResources, out int moneyAmount);
            var (processList, outputResourceName, outputResourceIcon) = GetProcessInfo(jobData.EntityId, prefabRefLookup, industrialProcessLookup);
            
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
            if (jobData.HasStatistics)
            {
                var companyStatisticDataLookup = SystemAPI.GetComponentLookup<CompanyStatisticData>(true);
                if (companyStatisticDataLookup.TryGetComponent(jobData.EntityId, out var companyStatisticData))
                {
                    income = companyStatisticData.m_Income;
                    worth = companyStatisticData.m_Worth;
                    profit = companyStatisticData.m_Profit;
                    wagePaid = companyStatisticData.m_WagePaid;
                    rentPaid = companyStatisticData.m_RentPaid;
                    electricityPaid = companyStatisticData.m_ElectricityPaid;
                    waterPaid = companyStatisticData.m_WaterPaid;
                    sewagePaid = companyStatisticData.m_SewagePaid;
                    garbagePaid = companyStatisticData.m_GarbagePaid;
                    taxPaid = companyStatisticData.m_TaxPaid;
                    resourcesBoughtPaid = companyStatisticData.m_CostBuyResource;
                    currentCustomers = companyStatisticData.m_CurrentNumberOfCustomers;
                    monthlyCustomers = companyStatisticData.m_MonthlyCustomerCount;
                }
            }
            
            var dto = new IndustrialCompanyDTO
            {
                EntityId = jobData.EntityId,
                CompanyName = companyNameString,
                TotalEmployees = jobData.TotalEmployees,
                MaxWorkers = jobData.MaxWorkers,
                VehicleCount = jobData.VehicleCount,
                VehicleCapacity = jobData.VehicleCapacity,
                ResourceAmount = jobData.ResourceCount,
                ProcessResources = processList,
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
                MoneyAmount = moneyAmount,
                Input1Resources = input1Resources,
                Input2Resources = input2Resources,
                OutputResources = outputResources,
                MaintenanceResources = maintenanceResources,
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
            m_ConvertedDTOCache[jobData.EntityId] = dto;
            return dto;
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
        public IndustrialCompanyDTO[] GetAllCompanies()
        {
            if (!m_JobDataValid || !m_CachedJobData.IsCreated)
                return m_IndustrialCompanyDTOs;
            
            var companies = new List<IndustrialCompanyDTO>(m_CachedJobData.Length);
            for (int i = 0; i < m_CachedJobData.Length; i++)
            {
                var jobData = m_CachedJobData[i];
                companies.Add(ConvertJobDataToDTO(jobData));
            }
            
            ApplySorts(companies);
            m_IndustrialCompanyDTOs = companies.ToArray();
            return m_IndustrialCompanyDTOs;
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

        public IndustrialCompanyDTO[] GetCompaniesByBrand(Entity brandEntity)
        {
            if (!m_JobDataValid || !m_CachedJobData.IsCreated)
                return Array.Empty<IndustrialCompanyDTO>();
            
            var matchingCompanies = new List<IndustrialCompanyDTO>();
            
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
            foreach (var company in m_IndustrialCompanyDTOs)
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
        public string[] GetAllInput2Resources()
        {
            var resourceNames = new HashSet<string>();
            foreach (var company in m_IndustrialCompanyDTOs)
            {
                if (company.Input2Resources != null)
                {
                    foreach (var res in company.Input2Resources)
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
            foreach (var company in m_IndustrialCompanyDTOs)
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