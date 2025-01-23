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
using HarmonyLib;
using InfoLoomTwo.Systems.CommercialSystems.CommercialProductData;
using static InfoLoomTwo.Systems.CommercialSystems.CommercialProductData.CommercialProductsSystem;

namespace InfoLoomTwo.Systems.CommercialSystems.CommercialDemandPatch
{
    [HarmonyPatch]
    public class CommercialDemandPatch
    {
        // -------------------------------------------------------------------------
        // 1) The main ECS job that computes resource demands
        // -------------------------------------------------------------------------
        [BurstCompile]
        private struct UpdateCommercialDemandJob : IJob
        {
            // -- Fields from the original logic --
            public bool m_AlwaysPositiveLodging;
            public float TaxRateEffect;

            [ReadOnly] public NativeList<ArchetypeChunk> m_FreePropertyChunks;
            [ReadOnly] public NativeList<ArchetypeChunk> m_CommercialProcessDataChunks;

            [ReadOnly] public EntityTypeHandle m_EntityType;
            [ReadOnly] public ComponentTypeHandle<PrefabRef> m_PrefabType;
            [ReadOnly] public ComponentTypeHandle<IndustrialProcessData> m_ProcessType;
            [ReadOnly] public BufferTypeHandle<Renter> m_RenterType;

            [ReadOnly] public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDatas;
            [ReadOnly] public ComponentLookup<ResourceData> m_ResourceDatas;
            [ReadOnly] public ComponentLookup<WorkplaceData> m_WorkplaceDatas;
            [ReadOnly] public ComponentLookup<CommercialCompany> m_CommercialCompanies;
            [ReadOnly] public ComponentLookup<Population> m_Populations;
            [ReadOnly] public ComponentLookup<Tourism> m_Tourisms;

            [ReadOnly] public ResourcePrefabs m_ResourcePrefabs;
            public EconomyParameterData m_EconomyParameters;
            public DemandParameterData m_DemandParameters;

            [ReadOnly] public NativeArray<int> m_EmployableByEducation;
            [ReadOnly] public NativeArray<int> m_TaxRates;
            [ReadOnly] public Workplaces m_FreeWorkplaces;

            public Entity m_City;

            public NativeValue<int> m_CompanyDemand;
            public NativeValue<int> m_BuildingDemand;

            // Arrays for final demands
            public NativeArray<int> m_DemandFactors;
            public NativeArray<int> m_FreeProperties;
            public NativeArray<int> m_ResourceDemands;
            public NativeArray<int> m_BuildingDemands;

            // Arrays used for workforce / capacity etc.
            [ReadOnly] public NativeArray<int> m_CurrentAvailables;
            [ReadOnly] public NativeArray<int> m_TotalAvailables;
            [ReadOnly] public NativeArray<int> m_Companies;
            [ReadOnly] public NativeArray<int> m_Propertyless;
            [ReadOnly] public NativeArray<int> m_MaxServiceWorkers;
            [ReadOnly] public NativeArray<int> m_CurrentServiceWorkers;
            [ReadOnly] public NativeArray<int> m_ProduceCapacity;
            [ReadOnly] public NativeArray<int> m_ResourceNeeds;

            // Final data for UI
            public NativeArray<CommercialProductsUISystem.CommercialDemandPatchData> m_DemandData;
            public NativeArray<ZoneData> m_UnlockedZoneDatas;

            // -- New fields for lodging override logic --
            public bool m_OverrideLodgingDemand;  // If true, override lodging
            public int m_LodgingDemandValue;      // Force lodging demand to this

