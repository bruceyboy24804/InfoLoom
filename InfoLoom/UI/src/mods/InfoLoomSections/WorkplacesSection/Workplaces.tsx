import React, { FC, useState, useEffect, useRef, useMemo } from 'react';
import { bindLocalValue, useValue } from 'cs2/api';
import { WorkplacesData } from 'mods/bindings';
import { DraggablePanelProps, Panel, Tooltip, Button, PanelFoldout } from 'cs2/ui';
import styles from './Workplaces.module.scss';
import { workplacesInfo } from '../../domain/WorkplacesInfo';
import { LocalizedPercentage, useLocalization } from 'cs2/l10n';
import { InfoCheckbox } from 'mods/components/InfoCheckbox/InfoCheckbox';
import { DistrictSelector } from '../../InfoloomInfoviewContents/DistrictSelector/districtSelector';
import { SelectedInfoSectionBase, Theme } from 'cs2/bindings';
import Chart from 'chart.js/auto';
import { getModule } from 'cs2/modding';
import classNames from 'classnames';
import { InfoRowSCSS } from 'mods/InfoLoomSections/ILInfoSections/Modules/info-Row/info-Row.module.scss';
import { Localekeys } from 'mods/locale';

export const InfoRowTheme: Theme | any = getModule(
  'game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.module.scss',
  'classes'
);

export const hideColumnsBinding = bindLocalValue(false);
export const panelTrigger = (state: boolean) => {
  hideColumnsBinding.update(state);
};

interface WorkplaceLevelProps {
  levelColor?: string;
  levelName: string | null;
  levelValues: workplacesInfo;
  total: number;
  isHeader?: boolean;
  translations?: {
    total: string | null;
    percent: string | null;
    service: string | null;
    commercial: string | null;
    leisure: string | null;
    extractor: string | null;
    industrial: string | null;
    office: string | null;
    employee: string | null;
    commuter: string | null;
    open: string | null;
    filled: string | null;
    totalTooltip?: string | null;
    percentTooltip?: string | null;
    serviceTooltip?: string | null;
    commercialTooltip?: string | null;
    leisureTooltip?: string | null;
    extractorTooltip?: string | null;
    industrialTooltip?: string | null;
    officeTooltip?: string | null;
    employeeTooltip?: string | null;
    commuterTooltip?: string | null;
    openTooltip?: string | null;
    filledTooltip?: string | null;
  };
  people?: Array<{ id: number; isEmployee: boolean; isCommuter: boolean }>;
}

