import React, {
  useState,
  useMemo,
  useCallback,
  FC,
  useRef,
  useEffect,
} from 'react';

// External hooks / frameworks
import $Panel from 'mods/panel';
import mod from 'mod.json';
import { ChartSettings, defaultChartSettings, ChartSettingsData } from 'mods/InfoLoomSections/DemographicsSection/ChartSettings';

// Chart.js
import { Bar, Line } from 'react-chartjs-2';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  BarElement,
  LineElement,
  PointElement,
  Tooltip,
  Legend,
  Title,
} from 'chart.js';

// Local or app-level imports
import { bindValue, useValue, trigger } from 'cs2/api';
import { InfoCheckbox } from '../../InfoCheckbox/InfoCheckbox';
import { getModule } from 'cs2/modding';

// Optional: Import lodash for debouncing
// Uncomment the following line if you decide to implement debouncing
// import { debounce } from 'lodash';

// Register Chart.js components globally
ChartJS.register(
  CategoryScale,
  LinearScale,
  BarElement,
  LineElement,
  PointElement,
  Tooltip,
  Legend,
  Title
);

// ==== Bindings (these connect to your mod's data) ====


const Population$ = bindValue<PopulationAtAge[]>(mod.id, 'StructureDetails', []);
const Totals$ = bindValue<number[]>(mod.id, 'StructureTotals', []);
const OldestCitizen$ = bindValue<number>(mod.id, 'OldestCitizen', 0);


// ==== Types & Interfaces ====
interface PopulationAtAge {
  Age: number;
  Work: number;
  School1: number;
  School2: number;
  School3: number;
  School4: number;
  Other: number;
  Total: number;
}

interface DemographicsProps {
  onClose: () => void;
  chartSettings: ChartSettingsData;
  setChartSettings: React.Dispatch<React.SetStateAction<ChartSettingsData>>;
}

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

interface DemographicsLevelProps {
  levelColor: string;
  levelName: string;
  total: number;
  levelValues: {
    work: number;
    elementary: number;
    highSchool: number;
    college: number;
    university: number;
    other: number;
    total: number;
  };
}

interface VirtualizedListProps {
  items: AggregatedInfo[];
  itemHeight?: number;
}

// === Chart Settings Data ===

// (Assuming ChartSettingsData and defaultChartSettings are defined elsewhere)

// ==== Constants & Helper Functions ====
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
 * Since we only have horizontal orientation,
 * we can hardcode the orientation props.
 */
function getOrientationProps(): {
  indexAxis: 'y';
  mainAxisTitle: string;
  crossAxisTitle: string;
  minChartHeight: string;
} {
  return {
    indexAxis: 'y',
    mainAxisTitle: 'Age in Days', // For the index axis
    crossAxisTitle: 'Number of People', // For the value axis
    minChartHeight: '400px',
  };
}

// AlignedParagraph: small summary row
const AlignedParagraph: React.FC<AlignedParagraphProps> = ({ left, right }) => (
  <div
    style={{
      width: '100%',
      padding: '0.5rem 0',
      display: 'flex',
      justifyContent: 'space-between',
      color: 'white',
      fontSize: `${commonFont.size}px`,
      fontFamily: commonFont.family,
      fontWeight: commonFont.weight,
    }}
  >
    <div style={{ textAlign: 'left' }}>{left}</div>
    <div style={{ textAlign: 'right' }}>{right}</div>
  </div>
);

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
      { label: 'Elderly', min: 84, max: 200 },
    ],
  },
];

/** Utility to generate AgeRange[] for grouping. */
function generateRanges(step: number): AgeRange[] {
  const ranges: AgeRange[] = [];
  for (let i = 0; i < 200; i += step) {
    ranges.push({
      label: i === 0 ? `0-${step}` : i + step >= 200 ? `${i}-200` : `${i}-${i + step}`,
      min: i,
      max: i + step,
    });
  }
  return ranges;
}

