using System.Runtime.CompilerServices;
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
            public NativeArray<WorkforcesInfo> m_Results;
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
                    if (!IsWorkableCitizen(citizenEntity, ref m_Citizens, ref m_Students, ref m_HealthProblems))
                        continue;
                    if (ShouldSkipCitizen(citizenEntity, citizen, household))
                        continue;
                    var hasWorker = m_Workers.HasComponent(citizenEntity);
                    var worker = hasWorker ? m_Workers[citizenEntity] : default;
                    ProcessCitizen(citizen, household, worker, hasWorker);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool IsWorkableCitizen(Entity citizenEntity, ref ComponentLookup<Citizen> citizens,
                ref ComponentLookup<Student> students, ref ComponentLookup<HealthProblem> healthProblems)
            {
                return (!healthProblems.HasComponent(citizenEntity) ||
                        !CitizenUtils.IsDead(healthProblems[citizenEntity])) &&
                       !students.HasComponent(citizenEntity) &&
                       (citizens[citizenEntity].m_State & (CitizenFlags.Tourist | CitizenFlags.Commuter)) ==
                       CitizenFlags.None &&
                       (citizens[citizenEntity].GetAge() == CitizenAge.Teen ||
                        citizens[citizenEntity].GetAge() == CitizenAge.Adult);
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
                var educationLevel = citizen.GetEducationLevel();
                var info = m_Results[educationLevel];
                info.Total++;
                var hasWorker = isWorker;
                if (hasWorker)
                {
                    info.Worker++;
                    var isWorkingOutside = m_OutsideConnections.HasComponent(worker.m_Workplace);
                    var isUnderemployed = worker.m_Level < educationLevel;
                    if (isWorkingOutside) info.Outside++;
                    if (isUnderemployed) info.Under++;
                    if (isWorkingOutside || isUnderemployed) info.Employable++;
                }
                else
                {
                    info.Employable++;
                }
                if (m_HomelessHouseholds.HasComponent(household) || !m_PropertyRenters.HasComponent(household)) info.Homeless++;
                m_Results[educationLevel] = info;
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
