import { bindValue, trigger } from 'cs2/api';
import mod from 'mod.json';
import { CommercialProductData } from './domain/commercialProductData';
import { populationAtAge } from './domain/populationAtAge';
import { District } from './domain/District';
import { industrialProductData } from './domain/industrialProductData';
import { ResourceTradeCost } from './domain/tradeCostData';
import { workforceInfo } from './domain/workforceInfo';
import { workplacesInfo } from './domain/WorkplacesInfo';
import { GroupingStrategy } from './domain/GroupingStrategy';
import { CommercialCompanyDebug } from './domain/CommercialCompanyDebugData';
import { IndustrialCompanyDebug } from './domain/IndustrialCompanyDebugData';
import { IndexSortingEnum } from 'mods/domain/CommercialCompanyEnums/IndexSortingEnum';
import { EfficiencyEnum } from 'mods/domain/CommercialCompanyEnums/EfficiencyEnum';
import { EmployeesEnum } from 'mods/domain/CommercialCompanyEnums/EmployeesEnum';
import { CompanyNameEnum } from 'mods/domain/CommercialCompanyEnums/CompanyNameEnum';
import { ProfitabilityEnum } from 'mods/domain/CommercialCompanyEnums/ProfitabilityEnum';
import { ServiceUsageEnum } from 'mods/domain/CommercialCompanyEnums/ServiceUsageEnum';
import { ResourceAmountEnum } from './domain/CommercialCompanyEnums/ResourceAmountEnum';
import {
  IndexSortingEnum2,
  ResourceAmountEnum2,
  ProfitabilityEnum2,
  CompanyNameEnum2,
  EfficiencyEnum2,
  EmployeesEnum2,
} from 'mods/domain/IndustrialCompanyEnums';
import {
  BuyCostEnum,
  ExportAmountEnum,
  ImportAmountEnum,
  ProfitEnum,
  ProfitMarginEnum,
  SellCostEnum,
  ResourceNameEnum,
} from 'mods/domain/TradeCostEnums';

const INFO_LOOM_MENU_OPEN = 'InfoLoomMenuOpen';
const COMMERCIAL_MENU_OPEN = 'CommercialMenuOpen';
const INDUSTRIAL_MENU_OPEN = 'IndustrialMenuOpen';
const DISTRICT_MENU_OPEN = 'DistrictMenuOpen';
const RESIDENTIAL_MENU_OPEN = 'ResidentialMenuOpen';
const BUILDING_DEMAND_OPEN = 'BuildingDemandOpen';
const COMMERCIAL_DEMAND_OPEN = 'CommercialDemandOpen';
const COMMERCIAL_PRODUCTS_OPEN = 'CommercialProductsOpen';
const DEMOGRAPHICS_OPEN = 'DemographicsOpen';
const DISTRICT_DATA_OPEN = 'DistrictDataOpen';
const INDUSTRIAL_DEMAND_OPEN = 'IndustrialDemandOpen';
const INDUSTRIAL_PRODUCTS_OPEN = 'IndustrialProductsOpen';
const RESIDENTIAL_DEMAND_OPEN = 'ResidentialDemandOpen';
const TRADE_COSTS_OPEN = 'TradeCostsOpen';
const WORKFORCE_OPEN = 'WorkforceOpen';
const WORKPLACES_OPEN = 'WorkplacesOpen';
const COMMERCIAL_COMPANY_DEBUG_OPEN = 'CommercialCompanyDebugOpen';
const INDUSTRIAL_COMPANY_DEBUG_OPEN = 'IndustrialCompanyDebugOpen';
const HOUSEHOLDS_DATA_OPEN = 'HouseholdsDataOpen';
export const BUILDING_DEMAND_DATA = 'BuildingDemandData';
export const COMMERCIAL_DATA = 'CommercialData';
export const COMMERCIAL_DATA_EX_RES = 'CommercialDataExRes';
export const COMMERCIAL_PRODUCTS_DATA = 'CommercialProductsData';
export const DEMOGRAPHICS_DATA_TOTALS = 'DemographicsDataTotals';
export const DEMOGRAPHICS_DATA_DETAILS = 'DemographicsDataDetails';
export const DEMOGRAPHICS_DATA_OLDEST_CITIZEN = 'DemographicsDataOldestCitizen';
export const DEMO_STATS_TOGGLED_ON = 'DemoStatsToggledOn';
export const DEMOGRAPHICS_DATA_GROUPED = 'DemographicsDataGrouped';

