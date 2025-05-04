import React, {memo, useEffect, useMemo, useRef, useState} from 'react';
import styles from './Demographics.module.scss';
import Chart from 'chart.js/auto';

// Local or app-level imports
import {useValue} from 'cs2/api';
import {InfoCheckbox} from 'mods/components/InfoCheckbox/InfoCheckbox';
import {DraggablePanelProps, Dropdown, DropdownItem, DropdownToggle, Panel, Scrollable,} from "cs2/ui";
import {populationAtAge} from "../../domain/populationAtAge";
import {GroupingStrategy} from "../../domain/GroupingStrategy";
import {
  DemoAgeGroupingToggledOn,
  DemographicsDataDetails,
  DemographicsDataOldestCitizen,
  DemographicsDataTotals,
  DemoStatsToggledOn,
  SetDemoStatsToggledOn,
  DemoGroupingStrategy,
  SetDemoGroupingStrategy
} from "../../bindings";
import {InfoRadioButton} from "../../components/InfoRadioButton/InfoRadioButton";
import {getModule} from "cs2/modding";


const DropdownStyle = getModule("game-ui/menu/themes/dropdown.module.scss", "classes");


interface AlignedParagraphProps {
  left: string;
  right: number;
}

type ChartOrientation = 'horizontal'; // we only have horizontal

interface AgeRange {
  label: string;
  min: number;
  max: number;
}

interface GroupingOption {
  label: string;
  value: GroupingStrategy;
  ranges: AgeRange[];
}

interface AggregatedInfo {
  label: string;
  work: number;
  elementary: number;
  highSchool: number;
  college: number;
  university: number;
  other: number;
  total: number;
}

interface StatisticsSummaryProps {
  StructureTotals: number[];
  OldestCitizen: number;
}

interface GroupingOptionsProps {
  groupingStrategy: GroupingStrategy;
  setGroupingStrategy: React.Dispatch<React.SetStateAction<GroupingStrategy>>;
  totalEntries: number;
}

// === Constants & helper functions ===

/** Common font settings used in chart options and UI elements. */
const commonFont = {
  family: 'Arial, sans-serif',
  size: 14,
  weight: 'normal' as const,
};

const yaxisfont = {
  family: 'Arial, sans-serif',
  size: 11,
  weight: 'normal' as const,
};

/**  
 * Hardcoded orientation properties because we only have a horizontal chart.
 */
function getOrientationProps() {
  return {
    indexAxis: 'y' as const,
    mainAxisTitle: 'Age in Days',
    crossAxisTitle: 'Number of People',
    minChartHeight: '400px',
  };
}

/** Utility to generate AgeRange[] for grouping. */
function generateRanges(step: number): AgeRange[] {
  const ranges: AgeRange[] = [];
  for (let i = 0; i < 120; i += step) {
    ranges.push({
      label:
        i === 0
          ? `0-${step}`
          : i + step >= 120
          ? `${i}-${120}`
          : `${i}-${i + step}`,
      min: i,
      max: i + step,
    });
  }
  return ranges;
}

/** Predefined grouping strategies. */
const GROUP_STRATEGIES: GroupingOption[] = [
  {
    label: 'Detailed View',
    value: GroupingStrategy.None,
    ranges: [],
  },
  {
    label: '5-Year Groups',
    value: GroupingStrategy.FiveYear,
    ranges: generateRanges(5),
  },
  {
    label: '10-Year Groups',
    value: GroupingStrategy.TenYear,
    ranges: generateRanges(10),
  },
  {
    label: 'Lifecycle Groups',
    value: GroupingStrategy.LifeCycle,
    ranges: [
      { label: 'Child', min: 0, max: 20 },
      { label: 'Teen', min: 21, max: 35 },
      { label: 'Adult', min: 36, max: 83 },
      { label: 'Elderly', min: 84, max: 120 },
    ],
  },
];

/**
 * Helper function to group raw population data based on the selected grouping strategy.
 */
