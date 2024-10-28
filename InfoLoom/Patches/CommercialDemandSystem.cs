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
using System.Reflection;
using InfoLoom;
using static InfoLoomBrucey.Systems.CommercialUISystem;

namespace InfoLoomBrucey.Systems;

[CompilerGenerated]
public partial class CommercialDemandUISystem : UISystemBase
{
    [BurstCompile]
    private struct UpdateCommercialDemandJob : IJob
    {
        [ReadOnly]
        public NativeList<ArchetypeChunk> m_FreePropertyChunks;

        [ReadOnly]
        public NativeList<ArchetypeChunk> m_CommercialProcessDataChunks;

        [ReadOnly]
        public EntityTypeHandle m_EntityType;

        [ReadOnly]
        public ComponentTypeHandle<PrefabRef> m_PrefabType;

        [ReadOnly]
        public ComponentTypeHandle<IndustrialProcessData> m_ProcessType;

        [ReadOnly]
        public BufferTypeHandle<Renter> m_RenterType;

        [ReadOnly]
        public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDatas;

        [ReadOnly]
        public ComponentLookup<ResourceData> m_ResourceDatas;

        [ReadOnly]
        public ComponentLookup<WorkplaceData> m_WorkplaceDatas;

        [ReadOnly]
        public ComponentLookup<CommercialCompany> m_CommercialCompanies;

        [ReadOnly]
        public ComponentLookup<Population> m_Populations;

        [ReadOnly]
        public ComponentLookup<Tourism> m_Tourisms;

        [ReadOnly]
        public ResourcePrefabs m_ResourcePrefabs;

        public EconomyParameterData m_EconomyParameters;

        public DemandParameterData m_DemandParameters;

        [ReadOnly]
        public NativeArray<int> m_EmployableByEducation;

        [ReadOnly]
        public NativeArray<int> m_TaxRates;

        [ReadOnly]
        public NativeArray<int> m_FreeWorkplaces;

        public float m_BaseConsumptionSum;

        public Entity m_City;

        public NativeValue<int> m_CompanyDemand;

        public NativeValue<int> m_BuildingDemand;

        public NativeArray<int> m_DemandFactors;

        public NativeArray<int> m_Consumptions;

        public NativeArray<int> m_FreeProperties;

        public NativeArray<int> m_ResourceDemands;

        public NativeArray<int> m_BuildingDemands;

        [ReadOnly]
        public NativeArray<int> m_Productions;

        [ReadOnly]
        public NativeArray<int> m_TotalAvailables;

        [ReadOnly]
        public NativeArray<int> m_TotalMaximums;

        [ReadOnly]
        public NativeArray<int> m_Companies;

        [ReadOnly]
        public NativeArray<int> m_Propertyless;

        [ReadOnly]
        public NativeArray<int> m_TotalMaxWorkers;

        [ReadOnly]
        public NativeArray<int> m_TotalCurrentWorkers;

        public NativeArray<int> m_ActualConsumptions;

        public NativeArray<DemandData> m_DemandData;

        [ReadOnly]
        public NativeArray<int> m_ResourceNeeds;

