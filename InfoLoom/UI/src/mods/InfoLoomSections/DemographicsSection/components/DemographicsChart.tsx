import React, { memo, useEffect, useRef } from 'react';
import Chart from 'chart.js/auto';
import { createChartConfig, updateChartOptionsForGrouping } from '../chartConfig';
import { useChartData } from '../hooks';
import { AgeGranularity, CensusDimension, CensusLabels, getCensusCategoryCount } from '../census';
import styles from '../Demographics.module.scss';

interface DemographicsChartProps {
  censusCounts: number[];
  censusRowDim: CensusDimension;
  censusColDim: CensusDimension;
  censusAgeGranularity: AgeGranularity;
  censusLabels: CensusLabels;
}

const DemographicsChartComponent = ({
  censusCounts,
  censusRowDim,
  censusColDim,
  censusAgeGranularity,
  censusLabels,
}: DemographicsChartProps): JSX.Element => {
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const chartRef = useRef<Chart | null>(null);
  const containerRef = useRef<HTMLDivElement | null>(null);

  // Use custom hook for chart data transformation
  const chartData = useChartData(censusCounts, censusRowDim, censusColDim, censusAgeGranularity, censusLabels);
  const rowCategoryCount = getCensusCategoryCount(censusRowDim, censusAgeGranularity);
  const rowLabel = censusLabels.dimensionNames[censusRowDim];

  // Initialize chart ONLY ONCE
  useEffect(() => {
    if (!canvasRef.current || !containerRef.current) return;
    const ctx = canvasRef.current.getContext('2d');
    if (!ctx) return;
    ctx.canvas.width = 200;
    ctx.canvas.height = 200;

    const config = createChartConfig(rowCategoryCount, rowLabel, chartData);
    chartRef.current = new Chart(ctx, config);

    // Clean up on unmount
    return () => {
      if (chartRef.current && canvasRef.current) {
        chartRef.current.destroy();
        chartRef.current = null;
      }
    };
  }, []);

  // Update chart data when it changes
  useEffect(() => {
    if (!chartRef.current) return;

    // Store the current hidden state of each dataset before updating
    const hiddenStates = chartRef.current.data.datasets.map(
      (dataset, index) => chartRef.current!.getDatasetMeta(index).hidden
    );

    // Update the data
    chartRef.current.data = chartData;

    // Restore the hidden states after updating
    chartRef.current.data.datasets.forEach((dataset, index) => {
      if (hiddenStates[index] !== undefined && hiddenStates[index] !== null) {
        const meta = chartRef.current!.getDatasetMeta(index);
        meta.hidden = hiddenStates[index];
      }
    });

    chartRef.current.update('none');
  }, [chartData]);

  // Handle resize events
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

  // Update chart options when the row dimension (or its category count) changes
  useEffect(() => {
    if (!chartRef.current) return;

    updateChartOptionsForGrouping(chartRef.current, rowCategoryCount, rowLabel);
    chartRef.current.update('none');
  }, [rowCategoryCount, rowLabel]);

  const INITIAL_CANVAS_STYLE = { height: `0`, width: '0', display: 'block' };

  return (
    <div className={styles.chartContainer} ref={containerRef} style={{ width: '100%', height: '100%' }}>
      <canvas ref={canvasRef} style={INITIAL_CANVAS_STYLE} />
    </div>
  );
};

export const DemographicsChart = memo(DemographicsChartComponent);
