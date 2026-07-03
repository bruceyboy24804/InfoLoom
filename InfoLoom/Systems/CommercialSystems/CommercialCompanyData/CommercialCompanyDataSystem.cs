using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Game.UI;
using Game.Vehicles;
using InfoLoomTwo.Domain.DataDomain.Enums.CompanyPanelEnums;
using ModsCommon.Extensions;
using ModsCommon.Systems;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using DeliveryTruck = Game.Vehicles.DeliveryTruck;
using StorageCompany = Game.Companies.StorageCompany;

namespace InfoLoomTwo.Systems.CommercialSystems.CommercialCompanyDebugData
{
    public partial class CommercialCompanyDataSystem : CommonUISystemBase
    {
        protected override string ModId => InfoLoomMod.Instance.Id;

        // Current sort state (driven by the UI sort triggers)
        private SortingEnum m_CurrentCompanyNameSorting = SortingEnum.Off;
        private SortingEnum m_CurrentServiceUsageSorting = SortingEnum.Off;
        private SortingEnum m_CurrentEmployeesSorting = SortingEnum.Off;
        private SortingEnum m_CurrentEfficiencySorting = SortingEnum.Off;
        private SortingEnum m_CurrentProfitabilitySorting = SortingEnum.Off;
        private SortingEnum m_CurrentResourceAmountSorting = SortingEnum.Off;
        private SortingEnum m_CurrentMoneySorting = SortingEnum.Off;
        private SortingEnum m_CurrentInput1Sorting = SortingEnum.Off;
        private SortingEnum m_CurrentOutputSorting = SortingEnum.Off;
        private SortingEnum m_CurrentMaintenanceSorting = SortingEnum.Off;

        // System dependencies
        private NameSystem m_NameSystem;
        private ImageSystem m_ImageSystem;
        private ResourceSystem m_ResourceSystem;
        private PrefabSystem m_PrefabSystem;
        private EntityQuery m_CommercialCompanyQuery;
        private UIUpdateState _updateState;

        // Bindings owned by this system
        private ValueBinding<bool> _visibleBinding;
        private ValueBindingHelper<CommercialCompanyRecord[]> m_DataBinding;
        private ValueBindingHelper<string[]> m_CompanyNamesBinding;
        private ValueBindingHelper<string> m_SelectedCompanyNameBinding;
        private ValueBindingHelper<string[]> m_Input1ResourcesBinding;
        private ValueBindingHelper<string[]> m_OutputResourcesBinding;
        private ValueBindingHelper<string> m_SelectedInput1Binding;
        private ValueBindingHelper<string> m_SelectedOutputBinding;
        private ValueBindingHelper<SortingEnum> m_NameSortingBinding;
        private ValueBindingHelper<SortingEnum> m_ServiceUsageSortingBinding;
        private ValueBindingHelper<SortingEnum> m_EmployeesSortingBinding;
        private ValueBindingHelper<SortingEnum> m_EfficiencySortingBinding;
        private ValueBindingHelper<SortingEnum> m_ProfitabilitySortingBinding;
        private ValueBindingHelper<SortingEnum> m_ResourceAmountSortingBinding;
        private ValueBindingHelper<SortingEnum> m_MoneySortingBinding;
        private ValueBindingHelper<SortingEnum> m_Input1SortingBinding;
        private ValueBindingHelper<SortingEnum> m_OutputSortingBinding;
        private ValueBindingHelper<SortingEnum> m_MaintenanceSortingBinding;

        // Built data (all companies, before filter/sort)
        private CommercialCompanyRecord[] m_AllRecords = Array.Empty<CommercialCompanyRecord>();

        // Managed caches
        private Dictionary<Entity, string> m_CompanyNameCache;
        private Dictionary<Resource, string> m_ResourceNameCache;
        private Dictionary<Resource, string> m_ResourceIconCache;
        private bool m_CacheInitialized;
        private int m_LastCompanyCount;
        private bool m_ForceCompanyNameCacheUpdate;

