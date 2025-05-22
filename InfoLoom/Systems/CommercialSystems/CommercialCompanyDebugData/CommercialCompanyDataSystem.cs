using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Entities;
using Colossal.Logging;
using Colossal.UI.Binding;
using Game;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Objects;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Simulation;
using Game.UI;
using Game.UI.InGame;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using DeliveryTruck = Game.Vehicles.DeliveryTruck;

namespace InfoLoomTwo.Systems.CommercialSystems.CommercialCompanyDebugData
{
    public struct CommercialCompanyDTO
    {
        public Entity EntityId;
        public string CompanyName;
        public int ServiceAvailable;
        public int MaxService;
        public int TotalEmployees;
        public int MaxWorkers;
        public int VehicleCount;
        public int VehicleCapacity;
        public string Resources;
        public int ResourceAmount;
        public int TotalEfficiency;
        public EfficiencyFactorInfo[] Factors;
        public float Profitability;
        public int LastTotalWorth;
        public int TotalWages;
        public int ProductionPerDay;
        public float EfficiencyValue;
        public float Concentration;
        public string OutputResourceName;
    }

    public struct EfficiencyFactorInfo
    {
        public Game.Buildings.EfficiencyFactor Factor;
        public int Value;
        public int Result;

        public EfficiencyFactorInfo(Game.Buildings.EfficiencyFactor factor, int value, int result)
        {
            Factor = factor;
            Value = value;
            Result = result;
        }
    }

    public struct ProcessResourceInfo
    {
        public string ResourceName;
        public int Amount;
        public bool IsOutput;
    }

    public partial class CommercialCompanyDataSystem : GameSystemBase
    {
        private NameSystem m_NameSystem;
        private ImageSystem m_ImageSystem;
        private ResourceSystem m_ResourceSystem;
        private PrefabSystem m_PrefabSystem;
        private EntityQuery m_CommercialCompanyQuery;
        private ILog m_Log;
        public bool IsPanelVisible;
        public CommercialCompanyDTO[] m_CommercialCompanyDTOs;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = Mod.log;
            m_CommercialCompanyDTOs = Array.Empty<CommercialCompanyDTO>();
            m_NameSystem = World.GetOrCreateSystemManaged<NameSystem>();
            m_ImageSystem = World.GetOrCreateSystemManaged<ImageSystem>();
            m_ResourceSystem = World.GetOrCreateSystemManaged<ResourceSystem>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();

            // Define query for commercial companies
            m_CommercialCompanyQuery = SystemAPI.QueryBuilder()
                .WithAll<CommercialCompany, ServiceAvailable, Game.Companies.CompanyData, WorkProvider, Employee>()
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

            UpdateCommercialStats();
        }

        private void UpdateCommercialStats()
        {
            if (!IsPanelVisible)
                return;

            var entities = m_CommercialCompanyQuery.ToEntityArray(Allocator.Temp);
            var companies = new List<CommercialCompanyDTO>(entities.Length);
            
            // Create component lookups
            var companyDataLookup = GetComponentLookup<Game.Companies.CompanyData>(true);
            var workProviderLookup = GetComponentLookup<WorkProvider>(true);
            var prefabRefLookup = GetComponentLookup<PrefabRef>(true);
            var serviceAvailableLookup = GetComponentLookup<ServiceAvailable>(true);
            var serviceCompanyDataLookup = GetComponentLookup<ServiceCompanyData>(true);
            var transportCompanyDataLookup = GetComponentLookup<TransportCompanyData>(true);
            var profitabilityLookup = GetComponentLookup<Profitability>(true);
            var propertyRenterLookup = GetComponentLookup<PropertyRenter>(true);
            var industrialProcessDataLookup = GetComponentLookup<IndustrialProcessData>(true);
            
            // Create buffer lookups
            var employeeBufferLookup = GetBufferLookup<Employee>(true);
            var ownedVehicleBufferLookup = GetBufferLookup<OwnedVehicle>(true);
            var resourcesBufferLookup = GetBufferLookup<Resources>(true);
            var efficiencyBufferLookup = GetBufferLookup<Efficiency>(true);
            
            // Get economy parameters
            var econParams = SystemAPI.GetSingleton<EconomyParameterData>();
            var resourcePrefabs = m_ResourceSystem.GetPrefabs();
            var resourceDataLookup = GetComponentLookup<ResourceData>(true);
            var citizenLookup = GetComponentLookup<Citizen>(true);
            var deliveryTruckLookup = GetComponentLookup<DeliveryTruck>(true);

            try
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];

