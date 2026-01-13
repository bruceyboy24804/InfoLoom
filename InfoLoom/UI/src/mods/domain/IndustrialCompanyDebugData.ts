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
  ResourceAmount: number;
  ResourceIcon: string;
  IsExtractor: boolean;

  MoneyAmount: number;
  Input1Resources: ResourceInfo[];
  Input2Resources: ResourceInfo[];
  OutputResources: ResourceInfo[];
  MaintenanceResources: ResourceInfo[];

  Income: number;
  Worth: number;
  Profit: number;
  WagePaid: number;
  RentPaid: number;
  ElectricityPaid: number;
  WaterPaid: number;
  SewagePaid: number;
  GarbagePaid: number;
  TaxPaid: number;
  ResourcesBoughtPaid: number;
  CurrentCustomers: number;
  MonthlyCustomers: number;
  
}
