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
import {EfficiencyFactorInfo} from '../../../domain/EfficiencyFactorInfo';
import styles from './IndustrialComany.module.scss';
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
  return (
    <div className={styles.tooltipContent}>
      <div className={styles.tooltipText}>
        <p>Factors affecting efficiency:</p>
        {company.Factors && company.Factors.map((factor, index) => (
          <div key={index} className={styles.factorRow}>
            <span className={styles.factorName}>{formatWords(factor.factor.toString())} </span>                        <span className={
            factor.value > 0 ? styles.positive :
              factor.value < 0 ? styles.negative :
                styles.neutral
          }>
                {factor.value > 0 ? '+' : ''}{formatPercentage2(factor.value)}
            </span>
            <span className={styles.factorResult}>{formatPercentage2(factor.result)}</span>
          </div>
        ))}
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

interface CompanyRowProps {
  company: IndustrialCompanyDebug;
}


const CompanyRow: FC<CompanyRowProps> = ({ company }) => {
  const totalEfficiency = company.TotalEfficiency;
  const handleNavigate = () => {
    trigger(mod.id, "GoTo", company.EntityId); // Call C# method
  };
  const employeeRatio = company.MaxWorkers > 0 ?
    formatPercentage1(company.TotalEmployees / company.MaxWorkers) : "N/A";

  return (
    <div className={styles.row}>
      <div className={styles.nameColumn}>
      {company.CompanyName} <img src={company.CompanyIcon} alt={""} className={styles.magnifierIcon} />

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
          {company.Resources}
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
            <>
              {company.ProcessResources.filter(r => !r.isOutput).length > 0 && (
                <span>
            {company.ProcessResources.filter(r => !r.isOutput).map((r, i) => (
              <span key={`input-${i}`}>{r.resourceName}{i < company.ProcessResources.filter(r => !r.isOutput).length - 1 ? ", " : ""}</span>
            ))}
          </span>
              )}
              {company.ProcessResources.filter(r => !r.isOutput).length > 0 &&
                company.ProcessResources.filter(r => r.isOutput).length > 0 &&
                <span style={{margin: "0 4rem"}}> → </span>
              }
              {company.ProcessResources.filter(r => r.isOutput).length > 0 && (
                <span>
            {company.ProcessResources.filter(r => r.isOutput).map((r, i) => (
              <span key={`output-${i}`}>{r.resourceName}{i < company.ProcessResources.filter(r => r.isOutput).length - 1 ? ", " : ""}</span>
            ))}
          </span>
              )}
            </>
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
};

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
        <Tooltip tooltip="Location of the commercial company">
          <div className={styles.locationColumn}><b>Location</b></div>
        </Tooltip>
      </div>
    </div>
  );
};

const IndustrialCompany: FC<DraggablePanelProps> = ({ onClose }) => {
  const companiesData = useValue(IndustrialCompanyDebugData);



  // Configure the size provider for the virtual list - 60px row height, 10 visible items, 5 buffer items
// Update the sizeProvider to be aware of the total items
  const sizeProvider = useUniformSizeProvider(30, companiesData.length, 5);

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
            />
          </div>
          <DataDivider />
        </div>
      )}
    </Panel>
  );
};

export default IndustrialCompany;