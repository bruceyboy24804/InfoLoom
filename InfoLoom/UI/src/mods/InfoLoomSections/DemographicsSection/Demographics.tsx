import React, { useState, useMemo, KeyboardEvent } from 'react';
import useDataUpdate from 'mods/use-data-update';
import $Panel from 'mods/panel';

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

// Register Chart.js components
ChartJS.register(CategoryScale, LinearScale, BarElement, Tooltip, Legend, Title);

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

// Define age ranges as a constant
const AGE_RANGES = [
  { label: '0-10', min: 0, max: 10 },
  { label: '10-20', min: 10, max: 20 },
  { label: '20-30', min: 20, max: 30 },
  { label: '30-40', min: 30, max: 40 },
  { label: '40-50', min: 40, max: 50 },
  { label: '50-60', min: 50, max: 60 },
  { label: '60-70', min: 60, max: 70 },
  { label: '70-80', min: 70, max: 80 },
  { label: '80-90', min: 80, max: 90 },
  { label: '90-100', min: 90, max: 100 },
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
        (info.age < range.max || (info.age === range.max && range.max === 100))
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
const DemographicsLevel: React.FC<{
  levelColor: string;
  levelName: string;
  levelValues: {
    work: number;
    elementary: number;
    highSchool: number;
    college: number;
    university: number;
    other: number;
  };
  total: number;
}> = ({ levelColor, levelName, levelValues, total }) => (
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

const Demographics: React.FC = () => {
  // State hooks for totals and details
  const [totals, setTotals] = useState<number[]>([]);
  const [details, setDetails] = useState<Info[]>([]);

  // State hooks for grouping and summary statistics visibility
  const [isGrouped, setIsGrouped] = useState<boolean>(false);
  const [showSummaryStats, setShowSummaryStats] = useState<boolean>(false);

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

  // Prepare detailed data for Chart.js with grouping
  const detailedChartData = useMemo(() => {
    const groupedData = groupDetailsByAge(details);
    const sortedAges = groupedData.sort((a, b) => parseInt(a.label) - parseInt(b.label));

    return {
      labels: sortedAges.map(data => data.label),
      datasets: [
        {
          label: 'Work',
          data: sortedAges.map(data => data.work),
          backgroundColor: '#624532',
        },
        {
          label: 'Elementary',
          data: sortedAges.map(data => data.elementary),
          backgroundColor: '#7E9EAE',
        },
        {
          label: 'High School',
          data: sortedAges.map(data => data.highSchool),
          backgroundColor: '#00C217',
        },
        {
          label: 'College',
          data: sortedAges.map(data => data.college),
          backgroundColor: '#005C4E',
        },
        {
          label: 'University',
          data: sortedAges.map(data => data.university),
          backgroundColor: '#2462FF',
        },
        {
          label: 'Other',
          data: sortedAges.map(data => data.other),
          backgroundColor: '#A1A1A1',
        },
      ],
    };
  }, [details]);

  // Prepare grouped data for Chart.js
  const groupedChartData = useMemo(() => {
    const aggregated = aggregateDataByAgeRanges(details);

    return {
      labels: aggregated.map(data => data.label),
      datasets: [
        {
          label: 'Work',
          data: aggregated.map(data => data.work),
          backgroundColor: '#624532',
        },
        {
          label: 'Elementary',
          data: aggregated.map(data => data.elementary),
          backgroundColor: '#7E9EAE',
        },
        {
          label: 'High School',
          data: aggregated.map(data => data.highSchool),
          backgroundColor: '#00C217',
        },
        {
          label: 'College',
          data: aggregated.map(data => data.college),
          backgroundColor: '#005C4E',
        },
        {
          label: 'University',
          data: aggregated.map(data => data.university),
          backgroundColor: '#2462FF',
        },
        {
          label: 'Other',
          data: aggregated.map(data => data.other),
          backgroundColor: '#A1A1A1',
        },
      ],
    };
  }, [details]);

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
          title: {
            display: true,
            text: isGrouped ? 'Age Groups' : 'Age',
            color: 'white',
            font: commonFont,
          },
          ticks: {
            color: 'white',
            font: { ...commonFont, size: commonFont.size - 4 },
            autoSkip: false,
          },
          grid: {
            color: 'rgba(255, 255, 255, 0.1)',
          },
        },
      },
    }),
    [isGrouped]
  );

  // Choose chart data based on isGrouped
  const chartDataToUse = isGrouped ? groupedChartData : detailedChartData;

  // Calculate dynamic chart height with a new maximum limit
  const chartHeight = useMemo(() => {
    const dataLength = isGrouped ? AGE_RANGES.length : details.length;
    return Math.min(dataLength * BAR_HEIGHT, MAX_CHART_HEIGHT);
  }, [isGrouped, details.length]);

  // Calculate detailed summary statistics per age or age group
  const detailedSummaryStats = useMemo(() => {
    return isGrouped ? aggregateDataByAgeRanges(details) : groupDetailsByAge(details);
  }, [details, isGrouped]);

  // Define functions to handle keypress on buttons for accessibility
  const handleToggleKeyPress = (
    e: KeyboardEvent<HTMLButtonElement>,
    toggleFunction: () => void
  ) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      toggleFunction();
    }
  };

  // NEW: Function to handle data reset
  const handleResetData = () => {
    setTotals([]);
    setDetails([]);
  };

  return (
    <$Panel
      title="Demographics"
      initialSize={{ width: panWidth, height: panHeight }}
      initialPosition={{ top: window.innerHeight * 0.009, left: window.innerWidth * 0.053 }}
      style={{
        backgroundColor: 'var(--panelColorNormal)',
        display: 'flex',
        flexDirection: 'column',
        overflow: 'hidden',
        margin: '3rem',
      }}
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
    justifyContent: 'center',
    margin: '5rem', // Increased gap between buttons
  }}
