using ModsCommon.Extensions;
using ModsCommon.Systems;
using Colossal.Entities;
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
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace InfoLoomTwo.Systems.SankeyUISystems
{
    /// <summary>
    /// Provides Sankey diagram data for the Workforce & Education Pipeline panel.
    ///
    /// Workforce view (viewMode=false):
    ///   ageEdu[ageGroup * 5 + eduLevel]           â€” Demographics â†’ Education       (4Ã—5 = 20)
    ///   eduNonWork[eduLevel * 4 + status]         â€” Education â†’ non-work status    (5Ã—4 = 20)
    ///     status: 0=NotInSchool  1=School  2=Unemployed  3=Retired
    ///   eduWorkSector[eduLevel * 5 + sector]      â€” Education â†’ sector (workers)   (5Ã—5 = 25)
    ///     sector: 0=Commercial 1=Industrial 2=Office 3=CityService 4=OutsideCity
    ///   eduWorkJobEdu[eduLevel * 5 + jobEdu]      â€” Education â†’ job edu (workers)  (5Ã—5 = 25)
    ///
    /// Workplace view (viewMode=true) â€” 4-column:
    ///   livingWorkerEdu[living * 5 + workerEdu]    â€” Living Place   â†’ Worker Edu   (2Ã—5 = 10)
    ///   workerEduJobEdu[workerEdu * 5 + jobEdu]    â€” Worker Edu     â†’ Job Edu      (5Ã—5 = 25)
    ///   jobEduSector[jobEdu * 5 + sector]          â€” Job Edu Needed â†’ Sector       (5Ã—5 = 25)
    ///   living: 0=OutsideCity 1=WithinCity
    ///   sector: 0=Commercial 1=Industrial 2=Office 3=CityService 4=OutsideCity
    /// </summary>
    public partial class WorkforcePipelineSankeySystem : CommonUISystemBase
    {
        protected override string ModId => InfoLoomMod.Instance.Id;

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

        // ComponentTypeHandles
        private EntityTypeHandle m_EntityTypeHandle;
        private ComponentTypeHandle<Citizen> m_CitizenTypeHandle;
        private ComponentTypeHandle<HouseholdMember> m_HouseholdMemberTypeHandle;
        private ComponentTypeHandle<Worker> m_WorkerTypeHandle;

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

        private UIUpdateState _updateState;
        // â”€â”€ Burst job â€” workforce view â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        [BurstCompile]
        private partial struct WorkforcePipelineJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle                                  m_EntityTypeHandle;
            [ReadOnly] public ComponentTypeHandle<Citizen>                      m_CitizenTypeHandle;
            [ReadOnly] public ComponentTypeHandle<HouseholdMember>              m_HouseholdMemberTypeHandle;
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

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entityArray = chunk.GetNativeArray(m_EntityTypeHandle);
                var citizenArray = chunk.GetNativeArray(ref m_CitizenTypeHandle);
                var memberArray = chunk.GetNativeArray(ref m_HouseholdMemberTypeHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out int i))
                {
                    Entity entity = entityArray[i];
                    Citizen citizen = citizenArray[i];
                    HouseholdMember member = memberArray[i];

                    Entity household = member.m_Household;

                    if (!m_Households.TryGetComponent(household, out Household hh)) continue;
                    if ((hh.m_Flags & HouseholdFlags.MovedIn) == 0) continue;
                    if ((citizen.m_State & (CitizenFlags.Tourist | CitizenFlags.Commuter)) != 0) continue;
                    if (m_MovingAways.HasComponent(household)) continue;
                    if (m_HealthProblems.TryGetComponent(entity, out HealthProblem hp) && CitizenUtils.IsDead(hp)) continue;

                    CitizenAge ageGroup = citizen.GetAge();
                    int        eduLevel = citizen.GetEducationLevel();
                    int        ageIdx   = (int)ageGroup;

                    m_AgeEduCounts[ageIdx * EDU_LEVELS + eduLevel]++;

                    if (m_Workers.TryGetComponent(entity, out Worker worker))
                    {
                        Entity workplace = worker.m_Workplace;
                        if (workplace == Entity.Null) continue;

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
        }

        // â”€â”€ Workplace view â€” iterates all workers (including commuters) â”€â”€â”€â”€â”€â”€â”€
        [BurstCompile]
        private partial struct WorkplaceSankeyJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle                                  m_EntityTypeHandle;
            [ReadOnly] public ComponentTypeHandle<Citizen>                      m_CitizenTypeHandle;
            [ReadOnly] public ComponentTypeHandle<HouseholdMember>              m_HouseholdMemberTypeHandle;
            [ReadOnly] public ComponentTypeHandle<Worker>                       m_WorkerTypeHandle;
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

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entityArray = chunk.GetNativeArray(m_EntityTypeHandle);
                var citizenArray = chunk.GetNativeArray(ref m_CitizenTypeHandle);
                var memberArray = chunk.GetNativeArray(ref m_HouseholdMemberTypeHandle);
                var workerArray = chunk.GetNativeArray(ref m_WorkerTypeHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out int i))
                {
                    Entity entity = entityArray[i];
                    Citizen citizen = citizenArray[i];
                    HouseholdMember member = memberArray[i];
                    Worker worker = workerArray[i];

                    Entity household = member.m_Household;

                    if (!m_Households.TryGetComponent(household, out Household hh)) continue;
                    bool isCommuter = (citizen.m_State & CitizenFlags.Commuter) != 0;
                    // Non-commuters must be MovedIn; skip tourists
                    if (!isCommuter)
                    {
                        if ((hh.m_Flags & HouseholdFlags.MovedIn) == 0) continue;
                        if ((citizen.m_State & CitizenFlags.Tourist) != 0) continue;
                    }
                    if (m_MovingAways.HasComponent(household)) continue;
                    if (m_HealthProblems.TryGetComponent(entity, out HealthProblem hp) && CitizenUtils.IsDead(hp)) continue;

                    Entity workplace = worker.m_Workplace;
                    if (workplace == Entity.Null) continue;

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
        }

        // â”€â”€ Lifecycle â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            _updateState = UIUpdateState.Create(World, 512);
            m_AgeEduCounts        = new NativeArray<int>(AGE_GROUPS * EDU_LEVELS,   Allocator.Persistent);
            m_EduNonWorkCounts    = new NativeArray<int>(EDU_LEVELS * NON_WORK_COUNT, Allocator.Persistent);
            m_EduWorkSectorCounts = new NativeArray<int>(EDU_LEVELS * SECTOR_COUNT, Allocator.Persistent);
            m_EduWorkJobEduCounts = new NativeArray<int>(EDU_LEVELS * EDU_LEVELS,   Allocator.Persistent);
            m_LivingWorkerEdu     = new NativeArray<int>(LIVING_COUNT * EDU_LEVELS, Allocator.Persistent);
            m_WorkerEduJobEdu     = new NativeArray<int>(EDU_LEVELS * EDU_LEVELS,   Allocator.Persistent);
            m_JobEduSector        = new NativeArray<int>(EDU_LEVELS * SECTOR_COUNT, Allocator.Persistent);

            m_EntityTypeHandle = SystemAPI.GetEntityTypeHandle();
            m_CitizenTypeHandle = SystemAPI.GetComponentTypeHandle<Citizen>(true);
            m_HouseholdMemberTypeHandle = SystemAPI.GetComponentTypeHandle<HouseholdMember>(true);
            m_WorkerTypeHandle = SystemAPI.GetComponentTypeHandle<Worker>(true);

            m_CitizenQuery = SystemAPI.QueryBuilder()
                .WithAll<Citizen, HouseholdMember>()
                .WithNone<Deleted, Temp>()
                .Build();

            m_WorkerQuery = SystemAPI.QueryBuilder()
                .WithAll<Citizen, HouseholdMember, Worker>()
                .WithNone<Deleted, Temp>()
                .Build();

            // Do not RequireForUpdate here â€” OnUpdate decides which query is active.

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
                // Panel closed â€” reset state so next open starts fresh
                m_JobScheduled = false;
                return;
            }
            bool isWorkplaceMode = m_ViewModeBinding.value;

            // â”€â”€ Step 1: Complete any in-flight job and push its results â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            // After 512 ticks the worker threads have been idle; Complete() is near-instant.
            if (m_JobScheduled)
            {
                CompleteDependency();
                // Only push data if the view mode hasn't changed since the job was scheduled.
                // If it changed, the arrays contain stale-mode data â€” skip the UI push.
                if (m_ViewModeWhenScheduled == isWorkplaceMode)
                    m_DataBinding.Update();
                m_JobScheduled = false;
            }

            // â”€â”€ Step 2: Guard â€” skip if the active query has no entities â”€â”€â”€â”€â”€â”€â”€â”€â”€
            bool activeQueryEmpty = isWorkplaceMode
                ? m_WorkerQuery.IsEmptyIgnoreFilter
                : m_CitizenQuery.IsEmptyIgnoreFilter;

            if (activeQueryEmpty)
                return;

            // â”€â”€ Step 3: Gate â€” only reschedule every N ticks â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            if (!_updateState.Advance())
                return;

            // â”€â”€ Step 4: Update component type handles for current frame â”€â”€â”€â”€â”€â”€â”€â”€â”€
            m_EntityTypeHandle = SystemAPI.GetEntityTypeHandle();
            m_CitizenTypeHandle = SystemAPI.GetComponentTypeHandle<Citizen>(true);
            m_HouseholdMemberTypeHandle = SystemAPI.GetComponentTypeHandle<HouseholdMember>(true);
            m_WorkerTypeHandle = SystemAPI.GetComponentTypeHandle<Worker>(true);

            // â”€â”€ Step 5: Zero output arrays and schedule job â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            if (!isWorkplaceMode)
            {
                for (int i = 0; i < m_AgeEduCounts.Length;        i++) m_AgeEduCounts[i]        = 0;
                for (int i = 0; i < m_EduNonWorkCounts.Length;    i++) m_EduNonWorkCounts[i]    = 0;
                for (int i = 0; i < m_EduWorkSectorCounts.Length; i++) m_EduWorkSectorCounts[i] = 0;
                for (int i = 0; i < m_EduWorkJobEduCounts.Length; i++) m_EduWorkJobEduCounts[i] = 0;
                
                WorkforcePipelineJob job = default(WorkforcePipelineJob);
                job.m_EntityTypeHandle     = m_EntityTypeHandle;
                job.m_CitizenTypeHandle    = m_CitizenTypeHandle;
                job.m_HouseholdMemberTypeHandle = m_HouseholdMemberTypeHandle;
                job.m_Workers              = SystemAPI.GetComponentLookup<Worker>(true);
                job.m_Students             = SystemAPI.GetComponentLookup<Game.Citizens.Student>(true);
                job.m_HealthProblems       = SystemAPI.GetComponentLookup<HealthProblem>(true);
                job.m_MovingAways          = SystemAPI.GetComponentLookup<MovingAway>(true);
                job.m_Households           = SystemAPI.GetComponentLookup<Household>(true);
                job.m_OutsideConnections   = SystemAPI.GetComponentLookup<Game.Objects.OutsideConnection>(true);
                job.m_CommercialCompanies  = SystemAPI.GetComponentLookup<CommercialCompany>(true);
                job.m_IndustrialCompanies  = SystemAPI.GetComponentLookup<IndustrialCompany>(true);
                job.m_PrefabRefs           = SystemAPI.GetComponentLookup<PrefabRef>(true);
                job.m_IndustrialProcessDatas = SystemAPI.GetComponentLookup<IndustrialProcessData>(true);
                job.m_AgeEduCounts         = m_AgeEduCounts;
                job.m_EduNonWorkCounts     = m_EduNonWorkCounts;
                job.m_EduWorkSectorCounts  = m_EduWorkSectorCounts;
                job.m_EduWorkJobEduCounts  = m_EduWorkJobEduCounts;
                
                // Schedule with ScheduleParallel for true parallelization across chunks
                Dependency = JobChunkExtensions.ScheduleParallel(job, m_CitizenQuery, Dependency);
            }
            else
            {
                for (int i = 0; i < m_LivingWorkerEdu.Length; i++) m_LivingWorkerEdu[i] = 0;
                for (int i = 0; i < m_WorkerEduJobEdu.Length; i++) m_WorkerEduJobEdu[i] = 0;
                for (int i = 0; i < m_JobEduSector.Length;    i++) m_JobEduSector[i]    = 0;

                WorkplaceSankeyJob wpJob = default(WorkplaceSankeyJob);
                wpJob.m_EntityTypeHandle   = m_EntityTypeHandle;
                wpJob.m_CitizenTypeHandle  = m_CitizenTypeHandle;
                wpJob.m_HouseholdMemberTypeHandle = m_HouseholdMemberTypeHandle;
                wpJob.m_WorkerTypeHandle   = m_WorkerTypeHandle;
                wpJob.m_Households           = SystemAPI.GetComponentLookup<Household>(true);
                wpJob.m_MovingAways          = SystemAPI.GetComponentLookup<MovingAway>(true);
                wpJob.m_HealthProblems       = SystemAPI.GetComponentLookup<HealthProblem>(true);
                wpJob.m_OutsideConnections   = SystemAPI.GetComponentLookup<Game.Objects.OutsideConnection>(true);
                wpJob.m_CommercialCompanies  = SystemAPI.GetComponentLookup<CommercialCompany>(true);
                wpJob.m_IndustrialCompanies  = SystemAPI.GetComponentLookup<IndustrialCompany>(true);
                wpJob.m_PrefabRefs           = SystemAPI.GetComponentLookup<PrefabRef>(true);
                wpJob.m_IndustrialProcessDatas = SystemAPI.GetComponentLookup<IndustrialProcessData>(true);
                wpJob.m_LivingWorkerEdu      = m_LivingWorkerEdu;
                wpJob.m_WorkerEduJobEdu      = m_WorkerEduJobEdu;
                wpJob.m_JobEduSector         = m_JobEduSector;
                
                // Schedule with ScheduleParallel for true parallelization across chunks
                Dependency = JobChunkExtensions.ScheduleParallel(wpJob, m_WorkerQuery, Dependency);
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

        // â”€â”€ JSON writer â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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


