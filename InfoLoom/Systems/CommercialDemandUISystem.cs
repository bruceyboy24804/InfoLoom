using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Game.Simulation;
using Game.UI;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Colossal.UI.Binding;
using System.Collections.Generic;

namespace InfoLoom.Systems
{
    [BurstCompile]
    public partial class CommercialDemandUISystem : UISystemBase
    {
        [BurstCompile]
        private struct UpdateCommercialDemandJob : IJob
        {
            // Input arrays and lookups
            [ReadOnly] public NativeList<ArchetypeChunk> FreePropertyChunks;
            [ReadOnly] public EntityTypeHandle EntityType;
            [ReadOnly] public ComponentTypeHandle<PrefabRef> PrefabType;
            [ReadOnly] public BufferTypeHandle<Renter> RenterType;
            [ReadOnly] public ComponentLookup<BuildingPropertyData> BuildingPropertyDatas;
            [ReadOnly] public ComponentLookup<ResourceData> ResourceDatas;
            [ReadOnly] public ComponentLookup<CommercialCompany> CommercialCompanies;
            [ReadOnly] public ResourcePrefabs ResourcePrefabs;
            [ReadOnly] public NativeArray<int> TaxRates;
            [ReadOnly] public NativeArray<int> ResourceNeeds;
            [ReadOnly] public NativeArray<int> ProduceCapacity;
            [ReadOnly] public NativeArray<int> CurrentAvailables;
            [ReadOnly] public NativeArray<int> Propertyless;

            // Parameters
            public DemandParameterData DemandParameters;

            // Output values
            public NativeValue<int> CompanyDemand;
            public NativeValue<int> BuildingDemand;
            public NativeArray<int> DemandFactors;
            public NativeArray<int> FreeProperties;
            public NativeArray<int> ResourceDemands;
            public NativeArray<int> BuildingDemands;

            // UI-specific variables
            public NativeArray<int> Results;
            public NativeValue<Resource> ExcludedResources;

