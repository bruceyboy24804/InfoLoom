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
using Game.Objects;
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

namespace InfoLoomTwo.Systems.IndustrialSystems.IndustrialDemandData
{
    public partial class IndustrialSystem : UISystemBase
    {
        [BurstCompile]
        private struct UpdateIndustrialDemandJob : IJob
        {
            [ReadOnly]
            public NativeList<ArchetypeChunk> m_FreePropertyChunks;

            [ReadOnly]
            public NativeList<ArchetypeChunk> m_StorageCompanyChunks;

            [ReadOnly]
            public NativeList<ArchetypeChunk> m_IndustrialProcessDataChunks;

            [ReadOnly]
            public NativeList<ArchetypeChunk> m_CityServiceChunks;

            [ReadOnly]
            public NativeList<ArchetypeChunk> m_SpawnableChunks;

            [ReadOnly]
            public EntityTypeHandle m_EntityType;

            [ReadOnly]
            public ComponentTypeHandle<PrefabRef> m_PrefabType;

            [ReadOnly]
            public ComponentTypeHandle<IndustrialProcessData> m_ProcessType;

            [ReadOnly]
            public ComponentTypeHandle<CityServiceUpkeep> m_ServiceUpkeepType;

            [ReadOnly]
            public ComponentLookup<IndustrialProcessData> m_IndustrialProcessDatas;

            [ReadOnly]
            public ComponentLookup<PropertyRenter> m_PropertyRenters;

            [ReadOnly]
            public ComponentLookup<PrefabRef> m_Prefabs;

            [ReadOnly]
            public ComponentLookup<BuildingData> m_BuildingDatas;

            [ReadOnly]
            public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDatas;

            [ReadOnly]
            public ComponentLookup<Attached> m_Attached;

            [ReadOnly]
            public ComponentLookup<ResourceData> m_ResourceDatas;

            [ReadOnly]
            public ComponentLookup<WorkplaceData> m_WorkplaceDatas;

            [ReadOnly]
            public ComponentLookup<ConsumptionData> m_ConsumptionDatas;

            [ReadOnly]
            public ComponentLookup<StorageLimitData> m_StorageLimitDatas;

            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingDatas;

            [ReadOnly]
            public BufferLookup<ServiceUpkeepData> m_ServiceUpkeeps;

            [ReadOnly]
            public BufferLookup<CityModifier> m_CityModifiers;

            [ReadOnly]
            public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

            [ReadOnly]
            public BufferLookup<ServiceUpkeepData> m_Upkeeps;

            [ReadOnly]
            public ComponentLookup<Population> m_Populations;

            [ReadOnly]
            public ComponentLookup<Tourism> m_Tourisms;

            [ReadOnly]
            public BufferLookup<TradeCost> m_TradeCosts;

            public EconomyParameterData m_EconomyParameters;

            public DemandParameterData m_DemandParameters;

            [ReadOnly]
            public ResourcePrefabs m_ResourcePrefabs;

            [ReadOnly]
            public NativeArray<int> m_EmployableByEducation;

            [ReadOnly]
            public NativeArray<int> m_TaxRates;

            [ReadOnly]
            public Workplaces m_FreeWorkplaces;

            public Entity m_City;

            public float m_HeatingNeed;

            public float m_BaseConsumptionSum;

            public NativeValue<int> m_IndustrialCompanyDemands;

            public NativeValue<int> m_IndustrialBuildingDemand;

            public NativeValue<int> m_StorageCompanyDemand;

            public NativeValue<int> m_StorageBuildingDemand;

            public NativeValue<int> m_OfficeCompanyDemand;

            public NativeValue<int> m_OfficeBuildingDemand;

            public NativeArray<int> m_DemandFactors;

            public NativeArray<int> m_OfficeDemandFactors;

            public NativeArray<int> m_IndustrialDemands;

            public NativeArray<int> m_IndustrialZoningDemands;

            public NativeArray<int> m_IndustrialBuildingDemands;

            public NativeArray<int> m_StorageBuildingDemands;

            public NativeArray<int> m_StorageCompanyDemands;

            [ReadOnly]
            public NativeArray<int> m_Productions;


            [ReadOnly]
            public NativeArray<int> m_Companies;

            public NativeArray<int> m_FreeProperties;

            [ReadOnly]
            public NativeArray<int> m_Propertyless;

            [ReadOnly]
            public NativeArray<int> m_TotalMaxWorkers;

            [ReadOnly]
            public NativeArray<int> m_TotalCurrentWorkers;

            public NativeArray<int> m_FreeStorages;

            public NativeArray<int> m_Storages;

            public NativeArray<int> m_StorageCapacities;

            public NativeArray<int> m_CachedDemands;

            public NativeValue<int> m_IndustrialCompanyDemand;

            [ReadOnly]
            public NativeArray<int> m_CompanyResourceDemands;

            public NativeArray<int> m_ResourceDemands;

            public NativeArray<int> m_Results; // InfoLoom

            public NativeValue<Resource> m_ExcludedResources; // InfoLoom

            // resource demand for various consumers
            public NativeArray<int> m_PopulationDemand;
            public NativeArray<int> m_CityServicesDemand;
            public NativeArray<int> m_SpawnablesDemand;
            public NativeArray<int> m_IndustrialDemand;

