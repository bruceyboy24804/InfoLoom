import React, { useState, useCallback, FC } from 'react';
import { Dropdown, DropdownToggle, PanelProps, Scrollable, Panel, Icon, Tooltip } from 'cs2/ui';
import { InfoCheckbox } from 'mods/components/InfoCheckbox/InfoCheckbox';
import { getModule } from 'cs2/modding';
import styles from './IndustrialProducts.module.scss';
import { formatWords } from 'mods/InfoLoomSections/utils/formatText';
import { useValue } from 'cs2/api';
import { industrialProductData } from 'mods/domain/industrialProductData';
import { IndustrialProductsData } from '../../../bindings';
import { useLocalization } from 'cs2/l10n';

const DropdownStyle = getModule('game-ui/menu/themes/dropdown.module.scss', 'classes');

// Interface for RowWithTwoColumns props
interface RowWithTwoColumnsProps {
  left: React.ReactNode;
  right: React.ReactNode;
}

// Component: RowWithTwoColumns
const RowWithTwoColumns: React.FC<RowWithTwoColumnsProps> = ({ left, right }) => {
  return (
    <div className="labels_L7Q row_S2v">
      <div className="row_S2v" style={{ width: '70%' }}>
        {left}
      </div>
      <div className="row_S2v" style={{ width: '30%', justifyContent: 'center' }}>
        {right}
      </div>
    </div>
  );
};

// Interface for RowWithThreeColumns props
interface RowWithThreeColumnsProps {
  left: React.ReactNode;
  leftSmall?: React.ReactNode;
  right1: React.ReactNode;
  flag1: boolean;
  right2?: React.ReactNode;
  flag2?: boolean;
}

// Component: RowWithThreeColumns
const RowWithThreeColumns: React.FC<RowWithThreeColumnsProps> = ({
  left,
  leftSmall,
  right1,
  flag1,
  right2,
  flag2,
}) => {
  const centerStyle: React.CSSProperties = {
    width: right2 === undefined ? '30%' : '15%',
    justifyContent: 'center',
  };

  const right1text = `${right1}`;
  const right2text = `${right2}`;

  return (
    <div className="labels_L7Q row_S2v">
      <div className="row_S2v" style={{ width: '70%', flexDirection: 'column' }}>
        <p>{left}</p>
        {leftSmall && <p style={{ fontSize: '80%' }}>{leftSmall}</p>}
      </div>
      {flag1 ? (
        <div className="row_S2v negative_YWY" style={centerStyle}>
          {right1text}
        </div>
      ) : (
        <div className="row_S2v positive_zrK" style={centerStyle}>
          {right1text}
        </div>
      )}
      {right2 !== undefined &&
        (flag2 ? (
          <div className="row_S2v negative_YWY" style={centerStyle}>
            {right2text}
          </div>
        ) : (
          <div className="row_S2v positive_zrK" style={centerStyle}>
            {right2text}
          </div>
        ))}
    </div>
  );
};

// Component: DataDivider
const DataDivider: React.FC = () => {
  return (
    <div
      style={{ display: 'flex', height: '4rem', flexDirection: 'column', justifyContent: 'center' }}
    >
      <div style={{ borderBottom: '1rem solid gray' }}></div>
    </div>
  );
};

// Interface for SingleValue props
interface SingleValueProps {
  value: React.ReactNode;
  flag?: boolean;
  width?: string;
  small?: boolean;
}

// Component: SingleValue
const SingleValue: React.FC<SingleValueProps> = ({ value, flag, width, small }) => {
  const rowClass = small ? 'row_S2v small_ExK' : 'row_S2v';
  const centerStyle: React.CSSProperties = {
    width: width === undefined ? '10%' : width, // Changed default width to '10%'
    justifyContent: 'center',
  };

  return flag === undefined ? (
    <div className={rowClass} style={centerStyle}>
      {value}
    </div>
  ) : flag ? (
    <div className={`${rowClass} negative_YWY`} style={centerStyle}>
      {value}
    </div>
  ) : (
    <div className={`${rowClass} positive_zrK`} style={centerStyle}>
      {value}
    </div>
  );
};

// Interface for ResourceLine props

interface ResourceLineProps {
  data: industrialProductData;
}

