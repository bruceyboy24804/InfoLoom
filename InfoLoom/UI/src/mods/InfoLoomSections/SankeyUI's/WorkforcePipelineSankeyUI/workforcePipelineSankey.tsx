import React, { memo, useCallback, useEffect, useRef, useState } from 'react';
import { bindValue, trigger, useValue } from 'cs2/api';
import { DraggablePanelProps, Panel } from 'cs2/ui';
import { Chart } from 'chart.js';
import { SankeyController, Flow } from 'chartjs-chart-sankey';
import styles from './workforcePipelineSankey.module.scss';

Chart.register(SankeyController, Flow);

// ── Bindings ─────────────────────────────────────────────────────────────────
interface WorkforcePipelineData {
  ageEdu:          number[]; // [ageGroup * 5 + eduLevel]       20 values
  eduNonWork:      number[]; // [eduLevel * 4 + status]         20 values
  eduWorkSector:   number[]; // [eduLevel * 5 + sector]         25 values
  eduWorkJobEdu:   number[]; // [eduLevel * 5 + jobEdu]         25 values
  livingWorkerEdu: number[]; // [living * 5 + workerEdu]        10 values
  workerEduJobEdu: number[]; // [workerEdu * 5 + jobEdu]        25 values
  jobEduSector:    number[]; // [jobEdu * 5 + sector]           25 values
}

const pipelineData$  = bindValue<WorkforcePipelineData>('InfoLoomTwo', 'workforcePipelineData', {
  ageEdu:          new Array(20).fill(0),
  eduNonWork:      new Array(20).fill(0),
  eduWorkSector:   new Array(25).fill(0),
  eduWorkJobEdu:   new Array(25).fill(0),
  livingWorkerEdu: new Array(10).fill(0),
  workerEduJobEdu: new Array(25).fill(0),
  jobEduSector:    new Array(25).fill(0),
});
const viewMode$ = bindValue<boolean>('InfoLoomTwo', 'WorkforcePipelineViewMode', false);
const setViewMode = (v: boolean) => trigger('InfoLoomTwo', 'WorkforcePipelineViewMode', v);

// ── Node label maps ──────────────────────────────────────────────────────────
const AGE_NAMES     = ['Children', 'Teens', 'Adults', 'Seniors'];
const EDU_NAMES     = ['Uneducated', 'Poorly Edu.', 'Educated', 'Well Edu.', 'Highly Edu.'];
// Workforce view — employment column
const NON_WORK_NAMES   = ['Not in School', 'School', 'Unemployed', 'Retired'];
const WF_SECTOR_NAMES  = ['Commercial', 'Industrial', 'Office', 'City Service', 'Outside City'];
const WF_JOB_EDU_NAMES = ['Uneducated Job', 'Poorly Educated Job', 'Educated Job', 'Well Educated Job', 'Highly Educated Job'];
// Workplace view — prefixed keys to keep Sankey nodes unique across columns
const LIVING_NAMES     = ['L: Outside City', 'L: Within City'];
const WP_EDU_NAMES     = ['WE: Uneducated', 'WE: Poorly Educated', 'WE: Educated', 'WE: Well Educated', 'WE: Highly Educated'];
const JOB_EDU_NAMES    = ['JE: Uneducated', 'JE: Poorly Educated', 'JE: Educated', 'JE: Well Educated', 'JE: Highly Educated'];
const WP_SECTOR_NAMES  = ['S: Commercial', 'S: Industrial', 'S: Office', 'S: City Service', 'S: Outside City'];

// ── Per-node colours ─────────────────────────────────────────────────────────
const EDU_COLORS = [
  'rgba(128,128,128,0.9)',  // Uneducated
  'rgba(176,152,104,0.9)',  // Poorly Educated
  'rgba(54,138,46,0.9)',    // Educated
  'rgba(185,129,192,0.9)',  // Well Edu
  'rgba(87,150,209,0.9)',   // Highly Edu
];

