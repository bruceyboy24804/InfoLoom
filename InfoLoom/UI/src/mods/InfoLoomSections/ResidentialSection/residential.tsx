import React, { useState, useEffect } from 'react';
import useDataUpdate from 'mods/use-data-update';
import $Panel from 'mods/panel';
import engine from 'cohtml/cohtml';
type ResidentialData = number[];
type RowWithTwoColumnsProps = {
  left: React.ReactNode;
  right: React.ReactNode;
};

const RowWithTwoColumns: React.FC<RowWithTwoColumnsProps> = ({ left, right }) => {
  
  return (
    <div className="labels_L7Q row_S2v">
      <div className="row_S2v" style={{ width: '70%' }}>{left}</div>
      <div className="row_S2v" style={{ width: '30%', justifyContent: 'center' }}>{right}</div>
    </div>
  );
};

type RowWithThreeColumnsProps = {
  left: React.ReactNode;
  leftSmall?: string;
  right1: React.ReactNode;
  flag1?: boolean;
  right2?: React.ReactNode;
  flag2?: boolean;
};

const RowWithThreeColumns: React.FC<RowWithThreeColumnsProps> = ({ left, leftSmall, right1, flag1, right2, flag2 }) => {
  
  const centerStyle = {
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
        <div className="row_S2v negative_YWY" style={centerStyle}>{right1text}</div>
      ) : (
        <div className="row_S2v positive_zrK" style={centerStyle}>{right1text}</div>
      )}
      {right2 && (
        flag2 ? (
          <div className="row_S2v negative_YWY" style={centerStyle}>{right2text}</div>
        ) : (
          <div className="row_S2v positive_zrK" style={centerStyle}>{right2text}</div>
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

type SingleValueProps = {
  value: React.ReactNode;
  flag?: boolean;
  width?: string;
  small?: boolean;
};

const SingleValue: React.FC<SingleValueProps> = ({ value, flag, width, small }) => {
 
  const rowClass = small ? "row_S2v small_ExK" : "row_S2v";
  const centerStyle = {
    width: width === undefined ? '16%' : width,
    justifyContent: 'center',
  };
  return flag === undefined ? (
    <div className={rowClass} style={centerStyle}>{value}</div>
  ) : (
    flag ? (
      <div className={`${rowClass} negative_YWY`} style={centerStyle}>{value}</div>
    ) : (
      <div className={`${rowClass} positive_zrK`} style={centerStyle}>{value}</div>
    )
  );
};

type BuildingDemandSectionProps = {
  data: number[];
};

const BuildingDemandSection: React.FC<BuildingDemandSectionProps> = ({ data }) => {
  
  const freeL = data[0] - data[3];
  const freeM = data[1] - data[4];
  const freeH = data[2] - data[5];
  const ratio = data[6] / 10;
  const needL = Math.max(1, Math.floor(ratio * data[0] / 100));
  const needM = Math.max(1, Math.floor(ratio * data[1] / 100));
  const needH = Math.max(1, Math.floor(ratio * data[2] / 100));
  const demandL = Math.floor((1 - freeL / needL) * 100);
  const demandM = Math.floor((1 - freeM / needM) * 100);
  const demandH = Math.floor((1 - freeH / needH) * 100);
  const totalRes = data[0] + data[1] + data[2];
  const totalOcc = data[3] + data[4] + data[5];
  const totalFree = totalRes - totalOcc;
  const freeRatio = totalRes > 0 ? Math.round(1000 * totalFree / totalRes) / 10 : 0;

  return (
    <div style={{ boxSizing: 'border-box', border: '1px solid gray' }}>
      <div className="labels_L7Q row_S2v">
        <div className="row_S2v" style={{ width: '36%' }}></div>
        <SingleValue value="LOW" />
        <SingleValue value="MEDIUM" />
        <SingleValue value="HIGH" />
        <SingleValue value="TOTAL" />
      </div>
      <div className="labels_L7Q row_S2v">
        <div className="row_S2v" style={{ width: '2%' }}></div>
        <div className="row_S2v" style={{ width: '34%' }}>Total properties</div>
        <SingleValue value={data[0]} />
        <SingleValue value={data[1]} />
        <SingleValue value={data[2]} />
        <SingleValue value={totalRes} />
      </div>
      {/* Rest of the sections */}
    </div>
  );
};

const $Residential = ({ react }: React.ComponentProps<any>) => {
    const [residentialData, setResidentialData] = useState<ResidentialData>([]);

  useEffect(() => {
   
  }, [residentialData]);
  useEffect(() => {
    // Poll data from C# method periodically
    const intervalId = setInterval(async () => {
      const data = engine.call('ResidentialDemandUISystem.GetResidentialData');  // Call the C# method
      setResidentialData(await data);
      
    }, 1000);  // Poll every second

    return () => clearInterval(intervalId);  // Cleanup on unmount
  }, []);
 

  const homelessThreshold = Math.round(residentialData[12] * residentialData[13] / 1000);

  return (
    <$Panel
      react={react}
      title="Residential Data"
     
      initialSize={{ width: window.innerWidth * 0.25, height: window.innerHeight * 0.26 }}
      initialPosition={{ top: window.innerHeight * 0.05, left: window.innerWidth * 0.005 }}
    >
      {residentialData.length === 0 ? (
        <p>Waiting...</p>
      ) : (
        <div>
          <BuildingDemandSection data={residentialData} />
          <div style={{ display: 'flex' }}>
            {/* Left Column */}
            <div style={{ width: '50%', boxSizing: 'border-box', border: '1px solid gray' }}>
              <RowWithTwoColumns left="STUDY POSITIONS" right={residentialData[14]} />
              {/* Add other sections as needed */}
            </div>
            {/* Right Column */}
            <div style={{ width: '50%', boxSizing: 'border-box', border: '1px solid gray' }}>
              <RowWithTwoColumns left="HOUSEHOLDS" right={residentialData[12]} />
              {/* Add other sections as needed */}
            </div>
          </div>
        </div>
      )}
    </$Panel>
  );
};

export default $Residential;
