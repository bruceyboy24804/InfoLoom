using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Debug;
using Game.Prefabs;
using Game.Reflection;
using Game.Tools;
using Game.Triggers;
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
using Game.Companies;
using System;

namespace InfoLoomTwo.Systems;

[CompilerGenerated]
public partial class ResidentialDemandUISystem : UISystemBase
{


    //[BurstCompile]
    private struct UpdateResidentialDemandJob : IJob
    {
        [ReadOnly]
        public NativeList<ArchetypeChunk> m_ResidentialChunks;

        [ReadOnly]
        public NativeList<ArchetypeChunk> m_HouseholdChunks;

        [DeallocateOnJobCompletion]
        [ReadOnly]
        public NativeArray<ZonePropertiesData> m_UnlockedZones;

        [ReadOnly]
        public BufferTypeHandle<Renter> m_RenterType;

        [ReadOnly]
        public ComponentTypeHandle<PrefabRef> m_PrefabType;

        [ReadOnly]
        public ComponentTypeHandle<PropertyRenter> m_PropertyRenterType;

        [ReadOnly]
        public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDatas;

        [ReadOnly]
        public ComponentLookup<Household> m_Households;

        [ReadOnly]
        public ComponentLookup<Population> m_Populations;

        [ReadOnly]
        public ComponentLookup<SpawnableBuildingData> m_SpawnableDatas;

        [ReadOnly]
        public ComponentLookup<ZonePropertiesData> m_ZonePropertyDatas;

        [ReadOnly]
        public NativeList<DemandParameterData> m_DemandParameters;



        [ReadOnly]
        public NativeArray<int> m_StudyPositions;

        [ReadOnly]
        public NativeArray<int> m_TaxRates;

        public Entity m_City;

        public NativeValue<int> m_HouseholdDemand;

        public NativeValue<int3> m_BuildingDemand;

        public NativeArray<int> m_LowDemandFactors;

        public NativeArray<int> m_MediumDemandFactors;

        public NativeArray<int> m_HighDemandFactors;

        //public NativeQueue<TriggerAction> m_TriggerQueue;

        public NativeArray<int> m_Results;


        public CountHouseholdDataSystem.HouseholdData m_HouseholdCountData;

        public CountResidentialPropertySystem.ResidentialPropertyData m_ResidentialPropertyData;

        public Workplaces m_FreeWorkplaces;

        public Workplaces m_TotalWorkplaces;

        [ReadOnly]
        public float m_UnemploymentRate;

