import React, { FC, useState } from 'react';
import { useValue } from 'cs2/api';
import { WorkplacesData } from 'mods/bindings';
import { DraggablePanelProps, Panel, Tooltip, Button, PanelFoldout } from 'cs2/ui';
import styles from './Workplaces.module.scss';
import { workplacesInfo } from '../../domain/WorkplacesInfo';
import { useLocalization } from 'cs2/l10n';

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

const WorkplaceLevel: React.FC<WorkplaceLevelProps> = ({
  levelColor,
  levelName,
  levelValues,
  total,
  isHeader = false,
  translations,
}) => {
  const percent = total > 0 ? ((100 * levelValues.Total) / total).toFixed(1) + '%' : '';
  const filledRate =
    levelValues.Total > 0
      ? ((100 * (levelValues.Employee + levelValues.Commuter)) / levelValues.Total).toFixed(1) + '%'
      : '';

  return (
    <div className={`labels_L7Q row_S2v ${styles.workplaceLevel}`}>
      <div className={styles.spacer1}></div>
      <div className={styles.levelNameContainer}>
        {levelColor && (
          <div
            className={`symbol_aAH ${styles.levelSymbol}`}
            style={{ backgroundColor: levelColor }}
          ></div>
        )}
        <div>{levelName}</div>
      </div>
      <div className={`row_S2v ${styles.dataColumn}`}>
        {isHeader ? (
          translations?.totalTooltip ? (
            <Tooltip tooltip={translations.totalTooltip} direction="down" alignment="center">
              <span>{translations.total}</span>
            </Tooltip>
          ) : (
            translations?.total
          )
        ) : (
          levelValues.Total.toLocaleString()
        )}
      </div>
      <div className={`row_S2v ${styles.percentColumn}`}>
        {isHeader ? (
          translations?.percentTooltip ? (
            <Tooltip tooltip={translations.percentTooltip} direction="down" alignment="center">
              <span>{translations.percent}</span>
            </Tooltip>
          ) : (
            translations?.percent
          )
        ) : (
          percent
        )}
      </div>
      <div className={`row_S2v ${styles.dataColumn}`}>
        {isHeader ? (
          translations?.serviceTooltip ? (
            <Tooltip tooltip={translations.serviceTooltip} direction="down" alignment="center">
              <span>{translations.service}</span>
            </Tooltip>
          ) : (
            translations?.service
          )
        ) : (
          levelValues.Service.toLocaleString()
        )}
      </div>
      <div className={`row_S2v ${styles.dataColumn}`}>
        {isHeader ? (
          translations?.commercialTooltip ? (
            <Tooltip tooltip={translations.commercialTooltip} direction="down" alignment="center">
              <span>{translations.commercial}</span>
            </Tooltip>
          ) : (
            translations?.commercial
          )
        ) : (
          levelValues.Commercial.toLocaleString()
        )}
      </div>
      <div className={`row_S2v ${styles.dataColumn}`}>
        {isHeader ? (
          translations?.leisureTooltip ? (
            <Tooltip tooltip={translations.leisureTooltip} direction="down" alignment="center">
              <span>{translations.leisure}</span>
            </Tooltip>
          ) : (
            translations?.leisure
          )
        ) : (
          levelValues.Leisure.toLocaleString()
        )}
      </div>
      <div className={`row_S2v ${styles.dataColumn}`}>
        {isHeader ? (
          translations?.extractorTooltip ? (
            <Tooltip tooltip={translations.extractorTooltip} direction="down" alignment="center">
              <span>{translations.extractor}</span>
            </Tooltip>
          ) : (
            translations?.extractor
          )
        ) : (
          levelValues.Extractor.toLocaleString()
        )}
      </div>
      <div className={`row_S2v ${styles.dataColumn}`}>
        {isHeader ? (
          translations?.industrialTooltip ? (
            <Tooltip tooltip={translations.industrialTooltip} direction="down" alignment="center">
              <span>{translations.industrial}</span>
            </Tooltip>
          ) : (
            translations?.industrial
          )
        ) : (
          levelValues.Industrial.toLocaleString()
        )}
      </div>
      <div className={`row_S2v ${styles.dataColumn}`}>
        {isHeader ? (
          translations?.officeTooltip ? (
            <Tooltip tooltip={translations.officeTooltip} direction="down" alignment="center">
              <span>{translations.office}</span>
            </Tooltip>
          ) : (
            translations?.office
          )
        ) : (
          levelValues.Office.toLocaleString()
        )}
      </div>
      <div className={`row_S2v ${styles.employeeColumn}`}>
        {isHeader ? (
          translations?.employeeTooltip ? (
            <Tooltip tooltip={translations.employeeTooltip} direction="down" alignment="center">
              <span>{translations.employee}</span>
            </Tooltip>
          ) : (
            translations?.employee
          )
        ) : (
          levelValues.Employee.toLocaleString()
        )}
      </div>
      <div className={`row_S2v ${styles.commuterColumn}`}>
        {isHeader ? (
          translations?.commuterTooltip ? (
            <Tooltip tooltip={translations.commuterTooltip} direction="down" alignment="center">
              <span>{translations.commuter}</span>
            </Tooltip>
          ) : (
            translations?.commuter
          )
        ) : (
          levelValues.Commuter.toLocaleString()
        )}
      </div>
      <div className={`row_S2v ${styles.openColumn}`}>
        {isHeader ? (
          translations?.openTooltip ? (
            <Tooltip tooltip={translations.openTooltip} direction="down" alignment="center">
              <span>{translations.open}</span>
            </Tooltip>
          ) : (
            translations?.open
          )
        ) : (
          levelValues.Open.toLocaleString()
        )}
      </div>
      <div className={`row_S2v small_ExK ${styles.percentColumn}`}>
        {isHeader ? (
          translations?.filledTooltip ? (
            <Tooltip tooltip={translations.filledTooltip} direction="down" alignment="center">
              <span>{translations.filled}</span>
            </Tooltip>
          ) : (
            translations?.filled
          )
        ) : (
          filledRate
        )}
      </div>
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
          <span>{(translations.segmentTooltipWithin || 'Within {0}').replace('{0}', levelName || '')}:</span>
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
      <div className={styles.tooltipHeader}>{(translations.barTooltipHeader || '{0} Summary').replace('{0}', levelName || '')}</div>
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

const Workplaces: FC<DraggablePanelProps> = ({ onClose, initialPosition }) => {
  const { translate } = useLocalization();
  const [chartType, setChartType] = useState<'sector' | 'employment'>('sector');
  const workplaces = useValue(WorkplacesData);
  const headers: workplacesInfo = {
    Total: 0,
    Service: 0,
    Commercial: 0,
    Leisure: 0,
    Extractor: 0,
    Industrial: 0,
    Office: 0,
    Employee: 0,
    Commuter: 0,
    Open: 0,
    Name: '',
  };

  return (
    <Panel
      draggable={true}
      onClose={onClose}
      initialPosition={{ x: 0.038, y: 0.15 }}
      className={styles.panel}
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
          <div className={styles.spacingTop}></div>
          <WorkplaceLevel 
            levelName={translate("InfoLoomTwo.WorkplacesPanel[HeaderItem1]", "Education")} 
            levelValues={headers} 
            total={0} 
            isHeader={true}
            translations={{
              total: translate("InfoLoomTwo.WorkplacesPanel[HeaderItem2]", "Total"),
              percent: translate("InfoLoomTwo.WorkplacesPanel[HeaderItem3]", "%"),
              service: translate("InfoLoomTwo.WorkplacesPanel[HeaderItem4]", "Service"),
              commercial: translate("InfoLoomTwo.WorkplacesPanel[HeaderItem5]", "Commercial"),
              leisure: translate("InfoLoomTwo.WorkplacesPanel[HeaderItem6]", "Leisure"),
              extractor: translate("InfoLoomTwo.WorkplacesPanel[HeaderItem7]", "Extractor"),
              industrial: translate("InfoLoomTwo.WorkplacesPanel[HeaderItem8]", "Industrial"),
              office: translate("InfoLoomTwo.WorkplacesPanel[HeaderItem9]", "Office"),
              employee: translate("InfoLoomTwo.WorkplacesPanel[HeaderItem10]", "Employee"),
              commuter: translate("InfoLoomTwo.WorkplacesPanel[HeaderItem11]", "Commuter"),
              open: translate("InfoLoomTwo.WorkplacesPanel[HeaderItem12]", "Open"),
              filled: translate("InfoLoomTwo.WorkplacesPanel[HeaderItem13]", "Filled"),
              totalTooltip: translate("InfoLoomTwo.WorkplacesPanel[TotalTooltip]", "Total workplaces at this education level"),
              percentTooltip: translate("InfoLoomTwo.WorkplacesPanel[PercentTooltip]", "Percentage of total workplaces"),
              serviceTooltip: translate("InfoLoomTwo.WorkplacesPanel[ServiceTooltip]", "Service sector workplaces"),
              commercialTooltip: translate("InfoLoomTwo.WorkplacesPanel[CommercialTooltip]", "Commercial sector workplaces"),
              leisureTooltip: translate("InfoLoomTwo.WorkplacesPanel[LeisureTooltip]", "Leisure sector workplaces"),
              extractorTooltip: translate("InfoLoomTwo.WorkplacesPanel[ExtractorTooltip]", "Extractor sector workplaces"),
              industrialTooltip: translate("InfoLoomTwo.WorkplacesPanel[IndustrialTooltip]", "Industrial sector workplaces"),
              officeTooltip: translate("InfoLoomTwo.WorkplacesPanel[OfficeTooltip]", "Office sector workplaces"),
              employeeTooltip: translate("InfoLoomTwo.WorkplacesPanel[EmployeeTooltip]", "Workplaces filled by city residents"),
              commuterTooltip: translate("InfoLoomTwo.WorkplacesPanel[CommuterTooltip]", "Workplaces filled by commuters from outside the city"),
              openTooltip: translate("InfoLoomTwo.WorkplacesPanel[OpenTooltip]", "Unfilled workplace positions"),
              filledTooltip: translate("InfoLoomTwo.WorkplacesPanel[FilledTooltip]", "Percentage of workplace positions that are filled"),
            }}
          />
          <div className={styles.spacingSmall}></div>
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
              segmentTooltipWithin: translate("InfoLoomTwo.WorkplacesPanel[SegmentTooltipWithin]", "Within {0}"),
              segmentTooltipOfTotal: translate("InfoLoomTwo.WorkplacesPanel[SegmentTooltipOfTotal]", "Of Total Workplaces"),
              barTooltipHeader: translate("InfoLoomTwo.WorkplacesPanel[BarTooltipHeader]", "{0} Summary"),
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
