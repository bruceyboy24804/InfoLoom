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
using Game.Objects;
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

namespace InfoLoomTwo.Systems.IndustrialSystems.IndustrialDemandData
{
    public partial class IndustrialSystem : UISystemBase
    {
        // m_Results array indices:
		// INDUSTRIAL (0-9):
		//   [0] - Free industrial properties
		//   [1] - Propertyless industrial companies
		//   [2] - Average tax rate (x10)
		//   [3] - Production capacity / local demand (percentage)
		//   [4] - Employee capacity ratio (x1000)
		//   [5] - Free storage properties ✅
		//   [6] - Propertyless storage companies ✅
		//   [7] - Input utilization (percentage) ✅
		//   [8] - Available educated workforce
		//   [9] - Available uneducated workforce
		// OFFICE (10-14):
		//   [10] - Free office properties
		//   [11] - Propertyless office companies
		//   [12] - Average tax rate (x10)
		//   [13] - Production capacity / local demand (percentage)
		//   [14] - Employee capacity ratio (x1000)
		// STORAGE (15):
		//   [15] - Storage company demand ✅

		// Constants
		private const Resource kOfficeResources = Resource.Software | Resource.Media | Resource.Telecom | Resource.Financial;
		private const Resource kIndustryResources =
			Resource.ConvenienceFood | Resource.Food | Resource.Timber | Resource.Paper |
			Resource.Furniture | Resource.Vehicles | Resource.Petrochemicals | Resource.Plastics |
			Resource.Metals | Resource.Electronics | Resource.Steel | Resource.Minerals |
			Resource.Concrete | Resource.Machinery | Resource.Chemicals | Resource.Pharmaceuticals |
			Resource.Beverages | Resource.Textiles;

		// System dependencies
		private ResourceSystem m_ResourceSystem;
		private CitySystem m_CitySystem;
		private TaxSystem m_TaxSystem;
		private CountHouseholdDataSystem m_CountHouseholdDataSystem;
		private CountWorkplacesSystem m_CountWorkplacesSystem;
		private CountCompanyDataSystem m_CountCompanyDataSystem;

		// Queries
		private EntityQuery m_EconomyParameterQuery;
		private EntityQuery m_DemandParameterQuery;
		private EntityQuery m_FreeIndustrialQuery;
		private EntityQuery m_ProcessDataQuery;
		private EntityQuery m_StorageCompanyQuery;

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
			m_CitySystem = World.GetOrCreateSystemManaged<CitySystem>();
			m_TaxSystem = World.GetOrCreateSystemManaged<TaxSystem>();
			m_CountHouseholdDataSystem = World.GetOrCreateSystemManaged<CountHouseholdDataSystem>();
			m_CountWorkplacesSystem = World.GetOrCreateSystemManaged<CountWorkplacesSystem>();
			m_CountCompanyDataSystem = World.GetOrCreateSystemManaged<CountCompanyDataSystem>();
			
			m_EconomyParameterQuery = SystemAPI.QueryBuilder().WithAll<EconomyParameterData>().Build();
			m_DemandParameterQuery = SystemAPI.QueryBuilder().WithAll<DemandParameterData>().Build();
			
			m_FreeIndustrialQuery = SystemAPI.QueryBuilder().WithAll<IndustrialProperty>().WithAll<PropertyOnMarket, PrefabRef>().WithNone<Abandoned, Destroyed, Deleted, Condemned, Temp>().Build();
			m_ProcessDataQuery = SystemAPI.QueryBuilder().WithAll<IndustrialProcessData>().WithNone<ServiceCompanyData>().Build();
			m_StorageCompanyQuery = SystemAPI.QueryBuilder().WithAll<PrefabRef, Game.Companies.StorageCompany>().WithNone<Game.Objects.OutsideConnection, Deleted>().Build();
			
			int resourceCount = EconomyUtils.ResourceCount;
			m_ResourceDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
			m_FreeProperties = new NativeArray<int>(resourceCount, Allocator.Persistent);
			m_Results = new NativeArray<int>(16, Allocator.Persistent);
			m_IncludedResources = new NativeValue<Resource>(Allocator.Persistent);

