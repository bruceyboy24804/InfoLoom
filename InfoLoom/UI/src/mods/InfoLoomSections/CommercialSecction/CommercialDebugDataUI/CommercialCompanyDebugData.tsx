import React, {FC, ReactElement, useCallback, useEffect, useMemo, useState} from 'react';
import {trigger, useValue} from 'cs2/api';
import {Button, DraggablePanelProps, Icon, Panel, Portal, Tooltip} from 'cs2/ui';
import {AutoNavigationScope, FocusActivation} from 'cs2/input';
import {formatNumber, formatPercentage1, formatPercentage2,} from 'mods/InfoLoomSections/utils/formatText';
import {CommercialCompanyDebug} from '../../../domain/CommercialCompanyDebugData';
import styles from './CommercialCompanyDebugData.module.scss';
import {
    CommercialCompanyDebugData,
    CommercialCompanyEfficiency,
    CommercialCompanyEmployee,
    CommercialCompanyIndexSorting,
    CommercialCompanyNameSorting,
    CommercialCompanyProfitability,
    CommercialCompanyServiceUsage,
    CommercialInput1Sorting,
    CommercialMaintenanceSorting,
    CommercialMoneySorting,
    CommercialOutputSorting,
    SetCommercialCompanyEfficiency,
    SetCommercialCompanyEmployee,
    SetCommercialCompanyNameSorting,
    SetCommercialCompanyProfitability,
    SetCommercialCompanyServiceUsage,
    SetCommercialInput1Sorting,
    SetCommercialMaintenanceSorting,
    SetCommercialMoneySorting,
    SetCommercialOutputSorting,
} from 'mods/bindings';
import {getModule} from 'cs2/modding';
import {Entity, useCssLength} from 'cs2/utils';
import {LocalizedNumber, Unit, useLocalization} from 'cs2/l10n';
import {EfficiencyFactorEnum} from 'mods/domain/EfficiencyFactorInfo';
import {SortingEnum} from 'mods/domain/SortingEnum';
import {CompanyNameSelector} from './Selectors/companyNameSelector';
import {ResourceSelector} from './Selectors/resourceSelector';
import {Localekeys} from "mods/locale"
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
const useUniformSizeProvider: (height: number, visible: number, extents: number) => SizeProvider = getModule(
  'game-ui/common/scrolling/virtual-list/virtual-list-size-provider.ts',
  'useUniformSizeProvider'
);

const DataDivider: FC = () => <div className={styles.dataDivider} />;

const focusEntity = (e: Entity) => {
  trigger('camera', 'focusEntity', e);
};

interface EfficiencyTooltipProps {
  company: CommercialCompanyDebug;
  translate: (key: string, fallback: string) => string | null;
}

