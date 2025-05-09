import React, { FC, ReactElement } from 'react';
import { useValue, trigger } from 'cs2/api';
import {Tooltip, Panel, DraggablePanelProps, Button, FloatingButton} from 'cs2/ui';
import { formatWords, formatNumber, formatpercentage } from 'mods/InfoLoomSections/utils/formatText';
import { EfficiencyFactorInfo, CommercialCompanyDebug, CommercialDatas } from '../../../domain/CommercialCompanyDebugData';
import styles from './CommercialCompanyDebugData.module.scss';
import { CommercialCompanyDebugData } from "mods/bindings";
import {getModule} from "cs2/modding";
import {Entity, useCssLength} from 'cs2/utils';
import mod from "mod.json";


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
    return (
        <div className={styles.tooltipContent}>
            <div className={styles.tooltipText}>
                <p><strong>Total Efficiency: {company.TotalEfficiency}</strong></p>
                <p>Factors affecting efficiency:</p>
                {company.Factors && company.Factors.map((factor: EfficiencyFactorInfo, index: number) => (
                    <div key={index} className={styles.factorRow}>
                        <span className={styles.factorName}>{factor.factor}</span>
                        <span className={
                            factor.value > 0
                                ? styles.positive
                                : factor.value < 0
                                    ? styles.negative
                                    : styles.neutral
                        }>
                          {factor.value > 0 ? '+' : ''}{factor.value}
                        </span>
                        <span className={styles.factorResult}>
                          {factor.result}
                        </span>
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
                <p><strong>Service Usage: {formatpercentage(serviceUsagePercentage)}</strong></p>
                <p>Available: {formatNumber(serviceAvailable)}</p>
                <p>Maximum: {formatNumber(maxService)}</p>
            </div>
        </div>
    );
};

interface CompanyRowProps {
    company: CommercialCompanyDebug;
}


const CompanyRow: FC<CompanyRowProps> = ({ company }) => {
    const totalEfficiency = company.TotalEfficiency;
    const handleNavigate = () => {
        trigger(mod.id, "GoTo", company.EntityId); // Call C# method
    };
    // Calculate service usage (inverted from availability)
    const serviceUsagePercentage = company.MaxService > 0 ?
        1 - (company.ServiceAvailable / company.MaxService) : 0;
    const serviceUsage = formatpercentage(serviceUsagePercentage);
    
    const employeeRatio = company.MaxWorkers > 0 ?
        formatpercentage(company.TotalEmployees / company.MaxWorkers) : "N/A";

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
                    resources={company.Resources}
                    amount={company.ResourceAmount}
                />
            }>
                <div className={styles.resourcesColumn}>
                    {company.Resources}
                </div>
            </Tooltip>

            <Tooltip tooltip={
                <EfficiencyTooltip company={company} />
            }>
                <div className={styles.efficiencyColumn}>
                    {totalEfficiency}
                </div>
            </Tooltip>
            <div className={styles.locationColumn}>
                <Button
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
                <Tooltip tooltip="Location of the commercial company">
                    <div className={styles.locationColumn}><b>Location</b></div>
                </Tooltip>
            </div>
        </div>
    );
};

const CommercialCompanyDebugDataPanel: FC<DraggablePanelProps> = ({ onClose }) => {
    const companiesData = useValue(CommercialCompanyDebugData);


    
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
                        />
                    </div>
                    <DataDivider />
                </div>
            )}
        </Panel>
    );
};

export default CommercialCompanyDebugDataPanel;