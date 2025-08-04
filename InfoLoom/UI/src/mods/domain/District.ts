import { Entity, Name, Typed, IndicatorValue } from 'cs2/bindings';
import { ValueBinding } from 'cs2/api';

export interface LocalServiceBuilding extends Typed<''> {
  name: Name;
  serviceIcon: string;
  entity: Entity;
}

export interface DistrictPolicy extends Typed<''> {
  name: Name;
  icon: string;
  entity: Entity;
}

export interface AgeData extends Typed<''> {
  children: number;
  teens: number;
  adults: number;
  elders: number;
  total: number;
}

export interface EducationData extends Typed<''> {
  uneducated: number;
  poorlyEducated: number;
  educated: number;
  wellEducated: number;
  highlyEducated: number;
  total: number;
}

export interface EmploymentData extends Typed<''> {
  uneducated: number;
  poorlyEducated: number;
  educated: number;
  wellEducated: number;
  highlyEducated: number;
  openPositions: number;
  total: number;
}

export interface District extends Typed<''> {
  name: Name;
  householdCount: number;
  maxHouseholds: number;
  residentCount: number;
  petCount: number;
  wealthKey: string;
  ageData: AgeData;
  educationData: EducationData;
  employeeCount: number;
  maxEmployees: number;
  educationDataEmployees: EmploymentData;
  educationDataWorkplaces: EmploymentData;
  localServiceBuildings: LocalServiceBuilding[];
  entity: Entity;
  policyCount: number;
  policies: DistrictPolicy[];
  
  elementaryEligible: number;
  highSchoolEligible: number;
  collegeEligible: number;
  universityEligible: number;
  
  elementaryCapacity: number;
  highSchoolCapacity: number;
  collegeCapacity: number;
  universityCapacity: number;
  
  elementaryStudents: number;
  highSchoolStudents: number;
  collegeStudents: number;
  universityStudents: number;

  elementaryAvailability: IndicatorValue;
  highSchoolAvailability: IndicatorValue;
  collegeAvailability: IndicatorValue;
  universityAvailability: IndicatorValue;

}
