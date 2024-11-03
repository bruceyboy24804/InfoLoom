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
using Game.Zones; // Added for ZoneData
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
using InfoLoomTwo;
using static InfoLoomTwo.Systems.CommercialUISystem;

namespace InfoLoomTwo.Patches;

[HarmonyPatch]
public class CommercialDemandSystem_Patches
{
    //[BurstCompile]
    private struct UpdateCommercialDemandJob : IJob
    {
        [DeallocateOnJobCompletion]
        [ReadOnly]
        public NativeArray<ZoneData> m_UnlockedZoneDatas; // Added for unlocked zones

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

        public NativeArray<int> m_CurrentServiceWorkers;

        public NativeArray<int> m_MaxServiceWorkers;

        public NativeArray<int> m_ServiceCompanies;

        public NativeArray<int> m_TotalAvailables;

       
        [ReadOnly]
        public NativeArray<int> m_ResourceNeeds;

        [ReadOnly]
        public NativeArray<int> m_ProduceCapacity;

        [ReadOnly]
        public NativeArray<int> m_CurrentAvailables;

        [ReadOnly]
        public NativeArray<int> m_Propertyless;

        public NativeArray<DemandData> m_DemandData; // Added for UI data

        public void Execute()
        {
            // Check for unlocked commercial zones
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
                if (!EconomyUtils.IsCommercialResource(iterator.resource) || !m_ResourceDatas.HasComponent(m_ResourcePrefabs[iterator.resource]))
                {
                    continue;
                }

                float taxRateMultiplier = Mod.setting.TaxRateMultiplier; // Get the multiplier from settings
                float multiplierValue = Mod.setting.TaxRateMultiplierValue; // Get the multiplier value from settings
                float num2 = taxRateMultiplier * multiplierValue * ((float)TaxSystem.GetCommercialTaxRate(iterator.resource, m_TaxRates) - 10f);
                int num3 = ((m_ResourceNeeds[resourceIndex2] == 0 && iterator.resource != Resource.Lodging) ? 100 : m_ResourceNeeds[resourceIndex2]);
                int num4 = ((m_CurrentAvailables[resourceIndex2] == 0) ? m_ProduceCapacity[resourceIndex2] : m_CurrentAvailables[resourceIndex2]);
                m_ResourceDemands[resourceIndex2] = Mathf.RoundToInt(
                    (1f + num2) * math.clamp(
                     math.max(m_DemandParameters.m_CommercialBaseDemand * (float)num3 - (float)num4, 0f),
                        0f,
                        100f
                    )
                );
                if (iterator.resource == Resource.Lodging && math.max((int)((float)m_Tourisms[m_City].m_CurrentTourists * m_DemandParameters.m_HotelRoomPercentRequirement) - m_Tourisms[m_City].m_Lodging.y, 0) > 0)
                {
                    m_ResourceDemands[resourceIndex2] = 100;
                }
                if (m_ResourceDemands[resourceIndex2] > 0)
                {
                    m_CompanyDemand.value += m_ResourceDemands[resourceIndex2];
                    m_BuildingDemands[resourceIndex2] = ((m_FreeProperties[resourceIndex2] - m_Propertyless[resourceIndex2] <= 0) ? m_ResourceDemands[resourceIndex2] : 0);
                    if (m_BuildingDemands[resourceIndex2] > 0)
                    {
                        m_BuildingDemand.value += m_BuildingDemands[resourceIndex2];
                    }
                    int num5 = ((m_BuildingDemands[resourceIndex2] > 0) ? m_ResourceDemands[resourceIndex2] : 0);
                    int num6 = m_ResourceDemands[resourceIndex2];
                    int num7 = Mathf.RoundToInt(100f * num2);
                    int num8 = num6 + num7;
                    if (iterator.resource == Resource.Lodging)
                    {
                        m_DemandFactors[9] += num6;
                    }
                    else if (iterator.resource == Resource.Petrochemicals)
                    {
                        m_DemandFactors[16] += num6;
                    }
                    else
                    {
                        m_DemandFactors[4] += num6;
                    }
                    m_DemandFactors[11] += num7;
                    m_DemandFactors[13] += math.min(0, num5 - num8);
                    num++;
                    
                    // UI Data Update
                    DemandData uiData = m_DemandData[resourceIndex2];
                    uiData.Resource = iterator.resource;
                    uiData.Demand = m_ResourceDemands[resourceIndex2];
                    uiData.Building = m_BuildingDemands[resourceIndex2];
                    uiData.Free = m_FreeProperties[resourceIndex2];
                    uiData.Companies = m_ServiceCompanies[resourceIndex2];
                    uiData.Workers = m_MaxServiceWorkers[resourceIndex2];
                    uiData.SvcFactor = Mathf.RoundToInt(100f * num7);
                    uiData.SvcPercent = (m_TotalAvailables[resourceIndex2] == 0 ? 0 : 100 * m_CurrentAvailables[resourceIndex2] / m_TotalAvailables[resourceIndex2]);
                    uiData.CapFactor = Mathf.RoundToInt(100f * num6);
                    uiData.CapPercent = 100 * m_ProduceCapacity[resourceIndex2] / math.max(100, m_ResourceNeeds[resourceIndex2]);
                    uiData.CapPerCompany = (m_ServiceCompanies[resourceIndex2] == 0 ? 0 : m_ProduceCapacity[resourceIndex2] / m_ServiceCompanies[resourceIndex2]);
                    uiData.WrkFactor = Mathf.RoundToInt(100f * num4);
                    uiData.WrkPercent = 100 * (m_CurrentServiceWorkers[resourceIndex2] + 1) / (m_MaxServiceWorkers[resourceIndex2] + 1);
                    uiData.TaxFactor = Mathf.RoundToInt(100f * num2);
                    m_DemandData[resourceIndex2] = uiData; // Update the demand data for UI


                }


            }


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

