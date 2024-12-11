import React, { useState, useMemo, KeyboardEvent, useCallback, FC, useRef, useEffect } from 'react';
import useDataUpdate from 'mods/use-data-update';
import $Panel from 'mods/panel';
import mod from "mod.json";

// Import Chart.js components
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
import { bindValue, useValue } from 'cs2/api';
import { InfoCheckbox } from "../../InfoCheckbox/InfoCheckbox";

// Register Chart.js components
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

const AgeCap$ = bindValue<number>(mod.id, 'AgeCap');

// Define interfaces for component props
interface AlignedParagraphProps {
  left: string;
  right: number;
}

interface Info {
  age: number;
  work: number;
  school1: number;
  school2: number;
  school3: number;
  school4: number;
  other: number;
  total: number;
}

// Font configurations
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

// Types and Interfaces
type GroupingStrategy = 'none' | 'fiveYear' | 'tenYear' | 'lifecycle';

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

// Chart configuration types
type ChartOptions = {
  responsive: boolean;
  maintainAspectRatio: boolean;
  indexAxis?: 'x' | 'y';
  animation: {
    duration: number;
  };
  plugins: {
    legend: {
      position: 'top' | 'bottom' | 'left' | 'right';
      labels: {
        color: string;
        font: {
          size: number;
        };
      };
    };
  };
  scales: {
    x: {
      stacked: boolean;
      grid: {
        display: boolean;
        color: string;
      };
      ticks: {
        color: string;
        font: {
          size: number;
        };
      };
    };
    y: {
      stacked: boolean;
      grid: {
        display: boolean;
        color: string;
      };
      ticks: {
        color: string;
        font: {
          size: number;
        };
      };
    };
  };
};

// Helper function to generate age ranges
const generateRanges = (step: number): AgeRange[] => {
  const ranges: AgeRange[] = [];
  for (let i = 0; i < 200; i += step) {
    ranges.push({
      label: i === 0 ? `0-${step}` : i >= 100 ? '100+' : `${i}-${i + step}`,
      min: i,
      max: i + step
    });
  }
  return ranges;
};

// Constants for chart configuration
const GROUP_STRATEGIES: GroupingOption[] = [
  {
    label: 'Detailed View',
    value: 'none',
    ranges: []
  },
  {
    label: '5-Year Groups',
    value: 'fiveYear',
    ranges: generateRanges(5)
  },
  {
    label: '10-Year Groups',
    value: 'tenYear',
    ranges: generateRanges(10)
  },
  {
    label: 'Lifecycle Groups',
    value: 'lifecycle',
    ranges: [
      { label: 'Child', min: 0, max: 20 },
      { label: 'Teen', min: 21, max: 35 },
      { label: 'Adult', min: 36, max: 83 },
      { label: 'Elderly', min: 84, max: 200 }
    ]
  }
];

// Chart configuration function
const getChartOptions = (
  legendPosition: 'top' | 'bottom' | 'left' | 'right',
  showGridLines: boolean,
  enableAnimation: boolean,
  stackedView: boolean
): ChartOptions => ({
  responsive: true,
  maintainAspectRatio: false,
  animation: {
    duration: enableAnimation ? 750 : 0
  },
  plugins: {
    legend: {
      position: legendPosition,
      labels: {
        color: 'rgba(255, 255, 255, 0.7)',
        font: {
          size: 12
        }
      }
    }
  },
  scales: {
    x: {
      stacked: stackedView,
      grid: {
        display: showGridLines,
        color: 'rgba(255, 255, 255, 0.1)'
      },
      ticks: {
        color: 'rgba(255, 255, 255, 0.7)',
        font: {
          size: 12
        }
      }
    },
    y: {
      stacked: stackedView,
      grid: {
        display: showGridLines,
        color: 'rgba(255, 255, 255, 0.1)'
      },
      ticks: {
        color: 'rgba(255, 255, 255, 0.7)',
        font: {
          size: 12
        }
      }
    }
  }
});

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

