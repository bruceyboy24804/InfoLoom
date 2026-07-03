using Colossal.UI.Binding;
using Game.Economy;
using Unity.Entities;

namespace InfoLoomTwo.Systems.CommercialSystems.CommercialCompanyDebugData
{
    public partial class CommercialCompanyDataSystem
    {
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

        public struct ProcessResourceInfo : IJsonWritable
        {
            public string ResourceName;
            public int Amount;
            public string ResourceIcon;
            public bool IsOutput;

            public void Write(IJsonWriter writer)
            {
                writer.TypeBegin(typeof(ProcessResourceInfo).FullName);
                writer.PropertyName("resourceName"); writer.Write(ResourceName);
                writer.PropertyName("amount"); writer.Write(Amount);
                writer.PropertyName("resourceIcon"); writer.Write(ResourceIcon);
                writer.PropertyName("isOutput"); writer.Write(IsOutput);
                writer.TypeEnd();
            }
        }

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

        /// <summary>
        /// Serializable per-company record sent to the UI. Replaces the old reflection-serialized
        /// CommercialCompanyDTO with an explicit <see cref="IJsonWritable"/> payload so the wire shape
        /// is owned in one place. Field names match the React CommercialCompanyDebug interface.
        /// </summary>
        public sealed class CommercialCompanyRecord : IJsonWritable
        {
            public Entity EntityId;
            public string CompanyName;
            public int ServiceAvailable;
            public int MaxService;
            public int TotalEmployees;
            public int MaxWorkers;
            public int VehicleCount;
            public int VehicleCapacity;
            public string ResourceName;
            public string ResourceIcon;
            public int ResourceAmount;
            public int TotalEfficiency;
            public EfficiencyFactorInfo[] Factors;
            public ProcessResourceInfo[] ProcessResources;
            public float Profitability;
            public int LastTotalWorth;
            public int TotalWages;
            public int ProductionPerDay;
            public float EfficiencyValue;
            public float Concentration;
            public string OutputResourceName;
            public int MoneyAmount;
            public ResourceInfo[] Input1Resources;
            public ResourceInfo[] OutputResources;
            public ResourceInfo[] MaintenanceResources;
            // Raw process resources (not serialized) — used to build the filter dropdowns.
            public Resource Input1Resource;
            public Resource OutputResource;
            public int Income;
            public int Worth;
            public int Profit;
            public int WagePaid;
            public int RentPaid;
            public int ElectricityPaid;
            public int WaterPaid;
            public int SewagePaid;
            public int GarbagePaid;
            public int TaxPaid;
            public int ResourcesBoughtPaid;
            public int CurrentCustomers;
            public int MonthlyCustomers;

            public void Write(IJsonWriter writer)
            {
                writer.TypeBegin(typeof(CommercialCompanyRecord).FullName);
                writer.PropertyName("EntityId"); writer.Write(EntityId);
                writer.PropertyName("CompanyName"); writer.Write(CompanyName ?? string.Empty);
                writer.PropertyName("ServiceAvailable"); writer.Write(ServiceAvailable);
                writer.PropertyName("MaxService"); writer.Write(MaxService);
                writer.PropertyName("TotalEmployees"); writer.Write(TotalEmployees);
                writer.PropertyName("MaxWorkers"); writer.Write(MaxWorkers);
                writer.PropertyName("VehicleCount"); writer.Write(VehicleCount);
                writer.PropertyName("VehicleCapacity"); writer.Write(VehicleCapacity);
                writer.PropertyName("ResourceName"); writer.Write(ResourceName ?? "None");
                writer.PropertyName("ResourceIcon"); writer.Write(ResourceIcon ?? string.Empty);
                writer.PropertyName("ResourceAmount"); writer.Write(ResourceAmount);
                writer.PropertyName("TotalEfficiency"); writer.Write(TotalEfficiency);
                WriteItems(writer, "Factors", Factors);
                WriteItems(writer, "ProcessResources", ProcessResources);
                writer.PropertyName("Profitability"); writer.Write(Profitability);
                writer.PropertyName("LastTotalWorth"); writer.Write(LastTotalWorth);
                writer.PropertyName("TotalWages"); writer.Write(TotalWages);
                writer.PropertyName("ProductionPerDay"); writer.Write(ProductionPerDay);
                writer.PropertyName("EfficiencyValue"); writer.Write(EfficiencyValue);
                writer.PropertyName("Concentration"); writer.Write(Concentration);
                writer.PropertyName("OutputResourceName"); writer.Write(OutputResourceName ?? string.Empty);
                writer.PropertyName("MoneyAmount"); writer.Write(MoneyAmount);
                WriteItems(writer, "Input1Resources", Input1Resources);
                WriteItems(writer, "OutputResources", OutputResources);
                WriteItems(writer, "MaintenanceResources", MaintenanceResources);
                writer.PropertyName("Income"); writer.Write(Income);
                writer.PropertyName("Worth"); writer.Write(Worth);
                writer.PropertyName("Profit"); writer.Write(Profit);
                writer.PropertyName("WagePaid"); writer.Write(WagePaid);
                writer.PropertyName("RentPaid"); writer.Write(RentPaid);
                writer.PropertyName("ElectricityPaid"); writer.Write(ElectricityPaid);
                writer.PropertyName("WaterPaid"); writer.Write(WaterPaid);
                writer.PropertyName("SewagePaid"); writer.Write(SewagePaid);
                writer.PropertyName("GarbagePaid"); writer.Write(GarbagePaid);
                writer.PropertyName("TaxPaid"); writer.Write(TaxPaid);
                writer.PropertyName("ResourcesBoughtPaid"); writer.Write(ResourcesBoughtPaid);
                writer.PropertyName("CurrentCustomers"); writer.Write(CurrentCustomers);
                writer.PropertyName("MonthlyCustomers"); writer.Write(MonthlyCustomers);
                writer.TypeEnd();
            }

            private static void WriteItems<TItem>(IJsonWriter writer, string name, TItem[] items)
                where TItem : IJsonWritable
            {
                writer.PropertyName(name);
                int length = items?.Length ?? 0;
                writer.ArrayBegin(length);
                for (int i = 0; i < length; i++)
                    items[i].Write(writer);
                writer.ArrayEnd();
            }
        }

        // Burst-compatible struct for commercial company data
        public struct CommercialCompanyJobData : IComponentData
        {
            public Entity EntityId;
            public Entity Brand;
            public int ServiceAvailable;
            public int MaxService;
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
            public int ResourceCount;
            public bool HasStatistics;
        }
    }
}