using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Collections;
using Colossal.UI.Binding;
using Game.Agents;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.Companies;
using Game.Economy;
using Game.Objects;
using Game.Prefabs;
using Game.UI;
using Game.Vehicles;
using InfoLoomTwo.Domain.DataDomain.Enums.CompanyPanelEnums;
using ModsCommon.Extensions;
using ModsCommon.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using DeliveryTruck = Game.Vehicles.DeliveryTruck;
using ExtractorCompany = Game.Companies.ExtractorCompany;
using StorageCompany = Game.Companies.StorageCompany;
using SubArea = Game.Areas.SubArea;
using ResourceInfo = InfoLoomTwo.Systems.IndustrialSystems.IndustrialCompanyData.IndustrialCompanySystem.ResourceInfo;
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
    /// Serializable per-company record sent to the UI. Replaces the old reflection-serialized
    /// IndustrialCompanyDTO with an explicit <see cref="IJsonWritable"/> payload. Field names match
    /// the React IndustrialCompanyDebug interface.
    /// </summary>
    public sealed class IndustrialCompanyRecord : IJsonWritable
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
        public IndustrialCompanySystem.EfficiencyFactorInfo[] Factors;
        public float Profitability;
        public int LastTotalWorth;
        public int TotalWages;
        public int ProductionPerDay;
        public float EfficiencyValue;
        public string OutputResourceName;
        public bool IsExtractor;
        public string ResourceIcon;
        public string ResourceName;
        public int MoneyAmount;
        public ResourceInfo[] Input1Resources;
        public ResourceInfo[] Input2Resources;
        public ResourceInfo[] OutputResources;
        public ResourceInfo[] MaintenanceResources;
        // Raw process resources (not serialized) — used to build the filter dropdowns.
        public Resource Input1Resource;
        public Resource Input2Resource;
        public Resource OutputResource;
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

        public void Write(IJsonWriter writer)
        {
            writer.TypeBegin(typeof(IndustrialCompanyRecord).FullName);
            writer.PropertyName("EntityId"); writer.Write(EntityId);
            writer.PropertyName("CompanyName"); writer.Write(CompanyName ?? string.Empty);
            writer.PropertyName("TotalEmployees"); writer.Write(TotalEmployees);
            writer.PropertyName("MaxWorkers"); writer.Write(MaxWorkers);
            writer.PropertyName("VehicleCount"); writer.Write(VehicleCount);
            writer.PropertyName("VehicleCapacity"); writer.Write(VehicleCapacity);
            writer.PropertyName("ResourceAmount"); writer.Write(ResourceAmount);
            WriteItems(writer, "ProcessResources", ProcessResources);
            writer.PropertyName("TotalEfficiency"); writer.Write(TotalEfficiency);
            WriteItems(writer, "Factors", Factors);
            writer.PropertyName("Profitability"); writer.Write(Profitability);
            writer.PropertyName("LastTotalWorth"); writer.Write(LastTotalWorth);
            writer.PropertyName("TotalWages"); writer.Write(TotalWages);
            writer.PropertyName("ProductionPerDay"); writer.Write(ProductionPerDay);
            writer.PropertyName("EfficiencyValue"); writer.Write(EfficiencyValue);
            writer.PropertyName("OutputResourceName"); writer.Write(OutputResourceName ?? string.Empty);
            writer.PropertyName("IsExtractor"); writer.Write(IsExtractor);
            writer.PropertyName("ResourceIcon"); writer.Write(ResourceIcon ?? string.Empty);
            writer.PropertyName("ResourceName"); writer.Write(ResourceName ?? string.Empty);
            writer.PropertyName("MoneyAmount"); writer.Write(MoneyAmount);
            WriteItems(writer, "Input1Resources", Input1Resources);
            WriteItems(writer, "Input2Resources", Input2Resources);
            WriteItems(writer, "OutputResources", OutputResources);
            WriteItems(writer, "MaintenanceResources", MaintenanceResources);
            WriteItems(writer, "Resources", Array.Empty<ResourceInfo>());
            writer.PropertyName("Income"); writer.Write(Income);
            writer.PropertyName("Worth"); writer.Write(Worth);
            writer.PropertyName("Profit"); writer.Write(Profit);
            writer.PropertyName("WagePaid"); writer.Write(WagePaid);
            writer.PropertyName("RentPaid"); writer.Write(RentPaid);
            writer.PropertyName("ElectricityPaid"); writer.Write(ElectricityPaid);
            writer.PropertyName("WaterPaid"); writer.Write(WaterPaid);
            writer.PropertyName("SewagePaid"); writer.Write(SewagePaid);
            writer.PropertyName("GarbagePaid"); writer.Write(GarbagePaid);
            writer.PropertyName("TaxPaid"); writer.Write(TaxPaid);
            writer.PropertyName("ResourcesBoughtPaid"); writer.Write(ResourcesBoughtPaid);
            writer.PropertyName("CurrentCustomers"); writer.Write(CurrentCustomers);
            writer.PropertyName("MonthlyCustomers"); writer.Write(MonthlyCustomers);
            writer.TypeEnd();
        }

        private static void WriteItems<TItem>(IJsonWriter writer, string name, TItem[] items)
            where TItem : IJsonWritable
        {
            writer.PropertyName(name);
            int length = items?.Length ?? 0;
            writer.ArrayBegin(length);
            for (int i = 0; i < length; i++)
                items[i].Write(writer);
            writer.ArrayEnd();
        }
    }

    /// <summary>
    /// Collects industrial company data and owns the UI bindings for the panel (data list,
    /// per-column sorting, company-name/resource filters, visibility). A Burst job gathers the
    /// cheap per-entity numbers; the managed name/icon/resource conversion is done inline in
    /// OnUpdate (no DTO struct, no lazy caches). Mirrors CommercialCompanyDataSystem.
    /// </summary>
    public partial class IndustrialCompanySystem : CommonUISystemBase
    {
        protected override string ModId => InfoLoomMod.Instance.Id;

        // Current sort state (driven by the UI sort triggers)
        private SortingEnum m_CurrentIndexSorting = SortingEnum.Off;
        private SortingEnum m_CurrentCompanyNameSorting = SortingEnum.Off;
        private SortingEnum m_CurrentEmployeesSorting = SortingEnum.Off;
        private SortingEnum m_CurrentEfficiencySorting = SortingEnum.Off;
        private SortingEnum m_CurrentProfitabilitySorting = SortingEnum.Off;
        private SortingEnum m_CurrentResourceAmountSorting = SortingEnum.Off;
        private SortingEnum m_CurrentMoneySorting = SortingEnum.Off;
        private SortingEnum m_CurrentInput1Sorting = SortingEnum.Off;
        private SortingEnum m_CurrentInput2Sorting = SortingEnum.Off;
        private SortingEnum m_CurrentOutputSorting = SortingEnum.Off;
        private SortingEnum m_CurrentMaintenanceSorting = SortingEnum.Off;

        // System dependencies
        private NameSystem m_NameSystem;
        private ImageSystem m_ImageSystem;
        private ResourceSystem m_ResourceSystem;
        private PrefabSystem m_PrefabSystem;
        private EntityQuery m_IndustrialCompanyQuery;
        private UIUpdateState _updateState;

        // Bindings owned by this system
        private ValueBinding<bool> _visibleBinding;
        private ValueBindingHelper<IndustrialCompanyRecord[]> m_DataBinding;
        private ValueBindingHelper<string[]> m_CompanyNamesBinding;
        private ValueBindingHelper<string> m_SelectedCompanyNameBinding;
        private ValueBindingHelper<string[]> m_Input1ResourcesBinding;
        private ValueBindingHelper<string[]> m_Input2ResourcesBinding;
        private ValueBindingHelper<string[]> m_OutputResourcesBinding;
        private ValueBindingHelper<string> m_SelectedInput1Binding;
        private ValueBindingHelper<string> m_SelectedInput2Binding;
        private ValueBindingHelper<string> m_SelectedOutputBinding;
        private ValueBindingHelper<SortingEnum> m_IndexSortingBinding;
        private ValueBindingHelper<SortingEnum> m_NameSortingBinding;
        private ValueBindingHelper<SortingEnum> m_EmployeesSortingBinding;
        private ValueBindingHelper<SortingEnum> m_EfficiencySortingBinding;
        private ValueBindingHelper<SortingEnum> m_ProfitabilitySortingBinding;
        private ValueBindingHelper<SortingEnum> m_ResourceAmountSortingBinding;
        private ValueBindingHelper<SortingEnum> m_MoneySortingBinding;
        private ValueBindingHelper<SortingEnum> m_Input1SortingBinding;
        private ValueBindingHelper<SortingEnum> m_Input2SortingBinding;
        private ValueBindingHelper<SortingEnum> m_OutputSortingBinding;
        private ValueBindingHelper<SortingEnum> m_MaintenanceSortingBinding;

        // Built data (all companies, before filter/sort)
        private IndustrialCompanyRecord[] m_AllRecords = Array.Empty<IndustrialCompanyRecord>();

        // Managed caches
        private Dictionary<Entity, string> m_CompanyNameCache;
        private Dictionary<Resource, string> m_ResourceNameCache;
        private Dictionary<Resource, string> m_ResourceIconCache;
        private bool m_CacheInitialized;
        private int m_LastCompanyCount;
        private bool m_ForceCompanyNameCacheUpdate;
        
        private NativeValue<Resource> m_Input1Resources;
        private NativeValue<Resource> m_Input2Resources;
        private NativeValue<Resource> m_OutputResources;

        protected override void OnCreate()
        {
            base.OnCreate();

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

            m_Input1Resources = new NativeValue<Resource>(Allocator.Persistent);
            m_Input2Resources = new NativeValue<Resource>(Allocator.Persistent);
            m_OutputResources = new NativeValue<Resource>(Allocator.Persistent);

            // Visibility binding (raw, no BINDING:/TRIGGER: prefix, matching the menu trigger)
            _visibleBinding = new ValueBinding<bool>(ModId, "IndustrialCompanyDebugOpen", false);
            AddBinding(_visibleBinding);
            AddBinding(new TriggerBinding<bool>(ModId, "IndustrialCompanyDebugOpen", OnVisibilityChanged));

            // Data + filter list bindings
            m_DataBinding = CreateBinding("IndustrialCompanyDebugData", Array.Empty<IndustrialCompanyRecord>());
            m_CompanyNamesBinding = CreateBinding("listOfCompanyNames", Array.Empty<string>());
            m_Input1ResourcesBinding = CreateBinding("listOfInput1Resources", Array.Empty<string>());
            m_Input2ResourcesBinding = CreateBinding("listOfInput2Resources", Array.Empty<string>());
            m_OutputResourcesBinding = CreateBinding("listOfOutputResources", Array.Empty<string>());

            // Filter selection bindings (two-way, separate setter keys to match the existing UI)
            m_SelectedCompanyNameBinding = CreateGenericBinding("selectedCompanyName",  "All Companies", OnSelectedCompanyNameChanged);
            m_SelectedInput1Binding = CreateGenericBinding("selectedInput1Resource",  "All", OnSelectedInput1Changed);
            m_SelectedInput2Binding = CreateGenericBinding("selectedInput2Resource", "All", OnSelectedInput2Changed);
            m_SelectedOutputBinding = CreateGenericBinding("selectedOutputResource", "All", OnSelectedOutputChanged);

            // Sorting bindings (two-way, separate setter keys to match the existing UI)
            m_IndexSortingBinding = CreateGenericBinding("IndustrialIndexSorting", "SetIndustrialIndexSorting", SortingEnum.Off, v => { m_CurrentIndexSorting = v; RefreshDisplayed(); });
            m_NameSortingBinding = CreateGenericBinding("IndustrialNameSorting", "SetIndustrialNameSorting", SortingEnum.Off, v => { m_CurrentCompanyNameSorting = v; RefreshDisplayed(); });
            m_EmployeesSortingBinding = CreateGenericBinding("IndustrialEmployeesSorting", "SetIndustrialEmployeesSorting", SortingEnum.Off, v => { m_CurrentEmployeesSorting = v; RefreshDisplayed(); });
            m_EfficiencySortingBinding = CreateGenericBinding("IndustrialEfficiencySorting", "SetIndustrialEfficiencySorting", SortingEnum.Off, v => { m_CurrentEfficiencySorting = v; RefreshDisplayed(); });
            m_ProfitabilitySortingBinding = CreateGenericBinding("IndustrialProfitabilitySorting", "SetIndustrialProfitabilitySorting", SortingEnum.Off, v => { m_CurrentProfitabilitySorting = v; RefreshDisplayed(); });
            m_ResourceAmountSortingBinding = CreateGenericBinding("IndustrialResourceAmountSorting", "SetIndustrialResourceAmountSorting", SortingEnum.Off, v => { m_CurrentResourceAmountSorting = v; RefreshDisplayed(); });
            m_MoneySortingBinding = CreateGenericBinding("IndustrialMoneySorting", "SetIndustrialMoneySorting", SortingEnum.Off, v => { m_CurrentMoneySorting = v; RefreshDisplayed(); });
            m_Input1SortingBinding = CreateGenericBinding("IndustrialInput1Sorting", "SetIndustrialInput1Sorting", SortingEnum.Off, v => { m_CurrentInput1Sorting = v; RefreshDisplayed(); });
            m_Input2SortingBinding = CreateGenericBinding("IndustrialInput2Sorting", "SetIndustrialInput2Sorting", SortingEnum.Off, v => { m_CurrentInput2Sorting = v; RefreshDisplayed(); });
            m_OutputSortingBinding = CreateGenericBinding("IndustrialOutputSorting", "SetIndustrialOutputSorting", SortingEnum.Off, v => { m_CurrentOutputSorting = v; RefreshDisplayed(); });
            m_MaintenanceSortingBinding = CreateGenericBinding("IndustrialMaintenanceSorting", "SetIndustrialMaintenanceSorting", SortingEnum.Off, v => { m_CurrentMaintenanceSorting = v; RefreshDisplayed(); });

            _updateState = UIUpdateState.Create(World, 512);
        }
        protected override void OnDestroy()
        {
            m_Input1Resources.Dispose();
            m_Input2Resources.Dispose();
            m_OutputResources.Dispose();
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

            m_SelectedInput1Binding.Value = "All";
            m_SelectedInput2Binding.Value = "All";
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
            {
                m_SelectedInput2Binding.Value = "All";
                m_SelectedOutputBinding.Value = "All";
            }
            RefreshDisplayed();
        }

        private void OnSelectedInput2Changed(string resourceName)
        {
            if (!string.IsNullOrEmpty(resourceName) && resourceName != "All")
            {
                m_SelectedInput1Binding.Value = "All";
                m_SelectedOutputBinding.Value = "All";
            }
            RefreshDisplayed();
        }

        private void OnSelectedOutputChanged(string resourceName)
        {
            if (!string.IsNullOrEmpty(resourceName) && resourceName != "All")
            {
                m_SelectedInput1Binding.Value = "All";
                m_SelectedInput2Binding.Value = "All";
            }
            RefreshDisplayed();
        }

        // -- Data build -------------------------------------------------------

        private void RebuildData()
        {
            InitializeCaches();
            UpdateCompanyNameCacheIfNeeded();
            m_Input1Resources.value = Resource.NoResource;
            m_Input2Resources.value = Resource.NoResource;
            m_OutputResources.value = Resource.NoResource;
            int estimatedCount = m_IndustrialCompanyQuery.CalculateEntityCount();
            var jobResults = new NativeList<IndustrialCompanyJobData>(Math.Max(1, estimatedCount), Allocator.TempJob);

            var job = new ProcessIndustrialCompaniesJob
            {
                EntityType = SystemAPI.GetEntityTypeHandle(),
                CompanyDataType = SystemAPI.GetComponentTypeHandle<CompanyData>(true),
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
                ExtractorLookup = SystemAPI.GetComponentLookup<Extractor>(true),
                PrefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(true),
                ExtractorAreaDataLookup = SystemAPI.GetComponentLookup<ExtractorAreaData>(true),
                EfficiencyLookup = SystemAPI.GetBufferLookup<Efficiency>(true),
                SubAreaLookup = SystemAPI.GetBufferLookup<SubArea>(true),
                EconomyParams = SystemAPI.GetSingleton<EconomyParameterData>(),
                ExtractorParams = SystemAPI.GetSingleton<ExtractorParameterData>(),
                ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
                ResultWriter = jobResults.AsParallelWriter(),
                Input1Resource = m_Input1Resources,
                Input2Resource = m_Input2Resources,
                OutputResource = m_OutputResources
            };
            Dependency = job.ScheduleParallel(m_IndustrialCompanyQuery, Dependency);
            Dependency.Complete();

            // Build the managed records inline (names/icons/resources/stats).
            var resourcesLookup = GetBufferLookup<Resources>(true);
            var efficiencyLookup = GetBufferLookup<Efficiency>(true);
            var propertyRenterLookup = GetComponentLookup<PropertyRenter>(true);
            var prefabRefLookup = GetComponentLookup<PrefabRef>(true);
            var processLookup = GetComponentLookup<IndustrialProcessData>(true);
            var statisticsLookup = GetComponentLookup<CompanyStatisticData>(true);

            var records = new IndustrialCompanyRecord[jobResults.Length];
            for (int i = 0; i < jobResults.Length; i++)
            {
                records[i] = BuildRecord(jobResults[i], resourcesLookup, efficiencyLookup,
                    propertyRenterLookup, prefabRefLookup, processLookup, statisticsLookup);
            }
            jobResults.Dispose();

            m_AllRecords = records;

            
            m_Input1ResourcesBinding.Value = m_Input1Resources.value == Resource.NoResource
                    ? new string[0]: ResourceMaskToNames(m_Input1Resources.value);
            m_Input2ResourcesBinding.Value = m_Input2Resources.value == Resource.NoResource
                    ? new string[0]: ResourceMaskToNames(m_Input2Resources.value);
            m_OutputResourcesBinding.Value = m_OutputResources.value == Resource.NoResource
                    ? new string[0]: ResourceMaskToNames(m_OutputResources.value);

            RefreshDisplayed();
        }

        // Applies the active resource + company-name filters and current sort, then pushes the list.
        private void RefreshDisplayed()
        {
            if (m_AllRecords == null)
                return;

            string input1 = m_SelectedInput1Binding.Value ?? "All";
            string input2 = m_SelectedInput2Binding.Value ?? "All";
            string output = m_SelectedOutputBinding.Value ?? "All";

            IEnumerable<IndustrialCompanyRecord> resourceFilteredQuery = m_AllRecords;
            if (!string.IsNullOrEmpty(input1) && input1 != "All")
                resourceFilteredQuery = m_AllRecords.Where(c => ContainsResource(c.Input1Resources, input1));
            else if (!string.IsNullOrEmpty(input2) && input2 != "All")
                resourceFilteredQuery = m_AllRecords.Where(c => ContainsResource(c.Input2Resources, input2));
            else if (!string.IsNullOrEmpty(output) && output != "All")
                resourceFilteredQuery = m_AllRecords.Where(c => ContainsResource(c.OutputResources, output));

            var resourceFiltered = resourceFilteredQuery as IndustrialCompanyRecord[] ?? resourceFilteredQuery.ToArray();

            UpdateCompanyNameList(resourceFiltered);

            string companyName = m_SelectedCompanyNameBinding.Value ?? "All Companies";
            IEnumerable<IndustrialCompanyRecord> displayed = resourceFiltered;
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

        private void UpdateCompanyNameList(IndustrialCompanyRecord[] companies)
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

        private void ApplySorts(List<IndustrialCompanyRecord> companies)
        {
            IOrderedEnumerable<IndustrialCompanyRecord> ordered = null;

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

        // -- Record construction ---------------------------------------------

        private IndustrialCompanyRecord BuildRecord(IndustrialCompanyJobData jobData,
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
                out var input1Resources, out var input2Resources, out var outputResources, out var maintenanceResources, out int moneyAmount);
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

            return new IndustrialCompanyRecord
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
                Input1Resource = processInput1,
                Input2Resource = processInput2,
                OutputResource = processOutput,
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
                MonthlyCustomers = monthlyCustomers
            };
        }

        private void ClassifyResources(Entity entity, BufferLookup<Resources> resourcesBufferLookup,
            Resource input1, Resource input2, Resource output,
            out ResourceInfo[] input1List, out ResourceInfo[] input2List, out ResourceInfo[] outputList,
            out ResourceInfo[] maintenanceList, out int money)
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
            var bufferLength = buffer.Length;

            var input1Array = new ResourceInfo[bufferLength];
            var input2Array = new ResourceInfo[bufferLength];
            var outputArray = new ResourceInfo[bufferLength];
            var maintenanceArray = new ResourceInfo[bufferLength];

            var in1Count = 0;
            var in2Count = 0;
            var outCount = 0;
            var maintCount = 0;

            for (var r = 0; r < bufferLength; r++)
            {
                var resource = buffer[r];
                if (resource.m_Resource == Resource.Money)
                {
                    money = resource.m_Amount;
                    continue;
                }

                var info = new ResourceInfo(GetCachedResourceName(resource.m_Resource), resource.m_Amount, GetCachedResourceIcon(resource.m_Resource));

                if (resource.m_Resource == input1 && input1 != Resource.NoResource)
                    input1Array[in1Count++] = info;
                else if (resource.m_Resource == input2 && input2 != Resource.NoResource)
                    input2Array[in2Count++] = info;
                else if (resource.m_Resource == output && output != Resource.NoResource)
                    outputArray[outCount++] = info;
                else
                    maintenanceArray[maintCount++] = info;
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
            var outName = "";
            var outIcon = "";

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
                    list.Add(new ProcessResourceInfo
                    {
                        ResourceName = GetCachedResourceName(processData.m_Input1.m_Resource),
                        Amount = processData.m_Input1.m_Amount,
                        ResourceIcon = GetCachedResourceIcon(processData.m_Input1.m_Resource),
                        IsOutput = false
                    });

                if (processData.m_Input2.m_Resource != Resource.NoResource)
                    list.Add(new ProcessResourceInfo
                    {
                        ResourceName = GetCachedResourceName(processData.m_Input2.m_Resource),
                        Amount = processData.m_Input2.m_Amount,
                        ResourceIcon = GetCachedResourceIcon(processData.m_Input2.m_Resource),
                        IsOutput = false
                    });
            }

            return (list.ToArray(), outName, outIcon);
        }

        private EfficiencyFactorInfo[] GetEfficiencyFactors(Entity targetEntity,
            BufferLookup<Efficiency> efficiencyBufferLookup)
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
                var cumulativeEffect = 100f;
                for (var i = 0; i < sortedEfficiencies.Length; i++)
                {
                    var item = sortedEfficiencies[i];
                    var efficiency = math.max(0f, item.m_Efficiency);
                    cumulativeEffect *= efficiency;

                    var percentageChange = math.max(-99, (int)math.round(100f * efficiency) - 100);
                    var result = math.max(1, (int)math.round(cumulativeEffect));

                    if (percentageChange != 0)
                        tempFactors.Add(new EfficiencyFactorInfo(item.m_Factor, percentageChange, result));
                }
            }
            else
            {
                for (var i = 0; i < sortedEfficiencies.Length; i++)
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
            if (m_CacheInitialized) return;

            for (var i = 0; i < 64; i++)
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
            var currentCount = m_IndustrialCompanyQuery.CalculateEntityCount();
            if (!m_ForceCompanyNameCacheUpdate && currentCount == m_LastCompanyCount)
                return;

            var entities = m_IndustrialCompanyQuery.ToEntityArray(Allocator.Temp);
            var companyDataLookup = GetComponentLookup<CompanyData>(true);

            try
            {
                for (var i = 0; i < entities.Length; i++)
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
            var resourcePrefab = m_ResourceSystem.GetPrefab(resource);
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

            if (!m_ResourceIconCache.TryGetValue(resource, out var icon))
            {
                icon = ResolveResourceIcon(resource);
                m_ResourceIconCache[resource] = icon;
            }

            return icon;
        }
    }
}