            public void Execute()
            {
                DynamicBuffer<CityModifier> modifiers = m_CityModifiers[m_City];
                DynamicBuffer<TradeCost> costs = m_TradeCosts[m_City];
                Population population = m_Populations[m_City];
                Tourism tourism = m_Tourisms[m_City];
                ResourceIterator iterator = ResourceIterator.GetIterator();
                while (iterator.Next())
                {
                    int resourceIndex = EconomyUtils.GetResourceIndex(iterator.resource);
                    ResourceData resourceData = m_ResourceDatas[m_ResourcePrefabs[iterator.resource]];
                    m_ResourceDemands[resourceIndex] = ((m_CompanyResourceDemands[resourceIndex] == 0 && EconomyUtils.IsIndustrialResource(resourceData, includeMaterial: false, includeOffice: false)) ? 100 : m_CompanyResourceDemands[resourceIndex]);
                    m_FreeProperties[resourceIndex] = 0;
                    m_Storages[resourceIndex] = 0;
                    m_FreeStorages[resourceIndex] = 0;
                    m_StorageCapacities[resourceIndex] = 0;
                }
                for (int i = 0; i < m_DemandFactors.Length; i++)
                {
                    m_DemandFactors[i] = 0;
                }
                for (int j = 0; j < m_OfficeDemandFactors.Length; j++)
                {
                    m_OfficeDemandFactors[j] = 0;
                }
                // Add city services upkeep
                for (int k = 0; k < m_CityServiceChunks.Length; k++)
                {
                    ArchetypeChunk archetypeChunk = m_CityServiceChunks[k];
                    if (!archetypeChunk.Has(ref m_ServiceUpkeepType))
                    {
                        continue;
                    }
                    NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
                    NativeArray<PrefabRef> nativeArray2 = archetypeChunk.GetNativeArray(ref m_PrefabType);
                    for (int l = 0; l < nativeArray2.Length; l++)
                    {
                        Entity prefab = nativeArray2[l].m_Prefab;
                        Entity entity = nativeArray[l];
                        if (m_ServiceUpkeeps.HasBuffer(prefab))
                        {
                            DynamicBuffer<ServiceUpkeepData> dynamicBuffer = m_ServiceUpkeeps[prefab];
                            for (int m = 0; m < dynamicBuffer.Length; m++)
                            {
                                ServiceUpkeepData serviceUpkeepData = dynamicBuffer[m];
                                if (serviceUpkeepData.m_Upkeep.m_Resource != Resource.Money)
                                {
                                    int amount = serviceUpkeepData.m_Upkeep.m_Amount;
                                    m_CachedDemands[EconomyUtils.GetResourceIndex(serviceUpkeepData.m_Upkeep.m_Resource)] += amount;
                                }
                            }
                        }
                        if (!m_InstalledUpgrades.HasBuffer(entity))
                        {
                            continue;
                        }
                        DynamicBuffer<InstalledUpgrade> dynamicBuffer2 = m_InstalledUpgrades[entity];
                        for (int n = 0; n < dynamicBuffer2.Length; n++)
                        {
                            Entity upgrade = dynamicBuffer2[n].m_Upgrade;
                            if (!m_Prefabs.HasComponent(upgrade))
                            {
                                continue;
                            }
                            Entity prefab2 = m_Prefabs[upgrade].m_Prefab;
                            if (m_Upkeeps.HasBuffer(prefab2))
                            {
                                DynamicBuffer<ServiceUpkeepData> dynamicBuffer3 = m_Upkeeps[prefab2];
                                for (int num = 0; num < dynamicBuffer3.Length; num++)
                                {
                                    ServiceUpkeepData serviceUpkeepData2 = dynamicBuffer3[num];
                                    m_CachedDemands[EconomyUtils.GetResourceIndex(serviceUpkeepData2.m_Upkeep.m_Resource)] += serviceUpkeepData2.m_Upkeep.m_Amount;
                                }
                            }
                        }
                    }
                }

                // InfoLoom
                for (int n = 0; n < m_CityServicesDemand.Length; n++)
                    m_CityServicesDemand[n] = m_CachedDemands[n] - math.max(m_CompanyResourceDemands[n], m_PopulationDemand[n]);

                // Add spawnable buildings demand for Timber, Concrete, Petrochemicals and Wood
                float num2 = 0f;
                float num3 = 0f;
                float num4 = 0f;
                float num5 = 0f;
                float price = m_ResourceDatas[m_ResourcePrefabs[Resource.Timber]].m_Price.x;
                float price2 = m_ResourceDatas[m_ResourcePrefabs[Resource.Concrete]].m_Price.x;
                float price3 = m_ResourceDatas[m_ResourcePrefabs[Resource.Petrochemicals]].m_Price.x;
                float price4 = m_ResourceDatas[m_ResourcePrefabs[Resource.Wood]].m_Price.x;
                for (int num6 = 0; num6 < m_SpawnableChunks.Length; num6++)
                {
                    NativeArray<PrefabRef> nativeArray3 = m_SpawnableChunks[num6].GetNativeArray(ref m_PrefabType);
                    for (int num7 = 0; num7 < nativeArray3.Length; num7++)
                    {
                        Entity prefab3 = nativeArray3[num7].m_Prefab;
                        if (m_ConsumptionDatas.HasComponent(prefab3))
                        {
                            int num8 = m_ConsumptionDatas[prefab3].m_Upkeep / BuildingUpkeepSystem.kMaterialUpkeep;
                            num2 += (float)num8 / price / 2f;
                            num3 += (float)num8 / price2 / 2f;
                        }
                        BuildingData buildingData = m_BuildingDatas[prefab3];
                        BuildingPropertyData buildingPropertyData = m_BuildingPropertyDatas[prefab3];
                        float num9 = math.sqrt(buildingData.m_LotSize.x * buildingData.m_LotSize.y * buildingPropertyData.CountProperties()) * m_HeatingNeed;
                        num4 += 0.5f * num9 / (5f * price3);
                        num5 += 0.5f * num9 / price4;
                    }
                }
                m_CachedDemands[EconomyUtils.GetResourceIndex(Resource.Timber)] += Mathf.RoundToInt(num2);
                m_CachedDemands[EconomyUtils.GetResourceIndex(Resource.Concrete)] += Mathf.RoundToInt(num3);
                m_CachedDemands[EconomyUtils.GetResourceIndex(Resource.Petrochemicals)] += Mathf.RoundToInt(num4);
                m_CachedDemands[EconomyUtils.GetResourceIndex(Resource.Wood)] += Mathf.RoundToInt(num5);

                // InfoLoom
                m_SpawnablesDemand[EconomyUtils.GetResourceIndex(Resource.Timber)] = Mathf.RoundToInt(num2);
                m_SpawnablesDemand[EconomyUtils.GetResourceIndex(Resource.Concrete)] = Mathf.RoundToInt(num3);
                m_SpawnablesDemand[EconomyUtils.GetResourceIndex(Resource.Petrochemicals)] = Mathf.RoundToInt(num4);
                m_SpawnablesDemand[EconomyUtils.GetResourceIndex(Resource.Wood)] = Mathf.RoundToInt(num5);

                //Plugin.Log($"Spawnable demand: Timber {num2:F0} Concrete {num3:F0} Petrochem {num4:F0} Wood {num5:F0}");
                // Add industrial demand for some specific resources
                int num10 = 0;
                int num11 = 0;
                for (int num12 = 0; num12 < m_Productions.Length; num12++)
                {
                    Resource resource = EconomyUtils.GetResource(num12);
                    ResourceData resourceData = m_ResourceDatas[m_ResourcePrefabs[resource]];
                    if (resourceData.m_IsProduceable)
                    {
                        if (resourceData.m_Weight > 0f)
                        {
                            num10 += m_Productions[num12];
                        }
                        else
                        {
                            num11 += m_Productions[num12];
                        }
                    }
                }
                int num13 = num11 + num10;
                m_CachedDemands[EconomyUtils.GetResourceIndex(Resource.Machinery)] += num10 / 2000;
                m_CachedDemands[EconomyUtils.GetResourceIndex(Resource.Paper)] += num11 / 4000;
                m_CachedDemands[EconomyUtils.GetResourceIndex(Resource.Furniture)] += num11 / 4000;
                m_CachedDemands[EconomyUtils.GetResourceIndex(Resource.Software)] += num13 / 2000;
                m_CachedDemands[EconomyUtils.GetResourceIndex(Resource.Financial)] += num13 / 2000;
                m_CachedDemands[EconomyUtils.GetResourceIndex(Resource.Telecom)] += num13 / 2000;

                // InfoLoom
                m_IndustrialDemand[EconomyUtils.GetResourceIndex(Resource.Machinery)] = num10 / 2000;
                m_IndustrialDemand[EconomyUtils.GetResourceIndex(Resource.Paper)] = num11 / 4000;
                m_IndustrialDemand[EconomyUtils.GetResourceIndex(Resource.Furniture)] = num11 / 4000;
                m_IndustrialDemand[EconomyUtils.GetResourceIndex(Resource.Software)] = num13 / 2000;
                m_IndustrialDemand[EconomyUtils.GetResourceIndex(Resource.Financial)] = num13 / 2000;
                m_IndustrialDemand[EconomyUtils.GetResourceIndex(Resource.Telecom)] = num13 / 2000;

                //Plugin.Log($"Industrial demand: Machinery {num10/2000} Paper {num11/4000} Furniture {num11/4000} Software {num13/2000} Financial {num13/2000} Telecom {num13/2000}");
                // Count storage capacities
                for (int num14 = 0; num14 < m_StorageCompanyChunks.Length; num14++)
                {
                    ArchetypeChunk archetypeChunk2 = m_StorageCompanyChunks[num14];
                    NativeArray<Entity> nativeArray4 = archetypeChunk2.GetNativeArray(m_EntityType);
                    NativeArray<PrefabRef> nativeArray5 = archetypeChunk2.GetNativeArray(ref m_PrefabType);
                    for (int num15 = 0; num15 < nativeArray4.Length; num15++)
                    {
                        Entity entity2 = nativeArray4[num15];
                        Entity prefab4 = nativeArray5[num15].m_Prefab;
                        if (m_IndustrialProcessDatas.HasComponent(prefab4))
                        {
                            int resourceIndex2 = EconomyUtils.GetResourceIndex(m_IndustrialProcessDatas[prefab4].m_Output.m_Resource);
                            m_Storages[resourceIndex2]++;
                            StorageLimitData storageLimitData = m_StorageLimitDatas[prefab4];
                            if (!m_PropertyRenters.HasComponent(entity2) || !m_Prefabs.HasComponent(m_PropertyRenters[entity2].m_Property))
                            {
                                m_FreeStorages[resourceIndex2]--;
                                m_StorageCapacities[resourceIndex2] += kStorageCompanyEstimateLimit;
                            }
                            else
                            {
                                Entity property = m_PropertyRenters[entity2].m_Property;
                                Entity prefab5 = m_Prefabs[property].m_Prefab;
                                m_StorageCapacities[resourceIndex2] += storageLimitData.GetAdjustedLimitForWarehouse(m_SpawnableBuildingDatas[prefab5], m_BuildingDatas[prefab5]);
                            }
                        }
                    }
                }
                // Count free properties and free storages
                for (int num16 = 0; num16 < m_FreePropertyChunks.Length; num16++)
                {
                    ArchetypeChunk archetypeChunk3 = m_FreePropertyChunks[num16];
                    NativeArray<Entity> nativeArray6 = archetypeChunk3.GetNativeArray(m_EntityType);
                    NativeArray<PrefabRef> nativeArray7 = archetypeChunk3.GetNativeArray(ref m_PrefabType);
                    for (int num17 = 0; num17 < nativeArray7.Length; num17++)
                    {
                        Entity prefab6 = nativeArray7[num17].m_Prefab;
                        if (!m_BuildingPropertyDatas.HasComponent(prefab6))
                        {
                            continue;
                        }
                        BuildingPropertyData buildingPropertyData2 = m_BuildingPropertyDatas[prefab6];
                        if (m_Attached.TryGetComponent(nativeArray6[num17], out var componentData) && m_Prefabs.TryGetComponent(componentData.m_Parent, out var componentData2) && m_BuildingPropertyDatas.TryGetComponent(componentData2.m_Prefab, out var componentData3))
                        {
                            buildingPropertyData2.m_AllowedManufactured &= componentData3.m_AllowedManufactured;
                        }
                        ResourceIterator iterator2 = ResourceIterator.GetIterator();
                        while (iterator2.Next())
                        {
                            int resourceIndex3 = EconomyUtils.GetResourceIndex(iterator2.resource);
                            if ((buildingPropertyData2.m_AllowedManufactured & iterator2.resource) != Resource.NoResource)
                            {
                                m_FreeProperties[resourceIndex3]++;
                            }
                            if ((buildingPropertyData2.m_AllowedStored & iterator2.resource) != Resource.NoResource)
                            {
                                m_FreeStorages[resourceIndex3]++;
                            }
                        }
                        // InfoLoom
                        if ((buildingPropertyData2.m_AllowedManufactured & kIndustryResources) != Resource.NoResource)
                        {
                            //Plugin.Log($"Free industry: {buildingPropertyData2.m_AllowedManufactured}");
                            m_Results[0]++;
                        }
                        if ((buildingPropertyData2.m_AllowedManufactured & kOfficeResources) != Resource.NoResource)
                        {
                            //Plugin.Log($"Free office: {buildingPropertyData2.m_AllowedManufactured}");
                            m_Results[10]++;
                        }
                        if (buildingPropertyData2.m_AllowedStored != Resource.NoResource)
                        {
                            //Plugin.Log($"Free storage: {buildingPropertyData2.m_AllowedStored}");
                            m_Results[5]++;
                        }
                    }
                }
                // MAIN LOOP, demand calculation per resource
                bool flag = m_IndustrialBuildingDemand.value > 0;
                bool flag2 = m_OfficeBuildingDemand.value > 0;
                bool flag3 = m_StorageBuildingDemand.value > 0;
                m_IndustrialCompanyDemand.value = 0;
                m_IndustrialBuildingDemand.value = 0;
                m_StorageCompanyDemand.value = 0;
                m_StorageBuildingDemand.value = 0;
                m_OfficeCompanyDemand.value = 0;
                m_OfficeBuildingDemand.value = 0;
                int num18 = 0; // counts all office resources
                int num19 = 0; // counts all indutry resources

                // InfoLoom: available workforce
                for (int m = 0; m < 5; m++)
                {
                    int employable = math.max(0, m_EmployableByEducation[m] - m_FreeWorkplaces[m]);
                    if (m >= 2) m_Results[8] += employable;
                    else m_Results[9] += employable;
                }

                // InfoLoom: input utilization, production capacity, tax rates, employee capacity
                float prodCapInd = 0f, prodCapOff = 0f;
                float taxRateInd = 0f, taxRateOff = 0f, empCapInd = 0f, empCapOff = 0f;
                float inputUtil = 0f; // input utilization
                int numInputs = 0;

                iterator = ResourceIterator.GetIterator();
                while (iterator.Next())
                {
                    int resourceIndex4 = EconomyUtils.GetResourceIndex(iterator.resource);
                    if (!m_ResourceDatas.HasComponent(m_ResourcePrefabs[iterator.resource]))
                    {
                        continue;
                    }
                    ResourceData resourceData2 = m_ResourceDatas[m_ResourcePrefabs[iterator.resource]];
                    bool isProduceable = resourceData2.m_IsProduceable;
                    bool isMaterial = resourceData2.m_IsMaterial;
                    bool isTradable = resourceData2.m_IsTradable;
                    bool flag4 = resourceData2.m_Weight == 0f; // IMMATERIAL RESOURCE
                    if (isTradable && !flag4)
                    {
                        int num20 = m_CachedDemands[resourceIndex4];
                        m_StorageCompanyDemands[resourceIndex4] = 0;
                        m_StorageBuildingDemands[resourceIndex4] = 0;
                        if (num20 > kStorageProductionDemand && m_StorageCapacities[resourceIndex4] < num20)
                        {
                            m_StorageCompanyDemands[resourceIndex4] = 1;
                        }
                        if (m_FreeStorages[resourceIndex4] < 0)
                        {
                            // more companies than buildings
                            m_StorageBuildingDemands[resourceIndex4] = 1;
                            m_Results[6] -= m_FreeStorages[resourceIndex4]; // InfoLoom: we subtract because it is a negative value
                        }
                        m_StorageCompanyDemand.value += m_StorageCompanyDemands[resourceIndex4];
                        m_StorageBuildingDemand.value += m_StorageBuildingDemands[resourceIndex4];
                        m_DemandFactors[17] += math.max(0, m_StorageBuildingDemands[resourceIndex4]); // LocalStorage
                    }
                    if (!isProduceable)
                    {
                        continue;
                    }
                    float value = (isMaterial ? m_DemandParameters.m_ExtractorBaseDemand : m_DemandParameters.m_IndustrialBaseDemand);
                    float num21 = (1f + (float)m_CachedDemands[resourceIndex4] - (float)m_Productions[resourceIndex4]) / ((float)m_CachedDemands[resourceIndex4] + 1f);
                    float productionCapacity = math.min(4, (float)m_Productions[resourceIndex4] / ((float)m_CachedDemands[resourceIndex4] + 1f)); // InfoLoom, capped at 400%
                                                                                                                                                  //_ = resourceData2.m_Price / resourceData2.m_Weight;
                    TradeCost tradeCost = EconomyUtils.GetTradeCost(EconomyUtils.GetResource(resourceIndex4), costs);
                    float num22 = (0.05f + tradeCost.m_SellCost) / resourceData2.m_Price.x;
                    float num23 = (0.05f + tradeCost.m_BuyCost) / resourceData2.m_Price.x;
                    float refCost = 0.05f / resourceData2.m_Price.x;
                    num21 *= ((m_Productions[resourceIndex4] > m_CachedDemands[resourceIndex4]) ? (10f * num22) : (10f * num23));
                    //Plugin.Log($"{iterator.resource} ({resourceIndex4}): price {resourceData2.m_Price} sellCost {tradeCost.m_SellCost} buyCost {tradeCost.m_BuyCost} refCost {refCost} num22 {num22} ({num22/refCost}) num23 {num23} ({num23/refCost})");
                    if (iterator.resource == Resource.Electronics)
                    {
                        CityUtils.ApplyModifier(ref value, modifiers, CityModifierType.IndustrialElectronicsDemand);
                    }
                    else if (iterator.resource == Resource.Software)
                    {
                        CityUtils.ApplyModifier(ref value, modifiers, CityModifierType.OfficeSoftwareDemand);
                    }
                    float num24 = -1.8f + 2.5f * (((float)m_TotalCurrentWorkers[resourceIndex4] + 1f) / ((float)m_TotalMaxWorkers[resourceIndex4] + 1f));
                    EconomyUtils.GetProcessComplexity(m_IndustrialProcessDataChunks, ref m_WorkplaceDatas, iterator.resource, m_EntityType, m_ProcessType, out var complexity);
                    Workplaces workplaces = EconomyUtils.CalculateNumberOfWorkplaces(20, complexity, 3);
                    float num25 = 0f;
                    for (int num26 = 0; num26 < 5; num26++)
                    {
                        num25 = ((num26 >= 2) ? (num25 + math.min(5f * (float)workplaces[num26], math.max(0, m_EmployableByEducation[num26] - m_FreeWorkplaces[num26]))) : (num25 + 5f * (float)workplaces[num26]));
                    }
                    float num27 = 1f * (num25 / 50f - 1f);
                    int num28 = (flag4 ? TaxSystem.GetOfficeTaxRate(iterator.resource, m_TaxRates) : TaxSystem.GetIndustrialTaxRate(iterator.resource, m_TaxRates));
                    float num29 = -0.1f * ((float)num28 - 10f);
                    float num30 = 0f;
                    if (!flag4) // weight > 0
                    {
                        m_IndustrialDemands[resourceIndex4] = Mathf.RoundToInt(100f * (math.max(-1f, value * num21) + num24 + num27 + num29));
                        if (!isMaterial)
                        {
                            num30 = float.NegativeInfinity;
                            for (int num31 = 0; num31 < m_IndustrialProcessDataChunks.Length; num31++)
                            {
                                ArchetypeChunk archetypeChunk4 = m_IndustrialProcessDataChunks[num31];
                                NativeArray<IndustrialProcessData> nativeArray8 = archetypeChunk4.GetNativeArray(ref m_ProcessType);
                                for (int num32 = 0; num32 < archetypeChunk4.Count; num32++)
                                {
                                    IndustrialProcessData industrialProcessData = nativeArray8[num32];
                                    if (industrialProcessData.m_Output.m_Resource == iterator.resource && industrialProcessData.m_Input1.m_Resource != iterator.resource)
                                    {
                                        float num33 = 0f;
                                        bool flag5 = false;
                                        if (industrialProcessData.m_Input1.m_Amount != 0)
                                        {
                                            Entity entity3 = m_ResourcePrefabs[industrialProcessData.m_Input1.m_Resource];
                                            int resourceIndex5 = EconomyUtils.GetResourceIndex(industrialProcessData.m_Input1.m_Resource);
                                            float num34 = math.max(-3f, (float)(m_Productions[resourceIndex5] - m_CachedDemands[resourceIndex5]) / ((float)m_Productions[resourceIndex5] + 1f));
                                            ResourceData resourceData3 = m_ResourceDatas[entity3];
                                            //_ = resourceData3.m_Weight / resourceData3.m_Price;
                                            float num35 = EconomyUtils.GetTradeCost(industrialProcessData.m_Input1.m_Resource, costs).m_BuyCost / resourceData3.m_Price.x;
                                            num33 += 10f * num35 * (num34 + 0.1f);
                                            flag5 = true;
                                            inputUtil += math.min(4, (float)m_CachedDemands[resourceIndex5] / ((float)m_Productions[resourceIndex5] + 1f));
                                            numInputs++;
                                            //Plugin.Log($"{iterator.resource} IPD1: {industrialProcessData.m_Input1.m_Resource} inputUtil {num34} tradeCost {num35} => {10f * num35 * (num34 + 0.1f)}");
                                        }
                                        if (industrialProcessData.m_Input2.m_Amount != 0)
                                        {
                                            Entity entity4 = m_ResourcePrefabs[industrialProcessData.m_Input2.m_Resource];
                                            int resourceIndex6 = EconomyUtils.GetResourceIndex(industrialProcessData.m_Input2.m_Resource);
                                            float num36 = math.max(-3f, (float)(m_Productions[resourceIndex6] - m_CachedDemands[resourceIndex6]) / ((float)m_Productions[resourceIndex6] + 1f));
                                            ResourceData resourceData4 = m_ResourceDatas[entity4];
                                            //_ = resourceData4.m_Weight / resourceData4.m_Price;
                                            float num37 = EconomyUtils.GetTradeCost(industrialProcessData.m_Input2.m_Resource, costs).m_BuyCost / resourceData4.m_Price.x;
                                            num33 += 10f * num37 * (num36 + 0.1f);
                                            flag5 = true;
                                            inputUtil += math.min(4, (float)m_CachedDemands[resourceIndex6] / ((float)m_Productions[resourceIndex6] + 1f));
                                            numInputs++;
                                            //Plugin.Log($"{iterator.resource} IPD2: {industrialProcessData.m_Input2.m_Resource} inputUtil {num36} tradeCost {num37} => {10f * num37 * (num36 + 0.1f)}");
                                        }
                                        if (flag5)
                                        {
                                            num30 = math.max(num30, num33);
                                        }
                                    }
                                }
                            }
                            m_IndustrialDemands[resourceIndex4] += Mathf.RoundToInt(100f * num30);
                        }
                    }
                    else // weight == 0
                    {
                        m_IndustrialDemands[resourceIndex4] = Mathf.RoundToInt(100f * (num24 + num27 + num29));
                    }
                    //Plugin.Log($"{iterator.resource} ({resourceIndex4}): rawdemand {m_IndustrialDemands[resourceIndex4]} value {value} num21 {num21} valuenum21 {100f*value*num21:F1} num24 {100*num24:F1} num27 {100f*num27:F1} num29 {100*num29:F1} num30 {100f*num30:F1}");
                    m_IndustrialDemands[resourceIndex4] = math.min(100, math.max(0, m_IndustrialDemands[resourceIndex4]));
                    if (flag4) // weight == 0
                    {
                        m_OfficeCompanyDemand.value += Mathf.RoundToInt(m_IndustrialDemands[resourceIndex4]);
                        num18++;
                    }
                    else // weight > 0
                    {
                        m_IndustrialCompanyDemand.value += Mathf.RoundToInt(m_IndustrialDemands[resourceIndex4]);
                        if (!isMaterial)
                        {
                            num19++;
                        }
                    }
                    m_IndustrialZoningDemands[resourceIndex4] = m_IndustrialDemands[resourceIndex4];
                    if (!isMaterial && m_FreeProperties[resourceIndex4] == 0)
                    {
                        m_IndustrialDemands[resourceIndex4] = 0;
                    }
                    if (m_CachedDemands[resourceIndex4] > 0)
                    {
                        if (!isMaterial && m_IndustrialZoningDemands[resourceIndex4] > 0)
                        {
                            m_IndustrialBuildingDemands[resourceIndex4] = math.max(0, Mathf.CeilToInt(math.max(1f, (float)math.min(1, m_Propertyless[resourceIndex4]) + (float)m_Companies[resourceIndex4] / m_DemandParameters.m_FreeIndustrialProportion) - (float)m_FreeProperties[resourceIndex4]));
                        }
                        else if (m_IndustrialZoningDemands[resourceIndex4] > 0)
                        {
                            m_IndustrialBuildingDemands[resourceIndex4] = 1;
                        }
                        else
                        {
                            m_IndustrialBuildingDemands[resourceIndex4] = 0;
                        }
                        if (m_IndustrialBuildingDemands[resourceIndex4] > 0)
                        {
                            if (flag4) // weight == 0
                            {
                                m_OfficeBuildingDemand.value += ((m_IndustrialBuildingDemands[resourceIndex4] > 0) ? m_IndustrialZoningDemands[resourceIndex4] : 0);
                            }
                            else if (!isMaterial) // weight > 0
                            {
                                m_IndustrialBuildingDemand.value += ((m_IndustrialBuildingDemands[resourceIndex4] > 0) ? m_IndustrialZoningDemands[resourceIndex4] : 0);
                            }
                        }
                        //Plugin.Log($"Com {iterator.resource}: noprop {m_Propertyless[resourceIndex2]} comp {m_Companies[resourceIndex2]} free {m_FreeProperties[resourceIndex2]} resdem {num8}");
                        if (flag4)
                            m_Results[11] += m_Propertyless[resourceIndex4];
                        else if (!isMaterial)
                            m_Results[1] += m_Propertyless[resourceIndex4];
                    }
                    if (isMaterial)
                    {
                        continue;
                    }
                    // HERE EXTRACTORS ARE GONE

                    //Plugin.Log($"Res {iterator.resource} ({num}): free {m_FreeProperties[resourceIndex2]} buldem {m_BuildingDemands[resourceIndex2]} wrkdem {num2} [ edu {num3:F2} wrk {num4:F2} cap {num5:F2} rat {num6:F2} tax {num7:F2} ] resdem {num8}");
                    // InfoLoom gather data
                    if (flag4) // weight == 0, Office
                    {
                        prodCapOff += productionCapacity;
                        empCapOff += ((float)m_TotalCurrentWorkers[resourceIndex4] + 1f) / ((float)m_TotalMaxWorkers[resourceIndex4] + 1f);
                        taxRateOff += num28;
                    }
                    else // weight > 0, Industry
                    {
                        prodCapInd += productionCapacity;
                        empCapInd += ((float)m_TotalCurrentWorkers[resourceIndex4] + 1f) / ((float)m_TotalMaxWorkers[resourceIndex4] + 1f);
                        taxRateInd += num28;
                    }

                    // InfoLoom, summary
                    //Plugin.Log($"{iterator.resource} ({resourceIndex4}): office {flag4} companies {m_Companies[resourceIndex4]} propLess {m_Propertyless[resourceIndex4]} freeProp {m_FreeProperties[resourceIndex4]} bldg {m_IndustrialBuildingDemands[resourceIndex4]} zone {m_IndustrialZoningDemands[resourceIndex4]}");
                    //Plugin.Log($"{iterator.resource} ({resourceIndex4}): work [1] {num24} edu [2] {num27} tax [11] {num29} inputs [10] {num30}");
                    //Plugin.Log($"{iterator.resource} ({resourceIndex4}): base {value} local [4] {num21} prodCap {productionCapacity} sell {num22} buy {num23}");

                    if (flag4) // weight == 0
                    {
                        if (!flag2 || (m_IndustrialBuildingDemands[resourceIndex4] > 0 && m_IndustrialZoningDemands[resourceIndex4] > 0))
                        {
                            int num38 = ((m_IndustrialBuildingDemands[resourceIndex4] > 0) ? m_IndustrialZoningDemands[resourceIndex4] : 0);
                            int demandFactorEffect = DemandUtils.GetDemandFactorEffect(m_IndustrialDemands[resourceIndex4], num27);
                            int demandFactorEffect2 = DemandUtils.GetDemandFactorEffect(m_IndustrialDemands[resourceIndex4], num24);
                            int demandFactorEffect3 = DemandUtils.GetDemandFactorEffect(m_IndustrialDemands[resourceIndex4], num29);
                            int num39 = demandFactorEffect + demandFactorEffect2 + demandFactorEffect3;
                            m_OfficeDemandFactors[2] += demandFactorEffect; // EducatedWorkforce 
                            m_OfficeDemandFactors[1] += demandFactorEffect2; // UneducatedWorkforce 
                            m_OfficeDemandFactors[11] += demandFactorEffect3; // Taxes 
                            m_OfficeDemandFactors[13] += math.min(0, num38 - num39); // EmptyBuildings 
                        }
                        // InfoLoom - no demand
                        else
                        {
                            //Plugin.Log($"No office demand for: {iterator.resource}");
                            m_ExcludedResources.value |= iterator.resource;
                        }
                    }
                    else if ((!flag && !flag3) || (m_IndustrialBuildingDemands[resourceIndex4] > 0 && m_IndustrialZoningDemands[resourceIndex4] > 0)) // weight > 0
                    {
                        int num40 = ((m_IndustrialBuildingDemands[resourceIndex4] > 0) ? m_IndustrialZoningDemands[resourceIndex4] : 0);
                        int demandFactorEffect4 = DemandUtils.GetDemandFactorEffect(m_IndustrialDemands[resourceIndex4], num27);
                        int demandFactorEffect5 = DemandUtils.GetDemandFactorEffect(m_IndustrialDemands[resourceIndex4], num24);
                        int demandFactorEffect6 = DemandUtils.GetDemandFactorEffect(m_IndustrialDemands[resourceIndex4], math.max(0f, value * num21));
                        int demandFactorEffect7 = DemandUtils.GetDemandFactorEffect(m_IndustrialDemands[resourceIndex4], num30);
                        int demandFactorEffect8 = DemandUtils.GetDemandFactorEffect(m_IndustrialDemands[resourceIndex4], num29);
                        int num41 = demandFactorEffect4 + demandFactorEffect5 + demandFactorEffect6 + demandFactorEffect7 + demandFactorEffect8;
                        m_DemandFactors[2] += demandFactorEffect4; // EducatedWorkforce 
                        m_DemandFactors[1] += demandFactorEffect5; // UneducatedWorkforce 
                        m_DemandFactors[4] += demandFactorEffect6; // LocalDemand 
                        m_DemandFactors[10] += demandFactorEffect7; // LocalInputs 
                        m_DemandFactors[11] += demandFactorEffect8; // Taxes 
                        m_DemandFactors[13] += math.min(0, num40 - num41); // EmptyBuildings 
                    }
                    // InfoLoom - no demand, TODO COUNT
                    else
                    {
                        //Plugin.Log($"No industry demand for: {iterator.resource}");
                        m_ExcludedResources.value |= iterator.resource;
                    }
                }

                // InfoLoom, storage section
                //m_Results[28] = m_StorageBuildingDemand.value; // basically says number of resources that need storage
                m_Results[15] = m_StorageCompanyDemand.value; // basically says number of resources where storage capacities don't meet demand
                                                              //Plugin.Log($"STORAGE: free [5] {m_Results[5]} propLess/demand [6] {m_Results[6]} companies [15] {m_Results[15]}");

                //Plugin.Log($"Native  values building/company/numres: IND {m_IndustrialBuildingDemand.value}/{m_IndustrialCompanyDemand.value}/{num19} " +
                //    $"STO {m_StorageBuildingDemand.value}/{m_StorageCompanyDemand.value} OFF {m_OfficeBuildingDemand.value}/{m_OfficeCompanyDemand.value}/{num18} ");
                m_StorageBuildingDemand.value = Mathf.CeilToInt(math.pow(20f * (float)m_StorageBuildingDemand.value, 0.75f));
                m_IndustrialBuildingDemand.value = 2 * m_IndustrialBuildingDemand.value / num19; // Infixo: THIS IS ERROR
                m_OfficeCompanyDemand.value *= 2 * m_OfficeCompanyDemand.value / num18; // Infixo: THIS IS ERROR
                m_IndustrialBuildingDemand.value = math.clamp(m_IndustrialBuildingDemand.value, 0, 100);
                m_OfficeBuildingDemand.value = math.clamp(m_OfficeBuildingDemand.value, 0, 100);
                //Plugin.Log($"Clamped values building/company/numres: IND {m_IndustrialBuildingDemand.value}/{m_IndustrialCompanyDemand.value}/{num19} " +
                //    $"STO {m_StorageBuildingDemand.value}/{m_StorageCompanyDemand.value} OFF {m_OfficeBuildingDemand.value}/{m_OfficeCompanyDemand.value}/{num18} ");
                // InfoLoom
                //Plugin.Log($"RESOURCES: ind {num19} off {num18} excluded {EconomyUtils.GetNames(m_ExcludedResources.value)}");
                //Plugin.Log($"INDUSTRY: freeProperties [0]={m_Results[0]} propertyless [1]={m_Results[1]}");
                //Plugin.Log($"OFFICE: freeProperties [10]={m_Results[10]} propertyless [11]={m_Results[11]}");

                // InfoLoom, tax section
                m_Results[2] = Mathf.RoundToInt(10f * taxRateInd / (float)num19);
                m_Results[12] = Mathf.RoundToInt(10f * taxRateOff / (float)num18);
                //Plugin.Log($"TAX RATE: [2]={m_Results[2]} {taxRateInd / (float)num19:F1} [12]={m_Results[12]} {taxRateOff / (float)num18:F1}");

                // InfoLoom, input utilization, only for industry
                m_Results[7] = Mathf.RoundToInt(100f * inputUtil / (float)numInputs);
                //Plugin.Log($"INPUT UTILIZATION: [7]={m_Results[7]} raw {inputUtil} num {numInputs}, 110% is the default threshold");

                // InfoLoom, production capacity => local demand
                m_Results[3] = Mathf.RoundToInt(100f * prodCapInd / (float)num19);
                m_Results[13] = Mathf.RoundToInt(100f * prodCapOff / (float)num18);
                //Plugin.Log($"LOCAL DEMAND: [3]={m_Results[3]} ({prodCapInd / (float)num19}) [13]={m_Results[13]} ({prodCapOff / (float)num18}), 100% means production = demand");

                // InfoLoom, employment section
                m_Results[4] = Mathf.RoundToInt(1000f * empCapInd / (float)num19);
                m_Results[14] = Mathf.RoundToInt(1000f * empCapOff / (float)num18);
                //Plugin.Log($"EMPLOYEE CAPACITY RATIO: [4]={m_Results[4]} ({empCapInd}) [14]={m_Results[14]} ({empCapOff}), 72% is the default threshold");
                //Plugin.Log($"AVAILABLE WORKFORCE: educated [8]={m_Results[8]} uneducated [9]={m_Results[9]}");
            }
        }

       

