import { bindValue, trigger } from 'cs2/api';
import mod from 'mod.json';
import { District } from './domain/District';
import { workforceInfo } from './domain/workforceInfo';
import { workplacesInfo } from './domain/WorkplacesInfo';
import { GroupingStrategy } from './domain/GroupingStrategy';
import { CommercialCompanyDebug } from './domain/CommercialCompanyDebugData';
import { IndustrialCompanyDebug } from './domain/IndustrialCompanyDebugData';
import { PopulationDetailedGroupInfo } from './domain/populationDetailedGroupInfo';
import { PopulationFiveYearGroupInfo } from './domain/populationFiveYearGroupInfo';
import { PopulationTenYearGroupInfo } from './domain/populationTenYearGroupInfo';
import { PopulationLifecycleInfo } from './domain/populationLifecycleInfo';
import { SortingEnum, OutsideConnectionType } from 'mods/domain/TradeCostEnums';
import { Demographics1, Demographics2 } from 'mods/domain/DemographicsEnums';
import { ResourceTradeCost } from 'mods/domain/tradeCostData';
import { StorageCompanyInfo } from './domain/StorageCompanyInfo';
import {EntityModifierData} from "./domain/EffectsData";
import { BudgetSankeyData } from './domain/budgetSankeyData';

const INFO_LOOM_MENU_OPEN = 'InfoLoomMenuOpen';
const COMMERCIAL_MENU_OPEN = 'CommercialMenuOpen';
const INDUSTRIAL_MENU_OPEN = 'IndustrialMenuOpen';
const DISTRICT_MENU_OPEN = 'DistrictMenuOpen';
const RESIDENTIAL_MENU_OPEN = 'ResidentialMenuOpen';
const SANKEY_MENU_OPEN = 'SankeyMenuOpen';
const BUILDING_DEMAND_OPEN = 'BuildingDemandOpen';
const COMMERCIAL_DEMAND_OPEN = 'CommercialDemandOpen';
const COMMERCIAL_PRODUCTS_OPEN = 'CommercialProductsOpen';
const DEMOGRAPHICS_OPEN = 'DemographicsOpen';
const INDUSTRIAL_DEMAND_OPEN = 'IndustrialDemandOpen';
const INDUSTRIAL_PRODUCTS_OPEN = 'IndustrialProductsOpen';
const RESIDENTIAL_DEMAND_OPEN = 'ResidentialDemandOpen';
const TRADE_COSTS_OPEN = 'TradeCostsOpen';
const WORKFORCE_OPEN = 'WorkforceOpen';
const WORKPLACES_OPEN = 'WorkplacesOpen';
const COMMERCIAL_COMPANY_DEBUG_OPEN = 'CommercialCompanyDebugOpen';
const INDUSTRIAL_COMPANY_DEBUG_OPEN = 'IndustrialCompanyDebugOpen';
const HOUSEHOLDS_DATA_OPEN = 'HouseholdsDataOpen';
const BUDGET_SANKEY_OPEN = 'BudgetSankeyOpen';
const WORKFORCE_PIPELINE_SANKEY_OPEN = 'WorkforcePipelineSankeyOpen';
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
export const RESIDENTIAL_DATA = 'ResidentialData';
export const TRADE_COSTS_DATA = 'TradeCostsData';
export const TRADE_COSTS_DATA_IMPORTS = 'TradeCostsDataImports';
export const TRADE_COSTS_DATA_EXPORTS = 'TradeCostsDataExports';
export const WORKFORCE_DATA = 'WorkforceData';
export const WORKPLACES_DATA = 'WorkplacesData';

