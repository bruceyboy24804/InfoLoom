
import React, { useState, useCallback, FC, useMemo, useRef, useEffect } from 'react';
import {
  Button,
  DraggablePanelProps,
  Dropdown,
  DropdownToggle,
  Icon,
  Number2,
  Panel,
  Scrollable,
  Tooltip,
} from 'cs2/ui';
import { useLocalization } from 'cs2/l10n';
import { InfoCheckbox } from 'mods/components/InfoCheckbox/InfoCheckbox';
import { getModule } from 'cs2/modding';
import styles from './TradeCost.module.scss';
import { formatWords } from 'mods/InfoLoomSections/utils/formatText';
import { bindValue, useValue } from 'cs2/api';
import mod from 'mod.json';
import Chart from 'chart.js/auto';
import { ResourceTradeCost } from 'mods/domain/tradeCostData';
import {
  TradeCostsData,
  SetSellCostSorting,
  SetProfitSorting,
  SetImportAmountSorting,
  SetBuyCostSorting,
  SetExportAmountSorting,
  SellCostSorting,
  BuyCostSorting,
  SetProfitMarginSorting,
  ProfitSorting,
  SetResourceNameSorting,
  ResourceNameSorting,
  ImportAmountSorting,
  ExportAmountSorting,
  ProfitMarginSorting,
} from '../../bindings';
import {
  ProfitEnum,
  BuyCostEnum,
  SellCostEnum,
  ProfitMarginEnum,
  ResourceNameEnum,
  ImportAmountEnum,
  ExportAmountEnum,
} from 'mods/domain/TradeCostEnums';
import {
  CompanyNameEnum2,
  EfficiencyEnum2,
  EmployeesEnum2,
  ProfitabilityEnum2,
  ResourceAmountEnum2,
} from '../../domain/IndustrialCompanyEnums';

const DropdownStyle = getModule('game-ui/menu/themes/dropdown.module.scss', 'classes');

interface ResourceLineProps {
  data: ResourceTradeCost;
  translations: {
    buyCostTooltip: string | null;
    sellCostTooltip: string | null;
    profitTooltip: string | null;
    profitMarginTooltip: string | null;
    importAmountTooltip: string | null;
    exportAmountTooltip: string | null;
  };
}

type ViewType = 'table' | 'graph';

interface TradeCostsGraphProps {
  selectedResources: Set<string>;
}

