import React, { FC, ReactElement, useState, useEffect, useMemo, useCallback } from 'react';
import { useValue, trigger } from 'cs2/api';
import { Tooltip, Panel, DraggablePanelProps, Button, FloatingButton, Icon, Portal } from 'cs2/ui';
import { AutoNavigationScope, FocusActivation } from 'cs2/input';
import {
  formatWords,
  formatNumber,
  formatPercentage2,
  formatPercentage1,
} from 'mods/InfoLoomSections/utils/formatText';
import { CommercialCompanyDebug, ResourceInfo } from '../../../domain/CommercialCompanyDebugData';
import styles from './CommercialCompanyDebugData.module.scss';
import {
  CommercialCompanyDebugData,
  SetDemoGroupingStrategy,
  CommercialCompanyIndexSorting,
  SetCommercialCompanyIndexSorting,
  CommercialCompanyNameSorting,
  SetCommercialCompanyNameSorting,
  CommercialCompanyServiceUsage,
  SetCommercialCompanyServiceUsage,
  CommercialCompanyEmployee,
  SetCommercialCompanyEmployee,
  CommercialCompanyEfficiency,
  SetCommercialCompanyEfficiency,
  CommercialCompanyProfitability,
  SetCommercialCompanyProfitability,
  CommercialCompannyResourceAmount,
  SetCommercialCompannyResourceAmount,
} from 'mods/bindings';
import { getModule } from 'cs2/modding';
import { Entity, useCssLength } from 'cs2/utils';
import mod from 'mod.json';
import { useLocalization } from 'cs2/l10n';
import { EfficiencyFactorEnum } from 'mods/domain/EfficiencyFactorInfo';
import { IndexSortingEnum } from 'mods/domain/CommercialCompanyEnums/IndexSortingEnum';
import { CompanyNameEnum } from 'mods/domain/CommercialCompanyEnums/CompanyNameEnum';
import { ServiceUsageEnum } from '../../../domain/CommercialCompanyEnums/ServiceUsageEnum';
import { EmployeesEnum } from '../../../domain/CommercialCompanyEnums/EmployeesEnum';
import { EfficiencyEnum } from '../../../domain/CommercialCompanyEnums/EfficiencyEnum';
import { ProfitabilityEnum } from '../../../domain/CommercialCompanyEnums/ProfitabilityEnum';
import { ResourceAmountEnum } from '../../../domain/CommercialCompanyEnums/ResourceAmountEnum';

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
  company: CommercialCompanyDebug;
}

const EfficiencyTooltip: FC<EfficiencyTooltipProps> = ({ company }) => {
  const getFactorName = (factor: EfficiencyFactorEnum): string => {
    switch (factor) {
      case EfficiencyFactorEnum.Destroyed:
        return 'Destroyed';
      case EfficiencyFactorEnum.Abandoned:
        return 'Abandoned';
      case EfficiencyFactorEnum.Disabled:
        return 'Disabled';
      case EfficiencyFactorEnum.Fire:
        return 'Fire';
      case EfficiencyFactorEnum.ServiceBudget:
        return 'Service Budget';
      case EfficiencyFactorEnum.NotEnoughEmployees:
        return 'Not Enough Employees';
      case EfficiencyFactorEnum.SickEmployees:
        return 'Sick Employees';
      case EfficiencyFactorEnum.EmployeeHappiness:
        return 'Employee Happiness';
      case EfficiencyFactorEnum.ElectricitySupply:
        return 'Electricity Supply';
      case EfficiencyFactorEnum.ElectricityFee:
        return 'Electricity Fee';
      case EfficiencyFactorEnum.WaterSupply:
        return 'Water Supply';
      case EfficiencyFactorEnum.DirtyWater:
        return 'Dirty Water';
      case EfficiencyFactorEnum.SewageHandling:
        return 'Sewage Handling';
      case EfficiencyFactorEnum.WaterFee:
        return 'Water Fee';
      case EfficiencyFactorEnum.Garbage:
        return 'Garbage';
      case EfficiencyFactorEnum.Telecom:
        return 'Telecommunications';
      case EfficiencyFactorEnum.Mail:
        return 'Mail';
      case EfficiencyFactorEnum.MaterialSupply:
        return 'Material Supply';
      case EfficiencyFactorEnum.WindSpeed:
        return 'Wind Speed';
      case EfficiencyFactorEnum.WaterDepth:
        return 'Water Depth';
      case EfficiencyFactorEnum.SunIntensity:
        return 'Sun Intensity';
      case EfficiencyFactorEnum.NaturalResources:
        return 'Natural Resources';
      case EfficiencyFactorEnum.CityModifierSoftware:
        return 'City Modifier: Software';
      case EfficiencyFactorEnum.CityModifierElectronics:
        return 'City Modifier: Electronics';
      case EfficiencyFactorEnum.CityModifierIndustrialEfficiency:
        return 'Industrial Efficiency';
      case EfficiencyFactorEnum.CityModifierOfficeEfficiency:
        return 'Office Efficiency';
      case EfficiencyFactorEnum.CityModifierHospitalEfficiency:
        return 'Hospital Efficiency';
      case EfficiencyFactorEnum.SpecializationBonus:
        return 'Specialization Bonus';
      case EfficiencyFactorEnum.Count:
        return 'Count';
      default:
        return 'Unknown Factor';
    }
  };

  return (
    <div className={styles.tooltipContent}>
      <div className={styles.tooltipText}>
        <p>Factors affecting efficiency:</p>
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
                      ? styles.positive
                      : factor.Value < 0
                        ? styles.negative
                        : styles.neutral
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

interface ResourcesToolTipProps {
  resources: ResourceInfo[];
}

const ResourcesToolTip: FC<ResourcesToolTipProps> = ({ resources }) => {
  return (
    <div className={styles.tooltipContent}>
      <div className={styles.tooltipText}>
        <p>
          <strong>Resources</strong>
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
          <p>No resources</p>
        )}
      </div>
    </div>
  );
};

