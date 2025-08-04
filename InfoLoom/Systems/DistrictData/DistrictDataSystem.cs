using System;
using System.Collections.Generic;
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
    public partial class DistrictDataSystem : GameSystemBase
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

        // Unified job that processes all citizen data in one pass
        [BurstCompile]
        private struct ProcessCitizenDataJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle m_EntityHandle;
            [ReadOnly] public ComponentTypeHandle<Citizen> m_CitizenHandle;
            [ReadOnly] public ComponentTypeHandle<Game.Citizens.Student> m_StudentHandle;
            [ReadOnly] public ComponentTypeHandle<Worker> m_WorkerHandle;
            [ReadOnly] public ComponentLookup<HouseholdMember> m_HouseholdMemberLookup;
            [ReadOnly] public ComponentLookup<Household> m_HouseholdLookup;
            [ReadOnly] public ComponentLookup<PropertyRenter> m_PropertyRenterLookup;
            [ReadOnly] public ComponentLookup<CurrentDistrict> m_CurrentDistrictLookup;
            [ReadOnly] public ComponentLookup<HealthProblem> m_HealthProblemLookup;
            [ReadOnly] public ComponentLookup<MovingAway> m_MovingAwayLookup;
            [ReadOnly] public BufferLookup<CityModifier> m_CityModifierLookup;
            [ReadOnly] public Entity m_City;
            [ReadOnly] public EducationParameterData m_EducationParameterData;
            [ReadOnly] public NativeHashMap<Entity, int> m_DistrictIndexMap;

            public NativeArray<int> m_AgeData; // districts * 4 (age categories)
            public NativeArray<int> m_EducationData; // districts * 5 (education levels)
            public NativeArray<int> m_EligibilityData; // districts * 4 (school levels)

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entities = chunk.GetNativeArray(m_EntityHandle);
                NativeArray<Citizen> citizens = chunk.GetNativeArray(ref m_CitizenHandle);
                
                bool hasStudents = chunk.Has(ref m_StudentHandle);
                bool hasWorkers = chunk.Has(ref m_WorkerHandle);
                
                NativeArray<Game.Citizens.Student> students = hasStudents ? chunk.GetNativeArray(ref m_StudentHandle) : default;
                NativeArray<Worker> workers = hasWorkers ? chunk.GetNativeArray(ref m_WorkerHandle) : default;

                var cityModifiers = m_CityModifierLookup[m_City];

                for (int i = 0; i < chunk.Count; i++)
                {
                    Entity citizenEntity = entities[i];
                    Citizen citizen = citizens[i];

                    if (!GetDistrictIndex(citizenEntity, out int districtIndex))
                        continue;

                    // Process age data
                    int ageIndex = districtIndex * 4 + (int)citizen.GetAge();
                    m_AgeData[ageIndex]++;

                    // Process education data
                    int educationIndex = districtIndex * 5 + citizen.GetEducationLevel();
                    m_EducationData[educationIndex]++;

                    // Process school eligibility
                    ProcessSchoolEligibility(citizen, citizenEntity, districtIndex, hasStudents && i < students.Length ? students[i] : default, 
                                           hasWorkers && i < workers.Length, cityModifiers);
                }
            }

            private bool GetDistrictIndex(Entity citizenEntity, out int districtIndex)
            {
                districtIndex = -1;
                
                if (!m_HouseholdMemberLookup.TryGetComponent(citizenEntity, out var householdMember) ||
                    !m_HouseholdLookup.TryGetComponent(householdMember.m_Household, out var household) ||
                    !m_PropertyRenterLookup.TryGetComponent(householdMember.m_Household, out var propertyRenter) ||
                    !m_CurrentDistrictLookup.TryGetComponent(propertyRenter.m_Property, out var currentDistrict))
                    return false;

                // Skip invalid citizens
                if (CitizenUtils.IsDead(citizenEntity, ref m_HealthProblemLookup) ||
                    (household.m_Flags & HouseholdFlags.MovedIn) == 0 ||
                    (household.m_Flags & HouseholdFlags.Tourist) != 0 ||
                    m_MovingAwayLookup.HasComponent(householdMember.m_Household))
                    return false;

                return m_DistrictIndexMap.TryGetValue(currentDistrict.m_District, out districtIndex);
            }

            private void ProcessSchoolEligibility(Citizen citizen, Entity citizenEntity, int districtIndex, 
                                                Game.Citizens.Student student, bool hasWorker, 
                                                DynamicBuffer<CityModifier> cityModifiers)
            {
                CitizenAge age = citizen.GetAge();
                
                if (student.m_School != Entity.Null) // Current student
                {
                    int eligibilityIndex = districtIndex * 4 + (student.m_Level - 1);
                    if (eligibilityIndex >= 0 && eligibilityIndex < m_EligibilityData.Length)
                        m_EligibilityData[eligibilityIndex]++;
                }
                else // Potential student
                {
                    float willingness = citizen.GetPseudoRandom(CitizenPseudoRandom.StudyWillingness).NextFloat();
                    
                    if (age == CitizenAge.Child)
                    {
                        m_EligibilityData[districtIndex * 4]++; // Elementary
                    }
                    else if (citizen.GetEducationLevel() == 1 && age <= CitizenAge.Adult)
                    {
                        float probability = ApplyToSchoolSystem.GetEnteringProbability(
                            age, hasWorker, 2, (int)citizen.m_WellBeing, willingness, cityModifiers, ref m_EducationParameterData);
                        m_EligibilityData[districtIndex * 4 + 1] += (int)math.ceil(probability); // High School
                    }
                    else if (citizen.GetEducationLevel() == 2 && citizen.GetFailedEducationCount() < 3)
                    {
                        float universityProb = ApplyToSchoolSystem.GetEnteringProbability(
                            age, hasWorker, 4, (int)citizen.m_WellBeing, willingness, cityModifiers, ref m_EducationParameterData);
                        m_EligibilityData[districtIndex * 4 + 3] += (int)math.ceil(universityProb); // University

                        float collegeProb = (1f - universityProb) * ApplyToSchoolSystem.GetEnteringProbability(
                            age, hasWorker, 3, (int)citizen.m_WellBeing, willingness, cityModifiers, ref m_EducationParameterData);
                        m_EligibilityData[districtIndex * 4 + 2] += (int)math.ceil(collegeProb); // College
                    }
                }
            }
        }

        // Unified job that processes all building data in one pass
        [BurstCompile]
        private struct ProcessBuildingDataJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle m_EntityHandle;
            [ReadOnly] public ComponentTypeHandle<PrefabRef> m_PrefabRefHandle;
            [ReadOnly] public ComponentTypeHandle<CurrentDistrict> m_CurrentDistrictHandle;
            [ReadOnly] public BufferTypeHandle<Renter> m_RenterHandle;
            [ReadOnly] public BufferTypeHandle<Employee> m_EmployeeHandle;
            [ReadOnly] public BufferTypeHandle<ServiceDistrict> m_ServiceDistrictHandle;
            [ReadOnly] public BufferTypeHandle<Game.Buildings.Student> m_StudentHandle;
            [ReadOnly] public BufferTypeHandle<Efficiency> m_EfficiencyHandle;
            [ReadOnly] public ComponentTypeHandle<WorkProvider> m_WorkProviderHandle;
            
            [ReadOnly] public ComponentLookup<Abandoned> m_AbandonedLookup;
            [ReadOnly] public ComponentLookup<BuildingPropertyData> m_PropertyDataLookup;
            [ReadOnly] public ComponentLookup<Household> m_HouseholdLookup;
            [ReadOnly] public ComponentLookup<WorkProvider> m_WorkProviderLookup;
            [ReadOnly] public ComponentLookup<CompanyData> m_CompanyDataLookup;
            [ReadOnly] public ComponentLookup<WorkplaceData> m_WorkplaceDataLookup;
            [ReadOnly] public ComponentLookup<SpawnableBuildingData> m_SpawnableDataLookup;
            [ReadOnly] public ComponentLookup<PropertyRenter> m_PropertyRenterLookup;
            [ReadOnly] public ComponentLookup<PrefabRef> m_PrefabRefLookup;
            [ReadOnly] public ComponentLookup<SchoolData> m_SchoolDataLookup;
            [ReadOnly] public BufferLookup<HouseholdCitizen> m_HouseholdCitizenLookup;
            [ReadOnly] public BufferLookup<HouseholdAnimal> m_HouseholdAnimalLookup;
            [ReadOnly] public BufferLookup<Employee> m_EmployeeLookup;
            [ReadOnly] public BufferLookup<InstalledUpgrade> m_InstalledUpgradeLookup;
            [ReadOnly] public NativeHashMap<Entity, int> m_DistrictIndexMap;

            public NativeArray<DistrictEntry> m_Districts;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entities = chunk.GetNativeArray(m_EntityHandle);
                NativeArray<PrefabRef> prefabs = chunk.GetNativeArray(ref m_PrefabRefHandle);
                NativeArray<CurrentDistrict> districts = chunk.GetNativeArray(ref m_CurrentDistrictHandle);

                for (int i = 0; i < chunk.Count; i++)
                {
                    Entity building = entities[i];
                    Entity prefab = prefabs[i].m_Prefab;
                    Entity districtEntity = districts[i].m_District;

                    if (m_AbandonedLookup.HasComponent(building) ||
                        !m_DistrictIndexMap.TryGetValue(districtEntity, out int districtIndex))
                        continue;

                    var district = m_Districts[districtIndex];

                    // Process residential properties
                    ProcessResidentialBuilding(building, prefab, ref district, chunk, i);
                    
                    // Process service buildings
                    ProcessServiceBuilding(building, prefab, districtEntity, ref district, chunk, i);
                    
                    // Process employment buildings
                    ProcessEmploymentBuilding(building, prefab, ref district, chunk, i);
                    
                    // Process schools
                    ProcessSchoolBuilding(building, prefab, ref district, chunk, i);

                    m_Districts[districtIndex] = district;
                }
            }

            private void ProcessResidentialBuilding(Entity building, Entity prefab, ref DistrictEntry district, 
                                                  in ArchetypeChunk chunk, int index)
            {
                if (!m_PropertyDataLookup.TryGetComponent(prefab, out var propertyData) ||
                    propertyData.m_ResidentialProperties <= 0 ||
                    !chunk.Has(ref m_RenterHandle))
                    return;

                district.maxHouseholds += propertyData.m_ResidentialProperties;
                
                var renters = chunk.GetBufferAccessor(ref m_RenterHandle)[index];
                for (int k = 0; k < renters.Length; k++)
                {
                    Entity household = renters[k].m_Renter;
                    if (ProcessHousehold(household, ref district))
                    {
                        district.households.Add(household);
                    }
                }
            }

            private bool ProcessHousehold(Entity household, ref DistrictEntry district)
            {
                if (!m_HouseholdLookup.HasComponent(household) ||
                    !m_HouseholdCitizenLookup.TryGetBuffer(household, out var citizens))
                    return false;

                district.householdCount++;
                district.residentCount += citizens.Length;

                if (m_HouseholdAnimalLookup.TryGetBuffer(household, out var animals))
                    district.petCount += animals.Length;

                return true;
            }

            private void ProcessServiceBuilding(Entity building, Entity prefab, Entity districtEntity, 
                                              ref DistrictEntry district, in ArchetypeChunk chunk, int index)
            {
                if (!chunk.Has(ref m_ServiceDistrictHandle))
                    return;

                var serviceDistricts = chunk.GetBufferAccessor(ref m_ServiceDistrictHandle)[index];
                for (int j = 0; j < serviceDistricts.Length; j++)
                {
                    if (serviceDistricts[j].m_District == districtEntity)
                    {
                        district.serviceBuildings.Add(building);
                        district.servicePrefabs.Add(prefab);
                        break;
                    }
                }
            }

            private void ProcessEmploymentBuilding(Entity building, Entity prefab, ref DistrictEntry district, 
                                                 in ArchetypeChunk chunk, int index)
            {
                Entity employeeEntity = building;
                bool hasEmployees = false;

                // Check for direct employees
                if (chunk.Has(ref m_EmployeeHandle) && chunk.Has(ref m_WorkProviderHandle))
                {
                    hasEmployees = true;
                }
                // Check for company renters
                else if (chunk.Has(ref m_RenterHandle))
                {
                    var renters = chunk.GetBufferAccessor(ref m_RenterHandle)[index];
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

                if (!hasEmployees)
                    return;

                ProcessEmployeeData(employeeEntity, prefab, building, ref district, chunk, index);
            }

            private void ProcessEmployeeData(Entity employeeEntity, Entity prefab, Entity building, 
                                           ref DistrictEntry district, in ArchetypeChunk chunk, int index)
            {
                if (!m_WorkProviderLookup.TryGetComponent(employeeEntity, out var workProvider) ||
                    !m_PrefabRefLookup.TryGetComponent(employeeEntity, out var employeePrefabRef) ||
                    !m_WorkplaceDataLookup.TryGetComponent(employeePrefabRef.m_Prefab, out var workplaceData))
                    return;

                int buildingLevel = GetBuildingLevel(prefab, building);
                var employees = m_EmployeeLookup.HasBuffer(employeeEntity) ? m_EmployeeLookup[employeeEntity] : default;

                EmploymentData workplacesData = EmploymentData.GetWorkplacesData(
                    workProvider.m_MaxWorkers, buildingLevel, workplaceData.m_Complexity);

                district.employeeCount += employees.IsCreated ? employees.Length : 0;
                district.maxEmployees += workplacesData.total;
                district.educationDataWorkplaces += workplacesData;
                
                if (employees.IsCreated)
                {
                    district.educationDataEmployees += EmploymentData.GetEmployeesData(
                        employees, workplacesData.total - employees.Length);
                }
            }

            private void ProcessSchoolBuilding(Entity building, Entity prefab, ref DistrictEntry district, 
                                             in ArchetypeChunk chunk, int index)
            {
                if (!chunk.Has(ref m_StudentHandle) || !chunk.Has(ref m_EfficiencyHandle) ||
                    !m_SchoolDataLookup.TryGetComponent(prefab, out var schoolData))
                    return;

                // Check efficiency
                if (BuildingUtils.GetEfficiency(chunk.GetBufferAccessor(ref m_EfficiencyHandle), index) == 0.0)
                    return;

                // Handle upgrades
                if (m_InstalledUpgradeLookup.TryGetBuffer(building, out var upgrades))
                {
                    UpgradeUtils.CombineStats(ref schoolData, upgrades, ref m_PrefabRefLookup, ref m_SchoolDataLookup);
                }

                var students = chunk.GetBufferAccessor(ref m_StudentHandle)[index];
                
                switch (schoolData.m_EducationLevel)
                {
                    case 1:
                        district.elementaryStudents += students.Length;
                        district.elementaryCapacity += schoolData.m_StudentCapacity;
                        break;
                    case 2:
                        district.highSchoolStudents += students.Length;
                        district.highSchoolCapacity += schoolData.m_StudentCapacity;
                        break;
                    case 3:
                        district.collegeStudents += students.Length;
                        district.collegeCapacity += schoolData.m_StudentCapacity;
                        break;
                    case 4:
                        district.universityStudents += students.Length;
                        district.universityCapacity += schoolData.m_StudentCapacity;
                        break;
                }
            }

            private int GetBuildingLevel(Entity prefab, Entity building)
            {
                if (m_SpawnableDataLookup.TryGetComponent(prefab, out var spawnableData))
                    return spawnableData.m_Level;
                    
                if (m_PropertyRenterLookup.TryGetComponent(building, out var propertyRenter) &&
                    m_PrefabRefLookup.TryGetComponent(propertyRenter.m_Property, out var propertyPrefabRef) &&
                    m_SpawnableDataLookup.TryGetComponent(propertyPrefabRef.m_Prefab, out var propertySpawnableData))
                    return propertySpawnableData.m_Level;
                    
                return 1;
            }
        }

        // Simple job for policies
        [BurstCompile]
        private struct ProcessDistrictPoliciesJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle m_EntityHandle;
            [ReadOnly] public BufferTypeHandle<Policy> m_PolicyHandle;
            [ReadOnly] public NativeArray<Entity> PolicyEntities;
            [ReadOnly] public NativeHashMap<Entity, int> m_DistrictIndexMap;
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
        
        private PrefabSystem m_PrefabSystem;
        private ImageSystem m_ImageSystem;
        private NameSystem m_NameSystem;
        private CitySystem m_CitySystem;
        private SimulationSystem m_SimulationSystem;
        private NativeList<DistrictEntry> m_Districts;
        
        public static readonly int kUpdatesPerDay = 1;
        public bool IsPanelVisible { get; set; }

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

            InitializeSystems();
            m_Districts = new NativeList<DistrictEntry>(64, Allocator.Persistent);
            RequireForUpdate<District>();
        }

        private void InitializeSystems()
        {
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_ImageSystem = World.GetOrCreateSystemManaged<ImageSystem>();
            m_NameSystem = World.GetOrCreateSystemManaged<NameSystem>();
            m_CitySystem = World.GetOrCreateSystemManaged<CitySystem>();
            m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
        }

        protected override void OnDestroy()
        {
            if (m_Districts.IsCreated)
            {
                DisposeDistricts();
                m_Districts.Dispose();
            }
            base.OnDestroy();
        }

        private void DisposeDistricts()
        {
            if (!m_Districts.IsCreated) return;

            for (int i = 0; i < m_Districts.Length; i++)
            {
                var district = m_Districts[i];
                if (district.households.IsCreated) district.households.Dispose();
                if (district.serviceBuildings.IsCreated) district.serviceBuildings.Dispose();
                if (district.servicePrefabs.IsCreated) district.servicePrefabs.Dispose();
                if (district.policies.IsCreated) district.policies.Dispose();
            }
            
            // Don't dispose the main list here since we want to reuse it
            // Only dispose in OnDestroy
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 262144 / (kUpdatesPerDay * 128);
        }

        protected override void OnUpdate()
        {
            if (!IsPanelVisible) return;

            InitializeDistricts();
            
            using var districtMap = BuildDistrictMap();
            
            // Process all citizen data in one job
            ProcessCitizenData(districtMap);
            
            // Process all building data in one job
            ProcessBuildingData(districtMap);
            
            // Process policies
            ProcessPolicies(districtMap);
            
            // Calculate wealth and availability indicators
            FinalizeDistrictData();
        }

        private void InitializeDistricts()
        {
            // Ensure m_Districts is created if it doesn't exist
            if (!m_Districts.IsCreated)
            {
                m_Districts = new NativeList<DistrictEntry>(64, Allocator.Persistent);
            }

            DisposeDistricts();
            m_Districts.Clear();

            using var districts = m_DistrictQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < districts.Length; i++)
            {
                m_Districts.Add(CreateDistrictEntry(districts[i]));
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
                // All other fields default to zero/empty
            };
        }

        private NativeHashMap<Entity, int> BuildDistrictMap()
        {
            var districtMap = new NativeHashMap<Entity, int>(m_Districts.Length, Allocator.TempJob);
            for (int i = 0; i < m_Districts.Length; i++)
            {
                districtMap.TryAdd(m_Districts[i].district, i);
            }
            return districtMap;
        }

        private void ProcessCitizenData(NativeHashMap<Entity, int> districtMap)
        {
            int districtCount = m_Districts.Length;
            using var ageData = new NativeArray<int>(districtCount * 4, Allocator.TempJob);
            using var educationData = new NativeArray<int>(districtCount * 5, Allocator.TempJob);
            using var eligibilityData = new NativeArray<int>(districtCount * 4, Allocator.TempJob);

            var job = new ProcessCitizenDataJob
            {
                m_EntityHandle = GetEntityTypeHandle(),
                m_CitizenHandle = GetComponentTypeHandle<Citizen>(true),
                m_StudentHandle = GetComponentTypeHandle<Game.Citizens.Student>(true),
                m_WorkerHandle = GetComponentTypeHandle<Worker>(true),
                m_HouseholdMemberLookup = GetComponentLookup<HouseholdMember>(true),
                m_HouseholdLookup = GetComponentLookup<Household>(true),
                m_PropertyRenterLookup = GetComponentLookup<PropertyRenter>(true),
                m_CurrentDistrictLookup = GetComponentLookup<CurrentDistrict>(true),
                m_HealthProblemLookup = GetComponentLookup<HealthProblem>(true),
                m_MovingAwayLookup = GetComponentLookup<MovingAway>(true),
                m_CityModifierLookup = GetBufferLookup<CityModifier>(true),
                m_City = m_CitySystem.City,
                m_EducationParameterData = m_EducationParameterQuery.GetSingleton<EducationParameterData>(),
                m_DistrictIndexMap = districtMap,
                m_AgeData = ageData,
                m_EducationData = educationData,
                m_EligibilityData = eligibilityData
            };

            Dependency = job.Schedule(m_CitizenQuery, Dependency);
            Dependency.Complete();

            // Copy results back to districts
            for (int i = 0; i < districtCount; i++)
            {
                var district = m_Districts[i];
                
                district.ageData = new AgeData(
                    ageData[i * 4 + 0], ageData[i * 4 + 1],
                    ageData[i * 4 + 2], ageData[i * 4 + 3]);
                    
                district.educationData = new EducationData(
                    educationData[i * 5 + 0], educationData[i * 5 + 1],
                    educationData[i * 5 + 2], educationData[i * 5 + 3], educationData[i * 5 + 4]);
                    
                district.elementaryEligible = eligibilityData[i * 4 + 0];
                district.highSchoolEligible = eligibilityData[i * 4 + 1];
                district.collegeEligible = eligibilityData[i * 4 + 2];
                district.universityEligible = eligibilityData[i * 4 + 3];

                m_Districts[i] = district;
            }
        }

        private void ProcessBuildingData(NativeHashMap<Entity, int> districtMap)
        {
            var job = new ProcessBuildingDataJob
            {
                m_EntityHandle = GetEntityTypeHandle(),
                m_PrefabRefHandle = GetComponentTypeHandle<PrefabRef>(true),
                m_CurrentDistrictHandle = GetComponentTypeHandle<CurrentDistrict>(true),
                m_RenterHandle = GetBufferTypeHandle<Renter>(true),
                m_EmployeeHandle = GetBufferTypeHandle<Employee>(true),
                m_ServiceDistrictHandle = GetBufferTypeHandle<ServiceDistrict>(true),
                m_StudentHandle = GetBufferTypeHandle<Game.Buildings.Student>(true),
                m_EfficiencyHandle = GetBufferTypeHandle<Efficiency>(true),
                m_WorkProviderHandle = GetComponentTypeHandle<WorkProvider>(true),
                
                m_AbandonedLookup = GetComponentLookup<Abandoned>(true),
                m_PropertyDataLookup = GetComponentLookup<BuildingPropertyData>(true),
                m_HouseholdLookup = GetComponentLookup<Household>(true),
                m_WorkProviderLookup = GetComponentLookup<WorkProvider>(true),
                m_CompanyDataLookup = GetComponentLookup<CompanyData>(true),
                m_WorkplaceDataLookup = GetComponentLookup<WorkplaceData>(true),
                m_SpawnableDataLookup = GetComponentLookup<SpawnableBuildingData>(true),
                m_PropertyRenterLookup = GetComponentLookup<PropertyRenter>(true),
                m_PrefabRefLookup = GetComponentLookup<PrefabRef>(true),
                m_SchoolDataLookup = GetComponentLookup<SchoolData>(true),
                m_HouseholdCitizenLookup = GetBufferLookup<HouseholdCitizen>(true),
                m_HouseholdAnimalLookup = GetBufferLookup<HouseholdAnimal>(true),
                m_EmployeeLookup = GetBufferLookup<Employee>(true),
                m_InstalledUpgradeLookup = GetBufferLookup<InstalledUpgrade>(true),
                m_DistrictIndexMap = districtMap,
                m_Districts = m_Districts.AsArray()
            };

            Dependency = job.Schedule(m_BuildingQuery, Dependency);
            Dependency.Complete();
        }

        private void ProcessPolicies(NativeHashMap<Entity, int> districtMap)
        {
            using var policyEntities = m_DistrictPoliciesQuery.ToEntityArray(Allocator.TempJob);
            
            var job = new ProcessDistrictPoliciesJob
            {
                m_EntityHandle = GetEntityTypeHandle(),
                m_PolicyHandle = GetBufferTypeHandle<Policy>(true),
                PolicyEntities = policyEntities,
                m_DistrictIndexMap = districtMap,
                Districts = m_Districts.AsArray()
            };

            Dependency = job.Schedule(m_DistrictQuery, Dependency);
            Dependency.Complete();
        }

        private void FinalizeDistrictData()
        {
            for (int i = 0; i < m_Districts.Length; i++)
            {
                var district = m_Districts[i];
                
                // Calculate wealth
                district.wealthKey = CitizenUIUtils.GetAverageHouseholdWealth(
                    EntityManager, district.households, SystemAPI.GetSingleton<CitizenHappinessParameterData>());
                
                // Calculate availability indicators
                district.elementaryAvailability = IndicatorValue.Calculate(
                    district.elementaryCapacity, district.elementaryEligible);
                district.highSchoolAvailability = IndicatorValue.Calculate(
                    district.highSchoolCapacity, district.highSchoolEligible);
                district.collegeAvailability = IndicatorValue.Calculate(
                    district.collegeCapacity, district.collegeEligible);
                district.universityAvailability = IndicatorValue.Calculate(
                    district.universityCapacity, district.universityEligible);

                m_Districts[i] = district;
            }
        }

        // Keep all your existing WriteDistricts and related methods unchanged
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

                // Employee data
                writer.PropertyName("employeeCount");
                writer.Write(district.employeeCount);
                writer.PropertyName("maxEmployees");
                writer.Write(district.maxEmployees);
                writer.PropertyName("educationDataEmployees");
                WriteEmploymentData(writer, district.educationDataEmployees);
                writer.PropertyName("educationDataWorkplaces");
                WriteEmploymentData(writer, district.educationDataWorkplaces);
                writer.PropertyName("localServiceBuildings");
                WriteServiceData(writer, district.serviceBuildings, district.servicePrefabs);
                writer.PropertyName("entity");
                writer.Write(district.district);

                writer.PropertyName("policyCount");
                writer.Write(district.policies.Length);
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
                    string policyName = prefab.name;
                    string localizedName = policyName;

                    if (GameManager.instance.localizationManager.activeDictionary.TryGetValue($"Policy.TITLE[{policyName}]", out var localizedValue))
                    {
                        localizedName = localizedValue;
                    }

                    writer.Write(localizedName);
                }
                else
                {
                    m_NameSystem.BindName(writer, policies[i]);
                }

                writer.PropertyName("icon");
                writer.Write(m_ImageSystem.GetIconOrGroupIcon(policies[i]));
                writer.PropertyName("entity");
                writer.Write(policies[i]);
                writer.TypeEnd();
            }
            writer.ArrayEnd();
        }
    }
}