export const InfoLoomMenuOpen = bindValue<boolean>(mod.id, INFO_LOOM_MENU_OPEN, false);
export const CommercialMenuOpen = bindValue<boolean>(mod.id, COMMERCIAL_MENU_OPEN, false);
export const IndustrialMenuOpen = bindValue<boolean>(mod.id, INDUSTRIAL_MENU_OPEN, false);
export const ResidentialMenuOpen = bindValue<boolean>(mod.id, RESIDENTIAL_MENU_OPEN, false);
export const BuildingDemandOpen = bindValue<boolean>(mod.id, BUILDING_DEMAND_OPEN, false);
export const CommercialDemandOpen = bindValue<boolean>(mod.id, COMMERCIAL_DEMAND_OPEN, false);
export const CommercialProductsOpen = bindValue<boolean>(mod.id, COMMERCIAL_PRODUCTS_OPEN, false);
export const CommercialCompanyDebugOpen = bindValue<boolean>(mod.id, COMMERCIAL_COMPANY_DEBUG_OPEN, false);
export const IndustrialCompanyDebugOpen = bindValue<boolean>(mod.id, INDUSTRIAL_COMPANY_DEBUG_OPEN, false);
export const DemographicsOpen = bindValue<boolean>(mod.id, DEMOGRAPHICS_OPEN, false);
export const DemoStatsToggledOn = bindValue<boolean>(mod.id, 'DemoStatsToggledOn', false);
export const DemoAgeGroupingToggledOn = bindValue<boolean>(mod.id, 'DemoAgeGroupingToggledOn', false);
export const DemoGroupingStrategy = bindValue<GroupingStrategy>(mod.id, 'DemoGroupingStrategy', GroupingStrategy.None);
export const IndustrialDemandOpen = bindValue<boolean>(mod.id, INDUSTRIAL_DEMAND_OPEN, false);
export const IndustrialProductsOpen = bindValue<boolean>(mod.id, INDUSTRIAL_PRODUCTS_OPEN, false);
export const ResidentialDemandOpen = bindValue<boolean>(mod.id, RESIDENTIAL_DEMAND_OPEN, false);
export const TradeCostsOpen = bindValue<boolean>(mod.id, TRADE_COSTS_OPEN, false);
export const WorkforceOpen = bindValue<boolean>(mod.id, WORKFORCE_OPEN, false);
export const WorkplacesOpen = bindValue<boolean>(mod.id, WORKPLACES_OPEN, false);
export const BudgetSankeyOpen = bindValue<boolean>(mod.id, BUDGET_SANKEY_OPEN, false);
export const WorkforcePipelineSankeyOpen = bindValue<boolean>(mod.id, WORKFORCE_PIPELINE_SANKEY_OPEN, false);
export const SankeyMenuOpen = bindValue<boolean>(mod.id, SANKEY_MENU_OPEN, false);

export const CommercialData = bindValue<number[]>(mod.id, 'CommercialData');
export const CommercialDataExRes = bindValue<string[]>(mod.id, 'CommercialDataExRes', []);
export const CommercialCompanyDebugData = bindValue<CommercialCompanyDebug[]>(mod.id, 'CommercialCompanyDebugData');
export const BuildingDemandData = bindValue<number[]>(mod.id, 'BuildingDemandData', []);
export const DemographicsDataTotals = bindValue<number[]>(mod.id, 'DemographicsDataTotals', []);
export const DemographicsDataOldestCitizen = bindValue<number>(mod.id, 'DemographicsDataOldestCitizen', 0);
export const DistrictData$ = bindValue<District[]>('InfoLoomTwo', 'DistrictData');
export const IndustrialData = bindValue<number[]>(mod.id, 'IndustrialData', []);
export const IndustrialDataExRes = bindValue<string[]>(mod.id, 'IndustrialDataExRes', []);
export const IndustrialCompanyDebugData = bindValue<IndustrialCompanyDebug[]>(mod.id, 'IndustrialCompanyDebugData', []);
export const ResidentialData = bindValue<number[]>(mod.id, 'ResidentialData', []);
export const TradeCostsData = bindValue<ResourceTradeCost[]>(mod.id, 'TradeCostsData', []);

export const WorkforceData = bindValue<workforceInfo[]>(mod.id, 'WorkforceData', []);
export const WorkplacesData = bindValue<workplacesInfo[]>(mod.id, 'WorkplacesData', []);

export const BudgetSankeyData$ = bindValue<BudgetSankeyData>(mod.id, 'budgetSankeyData');

