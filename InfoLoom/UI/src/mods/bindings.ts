import { bindLocalValue, bindValue, trigger } from 'cs2/api';
import mod from 'mod.json';
import { workforceInfo } from './domain/workforceInfo';
import { workplacesInfo } from './domain/WorkplacesInfo';
import { GroupingStrategy } from './domain/GroupingStrategy';
import { CommercialCompanyDebug } from './domain/CommercialCompanyDebugData';
import { IndustrialCompanyDebug } from './domain/IndustrialCompanyDebugData';
import { PopulationDetailedGroupInfo } from './domain/populationDetailedGroupInfo';
import { PopulationFiveYearGroupInfo } from './domain/populationFiveYearGroupInfo';
import { PopulationTenYearGroupInfo } from './domain/populationTenYearGroupInfo';
import { PopulationLifecycleInfo } from './domain/populationLifecycleInfo';
import { ResourceTradeCost } from 'mods/domain/tradeCostData';
import { StorageCompanyInfo } from './domain/StorageCompanyInfo';
import { EntityModifierData } from './domain/EffectsData';
import { getModule } from 'cs2/modding';
import { OneWayBinding } from 'utils/onewayBinding';
import TriggerBuilder from 'utils/trigger';
import { Theme } from 'cs2/bindings';
import { TwoWayBinding } from 'utils/bidirectionalBinding';
import { SortingEnum } from 'mods/domain/SortingEnum';
import { TCSortingEnum } from './domain/TradeCostEnums';

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
const BUDGET_SANKEY_OPEN = 'BudgetSankeyOpen';
const WORKFORCE_PIPELINE_SANKEY_OPEN = 'WorkforcePipelineSankeyOpen';

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
export const DemoGroupingStrategy = new OneWayBinding<GroupingStrategy>('DemoGroupingStrategy', GroupingStrategy.None);
export const IndustrialDemandOpen = bindValue<boolean>(mod.id, INDUSTRIAL_DEMAND_OPEN, false);
export const IndustrialProductsOpen = bindValue<boolean>(mod.id, INDUSTRIAL_PRODUCTS_OPEN, false);
export const ResidentialDemandOpen = bindValue<boolean>(mod.id, RESIDENTIAL_DEMAND_OPEN, false);
export const TradeCostsOpen = bindValue<boolean>(mod.id, TRADE_COSTS_OPEN, false);
export const WorkforceOpen = bindValue<boolean>(mod.id, WORKFORCE_OPEN, false);
export const WorkplacesOpen = bindValue<boolean>(mod.id, WORKPLACES_OPEN, false);
export const BudgetSankeyOpen = bindValue<boolean>(mod.id, BUDGET_SANKEY_OPEN, false);
export const WorkforcePipelineSankeyOpen = bindValue<boolean>(mod.id, WORKFORCE_PIPELINE_SANKEY_OPEN, false);
export const SankeyMenuOpen = bindValue<boolean>(mod.id, SANKEY_MENU_OPEN, false);

export const CommercialData = new OneWayBinding<number[]>('CommercialData', []);
export const CommercialDataExRes = new OneWayBinding<string[]>('CommercialDataExRes', []);

export const CommercialCompanyDebugData = new OneWayBinding<CommercialCompanyDebug[]>('CommercialCompanyDebugData');

export const BuildingDemandData = new OneWayBinding<number[]>('BuildingDemandData', []);
export const DemographicsDataTotals = new OneWayBinding<number[]>('DemographicsDataTotals', []);
export const DemographicsDataOldestCitizen = bindValue<number>(mod.id, 'DemographicsDataOldestCitizen', 0);
export const IndustrialData = new OneWayBinding<number[]>('IndustrialData', []);
export const IndustrialDataExRes = new OneWayBinding<string[]>('IndustrialDataExRes', []);
export const IndustrialCompanyDebugData = new OneWayBinding<IndustrialCompanyDebug[]>('IndustrialCompanyDebugData', []);
export const ResidentialData = new OneWayBinding<number[]>('ResidentialData', []);
export const TradeCostsData = new OneWayBinding<ResourceTradeCost[]>('TradeCostsData', []);

export const WorkforceData = new OneWayBinding<workforceInfo[]>('WorkforceData', []);
export const WorkplacesData = new OneWayBinding<workplacesInfo[]>('WorkplacesData', []);

