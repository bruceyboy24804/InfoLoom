import React, { FC, ReactElement, useState, useEffect, useMemo, useCallback } from 'react';
import { useValue, trigger } from 'cs2/api';
import { Tooltip, Panel, DraggablePanelProps, Button, FloatingButton, Icon, Portal } from 'cs2/ui';
import { AutoNavigationScope, FocusActivation } from "cs2/input";
import {
  formatWords,
  formatNumber,
  formatPercentage2,
  formatPercentage1
} from 'mods/InfoLoomSections/utils/formatText';
import { CommercialCompanyDebug, ResourceInfo} from '../../../domain/CommercialCompanyDebugData';
import styles from './CommercialCompanyDebugData.module.scss';
import { CommercialCompanyDebugData} from "mods/bindings";
import {getModule} from "cs2/modding";
import {Entity, useCssLength} from 'cs2/utils';
import mod from "mod.json";
import {EfficiencyFactorEnum} from "mods/domain/EfficiencyFactorInfo";


// Import VirtualList components
type SizeProvider = {getRenderedRange: () => ({offset: number, size: number, startIndex: number, endIndex: number}), getTotalSize: () => number};
type RenderItemFn = (itemIndex: number, indexInRange: number) => ReactElement | null;
type RenderedRangeChangedCallback = (startIndex: number, endIndex: number) => void;

interface VirtualListProps {
  className?: string,
  controller?: any;
  direction?: "vertical" | "horizontal",
  onRenderedRangeChange?: RenderedRangeChangedCallback;
  renderItem: RenderItemFn,
  sizeProvider: SizeProvider,
  smooth?: boolean;
  style?: Record<string, any>;
}

const VanillaVirtualList: FC<VirtualListProps> = getModule("game-ui/common/scrolling/virtual-list/virtual-list.tsx", "VirtualList");
const useUniformSizeProvider: (height: number, visible: number, extents: number) => SizeProvider =
  getModule("game-ui/common/scrolling/virtual-list/virtual-list-size-provider.ts", "useUniformSizeProvider");

const DataDivider: FC = () => (
  <div className={styles.dataDivider} />
);

const focusEntity = (e: Entity) => {
  trigger("camera", "focusEntity", e);
};

interface EfficiencyTooltipProps {
  company: CommercialCompanyDebug;
}

