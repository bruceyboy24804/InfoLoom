// Demographics.tsx

import React, { useState, useEffect, useMemo, KeyboardEvent } from 'react';
import useDataUpdate from 'mods/use-data-update';
import $Panel from 'mods/panel';
import engine from 'cohtml/cohtml';

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
  university: number;
  college: number;
  highSchool: number;
  elementary: number;
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
  family: 'Arial, sans-serif', // Replace with your desired font family
  size: 14, // Base font size in pixels
  weight: 'normal' as const, // Font weight
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

// Centralized aggregation function
const aggregateDataByAgeRanges = (details: Info[]): AggregatedInfo[] => {
  // Initialize aggregated data
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

  // Aggregate data
  details.forEach(info => {
    AGE_RANGES.forEach((range, index) => {
      if (info.age >= range.min && info.age < range.max) {
        aggregated[index].work += info.work;
        aggregated[index].elementary += info.school1;
        aggregated[index].highSchool += info.school2;
        aggregated[index].college += info.school3;
        aggregated[index].university += info.school4;
        aggregated[index].other += info.other;
        aggregated[index].total += info.total;
      } else if (info.age === range.max && range.max === 100) {
        // Include age 100 in the last group
        aggregated[index].work += info.work;
        aggregated[index].elementary += info.school1;
        aggregated[index].highSchool += info.school2;
        aggregated[index].college += info.school3;
        aggregated[index].university += info.school4;
        aggregated[index].other += info.other;
        aggregated[index].total += info.total;
      }
    });
  });

  return aggregated;
};

// Function to group details by individual age, mapping school1 etc. to elementary etc.
const groupDetailsByAge = (details: Info[]): AggregatedInfo[] => {
  const grouped: Record<number, AggregatedInfo> = details.reduce((acc, info) => {
    const age = info.age;
    if (!acc[age]) {
      acc[age] = {
        label: `Age ${age}`,
        work: info.work,
        elementary: info.school1,
        highSchool: info.school2,
        college: info.school3,
        university: info.school4,
        other: info.other,
        total: info.work + info.school1 + info.school2 + info.school3 + info.school4 + info.other,
      };
    } else {
      acc[age].work += info.work;
      acc[age].elementary += info.school1;
      acc[age].highSchool += info.school2;
      acc[age].college += info.school3;
      acc[age].university += info.school4;
      acc[age].other += info.other;
      acc[age].total += info.work + info.school1 + info.school2 + info.school3 + info.school4 + info.other;
    }
    return acc;
  }, {} as Record<number, AggregatedInfo>);
  
  // Convert the Record to an array before returning
  return Object.values(grouped);
};

