import { ChartConfiguration } from 'chart.js';
import { GroupingStrategy } from '../../domain/GroupingStrategy';

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
};

export function createChartConfig(
  groupingStrategy: GroupingStrategy,
  initialData: { labels: string[]; datasets: any[] }
): ChartConfiguration<'bar'> {
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
          text: 'Population Demographics by Age',
          color: '#ffffff',
          font: { size: 16 },
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
              return `Age: ${item.label}`;
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
          },
        },
        y: {
          stacked: true,
          grid: { color: 'rgba(255, 255, 255, 0.1)' },
          ticks: {
            color: '#ffffff',
            autoSkip: groupingStrategy === GroupingStrategy.None,
            maxTicksLimit: groupingStrategy === GroupingStrategy.None ? 30 : 20,
            padding: groupingStrategy === GroupingStrategy.None ? 8 : 2,
            font: { size: 12, family: 'Overpass' },
          },
          afterFit: function (scaleInstance) {
            // Set different heights based on grouping
            if (groupingStrategy === GroupingStrategy.None) {
              scaleInstance.height = Math.min(2500, scaleInstance.height);
            } else if (groupingStrategy === GroupingStrategy.LifeCycle) {
              // Compact height for 4 categories
              scaleInstance.height = 200;
            } else if (groupingStrategy === GroupingStrategy.TenYear) {
              // Compact height for 12 categories
              scaleInstance.height = 400;
            } else {
              // Compact height for 24 categories (5-year)
              scaleInstance.height = 600;
            }
          },
          title: {
            display: true,
            text: 'Age',
            color: '#ffffff',
          },
        },
      },
      datasets: {
        bar: {
          barThickness:
            groupingStrategy === GroupingStrategy.None
              ? 8
              : groupingStrategy === GroupingStrategy.FiveYear
                ? 15
                : groupingStrategy === GroupingStrategy.TenYear
                  ? 25
                  : 35,
          barPercentage:
            groupingStrategy === GroupingStrategy.None
              ? 0.98
              : groupingStrategy === GroupingStrategy.FiveYear
                ? 0.9
                : groupingStrategy === GroupingStrategy.TenYear
                  ? 0.85
                  : 0.95,
          categoryPercentage:
            groupingStrategy === GroupingStrategy.None
              ? 0.95
              : groupingStrategy === GroupingStrategy.FiveYear
                ? 0.85
                : groupingStrategy === GroupingStrategy.TenYear
                  ? 0.9
                  : 0.95,
          maxBarThickness:
            groupingStrategy === GroupingStrategy.None
              ? 5
              : groupingStrategy === GroupingStrategy.FiveYear
                ? 25
                : groupingStrategy === GroupingStrategy.TenYear
                  ? 35
                  : 50,
        },
      },
      animation: { duration: 0 },
    },
  };
}

export function updateChartOptionsForGrouping(chart: any, groupingStrategy: GroupingStrategy): void {
  if (!chart.options) return;

  // Update bar sizing
  chart.options.datasets = {
    bar: {
      barThickness:
        groupingStrategy === GroupingStrategy.None
          ? 8
          : groupingStrategy === GroupingStrategy.FiveYear
            ? 15
            : groupingStrategy === GroupingStrategy.TenYear
              ? 25
              : 35,
      barPercentage:
        groupingStrategy === GroupingStrategy.None
          ? 0.98
          : groupingStrategy === GroupingStrategy.FiveYear
            ? 0.9
            : groupingStrategy === GroupingStrategy.TenYear
              ? 0.85
              : 0.95,
      categoryPercentage:
        groupingStrategy === GroupingStrategy.None
          ? 0.95
          : groupingStrategy === GroupingStrategy.FiveYear
            ? 0.85
            : groupingStrategy === GroupingStrategy.TenYear
              ? 0.9
              : 0.95,
      maxBarThickness:
        groupingStrategy === GroupingStrategy.None
          ? 5
          : groupingStrategy === GroupingStrategy.FiveYear
            ? 25
            : groupingStrategy === GroupingStrategy.TenYear
              ? 35
              : 50,
    },
  };

  // Update scale configurations
  if (chart.options.scales?.y) {
    const yScale = chart.options.scales.y;

    yScale.afterFit = function (scaleInstance: any) {
      // Set different heights based on grouping
      if (groupingStrategy === GroupingStrategy.None) {
        scaleInstance.height = Math.min(2500, scaleInstance.height);
      } else if (groupingStrategy === GroupingStrategy.LifeCycle) {
        // Compact height for 4 categories
        scaleInstance.height = 200;
      } else if (groupingStrategy === GroupingStrategy.TenYear) {
        // Compact height for 12 categories
        scaleInstance.height = 400;
      } else {
        // Compact height for 24 categories (5-year)
        scaleInstance.height = 600;
      }
    };

    if (yScale.ticks) {
      const ticks = yScale.ticks as any;
      ticks.autoSkip = groupingStrategy === GroupingStrategy.None;
      ticks.maxTicksLimit = groupingStrategy === GroupingStrategy.None ? 30 : 20;
      ticks.padding = groupingStrategy === GroupingStrategy.None ? 15 : 8;
      ticks.lineHeight = groupingStrategy === GroupingStrategy.None ? 5 : 1;
    }
  }
}