const EfficiencyTooltip: FC<EfficiencyTooltipProps> = ({ company }) => {
  const getFactorName = (factor: EfficiencyFactorEnum): string => {
    switch (factor) {
      case EfficiencyFactorEnum.Destroyed: return "Destroyed";
      case EfficiencyFactorEnum.Abandoned: return "Abandoned";
      case EfficiencyFactorEnum.Disabled: return "Disabled";
      case EfficiencyFactorEnum.Fire: return "Fire";
      case EfficiencyFactorEnum.ServiceBudget: return "Service Budget";
      case EfficiencyFactorEnum.NotEnoughEmployees: return "Not Enough Employees";
      case EfficiencyFactorEnum.SickEmployees: return "Sick Employees";
      case EfficiencyFactorEnum.EmployeeHappiness: return "Employee Happiness";
      case EfficiencyFactorEnum.ElectricitySupply: return "Electricity Supply";
      case EfficiencyFactorEnum.ElectricityFee: return "Electricity Fee";
      case EfficiencyFactorEnum.WaterSupply: return "Water Supply";
      case EfficiencyFactorEnum.DirtyWater: return "Dirty Water";
      case EfficiencyFactorEnum.SewageHandling: return "Sewage Handling";
      case EfficiencyFactorEnum.WaterFee: return "Water Fee";
      case EfficiencyFactorEnum.Garbage: return "Garbage";
      case EfficiencyFactorEnum.Telecom: return "Telecommunications";
      case EfficiencyFactorEnum.Mail: return "Mail";
      case EfficiencyFactorEnum.MaterialSupply: return "Material Supply";
      case EfficiencyFactorEnum.WindSpeed: return "Wind Speed";
      case EfficiencyFactorEnum.WaterDepth: return "Water Depth";
      case EfficiencyFactorEnum.SunIntensity: return "Sun Intensity";
      case EfficiencyFactorEnum.NaturalResources: return "Natural Resources";
      case EfficiencyFactorEnum.CityModifierSoftware: return "City Modifier: Software";
      case EfficiencyFactorEnum.CityModifierElectronics: return "City Modifier: Electronics";
      case EfficiencyFactorEnum.CityModifierIndustrialEfficiency: return "Industrial Efficiency";
      case EfficiencyFactorEnum.CityModifierOfficeEfficiency: return "Office Efficiency";
      case EfficiencyFactorEnum.CityModifierHospitalEfficiency: return "Hospital Efficiency";
      case EfficiencyFactorEnum.SpecializationBonus: return "Specialization Bonus";
      case EfficiencyFactorEnum.Count: return "Count";
      default: return "Unknown Factor";
    }
  };

  return (
    <div className={styles.tooltipContent}>
      <div className={styles.tooltipText}>
        <p>Factors affecting efficiency:</p>
        {company.Factors && company.Factors.map((factor, index) => {
          if (!factor) return null;

          // Get a readable name for the factor from the enum value
          const factorName = getFactorName(factor.Factor);

          return (
            <div key={index} className={styles.factorRow}>
              <span className={styles.factorName}>
                {factorName}
              </span>
              <span className={
                factor.Value > 0 ? styles.positive :
                  factor.Value < 0 ? styles.negative :
                    styles.neutral
              }>
                {factor.Value > 0 ? '+' : ''}{formatPercentage2(factor.Value)}
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
        <p><strong>Resources</strong></p>
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
  const serviceUsagePercentage = maxService > 0 ?
    1 - (serviceAvailable / maxService) : 0;

  return (
    <div className={styles.tooltipContent}>
      <div className={styles.tooltipText}>
        <p><strong>Service Usage: {formatPercentage1(serviceUsagePercentage)}</strong></p>
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
        <p><strong>Financial Information</strong></p>
        <p>Last Total Worth: {formatNumber(company.LastTotalWorth)}</p>
        <p>Total Wages: {formatNumber(company.TotalWages)}</p>
        <p>Daily Production: {formatNumber(company.ProductionPerDay)}</p>
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
  const handleNavigate = () => {
    trigger(mod.id, "GoTo", company.EntityId); // Call C# method
  };
  // Calculate service usage (inverted from availability)
  const serviceUsagePercentage = company.MaxService > 0 ?
    1 - (company.ServiceAvailable / company.MaxService) : 0;
  const serviceUsage = formatPercentage1(serviceUsagePercentage);

  const employeeRatio = company.MaxWorkers > 0 ?
    formatPercentage1(company.TotalEmployees / company.MaxWorkers) : "N/A";

  return (
    <div className={styles.row}>
      <div className={styles.nameColumn}>
        {company.CompanyName}
      </div>

      <Tooltip tooltip={
        <ServiceTooltip
          serviceAvailable={company.ServiceAvailable}
          maxService={company.MaxService}
        />
      }>
        <div className={styles.serviceColumn}>
          {serviceUsage}
        </div>
      </Tooltip>

      <div className={styles.employeeColumn}>
        {`${formatNumber(company.TotalEmployees)}/${formatNumber(company.MaxWorkers)}`}
      </div>

      <div className={styles.vehicleColumn}>
        {`${formatNumber(company.VehicleCount)}/${formatNumber(company.VehicleCapacity)}`}
      </div>

      <Tooltip tooltip={
        <ResourcesToolTip
          resources={company.Resources || []}
        />
      }>
        <div className={styles.resourcesColumn}>
          {company.Resources && company.Resources.length > 0 ? (
            <div className={styles.processingWrapper}>
              <div className={styles.processContainer}>
                <div className={styles.resourceGroup}>
                  {company.Resources.map((r, i) => (
                    <div key={`resource-${i}`} className={styles.resourceItem}>
                      <Icon src={r.Icon} />
                    </div>
                  ))}
                </div>
              </div>
            </div>
          ) : (
            <Icon src={company.ResourceIcon} />
          )}
        </div>
      </Tooltip>

      <Tooltip tooltip={
        <EfficiencyTooltip company={company} />
      }>
        <div className={styles.efficiencyColumn}>
          {formatPercentage2(totalEfficiency)}
        </div>
      </Tooltip>

      <Tooltip tooltip={
        <ProfitabilityTooltip company={company} />
      }>
        <div className={styles.profitabilityColumn}>
          {formatPercentage2(company.Profitability)}
        </div>
      </Tooltip>

      <div className={styles.locationColumn}>
        <Button
          variant={"icon"}
          src={"Media/Game/Icons/MapMarker.svg"}
          onSelect={() => focusEntity(company.EntityId)}
          className={styles.magnifierIcon}
        />
      </div>
    </div>
  );
});

const TableHeader: FC = () => {
  return (
    <div className={styles.tableHeader}>
      <div className={styles.headerRow}>
        <div className={styles.nameColumn}><b>Company Name</b></div>
        <Tooltip tooltip="The service usage level in this commercial building">
          <div className={styles.serviceColumn}><b>Service Usage</b></div>
        </Tooltip>
        <Tooltip tooltip="Current employees vs maximum employee capacity">
          <div className={styles.employeeColumn}><b>Employees</b></div>
        </Tooltip>
        <Tooltip tooltip="Current vehicle count vs maximum vehicle capacity">
          <div className={styles.vehicleColumn}><b>Vehicles</b></div>
        </Tooltip>
        <Tooltip tooltip="Resources used by this commercial building">
          <div className={styles.resourcesColumn}><b>Resource Storage</b></div>
        </Tooltip>
        <Tooltip tooltip="Overall efficiency based on multiple factors">
          <div className={styles.efficiencyColumn}><b>Efficiency</b></div>
        </Tooltip>
        <Tooltip tooltip="Financial performance and worth of the commercial company">
          <div className={styles.profitabilityColumn}><b>Profitability</b></div>
        </Tooltip>
        <Tooltip tooltip="Location of the commercial company">
          <div className={styles.locationColumn}><b>Location</b></div>
        </Tooltip>
      </div>
    </div>
  );
};

const CommercialCompanyDebugDataPanel: FC<DraggablePanelProps> = ({ onClose }) => {
  const companiesData = useValue(CommercialCompanyDebugData);
  const [visibleRange, setVisibleRange] = useState({ startIndex: 0, endIndex: 0 });
  const [heightFull, setHeightFull] = useState(600); // Default fallback height

  // Dynamic height calculation
  const calculateHeights = useCallback(() => {
    const wrapperElement = document.querySelector(".info-layout_BVk") as HTMLElement | null;
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
    const calculatedHeight = Math.min(
      30 * companiesData.length, 
      heightFull - 250
    );
    
    return [companies, Math.max(200, calculatedHeight)]; // Minimum 200px
  }, [companiesData, visibleRange.startIndex, visibleRange.endIndex, heightFull]);

  // Size provider using proper CSS units
  const sizeProvider = useUniformSizeProvider(
    useCssLength("30rem"),
    companiesData.length,
    5
  );

  const handleRenderedRangeChange = useCallback((startIndex: number, endIndex: number) => {
    setVisibleRange({ startIndex, endIndex });
  }, []);

  const renderItem: RenderItemFn = useCallback((itemIndex: number, indexInRange: number) => {
    if (itemIndex < 0 || itemIndex >= companiesData.length) return null;
    
    const bufferSize = 5;
    const adjustedIndex = itemIndex - Math.max(0, visibleRange.startIndex - bufferSize);
    
    if (adjustedIndex < 0 || adjustedIndex >= visibleCompanies.length) return null;
    
    return (
      <CompanyRow
        key={`company-${itemIndex}`}
        company={visibleCompanies[adjustedIndex]}
      />
    );
  }, [companiesData.length, visibleRange.startIndex, visibleCompanies]);

  // Early return for no data
  if (!companiesData.length) {
    return (
      <Portal>
        <Panel
          draggable
          onClose={onClose}
          initialPosition={{ x: 0.50, y: 0.50 }}
          className={styles.panel}
          header={<div className={styles.header}><span className={styles.headerText}>Commercial Companies</span></div>}
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
        initialPosition={{ x: 0.50, y: 0.50 }}
        className={styles.panel}
        header={<div className={styles.header}><span className={styles.headerText}>Commercial Companies</span></div>}
      >
        <div>
          <TableHeader />
          <DataDivider />
          <div className={styles.virtualListContainer}>
            <AutoNavigationScope activation={FocusActivation.AnyChildren}>
              <VanillaVirtualList
                direction="vertical"
                sizeProvider={sizeProvider}
                renderItem={renderItem}
                style={{ 
                  maxHeight: `${maxListHeight}px`
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