import { Entity } from 'cs2/utils';
import { EfficiencyFactorInfo } from './EfficiencyFactorInfo';
export interface ResourceInfo {
  ResourceName: string;
  Amount: number;
  Icon: string;
}
export interface ProcessResourceInfo {
  resourceName: string;
  amount: number;
  resourceIcon: string;
  isOutput: boolean;
}

export interface IndustrialCompanyDebug {
  EntityId: Entity;
  CompanyName: string;
  CompanyIcon: string;
  TotalEmployees: number;
  MaxWorkers: number;
  VehicleCount: number;
  VehicleCapacity: number;
  ResourceAmount: number;
  ResourceIcon: string;
  ProcessResources: ProcessResourceInfo[];
  TotalEfficiency: number;
  Factors: EfficiencyFactorInfo[];
  Resources: ResourceInfo[];
  Profitability: number;
  LastTotalWorth: number;
  TotalWages: number;
  ProductionPerDay: number;
  EfficiencyValue: number;
  Concentration: number;
  OutputResourceName: string;
  IsExtractor: boolean;
}
