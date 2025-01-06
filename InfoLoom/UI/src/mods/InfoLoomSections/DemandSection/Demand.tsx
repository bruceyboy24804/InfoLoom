import React, { FC, useCallback, useEffect, useState } from 'react';
import $Panel from 'mods/panel';
import { bindValue, useValue } from 'cs2/api';
import mod from 'mod.json';
import { cityInfo } from 'cs2/bindings';

// Declare global 'engine' if needed

interface AlignedParagraphProps {
  left: number;
  right: string;
}
// Define the TypeScript interface for demand data

// Binding for demand data
const Demand$ = bindValue<number[]>(mod.id, "ilBuildingDemand", []);

const AlignedParagraph: FC<AlignedParagraphProps> = ({ left, right }) => {
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
    marginLeft: '10%',
  };
  const rightTextStyle: React.CSSProperties = {
    fontSize: '80%',
    width: '60%',
    marginRight: '10%',
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

// Map factor names to display-friendly names
const getDisplayName = (factor: string): string => {
  const displayNames: { [key: string]: string } = {
    EmptyBuildings: 'Building Occupancy',
    Unemployment: 'Availability of Jobs',
    Homelessness: 'Cost of Living',
    Warehouses: 'Availability of Warehouses',
    UneducatedWorkforce: 'Labour Availability',
    EducatedWorkforce: 'High Skill Labour Availability',
    PetrolLocalDemand: 'Gas Station Availability',
  };
  return displayNames[factor] || factor;
};

// Render demand sections
const DemandSection2: FC<DemandSection2Props> = ({ title, value, factors }) => {
  return (
    <div
      className="infoview-panel-section_RXJ"
      style={{ width: '95%', paddingTop: '3rem', paddingBottom: '3rem' }}
    >
      {/* Title */}
      <div className="labels_L7Q row_S2v uppercase_RJI">
        <div className="left_Lgw row_S2v">{title}</div>
        {value >= 0 && (
          <div className="right_k30 row_S2v">{Math.round(value * 100)}</div>
        )}
      </div>
      <div className="space_uKL" style={{ height: '3rem' }}></div>
      {/* Factors */}
      {factors.map((item, index) => (
        <div
          key={index}
          className="labels_L7Q row_S2v small_ExK"
          style={{ marginTop: '1rem' }}
        >
          <div className="left_Lgw row_S2v">{getDisplayName(item.factor)}</div>
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

interface DemandFactorsProps {
  onClose: () => void;
}

const $DemandFactors: FC<DemandFactorsProps> = ({ onClose }) => {
  const [isPanelVisible, setIsPanelVisible] = useState(true);

  // Demand values
  const residentialLowDemand = useValue(cityInfo.residentialLowDemand$);
  const residentialMediumDemand = useValue(cityInfo.residentialMediumDemand$);
  const residentialHighDemand = useValue(cityInfo.residentialHighDemand$);
  const commercialDemand = useValue(cityInfo.commercialDemand$);
  const industrialDemand = useValue(cityInfo.industrialDemand$);
  const officeDemand = useValue(cityInfo.officeDemand$);

  // Demand factors
  const residentialLowFactors = useValue(cityInfo.residentialLowFactors$);
  const residentialMediumFactors = useValue(cityInfo.residentialMediumFactors$);
  const residentialHighFactors = useValue(cityInfo.residentialHighFactors$);
  const commercialFactors = useValue(cityInfo.commercialFactors$);
  const industrialFactors = useValue(cityInfo.industrialFactors$);
  const officeFactors = useValue(cityInfo.officeFactors$);

  // Building demand
  const ilBuildingDemand = useValue(Demand$);
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
  weight: ilBuildingDemand[index] ?? 0, 
}));

  const defaultPosition = {
    top: window.innerHeight * 0.05,
    left: window.innerWidth * 0.005,
  };
  const [panelPosition, setPanelPosition] = useState(defaultPosition);
  const [lastClosedPosition, setLastClosedPosition] = useState(defaultPosition);

  const handleSavePosition = useCallback((position: { top: number; left: number }) => {
    setPanelPosition(position);
  }, []);

  const handleClose = useCallback(() => {
    setLastClosedPosition(panelPosition);
    setIsPanelVisible(false);
    onClose();
  }, [onClose, panelPosition]);

  useEffect(() => {
    if (!isPanelVisible) {
      setPanelPosition(lastClosedPosition);
    }
  }, [isPanelVisible, lastClosedPosition]);

  if (!isPanelVisible) {
    return null;
  }

  return (
    <$Panel
      id="infoloom-demand"
      title="Demand"
      onClose={handleClose}
      initialSize={{ width: window.innerWidth * 0.11, height: window.innerHeight * 0.73 }}
      initialPosition={panelPosition}
      
    >
      <DemandSection2 title="BUILDING DEMAND" value={-1} factors={buildingDemandFactors} />
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
      <DemandSection2 title="COMMERCIAL" value={commercialDemand} factors={commercialFactors} />
      <DemandSection2 title="INDUSTRIAL" value={industrialDemand} factors={industrialFactors} />
      <DemandSection2 title="OFFICE" value={officeDemand} factors={officeFactors} />
    </$Panel>
  );
};

export default $DemandFactors;