function aggregatePopulationData(
  details: populationAtAge[],
  grouping: GroupingOption
): AggregatedInfo[] {
  // Initialize aggregated data structure
  const aggregated = grouping.ranges.map((range) => ({
    label: range.label,
    work: 0,
    elementary: 0,
    highSchool: 0,
    college: 0,
    university: 0,
    other: 0,
    total: 0,
  }));

  details.forEach((info) => {
    if (info.Age > 120) return;
    const idx = grouping.ranges.findIndex(
      (range) => info.Age >= range.min && info.Age <= range.max
    );
    if (idx !== -1) {
      const agg = aggregated[idx];
      agg.work += info.Work;
      agg.elementary += info.School1;
      agg.highSchool += info.School2;
      agg.college += info.School3;
      agg.university += info.School4;
      agg.other += info.Other;
      agg.total += info.Total;
    }
  });

  return aggregated;
}

// === Reusable sub-components ===

/**
 * A simple row displaying "left" label and "right" numeric value, aligned and spaced.
 */
const AlignedParagraph = ({ left, right }: AlignedParagraphProps): JSX.Element => {
  return (
    <div className={styles.alignedParagraph}>
      <div className={styles.label}>{left}</div>
      <div className={styles.value}>{right}</div>
    </div>
  );
};

/**
 * Checkbox UI for selecting a grouping strategy.
 */
/*const GroupingOptions = ({
  groupingStrategy,
  setGroupingStrategy,
  totalEntries,
}: GroupingOptionsProps): JSX.Element => {
  return (
    <div className={styles.groupingOptions}>
      <div className={styles.label}>Age Grouping</div>
      {GROUP_STRATEGIES.map((strategy) => (
        <InfoRadioButton
          key={strategy.value}
          label={strategy.label}
          isSelected={groupingStrategy === strategy.value}
          onSelect={() => setGroupingStrategy(strategy.value)}
          count={strategy.ranges.length || totalEntries}
        />
      ))}
    </div>
  );
};*/

/**
 * Displays high-level statistics (Population, Tourists, etc.).
 */
const StatisticsSummary = ({
  StructureTotals,
  OldestCitizen,
}: StatisticsSummaryProps): JSX.Element => {
  // Safely extract required totals
  const allCitizens = StructureTotals[0];
  const population = StructureTotals[1];
  const tourists = StructureTotals[2];
  const commuters = StructureTotals[3];
  const students = StructureTotals[4];
  const workers = StructureTotals[5];
  const movingAway = StructureTotals[7];
  const dead = StructureTotals[8];
  const homeless = StructureTotals[9];

  return (
    <div className={styles.statisticsContainer}>
      <div className={`${styles.statisticsColumn} ${styles.left}`}>
        <AlignedParagraph left="All Citizens" right={allCitizens} />
        <div className={styles.spacer} />
        <AlignedParagraph left="- Tourists" right={tourists} />
        <div className={styles.spacer} />
        <AlignedParagraph left="- Commuters" right={commuters} />
        <div className={styles.spacer} />
        <AlignedParagraph left="- Moving Away" right={movingAway} />
        <div className={styles.spacer} />
        <AlignedParagraph left="Population" right={population} />
      </div>
      <div className={`${styles.statisticsColumn} ${styles.right}`}>
        <AlignedParagraph left="Dead" right={dead} />
        <div className={styles.spacer} />
        <AlignedParagraph left="Students" right={students} />
        <div className={styles.spacer} />
        <AlignedParagraph left="Workers" right={workers} />
        <div className={styles.spacer} />
        <AlignedParagraph left="Homeless" right={homeless} />
        <div className={styles.spacer} />
        <AlignedParagraph left="Oldest Citizen" right={OldestCitizen} />
      </div>
    </div>
  );
};

/**
 * Replace the custom canvas DemographicsChart with Chart.js implementation
 */
