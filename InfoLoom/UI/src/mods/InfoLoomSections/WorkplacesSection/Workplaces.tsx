import React, { FC } from 'react';
import { useValue } from 'cs2/api';
import { DraggablePanelProps, Panel, Scrollable } from "cs2/ui";
import styles from "./Workplaces.module.scss";
import { workplacesInfo } from 'mods/domain/WorkplacesInfo';
import { WorkplacesData } from "../../bindings";

// Define valid sector keys to fix TypeScript indexing error
type SectorKey = 'Service' | 'Commercial' | 'Leisure' | 'Extractor' | 'Industrial' | 'Office';

// Define sector interface with proper typing
interface Sector {
  name: string;
  key: SectorKey;
  color: string;
  total?: number;
}

// Main Workplaces Component
const Workplaces: FC<DraggablePanelProps> = ({ onClose, initialPosition }) => {
  const ilWorkplaces = useValue(WorkplacesData);
  initialPosition = { x: 0.038, y: 0.15 };
  
  // Early return if data isn't loaded yet
  if (ilWorkplaces.length === 0) {
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
        <p className={styles.loadingText}>Loading...</p>
      </Panel>
    );
  }

  // Central color definitions for consistent theming
  const colors = {
    // Education level colors
    education: {
      uneducated: '#808080',
      poorlyEducated: '#B09868',
      educated: '#368A2E',
      wellEducated: '#B981C0',
      highlyEducated: '#5796D1',
    },
    // Sector colors
    sector: {
      service: '#4287f5',
      commercial: '#f5d142',
      leisure: '#f542a7',
      extractor: '#8c42f5',
      industrial: '#f55142',
      office: '#42f5b3',
    },
    // Status colors
    status: {
      local: '#4CAF50', // Green
      commuter: '#9E9E9E', // Gray
      vacant: '#F44336', // Red
      overqualified: '#B981C0', // Purple
    }
  };

  // Configure our data for display
  const workforceLevels = [
    { levelColor: colors.education.uneducated, levelName: 'Uneducated', levelValues: ilWorkplaces[0] },
    { levelColor: colors.education.poorlyEducated, levelName: 'Poorly Educated', levelValues: ilWorkplaces[1] },
    { levelColor: colors.education.educated, levelName: 'Educated', levelValues: ilWorkplaces[2] },
    { levelColor: colors.education.wellEducated, levelName: 'Well Educated', levelValues: ilWorkplaces[3] },
    { levelColor: colors.education.highlyEducated, levelName: 'Highly Educated', levelValues: ilWorkplaces[4] },
  ];
  
  // Total values
  const totalWorkplaces = Number(ilWorkplaces[5]?.Total) || 0;
  const totalEmployees = workforceLevels.reduce((sum, level) => sum + (level.levelValues.Employee || 0), 0);
  const totalCommuters = workforceLevels.reduce((sum, level) => sum + (level.levelValues.Commuter || 0), 0);
  const totalOpen = workforceLevels.reduce((sum, level) => sum + (level.levelValues.Open || 0), 0);
  
  // Calculate workplace distribution by sector
  const sectors: Sector[] = [
    { name: 'City Services', key: 'Service', color: colors.sector.service },
    { name: 'Sales', key: 'Commercial', color: colors.sector.commercial },
    { name: 'Leisure', key: 'Leisure', color: colors.sector.leisure },
    { name: 'Extractor', key: 'Extractor', color: colors.sector.extractor },
    { name: 'Industrial', key: 'Industrial', color: colors.sector.industrial },
    { name: 'Office', key: 'Office', color: colors.sector.office },
  ];
  
  const sectorTotals = sectors.map(sector => {
    // Safe way to access properties with type checking
    const getSectorValue = (level: { levelValues: workplacesInfo }) => {
      // Type assertion to tell TypeScript this is a valid key
      const key = sector.key;
      return level.levelValues[key] || 0;
    };
    
    return {
      ...sector,
      total: workforceLevels.reduce((sum, level) => sum + getSectorValue(level), 0),
    };
  });

  // Calculate distribution of workforce by sector and education level
  const sectorEducationDistribution = sectors.map(sector => {
    const educationBreakdown = workforceLevels.map(level => {
      const key = sector.key;
      return {
        educationLevel: level.levelName,
        count: level.levelValues[key] || 0,
        color: level.levelColor
      };
    });

    return {
      ...sector,
      educationBreakdown
    };
  });

  // Calculate vacancy rates by education level
  const vacancyRates = workforceLevels.map(level => {
    const total = level.levelValues.Total || 0;
    const open = level.levelValues.Open || 0;
    const rate = total > 0 ? (open / total) * 100 : 0;

    return {
      level: level.levelName,
      color: level.levelColor,
      total,
      open,
      rate
    };
  });

  // Format number for display - abbreviate large numbers
  const formatNumber = (num: number) => {
    if (num >= 1000000) {
      return (num / 1000000).toFixed(1) + 'M';
    } else if (num >= 1000) {
      return (num / 1000).toFixed(1) + 'K';
    }
    return num.toString();
  };
  
  // Calculate percentage
  const getPercentage = (value: number, total: number) => {
    return total > 0 ? `${((value / total) * 100).toFixed(1)}%` : '0%';
  };
  
  return (
    <Panel 
      draggable={true}
      onClose={onClose}
      initialPosition={{ x: 0.71, y: 0.70 }}
      className={styles.panel}
      header={
        <div className={styles.header}>
          <span className={styles.headerText}>Workplaces</span>
        </div>
      }
    >
      <Scrollable className={styles.scrollable}>
        <div className={styles.container}>
          {/* Detailed Data Table */}
          <div className={styles.sectionTitle}>Detailed Workplace Data</div>
          <div>
            {/* Table Headers */}
            <div className={styles.headerRow}>
              <div className={styles.educationCol}>Education Level</div>
              <div className={styles.dataCol}>Total</div>
              <div className={styles.dataCol}>%</div>
              <div className={styles.dataCol}>Local</div>
              <div className={styles.dataCol}>Commuter</div>
              <div className={styles.dataCol}>Open</div>
            </div>

            {/* Workplace Levels Rows */}
            {[...workforceLevels,
              { levelColor: undefined, levelName: 'TOTAL', levelValues: ilWorkplaces[5] }
            ].map((level, index) => {
              const rowClassName = level.levelName === 'TOTAL' ?
                `${styles.workforceRow} ${styles.totalRow}` :
                styles.workforceRow;

              const percent = totalWorkplaces > 0 && typeof level.levelValues.Total === 'number'
                ? `${((100 * level.levelValues.Total) / totalWorkplaces).toFixed(1)}%`
                : '';

              return (
                <div key={index} className={rowClassName}>
                  <div className={styles.educationCol}>
                    {level.levelColor && (
                      <div
                        className={styles.colorBox}
                        style={{ backgroundColor: level.levelColor }}
                      />
                    )}
                    <div>{level.levelName}</div>
                  </div>
                  <div className={styles.dataCol}>{formatNumber(level.levelValues.Total || 0)}</div>
                  <div className={styles.dataCol}>{percent}</div>
                  <div className={styles.dataCol}>{formatNumber(level.levelValues.Employee || 0)}</div>
                  <div className={styles.dataCol}>{formatNumber(level.levelValues.Commuter || 0)}</div>
                  <div className={styles.dataCol}>{formatNumber(level.levelValues.Open || 0)}</div>
                </div>
              );
            })}
          </div>
          
          {/* Vacancy Analysis Table */}
          <div className={styles.sectionTitle}>Vacancy Analysis</div>
          <div>
            {/* Table Headers */}
            <div className={styles.headerRow}>
              <div className={styles.educationCol}>Education Level</div>
              <div className={styles.dataCol}>Open Positions</div>
              <div className={styles.dataCol}>Total Positions</div>
              <div className={styles.dataCol}>Vacancy Rate</div>
            </div>

            {/* Vacancy Rows */}
            {vacancyRates.map((item, index) => (
              <div key={index} className={styles.workforceRow}>
                <div className={styles.educationCol}>
                  <div
                    className={styles.colorBox}
                    style={{ backgroundColor: item.color }}
                  />
                  <div>{item.level}</div>
                </div>
                <div className={styles.dataCol}>{formatNumber(item.open)}</div>
                <div className={styles.dataCol}>{formatNumber(item.total)}</div>
                <div className={styles.dataCol}>{`${item.rate.toFixed(1)}%`}</div>
              </div>
            ))}
            {/* Total row for vacancies */}
            <div className={`${styles.workforceRow} ${styles.totalRow}`}>
              <div className={styles.educationCol}>TOTAL</div>
              <div className={styles.dataCol}>{formatNumber(totalOpen)}</div>
              <div className={styles.dataCol}>{formatNumber(totalWorkplaces)}</div>
              <div className={styles.dataCol}>
                {totalWorkplaces > 0 ? `${((totalOpen / totalWorkplaces) * 100).toFixed(1)}%` : '0%'}
              </div>
            </div>
          </div>

          {/* Commuter Analysis Table */}
          <div className={styles.sectionTitle}>Commuter Analysis</div>
          <div>
            {/* Table Headers */}
            <div className={styles.headerRow}>
              <div className={styles.educationCol}>Education Level</div>
              <div className={styles.dataCol}>Local Workers</div>
              <div className={styles.dataCol}>Commuters</div>
              <div className={styles.dataCol}>Commuter %</div>
              <div className={styles.dataCol}>Total Workers</div>
            </div>

            {/* Commuter Analysis Rows */}
            {workforceLevels.map((level, index) => {
              const localWorkers = level.levelValues.Employee || 0;
              const commuters = level.levelValues.Commuter || 0;
              const totalWorkers = localWorkers + commuters;
              const commuterPercent = totalWorkers > 0 ? (commuters / totalWorkers) * 100 : 0;

              return (
                <div key={index} className={styles.workforceRow}>
                  <div className={styles.educationCol}>
                    <div
                      className={styles.colorBox}
                      style={{ backgroundColor: level.levelColor }}
                    />
                    <div>{level.levelName}</div>
                  </div>
                  <div className={styles.dataCol}>{formatNumber(localWorkers)}</div>
                  <div className={styles.dataCol}>{formatNumber(commuters)}</div>
                  <div className={styles.dataCol}>{`${commuterPercent.toFixed(1)}%`}</div>
                  <div className={styles.dataCol}>{formatNumber(totalWorkers)}</div>
                </div>
              );
            })}
            {/* Total row for commuter analysis */}
            <div className={`${styles.workforceRow} ${styles.totalRow}`}>
              <div className={styles.educationCol}>TOTAL</div>
              <div className={styles.dataCol}>{formatNumber(totalEmployees)}</div>
              <div className={styles.dataCol}>{formatNumber(totalCommuters)}</div>
              <div className={styles.dataCol}>
                {totalEmployees + totalCommuters > 0 ?
                  `${((totalCommuters / (totalEmployees + totalCommuters)) * 100).toFixed(1)}%` : '0%'}
              </div>
              <div className={styles.dataCol}>{formatNumber(totalEmployees + totalCommuters)}</div>
            </div>
          </div>
        </div>
      </Scrollable>
    </Panel>
  );
};

export default Workplaces;

