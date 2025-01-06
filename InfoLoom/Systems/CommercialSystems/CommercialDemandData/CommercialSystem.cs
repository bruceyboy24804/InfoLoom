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
    public partial class CommercialSystem : SystemBase
    {

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

            //public NativeValue<int> m_CompanyDemand;

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
                //Plugin.Log($"Execute: baseDem {m_DemandParameters.m_CommercialBaseDemand} freeRatio {m_DemandParameters.m_FreeCommercialProportion} baseConsSum {m_BaseConsumptionSum} resCons {m_EconomyParameters.m_ResourceConsumption} tourMult {m_EconomyParameters.m_TouristConsumptionMultiplier}");
                // Calculate estimated and actual consumption
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
                            //Plugin.Log($"Property {m_Results[0]}: {buildingPropertyData.m_AllowedSold}");
                            m_Results[0]++;
                        }
                    }
                }
                //Plugin.Log($"Free properties {m_Results[0]}, building demand is {m_BuildingDemand.value}");
                //m_CompanyDemand.value = 0; // not used HERE, maybe needed for other systems
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
                    if ((resourceData.m_Weight == 0f && !resourceData.m_IsLeisure) || !EconomyUtils.GetProcessComplexity(m_CommercialProcessDataChunks, m_WorkplaceDatas, iterator.resource, m_EntityType, m_ProcessType, out var complexity))
                    {
                        continue;
                    }
                    Workplaces workplaces = WorkProviderSystem.CalculateNumberOfWorkplaces(20, complexity, 1);
                    float num2 = 0f;
                    for (int m = 0; m < 5; m++)
                    {
                        num2 = ((m >= 2) ? (num2 + math.min(5f * (float)workplaces[m], math.max(0, m_EmployableByEducation[m] - m_FreeWorkplaces[m]))) : (num2 + 5f * (float)workplaces[m]));
                    }
                    float num3 = 0.4f * (num2 / 50f - 1f);
                    float num4 = -3f + 4f * (((float)m_TotalCurrentWorkers[resourceIndex2] + 1f) / ((float)m_TotalMaxWorkers[resourceIndex2] + 1f));
                    if (num4 > 0f)
                    {
                        num4 *= 0.5f;
                    }
                    float num5 = ((m_TotalMaximums[resourceIndex2] == 0) ? 0f : (-3f + 10f * (1f - (float)m_TotalAvailables[resourceIndex2] / (float)m_TotalMaximums[resourceIndex2])));
                    float num6 = 2f * (m_DemandParameters.m_CommercialBaseDemand * (float)m_ResourceNeeds[resourceIndex2] - (float)m_ProduceCapacity[resourceIndex2]) / math.max(100f, (float)m_ResourceNeeds[resourceIndex2] + 1f);
                    //Plugin.Log($"Eff {iterator.resource}: capUtil {m_TotalAvailables[resourceIndex2]} / {m_TotalMaximums[resourceIndex2]} = {(m_TotalMaximums[resourceIndex2] == 0 ? "na" : (1f - (float)m_TotalAvailables[resourceIndex2] / (float)m_TotalMaximums[resourceIndex2]))}, " +
                    //$"resEff {m_Productions[resourceIndex2]} / {m_Consumptions[resourceIndex2]} = {(float)m_Productions[resourceIndex2]/((float)m_Consumptions[resourceIndex2]+1f)}");
                    float num7 = -0.1f * ((float)TaxSystem.GetCommercialTaxRate(iterator.resource, m_TaxRates) - 10f);
                    m_ResourceDemands[resourceIndex2] = Mathf.RoundToInt(100f * (0.2f + num5 + num4 + num3 + num7 + num6));
                    int num8 = m_ResourceDemands[resourceIndex2];
                    if (m_FreeProperties[resourceIndex2] == 0)
                    {
                        m_ResourceDemands[resourceIndex2] = 0;
                    }
                    if (m_ResourceNeeds[resourceIndex2] > 0)
                    {
                        //m_CompanyDemand.value += Mathf.RoundToInt(math.min(100, math.max(0, m_ResourceDemands[resourceIndex2])));
                        m_BuildingDemands[resourceIndex2] = math.max(0, Mathf.CeilToInt(math.min(math.max(1f, (float)math.min(1, m_Propertyless[resourceIndex2]) + (float)m_Companies[resourceIndex2] / m_DemandParameters.m_FreeCommercialProportion) - (float)m_FreeProperties[resourceIndex2], num8)));
                        if (m_BuildingDemands[resourceIndex2] > 0)
                        {
                            m_BuildingDemand.value += ((m_BuildingDemands[resourceIndex2] > 0) ? num8 : 0);
                        }
                        //Plugin.Log($"Com {iterator.resource}: noprop {m_Propertyless[resourceIndex2]} comp {m_Companies[resourceIndex2]} free {m_FreeProperties[resourceIndex2]} resdem {num8}");
                        m_Results[1] += m_Propertyless[resourceIndex2];
                        //m_Results[2] += m_Companies[resourceIndex2];
                    }
                    //Plugin.Log($"Res {iterator.resource} ({num}): free {m_FreeProperties[resourceIndex2]} buldem {m_BuildingDemands[resourceIndex2]} wrkdem {num2} [ edu {num3:F2} wrk {num4:F2} cap {num5:F2} rat {num6:F2} tax {num7:F2} ] resdem {num8}");
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
                    if (!flag2 || (m_BuildingDemands[resourceIndex2] > 0 && num8 > 0))
                    {
                        int num9 = ((m_BuildingDemands[resourceIndex2] > 0) ? num8 : 0);
                        int demandFactorEffect = DemandUtils.GetDemandFactorEffect(m_ResourceDemands[resourceIndex2], num3);
                        int demandFactorEffect2 = DemandUtils.GetDemandFactorEffect(m_ResourceDemands[resourceIndex2], num4);
                        int num10 = DemandUtils.GetDemandFactorEffect(m_ResourceDemands[resourceIndex2], num6) + DemandUtils.GetDemandFactorEffect(m_ResourceDemands[resourceIndex2], num5);
                        int demandFactorEffect3 = DemandUtils.GetDemandFactorEffect(m_ResourceDemands[resourceIndex2], num7);
                        int num11 = demandFactorEffect + demandFactorEffect2 + num10 + demandFactorEffect3;
                        m_DemandFactors[2] += demandFactorEffect; // EducatedWorkforce 
                        m_DemandFactors[1] += demandFactorEffect2; // UneducatedWorkforce 
                        if (iterator.resource == Resource.Lodging)
                        {
                            m_DemandFactors[9] += num10; // TouristDemand 
                        }
                        else if (iterator.resource == Resource.Petrochemicals)
                        {
                            m_DemandFactors[16] += num10; // PetrolLocalDemand 
                        }
                        else
                        {
                            m_DemandFactors[4] += num10; // LocalDemand 
                        }
                        m_DemandFactors[11] += demandFactorEffect3; // Taxes 
                        m_DemandFactors[13] += math.min(0, num9 - num11); // EmptyBuildings
                                                                          // InfoLoom
                        empCap += ((float)m_TotalCurrentWorkers[resourceIndex2] + 1f) / ((float)m_TotalMaxWorkers[resourceIndex2] + 1f);
                        numDemanded++;
                        //Plugin.Log($"... {iterator.resource}: resdem {num9} n10 {num10} effects {num11}, empty {num9-num11}");
                    }
                    else
                    {
                        m_ExcludedResources.value |= iterator.resource;
                    }
                    num++;
                    m_ResourceDemands[resourceIndex2] = math.min(100, math.max(0, m_ResourceDemands[resourceIndex2]));
                }
                m_BuildingDemand.value = math.clamp(2 * m_BuildingDemand.value / num, 0, 100);
                // InfoLoom
                //Plugin.Log($"RESOURCES: demanded {numDemanded} excluded {m_ExcludedResources.value:X} {m_ExcludedResources.value}");
                //Plugin.Log($"COMPANIES: freeProperties [0]={m_Results[0]} propertyless [1]={m_Results[1]}");// companies {m_Results[2]}");
                m_Results[2] = Mathf.RoundToInt(10f * taxRate / (float)(numStandard + numLeisure));
                //Plugin.Log($"TAX RATE: [2]={m_Results[2]} {taxRate / (float)(numStandard + numLeisure):F1}");
                // 3 & 4 - capacity utilization rate. (available/maximum), non-leisure/leisure
                // 5 & 6 - resource efficiency (production/consumption), non-leisure/leisure
                //Plugin.Log($"STANDARD: {numStandard} {capUtilStd/(float)numStandard} {salesCapStd/(float)numStandard}");
                //Plugin.Log($"LEISURE: {numLeisure} {capUtilLei / (float)numLeisure} {salesCapLei / (float)numLeisure}");
                m_Results[3] = Mathf.RoundToInt(100f * capUtilStd / (float)numStandard);
                m_Results[4] = Mathf.RoundToInt(100f * capUtilLei / (float)numLeisure);
                //Plugin.Log($"SERVICE UTILIZATION: [3]={m_Results[3]} [4]={m_Results[4]} std {capUtilStd / (float)numStandard} lei {capUtilLei / (float)numLeisure}, 30% is the default threshold");
                m_Results[5] = Mathf.RoundToInt(100f * salesCapStd / (float)numStandard);
                m_Results[6] = Mathf.RoundToInt(100f * salesCapLei / (float)numLeisure);
                //Plugin.Log($"SALES CAPACITY: [5]={m_Results[5]} [6]={m_Results[6]} std {salesCapStd / (float)numStandard} lei {salesCapLei / (float)numLeisure}, 100% means capacity = consumption");
                m_Results[7] = Mathf.RoundToInt(1000f * empCap / (float)numDemanded);
                //Plugin.Log($"EMPLOYEE CAPACITY RATIO: [7]={m_Results[7]} {100f*empCap/(float)numDemanded:F1}%, 75% is the default threshold");
                //Plugin.Log($"AVAILABLE WORKFORCE: educated [8]={m_Results[8]} uneducated [9]={m_Results[9]}");
            }
        }

        

        

        private SimulationSystem m_SimulationSystem;

        private ResourceSystem m_ResourceSystem;

        private TaxSystem m_TaxSystem;

        private CountHouseholdDataSystem m_CountHouseholdDataSystem;

        private CountWorkplacesSystem m_CountWorkplacesSystem;

        private CitySystem m_CitySystem;



        private CountCompanyDataSystem m_CountCompanyDataSystem;

        private EntityQuery m_EconomyParameterQuery;

        private EntityQuery m_DemandParameterQuery;

        private EntityQuery m_FreeCommercialQuery;

        private EntityQuery m_CommercialProcessDataQuery;

        //private NativeValue<int> m_CompanyDemand;

        private NativeValue<int> m_BuildingDemand;

        //[EnumArray(typeof(DemandFactor))]
        //[DebugWatchValue]
        private NativeArray<int> m_DemandFactors;

        //[ResourceArray]
        //[DebugWatchValue]
        private NativeArray<int> m_ResourceDemands;

        //[ResourceArray]
        //[DebugWatchValue]
        private NativeArray<int> m_BuildingDemands;

        //[ResourceArray]
        //[DebugWatchValue]
        private NativeArray<int> m_Consumption;

        //[ResourceArray]
        //[DebugWatchValue]
        private NativeArray<int> m_FreeProperties;

        //[DebugWatchDeps]
        //private JobHandle m_WriteDependencies;

        private JobHandle m_ReadDependencies;

        //private int m_LastCompanyDemand;

        //private int m_LastBuildingDemand;

       

        //[DebugWatchValue(color = "#008fff")]
        //public int companyDemand => m_LastCompanyDemand;

        //[DebugWatchValue(color = "#2b6795")]
        //public int buildingDemand => m_LastBuildingDemand;

        // InfoLoom

        [DebugWatchValue]
        public int BaseConsumptionSum => m_ResourceSystem.BaseConsumptionSum;





        

        public NativeArray<int> m_Results;
        public NativeValue<Resource> m_ExcludedResources;

        // COMMERCIAL
        // 0 - free properties
        // 1 - propertyless companies
        // 2 - tax rate
        // 3 & 4 - service utilization rate (available/maximum), non-leisure/leisure
        // 5 & 6 - sales efficiency (sales capacity/consumption), non-leisure/leisure // how effectively a shop is utilizing its sales capacity by comparing the actual sales to the maximum sales potential
        // 7 - employee capacity ratio // how efficiently the company is utilizing its workforce by comparing the actual number of employees to the maximum number it could employ
        // 8 & 9 - educated & uneducated workforce

        // 240209 Set gameMode to avoid errors in the Editor
        

       

        // ZoneSpawnSystem and CommercialSpawnSystem are using this
        // Both are called in SystemUpdatePhase.GameSimulation
        // so in UIUpdate they should already be finished (because of EndFrameBarrier)
        public void AddReader(JobHandle reader)
        {
            m_ReadDependencies = JobHandle.CombineDependencies(m_ReadDependencies, reader);
        }

        //[Preserve]
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
            //m_CompanyDemand = new NativeValue<int>(Allocator.Persistent);
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

        //[Preserve]
        protected override void OnDestroy()
        {
            //m_CompanyDemand.Dispose();
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

        public void SetDefaults() //Context context)
        {
            //m_CompanyDemand.value = 0;
            m_BuildingDemand.value = 50; // Infixo: default is 0 which is no demand, let's start with some demand
            m_DemandFactors.Fill(0);
            m_ResourceDemands.Fill(0);
            m_BuildingDemands.Fill(0);
            m_Consumption.Fill(0);
            m_FreeProperties.Fill(0);
            //m_LastCompanyDemand = 0;
            //m_LastBuildingDemand = 0;

        }

        

        //[Preserve]
        protected override void OnUpdate()
        {
            if (m_SimulationSystem.frameIndex % 128 != 55)
                return;
            //Plugin.Log($"OnUpdate: {m_SimulationSystem.frameIndex}");
            
            ResetResults();

            if (!m_DemandParameterQuery.IsEmptyIgnoreFilter && !m_EconomyParameterQuery.IsEmptyIgnoreFilter)
            {
                //m_LastCompanyDemand = m_CompanyDemand.value;
                //m_LastBuildingDemand = m_BuildingDemand.value;
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
                //updateCommercialDemandJob.m_CompanyDemand = m_CompanyDemand;
                updateCommercialDemandJob.m_BuildingDemand = m_BuildingDemand;
                updateCommercialDemandJob.m_DemandFactors = m_DemandFactors;
                updateCommercialDemandJob.m_ResourceDemands = m_ResourceDemands;
                updateCommercialDemandJob.m_BuildingDemands = m_BuildingDemands;
                updateCommercialDemandJob.m_ProduceCapacity = commercialCompanyDatas.m_ProduceCapacity;
                updateCommercialDemandJob.m_CurrentAvailables = commercialCompanyDatas.m_CurrentAvailables;
                updateCommercialDemandJob.m_ResourceNeeds = m_CountHouseholdDataSystem.GetResourceNeeds();
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
                // since this is a copy of an actual simulation system but for UI purposes, then noone will read from us or wait for us
                //m_WriteDependencies = base.Dependency;
                //m_CountConsumptionSystem.AddConsumptionWriter(base.Dependency);
                //m_ResourceSystem.AddPrefabsReader(base.Dependency);
                //m_CountEmploymentSystem.AddReader(base.Dependency);
                //m_CountFreeWorkplacesSystem.AddReader(base.Dependency);
                //m_TaxSystem.AddReader(base.Dependency);
            }

            // Update UI
            
        }

        private void ResetResults()
        {
            m_ExcludedResources.value = Resource.NoResource;
            m_Results.Fill<int>(0);
            //for (int i = 0; i < m_Results.Length; i++) // there are 5 education levels + 1 for totals
            //{
            //m_Results[i] = 0; // new WorkforceAtLevelInfo(i);
            //}
        }

        

        //[Preserve]
        public CommercialSystem()
        {
        }
    }
}


