using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Game.Zones;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using Game;
using Game.Simulation;
using Game.UI;
using InfoLoom;

namespace InfoLoomBrucey.Systems;

[CompilerGenerated]
public partial class CommercialUISystem : UISystemBase
{
    public struct DemandData
    {
        public Resource Resource;
        public int Demand;          // company demand
        public int Building;        // building demand
        public int Free;           // free properties
        public int Companies;      // num of companies
        public int Workers;        // num of workers
        public int SvcFactor;      // service availability
        public int SvcPercent;
        public int CapFactor;      // sales capacity
        public int CapPercent;
        public int CapPerCompany;
        public int WrkFactor;      // employee ratio
        public int WrkPercent;
        public int EduFactor;      // educated employees
        public int TaxFactor;      // tax factor

        public DemandData(Resource resource) { Resource = resource; }
    }

    [BurstCompile]
    private struct UpdateCommercialDemandJob : IJob
    {
        [DeallocateOnJobCompletion]
        [ReadOnly]
        public NativeArray<ZoneData> m_UnlockedZoneDatas;

        [ReadOnly]
        public NativeList<ArchetypeChunk> m_FreePropertyChunks;

        [ReadOnly]
        public ComponentTypeHandle<PrefabRef> m_PrefabType;

        [ReadOnly]
        public BufferTypeHandle<Renter> m_RenterType;

        [ReadOnly]
        public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDatas;

        [ReadOnly]
        public ComponentLookup<ResourceData> m_ResourceDatas;

        [ReadOnly]
        public ComponentLookup<CommercialCompany> m_CommercialCompanies;

        [ReadOnly]
        public ComponentLookup<Tourism> m_Tourisms;

        [ReadOnly]
        public ResourcePrefabs m_ResourcePrefabs;

        [ReadOnly]
        public DemandParameterData m_DemandParameters;

        [ReadOnly]
        public Entity m_City;

        [ReadOnly]
        public NativeArray<int> m_TaxRates;

        public NativeValue<int> m_CompanyDemand;
        public NativeValue<int> m_BuildingDemand;
        public NativeArray<int> m_DemandFactors;
        public NativeArray<int> m_FreeProperties;
        public NativeArray<int> m_ResourceDemands;
        public NativeArray<int> m_BuildingDemands;

        [ReadOnly]
        public NativeArray<int> m_ResourceNeeds;
        [ReadOnly]
        public NativeArray<int> m_ProduceCapacity;
        [ReadOnly]
        public NativeArray<int> m_CurrentAvailables;
        [ReadOnly]
        public NativeArray<int> m_Propertyless;
        [ReadOnly]
        public NativeArray<int> m_Companies;
        [ReadOnly]
        public NativeArray<int> m_Productions;
        [ReadOnly]
        public NativeArray<int> m_TotalCurrentWorkers;
        [ReadOnly]
        public NativeArray<int> m_TotalMaxWorkers;
        [ReadOnly]
        public NativeArray<int> m_TotalAvailables;
        [ReadOnly]
        public NativeArray<int> m_TotalMaximums;

        public NativeArray<DemandData> m_DemandData;

