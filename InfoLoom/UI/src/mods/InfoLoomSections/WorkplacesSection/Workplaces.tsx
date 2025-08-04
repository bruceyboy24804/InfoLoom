import React, { FC, useState } from 'react';
import { bindLocalValue, useValue } from 'cs2/api';
import { WorkplacesData } from 'mods/bindings';
import { DraggablePanelProps, Panel, Tooltip, Button, PanelFoldout } from 'cs2/ui';
import styles from './Workplaces.module.scss';
import { workplacesInfo } from '../../domain/WorkplacesInfo';
import { useLocalization } from 'cs2/l10n';
import { InfoCheckbox } from 'mods/components/InfoCheckbox/InfoCheckbox';
import {DistrictSelector} from "../../InfoloomInfoviewContents/DistrictSelector/districtSelector";


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

interface StackedBarProps {
  levelName: string | null;
  levelColor: string;
  levelValues: workplacesInfo;
  total: number;
  barType?: 'sector' | 'employment';
  translations: {
    segments: Array<{ label: string }>;
    segmentTooltipCount: string | null;
    segmentTooltipWithin: string | null;
    segmentTooltipOfTotal: string | null;
    barTooltipHeader: string | null;
    barTooltipTotal: string | null;
    barTooltipPercentage: string | null;
  };
}
const WorkplaceTableHeader: React.FC<{ translations: any; }> = ({ translations }) => {
  const hideColumns = useValue(hideColumnsBinding);
  return (  
      <div className={styles.headerRow}>
        <div className={styles.col1}>
          <Tooltip tooltip={translations?.totalTooltip} direction="down" alignment="center">
            <span>Education</span>
          </Tooltip>
        </div>
        <div className={styles.col2}>
          <Tooltip tooltip={translations?.percentTooltip} direction="down" alignment="center">
            <span>Total</span>
          </Tooltip>
        </div>
          <div className={styles.col3}>
            <Tooltip tooltip={translations?.workerTooltip} direction="down" alignment="center">
              <span>%</span>
            </Tooltip>
          </div>
          <div className={styles.col4}>
            <Tooltip tooltip={translations?.unemployedTooltip} direction="down" alignment="center">
              <span>Employee</span>
            </Tooltip>
          </div>
          <div className={styles.col5}>
            <Tooltip tooltip={translations?.unemploymentTooltip} direction="down" alignment="center">
              <span>Commuter</span>
            </Tooltip>
          </div>
          <div className={styles.col6}>
            <span>Open</span>
          </div>
          <div className={styles.col7}>
            <Tooltip tooltip={translations?.outsideTooltip} direction="down" alignment="center">
              <span>Filled</span>
            </Tooltip>
          </div>
          { !hideColumns  && (
            <>
              <div className={styles.col8}>
                <Tooltip tooltip={translations?.outsideTooltip} direction="down" alignment="center">
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
}  





const WorkplaceLevel: React.FC<WorkplaceLevelProps> = ({
  levelColor,
  levelName,
  levelValues,
  total,
  people,  
}) => {
  const hideColumns = useValue(hideColumnsBinding);
  const percent = total > 0 ? ((100 * levelValues.Total) / total).toFixed(1) + '%' : '';
  
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

  const filledRate =
    levelValues.Total > 0
      ? Math.min(100, (100 * (employeeCount + commuterCount)) / levelValues.Total).toFixed(1) + '%'
      : '';
  return (
   <div className={styles.row_S2v}>
      <div className={styles.col1}>
          <div className={styles.colorLegend}>
              <div className={styles.symbol} style={{ backgroundColor: levelColor }} />
              <div className={styles.label}>{levelName}</div>
          </div>
      </div>
      <div className={styles.col2}><span>{levelValues.Total.toLocaleString()}</span></div>
      <div className={styles.col3}><span>{percent}</span></div>
      <div className={styles.col4}><span>{levelValues.Employee}</span></div>
      <div className={styles.col5}><span>{levelValues.Commuter}</span></div>
      <div className={styles.col6}><span>{levelValues.Open}</span></div>
      <div className={styles.col7}><span>{filledRate}</span></div>
      {!hideColumns && (
        <>
          <div className={styles.col8}><span>{levelValues.Service}</span></div>
          <div className={styles.col9}><span>{levelValues.Commercial}</span></div>
          <div className={styles.col10}><span>{levelValues.Leisure}</span></div>
          <div className={styles.col11}><span>{levelValues.Extractor}</span></div>
          <div className={styles.col12}><span>{levelValues.Industrial}</span></div>
          <div className={styles.col13}><span>{levelValues.Office}</span></div>
        </>
      )}
    </div>
  );
};

const StackedBar: React.FC<StackedBarProps> = ({
  levelName,
  levelColor,
  levelValues,
  total,
  barType = 'sector',
  translations,
}) => {
  if (total === 0 || levelValues.Total === 0) return null;

  const sectorSegments = [
    { label: translations.segments[0]?.label || 'Service', value: levelValues.Service, color: '#4287f5' },
    { label: translations.segments[1]?.label || 'Commercial', value: levelValues.Commercial, color: '#f5d142' },
    { label: translations.segments[2]?.label || 'Leisure', value: levelValues.Leisure, color: '#f542a7' },
    { label: translations.segments[3]?.label || 'Extractor', value: levelValues.Extractor, color: '#8c42f5' },
    { label: translations.segments[4]?.label || 'Industrial', value: levelValues.Industrial, color: '#f55142' },
    { label: translations.segments[5]?.label || 'Office', value: levelValues.Office, color: '#42f5b3' },
  ];

  const employmentSegments = [
    { label: translations.segments[0]?.label || 'Employee', value: levelValues.Employee, color: '#4CAF50' },
    { label: translations.segments[1]?.label || 'Commuter', value: levelValues.Commuter, color: '#FF9800' },
    { label: translations.segments[2]?.label || 'Open', value: levelValues.Open, color: '#F44336' },
  ];

  const segments = barType === 'employment' ? employmentSegments : sectorSegments;
  const totalSegmentValue = segments.reduce((sum, segment) => sum + segment.value, 0);

  const createTooltipContent = (segment: (typeof segments)[0]) => {
    const percentage = totalSegmentValue > 0 ? (segment.value / totalSegmentValue) * 100 : 0;
    const totalPercentage = total > 0 ? (segment.value / total) * 100 : 0;

    return (
      <div className={styles.tooltipContent}>
        <div className={styles.tooltipHeader}>
          {levelName} - {segment.label}
        </div>
        <div className={styles.tooltipRow}>
          <span>{translations.segmentTooltipCount || 'Count'}:</span>
          <span>{segment.value.toLocaleString()}</span>
        </div>
        <div className={styles.tooltipRow}>
          <span>{(translations.segmentTooltipWithin || 'Within ').replace('{0}', levelName || '')}:</span>
          <span>{percentage.toFixed(1)}%</span>
        </div>
        <div className={styles.tooltipRow}>
          <span>{translations.segmentTooltipOfTotal || 'Of Total Workplaces'}:</span>
          <span>{totalPercentage.toFixed(1)}%</span>
        </div>
      </div>
    );
  };

  const barTooltipContent = (
    <div className={styles.tooltipContent}>
      <div className={styles.tooltipHeader}>{(translations.barTooltipHeader || 'Summary').replace('{0}', levelName || '')}</div>
      <div className={styles.tooltipRow}>
        <span>{translations.barTooltipTotal || 'Total'}:</span>
        <span>{levelValues.Total.toLocaleString()}</span>
      </div>
      <div className={styles.tooltipRow}>
        <span>{translations.barTooltipPercentage || '% of Total Workplaces'}:</span>
        <span>{total > 0 ? ((levelValues.Total / total) * 100).toFixed(1) : '0'}%</span>
      </div>
      <div className={styles.tooltipDivider}></div>
      {segments.map((segment, index) => {
        if (segment.value === 0) return null;
        const percentage = totalSegmentValue > 0 ? (segment.value / totalSegmentValue) * 100 : 0;
        return (
          <div key={index} className={styles.tooltipRow}>
            <span style={{ color: segment.color }}>• {segment.label}:</span>
            <span>
              {segment.value.toLocaleString()} ({percentage.toFixed(1)}%)
            </span>
          </div>
        );
      })}
    </div>
  );

  return (
    <PanelFoldout
      header={
        <div className={styles.barLabel}>
          <div className={styles.barLabelSymbol} style={{ backgroundColor: levelColor }}></div>
          <div className={styles.barLabelText}>{levelName}</div>
          <div className={styles.barLabelValue}>{levelValues.Total.toLocaleString()}</div>
        </div>
      }
      initialExpanded={false}
      className={styles.stackedBarContainer}
    >
      <Tooltip tooltip={barTooltipContent} direction="down" alignment="center">
        <div className={styles.stackedBar}>
          {segments.map((segment, index) => {
            const percentage =
              totalSegmentValue > 0 ? (segment.value / totalSegmentValue) * 100 : 0;
            return percentage > 0 ? (
              <Tooltip
                key={index}
                tooltip={createTooltipContent(segment)}
                direction="up"
                alignment="center"
              >
                <div
                  className={styles.barSegment}
                  style={{
                    width: `${percentage}%`,
                    backgroundColor: segment.color,
                  }}
                />
              </Tooltip>
            ) : null;
          })}
        </div>
      </Tooltip>
    </PanelFoldout>
  );
};

interface WorkplaceChartProps {
  workplaces: workplacesInfo[];
  chartType: 'sector' | 'employment';
  onChartTypeChange: (type: 'sector' | 'employment') => void;
  chartTitle: string | null;
  toggleButton1: string | null;
  toggleButton2: string | null;
  legendItems: Array<{ label: string | null; color: string }>;
  legendTooltipSuffix: string | null;
  educationLevels: Array<{ name: string | null; color: string; data: workplacesInfo }>;
  translations: any;
  totalLabel: string | null;
}

const WorkplaceChart: React.FC<WorkplaceChartProps> = ({
  workplaces,
  chartType,
  onChartTypeChange,
  chartTitle,
  toggleButton1,
  toggleButton2,
  legendItems,
  legendTooltipSuffix,
  educationLevels,
  translations,
  totalLabel,
}) => {
  return (
    <PanelFoldout
      header={<div className={styles.chartTitle}>{chartTitle}</div>}
      initialExpanded={false}
      className={styles.chartSection}
    >
      <div className={styles.chartToggle}>
        <button
          className={`${styles.toggleButton} ${chartType === 'sector' ? styles.buttonSelected : ''}`}
          onClick={() => onChartTypeChange('sector')}
        >
          {toggleButton1}
        </button>
        <button
          className={`${styles.toggleButton} ${chartType === 'employment' ? styles.buttonSelected : ''}`}
          onClick={() => onChartTypeChange('employment')}
        >
          {toggleButton2}
        </button>
      </div>
      <div className={styles.legendContainer}>
        {legendItems.map((item, index) => (
          <Tooltip
            key={index}
            tooltip={`${item.label}: ${legendTooltipSuffix}`}
            direction="up"
            alignment="center"
          >
            <div className={styles.legendItem}>
              <div className={styles.legendSymbol} style={{ backgroundColor: item.color }}></div>
              <span>{item.label}</span>
            </div>
          </Tooltip>
        ))}
      </div>
      {educationLevels.map((level, index) => (
        <StackedBar
          key={index}
          levelName={level.name}
          levelColor={level.color}
          levelValues={level.data}
          total={workplaces[5].Total}
          barType={chartType}
          translations={translations}
        />
      ))}

      <StackedBar
        levelName={totalLabel}
        levelColor="#FFFFFF"
        levelValues={workplaces[5]}
        total={workplaces[5].Total}
        barType={chartType}
        translations={translations}
      />
    </PanelFoldout>
  );
};
const HideColumnsToggle: FC = () => {
  const hideColumns = useValue(hideColumnsBinding);

  return (
    <InfoCheckbox
      label="Hide Columns"
      isChecked={hideColumns}
      onToggle={(newVal) => {
        hideColumnsBinding.update(newVal);
      }}
      className={styles.hideColumnsToggle}
    />
  );
}









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
          <span className={styles.headerText}>{translate("InfoLoomTwo.WorkplacesPanel[Title]", "Workplaces")}</span>
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
          <WorkplaceTableHeader translations={{
            totalTooltip: translate("InfoLoomTwo.WorkplacesPanel[TotalTooltip]", "Total workplaces at this education level"),
            percentTooltip: translate("InfoLoomTwo.WorkplacesPanel[PercentTooltip]", "Percentage of total workplaces"),
            workerTooltip: translate("InfoLoomTwo.WorkplacesPanel[WorkerTooltip]", "Workplaces filled by city residents"),
            unemployedTooltip: translate("InfoLoomTwo.WorkplacesPanel[UnemployedTooltip]", "Workplaces filled by commuters from outside the city"),
            unemploymentTooltip: translate("InfoLoomTwo.WorkplacesPanel[UnemploymentTooltip]", "Unfilled workplace positions"),
            outsideTooltip: translate("InfoLoomTwo.WorkplacesPanel[OutsideTooltip]", "Percentage of workplace positions that are filled"),
            homelessTooltip: translate("InfoLoomTwo.WorkplacesPanel[HomelessTooltip]", "Sector breakdown of workplaces"),
          }} /> 
          <WorkplaceLevel
            levelColor="#808080"
            levelName={translate("InfoLoomTwo.WorkplacesPanel[Row1EL]", "Uneducated")}
            levelValues={workplaces[0]}
            total={workplaces[5].Total}
          />
          <WorkplaceLevel
            levelColor="#B09868"
            levelName={translate("InfoLoomTwo.WorkplacesPanel[Row2EL]", "Poorly Educated")}
            levelValues={workplaces[1]}
            total={workplaces[5].Total}
          />
          <WorkplaceLevel
            levelColor="#368A2E"
            levelName={translate("InfoLoomTwo.WorkplacesPanel[Row3EL]", "Educated")}
            levelValues={workplaces[2]}
            total={workplaces[5].Total}
          />
          <WorkplaceLevel
            levelColor="#B981C0"
            levelName={translate("InfoLoomTwo.WorkplacesPanel[Row4EL]", "Well Educated")}
            levelValues={workplaces[3]}
            total={workplaces[5].Total}
          />
          <WorkplaceLevel
            levelColor="#5796D1"
            levelName={translate("InfoLoomTwo.WorkplacesPanel[Row5EL]", "Highly Educated")}
            levelValues={workplaces[4]}
            total={workplaces[5].Total}
          />
          <div className={styles.spacingSmall}></div>
          <WorkplaceLevel levelName={translate("InfoLoomTwo.WorkplacesPanel[Row6]", "TOTAL")} levelValues={workplaces[5]} total={0} />

          <WorkplaceChart
            workplaces={workplaces}
            chartType={chartType}
            onChartTypeChange={setChartType}
            chartTitle={chartType === 'employment' 
              ? translate("InfoLoomTwo.WorkplacesPanel[ChartTitleEmployment]", "Employment Status by Education Level")
              : translate("InfoLoomTwo.WorkplacesPanel[ChartTitleSector]", "Workplace Distribution by Sector")
            }
            toggleButton1={translate("InfoLoomTwo.WorkplacesPanel[ToggleButton1]", "By Sector")}
            toggleButton2={translate("InfoLoomTwo.WorkplacesPanel[ToggleButton2]", "By Employment")}
            legendItems={chartType === 'employment' ? [
              { label: translate("InfoLoomTwo.WorkplacesPanel[EmploymentLegendItem1]", "Employee"), color: '#4CAF50' },
              { label: translate("InfoLoomTwo.WorkplacesPanel[EmploymentLegendItem2]", "Commuter"), color: '#FF9800' },
              { label: translate("InfoLoomTwo.WorkplacesPanel[EmploymentLegendItem3]", "Open"), color: '#F44336' },
            ] : [
              { label: translate("InfoLoomTwo.WorkplacesPanel[SectorLegendItem1]", "Service"), color: '#4287f5' },
              { label: translate("InfoLoomTwo.WorkplacesPanel[SectorLegendItem2]", "Commercial"), color: '#f5d142' },
              { label: translate("InfoLoomTwo.WorkplacesPanel[SectorLegendItem3]", "Leisure"), color: '#f542a7' },
              { label: translate("InfoLoomTwo.WorkplacesPanel[SectorLegendItem4]", "Extractor"), color: '#8c42f5' },
              { label: translate("InfoLoomTwo.WorkplacesPanel[SectorLegendItem5]", "Industrial"), color: '#f55142' },
              { label: translate("InfoLoomTwo.WorkplacesPanel[SectorLegendItem6]", "Office"), color: '#42f5b3' },
            ]}
            legendTooltipSuffix={translate("InfoLoomTwo.WorkplacesPanel[LegendTooltipSuffix]", "Click on bar segments above to see detailed breakdown")}
            educationLevels={[
              { name: translate("InfoLoomTwo.WorkplacesPanel[Row1EL]", "Uneducated"), color: '#808080', data: workplaces[0] },
              { name: translate("InfoLoomTwo.WorkplacesPanel[Row2EL]", "Poorly Educated"), color: '#B09868', data: workplaces[1] },
              { name: translate("InfoLoomTwo.WorkplacesPanel[Row3EL]", "Educated"), color: '#368A2E', data: workplaces[2] },
              { name: translate("InfoLoomTwo.WorkplacesPanel[Row4EL]", "Well Educated"), color: '#B981C0', data: workplaces[3] },
              { name: translate("InfoLoomTwo.WorkplacesPanel[Row5EL]", "Highly Educated"), color: '#5796D1', data: workplaces[4] },
            ]}
            translations={{
              segments: chartType === 'employment' ? [
                { label: translate("InfoLoomTwo.WorkplacesPanel[EmploymentLegendItem1]", "Employee") },
                { label: translate("InfoLoomTwo.WorkplacesPanel[EmploymentLegendItem2]", "Commuter") },
                { label: translate("InfoLoomTwo.WorkplacesPanel[EmploymentLegendItem3]", "Open") },
              ] : [
                { label: translate("InfoLoomTwo.WorkplacesPanel[SectorLegendItem1]", "Service") },
                { label: translate("InfoLoomTwo.WorkplacesPanel[SectorLegendItem2]", "Commercial") },
                { label: translate("InfoLoomTwo.WorkplacesPanel[SectorLegendItem3]", "Leisure") },
                { label: translate("InfoLoomTwo.WorkplacesPanel[SectorLegendItem4]", "Extractor") },
                { label: translate("InfoLoomTwo.WorkplacesPanel[SectorLegendItem5]", "Industrial") },
                { label: translate("InfoLoomTwo.WorkplacesPanel[SectorLegendItem6]", "Office") },
              ],
              segmentTooltipCount: translate("InfoLoomTwo.WorkplacesPanel[SegmentTooltipCount]", "Count"),
              segmentTooltipWithin: translate("InfoLoomTwo.WorkplacesPanel[SegmentTooltipWithin]", "Within "),
              segmentTooltipOfTotal: translate("InfoLoomTwo.WorkplacesPanel[SegmentTooltipOfTotal]", "Of Total Workplaces"),
              barTooltipHeader: translate("InfoLoomTwo.WorkplacesPanel[BarTooltipHeader]", "Summary"),
              barTooltipTotal: translate("InfoLoomTwo.WorkplacesPanel[BarTooltipTotal]", "Total"),
              barTooltipPercentage: translate("InfoLoomTwo.WorkplacesPanel[BarTooltipPercentage]", "% of Total Workplaces"),
            }}
            totalLabel={translate("InfoLoomTwo.WorkplacesPanel[Row6]", "TOTAL")}
          />
        </div>
      )}
    </Panel>
  );
};

export default Workplaces;