export const SetInfoLoomMenuOpen = (open: boolean) => trigger(mod.id, INFO_LOOM_MENU_OPEN, open);
export const SetCommercialMenuOpen = (open: boolean) => trigger(mod.id, COMMERCIAL_MENU_OPEN, open);
export const SetIndustrialMenuOpen = (open: boolean) => trigger(mod.id, INDUSTRIAL_MENU_OPEN, open);
export const SetDistrictMenuOpen = (open: boolean) => trigger(mod.id, DISTRICT_MENU_OPEN, open);
export const SetResidentialMenuOpen = (open: boolean) => trigger(mod.id, RESIDENTIAL_MENU_OPEN, open);
export const SetBuildingDemandOpen = (open: boolean) => trigger(mod.id, BUILDING_DEMAND_OPEN, open);
export const SetCommercialDemandOpen = (open: boolean) => trigger(mod.id, COMMERCIAL_DEMAND_OPEN, open);
export const SetCommercialProductsOpen = (open: boolean) => trigger(mod.id, COMMERCIAL_PRODUCTS_OPEN, open);
export const SetCommercialCompanyDebugOpen = (open: boolean) => trigger(mod.id, COMMERCIAL_COMPANY_DEBUG_OPEN, open);
export const SetDemographicsOpen = (open: boolean) => trigger(mod.id, DEMOGRAPHICS_OPEN, open);
export const SetDemoStatsToggledOn = (on: boolean) => trigger(mod.id, 'DemoStatsToggledOn', on);
export const SetIndustrialDemandOpen = (open: boolean) => trigger(mod.id, INDUSTRIAL_DEMAND_OPEN, open);
export const SetIndustrialProductsOpen = (open: boolean) => trigger(mod.id, INDUSTRIAL_PRODUCTS_OPEN, open);
export const SetIndustrialCompanyDebugOpen = (open: boolean) => trigger(mod.id, INDUSTRIAL_COMPANY_DEBUG_OPEN, open);
export const SetResidentialDemandOpen = (open: boolean) => trigger(mod.id, RESIDENTIAL_DEMAND_OPEN, open);
export const SetTradeCostsOpen = (open: boolean) => trigger(mod.id, TRADE_COSTS_OPEN, open);
export const SetWorkforceOpen = (open: boolean) => trigger(mod.id, WORKFORCE_OPEN, open);
export const SetWorkplacesOpen = (open: boolean) => trigger(mod.id, WORKPLACES_OPEN, open);
export const SetBudgetSankeyOpen = (open: boolean) => trigger(mod.id, BUDGET_SANKEY_OPEN, open);
export const SetWorkforcePipelineSankeyOpen = (open: boolean) => trigger(mod.id, WORKFORCE_PIPELINE_SANKEY_OPEN, open);
export const SetSankeyMenuOpen = (open: boolean) => trigger(mod.id, SANKEY_MENU_OPEN, open);

export const SetDemoGroupingStrategy = (strategy: GroupingStrategy) =>
  trigger(mod.id, 'SetDemoGroupingStrategy', strategy);

//Commercial Company Data sorting
export const CommercialCompanyIndexSorting = bindValue<SortingEnum>(mod.id, 'CommercialIndexSorting', SortingEnum.Off);
export const SetCommercialCompanyIndexSorting = (sorting: SortingEnum) =>
  trigger(mod.id, 'SetCommercialIndexSorting', sorting);
export const CommercialCompanyNameSorting = bindValue<SortingEnum>(mod.id, 'CommercialNameSorting', SortingEnum.Off);
export const SetCommercialCompanyNameSorting = (sorting: SortingEnum) =>
  trigger(mod.id, 'SetCommercialNameSorting', sorting);
export const CommercialCompanyEfficiency = bindValue<SortingEnum>(
  mod.id,
  'CommercialEfficiencySorting',
  SortingEnum.Off
);
export const SetCommercialCompanyEfficiency = (sorting: SortingEnum) =>
  trigger(mod.id, 'SetCommercialEfficiencySorting', sorting);
export const CommercialCompanyEmployee = bindValue<SortingEnum>(mod.id, 'CommercialEmployeesSorting', SortingEnum.Off);
export const SetCommercialCompanyEmployee = (sorting: SortingEnum) =>
  trigger(mod.id, 'SetCommercialEmployeesSorting', sorting);
export const CommercialCompanyProfitability = bindValue<SortingEnum>(
  mod.id,
  'CommercialProfitabilitySorting',
  SortingEnum.Off
);
export const SetCommercialCompanyProfitability = (sorting: SortingEnum) =>
  trigger(mod.id, 'SetCommercialProfitabilitySorting', sorting);
export const CommercialCompanyServiceUsage = bindValue<SortingEnum>(
  mod.id,
  'CommercialServiceUsageSorting',
  SortingEnum.Off
);
export const SetCommercialCompanyServiceUsage = (sorting: SortingEnum) =>
  trigger(mod.id, 'SetCommercialServiceUsageSorting', sorting);