export const SetInfoLoomMenuOpen = (open: boolean) => trigger(mod.id, INFO_LOOM_MENU_OPEN, open);
export const SetCommercialMenuOpen = (open: boolean) => trigger(mod.id, COMMERCIAL_MENU_OPEN, open);
export const SetIndustrialMenuOpen = (open: boolean) => trigger(mod.id, INDUSTRIAL_MENU_OPEN, open);
export const SetDistrictMenuOpen = (open: boolean) => trigger(mod.id, DISTRICT_MENU_OPEN, open);
export const SetResidentialMenuOpen = (open: boolean) => trigger(mod.id, RESIDENTIAL_MENU_OPEN, open);
export const SetBuildingDemandOpen = (open: boolean) => trigger(mod.id, BUILDING_DEMAND_OPEN, open);
export const SetCommercialDemandOpen = (open: boolean) => trigger(mod.id, COMMERCIAL_DEMAND_OPEN, open);
export const SetCommercialCompanyDebugOpen = (open: boolean) => trigger(mod.id, COMMERCIAL_COMPANY_DEBUG_OPEN, open);
export const SetDemographicsOpen = (open: boolean) => trigger(mod.id, DEMOGRAPHICS_OPEN, open);
export const SetDemoStatsToggledOn = (on: boolean) => trigger(mod.id, 'DemoStatsToggledOn', on);
export const SetIndustrialDemandOpen = (open: boolean) => trigger(mod.id, INDUSTRIAL_DEMAND_OPEN, open);
export const SetIndustrialCompanyDebugOpen = (open: boolean) => trigger(mod.id, INDUSTRIAL_COMPANY_DEBUG_OPEN, open);
export const SetResidentialDemandOpen = (open: boolean) => trigger(mod.id, RESIDENTIAL_DEMAND_OPEN, open);
export const SetTradeCostsOpen = (open: boolean) => trigger(mod.id, TRADE_COSTS_OPEN, open);
export const SetWorkforceOpen = (open: boolean) => trigger(mod.id, WORKFORCE_OPEN, open);
export const SetWorkplacesOpen = (open: boolean) => trigger(mod.id, WORKPLACES_OPEN, open);
export const SetBudgetSankeyOpen = (open: boolean) => trigger(mod.id, BUDGET_SANKEY_OPEN, open);
export const SetWorkforcePipelineSankeyOpen = (open: boolean) => trigger(mod.id, WORKFORCE_PIPELINE_SANKEY_OPEN, open);
export const SetSankeyMenuOpen = (open: boolean) => trigger(mod.id, SANKEY_MENU_OPEN, open);

export const SetDemoGroupingStrategy = TriggerBuilder.create<[GroupingStrategy]>('SetDemoGroupingStrategy');

//Commercial Company Data sorting
export const COMMERCIAL = {
  Name: new TwoWayBinding<SortingEnum>('CommercialNameSorting', SortingEnum.Off),
  Efficiency: new TwoWayBinding<SortingEnum>('CommercialEfficiencySorting', SortingEnum.Off),
  Employees: new TwoWayBinding<SortingEnum>('CommercialEmployeesSorting', SortingEnum.Off),
  Profitability: new TwoWayBinding<SortingEnum>('CommercialProfitabilitySorting', SortingEnum.Off),
  ServiceUsage: new TwoWayBinding<SortingEnum>('CommercialServiceUsageSorting', SortingEnum.Off),
  Money: new TwoWayBinding<SortingEnum>('CommercialMoneySorting', SortingEnum.Off),
  Input1: new TwoWayBinding<SortingEnum>('CommercialInput1Sorting', SortingEnum.Off),
  Output: new TwoWayBinding<SortingEnum>('CommercialOutputSorting', SortingEnum.Off),
  Maintenance: new TwoWayBinding<SortingEnum>('CommercialMaintenanceSorting', SortingEnum.Off),
};
export const INDUSTRIAL = {
  Name: new TwoWayBinding<SortingEnum>('IndustrialNameSorting', SortingEnum.Off),
  Efficiency: new TwoWayBinding<SortingEnum>('IndustrialEfficiencySorting', SortingEnum.Off),
  Employees: new TwoWayBinding<SortingEnum>('IndustrialEmployeesSorting', SortingEnum.Off),
  Profitability: new TwoWayBinding<SortingEnum>('IndustrialProfitabilitySorting', SortingEnum.Off),
  Money: new TwoWayBinding<SortingEnum>('IndustrialMoneySorting', SortingEnum.Off),
  Input1: new TwoWayBinding<SortingEnum>('IndustrialInput1Sorting', SortingEnum.Off),
  Input2: new TwoWayBinding<SortingEnum>('IndustrialInput2Sorting', SortingEnum.Off),
  Output: new TwoWayBinding<SortingEnum>('IndustrialOutputSorting', SortingEnum.Off),
  Maintenance: new TwoWayBinding<SortingEnum>('IndustrialMaintenanceSorting', SortingEnum.Off),
};

