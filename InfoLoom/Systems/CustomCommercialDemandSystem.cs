using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Serialization.Entities;
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
using Game.Zones;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using static Game.Rendering.Debug.RenderPrefabRenderer;
using static InfoLoomTwo.Systems.CommercialUISystem;
using System.Linq;
using Game.UI;
using static InfoLoomTwo.Systems.IndustrialUISystem;

namespace InfoLoomTwo.Systems;

[CompilerGenerated]
public partial class CustomCommercialDemandSystem : UISystemBase, IDefaultSerializable, ISerializable
{
    Setting setting = Mod.setting;
    [BurstCompile]
    private struct UpdateCommercialDemandJob : IJob
    {

        [DeallocateOnJobCompletion]
        [ReadOnly]
        public NativeArray<ZoneData> m_UnlockedZoneDatas;

        [ReadOnly]
        public float TaxRateEffect;
        [ReadOnly]
        public float TaxRateMultiplier;

        [ReadOnly]
        public bool VanillaLodging;
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
        public NativeList<ArchetypeChunk> m_CommercialProcessDataChunks;

        [ReadOnly]
        public ComponentTypeHandle<IndustrialProcessData> m_ProcessType;

        [ReadOnly]
        public NativeArray<int> m_TaxRates;

        public NativeValue<int> m_CompanyDemand;

        public NativeValue<int> m_BuildingDemand;

        public NativeArray<int> m_DemandFactors;

        public NativeArray<int> m_FreeProperties;

        public NativeArray<int> m_ResourceDemands;

        public NativeArray<int> m_BuildingDemands;

        [ReadOnly]
        public ComponentLookup<Population> m_Populations; // Added for population data



        [ReadOnly]
        public EntityTypeHandle m_EntityType;

        [ReadOnly]
        public NativeArray<int> m_ResourceNeeds;

        [ReadOnly]
        public NativeArray<int> m_ProduceCapacity;

        [ReadOnly]
        public NativeArray<int> m_CurrentAvailables;

        [ReadOnly]
        public NativeArray<int> m_Propertyless;

        public NativeArray<DemandData> m_DemandData; // Added for UI data
        [ReadOnly]
        public NativeArray<int> m_CurrentServiceWorkers;
        [ReadOnly]
        public NativeArray<int> m_MaxServiceWorkers;
        [ReadOnly]
        public NativeArray<int> m_ServiceCompanies;
        [ReadOnly]
        public NativeArray<int> m_TotalAvailables;

        [ReadOnly]
        public NativeArray<int> m_EmployableByEducation;

        [ReadOnly]
        public ComponentLookup<WorkplaceData> m_WorkplaceDatas;

        [ReadOnly]
        public Workplaces m_FreeWorkplaces;


