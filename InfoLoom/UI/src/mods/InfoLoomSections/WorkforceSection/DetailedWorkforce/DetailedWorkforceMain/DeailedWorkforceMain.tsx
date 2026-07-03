import { Localekeys } from 'mods/locale';
import { WorkforceStackedBarChart } from '../StackedBarChart/StackedBarChart';
import { WorkforceTableHeader } from '../TableHeader/TableHeader';
import { WorkforceLine } from '../WorkforceLine/WorkforceLine';
import { DistrictSelector } from 'mods/InfoloomInfoviewContents/DistrictSelector/districtSelector';
import { useLocalization } from 'cs2/l10n';
import { useValue } from 'cs2/api';
import { WorkforceData } from '../../../../bindings';
import styles from './DetailedWorkforceMain.module.scss';
import { PanelFoldout } from 'cs2/ui';
import { Theme } from 'cs2/bindings';
import { getModule } from 'cs2/modding';

export const InfoRowTheme: Theme | any = getModule(
  'game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.module.scss',
  'classes'
);

export const DetailedWorkforceMain = () => {
  const { translate } = useLocalization();
  const workforce = useValue(WorkforceData.binding);

  return workforce.length === 0 ? (
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
  );
};