//Industrial Company Data sorting

//Trade Costs Data sorting
export const TC = {
  ResourceName: new TwoWayBinding<TCSortingEnum>('ResourceName', TCSortingEnum.Off),
  BuyCost: new TwoWayBinding<TCSortingEnum>('BuyCost', TCSortingEnum.Off),
  SellCost: new TwoWayBinding<TCSortingEnum>('SellCost', TCSortingEnum.Off),
  ExportAmount: new TwoWayBinding<TCSortingEnum>('ExportAmount', TCSortingEnum.Off),
  ImportAmount: new TwoWayBinding<TCSortingEnum>('ImportAmount', TCSortingEnum.Off),
  Profit: new TwoWayBinding<TCSortingEnum>('Profit', TCSortingEnum.Off),
  ProfitMargin: new TwoWayBinding<TCSortingEnum>('ProfitMargin', TCSortingEnum.Off),
};

export const DemographicsDetailedData = new OneWayBinding<PopulationDetailedGroupInfo[]>(
  'DemographicsDetailedData',
  []
);
export const DemographicsLifecycleDetails = new OneWayBinding<PopulationLifecycleInfo[]>(
  'DemographicsLifecycleDetails',
  []
);
export const DemographicsFiveYearDetails = new OneWayBinding<PopulationFiveYearGroupInfo[]>(
  'DemographicsFiveYearDetails',
  []
);
export const DemographicsTenYearDetails = new OneWayBinding<PopulationTenYearGroupInfo[]>(
  'DemographicsTenYearDetails',
  []
);

export const StorageCompaniesBinding = new OneWayBinding<StorageCompanyInfo[]>('StorageCompanies', []);
export const storagePanelVisibleBinding = new OneWayBinding<boolean>('StoragePanelVisible', false);
export const SetStoragePanelVisible = TriggerBuilder.create<[boolean]>('SetStoragePanelVisible');

export const EffectCountBinding = new OneWayBinding<number[]>('EffectCount', [0, 0, 0]);
export const EffectsBinding = new OneWayBinding<EntityModifierData[]>('Effects', []);
export const ShowEffectsButton = bindValue<boolean>(mod.id, 'showButton', true);
export const EffectsOpen = bindValue<boolean>(mod.id, 'EffectsOpen', false);
export const SetEffectsOpen = (open: boolean) => trigger(mod.id, 'EffectsOpen', open);
export const OverlayEffects = bindValue<string[]>(mod.id, 'OverlayEffects', []);
export const ToggleOverlay = TriggerBuilder.create<[string]>('ToggleOverlay');
export interface EffectColorInfo {
  Type: string;
  R: number;
  G: number;
  B: number;
  A: number;
}
export const EffectColors = new OneWayBinding<EffectColorInfo[]>('EffectColors', []);
export const ChangeEffectColor = TriggerBuilder.create<[string]>('ChangeEffectColor');

export const minimizedBinding = bindLocalValue(false);
export const toggleMinimize = () => minimizedBinding.update(!minimizedBinding.value);

export const workplacesMinimizedBinding = bindLocalValue(false);
export const toggleWorkplacesMinimize = () => workplacesMinimizedBinding.update(!workplacesMinimizedBinding.value);

export const Divider: any = getModule('game-ui/editor/widgets/divider/divider.tsx', 'Divider');
export const resourceBox = getModule(
  'game-ui/game/components/selected-info-panel/shared-components/resource-item/resource-item.module.scss',
  'classes'
);

export const storageBox = getModule(
  'game-ui/game/components/selected-info-panel/selected-info-sections/building-sections/storage-section/storage-section.module.scss',
  'classes'
);
export const InfoRowTheme: Theme | any = getModule(
  'game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.module.scss',
  'classes'
);
