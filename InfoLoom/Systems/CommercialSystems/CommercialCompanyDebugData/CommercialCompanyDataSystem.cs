using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Entities;
using Colossal.Logging;
using Game;
using Game.Buildings;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Game.UI;
using Game.UI.InGame;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using DeliveryTruck = Game.Vehicles.DeliveryTruck;

namespace InfoLoomTwo.Systems.CommercialSystems.CommercialCompanyDebugData
{
    

    public struct CommercialStats
    {
        public Entity companyEntity;
        public string CompanyName;
        public int ServiceAvailable;
        public int MaxService;
        public int TotalEmployees;
        public int MaxWorkers;
        public int VehicleCount;
        public int VehicleCapacity;
        public string Resources;
        public int ResourceAmount;
        public int Efficiency;
        public List<EfficiencyFactorInfo> EfficiencyFactors;
    }

    public struct EfficiencyData
    {
        public int TotalEfficiency;
        public List<EfficiencyFactorInfo> Factors;
    }
    public struct EfficiencyFactorInfo
    {
        private Game.Buildings.EfficiencyFactor factor;
        private int value;
        private int result;

        public EfficiencyFactorInfo(Game.Buildings.EfficiencyFactor factor, int value, int result)
        {
            this.factor = factor;
            this.value = value;
            this.result = result;
        }

        public Game.Buildings.EfficiencyFactor Factor => factor;
        public int Value => value;
        public int Result => result;
    }