        public void Execute()
        {
            ResourceIterator iterator = ResourceIterator.GetIterator();
            while (iterator.Next())
            {
                int resourceIndex = EconomyUtils.GetResourceIndex(iterator.resource);
                m_Consumptions[resourceIndex] = math.max(m_ResourceNeeds[resourceIndex], m_ActualConsumptions[resourceIndex]);
                m_FreeProperties[resourceIndex] = 0;
            }
           
            for (int i = 0; i < m_DemandFactors.Length; i++)
            {
                m_DemandFactors[i] = 0;
            }
            for (int j = 0; j < m_FreePropertyChunks.Length; j++)
            {
                ArchetypeChunk archetypeChunk = m_FreePropertyChunks[j];
                NativeArray<PrefabRef> nativeArray = archetypeChunk.GetNativeArray(ref m_PrefabType);
                BufferAccessor<Renter> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_RenterType);
                for (int k = 0; k < nativeArray.Length; k++)
                {
                    Entity prefab = nativeArray[k].m_Prefab;
                    if (!m_BuildingPropertyDatas.HasComponent(prefab))
                    {
                        continue;
                    }
                    bool flag = false;
                    DynamicBuffer<Renter> dynamicBuffer = bufferAccessor[k];
                    for (int l = 0; l < dynamicBuffer.Length; l++)
                    {
                        if (m_CommercialCompanies.HasComponent(dynamicBuffer[l].m_Renter))
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (flag)
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
            bool flag2 = m_BuildingDemand.value > 0;
            m_BuildingDemand.value = 0;
            iterator = ResourceIterator.GetIterator();
            int num = 0;
            while (iterator.Next())
            {
                int resourceIndex2 = EconomyUtils.GetResourceIndex(iterator.resource);
                if (!m_ResourceDatas.HasComponent(m_ResourcePrefabs[iterator.resource]))
                {
                    continue;
                }
                ResourceData resourceData = m_ResourceDatas[m_ResourcePrefabs[iterator.resource]];
                if (!EconomyUtils.GetProcessComplexity(m_CommercialProcessDataChunks, m_WorkplaceDatas, iterator.resource, m_EntityType, m_ProcessType, out var complexity))
                {
                    continue;
                }
                Workplaces workplaces = WorkProviderSystem.CalculateNumberOfWorkplaces(20, complexity, 1);
                float num2 = 0f;
                for (int m = 0; m < 5; m++)
                {
                    num2 = ((m >= 2) ? (num2 + math.min(5f * (float)workplaces[m], math.max(0, m_EmployableByEducation[m] - m_FreeWorkplaces[m]))) : (num2 + 5f * (float)workplaces[m]));
                }
                float num3 = 0.4f * (num2 / 50f - 1f);
                float num4 = -9f + 10f * (((float)m_TotalCurrentWorkers[resourceIndex2] + 1f) / ((float)m_TotalMaxWorkers[resourceIndex2] + 1f));
                if (num4 > 0f)
                {
                    num4 *= 0.5f;
                }
                float num5 = ((m_TotalMaximums[resourceIndex2] == 0) ? 0f : (-2.5f + 5f * (1f - (float)m_TotalAvailables[resourceIndex2] / (float)m_TotalMaximums[resourceIndex2])));
                float num6 = (m_DemandParameters.m_CommercialBaseDemand * (float)m_Consumptions[resourceIndex2] - (float)m_Productions[resourceIndex2]) / math.max(100f, (float)m_Consumptions[resourceIndex2] + 1f);
                float num7 = -0.2f * ((float)TaxSystem.GetCommercialTaxRate(iterator.resource, m_TaxRates) - 10f);
                m_ResourceDemands[resourceIndex2] = Mathf.RoundToInt(100f * (num5 + num4 + num3 + num7 + num6));

                // Update DemandData for UI
                DemandData uiData = m_DemandData[resourceIndex2];
                uiData.Resource = iterator.resource;
                uiData.Demand = m_ResourceDemands[resourceIndex2];
                uiData.Building = m_BuildingDemands[resourceIndex2];
                uiData.Free = m_FreeProperties[resourceIndex2];
                uiData.Companies = m_Companies[resourceIndex2];
                uiData.Workers = m_TotalCurrentWorkers[resourceIndex2];
                uiData.SvcFactor = Mathf.RoundToInt(100f * num5);
                uiData.SvcPercent = (m_TotalMaximums[resourceIndex2] == 0 ? 0 : 100 * m_TotalAvailables[resourceIndex2] / m_TotalMaximums[resourceIndex2]);
                uiData.CapFactor = Mathf.RoundToInt(100f * num6);
                uiData.CapPercent = 100 * m_Productions[resourceIndex2] / math.max(100, m_Consumptions[resourceIndex2]);
                uiData.CapPerCompany = (m_Companies[resourceIndex2] == 0 ? 0 : m_Productions[resourceIndex2] / m_Companies[resourceIndex2]);
                uiData.WrkFactor = Mathf.RoundToInt(100f * num4);
                uiData.WrkPercent = 100 * (m_TotalCurrentWorkers[resourceIndex2] + 1) / (m_TotalMaxWorkers[resourceIndex2] + 1);
                uiData.EduFactor = Mathf.RoundToInt(100f * num3);
                uiData.TaxFactor = Mathf.RoundToInt(100f * num7);
                m_DemandData[resourceIndex2] = uiData;

                int num8 = m_ResourceDemands[resourceIndex2];
                if (m_FreeProperties[resourceIndex2] == 0)
                {
                    m_ResourceDemands[resourceIndex2] = 0;
                }
                if (m_Consumptions[resourceIndex2] > 0)
                {
                    m_CompanyDemand.value += Mathf.RoundToInt(math.min(100, math.max(0, m_ResourceDemands[resourceIndex2])));
                    m_BuildingDemands[resourceIndex2] = math.max(0, Mathf.CeilToInt(math.min(math.max(1f, (float)math.min(1, m_Propertyless[resourceIndex2]) + (float)m_Companies[resourceIndex2] / m_DemandParameters.m_FreeCommercialProportion) - (float)m_FreeProperties[resourceIndex2], num8)));
                    if (m_BuildingDemands[resourceIndex2] > 0)
                    {
                        m_BuildingDemand.value += ((m_BuildingDemands[resourceIndex2] > 0) ? num8 : 0);
                    }
                }
                if (!flag2 || (m_BuildingDemands[resourceIndex2] > 0 && num8 > 0))
                {
                    int num9 = ((m_BuildingDemands[resourceIndex2] > 0) ? num8 : 0);
                    int demandFactorEffect = DemandUtils.GetDemandFactorEffect(m_ResourceDemands[resourceIndex2], num3);
                    int demandFactorEffect2 = DemandUtils.GetDemandFactorEffect(m_ResourceDemands[resourceIndex2], num4);
                    int num10 = DemandUtils.GetDemandFactorEffect(m_ResourceDemands[resourceIndex2], num6) + DemandUtils.GetDemandFactorEffect(m_ResourceDemands[resourceIndex2], num5);
                    int demandFactorEffect3 = DemandUtils.GetDemandFactorEffect(m_ResourceDemands[resourceIndex2], num7);
                    int num11 = demandFactorEffect + demandFactorEffect2 + num10 + demandFactorEffect3;
                    m_DemandFactors[2] += demandFactorEffect;
                    m_DemandFactors[1] += demandFactorEffect2;
                    if (iterator.resource == Resource.Lodging)
                    {
                        m_DemandFactors[9] += num10;
                    }
                    else if (iterator.resource == Resource.Petrochemicals)
                    {
                        m_DemandFactors[16] += num10;
                    }
                    else
                    {
                        m_DemandFactors[4] += num10;
                    }
                    m_DemandFactors[11] += demandFactorEffect3;
                    m_DemandFactors[13] += math.min(0, num9 - num11);
                }
                num++;
                m_ResourceDemands[resourceIndex2] = math.min(100, math.max(0, m_ResourceDemands[resourceIndex2]));
            }
            m_BuildingDemand.value = math.clamp(2 * m_BuildingDemand.value / num, 0, 100);
        }
    }

    private struct TypeHandle
    {
        [ReadOnly]
        public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentTypeHandle;

        [ReadOnly]
        public BufferTypeHandle<Renter> __Game_Buildings_Renter_RO_BufferTypeHandle;

        [ReadOnly]
        public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<WorkplaceData> __Game_Prefabs_WorkplaceData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<CommercialCompany> __Game_Companies_CommercialCompany_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Population> __Game_City_Population_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Tourism> __Game_City_Tourism_RO_ComponentLookup;

        public void __AssignHandles(ref SystemState state)
        {
            __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
            __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(true);
            __Game_Prefabs_IndustrialProcessData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<IndustrialProcessData>(true);
            __Game_Buildings_Renter_RO_BufferTypeHandle = state.GetBufferTypeHandle<Renter>(true);
            __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(true);
            __Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(true);
            __Game_Prefabs_WorkplaceData_RO_ComponentLookup = state.GetComponentLookup<WorkplaceData>(true);
            __Game_Companies_CommercialCompany_RO_ComponentLookup = state.GetComponentLookup<CommercialCompany>(true);
            __Game_City_Population_RO_ComponentLookup = state.GetComponentLookup<Population>(true);
            __Game_City_Tourism_RO_ComponentLookup = state.GetComponentLookup<Tourism>(true);
        }
    }

    private SimulationSystem m_SimulationSystem;
    private CountCompanyDataSystem m_CountCompanyDataSystem;
    private ResourceSystem m_ResourceSystem;
    private TaxSystem m_TaxSystem;
    private CountEmploymentSystem m_CountEmploymentSystem;
    private CountWorkplacesSystem m_CountWorkplacesSystem;
    private CountConsumptionSystem m_CountConsumptionSystem;
    
    private CitySystem m_CitySystem;
    private CountHouseholdDataSystem m_CountHouseholdDataSystem;

    private EntityQuery m_EconomyParameterQuery;
    private EntityQuery m_DemandParameterQuery;
    private EntityQuery m_FreeCommercialQuery;
    private EntityQuery m_CommercialProcessDataQuery;

    private NativeValue<int> m_CompanyDemand;
    private NativeValue<int> m_BuildingDemand;
    private NativeArray<int> m_DemandFactors;
    private NativeArray<int> m_ResourceDemands;
    private NativeArray<int> m_BuildingDemands;
    private NativeArray<int> m_Consumption;
    private NativeArray<int> m_FreeProperties;
    private NativeArray<float> m_EstimatedConsumptionPerCim;
    private NativeArray<float> m_ActualConsumptionPerCim;

    private RawValueBinding m_uiResults;
    private RawValueBinding m_uiExResources;

    private NativeArray<int> m_Results;
    private NativeValue<Resource> m_ExcludedResources;

    private TypeHandle __TypeHandle;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
        m_CountCompanyDataSystem = World.GetOrCreateSystemManaged<CountCompanyDataSystem>();
        m_ResourceSystem = World.GetOrCreateSystemManaged<ResourceSystem>();
        m_TaxSystem = World.GetOrCreateSystemManaged<TaxSystem>();
        m_CountEmploymentSystem = World.GetOrCreateSystemManaged<CountEmploymentSystem>();
        m_CountWorkplacesSystem = World.GetOrCreateSystemManaged<CountWorkplacesSystem>();
        m_CountConsumptionSystem = World.GetOrCreateSystemManaged<CountConsumptionSystem>();
       
        m_CitySystem = World.GetOrCreateSystemManaged<CitySystem>();
        m_CountHouseholdDataSystem = World.GetOrCreateSystemManaged<CountHouseholdDataSystem>();

        m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
        m_DemandParameterQuery = GetEntityQuery(ComponentType.ReadOnly<DemandParameterData>());
        m_FreeCommercialQuery = GetEntityQuery(ComponentType.ReadOnly<CommercialProperty>(), ComponentType.ReadOnly<PropertyOnMarket>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Abandoned>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Condemned>(), ComponentType.Exclude<Temp>());
        m_CommercialProcessDataQuery = GetEntityQuery(ComponentType.ReadOnly<IndustrialProcessData>(), ComponentType.ReadOnly<ServiceCompanyData>());

        int resourceCount = EconomyUtils.ResourceCount;
        m_CompanyDemand = new NativeValue<int>(Allocator.Persistent);
        m_BuildingDemand = new NativeValue<int>(Allocator.Persistent);
        m_DemandFactors = new NativeArray<int>(18, Allocator.Persistent);
        m_ResourceDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
        m_BuildingDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
        m_Consumption = new NativeArray<int>(resourceCount, Allocator.Persistent);
        m_FreeProperties = new NativeArray<int>(resourceCount, Allocator.Persistent);
        m_EstimatedConsumptionPerCim = new NativeArray<float>(resourceCount, Allocator.Persistent);
        m_ActualConsumptionPerCim = new NativeArray<float>(resourceCount, Allocator.Persistent);

        m_Results = new NativeArray<int>(10, Allocator.Persistent);
        m_ExcludedResources = new NativeValue<Resource>(Allocator.Persistent);

        RequireForUpdate(m_EconomyParameterQuery);
        RequireForUpdate(m_DemandParameterQuery);
    }

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

