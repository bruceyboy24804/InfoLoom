using System;
using System.Collections.Generic;
using Colossal.Entities;
using Colossal.Logging;
using Colossal.UI.Binding;
using Game;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.Companies;
using Game.Economy;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.UI;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using DeliveryTruck = Game.Vehicles.DeliveryTruck;
using ExtractorCompany = Game.Companies.ExtractorCompany;
using ProcessingCompany = Game.Companies.ProcessingCompany;
using StorageCompany = Game.Companies.StorageCompany;

namespace InfoLoomTwo.Systems.IndustrialSystems.IndustrialCompanyData
{
    public struct ProcessResourceInfo : IJsonWritable
    {
        public string ResourceName;
        public int Amount;
        public bool IsOutput;

        public void Write(IJsonWriter writer)
        {
            writer.TypeBegin(typeof(ProcessResourceInfo).FullName);
            writer.PropertyName("resourceName");
            writer.Write(ResourceName);
            writer.PropertyName("amount");
            writer.Write(Amount);
            writer.PropertyName("isOutput");
            writer.Write(IsOutput);
            writer.TypeEnd();
        }
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

    public struct IndustrialCompanyDTO
    {
        public Entity EntityId;
        public string CompanyName;
        public int TotalEmployees;
        public int MaxWorkers;
        public int VehicleCount;
        public int VehicleCapacity;
        public string Resources;
        public int ResourceAmount;
        public ProcessResourceInfo[] ProcessResources;
        public int TotalEfficiency;
        public EfficiencyFactorInfo[] Factors;
        // Added profitability fields
        public float Profitability;
        public int LastTotalWorth;
        public int TotalWages;
        public int ProductionPerDay;
        public float EfficiencyValue;
        public float Concentration;
        public string OutputResourceName;
        public bool IsExtractor;
    }

    public partial class IndustrialCompanySystem : GameSystemBase
    {
        private NameSystem m_NameSystem;
        private EntityQuery m_IndustrialCompanyQuery;
        private ILog m_Log;
        public IndustrialCompanyDTO[] m_IndustrialCompanyDTOs;
        private ImageSystem m_ImageSystem;
        private PrefabSystem m_PrefabSystem;
        private ResourceSystem m_ResourceSystem;
        public bool IsPanelVisible;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = Mod.log;
            m_IndustrialCompanyDTOs = Array.Empty<IndustrialCompanyDTO>();
            m_NameSystem = World.GetOrCreateSystemManaged<NameSystem>();
            m_ImageSystem = World.GetOrCreateSystemManaged<ImageSystem>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_ResourceSystem = World.GetOrCreateSystemManaged<ResourceSystem>();

            // Define query for industrial companies
            m_IndustrialCompanyQuery = SystemAPI.QueryBuilder()
                .WithAll<Game.Companies.CompanyData, WorkProvider, Employee>()
                .WithAny<IndustrialCompany, StorageCompany, ProcessingCompany, ExtractorCompany>()
                .WithNone<CommercialCompany>()
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

            UpdateIndustrialStats();
        }

        private void UpdateIndustrialStats()
        {
            var entities = m_IndustrialCompanyQuery.ToEntityArray(Allocator.Temp);
            var companies = new List<IndustrialCompanyDTO>(entities.Length);

            try
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];

                    // Skip companies with no employees
                    if (!EntityManager.TryGetBuffer<Employee>(entity, true, out var employeeBuffer) || employeeBuffer.Length == 0)
                    {
                        continue;
                    }

