using System.Runtime.CompilerServices;
using Colossal.UI.Binding;
using Game.Agents;
using Game.Citizens;
using Game.Common;
using Game.Simulation;
using Game.Tools;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System;
using Game.Buildings;
using Game;
using Game.Areas;
using Game.UI;
using Game.UI.InGame;
using InfoLoomTwo.Domain.DataDomain;
using InfoLoomTwo.Domain.DataDomain.Enums;
using InfoLoomTwo.Extensions;
using Unity.Burst;
using Unity.Mathematics;
using Purpose = Colossal.Serialization.Entities.Purpose;

namespace InfoLoomTwo.Systems.DemographicsData
{
    // This System is based on PopulationInfoviewUISystem by CO

    public partial class Demographics : GameSystemBase
    {
        private const int DEFAULT_AGE_CAP = 120;
		private const int TOTALS_COUNT = 10;

		private enum Totals
		{
			AllCitizens = 0,
			Locals = 1,
			Tourists = 2,
			Commuters = 3,
			Students = 4,
			Workers = 5,
			OldestCitizenAge = 6,
			MovingAways = 7,
			DeadCitizens = 8,
			HomelessCitizens = 9
		}
		
		public  void WriteData(IJsonWriter writer, PopulationAtAgeInfo info)
        {
            writer.TypeBegin("PopulationAtAgeInfo");
            writer.PropertyName("Age");
            writer.Write(info.Age);
            writer.PropertyName("Total");
            writer.Write(info.Total);
            writer.PropertyName("Work");
            writer.Write(info.Work);
            writer.PropertyName("School1");
            writer.Write(info.School1);
            writer.PropertyName("School2");
            writer.Write(info.School2);
            writer.PropertyName("School3");
            writer.Write(info.School3);
            writer.PropertyName("School4");
            writer.Write(info.School4);
            writer.PropertyName("Unemployed");
            writer.Write(info.Unemployed);
            writer.PropertyName("Retired");
            writer.Write(info.Retired);
            writer.PropertyName("ChildCount");
            writer.Write(info.ChildCount);
            writer.PropertyName("TeenCount");
            writer.Write(info.TeenCount);
            writer.PropertyName("AdultCount");
            writer.Write(info.AdultCount);
            writer.PropertyName("ElderlyCount");
            writer.Write(info.ElderlyCount);
            writer.PropertyName("Uneducated");
            writer.Write(info.Uneducated);
            writer.PropertyName("PoorlyEducated");
            writer.Write(info.PoorlyEducated);
            writer.PropertyName("Educated");
            writer.Write(info.Educated);
            writer.PropertyName("WellEducated");
            writer.Write(info.WellEducated);
            writer.PropertyName("HighlyEducated");
            writer.Write(info.HighlyEducated);
            writer.PropertyName("ChildOrTeenWithNoSchool");
            writer.Write(info.ChildOrTeenWithNoSchool);
            writer.TypeEnd();
        }


		[BurstCompile]
		private struct PopulationStructureJob : IJobChunk
		{
			[ReadOnly] public EntityTypeHandle m_EntityType;
			[ReadOnly] public ComponentTypeHandle<Citizen> m_CitizenType;
			[ReadOnly] public ComponentTypeHandle<Game.Citizens.Student> m_StudentType;
			[ReadOnly] public ComponentTypeHandle<Worker> m_WorkerType;
			[ReadOnly] public ComponentTypeHandle<HealthProblem> m_HealthProblemType;
			[ReadOnly] public ComponentTypeHandle<HouseholdMember> m_HouseholdMemberType;
			[ReadOnly] public ComponentLookup<MovingAway> m_MovingAways;
			[ReadOnly] public ComponentLookup<Household> m_Households;
			[ReadOnly] public ComponentLookup<PropertyRenter> m_PropertyRenters;
			[ReadOnly] public ComponentLookup<CurrentDistrict> m_CurrentDistrictLookup;

			public NativeArray<int> m_Totals;
			public NativeArray<PopulationAtAgeInfo> m_Results;
			public int m_CurrentDay;
			public Entity m_SelectedDistrict;

