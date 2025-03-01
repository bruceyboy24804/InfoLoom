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

  // Education level colors
  const educationColors = {
    uneducated: '#808080',
    poorlyEducated: '#B09868',
    educated: '#368A2E',
    wellEducated: '#B981C0',
    highlyEducated: '#5796D1',
  };

  // Configure our data for display
  const workforceLevels = [
    { levelColor: educationColors.uneducated, levelName: 'Uneducated', levelValues: ilWorkplaces[0] },
    { levelColor: educationColors.poorlyEducated, levelName: 'Poorly Educated', levelValues: ilWorkplaces[1] },
    { levelColor: educationColors.educated, levelName: 'Educated', levelValues: ilWorkplaces[2] },
    { levelColor: educationColors.wellEducated, levelName: 'Well Educated', levelValues: ilWorkplaces[3] },
    { levelColor: educationColors.highlyEducated, levelName: 'Highly Educated', levelValues: ilWorkplaces[4] },
  ];
  
  const totalWorkplaces = Number(ilWorkplaces[5]?.Total) || 0;
  const totalEmployees = workforceLevels.reduce((sum, level) => sum + (level.levelValues.Employee || 0), 0);
  const totalCommuters = workforceLevels.reduce((sum, level) => sum + (level.levelValues.Commuter || 0), 0);
  const totalOpen = workforceLevels.reduce((sum, level) => sum + (level.levelValues.Open || 0), 0);
  
  // Calculate workplace distribution by sector
  const sectors: Sector[] = [
    { name: 'City Services', key: 'Service', color: '#4287f5' },
    { name: 'Sales', key: 'Commercial', color: '#f5d142' },
    { name: 'Leisure', key: 'Leisure', color: '#f542a7' },
    { name: 'Extractor', key: 'Extractor', color: '#8c42f5' },
    { name: 'Industrial', key: 'Industrial', color: '#f55142' },
    { name: 'Office', key: 'Office', color: '#42f5b3' },
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

  // Format number with commas - make it more compact by abbreviating large numbers
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
    return total > 0 ? `${((value / total) * 100).toFixed(0)}%` : '0%'; // Removed decimal for compactness
  };
  
  // Get numeric percentage for styling
  const getNumericPercentage = (value: number, total: number) => {
    return total > 0 ? (value / total) * 100 : 0;
  };
  
  // Calculate data for education mismatch analysis
  const calculateEducationMismatch = () => {
    // Track workers in jobs requiring different education levels
    const mismatchData = {
      overqualifiedWorkers: {
        total: 0,
        breakdown: [] as {level: string, count: number, color: string}[]
      },
      underqualifiedWorkers: {
        total: 0,
        breakdown: [] as {level: string, count: number, color: string}[]
      },
      unmatchedJobs: {
        total: 0,
        breakdown: [] as {level: string, count: number, color: string}[]
      },
      commutersByLevel: [] as {level: string, count: number, color: string}[]
    };

    // Education levels in order from lowest to highest
    const educationOrder = ['Uneducated', 'Poorly Educated', 'Educated', 'Well Educated', 'Highly Educated'];

    // Calculate estimated overqualified workers (workers in jobs below their qualification)
    let remainingWorkers = [...workforceLevels];
    
    // First, fill jobs with appropriately qualified workers
    workforceLevels.forEach((level, index) => {
      const availableWorkers = level.levelValues.Employee || 0;
      const requiredJobs = level.levelValues.Total || 0;
      const perfectMatch = Math.min(availableWorkers, requiredJobs);
      
      // If there are more workers than jobs at this level, they might be overqualified for lower jobs
      if (availableWorkers > requiredJobs) {
        const surplus = availableWorkers - requiredJobs;
        mismatchData.overqualifiedWorkers.total += surplus;
        mismatchData.overqualifiedWorkers.breakdown.push({
          level: level.levelName,
          count: surplus,
          color: level.levelColor
        });
      }
      
      // If there are more jobs than workers at this level, they might be filled by overqualified workers
      // or need workers from outside (commuters)
      if (requiredJobs > availableWorkers) {
        const deficit = requiredJobs - availableWorkers;
        mismatchData.unmatchedJobs.total += deficit;
        mismatchData.unmatchedJobs.breakdown.push({
          level: level.levelName,
          count: deficit,
          color: level.levelColor
        });
      }

      // Track commuters by education level
      const commuters = level.levelValues.Commuter || 0;
      if (commuters > 0) {
        mismatchData.commutersByLevel.push({
          level: level.levelName,
          count: commuters,
          color: level.levelColor
        });
      }
    });

    return mismatchData;
  };
  
  const mismatchData = calculateEducationMismatch();

  // Calls to action based on mismatch data
  const getCallToAction = () => {
    const actions = [];

    if (mismatchData.overqualifiedWorkers.total > 0) {
      actions.push(`You have ${formatNumber(mismatchData.overqualifiedWorkers.total)} overqualified workers. Consider adding more higher education jobs.`);
    }
    
    if (mismatchData.unmatchedJobs.total > totalCommuters) {
      actions.push(`You need ${formatNumber(mismatchData.unmatchedJobs.total - totalCommuters)} more workers to fill open positions.`);
    }
    
    if (totalCommuters > 500) {
      actions.push(`${formatNumber(totalCommuters)} commuters are filling your job needs. Build more residential areas to house them.`);
    }
    
    return actions;
  };
  
  const callToActions = getCallToAction();

  // Case study generator component for education levels
  const EducationLevelCaseStudy = ({ levelIndex, title }: { levelIndex: number, title: string }) => {
    const level = workforceLevels[levelIndex];
    const totalJobs = level.levelValues.Total || 0;
    const localWorkers = level.levelValues.Employee || 0;
    const commuterWorkers = level.levelValues.Commuter || 0;
    const openJobs = level.levelValues.Open || 0;
    
    // For overqualified calculation, we need to check if there are workers with higher education
    // working in this level's jobs
    const isOverqualifiedPresent = totalJobs > localWorkers && commuterWorkers > 0;
    const overqualifiedInJobs = isOverqualifiedPresent ? 
      Math.min(totalJobs - localWorkers, commuterWorkers) : 0;
    
    // Calculate general balance metrics
    const localFillRate = totalJobs > 0 ? (localWorkers / totalJobs) * 100 : 0;
    const commuterRate = totalJobs > 0 ? (commuterWorkers / totalJobs) * 100 : 0;
    const vacancyRate = totalJobs > 0 ? (openJobs / totalJobs) * 100 : 0;
    
    // Determine insights
    let insight = "";
    
    if (vacancyRate > 15) {
      insight = `High vacancy rate (${vacancyRate.toFixed(0)}%). You need more ${level.levelName} workers.`;
    } else if (localFillRate < 60 && commuterRate > 20) {
      insight = `You rely heavily on commuters for ${level.levelName} jobs. Consider building more appropriate housing.`;
    } else if (overqualifiedInJobs > totalJobs * 0.2) {
      insight = `Many overqualified workers are filling ${level.levelName} positions. Consider adding more higher-level jobs.`;
    } else if (localFillRate > 80 && commuterRate < 10 && vacancyRate < 10) {
      insight = `Your ${level.levelName} job market is well-balanced. Good job!`;
    } else {
      insight = `Your ${level.levelName} job market is functioning adequately.`;
    }
    
    return (
      <div className={styles.caseStudyContainer}>
        <div className={styles.caseStudyTitle}>
          {title}
        </div>
        <div className={styles.caseStudyContent}>
          <div className={styles.caseStudyStats}>
            <div className={styles.caseStudyStatItem}>
              <div className={styles.statValue}>{formatNumber(totalJobs)}</div>
              <div className={styles.statLabel}>Total Jobs</div>
            </div>
            <div className={styles.caseStudyStatItem}>
              <div className={styles.statValue}>{formatNumber(localWorkers)}</div>
              <div className={styles.statLabel}>Local Workers</div>
            </div>
            {overqualifiedInJobs > 0 && (
              <div className={styles.caseStudyStatItem}>
                <div className={styles.statValue}>{formatNumber(overqualifiedInJobs)}</div>
                <div className={styles.statLabel}>Overqualified</div>
              </div>
            )}
            <div className={styles.caseStudyStatItem}>
              <div className={styles.statValue}>{formatNumber(commuterWorkers)}</div>
              <div className={styles.statLabel}>Commuters</div>
            </div>
            <div className={styles.caseStudyStatItem}>
              <div className={styles.statValue}>{formatNumber(openJobs)}</div>
              <div className={styles.statLabel}>Open Jobs</div>
            </div>
          </div>
          <div className={styles.caseStudyVisualization}>
            <div className={styles.caseStudyBarContainer}>
              <div className={styles.caseStudyBar}>
                {localWorkers > 0 && (
                  <div 
                    className={styles.caseStudyBarSegment} 
                    style={{
                      width: `${(localWorkers / totalJobs) * 100}%`,
                      backgroundColor: '#808080'
                    }}
                    title={`Local ${level.levelName} workers: ${formatNumber(localWorkers)}`}
                  />
                )}
                {overqualifiedInJobs > 0 && (
                  <div 
                    className={styles.caseStudyBarSegment} 
                    style={{
                      width: `${(overqualifiedInJobs / totalJobs) * 100}%`,
                      backgroundColor: '#B981C0' // Distinct color for overqualified
                    }}
                    title={`Overqualified workers: ${formatNumber(overqualifiedInJobs)}`}
                  />
                )}
                {commuterWorkers - overqualifiedInJobs > 0 && (
                  <div 
                    className={styles.caseStudyBarSegment} 
                    style={{
                      width: `${((commuterWorkers - overqualifiedInJobs) / totalJobs) * 100}%`,
                      backgroundColor: '#999999'
                    }}
                    title={`Commuters: ${formatNumber(commuterWorkers - overqualifiedInJobs)}`}
                  />
                )}
                {openJobs > 0 && (
                  <div 
                    className={styles.caseStudyBarSegment} 
                    style={{
                      width: `${(openJobs / totalJobs) * 100}%`,
                      backgroundColor: '#ff5555'
                    }}
                    title={`Open positions: ${formatNumber(openJobs)}`}
                  />
                )}
              </div>
            </div>
          </div>
          <div className={styles.caseStudyInsight}>
            {insight}
          </div>
        </div>
      </div>
    );
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
          {/* Summary Section - More compact */}
          <div className={styles.summarySection}>
            <div className={styles.summaryTitle}>Workplace Summary</div>
            <div className={styles.summaryGrid}>
              <div className={styles.summaryItem}>
                <div className={styles.summaryValue}>{formatNumber(totalWorkplaces)}</div>
                <div className={styles.summaryLabel}>Workplaces</div>
              </div>
              <div className={styles.summaryItem}>
                <div className={styles.summaryValue}>{formatNumber(totalEmployees)}</div>
                <div className={styles.summaryLabel}>Local</div>
              </div>
              <div className={styles.summaryItem}>
                <div className={styles.summaryValue}>{formatNumber(totalCommuters)}</div>
                <div className={styles.summaryLabel}>Commuters</div>
              </div>
              <div className={styles.summaryItem}>
                <div className={styles.summaryValue}>{formatNumber(totalOpen)}</div>
                <div className={styles.summaryLabel}>Open</div>
              </div>
            </div>
          </div>
          
          {/* Workforce Skills Mismatch */}
          <div className={styles.sectionTitle}>Workforce Skills Mismatch</div>
          
          {/* Calls to Action */}
          {callToActions.length > 0 && (
            <div className={styles.callToActionContainer}>
              {callToActions.map((action, index) => (
                <div key={index} className={styles.callToAction}>
                  <div className={styles.actionIcon}>!</div>
                  <div>{action}</div>
                </div>
              ))}
            </div>
          )}
          
          {/* Visual representation of mismatches */}
          <div className={styles.mismatchVisualContainer}>
            {mismatchData.overqualifiedWorkers.breakdown.length > 0 && (
              <div className={styles.mismatchCard}>
                <div className={styles.mismatchTitle}>Overqualified Workers</div>
                <div className={styles.mismatchTotal}>
                  {formatNumber(mismatchData.overqualifiedWorkers.total)}
                </div>
                <div className={styles.mismatchDescription}>
                  Workers in jobs below their education level
                </div>
                <div className={styles.mismatchBreakdown}>
                  {mismatchData.overqualifiedWorkers.breakdown.map((item, idx) => (
                    <div key={idx} className={styles.mismatchBreakdownItem}>
                      <div 
                        className={styles.mismatchColor}
                        style={{ backgroundColor: item.color }}
                      />
                      <span className={styles.mismatchLabel}>{item.level}:</span>
                      <span className={styles.mismatchCount}>{formatNumber(item.count)}</span>
                    </div>
                  ))}
                </div>
              </div>
            )}
            
            {totalCommuters > 0 && (
              <div className={styles.mismatchCard}>
                <div className={styles.mismatchTitle}>Commuters</div>
                <div className={styles.mismatchTotal}>
                  {formatNumber(totalCommuters)}
                </div>
                <div className={styles.mismatchDescription}>
                  Workers coming from outside your city
                </div>
                <div className={styles.mismatchBreakdown}>
                  {mismatchData.commutersByLevel.map((item, idx) => (
                    <div key={idx} className={styles.mismatchBreakdownItem}>
                      <div 
                        className={styles.mismatchColor}
                        style={{ backgroundColor: item.color }}
                      />
                      <span className={styles.mismatchLabel}>{item.level}:</span>
                      <span className={styles.mismatchCount}>{formatNumber(item.count)}</span>
                    </div>
                  ))}
                </div>
              </div>
            )}
            
            {totalOpen > 0 && (
              <div className={styles.mismatchCard}>
                <div className={styles.mismatchTitle}>Unfilled Positions</div>
                <div className={styles.mismatchTotal}>
                  {formatNumber(totalOpen)}
                </div>
                <div className={styles.mismatchDescription}>
                  Open jobs that need workers
                </div>
                <div className={styles.mismatchBreakdown}>
                  {workforceLevels.map((level, idx) => {
                    const openJobs = level.levelValues.Open || 0;
                    if (openJobs > 0) {
                      return (
                        <div key={idx} className={styles.mismatchBreakdownItem}>
                          <div 
                            className={styles.mismatchColor}
                            style={{ backgroundColor: level.levelColor }}
                          />
                          <span className={styles.mismatchLabel}>{level.levelName}:</span>
                          <span className={styles.mismatchCount}>{formatNumber(openJobs)}</span>
                        </div>
                      );
                    }
                    return null;
                  })}
                </div>
              </div>
            )}
          </div>
          
          {/* Education Jobs Analysis - Using the reusable component */}
          <div className={styles.sectionTitle}>Education Level Jobs Analysis</div>
          
          <EducationLevelCaseStudy levelIndex={0} title="Uneducated Jobs" />
          <EducationLevelCaseStudy levelIndex={1} title="Poorly Educated Jobs" />
          <EducationLevelCaseStudy levelIndex={2} title="Educated Jobs" />
          <EducationLevelCaseStudy levelIndex={3} title="Well Educated Jobs" />
          <EducationLevelCaseStudy levelIndex={4} title="Highly Educated Jobs" />
          
          {/* Legend for case studies */}
          <div className={styles.caseLegendContainer}>
            <div className={styles.caseLegendTitle}>Legend</div>
            <div className={styles.caseLegendItems}>
              <div className={styles.legendItem}>
                <div className={styles.legendColor} style={{ backgroundColor: '#808080' }}></div>
                <span>Local Workers</span>
              </div>
              <div className={styles.legendItem}>
                <div className={styles.legendColor} style={{ backgroundColor: '#B981C0' }}></div>
                <span>Overqualified</span>
              </div>
              <div className={styles.legendItem}>
                <div className={styles.legendColor} style={{ backgroundColor: '#999999' }}></div>
                <span>Commuters</span>
              </div>
              <div className={styles.legendItem}>
                <div className={styles.legendColor} style={{ backgroundColor: '#ff5555' }}></div>
                <span>Open Jobs</span>
              </div>
            </div>
          </div>
        </div>
      </Scrollable>
    </Panel>
  );
};

export default Workplaces;