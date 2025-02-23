import React, {
  useState,
  useMemo,
  useCallback,
  useRef,
  useEffect,
} from 'react';
import styles from './Demographics.module.scss';

// External hooks / frameworks
import $Panel from 'mods/panel';

// Chart.js
import { Bar } from 'react-chartjs-2';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  BarElement,
  Tooltip,
  Legend,
  Title,
} from 'chart.js';

// Local or app-level imports
import { bindValue, useValue } from 'cs2/api';
import { InfoCheckbox } from 'mods/components/InfoCheckbox/InfoCheckbox';
import {DraggablePanelProps, PanelProps, PanelTheme, Panel, Scrollable} from "cs2/ui";
import {populationAtAge} from "../../domain/populationAtAge";
import {DemographicsDataDetails, DemographicsDataTotals, DemographicsDataOldestCitizen} from "../../bindings";

// Register Chart.js components
ChartJS.register(
  CategoryScale,
  LinearScale,
  BarElement,
  Tooltip,
  Legend,
  Title
);

// ==== Bindings (connect to your mod's data) ====







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
const GroupingOptions = ({
  groupingStrategy,
  setGroupingStrategy,
  totalEntries,
}: GroupingOptionsProps): JSX.Element => {
  return (
    <div className={styles.groupingOptions}>
      <div className={styles.label}>Age Grouping</div>
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
 * Main Chart renderer for the demographics data.
 */
const DemographicsChart = ({
  StructureDetails,
  groupingStrategy,
}: {
  StructureDetails: populationAtAge[];
  groupingStrategy: GroupingStrategy;
}): JSX.Element => {
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

  // Build chart data
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
          },
          {
            label: 'Elementary',
            data: sortedAges.map((d) => d.School1),
            backgroundColor: chartColors.elementary,
          },
          {
            label: 'High School',
            data: sortedAges.map((d) => d.School2),
            backgroundColor: chartColors.highSchool,
          },
          {
            label: 'College',
            data: sortedAges.map((d) => d.School3),
            backgroundColor: chartColors.college,
          },
          {
            label: 'University',
            data: sortedAges.map((d) => d.School4),
            backgroundColor: chartColors.university,
          },
          {
            label: 'Other',
            data: sortedAges.map((d) => d.Other),
            backgroundColor: chartColors.other,
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
        },
        {
          label: 'Elementary',
          data: aggregated.map((d) => d.elementary),
          backgroundColor: chartColors.elementary,
        },
        {
          label: 'High School',
          data: aggregated.map((d) => d.highSchool),
          backgroundColor: chartColors.highSchool,
        },
        {
          label: 'College',
          data: aggregated.map((d) => d.college),
          backgroundColor: chartColors.college,
        },
        {
          label: 'University',
          data: aggregated.map((d) => d.university),
          backgroundColor: chartColors.university,
        },
        {
          label: 'Other',
          data: aggregated.map((d) => d.other),
          backgroundColor: chartColors.other,
        },
      ],
    };
  }, [StructureDetails, groupingStrategy, chartColors]);

  // Define chart options
  const chartOptions = useMemo(() => {
    const { indexAxis, mainAxisTitle, crossAxisTitle } = getOrientationProps();
    return {
      indexAxis: indexAxis,
      responsive: true,
      maintainAspectRatio: false,
      scales: {
        [indexAxis]: {
          title: {
            display: true,
            text: mainAxisTitle,
            color: 'white',
            font: yaxisfont,
          },
          ticks: {
            color: 'white',
            font: yaxisfont,
          },
          stacked: true,
        },
        x: {
          title: {
            display: true,
            text: crossAxisTitle,
            color: 'white',
            font: commonFont,
          },
          ticks: {
            color: 'white',
            font: commonFont,
          },
          stacked: true,
        },
      },
      plugins: {
        legend: {
          labels: {
            color: 'white',
            font: commonFont,
          },
        },
        title: {
          display: false,
        },
      },
    };
  }, []);

  return (
    <div 
      className={styles.chartVisualization}
      style={{ height: '1500px' }}
    >
      {StructureDetails.length === 0 ? (
        <p className={styles.noData}>No data available to display the chart.</p>
      ) : (
        <Bar data={chartData} options={chartOptions} />
      )}
    </div>
  );
};

// === Main Demographics Component ===
const Demographics = ({ onClose }: DraggablePanelProps): JSX.Element => {
  // Toggles
  const [showStatistics, setShowStatistics] = useState(true);
  const [showAgeGrouping, setShowAgeGrouping] = useState(true);

  // Grouping
  const [groupingStrategy, setGroupingStrategy] = useState<GroupingStrategy>('none');

  // Data from ECS / mod
  const demographicsDataStructureDetails = useValue(DemographicsDataDetails);
  const demographicsDataStructureTotals = useValue(DemographicsDataTotals);
  const demographicsDataOldestCitizen = useValue(DemographicsDataOldestCitizen);

  return (
    <Panel
      draggable
      onClose={onClose}
      className={styles.panel}
      header={
        <div className={styles.header}>
          <span className={styles.headerText}>Demographics</span>
        </div>
      }
    >
      <div className={styles.container}>
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
        <Scrollable smooth vertical trackVisibility="scrollable">
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

export default Demographics;
