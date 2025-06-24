import React, { FC, ReactElement, useState, useEffect, useMemo, useCallback } from 'react';
import { useValue, trigger } from 'cs2/api';
import { Tooltip, Panel, DraggablePanelProps, Button, Icon, Portal } from 'cs2/ui';
import { AutoNavigationScope, FocusActivation } from 'cs2/input';
import {
  formatWords,
  formatNumber,
  formatPercentage2,
  formatPercentage1,
} from 'mods/InfoLoomSections/utils/formatText';
import {
  IndustrialCompanyDebug,
  ProcessResourceInfo,
} from '../../../domain/IndustrialCompanyDebugData';
import { EfficiencyFactorEnum } from '../../../domain/EfficiencyFactorInfo';
import { useLocalization } from 'cs2/l10n';
import styles from './IndustrialCompany.module.scss';
import {
  IndustrialCompanyDebugData,
  SetIndustrialCompanyEfficiency,
  SetIndustrialCompanyEmployee,
  SetIndustrialCompanyNameSorting,
  SetIndustrialCompanyIndexSorting,
  SetIndustrialCompanyProfitability,
  SetIndustrialCompanyResourceAmount,
  IndustrialCompanyEmployee,
  IndustrialCompanyIndexSorting,
  IndustrialCompanyNameSorting,
  IndustrialCompanyProfitability,
  IndustrialCompanyResourceAmount,
  IndustrialCompanyEfficiency,
} from 'mods/bindings';
import { getModule } from 'cs2/modding';
import { Entity, useCssLength } from 'cs2/utils';
import mod from 'mod.json';
import { ResourceInfo } from '../../../domain/CommercialCompanyDebugData';
import {
  CompanyNameEnum2,
  EfficiencyEnum2,
  EmployeesEnum2,
  IndexSortingEnum2,
  ProfitabilityEnum2,
  ResourceAmountEnum2,
} from 'mods/domain/IndustrialCompanyEnums';

// Import VirtualList components
type SizeProvider = {
  getRenderedRange: () => { offset: number; size: number; startIndex: number; endIndex: number };
  getTotalSize: () => number;
};
type RenderItemFn = (itemIndex: number, indexInRange: number) => ReactElement | null;
type RenderedRangeChangedCallback = (startIndex: number, endIndex: number) => void;

interface VirtualListProps {
  className?: string;
  controller?: any;
  direction?: 'vertical' | 'horizontal';
  onRenderedRangeChange?: RenderedRangeChangedCallback;
  renderItem: RenderItemFn;
  sizeProvider: SizeProvider;
  smooth?: boolean;
  style?: Record<string, any>;
}

const VanillaVirtualList: FC<VirtualListProps> = getModule(
  'game-ui/common/scrolling/virtual-list/virtual-list.tsx',
  'VirtualList'
);
const useUniformSizeProvider: (height: number, visible: number, extents: number) => SizeProvider =
  getModule(
    'game-ui/common/scrolling/virtual-list/virtual-list-size-provider.ts',
    'useUniformSizeProvider'
  );

const DataDivider: FC = () => <div className={styles.dataDivider} />;

const focusEntity = (e: Entity) => {
  trigger('camera', 'focusEntity', e);
};

interface EfficiencyTooltipProps {
  company: IndustrialCompanyDebug;
  translate: (key: string, fallback: string) => string | null;
}