                    // Skip companies with no employees
                    if (!employeeBufferLookup.HasBuffer(entity) || employeeBufferLookup[entity].Length == 0)
                        continue;

                    // Get buffers
                    var employeeBuffer = employeeBufferLookup[entity];
                    
                    // Get essential components
                    var companyData = companyDataLookup[entity];
                    var workProvider = workProviderLookup[entity];
                    var prefabRef = prefabRefLookup[entity];
                    var serviceAvailable = serviceAvailableLookup[entity];
                    Entity prefab = prefabRef.m_Prefab;

                    // Get company name
                    string companyName = m_NameSystem.GetRenderedLabelName(companyData.m_Brand);

                    // Initialize variables
                    int serviceValue = serviceAvailable.m_ServiceAvailable;
                    int maxService = 0;
                    int activeVehicles = 0, maxDeliveryTrucks = 0;
                    string resourceType = "None";
                    int resourceAmount = 0;
                    int efficiency = 100;
                    float profitabilityValue = 0f;
                    int lastTotalWorth = 0;
                    int totalWages = 0;
                    int productionPerDay = 0;
                    float efficiencyValue = 1f;
                    float concentration = 0f;
                    string outputResourceName = "";
                    
                    // Get max service data
                    if (prefab != Entity.Null && serviceCompanyDataLookup.HasComponent(prefab))
                        maxService = serviceCompanyDataLookup[prefab].m_MaxService;

                    // Get vehicle data
                    if (ownedVehicleBufferLookup.HasBuffer(entity))
                    {
                        var vehicleBuffer = ownedVehicleBufferLookup[entity];
                        for (int v = 0; v < vehicleBuffer.Length; v++)
                        {
                            if (deliveryTruckLookup.HasComponent(vehicleBuffer[v].m_Vehicle))
                                activeVehicles++;
                        }
                    }

                    // Get max trucks
                    if (prefab != Entity.Null && transportCompanyDataLookup.HasComponent(prefab))
                        maxDeliveryTrucks = transportCompanyDataLookup[prefab].m_MaxTransports;

                    // Get resources
                    if (resourcesBufferLookup.HasBuffer(entity) && resourcesBufferLookup[entity].Length > 0)
                    {
                        var resourceBuffer = resourcesBufferLookup[entity];
                        resourceType = EconomyUtils.GetName(resourceBuffer[0].m_Resource);
                        resourceAmount = resourceBuffer[0].m_Amount;
                        outputResourceName = GetFormattedResourceName(resourceBuffer[0].m_Resource);
                    }

                    // Get profitability
                    if (profitabilityLookup.HasComponent(entity))
                    {
                        var profitability = profitabilityLookup[entity];
                        profitabilityValue = ((profitability.m_Profitability - 127f) / 127.5f) * 100f;
                        lastTotalWorth = profitability.m_LastTotalWorth;
                    }

                    // Calculate wages
                    totalWages = EconomyUtils.CalculateTotalWage(employeeBuffer, ref econParams);

                    // Get efficiency data
                    EfficiencyFactorInfo[] factors = Array.Empty<EfficiencyFactorInfo>();
                    Entity targetEntity = entity;
                    
                    if (propertyRenterLookup.HasComponent(entity))
                    {
                        targetEntity = propertyRenterLookup[entity].m_Property;
                        
                        // Calculate efficiency
                        if (efficiencyBufferLookup.HasBuffer(targetEntity))
                        {
                            var efficiencyBuffer = efficiencyBufferLookup[targetEntity];
                            efficiencyValue = BuildingUtils.GetEfficiency(efficiencyBuffer);
                            efficiency = (int)math.round(100f * efficiencyValue);
                            factors = GetEfficiencyFactors(targetEntity, efficiencyBufferLookup);
                        }
                    }