const NODE_COLORS: Record<string, string> = {
  // Age
  'Children':    'rgba(58, 145, 199, 1)',
  'Teens':       'rgba(139, 185, 214, 1)',
  'Adults':      'rgba(95, 184, 68, 1)',
  'Seniors':     'rgba(163, 184, 68, 1)',
  // Education
  'Uneducated':  EDU_COLORS[0],
  'Poorly Educated': EDU_COLORS[1],
  'Educated':    EDU_COLORS[2],
  'Well Educated':   EDU_COLORS[3],
  'Highly Educated': EDU_COLORS[4],
  // Workforce view — non-work statuses
  'Not in School': 'rgba(255,167,38,0.9)',
  'School':        'rgba(66,165,245,0.9)',
  'Unemployed':    'rgba(239,83,80,0.9)',
  'Retired':       'rgba(158,158,158,0.9)',
  // Workforce view — sector grouping
  'Commercial':    'rgba(70, 178, 219, 1)',
  'Industrial':    'rgba(219, 194, 70, 1)',
  'Office':        'rgba(141, 68, 173, 1)',
  'City Service':  'rgb(34, 0, 255)',
  'Outside City':  'rgba(255, 152, 0, 0.9)',
  // Workforce view — job edu grouping
  'Uneducated Job':  EDU_COLORS[0],
  'Poorly Educated Job': EDU_COLORS[1],
  'Educated Job':    EDU_COLORS[2],
  'Well Educated Job':   EDU_COLORS[3],
  'Highly Educated Job': EDU_COLORS[4],
  // Workplace view — Living Place
  'L: Outside City': 'rgba(0,188,212,0.9)',
  'L: Within City':  'rgba(76,175,80,0.9)',
  // Workplace view — Worker Education
  'WE: Uneducated':  EDU_COLORS[0],
  'WE: Poorly Educated': EDU_COLORS[1],
  'WE: Educated':    EDU_COLORS[2],
  'WE: Well Educated':   EDU_COLORS[3],
  'WE: Highly Educated': EDU_COLORS[4],
  // Workplace view — Job Education Needed
  'JE: Uneducated':  EDU_COLORS[0],
  'JE: Poorly Educated': EDU_COLORS[1],
  'JE: Educated':    EDU_COLORS[2],
  'JE: Well Educated':   EDU_COLORS[3],
  'JE: Highly Educated': EDU_COLORS[4],
  // Workplace view — Sectors
  'S: Commercial':   'rgba(70, 178, 219, 1)',
  'S: Industrial':   'rgba(219, 194, 70, 1)',
  'S: Office':       'rgba(141, 68, 173, 1)',
  'S: City Service': 'rgb(34, 0, 255)',
  'S: Outside City': 'rgba(255, 152, 0, 0.9)',
};

const DEFAULT_COLOR = 'rgba(120,120,120,0.7)';

function dimColor(color: string): string {
  return color.replace(/[\d.]+\)$/, '0.07)');
}

type SankeyFlow = { from: string; to: string; flow: number };
// ── Chart component ──────────────────────────────────────────────────────────
interface PipelineChartProps { isWorkplaceMode: boolean; groupBySector: boolean; }

