import { workforceInfo } from '../../../../domain/workforceInfo';
import React, { useEffect, useMemo, useRef } from 'react';
import Chart from 'chart.js/auto';

interface WorkforceChartProps {
  workforce: workforceInfo[];
  educationLevels: Array<{ name: string | null; color: string; data: workforceInfo }>;
  segmentLabels: {
    worker: string | null;
    unemployed: string | null;
    under: string | null;
    outside: string | null;
    homeless: string | null;
  };
  totalLabel: string | null;
  chartTitle: string | null;
}

export const WorkforceStackedBarChart: React.FC<WorkforceChartProps> = ({
  workforce,
  educationLevels,
  segmentLabels,
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

  const allLevels = useMemo(() => [...educationLevels.map(l => l.data), workforce[5]], [educationLevels, workforce]);

  // Use non-overlapping segments that sum to Total:
  // Employee = Worker - Outside (city workers, includes underemployed)
  // Unemployed = Total - Worker (truly not working, no Worker component)
  // Outside = workers at outside connections
  // Note: Under ⊂ Employee, Homeless is orthogonal — shown in tooltip only
  const chartData = useMemo(
    () => ({
      labels,
      datasets: [
        {
          label: segmentLabels.worker ?? 'Employee',
          data: allLevels.map(d => d.Worker - d.Outside),
          backgroundColor: '#4CAF50',
        },
        {
          label: segmentLabels.unemployed ?? 'Unemployed',
          data: allLevels.map(d => d.Total - d.Worker),
          backgroundColor: '#F44336',
        },
        {
          label: segmentLabels.outside ?? 'Outside',
          data: allLevels.map(d => d.Outside),
          backgroundColor: '#607D8B',
        },
      ],
    }),
    [labels, allLevels, segmentLabels]
  );

  // Initialize chart once
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
              afterBody: tooltipItems => {
                const idx = tooltipItems[0]?.dataIndex;
                if (idx === undefined || !allLevels[idx]) return '';
                const d = allLevels[idx];
                const lines: string[] = [];
                lines.push(`${segmentLabels.under ?? 'Under'}: ${d.Under.toLocaleString()}`);
                lines.push(`${segmentLabels.homeless ?? 'Homeless'}: ${d.Homeless.toLocaleString()}`);
                return lines;
              },
              footer: tooltipItems => {
                const idx = tooltipItems[0]?.dataIndex;
                if (idx === undefined || !allLevels[idx]) return '';
                return `Total: ${allLevels[idx].Total.toLocaleString()}`;
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
          bar: {
            barThickness: 20,
            maxBarThickness: 30,
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

  // Update chart data when workforce changes
  useEffect(() => {
    if (!chartRef.current) return;

    const hiddenStates = chartRef.current.data.datasets.map(
      (_, index) => chartRef.current!.getDatasetMeta(index).hidden
    );

    chartRef.current.data = chartData;

    chartRef.current.data.datasets.forEach((_, index) => {
      if (hiddenStates[index] !== undefined && hiddenStates[index] !== null) {
        chartRef.current!.getDatasetMeta(index).hidden = hiddenStates[index];
      }
    });

    chartRef.current.update('none');
  }, [chartData]);

  // Handle resize
  useEffect(() => {
    if (!containerRef.current || !canvasRef.current) return;

    const resizeObserver = new ResizeObserver(entries => {
      if (!entries[0]) return;
      const { width, height } = entries[0].contentRect;
      if (canvasRef.current && width > 0 && height > 0) {
        canvasRef.current.width = width;
        canvasRef.current.height = height;
      }
      if (chartRef.current) {
        chartRef.current.resize();
      }
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
