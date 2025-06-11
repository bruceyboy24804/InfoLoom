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
        [BurstCompile]
        private struct UpdateCommercialDemandJob : IJob
        {
            [ReadOnly]
            public NativeList<ArchetypeChunk> m_FreePropertyChunks;

            [ReadOnly]
            public NativeList<ArchetypeChunk> m_CommercialProcessDataChunks;

            [ReadOnly]
            public EntityTypeHandle m_EntityType;

            [ReadOnly]
            public ComponentTypeHandle<PrefabRef> m_PrefabType;

            [ReadOnly]
            public ComponentTypeHandle<IndustrialProcessData> m_ProcessType;

            [ReadOnly]
            public BufferTypeHandle<Renter> m_RenterType;

            [ReadOnly]
            public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDatas;

            [ReadOnly]
            public ComponentLookup<ResourceData> m_ResourceDatas;

            [ReadOnly]
            public ComponentLookup<WorkplaceData> m_WorkplaceDatas;

            [ReadOnly]
            public ComponentLookup<CommercialCompany> m_CommercialCompanies;

            [ReadOnly]
            public ComponentLookup<Population> m_Populations;

            [ReadOnly]
            public ComponentLookup<Tourism> m_Tourisms;

            [ReadOnly]
            public ResourcePrefabs m_ResourcePrefabs;

            public EconomyParameterData m_EconomyParameters;

            public DemandParameterData m_DemandParameters;

            [ReadOnly]
            public NativeArray<int> m_EmployableByEducation;

            [ReadOnly]
            public NativeArray<int> m_TaxRates;

            [ReadOnly]
            public NativeArray<int> m_ResourceNeeds;

            [ReadOnly]
            public NativeArray<int> m_ProduceCapacity;

            [ReadOnly]
            public NativeArray<int> m_CurrentAvailables;

            [ReadOnly]
            public NativeArray<int> m_Propertyless;

            [ReadOnly]
            public Workplaces m_FreeWorkplaces;

            public float m_BaseConsumptionSum;

            public Entity m_City;

            public NativeValue<int> m_BuildingDemand;

            public NativeArray<int> m_DemandFactors;

            public NativeArray<int> m_Consumptions;

            public NativeArray<int> m_FreeProperties;

            public NativeArray<int> m_ResourceDemands;

            public NativeArray<int> m_BuildingDemands;

            [ReadOnly]
            public NativeArray<int> m_Productions;

            [ReadOnly]
            public NativeArray<int> m_TotalAvailables;

            [ReadOnly]
            public NativeArray<int> m_TotalMaximums;

            [ReadOnly]
            public NativeArray<int> m_Companies;

            [ReadOnly]
            public NativeArray<int> m_TotalMaxWorkers;

            [ReadOnly]
            public NativeArray<int> m_TotalCurrentWorkers;

            public NativeArray<int> m_Results;

            public NativeValue<Resource> m_ExcludedResources;

            public void Execute()
            {
                ResourceIterator iterator = ResourceIterator.GetIterator();
                Population population = m_Populations[m_City];
                Tourism tourism = m_Tourisms[m_City];
                int population2 = (population.m_Population + population.m_PopulationWithMoveIn) / 2;
                while (iterator.Next())
                {
                    int resourceIndex = EconomyUtils.GetResourceIndex(iterator.resource);
                    m_FreeProperties[resourceIndex] = 0;
                    m_BuildingDemands[resourceIndex] = 0;
                    m_ResourceDemands[resourceIndex] = 0;
                }

                for (int i = 0; i < m_DemandFactors.Length; i++)
                {
                    m_DemandFactors[i] = 0;
                }
                // Count free properties
                for (int j = 0; j < m_FreePropertyChunks.Length; j++)
                {
                    ArchetypeChunk archetypeChunk = m_FreePropertyChunks[j];
                    NativeArray<PrefabRef> nativeArray = archetypeChunk.GetNativeArray(ref m_PrefabType);
                    BufferAccessor<Renter> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_RenterType);
                    for (int k = 0; k < nativeArray.Length; k++)
                    {
                        Entity prefab = nativeArray[k].m_Prefab;
                        if (!m_BuildingPropertyDatas.HasComponent(prefab))
                        {
                            continue;
                        }
                        bool flag = false;
                        DynamicBuffer<Renter> dynamicBuffer = bufferAccessor[k];
                        for (int l = 0; l < dynamicBuffer.Length; l++)
                        {
                            if (m_CommercialCompanies.HasComponent(dynamicBuffer[l].m_Renter))
                            {
                                flag = true;
                                break;
                            }
                        }
                        if (flag)
                        {
                            continue;
                        }
                        BuildingPropertyData buildingPropertyData = m_BuildingPropertyDatas[prefab];
                        ResourceIterator iterator2 = ResourceIterator.GetIterator();
                        while (iterator2.Next())
                        {
                            if ((buildingPropertyData.m_AllowedSold & iterator2.resource) != Resource.NoResource)
                            {
                                m_FreeProperties[EconomyUtils.GetResourceIndex(iterator2.resource)]++;
                            }
                        }
                        // InfoLoom
                        if (buildingPropertyData.m_AllowedSold != Resource.NoResource)
                        {
                            m_Results[0]++;
                        }
                    }
                }
                bool flag2 = m_BuildingDemand.value > 0;
                m_BuildingDemand.value = 0;
                iterator = ResourceIterator.GetIterator();
                int num = 0;
                // InfoLoom: resources
                int numDemanded = 0;
                // InfoLoom: available workforce
                for (int m = 0; m < 5; m++)
                {
                    int employable = math.max(0, m_EmployableByEducation[m] - m_FreeWorkplaces[m]);
                    if (m >= 2) m_Results[8] += employable;
                    else m_Results[9] += employable;
                }
                // InfoLoom: capacity utilization, sales efficiency
                int numStandard = 0, numLeisure = 0; // number of resources that are included in calculations
                float capUtilStd = 0f, capUtilLei = 0f, salesCapStd = 0f, salesCapLei = 0f;
                float taxRate = 0f, empCap = 0f;
                while (iterator.Next())
                {
                    int resourceIndex2 = EconomyUtils.GetResourceIndex(iterator.resource);
                    if (!m_ResourceDatas.HasComponent(m_ResourcePrefabs[iterator.resource]))
                    {
                        continue;
                    }
                    ResourceData resourceData = m_ResourceDatas[m_ResourcePrefabs[iterator.resource]];
                    if ((resourceData.m_Weight == 0f && !resourceData.m_IsLeisure) || !EconomyUtils.GetProcessComplexity(m_CommercialProcessDataChunks, ref m_WorkplaceDatas, iterator.resource, m_EntityType, m_ProcessType, out var complexity))
                    {
                        continue;
                    }
                    Workplaces workplaces = EconomyUtils.CalculateNumberOfWorkplaces(20, complexity, 1);
                    float num2 = 0f;
                    for (int m = 0; m < 5; m++)
                    {
                        num2 = ((m >= 2) ? (num2 + math.min(5f * (float)workplaces[m], math.max(0, m_EmployableByEducation[m] - m_FreeWorkplaces[m]))) : (num2 + 5f * (float)workplaces[m]));
                    }
                    // Workforce demand factor based on potential workforce availability
                    float workforceDemandFactor = 0.4f * (num2 / 50f - 1f);
                    
                    // Worker capacity factor - how full are current positions
                    float workerCapacityRatio = ((float)m_TotalCurrentWorkers[resourceIndex2] + 1f) / ((float)m_TotalMaxWorkers[resourceIndex2] + 1f);
                    float workerCapacityFactor = -3f + 4f * workerCapacityRatio;
                    // Dampen positive worker capacity factors
                    if (workerCapacityFactor > 0f)
                    {
                        workerCapacityFactor *= 0.5f;
                    }
                    
                    // Service utilization factor - based on remaining service capacity
                    float serviceUtilizationFactor = 0f;
                    if (m_TotalMaximums[resourceIndex2] > 0)
                    {
                        float capacityUtilization = 1f - (float)m_TotalAvailables[resourceIndex2] / (float)m_TotalMaximums[resourceIndex2];
                        serviceUtilizationFactor = -3f + 10f * capacityUtilization;
                    }
                    
                    // Resource needs vs capacity factor
                    float demandCapacityDiff = m_DemandParameters.m_CommercialBaseDemand * (float)m_ResourceNeeds[resourceIndex2] - (float)m_ProduceCapacity[resourceIndex2];
                    float resourceNeedsFactor = 2f * demandCapacityDiff / math.max(100f, (float)m_ResourceNeeds[resourceIndex2] + 1f);
                    
                    // Tax rate effect on demand
                    float taxDeviation = (float)TaxSystem.GetCommercialTaxRate(iterator.resource, m_TaxRates) - 10f; // 10% is baseline
                    float taxFactor = -0.1f * taxDeviation; // Higher taxes = lower demand
                    
                    // Calculate final resource demand with all factors
                    const float BASE_DEMAND = 0.2f;
                    m_ResourceDemands[resourceIndex2] = Mathf.RoundToInt(100f * (BASE_DEMAND + 
                                                                             serviceUtilizationFactor + 
                                                                             workerCapacityFactor + 
                                                                             workforceDemandFactor + 
                                                                             taxFactor + 
                                                                             resourceNeedsFactor));
                    
                    int resourceDemand = m_ResourceDemands[resourceIndex2];
                    
                    // No demand if no properties available
                    if (m_FreeProperties[resourceIndex2] == 0)
                    {
                        m_ResourceDemands[resourceIndex2] = 0;
                    }
                    
                    if (m_ResourceNeeds[resourceIndex2] > 0)
                    {
                        // Calculate building demand based on companies needing property and free properties
                        float companyNeedingProperty = math.min(1, m_Propertyless[resourceIndex2]) + 
                                                   (float)m_Companies[resourceIndex2] / m_DemandParameters.m_FreeCommercialProportion;
                        float propertyDeficit = math.max(1f, companyNeedingProperty) - (float)m_FreeProperties[resourceIndex2];
                        
                        m_BuildingDemands[resourceIndex2] = math.max(0, Mathf.CeilToInt(math.min(propertyDeficit, resourceDemand)));
                        
                        if (m_BuildingDemands[resourceIndex2] > 0)
                        {
                            m_BuildingDemand.value += resourceDemand;
                        }
                        
                        m_Results[1] += m_Propertyless[resourceIndex2];
                    }
                    
                    // InfoLoom gather data
                    float capUtil = ((m_TotalMaximums[resourceIndex2] == 0) ? 0.3f : (1f - (float)m_TotalAvailables[resourceIndex2] / (float)m_TotalMaximums[resourceIndex2])); // 0.3f is the threshold
                    float salesCapacity = (float)m_ProduceCapacity[resourceIndex2] / (m_DemandParameters.m_CommercialBaseDemand * math.max(100f, (float)m_ResourceNeeds[resourceIndex2]));
                    if (resourceData.m_IsLeisure)
                    {
                        // Meals,Lodging,Entertainment,Recreation
                        numLeisure++;
                        capUtilLei += capUtil;
                        salesCapLei += salesCapacity;
                    }
                    else
                    {
                        // Non-Leisure resources
                        numStandard++;
                        capUtilStd += capUtil;
                        salesCapStd += salesCapacity;
                    }
                    taxRate += (float)TaxSystem.GetCommercialTaxRate(iterator.resource, m_TaxRates);
                    if (!flag2 || (m_BuildingDemands[resourceIndex2] > 0 && resourceDemand > 0))
                    {
                        int effectiveDemand = ((m_BuildingDemands[resourceIndex2] > 0) ? resourceDemand : 0);
                        int educatedWorkforceEffect = DemandUtils.GetDemandFactorEffect(m_ResourceDemands[resourceIndex2], workforceDemandFactor);
                        int uneducatedWorkforceEffect = DemandUtils.GetDemandFactorEffect(m_ResourceDemands[resourceIndex2], workerCapacityFactor);
                        int resourceAndCapacityEffect = DemandUtils.GetDemandFactorEffect(m_ResourceDemands[resourceIndex2], resourceNeedsFactor) + 
                                                   DemandUtils.GetDemandFactorEffect(m_ResourceDemands[resourceIndex2], serviceUtilizationFactor);
                        int taxEffect = DemandUtils.GetDemandFactorEffect(m_ResourceDemands[resourceIndex2], taxFactor);
                        int totalEffects = educatedWorkforceEffect + uneducatedWorkforceEffect + resourceAndCapacityEffect + taxEffect;
                        
                        m_DemandFactors[2] += educatedWorkforceEffect; // EducatedWorkforce 
                        m_DemandFactors[1] += uneducatedWorkforceEffect; // UneducatedWorkforce 
                        if (iterator.resource == Resource.Lodging)
                        {
                            m_DemandFactors[9] += resourceAndCapacityEffect; // TouristDemand 
                        }
                        else if (iterator.resource == Resource.Petrochemicals)
                        {
                            m_DemandFactors[16] += resourceAndCapacityEffect; // PetrolLocalDemand 
                        }
                        else
                        {
                            m_DemandFactors[4] += resourceAndCapacityEffect; // LocalDemand 
                        }
                        m_DemandFactors[11] += taxEffect; // Taxes 
                        m_DemandFactors[13] += math.min(0, effectiveDemand - totalEffects); // EmptyBuildings
                        // InfoLoom
                        empCap += ((float)m_TotalCurrentWorkers[resourceIndex2] + 1f) / ((float)m_TotalMaxWorkers[resourceIndex2] + 1f);
                        numDemanded++;
                    }
                    else
                    {
                        m_ExcludedResources.value |= iterator.resource;
                    }
                    num++;
                    m_ResourceDemands[resourceIndex2] = math.min(100, math.max(0, m_ResourceDemands[resourceIndex2]));
                }
                m_BuildingDemand.value = math.clamp(2 * m_BuildingDemand.value / num, 0, 100);
                m_Results[2] = Mathf.RoundToInt(10f * taxRate / (float)(numStandard + numLeisure));
                m_Results[3] = Mathf.RoundToInt(100f * capUtilStd / (float)numStandard);
                m_Results[4] = Mathf.RoundToInt(100f * capUtilLei / (float)numLeisure);
                m_Results[5] = Mathf.RoundToInt(100f * salesCapStd / (float)numStandard);
                m_Results[6] = Mathf.RoundToInt(100f * salesCapLei / (float)numLeisure);
                m_Results[7] = Mathf.RoundToInt(1000f * empCap / (float)numDemanded);
            }
        }

        // System dependencies
        private SimulationSystem m_SimulationSystem;
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
        private NativeValue<int> m_BuildingDemand;
        private NativeArray<int> m_DemandFactors;
        private NativeArray<int> m_ResourceDemands;
        private NativeArray<int> m_BuildingDemands;
        private NativeArray<int> m_Consumption;
        private NativeArray<int> m_FreeProperties;
        private JobHandle m_ReadDependencies;

        // InfoLoom specific data
        public NativeArray<int> m_Results;
        public NativeValue<Resource> m_ExcludedResources;

        [DebugWatchValue]
        public int BaseConsumptionSum => m_ResourceSystem.BaseConsumptionSum;

        // Panel state
        public bool IsPanelVisible { get; set; }
        public bool ForceUpdate { get; private set; }

        // COMMERCIAL Results array mapping:
        // 0 - free properties
        // 1 - propertyless companies
        // 2 - tax rate
        // 3 & 4 - service utilization rate (available/maximum), non-leisure/leisure
        // 5 & 6 - sales efficiency (sales capacity/consumption), non-leisure/leisure // how effectively a shop is utilizing its sales capacity by comparing the actual sales to the maximum sales potential
        // 7 - employee capacity ratio // how efficiently the company is utilizing its workforce by comparing the actual number of employees to the maximum number it could employ
        // 8 & 9 - educated & uneducated workforce

        public void AddReader(JobHandle reader)
        {
            m_ReadDependencies = JobHandle.CombineDependencies(m_ReadDependencies, reader);
        }
        public void ForceUpdateOnce()
        {
            ForceUpdate = true;
        }
      
        protected override void OnCreate()
        {
            base.OnCreate();
            m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>(); // TODO: use UIUpdateState eventually
            m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
            m_TaxSystem = base.World.GetOrCreateSystemManaged<TaxSystem>();
            m_CountHouseholdDataSystem = base.World.GetOrCreateSystemManaged<CountHouseholdDataSystem>();
            m_CountWorkplacesSystem = base.World.GetOrCreateSystemManaged<CountWorkplacesSystem>();
            m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
            m_CountCompanyDataSystem = base.World.GetOrCreateSystemManaged<CountCompanyDataSystem>();
            m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
            m_DemandParameterQuery = GetEntityQuery(ComponentType.ReadOnly<DemandParameterData>());
            m_FreeCommercialQuery = GetEntityQuery(ComponentType.ReadOnly<CommercialProperty>(), ComponentType.ReadOnly<PropertyOnMarket>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Abandoned>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Condemned>(), ComponentType.Exclude<Temp>());
            m_CommercialProcessDataQuery = GetEntityQuery(ComponentType.ReadOnly<IndustrialProcessData>(), ComponentType.ReadOnly<ServiceCompanyData>());
            m_BuildingDemand = new NativeValue<int>(Allocator.Persistent);
            m_DemandFactors = new NativeArray<int>(18, Allocator.Persistent);
            int resourceCount = EconomyUtils.ResourceCount;
            m_ResourceDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
            m_BuildingDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
            m_FreeProperties = new NativeArray<int>(resourceCount, Allocator.Persistent);
            RequireForUpdate(m_EconomyParameterQuery);
            RequireForUpdate(m_DemandParameterQuery);
            RequireForUpdate(m_CommercialProcessDataQuery);

            // InfoLoom

            SetDefaults(); // there is no serialization, so init just for safety
            m_Results = new NativeArray<int>(10, Allocator.Persistent);
            m_ExcludedResources = new NativeValue<Resource>(Allocator.Persistent);

            Mod.log.Info("CommercialDemandUISystem created.");
        }

        protected override void OnDestroy()
        {
            m_BuildingDemand.Dispose();
            m_DemandFactors.Dispose();
            m_ResourceDemands.Dispose();
            m_BuildingDemands.Dispose();
            m_Consumption.Dispose();
            m_FreeProperties.Dispose();
            // InfoLoom

            m_Results.Dispose();
            m_ExcludedResources.Dispose();
            base.OnDestroy();
        }

        public void SetDefaults()
        {
            m_BuildingDemand.value = 50; // Infixo: default is 0 which is no demand, let's start with some demand
            m_DemandFactors.Fill(0);
            m_ResourceDemands.Fill(0);
            m_BuildingDemands.Fill(0);
            m_Consumption.Fill(0);
            m_FreeProperties.Fill(0);
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 256;
        }

        protected override void OnUpdate()
        {
           if (!IsPanelVisible)
                return;
           
           ForceUpdate = false;
            
            ResetResults();

            if (!m_DemandParameterQuery.IsEmptyIgnoreFilter && !m_EconomyParameterQuery.IsEmptyIgnoreFilter)
            {
                JobHandle deps;
                CountCompanyDataSystem.CommercialCompanyDatas commercialCompanyDatas = m_CountCompanyDataSystem.GetCommercialCompanyDatas(out deps);
               
                UpdateCommercialDemandJob updateCommercialDemandJob = default(UpdateCommercialDemandJob);
                updateCommercialDemandJob.m_FreePropertyChunks = m_FreeCommercialQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out var outJobHandle);
                updateCommercialDemandJob.m_CommercialProcessDataChunks = m_CommercialProcessDataQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out var outJobHandle2);
                updateCommercialDemandJob.m_EntityType = SystemAPI.GetEntityTypeHandle();
                updateCommercialDemandJob.m_PrefabType = SystemAPI.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
                updateCommercialDemandJob.m_ProcessType = SystemAPI.GetComponentTypeHandle<IndustrialProcessData>(isReadOnly: true);
                updateCommercialDemandJob.m_RenterType = SystemAPI.GetBufferTypeHandle<Renter>(isReadOnly: true);
                updateCommercialDemandJob.m_BuildingPropertyDatas = SystemAPI.GetComponentLookup<BuildingPropertyData>(isReadOnly: true); 
                updateCommercialDemandJob.m_ResourceDatas = SystemAPI.GetComponentLookup<ResourceData>(isReadOnly: true);
                updateCommercialDemandJob.m_WorkplaceDatas = SystemAPI.GetComponentLookup<WorkplaceData>(isReadOnly: true);
                updateCommercialDemandJob.m_CommercialCompanies = SystemAPI.GetComponentLookup<CommercialCompany>(isReadOnly: true);
                updateCommercialDemandJob.m_Populations = SystemAPI.GetComponentLookup<Population>(isReadOnly: true);
                updateCommercialDemandJob.m_Tourisms = SystemAPI.GetComponentLookup<Tourism>(isReadOnly: true);
                updateCommercialDemandJob.m_ResourcePrefabs = m_ResourceSystem.GetPrefabs();
                updateCommercialDemandJob.m_DemandParameters = m_DemandParameterQuery.GetSingleton<DemandParameterData>();
                updateCommercialDemandJob.m_EconomyParameters = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>();
                updateCommercialDemandJob.m_EmployableByEducation = m_CountHouseholdDataSystem.GetEmployables();
                updateCommercialDemandJob.m_TaxRates = m_TaxSystem.GetTaxRates();
                updateCommercialDemandJob.m_FreeWorkplaces = m_CountWorkplacesSystem.GetFreeWorkplaces();
                updateCommercialDemandJob.m_BaseConsumptionSum = m_ResourceSystem.BaseConsumptionSum;
                updateCommercialDemandJob.m_BuildingDemand = m_BuildingDemand;
                updateCommercialDemandJob.m_DemandFactors = m_DemandFactors;
                updateCommercialDemandJob.m_ResourceDemands = m_ResourceDemands;
                updateCommercialDemandJob.m_BuildingDemands = m_BuildingDemands;
                updateCommercialDemandJob.m_ProduceCapacity = commercialCompanyDatas.m_ProduceCapacity;
                updateCommercialDemandJob.m_CurrentAvailables = commercialCompanyDatas.m_CurrentAvailables;
                updateCommercialDemandJob.m_ResourceNeeds = m_CountHouseholdDataSystem.GetResourceNeeds(out deps);
                updateCommercialDemandJob.m_Consumptions = m_Consumption;
                updateCommercialDemandJob.m_TotalAvailables = commercialCompanyDatas.m_CurrentAvailables;
                updateCommercialDemandJob.m_TotalMaximums = commercialCompanyDatas.m_TotalAvailables;
                updateCommercialDemandJob.m_Companies = commercialCompanyDatas.m_ServiceCompanies;
                updateCommercialDemandJob.m_FreeProperties = m_FreeProperties;
                updateCommercialDemandJob.m_Propertyless = commercialCompanyDatas.m_ServicePropertyless;
                updateCommercialDemandJob.m_TotalMaxWorkers = commercialCompanyDatas.m_MaxServiceWorkers;
                updateCommercialDemandJob.m_TotalCurrentWorkers = commercialCompanyDatas.m_CurrentServiceWorkers;
                updateCommercialDemandJob.m_City = m_CitySystem.City;

                updateCommercialDemandJob.m_Results = m_Results;
                updateCommercialDemandJob.m_ExcludedResources = m_ExcludedResources;

                UpdateCommercialDemandJob jobData = updateCommercialDemandJob;
                IJobExtensions.Schedule(jobData, JobUtils.CombineDependencies(base.Dependency, m_ReadDependencies, outJobHandle, deps, outJobHandle2)).Complete();
            }
        }

        private void ResetResults()
        {
            m_ExcludedResources.value = Resource.NoResource;
            m_Results.Fill<int>(0);
        }
    }
}

