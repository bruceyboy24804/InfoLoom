using System;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Companies;
using Game.Economy;
using Game.Vehicles;
using InfoLoomTwo.Extensions;
using Unity.Entities;
using UnityEngine;
using System.Collections.Generic;
using Game.Prefabs;
using Game.Simulation;
using Game.UI;
using Game.UI.InGame;
using System.Linq;
using Game;
using Game.Buildings;
using Game.Citizens;
using Resources = Game.Economy.Resources;
using Game.Zones;

namespace InfoLoomTwo.Systems.Sections
{
    public partial class ILCompanyProfitSection : ExtendedInfoSectionBase
    {
        public class ResourceValueItem
        {
            public string ResourceName { get; set; }
            public int Amount { get; set; }
            public float UnitPrice { get; set; }
            public int TotalValue { get; set; }
        }

        private Entity companyEntity;
        private int _CurrentWorth;
        private int _PreviousWorth;
        private int _ProfitChange;
        private int _Profitability;
        private List<ResourceValueItem> _ResourceValues;
        private int _TotalResourceValue;
        private int _LoadedGoodsValue;
        private int _OwnedVehicles;
        private int _LoadedVehicles;
        private string _IndustrialPricesJson;

        private ResourceSystem m_ResourceSystem;
        protected override string group => nameof(ILCompanyProfitSection);

        protected override void OnCreate()
        {
            base.OnCreate();
            m_InfoUISystem.AddMiddleSection(this);
            m_ResourceSystem = World.GetOrCreateSystemManaged<ResourceSystem>();
        }

        protected override void Reset()
        {
            _CurrentWorth = 0;
            _PreviousWorth = 0;
            _ProfitChange = 0;
            _Profitability = 0;
            _ResourceValues = new List<ResourceValueItem>();
            _TotalResourceValue = 0;
            _LoadedGoodsValue = 0;
            _OwnedVehicles = 0;
            _LoadedVehicles = 0;
            _IndustrialPricesJson = "";
        }

        private bool Visible()
        {
            return CompanyUIUtils.HasCompany(EntityManager, selectedEntity, selectedPrefab, out companyEntity) && !Mod.setting.hideProfitSection;
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            // Match the profitability system update rate (16 times per day)
            return 262144;
        }

        protected override void OnUpdate()
        {
            visible = Visible();
            if (EntityManager.TryGetComponent(companyEntity, out Profitability profitability))
            {
                _PreviousWorth = profitability.m_LastTotalWorth;
                _Profitability = profitability.m_Profitability;

                if (EntityManager.HasBuffer<Game.Economy.Resources>(companyEntity))
                {
                    EntityManager.TryGetBuffer<Game.Economy.Resources>(companyEntity, isReadOnly: true, out var buffer);
                    EntityManager.TryGetBuffer<OwnedVehicle>(companyEntity, isReadOnly: true, out var vehicleBuffer);

                    var layouts = SystemAPI.GetBufferLookup<LayoutElement>(true);
                    var deliveryTrucks = SystemAPI.GetComponentLookup<Game.Vehicles.DeliveryTruck>(true);
                    var resourcePrefabs = m_ResourceSystem.GetPrefabs();
                    var resourceDatas = SystemAPI.GetComponentLookup<ResourceData>(true);

                    // Calculate current worth using the game's own method
                    _CurrentWorth = vehicleBuffer.IsCreated 
                        ? EconomyUtils.GetCompanyTotalWorth(buffer, vehicleBuffer, ref layouts, ref deliveryTrucks, resourcePrefabs, ref resourceDatas)
                        : EconomyUtils.GetCompanyTotalWorth(buffer, resourcePrefabs, ref resourceDatas);

                    // Calculate resource values per resource type with breakdown
                    _ResourceValues = CalculateResourceValueList(buffer, resourcePrefabs, ref resourceDatas);
                    
                    // Calculate total value of all resources
                    _TotalResourceValue = _ResourceValues.Sum(r => r.TotalValue);

                    // Calculate loaded goods value separately (vehicles only)
                    _LoadedGoodsValue = CalculateLoadedGoodsValue(vehicleBuffer, ref layouts, ref deliveryTrucks, resourcePrefabs, ref resourceDatas);

                    // Real-time profit change
                    _ProfitChange = _CurrentWorth - _PreviousWorth;

                    // Vehicle statistics
                    (_OwnedVehicles, _LoadedVehicles) = GetCompanyVehicleStats(vehicleBuffer, ref deliveryTrucks);
                }

                _IndustrialPricesJson = GetIndustrialPricesString();
            }
        }

