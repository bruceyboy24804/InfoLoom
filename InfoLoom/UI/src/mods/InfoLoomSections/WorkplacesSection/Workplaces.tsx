// Workplaces.tsx
import React, { FC, useCallback, useEffect, useState } from 'react';
import useDataUpdate from 'mods/use-data-update';
import $Panel from 'mods/panel';

// Define interfaces for component props
interface LevelValues {
  total: number | string;
  service: number | string;
  commercial: number | string;
  leisure: number | string;
  extractor: number | string;
  industry: number | string;
  office: number | string;
  employee?: number | string;
  commuter?: number | string;
  open?: number | string;
  [key: string]: any;
}
interface WorkforceLevelProps {
  levelColor?: string;
  levelName: string;
  levelValues: LevelValues;
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
    total > 0 && typeof levelValues.total === 'number'
      ? `${((100 * levelValues.total) / total).toFixed(1)}%`
      : '';

  return (
    <div
      className="labels_L7Q row_S2v"
      style={{ width: '99%', paddingTop: '1rem', paddingBottom: '1rem' }}
    >
      <div style={{ width: '1%' }}></div>
      <div style={{ display: 'flex', alignItems: 'center', width: '20%' }}>
        {levelColor && (
          <div
            className="symbol_aAH"
            style={{ backgroundColor: levelColor, width: '1.2em' }}
            aria-hidden="true"
          ></div>
        )}
        <div>{levelName}</div>
      </div>
      <div className="row_S2v" style={{ width: '8%', justifyContent: 'center' }}>
        {levelValues['total']}
      </div>
      <div className="row_S2v" style={{ width: '7%', justifyContent: 'center' }}>
        {percent}
      </div>
      <div className="row_S2v small_ExK" style={{ width: '6%', justifyContent: 'center' }}>
        {levelValues['service']}
      </div>
      <div className="row_S2v small_ExK" style={{ width: '6%', justifyContent: 'center' }}>
        {levelValues['commercial']}
      </div>
      <div className="row_S2v small_ExK" style={{ width: '6%', justifyContent: 'center' }}>
        {levelValues['leisure']}
      </div>
      <div className="row_S2v small_ExK" style={{ width: '7%', justifyContent: 'center' }}>
        {levelValues['extractor']}
      </div>
      <div className="row_S2v small_ExK" style={{ width: '8%', justifyContent: 'center' }}>
        {levelValues['industry']}
      </div>
      <div className="row_S2v small_ExK" style={{ width: '6%', justifyContent: 'center' }}>
        {levelValues['office']}
      </div>
      {/* Always render the columns, but conditionally display content */}
      <div className="row_S2v" style={{ width: '10%', justifyContent: 'center' }}>
        {showAll ? levelValues['employee'] : ''}
      </div>
      <div className="row_S2v small_ExK" style={{ width: '9%', justifyContent: 'center' }}>
        {showAll ? levelValues['commuter'] : ''}
      </div>
      <div className="row_S2v" style={{ width: '7%', justifyContent: 'center' }}>
        {showAll ? levelValues['open'] : ''}
      </div>
    </div>
  );
};

// Main Workplaces Component
interface WorkplacesProps {
  onClose: () => void;
}

const Workplaces: FC<WorkplacesProps> = ({ onClose }) => {
  // State for controlling the visibility of the panel
  const [isPanelVisible, setIsPanelVisible] = useState(true);

  // Data fetching and other logic
  const [workplaces, setWorkplaces] = useState<LevelValues[]>([]);
  useDataUpdate('workplaces.ilWorkplaces', setWorkplaces);

  const defaultPosition = { top: window.innerHeight * 0.05, left: window.innerWidth * 0.005 };
  const [panelPosition, setPanelPosition] = useState(defaultPosition);
  const handleSavePosition = useCallback((position: { top: number; left: number }) => {
    setPanelPosition(position);
  }, []);
  const [lastClosedPosition, setLastClosedPosition] = useState(defaultPosition);
  const headers: LevelValues = {
    total: 'Total',
    service: 'City',
    commercial: 'Sales',
    leisure: 'Leisure',
    extractor: 'Extractor',
    industry: 'Industry',
    office: 'Office',
    employee: 'Employees',
    commuter: 'Commute',
    open: 'Open',
  };

  // Handler for closing the panel
  const handleClose = useCallback(() => {
    setLastClosedPosition(panelPosition); // Save the current position before closing
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
      id="infoloom.workplaces"
      title="Workplaces Distribution"
      onClose={handleClose}
      initialSize={{ width: window.innerWidth * 0.45, height: window.innerHeight * 0.255 }}
      initialPosition={panelPosition}
      onSavePosition={handleSavePosition}
    >
      {workplaces.length === 0 ? (
        <p>Waiting...</p>
      ) : (
        <div>
          {/* Your existing content rendering */}
          {/* Adjusted heights as needed */}
          <div style={{ height: '10rem' }}></div>
          <WorkforceLevel levelName="Education" levelValues={headers} total={0} />
          <div style={{ height: '5rem' }}></div>
          {/* Render WorkforceLevel components for each level */}
          <WorkforceLevel
            levelColor="#808080"
            levelName="Uneducated"
            levelValues={workplaces[0]}
            total={Number(workplaces[5]?.total)}
          />
          <WorkforceLevel
            levelColor="#B09868"
            levelName="Poorly Educated"
            levelValues={workplaces[1]}
            total={Number(workplaces[5]?.total)}
          />
          <WorkforceLevel
            levelColor="#368A2E"
            levelName="Educated"
            levelValues={workplaces[2]}
            total={Number(workplaces[5]?.total)}
          />
          <WorkforceLevel
            levelColor="#B981C0"
            levelName="Well Educated"
            levelValues={workplaces[3]}
            total={Number(workplaces[5]?.total)}
          />
          <WorkforceLevel
            levelColor="#5796D1"
            levelName="Highly Educated"
            levelValues={workplaces[4]}
            total={Number(workplaces[5]?.total)}
          />
          <div style={{ height: '5rem' }}></div>
          <WorkforceLevel levelName="TOTAL" levelValues={workplaces[5]} total={0} />
          <WorkforceLevel
            levelName="Companies"
            levelValues={workplaces[6]}
            total={0}
            showAll={false}
          />
        </div>
      )}
    </$Panel>
  );
};

export default Workplaces;

// Registering the panel with HookUI (if needed)
// window._$hookui.registerPanel({
//     id: 'infoloom.workplaces',
//     name: 'InfoLoom: Workplaces',
//     icon: 'Media/Game/Icons/Workers.svg',
//     component: $Workplaces,
// });