            __TypeHandle.__AssignHandles(ref CheckedStateRef);

            UpdateCommercialDemandJob updateCommercialDemandJob = new UpdateCommercialDemandJob
            {
                m_FreePropertyChunks = m_FreeCommercialQuery.ToArchetypeChunkListAsync(World.UpdateAllocator.ToAllocator, out var outJobHandle),
                m_CommercialProcessDataChunks = m_CommercialProcessDataQuery.ToArchetypeChunkListAsync(World.UpdateAllocator.ToAllocator, out var outJobHandle2),
                m_EntityType = __TypeHandle.__Unity_Entities_Entity_TypeHandle,
                m_PrefabType = __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle,
                m_ProcessType = __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentTypeHandle,
                m_RenterType = __TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle,
                m_BuildingPropertyDatas = __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup,
                m_ResourceDatas = __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup,
                m_WorkplaceDatas = __TypeHandle.__Game_Prefabs_WorkplaceData_RO_ComponentLookup,
                m_CommercialCompanies = __TypeHandle.__Game_Companies_CommercialCompany_RO_ComponentLookup,
                m_Populations = __TypeHandle.__Game_City_Population_RO_ComponentLookup,
                m_Tourisms = __TypeHandle.__Game_City_Tourism_RO_ComponentLookup,
                m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
                m_DemandParameters = m_DemandParameterQuery.GetSingleton<DemandParameterData>(),
                m_EconomyParameters = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
               
                m_TaxRates = m_TaxSystem.GetTaxRates(),
               
                m_BaseConsumptionSum = m_ResourceSystem.BaseConsumptionSum,
                m_CompanyDemand = m_CompanyDemand,
                m_BuildingDemand = m_BuildingDemand,
                m_DemandFactors = m_DemandFactors,
                m_ResourceDemands = m_ResourceDemands,
                m_BuildingDemands = m_BuildingDemands,
              
                m_Consumptions = m_Consumption,
                m_TotalAvailables = commercialCompanyDatas.m_CurrentAvailables,
                m_TotalMaximums = commercialCompanyDatas.m_TotalAvailables,
                m_Companies = commercialCompanyDatas.m_ServiceCompanies,
                m_FreeProperties = m_FreeProperties,
                m_Propertyless = commercialCompanyDatas.m_ServicePropertyless,
                m_TotalMaxWorkers = commercialCompanyDatas.m_MaxServiceWorkers,
                m_TotalCurrentWorkers = commercialCompanyDatas.m_CurrentServiceWorkers,
                m_City = m_CitySystem.City,
                m_ActualConsumptions = m_CountConsumptionSystem.GetConsumptions(out var deps4),
              
                m_ResourceNeeds = m_CountHouseholdDataSystem.GetResourceNeeds(),
            };