        public void Execute()
        {
            bool3 c = default(bool3);
            for (int i = 0; i < m_UnlockedZones.Length; i++)
            {
                if (m_UnlockedZones[i].m_ResidentialProperties > 0f)
                {
                    float num = m_UnlockedZones[i].m_ResidentialProperties / m_UnlockedZones[i].m_SpaceMultiplier;
                    if (!m_UnlockedZones[i].m_ScaleResidentials)
                    {
                        c.x = true;
                    }
                    else if (num < 1f)
                    {
                        c.y = true;
                    }
                    else
                    {
                        c.z = true;
                    }
                }
            }

            int3 freeProperties = m_ResidentialPropertyData.m_FreeProperties;
            int3 totalProperties = m_ResidentialPropertyData.m_TotalProperties;
            DemandParameterData demandParameterData = m_DemandParameters[0];

            int num2 = 0;
            for (int j = 1; j <= 4; j++)
            {
                num2 += m_StudyPositions[j];
            }

            Population population = m_Populations[m_City];
            int num4 = math.max(demandParameterData.m_MinimumHappiness, population.m_AverageHappiness);

            float num5 = 0f;
            for (int k = 0; k < 5; k++)
            {
                num5 += (float)(-(TaxSystem.GetResidentialTaxRate(k, m_TaxRates) - 10));
            }
            num5 = demandParameterData.m_TaxEffect * (num5 / 5f);

            float num6 = demandParameterData.m_HappinessEffect * (float)(num4 - demandParameterData.m_NeutralHappiness);
            float x = (0f - demandParameterData.m_HomelessEffect) * (100f * (float)m_HouseholdCountData.m_HomelessHouseholdCount / (1f + (float)m_HouseholdCountData.m_MovedInHouseholdCount) - demandParameterData.m_NeutralHomelessness);
            x = math.min(x, kMaxFactorEffect);

            float num7 = demandParameterData.m_StudentEffect * math.clamp((float)num2 / 200f, 0f, 20f);
            float num8 = demandParameterData.m_NeutralUnemployment - m_UnemploymentRate;

            if (m_HouseholdCountData.m_MovingInHouseholdCount > kMaxMovingInHouseholdAmount)
            {
                m_HouseholdDemand.value = 0;
            }
            else
            {
                m_HouseholdDemand.value = math.min(200, (int)(num6 + x + num5 + num8 + num7));
            }

            int num9 = Mathf.RoundToInt(100f * (float)(demandParameterData.m_FreeResidentialRequirement.x - freeProperties.x) / (float)demandParameterData.m_FreeResidentialRequirement.x);
            int num10 = Mathf.RoundToInt(100f * (float)(demandParameterData.m_FreeResidentialRequirement.y - freeProperties.y) / (float)demandParameterData.m_FreeResidentialRequirement.y);
            int num11 = Mathf.RoundToInt(100f * (float)(demandParameterData.m_FreeResidentialRequirement.z - freeProperties.z) / (float)demandParameterData.m_FreeResidentialRequirement.z);

            m_LowDemandFactors[7] = Mathf.RoundToInt(num6);
            m_LowDemandFactors[5] = Mathf.RoundToInt(num8);
            m_LowDemandFactors[11] = Mathf.RoundToInt(num5);
            m_LowDemandFactors[13] = num9;

            m_MediumDemandFactors[7] = Mathf.RoundToInt(num6);
            m_MediumDemandFactors[5] = Mathf.RoundToInt(num8);
            m_MediumDemandFactors[11] = Mathf.RoundToInt(num5);
            m_MediumDemandFactors[12] = Mathf.RoundToInt(num7);
            m_MediumDemandFactors[13] = num10;

            m_HighDemandFactors[7] = Mathf.RoundToInt(num6);
            m_HighDemandFactors[8] = Mathf.RoundToInt(x);
            m_HighDemandFactors[5] = Mathf.RoundToInt(num8);
            m_HighDemandFactors[11] = Mathf.RoundToInt(num5);
            m_HighDemandFactors[12] = Mathf.RoundToInt(num7);
            m_HighDemandFactors[13] = num11;

            int num12 = ((m_LowDemandFactors[13] >= 0) ? (m_LowDemandFactors[7] + m_LowDemandFactors[11] + m_LowDemandFactors[5] + m_LowDemandFactors[13]) : 0);
            int num13 = ((m_MediumDemandFactors[13] >= 0) ? (m_MediumDemandFactors[7] + m_MediumDemandFactors[11] + m_MediumDemandFactors[12] + m_MediumDemandFactors[5] + m_MediumDemandFactors[13]) : 0);
            int num14 = ((m_HighDemandFactors[13] >= 0) ? (m_HighDemandFactors[7] + m_HighDemandFactors[8] + m_HighDemandFactors[11] + m_HighDemandFactors[12] + m_HighDemandFactors[5] + m_HighDemandFactors[13]) : 0);

            m_BuildingDemand.value = new int3(
                math.clamp(m_HouseholdDemand.value / 2 + num9 + num12, 0, 100),
                math.clamp(m_HouseholdDemand.value / 2 + num10 + num13, 0, 100),
                math.clamp(m_HouseholdDemand.value / 2 + num11 + num14, 0, 100)
            );
            m_BuildingDemand.value = math.select(default(int3), m_BuildingDemand.value, c);

            // InfoLoom: Store results instead of enqueueing trigger
            m_Results[0] = totalProperties.x;
            m_Results[1] = totalProperties.y;
            m_Results[2] = totalProperties.z;
            m_Results[3] = totalProperties.x - freeProperties.x;
            m_Results[4] = totalProperties.y - freeProperties.y;
            m_Results[5] = totalProperties.z - freeProperties.z;
            m_Results[6] = Mathf.RoundToInt(10f * demandParameterData.m_FreeResidentialRequirement.x);
            m_Results[7] = population.m_AverageHappiness;
            m_Results[8] = demandParameterData.m_NeutralHappiness;
            m_Results[9] = (int)(m_UnemploymentRate * 100f);
            m_Results[10] = Mathf.RoundToInt(10f * demandParameterData.m_NeutralUnemployment);
            m_Results[11] = m_HouseholdCountData.m_HomelessHouseholdCount;
            m_Results[12] = m_HouseholdCountData.m_MovedInHouseholdCount;
            m_Results[13] = Mathf.RoundToInt(10f * demandParameterData.m_NeutralHomelessness);
            m_Results[14] = num2;
            m_Results[15] = Mathf.RoundToInt(10f * (10f - num5 / demandParameterData.m_TaxEffect));
            m_Results[16] = m_HouseholdDemand.value;
            m_Results[17] = Mathf.RoundToInt(100f * num7 / (num7 + math.max(0, num8)));

        }

    }

