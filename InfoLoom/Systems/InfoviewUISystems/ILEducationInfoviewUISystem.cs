using Colossal.UI.Binding;
using Game.Areas;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.UI.InGame;
using InfoLoomTwo.Domain.DataDomain;
using System;
using Colossal.Entities;
using Game.UI;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace InfoLoomTwo.Systems.InfoviewUISystems
{
    public partial class ILEducationInfoviewUISystem : InfoviewUISystemBase
    {
        private const string kGroup = "InfoLoomTwo";

        // Other systems
        private SimulationSystem m_SimulationSystem;
        private CitySystem m_CitySystem;
        private NameSystem m_NameSystem;

        // District tracking
        private EntityQuery m_DistrictQuery;
        private DistrictInfos _DistrictInfos = new DistrictInfos();
        public static Entity EntireCity { get; } = Entity.Null;
        public Entity selectedDistrict { get; set; } = EntireCity;

        // UI Bindings
        private RawValueBinding m_EducationData;
        private ValueBinding<Entity> m_SelectedDistrict;
        private RawValueBinding m_DistrictInfos;
        private ValueBinding<int> m_ElementaryStudents;
        private ValueBinding<int> m_HighSchoolStudents;
        private ValueBinding<int> m_CollegeStudents;
        private ValueBinding<int> m_UniversityStudents;
        private ValueBinding<int> m_ElementaryEligible;
        private ValueBinding<int> m_HighSchoolEligible;
        private ValueBinding<int> m_CollegeEligible;
        private ValueBinding<int> m_UniversityEligible;
        private ValueBinding<int> m_ElementaryCapacity;
        private ValueBinding<int> m_HighSchoolCapacity;
        private ValueBinding<int> m_CollegeCapacity;
        private ValueBinding<int> m_UniversityCapacity;
        
        private GetterValueBinding<IndicatorValue> m_ElementaryAvailability;
        private GetterValueBinding<IndicatorValue> m_HighSchoolAvailability;
        private GetterValueBinding<IndicatorValue> m_CollegeAvailability;
        private GetterValueBinding<IndicatorValue> m_UniversityAvailability;

        // Entity queries
        private EntityQuery m_HouseholdQuery;
        private EntityQuery m_SchoolQuery;
        private EntityQuery m_SchoolModifiedQuery;
        private EntityQuery m_EligibleQuery;

        // Results array
        private NativeArray<int> m_Results;

        protected override void OnCreate()
        {
            base.OnCreate();

            // Get required systems
            m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
            m_CitySystem = World.GetOrCreateSystemManaged<CitySystem>();
            m_NameSystem = World.GetOrCreateSystemManaged<NameSystem>();

            // Set up entity queries
            m_DistrictQuery = GetEntityQuery(ComponentType.ReadOnly<District>());
            m_HouseholdQuery = GetEntityQuery(
                ComponentType.ReadOnly<Household>(),
                ComponentType.ReadOnly<PropertyRenter>(),
                ComponentType.ReadOnly<HouseholdCitizen>(),
                ComponentType.Exclude<TouristHousehold>(),
                ComponentType.Exclude<CommuterHousehold>(),
                ComponentType.Exclude<MovingAway>()
            );
            m_SchoolQuery = GetEntityQuery(
                ComponentType.ReadOnly<Building>(),
                ComponentType.ReadOnly<Game.Buildings.School>(),
                ComponentType.ReadOnly<Game.Buildings.Student>(),
                ComponentType.ReadOnly<PrefabRef>(),
                ComponentType.Exclude<Temp>(),
                ComponentType.Exclude<Deleted>()
            );
            m_SchoolModifiedQuery = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[2]
                {
                    ComponentType.ReadOnly<Building>(),
                    ComponentType.ReadOnly<Game.Buildings.School>()
                },
                Any = new ComponentType[3]
                {
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Created>(),
                    ComponentType.ReadOnly<Updated>()
                },
                None = new ComponentType[1]
                {
                    ComponentType.ReadOnly<Temp>()
                }
            });
            m_EligibleQuery = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[2]
                {
                    ComponentType.ReadWrite<Citizen>(),
                    ComponentType.ReadOnly<UpdateFrame>()
                },
                None = new ComponentType[3]
                {
                    ComponentType.ReadOnly<HasJobSeeker>(),
                    ComponentType.ReadOnly<Temp>(),
                    ComponentType.ReadOnly<Deleted>()
                }
            });

            // Initialize UI bindings
            AddBinding(m_EducationData = new RawValueBinding(kGroup, "educationData", UpdateEducationData));
            AddBinding(m_SelectedDistrict = new ValueBinding<Entity>(kGroup, "selectedDistrict", EntireCity));
            AddBinding(m_DistrictInfos = new RawValueBinding(kGroup, "districtInfos", UpdateDistrictInfos));
            AddBinding(m_ElementaryStudents = new ValueBinding<int>(kGroup, "elementaryStudentCount", 0));
            AddBinding(m_HighSchoolStudents = new ValueBinding<int>(kGroup, "highSchoolStudentCount", 0));
            AddBinding(m_CollegeStudents = new ValueBinding<int>(kGroup, "collegeStudentCount", 0));
            AddBinding(m_UniversityStudents = new ValueBinding<int>(kGroup, "universityStudentCount", 0));
            AddBinding(m_ElementaryEligible = new ValueBinding<int>(kGroup, "elementaryEligible", 0));
            AddBinding(m_HighSchoolEligible = new ValueBinding<int>(kGroup, "highSchoolEligible", 0));
            AddBinding(m_CollegeEligible = new ValueBinding<int>(kGroup, "collegeEligible", 0));
            AddBinding(m_UniversityEligible = new ValueBinding<int>(kGroup, "universityEligible", 0));
            AddBinding(m_ElementaryCapacity = new ValueBinding<int>(kGroup, "elementaryCapacity", 0));
            AddBinding(m_HighSchoolCapacity = new ValueBinding<int>(kGroup, "highSchoolCapacity", 0));
            AddBinding(m_CollegeCapacity = new ValueBinding<int>(kGroup, "collegeCapacity", 0));
            AddBinding(m_UniversityCapacity = new ValueBinding<int>(kGroup, "universityCapacity", 0));

            
            AddBinding(m_ElementaryAvailability = new GetterValueBinding<IndicatorValue>(kGroup, "elementaryAvailability", 
                () => IndicatorValue.Calculate((float)m_ElementaryCapacity.value, (float)m_ElementaryEligible.value), 
                new ValueWriter<IndicatorValue>()));

            AddBinding(m_HighSchoolAvailability = new GetterValueBinding<IndicatorValue>(kGroup, "highSchoolAvailability", 
                () => IndicatorValue.Calculate((float)m_HighSchoolCapacity.value, (float)m_HighSchoolEligible.value), 
                new ValueWriter<IndicatorValue>()));

            AddBinding(m_CollegeAvailability = new GetterValueBinding<IndicatorValue>(kGroup, "collegeAvailability", 
                () => IndicatorValue.Calculate((float)m_CollegeCapacity.value, (float)m_CollegeEligible.value), 
                new ValueWriter<IndicatorValue>()));

            AddBinding(m_UniversityAvailability = new GetterValueBinding<IndicatorValue>(kGroup, "universityAvailability", 
                () => IndicatorValue.Calculate((float)m_UniversityCapacity.value, (float)m_UniversityEligible.value), 
                new ValueWriter<IndicatorValue>()));
            
            // Initialize results array
            m_Results = new NativeArray<int>(17, Allocator.Persistent);

            // Add event bindings for district selection
            AddBinding(new TriggerBinding<Entity>(kGroup, "selectedDistrictChanged", SelectedDistrictChanged));
        }

        protected override void OnDestroy()
        {
            if (m_Results.IsCreated)
            {
                m_Results.Dispose();
            }
            base.OnDestroy();
        }

        protected override bool Active => 
            base.Active || 
            m_EducationData.active || 
            m_ElementaryStudents.active || 
            m_ElementaryCapacity.active || 
            m_ElementaryEligible.active || 
            m_ElementaryAvailability.active ||
            m_HighSchoolStudents.active || 
            m_HighSchoolCapacity.active || 
            m_HighSchoolEligible.active || 
            m_HighSchoolAvailability.active ||
            m_CollegeStudents.active || 
            m_CollegeCapacity.active || 
            m_CollegeEligible.active || 
            m_CollegeAvailability.active ||
            m_UniversityStudents.active || 
            m_UniversityCapacity.active || 
            m_UniversityEligible.active || 
            m_UniversityAvailability.active;
        protected override bool Modified => !m_SchoolModifiedQuery.IsEmptyIgnoreFilter;

        protected override void PerformUpdate()
        {
            // Check for district changes
            CheckForDistrictChange();

            // Reset results
            ResetResults();

            // Update education data for selected district
            UpdateStudentCounts();
            UpdateEligibility();

            // Update UI bindings
            m_EducationData.Update();
            m_ElementaryStudents.Update(m_Results[9]);
            m_HighSchoolStudents.Update(m_Results[10]);
            m_CollegeStudents.Update(m_Results[11]);
            m_UniversityStudents.Update(m_Results[12]);
            m_ElementaryCapacity.Update(m_Results[13]);
            m_HighSchoolCapacity.Update(m_Results[14]);
            m_CollegeCapacity.Update(m_Results[15]);
            m_UniversityCapacity.Update(m_Results[16]);
            m_ElementaryEligible.Update(m_Results[5]);
            m_HighSchoolEligible.Update(m_Results[6]);
            m_CollegeEligible.Update(m_Results[7]);
            m_UniversityEligible.Update(m_Results[8]);
            
            // Update indicator values
            m_ElementaryAvailability.Update();
            m_HighSchoolAvailability.Update();
            m_CollegeAvailability.Update();
            m_UniversityAvailability.Update();
        }

        private void ResetResults()
        {
            for (int i = 0; i < m_Results.Length; i++)
            {
                m_Results[i] = 0;
            }
        }

        private void UpdateEducationData(IJsonWriter writer)
        {
            // Write education data similar to original system but filtered by district
            writer.TypeBegin("educationData");
            writer.PropertyName("uneducated");
            writer.Write(m_Results[0]);
            writer.PropertyName("elementaryEducated");
            writer.Write(m_Results[1]);
            writer.PropertyName("highSchoolEducated");
            writer.Write(m_Results[2]);
            writer.PropertyName("collegeEducated");
            writer.Write(m_Results[3]);
            writer.PropertyName("universityEducated");
            writer.Write(m_Results[4]);
            writer.TypeEnd();
        }

        private void UpdateDistrictInfos(IJsonWriter writer)
        {
            _DistrictInfos.Write(writer);
        }

        private void CheckForDistrictChange()
        {
            // Get district infos and check for changes
            bool foundSelectedDistrict = (selectedDistrict == EntireCity);
            DistrictInfos districtInfos = new DistrictInfos();

            NativeArray<Entity> districtEntities = m_DistrictQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity districtEntity in districtEntities)
            {
                string districtName = m_NameSystem.GetRenderedLabelName(districtEntity);
                if (districtName != "Assets.DISTRICT_NAME")
                {
                    districtInfos.Add(new DistrictInfo(districtEntity, districtName));
                    if (districtEntity == selectedDistrict)
                    {
                        foundSelectedDistrict = true;
                    }
                }
            }

            if (!foundSelectedDistrict)
            {
                selectedDistrict = EntireCity;
                m_SelectedDistrict.Update(selectedDistrict);
            }

            districtInfos.Sort();
            districtInfos.Insert(0, new DistrictInfo(EntireCity, "Entire City"));

            // Check if district infos have changed
            bool districtsChanged = false;
            if (districtInfos.Count != _DistrictInfos.Count)
            {
                districtsChanged = true;
            }
            else
            {
                for (int i = 0; i < districtInfos.Count; i++)
                {
                    if (districtInfos[i].entity != _DistrictInfos[i].entity || districtInfos[i].name != _DistrictInfos[i].name)
                    {
                        districtsChanged = true;
                        break;
                    }
                }
            }

            if (districtsChanged)
            {
                _DistrictInfos = districtInfos;
                m_DistrictInfos.Update();
            }

            districtEntities.Dispose();
        }

        private void SelectedDistrictChanged(Entity newDistrict)
        {
            selectedDistrict = newDistrict;
            m_SelectedDistrict.Update(selectedDistrict);
        }

        private void UpdateStudentCounts()
{
    NativeArray<Entity> schoolEntities = m_SchoolQuery.ToEntityArray(Allocator.Temp);

    foreach (Entity schoolEntity in schoolEntities)
    {
        // Check if school is in selected district
        if (!IsInSelectedDistrict(schoolEntity))
            continue;

        // Get building efficiency
        if (!EntityManager.TryGetBuffer<Efficiency>(schoolEntity, true, out var efficiencyBuffer))
            continue;

        float efficiency = BuildingUtils.GetEfficiency(efficiencyBuffer);
        if (efficiency == 0.0f)
            continue;

        // Get school students
        if (!EntityManager.TryGetBuffer<Game.Buildings.Student>(schoolEntity, true, out var studentsBuffer))
            continue;

        // Get school prefab data
        if (!EntityManager.TryGetComponent<PrefabRef>(schoolEntity, out var prefabRef))
            continue;

        if (!EntityManager.TryGetComponent<SchoolData>(prefabRef.m_Prefab, out var schoolData))
            continue;

        // Handle building upgrades
        if (EntityManager.TryGetBuffer<InstalledUpgrade>(schoolEntity, true, out var upgradesBuffer))
        {
            // Create lookups using __TypeHandle pattern
            var prefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>();
            var schoolDataLookup = SystemAPI.GetComponentLookup<SchoolData>();
            UpgradeUtils.CombineStats(ref schoolData, upgradesBuffer, ref prefabRefLookup, ref schoolDataLookup);
        }

        // Update counts based on education level
        int studentCount = studentsBuffer.Length;
        int capacity = schoolData.m_StudentCapacity;

        switch (schoolData.m_EducationLevel)
        {
            case 1: // Elementary
                m_Results[9] += studentCount;
                m_Results[13] += capacity;
                break;
            case 2: // High School
                m_Results[10] += studentCount;
                m_Results[14] += capacity;
                break;
            case 3: // College
                m_Results[11] += studentCount;
                m_Results[15] += capacity;
                break;
            case 4: // University
                m_Results[12] += studentCount;
                m_Results[16] += capacity;
                break;
        }
    }

    schoolEntities.Dispose();
}

        private void UpdateEligibility()
        {
            // Get required singleton data
            var economyParameterData = SystemAPI.GetSingleton<EconomyParameterData>();
            var educationParameterData = SystemAPI.GetSingleton<EducationParameterData>();
            var timeData = SystemAPI.GetSingleton<TimeData>();
            
            // Filter citizens by district and update eligibility counts
            NativeArray<Entity> citizenEntities = m_EligibleQuery.ToEntityArray(Allocator.Temp);
            
            float elementaryEligible = 0f;
            float highSchoolEligible = 0f;
            float collegeEligible = 0f;
            float universityEligible = 0f;

            foreach (Entity citizenEntity in citizenEntities)
            {
                // Check if citizen is in selected district
                if (!IsInSelectedDistrict(citizenEntity))
                    continue;

                if (!EntityManager.TryGetComponent<Citizen>(citizenEntity, out var citizen))
                    continue;

                if (!EntityManager.TryGetComponent<HouseholdMember>(citizenEntity, out var householdMember))
                    continue;

                if (!EntityManager.TryGetComponent<Household>(householdMember.m_Household, out var household))
                    continue;

                // Skip if household not moved in, is tourist, or moving away
                if ((household.m_Flags & HouseholdFlags.MovedIn) == HouseholdFlags.None ||
                    (household.m_Flags & HouseholdFlags.Tourist) != HouseholdFlags.None ||
                    EntityManager.HasComponent<MovingAway>(householdMember.m_Household) ||
                    !EntityManager.HasComponent<PropertyRenter>(householdMember.m_Household))
                    continue;

                // Check if citizen is already a student
                if (EntityManager.TryGetComponent<Game.Citizens.Student>(citizenEntity, out var student))
                {
                    switch (student.m_Level)
                    {
                        case 1:
                            elementaryEligible++;
                            break;
                        case 2:
                            highSchoolEligible++;
                            break;
                        case 3:
                            collegeEligible++;
                            break;
                        case 4:
                            universityEligible++;
                            break;
                    }
                }
                else
                {
                    // Calculate eligibility for non-students
                    CitizenAge age = citizen.GetAge();
                    float willingness = citizen.GetPseudoRandom(CitizenPseudoRandom.StudyWillingness).NextFloat();
                    bool hasWorker = EntityManager.HasComponent<Worker>(citizenEntity);

                    if (age == CitizenAge.Child)
                    {
                        elementaryEligible++;
                    }
                    else if (citizen.GetEducationLevel() == 1 && age <= CitizenAge.Adult)
                    {
                        // Get city modifiers for probability calculation
                        var cityModifiers = EntityManager.GetBuffer<CityModifier>(m_CitySystem.City);
                        
                        highSchoolEligible += ApplyToSchoolSystem.GetEnteringProbability(
                            age, hasWorker, 2, (int)citizen.m_WellBeing, willingness, 
                            cityModifiers, ref educationParameterData);
                    }
                    else
                    {
                        int failedEducationCount = citizen.GetFailedEducationCount();
                        if (citizen.GetEducationLevel() == 2 && failedEducationCount < 3)
                        {
                            // Get city modifiers for probability calculation
                            var cityModifiers = EntityManager.GetBuffer<CityModifier>(m_CitySystem.City);
                            
                            float universityProbability = ApplyToSchoolSystem.GetEnteringProbability(
                                age, hasWorker, 4, (int)citizen.m_WellBeing, willingness,
                                cityModifiers, ref educationParameterData);
                            
                            universityEligible += universityProbability;
                            
                            float collegeProbability = (1f - universityProbability) * 
                                ApplyToSchoolSystem.GetEnteringProbability(
                                    age, hasWorker, 3, (int)citizen.m_WellBeing, willingness,
                                    cityModifiers, ref educationParameterData);
                            
                            collegeEligible += collegeProbability;
                        }
                    }
                }
            }

            // Update results array
            m_Results[5] += Mathf.CeilToInt(elementaryEligible);
            m_Results[6] += Mathf.CeilToInt(highSchoolEligible);
            m_Results[7] += Mathf.CeilToInt(collegeEligible);
            m_Results[8] += Mathf.CeilToInt(universityEligible);

            citizenEntities.Dispose();
        }

        private bool IsInSelectedDistrict(Entity entity)
        {
            if (selectedDistrict == EntireCity)
                return true;

            // Get the citizen's household
            if (!EntityManager.TryGetComponent<HouseholdMember>(entity, out var householdMember))
                return false;

            // Get the household's property
            if (!EntityManager.TryGetComponent<PropertyRenter>(householdMember.m_Household, out var propertyRenter))
                return false;

            // Get the building entity
            Entity buildingEntity = propertyRenter.m_Property;
            if (buildingEntity == Entity.Null)
                return false;

            // Check if the building has a district component
            if (EntityManager.TryGetComponent<CurrentDistrict>(buildingEntity, out var currentDistrict))
            {
                return currentDistrict.m_District == selectedDistrict;
            }

            // If building doesn't have a district assigned, it's considered part of the "no district" area
            // Return false since we're looking for a specific district
            return false;
        }
    }
}