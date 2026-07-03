using System.Runtime.CompilerServices;
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
using InfoLoomTwo.Domain;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Student = Game.Citizens.Student;
using ModsCommon.Extensions;
using ModsCommon.Systems;

namespace InfoLoomTwo.Systems.WorkforceData
{
    public partial class WorkforceSystem: CommonUISystemBase
    {
        protected override string ModId => InfoLoomMod.Instance.Id;

        private const int EDUCATION_LEVELS = 5;
        private const int TOTAL_INDEX = 5;
        private const int RESULTS_SIZE = 6; // 5 education levels + 1 totals
        private const int UPDATE_INTERVAL = 512;

        

        private SimulationSystem m_SimulationSystem;
        private CountHouseholdDataSystem m_CountHouseholdDataSystem;
        private EntityQuery m_AllAdultGroup;
        public Entity SelectedDistrict { get; set; } = Entity.Null;

        private EntityQuery m_DistrictQuery;

        private NameSystem m_NameSystem;

        // InfoLoom results
        public NativeArray<WorkforcesInfo> m_Results;
        private ValueBindingHelper<WorkforcesInfo[]> m_WorkforceInfoBinding;

        public bool IsPanelVisible { get; set; }
        public bool ForceUpdate { get; private set; }

        public void ForceUpdateOnce()
        {
            ForceUpdate = true;
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
            m_CountHouseholdDataSystem = World.GetOrCreateSystemManaged<CountHouseholdDataSystem>();
            m_NameSystem = World.GetOrCreateSystemManaged<NameSystem>();
            m_DistrictQuery = GetEntityQuery(ComponentType.ReadOnly<District>());
            m_AllAdultGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<Citizen>(),
                    ComponentType.ReadOnly<HouseholdMember>()
                },
                None = new[]
                {
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>()
                }
            });

            m_Results = new NativeArray<WorkforcesInfo>(RESULTS_SIZE, Allocator.Persistent);
            m_WorkforceInfoBinding = CreateBinding("WorkforceData", new WorkforcesInfo[0]);
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
                m_CitizenType = SystemAPI.GetComponentTypeHandle<Citizen>(true),
                m_HouseholdMemberType = SystemAPI.GetComponentTypeHandle<HouseholdMember>(true),
                m_Workers = SystemAPI.GetComponentLookup<Worker>(true),
                m_MovingAways = SystemAPI.GetComponentLookup<MovingAway>(true),
                m_PropertyRenters = SystemAPI.GetComponentLookup<PropertyRenter>(true),
                m_HomelessHouseholds = SystemAPI.GetComponentLookup<HomelessHousehold>(true),
                m_OutsideConnections = SystemAPI.GetComponentLookup<OutsideConnection>(true),
                m_Households = SystemAPI.GetComponentLookup<Household>(true),
                m_CurrentDistrictLookup = SystemAPI.GetComponentLookup<CurrentDistrict>(true),
                m_Students = SystemAPI.GetComponentLookup<Student>(true),
                m_Citizens = SystemAPI.GetComponentLookup<Citizen>(true),
                m_HealthProblems = SystemAPI.GetComponentLookup<HealthProblem>(true),
                m_SelectedDistrict = SelectedDistrict,
                m_Results = m_Results
            };
            jobData.ScheduleParallel(m_AllAdultGroup, Dependency).Complete();
            CalculateTotals();
        }

        protected override void OnUpdate()
        {
            if (!IsPanelVisible)
                return;


            ResetResults();

            var jobData = new CountEmploymentJob
            {
                m_EntityType = SystemAPI.GetEntityTypeHandle(),
                m_CitizenType = SystemAPI.GetComponentTypeHandle<Citizen>(true),
                m_HouseholdMemberType = SystemAPI.GetComponentTypeHandle<HouseholdMember>(true),
                m_Workers = SystemAPI.GetComponentLookup<Worker>(true),
                m_MovingAways = SystemAPI.GetComponentLookup<MovingAway>(true),
                m_PropertyRenters = SystemAPI.GetComponentLookup<PropertyRenter>(true),
                m_HomelessHouseholds = SystemAPI.GetComponentLookup<HomelessHousehold>(true),
                m_OutsideConnections = SystemAPI.GetComponentLookup<OutsideConnection>(true),
                m_Households = SystemAPI.GetComponentLookup<Household>(true),
                m_CurrentDistrictLookup = SystemAPI.GetComponentLookup<CurrentDistrict>(true),
                m_Students = SystemAPI.GetComponentLookup<Student>(true),
                m_Citizens = SystemAPI.GetComponentLookup<Citizen>(true),
                m_HealthProblems = SystemAPI.GetComponentLookup<HealthProblem>(true),
                m_SelectedDistrict = SelectedDistrict,
                m_Results = m_Results
            };

            jobData.ScheduleParallel(m_AllAdultGroup, Dependency).Complete();

            CalculateTotals();
        }

        
        private void ResetResults()
        {
            for (var i = 0; i < RESULTS_SIZE; i++) m_Results[i] = new WorkforcesInfo(i);
        }

        private void CalculateTotals()
        {
            var totals = new WorkforcesInfo(-1);

            for (var level = EducationLevel.Uneducated; level <= EducationLevel.HighlyEducated; level++)
            {
                var levelData = m_Results[(int)level];

                // Match vanilla's exact unemployment calculation:
                // Unemployed = WorkableCitizen - CityWorkers
                // where CityWorkers excludes outside workers
                if (levelData.Total > 0)
                {
                    // CityWorkers = all workers minus those working outside
                    var cityWorkers = levelData.Worker - levelData.Outside;
                    // Unemployed count should match vanilla's formula exactly
                    var unemployedCount = math.max(0, levelData.Total - cityWorkers);
                    levelData.UnemploymentRate = unemployedCount / (float)levelData.Total * 100f;
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
                var cityWorkers = totals.Worker - totals.Outside;
                var unemployedCount = math.max(0, totals.Total - cityWorkers);
                totals.UnemploymentRate = unemployedCount / (float)totals.Total * 100f;
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
