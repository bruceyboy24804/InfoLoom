import React, { FC } from 'react';
import { useValue } from 'cs2/api';
import { DraggablePanelProps, Panel, Scrollable } from "cs2/ui";
import styles from "./Workforce.module.scss";
import { workforceInfo } from "../../domain/workforceInfo";
import { WorkforceData } from "../../bindings";

// Main Workforce Component
const Workforce: FC<DraggablePanelProps> = ({ onClose, initialPosition }) => {
  const ilWorkforce = useValue(WorkforceData);
  initialPosition = { x: 0.038, y: 0.15 };
  
  // Early return if data isn't loaded yet
  if (ilWorkforce.length === 0) {
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
    { levelColor: educationColors.uneducated, levelName: 'Uneducated', levelValues: ilWorkforce[0] },
    { levelColor: educationColors.poorlyEducated, levelName: 'Poorly Educated', levelValues: ilWorkforce[1] },
    { levelColor: educationColors.educated, levelName: 'Educated', levelValues: ilWorkforce[2] },
    { levelColor: educationColors.wellEducated, levelName: 'Well Educated', levelValues: ilWorkforce[3] },
    { levelColor: educationColors.highlyEducated, levelName: 'Highly Educated', levelValues: ilWorkforce[4] },
  ];
  
  // Total workforce values
  const totalWorkforce = ilWorkforce[5]?.Total || 0;
  const totalEmployed = ilWorkforce[5]?.Worker || 0;
  const totalUnemployed = ilWorkforce[5]?.Unemployed || 0;
  const totalUnderemployed = ilWorkforce[5]?.Under || 0;
  const totalOutsideWorkforce = ilWorkforce[5]?.Outside || 0;
  const totalHomeless = ilWorkforce[5]?.Homeless || 0;
  
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
  
  // Calculate data for workforce mismatch analysis
  const calculateWorkforceMismatch = () => {
    // Track workforce issues
    const mismatchData = {
      unemployedByEducation: {
        total: totalUnemployed,
        breakdown: [] as {level: string, count: number, color: string, rate: number}[]
      },
      underemployedWorkers: {
        total: totalUnderemployed,
        breakdown: [] as {level: string, count: number, color: string}[]
      },
      homelessWorkers: {
        total: totalHomeless,
        breakdown: [] as {level: string, count: number, color: string}[]
      }
    };
    
    // Calculate breakdown of unemployment, underemployment, and homelessness by education level
    workforceLevels.forEach(level => {
      // Unemployment breakdown
      const unemployed = level.levelValues.Unemployed || 0;
      const unemploymentRate = level.levelValues.Total > 0 ? (unemployed / level.levelValues.Total) * 100 : 0;
      
      if (unemployed > 0) {
        mismatchData.unemployedByEducation.breakdown.push({
          level: level.levelName,
          count: unemployed,
          color: level.levelColor,
          rate: unemploymentRate
        });
      }
      
      // Underemployment breakdown
      const underemployed = level.levelValues.Under || 0;
      if (underemployed > 0) {
        mismatchData.underemployedWorkers.breakdown.push({
          level: level.levelName,
          count: underemployed,
          color: level.levelColor
        });
      }
      
      // Homelessness breakdown
      const homeless = level.levelValues.Homeless || 0;
      if (homeless > 0) {
        mismatchData.homelessWorkers.breakdown.push({
          level: level.levelName,
          count: homeless,
          color: level.levelColor
        });
      }
    });
    
    return mismatchData;
  };
  
  const mismatchData = calculateWorkforceMismatch();
  
  // Calls to action based on workforce data
  const getCallToAction = () => {
    const actions = [];
    
    const unemploymentRate = totalWorkforce > 0 ? (totalUnemployed / totalWorkforce) * 100 : 0;
    
    if (unemploymentRate > 10) {
      actions.push(`High unemployment rate (${unemploymentRate.toFixed(1)}%). Consider adding more workplaces.`);
    } else if (unemploymentRate > 5) {
      actions.push(`Moderate unemployment rate (${unemploymentRate.toFixed(1)}%). Your city could use more jobs.`);
    }
    
    if (totalUnderemployed > totalWorkforce * 0.05) {
      actions.push(`${formatNumber(totalUnderemployed)} workers are underemployed. Consider adding more suitable jobs for their education level.`);
    }
    
    if (totalHomeless > 100) {
      actions.push(`${formatNumber(totalHomeless)} homeless citizens need affordable housing.`);
    }
    
    if (totalOutsideWorkforce > totalWorkforce * 0.2) {
      actions.push(`${formatNumber(totalOutsideWorkforce)} citizens are working outside of the city. Consider providing more jobs.`);
    }
    
    return actions;
  };
  
  const callToActions = getCallToAction();
  
  // Education level case study component
  const EducationLevelCaseStudy = ({ levelIndex, title }: { levelIndex: number, title: string }) => {
    const level = workforceLevels[levelIndex];
    const totalPeople = level.levelValues.Total || 0;
    const working = level.levelValues.Worker || 0;
    const unemployed = level.levelValues.Unemployed || 0;
    const underemployed = level.levelValues.Under || 0;
    const outside = level.levelValues.Outside || 0;
    const homeless = level.levelValues.Homeless || 0;
    
    // Calculate rates
    const employmentRate = totalPeople > 0 ? (working / totalPeople) * 100 : 0;
    const unemploymentRate = totalPeople > 0 ? (unemployed / totalPeople) * 100 : 0;
    const underemploymentRate = totalPeople > 0 ? (underemployed / totalPeople) * 100 : 0;
    const outsideRate = totalPeople > 0 ? (outside / totalPeople) * 100 : 0;
    const homelessRate = totalPeople > 0 ? (homeless / totalPeople) * 100 : 0;
    
    // Determine insights
    let insight = "";
    
    if (unemploymentRate > 15) {
      insight = `High unemployment rate (${unemploymentRate.toFixed(0)}%) for ${level.levelName} workers. More jobs needed for this education level.`;
    } else if (underemploymentRate > 20) {
      insight = `Many ${level.levelName} workers (${underemploymentRate.toFixed(0)}%) are underemployed. Add more suitable jobs for their skills.`;
    } else if (outside > totalPeople * 0.3) {
      insight = `Large portion of ${level.levelName} citizens (${outsideRate.toFixed(0)}%) are outside the workforce. Consider policies to increase participation.`;
    } else if (employmentRate > 75 && unemploymentRate < 5) {
      insight = `Your ${level.levelName} workforce is well-balanced with good employment rates. Great job!`;
    } else {
      insight = `Your ${level.levelName} workforce situation is reasonably stable.`;
    }
    
    return (
      <div className={styles.caseStudyContainer}>
        <div className={styles.caseStudyTitle}>
          {title}
        </div>
        <div className={styles.caseStudyContent}>
          <div className={styles.caseStudyStats}>
            <div className={styles.caseStudyStatItem}>
              <div className={styles.statValue}>{formatNumber(totalPeople)}</div>
              <div className={styles.statLabel}>Total</div>
            </div>
            <div className={styles.caseStudyStatItem}>
              <div className={styles.statValue}>{formatNumber(working)}</div>
              <div className={styles.statLabel}>Working</div>
            </div>
            <div className={styles.caseStudyStatItem}>
              <div className={styles.statValue}>{formatNumber(unemployed)}</div>
              <div className={styles.statLabel}>Unemployed</div>
            </div>
            <div className={styles.caseStudyStatItem}>
              <div className={styles.statValue}>{formatNumber(underemployed)}</div>
              <div className={styles.statLabel}>Underemployed</div>
            </div>
          </div>
          <div className={styles.caseStudyVisualization}>
            <div className={styles.caseStudyBarContainer}>
              <div className={styles.caseStudyBar}>
                {working > 0 && (
                  <div 
                    className={styles.caseStudyBarSegment} 
                    style={{
                      width: `${(working / totalPeople) * 100}%`,
                      backgroundColor: '#4CAF50' // Green for employed
                    }}
                    title={`Employed ${level.levelName} workers: ${formatNumber(working)}`}
                  />
                )}
                {unemployed > 0 && (
                  <div 
                    className={styles.caseStudyBarSegment} 
                    style={{
                      width: `${(unemployed / totalPeople) * 100}%`,
                      backgroundColor: '#F44336' // Red for unemployed
                    }}
                    title={`Unemployed ${level.levelName} workers: ${formatNumber(unemployed)}`}
                  />
                )}
                {underemployed > 0 && (
                  <div 
                    className={styles.caseStudyBarSegment} 
                    style={{
                      width: `${(underemployed / totalPeople) * 100}%`,
                      backgroundColor: '#FFC107' // Yellow for underemployed
                    }}
                    title={`Underemployed ${level.levelName} workers: ${formatNumber(underemployed)}`}
                  />
                )}
                {outside > 0 && (
                  <div 
                    className={styles.caseStudyBarSegment} 
                    style={{
                      width: `${(outside / totalPeople) * 100}%`,
                      backgroundColor: '#9E9E9E' // Grey for outside workforce
                    }}
                    title={`${level.levelName} outside workforce: ${formatNumber(outside)}`}
                  />
                )}
                {homeless > 0 && (
                  <div 
                    className={styles.caseStudyBarSegment} 
                    style={{
                      width: `${(homeless / totalPeople) * 100}%`,
                      backgroundColor: '#673AB7' // Purple for homeless
                    }}
                    title={`Homeless ${level.levelName} citizens: ${formatNumber(homeless)}`}
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
      initialPosition={{ x: 0.71, y: 0.020 }}
      className={styles.panel}
      header={
        <div className={styles.header}>
          <span className={styles.headerText}>Workforce</span>
        </div>
      }
    >
      <Scrollable className={styles.scrollable}>
        <div className={styles.container}>
          {/* Summary Section */}
          <div className={styles.summarySection}>
            <div className={styles.summaryTitle}>Workforce Summary</div>
            <div className={styles.summaryGrid}>
              <div className={styles.summaryItem}>
                <div className={styles.summaryValue}>{formatNumber(totalWorkforce)}</div>
                <div className={styles.summaryLabel}>Total</div>
              </div>
              <div className={styles.summaryItem}>
                <div className={styles.summaryValue}>{formatNumber(totalEmployed)}</div>
                <div className={styles.summaryLabel}>Employed</div>
              </div>
              <div className={styles.summaryItem}>
                <div className={styles.summaryValue}>{formatNumber(totalUnemployed)}</div>
                <div className={styles.summaryLabel}>Unemployed</div>
              </div>
              <div className={styles.summaryItem}>
                <div className={styles.summaryValue}>
                  {getPercentage(totalUnemployed, totalWorkforce)}
                </div>
                <div className={styles.summaryLabel}>Unemployment</div>
              </div>
            </div>
          </div>
          
          {/* Workforce Mismatch Analysis */}
          <div className={styles.sectionTitle}>Workforce Status Analysis</div>
          
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
            {totalUnemployed > 0 && (
              <div className={styles.mismatchCard}>
                <div className={styles.mismatchTitle}>Unemployment</div>
                <div className={styles.mismatchTotal}>
                  {formatNumber(totalUnemployed)}
                </div>
                <div className={styles.mismatchDescription}>
                  Citizens looking for work
                </div>
                <div className={styles.mismatchBreakdown}>
                  {mismatchData.unemployedByEducation.breakdown.map((item, idx) => (
                    <div key={idx} className={styles.mismatchBreakdownItem}>
                      <div 
                        className={styles.mismatchColor}
                        style={{ backgroundColor: item.color }}
                      />
                      <span className={styles.mismatchLabel}>{item.level}:</span>
                      <span className={styles.mismatchCount}>
                        {formatNumber(item.count)} ({item.rate.toFixed(0)}%)
                      </span>
                    </div>
                  ))}
                </div>
              </div>
            )}
            
            {totalUnderemployed > 0 && (
              <div className={styles.mismatchCard}>
                <div className={styles.mismatchTitle}>Underemployed</div>
                <div className={styles.mismatchTotal}>
                  {formatNumber(totalUnderemployed)}
                </div>
                <div className={styles.mismatchDescription}>
                  Workers in jobs below their education
                </div>
                <div className={styles.mismatchBreakdown}>
                  {mismatchData.underemployedWorkers.breakdown.map((item, idx) => (
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
            
            {totalOutsideWorkforce > 0 && (
              <div className={styles.mismatchCard}>
                <div className={styles.mismatchTitle}>Outside Workforce</div>
                <div className={styles.mismatchTotal}>
                  {formatNumber(totalOutsideWorkforce)}
                </div>
                <div className={styles.mismatchDescription}>
                  Citizens woking outside of the city
                </div>
                <div className={styles.mismatchBreakdown}>
                  {workforceLevels.map((level, idx) => {
                    const outsideCount = level.levelValues.Outside || 0;
                    if (outsideCount > 0) {
                      return (
                        <div key={idx} className={styles.mismatchBreakdownItem}>
                          <div 
                            className={styles.mismatchColor}
                            style={{ backgroundColor: level.levelColor }}
                          />
                          <span className={styles.mismatchLabel}>{level.levelName}:</span>
                          <span className={styles.mismatchCount}>{formatNumber(outsideCount)}</span>
                        </div>
                      );
                    }
                    return null;
                  })}
                </div>
              </div>
            )}
          </div>
          
          {/* Education Level Analysis - Using the reusable component */}
          <div className={styles.sectionTitle}>Education Level Analysis</div>
          
          <EducationLevelCaseStudy levelIndex={0} title="Uneducated Citizens" />
          <EducationLevelCaseStudy levelIndex={1} title="Poorly Educated Citizens" />
          <EducationLevelCaseStudy levelIndex={2} title="Educated Citizens" />
          <EducationLevelCaseStudy levelIndex={3} title="Well Educated Citizens" />
          <EducationLevelCaseStudy levelIndex={4} title="Highly Educated Citizens" />
          
          {/* Legend for case studies */}
          <div className={styles.caseLegendContainer}>
            <div className={styles.caseLegendTitle}>Legend</div>
            <div className={styles.caseLegendItems}>
              <div className={styles.legendItem}>
                <div className={styles.legendColor} style={{ backgroundColor: '#4CAF50' }}></div>
                <span>Employed</span>
              </div>
              <div className={styles.legendItem}>
                <div className={styles.legendColor} style={{ backgroundColor: '#F44336' }}></div>
                <span>Unemployed</span>
              </div>
              <div className={styles.legendItem}>
                <div className={styles.legendColor} style={{ backgroundColor: '#FFC107' }}></div>
                <span>Underemployed</span>
              </div>
              <div className={styles.legendItem}>
                <div className={styles.legendColor} style={{ backgroundColor: '#9E9E9E' }}></div>
                <span>Outside Workforce</span>
              </div>
              <div className={styles.legendItem}>
                <div className={styles.legendColor} style={{ backgroundColor: '#673AB7' }}></div>
                <span>Homeless</span>
              </div>
            </div>
          </div>

          {/* Detailed Data Table */}
          <div className={styles.sectionTitle}>Detailed Workforce Data</div>
          <div>
            {/* Table Headers */}
            <div className={styles.headerRow}>
              <div className={styles.educationCol}>Education</div>
              <div className={styles.dataCol}>Total</div>
              <div className={styles.dataCol}>%</div>
              <div className={styles.dataCol}>Worker</div>
              <div className={styles.dataCol}>Unemployed</div>
              <div className={styles.dataCol}>%</div>
              <div className={styles.dataCol}>Under</div>
              <div className={styles.dataCol}>Outside</div>
              <div className={styles.dataCol}>Homeless</div>
            </div>

            {/* Workforce Levels Rows */}
            {[...workforceLevels, 
              { levelColor: undefined, levelName: 'TOTAL', levelValues: ilWorkforce[5] }
            ].map((level, index) => {
              const rowClassName = level.levelName === 'TOTAL' ? 
                `${styles.workforceRow} ${styles.totalRow}` : 
                styles.workforceRow;
                
              const percent = totalWorkforce > 0 && typeof level.levelValues.Total === 'number'
                ? `${((100 * level.levelValues.Total) / totalWorkforce).toFixed(1)}%`
                : '';
                
              const unemployment = level.levelValues.Total > 0
                ? `${((100 * level.levelValues.Unemployed) / level.levelValues.Total).toFixed(1)}%`
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
                  <div className={styles.dataCol}>{formatNumber(level.levelValues.Total)}</div>
                  <div className={styles.dataCol}>{percent}</div>
                  <div className={styles.dataCol}>{formatNumber(level.levelValues.Worker)}</div>
                  <div className={styles.dataCol}>{formatNumber(level.levelValues.Unemployed)}</div>
                  <div className={styles.dataCol}>{unemployment}</div>
                  <div className={styles.dataCol}>{formatNumber(level.levelValues.Under)}</div>
                  <div className={styles.dataCol}>{formatNumber(level.levelValues.Outside)}</div>
                  <div className={styles.dataCol}>{formatNumber(level.levelValues.Homeless)}</div>
                </div>
              );
            })}
          </div>
        </div>
      </Scrollable>
    </Panel>
  );
};

export default Workforce;