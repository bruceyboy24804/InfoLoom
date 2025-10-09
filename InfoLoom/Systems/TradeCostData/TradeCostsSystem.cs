using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Entities;
using Colossal.UI.Binding;
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
using InfoLoomTwo.Extensions;
using InfoLoomTwo.Systems.TradeCostData.TradeCostDomain;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using OutsideConnection = Game.Objects.OutsideConnection;
using Resources = Game.Economy.Resources;
using StorageCompany = Game.Companies.StorageCompany;

namespace InfoLoomTwo.Systems.TradeCostData
{
    public partial class TradeCostsSystem : ExtendedUISystemBase
    {
        private Dictionary<Resource, ResourceTradeCost> m_TradeCosts = new Dictionary<Resource, ResourceTradeCost>();

        // Restore the old enum fields for sorting state
        private BuyCostEnum m_buyCostEnum = BuyCostEnum.Off;
        private SellCostEnum m_sellCostEnum = SellCostEnum.Off;
        private ImportAmountEnum m_importAmountEnum = ImportAmountEnum.Off;
        private ExportAmountEnum m_exportAmountEnum = ExportAmountEnum.Off;
        private ProfitEnum m_profitEnum = ProfitEnum.Off;
        private ProfitMarginEnum m_profitMarginEnum = ProfitMarginEnum.Off;
        private ResourceNameEnum m_resourceNameEnum = ResourceNameEnum.Off;
       

        // System references
        private ImageSystem m_ImageSystem;
        private ResourceSystem m_ResourceSystem;
        private PrefabSystem m_PrefabSystem;
        private CommercialDemandSystem m_CommercialDemandSystem;
        private IndustrialDemandSystem m_IndustrialDemandSystem;
        private CountCompanyDataSystem m_CountCompanyDataSystem;
        private TradeSystem m_TradeSystem;
        private UIUpdateState _uiUpdateState;

        // Entity queries
        EntityQuery m_CityQuery;
        EntityQuery m_TradeParameterQuery;
        EntityQuery m_StorageGroupQuery;

        // Simplified bindings using base class
        private ValueBindingHelper<List<ResourceTradeCost>> m_TradeCostsBinding;
        private ValueBinding<bool> _tCPVBinding;

        protected override void OnCreate()
        {
            base.OnCreate();

            // Initialize systems
            m_ImageSystem = World.GetOrCreateSystemManaged<ImageSystem>();
            m_ResourceSystem = World.GetExistingSystemManaged<ResourceSystem>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_CommercialDemandSystem = World.GetOrCreateSystemManaged<CommercialDemandSystem>();
            m_IndustrialDemandSystem = World.GetOrCreateSystemManaged<IndustrialDemandSystem>();
            m_CountCompanyDataSystem = World.GetOrCreateSystemManaged<CountCompanyDataSystem>();
            m_TradeSystem = World.GetOrCreateSystemManaged<TradeSystem>();
            _uiUpdateState = UIUpdateState.Create(World, 512);

            // Initialize queries
            m_CityQuery = SystemAPI.QueryBuilder().WithAll<City>().Build();
            m_TradeParameterQuery = SystemAPI.QueryBuilder().WithAll<OutsideTradeParameterData>().Build();
            m_StorageGroupQuery = SystemAPI.QueryBuilder()
                .WithAll<StorageCompany, OutsideConnection, PrefabRef, Resources, TradeCost>()
                .WithNone<Deleted, Temp>()
                .Build();

            RequireForUpdate(m_CityQuery);
            RequireForUpdate(m_TradeParameterQuery);
            RequireForUpdate(m_StorageGroupQuery);

            // Create simplified bindings
            m_TradeCostsBinding = CreateBinding("TradeCostsData", new List<ResourceTradeCost>());
            _tCPVBinding = new ValueBinding<bool>("InfoLoomTwo", "TradeCostsOpen", false);
             AddBinding(_tCPVBinding);
             AddBinding(new TriggerBinding<bool>("InfoLoomTwo", "TradeCostsOpen", OnVisibilityChanged));

            // Create sorting triggers using the old manual approach
            CreateTrigger<BuyCostEnum>("SetBuyCost", OnBuyCostChanged);
            CreateTrigger<SellCostEnum>("SetSellCost", OnSellCostChanged);
            CreateTrigger<ImportAmountEnum>("SetImportAmount", OnImportAmountChanged);
            CreateTrigger<ExportAmountEnum>("SetExportAmount", OnExportAmountChanged);
            CreateTrigger<ProfitEnum>("SetProfit", OnProfitChanged);
            CreateTrigger<ProfitMarginEnum>("SetProfitMargin", OnProfitMarginChanged);
            CreateTrigger<ResourceNameEnum>("SetResourceName", OnResourceNameChanged);

            // Create value bindings for the sorting enums
            CreateBinding("BuyCost", () => m_buyCostEnum);
            CreateBinding("SellCost", () => m_sellCostEnum);
            CreateBinding("ImportAmount", () => m_importAmountEnum);
            CreateBinding("ExportAmount", () => m_exportAmountEnum);
            CreateBinding("Profit", () => m_profitEnum);
            CreateBinding("ProfitMargin", () => m_profitMarginEnum);
            CreateBinding("ResourceName", () => m_resourceNameEnum);
            

        }

