import React, { useState, useMemo, KeyboardEvent, useCallback, FC, useRef, useEffect } from 'react';
import useDataUpdate from 'mods/use-data-update';
import $Panel from 'mods/panel';
import mod from "mod.json";

// Import Chart.js components
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
import { bindValue, useValue } from 'cs2/api';
import { InfoCheckbox } from "../../InfoCheckbox/InfoCheckbox";

// Register Chart.js components
ChartJS.register(CategoryScale, LinearScale, BarElement, Tooltip, Legend, Title);
const AgeCap$ = bindValue<number>(mod.id, 'AgeCap');
// Define interfaces for component props
interface AlignedParagraphProps {
  left: string;
  right: number;
}

interface Info {
  age: number;
  total: number;
  work: number;
  school1: number; // Elementary
  school2: number; // High School
  school3: number; // College
  school4: number; // University
  other: number;
}

// Define aggregated info interface
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

// Define a common font configuration
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
// Define age ranges as a constant
const AGE_RANGES = [
  { label: '0-5', min: 0, max: 5 },
  { label: '6-10', min: 6, max: 10 },
  { label: '11-15', min: 11, max: 15 },
  { label: '16-20', min: 16, max: 20 },
  { label: '21-25', min: 21, max: 25 },
  { label: '26-30', min: 26, max: 30 },
  { label: '31-35', min: 31, max: 35 },
  { label: '36-40', min: 36, max: 40 },
  { label: '41-45', min: 41, max: 45 },
  { label: '46-50', min: 46, max: 50 },
  { label: '51-55', min: 51, max: 55 },
  { label: '56-60', min: 56, max: 60 },
  { label: '61-65', min: 61, max: 65 },
  { label: '66-70', min: 66, max: 70 },
  { label: '71-75', min: 71, max: 75 },
  { label: '76-80', min: 76, max: 80 },
  { label: '81-85', min: 81, max: 85 },
  { label: '86-90', min: 86, max: 90 },
  { label: '91-95', min: 91, max: 95 },
  { label: '96-100', min: 96, max: 100 },
  { label: '101-105', min: 101, max: 105 },
  { label: '106-110', min: 106, max: 110 },
  { label: '111-115', min: 111, max: 115 },
  { label: '116-120', min: 116, max: 120 },
  { label: '121-125', min: 121, max: 125 },
  { label: '126-130', min: 126, max: 130 },
  { label: '131-135', min: 131, max: 135 },
  { label: '136-140', min: 136, max: 140 },
  { label: '141-145', min: 141, max: 145 },
  { label: '146-150', min: 146, max: 150 },
  { label: '151-155', min: 151, max: 155 },
  { label: '156-160', min: 156, max: 160 },
  { label: '161-165', min: 161, max: 165 },
  { label: '166-170', min: 166, max: 170 },
  { label: '171-175', min: 171, max: 175 },
  { label: '176-180', min: 176, max: 180 },
  { label: '181-185', min: 181, max: 185 },
  { label: '186-190', min: 186, max: 190 },
  { label: '191-195', min: 191, max: 195 },
  { label: '196-200', min: 196, max: 200 },
  

  
];

// Optimized aggregation function
const aggregateDataByAgeRanges = (details: Info[]): AggregatedInfo[] => {
  const aggregated = AGE_RANGES.map(range => ({
    label: range.label,
    work: 0,
    elementary: 0,
    highSchool: 0,
    college: 0,
    university: 0,
    other: 0,
    total: 0,
  }));

  details.forEach(info => {
    const index = AGE_RANGES.findIndex(
      range =>
        info.age >= range.min &&
        (info.age < range.max || (info.age === range.max && range.max === AgeCap$.value))
    );

    if (index !== -1) {
      const agg = aggregated[index];
      agg.work += info.work;
      agg.elementary += info.school1;
      agg.highSchool += info.school2;
      agg.college += info.school3;
      agg.university += info.school4;
      agg.other += info.other;
      agg.total += info.total;
    }
  });

  return aggregated;
};