        // Job process resources (written by the burst job)
        private NativeValue<Resource> m_ProcessInput1;
        private NativeValue<Resource> m_ProcessInput2;
        private NativeValue<Resource> m_ProcessOutput;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_NameSystem = World.GetOrCreateSystemManaged<NameSystem>();
            m_ImageSystem = World.GetOrCreateSystemManaged<ImageSystem>();
            m_ResourceSystem = World.GetOrCreateSystemManaged<ResourceSystem>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();

            m_CommercialCompanyQuery = SystemAPI.QueryBuilder()
                .WithAll<CommercialCompany, PropertyRenter>()
                .WithNone<StorageCompany, MovingAway>()
                .Build();

            m_CompanyNameCache = new Dictionary<Entity, string>();
            m_ResourceNameCache = new Dictionary<Resource, string>();
            m_ResourceIconCache = new Dictionary<Resource, string>();
            m_LastCompanyCount = 0;
            m_ForceCompanyNameCacheUpdate = false;

            m_ProcessInput1 = new NativeValue<Resource>(Allocator.Persistent);
            m_ProcessInput2 = new NativeValue<Resource>(Allocator.Persistent);
            m_ProcessOutput = new NativeValue<Resource>(Allocator.Persistent);

            // Visibility binding (raw, no BINDING:/TRIGGER: prefix, matching the menu trigger)
            _visibleBinding = new ValueBinding<bool>(ModId, "CommercialCompanyDebugOpen", false);
            AddBinding(_visibleBinding);
            AddBinding(new TriggerBinding<bool>(ModId, "CommercialCompanyDebugOpen", OnVisibilityChanged));

            // Data + filter list bindings
            m_DataBinding = CreateBinding("CommercialCompanyDebugData", Array.Empty<CommercialCompanyRecord>());
            m_CompanyNamesBinding = CreateBinding("listOfCommercialCompanyNames", Array.Empty<string>());
            m_Input1ResourcesBinding = CreateBinding("listOfCommercialInput1Resources", Array.Empty<string>());
            m_OutputResourcesBinding = CreateBinding("listOfCommercialOutputResources", Array.Empty<string>());

            // Filter selection bindings (two-way)
            m_SelectedCompanyNameBinding = CreateGenericBinding("CommercialCompanyName", "All Companies", OnSelectedCompanyNameChanged);
            m_SelectedInput1Binding = CreateGenericBinding("CommercialInput1Resource", "All", OnSelectedInput1Changed);
            m_SelectedOutputBinding = CreateGenericBinding("CommercialOutputResource", "All", OnSelectedOutputChanged);

            // Sorting bindings (two-way)
            m_NameSortingBinding = CreateGenericBinding("CommercialNameSorting", SortingEnum.Off, v => { m_CurrentCompanyNameSorting = v; RefreshDisplayed(); });
            m_ServiceUsageSortingBinding = CreateGenericBinding("CommercialServiceUsageSorting", SortingEnum.Off, v => { m_CurrentServiceUsageSorting = v; RefreshDisplayed(); });
            m_EmployeesSortingBinding = CreateGenericBinding("CommercialEmployeesSorting", SortingEnum.Off, v => { m_CurrentEmployeesSorting = v; RefreshDisplayed(); });
            m_EfficiencySortingBinding = CreateGenericBinding("CommercialEfficiencySorting", SortingEnum.Off, v => { m_CurrentEfficiencySorting = v; RefreshDisplayed(); });
            m_ProfitabilitySortingBinding = CreateGenericBinding("CommercialProfitabilitySorting", SortingEnum.Off, v => { m_CurrentProfitabilitySorting = v; RefreshDisplayed(); });
            m_ResourceAmountSortingBinding = CreateGenericBinding("CommercialResourceAmountSorting", SortingEnum.Off, v => { m_CurrentResourceAmountSorting = v; RefreshDisplayed(); });
            m_MoneySortingBinding = CreateGenericBinding("CommercialMoneySorting", SortingEnum.Off, v => { m_CurrentMoneySorting = v; RefreshDisplayed(); });
            m_Input1SortingBinding = CreateGenericBinding("CommercialInput1Sorting", SortingEnum.Off, v => { m_CurrentInput1Sorting = v; RefreshDisplayed(); });
            m_OutputSortingBinding = CreateGenericBinding("CommercialOutputSorting", SortingEnum.Off, v => { m_CurrentOutputSorting = v; RefreshDisplayed(); });
            m_MaintenanceSortingBinding = CreateGenericBinding("CommercialMaintenanceSorting", SortingEnum.Off, v => { m_CurrentMaintenanceSorting = v; RefreshDisplayed(); });

