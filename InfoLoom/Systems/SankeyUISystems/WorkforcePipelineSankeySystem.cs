using Colossal.UI.Binding;
using Game;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.UI;
using InfoLoomTwo.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

namespace InfoLoomTwo.Systems.SankeyUISystems
{
    /// <summary>
    /// Provides Sankey diagram data for the Workforce & Education Pipeline panel.
    ///
    /// Workforce view (viewMode=false):
    ///   ageEdu[ageGroup * 5 + eduLevel]           — Demographics → Education       (4×5 = 20)
    ///   eduNonWork[eduLevel * 4 + status]         — Education → non-work status    (5×4 = 20)
    ///     status: 0=NotInSchool  1=School  2=Unemployed  3=Retired
    ///   eduWorkSector[eduLevel * 5 + sector]      — Education → sector (workers)   (5×5 = 25)
    ///     sector: 0=Commercial 1=Industrial 2=Office 3=CityService 4=OutsideCity
    ///   eduWorkJobEdu[eduLevel * 5 + jobEdu]      — Education → job edu (workers)  (5×5 = 25)
    ///
    /// Workplace view (viewMode=true) — 4-column:
    ///   livingWorkerEdu[living * 5 + workerEdu]    — Living Place   → Worker Edu   (2×5 = 10)
    ///   workerEduJobEdu[workerEdu * 5 + jobEdu]    — Worker Edu     → Job Edu      (5×5 = 25)
    ///   jobEduSector[jobEdu * 5 + sector]          — Job Edu Needed → Sector       (5×5 = 25)
    ///   living: 0=OutsideCity 1=WithinCity
    ///   sector: 0=Commercial 1=Industrial 2=Office 3=CityService 4=OutsideCity
    /// </summary>
    public partial class WorkforcePipelineSankeySystem : ExtendedUISystemBase
    {
        private const string kGroup   = "InfoLoomTwo";
        private const string kOpenKey = "WorkforcePipelineSankeyOpen";

        private const int AGE_GROUPS      = 4;
        private const int EDU_LEVELS      = 5;
        private const int NON_WORK_COUNT  = 4;
        private const int SECTOR_COUNT    = 5;
        private const int LIVING_COUNT    = 2;

        private ValueBinding<bool> m_OpenBinding;
        private ValueBinding<bool> m_ViewModeBinding; // false=workforce, true=workplace
        private RawValueBinding    m_DataBinding;

        private EntityQuery m_CitizenQuery;
        private EntityQuery m_WorkerQuery;

        // Tracks whether a job was scheduled and data is ready to be pushed
        private bool m_JobScheduled;
        private bool m_ViewModeWhenScheduled;

        // Workforce view arrays
        public NativeArray<int> m_AgeEduCounts;       // [ageGroup * 5 + eduLevel]      20
        public NativeArray<int> m_EduNonWorkCounts;   // [eduLevel * 4 + status]        20
        public NativeArray<int> m_EduWorkSectorCounts;// [eduLevel * 5 + sector]        25
        public NativeArray<int> m_EduWorkJobEduCounts;// [eduLevel * 5 + jobEdu]        25
        // Workplace view arrays
        public NativeArray<int> m_LivingWorkerEdu;    // [living * 5 + workerEdu]       10
        public NativeArray<int> m_WorkerEduJobEdu;    // [workerEdu * 5 + jobEdu]       25
        public NativeArray<int> m_JobEduSector;       // [jobEdu * 5 + sector]          25

        // ── Burst job — workforce view ─────────────────────────────────────────
        [BurstCompile]
        private partial struct WorkforcePipelineJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<Worker>                          m_Workers;
            [ReadOnly] public ComponentLookup<Game.Citizens.Student>           m_Students;
            [ReadOnly] public ComponentLookup<HealthProblem>                   m_HealthProblems;
            [ReadOnly] public ComponentLookup<MovingAway>                      m_MovingAways;
            [ReadOnly] public ComponentLookup<Household>                       m_Households;
            [ReadOnly] public ComponentLookup<Game.Objects.OutsideConnection>  m_OutsideConnections;
            [ReadOnly] public ComponentLookup<CommercialCompany>               m_CommercialCompanies;
            [ReadOnly] public ComponentLookup<IndustrialCompany>               m_IndustrialCompanies;
            [ReadOnly] public ComponentLookup<PrefabRef>                       m_PrefabRefs;
            [ReadOnly] public ComponentLookup<IndustrialProcessData>           m_IndustrialProcessDatas;

