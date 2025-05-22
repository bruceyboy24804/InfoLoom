import { Entity } from 'cs2/utils';
import { EfficiencyFactorInfo } from './EfficiencyFactorInfo';
import {Resource} from 'cs2/bindings';
/**
 * Represents a commercial company's data displayed in the debug panel
 */

export interface CommercialCompanyDebug {
    EntityId: Entity;
    CompanyName: string;
    ServiceAvailable: number;
    MaxService: number;
    TotalEmployees: number;
    MaxWorkers: number;
    VehicleCount: number;
    VehicleCapacity: number;
    Resources: string;
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
}