const EfficiencyTooltip: FC<EfficiencyTooltipProps> = ({ company, translate }) => {
  // Helper function to get readable factor name from enum value with translation
  const getFactorName = (factor: EfficiencyFactorEnum): string => {
    switch (factor) {
  case EfficiencyFactorEnum.Destroyed:
    return translate("InfoLoomTwo.IndustrialCompanyPanel[EfficiencyDestroyed]", "Destroyed") || "Destroyed";
  case EfficiencyFactorEnum.Abandoned:
    return translate("InfoLoomTwo.IndustrialCompanyPanel[EfficiencyAbandoned]", "Abandoned") || "Abandoned";
  case EfficiencyFactorEnum.Disabled:
    return translate("InfoLoomTwo.IndustrialCompanyPanel[EfficiencyDisabled]", "Disabled") || "Disabled";
  case EfficiencyFactorEnum.Fire:
    return translate("InfoLoomTwo.IndustrialCompanyPanel[EfficiencyFire]", "Fire") || "Fire";
  case EfficiencyFactorEnum.ServiceBudget:
    return translate("InfoLoomTwo.IndustrialCompanyPanel[EfficiencyServiceBudget]", "Service Budget") || "Service Budget";
  case EfficiencyFactorEnum.NotEnoughEmployees:
    return translate("InfoLoomTwo.IndustrialCompanyPanel[EfficiencyNotEnoughEmployees]", "Not Enough Employees") || "Not Enough Employees";
  case EfficiencyFactorEnum.SickEmployees:
    return translate("InfoLoomTwo.IndustrialCompanyPanel[EfficiencySickEmployees]", "Sick Employees") || "Sick Employees";
  case EfficiencyFactorEnum.EmployeeHappiness:
    return translate("InfoLoomTwo.IndustrialCompanyPanel[EfficiencyEmployeeHappiness]", "Employee Happiness") || "Employee Happiness";
  case EfficiencyFactorEnum.ElectricitySupply:
    return translate("InfoLoomTwo.IndustrialCompanyPanel[EfficiencyElectricitySupply]", "Lack of electricity") || "Lack of electricity";
  case EfficiencyFactorEnum.ElectricityFee:
    return translate("InfoLoomTwo.IndustrialCompanyPanel[EfficiencyElectricityFee]", "Electricity fee") || "Electricity fee";
  case EfficiencyFactorEnum.WaterSupply:
    return translate("InfoLoomTwo.IndustrialCompanyPanel[EfficiencyWaterSupply]", "Lack of water") || "Lack of water";
  case EfficiencyFactorEnum.DirtyWater:
    return translate("InfoLoomTwo.IndustrialCompanyPanel[EfficiencyDirtyWater]", "Polluted water") || "Polluted water";
  case EfficiencyFactorEnum.SewageHandling:
    return translate("InfoLoomTwo.IndustrialCompanyPanel[EfficiencySewageHandling]", "Backed up sewer") || "Backed up sewer";
  case EfficiencyFactorEnum.WaterFee:
    return translate("InfoLoomTwo.IndustrialCompanyPanel[EfficiencyWaterFee]", "Water fee") || "Water fee";
  case EfficiencyFactorEnum.Garbage:
    return translate("InfoLoomTwo.IndustrialCompanyPanel[EfficiencyGarbage]", "Piled up garbage") || "Piled up garbage";
  case EfficiencyFactorEnum.Telecom:
    return translate("InfoLoomTwo.IndustrialCompanyPanel[EfficiencyTelecom]", "Network Quality") || "Network Quality";
  case EfficiencyFactorEnum.Mail:
    return translate("InfoLoomTwo.IndustrialCompanyPanel[EfficiencyMail]", "Mail Handling") || "Mail Handling";
  case EfficiencyFactorEnum.MaterialSupply:
    return translate("InfoLoomTwo.IndustrialCompanyPanel[EfficiencyMaterialSupply]", "Lack of resources") || "Lack of resources";
  case EfficiencyFactorEnum.WindSpeed:
    return translate("InfoLoomTwo.IndustrialCompanyPanel[EfficiencyWindSpeed]", "Low wind speed") || "Low wind speed";
  case EfficiencyFactorEnum.WaterDepth:
    return translate("InfoLoomTwo.IndustrialCompanyPanel[EfficiencyWaterDepth]", "Low water depth") || "Low water depth";
  case EfficiencyFactorEnum.SunIntensity:
    return translate("InfoLoomTwo.IndustrialCompanyPanel[EfficiencySunIntensity]", "Lack of sunlight") || "Lack of sunlight";
  case EfficiencyFactorEnum.NaturalResources:
    return translate("InfoLoomTwo.IndustrialCompanyPanel[EfficiencyNaturalResources]", "Natural Resources") || "Natural Resources";
  case EfficiencyFactorEnum.CityModifierSoftware:
    return translate("InfoLoomTwo.IndustrialCompanyPanel[EfficiencyCityModifierSoftware]", "City Effect") || "City Effect";
  case EfficiencyFactorEnum.CityModifierElectronics:
    return translate("InfoLoomTwo.IndustrialCompanyPanel[EfficiencyCityModifierElectronics]", "City Effect") || "City Effect";
  case EfficiencyFactorEnum.CityModifierIndustrialEfficiency:
    return translate("InfoLoomTwo.IndustrialCompanyPanel[EfficiencyCityModifierIndustrial]", "Industrial Efficiency") || "Industrial Efficiency";
  case EfficiencyFactorEnum.CityModifierOfficeEfficiency:
    return translate("InfoLoomTwo.IndustrialCompanyPanel[EfficiencyCityModifierOffice]", "Office Efficiency") || "Office Efficiency";
  case EfficiencyFactorEnum.CityModifierHospitalEfficiency:
    return translate("InfoLoomTwo.IndustrialCompanyPanel[EfficiencyCityModifierHospital]", "Hospital Efficiency") || "Hospital Efficiency";
  case EfficiencyFactorEnum.SpecializationBonus:
    return translate("InfoLoomTwo.IndustrialCompanyPanel[EfficiencySpecializationBonus]", "City Production Specialization") || "City Production Specialization";
      case EfficiencyFactorEnum.Count:
        return translate("InfoLoomTwo.IndustrialCompanyPanel[EfficiencyCount]", "Count") || "Count";
      default:
        return translate("InfoLoomTwo.IndustrialCompanyPanel[EfficiencyUnknown]", "Unknown Factor") || "Unknown Factor";
    }
  };

  return (
    <div className={styles.tooltipContent}>
      <div className={styles.tooltipText}>
        <p>{translate("InfoLoomTwo.IndustrialCompanyPanel[EfficiencyFactorsTitle]", "Factors affecting efficiency:") || "Factors affecting efficiency:"}</p>
        {company.Factors &&
          company.Factors.map((factor, index) => {
            if (!factor) return null;

            // Get a readable name for the factor from the enum value
            const factorName = getFactorName(factor.Factor);

            return (
              <div key={index} className={styles.factorRow}>
                <span className={styles.factorName}>{factorName}</span>
                <span
                  className={
                    factor.Value > 0
                      ? styles.factorPositive
                      : factor.Value < 0
                        ? styles.factorNegative
                        : styles.factorNeutral
                  }
                >
                  {factor.Value > 0 ? '+' : ''}
                  {formatPercentage2(factor.Value)}
                </span>
                <span className={styles.factorResult}>{formatPercentage2(factor.Result)}</span>
              </div>
            );
          })}
      </div>
    </div>
  );
};

