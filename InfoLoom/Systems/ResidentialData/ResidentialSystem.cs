using System;
using System.Runtime.CompilerServices;
using Game;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace InfoLoomTwo.Systems.ResidentialData
{
    public partial class ResidentialSystem : GameSystemBase
    {
        // All the heavy lifting systems
        private ResidentialDemandSystem m_ResidentialDemandSystem;
        private CountResidentialPropertySystem m_CountResidentialPropertySystem;
        private CountHouseholdDataSystem m_CountHouseholdDataSystem;
        private CountStudyPositionsSystem m_CountStudyPositionsSystem;
        private TaxSystem m_TaxSystem;
        private CitySystem m_CitySystem;
        
        public bool IsPanelVisible { get; set; }
        public NativeArray<int> m_Results;

        // Direct property accessors using existing systems
        
        public int3 TotalProperties => m_CountResidentialPropertySystem.TotalProperties;
        
        
        public int3 FreeProperties => m_CountResidentialPropertySystem.FreeProperties;
        
        
        public int3 OccupiedProperties => TotalProperties - FreeProperties;
        
        
        public int HouseholdDemand => m_ResidentialDemandSystem.householdDemand;
        
        
        public int3 BuildingDemand => m_ResidentialDemandSystem.buildingDemand;

        protected override void OnCreate()
        {
            base.OnCreate();
            
            // Initialize all systems
            m_ResidentialDemandSystem = World.GetOrCreateSystemManaged<ResidentialDemandSystem>();
            m_CountResidentialPropertySystem = World.GetOrCreateSystemManaged<CountResidentialPropertySystem>();
            m_CountHouseholdDataSystem = World.GetOrCreateSystemManaged<CountHouseholdDataSystem>();
            m_CountStudyPositionsSystem = World.GetOrCreateSystemManaged<CountStudyPositionsSystem>();
            m_TaxSystem = World.GetOrCreateSystemManaged<TaxSystem>();
            m_CitySystem = World.GetOrCreateSystemManaged<CitySystem>();
            
            m_Results = new NativeArray<int>(21, Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            if (m_Results.IsCreated) m_Results.Dispose();
            base.OnDestroy();
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 512;

        protected override void OnUpdate()
        {
            if (!IsPanelVisible) return;
            
            PopulateResultsFromSystems();
        }

        private void PopulateResultsFromSystems()
        {
            // Get all data from existing systems in one go
            var residentialData = m_CountResidentialPropertySystem.GetResidentialPropertyData();
            var householdData = m_CountHouseholdDataSystem.GetHouseholdCountData();
            var studyPositions = m_CountStudyPositionsSystem.GetStudyPositionsByEducation(out var deps);
            deps.Complete();
            
            var city = m_CitySystem.City;
            var population = EntityManager.GetComponentData<Game.City.Population>(city);
            var demandParams = SystemAPI.GetSingleton<Game.Prefabs.DemandParameterData>();
            
            // Ultra-fast direct assignments
            PopulateBasicData(residentialData, householdData, population, demandParams, studyPositions);
        }

        private void PopulateBasicData(
            CountResidentialPropertySystem.ResidentialPropertyData residentialData,
            CountHouseholdDataSystem.HouseholdData householdData,
            Game.City.Population population,
            Game.Prefabs.DemandParameterData demandParams,
            NativeArray<int> studyPositions)
        {
            var total = residentialData.m_TotalProperties;
            var occupied = total - residentialData.m_FreeProperties;
            
            // Basic property counts (0-5)
            m_Results[0] = total.x;    // Low total
            m_Results[1] = total.y;    // Medium total  
            m_Results[2] = total.z;    // High total
            m_Results[3] = occupied.x; // Low occupied
            m_Results[4] = occupied.y; // Medium occupied
            m_Results[5] = occupied.z; // High occupied
            
            // Demand parameters (6, 8, 10, 13, 15)
            m_Results[8] = demandParams.m_NeutralHappiness;
            m_Results[10] = (int)(10f * demandParams.m_NeutralUnemployment);
            m_Results[13] = (int)(10f * demandParams.m_NeutralHomelessness);
            
            // Population data (7, 9, 11-12, 14-17)
            m_Results[7] = population.m_AverageHappiness;
            m_Results[9] = (int)m_CountHouseholdDataSystem.UnemploymentRate;
            m_Results[11] = householdData.m_HomelessHouseholdCount;
            m_Results[12] = householdData.m_MovedInHouseholdCount;
            
            // Study positions and tax data
            var totalStudy = studyPositions.Length > 4 ? 
                studyPositions[1] + studyPositions[2] + studyPositions[3] + studyPositions[4] : 0;
            m_Results[14] = totalStudy;
            m_Results[15] = CalculateWeightedTaxRate();
            
            // Demand data
            m_Results[16] = m_ResidentialDemandSystem.householdDemand;
            m_Results[17] = CalculateStudentRatio();
            
            m_Results[18] = 10  * demandParams.m_FreeResidentialRequirement.x;
            m_Results[19] = 10  * demandParams.m_FreeResidentialRequirement.y;
            m_Results[20] = 10  * demandParams.m_FreeResidentialRequirement.z;
        }

        private int CalculateWeightedTaxRate()
        {
            var taxRates = m_TaxSystem.GetTaxRates();
            var weightedRate = 0f;
            
            for (int i = 0; i <= 2; i++)
            {
                weightedRate -= 3f * (i + 1f) * (TaxSystem.GetResidentialTaxRate(i, taxRates) - 10f);
            }
            
            return (int)(100f - weightedRate / 1.8f); // Simplified calculation
        }

        private int CalculateStudentRatio()
        {
            var lowFactors = m_ResidentialDemandSystem.GetLowDensityDemandFactors(out var deps1);
            var mediumFactors = m_ResidentialDemandSystem.GetMediumDensityDemandFactors(out var deps2);
            var highFactors = m_ResidentialDemandSystem.GetHighDensityDemandFactors(out var deps3);
            
            JobHandle.CompleteAll(ref deps1, ref deps2, ref deps3);
            
            var unemployment = math.max(0, lowFactors[6] + mediumFactors[6] + highFactors[6]);
            var students = math.max(0, lowFactors[12] + mediumFactors[12] + highFactors[12]);
            
            return students == 0 ? 0 : (int)(100f * students / (students + unemployment));
        }

        // Ultra-simple accessor methods
        public int GetResult(int index) => m_Results[index];
        
        public NativeArray<int> GetAllResults() => m_Results;
    }
}