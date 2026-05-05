import React, { FC, useState, useEffect, useRef, useMemo } from 'react';
import { useValue, bindLocalValue, bindValue } from 'cs2/api';
import { WorkforceData } from 'mods/bindings';
import { SelectedInfoSectionBase, Theme } from 'cs2/bindings';
import { getModule } from 'cs2/modding';
import { DraggablePanelProps, Panel, Tooltip, PanelFoldout, Button, Scrollable } from 'cs2/ui';
import styles from './Workforce.module.scss';
import { workforceInfo } from '../../domain/workforceInfo';
import { LocalizedNumber, LocalizedPercentage, useLocalization } from 'cs2/l10n';
import mod from 'mod.json';
import { InfoRowSCSS } from 'mods/InfoLoomSections/ILInfoSections/Modules/info-Row/info-Row.module.scss';
import { DistrictSelector } from '../../InfoloomInfoviewContents/DistrictSelector/districtSelector';
import { infoview } from 'cs2/bindings';
import classNames from 'classnames';
import Chart from 'chart.js/auto';
import { Localekeys } from 'mods/locale';

const ShowExtraWorkforce = bindValue<number>(mod.id, 'ShowExtraWorkforce', 0);
export const InfoRowTheme: Theme | any = getModule(
  'game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.module.scss',
  'classes'
);

interface Props {
  value: number;
  start: number;
  end: number;
  step?: number;
  onChange: (newValue: number) => void;
}

const Slider: FC<Props> = getModule('game-ui/common/input/slider/slider.tsx', 'Slider');
const SliderRange = bindLocalValue<number[]>([0, 7]);
export const panelVisibleBinding = bindLocalValue(false);
export const panelTrigger = (state: boolean) => {
  panelVisibleBinding.update(state);
};

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