            public void Execute()
            {
                // Retrieve population / tourism for reference if needed
                Population population = m_Populations[m_City];
                Tourism tourism       = m_Tourisms[m_City];

                // 1) Clear out arrays for each resource
                ResourceIterator iterator = ResourceIterator.GetIterator();
                while (iterator.Next())
                {
                    int resourceIndex = EconomyUtils.GetResourceIndex(iterator.resource);
                    m_FreeProperties[resourceIndex]  = 0;
                    m_BuildingDemands[resourceIndex] = 0;
                    m_ResourceDemands[resourceIndex] = 0;
                }

                // 2) Zero out demand factors
                for (int i = 0; i < m_DemandFactors.Length; i++)
                {
                    m_DemandFactors[i] = 0;
                }

                // 3) Count free properties that do NOT have commercial renters
                for (int j = 0; j < m_FreePropertyChunks.Length; j++)
                {
                    ArchetypeChunk chunk           = m_FreePropertyChunks[j];
                    NativeArray<PrefabRef> prefabs = chunk.GetNativeArray(ref m_PrefabType);
                    BufferAccessor<Renter> renters = chunk.GetBufferAccessor(ref m_RenterType);

                    for (int k = 0; k < prefabs.Length; k++)
                    {
                        Entity prefab = prefabs[k].m_Prefab;
                        if (!m_BuildingPropertyDatas.HasComponent(prefab))
                            continue;

                        bool hasCommercial = false;
                        DynamicBuffer<Renter> buffer = renters[k];
                        for (int l = 0; l < buffer.Length; l++)
                        {
                            if (m_CommercialCompanies.HasComponent(buffer[l].m_Renter))
                            {
                                hasCommercial = true;
                                break;
                            }
                        }
                        if (hasCommercial)
                            continue;

                        // This building is free for commercial resource selling
                        BuildingPropertyData buildingPropData = m_BuildingPropertyDatas[prefab];
                        ResourceIterator it2 = ResourceIterator.GetIterator();
                        while (it2.Next())
                        {
                            if ((buildingPropData.m_AllowedSold & it2.resource) != Resource.NoResource)
                            {
                                int rIndex = EconomyUtils.GetResourceIndex(it2.resource);
                                m_FreeProperties[rIndex]++;
                            }
                        }
                    }
                }

                // 4) Reset top-level demands
                m_CompanyDemand.value  = 0;
                bool hadBuildingDemand = (m_BuildingDemand.value > 0);
                m_BuildingDemand.value = 0;

                // 5) Iterate all resources to compute final demands
                iterator = ResourceIterator.GetIterator();
                int numResourcesFound = 0;

                while (iterator.Next())
                {
                    int resourceIndex = EconomyUtils.GetResourceIndex(iterator.resource);

                    // Make sure resource is valid
                    if (!m_ResourceDatas.HasComponent(m_ResourcePrefabs[iterator.resource]))
                        continue;

                    // Also skip if no valid workplace process for this resource
                    if (!EconomyUtils.GetProcessComplexity(
                            m_CommercialProcessDataChunks,
                            m_WorkplaceDatas,
                            iterator.resource,
                            m_EntityType,
                            m_ProcessType,
                            out var complexity))
                    {
                        continue;
                    }

                    // 5a) Calculate workforce needed for complexity
                    Workplaces workplaces = WorkProviderSystem.CalculateNumberOfWorkplaces(20, complexity, 1);

                    // Education factor example
                    float demandFromEducation = 0f;
                    for (int m = 0; m < 5; m++)
                    {
                        if (m < 2)
                        {
                            // Uneducated slots
                            demandFromEducation += 5f * workplaces[m];
                        }
                        else
                        {
                            // More educated
                            float diff = math.max(0, m_EmployableByEducation[m] - m_FreeWorkplaces[m]);
                            demandFromEducation += math.min(5f * workplaces[m], diff);
                        }
                    }

                    // Various factor calculations:
                    float num3 = 0.4f * (demandFromEducation / 50f - 1f); // education factor
                    float num4 = -9f + 10f * ((float)m_CurrentServiceWorkers[resourceIndex] + 1f)
                                          / ((float)m_MaxServiceWorkers[resourceIndex] + 1f); 
                    if (num4 > 0f)
                        num4 *= 0.5f;

                    float num5 = (m_TotalAvailables[resourceIndex] == 0)
                        ? 0f
                        : (-2.5f + 5f * (1f - (float)m_CurrentAvailables[resourceIndex]
                                                / (float)m_TotalAvailables[resourceIndex]));

                    float num6 = (m_DemandParameters.m_CommercialBaseDemand * (float)m_ResourceNeeds[resourceIndex]
                                  - (float)m_ProduceCapacity[resourceIndex])
                                 / math.max(100f, (float)m_ResourceNeeds[resourceIndex] + 1f);

                    float num7 = TaxRateEffect * ((float)TaxSystem.GetCommercialTaxRate(iterator.resource, m_TaxRates) - 10f);

                    // Combine into "base demand"
                    int baseDemandValue = Mathf.RoundToInt(100f * (num5 + num4 + num3 + num7 + num6));

                    // 5b) Lodging override logic
                    if (iterator.resource == Resource.Lodging && m_OverrideLodgingDemand)
                    {
                        m_ResourceDemands[resourceIndex] = m_LodgingDemandValue;
                        m_BuildingDemands[resourceIndex] = m_LodgingDemandValue;
                    }
                    else
                    {
                        // Normal path
                        m_ResourceDemands[resourceIndex] = baseDemandValue;

                        // If "always positive lodging" is set
                        if (iterator.resource == Resource.Lodging && m_AlwaysPositiveLodging)
                        {
                            m_ResourceDemands[resourceIndex] = 5000;
                            m_BuildingDemands[resourceIndex] = 5000;
                        }
                    }

                    // 5c) Optional clamp if not forcibly overridden lodging
                    if (!(iterator.resource == Resource.Lodging && m_OverrideLodgingDemand))
                    {
                        m_ResourceDemands[resourceIndex] =
                            math.clamp(m_ResourceDemands[resourceIndex], 0, 100);
                    }

                    // 5d) Fill in UI data
                    var uiData = m_DemandData[resourceIndex];
                    uiData.Resource  = iterator.resource;
                    uiData.Demand    = m_ResourceDemands[resourceIndex];
                    uiData.Building  = m_BuildingDemands[resourceIndex];
                    uiData.Free      = m_FreeProperties[resourceIndex];
                    uiData.Companies = m_Companies[resourceIndex];
                    uiData.Workers   = m_CurrentServiceWorkers[resourceIndex];

                    // Store factor breakdown for debugging / UI:
                    uiData.SvcFactor  = Mathf.RoundToInt(100f * num5);
                    uiData.SvcPercent = (m_TotalAvailables[resourceIndex] == 0)
                        ? 0
                        : 100 * m_CurrentAvailables[resourceIndex]
                                 / m_TotalAvailables[resourceIndex];

                    uiData.CapFactor  = Mathf.RoundToInt(100f * num6);
                    uiData.CapPercent = 100 * m_ProduceCapacity[resourceIndex]
                                       / math.max(100, m_ResourceNeeds[resourceIndex]);

                    uiData.CapPerCompany = (m_Companies[resourceIndex] == 0)
                        ? 0
                        : m_ProduceCapacity[resourceIndex] / m_Companies[resourceIndex];

                    uiData.WrkFactor  = Mathf.RoundToInt(100f * num4);
                    uiData.WrkPercent = 100 * (m_CurrentServiceWorkers[resourceIndex] + 1)
                                        / (m_MaxServiceWorkers[resourceIndex] + 1);

                    uiData.EduFactor  = Mathf.RoundToInt(100f * num3);
                    uiData.TaxFactor  = Mathf.RoundToInt(100f * num7);

                    m_DemandData[resourceIndex] = uiData;

                    // If no free properties, zero out final resource demand
                    if (m_FreeProperties[resourceIndex] == 0)
                    {
                        m_ResourceDemands[resourceIndex] = 0;
                    }

                    // 5e) Compute final "company" or "building" demand
                    if (m_ResourceNeeds[resourceIndex] > 0)
                    {
                        // Company demand
                        m_CompanyDemand.value += Mathf.RoundToInt(
                            math.min(100, math.max(0, m_ResourceDemands[resourceIndex]))
                        );

                        // Building demand logic
                        m_BuildingDemands[resourceIndex] = math.max(
                            0,
                            Mathf.CeilToInt(
                                math.min(
                                    math.max(
                                        1f,
                                        (float)math.min(1, m_Propertyless[resourceIndex])
                                        + (float)m_Companies[resourceIndex]
                                          / m_DemandParameters.m_FreeCommercialProportion
                                    )
                                    - (float)m_FreeProperties[resourceIndex],
                                    baseDemandValue
                                )
                            )
                        );

                        if (m_BuildingDemands[resourceIndex] > 0)
                        {
                            m_BuildingDemand.value += baseDemandValue;
                        }
                    }

                    // 5f) Factor-based breakdown example using DemandUtils
                    // Let finalDemand = the resource's computed demand
                    int finalDemand = m_ResourceDemands[resourceIndex];

                    // We'll gather partial factor effects for debugging or AI logic:
                    int factorEdu   = ComputeScaledFactor(finalDemand, num3, 0.5f);
                    int factorWork  = ComputeScaledFactor(finalDemand, num4, 0.5f);
                    int factorLocal = ComputeScaledFactor(finalDemand, (num5 + num6), 0.5f);
                    int factorTax   = ComputeScaledFactor(finalDemand, num7, 0.5f);

                    // Place them in m_DemandFactors. Indices are up to you:
                    m_DemandFactors[1]  += factorWork; // Uneducated workforce factor
                    m_DemandFactors[2]  += factorEdu;  // Educated workforce factor
                    m_DemandFactors[4]  += factorLocal;// Local factor
                    m_DemandFactors[11] += factorTax;  // Tax factor

                    // Lodging might get special handling if you want to store in index 9, etc.
                    if (iterator.resource == Resource.Lodging)
                    {
                        m_DemandFactors[9] += factorLocal; // e.g. "Tourist demand"
                    }

                    numResourcesFound++;
                }

                // 6) Average out building demand across all resources
                m_BuildingDemand.value = math.clamp(
                    2 * m_BuildingDemand.value / math.max(1, numResourcesFound),
                    0, 100
                );
            }

