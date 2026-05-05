using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Debug;
using Game.Economy;
using Game.Prefabs;
using Game.Reflection;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using Game;
using Game.Simulation;
using Game.UI;
using Colossal.UI.Binding;
using System.Collections.Generic;
using System.Reflection;

namespace InfoLoomTwo.Systems.CommercialSystems.CommercialDemandData
{
    public partial class CommercialSystem : UISystemBase
    {
        // System dependencies
        private ResourceSystem m_ResourceSystem;
        private TaxSystem m_TaxSystem;
        private CountHouseholdDataSystem m_CountHouseholdDataSystem;
        private CountWorkplacesSystem m_CountWorkplacesSystem;
        private CitySystem m_CitySystem;
        private CountCompanyDataSystem m_CountCompanyDataSystem;

        // Queries
        private EntityQuery m_EconomyParameterQuery;
        private EntityQuery m_DemandParameterQuery;
        private EntityQuery m_FreeCommercialQuery;
        private EntityQuery m_CommercialProcessDataQuery;

        // Persistent data
        public NativeArray<int> m_ResourceDemands;
        private NativeArray<int> m_FreeProperties;
        public NativeArray<int> m_Results;
        public NativeValue<Resource> m_IncludedResources;

        // Panel state
        public bool IsPanelVisible { get; set; }

        protected override void OnCreate()
        {
            base.OnCreate();
            
            m_ResourceSystem = World.GetOrCreateSystemManaged<ResourceSystem>();
            m_TaxSystem = World.GetOrCreateSystemManaged<TaxSystem>();
            m_CountHouseholdDataSystem = World.GetOrCreateSystemManaged<CountHouseholdDataSystem>();
            m_CountWorkplacesSystem = World.GetOrCreateSystemManaged<CountWorkplacesSystem>();
            m_CitySystem = World.GetOrCreateSystemManaged<CitySystem>();
            m_CountCompanyDataSystem = World.GetOrCreateSystemManaged<CountCompanyDataSystem>();
            
             m_EconomyParameterQuery = SystemAPI.QueryBuilder().WithAll<EconomyParameterData>().Build();
             m_DemandParameterQuery = SystemAPI.QueryBuilder().WithAll<DemandParameterData>().Build();
             m_FreeCommercialQuery = SystemAPI.QueryBuilder().WithAll<CommercialProperty>().WithAll<PropertyOnMarket, PrefabRef>().WithNone<Abandoned, Destroyed, Deleted, Condemned, Temp >().Build();
             m_CommercialProcessDataQuery = SystemAPI.QueryBuilder().WithAll<IndustrialProcessData, ServiceCompanyData>().Build();

            int resourceCount = EconomyUtils.ResourceCount;
            m_ResourceDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
            m_FreeProperties = new NativeArray<int>(resourceCount, Allocator.Persistent);
            m_Results = new NativeArray<int>(10, Allocator.Persistent);
            m_IncludedResources = new NativeValue<Resource>(Allocator.Persistent);

            RequireForUpdate(m_EconomyParameterQuery);
            RequireForUpdate(m_DemandParameterQuery);
            RequireForUpdate(m_CommercialProcessDataQuery);

            Mod.log.Info("CommercialSystem created.");
        }

