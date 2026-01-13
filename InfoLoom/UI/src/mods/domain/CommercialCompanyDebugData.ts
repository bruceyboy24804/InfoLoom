import { Entity } from 'cs2/utils';
import { EfficiencyFactorInfo } from './EfficiencyFactorInfo';
import { Resource } from 'cs2/bindings';
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
export interface CommercialCompanyDebug {
  EntityId: Entity;
  CompanyName: string;
  ServiceAvailable: number;
  MaxService: number;
  TotalEmployees: number;
  MaxWorkers: number;
  VehicleCount: number;
  VehicleCapacity: number;
  ResourceName: string;
  ResourceIcon: string;
  ResourceAmount: number;
  TotalEfficiency: number;
  Factors: EfficiencyFactorInfo[];
  ProcessResources: ProcessResourceInfo[];
  Profitability: number;
  LastTotalWorth: number;
  TotalWages: number;
  ProductionPerDay: number;
  EfficiencyValue: number;
  Concentration: number;
  OutputResourceName: string;
  MoneyAmount: number;
  Input1Resources: ResourceInfo[];
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
