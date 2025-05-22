import React, { FC, ReactElement } from 'react';
import { useValue, trigger } from 'cs2/api';
import {Tooltip, Panel, DraggablePanelProps, Button, Icon} from 'cs2/ui';
import {
  formatWords,
  formatNumber,
  formatPercentage2,
  formatPercentage1
} from 'mods/InfoLoomSections/utils/formatText';
import { IndustrialCompanyDebug, ProcessResourceInfo} from '../../../domain/IndustrialCompanyDebugData';
import { EfficiencyFactorEnum, EfficiencyFactorInfo } from '../../../domain/EfficiencyFactorInfo';
import styles from './IndustrialCompany.module.scss';
import { IndustrialCompanyDebugData } from "mods/bindings";
import {getModule} from "cs2/modding";
import {Entity, useCssLength} from 'cs2/utils';
import mod from "mod.json";
import {ResourceIcon} from "mods/InfoLoomSections/IndustrialSection/IndustrialCompanyUI/resourceIcons";


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
  company: IndustrialCompanyDebug;
}

const EfficiencyTooltip: FC<EfficiencyTooltipProps> = ({ company }) => {
  // Helper function to get readable factor name from enum value
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
                factor.Value > 0 ? styles.factorPositive :
                  factor.Value < 0 ? styles.factorNegative :
                    styles.factorNeutral
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
  resources: string;
  amount: number;
}

const ResourcesToolTip: FC<ResourcesToolTipProps> = ({ resources, amount }) => {
  return (
    <div className={styles.tooltipContent}>
      <div className={styles.tooltipText}>
        <p><strong>Resources: {resources}</strong></p>
        <p>Amount: {amount}</p>
      </div>
    </div>
  );
};

// First, create a ProcessingInfoTooltip component
interface ProcessingInfoTooltipProps {
  inputResources: ProcessResourceInfo[];
  outputResources: ProcessResourceInfo[];
}

const ProcessingInfoTooltip: FC<ProcessingInfoTooltipProps> = ({ inputResources, outputResources }) => {
  return (
    <div className={styles.tooltipContent}>
      <div className={styles.tooltipText}>
        <p><strong>Processing Resources</strong></p>
        {outputResources.length > 0 ? (
          <div>
            <>
              {outputResources.map((resource, index) => (
                <p cohinline="cohinline" key={`output-${index}`}>
                  {index === 0 ? "Output: " : ""}{resource.resourceName}: {resource.amount}
                </p>
              ))}
            </>
          </div>
        ) : (
          <p>No output resources</p>
        )}

        {inputResources.length > 0 ? (
          <div>
            {inputResources.map((resource, index) => (
              <p key={`input-${index}`} cohinline="cohinline">
                {index === 0 ? "Input: " : ""}{resource.resourceName}: {resource.amount}
              </p>
            ))}
          </div>
        ) : (
          <p>No input resources</p>
        )}
      </div>
    </div>
  );
};

interface ProfitabilityTooltipProps {
  company: IndustrialCompanyDebug;
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
        {company.IsExtractor && <p>Extractor Type: Yes</p>}
      </div>
    </div>
  );
};

interface CompanyRowProps {
  company: IndustrialCompanyDebug;
}

