import React, { FC, useCallback, useState } from 'react';
import $Panel from 'mods/panel';
import useDataUpdate from 'mods/use-data-update';

// Declare 'engine' if it's a global variable


interface AlignedParagraphProps {
  left: number;
  right: string;
}

const AlignedParagraph: React.FC<AlignedParagraphProps> = ({ left, right }) => {
  // Set color based on value of left
  let color: string;
  if (left < -50) {
    color = 'red';
  } else if (left > 50) {
    color = '#00CC00';
  } else {
    color = 'white'; // default
  }

  const containerStyle: React.CSSProperties = {
    display: 'flex',
    justifyContent: 'space-between',
    textAlign: 'justify',
    marginBottom: '0.1em', // Add some spacing between the <p> tags
  };
  const leftTextStyle: React.CSSProperties = {
    color: color,
    fontSize: '80%',
    width: '20%',
    marginLeft: '10%', // Start 10% from the left edge
  };
  const rightTextStyle: React.CSSProperties = {
    fontSize: '80%',
    width: '60%',
    marginRight: '10%', // Start 10% from the right edge
    textAlign: 'right',
  };
  return (
    <p style={containerStyle}>
      <span style={leftTextStyle}>{left}</span>
      <span style={rightTextStyle}>{right}</span>
    </p>
  );
};

interface DemandSection2Props {
  title: string;
  value: number;
  factors: { factor: string; weight: number }[];
}

const DemandSection2: React.FC<DemandSection2Props> = ({ title, value, factors }) => {
  return (
    <div
      className="infoview-panel-section_RXJ"
      style={{ width: '95%', paddingTop: '3rem', paddingBottom: '3rem' }}
    >
      {/* title */}
      <div className="labels_L7Q row_S2v uppercase_RJI">
        <div className="left_Lgw row_S2v">{title}</div>
        {value >= 0 && (
          <div className="right_k30 row_S2v">{Math.round(value * 100)}</div>
        )}
      </div>
      <div className="space_uKL" style={{ height: '3rem' }}></div>
      {/* factors */}
      {factors.map((item, index) => (
        <div
          key={index}
          className="labels_L7Q row_S2v small_ExK"
          style={{ marginTop: '1rem' }}
        >
          <div className="left_Lgw row_S2v">{item.factor}</div>
          <div className="right_k30 row_S2v">
            {item.weight < 0 ? (
              <div className="negative_YWY">{item.weight}</div>
            ) : (
              <div className="positive_zrK">{item.weight}</div>
            )}
          </div>
        </div>
      ))}
    </div>
  );
};

// DemandSection1 is not used in the final code, so it's omitted.
interface DemandFactorsProps {
  onClose: () => void;
}


const $DemandFactors: FC<DemandFactorsProps> = ({ onClose }) => {
  

  // Demand values are just single numbers
  const [residentialLowDemand, setResidentialLowDemand] = useState<number>(0);
  useDataUpdate('cityInfo.residentialLowDemand', setResidentialLowDemand);

  const [residentialMediumDemand, setResidentialMediumDemand] = useState<number>(0);
  useDataUpdate('cityInfo.residentialMediumDemand', setResidentialMediumDemand);

  const [residentialHighDemand, setResidentialHighDemand] = useState<number>(0);
  useDataUpdate('cityInfo.residentialHighDemand', setResidentialHighDemand);

  const [commercialDemand, setCommercialDemand] = useState<number>(0);
  useDataUpdate('cityInfo.commercialDemand', setCommercialDemand);

  const [industrialDemand, setIndustrialDemand] = useState<number>(0);
  useDataUpdate('cityInfo.industrialDemand', setIndustrialDemand);

  const [officeDemand, setOfficeDemand] = useState<number>(0);
  useDataUpdate('cityInfo.officeDemand', setOfficeDemand);

  // Demand factors: an array with properties: __Type, factor, weight
  type DemandFactor = { __Type?: string; factor: string; weight: number };

  const [residentialLowFactors, setResidentialLowFactors] = useState<DemandFactor[]>([]);
  useDataUpdate('cityInfo.residentialLowFactors', setResidentialLowFactors);

  const [residentialMediumFactors, setResidentialMediumFactors] = useState<DemandFactor[]>([]);
  useDataUpdate('cityInfo.residentialMediumFactors', setResidentialMediumFactors);

  const [residentialHighFactors, setResidentialHighFactors] = useState<DemandFactor[]>([]);
  useDataUpdate('cityInfo.residentialHighFactors', setResidentialHighFactors);

  const [commercialFactors, setCommercialFactors] = useState<DemandFactor[]>([]);
  useDataUpdate('cityInfo.commercialFactors', setCommercialFactors);

  const [industrialFactors, setIndustrialFactors] = useState<DemandFactor[]>([]);
  useDataUpdate('cityInfo.industrialFactors', setIndustrialFactors);

  const [officeFactors, setOfficeFactors] = useState<DemandFactor[]>([]);
  useDataUpdate('cityInfo.officeFactors', setOfficeFactors);

  // Building demand
  const [buildingDemand, setBuildingDemand] = useState<number[]>([]);
  useDataUpdate('cityInfo.ilBuildingDemand', setBuildingDemand);

  // Convert buildingDemand array into "demand factors"
  const titles = [
    'Residential Low',
    'Residential Medium',
    'Residential High',
    'Commercial',
    'Industrial',
    'Storage',
    'Office',
  ];
  const buildingDemandFactors = titles.map((factor, index) => ({
    factor,
    weight: buildingDemand[index] || 0,
  }));

  

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
      title="Demand"
      onClose={handleClose}
      initialSize={{ width: window.innerWidth * 0.11, height: window.innerHeight * 0.73 }}
      initialPosition={{
        top: window.innerHeight * 0.05,
        left: window.innerWidth * 0.005,
      }}
      
    >
      <DemandSection2
        title="BUILDING DEMAND"
        value={-1}
        factors={buildingDemandFactors}
      />
      <DemandSection2
        title="RESIDENTIAL LOW"
        value={residentialLowDemand}
        factors={residentialLowFactors}
      />
      <DemandSection2
        title="RESIDENTIAL MEDIUM"
        value={residentialMediumDemand}
        factors={residentialMediumFactors}
      />
      <DemandSection2
        title="RESIDENTIAL HIGH"
        value={residentialHighDemand}
        factors={residentialHighFactors}
      />
      <DemandSection2
        title="COMMERCIAL"
        value={commercialDemand}
        factors={commercialFactors}
      />
      <DemandSection2
        title="INDUSTRIAL"
        value={industrialDemand}
        factors={industrialFactors}
      />
      <DemandSection2 title="OFFICE" value={officeDemand} factors={officeFactors} />
    </$Panel>
  );
};

export default $DemandFactors;

// Registering the panel with HookUI so it shows up in the menu
/*
window._$hookui.registerPanel({
  id: 'infoloom.demandfactors',
  name: 'InfoLoom: Demand Factors',
  icon: 'Media/Game/Icons/ZoningDemand.svg',
  component: $DemandFactors,
});
*/
