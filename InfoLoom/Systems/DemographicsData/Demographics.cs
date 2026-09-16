using Game.Agents;
using Game.Citizens;
using Game.Common;
using Game.Simulation;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using System.Collections.Generic;
using System;
using Game.Buildings;
using Game;
using Game.Areas;
using Game.UI;
using Game.Prefabs;
using Game.UI.InGame;
using InfoLoomTwo.Domain.DataDomain;
using Unity.Burst;


namespace InfoLoomTwo.Systems.DemographicsData
{
    // This System is based on PopulationInfoviewUISystem by CO

    public partial class Demographics : GameSystemBase
    {
        private const int DEFAULT_AGE_CAP = 120;
		private const int FIVE_YEAR_GROUP_COUNT = DEFAULT_AGE_CAP / 5;
		private const int TEN_YEAR_GROUP_COUNT = DEFAULT_AGE_CAP / 10;
		private const int TOTALS_COUNT = 10;
		private const int LIFECYCLE_TOTALS_COUNT = 4;
		private enum LifecycleTotals
		{
			Child = 0,
			Teen = 1,
			Adult = 2,
			Elderly = 3
		}
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

		// Density tier of the building a citizen actually lives in. Mirrors the game's own
		// PropertyUtils.GetZoneDensity/IsMixedBuilding classification (same source the vanilla
		// Residential Demand panel uses), plus an Unhoused bucket for homeless citizens or any
		// household whose property can't be resolved.
		private enum ResidencyType
		{
			Low = 0,
			Medium = 1,
			High = 2,
			Mixed = 3,
			Unhoused = 4
		}

		// What a citizen is doing day-to-day. Mirrors the existing mutually-exclusive
		// Work/School1-4/Retired/Unemployed/ChildOrTeenWithNoSchool classification already
		// computed per-citizen below, just captured as a single index instead of separate counters.
		public enum ActivityType
		{
			Work = 0,
			School1 = 1,
			School2 = 2,
			School3 = 3,
			School4 = 4,
			Retired = 5,
			Unemployed = 6,
			ChildOrTeenWithNoSchool = 7
		}

		// The "columns" a citizen can be classified along for the free-form census cross-tab.
		// Values are stable and referenced from the UI layer, so don't renumber existing entries.
		public enum CensusDimension
		{
			Age = 0,
			Education = 1,
			Wealth = 2,
			Residency = 3,
			Activity = 4
		}

		private const int CENSUS_DIMENSION_COUNT = 5;

		// How finely to bucket the Age dimension when it's chosen as a cross-tab axis. Every
		// other dimension only ever has one resolution; Age is the one that can be as coarse as
		// 4 lifecycle stages or as fine as a single simulation-day-equivalent unit, because we
		// keep each citizen's exact age (not just their lifecycle bucket) in CitizenCensusRecord.
		public enum AgeGranularity
		{
			Detailed = 0,
			FiveYear = 1,
			TenYear = 2,
			Lifecycle = 3
		}

		public static int GetAgeBucketCount(AgeGranularity granularity)
		{
			return granularity switch
			{
				AgeGranularity.Detailed => DEFAULT_AGE_CAP,
				AgeGranularity.FiveYear => FIVE_YEAR_GROUP_COUNT,
				AgeGranularity.TenYear => TEN_YEAR_GROUP_COUNT,
				_ => LIFECYCLE_TOTALS_COUNT
			};
		}

		public static int GetCensusCategoryCount(CensusDimension dimension, AgeGranularity ageGranularity = AgeGranularity.Lifecycle)
		{
			return dimension switch
			{
				CensusDimension.Age => GetAgeBucketCount(ageGranularity),
				CensusDimension.Education => 5,
				CensusDimension.Wealth => 5,
				CensusDimension.Residency => 5,
				CensusDimension.Activity => 8,
				_ => 1
			};
		}

