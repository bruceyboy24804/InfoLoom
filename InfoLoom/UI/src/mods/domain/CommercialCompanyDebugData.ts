import { Entity } from 'cs2/utils';
import { EfficiencyFactorInfo } from './EfficiencyFactorInfo';
import {Resource} from 'cs2/bindings';
export interface ResourceInfo {
    ResourceName: string;
    Amount: number;
    Icon: string;
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

    Profitability: number;
    LastTotalWorth: number;
    TotalWages: number;
    ProductionPerDay: number;
    EfficiencyValue: number;
    Concentration: number;
    OutputResourceName: string;
    Resources: ResourceInfo[]; // Array of all resources associated with the company
}