// AlignedParagraph Component for Summary
const AlignedParagraph: React.FC<AlignedParagraphProps> = ({ left, right }) => {
  return (
    <div
      className="labels_L7Q row_S2v"
      style={{
        width: '100%',
        paddingTop: '0.5rem',
        paddingBottom: '0.5rem',
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
};

// DemographicsLevel Component (Similar to WorkforceLevel)
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
}> = ({ levelColor, levelName, levelValues, total }) => {
 

  return (
    <div className="labels_L7Q row_S2v" style={{ width: "99%", paddingTop: "1rem", paddingBottom: "1rem" }}>
      <div style={{ width: "1%" }}></div>
      <div style={{ display: "flex", alignItems: "center", width: "22%" }}>
        <div className="symbol_aAH" style={{ backgroundColor: levelColor, width: "1.2em", height: "1.2em", marginRight: "0.5rem", borderRadius: "50%" }}></div>
        <div>{levelName}</div>
      </div>
      <div className="row_S2v" style={{ width: "11%", justifyContent: "center" }}>
        {total}
      </div>
      <div className="row_S2v" style={{ width: "11%", justifyContent: "center" }}>
        {levelValues.work}
      </div>
      <div className="row_S2v" style={{ width: "12%", justifyContent: "center" }}>
        {levelValues.elementary}
      </div>
      <div className="row_S2v" style={{ width: "12%", justifyContent: "center" }}>
        {levelValues.highSchool}
      </div>
      <div className="row_S2v" style={{ width: "12%", justifyContent: "center" }}>
        {levelValues.college}
      </div>
      <div className="row_S2v" style={{ width: "12%", justifyContent: "center" }}>
        {levelValues.university}
      </div>
      <div className="row_S2v" style={{ width: "12%", justifyContent: "center" }}>
        {levelValues.other}
      </div>
    </div>
  );
};

const Demographics: React.FC = () => {
  // State hooks for totals and details
  const [totals, setTotals] = useState<number[]>([]);
  const [details, setDetails] = useState<Info[]>([]);

  // State hooks for grouping and summary statistics visibility
  const [isGrouped, setIsGrouped] = useState<boolean>(false);
  const [showSummaryStats, setShowSummaryStats] = useState<boolean>(false); // New state

  // Fetch totals data using useDataUpdate hook with a safeguard
  useDataUpdate('populationInfo.structureTotals', (data) => setTotals(data || []));

  // Fetch details data using useDataUpdate hook with a safeguard
  useDataUpdate('populationInfo.structureDetails', (data) => setDetails(data || []));

  // Panel dimensions
  const panWidth = window.innerWidth * 0.20;
  const panHeight = window.innerHeight * 0.86;

  // Define per-bar height
  const BAR_HEIGHT = 40; // Increased from 30 to 40 px per bar

  // Calculate dynamic chart height with a new maximum limit
  const MAX_CHART_HEIGHT = 1200; // Increased from 600 to 1200 px

  // Prepare detailed data for Chart.js with grouping
  const detailedChartData = useMemo(() => {
    const groupedData = groupDetailsByAge(details);
    const sortedAges = Object.keys(groupedData)
      .map(Number)
      .sort((a, b) => a - b);

    return {
      labels: sortedAges.map((age) => `Age ${age}`),
      datasets: [
        {
          label: 'Work',
          data: sortedAges.map((age) => groupedData[age].work),
          backgroundColor: '#624532', // light brown
        },
        {
          label: 'Elementary',
          data: sortedAges.map((age) => groupedData[age].elementary),
          backgroundColor: '#7E9EAE', // pale lime
        },
        {
          label: 'High School',
          data: sortedAges.map((age) => groupedData[age].highSchool),
          backgroundColor: '#00C217', // mint green
        },
        {
          label: 'College',
          data: sortedAges.map((age) => groupedData[age].college),
          backgroundColor: '#005C4E', // turquoise
        },
        {
          label: 'University',
          data: sortedAges.map((age) => groupedData[age].university),
          backgroundColor: '#2462FF', // bright blue
        },
        {
          label: 'Other',
          data: sortedAges.map((age) => groupedData[age].other),
          backgroundColor: '#A1A1A1', // silver gray
        },
      ],
    };
  }, [details]);

  // Prepare grouped data for Chart.js
  const groupedChartData = useMemo(() => {
    const aggregated = aggregateDataByAgeRanges(details);

    return {
      labels: AGE_RANGES.map(range => range.label),
      datasets: [
        {
          label: 'Work',
          data: aggregated.map(range => range.work),
          backgroundColor: '#624532', // light brown
        },
        {
          label: 'Elementary',
          data: aggregated.map(range => range.elementary),
          backgroundColor: '#7E9EAE', // pale lime
        },
        {
          label: 'High School',
          data: aggregated.map(range => range.highSchool),
          backgroundColor: '#00C217', // mint green
        },
        {
          label: 'College',
          data: aggregated.map(range => range.college),
          backgroundColor: '#005C4E', // turquoise
        },
        {
          label: 'University',
          data: aggregated.map(range => range.university),
          backgroundColor: '#2462FF', // bright blue
        },
        {
          label: 'Other',
          data: aggregated.map(range => range.other),
          backgroundColor: '#A1A1A1', // silver gray
        },
      ],
    };
  }, [details]);

  // Determine the maximum value for the x-axis
  const maxTotal = useMemo(() => {
    const max = details.length > 0 ? Math.max(...details.map((info) => info.total)) : 100;
    console.log('Max Total:', max); // Debugging
    return max;
  }, [details]);

  // Chart options with aligned font settings
  const chartOptions = useMemo(() => ({
    indexAxis: 'y' as const, // Rotate the chart to horizontal
    responsive: true,
    maintainAspectRatio: false, // Allow the chart to fill the container
    plugins: {
      title: {
        display: true,
        text: 'Population Structure',
        color: 'white', // Set title color to white
        font: {
          family: commonFont.family,
          size: commonFont.size,
          weight: commonFont.weight,
        },
      },
      legend: {
        display: true, // Enable Chart.js built-in legend
        labels: {
          color: 'white', // Legend text color
          font: {
            family: commonFont.family,
            size: commonFont.size,
            weight: commonFont.weight,
          },
        },
      },
      tooltip: {
        backgroundColor: 'rgba(0, 0, 0, 0.7)', // Dark background for better visibility
        titleColor: 'white', // Tooltip title color
        bodyColor: 'white', // Tooltip body color
        titleFont: {
          family: commonFont.family,
          size: commonFont.size,
          weight: commonFont.weight,
        },
        bodyFont: {
          family: commonFont.family,
          size: commonFont.size,
          weight: commonFont.weight,
        },
        mode: 'index' as const,
        intersect: false,
      },
    },
    layout: {
      padding: {
        left: 20,
        right: 20,
        top: 20,
        bottom: 20,
      },
    },
    scales: {
      x: {
        stacked: true,
        title: {
          display: true,
          text: 'Number of People',
          color: 'white', // X-axis title color
          font: {
            family: commonFont.family,
            size: commonFont.size,
            weight: commonFont.weight,
          },
        },
        ticks: {
          color: 'white', // X-axis labels color
          font: {
            family: commonFont.family,
            size: commonFont.size - 4, // Slightly smaller for ticks
            weight: commonFont.weight,
          },
        },
        grid: {
          color: 'rgba(255, 255, 255, 0.1)', // X-axis grid lines color
        },
      },
      y: {
        stacked: true,
        beginAtZero: true,
        title: {
          display: true,
          text: 'Age Groups',
          color: 'white', // Y-axis title color
          font: {
            family: commonFont.family,
            size: commonFont.size,
            weight: commonFont.weight,
          },
        },
        ticks: {
          color: 'white', // Y-axis labels color
          font: {
            family: commonFont.family,
            size: commonFont.size - 4, // Slightly smaller for ticks
            weight: commonFont.weight,
          },
          autoSkip: false, // Ensure all labels are shown
        },
        grid: {
          color: 'rgba(255, 255, 255, 0.1)', // Y-axis grid lines color
        },
      },
    },
  }), [commonFont]);

  // Choose chart data based on isGrouped
  const chartDataToUse = useMemo(() => isGrouped ? groupedChartData : detailedChartData, [isGrouped, groupedChartData, detailedChartData]);

  // Calculate dynamic chart height with a new maximum limit
  const chartHeight = useMemo(() => {
    if (isGrouped) {
      return AGE_RANGES.length * BAR_HEIGHT; // Number of age ranges
    }
    // Limit detailed chart height to 1200px
    return Math.min(details.length * BAR_HEIGHT, MAX_CHART_HEIGHT);
  }, [isGrouped, details.length]);

  // Helper functions to calculate summary statistics
  const calculateAverageAge = (details: Info[]): number => {
    if (details.length === 0) return 0;
    const totalAge = details.reduce((sum, info) => sum + info.age, 0);
    return Math.round(totalAge / details.length);
  };

  const calculateMedianAge = (details: Info[]): number => {
    if (details.length === 0) return 0;
    const ages = [...details.map(info => info.age)].sort((a, b) => a - b);
    const mid = Math.floor(ages.length / 2);
    if (ages.length % 2 === 0) {
      return Math.round((ages[mid - 1] + ages[mid]) / 2);
    } else {
      return ages[mid];
    }
  };

  // Memoized summary statistics
  const averageAge = useMemo(() => calculateAverageAge(details), [details]);
  const medianAge = useMemo(() => calculateMedianAge(details), [details]);
  const totalWorkers = useMemo(() => details.reduce((sum, info) => sum + info.work, 0), [details]);

  // Calculate detailed summary statistics per age or age group
  const detailedSummaryStats = useMemo(() => {
    // Decide whether to group by individual age or by age ranges
    if (isGrouped) {
      const aggregated = aggregateDataByAgeRanges(details);
      return aggregated;
    } else {
      // Detailed per individual age
      const groupedData = groupDetailsByAge(details);
      const sortedAges = Object.keys(groupedData)
        .map(Number)
        .sort((a, b) => a - b);

      return sortedAges.map(age => ({
        label: `Age ${age}`,
        work: groupedData[age].work,
        elementary: groupedData[age].elementary,
        highSchool: groupedData[age].highSchool,
        college: groupedData[age].college,
        university: groupedData[age].university,
        other: groupedData[age].other,
      }));
    }
  }, [details, isGrouped]);

  // Define a function to handle keypress on the toggle button for accessibility
  const handleToggleKeyPress = (e: KeyboardEvent<HTMLButtonElement>) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      setIsGrouped(prev => !prev);
    }
  };

  // Define a function to handle keypress on the summary stats button for accessibility
  const handleSummaryStatsKeyPress = (e: KeyboardEvent<HTMLButtonElement>) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      setShowSummaryStats(prev => !prev);
    }
  };

  // Debugging: Log labels and datasets to verify alignment
  useEffect(() => {
    chartDataToUse.datasets.forEach((dataset, index) => {
      // You can add logging here if needed
      // console.log(`Dataset ${index}:`, dataset);
    });
  }, [chartDataToUse]);

  return (
    <$Panel
      react={React}
      title="Demographics"
      initialSize={{ width: panWidth, height: panHeight }}
      initialPosition={{ top: window.innerHeight * 0.009, left: window.innerWidth * 0.053 }}
      style={{
        backgroundColor: 'var(--panelColorNormal)', // Use CSS variables or appropriate color
        display: 'flex',
        flexDirection: 'column',
        overflow: 'hidden', // Prevent panel from overflowing
      }}
    >
      {/* Summary Paragraphs */}
      <div style={{ flex: '0 0 auto', display: 'flex', flexDirection: 'row', width: '100%' }}>
        <div style={{ width: '50%' }}>
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
        <div style={{ width: '50%', paddingLeft: '4rem' }}>
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
      <div style={{ flex: '0 0 auto', display: 'flex', justifyContent: 'center', gap: '1rem' }}>
        {/* Existing Grouped/Detailed View Toggle Button */}
        <button
          onClick={() => setIsGrouped(prev => !prev)}
          onKeyPress={handleToggleKeyPress}
          style={{
            padding: '0.5rem 1rem',
            backgroundColor: '#34495e',
            color: 'white',
            border: 'none',
            borderRadius: '4px',
            cursor: 'pointer',
            fontSize: '14px',
          }}
          aria-pressed={isGrouped}
          aria-label={isGrouped ? 'Show Detailed View' : 'Show Grouped View'}
        >
          {isGrouped ? 'Show Detailed View' : 'Show Grouped View'}
        </button>

        {/* New Summary Statistics Toggle Button */}
        <button
          onClick={() => setShowSummaryStats(prev => !prev)}
          onKeyPress={handleSummaryStatsKeyPress}
          style={{
            padding: '0.5rem 1rem',
            backgroundColor: '#34495e',
            color: 'white',
            border: 'none',
            borderRadius: '4px',
            cursor: 'pointer',
            fontSize: '14px',
          }}
          aria-pressed={showSummaryStats}
          aria-label={showSummaryStats ? 'Hide Summary Statistics' : 'Show Summary Statistics'}
        >
          {showSummaryStats ? 'Hide Summary Stats' : 'Show Summary Stats'}
        </button>
      </div>

      {/* Spacer */}
      <div style={{ flex: '0 0 auto', height: '1rem' }}></div>

      {/* Conditionally Render Summary Statistics */}
      {showSummaryStats && (
  <div
    style={{
      flex: '0 0 auto',
      padding: '1rem',
      backgroundColor: 'rgba(0, 0, 0, 0.5)', // Semi-transparent background
      borderRadius: '4px',
      margin: '0 2rem', // Horizontal margin for spacing
      overflow: 'hidden', // Hide overflow initially
      maxHeight: '300px', // Set a maximum height for the scrollable area
    }}
  >
    <h3 style={{ color: 'white', marginBottom: '0.5rem' }}>Summary Statistics</h3>

    {/* Scrollable Container */}
    <div
      style={{
        overflowY: 'auto', // Enable vertical scrolling
        maxHeight: '250px', // Adjust based on available space
        paddingRight: '10px', // Space for scrollbar
      }}
    >
      {/* Header Row */}
      <div className="labels_L7Q row_S2v" style={{ width: "100%", paddingTop: "1rem", paddingBottom: "1rem", borderBottom: "1px solid white" }}>
  <div style={{ width: "1%" }}></div>
  <div style={{ display: "flex", alignItems: "center", width: "22%" }}>
    <div>Age Group</div>
  </div>
  <div className="row_S2v" style={{ width: "11%", justifyContent: "center" }}>
    Total
  </div>
  <div className="row_S2v" style={{ width: "11%", justifyContent: "center" }}>
    Work
  </div>
  <div className="row_S2v" style={{ width: "12%", justifyContent: "center" }}>
    Elementary
  </div>
  <div className="row_S2v small_ExK" style={{ width: "9%", justifyContent: "center" }}>
    High School
  </div>
  <div className="row_S2v small_ExK" style={{ width: "9%", justifyContent: "center" }}>
    College
  </div>
  <div className="row_S2v small_ExK" style={{ width: "9%", justifyContent: "center" }}>
    University
  </div>
  <div className="row_S2v small_ExK" style={{ width: "9%", justifyContent: "center" }}>
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
    total={stat.work + stat.elementary + stat.highSchool + stat.college + stat.university + stat.other}
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

      {/* DEBUG Section (Commented Out) */}
      {/*
      <div>
        {details.map(info => (
          <p style={{ fontSize: '75%', color: 'white' }} key={info.age}> 
            {info.age} {info.total} {info.school1} {info.school2} {info.school3} {info.school4} {info.work} {info.other}
          </p>
        ))}
      </div>
      */}
    </$Panel>
  );
}

// Helper function to capitalize strings
const capitalize = (s: string) => {
  if (typeof s !== 'string') return '';
  return s.charAt(0).toUpperCase() + s.slice(1);
};

// Helper function to get distinct colors for datasets
const getColor = (index: number) => {
  const colors = [
    '#624532', // light brown
    '#7E9EAE', // pale lime
    '#00C217', // mint green
    '#005C4E', // turquoise
    '#2462FF', // bright blue
    '#A1A1A1', // silver gray
    '#FF5733', // orange
    '#C70039', // red
    '#900C3F', // dark red
    '#581845', // purple
  ];
  return colors[index % colors.length];
};

export default Demographics;
