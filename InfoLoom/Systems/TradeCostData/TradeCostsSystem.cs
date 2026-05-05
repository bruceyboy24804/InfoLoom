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
using Game.Debug;
using Game.Economy;
using Game.Prefabs;
using Game.Serialization.DataMigration;
using Game.Simulation;
using Game.Tools;
using Game.UI;
using InfoLoomTwo.Extensions;
using InfoLoomTwo.Systems.TradeCostData.TradeCostDomain;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using OutsideConnection = Game.Objects.OutsideConnection;
using Resources = Game.Economy.Resources;
using StorageCompany = Game.Companies.StorageCompany;

namespace InfoLoomTwo.Systems.TradeCostData
{
    public partial class TradeCostsSystem : ExtendedUISystemBase
    {
        private Dictionary<Resource, ResourceTradeCost> m_TradeCosts = new ();

        // Trade balance tracking
        private NativeArray<int> m_TradeBalances;
        private NativeArray<int> m_ImportAmounts;
        private NativeArray<int> m_ExportAmounts;

        // Sorting state
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

        // Bindings
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

            // Initialize native arrays for tracking
            m_TradeBalances = new NativeArray<int>(EconomyUtils.ResourceCount, Allocator.Persistent);
            m_ImportAmounts = new NativeArray<int>(EconomyUtils.ResourceCount, Allocator.Persistent);
            m_ExportAmounts = new NativeArray<int>(EconomyUtils.ResourceCount, Allocator.Persistent);

            // Create bindings
            m_TradeCostsBinding = CreateBinding("TradeCostsData", new List<ResourceTradeCost>());
            _tCPVBinding = new ValueBinding<bool>("InfoLoomTwo", "TradeCostsOpen", false);
            AddBinding(_tCPVBinding);
            AddBinding(new TriggerBinding<bool>("InfoLoomTwo", "TradeCostsOpen", OnVisibilityChanged));

            // Create sorting triggers
            CreateTrigger<SortingEnum>("SetBuyCost", OnBuyCostChanged);
            CreateTrigger<SortingEnum>("SetSellCost", OnSellCostChanged);
            CreateTrigger<SortingEnum>("SetImportAmount", OnImportAmountChanged);
            CreateTrigger<SortingEnum>("SetExportAmount", OnExportAmountChanged);
            CreateTrigger<SortingEnum>("SetProfit", OnProfitChanged);
            CreateTrigger<SortingEnum>("SetProfitMargin", OnProfitMarginChanged);
            CreateTrigger<SortingEnum>("SetResourceName", OnResourceNameChanged);

            // Create value bindings for sorting enums
            CreateBinding("BuyCost", () => m_buyCostSorting);
            CreateBinding("SellCost", () => m_sellCostSorting);
            CreateBinding("ImportAmount", () => m_importAmountSorting);
            CreateBinding("ExportAmount", () => m_exportAmountSorting);
            CreateBinding("Profit", () => m_profitSorting);
            CreateBinding("ProfitMargin", () => m_profitMarginSorting);
            CreateBinding("ResourceName", () => m_resourceNameSorting);

            // Create outside connection type binding
            outsideConnectionTypeBinding = CreateBinding("OutsideConnectionType", "SetOutsideConnectionType", OutsideConnectionType.All, OnOutsideConnectionTypeChanged);

        }

