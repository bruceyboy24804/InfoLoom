export interface LocalModifier {
  Type: string;
  Mode: string;
  RadiusCombineMode: string;
  DeltaMin: number;
  DeltaMax: number;
  RadiusMin: number;
  RadiusMax: number;
}

export interface CityModifier {
  Type: string;
  Mode: string;
  DeltaMin: number;
  DeltaMax: number;
}

export interface EntityModifierData {
  EntityIndex: number;
  Name: string;
  Modifiers: LocalModifier[];
  CityModifiers: CityModifier[];
}
