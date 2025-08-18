using Unity.Collections;
using Unity.Entities;

namespace InfoLoomTwo.Systems.IndustrialSystems.IndustrialCompanyData.IndustrialCompanyDomain
{
    public struct IndustrialCompanyJobData
    {
        public Entity EntityId;
        public Entity Brand;
        public int TotalEmployees;
        public int MaxWorkers;
        public int VehicleCount;
        public int VehicleCapacity;
        public int TotalEfficiency;
        public float Profitability;
        public int LastTotalWorth;
        public int TotalWages;
        public int ProductionPerDay;
        public float EfficiencyValue;
        public bool IsExtractor;
        public int ResourceCount;
    }
}