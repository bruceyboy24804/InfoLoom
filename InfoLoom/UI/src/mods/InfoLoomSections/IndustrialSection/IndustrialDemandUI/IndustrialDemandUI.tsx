import { DraggablePanelProps, Panel, Tooltip, PanelFoldout } from 'cs2/ui';
import { IndustrialData, IndustrialDataExRes, resourceBox, storageBox, InfoRowTheme, Divider } from '../../../bindings';
import styles from './IndustrialDemand.module.scss';
import { useLocalization } from 'cs2/l10n';
import { Localekeys } from 'mods/locale';
import React from 'react';
import { useValue } from 'cs2/api';
import classNames from 'classnames';
import { InfoRowSCSS } from '../../ILInfoSections/Modules/info-Row/info-Row.module.scss';

// Header block for a PanelFoldout, matching the sibling Commercial panel.
const foldoutHeader = (label: React.ReactNode) => (
  <div className={InfoRowTheme.infoRow}>
    <div className={classNames(InfoRowSCSS.left)}>{label}</div>
  </div>
);

// One data row: label + value cells. Optionally wrapped in a tooltip.
const DataRow: React.FC<{
  label: React.ReactNode;
  tooltip?: string | null;
  sub?: boolean;
  children: React.ReactNode;
}> = ({ label, tooltip, sub, children }) => {
  const row = (
    <div className={styles.row}>
      <div className={classNames(styles.rowLabel, sub && styles.subLabel)}>{label}</div>
      {children}
    </div>
  );
  return tooltip ? <Tooltip tooltip={tooltip}>{row}</Tooltip> : row;
};

// A percentage value colored by whether it's in the healthy or unhealthy range.
const Pct: React.FC<{ value: string; negative: boolean }> = ({ value, negative }) => (
  <span className={negative ? styles.negative : styles.positive}>{value}</span>
);

