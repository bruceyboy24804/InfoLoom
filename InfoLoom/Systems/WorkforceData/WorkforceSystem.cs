using System.Runtime.CompilerServices;
using Colossal;
using Colossal.Collections;
using Game;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Objects;
using Game.Simulation;
using Game.Tools;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using InfoLoomTwo.Domain;

namespace InfoLoomTwo.Systems.WorkforceData
{
    public partial class WorkforceSystem : GameSystemBase
    {
        private const int EDUCATION_LEVELS = 5;
        private const int TOTAL_INDEX = 5;
        private const int RESULTS_SIZE = 6; // 5 education levels + 1 totals
        private const int UPDATE_INTERVAL = 512;

        private struct CountEmploymentJob : IJobChunk
        {
            [ReadOnly] public ComponentTypeHandle<Citizen> m_CitizenType;
            [ReadOnly] public ComponentTypeHandle<Worker> m_WorkerType;
            [ReadOnly] public ComponentTypeHandle<Game.Citizens.Student> m_StudentType;
            [ReadOnly] public ComponentTypeHandle<HealthProblem> m_HealthProblemType;
            [ReadOnly] public ComponentTypeHandle<HouseholdMember> m_HouseholdMemberType;
            [ReadOnly] public ComponentLookup<OutsideConnection> m_OutsideConnections;
            [ReadOnly] public ComponentLookup<PropertyRenter> m_PropertyRenters;
            [ReadOnly] public ComponentLookup<HomelessHousehold> m_HomelessHouseholds;
            [ReadOnly] public ComponentLookup<MovingAway> m_MovingAways;
            [ReadOnly] public ComponentLookup<Household> m_Households;

            public NativeArray<WorkforcesInfo> m_Results;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var householdMemberArray = chunk.GetNativeArray(ref m_HouseholdMemberType);
                var citizenArray = chunk.GetNativeArray(ref m_CitizenType);
                var workerArray = chunk.GetNativeArray(ref m_WorkerType);
                var healthProblemArray = chunk.GetNativeArray(ref m_HealthProblemType);
                
                bool isWorkerChunk = workerArray.IsCreated;
                bool hasHealthProblems = healthProblemArray.IsCreated;

                for (int i = 0; i < chunk.Count; i++)
                {
                    var citizen = citizenArray[i];
                    var age = citizen.GetAge();
                    
                    // Skip children and seniors
                    if (age == CitizenAge.Child || age == CitizenAge.Elderly)
                        continue;
                    
                    var household = householdMemberArray[i].m_Household;
                    
                    // Skip invalid citizens
                    if (ShouldSkipCitizen(citizen, household, hasHealthProblems ? healthProblemArray[i] : default, hasHealthProblems))
                        continue;
                    
                    // Process valid citizen
                    ProcessCitizen(citizen, household, isWorkerChunk ? workerArray[i] : default, isWorkerChunk);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool ShouldSkipCitizen(Citizen citizen, Entity household, HealthProblem healthProblem, bool hasHealthProblems)
            {
                return (hasHealthProblems && CitizenUtils.IsDead(healthProblem)) ||
                       (citizen.m_State & (CitizenFlags.Tourist | CitizenFlags.Commuter)) != 0 ||
                       !m_Households.HasComponent(household) ||
                       (m_Households[household].m_Flags & HouseholdFlags.MovedIn) == 0 ||
                       m_MovingAways.HasComponent(household);
            }

            private void ProcessCitizen(Citizen citizen, Entity household, Worker worker, bool isWorker)
            {
                int educationLevel = citizen.GetEducationLevel();
                var info = m_Results[educationLevel];
                
                info.Total++;
                
                if (isWorker)
                {
                    info.Worker++;
                    ProcessWorker(ref info, worker, educationLevel);
                }
                else
                {
                    info.Unemployed++;
                }
                
                // Check if homeless
                if (m_HomelessHouseholds.HasComponent(household) || !m_PropertyRenters.HasComponent(household))
                {
                    info.Homeless++;
                }
                
                m_Results[educationLevel] = info;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void ProcessWorker(ref WorkforcesInfo info, Worker worker, int educationLevel)
            {
                bool isEmployable = false;
                
                if (m_OutsideConnections.HasComponent(worker.m_Workplace))
                {
                    info.Outside++;
                    isEmployable = true;
                }
                
                if (worker.m_Level < educationLevel)
                {
                    info.Under++;
                    isEmployable = true;
                    
                    
                }
                
                if (isEmployable)
                {
                    info.Employable++;
                }
            }

            void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
            }
        }

        private SimulationSystem m_SimulationSystem;
        private EntityQuery m_AllAdultGroup;
        
        // InfoLoom results
        public NativeArray<WorkforcesInfo> m_Results;
        
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
            
            m_AllAdultGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<Citizen>() },
                None = new ComponentType[]
                {
                    ComponentType.ReadOnly<Game.Citizens.Student>(),
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>()
                }
            });
            
            m_Results = new NativeArray<WorkforcesInfo>(RESULTS_SIZE, Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            if (m_Results.IsCreated)
                m_Results.Dispose();
            
            base.OnDestroy();
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return UPDATE_INTERVAL;
        }

        protected override void OnUpdate()
        {
            if (!IsPanelVisible)
                return;
            
            ForceUpdate = false;
            
            ResetResults();
            
            var jobData = new CountEmploymentJob
            {
                m_CitizenType = SystemAPI.GetComponentTypeHandle<Citizen>(isReadOnly: true),
                m_HealthProblemType = SystemAPI.GetComponentTypeHandle<HealthProblem>(isReadOnly: true),
                m_WorkerType = SystemAPI.GetComponentTypeHandle<Worker>(isReadOnly: true),
                m_StudentType = SystemAPI.GetComponentTypeHandle<Game.Citizens.Student>(isReadOnly: true),
                m_HouseholdMemberType = SystemAPI.GetComponentTypeHandle<HouseholdMember>(isReadOnly: true),
                m_MovingAways = SystemAPI.GetComponentLookup<MovingAway>(isReadOnly: true),
                m_PropertyRenters = SystemAPI.GetComponentLookup<PropertyRenter>(isReadOnly: true),
                m_HomelessHouseholds = SystemAPI.GetComponentLookup<HomelessHousehold>(isReadOnly: true),
                m_OutsideConnections = SystemAPI.GetComponentLookup<OutsideConnection>(isReadOnly: true),
                m_Households = SystemAPI.GetComponentLookup<Household>(isReadOnly: true),
                m_Results = m_Results
            };
            
            JobChunkExtensions.Schedule(jobData, m_AllAdultGroup, base.Dependency).Complete();
            
            CalculateTotals();
        }

        private void ResetResults()
        {
            for (int i = 0; i < RESULTS_SIZE; i++)
            {
                m_Results[i] = new WorkforcesInfo(i);
            }
        }

        private void CalculateTotals()
        {
            var totals = new WorkforcesInfo(-1);
            
            for (int i = 0; i < EDUCATION_LEVELS; i++)
            {
                var levelData = m_Results[i];
                totals.Total += levelData.Total;
                totals.Worker += levelData.Worker;
                totals.Unemployed += levelData.Unemployed;
                totals.Homeless += levelData.Homeless;
                totals.Employable += levelData.Employable;
                totals.Outside += levelData.Outside;
                totals.Under += levelData.Under;
            }
            
            m_Results[TOTAL_INDEX] = totals;
        }
    }
}