    private struct TypeHandle
    {
        [ReadOnly]
        public BufferTypeHandle<Renter> __Game_Buildings_Renter_RO_BufferTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentTypeHandle;

        [ReadOnly]
        public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<Population> __Game_City_Population_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

        [ReadOnly]
        public ComponentLookup<ZonePropertiesData> __Game_Prefabs_ZonePropertiesData_RO_ComponentLookup;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void __AssignHandles(ref SystemState state)
        {
            __Game_Buildings_Renter_RO_BufferTypeHandle = state.GetBufferTypeHandle<Renter>(isReadOnly: true);
            __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
            __Game_Buildings_PropertyRenter_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PropertyRenter>(isReadOnly: true);
            __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
            __Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(isReadOnly: true);
            __Game_City_Population_RO_ComponentLookup = state.GetComponentLookup<Population>(isReadOnly: true);
            __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
            __Game_Prefabs_ZonePropertiesData_RO_ComponentLookup = state.GetComponentLookup<ZonePropertiesData>(isReadOnly: true);
        }
    }

    private const string kGroup = "cityInfo";

    public static readonly int kMaxFactorEffect = 15;

    public static readonly int kMaxMovingInHouseholdAmount = 500;

    private SimulationSystem m_SimulationSystem;

    private TaxSystem m_TaxSystem;

    private CountStudyPositionsSystem m_CountStudyPositionsSystem;

    private CountWorkplacesSystem m_CountWorkplacesSystem;

    private CountHouseholdDataSystem m_CountHouseholdDataSystem;

    private CountResidentialPropertySystem m_CountResidentialPropertySystem;

    private CitySystem m_CitySystem;

    //private TriggerSystem m_TriggerSystem;

    private EntityQuery m_DemandParameterGroup;

    private EntityQuery m_AllHouseholdGroup;

    private EntityQuery m_AllResidentialGroup;

    private EntityQuery m_UnlockedZoneQuery;

    private NativeValue<int> m_HouseholdDemand;

    private NativeValue<int3> m_BuildingDemand;

    //[EnumArray(typeof(DemandFactor))]
    //[DebugWatchValue]
    private NativeArray<int> m_LowDemandFactors;

    //[EnumArray(typeof(DemandFactor))]
    //[DebugWatchValue]
    private NativeArray<int> m_MediumDemandFactors;

    //[EnumArray(typeof(DemandFactor))]
    //[DebugWatchValue]
    private NativeArray<int> m_HighDemandFactors;

    //[DebugWatchDeps]
    //private JobHandle m_WriteDependencies;

    private JobHandle m_ReadDependencies;

    private int m_LastHouseholdDemand;

    private int3 m_LastBuildingDemand;

    private TypeHandle __TypeHandle;

