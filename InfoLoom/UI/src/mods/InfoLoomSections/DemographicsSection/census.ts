// Mirrors InfoLoomTwo.Systems.DemographicsData.Demographics.CensusDimension (C#).
// Keep indices and category counts in sync with that enum.
export enum CensusDimension {
  Age = 0,
  Education = 1,
  Wealth = 2,
  Residency = 3,
  Activity = 4,
}

export const CENSUS_DIMENSIONS: CensusDimension[] = [
  CensusDimension.Age,
  CensusDimension.Education,
  CensusDimension.Wealth,
  CensusDimension.Residency,
  CensusDimension.Activity,
];

// Every dimension has a fixed category count — except Age, which can be viewed at four
// different resolutions (see AgeGranularity below) since CitizenCensusRecord keeps each
// citizen's exact age, not just their lifecycle bucket.
export const CENSUS_CATEGORY_COUNTS: Record<Exclude<CensusDimension, CensusDimension.Age>, number> = {
  [CensusDimension.Education]: 5,
  [CensusDimension.Wealth]: 5,
  [CensusDimension.Residency]: 5,
  [CensusDimension.Activity]: 8,
};

// Mirrors InfoLoomTwo.Systems.DemographicsData.Demographics.AgeGranularity (C#).
export enum AgeGranularity {
  Detailed = 0,
  FiveYear = 1,
  TenYear = 2,
  Lifecycle = 3,
}

export const AGE_GRANULARITIES: AgeGranularity[] = [
  AgeGranularity.Lifecycle,
  AgeGranularity.TenYear,
  AgeGranularity.FiveYear,
  AgeGranularity.Detailed,
];

// When Age is the Column dimension, each bucket becomes a stacked segment + legend entry —
// unlike Row, where extra buckets just mean more (scrollable) bars. FiveYear (24) and
// especially Detailed (up to 120) buckets are unreadable as a legend/stack and only make sense
// on Row, so Column is restricted to the coarser granularities.
export const COLUMN_SAFE_AGE_GRANULARITIES: AgeGranularity[] = [AgeGranularity.Lifecycle, AgeGranularity.TenYear];

// Mirrors Demographics.DEFAULT_AGE_CAP / FIVE_YEAR_GROUP_COUNT / TEN_YEAR_GROUP_COUNT /
// LIFECYCLE_TOTALS_COUNT (C#).
const AGE_BUCKET_COUNTS: Record<AgeGranularity, number> = {
  [AgeGranularity.Detailed]: 120,
  [AgeGranularity.FiveYear]: 24,
  [AgeGranularity.TenYear]: 12,
  [AgeGranularity.Lifecycle]: 4,
};

export function getCensusCategoryCount(dimension: CensusDimension, ageGranularity: AgeGranularity): number {
  return dimension === CensusDimension.Age
    ? AGE_BUCKET_COUNTS[ageGranularity]
    : CENSUS_CATEGORY_COUNTS[dimension];
}

export interface CensusLabels {
  dimensionNames: Record<CensusDimension, string>;
  // Age's category names depend on the selected granularity, so it's a function instead of a
  // fixed array like every other dimension.
  categoryNames: Record<Exclude<CensusDimension, CensusDimension.Age>, string[]> & {
    [CensusDimension.Age]: (ageGranularity: AgeGranularity) => string[];
  };
}

// Generic per-column palette for the census chart — the column dimension can be anything
// from 4 to 8 categories depending on what the user picks, so colors are assigned by index
// rather than by a fixed named category like the other charts use.
export const CENSUS_PALETTE: string[] = [
  '#624532',
  '#7E9EAE',
  '#00C217',
  '#005C4E',
  '#2462FF',
  '#A1A1A1',
  '#FF0000',
  '#B981C0',
];

// Reshapes the flat row-major counts array pushed from C# into a 2D grid using the
// currently selected dimensions' (and age granularity's) category counts.
export function reshapeCrossTab(
  flat: number[],
  rowDim: CensusDimension,
  colDim: CensusDimension,
  ageGranularity: AgeGranularity
): number[][] {
  const rowCount = getCensusCategoryCount(rowDim, ageGranularity);
  const colCount = getCensusCategoryCount(colDim, ageGranularity);
  const grid: number[][] = [];
  for (let r = 0; r < rowCount; r++) {
    const row: number[] = [];
    for (let c = 0; c < colCount; c++) {
      const index = r * colCount + c;
      row.push(index < flat.length ? flat[index] : 0);
    }
    grid.push(row);
  }
  return grid;
}

// Numeric age-range label for a bucket at a given granularity, e.g. "20-24" for FiveYear
// bucket 4. Matches Demographics.cs's simulation-age-unit scale (capped at 120), same as the
// old per-age/5-year/10-year exporter tables.
export function formatAgeBucketLabel(bucketIndex: number, ageGranularity: AgeGranularity): string {
  if (ageGranularity === AgeGranularity.Detailed) {
    return String(bucketIndex);
  }
  const step = ageGranularity === AgeGranularity.FiveYear ? 5 : 10;
  const start = bucketIndex * step;
  const end = Math.min(start + step - 1, 119);
  return `${start}-${end}`;
}