        public void Execute()
        {
            bool hasCommercialZones = false;
            for (int i = 0; i < m_UnlockedZoneDatas.Length; i++)
            {
                if (m_UnlockedZoneDatas[i].m_AreaType == AreaType.Commercial)
                {
                    hasCommercialZones = true;
                    break;
                }
            }

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

            // Count free properties
            for (int k = 0; k < m_FreePropertyChunks.Length; k++)
            {
                ArchetypeChunk chunk = m_FreePropertyChunks[k];
                NativeArray<PrefabRef> prefabRefs = chunk.GetNativeArray(ref m_PrefabType);
                BufferAccessor<Renter> renters = chunk.GetBufferAccessor(ref m_RenterType);

                for (int l = 0; l < prefabRefs.Length; l++)
                {
                    Entity prefab = prefabRefs[l].m_Prefab;
                    if (!m_BuildingPropertyDatas.HasComponent(prefab))
                        continue;

                    bool hasCommercialRenter = false;
                    DynamicBuffer<Renter> buildingRenters = renters[l];
                    for (int m = 0; m < buildingRenters.Length; m++)
                    {
                        if (m_CommercialCompanies.HasComponent(buildingRenters[m].m_Renter))
                        {
                            hasCommercialRenter = true;
                            break;
                        }
                    }

                    if (hasCommercialRenter)
                        continue;

                    BuildingPropertyData propertyData = m_BuildingPropertyDatas[prefab];
                    ResourceIterator iterator2 = ResourceIterator.GetIterator();
                    while (iterator2.Next())
                    {
                        if ((propertyData.m_AllowedSold & iterator2.resource) != Resource.NoResource)
                        {
                            m_FreeProperties[EconomyUtils.GetResourceIndex(iterator2.resource)]++;
                        }
                    }
                }
            }

            m_CompanyDemand.value = 0;
            m_BuildingDemand.value = 0;

            iterator = ResourceIterator.GetIterator();
            int resourceCount = 0;

            while (iterator.Next())
            {
                int resourceIndex = EconomyUtils.GetResourceIndex(iterator.resource);
                if (!EconomyUtils.IsCommercialResource(iterator.resource) || !m_ResourceDatas.HasComponent(m_ResourcePrefabs[iterator.resource]))
                    continue;

                float taxEffect = -0.05f * ((float)TaxSystem.GetCommercialTaxRate(iterator.resource, m_TaxRates) - 10f);
                int resourceNeed = ((m_ResourceNeeds[resourceIndex] == 0 && iterator.resource != Resource.Lodging) ? 100 : m_ResourceNeeds[resourceIndex]);
                int currentAvailable = ((m_CurrentAvailables[resourceIndex] == 0) ? m_ProduceCapacity[resourceIndex] : m_CurrentAvailables[resourceIndex]);

                // Calculate demand
                m_ResourceDemands[resourceIndex] = Mathf.RoundToInt((1f + taxEffect) *
                    math.clamp(math.max(m_DemandParameters.m_CommercialBaseDemand * (float)resourceNeed - (float)currentAvailable, 0f), 0f, 100f));

                if (iterator.resource == Resource.Lodging &&
                    math.max((int)((float)m_Tourisms[m_City].m_CurrentTourists *
                    m_DemandParameters.m_HotelRoomPercentRequirement) -
                    m_Tourisms[m_City].m_Lodging.y, 0) > 0)
                {
                    m_ResourceDemands[resourceIndex] = 100;
                }

                // Update UI data
                DemandData uiData = m_DemandData[resourceIndex];
                uiData.Resource = iterator.resource;
                uiData.Demand = m_ResourceDemands[resourceIndex];
                uiData.Building = m_BuildingDemands[resourceIndex];
                uiData.Free = m_FreeProperties[resourceIndex];
                uiData.Companies = m_Companies[resourceIndex];
                uiData.Workers = m_TotalCurrentWorkers[resourceIndex];

                // Service factors
                float serviceFactor = ((m_TotalMaximums[resourceIndex] == 0) ? 0f :
                    (-2.5f + 5f * (1f - (float)m_TotalAvailables[resourceIndex] / (float)m_TotalMaximums[resourceIndex])));
                uiData.SvcFactor = Mathf.RoundToInt(100f * serviceFactor);
                uiData.SvcPercent = (m_TotalMaximums[resourceIndex] == 0 ? 0 :
                    100 * m_TotalAvailables[resourceIndex] / m_TotalMaximums[resourceIndex]);

                // Capacity factors
                float capacityFactor = (m_DemandParameters.m_CommercialBaseDemand * (float)resourceNeed - (float)currentAvailable) /
                    math.max(100f, (float)resourceNeed + 1f);
                uiData.CapFactor = Mathf.RoundToInt(100f * capacityFactor);
                uiData.CapPercent = 100 * m_Productions[resourceIndex] / math.max(100, resourceNeed);
                uiData.CapPerCompany = (m_Companies[resourceIndex] == 0 ? 0 :
                    m_Productions[resourceIndex] / m_Companies[resourceIndex]);

                // Worker factors
                float workerFactor = -9f + 10f * (((float)m_TotalCurrentWorkers[resourceIndex] + 1f) /
                    ((float)m_TotalMaxWorkers[resourceIndex] + 1f));
                if (workerFactor > 0f)
                    workerFactor *= 0.5f;
                uiData.WrkFactor = Mathf.RoundToInt(100f * workerFactor);
                uiData.WrkPercent = 100 * (m_TotalCurrentWorkers[resourceIndex] + 1) /
                    (m_TotalMaxWorkers[resourceIndex] + 1);

                // Tax factor
                uiData.TaxFactor = Mathf.RoundToInt(100f * taxEffect);

                m_DemandData[resourceIndex] = uiData;

                if (m_ResourceDemands[resourceIndex] > 0)
                {
                    m_CompanyDemand.value += m_ResourceDemands[resourceIndex];
                    m_BuildingDemands[resourceIndex] = ((m_FreeProperties[resourceIndex] - m_Propertyless[resourceIndex] <= 0) ?
                        m_ResourceDemands[resourceIndex] : 0);

                    if (m_BuildingDemands[resourceIndex] > 0)
                    {
                        m_BuildingDemand.value += m_BuildingDemands[resourceIndex];
                    }

                    // Update demand factors
                    int buildingDemand = ((m_BuildingDemands[resourceIndex] > 0) ? m_ResourceDemands[resourceIndex] : 0);
                    int resourceDemand = m_ResourceDemands[resourceIndex];
                    int taxFactor = Mathf.RoundToInt(100f * taxEffect);
                    int totalFactor = resourceDemand + taxFactor;

                    if (iterator.resource == Resource.Lodging)
                        m_DemandFactors[9] += resourceDemand;
                    else if (iterator.resource == Resource.Petrochemicals)
                        m_DemandFactors[16] += resourceDemand;
                    else
                        m_DemandFactors[4] += resourceDemand;

                    m_DemandFactors[11] += taxFactor;
                    m_DemandFactors[13] += math.min(0, buildingDemand - totalFactor);
                    resourceCount++;
                }
            }

            // Final demand calculations
            m_CompanyDemand.value = ((resourceCount != 0) ? math.clamp(m_CompanyDemand.value / resourceCount, 0, 100) : 0);
            m_BuildingDemand.value = ((resourceCount != 0 && hasCommercialZones) ?
                math.clamp(m_BuildingDemand.value / resourceCount, 0, 100) : 0);
        }
    }

