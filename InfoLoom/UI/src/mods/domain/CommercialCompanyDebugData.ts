import { Entity } from 'cs2/utils';

/**
 * Represents efficiency factor information for a commercial company
 */
export interface EfficiencyFactorInfo {
    factor: string | number;
    value: number;
    result: number;
}

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

/**
 * Container for commercial company data from the C# backend - now array-based
 */
export interface CommercialDatas {
    Companies: CommercialCompanyDebug[];
}