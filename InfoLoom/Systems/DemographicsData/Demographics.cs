using System.Runtime.CompilerServices;
using Colossal.UI.Binding;
using Game.Agents;
using Game.Citizens;
using Game.Common;
using Game.Simulation;
using Game.Tools;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System;
using Game.Buildings;
using Game;
using InfoLoomTwo.Extensions;
using Unity.Burst;

namespace InfoLoomTwo.Systems.DemographicsData.Demographics
{
    // This System is based on PopulationInfoviewUISystem by CO

    public partial class Demographics : GameSystemBase
    {
        /// <summary>
        /// Holds info about population at Age
        /// </summary>

        [BurstCompile]
        private struct PopulationStructureJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;

            [ReadOnly]
            public ComponentTypeHandle<Citizen> m_CitizenType;

            [ReadOnly]
            public ComponentTypeHandle<Game.Citizens.Student> m_StudentType;

            [ReadOnly]
            public ComponentTypeHandle<Worker> m_WorkerType;

            [ReadOnly]
            public ComponentTypeHandle<HealthProblem> m_HealthProblemType;

            [ReadOnly]
            public ComponentTypeHandle<HouseholdMember> m_HouseholdMemberType;

            [ReadOnly]
            public ComponentLookup<MovingAway> m_MovingAways;

            [ReadOnly]
            public ComponentLookup<Household> m_Households;

            [ReadOnly]
            public ComponentLookup<HomelessHousehold> m_HomelessHouseholds;

            [ReadOnly]
            public ComponentLookup<PropertyRenter> m_PropertyRenters;

            public TimeData m_TimeData;

            public uint m_SimulationFrame;

            public NativeArray<int> m_Totals;

            public NativeArray<Domain.PopulationAtAgeInfo> m_Results;
            public int day;

            // this job is based on AgingJob
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                // access data in the chunk
                NativeArray<Entity> entities = chunk.GetNativeArray(m_EntityType);
                NativeArray<Citizen> citizenArray = chunk.GetNativeArray(ref m_CitizenType);
                NativeArray<Game.Citizens.Student> studentArray = chunk.GetNativeArray(ref m_StudentType);
                NativeArray<Worker> workerArray = chunk.GetNativeArray(ref m_WorkerType);
                NativeArray<HealthProblem> healthProblemArray = chunk.GetNativeArray(ref m_HealthProblemType);
                NativeArray<HouseholdMember> householdMemberArray = chunk.GetNativeArray(ref m_HouseholdMemberType);
                bool isStudent = chunk.Has(ref m_StudentType); // are there students in this chunk?
                bool isWorker = chunk.Has(ref m_WorkerType); // are there workers in this chunk?
                bool isHealthProblem = chunk.Has(ref m_HealthProblemType); // for checking dead cims

