import React, { memo } from 'react';
import styles from './Demographics.module.scss';
import { useLocalization } from 'cs2/l10n';
import { bindLocalValue, bindValue, useValue, trigger } from 'cs2/api';
import { InfoCheckbox } from 'mods/components/InfoCheckbox/InfoCheckbox';
import { DraggablePanelProps, Dropdown, DropdownToggle, Panel, Scrollable, DropdownItem, Button } from 'cs2/ui';
import {
  DemographicsDataOldestCitizen,
  DemographicsDataTotals,
  DemoStatsToggledOn,
  SetDemoStatsToggledOn,
  DemoGroupingStrategy,
  SetDemoGroupingStrategy,
  DemographicsDetailedData,
  DemographicsFiveYearDetails,
  DemographicsTenYearDetails,
  DemographicsLifecycleDetails,
} from '../../bindings';
import { getModule } from 'cs2/modding';
import { useLegendLabels, useLifecycleLabels, useGroupingStrategies } from './hooks';
import { DemographicsType } from './types';
import { DistrictSelector } from 'mods/InfoloomInfoviewContents/DistrictSelector/districtSelector';
import { ToggleButton, StatisticsPanel, DemographicsChart, ErrorBoundary, LoadingSpinner } from './components';
import mod from 'mod.json';
import { Localekeys } from 'mods/locale';

const demographics = bindValue<boolean>(mod.id, 'BINDING:demographics', false);
const updateDemographics = (value: boolean) => trigger(mod.id, 'TRIGGER:updateDemographics', value);

const DropdownStyle = getModule('game-ui/menu/themes/dropdown.module.scss', 'classes');

const ChartSwitch = bindLocalValue<DemographicsType>(DemographicsType.Employment);
const SetChartSwitch = (switchTo: DemographicsType) => {
  ChartSwitch.update(switchTo);
};

// === Main Demographics Component ===
const Demographics = ({ onClose }: DraggablePanelProps): JSX.Element => {
  const { translate } = useLocalization();
  const demographicsDataStructureDetails = useValue(DemographicsDetailedData.binding);
  const fiveYearDetails = useValue(DemographicsFiveYearDetails.binding);
  const tenYearDetails = useValue(DemographicsTenYearDetails.binding);
  const lifecycleDetails = useValue(DemographicsLifecycleDetails.binding);
  const demographicsDataStructureTotals = useValue(DemographicsDataTotals.binding);
  const demographicsDataOldestCitizen = useValue(DemographicsDataOldestCitizen);
  const demoStatsToggledOn = useValue(DemoStatsToggledOn);
  const demoGroupingStrategy = useValue(DemoGroupingStrategy.binding);
  const chartSwitch = useValue(ChartSwitch);
  const demographicsValue = useValue(demographics);

  // Use custom hooks for translated labels and strategies
  const lifecycleLabels = useLifecycleLabels(translate);
  const legendLabels = useLegendLabels(translate);
  const groupingStrategies = useGroupingStrategies(translate, demographicsDataStructureDetails);

  return (
    <Panel
      draggable
      onClose={onClose}
      className={styles.panel}
      initialPosition={{ x: 0.16, y: 0.15 }}
      header={
        <div className={styles.header}>
          <span className={styles.headerText}>{translate(Localekeys.Demographics, 'Demographics')}</span>
        </div>
      }
    >
      <div className={styles.container}>
        <div className={styles.toggleContainer}>
          <DistrictSelector />
          <InfoCheckbox
            label={translate(Localekeys.ShowStatistics, 'Show Statistics')}
            isChecked={demoStatsToggledOn}
            onToggle={SetDemoStatsToggledOn}
          />
          <Button
            onSelect={() => updateDemographics(true)}
            className={styles.refreshButton}
            variant="flat"
            type="button"
          >
            {translate(Localekeys.Refresh, 'Refresh')}
          </Button>
          <Dropdown
            theme={DropdownStyle}
            content={groupingStrategies.map(strategy => (
              <DropdownItem
                key={strategy.value}
                value={strategy.value}
                closeOnSelect={true}
                onChange={() => SetDemoGroupingStrategy(strategy.value)}
                className={DropdownStyle.dropdownItem}
                selected={demoGroupingStrategy === strategy.value}
              >
                <div className={styles.dropdownItem}>
                  <span>{strategy.label}</span>
                  <span className={styles.itemCount}>
                    ({strategy.ranges.length || demographicsDataStructureDetails?.length || 0})
                  </span>
                </div>
              </DropdownItem>
            ))}
          >
            <DropdownToggle disabled={false}>
              <div className={styles.dropdownName}>
                {groupingStrategies.find(s => s.value === demoGroupingStrategy)?.label || 'Detailed View'}
              </div>
            </DropdownToggle>
          </Dropdown>
        </div>

        {demoStatsToggledOn && (
          <StatisticsPanel
            totals={demographicsDataStructureTotals}
            oldestCitizen={demographicsDataOldestCitizen}
            translate={translate}
          />
        )}
        <div className={styles.chartToggle}>
          <ToggleButton
            selected={chartSwitch === DemographicsType.Employment}
            onSelected={() => SetChartSwitch(DemographicsType.Employment)}
          >
            {translate(Localekeys.Employment, 'Employment')}
          </ToggleButton>

          <ToggleButton
            selected={chartSwitch === DemographicsType.Education}
            onSelected={() => SetChartSwitch(DemographicsType.Education)}
          >
            {translate(Localekeys.Education, 'Education')}
          </ToggleButton>
        </div>
        <Scrollable vertical trackVisibility="always" style={{ flex: 1 }}>
          <div className={styles.chartContainer}>
            <ErrorBoundary>
              {!demographicsDataStructureDetails || demographicsDataStructureDetails.length === 0 ? (
                <LoadingSpinner message="Loading demographics data..." />
              ) : (
                <DemographicsChart
                  StructureDetails={demographicsDataStructureDetails}
                  fiveYearDetails={fiveYearDetails}
                  tenYearDetails={tenYearDetails}
                  lifecycleDetails={lifecycleDetails}
                  groupingStrategy={demoGroupingStrategy}
                  legendLabels={legendLabels}
                  lifecycleLabels={lifecycleLabels}
                  chartSwitch={chartSwitch}
                />
              )}
            </ErrorBoundary>
          </div>
        </Scrollable>
      </div>
    </Panel>
  );
};

// Single memoized export
export default memo(Demographics);
