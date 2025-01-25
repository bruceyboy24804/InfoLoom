// TradeCosts.tsx

import React, { useState, useCallback, FC, useMemo, useRef, useEffect } from 'react';
import $Panel from 'mods/panel'; // Custom Panel component
import { Button, Dropdown, DropdownToggle } from 'cs2/ui'; // Using only Button and Dropdown components
import { InfoCheckbox } from 'mods/InfoCheckbox/InfoCheckbox';
import { getModule } from 'cs2/modding';
import styles from './TradeCost.module.scss';
import { ResourceIcon } from 'mods/InfoLoomSections/CommercialSecction/CommercialProductsUI/resourceIcons';
import { formatWords } from 'mods/InfoLoomSections/utils/formatText';
import { bindValue, useValue } from 'cs2/api';
import mod from 'mod.json';
import Chart from 'chart.js/auto'; // Import Chart.js

const DropdownStyle = getModule("game-ui/menu/themes/dropdown.module.scss", "classes");

interface ResourceTradeCost {
  Resource: string;
  BuyCost: number;
  SellCost: number;
  Count: number;
}

const TradeCosts$ = bindValue<ResourceTradeCost[]>(mod.id, 'tradeCosts', []);

interface TradeCostsProps {
  onClose: () => void;
}

type ShowColumnsType = {
  buyCost: boolean;
  sellCost: boolean;
  profit: boolean;
  profitMargin: boolean;
};

interface ResourceLineProps {
  data: ResourceTradeCost;
  showColumns: ShowColumnsType;
}

type ViewType = 'table' | 'graph';

const ResourceLine: React.FC<ResourceLineProps> = ({ data, showColumns }) => {
  const formattedResourceName = formatWords(data.Resource, true);
  const profit = data.SellCost - data.BuyCost;
  const profitMargin = data.BuyCost !== 0 ? (profit / data.BuyCost) * 100 : 0;

  // Determine the CSS class based on profit value
  let profitClass = styles.neutral_blue; // Default to blue for zero
  if (profit < 0) {
    profitClass = styles.negative_YWY;
  } else if (profit > 0) {
    profitClass = styles.positive_zrK;
  }

  // Similarly for profitMargin
  let profitMarginClass = styles.neutral_blue; // Default to blue for zero
  if (profitMargin < 0) {
    profitMarginClass = styles.negative_YWY;
  } else if (profitMargin > 0) {
    profitMarginClass = styles.positive_zrK;
  }

  return (
    <div className={styles.row_S2v}>
      <div className={styles.cell} style={{ width: '3%' }}></div>
      <div className={styles.cell} style={{ width: '25%', justifyContent: 'flex-start', gap: '8px' }}>
        <ResourceIcon resourceName={data.Resource} />
        <span>{formattedResourceName}</span>
      </div>
      {showColumns.buyCost && (
        <div className={styles.cell} style={{ width: '15%' }}>
          {data.BuyCost.toFixed(2)}
        </div>
      )}
      {showColumns.sellCost && (
        <div className={styles.cell} style={{ width: '15%' }}>
          {data.SellCost.toFixed(2)}
        </div>
      )}
      {showColumns.profit && (
        <div className={styles.cell} style={{ width: '15%' }}>
          <span className={profitClass}>{profit.toFixed(2)}</span>
        </div>
      )}
      {showColumns.profitMargin && (
        <div className={styles.cell} style={{ width: '15%' }}>
          <span className={profitMarginClass}>{profitMargin.toFixed(2)}%</span>
        </div>
      )}
    </div>
  );
};

const TableHeader: React.FC<{ showColumns: ShowColumnsType }> = ({ showColumns }) => {
  return (
    <div className={styles.headerRow}>
      <div className={styles.headerCell} style={{ width: '3%' }}></div>
      <div className={styles.headerCell} style={{ width: '25%' }}>
        Resource
      </div>
      {showColumns.buyCost && (
        <div className={styles.headerCell} style={{ width: '15%' }}>
          Buy Cost
        </div>
      )}
      {showColumns.sellCost && (
        <div className={styles.headerCell} style={{ width: '15%' }}>
          Sell Cost
        </div>
      )}
      {showColumns.profit && (
        <div className={styles.headerCell} style={{ width: '15%' }}>
          Profit
        </div>
      )}
      {showColumns.profitMargin && (
        <div className={styles.headerCell} style={{ width: '15%' }}>
          Profit Margin
        </div>
      )}
    </div>
  );
};

/**
 * TradeCostsGraph component renders the Floating Bar Chart for Trade Costs using Chart.js.
 * Receives selectedResources as props to filter displayed data.
 */
interface TradeCostsGraphProps {
  selectedResources: Set<string>;
}

