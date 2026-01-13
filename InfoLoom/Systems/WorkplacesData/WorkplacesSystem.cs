using System.Runtime.CompilerServices;
using Colossal.UI.Binding;
using Game;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.UI.InGame;
using Unity.Collections;
using Unity.Entities;
using InfoLoomTwo.Domain;

namespace InfoLoomTwo.Systems.WorkplacesData
{
    public partial class WorkplacesSystem : GameSystemBase
    {
        private enum EducationIndex
        {
            Uneducated = 0,
            PoorlyEducated = 1,
            Educated = 2,
            WellEducated = 3,
            HighlyEducated = 4,
            Totals = 5,
            ProviderCounts = 6
        }

        private const string kGroup = "workplaces";
        private const int ResultsCount = 7;

        private SimulationSystem m_SimulationSystem;
        private EntityQuery m_WorkplaceQuery;

        public Entity SelectedDistrict { get; set; } = Entity.Null;
        public NativeArray<WorkplacesInfo> m_Results;
        public bool IsPanelVisible { get; set; }
        public bool ForceUpdate { get; private set; }

        protected override void OnCreate()
        {
            base.OnCreate();

            m_Results = new NativeArray<WorkplacesInfo>(ResultsCount, Allocator.Persistent);

            m_WorkplaceQuery = SystemAPI.QueryBuilder()
                .WithAll<Employee, WorkProvider, PrefabRef>()
                .WithAny<PropertyRenter, Building>()
                .WithNone<Game.Objects.OutsideConnection, Temp>()
                .Build();

            m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (m_Results.IsCreated)
            {
                m_Results.Dispose();
            }
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 512;
        }

        protected override void OnUpdate()
        {
            if (!IsPanelVisible)
                return;

            ForceUpdate = false;
            ResetResults();

            // Calculate workplace and employee data
            CalculateWorkplaceData();

            // Count working commuters by education level
            CountWorkingCommuters();

            // Calculate totals row
            CalculateTotals();
        }