    //[HarmonyPatch(typeof(Game.Simulation.CommercialDemandSystem), "OnCreate")]
    //[HarmonyPostfix]
    static void CommercialDemandSystem_OnCreate()
    {
        m_DemandData = new NativeArray<DemandData>(EconomyUtils.ResourceCount, Allocator.Persistent);
    }

    [HarmonyPatch(typeof(Game.Simulation.CommercialDemandSystem), "OnDestroy")]
    [HarmonyPrefix]
    static bool CommercialDemandSystem_OnDestroy()
    {
        m_DemandData.Dispose();
        return true;
    }

    // public void SetDefaults(Context context)
    // public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    // public void Deserialize<TReader>(TReader reader) where TReader : IReader													 

    [HarmonyPatch(typeof(Game.Simulation.CommercialDemandSystem), "OnUpdate")]
    [HarmonyPrefix]
    static bool CommercialDemandSystem_OnUpdate(
            CommercialDemandSystem __instance,
            //Game.Simulation.CommercialDemandSystem.TypeHandle __TypeHandle,
            ResourceSystem ___m_ResourceSystem,
            TaxSystem ___m_TaxSystem,
            CountHouseholdDataSystem ___m_CountHouseholdDataSystem,
            CitySystem ___m_CitySystem,
            CountCompanyDataSystem ___m_CountCompanyDataSystem,
            EntityQuery ___m_EconomyParameterQuery,
            EntityQuery ___m_DemandParameterQuery,
            EntityQuery ___m_FreeCommercialQuery,
            EntityQuery ___m_CommercialProcessDataQuery,
            EntityQuery ___m_UnlockedZoneDataQuery, // Added for unlocked zones
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
        // Skip the patch and execute the original if the feature is disabled
        if (!Mod.setting.FeatureCommercialDemand)
            return true;

        // 240331 TypeHandle must be late-initialized because CreateForCompiler is long gone
        if (!isInitialized)
            LateInitialize(__instance);

        // Patched code
        if (!___m_DemandParameterQuery.IsEmptyIgnoreFilter && !___m_EconomyParameterQuery.IsEmptyIgnoreFilter)
        {
            ___m_LastCompanyDemand = ___m_CompanyDemand.value;
            ___m_LastBuildingDemand = ___m_BuildingDemand.value;
            JobHandle deps;
            CountCompanyDataSystem.CommercialCompanyDatas commercialCompanyDatas = ___m_CountCompanyDataSystem.GetCommercialCompanyDatas(out deps);
            __TypeHandle.__Game_City_Tourism_RO_ComponentLookup.Update(ref __instance.CheckedStateRef);
            __TypeHandle.__Game_Companies_CommercialCompany_RO_ComponentLookup.Update(ref __instance.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup.Update(ref __instance.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup.Update(ref __instance.CheckedStateRef);
            __TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle.Update(ref __instance.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle.Update(ref __instance.CheckedStateRef);
            UpdateCommercialDemandJob updateCommercialDemandJob = default(UpdateCommercialDemandJob);
            updateCommercialDemandJob.m_FreePropertyChunks = ___m_FreeCommercialQuery.ToArchetypeChunkListAsync(__instance.World.UpdateAllocator.ToAllocator, out var outJobHandle);
            updateCommercialDemandJob.m_UnlockedZoneDatas = ___m_UnlockedZoneDataQuery.ToComponentDataArray<ZoneData>(Allocator.TempJob); // Updated to include unlocked zones
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
            updateCommercialDemandJob.m_MaxServiceWorkers = commercialCompanyDatas.m_MaxServiceWorkers;
            updateCommercialDemandJob.m_CurrentServiceWorkers = commercialCompanyDatas.m_CurrentServiceWorkers;
            updateCommercialDemandJob.m_ProduceCapacity = commercialCompanyDatas.m_ProduceCapacity;
            updateCommercialDemandJob.m_CurrentAvailables = commercialCompanyDatas.m_CurrentAvailables;
            updateCommercialDemandJob.m_TotalAvailables = commercialCompanyDatas.m_TotalAvailables;
            updateCommercialDemandJob.m_ServiceCompanies = commercialCompanyDatas.m_ServiceCompanies;
            updateCommercialDemandJob.m_ResourceNeeds = ___m_CountHouseholdDataSystem.GetResourceNeeds();
            updateCommercialDemandJob.m_FreeProperties = ___m_FreeProperties;
            updateCommercialDemandJob.m_Propertyless = commercialCompanyDatas.m_ServicePropertyless;
            updateCommercialDemandJob.m_Tourisms = __TypeHandle.__Game_City_Tourism_RO_ComponentLookup;
            updateCommercialDemandJob.m_DemandData = m_DemandData; // MODDED
            UpdateCommercialDemandJob jobData = updateCommercialDemandJob;
            baseDependency = IJobExtensions.Schedule(jobData, JobUtils.CombineDependencies(baseDependency, ___m_ReadDependencies, outJobHandle, deps));
            ___m_WriteDependencies = baseDependency;
            ___m_CountHouseholdDataSystem.AddHouseholdResourceNeedReader(baseDependency);
            ___m_ResourceSystem.AddPrefabsReader(baseDependency);
            ___m_TaxSystem.AddReader(baseDependency);
        }

        return false; // don't execute the original system
    }
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

    // ... existing code ...
}