const TradeCostsGraph: FC<TradeCostsGraphProps> = ({ selectedResources }) => {
  const tradeCosts = useValue(TradeCosts$);
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const chartRef = useRef<Chart | null>(null);

  // Filter tradeCosts based on selectedResources
  const filteredTradeCosts = useMemo(() => {
    return tradeCosts.filter(item => selectedResources.has(item.Resource));
  }, [tradeCosts, selectedResources]);

  // Prepare chart data
  const chartData = useMemo(() => {
    const labels = filteredTradeCosts.map(item => formatWords(item.Resource, true));
    const buyCosts = filteredTradeCosts.map(item => item.BuyCost);
    const sellCosts = filteredTradeCosts.map(item => item.SellCost - item.BuyCost); // Difference

    const sellBarColors = filteredTradeCosts.map(item =>
      item.SellCost >= item.BuyCost ? 'rgba(255, 99, 132, 0.5)' : 'rgba(75, 192, 192, 0.5)'
    );

    const sellBarBorderColors = filteredTradeCosts.map(item =>
      item.SellCost >= item.BuyCost ? 'rgba(255, 99, 132, 1)' : 'rgba(75, 192, 192, 1)'
    );

    return {
      labels: labels,
      datasets: [
        {
          label: 'Buy Cost',
          data: buyCosts,
          backgroundColor: 'rgba(75, 192, 192, 0.5)', // Teal color
          borderColor: 'rgba(75, 192, 192, 1)',
          borderWidth: 1,
          stack: 'Stack 0',
        },
        {
          label: 'Sell Cost',
          data: sellCosts,
          backgroundColor: sellBarColors,
          borderColor: sellBarBorderColors,
          borderWidth: 1,
          stack: 'Stack 0',
        },
      ],
    };
  }, [filteredTradeCosts]);

  // Initialize Chart.js instance once
  useEffect(() => {
    if (!canvasRef.current) return;

    const ctx = canvasRef.current.getContext('2d');
    if (!ctx) {
      console.error('Failed to get 2D context for canvas');
      return;
    }

    // Create Chart instance
    chartRef.current = new Chart(ctx, {
      type: 'bar',
      data: chartData,
      options: {
        responsive: true,
        maintainAspectRatio: false,
        scales: {
          x: {
            type: 'category',
            title: {
              display: true,
              text: 'Resource',
              color: 'white',
              font: {
                size: 14,
                family: 'Arial, sans-serif',
              },
            },
            ticks: {
              color: 'white',
              font: {
                size: 12,
                family: 'Arial, sans-serif',
              },
            },
            grid: {
              color: 'rgba(255, 255, 255, 0.1)',
            },
          },
          y: {
            stacked: false, // Enable stacking
            beginAtZero: true,
            title: {
              display: true,
              text: 'Cost ($)',
              color: 'white',
              font: {
                size: 14,
                family: 'Arial, sans-serif',
              },
            },
            ticks: {
              color: 'white',
              font: {
                size: 12,
                family: 'Arial, sans-serif',
              },
            },
          },
        },
        plugins: {
          tooltip: {
            callbacks: {
              label: function (context: any) {
                const label = context.dataset.label || '';
                const value = context.parsed.y;
                if (label === 'Buy Cost') {
                  return `Buy Cost: $${value.toFixed(2)}`;
                } else if (label === 'Sell Cost') {
                  const resource = context.label;
                  const totalSellCost = tradeCosts.find(item => formatWords(item.Resource, true) === resource)?.SellCost || 0;
                  return `Sell Cost: $${totalSellCost.toFixed(2)}`;
                }
                return `${label}: $${value.toFixed(2)}`;
              },
            },
          },
          legend: {
            display: true,
            labels: {
              color: 'white',
              font: {
                size: 14,
                family: 'Arial, sans-serif',
              },
            },
          },
          title: {
            display: true,
            text: 'Trade Costs Range Chart',
            color: 'white',
            font: {
              size: 18,
              family: 'Arial, sans-serif',
            },
          },
        },
        interaction: {
          mode: 'index',
          intersect: false,
        },

        animation: {
          duration: 0, // Disable animations to prevent flickering
        },
      },
    });

    return () => {
      // Cleanup on unmount
      if (chartRef.current) {
        chartRef.current.destroy();
      }
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []); // Empty dependency array ensures this runs only once

  // Update Chart data when filteredTradeCosts change
  useEffect(() => {
    if (!chartRef.current) return;

    chartRef.current.data = chartData;
    chartRef.current.update();
  }, [chartData]);

  // Handle canvas resizing
  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const resizeCanvas = () => {
      const parent = canvas.parentElement;
      if (parent) {
        canvas.width = parent.clientWidth;
        canvas.height = parent.clientHeight;
        if (chartRef.current) {
          chartRef.current.resize();
        }
      }
    };

    resizeCanvas();
    window.addEventListener('resize', resizeCanvas);

    return () => {
      window.removeEventListener('resize', resizeCanvas);
    };
  }, []);

  return (
    <div className={styles.graphContainer}>
      {filteredTradeCosts.length === 0 ? (
        <p className={styles.loadingText}>No resources selected to display the graph.</p>
      ) : (
        <canvas ref={canvasRef} style={{ width: '100%', height: '100%', backgroundColor: '#2c2c2c' }} />
      )}
    </div>
  );
};