const DataDivider: React.FC = () => (
  <div
    style={{ display: 'flex', height: '4rem', flexDirection: 'column', justifyContent: 'center' }}
  >
    <div style={{ borderBottom: '1px solid gray', width: '100%' }}></div>
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

const ResourceLine: FC<ResourceLineProps> = ({ data, translations }) => {
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
      <div className={styles.cell} style={{ width: '25%', justifyContent: 'flex-start' }}>
        <Icon src={data.ResourceIcon} />
        <span>{formattedName}</span>
      </div>
      <div className={styles.cell} style={{ width: '10%' }}>
        <Tooltip 
          tooltip={translations.buyCostTooltip}
          direction="up"
          alignment="center"
        >
          <span>{data.BuyCost.toFixed(2)}</span>
        </Tooltip>
      </div>
      <div className={styles.cell} style={{ width: '10%' }}>
        <Tooltip 
          tooltip={translations.sellCostTooltip}
          direction="up"
          alignment="center"
        >
          <span>{data.SellCost.toFixed(2)}</span>
        </Tooltip>
      </div>
      <div className={styles.cell} style={{ width: '10%' }}>
        <Tooltip 
          tooltip={translations.profitTooltip}
          direction="up"
          alignment="center"
        >
          <span className={profitClass}>{profit.toFixed(2)}</span>
        </Tooltip>
      </div>
      <div className={styles.cell} style={{ width: '10%' }}>
        <Tooltip 
          tooltip={translations.profitMarginTooltip}
          direction="up"
          alignment="center"
        >
          <span className={profitMarginClass}>{profitMargin.toFixed(2)}%</span>
        </Tooltip>
      </div>
      <div className={styles.cell} style={{ width: '10%' }}>
        <Tooltip 
          tooltip={translations.importAmountTooltip}
          direction="up"
          alignment="center"
        >
          <span>{importPerTon.toFixed(2)} /t</span>
        </Tooltip>
      </div>
      <div className={styles.cell} style={{ width: '10%' }}>
        <Tooltip 
          tooltip={translations.exportAmountTooltip}
          direction="up"
          alignment="center"
        >
          <span>{exportPerTon.toFixed(2)} /t</span>
        </Tooltip>
      </div>
    </div>
  );
};
interface SortableHeaderProps {
  title: string;
  sortState:
    | ResourceNameEnum
    | BuyCostEnum
    | SellCostEnum
    | ProfitEnum
    | ProfitMarginEnum
    | ImportAmountEnum
    | ExportAmountEnum;
  onSort: (direction: 'asc' | 'desc' | 'off') => void;
  className?: string;
  tooltip?: string | null;
}
const SortableHeader: FC<SortableHeaderProps> = ({ title, sortState, onSort, className, tooltip }) => {
  const handleClick = (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();

    // Cycle through sort states based on actual enum values:
    // Off = 0, Ascending = 1, Descending = 2
    // Cycle: Off -> Ascending -> Descending -> Off
    if (sortState === 0) {
      // Off
      onSort('asc');
    } else if (sortState === 1) {
      // Ascending
      onSort('desc');
    } else {
      // Descending
      onSort('off');
    }
  };

  const headerContent = (
    <div
      className={`${styles.sortableHeader} ${className || ''}`}
      onClick={handleClick}
      style={{ cursor: 'pointer' }}
    >
      <span>{title}</span>
      <div className={styles.sortArrows}>
        {sortState === 1 && (
          <Icon src="coui://uil/Standard/ArrowSortHighDown.svg" className={styles.sortIcon} />
        )}
        {sortState === 2 && (
          <Icon src="coui://uil/Standard/ArrowSortLowDown.svg" className={styles.sortIcon} />
        )}
      </div>
    </div>
  );

  return tooltip ? (
    <Tooltip tooltip={tooltip} direction="down" alignment="center">
      {headerContent}
    </Tooltip>
  ) : (
    headerContent
  );
};
interface TableHeaderProps {
  sortingStates: {
    ResourceName: ResourceNameEnum;
    BuyCost: BuyCostEnum;
    SellCost: SellCostEnum;
    Profit: ProfitEnum;
    ProfitMargin: ProfitMarginEnum;
    ImportAmount: ImportAmountEnum;
    ExportAmount: ExportAmountEnum;
  };
  translations: {
    resourceHeader: string | null;
    resourceHeaderTooltip: string | null;
    buyCostHeader: string | null;
    buyCostHeaderTooltip: string | null;
    sellCostHeader: string | null;
    sellCostHeaderTooltip: string | null;
    profitHeader: string | null;
    profitHeaderTooltip: string | null;
    profitMarginHeader: string | null;
    profitMarginHeaderTooltip: string | null;
    importAmountHeader: string | null;
    importAmountHeaderTooltip: string | null;
    exportAmountHeader: string | null;
    exportAmountHeaderTooltip: string | null;
  };
}

