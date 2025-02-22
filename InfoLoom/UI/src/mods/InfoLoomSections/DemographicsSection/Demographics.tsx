import React, {
  useState,
  useMemo,
  useCallback,
  FC,
  useRef,
  useEffect,
} from 'react';

// External hooks / frameworks
import { Scrollable } from 'cs2/ui';

// Chart.js imports
import { Chart, ChartOptions } from 'chart.js/auto';
import { Bar } from 'react-chartjs-2';

// Local or app-level imports
import { useValue } from 'cs2/api';
import { InfoCheckbox } from 'mods/components/InfoCheckbox/InfoCheckbox';
import {DraggablePanelProps, PanelProps, Panel} from "cs2/ui";
import {populationAtAge} from "../../domain/populationAtAge";
import {DemographicsDataDetails, DemographicsDataTotals, DemographicsDataOldestCitizen} from "../../bindings";
import styles from './Demographics.module.scss';


interface AlignedParagraphProps {
  left: string;
  right: number;
}

type GroupingStrategy = 'none' | 'fiveYear' | 'tenYear' | 'lifecycle';
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
    value: 'none',
    ranges: [],
  },
  {
    label: '5-Year Groups',
    value: 'fiveYear',
    ranges: generateRanges(5),
  },
  {
    label: '10-Year Groups',
    value: 'tenYear',
    ranges: generateRanges(10),
  },
  {
    label: 'Lifecycle Groups',
    value: 'lifecycle',
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
  // Initialize each range
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
const AlignedParagraph: FC<AlignedParagraphProps> = ({ left, right }) => {
  return (
    <div className={styles.alignedParagraph}>
      <div>{left}</div>
      <div>{right}</div>
    </div>
  );
};

/**
 * Checkbox UI for selecting a grouping strategy.
 */
const GroupingOptions: FC<GroupingOptionsProps> = ({
  groupingStrategy,
  setGroupingStrategy,
  totalEntries,
}) => {
  return (
    <div className={styles.groupingContainer}>
      <div className={styles.groupingLabel}>Age Grouping</div>
      {GROUP_STRATEGIES.map((strategy) => (
        <InfoCheckbox
          key={strategy.value}
          label={strategy.label}
          isChecked={groupingStrategy === strategy.value}
          onToggle={() => setGroupingStrategy(strategy.value)}
          count={strategy.ranges.length || totalEntries}
        />
      ))}
    </div>
  );
};

/**
 * Displays high-level statistics (Population, Tourists, etc.).
 */
const StatisticsSummary: FC<StatisticsSummaryProps> = ({
  StructureTotals,
  OldestCitizen,
}) => {
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
      <div className={styles.columnLeft}>
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
      <div className={styles.columnRight}>
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
 * Main Chart renderer for the demographics data.
 */
const DemographicsChart: FC<{
  StructureDetails: populationAtAge[];
  groupingStrategy: GroupingStrategy;
}> = ({ StructureDetails, groupingStrategy }) => {
  const chartContainerRef = useRef<HTMLDivElement>(null);
  const [containerHeight, setContainerHeight] = useState<number>(600);

  useEffect(() => {
    if (!chartContainerRef.current) return;
    const resizeObserver = new ResizeObserver((entries) => {
      if (entries[0]) {
        setContainerHeight(entries[0].contentRect.height);
      }
    });
    resizeObserver.observe(chartContainerRef.current);
    return () => resizeObserver.disconnect();
  }, []);

  // Define chart colors for each category
  const chartColors = useMemo(
    () => ({
      work: '#624532',
      elementary: '#7E9EAE',
      highSchool: '#00C217',
      college: '#005C4E',
      university: '#2462FF',
      other: '#A1A1A1',
    }),
    []
  );

  // Chart data setup
  const chartData = useMemo(() => {
    if (!StructureDetails.length) {
      return { labels: [], datasets: [] };
    }

    // If no grouping, show each age individually
    if (groupingStrategy === 'none') {
      const validDetails = StructureDetails.filter((d) => d.Age <= 120);
      const sortedAges = [...validDetails].sort((a, b) => a.Age - b.Age);
      const labels = sortedAges.map((d) => String(d.Age));

      return {
        labels,
        datasets: [
          {
            label: 'Work',
            data: sortedAges.map((d) => d.Work),
            backgroundColor: chartColors.work,
            offset: 2,
          },
          {
            label: 'Elementary',
            data: sortedAges.map((d) => d.School1),
            backgroundColor: chartColors.elementary,
            offset: 2,
          },
          {
            label: 'High School',
            data: sortedAges.map((d) => d.School2),
            backgroundColor: chartColors.highSchool,
            offset: 2,
          },
          {
            label: 'College',
            data: sortedAges.map((d) => d.School3),
            backgroundColor: chartColors.college,
            offset: 2,
          },
          {
            label: 'University',
            data: sortedAges.map((d) => d.School4),
            backgroundColor: chartColors.university,
            offset: 2,
          },
          {
            label: 'Other',
            data: sortedAges.map((d) => d.Other),
            backgroundColor: chartColors.other,
            offset: 2,
          },
        ],
      };
    }

    // If grouped, find the matching strategy and aggregate
    const selectedStrategy = GROUP_STRATEGIES.find((s) => s.value === groupingStrategy);
    if (!selectedStrategy) return { labels: [], datasets: [] };

    const aggregated = aggregatePopulationData(StructureDetails, selectedStrategy);
    const labels = aggregated.map((d) => d.label);

    return {
      labels,
      datasets: [
        {
          label: 'Work',
          data: aggregated.map((d) => d.work),
          backgroundColor: chartColors.work,
          offset: 1,
        },
        {
          label: 'Elementary',
          data: aggregated.map((d) => d.elementary),
          backgroundColor: chartColors.elementary,
          offset: 1,
        },
        {
          label: 'High School',
          data: aggregated.map((d) => d.highSchool),
          backgroundColor: chartColors.highSchool,
          offset: 1,
        },
        {
          label: 'College',
          data: aggregated.map((d) => d.college),
          backgroundColor: chartColors.college,
          offset: 1,
        },
        {
          label: 'University',
          data: aggregated.map((d) => d.university),
          backgroundColor: chartColors.university,
          offset: 1,
        },
        {
          label: 'Other',
          data: aggregated.map((d) => d.other),
          backgroundColor: chartColors.other,
          offset: 1,
        },
      ],
    };
  }, [StructureDetails, groupingStrategy, chartColors]);

  const chartOptions = useMemo(() => {
    const { indexAxis, mainAxisTitle, crossAxisTitle } = getOrientationProps();
    
    return {
      indexAxis,
      responsive: true,
      maintainAspectRatio: false,
      color: 'white',
      layout: {
        padding: {
          top: 20,
          right: 20,
          bottom: 20,
          left: 20
        }
      },
      scales: {
        y: {
          barThickness: 20,
          maxBarThickness: 25,
          barPercentage: 0.9,     
          beginAtZero: true,
          border: {
            display: true,
            color: 'rgba(255, 255, 255, 0.3)'
          },
          title: {
            display: true,
            text: mainAxisTitle,
            color: 'white',
            font: { ...yaxisfont, size: 16 },
            padding: { top: 10, bottom: 10 }
          },
          ticks: {
            color: 'white',
            font: { ...yaxisfont, size: 14 },
            padding: 15,
            maxRotation: 0
          },
          stacked: true,
          grid: {
            display: true,
            color: 'rgba(255, 255, 255, 0.1)',
            drawTicks: true
          }
        },
        x: {
          border: {
            display: true,
            color: 'rgba(255, 255, 255, 0.3)'
          },
          title: {
            display: true,
            text: crossAxisTitle,
            color: 'white',
            font: { ...commonFont, size: 16 },
            padding: { top: 10, bottom: 10 }
          },
          ticks: {
            color: 'white',
            font: { ...commonFont, size: 14 },
            padding: 15,
            callback: (value: number | string) => value
          },
          stacked: true,
          grid: {
            display: true,
            color: 'rgba(255, 255, 255, 0.1)',
            drawTicks: true
          }
        },
      },
      plugins: {
        legend: {
          position: 'top' as const,
          labels: {
            color: 'white',
            font: { ...commonFont, size: 14 },
            boxWidth: 30,
            padding: 15,
            usePointStyle: true,
            generateLabels: (chart: Chart) => {
              const datasets = chart.data.datasets;
              return datasets.map((dataset: any, i: number) => ({
                text: dataset.label || '',
                fillStyle: dataset.backgroundColor as string,
                hidden: !chart.isDatasetVisible(i),
                lineCap: 'round',
                lineDash: [],
                lineDashOffset: 0,
                lineWidth: 0,
                strokeStyle: dataset.backgroundColor as string,
                pointStyle: 'rect',
                datasetIndex: i
              }));
            }
          },
          onClick: (_event: any, legendItem: any, legend: any) => {
            const index = legendItem.datasetIndex;
            const ci = legend.chart;
            if (ci) {
              ci.setDatasetVisibility(index, !ci.isDatasetVisible(index));
              ci.update();
            }
          }
        },
        title: {
          display: false,
        },
        tooltip: {
          animation: {
            duration: 150
          },
          backgroundColor: 'rgba(0, 0, 0, 0.8)',
          titleColor: 'white',
          bodyColor: 'white',
          bodyFont: { ...commonFont },
          padding: 10,
          cornerRadius: 4,
          callbacks: {
            label: (context: any) => {
              const label = context.dataset.label || '';
              const value = context.parsed.x;
              return `${label}: ${value}`;
            }
          }
        }
      },
      animation: {
        duration: 400,
        easing: 'easeOutQuart'
      },
      transitions: {
        active: {
          animation: {
            duration: 200
          }
        }
      },
      interaction: {
        mode: 'index' as const,
        intersect: false
      }
    } as ChartOptions<'bar'>;
  }, [groupingStrategy]);

  return (
    <div 
      ref={chartContainerRef}
      style={{
        width: '100%',
        height: '100%',
        position: 'relative',
        overflowY: 'auto',
        overflowX: 'hidden',
        minHeight: '400px',
      }}
    >
      {StructureDetails.length === 0 ? (
        <p style={{ color: 'white' }}>No data available to display the chart.</p>
      ) : (
        <Bar data={chartData} options={chartOptions} />
      )}
    </div>
  );
};

// === Main Demographics Component ===
const Demographics: FC<DraggablePanelProps> = ({ onClose, initialPosition }) => {
  // Panel visibility
  const [isPanelVisible, setIsPanelVisible] = useState(true);

  // Toggles
  const [showStatistics, setShowStatistics] = useState(true);
  const [showAgeGrouping, setShowAgeGrouping] = useState(false);
  const [groupingStrategy, setGroupingStrategy] = useState<GroupingStrategy>('none');

  // Data from ECS / mod
  const demographicsDataStructureDetails = useValue(DemographicsDataDetails);
  const demographicsDataStructureTotals = useValue(DemographicsDataTotals);
  const demographicsDataOldestCitizen = useValue(DemographicsDataOldestCitizen);

  // Panel dims
  const panWidth = 800;  // Fixed width in pixels
  const panHeight = window.innerHeight * 0.8; // 80% of viewport height

  return (
    <Panel
      draggable
      onClose={onClose}
      initialPosition={initialPosition}
      className={styles.panel}
      style={{ width: panWidth, height: panHeight }}
      header={<div className={styles.header}><span className={styles.headerText}>Demographics</span></div>}
    >
      <div className={styles.contentContainer}>
        {/* Toggle checkboxes */}
        <div className={styles.toggleContainer}>
          <InfoCheckbox
            label="Show Statistics"
            isChecked={showStatistics}
            onToggle={() => setShowStatistics((prev) => !prev)}
          />
          <InfoCheckbox
            label="Show Age Grouping"
            isChecked={showAgeGrouping}
            onToggle={() => setShowAgeGrouping((prev) => !prev)}
          />
        </div>

        {/* Statistics Summary */}
        {showStatistics && (
          <StatisticsSummary
            StructureTotals={demographicsDataStructureTotals}
            OldestCitizen={demographicsDataOldestCitizen}
          />
        )}

        {/* Age Grouping Options */}
        {showAgeGrouping && (
          <GroupingOptions
            groupingStrategy={groupingStrategy}
            setGroupingStrategy={setGroupingStrategy}
            totalEntries={demographicsDataStructureDetails.length}
          />
        )}

        {/* Chart Section */}
        <div className={styles.chartSection}>
          
          <DemographicsChart
            StructureDetails={demographicsDataStructureDetails}
            groupingStrategy={groupingStrategy}
          />
          
        </div>
      </div>
  
    </Panel>
  );
};

export default Demographics;
