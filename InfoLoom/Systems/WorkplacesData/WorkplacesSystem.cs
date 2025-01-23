using System.Runtime.CompilerServices;
using Colossal.UI.Binding;
using Game;
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



namespace InfoLoomTwo.Systems.WorkplacesData
{
    public partial class WorkplacesSystem : SystemBase
    {

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

            public NativeArray<WorkplacesUISystem.workplacesInfo> m_Results;
            private const int ResultsCount = 7;

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

                WorkplacesUISystem.workplacesInfo count = m_Results[6];
                for (int i = 0; i < nativeArray.Length; i++)
                {
                    int buildingLevel = 1;
                    WorkProvider workProvider = nativeArray4[i];
                    DynamicBuffer<Employee> employees = bufferAccessor[i];
                    PrefabRef prefabRef = nativeArray2[i];
                    WorkplaceData workplaceData = m_WorkplaceDataFromEntity[prefabRef.m_Prefab];

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
                    int[] commuters = new int[5]; // by level
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
                    WorkplacesUISystem.workplacesInfo ProcessLevel(WorkplacesUISystem.workplacesInfo info, int workplaces, int employees, int commuters)
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

                    // uneducated
                    m_Results[0] = ProcessLevel(m_Results[0], workplacesData.uneducated, employeesData.uneducated, commuters[0]);
                    // poorlyEducated
                    m_Results[1] = ProcessLevel(m_Results[1], workplacesData.poorlyEducated, employeesData.poorlyEducated, commuters[1]);
                    // educated
                    m_Results[2] = ProcessLevel(m_Results[2], workplacesData.educated, employeesData.educated, commuters[2]);
                    // wellEducated
                    m_Results[3] = ProcessLevel(m_Results[3], workplacesData.wellEducated, employeesData.wellEducated, commuters[3]);
                    // highlyEducated
                    m_Results[4] = ProcessLevel(m_Results[4], workplacesData.highlyEducated, employeesData.highlyEducated, commuters[4]);

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
                    m_Results[6] = count;
                }
            }

            void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
            }
        }



        private const string kGroup = "workplaces";

        private SimulationSystem m_SimulationSystem;

        private EntityQuery m_WorkplaceQuery;



        public NativeArray<WorkplacesUISystem.workplacesInfo> m_Results;
        private const int ResultsCount = 7;




        protected override void OnCreate()
        {
            base.OnCreate();
            m_Results = new NativeArray<WorkplacesUISystem.workplacesInfo>(7, Allocator.Persistent);
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




        protected override void OnUpdate()
        {
            if (m_SimulationSystem.frameIndex % 128 != 22)
                return;

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
            jobData.m_Results = m_Results;

            JobChunkExtensions.Schedule(jobData, m_WorkplaceQuery, base.Dependency).Complete();



            // Calculate totals
            WorkplacesUISystem.workplacesInfo totals = new WorkplacesUISystem.workplacesInfo(-1);
            for (int i = 0; i < 5; i++)
            {
                totals.Total += m_Results[i].Total;
                totals.Service += m_Results[i].Service;
                totals.Commercial += m_Results[i].Commercial;
                totals.Leisure += m_Results[i].Leisure;
                totals.Extractor += m_Results[i].Extractor;
                totals.Industrial += m_Results[i].Industrial;
                totals.Office += m_Results[i].Office;
                totals.Employee += m_Results[i].Employee;
                totals.Commuter += m_Results[i].Commuter;
                totals.Open += m_Results[i].Open;
            }
            m_Results[5] = totals;

            // Update UI bindings

        }

        private void ResetResults()
        {
            for (int i = 0; i < ResultsCount; i++)
            {
                m_Results[i] = new WorkplacesUISystem.workplacesInfo(i);
            }
            m_Results[6] = new WorkplacesUISystem.workplacesInfo(-2);
        }




    }
}