            JobHandle combinedDeps = JobHandle.CombineDependencies(
                JobHandle.CombineDependencies(Dependency, deps, deps4),
                outJobHandle,
                outJobHandle2
            );

            Dependency = IJobExtensions.Schedule(updateCommercialDemandJob, combinedDeps);
            Dependency.Complete();

            m_uiResults.Update();
            m_uiExResources.Update();
        }
    }

    private void ResetResults()
    {
        m_ExcludedResources.value = Resource.NoResource;
        m_Results.Fill<int>(0);
    }

    protected override void OnDestroy()
    {
        m_CompanyDemand.Dispose();
        m_BuildingDemand.Dispose();
        m_DemandFactors.Dispose();
        m_ResourceDemands.Dispose();
        m_BuildingDemands.Dispose();
        m_Consumption.Dispose();
        m_FreeProperties.Dispose();
        m_EstimatedConsumptionPerCim.Dispose();
        m_ActualConsumptionPerCim.Dispose();
        m_Results.Dispose();
        m_ExcludedResources.Dispose();
        base.OnDestroy();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void __AssignQueries(ref SystemState state)
    {
        // Implement if necessary
    }

    protected override void OnCreateForCompiler()
    {
        base.OnCreateForCompiler();
        __AssignQueries(ref CheckedStateRef);
        __TypeHandle.__AssignHandles(ref CheckedStateRef);
    }

    [Preserve]
    public CommercialDemandUISystem()
    {
    }
}