    //[DebugWatchValue(color = "#27ae60")]
    public int householdDemand => m_LastHouseholdDemand;

    //[DebugWatchValue(color = "#117a65")]
    public int3 buildingDemand => m_LastBuildingDemand;

    // InfoLoom

    private RawValueBinding m_uiResults;

    private NativeArray<int> m_Results;

    // RESIDENTIAL
    // 0,1,2 - count of residential properties, low/med/high
    // 3,4,5 - count of occupied properties, low/med/high
    // 6 - free residential ratio (%) * 10
    // 7 & 8 - AverageHappiness, neutral happiness
    // 9 & 10 - unemployment value, neutral unemployment (%) * 10
    // 11, 12, 13 - homeless households, total households, NeutralHomelessness (%) * 10
    // 14 - study positions
    // 15 - tax rate (weighted)

    // 240209 Set gameMode to avoid errors in the Editor
    public override GameMode gameMode => GameMode.Game;

    /* not used
    public NativeArray<int> GetLowDensityDemandFactors(out JobHandle deps)
    {
        deps = m_WriteDependencies;
        return m_LowDemandFactors;
    }

    public NativeArray<int> GetMediumDensityDemandFactors(out JobHandle deps)
    {
        deps = m_WriteDependencies;
        return m_MediumDemandFactors;
    }

    public NativeArray<int> GetHighDensityDemandFactors(out JobHandle deps)
    {
        deps = m_WriteDependencies;
        return m_HighDemandFactors;
    }
    */

    public void AddReader(JobHandle reader)
    {
        m_ReadDependencies = JobHandle.CombineDependencies(m_ReadDependencies, reader);
    }

    //[Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>(); // TODO: use UIUpdateState eventually
        m_DemandParameterGroup = GetEntityQuery(ComponentType.ReadOnly<DemandParameterData>());
        m_AllHouseholdGroup = GetEntityQuery(ComponentType.ReadOnly<Household>(), ComponentType.Exclude<TouristHousehold>(), ComponentType.Exclude<CommuterHousehold>(), ComponentType.Exclude<MovingAway>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
        m_AllResidentialGroup = GetEntityQuery(ComponentType.ReadOnly<ResidentialProperty>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Condemned>(), ComponentType.Exclude<Abandoned>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Temp>());
        m_UnlockedZoneQuery = GetEntityQuery(ComponentType.ReadOnly<ZoneData>(), ComponentType.ReadOnly<ZonePropertiesData>(), ComponentType.Exclude<Locked>());
        m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
        m_TaxSystem = base.World.GetOrCreateSystemManaged<TaxSystem>();
        m_CountWorkplacesSystem = base.World.GetOrCreateSystemManaged<CountWorkplacesSystem>();
        m_CountHouseholdDataSystem = base.World.GetOrCreateSystemManaged<CountHouseholdDataSystem>();
        m_CountResidentialPropertySystem = base.World.GetOrCreateSystemManaged<CountResidentialPropertySystem>();
        m_CountStudyPositionsSystem = base.World.GetOrCreateSystemManaged<CountStudyPositionsSystem>();
        //m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
        m_HouseholdDemand = new NativeValue<int>(Allocator.Persistent);
        m_BuildingDemand = new NativeValue<int3>(Allocator.Persistent);
        m_LowDemandFactors = new NativeArray<int>(18, Allocator.Persistent);
        m_MediumDemandFactors = new NativeArray<int>(18, Allocator.Persistent);
        m_HighDemandFactors = new NativeArray<int>(18, Allocator.Persistent);

        // InfoLoom
        SetDefaults(); // there is no serialization, so init just for safety
        m_Results = new NativeArray<int>(18, Allocator.Persistent);

        AddBinding(m_uiResults = new RawValueBinding(kGroup, "ilResidential", delegate (IJsonWriter binder)
        {
            binder.ArrayBegin(m_Results.Length);
            for (int i = 0; i < m_Results.Length; i++)
                binder.Write(m_Results[i]);
            binder.ArrayEnd();
        }));

        Mod.log.Info("ResidentialDemandUISystem created.");
    }