        public void Execute()
        {
            bool flag = false;
            for (int i = 0; i < m_UnlockedZoneDatas.Length; i++)
            {
                if (m_UnlockedZoneDatas[i].m_AreaType == AreaType.Commercial)
                {
                    flag = true;
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
            for (int k = 0; k < m_FreePropertyChunks.Length; k++)
            {
                ArchetypeChunk archetypeChunk = m_FreePropertyChunks[k];
                NativeArray<PrefabRef> nativeArray = archetypeChunk.GetNativeArray(ref m_PrefabType);
                BufferAccessor<Renter> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_RenterType);
                for (int l = 0; l < nativeArray.Length; l++)
                {
                    Entity prefab = nativeArray[l].m_Prefab;
                    if (!m_BuildingPropertyDatas.HasComponent(prefab))
                    {
                        continue;
                    }
                    bool flag2 = false;
                    DynamicBuffer<Renter> dynamicBuffer = bufferAccessor[l];
                    for (int m = 0; m < dynamicBuffer.Length; m++)
                    {
                        if (m_CommercialCompanies.HasComponent(dynamicBuffer[m].m_Renter))
                        {
                            flag2 = true;
                            break;
                        }
                    }
                    if (flag2)
                    {
                        continue;
                    }
                    BuildingPropertyData buildingPropertyData = m_BuildingPropertyDatas[prefab];
                    ResourceIterator iterator2 = ResourceIterator.GetIterator();
                    while (iterator2.Next())
                    {
                        if ((buildingPropertyData.m_AllowedSold & iterator2.resource) != Resource.NoResource)
                        {
                            m_FreeProperties[EconomyUtils.GetResourceIndex(iterator2.resource)]++;
                        }
                    }
                }
            }

            m_CompanyDemand.value = 0;
            m_BuildingDemand.value = 0;
            iterator = ResourceIterator.GetIterator();
            int num = 0;
            while (iterator.Next())
            {
                int resourceIndex2 = EconomyUtils.GetResourceIndex(iterator.resource);

                // Early validation for resource
                if (!EconomyUtils.IsCommercialResource(iterator.resource) || !m_ResourceDatas.HasComponent(m_ResourcePrefabs[iterator.resource]))
                    continue;

                // Calculate tax effect
                float num2 = TaxRateEffect * ((float)TaxSystem.GetCommercialTaxRate(iterator.resource, m_TaxRates) - 10f);



                // Determine resource needs and availability
                int num3 = ((m_ResourceNeeds[resourceIndex2] == 0 && iterator.resource != Resource.Lodging) ? 100 : m_ResourceNeeds[resourceIndex2]);
                int num4 = ((m_CurrentAvailables[resourceIndex2] == 0) ? m_ProduceCapacity[resourceIndex2] : m_CurrentAvailables[resourceIndex2]);
                m_BuildingDemand.value = ((num != 0 && flag) ? math.clamp(m_BuildingDemand.value / num, 0, 100) : 0);
                // Calculate resource demands
                m_ResourceDemands[resourceIndex2] = Mathf.RoundToInt((1f + num2) * math.clamp(math.max(m_DemandParameters.m_CommercialBaseDemand * (float)num3 - (float)num4, 0f), 0f, 100f));

                // Update UI data for the current resource
                DemandData uiData = m_DemandData[resourceIndex2];
                uiData.Resource = iterator.resource;
                uiData.Demand = m_ResourceDemands[resourceIndex2];
                uiData.Building = m_BuildingDemand.value;
                uiData.Free = m_FreeProperties[resourceIndex2];
                uiData.Companies = m_ServiceCompanies[resourceIndex2];
                uiData.Workers = m_CurrentServiceWorkers[resourceIndex2];
                uiData.SvcPercent = (m_TotalAvailables[resourceIndex2] == 0 ? 0 : 100 * m_CurrentAvailables[resourceIndex2] / m_TotalAvailables[resourceIndex2]);
                uiData.CapPercent = 100 * m_ProduceCapacity[resourceIndex2] / math.max(100, m_ResourceNeeds[resourceIndex2]);
                uiData.CapPerCompany = (m_ServiceCompanies[resourceIndex2] == 0 ? 0 : m_ProduceCapacity[resourceIndex2] / m_ServiceCompanies[resourceIndex2]);
                uiData.WrkPercent = 100 * (m_CurrentServiceWorkers[resourceIndex2] + 1) / (m_MaxServiceWorkers[resourceIndex2] + 1);
                uiData.TaxFactor = Mathf.RoundToInt(100f * num2);
                m_DemandData[resourceIndex2] = uiData; // Update the demand data for UI

                // Special case for Lodging
                if (iterator.resource == Resource.Lodging && math.max((int)((float)m_Tourisms[m_City].m_CurrentTourists * m_DemandParameters.m_HotelRoomPercentRequirement) - m_Tourisms[m_City].m_Lodging.y, 0) > 0)
                {
                    m_ResourceDemands[resourceIndex2] = 100;
                }

                // Update company and building demands
                if (m_ResourceDemands[resourceIndex2] > 0)
                {
                    m_CompanyDemand.value += m_ResourceDemands[resourceIndex2];
                    m_BuildingDemands[resourceIndex2] = ((m_FreeProperties[resourceIndex2] - m_Propertyless[resourceIndex2] <= 0) ? m_ResourceDemands[resourceIndex2] : 0);

                    if (m_BuildingDemands[resourceIndex2] > 0)
                    {
                        m_BuildingDemand.value += m_BuildingDemands[resourceIndex2];
                    }

                    // Demand factor calculations
                    int num5 = ((m_BuildingDemands[resourceIndex2] > 0) ? m_ResourceDemands[resourceIndex2] : 0);
                    int num6 = m_ResourceDemands[resourceIndex2];
                    int num7 = Mathf.RoundToInt(100f * num2);
                    int num8 = num6 + num7;

                    // Update demand factors based on resource type
                    if (iterator.resource == Resource.Lodging)
                    {
                        m_DemandFactors[9] += num6; // Tourist Demand
                    }
                    else if (iterator.resource == Resource.Petrochemicals)
                    {
                        m_DemandFactors[16] += num6; // Petrol Local Demand 
                    }
                    else
                    {
                        m_DemandFactors[4] += num6; // Local Demand 
                    }
                    m_DemandFactors[11] += num7; // Taxes 
                    m_DemandFactors[13] += math.min(0, num5 - num8); // Empty Buildings

                    num++; // Increment the counter for valid resources


                }
            }

            // Final adjustments for company and building demand
            m_CompanyDemand.value = ((num != 0) ? math.clamp(m_CompanyDemand.value / num, 0, 100) : 0);
            m_BuildingDemand.value = ((num != 0 && flag) ? math.clamp(m_BuildingDemand.value / num, 0, 100) : 0);

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

        [ReadOnly]
        public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentLookup<WorkplaceData> __Game_Prefabs_WorkplaceData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Population> __Game_City_Population_RO_ComponentLookup;



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref SystemState state)
        {
            __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
            __Game_Buildings_Renter_RO_BufferTypeHandle = state.GetBufferTypeHandle<Renter>(isReadOnly: true);
            __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
            __Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
            __Game_Companies_CommercialCompany_RO_ComponentLookup = state.GetComponentLookup<CommercialCompany>(isReadOnly: true);
            __Game_City_Tourism_RO_ComponentLookup = state.GetComponentLookup<Tourism>(isReadOnly: true);
            __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
            __Game_Prefabs_IndustrialProcessData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<IndustrialProcessData>(isReadOnly: true);
            __Game_Prefabs_WorkplaceData_RO_ComponentLookup = state.GetComponentLookup<WorkplaceData>(isReadOnly: true);
        }
    }

