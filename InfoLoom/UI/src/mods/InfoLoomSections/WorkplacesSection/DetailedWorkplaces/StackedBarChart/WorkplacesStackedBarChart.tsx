import React, { useEffect, useRef, useMemo } from 'react';
import Chart from 'chart.js/auto';
import { workplacesInfo } from 'mods/domain/WorkplacesInfo';

export interface WorkplaceStackedBarChartProps {
  workplaces: workplacesInfo[];
  educationLevels: Array<{ name: string | null; color: string; data: workplacesInfo }>;
  chartType: 'sector' | 'employment';
  sectorLabels: {
    service: string | null;
    commercial: string | null;
    leisure: string | null;
    extractor: string | null;
    industrial: string | null;
    office: string | null;
  };
  employmentLabels: {
    employee: string | null;
    commuter: string | null;
    open: string | null;
  };
  totalLabel: string | null;
  chartTitle: string | null;
}

const WorkplacesStackedBarChart: React.FC<WorkplaceStackedBarChartProps> = ({
  workplaces,
  educationLevels,
  chartType,
  sectorLabels,
  employmentLabels,
  totalLabel,
  chartTitle,
}) => {
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const chartRef = useRef<Chart | null>(null);
  const containerRef = useRef<HTMLDivElement | null>(null);

  const labels = useMemo(
    () => [...educationLevels.map(l => l.name ?? ''), totalLabel ?? ''],
    [educationLevels, totalLabel]
  );

  const allLevels = useMemo(() => [...educationLevels.map(l => l.data), workplaces[5]], [educationLevels, workplaces]);

  const chartData = useMemo(() => {
    if (chartType === 'employment') {
      return {
        labels,
        datasets: [
          {
            label: employmentLabels.employee ?? 'Employee',
            data: allLevels.map(d => d.Employee),
            backgroundColor: '#4CAF50',
          },
          {
            label: employmentLabels.commuter ?? 'Commuter',
            data: allLevels.map(d => d.Commuter),
            backgroundColor: '#FF9800',
          },
          {
            label: employmentLabels.open ?? 'Open',
            data: allLevels.map(d => d.Open),
            backgroundColor: '#F44336',
          },
        ],
      };
    }
    return {
      labels,
      datasets: [
        {
          label: sectorLabels.service ?? 'Service',
          data: allLevels.map(d => d.Service),
          backgroundColor: '#4287f5',
        },
        {
          label: sectorLabels.commercial ?? 'Commercial',
          data: allLevels.map(d => d.Commercial),
          backgroundColor: '#f5d142',
        },
        {
          label: sectorLabels.leisure ?? 'Leisure',
          data: allLevels.map(d => d.Leisure),
          backgroundColor: '#f542a7',
        },
        {
          label: sectorLabels.extractor ?? 'Extractor',
          data: allLevels.map(d => d.Extractor),
          backgroundColor: '#8c42f5',
        },
        {
          label: sectorLabels.industrial ?? 'Industrial',
          data: allLevels.map(d => d.Industrial),
          backgroundColor: '#f55142',
        },
        {
          label: sectorLabels.office ?? 'Office',
          data: allLevels.map(d => d.Office),
          backgroundColor: '#42f5b3',
        },
      ],
    };
  }, [labels, allLevels, chartType, sectorLabels, employmentLabels]);

  useEffect(() => {
    if (!canvasRef.current || !containerRef.current) return;
    const ctx = canvasRef.current.getContext('2d');
    if (!ctx) return;

    chartRef.current = new Chart(ctx, {
      type: 'bar',
      data: chartData,
      options: {
        indexAxis: 'y',
        responsive: false,
        maintainAspectRatio: false,
        animation: { duration: 0 },
        plugins: {
          title: {
            display: true,
            text: chartTitle ?? '',
            color: '#ffffff',
            font: { size: 14, family: 'Overpass' },
          },
          legend: {
            position: 'bottom',
            labels: {
              color: '#ffffff',
              padding: 10,
              font: { size: 12, family: 'Overpass' },
            },
          },
          tooltip: {
            mode: 'index',
            intersect: false,
            backgroundColor: '#171d2b',
            titleFont: { weight: 'bold', size: 13, family: 'Overpass' },
            bodyFont: { size: 11, family: 'Overpass' },
            footerFont: { weight: 'bold', size: 11, family: 'Overpass' },
            padding: 8,
            callbacks: {
              label: context => {
                const val = (context.raw as number) || 0;
                return `${context.dataset.label}: ${val.toLocaleString()}`;
              },
              footer: tooltipItems => {
                let total = 0;
                tooltipItems.forEach(item => {
                  total += Number(item.raw) || 0;
                });
                return `Total: ${total.toLocaleString()}`;
              },
            },
          },
        },
        scales: {
          x: {
            stacked: true,
            grid: { color: 'rgba(255, 255, 255, 0.1)' },
            ticks: { color: '#ffffff', font: { size: 11, family: 'Overpass' } },
          },
          y: {
            stacked: true,
            grid: { color: 'rgba(255, 255, 255, 0.1)' },
            ticks: { color: '#ffffff', font: { size: 12, family: 'Overpass' } },
          },
        },
        datasets: {
          bar: { barThickness: 20, maxBarThickness: 30 },
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

  useEffect(() => {
    if (!chartRef.current) return;
    chartRef.current.data = chartData;
    if (chartRef.current.options.plugins?.title) {
      (chartRef.current.options.plugins.title as any).text = chartTitle ?? '';
    }
    chartRef.current.update('none');
  }, [chartData, chartTitle]);

  useEffect(() => {
    if (!containerRef.current || !canvasRef.current) return;
    const resizeObserver = new ResizeObserver(entries => {
      if (!entries[0]) return;
      const { width, height } = entries[0].contentRect;
      if (canvasRef.current && width > 0 && height > 0) {
        canvasRef.current.width = width;
        canvasRef.current.height = height;
      }
      if (chartRef.current) chartRef.current.resize();
    });
    resizeObserver.observe(containerRef.current);
    return () => resizeObserver.disconnect();
  }, []);

  return (
    <div ref={containerRef} style={{ width: '100%', height: '300rem', position: 'relative' }}>
      <canvas ref={canvasRef} style={{ height: '0', width: '0', display: 'block' }} />
    </div>
  );
};

export default WorkplacesStackedBarChart;