        private const Resource kOfficeResources = Resource.Software | Resource.Media | Resource.Telecom | Resource.Financial;

        private const Resource kIndustryResources =
            Resource.ConvenienceFood |
            Resource.Food |
            Resource.Timber |
            Resource.Paper |
            Resource.Furniture |
            Resource.Vehicles |
            Resource.Petrochemicals |
            Resource.Plastics |
            Resource.Metals |
            Resource.Electronics |
            Resource.Steel |
            Resource.Minerals |
            Resource.Concrete |
            Resource.Machinery |
            Resource.Chemicals |
            Resource.Pharmaceuticals |
            Resource.Beverages |
            Resource.Textiles;

        private const string kGroup = "cityInfo";

        private SimulationSystem m_SimulationSystem;

        private static readonly int kStorageProductionDemand = 20000;

        private static readonly int kStorageCompanyEstimateLimit = 864000;

        private ResourceSystem m_ResourceSystem;

        private CitySystem m_CitySystem;

        private ClimateSystem m_ClimateSystem;

        private TaxSystem m_TaxSystem;

        private CountHouseholdDataSystem m_CountHouseholdDataSystem;

        private CountWorkplacesSystem m_CountWorkplacesSystem;

        private CountCompanyDataSystem m_CountCompanyDataSystem;