export const Demographics: FC<DemographicsProps> = ({ onClose }) => {
  // State hooks
  const [details, setDetails] = useState<Info[]>([]);
  const [totals, setTotals] = useState<number[]>([]);
  const [isPanelVisible, setIsPanelVisible] = useState<boolean>(true);
  const [groupingStrategy, setGroupingStrategy] = useState<GroupingStrategy>('none');
  const [showChartSettings, setShowChartSettings] = useState<boolean>(false);
  const [chartType, setChartType] = useState<'bar' | 'line'>('bar');
  const [showGridLines, setShowGridLines] = useState<boolean>(true);
  const [enableAnimation, setEnableAnimation] = useState<boolean>(true);
  const [legendPosition, setLegendPosition] = useState<'top' | 'bottom' | 'left' | 'right'>('top');
  const [stackedView, setStackedView] = useState<boolean>(false);
  const [showAgeGrouping, setShowAgeGrouping] = useState<boolean>(true);
  const [showStatistics, setShowStatistics] = useState<boolean>(true);
  const [showDetailedStats, setShowDetailedStats] = useState<boolean>(false);
  const [isHorizontal, setIsHorizontal] = useState<boolean>(false);
  const [containerHeight, setContainerHeight] = useState<number>(600);

  // Refs
  const chartContainerRef = useRef<HTMLDivElement>(null);

  // Get AgeCap value
  const AgeCap = useValue(AgeCap$);

  // Effect for resize observer
  useEffect(() => {
    if (!chartContainerRef.current) return;

    const resizeObserver = new ResizeObserver(entries => {
      const entry = entries[0];
      if (entry) {
        const height = entry.contentRect.height;
        setContainerHeight(height);
      }
    });

    resizeObserver.observe(chartContainerRef.current);

    return () => {
      resizeObserver.disconnect();
    };
  }, []);

  // Fetch data using useDataUpdate hook
  useDataUpdate('populationInfo.structureDetails', data => setDetails(data || []));
  useDataUpdate('populationInfo.structureTotals', data => setTotals(data || []));

  // Panel dimensions
  const panWidth = window.innerWidth * 0.2;
  const panHeight = window.innerHeight * 0.86;

  // Define per-bar height and maximum chart height
  const BAR_HEIGHT = 40;
  const MAX_CHART_HEIGHT = 1200;

  // Memoized chart colors
  const chartColors = useMemo(() => ({
    work: '#624532',
    elementary: '#7E9EAE',
    highSchool: '#00C217',
    college: '#005C4E',
    university: '#2462FF',
    other: '#A1A1A1',
  } as const), []);

  // Memoized dataset configuration
  const createDatasetConfig = useMemo(() => ({
    work: { label: 'Work', backgroundColor: chartColors.work },
    elementary: { label: 'Elementary', backgroundColor: chartColors.elementary },
    highSchool: { label: 'High School', backgroundColor: chartColors.highSchool },
    college: { label: 'College', backgroundColor: chartColors.college },
    university: { label: 'University', backgroundColor: chartColors.university },
    other: { label: 'Other', backgroundColor: chartColors.other },
  }), [chartColors]);

  // Chart options using the configuration function
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
          position: 'left' as const,
          title: {
            display: true,
            text: groupingStrategy !== 'none' ? 'Age Groups in Days' : 'Age in Days',
            color: 'white',
            font: commonFont,
          },
          ticks: {
            color: 'white',
            font: { ...yaxisfont, size: yaxisfont.size },
            autoSkip: false,
            stepSize: groupingStrategy !== 'none' ? 1 : 5,
            padding: 10,
          },
          grid: {
            color: 'rgba(255, 255, 255, 0.1)',
          },
          afterFit: function(scaleInstance: { height: number }) {
            const minSpacing = 30; // Minimum pixels between ticks
            const numTicks = details.length;
            const totalSpacing = numTicks * minSpacing;
            scaleInstance.height = Math.max(scaleInstance.height, totalSpacing);
          },
        },
      },
    }),
    [groupingStrategy, AgeCap, details.length, commonFont, yaxisfont]
  );

  // Prepare chart data based on grouping strategy
  const chartData = useMemo(() => {
    if (!details.length) return { labels: [], datasets: [] };

    if (groupingStrategy === 'none') {
      // Detailed view
      const validDetails = details.filter(detail => detail.age <= (AgeCap || 100));
      const sortedAges = validDetails.sort((a, b) => a.age - b.age);
      const labels = sortedAges.map(data => String(data.age));

      const datasets = [
        {
          label: 'Work',
          data: sortedAges.map(d => d.work),
          backgroundColor: chartColors.work,
          borderColor: chartColors.work,
          borderWidth: 1
        },
        {
          label: 'Elementary',
          data: sortedAges.map(d => d.school1),
          backgroundColor: chartColors.elementary,
          borderColor: chartColors.elementary,
          borderWidth: 1
        },
        {
          label: 'High School',
          data: sortedAges.map(d => d.school2),
          backgroundColor: chartColors.highSchool,
          borderColor: chartColors.highSchool,
          borderWidth: 1
        },
        {
          label: 'College',
          data: sortedAges.map(d => d.school3),
          backgroundColor: chartColors.college,
          borderColor: chartColors.college,
          borderWidth: 1
        },
        {
          label: 'University',
          data: sortedAges.map(d => d.school4),
          backgroundColor: chartColors.university,
          borderColor: chartColors.university,
          borderWidth: 1
        },
        {
          label: 'Other',
          data: sortedAges.map(d => d.other),
          backgroundColor: chartColors.other,
          borderColor: chartColors.other,
          borderWidth: 1
        }
      ];

      return { labels, datasets };
    } else {
      // Grouped view
      const selectedStrategy = GROUP_STRATEGIES.find(s => s.value === groupingStrategy);
      if (!selectedStrategy) return { labels: [], datasets: [] };

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
        if (info.age > (AgeCap || 100)) return;
        
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

      const labels = aggregated.map(data => data.label);
      
      const datasets = [
        {
          label: 'Work',
          data: aggregated.map(d => d.work),
          backgroundColor: chartColors.work,
          borderColor: chartColors.work,
          borderWidth: 1
        },
        {
          label: 'Elementary',
          data: aggregated.map(d => d.elementary),
          backgroundColor: chartColors.elementary,
          borderColor: chartColors.elementary,
          borderWidth: 1
        },
        {
          label: 'High School',
          data: aggregated.map(d => d.highSchool),
          backgroundColor: chartColors.highSchool,
          borderColor: chartColors.highSchool,
          borderWidth: 1
        },
        {
          label: 'College',
          data: aggregated.map(d => d.college),
          backgroundColor: chartColors.college,
          borderColor: chartColors.college,
          borderWidth: 1
        },
        {
          label: 'University',
          data: aggregated.map(d => d.university),
          backgroundColor: chartColors.university,
          borderColor: chartColors.university,
          borderWidth: 1
        },
        {
          label: 'Other',
          data: aggregated.map(d => d.other),
          backgroundColor: chartColors.other,
          borderColor: chartColors.other,
          borderWidth: 1
        }
      ];

      return { labels, datasets };
    }
  }, [details, groupingStrategy, AgeCap, chartColors]);

  // Render chart function
  const renderChart = useCallback(() => {
    const ChartComponent = chartType === 'bar' ? Bar : Line;
    return (
      <div style={{ height: '100%', width: '100%', position: 'relative' }}>
        <ChartComponent data={chartData} options={chartOptions} />
      </div>
    );
  }, [chartType, chartData, chartOptions]);

  // Optimized toggle handlers using useCallback
  const handleGroupingStrategyChange = useCallback((strategy: GroupingStrategy) => {
    setGroupingStrategy(strategy);
  }, []);

  const handleStatisticsToggle = useCallback(() => {
    setShowStatistics((prev: boolean) => !prev);
  }, []);

  const handleDetailedStatsToggle = useCallback(() => {
    setShowDetailedStats((prev: boolean) => !prev);
  }, []);

  const handleAgeGroupingToggle = useCallback(() => {
    setShowAgeGrouping((prev: boolean) => !prev);
    if (!showAgeGrouping) {
      setGroupingStrategy('none');
    }
  }, [showAgeGrouping]);

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

  // Handler for closing the panel
  const handleClose = useCallback(() => {
    onClose();
  }, [onClose]);

  // Grouping options component
  const GroupingOptions = () => {
    const containerStyle = useMemo(() => ({
      display: 'flex',
      flexDirection: 'column' as 'column',
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

  // Calculate additional statistics
  const calculateDetailedStats = useCallback(() => {
    if (!details.length) return null;

    const totalPopulation = totals[1] || 0;
    const workingPopulation = totals[5] || 0;
    const studentPopulation = totals[4] || 0;

    // Calculate age distribution
    const ageGroups = {
      child: details.filter(d => d.age <= 20).length,
      teen: details.filter(d => d.age > 20 && d.age <= 35).length,
      adult: details.filter(d => d.age > 35 && d.age <= 83).length,
      elderly: details.filter(d => d.age > 83).length,
    };

    // Calculate percentages
    const getPercentage = (value: number) => ((value / totalPopulation) * 100).toFixed(1);

    return {
      demographics: {
        totalPopulation,
        populationDensity: {
          workers: getPercentage(workingPopulation),
          students: getPercentage(studentPopulation),
          homeless: getPercentage(totals[9] || 0),
          tourists: getPercentage(totals[2] || 0),
        },
      },
      ageDistribution: {
        child: getPercentage(ageGroups.child),
        teen: getPercentage(ageGroups.teen),
        adult: getPercentage(ageGroups.adult),
        elderly: getPercentage(ageGroups.elderly),
      },
      education: {
        elementary: getPercentage(details.reduce((acc, d) => acc + d.school1, 0)),
        highSchool: getPercentage(details.reduce((acc, d) => acc + d.school2, 0)),
        college: getPercentage(details.reduce((acc, d) => acc + d.school3, 0)),
        university: getPercentage(details.reduce((acc, d) => acc + d.school4, 0)),
      },
      averageAge: (details.reduce((acc, d) => acc + d.age, 0) / details.length).toFixed(1),
    };
  }, [details, totals]);

  // Detailed Statistics Component
  const DetailedStatistics = () => {
    const stats = calculateDetailedStats();
    if (!stats) return null;

    const StatRow = ({ label, value, color = 'white' }: { label: string; value: string | number; color?: string }) => (
      <div style={{ display: 'flex', justifyContent: 'space-between', margin: '0.25rem 0' }}>
        <span style={{ color: 'rgba(255, 255, 255, 0.7)' }}>{label}</span>
        <span style={{ color }}>{value}%</span>
      </div>
    );

    const SectionTitle = ({ title }: { title: string }) => (
      <div style={{ 
        color: 'white', 
        fontSize: '14px', 
        fontWeight: 'bold', 
        marginTop: '1rem', 
        marginBottom: '0.5rem',
        borderBottom: '1px solid rgba(255, 255, 255, 0.2)',
        paddingBottom: '0.25rem'
      }}>
        {title}
      </div>
    );

    return (
      <div style={{ 
        backgroundColor: 'rgba(0, 0, 0, 0.2)', 
        padding: '1rem',
        borderRadius: '4px',
        margin: '1rem',
        fontSize: '13px'
      }}>
        <SectionTitle title="Population Distribution" />
        <StatRow label="Workers" value={stats.demographics.populationDensity.workers} color="#4CAF50" />
        <StatRow label="Students" value={stats.demographics.populationDensity.students} color="#2196F3" />
        <StatRow label="Tourists" value={stats.demographics.populationDensity.tourists} color="#FFC107" />
        <StatRow label="Homeless" value={stats.demographics.populationDensity.homeless} color="#FF5722" />

        <SectionTitle title="Age Distribution" />
        <StatRow label="Children (0-20)" value={stats.ageDistribution.child} color="#81C784" />
        <StatRow label="Young Adults (21-35)" value={stats.ageDistribution.teen} color="#64B5F6" />
        <StatRow label="Adults (36-83)" value={stats.ageDistribution.adult} color="#FFB74D" />
        <StatRow label="Elderly (84+)" value={stats.ageDistribution.elderly} color="#E57373" />
        <StatRow label="Average Age" value={`${stats.averageAge} days`} />

        <SectionTitle title="Education Levels" />
        <StatRow label="Elementary" value={stats.education.elementary} color="#AED581" />
        <StatRow label="High School" value={stats.education.highSchool} color="#4FC3F7" />
        <StatRow label="College" value={stats.education.college} color="#FFD54F" />
        <StatRow label="University" value={stats.education.university} color="#FF8A65" />
      </div>
    );
  };

  // Chart Settings Component
  const ChartSettings = () => {
    return (
      <div style={{ 
        backgroundColor: 'rgba(0, 0, 0, 0.2)', 
        padding: '1rem',
        borderRadius: '4px',
        margin: '1rem',
        fontSize: '13px'
      }}>
        <div style={{ 
          color: 'white', 
          fontSize: '14px', 
          fontWeight: 'bold', 
          marginBottom: '1rem',
          borderBottom: '1px solid rgba(255, 255, 255, 0.2)',
          paddingBottom: '0.25rem'
        }}>
          Chart Settings
        </div>

        <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
          {/* Chart Type */}
          <div>
            <div style={{ color: 'rgba(255, 255, 255, 0.7)', marginBottom: '0.5rem' }}>Chart Type</div>
            <div style={{ display: 'flex', gap: '1rem' }}>
              <InfoCheckbox
                label="Bar Chart"
                isChecked={chartType === 'bar'}
                onToggle={() => setChartType('bar')}
              />
              <InfoCheckbox
                label="Line Chart"
                isChecked={chartType === 'line'}
                onToggle={() => setChartType('line')}
              />
            </div>
          </div>

          {/* Layout Options */}
          <div>
            <div style={{ color: 'rgba(255, 255, 255, 0.7)', marginBottom: '0.5rem' }}>Layout</div>
            <div style={{ display: 'flex', gap: '1rem', flexWrap: 'wrap' }}>
              <InfoCheckbox
                label="Show Grid Lines"
                isChecked={showGridLines}
                onToggle={setShowGridLines}
              />
              <InfoCheckbox
                label="Enable Animation"
                isChecked={enableAnimation}
                onToggle={setEnableAnimation}
              />
              <InfoCheckbox
                label="Stacked View"
                isChecked={stackedView}
                onToggle={setStackedView}
              />
            </div>
          </div>

          {/* Legend Position */}
          <div>
            <div style={{ color: 'rgba(255, 255, 255, 0.7)', marginBottom: '0.5rem' }}>Legend Position</div>
            <div style={{ display: 'flex', gap: '1rem', flexWrap: 'wrap' }}>
              {(['top', 'bottom', 'left', 'right'] as const).map(position => (
                <InfoCheckbox
                  key={position}
                  label={position.charAt(0).toUpperCase() + position.slice(1)}
                  isChecked={legendPosition === position}
                  onToggle={() => setLegendPosition(position)}
                />
              ))}
            </div>
          </div>

          {/* Chart Orientation */}
          <div>
            <div style={{ color: 'rgba(255, 255, 255, 0.7)', marginBottom: '0.5rem' }}>Chart Orientation</div>
            <div style={{ display: 'flex', gap: '1rem', flexWrap: 'wrap' }}>
              <InfoCheckbox
                label="Horizontal"
                isChecked={isHorizontal}
                onToggle={() => setIsHorizontal(prev => !prev)}
              />
            </div>
          </div>
        </div>
      </div>
    );
  };

  if (!isPanelVisible) {
    return null;
  }

  const chartHeightToUse = Math.min(MAX_CHART_HEIGHT, details.length * BAR_HEIGHT);

  return (
    <$Panel
      id="infoloom-demographics"
      onClose={handleClose}
      title="Demographics"
      initialSize={{ width: panWidth, height: panHeight }}
      initialPosition={{ top: window.innerHeight * 0.009, left: window.innerWidth * 0.053 }}
      style={panelStyle}
    >
      <div style={{ 
        padding: '10px',
        height: '100%',
        display: 'flex',
        flexDirection: 'column',
        overflow: 'hidden'
      }}>
        {/* View Options */}
        <div style={{ display: 'flex', justifyContent: 'flex-end', padding: '0.5rem 1rem', gap: '1rem', flexShrink: 0 }}>
          <InfoCheckbox
            label="Show Statistics"
            isChecked={showStatistics}
            onToggle={() => setShowStatistics(prev => !prev)}
          />
          <InfoCheckbox
            label="Show Age Grouping"
            isChecked={showAgeGrouping}
            onToggle={() => setShowAgeGrouping(prev => !prev)}
          />
          <InfoCheckbox
            label="Show Detailed Stats"
            isChecked={showDetailedStats}
            onToggle={() => setShowDetailedStats(prev => !prev)}
          />
          <InfoCheckbox
            label="Chart Settings"
            isChecked={showChartSettings}
            onToggle={() => setShowChartSettings(prev => !prev)}
          />
        </div>

        {/* Chart Settings Panel */}
        {showChartSettings && <div style={{ flexShrink: 0 }}><ChartSettings /></div>}
        
        {/* Statistics Panel */}
        {showStatistics && (
          <div style={{ flex: '0 0 auto', display: 'flex', flexDirection: 'row', width: '100%', flexShrink: 0 }}>
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
        )}

        {/* Age Grouping Section */}
        {showAgeGrouping && (
          <div style={{ flex: '0 0 auto', display: 'flex', flexDirection: 'column', margin: '1rem', flexShrink: 0 }}>
            <GroupingOptions />
          </div>
        )}

        {/* Detailed Statistics Panel */}
        {showDetailedStats && <div style={{ flexShrink: 0 }}><DetailedStatistics /></div>}

        {/* Chart Section */}
        <div style={{ 
          flex: '1 1 auto', 
          minHeight: 0, 
          padding: '1rem',
          position: 'relative',
          overflowY: 'auto',
          overflowX: 'hidden'
        }}>
          {details.length === 0 ? (
            <p style={{ color: 'white' }}>No data available to display the chart.</p>
          ) : (
            <div style={{ 
              height: `${chartHeightToUse}px`, 
              width: '100%',
              position: 'relative'
            }}>
              <Bar data={chartData} options={chartOptions} />
            </div>
          )}
        </div>
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