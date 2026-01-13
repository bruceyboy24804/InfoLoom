import { AgeRange } from './dataTransform';
import { GroupingStrategy } from '../../domain/GroupingStrategy';

export interface LegendLabels {
	work: string;
	elementary: string;
	highSchool: string;
	college: string;
	university: string;
	retired: string;
	unemployed: string;
	uneducated: string;
	poorlyEducated: string;
	educated: string;
	wellEducated: string;
	highlyEducated: string;
	childOrTeenWithNoSchool: string;
}

export interface ChartDataset {
	label: string;
	data: number[];
	backgroundColor: string;
}

export interface ChartData {
	labels: string[];
	datasets: ChartDataset[];
}

export interface GroupingStrategyOption {
	label: string;
	value: GroupingStrategy;
	ranges: AgeRange[];
}

export enum DemographicsType {
	Employment = 0,
	Education = 1,
}

export enum Totals {
	AllCitizens = 0, // num citizens in the city 0 = 1+2+3
	Locals = 1, // num locals
	Tourists = 2, // num tourists
	Commuters = 3, // num commuters
	Students = 4, // num students (in locals) 4 <= 1
	Workers = 5, // num workers (in locals) 5 <= 1
	OldestCitizenAge = 6, // oldest cim
	MovingAways = 7, // moving aways
	DeadCitizens = 8, // dead cims
	HomelessCitizens = 9, // homeless citizens
}