        private EntityQuery m_EconomyParameterQuery;

        private EntityQuery m_DemandParameterQuery;

        private EntityQuery m_FreeIndustrialQuery;

        private EntityQuery m_StorageCompanyQuery;

        private EntityQuery m_ProcessDataQuery;

        private EntityQuery m_CityServiceQuery;

        private EntityQuery m_SpawnableQuery;

        private NativeValue<int> m_IndustrialCompanyDemand;

        private NativeValue<int> m_IndustrialBuildingDemand;

        private NativeValue<int> m_StorageCompanyDemand;

        private NativeValue<int> m_StorageBuildingDemand;

        private NativeValue<int> m_OfficeCompanyDemand;

        private NativeValue<int> m_OfficeBuildingDemand;

        [ResourceArray]
        [DebugWatchValue]
        private NativeArray<int> m_CachedDemands;

        //[EnumArray(typeof(DemandFactor))]
        //[DebugWatchValue]
        private NativeArray<int> m_IndustrialDemandFactors;

        //[EnumArray(typeof(DemandFactor))]
        //[DebugWatchValue]
        private NativeArray<int> m_OfficeDemandFactors;

        //[ResourceArray]
        //[DebugWatchValue]
        private NativeArray<int> m_ResourceDemands;

        private NativeArray<int> m_IndustrialCompanyDemands;

