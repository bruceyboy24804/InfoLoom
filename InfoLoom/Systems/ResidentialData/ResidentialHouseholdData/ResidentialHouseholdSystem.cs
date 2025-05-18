using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Entities;
using Game;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Companies;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.UI;
using Game.UI.InGame;
using InfoLoomTwo.Systems.ResidentialData.ResidentialInfoSection;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using AreaType = Game.Zones.AreaType;
using CitizenHappiness = Game.UI.InGame.CitizenHappiness;
using ServiceCoverage = Game.Net.ServiceCoverage;


namespace InfoLoomTwo.Systems.ResidentialData.ResidentialHouseholdData
{
    [BurstCompile]
    public partial class ResidentialHouseholdSystem : GameSystemBase
    {
        public struct ResidentialDataInformation
        {
            public Entity ResidentialEntity;
            public string ResidentialName;
            public string ResidentialIcon;
            public int CurrentHouseholdCount;
            public int MaxHouseholdCount;
            
            public BuildingHappinessFactorValue[] HappinessFactors;
            public CitizenHappiness OverallHappiness;
        }
        public struct BuildingHappinessFactorValue : IComparable<BuildingHappinessFactorValue>
        {
            public BuildingHappinessFactor Factor;
            public int Value;
            
            public int CompareTo(BuildingHappinessFactorValue other)
            {
                return other.Value.CompareTo(Value);
            }
        }
        private PrefabSystem m_PrefabSystem;
        private ImageSystem m_ImageSystem;
        public ResidentialDataInformation[] m_ResidentialDataInformation;
        private NameSystem m_NameSystem;
        public bool IsPanelVisible;
        private EntityQuery m_ResidentialQuery;
        private EntityQuery m_HouseholdRenterQuery;
        
        private EntityQuery m_CitizenHappinessParameterQuery;
        private EntityQuery m_HealthcareParameterQuery;
        private EntityQuery m_ParkParameterQuery;
        private EntityQuery m_EducationParameterQuery;
        private EntityQuery m_TelecomParameterQuery;
        private EntityQuery m_HappinessFactorParameterQuery;
        private EntityQuery m_GarbageParameterQuery;
        private EntityQuery m_EconomyParameterQuery;
        private EntityQuery m_ServiceFeeParameterQuery;
        private EntityQuery m_ProcessQuery;

        private CitySystem m_CitySystem;
        private GroundPollutionSystem m_GroundPollutionSystem;
        private NoisePollutionSystem m_NoisePollutionSystem;
        private AirPollutionSystem m_AirPollutionSystem;
        private TelecomCoverageSystem m_TelecomCoverageSystem;
        private TaxSystem m_TaxSystem;
        private ResourceSystem m_ResourceSystem;
        // Add pagination properties
        private int m_CurrentPage = 1;
        private int m_ItemsPerPage = 15;
        private int m_TotalItems = 0;
        protected override void OnCreate()
        {
            base.OnCreate();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_ImageSystem = World.GetOrCreateSystemManaged<ImageSystem>();
            m_NameSystem = World.GetOrCreateSystemManaged<NameSystem>();
            m_ResidentialDataInformation = Array.Empty<ResidentialDataInformation>();
            
            m_CitySystem = World.GetOrCreateSystemManaged<CitySystem>();
            m_GroundPollutionSystem = World.GetOrCreateSystemManaged<GroundPollutionSystem>();
            m_NoisePollutionSystem = World.GetOrCreateSystemManaged<NoisePollutionSystem>();
            m_AirPollutionSystem = World.GetOrCreateSystemManaged<AirPollutionSystem>();
            m_TelecomCoverageSystem = World.GetOrCreateSystemManaged<TelecomCoverageSystem>();
            m_TaxSystem = World.GetOrCreateSystemManaged<TaxSystem>();
            m_ResourceSystem = World.GetOrCreateSystemManaged<ResourceSystem>();

            // Create query for residential properties
            m_ResidentialQuery = SystemAPI.QueryBuilder()
                .WithAll<ResidentialProperty>()
                .Build();
                
            // Create query for households with HouseholdData
            m_HouseholdRenterQuery = SystemAPI.QueryBuilder()
                .WithAll<Game.Citizens.Household>()
                .Build();
            m_CitizenHappinessParameterQuery = SystemAPI.QueryBuilder()
                .WithAll<CitizenHappinessParameterData>()
                .Build();

            m_HealthcareParameterQuery = SystemAPI.QueryBuilder()
                .WithAll<HealthcareParameterData>()
                .Build();

            m_ParkParameterQuery = SystemAPI.QueryBuilder()
                .WithAll<ParkParameterData>()
                .Build();

            m_EducationParameterQuery = SystemAPI.QueryBuilder()
                .WithAll<EducationParameterData>()
                .Build();

            m_TelecomParameterQuery = SystemAPI.QueryBuilder()
                .WithAll<TelecomParameterData>()
                .Build();

            m_HappinessFactorParameterQuery = SystemAPI.QueryBuilder()
                .WithAll<HappinessFactorParameterData>()
                .Build();

            m_GarbageParameterQuery = SystemAPI.QueryBuilder()
                .WithAll<GarbageParameterData>()
                .Build();

            m_EconomyParameterQuery = SystemAPI.QueryBuilder()
                .WithAll<EconomyParameterData>()
                .Build();

            m_ServiceFeeParameterQuery = SystemAPI.QueryBuilder()
                .WithAll<ServiceFeeParameterData>()
                .Build();

            m_ProcessQuery = SystemAPI.QueryBuilder()
                .WithAll<IndustrialProcessData>()
                .Build();
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 512;
        }