		// One compact record per counted citizen (dead/tourist/commuter/moving-away/not-moved-in
		// citizens never reach this — same population as every other table on this panel). This
		// is the single source of truth for the interactive census panel: every dimension a
		// citizen can be sliced by is captured once, at full resolution, so any pair of
		// dimensions — at any age granularity — can be cross-tabbed later without re-running the
		// per-citizen job. Kept as plain fields so a full city's worth of these is still only a
		// few hundred KB.
		private struct CitizenCensusRecord
		{
			public short AgeInDays;
			public byte Lifecycle;
			public byte Education;
			public byte Wealth;
			public byte Residency;
			public byte Activity;

			public int Get(CensusDimension dimension, AgeGranularity ageGranularity)
			{
				if (dimension == CensusDimension.Age)
				{
					return ageGranularity switch
					{
						AgeGranularity.Detailed => AgeInDays,
						AgeGranularity.FiveYear => AgeInDays / 5,
						AgeGranularity.TenYear => AgeInDays / 10,
						_ => Lifecycle
					};
				}
				return dimension switch
				{
					CensusDimension.Education => Education,
					CensusDimension.Wealth => Wealth,
					CensusDimension.Residency => Residency,
					CensusDimension.Activity => Activity,
					_ => 0
				};
			}
		}


        [BurstCompile]
        private partial struct PopulationStructureJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<Game.Citizens.Student> m_Students;
            [ReadOnly] public ComponentLookup<Worker> m_Workers;
            [ReadOnly] public ComponentLookup<HealthProblem> m_HealthProblems;
            [ReadOnly] public ComponentLookup<MovingAway> m_MovingAways;
            [ReadOnly] public ComponentLookup<Household> m_Households;
            [ReadOnly] public ComponentLookup<PropertyRenter> m_PropertyRenters;
            [ReadOnly] public ComponentLookup<CurrentDistrict> m_CurrentDistrictLookup;

            // Wealth classification inputs
            [ReadOnly] public BufferLookup<Game.Economy.Resources> m_HouseholdResourceBufs;
            public CitizenHappinessParameterData m_HappinessParameters;

            // Residency classification inputs — resolve household -> building -> zone prefab,
            // same chain PropertyUtils.GetZoneDensity/IsMixedBuilding expect.
            [ReadOnly] public ComponentLookup<PrefabRef> m_PrefabRefs;
            [ReadOnly] public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDatas;
            [ReadOnly] public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingDatas;
            [ReadOnly] public ComponentLookup<ZoneData> m_ZoneDatas;
            [ReadOnly] public ComponentLookup<ZonePropertiesData> m_ZonePropertiesDatas;

            public NativeArray<int> m_Totals;
            public NativeArray<PopulationDetailedGroupInfo> m_Results;

            public NativeArray<PopulationFiveYearGroupInfo> m_FiveYearDetails;

            public NativeArray<PopulationTenYearGroupInfo> m_TenYearDetails;

            public NativeArray<int> m_LifecycleTotals;
            public NativeArray<PopulationLifecycleInfo> m_LifecycleDetails;

            // One record per counted citizen, tagged with every dimension at once (at full age
            // resolution), so the interactive census panel can cross-tab any two of them —
            // Wealth and Residency no longer need their own fixed lifecycle-bucketed tables.
            public NativeList<CitizenCensusRecord> m_CensusRecords;

            public int m_CurrentDay;
            public Entity m_SelectedDistrict;