export const DISTRICT_DATA = 'DistrictData';
export const DISTRICT_EMPLOYEE_DATA = 'DistrictEmployeeData';
export const DISTRICT_COUNT = 'DistrictCount';
export const INDUSTRIAL_DATA = 'IndustrialData';
export const INDUSTRIAL_DATA_EX_RES = 'IndustrialDataExRes';
export const INDUSTRIAL_PRODUCTS_DATA = 'IndustrialProductsData';
export const RESIDENTIAL_DATA = 'ResidentialData';
export const TRADE_COSTS_DATA = 'TradeCostsData';
export const TRADE_COSTS_DATA_IMPORTS = 'TradeCostsDataImports';
export const TRADE_COSTS_DATA_EXPORTS = 'TradeCostsDataExports';
export const WORKFORCE_DATA = 'WorkforceData';
export const WORKPLACES_DATA = 'WorkplacesData';

export const InfoLoomMenuOpen = bindValue<boolean>(mod.id, INFO_LOOM_MENU_OPEN, false);
export const CommercialMenuOpen = bindValue<boolean>(mod.id, COMMERCIAL_MENU_OPEN, false);
export const IndustrialMenuOpen = bindValue<boolean>(mod.id, INDUSTRIAL_MENU_OPEN, false);
export const DistrictMenuOpen = bindValue<boolean>(mod.id, DISTRICT_MENU_OPEN, false);
export const ResidentialMenuOpen = bindValue<boolean>(mod.id, RESIDENTIAL_MENU_OPEN, false);
export const BuildingDemandOpen = bindValue<boolean>(mod.id, BUILDING_DEMAND_OPEN, false);
export const CommercialDemandOpen = bindValue<boolean>(mod.id, COMMERCIAL_DEMAND_OPEN, false);
export const CommercialProductsOpen = bindValue<boolean>(mod.id, COMMERCIAL_PRODUCTS_OPEN, false);
export const CommercialCompanyDebugOpen = bindValue<boolean>(
  mod.id,
  COMMERCIAL_COMPANY_DEBUG_OPEN,
  false
);
export const IndustrialCompanyDebugOpen = bindValue<boolean>(
  mod.id,
  INDUSTRIAL_COMPANY_DEBUG_OPEN,
  false
);
export const DemographicsOpen = bindValue<boolean>(mod.id, DEMOGRAPHICS_OPEN, false);
export const DemoStatsToggledOn = bindValue<boolean>(mod.id, 'DemoStatsToggledOn', false);
export const DemoAgeGroupingToggledOn = bindValue<boolean>(
  mod.id,
  'DemoAgeGroupingToggledOn',
  false
);
export const DemoGroupingStrategy = bindValue<GroupingStrategy>(
  mod.id,
  'DemoGroupingStrategy',
  GroupingStrategy.None
);
export const DistrictDataOpen = bindValue<boolean>(mod.id, DISTRICT_DATA_OPEN, false);
export const IndustrialDemandOpen = bindValue<boolean>(mod.id, INDUSTRIAL_DEMAND_OPEN, false);
export const IndustrialProductsOpen = bindValue<boolean>(mod.id, INDUSTRIAL_PRODUCTS_OPEN, false);
export const ResidentialDemandOpen = bindValue<boolean>(mod.id, RESIDENTIAL_DEMAND_OPEN, false);
export const TradeCostsOpen = bindValue<boolean>(mod.id, TRADE_COSTS_OPEN, false);
export const WorkforceOpen = bindValue<boolean>(mod.id, WORKFORCE_OPEN, false);
export const WorkplacesOpen = bindValue<boolean>(mod.id, WORKPLACES_OPEN, false);

