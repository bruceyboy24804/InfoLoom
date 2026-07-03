using Colossal.UI.Binding;
using Unity.Entities;

namespace InfoLoomTwo.Systems.IndustrialSystems.IndustrialCompanyData
{
    public partial class IndustrialCompanySystem
    {
        public struct EfficiencyFactorInfo : IJsonWritable
        {
            public Game.Buildings.EfficiencyFactor Factor;
            public int Value;
            public int Result;

            public EfficiencyFactorInfo(Game.Buildings.EfficiencyFactor factor, int value, int result)
            {
                Factor = factor;
                Value = value;
                Result = result;
            }

            public void Write(IJsonWriter writer)
            {
                writer.TypeBegin(typeof(EfficiencyFactorInfo).FullName);
                writer.PropertyName("Factor"); writer.Write((int)Factor);
                writer.PropertyName("Value"); writer.Write(Value);
                writer.PropertyName("Result"); writer.Write(Result);
                writer.TypeEnd();
            }
        }
        
        public struct ResourceInfo : IJsonWritable
        {
            public string ResourceName;
            public int Amount;
            public string Icon;

            public ResourceInfo(string resourceName, int amount, string icon)
            {
                ResourceName = resourceName;
                Amount = amount;
                Icon = icon;
            }

            public void Write(IJsonWriter writer)
            {
                writer.TypeBegin(typeof(ResourceInfo).FullName);
                writer.PropertyName("ResourceName"); writer.Write(ResourceName ?? string.Empty);
                writer.PropertyName("Amount"); writer.Write(Amount);
                writer.PropertyName("Icon"); writer.Write(Icon ?? string.Empty);
                writer.TypeEnd();
            }
        }
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
            
            public bool HasStatistics;
        }
    }
}