        //[ResourceArray]
        //[DebugWatchValue]
        private NativeArray<int> m_IndustrialZoningDemands;

        //[ResourceArray]
        //[DebugWatchValue]
        private NativeArray<int> m_IndustrialBuildingDemands;

        //[ResourceArray]
        //[DebugWatchValue]
        private NativeArray<int> m_StorageBuildingDemands;

        //[ResourceArray]
        //[DebugWatchValue]
        private NativeArray<int> m_StorageCompanyDemands;

        //[ResourceArray]
        //[DebugWatchValue]
        private NativeArray<int> m_FreeProperties;

        //[ResourceArray]
        //[DebugWatchValue]
        private NativeArray<int> m_FreeStorages;

        //[ResourceArray]
        //[DebugWatchValue]
        private NativeArray<int> m_Storages;

        //[ResourceArray]
        //[DebugWatchValue]
        private NativeArray<int> m_StorageCapacities;

        //[DebugWatchDeps]
        //private JobHandle m_WriteDependencies;

        private JobHandle m_ReadDependencies;

        private int m_LastIndustrialCompanyDemand;

        private int m_LastIndustrialBuildingDemand;

        private int m_LastStorageCompanyDemand;

        private int m_LastStorageBuildingDemand;

        private int m_LastOfficeCompanyDemand;

