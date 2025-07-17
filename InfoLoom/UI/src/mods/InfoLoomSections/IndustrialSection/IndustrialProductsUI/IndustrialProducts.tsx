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
  const resourceNameMap: Record<string, string> = {
      "conv.food": "Convenience Food",
    };
    const formattedResourceName =
    resourceNameMap[data.ResourceName] || formatWords(data.ResourceName, true);
  return (
    <div className={styles.row_S2v}>
      <div className={styles.col1}>
        <span>{formattedResourceName}</span>
      </div>
      <div className={styles.col2}>
        {data.Demand}
      </div>
      <div className={styles.col3}>
        {data.Building}
      </div>
      <div className={styles.col4}>
        {data.Free}
      </div>
      <div className={styles.col5}>
        {data.Companies}
      </div>
      <div className={styles.col6}>
        {data.SvcPercent}
      </div>
      <div className={styles.col7}>
        {data.CapPerCompany}
      </div>
      <div className={styles.col8}>
        {data.CapPercent}
      </div>
      <div className={styles.col9}>
        {data.Workers}
      </div>
      <div className={styles.col10}>
        {`${data.WrkPercent}%`}
      </div>
      <div className={styles.col11}>
        {data.TaxFactor}
      </div>
    </div>
  );
};

// Header component for the resource table
const TableHeader: React.FC = () => {
  return (
    <div className={styles.headerRow}>
      <div className={styles.col1}>
        <Tooltip tooltip={'The type/name of the resource'}>
          <span>Resource</span>
        </Tooltip>
      </div>
      <div className={styles.col2}>
        <Tooltip tooltip={'key number that decides what companies will spawn;  the higher the number, the higher probability of a company being spawned.'}>
          <span>Resource Demand</span>
        </Tooltip>
      </div>
      <div className={styles.col3}>
          <Tooltip tooltip={' key number that decides what buildings will spawn; 0 means there is no demand'}>
        <span>Building Demand</span>
        </Tooltip>
      </div>
      <div className={styles.col4}>
        <Tooltip tooltip={'Free indicates the number of free properties available for the resource. This is the number of properties that are not currently occupied or used by any industrial buildings.'}>
          <span>Free</span>
        </Tooltip>
      </div>
      <div className={styles.col5}>
        <Tooltip tooltip={'The number of industrial companies that are currently operating in the city for this resource.'}>
          <span>Companies</span>
        </Tooltip>
      </div>
      <div className={styles.col6}>
        <Tooltip tooltip={'The total number of storage building in the city for this resource.'}>
          <span>Storage</span>
        </Tooltip>
      </div>
      <div className={styles.col7}>
        <Tooltip tooltip={'Shows the amount of the resource produced by a single company. This helps determine how much each company contributes to meeting the overall resource demand.'}>
        <span>Production</span>
        </Tooltip>
      </div>
      <div className={styles.col8 }>
        <Tooltip tooltip={'Represents the total demand for the resource from all companies, indicating how much of the resource is needed across the city’s industrial sector. It is used to assess whether current production meets company requirements.'}>
        <span>Demand</span>
        </Tooltip>
      </div>
      <div className={styles.col9}>
        <Tooltip tooltip={'Workers indicates the number of workers employed by the industrial companies for the resource'}>
        <span>Workers</span>
        </Tooltip>
      </div>
      <div className={styles.col10}>
        <Tooltip tooltip={'worker % indicates the percentage of workers employed compared to the total number of workers available in the city. 100% means all workers are employed'}>
          <span>Worker %</span>
        </Tooltip>
      </div>
      <div className={styles.col11}>
        <Tooltip tooltip={'TaxFactor: Shows the effect of the current industrial/office tax rate on demand for this resource, scaled as a percentage. A higher value means taxes are reducing demand more.'}>
            <span>Tax Factor</span>
        </Tooltip>
      </div>
    </div>
  );
};

// Interface for $Industrial props
interface IndustrialProps extends PanelProps {}
const $IndustrialProducts: FC<IndustrialProps> = ({ onClose }) => {
  const { translate } = useLocalization();
  const industrialProducts = useValue(IndustrialProductsData);
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
          <TableHeader />
          {industrialProducts.map((item: industrialProductData) => (
            <ResourceLine key={item.ResourceName} data={item} />
          ))}
        </div>
      )}
    </Panel>
  );
};

export default $IndustrialProducts;
