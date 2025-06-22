// Import from CS2 bindings but define our own interface that matches the backend
import { EfficiencyFactor } from 'cs2/bindings';

export enum EfficiencyFactorEnum {
  Destroyed,
  Abandoned,
  Disabled,
  Fire,
  ServiceBudget,
  NotEnoughEmployees,
  SickEmployees,
  EmployeeHappiness,
  ElectricitySupply,
  ElectricityFee,
  WaterSupply,
  DirtyWater,
  SewageHandling,
  WaterFee,
  Garbage,
  Telecom,
  Mail,
  MaterialSupply,
  WindSpeed,
  WaterDepth,
  SunIntensity,
  NaturalResources,
  CityModifierSoftware,
  CityModifierElectronics,
  CityModifierIndustrialEfficiency,
  CityModifierOfficeEfficiency,
  CityModifierHospitalEfficiency,
  SpecializationBonus,
  Count,
}

// This interface needs to match the backend EfficiencyFactorInfo structure
export interface EfficiencyFactorInfo {
  Factor: EfficiencyFactorEnum; // Matches the C# enum
  Value: number; // Matches the C# int
  Result: number; // Matches the C# int
}
