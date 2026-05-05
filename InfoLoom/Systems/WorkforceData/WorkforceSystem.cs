using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal;
using Colossal.Collections;
using Game;
using Game.Agents;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Objects;
using Game.Simulation;
using Game.Tools;
using Game.UI;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using InfoLoomTwo.Domain;
using InfoLoomTwo.Domain.DataDomain;
using Unity.Mathematics;

namespace InfoLoomTwo.Systems.WorkforceData
{
    public partial class WorkforceSystem : GameSystemBase
    {
        private const int EDUCATION_LEVELS = 5;
        private const int TOTAL_INDEX = 5;
        private const int RESULTS_SIZE = 6; // 5 education levels + 1 totals
        private const int UPDATE_INTERVAL = 512;
        private enum EducationLevel
        {
            Uneducated = 0,
            PoorlyEducated = 1,
            Educated = 2,
            WellEducated = 3,
            HighlyEducated = 4,
            Totals = 5
        }
        private struct CountEmploymentJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle m_EntityType;
            [ReadOnly] public ComponentTypeHandle<Citizen> m_CitizenType;
            [ReadOnly] public ComponentTypeHandle<HouseholdMember> m_HouseholdMemberType;
            [ReadOnly] public ComponentLookup<Worker> m_Workers;
            [ReadOnly] public ComponentLookup<OutsideConnection> m_OutsideConnections;
            [ReadOnly] public ComponentLookup<PropertyRenter> m_PropertyRenters;
            [ReadOnly] public ComponentLookup<HomelessHousehold> m_HomelessHouseholds;
            [ReadOnly] public ComponentLookup<MovingAway> m_MovingAways;
            [ReadOnly] public ComponentLookup<Household> m_Households;
            [ReadOnly] public ComponentLookup<CurrentDistrict> m_CurrentDistrictLookup;
            [ReadOnly] public ComponentLookup<Game.Citizens.Student> m_Students;
            [ReadOnly] public ComponentLookup<Citizen> m_Citizens;
            [ReadOnly] public ComponentLookup<HealthProblem> m_HealthProblems;
            public NativeArray<WorkforcesInfo> m_Results;
            public Entity m_SelectedDistrict; 

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entityArray = chunk.GetNativeArray(m_EntityType);
                var householdMemberArray = chunk.GetNativeArray(ref m_HouseholdMemberType);
                var citizenArray = chunk.GetNativeArray(ref m_CitizenType);