        private void CalculateWorkplaceData()
        {
            ComponentLookup<PrefabRef> prefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(isReadOnly: true);
            ComponentLookup<WorkplaceData> workplaceDataLookup = SystemAPI.GetComponentLookup<WorkplaceData>(isReadOnly: true);
            ComponentLookup<SpawnableBuildingData> spawnableBuildingLookup = SystemAPI.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
            ComponentLookup<IndustrialProcessData> industrialProcessLookup = SystemAPI.GetComponentLookup<IndustrialProcessData>(isReadOnly: true);
            ComponentLookup<CurrentDistrict> districtLookup = SystemAPI.GetComponentLookup<CurrentDistrict>(isReadOnly: true);
            ComponentLookup<PropertyRenter> propertyRenterLookup = SystemAPI.GetComponentLookup<PropertyRenter>(isReadOnly: true);
            ComponentLookup<Game.Companies.ExtractorCompany> extractorLookup = SystemAPI.GetComponentLookup<Game.Companies.ExtractorCompany>(isReadOnly: true);
            ComponentLookup<IndustrialCompany> industrialLookup = SystemAPI.GetComponentLookup<IndustrialCompany>(isReadOnly: true);
            ComponentLookup<CommercialCompany> commercialLookup = SystemAPI.GetComponentLookup<CommercialCompany>(isReadOnly: true);
            BufferLookup<Employee> employeeLookup = SystemAPI.GetBufferLookup<Employee>(isReadOnly: true);

            WorkplacesInfo providerCounts = m_Results[(int)EducationIndex.ProviderCounts];

            NativeArray<Entity> entities = m_WorkplaceQuery.ToEntityArray(Allocator.Temp);
            NativeArray<WorkProvider> workProviders = m_WorkplaceQuery.ToComponentDataArray<WorkProvider>(Allocator.Temp);
            NativeArray<PrefabRef> prefabRefs = m_WorkplaceQuery.ToComponentDataArray<PrefabRef>(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                WorkProvider workProvider = workProviders[i];
                PrefabRef prefabRef = prefabRefs[i];

                // Get employee buffer
                if (!employeeLookup.HasBuffer(entity))
                    continue;

                DynamicBuffer<Employee> employees = employeeLookup[entity];

                // Determine company/building entity for district filtering
                Entity workplaceEntity = entity;
                if (propertyRenterLookup.HasComponent(entity))
                {
                    PropertyRenter renter = propertyRenterLookup[entity];
                    if (renter.m_Property != Entity.Null)
                        workplaceEntity = renter.m_Property;
                }

                // Apply district filter
                if (SelectedDistrict != Entity.Null)
                {
                    bool isInDistrict = false;

                    if (districtLookup.HasComponent(workplaceEntity))
                    {
                        isInDistrict = districtLookup[workplaceEntity].m_District == SelectedDistrict;
                    }
                    else if (districtLookup.HasComponent(entity))
                    {
                        isInDistrict = districtLookup[entity].m_District == SelectedDistrict;
                    }

                    if (!isInDistrict)
                        continue;
                }

                // Get workplace data
                WorkplaceData workplaceData = workplaceDataLookup[prefabRef.m_Prefab];

                // Get building level
                int buildingLevel = 1;
                if (propertyRenterLookup.HasComponent(entity))
                {
                    PropertyRenter renter = propertyRenterLookup[entity];
                    Entity propertyEntity = renter.m_Property;

                    if (propertyEntity != Entity.Null &&
                        prefabRefLookup.TryGetComponent(propertyEntity, out PrefabRef propertyPrefabRef) &&
                        spawnableBuildingLookup.TryGetComponent(propertyPrefabRef.m_Prefab, out SpawnableBuildingData spawnableData))
                    {
                        buildingLevel = (int)spawnableData.m_Level;
                    }
                }

                // Determine company type
                bool isExtractor = extractorLookup.HasComponent(entity);
                bool isIndustrial = industrialLookup.HasComponent(entity);
                bool isCommercial = commercialLookup.HasComponent(entity);
                bool isService = !(isIndustrial || isCommercial);

                bool isOffice = false;
                bool isLeisure = false;
                if (industrialProcessLookup.HasComponent(prefabRef.m_Prefab))
                {
                    IndustrialProcessData process = industrialProcessLookup[prefabRef.m_Prefab];
                    Resource outputRes = process.m_Output.m_Resource;
                    isLeisure = (outputRes & (Resource.Meals | Resource.Entertainment | Resource.Recreation | Resource.Lodging)) != Resource.NoResource;
                    isOffice = (outputRes & (Resource.Software | Resource.Telecom | Resource.Financial | Resource.Media)) != Resource.NoResource;
                }

                // Calculate workplace and employee data
                EmploymentData workplacesData = EmploymentData.GetWorkplacesData(
                    workProvider.m_MaxWorkers,
                    buildingLevel,
                    workplaceData.m_Complexity);

                EmploymentData employeesData = EmploymentData.GetEmployeesData(
                    employees,
                    workplacesData.total - employees.Length);

                // Update education level results
                UpdateEducationLevel(EducationIndex.Uneducated, workplacesData.uneducated, employeesData.uneducated,
                    isService, isCommercial, isIndustrial, isExtractor, isOffice, isLeisure);

                UpdateEducationLevel(EducationIndex.PoorlyEducated, workplacesData.poorlyEducated, employeesData.poorlyEducated,
                    isService, isCommercial, isIndustrial, isExtractor, isOffice, isLeisure);

                UpdateEducationLevel(EducationIndex.Educated, workplacesData.educated, employeesData.educated,
                    isService, isCommercial, isIndustrial, isExtractor, isOffice, isLeisure);

                UpdateEducationLevel(EducationIndex.WellEducated, workplacesData.wellEducated, employeesData.wellEducated,
                    isService, isCommercial, isIndustrial, isExtractor, isOffice, isLeisure);

                UpdateEducationLevel(EducationIndex.HighlyEducated, workplacesData.highlyEducated, employeesData.highlyEducated,
                    isService, isCommercial, isIndustrial, isExtractor, isOffice, isLeisure);

                // Update provider counts
                providerCounts.Total++;
                if (isService) providerCounts.Service++;
                if (isCommercial)
                {
                    if (isLeisure) providerCounts.Leisure++;
                    else providerCounts.Commercial++;
                }
                if (isIndustrial)
                {
                    if (isExtractor) providerCounts.Extractor++;
                    else if (isOffice) providerCounts.Office++;
                    else providerCounts.Industrial++;
                }
            }

            // Cleanup
            entities.Dispose();
            workProviders.Dispose();
            prefabRefs.Dispose();

            m_Results[(int)EducationIndex.ProviderCounts] = providerCounts;
        }