                for (int i = 0; i < citizenArray.Length; i++)
                {
                    Entity entity = entities[i];
                    Citizen value = citizenArray[i];  // Changed to value
                    Entity household = householdMemberArray[i].m_Household;

                    // skip: non-existing households (technical), and with flag not set
                    if (!m_Households.HasComponent(household) || m_Households[household].m_Flags == HouseholdFlags.None)
                    {
                        continue;
                    }

                    // skip but count dead citizens
                    if (isHealthProblem && CitizenUtils.IsDead(healthProblemArray[i]))
                    {
                        m_Totals[8]++; // dead
                        continue;
                    }

                    // citizen data
                    bool isCommuter = ((value.m_State & CitizenFlags.Commuter) != CitizenFlags.None);
                    bool isTourist = ((value.m_State & CitizenFlags.Tourist) != CitizenFlags.None);
                    bool isMovedIn = ((m_Households[household].m_Flags & HouseholdFlags.MovedIn) != HouseholdFlags.None);

                    // count All, Tourists and Commuters
                    m_Totals[0]++; // all
                    if (isTourist) m_Totals[2]++; // tourists
                    else if (isCommuter) m_Totals[3]++; // commuters
                    if (isTourist || isCommuter)
                        continue; // not local, go for the next

                    // skip but count citizens moving away
                    // 231230 moved after Tourist & Commuter check, so it will show only Locals that are moving away (more important info)
                    if (m_MovingAways.HasComponent(household))
                    {
                        m_Totals[7]++; // moving aways
                        continue;
                    }

                    // finally, count local population
                    if (isMovedIn) m_Totals[1]++; // locals; game Population is: MovedIn, not Tourist & Commuter, not dead
                    else
                    {
                        // skip glitches e.g. there is a case with Tourist household, but citizen is NOT Tourist
                        continue;
                    }

                    // Get age using the game's built-in methods
                    CitizenAge age = value.GetAge();
                    // Use the game's built-in GetAgeInDays method instead of custom calculation
                    int ageInDays = (int)Math.Min(day - value.m_BirthDay, 120);
                    // Ensure ageInDays is non-negative and within the bounds of m_Results
                    if (ageInDays >= 0 && ageInDays < m_Results.Length)
                    {
                        // Retrieve the struct from the array
                        Domain.PopulationAtAgeInfo info = m_Results[ageInDays];

                        // Modify the fields
                        info.Age = ageInDays;
                        info.Total++;
                        if (ageInDays <= 20)
                        {
                            info.ChildCount++; // Count in child demographic
                            info.School1++;    // All are in elementary school
                            m_Totals[4]++;     // Increment total students count
                            
                            // Don't process any other categories for this age range
                            if (ageInDays > m_Totals[6])
                                m_Totals[6] = ageInDays;
                            m_Results[ageInDays] = info;
                            continue; // Skip all other processing for ages 0-20
                        }
                        switch (age)
                        {
                            case CitizenAge.Child:
                                info.ChildCount++;
                                break;
                            case CitizenAge.Teen:
                                info.TeenCount++;
                                break;
                            case CitizenAge.Adult:
                                info.AdultCount++;
                                break;
                            case CitizenAge.Elderly:
                                info.ElderlyCount++;
                                break;
                        }

                        bool isActuallyStudent = false;
                        if (isStudent && chunk.Has(ref m_StudentType))
                        {
                            for (int j = 0; j < studentArray.Length; j++)
                            {
                                // Find matching entity in student array
                                if (entities[i].Index == entities[j].Index)
                                {
                                    m_Totals[4]++;
                                    isActuallyStudent = true;
                                    byte level = studentArray[j].m_Level;
                                    switch (level)
                                    {
                                        case 1: info.School1++; break;
                                        case 2: info.School2++; break;
                                        case 3: info.School3++; break;
                                        case 4: info.School4++; break;
                                    }
                                    break;
                                }
                            }
                        }

                        bool isActuallyWorker = false;
                        if (isWorker && chunk.Has(ref m_WorkerType))
                        {
                            for (int j = 0; j < workerArray.Length; j++)
                            {
                                // Find matching entity in worker array
                                if (entities[i].Index == entities[j].Index)
                                {
                                    m_Totals[5]++;
                                    info.Work++;
                                    isActuallyWorker = true;
                                    break;
                                }
                            }
                        }

                        if (!isActuallyStudent && !isActuallyWorker)
                        {
                            info.Other++;
                        }
                        if (ageInDays > m_Totals[6])
                            m_Totals[6] = ageInDays;
                        // **Reassign the modified struct back to the array** 
                        m_Results[ageInDays] = info;
                    }
                }
            }
        }

        private SimulationSystem m_SimulationSystem;
        private CountHouseholdDataSystem m_CountHouseholdDataSystem; // Add reference to CountHouseholdDataSystem
        private EntityQuery m_TimeDataQuery;
        private EntityQuery m_CitizenQuery;

        public NativeArray<int> m_Totals; // final results, totals at city level
        // 0 - num citizens in the city 0 = 1+2+3
        // 1 - num locals
        // 2 - num tourists
        // 3 - num commuters
        // 4 - num students (in locals) 4 <= 1
        // 5 - num workers (in locals) 5 <= 1
        // 6 - oldest cim
        // 7 - moving aways
        // 8 - dead cims
        // 9 - homeless citizens (will now be set from CountHouseholdDataSystem)

        public NativeArray<Domain.PopulationAtAgeInfo> m_Results; // final results, will be filled via jobs and then written as output

        public int m_AgeCap;
        
        public bool IsPanelVisible { get; set; }
        public bool ForceUpdate { get; private set; }
        public void ForceUpdateOnce()
        {
            ForceUpdate = true;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            
            m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
            m_CountHouseholdDataSystem = base.World.GetOrCreateSystemManaged<CountHouseholdDataSystem>(); // Get the system
            
            m_TimeDataQuery = GetEntityQuery(ComponentType.ReadOnly<TimeData>());

            m_CitizenQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[1] { ComponentType.ReadOnly<Citizen>() },
                None = new ComponentType[2] { ComponentType.ReadOnly<Deleted>(), ComponentType.ReadOnly<Temp>() }
            });
            RequireForUpdate(m_CitizenQuery);

            // allocate memory for results
            m_Totals = new NativeArray<int>(10, Allocator.Persistent);
            m_Results = new NativeArray<Domain.PopulationAtAgeInfo>(120, Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            m_Totals.Dispose();
            m_Results.Dispose();
            base.OnDestroy();
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 256;
        }

        protected override void OnUpdate()
        {
            if (!IsPanelVisible)
                return;
                
            ForceUpdate = false;

            Setting setting = Mod.setting;
            
            ResetResults();
            
            PopulationStructureJob structureJob = default(PopulationStructureJob);
            structureJob.m_EntityType = SystemAPI.GetEntityTypeHandle();
            structureJob.m_CitizenType = SystemAPI.GetComponentTypeHandle<Game.Citizens.Citizen>(isReadOnly: true);
            structureJob.m_StudentType = SystemAPI.GetComponentTypeHandle<Game.Citizens.Student>(isReadOnly: true);
            structureJob.m_WorkerType = SystemAPI.GetComponentTypeHandle<Game.Citizens.Worker>(isReadOnly: true);
            structureJob.m_MovingAways = SystemAPI.GetComponentLookup<MovingAway>(isReadOnly: true);
            structureJob.m_HealthProblemType = SystemAPI.GetComponentTypeHandle<HealthProblem>(isReadOnly: true);
            structureJob.m_HouseholdMemberType = SystemAPI.GetComponentTypeHandle<HouseholdMember>(isReadOnly: true);
            structureJob.m_Households = SystemAPI.GetComponentLookup<Household>(isReadOnly: true);
            structureJob.m_PropertyRenters = SystemAPI.GetComponentLookup<PropertyRenter>(isReadOnly: true);
            structureJob.m_HomelessHouseholds = SystemAPI.GetComponentLookup<HomelessHousehold>(isReadOnly: true);
            structureJob.m_SimulationFrame = m_SimulationSystem.frameIndex;
            structureJob.m_TimeData = m_TimeDataQuery.GetSingleton<TimeData>();
            structureJob.day = TimeSystem.GetDay(m_SimulationSystem.frameIndex, m_TimeDataQuery.GetSingleton<Game.Common.TimeData>());

            structureJob.m_Totals = m_Totals;
            structureJob.m_Results = m_Results;
            JobChunkExtensions.Schedule(structureJob, m_CitizenQuery, base.Dependency).Complete();

            // Get homeless count from CountHouseholdDataSystem instead of calculating in the job
            m_Totals[9] = m_CountHouseholdDataSystem.HomelessCitizenCount;
        }

        private void ResetResults()
        {
            for (int i = 0; i < m_Totals.Length; i++)
            {
                m_Totals[i] = 0;
            }
            for (int i = 0; i < m_Results.Length; i++)
            {
                m_Results[i] = new Domain.PopulationAtAgeInfo(i);
            }
        }
    }
}