            public void Execute()
            {
                // Initialize arrays
                ResourceIterator iterator = ResourceIterator.GetIterator();
                while (iterator.Next())
                {
                    int resourceIndex = EconomyUtils.GetResourceIndex(iterator.resource);
                    FreeProperties[resourceIndex] = 0;
                    BuildingDemands[resourceIndex] = 0;
                    ResourceDemands[resourceIndex] = 0;
                }

                DemandFactors.Clear();
                Results.Fill(0);

                // Count free properties
                for (int i = 0; i < FreePropertyChunks.Length; i++)
                {
                    ArchetypeChunk chunk = FreePropertyChunks[i];
                    NativeArray<PrefabRef> prefabArray = chunk.GetNativeArray(ref PrefabType);
                    BufferAccessor<Renter> renterBuffers = chunk.GetBufferAccessor(ref RenterType);

                    for (int j = 0; j < prefabArray.Length; j++)
                    {
                        Entity prefab = prefabArray[j].m_Prefab;

                        if (!BuildingPropertyDatas.HasComponent(prefab))
                            continue;

                        DynamicBuffer<Renter> renters = renterBuffers[j];
                        bool hasCommercialRenter = false;

                        for (int k = 0; k < renters.Length; k++)
                        {
                            if (CommercialCompanies.HasComponent(renters[k].m_Renter))
                            {
                                hasCommercialRenter = true;
                                break;
                            }
                        }

                        if (hasCommercialRenter)
                            continue;

                        BuildingPropertyData propertyData = BuildingPropertyDatas[prefab];
                        ResourceIterator resourceIterator = ResourceIterator.GetIterator();

                        while (resourceIterator.Next())
                        {
                            Resource resource = resourceIterator.resource;
                            if ((propertyData.m_AllowedSold & resource) != Resource.NoResource)
                            {
                                int resourceIndex = EconomyUtils.GetResourceIndex(resource);
                                FreeProperties[resourceIndex]++;
                            }
                        }

                        if (propertyData.m_AllowedSold != Resource.NoResource)
                        {
                            Results[0]++; // Increment free properties count for UI
                        }
                    }
                }

                CompanyDemand.Value = 0;
                BuildingDemand.Value = 0;

                // Calculate resource demands
                iterator = ResourceIterator.GetIterator();
                int numResources = 0;
                int numDemandedResources = 0;

                float totalTaxRate = 0f;
                int numStandardResources = 0;
                int numLeisureResources = 0;
                float capUtilizationStandard = 0f;
                float capUtilizationLeisure = 0f;
                float salesEfficiencyStandard = 0f;
                float salesEfficiencyLeisure = 0f;

                while (iterator.Next())
                {
                    Resource resource = iterator.resource;
                    int resourceIndex = EconomyUtils.GetResourceIndex(resource);

                    if (!EconomyUtils.IsCommercialResource(resource) || !ResourceDatas.HasComponent(ResourcePrefabs[resource]))
                        continue;

                    float taxEffect = -0.05f * (TaxSystem.GetCommercialTaxRate(resource, TaxRates) - 10f);
                    int resourceNeed = (ResourceNeeds[resourceIndex] == 0 && resource != Resource.Lodging) ? 100 : ResourceNeeds[resourceIndex];
                    int currentAvailable = (CurrentAvailables[resourceIndex] == 0) ? ProduceCapacity[resourceIndex] : CurrentAvailables[resourceIndex];

                    ResourceDemands[resourceIndex] = Mathf.RoundToInt((1f + taxEffect) * math.clamp(math.max(DemandParameters.m_CommercialBaseDemand * resourceNeed - currentAvailable, 0f), 0f, 100f));

                    if (ResourceDemands[resourceIndex] > 0)
                    {
                        CompanyDemand.Value += ResourceDemands[resourceIndex];
                        BuildingDemands[resourceIndex] = (FreeProperties[resourceIndex] - Propertyless[resourceIndex] <= 0) ? ResourceDemands[resourceIndex] : 0;

                        if (BuildingDemands[resourceIndex] > 0)
                        {
                            BuildingDemand.Value += BuildingDemands[resourceIndex];
                        }

                        int demandFactor = ResourceDemands[resourceIndex];
                        int taxFactor = Mathf.RoundToInt(100f * taxEffect);
                        int totalFactor = demandFactor + taxFactor;

                        if (resource == Resource.Lodging)
                        {
                            DemandFactors[(int)DemandFactor.TouristDemand] += demandFactor;
                        }
                        else if (resource == Resource.Petrochemicals)
                        {
                            DemandFactors[(int)DemandFactor.PetrolLocalDemand] += demandFactor;
                        }
                        else
                        {
                            DemandFactors[(int)DemandFactor.LocalDemand] += demandFactor;
                        }

                        DemandFactors[(int)DemandFactor.Taxes] += taxFactor;
                        DemandFactors[(int)DemandFactor.EmptyBuildings] += math.min(0, BuildingDemands[resourceIndex] - totalFactor);

                        // UI-specific calculations
                        totalTaxRate += TaxSystem.GetCommercialTaxRate(resource, TaxRates);

                        ResourceData resourceData = ResourceDatas[ResourcePrefabs[resource]];

                        float capacityUtilization = (ProduceCapacity[resourceIndex] == 0) ? 0.3f : 1f - (float)CurrentAvailables[resourceIndex] / ProduceCapacity[resourceIndex];
                        float salesEfficiency = (float)ProduceCapacity[resourceIndex] / (DemandParameters.m_CommercialBaseDemand * math.max(100f, resourceNeed));

                        if (resourceData.m_IsLeisure)
                        {
                            numLeisureResources++;
                            capUtilizationLeisure += capacityUtilization;
                            salesEfficiencyLeisure += salesEfficiency;
                        }
                        else
                        {
                            numStandardResources++;
                            capUtilizationStandard += capacityUtilization;
                            salesEfficiencyStandard += salesEfficiency;
                        }

                        numDemandedResources++;
                    }
                    else
                    {
                        ExcludedResources.Value |= resource;
                    }

                    numResources++;
                }

                CompanyDemand.Value = (numResources != 0) ? math.clamp(CompanyDemand.Value / numResources, 0, 100) : 0;
                BuildingDemand.Value = (numResources != 0) ? math.clamp(BuildingDemand.Value / numResources, 0, 100) : 0;

                // Update UI Results
                Results[2] = (numDemandedResources > 0) ? Mathf.RoundToInt(10f * totalTaxRate / numDemandedResources) : 0; // Average tax rate

                // Capacity utilization rates
                Results[3] = (numStandardResources > 0) ? Mathf.RoundToInt(100f * capUtilizationStandard / numStandardResources) : 0;
                Results[4] = (numLeisureResources > 0) ? Mathf.RoundToInt(100f * capUtilizationLeisure / numLeisureResources) : 0;

                // Sales efficiency
                Results[5] = (numStandardResources > 0) ? Mathf.RoundToInt(100f * salesEfficiencyStandard / numStandardResources) : 0;
                Results[6] = (numLeisureResources > 0) ? Mathf.RoundToInt(100f * salesEfficiencyLeisure / numLeisureResources) : 0;

                // Employee capacity ratio (Placeholder as we don't have worker data in this simplified job)
                Results[7] = 0;

                // Available workforce (Placeholder as we don't have workforce data in this simplified job)
                Results[8] = 0; // Educated workforce
                Results[9] = 0; // Uneducated workforce

                // Results[0] and Results[1] were updated earlier
            }
        }