            _updateState = UIUpdateState.Create(World, 512);
        }

        protected override void OnDestroy()
        {
            m_ProcessInput1.Dispose();
            m_ProcessInput2.Dispose();
            m_ProcessOutput.Dispose();
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (_visibleBinding.value && _updateState.Advance())
            {
                RebuildData();
            }

            // Flushes any dirty bindings (data/sort/filter) to the UI.
            base.OnUpdate();
        }

        private void OnVisibilityChanged(bool open)
        {
            _visibleBinding.Update(open);
            if (!open)
                return;

            // Reset filters to their initial state when the panel opens.
            m_SelectedInput1Binding.Value = "All";
            m_SelectedOutputBinding.Value = "All";
            m_SelectedCompanyNameBinding.Value = "All Companies";

            RebuildData();
        }

        private void OnSelectedCompanyNameChanged(string companyName)
        {
            RefreshDisplayed();
        }

        private void OnSelectedInput1Changed(string resourceName)
        {
            if (!string.IsNullOrEmpty(resourceName) && resourceName != "All")
                m_SelectedOutputBinding.Value = "All";
            RefreshDisplayed();
        }

        private void OnSelectedOutputChanged(string resourceName)
        {
            if (!string.IsNullOrEmpty(resourceName) && resourceName != "All")
                m_SelectedInput1Binding.Value = "All";
            RefreshDisplayed();
        }

        // -- Data build -------------------------------------------------------

