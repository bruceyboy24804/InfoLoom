using System;
using System.Collections.Generic;
using ModsCommon.Extensions;
using Colossal.PSI.Common;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Citizens;
using Game.Companies;
using Game.Economy;
using Game.Pathfind;
using Game.Prefabs;
using Game.Rendering;
using Game.UI.InGame;
using Game.Vehicles;
using Game.Zones;
using Unity.Entities;
using DeliveryTruck = Game.Vehicles.DeliveryTruck;
using Park = Game.Buildings.Park;
using Mod = InfoLoomTwo.InfoLoomMod;

namespace InfoLoomTwo.Systems.Sections
{
    public class TradeCostData
    {
        public Resource Resource { get; set; }
        public float BuyCost { get; set; }
        public float SellCost { get; set; }
    }

    public partial class ILBuildingSection : ExtendedInfoSectionBase
    {
        protected override string ModId => InfoLoomMod.Instance.Id;

        // Private fields following naming conventions
        private Entity _BuildingEntity;
        private Entity _CompanyEntity;
        private Entity _TradePartnerEntity;

        private int _EmployeeCount;
        private int _MaxEmployees;
        private int _OvereducatedEmployees;
        private int _CommuterEmployees;
        private Resource _Resource;
        private int _ResourceAmount;
        private float _TransportCost;
        private bool _IsStorageTransfer;
        private Entity _CostPayer;
        private float _MeanInputTripLength;
        private Entity _VehicleTarget;
        private DeliveryTruckFlags _TruckState;
        private Resource _Input1;
        private Resource _Input2;
        private Resource _Output;

        private List<TradeCostData> _TradeCosts;
        private EmploymentData _EducationDataEmployees;
        private EmploymentData _EducationDataWorkplaces;

        // Systems
        private CameraUpdateSystem m_CameraUpdateSystem;
        private PrefabSystem m_PrefabSystem;
        private ResourceSystem m_ResourceSystem;

        // Tracking dictionaries
        private Dictionary<int, int> m_OvereducatedByEducationLevel;
        private Dictionary<int, int> m_CommuterByEducationLevel;
        private Dictionary<int, Dictionary<int, int>> m_OvereducatedByWorkplaceAndEducationLevel;
        private Dictionary<int, Dictionary<int, int>> m_CommuterByWorkplaceAndEducationLevel;

        protected override string group => nameof(ILBuildingSection);

        protected override void OnCreate()
        {
            base.OnCreate();
            m_InfoUISystem.AddMiddleSection(this);

            // Initialize systems
            m_CameraUpdateSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<CameraUpdateSystem>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_ResourceSystem = World.GetOrCreateSystemManaged<ResourceSystem>();

            // Initialize collections
            _TradeCosts = new List<TradeCostData>();
            m_OvereducatedByEducationLevel = new Dictionary<int, int>();
            m_CommuterByEducationLevel = new Dictionary<int, int>();
            m_OvereducatedByWorkplaceAndEducationLevel = new Dictionary<int, Dictionary<int, int>>();
            m_CommuterByWorkplaceAndEducationLevel = new Dictionary<int, Dictionary<int, int>>();

            AddBinding(new TriggerBinding<Entity>(group, "GoTo", NavigateTo));
        }

        protected override void Reset()
        {
            _BuildingEntity = Entity.Null;
            _CompanyEntity = Entity.Null;
            _TradePartnerEntity = Entity.Null;

            _EmployeeCount = 0;
            _MaxEmployees = 0;
            _OvereducatedEmployees = 0;
            _CommuterEmployees = 0;
            _Resource = Resource.NoResource;
            _ResourceAmount = 0;
            _TransportCost = 0f;
            _IsStorageTransfer = false;
            _CostPayer = Entity.Null;
            _MeanInputTripLength = 0f;
            _VehicleTarget = Entity.Null;
            _TruckState = 0;
            _Input1 = Resource.NoResource;
            _Input2 = Resource.NoResource;
            _Output = Resource.NoResource;

            _TradeCosts.Clear();
            _EducationDataEmployees = default;
            _EducationDataWorkplaces = default;

            m_OvereducatedByEducationLevel.Clear();
            m_CommuterByEducationLevel.Clear();
            m_OvereducatedByWorkplaceAndEducationLevel.Clear();
            m_CommuterByWorkplaceAndEducationLevel.Clear();
        }

