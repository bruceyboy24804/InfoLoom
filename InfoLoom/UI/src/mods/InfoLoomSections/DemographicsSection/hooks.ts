import { useMemo } from 'react';
import { useValue } from 'cs2/api';
import { GroupingStrategy } from '../../domain/GroupingStrategy';
import { transformDataByStrategy, generateRanges, getLifecycleRangePlaceholders } from './dataTransform';
import { CHART_COLORS } from './chartConfig';
import { ChartData, LegendLabels, DemographicsType, GroupingStrategyOption } from './types';
import { Localekeys } from 'mods/locale';
import { PopulationLifecycleInfo } from 'mods/domain/populationLifecycleInfo';
import { PopulationDetailedGroupInfo } from 'mods/domain/populationDetailedGroupInfo';
import { PopulationFiveYearGroupInfo } from 'mods/domain/populationFiveYearGroupInfo';
import { PopulationTenYearGroupInfo } from 'mods/domain/populationTenYearGroupInfo';
import { DemographicsDetailedData, DemographicsFiveYearDetails, DemographicsTenYearDetails, DemographicsLifecycleDetails } from 'mods/bindings';
function generateAgeRangeLabel(age: number, step: number): string {
	const maxAge = 120;
	if (age === 0) {
		return `0-${step - 1}`;
	} else if (age + step >= maxAge) {
		return `${age}-${maxAge}`;
	} else {
		return `${age}-${age + step - 1}`;
	}
}