            public NativeArray<int> m_AgeEduCounts;         // [age*5+edu]
            public NativeArray<int> m_EduNonWorkCounts;     // [edu*4+status]
            public NativeArray<int> m_EduWorkSectorCounts;  // [edu*5+sector]
            public NativeArray<int> m_EduWorkJobEduCounts;  // [edu*5+jobEdu]

            private void Execute(Entity entity, in Citizen citizen, in HouseholdMember member)
            {
                Entity household = member.m_Household;

                if (!m_Households.TryGetComponent(household, out Household hh)) return;
                if ((hh.m_Flags & HouseholdFlags.MovedIn) == 0) return;
                if ((citizen.m_State & (CitizenFlags.Tourist | CitizenFlags.Commuter)) != 0) return;
                if (m_MovingAways.HasComponent(household)) return;
                if (m_HealthProblems.TryGetComponent(entity, out HealthProblem hp) && CitizenUtils.IsDead(hp)) return;

                CitizenAge ageGroup = citizen.GetAge();
                int        eduLevel = citizen.GetEducationLevel();
                int        ageIdx   = (int)ageGroup;

                m_AgeEduCounts[ageIdx * EDU_LEVELS + eduLevel]++;

                if (m_Workers.TryGetComponent(entity, out Worker worker))
                {
                    Entity workplace = worker.m_Workplace;
                    if (workplace == Entity.Null) return;

                    // Sector
                    int sector;
                    if (m_OutsideConnections.HasComponent(workplace))
                        sector = 4; // Outside City
                    else if (m_CommercialCompanies.HasComponent(workplace))
                        sector = 0; // Commercial
                    else if (m_IndustrialCompanies.HasComponent(workplace))
                    {
                        if (m_PrefabRefs.TryGetComponent(workplace, out PrefabRef prefabRef) &&
                            m_IndustrialProcessDatas.TryGetComponent(prefabRef.m_Prefab, out IndustrialProcessData process))
                        {
                            Resource output = process.m_Output.m_Resource;
                            sector = (output & (Resource.Software | Resource.Telecom | Resource.Financial | Resource.Media)) != Resource.NoResource
                                ? 2 : 1;
                        }
                        else
                            sector = 1; // Industrial
                    }
                    else
                        sector = 3; // City Service

                    m_EduWorkSectorCounts[eduLevel * SECTOR_COUNT + sector]++;

                    // Job edu level
                    int jobEdu = worker.m_Level;
                    if (jobEdu < 0 || jobEdu >= EDU_LEVELS) jobEdu = 0;
                    m_EduWorkJobEduCounts[eduLevel * EDU_LEVELS + jobEdu]++;
                }
                else if (m_Students.HasComponent(entity))
                {
                    m_EduNonWorkCounts[eduLevel * NON_WORK_COUNT + 1]++; // School
                }
                else
                {
                    int nonWorkIdx = ageGroup switch
                    {
                        CitizenAge.Elderly => 3,  // Retired
                        CitizenAge.Child   => 0,  // Not in School
                        CitizenAge.Teen    => 0,  // Not in School
                        _                  => 2,  // Unemployed
                    };
                    m_EduNonWorkCounts[eduLevel * NON_WORK_COUNT + nonWorkIdx]++;
                }
            }
        }

        // ── Workplace view — iterates all workers (including commuters) ───────
        [BurstCompile]
        private partial struct WorkplaceSankeyJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<Household>             m_Households;
            [ReadOnly] public ComponentLookup<MovingAway>            m_MovingAways;
            [ReadOnly] public ComponentLookup<HealthProblem>         m_HealthProblems;
            [ReadOnly] public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;
            [ReadOnly] public ComponentLookup<CommercialCompany>     m_CommercialCompanies;
            [ReadOnly] public ComponentLookup<IndustrialCompany>     m_IndustrialCompanies;
            [ReadOnly] public ComponentLookup<PrefabRef>             m_PrefabRefs;
            [ReadOnly] public ComponentLookup<IndustrialProcessData> m_IndustrialProcessDatas;

            public NativeArray<int> m_LivingWorkerEdu;  // [living * 5 + workerEdu]
            public NativeArray<int> m_WorkerEduJobEdu;  // [workerEdu * 5 + jobEdu]
            public NativeArray<int> m_JobEduSector;     // [jobEdu * 5 + sector]