const DemographicsChart = memo(({

  StructureDetails,
  groupingStrategy,
                                }: {
  StructureDetails: populationAtAge[];
  groupingStrategy: GroupingStrategy;
}): JSX.Element => {
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const chartRef = useRef<Chart | null>(null);
  const containerRef = useRef<HTMLDivElement | null>(null);

  // Define chart colors with clear names
  const chartColors = {
    work: '#624532',
    elementary: '#7E9EAE',
    highSchool: '#00C217',
    college: '#005C4E',
    university: '#2462FF',
    other: '#A1A1A1',
  };

  // Build chart data from StructureDetails based on groupingStrategy
  const chartData = useMemo(() => {
    if (!StructureDetails.length) {
      return {
        labels: [] as string[],
        datasets: [] as {
          label: string;
          data: number[];
          backgroundColor: string;
        }[]
      };
    }

    let labels: string[] = [];
    let datasets: Array<{
      label: string;
      data: number[];
      backgroundColor: string;
    }> = [
      { label: 'Work', data: [] as number[], backgroundColor: chartColors.work },
      { label: 'Elementary', data: [] as number[], backgroundColor: chartColors.elementary },
      { label: 'High School', data: [] as number[], backgroundColor: chartColors.highSchool },
      { label: 'College', data: [] as number[], backgroundColor: chartColors.college },
      { label: 'University', data: [] as number[], backgroundColor: chartColors.university },
      { label: 'Other', data: [] as number[], backgroundColor: chartColors.other },
    ];

    if (groupingStrategy === GroupingStrategy.None) {
      // Generate detailed view from age 0 to 120
      const age_range = Array.from({ length: 121 }, (_, i) => i);
      labels = age_range.map(age => String(age));
      
      // Helper function to sum values for a given attribute at a specific age
      const get_value = (attr: keyof populationAtAge, age: number): number => {
        return StructureDetails.filter(d => d.Age === age)
          .reduce((sum, d) => sum + (d[attr] || 0), 0);
      };
      
      datasets = [
        { 
          label: 'Work', 
          data: age_range.map(age => get_value('Work', age)), 
          backgroundColor: chartColors.work 
        },
        { 
          label: 'Elementary', 
          data: age_range.map(age => get_value('School1', age)), 
          backgroundColor: chartColors.elementary 
        },
        { 
          label: 'High School', 
          data: age_range.map(age => get_value('School2', age)), 
          backgroundColor: chartColors.highSchool 
        },
        { 
          label: 'College', 
          data: age_range.map(age => get_value('School3', age)), 
          backgroundColor: chartColors.college 
        },
        { 
          label: 'University', 
          data: age_range.map(age => get_value('School4', age)), 
          backgroundColor: chartColors.university 
        },
        { 
          label: 'Other', 
          data: age_range.map(age => get_value('Other', age)), 
          backgroundColor: chartColors.other 
        },
      ];
    } else if (groupingStrategy === GroupingStrategy.LifeCycle) {
      // Use the predefined lifecycle ranges
      const lifecycleRanges = GROUP_STRATEGIES.find(s => s.value === GroupingStrategy.LifeCycle)?.ranges || [];
      const groups = lifecycleRanges.map(range => ({
        label: range.label,
        work: 0,
        elementary: 0,
        highSchool: 0,
        college: 0,
        university: 0,
        other: 0
      }));

      StructureDetails.forEach(d => {
        if (d.Age > 120) return;
        const idx = lifecycleRanges.findIndex(range => d.Age >= range.min && d.Age <= range.max);
        if (idx !== -1) {
          groups[idx].work += d.Work;
          groups[idx].elementary += d.School1;
          groups[idx].highSchool += d.School2;
          groups[idx].college += d.School3;
          groups[idx].university += d.School4;
          groups[idx].other += d.Other;
        }
      });

      labels = groups.map(g => g.label);
      datasets = [
        { label: 'Work', data: groups.map(g => g.work), backgroundColor: chartColors.work },
        { label: 'Elementary', data: groups.map(g => g.elementary), backgroundColor: chartColors.elementary },
        { label: 'High School', data: groups.map(g => g.highSchool), backgroundColor: chartColors.highSchool },
        { label: 'College', data: groups.map(g => g.college), backgroundColor: chartColors.college },
        { label: 'University', data: groups.map(g => g.university), backgroundColor: chartColors.university },
        { label: 'Other', data: groups.map(g => g.other), backgroundColor: chartColors.other },
      ];
    } else {
      // Handle 5-year and 10-year grouping
      const step = groupingStrategy === GroupingStrategy.FiveYear ? 5 : 10;
      const groups: { label: string; work: number; elementary: number; highSchool: number; college: number; university: number; other: number; }[] = [];
      for (let i = 0; i < 120; i += step) {
        groups.push({
          label: `${i}-${i + step}`,
          work: 0,
          elementary: 0,
          highSchool: 0,
          college: 0,
          university: 0,
          other: 0,
        });
      }
      
      StructureDetails.forEach(d => {
        if (d.Age > 120) return;
        const idx = Math.floor(d.Age / step);
        if (groups[idx]) {
          groups[idx].work += d.Work;
          groups[idx].elementary += d.School1;
          groups[idx].highSchool += d.School2;
          groups[idx].college += d.School3;
          groups[idx].university += d.School4;
          groups[idx].other += d.Other;
        }
      });
      
      labels = groups.map(g => g.label);
      datasets = [
        { label: 'Work', data: groups.map(g => g.work), backgroundColor: chartColors.work },
        { label: 'Elementary', data: groups.map(g => g.elementary), backgroundColor: chartColors.elementary },
        { label: 'High School', data: groups.map(g => g.highSchool), backgroundColor: chartColors.highSchool },
        { label: 'College', data: groups.map(g => g.college), backgroundColor: chartColors.college },
        { label: 'University', data: groups.map(g => g.university), backgroundColor: chartColors.university },
        { label: 'Other', data: groups.map(g => g.other), backgroundColor: chartColors.other },
      ];
    }

    return {
      labels,
      datasets
    };
  }, [StructureDetails, groupingStrategy]);

  // Initialize chart ONLY ONCE - based on TradeCost.tsx pattern
  useEffect(() => {
    if (!canvasRef.current || !containerRef.current) return;
    const ctx = canvasRef.current.getContext('2d');
    if (!ctx) return;
    
    chartRef.current = new Chart(ctx, {
      type: 'bar',
      data: chartData,
      options: {
        indexAxis: 'y', // Horizontal bar chart
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          title: {
            display: true,
            text: 'Population Demographics by Age',
            color: '#ffffff',
            font: { size: 16 }
          },
          tooltip: {
            mode: 'index', // Changed from 'nearest' to 'index' to show all categories
            intersect: true, // Changed to false to trigger tooltip more easily
            position: 'nearest',
            caretSize: 5,
            backgroundColor: '#171d2b',
            titleFont: { weight: 'bold', size: 14, family: 'Overpass' }, // Set title font
            bodyFont: { size: 12, family: 'Overpass' }, // Set body font
            footerFont: { weight: 'bold', size: 12, family: 'Overpass' }, // Set footer font
            
            padding: 10,
            // Improved tooltip content
            callbacks: {
              title: (tooltipItems) => {
                const item = tooltipItems[0];
                return `Age: ${item.label}`;
              },
              label: (context) => {
                // Format numbers with commas for thousands
                const formattedNumber = context.raw ? context.raw.toLocaleString() : '0';
                return `${context.dataset.label}: ${formattedNumber}`;
              },
              footer: (tooltipItems) => {
                let total = 0;
                tooltipItems.forEach(item => {
                  total += Number(item.raw) || 0;
                });
                return `Total: ${total.toLocaleString()}`;
              }
            }
          },
          legend: {
            position: 'top',
            labels: { color: '#ffffff', padding: 15, font: { size: 16, family: 'Overpass' } }
          }
        },
        scales: {
          x: {
            stacked: true,
            grid: { color: 'rgba(255, 255, 255, 0.1)' },
            ticks: { color: '#ffffff', font:  { size: 12, family: 'Overpass' } },
            title: {
              display: true,
              text: 'Number of People',
              color: '#ffffff'
            }
          },
          y: {
            stacked: true,
            grid: { color: 'rgba(255, 255, 255, 0.1)' },
            ticks: {
              color: '#ffffff',
              // Use autoSkip when in detailed view, disable for grouped views
              autoSkip: groupingStrategy === GroupingStrategy.None,
              // Increase tick limit for grouped views
              maxTicksLimit: groupingStrategy === GroupingStrategy.None ? 30 : 20,
              // Add padding between labels for detailed view
              padding: groupingStrategy === GroupingStrategy.None ? 8 : 2,
              font:  { size: 12, family: 'Overpass' }
            },
            // Adjust the spacing between bars for detailed view
            afterFit: function(scaleInstance) {
              // Set different heights based on grouping
              if (groupingStrategy === GroupingStrategy.None) {
                scaleInstance.height = Math.min(1000, scaleInstance.height);
              } else if (groupingStrategy === GroupingStrategy.LifeCycle) {
                // Make lifecycle bars taller (few categories)
                scaleInstance.height = Math.min(400, scaleInstance.height);
              } else {
                // Make 5-year and 10-year bars taller
                scaleInstance.height = Math.min(1000, scaleInstance.height);
              }
            },
            title: {
              display: true, 
              text: 'Age',
              color: '#ffffff'
            },
          }
        },
        datasets: {
			bar: {
				// Optimized barThickness for each view type
				barThickness: 
				  groupingStrategy === GroupingStrategy.None ? 8 :
				  groupingStrategy === GroupingStrategy.FiveYear ? 15 :
				  groupingStrategy === GroupingStrategy.TenYear ? 25 :
				  80, // lifecycle
				
				// Optimized barPercentage for better spacing
				barPercentage: 
				  groupingStrategy === GroupingStrategy.None ? 0.98 :
				  groupingStrategy === GroupingStrategy.FiveYear ? 0.9 :
				  groupingStrategy === GroupingStrategy.TenYear ? 0.85 :
				  0.8, // lifecycle
				
				// Optimized categoryPercentage for each view type
				categoryPercentage: 
				  groupingStrategy === GroupingStrategy.None ? 0.95 :
				  groupingStrategy === GroupingStrategy.FiveYear ? 0.85 :
				  groupingStrategy === GroupingStrategy.TenYear ? 0.9 :
				  0.95, // lifecycle
				
				// Add maxBarThickness to prevent bars from becoming too large
				maxBarThickness: 
				  groupingStrategy === GroupingStrategy.None ? 5 :
				  groupingStrategy === GroupingStrategy.FiveYear ? 25 :
				  groupingStrategy === GroupingStrategy.TenYear ? 35 :
				  120, // lifecycle
			  }
        },
        animation: { duration: 0 } // Disable animations
      }
    });
    
    // Clean up on unmount
    return () => {
      if (chartRef.current) {
        chartRef.current.destroy();
        chartRef.current = null;
      }
    };
  }, []); // Empty dependency array - initialize only once

  // Update chart data when it changes (separate from initialization)
  useEffect(() => {
    if (!chartRef.current) return;
    
    // Only update data, don't recreate the entire chart
    chartRef.current.data = chartData;
    chartRef.current.update('none'); // Use 'none' mode to skip animations
  }, [chartData]);

  // Handle resize events
  useEffect(() => {
    if (!containerRef.current || !canvasRef.current) return;
    
    const resizeObserver = new ResizeObserver((entries) => {
      if (!entries[0]) return;
      
      const { width, height } = entries[0].contentRect;
      if (canvasRef.current && width > 0 && height > 0) {
        // Set exact dimensions only if valid values are provided
        canvasRef.current.width = width;
        canvasRef.current.height = height;
      }
      
      if (chartRef.current) {
        chartRef.current.resize();
      }
    });
    
    resizeObserver.observe(containerRef.current);
    return () => resizeObserver.disconnect();
  }, []);

  // Fix the issue with bar size not increasing for grouped views
  useEffect(() => {
    // Update chart options or create a new chart when groupingStrategy changes
    if (!chartRef.current) return;
    
    // Update chart options to ensure bar sizes are applied correctly
    chartRef.current.options.datasets = {
		bar: {
			// Optimized barThickness for each view type
			barThickness: 
			  groupingStrategy === GroupingStrategy.None ? 8 :
			  groupingStrategy === GroupingStrategy.FiveYear ? 15 :
			  groupingStrategy === GroupingStrategy.TenYear ? 25 :
			  80, // lifecycle
			
			// Optimized barPercentage for better spacing
			barPercentage: 
			  groupingStrategy === GroupingStrategy.None ? 0.98 :
			  groupingStrategy === GroupingStrategy.FiveYear ? 0.9 :
			  groupingStrategy === GroupingStrategy.TenYear ? 0.85 :
			  0.8, // lifecycle
			
			// Optimized categoryPercentage for each view type
			categoryPercentage: 
			  groupingStrategy === GroupingStrategy.None ? 0.95 :
			  groupingStrategy === GroupingStrategy.FiveYear ? 0.85 :
			  groupingStrategy === GroupingStrategy.TenYear ? 0.9 :
			  0.95, // lifecycle
			
			// Add maxBarThickness to prevent bars from becoming too large
			maxBarThickness: 
			  groupingStrategy === GroupingStrategy.None ? 5 :
			  groupingStrategy === GroupingStrategy.FiveYear ? 25 :
			  groupingStrategy === GroupingStrategy.TenYear ? 35 :
			  120, // lifecycle
		  }
    };

    // Also update scale configurations for different grouping strategies
    if (chartRef.current.options.scales?.y) {
      const yScale = chartRef.current.options.scales.y;
      
      // Set different heights based on current grouping
      if (yScale.afterFit) {
        yScale.afterFit = function(scaleInstance) {
          if (groupingStrategy === GroupingStrategy.None) {
            scaleInstance.height = Math.min(2500, scaleInstance.height);
          } else if (groupingStrategy === GroupingStrategy.LifeCycle) {
            scaleInstance.height = Math.min(400, scaleInstance.height);
          } else {
            scaleInstance.height = Math.min(1000, scaleInstance.height);
          }
        };
      }

      // Update tick configurations - Fix TypeScript error by using proper typing
      if (yScale.ticks) {
        // Use type assertion to access the properties safely
        const ticks = yScale.ticks as any;
        ticks.autoSkip = groupingStrategy === GroupingStrategy.None;
        ticks.maxTicksLimit = groupingStrategy === GroupingStrategy.None ? 30 : 20;
        ticks.padding = groupingStrategy === GroupingStrategy.None ? 15 : 8;
        ticks.lineHeight = groupingStrategy === GroupingStrategy.None ? 5 : 1;
      }
    }
    
    // Apply the updates
    chartRef.current.update('none');
  }, [groupingStrategy]);

  return (
    <div className={styles.chartContainer} ref={containerRef} style={{ width: '100%', height: '100%' }}>
      <canvas ref={canvasRef} />
    </div>
  );
});



