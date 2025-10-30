import { PopulationAtAge } from 'mods/domain/populationAtAge';
import { GroupingStrategy } from '../../domain/GroupingStrategy';

export interface AgeRange {
	label: string;
	min: number;
	max: number;
}

export interface AggregatedGroup {
	label: string;
	work: number;
	elementary: number;
	highSchool: number;
	college: number;
	university: number;
	retired: number;
	unemployed: number;
	uneducated: number;
	poorlyEducated: number;
	educated: number;
	wellEducated: number;
	highlyEducated: number;
	childOrTeenWithNoSchool: number;
}

/** Generate age ranges for grouping strategies */
export function generateRanges(step: number): AgeRange[] {
	const ranges: AgeRange[] = [];
	for (let i = 0; i < 120; i += step) {
		ranges.push({
			label: i === 0 ? `0-${step}` : i + step >= 120 ? `${i}-${120}` : `${i}-${i + step}`,
			min: i,
			max: i + step,
		});
	}
	return ranges;
}

/** Predefined lifecycle ranges */
export const LIFECYCLE_RANGES: AgeRange[] = [
	{ label: 'Child', min: 0, max: 20 },
	{ label: 'Teen', min: 21, max: 35 },
	{ label: 'Adult', min: 36, max: 83 },
	{ label: 'Elderly', min: 84, max: 120 },
];

/** Create empty aggregated group */
function createEmptyGroup(label: string): AggregatedGroup {
	return {
		label,
		work: 0,
		elementary: 0,
		highSchool: 0,
		college: 0,
		university: 0,
		retired: 0,
		unemployed: 0,
		uneducated: 0,
		poorlyEducated: 0,
		educated: 0,
		wellEducated: 0,
		highlyEducated: 0,
		childOrTeenWithNoSchool: 0,
	};
}

/** Aggregate a single data point into a group */
function aggregateDataPoint(group: AggregatedGroup, data: PopulationAtAge): void {
	group.work += data.Work;
	group.elementary += data.School1;
	group.highSchool += data.School2;
	group.college += data.School3;
	group.university += data.School4;
	group.retired += data.Retired;
	group.unemployed += data.Unemployed;
	group.uneducated += data.Uneducated;
	group.poorlyEducated += data.PoorlyEducated;
	group.educated += data.Educated;
	group.wellEducated += data.WellEducated;
	group.highlyEducated += data.HighlyEducated;
	group.childOrTeenWithNoSchool += data.ChildOrTeenWithNoSchool;
}

/** Aggregate data by predefined ranges (lifecycle or custom) */
function aggregateByRanges(
	data: PopulationAtAge[],
	ranges: AgeRange[]
): AggregatedGroup[] {
	const groups = ranges.map(range => createEmptyGroup(range.label));

	data.forEach(d => {
		if (d.Age > 120) return;
		const idx = ranges.findIndex(range => d.Age >= range.min && d.Age <= range.max);
		if (idx !== -1) {
			aggregateDataPoint(groups[idx], d);
		}
	});

	return groups;
}

/** Aggregate data by fixed step intervals (5-year, 10-year) */
function aggregateByStep(
	data: PopulationAtAge[],
	step: number
): AggregatedGroup[] {
	const groups: AggregatedGroup[] = [];
	
	for (let i = 0; i < 120; i += step) {
		groups.push(createEmptyGroup(`${i}-${i + step}`));
	}

	data.forEach(d => {
		if (d.Age > 120) return;
		const idx = Math.floor(d.Age / step);
		if (groups[idx]) {
			aggregateDataPoint(groups[idx], d);
		}
	});

	return groups;
}

/** Get value for a specific attribute at a specific age (detailed view) */
function getValueAtAge(
	data: PopulationAtAge[],
	attr: keyof PopulationAtAge,
	age: number
): number {
	return data
		.filter(d => d.Age === age)
		.reduce((sum, d) => sum + ((d[attr] as number) || 0), 0);
}

/** Transform data based on grouping strategy */
export function transformDataByStrategy(
	data: PopulationAtAge[],
	strategy: GroupingStrategy
): { labels: string[]; groups: AggregatedGroup[] | null } {
	if (!data.length) {
		return { labels: [], groups: null };
	}

	switch (strategy) {
		case GroupingStrategy.None: {
			// Detailed view: individual ages 0-120
			const labels = Array.from({ length: 121 }, (_, i) => String(i));
			const groups = labels.map(age => {
				const ageNum = parseInt(age);
				return {
					label: age,
					work: getValueAtAge(data, 'Work', ageNum),
					elementary: getValueAtAge(data, 'School1', ageNum),
					highSchool: getValueAtAge(data, 'School2', ageNum),
					college: getValueAtAge(data, 'School3', ageNum),
					university: getValueAtAge(data, 'School4', ageNum),
					retired: getValueAtAge(data, 'Retired', ageNum),
					unemployed: getValueAtAge(data, 'Unemployed', ageNum),
					uneducated: getValueAtAge(data, 'Uneducated', ageNum),
					poorlyEducated: getValueAtAge(data, 'PoorlyEducated', ageNum),
					educated: getValueAtAge(data, 'Educated', ageNum),
					wellEducated: getValueAtAge(data, 'WellEducated', ageNum),
					highlyEducated: getValueAtAge(data, 'HighlyEducated', ageNum),
					childOrTeenWithNoSchool: getValueAtAge(data, 'ChildOrTeenWithNoSchool', ageNum),
				};
			});
			return { labels, groups };
		}

		case GroupingStrategy.LifeCycle: {
			const groups = aggregateByRanges(data, LIFECYCLE_RANGES);
			return { labels: groups.map(g => g.label), groups };
		}

		case GroupingStrategy.FiveYear: {
			const groups = aggregateByStep(data, 5);
			return { labels: groups.map(g => g.label), groups };
		}

		case GroupingStrategy.TenYear: {
			const groups = aggregateByStep(data, 10);
			return { labels: groups.map(g => g.label), groups };
		}

		default:
			return { labels: [], groups: null };
	}
}