        protected override void OnUpdate()
        {
            if (!IsPanelVisible)
                return;
                
            UpdateResidentialDataInformation();
        }

        private void UpdateResidentialDataInformation()
    {
        // Get all entities with HouseholdData component
        var householdEntities = m_HouseholdRenterQuery.ToEntityArray(Allocator.TempJob);
        
        // Create native collections for valid entities and their household counts
        var validEntities = new NativeList<Entity>(Allocator.TempJob);
        var householdCounts = new NativeList<int>(Allocator.TempJob);

        try
        {
            // First pass to identify all valid residential entities with households
            var householdSet = new NativeHashSet<Entity>(householdEntities.Length, Allocator.TempJob);
            for (int i = 0; i < householdEntities.Length; i++)
            {
                householdSet.Add(householdEntities[i]);
            }

            // Collect all valid residential entities to determine total count
            var collectJob = new CollectResidentialEntitiesJob
            {
                EntityTypeHandle = EntityManager.GetEntityTypeHandle(),
                RenterBufferHandle = EntityManager.GetBufferTypeHandle<Game.Buildings.Renter>(true),
                HouseholdSet = householdSet.AsReadOnly(),
                ValidEntities = validEntities,
                HouseholdCounts = householdCounts
            };

            var collectHandle = collectJob.Schedule(m_ResidentialQuery, Dependency);
            collectHandle.Complete();

            // Store total count for pagination
            m_TotalItems = validEntities.Length;

            // Calculate pagination range
            int startIndex = (m_CurrentPage - 1) * m_ItemsPerPage;
            int endIndex = math.min(startIndex + m_ItemsPerPage, m_TotalItems);

            // Only process entities for the current page
            var residentialDataList = new List<ResidentialDataInformation>(endIndex - startIndex);

            for (int i = startIndex; i < endIndex; i++)
            {
                if (i < validEntities.Length)
                {
                    var entity = validEntities[i];
                    int householdCount = householdCounts[i];

                    if (TryResidenceDTO(entity, householdCount, out var residentialData))
                    {
                        residentialDataList.Add(residentialData);
                    }
                }
            }

            m_ResidentialDataInformation = residentialDataList.ToArray();
            householdSet.Dispose();
        }
        finally
        {
            householdEntities.Dispose();
            validEntities.Dispose();
            householdCounts.Dispose();
        }
    }

