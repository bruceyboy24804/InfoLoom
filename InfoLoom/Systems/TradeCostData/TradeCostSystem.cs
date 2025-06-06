using System;
using System.Collections.Generic;
using System.Linq;
using Game;
using Game.Economy;
using Game.Prefabs;
using Game.Simulation;
using Game.UI;
using InfoLoomTwo.Domain.DataDomain;
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
        public bool ForceUpdate { get; private set; }
        public int TotalImports => m_ResourceTradeCosts.Values.Sum(x => x.ImportAmount);
        public int TotalExports => m_ResourceTradeCosts.Values.Sum(x => x.ExportAmount);
        
        public void ForceUpdateOnce() => ForceUpdate = true;
        
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
            if (Mod.setting.CustomUpdateInterval)
            {
                return Mod.setting.UpdateInterval;
            }
            return 512;
        }

        protected override void OnUpdate()
        {
            if (!IsPanelVisible || !ForceUpdate) return;
            
            UpdateAllResourceData();
            ForceUpdate = false;
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
                
                // Debug logging to see what's happening
                Debug.Log($"Resource: {resource}, IconPath: '{iconPath}'");
                
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
            
            // Debug: Log how many resources we processed
            Debug.Log($"TradeCostSystem: Processed {m_ResourceTradeCosts.Count} resources");
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
            // Create a list from the dictionary values to sort
            var tradeCosts = m_ResourceTradeCosts.Values.ToList();

            // Create the comparison functions as Func<T,T,int> instead of Comparison<T>
            Func<ResourceTradeCost, ResourceTradeCost, int> resourceNameComparer = (a, b) => 
                string.Compare(a.Resource, b.Resource, StringComparison.Ordinal);
            
            Func<ResourceTradeCost, ResourceTradeCost, int> buyCostComparer = (a, b) => 
                a.BuyCost.CompareTo(b.BuyCost);
            
            Func<ResourceTradeCost, ResourceTradeCost, int> sellCostComparer = (a, b) => 
                a.SellCost.CompareTo(b.SellCost);
            
            Func<ResourceTradeCost, ResourceTradeCost, int> profitComparer = (a, b) => 
                (b.SellCost - b.BuyCost).CompareTo(a.SellCost - a.BuyCost);
            
            Func<ResourceTradeCost, ResourceTradeCost, int> profitMarginComparer = (a, b) => {
                float marginA = a.BuyCost > 0 ? (a.SellCost - a.BuyCost) / a.BuyCost : 0;
                float marginB = b.BuyCost > 0 ? (b.SellCost - b.BuyCost) / b.BuyCost : 0;
                return marginB.CompareTo(marginA);
            };
            
            Func<ResourceTradeCost, ResourceTradeCost, int> importAmountComparer = (a, b) => 
                a.ImportAmount.CompareTo(b.ImportAmount);
            
            Func<ResourceTradeCost, ResourceTradeCost, int> exportAmountComparer = (a, b) => 
                a.ExportAmount.CompareTo(b.ExportAmount);

            // Create the comparer using TradeCostSortingUtility
            var comparer = Utils.TradeCostSortingUtility.CreateComparer(
                resourceNameComparer,
                buyCostComparer,
                sellCostComparer,
                profitComparer,
                profitMarginComparer,
                importAmountComparer,
                exportAmountComparer);

            // Sort the list using the created comparer
            tradeCosts.Sort(comparer);

            return tradeCosts;
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
                    Debug.LogWarning($"No prefab found for resource: {resource}");
                    return GetFallbackIcon(resource);
                }
                
                // Get icon from ImageSystem
                string icon = m_ImageSystem.GetIconOrGroupIcon(resourcePrefab);
                if (string.IsNullOrEmpty(icon))
                {
                    Debug.LogWarning($"No icon found for resource: {resource}");
                    return GetFallbackIcon(resource);
                }
                
                return icon;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error getting icon for resource {resource}: {e.Message}");
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
    }    
}