                    // Calculate production per day
                    if (industrialProcessDataLookup.HasComponent(prefab))
                    {
                        var industryProcess = industrialProcessDataLookup[prefab];
                        productionPerDay = EconomyUtils.GetCompanyProductionPerDay(
                            efficiencyValue,
                            false,
                            employeeBuffer,
                            industryProcess,
                            resourcePrefabs,
                            resourceDataLookup,
                            citizenLookup,
                            ref econParams);
                    }

                    // Create DTO
                    var companyDTO = new CommercialCompanyDTO
                    {
                        EntityId = entity,
                        CompanyName = companyName,
                        ServiceAvailable = serviceValue,
                        MaxService = maxService,
                        TotalEmployees = employeeBuffer.Length,
                        MaxWorkers = workProvider.m_MaxWorkers,
                        VehicleCount = activeVehicles,
                        VehicleCapacity = maxDeliveryTrucks,
                        Resources = resourceType,
                        ResourceAmount = resourceAmount,
                        TotalEfficiency = efficiency,
                        Factors = factors,
                        Profitability = profitabilityValue,
                        LastTotalWorth = lastTotalWorth,
                        TotalWages = totalWages,
                        ProductionPerDay = productionPerDay,
                        EfficiencyValue = efficiencyValue * 100f,
                        Concentration = concentration,
                        OutputResourceName = outputResourceName
                    };
                    
                    companies.Add(companyDTO);
                }
            }
            finally
            {
                entities.Dispose();
            }

            m_CommercialCompanyDTOs = companies.ToArray();
        }

        

        // Helper methods for string processing
        private string GetFormattedResourceName(Resource resource)
        {
            if (resource == Resource.NoResource)
                return string.Empty;
                
            string resourceName = EconomyUtils.GetName(resource);
            if (string.IsNullOrEmpty(resourceName))
                return string.Empty;
                
            // Capitalize first letter
            return char.ToUpper(resourceName[0]) + resourceName.Substring(1);
        }
        
        private EfficiencyFactorInfo[] GetEfficiencyFactors(Entity entity, BufferLookup<Efficiency> efficiencyLookup)
        {
            if (!efficiencyLookup.HasBuffer(entity))
                return Array.Empty<EfficiencyFactorInfo>();
                
            var buffer = efficiencyLookup[entity];
            if (buffer.Length == 0)
                return Array.Empty<EfficiencyFactorInfo>();
                
            using var sortedEfficiencies = buffer.ToNativeArray(Allocator.Temp);
            sortedEfficiencies.Sort();
            
            var tempFactors = new List<EfficiencyFactorInfo>();
            var totalEfficiency = (int)math.round(100f * BuildingUtils.GetEfficiency(buffer));
            
            if (totalEfficiency > 0)
            {
                float cumulativeEffect = 100f;
                for (int i = 0; i < sortedEfficiencies.Length; i++)
                {
                    var item = sortedEfficiencies[i];
                    float efficiency = math.max(0f, item.m_Efficiency);
                    cumulativeEffect *= efficiency;
                    
                    int percentageChange = math.max(-99, (int)math.round(100f * efficiency) - 100);
                    int result = math.max(1, (int)math.round(cumulativeEffect));
                    
                    if (percentageChange != 0)
                    {
                        tempFactors.Add(new EfficiencyFactorInfo(item.m_Factor, percentageChange, result));
                    }
                }
            }
            else
            {
                for (int i = 0; i < sortedEfficiencies.Length; i++)
                {
                    var item = sortedEfficiencies[i];
                    if (math.max(0f, item.m_Efficiency) == 0f)
                    {
                        tempFactors.Add(new EfficiencyFactorInfo(item.m_Factor, -100, -100));
                        if ((int)item.m_Factor <= 3)
                        {
                            break;
                        }
                    }
                }
            }
            
            return tempFactors.ToArray();
        }
    }
}