interface ServiceTooltipProps {
  serviceAvailable: number;
  maxService: number;
}

const ServiceTooltip: FC<ServiceTooltipProps> = ({ serviceAvailable, maxService }) => {
  // Calculate service usage percentage (inverted from availability)
  const serviceUsagePercentage = maxService > 0 ? 1 - serviceAvailable / maxService : 0;

  return (
    <div className={styles.tooltipContent}>
      <div className={styles.tooltipText}>
        <p>
          <strong>Service Usage: {formatPercentage1(serviceUsagePercentage)}</strong>
        </p>
        <p>Available: {formatNumber(serviceAvailable)}</p>
        <p>Maximum: {formatNumber(maxService)}</p>
      </div>
    </div>
  );
};

interface ProfitabilityTooltipProps {
  company: CommercialCompanyDebug;
}

const ProfitabilityTooltip: FC<ProfitabilityTooltipProps> = ({ company }) => {
  return (
    <div className={styles.tooltipContent}>
      <div className={styles.tooltipText}>
        <p>
          <strong>Financial Information</strong>
        </p>
        <p>Last Total Worth: {formatNumber(company.LastTotalWorth)}</p>
        <p>Total Wages: {formatNumber(company.TotalWages)}</p>
        <p>Daily Production: {formatNumber(company.ProductionPerDay)}</p>
      </div>
    </div>
  );
};

