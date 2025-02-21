import React, {
  useState,
  useCallback,
  FC,
  useMemo,
  useRef,
  useEffect,
} from 'react';
import {
  Button,
  DraggablePanelProps,
  Dropdown,
  DropdownToggle,
  Number2,
  Panel,
  Scrollable,
} from 'cs2/ui';
import { InfoCheckbox } from 'mods/components/InfoCheckbox/InfoCheckbox';
import { getModule } from 'cs2/modding';
import styles from './TradeCost.module.scss';
import { ResourceIcon } from 'mods/InfoLoomSections/CommercialSecction/CommercialProductsUI/resourceIcons';
import { formatWords } from 'mods/InfoLoomSections/utils/formatText';
import { bindValue, useValue } from 'cs2/api';
import mod from 'mod.json';
import Chart from 'chart.js/auto';
import { ResourceTradeCost, ImportData, ExportData } from 'mods/domain/tradeCostData';
import {TradeCostsDataExports, TradeCostsDataImports, TradeCostsData} from "../../bindings";

const DropdownStyle = getModule('game-ui/menu/themes/dropdown.module.scss', 'classes');

type SortOption =
  | 'name'
  | 'buyCost'
  | 'sellCost'
  | 'profit'
  | 'profitMargin'
  | 'importAmount'
  | 'exportAmount';

export type ShowColumnsType = {
  buyCost: boolean;
  sellCost: boolean;
  profit: boolean;
  profitMargin: boolean;
  importAmount: boolean;
  exportAmount: boolean;
};

interface ResourceLineProps {
  data: ResourceTradeCost;
  showColumns: ShowColumnsType;
}

type ViewType = 'table' | 'graph';

interface TradeCostsGraphProps {
  selectedResources: Set<string>;
}
const DataDivider: React.FC = () => (
    <div style={{display: 'flex', height: '4rem', flexDirection: 'column', justifyContent: 'center'}}>
        <div style={{borderBottom: '1px solid gray', width: '100%'}}></div>
    </div>
);


const calculateProfit = (data: ResourceTradeCost) => data.SellCost - data.BuyCost;

const calculateProfitMargin = (data: ResourceTradeCost) =>
  data.BuyCost !== 0 ? (calculateProfit(data) / data.BuyCost) * 100 : 0;

const getProfitClass = (profit: number): string => {
  if (profit < 0) return styles.negative_YWY;
  if (profit > 0) return styles.positive_zrK;
  return styles.neutral_blue;
};

const getProfitMarginClass = (profitMargin: number): string => {
  if (profitMargin < 0) return styles.negative_YWY;
  if (profitMargin > 0) return styles.positive_zrK;
  return styles.neutral_blue;
};

const ResourceLine: FC<ResourceLineProps> = ({ data, showColumns }) => {
  const formattedName = formatWords(data.Resource, true);
  const profit = calculateProfit(data);
  const profitMargin = calculateProfitMargin(data);
  const profitClass = getProfitClass(profit);
  const profitMarginClass = getProfitMarginClass(profitMargin);
  const importPerTon = data.Count !== 0 ? data.ImportAmount / data.Count : 0;
  const exportPerTon = data.Count !== 0 ? data.ExportAmount / data.Count : 0;
  return (
    <div className={styles.row_S2v}>
      <div className={styles.cell} style={{ width: '3%' }} />
      <div className={styles.cell} style={{ width: '25%', justifyContent: 'flex-start'}}>
        <ResourceIcon resourceName={data.Resource} />
        <span>{formattedName}</span>
      </div>
      {showColumns.buyCost && (
        <div className={styles.cell} style={{ width: '10%' }}>
          {data.BuyCost.toFixed(2)}
        </div>
      )}
      {showColumns.sellCost && (
        <div className={styles.cell} style={{ width: '10%' }}>
          {data.SellCost.toFixed(2)}
        </div>
      )}
      {showColumns.profit && (
        <div className={styles.cell} style={{ width: '10%' }}>
          <span className={profitClass}>{profit.toFixed(2)}</span>
        </div>
      )}
      {showColumns.profitMargin && (
        <div className={styles.cell} style={{ width: '10%' }}>
          <span className={profitMarginClass}>{profitMargin.toFixed(2)}%</span>
        </div>
      )}
      {showColumns.importAmount && (
        <div className={styles.cell} style={{ width: '10%' }}>
          {importPerTon.toFixed(2)} /t
        </div>
      )}
      {showColumns.exportAmount && (
        <div className={styles.cell} style={{ width: '10%' }}>
          {exportPerTon.toFixed(2)} /t
        </div>
      )}
    </div>
  );
};

