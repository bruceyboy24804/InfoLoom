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

namespace InfoLoom.Systems
{
    
    public partial class ResidentialSystem : GameSystemBase
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
            public float m_UnemploymentRate;

            [ReadOnly]
            public NativeArray<int> m_StudyPositions;

            [ReadOnly]
            public NativeArray<int> m_TaxRates;

            // Add homeless count values from CountHouseholdDataSystem
            [ReadOnly]
            public int m_HomelessHouseholdCount;

            [ReadOnly]
            public int m_TotalHouseholdCount;

            public Entity m_City;

            public NativeValue<int> m_HouseholdDemand;

            public NativeValue<int3> m_BuildingDemand;

            public NativeArray<int> m_LowDemandFactors;

            public NativeArray<int> m_MediumDemandFactors;

            public NativeArray<int> m_HighDemandFactors;

            public NativeArray<int> m_Results;
             
            public void Execute()
            {
                DemandParameterData demandParameterData = m_DemandParameters[0];
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
                // free res properties
                int3 @int = default(int3);
                // total res properties
                int3 int2 = default(int3);
                int value = 0;
                for (int j = 1; j <= 4; j++)
                {
                    value += m_StudyPositions[j];
                }
                
                int value2 = Mathf.RoundToInt(m_UnemploymentRate);
                Population population = m_Populations[m_City];
                int num2 = math.max(demandParameterData.m_MinimumHappiness, population.m_AverageHappiness);
                
                // Use homeless counts from CountHouseholdDataSystem instead of calculating them
                int num3 = m_HomelessHouseholdCount; // homeless households
                int num4 = m_TotalHouseholdCount;    // total households
                
                float num5 = 0f;
                for (int k = 0; k <= 2; k++)
                {
                    num5 -= 3f * ((float)k + 1f) * ((float)TaxSystem.GetResidentialTaxRate(k, m_TaxRates) - 10f);
                }
                float taxRate = 10f - num5 / (3f * 6f);
                float num6 = demandParameterData.m_HappinessEffect * (float)(num2 - demandParameterData.m_NeutralHappiness);
                float num7 = (0f - demandParameterData.m_HomelessEffect) * (100f * (float)num3 / (1f + (float)num4) - demandParameterData.m_NeutralHomelessness);
                float num8 = (0f - m_UnemploymentRate) * ((float)value2 - demandParameterData.m_NeutralUnemployment);
                // value - study positions
                float num9 = math.min(math.sqrt(2f * (float)value), -1f + math.min(2.5f, math.sqrt((float)value / 300f)) + 0.5f * (num6 + num8 + num7 + num5));
                float y = num8 + num6 + num7 + num5;
                m_HouseholdDemand.value = math.min(100, math.max(0, Mathf.RoundToInt(math.max(num9, y))));
                m_Results[16] = Mathf.RoundToInt(math.max(num9, y)); // 220204 household demand
                m_LowDemandFactors[7] = Mathf.RoundToInt(num6);
                m_LowDemandFactors[8] = Mathf.RoundToInt(num7);
                m_LowDemandFactors[6] = Mathf.RoundToInt(num8);
                m_LowDemandFactors[11] = Mathf.RoundToInt(num5);
                m_MediumDemandFactors[7] = Mathf.RoundToInt(num6); // Happiness
                m_MediumDemandFactors[8] = Mathf.RoundToInt(num7); // Homelessness
                m_MediumDemandFactors[6] = Mathf.RoundToInt(num8); // Unemployment
                m_MediumDemandFactors[11] = Mathf.RoundToInt(num5); // Taxes
                m_MediumDemandFactors[12] = Mathf.RoundToInt(num9); // Students
                m_HighDemandFactors[7] = Mathf.RoundToInt(num6);
                m_HighDemandFactors[8] = Mathf.RoundToInt(num7);
                m_HighDemandFactors[6] = Mathf.RoundToInt(num8);
                m_HighDemandFactors[11] = Mathf.RoundToInt(num5);
                m_HighDemandFactors[12] = Mathf.RoundToInt(num9);
                
                // calculate empty buildings, loop through all ResidentialProperty, ex. Abandoned, Condemened, Destroyed, etc.
                for (int l = 0; l < m_ResidentialChunks.Length; l++)
                {
                    ArchetypeChunk archetypeChunk2 = m_ResidentialChunks[l];
                    NativeArray<PrefabRef> nativeArray = archetypeChunk2.GetNativeArray(ref m_PrefabType);
                    BufferAccessor<Renter> bufferAccessor = archetypeChunk2.GetBufferAccessor(ref m_RenterType);
                    // iterate through buildings
                    for (int m = 0; m < nativeArray.Length; m++)
                    {
                        Entity prefab = nativeArray[m].m_Prefab;
                        SpawnableBuildingData spawnableBuildingData = m_SpawnableDatas[prefab];
                        ZonePropertiesData zonePropertiesData = m_ZonePropertyDatas[spawnableBuildingData.m_ZonePrefab];
                        float num10 = zonePropertiesData.m_ResidentialProperties / zonePropertiesData.m_SpaceMultiplier;
                        if (!m_BuildingPropertyDatas.HasComponent(prefab))
                        {
                            continue;
                        }
                        BuildingPropertyData buildingPropertyData = m_BuildingPropertyDatas[prefab];
                        DynamicBuffer<Renter> dynamicBuffer = bufferAccessor[m];
                        // occupied properties
                        int num11 = 0;
                        for (int n = 0; n < dynamicBuffer.Length; n++)
                        {
                            if (m_Households.HasComponent(dynamicBuffer[n].m_Renter))
                            {
                                num11++;
                            }
                        }
                        if (!zonePropertiesData.m_ScaleResidentials)
                        {
                            // low density (not scalable, only 1 household per buildinng)
                            int2.x++;
                            @int.x += 1 - num11;
                            m_Results[3] += num11; // low
                        }
                        else if (num10 < 1f)
                        {
                            // medium density, scaling < 1f
                            int2.y += buildingPropertyData.m_ResidentialProperties;
                            @int.y += buildingPropertyData.m_ResidentialProperties - num11;
                            m_Results[4] += num11; // med
                        }
                        else
                        {
                            // high density, scaling >= 1f
                            int2.z += buildingPropertyData.m_ResidentialProperties;
                            @int.z += buildingPropertyData.m_ResidentialProperties - num11;
                            m_Results[5] += num11; // high
                        }
                    }
                }
                m_Results[3] = math.min(m_Results[3], int2.x); // Clamp low occupied
                m_Results[4] = math.min(m_Results[4], int2.y); // Clamp med occupied
                m_Results[5] = math.min(m_Results[5], int2.z); // Clamp high occupied
                
                @int.x = int2.x - m_Results[3];
                @int.y = int2.y - m_Results[4];
                @int.z = int2.z - m_Results[5];
                
                int num12 = m_LowDemandFactors[7] + m_LowDemandFactors[8] + m_LowDemandFactors[6] + m_LowDemandFactors[11] + m_LowDemandFactors[12];
                int num13 = m_MediumDemandFactors[7] + m_MediumDemandFactors[8] + m_MediumDemandFactors[6] + m_MediumDemandFactors[11] + m_MediumDemandFactors[12];
                int num14 = m_HighDemandFactors[7] + m_HighDemandFactors[8] + m_HighDemandFactors[6] + m_HighDemandFactors[11] + m_HighDemandFactors[12];
                // @float is needed free res properties (capped at min. 5)
                float3 @float = new float3(
                    math.max(5f, 0.01f * demandParameterData.m_FreeResidentialRequirement.x * math.max(1f, int2.x)), 
                    math.max(5f, 0.01f * demandParameterData.m_FreeResidentialRequirement.y * math.max(1f, int2.y)), 
                    math.max(5f, 0.01f * demandParameterData.m_FreeResidentialRequirement.z * math.max(1f, int2.z))
                );
                // actual demand: (needed - current) / needed => (1 - current/needed)
                // if current > needed => demand = 0
                // if current = 0 => demand = 100
                // if current < needed => demand = 0..100
                m_BuildingDemand.value = new int3((int)(100f * math.saturate((@float.x - (float)@int.x) / math.max(1f, @float.x))), (int)(100f * math.saturate((@float.y - (float)@int.y) / math.max(1f, @float.y))), (int)(100f * math.saturate((@float.z - (float)@int.z) / math.max(1f, @float.z))));
                m_BuildingDemand.value = math.select(default(int3), m_BuildingDemand.value, c);
                
                // EmptyBuildings
                // if sum of other factors is LOWER than raw building demand, then EmptyBuildings is 0
                // if sum of other factors is HIGHER than raw building demand, then EmptyBuildings is a diff between them
                // so, it says if CURRENT empty buildings are enough to satisfy existing demand, or MORE
                // valuable info: how many empty buildings are needed to satisfy current demand
                m_HighDemandFactors[13] = math.min(0, m_BuildingDemand.value.z - num14);
                m_MediumDemandFactors[13] = math.min(0, m_BuildingDemand.value.y - num13);
                m_LowDemandFactors[13] = math.min(0, m_BuildingDemand.value.x - num12);
                
                // InfoLoom
                m_Results[0] = int2.x; // total residential properties, low
                m_Results[1] = int2.y; // total residential properties, med
                m_Results[2] = int2.z; // total residential properties, high
                m_Results[6] = Mathf.RoundToInt(10f * (demandParameterData.m_FreeResidentialRequirement.x + 
                                           demandParameterData.m_FreeResidentialRequirement.y + 
                                           demandParameterData.m_FreeResidentialRequirement.z) / 3f);
                m_Results[7] = population.m_AverageHappiness;
                m_Results[8] = demandParameterData.m_NeutralHappiness;
                m_Results[9] = (int)m_UnemploymentRate;
                m_Results[10] = Mathf.RoundToInt(10f * demandParameterData.m_NeutralUnemployment);
                m_Results[11] = num3; // homeless households
                m_Results[12] = num4; // households
                m_Results[13] = Mathf.RoundToInt(10f * demandParameterData.m_NeutralHomelessness);
                m_Results[14] = value;
                m_Results[15] = Mathf.RoundToInt(10f * taxRate);
                
                // 240204 student ratio for new households, from HouseholdSpawnSystem
                int fact6 = math.max(0, m_LowDemandFactors[6] + m_MediumDemandFactors[6] + m_HighDemandFactors[6]); // unemployment
                int fact12 = math.max(0, m_LowDemandFactors[12] + m_MediumDemandFactors[12] + m_HighDemandFactors[12]); // students
                m_Results[17] = fact12 == 0 ? 0 : Mathf.RoundToInt(100f * (float)fact12 / (float)(fact12 + fact6));
            }
        }

        private const string kGroup = "cityInfo";

        private SimulationSystem m_SimulationSystem;
        private TaxSystem m_TaxSystem;
        private CountHouseholdDataSystem m_CountHouseholdDataSystem;
        private CountStudyPositionsSystem m_CountStudyPositionsSystem;
        private CitySystem m_CitySystem;
        private EntityQuery m_DemandParameterGroup;
        private EntityQuery m_AllHouseholdGroup;
        private EntityQuery m_AllResidentialGroup;
        private EntityQuery m_UnlockedZoneQuery;
        private NativeValue<int> m_HouseholdDemand;
        private NativeValue<int3> m_BuildingDemand;
        private NativeArray<int> m_LowDemandFactors;
        private NativeArray<int> m_MediumDemandFactors;
        private NativeArray<int> m_HighDemandFactors;
        private JobHandle m_ReadDependencies;
        private int m_LastHouseholdDemand;
        private int3 m_LastBuildingDemand;
        public int householdDemand => m_LastHouseholdDemand;
        public int3 buildingDemand => m_LastBuildingDemand;

        public NativeArray<int> m_Results;

        // RESIDENTIAL
        // 0,1,2 - count of residential properties, low/med/high
        // 3,4,5 - count of occupied properties, low/med/high
        // 6 - free residential ratio (%) * 10
        // 7 & 8 - AverageHappiness, neutral happiness
        // 9 & 10 - unemployment value, neutral unemployment (%) * 10
        // 11, 12, 13 - homeless households, total households, NeutralHomelessness (%) * 10
        // 14 - study positions
        // 15 - tax rate (weighted)

        public bool IsPanelVisible { get; set; }

        public void AddReader(JobHandle reader)
        {
            m_ReadDependencies = JobHandle.CombineDependencies(m_ReadDependencies, reader);
        }
        
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
            m_CountHouseholdDataSystem = base.World.GetOrCreateSystemManaged<CountHouseholdDataSystem>();
            m_CountStudyPositionsSystem = base.World.GetOrCreateSystemManaged<CountStudyPositionsSystem>();
            m_HouseholdDemand = new NativeValue<int>(Allocator.Persistent);
            m_BuildingDemand = new NativeValue<int3>(Allocator.Persistent);
            m_LowDemandFactors = new NativeArray<int>(18, Allocator.Persistent);
            m_MediumDemandFactors = new NativeArray<int>(18, Allocator.Persistent);
            m_HighDemandFactors = new NativeArray<int>(18, Allocator.Persistent);

            // InfoLoom
            SetDefaults(); // there is no serialization, so init just for safety
            m_Results = new NativeArray<int>(18, Allocator.Persistent);
        }

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

        public void SetDefaults()
        {
            m_HouseholdDemand.value = 0;
            m_BuildingDemand.value = default(int3);
            m_LowDemandFactors.Fill(0);
            m_MediumDemandFactors.Fill(0);
            m_HighDemandFactors.Fill(0);
            m_LastHouseholdDemand = 0;
            m_LastBuildingDemand = default(int3);
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 256;
        }
        
        protected override void OnUpdate()
        {
            if (!IsPanelVisible)
                return;
            
            ResetResults();

            if (!m_DemandParameterGroup.IsEmptyIgnoreFilter)
            {
                m_LastHouseholdDemand = m_HouseholdDemand.value;
                m_LastBuildingDemand = m_BuildingDemand.value;
                UpdateResidentialDemandJob updateResidentialDemandJob = default(UpdateResidentialDemandJob);
                updateResidentialDemandJob.m_ResidentialChunks = m_AllResidentialGroup.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out var outJobHandle);
                updateResidentialDemandJob.m_HouseholdChunks = m_AllHouseholdGroup.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out var outJobHandle2);
                updateResidentialDemandJob.m_UnlockedZones = m_UnlockedZoneQuery.ToComponentDataArray<ZonePropertiesData>(Allocator.TempJob);
                updateResidentialDemandJob.m_RenterType = SystemAPI.GetBufferTypeHandle<Renter>(isReadOnly: true);
                updateResidentialDemandJob.m_PrefabType = SystemAPI.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
                updateResidentialDemandJob.m_PropertyRenterType = SystemAPI.GetComponentTypeHandle<PropertyRenter>(isReadOnly: true);
                updateResidentialDemandJob.m_BuildingPropertyDatas = SystemAPI.GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
                updateResidentialDemandJob.m_Households = SystemAPI.GetComponentLookup<Household>(isReadOnly: true);
                updateResidentialDemandJob.m_Populations = SystemAPI.GetComponentLookup<Population>(isReadOnly: true);
                updateResidentialDemandJob.m_SpawnableDatas = SystemAPI.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
                updateResidentialDemandJob.m_ZonePropertyDatas = SystemAPI.GetComponentLookup<ZonePropertiesData>(isReadOnly: true);
                updateResidentialDemandJob.m_DemandParameters = m_DemandParameterGroup.ToComponentDataListAsync<DemandParameterData>(base.World.UpdateAllocator.ToAllocator, out var outJobHandle3);
                updateResidentialDemandJob.m_UnemploymentRate = m_CountHouseholdDataSystem.UnemploymentRate;
                
                // Use the homelessness data from CountHouseholdDataSystem
                updateResidentialDemandJob.m_HomelessHouseholdCount = m_CountHouseholdDataSystem.HomelessHouseholdCount;
                updateResidentialDemandJob.m_TotalHouseholdCount = m_CountHouseholdDataSystem.MovedInHouseholdCount;
                
                updateResidentialDemandJob.m_StudyPositions = m_CountStudyPositionsSystem.GetStudyPositionsByEducation(out var deps);
                updateResidentialDemandJob.m_TaxRates = m_TaxSystem.GetTaxRates();
                updateResidentialDemandJob.m_City = m_CitySystem.City;
                updateResidentialDemandJob.m_HouseholdDemand = m_HouseholdDemand;
                updateResidentialDemandJob.m_BuildingDemand = m_BuildingDemand;
                updateResidentialDemandJob.m_LowDemandFactors = m_LowDemandFactors;
                updateResidentialDemandJob.m_MediumDemandFactors = m_MediumDemandFactors;
                updateResidentialDemandJob.m_HighDemandFactors = m_HighDemandFactors;
                updateResidentialDemandJob.m_Results = m_Results;
                
                IJobExtensions.Schedule(updateResidentialDemandJob, JobUtils.CombineDependencies(base.Dependency, m_ReadDependencies, outJobHandle, outJobHandle2, outJobHandle3, deps)).Complete();
            }
        }

        private void ResetResults()
        {
            m_Results.Fill<int>(0);
        }
    }
}