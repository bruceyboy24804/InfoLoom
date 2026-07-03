import React, { memo, useEffect, useMemo, useRef } from 'react';
import { bindLocalValue, bindValue, useValue } from 'cs2/api';
import { BudgetItem } from 'cs2/bindings';
import { DraggablePanelProps, Panel } from 'cs2/ui';
import { Chart } from 'chart.js';
import { SankeyController, Flow } from 'chartjs-chart-sankey';
import styles from './budgetSankey.module.scss';

Chart.register(SankeyController, Flow);

// ── Game bindings (from the vanilla BudgetUISystem) ──────────────────────────
const totalIncome$ = bindValue<number>('budget', 'totalIncome', 0);
const totalExpenses$ = bindValue<number>('budget', 'totalExpenses', 0);
const incomeItems$ = bindValue<BudgetItem[]>('budget', 'incomeItems', []);
const incomeValues$ = bindValue<number[]>('budget', 'incomeValues', []);
const expenseItems$ = bindValue<BudgetItem[]>('budget', 'expenseItems', []);
const expenseValues$ = bindValue<number[]>('budget', 'expenseValues', []);

// ── Panel visibility ──────────────────────────────────────────────────────────
export const panelVisibleBinding = bindLocalValue(false);
export const panelTrigger = (state: boolean) => panelVisibleBinding.update(state);

// ── Helpers ───────────────────────────────────────────────────────────────────
function formatCurrency(v: number): string {
  if (v >= 1_000_000) return `¢${(v / 1_000_000).toFixed(1)}M`;
  if (v >= 1_000) return `¢${(v / 1_000).toFixed(0)}K`;
  return `¢${v.toLocaleString()}`;
}

// ── Sankey chart ──────────────────────────────────────────────────────────────
type SankeyFlow = { from: string; to: string; flow: number };

