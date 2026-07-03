using Colossal.Collections;
using Game.Buildings;
using Game.Citizens;
using Game.Companies;
using Game.Economy;
using Game.Objects;
using Game.Prefabs;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using ExtractorCompany = Game.Companies.ExtractorCompany;

namespace InfoLoomTwo.Systems.IndustrialSystems.IndustrialCompanyData
{
    public partial class IndustrialCompanySystem
    {
        [BurstCompile]
        public struct ProcessIndustrialCompaniesJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle EntityType;
            [ReadOnly] public ComponentTypeHandle<Game.Companies.CompanyData> CompanyDataType;
            [ReadOnly] public ComponentTypeHandle<WorkProvider> WorkProviderType;
            [ReadOnly] public ComponentTypeHandle<PrefabRef> PrefabRefType;

            [ReadOnly] public ComponentTypeHandle<PropertyRenter> PropertyRenterType;
            [ReadOnly] public ComponentTypeHandle<Profitability> ProfitabilityType;
            [ReadOnly] public ComponentTypeHandle<Attached> AttachedType;

            [ReadOnly] public BufferTypeHandle<Employee> EmployeeBufferType;
            [ReadOnly] public BufferTypeHandle<OwnedVehicle> OwnedVehicleBufferType;
            [ReadOnly] public BufferTypeHandle<Resources> ResourcesBufferType;

            [ReadOnly] public ComponentLookup<TransportCompanyData> TransportCompanyDataLookup;
            [ReadOnly] public ComponentLookup<IndustrialProcessData> IndustrialProcessDataLookup;
            [ReadOnly] public ComponentLookup<ResourceData> ResourceDataLookup;
            [ReadOnly] public ComponentLookup<Citizen> CitizenLookup;
            [ReadOnly] public ComponentLookup<Game.Vehicles.DeliveryTruck> DeliveryTruckLookup;
            [ReadOnly] public ComponentLookup<ExtractorCompany> ExtractorCompanyLookup;
            [ReadOnly] public ComponentLookup<Game.Areas.Extractor> ExtractorLookup;
            [ReadOnly] public ComponentLookup<PrefabRef> PrefabRefLookup;
            [ReadOnly] public ComponentLookup<Game.Prefabs.ExtractorAreaData> ExtractorAreaDataLookup;
            [ReadOnly] public BufferLookup<Efficiency> EfficiencyLookup;
            [ReadOnly] public BufferLookup<Game.Areas.SubArea> SubAreaLookup;

            [ReadOnly] public EconomyParameterData EconomyParams;
            [ReadOnly] public ExtractorParameterData ExtractorParams;
            [ReadOnly] public ResourcePrefabs ResourcePrefabs;
            
            [ReadOnly] public ComponentLookup<CompanyStatisticData> CompanyStatisticDataLookup;
            public NativeValue<Resource> Input1Resource;
            public NativeValue<Resource> Input2Resource;
            public NativeValue<Resource> OutputResource;

            public NativeList<IndustrialCompanyJobData>.ParallelWriter ResultWriter;
            
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                if (!chunk.Has(ref EmployeeBufferType))
                    return;

                var entities = chunk.GetNativeArray(EntityType);
                var companyDataArray = chunk.GetNativeArray(ref CompanyDataType);
                var workProviderArray = chunk.GetNativeArray(ref WorkProviderType);
                var prefabRefArray = chunk.GetNativeArray(ref PrefabRefType);

                var hasPropertyRenter = chunk.Has(ref PropertyRenterType);
                var hasProfitability = chunk.Has(ref ProfitabilityType);
                var hasOwnedVehicles = chunk.Has(ref OwnedVehicleBufferType);
                var hasResources = chunk.Has(ref ResourcesBufferType);
                var hasAttached = chunk.Has(ref AttachedType);

                var propertyRenterArray = hasPropertyRenter ? chunk.GetNativeArray(ref PropertyRenterType) : default;
                var profitabilityArray = hasProfitability ? chunk.GetNativeArray(ref ProfitabilityType) : default;
                var attachedArray = hasAttached ? chunk.GetNativeArray(ref AttachedType) : default;

                var employeeBufferAccessor = chunk.GetBufferAccessor(ref EmployeeBufferType);
                var ownedVehicleAccessor = hasOwnedVehicles ? chunk.GetBufferAccessor(ref OwnedVehicleBufferType) : default;
                var resourcesAccessor = hasResources ? chunk.GetBufferAccessor(ref ResourcesBufferType) : default;

                for (int i = 0; i < chunk.Count; i++)
                {
                    var entity = entities[i];
                    var employeeBuffer = employeeBufferAccessor[i];

                    if (employeeBuffer.Length == 0)
                        continue;
                    
                    var companyData = companyDataArray[i];
                    var workProvider = workProviderArray[i];
                    var prefabRef = prefabRefArray[i];
                    Entity prefab = prefabRef.m_Prefab;

                    int activeVehicles = hasOwnedVehicles ? CountActiveVehicles(ownedVehicleAccessor[i]) : 0;
                    int maxDeliveryTrucks = GetMaxDeliveryTrucks(prefab);
                    int resourceCount = hasResources ? resourcesAccessor[i].Length : 0;
                    bool isExtractor = ExtractorCompanyLookup.HasComponent(entity);

                    float profitabilityValue = 0f;
                    int lastTotalWorth = 0;
                    if (hasProfitability)
                    {
                        var profitability = profitabilityArray[i];
                        profitabilityValue = ((profitability.m_Profitability - 127f) / 127.5f) * 100f;
                        lastTotalWorth = profitability.m_LastTotalWorth;
                    }
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

                    int productionPerDay = CalculateProductionPerDay(prefab, efficiencyValue, employeeBuffer);
                    
                    if (IndustrialProcessDataLookup.TryGetComponent(prefab, out var processData))
                    {
                            if (processData.m_Input1.m_Resource != Resource.NoResource)
                                Input1Resource.value |= processData.m_Input1.m_Resource;
                            if (processData.m_Input2.m_Resource != Resource.NoResource)
                                Input2Resource.value |= processData.m_Input2.m_Resource;
                            if (processData.m_Output.m_Resource != Resource.NoResource)
                                OutputResource.value |= processData.m_Output.m_Resource;
                    }
                    
                    int totalWages = CalculateTotalWage(employeeBuffer);

                    IndustrialCompanyJobData result = new IndustrialCompanyJobData
                    {
                        EntityId = entity,
                        Brand = companyData.m_Brand,
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
                        IsExtractor = isExtractor,
                        ResourceCount = resourceCount
                    };
                    result.HasStatistics = CompanyStatisticDataLookup.HasComponent(entity);

                    ResultWriter.AddNoResize(result);
                }
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

                return EconomyUtils.GetCompanyProductionPerDay(
                    efficiencyValue,
                    true,
                    employeeBuffer,
                    industryProcess,
                    ResourcePrefabs,
                    ref ResourceDataLookup,
                    ref CitizenLookup,
                    ref EconomyParams);
            }
        }
    }
}