using Game;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace InfoLoomTwo.Systems.ResidentialData
{
    public partial class ResidentialSystem : GameSystemBase
    {
        private ResidentialDemandSystem m_ResidentialDemandSystem;
        private CountResidentialPropertySystem m_CountResidentialPropertySystem;
        private CountHouseholdDataSystem m_CountHouseholdDataSystem;
        private CountStudyPositionsSystem m_CountStudyPositionsSystem;
        private TaxSystem m_TaxSystem;
        private CitySystem m_CitySystem;
        
        public bool IsPanelVisible { get; set; }
        public NativeArray<float> m_Results;

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
            
            m_Results = new NativeArray<float>(21, Allocator.Persistent);
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
        public enum ResultIndex
        {
            LowTotal = 0,
            MediumTotal = 1,
            HighTotal = 2,
            LowOccupied = 3,
            MediumOccupied = 4,
            HighOccupied = 5,
            // Index 6 unused
            AverageHappiness = 7,
            NeutralHappiness = 8,
            UnemploymentRate = 9,
            NeutralUnemployment = 10,
            HomelessHouseholds = 11,
            MovedInHouseholds = 12,
            NeutralHomelessness = 13,
            TotalStudyPositions = 14,
            WeightedTaxRate = 15,
            HouseholdDemand = 16,
            StudentRatio = 17,
            FreeRequirementLow = 18,
            FreeRequirementMedium = 19,
            FreeRequirementHigh = 20
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
            
            // Basic property counts
            m_Results[(int)ResultIndex.LowTotal] = RTI(total.x);
            m_Results[(int)ResultIndex.MediumTotal] = RTI(total.y);
            m_Results[(int)ResultIndex.HighTotal] = RTI(total.z);
            m_Results[(int)ResultIndex.LowOccupied] = RTI(occupied.x);
            m_Results[(int)ResultIndex.MediumOccupied] = RTI(occupied.y);
            m_Results[(int)ResultIndex.HighOccupied] = RTI(occupied.z);
            
            // Demand parameters
            m_Results[(int)ResultIndex.NeutralHappiness] = RTI(demandParams.m_NeutralHappiness);
            m_Results[(int)ResultIndex.NeutralUnemployment] = RTI(10f * demandParams.m_NeutralUnemployment);
            m_Results[(int)ResultIndex.NeutralHomelessness] = RTI(10f * demandParams.m_NeutralHomelessness);
            
            // Population data
            m_Results[(int)ResultIndex.AverageHappiness] = RTI(population.m_AverageHappiness);
            m_Results[(int)ResultIndex.UnemploymentRate] = Mathf.Round(m_CountHouseholdDataSystem.UnemploymentRate * 10f) / 10f;
            m_Results[(int)ResultIndex.HomelessHouseholds] = RTI(householdData.m_HomelessHouseholdCount);
            m_Results[(int)ResultIndex.MovedInHouseholds] = RTI(householdData.m_MovedInHouseholdCount);
            
            // Study positions and tax data
            var totalStudy = studyPositions.Length > 4 ? 
                studyPositions[1] + studyPositions[2] + studyPositions[3] + studyPositions[4] : 0;
            m_Results[(int)ResultIndex.TotalStudyPositions] = RTI(totalStudy);
            m_Results[(int)ResultIndex.WeightedTaxRate] = CalculateWeightedTaxRate();
            
            // Demand data
            m_Results[(int)ResultIndex.HouseholdDemand] = RTI(m_ResidentialDemandSystem.householdDemand);
            m_Results[(int)ResultIndex.StudentRatio] = CalculateStudentRatio();
            
            // Free property requirements
            m_Results[(int)ResultIndex.FreeRequirementLow] = RTI(10 * demandParams.m_FreeResidentialRequirement.x);
            m_Results[(int)ResultIndex.FreeRequirementMedium] = RTI(10 * demandParams.m_FreeResidentialRequirement.y);
            m_Results[(int)ResultIndex.FreeRequirementHigh] = RTI(10 * demandParams.m_FreeResidentialRequirement.z);
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
        public int RTI(float value)
        {
            return Mathf.RoundToInt(value);
        }
        
    }
}