        private bool Visible()
        {
            _BuildingEntity = selectedEntity;
            return HasEmployees(_BuildingEntity, selectedPrefab) && !Mod.setting.hideBuildingSection;
            ;
        }

        protected override void OnUpdate()
        {
            visible = Visible();
        }

        protected override void OnProcess()
        {
            if (!visible) return;

            ProcessCompanyData();
            ProcessEmploymentData();
        }

        private void ProcessCompanyData()
        {
            var renterLookup = SystemAPI.GetBufferLookup<Renter>(true);
            if (!renterLookup.TryGetBuffer(_BuildingEntity, out var renterBuffer)) return;

            for (var i = 0; i < renterBuffer.Length; i++)
            {
                _CompanyEntity = renterBuffer[i].m_Renter;

                ProcessCompanyInputsOutputs();
                ProcessTradePartner();
                ProcessVehicleData();
                ProcessTradeCosts();
            }
        }

        private void ProcessCompanyInputsOutputs()
        {
            var prefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(true);
            var processDataLookup = SystemAPI.GetComponentLookup<IndustrialProcessData>(true);

            if (!prefabRefLookup.TryGetComponent(_CompanyEntity, out var prefabRef)) return;

            if (processDataLookup.TryGetComponent(prefabRef.m_Prefab, out var processData))
            {
                _Input1 = processData.m_Input1.m_Resource;
                _Input2 = processData.m_Input2.m_Resource;
                _Output = processData.m_Output.m_Resource;
            }
        }

        private void ProcessTradePartner()
        {
            var buyingCompanyLookup = SystemAPI.GetComponentLookup<BuyingCompany>(true);

            if (buyingCompanyLookup.TryGetComponent(_CompanyEntity, out var buyingCompany))
            {
                _TradePartnerEntity = buyingCompany.m_LastTradePartner;
                _MeanInputTripLength = buyingCompany.m_MeanInputTripLength;
            }
        }

        private void ProcessVehicleData()
        {
            var vehicleLookup = SystemAPI.GetBufferLookup<OwnedVehicle>(true);
            var deliveryTruckLookup = SystemAPI.GetComponentLookup<DeliveryTruck>(true);

            if (!vehicleLookup.TryGetBuffer(_CompanyEntity, out var vehicleBuffer)) return;

            for (var i = 0; i < vehicleBuffer.Length; i++)
            {
                var vehicleEntity = vehicleBuffer[i].m_Vehicle;

                if (deliveryTruckLookup.TryGetComponent(vehicleEntity, out var deliveryTruck))
                {
                    _Resource = deliveryTruck.m_Resource;
                    _ResourceAmount = deliveryTruck.m_Amount;
                    _TruckState = deliveryTruck.m_State;
                    _IsStorageTransfer = (deliveryTruck.m_State & DeliveryTruckFlags.StorageTransfer) != 0;
                    _CostPayer = _CompanyEntity;

                    ProcessTransportCost(vehicleEntity);
                    break;
                }
            }
        }

        private void ProcessTransportCost(Entity vehicleEntity)
        {
            var pathInfoLookup = SystemAPI.GetComponentLookup<PathInformation>(true);
            var resourceDatas = SystemAPI.GetComponentLookup<ResourceData>(true);

            if (!pathInfoLookup.TryGetComponent(vehicleEntity, out var pathInfo)) return;

            _VehicleTarget = pathInfo.m_Destination;

            var resourcePrefabs = m_ResourceSystem.GetPrefabs();
            if (resourcePrefabs[_Resource] == Entity.Null) return;

            var weight = EconomyUtils.GetWeight(_Resource, resourcePrefabs, ref resourceDatas);
            _TransportCost = EconomyUtils.GetTransportCost(pathInfo.m_Distance, _Resource, _ResourceAmount, weight);
        }

        private void ProcessTradeCosts()
        {
            var tradeCostLookup = SystemAPI.GetBufferLookup<TradeCost>(true);

            if (!tradeCostLookup.TryGetBuffer(_CompanyEntity, out var tradeCostBuffer)) return;

            for (var i = 0; i < tradeCostBuffer.Length; i++)
            {
                var tradeCost = tradeCostBuffer[i];
                _TradeCosts.Add(new TradeCostData
                {
                    Resource = tradeCost.m_Resource,
                    BuyCost = tradeCost.m_BuyCost,
                    SellCost = tradeCost.m_SellCost
                });
            }
        }