            private void Execute(Entity entity, in Citizen citizen, in HouseholdMember member, in Worker worker)
            {
                Entity household = member.m_Household;

                if (!m_Households.TryGetComponent(household, out Household hh)) return;
                bool isCommuter = (citizen.m_State & CitizenFlags.Commuter) != 0;
                // Non-commuters must be MovedIn; skip tourists
                if (!isCommuter)
                {
                    if ((hh.m_Flags & HouseholdFlags.MovedIn) == 0) return;
                    if ((citizen.m_State & CitizenFlags.Tourist) != 0) return;
                }
                if (m_MovingAways.HasComponent(household)) return;
                if (m_HealthProblems.TryGetComponent(entity, out HealthProblem hp) && CitizenUtils.IsDead(hp)) return;

                Entity workplace = worker.m_Workplace;
                if (workplace == Entity.Null) return;

                int workerEdu = citizen.GetEducationLevel();
                int jobEdu    = worker.m_Level;
                if (workerEdu < 0 || workerEdu >= EDU_LEVELS) workerEdu = 0;
                if (jobEdu < 0 || jobEdu >= EDU_LEVELS) jobEdu = 0;

                // Living place
                int living = isCommuter ? 0 : 1; // 0=Outside City, 1=Within City

                // Sector
                int sector;
                if (m_OutsideConnections.HasComponent(workplace))
                    sector = 4; // Outside City
                else if (m_CommercialCompanies.HasComponent(workplace))
                    sector = 0; // Commercial
                else if (m_IndustrialCompanies.HasComponent(workplace))
                {
                    // Check for office
                    if (m_PrefabRefs.TryGetComponent(workplace, out PrefabRef prefabRef) &&
                        m_IndustrialProcessDatas.TryGetComponent(prefabRef.m_Prefab, out IndustrialProcessData process))
                    {
                        Resource output = process.m_Output.m_Resource;
                        sector = (output & (Resource.Software | Resource.Telecom | Resource.Financial | Resource.Media)) != Resource.NoResource
                            ? 2   // Office
                            : 1;  // Industrial
                    }
                    else
                        sector = 1; // Industrial
                }
                else
                    sector = 3; // City Service

                m_LivingWorkerEdu[living * EDU_LEVELS + workerEdu]++;
                m_WorkerEduJobEdu[workerEdu * EDU_LEVELS + jobEdu]++;
                m_JobEduSector[jobEdu * SECTOR_COUNT + sector]++;
            }
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────
        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();

            m_AgeEduCounts        = new NativeArray<int>(AGE_GROUPS * EDU_LEVELS,   Allocator.Persistent);
            m_EduNonWorkCounts    = new NativeArray<int>(EDU_LEVELS * NON_WORK_COUNT, Allocator.Persistent);
            m_EduWorkSectorCounts = new NativeArray<int>(EDU_LEVELS * SECTOR_COUNT, Allocator.Persistent);
            m_EduWorkJobEduCounts = new NativeArray<int>(EDU_LEVELS * EDU_LEVELS,   Allocator.Persistent);
            m_LivingWorkerEdu     = new NativeArray<int>(LIVING_COUNT * EDU_LEVELS, Allocator.Persistent);
            m_WorkerEduJobEdu     = new NativeArray<int>(EDU_LEVELS * EDU_LEVELS,   Allocator.Persistent);
            m_JobEduSector        = new NativeArray<int>(EDU_LEVELS * SECTOR_COUNT, Allocator.Persistent);

            m_CitizenQuery = SystemAPI.QueryBuilder()
                .WithAll<Citizen, HouseholdMember>()
                .WithNone<Deleted, Temp>()
                .Build();

            m_WorkerQuery = SystemAPI.QueryBuilder()
                .WithAll<Citizen, HouseholdMember, Worker>()
                .WithNone<Deleted, Temp>()
                .Build();

            // Do not RequireForUpdate here — OnUpdate decides which query is active.