interface SortableHeaderProps {
  title: string;
  sortState:
    | IndexSortingEnum2
    | CompanyNameEnum2
    | EmployeesEnum2
    | EfficiencyEnum2
    | ProfitabilityEnum2
    | ResourceAmountEnum2;
  onSort: (direction: 'asc' | 'desc' | 'off') => void;
  className?: string;
}

const SortableHeader: FC<SortableHeaderProps> = ({ title, sortState, onSort, className }) => {
  const handleClick = (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();

    // Cycle through sort states based on actual enum values:
    // Off = 0, Ascending = 1, Descending = 2
    // Cycle: Off -> Ascending -> Descending -> Off
    if (sortState === 0) {
      // Off
      onSort('asc');
    } else if (sortState === 1) {
      // Ascending
      onSort('desc');
    } else {
      // Descending
      onSort('off');
    }
  };

  return (
    <div
      className={`${styles.sortableHeader} ${styles.headerCell} ${className || ''}`}
      onClick={handleClick}
      style={{ cursor: 'pointer' }}
    >
      <span>{title}</span>
      <div className={styles.sortArrows}>
        {sortState === 1 && (
          <Icon src="coui://uil/Standard/ArrowSortHighDown.svg" className={styles.sortIcon} />
        )}
        {sortState === 2 && (
          <Icon src="coui://uil/Standard/ArrowSortLowDown.svg" className={styles.sortIcon} />
        )}
      </div>
    </div>
  );
};

