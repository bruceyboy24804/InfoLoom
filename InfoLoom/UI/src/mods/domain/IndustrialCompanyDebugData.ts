import { Entity } from "cs2/utils";
import { EfficiencyFactorInfo } from "./EfficiencyFactorInfo";

export interface ProcessResourceInfo {
    resourceName: string;
    amount: number;
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
    Resources: string;
    ResourceAmount: number;
    ProcessResources: ProcessResourceInfo[];
    TotalEfficiency: number;
    Factors: EfficiencyFactorInfo[];
}