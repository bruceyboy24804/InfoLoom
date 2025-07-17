import React, { FC, useState } from 'react';
import {useValue, bindLocalValue} from 'cs2/api';
import { WorkforceData } from 'mods/bindings';
import { DraggablePanelProps, Panel, Tooltip, PanelFoldout, Button} from 'cs2/ui';
import styles from './Workforce.module.scss';
import { workforceInfo } from '../../domain/workforceInfo';
import { useLocalization } from 'cs2/l10n';
export const panelVisibleBinding = bindLocalValue(false);
export const panelTrigger = (state: boolean) => {
    panelVisibleBinding.update(state);
};
interface AlignedParagraphProps {
  left: React.ReactNode;
  right: React.ReactNode;
}

const AlignedParagraph: React.FC<AlignedParagraphProps> = ({ left, right }) => {
  return (
    <p className={styles.alignedParagraph}>
      <span className={styles.alignedParagraphLeft}>{left}</span>
      <span className={styles.alignedParagraphRight}>{right}</span>
    </p>
  );
};

interface HorizontalLineProps {
  length: number;
}

const HorizontalLine: React.FC<HorizontalLineProps> = ({ length }) => {
  return <div className={styles.horizontalLine} style={{ width: `${length}rem` }}></div>;
};

interface WorkforceLevelProps {
  levelColor?: string;
  levelName: string | null;
  levelValues: workforceInfo;
  total: number;
  isHeader?: boolean;
  translations?: {
    totalTooltip: string | null;
    total: string | null;
    percentTooltip: string | null;
    workerTooltip: string | null;
    worker: string | null;
    unemployedTooltip: string | null;
    unemployed: string | null;
    unemploymentTooltip: string | null;
    unemployment: string | null;
    employableTooltip: string | null;
    employable: string | null;
    underTooltip: string | null;
    under: string | null;
    outsideTooltip: string | null;
    outside: string | null;
    homelessTooltip: string | null;
    homeless: string | null;
  };
}

interface StackedBarProps {
  levelName: string | null;
  levelColor: string;
  levelValues: workforceInfo;
  total: number;
  translations: {
    segments: Array<{ label: string; }>;
    segmentTooltipCount: string;
    segmentTooltipWithin: string;
    segmentTooltipOfTotal: string;
    barTooltipHeader: string;
    barTooltipTotal: string;
    barTooltipPercentage: string;
  };
}

const WorkforceLevel: React.FC<WorkforceLevelProps> = ({
  levelColor,
  levelName,
  levelValues,
  total,
  isHeader = false,
  translations,
}) => {
  const percent = total > 0 ? ((100 * levelValues.Total) / total).toFixed(1) + '%' : '';
  const unemployment =
    levelValues.Total > 0
      ? ((100 * levelValues.Unemployed) / levelValues.Total).toFixed(1) + '%'
      : '';

  return (
    <div className={`labels_L7Q row_S2v ${styles.workforceLevel}`}>
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
          <Tooltip tooltip={translations?.totalTooltip} direction="down" alignment="center">
            <span>{translations?.total}</span>
          </Tooltip>
        ) : (
          levelValues.Total.toLocaleString()
        )}
      </div>
      <div className={`row_S2v ${styles.percentColumn}`}>
        {isHeader ? (
          <Tooltip tooltip={translations?.percentTooltip} direction="down" alignment="center">
            <span>%</span>
          </Tooltip>
        ) : (
          percent
        )}
      </div>
      <div className={`row_S2v ${styles.dataColumn}`}>
        {isHeader ? (
          <Tooltip tooltip={translations?.workerTooltip} direction="down" alignment="center">
            <span>{translations?.worker}</span>
          </Tooltip>
        ) : (
          levelValues.Worker.toLocaleString()
        )}
      </div>
      <div className={`row_S2v ${styles.unemployedColumn}`}>
        {isHeader ? (
          <Tooltip tooltip={translations?.unemployedTooltip} direction="down" alignment="center">
            <span>{translations?.unemployed}</span>
          </Tooltip>
        ) : (
          levelValues.Unemployed.toLocaleString()
        )}
      </div>
      <div className={`row_S2v small_ExK ${styles.percentColumn}`}>
        {isHeader ? (
          <Tooltip tooltip={translations?.unemploymentTooltip} direction="down" alignment="center">
            <span>{translations?.unemployment}</span>
          </Tooltip>
        ) : (
          unemployment
        )}
      </div>
      <div className={`row_S2v small_ExK ${styles.smallColumn}`}>
        {isHeader ? (
          <Tooltip tooltip={translations?.underTooltip} direction="down" alignment="center">
            <span>{translations?.under}</span>
          </Tooltip>
        ) : (
          levelValues.Under.toLocaleString()
        )}
      </div>
      <div className={`row_S2v small_ExK ${styles.smallColumn}`}>
        {isHeader ? (
          <Tooltip tooltip={translations?.outsideTooltip} direction="down" alignment="center">
            <span>{translations?.outside}</span>
          </Tooltip>
        ) : (
          levelValues.Outside.toLocaleString()
        )}
      </div>
      <div className={`row_S2v ${styles.employableColumn}`}>
        {isHeader ? (
          <Tooltip tooltip={translations?.employableTooltip} direction="down" alignment="center">
            <span>{translations?.employable}</span>
          </Tooltip>
        ) : (
          levelValues.Employable.toLocaleString()
        )}
      </div>
      <div className={`row_S2v small_ExK ${styles.smallColumn}`}>
        {isHeader ? (
          <Tooltip tooltip={translations?.homelessTooltip} direction="down" alignment="center">
            <span>{translations?.homeless}</span>
          </Tooltip>
        ) : (
          levelValues.Homeless.toLocaleString()
        )}
      </div>
    </div>
  );
};

