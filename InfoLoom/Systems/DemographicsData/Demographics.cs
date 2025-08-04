﻿﻿using System.Runtime.CompilerServices;
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
using Game.Areas;
using Game.UI;
using Game.UI.InGame;
using InfoLoomTwo.Domain.DataDomain;
using InfoLoomTwo.Extensions;
using Unity.Burst;
using Purpose = Colossal.Serialization.Entities.Purpose;

namespace InfoLoomTwo.Systems.DemographicsData
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

            [ReadOnly] public ComponentLookup<PropertyRenter> m_PropertyRenters;
            [ReadOnly] public ComponentLookup<CurrentDistrict> m_CurrentDistrictLookup; 

            public TimeData m_TimeData;

            public uint m_SimulationFrame;

            public NativeArray<int> m_Totals;

            public NativeArray<PopulationAtAgeInfo> m_Results;
            public int day;
            public Entity m_SelectedDistrict; 

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
                    if (!IsInSelectedDistrict(entity, household))
                        continue;
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
                    if (isMovedIn) m_Totals[1]++; 
                    else
                    {
                        continue;
                    }

                    int ageInDays = (int)Math.Min(day - value.m_BirthDay, 120);
                    CitizenAge age = CitizenAge.Adult; // Default value
                    if (ageInDays < AgingSystem.GetTeenAgeLimitInDays())
                        age = CitizenAge.Child;
                    else if (ageInDays < AgingSystem.GetAdultAgeLimitInDays())
                        age = CitizenAge.Teen;
                    else if (ageInDays < AgingSystem.GetElderAgeLimitInDays())
                        age = CitizenAge.Adult;
                    else
                        age = CitizenAge.Elderly;
                    // Use the game's built-in GetAgeInDays method instead of custom calculation
                   
                    // Ensure ageInDays is non-negative and within the bounds of m_Results
                    if (ageInDays >= 0 && ageInDays < m_Results.Length)
                    {
                        // Retrieve the struct from the array
                        PopulationAtAgeInfo info = m_Results[ageInDays];
                        info.Age = ageInDays;
                        info.Total++;
                        if (ageInDays <= 20)
                        {
                            info.ChildCount++; // Count in child demographic
                            
                            // Allow assignment to School2 instead of forcing all to School1
                            // Students will be assigned based on their actual assigned school level
                            // which will be determined by the game mechanics
                            if (isStudent && chunk.Has(ref m_StudentType))
                            {
                                for (int j = 0; j < studentArray.Length; j++)
                                {
                                    // Find matching entity in student array
                                    if (entities[i].Index == entities[j].Index)
                                    {
                                        byte level = studentArray[j].m_Level;
                                        switch (level)
                                        {
                                            case 1: info.School1++; break;
                                            case 2: info.School2++; break;
                                            default: info.School1++; break; // Default to School1 if unexpected level
                                        }
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                // If not a student or student data not accessible, default to School1
                                info.School1++;
                            }
                            
                            m_Totals[4]++; // Increment total students count
                            
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
                            // Check if citizen is elderly to categorize as retired, otherwise unemployed
                            if (age == CitizenAge.Elderly)
                            {
                                info.Retired++;
                                // You could also store the occupation type here if needed
                            }
                            else
                            {
                                info.Unemployed++;
                            }
                        }
                        if (ageInDays > m_Totals[6])
                            m_Totals[6] = ageInDays;
                        // **Reassign the modified struct back to the array** 
                        m_Results[ageInDays] = info;
                    }
                }
            }
            private bool IsInSelectedDistrict(Entity citizenEntity, Entity household)
            {
                // If no district selected, show entire city
                if (m_SelectedDistrict == Entity.Null)
                    return true;

                // Get the household's property
                if (!m_PropertyRenters.HasComponent(household))
                    return false;

                var propertyRenter = m_PropertyRenters[household];
                Entity buildingEntity = propertyRenter.m_Property;
                
                if (buildingEntity == Entity.Null)
                    return false;

                // Check if the building has a district component
                if (m_CurrentDistrictLookup.HasComponent(buildingEntity))
                {
                    var currentDistrict = m_CurrentDistrictLookup[buildingEntity];
                    return currentDistrict.m_District == m_SelectedDistrict;
                }

                return false;
            }
        }
        
        private SimulationSystem m_SimulationSystem;
        private CountHouseholdDataSystem m_CountHouseholdDataSystem; // Add reference to CountHouseholdDataSystem
        private EntityQuery m_TimeDataQuery;
        private EntityQuery m_CitizenQuery;
        public Entity SelectedDistrict { get; set; } = Entity.Null;
        private EntityQuery m_DistrictQuery;
        private NameSystem m_NameSystem;
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

        public NativeArray<PopulationAtAgeInfo> m_Results; // final results, will be filled via jobs and then written as output

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
            m_NameSystem = base.World.GetOrCreateSystemManaged<NameSystem>(); // Add this
            
            m_TimeDataQuery = GetEntityQuery(ComponentType.ReadOnly<TimeData>());

            m_CitizenQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[1] { ComponentType.ReadOnly<Citizen>() },
                None = new ComponentType[2] { ComponentType.ReadOnly<Deleted>(), ComponentType.ReadOnly<Temp>() }
            });
            m_DistrictQuery = GetEntityQuery(ComponentType.ReadOnly<District>()); // Add this

            RequireForUpdate(m_CitizenQuery);

            // allocate memory for results
            m_Totals = new NativeArray<int>(10, Allocator.Persistent);
            m_Results = new NativeArray<PopulationAtAgeInfo>(120, Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            m_Totals.Dispose();
            m_Results.Dispose();
            base.OnDestroy();
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

            //Setting setting = Mod.setting;
            
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
            structureJob.m_CurrentDistrictLookup = SystemAPI.GetComponentLookup<CurrentDistrict>(isReadOnly: true); // Add this

            structureJob.m_SimulationFrame = m_SimulationSystem.frameIndex;
            structureJob.m_TimeData = m_TimeDataQuery.GetSingleton<TimeData>();
            structureJob.day = TimeSystem.GetDay(m_SimulationSystem.frameIndex, m_TimeDataQuery.GetSingleton<Game.Common.TimeData>());
            structureJob.m_SelectedDistrict = SelectedDistrict; // Add this

            structureJob.m_Totals = m_Totals;
            structureJob.m_Results = m_Results;
            JobChunkExtensions.Schedule(structureJob, m_CitizenQuery, base.Dependency).Complete();

            // Get homeless count from CountHouseholdDataSystem instead of calculating in the job
            m_Totals[9] = m_CountHouseholdDataSystem.HomelessCitizenCount;
            UpdateStrategy(GroupingStrategy.None);
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            UpdateStrategy(GroupingStrategy.None);
        }

        private void ResetResults()
        {
            for (int i = 0; i < m_Totals.Length; i++)
            {
                m_Totals[i] = 0;
            }
            for (int i = 0; i < m_Results.Length; i++)
            {
                m_Results[i] = new PopulationAtAgeInfo(i);
            }
        }
        public class PopulationGroupData
        {
            public string Label { get; set; }
            public int StartAge { get; set; }
            public int EndAge { get; set; }
            public int Total { get; set; }
            public int ChildCount { get; set; }
            public int TeenCount { get; set; }
            public int AdultCount { get; set; }
            public int ElderlyCount { get; set; }
            public int School1 { get; set; }
            public int School2 { get; set; }
            public int School3 { get; set; }
            public int School4 { get; set; }
            public int Work { get; set; }
            public int Unemployed { get; set; }
            public int Retired { get; set; }

            public PopulationGroupData()
            {
            }
        }

        public List<PopulationGroupData> GetPopulationByAgeGroups(GroupingStrategy strategy)
        {
            List<PopulationGroupData> groups = new List<PopulationGroupData>();
            int maxAge = (int)m_Totals[6]; // Use the oldest citizen age as boundary
            maxAge = Math.Min(maxAge, m_Results.Length - 1); // Ensure we don't go beyond array bounds
            
            switch (strategy)
            {
                case GroupingStrategy.None:
                    // Return each individual age as its own group
                    for (int i = 0; i <= maxAge; i++)
                    {
                        var ageInfo = m_Results[i];
                        if (ageInfo.Total > 0)
                        {
                            groups.Add(new PopulationGroupData
                            {
                                Label = i.ToString(),
                                StartAge = i,
                                EndAge = i,
                                Total = ageInfo.Total,
                                ChildCount = ageInfo.ChildCount,
                                TeenCount = ageInfo.TeenCount,
                                AdultCount = ageInfo.AdultCount,
                                ElderlyCount = ageInfo.ElderlyCount,
                                School1 = ageInfo.School1,
                                School2 = ageInfo.School2,
                                School3 = ageInfo.School3,
                                School4 = ageInfo.School4,
                                Work = ageInfo.Work,
                                Unemployed = ageInfo.Unemployed,
                                Retired = ageInfo.Retired
                            });
                        }
                    }
                    break;

                case GroupingStrategy.FiveYear:
                    // Group in 5-year intervals (0-4, 5-9, etc.)
                    for (int i = 0; i <= maxAge; i += 5)
                    {
                        int endAge = Math.Min(i + 4, maxAge);
                        var group = CreateAgeGroup(i, endAge, $"{i}-{endAge}");
                        groups.Add(group);
                    }
                    break;

                case GroupingStrategy.TenYear:
                    // Group in 10-year intervals (0-9, 10-19, etc.)
                    for (int i = 0; i <= maxAge; i += 10)
                    {
                        int endAge = Math.Min(i + 9, maxAge);
                        var group = CreateAgeGroup(i, endAge, $"{i}-{endAge}");
                        groups.Add(group);
                    }
                    break;

                case GroupingStrategy.LifeCycle:
                    // Group by life stages using game's age limits
                    int childLimit = AgingSystem.GetTeenAgeLimitInDays() - 1;
                    int teenLimit = AgingSystem.GetAdultAgeLimitInDays() - 1;
                    int adultLimit = AgingSystem.GetElderAgeLimitInDays() - 1;
                    
                    groups.Add(CreateAgeGroup(0, childLimit, "Children"));
                    groups.Add(CreateAgeGroup(childLimit + 1, teenLimit, "Teens"));
                    groups.Add(CreateAgeGroup(teenLimit + 1, adultLimit, "Adults"));
                    groups.Add(CreateAgeGroup(adultLimit + 1, maxAge, "Elderly"));
                    break;
            }
            
            return groups;
        }

        private PopulationGroupData CreateAgeGroup(int startAge, int endAge, string label)
        {
            var group = new PopulationGroupData
            {
                Label = label,
                StartAge = startAge,
                EndAge = endAge,
                Total = 0,
                ChildCount = 0,
                TeenCount = 0,
                AdultCount = 0,
                ElderlyCount = 0,
                School1 = 0,
                School2 = 0,
                School3 = 0,
                School4 = 0,
                Work = 0,
                Unemployed = 0,
                Retired = 0
            };

            // Sum the values for all ages in the range
            for (int age = startAge; age <= endAge && age < m_Results.Length; age++)
            {
                var ageInfo = m_Results[age];
                group.Total += ageInfo.Total;
                group.ChildCount += ageInfo.ChildCount;
                group.TeenCount += ageInfo.TeenCount;
                group.AdultCount += ageInfo.AdultCount;
                group.ElderlyCount += ageInfo.ElderlyCount;
                group.School1 += ageInfo.School1;
                group.School2 += ageInfo.School2;
                group.School3 += ageInfo.School3;
                group.School4 += ageInfo.School4;
                group.Work += ageInfo.Work;
                group.Unemployed += ageInfo.Unemployed;
                group.Retired += ageInfo.Retired;
            }
            
            return group;
        }
        public void UpdateStrategy(GroupingStrategy strategy)
        {
            try 
            {
                GetPopulationByAgeGroups(strategy);
            }
            catch (Exception ex)
            {
                //Mod.log.Error($"Error updating strategy: {ex.Message}");
            };
            
        }
        public void SetSelectedDistrict(Entity district)
        {
            SelectedDistrict = district;
        }
        
    }
}    
    