        private void RebuildData()
        {
            InitializeCaches();
            UpdateCompanyNameCacheIfNeeded();

            m_ProcessInput1.value = Resource.NoResource;
            m_ProcessInput2.value = Resource.NoResource;
            m_ProcessOutput.value = Resource.NoResource;

            int estimatedCount = m_CommercialCompanyQuery.CalculateEntityCount();
            var jobResults = new NativeList<CommercialCompanyJobData>(estimatedCount, Allocator.TempJob);

            var job = new ProcessCommercialCompaniesJob
            {
                EntityType = SystemAPI.GetEntityTypeHandle(),
                CompanyDataType = SystemAPI.GetComponentTypeHandle<Game.Companies.CompanyData>(true),
                WorkProviderType = SystemAPI.GetComponentTypeHandle<WorkProvider>(true),
                PrefabRefType = SystemAPI.GetComponentTypeHandle<PrefabRef>(true),
                ServiceAvailableType = SystemAPI.GetComponentTypeHandle<ServiceAvailable>(true),
                PropertyRenterType = SystemAPI.GetComponentTypeHandle<PropertyRenter>(true),
                ProfitabilityType = SystemAPI.GetComponentTypeHandle<Profitability>(true),
                EmployeeBufferType = SystemAPI.GetBufferTypeHandle<Employee>(true),
                OwnedVehicleBufferType = SystemAPI.GetBufferTypeHandle<OwnedVehicle>(true),
                ResourcesBufferType = SystemAPI.GetBufferTypeHandle<Resources>(true),
                CompanyStatisticDataLookup = GetComponentLookup<CompanyStatisticData>(true),
                ServiceCompanyDataLookup = GetComponentLookup<ServiceCompanyData>(true),
                TransportCompanyDataLookup = GetComponentLookup<TransportCompanyData>(true),
                IndustrialProcessDataLookup = GetComponentLookup<IndustrialProcessData>(true),
                ResourceDataLookup = GetComponentLookup<ResourceData>(true),
                CitizenLookup = GetComponentLookup<Citizen>(true),
                DeliveryTruckLookup = GetComponentLookup<DeliveryTruck>(true),
                EfficiencyLookup = GetBufferLookup<Efficiency>(true),
                EconomyParams = SystemAPI.GetSingleton<EconomyParameterData>(),
                ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
                ResultWriter = jobResults.AsParallelWriter(),
                ProcessInput1 = m_ProcessInput1,
                ProcessInput2 = m_ProcessInput2,
                ProcessOutput = m_ProcessOutput,
            };
            Dependency = JobChunkExtensions.ScheduleParallel(job, m_CommercialCompanyQuery, Dependency);
            Dependency.Complete();

            // Build the managed records inline (names/icons/resources/stats).
            var resourcesLookup = GetBufferLookup<Resources>(true);
            var efficiencyLookup = GetBufferLookup<Efficiency>(true);
            var propertyRenterLookup = GetComponentLookup<PropertyRenter>(true);
            var prefabRefLookup = GetComponentLookup<PrefabRef>(true);
            var processLookup = GetComponentLookup<IndustrialProcessData>(true);
            var statisticsLookup = GetComponentLookup<CompanyStatisticData>(true);

            var records = new CommercialCompanyRecord[jobResults.Length];
            for (int i = 0; i < jobResults.Length; i++)
            {
                records[i] = BuildRecord(jobResults[i], resourcesLookup, efficiencyLookup,
                    propertyRenterLookup, prefabRefLookup, processLookup, statisticsLookup);
            }
            jobResults.Dispose();

            m_AllRecords = records;

            // Refresh the resource filter dropdowns from the full set.
            
            m_Input1ResourcesBinding.Value = m_ProcessInput1.value == Resource.NoResource
                    ? new string[0]: ResourceMaskToNames(m_ProcessInput1.value);
            m_OutputResourcesBinding.Value = m_ProcessOutput.value == Resource.NoResource
                    ? new string[0]: ResourceMaskToNames(m_ProcessOutput.value);

            RefreshDisplayed();
        }

        // Applies the active resource + company-name filters and current sort, then pushes the list.
        private void RefreshDisplayed()
        {
            if (m_AllRecords == null)
                return;

            string input1 = m_SelectedInput1Binding.Value ?? "All";
            string output = m_SelectedOutputBinding.Value ?? "All";

            IEnumerable<CommercialCompanyRecord> resourceFilteredQuery = m_AllRecords;
            if (!string.IsNullOrEmpty(input1) && input1 != "All")
                resourceFilteredQuery = m_AllRecords.Where(c => ContainsResource(c.Input1Resources, input1));
            else if (!string.IsNullOrEmpty(output) && output != "All")
                resourceFilteredQuery = m_AllRecords.Where(c => ContainsResource(c.OutputResources, output));

            var resourceFiltered = resourceFilteredQuery as CommercialCompanyRecord[] ?? resourceFilteredQuery.ToArray();

            // Company-name dropdown reflects the resource-filtered set.
            UpdateCompanyNameList(resourceFiltered);

            string companyName = m_SelectedCompanyNameBinding.Value ?? "All Companies";
            IEnumerable<CommercialCompanyRecord> displayed = resourceFiltered;
            if (!string.IsNullOrEmpty(companyName) && companyName != "All Companies")
                displayed = resourceFiltered.Where(c => c.CompanyName == companyName);

            var list = displayed.ToList();
            ApplySorts(list);
            m_DataBinding.Value = list.ToArray();
        }