            // ---------------------------------------------------------------------
            // Local Helper: Pre-scale factor, then use DemandUtils
            // ---------------------------------------------------------------------
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private int ComputeScaledFactor(int finalDemand, float factor, float multiplier)
            {
                // 1) Scale factor by (finalDemand / 100f) plus any multiplier
                float scaledFactor = factor * (finalDemand / 100f) * multiplier;

                // 2) Use DemandUtils with dummy '0' for the 'total' param
                return DemandUtils.GetDemandFactorEffect(0, scaledFactor);
            }
        }

        // -------------------------------------------------------------------------
        // 2) Type Handle for ECS
        // -------------------------------------------------------------------------
        private struct TypeHandle
        {
            [ReadOnly] public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;
            [ReadOnly] public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
            [ReadOnly] public ComponentTypeHandle<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentTypeHandle;
            [ReadOnly] public BufferTypeHandle<Renter> __Game_Buildings_Renter_RO_BufferTypeHandle;
            [ReadOnly] public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;
            [ReadOnly] public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;
            [ReadOnly] public ComponentLookup<WorkplaceData> __Game_Prefabs_WorkplaceData_RO_ComponentLookup;
            [ReadOnly] public ComponentLookup<CommercialCompany> __Game_Companies_CommercialCompany_RO_ComponentLookup;
            [ReadOnly] public ComponentLookup<Population> __Game_City_Population_RO_ComponentLookup;
            [ReadOnly] public ComponentLookup<Tourism> __Game_City_Tourism_RO_ComponentLookup;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle =
                    state.GetComponentTypeHandle<PrefabRef>(true);
                __Game_Prefabs_IndustrialProcessData_RO_ComponentTypeHandle =
                    state.GetComponentTypeHandle<IndustrialProcessData>(true);
                __Game_Buildings_Renter_RO_BufferTypeHandle =
                    state.GetBufferTypeHandle<Renter>(true);
                __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup =
                    state.GetComponentLookup<BuildingPropertyData>(true);
                __Game_Prefabs_ResourceData_RO_ComponentLookup =
                    state.GetComponentLookup<ResourceData>(true);
                __Game_Prefabs_WorkplaceData_RO_ComponentLookup =
                    state.GetComponentLookup<WorkplaceData>(true);
                __Game_Companies_CommercialCompany_RO_ComponentLookup =
                    state.GetComponentLookup<CommercialCompany>(true);
                __Game_City_Population_RO_ComponentLookup =
                    state.GetComponentLookup<Population>(true);
                __Game_City_Tourism_RO_ComponentLookup =
                    state.GetComponentLookup<Tourism>(true);
            }
        }