        private int m_LastOfficeBuildingDemand;

        

        //[DebugWatchValue(color = "#f7dc6f")]
        public int industrialCompanyDemand => m_LastIndustrialCompanyDemand;

        //[DebugWatchValue(color = "#b7950b")]
        public int industrialBuildingDemand => m_LastIndustrialBuildingDemand;

        //[DebugWatchValue(color = "#cccccc")]
        public int storageCompanyDemand => m_LastStorageCompanyDemand;

        //[DebugWatchValue(color = "#999999")]
        public int storageBuildingDemand => m_LastStorageBuildingDemand;

        //[DebugWatchValue(color = "#af7ac5")]
        public int officeCompanyDemand => m_LastOfficeCompanyDemand;

        //[DebugWatchValue(color = "#6c3483")]
        public int officeBuildingDemand => m_LastOfficeBuildingDemand;

        // InfoLoom

        // resource demand for various consumers

        [ResourceArray]
        [DebugWatchValue]
        public NativeArray<int> m_PopulationDemand;

        [ResourceArray]
        [DebugWatchValue]
        public NativeArray<int> m_CityServicesDemand;

        [ResourceArray]
        [DebugWatchValue]
        public NativeArray<int> m_SpawnablesDemand;

        [ResourceArray]
        [DebugWatchValue]
        public NativeArray<int> m_IndustrialDemand;

        [ResourceArray]
        [DebugWatchValue]
        public NativeArray<int> m_InputDemand;


        private RawValueBinding m_uiResults;
        private RawValueBinding m_uiExResources;

        public NativeArray<int> m_Results;
        public NativeValue<Resource> m_ExcludedResources;

        // INDUSTRIAL (0..7), OFFICE (10..19), STORAGE (20..29)
        // 0 - free properties, 1 - propertyless companies
        // 2 - tax rate
        // 3 & 4 - service utilization rate (available/maximum), non-leisure/leisure
        // 5 & 6 - sales efficiency (sales capacity/consumption), non-leisure/leisure // how effectively a shop is utilizing its sales capacity by comparing the actual sales to the maximum sales potential
        // 7 - employee capacity ratio // how efficiently the company is utilizing its workforce by comparing the actual number of employees to the maximum number it could employ
        // 8 & 9 - educated & uneducated workforce

        // 240209 Set gameMode to avoid errors in the Editor
        

       

        public void AddReader(JobHandle reader)
        {
            m_ReadDependencies = JobHandle.CombineDependencies(m_ReadDependencies, reader);
        }
        
        public bool IsPanelVisible { get; set; }
        public bool ForceUpdate { get; private set; }
        public void ForceUpdateOnce()
        {
            ForceUpdate = true;
        }
       
