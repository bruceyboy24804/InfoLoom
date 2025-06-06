export interface CommercialProductData {
  ResourceType: string;
  ResourceName: string;
  ResourceIcon: string;
  Demand: number;
  Building: number;
  Free: number;
  Companies: number;
  Workers: number;
  SvcPercent: number;
  CapPercent: number;
  CapPerCompany: number;
  WrkPercent: number;
  TaxFactor: number;
  ResourceNeeds: number;
  ProduceCapacity: number;
  CurrentAvailables: number;
  TotalAvailables: number;
  MaxServiceWorkers: number;
  CurrentServiceWorkers: number;
}