        // System variables
        private SimulationSystem m_SimulationSystem;
        private ResourceSystem m_ResourceSystem;
        private TaxSystem m_TaxSystem;
        private CountCompanyDataSystem m_CountCompanyDataSystem;
        private CountHouseholdDataSystem m_CountHouseholdDataSystem;

        private EntityQuery m_DemandParameterQuery;
        private EntityQuery m_FreeCommercialQuery;

        private NativeValue<int> m_CompanyDemand;
        private NativeValue<int> m_BuildingDemand;
        private NativeArray<int> m_DemandFactors;
        private NativeArray<int> m_ResourceDemands;
        private NativeArray<int> m_BuildingDemands;
        private NativeArray<int> m_FreeProperties;

        // UI-specific variables
        private RawValueBinding m_uiResults;
        private RawValueBinding m_uiExResources;

        private NativeArray<int> m_Results;
        private NativeValue<Resource> m_ExcludedResources;

        private TypeHandle __TypeHandle;

        // Constants
        private const string kGroup = "cityInfo";

        public override GameMode gameMode => GameMode.Game;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
            m_ResourceSystem = World.GetOrCreateSystemManaged<ResourceSystem>();
            m_TaxSystem = World.GetOrCreateSystemManaged<TaxSystem>();
            m_CountCompanyDataSystem = World.GetOrCreateSystemManaged<CountCompanyDataSystem>();
            m_CountHouseholdDataSystem = World.GetOrCreateSystemManaged<CountHouseholdDataSystem>();

            m_DemandParameterQuery = GetEntityQuery(ComponentType.ReadOnly<DemandParameterData>());
            m_FreeCommercialQuery = GetEntityQuery(
                ComponentType.ReadOnly<CommercialProperty>(),
                ComponentType.ReadOnly<PropertyOnMarket>(),
                ComponentType.ReadOnly<PrefabRef>(),
                ComponentType.Exclude<Abandoned>(),
                ComponentType.Exclude<Destroyed>(),
                ComponentType.Exclude<Deleted>(),
                ComponentType.Exclude<Condemned>(),
                ComponentType.Exclude<Temp>()
            );

            m_CompanyDemand = new NativeValue<int>(Allocator.Persistent);
            m_BuildingDemand = new NativeValue<int>(Allocator.Persistent);
            m_DemandFactors = new NativeArray<int>(18, Allocator.Persistent);

            int resourceCount = EconomyUtils.ResourceCount;
            m_ResourceDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
            m_BuildingDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
            m_FreeProperties = new NativeArray<int>(resourceCount, Allocator.Persistent);

            // UI-specific initializations
            m_Results = new NativeArray<int>(10, Allocator.Persistent);
            m_ExcludedResources = new NativeValue<Resource>(Allocator.Persistent);

            // Set default values
            SetDefaults();

            // UI bindings
            AddBinding(m_uiResults = new RawValueBinding(kGroup, "ilCommercial", binder =>
            {
                binder.ArrayBegin(m_Results.Length);
                for (int i = 0; i < m_Results.Length; i++)
                {
                    binder.Write(m_Results[i]);
                }
                binder.ArrayEnd();
            }));

            AddBinding(m_uiExResources = new RawValueBinding(kGroup, "ilCommercialExRes", binder =>
            {
                List<string> resList = new List<string>();
                for (int i = 0; i < EconomyUtils.ResourceCount; i++)
                {
                    if ((m_ExcludedResources.Value & EconomyUtils.GetResource(i)) != Resource.NoResource)
                    {
                        resList.Add(EconomyUtils.GetName(EconomyUtils.GetResource(i)));
                    }
                }
                binder.ArrayBegin(resList.Count);
                foreach (string res in resList)
                {
                    binder.Write(res);
                }
                binder.ArrayEnd();
            }));

            Mod.log.Info("CommercialDemandUISystem created.");
        }

        protected override void OnDestroy()
        {
            m_CompanyDemand.Dispose();
            m_BuildingDemand.Dispose();
            m_DemandFactors.Dispose();
            m_ResourceDemands.Dispose();
            m_BuildingDemands.Dispose();
            m_FreeProperties.Dispose();

            // UI-specific disposals
            m_Results.Dispose();
            m_ExcludedResources.Dispose();

            base.OnDestroy();
        }