        protected override void OnUpdate()
        {
            if (!_tCPVBinding.value) return;

            if (_uiUpdateState.Advance())
            {
                UpdateAllTradeCosts();
                UpdateSortedData();
            }

            base.OnUpdate();
        }

        // Sorting change handlers
        private void OnBuyCostChanged(BuyCostEnum value)
        {
            m_buyCostEnum = value;
            UpdateSortedData();
        }

        private void OnSellCostChanged(SellCostEnum value)
        {
            m_sellCostEnum = value;
            UpdateSortedData();
        }

        private void OnImportAmountChanged(ImportAmountEnum value)
        {
            m_importAmountEnum = value;
            UpdateSortedData();
        }

        private void OnExportAmountChanged(ExportAmountEnum value)
        {
            m_exportAmountEnum = value;
            UpdateSortedData();
        }

        private void OnProfitChanged(ProfitEnum value)
        {
            m_profitEnum = value;
            UpdateSortedData();
        }

        private void OnProfitMarginChanged(ProfitMarginEnum value)
        {
            m_profitMarginEnum = value;
            UpdateSortedData();
        }

        private void OnResourceNameChanged(ResourceNameEnum value)
        {
            m_resourceNameEnum = value;
            UpdateSortedData();
        }

        private void OnVisibilityChanged(bool isVisible)
        {
            _tCPVBinding.Update(isVisible);
            if (isVisible)
            {
                UpdateAllTradeCosts();
                UpdateSortedData();
            }
        }

        

        private void UpdateSortedData()
        {
            m_TradeCostsBinding.Value = GetSortedResourceTradeCosts().ToList();
        }

        public void UpdateAllTradeCosts()
        {
            // Get production and consumption data
            var production = m_CountCompanyDataSystem.GetProduction(out var deps1);
            var industrial = m_IndustrialDemandSystem.GetConsumption(out var deps2);
            var commercial = m_CommercialDemandSystem.GetConsumption(out var deps3);

            JobHandle.CompleteAll(ref deps1, ref deps2, ref deps3);

            ResourceIterator iterator = ResourceIterator.GetIterator();
            while (iterator.Next())
            {
                if (!ShouldProcessResource(iterator.resource))
                    continue;

                Resource resource = iterator.resource;
                var index = EconomyUtils.GetResourceIndex(resource);

                int resourceProduction = production[index];
                int resourceIndustrialConsumption = industrial[index];
                int resourceCommercialConsumption = commercial[index];
                int totalConsumption = resourceIndustrialConsumption + resourceCommercialConsumption;

                int satisfied = math.min(totalConsumption, resourceProduction);
                int industrialSatisfied = math.min(resourceIndustrialConsumption, satisfied);
                int unsatisfiedIndustrial = resourceIndustrialConsumption - industrialSatisfied;
                int commercialSatisfied = math.min(resourceCommercialConsumption, satisfied - industrialSatisfied);

                int importAmount = resourceCommercialConsumption - commercialSatisfied + unsatisfiedIndustrial;
                int exportAmount = resourceProduction - satisfied;
                var cityEntity = m_CityQuery.GetSingletonEntity();
                DynamicBuffer<CityModifier> cityEffects = EntityManager.GetBuffer<CityModifier>(cityEntity);

                // Get trade costs for all available connection types
                float bestBuyPrice = float.MaxValue;
                float bestSellPrice = 0f;
                var connectionTypes = new[] {
                    OutsideConnectionTransferType.Road,
                    OutsideConnectionTransferType.Train,
                    OutsideConnectionTransferType.Air,
                    OutsideConnectionTransferType.Ship
                };

                foreach (var connectionType in connectionTypes)
                {
                    var buyPrice = m_TradeSystem.GetTradePrice(resource, connectionType, true, cityEffects);
                    var sellPrice = m_TradeSystem.GetTradePrice(resource, connectionType, false, cityEffects);

                    if (buyPrice > 0 && buyPrice < bestBuyPrice)
                        bestBuyPrice = buyPrice;

                    if (sellPrice > bestSellPrice)
                        bestSellPrice = sellPrice;
                }

                float finalBuyPrice = bestBuyPrice == float.MaxValue ?
                    m_TradeSystem.GetTradePrice(resource, OutsideConnectionTransferType.Road, true, cityEffects) : bestBuyPrice;
                float finalSellPrice = bestSellPrice > 0 ? bestSellPrice :
                    m_TradeSystem.GetTradePrice(resource, OutsideConnectionTransferType.Road, false, cityEffects);

                if (!m_TradeCosts.TryGetValue(iterator.resource, out ResourceTradeCost tradeCost))
                {
                    string iconPath = GetResourceIconPath(iterator.resource);
                    tradeCost = new ResourceTradeCost
                    {
                        Resource = iterator.resource.ToString(),
                        ResourceIcon = iconPath,
                        Count = 1,
                        BuyCost = finalBuyPrice,
                        SellCost = finalSellPrice,
                        ImportAmount = importAmount,
                        ExportAmount = exportAmount
                    };
                    m_TradeCosts[iterator.resource] = tradeCost;
                }
                else
                {
                    tradeCost.BuyCost = finalBuyPrice;
                    tradeCost.SellCost = finalSellPrice;
                    tradeCost.ImportAmount = importAmount;
                    tradeCost.ExportAmount = exportAmount;
                    m_TradeCosts[iterator.resource] = tradeCost;
                }
            }
        }

