import { useMemo } from 'react';
import { PopulationAtAge } from 'mods/domain/populationAtAge';
import { GroupingStrategy } from '../../domain/GroupingStrategy';
import { transformDataByStrategy, generateRanges, LIFECYCLE_RANGES } from './dataTransform';
import { CHART_COLORS } from './chartConfig';
import { ChartData, LegendLabels, DemographicsType, GroupingStrategyOption } from './types';

/**
 * Custom hook to transform population data into chart-ready format
 */
export function useChartData(
	structureDetails: PopulationAtAge[],
	groupingStrategy: GroupingStrategy,
	chartType: DemographicsType,
	legendLabels: LegendLabels,
	lifecycleLabels?: string[]
): ChartData {
	return useMemo((): ChartData => {
		try {
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
	}, [structureDetails, groupingStrategy, chartType, legendLabels, lifecycleLabels]);
}

/**
 * Custom hook to create translated legend labels
 */
export function useLegendLabels(
	translate: (key: string, fallback: string) => string | null
): LegendLabels {
	return useMemo(
		(): LegendLabels => ({
			work: translate('InfoLoomTwo.DemographicsPanel[LegendItem1]', 'Work') || 'Work',
			elementary: translate('InfoLoomTwo.DemographicsPanel[LegendItem2]', 'Elementary') || 'Elementary',
			highSchool: translate('InfoLoomTwo.DemographicsPanel[LegendItem3]', 'High School') || 'High School',
			college: translate('InfoLoomTwo.DemographicsPanel[LegendItem4]', 'College') || 'College',
			university: translate('InfoLoomTwo.DemographicsPanel[LegendItem5]', 'University') || 'University',
			retired: translate('InfoLoomTwo.DemographicsPanel[LegendItem6]', 'Retired') || 'Retired',
			unemployed: translate('InfoLoomTwo.DemographicsPanel[LegendItem7]', 'Unemployed') || 'Unemployed',
			uneducated: translate('InfoLoomTwo.DemographicsPanel[LegendItem8]', 'Uneducated') || 'Uneducated',
			poorlyEducated:
				translate('InfoLoomTwo.DemographicsPanel[LegendItem9]', 'Poorly Educated') || 'Poorly Educated',
			educated: translate('InfoLoomTwo.DemographicsPanel[LegendItem10]', 'Educated') || 'Educated',
			wellEducated: translate('InfoLoomTwo.DemographicsPanel[LegendItem11]', 'Well Educated') || 'Well Educated',
			highlyEducated:
				translate('InfoLoomTwo.DemographicsPanel[LegendItem12]', 'Highly Educated') || 'Highly Educated',
			childOrTeenWithNoSchool:
				translate('InfoLoomTwo.DemographicsPanel[LegendItem13]', 'Child/Teen with No School') ||
				'Child/Teen with No School',
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
			translate('InfoLoomTwo.DemographicsPanel[YAxisItem1]', 'Child') || 'Child',
			translate('InfoLoomTwo.DemographicsPanel[YAxisItem2]', 'Teen') || 'Teen',
			translate('InfoLoomTwo.DemographicsPanel[YAxisItem3]', 'Adult') || 'Adult',
			translate('InfoLoomTwo.DemographicsPanel[YAxisItem4]', 'Elderly') || 'Elderly',
		],
		[translate]
	);
}

/**
 * Custom hook to create grouping strategy options
 */
export function useGroupingStrategies(
	translate: (key: string, fallback: string) => string | null
): GroupingStrategyOption[] {
	return useMemo(
		(): GroupingStrategyOption[] => [
			{
				label: translate('InfoLoomTwo.DemographicsPanel[DropdownItem1]', 'Detailed View') || 'Detailed View',
				value: GroupingStrategy.None,
				ranges: [],
			},
			{
				label: translate('InfoLoomTwo.DemographicsPanel[DropdownItem2]', '5-Year Groups') || '5-Year Groups',
				value: GroupingStrategy.FiveYear,
				ranges: generateRanges(5),
			},
			{
				label: translate('InfoLoomTwo.DemographicsPanel[DropdownItem3]', '10-Year Groups') || '10-Year Groups',
				value: GroupingStrategy.TenYear,
				ranges: generateRanges(10),
			},
			{
				label: translate('InfoLoomTwo.DemographicsPanel[DropdownItem4]', 'Lifecycle Groups') || 'Lifecycle Groups',
				value: GroupingStrategy.LifeCycle,
				ranges: LIFECYCLE_RANGES,
			},
		],
		[translate]
	);
}