            private void Execute(Entity entity, in Citizen citizen, in HouseholdMember member)
            {
                Entity household = member.m_Household;

                // Validate household
                if (!m_Households.TryGetComponent(household, out Household householdData) || householdData.m_Flags == HouseholdFlags.None)
                    return;

                // District filtering
                if (!IsInSelectedDistrict(household))
                    return;

                // Check for dead citizens
                if (m_HealthProblems.TryGetComponent(entity, out HealthProblem healthProblem) && CitizenUtils.IsDead(healthProblem))
                {
                    m_Totals[(int)Totals.DeadCitizens]++;
                    return;
                }

                // Classify citizen type
                bool isCommuter = (citizen.m_State & CitizenFlags.Commuter) != CitizenFlags.None;
                bool isTourist = (citizen.m_State & CitizenFlags.Tourist) != CitizenFlags.None;
                bool isMovedIn = (m_Households[household].m_Flags & HouseholdFlags.MovedIn) != HouseholdFlags.None;

                m_Totals[(int)Totals.AllCitizens]++;

                if (isTourist)
                {
                    m_Totals[(int)Totals.Tourists]++;
                    return;
                }

                if (isCommuter)
                {
                    m_Totals[(int)Totals.Commuters]++;
                    return;
                }

                // Check for moving away
                if (m_MovingAways.HasComponent(household))
                {
                    m_Totals[(int)Totals.MovingAways]++;
                    return;
                }

                // Only process moved-in locals
                if (!isMovedIn)
                    return;

                m_Totals[(int)Totals.Locals]++;

                // Calculate age and get age category
                int ageInDays = m_CurrentDay - citizen.m_BirthDay;
                if (ageInDays < 0 || ageInDays >= m_Results.Length)
                    return;

                CitizenAge ageCategory = citizen.GetAge();
                int educationLevel = citizen.GetEducationLevel();

                // Update age info
                PopulationDetailedGroupInfo info = m_Results[ageInDays];
                info.Age = ageInDays;
                info.Total++;

                // Determine lifecycle index and update age category counts
                int lifecycleIndex;
                switch (ageCategory)
                {
                    case CitizenAge.Child:
                        lifecycleIndex = (int)LifecycleTotals.Child;
                        m_LifecycleTotals[lifecycleIndex]++;
                        break;
                    case CitizenAge.Teen:
                        lifecycleIndex = (int)LifecycleTotals.Teen;
                        m_LifecycleTotals[lifecycleIndex]++;
                        break;
                    case CitizenAge.Adult:
                        lifecycleIndex = (int)LifecycleTotals.Adult;
                        m_LifecycleTotals[lifecycleIndex]++;
                        break;
                    case CitizenAge.Elderly:
                        lifecycleIndex = (int)LifecycleTotals.Elderly;
                        m_LifecycleTotals[lifecycleIndex]++;
                        break;
                    default:
                        lifecycleIndex = (int)LifecycleTotals.Adult;
                        break;
                }

                PopulationLifecycleInfo lifecycleInfo = m_LifecycleDetails[lifecycleIndex];
                lifecycleInfo.Total++;

                // Education level counts
                switch (educationLevel)
                {
                    case 0:
                        info.Uneducated++;
                        lifecycleInfo.Uneducated++;
                        break;
                    case 1:
                        info.PoorlyEducated++;
                        lifecycleInfo.PoorlyEducated++;
                        break;
                    case 2:
                        info.Educated++;
                        lifecycleInfo.Educated++;
                        break;
                    case 3:
                        info.WellEducated++;
                        lifecycleInfo.WellEducated++;
                        break;
                    case 4:
                        info.HighlyEducated++;
                        lifecycleInfo.HighlyEducated++;
                        break;
                }

                // Occupation tracking
                ActivityType activity;
                if (m_Students.TryGetComponent(entity, out Game.Citizens.Student student))
                {
                    m_Totals[(int)Totals.Students]++;
                    byte level = student.m_Level;

                    switch (level)
                    {
                        case 1:
                            info.School1++;
                            lifecycleInfo.School1++;
                            activity = ActivityType.School1;
                            break;
                        case 2:
                            info.School2++;
                            lifecycleInfo.School2++;
                            activity = ActivityType.School2;
                            break;
                        case 3:
                            info.School3++;
                            lifecycleInfo.School3++;
                            activity = ActivityType.School3;
                            break;
                        case 4:
                            info.School4++;
                            lifecycleInfo.School4++;
                            activity = ActivityType.School4;
                            break;
                        default:
                            activity = ActivityType.School1;
                            break;
                    }
                }
                else if (m_Workers.HasComponent(entity) && (ageCategory == CitizenAge.Teen || ageCategory == CitizenAge.Adult))
                {
                    m_Totals[(int)Totals.Workers]++;
                    info.Work++;
                    lifecycleInfo.Work++;
                    activity = ActivityType.Work;
                }
                else
                {
                    // Track occupation status for non-student/non-worker citizens
                    if (ageCategory == CitizenAge.Adult)
                    {
                        info.Unemployed++;
                        lifecycleInfo.Unemployed++;
                        activity = ActivityType.Unemployed;
                    }
                    else if (ageCategory == CitizenAge.Elderly)
                    {
                        info.Retired++;
                        lifecycleInfo.Retired++;
                        activity = ActivityType.Retired;
                    }
                    else
                    {
                        info.ChildOrTeenWithNoSchool++;
                        lifecycleInfo.ChildOrTeenWithNoSchool++;
                        activity = ActivityType.ChildOrTeenWithNoSchool;
                    }
                }

                // Wealth classification — same tiers/thresholds as the vanilla wealth panel
                int householdWealth = m_HouseholdResourceBufs.HasBuffer(household)
                    ? Game.Economy.EconomyUtils.GetHouseholdTotalWealth(householdData, m_HouseholdResourceBufs[household])
                    : householdData.m_Resources;
                HouseholdWealthKey wealthKey = GetHouseholdWealthKey(householdWealth);

                // Residency classification — density tier of the building the citizen lives in
                ResidencyType residencyType = GetResidencyType(household);

                // Record every dimension for this citizen, at full age resolution, so the census
                // panel can cross-tab any pair of them (including Age at any granularity) later.
                m_CensusRecords.Add(new CitizenCensusRecord
                {
                    AgeInDays = (short)ageInDays,
                    Lifecycle = (byte)lifecycleIndex,
                    Education = (byte)educationLevel,
                    Wealth = (byte)wealthKey,
                    Residency = (byte)residencyType,
                    Activity = (byte)activity
                });

                // Track oldest citizen
                if (ageInDays > m_Totals[(int)Totals.OldestCitizenAge])
                    m_Totals[(int)Totals.OldestCitizenAge] = ageInDays;

                m_Results[ageInDays] = info;
                m_LifecycleDetails[lifecycleIndex] = lifecycleInfo;
            }