const TableHeader: FC<{ showColumns: ShowColumnsType }> = ({ showColumns }) => (
  <div className={styles.headerRow}>
    <div className={styles.headerCell} style={{ width: '3%' }} />
    <div className={styles.headerCell} style={{ width: '25%' }}>
      Resource
    </div>
    {showColumns.buyCost && (
      <div className={styles.headerCell} style={{ width: '10%' }}>
        <div><b>Buy Cost</b></div>
      </div>
    )}
    {showColumns.sellCost && (
      <div className={styles.headerCell} style={{ width: '10%' }}>
        <div><b>Sell Cost</b></div>
      </div>
    )}
    {showColumns.profit && (
      <div className={styles.headerCell} style={{ width: '10%' }}>
        <div><b>Profit</b></div>
      </div>
    )}
    {showColumns.profitMargin && (
      <div className={styles.headerCell} style={{ width: '10%' }}>
        <div><b>Profit Margin</b></div>
      </div>
    )}
    {showColumns.importAmount && (
      <div className={styles.headerCell} style={{ width: '10%' }}>
        <div><b>Import Amount</b></div>
      </div>
    )}
    {showColumns.exportAmount && (
      <div className={styles.headerCell} style={{ width: '10%' }}>
        <div><b>Export Amount</b></div>
      </div>
    )}
  </div>
);

const SortOptions: FC<{
  sortBy: SortOption;
  setSortBy: (option: SortOption) => void;
}> = ({ sortBy, setSortBy }) => {
  const options: { label: string; key: SortOption }[] = [
    { label: 'Sort by Name', key: 'name' },
    { label: 'Sort by Buy Cost', key: 'buyCost' },
    { label: 'Sort by Sell Cost', key: 'sellCost' },
    { label: 'Sort by Profit', key: 'profit' },
    { label: 'Sort by Profit Margin', key: 'profitMargin' },
    { label: 'Sort by Import Amount (tonnes/ton)', key: 'importAmount' },
    { label: 'Sort by Export Amount (tonnes/ton)', key: 'exportAmount' },
  ];
  return (
    <Dropdown
      theme={DropdownStyle}
      content={
        <div className={styles.dropdownContent}>
          {options.map(({ label, key }) => (
            <div key={key} className={styles.dropdownItem}>
              <InfoCheckbox
                label={label}
                isChecked={sortBy === key}
                onToggle={() => setSortBy(key)}
              />
            </div>
          ))}
        </div>
      }
    >
      <DropdownToggle className={styles.dropdownToggle}>
        Sort Options
      </DropdownToggle>
    </Dropdown>
  );
};

const ColumnOptions: FC<{
  showColumns: ShowColumnsType;
  toggleColumn: (column: keyof ShowColumnsType) => void;
}> = ({ showColumns, toggleColumn }) => (
  <Dropdown
    theme={DropdownStyle}
    content={
      <div className={styles.dropdownContent}>
        {[
          { label: 'Show Buy Cost', key: 'buyCost' },
          { label: 'Show Sell Cost', key: 'sellCost' },
          { label: 'Show Profit', key: 'profit' },
          { label: 'Show Profit Margin', key: 'profitMargin' },
          { label: 'Show Import Amount (tonnes/ton)', key: 'importAmount' },
          { label: 'Show Export Amount (tonnes/ton)', key: 'exportAmount' },
        ].map(({ label, key }) => (
          <div key={key} className={styles.dropdownItem}>
            <InfoCheckbox
              label={label}
              isChecked={showColumns[key as keyof ShowColumnsType]}
              onToggle={() => toggleColumn(key as keyof ShowColumnsType)}
            />
          </div>
        ))}
      </div>
    }
  >
    <DropdownToggle className={styles.dropdownToggle}>
      Column Options
    </DropdownToggle>
  </Dropdown>
);

