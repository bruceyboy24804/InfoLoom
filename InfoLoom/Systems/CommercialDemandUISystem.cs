using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
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
using Game.Tools;
using Game.UI;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace InfoLoom.Systems;

[CompilerGenerated]
public partial class CommercialDemandUISystem : UISystemBase
{
    private const string kGroup = "cityInfo";

    private SimulationSystem m_SimulationSystem;
    private ResourceSystem m_ResourceSystem;
    private TaxSystem m_TaxSystem;
    private CountEmploymentSystem m_CountEmploymentSystem;
    private CountCompanyDataSystem m_CountCompanyDataSystem;
    private CountConsumptionSystem m_CountConsumptionSystem;
    private CountFreeWorkplacesSystem m_CountFreeWorkplacesSystem;
    private CitySystem m_CitySystem;
    private CountHouseholdDataSystem m_CountHouseholdDataSystem;

    // Queries and data arrays
    private EntityQuery m_EconomyParameterQuery;
    private EntityQuery m_DemandParameterQuery;
    private EntityQuery m_FreeCommercialQuery;
    private EntityQuery m_CommercialProcessDataQuery;
    private NativeValue<int> m_BuildingDemand;

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
    private JobHandle m_ReadDependencies;
    private JobHandle m_WriteDependencies;

    private TypeHandle __TypeHandle;

    [ResourceArray]
    [DebugWatchValue]
    private NativeArray<float> m_EstimatedConsumptionPerCim;

    [ResourceArray]
    [DebugWatchValue]
    private NativeArray<float> m_ActualConsumptionPerCim;

    // UI Bindings
    private RawValueBinding m_uiResults;
    private RawValueBinding m_uiExResources;

    // Results and Excluded Resources
    private NativeArray<int> m_Results;
    private NativeValue<Resource> m_ExcludedResources;

    // COMMERCIAL
    // 0 - free properties
    // 1 - propertyless companies
    // 2 - tax rate
    // 3 & 4 - service utilization rate (available/maximum), non-leisure/leisure
    // 5 & 6 - sales efficiency (sales capacity/consumption), non-leisure/leisure // how effectively a shop is utilizing its sales capacity by comparing the actual sales to the maximum sales potential
    // 7 - employee capacity ratio // how efficiently the company is utilizing its workforce by comparing the actual number of employees to the maximum number it could employ
    // 8 & 9 - educated & uneducated workforce

    // 240209 Set gameMode to avoid errors in the Editor
    public override GameMode gameMode => GameMode.Game;

    // ZoneSpawnSystem and CommercialSpawnSystem are using this
    // Both are called in SystemUpdatePhase.GameSimulation
    // so in UIUpdate they should already be finished (because of EndFrameBarrier)

    [Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();

        // Initialize systems
        m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();         m_ResourceSystem = World.GetOrCreateSystemManaged<ResourceSystem>();
        m_TaxSystem = World.GetOrCreateSystemManaged<TaxSystem>();
        m_CountEmploymentSystem = World.GetOrCreateSystemManaged<CountEmploymentSystem>();
        m_CountCompanyDataSystem = World.GetOrCreateSystemManaged<CountCompanyDataSystem>();
        m_CountFreeWorkplacesSystem = World.GetOrCreateSystemManaged<CountFreeWorkplacesSystem>();
        m_CountConsumptionSystem = World.GetOrCreateSystemManaged<CountConsumptionSystem>();
        m_CitySystem = World.GetOrCreateSystemManaged<CitySystem>();
        m_CountHouseholdDataSystem = World.GetOrCreateSystemManaged<CountHouseholdDataSystem>();

        // Initialize Queries
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
            ComponentType.Exclude<Temp>()
        );
        m_CommercialProcessDataQuery = GetEntityQuery(
            ComponentType.ReadOnly<IndustrialProcessData>(),
            ComponentType.ReadOnly<ServiceCompanyData>()
        );