    private ResourceSystem m_ResourceSystem;

    private TaxSystem m_TaxSystem;

    private CountCompanyDataSystem m_CountCompanyDataSystem;

    private CountHouseholdDataSystem m_CountHouseholdDataSystem;

    private CitySystem m_CitySystem;

    private EntityQuery m_EconomyParameterQuery;

    private EntityQuery m_DemandParameterQuery;

    private EntityQuery m_FreeCommercialQuery;

    private EntityQuery m_CommercialProcessDataQuery;

    private EntityQuery m_UnlockedZoneDataQuery;

    private NativeValue<int> m_CompanyDemand;

    private NativeValue<int> m_BuildingDemand;

    public static NativeArray<DemandData> m_DemandData;

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

    private TypeHandle __TypeHandle;

    [DebugWatchValue(color = "#008fff")]
    public int companyDemand => m_LastCompanyDemand;

    [DebugWatchValue(color = "#2b6795")]
    public int buildingDemand => m_LastBuildingDemand;

    public override int GetUpdateInterval(SystemUpdatePhase phase)
    {
        return 16;
    }

    public override int GetUpdateOffset(SystemUpdatePhase phase)
    {
        return 4;
    }

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

    [Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
        m_TaxSystem = base.World.GetOrCreateSystemManaged<TaxSystem>();
        m_CountCompanyDataSystem = base.World.GetOrCreateSystemManaged<CountCompanyDataSystem>();
        m_CountHouseholdDataSystem = base.World.GetOrCreateSystemManaged<CountHouseholdDataSystem>();
        m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
        m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
        m_DemandParameterQuery = GetEntityQuery(ComponentType.ReadOnly<DemandParameterData>());
        m_FreeCommercialQuery = GetEntityQuery(ComponentType.ReadOnly<CommercialProperty>(), ComponentType.ReadOnly<PropertyOnMarket>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Abandoned>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Condemned>(), ComponentType.Exclude<Temp>());
        m_CommercialProcessDataQuery = GetEntityQuery(ComponentType.ReadOnly<IndustrialProcessData>(), ComponentType.ReadOnly<ServiceCompanyData>());
        m_UnlockedZoneDataQuery = GetEntityQuery(ComponentType.ReadOnly<ZoneData>(), ComponentType.Exclude<Locked>());
        m_CompanyDemand = new NativeValue<int>(Allocator.Persistent);
        m_BuildingDemand = new NativeValue<int>(Allocator.Persistent);
        m_DemandFactors = new NativeArray<int>(18, Allocator.Persistent);
        int resourceCount = EconomyUtils.ResourceCount;
        m_ResourceDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
        m_BuildingDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
        m_Consumption = new NativeArray<int>(resourceCount, Allocator.Persistent);
        m_FreeProperties = new NativeArray<int>(resourceCount, Allocator.Persistent);
        m_DemandData = new NativeArray<DemandData>(resourceCount, Allocator.Persistent);
        RequireForUpdate(m_EconomyParameterQuery);
        RequireForUpdate(m_DemandParameterQuery);
        RequireForUpdate(m_CommercialProcessDataQuery);
    }