			RequireForUpdate(m_EconomyParameterQuery);
			RequireForUpdate(m_DemandParameterQuery);
			RequireForUpdate(m_ProcessDataQuery);

			Mod.log.Info("IndustrialSystemSimplified created.");
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
			var industrialData = m_CountCompanyDataSystem.GetIndustrialCompanyDatas(out deps);
			
			// Count free properties and storage companies
			CountFreeProperties();
			CountStorageCompanies();
			
			// Calculate demands using game's system
			var (gameDemands, buildingDemands) = CalculateGameDemands();
			
			// Gather display metrics
			GatherMetrics(industrialData, gameDemands, buildingDemands);
			
			deps.Complete();
		}

		private void CountFreeProperties()
		{
			var prefabType = SystemAPI.GetComponentTypeHandle<PrefabRef>(true);
			var renterType = SystemAPI.GetBufferTypeHandle<Renter>(true);
			var buildingDatas = SystemAPI.GetComponentLookup<BuildingPropertyData>(true);
			var industrialCompanies = SystemAPI.GetComponentLookup<IndustrialCompany>(true);
			var attached = SystemAPI.GetComponentLookup<Attached>(true);
			var prefabs = SystemAPI.GetComponentLookup<PrefabRef>(true);

			var chunks = m_FreeIndustrialQuery.ToArchetypeChunkArray(Allocator.Temp);
			
			foreach (var chunk in chunks)
			{
				var prefabRefs = chunk.GetNativeArray(ref prefabType);
				var renters = chunk.GetBufferAccessor(ref renterType);
				var entities = chunk.GetNativeArray(SystemAPI.GetEntityTypeHandle());

				for (int i = 0; i < chunk.Count; i++)
				{
					if (!buildingDatas.HasComponent(prefabRefs[i].m_Prefab))
						continue;

					bool hasIndustrialTenant = false;
					var renterBuffer = renters[i];
					
					for (int j = 0; j < renterBuffer.Length; j++)
					{
						if (industrialCompanies.HasComponent(renterBuffer[j].m_Renter))
						{
							hasIndustrialTenant = true;
							break;
						}
					}

					if (!hasIndustrialTenant)
					{
						var buildingData = buildingDatas[prefabRefs[i].m_Prefab];
						
						// Check for attached parent restrictions
						if (attached.TryGetComponent(entities[i], out var attachedData) && 
							prefabs.TryGetComponent(attachedData.m_Parent, out var parentPrefab) && 
							buildingDatas.TryGetComponent(parentPrefab.m_Prefab, out var parentBuildingData))
						{
							buildingData.m_AllowedManufactured &= parentBuildingData.m_AllowedManufactured;
						}
						
						var iterator = ResourceIterator.GetIterator();
						
						while (iterator.Next())
						{
							if ((buildingData.m_AllowedManufactured & iterator.resource) != Resource.NoResource)
							{
								m_FreeProperties[EconomyUtils.GetResourceIndex(iterator.resource)]++;
							}
						}
						
						// Count free industry/office/storage properties
						if ((buildingData.m_AllowedManufactured & kIndustryResources) != Resource.NoResource)
							m_Results[0]++;
						if ((buildingData.m_AllowedManufactured & kOfficeResources) != Resource.NoResource)
							m_Results[10]++;
						if (buildingData.m_AllowedStored != Resource.NoResource)
							m_Results[5]++;
					}
				}
			}
			
			chunks.Dispose();
		}

		private void CountStorageCompanies()
		{
			var prefabType = SystemAPI.GetComponentTypeHandle<PrefabRef>(true);
			var entityType = SystemAPI.GetEntityTypeHandle();
			var processDatas = SystemAPI.GetComponentLookup<IndustrialProcessData>(true);
			var propertyRenters = SystemAPI.GetComponentLookup<PropertyRenter>(true);

			var chunks = m_StorageCompanyQuery.ToArchetypeChunkArray(Allocator.Temp);
			int storageCompaniesWithoutProperty = 0;

			foreach (var chunk in chunks)
			{
				var prefabs = chunk.GetNativeArray(ref prefabType);
				var entities = chunk.GetNativeArray(entityType);

				for (int i = 0; i < chunk.Count; i++)
				{
					var prefab = prefabs[i].m_Prefab;
					var entity = entities[i];

					// Check if this storage company has a property
					if (!propertyRenters.HasComponent(entity) || 
						!propertyRenters[entity].m_Property.Equals(Entity.Null))
					{
						// Has property or no renter component
						continue;
					}

					// Count propertyless storage companies
					storageCompaniesWithoutProperty++;
				}
			}

			m_Results[6] = storageCompaniesWithoutProperty;
			chunks.Dispose();
		}

