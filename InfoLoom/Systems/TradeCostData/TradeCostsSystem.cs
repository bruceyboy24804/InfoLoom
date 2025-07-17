using System.Collections.Generic;
using System.Linq;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.UI;
using InfoLoomTwo.Domain.DataDomain;
using InfoLoomTwo.Domain.DataDomain.Enums.TradeCostEnums;
using InfoLoomTwo.Extensions;
using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using OutsideConnection = Game.Objects.OutsideConnection;
using Resources = Game.Economy.Resources;
using StorageCompany = Game.Companies.StorageCompany;

namespace InfoLoomTwo.Systems.TradeCostData
{
    public partial class TradeCostsSystem : GameSystemBase
    {
		public BuyCostEnum m_BuyCostEnum = BuyCostEnum.Off;
        public ExportAmountEnum m_exportAmountEnum = ExportAmountEnum.Off;
        public ImportAmountEnum m_importAmountEnum = ImportAmountEnum.Off;
        public ProfitEnum m_profitEnum = ProfitEnum.Off;
        public ProfitMarginEnum m_profitMarginEnum = ProfitMarginEnum.Off;
        public ResourceNameEnum m_resourceNameEnum = ResourceNameEnum.Off;
        public SellCostEnum m_sellCostEnum = SellCostEnum.Off;
        private Dictionary<Resource, ResourceTradeCost> m_TradeCosts = new Dictionary<Resource, ResourceTradeCost>();
        private ImageSystem m_ImageSystem;
        private ResourceSystem m_ResourceSystem;
        private PrefabSystem m_PrefabSystem;
        private CommercialDemandSystem m_CommercialDemandSystem;
        private IndustrialDemandSystem m_IndustrialDemandSystem;
        private CountCompanyDataSystem m_CountCompanyDataSystem;
        private TradeSystem m_TradeSystem;
        
        
        public bool IsPanelVisible { get; set; }
		EntityQuery m_CityQuery;
		EntityQuery m_TradeParameterQuery;
		EntityQuery m_StorageGroupQuery;
        public static readonly int kUpdatesPerDay = 128;
	    private static readonly float kRefreshRate = 0.01f;

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
	        return 262144 / kUpdatesPerDay;
        }
        protected override void OnCreate()
        {
            base.OnCreate();
            m_ImageSystem = World.GetOrCreateSystemManaged<ImageSystem>();
            m_ResourceSystem = World.GetExistingSystemManaged<ResourceSystem>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_CommercialDemandSystem = World.GetOrCreateSystemManaged<CommercialDemandSystem>();
            m_IndustrialDemandSystem = World.GetOrCreateSystemManaged<IndustrialDemandSystem>();
            m_CountCompanyDataSystem = World.GetOrCreateSystemManaged<CountCompanyDataSystem>();
            m_TradeSystem = World.GetOrCreateSystemManaged<TradeSystem>();
            
            
			

		    m_CityQuery = SystemAPI.QueryBuilder()
		        .WithAll<City>()
		        .Build();

		    m_TradeParameterQuery = SystemAPI.QueryBuilder()
		        .WithAll<OutsideTradeParameterData>()
		        .Build();

		    m_StorageGroupQuery = SystemAPI.QueryBuilder()
		        .WithAll<StorageCompany, OutsideConnection, PrefabRef, Resources, TradeCost>()
		        .WithNone<Deleted, Temp>()
		        .Build();
		    RequireForUpdate(m_CityQuery);
		    RequireForUpdate(m_TradeParameterQuery);
		    RequireForUpdate(m_StorageGroupQuery);
        }
        protected override void OnUpdate()
        {
            if (!IsPanelVisible ) return;
            UpdateAllTradeCosts();
        }
        public void UpdateAllTradeCosts()
        {
            // Get production and consumption data
           var production = m_CountCompanyDataSystem.GetProduction(out var deps1);
            var industrial = m_IndustrialDemandSystem.GetConsumption(out var deps2);
            var commercial = m_CommercialDemandSystem.GetConsumption(out var deps3);
            
            // Wait for all data to be ready
            JobHandle.CompleteAll(ref deps1, ref deps2, ref deps3);
            
            
                ResourceIterator iterator = ResourceIterator.GetIterator();
                while (iterator.Next())
                {
                    if (!ShouldProcessResource(iterator.resource))
                        continue;
                        
                    Resource resource = iterator.resource;
                     var index = EconomyUtils.GetResourceIndex(resource);
                    
                    // Get production and consumption for this resource
                    int resourceProduction = production[index];
                    int resourceIndustrialConsumption = industrial[index];
                    int resourceCommercialConsumption = commercial[index];
                    int totalConsumption = resourceIndustrialConsumption + resourceCommercialConsumption;
                    
                    // Calculate import/export amounts based on the same logic as OutsideConnectionsInfoviewUISystem
                    int satisfied = math.min(totalConsumption, resourceProduction);
                    int industrialSatisfied = math.min(resourceIndustrialConsumption, satisfied);
                    int unsatisfiedIndustrial = resourceIndustrialConsumption - industrialSatisfied;
                    int commercialSatisfied = math.min(resourceCommercialConsumption, satisfied - industrialSatisfied);
                    
                    int importAmount = resourceCommercialConsumption - commercialSatisfied + unsatisfiedIndustrial;
                    int exportAmount = resourceProduction - satisfied;
                    var cityEntity = m_CityQuery.GetSingletonEntity();
                    DynamicBuffer<CityModifier> cityEffects = EntityManager.GetBuffer<CityModifier>(cityEntity);
                    // Get trade costs
                    var tradeCosts1 = m_TradeSystem.GetTradePrice(resource, OutsideConnectionTransferType.Road, true, cityEffects);
                    var tradeCosts2 = m_TradeSystem.GetTradePrice(resource, OutsideConnectionTransferType.Road, false, cityEffects);
                    
                    if (!m_TradeCosts.TryGetValue(iterator.resource, out ResourceTradeCost tradeCost))
                    {
                        string iconPath = GetResourceIconPath(iterator.resource);
                        tradeCost = new ResourceTradeCost
                        {
                            Resource = iterator.resource.ToString(),
                            ResourceIcon = iconPath,
                            Count = 1,
                            BuyCost = tradeCosts1,
                            SellCost = tradeCosts2,
                            ImportAmount = importAmount,
                            ExportAmount = exportAmount
                        };
                        m_TradeCosts[iterator.resource] = tradeCost;
                    }
                    else
                    {
                        // Update existing trade cost with new import/export amounts
                        tradeCost.BuyCost = tradeCosts1;
                        tradeCost.SellCost = tradeCosts2;
                        tradeCost.ImportAmount = importAmount;
                        tradeCost.ExportAmount = exportAmount;
                        m_TradeCosts[iterator.resource] = tradeCost;
                    }
                }
            
        }
        private static bool ShouldProcessResource(Resource resource)
        {
            // Exclude Money and any other resources you don't want
            return resource != Resource.Money && 
                   resource != Resource.NoResource;
        }
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
        public IEnumerable<ResourceTradeCost> GetSortedResourceTradeCosts()
        {
            // Start with the basic filtered collection
            var query = m_TradeCosts.Values
                .Where(x => x.ImportAmount > 0 || x.ExportAmount > 0);
            
            // Apply sorting in order of precedence (first criteria has lowest precedence)
            if (m_resourceNameEnum != ResourceNameEnum.Off)
            {
                query = m_resourceNameEnum == ResourceNameEnum.Ascending
                    ? query.OrderBy(x => x.Resource)
                    : query.OrderByDescending(x => x.Resource);
            }
            
            if (m_profitMarginEnum != ProfitMarginEnum.Off)
            {
                query = m_profitMarginEnum == ProfitMarginEnum.Ascending
                    ? query.OrderBy(x => (x.SellCost - x.BuyCost) / (x.BuyCost > 0 ? x.BuyCost : 1))
                    : query.OrderByDescending(x => (x.SellCost - x.BuyCost) / (x.BuyCost > 0 ? x.BuyCost : 1));
            }
            
            if (m_profitEnum != ProfitEnum.Off)
            {
                query = m_profitEnum == ProfitEnum.Ascending
                    ? query.OrderBy(x => x.SellCost * x.ExportAmount - x.BuyCost * x.ImportAmount)
                    : query.OrderByDescending(x => x.SellCost * x.ExportAmount - x.BuyCost * x.ImportAmount);
            }
            
            if (m_exportAmountEnum != ExportAmountEnum.Off)
            {
                query = m_exportAmountEnum == ExportAmountEnum.Ascending
                    ? query.OrderBy(x => x.ExportAmount)
                    : query.OrderByDescending(x => x.ExportAmount);
            }
            
            if (m_importAmountEnum != ImportAmountEnum.Off)
            {
                query = m_importAmountEnum == ImportAmountEnum.Ascending
                    ? query.OrderBy(x => x.ImportAmount)
                    : query.OrderByDescending(x => x.ImportAmount);
            }
            
            if (m_sellCostEnum != SellCostEnum.Off)
            {
                query = m_sellCostEnum == SellCostEnum.Ascending
                    ? query.OrderBy(x => x.SellCost)
                    : query.OrderByDescending(x => x.SellCost);
            }
            
            if (m_BuyCostEnum != BuyCostEnum.Off)
            {
                query = m_BuyCostEnum == BuyCostEnum.Ascending
                    ? query.OrderBy(x => x.BuyCost)
                    : query.OrderByDescending(x => x.BuyCost);
            }
            
            // If no sorting was applied, use the default sort
            if (m_BuyCostEnum == BuyCostEnum.Off && 
                m_sellCostEnum == SellCostEnum.Off && 
                m_importAmountEnum == ImportAmountEnum.Off && 
                m_exportAmountEnum == ExportAmountEnum.Off && 
                m_profitEnum == ProfitEnum.Off && 
                m_profitMarginEnum == ProfitMarginEnum.Off && 
                m_resourceNameEnum == ResourceNameEnum.Off)
            {
                query = query.OrderByDescending(x => x.ImportAmount + x.ExportAmount)
                            .ThenBy(x => x.Resource);
            }
            
            return query;
        }
    }
}