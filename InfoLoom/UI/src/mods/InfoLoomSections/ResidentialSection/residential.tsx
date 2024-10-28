import React, { FC, useCallback, useState } from 'react';
import useDataUpdate from 'mods/use-data-update';
import $Panel from 'mods/panel';

interface RowWithTwoColumnsProps {
  left: React.ReactNode;
  right: React.ReactNode;
}

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
  right1: React.ReactNode;
  flag1: boolean;
  right2?: React.ReactNode;
  flag2?: boolean;
}

const RowWithThreeColumns: React.FC<RowWithThreeColumnsProps> = ({ left, leftSmall, right1, flag1, right2, flag2 }) => {
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
        <div className="row_S2v negative_YWY" style={centerStyle}>{right1text}</div>
      ) : (
        <div className="row_S2v positive_zrK" style={centerStyle}>{right1text}</div>
      )}
      {right2 !== undefined && (
        flag2 ? (
          <div className="row_S2v negative_YWY" style={centerStyle}>{right2text}</div>
        ) : (
          <div className="row_S2v positive_zrK" style={centerStyle}>{right2text}</div>
        )
      )}
    </div>
  );
};

// Simple horizontal line
const DataDivider: React.FC = () => {
  return (
    <div style={{ display: 'flex', height: '4rem', flexDirection: 'column', justifyContent: 'center' }}>
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
const SingleValue: React.FC<SingleValueProps> = ({ value, flag, width, small }) => {
  const rowClass = small ? 'row_S2v small_ExK' : 'row_S2v';
  const centerStyle: React.CSSProperties = {
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

interface BuildingDemandSectionProps {
  data: number[];
}

const BuildingDemandSection: React.FC<BuildingDemandSectionProps> = ({ data }) => {
  const freeL = data[0] - data[3];
  const freeM = data[1] - data[4];
  const freeH = data[2] - data[5];
  const ratio = data[6] / 10;
  const ratioString = `No demand at ${ratio}%`;
  const needL = Math.max(1, Math.floor((ratio * data[0]) / 100));
  const needM = Math.max(1, Math.floor((ratio * data[1]) / 100));
  const needH = Math.max(1, Math.floor((ratio * data[2]) / 100));
  const demandL = Math.floor((1 - freeL / needL) * 100);
  const demandM = Math.floor((1 - freeM / needM) * 100);
  const demandH = Math.floor((1 - freeH / needH) * 100);
  // Calculate totals and free ratio
  const totalRes = data[0] + data[1] + data[2];
  const totalOcc = data[3] + data[4] + data[5];
  const totalFree = totalRes - totalOcc;
  const freeRatio = totalRes > 0 ? Math.round((1000 * totalFree) / totalRes) / 10 : 0;
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
      <div className="labels_L7Q row_S2v">
        <div className="row_S2v small_ExK" style={{ width: '2%' }}></div>
        <div className="row_S2v small_ExK" style={{ width: '34%' }}>- Occupied properties</div>
        <SingleValue value={data[3]} small={true} />
        <SingleValue value={data[4]} small={true} />
        <SingleValue value={data[5]} small={true} />
        <SingleValue value={totalOcc} small={true} />
      </div>
      <div className="labels_L7Q row_S2v">
        <div className="row_S2v" style={{ width: '2%' }}></div>
        <div className="row_S2v" style={{ width: '34%' }}>= Empty properties</div>
        <SingleValue value={freeL} flag={freeL > needL} />
        <SingleValue value={freeM} flag={freeM > needM} />
        <SingleValue value={freeH} flag={freeH > needH} />
        <SingleValue value={totalFree} />
      </div>
      <div className="labels_L7Q row_S2v">
        <div className="row_S2v small_ExK" style={{ width: '2%' }}></div>
        <div className="row_S2v small_ExK" style={{ width: '34%' }}>{`No demand at ${ratio}%`}</div>
        <SingleValue value={needL} small={true} />
        <SingleValue value={needM} small={true} />
        <SingleValue value={needH} small={true} />
        <div className="row_S2v" style={{ width: '16%' }}></div>
      </div>
      <div className="labels_L7Q row_S2v">
        <div className="row_S2v" style={{ width: '2%' }}></div>
        <div className="row_S2v" style={{ width: '34%' }}>BUILDING DEMAND</div>
        <SingleValue value={demandL} flag={demandL < 0} />
        <SingleValue value={demandM} flag={demandM < 0} />
        <SingleValue value={demandH} flag={demandH < 0} />
        <SingleValue value={`${freeRatio}%`} />
      </div>
      <div className="space_uKL" style={{ height: '3rem' }}></div>
    </div>
  );
};

interface ResidentialProps {
  onClose: () => void;
}

const Residential: FC<ResidentialProps> = ({ onClose }) => {
  // Residential data
  const [residentialData, setResidentialData] = useState<number[]>([]);
  useDataUpdate('cityInfo.ilResidential', setResidentialData);

  // New state to control panel visibility
  const [isPanelVisible, setIsPanelVisible] = useState(true);

  // Handler for closing the panel
  const handleClose = useCallback(() => {
    onClose();
  }, [onClose]);

  if (!isPanelVisible) {
    return null;
  }

  const homelessThreshold = residentialData.length > 13 ? Math.round((residentialData[12] * residentialData[13]) / 1000) : 0;

  return (
    <$Panel
      title="Residential Data"
      onClose={handleClose}
      initialSize={{ width: window.innerWidth * 0.25, height: window.innerHeight * 0.30 }}
      initialPosition={{ top: window.innerHeight * 0.05, left: window.innerWidth * 0.005 }}
    >
      {residentialData.length === 0 ? (
        <p>Waiting...</p>
      ) : (
        <div>
          <BuildingDemandSection data={residentialData} />
          {/* OTHER DATA, two columns */}
          <div style={{ display: 'flex' }}>
            <div style={{ width: '50%', boxSizing: 'border-box', border: '1px solid gray', paddingLeft: '10rem', paddingRight: '10rem'  }}>
              <div className="space_uKL" style={{ height: '3rem' }}></div>
              <RowWithTwoColumns left="STUDY POSITIONS" right={residentialData[14]} />
              <DataDivider />
              <RowWithThreeColumns
                left="HAPPINESS"
                leftSmall={`${residentialData[8]} is neutral`}
                right1={residentialData[7]}
                flag1={residentialData[7] < residentialData[8]}
              />
              <DataDivider />
              <RowWithThreeColumns
              left="UNEMPLOYMENT"
              leftSmall={`${residentialData[10] / 10}% is neutral`}
              right1={`${(residentialData[9] / 100).toFixed(2)}`}
              flag1={residentialData[9] >= residentialData[10] * 10}  // Changed this line
            />
              <DataDivider />
              <RowWithThreeColumns left="HOUSEHOLD DEMAND" right1={residentialData[16]} flag1={residentialData[16] < 0} />
              <div className="space_uKL" style={{ height: '3rem' }}></div>
            </div>
            <div style={{ width: '50%', boxSizing: 'border-box', border: '1px solid gray', paddingLeft: '10rem', paddingRight: '10rem' }}>
              <div className="space_uKL" style={{ height: '3rem' }}></div>
              <RowWithTwoColumns left="HOUSEHOLDS" right={residentialData[12]} />
              <DataDivider />
              <RowWithThreeColumns
                left="HOMELESS"
                leftSmall={`${homelessThreshold} is neutral`}
                right1={residentialData[11]}
                flag1={residentialData[11] > homelessThreshold}
              />
              <DataDivider />
              <RowWithThreeColumns
                left="TAX RATE (weighted)"
                leftSmall="10% is neutral"
                right1={residentialData[15] / 10}
                flag1={residentialData[15] > 100}
              />
              <DataDivider />
              <RowWithTwoColumns left="STUDENT CHANCE" right={`${residentialData[17]} %`} />
              <div className="space_uKL" style={{ height: '3rem' }}></div>
            </div>
          </div>
        </div>
      )}
    </$Panel>
  );
};

export default Residential;

/* Note: If you are using HookUI or need to register the panel, uncomment and adjust the following code:

// Registering the panel with HookUI so it shows up in the menu
window._$hookui.registerPanel({
  id: 'infoloom.residential',
  name: 'InfoLoom: Residential Data',
  icon: 'Media/Game/Icons/ZoneResidential.svg',
  component: $Residential,
});

*/
