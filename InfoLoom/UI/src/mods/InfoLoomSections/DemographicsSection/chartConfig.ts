import { ChartConfiguration } from 'chart.js';

export interface ChartColors {
  work: string;
  elementary: string;
  highSchool: string;
  college: string;
  university: string;
  Retired: string;
  Unemployed: string;
  Uneducated: string;
  PoorlyEducated: string;
  Educated: string;
  WellEducated: string;
  HighlyEducated: string;
  ChildOrTeenWithNoSchool: string;
  Wretched: string;
  Poor: string;
  Modest: string;
  Comfortable: string;
  Wealthy: string;
  LowDensity: string;
  MediumDensity: string;
  HighDensity: string;
  MixedUse: string;
  Unhoused: string;
}

export const CHART_COLORS: ChartColors = {
  work: '#624532',
  elementary: '#7E9EAE',
  highSchool: '#00C217',
  college: '#005C4E',
  university: '#2462FF',
  Retired: '#A1A1A1',
  Unemployed: '#FF0000',
  Uneducated: '#808080',
  PoorlyEducated: '#B09868',
  Educated: '#368A2E',
  WellEducated: '#B981C0',
  HighlyEducated: '#5796D1',
  ChildOrTeenWithNoSchool: '#ff5e00ff',
  // Wealth tiers: red (poorest) through green (wealthiest)
  Wretched: '#B33A3A',
  Poor: '#C97A3C',
  Modest: '#C9B23C',
  Comfortable: '#7DBF5E',
  Wealthy: '#3F9E5C',
  // Residency density tiers
  LowDensity: '#7E9EAE',
  MediumDensity: '#5796D1',
  HighDensity: '#2462FF',
  MixedUse: '#B981C0',
  Unhoused: '#808080',
};

// Bar/height sizing scales with how many rows the currently selected row dimension has —
// anywhere from 4 (Wealth/Residency/Lifecycle-Age) to 120 (Detailed-Age). Replaces the old
// fixed-tier lookup that assumed the row axis was always one of Age's four granularities.
interface RowSizing {
  barThickness: number;
  barPercentage: number;
  categoryPercentage: number;
  maxBarThickness: number;
  height: number;
}

function getRowSizing(rowCategoryCount: number): RowSizing {
  if (rowCategoryCount <= 8) {
    return { barThickness: 32, barPercentage: 0.95, categoryPercentage: 0.95, maxBarThickness: 50, height: 220 };
  }
  if (rowCategoryCount <= 12) {
    return { barThickness: 25, barPercentage: 0.85, categoryPercentage: 0.9, maxBarThickness: 35, height: 400 };
  }
  if (rowCategoryCount <= 24) {
    return { barThickness: 15, barPercentage: 0.9, categoryPercentage: 0.85, maxBarThickness: 25, height: 600 };
  }
  return {
    barThickness: 8,
    barPercentage: 0.98,
    categoryPercentage: 0.95,
    maxBarThickness: 5,
    height: Math.min(2500, rowCategoryCount * 20),
  };
}

export function createChartConfig(
  rowCategoryCount: number,
  rowLabel: string,
  initialData: { labels: string[]; datasets: any[] }
): ChartConfiguration<'bar'> {
  const sizing = getRowSizing(rowCategoryCount);
  return {
    type: 'bar',
    data: initialData,
    options: {
      indexAxis: 'y',
      responsive: false,
      maintainAspectRatio: false,
      plugins: {
        title: {
          display: true,
          text: `Population Demographics by ${rowLabel}`,
          color: '#ffffff',
          font: { size: 16, family: 'Overpass' },
        },
        tooltip: {
          mode: 'index',
          intersect: true,
          position: 'nearest',
          caretSize: 5,
          backgroundColor: '#171d2b',
          titleFont: { weight: 'bold', size: 14, family: 'Overpass' },
          bodyFont: { size: 12, family: 'Overpass' },
          footerFont: { weight: 'bold', size: 12, family: 'Overpass' },
          padding: 10,
          callbacks: {
            title: tooltipItems => {
              const item = tooltipItems[0];
              return `${rowLabel}: ${item.label}`;
            },
            label: context => {
              const formattedNumber = context.raw ? (context.raw as number).toLocaleString() : '0';
              return `${context.dataset.label}: ${formattedNumber}`;
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
        legend: {
          position: 'top',
          labels: { color: '#ffffff', padding: 15, font: { size: 16, family: 'Overpass' } },
          onClick: (e, legendItem, legend) => {
            const index = legendItem.datasetIndex;
            if (index === undefined) return;

            const ci = legend.chart;
            if (ci.isDatasetVisible(index)) {
              ci.hide(index);
              legendItem.hidden = true;
            } else {
              ci.show(index);
              legendItem.hidden = false;
            }
          },
        },
      },
      scales: {
        x: {
          stacked: true,
          grid: { color: 'rgba(255, 255, 255, 0.1)' },
          ticks: { color: '#ffffff', font: { size: 12, family: 'Overpass' } },
          title: {
            display: true,
            text: 'Number of People',
            color: '#ffffff',
            font: { family: 'Overpass' },
          },
        },
        y: {
          stacked: true,
          grid: { color: 'rgba(255, 255, 255, 0.1)' },
          ticks: {
            color: '#ffffff',
            autoSkip: rowCategoryCount > 30,
            maxTicksLimit: rowCategoryCount > 30 ? 30 : 20,
            padding: rowCategoryCount > 30 ? 8 : 2,
            font: { size: 12, family: 'Overpass' },
          },
          afterFit: function (scaleInstance) {
            scaleInstance.height = sizing.height;
          },
          title: {
            display: true,
            text: rowLabel,
            color: '#ffffff',
            font: { family: 'Overpass' },
          },
        },
      },
      datasets: {
        bar: {
          barThickness: sizing.barThickness,
          barPercentage: sizing.barPercentage,
          categoryPercentage: sizing.categoryPercentage,
          maxBarThickness: sizing.maxBarThickness,
        },
      },
      animation: { duration: 0 },
    },
  };
}

export function updateChartOptionsForGrouping(chart: any, rowCategoryCount: number, rowLabel: string): void {
  if (!chart.options) return;

  const sizing = getRowSizing(rowCategoryCount);

  // Update bar sizing
  chart.options.datasets = {
    bar: {
      barThickness: sizing.barThickness,
      barPercentage: sizing.barPercentage,
      categoryPercentage: sizing.categoryPercentage,
      maxBarThickness: sizing.maxBarThickness,
    },
  };

  // Update title/tooltip/axis text
  if (chart.options.plugins?.title) {
    chart.options.plugins.title.text = `Population Demographics by ${rowLabel}`;
  }
  if (chart.options.plugins?.tooltip?.callbacks) {
    chart.options.plugins.tooltip.callbacks.title = (tooltipItems: any) => `${rowLabel}: ${tooltipItems[0].label}`;
  }

  // Update scale configurations
  if (chart.options.scales?.y) {
    const yScale = chart.options.scales.y;

    yScale.afterFit = function (scaleInstance: any) {
      scaleInstance.height = sizing.height;
    };

    if (yScale.ticks) {
      const ticks = yScale.ticks as any;
      ticks.autoSkip = rowCategoryCount > 30;
      ticks.maxTicksLimit = rowCategoryCount > 30 ? 30 : 20;
      ticks.padding = rowCategoryCount > 30 ? 15 : 8;
      ticks.lineHeight = rowCategoryCount > 30 ? 5 : 1;
    }

    if (yScale.title) {
      yScale.title.text = rowLabel;
    }
  }
}