                for (int i = 0; i < chunk.Count; i++)
                {
                    Entity citizenEntity = entityArray[i];
                    Entity household = householdMemberArray[i].m_Household;

                    if (m_SelectedDistrict != Entity.Null && !IsInSelectedDistrict(household))
                        continue;

                    Citizen citizen = citizenArray[i];
                    
                    // Use vanilla's IsWorkableCitizen check
                    if (!CitizenUtils.IsWorkableCitizen(citizenEntity, ref m_Citizens, ref m_Students, ref m_HealthProblems))
                        continue;
                        
                    if (ShouldSkipCitizen(citizenEntity, citizen, household))
                        continue;
                    
                    // Check if this citizen is a worker (per-citizen, not per-chunk)
                    bool hasWorker = m_Workers.HasComponent(citizenEntity);
                    Worker worker = hasWorker ? m_Workers[citizenEntity] : default;
                        
                    ProcessCitizen(citizen, household, worker, hasWorker);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool ShouldSkipCitizen(Entity citizenEntity, Citizen citizen, Entity household)
            {
                // Check if dead using ComponentLookup (per-citizen check)
                if (CitizenUtils.IsDead(citizenEntity, ref m_HealthProblems))
                    return true;
                    
                return (citizen.m_State & (CitizenFlags.Tourist | CitizenFlags.Commuter)) != 0 ||
                       !m_Households.HasComponent(household) ||
                       (m_Households[household].m_Flags & HouseholdFlags.MovedIn) == 0 ||
                       m_MovingAways.HasComponent(household);
            }

            private void ProcessCitizen(Citizen citizen, Entity household, Worker worker, bool isWorker)
            {
                int educationLevel = citizen.GetEducationLevel();
                var info = m_Results[educationLevel];
                
                info.Total++;
                
                // Match vanilla logic: check worker component first
                bool hasWorker = isWorker;
                
                if (hasWorker)
                {
                    info.Worker++;
                     bool isWorkingOutside = m_OutsideConnections.HasComponent(worker.m_Workplace);
                    bool isUnderemployed = worker.m_Level < educationLevel;
                    
                    if (isWorkingOutside)
                    {
                        info.Outside++;
                    }
                    
                    if (isUnderemployed)
                    {
                        info.Under++;
                    }
                    
                    // Count as employable if working outside OR underemployed
                    if (isWorkingOutside || isUnderemployed)
                    {
                        info.Employable++;
                    }
                }
                else
                {
                    // Don't increment Unemployed here - it's calculated in CalculateTotals
                    // to match vanilla's formula: Unemployed = Total - CityWorkers
                    // Unemployed are also employable
                    info.Employable++;
                }
                
                // Check if homeless
                if (m_HomelessHouseholds.HasComponent(household) || !m_PropertyRenters.HasComponent(household))
                {
                    info.Homeless++;
                }
                
                m_Results[educationLevel] = info;
            }

            private bool IsInSelectedDistrict(Entity household)
            {
                // If no district selected, include all households (even those without property)
                if (m_SelectedDistrict == Entity.Null)
                    return true;

                // For district filtering, exclude households without property (can't determine their district)
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
            void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
            }
        }

        private SimulationSystem m_SimulationSystem;
        private Game.Simulation.CountHouseholdDataSystem m_CountHouseholdDataSystem;
        private EntityQuery m_AllAdultGroup;
        public Entity SelectedDistrict { get; set; } = Entity.Null;

