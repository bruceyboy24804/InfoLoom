import React, { FC } from 'react';
import { useValue } from 'cs2/api';
import { DraggablePanelProps, Panel, Scrollable, Tooltip } from "cs2/ui";
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
    // Workforce status colors
    status: {
      employed: '#4CAF50', // Green
      unemployed: '#F44336', // Red
      underemployed: '#FFC107', // Yellow/amber
      outside: '#9E9E9E', // Gray
      homeless: '#673AB7', // Purple
    }
  };

  // Configure our data for display
  const workforceLevels = [
    { levelColor: colors.education.uneducated, levelName: 'Uneducated', levelValues: ilWorkforce[0] },
    { levelColor: colors.education.poorlyEducated, levelName: 'Poorly Educated', levelValues: ilWorkforce[1] },
    { levelColor: colors.education.educated, levelName: 'Educated', levelValues: ilWorkforce[2] },
    { levelColor: colors.education.wellEducated, levelName: 'Well Educated', levelValues: ilWorkforce[3] },
    { levelColor: colors.education.highlyEducated, levelName: 'Highly Educated', levelValues: ilWorkforce[4] },
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
        breakdown: [] as {level: string, count: number, color: string, levelTotal: number, rate: number, idealJobType: string, jobTakingDetails: {levelName: string, count: number}[]}[]
      },
      homelessWorkers: {
        total: totalHomeless,
        breakdown: [] as {level: string, count: number, color: string}[]
      },
      outsideWorkers: {
        total: totalOutsideWorkforce,
        breakdown: [] as {level: string, count: number, color: string}[]
      }
    };

    // Store information about which jobs underemployed people are currently working in
    // This is a simplified model based on available data
    // Key structure: "SourceEducation_TargetJob" -> count
    const underemploymentDistribution: Record<string, number> = {};

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
      const levelTotal = level.levelValues.Total || 0;
      const underemploymentRate = levelTotal > 0 ? (underemployed / levelTotal) * 100 : 0;

      // Map education level to ideal job type
      let idealJobType = "Higher education jobs";
      let jobTakingDetails: {levelName: string, count: number}[] = [];

      // Calculate detailed distribution of underemployed workers into different job categories
      // This is an approximation based on available data
      if (level.levelName === 'Highly Educated') {
        idealJobType = "Not applicable"; // They're at the top already

        // Distribution of Highly Educated workers in lower-level jobs
        // Assuming most (60%) work in Well Educated jobs, 30% in Educated jobs, 10% in Poorly Educated
        const wellEducatedJobsCount = Math.round(underemployed * 0.6);
        const educatedJobsCount = Math.round(underemployed * 0.3);
        const poorlyEducatedJobsCount = Math.round(underemployed * 0.1);

        jobTakingDetails = [
          { levelName: 'Well Educated', count: wellEducatedJobsCount },
          { levelName: 'Educated', count: educatedJobsCount },
          { levelName: 'Poorly Educated', count: poorlyEducatedJobsCount }
        ];

        // Store for analysis if needed elsewhere
        underemploymentDistribution["Highly Educated_Well Educated"] = wellEducatedJobsCount;
        underemploymentDistribution["Highly Educated_Educated"] = educatedJobsCount;
        underemploymentDistribution["Highly Educated_Poorly Educated"] = poorlyEducatedJobsCount;
      }
      else if (level.levelName === 'Well Educated') {
        idealJobType = "Highly Educated jobs";

        // Distribution of Well Educated workers in lower-level jobs
        // Assuming most (70%) work in Educated jobs, 30% in Poorly Educated jobs
        const educatedJobsCount = Math.round(underemployed * 0.7);
        const poorlyEducatedJobsCount = Math.round(underemployed * 0.3);

        jobTakingDetails = [
          { levelName: 'Educated', count: educatedJobsCount },
          { levelName: 'Poorly Educated', count: poorlyEducatedJobsCount }
        ];

        // Store for analysis
        underemploymentDistribution["Well Educated_Educated"] = educatedJobsCount;
        underemploymentDistribution["Well Educated_Poorly Educated"] = poorlyEducatedJobsCount;
      }
      else if (level.levelName === 'Educated') {
        idealJobType = "Well Educated jobs";

        // All Educated underemployed workers assumed to work in Poorly Educated jobs
        jobTakingDetails = [
          { levelName: 'Poorly Educated', count: underemployed }
        ];

        // Store for analysis
        underemploymentDistribution["Educated_Poorly Educated"] = underemployed;
      }
      else if (level.levelName === 'Poorly Educated') {
        idealJobType = "Educated jobs";

        // All Poorly Educated underemployed workers assumed to work in Uneducated jobs
        jobTakingDetails = [
          { levelName: 'Uneducated', count: underemployed }
        ];

        // Store for analysis
        underemploymentDistribution["Poorly Educated_Uneducated"] = underemployed;
      }
      else {
        idealJobType = "Not applicable"; // For Uneducated
      }

      if (underemployed > 0) {
        mismatchData.underemployedWorkers.breakdown.push({
          level: level.levelName,
          count: underemployed,
          color: level.levelColor,
          levelTotal: levelTotal,
          rate: underemploymentRate,
          idealJobType: idealJobType,
          jobTakingDetails: jobTakingDetails
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

      // Outside workforce breakdown
      const outside = level.levelValues.Outside || 0;
      if (outside > 0) {
        mismatchData.outsideWorkers.breakdown.push({
          level: level.levelName,
          count: outside,
          color: level.levelColor
        });
      }
    });
    
    return mismatchData;
  };
  
  const mismatchData = calculateWorkforceMismatch();

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
          {/* Detailed Data Table - Moved to top */}
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
          
          {/* Unemployment table */}
          {totalUnemployed > 0 && (
            <div>
              <div className={styles.tableSectionTitle}>Unemployment Breakdown</div>
              <div className={styles.tableDescription}>Citizens looking for work: {formatNumber(totalUnemployed)}</div>
              <div>
                <div className={styles.headerRow}>
                  <div className={styles.educationCol}>Education Level</div>
                  <div className={styles.dataCol}>Unemployed</div>
                  <div className={styles.dataCol}>% of Level</div>
                  <div className={styles.dataCol}>% of Total Unemployed</div>
                </div>
                {mismatchData.unemployedByEducation.breakdown.map((item, idx) => (
                  <div key={idx} className={styles.workforceRow}>
                    <div className={styles.educationCol}>
                      <div className={styles.colorBox} style={{ backgroundColor: item.color }}></div>
                      {item.level}
                    </div>
                    <div className={styles.dataCol}>{formatNumber(item.count)}</div>
                    <div className={styles.dataCol}>{item.rate.toFixed(1)}%</div>
                    <div className={styles.dataCol}>{getPercentage(item.count, totalUnemployed)}</div>
                  </div>
                ))}
                <div className={`${styles.workforceRow} ${styles.totalRow}`}>
                  <div className={styles.educationCol}>TOTAL</div>
                  <div className={styles.dataCol}>{formatNumber(totalUnemployed)}</div>
                  <div className={styles.dataCol}>-</div>
                  <div className={styles.dataCol}>100%</div>
                </div>
              </div>
            </div>
          )}

          {/* Underemployment table */}
          {totalUnderemployed > 0 && (
            <div>
              <div className={styles.tableSectionTitle}>Underemployment Breakdown</div>
              <div className={styles.tableDescription}>Workers in jobs below their education level: {formatNumber(totalUnderemployed)}</div>
              <div>
                <div className={styles.headerRow}>
                  <div className={styles.educationCol}>Education Level</div>
                  <div className={styles.dataCol}>Underemployed</div>
                  <div className={styles.dataCol}>% of Level</div>
                  <div className={styles.dataCol}>Ideal Jobs</div>
                  <div className={styles.dataCol}>Working In</div>
                </div>
                {mismatchData.underemployedWorkers.breakdown.map((item, idx) => {
                  // Find the workers with the education level matching the ideal job type
                  let idealWorkersCount = 0;
                  if (item.idealJobType !== "Not applicable") {
                    // Map ideal job type back to education level
                    const targetEducationLevel = item.idealJobType.replace(" jobs", "");
                    // Find this education level in the workforceLevels array
                    const targetLevel = workforceLevels.find(wl => wl.levelName === targetEducationLevel);
                    if (targetLevel) {
                      idealWorkersCount = targetLevel.levelValues.Total || 0;
                    }
                  }

                  // Create tooltip content showing detailed job distribution
                  const tooltipContent = (
                    <div className={styles.tooltipContent}>
                      <div className={styles.tooltipTitle}>Job Distribution</div>
                      {item.jobTakingDetails.map((detail, detailIdx) => (
                        <div key={detailIdx} className={styles.tooltipRow}>
                          <span>{detail.levelName}:</span>
                          <span>{formatNumber(detail.count)}</span>
                        </div>
                      ))}
                    </div>
                  );

                  return (
                  <div key={idx} className={styles.workforceRow}>
                    <div className={styles.educationCol}>
                      <div className={styles.colorBox} style={{ backgroundColor: item.color }}></div>
                      {item.level}
                    </div>
                    <div className={styles.dataCol}>{formatNumber(item.count)}</div>
                    <div className={styles.dataCol}>{item.rate.toFixed(1)}%</div>
                    <div className={styles.dataCol}>
                      {item.idealJobType !== "Not applicable" ? (
                        <Tooltip tooltip={`Total ${item.idealJobType}: ${formatNumber(idealWorkersCount)}`}>
                          <div className={styles.tooltipTrigger}>{item.idealJobType}</div>
                        </Tooltip>
                      ) : "N/A"}
                    </div>
                    <div className={styles.dataCol}>
                      <Tooltip tooltip={tooltipContent}>
                        <div className={styles.tooltipTrigger}>
                          {item.jobTakingDetails.length > 0 ?
                            `${item.jobTakingDetails[0].levelName} + ${item.jobTakingDetails.length > 1 ? 
                              (item.jobTakingDetails.length - 1) : 0} more` :
                            "N/A"}
                        </div>
                      </Tooltip>
                    </div>
                  </div>
                )})}
                <div className={`${styles.workforceRow} ${styles.totalRow}`}>
                  <div className={styles.educationCol}>TOTAL</div>
                  <div className={styles.dataCol}>{formatNumber(totalUnderemployed)}</div>
                  <div className={styles.dataCol}>-</div>
                  <div className={styles.dataCol}>-</div>
                  <div className={styles.dataCol}>-</div>
                </div>
              </div>
            </div>
          )}

          {/* Outside Workforce table */}
          {totalOutsideWorkforce > 0 && (
            <div>
              <div className={styles.tableSectionTitle}>Outside Workforce Breakdown</div>
              <div className={styles.tableDescription}>Citizens working outside the city: {formatNumber(totalOutsideWorkforce)}</div>
              <div>
                <div className={styles.headerRow}>
                  <div className={styles.educationCol}>Education Level</div>
                  <div className={styles.dataCol}>Outside Workers</div>
                  <div className={styles.dataCol}>% of Total Outside</div>
                </div>
                {mismatchData.outsideWorkers.breakdown.map((item, idx) => (
                  <div key={idx} className={styles.workforceRow}>
                    <div className={styles.educationCol}>
                      <div className={styles.colorBox} style={{ backgroundColor: item.color }}></div>
                      {item.level}
                    </div>
                    <div className={styles.dataCol}>{formatNumber(item.count)}</div>
                    <div className={styles.dataCol}>{getPercentage(item.count, totalOutsideWorkforce)}</div>
                  </div>
                ))}
                <div className={`${styles.workforceRow} ${styles.totalRow}`}>
                  <div className={styles.educationCol}>TOTAL</div>
                  <div className={styles.dataCol}>{formatNumber(totalOutsideWorkforce)}</div>
                  <div className={styles.dataCol}>100%</div>
                </div>
              </div>
            </div>
          )}

          {/* Homeless Workers table */}
          {totalHomeless > 0 && (
            <div>
              <div className={styles.tableSectionTitle}>Homeless Citizens Breakdown</div>
              <div className={styles.tableDescription}>Homeless citizens: {formatNumber(totalHomeless)}</div>
              <div>
                <div className={styles.headerRow}>
                  <div className={styles.educationCol}>Education Level</div>
                  <div className={styles.dataCol}>Homeless</div>
                  <div className={styles.dataCol}>% of Total Homeless</div>
                </div>
                {mismatchData.homelessWorkers.breakdown.map((item, idx) => (
                  <div key={idx} className={styles.workforceRow}>
                    <div className={styles.educationCol}>
                      <div className={styles.colorBox} style={{ backgroundColor: item.color }}></div>
                      {item.level}
                    </div>
                    <div className={styles.dataCol}>{formatNumber(item.count)}</div>
                    <div className={styles.dataCol}>{getPercentage(item.count, totalHomeless)}</div>
                  </div>
                ))}
                <div className={`${styles.workforceRow} ${styles.totalRow}`}>
                  <div className={styles.educationCol}>TOTAL</div>
                  <div className={styles.dataCol}>{formatNumber(totalHomeless)}</div>
                  <div className={styles.dataCol}>100%</div>
                </div>
              </div>
            </div>
          )}
        </div>
      </Scrollable>
    </Panel>
  );
};

export default Workforce;

