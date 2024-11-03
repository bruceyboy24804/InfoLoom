using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
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
using InfoLoom;
using Game.Objects;

namespace InfoLoomTwo.Systems;

[CompilerGenerated]
public partial class IndustrialUISystem : UISystemBase
{
    public struct DemandData
    {
        public Resource Resource;
        public int Demand;          // company demand
        public int Building;        // building demand
        public int Free;           // free properties
        public int Companies;      // num of companies
        public int Workers;        // num of workers
        public int SvcFactor;      // service availability
        public int SvcPercent;
        public int CapFactor;      // sales capacity
        public int CapPercent;
        public int CapPerCompany;
        public int WrkFactor;      // employee ratio
        public int WrkPercent;
        public int EduFactor;      // educated employees
        public int TaxFactor;      // tax factor

        public DemandData(Resource resource) { Resource = resource; }
    }

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

        [ReadOnly]
        public BufferTypeHandle<Renter> m_RenterType;

        [ReadOnly]
        public ComponentLookup<IndustrialCompany> m_IndustrialCompanies;

        [ReadOnly]
        public ComponentLookup<Tourism> m_Tourisms;

        public NativeValue<int> m_CompanyDemand;
        public NativeValue<int> m_BuildingDemand;
        public NativeArray<int> m_DemandFactors;
        
        public NativeArray<int> m_BuildingDemands;

        [ReadOnly]
        public NativeArray<int> m_ResourceNeeds;
        [ReadOnly]
        public NativeArray<int> m_ProduceCapacity;
        [ReadOnly]
        public NativeArray<int> m_CurrentAvailables;
        [ReadOnly]
        public NativeArray<int> m_Companies;
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
        public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

        [ReadOnly]
        public BufferTypeHandle<Renter> __Game_Buildings_Renter_RO_BufferTypeHandle;