        private EntityQuery m_DistrictQuery;
        private NameSystem m_NameSystem;
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
            m_CountHouseholdDataSystem = base.World.GetOrCreateSystemManaged<Game.Simulation.CountHouseholdDataSystem>();
            m_NameSystem = base.World.GetOrCreateSystemManaged<NameSystem>();
            m_DistrictQuery = GetEntityQuery(ComponentType.ReadOnly<District>());
            m_AllAdultGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<Citizen>(),
                    ComponentType.ReadOnly<HouseholdMember>()
                },
                None = new ComponentType[]
                {
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

        /// <summary>Runs the workforce calculation immediately on the calling thread. Used by the exporter.</summary>
        public void RecalculateNow()
        {
            ForceUpdate = false;
            ResetResults();
            var jobData = new CountEmploymentJob
            {
                m_EntityType = SystemAPI.GetEntityTypeHandle(),
                m_CitizenType = SystemAPI.GetComponentTypeHandle<Citizen>(isReadOnly: true),
                m_HouseholdMemberType = SystemAPI.GetComponentTypeHandle<HouseholdMember>(isReadOnly: true),
                m_Workers = SystemAPI.GetComponentLookup<Worker>(isReadOnly: true),
                m_MovingAways = SystemAPI.GetComponentLookup<MovingAway>(isReadOnly: true),
                m_PropertyRenters = SystemAPI.GetComponentLookup<PropertyRenter>(isReadOnly: true),
                m_HomelessHouseholds = SystemAPI.GetComponentLookup<HomelessHousehold>(isReadOnly: true),
                m_OutsideConnections = SystemAPI.GetComponentLookup<OutsideConnection>(isReadOnly: true),
                m_Households = SystemAPI.GetComponentLookup<Household>(isReadOnly: true),
                m_CurrentDistrictLookup = SystemAPI.GetComponentLookup<CurrentDistrict>(isReadOnly: true),
                m_Students = SystemAPI.GetComponentLookup<Game.Citizens.Student>(isReadOnly: true),
                m_Citizens = SystemAPI.GetComponentLookup<Citizen>(isReadOnly: true),
                m_HealthProblems = SystemAPI.GetComponentLookup<HealthProblem>(isReadOnly: true),
                m_SelectedDistrict = SelectedDistrict,
                m_Results = m_Results
            };
            JobChunkExtensions.Schedule(jobData, m_AllAdultGroup, base.Dependency).Complete();
            CalculateTotals();
        }

        protected override void OnUpdate()
        {
            if (!IsPanelVisible)
                return;
            
            ForceUpdate = false;
            
            ResetResults();
            
            var jobData = new CountEmploymentJob
            {
                m_EntityType = SystemAPI.GetEntityTypeHandle(),
                m_CitizenType = SystemAPI.GetComponentTypeHandle<Citizen>(isReadOnly: true),
                m_HouseholdMemberType = SystemAPI.GetComponentTypeHandle<HouseholdMember>(isReadOnly: true),
                m_Workers = SystemAPI.GetComponentLookup<Worker>(isReadOnly: true),
                m_MovingAways = SystemAPI.GetComponentLookup<MovingAway>(isReadOnly: true),
                m_PropertyRenters = SystemAPI.GetComponentLookup<PropertyRenter>(isReadOnly: true),
                m_HomelessHouseholds = SystemAPI.GetComponentLookup<HomelessHousehold>(isReadOnly: true),
                m_OutsideConnections = SystemAPI.GetComponentLookup<OutsideConnection>(isReadOnly: true),
                m_Households = SystemAPI.GetComponentLookup<Household>(isReadOnly: true),
                m_CurrentDistrictLookup = SystemAPI.GetComponentLookup<CurrentDistrict>(isReadOnly: true),
                m_Students = SystemAPI.GetComponentLookup<Game.Citizens.Student>(isReadOnly: true),
                m_Citizens = SystemAPI.GetComponentLookup<Citizen>(isReadOnly: true),
                m_HealthProblems = SystemAPI.GetComponentLookup<HealthProblem>(isReadOnly: true),
                m_SelectedDistrict = SelectedDistrict,
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

            for (EducationLevel level = EducationLevel.Uneducated; level <= EducationLevel.HighlyEducated; level++)
            {
                var levelData = m_Results[(int)level];
                
                // Match vanilla's exact unemployment calculation:
                // Unemployed = WorkableCitizen - CityWorkers
                // where CityWorkers excludes outside workers
                if (levelData.Total > 0)
                {
                    // CityWorkers = all workers minus those working outside
                    int cityWorkers = levelData.Worker - levelData.Outside;
                    // Unemployed count should match vanilla's formula exactly
                    int unemployedCount = math.max(0, levelData.Total - cityWorkers);
                    levelData.UnemploymentRate = (float)unemployedCount / (float)levelData.Total * 100f;
                    // Update the Unemployed field to match the calculated value
                    levelData.Unemployed = unemployedCount;
                    m_Results[(int)level] = levelData;
                }
                
                totals.Total += levelData.Total;
                totals.Worker += levelData.Worker;
                totals.Unemployed += levelData.Unemployed;
                totals.UnemploymentRate += levelData.UnemploymentRate;
                totals.Homeless += levelData.Homeless;
                totals.Employable += levelData.Employable;
                totals.Outside += levelData.Outside;
                totals.Under += levelData.Under;
            }

            // Calculate total unemployment rate using vanilla's exact formula
            if (totals.Total > 0)
            {
                int cityWorkers = totals.Worker - totals.Outside;
                int unemployedCount = math.max(0, totals.Total - cityWorkers);
                totals.UnemploymentRate = (float)unemployedCount / (float)totals.Total * 100f;
                totals.Unemployed = unemployedCount;
            }

            m_Results[(int)EducationLevel.Totals] = totals;
        }
        public void SetSelectedDistrict(Entity district)
        {
            SelectedDistrict = district;
        }
    }
}