        private void ProcessEmploymentData()
        {
            var renterLookup = SystemAPI.GetBufferLookup<Renter>(true);

            if (!renterLookup.TryGetBuffer(_BuildingEntity, out var renterBuffer)) return;

            for (var renterIndex = 0; renterIndex < renterBuffer.Length; renterIndex++)
            {
                var companyEntity = renterBuffer[renterIndex].m_Renter;
                ProcessCompanyEmployees(companyEntity);
            }

            AddEmployees();
            visible = _MaxEmployees > 0;
        }

        private void ProcessCompanyEmployees(Entity companyEntity)
        {
            var employeeLookup = SystemAPI.GetBufferLookup<Employee>(true);
            var workerLookup = SystemAPI.GetComponentLookup<Worker>(true);
            var citizenLookup = SystemAPI.GetComponentLookup<Citizen>(true);

            if (!employeeLookup.TryGetBuffer(companyEntity, out var employeeBuffer)) return;

            for (var i = 0; i < employeeBuffer.Length; i++)
            {
                var workerEntity = employeeBuffer[i].m_Worker;

                if (workerLookup.TryGetComponent(workerEntity, out var worker) &&
                    citizenLookup.TryGetComponent(workerEntity, out var citizen))
                    ProcessWorkerData(worker, citizen);
            }
        }

        private void ProcessWorkerData(Worker worker, Citizen citizen)
        {
            var educationLevel = citizen.GetEducationLevel();
            int workerLevel = worker.m_Level;

            // Process overeducated workers
            if (workerLevel < educationLevel)
            {
                _OvereducatedEmployees++;
                UpdateEducationLevelCount(m_OvereducatedByEducationLevel, educationLevel);
                UpdateWorkplaceEducationCount(m_OvereducatedByWorkplaceAndEducationLevel, workerLevel, educationLevel);
            }

            // Process commuter workers
            if ((citizen.m_State & CitizenFlags.Commuter) != CitizenFlags.None)
            {
                _CommuterEmployees++;
                UpdateEducationLevelCount(m_CommuterByEducationLevel, educationLevel);
                UpdateWorkplaceEducationCount(m_CommuterByWorkplaceAndEducationLevel, workerLevel, educationLevel);
            }
        }

        private void UpdateEducationLevelCount(Dictionary<int, int> dictionary, int educationLevel)
        {
            if (!dictionary.ContainsKey(educationLevel))
                dictionary[educationLevel] = 0;
            dictionary[educationLevel]++;
        }

        private void UpdateWorkplaceEducationCount(Dictionary<int, Dictionary<int, int>> dictionary, int workerLevel,
            int educationLevel)
        {
            if (!dictionary.ContainsKey(workerLevel))
                dictionary[workerLevel] = new Dictionary<int, int>();
            if (!dictionary[workerLevel].ContainsKey(educationLevel))
                dictionary[workerLevel][educationLevel] = 0;
            dictionary[workerLevel][educationLevel]++;
        }

        private bool HasEmployees(Entity entity, Entity prefab)
        {
            var renterLookup = SystemAPI.GetBufferLookup<Renter>(true);
            var employeeLookup = SystemAPI.GetBufferLookup<Employee>(true);
            var workProviderLookup = SystemAPI.GetComponentLookup<WorkProvider>(true);
            var spawnableBuildingLookup = SystemAPI.GetComponentLookup<SpawnableBuildingData>(true);
            var companyDataLookup = SystemAPI.GetComponentLookup<CompanyData>(true);

            if (!renterLookup.TryGetBuffer(entity, out var buffer))
                return employeeLookup.HasBuffer(entity) && workProviderLookup.HasComponent(entity);

            if (buffer.Length == 0 && spawnableBuildingLookup.TryGetComponent(prefab, out var component))
            {
                if (m_PrefabSystem.TryGetPrefab<ZonePrefab>(component.m_ZonePrefab, out var prefab2))
                    return prefab2.m_AreaType == AreaType.Commercial ||
                           prefab2.m_AreaType == AreaType.Industrial;
                return false;
            }

            for (var i = 0; i < buffer.Length; i++)
            {
                var renter = buffer[i].m_Renter;
                if (companyDataLookup.HasComponent(renter))
                    return employeeLookup.HasComponent(renter) && workProviderLookup.HasComponent(renter);
            }

            return false;
        }