        private void SetDefaults()
        {
            m_CompanyDemand.Value = 0;
            m_BuildingDemand.Value = 50; // Start with some demand
            m_DemandFactors.Fill(0);
            m_ResourceDemands.Fill(0);
            m_BuildingDemands.Fill(0);
            m_FreeProperties.Fill(0);
            m_Results.Fill(0);
            m_ExcludedResources.Value = Resource.NoResource;
        }

        public void AddReader(JobHandle reader)
        {
            // Method to manage dependencies if needed
        }

        protected override void OnUpdate()
        {
            // Control update frequency
            if (m_SimulationSystem.frameIndex % 128 != 55)
                return;

            base.OnUpdate();
            ResetResults();

            if (!m_DemandParameterQuery.IsEmptyIgnoreFilter)
            {
                JobHandle companyDataDeps;
                var commercialCompanyDatas = m_CountCompanyDataSystem.GetCommercialCompanyDatas(out companyDataDeps);

                __TypeHandle.Update(ref base.CheckedStateRef);

                var freePropertyJobHandle = new JobHandle();
                var freePropertyChunks = m_FreeCommercialQuery.CreateArchetypeChunkArrayAsync(Allocator.TempJob, out freePropertyJobHandle);

                var updateJob = new UpdateCommercialDemandJob
                {
                    FreePropertyChunks = freePropertyChunks,
                    EntityType = __TypeHandle.EntityTypeHandle,
                    PrefabType = __TypeHandle.PrefabRefTypeHandle,
                    RenterType = __TypeHandle.RenterBufferTypeHandle,
                    BuildingPropertyDatas = __TypeHandle.BuildingPropertyDataLookup,
                    ResourceDatas = __TypeHandle.ResourceDataLookup,
                    CommercialCompanies = __TypeHandle.CommercialCompanyLookup,
                    ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
                    DemandParameters = m_DemandParameterQuery.GetSingleton<DemandParameterData>(),
                    TaxRates = m_TaxSystem.GetTaxRates(),
                    CompanyDemand = m_CompanyDemand,
                    BuildingDemand = m_BuildingDemand,
                    DemandFactors = m_DemandFactors,
                    ResourceDemands = m_ResourceDemands,
                    BuildingDemands = m_BuildingDemands,
                    FreeProperties = m_FreeProperties,
                    ResourceNeeds = m_CountHouseholdDataSystem.GetResourceNeeds(),
                    ProduceCapacity = commercialCompanyDatas.m_ProduceCapacity,
                    CurrentAvailables = commercialCompanyDatas.m_CurrentAvailables,
                    Propertyless = commercialCompanyDatas.m_ServicePropertyless,

                    // UI-specific variables
                    Results = m_Results,
                    ExcludedResources = m_ExcludedResources
                };

                var jobHandle = updateJob.Schedule(JobHandle.CombineDependencies(freePropertyJobHandle, companyDataDeps));
                jobHandle.Complete();

                // Dispose temporary arrays
                freePropertyChunks.Dispose();

                // Update UI bindings
                m_uiResults.Update();
                m_uiExResources.Update();
            }
        }

        private void ResetResults()
        {
            m_ExcludedResources.Value = Resource.NoResource;
            m_Results.Fill(0);
        }

        private struct TypeHandle
        {
            [ReadOnly] public EntityTypeHandle EntityTypeHandle;
            [ReadOnly] public ComponentTypeHandle<PrefabRef> PrefabRefTypeHandle;
            [ReadOnly] public BufferTypeHandle<Renter> RenterBufferTypeHandle;
            [ReadOnly] public ComponentLookup<BuildingPropertyData> BuildingPropertyDataLookup;
            [ReadOnly] public ComponentLookup<ResourceData> ResourceDataLookup;
            [ReadOnly] public ComponentLookup<CommercialCompany> CommercialCompanyLookup;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Update(ref SystemState state)
            {
                EntityTypeHandle = state.GetEntityTypeHandle();
                PrefabRefTypeHandle = state.GetComponentTypeHandle<PrefabRef>(true);
                RenterBufferTypeHandle = state.GetBufferTypeHandle<Renter>(true);
                BuildingPropertyDataLookup = state.GetComponentLookup<BuildingPropertyData>(true);
                ResourceDataLookup = state.GetComponentLookup<ResourceData>(true);
                CommercialCompanyLookup = state.GetComponentLookup<CommercialCompany>(true);
            }
        }

        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            __TypeHandle.Update(ref base.CheckedStateRef);
        }

        public CommercialDemandUISystem()
        {
        }
    }
}
