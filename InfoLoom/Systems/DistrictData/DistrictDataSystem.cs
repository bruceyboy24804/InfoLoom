using System;
using System.Collections.Generic;
using System.Threading;
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
using Game.Companies;
using Game.Policies;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Simulation;
using Game.Tools;
using Game.UI;
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
using UnityEngine.Scripting;

namespace InfoLoomTwo.Systems.DistrictData
{
    public partial class DistrictDataSystem : ExtendedUISystemBase
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
            public int employeeCount;
            public int maxEmployees;
            public EmploymentData educationDataEmployees;
            public EmploymentData educationDataWorkplaces;
            public NativeList<Entity> serviceBuildings;
            public NativeList<Entity> servicePrefabs;
            public NativeList<Entity> policies;

            public int elementaryEligible;
            public int highSchoolEligible;
            public int collegeEligible;
            public int universityEligible;

            public int elementaryCapacity;
            public int highSchoolCapacity;
            public int collegeCapacity;
            public int universityCapacity;

            public int elementaryStudents;
            public int highSchoolStudents;
            public int collegeStudents;
            public int universityStudents;

            public IndicatorValue elementaryAvailability;
            public IndicatorValue highSchoolAvailability;
            public IndicatorValue collegeAvailability;
            public IndicatorValue universityAvailability;
        }

        // Efficient citizen processing using IJobChunk
        [BurstCompile]
        private struct ProcessCitizensJob : IJobChunk
        {
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
            [ReadOnly] public BufferLookup<HouseholdCitizen> m_HouseholdCitizenLookup;
            [ReadOnly] public BufferLookup<HouseholdAnimal> m_HouseholdAnimalLookup;
            [ReadOnly] public BufferLookup<CityModifier> m_CityModifierLookup;
            
            [ReadOnly] public Entity m_City;
            [ReadOnly] public EducationParameterData m_EducationParameterData;
            [ReadOnly] public NativeHashMap<Entity, int> m_DistrictIndexMap;
            
            [NativeDisableParallelForRestriction]
            public NativeArray<DistrictEntry> m_Results;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(m_EntityHandle);
                var citizens = chunk.GetNativeArray(m_CitizenHandle);
                var householdMembers = chunk.GetNativeArray(m_HouseholdMemberHandle);
                
                for (int i = 0; i < chunk.Count; i++)
                {
                    Entity citizenEntity = entities[i];
                    Citizen citizen = citizens[i];
                    HouseholdMember householdMember = householdMembers[i];
                    
                    if (!GetDistrictIndex(citizenEntity, householdMember, out int districtIndex))
                        continue;
                    
                    var district = m_Results[districtIndex];
                    
                    // Process age data
                    int ageCategory = (int)citizen.GetAge();
                    if (ageCategory >= 0 && ageCategory < 4)
                    {
                        switch (ageCategory)
                        {
                            case 0: district.ageData = new AgeData(district.ageData.children + 1, district.ageData.teens, district.ageData.adults, district.ageData.elders); break;
                            case 1: district.ageData = new AgeData(district.ageData.children, district.ageData.teens + 1, district.ageData.adults, district.ageData.elders); break;
                            case 2: district.ageData = new AgeData(district.ageData.children, district.ageData.teens, district.ageData.adults + 1, district.ageData.elders); break;
                            case 3: district.ageData = new AgeData(district.ageData.children, district.ageData.teens, district.ageData.adults, district.ageData.elders + 1); break;
                        }
                    }
                    
                    // Process education data
                    int educationLevel = citizen.GetEducationLevel();
                    if (educationLevel >= 0 && educationLevel <= 4)
                    {
                        switch (educationLevel)
                        {
                            case 0: district.educationData = new EducationData(district.educationData.uneducated + 1, district.educationData.poorlyEducated, district.educationData.educated, district.educationData.wellEducated, district.educationData.highlyEducated); break;
                            case 1: district.educationData = new EducationData(district.educationData.uneducated, district.educationData.poorlyEducated + 1, district.educationData.educated, district.educationData.wellEducated, district.educationData.highlyEducated); break;
                            case 2: district.educationData = new EducationData(district.educationData.uneducated, district.educationData.poorlyEducated, district.educationData.educated + 1, district.educationData.wellEducated, district.educationData.highlyEducated); break;
                            case 3: district.educationData = new EducationData(district.educationData.uneducated, district.educationData.poorlyEducated, district.educationData.educated, district.educationData.wellEducated + 1, district.educationData.highlyEducated); break;
                            case 4: district.educationData = new EducationData(district.educationData.uneducated, district.educationData.poorlyEducated, district.educationData.educated, district.educationData.wellEducated, district.educationData.highlyEducated + 1); break;
                        }
                    }
                    
                    // Process school eligibility
                    ProcessSchoolEligibility(citizenEntity, citizen, ref district);
                    
                    m_Results[districtIndex] = district;
                }
            }
            
            private bool GetDistrictIndex(Entity citizenEntity, HouseholdMember householdMember, out int districtIndex)
            {
                districtIndex = -1;
                
                Entity household = householdMember.m_Household;
                
                if (!m_HouseholdLookup.TryGetComponent(household, out var householdData))
                    return false;
                
                // Skip invalid households
                if (CitizenUtils.IsDead(citizenEntity, ref m_HealthProblemLookup) ||
                    (householdData.m_Flags & HouseholdFlags.MovedIn) == 0 ||
                    (householdData.m_Flags & HouseholdFlags.Tourist) != 0 ||
                    m_MovingAwayLookup.HasComponent(household))
                    return false;
                
                if (!m_PropertyRenterLookup.TryGetComponent(household, out var propertyRenter))
                    return false;
                
                if (!m_CurrentDistrictLookup.TryGetComponent(propertyRenter.m_Property, out var currentDistrict))
                    return false;
                
                return m_DistrictIndexMap.TryGetValue(currentDistrict.m_District, out districtIndex);
            }
            
            private void ProcessSchoolEligibility(Entity citizen, Citizen citizenData, ref DistrictEntry result)
            {
                CitizenAge age = citizenData.GetAge();
                
                if (m_StudentLookup.TryGetComponent(citizen, out var student) && student.m_School != Entity.Null)
                {
                    // Current student
                    switch (student.m_Level)
                    {
                        case 1: result.elementaryEligible++; break;
                        case 2: result.highSchoolEligible++; break;
                        case 3: result.collegeEligible++; break;
                        case 4: result.universityEligible++; break;
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
                        result.elementaryEligible++;
                    }
                    else if (citizenData.GetEducationLevel() == 1 && age <= CitizenAge.Adult)
                    {
                        float probability = ApplyToSchoolSystem.GetEnteringProbability(
                            age, hasWorker, 2, (int)citizenData.m_WellBeing, willingness, cityModifiers, ref m_EducationParameterData);
                        result.highSchoolEligible += (int)math.ceil(probability);
                    }
                    else if (citizenData.GetEducationLevel() == 2 && citizenData.GetFailedEducationCount() < 3)
                    {
                        float universityProb = ApplyToSchoolSystem.GetEnteringProbability(
                            age, hasWorker, 4, (int)citizenData.m_WellBeing, willingness, cityModifiers, ref m_EducationParameterData);
                        result.universityEligible += (int)math.ceil(universityProb);
                        
                        float collegeProb = (1f - universityProb) * ApplyToSchoolSystem.GetEnteringProbability(
                            age, hasWorker, 3, (int)citizenData.m_WellBeing, willingness, cityModifiers, ref m_EducationParameterData);
                        result.collegeEligible += (int)math.ceil(collegeProb);
                    }
                }
            }
        }

        // Efficient building processing using IJobChunk
        [BurstCompile]
        private struct ProcessBuildingsJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle m_EntityHandle;
            [ReadOnly] public ComponentTypeHandle<PrefabRef> m_PrefabRefHandle;
            [ReadOnly] public ComponentTypeHandle<CurrentDistrict> m_CurrentDistrictHandle;
            
            [ReadOnly] public ComponentLookup<Abandoned> m_AbandonedLookup;
            [ReadOnly] public ComponentLookup<BuildingPropertyData> m_PropertyDataLookup;
            [ReadOnly] public ComponentLookup<WorkProvider> m_WorkProviderLookup;
            [ReadOnly] public ComponentLookup<CompanyData> m_CompanyDataLookup;
            [ReadOnly] public ComponentLookup<WorkplaceData> m_WorkplaceDataLookup;
            [ReadOnly] public ComponentLookup<SpawnableBuildingData> m_SpawnableDataLookup;
            [ReadOnly] public ComponentLookup<SchoolData> m_SchoolDataLookup;
            [ReadOnly] public ComponentLookup<Household> m_HouseholdLookup;
            [ReadOnly] public ComponentLookup<PrefabRef> m_PrefabRefLookup;
            [ReadOnly] public ComponentLookup<PropertyRenter> m_PropertyRenterLookup;
            
            [ReadOnly] public BufferLookup<Renter> m_RenterLookup;
            [ReadOnly] public BufferLookup<Employee> m_EmployeeLookup;
            [ReadOnly] public BufferLookup<ServiceDistrict> m_ServiceDistrictLookup;
            [ReadOnly] public BufferLookup<Game.Buildings.Student> m_StudentLookup;
            [ReadOnly] public BufferLookup<Efficiency> m_EfficiencyLookup;
            [ReadOnly] public BufferLookup<InstalledUpgrade> m_InstalledUpgradeLookup;
            [ReadOnly] public BufferLookup<HouseholdCitizen> m_HouseholdCitizenLookup;
            [ReadOnly] public BufferLookup<HouseholdAnimal> m_HouseholdAnimalLookup;
            
            [ReadOnly] public NativeHashMap<Entity, int> m_DistrictIndexMap;
            
            [NativeDisableParallelForRestriction]
            public NativeArray<DistrictEntry> m_Results;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(m_EntityHandle);
                var prefabRefs = chunk.GetNativeArray(m_PrefabRefHandle);
                var currentDistricts = chunk.GetNativeArray(m_CurrentDistrictHandle);
                
                for (int i = 0; i < chunk.Count; i++)
                {
                    Entity building = entities[i];
                    Entity prefab = prefabRefs[i].m_Prefab;
                    Entity districtEntity = currentDistricts[i].m_District;
                    
                    if (m_AbandonedLookup.HasComponent(building) ||
                        !m_DistrictIndexMap.TryGetValue(districtEntity, out int districtIndex))
                        continue;
                    
                    var district = m_Results[districtIndex];
                    
                    ProcessResidentialBuilding(building, prefab, ref district);
                    ProcessServiceBuilding(building, prefab, districtEntity, ref district);
                    ProcessEmploymentBuilding(building, prefab, ref district);
                    ProcessSchoolBuilding(building, prefab, ref district);
                    
                    m_Results[districtIndex] = district;
                }
            }
            
            private void ProcessResidentialBuilding(Entity building, Entity prefab, ref DistrictEntry result)
            {
                if (!m_PropertyDataLookup.TryGetComponent(prefab, out var propertyData) ||
                    propertyData.m_ResidentialProperties <= 0 ||
                    !m_RenterLookup.TryGetBuffer(building, out var renters))
                    return;
                
                result.maxHouseholds += propertyData.m_ResidentialProperties;
                
                for (int k = 0; k < renters.Length; k++)
                {
                    Entity household = renters[k].m_Renter;
                    if (!m_HouseholdLookup.HasComponent(household))
                        continue;
                    
                    result.households.Add(household);
                    result.householdCount++;
                    
                    if (m_HouseholdCitizenLookup.TryGetBuffer(household, out var citizens))
                        result.residentCount += citizens.Length;
                    
                    if (m_HouseholdAnimalLookup.TryGetBuffer(household, out var animals))
                        result.petCount += animals.Length;
                }
            }
            
            private void ProcessServiceBuilding(Entity building, Entity prefab, Entity districtEntity, ref DistrictEntry result)
            {
                if (!m_ServiceDistrictLookup.TryGetBuffer(building, out var serviceDistricts))
                    return;
                
                for (int j = 0; j < serviceDistricts.Length; j++)
                {
                    if (serviceDistricts[j].m_District == districtEntity)
                    {
                        result.serviceBuildings.Add(building);
                        result.servicePrefabs.Add(prefab);
                        break;
                    }
                }
            }
            
            private void ProcessEmploymentBuilding(Entity building, Entity prefab, ref DistrictEntry result)
            {
                Entity employeeEntity = building;
                bool hasEmployees = false;
                
                if (m_EmployeeLookup.HasBuffer(building) && m_WorkProviderLookup.HasComponent(building))
                {
                    hasEmployees = true;
                }
                else if (m_RenterLookup.TryGetBuffer(building, out var renters))
                {
                    for (int j = 0; j < renters.Length; j++)
                    {
                        Entity renter = renters[j].m_Renter;
                        if (m_CompanyDataLookup.HasComponent(renter) &&
                            m_EmployeeLookup.HasBuffer(renter) &&
                            m_WorkProviderLookup.HasComponent(renter))
                        {
                            employeeEntity = renter;
                            hasEmployees = true;
                            break;
                        }
                    }
                }
                
                if (!hasEmployees ||
                    !m_WorkProviderLookup.TryGetComponent(employeeEntity, out var workProvider) ||
                    !m_PrefabRefLookup.TryGetComponent(employeeEntity, out var employeePrefabRef) ||
                    !m_WorkplaceDataLookup.TryGetComponent(employeePrefabRef.m_Prefab, out var workplaceData))
                    return;
                
                int buildingLevel = GetBuildingLevel(building, prefab);
                
                var employees = m_EmployeeLookup.TryGetBuffer(employeeEntity, out var employeeBuffer) ? employeeBuffer : default;
                EmploymentData workplacesData = EmploymentData.GetWorkplacesData(
                    workProvider.m_MaxWorkers, buildingLevel, workplaceData.m_Complexity);
                
                EmploymentData employeesData = default;
                int employeeCount = 0;
                if (employees.IsCreated)
                {
                    employeeCount = employees.Length;
                    employeesData = EmploymentData.GetEmployeesData(employees, workplacesData.total - employees.Length);
                }
                
                result.employeeCount += employeeCount;
                result.maxEmployees += workplacesData.total;
                result.educationDataEmployees += employeesData;
                result.educationDataWorkplaces += workplacesData;
            }
            
            private int GetBuildingLevel(Entity building, Entity prefab)
            {
                if (m_SpawnableDataLookup.TryGetComponent(prefab, out var spawnableData))
                    return spawnableData.m_Level;
                
                if (m_PropertyRenterLookup.TryGetComponent(building, out var propertyRenter) &&
                    m_PrefabRefLookup.TryGetComponent(propertyRenter.m_Property, out var propertyPrefabRef) &&
                    m_SpawnableDataLookup.TryGetComponent(propertyPrefabRef.m_Prefab, out var propertySpawnableData))
                    return propertySpawnableData.m_Level;
                
                return 1;
            }
            
            private void ProcessSchoolBuilding(Entity building, Entity prefab, ref DistrictEntry result)
            {
                if (!m_StudentLookup.TryGetBuffer(building, out var students) ||
                    !m_EfficiencyLookup.TryGetBuffer(building, out var efficiency) ||
                    !m_SchoolDataLookup.TryGetComponent(prefab, out var schoolData) ||
                    BuildingUtils.GetEfficiency(efficiency) <= 0.0)
                    return;
                
                if (m_InstalledUpgradeLookup.TryGetBuffer(building, out var upgrades))
                {
                    UpgradeUtils.CombineStats(ref schoolData, upgrades, ref m_PrefabRefLookup, ref m_SchoolDataLookup);
                }
                
                switch (schoolData.m_EducationLevel)
                {
                    case 1:
                        result.elementaryStudents += students.Length;
                        result.elementaryCapacity += schoolData.m_StudentCapacity;
                        break;
                    case 2:
                        result.highSchoolStudents += students.Length;
                        result.highSchoolCapacity += schoolData.m_StudentCapacity;
                        break;
                    case 3:
                        result.collegeStudents += students.Length;
                        result.collegeCapacity += schoolData.m_StudentCapacity;
                        break;
                    case 4:
                        result.universityStudents += students.Length;
                        result.universityCapacity += schoolData.m_StudentCapacity;
                        break;
                }
            }
        }

        // Policy processing job (kept as IJobChunk since it's already efficient)
        [BurstCompile]
        private struct ProcessDistrictPoliciesJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle m_EntityHandle;
            [ReadOnly] public BufferTypeHandle<Policy> m_PolicyHandle;
            [ReadOnly] public NativeArray<Entity> PolicyEntities;
            [ReadOnly] public NativeHashMap<Entity, int> m_DistrictIndexMap;
            
            [NativeDisableParallelForRestriction]
            public NativeArray<DistrictEntry> Districts;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entities = chunk.GetNativeArray(m_EntityHandle);
                BufferAccessor<Policy> policyAccessor = chunk.GetBufferAccessor(ref m_PolicyHandle);

                for (int i = 0; i < chunk.Count; i++)
                {
                    Entity districtEntity = entities[i];
                    if (!m_DistrictIndexMap.TryGetValue(districtEntity, out int districtIndex))
                        continue;

                    var district = Districts[districtIndex];
                    district.policies.Clear();

                    var activePolicies = policyAccessor[i];
                    for (int j = 0; j < activePolicies.Length; j++)
                    {
                        Policy policy = activePolicies[j];
                        if ((policy.m_Flags & PolicyFlags.Active) == 0)
                            continue;

                        // Check if it's a district policy
                        for (int k = 0; k < PolicyEntities.Length; k++)
                        {
                            if (PolicyEntities[k] == policy.m_Policy)
                            {
                                district.policies.Add(policy.m_Policy);
                                break;
                            }
                        }
                    }
                    Districts[districtIndex] = district;
                }
            }
        }

        private EntityQuery m_BuildingQuery;
        private EntityQuery m_DistrictQuery;
        private EntityQuery m_CitizenQuery;
        private EntityQuery m_EducationParameterQuery;
        private EntityQuery m_DistrictPoliciesQuery;
        
        private ValueBinding<bool> _dDPVBinding;
        private RawValueBinding m_uiDistricts;
        
        private PrefabSystem m_PrefabSystem;
        private ImageSystem m_ImageSystem;
        private NameSystem m_NameSystem;
        private CitySystem m_CitySystem;
        private SimulationSystem m_SimulationSystem;
        private NativeList<DistrictEntry> m_Districts;
        private UIUpdateState _uiUpdateState;

        public static readonly int kUpdatesPerDay = 1;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_BuildingQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    typeof(Building),
                    typeof(PrefabRef),
                    typeof(CurrentDistrict)
                },
                None = new ComponentType[]
                {
                    typeof(Temp),
                    typeof(Deleted)
                }
            });

            m_DistrictQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(District) },
                None = new ComponentType[] { typeof(Temp) }
            });

            m_CitizenQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    typeof(Citizen),
                    typeof(HouseholdMember)
                },
                None = new ComponentType[]
                {
                    typeof(Temp),
                    typeof(Deleted)
                }
            });

            m_EducationParameterQuery = GetEntityQuery(typeof(EducationParameterData));

            m_DistrictPoliciesQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<PolicyData>() },
                Any = new ComponentType[]
                {
                    ComponentType.ReadOnly<DistrictOptionData>(),
                    ComponentType.ReadOnly<DistrictModifierData>()
                }
            });

            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_ImageSystem = World.GetOrCreateSystemManaged<ImageSystem>();
            m_NameSystem = World.GetOrCreateSystemManaged<NameSystem>();
            m_CitySystem = World.GetOrCreateSystemManaged<CitySystem>();
            m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
            _uiUpdateState = UIUpdateState.Create(World, 512);
            m_Districts = new NativeList<DistrictEntry>(64, Allocator.Persistent);

            RequireForUpdate<District>();

            _dDPVBinding = new ValueBinding<bool>("InfoLoomTwo", "DistrictDataOpen", false);
            AddBinding(_dDPVBinding);
            AddBinding(new TriggerBinding<bool>("InfoLoomTwo", "DistrictDataOpen", SetDistrictDataVisibility));
            AddBinding(m_uiDistricts = new RawValueBinding("InfoLoomTwo", "DistrictData", WriteDistricts));
        }

        protected override void OnDestroy()
        {
            if (m_Districts.IsCreated)
            {
                for (int i = 0; i < m_Districts.Length; i++)
                {
                    var district = m_Districts[i];
                    if (district.households.IsCreated) district.households.Dispose();
                    if (district.serviceBuildings.IsCreated) district.serviceBuildings.Dispose();
                    if (district.servicePrefabs.IsCreated) district.servicePrefabs.Dispose();
                    if (district.policies.IsCreated) district.policies.Dispose();
                }
                m_Districts.Dispose();
            }
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (!_dDPVBinding.value) return;

            if (_uiUpdateState.Advance())
            {
                UpdateDistrictData();
            }

            base.OnUpdate();
        }

        private void UpdateDistrictData()
        {
            // Initialize districts
            if (!m_Districts.IsCreated)
            {
                m_Districts = new NativeList<DistrictEntry>(64, Allocator.Persistent);
            }

            m_Districts.Clear();

            using var districts = m_DistrictQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < districts.Length; i++)
            {
                m_Districts.Add(CreateDistrictEntry(districts[i]));
            }

            if (m_Districts.Length == 0) return;

            // Build district map for lookups
            var districtMap = new NativeHashMap<Entity, int>(m_Districts.Length, Allocator.TempJob);
            for (int i = 0; i < m_Districts.Length; i++)
            {
                districtMap.TryAdd(m_Districts[i].district, i);
            }

            using (districtMap)
            {
                JobHandle dependency = Dependency;
                
                // Process citizens with chunk job - much more efficient than parallel for
                var citizenJob = new ProcessCitizensJob
                {
                    m_EntityHandle = SystemAPI.GetEntityTypeHandle(),
                    m_CitizenHandle = SystemAPI.GetComponentTypeHandle<Citizen>(true),
                    m_HouseholdMemberHandle = SystemAPI.GetComponentTypeHandle<HouseholdMember>(true),
                    m_StudentLookup = SystemAPI.GetComponentLookup<Game.Citizens.Student>(true),
                    m_WorkerLookup = SystemAPI.GetComponentLookup<Worker>(true),
                    m_HouseholdLookup = SystemAPI.GetComponentLookup<Household>(true),
                    m_PropertyRenterLookup = SystemAPI.GetComponentLookup<PropertyRenter>(true),
                    m_CurrentDistrictLookup = SystemAPI.GetComponentLookup<CurrentDistrict>(true),
                    m_HealthProblemLookup = SystemAPI.GetComponentLookup<HealthProblem>(true),
                    m_MovingAwayLookup = SystemAPI.GetComponentLookup<MovingAway>(true),
                    m_HouseholdCitizenLookup = SystemAPI.GetBufferLookup<HouseholdCitizen>(true),
                    m_HouseholdAnimalLookup = SystemAPI.GetBufferLookup<HouseholdAnimal>(true),
                    m_CityModifierLookup = SystemAPI.GetBufferLookup<CityModifier>(true),
                    m_City = m_CitySystem.City,
                    m_EducationParameterData = m_EducationParameterQuery.GetSingleton<EducationParameterData>(),
                    m_DistrictIndexMap = districtMap,
                    m_Results = m_Districts.AsArray()
                };
                
                dependency = citizenJob.Schedule(m_CitizenQuery, dependency);
                
                // Process buildings with chunk job
                var buildingJob = new ProcessBuildingsJob
                {
                    m_EntityHandle = SystemAPI.GetEntityTypeHandle(),
                    m_PrefabRefHandle = SystemAPI.GetComponentTypeHandle<PrefabRef>(true),
                    m_CurrentDistrictHandle = SystemAPI.GetComponentTypeHandle<CurrentDistrict>(true),
                    m_AbandonedLookup = SystemAPI.GetComponentLookup<Abandoned>(true),
                    m_PropertyDataLookup = SystemAPI.GetComponentLookup<BuildingPropertyData>(true),
                    m_WorkProviderLookup = SystemAPI.GetComponentLookup<WorkProvider>(true),
                    m_CompanyDataLookup = SystemAPI.GetComponentLookup<CompanyData>(true),
                    m_WorkplaceDataLookup = SystemAPI.GetComponentLookup<WorkplaceData>(true),
                    m_SpawnableDataLookup = SystemAPI.GetComponentLookup<SpawnableBuildingData>(true),
                    m_SchoolDataLookup = SystemAPI.GetComponentLookup<SchoolData>(true),
                    m_HouseholdLookup = SystemAPI.GetComponentLookup<Household>(true),
                    m_PrefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(true),
                    m_PropertyRenterLookup = SystemAPI.GetComponentLookup<PropertyRenter>(true),
                    m_RenterLookup = SystemAPI.GetBufferLookup<Renter>(true),
                    m_EmployeeLookup = SystemAPI.GetBufferLookup<Employee>(true),
                    m_ServiceDistrictLookup = SystemAPI.GetBufferLookup<ServiceDistrict>(true),
                    m_StudentLookup = SystemAPI.GetBufferLookup<Game.Buildings.Student>(true),
                    m_EfficiencyLookup = SystemAPI.GetBufferLookup<Efficiency>(true),
                    m_InstalledUpgradeLookup = SystemAPI.GetBufferLookup<InstalledUpgrade>(true),
                    m_HouseholdCitizenLookup = SystemAPI.GetBufferLookup<HouseholdCitizen>(true),
                    m_HouseholdAnimalLookup = SystemAPI.GetBufferLookup<HouseholdAnimal>(true),
                    m_DistrictIndexMap = districtMap,
                    m_Results = m_Districts.AsArray()
                };
                
                dependency = buildingJob.Schedule(m_BuildingQuery, dependency);
                
                // Process policies
                using var policyEntities = m_DistrictPoliciesQuery.ToEntityArray(Allocator.TempJob);
                var policyJob = new ProcessDistrictPoliciesJob
                {
                    m_EntityHandle = SystemAPI.GetEntityTypeHandle(),
                    m_PolicyHandle = SystemAPI.GetBufferTypeHandle<Policy>(true),
                    PolicyEntities = policyEntities,
                    m_DistrictIndexMap = districtMap,
                    Districts = m_Districts.AsArray()
                };

                dependency = policyJob.Schedule(m_DistrictQuery, dependency);
                dependency.Complete();

                // Finalize district data
                for (int i = 0; i < m_Districts.Length; i++)
                {
                    var district = m_Districts[i];
                    district.wealthKey = CitizenUIUtils.GetAverageHouseholdWealth(
                        EntityManager, district.households, SystemAPI.GetSingleton<CitizenHappinessParameterData>());
                    district.elementaryAvailability = IndicatorValue.Calculate(district.elementaryCapacity, district.elementaryEligible);
                    district.highSchoolAvailability = IndicatorValue.Calculate(district.highSchoolCapacity, district.highSchoolEligible);
                    district.collegeAvailability = IndicatorValue.Calculate(district.collegeCapacity, district.collegeEligible);
                    district.universityAvailability = IndicatorValue.Calculate(district.universityCapacity, district.universityEligible);
                    m_Districts[i] = district;
                }
                
                Dependency = dependency;
            }
        }

        private DistrictEntry CreateDistrictEntry(Entity district)
        {
            return new DistrictEntry
            {
                district = district,
                households = new NativeList<Entity>(32, Allocator.Persistent),
                serviceBuildings = new NativeList<Entity>(16, Allocator.Persistent),
                servicePrefabs = new NativeList<Entity>(16, Allocator.Persistent),
                policies = new NativeList<Entity>(8, Allocator.Persistent),
                residentCount = 0,
                petCount = 0,
                householdCount = 0,
                maxHouseholds = 0,
                ageData = new AgeData(0, 0, 0, 0),
                educationData = new EducationData(0, 0, 0, 0, 0),
                wealthKey = new HouseholdWealthKey(),
                employeeCount = 0,
                maxEmployees = 0,
                educationDataEmployees = new EmploymentData(0, 0, 0, 0, 0, 0),
                educationDataWorkplaces = new EmploymentData(0, 0, 0, 0, 0, 0),
                elementaryEligible = 0,
                highSchoolEligible = 0,
                collegeEligible = 0,
                universityEligible = 0,
                elementaryCapacity = 0,
                highSchoolCapacity = 0,
                collegeCapacity = 0,
                universityCapacity = 0,
                elementaryStudents = 0,
                highSchoolStudents = 0,
                collegeStudents = 0,
                universityStudents = 0,
                elementaryAvailability = new IndicatorValue(),
                highSchoolAvailability = new IndicatorValue(),
                collegeAvailability = new IndicatorValue(),
                universityAvailability = new IndicatorValue()
            };
        }

        public void WriteDistricts(IJsonWriter writer)
        {
            writer.ArrayBegin(m_Districts.Length);
            for (int i = 0; i < m_Districts.Length; i++)
            {
                var district = m_Districts[i];
                writer.TypeBegin("DistrictDataModel");

                writer.PropertyName("entity");
                writer.Write(district.district);

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

                writer.PropertyName("ageData");
                WriteAgeData(writer, district.ageData);

                writer.PropertyName("educationData");
                WriteEducationData(writer, district.educationData);

                writer.PropertyName("wealthKey");
                writer.Write(district.wealthKey.ToString());

                writer.PropertyName("employeeCount");
                writer.Write(district.employeeCount);

                writer.PropertyName("maxEmployees");
                writer.Write(district.maxEmployees);

                writer.PropertyName("educationDataEmployees");
                WriteEmploymentData(writer, district.educationDataEmployees);

                writer.PropertyName("educationDataWorkplaces");
                WriteEmploymentData(writer, district.educationDataWorkplaces);

                writer.PropertyName("serviceBuildings");
                WriteServiceData(writer, district.serviceBuildings, district.servicePrefabs);

                writer.PropertyName("policies");
                WritePolicyData(writer, district.policies);

                writer.PropertyName("elementaryEligible");
                writer.Write(district.elementaryEligible);

                writer.PropertyName("highSchoolEligible");
                writer.Write(district.highSchoolEligible);

                writer.PropertyName("collegeEligible");
                writer.Write(district.collegeEligible);

                writer.PropertyName("universityEligible");
                writer.Write(district.universityEligible);

                writer.PropertyName("elementaryCapacity");
                writer.Write(district.elementaryCapacity);

                writer.PropertyName("highSchoolCapacity");
                writer.Write(district.highSchoolCapacity);

                writer.PropertyName("collegeCapacity");
                writer.Write(district.collegeCapacity);

                writer.PropertyName("universityCapacity");
                writer.Write(district.universityCapacity);

                writer.PropertyName("elementaryStudents");
                writer.Write(district.elementaryStudents);

                writer.PropertyName("highSchoolStudents");
                writer.Write(district.highSchoolStudents);

                writer.PropertyName("collegeStudents");
                writer.Write(district.collegeStudents);

                writer.PropertyName("universityStudents");
                writer.Write(district.universityStudents);

                writer.PropertyName("elementaryAvailability");
                district.elementaryAvailability.Write(writer);

                writer.PropertyName("highSchoolAvailability");
                district.highSchoolAvailability.Write(writer);

                writer.PropertyName("collegeAvailability");
                district.collegeAvailability.Write(writer);

                writer.PropertyName("universityAvailability");
                district.universityAvailability.Write(writer);

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

        public void WriteEmploymentData(IJsonWriter writer, EmploymentData employmentData)
        {
            writer.TypeBegin("EmploymentData");
            writer.PropertyName("uneducated");
            writer.Write(employmentData.uneducated);
            writer.PropertyName("poorlyEducated");
            writer.Write(employmentData.poorlyEducated);
            writer.PropertyName("educated");
            writer.Write(employmentData.educated);
            writer.PropertyName("wellEducated");
            writer.Write(employmentData.wellEducated);
            writer.PropertyName("highlyEducated");
            writer.Write(employmentData.highlyEducated);
            writer.PropertyName("openPositions");
            writer.Write(employmentData.openPositions);
            writer.PropertyName("total");
            writer.Write(employmentData.total);
            writer.TypeEnd();
        }

        private void WriteServiceData(IJsonWriter writer, NativeList<Entity> serviceBuildings, NativeList<Entity> servicePrefabs)
        {
            writer.ArrayBegin(serviceBuildings.Length);
            for (int i = 0; i < serviceBuildings.Length; i++)
            {
                writer.TypeBegin("LocalServiceBuilding");
                writer.PropertyName("name");
                m_NameSystem.BindName(writer, serviceBuildings[i]);
                writer.PropertyName("serviceIcon");
                writer.Write(m_ImageSystem.GetGroupIcon(servicePrefabs[i]));
                writer.PropertyName("entity");
                writer.Write(serviceBuildings[i]);
                writer.TypeEnd();
            }
            writer.ArrayEnd();
        }

        private void WritePolicyData(IJsonWriter writer, NativeList<Entity> policies)
        {
            writer.ArrayBegin(policies.Length);
            for (int i = 0; i < policies.Length; i++)
            {
                writer.TypeBegin("DistrictPolicy");
                writer.PropertyName("name");

                if (m_PrefabSystem.TryGetPrefab<PolicyPrefab>(policies[i], out var prefab))
                {
                    writer.Write(prefab.name);
                }
                else
                {
                    writer.Write("Unknown Policy");
                }

                writer.PropertyName("icon");
                writer.Write(m_ImageSystem.GetIconOrGroupIcon(policies[i]));
                writer.PropertyName("entity");
                writer.Write(policies[i]);
                writer.TypeEnd();
            }
            writer.ArrayEnd();
        }

        private void SetDistrictDataVisibility(bool open)
        {
            _dDPVBinding.Update(open);

            if (open)
            {
                m_uiDistricts.Update();
            }
        }
    }
}