        private void AddEmployees()
        {
            var serviceUsageLookup = SystemAPI.GetComponentLookup<ServiceUsage>(true);

            if (serviceUsageLookup.HasComponent(_BuildingEntity))
                tooltipKeys.Add("ServiceUsage");
            else
                AddEmployees(_BuildingEntity);
        }

        private void AddEmployees(Entity entity)
        {
            var prefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(true);
            var employeeLookup = SystemAPI.GetBufferLookup<Employee>(true);
            var workProviderLookup = SystemAPI.GetComponentLookup<WorkProvider>(true);
            var workplaceDataLookup = SystemAPI.GetComponentLookup<WorkplaceData>(true);

            var prefab = prefabRefLookup[entity].m_Prefab;
            var companyEntity = GetCompanyEntity(entity);
            var companyPrefab = prefabRefLookup[companyEntity].m_Prefab;

            var buildingLevel = GetBuildingLevel(entity, prefab);

            if (employeeLookup.TryGetBuffer(companyEntity, out var buffer) &&
                workProviderLookup.TryGetComponent(companyEntity, out var workProvider))
            {
                _EmployeeCount += buffer.Length;
                var complexity = workplaceDataLookup[companyPrefab].m_Complexity;
                var workplacesData =
                    EmploymentData.GetWorkplacesData(workProvider.m_MaxWorkers, buildingLevel, complexity);
                _MaxEmployees += workplacesData.total;
                _EducationDataWorkplaces += workplacesData;
                _EducationDataEmployees +=
                    EmploymentData.GetEmployeesData(buffer, workplacesData.total - buffer.Length);
            }
        }

        private int GetBuildingLevel(Entity entity, Entity prefab)
        {
            var spawnableBuildingLookup = SystemAPI.GetComponentLookup<SpawnableBuildingData>(true);
            var propertyRenterLookup = SystemAPI.GetComponentLookup<PropertyRenter>(true);
            var prefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(true);

            if (spawnableBuildingLookup.TryGetComponent(prefab, out var component)) return component.m_Level;

            if (propertyRenterLookup.TryGetComponent(entity, out var propertyRenter) &&
                prefabRefLookup.TryGetComponent(propertyRenter.m_Property, out var propertyPrefabRef) &&
                spawnableBuildingLookup.TryGetComponent(propertyPrefabRef.m_Prefab, out var propertyComponent))
                return propertyComponent.m_Level;

            return 1;
        }

        private Entity GetCompanyEntity(Entity entity)
        {
            var parkLookup = SystemAPI.GetComponentLookup<Park>(true);
            var renterLookup = SystemAPI.GetBufferLookup<Renter>(true);
            var companyDataLookup = SystemAPI.GetComponentLookup<CompanyData>(true);

            if (!parkLookup.HasComponent(entity) && renterLookup.TryGetBuffer(entity, out var buffer))
                for (var i = 0; i < buffer.Length; i++)
                {
                    var renter = buffer[i].m_Renter;
                    if (companyDataLookup.HasComponent(renter)) return renter;
                }

            return entity;
        }

        // Rest of the methods remain the same...
        public override void OnWriteProperties(IJsonWriter writer)
        {
            writer.PropertyName("HideBuildingSection");
            writer.Write(Mod.setting.hideBuildingSection);
            writer.PropertyName("ResourceAmount");
            writer.Write(UnitConversionUtils.KilogramsToTons(_ResourceAmount));
            writer.PropertyName("TransportCost");
            writer.Write(_TransportCost);
            writer.PropertyName("EmployeeCount");
            writer.Write(_EmployeeCount);
            writer.PropertyName("MaxEmployees");
            writer.Write(_MaxEmployees);
            writer.PropertyName("EducationDataEmployees");
            writer.Write(_EducationDataEmployees);
            writer.PropertyName("EducationDataWorkplaces");
            writer.Write(_EducationDataWorkplaces);
            writer.PropertyName("OvereductedEmployees");
            writer.Write(_OvereducatedEmployees);

            WriteEducationLevelData(writer, "OvereductedByEducationLevel", "OvereducatedLevelData",
                m_OvereducatedByEducationLevel);
            WriteWorkplaceEducationData(writer, "OvereductedByWorkplaceAndEducationLevel",
                "OvereducatedWorkplaceLevelData", "OvereducatedEducationLevelData",
                m_OvereducatedByWorkplaceAndEducationLevel);

            writer.PropertyName("CommuterEmployees");
            writer.Write(_CommuterEmployees);

            WriteEducationLevelData(writer, "CommuterByEducationLevel", "CommuterLevelData",
                m_CommuterByEducationLevel);
            WriteWorkplaceEducationData(writer, "CommuterByWorkplaceAndEducationLevel", "CommuterWorkplaceLevelData",
                "CommuterEducationLevelData", m_CommuterByWorkplaceAndEducationLevel);

            WriteTradeCosts(writer);
            WriteVehicleData(writer);
            WriteCompanyInputOutput(writer);
        }