export const CommercialMoneySorting = bindValue<SortingEnum>(mod.id, 'CommercialMoneySorting', SortingEnum.Off);
export const SetCommercialMoneySorting = (sorting: SortingEnum) =>
  trigger(mod.id, 'SetCommercialMoneySorting', sorting);
export const CommercialInput1Sorting = bindValue<SortingEnum>(mod.id, 'CommercialInput1Sorting', SortingEnum.Off);
export const SetCommercialInput1Sorting = (sorting: SortingEnum) =>
  trigger(mod.id, 'SetCommercialInput1Sorting', sorting);
export const CommercialOutputSorting = bindValue<SortingEnum>(mod.id, 'CommercialOutputSorting', SortingEnum.Off);
export const SetCommercialOutputSorting = (sorting: SortingEnum) =>
  trigger(mod.id, 'SetCommercialOutputSorting', sorting);
export const CommercialMaintenanceSorting = bindValue<SortingEnum>(
  mod.id,
  'CommercialMaintenanceSorting',
  SortingEnum.Off
);
export const SetCommercialMaintenanceSorting = (sorting: SortingEnum) =>
  trigger(mod.id, 'SetCommercialMaintenanceSorting', sorting);

//Industrial Company Data sorting
export const IndustrialCompanyIndexSorting = bindValue<SortingEnum>(mod.id, 'IndustrialIndexSorting', SortingEnum.Off);
export const SetIndustrialCompanyIndexSorting = (sorting: SortingEnum) =>
  trigger(mod.id, 'SetIndustrialIndexSorting', sorting);
export const IndustrialCompanyNameSorting = bindValue<SortingEnum>(mod.id, 'IndustrialNameSorting', SortingEnum.Off);
export const SetIndustrialCompanyNameSorting = (sorting: SortingEnum) =>
  trigger(mod.id, 'SetIndustrialNameSorting', sorting);
export const IndustrialCompanyEfficiency = bindValue<SortingEnum>(
  mod.id,
  'IndustrialEfficiencySorting',
  SortingEnum.Off
);
export const SetIndustrialCompanyEfficiency = (sorting: SortingEnum) =>
  trigger(mod.id, 'SetIndustrialEfficiencySorting', sorting);
export const IndustrialCompanyEmployee = bindValue<SortingEnum>(mod.id, 'IndustrialEmployeesSorting', SortingEnum.Off);
export const SetIndustrialCompanyEmployee = (sorting: SortingEnum) =>
  trigger(mod.id, 'SetIndustrialEmployeesSorting', sorting);
export const IndustrialCompanyProfitability = bindValue<SortingEnum>(
  mod.id,
  'IndustrialProfitabilitySorting',
  SortingEnum.Off
);
export const SetIndustrialCompanyProfitability = (sorting: SortingEnum) =>
  trigger(mod.id, 'SetIndustrialProfitabilitySorting', sorting);
export const IndustrialMoneySorting = bindValue<SortingEnum>(mod.id, 'IndustrialMoneySorting', SortingEnum.Off);
export const SetIndustrialMoneySorting = (sorting: SortingEnum) =>
  trigger(mod.id, 'SetIndustrialMoneySorting', sorting);
export const IndustrialInput1Sorting = bindValue<SortingEnum>(mod.id, 'IndustrialInput1Sorting', SortingEnum.Off);
export const SetIndustrialInput1Sorting = (sorting: SortingEnum) =>
  trigger(mod.id, 'SetIndustrialInput1Sorting', sorting);
export const IndustrialInput2Sorting = bindValue<SortingEnum>(mod.id, 'IndustrialInput2Sorting', SortingEnum.Off);
export const SetIndustrialInput2Sorting = (sorting: SortingEnum) =>
  trigger(mod.id, 'SetIndustrialInput2Sorting', sorting);
export const IndustrialOutputSorting = bindValue<SortingEnum>(mod.id, 'IndustrialOutputSorting', SortingEnum.Off);
export const SetIndustrialOutputSorting = (sorting: SortingEnum) =>
  trigger(mod.id, 'SetIndustrialOutputSorting', sorting);
export const IndustrialMaintenanceSorting = bindValue<SortingEnum>(
  mod.id,
  'IndustrialMaintenanceSorting',
  SortingEnum.Off
);
export const SetIndustrialMaintenanceSorting = (sorting: SortingEnum) =>
  trigger(mod.id, 'SetIndustrialMaintenanceSorting', sorting);

