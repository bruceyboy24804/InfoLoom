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
using Game.Objects;
using Game.Prefabs;
using Game.Reflection;
using Game.Simulation;
using Game.Tools;
using Game.Zones;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using static InfoLoomTwo.Systems.IndustrialUISystem;

namespace InfoLoomTwo.Systems;

[CompilerGenerated]
public partial class CustomIndustrialDemandSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
    [BurstCompile]
    private struct UpdateIndustrialDemandJob : IJob
    {
        [DeallocateOnJobCompletion]
        [ReadOnly]
        public NativeArray<ZoneData> m_UnlockedZoneDatas;

        [ReadOnly]
        public NativeList<ArchetypeChunk> m_FreePropertyChunks;

        [ReadOnly]
        public NativeList<ArchetypeChunk> m_StorageCompanyChunks;

        [ReadOnly]
        public NativeList<ArchetypeChunk> m_CityServiceChunks;

        [ReadOnly]
        public EntityTypeHandle m_EntityType;

        [ReadOnly]
        public ComponentTypeHandle<PrefabRef> m_PrefabType;

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

        [ReadOnly]
        public float TaxRateEffect2;

        public NativeValue<int> m_IndustrialCompanyDemand;

        public NativeValue<int> m_IndustrialBuildingDemand;

        public NativeValue<int> m_StorageCompanyDemand;

        public NativeValue<int> m_StorageBuildingDemand;

        public NativeValue<int> m_OfficeCompanyDemand;

        public NativeValue<int> m_OfficeBuildingDemand;

        public NativeArray<int> m_IndustrialDemandFactors;

        public NativeArray<int> m_OfficeDemandFactors;

        public NativeArray<int> m_IndustrialCompanyDemands;

        public NativeArray<int> m_IndustrialBuildingDemands;

        public NativeArray<int> m_StorageBuildingDemands;

        public NativeArray<int> m_StorageCompanyDemands;

        [ReadOnly]
        public NativeArray<int> m_Productions;

        [ReadOnly]
        public NativeArray<int> m_CompanyResourceDemands;

        public NativeArray<int> m_FreeProperties;

        [ReadOnly]
        public NativeArray<int> m_Propertyless;

        public NativeArray<int> m_FreeStorages;

        public NativeArray<int> m_Storages;

        public NativeArray<int> m_StorageCapacities;

        public NativeArray<int> m_ResourceDemands;

        public NativeArray<DemandData> m_DemandData; // Added for UI data

        public NativeArray<int> m_CurrentProductionWorkers;

        public NativeArray<int> m_MaxProductionWorkers;

        

       

        public NativeArray<int> m_ProductionCompanies;

        public NativeArray<int> m_ProductionPropertyless;

        

       

        public void Execute()
        {
            bool flag = false;
            for (int i = 0; i < m_UnlockedZoneDatas.Length; i++)
            {
                if (m_UnlockedZoneDatas[i].m_AreaType == AreaType.Industrial)
                {
                    flag = true;
                    break;
                }
            }
            DynamicBuffer<CityModifier> modifiers = m_CityModifiers[m_City];
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
            for (int j = 0; j < m_IndustrialDemandFactors.Length; j++)
            {
                m_IndustrialDemandFactors[j] = 0;
            }
            for (int k = 0; k < m_OfficeDemandFactors.Length; k++)
            {
                m_OfficeDemandFactors[k] = 0;
            }
            for (int l = 0; l < m_CityServiceChunks.Length; l++)
            {
                ArchetypeChunk archetypeChunk = m_CityServiceChunks[l];
                if (!archetypeChunk.Has(ref m_ServiceUpkeepType))
                {
                    continue;
                }
                NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
                NativeArray<PrefabRef> nativeArray2 = archetypeChunk.GetNativeArray(ref m_PrefabType);
                for (int m = 0; m < nativeArray2.Length; m++)
                {
                    Entity prefab = nativeArray2[m].m_Prefab;
                    Entity entity = nativeArray[m];
                    if (m_ServiceUpkeeps.HasBuffer(prefab))
                    {
                        DynamicBuffer<ServiceUpkeepData> dynamicBuffer = m_ServiceUpkeeps[prefab];
                        for (int n = 0; n < dynamicBuffer.Length; n++)
                        {
                            ServiceUpkeepData serviceUpkeepData = dynamicBuffer[n];
                            if (serviceUpkeepData.m_Upkeep.m_Resource != Resource.Money)
                            {
                                int amount = serviceUpkeepData.m_Upkeep.m_Amount;
                                m_ResourceDemands[EconomyUtils.GetResourceIndex(serviceUpkeepData.m_Upkeep.m_Resource)] += amount;
                            }
                        }
                    }
                    if (!m_InstalledUpgrades.HasBuffer(entity))
                    {
                        continue;
                    }
                    DynamicBuffer<InstalledUpgrade> dynamicBuffer2 = m_InstalledUpgrades[entity];
                    for (int num = 0; num < dynamicBuffer2.Length; num++)
                    {
                        Entity upgrade = dynamicBuffer2[num].m_Upgrade;
                        if (BuildingUtils.CheckOption(dynamicBuffer2[num], BuildingOption.Inactive) || !m_Prefabs.HasComponent(upgrade))
                        {
                            continue;
                        }
                        Entity prefab2 = m_Prefabs[upgrade].m_Prefab;
                        if (m_Upkeeps.HasBuffer(prefab2))
                        {
                            DynamicBuffer<ServiceUpkeepData> dynamicBuffer3 = m_Upkeeps[prefab2];
                            for (int num2 = 0; num2 < dynamicBuffer3.Length; num2++)
                            {
                                ServiceUpkeepData serviceUpkeepData2 = dynamicBuffer3[num2];
                                m_ResourceDemands[EconomyUtils.GetResourceIndex(serviceUpkeepData2.m_Upkeep.m_Resource)] += serviceUpkeepData2.m_Upkeep.m_Amount;
                            }
                        }
                    }
                }
            }
            int num3 = 0;
            int num4 = 0;
            for (int num5 = 0; num5 < m_Productions.Length; num5++)
            {
                Resource resource = EconomyUtils.GetResource(num5);
                ResourceData resourceData2 = m_ResourceDatas[m_ResourcePrefabs[resource]];
                if (resourceData2.m_IsProduceable)
                {
                    if (resourceData2.m_Weight > 0f)
                    {
                        num3 += m_Productions[num5];
                    }
                    else
                    {
                        num4 += m_Productions[num5];
                    }
                }
            }
            int num6 = num4 + num3;
            m_ResourceDemands[EconomyUtils.GetResourceIndex(Resource.Software)] += num6 / m_EconomyParameters.m_PerOfficeResourceNeededForIndustrial;
            m_ResourceDemands[EconomyUtils.GetResourceIndex(Resource.Financial)] += num6 / m_EconomyParameters.m_PerOfficeResourceNeededForIndustrial;
            m_ResourceDemands[EconomyUtils.GetResourceIndex(Resource.Telecom)] += num6 / m_EconomyParameters.m_PerOfficeResourceNeededForIndustrial;
            for (int num7 = 0; num7 < m_StorageCompanyChunks.Length; num7++)
            {
                ArchetypeChunk archetypeChunk2 = m_StorageCompanyChunks[num7];
                NativeArray<Entity> nativeArray3 = archetypeChunk2.GetNativeArray(m_EntityType);
                NativeArray<PrefabRef> nativeArray4 = archetypeChunk2.GetNativeArray(ref m_PrefabType);
                for (int num8 = 0; num8 < nativeArray3.Length; num8++)
                {
                    Entity entity2 = nativeArray3[num8];
                    Entity prefab3 = nativeArray4[num8].m_Prefab;
                    if (m_IndustrialProcessDatas.HasComponent(prefab3))
                    {
                        int resourceIndex2 = EconomyUtils.GetResourceIndex(m_IndustrialProcessDatas[prefab3].m_Output.m_Resource);
                        m_Storages[resourceIndex2]++;
                        StorageLimitData storageLimitData = m_StorageLimitDatas[prefab3];
                        if (!m_PropertyRenters.HasComponent(entity2) || !m_Prefabs.HasComponent(m_PropertyRenters[entity2].m_Property))
                        {
                            m_FreeStorages[resourceIndex2]--;
                            m_StorageCapacities[resourceIndex2] += kStorageCompanyEstimateLimit;
                        }
                        else
                        {
                            Entity property = m_PropertyRenters[entity2].m_Property;
                            Entity prefab4 = m_Prefabs[property].m_Prefab;
                            m_StorageCapacities[resourceIndex2] += storageLimitData.GetAdjustedLimit(m_SpawnableBuildingDatas[prefab4], m_BuildingDatas[prefab4]);
                        }
                    }
                }
            }
            for (int num9 = 0; num9 < m_FreePropertyChunks.Length; num9++)
            {
                ArchetypeChunk archetypeChunk3 = m_FreePropertyChunks[num9];
                NativeArray<Entity> nativeArray5 = archetypeChunk3.GetNativeArray(m_EntityType);
                NativeArray<PrefabRef> nativeArray6 = archetypeChunk3.GetNativeArray(ref m_PrefabType);
                for (int num10 = 0; num10 < nativeArray6.Length; num10++)
                {
                    Entity prefab5 = nativeArray6[num10].m_Prefab;
                    if (!m_BuildingPropertyDatas.HasComponent(prefab5))
                    {
                        continue;
                    }
                    BuildingPropertyData buildingPropertyData = m_BuildingPropertyDatas[prefab5];
                    if (m_Attached.TryGetComponent(nativeArray5[num10], out var componentData) && m_Prefabs.TryGetComponent(componentData.m_Parent, out var componentData2) && m_BuildingPropertyDatas.TryGetComponent(componentData2.m_Prefab, out var componentData3))
                    {
                        buildingPropertyData.m_AllowedManufactured &= componentData3.m_AllowedManufactured;
                    }
                    ResourceIterator iterator2 = ResourceIterator.GetIterator();
                    while (iterator2.Next())
                    {
                        int resourceIndex3 = EconomyUtils.GetResourceIndex(iterator2.resource);
                        if ((buildingPropertyData.m_AllowedManufactured & iterator2.resource) != Resource.NoResource)
                        {
                            m_FreeProperties[resourceIndex3]++;
                        }
                        if ((buildingPropertyData.m_AllowedStored & iterator2.resource) != Resource.NoResource)
                        {
                            m_FreeStorages[resourceIndex3]++;
                        }
                    }
                }
            }
            _ = m_IndustrialBuildingDemand.value;
            bool flag2 = m_OfficeBuildingDemand.value > 0;
            _ = m_StorageBuildingDemand.value;
            m_IndustrialCompanyDemand.value = 0;
            m_IndustrialBuildingDemand.value = 0;
            m_StorageCompanyDemand.value = 0;
            m_StorageBuildingDemand.value = 0;
            m_OfficeCompanyDemand.value = 0;
            m_OfficeBuildingDemand.value = 0;
            int num11 = 0;
            int num12 = 0;
            iterator = ResourceIterator.GetIterator();
            while (iterator.Next())
            {
                int resourceIndex4 = EconomyUtils.GetResourceIndex(iterator.resource);
                if (!m_ResourceDatas.HasComponent(m_ResourcePrefabs[iterator.resource]))
                {
                    continue;
                }
                ResourceData resourceData3 = m_ResourceDatas[m_ResourcePrefabs[iterator.resource]];
                bool isProduceable = resourceData3.m_IsProduceable;
                bool isMaterial = resourceData3.m_IsMaterial;
                bool isTradable = resourceData3.m_IsTradable;
                bool flag3 = resourceData3.m_Weight == 0f;
                if (isTradable && !flag3)
                {
                    int num13 = m_ResourceDemands[resourceIndex4];
                    m_StorageCompanyDemands[resourceIndex4] = 0;
                    m_StorageBuildingDemands[resourceIndex4] = 0;
                    if (num13 > kStorageProductionDemand && m_StorageCapacities[resourceIndex4] < num13)
                    {
                        m_StorageCompanyDemands[resourceIndex4] = 1;
                    }
                    if (m_FreeStorages[resourceIndex4] < 0)
                    {
                        m_StorageBuildingDemands[resourceIndex4] = 1;
                    }
                    m_StorageCompanyDemand.value += m_StorageCompanyDemands[resourceIndex4];
                    m_StorageBuildingDemand.value += m_StorageBuildingDemands[resourceIndex4];
                    m_IndustrialDemandFactors[17] += math.max(0, m_StorageBuildingDemands[resourceIndex4]);
                }
                if (!isProduceable)
                {
                    continue;
                }
                float value = (isMaterial ? m_DemandParameters.m_ExtractorBaseDemand : m_DemandParameters.m_IndustrialBaseDemand);
                float num14 = (1f + (float)m_ResourceDemands[resourceIndex4] - (float)m_Productions[resourceIndex4]) / ((float)m_ResourceDemands[resourceIndex4] + 1f);
                if (iterator.resource == Resource.Electronics)
                {
                    CityUtils.ApplyModifier(ref value, modifiers, CityModifierType.IndustrialElectronicsDemand);
                }
                else if (iterator.resource == Resource.Software)
                {
                    CityUtils.ApplyModifier(ref value, modifiers, CityModifierType.OfficeSoftwareDemand);
                }
                int num15 = (flag3 ? TaxSystem.GetOfficeTaxRate(iterator.resource, m_TaxRates) : TaxSystem.GetIndustrialTaxRate(iterator.resource, m_TaxRates));
                float num16 = m_DemandParameters.m_TaxEffect * -5f * ((float)num15 - 10f);
                int num17 = 0;
                int num18 = 0;
                float num19 = m_DemandParameters.m_NeutralUnemployment / 100f;
                for (int num20 = 0; num20 < 5; num20++)
                {
                    if (num20 < 2)
                    {
                        num18 += (int)((float)m_EmployableByEducation[num20] * (1f - num19)) - m_FreeWorkplaces[num20];
                    }
                    else
                    {
                        num17 += (int)((float)m_EmployableByEducation[num20] * (1f - num19)) - m_FreeWorkplaces[num20];
                    }
                }
                num17 = math.clamp(num17, -10, 10);
                num18 = math.clamp(num18, -10, 15);
                float num21 = 50f * math.max(0f, value * num14);

                if (flag3)
                {
                    m_IndustrialCompanyDemands[resourceIndex4] = Mathf.RoundToInt(num21 + num16 + (float)num17);
                    m_IndustrialCompanyDemands[resourceIndex4] = math.min(100, math.max(0, m_IndustrialCompanyDemands[resourceIndex4]));
                    m_OfficeCompanyDemand.value += Mathf.RoundToInt(m_IndustrialCompanyDemands[resourceIndex4]);
                    num11++;
                }
                else
                {
                    m_IndustrialCompanyDemands[resourceIndex4] = Mathf.RoundToInt(num21 + num16 + (float)num17 + (float)num18);
                    m_IndustrialCompanyDemands[resourceIndex4] = math.min(100, math.max(0, m_IndustrialCompanyDemands[resourceIndex4]));
                    m_IndustrialCompanyDemand.value += Mathf.RoundToInt(m_IndustrialCompanyDemands[resourceIndex4]);
                    if (!isMaterial)
                    {
                        num12++;
                    }
                }

                // Update the uiData for the current resource
                DemandData uiData = m_DemandData[resourceIndex4];
                uiData.Resource = iterator.resource;
                uiData.Demand = m_ResourceDemands[resourceIndex4];
                uiData.Building = m_IndustrialCompanyDemands[resourceIndex4];
                
                uiData.Free = m_FreeProperties[resourceIndex4];
                uiData.Companies = m_ProductionCompanies[resourceIndex4];
                uiData.Workers = m_CurrentProductionWorkers[resourceIndex4];
               
                uiData.CapFactor = Mathf.RoundToInt(100f * num14);
                uiData.CapPercent = 100 * m_Productions[resourceIndex4] / math.max(100, m_CompanyResourceDemands[resourceIndex4]);
                uiData.CapPerCompany = (m_ProductionCompanies[resourceIndex4] == 0 ? 0 : m_Productions[resourceIndex4] / m_ProductionCompanies[resourceIndex4]);
                uiData.WrkFactor = Mathf.RoundToInt(100f * num4);
                uiData.WrkPercent = 100 * (m_CurrentProductionWorkers[resourceIndex4] + 1) / (m_MaxProductionWorkers[resourceIndex4] + 1);
                uiData.TaxFactor = Mathf.RoundToInt(100f * num16);
               
               
                m_DemandData[resourceIndex4] = uiData;








               
                if (m_ResourceDemands[resourceIndex4] > 0)
                {
                    if (!isMaterial && m_IndustrialCompanyDemands[resourceIndex4] > 0)
                    {
                        m_IndustrialBuildingDemands[resourceIndex4] = ((m_FreeProperties[resourceIndex4] - m_Propertyless[resourceIndex4] <= 0) ? 50 : 0);
                    }
                    else if (m_IndustrialCompanyDemands[resourceIndex4] > 0)
                    {
                        m_IndustrialBuildingDemands[resourceIndex4] = 1;
                    }
                    else
                    {
                        m_IndustrialBuildingDemands[resourceIndex4] = 0;
                    }
                    if (m_IndustrialBuildingDemands[resourceIndex4] > 0)
                    {
                        if (flag3)
                        {
                            m_OfficeBuildingDemand.value += ((m_IndustrialBuildingDemands[resourceIndex4] > 0) ? m_IndustrialCompanyDemands[resourceIndex4] : 0);
                        }
                        else if (!isMaterial)
                        {
                            m_IndustrialBuildingDemand.value += ((m_IndustrialBuildingDemands[resourceIndex4] > 0) ? m_IndustrialCompanyDemands[resourceIndex4] : 0);
                        }
                    }
                }
                if (isMaterial)
                {
                    continue;
                }
                if (flag3)
                {
                    if (!flag2 || (m_IndustrialBuildingDemands[resourceIndex4] > 0 && m_IndustrialCompanyDemands[resourceIndex4] > 0))
                    {
                        m_OfficeDemandFactors[2] = num17;
                        m_OfficeDemandFactors[4] += (int)num21;
                        m_OfficeDemandFactors[11] += (int)num16;
                        m_OfficeDemandFactors[13] += m_IndustrialBuildingDemands[resourceIndex4];
                    }
                }
                else
                {
                    m_IndustrialDemandFactors[2] = num17;
                    m_IndustrialDemandFactors[1] = num18;
                    m_IndustrialDemandFactors[4] += (int)num21;
                    m_IndustrialDemandFactors[11] += (int)num16;
                    m_IndustrialDemandFactors[13] += m_IndustrialBuildingDemands[resourceIndex4];
                }
            }
            m_StorageBuildingDemand.value = Mathf.CeilToInt(math.pow(20f * (float)m_StorageBuildingDemand.value, 0.75f));
            m_IndustrialBuildingDemand.value = (flag ? (2 * m_IndustrialBuildingDemand.value / num12) : 0);
            m_OfficeCompanyDemand.value *= 2 * m_OfficeCompanyDemand.value / num11;
            m_IndustrialBuildingDemand.value = math.clamp(m_IndustrialBuildingDemand.value, 0, 100);
            m_OfficeBuildingDemand.value = math.clamp(m_OfficeBuildingDemand.value, 0, 100);
        }
    }

    private struct TypeHandle
    {
        [ReadOnly]
        public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<CityServiceUpkeep> __Game_City_CityServiceUpkeep_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentLookup<StorageLimitData> __Game_Companies_StorageLimitData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Attached> __Game_Objects_Attached_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

        [ReadOnly]
        public BufferLookup<ServiceUpkeepData> __Game_Prefabs_ServiceUpkeepData_RO_BufferLookup;

        [ReadOnly]
        public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

        [ReadOnly]
        public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref SystemState state)
        {
            __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
            __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
            __Game_City_CityServiceUpkeep_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CityServiceUpkeep>(isReadOnly: true);
            __Game_Companies_StorageLimitData_RO_ComponentLookup = state.GetComponentLookup<StorageLimitData>(isReadOnly: true);
            __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
            __Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
            __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
            __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup = state.GetComponentLookup<IndustrialProcessData>(isReadOnly: true);
            __Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
            __Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
            __Game_Objects_Attached_RO_ComponentLookup = state.GetComponentLookup<Attached>(isReadOnly: true);
            __Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
            __Game_Prefabs_ServiceUpkeepData_RO_BufferLookup = state.GetBufferLookup<ServiceUpkeepData>(isReadOnly: true);
            __Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
            __Game_Buildings_InstalledUpgrade_RO_BufferLookup = state.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
        }
    }

    private static readonly int kStorageProductionDemand = 2000;

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

    private EntityQuery m_UnlockedZoneDataQuery;

    private NativeValue<int> m_IndustrialCompanyDemand;

    private NativeValue<int> m_IndustrialBuildingDemand;

    private NativeValue<int> m_StorageCompanyDemand;

    private NativeValue<int> m_StorageBuildingDemand;

    private NativeValue<int> m_OfficeCompanyDemand;

    private NativeValue<int> m_OfficeBuildingDemand;

    public static NativeArray<DemandData> m_DemandData;

    [ResourceArray]
    [DebugWatchValue]
    private NativeArray<int> m_ResourceDemands;

    [EnumArray(typeof(DemandFactor))]
    [DebugWatchValue]
    private NativeArray<int> m_IndustrialDemandFactors;

    [EnumArray(typeof(DemandFactor))]
    [DebugWatchValue]
    private NativeArray<int> m_OfficeDemandFactors;

    [ResourceArray]
    [DebugWatchValue]
    private NativeArray<int> m_IndustrialCompanyDemands;

    [ResourceArray]
    [DebugWatchValue]
    private NativeArray<int> m_IndustrialZoningDemands;

    [ResourceArray]
    [DebugWatchValue]
    private NativeArray<int> m_IndustrialBuildingDemands;

    [ResourceArray]
    [DebugWatchValue]
    private NativeArray<int> m_StorageBuildingDemands;

    [ResourceArray]
    [DebugWatchValue]
    private NativeArray<int> m_StorageCompanyDemands;

    [ResourceArray]
    [DebugWatchValue]
    private NativeArray<int> m_FreeProperties;

    [ResourceArray]
    [DebugWatchValue]
    private NativeArray<int> m_FreeStorages;

    [ResourceArray]
    [DebugWatchValue]
    private NativeArray<int> m_Storages;

    [ResourceArray]
    [DebugWatchValue]
    private NativeArray<int> m_StorageCapacities;

    [DebugWatchDeps]
    private JobHandle m_WriteDependencies;

    private JobHandle m_ReadDependencies;

    private int m_LastIndustrialCompanyDemand;

    private int m_LastIndustrialBuildingDemand;

    private int m_LastStorageCompanyDemand;

    private int m_LastStorageBuildingDemand;

    private int m_LastOfficeCompanyDemand;

    private int m_LastOfficeBuildingDemand;

    private TypeHandle __TypeHandle;

    [DebugWatchValue(color = "#f7dc6f")]
    public int industrialCompanyDemand => m_LastIndustrialCompanyDemand;

    [DebugWatchValue(color = "#b7950b")]
    public int industrialBuildingDemand => m_LastIndustrialBuildingDemand;

    [DebugWatchValue(color = "#cccccc")]
    public int storageCompanyDemand => m_LastStorageCompanyDemand;

    [DebugWatchValue(color = "#999999")]
    public int storageBuildingDemand => m_LastStorageBuildingDemand;

    [DebugWatchValue(color = "#af7ac5")]
    public int officeCompanyDemand => m_LastOfficeCompanyDemand;

    [DebugWatchValue(color = "#6c3483")]
    public int officeBuildingDemand => m_LastOfficeBuildingDemand;

    public override int GetUpdateInterval(SystemUpdatePhase phase)
    {
        return 16;
    }

    public override int GetUpdateOffset(SystemUpdatePhase phase)
    {
        return 7;
    }

    public NativeArray<int> GetConsumption(out JobHandle deps)
    {
        deps = m_WriteDependencies;
        return m_ResourceDemands;
    }

    public NativeArray<int> GetIndustrialDemandFactors(out JobHandle deps)
    {
        deps = m_WriteDependencies;
        return m_IndustrialDemandFactors;
    }

    public NativeArray<int> GetOfficeDemandFactors(out JobHandle deps)
    {
        deps = m_WriteDependencies;
        return m_OfficeDemandFactors;
    }

    public NativeArray<int> GetResourceDemands(out JobHandle deps)
    {
        deps = m_WriteDependencies;
        return m_IndustrialCompanyDemands;
    }

    public NativeArray<int> GetBuildingDemands(out JobHandle deps)
    {
        deps = m_WriteDependencies;
        return m_IndustrialBuildingDemands;
    }

    public NativeArray<int> GetStorageCompanyDemands(out JobHandle deps)
    {
        deps = m_WriteDependencies;
        return m_StorageCompanyDemands;
    }

    public NativeArray<int> GetStorageBuildingDemands(out JobHandle deps)
    {
        deps = m_WriteDependencies;
        return m_StorageBuildingDemands;
    }

    public NativeArray<int> GetIndustrialResourceDemands(out JobHandle deps)
    {
        deps = m_WriteDependencies;
        return m_ResourceDemands;
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
        m_UnlockedZoneDataQuery = GetEntityQuery(ComponentType.ReadOnly<ZoneData>(), ComponentType.Exclude<Locked>());
        m_IndustrialCompanyDemand = new NativeValue<int>(Allocator.Persistent);
        m_IndustrialBuildingDemand = new NativeValue<int>(Allocator.Persistent);
        m_StorageCompanyDemand = new NativeValue<int>(Allocator.Persistent);
        m_StorageBuildingDemand = new NativeValue<int>(Allocator.Persistent);
        m_OfficeCompanyDemand = new NativeValue<int>(Allocator.Persistent);
        m_OfficeBuildingDemand = new NativeValue<int>(Allocator.Persistent);
        m_IndustrialDemandFactors = new NativeArray<int>(18, Allocator.Persistent);
        m_OfficeDemandFactors = new NativeArray<int>(18, Allocator.Persistent);
        int resourceCount = EconomyUtils.ResourceCount;
        m_IndustrialCompanyDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
        m_IndustrialZoningDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
        m_IndustrialBuildingDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
        m_ResourceDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
        m_StorageBuildingDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
        m_StorageCompanyDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
        m_FreeProperties = new NativeArray<int>(resourceCount, Allocator.Persistent);
        m_FreeStorages = new NativeArray<int>(resourceCount, Allocator.Persistent);
        m_Storages = new NativeArray<int>(resourceCount, Allocator.Persistent);
        m_StorageCapacities = new NativeArray<int>(resourceCount, Allocator.Persistent);
        m_DemandData = new NativeArray<DemandData>(resourceCount, Allocator.Persistent);
        RequireForUpdate(m_EconomyParameterQuery);
        RequireForUpdate(m_DemandParameterQuery);
        RequireForUpdate(m_ProcessDataQuery);
    }

    [Preserve]
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
        m_DemandData.Dispose();
        base.OnDestroy();
    }

    public void SetDefaults(Context context)
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
    }

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        writer.Write(m_IndustrialCompanyDemand.value);
        writer.Write(m_IndustrialBuildingDemand.value);
        writer.Write(m_StorageCompanyDemand.value);
        writer.Write(m_StorageBuildingDemand.value);
        writer.Write(m_OfficeCompanyDemand.value);
        writer.Write(m_OfficeBuildingDemand.value);
        writer.Write(m_IndustrialDemandFactors.Length);
        writer.Write(m_IndustrialDemandFactors);
        writer.Write(m_OfficeDemandFactors);
        writer.Write(m_IndustrialCompanyDemands);
        writer.Write(m_IndustrialZoningDemands);
        writer.Write(m_IndustrialBuildingDemands);
        writer.Write(m_StorageBuildingDemands);
        writer.Write(m_StorageCompanyDemands);
        writer.Write(m_FreeProperties);
        writer.Write(m_Storages);
        writer.Write(m_FreeStorages);
        writer.Write(m_LastIndustrialCompanyDemand);
        writer.Write(m_LastIndustrialBuildingDemand);
        writer.Write(m_LastStorageCompanyDemand);
        writer.Write(m_LastStorageBuildingDemand);
        writer.Write(m_LastOfficeCompanyDemand);
        writer.Write(m_LastOfficeBuildingDemand);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        reader.Read(out int value);
        m_IndustrialCompanyDemand.value = value;
        reader.Read(out int value2);
        m_IndustrialBuildingDemand.value = value2;
        reader.Read(out int value3);
        m_StorageCompanyDemand.value = value3;
        reader.Read(out int value4);
        m_StorageBuildingDemand.value = value4;
        reader.Read(out int value5);
        m_OfficeCompanyDemand.value = value5;
        reader.Read(out int value6);
        m_OfficeBuildingDemand.value = value6;
        if (reader.context.version < Version.demandFactorCountSerialization)
        {
            NativeArray<int> nativeArray = new NativeArray<int>(13, Allocator.Temp);
            reader.Read(nativeArray);
            CollectionUtils.CopySafe(nativeArray, m_IndustrialDemandFactors);
            reader.Read(nativeArray);
            CollectionUtils.CopySafe(nativeArray, m_OfficeDemandFactors);
            nativeArray.Dispose();
        }
        else
        {
            reader.Read(out int value7);
            if (value7 == m_IndustrialDemandFactors.Length)
            {
                reader.Read(m_IndustrialDemandFactors);
                reader.Read(m_OfficeDemandFactors);
            }
            else
            {
                NativeArray<int> nativeArray2 = new NativeArray<int>(value7, Allocator.Temp);
                reader.Read(nativeArray2);
                CollectionUtils.CopySafe(nativeArray2, m_IndustrialDemandFactors);
                reader.Read(nativeArray2);
                CollectionUtils.CopySafe(nativeArray2, m_OfficeDemandFactors);
                nativeArray2.Dispose();
            }
        }
        reader.Read(m_IndustrialCompanyDemands);
        reader.Read(m_IndustrialZoningDemands);
        reader.Read(m_IndustrialBuildingDemands);
        reader.Read(m_StorageBuildingDemands);
        reader.Read(m_StorageCompanyDemands);
        if (reader.context.version <= Version.companyDemandOptimization)
        {
            NativeArray<int> value8 = new NativeArray<int>(EconomyUtils.ResourceCount, Allocator.Temp);
            reader.Read(value8);
            reader.Read(value8);
            if (reader.context.version <= Version.demandFactorCountSerialization)
            {
                reader.Read(value8);
                reader.Read(value8);
            }
            reader.Read(value8);
            reader.Read(value8);
        }
        reader.Read(m_FreeProperties);
        reader.Read(m_Storages);
        reader.Read(m_FreeStorages);
        reader.Read(out m_LastIndustrialCompanyDemand);
        reader.Read(out m_LastIndustrialBuildingDemand);
        reader.Read(out m_LastStorageCompanyDemand);
        reader.Read(out m_LastStorageBuildingDemand);
        reader.Read(out m_LastOfficeCompanyDemand);
        reader.Read(out m_LastOfficeBuildingDemand);
    }

    [Preserve]
    protected override void OnUpdate()
    {
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
            __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_City_CityModifier_RO_BufferLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_ServiceUpkeepData_RO_BufferLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Objects_Attached_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Companies_StorageLimitData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_City_CityServiceUpkeep_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            __TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);
            UpdateIndustrialDemandJob updateIndustrialDemandJob = default(UpdateIndustrialDemandJob);
            updateIndustrialDemandJob.m_FreePropertyChunks = m_FreeIndustrialQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out var outJobHandle);
            updateIndustrialDemandJob.m_StorageCompanyChunks = m_StorageCompanyQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out var outJobHandle2);
            updateIndustrialDemandJob.m_CityServiceChunks = m_CityServiceQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out var outJobHandle3);
            updateIndustrialDemandJob.m_UnlockedZoneDatas = m_UnlockedZoneDataQuery.ToComponentDataArray<ZoneData>(Allocator.TempJob);
            updateIndustrialDemandJob.m_EntityType = __TypeHandle.__Unity_Entities_Entity_TypeHandle;
            updateIndustrialDemandJob.m_PrefabType = __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
            updateIndustrialDemandJob.m_ServiceUpkeepType = __TypeHandle.__Game_City_CityServiceUpkeep_RO_ComponentTypeHandle;
            updateIndustrialDemandJob.m_StorageLimitDatas = __TypeHandle.__Game_Companies_StorageLimitData_RO_ComponentLookup;
            updateIndustrialDemandJob.m_SpawnableBuildingDatas = __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;
            updateIndustrialDemandJob.m_BuildingDatas = __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup;
            updateIndustrialDemandJob.m_BuildingPropertyDatas = __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;
            updateIndustrialDemandJob.m_IndustrialProcessDatas = __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;
            updateIndustrialDemandJob.m_Prefabs = __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup;
            updateIndustrialDemandJob.m_PropertyRenters = __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup;
            updateIndustrialDemandJob.m_Attached = __TypeHandle.__Game_Objects_Attached_RO_ComponentLookup;
            updateIndustrialDemandJob.m_ResourceDatas = __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup;
            updateIndustrialDemandJob.m_ServiceUpkeeps = __TypeHandle.__Game_Prefabs_ServiceUpkeepData_RO_BufferLookup;
            updateIndustrialDemandJob.m_CityModifiers = __TypeHandle.__Game_City_CityModifier_RO_BufferLookup;
            updateIndustrialDemandJob.m_InstalledUpgrades = __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup;
            updateIndustrialDemandJob.m_Upkeeps = __TypeHandle.__Game_Prefabs_ServiceUpkeepData_RO_BufferLookup;
            updateIndustrialDemandJob.m_DemandParameters = m_DemandParameterQuery.GetSingleton<DemandParameterData>();
            updateIndustrialDemandJob.m_EconomyParameters = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>();
            updateIndustrialDemandJob.m_ResourcePrefabs = m_ResourceSystem.GetPrefabs();
            updateIndustrialDemandJob.m_EmployableByEducation = m_CountHouseholdDataSystem.GetEmployables();
            updateIndustrialDemandJob.m_TaxRates = m_TaxSystem.GetTaxRates();
            updateIndustrialDemandJob.m_FreeWorkplaces = m_CountWorkplacesSystem.GetFreeWorkplaces();
            updateIndustrialDemandJob.m_City = m_CitySystem.City;
            updateIndustrialDemandJob.m_IndustrialCompanyDemand = m_IndustrialCompanyDemand;
            updateIndustrialDemandJob.m_IndustrialBuildingDemand = m_IndustrialBuildingDemand;
            updateIndustrialDemandJob.m_StorageCompanyDemand = m_StorageCompanyDemand;
            updateIndustrialDemandJob.m_StorageBuildingDemand = m_StorageBuildingDemand;
            updateIndustrialDemandJob.m_OfficeCompanyDemand = m_OfficeCompanyDemand;
            updateIndustrialDemandJob.m_OfficeBuildingDemand = m_OfficeBuildingDemand;
            updateIndustrialDemandJob.m_IndustrialCompanyDemands = m_IndustrialCompanyDemands;
            updateIndustrialDemandJob.m_IndustrialBuildingDemands = m_IndustrialBuildingDemands;
            updateIndustrialDemandJob.m_StorageBuildingDemands = m_StorageBuildingDemands;
            updateIndustrialDemandJob.m_StorageCompanyDemands = m_StorageCompanyDemands;
            updateIndustrialDemandJob.m_Propertyless = industrialCompanyDatas.m_ProductionPropertyless;
            updateIndustrialDemandJob.m_CompanyResourceDemands = industrialCompanyDatas.m_Demand;
            updateIndustrialDemandJob.m_FreeProperties = m_FreeProperties;
            updateIndustrialDemandJob.m_Productions = industrialCompanyDatas.m_Production;
            updateIndustrialDemandJob.m_CurrentProductionWorkers = industrialCompanyDatas.m_CurrentProductionWorkers;
            updateIndustrialDemandJob.m_MaxProductionWorkers = industrialCompanyDatas.m_MaxProductionWorkers;
            updateIndustrialDemandJob.m_ProductionCompanies = industrialCompanyDatas.m_ProductionCompanies;
            updateIndustrialDemandJob.m_DemandData = m_DemandData; // MODDED
            updateIndustrialDemandJob.m_Storages = m_Storages;
            updateIndustrialDemandJob.m_FreeStorages = m_FreeStorages;
            updateIndustrialDemandJob.m_StorageCapacities = m_StorageCapacities;
            updateIndustrialDemandJob.m_IndustrialDemandFactors = m_IndustrialDemandFactors;
            updateIndustrialDemandJob.m_OfficeDemandFactors = m_OfficeDemandFactors;
            updateIndustrialDemandJob.m_ResourceDemands = m_ResourceDemands;
            //updateIndustrialDemandJob.TaxRateEffect2 = Mod.setting.TaxRateEffect2;
            UpdateIndustrialDemandJob jobData = updateIndustrialDemandJob;
            base.Dependency = IJobExtensions.Schedule(jobData, JobUtils.CombineDependencies(base.Dependency, m_ReadDependencies, outJobHandle, deps, outJobHandle2, outJobHandle3));
            m_WriteDependencies = base.Dependency;
            m_CountCompanyDataSystem.AddReader(base.Dependency);
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
    public CustomIndustrialDemandSystem()
    {
    }
}
