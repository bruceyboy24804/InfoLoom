import React, { useState } from 'react';
import { useValue } from 'cs2/api';
import { PanelFoldout } from 'cs2/ui';
import { useLocalization } from 'cs2/l10n';
import { getModule } from 'cs2/modding';
import { Theme } from 'cs2/bindings';
import { WorkplacesData } from 'mods/bindings';
import { Localekeys } from 'mods/locale';
import { DistrictSelector } from 'mods/InfoloomInfoviewContents/DistrictSelector/districtSelector';
import WorkplacesTableHeader from '../WorkplacesTableHeader/WorkplacesTableHeader';
import { WorkplacesLevel, HideColumnsToggle } from '../WorkplacesLevel/WorkplacesLevel';
import WorkplacesStackedBarChart from '../StackedBarChart/WorkplacesStackedBarChart';
import styles from './DetailedWorkplacesMain.module.scss';

export const InfoRowTheme: Theme | any = getModule(
  'game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.module.scss',
  'classes'
);

export const DetailedWorkplacesMain: React.FC = () => {
  const { translate } = useLocalization();
  const [chartType, setChartType] = useState<'sector' | 'employment'>('sector');
  const workplaces = useValue(WorkplacesData.binding);

  if (workplaces.length === 0) {
    return <p>Waiting...</p>;
  }

  const total = workplaces[5].Total;

  return (
    <div>
      <div className={styles.toggleContainer}>
        <DistrictSelector />
        <HideColumnsToggle />
      </div>
      <WorkplacesTableHeader
        translations={{
          totalTooltip: translate(
            'InfoLoomTwo.WorkplacesPanel[TotalTooltip]',
            'Total workplaces at this education level'
          ),
          percentTooltip: translate('InfoLoomTwo.WorkplacesPanel[PercentTooltip]', 'Percentage of total workplaces'),
          workerTooltip: translate('InfoLoomTwo.WorkplacesPanel[WorkerTooltip]', 'Workplaces filled by city residents'),
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
          homelessTooltip: translate('InfoLoomTwo.WorkplacesPanel[HomelessTooltip]', 'Sector breakdown of workplaces'),
        }}
      />
      <WorkplacesLevel
        levelColor="#808080"
        levelName={translate(Localekeys.Uneducated, 'Uneducated')}
        levelValues={workplaces[0]}
        total={total}
      />
      <WorkplacesLevel
        levelColor="#B09868"
        levelName={translate(Localekeys.PoorlyEducated, 'Poorly Educated')}
        levelValues={workplaces[1]}
        total={total}
      />
      <WorkplacesLevel
        levelColor="#368A2E"
        levelName={translate(Localekeys.Educated, 'Educated')}
        levelValues={workplaces[2]}
        total={total}
      />
      <WorkplacesLevel
        levelColor="#B981C0"
        levelName={translate(Localekeys.WellEducated, 'Well Educated')}
        levelValues={workplaces[3]}
        total={total}
      />
      <WorkplacesLevel
        levelColor="#5796D1"
        levelName={translate(Localekeys.HighlyEducated, 'Highly Educated')}
        levelValues={workplaces[4]}
        total={total}
      />
      <div className={styles.spacingSmall} />
      <WorkplacesLevel levelName={translate(Localekeys.Total, 'TOTAL')} levelValues={workplaces[5]} total={total} />
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
        <WorkplacesStackedBarChart
          workplaces={workplaces}
          chartType={chartType}
          chartTitle={
            chartType === 'employment'
              ? translate('InfoLoomTwo.WorkplacesPanel[ChartTitleEmployment]', 'Employment Status by Education Level')
              : translate('InfoLoomTwo.WorkplacesPanel[ChartTitleSector]', 'Workplace Distribution by Sector')
          }
          educationLevels={[
            { name: translate(Localekeys.Uneducated, 'Uneducated'), color: '#808080', data: workplaces[0] },
            { name: translate(Localekeys.PoorlyEducated, 'Poorly Educated'), color: '#B09868', data: workplaces[1] },
            { name: translate(Localekeys.Educated, 'Educated'), color: '#368A2E', data: workplaces[2] },
            { name: translate(Localekeys.WellEducated, 'Well Educated'), color: '#B981C0', data: workplaces[3] },
            { name: translate(Localekeys.HighlyEducated, 'Highly Educated'), color: '#5796D1', data: workplaces[4] },
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
  );
};
