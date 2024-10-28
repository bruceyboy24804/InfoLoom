using System.Runtime.CompilerServices;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Common;
using Game.Companies;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Game;
using Game.UI;
using Game.Simulation;
using Game.Economy;
using Game.Citizens;
using Game.UI.InGame;
using InfoLoom;

namespace InfoLoomBrucey.Systems
{
    // Extend from ExtendedUISystemBase to use the binding helpers
    public partial class WorkplacesInfoLoomUISystem : UISystemBase
    {
        private struct WorkplacesAtLevelInfo
        {
            public int Level;
            public int Total;
            public int Service;
            public int Commercial;
            public int Leisure;
            public int Extractor;
            public int Industry;  // Changed from 'Industrial' to 'Industry'
            public int Office;
            public int Employee;
            public int Open;
            public int Commuter;

            public WorkplacesAtLevelInfo(int _level)
            {
                Level = _level;
                Total = 0;
                Service = 0;
                Commercial = 0;
                Leisure = 0;
                Extractor = 0;
                Industry = 0;
                Office = 0;
                Employee = 0;
                Open = 0;
                Commuter = 0;
            }
        }

        //[BurstCompile]
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

            public NativeArray<WorkplacesAtLevelInfo> m_Results;

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

                WorkplacesAtLevelInfo count = m_Results[6];
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
                    WorkplacesAtLevelInfo ProcessLevel(WorkplacesAtLevelInfo info, int workplaces, int employees, int commuters)
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
                            else info.Industry += workplaces; // Changed from 'Industrial' to 'Industry'
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
                        else count.Industry++; // Changed from 'Industrial' to 'Industry'
                    }
                    m_Results[6] = count;
                }
            }

            void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
            }
        }

        private struct TypeHandle
        {
            [ReadOnly]
            public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

            [ReadOnly]
            public BufferTypeHandle<Employee> __Game_Companies_Employee_RO_BufferTypeHandle;

            [ReadOnly]
            public ComponentTypeHandle<WorkProvider> __Game_Companies_WorkProvider_RO_ComponentTypeHandle;

            [ReadOnly]
            public ComponentTypeHandle<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentTypeHandle;

            [ReadOnly]
            public ComponentTypeHandle<Game.Companies.ExtractorCompany> __Game_Companies_ExtractorCompany_RO_ComponentTypeHandle;

            [ReadOnly]
            public ComponentTypeHandle<IndustrialCompany> __Game_Companies_IndustrialCompany_RO_ComponentTypeHandle;

            [ReadOnly]
            public ComponentTypeHandle<CommercialCompany> __Game_Companies_CommercialCompany_RO_ComponentTypeHandle;

            [ReadOnly]
            public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

            [ReadOnly]
            public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<WorkplaceData> __Game_Prefabs_WorkplaceData_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                __Game_Companies_Employee_RO_BufferTypeHandle = state.GetBufferTypeHandle<Employee>(isReadOnly: true);
                __Game_Companies_WorkProvider_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WorkProvider>(isReadOnly: true);
                __Game_Companies_ExtractorCompany_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Companies.ExtractorCompany>(isReadOnly: true);
                __Game_Companies_IndustrialCompany_RO_ComponentTypeHandle = state.GetComponentTypeHandle<IndustrialCompany>(isReadOnly: true);
                __Game_Companies_CommercialCompany_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CommercialCompany>(isReadOnly: true);
                __Game_Buildings_PropertyRenter_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PropertyRenter>(isReadOnly: true);
                __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
                __Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
                __Game_Prefabs_WorkplaceData_RO_ComponentLookup = state.GetComponentLookup<WorkplaceData>(isReadOnly: true);
                __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
                __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup = state.GetComponentLookup<IndustrialProcessData>(isReadOnly: true);
                __Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
            }
        }

        private const string kGroup = "workplaces";

        private SimulationSystem m_SimulationSystem;

        private EntityQuery m_WorkplaceQuery;

        private RawValueBinding m_uiResults;

        private NativeArray<WorkplacesAtLevelInfo> m_Results;

        private TypeHandle __TypeHandle;

        public override GameMode gameMode => GameMode.Game;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();

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

            m_Results = new NativeArray<WorkplacesAtLevelInfo>(7, Allocator.Persistent);

            AddBinding(m_uiResults = new RawValueBinding(kGroup, "ilWorkplaces", delegate (IJsonWriter binder)
            {
                binder.ArrayBegin(m_Results.Length);
                for (int i = 0; i < m_Results.Length; i++)
                    WriteData(binder, m_Results[i]);
                binder.ArrayEnd();
            }));

            Mod.log.Info("WorkplacesInfoLoomUISystem created.");
        }


        protected override void OnDestroy()
        {
            m_Results.Dispose();
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (m_SimulationSystem.frameIndex % 128 != 22)
                return;

            ResetResults();

            // Update handles
            __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_WorkplaceData_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Companies_WorkProvider_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Companies_ExtractorCompany_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Companies_IndustrialCompany_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Companies_CommercialCompany_RO_ComponentTypeHandle.Update(ref base.CheckedStateRef);
            __TypeHandle.__Game_Companies_Employee_RO_BufferTypeHandle.Update(ref base.CheckedStateRef);
            __TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref base.CheckedStateRef);

            // Prepare and schedule job
            CalculateWorkplaceDataJob jobData = default(CalculateWorkplaceDataJob);
            jobData.m_EntityHandle = __TypeHandle.__Unity_Entities_Entity_TypeHandle;
            jobData.m_EmployeeHandle = __TypeHandle.__Game_Companies_Employee_RO_BufferTypeHandle;
            jobData.m_WorkProviderHandle = __TypeHandle.__Game_Companies_WorkProvider_RO_ComponentTypeHandle;
            jobData.m_PropertyRenterHandle = __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle;
            jobData.m_ExtractorCompanyHandle = __TypeHandle.__Game_Companies_ExtractorCompany_RO_ComponentTypeHandle;
            jobData.m_IndustrialCompanyHandle = __TypeHandle.__Game_Companies_IndustrialCompany_RO_ComponentTypeHandle;
            jobData.m_CommercialCompanyHandle = __TypeHandle.__Game_Companies_CommercialCompany_RO_ComponentTypeHandle;
            jobData.m_PrefabRefHandle = __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
            jobData.m_PrefabRefFromEntity = __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup;
            jobData.m_WorkplaceDataFromEntity = __TypeHandle.__Game_Prefabs_WorkplaceData_RO_ComponentLookup;
            jobData.m_SpawnableBuildingFromEntity = __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;
            jobData.m_IndustrialProcessDataFromEntity = __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;
            jobData.m_CitizenFromEntity = __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup;
            jobData.m_Results = m_Results;

            JobChunkExtensions.Schedule(jobData, m_WorkplaceQuery, base.Dependency).Complete();

            

            // Calculate totals
            WorkplacesAtLevelInfo totals = new WorkplacesAtLevelInfo(-1);
            for (int i = 0; i < 5; i++)
            {
                totals.Total += m_Results[i].Total;
                totals.Service += m_Results[i].Service;
                totals.Commercial += m_Results[i].Commercial;
                totals.Leisure += m_Results[i].Leisure;
                totals.Extractor += m_Results[i].Extractor;
                totals.Industry += m_Results[i].Industry;
                totals.Office += m_Results[i].Office;
                totals.Employee += m_Results[i].Employee;
                totals.Commuter += m_Results[i].Commuter;
                totals.Open += m_Results[i].Open;
            }
            m_Results[5] = totals;

            // Update UI bindings
            m_uiResults.Update();
        }

        private void ResetResults()
        {
            for (int i = 0; i < 6; i++) // there are 5 education levels + 1 for totals
            {
                m_Results[i] = new WorkplacesAtLevelInfo(i);
            }
            m_Results[6] = new WorkplacesAtLevelInfo(-2);
        }

        private static void WriteData(IJsonWriter writer, WorkplacesAtLevelInfo info)
        {
            writer.TypeBegin("workplacesAtLevelInfo");
            writer.PropertyName("level"); writer.Write(info.Level);
            writer.PropertyName("total"); writer.Write(info.Total);
            writer.PropertyName("service"); writer.Write(info.Service);
            writer.PropertyName("commercial"); writer.Write(info.Commercial);
            writer.PropertyName("leisure"); writer.Write(info.Leisure);
            writer.PropertyName("extractor"); writer.Write(info.Extractor);
            writer.PropertyName("industry"); writer.Write(info.Industry); // Ensure property name is 'industry'
            writer.PropertyName("office"); writer.Write(info.Office);
            writer.PropertyName("employee"); writer.Write(info.Employee);
            writer.PropertyName("open"); writer.Write(info.Open);
            writer.PropertyName("commuter"); writer.Write(info.Commuter);
            writer.TypeEnd(); ;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void __AssignQueries(ref SystemState state)
        {
        }

        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            __AssignQueries(ref base.CheckedStateRef);
            __TypeHandle.__AssignHandles(ref base.CheckedStateRef);
        }

        public WorkplacesInfoLoomUISystem()
        {
        }
    }
}