    //[Preserve]
    protected override void OnDestroy()
    {
        m_HouseholdDemand.Dispose();
        m_BuildingDemand.Dispose();
        m_LowDemandFactors.Dispose();
        m_MediumDemandFactors.Dispose();
        m_HighDemandFactors.Dispose();
        // InfoLoom
        m_Results.Dispose();
        base.OnDestroy();
    }

    public void SetDefaults() //Context context)
    {
        m_HouseholdDemand.value = 0;
        m_BuildingDemand.value = default(int3);
        m_LowDemandFactors.Fill(0);
        m_MediumDemandFactors.Fill(0);
        m_HighDemandFactors.Fill(0);
        m_LastHouseholdDemand = 0;
        m_LastBuildingDemand = default(int3);
    }

    /* not used
    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        writer.Write(m_HouseholdDemand.value);
        writer.Write(m_BuildingDemand.value);
        writer.Write(m_LowDemandFactors.Length);
        writer.Write(m_LowDemandFactors);
        writer.Write(m_MediumDemandFactors);
        writer.Write(m_HighDemandFactors);
        writer.Write(m_LastHouseholdDemand);
        writer.Write(m_LastBuildingDemand);
    }
    */

    /* not used
    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        reader.Read(out int value);
        m_HouseholdDemand.value = value;
        if (reader.context.version < Version.residentialDemandSplit)
        {
            reader.Read(out int value2);
            m_BuildingDemand.value = new int3(value2 / 3, value2 / 3, value2 / 3);
        }
        else
        {
            reader.Read(out int3 value3);
            m_BuildingDemand.value = value3;
        }
        if (reader.context.version < Version.demandFactorCountSerialization)
        {
            NativeArray<int> nativeArray = new NativeArray<int>(13, Allocator.Temp);
            reader.Read(nativeArray);
            CollectionUtils.CopySafe(nativeArray, m_LowDemandFactors);
            nativeArray.Dispose();
        }
        else
        {
            reader.Read(out int value4);
            if (value4 == m_LowDemandFactors.Length)
            {
                reader.Read(m_LowDemandFactors);
                reader.Read(m_MediumDemandFactors);
                reader.Read(m_HighDemandFactors);
            }
            else
            {
                NativeArray<int> nativeArray2 = new NativeArray<int>(value4, Allocator.Temp);
                reader.Read(nativeArray2);
                CollectionUtils.CopySafe(nativeArray2, m_LowDemandFactors);
                reader.Read(nativeArray2);
                CollectionUtils.CopySafe(nativeArray2, m_MediumDemandFactors);
                reader.Read(nativeArray2);
                CollectionUtils.CopySafe(nativeArray2, m_HighDemandFactors);
                nativeArray2.Dispose();
            }
        }
        reader.Read(out m_LastHouseholdDemand);
        if (reader.context.version < Version.residentialDemandSplit)
        {
            reader.Read(out int value5);
            m_LastBuildingDemand = new int3(value5 / 3, value5 / 3, value5 / 3);
        }
        else
        {
            reader.Read(out m_LastBuildingDemand);
        }
    }
    */

