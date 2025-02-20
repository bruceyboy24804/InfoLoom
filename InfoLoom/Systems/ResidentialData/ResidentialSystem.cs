using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace InfoLoomTwo.Systems.ResidentialData
{
    public partial class ResidentialSystem : SystemBase
    {
        [BurstCompile]
        private struct UpdateResidentialDemandJob : IJob
        {
            /*[ReadOnly]
            public NativeList<ArchetypeChunk> m_ResidentialChunks;

            [ReadOnly]
            public NativeList<ArchetypeChunk> m_HouseholdChunks;*/

            
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

                int num9 = Mathf.RoundToInt(100 * (demandParameterData.m_FreeResidentialRequirement.x - freeProperties.x) / demandParameterData.m_FreeResidentialRequirement.x);
                int num10 = Mathf.RoundToInt(100 * (demandParameterData.m_FreeResidentialRequirement.y - freeProperties.y) / demandParameterData.m_FreeResidentialRequirement.y);
                int num11 = Mathf.RoundToInt(100 * (demandParameterData.m_FreeResidentialRequirement.z - freeProperties.z) / demandParameterData.m_FreeResidentialRequirement.z);

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



                float3 freeReqFloat = (float3)demandParameterData.m_FreeResidentialRequirement;
                float3 computedValue = new float3(
                    math.max(5f, 0.01f * freeReqFloat.x * math.max(1f, freeReqFloat.x)),
                    math.max(5f, 0.01f * freeReqFloat.y * math.max(1f, freeReqFloat.y)),
                    math.max(5f, 0.01f * freeReqFloat.z * math.max(1f, freeReqFloat.z))
                );
                float average = math.csum(computedValue) / 3f;
    

                // InfoLoom: Store results instead of enqueueing trigger
                m_Results[0] = totalProperties.x;
                m_Results[1] = totalProperties.y;
                m_Results[2] = totalProperties.z;
                m_Results[3] = totalProperties.x - freeProperties.x;
                m_Results[4] = totalProperties.y - freeProperties.y;
                m_Results[5] = totalProperties.z - freeProperties.z;
                
                m_Results[6] =  Mathf.RoundToInt(10f * average);

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

        

        //[DebugWatchValue(color = "#27ae60")]
        public int householdDemand => m_LastHouseholdDemand;

        //[DebugWatchValue(color = "#117a65")]
        public int3 buildingDemand => m_LastBuildingDemand;

        // InfoLoom

        

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

       
        protected override void OnUpdate()
        {
            if (!IsPanelVisible)
                return;
            if (m_SimulationSystem.frameIndex % 256 != 0 && !ForceUpdate)
                return;
            ForceUpdate = false;
            
            ResetResults();

            if (!m_DemandParameterGroup.IsEmptyIgnoreFilter)
            {
                m_LastHouseholdDemand = m_HouseholdDemand.value;
                m_LastBuildingDemand = m_BuildingDemand.value;
                
                
                
                UpdateResidentialDemandJob updateResidentialDemandJob = default(UpdateResidentialDemandJob);
                /*updateResidentialDemandJob.m_ResidentialChunks = m_AllResidentialGroup.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out var outJobHandle3);
                updateResidentialDemandJob.m_HouseholdChunks = m_AllHouseholdGroup.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out var outJobHandle2);*/
                updateResidentialDemandJob.m_UnlockedZones = m_UnlockedZoneQuery.ToComponentDataArray<ZonePropertiesData>(Allocator.TempJob);
                updateResidentialDemandJob.m_RenterType = SystemAPI.GetBufferTypeHandle<Renter>(isReadOnly: true);
                updateResidentialDemandJob.m_PrefabType = SystemAPI.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
                updateResidentialDemandJob.m_PropertyRenterType = SystemAPI.GetComponentTypeHandle<PropertyRenter>(isReadOnly: true);
                updateResidentialDemandJob.m_BuildingPropertyDatas = SystemAPI.GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
                updateResidentialDemandJob.m_Households = SystemAPI.GetComponentLookup<Household>(isReadOnly: true);
                updateResidentialDemandJob.m_Populations = SystemAPI.GetComponentLookup<Population>(isReadOnly: true);
                updateResidentialDemandJob.m_SpawnableDatas = SystemAPI.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
                updateResidentialDemandJob.m_ZonePropertyDatas = SystemAPI.GetComponentLookup<ZonePropertiesData>(isReadOnly: true);
                updateResidentialDemandJob.m_Populations = SystemAPI.GetComponentLookup<Population>(isReadOnly: true);
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
                
                updateResidentialDemandJob.m_Results = m_Results;
                UpdateResidentialDemandJob jobData = updateResidentialDemandJob;
                IJobExtensions.Schedule(jobData, JobUtils.CombineDependencies(base.Dependency, m_ReadDependencies, outJobHandle, /*outJobHandle2, outJobHandle3,*/ deps)).Complete();
                
            }
            // Update UI
           
        }

        private void ResetResults()
        {
            m_Results.Fill<int>(0);
        }

    }
}

        
  
    