        protected override void OnDestroy()
        {
            // Dispose native arrays
            m_TradeBalances.Dispose();
            m_ImportAmounts.Dispose();
            m_ExportAmounts.Dispose();
            base.OnDestroy();
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
            // Reset history when connection type changes since prices change
            
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

            EntityQuery m_CityQuery = SystemAPI.QueryBuilder().WithAll<City>().Build();
            RequireForUpdate(m_CityQuery);
            var cityEntity = m_CityQuery.GetSingletonEntity();
            EntityManager.TryGetBuffer(cityEntity, isReadOnly: true, out DynamicBuffer<CityModifier> modifier);

            // Get selected connection type
            OutsideConnectionType selectedConnectionType = outsideConnectionTypeBinding.Value;
            OutsideConnectionTransferType transferType = OutsideConnectionTypeUtils.GetTransferType(selectedConnectionType);

            // Reset tracking arrays
            for (int i = 0; i < m_ImportAmounts.Length; i++)
            {
                m_ImportAmounts[i] = 0;
                m_ExportAmounts[i] = 0;
            }

            // Query storage entities
            EntityQuery m_StorageGroupQuery = SystemAPI.QueryBuilder()
                .WithAll<StorageCompany, OutsideConnection, PrefabRef, Resources, TradeCost>()
                .WithNone<Deleted, Temp>()
                .Build();
            RequireForUpdate(m_StorageGroupQuery);

            var storageEntities = m_StorageGroupQuery.ToEntityArray(Allocator.Temp);

            foreach (var storageEntity in storageEntities)
            {
                var prefabRef = EntityManager.GetComponentData<PrefabRef>(storageEntity);
                Entity prefab = prefabRef.m_Prefab;

                if (!EntityManager.HasComponent<StorageCompanyData>(prefab))
                    continue;

                if (!EntityManager.HasComponent<OutsideConnectionData>(prefab))
                    continue;

                var connectionData = EntityManager.GetComponentData<OutsideConnectionData>(prefab);

                // Only process if matches selected connection type
                if ((connectionData.m_Type & transferType) == OutsideConnectionTransferType.None)
                    continue;

                if (!EntityManager.HasBuffer<Resources>(storageEntity))
                    continue;

                var resourceBuffer = EntityManager.GetBuffer<Resources>(storageEntity);
                var storageData = EntityManager.GetComponentData<StorageCompanyData>(prefab);

                // Calculate storage limit with upgrades
                StorageLimitData limit = default;
                if (EntityManager.HasComponent<StorageLimitData>(prefab))
                {
                    limit = EntityManager.GetComponentData<StorageLimitData>(prefab);

                    // Account for upgrades
                    if (EntityManager.HasBuffer<InstalledUpgrade>(storageEntity))
                    {
                        var upgrades = EntityManager.GetBuffer<InstalledUpgrade>(storageEntity);
                        foreach (var upgrade in upgrades)
                        {
                            Entity upgradePrefab = upgrade.m_Upgrade;
                            if (EntityManager.HasComponent<PrefabRef>(upgradePrefab))
                            {
                                Entity upgradeData = EntityManager.GetComponentData<PrefabRef>(upgradePrefab).m_Prefab;
                                if (EntityManager.HasComponent<StorageLimitData>(upgradeData))
                                {
                                    var upgradeLimit = EntityManager.GetComponentData<StorageLimitData>(upgradeData);
                                    limit.m_Limit += upgradeLimit.m_Limit;
                                }
                            }
                        }
                    }
                }

                ResourceIterator iterator = ResourceIterator.GetIterator();
                int resourceCount = EconomyUtils.CountResources(storageData.m_StoredResources);

                while (iterator.Next())
                {
                    if ((storageData.m_StoredResources & iterator.resource) != Resource.NoResource)
                    {
                        int amount = EconomyUtils.GetResources(iterator.resource, resourceBuffer);
                        int index = EconomyUtils.GetResourceIndex(iterator.resource);

                        // Calculate capacity per resource
                        int capacityPerResource = resourceCount > 0 ? limit.m_Limit / resourceCount : 0;
                        int halfCapacity = capacityPerResource / 2;

                        // Track import/export based on deviation from half capacity
                        if (amount < halfCapacity)
                            m_ImportAmounts[index] += halfCapacity - amount;
                        else if (amount > halfCapacity)
                            m_ExportAmounts[index] += amount - halfCapacity;
                    }
                }
            }

            storageEntities.Dispose();

            // Process all resources
            ResourceIterator mainIterator = ResourceIterator.GetIterator();
            while (mainIterator.Next())
            {
                if (!ShouldProcessResource(mainIterator.resource))
                    continue;

                Resource resource = mainIterator.resource;
                var index = EconomyUtils.GetResourceIndex(resource);

                int resourceProduction = production[index];
                int resourceIndustrialConsumption = industrial[index];
                int resourceCommercialConsumption = commercial[index];
                int totalConsumption = resourceIndustrialConsumption + resourceCommercialConsumption;

                int satisfied = math.min(totalConsumption, resourceProduction);
                int industrialSatisfied = math.min(resourceIndustrialConsumption, satisfied);
                int unsatisfiedIndustrial = resourceIndustrialConsumption - industrialSatisfied;
                int commercialSatisfied = math.min(resourceCommercialConsumption, satisfied - industrialSatisfied);

                // Calculate theoretical import/export needs
                float theoreticalImport = resourceCommercialConsumption - commercialSatisfied + unsatisfiedIndustrial;
                float theoreticalExport = resourceProduction - satisfied;

                // Blend with actual storage data
                float importAmount = math.lerp(theoreticalImport, m_ImportAmounts[index], 0.3f);
                float exportAmount = math.lerp(theoreticalExport, m_ExportAmounts[index], 0.3f);

                // Update trade balance
                int netTrade = (int)(exportAmount - importAmount);
                float refreshRate = 0.1f;
                m_TradeBalances[index] = (int)math.lerp(m_TradeBalances[index], netTrade, refreshRate);

                // Get trade costs
                float finalBuyPrice = m_TradeSystem.GetBestTradePriceAmongTypes(resource, transferType, true, modifier);
                float finalSellPrice = m_TradeSystem.GetBestTradePriceAmongTypes(resource, transferType, false, modifier);

                if (!m_TradeCosts.TryGetValue(mainIterator.resource, out ResourceTradeCost tradeCost))
                {
                    string iconPath = GetResourceIconPath(mainIterator.resource);
                    tradeCost = new ResourceTradeCost
                    {
                        Resource = mainIterator.resource.ToString(),
                        ResourceIcon = iconPath,
                        Count = 1,
                        BuyCost = finalBuyPrice,
                        SellCost = finalSellPrice,
                        ImportAmount = importAmount,
                        ExportAmount = exportAmount
                    };
                    m_TradeCosts[mainIterator.resource] = tradeCost;
                }
                else
                {
                    tradeCost.BuyCost = finalBuyPrice;
                    tradeCost.SellCost = finalSellPrice;
                    tradeCost.ImportAmount = importAmount;
                    tradeCost.ExportAmount = exportAmount;
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
            if (Enum.TryParse(resourceName, out Resource resource))
            {
                if (Enum.TryParse(resource.ToString(), out DefaultSorting sortOrder))
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