const PipelineChartComponent: React.FC<PipelineChartProps> = ({ isWorkplaceMode, groupBySector }) => {
  const canvasRef    = useRef<HTMLCanvasElement | null>(null);
  const containerRef = useRef<HTMLDivElement | null>(null);
  const chartRef     = useRef<Chart | null>(null);
  const flowsRef     = useRef<SankeyFlow[]>([]);
  const selectedRef  = useRef<Set<string>>(new Set());
  const labelsRef    = useRef<Record<string, string>>({});
  const legendItemsRef = useRef<{text: string; fillStyle: string; key: string}[]>([]);
  const hiddenRef = useRef<Set<string>>(new Set());
  const allFlowsRef = useRef<SankeyFlow[]>([]);

  const data = useValue(pipelineData$);

  // ── Create chart ONCE ──────────────────────────────────────────────────
  useEffect(() => {
    if (!canvasRef.current || !containerRef.current) return;
    const ctx = canvasRef.current.getContext('2d');
    if (!ctx) return;

    ctx.canvas.width  = 1;
    ctx.canvas.height = 1;

    const nodeStatsPlugin = {
      id: 'pipelineNodeStats',
      beforeDraw(chart: Chart) {
        const meta       = chart.getDatasetMeta(0) as any;
        const controller = meta?.controller;
        if (!controller?._nodes) return;
        const sel   = selectedRef.current;
        const flows = flowsRef.current;

        // Compute connected nodes: selected + their direct neighbors
        const connected = new Set(sel);
        if (sel.size > 0) {
          for (const f of flows) {
            if (sel.has(f.from)) connected.add(f.to);
            if (sel.has(f.to))   connected.add(f.from);
          }
        }

        for (const [nodeId, node] of Object.entries(controller._nodes as Record<string, any>)) {
          const baseColor = NODE_COLORS[nodeId] ?? DEFAULT_COLOR;
          if (sel.size === 0) {
            node.color = baseColor;
          } else {
            node.color = connected.has(nodeId) ? baseColor : dimColor(baseColor);
          }
        }
      },

      afterDraw(chart: Chart) {
        const meta       = chart.getDatasetMeta(0) as any;
        const controller = meta?.controller;
        if (!controller?._nodes) return;

        const ctx          = chart.ctx;
        const flows        = flowsRef.current;
        const sel          = selectedRef.current;
        const displayLabels = labelsRef.current;
        const hasSelection = sel.size > 0;

        const connected = new Set(sel);
        if (hasSelection) {
          for (const f of flows) {
            if (sel.has(f.from)) connected.add(f.to);
            if (sel.has(f.to))   connected.add(f.from);
          }
        }

        // Compute per-node flow sums
        const nodeIn:  Record<string, number> = {};
        const nodeOut: Record<string, number> = {};
        for (const f of flows) {
          if (!hasSelection || sel.has(f.from) || sel.has(f.to)) {
            nodeOut[f.from] = (nodeOut[f.from] || 0) + f.flow;
            nodeIn[f.to]   = (nodeIn[f.to]   || 0) + f.flow;
          }
        }
        let grandTotal = 0;
        for (const nodeId of Object.keys(controller._nodes)) {
          if (!nodeIn[nodeId]) grandTotal += nodeOut[nodeId] || 0;
        }
        if (grandTotal === 0) grandTotal = 1;

        ctx.save();
        ctx.textAlign    = 'center';
        ctx.textBaseline = 'middle';
        ctx.lineJoin     = 'round' as CanvasLineJoin;

        for (const [nodeId, node] of Object.entries(controller._nodes as Record<string, any>)) {
          const displayName = displayLabels[nodeId] || nodeId;
          const cx = node.x + 40;  // nodeWidth/2 = 80/2
          const cy = node.y + node.height / 2;
          const h  = node.height;

          if (hasSelection && !connected.has(nodeId)) {
            if (h >= 14) {
              const fs = Math.min(11, Math.floor(h * 0.5));
              ctx.font        = `bold ${fs}px Overpass, sans-serif`;
              ctx.fillStyle   = 'rgba(255,255,255,0.2)';
              ctx.strokeStyle = 'rgba(0,0,0,0.15)';
              ctx.lineWidth   = 2;
              ctx.strokeText(displayName, cx, cy);
              ctx.fillText(displayName, cx, cy);
            }
            continue;
          }

          const count = Math.max(nodeIn[nodeId] || 0, nodeOut[nodeId] || 0);
          if (count === 0) continue;

          const pct = ((count / grandTotal) * 100).toFixed(1);
          ctx.fillStyle   = '#ffffff';
          ctx.strokeStyle = 'rgba(0,0,0,0.55)';
          ctx.lineWidth   = 2.5;

          if (h >= 50) {
            const fs = Math.min(12, Math.floor(h / 5));
            const lh = fs * 1.35;
            ctx.font = `bold ${fs}px Overpass, sans-serif`;
            ctx.strokeText(displayName, cx, cy - lh);
            ctx.fillText(displayName, cx, cy - lh);
            ctx.font = `${fs}px Overpass, sans-serif`;
            ctx.strokeText(count.toLocaleString(), cx, cy);
            ctx.fillText(count.toLocaleString(), cx, cy);
            ctx.strokeText(`${pct}%`, cx, cy + lh);
            ctx.fillText(`${pct}%`, cx, cy + lh);
          } else if (h >= 24) {
            const fs = Math.min(11, Math.floor(h / 3));
            const lh = fs * 1.25;
            ctx.font = `bold ${fs}px Overpass, sans-serif`;
            ctx.strokeText(displayName, cx, cy - lh * 0.35);
            ctx.fillText(displayName, cx, cy - lh * 0.35);
            ctx.font = `${fs - 1}px Overpass, sans-serif`;
            const sub = `${count.toLocaleString()} \u00b7 ${pct}%`;
            ctx.strokeText(sub, cx, cy + lh * 0.55);
            ctx.fillText(sub, cx, cy + lh * 0.55);
          } else if (h >= 8) {
            const fs = Math.min(10, Math.floor(h * 0.7));
            ctx.font = `bold ${fs}px Overpass, sans-serif`;
            ctx.strokeText(displayName, cx, cy);
            ctx.fillText(displayName, cx, cy);
          }
        }
        ctx.restore();
      },
    };

    const handleClick = (e: MouseEvent) => {
      const chart  = chartRef.current;
      const canvas = canvasRef.current;
      if (!chart || !canvas) return;
      const controller = (chart.getDatasetMeta(0) as any)?.controller;
      if (!controller?._nodes) return;
      // offsetX/Y gives position relative to the canvas element in CSS space
      // Scale from CSS display size to canvas intrinsic size
      const cssW = canvas.offsetWidth  || canvas.clientWidth  || 1;
      const cssH = canvas.offsetHeight || canvas.clientHeight || 1;
      const mx   = e.offsetX * (canvas.width  / cssW);
      const my   = e.offsetY * (canvas.height / cssH);
      const sel  = selectedRef.current;
      let   hit  = false;
      for (const [nodeId, node] of Object.entries(controller._nodes as Record<string, any>)) {
        if (mx >= node.x && mx <= node.x + node.width && my >= node.y && my <= node.y + node.height) {
          sel.has(nodeId) ? sel.delete(nodeId) : sel.add(nodeId);
          hit = true;
          break;
        }
      }
      if (!hit) sel.clear();
      chart.update();
    };

    const canvas = canvasRef.current;
    canvas.addEventListener('click', handleClick);

    chartRef.current = new Chart(ctx, {
      type: 'sankey',
      plugins: [nodeStatsPlugin],
      data: {
        datasets: [{
          label: 'Workforce Pipeline',
          data: [] as SankeyFlow[],
          colorFrom: (c: any) => {
            const f    = flowsRef.current[c.dataIndex];
            if (!f) return DEFAULT_COLOR;
            const base = NODE_COLORS[f.from] ?? DEFAULT_COLOR;
            const sel  = selectedRef.current;
            if (sel.size === 0) return base;
            return (sel.has(f.from) || sel.has(f.to)) ? base : dimColor(base);
          },
          colorTo: (c: any) => {
            const f    = flowsRef.current[c.dataIndex];
            if (!f) return DEFAULT_COLOR;
            const base = NODE_COLORS[f.to] ?? DEFAULT_COLOR;
            const sel  = selectedRef.current;
            if (sel.size === 0) return base;
            return (sel.has(f.from) || sel.has(f.to)) ? base : dimColor(base);
          },
          // Hover: dim all other flows so the hovered one pops
          hoverColorFrom: (c: any) => {
            const f = flowsRef.current[c.dataIndex];
            if (!f) return DEFAULT_COLOR;
            return NODE_COLORS[f.from] ?? DEFAULT_COLOR;
          },
          hoverColorTo: (c: any) => {
            const f = flowsRef.current[c.dataIndex];
            if (!f) return DEFAULT_COLOR;
            return NODE_COLORS[f.to] ?? DEFAULT_COLOR;
          },
          colorMode: 'gradient',
          alpha: 0.65,
          nodeWidth: 80,
          nodePadding: 20,
          labels: {} as Record<string, string>,
          size: 'max',
        } as any],
      },
      options: {
        responsive: false,
        maintainAspectRatio: false,
        animation: { duration: 0 },
        color: '#ffffff',
        interaction: { mode: 'nearest', intersect: true },
        plugins: {
          legend: {
            display: true,
            position: 'bottom',
            onClick: (_e: any, legendItem: any) => {
              const item = legendItemsRef.current.find(i => i.text === legendItem.text);
              if (!item) return;
              const hidden = hiddenRef.current;
              if (hidden.has(item.key)) hidden.delete(item.key); else hidden.add(item.key);
              // Re-filter flows
              const visible = allFlowsRef.current.filter(f => !hidden.has(f.from) && !hidden.has(f.to));
              flowsRef.current = visible;
              const chart = chartRef.current;
              if (!chart) return;
              const ds = chart.data.datasets[0] as any;
              ds.data = visible;
              chart.update();
            },
            labels: {
              color: '#ffffff',
              font: { size: 11, family: 'Overpass, sans-serif' },
              usePointStyle: true,
              pointStyle: 'rectRounded',
              padding: 12,
              generateLabels: () => legendItemsRef.current.map(item => {
                const isHidden = hiddenRef.current.has(item.key);
                return {
                  text: item.text,
                  fillStyle: isHidden ? 'rgba(80,80,80,0.4)' : item.fillStyle,
                  fontColor: isHidden ? 'rgba(255,255,255,0.3)' : '#ffffff',
                  strokeStyle: 'transparent',
                  lineWidth: 0,
                  hidden: false,
                  pointStyle: 'rectRounded' as const,
                };
              }),
            },
          },
          tooltip: {
            backgroundColor: '#171d2b',
            titleColor: '#ffffff',
            bodyColor: '#cccccc',
            titleFont: { size: 13, family: 'Overpass', weight: 'bold' },
            bodyFont:  { size: 12, family: 'Overpass' },
            padding: 10,
            callbacks: {
              label: (ctx) => {
                const d = flowsRef.current[ctx.dataIndex];
                if (!d) return '';
                const lbl = labelsRef.current;
                const fromName = lbl[d.from] || d.from;
                const toName   = lbl[d.to]   || d.to;
                const fromTotal = flowsRef.current.reduce((s, f) => f.from === d.from ? s + f.flow : s, 0);
                const pct = fromTotal > 0 ? ((d.flow / fromTotal) * 100).toFixed(1) : '0.0';
                return `${fromName} ${String.fromCharCode(8594)} ${toName}: ${d.flow.toLocaleString()} (${pct}%)`;
              },
              title: () => '',
            },
          },
        },
      },
    });

    return () => {
      canvas.removeEventListener('click', handleClick);
      chartRef.current?.destroy();
      chartRef.current = null;
    };
  }, []);

  // ── Clear hidden/selected when mode changes ─────────────────────────
  useEffect(() => {
    hiddenRef.current.clear();
    selectedRef.current.clear();
  }, [isWorkplaceMode, groupBySector]);

  // ── Update data when bindings or mode change ───────────────────────────
  useEffect(() => {
    const { ageEdu, eduNonWork, eduWorkSector, eduWorkJobEdu, livingWorkerEdu, workerEduJobEdu, jobEduSector } = data;
    const flows: SankeyFlow[]            = [];
    const labels: Record<string, string> = {};

    if (!isWorkplaceMode) {
      // ── Workforce view: Demographics → Education → Employment ─────────
      const workerNames = groupBySector ? WF_SECTOR_NAMES : WF_JOB_EDU_NAMES;
      [...AGE_NAMES, ...EDU_NAMES, ...NON_WORK_NAMES, ...workerNames].forEach(n => { labels[n] = n; });

      // Col 1 → Col 2: Age → Education
      for (let a = 0; a < 4; a++)
        for (let e = 0; e < 5; e++) {
          const v = ageEdu[a * 5 + e] ?? 0;
          if (v > 0) flows.push({ from: AGE_NAMES[a], to: EDU_NAMES[e], flow: v });
        }

      // Col 2 → Col 3: Education → Non-work (shared between both groupings)
      for (let e = 0; e < 5; e++)
        for (let n = 0; n < 4; n++) {
          const v = eduNonWork[e * 4 + n] ?? 0;
          if (v > 0) flows.push({ from: EDU_NAMES[e], to: NON_WORK_NAMES[n], flow: v });
        }

      // Col 2 → Col 3: Education → Workers (grouped by sector or job edu)
      if (groupBySector) {
        for (let e = 0; e < 5; e++)
          for (let s = 0; s < 5; s++) {
            const v = eduWorkSector[e * 5 + s] ?? 0;
            if (v > 0) flows.push({ from: EDU_NAMES[e], to: WF_SECTOR_NAMES[s], flow: v });
          }
      } else {
        for (let e = 0; e < 5; e++)
          for (let j = 0; j < 5; j++) {
            const v = eduWorkJobEdu[e * 5 + j] ?? 0;
            if (v > 0) flows.push({ from: EDU_NAMES[e], to: WF_JOB_EDU_NAMES[j], flow: v });
          }
      }

    } else {
      // ── Workplace view: Living → Worker Edu → Job Edu → Sector ────────
      LIVING_NAMES.forEach(k  => { labels[k] = k.replace('L: ', ''); });
      WP_EDU_NAMES.forEach((k, i) => { labels[k] = EDU_NAMES[i]; });
      JOB_EDU_NAMES.forEach((k, i) => { labels[k] = EDU_NAMES[i]; });
      WP_SECTOR_NAMES.forEach(k => { labels[k] = k.replace('S: ', ''); });

      // Col 1 → Col 2: Living Place → Worker Education
      for (let l = 0; l < 2; l++)
        for (let e = 0; e < 5; e++) {
          const v = livingWorkerEdu[l * 5 + e] ?? 0;
          if (v > 0) flows.push({ from: LIVING_NAMES[l], to: WP_EDU_NAMES[e], flow: v });
        }

      // Col 2 → Col 3: Worker Education → Job Education Needed
      for (let we = 0; we < 5; we++)
        for (let je = 0; je < 5; je++) {
          const v = workerEduJobEdu[we * 5 + je] ?? 0;
          if (v > 0) flows.push({ from: WP_EDU_NAMES[we], to: JOB_EDU_NAMES[je], flow: v });
        }

      // Col 3 → Col 4: Job Education Needed → Sector
      for (let je = 0; je < 5; je++)
        for (let s = 0; s < 5; s++) {
          const v = jobEduSector[je * 5 + s] ?? 0;
          if (v > 0) flows.push({ from: JOB_EDU_NAMES[je], to: WP_SECTOR_NAMES[s], flow: v });
        }
    }

    flowsRef.current  = flows;
    allFlowsRef.current = flows;
    labelsRef.current  = labels;

    // Build legend items from unique nodes
    const legendNodes: {text: string; fillStyle: string; key: string}[] = [];
    const seen = new Set<string>();
    for (const f of flows) {
      for (const key of [f.from, f.to]) {
        if (!seen.has(key)) {
          seen.add(key);
          legendNodes.push({ text: labels[key] || key, fillStyle: NODE_COLORS[key] ?? DEFAULT_COLOR, key });
        }
      }
    }
    legendItemsRef.current = legendNodes;

    // Apply hidden filter (preserved across data updates, cleared on mode change)
    const hidden = hiddenRef.current;
    const visible = hidden.size > 0
      ? flows.filter(f => !hidden.has(f.from) && !hidden.has(f.to))
      : flows;
    flowsRef.current = visible;

    if (!chartRef.current) return;
    const ds = chartRef.current.data.datasets[0] as any;
    ds.data   = visible;
    ds.labels = labels;
    chartRef.current.update('none');
  }, [data, isWorkplaceMode, groupBySector]);

  // ── Resize observer ────────────────────────────────────────────────────
  useEffect(() => {
    if (!containerRef.current || !canvasRef.current) return;
    const ro = new ResizeObserver(entries => {
      const { width, height } = entries[0].contentRect;
      if (canvasRef.current && width > 0 && height > 0) {
        canvasRef.current.width  = width;
        canvasRef.current.height = height;
      }
      chartRef.current?.resize();
    });
    ro.observe(containerRef.current);
    return () => ro.disconnect();
  }, []);

  return (
    <div ref={containerRef} className={styles.chartWrapper}>
      <canvas ref={canvasRef} style={{ display: 'block', width: '100%', height: '100%', cursor: 'pointer' }} />
    </div>
  );
};