    [Preserve]
    protected override void OnDestroy()
    {
        m_CompanyDemand.Dispose();
        m_BuildingDemand.Dispose();
        m_DemandFactors.Dispose();
        m_ResourceDemands.Dispose();
        m_BuildingDemands.Dispose();
        m_Consumption.Dispose();
        m_FreeProperties.Dispose();
        m_DemandData.Dispose();
        base.OnDestroy();
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

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        writer.Write(m_CompanyDemand.value);
        writer.Write(m_BuildingDemand.value);
        writer.Write(m_DemandFactors.Length);
        writer.Write(m_DemandFactors);
        writer.Write(m_ResourceDemands);
        writer.Write(m_BuildingDemands);
        writer.Write(m_Consumption);
        writer.Write(m_FreeProperties);
        writer.Write(m_LastCompanyDemand);
        writer.Write(m_LastBuildingDemand);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        reader.Read(out int value);
        m_CompanyDemand.value = value;
        reader.Read(out int value2);
        m_BuildingDemand.value = value2;
        if (reader.context.version < Version.demandFactorCountSerialization)
        {
            NativeArray<int> nativeArray = new NativeArray<int>(13, Allocator.Temp);
            reader.Read(nativeArray);
            CollectionUtils.CopySafe(nativeArray, m_DemandFactors);
            nativeArray.Dispose();
        }
        else
        {
            reader.Read(out int value3);
            if (value3 == m_DemandFactors.Length)
            {
                reader.Read(m_DemandFactors);
            }
            else
            {
                NativeArray<int> nativeArray2 = new NativeArray<int>(value3, Allocator.Temp);
                reader.Read(nativeArray2);
                CollectionUtils.CopySafe(nativeArray2, m_DemandFactors);
                nativeArray2.Dispose();
            }
        }
        reader.Read(m_ResourceDemands);
        reader.Read(m_BuildingDemands);
        NativeArray<int> value4 = default(NativeArray<int>);
        if (reader.context.version < Version.companyDemandOptimization)
        {
            value4 = new NativeArray<int>(EconomyUtils.ResourceCount, Allocator.Temp);
            reader.Read(value4);
        }
        reader.Read(m_Consumption);
        if (reader.context.version < Version.companyDemandOptimization)
        {
            reader.Read(value4);
            reader.Read(value4);
            reader.Read(value4);
        }
        reader.Read(m_FreeProperties);
        if (reader.context.version < Version.companyDemandOptimization)
        {
            reader.Read(value4);
            value4.Dispose();
        }
        reader.Read(out m_LastCompanyDemand);
        reader.Read(out m_LastBuildingDemand);
    }