const IndustrialDemandUI = ({ onClose }: DraggablePanelProps): JSX.Element => {
  const { translate } = useLocalization();
  const data = useValue(IndustrialData.binding);
  const dataExRes = useValue(IndustrialDataExRes.binding);

  const t = (key: string, fallback: string) => translate(key, fallback) ?? fallback;

  const emptyBuildingsLabel = t(Localekeys.EmptyBuildings, 'Empty Buildings');
  const emptyBuildingsTooltip = t(
    Localekeys.EmptyBuildingsTooltip,
    'Number of empty industrial/office buildings available for new companies to move in.'
  );
  const propertylessLabel = t(Localekeys.PropertylessCompanies, 'Propertyless Companies');
  const propertylessTooltip = t(
    Localekeys.PropertylessCompaniesTooltip,
    "Companies that exist but don't have a building to operate from. High numbers indicate a need for more zoned land."
  );
  const taxRateLabel = t(Localekeys.TaxRate, 'Average Tax Rate');
  const taxRateTooltip = t(
    Localekeys.TaxRateTooltip,
    'Average tax rate across all resource types. Higher taxes reduce company demand. 10% is neutral.'
  );
  const localDemandLabel = t(Localekeys.LocalDemand, 'Local Demand (ind)');
  const localDemandTooltip = t(
    Localekeys.LocalDemandTooltip,
    'Production capacity compared to local demand. 100% means production equals demand. Higher values indicate overproduction.'
  );
  const inputUtilizationLabel = t(Localekeys.InputUtilization, 'Input Utilization (ind)');
  const inputUtilizationTooltip = t(
    Localekeys.InputUtilizationTooltip,
    'How well input resources are being utilized by manufacturing. 110% is neutral, values capped at 400%.'
  );
  const employeeCapacityLabel = t(Localekeys.EmployeeCapacity, 'Employee Capacity Ratio');
  const employeeCapacityTooltip = t(
    Localekeys.EmployeeCapacityTooltip,
    'Ratio of current employees to maximum employee capacity. 72% industrial and 75% office are neutral ratios.'
  );
  const availableWorkforceLabel = t(Localekeys.Workforce, 'Available Workforce');
  const availableWorkforceTooltip = t(
    Localekeys.WorkforceTooltip,
    'Available educated and uneducated workers that can be employed by new companies.'
  );
  const educatedLabel = t(Localekeys.Educated, 'Educated');
  const educatedTooltip = t(Localekeys.EducatedWorkforceTooltip, 'Number of educated citizens available for work.');
  const uneducatedLabel = t(Localekeys.Uneducated, 'Uneducated');
  const uneducatedTooltip = t(Localekeys.UneducatedWorkforceTooltip, 'Number of uneducated citizens available for work.');
  const storageLabel = t(Localekeys.Storage, 'Storage');
  const storageTooltip = t(
    Localekeys.StorageTooltip,
    'Storage facilities for industrial goods. The game spawns warehouses when demand for storage exists.'
  );
  const storageDescription = t(
    Localekeys.StorageDescription,
    'The game spawns warehouses when demanded types exist.'
  );
  const demandedTypesLabel = t(Localekeys.DemandedTypes, 'Demanded Types');
  const demandedTypesTooltip = t(
    Localekeys.DemandedTypesTooltip,
    'Number of resource types that need storage capacity based on demand vs storage capacity.'
  );
  const demandForLabel = t(Localekeys.DemandFor, 'Demand For');
  const demandForTooltip = t(
    Localekeys.IncludedResourcesTooltip,
    'Resources that currently have no demand in your city. This may be due to oversupply, lack of customers, or economic factors.'
  );

  const header = (
    <div className={styles.header}>
      <span className={styles.headerText}>
        {t(Localekeys.IndustrialPanelTitle, 'Industrial and Office Demand')}
      </span>
    </div>
  );

  if (!data || data.length === 0) {
    return (
      <Panel draggable onClose={onClose} initialPosition={{ x: 0.2, y: 0.5 }} className={styles.panel} header={header}>
        <p className={styles.note}>{t(Localekeys.Waiting, 'Waiting…')}</p>
      </Panel>
    );
  }

  return (
    <Panel draggable onClose={onClose} initialPosition={{ x: 0.2, y: 0.5 }} className={styles.panel} header={header}>
      {/* Primary metrics — always visible */}
      <div className={styles.demandTable}>
        <div className={styles.row}>
          <div className={styles.rowLabel} />
          <div className={styles.colHead}>{t(Localekeys.Industrial, 'Industrial')}</div>
          <div className={styles.colHead}>{t(Localekeys.Office, 'Office')}</div>
        </div>

        <DataRow label={emptyBuildingsLabel} tooltip={emptyBuildingsTooltip}>
          <div className={styles.cell}>{data[0]}</div>
          <div className={styles.cell}>{data[10]}</div>
        </DataRow>
        <DataRow label={propertylessLabel} tooltip={propertylessTooltip}>
          <div className={styles.cell}>{data[1]}</div>
          <div className={styles.cell}>{data[11]}</div>
        </DataRow>

        <Divider noMargin={1} />

        <DataRow label={taxRateLabel} tooltip={taxRateTooltip}>
          <div className={styles.cell}>
            <Pct value={`${(data[2] / 10).toFixed(1)} %`} negative={data[2] > 100} />
          </div>
          <div className={styles.cell}>
            <Pct value={`${(data[12] / 10).toFixed(1)} %`} negative={data[12] > 100} />
          </div>
        </DataRow>

        <Divider noMargin={1} />

        <DataRow label={employeeCapacityLabel} tooltip={employeeCapacityTooltip}>
          <div className={styles.cell}>
            <Pct value={`${(data[4] / 10).toFixed(1)} %`} negative={data[4] < 720} />
          </div>
          <div className={styles.cell}>
            <Pct value={`${(data[14] / 10).toFixed(1)} %`} negative={data[14] < 750} />
          </div>
        </DataRow>
      </div>
        <PanelFoldout header={foldoutHeader("Demand & Utilization")}  initialExpanded={true}>
            <DataRow label={localDemandLabel} tooltip={localDemandTooltip} sub>
                <div className={styles.cellWide}>
                    <Pct value={`${data[3]} %`} negative={data[3] > 100} />
                </div>
            </DataRow>
            <DataRow label={inputUtilizationLabel} tooltip={inputUtilizationTooltip} sub>
                <div className={styles.cellWide}>
                    <Pct value={`${data[7]} %`} negative={data[7] > 100} />
                </div>
            </DataRow>
        </PanelFoldout>

      {/* Secondary sections — collapsible */}
      <PanelFoldout header={foldoutHeader(availableWorkforceLabel)} tooltip={availableWorkforceTooltip} initialExpanded={true}>
        <DataRow label={educatedLabel} tooltip={educatedTooltip} sub>
          <div className={styles.cellWide}>{data[8]}</div>
        </DataRow>
        <DataRow label={uneducatedLabel} tooltip={uneducatedTooltip} sub>
          <div className={styles.cellWide}>{data[9]}</div>
        </DataRow>
      </PanelFoldout>

      <PanelFoldout header={foldoutHeader(storageLabel)} tooltip={storageTooltip} initialExpanded={false}>
        <DataRow label={emptyBuildingsLabel} sub>
          <div className={styles.cellWide}>{data[5]}</div>
        </DataRow>
        <DataRow label={propertylessLabel} sub>
          <div className={styles.cellWide}>{data[6]}</div>
        </DataRow>
        <DataRow label={demandedTypesLabel} tooltip={demandedTypesTooltip} sub>
          <div className={styles.cellWide}>{data[15]}</div>
        </DataRow>
      </PanelFoldout>

      <PanelFoldout header={foldoutHeader(demandForLabel)} tooltip={demandForTooltip} initialExpanded={true}>
        <div className={styles.resourceGrid}>
          {dataExRes.map(resourceName => (
            <div className={`${storageBox.resource} ${resourceBox.field}`} key={resourceName}>
              <img className={`${resourceBox.icon}`} src={`Media/Game/Resources/${resourceName}.svg`} />
              <div className={resourceBox.label}>{t(`Resources.TITLE[${resourceName}]`, resourceName)}</div>
            </div>
          ))}
        </div>
      </PanelFoldout>
    </Panel>
  );
};

export default IndustrialDemandUI;
