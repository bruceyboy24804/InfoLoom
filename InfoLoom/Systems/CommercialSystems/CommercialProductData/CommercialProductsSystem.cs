using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Debug;
using Game.Economy;
using Game.Prefabs;
using Game.Reflection;
using Game.Simulation;
using Game.Simulation.Flow;
using Game.Tools;
using Game.Zones;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using static Game.Rendering.Debug.RenderPrefabRenderer;


using System.Linq;
using Game.Prefabs.Modes;
using Game.UI;
using InfoLoomTwo.Domain;
using InfoLoomTwo.Domain.DataDomain;
using Version = Game.Version;


namespace InfoLoomTwo.Systems.CommercialSystems.CommercialProductData
{
    public partial class CommercialProductsSystem : GameSystemBase
    {
	    public struct ProductData
        {
            public Resource ResourceName;
            public int Demand;
            public int Building;
            public int Free;
            public int Companies;
            public int Workers;
            public int SvcFactor;
            public int SvcPercent;
            public int ResourceNeedPercent;
            public int ResourceNeedPerCompany;
            public int WrkPercent;
            public int TaxFactor;
            
            public int CurrentTourists; // For lodging, shows current tourists instead of tax factor
            public int AvailableLodging; // For lodging, shows available lodging rooms
            public int RequiredRooms; // For lodging, shows required rooms for tourists
            public ProductData(Resource resource)
            {
                ResourceName = resource;
                Demand = 0;
                Building = 0;
                Free = 0;
                Companies = 0;
                Workers = 0;
                SvcFactor = 0;
                SvcPercent = 0;
                ResourceNeedPercent = 0;
                ResourceNeedPerCompany = 0;
                WrkPercent = 0;
                TaxFactor = 0;
                CurrentTourists = 0;
                AvailableLodging = 0;
                RequiredRooms = 0;
            }
        }
	    [BurstCompile]
		private struct UpdateCommercialDemandJob : IJob
		{
			[DeallocateOnJobCompletion]
			[ReadOnly]
			public NativeArray<ZoneData> m_UnlockedZoneDatas;

			[ReadOnly]
			public NativeList<ArchetypeChunk> m_CommercialPropertyChunks;

			[ReadOnly]
			public ComponentTypeHandle<PrefabRef> m_PrefabType;

			[ReadOnly]
			public BufferTypeHandle<Renter> m_RenterType;

			[ReadOnly]
			public ComponentTypeHandle<PropertyOnMarket> m_PropertyOnMarketType;

			[ReadOnly]
			public ComponentLookup<Population> m_Populations;

			[ReadOnly]
			public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDatas;

			[ReadOnly]
			public ComponentLookup<ResourceData> m_ResourceDatas;

			[ReadOnly]
			public ComponentLookup<CommercialCompany> m_CommercialCompanies;

			[ReadOnly]
			public ComponentLookup<Tourism> m_Tourisms;

			[ReadOnly]
			public ResourcePrefabs m_ResourcePrefabs;

			[ReadOnly]
			public DemandParameterData m_DemandParameters;

			[ReadOnly]
			public Entity m_City;

			[ReadOnly]
			public NativeArray<int> m_TaxRates;

			public NativeValue<int> m_CompanyDemand;

			public NativeValue<int> m_BuildingDemand;

			public NativeArray<int> m_DemandFactors;

			public NativeArray<int> m_FreeProperties;

			public NativeArray<int> m_ResourceDemands;

			public NativeArray<int> m_BuildingDemands;

			[ReadOnly]
			public NativeArray<int> m_ProduceCapacity;

			[ReadOnly]
			public NativeArray<int> m_CurrentAvailables;

			[ReadOnly]
			public NativeArray<int> m_Propertyless;

			public float m_CommercialTaxEffectDemandOffset;
			[ReadOnly]
			public NativeArray<int> m_ServiceCompanies;
			public NativeArray<ProductData> m_ProductsData;
			[ReadOnly]
            public NativeArray<int> m_CurrentServiceWorkers;
            [ReadOnly]
            public NativeArray<int> m_MaxServiceWorkers;
            [ReadOnly]
            public NativeArray<int> m_TotalAvailables;
            [ReadOnly]
            public NativeArray<int> m_ResourceNeeds;
            
            [ReadOnly]
			public NativeList<ArchetypeChunk> m_CommercialProcessDataChunks;

			[ReadOnly]
			public EntityTypeHandle m_EntityType;

