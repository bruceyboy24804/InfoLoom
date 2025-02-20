using System.Runtime.CompilerServices;
using Colossal;
using Colossal.Collections;
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
    public partial class WorkforceSystem : SystemBase
    {
        private struct CountEmploymentJob : IJobChunk
        {
            [ReadOnly]
            public ComponentTypeHandle<Citizen> m_CitizenType;

            [ReadOnly]
            public ComponentTypeHandle<Worker> m_WorkerType;

            [ReadOnly]
            public ComponentTypeHandle<Game.Citizens.Student> m_StudentType;

            [ReadOnly]
            public ComponentTypeHandle<HealthProblem> m_HealthProblemType;

            [ReadOnly]
            public ComponentTypeHandle<HouseholdMember> m_HouseholdMemberType;

            [ReadOnly]
            public ComponentLookup<OutsideConnection> m_OutsideConnections;

            [ReadOnly]
            public ComponentLookup<PropertyRenter> m_PropertyRenters;

            [ReadOnly]
            public ComponentLookup<HomelessHousehold> m_HomelessHouseholds;

            [ReadOnly]
            public ComponentLookup<MovingAway> m_MovingAways;

            [ReadOnly]
            public ComponentLookup<Household> m_Households;

            public NativeCounter.Concurrent m_PotentialWorkersByEducation0;

            public NativeCounter.Concurrent m_PotentialWorkersByEducation1;

            public NativeCounter.Concurrent m_PotentialWorkersByEducation2;

            public NativeCounter.Concurrent m_PotentialWorkersByEducation3;

            public NativeCounter.Concurrent m_PotentialWorkersByEducation4;

            public NativeCounter.Concurrent m_Workers;

            public NativeCounter.Concurrent m_Adults;

            public NativeCounter.Concurrent m_Unemployed;

            public NativeCounter.Concurrent m_EmployableByEducation0;

            public NativeCounter.Concurrent m_EmployableByEducation1;

            public NativeCounter.Concurrent m_EmployableByEducation2;

            public NativeCounter.Concurrent m_EmployableByEducation3;

            public NativeCounter.Concurrent m_EmployableByEducation4;

            public NativeCounter.Concurrent m_UnemploymentByEducation0;

            public NativeCounter.Concurrent m_UnemploymentByEducation1;

            public NativeCounter.Concurrent m_UnemploymentByEducation2;

            public NativeCounter.Concurrent m_UnemploymentByEducation3;

            public NativeCounter.Concurrent m_UnemploymentByEducation4;

            public NativeArray<WorkforcesInfo> m_Results;

            private void AddPotential(int level)
            {
                switch (level)
                {
                    case 0:
                        m_PotentialWorkersByEducation0.Increment();
                        break;
                    case 1:
                        m_PotentialWorkersByEducation1.Increment();
                        break;
                    case 2:
                        m_PotentialWorkersByEducation2.Increment();
                        break;
                    case 3:
                        m_PotentialWorkersByEducation3.Increment();
                        break;
                    case 4:
                        m_PotentialWorkersByEducation4.Increment();
                        break;
                }
            }

            private void AddEmployable(int level)
            {
                switch (level)
                {
                    case 0:
                        m_EmployableByEducation0.Increment();
                        break;
                    case 1:
                        m_EmployableByEducation1.Increment();
                        break;
                    case 2:
                        m_EmployableByEducation2.Increment();
                        break;
                    case 3:
                        m_EmployableByEducation3.Increment();
                        break;
                    case 4:
                        m_EmployableByEducation4.Increment();
                        break;
                }
            }

            private void AddUnemployment(int level)
            {
                switch (level)
                {
                    case 0:
                        m_UnemploymentByEducation0.Increment();
                        break;
                    case 1:
                        m_UnemploymentByEducation1.Increment();
                        break;
                    case 2:
                        m_UnemploymentByEducation2.Increment();
                        break;
                    case 3:
                        m_UnemploymentByEducation3.Increment();
                        break;
                    case 4:
                        m_UnemploymentByEducation4.Increment();
                        break;
                }
            }

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                //Plugin.Log($"chunk with {chunk.Count} entities");
                NativeArray<HouseholdMember> householdMemberArray = chunk.GetNativeArray(ref m_HouseholdMemberType);
                // this is probably not needed - all Citizen and Worker components are paired with HouseholdMember
                //if (!nativeArray.IsCreated)
                //{
                //return;
                //}
                NativeArray<Citizen> citizenArray = chunk.GetNativeArray(ref m_CitizenType);
                NativeArray<Worker> workerArray = chunk.GetNativeArray(ref m_WorkerType);
                NativeArray<HealthProblem> healthProblemArray = chunk.GetNativeArray(ref m_HealthProblemType);
                bool isWorker = workerArray.IsCreated;
                // [Worker and Student] are exclusive - there are no entities that have both - so, this can be used at Query level
                //bool isStudent = chunk.Has(ref m_StudentType); // students ARE excluded
                bool isHealthProblem = healthProblemArray.IsCreated;
                for (int i = 0; i < chunk.Count; i++)
                {
                    Citizen citizen = citizenArray[i];
                    CitizenAge age = citizen.GetAge();
                    // skip children and seniors
                    if (age == CitizenAge.Child || age == CitizenAge.Elderly)
                    {
                        continue;
                    }
                    Entity household = householdMemberArray[i].m_Household;
                    // skip: dead citizens, tourists and commuters, non-existing households (technical), not MovedIn yet, already MovingAways
                    if ((isHealthProblem && CitizenUtils.IsDead(healthProblemArray[i])) || (citizen.m_State & (CitizenFlags.Tourist | CitizenFlags.Commuter)) != 0 || !m_Households.HasComponent(household) || (m_Households[household].m_Flags & HouseholdFlags.MovedIn) == 0 || m_MovingAways.HasComponent(household))
                    {
                        continue;
                    }
                    // PROCESS ENTITY
                    int educationLevel = citizen.GetEducationLevel();
                    //Plugin.Log($"{age} {educationLevel} {isWorker}");
                    WorkforcesInfo info = m_Results[educationLevel];
                    //m_Adults.Increment(); // it is called Adults, but counts also Teens
                    info.Total++;
                    if (isWorker)
                    {
                        //m_Workers.Increment(); // actual workers
                        info.Worker++;
                        //AddPotential(educationLevel);
                        Worker worker = workerArray[i];
                        // This counts people working outside (???) or underemployed
                        //if (m_OutsideConnections.HasComponent(worker.m_Workplace) || worker.m_Level < educationLevel)
                        //{
                        //AddEmployable(educationLevel);
                        //}
                        bool isEmployable = false;
                        if (m_OutsideConnections.HasComponent(worker.m_Workplace)) { info.Outside++; isEmployable = true; }
                        if (worker.m_Level < educationLevel) { info.Under++; isEmployable = true; }
                        if (isEmployable) info.Employable++;
                    }
                    else
                    {
                        info.Unemployed++;
                    }
                    if (m_HomelessHouseholds.HasComponent(household) || !m_PropertyRenters.HasComponent(household)) // students ARE excluded
                    {
                        info.Homeless++;
                        //m_Unemployed.Increment();
                        //int educationLevel2 = citizen.GetEducationLevel();
                        //AddPotential(educationLevel2);
                        //AddEmployable(educationLevel2); // this is actually confusing, anyone not-working is employable, so this is no extra info
                        //AddUnemployment(educationLevel2);
                    }
                    // Potentials - already workers, not students, not homeless
                    // Employable
                    // Unemployment - not worker, not student
                    // student=0, home=0 -> counts
                    // student=0, home=1 -> counts [ok, typical unemployed]
                    // student=1, home=0 -> counts [weird, student but no home?] BUGGED
                    // student=1, home=1 -> excluded [ok, typical student]
                    // I should be showing homeless, because they may be leaving city soon...
                    // [Worker and Student] are exclusive - there are no entities that have both - so, this can be used at Query level
                    m_Results[educationLevel] = info;
                }
            }

            void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
            }
        }







        private SimulationSystem m_SimulationSystem;

        private EntityQuery m_AllAdultGroup;

        //[DebugWatchValue]
        private NativeValue<int> m_Workers;

        //[DebugWatchValue]
        private NativeValue<int> m_Adults;

        //[DebugWatchValue]
        private NativeValue<int> m_Unemployed;

        //[DebugWatchValue]
        private NativeArray<int> m_EmployableByEducation;

        //[DebugWatchValue]
        private NativeValue<int> m_Unemployment;

        //[DebugWatchValue]
        private NativeArray<int> m_UnemploymentByEducation;

        private NativeArray<int> m_PotentialWorkersByEducation;

        private NativeCounter m_WorkersTemp;

        private NativeCounter m_AdultsTemp;

        private NativeCounter m_UnemployedTemp;

        private NativeCounter m_EmployableByEducation0;

        private NativeCounter m_EmployableByEducation1;

        private NativeCounter m_EmployableByEducation2;

        private NativeCounter m_EmployableByEducation3;

        private NativeCounter m_EmployableByEducation4;

        private NativeCounter m_UnemploymentByEducation0;

        private NativeCounter m_UnemploymentByEducation1;

        private NativeCounter m_UnemploymentByEducation2;

        private NativeCounter m_UnemploymentByEducation3;

        private NativeCounter m_UnemploymentByEducation4;

        private NativeCounter m_PotentialWorkersByEducation0;

        private NativeCounter m_PotentialWorkersByEducation1;

        private NativeCounter m_PotentialWorkersByEducation2;

        private NativeCounter m_PotentialWorkersByEducation3;

        private NativeCounter m_PotentialWorkersByEducation4;
        
        // InfoLoom
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
            m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>(); // TODO: use UIUpdateState eventually
                                                                                          // main query
            m_AllAdultGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[1] { ComponentType.ReadOnly<Citizen>() },
                None = new ComponentType[3]
                {
                ComponentType.ReadOnly<Game.Citizens.Student>(),
                ComponentType.ReadOnly<Deleted>(),
                ComponentType.ReadOnly<Temp>()
                }
            });
            // data
            m_Workers = new NativeValue<int>(Allocator.Persistent);
            m_Adults = new NativeValue<int>(Allocator.Persistent);
            m_Unemployed = new NativeValue<int>(Allocator.Persistent);
            m_EmployableByEducation = new NativeArray<int>(5, Allocator.Persistent);
            m_UnemploymentByEducation = new NativeArray<int>(5, Allocator.Persistent);
            m_PotentialWorkersByEducation = new NativeArray<int>(5, Allocator.Persistent);
            m_Unemployment = new NativeValue<int>(Allocator.Persistent);
            m_WorkersTemp = new NativeCounter(Allocator.Persistent);
            m_AdultsTemp = new NativeCounter(Allocator.Persistent);
            m_UnemployedTemp = new NativeCounter(Allocator.Persistent);
            m_EmployableByEducation0 = new NativeCounter(Allocator.Persistent);
            m_EmployableByEducation1 = new NativeCounter(Allocator.Persistent);
            m_EmployableByEducation2 = new NativeCounter(Allocator.Persistent);
            m_EmployableByEducation3 = new NativeCounter(Allocator.Persistent);
            m_EmployableByEducation4 = new NativeCounter(Allocator.Persistent);
            m_UnemploymentByEducation0 = new NativeCounter(Allocator.Persistent);
            m_UnemploymentByEducation1 = new NativeCounter(Allocator.Persistent);
            m_UnemploymentByEducation2 = new NativeCounter(Allocator.Persistent);
            m_UnemploymentByEducation3 = new NativeCounter(Allocator.Persistent);
            m_UnemploymentByEducation4 = new NativeCounter(Allocator.Persistent);
            m_PotentialWorkersByEducation0 = new NativeCounter(Allocator.Persistent);
            m_PotentialWorkersByEducation1 = new NativeCounter(Allocator.Persistent);
            m_PotentialWorkersByEducation2 = new NativeCounter(Allocator.Persistent);
            m_PotentialWorkersByEducation3 = new NativeCounter(Allocator.Persistent);
            m_PotentialWorkersByEducation4 = new NativeCounter(Allocator.Persistent);

            // InfoLoom
            m_Results = new NativeArray<WorkforcesInfo>(6, Allocator.Persistent); // there are 5 education levels + 1 for totals




        }


        protected override void OnDestroy()
        {
            m_Workers.Dispose();
            m_Adults.Dispose();
            m_Unemployed.Dispose();
            m_EmployableByEducation.Dispose();
            m_Unemployment.Dispose();
            m_UnemploymentByEducation.Dispose();
            m_PotentialWorkersByEducation.Dispose();
            m_WorkersTemp.Dispose();
            m_AdultsTemp.Dispose();
            m_UnemployedTemp.Dispose();
            m_EmployableByEducation0.Dispose();
            m_EmployableByEducation1.Dispose();
            m_EmployableByEducation2.Dispose();
            m_EmployableByEducation3.Dispose();
            m_EmployableByEducation4.Dispose();
            m_UnemploymentByEducation0.Dispose();
            m_UnemploymentByEducation1.Dispose();
            m_UnemploymentByEducation2.Dispose();
            m_UnemploymentByEducation3.Dispose();
            m_UnemploymentByEducation4.Dispose();
            m_PotentialWorkersByEducation0.Dispose();
            m_PotentialWorkersByEducation1.Dispose();
            m_PotentialWorkersByEducation2.Dispose();
            m_PotentialWorkersByEducation3.Dispose();
            m_PotentialWorkersByEducation4.Dispose();
            // InfoLoom
            m_Results.Dispose();
            base.OnDestroy();
        }



        //[Preserve]
        protected override void OnUpdate()
        {
            if (!IsPanelVisible)
                return;
            if (m_SimulationSystem.frameIndex % 256 != 0 && !ForceUpdate)
                return;
            ForceUpdate = false;



            ResetResults();


            CountEmploymentJob jobData = default(CountEmploymentJob);
            jobData.m_CitizenType = SystemAPI.GetComponentTypeHandle<Citizen>(isReadOnly: true);
            jobData.m_HealthProblemType = SystemAPI.GetComponentTypeHandle<HealthProblem>(isReadOnly: true);
            jobData.m_WorkerType = SystemAPI.GetComponentTypeHandle<Worker>(isReadOnly: true);
            jobData.m_StudentType = SystemAPI.GetComponentTypeHandle<Game.Citizens.Student>(isReadOnly: true);
            jobData.m_HouseholdMemberType = SystemAPI.GetComponentTypeHandle<HouseholdMember>(isReadOnly: true);
            jobData.m_MovingAways = SystemAPI.GetComponentLookup<MovingAway>(isReadOnly: true);
            jobData.m_PropertyRenters = SystemAPI.GetComponentLookup<PropertyRenter>(isReadOnly: true);
            jobData.m_HomelessHouseholds = SystemAPI.GetComponentLookup<HomelessHousehold>(isReadOnly: true);
            jobData.m_OutsideConnections = SystemAPI.GetComponentLookup<OutsideConnection>(isReadOnly: true);
            jobData.m_Households = SystemAPI.GetComponentLookup<Household>(isReadOnly: true);
            jobData.m_Workers = m_WorkersTemp.ToConcurrent();
            jobData.m_Adults = m_AdultsTemp.ToConcurrent();
            jobData.m_Unemployed = m_UnemployedTemp.ToConcurrent();
            jobData.m_EmployableByEducation0 = m_EmployableByEducation0.ToConcurrent();
            jobData.m_EmployableByEducation1 = m_EmployableByEducation1.ToConcurrent();
            jobData.m_EmployableByEducation2 = m_EmployableByEducation2.ToConcurrent();
            jobData.m_EmployableByEducation3 = m_EmployableByEducation3.ToConcurrent();
            jobData.m_EmployableByEducation4 = m_EmployableByEducation4.ToConcurrent();
            jobData.m_UnemploymentByEducation0 = m_UnemploymentByEducation0.ToConcurrent();
            jobData.m_UnemploymentByEducation1 = m_UnemploymentByEducation1.ToConcurrent();
            jobData.m_UnemploymentByEducation2 = m_UnemploymentByEducation2.ToConcurrent();
            jobData.m_UnemploymentByEducation3 = m_UnemploymentByEducation3.ToConcurrent();
            jobData.m_UnemploymentByEducation4 = m_UnemploymentByEducation4.ToConcurrent();
            jobData.m_PotentialWorkersByEducation0 = m_PotentialWorkersByEducation0.ToConcurrent();
            jobData.m_PotentialWorkersByEducation1 = m_PotentialWorkersByEducation1.ToConcurrent();
            jobData.m_PotentialWorkersByEducation2 = m_PotentialWorkersByEducation2.ToConcurrent();
            jobData.m_PotentialWorkersByEducation3 = m_PotentialWorkersByEducation3.ToConcurrent();
            jobData.m_PotentialWorkersByEducation4 = m_PotentialWorkersByEducation4.ToConcurrent();
            jobData.m_Results = m_Results;
            JobChunkExtensions.Schedule(jobData, m_AllAdultGroup, base.Dependency).Complete();



            // calculate totals
            WorkforcesInfo totals = new WorkforcesInfo(-1);
            for (int i = 0; i < 5; i++)
            {
                totals.Total += m_Results[i].Total;
                totals.Worker += m_Results[i].Worker;
                totals.Unemployed += m_Results[i].Unemployed;
                totals.Homeless += m_Results[i].Homeless;
                totals.Employable += m_Results[i].Employable;
                totals.Outside += m_Results[i].Outside;
                totals.Under += m_Results[i].Under;
            }
            m_Results[5] = totals;


        }

        private void ResetResults()
        {

            for (int i = 0; i < 6; i++) // there are 5 education levels + 1 for totals
            {
                m_Results[i] = new WorkforcesInfo(i);
            }
        }






    }
}

