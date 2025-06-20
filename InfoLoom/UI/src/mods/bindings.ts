import {bindValue, trigger} from "cs2/api";
import mod from "mod.json"
import {CommercialProductData} from "./domain/commercialProductData";
import {populationAtAge} from "./domain/populationAtAge";
import {District} from "./domain/District";
import {industrialProductData} from "./domain/industrialProductData";
import {ResourceTradeCost} from "./domain/tradeCostData";
import {workforceInfo} from "./domain/workforceInfo";
import {workplacesInfo} from "./domain/WorkplacesInfo";
import {GroupingStrategy} from "./domain/GroupingStrategy";
import { CommercialCompanyDebug } from "./domain/CommercialCompanyDebugData";
import {IndustrialCompanyDebug} from "./domain/IndustrialCompanyDebugData";


const INFO_LOOM_MENU_OPEN = "InfoLoomMenuOpen";
const COMMERCIAL_MENU_OPEN = "CommercialMenuOpen";
const INDUSTRIAL_MENU_OPEN = "IndustrialMenuOpen";
const DISTRICT_MENU_OPEN = "DistrictMenuOpen";
const RESIDENTIAL_MENU_OPEN = "ResidentialMenuOpen";
const BUILDING_DEMAND_OPEN = "BuildingDemandOpen";
const COMMERCIAL_DEMAND_OPEN = "CommercialDemandOpen";
const COMMERCIAL_PRODUCTS_OPEN = "CommercialProductsOpen";
const DEMOGRAPHICS_OPEN = "DemographicsOpen";
const DISTRICT_DATA_OPEN = "DistrictDataOpen";
const INDUSTRIAL_DEMAND_OPEN = "IndustrialDemandOpen";
const INDUSTRIAL_PRODUCTS_OPEN = "IndustrialProductsOpen";
const RESIDENTIAL_DEMAND_OPEN = "ResidentialDemandOpen";
const TRADE_COSTS_OPEN = "TradeCostsOpen";
const WORKFORCE_OPEN = "WorkforceOpen";
const WORKPLACES_OPEN = "WorkplacesOpen";
const COMMERCIAL_COMPANY_DEBUG_OPEN = "CommercialCompanyDebugOpen";
const INDUSTRIAL_COMPANY_DEBUG_OPEN = "IndustrialCompanyDebugOpen";
const HOUSEHOLDS_DATA_OPEN = "HouseholdsDataOpen";
export const BUILDING_DEMAND_DATA = "BuildingDemandData";
export const COMMERCIAL_DATA = "CommercialData";
export const COMMERCIAL_DATA_EX_RES = "CommercialDataExRes";
export const COMMERCIAL_PRODUCTS_DATA = "CommercialProductsData";
export const DEMOGRAPHICS_DATA_TOTALS = "DemographicsDataTotals";
export const DEMOGRAPHICS_DATA_DETAILS = "DemographicsDataDetails";
export const DEMOGRAPHICS_DATA_OLDEST_CITIZEN = "DemographicsDataOldestCitizen";
export const DEMO_STATS_TOGGLED_ON = "DemoStatsToggledOn";
export const DEMOGRAPHICS_DATA_GROUPED = "DemographicsDataGrouped";



export const DISTRICT_DATA = "DistrictData";
export const DISTRICT_EMPLOYEE_DATA = "DistrictEmployeeData";
export const DISTRICT_COUNT = "DistrictCount";
export const INDUSTRIAL_DATA = "IndustrialData";
export const INDUSTRIAL_DATA_EX_RES = "IndustrialDataExRes";
export const INDUSTRIAL_PRODUCTS_DATA = "IndustrialProductsData";
export const RESIDENTIAL_DATA = "ResidentialData";
export const TRADE_COSTS_DATA = "TradeCostsData";
export const TRADE_COSTS_DATA_IMPORTS = "TradeCostsDataImports";
export const TRADE_COSTS_DATA_EXPORTS = "TradeCostsDataExports";
export const WORKFORCE_DATA = "WorkforceData";
export const WORKPLACES_DATA = "WorkplacesData";

