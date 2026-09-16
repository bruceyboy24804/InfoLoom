using System.Runtime.CompilerServices;
using Colossal.Collections;
using ModsCommon.Extensions;
using Game.Agents;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.Objects;
using InfoLoomTwo.Domain;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Student = Game.Citizens.Student;

namespace InfoLoomTwo.Systems.WorkforceData
{
    public partial class WorkforceSystem
    {

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
            [ReadOnly] public ComponentLookup<Student> m_Students;
            [ReadOnly] public ComponentLookup<Citizen> m_Citizens;
            [ReadOnly] public ComponentLookup<HealthProblem> m_HealthProblems;

            // Per-thread accumulator (5 slots, one per education level) — safe under
            // ScheduleParallel because each thread accumulates into its own scratch buffer;
            // GetResult() sums across threads afterwards. Replaces a shared NativeArray that
            // was being read-modify-written from many chunks concurrently (lost-update race).
            public NativeAccumulator<WorkforcesInfo>.ParallelWriter m_ResultsWriter;
            public Entity m_SelectedDistrict;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entityArray = chunk.GetNativeArray(m_EntityType);
                var householdMemberArray = chunk.GetNativeArray(ref m_HouseholdMemberType);
                var citizenArray = chunk.GetNativeArray(ref m_CitizenType);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var citizenEntity = entityArray[i];
                    var household = householdMemberArray[i].m_Household;
                    if (m_SelectedDistrict != Entity.Null && !IsInSelectedDistrict(household))
                        continue;
                    var citizen = citizenArray[i];

                    if (CitizenUtils.IsDead(citizenEntity, ref m_HealthProblems))
                        continue;
                    if (m_Students.HasComponent(citizenEntity))
                        continue;
                    if ((citizen.m_State & (CitizenFlags.Tourist | CitizenFlags.Commuter)) != CitizenFlags.None)
                        continue;
                    var age = citizen.GetAge();
                    if (age != CitizenAge.Teen && age != CitizenAge.Adult)
                        continue;
                    if (!m_Households.HasComponent(household))
                        continue;
                    if ((m_Households[household].m_Flags & HouseholdFlags.MovedIn) == 0)
                        continue;
                    if (m_MovingAways.HasComponent(household))
                        continue;

                    var hasWorker = m_Workers.HasComponent(citizenEntity);
                    var worker = hasWorker ? m_Workers[citizenEntity] : default;
                    ProcessCitizen(citizen, household, worker, hasWorker);
                }
            }

            private void ProcessCitizen(Citizen citizen, Entity household, Worker worker, bool isWorker)
            {
                var educationLevel = citizen.GetEducationLevel();
                var delta = new WorkforcesInfo(educationLevel) { Total = 1 };
                var hasWorker = isWorker;
                if (hasWorker)
                {
                    delta.Worker = 1;
                    var isWorkingOutside = m_OutsideConnections.HasComponent(worker.m_Workplace);
                    var isUnderemployed = worker.m_Level < educationLevel;
                    if (isWorkingOutside) delta.Outside = 1;
                    if (isUnderemployed) delta.Under = 1;
                    if (isWorkingOutside || isUnderemployed) delta.Employable = 1;
                }
                else
                {
                    delta.Employable = 1;
                }
                if (m_HomelessHouseholds.HasComponent(household) || !m_PropertyRenters.HasComponent(household)) delta.Homeless = 1;
                m_ResultsWriter.Accumulate(educationLevel, delta);
            }

            private bool IsInSelectedDistrict(Entity household)
            {
                if (m_SelectedDistrict == Entity.Null) return true;
                if (!m_PropertyRenters.HasComponent(household)) return false;
                var propertyRenter = m_PropertyRenters[household];
                var buildingEntity = propertyRenter.m_Property;
                if (buildingEntity == Entity.Null) return false;
                if (m_CurrentDistrictLookup.HasComponent(buildingEntity))
                {
                    var currentDistrict = m_CurrentDistrictLookup[buildingEntity];
                    return currentDistrict.m_District == m_SelectedDistrict;
                }
                return false;
            }
        }
    }
}
