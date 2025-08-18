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
using Unity.Burst.Intrinsics;
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
        private struct CalculateWorkplaceDataJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityHandle;

            [ReadOnly]
            public BufferTypeHandle<Employee> m_EmployeeHandle;

            [ReadOnly]
            public ComponentTypeHandle<WorkProvider> m_WorkProviderHandle;

            [ReadOnly]
            public ComponentTypeHandle<PropertyRenter> m_PropertyRenterHandle;

            [ReadOnly]
            public ComponentTypeHandle<PrefabRef> m_PrefabRefHandle;

            [ReadOnly]
            public ComponentTypeHandle<IndustrialCompany> m_IndustrialCompanyHandle;

            [ReadOnly]
            public ComponentTypeHandle<Game.Companies.ExtractorCompany> m_ExtractorCompanyHandle;

            [ReadOnly]
            public ComponentTypeHandle<CommercialCompany> m_CommercialCompanyHandle;

            [ReadOnly]
            public ComponentLookup<PrefabRef> m_PrefabRefFromEntity;

            [ReadOnly]
            public ComponentLookup<WorkplaceData> m_WorkplaceDataFromEntity;

            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingFromEntity;

            [ReadOnly]
            public ComponentLookup<IndustrialProcessData> m_IndustrialProcessDataFromEntity;

            [ReadOnly]
            public ComponentLookup<Citizen> m_CitizenFromEntity;

            public NativeArray<WorkplacesInfo> m_Results;
            private const int ResultsCount = 7;
            [ReadOnly] public ComponentLookup<CurrentDistrict> m_CurrentDistrictLookup; 
            public Entity m_SelectedDistrict; 
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityHandle);
                NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefHandle);
                NativeArray<PropertyRenter> nativeArray3 = chunk.GetNativeArray(ref m_PropertyRenterHandle);
                NativeArray<WorkProvider> nativeArray4 = chunk.GetNativeArray(ref m_WorkProviderHandle);
                BufferAccessor<Employee> bufferAccessor = chunk.GetBufferAccessor(ref m_EmployeeHandle);

                bool isExtractor = chunk.Has(ref m_ExtractorCompanyHandle);
                bool isIndustrial = chunk.Has(ref m_IndustrialCompanyHandle);
                bool isCommercial = chunk.Has(ref m_CommercialCompanyHandle);
                bool isService = !(isIndustrial || isCommercial);
                bool hasPropertyRenter = nativeArray3.IsCreated && nativeArray3.Length > 0;

                WorkplacesInfo count = m_Results[(int)EducationIndex.ProviderCounts];
                for (int i = 0; i < nativeArray.Length; i++)
                {
                    int buildingLevel = 1;
                    WorkProvider workProvider = nativeArray4[i];
                    DynamicBuffer<Employee> employees = bufferAccessor[i];
                    PrefabRef prefabRef = nativeArray2[i];
                    WorkplaceData workplaceData = m_WorkplaceDataFromEntity[prefabRef.m_Prefab];
                    // Then use it safely
                    Entity workplace = nativeArray[i];
                    if (hasPropertyRenter && nativeArray3.Length > i)
                    {
                        workplace = nativeArray3[i].m_Property;
                    }

                    if (m_SelectedDistrict != Entity.Null && !IsInSelectedDistrict(nativeArray[i], workplace))
                        continue;
                    
                    if (chunk.Has(ref m_PropertyRenterHandle))
                    {
                        buildingLevel = m_PrefabRefFromEntity.TryGetComponent(nativeArray3[i].m_Property, out PrefabRef prefabRef2)
                               && m_SpawnableBuildingFromEntity.TryGetComponent(prefabRef2.m_Prefab, out SpawnableBuildingData componentData2) ? (int)componentData2.m_Level : 1;
                    }
                    // this holds workplaces for each level
                    EmploymentData workplacesData = EmploymentData.GetWorkplacesData(workProvider.m_MaxWorkers, buildingLevel, workplaceData.m_Complexity);
                    // this holds employees for each level
                    EmploymentData employeesData = EmploymentData.GetEmployeesData(employees, workplacesData.total - employees.Length);

                    // Determine if the company is an office or leisure
                    bool isOffice = false;
                    bool isLeisure = false;
                    if (m_IndustrialProcessDataFromEntity.HasComponent(prefabRef.m_Prefab))
                    {
                        IndustrialProcessData process = m_IndustrialProcessDataFromEntity[prefabRef.m_Prefab];
                        Resource outputRes = process.m_Output.m_Resource;
                        isLeisure = (outputRes & (Resource.Meals | Resource.Entertainment | Resource.Recreation | Resource.Lodging)) != Resource.NoResource;
                        isOffice = (outputRes & (Resource.Software | Resource.Telecom | Resource.Financial | Resource.Media)) != Resource.NoResource;
                    }

                    // Count Commuters among Employees
                    int[] commuters = new int[(int)EducationIndex.Totals]; // by level
                    for (int k = 0; k < employees.Length; k++)
                    {
                        Entity worker = employees[k].m_Worker;
                        if (m_CitizenFromEntity.HasComponent(worker))
                        {
                            Citizen citizen = m_CitizenFromEntity[worker];
                            if ((citizen.m_State & CitizenFlags.Commuter) != CitizenFlags.None)
                                commuters[employees[k].m_Level]++;
                        }
                    }

                    // Work with a local variable to avoid CS0206 error
                    WorkplacesInfo ProcessLevel(WorkplacesInfo info, int workplaces, int employees, int commuters)
                    {
                        info.Total += workplaces;
                        if (isService) info.Service += workplaces;
                        if (isCommercial)
                        {
                            if (isLeisure) info.Leisure += workplaces;
                            else info.Commercial += workplaces;
                        }
                        if (isIndustrial)
                        {
                            if (isExtractor) info.Extractor += workplaces;
                            else if (isOffice) info.Office += workplaces;
                            else info.Industrial += workplaces; // Changed from 'Industrial' to 'Industry'
                        }
                        info.Employee += employees;
                        info.Open += workplaces - employees;
                        info.Commuter += commuters;
                        return info;
                    }
                    m_Results[(int)EducationIndex.Uneducated] = ProcessLevel(m_Results[(int)EducationIndex.Uneducated], workplacesData.uneducated, employeesData.uneducated, commuters[(int)EducationIndex.Uneducated]);
                    // poorlyEducated
                    m_Results[(int)EducationIndex.PoorlyEducated] = ProcessLevel(m_Results[(int)EducationIndex.PoorlyEducated], workplacesData.poorlyEducated, employeesData.poorlyEducated, commuters[(int)EducationIndex.PoorlyEducated]);
                    // educated
                    m_Results[(int)EducationIndex.Educated] = ProcessLevel(m_Results[(int)EducationIndex.Educated], workplacesData.educated, employeesData.educated, commuters[(int)EducationIndex.Educated]);
                    // wellEducated
                    m_Results[(int)EducationIndex.WellEducated] = ProcessLevel(m_Results[(int)EducationIndex.WellEducated], workplacesData.wellEducated, employeesData.wellEducated, commuters[(int)EducationIndex.WellEducated]);
                    // highlyEducated
                    m_Results[(int)EducationIndex.HighlyEducated] = ProcessLevel(m_Results[(int)EducationIndex.HighlyEducated], workplacesData.highlyEducated, employeesData.highlyEducated, commuters[(int)EducationIndex.HighlyEducated]);

                    // Count work providers
                    count.Total++;
                    if (isService) count.Service++;
                    if (isCommercial)
                    {
                        if (isLeisure) count.Leisure++;
                        else count.Commercial++;
                    }
                    if (isIndustrial)
                    {
                        if (isExtractor) count.Extractor++;
                        else if (isOffice) count.Office++;
                        else count.Industrial++; // Changed from 'Industrial' to 'Industry'
                    }
                    m_Results[(int)EducationIndex.ProviderCounts] = count;
                }
            }
            private bool IsInSelectedDistrict(Entity companyEntity, Entity buildingEntity)
            {
                if (m_SelectedDistrict == Entity.Null)
                    return true;

                // Try building first
                if (buildingEntity != Entity.Null && m_CurrentDistrictLookup.HasComponent(buildingEntity))
                {
                    return m_CurrentDistrictLookup[buildingEntity].m_District == m_SelectedDistrict;
                }

                // Fallback to company entity
                if (m_CurrentDistrictLookup.HasComponent(companyEntity))
                {
                    return m_CurrentDistrictLookup[companyEntity].m_District == m_SelectedDistrict;
                }

                return false;
            }
            void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
            }
        }



        private const string kGroup = "workplaces";

        private SimulationSystem m_SimulationSystem;

        private EntityQuery m_WorkplaceQuery;
        public Entity SelectedDistrict { get; set; } = Entity.Null;


        public NativeArray<WorkplacesInfo> m_Results;
        private const int ResultsCount = 7;


 
        public bool IsPanelVisible { get; set; }
        public bool ForceUpdate { get; private set; }
        public void ForceUpdateOnce()
        {
            ForceUpdate = true;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Results = new NativeArray<WorkplacesInfo>(7, Allocator.Persistent);
            m_WorkplaceQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
            {
                ComponentType.ReadOnly<Employee>(),
                ComponentType.ReadOnly<WorkProvider>(),
                ComponentType.ReadOnly<PrefabRef>()
            },
                Any = new ComponentType[]
            {
                ComponentType.ReadOnly<PropertyRenter>(),
                ComponentType.ReadOnly<Building>()
            },
                None = new ComponentType[]
            {
                ComponentType.ReadOnly<Game.Objects.OutsideConnection>(),
                ComponentType.ReadOnly<Temp>()
            }
            });
            m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
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

            // Update handles


            // Prepare and schedule job
            CalculateWorkplaceDataJob jobData = default(CalculateWorkplaceDataJob);
            jobData.m_EntityHandle = SystemAPI.GetEntityTypeHandle();
            jobData.m_EmployeeHandle = SystemAPI.GetBufferTypeHandle<Employee>(isReadOnly: true);
            jobData.m_WorkProviderHandle = SystemAPI.GetComponentTypeHandle<WorkProvider>(isReadOnly: true);
            jobData.m_PropertyRenterHandle = SystemAPI.GetComponentTypeHandle<PropertyRenter>(isReadOnly: true);
            jobData.m_ExtractorCompanyHandle = SystemAPI.GetComponentTypeHandle<Game.Companies.ExtractorCompany>(isReadOnly: true);
            jobData.m_IndustrialCompanyHandle = SystemAPI.GetComponentTypeHandle<IndustrialCompany>(isReadOnly: true);
            jobData.m_CommercialCompanyHandle = SystemAPI.GetComponentTypeHandle<CommercialCompany>(isReadOnly: true);
            jobData.m_PrefabRefHandle = SystemAPI.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
            jobData.m_PrefabRefFromEntity = SystemAPI.GetComponentLookup<PrefabRef>(isReadOnly: true);
            jobData.m_WorkplaceDataFromEntity = SystemAPI.GetComponentLookup<WorkplaceData>(isReadOnly: true);
            jobData.m_SpawnableBuildingFromEntity = SystemAPI.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
            jobData.m_IndustrialProcessDataFromEntity = SystemAPI.GetComponentLookup<IndustrialProcessData>(isReadOnly: true);
            jobData.m_CitizenFromEntity = SystemAPI.GetComponentLookup<Citizen>(isReadOnly: true);
            jobData.m_CurrentDistrictLookup = SystemAPI.GetComponentLookup<CurrentDistrict>(isReadOnly: true);
            jobData.m_SelectedDistrict = SelectedDistrict;
            jobData.m_Results = m_Results;

            JobChunkExtensions.Schedule(jobData, m_WorkplaceQuery, base.Dependency).Complete();



            // Calculate totals
            WorkplacesInfo totals = new WorkplacesInfo(-1);
            for (int i = 0; i < (int)EducationIndex.Totals; i++)
            {
                var educationLevel = (EducationIndex)i;
                totals.Total += m_Results[(int)educationLevel].Total;
                totals.Service += m_Results[(int)educationLevel].Service;
                totals.Commercial += m_Results[(int)educationLevel].Commercial;
                totals.Leisure += m_Results[(int)educationLevel].Leisure;
                totals.Extractor += m_Results[(int)educationLevel].Extractor;
                totals.Industrial += m_Results[(int)educationLevel].Industrial;
                totals.Office += m_Results[(int)educationLevel].Office;
                totals.Employee += m_Results[(int)educationLevel].Employee;
                totals.Commuter += m_Results[(int)educationLevel].Commuter;
                totals.Open += m_Results[(int)educationLevel].Open;
            }
            m_Results[(int)EducationIndex.Totals] = totals;

            // Update UI bindings

        }

        private void ResetResults()
        {
            for (int i = 0; i < ResultsCount; i++)
            {
                m_Results[i] = new WorkplacesInfo(i);
            }
            m_Results[6] = new WorkplacesInfo(-2);
        }
        public void SetSelectedDistrict(Entity district)
        {
            SelectedDistrict = district;
        }



    }
}
