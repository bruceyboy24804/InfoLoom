import React, { FC, useState } from 'react';
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
import { useRem } from 'cs2/utils';
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

interface StackedBarProps {
  levelName: string | null;
  levelColor: string;
  levelValues: workforceInfo;
  total: number;
  translations: {
    segments: Array<{ label: string }>;
    segmentTooltipCount: string;
    segmentTooltipWithin: string;
    segmentTooltipOfTotal: string;
    barTooltipHeader: string;
    barTooltipTotal: string;
    barTooltipPercentage: string;
  };
}
enum SegmentsType {
  Worker = 0,
  Unemployed = 1,
  Under = 2,
  Outside = 3,
  Homeless = 4,
}

const StackedBar: React.FC<StackedBarProps> = ({ levelName, levelColor, levelValues, total, translations }) => {
  if (total === 0 || levelValues.Total === 0) return null;

  const rem = useRem();
  const barWidthInRem = 600;
  const barHeightInRem = 24;
  const barWidth = barWidthInRem * rem;
  const barHeight = barHeightInRem * rem;

  const segments = [
    {
      label: translations.segments[SegmentsType.Worker].label,
      value: levelValues.Worker,
      color: '#4CAF50',
    },
    {
      label: translations.segments[SegmentsType.Unemployed].label,
      value: levelValues.Unemployed,
      color: '#F44336',
    },
    {
      label: translations.segments[SegmentsType.Under].label,
      value: levelValues.Under,
      color: '#9C27B0',
    },
    {
      label: translations.segments[SegmentsType.Outside].label,
      value: levelValues.Outside,
      color: '#607D8B',
    },
    {
      label: translations.segments[SegmentsType.Homeless].label,
      value: levelValues.Homeless,
      color: '#795548',
    },
  ];

  const totalSegmentValue = segments.reduce((sum, segment) => sum + segment.value, 0);

  // Calculate segment widths in pixels
  let x = 0;
  const svgSegments = segments
    .filter(segment => segment.value > 0)
    .map((segment, idx) => {
      const width = totalSegmentValue > 0 ? (segment.value / totalSegmentValue) * barWidth : 0;
      const rect = <rect key={idx} x={x} y={0} width={width} height={barHeight} fill={segment.color} />;
      x += width;
      return rect;
    });

  return (
    <PanelFoldout
      header={
        <div className={InfoRowTheme.infoRow}>
          <div className={styles.barLabel}>
            <div className={styles.barLabelSymbol} style={{ backgroundColor: levelColor }}></div>
            <div className={styles.barLabelText}>{levelName}</div>
            <div className={classNames(InfoRowTheme.infoRow, InfoRowSCSS.right)}>
              <div className={styles.barLabelValue}>{levelValues.Total.toLocaleString()}</div>
            </div>
          </div>
        </div>
      }
      initialExpanded={false}
      className={styles.stackedBarContainer}
    >
      <div
        style={{
          position: 'relative',
          width: `${barWidthInRem}rem`,
          height: `${barHeightInRem}rem`,
          paddingLeft: '10rem',
          paddingRight: '10rem',
        }}
      >
        <svg width={barWidth} height={barHeight} style={{ display: 'block' }}>
          {svgSegments}
        </svg>
      </div>
      <div style={{ display: 'flex', flexWrap: 'wrap', marginTop: '10rem', fontSize: '15rem' }}>
        {segments.map((segment, idx) => (
          <div key={idx} style={{ display: 'flex', alignItems: 'center' }}>
            <div
              style={{
                width: '15rem',
                height: '15rem',
                backgroundColor: segment.color,
                marginRight: '5rem',
                marginLeft: '10rem',
                borderRadius: '4rem',
              }}
            ></div>
            <div style={{ marginRight: '10rem', display: 'flex', flexDirection: 'row' }}>
              {segment.label}: {segment.value}
            </div>
          </div>
        ))}
      </div>
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
      header={
        <div className={InfoRowTheme.infoRow}>
          <div className={classNames(InfoRowSCSS.left)}>{chartTitle}</div>
        </div>
      }
      initialExpanded={false}
      className={styles.chartSection}
    >
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
const WorkforceTableHeader: React.FC<{ translations: any }> = ({ translations }) => {
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
          <Scrollable smooth={true} vertical={true} trackVisibility={'scrollable'}>
            <WorkforceChart
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
              legendItems={[
                {
                  label: translate(Localekeys.Worker, 'Worker'),
                  color: '#4CAF50',
                },
                {
                  label: translate(Localekeys.Unemployed, 'Unemployed'),
                  color: '#F44336',
                },
                {
                  label: translate(Localekeys.WorkforcePanelUnder, 'Under Employed'),
                  color: '#9C27B0',
                },
                {
                  label: translate(Localekeys.WorkforcePanelOutside, 'Outside'),
                  color: '#607D8B',
                },
                {
                  label: translate(Localekeys.Homeless, 'Homeless'),
                  color: '#795548',
                },
              ]}
              legendTooltipSuffix={translate(
                'InfoLoomTwo.WorkforcePanel[LegendTooltipSuffix]',
                'Click on bar segments above to see detailed breakdown'
              )}
              translations={{
                segments: [
                  {
                    label: translate(Localekeys.Worker, 'Worker'),
                  },
                  {
                    label: translate(Localekeys.Unemployed, 'Unemployed'),
                  },
                  {
                    label: translate(Localekeys.WorkforcePanelUnder, 'Under Employed'),
                  },
                  {
                    label: translate(Localekeys.WorkforcePanelOutside, 'Outside'),
                  },
                  {
                    label: translate(Localekeys.Homeless, 'Homeless'),
                  },
                ],
                segmentTooltipCount: translate('InfoLoomTwo.WorkforcePanel[SegmentTooltipCount]', 'Count'),
                segmentTooltipWithin: translate(
                  'InfoLoomTwo.WorkforcePanel[SegmentTooltipWithin]',
                  'Within Education Level'
                ),
                segmentTooltipOfTotal: translate(
                  'InfoLoomTwo.WorkforcePanel[SegmentTooltipOfTotal]',
                  'Of Total Workforce'
                ),
                barTooltipHeader: translate('InfoLoomTwo.WorkforcePanel[BarTooltipHeader]', 'Summary'),
                barTooltipTotal: translate(Localekeys.Total, 'Total'),
                barTooltipPercentage: translate(
                  'InfoLoomTwo.WorkforcePanel[BarTooltipPercentage]',
                  '% of Total Workforce'
                ),
                totalLabel: translate(Localekeys.Total, 'TOTAL'),
              }}
            />
          </Scrollable>
        </div>
      )}
    </Panel>
  );
};
export default Workforce;