            m_OpenBinding     = new ValueBinding<bool>(kGroup, kOpenKey, false);
            m_ViewModeBinding = new ValueBinding<bool>(kGroup, "WorkforcePipelineViewMode", false);
            AddBinding(m_OpenBinding);
            AddBinding(new TriggerBinding<bool>(kGroup, kOpenKey, SetOpen));
            AddBinding(m_ViewModeBinding);
            AddBinding(new TriggerBinding<bool>(kGroup, "WorkforcePipelineViewMode", SetViewMode));
            AddBinding(m_DataBinding = new RawValueBinding(kGroup, "workforcePipelineData", WriteData));
        }

        [Preserve]
        protected override void OnDestroy()
        {
            if (m_AgeEduCounts.IsCreated)        m_AgeEduCounts.Dispose();
            if (m_EduNonWorkCounts.IsCreated)    m_EduNonWorkCounts.Dispose();
            if (m_EduWorkSectorCounts.IsCreated) m_EduWorkSectorCounts.Dispose();
            if (m_EduWorkJobEduCounts.IsCreated) m_EduWorkJobEduCounts.Dispose();
            if (m_LivingWorkerEdu.IsCreated)     m_LivingWorkerEdu.Dispose();
            if (m_WorkerEduJobEdu.IsCreated)     m_WorkerEduJobEdu.Dispose();
            if (m_JobEduSector.IsCreated)        m_JobEduSector.Dispose();
            base.OnDestroy();
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 512;

        [Preserve]
        protected override void OnUpdate()
        {
            if (!m_OpenBinding.value)
            {
                // Panel closed — reset state so next open starts fresh
                m_JobScheduled = false;
                return;
            }

            bool isWorkplaceMode = m_ViewModeBinding.value;

            // ── Step 1: Complete any in-flight job and push its results ──────────
            // After 512 ticks the worker threads have been idle; Complete() is near-instant.
            if (m_JobScheduled)
            {
                CompleteDependency();
                // Only push data if the view mode hasn't changed since the job was scheduled.
                // If it changed, the arrays contain stale-mode data — skip the UI push.
                if (m_ViewModeWhenScheduled == isWorkplaceMode)
                    m_DataBinding.Update();
                m_JobScheduled = false;
            }

            // ── Step 2: Guard — skip if the active query has no entities ─────────
            bool activeQueryEmpty = isWorkplaceMode
                ? m_WorkerQuery.IsEmptyIgnoreFilter
                : m_CitizenQuery.IsEmptyIgnoreFilter;

            if (activeQueryEmpty)
                return;

            // ── Step 3: Zero output arrays ────────────────────────────────────────
            if (!isWorkplaceMode)
            {
                for (int i = 0; i < m_AgeEduCounts.Length;        i++) m_AgeEduCounts[i]        = 0;
                for (int i = 0; i < m_EduNonWorkCounts.Length;    i++) m_EduNonWorkCounts[i]    = 0;
                for (int i = 0; i < m_EduWorkSectorCounts.Length; i++) m_EduWorkSectorCounts[i] = 0;
                for (int i = 0; i < m_EduWorkJobEduCounts.Length; i++) m_EduWorkJobEduCounts[i] = 0;

                var job = new WorkforcePipelineJob
                {
                    m_Workers              = SystemAPI.GetComponentLookup<Worker>(true),
                    m_Students             = SystemAPI.GetComponentLookup<Game.Citizens.Student>(true),
                    m_HealthProblems       = SystemAPI.GetComponentLookup<HealthProblem>(true),
                    m_MovingAways          = SystemAPI.GetComponentLookup<MovingAway>(true),
                    m_Households           = SystemAPI.GetComponentLookup<Household>(true),
                    m_OutsideConnections   = SystemAPI.GetComponentLookup<Game.Objects.OutsideConnection>(true),
                    m_CommercialCompanies  = SystemAPI.GetComponentLookup<CommercialCompany>(true),
                    m_IndustrialCompanies  = SystemAPI.GetComponentLookup<IndustrialCompany>(true),
                    m_PrefabRefs           = SystemAPI.GetComponentLookup<PrefabRef>(true),
                    m_IndustrialProcessDatas = SystemAPI.GetComponentLookup<IndustrialProcessData>(true),
                    m_AgeEduCounts         = m_AgeEduCounts,
                    m_EduNonWorkCounts     = m_EduNonWorkCounts,
                    m_EduWorkSectorCounts  = m_EduWorkSectorCounts,
                    m_EduWorkJobEduCounts  = m_EduWorkJobEduCounts,
                };
                // Schedule non-blocking: job runs on worker threads across the next 512 ticks.
                Dependency = job.Schedule(m_CitizenQuery, Dependency);
            }
            else
            {
                for (int i = 0; i < m_LivingWorkerEdu.Length; i++) m_LivingWorkerEdu[i] = 0;
                for (int i = 0; i < m_WorkerEduJobEdu.Length; i++) m_WorkerEduJobEdu[i] = 0;
                for (int i = 0; i < m_JobEduSector.Length;    i++) m_JobEduSector[i]    = 0;

                var wpJob = new WorkplaceSankeyJob
                {
                    m_Households           = SystemAPI.GetComponentLookup<Household>(true),
                    m_MovingAways          = SystemAPI.GetComponentLookup<MovingAway>(true),
                    m_HealthProblems       = SystemAPI.GetComponentLookup<HealthProblem>(true),
                    m_OutsideConnections   = SystemAPI.GetComponentLookup<Game.Objects.OutsideConnection>(true),
                    m_CommercialCompanies  = SystemAPI.GetComponentLookup<CommercialCompany>(true),
                    m_IndustrialCompanies  = SystemAPI.GetComponentLookup<IndustrialCompany>(true),
                    m_PrefabRefs           = SystemAPI.GetComponentLookup<PrefabRef>(true),
                    m_IndustrialProcessDatas = SystemAPI.GetComponentLookup<IndustrialProcessData>(true),
                    m_LivingWorkerEdu      = m_LivingWorkerEdu,
                    m_WorkerEduJobEdu      = m_WorkerEduJobEdu,
                    m_JobEduSector         = m_JobEduSector,
                };
                // Schedule non-blocking: job runs on worker threads across the next 512 ticks.
                Dependency = wpJob.Schedule(m_WorkerQuery, Dependency);
            }

            m_JobScheduled = true;
            m_ViewModeWhenScheduled = isWorkplaceMode;
        }

        private void SetOpen(bool open)
        {
            m_OpenBinding.Update(open);
            if (!open) m_JobScheduled = false;
        }

        private void SetViewMode(bool workplace)
        {
            m_ViewModeBinding.Update(workplace);
            // Invalidate pending results so stale-mode data isn't shown
            m_JobScheduled = false;
        }

        // ── JSON writer ───────────────────────────────────────────────────────
        private void WriteData(IJsonWriter writer)
        {
            writer.TypeBegin("WorkforcePipelineData");

            writer.PropertyName("ageEdu");
            writer.ArrayBegin((uint)m_AgeEduCounts.Length);
            for (int i = 0; i < m_AgeEduCounts.Length; i++)
                writer.Write(m_AgeEduCounts[i]);
            writer.ArrayEnd();

            writer.PropertyName("eduNonWork");
            writer.ArrayBegin((uint)m_EduNonWorkCounts.Length);
            for (int i = 0; i < m_EduNonWorkCounts.Length; i++)
                writer.Write(m_EduNonWorkCounts[i]);
            writer.ArrayEnd();

            writer.PropertyName("eduWorkSector");
            writer.ArrayBegin((uint)m_EduWorkSectorCounts.Length);
            for (int i = 0; i < m_EduWorkSectorCounts.Length; i++)
                writer.Write(m_EduWorkSectorCounts[i]);
            writer.ArrayEnd();

            writer.PropertyName("eduWorkJobEdu");
            writer.ArrayBegin((uint)m_EduWorkJobEduCounts.Length);
            for (int i = 0; i < m_EduWorkJobEduCounts.Length; i++)
                writer.Write(m_EduWorkJobEduCounts[i]);
            writer.ArrayEnd();

            writer.PropertyName("livingWorkerEdu");
            writer.ArrayBegin((uint)m_LivingWorkerEdu.Length);
            for (int i = 0; i < m_LivingWorkerEdu.Length; i++)
                writer.Write(m_LivingWorkerEdu[i]);
            writer.ArrayEnd();

            writer.PropertyName("workerEduJobEdu");
            writer.ArrayBegin((uint)m_WorkerEduJobEdu.Length);
            for (int i = 0; i < m_WorkerEduJobEdu.Length; i++)
                writer.Write(m_WorkerEduJobEdu[i]);
            writer.ArrayEnd();

            writer.PropertyName("jobEduSector");
            writer.ArrayBegin((uint)m_JobEduSector.Length);
            for (int i = 0; i < m_JobEduSector.Length; i++)
                writer.Write(m_JobEduSector[i]);
            writer.ArrayEnd();

            writer.TypeEnd();
        }
    }
}