    //[Preserve]
    protected override void OnUpdate()
    {
        if (m_SimulationSystem.frameIndex % 128 != 11)
            return;
        //Plugin.Log($"OnUpdate: {m_SimulationSystem.frameIndex}");
        base.OnUpdate();
        ResetResults();

        if (!m_DemandParameterGroup.IsEmptyIgnoreFilter)
        {
            m_LastHouseholdDemand = m_HouseholdDemand.value;
            m_LastBuildingDemand = m_BuildingDemand.value;
            __TypeHandle.__Game_Prefabs_ZonePropertiesData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_City_Population_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
            UpdateResidentialDemandJob updateResidentialDemandJob = default(UpdateResidentialDemandJob);
            updateResidentialDemandJob.m_ResidentialChunks = m_AllResidentialGroup.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out var outJobHandle3);
            updateResidentialDemandJob.m_HouseholdChunks = m_AllHouseholdGroup.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out var outJobHandle2);
            updateResidentialDemandJob.m_UnlockedZones = m_UnlockedZoneQuery.ToComponentDataArray<ZonePropertiesData>(Allocator.TempJob);
            updateResidentialDemandJob.m_RenterType = __TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle;
            updateResidentialDemandJob.m_PrefabType = __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
            updateResidentialDemandJob.m_PropertyRenterType = __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle;
            updateResidentialDemandJob.m_BuildingPropertyDatas = __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;
            updateResidentialDemandJob.m_Households = __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup;
            updateResidentialDemandJob.m_Populations = __TypeHandle.__Game_City_Population_RO_ComponentLookup;
            updateResidentialDemandJob.m_SpawnableDatas = __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;
            updateResidentialDemandJob.m_ZonePropertyDatas = __TypeHandle.__Game_Prefabs_ZonePropertiesData_RO_ComponentLookup;
            updateResidentialDemandJob.m_UnlockedZones = m_UnlockedZoneQuery.ToComponentDataArray<ZonePropertiesData>(Allocator.TempJob);
            updateResidentialDemandJob.m_Populations = __TypeHandle.__Game_City_Population_RO_ComponentLookup;
            updateResidentialDemandJob.m_DemandParameters = m_DemandParameterGroup.ToComponentDataListAsync<DemandParameterData>(base.World.UpdateAllocator.ToAllocator, out var outJobHandle);
            updateResidentialDemandJob.m_StudyPositions = m_CountStudyPositionsSystem.GetStudyPositionsByEducation(out var deps);
            updateResidentialDemandJob.m_FreeWorkplaces = m_CountWorkplacesSystem.GetFreeWorkplaces();
            updateResidentialDemandJob.m_TotalWorkplaces = m_CountWorkplacesSystem.GetTotalWorkplaces();
            updateResidentialDemandJob.m_HouseholdCountData = m_CountHouseholdDataSystem.GetHouseholdCountData();
            updateResidentialDemandJob.m_ResidentialPropertyData = m_CountResidentialPropertySystem.GetResidentialPropertyData();
            updateResidentialDemandJob.m_TaxRates = m_TaxSystem.GetTaxRates();
            updateResidentialDemandJob.m_City = m_CitySystem.City;
            updateResidentialDemandJob.m_HouseholdDemand = m_HouseholdDemand;
            updateResidentialDemandJob.m_BuildingDemand = m_BuildingDemand;
            updateResidentialDemandJob.m_LowDemandFactors = m_LowDemandFactors;
            updateResidentialDemandJob.m_MediumDemandFactors = m_MediumDemandFactors;
            updateResidentialDemandJob.m_HighDemandFactors = m_HighDemandFactors;
            updateResidentialDemandJob.m_UnemploymentRate = m_CountHouseholdDataSystem.UnemploymentRate;
            //updateResidentialDemandJob.m_TriggerQueue = m_TriggerSystem.CreateActionBuffer();
            updateResidentialDemandJob.m_Results = m_Results;
            UpdateResidentialDemandJob jobData = updateResidentialDemandJob;
            IJobExtensions.Schedule(jobData, JobUtils.CombineDependencies(base.Dependency, m_ReadDependencies, outJobHandle, outJobHandle2, outJobHandle3, deps)).Complete();
            // since this is a copy of an actual simulation system but for UI purposes, then noone will read from us or wait for us
            //m_WriteDependencies = base.Dependency;
            //m_CountEmploymentSystem.AddReader(base.Dependency);
            //m_CountStudyPositionsSystem.AddReader(base.Dependency);
            //m_TaxSystem.AddReader(base.Dependency);
            //m_TriggerSystem.AddActionBufferWriter(base.Dependency);
        }
        // Update UI
        m_uiResults.Update();
    }

    private void ResetResults()
    {
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

    //[Preserve]
    public ResidentialDemandUISystem()
    {
    }
}