using System;
using System.Collections.Generic;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Policies;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Simulation;
using Game.Tools;
using Game.UI;
using Game.UI.InGame; // Uses shared definitions for AgeData and EducationData
using Game.Zones;   // Added for zone-related components
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
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
            [ReadOnly] public NativeHashMap<Entity, int> m_DistrictIndexMap; 

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

        // Combined job to process service buildings and employees in one pass
        [BurstCompile]
        private struct ProcessBuildingsJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle m_EntityHandle;
            [ReadOnly] public ComponentTypeHandle<PrefabRef> m_PrefabRefHandle;
            [ReadOnly] public ComponentTypeHandle<CurrentDistrict> m_CurrentDistrictHandle;
            [ReadOnly] public BufferTypeHandle<ServiceDistrict> m_ServiceDistrictHandle;
            [ReadOnly] public BufferTypeHandle<Renter> m_RenterHandle;
            [ReadOnly] public BufferTypeHandle<Employee> m_EmployeeHandle;
            [ReadOnly] public ComponentTypeHandle<WorkProvider> m_WorkProviderHandle;
            [ReadOnly] public BufferLookup<Employee> m_EmployeeLookup;
            [ReadOnly] public ComponentLookup<WorkProvider> m_WorkProviderLookup;
            [ReadOnly] public ComponentLookup<CompanyData> m_CompanyDataLookup;
            [ReadOnly] public ComponentLookup<WorkplaceData> m_WorkplaceDataLookup;
            [ReadOnly] public ComponentLookup<SpawnableBuildingData> m_SpawnableDataLookup;
            [ReadOnly] public ComponentLookup<Game.Buildings.Park> m_ParkLookup;
            [ReadOnly] public ComponentLookup<PropertyRenter> m_PropertyRenterLookup;
            [ReadOnly] public ComponentLookup<PrefabRef> m_PrefabRefLookup;
            [ReadOnly] public NativeHashMap<Entity, int> m_DistrictIndexMap;
            
            public NativeArray<DistrictEntry> m_Districts;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entities = chunk.GetNativeArray(m_EntityHandle);
                NativeArray<PrefabRef> prefabs = chunk.GetNativeArray(ref m_PrefabRefHandle);
                NativeArray<CurrentDistrict> districts = chunk.GetNativeArray(ref m_CurrentDistrictHandle);
                
                bool hasServiceDistrict = chunk.Has(ref m_ServiceDistrictHandle);
                bool hasRenter = chunk.Has(ref m_RenterHandle);
                bool hasEmployee = chunk.Has(ref m_EmployeeHandle);
                bool hasWorkProvider = chunk.Has(ref m_WorkProviderHandle);

                // Process service buildings if chunk has ServiceDistrict component
                if (hasServiceDistrict)
                {
                    BufferAccessor<ServiceDistrict> serviceDistricts = chunk.GetBufferAccessor(ref m_ServiceDistrictHandle);
                    
                    for (int i = 0; i < chunk.Count; i++)
                    {
                        Entity building = entities[i];
                        Entity prefabEntity = prefabs[i].m_Prefab;
                        var serviceDistrictBuffer = serviceDistricts[i];
                        
                        for (int j = 0; j < serviceDistrictBuffer.Length; j++)
                        {
                            Entity districtEntity = serviceDistrictBuffer[j].m_District;
                            
                            if (m_DistrictIndexMap.TryGetValue(districtEntity, out int districtIndex))
                            {
                                var district = m_Districts[districtIndex];
                                district.serviceBuildings.Add(building);
                                district.servicePrefabs.Add(prefabEntity);
                                m_Districts[districtIndex] = district;
                            }
                        }
                    }
                }

                // Process employee data
                bool isParkChunk = chunk.Has<Game.Buildings.Park>();
                
                for (int i = 0; i < chunk.Count; i++)
                {
                    Entity building = entities[i];
                    Entity prefabEntity = prefabs[i].m_Prefab;
                    Entity districtEntity = districts[i].m_District;
                    
                    if (!m_DistrictIndexMap.TryGetValue(districtEntity, out int districtIndex))
                        continue;

                    // Determine if this building has employees
                    bool hasEmployees = false;
                    Entity employeeEntity = building;
                    
                    if (!isParkChunk && hasRenter)
                    {
                        // Handle buildings with renters
                        var renterBuffer = chunk.GetBufferAccessor(ref m_RenterHandle)[i];
                        
                        if (renterBuffer.Length == 0 && m_SpawnableDataLookup.HasComponent(prefabEntity))
                        {
                            // Skip empty commercial or industrial buildings - handled by zone check logic
                            continue;
                        }
                        
                        for (int j = 0; j < renterBuffer.Length; j++)
                        {
                            Entity renter = renterBuffer[j].m_Renter;
                            
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
                    else if (hasEmployee && hasWorkProvider)
                    {
                        // Direct employee and work provider
                        hasEmployees = true;
                    }
                    
                    if (!hasEmployees)
                        continue;
                    
                    // Process employee data for this building
                    var district = m_Districts[districtIndex];
                    
                    int buildingLevel = 1;
                    if (m_SpawnableDataLookup.TryGetComponent(prefabEntity, out var spawnableData))
                    {
                        buildingLevel = spawnableData.m_Level;
                    }
                    else if (m_PropertyRenterLookup.TryGetComponent(building, out var propertyRenter) && 
                             m_PrefabRefLookup.TryGetComponent(propertyRenter.m_Property, out var propertyPrefabRef) && 
                             m_SpawnableDataLookup.TryGetComponent(propertyPrefabRef.m_Prefab, out var propertySpawnableData))
                    {
                        buildingLevel = propertySpawnableData.m_Level;
                    }
                    
                    if (hasEmployee && hasWorkProvider)
                    {
                        var employees = chunk.GetBufferAccessor(ref m_EmployeeHandle)[i];
                        var workProvider = chunk.GetNativeArray(ref m_WorkProviderHandle)[i];
                        Entity employeePrefab = m_PrefabRefLookup[employeeEntity].m_Prefab;
                        
                        if (m_WorkplaceDataLookup.TryGetComponent(employeePrefab, out var workplaceData))
                        {
                            WorkplaceComplexity complexity = workplaceData.m_Complexity;
                            EmploymentData workplacesData = EmploymentData.GetWorkplacesData(
                                workProvider.m_MaxWorkers, buildingLevel, complexity);
                                
                            district.employeeCount += employees.Length;
                            district.maxEmployees += workplacesData.total;
                            district.educationDataWorkplaces += workplacesData;
                            district.educationDataEmployees += EmploymentData.GetEmployeesData(
                                employees, workplacesData.total - employees.Length);
                                
                            m_Districts[districtIndex] = district;
                        }
                    }
                    else if (m_EmployeeLookup.HasBuffer(employeeEntity) && 
                             m_WorkProviderLookup.TryGetComponent(employeeEntity, out var workProviderData))
                    {
                        Entity employeePrefab = m_PrefabRefLookup[employeeEntity].m_Prefab;
                        
                        if (m_WorkplaceDataLookup.TryGetComponent(employeePrefab, out var workplaceData))
                        {
                            var employees = m_EmployeeLookup[employeeEntity];
                            WorkplaceComplexity complexity = workplaceData.m_Complexity;
                            EmploymentData workplacesData = EmploymentData.GetWorkplacesData(
                                workProviderData.m_MaxWorkers, buildingLevel, complexity);
                                
                            district.employeeCount += employees.Length;
                            district.maxEmployees += workplacesData.total;
                            district.educationDataWorkplaces += workplacesData;
                            district.educationDataEmployees += EmploymentData.GetEmployeesData(
                                employees, workplacesData.total - employees.Length);
                                
                            m_Districts[districtIndex] = district;
                        }
                    }
                }
            }
        }
        
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

               
                district.ageData = new AgeData(children, teens, adults, elders);
                district.educationData = new EducationData(uneducated, poorlyEducated, educated, wellEducated, highlyEducated);
                Districts[index] = district;
            }
        }

        [BurstCompile]
        private struct ProcessDistrictPoliciesJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Entity> PolicyEntities;
            [ReadOnly] public BufferLookup<Policy> PolicyBufferLookup;
            [ReadOnly] public EntityCommandBuffer.ParallelWriter CommandBuffer;
            public NativeArray<DistrictEntry> Districts;

            public void Execute(int index)
            {
                var district = Districts[index];
                district.policies.Clear();
                
                Entity districtEntity = district.district;
                
                if (!PolicyBufferLookup.HasBuffer(districtEntity))
                {
                    Districts[index] = district;
                    return;
                }
                
                var activePolicies = PolicyBufferLookup[districtEntity];
                if (activePolicies.Length == 0)
                {
                    Districts[index] = district;
                    return;
                }
                
                for (int j = 0; j < activePolicies.Length; j++)
                {
                    Policy policy = activePolicies[j];
                    
                    if ((policy.m_Flags & PolicyFlags.Active) == 0)
                        continue;
                        
                    Entity policyEntity = policy.m_Policy;
                    
                    bool isDistrictPolicy = false;
                    for (int k = 0; k < PolicyEntities.Length; k++)
                    {
                        if (PolicyEntities[k] == policyEntity)
                        {
                            isDistrictPolicy = true;
                            break;
                        }
                    }
                    
                    if (!isDistrictPolicy)
                        continue;
                    
                    district.policies.Add(policyEntity);
                }
                
                Districts[index] = district;
            }
        }

        // Combined query for buildings that might have employees or be service buildings
        private EntityQuery m_CombinedBuildingQuery;
        private EntityQuery m_DistrictBuildingQuery;
        private EntityQuery m_HappinessParameterQuery;
        private EntityQuery m_DistrictQuery;
        private EntityQuery m_DistrictPoliciesQuery;
        private PrefabSystem m_PrefabSystem;
        private ImageSystem m_ImageSystem;
        private PrefabUISystem m_PrefabUISystem;
        private PoliciesUISystem m_PoliciesUISystem;
        private SelectedInfoUISystem m_SelectedInfoUISystem;
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

            // Create a combined query for service buildings and employee buildings
            m_CombinedBuildingQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    typeof(Building),
                    typeof(PrefabRef),
                    typeof(CurrentDistrict)
                },
                Any = new ComponentType[]
                {
                    typeof(ServiceDistrict),
                    typeof(Renter),
                    typeof(Employee),
                    typeof(WorkProvider)
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
            
            m_DistrictPoliciesQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<PolicyData>() },
                Any = new ComponentType[]
                {
                    ComponentType.ReadOnly<DistrictOptionData>(),
                    ComponentType.ReadOnly<DistrictModifierData>()
                }
            });
            
            m_NameSystem = World.GetOrCreateSystemManaged<NameSystem>();
            m_Districts = new NativeList<DistrictEntry>(64, Allocator.Persistent);
            m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_ImageSystem = World.GetOrCreateSystemManaged<ImageSystem>();
            m_PoliciesUISystem = World.GetOrCreateSystemManaged<PoliciesUISystem>();
            m_PrefabUISystem = World.GetOrCreateSystemManaged<PrefabUISystem>();

            RequireForUpdate<District>();
        }

        protected override void OnDestroy()
        {
            for (int i = 0; i < m_Districts.Length; i++)
            {
                if (m_Districts[i].households.IsCreated)
                    m_Districts[i].households.Dispose();
                if (m_Districts[i].serviceBuildings.IsCreated)
                    m_Districts[i].serviceBuildings.Dispose();
                if (m_Districts[i].servicePrefabs.IsCreated)
                    m_Districts[i].servicePrefabs.Dispose();
                if (m_Districts[i].policies.IsCreated)
                    m_Districts[i].policies.Dispose();
            }
            if (m_Districts.IsCreated)
                m_Districts.Dispose();
        }
        
        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
           if (Mod.setting.CustomUpdateInterval)
           {
               return Mod.setting.UpdateInterval;
           }
           return 512;
        }
        
        protected override void OnUpdate()
        {
            if (!IsPanelVisible)
                return;
            
            ResetDistrictEntries();
            BuildDistrictEntries();

            using (var districtMap = BuildDistrictMap())
            {
                // Schedule the stats job for residential buildings
                ScheduleStatsJob(districtMap);
                
                // Process service buildings and employees in one pass with a chunk job
                var districtsArray = m_Districts.AsArray();
                var processBuildingsJob = new ProcessBuildingsJob
                {
                    m_EntityHandle = GetEntityTypeHandle(),
                    m_PrefabRefHandle = GetComponentTypeHandle<PrefabRef>(true),
                    m_CurrentDistrictHandle = GetComponentTypeHandle<CurrentDistrict>(true),
                    m_ServiceDistrictHandle = GetBufferTypeHandle<ServiceDistrict>(true),
                    m_RenterHandle = GetBufferTypeHandle<Renter>(true),
                    m_EmployeeHandle = GetBufferTypeHandle<Employee>(true),
                    m_WorkProviderHandle = GetComponentTypeHandle<WorkProvider>(true),
                    m_EmployeeLookup = GetBufferLookup<Employee>(true),
                    m_WorkProviderLookup = GetComponentLookup<WorkProvider>(true),
                    m_CompanyDataLookup = GetComponentLookup<CompanyData>(true),
                    m_WorkplaceDataLookup = GetComponentLookup<WorkplaceData>(true),
                    m_SpawnableDataLookup = GetComponentLookup<SpawnableBuildingData>(true),
                    m_ParkLookup = GetComponentLookup<Game.Buildings.Park>(true),
                    m_PropertyRenterLookup = GetComponentLookup<PropertyRenter>(true),
                    m_PrefabRefLookup = GetComponentLookup<PrefabRef>(true),
                    m_DistrictIndexMap = districtMap,
                    m_Districts = districtsArray
                };
                
                Dependency = processBuildingsJob.Schedule(m_CombinedBuildingQuery, Dependency);
                
                // Process household and citizen data in parallel
                var processDataJob = new ProcessDistrictDataJob
                {
                    Districts = districtsArray,
                    HouseholdCitizenLookup = GetBufferLookup<HouseholdCitizen>(true),
                    CitizenDataLookup = GetComponentLookup<Citizen>(true)
                };
                
                Dependency = processDataJob.Schedule(districtsArray.Length, 32, Dependency);
                Dependency.Complete();
            }

            ProcessWealthData();
            ProcessDistrictPoliciesParallel();
        }

        // Clears and disposes existing district entries.
        private void ResetDistrictEntries()
        {
            for (int i = 0; i < m_Districts.Length; i++)
            {
                if (m_Districts[i].households.IsCreated)
                    m_Districts[i].households.Dispose();
                if (m_Districts[i].serviceBuildings.IsCreated)
                    m_Districts[i].serviceBuildings.Dispose();
                if (m_Districts[i].servicePrefabs.IsCreated)
                    m_Districts[i].servicePrefabs.Dispose();
                if (m_Districts[i].policies.IsCreated)
                    m_Districts[i].policies.Dispose();
            }
            m_Districts.Clear();
        }

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
                        wealthKey = default(HouseholdWealthKey),
                        employeeCount = 0,
                        maxEmployees = 0,
                        educationDataEmployees = default(EmploymentData),
                        educationDataWorkplaces = default(EmploymentData),
                        serviceBuildings = new NativeList<Entity>(16, Allocator.Persistent),
                        servicePrefabs = new NativeList<Entity>(16, Allocator.Persistent),
                        policies = new NativeList<Entity>(8, Allocator.Persistent)
                    });
                }
            }
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

        private void ProcessWealthData()
        {
            for (int i = 0; i < m_Districts.Length; i++)
            {
                var district = m_Districts[i];
                district.wealthKey = CitizenUIUtils.GetAverageHouseholdWealth(EntityManager, district.households, SystemAPI.GetSingleton<CitizenHappinessParameterData>());
                m_Districts[i] = district;
            }
        }

        private void ProcessDistrictPoliciesParallel()
        {
            var policyEntities = m_DistrictPoliciesQuery.ToEntityArray(Allocator.TempJob);
            var districtsArray = m_Districts.AsArray();
            
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            
            var job = new ProcessDistrictPoliciesJob
            {
                PolicyEntities = policyEntities,
                PolicyBufferLookup = GetBufferLookup<Policy>(true),
                CommandBuffer = commandBuffer.AsParallelWriter(),
                Districts = districtsArray
            };
            
            Dependency = job.Schedule(districtsArray.Length, 32, Dependency);
            Dependency.Complete();
            
            commandBuffer.Dispose();
            policyEntities.Dispose();
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
