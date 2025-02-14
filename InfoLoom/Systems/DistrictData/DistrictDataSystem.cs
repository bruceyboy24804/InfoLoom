using System;
using System.Collections.Generic;
using Colossal.UI.Binding;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Game.UI;
using InfoLoomTwo.Domain;
using InfoLoomTwo.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Scripting;

namespace InfoLoomTwo.Systems.DistrictData
{
    public struct DistrictHouseholdData
    {
        public int ResidentCount;
        public int PetCount;
        public int HouseholdCount;
        public int MaxHouseholds;
    }

    [Serializable]
    public struct DistrictOutputData
    {
        public int residentCount;
        public int petCount;
        public int householdCount;
        public int maxHouseholds;
        public Entity districtEntity;
    }

    // This is the Burst‑compiled job used for counting household data for a single district.
    [BurstCompile]
    public struct CountDistrictHouseholdsJob : IJobChunk
    {
        [ReadOnly] public Entity m_SelectedEntity;
        [ReadOnly] public EntityTypeHandle m_EntityHandle;
        [ReadOnly] public ComponentTypeHandle<CurrentDistrict> m_CurrentDistrictHandle;
        [ReadOnly] public ComponentTypeHandle<PrefabRef> m_PrefabRefHandle;
        [ReadOnly] public ComponentLookup<Abandoned> m_AbandonedFromEntity;
        [ReadOnly] public ComponentLookup<HealthProblem> m_HealthProblemFromEntity;
        [ReadOnly] public ComponentLookup<TravelPurpose> m_TravelPurposeFromEntity;
        [ReadOnly] public ComponentLookup<BuildingPropertyData> m_PropertyDataFromEntity;
        [ReadOnly] public ComponentLookup<Household> m_HouseholdFromEntity;
        [ReadOnly] public BufferLookup<HouseholdCitizen> m_HouseholdCitizenFromEntity;
        [ReadOnly] public BufferLookup<HouseholdAnimal> m_HouseholdAnimalFromEntity;
        [ReadOnly] public BufferLookup<Renter> m_RenterFromEntity;

        // m_Results is an array of 5 integers:
        // [0]: A flag (unused here), [1]: residentCount, [2]: petCount, [3]: householdCount, [4]: maxHouseholds.
        public NativeArray<int> m_Results;

