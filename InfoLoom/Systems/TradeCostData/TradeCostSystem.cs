using System;
using System.Collections.Generic;
using System.Linq;
using Game;
using Game.Economy;
using Game.Prefabs;
using Game.Simulation;
using Game.UI;
using InfoLoomTwo.Domain.DataDomain;
using InfoLoomTwo.Domain.DataDomain.Enums.TradeCostEnums;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace InfoLoomTwo.Systems.TradeCostData
{
    public partial class TradeCostSystem : GameSystemBase
    {
        private Dictionary<Resource, ResourceTradeCost> m_ResourceTradeCosts = new();
        
        // Core systems we need
        private TradeSystem m_TradeSystem;
        private CommercialDemandSystem m_CommercialDemandSystem;
        private IndustrialDemandSystem m_IndustrialDemandSystem;
        private CountCompanyDataSystem m_CountCompanyDataSystem;
        private ResourceSystem m_ResourceSystem;
        private ImageSystem m_ImageSystem;
        private PrefabSystem m_PrefabSystem;
        
        // Simple properties
        public bool IsPanelVisible { get; set; }
        
        public int TotalImports => m_ResourceTradeCosts.Values.Sum(x => x.ImportAmount);
        public int TotalExports => m_ResourceTradeCosts.Values.Sum(x => x.ExportAmount);
        public BuyCostEnum m_BuyCostEnum = BuyCostEnum.Off;
        public ExportAmountEnum m_exportAmountEnum = ExportAmountEnum.Off;
        public ImportAmountEnum m_importAmountEnum = ImportAmountEnum.Off;
        public ProfitEnum m_profitEnum = ProfitEnum.Off;
        public ProfitMarginEnum m_profitMarginEnum = ProfitMarginEnum.Off;
        public ResourceNameEnum m_resourceNameEnum = ResourceNameEnum.Off;
        public SellCostEnum m_sellCostEnum = SellCostEnum.Off;
        protected override void OnCreate()
        {
            // Get all required systems in one go
            m_TradeSystem = World.GetOrCreateSystemManaged<TradeSystem>();
            m_CommercialDemandSystem = World.GetOrCreateSystemManaged<CommercialDemandSystem>();
            m_IndustrialDemandSystem = World.GetOrCreateSystemManaged<IndustrialDemandSystem>();
            m_CountCompanyDataSystem = World.GetOrCreateSystemManaged<CountCompanyDataSystem>();
            m_ResourceSystem = World.GetOrCreateSystemManaged<ResourceSystem>();
            m_ImageSystem = World.GetOrCreateSystemManaged<ImageSystem>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
           
            
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 1024;
        }

        protected override void OnUpdate()
        {
            if (!IsPanelVisible ) return;
            UpdateAllResourceData();
        }

        private void UpdateAllResourceData()
        {
            m_ResourceTradeCosts.Clear();
            
            // Get consumption/production data
            var production = m_CountCompanyDataSystem.GetProduction(out var deps1);
            var industrial = m_IndustrialDemandSystem.GetConsumption(out var deps2);
            var commercial = m_CommercialDemandSystem.GetConsumption(out var deps3);
            
            JobHandle.CompleteAll(ref deps1, ref deps2, ref deps3);
            
            // Get city effects once
            var cityEntity = GetEntityQuery(ComponentType.ReadOnly<Game.City.City>()).GetSingletonEntity();
            var cityEffects = EntityManager.GetBuffer<Game.City.CityModifier>(cityEntity);
            
            // Process all resources in one iterator
            var iterator = ResourceIterator.GetIterator();
            while (iterator.Next())
            {
                if (!ShouldProcessResource(iterator.resource))
                    continue;
                var resource = iterator.resource;
                var index = EconomyUtils.GetResourceIndex(resource);
                
                // Skip if no meaningful data
                if (production[index] == 0 && industrial[index] == 0 && commercial[index] == 0)
                    continue;
                
                var (imports, exports) = CalculateTradeFlow(production[index], industrial[index], commercial[index]);
                
                // Get icon path with better error handling
                string iconPath = GetResourceIconPath(resource);
                m_ResourceTradeCosts[resource] = new ResourceTradeCost
                {
                    Resource = resource.ToString(),
                    ResourceIcon = iconPath,
                    Count = 1,
                    BuyCost = GetTradeCost(resource, true, cityEffects),
                    SellCost = GetTradeCost(resource, false, cityEffects),
                    ImportAmount = imports,
                    ExportAmount = exports
                };
            }
        }

        private float GetTradeCost(Resource resource, bool import, DynamicBuffer<Game.City.CityModifier> cityEffects)
        {
            var cost = m_TradeSystem.GetTradePrice(resource, OutsideConnectionTransferType.Road, import, cityEffects);
            return (float)Math.Round(cost, 2);
        }

        private (int imports, int exports) CalculateTradeFlow(int production, int industrial, int commercial)
        {
            var totalDemand = industrial + commercial;
            var deficit = Math.Max(0, totalDemand - production);
            var surplus = Math.Max(0, production - totalDemand);
            
            return (TruncateLargeNumbers(deficit), TruncateLargeNumbers(surplus));
        }

        private static int TruncateLargeNumbers(int value) =>
            value < 100000 ? value : 
            ((value + 50000) / 100000) * 10000; // Simple 5-digit truncation

        public IEnumerable<ResourceTradeCost> GetSortedResourceTradeCosts()
        {
            // Start with the basic filtered collection
            var query = m_ResourceTradeCosts.Values
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

        private string GetResourceIconPath(Resource resource)
        {
            try
            {
                // Handle Money separately
                if (resource == Resource.Money)
                {
                    return "Media/Game/Icons/Money.svg";
                }
                
                // Get resource prefab
                Entity resourcePrefab = m_ResourceSystem.GetPrefab(resource);
                if (resourcePrefab == Entity.Null)
                {
                    return GetFallbackIcon(resource);
                }
                
                // Get icon from ImageSystem
                string icon = m_ImageSystem.GetIconOrGroupIcon(resourcePrefab);
                if (string.IsNullOrEmpty(icon))
                {
                    return GetFallbackIcon(resource);
                }
                
                return icon;
            }
            catch (Exception e)
            {
                return GetFallbackIcon(resource);
            }
        }

        private string GetFallbackIcon(Resource resource)
        {
            // Return a fallback icon path based on resource type
            switch (resource)
            {
                case Resource.Food:
                case Resource.ConvenienceFood:
                case Resource.Meals:
                case Resource.Vegetables:
                case Resource.Grain:
                    return "Media/Game/Icons/ResourceInEditor.svg";
                case Resource.Wood:
                case Resource.Timber:
                case Resource.Paper:
                    return "Media/Game/Icons/ResourceInEditor.svg";
                case Resource.Oil:
                case Resource.Petrochemicals:
                case Resource.Coal:
                    return "Media/Game/Icons/ResourceInEditor.svg";
                default:
                    return "Media/Game/Icons/ResourceInEditor.svg"; // Generic fallback
            }
        }
        
        private static bool ShouldProcessResource(Resource resource)
        {
            // Exclude Money and any other resources you don't want
            return resource != Resource.Money && 
                   resource != Resource.NoResource;
        }
        public int CompareByBuyCost(ResourceTradeCost x, ResourceTradeCost y)
        {
            switch (m_BuyCostEnum)
            {
                case BuyCostEnum.Ascending:
                    return x.BuyCost.CompareTo(y.BuyCost);
                case BuyCostEnum.Descending:
                    return y.BuyCost.CompareTo(x.BuyCost);
                default:
                    return 0;
            }
        }
        public int CompareByExportAmount(ResourceTradeCost x, ResourceTradeCost y)
        {
            switch (m_exportAmountEnum)
            {
                case ExportAmountEnum.Ascending:
                    return x.ExportAmount.CompareTo(y.ExportAmount);
                case ExportAmountEnum.Descending:
                    return y.ExportAmount.CompareTo(x.ExportAmount);
                default:
                    return 0;
            }
        }
        public int CompareByImportAmount(ResourceTradeCost x, ResourceTradeCost y)
        {
            switch (m_importAmountEnum)
            {
                case ImportAmountEnum.Ascending:
                    return x.ImportAmount.CompareTo(y.ImportAmount);
                case ImportAmountEnum.Descending:
                    return y.ImportAmount.CompareTo(x.ImportAmount);
                default:
                    return 0;
            }
        }
        public int CompareByProfit(ResourceTradeCost x, ResourceTradeCost y)
        {
            switch (m_profitEnum)
            {
                case ProfitEnum.Ascending:
                    return (x.ExportAmount - x.ImportAmount).CompareTo(y.ExportAmount - y.ImportAmount);
                case ProfitEnum.Descending:
                    return (y.ExportAmount - y.ImportAmount).CompareTo(x.ExportAmount - x.ImportAmount);
                default:
                    return 0;
            }
        }
        public int CompareByProfitMargin(ResourceTradeCost x, ResourceTradeCost y)
        {
            switch (m_profitMarginEnum)
            {
                case ProfitMarginEnum.Ascending:
                    return (x.ExportAmount - x.ImportAmount).CompareTo(y.ExportAmount - y.ImportAmount);
                case ProfitMarginEnum.Descending:
                    return (y.ExportAmount - y.ImportAmount).CompareTo(x.ExportAmount - x.ImportAmount);
                default:
                    return 0;
            }
        }
        public int CompareByResourceName(ResourceTradeCost x, ResourceTradeCost y)
        {
            switch (m_resourceNameEnum)
            {
                case ResourceNameEnum.Ascending:
                    return string.Compare(x.Resource, y.Resource, StringComparison.Ordinal);
                case ResourceNameEnum.Descending:
                    return string.Compare(y.Resource, x.Resource, StringComparison.Ordinal);
                default:
                    return 0;
            }
        }
        public int CompareBySellCost(ResourceTradeCost x, ResourceTradeCost y)
        {
            switch (m_sellCostEnum)
            {
                case SellCostEnum.Ascending:
                    return x.SellCost.CompareTo(y.SellCost);
                case SellCostEnum.Descending:
                    return y.SellCost.CompareTo(x.SellCost);
                default:
                    return 0;
            }
        }    
    }    
}