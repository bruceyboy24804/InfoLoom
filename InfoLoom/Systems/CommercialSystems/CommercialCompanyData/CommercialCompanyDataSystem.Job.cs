using Colossal.Collections;
using Game.Buildings;
using Game.Citizens;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using DeliveryTruck = Game.Vehicles.DeliveryTruck;

namespace InfoLoomTwo.Systems.CommercialSystems.CommercialCompanyDebugData
{
    public partial class CommercialCompanyDataSystem
    {
        [BurstCompile]
        public struct ProcessCommercialCompaniesJob : IJobChunk
        {
            // Required component type handles
            [ReadOnly] public EntityTypeHandle EntityType;
            [ReadOnly] public ComponentTypeHandle<Game.Companies.CompanyData> CompanyDataType;
            [ReadOnly] public ComponentTypeHandle<WorkProvider> WorkProviderType;
            [ReadOnly] public ComponentTypeHandle<PrefabRef> PrefabRefType;
            [ReadOnly] public ComponentTypeHandle<ServiceAvailable> ServiceAvailableType;

            // Optional component type handles
            [ReadOnly] public ComponentTypeHandle<PropertyRenter> PropertyRenterType;
            [ReadOnly] public ComponentTypeHandle<Profitability> ProfitabilityType;

            // Buffer type handles
            [ReadOnly] public BufferTypeHandle<Employee> EmployeeBufferType;
            [ReadOnly] public BufferTypeHandle<OwnedVehicle> OwnedVehicleBufferType;
            [ReadOnly] public BufferTypeHandle<Resources> ResourcesBufferType;

            // Component lookups for related entities
            [ReadOnly] public ComponentLookup<ServiceCompanyData> ServiceCompanyDataLookup;
            [ReadOnly] public ComponentLookup<TransportCompanyData> TransportCompanyDataLookup;
            [ReadOnly] public ComponentLookup<IndustrialProcessData> IndustrialProcessDataLookup;
            [ReadOnly] public ComponentLookup<ResourceData> ResourceDataLookup;
            [ReadOnly] public ComponentLookup<Citizen> CitizenLookup;
            [ReadOnly] public ComponentLookup<DeliveryTruck> DeliveryTruckLookup;
            [ReadOnly] public BufferLookup<Efficiency> EfficiencyLookup;

            // Shared data
            [ReadOnly] public EconomyParameterData EconomyParams;
            [ReadOnly] public ResourcePrefabs ResourcePrefabs;
            [ReadOnly] public ComponentLookup<CompanyStatisticData> CompanyStatisticDataLookup;

            // Output data
            public NativeList<CommercialCompanyJobData>.ParallelWriter ResultWriter;
            [NativeDisableParallelForRestriction] public NativeValue<Resource> ProcessInput1;
            [NativeDisableParallelForRestriction] public NativeValue<Resource> ProcessInput2;
            [NativeDisableParallelForRestriction] public NativeValue<Resource> ProcessOutput;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                // Early exit if chunk doesn't have required components
                if (!chunk.Has(ref EmployeeBufferType))
                    return;

                // Get arrays for the entire chunk
                var entities = chunk.GetNativeArray(EntityType);
                var companyDataArray = chunk.GetNativeArray(ref CompanyDataType);
                var workProviderArray = chunk.GetNativeArray(ref WorkProviderType);
                var prefabRefArray = chunk.GetNativeArray(ref PrefabRefType);
                var serviceAvailableArray = chunk.GetNativeArray(ref ServiceAvailableType);

                // Check for optional components at chunk level
                var hasPropertyRenter = chunk.Has(ref PropertyRenterType);
                var hasProfitability = chunk.Has(ref ProfitabilityType);
                var hasOwnedVehicles = chunk.Has(ref OwnedVehicleBufferType);
                var hasResources = chunk.Has(ref ResourcesBufferType);

                var propertyRenterArray = hasPropertyRenter ? chunk.GetNativeArray(ref PropertyRenterType) : default;
                var profitabilityArray = hasProfitability ? chunk.GetNativeArray(ref ProfitabilityType) : default;

                // Get buffer accessors
                var employeeBufferAccessor = chunk.GetBufferAccessor(ref EmployeeBufferType);
                var ownedVehicleAccessor = hasOwnedVehicles ? chunk.GetBufferAccessor(ref OwnedVehicleBufferType) : default;
                var resourcesAccessor = hasResources ? chunk.GetBufferAccessor(ref ResourcesBufferType) : default;

