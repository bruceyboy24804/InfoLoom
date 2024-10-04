import React, { useState, useEffect, useMemo } from 'react';
import useDataUpdate from 'mods/use-data-update';
import $Panel from 'mods/panel';
import engine from 'cohtml/cohtml';
import styles from './Workplaces.module.css';

// Define interfaces for component props
interface AlignedParagraphProps {
  left: string;
  right: number;
}

interface WorkforceValues {
  total: number;
  service: number;
  commercial: number;
  leisure: number;
  extractor: number;
  industry: number;
  office: number;
  employee: number;
  commuter: number;
  open: number;
}

interface WorkforceLevelProps {
  levelColor?: string;
  levelName: string;
  levelValues: WorkforceValues;
  total?: number;
  showAll?: boolean;
}

interface WorkplacesData {
  [index: number]: WorkforceValues;
}

// AlignedParagraph Component
const AlignedParagraph: React.FC<AlignedParagraphProps> = React.memo(({ left, right }) => {
  return (
    <div style={{ display: 'flex', justifyContent: 'space-between', color: 'white', fontSize: '14px', marginBottom: '0.25em' }}>
      <span>{left}</span>
      <span>{right}</span>
    </div>
  );
});

// WorkforceLevel Component
const WorkforceLevel: React.FC<WorkforceLevelProps> = React.memo(({ levelColor = '#FFFFFF', levelName, levelValues, total = 0, showAll }) => {
  const percent = total > 0 ? `${((100 * levelValues.total) / total).toFixed(1)}%` : '0%';
  
  return (
    <div className={styles.workforceItem}>
      {levelColor && (
        <div className={styles.symbol} style={{ backgroundColor: levelColor }} aria-hidden="true"></div>
      )}
      <span>{levelName}</span>
      <span style={{ marginLeft: 'auto' }}>{showAll ?? levelValues.employee}</span>
    </div>
  );
});

// Main Workplaces Component
const $Workplaces: React.FC = () => {
  const [workplaces, setWorkplaces] = useState<WorkplacesData>({});

  // Fetch workplaces data using useDataUpdate hook with type safety
  useDataUpdate('workplaces.ilWorkplaces', (data: WorkplacesData | undefined) => setWorkplaces(data || {}));

  
  const workforceLevels = useMemo(() => {
    if (Object.keys(workplaces).length === 0) return [];

    return [
      { levelColor: '#808080', levelName: 'Uneducated', levelValues: workplaces[0] },
      { levelColor: '#B09868', levelName: 'Poorly Educated', levelValues: workplaces[1] },
      { levelColor: '#368A2E', levelName: 'Educated', levelValues: workplaces[2] },
      { levelColor: '#B981C0', levelName: 'Well Educated', levelValues: workplaces[3] },
      { levelColor: '#5796D1', levelName: 'Highly Educated', levelValues: workplaces[4] },
      { levelName: 'TOTAL', levelValues: workplaces[5], total: workplaces[5].total },
      { levelName: 'Companies', levelValues: workplaces[6], showAll: false },
    ];
  }, [workplaces]);

  return (
    <$Panel react={React} title="Workplace Distribution"  initialSize={{ width: window.innerWidth * 0.38, height: window.innerHeight * 0.22 }} initialPosition={{ top: window.innerHeight * 0.05, left: window.innerWidth * 0.005 }} className={styles.panelContainer}>
      {Object.keys(workplaces).length === 0 ? (
        <p style={{ color: 'white' }}>Waiting...</p>
      ) : (
        <div className={styles.workforceList}>
          {/* Header */}
          <div className={styles.workforceHeader}>
            <span>Level</span>
            <span>Total</span>
            <span>%</span>
            <span>Service</span>
            <span>Commercial</span>
            <span>Leisure</span>
            <span>Extractor</span>
            <span>Industry</span>
            <span>Office</span>
            <span>Employees</span>
            <span>Commuters</span>
            <span>Open</span>
          </div>

          {/* Workforce Levels */}
          {workforceLevels.map((level, index) => (
            <WorkforceLevel key={index} levelColor={level.levelColor} levelName={level.levelName} levelValues={level.levelValues} total={level.total} showAll={level.showAll} />
          ))}
        </div>
      )}
    </$Panel>
  );
};

export default $Workplaces;