        // Initialize native arrays and values
        m_BuildingDemand = new NativeValue<int>(Allocator.Persistent);
        m_DemandFactors = new NativeArray<int>(18, Allocator.Persistent);
        int resourceCount = EconomyUtils.ResourceCount;
        m_ResourceDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
        m_BuildingDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
        m_Consumption = new NativeArray<int>(resourceCount, Allocator.Persistent);
        m_FreeProperties = new NativeArray<int>(resourceCount, Allocator.Persistent);
        m_EstimatedConsumptionPerCim = new NativeArray<float>(resourceCount, Allocator.Persistent);
        m_ActualConsumptionPerCim = new NativeArray<float>(resourceCount, Allocator.Persistent);

        RequireForUpdate(m_EconomyParameterQuery);
        RequireForUpdate(m_DemandParameterQuery);
        RequireForUpdate(m_CommercialProcessDataQuery);

        // Initialize UI bindings
        m_Results = new NativeArray<int>(10, Allocator.Persistent);
        m_ExcludedResources = new NativeValue<Resource>(Allocator.Persistent);

        AddBinding(
            m_uiResults = new RawValueBinding(
                kGroup,
                "ilCommercial",
                delegate(IJsonWriter binder)
                {
                    binder.ArrayBegin(m_Results.Length);
                    for (int i = 0; i < m_Results.Length; i++)
                        binder.Write(m_Results[i]);
                    binder.ArrayEnd();
                }
            )
        );

        AddBinding(
            m_uiExResources = new RawValueBinding(
                kGroup,
                "ilCommercialExRes",
                delegate(IJsonWriter binder)
                {
                    List<string> resList = new List<string>();
                    for (int i = 0; i < EconomyUtils.ResourceCount; i++)
                        if (
                            (m_ExcludedResources.value & EconomyUtils.GetResource(i))
                            != Resource.NoResource
                        )
                            resList.Add(EconomyUtils.GetName(EconomyUtils.GetResource(i)));
                    binder.ArrayBegin(resList.Count);
                    foreach (string res in resList)
                        binder.Write(res);
                    binder.ArrayEnd();
                }
            )
        );

