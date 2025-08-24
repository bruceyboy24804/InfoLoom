﻿﻿﻿using System.Runtime.CompilerServices;
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
        private enum Totals
        {
            AllCitizens, // 0 - num citizens in the city 0 = 1+2+3
            Locals, // 1 - num locals
            Tourists, // 2 - num tourists
            Commuters, // 3 - num commuters
            Students, // 4 - num students (in locals) 4 <= 1
            Workers, // 5 - num workers (in locals) 5 <= 1
            OldestCitizenAge, // 6 - oldest cim
            MovingAways, // 7 - moving aways
            DeadCitizens, // 8 - dead cims
            HomelessCitizens // 9 - homeless citizens (will now be set from CountHouseholdDataSystem)
        }
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
                    Citizen value = citizenArray[i];  
                    Entity household = householdMemberArray[i].m_Household;
                    if (!m_Households.HasComponent(household) || m_Households[household].m_Flags == HouseholdFlags.None)
                    {
                        continue;
                    }
                    if (!IsInSelectedDistrict(entity, household))
                        continue;
                    if (isHealthProblem && CitizenUtils.IsDead(healthProblemArray[i]))
                    {
                        m_Totals[(int)Totals.DeadCitizens]++; 
                        continue;
                    }
                    bool isCommuter = ((value.m_State & CitizenFlags.Commuter) != CitizenFlags.None);
                    bool isTourist = ((value.m_State & CitizenFlags.Tourist) != CitizenFlags.None);
                    bool isMovedIn = ((m_Households[household].m_Flags & HouseholdFlags.MovedIn) != HouseholdFlags.None);
                    m_Totals[(int)Totals.AllCitizens]++; 
                    if (isTourist) m_Totals[(int)Totals.Tourists]++; 
                    else if (isCommuter) m_Totals[(int)Totals.Commuters]++; 
                    if (isTourist || isCommuter)
                        continue; 
                    
                    if (m_MovingAways.HasComponent(household))
                    {
                        m_Totals[(int)Totals.MovingAways]++; 
                        continue;
                    }
                    if (isMovedIn) m_Totals[(int)Totals.Locals]++; 
                    else
                    {
                        continue;
                    }
                    int ageInDays = (int)Math.Min(day - value.m_BirthDay, 120);
                    CitizenAge age = value.GetAge();
                    if (ageInDays >= 0 && ageInDays < m_Results.Length)
                    {
                        PopulationAtAgeInfo info = m_Results[ageInDays];
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
                                info.Retired++;
                                break;
                        }
                        switch (value.GetEducationLevel())
                        {
                            case 0:
                                info.Uneducated++;
                                break;
                            case 1:
                                info.PoorlyEducated++;
                                break;
                            case 2:
                                info.Educated++;
                                break;
                            case 3:
                                info.WellEducated++;
                                break;
                            case 4:
                                info.HighlyEducated++;
                                break;
                        }
                        
                        if (isStudent)
                        {
                            for (int j = 0; j < studentArray.Length; j++)
                            {
                                // Find matching entity in student array
                                if (entities[i].Index == entities[j].Index)
                                {
                                    m_Totals[(int)Totals.Students]++;
                                    byte level = studentArray[j].m_Level;
                                    switch (level)
                                    {
                                        case 1: info.School1++; 
                                            
                                            break;
                                        case 2: info.School2++; break;
                                        case 3: info.School3++; break;
                                        case 4: info.School4++; break;
                                    }
                                    break;
                                }
                            }
                        }
                        bool isCitizenWorker = false;
                        if (isWorker)
                        {
                            for (int j = 0; j < workerArray.Length; j++)
                            {
                                isCitizenWorker = true;
                                m_Totals[(int)Totals.Workers]++;
                                info.Work++;
                                break;
                            }
                        }
                        if (!isCitizenWorker)
                        {
                            info.Unemployed++;
                        }
                        
                        
                        
                        if (ageInDays > m_Totals[(int)Totals.OldestCitizenAge])
                            m_Totals[(int)Totals.OldestCitizenAge] = ageInDays;
                        m_Results[ageInDays] = info;
                        continue; 
                    }
                }
            }
            private bool IsInSelectedDistrict(Entity citizenEntity, Entity household)
            {
                if (m_SelectedDistrict == Entity.Null)
                    return true;
                if (!m_PropertyRenters.HasComponent(household))
                    return false;
                var propertyRenter = m_PropertyRenters[household];
                Entity buildingEntity = propertyRenter.m_Property;
                if (buildingEntity == Entity.Null)
                    return false;
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
        public NativeArray<int> m_Totals; 
        public NativeArray<PopulationAtAgeInfo> m_Results; 

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
            m_Totals[(int)Totals.HomelessCitizens] = m_CountHouseholdDataSystem.HomelessCitizenCount;
           
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
        
        public void SetSelectedDistrict(Entity district)
        {
            SelectedDistrict = district;
        }
        
    }
}    
    