// Optimized function to group details by individual age
const groupDetailsByAge = (details: Info[]): AggregatedInfo[] => {
  const grouped = details.reduce<Record<number, AggregatedInfo>>((acc, info) => {
    const age = info.age;
    if (!acc[age]) {
      acc[age] = {
        label: `${age}`,
        work: 0,
        elementary: 0,
        highSchool: 0,
        college: 0,
        university: 0,
        other: 0,
        total: 0,
      };
    }
    const agg = acc[age];
    agg.work += info.work;
    agg.elementary += info.school1;
    agg.highSchool += info.school2;
    agg.college += info.school3;
    agg.university += info.school4;
    agg.other += info.other;
    agg.total += info.total;
    return acc;
  }, {});

  return Object.values(grouped);
};

// AlignedParagraph Component for Summary
const AlignedParagraph: React.FC<AlignedParagraphProps> = ({ left, right }) => (
  <div
    className="labels_L7Q row_S2v"
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

// DemographicsLevel Component
interface DemographicsLevelProps {
  levelColor: string;
  levelName: string;
  levelValues: {
    work: number;
    elementary: number;
    highSchool: number;
    college: number;
    university: number;
    other: number;
    total: number;
  };
  total: number;
}

const DemographicsLevel: React.FC<DemographicsLevelProps> = ({ levelColor, levelName, levelValues, total }) => (
  <div
    className="labels_L7Q row_S2v"
    style={{ width: '99%', padding: '1rem 0', backgroundColor: levelColor }}
  >
    <div style={{ width: '1%' }}></div>
    <div style={{ display: 'flex', alignItems: 'center', width: '22%' }}>
      <div
        className="symbol_aAH"
        style={{
          backgroundColor: levelColor,
          width: '1.2em',
          height: '1.2em',
          marginRight: '0.5rem',
          borderRadius: '50%',
        }}
      ></div>
      <div>{levelName}</div>
    </div>
    <div className="row_S2v" style={{ width: '11%', justifyContent: 'center' }}>
      {total}
    </div>
    <div className="row_S2v" style={{ width: '11%', justifyContent: 'center' }}>
      {levelValues.work}
    </div>
    <div className="row_S2v" style={{ width: '12%', justifyContent: 'center' }}>
      {levelValues.elementary}
    </div>
    <div className="row_S2v" style={{ width: '12%', justifyContent: 'center' }}>
      {levelValues.highSchool}
    </div>
    <div className="row_S2v" style={{ width: '12%', justifyContent: 'center' }}>
      {levelValues.college}
    </div>
    <div className="row_S2v" style={{ width: '12%', justifyContent: 'center' }}>
      {levelValues.university}
    </div>
    <div className="row_S2v" style={{ width: '12%', justifyContent: 'center' }}>
      {levelValues.other}
    </div>
  </div>
);

interface DemographicsProps {
  onClose: () => void;
}

const Demographics: FC<DemographicsProps> = ({ onClose }) => {
  // State hooks for totals and details
  const [totals, setTotals] = useState<number[]>([]);
  const [details, setDetails] = useState<Info[]>([]);
  const AgeCap = useValue(AgeCap$);
  // State hooks for grouping and summary statistics visibility
  const [isGrouped, setIsGrouped] = useState<boolean>(false);
  const [showSummaryStats, setShowSummaryStats] = useState<boolean>(false);
  const [groupingStrategy, setGroupingStrategy] = useState<'none' | 'fiveYear' | 'tenYear' | 'custom' | 'lifecycle'>('none');

  // Fetch totals data using useDataUpdate hook
  useDataUpdate('populationInfo.structureTotals', data => setTotals(data || []));

  // Fetch details data using useDataUpdate hook
  useDataUpdate('populationInfo.structureDetails', data => setDetails(data || []));

  // Panel dimensions
  const panWidth = window.innerWidth * 0.2;
  const panHeight = window.innerHeight * 0.86;

  // Define per-bar height and maximum chart height
  const BAR_HEIGHT = 40;
  const MAX_CHART_HEIGHT = 1200;

  // Memoized chart colors
  const chartColors = {
    work: '#624532',
    elementary: '#7E9EAE',
    highSchool: '#00C217',
    college: '#005C4E',
    university: '#2462FF',
    other: '#A1A1A1',
  } as const;

  // Memoized dataset configuration
  const createDatasetConfig = useMemo(() => ({
    work: { label: 'Work', backgroundColor: chartColors.work },
    elementary: { label: 'Elementary', backgroundColor: chartColors.elementary },
    highSchool: { label: 'High School', backgroundColor: chartColors.highSchool },
    college: { label: 'College', backgroundColor: chartColors.college },
    university: { label: 'University', backgroundColor: chartColors.university },
    other: { label: 'Other', backgroundColor: chartColors.other },
  }), []);

  // Prepare detailed data for Chart.js with grouping
  const detailedChartData = useMemo(() => {
    // Early return if no details
    if (!details.length) return { labels: [], datasets: [] };

    const validDetails = details.filter(detail => detail.age <= AgeCap);
    const sortedAges = validDetails.sort((a, b) => a.age - b.age);
  
    const labels = sortedAges.map(data => String(data.age));
  
    const datasets = Object.entries(createDatasetConfig).map(([key, config]) => ({
      ...config,
      data: sortedAges.map(data => 
        key === 'work' ? data.work :
        key === 'elementary' ? data.school1 :
        key === 'highSchool' ? data.school2 :
        key === 'college' ? data.school3 :
        key === 'university' ? data.school4 :
        data.other
      )
    }));

    return { labels, datasets };
  }, [details, AgeCap, createDatasetConfig]);

  // Prepare grouped data for Chart.js with optimized processing
  const groupedChartData = useMemo(() => {
    // Early return if no details
    if (!details.length) return { labels: [], datasets: [] };

    const aggregated = aggregateDataByAgeRanges(details);
    const labels = aggregated.map(data => data.label);
  
    const datasets = Object.entries(createDatasetConfig).map(([key, config]) => ({
      ...config,
      data: aggregated.map(data => 
        key === 'work' ? data.work :
        key === 'elementary' ? data.elementary :
        key === 'highSchool' ? data.highSchool :
        key === 'college' ? data.college :
        key === 'university' ? data.university :
        data.other
      )
    }));

    return { labels, datasets };
  }, [details, createDatasetConfig]);

  // Optimized dynamic font size calculation
  const dynamicFontConfig = useMemo(() => {
    const baseFontSize = 8;
    const fontSizeMultiplier = Math.max(0.5, 50 / AgeCap);
    return {
      size: Math.max(8, Math.floor(baseFontSize * fontSizeMultiplier))
    };
  }, [AgeCap]);

  // Debounced chart height calculation
  const chartHeight = useMemo(() => {
    if (!details.length) return MAX_CHART_HEIGHT;

    const baseHeight = 50;
    const heightMultiplier = Math.max(1, AgeCap / 50);
    const dataLength = isGrouped ? AGE_RANGES.length : details.length;
    return Math.min(dataLength * baseHeight * heightMultiplier, MAX_CHART_HEIGHT);
  }, [isGrouped, details.length, AgeCap]);

  // Chart options with aligned font settings
  const chartOptions = useMemo(
    () => ({
      
      indexAxis: 'y' as const,
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        title: {
          display: true,
          text: 'Population Structure',
          color: 'white',
          font: commonFont,
        },
        legend: {
          labels: {
            color: 'white',
            font: commonFont,
          },
        },
      },
      scales: {
        x: {
          stacked: true,
          title: {
            display: true,
            text: 'Number of People',
            color: 'white',
            font: commonFont,
          },
          ticks: {
            color: 'white',
            font: { ...commonFont, size: commonFont.size - 4 },

          },
          grid: {
            color: 'rgba(255, 255, 255, 0.1)',
          },
        },
        y: {
          stacked: true,
          beginAtZero: true,
          max: AgeCap,
          afterFit: (scaleInstance: { height: number }) => {
            // Dynamically calculate spacing multiplier based on AgeCap
            const baseMultiplier = 1;
            const spacingMultiplier = Math.max(baseMultiplier, AgeCap / 50); // Adjust divisor (50) to tune sensitivity
            scaleInstance.height = scaleInstance.height * spacingMultiplier;
          },
          title: {
            display: true,
            text: isGrouped ? 'Age Groups in Days' : 'Age in Days',
            color: 'white',
            font: commonFont,
          },
          ticks: {
            color: 'white',
            font: { ...yaxisfont, size: yaxisfont.size},
            baseFontSize: yaxisfont.size,
            fontSizeMultiplier: Math.max(0.5, 50 / AgeCap), // Inverse relationship: larger AgeCap = smaller font
            dynamicFontSize: Math.max(8, Math.floor(dynamicFontConfig.size * (50 / AgeCap))), // Never go below 8px
            autoSkip: false,
            stepSize: isGrouped ? 1 : 5,
            padding: 10, 
          },
          grid: {
            color: 'rgba(255, 255, 255, 0.1)',
          },
        },
      },
    }),
    [isGrouped, AgeCap]
  );

  // Choose chart data based on isGrouped
  const chartDataToUse = isGrouped ? groupedChartData : detailedChartData;

  // Calculate dynamic chart height with a new maximum limit
  const chartHeightToUse = chartHeight;

  // Calculate detailed summary statistics per age or age group
  const detailedSummaryStats = useMemo(() => {
    return isGrouped ? aggregateDataByAgeRanges(details) : groupDetailsByAge(details);
  }, [details, isGrouped]);

  // Optimized toggle handlers using useCallback
  const toggleGrouped = useCallback(() => {
    setIsGrouped(prev => !prev);
  }, []);

  const toggleSummaryStats = useCallback(() => {
    setShowSummaryStats(prev => !prev);
  }, []);

  const handleResetData = useCallback(() => {
    setTotals([]);
    setDetails([]);
  }, []);

  // Optimized key press handler
  const handleToggleKeyPress = useCallback((
    e: KeyboardEvent<HTMLButtonElement>,
    toggleFunction: () => void
  ) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      toggleFunction();
    }
  }, []);

  // Memoized panel style
  const panelStyle = useMemo(() => ({
    backgroundColor: 'var(--panelColorNormal)',
    display: 'flex',
    flexDirection: 'column' as const,
    overflow: 'hidden',
    margin: '3rem',
  }), []);

  // Memoized button styles
  const buttonStyle = useMemo(() => ({
    base: {
      padding: '0.5rem 1rem',
      color: 'white',
      border: 'none',
      borderRadius: '4px',
      cursor: 'pointer',
      fontSize: '14px',
      margin: '3rem',
    },
    normal: {
      backgroundColor: '#34495e',
    },
    reset: {
      backgroundColor: '#e74c3c',
    }
  }), []);

  // Virtualized summary stats list
  interface VirtualizedListProps {
    items: AggregatedInfo[];
    itemHeight?: number;
  }

  const VirtualizedSummaryList: FC<VirtualizedListProps> = useCallback(({ items, itemHeight = 40 }) => {
    const containerRef = useRef<HTMLDivElement>(null);
    const [visibleRange, setVisibleRange] = useState({ start: 0, end: 20 });

    const handleScroll = useCallback(() => {
      if (!containerRef.current) return;
      
      const { scrollTop, clientHeight } = containerRef.current;
      const start = Math.floor(scrollTop / itemHeight);
      const end = Math.min(
        start + Math.ceil(clientHeight / itemHeight) + 1,
        items.length
      );
      
      setVisibleRange({ start, end });
    }, [items.length, itemHeight]);

    useEffect(() => {
      const container = containerRef.current;
      if (!container) return;

      container.addEventListener('scroll', handleScroll);
      handleScroll();

      return () => container.removeEventListener('scroll', handleScroll);
    }, [handleScroll]);

    const visibleItems = items.slice(visibleRange.start, visibleRange.end);
    const totalHeight = items.length * itemHeight;
    const offsetY = visibleRange.start * itemHeight;

    return (
      <div
        ref={containerRef}
        style={{
          overflowY: 'auto',
          maxHeight: '250px',
          paddingRight: '10px',
          position: 'relative'
        }}
      >
        <div style={{ height: totalHeight, position: 'relative' }}>
          <div style={{ transform: `translateY(${offsetY}px)` }}>
            {visibleItems.map((stat: AggregatedInfo, index: number) => (
              <DemographicsLevel
                key={visibleRange.start + index}
                levelColor={(visibleRange.start + index) % 2 === 0 ? 'rgba(255, 255, 255, 0.1)' : 'transparent'}
                levelName={stat.label}
                levelValues={{
                  work: stat.work,
                  elementary: stat.elementary,
                  highSchool: stat.highSchool,
                  college: stat.college,
                  university: stat.university,
                  other: stat.other,
                  total: stat.total
                }}
                total={stat.total}
              />
            ))}
          </div>
        </div>
      </div>
    );
  }, []);

  // Performance monitoring hook
  const usePerformanceMonitor = (componentName: string) => {
    useEffect(() => {
      const startTime = performance.now();
      
      return () => {
        const endTime = performance.now();
        const duration = endTime - startTime;
        if (duration > 16.67) { // Longer than one frame (60fps)
          console.warn(`${componentName} render took ${duration.toFixed(2)}ms`);
        }
      };
    });
  };

  // New state to control panel visibility
  const [isPanelVisible, setIsPanelVisible] = useState(true);

  // Handler for closing the panel
  const handleClose = useCallback(() => {
    onClose();
  }, [onClose]);

  // Define grouping strategies
  type GroupingStrategy = 'none' | 'fiveYear' | 'tenYear' | 'custom' | 'lifecycle';

  interface GroupingOption {
    label: string;
    value: GroupingStrategy;
    ranges: { label: string; min: number; max: number }[];
  }

  const GROUP_STRATEGIES: GroupingOption[] = [
    {
      label: 'Detailed View',
      value: 'none',
      ranges: []
    },
    {
      label: '5-Year Groups',
      value: 'fiveYear',
      ranges: AGE_RANGES
    },
    {
      label: '10-Year Groups',
      value: 'tenYear',
      ranges: [
        { label: '0-10', min: 0, max: 10 },
        { label: '11-20', min: 11, max: 20 },
        { label: '21-30', min: 21, max: 30 },
        { label: '31-40', min: 31, max: 40 },
        { label: '41-50', min: 41, max: 50 },
        { label: '51-60', min: 51, max: 60 },
        { label: '61-70', min: 61, max: 70 },
        { label: '71-80', min: 71, max: 80 },
        { label: '81-90', min: 81, max: 90 },
        { label: '91-100', min: 91, max: 100 },
        { label: '101+', min: 101, max: 200 }
      ]
    },
    {
      label: 'Lifecycle Groups',
      value: 'lifecycle',
      ranges: [
        { label: 'Child (0-20)', min: 0, max: 20 },
        { label: 'Teen (21-35)', min: 21, max: 35 },
        { label: 'Adult (36-83)', min: 36, max: 83 },
        { label: 'Elderly (84-200+)', min: 84, max: 200 },
        
      ]
    }
  ];

  // Modified aggregation function to use selected grouping strategy
  const aggregateDataByStrategy = useCallback((details: Info[], strategy: GroupingStrategy): AggregatedInfo[] => {
    if (strategy === 'none') {
      return groupDetailsByAge(details);
    }

    const selectedStrategy = GROUP_STRATEGIES.find(s => s.value === strategy);
    if (!selectedStrategy) return [];

    const aggregated = selectedStrategy.ranges.map(range => ({
      label: range.label,
      work: 0,
      elementary: 0,
      highSchool: 0,
      college: 0,
      university: 0,
      other: 0,
      total: 0,
    }));

    details.forEach(info => {
      const index = selectedStrategy.ranges.findIndex(
        range => info.age >= range.min && info.age <= range.max
      );

      if (index !== -1) {
        const agg = aggregated[index];
        agg.work += info.work;
        agg.elementary += info.school1;
        agg.highSchool += info.school2;
        agg.college += info.school3;
        agg.university += info.school4;
        agg.other += info.other;
        agg.total += info.total;
      }
    });

    return aggregated;
  }, []);

  // Modified chart data preparation
  const chartData = useMemo(() => {
    // Early return if no details
    if (!details.length) return { labels: [], datasets: [] };

    const aggregated = aggregateDataByStrategy(details, groupingStrategy);
    const labels = aggregated.map(data => data.label);
    
    const datasets = Object.entries(createDatasetConfig).map(([key, config]) => ({
      ...config,
      data: aggregated.map(data => 
        key === 'work' ? data.work :
        key === 'elementary' ? data.elementary :
        key === 'highSchool' ? data.highSchool :
        key === 'college' ? data.college :
        key === 'university' ? data.university :
        data.other
      )
    }));

    return { labels, datasets };
  }, [details, groupingStrategy, createDatasetConfig]);

  // Grouping options component
  const GroupingOptions = () => {
    const containerStyle = useMemo(() => ({
      display: 'flex',
      flexDirection: 'column' as const,
      gap: '0.5rem',
      padding: '1rem',
      backgroundColor: 'rgba(0, 0, 0, 0.2)',
      borderRadius: '4px',
      margin: '0 1rem'
    }), []);

    return (
      <div style={containerStyle}>
        <div style={{ color: 'white', marginBottom: '0.5rem', fontSize: '14px' }}>Age Grouping</div>
        {GROUP_STRATEGIES.map(strategy => (
          <InfoCheckbox
            key={strategy.value}
            label={strategy.label}
            isChecked={groupingStrategy === strategy.value}
            onToggle={() => setGroupingStrategy(strategy.value)}
            count={strategy.ranges.length || details.length}
          />
        ))}
      </div>
    );
  };

  if (!isPanelVisible) {
    return null;
  }

  return (
    <$Panel
      id="infoloom-demographics"
      title="Demographics"
      onClose={handleClose}
      initialSize={{ width: panWidth, height: panHeight }}
      initialPosition={{ top: window.innerHeight * 0.009, left: window.innerWidth * 0.053 }}
      style={panelStyle}
    >
      
      <div style={{ flex: '0 0 auto', display: 'flex', flexDirection: 'row', width: '100%' }}>
        <div style={{ width: '50%', paddingRight: '4rem'}}>
          <AlignedParagraph left="All Citizens" right={totals[0] || 0} />
          <div style={{ height: '1rem' }}></div>
          <AlignedParagraph left="- Tourists" right={totals[2] || 0} />
          <div style={{ height: '1rem' }}></div>
          <AlignedParagraph left="- Commuters" right={totals[3] || 0} />
          <div style={{ height: '1rem' }}></div>
          <AlignedParagraph left="- Moving Away" right={totals[7] || 0} />
          <div style={{ height: '1rem' }}></div>
          <AlignedParagraph left="Population" right={totals[1] || 0} />
        </div>
        <div style={{ width: '50%', paddingLeft: '4rem'}}>
          <AlignedParagraph left="Dead" right={totals[8] || 0} />
          <div style={{ height: '1rem' }}></div>
          <AlignedParagraph left="Students" right={totals[4] || 0} />
          <div style={{ height: '1rem' }}></div>
          <AlignedParagraph left="Workers" right={totals[5] || 0} />
          <div style={{ height: '1rem' }}></div>
          <AlignedParagraph left="Homeless" right={totals[9] || 0} />
          <div style={{ height: '1rem' }}></div>
          <AlignedParagraph left="Oldest Citizen" right={totals[6] || 0} />
        </div>
      </div>

      {/* Spacer */}
      <div style={{ flex: '0 0 auto', height: '1rem' }}></div>

      {/* Toggle Buttons */}
<div
  style={{
    flex: '0 0 auto',
    display: 'flex',
    flexDirection: 'column',
    margin: '1rem'
  }}
>
  <GroupingOptions />
  
  <div style={{ display: 'flex', justifyContent: 'center', margin: '1rem' }}>
    
    
  </div>
</div>

      {/* Spacer */}
      <div style={{ flex: '0 0 auto', height: '1rem' }}></div>

      {/* Conditionally Render Summary Statistics with Virtualization */}
      {showSummaryStats && (
        <div
          style={{
            flex: '0 0 auto',
            padding: '2rem',
            backgroundColor: 'rgba(0, 0, 0, 0.5)',
            borderRadius: '4px',
            margin: '0 2rem',
            overflow: 'hidden',
            maxHeight: '300px',
          }}
        >
          <h3 style={{ color: 'white', marginBottom: '0.5rem' }}>Summary Statistics</h3>
          <VirtualizedSummaryList items={aggregateDataByStrategy(details, groupingStrategy)} />
        </div>
      )}
      {/* Spacer */}
      <div style={{ flex: '0 0 auto', height: '1rem' }}></div>

      {/* Scrollable Chart Container */}
      <div style={{ flex: '1 1 auto', width: '100%', overflowY: 'auto' }}>
        {details.length === 0 ? (
          <p style={{ color: 'white' }}>No data available to display the chart.</p>
        ) : (
          <div style={{ height: `${chartHeightToUse}px`, width: '100%' }}>
            <Bar data={chartData} options={chartOptions} />
          </div>
        )}
      </div>
    </$Panel>
  );
};

// Helper function to get distinct colors for datasets
const getColor = (index: number) => {
  const colors = [
    '#624532',
    '#7E9EAE',
    '#00C217',
    '#005C4E',
    '#2462FF',
    '#A1A1A1',
    '#FF5733',
    '#C70039',
    '#900C3F',
    '#581845',
  ];
  return colors[index % colors.length];
};

export default Demographics;