export const CommercialData = bindValue<number[]>(mod.id, 'CommercialData');
export const CommercialDataExRes = bindValue<string[]>(mod.id, 'CommercialDataExRes', []);
export const CommercialProductsData = bindValue<CommercialProductData[]>(
  mod.id,
  'CommercialProductsData',
  []
);
export const CommercialCompanyDebugData = bindValue<CommercialCompanyDebug[]>(
  mod.id,
  'CommercialCompanyDebugData'
);
export const BuildingDemandData = bindValue<number[]>(mod.id, 'BuildingDemandData', []);
export const DemographicsDataDetails = bindValue<populationAtAge[]>(
  mod.id,
  'DemographicsDataDetails',
  []
);
export const DemographicsDataTotals = bindValue<number[]>(mod.id, 'DemographicsDataTotals', []);
export const DemographicsDataOldestCitizen = bindValue<number>(
  mod.id,
  'DemographicsDataOldestCitizen',
  0
);
export const DistrictData$ = bindValue<District[]>('InfoLoomTwo', 'DistrictData', []);
export const IndustrialData = bindValue<number[]>(mod.id, 'IndustrialData', []);
export const IndustrialDataExRes = bindValue<string[]>(mod.id, 'IndustrialDataExRes', []);
export const IndustrialProductsData = bindValue<industrialProductData[]>(
  mod.id,
  'IndustrialProductsData',
  []
);
export const IndustrialCompanyDebugData = bindValue<IndustrialCompanyDebug[]>(
  mod.id,
  'IndustrialCompanyDebugData',
  []
);
export const ResidentialData = bindValue<number[]>(mod.id, 'ResidentialData', []);
export const TradeCostsData = bindValue<ResourceTradeCost[]>(mod.id, 'TradeCostsData', []);
export const WorkforceData = bindValue<workforceInfo[]>(mod.id, 'WorkforceData', []);
export const WorkplacesData = bindValue<workplacesInfo[]>(mod.id, 'WorkplacesData', []);

export const SetInfoLoomMenuOpen = (open: boolean) => trigger(mod.id, INFO_LOOM_MENU_OPEN, open);
export const SetCommercialMenuOpen = (open: boolean) => trigger(mod.id, COMMERCIAL_MENU_OPEN, open);
export const SetIndustrialMenuOpen = (open: boolean) => trigger(mod.id, INDUSTRIAL_MENU_OPEN, open);
export const SetDistrictMenuOpen = (open: boolean) => trigger(mod.id, DISTRICT_MENU_OPEN, open);
export const SetResidentialMenuOpen = (open: boolean) =>
  trigger(mod.id, RESIDENTIAL_MENU_OPEN, open);
export const SetBuildingDemandOpen = (open: boolean) => trigger(mod.id, BUILDING_DEMAND_OPEN, open);
export const SetCommercialDemandOpen = (open: boolean) =>
  trigger(mod.id, COMMERCIAL_DEMAND_OPEN, open);
export const SetCommercialProductsOpen = (open: boolean) =>
  trigger(mod.id, COMMERCIAL_PRODUCTS_OPEN, open);
export const SetCommercialCompanyDebugOpen = (open: boolean) =>
  trigger(mod.id, COMMERCIAL_COMPANY_DEBUG_OPEN, open);
export const SetDemographicsOpen = (open: boolean) => trigger(mod.id, DEMOGRAPHICS_OPEN, open);
export const SetDemoStatsToggledOn = (on: boolean) => trigger(mod.id, 'DemoStatsToggledOn', on);
export const SetDistrictDataOpen = (open: boolean) => trigger(mod.id, DISTRICT_DATA_OPEN, open);
export const SetIndustrialDemandOpen = (open: boolean) =>
  trigger(mod.id, INDUSTRIAL_DEMAND_OPEN, open);
