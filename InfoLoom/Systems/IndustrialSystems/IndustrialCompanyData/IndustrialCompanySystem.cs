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
using InfoLoomTwo.Systems.IndustrialSystems.IndustrialCompanyData.IndustrialCompanyDomain;
using InfoLoomTwo.Systems.IndustrialSystems.IndustrialCompanyData.Jobs;
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

    public partial class IndustrialCompanySystem : GameSystemBase
    {
        public IndexSortingEnum2 m_CurrentIndexSorting = IndexSortingEnum2.Off;
        public CompanyNameEnum2 m_CurrentCompanyNameSorting = CompanyNameEnum2.Off;
        public EmployeesEnum2 m_CurrentEmployeesSorting = EmployeesEnum2.Off;
        public EfficiancyEnum2 m_CurrentEfficiencySorting = EfficiancyEnum2.Off;
        public ProfitabilityEnum2 m_CurrentProfitabilitySorting = ProfitabilityEnum2.Off;
        public ResourceAmountEnum2 m_CurrentResourceAmountSorting = ResourceAmountEnum2.Off;
        public MoneyEnum2 m_CurrentMoneySorting = MoneyEnum2.Off;
        public Input1Enum2 m_CurrentInput1Sorting = Input1Enum2.Off;
        public Input2Enum2 m_CurrentInput2Sorting = Input2Enum2.Off;
        public OutputEnum2 m_CurrentOutputSorting = OutputEnum2.Off;
        public MaintenanceEnum2 m_CurrentMaintenanceSorting = MaintenanceEnum2.Off;
        public ResourceFilter m_CurrentResourceFilter = ResourceFilter.ShowAll;

        private NameSystem m_NameSystem;
        private ImageSystem m_ImageSystem;
        private ResourceSystem m_ResourceSystem;
        private PrefabSystem m_PrefabSystem;
        private EntityQuery m_IndustrialCompanyQuery;
        private ILog m_Log;

        public bool IsPanelVisible;
        public IndustrialCompanyDTO[] m_IndustrialCompanyDTOs;

        private Dictionary<Entity, string> m_CompanyNameCache;
        private Dictionary<Resource, string> m_ResourceNameCache;
        private Dictionary<Resource, string> m_ResourceIconCache;
        private bool m_CacheInitialized;
        private bool m_JobScheduled = false;
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
                .WithAll<IndustrialCompany>()
                .WithNone<StorageCompany>()
                .Build();

            m_CompanyNameCache = new Dictionary<Entity, string>();
            m_ResourceNameCache = new Dictionary<Resource, string>();
            m_ResourceIconCache = new Dictionary<Resource, string>();
        }

       protected override void OnDestroy()
        {
            if (m_JobScheduled)
            {
                try { m_JobHandle.Complete(); }
                catch { /* ignore completion errors during shutdown */ }

                if (m_JobResults.IsCreated)
                {
                    ConvertJobResultsToDTO(m_JobResults);
                    m_JobResults.Dispose();
                }

                m_JobScheduled = false;
            }

            if (m_JobResults.IsCreated) m_JobResults.Dispose();
            base.OnDestroy();
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 1024;

        protected override void OnUpdate()
        {
            if (!IsPanelVisible) return;
            if (m_JobScheduled)
            {
                if (m_JobHandle.IsCompleted)
                {
                    m_JobHandle.Complete();
                    try
                    {
                        ConvertJobResultsToDTO(m_JobResults);
                    }
                    finally
                    {
                        if (m_JobResults.IsCreated) m_JobResults.Dispose();
                        m_JobScheduled = false;
                    }
                }
                return;
            }
            UpdateIndustrialStatsWithBurstJob();
        }

        protected override void OnGameLoaded(Context serializationContext)
        {
            base.OnGameLoaded(serializationContext);
            InitializeCaches();
        }

        private void InitializeCaches()
        {
            if (m_CacheInitialized) return;

            for (int i = 0; i < 64; i++)
            {
                var resource = (Resource)i;
                if (resource == Resource.NoResource) continue;
                m_ResourceNameCache[resource] = GetFormattedResourceName(resource);
                m_ResourceIconCache[resource] = GetResourceIconPath(resource);
            }

            m_CacheInitialized = true;
        }

        private void UpdateIndustrialStatsWithBurstJob()
        {
            if (!IsPanelVisible) return;
            InitializeCaches();
            UpdateCompanyNameCache();

            int estimatedCount = m_IndustrialCompanyQuery.CalculateEntityCount();
            if (m_JobResults.IsCreated)
            {
                if (m_JobScheduled) return;
                m_JobResults.Dispose();
            }
            m_JobResults = new NativeList<IndustrialCompanyJobData>(Math.Max(1, estimatedCount), Allocator.TempJob);

            m_JobHandle = new ProcessIndustrialCompaniesJob()
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
                
                ResultWriter = m_JobResults.AsParallelWriter(),
            }.ScheduleParallel(m_IndustrialCompanyQuery, Dependency);
            m_JobScheduled = true;
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
                            m_CompanyNameCache[companyData.m_Brand] = companyName;
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

                // single prefab/process fetch
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
                };

                companies.Add(dto);
            }

            ApplySorts(companies);
            m_IndustrialCompanyDTOs = companies.ToArray();
        }

        private void ClassifyResources(Entity entity, BufferLookup<Resources> resourcesBufferLookup,
            Resource input1, Resource input2, Resource output,
            out ResourceInfo[] input1List, out ResourceInfo[] input2List, out ResourceInfo[] outputList, out ResourceInfo[] maintenanceList, out int money)
        {
            var in1 = new List<ResourceInfo>();
            var in2 = new List<ResourceInfo>();
            var outL = new List<ResourceInfo>();
            var maint = new List<ResourceInfo>();
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
            for (int r = 0; r < buffer.Length; r++)
            {
                var resource = buffer[r];
                string name = GetFormattedResourceName(resource.m_Resource);
                string icon = m_ResourceIconCache.TryGetValue(resource.m_Resource, out var cached) ? cached : GetResourceIconPath(resource.m_Resource);
                var info = new ResourceInfo(name, resource.m_Amount, icon);

                if (resource.m_Resource == Resource.Money) money = resource.m_Amount;
                else if (resource.m_Resource == input1 && input1 != Resource.NoResource) in1.Add(info);
                else if (resource.m_Resource == input2 && input2 != Resource.NoResource) in2.Add(info);
                else if (resource.m_Resource == output && output != Resource.NoResource) outL.Add(info);
                else maint.Add(info);
            }

            input1List = in1.ToArray();
            input2List = in2.ToArray();
            outputList = outL.ToArray();
            maintenanceList = maint.ToArray();
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
                    outName = EconomyUtils.GetName(processData.m_Output.m_Resource).ToString();
                    outIcon = m_ResourceIconCache.TryGetValue(processData.m_Output.m_Resource, out var cached) ? cached : GetResourceIconPath(processData.m_Output.m_Resource);
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
                        ResourceName = EconomyUtils.GetName(processData.m_Input1.m_Resource).ToString(),
                        Amount = processData.m_Input1.m_Amount,
                        ResourceIcon = m_ResourceIconCache.TryGetValue(processData.m_Input1.m_Resource, out var c1) ? c1 : GetResourceIconPath(processData.m_Input1.m_Resource),
                        IsOutput = false
                    });
                }

                if (processData.m_Input2.m_Resource != Resource.NoResource)
                {
                    list.Add(new ProcessResourceInfo
                    {
                        ResourceName = EconomyUtils.GetName(processData.m_Input2.m_Resource).ToString(),
                        Amount = processData.m_Input2.m_Amount,
                        ResourceIcon = m_ResourceIconCache.TryGetValue(processData.m_Input2.m_Resource, out var c2) ? c2 : GetResourceIconPath(processData.m_Input2.m_Resource),
                        IsOutput = false
                    });
                }
            }

            return (list.ToArray(), outName, outIcon);
        }

        private void ApplySorts(List<IndustrialCompanyDTO> companies)
        {
            var comparers = new List<Comparison<IndustrialCompanyDTO>>();

            if (m_CurrentIndexSorting != IndexSortingEnum2.Off)
            {
                Comparison<IndustrialCompanyDTO> c = (a, b) => a.EntityId.Index.CompareTo(b.EntityId.Index);
                if (m_CurrentIndexSorting == IndexSortingEnum2.Descending) c = (a, b) => -c(a, b);
                comparers.Add(c);
            }
            if (m_CurrentCompanyNameSorting != CompanyNameEnum2.Off)
            {
                Comparison<IndustrialCompanyDTO> c = (a, b) => string.Compare(a.CompanyName, b.CompanyName, StringComparison.Ordinal);                
                if (m_CurrentCompanyNameSorting == CompanyNameEnum2.Descending) c = (a, b) => -c(a, b);
                comparers.Add(c);
            }
            if (m_CurrentEmployeesSorting != EmployeesEnum2.Off)
            {
                Comparison<IndustrialCompanyDTO> c = (a, b) => a.TotalEmployees.CompareTo(b.TotalEmployees);
                if (m_CurrentEmployeesSorting == EmployeesEnum2.Descending) c = (a, b) => -c(a, b);
                comparers.Add(c);
            }
            if (m_CurrentEfficiencySorting != EfficiancyEnum2.Off)
            {
                Comparison<IndustrialCompanyDTO> c = (a, b) => a.EfficiencyValue.CompareTo(b.EfficiencyValue);
                if (m_CurrentEfficiencySorting == EfficiancyEnum2.Descending) c = (a, b) => -c(a, b);
                comparers.Add(c);
            }
            if (m_CurrentProfitabilitySorting != ProfitabilityEnum2.Off)
            {
                Comparison<IndustrialCompanyDTO> c = (a, b) => a.Profitability.CompareTo(b.Profitability);
                if (m_CurrentProfitabilitySorting == ProfitabilityEnum2.Descending) c = (a, b) => -c(a, b);
                comparers.Add(c);
            }
            if (m_CurrentResourceAmountSorting != ResourceAmountEnum2.Off)
            {
                Comparison<IndustrialCompanyDTO> c = (a, b) => a.ResourceAmount.CompareTo(b.ResourceAmount);
                if (m_CurrentResourceAmountSorting == ResourceAmountEnum2.Descending) c = (a, b) => -c(a, b);
                comparers.Add(c);
            }
            if (m_CurrentMoneySorting != MoneyEnum2.Off)
            {
                Comparison<IndustrialCompanyDTO> c = (a, b) => a.MoneyAmount.CompareTo(b.MoneyAmount);
                if (m_CurrentMoneySorting == MoneyEnum2.Descending) c = (a, b) => -c(a, b);
                comparers.Add(c);
            }
            if (m_CurrentInput1Sorting != Input1Enum2.Off)
            {
                Comparison<IndustrialCompanyDTO> c = (a, b) => SumResourceAmounts(a.Input1Resources).CompareTo(SumResourceAmounts(b.Input1Resources));
                if (m_CurrentInput1Sorting == Input1Enum2.Descending) c = (a, b) => -c(a, b);
                comparers.Add(c);
            }
            if (m_CurrentInput2Sorting != Input2Enum2.Off)
            {
                Comparison<IndustrialCompanyDTO> c = (a, b) => SumResourceAmounts(a.Input2Resources).CompareTo(SumResourceAmounts(b.Input2Resources));
                if (m_CurrentInput2Sorting == Input2Enum2.Descending) c = (a, b) => -c(a, b);
                comparers.Add(c);
            }
            if (m_CurrentOutputSorting != OutputEnum2.Off)
            {
                Comparison<IndustrialCompanyDTO> c = (a, b) => SumResourceAmounts(a.OutputResources).CompareTo(SumResourceAmounts(b.OutputResources));
                if (m_CurrentOutputSorting == OutputEnum2.Descending) c = (a, b) => -c(a, b);
                comparers.Add(c);
            }
            if (m_CurrentMaintenanceSorting != MaintenanceEnum2.Off)
            {
                Comparison<IndustrialCompanyDTO> c = (a, b) => SumResourceAmounts(a.MaintenanceResources).CompareTo(SumResourceAmounts(b.MaintenanceResources));
                if (m_CurrentMaintenanceSorting == MaintenanceEnum2.Descending) c = (a, b) => -c(a, b);
                comparers.Add(c);
            }

            if (comparers.Count == 0) return;

            companies.Sort((a, b) =>
            {
                foreach (var cmp in comparers)
                {
                    int r = cmp(a, b);
                    if (r != 0) return r;
                }
                return 0;
            });
        }

        private static int SumResourceAmounts(ResourceInfo[] arr)
        {
            if (arr == null || arr.Length == 0) return 0;
            int s = 0;
            for (int i = 0; i < arr.Length; i++) s += arr[i].Amount;
            return s;
        }

        private string GetFormattedResourceName(Resource resource)
        {
            if (m_ResourceNameCache.TryGetValue(resource, out var name)) return name;
            var resourceName = EconomyUtils.GetName(resource).ToString();
            m_ResourceNameCache[resource] = resourceName;
            return resourceName;
        }

        private string GetResourceIconPath(Resource resource)
        {
            if (resource == Resource.Money) return "Media/Game/Icons/Money.svg";
            if (m_ResourceIconCache.TryGetValue(resource, out var cached)) return cached;
            Entity resourcePrefab = m_ResourceSystem.GetPrefab(resource);
            string icon = m_ImageSystem.GetIconOrGroupIcon(resourcePrefab);
            m_ResourceIconCache[resource] = icon;
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

        public void SetSelectedResource(Resource resource) => SelectedResource = resource;
    }
}