const TableHeader: FC<TableHeaderProps> = ({ sortingStates, translations }) => {
  return (
    <div className={styles.headerRow}>
      <div className={styles.headerCell} style={{ width: '3%' }} />
      <div className={styles.headerCell} style={{ width: '25%' }}>
        <SortableHeader
          title={translations.resourceHeader || "Resource"}
          sortState={sortingStates.ResourceName}
          tooltip={translations.resourceHeaderTooltip}
          onSort={direction => {
            if (direction === 'asc') SetResourceNameSorting(ResourceNameEnum.Ascending);
            else if (direction === 'desc') SetResourceNameSorting(ResourceNameEnum.Descending);
            else SetResourceNameSorting(ResourceNameEnum.Off);
          }}
        />
      </div>
      <div className={styles.headerCell} style={{ width: '10%' }}>
        <SortableHeader
          title={translations.buyCostHeader || "Buy Cost"}
          sortState={sortingStates.BuyCost}
          tooltip={translations.buyCostHeaderTooltip}
          onSort={direction => {
            if (direction === 'asc') SetBuyCostSorting(BuyCostEnum.Ascending);
            else if (direction === 'desc') SetBuyCostSorting(BuyCostEnum.Descending);
            else SetBuyCostSorting(BuyCostEnum.Off);
          }}
        />
      </div>
      <div className={styles.headerCell} style={{ width: '10%' }}>
        <SortableHeader
          title={translations.sellCostHeader || "Sell Cost"}
          sortState={sortingStates.SellCost}
          tooltip={translations.sellCostHeaderTooltip}
          onSort={direction => {
            if (direction === 'asc') SetSellCostSorting(SellCostEnum.Ascending);
            else if (direction === 'desc') SetSellCostSorting(SellCostEnum.Descending);
            else SetSellCostSorting(SellCostEnum.Off);
          }}
        />
      </div>
      <div className={styles.headerCell} style={{ width: '10%' }}>
        <SortableHeader
          title={translations.profitHeader || "Profit"}
          sortState={sortingStates.Profit}
          tooltip={translations.profitHeaderTooltip}
          onSort={direction => {
            if (direction === 'asc') SetProfitSorting(ProfitEnum.Ascending);
            else if (direction === 'desc') SetProfitSorting(ProfitEnum.Descending);
            else SetProfitSorting(ProfitEnum.Off);
          }}
        />
      </div>
      <div className={styles.headerCell} style={{ width: '10%' }}>
        <SortableHeader
          title={translations.profitMarginHeader || "Profit Margin"}
          sortState={sortingStates.ProfitMargin}
          tooltip={translations.profitMarginHeaderTooltip}
          onSort={direction => {
            if (direction === 'asc') SetProfitMarginSorting(ProfitMarginEnum.Ascending);
            else if (direction === 'desc') SetProfitMarginSorting(ProfitMarginEnum.Descending);
            else SetProfitMarginSorting(ProfitMarginEnum.Off);
          }}
        />
      </div>
      <div className={styles.headerCell} style={{ width: '10%' }}>
        <SortableHeader
          title={translations.importAmountHeader || "Import Amount"}
          sortState={sortingStates.ImportAmount}
          tooltip={translations.importAmountHeaderTooltip}
          onSort={direction => {
            if (direction === 'asc') SetImportAmountSorting(ImportAmountEnum.Ascending);
            else if (direction === 'desc') SetImportAmountSorting(ImportAmountEnum.Descending);
            else SetImportAmountSorting(ImportAmountEnum.Off);
          }}
        />
      </div>
      <div className={styles.headerCell} style={{ width: '10%' }}>
        <SortableHeader
          title={translations.exportAmountHeader || "Export Amount"}
          sortState={sortingStates.ExportAmount}
          tooltip={translations.exportAmountHeaderTooltip}
          onSort={direction => {
            if (direction === 'asc') SetExportAmountSorting(ExportAmountEnum.Ascending);
            else if (direction === 'desc') SetExportAmountSorting(ExportAmountEnum.Descending);
            else SetExportAmountSorting(ExportAmountEnum.Off);
          }}
        />
      </div>
    </div>
  );
};
const TradeCostsGraph: FC<TradeCostsGraphProps> = ({ selectedResources }) => {
  const tradeCosts = useValue(TradeCostsData);
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const chartRef = useRef<Chart<'bar', { x: string; y: number[] }[], string> | null>(null);
  const containerRef = useRef<HTMLDivElement | null>(null);
  const filteredTradeCosts = useMemo(
    () => tradeCosts.filter(item => selectedResources.has(item.Resource)),
    [tradeCosts, selectedResources]
  );
  const chartData = useMemo(() => {
    const labels = filteredTradeCosts.map(item => formatWords(item.Resource, true));
    const floatingBars = filteredTradeCosts.map(item => ({
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
  useEffect(() => {
    if (!canvasRef.current || !containerRef.current || filteredTradeCosts.length === 0) return;
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
                const resourceData = filteredTradeCosts.find(item => item.Resource === context.raw.x);
                const profit = resourceData ? calculateProfit(resourceData) : 0;
                const profitMargin = resourceData ? calculateProfitMargin(resourceData) : 0;
                
                return [
                  range[0] === 0
                    ? `Buy/Sell Price: $${range[1].toFixed(2)}`
                    : `Buy: $${range[0].toFixed(2)}, Sell: $${range[1].toFixed(2)}`,
                  `Profit: $${profit.toFixed(2)}`,
                  `Profit Margin: ${profitMargin.toFixed(2)}%`
                ];
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
    const resizeObserver = new ResizeObserver(entries => {
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
        <p className={styles.loadingText}>No resources selected to display the graph.</p>
      ) : (
        <canvas ref={canvasRef} />
      )}
    </div>
  );
};

const MemoizedTradeCostsGraph = React.memo(TradeCostsGraph, (prevProps, nextProps) => {
  return (
    prevProps.selectedResources.size === nextProps.selectedResources.size &&
    [...prevProps.selectedResources].every(res => nextProps.selectedResources.has(res))
  );
});

const $TradeCosts: FC<DraggablePanelProps> = ({ onClose, initialPosition, ...props }) => {
  const { translate } = useLocalization();
  const tradeCosts = useValue(TradeCostsData);

  const resourceNameSorting = useValue(ResourceNameSorting);
  const buyCostSorting = useValue(BuyCostSorting);
  const sellCostSorting = useValue(SellCostSorting);
  const profitSorting = useValue(ProfitSorting);
  const profitMarginSorting = useValue(ProfitMarginSorting);
  const importAmountSorting = useValue(ImportAmountSorting);
  const exportAmountSorting = useValue(ExportAmountSorting);

  const initialPos: Number2 = { x: 0.038, y: 0.15 };
  // Now that TradeCostsData already includes ImportAmount/ExportAmount,
  // we directly assign it.
  const mergedTradeCosts: ResourceTradeCost[] = tradeCosts || [];
  const [view, setView] = useState<ViewType>('table');
  const [selectedResources, setSelectedResources] = useState<Set<string>>(
    () => new Set(mergedTradeCosts.map(item => item.Resource))
  );
  useEffect(() => {
    if (mergedTradeCosts.length > 0 && selectedResources.size === 0) {
      setSelectedResources(new Set(mergedTradeCosts.map(item => item.Resource)));
    }
  }, [mergedTradeCosts, selectedResources]);
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
  
  return (
    <Panel
      draggable={true}
      onClose={onClose}
      initialPosition={{ x: 0.5, y: 0.5 }}
      className={styles.panel}
      header={
        <div className={styles.header}>
          <span className={styles.headerText}>
            {translate("InfoLoomTwo.TradeCostsPanel[Title]", "Trade Costs")}
          </span>
        </div>
      }
    >
      {mergedTradeCosts.length === 0 ? (
        <p className={styles.loadingText}>
          {translate("InfoLoomTwo.TradeCostsPanel[Loading]", "Loading Trade Costs...")}
        </p>
      ) : (
        <div className={styles.panelContent}>
          {view === 'table' ? (
            <>
              <div className={styles.controls}>
                <Tooltip 
                  tooltip={translate("InfoLoomTwo.TradeCostsPanel[GraphButtonTooltip]", "Switch to graphical view to visualize trade cost ranges for selected resources")}
                  direction="up"
                  alignment="center"
                >
                  <Button onClick={() => setView('graph')} className={styles.graphButton}>
                    {translate("InfoLoomTwo.TradeCostsPanel[GraphButton]", "Trade Cost Graph")}
                  </Button>
                </Tooltip>
              </div>
              <TableHeader
                sortingStates={{
                  ResourceName: resourceNameSorting,
                  BuyCost: buyCostSorting,
                  SellCost: sellCostSorting,
                  Profit: profitSorting,
                  ProfitMargin: profitMarginSorting,
                  ImportAmount: importAmountSorting,
                  ExportAmount: exportAmountSorting,
                }}
                translations={{
                  resourceHeader: translate("InfoLoomTwo.TradeCostsPanel[ResourceHeader]", "Resource"),
                  resourceHeaderTooltip: translate("InfoLoomTwo.TradeCostsPanel[ResourceHeaderTooltip]", "Click to sort by resource name"),
                  buyCostHeader: translate("InfoLoomTwo.TradeCostsPanel[BuyCostHeader]", "Buy Cost"),
                  buyCostHeaderTooltip: translate("InfoLoomTwo.TradeCostsPanel[BuyCostHeaderTooltip]", "Cost to purchase resources from outside connections. Click to sort."),
                  sellCostHeader: translate("InfoLoomTwo.TradeCostsPanel[SellCostHeader]", "Sell Cost"),
                  sellCostHeaderTooltip: translate("InfoLoomTwo.TradeCostsPanel[SellCostHeaderTooltip]", "Price received when selling resources to outside connections. Click to sort."),
                  profitHeader: translate("InfoLoomTwo.TradeCostsPanel[ProfitHeader]", "Profit"),
                  profitHeaderTooltip: translate("InfoLoomTwo.TradeCostsPanel[ProfitHeaderTooltip]", "Profit per unit (Sell Price - Buy Price). Positive values are profitable. Click to sort."),
                  profitMarginHeader: translate("InfoLoomTwo.TradeCostsPanel[ProfitMarginHeader]", "Profit Margin"),
                  profitMarginHeaderTooltip: translate("InfoLoomTwo.TradeCostsPanel[ProfitMarginHeaderTooltip]", "Profit margin percentage ((Profit / Buy Cost) * 100). Higher is better. Click to sort."),
                  importAmountHeader: translate("InfoLoomTwo.TradeCostsPanel[ImportAmountHeader]", "Import Amount"),
                  importAmountHeaderTooltip: translate("InfoLoomTwo.TradeCostsPanel[ImportAmountHeaderTooltip]", "Amount of resources imported from outside connections per ton. Click to sort."),
                  exportAmountHeader: translate("InfoLoomTwo.TradeCostsPanel[ExportAmountHeader]", "Export Amount"),
                  exportAmountHeaderTooltip: translate("InfoLoomTwo.TradeCostsPanel[ExportAmountHeaderTooltip]", "Amount of resources exported to outside connections per ton. Click to sort."),
                }}
              />
              <DataDivider />
              <div className={styles.tableBody}>
                {mergedTradeCosts.map(item => (
                  <ResourceLine 
                    key={item.Resource} 
                    data={item}
                    translations={{
                      buyCostTooltip: translate("InfoLoomTwo.TradeCostsPanel[BuyCostTooltip]", `Cost to buy ${formatWords(item.Resource, true)} from outside connections: $${item.BuyCost.toFixed(2)}`),
                      sellCostTooltip: translate("InfoLoomTwo.TradeCostsPanel[SellCostTooltip]", `Price received when selling ${formatWords(item.Resource, true)} to outside connections: $${item.SellCost.toFixed(2)}`),
                      profitTooltip: translate("InfoLoomTwo.TradeCostsPanel[ProfitTooltip]", `Profit per unit: $${calculateProfit(item).toFixed(2)} (Sell Price - Buy Price)`),
                      profitMarginTooltip: translate("InfoLoomTwo.TradeCostsPanel[ProfitMarginTooltip]", `Profit margin: ${calculateProfitMargin(item).toFixed(2)}% ((Profit / Buy Cost) * 100)`),
                      importAmountTooltip: translate("InfoLoomTwo.TradeCostsPanel[ImportAmountTooltip]", `Import amount per ton: ${(item.Count !== 0 ? item.ImportAmount / item.Count : 0).toFixed(2)} (Total Import Amount / Count)`),
                      exportAmountTooltip: translate("InfoLoomTwo.TradeCostsPanel[ExportAmountTooltip]", `Export amount per ton: ${(item.Count !== 0 ? item.ExportAmount / item.Count : 0).toFixed(2)} (Total Export Amount / Count)`),
                    }}
                  />
                ))}
              </div>
            </>
          ) : (
            <>
              <div className={styles.graphViewContainer}>
                <div className={styles.resourceCheckboxes}>
                  <Scrollable
                    vertical={true}
                    smooth={true}
                    trackVisibility="scrollable"
                    onOverflowY={() => false}
                  >
                    <div className={styles.checkboxHeader}>
                      <Tooltip 
                        tooltip={translate("InfoLoomTwo.TradeCostsPanel[SelectResourcesTooltip]", "Select which resources to display in the graph. Multiple resources can be selected.")}
                        direction="right"
                        alignment="center"
                      >
                        <span>{translate("InfoLoomTwo.TradeCostsPanel[SelectResources]", "Select Resources:")}</span>
                      </Tooltip>
                    </div>
                    {tradeCosts
                      .sort((a, b) => a.Resource.localeCompare(b.Resource))
                      .map(item => (
                        <div key={item.Resource} className={styles.checkboxItem}>
                          <InfoCheckbox
                            label={
                              <div className={styles.checkboxLabel}>
                                <Icon src={item.ResourceIcon} />
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
                    <Tooltip 
                      tooltip={translate("InfoLoomTwo.TradeCostsPanel[BackButtonTooltip]", "Return to the detailed table view")}
                      direction="up"
                      alignment="center"
                    >
                      <Button onClick={() => setView('table')} className={styles.backButton}>
                        {translate("InfoLoomTwo.TradeCostsPanel[BackButton]", "Back to Table")}
                      </Button>
                    </Tooltip>
                  </div>
                  <MemoizedTradeCostsGraph selectedResources={selectedResources} />
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