export const SetIndustrialProductsOpen = (open: boolean) =>
  trigger(mod.id, INDUSTRIAL_PRODUCTS_OPEN, open);
export const SetIndustrialCompanyDebugOpen = (open: boolean) =>
  trigger(mod.id, INDUSTRIAL_COMPANY_DEBUG_OPEN, open);
export const SetResidentialDemandOpen = (open: boolean) =>
  trigger(mod.id, RESIDENTIAL_DEMAND_OPEN, open);
export const SetTradeCostsOpen = (open: boolean) => trigger(mod.id, TRADE_COSTS_OPEN, open);
export const SetWorkforceOpen = (open: boolean) => trigger(mod.id, WORKFORCE_OPEN, open);
export const SetWorkplacesOpen = (open: boolean) => trigger(mod.id, WORKPLACES_OPEN, open);
export const SetDemoGroupingStrategy = (strategy: GroupingStrategy) =>
  trigger(mod.id, 'SetDemoGroupingStrategy', strategy);

//Commercial Company Data sorting
export const CommercialCompanyIndexSorting = bindValue<IndexSortingEnum>(
  mod.id,
  'CommercialIndexSorting',
  IndexSortingEnum.Off
);
export const SetCommercialCompanyIndexSorting = (sorting: IndexSortingEnum) =>
  trigger(mod.id, 'SetCommercialIndexSorting', sorting);
export const CommercialCompanyNameSorting = bindValue<CompanyNameEnum>(
  mod.id,
  'CommercialNameSorting',
  CompanyNameEnum.Off
);
export const SetCommercialCompanyNameSorting = (sorting: CompanyNameEnum) =>
  trigger(mod.id, 'SetCommercialNameSorting', sorting);
export const CommercialCompanyEfficiency = bindValue<EfficiencyEnum>(
  mod.id,
  'CommercialEfficiencySorting',
  EfficiencyEnum.Off
);
export const SetCommercialCompanyEfficiency = (sorting: EfficiencyEnum) =>
  trigger(mod.id, 'SetCommercialEfficiencySorting', sorting);
export const CommercialCompanyEmployee = bindValue<EmployeesEnum>(
  mod.id,
  'CommercialEmployeesSorting',
  EmployeesEnum.Off
);
export const SetCommercialCompanyEmployee = (sorting: EmployeesEnum) =>
  trigger(mod.id, 'SetCommercialEmployeesSorting', sorting);
export const CommercialCompanyProfitability = bindValue<ProfitabilityEnum>(
  mod.id,
  'CommercialProfitabilitySorting',
  ProfitabilityEnum.Off
);
export const SetCommercialCompanyProfitability = (sorting: ProfitabilityEnum) =>
  trigger(mod.id, 'SetCommercialProfitabilitySorting', sorting);
export const CommercialCompanyServiceUsage = bindValue<ServiceUsageEnum>(
  mod.id,
  'CommercialServiceUsageSorting',
  ServiceUsageEnum.Off
);
export const SetCommercialCompanyServiceUsage = (sorting: ServiceUsageEnum) =>
  trigger(mod.id, 'SetCommercialServiceUsageSorting', sorting);
export const CommercialCompannyResourceAmount = bindValue<ResourceAmountEnum>(
  mod.id,
  'CommercialResourceAmountSorting',
  ResourceAmountEnum.Off
);
export const SetCommercialCompannyResourceAmount = (sorting: ResourceAmountEnum) =>
  trigger(mod.id, 'SetCommercialResourceAmountSorting', sorting);

//Industrial Company Data sorting
export const IndustrialCompanyIndexSorting = bindValue<IndexSortingEnum2>(
  mod.id,
  'IndustrialIndexSorting',
  IndexSortingEnum2.Off
);
export const SetIndustrialCompanyIndexSorting = (sorting: IndexSortingEnum2) =>
  trigger(mod.id, 'SetIndustrialIndexSorting', sorting);