    // Company DTO for UI consumption
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
        public List<EfficiencyFactorInfo> Factors;
    }

    // Modified CommercialData to use array
    public struct CommercialData
    {
        public CommercialCompanyDTO[] Companies;
    }

    public partial class CommercialCompanyDataSystem : GameSystemBase
    {
        private NameSystem m_NameSystem;
        private EntityQuery m_CommercialCompanyQuery;
        private EntityQuery m_CommercialCompanyDataQuery;
        private EntityQuery m_EfficiencyQuery;
        private ILog m_Log;
        
        // Internal tracking dictionaries (keep these for easier data manipulation)
        private Dictionary<Entity, CommercialStats> m_StatsDict;
        private Dictionary<Entity, EfficiencyData> m_EfficiencyDict;
        
        // Public array-based data structure for UI
        public CommercialData m_CommercialData;
        public bool IsPanelVisible;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = Mod.log;
            m_StatsDict = new Dictionary<Entity, CommercialStats>();
            m_EfficiencyDict = new Dictionary<Entity, EfficiencyData>();
            m_CommercialData = new CommercialData { Companies = Array.Empty<CommercialCompanyDTO>() };
            m_NameSystem = World.GetOrCreateSystemManaged<NameSystem>();

            // Original query setup remains the same
            m_CommercialCompanyQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<CommercialCompany>(),
                    ComponentType.ReadOnly<ServiceAvailable>(),
                    ComponentType.ReadOnly<Game.Companies.CompanyData>(),
                    ComponentType.ReadOnly<WorkProvider>(),
                    ComponentType.ReadOnly<Employee>(),
                    ComponentType.ReadOnly<OwnedVehicle>(),
                    ComponentType.ReadOnly<Resources>(),
                }
            });

            m_CommercialCompanyDataQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<CommercialCompanyData>(),
                    ComponentType.ReadOnly<ServiceCompanyData>(),
                    ComponentType.ReadOnly<TransportCompanyData>(),
                    ComponentType.ReadOnly<IndustrialProcessData>(),
                }
            });

            m_EfficiencyQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<CommercialProperty>(),
                    ComponentType.ReadOnly<Efficiency>(),
                }
            });
        }

        // The rest of the helper methods remain unchanged
        private int CountActiveVehicles(Entity entity)
        {
            // Original implementation
            var activeVehicles = 0;
            if (EntityManager.TryGetBuffer<OwnedVehicle>(entity, true, out var vehicleBuffer))
            {
                for (int i = 0; i < vehicleBuffer.Length; i++)
                {
                    var vehicle = vehicleBuffer[i].m_Vehicle;
                    if (EntityManager.HasComponent<DeliveryTruck>(vehicle))
                    {
                        activeVehicles++;
                    }
                }
            }
            return activeVehicles;
        }

        private EfficiencyData CalculateEfficiency(Entity entity)
        {
            // Original implementation
            var factors = new List<EfficiencyFactorInfo>();

            if (!EntityManager.HasComponent<Efficiency>(entity))
                return new EfficiencyData { TotalEfficiency = 100, Factors = factors };

            var buffer = EntityManager.GetBuffer<Efficiency>(entity, true);
            using var array = buffer.ToNativeArray(Allocator.Temp);
            array.Sort();

            if (array.Length == 0)
                return new EfficiencyData { TotalEfficiency = 100, Factors = factors };

            int totalEfficiency = (int)math.round(100f * BuildingUtils.GetEfficiency(buffer));

            if (totalEfficiency > 0)
            {
                float cumulativeEffect = 100f;

                foreach (var item in array)
                {
                    float efficiency = math.max(0f, item.m_Efficiency);
                    cumulativeEffect *= efficiency;

                    int percentageChange = math.max(-99, (int)math.round(100f * efficiency) - 100);
                    int cumulativeResult = math.max(1, (int)math.round(cumulativeEffect));

                    if (percentageChange != 0)
                    {
                        factors.Add(new EfficiencyFactorInfo(item.m_Factor, percentageChange, cumulativeResult));
                    }
                }
            }
            else
            {
                foreach (var item in array)
                {
                    if (math.max(0f, item.m_Efficiency) == 0f)
                    {
                        factors.Add(new EfficiencyFactorInfo(item.m_Factor, -100, -100));
                        if ((int)item.m_Factor <= 3)
                        {
                            break;
                        }
                    }
                }
            }

            return new EfficiencyData
            {
                TotalEfficiency = totalEfficiency,
                Factors = factors
            };
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 512;
        }

        
        protected override void OnUpdate()
        {
            if (!IsPanelVisible)
                return;
                
            m_StatsDict.Clear();
            m_EfficiencyDict.Clear();
            
            // Collect data into dictionaries first (original logic)
            UpdateCommercialStats();
            
            // Then convert to array format for UI consumption
            ConvertToArrayFormat();
        }

        private void UpdateCommercialStats()
        {
            // Original implementation with minor changes to use local dictionaries
            using var entities = m_CommercialCompanyQuery.ToEntityArray(Allocator.Temp);
            using var serviceAvailables = m_CommercialCompanyQuery.ToComponentDataArray<ServiceAvailable>(Allocator.Temp);
            using var companyDatas = m_CommercialCompanyQuery.ToComponentDataArray<Game.Companies.CompanyData>(Allocator.Temp);
            using var workProviders = m_CommercialCompanyQuery.ToComponentDataArray<WorkProvider>(Allocator.Temp);
            using var maxServices = m_CommercialCompanyDataQuery.ToComponentDataArray<ServiceCompanyData>(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                var serviceAvailable = serviceAvailables[i];
                var maxServiceData = maxServices[i];
                var companyData = companyDatas[i];
                var workProvider = workProviders[i];

                string brandName = m_NameSystem.GetRenderedLabelName(companyData.m_Brand);

                var employeeCount = EntityManager.TryGetBuffer<Employee>(entity, true, out var employeeBuffer)
                    ? employeeBuffer.Length
                    : 0;

                var maxDeliveryTrucks = 0;
                if (EntityManager.TryGetComponent<PrefabRef>(entity, out var prefabRef) &&
                    EntityManager.HasComponent<CommercialCompanyData>(prefabRef.m_Prefab))
                {
                    var commercialData = EntityManager.GetComponentData<TransportCompanyData>(prefabRef.m_Prefab);
                    maxDeliveryTrucks = commercialData.m_MaxTransports;
                }

                var activeVehicles = EntityManager.HasComponent<OwnedVehicle>(entity)
                    ? CountActiveVehicles(entity)
                    : 0;

                EntityManager.TryGetBuffer<Resources>(entity, true, out var resourceBuffer);
                string resourceType = resourceBuffer.Length > 0 ? resourceBuffer[0].m_Resource.ToString() : "None";
                int resourceAmount = resourceBuffer.Length > 0 ? resourceBuffer[0].m_Amount : 0;

                var efficiencyData = CalculateEfficiency(entity);
                m_EfficiencyDict[entity] = efficiencyData;

                m_StatsDict[entity] = new CommercialStats
                {
                    companyEntity = entity,
                    CompanyName = brandName,
                    ServiceAvailable = serviceAvailable.m_ServiceAvailable,
                    MaxService = maxServiceData.m_MaxService,
                    TotalEmployees = employeeCount,
                    MaxWorkers = workProvider.m_MaxWorkers,
                    VehicleCount = activeVehicles,
                    VehicleCapacity = maxDeliveryTrucks,
                    Resources = resourceType,
                    ResourceAmount = resourceAmount,
                    Efficiency = efficiencyData.TotalEfficiency,
                    EfficiencyFactors = efficiencyData.Factors
                };
            }
        }

        private void ConvertToArrayFormat()
        {
            // Convert dictionary data to array for UI
            var companies = new List<CommercialCompanyDTO>();

            // Get keys as an array for indexed access
            Entity[] entities = m_StatsDict.Keys.ToArray();
            
            // Use standard for loop with index
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                var stats = m_StatsDict[entity];
                var effData = m_EfficiencyDict[entity];

                companies.Add(new CommercialCompanyDTO
                {
                    EntityId = entity,
                    CompanyName = stats.CompanyName,
                    ServiceAvailable = stats.ServiceAvailable,
                    MaxService = stats.MaxService,
                    TotalEmployees = stats.TotalEmployees,
                    MaxWorkers = stats.MaxWorkers,
                    VehicleCount = stats.VehicleCount,
                    VehicleCapacity = stats.VehicleCapacity,
                    Resources = stats.Resources,
                    ResourceAmount = stats.ResourceAmount,
                    TotalEfficiency = effData.TotalEfficiency,
                    Factors = effData.Factors
                });
            }

            // Update the public data structure
            m_CommercialData = new CommercialData { Companies = companies.ToArray() };
        }
    }
}