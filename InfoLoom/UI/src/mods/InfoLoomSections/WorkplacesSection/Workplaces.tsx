import React, { FC, useCallback, useEffect, useState } from 'react';
import $Panel from 'mods/panel';
import { useValue, bindValue } from 'cs2/api';
import mod from 'mod.json';
import {PanelProps, DraggablePanelProps, Panel} from "cs2/ui";
import styles from "./Workplaces.module.scss";

// Define interface for workplaces information
interface WorkplacesInfo {
  Total: number;
  Service: number;
  Commercial: number;
  Leisure: number;
  Extractor: number;
  Industrial: number;
  Office: number;
  Employee: number;
  Commuter: number;
  Open: number;
  Name: string;
}

// Default placeholder for workplacesInfo
const defaultWorkplacesInfo: WorkplacesInfo = {
  Total: 0,
  Service: 0,
  Commercial: 0,
  Leisure: 0,
  Extractor: 0,
  Industrial: 0,
  Office: 0,
  Employee: 0,
  Commuter: 0,
  Open: 0,
  Name: '',
};

// Bind workplaces data using CS2 API
const IlWorkplaces$ = bindValue<WorkplacesInfo[]>(mod.id, 'ilWorkplaces');

// Define props for WorkforceLevel component
interface WorkforceLevelProps {
  levelColor?: string;
  levelName: string;
  levelValues: WorkplacesInfo;
  total: number;
  showAll?: boolean;
}

// WorkforceLevel Component
const WorkforceLevel: React.FC<WorkforceLevelProps> = ({
  levelColor,
  levelName,
  levelValues,
  total,
  showAll = true,
}) => {
  const percent =
    total > 0 && typeof levelValues.Total === 'number'
      ? `${((100 * levelValues.Total) / total).toFixed(1)}%`
      : '';

  return (
    <div
      className="row"
      style={{
        display: 'flex',
        alignItems: 'center',
        padding: '1rem',
        width: '100%',
        color: 'white', // Ensures white text
      }}
    >
      <div style={{ display: 'flex', alignItems: 'center', width: '20%' }}>
        {levelColor && (
          <div
            style={{
              backgroundColor: levelColor,
              width: '1.2em',
              height: '1.2em',
              marginRight: '0.5em',
            }}
          />
        )}
        <div>{levelName}</div>
      </div>
      <div style={{ width: '8%', textAlign: 'center' }}>{levelValues.Total}</div>
      <div style={{ width: '7%', textAlign: 'center' }}>{percent}</div>

      {showAll && (
        <>
          <div style={{ width: '6%', textAlign: 'center' }}>{levelValues.Service}</div>
          <div style={{ width: '6%', textAlign: 'center' }}>{levelValues.Commercial}</div>
          <div style={{ width: '6%', textAlign: 'center' }}>{levelValues.Leisure}</div>
          <div style={{ width: '7%', textAlign: 'center' }}>{levelValues.Extractor}</div>
          <div style={{ width: '8%', textAlign: 'center' }}>{levelValues.Industrial}</div>
          <div style={{ width: '6%', textAlign: 'center' }}>{levelValues.Office}</div>

          {/** Only render these if it's NOT the "Companies" row */}
          {levelName !== 'Companies' && (
            <>
              <div style={{ width: '10%', textAlign: 'center' }}>{levelValues.Employee}</div>
              <div style={{ width: '9%', textAlign: 'center' }}>{levelValues.Commuter}</div>
              <div style={{ width: '7%', textAlign: 'center' }}>{levelValues.Open}</div>
            </>
          )}
        </>
      )}
    </div>
  );
};

// Main Workplaces Component Props


// Main Workplaces Component
const Workplaces: FC<DraggablePanelProps> = ({ onClose, initialPosition}) => {
 
  const ilWorkplaces = useValue(IlWorkplaces$) || Array(7).fill(defaultWorkplacesInfo);
  initialPosition = { x: 0.038, y: 0.15 };
  

  

  

  

  // Workforce levels configuration
  const workforceLevels = [
    { levelColor: '#808080', levelName: 'Uneducated', levelValues: ilWorkplaces[0] },
    { levelColor: '#B09868', levelName: 'Poorly Educated', levelValues: ilWorkplaces[1] },
    { levelColor: '#368A2E', levelName: 'Educated', levelValues: ilWorkplaces[2] },
    { levelColor: '#B981C0', levelName: 'Well Educated', levelValues: ilWorkplaces[3] },
    { levelColor: '#5796D1', levelName: 'Highly Educated', levelValues: ilWorkplaces[4] },
    { levelColor: undefined, levelName: 'TOTAL', levelValues: ilWorkplaces[5] },
    { levelColor: undefined, levelName: 'Companies', levelValues: ilWorkplaces[6], showAll: true },
  ];

  return (
    <Panel 
        draggable={true}
        onClose={onClose}
        initialPosition={initialPosition}
        className={styles.panel}
        header={
          <div className={styles.header}>
            <span className={styles.headerText}>Workplaces</span>
          </div>
        }
    >
      {ilWorkplaces.length === 0 ? (
        <p style={{ color: 'white' }}>Loading...</p>
      ) : (
        <div>
          {/* Render header for the table */}
          <div
            className="row headers"
            style={{
              display: 'flex',
              padding: '1rem',
              fontWeight: 'bold',
              color: 'white',
            }}
          >
            <div style={{ width: '20%' }}>Education</div>
            <div style={{ width: '8%', textAlign: 'center' }}>Total</div>
            <div style={{ width: '7%', textAlign: 'center' }}>%</div>
            <div style={{ width: '6%', textAlign: 'center' }}>City</div>
            <div style={{ width: '6%', textAlign: 'center' }}>Sales</div>
            <div style={{ width: '6%', textAlign: 'center' }}>Leisure</div>
            <div style={{ width: '7%', textAlign: 'center' }}>Extractor</div>
            <div style={{ width: '8%', textAlign: 'center' }}>Industrial</div>
            <div style={{ width: '6%', textAlign: 'center' }}>Office</div>
            <div style={{ width: '10%', textAlign: 'center' }}>Employee</div>
            <div style={{ width: '9%', textAlign: 'center' }}>Commuter</div>
            <div style={{ width: '7%', textAlign: 'center' }}>Open</div>
          </div>

          {/* Render workforce levels */}
          {workforceLevels.map(({ levelColor, levelName, levelValues, showAll = true }, index) => (
            <WorkforceLevel
              key={index}
              levelColor={levelColor}
              levelName={levelName}
              levelValues={levelValues}
              total={Number(ilWorkplaces[5]?.Total) || 0} // Use total from the 'TOTAL' row
              showAll={showAll}
            />
          ))}
        </div>
      )}
    </Panel>
  );
};

export default Workplaces;
