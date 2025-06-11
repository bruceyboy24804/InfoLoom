using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Logging;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Debug;
using Game.Economy;
using Game.Prefabs;
using Game.Reflection;
using Game.Simulation;
using Game.Simulation.Flow;
using Game.Tools;
using Game.UI;
using Game.Zones;
using InfoLoomTwo.Utils;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace InfoLoomTwo.Systems.CommercialSystems.CommercialProductData
{
    public struct CommercialProductDTO
    {
        
        public string ResourceName;
        public string ResourceIcon;
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
        public int ResourceNeeds;
        public int ProduceCapacity;
        public int CurrentAvailables;
        public int TotalAvailables;
        public int MaxServiceWorkers;
        public int CurrentServiceWorkers;
    }

    // Burst-compatible struct for demand data
    public struct DemandJobData : IComponentData
    {
        public Resource ResourceType;
        public FixedString32Bytes ResourceName;
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
        public int ResourceNeeds;
        public int ProduceCapacity;
        public int CurrentAvailables;
        public int TotalAvailables;
        public int MaxServiceWorkers;
        public int CurrentServiceWorkers;
    }

    // Burst-compiled job for processing commercial demand
    [BurstCompile]
    public struct ProcessCommercialDemandJob : IJob
    {
        // Zone and building data
        [DeallocateOnJobCompletion]
        [ReadOnly]
        public NativeArray<ZoneData> m_UnlockedZoneDatas;

        [ReadOnly]
        public NativeList<ArchetypeChunk> m_FreePropertyChunks;

        [ReadOnly]
        public ComponentTypeHandle<PrefabRef> m_PrefabType;

        [ReadOnly]
        public BufferTypeHandle<Renter> m_RenterType;

        // Component lookups
        [ReadOnly]
        public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDatas;

        [ReadOnly]
        public ComponentLookup<ResourceData> m_ResourceDatas;

        [ReadOnly]
        public ComponentLookup<CommercialCompany> m_CommercialCompanies;

        [ReadOnly]
        public ComponentLookup<Tourism> m_Tourisms;

        // System data
        [ReadOnly]
        public ResourcePrefabs m_ResourcePrefabs;

        [ReadOnly]
        public DemandParameterData m_DemandParameters;

        [ReadOnly]
        public Entity m_City;

        [ReadOnly]
        public NativeArray<int> m_TaxRates;

        // Economic data arrays
        [ReadOnly]
        public NativeArray<int> m_ResourceNeeds;

        [ReadOnly]
        public NativeArray<int> m_ProduceCapacity;

        [ReadOnly]
        public NativeArray<int> m_CurrentAvailables;

        [ReadOnly]
        public NativeArray<int> m_Propertyless;

        [ReadOnly]
        public NativeArray<int> m_CurrentServiceWorkers;

        [ReadOnly]
        public NativeArray<int> m_MaxServiceWorkers;

        [ReadOnly]
        public NativeArray<int> m_ServiceCompanies;

        [ReadOnly]
        public NativeArray<int> m_TotalAvailables;


        // Output data
        public NativeValue<int> m_CompanyDemand;

        public NativeValue<int> m_BuildingDemand;

        public NativeArray<int> m_DemandFactors;

        public NativeArray<int> m_FreeProperties;

        public NativeArray<int> m_ResourceDemands;

        public NativeArray<int> m_BuildingDemands;

        public NativeList<DemandJobData> m_DemandJobResults;

        [BurstCompile]
        public void Execute()
        {
            // Check if commercial zones are unlocked
            bool commercialZonesUnlocked = false;
            for (int i = 0; i < m_UnlockedZoneDatas.Length; i++)
            {
                if (m_UnlockedZoneDatas[i].m_AreaType == AreaType.Commercial)
                {
                    commercialZonesUnlocked = true;
                    break;
                }
            }

            // Initialize arrays to zero
            InitializeArrays();

            // Process free properties
            ProcessFreeProperties();

            // Process demand for each resource
            ProcessResourceDemands(commercialZonesUnlocked);
        }

        [BurstCompile]
        private void InitializeArrays()
        {
            ResourceIterator iterator = ResourceIterator.GetIterator();
            while (iterator.Next())
            {
                int resourceIndex = EconomyUtils.GetResourceIndex(iterator.resource);
                m_FreeProperties[resourceIndex] = 0;
                m_BuildingDemands[resourceIndex] = 0;
                m_ResourceDemands[resourceIndex] = 0;
            }

            for (int j = 0; j < m_DemandFactors.Length; j++)
            {
                m_DemandFactors[j] = 0;
            }
        }

        [BurstCompile]
        private void ProcessFreeProperties()
        {
            for (int k = 0; k < m_FreePropertyChunks.Length; k++)
            {
                ArchetypeChunk archetypeChunk = m_FreePropertyChunks[k];
                NativeArray<PrefabRef> prefabRefs = archetypeChunk.GetNativeArray(ref m_PrefabType);
                BufferAccessor<Renter> renterAccessor = archetypeChunk.GetBufferAccessor(ref m_RenterType);

                for (int l = 0; l < prefabRefs.Length; l++)
                {
                    Entity prefab = prefabRefs[l].m_Prefab;
                    if (!m_BuildingPropertyDatas.HasComponent(prefab))
                        continue;

                    bool hasCommercialCompany = false;
                    DynamicBuffer<Renter> renters = renterAccessor[l];
                    for (int m = 0; m < renters.Length; m++)
                    {
                        if (m_CommercialCompanies.HasComponent(renters[m].m_Renter))
                        {
                            hasCommercialCompany = true;
                            break;
                        }
                    }

                    if (hasCommercialCompany)
                        continue;

                    BuildingPropertyData buildingPropertyData = m_BuildingPropertyDatas[prefab];
                    ResourceIterator iterator = ResourceIterator.GetIterator();
                    while (iterator.Next())
                    {
                        if ((buildingPropertyData.m_AllowedSold & iterator.resource) != Resource.NoResource)
                        {
                            m_FreeProperties[EconomyUtils.GetResourceIndex(iterator.resource)]++;
                        }
                    }
                }
            }
        }

        [BurstCompile]
        private void ProcessResourceDemands(bool commercialZonesUnlocked)
        {
            m_CompanyDemand.value = 0;
            m_BuildingDemand.value = 0;

            ResourceIterator iterator = ResourceIterator.GetIterator();
            int validResourceCount = 0;

            while (iterator.Next())
            {
                if (!ProcessSingleResource(iterator.resource, commercialZonesUnlocked))
                    continue;

                validResourceCount++;
            }

            // Final demand calculations
            m_CompanyDemand.value = (validResourceCount != 0) ? 
                math.clamp(m_CompanyDemand.value / validResourceCount, 0, 100) : 0;
            m_BuildingDemand.value = (validResourceCount != 0 && commercialZonesUnlocked) ? 
                math.clamp(m_BuildingDemand.value / validResourceCount, 0, 100) : 0;
        }

        [BurstCompile]
        private bool ProcessSingleResource(Resource resource, bool commercialZonesUnlocked)
        {
            int resourceIndex = EconomyUtils.GetResourceIndex(resource);

            // Only process commercial resources, just like the vanilla system
            if (!EconomyUtils.IsCommercialResource(resource) || 
                !m_ResourceDatas.HasComponent(m_ResourcePrefabs[resource]))
                return false;

            // Determine resource needs and availability
            int resourceNeeds = (m_ResourceNeeds[resourceIndex] == 0 && resource != Resource.Lodging) ? 
                100 : m_ResourceNeeds[resourceIndex];
            int currentAvailable = (m_CurrentAvailables[resourceIndex] == 0) ? 
                m_ProduceCapacity[resourceIndex] : m_CurrentAvailables[resourceIndex];

            // Calculate resource demand (without tax effects)
            float baseDemand = m_DemandParameters.m_CommercialBaseDemand * (float)resourceNeeds - (float)currentAvailable;
            m_ResourceDemands[resourceIndex] = Mathf.RoundToInt(
                math.clamp(math.max(baseDemand, 0f), 0f, 100f));

            // Special case for Lodging
            if (resource == Resource.Lodging && m_Tourisms.HasComponent(m_City))
            {
                var tourism = m_Tourisms[m_City];
                int hotelRoomDemand = math.max(
                    (int)((float)tourism.m_CurrentTourists * m_DemandParameters.m_HotelRoomPercentRequirement) - 
                    tourism.m_Lodging.y, 0);
                
                if (hotelRoomDemand > 0)
                {
                    m_ResourceDemands[resourceIndex] = 100;
                }
            }

            // Create demand job data for UI - now only for commercial resources
            var demandJobData = new DemandJobData
            {
                ResourceType = resource,
                ResourceName = default, // Will be populated outside the job to avoid managed calls in Burst
                Demand = m_ResourceDemands[resourceIndex],
                Building = m_BuildingDemand.value,
                Free = m_FreeProperties[resourceIndex],
                Companies = m_ServiceCompanies[resourceIndex],
                Workers = m_CurrentServiceWorkers[resourceIndex],
                SvcPercent = (m_TotalAvailables[resourceIndex] == 0) ? 0 : 
                    100 * m_CurrentAvailables[resourceIndex] / m_TotalAvailables[resourceIndex],
                CapPercent = 100 * m_ProduceCapacity[resourceIndex] / math.max(100, m_ResourceNeeds[resourceIndex]),
                CapPerCompany = (m_ServiceCompanies[resourceIndex] == 0) ? 0 : 
                    m_ProduceCapacity[resourceIndex] / m_ServiceCompanies[resourceIndex],
                WrkPercent = 100 * (m_CurrentServiceWorkers[resourceIndex] + 1) / 
                    (m_MaxServiceWorkers[resourceIndex] + 1),
                TaxFactor = 0,
                ResourceNeeds = resourceNeeds,
                ProduceCapacity = m_ProduceCapacity[resourceIndex],
                CurrentAvailables = m_CurrentAvailables[resourceIndex],
                TotalAvailables = m_TotalAvailables[resourceIndex],
                MaxServiceWorkers = m_MaxServiceWorkers[resourceIndex],
                CurrentServiceWorkers = m_CurrentServiceWorkers[resourceIndex]
            };

            // Always add commercial resources to results
            m_DemandJobResults.Add(demandJobData);

            // Update company and building demands if there's demand
            if (m_ResourceDemands[resourceIndex] > 0)
            {
                ProcessDemandFactors(resource, resourceIndex);
                return true;
            }

            // Return true to count this as a valid commercial resource even if no demand
            return true;
        }

        [BurstCompile]
        private void ProcessDemandFactors(Resource resource, int resourceIndex)
        {
            m_CompanyDemand.value += m_ResourceDemands[resourceIndex];
            
            m_BuildingDemands[resourceIndex] = 
                (m_FreeProperties[resourceIndex] - m_Propertyless[resourceIndex] <= 0) ? 
                m_ResourceDemands[resourceIndex] : 0;

            if (m_BuildingDemands[resourceIndex] > 0)
            {
                m_BuildingDemand.value += m_BuildingDemands[resourceIndex];
            }

            // Demand factor calculations (without tax effects)
            int buildingDemandFactor = (m_BuildingDemands[resourceIndex] > 0) ? 
                m_ResourceDemands[resourceIndex] : 0;
            int resourceDemand = m_ResourceDemands[resourceIndex];

            // Update demand factors based on resource type
            if (resource == Resource.Lodging)
            {
                m_DemandFactors[9] += resourceDemand; // Tourist Demand
            }
            else if (resource == Resource.Petrochemicals)
            {
                m_DemandFactors[16] += resourceDemand; // Petrol Local Demand 
            }
            else
            {
                m_DemandFactors[4] += resourceDemand; // Local Demand 
            }
            
            // No tax effects or empty buildings calculation since tax is removed
        }
    }

    public partial class CommercialProductsSystem : GameSystemBase
    {
        private SimulationSystem m_SimulationSystem;
        private ResourceSystem m_ResourceSystem;
        private ImageSystem m_ImageSystem;
        private TaxSystem m_TaxSystem;
        private CountCompanyDataSystem m_CountCompanyDataSystem;
        private CountHouseholdDataSystem m_CountHouseholdDataSystem;
        private CitySystem m_CitySystem;
        private ILog m_Log;

        private EntityQuery m_EconomyParameterQuery;
        private EntityQuery m_DemandParameterQuery;
        private EntityQuery m_FreeCommercialQuery;
        private EntityQuery m_CommercialProcessDataQuery;
        private EntityQuery m_UnlockedZoneDataQuery;

        private NativeValue<int> m_CompanyDemand;
        private NativeValue<int> m_BuildingDemand;

        public static NativeArray<DemandJobData> m_DemandData;
        public CommercialProductDTO[] m_CommercialProductDTOs;

        [EnumArray(typeof(DemandFactor))]
        [DebugWatchValue]
        private NativeArray<int> m_DemandFactors;

        [ResourceArray]
        [DebugWatchValue]
        private NativeArray<int> m_ResourceDemands;

        [ResourceArray]
        [DebugWatchValue]
        private NativeArray<int> m_BuildingDemands;

        [ResourceArray]
        [DebugWatchValue]
        private NativeArray<int> m_Consumption;

        [ResourceArray]
        [DebugWatchValue]
        private NativeArray<int> m_FreeProperties;

        [DebugWatchDeps]
        private JobHandle m_WriteDependencies;

        private JobHandle m_ReadDependencies;

        private int m_LastCompanyDemand;
        private int m_LastBuildingDemand;

        // Cached lookups for performance
        private Dictionary<Resource, string> m_ResourceNameCache;
        private Dictionary<Resource, string> m_ResourceIconCache;
        private bool m_CacheInitialized;

        [DebugWatchValue(color = "#008fff")]
        public int companyDemand => m_LastCompanyDemand;

        [DebugWatchValue(color = "#2b6795")]
        public int buildingDemand => m_LastBuildingDemand;

        public bool IsPanelVisible { get; set; }
        public bool ForceUpdate { get; private set; }

        public void ForceUpdateOnce()
        {
            ForceUpdate = true;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = Mod.log;
            m_CommercialProductDTOs = Array.Empty<CommercialProductDTO>();

            // Initialize systems
            m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
            m_ResourceSystem = World.GetOrCreateSystemManaged<ResourceSystem>();
            m_ImageSystem = World.GetOrCreateSystemManaged<ImageSystem>();
            m_TaxSystem = World.GetOrCreateSystemManaged<TaxSystem>();
            m_CountCompanyDataSystem = World.GetOrCreateSystemManaged<CountCompanyDataSystem>();
            m_CountHouseholdDataSystem = World.GetOrCreateSystemManaged<CountHouseholdDataSystem>();
            m_CitySystem = World.GetOrCreateSystemManaged<CitySystem>();

            // Initialize queries
            m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
            m_DemandParameterQuery = GetEntityQuery(ComponentType.ReadOnly<DemandParameterData>());
            m_FreeCommercialQuery = GetEntityQuery(
                ComponentType.ReadOnly<CommercialProperty>(), 
                ComponentType.ReadOnly<PropertyOnMarket>(), 
                ComponentType.ReadOnly<PrefabRef>(), 
                ComponentType.Exclude<Abandoned>(), 
                ComponentType.Exclude<Destroyed>(), 
                ComponentType.Exclude<Deleted>(), 
                ComponentType.Exclude<Condemned>(), 
                ComponentType.Exclude<Temp>());
            m_CommercialProcessDataQuery = GetEntityQuery(
                ComponentType.ReadOnly<IndustrialProcessData>(), 
                ComponentType.ReadOnly<ServiceCompanyData>());
            m_UnlockedZoneDataQuery = GetEntityQuery(
                ComponentType.ReadOnly<ZoneData>(), 
                ComponentType.Exclude<Locked>());

            // Initialize native collections
            m_CompanyDemand = new NativeValue<int>(Allocator.Persistent);
            m_BuildingDemand = new NativeValue<int>(Allocator.Persistent);
            m_DemandFactors = new NativeArray<int>(18, Allocator.Persistent);
            
            int resourceCount = EconomyUtils.ResourceCount;
            m_ResourceDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
            m_BuildingDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
            m_Consumption = new NativeArray<int>(resourceCount, Allocator.Persistent);
            m_FreeProperties = new NativeArray<int>(resourceCount, Allocator.Persistent);
            m_DemandData = new NativeArray<DemandJobData>(resourceCount, Allocator.Persistent);

            // Initialize caches
            m_ResourceNameCache = new Dictionary<Resource, string>();
            m_ResourceIconCache = new Dictionary<Resource, string>();

            RequireForUpdate(m_EconomyParameterQuery);
            RequireForUpdate(m_DemandParameterQuery);
            RequireForUpdate(m_CommercialProcessDataQuery);
        }

        [Preserve]
        protected override void OnDestroy()
        {
            if (m_CompanyDemand.IsCreated) m_CompanyDemand.Dispose();
            if (m_BuildingDemand.IsCreated) m_BuildingDemand.Dispose();
            if (m_DemandFactors.IsCreated) m_DemandFactors.Dispose();
            if (m_ResourceDemands.IsCreated) m_ResourceDemands.Dispose();
            if (m_BuildingDemands.IsCreated) m_BuildingDemands.Dispose();
            if (m_Consumption.IsCreated) m_Consumption.Dispose();
            if (m_FreeProperties.IsCreated) m_FreeProperties.Dispose();
            if (m_DemandData.IsCreated) m_DemandData.Dispose();
            base.OnDestroy();
        }

        protected override void OnGameLoaded(Context serializationContext)
        {
            base.OnGameLoaded(serializationContext);
            InitializeCaches();
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            if (Mod.setting.CustomUpdateInterval)
            {
                return Mod.setting.UpdateInterval;
            }
            return 512;
        }

        protected override void OnUpdate()
        {
            if (!IsPanelVisible)
            {
                return;
            }

            ForceUpdate = false;
            
            if (m_DemandParameterQuery.IsEmptyIgnoreFilter || m_EconomyParameterQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            UpdateCommercialDemandWithBurstJob();
        }

        private void InitializeCaches()
        {
            if (m_CacheInitialized)
            {
                return;
            }

            // Initialize resource name and icon caches
            int resourceCount = 0;
            ResourceIterator iterator = ResourceIterator.GetIterator();
            while (iterator.Next())
            {
                if (iterator.resource == Resource.NoResource) continue;
                
                var resourceName = GetFormattedResourceName(iterator.resource);
                var iconPath = GetResourceIconPath(iterator.resource);
                m_ResourceNameCache[iterator.resource] = resourceName;
                m_ResourceIconCache[iterator.resource] = iconPath;
                resourceCount++;
            }

            m_CacheInitialized = true;
        }

        private void UpdateCommercialDemandWithBurstJob()
        {
            
            if (!IsPanelVisible)
            {
                return;
            }

            InitializeCaches();

            m_LastCompanyDemand = m_CompanyDemand.value;
            m_LastBuildingDemand = m_BuildingDemand.value;

            JobHandle deps;
            CountCompanyDataSystem.CommercialCompanyDatas commercialCompanyDatas = 
                m_CountCompanyDataSystem.GetCommercialCompanyDatas(out deps);
            

            // Create result list for job data
            var jobResults = new NativeList<DemandJobData>(EconomyUtils.ResourceCount, Allocator.TempJob);

            

            // Create the burst-compiled job
            var job = new ProcessCommercialDemandJob
            {
                // Zone and building data
                m_UnlockedZoneDatas = m_UnlockedZoneDataQuery.ToComponentDataArray<ZoneData>(Allocator.TempJob),
                m_FreePropertyChunks = m_FreeCommercialQuery.ToArchetypeChunkListAsync(
                    World.UpdateAllocator.ToAllocator, out var outJobHandle),

                // Component type handles
                m_PrefabType = GetComponentTypeHandle<PrefabRef>(true),
                m_RenterType = GetBufferTypeHandle<Renter>(true),

                // Component lookups
                m_BuildingPropertyDatas = GetComponentLookup<BuildingPropertyData>(true),
                m_ResourceDatas = GetComponentLookup<ResourceData>(true),
                m_CommercialCompanies = GetComponentLookup<CommercialCompany>(true),
                m_Tourisms = GetComponentLookup<Tourism>(true),

                // System data
                m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
                m_DemandParameters = m_DemandParameterQuery.GetSingleton<DemandParameterData>(),
                m_City = m_CitySystem.City,
                m_TaxRates = m_TaxSystem.GetTaxRates(),

                // Economic data
                m_ResourceNeeds = m_CountHouseholdDataSystem.GetResourceNeeds(out deps),
                m_ProduceCapacity = commercialCompanyDatas.m_ProduceCapacity,
                m_CurrentAvailables = commercialCompanyDatas.m_CurrentAvailables,
                m_Propertyless = commercialCompanyDatas.m_ServicePropertyless,
                m_CurrentServiceWorkers = commercialCompanyDatas.m_CurrentServiceWorkers,
                m_MaxServiceWorkers = commercialCompanyDatas.m_MaxServiceWorkers,
                m_ServiceCompanies = commercialCompanyDatas.m_ServiceCompanies,
                m_TotalAvailables = commercialCompanyDatas.m_TotalAvailables,
                // Output data
                m_CompanyDemand = m_CompanyDemand,
                m_BuildingDemand = m_BuildingDemand,
                m_DemandFactors = m_DemandFactors,
                m_FreeProperties = m_FreeProperties,
                m_ResourceDemands = m_ResourceDemands,
                m_BuildingDemands = m_BuildingDemands,
                m_DemandJobResults = jobResults
            };

           

            // Schedule the job
            var jobHandle = job.Schedule(JobUtils.CombineDependencies(
                Dependency, m_ReadDependencies, outJobHandle, deps));
            
            
            // Complete the job and convert results
            jobHandle.Complete();
            
            
            // Convert job results to final DTOs
            ConvertJobResultsToDTO(jobResults);
            
            jobResults.Dispose();
            

            // Update dependencies
            m_WriteDependencies = Dependency;
            m_CountHouseholdDataSystem.AddHouseholdDataReader(Dependency);
            m_ResourceSystem.AddPrefabsReader(Dependency);
            m_TaxSystem.AddReader(Dependency);
            
            
        }

        private void ConvertJobResultsToDTO(NativeList<DemandJobData> jobResults)
        {
            
            var products = new List<CommercialProductDTO>(jobResults.Length);

            for (int i = 0; i < jobResults.Length; i++)
            {
                var jobData = jobResults[i];

                string resourceName = m_ResourceNameCache.TryGetValue(jobData.ResourceType, out var name) ? 
                    name : jobData.ResourceName.ToString();

                var productDTO = new CommercialProductDTO
                {
                    ResourceName = resourceName,
                    ResourceIcon = m_ResourceIconCache.TryGetValue(jobData.ResourceType, out var icon) ? 
                        icon : "",
                    Demand = jobData.Demand,
                    Building = jobData.Building,
                    Free = jobData.Free,
                    Companies = jobData.Companies,
                    Workers = jobData.Workers,
                    SvcPercent = jobData.SvcPercent,
                    CapPercent = jobData.CapPercent,
                    CapPerCompany = jobData.CapPerCompany,
                    WrkPercent = jobData.WrkPercent,
                    TaxFactor = jobData.TaxFactor,
                    ResourceNeeds = jobData.ResourceNeeds,
                    ProduceCapacity = jobData.ProduceCapacity,
                    CurrentAvailables = jobData.CurrentAvailables,
                    TotalAvailables = jobData.TotalAvailables,
                    MaxServiceWorkers = jobData.MaxServiceWorkers,
                    CurrentServiceWorkers = jobData.CurrentServiceWorkers
                };

                

                products.Add(productDTO);
            }

            // Sort products by resource type for consistent ordering
            m_CommercialProductDTOs = products.ToArray();
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

        // Public API methods (similar to original)
        public NativeArray<int> GetDemandFactors(out JobHandle deps)
        {
            deps = m_WriteDependencies;
            return m_DemandFactors;
        }

        public NativeArray<int> GetResourceDemands(out JobHandle deps)
        {
            deps = m_WriteDependencies;
            return m_ResourceDemands;
        }

        public NativeArray<int> GetBuildingDemands(out JobHandle deps)
        {
            deps = m_WriteDependencies;
            return m_BuildingDemands;
        }

        public NativeArray<int> GetConsumption(out JobHandle deps)
        {
            deps = m_WriteDependencies;
            return m_Consumption;
        }

        public void AddReader(JobHandle reader)
        {
            m_ReadDependencies = JobHandle.CombineDependencies(m_ReadDependencies, reader);
        }

        public void SetDefaults(Context context)
        {
            m_CompanyDemand.value = 0;
            m_BuildingDemand.value = 0;
            m_DemandFactors.Fill(0);
            m_ResourceDemands.Fill(0);
            m_BuildingDemands.Fill(0);
            m_Consumption.Fill(0);
            m_FreeProperties.Fill(0);
            m_LastCompanyDemand = 0;
            m_LastBuildingDemand = 0;
        }
    }
}
