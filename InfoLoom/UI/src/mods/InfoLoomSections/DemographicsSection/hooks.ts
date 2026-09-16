import { useMemo } from 'react';
import { ChartData, LegendLabels } from './types';
import {
  AgeGranularity,
  CensusDimension,
  CensusLabels,
  CENSUS_PALETTE,
  formatAgeBucketLabel,
  reshapeCrossTab,
} from './census';
import { Localekeys } from 'mods/locale';

/**
 * Builds Chart.js-ready data from the free-form census cross-tab. The row dimension becomes
 * the chart's categories (bars); the column dimension becomes the stacked series within each
 * bar. This is the only chart the Demographics panel renders — every "view" (what used to be
 * separate Employment/Education/Wealth/Residency tabs) is just a particular choice of
 * row/column dimension now.
 */
export function useChartData(
  censusCounts: number[],
  censusRowDim: CensusDimension,
  censusColDim: CensusDimension,
  censusAgeGranularity: AgeGranularity,
  censusLabels: CensusLabels
): ChartData {
  return useMemo((): ChartData => {
    try {
      if (!censusCounts?.length) {
        return { labels: [], datasets: [] };
      }
      const grid = reshapeCrossTab(censusCounts, censusRowDim, censusColDim, censusAgeGranularity);
      const rowNames =
        censusRowDim === CensusDimension.Age
          ? censusLabels.categoryNames[CensusDimension.Age](censusAgeGranularity)
          : censusLabels.categoryNames[censusRowDim];
      const colNames =
        censusColDim === CensusDimension.Age
          ? censusLabels.categoryNames[CensusDimension.Age](censusAgeGranularity)
          : censusLabels.categoryNames[censusColDim];
      return {
        labels: rowNames,
        datasets: colNames.map((name, c) => ({
          label: name,
          data: grid.map(row => row[c] || 0),
          backgroundColor: CENSUS_PALETTE[c % CENSUS_PALETTE.length],
        })),
      };
    } catch (error) {
      console.error('Error transforming census chart data:', error);
      return { labels: [], datasets: [] };
    }
  }, [censusCounts, censusRowDim, censusColDim, censusAgeGranularity, censusLabels]);
}

/**
 * Custom hook to create translated legend labels
 */
export function useLegendLabels(translate: (key: string, fallback: string) => string | null): LegendLabels {
  return useMemo(
    (): LegendLabels => ({
      work: translate(Localekeys.Worker, 'Work') || 'Work',
      elementary: translate(Localekeys.Elementary, 'Elementary') || 'Elementary',
      highSchool: translate(Localekeys.HighSchool, 'High School') || 'High School',
      college: translate(Localekeys.College, 'College') || 'College',
      university: translate(Localekeys.University, 'University') || 'University',
      retired: translate(Localekeys.Retired, 'Retired') || 'Retired',
      unemployed: translate(Localekeys.Unemployed, 'Unemployed') || 'Unemployed',
      uneducated: translate(Localekeys.Uneducated, 'Uneducated') || 'Uneducated',
      poorlyEducated: translate(Localekeys.PoorlyEducated, 'Poorly Educated') || 'Poorly Educated',
      educated: translate(Localekeys.Educated, 'Educated') || 'Educated',
      wellEducated: translate(Localekeys.WellEducated, 'Well Educated') || 'Well Educated',
      highlyEducated: translate(Localekeys.HighlyEducated, 'Highly Educated') || 'Highly Educated',
      childOrTeenWithNoSchool:
        translate('InfoLoomTwo.DemographicsPanel[LegendItem13]', 'Child/Teen with No School') ||
        'Child/Teen with No School',
      wretched: translate(Localekeys.LegendItemWretched, 'Wretched') || 'Wretched',
      poor: translate(Localekeys.LegendItemPoor, 'Poor') || 'Poor',
      modest: translate(Localekeys.LegendItemModest, 'Modest') || 'Modest',
      comfortable: translate(Localekeys.LegendItemComfortable, 'Comfortable') || 'Comfortable',
      wealthy: translate(Localekeys.LegendItemWealthy, 'Wealthy') || 'Wealthy',
      lowDensity: translate(Localekeys.LegendItemLowDensity, 'Low Density') || 'Low Density',
      mediumDensity: translate(Localekeys.LegendItemMediumDensity, 'Medium Density') || 'Medium Density',
      highDensity: translate(Localekeys.LegendItemHighDensity, 'High Density') || 'High Density',
      mixedUse: translate(Localekeys.LegendItemMixedUse, 'Mixed Use') || 'Mixed Use',
      unhoused: translate(Localekeys.LegendItemUnhoused, 'Unhoused') || 'Unhoused',
    }),
    [translate]
  );
}