        protected override void OnDestroy()
        {
            m_ResourceDemands.Dispose();
            m_FreeProperties.Dispose();
            m_Results.Dispose();
            m_IncludedResources.Dispose();
            base.OnDestroy();
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 512;

        protected override void OnUpdate()
        {
            if (!IsPanelVisible)
                return;

            m_IncludedResources.value = Resource.NoResource;
            m_Results.Fill(0);
            m_ResourceDemands.Fill(0);
            m_FreeProperties.Fill(0);

            JobHandle deps;
            var commercialData = m_CountCompanyDataSystem.GetCommercialCompanyDatas(out deps);
            
            // Count free properties
            CountFreeProperties();
            
            // Calculate demands using game's system
            var gameDemands = CalculateGameDemands();
            
            // Gather display metrics
            GatherMetrics(commercialData, gameDemands);
            
            deps.Complete();
        }

        private void CountFreeProperties()
        {
            var prefabType = SystemAPI.GetComponentTypeHandle<PrefabRef>(true);
            var renterType = SystemAPI.GetBufferTypeHandle<Renter>(true);
            var buildingDatas = SystemAPI.GetComponentLookup<BuildingPropertyData>(true);
            var commercialCompanies = SystemAPI.GetComponentLookup<CommercialCompany>(true);

            var chunks = m_FreeCommercialQuery.ToArchetypeChunkArray(Allocator.Temp);
            
            foreach (var chunk in chunks)
            {
                var prefabs = chunk.GetNativeArray(ref prefabType);
                var renters = chunk.GetBufferAccessor(ref renterType);

                for (int i = 0; i < chunk.Count; i++)
                {
                    if (!buildingDatas.HasComponent(prefabs[i].m_Prefab))
                        continue;

                    bool hasCommercialTenant = false;
                    var renterBuffer = renters[i];
                    
                    for (int j = 0; j < renterBuffer.Length; j++)
                    {
                        if (commercialCompanies.HasComponent(renterBuffer[j].m_Renter))
                        {
                            hasCommercialTenant = true;
                            break;
                        }
                    }

                    if (!hasCommercialTenant)
                    {
                        var buildingData = buildingDatas[prefabs[i].m_Prefab];
                        var iterator = ResourceIterator.GetIterator();
                        
                        while (iterator.Next())
                        {
                            if ((buildingData.m_AllowedSold & iterator.resource) != Resource.NoResource)
                            {
                                m_FreeProperties[EconomyUtils.GetResourceIndex(iterator.resource)]++;
                            }
                        }
                        
                        if (buildingData.m_AllowedSold != Resource.NoResource)
                            m_Results[0]++;
                    }
                }
            }
            
            chunks.Dispose();
        }

        private NativeArray<int> CalculateGameDemands()
        {
            var commercialSystem = World.GetExistingSystemManaged<CommercialDemandSystem>();
            if (commercialSystem == null)
                return new NativeArray<int>(EconomyUtils.ResourceCount, Allocator.Temp);

            JobHandle deps;
            var demands = commercialSystem.GetResourceDemands(out deps);
            deps.Complete();
            
            var result = new NativeArray<int>(demands.Length, Allocator.Temp);
            demands.CopyTo(result);
            
            return result;
        }

        private void GatherMetrics(CountCompanyDataSystem.CommercialCompanyDatas data, NativeArray<int> gameDemands)
        {
            var resourceDatas = SystemAPI.GetComponentLookup<ResourceData>(true);
            var resourcePrefabs = m_ResourceSystem.GetPrefabs();
            var taxRates = m_TaxSystem.GetTaxRates();
            var employables = m_CountHouseholdDataSystem.GetEmployables(out JobHandle deps1);
            deps1.Complete();
            var freeWorkplaces = m_CountWorkplacesSystem.GetFreeWorkplaces();
            var demandParams = m_DemandParameterQuery.GetSingleton<DemandParameterData>();
            var population = EntityManager.GetComponentData<Population>(m_CitySystem.City);
            
            int actualPopulation = population.m_Population;

            int numStandard = 0, numLeisure = 0;
            float capUtilStd = 0f, capUtilLei = 0f, salesCapStd = 0f, salesCapLei = 0f;
            float totalTaxRate = 0f, totalEmpCapacity = 0f;
            int resourceCount = 0;

            var iterator = ResourceIterator.GetIterator();

            while (iterator.Next())
            {
                if (!EconomyUtils.IsCommercialResource(iterator.resource))
                    continue;

                int idx = EconomyUtils.GetResourceIndex(iterator.resource);

                if (!resourceDatas.HasComponent(resourcePrefabs[iterator.resource]))
                    continue;

                var resourceData = resourceDatas[resourcePrefabs[iterator.resource]];

                m_ResourceDemands[idx] = gameDemands[idx];
                m_Results[1] += data.m_ServicePropertyless[idx];

                float capUtil = data.m_TotalAvailables[idx] == 0 ? 0.3f :
                    1f - (float)data.m_CurrentAvailables[idx] / data.m_TotalAvailables[idx];

                float empCapacity = (data.m_MaxServiceWorkers[idx] == 0) ? 0f :
                    (float)data.m_CurrentServiceWorkers[idx] / data.m_MaxServiceWorkers[idx];

                // Calculate sales capacity based on current population demand
                // This matches the game's approach: comparing current availables to expected demand
                float salesCapacity = 0f;
                if (iterator.resource != Resource.Lodging && actualPopulation > 0)
                {
                    // Calculate expected demand based on population (same as game)
                    int expectedDemand = actualPopulation <= 1000 ? 2500 : 
                        2500 * (int)Mathf.Log10(0.01f * actualPopulation);
                    
                    // Sales capacity = how current availables compare to expected demand
                    salesCapacity = expectedDemand > 0 ? 
                        (float)data.m_CurrentAvailables[idx] / expectedDemand : 0f;
                }
                else if (iterator.resource == Resource.Lodging)
                {
                    // For lodging, use tourism data if available
                    if (EntityManager.HasComponent<Tourism>(m_CitySystem.City))
                    {
                        var tourism = EntityManager.GetComponentData<Tourism>(m_CitySystem.City);
                        int expectedLodging = (int)(tourism.m_CurrentTourists * 
                            demandParams.m_HotelRoomPercentRequirement);
                        salesCapacity = expectedLodging > 0 ? 
                            (float)tourism.m_Lodging.y / expectedLodging : 0f;
                    }
                }

                if (resourceData.m_IsLeisure)
                {
                    numLeisure++;
                    capUtilLei += capUtil;
                    salesCapLei += salesCapacity;
                }
                else
                {
                    numStandard++;
                    capUtilStd += capUtil;
                    salesCapStd += salesCapacity;
                }

                totalTaxRate += TaxSystem.GetCommercialTaxRate(iterator.resource, taxRates);
                totalEmpCapacity += empCapacity;
                resourceCount++;

                if (m_ResourceDemands[idx] >= Mod.setting.comResDemValue)
                    m_IncludedResources.value |= iterator.resource;
            }

            // Calculate workforce availability
            for (int i = 0; i < 5; i++)
            {
                int available = math.max(0, employables[i] - freeWorkplaces[i]);
                if (i >= 2) m_Results[8] += available;
                else m_Results[9] += available;
            }

            m_Results[2] = resourceCount > 0 ? Mathf.RoundToInt(10f * totalTaxRate / resourceCount) : 0;
            m_Results[3] = numStandard > 0 ? Mathf.RoundToInt(100f * capUtilStd / numStandard) : 0;
            m_Results[4] = numLeisure > 0 ? Mathf.RoundToInt(100f * capUtilLei / numLeisure) : 0;
            m_Results[5] = numStandard > 0 ? Mathf.RoundToInt(100f * salesCapStd / numStandard) : 0;
            m_Results[6] = numLeisure > 0 ? Mathf.RoundToInt(100f * salesCapLei / numLeisure) : 0;
            m_Results[7] = resourceCount > 0 ? Mathf.RoundToInt(1000f * totalEmpCapacity / resourceCount) : 0;

            gameDemands.Dispose();
            employables.Dispose();
        }
    }
}

