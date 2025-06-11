import React, { useState, useCallback, FC } from 'react';
import { Dropdown, DropdownToggle, PanelProps, Scrollable, Panel, Icon } from 'cs2/ui';
import { InfoCheckbox } from 'mods/components/InfoCheckbox/InfoCheckbox';
import { getModule } from "cs2/modding";
import styles from './IndustrialProducts.module.scss';
import { formatWords } from 'mods/InfoLoomSections/utils/formatText';
import {useValue} from "cs2/api";
import { industrialProductData } from 'mods/domain/industrialProductData';
import {IndustrialProductsData} from "../../../bindings";


const DropdownStyle = getModule("game-ui/menu/themes/dropdown.module.scss", "classes");



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
      {right2 !== undefined && (
        flag2 ? (
          <div className="row_S2v negative_YWY" style={centerStyle}>
            {right2text}
          </div>
        ) : (
          <div className="row_S2v positive_zrK" style={centerStyle}>
            {right2text}
          </div>
        )
      )}
    </div>
  );
};

// Component: DataDivider
const DataDivider: React.FC = () => {
  return (
    <div style={{ display: 'flex', height: '4rem', flexDirection: 'column', justifyContent: 'center' }}>
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
  const displayName = data.ResourceName === 'Ore' ? 'MetalOre' : 
                     data.ResourceName === 'Oil' ? 'CrudeOil' : 
                     data.ResourceName;
  const formattedResourceName = formatWords(displayName, true);
  
  return (
    <div className={styles.row_S2v}>
      <div className={styles.cell} style={{ width: '3%' }}></div>
      <div className={styles.cell} style={{ width: '15%', justifyContent: 'flex-start'}}>
        <Icon src={data.ResourceIcon}/>
        <span>{formattedResourceName}</span>
      </div>
          <div className={`${styles.cell} ${data.Demand < 0 ? styles.negative_YWY : ''}`} style={{ width: '6%' }}>
            {data.Demand}
          </div>
          <div className={`${styles.cell} ${data.Building <= 0 ? styles.negative_YWY : ''}`} style={{ width: '4%' }}>
            {data.Building}
          </div>
          <div className={`${styles.cell} ${data.Free <= 0 ? styles.negative_YWY : ''}`} style={{ width: '10%' }}>
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
          <div className={`${styles.cell} ${data.WrkPercent < 90 ? styles.negative_YWY : styles.positive_zrK}`} style={{ width: '9%' }}>
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
          <div className={styles.headerCell} style={{ width: '6%', display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
            <span>Resource</span>
            <span>Demand</span>
          </div>
          <div className={styles.headerCell} style={{ width: '4%', display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
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
// Component: $Commercial
const $IndustrialProducts: FC<IndustrialProps> = ({ onClose }) => {
  const industrialProducts = useValue(IndustrialProductsData);
  return (
    <Panel
      draggable
      onClose={onClose}
      initialPosition={{ x: 0.50, y: 0.50 }}
      className={styles.panel}
      header={<div className={styles.header}><span className={styles.headerText}>Industrial & Office Products</span></div>}
    >
      {industrialProducts.length === 0 ? (
        <p>Waiting...</p>
      ) : (
        <div className={styles.panelContent}>
          <div className={styles.controls}>

          </div>
          <TableHeader />
              {industrialProducts.map((item: industrialProductData) => (
                <ResourceLine
                  key={item.ResourceName}
                  data={item}
                />
              ))}
        </div>
      )}
    </Panel>
  );
};

export default $IndustrialProducts;