        protected override void OnProcess() { }

        private List<ResourceValueItem> CalculateResourceValueList(
            DynamicBuffer<Game.Economy.Resources> resources,
            ResourcePrefabs resourcePrefabs,
            ref ComponentLookup<ResourceData> resourceDatas)
        {
            var resourceList = new List<ResourceValueItem>();
            
            for (int i = 0; i < resources.Length; i++)
            {
                Resource resource = resources[i].m_Resource;
                int amount = resources[i].m_Amount;

                if (amount >= 0)
                {
                    float price = EconomyUtils.GetMarketPrice(resource, resourcePrefabs, ref resourceDatas);
                    int totalValue = Mathf.RoundToInt(amount * price);

                    resourceList.Add(new ResourceValueItem
                    {
                        ResourceName = resource.ToString(),
                        Amount = amount,
                        UnitPrice = price,
                        TotalValue = totalValue
                    });
                }
            }

            return resourceList;
        }

        private int CalculateLoadedGoodsValue(
            DynamicBuffer<OwnedVehicle> vehicles,
            ref BufferLookup<LayoutElement> layouts,
            ref ComponentLookup<Game.Vehicles.DeliveryTruck> deliveryTrucks,
            ResourcePrefabs resourcePrefabs,
            ref ComponentLookup<ResourceData> resourceDatas)
        {
            int loadedGoodsValue = 0;
            if (!vehicles.IsCreated) return 0;
            
            for (int i = 0; i < vehicles.Length; i++)
            {
                Entity vehicle = vehicles[i].m_Vehicle;
                if (!deliveryTrucks.HasComponent(vehicle)) continue;

                // Check for vehicle layouts (trailers, etc.)
                if (layouts.HasBuffer(vehicle))
                {
                    var layoutElements = layouts[vehicle];
                    for (int j = 0; j < layoutElements.Length; j++)
                    {
                        Entity layoutVehicle = layoutElements[j].m_Vehicle;
                        if (deliveryTrucks.HasComponent(layoutVehicle))
                        {
                            var truck = deliveryTrucks[layoutVehicle];
                            if ((truck.m_State & DeliveryTruckFlags.Loaded) != 0)
                            {
                                float price = EconomyUtils.GetMarketPrice(truck.m_Resource, resourcePrefabs, ref resourceDatas);
                                loadedGoodsValue += Mathf.RoundToInt(truck.m_Amount * price);
                            }
                        }
                    }
                }
                // Single vehicle without layout
                else if ((deliveryTrucks[vehicle].m_State & DeliveryTruckFlags.Loaded) != 0)
                {
                    var truck = deliveryTrucks[vehicle];
                    float price = EconomyUtils.GetMarketPrice(truck.m_Resource, resourcePrefabs, ref resourceDatas);
                    loadedGoodsValue += Mathf.RoundToInt(truck.m_Amount * price);
                }
            }
            return loadedGoodsValue;
        }

