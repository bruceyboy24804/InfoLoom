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

        private SortingEnum m_buyCostSorting = SortingEnum.Off;
        private SortingEnum m_sellCostSorting = SortingEnum.Off;
        private SortingEnum m_importAmountSorting = SortingEnum.Off;
        private SortingEnum m_exportAmountSorting = SortingEnum.Off;
        private SortingEnum m_profitSorting = SortingEnum.Off;
        private SortingEnum m_profitMarginSorting = SortingEnum.Off;
        private SortingEnum m_resourceNameSorting = SortingEnum.Off;
        private string m_currentSortColumn = "";

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
        private ValueBindingHelper<OutsideConnectionType> outsideConnectionTypeBinding;

        protected override void OnCreate()
        {
            base.OnCreate();

            // Initialize systems
            m_ImageSystem = World.GetOrCreateSystemManaged<ImageSystem>();
            m_ResourceSystem = World.GetExistingSystemManaged<ResourceSystem>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_CommercialDemandSystem = World.GetOrCreateSystemManaged<CommercialDemandSystem>();
            m_IndustrialDemandSystem = World.GetOrCreateSystemManaged<Game.Simulation.IndustrialDemandSystem>();
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
            CreateTrigger<SortingEnum>("SetBuyCost", OnBuyCostChanged);
            CreateTrigger<SortingEnum>("SetSellCost", OnSellCostChanged);
            CreateTrigger<SortingEnum>("SetImportAmount", OnImportAmountChanged);
            CreateTrigger<SortingEnum>("SetExportAmount", OnExportAmountChanged);
            CreateTrigger<SortingEnum>("SetProfit", OnProfitChanged);
            CreateTrigger<SortingEnum>("SetProfitMargin", OnProfitMarginChanged);
            CreateTrigger<SortingEnum>("SetResourceName", OnResourceNameChanged);

            // Create value bindings for the sorting enums
            CreateBinding("BuyCost", () => m_buyCostSorting);
            CreateBinding("SellCost", () => m_sellCostSorting);
            CreateBinding("ImportAmount", () => m_importAmountSorting);
            CreateBinding("ExportAmount", () => m_exportAmountSorting);
            CreateBinding("Profit", () => m_profitSorting);
            CreateBinding("ProfitMargin", () => m_profitMarginSorting);
            CreateBinding("ResourceName", () => m_resourceNameSorting);

            // Create outside connection type binding and trigger
            outsideConnectionTypeBinding = CreateBinding("OutsideConnectionType", "SetOutsideConnectionType", OutsideConnectionType.Road, OnOutsideConnectionTypeChanged);
            //CreateTrigger<OutsideConnectionType>("SetOutsideConnectionType", OnOutsideConnectionTypeChanged);
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
        private void OnBuyCostChanged(SortingEnum value)
        {
            m_buyCostSorting = value;
            m_currentSortColumn = "BuyCost";
            UpdateSortedData();
        }

        private void OnSellCostChanged(SortingEnum value)
        {
            m_sellCostSorting = value;
            m_currentSortColumn = "SellCost";
            UpdateSortedData();
        }

        private void OnImportAmountChanged(SortingEnum value)
        {
            m_importAmountSorting = value;
            m_currentSortColumn = "ImportAmount";
            UpdateSortedData();
        }

        private void OnExportAmountChanged(SortingEnum value)
        {
            m_exportAmountSorting = value;
            m_currentSortColumn = "ExportAmount";
            UpdateSortedData();
        }

        private void OnProfitChanged(SortingEnum value)
        {
            m_profitSorting = value;
            m_currentSortColumn = "Profit";
            UpdateSortedData();
        }

        private void OnProfitMarginChanged(SortingEnum value)
        {
            m_profitMarginSorting = value;
            m_currentSortColumn = "ProfitMargin";
            UpdateSortedData();
        }

        private void OnResourceNameChanged(SortingEnum value)
        {
            m_resourceNameSorting = value;
            m_currentSortColumn = "ResourceName";
            UpdateSortedData();
        }

        

        private void OnOutsideConnectionTypeChanged(OutsideConnectionType connectionType)
        {
            UpdateAllTradeCosts();
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

                float importAmount = resourceCommercialConsumption - commercialSatisfied + unsatisfiedIndustrial;
                float exportAmount = resourceProduction - satisfied;
                var cityEntity = m_CityQuery.GetSingletonEntity();
                DynamicBuffer<CityModifier> cityEffects = EntityManager.GetBuffer<CityModifier>(cityEntity);

                // Get the selected connection type and convert to OutsideConnectionTransferType
                OutsideConnectionType selectedConnectionType = outsideConnectionTypeBinding.Value;
                OutsideConnectionTransferType transferType = OutsideConnectionTypeUtils.GetTransferType(selectedConnectionType);

                // Get trade costs for the selected connection type
                float finalBuyPrice = m_TradeSystem.GetBestTradePriceAmongTypes(resource, transferType, true, cityEffects);
                float finalSellPrice = m_TradeSystem.GetBestTradePriceAmongTypes(resource, transferType, false, cityEffects);

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

            if (!string.IsNullOrEmpty(m_currentSortColumn))
            {
                var currentSorting = m_currentSortColumn switch
                {
                    "BuyCost" => m_buyCostSorting,
                    "SellCost" => m_sellCostSorting,
                    "ImportAmount" => m_importAmountSorting,
                    "ExportAmount" => m_exportAmountSorting,
                    "Profit" => m_profitSorting,
                    "ProfitMargin" => m_profitMarginSorting,
                    "ResourceName" => m_resourceNameSorting,
                    _ => SortingEnum.Off
                };

                if (currentSorting != SortingEnum.Off)
                {
                    query = m_currentSortColumn switch
                    {
                        "ResourceName" => currentSorting == SortingEnum.Ascending
                            ? query.OrderBy(x => x.Resource)
                            : query.OrderByDescending(x => x.Resource),
                        "BuyCost" => currentSorting == SortingEnum.Ascending
                            ? query.OrderBy(x => x.BuyCost)
                            : query.OrderByDescending(x => x.BuyCost),
                        "SellCost" => currentSorting == SortingEnum.Ascending
                            ? query.OrderBy(x => x.SellCost)
                            : query.OrderByDescending(x => x.SellCost),
                        "ImportAmount" => currentSorting == SortingEnum.Ascending
                            ? query.OrderBy(x => x.ImportAmount)
                            : query.OrderByDescending(x => x.ImportAmount),
                        "ExportAmount" => currentSorting == SortingEnum.Ascending
                            ? query.OrderBy(x => x.ExportAmount)
                            : query.OrderByDescending(x => x.ExportAmount),
                        "Profit" => currentSorting == SortingEnum.Ascending
                            ? query.OrderBy(x => x.SellCost * x.ExportAmount - x.BuyCost * x.ImportAmount)
                            : query.OrderByDescending(x => x.SellCost * x.ExportAmount - x.BuyCost * x.ImportAmount),
                        "ProfitMargin" => currentSorting == SortingEnum.Ascending
                            ? query.OrderBy(x => (x.SellCost - x.BuyCost) / (x.BuyCost > 0 ? x.BuyCost : 1))
                            : query.OrderByDescending(x => (x.SellCost - x.BuyCost) / (x.BuyCost > 0 ? x.BuyCost : 1)),
                        _ => query.OrderBy(x => GetResourcePriority(x.Resource)).ThenBy(x => x.Resource)
                    };
                }
                else
                {
                    query = query.OrderBy(x => GetResourcePriority(x.Resource))
                        .ThenBy(x => x.Resource);
                }
            }
            else
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