>
  <button
    onClick={() => setIsGrouped(prev => !prev)}
    onKeyPress={e => handleToggleKeyPress(e, () => setIsGrouped(prev => !prev))}
    style={{
      padding: '0.5rem 1rem', // Reduced padding for better appearance
      backgroundColor: '#34495e',
      color: 'white',
      border: 'none',
      borderRadius: '4px',
      cursor: 'pointer',
      fontSize: '14px',
      margin: '3rem',
    }}
    aria-pressed={isGrouped}
    aria-label={isGrouped ? 'Show Detailed View' : 'Show Grouped View'}
  >
    {isGrouped ? 'Show Detailed View' : 'Show Grouped View'}
  </button>

  <button
    onClick={() => setShowSummaryStats(prev => !prev)}
    onKeyPress={e => handleToggleKeyPress(e, () => setShowSummaryStats(prev => !prev))}
    style={{
      padding: '0.5rem 1rem',
      backgroundColor: '#34495e',
      color: 'white',
      border: 'none',
      borderRadius: '4px',
      cursor: 'pointer',
      fontSize: '14px',
      margin: '3rem',
    }}
    aria-pressed={showSummaryStats}
    aria-label={showSummaryStats ? 'Hide Summary Statistics' : 'Show Summary Statistics'}
  >
    {showSummaryStats ? 'Hide Summary Stats' : 'Show Summary Stats'}
  </button>

  {/* Reset Data Button */}
  <button
    onClick={handleResetData}
    onKeyPress={e => handleToggleKeyPress(e, handleResetData)}
    style={{
      padding: '0.5rem 1rem',
      backgroundColor: '#e74c3c',
      color: 'white',
      border: 'none',
      borderRadius: '4px',
      cursor: 'pointer',
      fontSize: '14px',
      margin: '3rem',
    }}
    aria-label="Reset Data"
  >
    Reset Data
  </button>
</div>

      {/* Spacer */}
      <div style={{ flex: '0 0 auto', height: '1rem' }}></div>

      {/* Conditionally Render Summary Statistics */}
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

          {/* Scrollable Container */}
          <div
            style={{
              overflowY: 'auto',
              maxHeight: '250px',
              paddingRight: '10px',
            }}
          >
            {/* Header Row */}
            <div
              className="labels_L7Q row_S2v"
              style={{ width: '100%', padding: '1rem 0', borderBottom: '1px solid white' }}
            >
              <div style={{ width: '1%' }}></div>
              <div style={{ display: 'flex', alignItems: 'center', width: '22%' }}>
                <div>Age</div>
              </div>
              <div className="row_S2v" style={{ width: '11%', justifyContent: 'center' }}>
                Total
              </div>
              <div className="row_S2v" style={{ width: '11%', justifyContent: 'center' }}>
                Work
              </div>
              <div className="row_S2v" style={{ width: '12%', justifyContent: 'center' }}>
                Elementary
              </div>
              <div className="row_S2v small_ExK" style={{ width: '9%', justifyContent: 'center' }}>
                High School
              </div>
              <div className="row_S2v small_ExK" style={{ width: '9%', justifyContent: 'center' }}>
                College
              </div>
              <div className="row_S2v small_ExK" style={{ width: '9%', justifyContent: 'center' }}>
                University
              </div>
              <div className="row_S2v small_ExK" style={{ width: '9%', justifyContent: 'center' }}>
                Other
              </div>
            </div>

            {/* Summary Rows */}
            {detailedSummaryStats.map((stat, index) => (
              <DemographicsLevel
                key={index}
                levelColor={index % 2 === 0 ? 'rgba(255, 255, 255, 0.1)' : 'transparent'}
                levelName={stat.label}
                levelValues={{
                  work: stat.work,
                  elementary: stat.elementary,
                  highSchool: stat.highSchool,
                  college: stat.college,
                  university: stat.university,
                  other: stat.other,
                }}
                total={stat.total}
              />
            ))}
          </div>
        </div>
      )}

      {/* Spacer */}
      <div style={{ flex: '0 0 auto', height: '1rem' }}></div>

      {/* Scrollable Chart Container */}
      <div style={{ flex: '1 1 auto', width: '100%', overflowY: 'auto' }}>
        {details.length === 0 ? (
          <p style={{ color: 'white' }}>No data available to display the chart.</p>
        ) : (
          <div style={{ height: `${chartHeight}px`, width: '100%' }}>
            <Bar data={chartDataToUse} options={chartOptions} />
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