const TradeCostsGraph: FC<TradeCostsGraphProps> = ({ selectedResources }) => {
  const tradeCosts = useValue(TradeCostsData);
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const chartRef = useRef<Chart<'bar', { x: string; y: number[] }[], string> | null>(null);
  const containerRef = useRef<HTMLDivElement | null>(null);
  const filteredTradeCosts = useMemo(
    () => tradeCosts.filter((item) => selectedResources.has(item.Resource)),
    [tradeCosts, selectedResources]
  );
  const chartData = useMemo(() => {
    const labels = filteredTradeCosts.map((item) =>
      formatWords(item.Resource, true)
    );
    const floatingBars = filteredTradeCosts.map((item) => ({
      x: item.Resource,
      y: [item.BuyCost === item.SellCost ? 0 : item.BuyCost, item.SellCost],
    }));
    return {
      labels,
      datasets: [
        {
          label: 'Trade Costs Range',
          data: floatingBars,
          backgroundColor: (context: any) => {
            const { y } = context.raw;
            if (y[0] === 0 || y[1] === 0) return 'rgb(255, 211, 80)';
            return y[0] < y[1]
              ? 'rgba(68, 151, 130, 1)'
              : 'rgba(223, 72, 76, 1)';
          },
          borderColor: (context: any) => {
            const { y } = context.raw;
            if (y[0] === 0 || y[1] === 0) return 'rgb(255, 211, 80)';
            return y[0] < y[1]
              ? 'rgba(68, 151, 130, 1)'
              : 'rgba(223, 72, 76, 1)';
          },
          borderWidth: 1,
        },
      ],
    };
  }, [filteredTradeCosts]);
  useEffect(() => {
    if (!canvasRef.current || !containerRef.current || filteredTradeCosts.length === 0)
      return;
    const ctx = canvasRef.current.getContext('2d');
    if (!ctx) return;
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
    chartRef.current.data = chartData;
    chartRef.current.update();
  }, [chartData]);
  useEffect(() => {
    if (!containerRef.current || !canvasRef.current) return;
    const resizeObserver = new ResizeObserver((entries) => {
      const { width, height } = entries[0].contentRect;
      if (canvasRef.current) {
        canvasRef.current.width = width;
        canvasRef.current.height = height;
      }
      chartRef.current?.resize();
    });
    resizeObserver.observe(containerRef.current);
    return () => resizeObserver.disconnect();
  }, []);
  return (
    <div className={styles.graphContainer} ref={containerRef}>
      {filteredTradeCosts.length === 0 ? (
        <p className={styles.loadingText}>
          No resources selected to display the graph.
        </p>
      ) : (
        <canvas ref={canvasRef} />
      )}
    </div>
  );
};

const MemoizedTradeCostsGraph = React.memo(TradeCostsGraph, (prevProps, nextProps) => {
  return (
    prevProps.selectedResources.size === nextProps.selectedResources.size &&
    [...prevProps.selectedResources].every((res) =>
      nextProps.selectedResources.has(res)
    )
  );
});