        private void WriteEducationLevelData(IJsonWriter writer, string propertyName, string typeName,
            Dictionary<int, int> data)
        {
            writer.PropertyName(propertyName);
            writer.ArrayBegin((uint)data.Count);
            foreach (var kvp in data)
            {
                writer.TypeBegin(typeName);
                writer.PropertyName("EducationLevel");
                writer.Write(kvp.Key);
                writer.PropertyName("Count");
                writer.Write(kvp.Value);
                writer.TypeEnd();
            }

            writer.ArrayEnd();
        }

        private void WriteWorkplaceEducationData(IJsonWriter writer, string propertyName, string workplaceTypeName,
            string educationTypeName, Dictionary<int, Dictionary<int, int>> data)
        {
            writer.PropertyName(propertyName);
            writer.ArrayBegin((uint)data.Count);
            foreach (var workplaceKvp in data)
            {
                writer.TypeBegin(workplaceTypeName);
                writer.PropertyName("WorkplaceLevel");
                writer.Write(workplaceKvp.Key);
                writer.PropertyName("EducationLevels");
                writer.ArrayBegin((uint)workplaceKvp.Value.Count);
                foreach (var eduKvp in workplaceKvp.Value)
                {
                    writer.TypeBegin(educationTypeName);
                    writer.PropertyName("EducationLevel");
                    writer.Write(eduKvp.Key);
                    writer.PropertyName("Count");
                    writer.Write(eduKvp.Value);
                    writer.TypeEnd();
                }

                writer.ArrayEnd();
                writer.TypeEnd();
            }

            writer.ArrayEnd();
        }

        private void WriteTradeCosts(IJsonWriter writer)
        {
            writer.PropertyName("TradeCosts");
            writer.ArrayBegin((uint)_TradeCosts.Count);
            foreach (var tradeCost in _TradeCosts)
            {
                writer.TypeBegin("TradeCostData");
                writer.PropertyName("Resource");
                writer.Write(Enum.GetName(typeof(Resource), tradeCost.Resource));
                writer.PropertyName("BuyCost");
                writer.Write(tradeCost.BuyCost);
                writer.PropertyName("SellCost");
                writer.Write(tradeCost.SellCost);
                writer.TypeEnd();
            }

            writer.ArrayEnd();
        }

        private void WriteVehicleData(IJsonWriter writer)
        {
            writer.PropertyName("IsStorageTransfer");
            writer.Write(_IsStorageTransfer);
            writer.PropertyName("CostPayer");
            writer.Write(_CostPayer);
            writer.PropertyName("MeanInputTripLength");
            writer.Write(_MeanInputTripLength);
            writer.PropertyName("VehicleTarget");
            writer.Write(_VehicleTarget);
            writer.PropertyName("TruckState");
            writer.Write((int)_TruckState);
        }

        private void WriteCompanyInputOutput(IJsonWriter writer)
        {
            writer.PropertyName("Input1");
            writer.Write(Enum.GetName(typeof(Resource), _Input1));
            writer.PropertyName("Input2");
            writer.Write(Enum.GetName(typeof(Resource), _Input2));
            writer.PropertyName("Output");
            writer.Write(Enum.GetName(typeof(Resource), _Output));
        }

        public void NavigateTo(Entity entity)
        {
            if (m_CameraUpdateSystem.orbitCameraController != null && entity != Entity.Null)
            {
                m_CameraUpdateSystem.orbitCameraController.followedEntity = entity;
                m_CameraUpdateSystem.orbitCameraController.TryMatchPosition(m_CameraUpdateSystem
                    .activeCameraController);
                m_CameraUpdateSystem.activeCameraController = m_CameraUpdateSystem.orbitCameraController;
            }
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
    }
}
