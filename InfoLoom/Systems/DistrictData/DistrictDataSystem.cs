using System;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Areas;
using Game.Common;
using Game.Buildings;
using Game.Citizens;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.UI;
using Game.UI.InGame; // Uses shared definitions for AgeData and EducationData
using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace InfoLoomTwo.Systems.DistrictData
{
    public partial class DistrictDataSystem : SystemBase
    {
        // DistrictEntry uses the shared AgeData and EducationData types.
        private struct DistrictEntry
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
                                
                            }
                        }
                    }
                    m_Districts[districtIndex] = entry;
                }
            }
        }

        // Helper method to calculate AgeData.
        private AgeData GetAgeData(DynamicBuffer<HouseholdCitizen> citizens)
        {
            int children = 0;
            int teens = 0;
            int adults = 0;
            int elders = 0;
            for (int i = 0; i < citizens.Length; i++)
            {
                Entity citizen = citizens[i].m_Citizen;
                if (EntityManager.TryGetComponent<Citizen>(citizen, out var component) &&
                    !CitizenUtils.IsCorpsePickedByHearse(EntityManager, citizen))
                {
                    switch (component.GetAge())
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
                }
            }
            return new AgeData(children, teens, adults, elders);
        }

        // Helper method to calculate EducationData.
        private EducationData GetEducationData(Entity household)
        {
            int uneducated = 0;
            int poorlyEducated = 0;
            int educated = 0;
            int wellEducated = 0;
            int highlyEducated = 0;

            if (EntityManager.HasComponent<HouseholdCitizen>(household))
            {
                DynamicBuffer<HouseholdCitizen> citizens = EntityManager.GetBuffer<HouseholdCitizen>(household);
                for (int i = 0; i < citizens.Length; i++)
                {
                    if (EntityManager.TryGetComponent<Citizen>(citizens[i].m_Citizen, out var component) &&
                        !CitizenUtils.IsCorpsePickedByHearse(EntityManager, citizens[i].m_Citizen))
                    {
                        switch (component.GetEducationLevel())
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
            }
            return new EducationData(uneducated, poorlyEducated, educated, wellEducated, highlyEducated);
        }

        // Normal Entity Queries.
        private EntityQuery m_DistrictBuildingQuery;
        private EntityQuery m_HappinessParameterQuery;
        private EntityQuery m_DistrictQuery;

        private NameSystem m_NameSystem;
        private NativeList<DistrictEntry> m_Districts;
        private SimulationSystem m_SimulationSystem;
        public bool IsPanelVisible { get; set; }
        public bool ForceUpdate { get; private set; }
        public void ForceUpdateOnce() => ForceUpdate = true;

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

            if (m_SimulationSystem.frameIndex % 256 != 0 && !ForceUpdate)
                return;
            ForceUpdate = false;

            ResetDistrictEntries();
            BuildDistrictEntries();
            using (var districtMap = BuildDistrictMap())
            {
                ScheduleStatsJob(districtMap);
            }
            ProcessAgeAndEducationData();
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
                        households = new NativeList<Entity>(32, Allocator.TempJob),
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

        // Processes AgeData, EducationData, and wealth key calculations on the main thread.
        private void ProcessAgeAndEducationData()
        {
            for (int i = 0; i < m_Districts.Length; i++)
            {
                var district = m_Districts[i];
                AgeData ageTotal = new AgeData(0, 0, 0, 0);
                EducationData eduTotal = new EducationData(0, 0, 0, 0, 0);
                for (int j = 0; j < district.households.Length; j++)
                {
                    var household = district.households[j];
                    var citizens = EntityManager.GetBuffer<HouseholdCitizen>(household);
                    ageTotal += GetAgeData(citizens);
                    eduTotal += GetEducationData(household);
                }
                district.ageData = ageTotal;
                district.educationData = eduTotal;
                district.wealthKey = CitizenUIUtils.GetAverageHouseholdWealth(EntityManager, district.households, SystemAPI.GetSingleton<CitizenHappinessParameterData>());
                m_Districts[i] = district;
            }
        }

        private void WriteProperty(IJsonWriter writer, string propertyName, Action writeValue)
        {
            writer.PropertyName(propertyName);
            writeValue();
        }

        public void WriteDistricts(IJsonWriter writer)
        {
            writer.ArrayBegin(m_Districts.Length);
            for (int i = 0; i < m_Districts.Length; i++)
            {
                var district = m_Districts[i];
                writer.TypeBegin("District");
                WriteProperty(writer, "name", () => m_NameSystem.BindName(writer, district.district));
                WriteProperty(writer, "householdCount", () => writer.Write(district.householdCount));
                WriteProperty(writer, "maxHouseholds", () => writer.Write(district.maxHouseholds));
                WriteProperty(writer, "residentCount", () => writer.Write(district.residentCount));
                WriteProperty(writer, "petCount", () => writer.Write(district.petCount));
                WriteProperty(writer, "wealthKey", () => writer.Write(district.wealthKey.ToString()));
                WriteProperty(writer, "ageData", () => WriteAgeData(writer, district.ageData));
                WriteProperty(writer, "educationData", () => WriteEducationData(writer, district.educationData));
                WriteProperty(writer, "entity", () => writer.Write(district.district));
                writer.TypeEnd();
            }
            writer.ArrayEnd();
        }

        public void WriteAgeData(IJsonWriter writer, AgeData ageData)
        {
            writer.TypeBegin("AgeData");
            WriteProperty(writer, "children", () => writer.Write(ageData.children));
            WriteProperty(writer, "teens", () => writer.Write(ageData.teens));
            WriteProperty(writer, "adults", () => writer.Write(ageData.adults));
            WriteProperty(writer, "elders", () => writer.Write(ageData.elders));
            WriteProperty(writer, "total", () => writer.Write(ageData.children + ageData.teens + ageData.adults + ageData.elders));
            writer.TypeEnd();
        }

        public void WriteEducationData(IJsonWriter writer, EducationData educationData)
        {
            writer.TypeBegin("EducationData");
            WriteProperty(writer, "uneducated", () => writer.Write(educationData.uneducated));
            WriteProperty(writer, "poorlyEducated", () => writer.Write(educationData.poorlyEducated));
            WriteProperty(writer, "educated", () => writer.Write(educationData.educated));
            WriteProperty(writer, "wellEducated", () => writer.Write(educationData.wellEducated));
            WriteProperty(writer, "highlyEducated", () => writer.Write(educationData.highlyEducated));
            WriteProperty(writer, "total", () => writer.Write(educationData.uneducated + educationData.poorlyEducated + educationData.educated + educationData.wellEducated + educationData.highlyEducated));
            writer.TypeEnd();
        }
    }
}