/**
 * Main TradeCosts Component
 */
const $TradeCosts: FC<TradeCostsProps> = ({ onClose }) => {
  const tradeCosts = useValue(TradeCosts$);

  const [showColumns, setShowColumns] = useState<ShowColumnsType>({
    buyCost: true,
    sellCost: true,
    profit: true,
    profitMargin: true,
  });

  const [sortBy, setSortBy] = useState<'name' | 'buyCost' | 'sellCost' | 'profit' | 'profitMargin'>('name');

  // State to handle current view: 'table' or 'graph'
  const [view, setView] = useState<ViewType>('table');

  // State to track selected resources for the graph
  const [selectedResources, setSelectedResources] = useState<Set<string>>(() => {
    // Initialize with all resources selected
    return new Set(tradeCosts.map(item => item.Resource));
  });

  const toggleColumn = useCallback((column: keyof ShowColumnsType) => {
    setShowColumns(prev => ({
      ...prev,
      [column]: !prev[column],
    }));
  }, []);

  const sortData = useCallback(
    (a: ResourceTradeCost, b: ResourceTradeCost) => {
      switch (sortBy) {
        case 'name':
          return a.Resource.localeCompare(b.Resource);
        case 'buyCost':
          return b.BuyCost - a.BuyCost;
        case 'sellCost':
          return b.SellCost - a.SellCost;
        case 'profit':
          return (b.SellCost - b.BuyCost) - (a.SellCost - a.BuyCost);
        case 'profitMargin':
          const profitMarginA = a.BuyCost !== 0 ? ((a.SellCost - a.BuyCost) / a.BuyCost) * 100 : 0;
          const profitMarginB = b.BuyCost !== 0 ? ((b.SellCost - b.BuyCost) / b.BuyCost) * 100 : 0;
          return profitMarginB - profitMarginA;
        default:
          return 0;
      }
    },
    [sortBy]
  );

  const handleOpenGraph = useCallback(() => {
    setView('graph');
  }, []);

  const handleBackToTable = useCallback(() => {
    setView('table');
  }, []);

  const handleClose = useCallback(() => {
    onClose();
  }, [onClose]);

  const sortedTradeCosts = useMemo(() => {
    return [...tradeCosts].sort(sortData);
  }, [tradeCosts, sortData]);

  /**
   * Initialize selectedResources only once when tradeCosts are loaded.
   * Prevents resetting selectedResources on every tradeCosts update.
   */
  useEffect(() => {
    if (tradeCosts.length > 0 && selectedResources.size === 0) {
      setSelectedResources(new Set(tradeCosts.map(item => item.Resource)));
    }
  }, [tradeCosts, selectedResources]);

  // Handler for toggling resource selection
  const handleToggleResource = useCallback((resource: string) => {
    setSelectedResources(prev => {
      const newSet = new Set(prev);
      if (newSet.has(resource)) {
        newSet.delete(resource);
      } else {
        newSet.add(resource);
      }
      return newSet;
    });
  }, []);

  // Panel dimensions
  const panelWidth = window.innerWidth * 0.6;
  const panelHeight = window.innerHeight * 0.7;

  // Panel style
  const panelStyle: React.CSSProperties = {
    backgroundColor: 'var(--panelColorNormal)',
    display: 'flex',
    flexDirection: 'column',
    overflow: 'hidden', // Ensure that child components handle overflow
    padding: '1rem',
  };

  return (
    <$Panel
      id="infoloom-trade-costs"
      title="Trade Costs"
      onClose={handleClose}
      initialSize={{ width: panelWidth, height: panelHeight }}
      initialPosition={{
        top: window.innerHeight * 0.05,
        left: window.innerWidth * 0.2,
      }}
      style={panelStyle}
    >
      {tradeCosts.length === 0 ? (
        <p className={styles.loadingText}>Loading Trade Costs...</p>
      ) : (
        <div className={styles.panelContent}>
          {/* Render either the table view or the graph view */}
          {view === 'table' ? (
            <>
              {/* Controls: Sort Options, Column Options, Graphs Button */}
              <div className={styles.controls}>
                <Dropdown
                  theme={DropdownStyle}
                  content={
                    <div className={styles.dropdownContent}>
                      <div className={styles.dropdownItem}>
                        <InfoCheckbox
                          label="Sort by Name"
                          isChecked={sortBy === 'name'}
                          onToggle={() => setSortBy('name')}
                        />
                      </div>
                      <div className={styles.dropdownItem}>
                        <InfoCheckbox
                          label="Sort by Buy Cost"
                          isChecked={sortBy === 'buyCost'}
                          onToggle={() => setSortBy('buyCost')}
                        />
                      </div>
                      <div className={styles.dropdownItem}>
                        <InfoCheckbox
                          label="Sort by Sell Cost"
                          isChecked={sortBy === 'sellCost'}
                          onToggle={() => setSortBy('sellCost')}
                        />
                      </div>
                      <div className={styles.dropdownItem}>
                        <InfoCheckbox
                          label="Sort by Profit"
                          isChecked={sortBy === 'profit'}
                          onToggle={() => setSortBy('profit')}
                        />
                      </div>
                      <div className={styles.dropdownItem}>
                        <InfoCheckbox
                          label="Sort by Profit Margin"
                          isChecked={sortBy === 'profitMargin'}
                          onToggle={() => setSortBy('profitMargin')}
                        />
                      </div>
                    </div>
                  }
                >
                  <DropdownToggle className={styles.dropdownToggle}>
                    Sort Options
                  </DropdownToggle>
                </Dropdown>

                <Dropdown
                  theme={DropdownStyle}
                  content={
                    <div className={styles.dropdownContent}>
                      <div className={styles.dropdownItem}>
                        <InfoCheckbox
                          label="Show Buy Cost"
                          isChecked={showColumns.buyCost}
                          onToggle={() => toggleColumn('buyCost')}
                        />
                      </div>
                      <div className={styles.dropdownItem}>
                        <InfoCheckbox
                          label="Show Sell Cost"
                          isChecked={showColumns.sellCost}
                          onToggle={() => toggleColumn('sellCost')}
                        />
                      </div>
                      <div className={styles.dropdownItem}>
                        <InfoCheckbox
                          label="Show Profit"
                          isChecked={showColumns.profit}
                          onToggle={() => toggleColumn('profit')}
                        />
                      </div>
                      <div className={styles.dropdownItem}>
                        <InfoCheckbox
                          label="Show Profit Margin"
                          isChecked={showColumns.profitMargin}
                          onToggle={() => toggleColumn('profitMargin')}
                        />
                      </div>
                    </div>
                  }
                >
                  <DropdownToggle className={styles.dropdownToggle}>
                    Column Options
                  </DropdownToggle>
                </Dropdown>

                {/* "Graph" Button with .graphButton class */}
                <Button onClick={handleOpenGraph} className={styles.graphButton}>
                  Trade Cost Graph
                </Button>
              </div>

              {/* Table Header */}
              <TableHeader showColumns={showColumns} />

              {/* Table Body */}
              <div className={styles.tableBody}>
                {sortedTradeCosts.map((item) => (
                  <ResourceLine
                    key={item.Resource}
                    data={item}
                    showColumns={showColumns}
                  />
                ))}
              </div>
            </>
          ) : (
            // Graph View
            <>
              {/* Container for Checkboxes and Graph */}
              <div className={styles.graphViewContainer}>
                {/* Checkboxes for Each Resource */}
                <div className={styles.resourceCheckboxes}>
                  <h3 className={styles.checkboxHeader}>Select Resources:</h3>
                  {tradeCosts.map((item) => (
                    <div key={item.Resource} className={styles.checkboxItem}>
                      <InfoCheckbox
                        label={
                          <div className={styles.checkboxLabel}>
                            <ResourceIcon resourceName={item.Resource} />
                            <span>{formatWords(item.Resource, true)}</span>
                          </div>
                        }
                        isChecked={selectedResources.has(item.Resource)}
                        onToggle={() => handleToggleResource(item.Resource)}
                      />
                    </div>
                  ))}
                </div>

                {/* Graph Area */}
                <div className={styles.graphArea}>
                  {/* Back Button with .backButton class */}
                  <div className={styles.graphHeader}>
                    <Button onClick={handleBackToTable} className={styles.backButton}>
                      &larr; Back to Table
                    </Button>
                  </div>

                  {/* TradeCostsGraph with selectedResources passed as props */}
                  <TradeCostsGraph selectedResources={selectedResources} />
                </div>
              </div>
            </>
          )}
        </div>
      )}
    </$Panel>
  );
};

export default $TradeCosts;
