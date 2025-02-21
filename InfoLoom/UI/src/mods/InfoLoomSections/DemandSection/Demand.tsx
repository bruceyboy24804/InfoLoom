import React, { FC, useCallback, useEffect, useState } from 'react';
import { useValue } from 'cs2/api';
import { cityInfo, Number2 } from 'cs2/bindings';
import {DraggablePanelProps, PanelProps, Scrollable, Panel} from 'cs2/ui';
import { BuildingDemandData } from '../../bindings';
import styles from './Demand.module.scss';

// Declare global 'engine' if needed

interface AlignedParagraphProps {
  left: number;
  right: string;
}

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
      style={{ width: '100%', paddingTop: '3rem', paddingBottom: '3rem' }}
    >
      {/* Title */}
      <div className="labels_L7Q row_S2v uppercase_RJI">
        <div
          className="left_Lgw row_S2v"
          style={{
            fontSize: 'var(--fontSizeM)',
          }}
        >
          {title}
        </div>
        {value >= 0 && (
          <div
            className="right_k30 row_S2v"
            style={{
              fontSize: 'var(--fontSizeM)',
            }}
          >
            {Math.round(value * 100)}
          </div>
        )}
      </div>
      <div className="space_uKL" style={{ height: '3rem' }}></div>
      {/* Factors */}
      {factors.map((item, index) => (
        <div key={index} className="labels_L7Q row_S2v small_ExK" style={{ marginTop: '1rem' }}>
          <div
            className="left_Lgw row_S2v"
            style={{
              fontSize: 'var(--fontSizeS)',
            }}
          >
            {getDisplayName(item.factor)}
          </div>
          <div className="right_k30 row_S2v">
            {item.weight < 0 ? (
              <div
                className="negative_YWY"
                style={{
                  fontSize: 'var(--fontSizeS)',
                }}
              >
                {item.weight}
              </div>
            ) : (
              <div
                className="positive_zrK"
                style={{
                  fontSize: 'var(--fontSizeS)',
                }}
              >
                {item.weight}
              </div>
            )}
          </div>
        </div>
      ))}
    </div>
  );
};



const $DemandFactors: FC<DraggablePanelProps> = ({ onClose, initialPosition }) => {
  


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
  const buildingDemandData = useValue(BuildingDemandData);
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
    weight: buildingDemandData[index] ?? 0,
  }));

  

  

  
  

  return (
    <Panel
      draggable
      onClose={onClose}
      initialPosition={initialPosition}
      className={styles.panel}
            header={
              <div className={styles.header}>
                <span className={styles.headerText}>Demand</span>
              </div>
            }
    >
      <Scrollable vertical={true} trackVisibility={'scrollable'} smooth={true}>
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
      </Scrollable>
    </Panel>
  );
};

export default $DemandFactors;