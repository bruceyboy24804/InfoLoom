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

public partial class Demographics : SystemBase
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

        //[ReadOnly]
        //public BufferTypeHandle<HouseholdCitizen> m_HouseholdCitizenHandle;

        //[ReadOnly]
        //public ComponentTypeHandle<Household> m_HouseholdType;

        [ReadOnly]
        public ComponentTypeHandle<HealthProblem> m_HealthProblemType;

        [ReadOnly]
        public ComponentTypeHandle<HouseholdMember> m_HouseholdMemberType;

        //[ReadOnly]
        //public ComponentLookup<Worker> m_WorkerFromEntity;

        //[ReadOnly]
        //public ComponentLookup<HealthProblem> m_HealthProblems;

        [ReadOnly]
        public ComponentLookup<MovingAway> m_MovingAways;

        //[ReadOnly]
        //public ComponentLookup<Citizen> m_Citizens;

        //[ReadOnly]
        //public ComponentLookup<Student> m_Students;

        [ReadOnly]
        public ComponentLookup<Household> m_Households;

        [ReadOnly]
        public ComponentLookup<HomelessHousehold> m_HomelessHouseholds;

        [ReadOnly]
        public ComponentLookup<PropertyRenter> m_PropertyRenters;

        public TimeData m_TimeData;

       

        //public uint m_UpdateFrameIndex;

        public uint m_SimulationFrame;

        public NativeArray<int> m_Totals;

        public NativeArray<Domain.PopulationAtAgeInfo> m_Results;
            public int day;

        


        // this job is based on AgingJob
        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            //PopulationStructureUISystem.LogChunk(chunk);
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
            //Plugin.Log($"day {day} chunk: {entities.Length} entities, {citizens.Length} citizens, {isStudent} {students.Length} students, {isWorker} {workers.Length} workers");

            for (int i = 0; i < citizenArray.Length; i++)
            {
                Entity entity = entities[i];
                //Plugin.Log($"{entity}");
                //List<ComponentType> list = PopulationStructureUISystem.ListEntityComponents();
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

                // are components in sync with flags?
                //if (isTourist || m_TouristHouseholds.HasComponent(household))
                //Plugin.Log($"{entity.Index}: tourist {isTourist} {isTouristHousehold} {m_Households[household].m_Flags}");
                //if (isCommuter || m_CommuterHouseholds.HasComponent(household))
                //Plugin.Log($"{entity.Index}: commuter {isCommuter} {isCommuterHousehold} {m_Households[household].m_Flags}");
                // Infixo: notes for the future
                // Tourists: citizen flag is always set, component sometimes exists, sometimes not
                //           most of them don't have MovedIn flag set, just Tourist flag in household
                //           usually Tourist household flag is correlated with TouristHousehold component, but NOT always
                //           MovedIn tourists DON'T have TouristHousehold component - why??? where do they stay?
                //           tl;dr CitizenFlags.Tourist is the only reliable way
                // Commuters: very similar logic, CitizenFlag is always SET
                //            CommuterHousehold component is present when household flag is Commuter
                //                                        is not present when flag is MovedIn

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
                    //Plugin.Log($"Warning: unknown citizen {citizen.m_State} household {m_Households[household].m_Flags}");
                    continue;
                }

                // homeless citizens, already MovingAway are not included (they are gone, anyway)
                if (m_HomelessHouseholds.HasComponent(household) || !m_PropertyRenters.HasComponent(household))
                {
                    m_Totals[9]++; // homeless
                }

                // Get age using the game's built-in methods
                CitizenAge age = value.GetAge();
                int ageInDays = (int)Math.Min(day - value.m_BirthDay, 120);
                // Ensure ageInDays is non-negative and within the bounds of m_Results
                if (ageInDays >= 0 && ageInDays < m_Results.Length)
                {
                    // Retrieve the struct from the array
                    Domain.PopulationAtAgeInfo info = m_Results[ageInDays];

                    // Modify the fields
                    info.Age = ageInDays;
                    info.Total++;

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

                    if (isStudent)
                    {
                        m_Totals[4]++; // Increment total students
                        switch (studentArray[i].m_Level)
                        {
                            case 1: info.School1++; break;
                            case 2: info.School2++; break;
                            case 3: info.School3++; break;
                            case 4: info.School4++; break;
                        }
                    }

                    if (isWorker && ageInDays >= 21)
                    {
                        m_Totals[5]++; // Increment total workers
                        info.Work++;
                    }

                    if (!isStudent && !isWorker)
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

    

    


    //private CitySystem m_CitySystem;

    private SimulationSystem m_SimulationSystem;

    //private RawValueBinding m_uiTotals;

    

   

    private EntityQuery m_TimeDataQuery;

    //private EntityQuery m_HouseholdQuery;

    private EntityQuery m_CitizenQuery;

    //private EntityQuery m_WorkProviderQuery;

    //private EntityQuery m_WorkProviderModifiedQuery;

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
    // 9 - homeless citizens

    public NativeArray<Domain.PopulationAtAgeInfo> m_Results; // final results, will be filled via jobs and then written as output

    
    public int m_AgeCap;
    // 240209 Set gameMode to avoid errors in the Editor
   
    public bool IsPanelVisible { get; set; }
    public bool ForceUpdate { get; private set; }
    public void ForceUpdateOnce()
    {
        ForceUpdate = true;
    }
    //[Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        
       
        

        //m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
        m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
        m_TimeDataQuery = GetEntityQuery(ComponentType.ReadOnly<TimeData>());

        m_CitizenQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[1] { ComponentType.ReadOnly<Citizen>() },
            None = new ComponentType[2] { ComponentType.ReadOnly<Deleted>(), ComponentType.ReadOnly<Temp>() }
        });
        RequireForUpdate(m_CitizenQuery);

        

        

        // TEST
        

        // allocate memory for results
        m_Totals = new NativeArray<int>(10, Allocator.Persistent);
        m_Results = new NativeArray<Domain.PopulationAtAgeInfo>(120, Allocator.Persistent); // INFIXO: TODO
        
    }

    //[Preserve]
    protected override void OnDestroy()
    {
        m_Totals.Dispose();
        m_Results.Dispose();
        base.OnDestroy();
    }

    protected override void OnUpdate()
    {
        if (!IsPanelVisible)
                return;
            if (m_SimulationSystem.frameIndex % 256 != 44 && !ForceUpdate)
                return;
            ForceUpdate = false;
       

        //Plugin.Log($"OnUpdate at frame {m_SimulationSystem.frameIndex}");
        



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

        //int ageInDays = m_Results[2];
        //int num2 = m_Results[1];
        //int num3 = m_Results[4];
        //int newValue = m_Results[5];
        //int num4 = num2 + ageInDays - m_Results[6];
        //float newValue2 = (((float)num4 > 0f) ? ((float)(num4 - num3) / (float)num4 * 100f) : 0f);
        //m_Jobs.Update(newValue);
        //m_Employed.Update(num3);
        //m_Unemployment.Update(newValue2);
        //Population componentData = base.EntityManager.GetComponentData<Population>(m_CitySystem.City);
        //m_Population.Update(componentData.m_Population);

        /* DEBUG
        Plugin.Log($"results: {m_Totals[0]} {m_Totals[1]} {m_Totals[2]} {m_Totals[3]} students {m_Totals[4]} workers {m_Totals[5]}");
        for (int i = 0; i < m_Results.Length; i++)
        {
            PopulationAtAgeInfo info = m_Results[i];
            Plugin.Log($"...[{i}]: {info.Age} {info.Total} students {info.School1} {info.School2} {info.School3} {info.School4} workers {info.Work} other {info.Other}");
        }
        */

        
        


        
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
        //Plugin.Log("reset",true);
    }

   
    
}
}