using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Entities;
using Colossal.Logging;
using Colossal.UI.Binding;
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
       public readonly struct EfficiencyFactorInfo : IJsonWritable
    {
        public readonly Game.Buildings.EfficiencyFactor Factor;
        public readonly int Value;
        public readonly int Result;

        public EfficiencyFactorInfo(Game.Buildings.EfficiencyFactor factor, int value, int result)
        {
            Factor = factor;
            Value = value;
            Result = result;
        }

        public void Write(IJsonWriter writer)
        {
            writer.TypeBegin(typeof(EfficiencyFactorInfo).FullName);
            writer.PropertyName("factor");
            writer.Write(Enum.GetName(typeof(Game.Buildings.EfficiencyFactor), Factor));
            writer.PropertyName("value");
            writer.Write(Value);
            writer.PropertyName("result");
            writer.Write(Result);
            writer.TypeEnd();
        }
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
        public EfficiencyFactorInfo[] Factors;
    }

    // Modified CommercialData to use array
    public struct CommercialData
    {
        public CommercialCompanyDTO[] Companies;
    }

    public partial class CommercialCompanyDataSystem : GameSystemBase
    {
        private NameSystem m_NameSystem;
        private ImageSystem m_ImageSystem;
        private EntityQuery m_CommercialCompanyQuery;
        private EntityQuery m_CommercialCompanyDataQuery;
        private EntityQuery m_EfficiencyQuery;
        private ILog m_Log;
        public CommercialData m_CommercialData;
        public bool IsPanelVisible;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = Mod.log;
            m_CommercialData = new CommercialData { Companies = Array.Empty<CommercialCompanyDTO>() };
            m_NameSystem = World.GetOrCreateSystemManaged<NameSystem>();
            m_ImageSystem = World.GetOrCreateSystemManaged<ImageSystem>();

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

        private (int efficiency, EfficiencyFactorInfo[] factors) CalculateEfficiency(Entity entity)
        {
            Entity targetEntity = EntityManager.TryGetComponent<PropertyRenter>(entity, out var renter)
                ? renter.m_Property
                : entity;

            if (!EntityManager.HasComponent<Efficiency>(targetEntity))
            {
                return (100, Array.Empty<EfficiencyFactorInfo>());
            }

            var buffer = EntityManager.GetBuffer<Efficiency>(targetEntity, true);
            if (buffer.Length == 0)
            {
                return (100, Array.Empty<EfficiencyFactorInfo>());
            }

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
                for (int i =0; i < sortedEfficiencies.Length; i++)
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

            return (totalEfficiency, tempFactors.ToArray());
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
            var entities = m_CommercialCompanyQuery.ToEntityArray(Allocator.Temp);
            var companies = new List<CommercialCompanyDTO>(entities.Length);

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                var prefab = EntityManager.GetComponentData<PrefabRef>(entity).m_Prefab;

                var serviceAvailable = EntityManager.GetComponentData<ServiceAvailable>(entity);
                var serviceCompanyData = EntityManager.GetComponentData<ServiceCompanyData>(prefab);
                var companyData = EntityManager.GetComponentData<Game.Companies.CompanyData>(entity);
                var workProvider = EntityManager.GetComponentData<WorkProvider>(entity);

                var employeeCount = EntityManager.TryGetBuffer<Employee>(entity, true, out var employeeBuffer)
                    ? employeeBuffer.Length
                    : 0;

                var maxDeliveryTrucks = 0;
                if (EntityManager.HasComponent<CommercialCompanyData>(prefab))
                {
                    maxDeliveryTrucks = EntityManager
                        .GetComponentData<TransportCompanyData>(prefab)
                        .m_MaxTransports;
                }

                var activeVehicles = EntityManager.HasComponent<OwnedVehicle>(entity)
                    ? CountActiveVehicles(entity)
                    : 0;

                string resourceType = "None";
                int resourceAmount = 0;
                if (EntityManager.TryGetBuffer<Resources>(entity, true, out var resourceBuffer) &&
                    resourceBuffer.Length > 0)
                {
                    resourceType = resourceBuffer[0].m_Resource.ToString();
                    resourceAmount = resourceBuffer[0].m_Amount;
                }
                var (efficiency, factors) = CalculateEfficiency(entity);

                

                companies.Add(new CommercialCompanyDTO
                {
                    EntityId = entity,
                    CompanyName = m_NameSystem.GetRenderedLabelName(companyData.m_Brand),
                    ServiceAvailable = serviceAvailable.m_ServiceAvailable,
                    MaxService = serviceCompanyData.m_MaxService,
                    TotalEmployees = employeeCount,
                    MaxWorkers = workProvider.m_MaxWorkers,
                    VehicleCount = activeVehicles,
                    VehicleCapacity = maxDeliveryTrucks,
                    Resources = resourceType,
                    ResourceAmount = resourceAmount,
                    TotalEfficiency = efficiency,
                    Factors = factors,
                });
            }

            entities.Dispose();
            m_CommercialData = new CommercialData { Companies = companies.ToArray() };
        }
    }
}