// === Main Demographics Component ===
const Demographics = ({ onClose }: DraggablePanelProps): JSX.Element => {
  const [groupingStrategy, setGroupingStrategy] = useState<GroupingStrategy>(GroupingStrategy.None);
  const demographicsDataStructureDetails = useValue(DemographicsDataDetails);
  const demographicsDataStructureTotals = useValue(DemographicsDataTotals);
  const demographicsDataOldestCitizen = useValue(DemographicsDataOldestCitizen);
  const demoStatsToggledOn = useValue(DemoStatsToggledOn);
  const demoAgeGroupingToggledOn = useValue(DemoAgeGroupingToggledOn)
  const demoGroupingStrategy = useValue(DemoGroupingStrategy);

  function GetSelectedGroupStrategy() 
  {
    for (let i=0; i<GROUP_STRATEGIES.length; i++) 
    {
      if (GROUP_STRATEGIES[i].value == demoGroupingStrategy) 
      {
        let strategy = GROUP_STRATEGIES[i];
        return (
          <DropdownItem value={strategy} className={styles.dropdownItem} selected={demoGroupingStrategy == strategy.value} onChange={() =>  SetDemoGroupingStrategy(strategy.value)}>
              <div key={strategy.value} className={styles.dropdownItem}>
              <InfoRadioButton
                  label={strategy.label}
                  isChecked={demoGroupingStrategy == strategy.value}
                  groupingStrategy={strategy.value}
                  onToggle={SetDemoGroupingStrategy}
                  count={strategy.ranges.length || demographicsDataStructureDetails?.length || 0}
              />
            </div>
          </DropdownItem>
        );
      }
    }

    let strategy = GROUP_STRATEGIES[0];
    return (
      <DropdownItem value={strategy} className={styles.dropdownItem} selected={demoGroupingStrategy == strategy.value} onChange={() =>  SetDemoGroupingStrategy(strategy.value)}>
          <div key={strategy.value} className={styles.dropdownItem}>
          <InfoRadioButton
              label={strategy.label}
              isChecked={demoGroupingStrategy == strategy.value}
              groupingStrategy={strategy.value}
              onToggle={SetDemoGroupingStrategy}
              count={strategy.ranges.length || demographicsDataStructureDetails?.length || 0}
          />
        </div>
      </DropdownItem>
    );
  }

  return (
      <Panel
          draggable
          onClose={onClose}
          className={styles.panel}
          initialPosition={{x: 0.16, y: 0.15}}
          header={
            <div className={styles.header}>
              <span className={styles.headerText}>Demographics</span>
            </div>
          }
      >
        <div className={styles.container}>
          <div className={styles.toggleContainer}>
            <InfoCheckbox
                label="Show Statistics"
                isChecked={demoStatsToggledOn}
                onToggle={SetDemoStatsToggledOn}

            />
            <div>
              Age Grouping Options
              <Dropdown
                  theme={DropdownStyle}
                  
                  content={  
                    <div className={styles.dropdownContent}>
                      {GROUP_STRATEGIES.map((strategy) => (
                        <DropdownItem value={strategy} className={styles.dropdownItem} selected={demoGroupingStrategy == strategy.value} onChange={() =>  SetDemoGroupingStrategy(strategy.value)}>
                          <div key={strategy.value} className={styles.dropdownItem}>
                            <InfoRadioButton
                                label={strategy.label}
                                isChecked={demoGroupingStrategy == strategy.value}
                                groupingStrategy={strategy.value}
                                onToggle={SetDemoGroupingStrategy}
                                count={strategy.ranges.length || demographicsDataStructureDetails?.length || 0}
                            />
                          </div>
                        </DropdownItem>
                      ))}
                    </div>
                  }
              >
                <DropdownToggle style={{ marginRight: '5rem' }}>
                  {GetSelectedGroupStrategy()}
                </DropdownToggle>
              </Dropdown>
            </div>
          </div>

          {demoStatsToggledOn && (
              <StatisticsSummary
                  StructureTotals={demographicsDataStructureTotals}
                  OldestCitizen={demographicsDataOldestCitizen}
              />
          )}

          <Scrollable vertical trackVisibility="always" style={{ flex: 1 }}>
            <div className={styles.chartContainer}>
              <DemographicsChart
                  StructureDetails={demographicsDataStructureDetails}
                  groupingStrategy={groupingStrategy}
              />
            </div>
          </Scrollable>
        </div>
      </Panel>
  );
};

// Single memoized export
export default memo(Demographics);