            private HouseholdWealthKey GetHouseholdWealthKey(int wealth)
            {
                if (wealth < m_HappinessParameters.m_WealthyMoneyAmount.x)
                    return HouseholdWealthKey.Wretched;
                if (wealth < m_HappinessParameters.m_WealthyMoneyAmount.y)
                    return HouseholdWealthKey.Poor;
                if (wealth < m_HappinessParameters.m_WealthyMoneyAmount.z)
                    return HouseholdWealthKey.Modest;
                if (wealth < m_HappinessParameters.m_WealthyMoneyAmount.w)
                    return HouseholdWealthKey.Comfortable;
                return HouseholdWealthKey.Wealthy;
            }

            private ResidencyType GetResidencyType(Entity household)
            {
                if (!m_PropertyRenters.HasComponent(household))
                    return ResidencyType.Unhoused;

                Entity buildingEntity = m_PropertyRenters[household].m_Property;
                if (buildingEntity == Entity.Null || !m_PrefabRefs.HasComponent(buildingEntity))
                    return ResidencyType.Unhoused;

                Entity prefab = m_PrefabRefs[buildingEntity].m_Prefab;
                if (!m_BuildingPropertyDatas.HasComponent(prefab) || !m_SpawnableBuildingDatas.HasComponent(prefab))
                    return ResidencyType.Unhoused;

                if (PropertyUtils.IsMixedBuilding(prefab, ref m_BuildingPropertyDatas))
                    return ResidencyType.Mixed;

                Entity zonePrefab = m_SpawnableBuildingDatas[prefab].m_ZonePrefab;
                if (!m_ZoneDatas.HasComponent(zonePrefab) || !m_ZonePropertiesDatas.HasComponent(zonePrefab))
                    return ResidencyType.Unhoused;

                ZoneData zoneData = m_ZoneDatas[zonePrefab];
                if (zoneData.m_AreaType != Game.Zones.AreaType.Residential)
                    return ResidencyType.Unhoused;

                return PropertyUtils.GetZoneDensity(zoneData, m_ZonePropertiesDatas[zonePrefab]) switch
                {
                    ZoneDensity.Low => ResidencyType.Low,
                    ZoneDensity.Medium => ResidencyType.Medium,
                    ZoneDensity.High => ResidencyType.High,
                    _ => ResidencyType.Unhoused
                };
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
        private NameSystem m_NameSystem;
        private UIUpdateState _uiUpdateState;
        public NativeArray<int> m_Totals;
        public NativeArray<PopulationDetailedGroupInfo> m_Results;
        public NativeArray<PopulationFiveYearGroupInfo> m_FiveYearDetails;
        public NativeArray<PopulationTenYearGroupInfo> m_TenYearDetails;
        public NativeArray<int> m_LifecycleTotals;
        public NativeArray<PopulationLifecycleInfo> m_LifecycleDetails;

        // Free-form census cross-tab: one tagged record per citizen, plus the currently
        // selected row/column dimensions (and age granularity) and the resulting flattened
        // (row-major) count matrix.
        private NativeList<CitizenCensusRecord> m_CensusRecords;
        public NativeArray<int> m_CensusCrossTabCounts;
        public CensusDimension CensusRowDimension { get; private set; } = CensusDimension.Age;
        public CensusDimension CensusColDimension { get; private set; } = CensusDimension.Activity;
        public AgeGranularity CensusAgeGranularity { get; private set; } = AgeGranularity.Lifecycle;

        private const string ModID = "InfoLoomTwo";
        public Entity SelectedDistrict { get; set; } = Entity.Null;
        public bool IsPanelVisible { get; set; }
        public int m_AgeCap { get; private set; }
       


        protected override void OnCreate()
        {
            base.OnCreate();

            m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
            m_CountHouseholdDataSystem = World.GetOrCreateSystemManaged<CountHouseholdDataSystem>();
            m_NameSystem = World.GetOrCreateSystemManaged<NameSystem>();




            m_AgeCap = DEFAULT_AGE_CAP;
            m_Totals = new NativeArray<int>(TOTALS_COUNT, Allocator.Persistent);
            m_Results = new NativeArray<PopulationDetailedGroupInfo>(m_AgeCap, Allocator.Persistent);
            m_FiveYearDetails = new NativeArray<PopulationFiveYearGroupInfo>(FIVE_YEAR_GROUP_COUNT, Allocator.Persistent);
            m_TenYearDetails = new NativeArray<PopulationTenYearGroupInfo>(TEN_YEAR_GROUP_COUNT, Allocator.Persistent);
            m_LifecycleTotals = new NativeArray<int>(LIFECYCLE_TOTALS_COUNT, Allocator.Persistent);
            m_LifecycleDetails = new NativeArray<PopulationLifecycleInfo>(LIFECYCLE_TOTALS_COUNT, Allocator.Persistent);
            m_CensusRecords = new NativeList<CitizenCensusRecord>(Allocator.Persistent);
            m_CensusCrossTabCounts = new NativeArray<int>(
                GetCensusCategoryCount(CensusRowDimension, CensusAgeGranularity) *
                GetCensusCategoryCount(CensusColDimension, CensusAgeGranularity),
                Allocator.Persistent);

        }

        protected override void OnDestroy()
        {
            m_Totals.Dispose();
            m_Results.Dispose();
            m_LifecycleTotals.Dispose();
            m_LifecycleDetails.Dispose();
            m_FiveYearDetails.Dispose();
            m_TenYearDetails.Dispose();
            m_CensusRecords.Dispose();
            m_CensusCrossTabCounts.Dispose();
            base.OnDestroy();
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 512;
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
                m_Results[i] = new PopulationDetailedGroupInfo(i);
            }
            for(int i = 0; i < m_FiveYearDetails.Length; i++)
            {
                m_FiveYearDetails[i] = new PopulationFiveYearGroupInfo(i * 5);
            }
            for(int i = 0; i < m_TenYearDetails.Length; i++)
            {
                m_TenYearDetails[i] = new PopulationTenYearGroupInfo(i * 10);
            }
            for (int i = 0; i < m_LifecycleTotals.Length; i++)
            {
                m_LifecycleTotals[i] = 0;
            }
            for (int i = 0; i < m_LifecycleDetails.Length; i++)
            {
                m_LifecycleDetails[i] = new PopulationLifecycleInfo(i);
            }
            m_CensusRecords.Clear();

        }
        public void SetSelectedDistrict(Entity district)
        {
            SelectedDistrict = district;
        }