                // Process all entities in this chunk
                for (int i = 0; i < chunk.Count; i++)
                {
                    var entity = entities[i];
                    var employeeBuffer = employeeBufferAccessor[i];

                    // Skip companies with no employees
                    if (employeeBuffer.Length == 0)
                        continue;

                    var companyData = companyDataArray[i];
                    var workProvider = workProviderArray[i];
                    var prefabRef = prefabRefArray[i];
                    var serviceAvailable = serviceAvailableArray[i];
                    Entity prefab = prefabRef.m_Prefab;

                    // Calculate basic data
                    int serviceValue = serviceAvailable.m_ServiceAvailable;
                    int maxService = GetMaxService(prefab);
                    int activeVehicles = hasOwnedVehicles ? CountActiveVehicles(ownedVehicleAccessor[i]) : 0;
                    int maxDeliveryTrucks = GetMaxDeliveryTrucks(prefab);
                    int resourceCount = hasResources ? resourcesAccessor[i].Length : 0;

                    // Calculate profitability
                    float profitabilityValue = 0f;
                    int lastTotalWorth = 0;
                    if (hasProfitability)
                    {
                        var profitability = profitabilityArray[i];
                        profitabilityValue = ((profitability.m_Profitability - 127f) / 127.5f) * 100f;
                        lastTotalWorth = profitability.m_LastTotalWorth;
                    }

                    // Calculate wages
                    int totalWages = CalculateTotalWage(employeeBuffer);

                    // Get efficiency data
                    float efficiencyValue = 1f;
                    int efficiency = 100;

                    if (hasPropertyRenter)
                    {
                        Entity targetEntity = propertyRenterArray[i].m_Property;
                        if (EfficiencyLookup.HasBuffer(targetEntity))
                        {
                            var efficiencyBuffer = EfficiencyLookup[targetEntity];
                            efficiencyValue = BuildingUtils.GetEfficiency(efficiencyBuffer);
                            efficiency = (int)math.round(100f * efficiencyValue);
                        }
                    }

                    // Calculate production per day
                    int productionPerDay = CalculateProductionPerDay(prefab, efficiencyValue, employeeBuffer);

                    if (IndustrialProcessDataLookup.TryGetComponent(prefab, out var processData))
                    {
                        if (processData.m_Input1.m_Resource != Resource.NoResource)
                            ProcessInput1.value |= processData.m_Input1.m_Resource;
                        if (processData.m_Input2.m_Resource != Resource.NoResource)
                            ProcessInput2.value |= processData.m_Input2.m_Resource;
                        if (processData.m_Output.m_Resource != Resource.NoResource)
                            ProcessOutput.value |= processData.m_Output.m_Resource;
                    }

                    // Create job data
                    var jobData = new CommercialCompanyJobData
                    {
                        EntityId = entity,
                        Brand = companyData.m_Brand,
                        ServiceAvailable = serviceValue,
                        MaxService = maxService,
                        TotalEmployees = employeeBuffer.Length,
                        MaxWorkers = workProvider.m_MaxWorkers,
                        VehicleCount = activeVehicles,
                        VehicleCapacity = maxDeliveryTrucks,
                        TotalEfficiency = efficiency,
                        Profitability = profitabilityValue,
                        LastTotalWorth = lastTotalWorth,
                        TotalWages = totalWages,
                        ProductionPerDay = productionPerDay,
                        EfficiencyValue = efficiencyValue * 100f,
                        ResourceCount = resourceCount,
                    };
                    jobData.HasStatistics = CompanyStatisticDataLookup.HasComponent(entity);
                    ResultWriter.AddNoResize(jobData);
                }
            }

            [BurstCompile]
            private int GetMaxService(Entity prefab)
            {
                return ServiceCompanyDataLookup.TryGetComponent(prefab, out var serviceData) ?
                    serviceData.m_MaxService : 0;
            }

            [BurstCompile]
            private int GetMaxDeliveryTrucks(Entity prefab)
            {
                return TransportCompanyDataLookup.TryGetComponent(prefab, out var transportData) ?
                    transportData.m_MaxTransports : 0;
            }

            [BurstCompile]
            private int CountActiveVehicles(DynamicBuffer<OwnedVehicle> vehicleBuffer)
            {
                int count = 0;
                for (int i = 0; i < vehicleBuffer.Length; i++)
                {
                    if (DeliveryTruckLookup.HasComponent(vehicleBuffer[i].m_Vehicle))
                        count++;
                }
                return count;
            }

            [BurstCompile]
            private int CalculateTotalWage(DynamicBuffer<Employee> employeeBuffer)
            {
                return EconomyUtils.CalculateTotalWage(employeeBuffer, ref EconomyParams);
            }

            [BurstCompile]
            private int CalculateProductionPerDay(Entity prefab, float efficiencyValue, DynamicBuffer<Employee> employeeBuffer)
            {
                if (!IndustrialProcessDataLookup.TryGetComponent(prefab, out var industryProcess))
                    return 0;

                // Get the resource data for the output resource
                var resourceData = ResourceDataLookup[ResourcePrefabs[industryProcess.m_Output.m_Resource]];

                // Get service availability data for commercial companies
                ServiceAvailable serviceAvailable = default;
                ServiceCompanyData serviceCompanyData = default;

                if (ServiceCompanyDataLookup.HasComponent(prefab))
                {
                    serviceCompanyData = ServiceCompanyDataLookup[prefab];
                }

                return EconomyUtils.GetCompanyProductionPerDay(
                    efficiencyValue,
                    false, // isIndustrial parameter (commercial companies)
                    employeeBuffer,
                    industryProcess,
                    resourceData,
                    ref CitizenLookup,
                    ref EconomyParams,
                    serviceAvailable,
                    serviceCompanyData
                );
            }
        }
    }
}