const BudgetSankeyChartComponent: React.FC = () => {
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const containerRef = useRef<HTMLDivElement | null>(null);
  const chartRef = useRef<Chart | null>(null);
  // Keep latest flows in a ref so colorFrom/colorTo callbacks always read current data
  const flowsRef = useRef<SankeyFlow[]>([]);
  // Tracks which node IDs belong to the expense side (Expense + category nodes)
  const expenseNodeIdsRef = useRef<Set<string>>(new Set());

  const totalIncome = useValue(totalIncome$);
  const totalExpenses = useValue(totalExpenses$);
  const incomeItems = useValue(incomeItems$);
  const incomeValues = useValue(incomeValues$);
  const expenseItems = useValue(expenseItems$);
  const expenseValues = useValue(expenseValues$);

  const surplus = Math.max(0, totalIncome + totalExpenses);

  // ── Create chart ONCE ────────────────────────────────────────────────────
  useEffect(() => {
    if (!canvasRef.current || !containerRef.current) return;
    const ctx = canvasRef.current.getContext('2d');
    if (!ctx) return;

    // Set an explicit initial size so the canvas isn't zero-sized on first render
    ctx.canvas.width = 200;
    ctx.canvas.height = 200;

    chartRef.current = new Chart(ctx, {
      type: 'sankey',
      data: {
        datasets: [
          {
            label: 'City Budget',
            data: [] as SankeyFlow[],
            colorFrom: (c: any) => {
              const f = flowsRef.current[c.dataIndex];
              return expenseNodeIdsRef.current.has(f?.from ?? '') ? 'rgba(239,83,80,0.85)' : 'rgba(76,175,80,0.85)';
            },
            colorTo: (c: any) => {
              const f = flowsRef.current[c.dataIndex];
              return expenseNodeIdsRef.current.has(f?.to ?? '') ? 'rgba(239,83,80,0.85)' : 'rgba(76,175,80,0.85)';
            },
            colorMode: 'gradient',
            alpha: 0.65,
            labels: {} as Record<string, string>,
            size: 'max',
          } as any,
        ],
      },
      options: {
        responsive: false,
        maintainAspectRatio: false,
        animation: { duration: 0 },
        color: '#ffffff',
        plugins: {
          legend: { display: false },
          tooltip: {
            backgroundColor: '#171d2b',
            titleColor: '#ffffff',
            bodyColor: '#cccccc',
            titleFont: { size: 13, family: 'Overpass', weight: 'bold' },
            bodyFont: { size: 12, family: 'Overpass' },
            padding: 10,
            callbacks: {
              label: ctx => {
                const d = flowsRef.current[ctx.dataIndex];
                if (!d) return '';
                return `${d.from} → ${d.to}: ${formatCurrency(d.flow)}`;
              },
              title: () => '',
            },
          },
        },
      },
    });

    return () => {
      if (chartRef.current) {
        chartRef.current.destroy();
        chartRef.current = null;
      }
    };
  }, []);

  // ── Update data when bindings change ─────────────────────────────────────
  useEffect(() => {
    // Aggregate income groups
    const incomeGroups = incomeItems
      .filter(item => item.active)
      .map(item => ({
        id: item.id,
        value: item.sources.reduce((s, src) => s + Math.abs(incomeValues[src.index] ?? 0), 0),
      }))
      .filter(g => g.value > 0);

    const expenseGroups = expenseItems
      .filter(item => item.active)
      .map(item => ({
        id: item.id,
        value: item.sources.reduce((s, src) => s + Math.abs(expenseValues[src.index] ?? 0), 0),
      }))
      .filter(g => g.value > 0);

    const absExpenses = Math.abs(totalExpenses);

    // Build flows
    const flows: SankeyFlow[] = [];
    for (const g of incomeGroups) flows.push({ from: g.id, to: 'Revenue', flow: g.value });
    if (absExpenses > 0) flows.push({ from: 'Revenue', to: 'Expense', flow: absExpenses });
    if (surplus > 0) flows.push({ from: 'Revenue', to: 'Surplus', flow: surplus });
    for (const g of expenseGroups) flows.push({ from: 'Expense', to: g.id, flow: g.value });

    // Build labels
    const labels: Record<string, string> = { Revenue: 'Revenue', Expense: 'Expense', Surplus: 'Surplus' };
    for (const g of incomeGroups) labels[g.id] = g.id;
    for (const g of expenseGroups) labels[g.id] = g.id;

    // Update refs — expense-side node IDs (Expense aggregate + all category nodes)
    expenseNodeIdsRef.current = new Set(['Expense', ...expenseGroups.map(g => g.id)]);
    flowsRef.current = flows;

    if (!chartRef.current) return;

    // Update dataset in-place — no destroy/recreate
    const ds = chartRef.current.data.datasets[0] as any;
    ds.data = flows;
    ds.labels = labels;
    chartRef.current.update('none');
  }, [incomeItems, incomeValues, expenseItems, expenseValues, totalIncome, totalExpenses]);

  // ── Resize observer ───────────────────────────────────────────────────────
  useEffect(() => {
    if (!containerRef.current || !canvasRef.current) return;
    const ro = new ResizeObserver(entries => {
      const entry = entries[0];
      if (!entry) return;
      const { width, height } = entry.contentRect;
      if (canvasRef.current && width > 0 && height > 0) {
        canvasRef.current.width = width;
        canvasRef.current.height = height;
      }
      chartRef.current?.resize();
    });
    ro.observe(containerRef.current);
    return () => ro.disconnect();
  }, []);

  return (
    <div ref={containerRef} className={styles.chartWrapper}>
      <canvas ref={canvasRef} style={{ display: 'block', width: 0, height: 0 }} />
    </div>
  );
};

const BudgetSankeyChart = memo(BudgetSankeyChartComponent);

// ── Summary row ───────────────────────────────────────────────────────────────
const SummaryBar: React.FC<{ label: string; value: number; positive: boolean }> = ({ label, value, positive }) => (
  <div className={styles.summaryItem}>
    <span className={styles.summaryLabel}>{label}</span>
    <span className={positive ? styles.summaryValuePos : styles.summaryValueNeg}>
      {positive ? '+' : '-'}
      {formatCurrency(Math.abs(value))}
    </span>
  </div>
);

// ── Main panel ────────────────────────────────────────────────────────────────
const BudgetSankey: React.FC<DraggablePanelProps> = ({ onClose, initialPosition }) => {
  const totalIncome = useValue(totalIncome$);
  const totalExpenses = useValue(totalExpenses$);
  const surplus = totalIncome + totalExpenses;

  return (
    <Panel
      draggable
      onClose={onClose}
      initialPosition={initialPosition}
      className={styles.panel}
      header={
        <div className={styles.header}>
          <span className={styles.title}>City Budget — Sankey</span>
        </div>
      }
    >
      <div className={styles.summaryBar}>
        <SummaryBar label="Revenue" value={totalIncome} positive={true} />
        <SummaryBar label="Expenses" value={totalExpenses} positive={false} />
        <SummaryBar label="Surplus" value={surplus} positive={surplus >= 0} />
      </div>
      <BudgetSankeyChart />
    </Panel>
  );
};

export default BudgetSankey;
