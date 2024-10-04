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
  age: number;
  total: number;
  work: number;
  school1: number;
  school2: number;
  school3: number;
  school4: number;
  other: number;
}

// Define a common font configuration
const commonFont = {
  family: 'Arial, sans-serif', // Replace with your desired font family
  size: 14, // Base font size in pixels
  weight: 'normal' as const, // Font weight
};

const AlignedParagraph: React.FC<AlignedParagraphProps> = ({ left, right }) => {
  return (
    <div
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

// Function to group details by age
const groupDetailsByAge = (details: Info[]) => {
  return details.reduce((acc: Record<number, Info>, info: Info) => {
    const age = info.age;
    if (!acc[age]) {
      acc[age] = { ...info };
    } else {
      acc[age].work += info.work;
      acc[age].school1 += info.school1;
      acc[age].school2 += info.school2;
      acc[age].school3 += info.school3;
      acc[age].school4 += info.school4;
      acc[age].other += info.other;
      acc[age].total += info.total;
    }
    return acc;
  }, {});
};

const Demographics: React.FC = () => {
  // State hooks for totals and details
  const [totals, setTotals] = useState<number[]>([]);
  const [details, setDetails] = useState<Info[]>([]);

  // State hook for grouping
  const [isGrouped, setIsGrouped] = useState<boolean>(false);

  // Fetch totals data using useDataUpdate hook with a safeguard
  useDataUpdate('populationInfo.structureTotals', (data) => setTotals(data || []));

  // Fetch details data using useDataUpdate hook with a safeguard
  useDataUpdate('populationInfo.structureDetails', (data) => setDetails(data || []));

  // Close handler
  

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
          data: sortedAges.map((age) => groupedData[age].school1),
          backgroundColor: '#7E9EAE', // pale lime
        },
        {
          label: 'High School',
          data: sortedAges.map((age) => groupedData[age].school2),
          backgroundColor: '#00C217', // mint green
        },
        {
          label: 'College',
          data: sortedAges.map((age) => groupedData[age].school3),
          backgroundColor: '#005C4E', // turquoise
        },
        {
          label: 'University',
          data: sortedAges.map((age) => groupedData[age].school4),
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
    // Define age ranges
    const ageRanges = [
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

    // Initialize aggregated data
    const aggregated = ageRanges.map(range => ({
      label: range.label,
      work: 0,
      school1: 0,
      school2: 0,
      school3: 0,
      school4: 0,
      other: 0,
      total: 0,
    }));

    // Aggregate data
    details.forEach(info => {
      ageRanges.forEach((range, index) => {
        if (info.age >= range.min && info.age < range.max) {
          aggregated[index].work += info.work;
          aggregated[index].school1 += info.school1;
          aggregated[index].school2 += info.school2;
          aggregated[index].school3 += info.school3;
          aggregated[index].school4 += info.school4;
          aggregated[index].other += info.other;
          aggregated[index].total += info.total;
        } else if (info.age === range.max && range.max === 100) {
          // Include age 100 in the last group
          aggregated[index].work += info.work;
          aggregated[index].school1 += info.school1;
          aggregated[index].school2 += info.school2;
          aggregated[index].school3 += info.school3;
          aggregated[index].school4 += info.school4;
          aggregated[index].other += info.other;
          aggregated[index].total += info.total;
        }
      });
    });

    return {
      labels: ageRanges.map(range => range.label),
      datasets: [
        {
          label: 'Work',
          data: aggregated.map(range => range.work),
          backgroundColor: '#624532', // light brown
        },
        {
          label: 'Elementary',
          data: aggregated.map(range => range.school1),
          backgroundColor: '#7E9EAE', // pale lime
        },
        {
          label: 'High School',
          data: aggregated.map(range => range.school2),
          backgroundColor: '#00C217', // mint green
        },
        {
          label: 'College',
          data: aggregated.map(range => range.school3),
          backgroundColor: '#005C4E', // turquoise
        },
        {
          label: 'University',
          data: aggregated.map(range => range.school4),
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
      return 10 * BAR_HEIGHT; // 10 age ranges
    }
    // Limit detailed chart height to 1200px
    return Math.min(details.length * BAR_HEIGHT, MAX_CHART_HEIGHT);
  }, [isGrouped, details.length]);

  // Debugging: Check if data is loaded
  useEffect(() => {
    
  }, [totals, details]);

  // Define a function to handle keypress on the toggle button for accessibility
  const handleToggleKeyPress = (e: KeyboardEvent<HTMLButtonElement>) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      setIsGrouped(prev => !prev);
    }
  };

  // Debugging: Log labels and datasets to verify alignment
  useEffect(() => {
    
    chartDataToUse.datasets.forEach((dataset, index) => {
      
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

      {/* Toggle Button */}
      <div style={{ flex: '0 0 auto', display: 'flex', justifyContent: 'center' }}>
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
      </div>

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