        public void SetCensusDimensions(int rowDimension, int colDimension, int ageGranularity)
        {
            CensusRowDimension = (CensusDimension)rowDimension;
            CensusColDimension = (CensusDimension)colDimension;
            CensusAgeGranularity = (AgeGranularity)ageGranularity;
            ComputeCensusCrossTab();
        }

        private void ComputeCensusCrossTab()
        {
            int rowCount = GetCensusCategoryCount(CensusRowDimension, CensusAgeGranularity);
            int colCount = GetCensusCategoryCount(CensusColDimension, CensusAgeGranularity);
            int cellCount = rowCount * colCount;

            if (m_CensusCrossTabCounts.Length != cellCount)
            {
                m_CensusCrossTabCounts.Dispose();
                m_CensusCrossTabCounts = new NativeArray<int>(cellCount, Allocator.Persistent);
            }
            else
            {
                for (int i = 0; i < m_CensusCrossTabCounts.Length; i++)
                    m_CensusCrossTabCounts[i] = 0;
            }

            for (int i = 0; i < m_CensusRecords.Length; i++)
            {
                CitizenCensusRecord record = m_CensusRecords[i];
                int row = record.Get(CensusRowDimension, CensusAgeGranularity);
                int col = record.Get(CensusColDimension, CensusAgeGranularity);
                if (row < 0 || row >= rowCount || col < 0 || col >= colCount)
                    continue;
                m_CensusCrossTabCounts[row * colCount + col]++;
            }
        }
        public void UpdateDemographics()
        {
            ResetResults();
            EntityQuery citizenQuery = SystemAPI.QueryBuilder().WithAll<Citizen, HouseholdMember>().WithNone<Deleted, Temp>().Build();
            PopulationStructureJob job = new PopulationStructureJob
            {
                m_Students = SystemAPI.GetComponentLookup<Game.Citizens.Student>(true),
                m_Workers = SystemAPI.GetComponentLookup<Worker>(true),
                m_HealthProblems = SystemAPI.GetComponentLookup<HealthProblem>(true),
                m_MovingAways = SystemAPI.GetComponentLookup<MovingAway>(true),
                m_Households = SystemAPI.GetComponentLookup<Household>(true),
                m_PropertyRenters = SystemAPI.GetComponentLookup<PropertyRenter>(true),
                m_CurrentDistrictLookup = SystemAPI.GetComponentLookup<CurrentDistrict>(true),

                m_HouseholdResourceBufs = SystemAPI.GetBufferLookup<Game.Economy.Resources>(true),
                m_HappinessParameters = SystemAPI.GetSingleton<CitizenHappinessParameterData>(),
                m_PrefabRefs = SystemAPI.GetComponentLookup<PrefabRef>(true),
                m_BuildingPropertyDatas = SystemAPI.GetComponentLookup<BuildingPropertyData>(true),
                m_SpawnableBuildingDatas = SystemAPI.GetComponentLookup<SpawnableBuildingData>(true),
                m_ZoneDatas = SystemAPI.GetComponentLookup<ZoneData>(true),
                m_ZonePropertiesDatas = SystemAPI.GetComponentLookup<ZonePropertiesData>(true),

                m_Totals = m_Totals,
                m_Results = m_Results,
                m_FiveYearDetails = m_FiveYearDetails,
                m_TenYearDetails = m_TenYearDetails,
                m_LifecycleTotals = m_LifecycleTotals,
                m_LifecycleDetails = m_LifecycleDetails,
                m_CensusRecords = m_CensusRecords,
                m_CurrentDay = TimeSystem.GetDay(m_SimulationSystem.frameIndex, SystemAPI.GetSingleton<TimeData>()),
                m_SelectedDistrict = SelectedDistrict,
            };
            var jobHandle = job.Schedule(citizenQuery, Dependency);
            Dependency = jobHandle;
            jobHandle.Complete();


            m_Totals[(int)Totals.HomelessCitizens] = m_CountHouseholdDataSystem.HomelessCitizenCount;
            UpdateAgeGroupDetails();
            GetAgeRangeForCategory(CitizenAge.Child);
            GetAgeRangeForCategory(CitizenAge.Teen);
            GetAgeRangeForCategory(CitizenAge.Adult);
            GetAgeRangeForCategory(CitizenAge.Elderly);
            ComputeCensusCrossTab();
        }