export function useChartData(
	structureDetails: PopulationDetailedGroupInfo[],
	fiveYearGroups: PopulationFiveYearGroupInfo[],
	tenYearGroups: PopulationTenYearGroupInfo[],
	lifecycleDetails: PopulationLifecycleInfo[],
	groupingStrategy: GroupingStrategy,
	chartType: DemographicsType,
	legendLabels: LegendLabels,
	lifecycleLabels?: string[]
): ChartData {
	const detailedData = structureDetails as PopulationDetailedGroupInfo[];
	const fiveYearDetails = fiveYearGroups as PopulationFiveYearGroupInfo[];
	const tenYearDetails = tenYearGroups as PopulationTenYearGroupInfo[];
	const lifecycleData = lifecycleDetails as PopulationLifecycleInfo[];

	return useMemo((): ChartData => {
		try {
			if (groupingStrategy === GroupingStrategy.FiveYear && fiveYearDetails?.length) {
				const labels = fiveYearDetails.map(g => generateAgeRangeLabel(g.Age, 5));

				const datasets =
					chartType === DemographicsType.Employment
						? [
								{
									label: legendLabels.work,
									data: fiveYearDetails.map(g => g.Work),
									backgroundColor: CHART_COLORS.work,
								},
								{
									label: legendLabels.elementary,
									data: fiveYearDetails.map(g => g.School1),
									backgroundColor: CHART_COLORS.elementary,
								},
								{
									label: legendLabels.highSchool,
									data: fiveYearDetails.map(g => g.School2),
									backgroundColor: CHART_COLORS.highSchool,
								},
								{
									label: legendLabels.college,
									data: fiveYearDetails.map(g => g.School3),
									backgroundColor: CHART_COLORS.college,
								},
								{
									label: legendLabels.university,
									data: fiveYearDetails.map(g => g.School4),
									backgroundColor: CHART_COLORS.university,
								},
								{
									label: legendLabels.retired,
									data: fiveYearDetails.map(g => g.Retired),
									backgroundColor: CHART_COLORS.Retired,
								},
								{
									label: legendLabels.unemployed,
									data: fiveYearDetails.map(g => g.Unemployed),
									backgroundColor: CHART_COLORS.Unemployed,
								},
								{
									label: legendLabels.childOrTeenWithNoSchool,
									data: fiveYearDetails.map(g => g.ChildOrTeenWithNoSchool),
									backgroundColor: CHART_COLORS.ChildOrTeenWithNoSchool,
								},
							]
						: [
								{
									label: legendLabels.uneducated,
									data: fiveYearDetails.map(g => g.Uneducated),
									backgroundColor: CHART_COLORS.Uneducated,
								},
								{
									label: legendLabels.poorlyEducated,
									data: fiveYearDetails.map(g => g.PoorlyEducated),
									backgroundColor: CHART_COLORS.PoorlyEducated,
								},
								{
									label: legendLabels.educated,
									data: fiveYearDetails.map(g => g.Educated),
									backgroundColor: CHART_COLORS.Educated,
								},
								{
									label: legendLabels.wellEducated,
									data: fiveYearDetails.map(g => g.WellEducated),
									backgroundColor: CHART_COLORS.WellEducated,
								},
								{
									label: legendLabels.highlyEducated,
									data: fiveYearDetails.map(g => g.HighlyEducated),
									backgroundColor: CHART_COLORS.HighlyEducated,
								},
							];

				return { labels, datasets };
			}

			if (groupingStrategy === GroupingStrategy.TenYear && tenYearDetails?.length) {
				const labels = tenYearDetails.map(g => generateAgeRangeLabel(g.Age, 10));

				const datasets =
					chartType === DemographicsType.Employment
						? [
								{
									label: legendLabels.work,
									data: tenYearDetails.map(g => g.Work),
									backgroundColor: CHART_COLORS.work,
								},
								{
									label: legendLabels.elementary,
									data: tenYearDetails.map(g => g.School1),
									backgroundColor: CHART_COLORS.elementary,
								},
								{
									label: legendLabels.highSchool,
									data: tenYearDetails.map(g => g.School2),
									backgroundColor: CHART_COLORS.highSchool,
								},
								{
									label: legendLabels.college,
									data: tenYearDetails.map(g => g.School3),
									backgroundColor: CHART_COLORS.college,
								},
								{
									label: legendLabels.university,
									data: tenYearDetails.map(g => g.School4),
									backgroundColor: CHART_COLORS.university,
								},
								{
									label: legendLabels.retired,
									data: tenYearDetails.map(g => g.Retired),
									backgroundColor: CHART_COLORS.Retired,
								},
								{
									label: legendLabels.unemployed,
									data: tenYearDetails.map(g => g.Unemployed),
									backgroundColor: CHART_COLORS.Unemployed,
								},
								{
									label: legendLabels.childOrTeenWithNoSchool,
									data: tenYearDetails.map(g => g.ChildOrTeenWithNoSchool),
									backgroundColor: CHART_COLORS.ChildOrTeenWithNoSchool,
								},
							]
						: [
								{
									label: legendLabels.uneducated,
									data: tenYearDetails.map(g => g.Uneducated),
									backgroundColor: CHART_COLORS.Uneducated,
								},
								{
									label: legendLabels.poorlyEducated,
									data: tenYearDetails.map(g => g.PoorlyEducated),
									backgroundColor: CHART_COLORS.PoorlyEducated,
								},
								{
									label: legendLabels.educated,
									data: tenYearDetails.map(g => g.Educated),
									backgroundColor: CHART_COLORS.Educated,
								},
								{
									label: legendLabels.wellEducated,
									data: tenYearDetails.map(g => g.WellEducated),
									backgroundColor: CHART_COLORS.WellEducated,
								},
								{
									label: legendLabels.highlyEducated,
									data: tenYearDetails.map(g => g.HighlyEducated),
									backgroundColor: CHART_COLORS.HighlyEducated,
								},
							];

				return { labels, datasets };
			}

			if (groupingStrategy === GroupingStrategy.LifeCycle && lifecycleDetails?.length) {
				let labels = lifecycleDetails.map(g => String(g.Group));
				if (
					lifecycleLabels &&
					lifecycleLabels.length === lifecycleDetails.length
				) {
					labels = lifecycleLabels;
				}

				const datasets =
					chartType === DemographicsType.Employment
						? [
								{
									label: legendLabels.work,
									data: lifecycleDetails.map(g => g.Work),
									backgroundColor: CHART_COLORS.work,
								},
								{
									label: legendLabels.elementary,
									data: lifecycleDetails.map(g => g.School1),
									backgroundColor: CHART_COLORS.elementary,
								},
								{
									label: legendLabels.highSchool,
									data: lifecycleDetails.map(g => g.School2),
									backgroundColor: CHART_COLORS.highSchool,
								},
								{
									label: legendLabels.college,
									data: lifecycleDetails.map(g => g.School3),
									backgroundColor: CHART_COLORS.college,
								},
								{
									label: legendLabels.university,
									data: lifecycleDetails.map(g => g.School4),
									backgroundColor: CHART_COLORS.university,
								},
								{
									label: legendLabels.retired,
									data: lifecycleDetails.map(g => g.Retired),
									backgroundColor: CHART_COLORS.Retired,
								},
								{
									label: legendLabels.unemployed,
									data: lifecycleDetails.map(g => g.Unemployed),
									backgroundColor: CHART_COLORS.Unemployed,
								},
								{
									label: legendLabels.childOrTeenWithNoSchool,
									data: lifecycleDetails.map(g => g.ChildOrTeenWithNoSchool),
									backgroundColor: CHART_COLORS.ChildOrTeenWithNoSchool,
								},
							]
						: [
								{
									label: legendLabels.uneducated,
									data: lifecycleDetails.map(g => g.Uneducated),
									backgroundColor: CHART_COLORS.Uneducated,
								},
								{
									label: legendLabels.poorlyEducated,
									data: lifecycleDetails.map(g => g.PoorlyEducated),
									backgroundColor: CHART_COLORS.PoorlyEducated,
								},
								{
									label: legendLabels.educated,
									data: lifecycleDetails.map(g => g.Educated),
									backgroundColor: CHART_COLORS.Educated,
								},
								{
									label: legendLabels.wellEducated,
									data: lifecycleDetails.map(g => g.WellEducated),
									backgroundColor: CHART_COLORS.WellEducated,
								},
								{
									label: legendLabels.highlyEducated,
									data: lifecycleDetails.map(g => g.HighlyEducated),
									backgroundColor: CHART_COLORS.HighlyEducated,
								},
							];

				return {
					labels,
					datasets,
				};
			}

			// Transform data using unified aggregation function
			const transformed = transformDataByStrategy(structureDetails, groupingStrategy);

			if (!transformed.groups) {
				return {
					labels: [],
					datasets: [],
				};
			}

		const groups = transformed.groups;
		let labels = transformed.labels;

		// Use translated lifecycle labels if applicable
		if (
			groupingStrategy === GroupingStrategy.LifeCycle &&
			lifecycleLabels &&
			lifecycleLabels.length === groups.length
		) {
			labels = lifecycleLabels;
		}

		// Build datasets based on chart type (Employment vs Education)
		const datasets =
			chartType === DemographicsType.Employment
				? [
						{
							label: legendLabels.work,
							data: groups.map(g => g.work),
							backgroundColor: CHART_COLORS.work,
						},
						{
							label: legendLabels.elementary,
							data: groups.map(g => g.elementary),
							backgroundColor: CHART_COLORS.elementary,
						},
						{
							label: legendLabels.highSchool,
							data: groups.map(g => g.highSchool),
							backgroundColor: CHART_COLORS.highSchool,
						},
						{
							label: legendLabels.college,
							data: groups.map(g => g.college),
							backgroundColor: CHART_COLORS.college,
						},
						{
							label: legendLabels.university,
							data: groups.map(g => g.university),
							backgroundColor: CHART_COLORS.university,
						},
						{
							label: legendLabels.retired,
							data: groups.map(g => g.retired),
							backgroundColor: CHART_COLORS.Retired,
						},
						{
							label: legendLabels.unemployed,
							data: groups.map(g => g.unemployed),
							backgroundColor: CHART_COLORS.Unemployed,
						},
						{
							label: legendLabels.childOrTeenWithNoSchool,
							data: groups.map(g => g.childOrTeenWithNoSchool),
							backgroundColor: CHART_COLORS.ChildOrTeenWithNoSchool,
						},
					]
				: [
						{
							label: legendLabels.uneducated,
							data: groups.map(g => g.uneducated),
							backgroundColor: CHART_COLORS.Uneducated,
						},
						{
							label: legendLabels.poorlyEducated,
							data: groups.map(g => g.poorlyEducated),
							backgroundColor: CHART_COLORS.PoorlyEducated,
						},
						{
							label: legendLabels.educated,
							data: groups.map(g => g.educated),
							backgroundColor: CHART_COLORS.Educated,
						},
						{
							label: legendLabels.wellEducated,
							data: groups.map(g => g.wellEducated),
							backgroundColor: CHART_COLORS.WellEducated,
						},
						{
							label: legendLabels.highlyEducated,
							data: groups.map(g => g.highlyEducated),
							backgroundColor: CHART_COLORS.HighlyEducated,
						},
					];

			return {
				labels,
				datasets,
			};
		} catch (error) {
			console.error('Error transforming chart data:', error);
			return {
				labels: [],
				datasets: [],
			};
		}
	}, [
		structureDetails,
		groupingStrategy,
		chartType,
		legendLabels,
		lifecycleLabels,
		lifecycleDetails,
		fiveYearDetails,
		tenYearDetails,
	]);
}