// Component: ResourceLine
const ResourceLine: React.FC<ResourceLineProps> = ({ data }) => {
  // Use the display name mapping if available
  const displayName =
    data.ResourceName === 'Ore'
      ? 'MetalOre'
      : data.ResourceName === 'Oil'
        ? 'CrudeOil'
        : data.ResourceName;
  const formattedResourceName = formatWords(displayName, true);

  return (
    <div className={styles.row_S2v}>
      <div className={styles.cell} style={{ width: '3%' }}></div>
      <div className={styles.cell} style={{ width: '15%', justifyContent: 'flex-start' }}>
        <Icon src={data.ResourceIcon} />
        <span>{formattedResourceName}</span>
      </div>
      <div
        className={`${styles.cell} ${data.Demand < 0 ? styles.negative_YWY : ''}`}
        style={{ width: '6%' }}
      >
        {data.Demand}
      </div>
      <div
        className={`${styles.cell} ${data.Building <= 0 ? styles.negative_YWY : ''}`}
        style={{ width: '4%' }}
      >
        {data.Building}
      </div>
      <div
        className={`${styles.cell} ${data.Free <= 0 ? styles.negative_YWY : ''}`}
        style={{ width: '10%' }}
      >
        {data.Free}
      </div>
      <div className={styles.cell} style={{ width: '10%' }}>
        {data.Companies}
      </div>

      <div className={styles.cell} style={{ width: '12%' }}>
        {data.SvcPercent}
      </div>

      <div className={styles.cell} style={{ width: '10%' }}>
        {data.CapPerCompany}
      </div>
      <div className={styles.cell} style={{ width: '10%' }}>
        {data.CapPercent}
      </div>

      <div className={styles.cell} style={{ width: '9%' }}>
        {data.Workers}
      </div>
      <div
        className={`${styles.cell} ${data.WrkPercent < 90 ? styles.negative_YWY : styles.positive_zrK}`}
        style={{ width: '9%' }}
      >
        {`${data.WrkPercent}%`}
      </div>
    </div>
  );
};

// Header component for the resource table
const TableHeader: React.FC = () => {
  return (
    <div className={styles.headerRow}>
      <div className={styles.headerCell} style={{ width: '3%' }}></div>
      <div className={styles.headerCell} style={{ width: '15%' }}>
        Resource
      </div>
      <div
        className={styles.headerCell}
        style={{ width: '6%', display: 'flex', flexDirection: 'column', alignItems: 'center' }}
      >
        <span>Resource</span>
        <span>Demand</span>
      </div>
      <div
        className={styles.headerCell}
        style={{ width: '4%', display: 'flex', flexDirection: 'column', alignItems: 'center' }}
      >
        <span>Building</span>
        <span>Demand</span>
      </div>
      <div className={styles.headerCell} style={{ width: '10%' }}>
        Free
      </div>
      <div className={styles.headerCell} style={{ width: '10%' }}>
        Num
      </div>
      <div className={styles.headerCell} style={{ width: '12%' }}>
        Storage
      </div>
      <div className={styles.headerCell} style={{ width: '10%' }}>
        Production
      </div>
      <div className={styles.headerCell} style={{ width: '10%' }}>
        Demand
      </div>
      <div className={styles.headerCell} style={{ width: '9%' }}>
        workers
      </div>
      <div className={styles.headerCell} style={{ width: '9%' }}>
        Worker %
      </div>
    </div>
  );
};