        private static bool ShouldProcessResource(Resource resource)
        {
            return resource != Resource.Money &&
                   resource != Resource.NoResource && 
                   resource != Resource.UnsortedMail && 
                   resource != Resource.OutgoingMail &&
                   resource != Resource.LocalMail &&
                   resource != Resource.Garbage &&
                   resource != Resource.Lodging &&
                   resource != Resource.Entertainment &&
                   resource != Resource.Recreation;
        }
        public static class UnitConversionUtils
        {
            private const float KilogramsPerTon = 1000f;

            public static float KilogramsToTons(float kilograms)
            {
                return kilograms / KilogramsPerTon;
            }

            public static float TonsToKilograms(float tons)
            {
                return tons * KilogramsPerTon;
            }
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
            IEnumerable<ResourceTradeCost> query = m_TradeCosts.Values;

            // Use the enum fields instead of binding values
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

            if (m_buyCostEnum != BuyCostEnum.Off)
            {
                query = m_buyCostEnum == BuyCostEnum.Ascending
                    ? query.OrderBy(x => x.BuyCost)
                    : query.OrderByDescending(x => x.BuyCost);
            }

            // Default sorting when no other sorting is applied
            if (m_buyCostEnum == BuyCostEnum.Off &&
                m_sellCostEnum == SellCostEnum.Off &&
                m_importAmountEnum == ImportAmountEnum.Off &&
                m_exportAmountEnum == ExportAmountEnum.Off &&
                m_profitEnum == ProfitEnum.Off &&
                m_profitMarginEnum == ProfitMarginEnum.Off &&
                m_resourceNameEnum == ResourceNameEnum.Off)
            {
                query = query.OrderBy(x => GetResourcePriority(x.Resource))
                    .ThenBy(x => x.Resource);
            }

            return query;
        }

        private int GetResourcePriority(string resourceName)
        {
            if (Enum.TryParse<Resource>(resourceName, out Resource resource))
            {
                if (Enum.TryParse<DefaultSorting>(resource.ToString(), out DefaultSorting sortOrder))
                {
                    return (int)sortOrder;
                }
            }
            return int.MaxValue;
        }

        public enum DefaultSorting
        {
            Wood, Grain, Livestock, Fish, Vegetables, Cotton,
            Oil, Ore, Coal, Stone, Metals, Steel, Minerals,
            Concrete, Machinery, Petrochemicals, Chemicals,
            Plastics, Pharmaceuticals, Electronics, Vehicles,
            Beverages, ConvenienceFood, Food, Textiles,
            Timber, Paper, Furniture, Software, Telecom,
            Financial, Media, Lodging, Meals, Entertainment, Recreation,
        }
    }
}