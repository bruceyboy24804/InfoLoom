import React, { useCallback, useState } from 'react';
import { bindValue, useValue } from "cs2/api";
import { DraggablePanelProps, Panel, PanelProps } from "cs2/ui";
import styles from "./Residential.module.scss";
import { ResidentialData } from "mods/bindings";


interface RowWithTwoColumnsProps {
  left: React.ReactNode;
  right: React.ReactNode;
}

const RowWithTwoColumns = ({ left, right }: RowWithTwoColumnsProps): JSX.Element => {
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

interface RowWithThreeColumnsProps {
  left: React.ReactNode;
  leftSmall?: React.ReactNode;
  right1: React.ReactNode;
  flag1: boolean;
  right2?: React.ReactNode;
  flag2?: boolean;
}

const RowWithThreeColumns = ({
  left,
  leftSmall,
  right1,
  flag1,
  right2,
  flag2,
}: RowWithThreeColumnsProps): JSX.Element => {
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

// Simple horizontal line
const DataDivider = (): JSX.Element => {
  return (
    <div
      style={{ display: 'flex', height: '4rem', flexDirection: 'column', justifyContent: 'center' }}
    >
      <div style={{ borderBottom: '1px solid gray' }}></div>
    </div>
  );
};

interface SingleValueProps {
  value: React.ReactNode;
  flag?: boolean;
  width?: string;
  small?: boolean;
}

// Centered value, if flag exists then uses colors for negative/positive
// Width is 20% by default
const SingleValue = ({ value, flag, width, small }: SingleValueProps): JSX.Element => {
  const rowClass = small ? 'row_S2v small_ExK' : 'row_S2v';
  const centerStyle: React.CSSProperties = {
    width: width === undefined ? '16%' : width,
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

interface BuildingDemandSectionProps {
  data: number[];
}

const BuildingDemandSection = ({ data }: BuildingDemandSectionProps): JSX.Element => {
    const freeL = data[0]-data[3];
	const freeM = data[1]-data[4];
	const freeH = data[2]-data[5];
	const ratio = data[6]/10;
	const ratioString = `$No demand at {ratio}%`;
	const needL = Math.max(1, Math.floor(ratio * data[0] / 100));
	const needM = Math.max(1, Math.floor(ratio * data[1] / 100));
	const needH = Math.max(1, Math.floor(ratio * data[2] / 100));
	const demandL = Math.floor((1 - freeL / needL) * 100);
	const demandM = Math.floor((1 - freeM / needM) * 100);
	const demandH = Math.floor((1 - freeH / needH) * 100);
	// calculate totals and free ratio
	const totalRes = data[0] + data[1] + data[2];
	const totalOcc = data[3] + data[4] + data[5];
	const totalFree = totalRes - totalOcc;
	const freeRatio = (totalRes > 0 ? Math.round(1000*totalFree/totalRes)/10 : 0);
	return (

        <div style={{boxSizing: 'border-box', border: '1px solid gray'}}>
            <div className="labels_L7Q row_S2v">
                <div className="row_S2v" style={{width: '36%'}}></div>
                <SingleValue value="LOW"/>
                <SingleValue value="MEDIUM"/>
                <SingleValue value="HIGH"/>
                <SingleValue value="TOTAL"/>
            </div>
            <div className="labels_L7Q row_S2v">
                <div className="row_S2v" style={{width: '2%'}}></div>
                <div className="row_S2v" style={{width: '34%'}}>
                    Total properties
                </div>
                <SingleValue value={data[0]}/>
                <SingleValue value={data[1]}/>
                <SingleValue value={data[2]}/>
                <SingleValue value={totalRes}/>
            </div>
            <div className="labels_L7Q row_S2v">
                <div className="row_S2v small_ExK" style={{width: '2%'}}></div>
                <div className="row_S2v small_ExK" style={{width: '34%'}}>
                    - Occupied properties
                </div>
                <SingleValue value={data[3]} small={true}/>
                <SingleValue value={data[4]} small={true}/>
                <SingleValue value={data[5]} small={true}/>
                <SingleValue value={totalOcc} small={true}/>
            </div>
            <div className="labels_L7Q row_S2v">
                <div className="row_S2v" style={{width: '2%'}}></div>
                <div className="row_S2v" style={{width: '34%'}}>
                    = Empty properties
                </div>
                <SingleValue value={freeL} flag={freeL > needL}/>
                <SingleValue value={freeM} flag={freeM > needM}/>
                <SingleValue value={freeH} flag={freeH > needH}/>
                <SingleValue value={totalFree}/>
            </div>
            <div className="labels_L7Q row_S2v">
                <div className="row_S2v small_ExK" style={{width: '2%'}}></div>
                <div className="row_S2v small_ExK" style={{width: '34%'}}>{"No demand at " + ratio + "%"}</div>
                <SingleValue value={needL} small={true}/>
                <SingleValue value={needM} small={true}/>
                <SingleValue value={needH} small={true}/>
                <div className="row_S2v" style={{width: '16%'}}></div>
            </div>
            <div className="labels_L7Q row_S2v">
                <div className="row_S2v" style={{width: '2%'}}></div>
                <div className="row_S2v" style={{width: '34%'}}>BUILDING DEMAND</div>
                <SingleValue value={demandL} flag={demandL < 0}/>
                <SingleValue value={demandM} flag={demandM < 0}/>
                <SingleValue value={demandH} flag={demandH < 0}/>
                <SingleValue value={`${freeRatio}%`}/>
            </div>
            <div className="space_uKL" style={{height: '3rem'}}></div>
        </div>
    );
};

const Residential = ({onClose, initialPosition}: DraggablePanelProps): JSX.Element => {
    const ilResidential = useValue(ResidentialData);


    const homelessThreshold =
        ilResidential.length > 13
            ? Math.round((ilResidential[12] * ilResidential[13]) / 1000)
            : 0;

    return (
        <Panel
            draggable={true}
            onClose={onClose}
          initialPosition={{x: 0.15, y: 0.020 }}
      className={styles.panel}
      header={
        <div className={styles.header}>
          <span className={styles.headerText}>Residential</span>
        </div>
      }
    >
      {ilResidential.length === 0 ? (
        <p style={{ color: 'white' }}>Waiting...</p> 
      ) : (
        <div>
          <BuildingDemandSection data={ilResidential} />
          {/* OTHER DATA, two columns */}
          <div style={{ display: 'flex' }}>
            <div
              style={{
                width: '50%',
                boxSizing: 'border-box',
                border: '1px solid gray',
                paddingLeft: '10rem',
                paddingRight: '10rem',
              }}
            >
              <div className="space_uKL" style={{ height: '3rem' }}></div>
              <RowWithTwoColumns left="STUDY POSITIONS" right={ilResidential[14]} />
              <DataDivider />
              <RowWithThreeColumns
                left="HAPPINESS"
                leftSmall={`${ilResidential[8]} is neutral`}
                right1={ilResidential[7]}
                flag1={ilResidential[7] < ilResidential[8]}
              />
              <DataDivider />
              <RowWithThreeColumns 
                  left="UNEMPLOYMENT" 
                  leftSmall={`${ilResidential[10]/10}% is neutral`} 
                  right1={ilResidential[9]} 
                  flag1={ilResidential[9]>ilResidential[10]/10} 
              />
              <DataDivider />
              <RowWithThreeColumns
                left="HOUSEHOLD DEMAND"
                right1={ilResidential[16]}
                flag1={ilResidential[16] < 0}
              />
              <div className="space_uKL" style={{ height: '3rem' }}></div>
            </div>
            <div
              style={{
                width: '50%',
                boxSizing: 'border-box',
                border: '1px solid gray',
                paddingLeft: '10rem',
                paddingRight: '10rem',
              }}
            >
              <div className="space_uKL" style={{ height: '3rem' }}></div>
              <RowWithTwoColumns left="HOUSEHOLDS" right={ilResidential[12]} />
              <DataDivider />
              <RowWithThreeColumns
                left="HOMELESS"
                leftSmall={`${homelessThreshold} is neutral`}
                right1={ilResidential[11]}
                flag1={ilResidential[11] > homelessThreshold}
              />
              <DataDivider />
              <RowWithThreeColumns
                left="TAX RATE (weighted)"
                leftSmall="10% is neutral"
                right1={ilResidential[15] / 10}
                flag1={ilResidential[15] > 100}
              />
              <DataDivider />
              <RowWithTwoColumns left="STUDENT CHANCE" right={`${ilResidential[17]} %`} />
              <div className="space_uKL" style={{ height: '3rem' }}></div>
            </div>
          </div>
        </div>
      )}
    </Panel>
  );
};

export default Residential;