        private void UpdateEducationLevel(
            EducationIndex level,
            int workplaces,
            int employees,
            bool isService,
            bool isCommercial,
            bool isIndustrial,
            bool isExtractor,
            bool isOffice,
            bool isLeisure)
        {
            WorkplacesInfo info = m_Results[(int)level];

            info.Total += workplaces;

            if (isService)
                info.Service += workplaces;

            if (isCommercial)
            {
                if (isLeisure)
                    info.Leisure += workplaces;
                else
                    info.Commercial += workplaces;
            }

            if (isIndustrial)
            {
                if (isExtractor)
                    info.Extractor += workplaces;
                else if (isOffice)
                    info.Office += workplaces;
                else
                    info.Industrial += workplaces;
            }

            info.Employee += employees;
            info.Open += workplaces - employees;

            m_Results[(int)level] = info;
        }

        private void CountWorkingCommuters()
        {
            int[] commutersByEducation = new int[(int)EducationIndex.Totals];

            ComponentLookup<CommuterHousehold> commuterHouseholdLookup = SystemAPI.GetComponentLookup<CommuterHousehold>(isReadOnly: true);
            ComponentLookup<CurrentDistrict> districtLookup = SystemAPI.GetComponentLookup<CurrentDistrict>(isReadOnly: true);

            EntityQuery commuterQuery = SystemAPI.QueryBuilder()
                .WithAll<Worker, Citizen, HouseholdMember>()
                .Build();

            NativeArray<Worker> workers = commuterQuery.ToComponentDataArray<Worker>(Allocator.Temp);
            NativeArray<Citizen> citizens = commuterQuery.ToComponentDataArray<Citizen>(Allocator.Temp);
            NativeArray<HouseholdMember> householdMembers = commuterQuery.ToComponentDataArray<HouseholdMember>(Allocator.Temp);

            for (int i = 0; i < citizens.Length; i++)
            {
                // Must be from a commuter household
                if (!commuterHouseholdLookup.HasComponent(householdMembers[i].m_Household))
                    continue;

                // Apply district filter if selected
                if (SelectedDistrict != Entity.Null)
                {
                    if (workers[i].m_Workplace == Entity.Null)
                        continue;

                    if (!districtLookup.HasComponent(workers[i].m_Workplace))
                        continue;

                    if (districtLookup[workers[i].m_Workplace].m_District != SelectedDistrict)
                        continue;
                }

                // Count by education level
                int educationLevel = citizens[i].GetEducationLevel();
                commutersByEducation[educationLevel]++;
            }

            // Cleanup
            workers.Dispose();
            citizens.Dispose();
            householdMembers.Dispose();

            // Assign to results
            for (int i = 0; i < (int)EducationIndex.Totals; i++)
            {
                WorkplacesInfo info = m_Results[i];
                info.Commuter = commutersByEducation[i];
                m_Results[i] = info;
            }

            // Debug logging
            int total = 0;
            for (int i = 0; i < (int)EducationIndex.Totals; i++)
                total += commutersByEducation[i];

            string districtInfo = SelectedDistrict == Entity.Null ? "Citywide" : $"District {SelectedDistrict.Index}";
            Mod.log.Info($"Working commuters ({districtInfo}): U={commutersByEducation[0]}, P={commutersByEducation[1]}, E={commutersByEducation[2]}, W={commutersByEducation[3]}, H={commutersByEducation[4]} | Total={total}");
        }

        private void CalculateTotals()
        {
            WorkplacesInfo totals = new WorkplacesInfo(-1);

            for (int i = 0; i < (int)EducationIndex.Totals; i++)
            {
                WorkplacesInfo levelData = m_Results[i];
                totals.Total += levelData.Total;
                totals.Service += levelData.Service;
                totals.Commercial += levelData.Commercial;
                totals.Leisure += levelData.Leisure;
                totals.Extractor += levelData.Extractor;
                totals.Industrial += levelData.Industrial;
                totals.Office += levelData.Office;
                totals.Employee += levelData.Employee;
                totals.Open += levelData.Open;
                totals.Commuter += levelData.Commuter;
            }

            m_Results[(int)EducationIndex.Totals] = totals;
        }

        private void ResetResults()
        {
            for (int i = 0; i < ResultsCount; i++)
            {
                m_Results[i] = new WorkplacesInfo(i);
            }
            m_Results[(int)EducationIndex.ProviderCounts] = new WorkplacesInfo(-2);
        }

        public void SetSelectedDistrict(Entity district)
        {
            SelectedDistrict = district;
            ForceUpdate = true;
        }

        public void ForceUpdateOnce()
        {
            ForceUpdate = true;
        }
    }
}