			[ReadOnly]
			public ComponentTypeHandle<IndustrialProcessData> m_ProcessType;

			[ReadOnly]
			public ComponentLookup<WorkplaceData> m_WorkplaceDatas;

			[ReadOnly]
			public NativeArray<int> m_EmployableByEducation;

			[ReadOnly]
			public Workplaces m_FreeWorkplaces;

			public EconomyParameterData m_EconomyParameters;
            
			public void Execute()
			{
				bool flag = false;
				for (int i = 0; i < m_UnlockedZoneDatas.Length; i++)
				{
					if (m_UnlockedZoneDatas[i].m_AreaType == AreaType.Commercial)
					{
						flag = true;
						break;
					}
				}
				ResourceIterator iterator = ResourceIterator.GetIterator();
				while (iterator.Next())
				{
					int resourceIndex = EconomyUtils.GetResourceIndex(iterator.resource);
					m_FreeProperties[resourceIndex] = 0;
					m_BuildingDemands[resourceIndex] = 0;
					m_ResourceDemands[resourceIndex] = 0;
				}
				for (int j = 0; j < m_DemandFactors.Length; j++)
				{
					m_DemandFactors[j] = 0;
				}
				for (int k = 0; k < m_CommercialPropertyChunks.Length; k++)
				{
					ArchetypeChunk archetypeChunk = m_CommercialPropertyChunks[k];
					if (!archetypeChunk.Has(ref m_PropertyOnMarketType))
					{
						continue;
					}
					NativeArray<PrefabRef> nativeArray = archetypeChunk.GetNativeArray(ref m_PrefabType);
					BufferAccessor<Renter> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_RenterType);
					for (int l = 0; l < nativeArray.Length; l++)
					{
						Entity prefab = nativeArray[l].m_Prefab;
						if (!m_BuildingPropertyDatas.HasComponent(prefab))
						{
							continue;
						}
						bool flag2 = false;
						DynamicBuffer<Renter> dynamicBuffer = bufferAccessor[l];
						for (int m = 0; m < dynamicBuffer.Length; m++)
						{
							if (m_CommercialCompanies.HasComponent(dynamicBuffer[m].m_Renter))
							{
								flag2 = true;
								break;
							}
						}
						if (flag2)
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
					}
				}
				m_CompanyDemand.value = 0;
				m_BuildingDemand.value = 0;
				int population = m_Populations[m_City].m_Population;
				iterator = ResourceIterator.GetIterator();
				int num = 0;
				while (iterator.Next())
				{
					int resourceIndex2 = EconomyUtils.GetResourceIndex(iterator.resource);
					if (!EconomyUtils.IsCommercialResource(iterator.resource) || !m_ResourceDatas.HasComponent(m_ResourcePrefabs[iterator.resource]))
					{
						continue;
					}
					ResourceData resourceData = m_ResourceDatas[m_ResourcePrefabs[iterator.resource]];

					float num2 = -0.05f * ((float)TaxSystem.GetCommercialTaxRate(iterator.resource, m_TaxRates) - 10f) * m_DemandParameters.m_TaxEffect.y;
					num2 += m_CommercialTaxEffectDemandOffset;
					if (iterator.resource != Resource.Lodging)
					{
						int num3 = ((population <= 1000) ? 2500 : (2500 * (int)Mathf.Log10(0.01f * (float)population)));
						m_ResourceDemands[resourceIndex2] = math.clamp(100 - (m_CurrentAvailables[resourceIndex2] - num3) / 25, 0, 100);
					}
					else if (math.max((int)((float)m_Tourisms[m_City].m_CurrentTourists * m_DemandParameters.m_HotelRoomPercentRequirement) - m_Tourisms[m_City].m_Lodging.y, 0) > 0)
					{
						m_ResourceDemands[resourceIndex2] = 100;
					}
					m_ResourceDemands[resourceIndex2] = Mathf.RoundToInt((1f + num2) * (float)m_ResourceDemands[resourceIndex2]);
					
					// With this safer approach:
					
					
					int num4 = Mathf.RoundToInt(100f * num2);
					m_DemandFactors[11] += num4;
					if (m_ResourceDemands[resourceIndex2] > 0)
					{
						m_CompanyDemand.value += m_ResourceDemands[resourceIndex2];
						m_BuildingDemands[resourceIndex2] = ((m_FreeProperties[resourceIndex2] - m_Propertyless[resourceIndex2] <= 0) ? m_ResourceDemands[resourceIndex2] : 0);
						if (m_BuildingDemands[resourceIndex2] > 0)
						{
							m_BuildingDemand.value += m_BuildingDemands[resourceIndex2];
						}
						int num5 = ((m_BuildingDemands[resourceIndex2] > 0) ? m_ResourceDemands[resourceIndex2] : 0);
						int num6 = m_ResourceDemands[resourceIndex2];
						int num7 = num6 + num4;
						if (iterator.resource == Resource.Lodging)
						{
							m_DemandFactors[9] += num6;
						}
						else if (iterator.resource == Resource.Petrochemicals)
						{
							m_DemandFactors[16] += num6;
						}
						else
						{
							m_DemandFactors[4] += num6;
						}
						m_DemandFactors[13] += math.min(0, num5 - num7);
						num++;
					}
					
					ProductData uiData = m_ProductsData[resourceIndex2];
					{
						
					    uiData.ResourceName = iterator.resource;
					    uiData.Demand = m_ResourceDemands[resourceIndex2];
					    uiData.Building = m_BuildingDemands[resourceIndex2]; // Use pre-calculated value
					    uiData.Free = m_FreeProperties[resourceIndex2];
					    uiData.Companies = m_ServiceCompanies[resourceIndex2];
					    uiData.Workers = m_CurrentServiceWorkers[resourceIndex2];
					    uiData.SvcFactor = Mathf.RoundToInt(100f * m_DemandParameters.m_CommercialBaseDemand * (float)m_ResourceNeeds[resourceIndex2] - (float)m_ProduceCapacity[resourceIndex2]);
					    uiData.SvcPercent = (m_TotalAvailables[resourceIndex2] == 0 ? 0 : 100 * m_CurrentAvailables[resourceIndex2] / m_TotalAvailables[resourceIndex2]);
					    uiData.ResourceNeedPercent = 100 * m_ProduceCapacity[resourceIndex2] / math.max(100, m_ResourceNeeds[resourceIndex2]);
					    uiData.ResourceNeedPerCompany = (m_ServiceCompanies[resourceIndex2] == 0 ? 0 : m_ResourceNeeds[resourceIndex2] / m_ServiceCompanies[resourceIndex2]);
					    uiData.WrkPercent = 100 * (m_CurrentServiceWorkers[resourceIndex2] + 1) / (m_MaxServiceWorkers[resourceIndex2] + 1);
					    uiData.TaxFactor = num4;
					    if (iterator.resource == Resource.Lodging)
					    {
					        var tourism = m_Tourisms[m_City];
					        int currentTourists = tourism.m_CurrentTourists;
					        int availableLodging = tourism.m_Lodging.y;
					        int requiredRooms = (int)((float)currentTourists * m_DemandParameters.m_HotelRoomPercentRequirement);
					        
					        uiData.CurrentTourists = currentTourists; 
					        uiData.AvailableLodging = availableLodging; 
					        uiData.RequiredRooms = requiredRooms; 
					    }
					};
					m_ProductsData[resourceIndex2] = uiData;
				}
				if (m_DemandFactors[4] == 0)
				{
					m_DemandFactors[4] = -10;
				}
				if (population <= 0)
				{
					m_DemandFactors[4] = 0;
				}
				if (m_CommercialPropertyChunks.Length == 0 && m_DemandFactors[13] > 0)
				{
					m_DemandFactors[13] = 0;
				}
				m_CompanyDemand.value = ((num != 0) ? math.clamp(m_CompanyDemand.value / num, 0, 100) : 0);
				m_BuildingDemand.value = ((num != 0 && flag) ? math.clamp(m_BuildingDemand.value / num, 0, 100) : 0);
			}
		}
		public bool IsPanelVisible { get; set; }
		private ResourceSystem m_ResourceSystem;