const EfficiencyTooltip: FC<EfficiencyTooltipProps> = ({ company, translate }) => {
  // Helper function to get readable factor name from enum value with translation
  const getFactorName = (factor: EfficiencyFactorEnum): string => {
    switch (factor) {
      case EfficiencyFactorEnum.Destroyed:
        return translate(Localekeys.Destroyed, "Destroyed") || "Destroyed";
      case EfficiencyFactorEnum.Abandoned:
        return translate(Localekeys.Abandoned, "Abandoned") || "Abandoned";
      case EfficiencyFactorEnum.Disabled:
        return translate(Localekeys.Disabled, "Disabled") || "Disabled";
      case EfficiencyFactorEnum.Fire:
        return translate(Localekeys.Fire, "Fire") || "Fire";
      case EfficiencyFactorEnum.ServiceBudget:
        return translate(Localekeys.ServiceBudget, "Service Budget") || "Service Budget";
      case EfficiencyFactorEnum.NotEnoughEmployees:
        return translate(Localekeys.NotEnoughEmployees, "Not Enough Employees") || "Not Enough Employees";
      case EfficiencyFactorEnum.SickEmployees:
        return translate(Localekeys.SickEmployees, "Sick Employees") || "Sick Employees";
      case EfficiencyFactorEnum.EmployeeHappiness:
        return translate(Localekeys.EmployeeHappiness, "Employee Happiness") || "Employee Happiness";
      case EfficiencyFactorEnum.ElectricitySupply:
        return translate(Localekeys.ElectricitySupply, "Electricity Supply") || "Electricity Supply";
      case EfficiencyFactorEnum.ElectricityFee:
        return translate(Localekeys.ElectricityFee, "Electricity Fee") || "Electricity Fee";
      case EfficiencyFactorEnum.WaterSupply:
        return translate(Localekeys.WaterSupply, "Water Supply") || "Water Supply";
      case EfficiencyFactorEnum.DirtyWater:
        return translate(Localekeys.DirtyWater, "Dirty Water") || "Dirty Water";
      case EfficiencyFactorEnum.SewageHandling:
        return translate(Localekeys.SewageHandling, "Sewage Handling") || "Sewage Handling";
      case EfficiencyFactorEnum.WaterFee:
        return translate(Localekeys.WaterFee, "Water Fee") || "Water Fee";
      case EfficiencyFactorEnum.Garbage:
        return translate(Localekeys.Garbage, "Garbage") || "Garbage";
      case EfficiencyFactorEnum.Telecom:
        return translate(Localekeys.Telecom, "Telecom") || "Telecom";
      case EfficiencyFactorEnum.Mail:
        return translate(Localekeys.Mail, "Mail") || "Mail";
      case EfficiencyFactorEnum.MaterialSupply:
        return translate(Localekeys.MaterialSupply, "Material Supply") || "Material Supply";
      case EfficiencyFactorEnum.WindSpeed:
        return translate(Localekeys.WindSpeed, "Wind Speed") || "Wind Speed";
      case EfficiencyFactorEnum.WaterDepth:
        return translate(Localekeys.WaterDepth, "Water Depth") || "Water Depth";
      case EfficiencyFactorEnum.SunIntensity:
        return translate(Localekeys.SunIntensity, "Sun Intensity") || "Sun Intensity";
      case EfficiencyFactorEnum.NaturalResources:
        return translate(Localekeys.NaturalResources, "Natural Resources") || "Natural Resources";
      case EfficiencyFactorEnum.CityModifierSoftware:
        return translate(Localekeys.CityModifierSoftware, "City Modifier Software") || "City Modifier Software";
      case EfficiencyFactorEnum.CityModifierElectronics:
        return translate(Localekeys.CityModifierElectronics, "City Modifier Electronics") || "City Modifier Electronics";
      case EfficiencyFactorEnum.CityModifierIndustrialEfficiency:
        return translate(Localekeys.CityModifierIndustrialEfficiency, "City Modifier Industrial Efficiency") || "City Modifier Industrial Efficiency";
      case EfficiencyFactorEnum.CityModifierOfficeEfficiency:
        return translate(Localekeys.CityModifierOfficeEfficiency, "City Modifier Office Efficiency") || "City Modifier Office Efficiency";
      case EfficiencyFactorEnum.CityModifierHospitalEfficiency:
        return translate(Localekeys.CityModifierHospitalEfficiency, "City Modifier Hospital Efficiency") || "City Modifier Hospital Efficiency";
      case EfficiencyFactorEnum.SpecializationBonus:
        return translate(Localekeys.SpecializationBonus, "Specialization Bonus") || "Specialization Bonus";
      case EfficiencyFactorEnum.LackResources:
        return translate(Localekeys.LackResources, "Lack of Resources") || "Lack of Resources";
      default:
        return '';
    }
  };

  return (
    <div className={styles.tooltipContent}>
      <div className={styles.tooltipText}>
        {company.Factors &&
          company.Factors.map((factor, index) => {
            if (!factor) return null;

            const factorName = getFactorName(factor.Factor);

            return (
              <div key={index} className={styles.factorRow}>
                <span className={styles.factorName}>{factorName}</span>
                <span
                  className={factor.Value > 0 ? styles.positive : factor.Value < 0 ? styles.negative : styles.neutral}
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

interface CompanyMoneyTooltipProps {
  company: CommercialCompanyDebug;
}
const CompanyMoneyTooltip: FC<CompanyMoneyTooltipProps> = ({ company }) => {
 const {translate} = useLocalization();
  return (
    <div className={styles.tooltipContent}>
      <div className={styles.tooltipText}>
        <div className={styles.tooltipRow}> {translate(Localekeys.Income, "Income")} : <LocalizedNumber value={company.Income} unit={Unit.MoneyPerMonth} /></div> 
        <div className={styles.tooltipRow}> {translate(Localekeys.Worth, "Bank Balance")} : <LocalizedNumber value={company.Worth} unit={Unit.Money} /></div>  
        <div className={styles.tooltipRow}> {translate(Localekeys.Profit, "Profit")} : <LocalizedNumber value={company.Profit} unit={Unit.MoneyPerMonth} /></div>
        <div className={styles.tooltipRow}> {translate(Localekeys.WagePaid, "Wage Paid")} : <LocalizedNumber value={company.WagePaid} unit={Unit.MoneyPerMonth} /></div>
        <div className={styles.tooltipRow}> {translate(Localekeys.RentPaid, "Rent Paid")} : <LocalizedNumber value={company.RentPaid} unit={Unit.MoneyPerMonth} /></div>
        <div className={styles.tooltipRow}> {translate(Localekeys.ElectricityPaid, "Electricity Paid")} : <LocalizedNumber value={company.ElectricityPaid} unit={Unit.MoneyPerMonth} /></div>
        <div className={styles.tooltipRow}> {translate(Localekeys.WaterFeePaid, "Water Paid")} : <LocalizedNumber value={company.WaterPaid} unit={Unit.MoneyPerMonth} /></div>
        <div className={styles.tooltipRow}> {translate(Localekeys.SewageFeePaid, "Sewage Paid")} : <LocalizedNumber value={company.SewagePaid} unit={Unit.MoneyPerMonth} /></div>
        <div className={styles.tooltipRow}> {translate(Localekeys.GarbageFeePaid, "Garbage Paid")} : <LocalizedNumber value={company.GarbagePaid} unit={Unit.MoneyPerMonth} /></div>
        <div className={styles.tooltipRow}> {translate(Localekeys.TaxesPaid, "Tax Paid")} : <LocalizedNumber value={company.TaxPaid} unit={Unit.MoneyPerMonth} /></div>
        <div className={styles.tooltipRow}> {translate(Localekeys.ResourcesBoughtPaid, "Resources Bought Paid")} : <LocalizedNumber value={company.ResourcesBoughtPaid} unit={Unit.MoneyPerMonth} /></div>
        <div className={styles.tooltipRow}> {translate(Localekeys.Customers, "Current Customers")} : <LocalizedNumber value={company.CurrentCustomers} unit={Unit.Integer} /></div>
        <div className={styles.tooltipRow}> {translate(Localekeys.DailyCustomers, "Monthly Customers")} : <LocalizedNumber value={company.MonthlyCustomers} unit={Unit.IntegerPerMonth} /></div>
        
      </div>
    </div>
  );
};
interface SortableHeaderProps {
  title: string | null;
  sortState: SortingEnum;
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
    switch (sortState) {
      case 0: // Off
        onSort('asc');
        break;
      case 1: // Ascending
        onSort('desc');
        break;
      case 2: // Descending
        onSort('off');
        break;
      default:
        onSort('off');
        break;
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
        {sortState === 1 && <Icon src="coui://uil/Standard/ArrowSortLowDown.svg" className={styles.sortIcon} />}
        {sortState === 2 && <Icon src="coui://uil/Standard/ArrowSortHighDown.svg" className={styles.sortIcon} />}
      </div>
    </div>
  );
};

// Memoize the CompanyRow component to prevent re-renders when props haven't changed
const CompanyRowWithTranslation = React.memo(({ company }: { company: CommercialCompanyDebug }) => {
  const { translate } = useLocalization();
  const totalEfficiency = company.TotalEfficiency;

  // Pre-calculate processing resources to avoid inline filtering
  const inputResources = useMemo(
    () => company.ProcessResources?.filter(r => !r.isOutput) || [],
    [company.ProcessResources]
  );
  const outputResources = useMemo(
    () => company.ProcessResources?.filter(r => r.isOutput) || [],
    [company.ProcessResources]
  );

  const processingTooltip = useMemo(
    () => (
      <div className={styles.tooltipContent}>
        <div className={styles.tooltipText}>
          <p>
            <strong>
              {translate(Localekeys.ProcessingResourcesTitle, 'Processing Resources')}
            </strong>
          </p>
          {outputResources.length > 0 ? (
            <div>
              {outputResources.map((resource, index) => (
                <p cohinline="cohinline" key={`output-${index}`}>
                  {index === 0
                    ? (translate(Localekeys.Output, 'Output:')) + ' ' : ''}
                  {resource.resourceName}: {resource.amount}
                </p>
              ))}
            </div>
          ) : (
            <p>
              {translate(Localekeys.NoOutputResources, 'No output resources')}
            </p>
          )}

          {inputResources.length > 0 ? (
            <div>
              {inputResources.map((resource, index) => (
                <p key={`input-${index}`} cohinline="cohinline">
                  {index === 0
                    ? (translate(Localekeys.Input, 'Input:') + ' ') : ''}
                  {resource.resourceName}: {resource.amount}
                </p>
              ))}
            </div>
          ) : (
            <p>
              {translate(Localekeys.NoInputResources, 'No input resources')}
            </p>
          )}
        </div>
      </div>
    ),
    [inputResources, outputResources, translate]
  );

  const efficiencyTooltip = useMemo(
    () => <EfficiencyTooltip company={company} translate={translate} />,
    [company, translate]
  );
  const profitabilityTooltip = useMemo(() => <ProfitabilityTooltip company={company} />, [company]);

  const getEfficiencyClass = (efficiency: number) => {
    if (efficiency >= 0.8) return styles.efficiencyHigh;
    if (efficiency >= 0.5) return styles.efficiencyMedium;
    return styles.efficiencyLow;
  };

  const getProfitabilityClass = (profitability: number) => {
    return profitability >= 0 ? styles.profitabilityPositive : styles.profitabilityNegative;
  };

  const serviceUsagePercentage = company.MaxService > 0 ? 1 - company.ServiceAvailable / company.MaxService : 0;
  const serviceUsage = formatPercentage1(serviceUsagePercentage);
  const getServiceClass = (service: number) => {
    if (service >= 0.8) return styles.serviceHigh;
    if (service >= 0.5) return styles.serviceMedium;
    return styles.serviceLow;
  };

  return (
    <div className={styles.row}>
      {/* Name Column */}
      <div className={styles.nameColumn}>{company.CompanyName}</div>

      {/* Service Usage Column */}
      <Tooltip tooltip={<ServiceTooltip serviceAvailable={company.ServiceAvailable} maxService={company.MaxService} />}>
        <div className={styles.serviceColumn}>
          <span className={getServiceClass(serviceUsagePercentage)}>{serviceUsage}</span>
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

      {/* Money Column */}
      <Tooltip tooltip={<CompanyMoneyTooltip company={company} />}>
         <div className={styles.moneyColumn}>
        <div className={styles.resourceGroup}>
          <span className={styles.resourceAmount}>{formatNumber(company.MoneyAmount)}</span>
        </div>
      </div> 
      </Tooltip>  
      

      {/* Input 1 Column */}
      <div className={styles.input1Column}>
        {company.Input1Resources && company.Input1Resources.length > 0 ? (
          <div className={styles.resourceGroup}>
            {company.Input1Resources.map((r, i) => (
              <div key={`input1-${i}`} className={styles.resourceItem}>
                <Icon src={r.Icon} className={styles.resourceIcon} />
                <span className={styles.resourceAmount}>{formatNumber(r.Amount)}</span>
              </div>
            ))}
          </div>
        ) : (
          <div className={styles.emptyGroup}></div>
        )}
      </div>

      {/* Output Column */}
      <div className={styles.outputColumn}>
        {company.OutputResources && company.OutputResources.length > 0 ? (
          <div className={styles.resourceGroup}>
            {company.OutputResources.map((r, i) => (
              <div key={`output-${i}`} className={styles.resourceItem}>
                <Icon src={r.Icon} className={styles.resourceIcon} />
                <span className={styles.resourceAmount}>{formatNumber(r.Amount)}</span>
              </div>
            ))}
          </div>
        ) : (
          <div className={styles.emptyGroup}></div>
        )}
      </div>

      {/* Maintenance Column */}
      <div className={styles.maintenanceColumn}>
        {company.MaintenanceResources && company.MaintenanceResources.length > 0 ? (
          <div className={styles.resourceGroup}>
            {company.MaintenanceResources.map((r, i) => (
              <div key={`maint-${i}`} className={styles.resourceItem}>
                <Icon src={r.Icon} className={styles.resourceIcon} />
                <span className={styles.resourceAmount}>{formatNumber(r.Amount)}</span>
              </div>
            ))}
          </div>
        ) : (
          <div className={styles.emptyGroup}></div>
        )}
      </div>

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
            translate(Localekeys.None, 'None'))}
        </div>
      </Tooltip>

      {/* Efficiency Column */}
      <Tooltip tooltip={efficiencyTooltip}>
        <div className={styles.efficiencyColumn}>
          <span className={getEfficiencyClass(totalEfficiency)}>{formatPercentage2(totalEfficiency)}</span>
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
  const moneySortingOptions = useValue(CommercialMoneySorting);
  const input1SortingOptions = useValue(CommercialInput1Sorting);
  const outputSortingOptions = useValue(CommercialOutputSorting);
  const maintenanceSortingOptions = useValue(CommercialMaintenanceSorting);

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
      // Don't limit by item count - let virtual list fill available space
      const calculatedHeight = heightFull - 250;
      return Math.max(200, calculatedHeight); // Minimum 200px
    }, [heightFull]);
  
    // Size provider - row height in rem units, total items count, 5 extra items for smooth scrolling
    const sizeProvider = useUniformSizeProvider(useCssLength('32rem'), companiesData.length, 5);
  
    const handleRenderedRangeChange = useCallback((startIndex: number, endIndex: number) => {
      setVisibleRange({ startIndex, endIndex });
    }, []);
    const renderItem: RenderItemFn = useCallback(
      (itemIndex: number, indexInRange: number) => {
        if (itemIndex < 0 || itemIndex >= companiesData.length) return null;
  
        const company = companiesData[itemIndex];
        if (!company) return null;
  
        return <CompanyRowWithTranslation key={`industrial-company-${itemIndex}`} company={company} />;
      },
      [companiesData]
    );

  // Early return for no data (use translate + fallback)
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
              <span className={styles.headerText}>
                {translate(Localekeys.CommercialCompanies, 'Commercial Companies')}
              </span>
            </div>
          }
        >
          <p className={styles.loadingText}>
            {translate(Localekeys.NoCompanyFound, 'No Companies Found')}
          </p>
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
              {translate(Localekeys.CommercialCompanies, 'Commercial Companies')}
            </span>
          </div>
        }
      >
        <div>
          <div className={styles.resourceFilterRow}>
                      <div className={styles.nameColumn}>
                        <CompanyNameSelector />
                      </div>
                      <div className={styles.serviceColumn}></div>
                      <div className={styles.employeeColumn}></div>
                      <div className={styles.vehicleColumn}></div>
                      <div className={styles.moneyColumn}></div>
                      <div className={styles.input1Column}>
                        <ResourceSelector 
                          resourceType="input1" 
                          label="Input 1" 
                          tooltipText="Select an input 1 resource to filter companies by their first input."
                        />
                      </div>
                      <div className={styles.outputColumn}>
                        <ResourceSelector 
                          resourceType="output" 
                          label="Output" 
                          tooltipText="Select an output resource to filter companies by what they produce."
                        />
                      </div>
                    </div>
          <div className={styles.tableHeader}>
            <div className={styles.headerRow}>
              <SortableHeader
                title={translate(Localekeys.Name, 'Name')}
                sortState={nameSortingOptions}
                onSort={direction => {
                  if (direction === 'asc') SetCommercialCompanyNameSorting(SortingEnum.Ascending);
                  else if (direction === 'desc') SetCommercialCompanyNameSorting(SortingEnum.Descending);
                  else SetCommercialCompanyNameSorting(SortingEnum.Off);
                }}
                className={styles.nameColumn}
              />

              <SortableHeader
                title={translate(Localekeys.ServiceUsage, 'Service Usage')}
                sortState={serviceUsageSortingOptions}
                onSort={direction => {
                  if (direction === 'asc') SetCommercialCompanyServiceUsage(SortingEnum.Ascending);
                  else if (direction === 'desc') SetCommercialCompanyServiceUsage(SortingEnum.Descending);
                  else SetCommercialCompanyServiceUsage(SortingEnum.Off);
                }}
                className={styles.serviceColumn}
              />

              <SortableHeader
                title={translate(Localekeys.Employees, 'Employees')}
                sortState={employeeSortingOptions}
                onSort={direction => {
                  if (direction === 'asc') SetCommercialCompanyEmployee(SortingEnum.Ascending);
                  else if (direction === 'desc') SetCommercialCompanyEmployee(SortingEnum.Descending);
                  else SetCommercialCompanyEmployee(SortingEnum.Off);
                }}
                className={styles.employeeColumn}
              />

              <Tooltip
                tooltip={translate(
                  Localekeys.VehiclesTooltip,
                  'Current vehicle count vs maximum vehicle capacity for deliveries and transportation'
                )}
              >
                <div className={`${styles.headerCell} ${styles.vehicleColumn}`}>
                  <b>{translate(Localekeys.Vehicles, 'Vehicles')}</b>
                </div>
              </Tooltip>
              <SortableHeader
                title={translate(Localekeys.Resource_Money, 'Money')}
                sortState={moneySortingOptions}
                onSort={direction => {
                  if (direction === 'asc') SetCommercialMoneySorting(SortingEnum.Ascending);
                  else if (direction === 'desc') SetCommercialMoneySorting(SortingEnum.Descending);
                  else SetCommercialMoneySorting(SortingEnum.Off);
                }}
                className={styles.moneyColumn}
              />

              <SortableHeader
                title={translate(Localekeys.Input, 'Input')}
                sortState={input1SortingOptions}
                onSort={direction => {
                  if (direction === 'asc') SetCommercialInput1Sorting(SortingEnum.Ascending);
                  else if (direction === 'desc') SetCommercialInput1Sorting(SortingEnum.Descending);
                  else SetCommercialInput1Sorting(SortingEnum.Off);
                }}
                className={styles.input1Column}
              />
              <SortableHeader
                title={translate(Localekeys.Output, 'Output')}
                sortState={outputSortingOptions}
                onSort={direction => {
                  if (direction === 'asc') SetCommercialOutputSorting(SortingEnum.Ascending);
                  else if (direction === 'desc') SetCommercialOutputSorting(SortingEnum.Descending);
                  else SetCommercialOutputSorting(SortingEnum.Off);
                }}
                className={styles.outputColumn}
              />
              <SortableHeader
                title={translate(Localekeys.Maintenance, 'Maintenance')}
                sortState={maintenanceSortingOptions}
                onSort={direction => {
                  if (direction === 'asc') SetCommercialMaintenanceSorting(SortingEnum.Ascending);
                  else if (direction === 'desc') SetCommercialMaintenanceSorting(SortingEnum.Descending);
                  else SetCommercialMaintenanceSorting(SortingEnum.Off);
                }}
                className={styles.maintenanceColumn}
              />
              <Tooltip
                tooltip={
                  translate(
                    Localekeys.ProcessingTooltip,
                    'Input and output resources processed by this company in the production chain'
                  )
                }
              >
                <div className={`${styles.headerCell} ${styles.processingColumn}`}>
                  <b>{translate(Localekeys.Processing, 'Processing')}</b>
                </div>
              </Tooltip>

              <SortableHeader
                title={translate(Localekeys.Efficiency, 'Efficiency')}
                sortState={efficiencySortingOptions}
                onSort={direction => {
                  if (direction === 'asc') SetCommercialCompanyEfficiency(SortingEnum.Ascending);
                  else if (direction === 'desc') SetCommercialCompanyEfficiency(SortingEnum.Descending);
                  else SetCommercialCompanyEfficiency(SortingEnum.Off);
                }}
                className={styles.efficiencyColumn}
              />

              <SortableHeader
                title={translate(Localekeys.Profitability, 'Profitability')}
                sortState={profitabilitySortingOptions}
                onSort={direction => {
                  if (direction === 'asc') SetCommercialCompanyProfitability(SortingEnum.Ascending);
                  else if (direction === 'desc') SetCommercialCompanyProfitability(SortingEnum.Descending);
                  else SetCommercialCompanyProfitability(SortingEnum.Off);
                }}
                className={styles.profitabilityColumn}
              />

              
                <div className={`${styles.headerCell} ${styles.locationColumn}`}>
                  <b>{translate(Localekeys.Location, 'Location')}</b>
                </div>
              
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