interface SortableHeaderProps {
  title: string | null;
  sortState:
    | IndexSortingEnum
    | CompanyNameEnum
    | ServiceUsageEnum
    | EmployeesEnum
    | EfficiencyEnum
    | ProfitabilityEnum
    | ResourceAmountEnum;
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

interface CompanyRowProps {
  company: CommercialCompanyDebug;
}

// Memoize the CompanyRow component to prevent re-renders when props haven't changed
const CompanyRow: FC<CompanyRowProps> = React.memo(({ company }) => {
  const totalEfficiency = company.TotalEfficiency;
  
  // Calculate service usage (inverted from availability)
  const serviceUsagePercentage =
    company.MaxService > 0 ? 1 - company.ServiceAvailable / company.MaxService : 0;
  const serviceUsage = formatPercentage1(serviceUsagePercentage);

  // Helper functions for color classes
  const getEfficiencyClass = (efficiency: number) => {
    if (efficiency >= 0.8) return styles.efficiencyHigh;
    if (efficiency >= 0.5) return styles.efficiencyMedium;
    return styles.efficiencyLow;
  };

  const getProfitabilityClass = (profitability: number) => {
    return profitability >= 0 ? styles.profitabilityPositive : styles.profitabilityNegative;
  };

  const getServiceClass = (serviceUsage: number) => {
    if (serviceUsage >= 0.8) return styles.serviceHigh;
    if (serviceUsage >= 0.5) return styles.serviceMedium;
    return styles.serviceLow;
  };

  return (
    <div className={styles.row}>
      {/* Name Column */}
      <div className={styles.nameColumn}>{company.CompanyName}</div>

      {/* Service Usage Column */}
      <Tooltip
        tooltip={
          <ServiceTooltip
            serviceAvailable={company.ServiceAvailable}
            maxService={company.MaxService}
          />
        }
      >
        <div className={styles.serviceColumn}>
          <span className={getServiceClass(serviceUsagePercentage)}>
            {serviceUsage}
          </span>
        </div>
      </Tooltip>

      {/* Employees Column */}
      <div className={styles.employeeColumn}>
        {`${formatNumber(company.TotalEmployees)}/${formatNumber(company.MaxWorkers)}`}
      </div>

      {/* Vehicles Column */}
      <div className={styles.vehicleColumn}>
        {`${formatNumber(company.VehicleCount)}/${formatNumber(company.VehicleCapacity)}`}
      </div>

      {/* Resources Column */}
      <Tooltip tooltip={<ResourcesToolTip resources={company.Resources || []} />}>
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

      {/* Efficiency Column */}
      <Tooltip tooltip={<EfficiencyTooltip company={company} />}>
        <div className={styles.efficiencyColumn}>
          <span className={getEfficiencyClass(totalEfficiency)}>
            {formatPercentage2(totalEfficiency)}
          </span>
        </div>
      </Tooltip>

      {/* Profitability Column */}
      <Tooltip tooltip={<ProfitabilityTooltip company={company} />}>
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

const CommercialCompanyDebugDataPanel: FC<DraggablePanelProps> = ({ onClose }) => {
  const { translate } = useLocalization();
  const companiesData = useValue(CommercialCompanyDebugData);
  const [visibleRange, setVisibleRange] = useState({ startIndex: 0, endIndex: 0 });
  const [heightFull, setHeightFull] = useState(600);
  const indexSortingOptions = useValue(CommercialCompanyIndexSorting);
  const nameSortingOptions = useValue(CommercialCompanyNameSorting);
  const serviceUsageSortingOptions = useValue(CommercialCompanyServiceUsage);
  const employeeSortingOptions = useValue(CommercialCompanyEmployee);
  const efficiencySortingOptions = useValue(CommercialCompanyEfficiency);
  const profitabilitySortingOptions = useValue(CommercialCompanyProfitability);
  const resourceAmountSortingOptions = useValue(CommercialCompannyResourceAmount);
  
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

  // Improved memoization with dynamic height
  const [visibleCompanies, maxListHeight] = useMemo(() => {
    const bufferSize = 5;
    const start = Math.max(0, visibleRange.startIndex - bufferSize);
    const end = Math.min(companiesData.length, visibleRange.endIndex + bufferSize);
    const companies = companiesData.slice(start, end);

    // Calculate max height: reserve 250px for header, footer, and padding
    const calculatedHeight = Math.min(32 * companiesData.length, heightFull - 250);

    return [companies, Math.max(200, calculatedHeight)]; // Minimum 200px
  }, [companiesData, visibleRange.startIndex, visibleRange.endIndex, heightFull]);

  // Size provider using proper CSS units
  const sizeProvider = useUniformSizeProvider(useCssLength('32rem'), companiesData.length, 5);

  const handleRenderedRangeChange = useCallback((startIndex: number, endIndex: number) => {
    setVisibleRange({ startIndex, endIndex });
  }, []);

  const renderItem: RenderItemFn = useCallback(
    (itemIndex: number, indexInRange: number) => {
      if (itemIndex < 0 || itemIndex >= companiesData.length) return null;

      const bufferSize = 5;
      const adjustedIndex = itemIndex - Math.max(0, visibleRange.startIndex - bufferSize);

      if (adjustedIndex < 0 || adjustedIndex >= visibleCompanies.length) return null;

      return <CompanyRow key={`company-${itemIndex}`} company={visibleCompanies[adjustedIndex]} />;
    },
    [companiesData.length, visibleRange.startIndex, visibleCompanies]
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
              <span className={styles.headerText}>Commercial Companies</span>
            </div>
          }
        >
          <p className={styles.loadingText}>No Commercial Companies Found</p>
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
            <span className={styles.headerText}>
              {translate("InfoLoomTwo.CommercialCompanyPanel[Title]", "Commercial Companies")}
            </span>
          </div>
        }
      >
        <div>
          <div className={styles.tableHeader}>
            <div className={styles.headerRow}>
              <SortableHeader
                title={translate("InfoLoomTwo.CommercialCompanyPanel[Name]", "Name")}
                sortState={nameSortingOptions}
                onSort={direction => {
                  if (direction === 'asc') SetCommercialCompanyNameSorting(CompanyNameEnum.Ascending);
                  else if (direction === 'desc') SetCommercialCompanyNameSorting(CompanyNameEnum.Descending);
                  else SetCommercialCompanyNameSorting(CompanyNameEnum.Off);
                }}
                className={styles.nameColumn}
              />

              <SortableHeader
                title={translate("InfoLoomTwo.CommercialCompanyPanel[ServiceUsage]", "Service Usage")}
                sortState={serviceUsageSortingOptions}
                onSort={direction => {
                  if (direction === 'asc') SetCommercialCompanyServiceUsage(ServiceUsageEnum.Ascending);
                  else if (direction === 'desc') SetCommercialCompanyServiceUsage(ServiceUsageEnum.Descending);
                  else SetCommercialCompanyServiceUsage(ServiceUsageEnum.Off);
                }}
                className={styles.serviceColumn}
              />

              <SortableHeader
                title={translate("InfoLoomTwo.CommercialCompanyPanel[Employees]", "Employees")}
                sortState={employeeSortingOptions}
                onSort={direction => {
                  if (direction === 'asc') SetCommercialCompanyEmployee(EmployeesEnum.Ascending);
                  else if (direction === 'desc') SetCommercialCompanyEmployee(EmployeesEnum.Descending);
                  else SetCommercialCompanyEmployee(EmployeesEnum.Off);
                }}
                className={styles.employeeColumn}
              />

              <Tooltip tooltip={translate("InfoLoomTwo.CommercialCompanyPanel[VehiclesTooltip]", "Current vehicle count vs maximum vehicle capacity for deliveries and transportation")}>
                <div className={`${styles.headerCell} ${styles.vehicleColumn}`}>
                  <b>{translate("InfoLoomTwo.CommercialCompanyPanel[Vehicles]", "Vehicles")}</b>
                </div>
              </Tooltip>

              <SortableHeader
                title={translate("InfoLoomTwo.CommercialCompanyPanel[Resources]", "Resources")}
                sortState={resourceAmountSortingOptions}
                onSort={direction => {
                  if (direction === 'asc') SetCommercialCompannyResourceAmount(ResourceAmountEnum.Ascending);
                  else if (direction === 'desc') SetCommercialCompannyResourceAmount(ResourceAmountEnum.Descending);
                  else SetCommercialCompannyResourceAmount(ResourceAmountEnum.Off);
                }}
                className={styles.resourcesColumn}
              />

              <SortableHeader
                title={translate("InfoLoomTwo.CommercialCompanyPanel[Efficiency]", "Efficiency")}
                sortState={efficiencySortingOptions}
                onSort={direction => {
                  if (direction === 'asc') SetCommercialCompanyEfficiency(EfficiencyEnum.Ascending);
                  else if (direction === 'desc') SetCommercialCompanyEfficiency(EfficiencyEnum.Descending);
                  else SetCommercialCompanyEfficiency(EfficiencyEnum.Off);
                }}
                className={styles.efficiencyColumn}
              />

              <SortableHeader
                title={translate("InfoLoomTwo.CommercialCompanyPanel[Profitability]", "Profitability")}
                sortState={profitabilitySortingOptions}
                onSort={direction => {
                  if (direction === 'asc') SetCommercialCompanyProfitability(ProfitabilityEnum.Ascending);
                  else if (direction === 'desc') SetCommercialCompanyProfitability(ProfitabilityEnum.Descending);
                  else SetCommercialCompanyProfitability(ProfitabilityEnum.Off);
                }}
                className={styles.profitabilityColumn}
              />

              <Tooltip tooltip={translate("InfoLoomTwo.CommercialCompanyPanel[LocationTooltip]", "Click to focus camera on the company's location")}>
                <div className={`${styles.headerCell} ${styles.locationColumn}`}>
                  <b>{translate("InfoLoomTwo.CommercialCompanyPanel[Location]", "Location")}</b>
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

export default CommercialCompanyDebugDataPanel;