        private string GetIndustrialPricesString()
        {
            var resourcePrefabs = m_ResourceSystem.GetPrefabs();
            var resourceDatas = SystemAPI.GetComponentLookup<ResourceData>();
            var companyResources = new List<Resource>();
            
            if (EntityManager.HasBuffer<Game.Economy.Resources>(companyEntity))
            {
                var resources = EntityManager.TryGetBuffer<Game.Economy.Resources>(companyEntity, isReadOnly: true, out var buffer);
                if (resources)
                {
                    foreach (var resourceComponent in buffer)
                    {
                        if (!companyResources.Contains(resourceComponent.m_Resource))
                        {
                            companyResources.Add(resourceComponent.m_Resource);
                        }
                    }
                }
            }

            if (EntityManager.HasBuffer<OwnedVehicle>(companyEntity))
            {
                EntityManager.TryGetBuffer<OwnedVehicle>(companyEntity, isReadOnly: true, out var vehicles);
                var deliveryTrucks = SystemAPI.GetComponentLookup<Game.Vehicles.DeliveryTruck>();
                var layouts = SystemAPI.GetBufferLookup<LayoutElement>();

                for (int i = 0; i < vehicles.Length; i++)
                {
                    Entity vehicle = vehicles[i].m_Vehicle;
                    if (!deliveryTrucks.HasComponent(vehicle)) continue;

                    Resource vehicleResource = Resource.NoResource;

                    if (layouts.HasBuffer(vehicle))
                    {
                        var layoutElements = layouts[vehicle];
                        foreach (var element in layoutElements)
                        {
                            if (deliveryTrucks.HasComponent(element.m_Vehicle))
                            {
                                vehicleResource = deliveryTrucks[element.m_Vehicle].m_Resource;
                                break;
                            }
                        }
                    }
                    else if ((deliveryTrucks[vehicle].m_State & DeliveryTruckFlags.Loaded) != 0)
                    {
                        vehicleResource = deliveryTrucks[vehicle].m_Resource;
                    }

                    if (vehicleResource != Resource.NoResource && !companyResources.Contains(vehicleResource))
                    {
                        companyResources.Add(vehicleResource);
                    }
                }
            }

            var parts = new string[companyResources.Count];
            for (int i = 0; i < companyResources.Count; i++)
            {
                var resource = companyResources[i];
                float price = EconomyUtils.GetMarketPrice(resource, resourcePrefabs, ref resourceDatas);
                parts[i] = $"{resource}:{price:F2}";
            }

            return string.Join(";", parts);
        }

        public override void OnWriteProperties(IJsonWriter writer)
        {
            writer.PropertyName("HideCompanyProfitSection");
            writer.Write(Mod.setting.hideProfitSection);
            writer.PropertyName("CurrentWorth");
            writer.Write(_CurrentWorth);
            writer.PropertyName("PreviousWorth");
            writer.Write(_PreviousWorth);
            writer.PropertyName("ProfitChange");
            writer.Write(_ProfitChange);
            writer.PropertyName("Profitability");
            writer.Write(_Profitability);

            writer.PropertyName("ResourceValues");
            writer.ArrayBegin(_ResourceValues.Count);
            foreach (var resourceValue in _ResourceValues)
            {
                writer.TypeBegin("ResourceValueItem");
                writer.PropertyName("resourceName");
                writer.Write(resourceValue.ResourceName);
                writer.PropertyName("amount");
                writer.Write(resourceValue.Amount);
                writer.PropertyName("unitPrice");
                writer.Write(resourceValue.UnitPrice);
                writer.PropertyName("totalValue");
                writer.Write(resourceValue.TotalValue);
                writer.TypeEnd();
            }
            writer.ArrayEnd();

            writer.PropertyName("TotalResourceValue");
            writer.Write(_TotalResourceValue);
            writer.PropertyName("LoadedGoodsValue");
            writer.Write(_LoadedGoodsValue);
            writer.PropertyName("OwnedVehicles");
            writer.Write(_OwnedVehicles);
            writer.PropertyName("LoadedVehicles");
            writer.Write(_LoadedVehicles);
            writer.PropertyName("IndustrialPrices");
            writer.Write(_IndustrialPricesJson);
        }
        private (int owned, int loaded) GetCompanyVehicleStats(DynamicBuffer<OwnedVehicle> vehicles, ref ComponentLookup<Game.Vehicles.DeliveryTruck> deliveryTrucks)
        {
            int owned = vehicles.Length;
            int loaded = 0;

            for (int i = 0; i < vehicles.Length; i++)
            {
                Entity vehicle = vehicles[i].m_Vehicle;
                if (deliveryTrucks.HasComponent(vehicle) && (deliveryTrucks[vehicle].m_State & DeliveryTruckFlags.Loaded) != 0)
                {
                    loaded++;
                }
            }

            return (owned, loaded);
        }
    }
}