const WorkforceStackedBarChart: React.FC<WorkforceChartProps> = ({
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

  const allLevels = useMemo(
    () => [...educationLevels.map(l => l.data), workforce[5]],
    [educationLevels, workforce]
  );

  // Use non-overlapping segments that sum to Total:
  // Employee = Worker - Outside (city workers, includes underemployed)
  // Unemployed = Total - Worker (truly not working, no Worker component)
  // Outside = workers at outside connections
  // Note: Under ⊂ Employee, Homeless is orthogonal — shown in tooltip only
  const chartData = useMemo(() => ({
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
  }), [labels, allLevels, segmentLabels]);

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
              label: (context) => {
                const val = (context.raw as number) || 0;
                return `${context.dataset.label}: ${val.toLocaleString()}`;
              },
              afterBody: (tooltipItems) => {
                const idx = tooltipItems[0]?.dataIndex;
                if (idx === undefined || !allLevels[idx]) return '';
                const d = allLevels[idx];
                const lines: string[] = [];
                lines.push(`${segmentLabels.under ?? 'Under'}: ${d.Under.toLocaleString()}`);
                lines.push(`${segmentLabels.homeless ?? 'Homeless'}: ${d.Homeless.toLocaleString()}`);
                return lines;
              },
              footer: (tooltipItems) => {
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

    const hiddenStates = chartRef.current.data.datasets.map((_, index) =>
      chartRef.current!.getDatasetMeta(index).hidden
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
    <div
      ref={containerRef}
      style={{ width: '100%', height: '300rem', position: 'relative' }}
    >
      <canvas ref={canvasRef} style={{ height: '0', width: '0', display: 'block' }} />
    </div>
  );
};

// Header component
const WorkforceTableHeader: React.FC<{ translations: any }> = ({ translations }) => {
  const value = ShowExtraWorkforce.value;

  return (
    <div className={styles.headerRow}>
      <div className={styles.col1}>
        <span>Education</span>
      </div>
      <div className={styles.col2}>
        <Tooltip tooltip={translations?.totalTooltip} direction="down" alignment="center">
          <span>Total</span>
        </Tooltip>
      </div>
      {value < 7 && (
        <div className={styles.col3}>
          <Tooltip tooltip={translations?.percentTooltip} direction="down" alignment="center">
            <span>%</span>
          </Tooltip>
        </div>
      )}
      {value < 6 && (
        <div className={styles.col4}>
          <Tooltip tooltip={translations?.workerTooltip} direction="down" alignment="center">
            <span>Worker</span>
          </Tooltip>
        </div>
      )}
      {value < 5 && (
        <div className={styles.col5}>
          <Tooltip tooltip={translations?.unemployedTooltip} direction="down" alignment="center">
            <span>Unemployed</span>
          </Tooltip>
        </div>
      )}
      {value < 4 && (
        <div className={styles.col6}>
          <Tooltip tooltip={translations?.unemploymentTooltip} direction="down" alignment="center">
            <span>%</span>
          </Tooltip>
        </div>
      )}
      {value < 3 && (
        <div className={styles.col7}>
          <Tooltip tooltip={translations?.underTooltip} direction="down" alignment="center">
            <span>Under</span>
          </Tooltip>
        </div>
      )}
      {value < 2 && (
        <div className={styles.col8}>
          <Tooltip tooltip={translations?.outsideTooltip} direction="down" alignment="center">
            <span>Outside</span>
          </Tooltip>
        </div>
      )}
      {value < 1 && (
        <div className={styles.col9}>
          <Tooltip tooltip={translations?.homelessTooltip} direction="down" alignment="center">
            <span>Homeless</span>
          </Tooltip>
        </div>
      )}
    </div>
  );
};

// Line component
interface WorkforceLineProps {
  levelColor?: string;
  levelName: string | null;
  levelValues: workforceInfo;
  total: number;
  useOverallTotalForUnemployment?: boolean;
  unemploymentOverride?: number;
}

const WorkforceLine: React.FC<WorkforceLineProps> = ({
  levelColor,
  levelName,
  levelValues,
  total,
  useOverallTotalForUnemployment = false,
  unemploymentOverride,
}) => {
  const value = SliderRange.value[0];
  const denominator = useOverallTotalForUnemployment ? total : levelValues.Total;

  const percent = total > 0 ? ((100 * levelValues.Total) / total).toFixed(1) + '%' : '';
  const unemploymentValue = typeof unemploymentOverride === 'number' ? unemploymentOverride : levelValues.Unemployed;

  const unemploymentMax =
    typeof unemploymentOverride === 'number'
      ? 100 // unemploymentOverride is already a percentage value
      : denominator;

  const unemployment = total > 0 ? ((100 * unemploymentValue) / unemploymentMax).toFixed(1) + '%' : '';

  return (
    <div className={styles.row_S2v}>
      <div className={styles.col1}>
        <div className={styles.colorLegend}>
          <div className={styles.symbol} style={{ backgroundColor: levelColor }} />
          <div className={styles.label}>{levelName}</div>
        </div>
      </div>
      <div className={styles.col2}>
        <span>{levelValues.Total.toLocaleString()}</span>
      </div>
      {value < 7 && (
        <div className={styles.col3}>
          <span>
            <LocalizedPercentage value={levelValues.Total} max={total} />
          </span>
        </div>
      )}
      {value < 6 && (
        <div className={styles.col4}>
          <span>{levelValues.Worker}</span>
        </div>
      )}
      {value < 5 && (
        <div className={styles.col5}>
          <span>{levelValues.Unemployed}</span>
        </div>
      )}
      {value < 4 && (
        <div className={styles.col6}>
          <span>{levelValues.UnemploymentRate.toFixed(1)}%</span>
        </div>
      )}
      {value < 3 && (
        <div className={styles.col7}>
          <span>{levelValues.Under.toLocaleString()}</span>
        </div>
      )}
      {value < 2 && (
        <div className={styles.col8}>
          <span>{levelValues.Outside.toLocaleString()}</span>
        </div>
      )}
      {value < 1 && (
        <div className={styles.col9}>
          <span>{levelValues.Homeless.toLocaleString()}</span>
        </div>
      )}
    </div>
  );
};

const Workforce: FC<DraggablePanelProps> = ({ onClose, initialPosition }) => {
  const { translate } = useLocalization();
  const workforce = useValue(WorkforceData);
  const isPanelOpen = useValue(panelVisibleBinding);
  const showExtraWorkforce = useValue(ShowExtraWorkforce);
  const unemployment = useValue(infoview.unemployment$);
  const headers: workforceInfo = {
    Total: 0,
    Worker: 0,
    Unemployed: 0,
    UnemploymentRate: 0,
    Homeless: 0,
    Under: 0,
    Outside: 0,
  };

  return (
    <Panel
      draggable={true}
      onClose={onClose}
      initialPosition={{ x: 0.71, y: 0.7 }}
      className={
        ShowExtraWorkforce.value == 1
          ? styles.panel1
          : ShowExtraWorkforce.value == 2
            ? styles.panel2
            : ShowExtraWorkforce.value == 3
              ? styles.panel3
              : ShowExtraWorkforce.value == 4
                ? styles.panel4
                : ShowExtraWorkforce.value == 5
                  ? styles.panel5
                  : ShowExtraWorkforce.value == 6
                    ? styles.panel6
                    : ShowExtraWorkforce.value == 7
                      ? styles.panel7
                      : styles.panel
      }
      header={
        <div className={styles.header}>
          <div className={styles.headerText}>
            <span className={styles.title}>{translate('InfoLoomTwo.WorkforcePanel[Title]', 'Workforce')}</span>
          </div>
        </div>
      }
    >
      {workforce.length === 0 ? (
        <p>{translate('InfoLoomTwo.WorkforcePanel[Waiting]', 'Waiting...')}</p>
      ) : (
        <div>
          <div className={styles.toggleContainer}>
            <DistrictSelector />
          </div>
          <WorkforceTableHeader
            translations={{
              totalTooltip: translate(Localekeys.WorkforcePanelTotalTooltip, 'Total Workforce'),
              percentTooltip: translate(Localekeys.WorkforcePanelPercentTooltip, 'Percentage of Total Workforce'),
              workerTooltip: translate(Localekeys.WorkforcePanelWorkerTooltip, 'Workers'),
              unemployedTooltip: translate(Localekeys.WorkforcePanelUnemployedTooltip, 'Unemployed'),
              unemploymentTooltip: translate(Localekeys.WorkforcePanelUnemploymentTooltip, 'Unemployment Rate'),
              underTooltip: translate(Localekeys.WorkforcePanelUnderTooltip, 'Under Employed'),
              outsideTooltip: translate(Localekeys.WorkforcePanelOutsideTooltip, 'Outside Workforce'),
              homelessTooltip: translate(Localekeys.WorkforcePanelHomelessTooltip, 'Homeless Workforce'),
            }}
          />
          <WorkforceLine
            levelColor="#808080"
            levelName={translate(Localekeys.Uneducated, 'Uneducated')}
            levelValues={workforce[0]}
            total={workforce[5].Total}
          />
          <WorkforceLine
            levelColor="#B09868"
            levelName={translate(Localekeys.PoorlyEducated, 'Poorly Educated')}
            levelValues={workforce[1]}
            total={workforce[5].Total}
          />
          <WorkforceLine
            levelColor="#368A2E"
            levelName={translate(Localekeys.Educated, 'Educated')}
            levelValues={workforce[2]}
            total={workforce[5].Total}
          />
          <WorkforceLine
            levelColor="#B981C0"
            levelName={translate(Localekeys.WellEducated, 'Well Educated')}
            levelValues={workforce[3]}
            total={workforce[5].Total}
          />
          <WorkforceLine
            levelColor="#5796D1"
            levelName={translate(Localekeys.HighlyEducated, 'Highly Educated')}
            levelValues={workforce[4]}
            total={workforce[5].Total}
          />
          <div className={styles.spacingSmall}></div>
          <WorkforceLine
            levelName={translate(Localekeys.Total, 'TOTAL')}
            levelValues={workforce[5]}
            total={workforce[5].Total}
          />
          <PanelFoldout
            header={
              <div className={InfoRowTheme.infoRow}>
                {translate('InfoLoomTwo.WorkforcePanel[StackedBarTitle]', 'Workforce Distribution by Education Level')}
              </div>
            }
            initialExpanded={false}
          >
            <WorkforceStackedBarChart
              workforce={workforce}
              chartTitle={translate(
                'InfoLoomTwo.WorkforcePanel[StackedBarTitle]',
                'Workforce Distribution by Education Level'
              )}
              educationLevels={[
                {
                  name: translate(Localekeys.Uneducated, 'Uneducated'),
                  color: '#808080',
                  data: workforce[0],
                },
                {
                  name: translate(Localekeys.PoorlyEducated, 'Poorly Educated'),
                  color: '#B09868',
                  data: workforce[1],
                },
                {
                  name: translate(Localekeys.Educated, 'Educated'),
                  color: '#368A2E',
                  data: workforce[2],
                },
                {
                  name: translate(Localekeys.WellEducated, 'Well Educated'),
                  color: '#B981C0',
                  data: workforce[3],
                },
                {
                  name: translate(Localekeys.HighlyEducated, 'Highly Educated'),
                  color: '#5796D1',
                  data: workforce[4],
                },
              ]}
              segmentLabels={{
                worker: translate(Localekeys.Worker, 'Worker'),
                unemployed: translate(Localekeys.Unemployed, 'Unemployed'),
                under: translate(Localekeys.WorkforcePanelUnder, 'Under Employed'),
                outside: translate(Localekeys.WorkforcePanelOutside, 'Outside'),
                homeless: translate(Localekeys.Homeless, 'Homeless'),
              }}
              totalLabel={translate(Localekeys.Total, 'TOTAL')}
            />
          </PanelFoldout>
        </div>
      )}
    </Panel>
  );
};
export default Workforce;