    [Preserve]
    protected override void OnUpdate()
    {

        if (!m_DemandParameterQuery.IsEmptyIgnoreFilter && !m_EconomyParameterQuery.IsEmptyIgnoreFilter)
        {
            m_LastCompanyDemand = m_CompanyDemand.value;
            m_LastBuildingDemand = m_BuildingDemand.value;
            JobHandle deps;
            CountCompanyDataSystem.CommercialCompanyDatas commercialCompanyDatas = m_CountCompanyDataSystem.GetCommercialCompanyDatas(out deps);
            __TypeHandle.__Game_City_Tourism_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Companies_CommercialCompany_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_WorkplaceData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            __TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
            UpdateCommercialDemandJob updateCommercialDemandJob = default(UpdateCommercialDemandJob);
            updateCommercialDemandJob.m_FreePropertyChunks = m_FreeCommercialQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out var outJobHandle);
            updateCommercialDemandJob.m_CommercialProcessDataChunks = m_CommercialProcessDataQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out var outJobHandle2);
            updateCommercialDemandJob.m_UnlockedZoneDatas = m_UnlockedZoneDataQuery.ToComponentDataArray<ZoneData>(Allocator.TempJob);
            updateCommercialDemandJob.m_EntityType = __TypeHandle.__Unity_Entities_Entity_TypeHandle;
            updateCommercialDemandJob.m_ProcessType = __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentTypeHandle;
            updateCommercialDemandJob.m_WorkplaceDatas = __TypeHandle.__Game_Prefabs_WorkplaceData_RO_ComponentLookup;
            updateCommercialDemandJob.m_PrefabType = __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
            updateCommercialDemandJob.m_RenterType = __TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle;
            updateCommercialDemandJob.m_BuildingPropertyDatas = __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;
            updateCommercialDemandJob.m_ResourceDatas = __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup;
            updateCommercialDemandJob.m_CommercialCompanies = __TypeHandle.__Game_Companies_CommercialCompany_RO_ComponentLookup;
            updateCommercialDemandJob.m_ResourcePrefabs = m_ResourceSystem.GetPrefabs();
            updateCommercialDemandJob.m_DemandParameters = m_DemandParameterQuery.GetSingleton<DemandParameterData>();
            updateCommercialDemandJob.m_TaxRates = m_TaxSystem.GetTaxRates();
            updateCommercialDemandJob.m_CompanyDemand = m_CompanyDemand;
            updateCommercialDemandJob.m_BuildingDemand = m_BuildingDemand;
            updateCommercialDemandJob.m_DemandFactors = m_DemandFactors;
            updateCommercialDemandJob.m_City = m_CitySystem.City;
            updateCommercialDemandJob.m_ResourceDemands = m_ResourceDemands;
            updateCommercialDemandJob.m_BuildingDemands = m_BuildingDemands;
            updateCommercialDemandJob.m_ProduceCapacity = commercialCompanyDatas.m_ProduceCapacity;
            updateCommercialDemandJob.m_CurrentAvailables = commercialCompanyDatas.m_CurrentAvailables;
            updateCommercialDemandJob.m_ResourceNeeds = m_CountHouseholdDataSystem.GetResourceNeeds();
            updateCommercialDemandJob.m_FreeProperties = m_FreeProperties;
            updateCommercialDemandJob.m_Propertyless = commercialCompanyDatas.m_ServicePropertyless;
            updateCommercialDemandJob.m_Tourisms = __TypeHandle.__Game_City_Tourism_RO_ComponentLookup;
            updateCommercialDemandJob.m_DemandData = m_DemandData; // MODDED
            updateCommercialDemandJob.m_MaxServiceWorkers = commercialCompanyDatas.m_MaxServiceWorkers;
            updateCommercialDemandJob.m_CurrentServiceWorkers = commercialCompanyDatas.m_CurrentServiceWorkers;
            updateCommercialDemandJob.m_ProduceCapacity = commercialCompanyDatas.m_ProduceCapacity;
            updateCommercialDemandJob.m_TotalAvailables = commercialCompanyDatas.m_TotalAvailables;
            updateCommercialDemandJob.m_ServiceCompanies = commercialCompanyDatas.m_ServiceCompanies;
            updateCommercialDemandJob.TaxRateEffect = Mod.setting.TaxRateEffect;

            UpdateCommercialDemandJob jobData = updateCommercialDemandJob;
            base.Dependency = IJobExtensions.Schedule(jobData, JobUtils.CombineDependencies(base.Dependency, m_ReadDependencies, outJobHandle, outJobHandle2, deps));
            m_WriteDependencies = base.Dependency;
            m_CountHouseholdDataSystem.AddHouseholdResourceNeedReader(base.Dependency);
            m_ResourceSystem.AddPrefabsReader(base.Dependency);
            m_TaxSystem.AddReader(base.Dependency);


        }


    }



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void __AssignQueries(ref SystemState state)
    {
    }

    protected override void OnCreateForCompiler()
    {
        base.OnCreateForCompiler();
        __AssignQueries(ref base.CheckedStateRef);
        __TypeHandle.__AssignHandles(ref base.CheckedStateRef);
    }

    [Preserve]
    public CustomCommercialDemandSystem()
    {
    }
}