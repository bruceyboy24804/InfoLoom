import React, { FC, useCallback, useState } from 'react';
import useDataUpdate from 'mods/use-data-update';
import $Panel from 'mods/panel';

interface Factor {
  factor: string;
  weight: number;
}

interface DemandSection2Props {
  title: string;
  value: number;
  factors: Factor[];
}

const DemandSection2: FC<DemandSection2Props> = ({ title, value, factors }) => {
  return (
    <div className="infoview-panel-section_RXJ" style={{width: '95%', paddingTop: '3rem', paddingBottom: '3rem'}}>
      {/* title */}
      <div className="labels_L7Q row_S2v uppercase_RJI">
        <div className="left_Lgw row_S2v">{title}</div>
        { value >= 0 && ( <div className="right_k30 row_S2v">{Math.round(value*100)}</div> ) }
      </div>
      <div className="space_uKL" style={{height: '3rem'}}></div>
      {/* factors */}
      {factors.map((item, index) => (
        <div key={index} className="labels_L7Q row_S2v small_ExK" style={{marginTop: '1rem'}}>
          <div className="left_Lgw row_S2v">{item.factor}</div>
          <div className="right_k30 row_S2v">
          {item.weight < 0 ?
          <div className="negative_YWY">{item.weight}</div> :
          <div className="positive_zrK">{item.weight}</div>}
          </div>  
        </div>
      ))}
    </div>
  );  	
};

interface RowWithTwoColumnsProps {
  left: React.ReactNode;
  right: React.ReactNode;
}

const RowWithTwoColumns: FC<RowWithTwoColumnsProps> = ({left, right}) => {
  return (
    <div className="labels_L7Q row_S2v">
      <div className="row_S2v" style={{width: '60%'}}>{left}</div>
      <div className="row_S2v" style={{width: '40%', justifyContent: 'center'}}>{right}</div>
    </div>
  );
};

interface RowWithThreeColumnsProps {
  left: string;
  leftSmall: string;
  right1: number;
  flag1: boolean;
  right2?: number;
  flag2?: boolean;
}

const RowWithThreeColumns: FC<RowWithThreeColumnsProps> = ({left, leftSmall, right1, flag1, right2, flag2}) => {
  const centerStyle = {
    width: right2 === undefined ? '40%' : '20%',
    justifyContent: 'center',
  };
  const right1text = `${right1} %`;
  const right2text = right2 !== undefined ? `${right2} %` : '';
  return (
    <div className="labels_L7Q row_S2v">
      <div className="row_S2v" style={{width: '60%', flexDirection: 'column'}}>
        <p>{left}</p>
        <p style={{fontSize: '80%'}}>{leftSmall}</p>
      </div>
      <div className={`row_S2v ${flag1 ? 'negative_YWY' : 'positive_zrK'}`} style={centerStyle}>{right1text}</div>
      {right2 !== undefined && (
        <div className={`row_S2v ${flag2 ? 'negative_YWY' : 'positive_zrK'}`} style={centerStyle}>{right2text}</div>
      )}
    </div>
  );
};

const DataDivider: FC = () => {
  return (
    <div style={{display: 'flex', height: '4rem', flexDirection: 'column', justifyContent: 'center'}}>
      <div style={{borderBottom: '1px solid gray'}}></div>
    </div>
  );
};

interface ColumnCommercialDataProps {
  data: number[];
}

const ColumnCommercialData: FC<ColumnCommercialDataProps> = ({ data }) => {
  return (
    <div style={{width: '70%', boxSizing: 'border-box', border: '1px solid gray'}}>
      <RowWithTwoColumns left="EMPTY BUILDINGS" right={data[0]} />
      <RowWithTwoColumns left="PROPERTYLESS COMPANIES" right={data[1]} />
      
      <DataDivider />
      
      <RowWithThreeColumns left="AVERAGE TAX RATE" leftSmall="10% is the neutral rate" right1={data[2]/10} flag1={data[2]>100} />
      
      <DataDivider />

      <div className="labels_L7Q row_S2v">
        <div className="row_S2v" style={{width: '60%'}} />
        <div className="row_S2v" style={{width: '20%', justifyContent: 'center'}}>Standard</div>
        <div className="row_S2v" style={{width: '20%', justifyContent: 'center'}}>Leisure</div>
      </div>
      
      <RowWithThreeColumns left="SERVICE UTILIZATION" leftSmall="30% is the neutral ratio" right1={data[3]} flag1={data[3]<30} right2={data[4]} flag2={data[4]<30} />
      <RowWithThreeColumns left="PRODUCTION CAPACITY" leftSmall="100% when production capacity = resources needs" right1={data[5]} flag1={data[5]>100} right2={data[6]} flag2={data[6]>100} />
      
      <DataDivider />
      
      <RowWithThreeColumns left="EMPLOYEE CAPACITY RATIO" leftSmall="75% is the neutral ratio" right1={data[7]/10} flag1={data[7]<750} />
      
      <DataDivider />
      
      <div className="labels_L7Q row_S2v">
        <div className="row_S2v" style={{width: '60%'}}>
          <p style={{margin: 0}}>AVAILABLE WORKFORCE</p>
        </div>
        <div className="row_S2v" style={{width: '40%', flexDirection: 'column'}}>
          <RowWithTwoColumns left="Educated" right={data[8]} />
          <RowWithTwoColumns left="Uneducated" right={data[9]} />
        </div>
      </div>
    </div>
  );
};

interface ColumnExcludedResourcesProps {
  resources: string[];
}

const ColumnExcludedResources: FC<ColumnExcludedResourcesProps> = ({ resources }) => {
  return (
    <div style={{width: '30%', boxSizing: 'border-box', border: '1px solid gray'}}>
      <div className="labels_L7Q row_S2v">
        <div className="row_S2v" style={{width: '100%'}}>
          <p style={{margin: 0}}>NO DEMAND FOR</p>
        </div>
      </div>
      <ul style={{listStyleType: 'none', padding: 0, margin: 0}}>
        {resources.map((item, index) => (
          <li key={index}>
            <div className="row_S2v small_ExK">{item}</div>
          </li>
        ))}
      </ul>
    </div>
  );
};

interface CommercialProps {
  
  onClose: () => void;
}

const $Commercial: FC<CommercialProps> = ({ onClose }) => {
  const [commercialData, setCommercialData] = useState<number[]>([]);
  useDataUpdate('cityInfo.ilCommercial', setCommercialData);
  
  const [excludedResources, setExcludedResources] = useState<string[]>([]);
  useDataUpdate('cityInfo.ilCommercialExRes', setExcludedResources);

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
      title="Commercial Data" 
      onClose={handleClose}
      initialSize={{ width: window.innerWidth * 0.25, height: window.innerHeight * 0.31 }} 
      initialPosition={{ top: window.innerHeight * 0.05, left: window.innerWidth * 0.005 }}
    >	
      {commercialData.length === 0 ? (
        <p>Waiting...</p>
      ) : (
        <div style={{display: 'flex'}}>
          <ColumnCommercialData data={commercialData} />
          <ColumnExcludedResources resources={excludedResources} />
        </div>
      )}
    </$Panel>
  );
};

export default $Commercial;
  
