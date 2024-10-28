using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Debug;
using Game.Economy;
using Game.Prefabs;
using Game.Reflection;
using Game.Tools;
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
using Colossal.UI.Binding;
using System.Collections.Generic;
using InfoLoom;
using System;
using System.Linq;

namespace InfoLoomBrucey.Systems;

[CompilerGenerated]
public partial class CommercialDemandUISystem : UISystemBase
{
    private struct UpdateCommercialDemandJob : IJob
    {
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

        public NativeArray<int> m_Results;

        public NativeValue<Resource> m_ExcludedResources;


        [ReadOnly]
        public NativeArray<int> m_EmployableByEducation;

        [ReadOnly]
        public Workplaces m_FreeWorkplaces;

        [ReadOnly] public NativeArray<int> m_TotalMaximums;
        [ReadOnly] public NativeArray<int> m_TotalAvailables;
        [ReadOnly] public NativeArray<int> m_Productions;
        [ReadOnly] public NativeArray<int> m_ActualConsumptions;
        [ReadOnly] public NativeArray<int> m_TotalMaxWorkers;
        [ReadOnly] public NativeArray<int> m_TotalCurrentWorkers;

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

            // Count free properties
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
                    if (buildingPropertyData.m_AllowedSold != Resource.NoResource)
                    {
                        m_Results[0]++;
                    }
                }
            }

            m_CompanyDemand.value = 0;
            m_BuildingDemand.value = 0;
            iterator = ResourceIterator.GetIterator();
            int num = 0;
            int numStandard = 0, numLeisure = 0;
            float capUtilStd = 0f, capUtilLei = 0f, salesCapStd = 0f, salesCapLei = 0f;
            float taxRate = 0f;
            int totalPropertyless = 0;
            int numDemanded = 0;
            int educatedWorkforce = 0, uneducatedWorkforce = 0;

            // Calculate available workforce
            for (int i = 0; i < 5; i++)
            {
                int employable = math.max(0, m_EmployableByEducation[i] - m_FreeWorkplaces[i]);
                if (i >= 2)
                    educatedWorkforce += employable;
                else
                    uneducatedWorkforce += employable;
            }

            float totalEmployeeCapacity = 0f;
            int resourcesWithEmployees = 0;

            while (iterator.Next())
            {
                int resourceIndex = EconomyUtils.GetResourceIndex(iterator.resource);
                if (!EconomyUtils.IsCommercialResource(iterator.resource) || !m_ResourceDatas.HasComponent(m_ResourcePrefabs[iterator.resource]))
                    continue;

                ResourceData resourceData = m_ResourceDatas[m_ResourcePrefabs[iterator.resource]];

                // Calculate utilization using same logic as vanilla game
                float capUtil = math.select(
                    0.3f,  // Default utilization when no maximum capacity (matches vanilla)
                    1f - (float)m_CurrentAvailables[resourceIndex] / math.max(1, (float)m_TotalMaximums[resourceIndex]),
                    m_TotalMaximums[resourceIndex] > 0
                );

                // Calculate sales capacity using vanilla game formula
                float salesCapacity = math.select(
                    0f,
                    (float)m_ProduceCapacity[resourceIndex] / math.max(100f, (float)m_ResourceNeeds[resourceIndex]),
                    m_ResourceNeeds[resourceIndex] > 0
                );

                // Get tax rate using vanilla method
                float resourceTaxRate = (float)TaxSystem.GetCommercialTaxRate(iterator.resource, m_TaxRates);

                // Aggregate metrics based on resource type
                if (resourceData.m_IsLeisure)
                {
                    numLeisure++;
                    capUtilLei += capUtil;
                    salesCapLei += salesCapacity;
                }
                else
                {
                    numStandard++;
                    capUtilStd += capUtil;
                    salesCapStd += salesCapacity;
                }
                taxRate += resourceTaxRate;
                num++;
            }

            // Calculate the average employee capacity
            float averageEmployeeCapacity = (resourcesWithEmployees > 0) ? (totalEmployeeCapacity / resourcesWithEmployees) : 0f;

            // Calculate final results
            m_Results[0] = m_FreeProperties.Sum();  // Total free properties
            m_Results[1] = totalPropertyless;
            m_Results[2] = Mathf.RoundToInt(math.select(0f, 10f * taxRate / (float)(numStandard + numLeisure), numStandard + numLeisure != 0));
            m_Results[3] = Mathf.RoundToInt(math.select(0f, 100f * capUtilStd / (float)numStandard, numStandard != 0));
            m_Results[4] = Mathf.RoundToInt(math.select(0f, 100f * capUtilLei / (float)numLeisure, numLeisure != 0));
            m_Results[5] = Mathf.RoundToInt(math.select(0f, 100f * salesCapStd / (float)numStandard, numStandard != 0));
            m_Results[6] = Mathf.RoundToInt(math.select(0f, 100f * salesCapLei / (float)numLeisure, numLeisure != 0));
            m_Results[7] = Mathf.RoundToInt(1000f * averageEmployeeCapacity);
            m_Results[8] = educatedWorkforce;
            m_Results[9] = uneducatedWorkforce;

            m_CompanyDemand.value = math.select(0, math.clamp(m_CompanyDemand.value / num, 0, 100), num != 0);
            m_BuildingDemand.value = math.select(0, math.clamp(2 * m_BuildingDemand.value / num, 0, 100), num != 0);
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
        public ComponentLookup<WorkplaceData> __Game_Prefabs_WorkplaceData_RO_ComponentLookup;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref SystemState state)
        {
            __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
            __Game_Buildings_Renter_RO_BufferTypeHandle = state.GetBufferTypeHandle<Renter>(isReadOnly: true);
            __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
            __Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
            __Game_Companies_CommercialCompany_RO_ComponentLookup = state.GetComponentLookup<CommercialCompany>(isReadOnly: true);
            __Game_City_Tourism_RO_ComponentLookup = state.GetComponentLookup<Tourism>(isReadOnly: true);
            __Game_Prefabs_WorkplaceData_RO_ComponentLookup = state.GetComponentLookup<WorkplaceData>(isReadOnly: true);
        }
    }

    private const string kGroup = "cityInfo";

    private SimulationSystem m_SimulationSystem;
    private ResourceSystem m_ResourceSystem;
    private TaxSystem m_TaxSystem;
    private CountCompanyDataSystem m_CountCompanyDataSystem;
    private CountHouseholdDataSystem m_CountHouseholdDataSystem;
    private CitySystem m_CitySystem;
    private CountWorkplacesSystem m_CountWorkplacesSystem;
    private EntityQuery m_EconomyParameterQuery;
    private EntityQuery m_DemandParameterQuery;
    private EntityQuery m_FreeCommercialQuery;
    private EntityQuery m_CommercialProcessDataQuery;
    private EntityQuery m_UnlockedZoneDataQuery;

    private NativeValue<int> m_CompanyDemand;
    private NativeValue<int> m_BuildingDemand;
    private NativeArray<int> m_DemandFactors;
    private NativeArray<int> m_ResourceDemands;
    private NativeArray<int> m_BuildingDemands;
    private NativeArray<int> m_Consumption;
    private NativeArray<int> m_FreeProperties;

    private JobHandle m_ReadDependencies;

    private TypeHandle __TypeHandle;

    // InfoLoom specific fields
    private RawValueBinding m_uiResults;
    private RawValueBinding m_uiExResources;
    private NativeArray<int> m_Results;
    private NativeValue<Resource> m_ExcludedResources;

    public override GameMode gameMode => GameMode.Game;

    [Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
        m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
        m_TaxSystem = base.World.GetOrCreateSystemManaged<TaxSystem>();
        m_CountCompanyDataSystem = base.World.GetOrCreateSystemManaged<CountCompanyDataSystem>();
        m_CountHouseholdDataSystem = base.World.GetOrCreateSystemManaged<CountHouseholdDataSystem>();
        m_CountWorkplacesSystem = base.World.GetOrCreateSystemManaged<CountWorkplacesSystem>();
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
        RequireForUpdate(m_EconomyParameterQuery);
        RequireForUpdate(m_DemandParameterQuery);
        RequireForUpdate(m_CommercialProcessDataQuery);

        // InfoLoom specific initialization
        m_Results = new NativeArray<int>(10, Allocator.Persistent);
        m_ExcludedResources = new NativeValue<Resource>(Allocator.Persistent);

        AddBinding(m_uiResults = new RawValueBinding(kGroup, "ilCommercial", delegate (IJsonWriter binder)
        {
            binder.ArrayBegin(m_Results.Length);
            for (int i = 0; i < m_Results.Length; i++)
                binder.Write(m_Results[i]);
            binder.ArrayEnd();
        }));

        AddBinding(m_uiExResources = new RawValueBinding(kGroup, "ilCommercialExRes", delegate (IJsonWriter binder)
        {
            List<string> resList = new List<string>();
            for (int i = 0; i < Game.Economy.EconomyUtils.ResourceCount; i++)
                if ((m_ExcludedResources.value & Game.Economy.EconomyUtils.GetResource(i)) != Resource.NoResource)
                    resList.Add(Game.Economy.EconomyUtils.GetName(Game.Economy.EconomyUtils.GetResource(i)));
            binder.ArrayBegin(resList.Count);
            foreach (string res in resList)
                binder.Write(res);
            binder.ArrayEnd();
        }));

        Mod.log.Info("CommercialDemandUISystem created.");
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
        m_Results.Dispose();
        m_ExcludedResources.Dispose();
        base.OnDestroy();
    }
    public void SetDefaults() //Context context)
    {
        //m_CompanyDemand.value = 0;
        m_BuildingDemand.value = 50; // Infixo: default is 0 which is no demand, let's start with some demand
        m_DemandFactors.Fill(0);
        m_ResourceDemands.Fill(0);
        m_BuildingDemands.Fill(0);
        m_Consumption.Fill(0);
        m_FreeProperties.Fill(0);
        m_Results.Fill(0);
        m_ExcludedResources.value = Resource.NoResource;
    }
    [Preserve]
    protected override void OnUpdate()
    {
        if (m_SimulationSystem.frameIndex % 128 != 55)
            return;

        base.OnUpdate();
        ResetResults();

        if (!m_DemandParameterQuery.IsEmptyIgnoreFilter && !m_EconomyParameterQuery.IsEmptyIgnoreFilter)
        {
            JobHandle deps;
            CountCompanyDataSystem.CommercialCompanyDatas commercialCompanyDatas = m_CountCompanyDataSystem.GetCommercialCompanyDatas(out deps);
            __TypeHandle.__Game_City_Tourism_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Companies_CommercialCompany_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);

            UpdateCommercialDemandJob updateCommercialDemandJob = default(UpdateCommercialDemandJob);
            updateCommercialDemandJob.m_FreePropertyChunks = m_FreeCommercialQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out var outJobHandle);
            updateCommercialDemandJob.m_UnlockedZoneDatas = m_UnlockedZoneDataQuery.ToComponentDataArray<ZoneData>(Allocator.TempJob);
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
            updateCommercialDemandJob.m_Results = m_Results;
            updateCommercialDemandJob.m_ExcludedResources = m_ExcludedResources;
            updateCommercialDemandJob.m_EmployableByEducation = m_CountHouseholdDataSystem.GetEmployables();
            updateCommercialDemandJob.m_FreeWorkplaces = m_CountWorkplacesSystem.GetFreeWorkplaces();
            updateCommercialDemandJob.m_TotalMaximums = commercialCompanyDatas.m_TotalAvailables;
            updateCommercialDemandJob.m_TotalAvailables = commercialCompanyDatas.m_CurrentAvailables;
            updateCommercialDemandJob.m_TotalMaxWorkers = commercialCompanyDatas.m_MaxServiceWorkers;
            updateCommercialDemandJob.m_TotalCurrentWorkers = commercialCompanyDatas.m_CurrentServiceWorkers;

            UpdateCommercialDemandJob jobData = updateCommercialDemandJob;
            IJobExtensions.Schedule(jobData, JobUtils.CombineDependencies(base.Dependency, m_ReadDependencies, outJobHandle, deps)).Complete();
        }

        // Update UI
        m_uiResults.Update();
        m_uiExResources.Update();
    }

    private void ResetResults()
    {
        m_ExcludedResources.value = Resource.NoResource;
        m_Results.Fill<int>(0);
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
    public CommercialDemandUISystem()
    {
    }
}