const PipelineChart = memo(PipelineChartComponent);

// ── Main panel ────────────────────────────────────────────────────────────────
const WorkforcePipelineSankey: React.FC<DraggablePanelProps> = ({ onClose, initialPosition }) => {
  const isWorkplaceMode = useValue(viewMode$);
  const [groupBySector, setGroupBySector] = useState(true);
  const workforceColumns = (
    <>
      <span>Demographics</span>
      <span>Education</span>
      <span>Employment</span>
    </>
  );

  const workplaceColumns = (
    <>
      <span>Living Place</span>
      <span>Worker Education</span>
      <span>Education Needed</span>
      <span>Employment Sector</span>
    </>
  );

  return (
    <Panel
      draggable
      onClose={onClose}
      initialPosition={initialPosition}
      className={styles.panel}
      header={
        <div className={styles.header}>
          <span className={styles.title}>Workforce &amp; Education Sankey</span>
        </div>
      }
    >
      <div className={styles.toolbar}>
        <button
          className={`${styles.toggleBtn} ${!isWorkplaceMode ? styles.active : ''}`}
          onClick={() => setViewMode(false)}
        >
          Workforce
        </button>
        <button
          className={`${styles.toggleBtn} ${isWorkplaceMode ? styles.active : ''}`}
          onClick={() => setViewMode(true)}
        >
          Workplace
        </button>
        {!isWorkplaceMode && (
          <>
            <span className={styles.groupLabel}>Group jobs by:</span>
            <button
              className={`${styles.toggleBtn} ${groupBySector ? styles.active : ''}`}
              onClick={() => setGroupBySector(true)}
            >
              Sector
            </button>
            <button
              className={`${styles.toggleBtn} ${!groupBySector ? styles.active : ''}`}
              onClick={() => setGroupBySector(false)}
            >
              Education
            </button>
          </>
        )}
      </div>
      <div className={styles.columnHeaders}>
        {isWorkplaceMode ? workplaceColumns : workforceColumns}
      </div>
      <PipelineChart isWorkplaceMode={isWorkplaceMode} groupBySector={groupBySector} />
    </Panel>
  );
};

export default WorkforcePipelineSankey;