export const IndustrialCompanyNameSorting = bindValue<CompanyNameEnum2>(
  mod.id,
  'IndustrialNameSorting',
  CompanyNameEnum2.Off
);
export const SetIndustrialCompanyNameSorting = (sorting: CompanyNameEnum2) =>
  trigger(mod.id, 'SetIndustrialNameSorting', sorting);
export const IndustrialCompanyEfficiency = bindValue<EfficiencyEnum2>(
  mod.id,
  'IndustrialEfficiencySorting',
  EfficiencyEnum2.Off
);
export const SetIndustrialCompanyEfficiency = (sorting: EfficiencyEnum2) =>
  trigger(mod.id, 'SetIndustrialEfficiencySorting', sorting);
export const IndustrialCompanyEmployee = bindValue<EmployeesEnum2>(
  mod.id,
  'IndustrialEmployeesSorting',
  EmployeesEnum2.Off
);
export const SetIndustrialCompanyEmployee = (sorting: EmployeesEnum2) =>
  trigger(mod.id, 'SetIndustrialEmployeesSorting', sorting);
export const IndustrialCompanyProfitability = bindValue<ProfitabilityEnum2>(
  mod.id,
  'IndustrialProfitabilitySorting',
  ProfitabilityEnum2.Off
);
export const SetIndustrialCompanyProfitability = (sorting: ProfitabilityEnum2) =>
  trigger(mod.id, 'SetIndustrialProfitabilitySorting', sorting);
export const IndustrialCompanyResourceAmount = bindValue<ResourceAmountEnum2>(
  mod.id,
  'IndustrialResourceAmountSorting',
  ResourceAmountEnum2.Off
);
export const SetIndustrialCompanyResourceAmount = (sorting: ResourceAmountEnum2) =>
  trigger(mod.id, 'SetIndustrialResourceAmountSorting', sorting);

//Trade Costs Data sorting
export const BuyCostSorting = bindValue<BuyCostEnum>(mod.id, 'BuyCost', BuyCostEnum.Off);
export const SetBuyCostSorting = (sorting: BuyCostEnum) => trigger(mod.id, 'SetBuyCost', sorting);
export const SellCostSorting = bindValue<SellCostEnum>(mod.id, 'SellCost', SellCostEnum.Off);
export const SetSellCostSorting = (sorting: SellCostEnum) =>
  trigger(mod.id, 'SetSellCost', sorting);
export const ExportAmountSorting = bindValue<ExportAmountEnum>(
  mod.id,
  'ExportAmount',
  ExportAmountEnum.Off
);
export const SetExportAmountSorting = (sorting: ExportAmountEnum) =>
  trigger(mod.id, 'SetExportAmount', sorting);
export const ImportAmountSorting = bindValue<ImportAmountEnum>(
  mod.id,
  'ImportAmount',
  ImportAmountEnum.Off
);
export const SetImportAmountSorting = (sorting: ImportAmountEnum) =>
  trigger(mod.id, 'SetImportAmount', sorting);
export const ProfitSorting = bindValue<ProfitEnum>(mod.id, 'Profit', ProfitEnum.Off);
export const SetProfitSorting = (sorting: ProfitEnum) => trigger(mod.id, 'SetProfit', sorting);
export const ProfitMarginSorting = bindValue<ProfitMarginEnum>(
  mod.id,
  'ProfitMargin',
  ProfitMarginEnum.Off
);
export const SetProfitMarginSorting = (sorting: ProfitMarginEnum) =>
  trigger(mod.id, 'SetProfitMargin', sorting);
export const ResourceNameSorting = bindValue<ResourceNameEnum>(
  mod.id,
  'ResourceName',
  ResourceNameEnum.Off
);
export const SetResourceNameSorting = (sorting: ResourceNameEnum) =>
  trigger(mod.id, 'SetResourceName', sorting);


export const HistoricalData = bindValue<number[]>(mod.id, 'ResourceHistoricalData', []);
export const SetHistoricalData = (resourceName: string) => trigger(mod.id, 'GetResourceHistoricalData', resourceName);