const $TradeCosts: FC<DraggablePanelProps> = ({ onClose, initialPosition, ...props }) => {
  const tradeCosts = useValue(TradeCostsData);
  const imports = useValue(TradeCostsDataImports);
  const exports = useValue(TradeCostsDataImports);
  const initialPos: Number2 = { x: 0.038, y: 0.15 };
  const mergedTradeCosts = useMemo(() => {
    return tradeCosts.map((tradeCost, index) => {
      const importAmount = imports[index]?.Amount || 0;
      const exportAmount = exports[index]?.Amount || 0;
      return {
        ...tradeCost,
        ImportAmount: Number(importAmount.toFixed(5)),
        ExportAmount: Number(exportAmount.toFixed(5)),
      };
    });
  }, [tradeCosts, imports, exports]);
  const [showColumns, setShowColumns] = useState<ShowColumnsType>({
    buyCost: true,
    sellCost: true,
    profit: true,
    profitMargin: true,
    importAmount: true,
    exportAmount: true,
  });
  const [sortBy, setSortBy] = useState<SortOption>('name');
  const [view, setView] = useState<ViewType>('table');
  const [selectedResources, setSelectedResources] = useState<Set<string>>(() =>
    new Set(mergedTradeCosts.map((item) => item.Resource))
  );
  const toggleColumn = useCallback((column: keyof ShowColumnsType) => {
    setShowColumns((prev) => ({ ...prev, [column]: !prev[column] }));
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
          return calculateProfit(b) - calculateProfit(a);
        case 'profitMargin': {
          const marginA = calculateProfitMargin(a);
          const marginB = calculateProfitMargin(b);
          return marginB - marginA;
        }
        case 'importAmount': {
          const perTonA = a.Count !== 0 ? a.ImportAmount / a.Count : 0;
          const perTonB = b.Count !== 0 ? b.ImportAmount / b.Count : 0;
          return perTonB - perTonA;
        }
        case 'exportAmount': {
          const perTonA = a.Count !== 0 ? a.ExportAmount / a.Count : 0;
          const perTonB = b.Count !== 0 ? b.ExportAmount / b.Count : 0;
          return perTonB - perTonA;
        }
        default:
          return 0;
      }
    },
    [sortBy]
  );
  const sortedTradeCosts = useMemo(
    () => [...mergedTradeCosts].sort(sortData),
    [mergedTradeCosts, sortData]
  );
  useEffect(() => {
    if (mergedTradeCosts.length > 0 && selectedResources.size === 0) {
      setSelectedResources(new Set(mergedTradeCosts.map((item) => item.Resource)));
    }
  }, [mergedTradeCosts, selectedResources]);
  const handleToggleResource = useCallback((resource: string) => {
  setSelectedResources((prev) => {
    const newSet = new Set(prev);
    if (newSet.has(resource)) {
      newSet.delete(resource);
    } else {
      newSet.add(resource);
    }
    return newSet;
  });
}, []);
  return (
    <Panel
      draggable={true}
      id="infoloom-trade-costs"
      onClose={onClose}
      initialPosition={initialPosition || initialPos}
      className={styles.panel}
      header={
        <div className={styles.header}>
          <span className={styles.headerText}>Trade Costs</span>
        </div>
      }
    >
      {mergedTradeCosts.length === 0 ? (
        <p className={styles.loadingText}>Loading Trade Costs...</p>
      ) : (
        <div className={styles.panelContent}>
          {view === 'table' ? (
            <>
              <div className={styles.controls}>
                <SortOptions sortBy={sortBy} setSortBy={setSortBy} />
                <ColumnOptions showColumns={showColumns} toggleColumn={toggleColumn} />
                <Button onClick={() => setView('graph')} className={styles.graphButton}>
                  Trade Cost Graph
                </Button>
              </div>
              <DataDivider />
              <TableHeader showColumns={showColumns} />
              <DataDivider />
              <div className={styles.tableBody}>
                {sortedTradeCosts.map((item) => (
                  <ResourceLine key={item.Resource} data={item} showColumns={showColumns} />
                ))}
              </div>
             
            </>
          ) : (
            <>
              <div className={styles.graphViewContainer}>
                <div className={styles.resourceCheckboxes}>
                  <Scrollable vertical={true} smooth={true} trackVisibility={"scrollable"} onOverflowY={() => false}>
                  <div className={styles.checkboxHeader}>Select Resources:</div>
                    {tradeCosts
                        .sort((a, b) => a.Resource.localeCompare(b.Resource))
                        .map((item) => (
                            <div key={item.Resource} className={styles.checkboxItem}>
                              <InfoCheckbox
                                  label={
                                    <div className={styles.checkboxLabel}>
                                      <ResourceIcon resourceName={item.Resource}/>
                                      <span>{formatWords(item.Resource, true)}</span>
                                    </div>
                                  }
                                  isChecked={selectedResources.has(item.Resource)}
                                  onToggle={() => handleToggleResource(item.Resource)}
                              />
                            </div>
                        ))}
                  </Scrollable>
                </div>
                <div className={styles.graphArea}>
                  <div className={styles.graphHeader}>
                    <Button onClick={() => setView('table')} className={styles.backButton}>
                      &larr; Back to Table
                    </Button>
                  </div>
                  <MemoizedTradeCostsGraph selectedResources={selectedResources}/>
                </div>
              </div>
            </>
          )}
        </div>
      )}
    </Panel>
  );
};

export default $TradeCosts;
