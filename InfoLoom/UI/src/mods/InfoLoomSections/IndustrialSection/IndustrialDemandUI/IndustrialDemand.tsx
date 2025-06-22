import React, { FC, useCallback, useState } from 'react';
import { bindValue, useValue } from 'cs2/api';
import mod from 'mod.json';
import { DraggablePanelProps, PanelProps, Panel, Tooltip } from 'cs2/ui';
import { IndustrialData, IndustrialDataExRes } from '../../../bindings';
import { useLocalization } from 'cs2/l10n';
import styles from './IndustrialDemand.module.scss';

// Define interfaces for props
interface RowWithTwoColumnsProps {
  left: React.ReactNode;
  right: React.ReactNode;
}

interface RowWithThreeColumnsProps {
  left: string;
  leftSmall: string;
  right1: number;
  flag1: boolean;
  right2?: number;
  flag2?: boolean;
}

interface SingleValueProps {
  value: number | string;
  flag?: boolean;
  width?: string;
  small?: boolean;
}
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

const RowWithThreeColumns: React.FC<RowWithThreeColumnsProps> = ({
  left,
  leftSmall,
  right1,
  flag1,
  right2,
  flag2,
}) => {
  const centerStyle: React.CSSProperties = {
    width: right2 === undefined ? '40%' : '20%',
    justifyContent: 'center',
  };
  const right1text = `${right1} %`;
  const right2text = `${right2} %`;

  return (
    <div className="labels_L7Q row_S2v">
      <div className="row_S2v" style={{ width: '60%', flexDirection: 'column' }}>
        <p>{left}</p>
        <p style={{ fontSize: '80%' }}>{leftSmall}</p>
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

// Simple horizontal line
const DataDivider: React.FC = () => {
  return (
    <div
      style={{
        display: 'flex',
        height: '4rem',
        flexDirection: 'column',
        justifyContent: 'center',
      }}
    >
      <div style={{ borderBottom: '1px solid gray' }}></div>
    </div>
  );
};

// Centered value with optional flag for styling
const SingleValue: React.FC<SingleValueProps> = ({ value, flag, width, small }) => {
  const rowClass = small ? 'row_S2v small_ExK' : 'row_S2v';
  const centerStyle: React.CSSProperties = {
    width: width === undefined ? '20%' : width,
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
// Declare the engine object if it's globally available

const $Industrial: React.FC<DraggablePanelProps> = ({ onClose, initialPosition, draggable }) => {
  const { translate } = useLocalization();
  const ilIndustrial = useValue(IndustrialData);
  const ilIndustrialExRes = useValue(IndustrialDataExRes);

  const IndustrialDataWithTranslation = ({ data }: { data: number[] }) => {
    return (
      <div style={{ width: '70%', boxSizing: 'border-box', border: '1px solid gray' }}>
        <div className="labels_L7Q row_S2v">
          <div className="row_S2v" style={{ width: '60%' }}></div>
          <SingleValue value={translate("InfoLoomTwo.IndustrialPanel[Industrial]", "INDUSTRIAL") || "INDUSTRIAL"} />
          <SingleValue value={translate("InfoLoomTwo.IndustrialPanel[Office]", "OFFICE") || "OFFICE"} />
        </div>

        <div className="labels_L7Q row_S2v">
          <div className="row_S2v" style={{ width: '60%' }}>
            <Tooltip tooltip={translate("InfoLoomTwo.IndustrialPanel[EmptyBuildingsTooltip]", "Number of empty industrial/office buildings available for new companies to move in")}>
              <span>{translate("InfoLoomTwo.IndustrialPanel[EmptyBuildings]", "EMPTY BUILDINGS")}</span>
            </Tooltip>
          </div>
          <SingleValue value={data[0]} />
          <SingleValue value={data[10]} />
        </div>
        
        <div className="labels_L7Q row_S2v">
          <div className="row_S2v" style={{ width: '60%' }}>
            <Tooltip tooltip={translate("InfoLoomTwo.IndustrialPanel[PropertylessCompaniesTooltip]", "Companies that exist but don't have a building to operate from. High numbers indicate need for more zoned land.")}>
              <span>{translate("InfoLoomTwo.IndustrialPanel[PropertylessCompanies]", "PROPERTYLESS COMPANIES")}</span>
            </Tooltip>
          </div>
          <div className="row_S2v" style={{ width: '20%', justifyContent: 'center' }}>
            {data[1]}
          </div>
          <div className="row_S2v" style={{ width: '20%', justifyContent: 'center' }}>
            {data[11]}
          </div>
        </div>

        <DataDivider />

        <div className="labels_L7Q row_S2v">
          <div className="row_S2v" style={{ width: '60%', flexDirection: 'column' }}>
            <Tooltip tooltip={translate("InfoLoomTwo.IndustrialPanel[TaxRateTooltip]", "Average tax rate across all resource types. Higher taxes reduce company demand. 10% is neutral.")}>
              <p>{translate("InfoLoomTwo.IndustrialPanel[TaxRate]", "AVERAGE TAX RATE")}</p>
            </Tooltip>
            <p style={{ fontSize: '80%' }}>{translate("InfoLoomTwo.IndustrialPanel[TaxRateNeutral]", "10% is the neutral rate")}</p>
          </div>
          <div className={`row_S2v ${data[2] > 100 ? 'negative_YWY' : 'positive_zrK'}`} style={{ width: '20%', justifyContent: 'center' }}>
            {(data[2] / 10).toFixed(1)} %
          </div>
          <div className={`row_S2v ${data[12] > 100 ? 'negative_YWY' : 'positive_zrK'}`} style={{ width: '20%', justifyContent: 'center' }}>
            {(data[12] / 10).toFixed(1)} %
          </div>
        </div>

        <DataDivider />

        <div className="labels_L7Q row_S2v">
          <div className="row_S2v" style={{ width: '60%', flexDirection: 'column' }}>
            <Tooltip tooltip={translate("InfoLoomTwo.IndustrialPanel[LocalDemandTooltip]", "Production capacity compared to local demand. 100% means production equals demand. Higher values indicate overproduction.")}>
              <p>{translate("InfoLoomTwo.IndustrialPanel[LocalDemand]", "LOCAL DEMAND (ind)")}</p>
            </Tooltip>
            <p style={{ fontSize: '80%' }}>{translate("InfoLoomTwo.IndustrialPanel[LocalDemandNeutral]", "100% when production = demand")}</p>
          </div>
          <div className={`row_S2v ${data[3] > 100 ? 'negative_YWY' : 'positive_zrK'}`} style={{ width: '40%', justifyContent: 'center' }}>
            {data[3]} %
          </div>
        </div>

        <div className="labels_L7Q row_S2v">
          <div className="row_S2v" style={{ width: '60%', flexDirection: 'column' }}>
            <Tooltip tooltip={translate("InfoLoomTwo.IndustrialPanel[InputUtilizationTooltip]", "How well input resources are being utilized by manufacturing. 110% is neutral, values capped at 400%.")}>
              <p>{translate("InfoLoomTwo.IndustrialPanel[InputUtilization]", "INPUT UTILIZATION (ind)")}</p>
            </Tooltip>
            <p style={{ fontSize: '80%' }}>{translate("InfoLoomTwo.IndustrialPanel[InputUtilizationNeutral]", "110% is the neutral ratio, capped at 400%")}</p>
          </div>
          <div className={`row_S2v ${data[7] > 100 ? 'negative_YWY' : 'positive_zrK'}`} style={{ width: '40%', justifyContent: 'center' }}>
            {data[7]} %
          </div>
        </div>

        <DataDivider />

        <div className="labels_L7Q row_S2v">
          <div className="row_S2v" style={{ width: '60%', flexDirection: 'column' }}>
            <Tooltip tooltip={translate("InfoLoomTwo.IndustrialPanel[EmployeeCapacityTooltip]", "Ratio of current employees to maximum employee capacity. 72% industrial and 75% office are neutral ratios.")}>
              <p>{translate("InfoLoomTwo.IndustrialPanel[EmployeeCapacity]", "EMPLOYEE CAPACITY RATIO")}</p>
            </Tooltip>
            <p style={{ fontSize: '80%' }}>{translate("InfoLoomTwo.IndustrialPanel[EmployeeCapacityNeutral]", "72% is the neutral ratio")}</p>
          </div>
          <div className={`row_S2v ${data[4] < 720 ? 'negative_YWY' : 'positive_zrK'}`} style={{ width: '20%', justifyContent: 'center' }}>
            {(data[4] / 10).toFixed(1)} %
          </div>
          <div className={`row_S2v ${data[14] < 750 ? 'negative_YWY' : 'positive_zrK'}`} style={{ width: '20%', justifyContent: 'center' }}>
            {(data[14] / 10).toFixed(1)} %
          </div>
        </div>

        <DataDivider />

        <div style={{ display: 'flex' }}>
          <div style={{ width: '60%', height: '2.2em', display: 'flex', alignItems: 'center', fontSize: '15rem', color: 'white' }}>
            <Tooltip tooltip={translate("InfoLoomTwo.IndustrialPanel[WorkforceTooltip]", "Available educated and uneducated workers that can be employed by new companies")}>
              <span>{translate("InfoLoomTwo.IndustrialPanel[Workforce]", "AVAILABLE WORKFORCE")}</span>
            </Tooltip>
          </div>
          <div style={{ width: '40%' }}>
            <RowWithTwoColumns left={translate("InfoLoomTwo.IndustrialPanel[Educated]", "Educated")} right={data[8]} />
            <RowWithTwoColumns left={translate("InfoLoomTwo.IndustrialPanel[Uneducated]", "Uneducated")} right={data[9]} />
          </div>
        </div>

        <DataDivider />

        <div style={{ display: 'flex' }}>
          <div style={{ width: '50%', height: '2.2em', display: 'flex', flexDirection: 'column' }}>
            <p style={{ fontSize: '15rem', color: 'white' }}>
              <Tooltip tooltip={translate("InfoLoomTwo.IndustrialPanel[StorageTooltip]", "Storage facilities for industrial goods. The game spawns warehouses when demand for storage exists.")}>
                <span>{translate("InfoLoomTwo.IndustrialPanel[Storage]", "STORAGE")}</span>
              </Tooltip>
            </p>
            <p style={{ fontSize: '12rem', color: 'white' }}>
              {translate("InfoLoomTwo.IndustrialPanel[StorageDescription]", "The game will spawn warehouses when DEMANDED TYPES exist.")}
            </p>
          </div>
          <div style={{ width: '50%' }}>
            <RowWithTwoColumns 
              left={
                <Tooltip tooltip={translate("InfoLoomTwo.IndustrialPanel[StorageEmptyBuildingsTooltip]", "Empty warehouse buildings available for storage companies")}>
                  <span>{translate("InfoLoomTwo.IndustrialPanel[StorageEmptyBuildings]", "Empty buildings")}</span>
                </Tooltip>
              } 
              right={data[5]} 
            />
            <RowWithTwoColumns 
              left={
                <Tooltip tooltip={translate("InfoLoomTwo.IndustrialPanel[StoragePropertylessTooltip]", "Storage companies without warehouse buildings")}>
                  <span>{translate("InfoLoomTwo.IndustrialPanel[StoragePropertyless]", "Propertyless companies")}</span>
                </Tooltip>
              } 
              right={data[6]} 
            />
            <RowWithTwoColumns 
              left={
                <Tooltip tooltip={translate("InfoLoomTwo.IndustrialPanel[DemandedTypesTooltip]", "Number of resource types that need storage capacity based on demand vs storage capacity")}>
                  <span>{translate("InfoLoomTwo.IndustrialPanel[DemandedTypes]", "DEMANDED TYPES")}</span>
                </Tooltip>
              } 
              right={data[15]} 
            />
          </div>
        </div>
      </div>
    );
  };

  const ExcludedResourcesWithTranslation = ({ resources }: { resources: string[] }) => {
    return (
      <div style={{ width: '30%', boxSizing: 'border-box', border: '1px solid gray' }}>
        <div className="labels_L7Q row_S2v">
          <div className="row_S2v" style={{ width: '100%' }}>
            <Tooltip tooltip={translate("InfoLoomTwo.IndustrialPanel[NoDemandTooltip]", "Resources that currently have no company demand due to oversupply, lack of workforce, or high taxes")}>
              <p style={{ margin: 0 }}>{translate("InfoLoomTwo.IndustrialPanel[NoDemand]", "NO DEMAND FOR")}</p>
            </Tooltip>
          </div>
        </div>
        <ul style={{ listStyleType: 'none', padding: 0, margin: 0 }}>
          {resources.map((item, index) => (
            <li key={index}>
              <div className="row_S2v small_ExK">{item}</div>
            </li>
          ))}
        </ul>
      </div>
    );
  };

  return (
    <Panel
      draggable
      onClose={onClose}
      initialPosition={initialPosition}
      className={styles.panel}
      header={
        <div className={styles.header}>
          <span className={styles.headerText}>{translate("InfoLoomTwo.IndustrialPanel[Title]", "Industrial and Office Demand")}</span>
        </div>
      }
    >
      {ilIndustrial.length === 0 ? (
        <p>{translate("InfoLoomTwo.IndustrialPanel[Waiting]", "Waiting...")}</p>
      ) : (
        <div style={{ display: 'flex' }}>
          <IndustrialDataWithTranslation data={ilIndustrial} />
          <ExcludedResourcesWithTranslation resources={ilIndustrialExRes} />
        </div>
      )}
    </Panel>
  );
};

export default $Industrial;