                    if (TryCreateCompanyDTO(entity, employeeBuffer.Length, out var companyDTO))
                    {
                        companies.Add(companyDTO);
                    }
                }
            }
            finally
            {
                entities.Dispose();
            }

            m_IndustrialCompanyDTOs = companies.ToArray();
        }

        private bool TryCreateCompanyDTO(Entity entity, int employeeCount, out IndustrialCompanyDTO companyDTO)
        {
            companyDTO = default;

            // Make sure we have basic required components
            if (!EntityManager.HasComponent<Game.Companies.CompanyData>(entity) ||
                !EntityManager.HasComponent<WorkProvider>(entity) ||
                !EntityManager.HasComponent<PrefabRef>(entity))
            {
                return false;
            }

            // Get basic company data
            var companyData = EntityManager.GetComponentData<Game.Companies.CompanyData>(entity);
            var workProvider = EntityManager.GetComponentData<WorkProvider>(entity);
            var prefabRef = EntityManager.GetComponentData<PrefabRef>(entity);
            Entity prefab = prefabRef.m_Prefab;

            // Get company name
            string companyName = m_NameSystem.GetRenderedLabelName(companyData.m_Brand);

            // Gather all data in a single pass using local methods
            int activeVehicles = 0, maxDeliveryTrucks = 0;
            string resourceType = "None";
            int resourceAmount = 0;
            ProcessResourceInfo[] processResources = Array.Empty<ProcessResourceInfo>();
            int efficiency = 100;
            EfficiencyFactorInfo[] factors = Array.Empty<EfficiencyFactorInfo>();

            // Get economy parameters for calculations
            var econParams = SystemAPI.GetSingleton<EconomyParameterData>();
            var resourcePrefabs = m_ResourceSystem.GetPrefabs();
            var resourceDataLookup = GetComponentLookup<ResourceData>(true);
            var citizenLookup = GetComponentLookup<Citizen>(true);

            // Profitability related variables
            float profitabilityValue = 0f;
            int lastTotalWorth = 0;
            int totalWages = 0;
            int productionPerDay = 0;
            float efficiencyValue = 1f;
            float concentration = 0f;
            string outputResourceName = "";
            bool isExtractor = EntityManager.HasComponent<Game.Companies.ExtractorCompany>(entity);

            // Get vehicle data
            if (EntityManager.TryGetBuffer<OwnedVehicle>(entity, true, out var vehicleBuffer))
            {
                for (int i = 0; i < vehicleBuffer.Length; i++)
                {
                    if (EntityManager.HasComponent<DeliveryTruck>(vehicleBuffer[i].m_Vehicle))
                    {
                        activeVehicles++;
                    }
                }
            }

            // Get maximum trucks capacity
            if (prefab != Entity.Null && EntityManager.HasComponent<TransportCompanyData>(prefab))
            {
                maxDeliveryTrucks = EntityManager.GetComponentData<TransportCompanyData>(prefab).m_MaxTransports;
            }

            // Get resources
            if (EntityManager.TryGetBuffer<Resources>(entity, true, out var resourceBuffer) && resourceBuffer.Length > 0)
            {
                resourceType = resourceBuffer[0].m_Resource.ToString();
                resourceAmount = resourceBuffer[0].m_Amount;
            }

            // Get profitability component if available
            if (EntityManager.TryGetComponent<Profitability>(entity, out var profitability))
            {
                profitabilityValue = ((profitability.m_Profitability - 127f) / 127.5f) * 100f;
                lastTotalWorth = profitability.m_LastTotalWorth;
            }

            // Calculate wages
            if (EntityManager.TryGetBuffer<Employee>(entity, true, out var employeeBuffer))
            {
                totalWages = EconomyUtils.CalculateTotalWage(employeeBuffer, ref econParams);
            }

            // Get process resources
            if (prefab != Entity.Null &&
                EntityManager.HasComponent<IndustrialProcessData>(prefab))
            {
                var processData = EntityManager.GetComponentData<IndustrialProcessData>(prefab);
                var processList = new List<ProcessResourceInfo>();

                // Output
                if (processData.m_Output.m_Resource != Resource.NoResource)
                {
                    string resourceName = EconomyUtils.GetName(processData.m_Output.m_Resource).ToString();
                    outputResourceName = resourceName;
                    processList.Add(new ProcessResourceInfo {
                        ResourceName = resourceName,
                        Amount = processData.m_Output.m_Amount,
                        IsOutput = true
                    });
                }

                // Input 1
                if (processData.m_Input1.m_Resource != Resource.NoResource)
                {
                    processList.Add(new ProcessResourceInfo {
                        ResourceName = EconomyUtils.GetName(processData.m_Input1.m_Resource).ToString(),
                        Amount = processData.m_Input1.m_Amount,
                        IsOutput = false
                    });
                }

                // Input 2
                if (processData.m_Input2.m_Resource != Resource.NoResource)
                {
                    processList.Add(new ProcessResourceInfo {
                        ResourceName = EconomyUtils.GetName(processData.m_Input2.m_Resource).ToString(),
                        Amount = processData.m_Input2.m_Amount,
                        IsOutput = false
                    });
                }

                processResources = processList.ToArray();
            }

            // Get efficiency and property data
            Entity targetEntity = entity;
            if (EntityManager.TryGetComponent<PropertyRenter>(entity, out var renter))
            {
                targetEntity = renter.m_Property;

                // Calculate efficiency
                if (EntityManager.TryGetBuffer(targetEntity, true, out DynamicBuffer<Efficiency> efficiencyBuffer))
                {
                    efficiencyValue = BuildingUtils.GetEfficiency(efficiencyBuffer);
                    
                    // For extractors, calculate concentration
                    if (isExtractor &&
                        EntityManager.TryGetComponent<Attached>(targetEntity, out var attached) &&
                        EntityManager.TryGetBuffer(attached.m_Parent, true, out DynamicBuffer<Game.Areas.SubArea> subAreas) &&
                        EntityManager.TryGetComponent<IndustrialProcessData>(prefab, out var extractorProcess))
                    {
                        var extractorLookup = GetComponentLookup<Game.Areas.Extractor>(true);
                        var prefabRefLookup = GetComponentLookup<PrefabRef>(true);
                        var extractorAreaDataLookup = GetComponentLookup<Game.Prefabs.ExtractorAreaData>(true);
                        var extractorParams = SystemAPI.GetSingleton<ExtractorParameterData>();

                        ExtractorCompanySystem.GetBestConcentration(
                            extractorProcess.m_Output.m_Resource,
                            subAreas,
                            extractorLookup,
                            prefabRefLookup,
                            extractorAreaDataLookup,
                            extractorParams,
                            resourcePrefabs,
                            resourceDataLookup,
                            out concentration,
                            out var _
                        );
                    }
                }
            }

            // Calculate production per day
            if (EntityManager.TryGetBuffer<Employee>(entity, true, out var employees) &&
                EntityManager.TryGetComponent<IndustrialProcessData>(prefab, out var industryProcess))
            {
                productionPerDay = EconomyUtils.GetCompanyProductionPerDay(
                    efficiencyValue,
                    true, // isIndustrial is always true here
                    employees,
                    industryProcess,
                    resourcePrefabs,
                    resourceDataLookup,
                    citizenLookup,
                    ref econParams);
            }

            // Get efficiency
            if (EntityManager.HasComponent<Efficiency>(targetEntity))
            {
                var buffer = EntityManager.GetBuffer<Efficiency>(targetEntity, true);
                if (buffer.Length > 0)
                {
                    using var sortedEfficiencies = buffer.ToNativeArray(Allocator.Temp);
                    sortedEfficiencies.Sort();

                    var tempFactors = new List<EfficiencyFactorInfo>();
                    efficiency = (int)math.round(100f * BuildingUtils.GetEfficiency(buffer));

                    if (efficiency > 0)
                    {
                        float cumulativeEffect = 100f;
                        for (int i = 0; i < sortedEfficiencies.Length; i++)
                        {
                            var item = sortedEfficiencies[i];
                            float effValue = math.max(0f, item.m_Efficiency);
                            cumulativeEffect *= effValue;

                            int percentageChange = math.max(-99, (int)math.round(100f * effValue) - 100);
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

                    factors = tempFactors.ToArray();
                }
            }

            // Create and return the DTO with all collected data
            companyDTO = new IndustrialCompanyDTO
            {
                EntityId = entity,
                CompanyName = companyName,
                TotalEmployees = employeeCount,
                MaxWorkers = workProvider.m_MaxWorkers,
                VehicleCount = activeVehicles,
                VehicleCapacity = maxDeliveryTrucks,
                Resources = resourceType,
                ResourceAmount = resourceAmount,
                ProcessResources = processResources,
                TotalEfficiency = efficiency,
                Factors = factors,
                // Add profitability information
                Profitability = profitabilityValue,
                LastTotalWorth = lastTotalWorth,
                TotalWages = totalWages,
                ProductionPerDay = productionPerDay,
                EfficiencyValue = efficiencyValue * 100f,
                Concentration = concentration,
                OutputResourceName = outputResourceName,
                IsExtractor = isExtractor
            };

            return true;
        }
    }
}