const IndustrialCompany: FC<DraggablePanelProps> = ({ onClose }) => {
  const { translate } = useLocalization();
  const companiesData = useValue(IndustrialCompanyDebugData);
  const [visibleRange, setVisibleRange] = useState({ startIndex: 0, endIndex: 0 });
  const [heightFull, setHeightFull] = useState(600); // Default fallback height
  const indexSortingOptions = useValue(IndustrialCompanyIndexSorting);
  const nameSortingOptions = useValue(IndustrialCompanyNameSorting);
  const employeeSortingOptions = useValue(IndustrialCompanyEmployee);
  const efficiencySortingOptions = useValue(IndustrialCompanyEfficiency);
  const profitabilitySortingOptions = useValue(IndustrialCompanyProfitability);
  const resourceAmountSortingOptions = useValue(IndustrialCompanyResourceAmount);

  // Helper function to get efficiency class
  const getEfficiencyClass = (efficiency: number) => {
    if (efficiency >= 0.8) return styles.efficiencyHigh;
    if (efficiency >= 0.5) return styles.efficiencyMedium;
    return styles.efficiencyLow;
  };

  // Helper function to get profitability class
  const getProfitabilityClass = (profitability: number) => {
    return profitability >= 0 ? styles.profitabilityPositive : styles.profitabilityNegative;
  };

  // Component with translations
  const ResourcesToolTipWithTranslation = ({ resources }: { resources: ResourceInfo[] }) => {
    return (
      <div className={styles.tooltipContent}>
        <div className={styles.tooltipText}>
          <p>
            <strong>{translate("InfoLoomTwo.IndustrialCompanyPanel[ResourcesTitle]", "Resources") || "Resources"}</strong>
          </p>
          {resources && resources.length > 0 ? (
            resources.map((resource, index) => (
              <div key={index} className={styles.resourceItem}>
                <p cohinline="cohinline">
                  {resource.ResourceName}: {resource.Amount}
                </p>
              </div>
            ))
          ) : (
            <p>{translate("InfoLoomTwo.IndustrialCompanyPanel[NoResources]", "No resources") || "No resources"}</p>
          )}
        </div>
      </div>
    );
  };

  const ProcessingInfoTooltipWithTranslation = ({ inputResources, outputResources }: { inputResources: ProcessResourceInfo[], outputResources: ProcessResourceInfo[] }) => {
    return (
      <div className={styles.tooltipContent}>
        <div className={styles.tooltipText}>
          <p>
            <strong>{translate("InfoLoomTwo.IndustrialCompanyPanel[ProcessingResourcesTitle]", "Processing Resources") || "Processing Resources"}</strong>
          </p>
          {outputResources.length > 0 ? (
            <div>
              {outputResources.map((resource, index) => (
                <p cohinline="cohinline" key={`output-${index}`}>
                  {index === 0 ? (translate("InfoLoomTwo.IndustrialCompanyPanel[Output]", "Output:") || "Output:") + " " : ''}
                  {resource.resourceName}: {resource.amount}
                </p>
              ))}
            </div>
          ) : (
            <p>{translate("InfoLoomTwo.IndustrialCompanyPanel[NoOutputResources]", "No output resources") || "No output resources"}</p>
          )}

          {inputResources.length > 0 ? (
            <div>
              {inputResources.map((resource, index) => (
                <p key={`input-${index}`} cohinline="cohinline">
                  {index === 0 ? (translate("InfoLoomTwo.IndustrialCompanyPanel[Input]", "Input:") || "Input:") + " " : ''}
                  {resource.resourceName}: {resource.amount}
                </p>
              ))}
            </div>
          ) : (
            <p>{translate("InfoLoomTwo.IndustrialCompanyPanel[NoInputResources]", "No input resources") || "No input resources"}</p>
          )}
        </div>
      </div>
    );
  };

  const ProfitabilityTooltipWithTranslation = ({ company }: { company: IndustrialCompanyDebug }) => {
    return (
      <div className={styles.tooltipContent}>
        <div className={styles.tooltipText}>
          <p>
            <strong>{translate("InfoLoomTwo.IndustrialCompanyPanel[FinancialInformation]", "Financial Information") || "Financial Information"}</strong>
          </p>
          <p>{translate("InfoLoomTwo.IndustrialCompanyPanel[TotalWorth]", "Total Worth") || "Total Worth"}: {formatNumber(company.LastTotalWorth)}</p>
          <p>{translate("InfoLoomTwo.IndustrialCompanyPanel[TotalWages]", "Total Wages") || "Total Wages"}: {formatNumber(company.TotalWages)}</p>
          <p>{translate("InfoLoomTwo.IndustrialCompanyPanel[DailyProduction]", "Daily Production") || "Daily Production"}: {formatNumber(company.ProductionPerDay)}</p>
          <p>{translate("InfoLoomTwo.IndustrialCompanyPanel[Concentration]", "Concentration") || "Concentration"}: {formatPercentage2(company.Concentration)}</p>
          {company.IsExtractor && <p>{translate("InfoLoomTwo.IndustrialCompanyPanel[ExtractorType]", "Extractor Type: Yes") || "Extractor Type: Yes"}</p>}
        </div>
      </div>
    );
  };

  // Memoized CompanyRow component to prevent re-renders when props haven't changed
  const CompanyRowWithTranslation = React.memo(({ company }: { company: IndustrialCompanyDebug }) => {
    const totalEfficiency = company.TotalEfficiency;

    // Pre-calculate processing resources to avoid inline filtering
    const inputResources = useMemo(() => 
      company.ProcessResources?.filter(r => !r.isOutput) || [], 
      [company.ProcessResources]
    );
    const outputResources = useMemo(() => 
      company.ProcessResources?.filter(r => r.isOutput) || [], 
      [company.ProcessResources]
    );

    // Memoized tooltips to prevent recreation
    const resourcesTooltip = useMemo(() => 
      <ResourcesToolTipWithTranslation resources={company.Resources || []} />, 
      [company.Resources]
    );

    const processingTooltip = useMemo(() => 
      <ProcessingInfoTooltipWithTranslation
        inputResources={inputResources}
        outputResources={outputResources}
      />, 
      [inputResources, outputResources]
    );

    const efficiencyTooltip = useMemo(() => 
      <EfficiencyTooltip company={company} translate={translate} />, 
      [company, translate]
    );

    const profitabilityTooltip = useMemo(() => 
      <ProfitabilityTooltipWithTranslation company={company} />, 
      [company]
    );

    return (
      <div className={styles.row}>
        {/* Name Column */}
        <div className={styles.nameColumn}>
          {company.CompanyName}
        </div>

        {/* Employees Column */}
        <div className={styles.employeeColumn}>
          {`${formatNumber(company.TotalEmployees)}/${formatNumber(company.MaxWorkers)}`}
        </div>

        {/* Vehicles Column */}
        <div className={styles.vehicleColumn}>
          {`${formatNumber(company.VehicleCount)}/${formatNumber(company.VehicleCapacity)}`}
        </div>

        {/* Resources Column */}
        <Tooltip tooltip={resourcesTooltip}>
          <div className={styles.resourcesColumn}>
            {company.Resources && company.Resources.length > 0 ? (
              <div className={styles.resourceGroup}>
                {company.Resources.map((r, i) => (
                  <Icon key={`resource-${i}`} src={r.Icon} className={styles.resourceIcon} />
                ))}
              </div>
            ) : (
              <Icon src={company.ResourceIcon} className={styles.resourceIcon} />
            )}
          </div>
        </Tooltip>

        {/* Processing Column - Simplified */}
        <Tooltip tooltip={processingTooltip}>
          <div className={styles.processingColumn}>
            {company.ProcessResources ? (
              <div className={styles.processingWrapper}>
                <div className={styles.processContainer}>
                  {inputResources.length > 0 ? (
                    <div className={styles.resourceGroup}>
                      {inputResources.map((r, i) => (
                        <Icon key={`input-${i}`} src={r.resourceIcon} className={styles.resourceIcon} />
                      ))}
                    </div>
                  ) : (
                    <div className={styles.emptyGroup}></div>
                  )}

                  <div className={styles.divider}>→</div>

                  {outputResources.length > 0 ? (
                    <div className={styles.resourceGroup}>
                      {outputResources.map((r, i) => (
                        <Icon key={`output-${i}`} src={r.resourceIcon} className={styles.resourceIcon} />
                      ))}
                    </div>
                  ) : (
                    <div className={styles.emptyGroup}></div>
                  )}
                </div>
              </div>
            ) : (
              translate("InfoLoomTwo.IndustrialCompanyPanel[None]", "None") || "None"
            )}
          </div>
        </Tooltip>

        {/* Efficiency Column */}
        <Tooltip tooltip={efficiencyTooltip}>
          <div className={styles.efficiencyColumn}>
            <span className={getEfficiencyClass(totalEfficiency)}>
              {formatPercentage2(totalEfficiency)}
            </span>
          </div>
        </Tooltip>

        {/* Profitability Column */}
        <Tooltip tooltip={profitabilityTooltip}>
          <div className={styles.profitabilityColumn}>
            <span className={getProfitabilityClass(company.Profitability)}>
              {formatPercentage2(company.Profitability)}
            </span>
          </div>
        </Tooltip>

        {/* Location Column */}
        <div className={styles.locationColumn}>
          <Button
            variant={'icon'}
            src={'Media/Game/Icons/MapMarker.svg'}
            onSelect={() => focusEntity(company.EntityId)}
            className={styles.magnifierIcon}
          />
        </div>
      </div>
    );
  });

  // Dynamic height calculation
  const calculateHeights = useCallback(() => {
    const wrapperElement = document.querySelector('.info-layout_BVk') as HTMLElement | null;
    const newHeightFull = wrapperElement?.offsetHeight ?? 600;
    setHeightFull(newHeightFull);
  }, []);

  useEffect(() => {
    calculateHeights();
    const observer = new MutationObserver(() => {
      calculateHeights();
    });

    observer.observe(document.body, {
      childList: true,
      subtree: true,
    });

    return () => observer.disconnect();
  }, [calculateHeights]);
  // Optimized memoization - only calculate height, let virtual list handle rendering
  const maxListHeight = useMemo(() => {
    // Calculate max height: reserve 250px for header, footer, and padding
    const calculatedHeight = Math.min(32 * companiesData.length, heightFull - 250);
    return Math.max(200, calculatedHeight); // Minimum 200px
  }, [companiesData.length, heightFull]);

  // Size provider using proper CSS units
  const sizeProvider = useUniformSizeProvider(useCssLength('32rem'), companiesData.length, 5);

  const handleRenderedRangeChange = useCallback((startIndex: number, endIndex: number) => {
    setVisibleRange({ startIndex, endIndex });
  }, []);
  const renderItem: RenderItemFn = useCallback(
    (itemIndex: number, indexInRange: number) => {
      if (itemIndex < 0 || itemIndex >= companiesData.length) return null;

      const company = companiesData[itemIndex];
      if (!company) return null;

      return (
        <CompanyRowWithTranslation
          key={`industrial-company-${itemIndex}`}
          company={company}
        />
      );
    },
    [companiesData]
  );

  // Early return for no data
  if (!companiesData.length) {
    return (
      <Portal>
        <Panel
          draggable
          onClose={onClose}
          initialPosition={{ x: 0.5, y: 0.5 }}
          className={styles.panel}
          header={
            <div className={styles.header}>
              <span className={styles.headerText}>{translate("InfoLoomTwo.IndustrialCompanyPanel[Title]", "Industrial Companies") || "Industrial Companies"}</span>
            </div>
          }
        >
          <p className={styles.loadingText}>{translate("InfoLoomTwo.IndustrialCompanyPanel[NoCompanies]", "No Industrial Companies Found") || "No Industrial Companies Found"}</p>
        </Panel>
      </Portal>
    );
  }

  return (
    <Portal>
      <Panel
        draggable
        onClose={onClose}
        initialPosition={{ x: 0.5, y: 0.5 }}
        className={styles.panel}
        header={
          <div className={styles.header}>
            <span className={styles.headerText}>{translate("InfoLoomTwo.IndustrialCompanyPanel[Title]", "Industrial Companies") || "Industrial Companies"}</span>
          </div>
        }
      >
        <div>
          <div className={styles.tableHeader}>
            <div className={styles.headerRow}>
              <SortableHeader
                title={translate("InfoLoomTwo.IndustrialCompanyPanel[Name]", "Name") || "Name"}
                sortState={nameSortingOptions}
                onSort={direction => {
                  if (direction === 'asc') SetIndustrialCompanyNameSorting(CompanyNameEnum2.Ascending);
                  else if (direction === 'desc')
                    SetIndustrialCompanyNameSorting(CompanyNameEnum2.Descending);
                  else SetIndustrialCompanyNameSorting(CompanyNameEnum2.Off);
                }}
                className={styles.nameColumn}
              />
              
              <SortableHeader
                title={translate("InfoLoomTwo.IndustrialCompanyPanel[Employees]", "Employees") || "Employees"}
                sortState={employeeSortingOptions}
                onSort={direction => {
                  if (direction === 'asc') SetIndustrialCompanyEmployee(EmployeesEnum2.Ascending);
                  else if (direction === 'desc') SetIndustrialCompanyEmployee(EmployeesEnum2.Descending);
                  else SetIndustrialCompanyEmployee(EmployeesEnum2.Off);
                }}
                className={styles.employeeColumn}
              />
              
              <Tooltip tooltip={translate("InfoLoomTwo.IndustrialCompanyPanel[VehiclesTooltip]", "Current vehicle count vs maximum vehicle capacity for deliveries and transportation") || "Current vehicle count vs maximum vehicle capacity"}>
                <div className={`${styles.headerCell} ${styles.vehicleColumn}`}>
                  <b>{translate("InfoLoomTwo.IndustrialCompanyPanel[Vehicles]", "Vehicles") || "Vehicles"}</b>
                </div>
              </Tooltip>
              
              <SortableHeader
                title={translate("InfoLoomTwo.IndustrialCompanyPanel[Resources]", "Resources") || "Resources"}
                sortState={resourceAmountSortingOptions}
                onSort={direction => {
                  if (direction === 'asc')
                    SetIndustrialCompanyResourceAmount(ResourceAmountEnum2.Ascending);
                  else if (direction === 'desc')
                    SetIndustrialCompanyResourceAmount(ResourceAmountEnum2.Descending);
                  else SetIndustrialCompanyResourceAmount(ResourceAmountEnum2.Off);
                }}
                className={styles.resourcesColumn}
              />
              
              <Tooltip tooltip={translate("InfoLoomTwo.IndustrialCompanyPanel[ProcessingTooltip]", "Input and output resources processed by this company in the production chain") || "Input/output resources processed by this industrial company"}>
                <div className={`${styles.headerCell} ${styles.processingColumn}`}>
                  <b>{translate("InfoLoomTwo.IndustrialCompanyPanel[Processing]", "Processing") || "Processing"}</b>
                </div>
              </Tooltip>
              
              <SortableHeader
                title={translate("InfoLoomTwo.IndustrialCompanyPanel[Efficiency]", "Efficiency") || "Efficiency"}
                sortState={efficiencySortingOptions}
                onSort={direction => {
                  if (direction === 'asc') SetIndustrialCompanyEfficiency(EfficiencyEnum2.Ascending);
                  else if (direction === 'desc')
                    SetIndustrialCompanyEfficiency(EfficiencyEnum2.Descending);
                  else SetIndustrialCompanyEfficiency(EfficiencyEnum2.Off);
                }}
                className={styles.efficiencyColumn}
              />
              
              <SortableHeader
                title={translate("InfoLoomTwo.IndustrialCompanyPanel[Profitability]", "Profitability") || "Profitability"}
                sortState={profitabilitySortingOptions}
                onSort={direction => {
                  if (direction === 'asc')
                    SetIndustrialCompanyProfitability(ProfitabilityEnum2.Ascending);
                  else if (direction === 'desc')
                    SetIndustrialCompanyProfitability(ProfitabilityEnum2.Descending);
                  else SetIndustrialCompanyProfitability(ProfitabilityEnum2.Off);
                }}
                className={styles.profitabilityColumn}
              />
              
              <Tooltip tooltip={translate("InfoLoomTwo.IndustrialCompanyPanel[LocationTooltip]", "Click to focus camera on the company's location") || "Location of the industrial company"}>
                <div className={`${styles.headerCell} ${styles.locationColumn}`}>
                  <b>{translate("InfoLoomTwo.IndustrialCompanyPanel[Location]", "Location") || "Location"}</b>
                </div>
              </Tooltip>
            </div>
          </div>
          
          <DataDivider />
          
          <div className={styles.virtualListContainer}>
            <AutoNavigationScope activation={FocusActivation.AnyChildren}>
              <VanillaVirtualList
                direction="vertical"
                sizeProvider={sizeProvider}
                renderItem={renderItem}
                style={{
                  maxHeight: `${maxListHeight}px`,
                }}
                smooth
                onRenderedRangeChange={handleRenderedRangeChange}
              />
            </AutoNavigationScope>
          </div>
          
          <DataDivider />
        </div>
      </Panel>
    </Portal>
  );
};

export default IndustrialCompany;
