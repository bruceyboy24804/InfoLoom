using InfoLoomTwo.Domain.DataDomain;
using Unity.Entities;

namespace InfoLoomTwo.Systems.IndustrialSystems.IndustrialCompanyData.IndustrialCompanyDomain
{
    public struct IndustrialCompanyDTO
    {
        public Entity EntityId;
        public string CompanyName;
        public int TotalEmployees;
        public int MaxWorkers;
        public int VehicleCount;
        public int VehicleCapacity;
        public int ResourceAmount;
        public ProcessResourceInfo[] ProcessResources;
        public int TotalEfficiency;
        public EfficiencyFactorInfo[] Factors;
        public float Profitability;
        public int LastTotalWorth;
        public int TotalWages;
        public int ProductionPerDay;
        public float EfficiencyValue;
        public string OutputResourceName;
        public bool IsExtractor;
        public string ResourceIcon; // For backward compatibility
        public string ResourceName; // For backward compatibility
        public Game.Economy.ResourceInfo[] Resources; // New field to hold all resources
        
        public int MoneyAmount;
        public ResourceInfo[] Input1Resources;
        public ResourceInfo[] Input2Resources;
        public ResourceInfo[] OutputResources;
        public ResourceInfo[] MaintenanceResources;
    }
}