        protected override void OnCreate()
        {
            base.OnCreate();
            m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>(); // TODO: use UIUpdateState eventually
            m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
            m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
            m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
            m_TaxSystem = base.World.GetOrCreateSystemManaged<TaxSystem>();
            m_CountHouseholdDataSystem = base.World.GetOrCreateSystemManaged<CountHouseholdDataSystem>();
            m_CountWorkplacesSystem = base.World.GetOrCreateSystemManaged<CountWorkplacesSystem>();
            m_CountCompanyDataSystem = base.World.GetOrCreateSystemManaged<CountCompanyDataSystem>();
            m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
            m_DemandParameterQuery = GetEntityQuery(ComponentType.ReadOnly<DemandParameterData>());
            m_FreeIndustrialQuery = GetEntityQuery(ComponentType.ReadOnly<IndustrialProperty>(), ComponentType.ReadOnly<PropertyOnMarket>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Abandoned>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Condemned>());
            m_StorageCompanyQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Game.Companies.StorageCompany>(), ComponentType.Exclude<Game.Objects.OutsideConnection>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
            m_ProcessDataQuery = GetEntityQuery(ComponentType.ReadOnly<IndustrialProcessData>(), ComponentType.Exclude<ServiceCompanyData>());
            m_CityServiceQuery = GetEntityQuery(ComponentType.ReadOnly<CityServiceUpkeep>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
            m_SpawnableQuery = GetEntityQuery(ComponentType.ReadOnly<BuildingCondition>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Abandoned>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Temp>());
            m_IndustrialCompanyDemand = new NativeValue<int>(Allocator.Persistent);
            m_IndustrialBuildingDemand = new NativeValue<int>(Allocator.Persistent);
            m_StorageCompanyDemand = new NativeValue<int>(Allocator.Persistent);
            m_StorageBuildingDemand = new NativeValue<int>(Allocator.Persistent);
            m_OfficeCompanyDemand = new NativeValue<int>(Allocator.Persistent);
            m_OfficeBuildingDemand = new NativeValue<int>(Allocator.Persistent);
            m_IndustrialDemandFactors = new NativeArray<int>(18, Allocator.Persistent);
            m_OfficeDemandFactors = new NativeArray<int>(18, Allocator.Persistent);
            int resourceCount = EconomyUtils.ResourceCount;
            m_ResourceDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
            m_IndustrialZoningDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
            m_IndustrialBuildingDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
            m_ResourceDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
            m_CachedDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
            m_StorageBuildingDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
            m_StorageCompanyDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
            m_FreeProperties = new NativeArray<int>(resourceCount, Allocator.Persistent);
            m_FreeStorages = new NativeArray<int>(resourceCount, Allocator.Persistent);
            m_Storages = new NativeArray<int>(resourceCount, Allocator.Persistent);
            m_StorageCapacities = new NativeArray<int>(resourceCount, Allocator.Persistent);
            RequireForUpdate(m_EconomyParameterQuery);
            RequireForUpdate(m_DemandParameterQuery);
            RequireForUpdate(m_ProcessDataQuery);

            // InfoLoom
            m_PopulationDemand = new NativeArray<int>(resourceCount, Allocator.Persistent);
            m_CityServicesDemand = new NativeArray<int>(resourceCount, Allocator.Persistent);
            m_SpawnablesDemand = new NativeArray<int>(resourceCount, Allocator.Persistent);
            m_IndustrialDemand = new NativeArray<int>(resourceCount, Allocator.Persistent);
            m_InputDemand = new NativeArray<int>(resourceCount, Allocator.Persistent);

            SetDefaults(); // there is no serialization, so init just for safety
            m_Results = new NativeArray<int>(16, Allocator.Persistent);
            m_ExcludedResources = new NativeValue<Resource>(Allocator.Persistent);

            

            
        }

        //[Preserve]
        protected override void OnDestroy()
        {
            m_IndustrialCompanyDemand.Dispose();
            m_IndustrialBuildingDemand.Dispose();
            m_StorageCompanyDemand.Dispose();
            m_StorageBuildingDemand.Dispose();
            m_OfficeCompanyDemand.Dispose();
            m_OfficeBuildingDemand.Dispose();
            m_IndustrialDemandFactors.Dispose();
            m_OfficeDemandFactors.Dispose();
            m_IndustrialCompanyDemands.Dispose();
            m_IndustrialZoningDemands.Dispose();
            m_IndustrialBuildingDemands.Dispose();
            m_StorageBuildingDemands.Dispose();
            m_StorageCompanyDemands.Dispose();
            m_ResourceDemands.Dispose();
            m_FreeProperties.Dispose();
            m_Storages.Dispose();
            m_FreeStorages.Dispose();
            m_StorageCapacities.Dispose();
            // InfoLoom
            m_PopulationDemand.Dispose();
            m_CityServicesDemand.Dispose();
            m_SpawnablesDemand.Dispose();
            m_IndustrialDemand.Dispose();
            m_InputDemand.Dispose();
            base.OnDestroy();
        }

        public void SetDefaults() //Context context)
        {
            m_IndustrialCompanyDemand.value = 0;
            m_IndustrialBuildingDemand.value = 0;
            m_StorageCompanyDemand.value = 0;
            m_StorageBuildingDemand.value = 0;
            m_OfficeCompanyDemand.value = 0;
            m_OfficeBuildingDemand.value = 0;
            m_IndustrialDemandFactors.Fill(0);
            m_OfficeDemandFactors.Fill(0);
            m_IndustrialCompanyDemands.Fill(0);
            m_IndustrialZoningDemands.Fill(0);
            m_IndustrialBuildingDemands.Fill(0);
            m_StorageBuildingDemands.Fill(0);
            m_StorageCompanyDemands.Fill(0);
            m_FreeProperties.Fill(0);
            m_Storages.Fill(0);
            m_FreeStorages.Fill(0);
            m_LastIndustrialCompanyDemand = 0;
            m_LastIndustrialBuildingDemand = 0;
            m_LastStorageCompanyDemand = 0;
            m_LastStorageBuildingDemand = 0;
            m_LastOfficeCompanyDemand = 0;
            m_LastOfficeBuildingDemand = 0;
            // InfoLoom
            m_PopulationDemand.Fill(0);
            m_CityServicesDemand.Fill(0);
            m_SpawnablesDemand.Fill(0);
            m_IndustrialDemand.Fill(0);
            m_InputDemand.Fill(0);
        }

        

       public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 512;
        }
        protected override void OnUpdate()
        {
             if (!IsPanelVisible)
                return;
             
             ForceUpdate = false;
            
            ResetResults();

            if (!m_DemandParameterQuery.IsEmptyIgnoreFilter && !m_EconomyParameterQuery.IsEmptyIgnoreFilter)
            {
                m_LastIndustrialCompanyDemand = m_IndustrialCompanyDemand.value;
                m_LastIndustrialBuildingDemand = m_IndustrialBuildingDemand.value;
                m_LastStorageCompanyDemand = m_StorageCompanyDemand.value;
                m_LastStorageBuildingDemand = m_StorageBuildingDemand.value;
                m_LastOfficeCompanyDemand = m_OfficeCompanyDemand.value;
                m_LastOfficeBuildingDemand = m_OfficeBuildingDemand.value;
                JobHandle deps;
                CountCompanyDataSystem.IndustrialCompanyDatas industrialCompanyDatas = m_CountCompanyDataSystem.GetIndustrialCompanyDatas(out deps);
               
                
                
                
                UpdateIndustrialDemandJob updateIndustrialDemandJob = default(UpdateIndustrialDemandJob);
                updateIndustrialDemandJob.m_FreePropertyChunks = m_FreeIndustrialQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out var outJobHandle);
                updateIndustrialDemandJob.m_StorageCompanyChunks = m_StorageCompanyQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out var outJobHandle2);
                updateIndustrialDemandJob.m_IndustrialProcessDataChunks = m_ProcessDataQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out var outJobHandle3);
                updateIndustrialDemandJob.m_CityServiceChunks = m_CityServiceQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out var outJobHandle4);
                updateIndustrialDemandJob.m_SpawnableChunks = m_SpawnableQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out var outJobHandle5);
                updateIndustrialDemandJob.m_EntityType = SystemAPI.GetEntityTypeHandle();
                updateIndustrialDemandJob.m_PrefabType = SystemAPI.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
                updateIndustrialDemandJob.m_ProcessType = SystemAPI.GetComponentTypeHandle<IndustrialProcessData>(isReadOnly: true);
                updateIndustrialDemandJob.m_ServiceUpkeepType = SystemAPI.GetComponentTypeHandle<CityServiceUpkeep>(isReadOnly: true);
                updateIndustrialDemandJob.m_StorageLimitDatas = SystemAPI.GetComponentLookup<StorageLimitData>(isReadOnly: true);
                updateIndustrialDemandJob.m_SpawnableBuildingDatas = SystemAPI.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
                updateIndustrialDemandJob.m_BuildingDatas = SystemAPI.GetComponentLookup<BuildingData>(isReadOnly: true);
                updateIndustrialDemandJob.m_BuildingPropertyDatas = SystemAPI.GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
                updateIndustrialDemandJob.m_IndustrialProcessDatas = SystemAPI.GetComponentLookup<IndustrialProcessData>(isReadOnly: true);
                updateIndustrialDemandJob.m_Prefabs = SystemAPI.GetComponentLookup<PrefabRef>(isReadOnly: true);
                updateIndustrialDemandJob.m_PropertyRenters = SystemAPI.GetComponentLookup<PropertyRenter>(isReadOnly: true);
                updateIndustrialDemandJob.m_Attached = SystemAPI.GetComponentLookup<Attached>(isReadOnly: true);
                updateIndustrialDemandJob.m_ResourceDatas = SystemAPI.GetComponentLookup<ResourceData>(isReadOnly: true);
                updateIndustrialDemandJob.m_WorkplaceDatas = SystemAPI.GetComponentLookup<WorkplaceData>(isReadOnly: true);
                updateIndustrialDemandJob.m_ServiceUpkeeps = SystemAPI.GetBufferLookup<ServiceUpkeepData>(isReadOnly: true);
                updateIndustrialDemandJob.m_CityModifiers = SystemAPI.GetBufferLookup<CityModifier>(isReadOnly: true);
                updateIndustrialDemandJob.m_ConsumptionDatas = SystemAPI.GetComponentLookup<ConsumptionData>(isReadOnly: true);
                updateIndustrialDemandJob.m_InstalledUpgrades = SystemAPI.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
                updateIndustrialDemandJob.m_Upkeeps = SystemAPI.GetBufferLookup<ServiceUpkeepData>(isReadOnly: true);
                updateIndustrialDemandJob.m_Populations = SystemAPI.GetComponentLookup<Population>(isReadOnly: true);
                updateIndustrialDemandJob.m_Tourisms = SystemAPI.GetComponentLookup<Tourism>(isReadOnly: true);
                updateIndustrialDemandJob.m_TradeCosts = SystemAPI.GetBufferLookup<TradeCost>(isReadOnly: true);
                updateIndustrialDemandJob.m_DemandParameters = m_DemandParameterQuery.GetSingleton<DemandParameterData>();
                updateIndustrialDemandJob.m_EconomyParameters = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>();
                updateIndustrialDemandJob.m_ResourcePrefabs = m_ResourceSystem.GetPrefabs();
                updateIndustrialDemandJob.m_EmployableByEducation = m_CountHouseholdDataSystem.GetEmployables();
                updateIndustrialDemandJob.m_TaxRates = m_TaxSystem.GetTaxRates();
                updateIndustrialDemandJob.m_FreeWorkplaces = m_CountWorkplacesSystem.GetFreeWorkplaces();
                updateIndustrialDemandJob.m_City = m_CitySystem.City;
                updateIndustrialDemandJob.m_HeatingNeed = BuildingUpkeepSystem.GetHeatingMultiplier(m_ClimateSystem.temperature);
                updateIndustrialDemandJob.m_BaseConsumptionSum = m_ResourceSystem.BaseConsumptionSum;
                updateIndustrialDemandJob.m_IndustrialCompanyDemand = m_IndustrialCompanyDemand;
                updateIndustrialDemandJob.m_IndustrialBuildingDemand = m_IndustrialBuildingDemand;
                updateIndustrialDemandJob.m_StorageCompanyDemand = m_StorageCompanyDemand;
                updateIndustrialDemandJob.m_StorageBuildingDemand = m_StorageBuildingDemand;
                updateIndustrialDemandJob.m_OfficeCompanyDemand = m_OfficeCompanyDemand;
                updateIndustrialDemandJob.m_OfficeBuildingDemand = m_OfficeBuildingDemand;
                updateIndustrialDemandJob.m_IndustrialBuildingDemands = m_IndustrialBuildingDemands;
                updateIndustrialDemandJob.m_IndustrialDemands = m_ResourceDemands;
                updateIndustrialDemandJob.m_IndustrialZoningDemands = m_IndustrialZoningDemands;
                updateIndustrialDemandJob.m_StorageBuildingDemands = m_StorageBuildingDemands;
                updateIndustrialDemandJob.m_StorageCompanyDemands = m_StorageCompanyDemands;
                updateIndustrialDemandJob.m_Companies = industrialCompanyDatas.m_ProductionCompanies;
                updateIndustrialDemandJob.m_Propertyless = industrialCompanyDatas.m_ProductionPropertyless;

                updateIndustrialDemandJob.m_FreeProperties = m_FreeProperties;
                updateIndustrialDemandJob.m_Productions = industrialCompanyDatas.m_Production;
                updateIndustrialDemandJob.m_TotalCurrentWorkers = industrialCompanyDatas.m_CurrentProductionWorkers;
                updateIndustrialDemandJob.m_TotalMaxWorkers = industrialCompanyDatas.m_MaxProductionWorkers;
                updateIndustrialDemandJob.m_Propertyless = industrialCompanyDatas.m_ProductionPropertyless;
                updateIndustrialDemandJob.m_CompanyResourceDemands = industrialCompanyDatas.m_Demand;
                updateIndustrialDemandJob.m_FreeProperties = m_FreeProperties;
                updateIndustrialDemandJob.m_Productions = industrialCompanyDatas.m_Production;
                updateIndustrialDemandJob.m_Storages = m_Storages;
                updateIndustrialDemandJob.m_FreeStorages = m_FreeStorages;
                updateIndustrialDemandJob.m_StorageCapacities = m_StorageCapacities;
                updateIndustrialDemandJob.m_DemandFactors = m_IndustrialDemandFactors;
                updateIndustrialDemandJob.m_OfficeDemandFactors = m_OfficeDemandFactors;
                updateIndustrialDemandJob.m_CachedDemands = m_CachedDemands;
                updateIndustrialDemandJob.m_Results = m_Results;
                updateIndustrialDemandJob.m_ExcludedResources = m_ExcludedResources;
                updateIndustrialDemandJob.m_PopulationDemand = m_PopulationDemand;
                updateIndustrialDemandJob.m_CityServicesDemand = m_CityServicesDemand;
                updateIndustrialDemandJob.m_SpawnablesDemand = m_SpawnablesDemand;
                updateIndustrialDemandJob.m_IndustrialDemand = m_IndustrialDemand;
                updateIndustrialDemandJob.m_ResourceDemands = m_ResourceDemands;
                UpdateIndustrialDemandJob jobData = updateIndustrialDemandJob;
                base.Dependency = IJobExtensions.Schedule(jobData, JobUtils.CombineDependencies(base.Dependency, m_ReadDependencies, outJobHandle, deps, outJobHandle2, outJobHandle3, outJobHandle4, outJobHandle5));
                // since this is a copy of an actual simulation system but for UI purposes, then noone will read from us or wait for us
                base.Dependency.Complete();
                m_InputDemand = industrialCompanyDatas.m_Demand; // InfoLoom
                                                                 //m_WriteDependencies = base.Dependency;
                                                                 //m_CountCompanyDataSystem.AddReader(base.Dependency);
                                                                 //m_ResourceSystem.AddPrefabsReader(base.Dependency);
                                                                 //m_CountEmploymentSystem.AddReader(base.Dependency);
                                                                 //m_CountFreeWorkplacesSystem.AddReader(base.Dependency);
                                                                 //m_TaxSystem.AddReader(base.Dependency);

                
                
            }
        }

        private void ResetResults()
        {
            m_ExcludedResources.value = Resource.NoResource;
            m_Results.Fill<int>(0);
        }

        
    }
}