        [ReadOnly]
        public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<IndustrialCompany> __Game_Companies_IndustrialCompany_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Tourism> __Game_City_Tourism_RO_ComponentLookup;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref SystemState state)
        {
            __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
            __Game_Buildings_Renter_RO_BufferTypeHandle = state.GetBufferTypeHandle<Renter>(isReadOnly: true);
            __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
            __Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
            __Game_Companies_IndustrialCompany_RO_ComponentLookup = state.GetComponentLookup<IndustrialCompany>(isReadOnly: true);
            __Game_City_Tourism_RO_ComponentLookup = state.GetComponentLookup<Tourism>(isReadOnly: true);
        }
    }

    private const string kGroup = "realEco";

    // Systems
    private SimulationSystem m_SimulationSystem;
    private ResourceSystem m_ResourceSystem;
    private TaxSystem m_TaxSystem;
    private CountHouseholdDataSystem m_CountHouseholdDataSystem;
    private CountCompanyDataSystem m_CountCompanyDataSystem;
    private CountWorkplacesSystem m_CountWorkplacesSystem;
    private CitySystem m_CitySystem;

    // Queries
    private EntityQuery m_EconomyParameterQuery;
    private EntityQuery m_DemandParameterQuery;
    private EntityQuery m_FreeCommercialQuery;
    private EntityQuery m_UnlockedZoneDataQuery;

    // Type handles
    private TypeHandle __TypeHandle;

    // Demand calculation data
    private NativeValue<int> m_CompanyDemand;
    private NativeValue<int> m_BuildingDemand;
    private NativeArray<int> m_DemandFactors;
    private NativeArray<int> m_ResourceDemands;
    private NativeArray<int> m_BuildingDemands;
    private NativeArray<int> m_FreeProperties;
    private int m_LastCompanyDemand;
    private int m_LastBuildingDemand;
    private JobHandle m_WriteDependencies;
    private JobHandle m_ReadDependencies;

    // UI data
    private RawValueBinding m_uiResults;
    private NativeArray<DemandData> m_DemandData;

    public override GameMode gameMode => GameMode.Game;

    [Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();

        // Get required systems
        m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
        m_ResourceSystem = World.GetOrCreateSystemManaged<ResourceSystem>();
        m_TaxSystem = World.GetOrCreateSystemManaged<TaxSystem>();
        m_CountWorkplacesSystem = World.GetOrCreateSystemManaged<CountWorkplacesSystem>();
        m_CountHouseholdDataSystem = World.GetOrCreateSystemManaged<CountHouseholdDataSystem>();
        m_CountCompanyDataSystem = World.GetOrCreateSystemManaged<CountCompanyDataSystem>();
        m_CitySystem = World.GetOrCreateSystemManaged<CitySystem>();

        // Create queries
        m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
        m_DemandParameterQuery = GetEntityQuery(ComponentType.ReadOnly<DemandParameterData>());
        m_FreeCommercialQuery = GetEntityQuery(
            ComponentType.ReadOnly<PrefabRef>(),
            ComponentType.ReadOnly<Renter>(),
            ComponentType.ReadOnly<CommercialProperty>());
        m_UnlockedZoneDataQuery = GetEntityQuery(ComponentType.ReadOnly<ZoneData>());

        // Initialize arrays
        m_DemandData = new NativeArray<DemandData>(EconomyUtils.ResourceCount, Allocator.Persistent);
        m_CompanyDemand = new NativeValue<int>(Allocator.Persistent);
        m_BuildingDemand = new NativeValue<int>(Allocator.Persistent);
        m_DemandFactors = new NativeArray<int>(32, Allocator.Persistent);
        m_ResourceDemands = new NativeArray<int>(EconomyUtils.ResourceCount, Allocator.Persistent);
        m_BuildingDemands = new NativeArray<int>(EconomyUtils.ResourceCount, Allocator.Persistent);
        m_FreeProperties = new NativeArray<int>(EconomyUtils.ResourceCount, Allocator.Persistent);

        // UI binding
        AddBinding(m_uiResults = new RawValueBinding(kGroup, "commercialDemand", WriteResults));

        __TypeHandle.__AssignHandles(ref CheckedStateRef);

        Mod.log.Info("CommercialUISystem created.");
    }

    [Preserve]
    protected override void OnDestroy()
    {
        if (m_DemandData.IsCreated) m_DemandData.Dispose();
        if (m_CompanyDemand.IsCreated) m_CompanyDemand.Dispose();
        if (m_BuildingDemand.IsCreated) m_BuildingDemand.Dispose();
        if (m_DemandFactors.IsCreated) m_DemandFactors.Dispose();
        if (m_ResourceDemands.IsCreated) m_ResourceDemands.Dispose();
        if (m_BuildingDemands.IsCreated) m_BuildingDemands.Dispose();
        if (m_FreeProperties.IsCreated) m_FreeProperties.Dispose();

        base.OnDestroy();
    }

    [Preserve]
    protected override void OnUpdate()
    {
        if (m_SimulationSystem.frameIndex % 64 != 17)
            return;

        if (!m_DemandParameterQuery.IsEmptyIgnoreFilter && !m_EconomyParameterQuery.IsEmptyIgnoreFilter)
        {
            m_LastCompanyDemand = m_CompanyDemand.value;
            m_LastBuildingDemand = m_BuildingDemand.value;

            JobHandle deps;
            CountCompanyDataSystem.IndustrialCompanyDatas industrialCompanyDatas =
                m_CountCompanyDataSystem.GetIndustrialCompanyDatas(out deps);

            // Update type handles
            UpdateTypeHandles();

            // Setup and schedule job
            UpdateIndustrialDemandJob job = CreateDemandJob(industrialCompanyDatas, out JobHandle outJobHandle);

            // Schedule job with combined dependencies
            JobHandle combinedDeps = JobHandle.CombineDependencies(m_ReadDependencies, outJobHandle, deps);
            JobHandle dependency = job.Schedule(combinedDeps);

            m_WriteDependencies = dependency;
            m_CountHouseholdDataSystem.AddHouseholdResourceNeedReader(dependency);
            m_ResourceSystem.AddPrefabsReader(dependency);
            m_TaxSystem.AddReader(dependency);
        }

        m_uiResults.Update();
    }

    private void WriteResults(IJsonWriter writer)
    {
        writer.ArrayBegin(m_DemandData.Length);
        for (int i = 0; i < m_DemandData.Length; i++)
            WriteData(writer, m_DemandData[i]);
        writer.ArrayEnd();
    }

    private static void WriteData(IJsonWriter writer, DemandData data)
    {
        writer.TypeBegin("DemandData");
        writer.PropertyName("resource");
        writer.Write(data.Resource.ToString());
        writer.PropertyName("demand");
        writer.Write(data.Demand);
        writer.PropertyName("building");
        writer.Write(data.Building);
        writer.PropertyName("free");
        writer.Write(data.Free);
        writer.PropertyName("companies");
        writer.Write(data.Companies);
        writer.PropertyName("workers");
        writer.Write(data.Workers);
        writer.PropertyName("svcfactor");
        writer.Write(data.SvcFactor);
        writer.PropertyName("svcpercent");
        writer.Write(data.SvcPercent);
        writer.PropertyName("capfactor");
        writer.Write(data.CapFactor);
        writer.PropertyName("cappercent");
        writer.Write(data.CapPercent);
        writer.PropertyName("cappercompany");
        writer.Write(data.CapPerCompany);
        writer.PropertyName("wrkfactor");
        writer.Write(data.WrkFactor);
        writer.PropertyName("wrkpercent");
        writer.Write(data.WrkPercent);
        writer.PropertyName("edufactor");
        writer.Write(data.EduFactor);
        writer.PropertyName("taxfactor");
        writer.Write(data.TaxFactor);
        writer.TypeEnd();
    }

    private void UpdateTypeHandles()
    {
        __TypeHandle.__Game_City_Tourism_RO_ComponentLookup.Update(ref CheckedStateRef);
        __TypeHandle.__Game_Companies_IndustrialCompany_RO_ComponentLookup.Update(ref CheckedStateRef);
        __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup.Update(ref CheckedStateRef);
        __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup.Update(ref CheckedStateRef);
        __TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle.Update(ref CheckedStateRef);
        __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle.Update(ref CheckedStateRef);
    }

    private UpdateIndustrialDemandJob CreateDemandJob(CountCompanyDataSystem.IndustrialCompanyDatas industrialCompanyDatas, out JobHandle outJobHandle)
    {
        var job = default(UpdateIndustrialDemandJob);

        job.m_FreePropertyChunks = m_FreeCommercialQuery.ToArchetypeChunkListAsync(
            World.UpdateAllocator.ToAllocator, out outJobHandle);
        job.m_UnlockedZoneDatas = m_UnlockedZoneDataQuery.ToComponentDataArray<ZoneData>(Allocator.TempJob);
        job.m_PrefabType = __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
        job.m_RenterType = __TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle;
        job.m_BuildingPropertyDatas = __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;
        job.m_ResourceDatas = __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup;
        job.m_IndustrialCompanies = __TypeHandle.__Game_Companies_IndustrialCompany_RO_ComponentLookup;
        job.m_ResourcePrefabs = m_ResourceSystem.GetPrefabs();
        job.m_DemandParameters = m_DemandParameterQuery.GetSingleton<DemandParameterData>();
        job.m_TaxRates = m_TaxSystem.GetTaxRates();
        job.m_CompanyDemand = m_CompanyDemand;
        job.m_BuildingDemand = m_BuildingDemand;
        job.m_DemandFactors = m_DemandFactors;
        job.m_City = m_CitySystem.City;
        job.m_ResourceDemands = m_ResourceDemands;
        job.m_BuildingDemands = m_BuildingDemands;
        job.m_ProduceCapacity = industrialCompanyDatas.m_ProduceCapacity;
        job.m_CurrentAvailables = industrialCompanyDatas.m_CurrentAvailables;
        job.m_ResourceNeeds = m_CountHouseholdDataSystem.GetResourceNeeds();
        job.m_FreeProperties = m_FreeProperties;
        job.m_Propertyless = industrialCompanyDatas.m_ServicePropertyless;
        job.m_Tourisms = __TypeHandle.__Game_City_Tourism_RO_ComponentLookup;
        

        job.m_FreePropertyChunks = m_FreeIndustrialQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out var outJobHandle);
        job.m_StorageCompanyChunks = m_StorageCompanyQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out var outJobHandle2);
        var updateIndustrialDemandJob = job.m_StorageCompanyChunks = m_StorageCompanyQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out var outJobHandle2);
        job.m_UnlockedZoneDatas = m_UnlockedZoneDataQuery.ToComponentDataArray<ZoneData>(Allocator.TempJob);
        job.m_EntityType = __TypeHandle.__Unity_Entities_Entity_TypeHandle;
        job.m_PrefabType = __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
        job.m_ServiceUpkeepType = __TypeHandle.__Game_City_CityServiceUpkeep_RO_ComponentTypeHandle;
        job.m_StorageLimitDatas = __TypeHandle.__Game_Companies_StorageLimitData_RO_ComponentLookup;
        job.m_SpawnableBuildingDatas = __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;
        job.m_BuildingDatas = __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup;
        job.m_BuildingPropertyDatas = __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;
        job.m_IndustrialProcessDatas = __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;
        job.m_Prefabs = __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup;
        job.m_PropertyRenters = __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup;
        job.m_Attached = __TypeHandle.__Game_Objects_Attached_RO_ComponentLookup;
        job.m_ResourceDatas = __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup;
        job.m_ServiceUpkeeps = __TypeHandle.__Game_Prefabs_ServiceUpkeepData_RO_BufferLookup;
        job.m_CityModifiers = __TypeHandle.__Game_City_CityModifier_RO_BufferLookup;
        job.m_InstalledUpgrades = __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup;
        job.m_Upkeeps = __TypeHandle.__Game_Prefabs_ServiceUpkeepData_RO_BufferLookup;
        job.m_DemandParameters = m_DemandParameterQuery.GetSingleton<DemandParameterData>();
        job.m_EconomyParameters = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>();
        job.m_ResourcePrefabs = m_ResourceSystem.GetPrefabs();
        job.m_EmployableByEducation = m_CountHouseholdDataSystem.GetEmployables();
        job.m_TaxRates = m_TaxSystem.GetTaxRates();
        job.m_FreeWorkplaces = m_CountWorkplacesSystem.GetFreeWorkplaces();
        job.m_City = m_CitySystem.City;
        job.m_IndustrialCompanyDemand = m_IndustrialCompanyDemand;
        job.m_IndustrialBuildingDemand = m_IndustrialBuildingDemand;
        job.m_StorageCompanyDemand = m_StorageCompanyDemand;
        job.m_StorageBuildingDemand = m_StorageBuildingDemand;
        job.m_OfficeCompanyDemand = m_OfficeCompanyDemand;
        job.m_OfficeBuildingDemand = m_OfficeBuildingDemand;
        job.m_IndustrialCompanyDemands = m_IndustrialCompanyDemands;
        job.m_IndustrialBuildingDemands = m_IndustrialBuildingDemands;
        job.m_StorageBuildingDemands = m_StorageBuildingDemands;
        job.m_StorageCompanyDemands = m_StorageCompanyDemands;
        job.m_Propertyless = industrialCompanyDatas.m_ProductionPropertyless;
        job.m_CompanyResourceDemands = industrialCompanyDatas.m_Demand;
        job.m_FreeProperties = m_FreeProperties;
        job.m_Productions = industrialCompanyDatas.m_Production;
        job.m_Storages = m_Storages;
        job.m_FreeStorages = m_FreeStorages;
        job.m_StorageCapacities = m_StorageCapacities;
        job.m_IndustrialDemandFactors = m_IndustrialDemandFactors;
        job.m_OfficeDemandFactors = m_OfficeDemandFactors;
        job.m_ResourceDemands = m_ResourceDemands;

        job.m_DemandData = m_DemandData;

        return job;
    }
}