using System;
using InfoLoomTwo.Domain.DataDomain.Enums.TradeCostEnums;

namespace InfoLoomTwo.Utils
{
    public class TradeCostSortingUtility
    {
        public static Comparison<T> CreateComparer<T>(
            Func<T, T, int> resourceNameComparer,
            Func<T, T, int> buyCostComparer,
            Func<T, T, int> sellCostComparer,
            Func<T, T, int> profitComparer,
            Func<T, T, int> profitMarginComparer,
            Func<T, T, int> importAmountComparer,
            Func<T, T, int> exportAmountComparer)
        {
            Comparison<T> comparer = (a, b) => 0;
            bool isDefaultComparer = true;

            // Apply index sorting if enabled
            if (Mod.setting.ResourceName != ResourceNameEnum.Off)
            {
                comparer = Mod.setting.ResourceName == ResourceNameEnum.Ascending
                    ? new Comparison<T>((a, b) => resourceNameComparer(a, b))
                    : new Comparison<T>((a, b) => resourceNameComparer(b, a)); // Reverse for descending
                isDefaultComparer = false;
            }

            // Apply name sorting if enabled
            if (Mod.setting.BuyCost != BuyCostEnum.Off)
            {
                Comparison<T> nameComp = Mod.setting.BuyCost == BuyCostEnum.Ascending
                    ? new Comparison<T>((a, b) => buyCostComparer(a, b))
                    : new Comparison<T>((a, b) => buyCostComparer(b, a)); // Reverse for descending

                comparer = CombineComparers(comparer, nameComp, isDefaultComparer);
                isDefaultComparer = false;
            }
            // Apply employees sorting if enabled
            if (Mod.setting.SellCost != SellCostEnum.Off)
            {
                Comparison<T> employeesComp = Mod.setting.SellCost == SellCostEnum.Ascending
                    ? new Comparison<T>((a, b) => sellCostComparer(a, b))
                    : new Comparison<T>((a, b) => sellCostComparer(b, a)); // Reverse for descending

                comparer = CombineComparers(comparer, employeesComp, isDefaultComparer);
                isDefaultComparer = false;
            }

            // Apply efficiency sorting if enabled
            if (Mod.setting.Profit != ProfitEnum.Off)
            {
                Comparison<T> efficiencyComp = Mod.setting.Profit == ProfitEnum.Ascending
                    ? new Comparison<T>((a, b) => profitComparer(a, b))
                    : new Comparison<T>((a, b) => profitComparer(b, a)); // Reverse for descending

                comparer = CombineComparers(comparer, efficiencyComp, isDefaultComparer);
                isDefaultComparer = false;
            }

            // Apply profitability sorting if enabled
            if (Mod.setting.ProfitMargin != ProfitMarginEnum.Off)
            {
                Comparison<T> profitabilityComp = Mod.setting.ProfitMargin == ProfitMarginEnum.Ascending
                    ? new Comparison<T>((a, b) => profitMarginComparer(a, b))
                    : new Comparison<T>((a, b) => profitMarginComparer(b, a)); // Reverse for descending

                comparer = CombineComparers(comparer, profitabilityComp, isDefaultComparer);
            }
            if (Mod.setting.ImportAmount != ImportAmountEnum.Off)
            {
                Comparison<T> importAmountComp = Mod.setting.ImportAmount == ImportAmountEnum.Ascending
                    ? new Comparison<T>((a, b) => importAmountComparer(a, b))
                    : new Comparison<T>((a, b) => importAmountComparer(b, a)); // Reverse for descending

                comparer = CombineComparers(comparer, importAmountComp, isDefaultComparer);
                isDefaultComparer = false;
            }
            if (Mod.setting.ExportAmount != ExportAmountEnum.Off)
            {
                Comparison<T> exportAmountComp = Mod.setting.ExportAmount == ExportAmountEnum.Ascending
                    ? new Comparison<T>((a, b) => exportAmountComparer(a, b))
                    : new Comparison<T>((a, b) => exportAmountComparer(b, a)); // Reverse for descending

                comparer = CombineComparers(comparer, exportAmountComp, isDefaultComparer);
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