			public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
			{
				NativeArray<Entity> entities = chunk.GetNativeArray(m_EntityType);
				NativeArray<Citizen> citizenArray = chunk.GetNativeArray(ref m_CitizenType);
				NativeArray<HouseholdMember> householdMemberArray = chunk.GetNativeArray(ref m_HouseholdMemberType);

				bool hasStudents = chunk.Has(ref m_StudentType);
				bool hasWorkers = chunk.Has(ref m_WorkerType);
				bool hasHealthProblems = chunk.Has(ref m_HealthProblemType);

				NativeArray<Game.Citizens.Student> studentArray = hasStudents ? chunk.GetNativeArray(ref m_StudentType) : default;
				NativeArray<Worker> workerArray = hasWorkers ? chunk.GetNativeArray(ref m_WorkerType) : default;
				NativeArray<HealthProblem> healthProblemArray = hasHealthProblems ? chunk.GetNativeArray(ref m_HealthProblemType) : default;

				for (int i = 0; i < citizenArray.Length; i++)
				{
					Entity entity = entities[i];
					Citizen citizen = citizenArray[i];
					Entity household = householdMemberArray[i].m_Household;

					// Validate household
					if (!m_Households.TryGetComponent(household, out Household householdData) || householdData.m_Flags == HouseholdFlags.None)
						continue;

					// District filtering
					if (!IsInSelectedDistrict(household))
						continue;

					// Check for dead citizens
					if (hasHealthProblems && CitizenUtils.IsDead(healthProblemArray[i]))
					{
						m_Totals[(int)Totals.DeadCitizens]++;
						continue;
					}

					// Classify citizen type
					bool isCommuter = (citizen.m_State & CitizenFlags.Commuter) != CitizenFlags.None;
					bool isTourist = (citizen.m_State & CitizenFlags.Tourist) != CitizenFlags.None;
					bool isMovedIn = (m_Households[household].m_Flags & HouseholdFlags.MovedIn) != HouseholdFlags.None;

					m_Totals[(int)Totals.AllCitizens]++;

					if (isTourist)
					{
						m_Totals[(int)Totals.Tourists]++;
						continue;
					}

					if (isCommuter)
					{
						m_Totals[(int)Totals.Commuters]++;
						continue;
					}

					// Check for moving away
					if (m_MovingAways.HasComponent(household))
					{
						m_Totals[(int)Totals.MovingAways]++;
						continue;
					}

					// Only process moved-in locals
					if (!isMovedIn)
						continue;

					m_Totals[(int)Totals.Locals]++;

					// Calculate age and get age category
					int ageInDays = (int)math.min(m_CurrentDay - citizen.m_BirthDay, m_Results.Length - 1);
					if (ageInDays < 0 || ageInDays >= m_Results.Length)
						continue;

					CitizenAge ageCategory = citizen.GetAge();
					int educationLevel = citizen.GetEducationLevel();

					// Update age info
					PopulationAtAgeInfo info = m_Results[ageInDays];
					info.Age = ageInDays;
					info.Total++;

					// Age category counts
					switch (ageCategory)
					{
						case CitizenAge.Child:
							info.ChildCount++;
							break;
						case CitizenAge.Teen:
							info.TeenCount++;
							break;
						case CitizenAge.Adult:
							info.AdultCount++;
							break;
						case CitizenAge.Elderly:
							info.ElderlyCount++;
							break;
					}

					// Education level counts
					switch (educationLevel)
					{
						case 0: info.Uneducated++; break;
						case 1: info.PoorlyEducated++; break;
						case 2: info.Educated++; break;
						case 3: info.WellEducated++; break;
						case 4: info.HighlyEducated++; break;
					}

					// Occupation tracking - FIXED: Direct array access instead of nested loops
					bool isStudent = hasStudents && i < studentArray.Length;
					bool isWorker = hasWorkers && i < workerArray.Length;

					if (isStudent)
					{
						m_Totals[(int)Totals.Students]++;
						byte level = studentArray[i].m_Level;
						
						switch (level)
						{
							case 1: info.School1++; break;
							case 2: info.School2++; break;
							case 3: info.School3++; break;
							case 4: info.School4++; break;
						}
					}
					else if (isWorker && (ageCategory == CitizenAge.Teen || ageCategory == CitizenAge.Adult))
					{
						m_Totals[(int)Totals.Workers]++;
						info.Work++;
					}
					else
					{
						// Track occupation status for non-student/non-worker citizens
						if (ageCategory == CitizenAge.Adult)
						{
							info.Unemployed++;
						}
						else if (ageCategory == CitizenAge.Elderly)
						{
							info.Retired++;
						}
						else if (ageCategory == CitizenAge.Child || ageCategory == CitizenAge.Teen)
						{
							info.ChildOrTeenWithNoSchool++;
						}
					}

					// Track oldest citizen
					if (ageInDays > m_Totals[(int)Totals.OldestCitizenAge])
						m_Totals[(int)Totals.OldestCitizenAge] = ageInDays;

					m_Results[ageInDays] = info;
				}
			}