        private bool TryResidenceDTO(Entity entity, int householdCount, out ResidentialDataInformation residentialData)
        {
            // Get prefab reference
            var prefabRef = EntityManager.GetComponentData<PrefabRef>(entity);
            Entity prefab = prefabRef.m_Prefab;

            // Get building name
            string residentialName = "Unknown";
            if (BuildingUtils.GetAddress(EntityManager, entity, out var road, out var number))
            {
                residentialName = m_NameSystem.GetRenderedLabelName(road) + " " + number;
            }

            // Get icon
            string icon = m_ImageSystem.GetInstanceIcon(entity);
            if (string.IsNullOrEmpty(icon) && prefab != Entity.Null)
            {
                icon = m_ImageSystem.GetIconOrGroupIcon(prefab);
            }

            var maxHouseholdCount = EntityManager.GetComponentData<BuildingPropertyData>(prefab).m_ResidentialProperties;
            
            // Get building happiness factors
            var happinessFactors = GetBuildingHappinessFactors(entity, out var citizenHappiness);
            
            residentialData = new ResidentialDataInformation()
            {
                ResidentialEntity = entity,
                ResidentialName = residentialName,
                ResidentialIcon = icon,
                CurrentHouseholdCount = householdCount,
                MaxHouseholdCount = maxHouseholdCount,
                HappinessFactors = happinessFactors,
                OverallHappiness = citizenHappiness
            };

            return true;
        }
        