/**
 * Custom hook to create translated lifecycle labels
 */
export function useLifecycleLabels(translate: (key: string, fallback: string) => string | null): string[] {
  return useMemo(
    () => [
      translate(Localekeys.Child, 'Child') || 'Child',
      translate(Localekeys.Teen, 'Teen') || 'Teen',
      translate(Localekeys.Adult, 'Adult') || 'Adult',
      translate(Localekeys.Elder, 'Elder') || 'Elder',
    ],
    [translate]
  );
}

/**
 * Custom hook to create translated age-granularity labels (for the Age-axis resolution picker).
 */
export function useAgeGranularityLabels(
  translate: (key: string, fallback: string) => string | null
): Record<AgeGranularity, string> {
  return useMemo(
    () => ({
      [AgeGranularity.Lifecycle]: translate(Localekeys.LifeCycleGroups, 'Lifecycle Groups') || 'Lifecycle Groups',
      [AgeGranularity.TenYear]: translate(Localekeys.TenYearGroups, '10-Year Groups') || '10-Year Groups',
      [AgeGranularity.FiveYear]: translate(Localekeys.FiveYearGroups, '5-Year Groups') || '5-Year Groups',
      [AgeGranularity.Detailed]: translate(Localekeys.DetailedView, 'Detailed View') || 'Detailed View',
    }),
    [translate]
  );
}

/**
 * Custom hook building the dimension names and per-category labels used by the free-form
 * census cross-tab. Reuses the same translated strings as the legend. Category order within
 * each dimension must stay in sync with Demographics.cs's CensusDimension classification.
 */
export function useCensusLabels(
  translate: (key: string, fallback: string) => string | null,
  legendLabels: LegendLabels,
  lifecycleLabels: string[]
): CensusLabels {
  return useMemo(
    (): CensusLabels => ({
      dimensionNames: {
        [CensusDimension.Age]: translate(Localekeys.CensusDimensionAge, 'Age') || 'Age',
        [CensusDimension.Education]: translate(Localekeys.CensusDimensionEducation, 'Education') || 'Education',
        [CensusDimension.Wealth]: translate(Localekeys.CensusDimensionWealth, 'Wealth') || 'Wealth',
        [CensusDimension.Residency]: translate(Localekeys.CensusDimensionResidency, 'Residency') || 'Residency',
        [CensusDimension.Activity]: translate(Localekeys.CensusDimensionActivity, 'Activity') || 'Activity',
      },
      categoryNames: {
        [CensusDimension.Age]: (ageGranularity: AgeGranularity) =>
          ageGranularity === AgeGranularity.Lifecycle
            ? lifecycleLabels
            : Array.from({ length: bucketCountFor(ageGranularity) }, (_, i) => formatAgeBucketLabel(i, ageGranularity)),
        [CensusDimension.Education]: [
          legendLabels.uneducated,
          legendLabels.poorlyEducated,
          legendLabels.educated,
          legendLabels.wellEducated,
          legendLabels.highlyEducated,
        ],
        [CensusDimension.Wealth]: [
          legendLabels.wretched,
          legendLabels.poor,
          legendLabels.modest,
          legendLabels.comfortable,
          legendLabels.wealthy,
        ],
        [CensusDimension.Residency]: [
          legendLabels.lowDensity,
          legendLabels.mediumDensity,
          legendLabels.highDensity,
          legendLabels.mixedUse,
          legendLabels.unhoused,
        ],
        [CensusDimension.Activity]: [
          legendLabels.work,
          legendLabels.elementary,
          legendLabels.highSchool,
          legendLabels.college,
          legendLabels.university,
          legendLabels.retired,
          legendLabels.unemployed,
          legendLabels.childOrTeenWithNoSchool,
        ],
      },
    }),
    [translate, legendLabels, lifecycleLabels]
  );
}

function bucketCountFor(ageGranularity: AgeGranularity): number {
  switch (ageGranularity) {
    case AgeGranularity.Detailed:
      return 120;
    case AgeGranularity.FiveYear:
      return 24;
    case AgeGranularity.TenYear:
      return 12;
    default:
      return 4;
  }
}
