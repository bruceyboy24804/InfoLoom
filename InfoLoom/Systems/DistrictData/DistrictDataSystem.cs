using System;
using System.Runtime.CompilerServices;
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
using Game.UI.InGame;
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
            [ReadOnly] public ComponentLookup<HealthProblem> m_HealthLookup;
            [ReadOnly] public ComponentLookup<TravelPurpose> m_TravelLookup;
            [ReadOnly] public ComponentLookup<Citizen> m_CitizenLookup;
            [ReadOnly] public CitizenHappinessParameterData m_HappinessData;

            public NativeList<DistrictEntry> m_Districts;

            bool TryProcessHousehold(ref DistrictEntry entry, Entity household)
            {
                if (!m_HouseholdFromEntity.HasComponent(household) || 
                    !m_HouseholdCitizenFromEntity.TryGetBuffer(household, out var citizens))
                    return false;

                entry.householdCount++;
                entry.households.Add(household);
                entry.residentCount += citizens.Length;

                if (m_HouseholdAnimalFromEntity.TryGetBuffer(household, out var animals))
                {
                    entry.petCount += animals.Length;
                }

                return true;
            }

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entities = chunk.GetNativeArray(m_EntityHandle);
                NativeArray<CurrentDistrict> currentDistricts = chunk.GetNativeArray(ref m_CurrentDistrictHandle);
                NativeArray<PrefabRef> prefabRefs = chunk.GetNativeArray(ref m_PrefabRefHandle);

                // Create temp buffers for age/education counts per district
                var districtAgeData = new NativeArray<int4>(m_Districts.Length, Allocator.Temp);
                var districtEduData = new NativeArray<int4>(m_Districts.Length, Allocator.Temp);
                var districtHighlyEducated = new NativeArray<int>(m_Districts.Length, Allocator.Temp);

                // Initialize arrays with zeros
                for (int i = 0; i < m_Districts.Length; i++)
                {
                    districtAgeData[i] = int4.zero;
                    districtEduData[i] = int4.zero;
                    districtHighlyEducated[i] = 0;
                }

                for (int i = 0; i < chunk.Count; i++)
                {
                    Entity building = entities[i];
                    Entity district = currentDistricts[i].m_District;
                    Entity prefab = prefabRefs[i].m_Prefab;

                    if (m_AbandonedFromEntity.HasComponent(building))
                        continue;

                    if (!m_PropertyDataFromEntity.TryGetComponent(prefab, out var propertyData) || 
                        propertyData.m_ResidentialProperties <= 0)
                        continue;

                    // Find the district entry
                    for (int j = 0; j < m_Districts.Length; j++)
                    {
                        if (m_Districts[j].district.Equals(district))
                        {
                            var entry = m_Districts[j];
                            entry.maxHouseholds += propertyData.m_ResidentialProperties;

                            if (m_RenterFromEntity.TryGetBuffer(building, out var renters))
                            {
                                for (int k = 0; k < renters.Length; k++)
                                {
                                    Entity household = renters[k].m_Renter;
                                    if (TryProcessHousehold(ref entry, household) &&
                                        m_HouseholdCitizenFromEntity.TryGetBuffer(household, out var citizens))
                                    {
                                        // Process all citizens in this household for age/education
                                        for (int c = 0; c < citizens.Length; c++)
                                        {
                                            Entity citizen = citizens[c].m_Citizen;
                                            if (m_CitizenLookup.HasComponent(citizen) && 
                                                m_CitizenLookup.TryGetComponent(citizen, out var component) && 
                                                !CitizenUtils.IsCorpsePickedByHearse(citizen, ref m_HealthLookup, ref m_TravelLookup))
                                            {
                                                var ageData = districtAgeData[j];
                                                switch (component.GetAge())
                                                {
                                                    case CitizenAge.Child: ageData.x++; break;
                                                    case CitizenAge.Teen: ageData.y++; break;
                                                    case CitizenAge.Adult: ageData.z++; break;
                                                    case CitizenAge.Elderly: ageData.w++; break;
                                                }
                                                districtAgeData[j] = ageData;

                                                var eduData = districtEduData[j];
                                                switch (component.GetEducationLevel())
                                                {
                                                    case 0: eduData.x++; break;
                                                    case 1: eduData.y++; break;
                                                    case 2: eduData.z++; break;
                                                    case 3: eduData.w++; break;
                                                    case 4: districtHighlyEducated[j]++; break;
                                                }
                                                districtEduData[j] = eduData;
                                            }
                                        }
                                    }
                                }
                            }

                            m_Districts[j] = entry;
                            break;
                        }
                    }
                }

                // Update the final age and education data for all districts
                for (int i = 0; i < m_Districts.Length; i++)
                {
                    var district = m_Districts[i];
                    var ageData = districtAgeData[i];
                    var eduData = districtEduData[i];
                    
                    district.ageData = new AgeData((int)ageData.x, (int)ageData.y, (int)ageData.z, (int)ageData.w);
                    district.educationData = new EducationData(
                        (int)eduData.x, 
                        (int)eduData.y, 
                        (int)eduData.z, 
                        (int)eduData.w, 
                        districtHighlyEducated[i]
                    );
                    
                    m_Districts[i] = district;
                }

                districtAgeData.Dispose();
                districtEduData.Dispose();
                districtHighlyEducated.Dispose();
            }

            void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
            }
        }

        private static readonly ComponentType[] k_DistrictTypes = new ComponentType[]
        {
            ComponentType.ReadOnly<District>()
        };

        private static readonly ComponentType[] k_DistrictBuildingTypes = new ComponentType[]
        {
            ComponentType.ReadOnly<Building>(),
            ComponentType.ReadOnly<ResidentialProperty>(),
            ComponentType.ReadOnly<PrefabRef>(),
            ComponentType.ReadOnly<Renter>(),
            ComponentType.ReadOnly<CurrentDistrict>()
        };

        private static readonly ComponentType[] k_DistrictBuildingExcludeTypes = new ComponentType[]
        {
            ComponentType.ReadOnly<Temp>(),
            ComponentType.ReadOnly<Deleted>()
        };

        private EntityQuery m_DistrictBuildingQuery;
        private EntityQuery m_HappinessParameterQuery;
        private NameSystem m_NameSystem;
        private NativeList<DistrictEntry> m_Districts;
        private SimulationSystem m_SimulationSystem;
        public bool IsPanelVisible { get; set; }
        public bool ForceUpdate { get; private set; }
        public void ForceUpdateOnce()
        {
            ForceUpdate = true;
        }
        protected override void OnCreate()
        {
            base.OnCreate();
            
            m_DistrictBuildingQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = k_DistrictBuildingTypes,
                None = k_DistrictBuildingExcludeTypes
            });

            m_HappinessParameterQuery = GetEntityQuery(ComponentType.ReadOnly<CitizenHappinessParameterData>());
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

        protected override void OnUpdate()
        {
            if (!IsPanelVisible)
                return;
            if (m_SimulationSystem.frameIndex % 256 != 0 && !ForceUpdate)
                return;
            ForceUpdate = false;
            
           
            for (int i = 0; i < m_Districts.Length; i++)
            {
                if (m_Districts[i].households.IsCreated)
                    m_Districts[i].households.Dispose();
            }
            m_Districts.Clear();

            // Get all districts first and initialize them with zero values
            var districtQuery = GetEntityQuery(ComponentType.ReadOnly<District>(), ComponentType.Exclude<Temp>());
            using (var districts = districtQuery.ToEntityArray(Allocator.Temp))
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

            // Now process buildings and update district data
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
                m_HealthLookup = SystemAPI.GetComponentLookup<HealthProblem>(true),
                m_TravelLookup = SystemAPI.GetComponentLookup<TravelPurpose>(true),
                m_CitizenLookup = SystemAPI.GetComponentLookup<Citizen>(true),
                m_HappinessData = SystemAPI.GetSingleton<CitizenHappinessParameterData>(),
                m_Districts = m_Districts
            };

            this.Dependency = JobChunkExtensions.Schedule(jobData, m_DistrictBuildingQuery, this.Dependency);
            this.Dependency.Complete();

            // Calculate wealth keys after job completes
            var happinessData = SystemAPI.GetSingleton<CitizenHappinessParameterData>();
            for (int i = 0; i < m_Districts.Length; i++)
            {
                var district = m_Districts[i];
                district.wealthKey = CitizenUIUtils.GetAverageHouseholdWealth(EntityManager, district.households, happinessData);
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
                writer.PropertyName("householdCount");
                writer.Write(district.householdCount);
                writer.PropertyName("maxHouseholds");
                writer.Write(district.maxHouseholds);
                writer.PropertyName("residentCount");
                writer.Write(district.residentCount);
                writer.PropertyName("petCount");
                writer.Write(district.petCount);
                writer.PropertyName("wealthKey");
                writer.Write(district.wealthKey.ToString());
                writer.PropertyName("ageData");
                writer.Write(district.ageData);
                writer.PropertyName("educationData");
                writer.Write(district.educationData);
                writer.PropertyName("entity");
                writer.Write(district.district);
                writer.TypeEnd();
            }
            writer.ArrayEnd();
        }
    }
}
