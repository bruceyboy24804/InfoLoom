import { Entity } from 'cs2/utils';
import { EfficiencyFactorInfo } from './EfficiencyFactorInfo';
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
}