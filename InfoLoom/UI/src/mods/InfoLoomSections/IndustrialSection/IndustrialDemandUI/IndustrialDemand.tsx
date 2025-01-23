import React, { FC, useCallback, useState } from 'react';

import $Panel from 'mods/panel';
import {bindValue, useValue} from "cs2/api";
import mod from "mod.json";

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

interface ColumnIndustrialDataProps {
  data: number[];
}

interface ColumnExcludedResourcesProps {
  resources: string[];
}

// Component Definitions
const IndustrialDemand$ = bindValue<number[]>(mod.id, "ilIndustrial", []);
const IndustrialExRes$ = bindValue<string[]>(mod.id, "ilIndustrialExRes", []);
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

const ColumnIndustrialData: FC<ColumnIndustrialDataProps> = ({ data }) => {
  return (
    <div style={{ width: '70%', boxSizing: 'border-box', border: '1px solid gray' }}>
      <div className="labels_L7Q row_S2v">
        <div className="row_S2v" style={{ width: '60%' }}></div>
        <SingleValue value="INDUSTRIAL" />
        <SingleValue value="OFFICE" />
      </div>

      <div className="labels_L7Q row_S2v">
        <div className="row_S2v" style={{ width: '60%' }}>
          EMPTY BUILDINGS
        </div>
        <SingleValue value={data[0]} />
        <SingleValue value={data[10]} />
      </div>
      <div className="labels_L7Q row_S2v">
        <div className="row_S2v" style={{ width: '60%' }}>
          PROPERTYLESS COMPANIES
        </div>
        <div className="row_S2v" style={{ width: '20%', justifyContent: 'center' }}>
          {data[1]}
        </div>
        <div className="row_S2v" style={{ width: '20%', justifyContent: 'center' }}>
          {data[11]}
        </div>
      </div>

      <DataDivider />

      <RowWithThreeColumns
        left="AVERAGE TAX RATE"
        leftSmall="10% is the neutral rate"
        right1={data[2] / 10}
        flag1={data[2] > 100}
        right2={data[12] / 10}
        flag2={data[12] > 100}
      />

      <DataDivider />

      <RowWithThreeColumns
        left="LOCAL DEMAND (ind)"
        leftSmall="100% when production = demand"
        right1={data[3]}
        flag1={data[3] > 100}
      />
      <RowWithThreeColumns
        left="INPUT UTILIZATION (ind)"
        leftSmall="110% is the neutral ratio, capped at 400%"
        right1={data[7]}
        flag1={data[7] > 100}
      />

      <DataDivider />

      <RowWithThreeColumns
        left="EMPLOYEE CAPACITY RATIO"
        leftSmall="72% is the neutral ratio"
        right1={data[4] / 10}
        flag1={data[4] < 720}
        right2={data[14] / 10}
        flag2={data[14] < 750}
      />

      <DataDivider />

      <div style={{ display: 'flex' }}>
        <div
          style={{
            width: '60%',
            height: '2.2em',
            display: 'flex',
            alignItems: 'center',
            fontSize: '15rem',
            color: 'white',
          }}
        >
          AVAILABLE WORKFORCE
        </div>
        <div style={{ width: '40%' }}>
          <RowWithTwoColumns left="Educated" right={data[8]} />
          <RowWithTwoColumns left="Uneducated" right={data[9]} />
        </div>
      </div>

      <DataDivider />

      <div style={{ display: 'flex' }}>
        <div
          style={{
            width: '50%',
            height: '2.2em',
            display: 'flex',
            flexDirection: 'column',
          }}
        >
          <p style={{ fontSize: '15rem', color: 'white' }}>
            STORAGE</p>
          <p style={{ fontSize: '12rem', color: 'white' }}>
            The game will spawn warehouses when DEMANDED TYPES exist.
          </p>
        </div>
        <div style={{ width: '50%' }}>
          <RowWithTwoColumns left="Empty buildings" right={data[5]} />
          <RowWithTwoColumns left="Propertyless companies" right={data[6]} />
          <RowWithTwoColumns left="DEMANDED TYPES" right={data[15]} />
        </div>
      </div>
    </div>
  );
};

const ColumnExcludedResources: FC<ColumnExcludedResourcesProps> = ({ resources }) => {
  return (
    <div style={{ width: '30%', boxSizing: 'border-box', border: '1px solid gray' }}>
      <div className="labels_L7Q row_S2v">
        <div className="row_S2v" style={{ width: '100%' }}>
          <p style={{ margin: 0 }}>NO DEMAND FOR</p>
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


// Declare the engine object if it's globally available


interface IndustrialProps {
  onClose: () => void;
}

const $Industrial: React.FC<IndustrialProps> = ({ onClose }) => {
  // Commercial data
  const ilIndustrial = useValue(IndustrialDemand$);  
  const ilIndustrialExRes = useValue(IndustrialExRes$);

  const [isPanelVisible, setIsPanelVisible] = useState(true);

  // Handler for closing the panel
  const handleClose = useCallback(() => {
    onClose();
  }, [onClose]);

  if (!isPanelVisible) {
    return null;
  }

  return (
    <$Panel
      id="infoloom-industrial"
      title="Industrial and Office Data"
      onClose={handleClose}
      initialSize={{ width: window.innerWidth * 0.3, height: window.innerHeight * 0.60 }}
      initialPosition={{ top: window.innerHeight * 0.05, left: window.innerWidth * 0.005 }}
    >
      {ilIndustrial.length === 0 ? (
        <p>Waiting...</p>
      ) : (
        <div style={{ display: 'flex' }}>
          <ColumnIndustrialData data={ilIndustrial} />
          <ColumnExcludedResources resources={ilIndustrialExRes} />
        </div>
      )}
    </$Panel>
  );
};

export default $Industrial;
