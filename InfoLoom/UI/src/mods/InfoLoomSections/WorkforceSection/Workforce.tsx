import React, { useState, useEffect, useCallback } from 'react';
import useDataUpdate from 'mods/use-data-update';
import $Panel from 'mods/panel';
import engine from 'cohtml/cohtml';

// Define the shape of the level values
interface LevelValues {
  total: number;
  worker: number;
  unemployed: number;
  under: number;
  outside: number;
  homeless: number;
}

// Props for WorkforceLevel component
interface WorkforceLevelProps {
  levelColor?: string;
  levelName: string;
  levelValues: LevelValues;
  total: number;
}

// WorkforceLevel component
const WorkforceLevel: React.FC<WorkforceLevelProps> = ({
  levelColor = '',
  levelName,
  levelValues,
  total,
}) => {
  const percent = total > 0 ? `${((100 * levelValues.total) / total).toFixed(1)}%` : '';
  const unemployment =
    levelValues.total > 0 ? `${((100 * levelValues.unemployed) / levelValues.total).toFixed(1)}%` : '';

  return (
    <div className="labels_L7Q row_S2v" style={{ width: '99%', padding: '1rem 0' }}>
      <div style={{ width: '1%' }}></div>
      <div style={{ display: 'flex', alignItems: 'center', width: '22%' }}>
        {levelColor && (
          <div
            className="symbol_aAH"
            style={{ backgroundColor: levelColor, width: '1.2em', height: '1.2em', marginRight: '0.5em' }}
          ></div>
        )}
        <div>{levelName}</div>
      </div>
      <div className="row_S2v" style={{ width: '11%', justifyContent: 'center' }}>
        {levelValues.total}
      </div>
      <div className="row_S2v" style={{ width: '8%', justifyContent: 'center' }}>
        {percent}
      </div>
      <div className="row_S2v" style={{ width: '11%', justifyContent: 'center' }}>
        {levelValues.worker}
      </div>
      <div className="row_S2v" style={{ width: '12%', justifyContent: 'center' }}>
        {levelValues.unemployed}
      </div>
      <div className="row_S2v small_ExK" style={{ width: '8%', justifyContent: 'center' }}>
        {unemployment}
      </div>
      <div className="row_S2v small_ExK" style={{ width: '9%', justifyContent: 'center' }}>
        {levelValues.under}
      </div>
      <div className="row_S2v small_ExK" style={{ width: '9%', justifyContent: 'center' }}>
        {levelValues.outside}
      </div>
      <div className="row_S2v small_ExK" style={{ width: '9%', justifyContent: 'center' }}>
        {levelValues.homeless}
      </div>
    </div>
  );
};

// Props for $Workforce component
interface WorkforceProps {
  onClose: () => void;
}

// $Workforce component
const Workforce: React.FC<WorkforceProps> = ({ onClose }) => {
  const [workforce, setWorkforce] = useState<LevelValues[]>([]);

  // New state to control panel visibility
  

  // Fetch workforce data
  useDataUpdate('populationInfo.ilWorkforce', setWorkforce);

  const [isPanelVisible, setIsPanelVisible] = useState(true);

  // Handler for closing the panel
  const handleClose = useCallback(() => {
    onClose();
  }, [onClose]);

  if (!isPanelVisible) {
    return null;
  }
 

  // Headers for the workforce table
  const headers: { [key: string]: string } = {
    total: 'Total',
    worker: 'Workers',
    unemployed: 'Unemployed',
    homeless: 'Homeless',
    employable: 'Employable',
    under: 'Under',
    outside: 'Outside',
  };

  

  return (
    <$Panel
      title="Workforce Structure"
      onClose={handleClose}
      initialSize={{ width: window.innerWidth * 0.37, height: window.innerHeight * 0.222 }}
      initialPosition={{ top: window.innerHeight * 0.05, left: window.innerWidth * 0.005 }}
    >
      {workforce.length === 0 ? (
        <p>Waiting...</p>
      ) : (
        <div>
          {/* Headers */}
          <WorkforceLevel levelName="Education" levelValues={headers as unknown as LevelValues} total={0} />

          {/* Workforce Levels */}
          <WorkforceLevel
            levelColor="#808080"
            levelName="Uneducated"
            levelValues={workforce[0]}
            total={workforce[5].total}
          />
          <WorkforceLevel
            levelColor="#B09868"
            levelName="Poorly Educated"
            levelValues={workforce[1]}
            total={workforce[5].total}
          />
          <WorkforceLevel
            levelColor="#368A2E"
            levelName="Educated"
            levelValues={workforce[2]}
            total={workforce[5].total}
          />
          <WorkforceLevel
            levelColor="#B981C0"
            levelName="Well Educated"
            levelValues={workforce[3]}
            total={workforce[5].total}
          />
          <WorkforceLevel
            levelColor="#5796D1"
            levelName="Highly Educated"
            levelValues={workforce[4]}
            total={workforce[5].total}
          />

          {/* Total */}
          <WorkforceLevel levelName="TOTAL" levelValues={workforce[5]} total={0} />
        </div>
      )}
    </$Panel>
  );
};

export default Workforce;

// Registering the panel with HookUI so it shows up in the menu
/*
window._$hookui.registerPanel({
  id: 'infoloom.workforce',
  name: 'InfoLoom: Workforce',
  icon: 'Media/Game/Icons/Workers.svg',
  component: $Workforce,
});
*/