		private TaxSystem m_TaxSystem;

		private CountCompanyDataSystem m_CountCompanyDataSystem;

		private CountHouseholdDataSystem m_CountHouseholdDataSystem;

		private CitySystem m_CitySystem;

		private EntityQuery m_EconomyParameterQuery;

		private EntityQuery m_DemandParameterQuery;

		private EntityQuery m_CommercialQuery;

		private EntityQuery m_CommercialProcessDataQuery;

		private EntityQuery m_UnlockedZoneDataQuery;

		private EntityQuery m_GameModeSettingQuery;

		private NativeValue<int> m_CompanyDemand;

		private NativeValue<int> m_BuildingDemand;

		[EnumArray(typeof(DemandFactor))]
		[DebugWatchValue]
		private NativeArray<int> m_DemandFactors;

		[ResourceArray]
		[DebugWatchValue]
		private NativeArray<int> m_ResourceDemands;

		[ResourceArray]
		[DebugWatchValue]
		private NativeArray<int> m_BuildingDemands;

		[ResourceArray]
		[DebugWatchValue]
		private NativeArray<int> m_Consumption;

		[ResourceArray]
		[DebugWatchValue]
		private NativeArray<int> m_FreeProperties;

		[DebugWatchDeps]
		private JobHandle m_WriteDependencies;

