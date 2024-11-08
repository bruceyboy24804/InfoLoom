import React, { useState, useCallback, FC } from 'react';
import useDataUpdate from 'mods/use-data-update';
import $Panel from 'mods/panel';


// Declare the global 'engine' object to avoid TypeScript errors.
// You should replace 'any' with the appropriate type if available.


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
      <div style={{ borderBottom: '1px solid white' }}></div>
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
interface ResourceData {
  resource: string;
  demand: number;
  building: number;
  free: number;
  companies: number;
  svcfactor: number;
  svcpercent: number;
  capfactor: number;
  cappercent: number;
  cappercompany: number;
  workers: number;
  wrkfactor: number;
  wrkpercent: number;
  taxfactor: number;
}

interface ResourceLineProps {
  data: ResourceData;
}

// Component: ResourceLine
const ResourceLine: React.FC<ResourceLineProps> = ({ data }) => {
  return (
    <div className="labels_L7Q row_S2v" style={{ lineHeight: 0.7 }}>
      <div className="row_S2v" style={{ width: '3%' }}></div>
      <div className="row_S2v" style={{ width: '15%' }}>
        {data.resource}
      </div>
      <SingleValue value={data.demand} width="6%" flag={data.demand < 0} />
      <SingleValue value={data.building} width="4%" flag={data.building <= 0} />
      <SingleValue value={data.free} width="4%" flag={data.free <= 0} />
      <SingleValue value={data.companies} width="5%" />

    
      <SingleValue value={`${data.svcpercent}%`} width="12%" flag={data.svcpercent > 50} small={true} />

      <SingleValue value={data.cappercompany} width="10%" small={true} />
      <SingleValue value={`${data.cappercent}%`} width="10%" flag={data.cappercent > 200} small={true} />
      
      <SingleValue value={data.workers} width="9%" small={true} />
      <SingleValue value={`${data.wrkpercent}%`} width="9%" flag={data.wrkpercent < 90} small={true} />

      <SingleValue value={data.taxfactor} width="12%" flag={data.taxfactor < 0} small={true} />
    </div>
    // <div className="row_S2v" style={{ width: '45%', fontSize: '80%' }}>{data.details}</div>
  );
};

// Interface for $Commercial props
interface CommercialProps {
  
  onClose: () => void;
}

// Component: $Commercial
const $Commercial: FC<CommercialProps> = ({ onClose }) => {
  // Demand data for each resource
  const [demandData, setDemandData] = useState<ResourceData[]>([]);

  // Custom hook to update data
  useDataUpdate('realEco.commercialDemand', setDemandData);

  // State to control panel visibility
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
      title="Commercial Products"
      onClose={handleClose}
      initialSize={{ width: window.innerWidth * 0.45, height: window.innerHeight * 0.32 }}
      initialPosition={{ top: window.innerHeight * 0.05, left: window.innerWidth * 0.005 }}
    >
      {demandData.length === 0 ? (
        <p>Waiting...</p>
      ) : (
        <div>
          <div className="labels_L7Q row_S2v">
            <div className="row_S2v" style={{ width: '3%' }}></div>
            <div className="row_S2v" style={{ width: '15%' }}>
              Resource
            </div>
            <SingleValue value="Demand" width="10%" />
            <SingleValue value="Free" width="4%" />
            <SingleValue value="Num" width="5%" />
            <SingleValue value="Service" width="12%" small={true} />
            <SingleValue value="Household Need" width="20%" small={true} />
            <SingleValue value="Workers" width="18%" small={true} />
            <SingleValue value="Tax" width="12%" small={true} />
          </div>

          {demandData
            .filter((item) => item.resource !== 'NoResource')
            .map((item) => (
              <ResourceLine key={item.resource} data={item} />
            ))}
        </div>
      )}
    </$Panel>
  );
};

export default $Commercial;

// Registering the panel with HookUI so it shows up in the menu
/*
window._$hookui.registerPanel({
  id: "realeco.commercial",
  name: "RealEco: Commercial",
  icon: "Media/Game/Icons/ZoneCommercial.svg",
  component: $Commercial
});
*/