		private (int minAge, int maxAge) GetAgeRangeForCategory(CitizenAge targetCategory)
	{
		int minAge = -1;
		int maxAge = -1;
		int currentDay = TimeSystem.GetDay(m_SimulationSystem.frameIndex, SystemAPI.GetSingleton<TimeData>());
		
		for (int i = 0; i < m_Results.Length; i++)
		{
			PopulationDetailedGroupInfo info = m_Results[i];
			if (info.Total == 0)
				continue;

			int ageInDays = info.Age;
			if (ageInDays < 0 || ageInDays >= DEFAULT_AGE_CAP)
				continue;

			Citizen tempCitizen = new Citizen { m_BirthDay = (short)(currentDay - ageInDays) };
			CitizenAge category = tempCitizen.GetAge();

			if (category == targetCategory)
			{
				if (minAge == -1 || ageInDays < minAge)
					minAge = ageInDays;
				if (maxAge == -1 || ageInDays > maxAge)
					maxAge = ageInDays;
			}
		}

		if (minAge == -1 || maxAge == -1)
		{
			return targetCategory switch
			{
				CitizenAge.Child => (0, 20),
				CitizenAge.Teen => (21, 35),
				CitizenAge.Adult => (36, 89),
				CitizenAge.Elderly => (90, 120),
				_ => (0, 120)
			};
		}

		return (minAge, maxAge);
	}

