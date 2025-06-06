using InfoLoomTwo.Domain.DataDomain.Enums;
using InfoLoomTwo.Domain.DataDomain.Enums.CompanyPanelEnums;
using System;

namespace InfoLoomTwo.Utils
{
    public static class CommercialCompanySortingUtility
    {
        /// <summary>
        /// Creates a combined comparer function based on the current mod settings
        /// </summary>
        public static Comparison<T> CreateComparer<T>(
            Func<T, T, int> indexComparer,
            Func<T, T, int> nameComparer,
            Func<T, T, int> serviceUsageComparer,
            Func<T, T, int> employeesComparer,
            Func<T, T, int> efficiencyComparer,
            Func<T, T, int> profitabilityComparer) where T : struct
        {
            Comparison<T> comparer = (a, b) => 0;
            bool isDefaultComparer = true;

            // Apply index sorting if enabled
            if (Mod.setting.CommercialIndexSorting != IndexSortingEnum.Off)
            {
                comparer = Mod.setting.CommercialIndexSorting == IndexSortingEnum.Ascending
                    ? new Comparison<T>((a, b) => indexComparer(a, b))
                    : new Comparison<T>((a, b) => indexComparer(b, a)); // Reverse for descending
                isDefaultComparer = false;
            }

            // Apply name sorting if enabled
            if (Mod.setting.CommercialNameSorting != CompanyNameEnum.Off)
            {
                Comparison<T> nameComp = Mod.setting.CommercialNameSorting == CompanyNameEnum.Ascending
                    ? new Comparison<T>((a, b) => nameComparer(a, b))
                    : new Comparison<T>((a, b) => nameComparer(b, a)); // Reverse for descending

                comparer = CombineComparers(comparer, nameComp, isDefaultComparer);
                isDefaultComparer = false;
            }

            // Apply service usage sorting if enabled
            if (Mod.setting.CommercialServiceUsageSorting != ServiceUsageEnum.Off)
            {
                Comparison<T> serviceComp = Mod.setting.CommercialServiceUsageSorting == ServiceUsageEnum.Ascending
                    ? new Comparison<T>((a, b) => serviceUsageComparer(a, b))
                    : new Comparison<T>((a, b) => serviceUsageComparer(b, a)); // Reverse for descending

                comparer = CombineComparers(comparer, serviceComp, isDefaultComparer);
                isDefaultComparer = false;
            }

            // Apply employees sorting if enabled
            if (Mod.setting.CommercialEmployeesSorting != EmployeesEnum.Off)
            {
                Comparison<T> employeesComp = Mod.setting.CommercialEmployeesSorting == EmployeesEnum.Ascending
                    ? new Comparison<T>((a, b) => employeesComparer(a, b))
                    : new Comparison<T>((a, b) => employeesComparer(b, a)); // Reverse for descending

                comparer = CombineComparers(comparer, employeesComp, isDefaultComparer);
                isDefaultComparer = false;
            }

            // Apply efficiency sorting if enabled
            if (Mod.setting.CommercialEfficiencySorting != EfficiancyEnum.Off)
            {
                Comparison<T> efficiencyComp = Mod.setting.CommercialEfficiencySorting == EfficiancyEnum.Ascending
                    ? new Comparison<T>((a, b) => efficiencyComparer(a, b))
                    : new Comparison<T>((a, b) => efficiencyComparer(b, a)); // Reverse for descending

                comparer = CombineComparers(comparer, efficiencyComp, isDefaultComparer);
                isDefaultComparer = false;
            }

            // Apply profitability sorting if enabled
            if (Mod.setting.CommercialProfitabilitySorting != ProfitabilityEnum.Off)
            {
                Comparison<T> profitabilityComp = Mod.setting.CommercialProfitabilitySorting == ProfitabilityEnum.Ascending
                    ? new Comparison<T>((a, b) => profitabilityComparer(a, b))
                    : new Comparison<T>((a, b) => profitabilityComparer(b, a)); // Reverse for descending

                comparer = CombineComparers(comparer, profitabilityComp, isDefaultComparer);
            }

            return comparer;
        }

        /// <summary>
        /// Combines two comparers, using the second one as a tiebreaker for the first
        /// </summary>
        private static Comparison<T> CombineComparers<T>(
            Comparison<T> primaryComparer,
            Comparison<T> secondaryComparer,
            bool isDefaultPrimary)
        {
            if (isDefaultPrimary)
                return secondaryComparer;

            return (a, b) => {
                var result = primaryComparer(a, b);
                return result != 0 ? result : secondaryComparer(a, b);
            };
        }
    }
}