			private bool IsInSelectedDistrict(Entity household)
			{
				if (m_SelectedDistrict == Entity.Null)
					return true;

				if (!m_PropertyRenters.HasComponent(household))
					return false;

				Entity buildingEntity = m_PropertyRenters[household].m_Property;
				if (buildingEntity == Entity.Null)
					return false;

				if (!m_CurrentDistrictLookup.HasComponent(buildingEntity))
					return false;

				return m_CurrentDistrictLookup[buildingEntity].m_District == m_SelectedDistrict;
			}
		}

		private SimulationSystem m_SimulationSystem;
		private CountHouseholdDataSystem m_CountHouseholdDataSystem;
		private EntityQuery m_TimeDataQuery;
		private EntityQuery m_CitizenQuery;
		private EntityQuery m_DistrictQuery;
		private NameSystem m_NameSystem;
		private UIUpdateState _uiUpdateState;
		public NativeArray<int> m_Totals;
		public NativeArray<PopulationAtAgeInfo> m_Results;
		private const string ModID = "InfoLoomTwo";
		public Entity SelectedDistrict { get; set; } = Entity.Null;
		public bool IsPanelVisible { get; set; }
		public bool ForceUpdate { get; private set; }
		public int m_AgeCap { get; private set; }
		
		protected override void OnCreate()
		{
			base.OnCreate();

			m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
			m_CountHouseholdDataSystem = World.GetOrCreateSystemManaged<CountHouseholdDataSystem>();
			m_NameSystem = World.GetOrCreateSystemManaged<NameSystem>();

			m_TimeDataQuery = GetEntityQuery(ComponentType.ReadOnly<TimeData>());
			m_CitizenQuery = GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[] { ComponentType.ReadOnly<Citizen>() },
				None = new ComponentType[] { ComponentType.ReadOnly<Deleted>(), ComponentType.ReadOnly<Temp>() }
			});
			m_DistrictQuery = GetEntityQuery(ComponentType.ReadOnly<District>());

			RequireForUpdate(m_CitizenQuery);

			m_AgeCap = DEFAULT_AGE_CAP;
			m_Totals = new NativeArray<int>(TOTALS_COUNT, Allocator.Persistent);
			m_Results = new NativeArray<PopulationAtAgeInfo>(m_AgeCap, Allocator.Persistent);
		}

		protected override void OnDestroy()
		{
			if (m_Totals.IsCreated)
				m_Totals.Dispose();
			if (m_Results.IsCreated)
				m_Results.Dispose();
			base.OnDestroy();
		}

		public override int GetUpdateInterval(SystemUpdatePhase phase)
		{
			return 262144;
		}

		protected override void OnUpdate()
		{
			if (!IsPanelVisible)
				return;
			UpdateDemographics();
		}

		private void ResetResults()
		{
			for (int i = 0; i < m_Totals.Length; i++)
			{
				m_Totals[i] = 0;
			}
			for (int i = 0; i < m_Results.Length; i++)
			{
				m_Results[i] = new PopulationAtAgeInfo(i);
			}
		}
		public void SetSelectedDistrict(Entity district)
		{
			SelectedDistrict = district;
		}
		public void UpdateDemographics()
		{
			ResetResults();
			PopulationStructureJob job = new PopulationStructureJob
			{
				m_EntityType = SystemAPI.GetEntityTypeHandle(),
				m_CitizenType = SystemAPI.GetComponentTypeHandle<Citizen>(isReadOnly: true),
				m_StudentType = SystemAPI.GetComponentTypeHandle<Game.Citizens.Student>(isReadOnly: true),
				m_WorkerType = SystemAPI.GetComponentTypeHandle<Worker>(isReadOnly: true),
				m_HealthProblemType = SystemAPI.GetComponentTypeHandle<HealthProblem>(isReadOnly: true),
				m_HouseholdMemberType = SystemAPI.GetComponentTypeHandle<HouseholdMember>(isReadOnly: true),
				m_MovingAways = SystemAPI.GetComponentLookup<MovingAway>(isReadOnly: true),
				m_Households = SystemAPI.GetComponentLookup<Household>(isReadOnly: true),
				m_PropertyRenters = SystemAPI.GetComponentLookup<PropertyRenter>(isReadOnly: true),
				m_CurrentDistrictLookup = SystemAPI.GetComponentLookup<CurrentDistrict>(isReadOnly: true),
				m_Totals = m_Totals,
				m_Results = m_Results,
				m_CurrentDay = TimeSystem.GetDay(m_SimulationSystem.frameIndex, m_TimeDataQuery.GetSingleton<TimeData>()),
				m_SelectedDistrict = SelectedDistrict
			};
			Dependency = job.Schedule(m_CitizenQuery, Dependency);
			Dependency.Complete();
			m_Totals[(int)Totals.HomelessCitizens] = m_CountHouseholdDataSystem.HomelessCitizenCount;
		}
        
    }
}    
    