	private static void AddToFiveYearGroup(ref PopulationFiveYearGroupInfo group, PopulationDetailedGroupInfo info)
	{
		group.Total += info.Total;
		group.Work += info.Work;
		group.School1 += info.School1;
		group.School2 += info.School2;
		group.School3 += info.School3;
		group.School4 += info.School4;
		group.Unemployed += info.Unemployed;
		group.Retired += info.Retired;
		group.Uneducated += info.Uneducated;
		group.PoorlyEducated += info.PoorlyEducated;
		group.Educated += info.Educated;
		group.WellEducated += info.WellEducated;
		group.HighlyEducated += info.HighlyEducated;
		group.ChildOrTeenWithNoSchool += info.ChildOrTeenWithNoSchool;
	}

	private static void AddToTenYearGroup(ref PopulationTenYearGroupInfo group, PopulationDetailedGroupInfo info)
	{
		group.Total += info.Total;
		group.Work += info.Work;
		group.School1 += info.School1;
		group.School2 += info.School2;
		group.School3 += info.School3;
		group.School4 += info.School4;
		group.Unemployed += info.Unemployed;
		group.Retired += info.Retired;
		group.Uneducated += info.Uneducated;
		group.PoorlyEducated += info.PoorlyEducated;
		group.Educated += info.Educated;
		group.WellEducated += info.WellEducated;
		group.HighlyEducated += info.HighlyEducated;
		group.ChildOrTeenWithNoSchool += info.ChildOrTeenWithNoSchool;
	}

	private void UpdateAgeGroupDetails()
	{
		for (int i = 0; i < m_Results.Length; i++)
		{
			PopulationDetailedGroupInfo info = m_Results[i];
			if (info.Age < 0 || info.Age > 120)
				continue;

			int fiveIdx = info.Age / 5;
			if (fiveIdx >= 0 && fiveIdx < m_FiveYearDetails.Length)
			{
				PopulationFiveYearGroupInfo group = m_FiveYearDetails[fiveIdx];
				AddToFiveYearGroup(ref group, info);
				m_FiveYearDetails[fiveIdx] = group;
			}

			int tenIdx = info.Age / 10;
			if (tenIdx >= 0 && tenIdx < m_TenYearDetails.Length)
			{
				PopulationTenYearGroupInfo group = m_TenYearDetails[tenIdx];
				AddToTenYearGroup(ref group, info);
				m_TenYearDetails[tenIdx] = group;
			}
		}
	}
        
    }
}
