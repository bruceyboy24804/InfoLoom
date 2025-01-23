import React, {useCallback, useState} from 'react';
import $Panel from 'mods/panel';
import {bindValue, useValue} from "cs2/api";
import mod from 'mod.json';
// A basic interface for data used by ResourceLine.
// Adjust fields/types as needed for your actual shape of data.
interface ResourceData {
  Resource: string;
  Demand: number;
  Building: number;
  Free: number;
  Companies: number;
  SvcFactor: number;
  SvcPercent: number;
  CapFactor: number;
  CapPercent: number;
  CapPerCompany: number;
  WrkFactor: number;
  WrkPercent: number;
  Workers: number;
  EduFactor: number;
  TaxFactor: number;
}

interface RowWithTwoColumnsProps {
  left: React.ReactNode;
  right: React.ReactNode;
}
const ResourceData$ = bindValue<ResourceData[]>(mod.id, "commercialDemand)", []);
const RowWithTwoColumns: React.FC<RowWithTwoColumnsProps> = ({ left, right }) => {
  return (
    <div className="labels_L7Q row_S2v">
      <div className="row_S2v" style={{ width: '70%' }}>{left}</div>
      <div className="row_S2v" style={{ width: '30%', justifyContent: 'center' }}>{right}</div>
    </div>
  );
};

interface RowWithThreeColumnsProps {
  left: React.ReactNode;
  leftSmall?: React.ReactNode;
  right1: string | number;
  flag1?: boolean;
  right2?: string | number;
  flag2?: boolean;
}

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
      {right2 && (
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

const DataDivider: React.FC = () => {
  return (
    <div style={{ display: 'flex', height: '4rem', flexDirection: 'column', justifyContent: 'center' }}>
      <div style={{ borderBottom: '1px solid gray' }}></div>
    </div>
  );
};

interface SingleValueProps {
  value: string | number;
  flag?: boolean;
  width?: string;
  small?: boolean;
}

const SingleValue: React.FC<SingleValueProps> = ({ value, flag, width, small }) => {
  const rowClass = small ? 'row_S2v small_ExK' : 'row_S2v';
  const centerStyle: React.CSSProperties = {
    width: width ?? '10%',
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

interface ResourceLineProps {
  data: ResourceData;
}

const ResourceLine: React.FC<ResourceLineProps> = ({ data }) => {
  return (
    <div className="labels_L7Q row_S2v" style={{ lineHeight: 0.7 }}>
      <div className="row_S2v" style={{ width: '3%' }} />
      <div className="row_S2v" style={{ width: '15%' }}>{data.Resource}</div>
      <SingleValue value={data.Demand} width="6%" flag={data.Demand < 0} />
      <SingleValue value={data.Building} width="4%" flag={data.Building <= 0} />
      <SingleValue value={data.Free} width="4%" flag={data.Free <= 0} />
      <SingleValue value={data.Companies} width="5%" />
      <SingleValue value={data.SvcFactor} width="6%" flag={data.SvcFactor < 0} small />
      <SingleValue value={`${data.SvcPercent}%`} width="6%" flag={data.SvcPercent > 50} small />
      <SingleValue value={data.CapFactor} width="6%" flag={data.CapFactor < 0} small />
      <SingleValue value={`${data.CapPercent}%`} width="7%" flag={data.CapPercent > 200} small />
      <SingleValue value={data.CapPerCompany} width="7%" small />
      <SingleValue value={data.WrkFactor} width="6%" flag={data.WrkFactor < 0} small />
      <SingleValue value={`${data.WrkPercent}%`} width="6%" flag={data.WrkPercent < 90} small />
      <SingleValue value={data.Workers} width="6%" small />
      <SingleValue value={data.EduFactor} width="6%" flag={data.EduFactor < 0} small />
      <SingleValue value={data.TaxFactor} width="6%" flag={data.TaxFactor < 0} small />
    </div>
  );
};

// Props for the $Commercial component
interface CommercialProps {
  
    onClose: () => void
}

const $CommercialDemand: React.FC<CommercialProps> = ({ onClose }) => {
    
  const commercialDemand  = useValue(ResourceData$);
  
  

  
  
  const handleClose = useCallback(() => {
    onClose();
  }, [onClose]);

  return (
    <$Panel
      title="Commercial Demand"
      onClose={handleClose}
      initialSize={{ width: window.innerWidth * 0.37, height: window.innerHeight * 0.333 }}
      initialPosition={{ top: window.innerHeight * 0.05, left: window.innerWidth * 0.005 }}
    >
      {commercialDemand.length === 0 ? (
        <p>Waiting...</p>
      ) : (
        <div>
          <div className="labels_L7Q row_S2v">
            <div className="row_S2v" style={{ width: '3%' }} />
            <div className="row_S2v" style={{ width: '15%' }}>Resource</div>
            <SingleValue value="Demand" width="10%" />
            <SingleValue value="Free" width="4%" />
            <SingleValue value="Num" width="5%" />
            <SingleValue value="Service" width="12%" small />
            <SingleValue value="Sales Capacity" width="20%" small />
            <SingleValue value="Workers" width="18%" small />
            <SingleValue value="Edu" width="6%" small />
            <SingleValue value="Tax" width="6%" small />
          </div>

          {commercialDemand
            .filter(item => item.Resource !== 'NoResource')
            .map(item => (
              <ResourceLine key={item.Resource} data={item} />
            ))}
        </div>
      )}
    </$Panel>
  );
};

export default $CommercialDemand;


