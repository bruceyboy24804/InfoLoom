﻿using System.Runtime.CompilerServices;
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
using InfoLoomTwo;
using static InfoLoomTwo.Systems.CommercialUISystem;

namespace InfoLoomTwo.Patches;

[HarmonyPatch]
public class CommercialDemandSystem_Patches
{
    //[BurstCompile]
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
        public Workplaces m_FreeWorkplaces;

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

        [ReadOnly]
        public NativeArray<int> m_ProduceCapacity;

        public void Execute()
        {
            //Plugin.Log($"--- EXECUTE ---");
            ResourceIterator iterator = ResourceIterator.GetIterator();
            Population population = m_Populations[m_City];
            Tourism tourism = m_Tourisms[m_City];
            int population2 = (population.m_Population + population.m_PopulationWithMoveIn) / 2;
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
                /*if (EconomyUtils.GetProcessComplexity(m_CommercialProcessDataChunks, m_WorkplaceDatas, iterator.resource, m_EntityType, m_ProcessType, out var complexityTest))
                    Plugin.Log($"{iterator.resource}: {complexityTest}");
                else
                    Plugin.Log($"{iterator.resource}: ---");*/
                // 240308 PATCH HERE - DO NOT SKIP IMMATERIAL RESOURCES
                if ((resourceData.m_Weight == 0f && !resourceData.m_IsLeisure) || !EconomyUtils.GetProcessComplexity(m_CommercialProcessDataChunks, m_WorkplaceDatas, iterator.resource, m_EntityType, m_ProcessType, out var complexity))
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
                float num4 = -9f + 10f * (((float)m_TotalCurrentWorkers[resourceIndex2] + 1f) / ((float)m_TotalMaxWorkers[resourceIndex2] + 1f)); // MODDED: (-3/4) 75% => (-9/10) 90%
                if (num4 > 0f)
                {
                    num4 *= 0.5f;
                }
                float num5 = ((m_TotalMaximums[resourceIndex2] == 0) ? 0f : (-2.5f + 5f * (1f - (float)m_TotalAvailables[resourceIndex2] / (float)m_TotalMaximums[resourceIndex2]))); // MODDED: (-3;10) => (-2.5;5)
                float num6 = /*2f **/ (m_DemandParameters.m_CommercialBaseDemand * (float)m_ResourceNeeds[resourceIndex2] - (float)m_ProduceCapacity[resourceIndex2]) / math.max(100f, (float)m_ResourceNeeds[resourceIndex2] + 1f); // MODDED: no 2f factor
                float num7 = -0.2f * ((float)TaxSystem.GetCommercialTaxRate(iterator.resource, m_TaxRates) - 10f); // MODDED: -0.1 => -0.2 (2x bigger effect)
                m_ResourceDemands[resourceIndex2] = Mathf.RoundToInt(100f * (/*0.2f + */num5 + num4 + num3 + num7 + num6)); // MODDED: 0.2f removed

                // REAL ECO UI ADDITION
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
                uiData.CapPercent = 100 * m_ProduceCapacity[resourceIndex2] / math.max(100, m_ResourceNeeds[resourceIndex2]);
                uiData.CapPerCompany = (m_Companies[resourceIndex2] == 0 ? 0 : m_ProduceCapacity[resourceIndex2] / m_Companies[resourceIndex2]);
                uiData.WrkFactor = Mathf.RoundToInt(100f * num4);
                uiData.WrkPercent = 100 * (m_TotalCurrentWorkers[resourceIndex2] + 1) / (m_TotalMaxWorkers[resourceIndex2] + 1);
                uiData.TaxFactor = Mathf.RoundToInt(100f * num7);
                /*
                uiData.Details = new FixedString512Bytes(
                //$"{iterator.resource} {m_ResourceDemands[resourceIndex2]}/{m_BuildingDemands[resourceIndex2]}: " +
                $"svc {m_TotalAvailables[resourceIndex2]}/{m_TotalMaximums[resourceIndex2]} " +
                $"cap {m_Consumptions[resourceIndex2]}/{m_Productions[resourceIndex2]} " +
                $"num {m_Companies[resourceIndex2]}/{(m_Companies[resourceIndex2] == 0 ? -1 : m_Productions[resourceIndex2] / m_Companies[resourceIndex2])} " +
                $"wrk {m_TotalCurrentWorkers[resourceIndex2]}/{m_TotalMaxWorkers[resourceIndex2]}");
                //$"edu {num3 * 100:F0} " +
                //$"tax {num7 * 100:F0} " +
                //$"free {m_FreeProperties[resourceIndex2]}");
                */
                /*
                Plugin.Log($"{iterator.resource} {m_ResourceDemands[resourceIndex2]}/{m_BuildingDemands[resourceIndex2]}: " +
                        $"svc {num5 * 100:F0} ({m_TotalAvailables[resourceIndex2]}/{m_TotalMaximums[resourceIndex2]}) " +
                        $"cap {num6 * 100:F0} ({m_Consumptions[resourceIndex2]}/{m_Productions[resourceIndex2]}) [{m_Companies[resourceIndex2]}/{(m_Companies[resourceIndex2] == 0 ? -1 : m_Productions[resourceIndex2] / m_Companies[resourceIndex2])}] " +
                        $"wrk {num4 * 100:F0} ({m_TotalCurrentWorkers[resourceIndex2]}/{m_TotalMaxWorkers[resourceIndex2]}) " +
                        $"edu {num3 * 100:F0} " +
                        $"tax {num7 * 100:F0} " +
                        $"free {m_FreeProperties[resourceIndex2]}");*/
                m_DemandData[resourceIndex2] = uiData;

                int num8 = m_ResourceDemands[resourceIndex2];
                if (m_FreeProperties[resourceIndex2] == 0)
                {
                    m_ResourceDemands[resourceIndex2] = 0;
                }
                if (m_ResourceNeeds[resourceIndex2] > 0)
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
                    m_DemandFactors[2] += demandFactorEffect; // EducatedWorkforce
                    m_DemandFactors[1] += demandFactorEffect2; // UneducatedWorkforce
                    if (iterator.resource == Resource.Lodging)
                    {
                        m_DemandFactors[9] += num10; // TouristDemand
                    }
                    else if (iterator.resource == Resource.Petrochemicals)
                    {
                        m_DemandFactors[16] += num10; // PetrolLocalDemand 
                    }
                    else
                    {
                        m_DemandFactors[4] += num10; // LocalDemand 
                    }
                    m_DemandFactors[11] += demandFactorEffect3; // Taxes 
                    m_DemandFactors[13] += math.min(0, num9 - num11); // EmptyBuildings
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref SystemState state)
        {
            __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
            __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
            __Game_Prefabs_IndustrialProcessData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<IndustrialProcessData>(isReadOnly: true);
            __Game_Buildings_Renter_RO_BufferTypeHandle = state.GetBufferTypeHandle<Renter>(isReadOnly: true);
            __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
            __Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
            __Game_Prefabs_WorkplaceData_RO_ComponentLookup = state.GetComponentLookup<WorkplaceData>(isReadOnly: true);
            __Game_Companies_CommercialCompany_RO_ComponentLookup = state.GetComponentLookup<CommercialCompany>(isReadOnly: true);
            __Game_City_Population_RO_ComponentLookup = state.GetComponentLookup<Population>(isReadOnly: true);
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
        // Skip the patch and execute the original if the feaure is disabled
        //if (!Mod.setting.FeatureCommercialDemand)
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
            __TypeHandle.__Game_City_Population_RO_ComponentLookup.Update(ref __instance.CheckedStateRef);
            __TypeHandle.__Game_Companies_CommercialCompany_RO_ComponentLookup.Update(ref __instance.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_WorkplaceData_RO_ComponentLookup.Update(ref __instance.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup.Update(ref __instance.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup.Update(ref __instance.CheckedStateRef);
            __TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle.Update(ref __instance.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentTypeHandle.Update(ref __instance.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle.Update(ref __instance.CheckedStateRef);
            __TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref __instance.CheckedStateRef);
            UpdateCommercialDemandJob updateCommercialDemandJob = default(UpdateCommercialDemandJob);
            updateCommercialDemandJob.m_FreePropertyChunks = ___m_FreeCommercialQuery.ToArchetypeChunkListAsync(__instance.World.UpdateAllocator.ToAllocator, out var outJobHandle);
            updateCommercialDemandJob.m_CommercialProcessDataChunks = ___m_CommercialProcessDataQuery.ToArchetypeChunkListAsync(__instance.World.UpdateAllocator.ToAllocator, out var outJobHandle2);
            updateCommercialDemandJob.m_EntityType = __TypeHandle.__Unity_Entities_Entity_TypeHandle;
            updateCommercialDemandJob.m_PrefabType = __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
            updateCommercialDemandJob.m_ProcessType = __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentTypeHandle;
            updateCommercialDemandJob.m_RenterType = __TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle;
            updateCommercialDemandJob.m_BuildingPropertyDatas = __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;
            updateCommercialDemandJob.m_ResourceDatas = __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup;
            updateCommercialDemandJob.m_WorkplaceDatas = __TypeHandle.__Game_Prefabs_WorkplaceData_RO_ComponentLookup;
            updateCommercialDemandJob.m_CommercialCompanies = __TypeHandle.__Game_Companies_CommercialCompany_RO_ComponentLookup;
            updateCommercialDemandJob.m_Populations = __TypeHandle.__Game_City_Population_RO_ComponentLookup;
            updateCommercialDemandJob.m_Tourisms = __TypeHandle.__Game_City_Tourism_RO_ComponentLookup;
            updateCommercialDemandJob.m_ResourcePrefabs = ___m_ResourceSystem.GetPrefabs();
            updateCommercialDemandJob.m_DemandParameters = ___m_DemandParameterQuery.GetSingleton<DemandParameterData>();
            updateCommercialDemandJob.m_EconomyParameters = ___m_EconomyParameterQuery.GetSingleton<EconomyParameterData>();
            updateCommercialDemandJob.m_EmployableByEducation = ___m_CountHouseholdDataSystem.GetEmployables();
            updateCommercialDemandJob.m_TaxRates = ___m_TaxSystem.GetTaxRates();
            updateCommercialDemandJob.m_ResourceNeeds = ___m_CountHouseholdDataSystem.GetResourceNeeds();
            updateCommercialDemandJob.m_BaseConsumptionSum = ___m_ResourceSystem.BaseConsumptionSum;
            updateCommercialDemandJob.m_CompanyDemand = ___m_CompanyDemand;
            updateCommercialDemandJob.m_BuildingDemand = ___m_BuildingDemand;
            updateCommercialDemandJob.m_DemandFactors = ___m_DemandFactors;
            updateCommercialDemandJob.m_ResourceDemands = ___m_ResourceDemands;
            updateCommercialDemandJob.m_BuildingDemands = ___m_BuildingDemands;
            updateCommercialDemandJob.m_Consumptions = ___m_Consumption;
            updateCommercialDemandJob.m_ProduceCapacity = commercialCompanyDatas.m_ProduceCapacity;
            updateCommercialDemandJob.m_TotalAvailables = commercialCompanyDatas.m_CurrentAvailables;
            updateCommercialDemandJob.m_TotalMaximums = commercialCompanyDatas.m_TotalAvailables;
            updateCommercialDemandJob.m_Companies = commercialCompanyDatas.m_ServiceCompanies;
            updateCommercialDemandJob.m_FreeProperties = ___m_FreeProperties;
            updateCommercialDemandJob.m_Propertyless = commercialCompanyDatas.m_ServicePropertyless;
            updateCommercialDemandJob.m_TotalMaxWorkers = commercialCompanyDatas.m_MaxServiceWorkers;
            updateCommercialDemandJob.m_TotalCurrentWorkers = commercialCompanyDatas.m_CurrentServiceWorkers;
            updateCommercialDemandJob.m_City = ___m_CitySystem.City;
            updateCommercialDemandJob.m_DemandData = m_DemandData; // MODDED
            UpdateCommercialDemandJob jobData = updateCommercialDemandJob;
            baseDependency = IJobExtensions.Schedule(jobData, JobUtils.CombineDependencies(baseDependency, ___m_ReadDependencies, outJobHandle, deps, outJobHandle2));
            ___m_WriteDependencies = baseDependency;
           
            ___m_ResourceSystem.AddPrefabsReader(baseDependency);
            ___m_CountHouseholdDataSystem.AddHouseholdResourceNeedReader(baseDependency);
            ___m_TaxSystem.AddReader(baseDependency);
        }

        return false; // don't execute the original system
    }

    //private void __AssignQueries(ref SystemState state)

    //[HarmonyPatch(typeof(Game.Simulation.CommercialDemandSystem), "OnCreateForCompiler")]
    //[HarmonyPostfix]
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

    // public CommercialDemandSystem()
}