    private struct TypeHandle
    {
        [ReadOnly]
        public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

        [ReadOnly]
        public BufferTypeHandle<Renter> __Game_Buildings_Renter_RO_BufferTypeHandle;

        [ReadOnly]
        public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<CommercialCompany> __Game_Companies_CommercialCompany_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Tourism> __Game_City_Tourism_RO_ComponentLookup;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref SystemState state)
        {
            __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
            __Game_Buildings_Renter_RO_BufferTypeHandle = state.GetBufferTypeHandle<Renter>(isReadOnly: true);
            __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
            __Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
            __Game_Companies_CommercialCompany_RO_ComponentLookup = state.GetComponentLookup<CommercialCompany>(isReadOnly: true);
            __Game_City_Tourism_RO_ComponentLookup = state.GetComponentLookup<Tourism>(isReadOnly: true);
        }
    }

    private const string kGroup = "realEco";

    // Systems
    private SimulationSystem m_SimulationSystem;
    private ResourceSystem m_ResourceSystem;
    private TaxSystem m_TaxSystem;
    private CountHouseholdDataSystem m_CountHouseholdDataSystem;
    private CountCompanyDataSystem m_CountCompanyDataSystem;
    private CitySystem m_CitySystem;

    // Queries
    private EntityQuery m_EconomyParameterQuery;
    private EntityQuery m_DemandParameterQuery;
    private EntityQuery m_FreeCommercialQuery;
    private EntityQuery m_UnlockedZoneDataQuery;

    // Type handles
    private TypeHandle __TypeHandle;

    // Demand calculation data
    private NativeValue<int> m_CompanyDemand;
    private NativeValue<int> m_BuildingDemand;
    private NativeArray<int> m_DemandFactors;
    private NativeArray<int> m_ResourceDemands;
    private NativeArray<int> m_BuildingDemands;
    private NativeArray<int> m_FreeProperties;
    private int m_LastCompanyDemand;
    private int m_LastBuildingDemand;
    private JobHandle m_WriteDependencies;
    private JobHandle m_ReadDependencies;

    // UI data
    private RawValueBinding m_uiResults;
    private NativeArray<DemandData> m_DemandData;

    public override GameMode gameMode => GameMode.Game;

    [Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();

        // Get required systems
        m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
        m_ResourceSystem = World.GetOrCreateSystemManaged<ResourceSystem>();
        m_TaxSystem = World.GetOrCreateSystemManaged<TaxSystem>();
        m_CountHouseholdDataSystem = World.GetOrCreateSystemManaged<CountHouseholdDataSystem>();
        m_CountCompanyDataSystem = World.GetOrCreateSystemManaged<CountCompanyDataSystem>();
        m_CitySystem = World.GetOrCreateSystemManaged<CitySystem>();

        // Create queries
        m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
        m_DemandParameterQuery = GetEntityQuery(ComponentType.ReadOnly<DemandParameterData>());
        m_FreeCommercialQuery = GetEntityQuery(
            ComponentType.ReadOnly<PrefabRef>(),
            ComponentType.ReadOnly<Renter>(),
            ComponentType.ReadOnly<CommercialProperty>());
        m_UnlockedZoneDataQuery = GetEntityQuery(ComponentType.ReadOnly<ZoneData>());

        // Initialize arrays
        m_DemandData = new NativeArray<DemandData>(EconomyUtils.ResourceCount, Allocator.Persistent);
        m_CompanyDemand = new NativeValue<int>(Allocator.Persistent);
        m_BuildingDemand = new NativeValue<int>(Allocator.Persistent);
        m_DemandFactors = new NativeArray<int>(32, Allocator.Persistent);
        m_ResourceDemands = new NativeArray<int>(EconomyUtils.ResourceCount, Allocator.Persistent);
        m_BuildingDemands = new NativeArray<int>(EconomyUtils.ResourceCount, Allocator.Persistent);
        m_FreeProperties = new NativeArray<int>(EconomyUtils.ResourceCount, Allocator.Persistent);

        // UI binding
        AddBinding(m_uiResults = new RawValueBinding(kGroup, "commercialDemand", WriteResults));

        __TypeHandle.__AssignHandles(ref CheckedStateRef);

        Mod.log.Info("CommercialUISystem created.");
    }

    [Preserve]
    protected override void OnDestroy()
    {
        if (m_DemandData.IsCreated) m_DemandData.Dispose();
        if (m_CompanyDemand.IsCreated) m_CompanyDemand.Dispose();
        if (m_BuildingDemand.IsCreated) m_BuildingDemand.Dispose();
        if (m_DemandFactors.IsCreated) m_DemandFactors.Dispose();
        if (m_ResourceDemands.IsCreated) m_ResourceDemands.Dispose();
        if (m_BuildingDemands.IsCreated) m_BuildingDemands.Dispose();
        if (m_FreeProperties.IsCreated) m_FreeProperties.Dispose();

        base.OnDestroy();
    }

    [Preserve]
    protected override void OnUpdate()
    {
        if (m_SimulationSystem.frameIndex % 64 != 17)
            return;

        if (!m_DemandParameterQuery.IsEmptyIgnoreFilter && !m_EconomyParameterQuery.IsEmptyIgnoreFilter)
        {
            m_LastCompanyDemand = m_CompanyDemand.value;
            m_LastBuildingDemand = m_BuildingDemand.value;

            JobHandle deps;
            CountCompanyDataSystem.CommercialCompanyDatas commercialCompanyDatas =
                m_CountCompanyDataSystem.GetCommercialCompanyDatas(out deps);

            // Update type handles
            UpdateTypeHandles();

            // Setup and schedule job
            UpdateCommercialDemandJob job = CreateDemandJob(commercialCompanyDatas, out JobHandle outJobHandle);

            // Schedule job with combined dependencies
            JobHandle combinedDeps = JobHandle.CombineDependencies(m_ReadDependencies, outJobHandle, deps);
            JobHandle dependency = job.Schedule(combinedDeps);

            m_WriteDependencies = dependency;
            m_CountHouseholdDataSystem.AddHouseholdResourceNeedReader(dependency);
            m_ResourceSystem.AddPrefabsReader(dependency);
            m_TaxSystem.AddReader(dependency);
        }

        m_uiResults.Update();
    }

    private void WriteResults(IJsonWriter writer)
    {
        writer.ArrayBegin(m_DemandData.Length);
        for (int i = 0; i < m_DemandData.Length; i++)
            WriteData(writer, m_DemandData[i]);
        writer.ArrayEnd();
    }

    private static void WriteData(IJsonWriter writer, DemandData data)
    {
        writer.TypeBegin("DemandData");
        writer.PropertyName("resource");
        writer.Write(data.Resource.ToString());
        writer.PropertyName("demand");
        writer.Write(data.Demand);
        writer.PropertyName("building");
        writer.Write(data.Building);
        writer.PropertyName("free");
        writer.Write(data.Free);
        writer.PropertyName("companies");
        writer.Write(data.Companies);
        writer.PropertyName("workers");
        writer.Write(data.Workers);
        writer.PropertyName("svcfactor");
        writer.Write(data.SvcFactor);
        writer.PropertyName("svcpercent");
        writer.Write(data.SvcPercent);
        writer.PropertyName("capfactor");
        writer.Write(data.CapFactor);
        writer.PropertyName("cappercent");
        writer.Write(data.CapPercent);
        writer.PropertyName("cappercompany");
        writer.Write(data.CapPerCompany);
        writer.PropertyName("wrkfactor");
        writer.Write(data.WrkFactor);
        writer.PropertyName("wrkpercent");
        writer.Write(data.WrkPercent);
        writer.PropertyName("edufactor");
        writer.Write(data.EduFactor);
        writer.PropertyName("taxfactor");
        writer.Write(data.TaxFactor);
        writer.TypeEnd();
    }

    private void UpdateTypeHandles()
    {
        __TypeHandle.__Game_City_Tourism_RO_ComponentLookup.Update(ref CheckedStateRef);
        __TypeHandle.__Game_Companies_CommercialCompany_RO_ComponentLookup.Update(ref CheckedStateRef);
        __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup.Update(ref CheckedStateRef);
        __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup.Update(ref CheckedStateRef);
        __TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle.Update(ref CheckedStateRef);
        __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle.Update(ref CheckedStateRef);
    }

    private UpdateCommercialDemandJob CreateDemandJob(CountCompanyDataSystem.CommercialCompanyDatas commercialCompanyDatas, out JobHandle outJobHandle)
    {
        var job = default(UpdateCommercialDemandJob);

        job.m_FreePropertyChunks = m_FreeCommercialQuery.ToArchetypeChunkListAsync(
            World.UpdateAllocator.ToAllocator, out outJobHandle);
        job.m_UnlockedZoneDatas = m_UnlockedZoneDataQuery.ToComponentDataArray<ZoneData>(Allocator.TempJob);
        job.m_PrefabType = __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
        job.m_RenterType = __TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle;
        job.m_BuildingPropertyDatas = __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;
        job.m_ResourceDatas = __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup;
        job.m_CommercialCompanies = __TypeHandle.__Game_Companies_CommercialCompany_RO_ComponentLookup;
        job.m_ResourcePrefabs = m_ResourceSystem.GetPrefabs();
        job.m_DemandParameters = m_DemandParameterQuery.GetSingleton<DemandParameterData>();
        job.m_TaxRates = m_TaxSystem.GetTaxRates();
        job.m_CompanyDemand = m_CompanyDemand;
        job.m_BuildingDemand = m_BuildingDemand;
        job.m_DemandFactors = m_DemandFactors;
        job.m_City = m_CitySystem.City;
        job.m_ResourceDemands = m_ResourceDemands;
        job.m_BuildingDemands = m_BuildingDemands;
        job.m_ProduceCapacity = commercialCompanyDatas.m_ProduceCapacity;
        job.m_CurrentAvailables = commercialCompanyDatas.m_CurrentAvailables;
        job.m_ResourceNeeds = m_CountHouseholdDataSystem.GetResourceNeeds();
        job.m_FreeProperties = m_FreeProperties;
        job.m_Propertyless = commercialCompanyDatas.m_ServicePropertyless;
        job.m_Tourisms = __TypeHandle.__Game_City_Tourism_RO_ComponentLookup;
        job.m_Companies = commercialCompanyDatas.m_ServiceCompanies;
        job.m_Productions = commercialCompanyDatas.m_ProduceCapacity;
        job.m_TotalMaximums = commercialCompanyDatas.m_TotalAvailables;
        job.m_TotalAvailables = commercialCompanyDatas.m_CurrentAvailables;
        job.m_TotalMaxWorkers = commercialCompanyDatas.m_MaxServiceWorkers;
        job.m_TotalCurrentWorkers = commercialCompanyDatas.m_CurrentServiceWorkers;
        job.m_DemandData = m_DemandData;

        return job;
    }
}