        private static JobHandle baseDependency = new JobHandle();
        private static TypeHandle __TypeHandle = new TypeHandle();
        private static bool isInitialized = false;

        // -------------------------------------------------------------------------
        // 3) A global NativeArray for UI data
        // -------------------------------------------------------------------------
        public static NativeArray<CommercialProductsUISystem.CommercialDemandPatchData> m_DemandData;

        // -------------------------------------------------------------------------
        // 4) OnCreate / OnDestroy Patches
        // -------------------------------------------------------------------------
        [HarmonyPatch(typeof(Game.Simulation.CommercialDemandSystem), "OnCreate")]
        [HarmonyPostfix]
        static void CommercialDemandSystem_OnCreate()
        {
            // We allocate the m_DemandData array for all resources
            m_DemandData = new NativeArray<CommercialProductsUISystem.CommercialDemandPatchData>(
                EconomyUtils.ResourceCount,
                Allocator.Persistent
            );
        }

        [HarmonyPatch(typeof(Game.Simulation.CommercialDemandSystem), "OnDestroy")]
        [HarmonyPrefix]
        static bool CommercialDemandSystem_OnDestroy()
        {
            // Dispose the array if created
            if (m_DemandData.IsCreated)
                m_DemandData.Dispose();
            return true; 
        }

