import React, { FC, ReactElement } from 'react';
import { useValue, trigger } from 'cs2/api';
import {Tooltip, Panel, DraggablePanelProps, Button, FloatingButton} from 'cs2/ui';
import {
  formatWords,
  formatNumber,
  formatPercentage2,
  formatPercentage1
} from 'mods/InfoLoomSections/utils/formatText';
import { CommercialCompanyDebug} from '../../../domain/CommercialCompanyDebugData';
import styles from './CommercialCompanyDebugData.module.scss';
import { CommercialCompanyDebugData} from "mods/bindings";
import {getModule} from "cs2/modding";
import {Entity, useCssLength} from 'cs2/utils';
import mod from "mod.json";
import {Resource} from "cs2/bindings"
import {EfficiencyFactorEnum} from "mods/domain/EfficiencyFactorInfo";
import { ResourceIcon } from '../../IndustrialSection/IndustrialCompanyUI/resourceIcons';


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
  resources: CommercialCompanyDebug;
  amount: number;
}

const ResourcesToolTip: FC<ResourcesToolTipProps> = ({ resources, amount }) => {
  return (
    <div className={styles.tooltipContent}>
      <div className={styles.tooltipText}>
        <p><strong>Resources: {resources.Resources}</strong></p>
        <p>Amount: {amount}</p>
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
        <p>Total Worth: {formatNumber(company.LastTotalWorth)}</p>
        <p>Total Wages: {formatNumber(company.TotalWages)}</p>
        <p>Daily Production: {formatNumber(company.ProductionPerDay)}</p>
        <p>Efficiency Value: {formatPercentage2(company.EfficiencyValue)}</p>
        <p>Concentration: {formatPercentage2(company.Concentration)}</p>
        <p>Output Resource: {company.OutputResourceName}</p>
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
          resources={company}
          amount={company.ResourceAmount}
        />
      }>
        <div className={styles.resourcesColumn}>
          <ResourceIcon resourceName={company.Resources} />
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
          <div className={styles.resourcesColumn}><b>Resources</b></div>
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
  const [visibleRange, setVisibleRange] = React.useState({ startIndex: 0, endIndex: 0 });

  // Only process/prepare data for visible items plus buffer
  const visibleCompanies = React.useMemo(() => {
    const bufferSize = 5;
    const start = Math.max(0, visibleRange.startIndex - bufferSize);
    const end = Math.min(companiesData.length, visibleRange.endIndex + bufferSize);
    return companiesData.slice(start, end);
  }, [companiesData, visibleRange.startIndex, visibleRange.endIndex]);

  const sizeProvider = useUniformSizeProvider(30, companiesData.length, 5);

  const handleRenderedRangeChange = React.useCallback((startIndex: number, endIndex: number) => {
    setVisibleRange({ startIndex, endIndex });
  }, []);

  const renderItem: RenderItemFn = (itemIndex, indexInRange) => {
    if (itemIndex < 0 || itemIndex >= companiesData.length) {
      return null;
    }

    // Calculate the adjusted index within visibleCompanies
    const bufferSize = 5;
    const adjustedIndex = itemIndex - Math.max(0, visibleRange.startIndex - bufferSize);

    // Only render if this index exists in our visible window
    if (adjustedIndex >= 0 && adjustedIndex < visibleCompanies.length) {
      return (
        <CompanyRow
          key={itemIndex}
          company={visibleCompanies[adjustedIndex]}
        />
      );
    }

    return null;
  };

  return (
    <Panel
      draggable
      onClose={onClose}
      initialPosition={{ x: 0.50, y: 0.50 }}
      className={styles.panel}
      header={<div className={styles.header}><span className={styles.headerText}>Commercial Companies</span></div>}
    >
      {!companiesData.length ? (
        <p className={styles.loadingText}>No Commercial Companies Found</p>
      ) : (
        <div>
          <TableHeader />
          <DataDivider />
          <div className={styles.virtualListContainer}>
            <VanillaVirtualList
              direction="vertical"
              sizeProvider={sizeProvider}
              renderItem={renderItem}
              style={{ height: '500rem' }}
              smooth
              onRenderedRangeChange={handleRenderedRangeChange}
            />
          </div>
          <DataDivider />
        </div>
      )}
    </Panel>
  );
};

export default CommercialCompanyDebugDataPanel;