export const InfoLoomMenuOpen = bindValue<boolean>(mod.id, INFO_LOOM_MENU_OPEN, false);
export const CommercialMenuOpen = bindValue<boolean>(mod.id, COMMERCIAL_MENU_OPEN, false);
export const IndustrialMenuOpen = bindValue<boolean>(mod.id, INDUSTRIAL_MENU_OPEN, false);
export const DistrictMenuOpen = bindValue<boolean>(mod.id, DISTRICT_MENU_OPEN, false);
export const ResidentialMenuOpen = bindValue<boolean>(mod.id, RESIDENTIAL_MENU_OPEN, false);
export const BuildingDemandOpen = bindValue<boolean>(mod.id, BUILDING_DEMAND_OPEN, false);
export const CommercialDemandOpen = bindValue<boolean>(mod.id, COMMERCIAL_DEMAND_OPEN, false);
export const CommercialProductsOpen = bindValue<boolean>(mod.id, COMMERCIAL_PRODUCTS_OPEN, false);
export const CommercialCompanyDebugOpen = bindValue<boolean>(mod.id, COMMERCIAL_COMPANY_DEBUG_OPEN, false);
export const IndustrialCompanyDebugOpen = bindValue<boolean>(mod.id, INDUSTRIAL_COMPANY_DEBUG_OPEN, false);
export const DemographicsOpen = bindValue<boolean>(mod.id, DEMOGRAPHICS_OPEN, false);
export const DemoStatsToggledOn = bindValue<boolean>(mod.id, "DemoStatsToggledOn", false);
export const DemoAgeGroupingToggledOn = bindValue<boolean>(mod.id, "DemoAgeGroupingToggledOn", false);
export const DemoGroupingStrategy = bindValue<GroupingStrategy>(mod.id, "DemoGroupingStrategy", GroupingStrategy.None);
export const DistrictDataOpen = bindValue<boolean>(mod.id, DISTRICT_DATA_OPEN, false);
export const IndustrialDemandOpen = bindValue<boolean>(mod.id, INDUSTRIAL_DEMAND_OPEN, false);
export const IndustrialProductsOpen = bindValue<boolean>(mod.id, INDUSTRIAL_PRODUCTS_OPEN, false);
export const ResidentialDemandOpen = bindValue<boolean>(mod.id, RESIDENTIAL_DEMAND_OPEN, false);
export const TradeCostsOpen = bindValue<boolean>(mod.id, TRADE_COSTS_OPEN, false);
export const WorkforceOpen = bindValue<boolean>(mod.id, WORKFORCE_OPEN, false);
export const WorkplacesOpen = bindValue<boolean>(mod.id, WORKPLACES_OPEN, false);



export const CommercialData = bindValue<number[]>(mod.id, "CommercialData");
export const CommercialDataExRes = bindValue<string[]>(mod.id, "CommercialDataExRes", []);
export const CommercialProductsData = bindValue<CommercialProductData[]>(mod.id, "CommercialProductsData", []);
export const CommercialCompanyDebugData = bindValue<CommercialCompanyDebug[]>(mod.id, "CommercialCompanyDebugData");
export const BuildingDemandData  = bindValue<number[]>(mod.id, "BuildingDemandData", []);
export const DemographicsDataDetails = bindValue<populationAtAge[]>(mod.id, "DemographicsDataDetails", []);
export const DemographicsDataTotals = bindValue<number[]>(mod.id, "DemographicsDataTotals", []);
export const DemographicsDataOldestCitizen = bindValue<number>(mod.id, "DemographicsDataOldestCitizen", 0);
export const DistrictData$ = bindValue<District[]>("InfoLoomTwo", "DistrictData", []); 
export const IndustrialData = bindValue<number[]>(mod.id, "IndustrialData", []);
export const IndustrialDataExRes = bindValue<string[]>(mod.id, "IndustrialDataExRes", []);
export const IndustrialProductsData = bindValue<industrialProductData[]>(mod.id, "IndustrialProductsData", []);
export const IndustrialCompanyDebugData = bindValue<IndustrialCompanyDebug[]>(mod.id, "IndustrialCompanyDebugData", []);
export const ResidentialData = bindValue<number[]>(mod.id, "ResidentialData", []);
export const TradeCostsData = bindValue<ResourceTradeCost[]>(mod.id, "TradeCostsData", []);
export const WorkforceData = bindValue<workforceInfo[]>(mod.id, "WorkforceData", []);
export const WorkplacesData = bindValue<workplacesInfo[]>(mod.id, "WorkplacesData", []);
    



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
export const SetDemoStatsToggledOn = (on: boolean) => trigger(mod.id, "DemoStatsToggledOn", on);
export const SetDistrictDataOpen = (open: boolean) => trigger(mod.id, DISTRICT_DATA_OPEN, open);
export const SetIndustrialDemandOpen = (open: boolean) => trigger(mod.id, INDUSTRIAL_DEMAND_OPEN, open);
export const SetIndustrialProductsOpen = (open: boolean) => trigger(mod.id, INDUSTRIAL_PRODUCTS_OPEN, open);
export const SetIndustrialCompanyDebugOpen = (open: boolean) => trigger(mod.id, INDUSTRIAL_COMPANY_DEBUG_OPEN, open);
export const SetResidentialDemandOpen = (open: boolean) => trigger(mod.id, RESIDENTIAL_DEMAND_OPEN, open);
export const SetTradeCostsOpen = (open: boolean) => trigger(mod.id, TRADE_COSTS_OPEN, open);
export const SetWorkforceOpen = (open: boolean) => trigger(mod.id, WORKFORCE_OPEN, open);
export const SetWorkplacesOpen = (open: boolean) => trigger(mod.id, WORKPLACES_OPEN, open);
export const SetDemoGroupingStrategy = (strategy: GroupingStrategy) => trigger(mod.id, "SetDemoGroupingStrategy", strategy);