		private JobHandle m_ReadDependencies;

		private int m_LastCompanyDemand;

		private int m_LastBuildingDemand;

		private float m_CommercialTaxEffectDemandOffset;
		public static NativeArray<ProductData> m_ProductsData;
		
		
		public override int GetUpdateInterval(SystemUpdatePhase phase)
		{
			return 16;
		}

		public override int GetUpdateOffset(SystemUpdatePhase phase)
		{
			return 4;
		}
		public NativeArray<int> GetDemandFactors(out JobHandle deps)
		{
			deps = m_WriteDependencies;
			return m_DemandFactors;
		}

		public NativeArray<int> GetResourceDemands(out JobHandle deps)
		{
			deps = m_WriteDependencies;
			return m_ResourceDemands;
		}

		public NativeArray<int> GetBuildingDemands(out JobHandle deps)
		{
			deps = m_WriteDependencies;
			return m_BuildingDemands;
		}

		public NativeArray<int> GetConsumption(out JobHandle deps)
		{
			deps = m_WriteDependencies;
			return m_Consumption;
		}

		public void AddReader(JobHandle reader)
		{
			m_ReadDependencies = JobHandle.CombineDependencies(m_ReadDependencies, reader);
		}
		

		protected override void OnGameLoaded(Context serializationContext)
		{
			base.OnGameLoaded(serializationContext);
			m_Consumption.Fill(0);
			if (m_GameModeSettingQuery.IsEmptyIgnoreFilter)
			{
				m_CommercialTaxEffectDemandOffset = 0f;
				return;
			}
			ModeSettingData singleton = m_GameModeSettingQuery.GetSingleton<ModeSettingData>();
			if (singleton.m_Enable)
			{
				m_CommercialTaxEffectDemandOffset = singleton.m_CommercialTaxEffectDemandOffset;
			}
			else
			{
				m_CommercialTaxEffectDemandOffset = 0f;
			}
		}
		protected override void OnCreate()
		{
			base.OnCreate();
			m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
			m_TaxSystem = base.World.GetOrCreateSystemManaged<TaxSystem>();
			m_CountCompanyDataSystem = base.World.GetOrCreateSystemManaged<CountCompanyDataSystem>();
			m_CountHouseholdDataSystem = base.World.GetOrCreateSystemManaged<CountHouseholdDataSystem>();
			m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
			m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
			m_DemandParameterQuery = GetEntityQuery(ComponentType.ReadOnly<DemandParameterData>());
			m_CommercialQuery = GetEntityQuery(ComponentType.ReadOnly<CommercialProperty>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Abandoned>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Condemned>(), ComponentType.Exclude<Temp>());
			m_CommercialProcessDataQuery = GetEntityQuery(ComponentType.ReadOnly<IndustrialProcessData>(), ComponentType.ReadOnly<ServiceCompanyData>());
			m_UnlockedZoneDataQuery = GetEntityQuery(ComponentType.ReadOnly<ZoneData>(), ComponentType.Exclude<Locked>());
			m_GameModeSettingQuery = GetEntityQuery(ComponentType.ReadOnly<ModeSettingData>());
			m_CompanyDemand = new NativeValue<int>(Allocator.Persistent);
			m_BuildingDemand = new NativeValue<int>(Allocator.Persistent);
			m_DemandFactors = new NativeArray<int>(18, Allocator.Persistent);
			int resourceCount = EconomyUtils.ResourceCount;
			m_ResourceDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
			m_BuildingDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
			m_Consumption = new NativeArray<int>(resourceCount, Allocator.Persistent);
			m_FreeProperties = new NativeArray<int>(resourceCount, Allocator.Persistent);
			m_ProductsData = new NativeArray<ProductData>(resourceCount, Allocator.Persistent);
			
			
			m_CommercialTaxEffectDemandOffset = 0f;
			RequireForUpdate(m_EconomyParameterQuery);
			RequireForUpdate(m_DemandParameterQuery);
			RequireForUpdate(m_CommercialProcessDataQuery);
		}