interface WorkplaceStackedBarChartProps {
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

const WorkplaceStackedBarChart: React.FC<WorkplaceStackedBarChartProps> = ({
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

  const allLevels = useMemo(
    () => [...educationLevels.map(l => l.data), workplaces[5]],
    [educationLevels, workplaces]
  );

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
              footer: (tooltipItems) => {
                let total = 0;
                tooltipItems.forEach(item => {
                  total += (Number(item.raw) || 0);
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

  // Update chart data when workplaces or chartType changes
  useEffect(() => {
    if (!chartRef.current) return;

    chartRef.current.data = chartData;

    if (chartRef.current.options.plugins?.title) {
      (chartRef.current.options.plugins.title as any).text = chartTitle ?? '';
    }

    chartRef.current.update('none');
  }, [chartData, chartTitle]);

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

const WorkplaceTableHeader: React.FC<{ translations: any }> = ({ translations }) => {
  const hideColumns = useValue(hideColumnsBinding);
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
      <div className={styles.col3}>
        <Tooltip tooltip={translations?.percentTooltip} direction="down" alignment="center">
          <span>%</span>
        </Tooltip>
      </div>
      <div className={styles.col4}>
        <Tooltip tooltip={translations?.workerTooltip} direction="down" alignment="center">
          <span>Employee</span>
        </Tooltip>
      </div>
      <div className={styles.col5}>
        <Tooltip tooltip={translations?.unemployedTooltip} direction="down" alignment="center">
          <span>Commuter</span>
        </Tooltip>
      </div>
      <div className={styles.col6}>
        <Tooltip tooltip={translations?.unemploymentTooltip} direction="down" alignment="center">
          <span>Open</span>
        </Tooltip>
      </div>
      <div className={styles.col7}>
        <Tooltip tooltip={translations?.outsideTooltip} direction="down" alignment="center">
          <span>Filled</span>
        </Tooltip>
      </div>
      {!hideColumns && (
        <>
          <div className={styles.col8}>
            <Tooltip tooltip={translations?.homelessTooltip} direction="down" alignment="center">
              <span>Service</span>
            </Tooltip>
          </div>
          <div className={styles.col9}>
            <Tooltip tooltip={translations?.homelessTooltip} direction="down" alignment="center">
              <span>Commercial</span>
            </Tooltip>
          </div>
          <div className={styles.col10}>
            <Tooltip tooltip={translations?.homelessTooltip} direction="down" alignment="center">
              <span>Leisure</span>
            </Tooltip>
          </div>
          <div className={styles.col11}>
            <Tooltip tooltip={translations?.homelessTooltip} direction="down" alignment="center">
              <span>Extractor</span>
            </Tooltip>
          </div>
          <div className={styles.col12}>
            <Tooltip tooltip={translations?.homelessTooltip} direction="down" alignment="center">
              <span>Industrial</span>
            </Tooltip>
          </div>
          <div className={styles.col13}>
            <Tooltip tooltip={translations?.homelessTooltip} direction="down" alignment="center">
              <span>Office</span>
            </Tooltip>
          </div>
        </>
      )}
    </div>
  );
};

const WorkplaceLevel: React.FC<WorkplaceLevelProps> = ({ levelColor, levelName, levelValues, total, people }) => {
  const hideColumns = useValue(hideColumnsBinding);

  // Count unique employees and commuters
  const counted = new Set<number>();
  let employeeCount = 0;
  let commuterCount = 0;

  if (people) {
    for (const person of people) {
      if (!counted.has(person.id)) {
        if (person.isEmployee) {
          employeeCount++;
        } else if (person.isCommuter) {
          commuterCount++;
        }
        counted.add(person.id);
      }
    }
  } else {
    employeeCount = levelValues.Employee;
    commuterCount = levelValues.Commuter;
  }

  const filledCount = employeeCount + commuterCount;
  const percent = total > 0 ? (100 * levelValues.Total) / total + '%' : '';
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
      <div className={styles.col3}>
        <span>
          <LocalizedPercentage value={levelValues.Total} max={total} />
        </span>
      </div>
      <div className={styles.col4}>
        <span>{levelValues.Employee}</span>
      </div>
      <div className={styles.col5}>
        <span>{levelValues.Commuter}</span>
      </div>
      <div className={styles.col6}>
        <span>{levelValues.Open}</span>
      </div>
      <div className={styles.col7}>
        <span>
          <LocalizedPercentage value={Math.min(filledCount, levelValues.Total)} max={levelValues.Total} />
        </span>
      </div>
      {!hideColumns && (
        <>
          <div className={styles.col8}>
            <span>{levelValues.Service}</span>
          </div>
          <div className={styles.col9}>
            <span>{levelValues.Commercial}</span>
          </div>
          <div className={styles.col10}>
            <span>{levelValues.Leisure}</span>
          </div>
          <div className={styles.col11}>
            <span>{levelValues.Extractor}</span>
          </div>
          <div className={styles.col12}>
            <span>{levelValues.Industrial}</span>
          </div>
          <div className={styles.col13}>
            <span>{levelValues.Office}</span>
          </div>
        </>
      )}
    </div>
  );
};

const HideColumnsToggle: FC = () => {
  const hideColumns = useValue(hideColumnsBinding);

  return (
    <InfoCheckbox
      label="Hide Columns"
      isChecked={hideColumns}
      onToggle={newVal => {
        hideColumnsBinding.update(newVal);
      }}
      className={styles.hideColumnsToggle}
    />
  );
};

const Workplaces: FC<DraggablePanelProps> = ({ onClose, initialPosition }) => {
  const { translate } = useLocalization();
  const [chartType, setChartType] = useState<'sector' | 'employment'>('sector');
  const workplaces = useValue(WorkplacesData);
  const hideColumns = useValue(hideColumnsBinding);
  return (
    <Panel
      draggable={true}
      onClose={onClose}
      initialPosition={{ x: 0.038, y: 0.15 }}
      className={hideColumns ? styles.compactPanel : styles.panel}
      header={
        <div className={styles.header}>
          <span className={styles.headerText}>{translate('InfoLoomTwo.WorkplacesPanel[Title]', 'Workplaces')}</span>
        </div>
      }
    >
      {workplaces.length === 0 ? (
        <p>Waiting...</p>
      ) : (
        <div>
          <div className={styles.toggleContainer}>
            <DistrictSelector />
            <HideColumnsToggle />
          </div>
          <WorkplaceTableHeader
            translations={{
              totalTooltip: translate(
                'InfoLoomTwo.WorkplacesPanel[TotalTooltip]',
                'Total workplaces at this education level'
              ),
              percentTooltip: translate(
                'InfoLoomTwo.WorkplacesPanel[PercentTooltip]',
                'Percentage of total workplaces'
              ),
              workerTooltip: translate(
                'InfoLoomTwo.WorkplacesPanel[WorkerTooltip]',
                'Workplaces filled by city residents'
              ),
              unemployedTooltip: translate(
                'InfoLoomTwo.WorkplacesPanel[UnemployedTooltip]',
                'Workplaces filled by commuters from outside the city'
              ),
              unemploymentTooltip: translate(
                'InfoLoomTwo.WorkplacesPanel[UnemploymentTooltip]',
                'Unfilled workplace positions'
              ),
              outsideTooltip: translate(
                'InfoLoomTwo.WorkplacesPanel[OutsideTooltip]',
                'Percentage of workplace positions that are filled'
              ),
              homelessTooltip: translate(
                'InfoLoomTwo.WorkplacesPanel[HomelessTooltip]',
                'Sector breakdown of workplaces'
              ),
            }}
          />
          <WorkplaceLevel
            levelColor="#808080"
            levelName={translate(Localekeys.Uneducated, 'Uneducated')}
            levelValues={workplaces[0]}
            total={workplaces[5].Total}
          />
          <WorkplaceLevel
            levelColor="#B09868"
            levelName={translate(Localekeys.PoorlyEducated, 'Poorly Educated')}
            levelValues={workplaces[1]}
            total={workplaces[5].Total}
          />
          <WorkplaceLevel
            levelColor="#368A2E"
            levelName={translate(Localekeys.Educated, 'Educated')}
            levelValues={workplaces[2]}
            total={workplaces[5].Total}
          />
          <WorkplaceLevel
            levelColor="#B981C0"
            levelName={translate(Localekeys.WellEducated, 'Well Educated')}
            levelValues={workplaces[3]}
            total={workplaces[5].Total}
          />
          <WorkplaceLevel
            levelColor="#5796D1"
            levelName={translate(Localekeys.HighlyEducated, 'Highly Educated')}
            levelValues={workplaces[4]}
            total={workplaces[5].Total}
          />
          <div className={styles.spacingSmall}></div>
          <WorkplaceLevel
            levelName={translate(Localekeys.Total, 'TOTAL')}
            levelValues={workplaces[5]}
            total={workplaces[5].Total}
          />
          <PanelFoldout
            header={
              <div className={InfoRowTheme.infoRow}>
                {chartType === 'employment'
                  ? translate('InfoLoomTwo.WorkplacesPanel[ChartTitleEmployment]', 'Employment Status by Education Level')
                  : translate('InfoLoomTwo.WorkplacesPanel[ChartTitleSector]', 'Workplace Distribution by Sector')}
              </div>
            }
            initialExpanded={false}
          >
            <div className={styles.chartToggle}>
              <button
                className={`${styles.toggleButton} ${chartType === 'sector' ? styles.buttonSelected : ''}`}
                onClick={() => setChartType('sector')}
              >
                {translate('InfoLoomTwo.WorkplacesPanel[ToggleButton1]', 'By Sector')}
              </button>
              <button
                className={`${styles.toggleButton} ${chartType === 'employment' ? styles.buttonSelected : ''}`}
                onClick={() => setChartType('employment')}
              >
                {translate('InfoLoomTwo.WorkplacesPanel[ToggleButton2]', 'By Employment')}
              </button>
            </div>
            <WorkplaceStackedBarChart
              workplaces={workplaces}
              chartType={chartType}
              chartTitle={
                chartType === 'employment'
                  ? translate('InfoLoomTwo.WorkplacesPanel[ChartTitleEmployment]', 'Employment Status by Education Level')
                  : translate('InfoLoomTwo.WorkplacesPanel[ChartTitleSector]', 'Workplace Distribution by Sector')
              }
              educationLevels={[
                {
                  name: translate(Localekeys.Uneducated, 'Uneducated'),
                  color: '#808080',
                  data: workplaces[0],
                },
                {
                  name: translate(Localekeys.PoorlyEducated, 'Poorly Educated'),
                  color: '#B09868',
                  data: workplaces[1],
                },
                {
                  name: translate(Localekeys.Educated, 'Educated'),
                  color: '#368A2E',
                  data: workplaces[2],
                },
                {
                  name: translate(Localekeys.WellEducated, 'Well Educated'),
                  color: '#B981C0',
                  data: workplaces[3],
                },
                {
                  name: translate(Localekeys.HighlyEducated, 'Highly Educated'),
                  color: '#5796D1',
                  data: workplaces[4],
                },
              ]}
              sectorLabels={{
                service: translate(Localekeys.Services, 'Service'),
                commercial: translate(Localekeys.Commercial, 'Commercial'),
                leisure: translate(Localekeys.Leisure, 'Leisure'),
                extractor: translate(Localekeys.Extractors, 'Extractor'),
                industrial: translate(Localekeys.Industrial, 'Industrial'),
                office: translate(Localekeys.Office, 'Office'),
              }}
              employmentLabels={{
                employee: translate(Localekeys.Employees, 'Employee'),
                commuter: translate(Localekeys.Commuter, 'Commuter'),
                open: translate(Localekeys.Open, 'Open'),
              }}
              totalLabel={translate(Localekeys.Total, 'TOTAL')}
            />
          </PanelFoldout>
        </div>
      )}
    </Panel>
  );
};

export default Workplaces;