        // Assign ECS TypeHandles
        [HarmonyPatch(typeof(Game.Simulation.CommercialDemandSystem), "OnCreateForCompiler")]
        [HarmonyPostfix]
        public static void CommercialDemandSystem_OnCreateForCompiler(CommercialDemandSystem __instance)
        {
            __TypeHandle.__AssignHandles(ref __instance.CheckedStateRef);
        }

        public static void LateInitialize(CommercialDemandSystem __instance)
        {
            CommercialDemandSystem_OnCreate();
            CommercialDemandSystem_OnCreateForCompiler(__instance);
            isInitialized = true;
        }

        // -------------------------------------------------------------------------
        // 5) The main OnUpdate patch that schedules our job
        // -------------------------------------------------------------------------
        [HarmonyPatch(typeof(Game.Simulation.CommercialDemandSystem), "OnUpdate")]
        [HarmonyPrefix]
        static bool CommercialDemandSystem_OnUpdate(
            CommercialDemandSystem __instance,
            ResourceSystem ___m_ResourceSystem,
            TaxSystem ___m_TaxSystem,
            
            CountHouseholdDataSystem ___m_CountHouseholdDataSystem,
            CitySystem ___m_CitySystem,
            CountCompanyDataSystem ___m_CountCompanyDataSystem,
            EntityQuery ___m_EconomyParameterQuery,
            EntityQuery ___m_DemandParameterQuery,
            EntityQuery ___m_FreeCommercialQuery,
            EntityQuery ___m_CommercialProcessDataQuery,
            NativeValue<int> ___m_CompanyDemand,
            NativeValue<int> ___m_BuildingDemand,
            NativeArray<int> ___m_DemandFactors,
            NativeArray<int> ___m_ResourceDemands,
            NativeArray<int> ___m_BuildingDemands,
            NativeArray<int> ___m_Consumption,
            NativeArray<int> ___m_FreeProperties,
            ref JobHandle ___m_WriteDependencies,
            JobHandle ___m_ReadDependencies,
            ref int ___m_LastCompanyDemand,
            ref int ___m_LastBuildingDemand
        )
        {
            // 1) If our mod’s feature is disabled, do nothing
            if (!Mod.setting.FeatureCommercialDemand)
                return true; // run original code

            // 2) Make sure we have assigned type handles
            if (!isInitialized)
                LateInitialize(__instance);

            // 3) If no demand/economy parameters exist, skip
            if (___m_DemandParameterQuery.IsEmptyIgnoreFilter || ___m_EconomyParameterQuery.IsEmptyIgnoreFilter)
                return true; // original code

            // 4) Remember last frame’s demands
            ___m_LastCompanyDemand  = ___m_CompanyDemand.value;
            ___m_LastBuildingDemand = ___m_BuildingDemand.value;

            // 5) Get commercial data from CountCompanyDataSystem
            JobHandle deps;
            var commercialCompanyDatas = ___m_CountCompanyDataSystem.GetCommercialCompanyDatas(out deps);

            // 6) Update ECS read-only handles
            __TypeHandle.__Game_City_Tourism_RO_ComponentLookup.Update(ref __instance.CheckedStateRef);
            __TypeHandle.__Game_City_Population_RO_ComponentLookup.Update(ref __instance.CheckedStateRef);
            __TypeHandle.__Game_Companies_CommercialCompany_RO_ComponentLookup.Update(ref __instance.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_WorkplaceData_RO_ComponentLookup.Update(ref __instance.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup.Update(ref __instance.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup.Update(ref __instance.CheckedStateRef);
            __TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle.Update(ref __instance.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentTypeHandle.Update(ref __instance.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle.Update(ref __instance.CheckedStateRef);
            __TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref __instance.CheckedStateRef);

            // 7) Construct the job
            var job = new UpdateCommercialDemandJob
            {
                // ECS chunk references
                m_FreePropertyChunks         = ___m_FreeCommercialQuery.ToArchetypeChunkListAsync(__instance.World.UpdateAllocator.ToAllocator, out var handle1),
                m_CommercialProcessDataChunks= ___m_CommercialProcessDataQuery.ToArchetypeChunkListAsync(__instance.World.UpdateAllocator.ToAllocator, out var handle2),

                // ECS type handles
                m_EntityType                 = __TypeHandle.__Unity_Entities_Entity_TypeHandle,
                m_PrefabType                 = __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle,
                m_ProcessType                = __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentTypeHandle,
                m_RenterType                 = __TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle,

                // Component lookups
                m_BuildingPropertyDatas      = __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup,
                m_ResourceDatas              = __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup,
                m_WorkplaceDatas             = __TypeHandle.__Game_Prefabs_WorkplaceData_RO_ComponentLookup,
                m_CommercialCompanies        = __TypeHandle.__Game_Companies_CommercialCompany_RO_ComponentLookup,
                m_Populations                = __TypeHandle.__Game_City_Population_RO_ComponentLookup,
                m_Tourisms                   = __TypeHandle.__Game_City_Tourism_RO_ComponentLookup,

                // Other references
                m_ResourcePrefabs            = ___m_ResourceSystem.GetPrefabs(),
                m_EconomyParameters          = ___m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
                m_DemandParameters           = ___m_DemandParameterQuery.GetSingleton<DemandParameterData>(),
                m_EmployableByEducation      = ___m_CountHouseholdDataSystem.GetEmployables(),
                m_TaxRates                   = ___m_TaxSystem.GetTaxRates(),
                m_City                       = ___m_CitySystem.City,

                // Input / Output arrays
                m_CompanyDemand              = ___m_CompanyDemand,
                m_BuildingDemand             = ___m_BuildingDemand,
                m_DemandFactors              = ___m_DemandFactors,
                m_ResourceDemands            = ___m_ResourceDemands,
                m_BuildingDemands            = ___m_BuildingDemands,
                m_FreeProperties             = ___m_FreeProperties,
                m_ResourceNeeds              = ___m_CountHouseholdDataSystem.GetResourceNeeds(),
                m_Companies                  = commercialCompanyDatas.m_ServiceCompanies,
                m_Propertyless               = commercialCompanyDatas.m_ServicePropertyless,
                m_MaxServiceWorkers          = commercialCompanyDatas.m_MaxServiceWorkers,
                m_CurrentServiceWorkers      = commercialCompanyDatas.m_CurrentServiceWorkers,
                m_ProduceCapacity            = commercialCompanyDatas.m_ProduceCapacity,
                m_CurrentAvailables          = commercialCompanyDatas.m_CurrentAvailables,
                m_TotalAvailables            = commercialCompanyDatas.m_TotalAvailables,

                // UI data array
                m_DemandData                 = m_DemandData,
                m_UnlockedZoneDatas          = default, // if used or left unused

                // Settings from your mod
                
                TaxRateEffect                = Mod.setting.TaxRateEffect,
                m_OverrideLodgingDemand      = Mod.setting.OverrideLodgingDemand,
                m_LodgingDemandValue         = Mod.setting.CustomLodgingDemandValue
            };

            // 8) Schedule the job with combined dependencies
            baseDependency = job.Schedule(
                JobUtils.CombineDependencies(
                    baseDependency,
                    ___m_ReadDependencies,
                    handle1,
                    deps,
                    handle2
                )
            );

            // 9) Store the handle so other systems can wait on it
            ___m_WriteDependencies = baseDependency;

            // 10) Make sure dependent systems read after this job
            ___m_ResourceSystem.AddPrefabsReader(baseDependency);
            ___m_CountHouseholdDataSystem.AddHouseholdDataReader(baseDependency);
            ___m_TaxSystem.AddReader(baseDependency);

            // Return false to skip the original OnUpdate logic
            return false;
        }
    }
}