		[Preserve]
		protected override void OnDestroy()
		{
			m_CompanyDemand.Dispose();
			m_BuildingDemand.Dispose();
			m_DemandFactors.Dispose();
			m_ResourceDemands.Dispose();
			m_BuildingDemands.Dispose();
			m_Consumption.Dispose();
			m_FreeProperties.Dispose();
			base.OnDestroy();
		}

		public void SetDefaults(Context context)
		{
			m_CompanyDemand.value = 0;
			m_BuildingDemand.value = 0;
			m_DemandFactors.Fill(0);
			m_ResourceDemands.Fill(0);
			m_BuildingDemands.Fill(0);
			m_Consumption.Fill(0);
			m_FreeProperties.Fill(0);
			m_LastCompanyDemand = 0;
			m_LastBuildingDemand = 0;
		}
		public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
		{
			writer.Write(m_CompanyDemand.value);
			writer.Write(m_BuildingDemand.value);
			writer.Write(m_DemandFactors.Length);
			writer.Write(m_DemandFactors);
			writer.Write(m_ResourceDemands);
			writer.Write(m_BuildingDemands);
			writer.Write(m_Consumption);
			writer.Write(m_FreeProperties);
			writer.Write(m_LastCompanyDemand);
			writer.Write(m_LastBuildingDemand);
		}

