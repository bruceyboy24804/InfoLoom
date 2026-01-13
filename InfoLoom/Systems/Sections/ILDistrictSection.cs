using System;
using Colossal;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game;
using Game.Agents;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.UI.InGame;
using Game.Zones;
using InfoLoomTwo.Extensions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace InfoLoomTwo.Systems.Sections
{
    public partial class ILDistrictSection : ExtendedInfoSectionBase
    {
        

        [BurstCompile]
        private struct CountDistrictPropertiesJob : IJobChunk
        {
            [ReadOnly]
            public Entity m_SelectedEntity;

            [ReadOnly]
            public EntityTypeHandle m_EntityHandle;

            [ReadOnly]
            public ComponentTypeHandle<CurrentDistrict> m_CurrentDistrictHandle;

            [ReadOnly]
            public ComponentTypeHandle<PrefabRef> m_PrefabRefHandle;

            [ReadOnly]
            public ComponentLookup<ResidentialProperty> m_ResidentialPropertyLookup;

            [ReadOnly]
            public ComponentLookup<CommercialProperty> m_CommercialPropertyLookup;

            [ReadOnly]
            public ComponentLookup<IndustrialProperty> m_IndustrialPropertyLookup;

            [ReadOnly]
            public ComponentLookup<OfficeProperty> m_OfficePropertyLookup;

            [ReadOnly]
            public ComponentLookup<StorageProperty> m_StoragePropertyLookup;

            [ReadOnly]
            public ComponentLookup<Abandoned> m_AbandonedLookup;

            [ReadOnly]
            public ComponentLookup<Game.Buildings.Park> m_ParkLookup;

            public NativeCounter.Concurrent m_ResidentialCount;
            public NativeCounter.Concurrent m_CommercialCount;
            public NativeCounter.Concurrent m_IndustrialCount;
            public NativeCounter.Concurrent m_OfficeCount;
            public NativeCounter.Concurrent m_StorageCount;

            
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                
                NativeArray<Entity> entities = chunk.GetNativeArray(m_EntityHandle);
                NativeArray<CurrentDistrict> districts = chunk.GetNativeArray(ref m_CurrentDistrictHandle);
                NativeArray<PrefabRef> prefabRefs = chunk.GetNativeArray(ref m_PrefabRefHandle);
                
                int resCount = 0;
                int comCount = 0;
                int indCount = 0;
                int offCount = 0;
                int storCount = 0;

                for (int i = 0; i < chunk.Count; i++)
                {
                    Entity entity = entities[i];
                    CurrentDistrict district = districts[i];
                    Entity prefab = prefabRefs[i].m_Prefab;

                    if (district.m_District != m_SelectedEntity)
                        continue;

                    if (m_AbandonedLookup.HasComponent(entity) || m_ParkLookup.HasComponent(prefab))
                        continue;

                    // Check once and cache results
                    bool hasStorage = m_StoragePropertyLookup.HasComponent(entity);
                    bool hasOffice = m_OfficePropertyLookup.HasComponent(entity);

                    if (hasStorage)
                    {
                        storCount++;
                    }
                    else if (hasOffice)
                    {
                        offCount++;
                    }
                    else if (m_ResidentialPropertyLookup.HasComponent(entity))
                    {
                        resCount++;
                    }
                    else if (m_CommercialPropertyLookup.HasComponent(entity))
                    {
                        comCount++;
                    }
                    else if (m_IndustrialPropertyLookup.HasComponent(entity))
                    {
                        indCount++;
                    }
                }

                m_ResidentialCount.Increment(resCount);
                m_CommercialCount.Increment(comCount);
                m_IndustrialCount.Increment(indCount);
                m_OfficeCount.Increment(offCount);
                m_StorageCount.Increment(storCount);
            }
        }

        [BurstCompile]
        private struct ProcessDistrictCitizensJob : IJobChunk
        {
            [ReadOnly] public Entity m_SelectedDistrict;
            [ReadOnly] public Entity m_City;
            [ReadOnly] public EducationParameterData m_EducationParameterData;

            [ReadOnly] public EntityTypeHandle m_EntityHandle;
            [ReadOnly] public ComponentTypeHandle<Citizen> m_CitizenHandle;
            [ReadOnly] public ComponentTypeHandle<HouseholdMember> m_HouseholdMemberHandle;

            [ReadOnly] public ComponentLookup<Game.Citizens.Student> m_StudentLookup;
            [ReadOnly] public ComponentLookup<Worker> m_WorkerLookup;
            [ReadOnly] public ComponentLookup<Household> m_HouseholdLookup;
            [ReadOnly] public ComponentLookup<PropertyRenter> m_PropertyRenterLookup;
            [ReadOnly] public ComponentLookup<CurrentDistrict> m_CurrentDistrictLookup;
            [ReadOnly] public ComponentLookup<HealthProblem> m_HealthProblemLookup;
            [ReadOnly] public ComponentLookup<MovingAway> m_MovingAwayLookup;
            [ReadOnly] public BufferLookup<CityModifier> m_CityModifierLookup;

            public NativeCounter.Concurrent m_ElementaryEligible;
            public NativeCounter.Concurrent m_HighSchoolEligible;
            public NativeCounter.Concurrent m_CollegeEligible;
            public NativeCounter.Concurrent m_UniversityEligible;

           
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                
                var entities = chunk.GetNativeArray(m_EntityHandle);
                var citizens = chunk.GetNativeArray(ref m_CitizenHandle);
                var householdMembers = chunk.GetNativeArray(ref m_HouseholdMemberHandle);
                
                int elementary = 0;
                int highSchool = 0;
                int college = 0;
                int university = 0;

                for (int i = 0; i < chunk.Count; i++)
                {
                    Entity citizenEntity = entities[i];
                    Citizen citizen = citizens[i];
                    HouseholdMember householdMember = householdMembers[i];

                    if (!IsInSelectedDistrict(citizenEntity, householdMember))
                        continue;

                    ProcessSchoolEligibility(citizenEntity, citizen, ref elementary, ref highSchool, ref college, ref university);
                }

                m_ElementaryEligible.Increment(elementary);
                m_HighSchoolEligible.Increment(highSchool);
                m_CollegeEligible.Increment(college);
                m_UniversityEligible.Increment(university);
            }

            private bool IsInSelectedDistrict(Entity citizenEntity, HouseholdMember householdMember)
            {
                Entity household = householdMember.m_Household;

                if (!m_HouseholdLookup.TryGetComponent(household, out var householdData))
                    return false;

                if (CitizenUtils.IsDead(citizenEntity, ref m_HealthProblemLookup) ||
                    (householdData.m_Flags & HouseholdFlags.MovedIn) == 0 ||
                    (householdData.m_Flags & HouseholdFlags.Tourist) != 0 ||
                    m_MovingAwayLookup.HasComponent(household))
                    return false;

                if (!m_PropertyRenterLookup.TryGetComponent(household, out var propertyRenter))
                    return false;

                if (!m_CurrentDistrictLookup.TryGetComponent(propertyRenter.m_Property, out var currentDistrict))
                    return false;

                return currentDistrict.m_District == m_SelectedDistrict;
            }

            private void ProcessSchoolEligibility(Entity citizen, Citizen citizenData, ref int elementary, ref int highSchool, ref int college, ref int university)
            {
                CitizenAge age = citizenData.GetAge();

                if (m_StudentLookup.TryGetComponent(citizen, out var student) && student.m_School != Entity.Null)
                {
                    // Current student
                    switch (student.m_Level)
                    {
                        case 1: elementary++; break;
                        case 2: highSchool++; break;
                        case 3: college++; break;
                        case 4: university++; break;
                    }
                }
                else
                {
                    // Potential student
                    var cityModifiers = m_CityModifierLookup[m_City];
                    float willingness = citizenData.GetPseudoRandom(CitizenPseudoRandom.StudyWillingness).NextFloat();
                    bool hasWorker = m_WorkerLookup.HasComponent(citizen);

                    if (age == CitizenAge.Child)
                    {
                        elementary++;
                    }
                    else if (citizenData.GetEducationLevel() == 1 && age <= CitizenAge.Adult)
                    {
                        float probability = ApplyToSchoolSystem.GetEnteringProbability(
                            age, hasWorker, 2, (int)citizenData.m_WellBeing, willingness, cityModifiers, ref m_EducationParameterData);
                        highSchool += (int)math.ceil(probability);
                    }
                    else if (citizenData.GetEducationLevel() == 2 && citizenData.GetFailedEducationCount() < 3)
                    {
                        float universityProb = ApplyToSchoolSystem.GetEnteringProbability(
                            age, hasWorker, 4, (int)citizenData.m_WellBeing, willingness, cityModifiers, ref m_EducationParameterData);
                        university += (int)math.ceil(universityProb);

                        float collegeProb = (1f - universityProb) * ApplyToSchoolSystem.GetEnteringProbability(
                            age, hasWorker, 3, (int)citizenData.m_WellBeing, willingness, cityModifiers, ref m_EducationParameterData);
                        college += (int)math.ceil(collegeProb);
                    }
                }
            }
        }

        [BurstCompile]
        private struct ProcessSchoolCapacityJob : IJobChunk
        {
            
            [ReadOnly] public Entity m_SelectedEntity;
            [ReadOnly] public EntityTypeHandle m_EntityHandle;
            [ReadOnly] public ComponentTypeHandle<CurrentDistrict> m_CurrentDistrictHandle;
            [ReadOnly] public ComponentTypeHandle<PrefabRef> m_PrefabRefHandle;

            [ReadOnly] public ComponentLookup<SchoolData> m_SchoolDataLookup;
            [ReadOnly] public ComponentLookup<PrefabRef> m_PrefabRefLookup;
            [ReadOnly] public BufferLookup<Efficiency> m_EfficiencyLookup;
            [ReadOnly] public BufferLookup<InstalledUpgrade> m_InstalledUpgradeLookup;

            public NativeCounter.Concurrent m_ElementaryCapacity;
            public NativeCounter.Concurrent m_HighSchoolCapacity;
            public NativeCounter.Concurrent m_CollegeCapacity;
            public NativeCounter.Concurrent m_UniversityCapacity;

           
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(m_EntityHandle);
                var districts = chunk.GetNativeArray(ref m_CurrentDistrictHandle);
                var prefabRefs = chunk.GetNativeArray(ref m_PrefabRefHandle);
                
                int elemCap = 0, hsCap = 0, collCap = 0, uniCap = 0;

                for (int i = 0; i < chunk.Count; i++)
                {
                    Entity building = entities[i];
                    CurrentDistrict district = districts[i];
                    Entity prefab = prefabRefs[i].m_Prefab;

                    if (district.m_District != m_SelectedEntity)
                        continue;

                    if (!m_EfficiencyLookup.TryGetBuffer(building, out var efficiency) ||
                        !m_SchoolDataLookup.TryGetComponent(prefab, out var schoolData) ||
                        BuildingUtils.GetEfficiency(efficiency) <= 0.0)
                        continue;

                    if (m_InstalledUpgradeLookup.TryGetBuffer(building, out var upgrades))
                    {
                        UpgradeUtils.CombineStats(ref schoolData, upgrades, ref m_PrefabRefLookup, ref m_SchoolDataLookup);
                    }

                    switch (schoolData.m_EducationLevel)
                    {
                        case 1: elemCap += schoolData.m_StudentCapacity; break;
                        case 2: hsCap += schoolData.m_StudentCapacity; break;
                        case 3: collCap += schoolData.m_StudentCapacity; break;
                        case 4: uniCap += schoolData.m_StudentCapacity; break;
                    }
                }

                m_ElementaryCapacity.Increment(elemCap);
                m_HighSchoolCapacity.Increment(hsCap);
                m_CollegeCapacity.Increment(collCap);
                m_UniversityCapacity.Increment(uniCap);
            }
        }

        [BurstCompile]
        private struct ProcessSchoolStudentsJob : IJobChunk
        {
            [ReadOnly] public Entity m_SelectedEntity;
            [ReadOnly] public EntityTypeHandle m_EntityHandle;
            [ReadOnly] public ComponentTypeHandle<CurrentDistrict> m_CurrentDistrictHandle;
            [ReadOnly] public ComponentTypeHandle<PrefabRef> m_PrefabRefHandle;

            [ReadOnly] public ComponentLookup<SchoolData> m_SchoolDataLookup;
            [ReadOnly] public BufferLookup<Game.Buildings.Student> m_StudentLookup;
            [ReadOnly] public BufferLookup<Efficiency> m_EfficiencyLookup;

            public NativeCounter.Concurrent m_ElementaryStudents;
            public NativeCounter.Concurrent m_HighSchoolStudents;
            public NativeCounter.Concurrent m_CollegeStudents;
            public NativeCounter.Concurrent m_UniversityStudents;

            
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(m_EntityHandle);
                var districts = chunk.GetNativeArray(ref m_CurrentDistrictHandle);
                var prefabRefs = chunk.GetNativeArray(ref m_PrefabRefHandle);
                
                int elemStud = 0, hsStud = 0, collStud = 0, uniStud = 0;

                for (int i = 0; i < chunk.Count; i++)
                {
                    Entity building = entities[i];
                    CurrentDistrict district = districts[i];
                    Entity prefab = prefabRefs[i].m_Prefab;

                    if (district.m_District != m_SelectedEntity)
                        continue;

                    if (!m_StudentLookup.TryGetBuffer(building, out var students) ||
                        !m_EfficiencyLookup.TryGetBuffer(building, out var efficiency) ||
                        !m_SchoolDataLookup.TryGetComponent(prefab, out var schoolData) ||
                        BuildingUtils.GetEfficiency(efficiency) <= 0.0)
                        continue;

                    switch (schoolData.m_EducationLevel)
                    {
                        case 1: elemStud += students.Length; break;
                        case 2: hsStud += students.Length; break;
                        case 3: collStud += students.Length; break;
                        case 4: uniStud += students.Length; break;
                    }
                }

                m_ElementaryStudents.Increment(elemStud);
                m_HighSchoolStudents.Increment(hsStud);
                m_CollegeStudents.Increment(collStud);
                m_UniversityStudents.Increment(uniStud);
            }
        }

        protected override string group => nameof(ILDistrictSection);
        
        private int _NoOfResProperties;
        private int _NoOfComProperties;
        private int _NoOfIndProperties;
        private int _NoOfOffProperties;
        private int _NoOfStorageProperties;
        private bool _IsDistrict;
        private float _AverageLandValue;  // Add this
        private float _AverageBuildingLevel; // Add this

        // School eligibility
        private int _ElementaryEligible;
        private int _HighSchoolEligible;
        private int _CollegeEligible;
        private int _UniversityEligible;

        // School capacity and students
        private int _ElementaryCapacity;
        private int _HighSchoolCapacity;
        private int _CollegeCapacity;
        private int _UniversityCapacity;

        private int _ElementaryStudents;
        private int _HighSchoolStudents;
        private int _CollegeStudents;
        private int _UniversityStudents;
        
       
        private bool hasSchool;
        private CitySystem m_CitySystem;
        public static readonly int kUpdatesPerDay = 1;
        
        private float _Geometry;
        private int residentCount { get; set; }
        private NativeArray<int> m_Results;
        private float _PopulationDensity;
        private NativeList<Entity> m_HouseholdsResult;
        
        protected override void OnCreate()
        {
            base.OnCreate();
            m_InfoUISystem.AddMiddleSection(this);
            m_CitySystem = World.GetOrCreateSystemManaged<CitySystem>();
            m_Results = new NativeArray<int>(2, Allocator.Persistent);
            m_HouseholdsResult = new NativeList<Entity>(Allocator.Persistent);

        }
        protected override void OnDestroy()
	    {
            m_HouseholdsResult.Dispose();
		    m_Results.Dispose();
		    base.OnDestroy();
	    }
        protected override void Reset()
        {
            _NoOfResProperties = 0;
            _NoOfComProperties = 0;
            _NoOfIndProperties = 0;
            _NoOfOffProperties = 0;
            _NoOfStorageProperties = 0;
            _IsDistrict = false;
            _AverageLandValue = 0f;  // Add this
            _AverageBuildingLevel = 0f; // Add this

            _ElementaryEligible = 0;
            _HighSchoolEligible = 0;
            _CollegeEligible = 0;
            _UniversityEligible = 0;

            _ElementaryCapacity = 0;
            _HighSchoolCapacity = 0;
            _CollegeCapacity = 0;
            _UniversityCapacity = 0;

            _ElementaryStudents = 0;
            _HighSchoolStudents = 0;
            _CollegeStudents = 0;
            _UniversityStudents = 0;
            m_HouseholdsResult.Clear();
            m_Results[0] = 0;
            m_Results[1] = 0;
        }


        private bool Visible()
        {
            _IsDistrict = false;
	        if (base.EntityManager.HasComponent<District>(selectedEntity) && base.EntityManager.HasComponent<Area>(selectedEntity) && !Mod.setting.hideDistrictSection)
	        {
		        return true;
	        }
	        return _IsDistrict;
        }
        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
	        return 262144 / (kUpdatesPerDay * 16);
        }
        protected override void OnUpdate()
        {
            visible = Visible();
            // Property counting job

            var resCounter = new NativeCounter(Allocator.TempJob);
            var comCounter = new NativeCounter(Allocator.TempJob);
            var indCounter = new NativeCounter(Allocator.TempJob);
            var offCounter = new NativeCounter(Allocator.TempJob);
            var storCounter = new NativeCounter(Allocator.TempJob);

            var propertyJob = new CountDistrictPropertiesJob
            {
                m_SelectedEntity = selectedEntity,
                m_EntityHandle = SystemAPI.GetEntityTypeHandle(),
                m_CurrentDistrictHandle = SystemAPI.GetComponentTypeHandle<CurrentDistrict>(isReadOnly: true),
                m_PrefabRefHandle = SystemAPI.GetComponentTypeHandle<PrefabRef>(isReadOnly: true),
                m_ResidentialPropertyLookup = SystemAPI.GetComponentLookup<ResidentialProperty>(isReadOnly: true),
                m_CommercialPropertyLookup = SystemAPI.GetComponentLookup<CommercialProperty>(isReadOnly: true),
                m_IndustrialPropertyLookup = SystemAPI.GetComponentLookup<IndustrialProperty>(isReadOnly: true),
                m_OfficePropertyLookup = SystemAPI.GetComponentLookup<OfficeProperty>(isReadOnly: true),
                m_StoragePropertyLookup = SystemAPI.GetComponentLookup<StorageProperty>(isReadOnly: true),
                m_AbandonedLookup = SystemAPI.GetComponentLookup<Abandoned>(isReadOnly: true),
                m_ParkLookup = SystemAPI.GetComponentLookup<Game.Buildings.Park>(isReadOnly: true),
                m_ResidentialCount = resCounter.ToConcurrent(),
                m_CommercialCount = comCounter.ToConcurrent(),
                m_IndustrialCount = indCounter.ToConcurrent(),
                m_OfficeCount = offCounter.ToConcurrent(),
                m_StorageCount = storCounter.ToConcurrent(),
                
            };

            var buildingQuery = SystemAPI.QueryBuilder()
                .WithAll<Building, CurrentDistrict, PrefabRef>()
                .Build();

            // Land value calculation job
            var landValueResults = new NativeArray<float>(2, Allocator.TempJob);
            
            var landValueJob = new CalculateDistrictLandValueJob
            {
                m_SelectedEntity = selectedEntity,
                m_EntityHandle = SystemAPI.GetEntityTypeHandle(),
                m_BuildingHandle = SystemAPI.GetComponentTypeHandle<Building>(isReadOnly: true),
                m_CurrentDistrictHandle = SystemAPI.GetComponentTypeHandle<CurrentDistrict>(isReadOnly: true),
                m_LandValueLookup = SystemAPI.GetComponentLookup<LandValue>(isReadOnly: true),
                m_Results = landValueResults
            };

            // Building level calculation job
            var buildingLevelResults = new NativeArray<int>(2, Allocator.TempJob);
            
            var buildingLevelJob = new CalculateBuildingLevelsJob
            {
                m_SelectedEntity = selectedEntity,
                m_EntityHandle = SystemAPI.GetEntityTypeHandle(),
                m_CurrentDistrictHandle = SystemAPI.GetComponentTypeHandle<CurrentDistrict>(isReadOnly: true),
                m_PrefabRefHandle = SystemAPI.GetComponentTypeHandle<PrefabRef>(isReadOnly: true),
                m_SpawnableBuildingDataLookup = SystemAPI.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true),
                m_AbandonedLookup = SystemAPI.GetComponentLookup<Abandoned>(isReadOnly: true),
                m_ParkLookup = SystemAPI.GetComponentLookup<Game.Buildings.Park>(isReadOnly: true),
                m_Results = buildingLevelResults
            };

            // School capacity job
            var elemCapCounter = new NativeCounter(Allocator.TempJob);
            var hsCapCounter = new NativeCounter(Allocator.TempJob);
            var collCapCounter = new NativeCounter(Allocator.TempJob);
            var uniCapCounter = new NativeCounter(Allocator.TempJob);

            var capacityJob = new ProcessSchoolCapacityJob
            {
                m_SelectedEntity = selectedEntity,
                m_EntityHandle = SystemAPI.GetEntityTypeHandle(),
                m_CurrentDistrictHandle = SystemAPI.GetComponentTypeHandle<CurrentDistrict>(isReadOnly: true),
                m_PrefabRefHandle = SystemAPI.GetComponentTypeHandle<PrefabRef>(isReadOnly: true),
                m_SchoolDataLookup = SystemAPI.GetComponentLookup<SchoolData>(isReadOnly: true),
                m_PrefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(isReadOnly: true),
                m_EfficiencyLookup = SystemAPI.GetBufferLookup<Efficiency>(isReadOnly: true),
                m_InstalledUpgradeLookup = SystemAPI.GetBufferLookup<InstalledUpgrade>(isReadOnly: true),
                m_ElementaryCapacity = elemCapCounter.ToConcurrent(),
                m_HighSchoolCapacity = hsCapCounter.ToConcurrent(),
                m_CollegeCapacity = collCapCounter.ToConcurrent(),
                m_UniversityCapacity = uniCapCounter.ToConcurrent(),
            };

            // School students job
            var elemStudCounter = new NativeCounter(Allocator.TempJob);
            var hsStudCounter = new NativeCounter(Allocator.TempJob);
            var collStudCounter = new NativeCounter(Allocator.TempJob);
            var uniStudCounter = new NativeCounter(Allocator.TempJob);

            var studentsJob = new ProcessSchoolStudentsJob
            {
                m_SelectedEntity = selectedEntity,
                m_EntityHandle = SystemAPI.GetEntityTypeHandle(),
                m_CurrentDistrictHandle = SystemAPI.GetComponentTypeHandle<CurrentDistrict>(isReadOnly: true),
                m_PrefabRefHandle = SystemAPI.GetComponentTypeHandle<PrefabRef>(isReadOnly: true),
                m_SchoolDataLookup = SystemAPI.GetComponentLookup<SchoolData>(isReadOnly: true),
                m_StudentLookup = SystemAPI.GetBufferLookup<Game.Buildings.Student>(isReadOnly: true),
                m_EfficiencyLookup = SystemAPI.GetBufferLookup<Efficiency>(isReadOnly: true),
                m_ElementaryStudents = elemStudCounter.ToConcurrent(),
                m_HighSchoolStudents = hsStudCounter.ToConcurrent(),
                m_CollegeStudents = collStudCounter.ToConcurrent(),
                m_UniversityStudents = uniStudCounter.ToConcurrent(),
            };

            // Citizen eligibility job
            var elementaryCounter = new NativeCounter(Allocator.TempJob);
            var highSchoolCounter = new NativeCounter(Allocator.TempJob);
            var collegeCounter = new NativeCounter(Allocator.TempJob);
            var universityCounter = new NativeCounter(Allocator.TempJob);
            
            var citizenJob = new ProcessDistrictCitizensJob
            {
                m_SelectedDistrict = selectedEntity,
                m_City = m_CitySystem.City,
                m_EducationParameterData = SystemAPI.GetSingleton<EducationParameterData>(),
                m_EntityHandle = SystemAPI.GetEntityTypeHandle(),
                m_CitizenHandle = SystemAPI.GetComponentTypeHandle<Citizen>(isReadOnly: true),
                m_HouseholdMemberHandle = SystemAPI.GetComponentTypeHandle<HouseholdMember>(isReadOnly: true),
                m_StudentLookup = SystemAPI.GetComponentLookup<Game.Citizens.Student>(isReadOnly: true),
                m_WorkerLookup = SystemAPI.GetComponentLookup<Worker>(isReadOnly: true),
                m_HouseholdLookup = SystemAPI.GetComponentLookup<Household>(isReadOnly: true),
                m_PropertyRenterLookup = SystemAPI.GetComponentLookup<PropertyRenter>(isReadOnly: true),
                m_CurrentDistrictLookup = SystemAPI.GetComponentLookup<CurrentDistrict>(isReadOnly: true),
                m_HealthProblemLookup = SystemAPI.GetComponentLookup<HealthProblem>(isReadOnly: true),
                m_MovingAwayLookup = SystemAPI.GetComponentLookup<MovingAway>(isReadOnly: true),
                m_CityModifierLookup = SystemAPI.GetBufferLookup<CityModifier>(isReadOnly: true),
                m_ElementaryEligible = elementaryCounter.ToConcurrent(),
                m_HighSchoolEligible = highSchoolCounter.ToConcurrent(),
                m_CollegeEligible = collegeCounter.ToConcurrent(),
                m_UniversityEligible = universityCounter.ToConcurrent(),
            };
           
            
            var job = new CountDistrictHouseholdsJob()
            {
                m_SelectedEntity = selectedEntity,
                m_EntityHandle = SystemAPI.GetEntityTypeHandle(),
                m_CurrentDistrictHandle = SystemAPI.GetComponentTypeHandle<CurrentDistrict>(isReadOnly: true),
                m_PrefabRefHandle = SystemAPI.GetComponentTypeHandle<PrefabRef>(isReadOnly: true),
                m_ParkLookup = SystemAPI.GetComponentLookup<Game.Buildings.Park>(isReadOnly: true),
                m_AbandonedLookup = SystemAPI.GetComponentLookup<Abandoned>(isReadOnly: true),
                m_HealthProblemLookup = SystemAPI.GetComponentLookup<HealthProblem>(isReadOnly: true),
                m_TravelPurposeLookup = SystemAPI.GetComponentLookup<TravelPurpose>(isReadOnly: true),
                m_PropertyDataLookup = SystemAPI.GetComponentLookup<BuildingPropertyData>(isReadOnly: true),
                m_HouseholdLookup = SystemAPI.GetComponentLookup<Household>(isReadOnly: true),
                m_HouseholdCitizenLookup = SystemAPI.GetBufferLookup<HouseholdCitizen>(isReadOnly: true),
                m_HouseholdAnimalLookup = SystemAPI.GetBufferLookup<HouseholdAnimal>(isReadOnly: true),
                m_RenterLookup = SystemAPI.GetBufferLookup<Renter>(isReadOnly: true),
                m_Results = m_Results,
                m_HouseholdsResult = m_HouseholdsResult
            };

            EntityQuery m_DistrictBuildingQuery = SystemAPI.QueryBuilder()
                .WithAll<Building, ResidentialProperty, PrefabRef, Renter, CurrentDistrict>()
                .WithNone<Temp, Deleted>()
                .Build();

            JobChunkExtensions.Schedule(job, m_DistrictBuildingQuery, base.Dependency).Complete();

            
               

            EntityQuery citizenQuery = SystemAPI.QueryBuilder().WithAll<Citizen, HouseholdMember>().WithNone<Temp, Deleted>().Build();

            // Schedule jobs with explicit dependencies for maximum parallelization
            // All building jobs can run in parallel, then citizen job depends on all of them
            JobHandle propertyHandle = JobChunkExtensions.ScheduleParallel(propertyJob, buildingQuery, Dependency);
            JobHandle landValueHandle = JobChunkExtensions.ScheduleParallel(landValueJob, buildingQuery, Dependency);
            JobHandle buildingLevelHandle = JobChunkExtensions.ScheduleParallel(buildingLevelJob, buildingQuery, Dependency);
            JobHandle capacityHandle = JobChunkExtensions.ScheduleParallel(capacityJob, buildingQuery, Dependency);
            JobHandle studentsHandle = JobChunkExtensions.ScheduleParallel(studentsJob, buildingQuery, Dependency);
            
            // Combine all building job handles
            JobHandle allBuildingJobs = JobHandle.CombineDependencies(propertyHandle, landValueHandle, buildingLevelHandle);
            allBuildingJobs = JobHandle.CombineDependencies(allBuildingJobs, capacityHandle, studentsHandle);
            
            // Citizen job runs independently (different query, no data conflicts)
            JobHandle citizenHandle = JobChunkExtensions.ScheduleParallel(citizenJob, citizenQuery, Dependency);
            
            // Combine all jobs for final dependency
            Dependency = JobHandle.CombineDependencies(allBuildingJobs, citizenHandle);
            
            // Complete all jobs before reading results
            Dependency.Complete();

            // Calculate average land value
            float buildingCount = landValueResults[1];
            _AverageLandValue = (buildingCount > 0f) ? (landValueResults[0] / buildingCount) : 0f;

            // Calculate average building level
            float levelBuildingCount = buildingLevelResults[1];
            _AverageBuildingLevel = (levelBuildingCount > 0) ? ((float)buildingLevelResults[0] / levelBuildingCount) : 0f;

            _NoOfResProperties = resCounter.Count;
            _NoOfComProperties = comCounter.Count;
            _NoOfIndProperties = indCounter.Count;
            _NoOfOffProperties = offCounter.Count;
            _NoOfStorageProperties = storCounter.Count;

            _ElementaryCapacity = elemCapCounter.Count;
            _HighSchoolCapacity = hsCapCounter.Count;
            _CollegeCapacity = collCapCounter.Count;
            _UniversityCapacity = uniCapCounter.Count;

            _ElementaryStudents = elemStudCounter.Count;
            _HighSchoolStudents = hsStudCounter.Count;
            _CollegeStudents = collStudCounter.Count;
            _UniversityStudents = uniStudCounter.Count;

            _ElementaryEligible = elementaryCounter.Count;
            _HighSchoolEligible = highSchoolCounter.Count;
            _CollegeEligible = collegeCounter.Count;
            _UniversityEligible = universityCounter.Count;
            
            // Dispose all TempJob allocations
            landValueResults.Dispose();
            buildingLevelResults.Dispose();
            resCounter.Dispose();
            comCounter.Dispose();
            indCounter.Dispose();
            offCounter.Dispose();
            storCounter.Dispose();
            elemCapCounter.Dispose();
            hsCapCounter.Dispose();
            collCapCounter.Dispose();
            uniCapCounter.Dispose();
            elemStudCounter.Dispose();
            hsStudCounter.Dispose();
            collStudCounter.Dispose();
            uniStudCounter.Dispose();
            elementaryCounter.Dispose();
            highSchoolCounter.Dispose();
            collegeCounter.Dispose();
            universityCounter.Dispose();
        }
        protected override void OnProcess()
        {
            hasSchool = false;

            if (selectedEntity != Entity.Null)
            {
                EntityQuery schoolQuery = SystemAPI.QueryBuilder()
                    .WithAll<Game.Buildings.School, CurrentDistrict>()
                    .Build();

                NativeArray<CurrentDistrict> districts = schoolQuery.ToComponentDataArray<CurrentDistrict>(Allocator.Temp);

                for (int i = 0; i < districts.Length; i++)
                {
                    if (districts[i].m_District == selectedEntity)
                    {
                        hasSchool = true;
                        break;
                    }
                }
                districts.Dispose();

                EntityManager.TryGetComponent<Geometry>(selectedEntity, out var geometry);
                _Geometry = geometry.m_SurfaceArea;
                residentCount = m_Results[1];
                
                // Convert square meters to square kilometers (divide by 1,000,000)
                float areaInSqKm = _Geometry / 1000000f;
                _PopulationDensity = (areaInSqKm > 0) ? (residentCount / areaInSqKm) : 0f;
            }
        }

        
        public override void OnWriteProperties(IJsonWriter writer)
        {
            writer.PropertyName("HideDistrictSection");
            writer.Write(Mod.setting.hideDistrictSection);
            writer.PropertyName("NoOfResProperties");
            writer.Write(_NoOfResProperties);
            writer.PropertyName("NoOfComProperties");
            writer.Write(_NoOfComProperties);
            writer.PropertyName("NoOfIndProperties");
            writer.Write(_NoOfIndProperties);
            writer.PropertyName("NoOfOffProperties");
            writer.Write(_NoOfOffProperties);
            writer.PropertyName("NoOfStorageProperties");
            writer.Write(_NoOfStorageProperties);
            writer.PropertyName("AverageLandValue");
            writer.Write(_AverageLandValue);
            writer.PropertyName("AverageBuildingLevel");
            writer.Write(Mathf.RoundToInt(_AverageBuildingLevel));

            writer.PropertyName("ElementaryCapacity");
            writer.Write(_ElementaryCapacity);
            writer.PropertyName("HighSchoolCapacity");
            writer.Write(_HighSchoolCapacity);
            writer.PropertyName("CollegeCapacity");
            writer.Write(_CollegeCapacity);
            writer.PropertyName("UniversityCapacity");
            writer.Write(_UniversityCapacity);

            writer.PropertyName("ElementaryStudents");
            writer.Write(_ElementaryStudents);
            writer.PropertyName("HighSchoolStudents");
            writer.Write(_HighSchoolStudents);
            writer.PropertyName("CollegeStudents");
            writer.Write(_CollegeStudents);
            writer.PropertyName("UniversityStudents");
            writer.Write(_UniversityStudents);

            writer.PropertyName("ElementaryEligible");
            writer.Write(_ElementaryEligible);
            writer.PropertyName("HighSchoolEligible");
            writer.Write(_HighSchoolEligible);
            writer.PropertyName("CollegeEligible");
            writer.Write(_CollegeEligible);
            writer.PropertyName("UniversityEligible");
            writer.Write(_UniversityEligible);
            
            writer.PropertyName("hasSchool");
            writer.Write(hasSchool);
            writer.PropertyName("Geometry");
            writer.Write(_Geometry);
            writer.PropertyName("PopulationDensity");
            writer.Write(_PopulationDensity);
        }
        [BurstCompile]
        private struct CalculateDistrictLandValueJob : IJobChunk
        {
            [ReadOnly]
            public Entity m_SelectedEntity;

            [ReadOnly]
            public EntityTypeHandle m_EntityHandle;

            [ReadOnly]
            public ComponentTypeHandle<Building> m_BuildingHandle;

            [ReadOnly]
            public ComponentTypeHandle<CurrentDistrict> m_CurrentDistrictHandle;

            [ReadOnly]
            public ComponentLookup<LandValue> m_LandValueLookup;

            public NativeArray<float> m_Results;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(m_EntityHandle);
                var buildings = chunk.GetNativeArray(ref m_BuildingHandle);
                var districts = chunk.GetNativeArray(ref m_CurrentDistrictHandle);

                float sumLandValue = 0f;
                float count = 0f;

                for (int i = 0; i < chunk.Count; i++)
                {
                    CurrentDistrict district = districts[i];
                    
                    if (district.m_District != m_SelectedEntity)
                        continue;

                    Building building = buildings[i];
                    
                    if (m_LandValueLookup.HasComponent(building.m_RoadEdge))
                    {
                        LandValue landValue = m_LandValueLookup[building.m_RoadEdge];
                        sumLandValue += landValue.m_LandValue;
                        count += 1f;
                    }
                }

                // Atomic write at the end
                if (count > 0f)
                {
                    m_Results[0] += sumLandValue;
                    m_Results[1] += count;
                }
            }
        }

        [BurstCompile]
        private struct CalculateBuildingLevelsJob : IJobChunk
        {
            [ReadOnly]
            public Entity m_SelectedEntity;

            [ReadOnly]
            public EntityTypeHandle m_EntityHandle;

            [ReadOnly]
            public ComponentTypeHandle<CurrentDistrict> m_CurrentDistrictHandle;

            [ReadOnly]
            public ComponentTypeHandle<PrefabRef> m_PrefabRefHandle;

            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingDataLookup;

            [ReadOnly]
            public ComponentLookup<Abandoned> m_AbandonedLookup;

            [ReadOnly]
            public ComponentLookup<Game.Buildings.Park> m_ParkLookup;

            public NativeArray<int> m_Results; // [0] = sum of levels, [1] = count

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(m_EntityHandle);
                var districts = chunk.GetNativeArray(ref m_CurrentDistrictHandle);
                var prefabRefs = chunk.GetNativeArray(ref m_PrefabRefHandle);

                int sumLevels = 0;
                int count = 0;

                for (int i = 0; i < chunk.Count; i++)
                {
                    Entity entity = entities[i];
                    CurrentDistrict district = districts[i];
                    Entity prefab = prefabRefs[i].m_Prefab;

                    if (district.m_District != m_SelectedEntity)
                        continue;

                    // Skip abandoned buildings and parks
                    if (m_AbandonedLookup.HasComponent(entity) || m_ParkLookup.HasComponent(prefab))
                        continue;

                    // Get building level from SpawnableBuildingData
                    if (m_SpawnableBuildingDataLookup.TryGetComponent(prefab, out var spawnableData))
                    {
                        sumLevels += spawnableData.m_Level;
                        count++;
                    }
                }

                // Atomic write at the end
                if (count > 0)
                {
                    m_Results[0] += sumLevels;
                    m_Results[1] += count;
                }
            }
        }
        [BurstCompile]
        public struct CountDistrictHouseholdsJob : IJobChunk
	    {
		    [ReadOnly]
		    public Entity m_SelectedEntity;

		    [ReadOnly]
		    public EntityTypeHandle m_EntityHandle;

		    [ReadOnly]
		    public ComponentTypeHandle<CurrentDistrict> m_CurrentDistrictHandle;

		    [ReadOnly]
		    public ComponentTypeHandle<PrefabRef> m_PrefabRefHandle;

		    [ReadOnly]
		    public ComponentLookup<Game.Buildings.Park> m_ParkLookup;

		    [ReadOnly]
		    public ComponentLookup<Abandoned> m_AbandonedLookup;

		    [ReadOnly]
		    public ComponentLookup<HealthProblem> m_HealthProblemLookup;

		    [ReadOnly]
		    public ComponentLookup<TravelPurpose> m_TravelPurposeLookup;

		    [ReadOnly]
		    public ComponentLookup<BuildingPropertyData> m_PropertyDataLookup;

		    [ReadOnly]
		    public ComponentLookup<Household> m_HouseholdLookup;

		    [ReadOnly]
		    public BufferLookup<HouseholdCitizen> m_HouseholdCitizenLookup;

		    [ReadOnly]
		    public BufferLookup<HouseholdAnimal> m_HouseholdAnimalLookup;

		    [ReadOnly]
		    public BufferLookup<Renter> m_RenterLookup;

		    public NativeArray<int> m_Results;

		    public NativeList<Entity> m_HouseholdsResult;

		    public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		    {
			    NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityHandle);
			    NativeArray<CurrentDistrict> nativeArray2 = chunk.GetNativeArray(ref m_CurrentDistrictHandle);
			    NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefHandle);
			    int num = 0;
			    int residentCount = 0;
			    for (int i = 0; i < nativeArray.Length; i++)
			    {
				    Entity entity = nativeArray[i];
				    CurrentDistrict currentDistrict = nativeArray2[i];
				    PrefabRef prefabRef = nativeArray3[i];
				    if (!(currentDistrict.m_District != m_SelectedEntity) && TryCountHouseholds(ref residentCount, entity, prefabRef.m_Prefab, ref m_ParkLookup, ref m_AbandonedLookup, ref m_PropertyDataLookup, ref m_HealthProblemLookup, ref m_TravelPurposeLookup, ref m_HouseholdLookup, ref m_RenterLookup, ref m_HouseholdCitizenLookup, ref m_HouseholdAnimalLookup, m_HouseholdsResult))
				    {
					    num = 1;
				    }
			    }
			    m_Results[0] += num;
			    m_Results[1] += residentCount;
		    }

		    void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		    {
			    Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		    }
	    }
        private static bool TryCountHouseholds(ref int residentCount, Entity entity, Entity prefab, ref ComponentLookup<Game.Buildings.Park> parkLookup, ref ComponentLookup<Abandoned> abandonedLookup, ref ComponentLookup<BuildingPropertyData> propertyDataLookup, ref ComponentLookup<HealthProblem> healthProblemLookup, ref ComponentLookup<TravelPurpose> travelPurposeLookup, ref ComponentLookup<Household> householdLookup, ref BufferLookup<Renter> renterLookup, ref BufferLookup<HouseholdCitizen> householdCitizenLookup, ref BufferLookup<HouseholdAnimal> householdAnimalLookup, NativeList<Entity> householdsResult)
        {
            bool result = false;
            bool isAbandoned = abandonedLookup.HasComponent(entity);

            DynamicBuffer<Renter> renterBuffer;
            bool hasRenters = renterLookup.TryGetBuffer(entity, out renterBuffer) && renterBuffer.Length > 0;
            bool isPark = parkLookup.HasComponent(entity);

            BuildingPropertyData componentData;
            bool hasResidential = propertyDataLookup.TryGetComponent(prefab, out componentData) && componentData.m_ResidentialProperties > 0 && !isAbandoned;
            bool parkOrAbandonedWithRenters = (isPark || isAbandoned) && hasRenters;

            if (hasResidential || parkOrAbandonedWithRenters)
            {
                result = true;

                // Only update residentCount here; leave other refs untouched.
                for (int i = 0; i < renterBuffer.Length; i++)
                {
                    Entity renterHousehold = renterBuffer[i].m_Renter;

                    if (!householdLookup.HasComponent(renterHousehold) || !householdCitizenLookup.TryGetBuffer(renterHousehold, out var householdCitizens))
                        continue;

                    for (int j = 0; j < householdCitizens.Length; j++)
                    {
                        if (!CitizenUtils.IsCorpsePickedByHearse(householdCitizens[j].m_Citizen, ref healthProblemLookup, ref travelPurposeLookup))
                        {
                            residentCount++;
                        }
                    }
                }
            }

            return result;
        }
    }
}