//Trade Costs Data sorting
export const BuyCostSorting = bindValue<SortingEnum>(mod.id, 'BuyCost', SortingEnum.Off);
export const SetBuyCostSorting = (sorting: SortingEnum) => trigger(mod.id, 'SetBuyCost', sorting);
export const SellCostSorting = bindValue<SortingEnum>(mod.id, 'SellCost', SortingEnum.Off);
export const SetSellCostSorting = (sorting: SortingEnum) => trigger(mod.id, 'SetSellCost', sorting);
export const ExportAmountSorting = bindValue<SortingEnum>(mod.id, 'ExportAmount', SortingEnum.Off);
export const SetExportAmountSorting = (sorting: SortingEnum) => trigger(mod.id, 'SetExportAmount', sorting);
export const ImportAmountSorting = bindValue<SortingEnum>(mod.id, 'ImportAmount', SortingEnum.Off);
export const SetImportAmountSorting = (sorting: SortingEnum) => trigger(mod.id, 'SetImportAmount', sorting);
export const ProfitSorting = bindValue<SortingEnum>(mod.id, 'Profit', SortingEnum.Off);
export const SetProfitSorting = (sorting: SortingEnum) => trigger(mod.id, 'SetProfit', sorting);
export const ProfitMarginSorting = bindValue<SortingEnum>(mod.id, 'ProfitMargin', SortingEnum.Off);
export const SetProfitMarginSorting = (sorting: SortingEnum) => trigger(mod.id, 'SetProfitMargin', sorting);
export const ResourceNameSorting = bindValue<SortingEnum>(mod.id, 'ResourceName', SortingEnum.Off);
export const SetResourceNameSorting = (sorting: SortingEnum) => trigger(mod.id, 'SetResourceName', sorting);

export const DemographicsOne = bindValue<Demographics1>(mod.id, 'Demographics1', Demographics1.All);
export const SetDemographicsOne = (demographics: Demographics1) => trigger(mod.id, 'SetDemographics1', demographics);
export const DemographicsTwo = bindValue<Demographics2>(mod.id, 'Demographics2', Demographics2.All);
export const SetDemographicsTwo = (demographics: Demographics2) => trigger(mod.id, 'SetDemographics2', demographics);
export const DemographicsLifecycleTotals = bindValue<number[]>(
  'InfoLoomTwo',
  'DemographicsLifecycleTotals',
  [0, 0, 0, 0]
);

export const DemographicsDetailedData = bindValue<PopulationDetailedGroupInfo[]>(
  'InfoLoomTwo',
  'DemographicsDetailedData',
  []
);

export const DemographicsLifecycleDetails = bindValue<PopulationLifecycleInfo[]>(
  'InfoLoomTwo',
  'DemographicsLifecycleDetails',
  []
);

export const DemographicsFiveYearDetails = bindValue<PopulationFiveYearGroupInfo[]>(
  'InfoLoomTwo',
  'DemographicsFiveYearDetails',
  []
);

export const DemographicsTenYearDetails = bindValue<PopulationTenYearGroupInfo[]>(
  'InfoLoomTwo',
  'DemographicsTenYearDetails',
  []
);

export const StorageCompaniesBinding = bindValue<StorageCompanyInfo[]>(mod.id, 'StorageCompanies', []);
export const storagePanelVisibleBinding = bindValue<boolean>(mod.id, 'StoragePanelVisible', false);
export const SetStoragePanelVisible = (visible: boolean) => trigger(mod.id, 'SetStoragePanelVisible', visible);

export const EffectCountBinding = bindValue<number[]>(mod.id, 'EffectCount', [0, 0, 0]);
export const EffectsBinding = bindValue<EntityModifierData[]>(mod.id, 'Effects', []);
export const ShowEffectsButton = bindValue<boolean>(mod.id, 'showButton', true);
export const EffectsOpen = bindValue<boolean>(mod.id, 'EffectsOpen', false);
export const SetEffectsOpen = (open: boolean) => trigger(mod.id, 'EffectsOpen', open);
export const OverlayEffects = bindValue<string[]>(mod.id, 'OverlayEffects', []);
export const ToggleOverlay = (key: string) => trigger(mod.id, 'ToggleOverlay', key);
export interface EffectColorInfo { Type: string; R: number; G: number; B: number; A: number; }
export const EffectColors = bindValue<EffectColorInfo[]>(mod.id, 'EffectColors', []);
export const ChangeEffectColor = (packed: string) => trigger(mod.id, 'ChangeEffectColor', packed);
