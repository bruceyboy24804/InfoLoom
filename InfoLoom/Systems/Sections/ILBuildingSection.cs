using System;
using System.Collections.Generic;
using Colossal;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Game.Rendering;
using Game.UI;
using Game.UI.InGame;
using Game.Vehicles;
using Game.Zones;
using InfoLoomTwo.Extensions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using DeliveryTruck = Game.Prefabs.DeliveryTruck;

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
        protected override string group => nameof(ILBuildingSection);
        private bool isCompany { get; set; }
        private CameraUpdateSystem m_CameraUpdateSystem;
        private ValueBinding<Entity> m_CameraBinding;
        public Entity tradePartnerName;
        public Resource resource;
        public int resourceAmount;
        public int employeeCount;
        public int maxEmployees;
        public int overeductedEmployees;
        public int commuterEmployees;
        public float transportCost;
        private NameSystem m_NameSystem;
        private PrefabSystem m_PrefabSystem;
        private ResourceSystem m_ResourceSystem;
        public List<TradeCostData> TradeCosts { get; set; } = new List<TradeCostData>();
        public EmploymentData educationDataEmployees { get; set; }
        public EmploymentData educationDataWorkplaces { get; set; }
        protected override void OnCreate()
        {
            base.OnCreate();
            m_InfoUISystem.AddMiddleSection(this);
            m_NameSystem = World.GetOrCreateSystemManaged<NameSystem>();
            m_CameraUpdateSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<CameraUpdateSystem>();
            AddBinding(new TriggerBinding<Entity>(group, "GoTo", NavigateTo));
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_ResourceSystem = World.GetOrCreateSystemManaged<ResourceSystem>();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        protected override void Reset()
        {
            employeeCount = 0;
            maxEmployees = 0;
            overeductedEmployees = 0;
            commuterEmployees = 0;
            resourceAmount = 0;
            transportCost = 0f;
            tradePartnerName = Entity.Null;
            educationDataEmployees = default(EmploymentData);
            educationDataWorkplaces = default(EmploymentData);
            TradeCosts.Clear();
        }

        protected override void OnUpdate()
        {
	        visible = Visible();
	        if (visible)
            {
                TradeCosts.Clear(); // Move this outside the loop
                
                if (EntityManager.HasComponent<Renter>(selectedEntity))
                {
                    if (EntityManager.TryGetBuffer<Renter>(selectedEntity, isReadOnly: true, out var renterBuffer))
                        if (renterBuffer.Length > 0)
                        {
                            for (int i = 0; i < renterBuffer.Length; i++)
                            {
                                var companyEntity = renterBuffer[i].m_Renter;
                                
                                // Get trade partner
                                if (EntityManager.HasComponent<BuyingCompany>(companyEntity))
                                {
                                    var buyingCompany = EntityManager.GetComponentData<BuyingCompany>(companyEntity);
                                    tradePartnerName = buyingCompany.m_LastTradePartner;
                                }
                                
                                // Process vehicles
                                if (EntityManager.HasComponent<OwnedVehicle>(companyEntity))
                                {
                                    if (EntityManager.TryGetBuffer<OwnedVehicle>(companyEntity, isReadOnly: true, out var vehicleBuffer))
                                    {
                                        foreach (var ownedVehicle in vehicleBuffer)
                                        {
                                            var vehicleEntity = ownedVehicle.m_Vehicle;
                                            if (EntityManager.HasComponent<Game.Vehicles.DeliveryTruck>(vehicleEntity))
                                            {
                                                var deliveryTruck = EntityManager.GetComponentData<Game.Vehicles.DeliveryTruck>(vehicleEntity);
                                                
                                                // Set resource from delivery truck
                                                resource = deliveryTruck.m_Resource;
                                                resourceAmount = deliveryTruck.m_Amount;
                                                
                                                float distance = 0f;
                                                if (EntityManager.HasComponent<Game.Pathfind.PathInformation>(vehicleEntity))
                                                {
                                                    var pathInfo = EntityManager.GetComponentData<Game.Pathfind.PathInformation>(vehicleEntity);
                                                    distance = pathInfo.m_Distance;
                                                }
                                                
                                                ResourcePrefabs resourcePrefabs = m_ResourceSystem.GetPrefabs();
                                                float weight = EconomyUtils.GetWeight(EntityManager, resource, resourcePrefabs);
                                                transportCost = EconomyUtils.GetTransportCost(distance, resourceAmount, weight, StorageTransferFlags.Car);
                                                break;
                                            }
                                        }
                                    }
                                }
                            
                                // Add trade costs (don't clear here)
                                if (EntityManager.HasComponent<TradeCost>(companyEntity))
                                {
                                    if (EntityManager.TryGetBuffer<TradeCost>(companyEntity, isReadOnly: true, out var tradeCostBuffer))
                                    {
                                        for (int j = 0; j < tradeCostBuffer.Length; j++)
                                        {
                                            var tradeCost = tradeCostBuffer[j];
                                            TradeCosts.Add(new TradeCostData
                                            {
                                                Resource = tradeCost.m_Resource,
                                                BuyCost = tradeCost.m_BuyCost,
                                                SellCost = tradeCost.m_SellCost
                                            });
                                        }
                                    }
                                }
                            }
                        }
                }
                AddEmployees();
                visible = maxEmployees > 0;
            }
	        
        }
        private Dictionary<int, int> overeducatedByEducationLevel = new();
        private Dictionary<int, int> commuterByEducationLevel = new();
        private Dictionary<int, Dictionary<int, int>> overeducatedByWorkplaceAndEducationLevel = new();
        private Dictionary<int, Dictionary<int, int>> commuterByWorkplaceAndEducationLevel = new();

        protected override void OnProcess()
        {
            overeducatedByEducationLevel.Clear();
            commuterByEducationLevel.Clear();
            overeducatedByWorkplaceAndEducationLevel.Clear();
            commuterByWorkplaceAndEducationLevel.Clear();

            if (EntityManager.TryGetBuffer<Renter>(selectedEntity, isReadOnly: true, out var renterBuffer))
            {
                for (int renterIndex = 0; renterIndex < renterBuffer.Length; renterIndex++)
                {
                    var companyEntity = renterBuffer[renterIndex].m_Renter;

                    if (EntityManager.TryGetBuffer<Employee>(companyEntity, isReadOnly: true, out var employeeBuffer))
                    {
                        for (int i = 0; i < employeeBuffer.Length; i++)
                        {
                            var employee = employeeBuffer[i];
                            var workerEntity = employee.m_Worker;

                            if (EntityManager.TryGetComponent<Worker>(workerEntity, out var worker) &&
                                EntityManager.TryGetComponent<Citizen>(workerEntity, out var citizen))
                            {
                                int educationLevel = citizen.GetEducationLevel();
                                int workerLevel = worker.m_Level;

                                // Overeducated
                                if (workerLevel < educationLevel)
                                {
                                    overeductedEmployees++;
                                    if (!overeducatedByEducationLevel.ContainsKey(educationLevel))
                                        overeducatedByEducationLevel[educationLevel] = 0;
                                    overeducatedByEducationLevel[educationLevel]++;

                                    if (!overeducatedByWorkplaceAndEducationLevel.ContainsKey(workerLevel))
                                        overeducatedByWorkplaceAndEducationLevel[workerLevel] = new Dictionary<int, int>();
                                    if (!overeducatedByWorkplaceAndEducationLevel[workerLevel].ContainsKey(educationLevel))
                                        overeducatedByWorkplaceAndEducationLevel[workerLevel][educationLevel] = 0;
                                    overeducatedByWorkplaceAndEducationLevel[workerLevel][educationLevel]++;
                                }

                                // Commuter
                                if ((citizen.m_State & CitizenFlags.Commuter) != CitizenFlags.None)
                                {
                                    commuterEmployees++;
                                    if (!commuterByEducationLevel.ContainsKey(educationLevel))
                                        commuterByEducationLevel[educationLevel] = 0;
                                    commuterByEducationLevel[educationLevel]++;

                                    if (!commuterByWorkplaceAndEducationLevel.ContainsKey(workerLevel))
                                        commuterByWorkplaceAndEducationLevel[workerLevel] = new Dictionary<int, int>();
                                    if (!commuterByWorkplaceAndEducationLevel[workerLevel].ContainsKey(educationLevel))
                                        commuterByWorkplaceAndEducationLevel[workerLevel][educationLevel] = 0;
                                    commuterByWorkplaceAndEducationLevel[workerLevel][educationLevel]++;
                                }
                            }
                        }
                    }
                }
            }
        }
        
        private bool Visible()
        {
            return HasEmployees(selectedEntity, selectedPrefab);
        }
        private bool HasEmployees(Entity entity, Entity prefab)
        {
            if (!base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<Renter> buffer))
            {
                if (base.EntityManager.HasComponent<Employee>(entity) && base.EntityManager.HasComponent<WorkProvider>(entity))
                {
                    return base.Enabled;
                }
                return false;
            }
            if (buffer.Length == 0 && base.EntityManager.TryGetComponent<SpawnableBuildingData>(prefab, out var component))
            {
                m_PrefabSystem.TryGetPrefab<ZonePrefab>(component.m_ZonePrefab, out var prefab2);
                if (prefab2 != null)
                {
                    if (prefab2.m_AreaType != Game.Zones.AreaType.Commercial)
                    {
                        return prefab2.m_AreaType == Game.Zones.AreaType.Industrial;
                    }
                    return true;
                }
                return false;
            }
            for (int i = 0; i < buffer.Length; i++)
            {
                Entity renter = buffer[i].m_Renter;
                if (base.EntityManager.HasComponent<CompanyData>(renter))
                {
                    if (base.EntityManager.HasComponent<Employee>(renter))
                    {
                        return base.EntityManager.HasComponent<WorkProvider>(renter);
                    }
                    return false;
                }
            }
            return false;
        }
        private void AddEmployees()
        {
	        if (base.EntityManager.HasComponent<ServiceUsage>(selectedEntity))
	        {
		        base.tooltipKeys.Add("ServiceUsage");
	        }
	        else
	        {
		        AddEmployees(selectedEntity);
	        }
        }
        private void AddEmployees(Entity entity)
        {
            Entity prefab = base.EntityManager.GetComponentData<PrefabRef>(entity).m_Prefab;
            Entity entity2 = GetEntity(entity);
            Entity prefab2 = base.EntityManager.GetComponentData<PrefabRef>(entity2).m_Prefab;
            int buildingLevel = 1;
            PropertyRenter component2;
            PrefabRef component3;
            SpawnableBuildingData component4;
            Worker component6;

            if (base.EntityManager.TryGetComponent<SpawnableBuildingData>(prefab, out var component))
            {
                buildingLevel = component.m_Level;
            }
            else if (base.EntityManager.TryGetComponent<PropertyRenter>(entity, out component2) && base.EntityManager.TryGetComponent<PrefabRef>(component2.m_Property, out component3) && base.EntityManager.TryGetComponent<SpawnableBuildingData>(component3.m_Prefab, out component4))
            {
                buildingLevel = component4.m_Level;
            }

            if (base.EntityManager.TryGetBuffer(entity2, isReadOnly: true, out DynamicBuffer<Employee> buffer) && base.EntityManager.TryGetComponent<WorkProvider>(entity2, out var component5))
            {
                employeeCount += buffer.Length;
                WorkplaceComplexity complexity = base.EntityManager.GetComponentData<WorkplaceData>(prefab2).m_Complexity;
                EmploymentData workplacesData = EmploymentData.GetWorkplacesData(component5.m_MaxWorkers, buildingLevel, complexity);
                maxEmployees += workplacesData.total;
                educationDataWorkplaces += workplacesData;
                var employeesData = EmploymentData.GetEmployeesData(buffer, workplacesData.total - buffer.Length);
                educationDataEmployees += employeesData;
            }
        }
        private Entity GetEntity(Entity entity)
        {
	        if (!base.EntityManager.HasComponent<Game.Buildings.Park>(entity) && base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<Renter> buffer))
	        {
		        for (int i = 0; i < buffer.Length; i++)
		        {
			        Entity renter = buffer[i].m_Renter;
			        if (base.EntityManager.HasComponent<CompanyData>(renter))
			        {
				        return renter;
			        }
		        }
	        }
	        return entity;
        }
        public override void OnWriteProperties(IJsonWriter writer)
        {
            writer.PropertyName("TradePartnerName");
            m_NameSystem.BindName(writer, tradePartnerName);
            writer.PropertyName("TradePartnerEntity");
            writer.Write(tradePartnerName);
            writer.PropertyName("ResourceAmount");
            writer.Write(UnitConversionUtils.KilogramsToTons(resourceAmount));
            writer.PropertyName("TransportCost");
            writer.Write(transportCost);
            writer.PropertyName("EmployeeCount");
            writer.Write(employeeCount);
            writer.PropertyName("MaxEmployees");
            writer.Write(maxEmployees);
            writer.PropertyName("EducationDataEmployees");
            writer.Write(educationDataEmployees);
            writer.PropertyName("EducationDataWorkplaces");
            writer.Write(educationDataWorkplaces);

            writer.PropertyName("OvereductedEmployees");
            writer.Write(overeductedEmployees);
            writer.PropertyName("OvereductedByEducationLevel");
            writer.ArrayBegin((uint)overeducatedByEducationLevel.Count);
            foreach (var kvp in overeducatedByEducationLevel)
            {
                writer.TypeBegin("OvereducatedLevelData");
                writer.PropertyName("EducationLevel");
                writer.Write(kvp.Key);
                writer.PropertyName("Count");
                writer.Write(kvp.Value);
                writer.TypeEnd();
            }
            writer.ArrayEnd();
            
            writer.PropertyName("OvereductedByWorkplaceAndEducationLevel");
            
            writer.ArrayBegin((uint)overeducatedByWorkplaceAndEducationLevel.Count);
            foreach (var workplaceKvp in overeducatedByWorkplaceAndEducationLevel)
            {
                writer.TypeBegin("OvereducatedWorkplaceLevelData");
                writer.PropertyName("WorkplaceLevel");
                writer.Write(workplaceKvp.Key);
                writer.PropertyName("EducationLevels");
                writer.ArrayBegin((uint)workplaceKvp.Value.Count);
                foreach (var eduKvp in workplaceKvp.Value)
                {
                    writer.TypeBegin("OvereducatedEducationLevelData");
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
            

            writer.PropertyName("CommuterEmployees");
            
            writer.Write(commuterEmployees);
            writer.PropertyName("CommuterByEducationLevel");
            
            writer.ArrayBegin((uint)commuterByEducationLevel.Count);
            foreach (var kvp in commuterByEducationLevel)
            {
                writer.TypeBegin("CommuterLevelData");
                writer.PropertyName("EducationLevel");
                writer.Write(kvp.Key);
                writer.PropertyName("Count");
                writer.Write(kvp.Value);
                writer.TypeEnd();
            }
            writer.ArrayEnd();

            writer.PropertyName("CommuterByWorkplaceAndEducationLevel");
            
            writer.ArrayBegin((uint)commuterByWorkplaceAndEducationLevel.Count);
            foreach (var workplaceKvp in commuterByWorkplaceAndEducationLevel)
            {
                writer.TypeBegin("CommuterWorkplaceLevelData");
                writer.PropertyName("WorkplaceLevel");
                writer.Write(workplaceKvp.Key);
                writer.PropertyName("EducationLevels");
                writer.ArrayBegin((uint)workplaceKvp.Value.Count);
                foreach (var eduKvp in workplaceKvp.Value)
                {
                    writer.TypeBegin("CommuterEducationLevelData");
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
            

            writer.PropertyName("TradeCosts");
            writer.ArrayBegin((uint)TradeCosts.Count);
            foreach (var tradeCost in TradeCosts)
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
        public void NavigateTo(Entity entity)
        {
            if (m_CameraUpdateSystem.orbitCameraController != null && entity != Entity.Null)
            {
                m_CameraUpdateSystem.orbitCameraController.followedEntity = entity;
                m_CameraUpdateSystem.orbitCameraController.TryMatchPosition(m_CameraUpdateSystem.activeCameraController);
               m_CameraUpdateSystem.activeCameraController = m_CameraUpdateSystem.orbitCameraController;
            }
        }
    }
}