		public void Deserialize<TReader>(TReader reader) where TReader : IReader
		{
			reader.Read(out int value);
			m_CompanyDemand.value = value;
			reader.Read(out int value2);
			m_BuildingDemand.value = value2;
			if (reader.context.version < Version.demandFactorCountSerialization)
			{
				NativeArray<int> nativeArray = new NativeArray<int>(13, Allocator.Temp);
				reader.Read(nativeArray);
				CollectionUtils.CopySafe(nativeArray, m_DemandFactors);
				nativeArray.Dispose();
			}
			else
			{
				reader.Read(out int value3);
				if (value3 == m_DemandFactors.Length)
				{
					reader.Read(m_DemandFactors);
				}
				else
				{
					NativeArray<int> nativeArray2 = new NativeArray<int>(value3, Allocator.Temp);
					reader.Read(nativeArray2);
					CollectionUtils.CopySafe(nativeArray2, m_DemandFactors);
					nativeArray2.Dispose();
				}
			}
			if (reader.context.format.Has(FormatTags.FishResource))
			{
				reader.Read(m_ResourceDemands);
				reader.Read(m_BuildingDemands);
			}
			else
			{
				reader.Read(m_ResourceDemands.GetSubArray(0, 40));
				reader.Read(m_BuildingDemands.GetSubArray(0, 40));
				m_ResourceDemands[40] = 0;
				m_BuildingDemands[40] = 0;
			}
			NativeArray<int> value4 = default(NativeArray<int>);
			if (reader.context.version < Version.companyDemandOptimization)
			{
				value4 = new NativeArray<int>(EconomyUtils.ResourceCount, Allocator.Temp);
				reader.Read(value4);
			}
			if (reader.context.format.Has(FormatTags.FishResource))
			{
				reader.Read(m_Consumption);
			}
			else
			{
				reader.Read(m_Consumption.GetSubArray(0, 40));
				m_Consumption[40] = 0;
			}
			if (reader.context.version < Version.companyDemandOptimization)
			{
				reader.Read(value4);
				reader.Read(value4);
				reader.Read(value4);
			}
			if (reader.context.format.Has(FormatTags.FishResource))
			{
				reader.Read(m_FreeProperties);
			}
			else
			{
				reader.Read(m_FreeProperties.GetSubArray(0, 40));
				m_FreeProperties[40] = 0;
			}
			if (reader.context.version < Version.companyDemandOptimization)
			{
				reader.Read(value4);
				value4.Dispose();
			}
			reader.Read(out m_LastCompanyDemand);
			reader.Read(out m_LastBuildingDemand);
		}
		protected override void OnUpdate()
		{
			 if (!IsPanelVisible ) return;
			if (!m_DemandParameterQuery.IsEmptyIgnoreFilter && !m_EconomyParameterQuery.IsEmptyIgnoreFilter)
			{
				m_LastCompanyDemand = m_CompanyDemand.value;
				m_LastBuildingDemand = m_BuildingDemand.value;
				JobHandle deps;
				JobHandle deps2;
				CountCompanyDataSystem.CommercialCompanyDatas commercialCompanyDatas = m_CountCompanyDataSystem.GetCommercialCompanyDatas(out deps);
				UpdateCommercialDemandJob updateCommercialDemandJob = default(UpdateCommercialDemandJob);
				updateCommercialDemandJob.m_CommercialPropertyChunks = m_CommercialQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out var outJobHandle);
				updateCommercialDemandJob.m_UnlockedZoneDatas = m_UnlockedZoneDataQuery.ToComponentDataArray<ZoneData>(Allocator.TempJob);
				updateCommercialDemandJob.m_PrefabType = SystemAPI.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
				updateCommercialDemandJob.m_RenterType = SystemAPI.GetBufferTypeHandle<Renter>(isReadOnly: true);
				updateCommercialDemandJob.m_PropertyOnMarketType = SystemAPI.GetComponentTypeHandle<PropertyOnMarket>(isReadOnly: true);
				updateCommercialDemandJob.m_Populations = SystemAPI.GetComponentLookup<Population>(isReadOnly: true);
				updateCommercialDemandJob.m_BuildingPropertyDatas = SystemAPI.GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
				updateCommercialDemandJob.m_ResourceDatas = SystemAPI.GetComponentLookup<ResourceData>(isReadOnly: true);
				updateCommercialDemandJob.m_CommercialCompanies = SystemAPI.GetComponentLookup<CommercialCompany>(isReadOnly: true);
				updateCommercialDemandJob.m_ResourcePrefabs = m_ResourceSystem.GetPrefabs();
				updateCommercialDemandJob.m_DemandParameters = m_DemandParameterQuery.GetSingleton<DemandParameterData>();
				updateCommercialDemandJob.m_TaxRates = m_TaxSystem.GetTaxRates();
				updateCommercialDemandJob.m_CompanyDemand = m_CompanyDemand;
				updateCommercialDemandJob.m_BuildingDemand = m_BuildingDemand;
				updateCommercialDemandJob.m_DemandFactors = m_DemandFactors;
				updateCommercialDemandJob.m_City = m_CitySystem.City;
				updateCommercialDemandJob.m_ResourceDemands = m_ResourceDemands;
				updateCommercialDemandJob.m_BuildingDemands = m_BuildingDemands;
				updateCommercialDemandJob.m_ProduceCapacity = commercialCompanyDatas.m_ProduceCapacity;
				updateCommercialDemandJob.m_CurrentAvailables = commercialCompanyDatas.m_CurrentAvailables;
				updateCommercialDemandJob.m_FreeProperties = m_FreeProperties;
				updateCommercialDemandJob.m_Propertyless = commercialCompanyDatas.m_ServicePropertyless;
				updateCommercialDemandJob.m_Tourisms = SystemAPI.GetComponentLookup<Tourism>(isReadOnly: true);
				updateCommercialDemandJob.m_ServiceCompanies = commercialCompanyDatas.m_ServiceCompanies;
				updateCommercialDemandJob.m_CurrentServiceWorkers = commercialCompanyDatas.m_CurrentServiceWorkers;
				updateCommercialDemandJob.m_MaxServiceWorkers = commercialCompanyDatas.m_MaxServiceWorkers;
				updateCommercialDemandJob.m_TotalAvailables = commercialCompanyDatas.m_TotalAvailables;
				updateCommercialDemandJob.m_ResourceNeeds = m_CountHouseholdDataSystem.GetResourceNeeds(out deps2);
				updateCommercialDemandJob.m_CommercialTaxEffectDemandOffset = m_CommercialTaxEffectDemandOffset;
				updateCommercialDemandJob.m_ProductsData = m_ProductsData;
				UpdateCommercialDemandJob jobData = updateCommercialDemandJob;
				base.Dependency = IJobExtensions.Schedule(jobData, JobUtils.CombineDependencies(base.Dependency, m_ReadDependencies, outJobHandle, deps, deps2));
				m_WriteDependencies = base.Dependency;
				m_CountHouseholdDataSystem.AddHouseholdDataReader(base.Dependency);
				m_ResourceSystem.AddPrefabsReader(base.Dependency);
				m_TaxSystem.AddReader(base.Dependency);
			}
		}
    }
}