export interface EntityModifierData {
  EntityIndex: number;
  Name: string;
  Modifiers: LocalInfo[];
  CityModifiers: CityInfo[];
}
export interface LocalInfo {
  Type: string;
  Mode: string;
  RadiusCombineMode: string;
  DeltaMin: number;
  DeltaMax: number;
  RadiusMin: number;
  RadiusMax: number;
}
export interface CityInfo {
  Type: string;
  Mode: string;
  DeltaMin: number;
  DeltaMax: number;
}