// Interface for $Industrial props
interface IndustrialProps extends PanelProps {}
const $IndustrialProducts: FC<IndustrialProps> = ({ onClose }) => {
  const { translate } = useLocalization();
  const industrialProducts = useValue(IndustrialProductsData);

  const TableHeaderWithTranslation = () => {
    return (
      <div className={styles.headerRow}>
        <div className={styles.headerCell} style={{ width: '3%' }}></div>
        <div className={styles.headerCell} style={{ width: '15%' }}>
          <Tooltip tooltip={translate("InfoLoomTwo.IndustrialProductsPanel[ResourceTooltip]", "Resource type being produced")}>
            <span>{translate("InfoLoomTwo.IndustrialProductsPanel[Resource]", "Resource")}</span>
          </Tooltip>
        </div>
        <div
          className={styles.headerCell}
          style={{ width: '6%', display: 'flex', flexDirection: 'column', alignItems: 'center' }}
        >
          <Tooltip tooltip={translate("InfoLoomTwo.IndustrialProductsPanel[ResourceDemandTooltip]", "Total demand for this resource from all sources (city services, population, companies)")}>
            <div style={{ textAlign: 'center' }}>
              <span>{translate("InfoLoomTwo.IndustrialProductsPanel[ResourceDemand1]", "Resource")}</span>
              <span>{translate("InfoLoomTwo.IndustrialProductsPanel[ResourceDemand2]", "Demand")}</span>
            </div>
          </Tooltip>
        </div>
        <div
          className={styles.headerCell}
          style={{ width: '4%', display: 'flex', flexDirection: 'column', alignItems: 'center' }}
        >
          <Tooltip tooltip={translate("InfoLoomTwo.IndustrialProductsPanel[BuildingDemandTooltip]", "Building demand percentage (0-100%) based on company demand, workforce, taxes, and local market conditions")}>
            <div style={{ textAlign: 'center' }}>
              <span>{translate("InfoLoomTwo.IndustrialProductsPanel[BuildingDemand1]", "Building")}</span>
              <span>{translate("InfoLoomTwo.IndustrialProductsPanel[BuildingDemand2]", "Demand")}</span>
            </div>
          </Tooltip>
        </div>
        <div className={styles.headerCell} style={{ width: '10%' }}>
          <Tooltip tooltip={translate("InfoLoomTwo.IndustrialProductsPanel[FreeTooltip]", "Number of empty industrial buildings available for companies producing this resource")}>
            <span>{translate("InfoLoomTwo.IndustrialProductsPanel[Free]", "Free")}</span>
          </Tooltip>
        </div>
        <div className={styles.headerCell} style={{ width: '10%' }}>
          <Tooltip tooltip={translate("InfoLoomTwo.IndustrialProductsPanel[NumTooltip]", "Number of companies currently producing this resource")}>
            <span>{translate("InfoLoomTwo.IndustrialProductsPanel[Num]", "Num")}</span>
          </Tooltip>
        </div>
        <div className={styles.headerCell} style={{ width: '12%' }}>
          <Tooltip tooltip={translate("InfoLoomTwo.IndustrialProductsPanel[StorageTooltip]", "Number of storage facilities available for this resource")}>
            <span>{translate("InfoLoomTwo.IndustrialProductsPanel[Storage]", "Storage")}</span>
          </Tooltip>
        </div>
        <div className={styles.headerCell} style={{ width: '10%' }}>
          <Tooltip tooltip={translate("InfoLoomTwo.IndustrialProductsPanel[ProductionTooltip]", "Current production output per company for this resource")}>
            <span>{translate("InfoLoomTwo.IndustrialProductsPanel[Production]", "Production")}</span>
          </Tooltip>
        </div>
        <div className={styles.headerCell} style={{ width: '10%' }}>
          <Tooltip tooltip={translate("InfoLoomTwo.IndustrialProductsPanel[DemandTooltip]", "Company demand for this resource from other companies as input materials")}>
            <span>{translate("InfoLoomTwo.IndustrialProductsPanel[Demand]", "Demand")}</span>
          </Tooltip>
        </div>
        <div className={styles.headerCell} style={{ width: '9%' }}>
          <Tooltip tooltip={translate("InfoLoomTwo.IndustrialProductsPanel[WorkersTooltip]", "Current number of workers employed in companies producing this resource")}>
            <span>{translate("InfoLoomTwo.IndustrialProductsPanel[Workers]", "Workers")}</span>
          </Tooltip>
        </div>
        <div className={styles.headerCell} style={{ width: '9%' }}>
          <Tooltip tooltip={translate("InfoLoomTwo.IndustrialProductsPanel[WorkerPercentTooltip]", "Percentage of maximum worker capacity currently employed. Low values indicate workforce shortages.")}>
            <span>{translate("InfoLoomTwo.IndustrialProductsPanel[WorkerPercent]", "Worker %")}</span>
          </Tooltip>
        </div>
      </div>
    );
  };

  const ResourceLineWithTranslation = ({ data }: { data: industrialProductData }) => {
    // Use the display name mapping if available
    const displayName =
      data.ResourceName === 'Ore'
        ? 'MetalOre'
        : data.ResourceName === 'Oil'
          ? 'CrudeOil'
          : data.ResourceName;
    const formattedResourceName = formatWords(displayName, true);

    return (
      <div className={styles.row_S2v}>
        <div className={styles.cell} style={{ width: '3%' }}></div>
        <div className={styles.cell} style={{ width: '15%', justifyContent: 'flex-start' }}>
          <Icon src={data.ResourceIcon} />
          <span>{formattedResourceName}</span>
        </div>
        <Tooltip tooltip={translate("InfoLoomTwo.IndustrialProductsPanel[ResourceDemandValueTooltip]", "Total resource demand from all consumers including city services, population, and industrial processes")}>
          <div
            className={`${styles.cell} ${data.Demand < 0 ? styles.negative_YWY : ''}`}
            style={{ width: '6%' }}
          >
            {data.Demand}
          </div>
        </Tooltip>
        <Tooltip tooltip={translate("InfoLoomTwo.IndustrialProductsPanel[BuildingDemandValueTooltip]", "Building demand calculated from workforce availability, tax rates, local demand, and input costs. Higher values indicate stronger demand for new buildings.")}>
          <div
            className={`${styles.cell} ${data.Building <= 0 ? styles.negative_YWY : ''}`}
            style={{ width: '4%' }}
          >
            {data.Building}
          </div>
        </Tooltip>
        <Tooltip tooltip={translate("InfoLoomTwo.IndustrialProductsPanel[FreeValueTooltip]", "Empty industrial buildings ready for companies. Red indicates shortage of available buildings.")}>
          <div
            className={`${styles.cell} ${data.Free <= 0 ? styles.negative_YWY : ''}`}
            style={{ width: '10%' }}
          >
            {data.Free}
          </div>
        </Tooltip>
        <Tooltip tooltip={translate("InfoLoomTwo.IndustrialProductsPanel[CompaniesValueTooltip]", "Active companies producing this resource")}>
          <div className={styles.cell} style={{ width: '10%' }}>
            {data.Companies}
          </div>
        </Tooltip>
        <Tooltip tooltip={translate("InfoLoomTwo.IndustrialProductsPanel[StorageValueTooltip]", "Number of storage facilities for this resource")}>
          <div className={styles.cell} style={{ width: '12%' }}>
            {data.SvcPercent}
          </div>
        </Tooltip>
        <Tooltip tooltip={translate("InfoLoomTwo.IndustrialProductsPanel[ProductionValueTooltip]", "Current production capacity per company")}>
          <div className={styles.cell} style={{ width: '10%' }}>
            {data.CapPerCompany}
          </div>
        </Tooltip>
        <Tooltip tooltip={translate("InfoLoomTwo.IndustrialProductsPanel[DemandValueTooltip]", "Input demand from companies using this resource in their production processes")}>
          <div className={styles.cell} style={{ width: '10%' }}>
            {data.CapPercent}
          </div>
        </Tooltip>
        <Tooltip tooltip={translate("InfoLoomTwo.IndustrialProductsPanel[WorkersValueTooltip]", "Total workers currently employed by companies producing this resource")}>
          <div className={styles.cell} style={{ width: '9%' }}>
            {data.Workers}
          </div>
        </Tooltip>
        <Tooltip tooltip={translate("InfoLoomTwo.IndustrialProductsPanel[WorkerPercentValueTooltip]", "Worker capacity utilization. Values below 90% indicate workforce shortages affecting production efficiency.")}>
          <div
            className={`${styles.cell} ${data.WrkPercent < 90 ? styles.negative_YWY : styles.positive_zrK}`}
            style={{ width: '9%' }}
          >
            {`${data.WrkPercent}%`}
          </div>
        </Tooltip>
      </div>
    );
  };

  return (
    <Panel
      draggable
      onClose={onClose}
      initialPosition={{ x: 0.5, y: 0.5 }}
      className={styles.panel}
      header={
        <div className={styles.header}>
          <span className={styles.headerText}>{translate("InfoLoomTwo.IndustrialProductsPanel[Title]", "Industrial & Office Products")}</span>
        </div>
      }
    >
      {industrialProducts.length === 0 ? (
        <p>{translate("InfoLoomTwo.IndustrialProductsPanel[Waiting]", "Waiting...")}</p>
      ) : (
        <div className={styles.panelContent}>
          <div className={styles.controls}></div>
          <TableHeaderWithTranslation />
          {industrialProducts.map((item: industrialProductData) => (
            <ResourceLineWithTranslation key={item.ResourceName} data={item} />
          ))}
        </div>
      )}
    </Panel>
  );
};

export default $IndustrialProducts;
