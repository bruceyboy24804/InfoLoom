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

const DropdownStyle = getModule('game-ui/menu/themes/dropdown.module.scss', 'classes');

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
      <div
        className={styles.cell}
        style={{ width: '25%', justifyContent: 'flex-start', gap: '8px' }}
      >
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
  const chartRef = useRef<Chart<'bar', { x: string; y: number[] }[], string> | null>(null);
  const containerRef = useRef<HTMLDivElement | null>(null);

  // Filter tradeCosts based on selectedResources
  const filteredTradeCosts = useMemo(() => {
    return tradeCosts.filter(item => selectedResources.has(item.Resource));
  }, [tradeCosts, selectedResources]);

  // Prepare chart data
  const chartData = useMemo(() => {
    const labels = filteredTradeCosts.map(item => formatWords(item.Resource, true));
    const floatingBars = filteredTradeCosts.map(item => ({
      x: item.Resource,
      y: [item.BuyCost === item.SellCost ? 0 : item.BuyCost, item.SellCost],
    }));

    return {
      labels: labels,
      datasets: [
        {
          label: 'Trade Costs Range',
          data: floatingBars,
          backgroundColor: (context: any) => {
            const { y } = context.raw;
            if (y[0] === 0 || y[1] === 0) return 'rgb(255, 211, 80)';
            return y[0] < y[1] ? 'rgba(68, 151, 130, 1)' : 'rgba(223, 72, 76, 1)';
          },
          borderColor: (context: any) => {
            const { y } = context.raw;
            if (y[0] === 0 || y[1] === 0) return 'rgb(255, 211, 80)';
            return y[0] < y[1] ? 'rgba(68, 151, 130, 1)' : 'rgba(223, 72, 76, 1)';
          },
          borderWidth: 1,
        },
      ],
    };
  }, [filteredTradeCosts]);

  // Initialize Chart.js instance
  useEffect(() => {
    if (!canvasRef.current || !containerRef.current) return;
    if (filteredTradeCosts.length === 0) return;

    const ctx = canvasRef.current.getContext('2d');
    if (!ctx) return;

    // Destroy existing chart
    if (chartRef.current) {
      chartRef.current.destroy();
    }

    // Create new Chart instance
    chartRef.current = new Chart(ctx, {
      type: 'bar',
      data: chartData,
      options: {
        responsive: true,
        maintainAspectRatio: false,
        scales: {
          x: {
            type: 'category',
            title: { display: true, text: 'Resource', color: 'white' },
            ticks: { color: 'white' },
            grid: { color: 'rgba(255, 255, 255, 0.1)' },
          },
          y: {
            type: 'linear',
            title: { display: true, text: 'Cost ($)', color: 'white' },
            ticks: { color: 'white' },
          },
        },
        plugins: {
          tooltip: {
            callbacks: {
              label: (context: any) => {
                const range = context.raw.y;
                return range[0] === 0
                  ? `Buy/Sell Price: $${range[1].toFixed(2)}`
                  : `Buy: $${range[0].toFixed(2)}, Sell: $${range[1].toFixed(2)}`;
              },
            },
          },
          legend: { display: false },
          title: { display: true, text: 'Trade Costs Range Chart', color: 'white' },
        },
        interaction: { mode: 'index', intersect: false },
        animation: { duration: 0 },
      },
    });

    return () => {
      if (chartRef.current) {
        chartRef.current.destroy();
        chartRef.current = null;
      }
    };
  }, []);

  useEffect(() => {
    if (!chartRef.current) return;

    // update silently
    chartRef.current.data = chartData;
    chartRef.current.update('none');
  }, [chartData]);

  // Handle container resizing with ResizeObserver
  useEffect(() => {
    if (!containerRef.current || !canvasRef.current) return;

    const resizeObserver = new ResizeObserver(entries => {
      const entry = entries[0];
      const { width, height } = entry.contentRect;

      const offscreenCanvas = document.createElement('canvas');
      const offscreenCtx = offscreenCanvas.getContext('2d')!;

      // sync Canvas
      offscreenCanvas.width = width;
      offscreenCanvas.height = height;

      // copy to
      offscreenCtx.drawImage(canvasRef.current!, 0, 0);

      // apply
      canvasRef.current!.width = width;
      canvasRef.current!.height = height;

      // restore
      const ctx = canvasRef.current!.getContext('2d')!;
      ctx.drawImage(offscreenCanvas, 0, 0);

      // resize
      if (chartRef.current) {
        chartRef.current.resize();
      }
    });

    resizeObserver.observe(containerRef.current);
    return () => resizeObserver.disconnect();
  }, []);

  return (
    <div className={styles.graphContainer} ref={containerRef}>
      {filteredTradeCosts.length === 0 ? (
        <p className={styles.loadingText}>No resources selected to display the graph.</p>
      ) : (
        <canvas ref={canvasRef} />
      )}
    </div>
  );
};

// memo!
const MemoizedTradeCostsGraph = React.memo(TradeCostsGraph, (prevProps, nextProps) => {
  // compare selectedResources deeply
  return (
    prevProps.selectedResources.size === nextProps.selectedResources.size &&
    [...prevProps.selectedResources].every(res => nextProps.selectedResources.has(res))
  );
});

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

  const [sortBy, setSortBy] = useState<'name' | 'buyCost' | 'sellCost' | 'profit' | 'profitMargin'>(
    'name'
  );

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
          return b.SellCost - b.BuyCost - (a.SellCost - a.BuyCost);
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
                  <DropdownToggle className={styles.dropdownToggle}>Sort Options</DropdownToggle>
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
                  <DropdownToggle className={styles.dropdownToggle}>Column Options</DropdownToggle>
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
                {sortedTradeCosts.map(item => (
                  <ResourceLine key={item.Resource} data={item} showColumns={showColumns} />
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
                  {tradeCosts.map(item => (
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
                  <MemoizedTradeCostsGraph selectedResources={selectedResources} />
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