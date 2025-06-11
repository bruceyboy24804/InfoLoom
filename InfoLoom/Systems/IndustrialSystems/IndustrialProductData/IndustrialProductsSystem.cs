using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Logging;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Debug;
using Game.Economy;
using Game.Objects;
using Game.Prefabs;
using Game.Reflection;
using Game.Simulation;
using Game.Tools;
using Game.UI;
using Game.Zones;
using InfoLoomTwo.Utils;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace InfoLoomTwo.Systems.IndustrialSystems.IndustrialProductData
{
    public struct IndustrialProductDTO
    {
        public string ResourceName;
        public string ResourceIcon;
        public int Demand;
        public int Building;
        public int Free;
        public int Companies;
        public int Workers;
        public int SvcPercent;
        public int CapPercent;
        public int CapPerCompany;
        public int WrkPercent;
        public int TaxFactor;
        public int ResourceNeeds;
        public int Production;
        public int Storages;
        public int FreeStorages;
        public int StorageCapacities;
        public int MaxProductionWorkers;
        public int CurrentProductionWorkers;
    }

    // Burst-compatible struct for industrial demand data
    public struct IndustrialDemandJobData : IComponentData
    {
        public Resource ResourceType;
        public FixedString32Bytes ResourceName;
        public int Demand;
        public int Building;
        public int Free;
        public int Companies;
        public int Workers;
        public int SvcPercent;
        public int CapPercent;
        public int CapPerCompany;
        public int WrkPercent;
        public int TaxFactor;
        public int ResourceNeeds;
        public int Production;
        public int Storages;
        public int FreeStorages;
        public int StorageCapacities;
        public int MaxProductionWorkers;
        public int CurrentProductionWorkers;
    }

    // Burst-compiled job for processing industrial demand
    [BurstCompile]
    public struct ProcessIndustrialDemandJob : IJob
    {
        private static readonly int kStorageProductionDemand = 2000;
        private static readonly int kStorageCompanyEstimateLimit = 864000;

        // Zone and building data
        [DeallocateOnJobCompletion]
        [ReadOnly]
        public NativeArray<ZoneData> m_UnlockedZoneDatas;

        [ReadOnly]
        public NativeList<ArchetypeChunk> m_FreePropertyChunks;

        [ReadOnly]
        public NativeList<ArchetypeChunk> m_StorageCompanyChunks;

        [ReadOnly]
        public NativeList<ArchetypeChunk> m_CityServiceChunks;

        // Type handles
        [ReadOnly]
        public EntityTypeHandle m_EntityType;

        [ReadOnly]
        public ComponentTypeHandle<PrefabRef> m_PrefabType;

        [ReadOnly]
        public ComponentTypeHandle<CityServiceUpkeep> m_ServiceUpkeepType;

        // Component lookups
        [ReadOnly]
        public ComponentLookup<IndustrialProcessData> m_IndustrialProcessDatas;

        [ReadOnly]
        public ComponentLookup<PropertyRenter> m_PropertyRenters;

        [ReadOnly]
        public ComponentLookup<PrefabRef> m_Prefabs;

        [ReadOnly]
        public ComponentLookup<BuildingData> m_BuildingDatas;

        [ReadOnly]
        public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDatas;

        [ReadOnly]
        public ComponentLookup<Attached> m_Attached;

        [ReadOnly]
        public ComponentLookup<ResourceData> m_ResourceDatas;

        [ReadOnly]
        public ComponentLookup<StorageLimitData> m_StorageLimitDatas;

        [ReadOnly]
        public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingDatas;

        // Buffer lookups
        [ReadOnly]
        public BufferLookup<ServiceUpkeepData> m_ServiceUpkeeps;

        [ReadOnly]
        public BufferLookup<CityModifier> m_CityModifiers;

        [ReadOnly]
        public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

        [ReadOnly]
        public BufferLookup<ServiceUpkeepData> m_Upkeeps;

        // System data
        [ReadOnly]
        public EconomyParameterData m_EconomyParameters;

        [ReadOnly]
        public DemandParameterData m_DemandParameters;

        [ReadOnly]
        public ResourcePrefabs m_ResourcePrefabs;

        [ReadOnly]
        public NativeArray<int> m_EmployableByEducation;

        [ReadOnly]
        public NativeArray<int> m_TaxRates;

        [ReadOnly]
        public Workplaces m_FreeWorkplaces;

        [ReadOnly]
        public Entity m_City;

        // Industrial company data
        [ReadOnly]
        public NativeArray<int> m_Productions;

        [ReadOnly]
        public NativeArray<int> m_CompanyResourceDemands;

        [ReadOnly]
        public NativeArray<int> m_Propertyless;

        [ReadOnly]
        public NativeArray<int> m_CurrentProductionWorkers;

        [ReadOnly]
        public NativeArray<int> m_MaxProductionWorkers;

        [ReadOnly]
        public NativeArray<int> m_ProductionCompanies;

        // Output data
        public NativeValue<int> m_IndustrialCompanyDemand;
        public NativeValue<int> m_IndustrialBuildingDemand;
        public NativeValue<int> m_StorageCompanyDemand;
        public NativeValue<int> m_StorageBuildingDemand;
        public NativeValue<int> m_OfficeCompanyDemand;
        public NativeValue<int> m_OfficeBuildingDemand;

        public NativeArray<int> m_IndustrialDemandFactors;
        public NativeArray<int> m_OfficeDemandFactors;
        public NativeArray<int> m_IndustrialCompanyDemands;
        public NativeArray<int> m_IndustrialBuildingDemands;
        public NativeArray<int> m_StorageBuildingDemands;
        public NativeArray<int> m_StorageCompanyDemands;
        public NativeArray<int> m_ResourceDemands;
        public NativeArray<int> m_FreeProperties;
        public NativeArray<int> m_FreeStorages;
        public NativeArray<int> m_Storages;
        public NativeArray<int> m_StorageCapacities;

        public NativeList<IndustrialDemandJobData> m_DemandJobResults;

        [BurstCompile]
        public void Execute()
        {
            // Check if industrial zones are unlocked
            bool industrialZonesUnlocked = false;
            for (int i = 0; i < m_UnlockedZoneDatas.Length; i++)
            {
                if (m_UnlockedZoneDatas[i].m_AreaType == AreaType.Industrial)
                {
                    industrialZonesUnlocked = true;
                    break;
                }
            }

            // Initialize arrays
            InitializeArrays();

            // Process city services
            ProcessCityServices();

            // Process storage companies
            ProcessStorageCompanies();

            // Process free properties
            ProcessFreeProperties();

            // Process resource demands
            ProcessResourceDemands(industrialZonesUnlocked);
        }

        [BurstCompile]
        private void InitializeArrays()
        {
            DynamicBuffer<CityModifier> modifiers = m_CityModifiers[m_City];
            ResourceIterator iterator = ResourceIterator.GetIterator();
            
            while (iterator.Next())
            {
                int resourceIndex = EconomyUtils.GetResourceIndex(iterator.resource);
                ResourceData resourceData = m_ResourceDatas[m_ResourcePrefabs[iterator.resource]];
                
                m_ResourceDemands[resourceIndex] = ((m_CompanyResourceDemands[resourceIndex] == 0 && 
                    EconomyUtils.IsIndustrialResource(resourceData, includeMaterial: false, includeOffice: false)) ? 
                    100 : m_CompanyResourceDemands[resourceIndex]);
                
                m_FreeProperties[resourceIndex] = 0;
                m_Storages[resourceIndex] = 0;
                m_FreeStorages[resourceIndex] = 0;
                m_StorageCapacities[resourceIndex] = 0;
            }

            for (int j = 0; j < m_IndustrialDemandFactors.Length; j++)
            {
                m_IndustrialDemandFactors[j] = 0;
            }

            for (int k = 0; k < m_OfficeDemandFactors.Length; k++)
            {
                m_OfficeDemandFactors[k] = 0;
            }
        }

        [BurstCompile]
        private void ProcessCityServices()
        {
            for (int l = 0; l < m_CityServiceChunks.Length; l++)
            {
                ArchetypeChunk archetypeChunk = m_CityServiceChunks[l];
                if (!archetypeChunk.Has(ref m_ServiceUpkeepType))
                {
                    continue;
                }

                NativeArray<Entity> entities = archetypeChunk.GetNativeArray(m_EntityType);
                NativeArray<PrefabRef> prefabRefs = archetypeChunk.GetNativeArray(ref m_PrefabType);

                for (int m = 0; m < prefabRefs.Length; m++)
                {
                    Entity prefab = prefabRefs[m].m_Prefab;
                    Entity entity = entities[m];

                    if (m_ServiceUpkeeps.HasBuffer(prefab))
                    {
                        DynamicBuffer<ServiceUpkeepData> upkeeps = m_ServiceUpkeeps[prefab];
                        for (int n = 0; n < upkeeps.Length; n++)
                        {
                            ServiceUpkeepData upkeep = upkeeps[n];
                            if (upkeep.m_Upkeep.m_Resource != Resource.Money)
                            {
                                int amount = upkeep.m_Upkeep.m_Amount;
                                m_ResourceDemands[EconomyUtils.GetResourceIndex(upkeep.m_Upkeep.m_Resource)] += amount;
                            }
                        }
                    }

                    if (!m_InstalledUpgrades.HasBuffer(entity))
                    {
                        continue;
                    }

                    DynamicBuffer<InstalledUpgrade> upgrades = m_InstalledUpgrades[entity];
                    for (int num = 0; num < upgrades.Length; num++)
                    {
                        Entity upgrade = upgrades[num].m_Upgrade;
                        if (BuildingUtils.CheckOption(upgrades[num], BuildingOption.Inactive) || 
                            !m_Prefabs.HasComponent(upgrade))
                        {
                            continue;
                        }

                        Entity upgradePrefab = m_Prefabs[upgrade].m_Prefab;
                        if (m_Upkeeps.HasBuffer(upgradePrefab))
                        {
                            DynamicBuffer<ServiceUpkeepData> upgradeUpkeeps = m_Upkeeps[upgradePrefab];
                            for (int num2 = 0; num2 < upgradeUpkeeps.Length; num2++)
                            {
                                ServiceUpkeepData upgradeUpkeep = upgradeUpkeeps[num2];
                                m_ResourceDemands[EconomyUtils.GetResourceIndex(upgradeUpkeep.m_Upkeep.m_Resource)] += 
                                    upgradeUpkeep.m_Upkeep.m_Amount;
                            }
                        }
                    }
                }
            }
        }

        [BurstCompile]
        private void ProcessStorageCompanies()
        {
            for (int i = 0; i < m_StorageCompanyChunks.Length; i++)
            {
                ArchetypeChunk chunk = m_StorageCompanyChunks[i];
                NativeArray<Entity> entities = chunk.GetNativeArray(m_EntityType);
                NativeArray<PrefabRef> prefabRefs = chunk.GetNativeArray(ref m_PrefabType);

                for (int j = 0; j < entities.Length; j++)
                {
                    Entity entity = entities[j];
                    Entity prefab = prefabRefs[j].m_Prefab;

                    if (m_IndustrialProcessDatas.HasComponent(prefab))
                    {
                        int resourceIndex = EconomyUtils.GetResourceIndex(
                            m_IndustrialProcessDatas[prefab].m_Output.m_Resource);
                        m_Storages[resourceIndex]++;

                        StorageLimitData storageLimitData = m_StorageLimitDatas[prefab];
                        if (!m_PropertyRenters.HasComponent(entity) || 
                            !m_Prefabs.HasComponent(m_PropertyRenters[entity].m_Property))
                        {
                            m_FreeStorages[resourceIndex]--;
                            m_StorageCapacities[resourceIndex] += kStorageCompanyEstimateLimit;
                        }
                        else
                        {
                            Entity property = m_PropertyRenters[entity].m_Property;
                            Entity propertyPrefab = m_Prefabs[property].m_Prefab;
                            m_StorageCapacities[resourceIndex] += storageLimitData.GetAdjustedLimitForWarehouse(
                                m_SpawnableBuildingDatas[propertyPrefab], m_BuildingDatas[propertyPrefab]);
                        }
                    }
                }
            }
        }

        [BurstCompile]
        private void ProcessFreeProperties()
        {
            for (int i = 0; i < m_FreePropertyChunks.Length; i++)
            {
                ArchetypeChunk chunk = m_FreePropertyChunks[i];
                NativeArray<Entity> entities = chunk.GetNativeArray(m_EntityType);
                NativeArray<PrefabRef> prefabRefs = chunk.GetNativeArray(ref m_PrefabType);

                for (int j = 0; j < prefabRefs.Length; j++)
                {
                    Entity prefab = prefabRefs[j].m_Prefab;
                    if (!m_BuildingPropertyDatas.HasComponent(prefab))
                    {
                        continue;
                    }

                    BuildingPropertyData buildingPropertyData = m_BuildingPropertyDatas[prefab];
                    if (m_Attached.TryGetComponent(entities[j], out var attached) && 
                        m_Prefabs.TryGetComponent(attached.m_Parent, out var parentPrefab) && 
                        m_BuildingPropertyDatas.TryGetComponent(parentPrefab.m_Prefab, out var parentProperty))
                    {
                        buildingPropertyData.m_AllowedManufactured &= parentProperty.m_AllowedManufactured;
                    }

                    ResourceIterator iterator = ResourceIterator.GetIterator();
                    while (iterator.Next())
                    {
                        int resourceIndex = EconomyUtils.GetResourceIndex(iterator.resource);
                        if ((buildingPropertyData.m_AllowedManufactured & iterator.resource) != Resource.NoResource)
                        {
                            m_FreeProperties[resourceIndex]++;
                        }
                        if ((buildingPropertyData.m_AllowedStored & iterator.resource) != Resource.NoResource)
                        {
                            m_FreeStorages[resourceIndex]++;
                        }
                    }
                }
            }
        }

        [BurstCompile]
        private void ProcessResourceDemands(bool industrialZonesUnlocked)
        {
            // Calculate office resource needs
            int totalProduction = 0;
            int materialProduction = 0;
            int officeProduction = 0;
            
            for (int i = 0; i < m_Productions.Length; i++)
            {
                Resource resource = EconomyUtils.GetResource(i);
                ResourceData resourceData = m_ResourceDatas[m_ResourcePrefabs[resource]];
                if (resourceData.m_IsProduceable)
                {
                    if (resourceData.m_Weight > 0f)
                    {
                        materialProduction += m_Productions[i];
                    }
                    else
                    {
                        officeProduction += m_Productions[i];
                    }
                }
            }

            totalProduction = officeProduction + materialProduction;
            
            // Add office resource demands
            m_ResourceDemands[EconomyUtils.GetResourceIndex(Resource.Software)] += 
                totalProduction / m_EconomyParameters.m_PerOfficeResourceNeededForIndustrial;
            m_ResourceDemands[EconomyUtils.GetResourceIndex(Resource.Financial)] += 
                totalProduction / m_EconomyParameters.m_PerOfficeResourceNeededForIndustrial;
            m_ResourceDemands[EconomyUtils.GetResourceIndex(Resource.Telecom)] += 
                totalProduction / m_EconomyParameters.m_PerOfficeResourceNeededForIndustrial;

            // Reset demand values
            m_IndustrialCompanyDemand.value = 0;
            m_IndustrialBuildingDemand.value = 0;
            m_StorageCompanyDemand.value = 0;
            m_StorageBuildingDemand.value = 0;
            m_OfficeCompanyDemand.value = 0;
            m_OfficeBuildingDemand.value = 0;

            int validIndustrialCount = 0;
            int validOfficeCount = 0;
            bool previousOfficeBuildingDemand = m_OfficeBuildingDemand.value > 0;

            // Process each resource
            ResourceIterator iterator = ResourceIterator.GetIterator();
            while (iterator.Next())
            {
                if (!ProcessSingleResource(iterator.resource, industrialZonesUnlocked, 
                    previousOfficeBuildingDemand, ref validIndustrialCount, ref validOfficeCount))
                    continue;
            }

            // Final demand calculations
            m_StorageBuildingDemand.value = Mathf.CeilToInt(math.pow(20f * (float)m_StorageBuildingDemand.value, 0.75f));
            m_IndustrialBuildingDemand.value = industrialZonesUnlocked ? 
                (2 * m_IndustrialBuildingDemand.value / math.max(1, validIndustrialCount)) : 0;
            m_OfficeCompanyDemand.value = 2 * m_OfficeCompanyDemand.value / math.max(1, validOfficeCount);
            m_IndustrialBuildingDemand.value = math.clamp(m_IndustrialBuildingDemand.value, 0, 100);
            m_OfficeBuildingDemand.value = math.clamp(m_OfficeBuildingDemand.value, 0, 100);
        }

        [BurstCompile]
        private bool ProcessSingleResource(Resource resource, bool industrialZonesUnlocked, 
            bool previousOfficeBuildingDemand, ref int validIndustrialCount, ref int validOfficeCount)
        {
            int resourceIndex = EconomyUtils.GetResourceIndex(resource);
            
            if (!m_ResourceDatas.HasComponent(m_ResourcePrefabs[resource]))
            {
                return false;
            }

            ResourceData resourceData = m_ResourceDatas[m_ResourcePrefabs[resource]];
            bool isProduceable = resourceData.m_IsProduceable;
            bool isMaterial = resourceData.m_IsMaterial;
            bool isTradable = resourceData.m_IsTradable;
            bool isOffice = resourceData.m_Weight == 0f;

            // Handle storage demands
            if (isTradable && !isOffice)
            {
                ProcessStorageDemand(resourceIndex);
            }

            if (!isProduceable)
            {
                return false;
            }

            // Calculate production demand
            float baseDemand = isMaterial ? m_DemandParameters.m_ExtractorBaseDemand : 
                m_DemandParameters.m_IndustrialBaseDemand;
            float demandRatio = (1f + (float)m_ResourceDemands[resourceIndex] - (float)m_Productions[resourceIndex]) / 
                ((float)m_ResourceDemands[resourceIndex] + 1f);

            // Apply modifiers
            DynamicBuffer<CityModifier> modifiers = m_CityModifiers[m_City];
            if (resource == Resource.Electronics)
            {
                CityUtils.ApplyModifier(ref baseDemand, modifiers, CityModifierType.IndustrialElectronicsDemand);
            }
            else if (resource == Resource.Software)
            {
                CityUtils.ApplyModifier(ref baseDemand, modifiers, CityModifierType.OfficeSoftwareDemand);
            }

            // Calculate tax effects
            int taxRate = isOffice ? TaxSystem.GetOfficeTaxRate(resource, m_TaxRates) : 
                TaxSystem.GetIndustrialTaxRate(resource, m_TaxRates);
            float3 taxEffect = m_DemandParameters.m_TaxEffect * -5f * ((float)taxRate - 10f);

            // Calculate unemployment effects
            int highEducationUnemployment = 0;
            int lowEducationUnemployment = 0;
            float neutralUnemployment = m_DemandParameters.m_NeutralUnemployment / 100f;

            for (int i = 0; i < 5; i++)
            {
                if (i < 2)
                {
                    lowEducationUnemployment += (int)((float)m_EmployableByEducation[i] * (1f - neutralUnemployment)) - 
                        m_FreeWorkplaces[i];
                }
                else
                {
                    highEducationUnemployment += (int)((float)m_EmployableByEducation[i] * (1f - neutralUnemployment)) - 
                        m_FreeWorkplaces[i];
                }
            }

            highEducationUnemployment = math.clamp(highEducationUnemployment, -10, 10);
            lowEducationUnemployment = math.clamp(lowEducationUnemployment, -10, 15);

            float finalDemand = 50f * math.max(0f, baseDemand * demandRatio);

            // Calculate company demand
            if (isOffice)
            {
                m_IndustrialCompanyDemands[resourceIndex] = Mathf.RoundToInt(finalDemand + taxEffect.x + taxEffect.y + taxEffect.z +
                    (float)highEducationUnemployment);
                m_IndustrialCompanyDemands[resourceIndex] = math.clamp(m_IndustrialCompanyDemands[resourceIndex], 0, 100);
                m_OfficeCompanyDemand.value += m_IndustrialCompanyDemands[resourceIndex];
                validOfficeCount++;
            }
            else
            {
                m_IndustrialCompanyDemands[resourceIndex] = Mathf.RoundToInt(finalDemand + taxEffect.x + taxEffect.y + taxEffect.z +
                    (float)highEducationUnemployment + (float)lowEducationUnemployment);
                m_IndustrialCompanyDemands[resourceIndex] = math.clamp(m_IndustrialCompanyDemands[resourceIndex], 0, 100);
                m_IndustrialCompanyDemand.value += m_IndustrialCompanyDemands[resourceIndex];
                if (!isMaterial)
                {
                    validIndustrialCount++;
                }
            }

            // Create job data for UI
            var jobData = new IndustrialDemandJobData
            {
                ResourceType = resource,
                ResourceName = default, // Will be populated outside the job
                Demand = m_ResourceDemands[resourceIndex],
                Building = m_IndustrialCompanyDemands[resourceIndex],
                Free = m_FreeProperties[resourceIndex],
                Companies = m_ProductionCompanies[resourceIndex],
                Workers = m_CurrentProductionWorkers[resourceIndex],
                SvcPercent = m_Storages[resourceIndex],
                CapPercent = m_CompanyResourceDemands[resourceIndex],
                CapPerCompany = m_Productions[resourceIndex],
                WrkPercent = 100 * (m_CurrentProductionWorkers[resourceIndex] + 1) / 
                    (m_MaxProductionWorkers[resourceIndex] + 1),
                TaxFactor = Mathf.RoundToInt(taxEffect.x + taxEffect.y + taxEffect.z), // Use x component here
                ResourceNeeds = m_ResourceDemands[resourceIndex],
                Production = m_Productions[resourceIndex],
                Storages = m_Storages[resourceIndex],
                FreeStorages = m_FreeStorages[resourceIndex],
                StorageCapacities = m_StorageCapacities[resourceIndex],
                MaxProductionWorkers = m_MaxProductionWorkers[resourceIndex],
                CurrentProductionWorkers = m_CurrentProductionWorkers[resourceIndex]
            };

            m_DemandJobResults.Add(jobData);

            // Calculate building demand
            ProcessBuildingDemand(resourceIndex, isMaterial, isOffice, previousOfficeBuildingDemand, 
                finalDemand, taxEffect.x, highEducationUnemployment); // Pass x component here

            return true;
        }

        [BurstCompile]
        private void ProcessStorageDemand(int resourceIndex)
        {
            int resourceDemand = m_ResourceDemands[resourceIndex];
            m_StorageCompanyDemands[resourceIndex] = 0;
            m_StorageBuildingDemands[resourceIndex] = 0;

            if (resourceDemand > kStorageProductionDemand && m_StorageCapacities[resourceIndex] < resourceDemand)
            {
                m_StorageCompanyDemands[resourceIndex] = 1;
            }

            if (m_FreeStorages[resourceIndex] < 0)
            {
                m_StorageBuildingDemands[resourceIndex] = 1;
            }

            m_StorageCompanyDemand.value += m_StorageCompanyDemands[resourceIndex];
            m_StorageBuildingDemand.value += m_StorageBuildingDemands[resourceIndex];
            m_IndustrialDemandFactors[17] += math.max(0, m_StorageBuildingDemands[resourceIndex]);
        }

        [BurstCompile]
        private void ProcessBuildingDemand(int resourceIndex, bool isMaterial, bool isOffice, 
            bool previousOfficeBuildingDemand, float finalDemand, float taxEffect, int highEducationUnemployment)
        {
            if (m_ResourceDemands[resourceIndex] > 0)
            {
                if (!isMaterial && m_IndustrialCompanyDemands[resourceIndex] > 0)
                {
                    m_IndustrialBuildingDemands[resourceIndex] = 
                        (m_FreeProperties[resourceIndex] - m_Propertyless[resourceIndex] <= 0) ? 50 : 0;
                }
                else if (m_IndustrialCompanyDemands[resourceIndex] > 0)
                {
                    m_IndustrialBuildingDemands[resourceIndex] = 1;
                }
                else
                {
                    m_IndustrialBuildingDemands[resourceIndex] = 0;
                }

                if (m_IndustrialBuildingDemands[resourceIndex] > 0)
                {
                    if (isOffice)
                    {
                        m_OfficeBuildingDemand.value += 
                            (m_IndustrialBuildingDemands[resourceIndex] > 0) ? m_IndustrialCompanyDemands[resourceIndex] : 0;
                    }
                    else if (!isMaterial)
                    {
                        m_IndustrialBuildingDemand.value += 
                            (m_IndustrialBuildingDemands[resourceIndex] > 0) ? m_IndustrialCompanyDemands[resourceIndex] : 0;
                    }
                }
            }

            // Update demand factors
            if (!isMaterial)
            {
                if (isOffice)
                {
                    if (!previousOfficeBuildingDemand || 
                        (m_IndustrialBuildingDemands[resourceIndex] > 0 && m_IndustrialCompanyDemands[resourceIndex] > 0))
                    {
                        m_OfficeDemandFactors[2] = highEducationUnemployment;
                        m_OfficeDemandFactors[4] += (int)finalDemand;
                        m_OfficeDemandFactors[11] += (int)taxEffect;
                        m_OfficeDemandFactors[13] += m_IndustrialBuildingDemands[resourceIndex];
                    }
                }
                else
                {
                    m_IndustrialDemandFactors[2] = highEducationUnemployment;
                    m_IndustrialDemandFactors[1] = m_IndustrialDemandFactors[1]; // Low education unemployment
                    m_IndustrialDemandFactors[4] += (int)finalDemand;
                    m_IndustrialDemandFactors[11] += (int)taxEffect;
                    m_IndustrialDemandFactors[13] += m_IndustrialBuildingDemands[resourceIndex];
                }
            }
        }
    }

    public partial class IndustrialProductsSystem : GameSystemBase
    {
        private SimulationSystem m_SimulationSystem;
        private ResourceSystem m_ResourceSystem;
        private ImageSystem m_ImageSystem;
        private CitySystem m_CitySystem;
        private ClimateSystem m_ClimateSystem;
        private TaxSystem m_TaxSystem;
        private CountHouseholdDataSystem m_CountHouseholdDataSystem;
        private CountWorkplacesSystem m_CountWorkplacesSystem;
        private CountCompanyDataSystem m_CountCompanyDataSystem;
        private ILog m_Log;

        private EntityQuery m_EconomyParameterQuery;
        private EntityQuery m_DemandParameterQuery;
        private EntityQuery m_FreeIndustrialQuery;
        private EntityQuery m_StorageCompanyQuery;
        private EntityQuery m_ProcessDataQuery;
        private EntityQuery m_CityServiceQuery;
        private EntityQuery m_UnlockedZoneDataQuery;

        private NativeValue<int> m_IndustrialCompanyDemand;
        private NativeValue<int> m_IndustrialBuildingDemand;
        private NativeValue<int> m_StorageCompanyDemand;
        private NativeValue<int> m_StorageBuildingDemand;
        private NativeValue<int> m_OfficeCompanyDemand;
        private NativeValue<int> m_OfficeBuildingDemand;

        public static NativeArray<IndustrialDemandJobData> m_DemandData;
        public IndustrialProductDTO[] m_IndustrialProductDTOs;

        [EnumArray(typeof(DemandFactor))]
        [DebugWatchValue]
        private NativeArray<int> m_IndustrialDemandFactors;

        [EnumArray(typeof(DemandFactor))]
        [DebugWatchValue]
        private NativeArray<int> m_OfficeDemandFactors;

        [ResourceArray]
        [DebugWatchValue]
        private NativeArray<int> m_ResourceDemands;

        [ResourceArray]
        [DebugWatchValue]
        private NativeArray<int> m_IndustrialCompanyDemands;

        [ResourceArray]
        [DebugWatchValue]
        private NativeArray<int> m_IndustrialBuildingDemands;

        [ResourceArray]
        [DebugWatchValue]
        private NativeArray<int> m_StorageBuildingDemands;

        [ResourceArray]
        [DebugWatchValue]
        private NativeArray<int> m_StorageCompanyDemands;

        [ResourceArray]
        [DebugWatchValue]
        private NativeArray<int> m_FreeProperties;

        [ResourceArray]
        [DebugWatchValue]
        private NativeArray<int> m_FreeStorages;

        [ResourceArray]
        [DebugWatchValue]
        private NativeArray<int> m_Storages;

        [ResourceArray]
        [DebugWatchValue]
        private NativeArray<int> m_StorageCapacities;

        [DebugWatchDeps]
        private JobHandle m_WriteDependencies;

        private JobHandle m_ReadDependencies;

        private int m_LastIndustrialCompanyDemand;
        private int m_LastIndustrialBuildingDemand;
        private int m_LastStorageCompanyDemand;
        private int m_LastStorageBuildingDemand;
        private int m_LastOfficeCompanyDemand;
        private int m_LastOfficeBuildingDemand;

        // Cached lookups for performance
        private Dictionary<Resource, string> m_ResourceNameCache;
        private Dictionary<Resource, string> m_ResourceIconCache;
        private bool m_CacheInitialized;

        [DebugWatchValue(color = "#f7dc6f")]
        public int industrialCompanyDemand => m_LastIndustrialCompanyDemand;

        [DebugWatchValue(color = "#b7950b")]
        public int industrialBuildingDemand => m_LastIndustrialBuildingDemand;

        [DebugWatchValue(color = "#cccccc")]
        public int storageCompanyDemand => m_LastStorageCompanyDemand;

        [DebugWatchValue(color = "#999999")]
        public int storageBuildingDemand => m_LastStorageBuildingDemand;

        [DebugWatchValue(color = "#af7ac5")]
        public int officeCompanyDemand => m_LastOfficeCompanyDemand;

        [DebugWatchValue(color = "#6c3483")]
        public int officeBuildingDemand => m_LastOfficeBuildingDemand;

        public bool IsPanelVisible { get; set; }
        public bool ForceUpdate { get; private set; }

        

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = Mod.log;
            m_IndustrialProductDTOs = Array.Empty<IndustrialProductDTO>();

            // Initialize systems
            m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
            m_ResourceSystem = World.GetOrCreateSystemManaged<ResourceSystem>();
            m_ImageSystem = World.GetOrCreateSystemManaged<ImageSystem>();
            m_CitySystem = World.GetOrCreateSystemManaged<CitySystem>();
            m_ClimateSystem = World.GetOrCreateSystemManaged<ClimateSystem>();
            m_TaxSystem = World.GetOrCreateSystemManaged<TaxSystem>();
            m_CountHouseholdDataSystem = World.GetOrCreateSystemManaged<CountHouseholdDataSystem>();
            m_CountWorkplacesSystem = World.GetOrCreateSystemManaged<CountWorkplacesSystem>();
            m_CountCompanyDataSystem = World.GetOrCreateSystemManaged<CountCompanyDataSystem>();

            // Initialize queries
            m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
            m_DemandParameterQuery = GetEntityQuery(ComponentType.ReadOnly<DemandParameterData>());
            m_FreeIndustrialQuery = GetEntityQuery(
                ComponentType.ReadOnly<IndustrialProperty>(), 
                ComponentType.ReadOnly<PropertyOnMarket>(), 
                ComponentType.ReadOnly<PrefabRef>(), 
                ComponentType.Exclude<Abandoned>(), 
                ComponentType.Exclude<Destroyed>(), 
                ComponentType.Exclude<Deleted>(), 
                ComponentType.Exclude<Temp>(), 
                ComponentType.Exclude<Condemned>());
            m_StorageCompanyQuery = GetEntityQuery(
                ComponentType.ReadOnly<PrefabRef>(), 
                ComponentType.ReadOnly<Game.Companies.StorageCompany>(), 
                ComponentType.Exclude<Game.Objects.OutsideConnection>(), 
                ComponentType.Exclude<Deleted>(), 
                ComponentType.Exclude<Temp>());
            m_ProcessDataQuery = GetEntityQuery(
                ComponentType.ReadOnly<IndustrialProcessData>(), 
                ComponentType.Exclude<ServiceCompanyData>());
            m_CityServiceQuery = GetEntityQuery(
                ComponentType.ReadOnly<CityServiceUpkeep>(), 
                ComponentType.Exclude<Deleted>(), 
                ComponentType.Exclude<Temp>());
            m_UnlockedZoneDataQuery = GetEntityQuery(
                ComponentType.ReadOnly<ZoneData>(), 
                ComponentType.Exclude<Locked>());

            // Initialize native collections
            m_IndustrialCompanyDemand = new NativeValue<int>(Allocator.Persistent);
            m_IndustrialBuildingDemand = new NativeValue<int>(Allocator.Persistent);
            m_StorageCompanyDemand = new NativeValue<int>(Allocator.Persistent);
            m_StorageBuildingDemand = new NativeValue<int>(Allocator.Persistent);
            m_OfficeCompanyDemand = new NativeValue<int>(Allocator.Persistent);
            m_OfficeBuildingDemand = new NativeValue<int>(Allocator.Persistent);
            m_IndustrialDemandFactors = new NativeArray<int>(18, Allocator.Persistent);
            m_OfficeDemandFactors = new NativeArray<int>(18, Allocator.Persistent);

            int resourceCount = EconomyUtils.ResourceCount;
            m_IndustrialCompanyDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
            m_IndustrialBuildingDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
            m_ResourceDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
            m_StorageBuildingDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
            m_StorageCompanyDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
            m_FreeProperties = new NativeArray<int>(resourceCount, Allocator.Persistent);
            m_FreeStorages = new NativeArray<int>(resourceCount, Allocator.Persistent);
            m_Storages = new NativeArray<int>(resourceCount, Allocator.Persistent);
            m_StorageCapacities = new NativeArray<int>(resourceCount, Allocator.Persistent);
            m_DemandData = new NativeArray<IndustrialDemandJobData>(resourceCount, Allocator.Persistent);

            // Initialize caches
            m_ResourceNameCache = new Dictionary<Resource, string>();
            m_ResourceIconCache = new Dictionary<Resource, string>();

            RequireForUpdate(m_EconomyParameterQuery);
            RequireForUpdate(m_DemandParameterQuery);
            RequireForUpdate(m_ProcessDataQuery);
        }

        [Preserve]
        protected override void OnDestroy()
        {
            if (m_IndustrialCompanyDemand.IsCreated) m_IndustrialCompanyDemand.Dispose();
            if (m_IndustrialBuildingDemand.IsCreated) m_IndustrialBuildingDemand.Dispose();
            if (m_StorageCompanyDemand.IsCreated) m_StorageCompanyDemand.Dispose();
            if (m_StorageBuildingDemand.IsCreated) m_StorageBuildingDemand.Dispose();
            if (m_OfficeCompanyDemand.IsCreated) m_OfficeCompanyDemand.Dispose();
            if (m_OfficeBuildingDemand.IsCreated) m_OfficeBuildingDemand.Dispose();
            if (m_IndustrialDemandFactors.IsCreated) m_IndustrialDemandFactors.Dispose();
            if (m_OfficeDemandFactors.IsCreated) m_OfficeDemandFactors.Dispose();
            if (m_IndustrialCompanyDemands.IsCreated) m_IndustrialCompanyDemands.Dispose();
            if (m_IndustrialBuildingDemands.IsCreated) m_IndustrialBuildingDemands.Dispose();
            if (m_ResourceDemands.IsCreated) m_ResourceDemands.Dispose();
            if (m_StorageBuildingDemands.IsCreated) m_StorageBuildingDemands.Dispose();
            if (m_StorageCompanyDemands.IsCreated) m_StorageCompanyDemands.Dispose();
            if (m_FreeProperties.IsCreated) m_FreeProperties.Dispose();
            if (m_FreeStorages.IsCreated) m_FreeStorages.Dispose();
            if (m_Storages.IsCreated) m_Storages.Dispose();
            if (m_StorageCapacities.IsCreated) m_StorageCapacities.Dispose();
            if (m_DemandData.IsCreated) m_DemandData.Dispose();
            base.OnDestroy();
        }

        protected override void OnGameLoaded(Context serializationContext)
        {
            base.OnGameLoaded(serializationContext);
            InitializeCaches();
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            if (Mod.setting.CustomUpdateInterval)
            {
                return Mod.setting.UpdateInterval;
            }
            return 512;
        }

        protected override void OnUpdate()
        {
            if (!IsPanelVisible)
            {
                return;
            }

            ForceUpdate = false;
            
            if (m_DemandParameterQuery.IsEmptyIgnoreFilter || m_EconomyParameterQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            UpdateIndustrialDemandWithBurstJob();
        }

        private void InitializeCaches()
        {
            if (m_CacheInitialized)
            {
                return;
            }

            // Initialize resource name and icon caches
            ResourceIterator iterator = ResourceIterator.GetIterator();
            while (iterator.Next())
            {
                if (iterator.resource == Resource.NoResource) continue;
                
                var resourceName = GetFormattedResourceName(iterator.resource);
                var iconPath = GetResourceIconPath(iterator.resource);
                m_ResourceNameCache[iterator.resource] = resourceName;
                m_ResourceIconCache[iterator.resource] = iconPath;
            }

            m_CacheInitialized = true;
        }

        private void UpdateIndustrialDemandWithBurstJob()
        {
            if (!IsPanelVisible)
            {
                return;
            }

            InitializeCaches();

            m_LastIndustrialCompanyDemand = m_IndustrialCompanyDemand.value;
            m_LastIndustrialBuildingDemand = m_IndustrialBuildingDemand.value;
            m_LastStorageCompanyDemand = m_StorageCompanyDemand.value;
            m_LastStorageBuildingDemand = m_StorageBuildingDemand.value;
            m_LastOfficeCompanyDemand = m_OfficeCompanyDemand.value;
            m_LastOfficeBuildingDemand = m_OfficeBuildingDemand.value;

            JobHandle deps;
            CountCompanyDataSystem.IndustrialCompanyDatas industrialCompanyDatas = 
                m_CountCompanyDataSystem.GetIndustrialCompanyDatas(out deps);

            // Create result list for job data
            var jobResults = new NativeList<IndustrialDemandJobData>(EconomyUtils.ResourceCount, Allocator.TempJob);

            // Create the burst-compiled job
            var job = new ProcessIndustrialDemandJob
            {
                // Zone and building data
                m_UnlockedZoneDatas = m_UnlockedZoneDataQuery.ToComponentDataArray<ZoneData>(Allocator.TempJob),
                m_FreePropertyChunks = m_FreeIndustrialQuery.ToArchetypeChunkListAsync(
                    World.UpdateAllocator.ToAllocator, out var outJobHandle1),
                m_StorageCompanyChunks = m_StorageCompanyQuery.ToArchetypeChunkListAsync(
                    World.UpdateAllocator.ToAllocator, out var outJobHandle2),
                m_CityServiceChunks = m_CityServiceQuery.ToArchetypeChunkListAsync(
                    World.UpdateAllocator.ToAllocator, out var outJobHandle3),

                // Type handles
                m_EntityType = GetEntityTypeHandle(),
                m_PrefabType = GetComponentTypeHandle<PrefabRef>(true),
                m_ServiceUpkeepType = GetComponentTypeHandle<CityServiceUpkeep>(true),

                // Component lookups
                m_IndustrialProcessDatas = GetComponentLookup<IndustrialProcessData>(true),
                m_PropertyRenters = GetComponentLookup<PropertyRenter>(true),
                m_Prefabs = GetComponentLookup<PrefabRef>(true),
                m_BuildingDatas = GetComponentLookup<BuildingData>(true),
                m_BuildingPropertyDatas = GetComponentLookup<BuildingPropertyData>(true),
                m_Attached = GetComponentLookup<Attached>(true),
                m_ResourceDatas = GetComponentLookup<ResourceData>(true),
                m_StorageLimitDatas = GetComponentLookup<StorageLimitData>(true),
                m_SpawnableBuildingDatas = GetComponentLookup<SpawnableBuildingData>(true),

                // Buffer lookups
                m_ServiceUpkeeps = GetBufferLookup<ServiceUpkeepData>(true),
                m_CityModifiers = GetBufferLookup<CityModifier>(true),
                m_InstalledUpgrades = GetBufferLookup<InstalledUpgrade>(true),
                m_Upkeeps = GetBufferLookup<ServiceUpkeepData>(true),

                // System data
                m_EconomyParameters = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
                m_DemandParameters = m_DemandParameterQuery.GetSingleton<DemandParameterData>(),
                m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
                m_EmployableByEducation = m_CountHouseholdDataSystem.GetEmployables(),
                m_TaxRates = m_TaxSystem.GetTaxRates(),
                m_FreeWorkplaces = m_CountWorkplacesSystem.GetFreeWorkplaces(),
                m_City = m_CitySystem.City,

                // Industrial company data
                m_Productions = industrialCompanyDatas.m_Production,
                m_CompanyResourceDemands = industrialCompanyDatas.m_Demand,
                m_Propertyless = industrialCompanyDatas.m_ProductionPropertyless,
                m_CurrentProductionWorkers = industrialCompanyDatas.m_CurrentProductionWorkers,
                m_MaxProductionWorkers = industrialCompanyDatas.m_MaxProductionWorkers,
                m_ProductionCompanies = industrialCompanyDatas.m_ProductionCompanies,

                // Output data
                m_IndustrialCompanyDemand = m_IndustrialCompanyDemand,
                m_IndustrialBuildingDemand = m_IndustrialBuildingDemand,
                m_StorageCompanyDemand = m_StorageCompanyDemand,
                m_StorageBuildingDemand = m_StorageBuildingDemand,
                m_OfficeCompanyDemand = m_OfficeCompanyDemand,
                m_OfficeBuildingDemand = m_OfficeBuildingDemand,
                m_IndustrialDemandFactors = m_IndustrialDemandFactors,
                m_OfficeDemandFactors = m_OfficeDemandFactors,
                m_IndustrialCompanyDemands = m_IndustrialCompanyDemands,
                m_IndustrialBuildingDemands = m_IndustrialBuildingDemands,
                m_StorageBuildingDemands = m_StorageBuildingDemands,
                m_StorageCompanyDemands = m_StorageCompanyDemands,
                m_ResourceDemands = m_ResourceDemands,
                m_FreeProperties = m_FreeProperties,
                m_FreeStorages = m_FreeStorages,
                m_Storages = m_Storages,
                m_StorageCapacities = m_StorageCapacities,
                m_DemandJobResults = jobResults
            };

            // Schedule the job
            var jobHandle = job.Schedule(JobUtils.CombineDependencies(
                Dependency, m_ReadDependencies, outJobHandle1, outJobHandle2, outJobHandle3, deps));

            // Complete the job and convert results
            jobHandle.Complete();

            // Convert job results to final DTOs
            ConvertJobResultsToDTO(jobResults);

            jobResults.Dispose();

            // Update dependencies
            m_WriteDependencies = Dependency;
            m_CountCompanyDataSystem.AddReader(Dependency);
            m_ResourceSystem.AddPrefabsReader(Dependency);
            m_TaxSystem.AddReader(Dependency);
        }

        private void ConvertJobResultsToDTO(NativeList<IndustrialDemandJobData> jobResults)
        {
            var products = new List<IndustrialProductDTO>(jobResults.Length);

            for (int i = 0; i < jobResults.Length; i++)
            {
                var jobData = jobResults[i];

                string resourceName = m_ResourceNameCache.TryGetValue(jobData.ResourceType, out var name) ? 
                    name : jobData.ResourceName.ToString();

                var productDTO = new IndustrialProductDTO
                {
                    ResourceName = resourceName,
                    ResourceIcon = m_ResourceIconCache.TryGetValue(jobData.ResourceType, out var icon) ? 
                        icon : "",
                    Demand = jobData.Demand,
                    Building = jobData.Building,
                    Free = jobData.Free,
                    Companies = jobData.Companies,
                    Workers = jobData.Workers,
                    SvcPercent = jobData.SvcPercent,
                    CapPercent = jobData.CapPercent,
                    CapPerCompany = jobData.CapPerCompany,
                    WrkPercent = jobData.WrkPercent,
                    TaxFactor = jobData.TaxFactor,
                    ResourceNeeds = jobData.ResourceNeeds,
                    Production = jobData.Production,
                    Storages = jobData.Storages,
                    FreeStorages = jobData.FreeStorages,
                    StorageCapacities = jobData.StorageCapacities,
                    MaxProductionWorkers = jobData.MaxProductionWorkers,
                    CurrentProductionWorkers = jobData.CurrentProductionWorkers
                };

                products.Add(productDTO);
            }

            // Sort products by resource type for consistent ordering
            m_IndustrialProductDTOs = products.ToArray();
        }

        // Helper methods
        private string GetResourceIconPath(Resource resource)
        {
            if (resource == Resource.Money)
            {
                return "Media/Game/Icons/Money.svg";
            }
            
            Entity resourcePrefab = m_ResourceSystem.GetPrefab(resource);
            string icon = m_ImageSystem.GetIconOrGroupIcon(resourcePrefab);
            return icon;
        }

        private string GetFormattedResourceName(Resource resource)
        {
            if (resource == Resource.NoResource)
                return string.Empty;
                
            string resourceName = EconomyUtils.GetName(resource);
            if (string.IsNullOrEmpty(resourceName))
                return string.Empty;
                
            return char.ToUpper(resourceName[0]) + resourceName.Substring(1);
        }

        // Public API methods (similar to original)
        public NativeArray<int> GetConsumption(out JobHandle deps)
        {
            deps = m_WriteDependencies;
            return m_ResourceDemands;
        }

        public NativeArray<int> GetIndustrialDemandFactors(out JobHandle deps)
        {
            deps = m_WriteDependencies;
            return m_IndustrialDemandFactors;
        }

        public NativeArray<int> GetOfficeDemandFactors(out JobHandle deps)
        {
            deps = m_WriteDependencies;
            return m_OfficeDemandFactors;
        }

        public NativeArray<int> GetResourceDemands(out JobHandle deps)
        {
            deps = m_WriteDependencies;
            return m_IndustrialCompanyDemands;
        }

        public NativeArray<int> GetBuildingDemands(out JobHandle deps)
        {
            deps = m_WriteDependencies;
            return m_IndustrialBuildingDemands;
        }

        public NativeArray<int> GetStorageCompanyDemands(out JobHandle deps)
        {
            deps = m_WriteDependencies;
            return m_StorageCompanyDemands;
        }

        public NativeArray<int> GetStorageBuildingDemands(out JobHandle deps)
        {
            deps = m_WriteDependencies;
            return m_StorageBuildingDemands;
        }

        public NativeArray<int> GetIndustrialResourceDemands(out JobHandle deps)
        {
            deps = m_WriteDependencies;
            return m_ResourceDemands;
        }

        public void AddReader(JobHandle reader)
        {
            m_ReadDependencies = JobHandle.CombineDependencies(m_ReadDependencies, reader);
        }

        public void SetDefaults(Context context)
        {
            m_IndustrialCompanyDemand.value = 0;
            m_IndustrialBuildingDemand.value = 0;
            m_StorageCompanyDemand.value = 0;
            m_StorageBuildingDemand.value = 0;
            m_OfficeCompanyDemand.value = 0;
            m_OfficeBuildingDemand.value = 0;
            m_IndustrialDemandFactors.Fill(0);
            m_OfficeDemandFactors.Fill(0);
            m_IndustrialCompanyDemands.Fill(0);
            m_IndustrialBuildingDemands.Fill(0);
            m_ResourceDemands.Fill(0);
            m_StorageBuildingDemands.Fill(0);
            m_StorageCompanyDemands.Fill(0);
            m_FreeProperties.Fill(0);
            m_Storages.Fill(0);
            m_FreeStorages.Fill(0);
            m_StorageCapacities.Fill(0);
            m_LastIndustrialCompanyDemand = 0;
            m_LastIndustrialBuildingDemand = 0;
            m_LastStorageCompanyDemand = 0;
            m_LastStorageBuildingDemand = 0;
            m_LastOfficeCompanyDemand = 0;
            m_LastOfficeBuildingDemand = 0;
        }
    }
}