		private (NativeArray<int> resourceDemands, NativeArray<int> buildingDemands) CalculateGameDemands()
		{
			var industrialDemandSystem = World.GetExistingSystemManaged<IndustrialDemandSystem>();
			if (industrialDemandSystem == null)
			{
				var empty = new NativeArray<int>(EconomyUtils.ResourceCount, Allocator.Temp);
				return (empty, new NativeArray<int>(EconomyUtils.ResourceCount, Allocator.Temp));
			}

			JobHandle deps1, deps2;
			var demands = industrialDemandSystem.GetResourceDemands(out deps1);
			var buildingDemands = industrialDemandSystem.GetBuildingDemands(out deps2);
			deps1.Complete();
			deps2.Complete();
			
			var resultDemands = new NativeArray<int>(demands.Length, Allocator.Temp);
			var resultBuilding = new NativeArray<int>(buildingDemands.Length, Allocator.Temp);
			demands.CopyTo(resultDemands);
			buildingDemands.CopyTo(resultBuilding);
			
			return (resultDemands, resultBuilding);
		}

		private void GatherMetrics(CountCompanyDataSystem.IndustrialCompanyDatas data, NativeArray<int> gameDemands, NativeArray<int> buildingDemands)
		{
			var resourceDatas = SystemAPI.GetComponentLookup<ResourceData>(true);
			var resourcePrefabs = m_ResourceSystem.GetPrefabs();
			var taxRates = m_TaxSystem.GetTaxRates();
			var employables = m_CountHouseholdDataSystem.GetEmployables(out JobHandle deps);
			deps.Complete();
			var freeWorkplaces = m_CountWorkplacesSystem.GetFreeWorkplaces();
			var processDatas = SystemAPI.GetComponentLookup<IndustrialProcessData>(true);

			int numIndustry = 0, numOffice = 0;
			float prodCapInd = 0f, prodCapOff = 0f;
			float empCapInd = 0f, empCapOff = 0f;
			float taxRateInd = 0f, taxRateOff = 0f;
			float inputUtil = 0f;
			int numInputs = 0;
			int storageCompanyDemand = 0;

			const int kStorageProductionDemand = 20000;

			// Get process data chunks for input utilization calculation
			var processChunks = m_ProcessDataQuery.ToArchetypeChunkArray(Allocator.Temp);
			var processType = SystemAPI.GetComponentTypeHandle<IndustrialProcessData>(true);

			var iterator = ResourceIterator.GetIterator();

			while (iterator.Next())
			{
				int idx = EconomyUtils.GetResourceIndex(iterator.resource);

				if (!resourceDatas.HasComponent(resourcePrefabs[iterator.resource]))
					continue;

				var resourceData = resourceDatas[resourcePrefabs[iterator.resource]];
				
				// Check storage demand for tradable material resources
				bool isTradable = resourceData.m_IsTradable;
				bool isOffice = resourceData.m_Weight == 0f;
				bool isMaterial = resourceData.m_IsMaterial;
				
				if (isTradable && !isOffice && data.m_Demand[idx] > kStorageProductionDemand)
				{
					// This resource needs storage if demand is high
					storageCompanyDemand++;
				}
				
				if (!resourceData.m_IsProduceable)
					continue;

				// Skip extractors (materials) for production metrics
				if (isMaterial)
					continue;

				m_ResourceDemands[idx] = gameDemands[idx];

				// Production capacity
				float productionCapacity = data.m_Production[idx] == 0 ? 0f :
					math.min(4f, (float)data.m_Production[idx] / ((float)data.m_Demand[idx] + 1f));

				// Employee capacity
				float empCapacity = data.m_MaxProductionWorkers[idx] == 0 ? 0f :
					(float)data.m_CurrentProductionWorkers[idx] / data.m_MaxProductionWorkers[idx];

				// Tax rate
				int taxRate = isOffice ? 
					TaxSystem.GetOfficeTaxRate(iterator.resource, taxRates) : 
					TaxSystem.GetIndustrialTaxRate(iterator.resource, taxRates);

				// Input utilization - only for non-material industrial resources
				if (!isMaterial && !isOffice)
				{
					// Check all industrial processes that output this resource
					foreach (var chunk in processChunks)
					{
						var processes = chunk.GetNativeArray(ref processType);
						for (int i = 0; i < processes.Length; i++)
						{
							var process = processes[i];
							if (process.m_Output.m_Resource == iterator.resource && 
								process.m_Input1.m_Resource != iterator.resource)
							{
								// Check input 1
								if (process.m_Input1.m_Amount != 0)
								{
									int inputIdx = EconomyUtils.GetResourceIndex(process.m_Input1.m_Resource);
									inputUtil += math.min(4f, (float)data.m_Demand[inputIdx] / ((float)data.m_Production[inputIdx] + 1f));
									numInputs++;
								}
								// Check input 2
								if (process.m_Input2.m_Amount != 0)
								{
									int inputIdx = EconomyUtils.GetResourceIndex(process.m_Input2.m_Resource);
									inputUtil += math.min(4f, (float)data.m_Demand[inputIdx] / ((float)data.m_Production[inputIdx] + 1f));
									numInputs++;
								}
							}
						}
					}
				}

				if (isOffice)
				{
					numOffice++;
					prodCapOff += productionCapacity;
					empCapOff += empCapacity;
					taxRateOff += taxRate;
					m_Results[11] += data.m_ProductionPropertyless[idx];
				}
				else
				{
					numIndustry++;
					prodCapInd += productionCapacity;
					empCapInd += empCapacity;
					taxRateInd += taxRate;
					m_Results[1] += data.m_ProductionPropertyless[idx];
				}

				if (buildingDemands[idx] >= Mod.setting.indResDemValue)
					m_IncludedResources.value |= iterator.resource;
			}

			// Calculate workforce availability
			for (int i = 0; i < 5; i++)
			{
				int available = math.max(0, employables[i] - freeWorkplaces[i]);
				if (i >= 2) m_Results[8] += available; // Educated
				else m_Results[9] += available; // Uneducated
			}

			// Industry metrics
			m_Results[2] = numIndustry > 0 ? Mathf.RoundToInt(10f * taxRateInd / numIndustry) : 0;  // Tax rate
			m_Results[3] = numIndustry > 0 ? Mathf.RoundToInt(100f * prodCapInd / numIndustry) : 0;  // Production capacity
			m_Results[4] = numIndustry > 0 ? Mathf.RoundToInt(1000f * empCapInd / numIndustry) : 0;  // Employee capacity
			m_Results[7] = numInputs > 0 ? Mathf.RoundToInt(100f * inputUtil / numInputs) : 0;  // Input utilization

			// Office metrics
			m_Results[12] = numOffice > 0 ? Mathf.RoundToInt(10f * taxRateOff / numOffice) : 0;  // Tax rate
			m_Results[13] = numOffice > 0 ? Mathf.RoundToInt(100f * prodCapOff / numOffice) : 0;  // Production capacity
			m_Results[14] = numOffice > 0 ? Mathf.RoundToInt(1000f * empCapOff / numOffice) : 0;  // Employee capacity

			// Storage metrics
			// m_Results[5] - Free storage properties (calculated in CountFreeProperties)
			// m_Results[6] - Propertyless storage companies (calculated in CountStorageCompanies)
			m_Results[15] = storageCompanyDemand;  // Number of resources that need storage

			processChunks.Dispose();
			gameDemands.Dispose();
			buildingDemands.Dispose();
			employables.Dispose();
			
		}

        
    }
}