const GroupingOptions: React.FC<{
  groupingStrategy: GroupingStrategy;
  setGroupingStrategy: React.Dispatch<React.SetStateAction<GroupingStrategy>>;
  totalEntries: number;
}> = ({ groupingStrategy, setGroupingStrategy, totalEntries }) => {
  return (
    <div
      style={{
        display: 'flex',
        flexDirection: 'column',
        gap: '0.3rem',
        padding: '1rem',
        backgroundColor: 'rgba(0, 0, 0, 0.2)',
        borderRadius: '4px',
        margin: '0 1rem',
      }}
    >
      <div style={{ color: 'white', marginBottom: '0.3rem', fontSize: '14px' }}>
        Age Grouping
      </div>
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

const DemographicsLevel: React.FC<DemographicsLevelProps> = ({
  levelColor,
  levelName,
  total,
  levelValues,
}) => (
  <div style={{ width: '99%', padding: '1rem 0', backgroundColor: levelColor }}>
    <div style={{ width: '1%' }} />
    <div style={{ display: 'flex', alignItems: 'center', width: '22%' }}>
      <div
        style={{
          backgroundColor: levelColor,
          width: '1.2em',
          height: '1.2em',
          marginRight: '0.5rem',
          borderRadius: '50%',
        }}
      />
      <div>{levelName}</div>
    </div>
    <div style={{ width: '11%', textAlign: 'center' }}>{total}</div>
    <div style={{ width: '11%', textAlign: 'center' }}>{levelValues.work}</div>
    <div style={{ width: '12%', textAlign: 'center' }}>{levelValues.elementary}</div>
    <div style={{ width: '12%', textAlign: 'center' }}>{levelValues.highSchool}</div>
    <div style={{ width: '12%', textAlign: 'center' }}>{levelValues.college}</div>
    <div style={{ width: '12%', textAlign: 'center' }}>{levelValues.university}</div>
    <div style={{ width: '12%', textAlign: 'center' }}>{levelValues.other}</div>
  </div>
);

// DetailedStatistics
const DetailedStatistics: React.FC<{
  StructureDetails: PopulationAtAge[];
  StructureTotals: number[];
}> = ({ StructureDetails, StructureTotals }) => {
  const totalPopulation = StructureTotals[1];

  const stats = useMemo(() => {
    if (!StructureDetails || StructureDetails.length === 0) return null;

    const workingPopulation = StructureTotals[5];
    const studentPopulation = StructureTotals[4];

    const childCount = StructureDetails.filter((d) => d.Age <= 20).length;
    const teenCount = StructureDetails.filter((d) => d.Age > 20 && d.Age <= 35).length;
    const adultCount = StructureDetails.filter((d) => d.Age > 35 && d.Age <= 83).length;
    const elderlyCount = StructureDetails.filter((d) => d.Age > 83).length;

    const toPercent = (value: number) => ((value / totalPopulation) * 100).toFixed(1);

    return {
      populationDensity: {
        workers: toPercent(workingPopulation),
        students: toPercent(studentPopulation),
        homeless: toPercent(StructureTotals[9]),
        tourists: toPercent(StructureTotals[2]),
      },
      ageDistribution: {
        child: toPercent(childCount),
        teen: toPercent(teenCount),
        adult: toPercent(adultCount),
        elderly: toPercent(elderlyCount),
      },
      averageAge: (
        StructureDetails.reduce((acc, d) => acc + d.Age, 0) / StructureDetails.length
      ).toFixed(1),
    };
  }, [StructureDetails, StructureTotals, totalPopulation]);

  if (!stats) return null;

  return (
    <div
      style={{
        backgroundColor: 'rgba(0, 0, 0, 0.2)',
        padding: '1rem',
        borderRadius: '4px',
        margin: '1rem',
        fontSize: '13px',
        color: 'white',
      }}
    >
      <h4 style={{ marginBottom: '0.5rem', borderBottom: '1px solid rgba(255,255,255,0.2)' }}>
        Detailed Statistics
      </h4>
      <p>Workers: {stats.populationDensity.workers}%</p>
      <p>Students: {stats.populationDensity.students}%</p>
      <p>Tourists: {stats.populationDensity.tourists}%</p>
      <p>Homeless: {stats.populationDensity.homeless}%</p>

      <p>Children: {stats.ageDistribution.child}%</p>
      <p>Youth: {stats.ageDistribution.teen}%</p>
      <p>Adults: {stats.ageDistribution.adult}%</p>
      <p>Elderly: {stats.ageDistribution.elderly}%</p>

      <p>Average Age: {stats.averageAge} days</p>
    </div>
  );
};

// Define the prop types for FloatSliderField
interface FloatSliderFieldProps {
  value: number;
  min: number;
  max: number;
  step: number;
  onChange: (value: number) => void;
  fractionDigits?: number;
  // Add other props as needed based on actual implementation
}

// Main Demographics Component
const Demographics: FC<DemographicsProps> = ({
  onClose,
  chartSettings,
  setChartSettings,
}) => {
  const [showChartSettings, setShowChartSettings] = useState(false);

  // We only have horizontal orientation now
  const chartOrientation: ChartOrientation = 'horizontal';

  const [isPanelVisible, setIsPanelVisible] = useState(true);
  const [groupingStrategy, setGroupingStrategy] = useState<GroupingStrategy>('none');

  const [showAgeGrouping, setShowAgeGrouping] = useState(true);
  const [showStatistics, setShowStatistics] = useState(true);
  const [showDetailedStats, setShowDetailedStats] = useState(false);

  // Consolidated chart settings in one object:

  const chartContainerRef = useRef<HTMLDivElement>(null);
  const [containerHeight, setContainerHeight] = useState<number>(600);

  // Data from ECS / mod
  const StructureDetails = useValue(Population$);
  const StructureTotals = useValue(Totals$);
  const OldestCitizen = useValue(OldestCitizen$);
  

  

  // Synchronize localAgeCap with external AgeCap$
  

  // Optional: Debounced trigger
  // Uncomment the following lines if you wish to implement debouncing
  /*
  const debounceTriggerRef = useRef(
    debounce((value: number) => {
      trigger(mod.id, 'SetAgeCap', value);
    }, 300) // Adjust the delay as needed
  );

  const handleSliderChange = (newValue: number) => {
    const clampedValue = Math.max(100, Math.min(200, newValue));
    setLocalAgeCap(clampedValue);
    debounceTriggerRef.current(clampedValue);
  };
  */

  // Handle slider change without debouncing
  

  // Panel dims
  const panWidth = window.innerWidth * 0.2;
  const panHeight = window.innerHeight * 0.86;

  const BAR_HEIGHT = 40;
  const MAX_CHART_HEIGHT = 1200;

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

  const handleClose = useCallback(() => {
    onClose();
  }, [onClose]);

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

  // Chart data
  const chartData = useMemo(() => {
    if (!StructureDetails.length) {
      return { labels: [], datasets: [] };
    }

    if (groupingStrategy === 'none') {
      const validDetails = StructureDetails.filter((d) => d.Age <= 200);
      const sortedAges = [...validDetails].sort((a, b) => a.Age - b.Age);
      const labels = sortedAges.map((d) => String(d.Age));

      return {
        labels,
        datasets: [
          {
            label: 'Work',
            data: sortedAges.map((d) => d.Work),
            backgroundColor: chartColors.work,
            borderColor: chartColors.work,
            borderWidth: 1,
          },
          {
            label: 'Elementary',
            data: sortedAges.map((d) => d.School1),
            backgroundColor: chartColors.elementary,
            borderColor: chartColors.elementary,
            borderWidth: 1,
          },
          {
            label: 'High School',
            data: sortedAges.map((d) => d.School2),
            backgroundColor: chartColors.highSchool,
            borderColor: chartColors.highSchool,
            borderWidth: 1,
          },
          {
            label: 'College',
            data: sortedAges.map((d) => d.School3),
            backgroundColor: chartColors.college,
            borderColor: chartColors.college,
            borderWidth: 1,
          },
          {
            label: 'University',
            data: sortedAges.map((d) => d.School4),
            backgroundColor: chartColors.university,
            borderColor: chartColors.university,
            borderWidth: 1,
          },
          {
            label: 'Other',
            data: sortedAges.map((d) => d.Other),
            backgroundColor: chartColors.other,
            borderColor: chartColors.other,
            borderWidth: 1,
          },
        ],
      };
    }

    // Grouped
    const selectedStrategy = GROUP_STRATEGIES.find((s) => s.value === groupingStrategy);
    if (!selectedStrategy) return { labels: [], datasets: [] };

    const aggregated = selectedStrategy.ranges.map((range) => ({
      label: range.label,
      work: 0,
      elementary: 0,
      highSchool: 0,
      college: 0,
      university: 0,
      other: 0,
      total: 0,
    }));

    StructureDetails.forEach((info) => {
      if (info.Age > 200) return;
      const idx = selectedStrategy.ranges.findIndex(
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

    const labels = aggregated.map((d) => d.label);
    return {
      labels,
      datasets: [
        {
          label: 'Work',
          data: aggregated.map((d) => d.work),
          backgroundColor: chartColors.work,
          borderColor: chartColors.work,
          borderWidth: 1,
        },
        {
          label: 'Elementary',
          data: aggregated.map((d) => d.elementary),
          backgroundColor: chartColors.elementary,
          borderColor: chartColors.elementary,
          borderWidth: 1,
        },
        {
          label: 'High School',
          data: aggregated.map((d) => d.highSchool),
          backgroundColor: chartColors.highSchool,
          borderColor: chartColors.highSchool,
          borderWidth: 1,
        },
        {
          label: 'College',
          data: aggregated.map((d) => d.college),
          backgroundColor: chartColors.college,
          borderColor: chartColors.college,
          borderWidth: 1,
        },
        {
          label: 'University',
          data: aggregated.map((d) => d.university),
          backgroundColor: chartColors.university,
          borderColor: chartColors.university,
          borderWidth: 1,
        },
        {
          label: 'Other',
          data: aggregated.map((d) => d.other),
          backgroundColor: chartColors.other,
          borderColor: chartColors.other,
          borderWidth: 1,
        },
      ],
    };
  }, [StructureDetails, groupingStrategy, 200, chartColors]);

  // Chart options
  const chartOptions = useMemo(() => {
    const { indexAxis, mainAxisTitle, crossAxisTitle } = getOrientationProps();

    return {
      indexAxis, // always 'y'
      responsive: true,
      maintainAspectRatio: false,
      animation: {
        duration: chartSettings.enableAnimation ? 750 : 0,
      },
      plugins: {
        title: {
          display: true,
          text: 'Population Structure',
          color: 'white',
          font: commonFont,
        },
        legend: {
          position: chartSettings.legendPosition,
          labels: {
            color: 'white',
            font: commonFont,
          },
        },
      },
      scales: {
        y: {
          stacked: chartSettings.stackedView,
          title: {
            display: true,
            text: mainAxisTitle, // "Age in Days"
            color: 'white',
            font: commonFont,
          },
          // Move 'max' here
          max: 200, // Updated to use localAgeCap
          ticks: {
            color: 'white',
            font: { ...commonFont, size: commonFont.size - 4 },
            stepSize: groupingStrategy !== 'none' ? 1 : 5,
            padding: 10,
            maxTicksLimit: 200, // Updated to use localAgeCap
          },
          grid: {
            display: chartSettings.showGridLines,
            color: 'rgba(255, 255, 255, 0.1)',
          },
        },
        x: {
          stacked: chartSettings.stackedView,
          beginAtZero: true,
          title: {
            display: true,
            text: crossAxisTitle, // "Number of People"
            color: 'white',
            font: commonFont,
          },
          ticks: {
            color: 'white',
            font: { ...commonFont, size: commonFont.size - 4 },
          },
          grid: {
            display: chartSettings.showGridLines,
            color: 'rgba(255, 255, 255, 0.1)',
          },
          barPercentage: 0.95,
          categoryPercentage: 0.9,
        },
      },
      layout: {
        padding: {
          top: 20,
          bottom: 20,
          left: 10,
          right: 10,
        },
      },
    };
  }, [chartSettings, groupingStrategy, 200]);

  // Render chart
  const renderChart = () => {
    const { minChartHeight } = getOrientationProps();

    return (
      <div
        style={{
          height: '100%',
          width: '100%',
          position: 'relative',
          overflowY: 'auto',
          overflowX: 'hidden',
          minHeight: minChartHeight,
        }}
        ref={chartContainerRef}
      >
        {chartSettings.chartType === 'bar' ? (
          <Bar data={chartData} options={chartOptions} />
        ) : (
          <Line data={chartData} options={chartOptions} />
        )}
      </div>
    );
  };

  const panelStyle = useMemo(
    () => ({
      backgroundColor: 'var(--panelColorNormal)',
      display: 'flex',
      flexDirection: 'column' as const,
      overflow: 'hidden',
      margin: '3rem',
    }),
    []
  );

  if (!isPanelVisible) return null;

  // final chart height
  const chartHeightToUse = Math.min(MAX_CHART_HEIGHT, StructureDetails.length * BAR_HEIGHT);

  return (
    <$Panel
      id="infoloom-demographics"
      onClose={handleClose}
      title="Demographics"
      initialSize={{ width: panWidth, height: panHeight }}
      initialPosition={{
        top: window.innerHeight * 0.009,
        left: window.innerWidth * 0.053,
      }}
      style={panelStyle}
    >
      <div
        style={{
          padding: '10px',
          height: '100%',
          display: 'flex',
          flexDirection: 'column',
          overflow: 'hidden',
        }}
      >
        {/* Top checkboxes: toggles for statistics, age grouping, chart settings */}
        <div style={{ display: 'flex', justifyContent: 'flex-end', padding: '0.5rem 1rem' }}>
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
          <InfoCheckbox
            label="Show Chart Settings"
            isChecked={showChartSettings}
            onToggle={() => setShowChartSettings((prev) => !prev)}
          />
        </div>

        
        

        {/* Statistics Summary */}
        {showStatistics && (
            <div
                style={{
                  flex: '0 0 auto',
                  display: 'flex',
                  flexDirection: 'row',
                  width: '100%',
                  flexShrink: 0,
                }}
            >
              <div style={{width: '50%', paddingRight: '4rem'}}>
                <AlignedParagraph left="All Citizens" right={StructureTotals[0]}/>
                <div style={{height: '1rem'}}/>
                <AlignedParagraph left="- Tourists" right={StructureTotals[2]}/>
                <div style={{ height: '1rem' }} />
              <AlignedParagraph left="- Commuters" right={StructureTotals[3]} />
              <div style={{ height: '1rem' }} />
              <AlignedParagraph left="- Moving Away" right={StructureTotals[7]} />
              <div style={{ height: '1rem' }} />
              <AlignedParagraph left="Population" right={StructureTotals[1]} />
            </div>
            <div style={{ width: '50%', paddingLeft: '4rem' }}>
              <AlignedParagraph left="Dead" right={StructureTotals[8]} />
              <div style={{ height: '1rem' }} />
              <AlignedParagraph left="Students" right={StructureTotals[4]} />
              <div style={{ height: '1rem' }} />
              <AlignedParagraph left="Workers" right={StructureTotals[5]} />
              <div style={{ height: '1rem' }} />
              <AlignedParagraph left="Homeless" right={StructureTotals[9]} />
              <div style={{ height: '1rem' }} />
              <AlignedParagraph left="Oldest Citizen" right={OldestCitizen} />
            </div>
          </div>
        )}

        {/* Age Grouping section */}
        {showAgeGrouping && (
          <div
            style={{
              flex: '0 0 auto',
              display: 'flex',
              flexDirection: 'column',
              margin: '1rem',
              flexShrink: 0,
            }}
          >
            <GroupingOptions
              groupingStrategy={groupingStrategy}
              setGroupingStrategy={setGroupingStrategy}
              totalEntries={StructureDetails.length}
            />
          </div>
        )}

        {/* Detailed Statistics (Optional) */}
        {showDetailedStats && (
          <DetailedStatistics
            StructureDetails={StructureDetails}
            StructureTotals={StructureTotals}
          />
        )}

        {/* Chart Section */}
        <div
          style={{
            flex: '1 1 auto',
            minHeight: 0,
            padding: '1rem',
            position: 'relative',
            overflowY: 'auto',
            overflowX: 'hidden',
          }}
        >
          {StructureDetails.length === 0 ? (
            <p style={{ color: 'white' }}>No data available to display the chart.</p>
          ) : (
            <div
              style={{
                height: `${chartHeightToUse}px`,
                width: '100%',
                position: 'relative',
              }}
            >
              {renderChart()}
            </div>
          )}
        </div>
      </div>
    </$Panel>
  );
};

export default Demographics;
