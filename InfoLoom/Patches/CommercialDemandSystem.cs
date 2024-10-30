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
using HarmonyLib;
using static InfoLoomBrucey.Systems.CommercialUISystem;
using InfoLoom;

namespace InfoLoomBrucey.Patches;

[HarmonyPatch]
public class CommercialDemandSystem_Patches
{
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

    private static JobHandle baseDependency = new JobHandle();
    private static TypeHandle __TypeHandle = new TypeHandle();
    private static bool isInitialized = false;
    public static NativeArray<DemandData> m_DemandData;

    [HarmonyPatch(typeof(Game.Simulation.CommercialDemandSystem), "OnCreate")]
    [HarmonyPostfix]
    static void CommercialDemandSystem_OnCreate()
    {
        m_DemandData = new NativeArray<DemandData>(EconomyUtils.ResourceCount, Allocator.Persistent);
    }

    [HarmonyPatch(typeof(Game.Simulation.CommercialDemandSystem), "OnDestroy")]
    [HarmonyPrefix]
    static bool CommercialDemandSystem_OnDestroy()
    {
        if (m_DemandData.IsCreated)
            m_DemandData.Dispose();
        return true;
    }

    [HarmonyPatch(typeof(Game.Simulation.CommercialDemandSystem), "OnUpdate")]
    [HarmonyPrefix]
    static bool CommercialDemandSystem_OnUpdate(
        CommercialDemandSystem __instance,
        ResourceSystem ___m_ResourceSystem,
        TaxSystem ___m_TaxSystem,
        CountHouseholdDataSystem ___m_CountHouseholdDataSystem,
        CountCompanyDataSystem ___m_CountCompanyDataSystem,
        CitySystem ___m_CitySystem,
        EntityQuery ___m_EconomyParameterQuery,
        EntityQuery ___m_DemandParameterQuery,
        EntityQuery ___m_FreeCommercialQuery,
        EntityQuery ___m_UnlockedZoneDataQuery,
        NativeValue<int> ___m_CompanyDemand,
        NativeValue<int> ___m_BuildingDemand,
        NativeArray<int> ___m_DemandFactors,
        NativeArray<int> ___m_ResourceDemands,
        NativeArray<int> ___m_BuildingDemands,
        NativeArray<int> ___m_FreeProperties,
        ref JobHandle ___m_WriteDependencies,
        JobHandle ___m_ReadDependencies,
        ref int ___m_LastCompanyDemand,
        ref int ___m_LastBuildingDemand)
    {
        if (!Mod.setting.FeatureCommercialDemand)
            return true;

        if (!isInitialized)
            LateInitialize(__instance);

        if (!___m_DemandParameterQuery.IsEmptyIgnoreFilter && !___m_EconomyParameterQuery.IsEmptyIgnoreFilter)
        {
            ___m_LastCompanyDemand = ___m_CompanyDemand.value;
            ___m_LastBuildingDemand = ___m_BuildingDemand.value;

            JobHandle deps;
            CountCompanyDataSystem.CommercialCompanyDatas commercialCompanyDatas =
                ___m_CountCompanyDataSystem.GetCommercialCompanyDatas(out deps);

            // Update type handles
            __TypeHandle.__Game_City_Tourism_RO_ComponentLookup.Update(ref __instance.CheckedStateRef);
            __TypeHandle.__Game_Companies_CommercialCompany_RO_ComponentLookup.Update(ref __instance.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup.Update(ref __instance.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup.Update(ref __instance.CheckedStateRef);
            __TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle.Update(ref __instance.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle.Update(ref __instance.CheckedStateRef);

            // Setup job
            UpdateCommercialDemandJob updateCommercialDemandJob = default(UpdateCommercialDemandJob);
            updateCommercialDemandJob.m_FreePropertyChunks = ___m_FreeCommercialQuery.ToArchetypeChunkListAsync(
                __instance.World.UpdateAllocator.ToAllocator, out var outJobHandle);
            updateCommercialDemandJob.m_UnlockedZoneDatas = ___m_UnlockedZoneDataQuery.ToComponentDataArray<ZoneData>(Allocator.TempJob);
            updateCommercialDemandJob.m_PrefabType = __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
            updateCommercialDemandJob.m_RenterType = __TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle;
            updateCommercialDemandJob.m_BuildingPropertyDatas = __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;
            updateCommercialDemandJob.m_ResourceDatas = __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup;
            updateCommercialDemandJob.m_CommercialCompanies = __TypeHandle.__Game_Companies_CommercialCompany_RO_ComponentLookup;
            updateCommercialDemandJob.m_ResourcePrefabs = ___m_ResourceSystem.GetPrefabs();
            updateCommercialDemandJob.m_DemandParameters = ___m_DemandParameterQuery.GetSingleton<DemandParameterData>();
            updateCommercialDemandJob.m_TaxRates = ___m_TaxSystem.GetTaxRates();
            updateCommercialDemandJob.m_CompanyDemand = ___m_CompanyDemand;
            updateCommercialDemandJob.m_BuildingDemand = ___m_BuildingDemand;
            updateCommercialDemandJob.m_DemandFactors = ___m_DemandFactors;
            updateCommercialDemandJob.m_City = ___m_CitySystem.City;
            updateCommercialDemandJob.m_ResourceDemands = ___m_ResourceDemands;
            updateCommercialDemandJob.m_BuildingDemands = ___m_BuildingDemands;
            updateCommercialDemandJob.m_ProduceCapacity = commercialCompanyDatas.m_ProduceCapacity;
            updateCommercialDemandJob.m_CurrentAvailables = commercialCompanyDatas.m_CurrentAvailables;
            updateCommercialDemandJob.m_ResourceNeeds = ___m_CountHouseholdDataSystem.GetResourceNeeds();
            updateCommercialDemandJob.m_FreeProperties = ___m_FreeProperties;
            updateCommercialDemandJob.m_Propertyless = commercialCompanyDatas.m_ServicePropertyless;
            updateCommercialDemandJob.m_Tourisms = __TypeHandle.__Game_City_Tourism_RO_ComponentLookup;
            updateCommercialDemandJob.m_Companies = commercialCompanyDatas.m_ServiceCompanies;
            updateCommercialDemandJob.m_Productions = commercialCompanyDatas.m_ProduceCapacity;
            updateCommercialDemandJob.m_TotalMaximums = commercialCompanyDatas.m_TotalAvailables;
            updateCommercialDemandJob.m_TotalAvailables = commercialCompanyDatas.m_CurrentAvailables;
            updateCommercialDemandJob.m_TotalMaxWorkers = commercialCompanyDatas.m_MaxServiceWorkers;
            updateCommercialDemandJob.m_TotalCurrentWorkers = commercialCompanyDatas.m_CurrentServiceWorkers;

            updateCommercialDemandJob.m_DemandData = m_DemandData;

            // Schedule job
            baseDependency = IJobExtensions.Schedule(updateCommercialDemandJob,
                JobUtils.CombineDependencies(baseDependency, ___m_ReadDependencies, outJobHandle, deps));

            ___m_WriteDependencies = baseDependency;
            ___m_CountHouseholdDataSystem.AddHouseholdResourceNeedReader(baseDependency);
            ___m_ResourceSystem.AddPrefabsReader(baseDependency);
            ___m_TaxSystem.AddReader(baseDependency);
        }

        return false;
    }

    public static void LateInitialize(CommercialDemandSystem __instance)
    {
        CommercialDemandSystem_OnCreate();
        __TypeHandle.__AssignHandles(ref __instance.CheckedStateRef);
        isInitialized = true;
    }
}