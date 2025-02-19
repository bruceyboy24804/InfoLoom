import React, { FC, useCallback, useEffect, useState } from 'react';
import { useValue, bindValue } from 'cs2/api';
import {DraggablePanelProps, PanelProps, Panel} from "cs2/ui";
import styles from "./Workforce.module.scss";
import {workforceInfo} from "../../domain/workforceInfo";
import {WorkforceData} from "../../bindings";




const WorkforceLevel: FC<workforceLevelProps> = ({ levelColor, levelName, levelValues, total }) => {
  const percent =
    total > 0 && typeof levelValues.Total === 'number'
      ? `${((100 * levelValues.Total) / total).toFixed(1)}%`
      : '';
  const unemployment =
    levelValues.Total > 0
      ? `${((100 * levelValues.Unemployed) / levelValues.Total).toFixed(1)}%`
      : '';

  return (
    <div
      className="row"
      style={{
        display: 'flex',
        alignItems: 'center',
        padding: '1rem',
        width: '100%',
        color: 'white', // For visibility in darker themes
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
      <div style={{ width: '10%', textAlign: 'center' }}>{levelValues.Total}</div>
      <div style={{ width: '10%', textAlign: 'center' }}>{percent}</div>
      <div style={{ width: '10%', textAlign: 'center' }}>{levelValues.Worker}</div>
      <div style={{ width: '10%', textAlign: 'center' }}>{levelValues.Unemployed}</div>
      <div style={{ width: '10%', textAlign: 'center' }}>{unemployment}</div>
      <div style={{ width: '10%', textAlign: 'center' }}>{levelValues.Under}</div>
      <div style={{ width: '10%', textAlign: 'center' }}>{levelValues.Outside}</div>
      <div style={{ width: '10%', textAlign: 'center' }}>{levelValues.Homeless}</div>
    </div>
  );
};

// Props for the main Workforce component
export interface workforceLevelProps {
  levelColor?: string;
  levelName: string;
  levelValues: workforceInfo;
  total: number;
}

// Workforce main component
const Workforce: FC<DraggablePanelProps> = ({ onClose, initialPosition }) => {
  const ilWorkforce = useValue(WorkforceData);
  initialPosition = { x: 0.038, y: 0.15 };
  const workforceLevels = [
    { levelColor: '#808080', levelName: 'Uneducated', levelValues: ilWorkforce[0] },
    { levelColor: '#B09868', levelName: 'Poorly Educated', levelValues: ilWorkforce[1] },
    { levelColor: '#368A2E', levelName: 'Educated', levelValues: ilWorkforce[2] },
    { levelColor: '#B981C0', levelName: 'Well Educated', levelValues: ilWorkforce[3] },
    { levelColor: '#5796D1', levelName: 'Highly Educated', levelValues: ilWorkforce[4] },
    { levelColor: undefined, levelName: 'TOTAL', levelValues: ilWorkforce[5] },
  ];

  const totalWorkers = ilWorkforce[5]?.Total || 0; // The total to calculate percentages

  return (
    <Panel 
        draggable={true} 
        onClose={onClose}
        initialPosition={initialPosition}
        className={styles.panel}
        header={
        <div className={styles.header}>
          <span className={styles.headerText}>Workforce</span>
        </div>
      }
    >
      {ilWorkforce.length === 0 ? (
        <p style={{ color: 'white' }}>Loading...</p>
      ) : (
        <div>
          {/* Table Headers */}
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
            <div style={{ width: '10%', textAlign: 'center' }}>Total</div>
            <div style={{ width: '10%', textAlign: 'center' }}>%</div>
            <div style={{ width: '10%', textAlign: 'center' }}>Worker</div>
            <div style={{ width: '10%', textAlign: 'center' }}>Unemployed</div>
            <div style={{ width: '10%', textAlign: 'center' }}>%</div>
            <div style={{ width: '10%', textAlign: 'center' }}>Under</div>
            <div style={{ width: '10%', textAlign: 'center' }}>Outside</div>
            <div style={{ width: '10%', textAlign: 'center' }}>Homeless</div>
          </div>

          {/* Workforce Levels */}
          {workforceLevels.map(({ levelColor, levelName, levelValues }, index) => (
            <WorkforceLevel
              key={index}
              levelColor={levelColor}
              levelName={levelName}
              levelValues={levelValues}
              total={totalWorkers}
            />
          ))}
        </div>
      )}
    </Panel>
  );
};

export default Workforce;