        // (m_HouseholdsResult is not used in our output but is provided for compatibility.)
        public NativeList<Entity> m_HouseholdsResult;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in Unity.Burst.Intrinsics.v128 chunkEnabledMask)
        {
            NativeArray<Entity> entities = chunk.GetNativeArray(m_EntityHandle);
            NativeArray<CurrentDistrict> currentDistricts = chunk.GetNativeArray(ref m_CurrentDistrictHandle);
            NativeArray<PrefabRef> prefabRefs = chunk.GetNativeArray(ref m_PrefabRefHandle);

            int residentCount = 0;
            int petCount = 0;
            int householdCount = 0;
            int maxHouseholds = 0;
            int found = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                // Process only buildings whose CurrentDistrict matches the selected district.
                if (currentDistricts[i].m_District != m_SelectedEntity)
                    continue;
                found = 1; // Found at least one building in this district.
                Entity buildingEntity = entities[i];
                PrefabRef prefabRef = prefabRefs[i];
                if (!m_AbandonedFromEntity.HasComponent(buildingEntity) &&
                    m_PropertyDataFromEntity.TryGetComponent(prefabRef.m_Prefab, out var propertyData) &&
                    propertyData.m_ResidentialProperties > 0)
                {
                    maxHouseholds += propertyData.m_ResidentialProperties;
                    if (m_RenterFromEntity.TryGetBuffer(buildingEntity, out var renterBuffer))
                    {
                        for (int j = 0; j < renterBuffer.Length; j++)
                        {
                            Entity householdEntity = renterBuffer[j].m_Renter;
                            if (!m_HouseholdFromEntity.HasComponent(householdEntity) ||
                                !m_HouseholdCitizenFromEntity.TryGetBuffer(householdEntity, out var citizenBuffer))
                                continue;
                            householdCount++;
                            for (int k = 0; k < citizenBuffer.Length; k++)
                            {
                                if (!CitizenUtils.IsCorpsePickedByHearse(citizenBuffer[k].m_Citizen,
                                    ref m_HealthProblemFromEntity, ref m_TravelPurposeFromEntity))
                                {
                                    residentCount++;
                                }
                            }
                            if (m_HouseholdAnimalFromEntity.TryGetBuffer(householdEntity, out var animalBuffer))
                            {
                                petCount += animalBuffer.Length;
                            }
                        }
                    }
                }
            }
            m_Results[0] += found;
            m_Results[1] += residentCount;
            m_Results[2] += petCount;
            m_Results[3] += householdCount;
            m_Results[4] += maxHouseholds;
        }

        void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in Unity.Burst.Intrinsics.v128 chunkEnabledMask)
        {
            Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
        }
    }

    // This system accumulates household data for each district by scheduling a job per district.
    public partial class DistrictDataSystem : SystemBase
    {
        public static DistrictOutputData[] SharedDistrictOutputData;

        protected override void OnUpdate()
        {
            // Query all district entities.
            var districtQuery = GetEntityQuery(ComponentType.ReadOnly<District>(), ComponentType.Exclude<Temp>());
            NativeArray<Entity> districtEntities = districtQuery.ToEntityArray(Allocator.Temp);

            List<DistrictOutputData> outputDataList = new List<DistrictOutputData>();

            // Prepare lookups and handles to be passed to the job.
            var abandonedLookup = GetComponentLookup<Abandoned>(isReadOnly: true);
            var healthProblemLookup = GetComponentLookup<HealthProblem>(isReadOnly: true);
            var travelPurposeLookup = GetComponentLookup<TravelPurpose>(isReadOnly: true);
            var propertyDataLookup = GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
            var householdLookup = GetComponentLookup<Household>(isReadOnly: true);
            var householdCitizenLookup = GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
            var householdAnimalLookup = GetBufferLookup<HouseholdAnimal>(isReadOnly: true);
            var renterLookup = GetBufferLookup<Renter>(isReadOnly: true);

            var entityHandle = GetEntityTypeHandle();
            var currentDistrictHandle = GetComponentTypeHandle<CurrentDistrict>(isReadOnly: true);
            var prefabRefHandle = GetComponentTypeHandle<PrefabRef>(isReadOnly: true);

            // Create a query for buildings; the job will filter by district.
            var buildingQuery = GetEntityQuery(
                ComponentType.ReadOnly<Building>(),
                ComponentType.ReadOnly<ResidentialProperty>(),
                ComponentType.ReadOnly<PrefabRef>(),
                ComponentType.ReadOnly<Renter>(),
                ComponentType.ReadOnly<CurrentDistrict>(),
                ComponentType.Exclude<Temp>()
            );

            // For each district, schedule a job that counts household data.
            for (int i = 0; i < districtEntities.Length; i++)
            {
                Entity districtEntity = districtEntities[i];

                NativeArray<int> results = new NativeArray<int>(5, Allocator.TempJob);
                NativeList<Entity> householdsResult = new NativeList<Entity>(Allocator.TempJob);

                var job = new CountDistrictHouseholdsJob
                {
                    m_SelectedEntity = districtEntity,
                    m_EntityHandle = entityHandle,
                    m_CurrentDistrictHandle = currentDistrictHandle,
                    m_PrefabRefHandle = prefabRefHandle,
                    m_AbandonedFromEntity = abandonedLookup,
                    m_HealthProblemFromEntity = healthProblemLookup,
                    m_TravelPurposeFromEntity = travelPurposeLookup,
                    m_PropertyDataFromEntity = propertyDataLookup,
                    m_HouseholdFromEntity = householdLookup,
                    m_HouseholdCitizenFromEntity = householdCitizenLookup,
                    m_HouseholdAnimalFromEntity = householdAnimalLookup,
                    m_RenterFromEntity = renterLookup,
                    m_Results = results,
                    m_HouseholdsResult = householdsResult
                };

                JobHandle handle = job.ScheduleParallel(buildingQuery, Dependency);
                handle.Complete();

                int residentCount = results[1];
                int petCount = results[2];
                int householdCount = results[3];
                int maxHouseholds = results[4];

                results.Dispose();
                householdsResult.Dispose();

                DistrictOutputData output = new DistrictOutputData
                {
                    residentCount = residentCount,
                    petCount = petCount,
                    householdCount = householdCount,
                    maxHouseholds = maxHouseholds,
                    districtEntity = districtEntity
                };
                outputDataList.Add(output);
            }
            districtEntities.Dispose();

            SharedDistrictOutputData = outputDataList.ToArray();
        }
    }
}
