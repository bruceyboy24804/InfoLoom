import React, { FC, useState } from 'react';
import {useValue, bindLocalValue, bindValue} from 'cs2/api';
import { WorkforceData } from 'mods/bindings';
import { DraggablePanelProps, Panel, Tooltip, PanelFoldout, Button} from 'cs2/ui';
import styles from './Workforce.module.scss';
import { workforceInfo } from '../../domain/workforceInfo';
import { useLocalization } from 'cs2/l10n';
import mod from "mod.json";
import { InfoRowSCSS } from "mods/InfoLoomSections/ILInfoSections/Modules/info-Row/info-Row.module.scss";
import { getModule } from 'cs2/modding';
import { hideColumnsBinding } from '../WorkplacesSection/Workplaces';
import {DistrictSelector} from "../../InfoloomInfoviewContents/DistrictSelector/districtSelector";
const ShowExtraWorkforce = bindValue<number>(mod.id, "ShowExtraWorkforce", 0);



interface Props { 
  value: number;
  start: number;
  end: number;
  step?: number;
onChange: (newValue: number) => void}

const Slider: FC<Props> = getModule("game-ui/common/input/slider/slider.tsx", "Slider");
const SliderRange = bindLocalValue<number[]>([0, 7]);
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



const StackedBar: React.FC<StackedBarProps> = ({ levelName, levelColor, levelValues, total, translations }) => {
  if (total === 0 || levelValues.Total === 0) return null;

  const segments = [
    { label: translations.segments[0].label, value: levelValues.Worker, color: '#4CAF50' },
    { label: translations.segments[1].label, value: levelValues.Unemployed, color: '#F44336' },
    { label: translations.segments[2].label, value: levelValues.Under, color: '#9C27B0' },
    { label: translations.segments[3].label, value: levelValues.Outside, color: '#607D8B' },
    { label: translations.segments[4].label, value: levelValues.Homeless, color: '#795548' },
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

// Header component
const WorkforceTableHeader: React.FC<{ translations: any; }> = ({ translations }) => {
  const value = ShowExtraWorkforce.value;

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
      {value < 7 && (
        <div className={styles.col3}>
          <Tooltip tooltip={translations?.workerTooltip} direction="down" alignment="center">
            <span>%</span>
          </Tooltip>
        </div>
      )}
      {value < 6 && (
        <div className={styles.col4}>
          <Tooltip tooltip={translations?.unemployedTooltip} direction="down" alignment="center">
            <span>Worker</span>
          </Tooltip>
        </div>
      )}
      {value < 5 && (
        <div className={styles.col5}>
          <Tooltip tooltip={translations?.unemploymentTooltip} direction="down" alignment="center">
            <span>Unemployed</span>
          </Tooltip>
        </div>
      )}
      {value < 4 && (
        <div className={styles.col6}>
          <span>%</span>
        </div>
      )}
      {value < 3 && (
        <div className={styles.col7}>
          <Tooltip tooltip={translations?.outsideTooltip} direction="down" alignment="center">
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
}

const WorkforceLine: React.FC<WorkforceLineProps> = ({
  levelColor,
  levelName,
  levelValues,
  total,
}) => {
  const value = SliderRange.value[0];
  const percent = total > 0 ? ((100 * levelValues.Total) / total).toFixed(1) + '%' : '';
  const unemployment =
    levelValues.Total > 0
      ? ((100 * levelValues.Unemployed) / levelValues.Total).toFixed(1) + '%'
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
      {value < 7 && (
        <div className={styles.col3}><span>{percent}</span></div>
      )}
      {value < 6 && (
        <div className={styles.col4}><span>{levelValues.Worker.toLocaleString()}</span></div>
      )}
      {value < 5 && (
        <div className={styles.col5}><span>{levelValues.Unemployed.toLocaleString()}</span></div>
      )}
      {value < 4 && (
        <div className={styles.col6}><span>{unemployment}</span></div>
      )}
      {value < 3 && (
        <div className={styles.col7}><span>{levelValues.Under.toLocaleString()}</span></div>
      )}
      {value < 2 && (
        <div className={styles.col8}><span>{levelValues.Outside.toLocaleString()}</span></div>
      )}
      {value < 1 && (
        <div className={styles.col9}><span>{levelValues.Homeless.toLocaleString()}</span></div>
      )}
    </div>
  );
};






const Workforce: FC<DraggablePanelProps> = ({ onClose, initialPosition }) => {
  const { translate } = useLocalization();
  const workforce = useValue(WorkforceData);
  const isPanelOpen = useValue(panelVisibleBinding);
  const showExtraWorkforce = useValue(ShowExtraWorkforce);
  const headers: workforceInfo = {
    Total: 0,
    Worker: 0,
    Unemployed: 0,
    Homeless: 0,
    Under: 0,
    Outside: 0,
  };


  return (
    <Panel
      draggable={true}
      onClose={onClose}
      initialPosition={{ x: 0.71, y: 0.7 }}
      className={ShowExtraWorkforce.value == 1 ? styles.panel1 : 
        ShowExtraWorkforce.value == 2 ? styles.panel2 : 
        ShowExtraWorkforce.value == 3 ? styles.panel3 : 
        ShowExtraWorkforce.value == 4 ? styles.panel4 :
        ShowExtraWorkforce.value == 5 ? styles.panel5 :
        ShowExtraWorkforce.value == 6 ? styles.panel6 :
        ShowExtraWorkforce.value == 7 ? styles.panel7 :
        styles.panel}
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
          <div className={styles.toggleContainer}>
            <DistrictSelector />
          </div>
            <WorkforceTableHeader
            translations={{
              totalTooltip: translate("InfoLoomTwo.WorkforcePanel[TotalTooltip]", "Total Workforce"),
              percentTooltip: translate("InfoLoomTwo.WorkforcePanel[PercentTooltip]", "Percentage of Total Workforce"),
              workerTooltip: translate("InfoLoomTwo.WorkforcePanel[WorkerTooltip]", "Workers"),
              unemployedTooltip: translate("InfoLoomTwo.WorkforcePanel[UnemployedTooltip]", "Unemployed"),
              unemploymentTooltip: translate("InfoLoomTwo.WorkforcePanel[UnemploymentTooltip]", "Unemployment Rate"),
              underTooltip: translate("InfoLoomTwo.WorkforcePanel[UnderTooltip]", "Under Employed"),
              outsideTooltip: translate("InfoLoomTwo.WorkforcePanel[OutsideTooltip]", "Outside Workforce"),
              homelessTooltip: translate("InfoLoomTwo.WorkforcePanel[HomelessTooltip]", "Homeless Workforce"),
            }}
          />
          <WorkforceLine
            levelColor="#808080"
            levelName={translate("InfoLoomTwo.WorkforcePanel[Row1EL]", "Uneducated")}
            levelValues={workforce[0]}
            total={workforce[5].Total}
            
          />
          <WorkforceLine
            levelColor="#B09868"
            levelName={translate("InfoLoomTwo.WorkforcePanel[Row2EL]", "Poorly Educated")}
            levelValues={workforce[1]}
            total={workforce[5].Total}
            
          />
          <WorkforceLine
            levelColor="#368A2E"
            levelName={translate("InfoLoomTwo.WorkforcePanel[Row3EL]", "Educated")}
            levelValues={workforce[2]}
            total={workforce[5].Total}
            
          />
          <WorkforceLine
            levelColor="#B981C0"
            levelName={translate("InfoLoomTwo.WorkforcePanel[Row4EL]", "Well Educated")}
            levelValues={workforce[3]}
            total={workforce[5].Total}
            
          />
          <WorkforceLine
            levelColor="#5796D1"
            levelName={translate("InfoLoomTwo.WorkforcePanel[Row5EL]", "Highly Educated")}
            levelValues={workforce[4]}
            total={workforce[5].Total}
            
          />
          <div className={styles.spacingSmall}></div>
          <WorkforceLine 
            levelName={translate("InfoLoomTwo.WorkforcePanel[Row6]", "TOTAL")} 
            levelValues={workforce[5]} 
            total={0} 
            
          />
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
              { label: translate("InfoLoomTwo.WorkforcePanel[StackedBarLegendItem6]", 'Homeless'), color: '#795548' },
            ]}
            legendTooltipSuffix={translate("InfoLoomTwo.WorkforcePanel[LegendTooltipSuffix]", "Click on bar segments above to see detailed breakdown")}
            translations={{
              segments: [
                { label: translate("InfoLoomTwo.WorkforcePanel[StackedBarLegendItem1]", 'Worker') },
                { label: translate("InfoLoomTwo.WorkforcePanel[StackedBarLegendItem2]", 'Unemployed') },
                { label: translate("InfoLoomTwo.WorkforcePanel[StackedBarLegendItem4]", 'Under Employed') },
                { label: translate("InfoLoomTwo.WorkforcePanel[StackedBarLegendItem5]", 'Outside') },
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