        private static bool ContainsResource(ResourceInfo[] resources, string resourceName)
        {
            if (resources == null)
                return false;
            for (int i = 0; i < resources.Length; i++)
                if (resources[i].ResourceName == resourceName)
                    return true;
            return false;
        }

        private void UpdateCompanyNameList(CommercialCompanyRecord[] companies)
        {
            var uniqueCompanyNames = new HashSet<string>();
            foreach (var company in companies)
            {
                if (!string.IsNullOrEmpty(company.CompanyName))
                    uniqueCompanyNames.Add(company.CompanyName);
            }

            var sortedNames = uniqueCompanyNames.OrderBy(name => name).ToList();
            sortedNames.Insert(0, "All Companies");
            m_CompanyNamesBinding.Value = sortedNames.ToArray();
        }

        // Decomposes a resource bitmask into the display names (matching the filter comparison),
        // sorted, with "All" first — used to populate the resource filter dropdowns.
        private string[] ResourceMaskToNames(Resource mask)
        {
            var names = new List<string>();
            var iterator = ResourceIterator.GetIterator();
            while (iterator.Next())
            {
                var res = iterator.resource;
                if (res == Resource.NoResource || (mask & res) == 0)
                    continue;
                names.Add(GetCachedResourceName(res));
            }

            names.Sort();
            names.Insert(0, "All");
            return names.ToArray();
        }

        // -- Sorting ----------------------------------------------------------

