import { Entity } from 'cs2/utils';

export enum BuildingHappinessFactor {
  Telecom,
  Crime,
  AirPollution,
  Electricity,
  Healthcare,
  GroundPollution,
  NoisePollution,
  Water,
  WaterPollution,
  Sewage,
  Garbage,
  Entertainment,
  Education,
  Mail,
  Welfare,
  Leisure,
  Tax,
  Materials,
  Customers,
  UneducatedWorkers,
  EducatedWorkers,
  Apartment,
  MissingWorkers,
  Efficiency,
  InputCosts,
  OutputCosts,
  ElectricityFee,
  WaterFee,
  Count
  }
  export enum CitizenHappinessKey {
  Depressed,
  Sad,
  Neutral,
  Content,
  Happy
}

export interface CitizenHappiness {
  key: string; // The enum name as string ("Depressed", "Sad", etc.)
  iconPath: string; // Path to the happiness icon
}
export interface ResidentialHouseholdData {
  ResidentialEntity: Entity;
  ResidentialName: string;
  ResidentialIcon: string;
  CurrentHouseholdCount: number;
  MaxHouseholdCount: number;
  HappinessFactors: BuildingHappinessFactorValue[];
  OverallHappiness: CitizenHappiness; // Changed from number to CitizenHappiness
}
export interface BuildingHappinessFactorValue {
    Factor: BuildingHappinessFactor;
    Value: number;
}
export function compareHappinessFactors(a: BuildingHappinessFactorValue, b: BuildingHappinessFactorValue): number {
    return b.Value - a.Value; // Descending order (matching your C# implementation)
}