// Memoize the CompanyRow component to prevent re-renders when props haven't changed
const CompanyRow: FC<CompanyRowProps> = React.memo(({ company }) => {
  const totalEfficiency = company.TotalEfficiency;
  const handleNavigate = () => {
    trigger(mod.id, "GoTo", company.EntityId); // Call C# method
  };
  const employeeRatio = company.MaxWorkers > 0 ?
    formatPercentage1(company.TotalEmployees / company.MaxWorkers) : "N/A";

  return (
    <div className={styles.row}>
      <div className={styles.nameColumn}>
        {company.CompanyName}

      </div>
      <div className={styles.employeeColumn}>

        {`${formatNumber(company.TotalEmployees)}/${formatNumber(company.MaxWorkers)}`}

      </div>

      <div className={styles.vehicleColumn}>
        {`${formatNumber(company.VehicleCount)}/${formatNumber(company.VehicleCapacity)}`}
      </div>

      <Tooltip tooltip={
        <ResourcesToolTip
          resources={company.Resources}
          amount={company.ResourceAmount}
        />
      }>
        <div className={styles.resourcesColumn}>
          <ResourceIcon resourceName={company.Resources} />
        </div>
      </Tooltip>
      <Tooltip tooltip={
        <ProcessingInfoTooltip
          inputResources={company.ProcessResources?.filter(r => !r.isOutput) || []}
          outputResources={company.ProcessResources?.filter(r => r.isOutput) || []}
        />
      }>
        <div className={styles.processingColumn}>
          {company.ProcessResources ? (
            <div className={styles.processingWrapper}>
              {(() => {
                const inputResources = company.ProcessResources.filter(r => !r.isOutput);
                const outputResources = company.ProcessResources.filter(r => r.isOutput);

                return (
                  <div className={styles.processContainer}>
                    {inputResources.length > 0 ? (
                      <div className={styles.resourceGroup}>
                        {inputResources.map((r, i) => (
                          <div key={`input-${i}`} className={styles.resourceItem}>
                            <ResourceIcon resourceName={r.resourceName} />
                            {i < inputResources.length - 1 &&
                              <span className={styles.separator}>+</span>
                            }
                          </div>
                        ))}
                      </div>
                    ) : <div className={styles.emptyGroup}></div>}

                    <div className={styles.divider}>→</div>

                    {outputResources.length > 0 ? (
                      <div className={styles.resourceGroup}>
                        {outputResources.map((r, i) => (
                          <div key={`output-${i}`} className={styles.resourceItem}>
                            <ResourceIcon resourceName={r.resourceName} />
                            {i < outputResources.length - 1 &&
                              <span className={styles.separator}>+</span>
                            }
                          </div>
                        ))}
                      </div>
                    ) : <div className={styles.emptyGroup}></div>}
                  </div>
                );
              })()}
            </div>
          ) : "None"}
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
        <Tooltip tooltip="Current employees vs maximum employee capacity">
          <div className={styles.employeeColumn}><b>Employees</b></div>
        </Tooltip>
        <Tooltip tooltip="Current vehicle count vs maximum vehicle capacity">
          <div className={styles.vehicleColumn}><b>Vehicles</b></div>
        </Tooltip>
        <Tooltip tooltip="Resources used by this commercial building">
          <div className={styles.resourcesColumn}><b>Resources</b></div>
        </Tooltip>
        <Tooltip tooltip="Input/output resources processed by this industrial company">
          <div className={styles.processingColumn}><b>Processing</b></div>
        </Tooltip>
        <Tooltip tooltip="Overall efficiency based on multiple factors">
          <div className={styles.efficiencyColumn}><b>Efficiency</b></div>
        </Tooltip>
        <Tooltip tooltip="Financial performance and worth of the industrial company">
          <div className={styles.profitabilityColumn}><b>Profitability</b></div>
        </Tooltip>
        <Tooltip tooltip="Location of the industrial company">
          <div className={styles.locationColumn}><b>Location</b></div>
        </Tooltip>
      </div>
    </div>
  );
};

const IndustrialCompany: FC<DraggablePanelProps> = ({ onClose }) => {
  const companiesData = useValue(IndustrialCompanyDebugData);
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
    return (
      <CompanyRow
        key={itemIndex}
        company={companiesData[itemIndex]}
      />
    );
  };

  return (
    <Panel
      draggable
      onClose={onClose}
      initialPosition={{ x: 0.50, y: 0.50 }}
      className={styles.panel}
      header={<div className={styles.header}><span className={styles.headerText}>Industrial Companies</span></div>}
    >
      {!companiesData.length ? (
        <p className={styles.loadingText}>Please wait...</p>
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

export default IndustrialCompany;