        private void ApplySorts(List<CommercialCompanyRecord> companies)
        {
            IOrderedEnumerable<CommercialCompanyRecord> ordered = null;

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

            if (m_CurrentServiceUsageSorting == SortingEnum.Ascending)
                ordered = ordered == null ? companies.OrderBy(x => x.ServiceAvailable) : ordered.ThenBy(x => x.ServiceAvailable);
            else if (m_CurrentServiceUsageSorting == SortingEnum.Descending)
                ordered = ordered == null ? companies.OrderByDescending(x => x.ServiceAvailable) : ordered.ThenByDescending(x => x.ServiceAvailable);

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

        // -- Record construction ---------------------------------------------

        private CommercialCompanyRecord BuildRecord(CommercialCompanyJobData jobData,
            BufferLookup<Resources> resourcesLookup, BufferLookup<Efficiency> efficiencyLookup,
            ComponentLookup<PropertyRenter> propertyRenterLookup, ComponentLookup<PrefabRef> prefabRefLookup,
            ComponentLookup<IndustrialProcessData> processLookup, ComponentLookup<CompanyStatisticData> statisticsLookup)
        {
            Resource processInput1 = Resource.NoResource;
            Resource processInput2 = Resource.NoResource;
            Resource processOutput = Resource.NoResource;
            if (prefabRefLookup.TryGetComponent(jobData.EntityId, out var prefabRef) &&
                processLookup.TryGetComponent(prefabRef.m_Prefab, out var proc))
            {
                processInput1 = proc.m_Input1.m_Resource;
                processInput2 = proc.m_Input2.m_Resource;
                processOutput = proc.m_Output.m_Resource;
            }

            ClassifyResources(jobData.EntityId, resourcesLookup,
                processInput1, processInput2, processOutput,
                out var input1Resources, out _, out var outputResources, out var maintenanceResources, out int moneyAmount);
            var (processList, outputResourceName, outputResourceIcon) = GetProcessInfo(jobData.EntityId, prefabRefLookup, processLookup);

            EfficiencyFactorInfo[] factors = Array.Empty<EfficiencyFactorInfo>();
            if (propertyRenterLookup.HasComponent(jobData.EntityId))
            {
                var targetEntity = propertyRenterLookup[jobData.EntityId].m_Property;
                factors = GetEfficiencyFactors(targetEntity, efficiencyLookup);
            }

            string companyNameString = m_CompanyNameCache.TryGetValue(jobData.Brand, out var fixedName)
                ? fixedName
                : "Unknown Company";

            int income = 0, worth = 0, profit = 0, wagePaid = 0, rentPaid = 0, electricityPaid = 0;
            int waterPaid = 0, sewagePaid = 0, garbagePaid = 0, taxPaid = 0, resourcesBoughtPaid = 0;
            int currentCustomers = 0, monthlyCustomers = 0;

            if (jobData.HasStatistics && statisticsLookup.TryGetComponent(jobData.EntityId, out var statistics))
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

            return new CommercialCompanyRecord
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
                Input1Resource = processInput1,
                OutputResource = processOutput,
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

            var input1Array = new ResourceInfo[bufferLength];
            var input2Array = new ResourceInfo[bufferLength];
            var outputArray = new ResourceInfo[bufferLength];
            var maintenanceArray = new ResourceInfo[bufferLength];

            int in1Count = 0, in2Count = 0, outCount = 0, maintCount = 0;

            for (int r = 0; r < bufferLength; r++)
            {
                var resource = buffer[r];
                if (resource.m_Resource == Resource.Money)
                {
                    money = resource.m_Amount;
                    continue;
                }

                var info = new ResourceInfo(GetCachedResourceName(resource.m_Resource), resource.m_Amount, GetCachedResourceIcon(resource.m_Resource));

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

            input1List = in1Count == 0 ? Array.Empty<ResourceInfo>() : TrimArray(input1Array, in1Count);
            input2List = in2Count == 0 ? Array.Empty<ResourceInfo>() : TrimArray(input2Array, in2Count);
            outputList = outCount == 0 ? Array.Empty<ResourceInfo>() : TrimArray(outputArray, outCount);
            maintenanceList = maintCount == 0 ? Array.Empty<ResourceInfo>() : TrimArray(maintenanceArray, maintCount);
        }

        private static ResourceInfo[] TrimArray(ResourceInfo[] source, int length)
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

        // -- Caches -----------------------------------------------------------

        private void InitializeCaches()
        {
            if (m_CacheInitialized)
                return;

            for (int i = 0; i < 64; i++)
            {
                var resource = (Resource)i;
                if (resource == Resource.NoResource) continue;

                if (!m_ResourceNameCache.ContainsKey(resource))
                    m_ResourceNameCache[resource] = resource.ToString();

                if (!m_ResourceIconCache.ContainsKey(resource))
                    m_ResourceIconCache[resource] = ResolveResourceIcon(resource);
            }

            m_CacheInitialized = true;
        }

        private void UpdateCompanyNameCacheIfNeeded()
        {
            int currentCount = m_CommercialCompanyQuery.CalculateEntityCount();
            if (!m_ForceCompanyNameCacheUpdate && currentCount == m_LastCompanyCount)
                return;

            var entities = m_CommercialCompanyQuery.ToEntityArray(Allocator.Temp);
            var companyDataLookup = GetComponentLookup<Game.Companies.CompanyData>(true);

            try
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    if (companyDataLookup.TryGetComponent(entities[i], out var companyData) &&
                        !m_CompanyNameCache.ContainsKey(companyData.m_Brand))
                    {
                        m_CompanyNameCache[companyData.m_Brand] = m_NameSystem.GetRenderedLabelName(companyData.m_Brand);
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

        private string ResolveResourceIcon(Resource resource)
        {
            if (resource == Resource.Money)
                return "Media/Game/Icons/Money.svg";
            Entity resourcePrefab = m_ResourceSystem.GetPrefab(resource);
            return m_ImageSystem.GetIconOrGroupIcon(resourcePrefab);
        }

        private string GetCachedResourceName(Resource resource)
        {
            if (!m_ResourceNameCache.TryGetValue(resource, out var name))
            {
                name = resource.ToString();
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
                icon = ResolveResourceIcon(resource);
                m_ResourceIconCache[resource] = icon;
            }
            return icon;
        }
    }
}