/**
 * Custom hook to create translated legend labels
 */
export function useLegendLabels(
	translate: (key: string, fallback: string) => string | null
): LegendLabels {
	return useMemo(
		(): LegendLabels => ({
			work: translate(Localekeys.Worker, 'Work') || 'Work',
			elementary: translate(Localekeys.Elementary, 'Elementary') || 'Elementary' ,
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
			childOrTeenWithNoSchool:translate('InfoLoomTwo.DemographicsPanel[LegendItem13]', 'Child/Teen with No School') || 'Child/Teen with No School',
		}),
		[translate]
	);
}

/**
 * Custom hook to create translated lifecycle labels
 */
export function useLifecycleLabels(
	translate: (key: string, fallback: string) => string | null
): string[] {
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
 * Custom hook to create grouping strategy options
 */
export function useGroupingStrategies(
	translate: (key: string, fallback: string) => string | null,
	structureDetails?: PopulationDetailedGroupInfo[]
): GroupingStrategyOption[] {
	return useMemo(
		(): GroupingStrategyOption[] => [
			{
				label: translate(Localekeys.DetailedView, 'Detailed View') || 'Detailed View',
				value: GroupingStrategy.None,
				ranges: [],
			},
			{
				label: translate(Localekeys.FiveYearGroups, '5-Year Groups') || '5-Year Groups',
				value: GroupingStrategy.FiveYear,
				ranges: generateRanges(5),
			},
			{
				label: translate(Localekeys.TenYearGroups, '10-Year Groups') || '10-Year Groups',
				value: GroupingStrategy.TenYear,
				ranges: generateRanges(10),
			},
			{
				label: translate(Localekeys.LifeCycleGroups, 'Lifecycle Groups') || 'Lifecycle Groups',
				value: GroupingStrategy.LifeCycle,
				ranges: getLifecycleRangePlaceholders(),
			},
		],
		[translate, structureDetails]
	);
}