        private BuildingHappinessFactorValue[] GetBuildingHappinessFactors(Entity entity, out Game.UI.InGame.CitizenHappiness happiness)
        {
            // Create a native array to store happiness factors
            int factorCount = System.Enum.GetValues(typeof(BuildingHappinessFactor)).Length;
            NativeArray<int2> factorsArray = new NativeArray<int2>(factorCount, Allocator.Temp);
            NativeArray<int> results = new NativeArray<int>(3, Allocator.Temp);
            happiness = default; // Initialize default value

            try
            {
                // Create the CountHappinessJob
                var job = new CountHappinessJob
                {
                    m_SelectedEntity = entity,
                    m_BuildingFromEntity = SystemAPI.GetComponentLookup<Building>(true),
                    m_ResidentialPropertyFromEntity = SystemAPI.GetComponentLookup<ResidentialProperty>(true),
                    m_HouseholdFromEntity = SystemAPI.GetComponentLookup<Household>(true),
                    m_CitizenFromEntity = SystemAPI.GetComponentLookup<Citizen>(true),
                    m_HealthProblemFromEntity = SystemAPI.GetComponentLookup<HealthProblem>(true),
                    m_PropertyRenterFromEntity = SystemAPI.GetComponentLookup<PropertyRenter>(true),
                    m_AbandonedFromEntity = SystemAPI.GetComponentLookup<Abandoned>(true),
                    m_HouseholdCitizenFromEntity = SystemAPI.GetBufferLookup<HouseholdCitizen>(true),
                    m_RenterFromEntity = SystemAPI.GetBufferLookup<Renter>(true),
                    m_PrefabRefFromEntity = SystemAPI.GetComponentLookup<PrefabRef>(true),
                    m_SpawnableBuildingDataFromEntity = SystemAPI.GetComponentLookup<SpawnableBuildingData>(true),
                    m_BuildingPropertyDataFromEntity = SystemAPI.GetComponentLookup<BuildingPropertyData>(true),
                    m_ElectricityConsumerFromEntity = SystemAPI.GetComponentLookup<ElectricityConsumer>(true),
                    m_WaterConsumerFromEntity = SystemAPI.GetComponentLookup<WaterConsumer>(true),
                    m_LockedFromEntity = SystemAPI.GetComponentLookup<Locked>(true),
                    m_TransformFromEntity = SystemAPI.GetComponentLookup<Transform>(true),
                    m_GarbageProducersFromEntity = SystemAPI.GetComponentLookup<GarbageProducer>(true),
                    m_CrimeProducersFromEntity = SystemAPI.GetComponentLookup<CrimeProducer>(true),
                    m_MailProducerFromEntity = SystemAPI.GetComponentLookup<MailProducer>(true),
                    m_BuildingDataFromEntity = SystemAPI.GetComponentLookup<BuildingData>(true),
                    m_CityModifierFromEntity = SystemAPI.GetBufferLookup<CityModifier>(true),
                    m_ServiceCoverageFromEntity = SystemAPI.GetBufferLookup<Game.Net.ServiceCoverage>(true),

                    // Get parameter data from queries
                    m_CitizenHappinessParameters = m_CitizenHappinessParameterQuery.GetSingleton<CitizenHappinessParameterData>(),
                    m_GarbageParameters = m_GarbageParameterQuery.GetSingleton<GarbageParameterData>(),
                    m_HealthcareParameters = m_HealthcareParameterQuery.GetSingleton<HealthcareParameterData>(),
                    m_ParkParameters = m_ParkParameterQuery.GetSingleton<ParkParameterData>(),
                    m_EducationParameters = m_EducationParameterQuery.GetSingleton<EducationParameterData>(),
                    m_TelecomParameters = m_TelecomParameterQuery.GetSingleton<TelecomParameterData>(),

                    // Get happiness factor parameters
                    m_HappinessFactorParameters = SystemAPI.GetBufferLookup<HappinessFactorParameterData>(false)[m_HappinessFactorParameterQuery.GetSingletonEntity()],

                    // Get pollution and coverage data
                    m_PollutionMap = m_GroundPollutionSystem.GetData(true, out var pollutionDep).m_Buffer,
                    m_NoisePollutionMap = m_NoisePollutionSystem.GetData(true, out var noiseDep).m_Buffer,
                    m_AirPollutionMap = m_AirPollutionSystem.GetData(true, out var airDep).m_Buffer,
                    m_TelecomCoverage = m_TelecomCoverageSystem.GetData(true, out var telecomDep),

                    // Get tax rates and city
                    m_TaxRates = m_TaxSystem.GetTaxRates(),
                    m_City = m_CitySystem.City,

                    // Set output arrays
                    m_Factors = factorsArray,
                    m_Results = results,

                    // Service fees
                    m_RelativeElectricityFee = ServiceFeeSystem.GetFee(PlayerResource.Electricity,
                        EntityManager.GetBuffer<ServiceFee>(m_CitySystem.City)) /
                        m_ServiceFeeParameterQuery.GetSingleton<ServiceFeeParameterData>().m_ElectricityFee.m_Default,
                    m_RelativeWaterFee = ServiceFeeSystem.GetFee(PlayerResource.Water,
                        EntityManager.GetBuffer<ServiceFee>(m_CitySystem.City)) /
                        m_ServiceFeeParameterQuery.GetSingleton<ServiceFeeParameterData>().m_WaterFee.m_Default
                };

                // Run the job immediately
                job.Execute();

                // Process citizen count and happiness from results
                int num = results[1];
                int num2 = results[2];
                
                // Calculate average happiness similar to CitizenUIUtils.GetCitizenHappiness
                // Calculate average happiness
                int averageRawHappiness = 0;
                if (results[0] > 0) // Valid results available
                {
                    averageRawHappiness = num2 / math.select(num, 1, num == 0);
                    // Convert to CitizenHappiness object
                    happiness = CitizenUIUtils.GetCitizenHappiness(averageRawHappiness);
                }

                // Convert factors to BuildingHappinessFactorValue array using proper normalization
                var happinessList = new List<BuildingHappinessFactorValue>();
                
                // Process factors similar to the game's approach
                for (int i = 0; i < factorsArray.Length; i++)
                {
                    int x = factorsArray[i].x;
                    if (x > 0)
                    {
                        // Calculate the normalized factor value
                        int normalizedValue = (int)math.round((float)factorsArray[i].y / (float)x);
                        if (normalizedValue != 0)
                        {
                            var factorValue = new BuildingHappinessFactorValue
                            {
                                Factor = (BuildingHappinessFactor)i,
                                Value = normalizedValue
                            };
                            happinessList.Add(factorValue);
                        }
                    }
                }

                happinessList.Sort();
                return happinessList.ToArray();
            }
            finally
            {
                if (factorsArray.IsCreated)
                    factorsArray.Dispose();
                if (results.IsCreated)
                    results.Dispose();
            }
            
        }
        public ResidentialDataInformation[] FilterHouseholdsByRoad(string roadName)
    {
        if (string.IsNullOrEmpty(roadName) || m_ResidentialDataInformation == null)
        {
            return m_ResidentialDataInformation;
        }

        // First collect all matching entities to get total count
        var allMatching = new List<Entity>();
        var allHouseholdCounts = new List<int>();

        // Query all residential entities with the specified road
        var householdEntities = m_HouseholdRenterQuery.ToEntityArray(Allocator.TempJob);
        var validEntities = new NativeList<Entity>(Allocator.TempJob);
        var householdCounts = new NativeList<int>(Allocator.TempJob);

        try
        {
            // First build a collection of all households
            var householdSet = new NativeHashSet<Entity>(householdEntities.Length, Allocator.TempJob);
            for (int i = 0; i < householdEntities.Length; i++)
            {
                householdSet.Add(householdEntities[i]);
            }

            // Collect all valid residential entities
            var collectJob = new CollectResidentialEntitiesJob
            {
                EntityTypeHandle = EntityManager.GetEntityTypeHandle(),
                RenterBufferHandle = EntityManager.GetBufferTypeHandle<Game.Buildings.Renter>(true),
                HouseholdSet = householdSet.AsReadOnly(),
                ValidEntities = validEntities,
                HouseholdCounts = householdCounts
            };

            var collectHandle = collectJob.Schedule(m_ResidentialQuery, Dependency);
            collectHandle.Complete();

            // Filter by road name
            for (int i = 0; i < validEntities.Length; i++)
            {
                Entity entity = validEntities[i];
                
                if (BuildingUtils.GetAddress(EntityManager, entity, out var road, out var _))
                {
                    string entityRoadName = m_NameSystem.GetRenderedLabelName(road);
                    
                    if (entityRoadName.IndexOf(roadName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        allMatching.Add(entity);
                        allHouseholdCounts.Add(householdCounts[i]);
                    }
                }
            }

            // Update total count for pagination
            m_TotalItems = allMatching.Count;

            // Calculate pagination range
            int startIndex = (m_CurrentPage - 1) * m_ItemsPerPage;
            int endIndex = math.min(startIndex + m_ItemsPerPage, m_TotalItems);

            // Second pass: only process items for current page
            var filteredList = new List<ResidentialDataInformation>(endIndex - startIndex);

            for (int i = startIndex; i < endIndex; i++)
            {
                if (i < allMatching.Count)
                {
                    var entity = allMatching[i];
                    int householdCount = allHouseholdCounts[i];

                    if (TryResidenceDTO(entity, householdCount, out var residentialData))
                    {
                        filteredList.Add(residentialData);
                    }
                }
            }

            return filteredList.ToArray();
        }
        finally
        {
            householdEntities.Dispose();
            validEntities.Dispose();
            householdCounts.Dispose();
        }
    }
        public string[] GetAllRoadNames()
        {
            if (m_ResidentialDataInformation == null || m_ResidentialDataInformation.Length == 0)
            {
                return Array.Empty<string>();
            }

            // Use HashSet to automatically handle duplicates
            var roadNames = new HashSet<string>();

            foreach (var residence in m_ResidentialDataInformation)
            {
                Entity entity = residence.ResidentialEntity;

                // Get the road name from the entity's address
                if (BuildingUtils.GetAddress(EntityManager, entity, out var road, out var _))
                {
                    string roadName = m_NameSystem.GetRenderedLabelName(road);
                    
                    // Only add non-empty road names
                    if (!string.IsNullOrEmpty(roadName))
                    {
                        roadNames.Add(roadName);
                    }
                }
            }

            // Convert to array and sort alphabetically
            string[] result = roadNames.ToArray();
            Array.Sort(result, StringComparer.OrdinalIgnoreCase);
            
            return result;
        }
        public void SetPagination(int currentPage, int itemsPerPage)
        {
            m_CurrentPage = math.max(1, currentPage);
            m_ItemsPerPage = math.max(1, itemsPerPage);
        }
        public int GetTotalCount()
        {
            return m_TotalItems;
        }
    }
}