        SetDefaults(); // there is no serialization, so init just for safety
        Mod.log.Info("CommercialDemandUISystem created.");
    }

    protected override void OnDestroy()
    {
        m_BuildingDemand.Dispose();
        m_DemandFactors.Dispose();
        m_ResourceDemands.Dispose();
        m_BuildingDemands.Dispose();
        m_Consumption.Dispose();
        m_FreeProperties.Dispose();
        m_Results.Dispose();
        m_ExcludedResources.Dispose();
        m_EstimatedConsumptionPerCim.Dispose();
        m_ActualConsumptionPerCim.Dispose();
        base.OnDestroy();
    }

    public void SetDefaults()
    {
        m_BuildingDemand.value = 50; // Starting with some demand
        m_DemandFactors.Fill(0);
        m_ResourceDemands.Fill(0);
        m_BuildingDemands.Fill(0);
        m_Consumption.Fill(0);
        m_FreeProperties.Fill(0);
        m_EstimatedConsumptionPerCim.Fill(0f);
        m_ActualConsumptionPerCim.Fill(0f);
    }

    protected override void OnUpdate()
    {
        if (m_SimulationSystem.frameIndex % 128 != 55)
            return;

        base.OnUpdate();
        ResetResults();

        if (
            !m_DemandParameterQuery.IsEmptyIgnoreFilter
            && !m_EconomyParameterQuery.IsEmptyIgnoreFilter
        )
        {
            JobHandle deps;
            var commercialCompanyDatas = m_CountCompanyDataSystem.GetCommercialCompanyDatas(
                out deps
            );

            // Update TypeHandle
            __TypeHandle.__Game_City_Tourism_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            __TypeHandle.__Game_City_Population_RO_ComponentLookup.Update(ref this.CheckedStateRef);
            __TypeHandle.__Game_Companies_CommercialCompany_RO_ComponentLookup.Update(
                ref this.CheckedStateRef
            );
            __TypeHandle.__Game_Prefabs_WorkplaceData_RO_ComponentLookup.Update(
                ref this.CheckedStateRef
            );
            __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup.Update(
                ref this.CheckedStateRef
            );
            __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup.Update(
                ref this.CheckedStateRef
            );
            __TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle.Update(
                ref this.CheckedStateRef
            );
            __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentTypeHandle.Update(
                ref this.CheckedStateRef
            );
            __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle.Update(
                ref this.CheckedStateRef
            );
            __TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref this.CheckedStateRef);

            // Initialize the job
            UpdateCommercialDemandJob updateJob = new UpdateCommercialDemandJob
            {
                m_FreePropertyChunks = m_FreeCommercialQuery.ToArchetypeChunkListAsync(
                    base.World.UpdateAllocator.ToAllocator,
                    out var outJobHandle
                ),
                m_CommercialProcessDataChunks =
                    m_CommercialProcessDataQuery.ToArchetypeChunkListAsync(
                        base.World.UpdateAllocator.ToAllocator,
                        out var outJobHandle2
                    ),
                m_EntityType = __TypeHandle.__Unity_Entities_Entity_TypeHandle,
                m_PrefabType = __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle,
                m_ProcessType =
                    __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentTypeHandle,
                m_RenterType = __TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle,
                m_BuildingPropertyDatas =
                    __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup,
                m_ResourceDatas = __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup,
                m_WorkplaceDatas = __TypeHandle.__Game_Prefabs_WorkplaceData_RO_ComponentLookup,
                m_CommercialCompanies =
                    __TypeHandle.__Game_Companies_CommercialCompany_RO_ComponentLookup,
                m_Populations = __TypeHandle.__Game_City_Population_RO_ComponentLookup,
                m_Tourisms = __TypeHandle.__Game_City_Tourism_RO_ComponentLookup,
                m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
                m_DemandParameters = m_DemandParameterQuery.GetSingleton<DemandParameterData>(),
                m_EconomyParameters = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
                m_EmployableByEducation = m_CountEmploymentSystem.GetEmployableByEducation(
                    out var deps2
                ),
                m_TaxRates = m_TaxSystem.GetTaxRates(),
                m_FreeWorkplaces = m_CountFreeWorkplacesSystem.GetFreeWorkplaces(out var deps3),
                m_BaseConsumptionSum = m_ResourceSystem.BaseConsumptionSum,
                m_City = m_CitySystem.City,
                m_BuildingDemand = m_BuildingDemand,
                m_DemandFactors = m_DemandFactors,
                m_ResourceDemands = m_ResourceDemands,
                m_BuildingDemands = m_BuildingDemands,
                m_Productions = commercialCompanyDatas.m_SalesCapacities,
                m_Consumptions = m_Consumption,
                m_TotalAvailables = commercialCompanyDatas.m_CurrentAvailables,
                m_TotalMaximums = commercialCompanyDatas.m_TotalAvailables,
                m_Companies = commercialCompanyDatas.m_ServiceCompanies,
                m_FreeProperties = m_FreeProperties,
                m_Propertyless = commercialCompanyDatas.m_ServicePropertyless,
                m_TotalMaxWorkers = commercialCompanyDatas.m_MaxServiceWorkers,
                m_TotalCurrentWorkers = commercialCompanyDatas.m_CurrentServiceWorkers,
                m_ActualConsumptions = m_CountConsumptionSystem.GetConsumptions(out var deps4),
                m_Results = m_Results,
                m_ExcludedResources = m_ExcludedResources,
                m_EstimatedConsumptionPerCim = m_EstimatedConsumptionPerCim,
                m_ActualConsumptionPerCim = m_ActualConsumptionPerCim,
            };

            JobHandle combinedDeps = JobUtils.CombineDependencies(
                Dependency,
                m_ReadDependencies,
                deps4,
                outJobHandle,
                deps,
                outJobHandle2,
                deps2,
                deps3
            );
            Dependency = updateJob.Schedule(combinedDeps);

            m_WriteDependencies = Dependency;

            // Add dependency readers
            m_CountConsumptionSystem.AddConsumptionWriter(Dependency);
            m_ResourceSystem.AddPrefabsReader(Dependency);
            m_CountEmploymentSystem.AddReader(Dependency);
            m_CountFreeWorkplacesSystem.AddReader(Dependency);
            m_TaxSystem.AddReader(Dependency);

            Dependency.Complete(); // Ensure completion before updating UI
        }

        // Update UI
        m_uiResults.Update();
        m_uiExResources.Update();
    }

    private void ResetResults()
    {
        m_ExcludedResources.value = Resource.NoResource;
        m_Results.Fill(0);
    }

    [BurstCompile]
    private struct UpdateCommercialDemandJob : IJob
    {
        [ReadOnly]
        public NativeList<ArchetypeChunk> m_FreePropertyChunks;

        [ReadOnly]
        public EntityTypeHandle m_EntityType;

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
        public ResourcePrefabs m_ResourcePrefabs;

        public DemandParameterData m_DemandParameters;

        [ReadOnly]
        public NativeArray<int> m_TaxRates;

        [ReadOnly]
        public NativeArray<int> m_ProduceCapacity;

        [ReadOnly]
        public NativeArray<int> m_CurrentAvailables;

        [ReadOnly]
        public NativeArray<int> m_Propertyless;

        [ReadOnly]
        public NativeArray<int> m_ResourceNeeds; // Household resource needs

        public NativeValue<int> m_BuildingDemand;

        public NativeArray<int> m_DemandFactors;

        public NativeArray<int> m_FreeProperties;

        public NativeArray<int> m_ResourceDemands;

        public NativeArray<int> m_BuildingDemands;

        public void Execute()
        {
            // Initialize arrays
            ResourceIterator iterator = ResourceIterator.GetIterator();
            while (iterator.Next())
            {
                int resourceIndex = EconomyUtils.GetResourceIndex(iterator.resource);
                m_FreeProperties[resourceIndex] = 0;
                m_BuildingDemands[resourceIndex] = 0;
                m_ResourceDemands[resourceIndex] = 0;
            }

            for (int i = 0; i < m_DemandFactors.Length; i++)
            {
                m_DemandFactors[i] = 0;
            }

            // Count free properties
            for (int chunkIndex = 0; chunkIndex < m_FreePropertyChunks.Length; chunkIndex++)
            {
                ArchetypeChunk chunk = m_FreePropertyChunks[chunkIndex];
                NativeArray<PrefabRef> prefabRefs = chunk.GetNativeArray(ref m_PrefabType);
                BufferAccessor<Renter> rentersAccessor = chunk.GetBufferAccessor(ref m_RenterType);

                for (int i = 0; i < chunk.Count; i++)
                {
                    Entity prefabEntity = prefabRefs[i].m_Prefab;

                    if (m_BuildingPropertyDatas.HasComponent(prefabEntity))
                    {
                        bool hasCommercialCompany = false;
                        DynamicBuffer<Renter> renters = rentersAccessor[i];

                        for (int renterIndex = 0; renterIndex < renters.Length; renterIndex++)
                        {
                            if (m_CommercialCompanies.HasComponent(renters[renterIndex].m_Renter))
                            {
                                hasCommercialCompany = true;
                                break;
                            }
                        }

                        if (!hasCommercialCompany)
                        {
                            BuildingPropertyData buildingPropertyData = m_BuildingPropertyDatas[
                                prefabEntity
                            ];
                            iterator = ResourceIterator.GetIterator();

                            while (iterator.Next())
                            {
                                if (
                                    (buildingPropertyData.m_AllowedSold & iterator.resource)
                                    != Resource.NoResource
                                )
                                {
                                    int resourceIndex = EconomyUtils.GetResourceIndex(
                                        iterator.resource
                                    );
                                    m_FreeProperties[resourceIndex]++;
                                }
                            }
                        }
                    }
                }
            }

            // Reset building demand
            m_BuildingDemand.value = 0;

            iterator = ResourceIterator.GetIterator();
            int resourceCount = 0;

            while (iterator.Next())
            {
                Resource resource = iterator.resource;
                int resourceIndex = EconomyUtils.GetResourceIndex(resource);

                if (
                    EconomyUtils.IsCommercialResource(resource)
                    && m_ResourceDatas.HasComponent(m_ResourcePrefabs[resource])
                )
                {
                    // Calculate tax effect
                    float taxRate = TaxSystem.GetCommercialTaxRate(resource, m_TaxRates);
                    float taxEffect = -0.05f * (taxRate - 10f);

                    // Get resource need from household data
                    int resourceNeed = m_ResourceNeeds[resourceIndex];
                    if (resourceNeed == 0 && resource != Resource.Lodging)
                    {
                        // If no specific need, assume a base need
                        resourceNeed = 100;
                    }

                    // Get current available amount
                    int currentAvailable =
                        m_CurrentAvailables[resourceIndex] == 0
                            ? m_ProduceCapacity[resourceIndex]
                            : m_CurrentAvailables[resourceIndex];

                    // Calculate resource demand
                    float baseDemand = m_DemandParameters.m_CommercialBaseDemand * resourceNeed;
                    float demandValue =
                        (1f + taxEffect) * Mathf.Clamp(baseDemand - currentAvailable, 0f, 100f);

                    m_ResourceDemands[resourceIndex] = Mathf.RoundToInt(demandValue);

                    if (m_ResourceDemands[resourceIndex] > 0)
                    {
                        // Determine if we need more buildings for this resource
                        int freePropertyCount = m_FreeProperties[resourceIndex];
                        int propertylessCompanies = m_Propertyless[resourceIndex];

                        m_BuildingDemands[resourceIndex] =
                            (freePropertyCount - propertylessCompanies) > 0
                                ? 0
                                : m_ResourceDemands[resourceIndex];

                        if (m_BuildingDemands[resourceIndex] > 0)
                        {
                            m_BuildingDemand.value += m_BuildingDemands[resourceIndex];
                        }

                        // Update demand factors
                        int resourceDemand =
                            m_BuildingDemands[resourceIndex] > 0
                                ? m_ResourceDemands[resourceIndex]
                                : 0;
                        int totalDemand = m_ResourceDemands[resourceIndex];
                        int taxEffectInt = Mathf.RoundToInt(100f * taxEffect);
                        int netDemand = totalDemand + taxEffectInt;

                        if (resource == Resource.Lodging)
                        {
                            m_DemandFactors[(int)DemandFactor.TouristDemand] += totalDemand;
                        }
                        else if (resource == Resource.Petrochemicals)
                        {
                            m_DemandFactors[(int)DemandFactor.PetrolLocalDemand] += totalDemand;
                        }
                        else
                        {
                            m_DemandFactors[(int)DemandFactor.LocalDemand] += totalDemand;
                        }

                        m_DemandFactors[(int)DemandFactor.Taxes] += taxEffectInt;
                        m_DemandFactors[(int)DemandFactor.EmptyBuildings] += Mathf.Min(
                            0,
                            resourceDemand - netDemand
                        );

                        resourceCount++;
                    }
                    else
                    {
                        m_ExcludedResources.value |= resource; // Exclude resources with no demand
                    }
                }
                else
                {
                    m_ExcludedResources.value |= resource; // Exclude non-commercial resources
                }
            }

            // Normalize building demand
            m_BuildingDemand.value =
                resourceCount > 0 ? Mathf.Clamp(m_BuildingDemand.value / resourceCount, 0, 100) : 0;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref SystemState state)
        {
            __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
            __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle =
                state.GetComponentTypeHandle<PrefabRef>(true);
            __Game_Prefabs_IndustrialProcessData_RO_ComponentTypeHandle =
                state.GetComponentTypeHandle<IndustrialProcessData>(true);
            __Game_Buildings_Renter_RO_BufferTypeHandle = state.GetBufferTypeHandle<Renter>(true);
            __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup =
                state.GetComponentLookup<BuildingPropertyData>(true);
            __Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(
                true
            );
            __Game_Prefabs_WorkplaceData_RO_ComponentLookup =
                state.GetComponentLookup<WorkplaceData>(true);
            __Game_Companies_CommercialCompany_RO_ComponentLookup =
                state.GetComponentLookup<CommercialCompany>(true);
            __Game_City_Population_RO_ComponentLookup = state.GetComponentLookup<Population>(true);
            __Game_City_Tourism_RO_ComponentLookup = state.GetComponentLookup<Tourism>(true);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void __AssignQueries(ref SystemState state) { }

    protected override void OnCreateForCompiler()
    {
        base.OnCreateForCompiler();
        __AssignQueries(ref base.CheckedStateRef);
        __TypeHandle.__AssignHandles(ref base.CheckedStateRef);
    }

    public void AddReader(JobHandle reader)
    {
        m_ReadDependencies = JobHandle.CombineDependencies(m_ReadDependencies, reader);
    }
}