const StackedBar: React.FC<StackedBarProps> = ({ levelName, levelColor, levelValues, total, translations }) => {
  if (total === 0 || levelValues.Total === 0) return null;

  const segments = [
    { label: translations.segments[0].label, value: levelValues.Worker, color: '#4CAF50' },
    { label: translations.segments[1].label, value: levelValues.Unemployed, color: '#F44336' },
    { label: translations.segments[3].label, value: levelValues.Under, color: '#9C27B0' },
    { label: translations.segments[4].label, value: levelValues.Outside, color: '#607D8B' },
    { label: translations.segments[2].label, value: levelValues.Employable, color: '#FF9800' },
    { label: translations.segments[5].label, value: levelValues.Homeless, color: '#795548' },
  ];

  const totalSegmentValue = segments.reduce((sum, segment) => sum + segment.value, 0);

  const createTooltipContent = (segment: (typeof segments)[0]) => {
    const percentage = totalSegmentValue > 0 ? (segment.value / totalSegmentValue) * 100 : 0;
    const totalPercentage = total > 0 ? (segment.value / total) * 100 : 0;

    return (
      <div className={styles.tooltipContent}>
        <div className={styles.tooltipHeader}>
          {levelName}
          <span style={{ margin: '0 8px' }}>-</span>
          {segment.label}
        </div>
        <div className={styles.tooltipRow}>
          <span>{translations.segmentTooltipCount}:</span>
          <span>{segment.value.toLocaleString()}</span>
        </div>
        <div className={styles.tooltipRow}>
          <span>{translations.segmentTooltipWithin}:</span>
          <span>{percentage.toFixed(1)}%</span>
        </div>
        <div className={styles.tooltipRow}>
          <span>{translations.segmentTooltipOfTotal}:</span>
          <span>{totalPercentage.toFixed(1)}%</span>
        </div>
      </div>
    );
  };

  const barTooltipContent = (
    <div className={styles.tooltipContent}>
      <div className={styles.tooltipHeader}>{levelName} {translations.barTooltipHeader}</div>
      <div className={styles.tooltipRow}>
        <span>{translations.barTooltipTotal}:</span>
        <span>{levelValues.Total.toLocaleString()}</span>
      </div>
      <div className={styles.tooltipRow}>
        <span>{translations.barTooltipPercentage}:</span>
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

const WorkforceChart: React.FC<{ 
  workforce: workforceInfo[]; 
  chartTitle: string | null;
  educationLevels: Array<{ name: string | null; color: string; data: workforceInfo }>;
  legendItems: Array<{ label: string | null; color: string }>;
  legendTooltipSuffix: string | null;
  translations: any;
}> = ({ workforce, chartTitle, educationLevels, legendItems, legendTooltipSuffix, translations }) => {
  return (
    <PanelFoldout
      header={<div className={styles.chartTitle}>{chartTitle}</div>}
      initialExpanded={false}
      className={styles.chartSection}
    >
      <div className={styles.legendContainer}>
        {legendItems.map((item, index) => (
          <Tooltip
            key={index}
            tooltip={`${item.label}: ${legendTooltipSuffix}`}
            direction="up"
            alignment="center"
          >
            <div className={styles.legendItem}>
              <div className={styles.legendSymbol} style={{ backgroundColor: item.color || '#000000' }}></div>
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
          total={workforce[5].Total}
          translations={translations}
        />
      ))}

      <StackedBar
        levelName={translations.totalLabel}
        levelColor="#FFFFFF"
        levelValues={workforce[5]}
        total={workforce[5].Total}
        translations={translations}
      />
    </PanelFoldout>
  );
};

const Workforce: FC<DraggablePanelProps> = ({ onClose, initialPosition }) => {
  const { translate } = useLocalization();
  const workforce = useValue(WorkforceData);
    const isPanelOpen = useValue(panelVisibleBinding);
  const headers: workforceInfo = {
    Total: 0,
    Worker: 0,
    Unemployed: 0,
    Homeless: 0,
    Employable: 0,
    Under: 0,
    Outside: 0,
  };


  return (
    <Panel
      draggable={true}
      onClose={onClose}
      initialPosition={{ x: 0.71, y: 0.7 }}
      className={styles.panel}
      header={
        <div className={styles.header}>
            <div className={styles.headerText}>
                <span className={styles.title}>{translate("InfoLoomTwo.WorkforcePanel[Title]", "Workforce")}</span>
            </div>
        </div>
      }
    >
      {workforce.length === 0 ? (
        <p>{translate("InfoLoomTwo.WorkforcePanel[Waiting]", "Waiting...")}</p>
      ) : (
        <div>
          <div className={styles.spacingTop}></div>
          <WorkforceLevel 
            levelName={translate("InfoLoomTwo.WorkforcePanel[HeaderItem1]", "Education")} 
            levelValues={headers} 
            total={0} 
            isHeader={true}
            translations={{
              totalTooltip: translate("InfoLoomTwo.WorkforcePanel[TotalTooltip]", "Total population at this education level"),
              total: translate("InfoLoomTwo.WorkforcePanel[HeaderItem2]", "Total"),
              percentTooltip: translate("InfoLoomTwo.WorkforcePanel[PercentTooltip]", "Percentage of total workforce"),
              workerTooltip: translate("InfoLoomTwo.WorkforcePanel[WorkerTooltip]", "Citizens currently employed and working"),
              worker: translate("InfoLoomTwo.WorkforcePanel[HeaderItem3]", "Worker"),
              unemployedTooltip: translate("InfoLoomTwo.WorkforcePanel[UnemployedTooltip]", "Citizens actively looking for work but currently unemployed"),
              unemployed: translate("InfoLoomTwo.WorkforcePanel[HeaderItem4]", "Unemployed"),
              unemploymentTooltip: translate("InfoLoomTwo.WorkforcePanel[UnemploymentTooltip]", "Unemployment rate: unemployed / total population"),
              unemployment: translate("InfoLoomTwo.WorkforcePanel[HeaderItem5]", "Unemployment"),
              employableTooltip: translate("InfoLoomTwo.WorkforcePanel[EmployableTooltip]", "Sum of Under Employed and Citizens who are working outside of the city"),
              employable: translate("InfoLoomTwo.WorkforcePanel[HeaderItem6]", "Employable"),
              underTooltip: translate("InfoLoomTwo.WorkforcePanel[UnderTooltip]", "Citizens who are woking below the education level (Under employed)"),
              under: translate("InfoLoomTwo.WorkforcePanel[HeaderItem7]", "Under"),
              outsideTooltip: translate("InfoLoomTwo.WorkforcePanel[OutsideTooltip]", "Citizens who are working outside of the city"),
              outside: translate("InfoLoomTwo.WorkforcePanel[HeaderItem8]", "Outside"),
              homelessTooltip: translate("InfoLoomTwo.WorkforcePanel[HomelessTooltip]", "Citizens without permanent housing"),
              homeless: translate("InfoLoomTwo.WorkforcePanel[HeaderItem9]", "Homeless"),
            }}
          />
          <div className={styles.spacingSmall}></div>
          <WorkforceLevel
            levelColor="#808080"
            levelName={translate("InfoLoomTwo.WorkforcePanel[Row1EL]", "Uneducated")}
            levelValues={workforce[0]}
            total={workforce[5].Total}
          />
          <WorkforceLevel
            levelColor="#B09868"
            levelName={translate("InfoLoomTwo.WorkforcePanel[Row2EL]", "Poorly Educated")}
            levelValues={workforce[1]}
            total={workforce[5].Total}
          />
          <WorkforceLevel
            levelColor="#368A2E"
            levelName={translate("InfoLoomTwo.WorkforcePanel[Row3EL]", "Educated")}
            levelValues={workforce[2]}
            total={workforce[5].Total}
          />
          <WorkforceLevel
            levelColor="#B981C0"
            levelName={translate("InfoLoomTwo.WorkforcePanel[Row4EL]", "Well Educated")}
            levelValues={workforce[3]}
            total={workforce[5].Total}
          />
          <WorkforceLevel
            levelColor="#5796D1"
            levelName={translate("InfoLoomTwo.WorkforcePanel[Row5EL]", "Highly Educated")}
            levelValues={workforce[4]}
            total={workforce[5].Total}
          />
          <div className={styles.spacingSmall}></div>
          <WorkforceLevel levelName={translate("InfoLoomTwo.WorkforcePanel[Row6]", "TOTAL")} levelValues={workforce[5]} total={0} />

          <WorkforceChart 
            workforce={workforce}
            chartTitle={translate("InfoLoomTwo.WorkforcePanel[StackedBarTitle]", "Workforce Distribution by Education Level")}
            educationLevels={[
              { name: translate("InfoLoomTwo.WorkforcePanel[StackedBar1]", 'Uneducated'), color: '#808080', data: workforce[0] },
              { name: translate("InfoLoomTwo.WorkforcePanel[StackedBar2]", 'Poorly Educated'), color: '#B09868', data: workforce[1] },
              { name: translate("InfoLoomTwo.WorkforcePanel[StackedBar3]", 'Educated'), color: '#368A2E', data: workforce[2] },
              { name: translate("InfoLoomTwo.WorkforcePanel[StackedBar4]", 'Well Educated'), color: '#B981C0', data: workforce[3] },
              { name: translate("InfoLoomTwo.WorkforcePanel[StackedBar5]", 'Highly Educated'), color: '#5796D1', data: workforce[4] },
            ]}
            legendItems={[
              { label: translate("InfoLoomTwo.WorkforcePanel[StackedBarLegendItem1]", 'Worker'), color: '#4CAF50' },
              { label: translate("InfoLoomTwo.WorkforcePanel[StackedBarLegendItem2]", 'Unemployed'), color: '#F44336' },
              { label: translate("InfoLoomTwo.WorkforcePanel[StackedBarLegendItem4]", 'Under Employed'), color: '#9C27B0' },
              { label: translate("InfoLoomTwo.WorkforcePanel[StackedBarLegendItem5]", 'Outside'), color: '#607D8B' },
              { label: translate("InfoLoomTwo.WorkforcePanel[StackedBarLegendItem3]", 'Employable'), color: '#FF9800' },
              { label: translate("InfoLoomTwo.WorkforcePanel[StackedBarLegendItem6]", 'Homeless'), color: '#795548' },
            ]}
            legendTooltipSuffix={translate("InfoLoomTwo.WorkforcePanel[LegendTooltipSuffix]", "Click on bar segments above to see detailed breakdown")}
            translations={{
              segments: [
                { label: translate("InfoLoomTwo.WorkforcePanel[StackedBarLegendItem1]", 'Worker') },
                { label: translate("InfoLoomTwo.WorkforcePanel[StackedBarLegendItem2]", 'Unemployed') },
                { label: translate("InfoLoomTwo.WorkforcePanel[StackedBarLegendItem4]", 'Under Employed') },
                { label: translate("InfoLoomTwo.WorkforcePanel[StackedBarLegendItem5]", 'Outside') },
                { label: translate("InfoLoomTwo.WorkforcePanel[StackedBarLegendItem3]", 'Employable') },
                { label: translate("InfoLoomTwo.WorkforcePanel[StackedBarLegendItem6]", 'Homeless') },
              ],
              segmentTooltipCount: translate("InfoLoomTwo.WorkforcePanel[SegmentTooltipCount]", "Count"),
              segmentTooltipWithin: translate("InfoLoomTwo.WorkforcePanel[SegmentTooltipWithin]", "Within Education Level"),
              segmentTooltipOfTotal: translate("InfoLoomTwo.WorkforcePanel[SegmentTooltipOfTotal]", "Of Total Workforce"),
              barTooltipHeader: translate("InfoLoomTwo.WorkforcePanel[BarTooltipHeader]", "Summary"),
              barTooltipTotal: translate("InfoLoomTwo.WorkforcePanel[BarTooltipTotal]", "Total"),
              barTooltipPercentage: translate("InfoLoomTwo.WorkforcePanel[BarTooltipPercentage]", "% of Total Workforce"),
              totalLabel: translate("InfoLoomTwo.WorkforcePanel[StackedBar6]", "TOTAL"),
            }}
          />
        </div>
      )}
    </Panel>
  );
};

export default Workforce;
