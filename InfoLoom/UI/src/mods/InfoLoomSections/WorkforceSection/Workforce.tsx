import React, { FC, useCallback, useEffect, useState } from 'react';
import $Panel from 'mods/panel';
import { useValue, bindValue } from 'cs2/api';
import mod from 'mod.json';

// Define the structure of workforce information
interface WorkforceInfo {
  Total: number;
  Worker: number;
  Unemployed: number;
  Under: number;
  Outside: number;
  Homeless: number;
}

// Default values for workforce info
const defaultWorkforceInfo: WorkforceInfo = {
  Total: 0,
  Worker: 0,
  Unemployed: 0,
  Under: 0,
  Outside: 0,
  Homeless: 0,
};

// Bind workforce data using CS2 API
const IlWorkforce$ = bindValue<WorkforceInfo[]>(mod.id, 'ilWorkforce');

// Props for each level of workforce representation
interface WorkforceLevelProps {
  levelColor?: string;
  levelName: string;
  levelValues: WorkforceInfo;
  total: number;
}

// WorkforceLevel component
const WorkforceLevel: FC<WorkforceLevelProps> = ({ levelColor, levelName, levelValues, total }) => {
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
interface WorkforceProps {
  onClose: () => void;
}

// Workforce main component
const Workforce: FC<WorkforceProps> = ({ onClose }) => {
  const [isPanelVisible, setIsPanelVisible] = useState(true);
  const ilWorkforce = useValue(IlWorkforce$) || Array(6).fill(defaultWorkforceInfo);

  const defaultPosition = { top: window.innerHeight * 0.05, left: window.innerWidth * 0.005 };
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

  // Workforce configuration
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
    <$Panel
      id="infoloom.workforce"
      title="Workforce Structure"
      onClose={handleClose}
      initialSize={{ width: window.innerWidth * 0.45, height: window.innerHeight * 0.33 }}
      initialPosition={panelPosition}
     
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
    </$Panel>
  );
};

export default Workforce;