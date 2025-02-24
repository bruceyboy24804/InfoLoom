using System;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.UI;
using Game.UI.InGame; // Uses shared definitions for AgeData and EducationData
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace InfoLoomTwo.Systems.DistrictData
{
    // For the purposes of the job below, we assume that Citizen is a job-safe IComponentData,
    // and that it has fields "age" (of type CitizenAge) and "educationLevel" (an int).
    // We also assume AgeData and EducationData are immutable structs that expose read-only properties and are initialized via constructors.
    public partial class DistrictDataSystem : SystemBase
    {
        public struct DistrictEntry
        {
            public Entity district;
            public int residentCount;
            public int petCount;
            public int householdCount;
            public int maxHouseholds;
            public AgeData ageData;
            public EducationData educationData;
            public HouseholdWealthKey wealthKey;
            public NativeList<Entity> households;
        }

        // Job to count household data for each district.
        [BurstCompile]
        private struct CountDistrictStatsJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle m_EntityHandle;
            [ReadOnly] public ComponentTypeHandle<CurrentDistrict> m_CurrentDistrictHandle;
            [ReadOnly] public ComponentTypeHandle<PrefabRef> m_PrefabRefHandle;
            [ReadOnly] public ComponentLookup<Abandoned> m_AbandonedFromEntity;
            [ReadOnly] public ComponentLookup<BuildingPropertyData> m_PropertyDataFromEntity;
            [ReadOnly] public ComponentLookup<Household> m_HouseholdFromEntity;
            [ReadOnly] public BufferLookup<HouseholdCitizen> m_HouseholdCitizenFromEntity;
            [ReadOnly] public BufferLookup<HouseholdAnimal> m_HouseholdAnimalFromEntity;
            [ReadOnly] public BufferLookup<Renter> m_RenterFromEntity;
            [ReadOnly] public CitizenHappinessParameterData m_HappinessData;
            [ReadOnly] public NativeHashMap<Entity, int> m_DistrictIndexMap; // Map: district Entity -> index in m_Districts

            public NativeList<DistrictEntry> m_Districts;

            bool TryProcessHousehold(ref DistrictEntry entry, Entity household)
            {
                if (!m_HouseholdFromEntity.HasComponent(household) ||
                    !m_HouseholdCitizenFromEntity.TryGetBuffer(household, out _))
                    return false;

                entry.householdCount++;
                // Add number of citizens as residents.
                var citizens = m_HouseholdCitizenFromEntity[household];
                entry.residentCount += citizens.Length;
                entry.households.Add(household);

                if (m_HouseholdAnimalFromEntity.TryGetBuffer(household, out var animals))
                    entry.petCount += animals.Length;

                return true;
            }

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entities = chunk.GetNativeArray(m_EntityHandle);
                NativeArray<CurrentDistrict> currentDistricts = chunk.GetNativeArray(ref m_CurrentDistrictHandle);
                NativeArray<PrefabRef> prefabRefs = chunk.GetNativeArray(ref m_PrefabRefHandle);

                // Process each chunk entity.
                for (int i = 0; i < chunk.Count; i++)
                {
                    Entity building = entities[i];
                    Entity district = currentDistricts[i].m_District;
                    Entity prefab = prefabRefs[i].m_Prefab;

                    if (m_AbandonedFromEntity.HasComponent(building))
                        continue;
                    if (!m_PropertyDataFromEntity.HasComponent(prefab))
                        continue;
                    var propertyData = m_PropertyDataFromEntity[prefab];
                    if (propertyData.m_ResidentialProperties <= 0)
                        continue;
                    if (!m_DistrictIndexMap.TryGetValue(district, out int districtIndex))
                        continue;

                    var entry = m_Districts[districtIndex];
                    entry.maxHouseholds += propertyData.m_ResidentialProperties;

                    if (m_RenterFromEntity.TryGetBuffer(building, out var renters))
                    {
                        for (int k = 0; k < renters.Length; k++)
                        {
                            Entity household = renters[k].m_Renter;
                            if (TryProcessHousehold(ref entry, household) &&
                                m_HouseholdCitizenFromEntity.TryGetBuffer(household, out var citizens))
                            {
                                // Additional household processing could be done here.
                            }
                        }
                    }
                    m_Districts[districtIndex] = entry;
                }
            }
        }

        // New job to process AgeData and EducationData for each district.
        // This job now uses ComponentLookup<T> for citizen data.
        [BurstCompile]
        private struct ProcessDistrictDataJob : IJobParallelFor
        {
            public NativeArray<DistrictEntry> Districts;

            // Lookup for household citizens (job-safe).
            [ReadOnly] public BufferLookup<HouseholdCitizen> HouseholdCitizenLookup;

            // Lookup for citizen data.
            [ReadOnly] public ComponentLookup<Citizen> CitizenDataLookup;

            public void Execute(int index)
            {
                // Create local accumulators since AgeData and EducationData are immutable and have no setters.
                int children = 0, teens = 0, adults = 0, elders = 0;
                int uneducated = 0, poorlyEducated = 0, educated = 0, wellEducated = 0, highlyEducated = 0;

                DistrictEntry district = Districts[index];
                for (int j = 0; j < district.households.Length; j++)
                {
                    Entity household = district.households[j];
                    if (!HouseholdCitizenLookup.HasBuffer(household))
                        continue;

                    DynamicBuffer<HouseholdCitizen> citizens = HouseholdCitizenLookup[household];
                    for (int k = 0; k < citizens.Length; k++)
                    {
                        Entity citizenEntity = citizens[k].m_Citizen;
                        if (!CitizenDataLookup.HasComponent(citizenEntity))
                            continue;
                        Citizen citizen = CitizenDataLookup[citizenEntity];

                        // Accumulate AgeData values.
                        switch (citizen.GetAge())
                        {
                            case CitizenAge.Child:
                                children++;
                                break;
                            case CitizenAge.Teen:
                                teens++;
                                break;
                            case CitizenAge.Adult:
                                adults++;
                                break;
                            case CitizenAge.Elderly:
                                elders++;
                                break;
                        }

                        // Accumulate EducationData values.
                        switch (citizen.GetEducationLevel())
                        {
                            case 0:
                                uneducated++;
                                break;
                            case 1:
                                poorlyEducated++;
                                break;
                            case 2:
                                educated++;
                                break;
                            case 3:
                                wellEducated++;
                                break;
                            case 4:
                                highlyEducated++;
                                break;
                        }
                    }
                }

                // Create new structs using the accumulated values.
                district.ageData = new AgeData(children, teens, adults, elders);
                district.educationData = new EducationData(uneducated, poorlyEducated, educated, wellEducated, highlyEducated);
                Districts[index] = district;
            }
        }

        // Normal Entity Queries.
        private EntityQuery m_DistrictBuildingQuery;
        private EntityQuery m_HappinessParameterQuery;
        private EntityQuery m_DistrictQuery;

        private NameSystem m_NameSystem;
        private NativeList<DistrictEntry> m_Districts;
        private SimulationSystem m_SimulationSystem;
        public bool IsPanelVisible { get; set; }
        

        protected override void OnCreate()
        {
            base.OnCreate();

            m_DistrictBuildingQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    typeof(Building),
                    typeof(ResidentialProperty),
                    typeof(PrefabRef),
                    typeof(Renter),
                    typeof(CurrentDistrict)
                },
                None = new ComponentType[]
                {
                    typeof(Temp),
                    typeof(Deleted)
                }
            });

            m_HappinessParameterQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(CitizenHappinessParameterData) }
            });

            m_DistrictQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(District) },
                None = new ComponentType[] { typeof(Temp) }
            });

            m_NameSystem = World.GetOrCreateSystemManaged<NameSystem>();
            m_Districts = new NativeList<DistrictEntry>(64, Allocator.Persistent);
            m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
            RequireForUpdate<District>();
        }

        protected override void OnDestroy()
        {
            for (int i = 0; i < m_Districts.Length; i++)
            {
                if (m_Districts[i].households.IsCreated)
                    m_Districts[i].households.Dispose();
            }
            if (m_Districts.IsCreated)
                m_Districts.Dispose();
        }

        // Refactored OnUpdate that splits functionality to helper methods.
        protected override void OnUpdate()
        {
            if (!IsPanelVisible)
                return;

            if (m_SimulationSystem.frameIndex % 256 != 0)
                return;
            

            ResetDistrictEntries();
            BuildDistrictEntries();

            using (var districtMap = BuildDistrictMap())
            {
                ScheduleStatsJob(districtMap);
            }

            // Process age and education data in a parallel job.
            var districtsArray = m_Districts.AsArray();
            var processJob = new ProcessDistrictDataJob
            {
                Districts = districtsArray,
                HouseholdCitizenLookup = GetBufferLookup<HouseholdCitizen>(true),
                CitizenDataLookup = GetComponentLookup<Citizen>(true)
            };
            Dependency = processJob.Schedule(districtsArray.Length, 1, Dependency);
            Dependency.Complete();

            ProcessWealthData();
        }

        // Clears and disposes existing district entries.
        private void ResetDistrictEntries()
        {
            for (int i = 0; i < m_Districts.Length; i++)
            {
                if (m_Districts[i].households.IsCreated)
                    m_Districts[i].households.Dispose();
            }
            m_Districts.Clear();
        }

        // Builds new district entries from the district query.
        private void BuildDistrictEntries()
        {
            using (var districts = m_DistrictQuery.ToEntityArray(Allocator.Temp))
            {
                for (int i = 0; i < districts.Length; i++)
                {
                    m_Districts.Add(new DistrictEntry
                    {
                        district = districts[i],
                        households = new NativeList<Entity>(32, Allocator.Persistent),
                        residentCount = 0,
                        petCount = 0,
                        householdCount = 0,
                        maxHouseholds = 0,
                        ageData = new AgeData(0, 0, 0, 0),
                        educationData = new EducationData(0, 0, 0, 0, 0),
                        wealthKey = default
                    });
                }
            }
        }

        // Builds a NativeHashMap for fast lookup from district entity to index.
        private NativeHashMap<Entity, int> BuildDistrictMap()
        {
            var districtMap = new NativeHashMap<Entity, int>(m_Districts.Length, Allocator.TempJob);
            for (int i = 0; i < m_Districts.Length; i++)
            {
                districtMap.TryAdd(m_Districts[i].district, i);
            }
            return districtMap;
        }

        // Schedules the stats job that processes building and resident data.
        private void ScheduleStatsJob(NativeHashMap<Entity, int> districtMap)
        {
            var jobData = new CountDistrictStatsJob
            {
                m_EntityHandle = SystemAPI.GetEntityTypeHandle(),
                m_CurrentDistrictHandle = SystemAPI.GetComponentTypeHandle<CurrentDistrict>(true),
                m_PrefabRefHandle = SystemAPI.GetComponentTypeHandle<PrefabRef>(true),
                m_AbandonedFromEntity = SystemAPI.GetComponentLookup<Abandoned>(true),
                m_PropertyDataFromEntity = SystemAPI.GetComponentLookup<BuildingPropertyData>(true),
                m_HouseholdFromEntity = SystemAPI.GetComponentLookup<Household>(true),
                m_HouseholdCitizenFromEntity = SystemAPI.GetBufferLookup<HouseholdCitizen>(true),
                m_HouseholdAnimalFromEntity = SystemAPI.GetBufferLookup<HouseholdAnimal>(true),
                m_RenterFromEntity = SystemAPI.GetBufferLookup<Renter>(true),
                m_HappinessData = SystemAPI.GetSingleton<CitizenHappinessParameterData>(),
                m_Districts = m_Districts,
                m_DistrictIndexMap = districtMap
            };

            Dependency = JobChunkExtensions.Schedule(jobData, m_DistrictBuildingQuery, Dependency);
            Dependency.Complete();
        }

        // Process wealth data on the main thread.
        private void ProcessWealthData()
        {
            for (int i = 0; i < m_Districts.Length; i++)
            {
                var district = m_Districts[i];
                district.wealthKey = CitizenUIUtils.GetAverageHouseholdWealth(EntityManager, district.households, SystemAPI.GetSingleton<CitizenHappinessParameterData>());
                m_Districts[i] = district;
            }
        }

        

        public void WriteDistricts(IJsonWriter writer)
        {
            writer.ArrayBegin(m_Districts.Length);
            for (int i = 0; i < m_Districts.Length; i++)
            {
                var district = m_Districts[i];
                writer.TypeBegin("District");
                writer.PropertyName("name");
                m_NameSystem.BindName(writer, district.district);
                writer.PropertyName("residentCount");
                writer.Write(district.residentCount);
                writer.PropertyName("petCount");
                writer.Write(district.petCount);
                writer.PropertyName("householdCount");
                writer.Write(district.householdCount);
                writer.PropertyName("maxHouseholds");
                writer.Write(district.maxHouseholds);
                writer.PropertyName("wealthKey");
                writer.Write(district.wealthKey.ToString());
                writer.PropertyName("educationData");
                WriteEducationData(writer, district.educationData);
                writer.PropertyName("ageData");
                WriteAgeData(writer, district.ageData);
                writer.PropertyName("entity");
                writer.Write(district.district);
                writer.TypeEnd();
            }
            writer.ArrayEnd();
        }

        public void WriteAgeData(IJsonWriter writer, AgeData ageData)
        {
            writer.TypeBegin("AgeData");
            writer.PropertyName("children");
            writer.Write(ageData.children);
            writer.PropertyName("teens");
            writer.Write(ageData.teens);
            writer.PropertyName("adults");
            writer.Write(ageData.adults);
            writer.PropertyName("elders");
            writer.Write(ageData.elders);
            writer.PropertyName("total");
            writer.Write(ageData.children + ageData.teens + ageData.adults + ageData.elders);
            writer.TypeEnd();
        }

        public void WriteEducationData(IJsonWriter writer, EducationData educationData)
        {
            writer.TypeBegin("EducationData");
            writer.PropertyName("uneducated");
            writer.Write(educationData.uneducated);
            writer.PropertyName("poorlyEducated");
            writer.Write(educationData.poorlyEducated);
            writer.PropertyName("educated");
            writer.Write(educationData.educated);
            writer.PropertyName("wellEducated");
            writer.Write(educationData.wellEducated);
            writer.PropertyName("highlyEducated");
            writer.Write(educationData.highlyEducated);
            writer.PropertyName("total");
            writer.Write(educationData.uneducated + educationData.poorlyEducated + educationData.educated + educationData.wellEducated + educationData.highlyEducated);
            writer.TypeEnd();
        }
    }
}